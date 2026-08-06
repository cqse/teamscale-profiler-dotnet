using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using UploadDaemon.Archiving;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Configuration;
using UploadDaemon.Scanning;
using UploadDaemon.Upload;
using UploadDaemon.Report;
using File = System.IO.File;

namespace UploadDaemon
{
    /// <summary>
    /// Triggered any time the timer goes off. Performs the scan and upload/archiving of trace files.
    /// </summary>
    public class UploadTask
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IFileSystem fileSystem;
        private readonly IUploadFactory uploadFactory;
        private readonly ILineCoverageSynthesizerFactory lineCoverageSynthesizerFactory;

        /// <summary>
        /// Configuration problems already reported during this run, to avoid logging the same
        /// problem once per affected trace file.
        /// </summary>
        private readonly ISet<string> reportedConfigErrors = new HashSet<string>();

        public UploadTask(IFileSystem fileSystem, IUploadFactory uploadFactory, ILineCoverageSynthesizerFactory lineCoverageSynthesizerFactory)
        {
            this.fileSystem = fileSystem;
            this.uploadFactory = uploadFactory;
            this.lineCoverageSynthesizerFactory = lineCoverageSynthesizerFactory;
        }

        /// <summary>
        /// Scans the trace directories for traces to process and either tries to upload or archive them.
        /// </summary>
        public void Run(Config config)
        {
            foreach (string traceDirectory in config.TraceDirectoriesToWatch)
            {
                ScanDirectory(traceDirectory, config);
            }
        }

        private void ScanDirectory(string traceDirectory, Config config)
        {
            logger.Debug("Scanning trace directory {traceDirectory}", traceDirectory);

            TraceFileScanner scanner = new TraceFileScanner(traceDirectory, fileSystem);
            Archive archive = new Archive(traceDirectory, fileSystem, new DefaultDateTimeProvider());
            LineCoverageMerger coverageMerger = new LineCoverageMerger();

            ProcessTraceFiles(scanner.ListTraceFilesReadyForUpload(), archive, config, coverageMerger);

            UploadMergedCoverage(archive, coverageMerger, config);

            logger.Debug("Finished scan");
        }

        /// <summary>
        /// Processes all given trace files and logs those that could not be processed, so they are retried later.
        /// </summary>
        private void ProcessTraceFiles(IEnumerable<TraceFile> traces, Archive archive, Config config, LineCoverageMerger coverageMerger)
        {
            List<string> errorTraceFilePaths = new List<string>();
            foreach (TraceFile traceFile in traces)
            {
                try
                {
                    ProcessTraceFile(traceFile, archive, config, coverageMerger);
                }
                catch (Config.InvalidConfigException e)
                {
                    ReportConfigError(e);
                    errorTraceFilePaths.Add(traceFile.FilePath);
                }
                catch (Exception e)
                {
                    logger.Debug(e, "Failed to process trace file {trace}. Will retry later", traceFile.FilePath);
                    errorTraceFilePaths.Add(traceFile.FilePath);
                }
            }
            if (errorTraceFilePaths.Count > 0)
            {
                logger.Error("Failed to process trace files {traces}. Will retry later", String.Join(", ", errorTraceFilePaths));
            }
        }

        /// <summary>
        /// Logs the given configuration problem unless it was already reported during this run. The same invalid
        /// configuration applies to every trace file of that process, hence we report each distinct problem only
        /// once instead of once per affected trace file.
        /// </summary>
        private void ReportConfigError(Config.InvalidConfigException exception)
        {
            if (reportedConfigErrors.Add(exception.Message))
            {
                logger.Error(exception, "Invalid configuration. No coverage will be uploaded until this is fixed");
            }
        }

        private static void UploadMergedCoverage(Archive archive, LineCoverageMerger coverageMerger, Config config)
        {
            IEnumerable<LineCoverageMerger.CoverageBatch> batches = coverageMerger.GetBatches();
            if (batches.Count() == 0)
            {
                logger.Debug("Skipping upload of merged coverage since none was recorded");
                return;
            }

            logger.Debug("Uploading line coverage of {count} batches", batches.Count());
            foreach (LineCoverageMerger.CoverageBatch batch in batches)
            {
                UploadCoverageBatch(archive, config, batch);
            }
        }

        private static void UploadCoverageBatch(Archive archive, Config config, LineCoverageMerger.CoverageBatch batch)
        {
            logger.Debug("Uploading merged line coverage from {traceFile} to {upload}",
                                string.Join(", ", batch.TraceFilePaths), batch.Upload.Describe());
            ICoverageReport report = batch.AggregatedCoverageReport;

            string traceFilePaths = string.Join(", ", batch.TraceFilePaths);

            if (config.ArchiveLineCoverage)
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                archive.ArchiveCoverageReport($"merged_{timestamp}", report);
            }

            if (RunSync(batch.Upload.UploadLineCoverageAsync(traceFilePaths, report, batch.RevisionOrTimestamp)))
            {
                foreach (string tracePath in batch.TraceFilePaths)
                {
                    archive.ArchiveUploadedFile(tracePath);
                }
            }
            else
            {
                logger.Error("Failed to upload merged line coverage from {traceFile} to {upload}. Will retry later", traceFilePaths, batch.Upload.Describe());
            }
        }

        private void ProcessTraceFile(TraceFile traceFile, Archive archive, Config config, LineCoverageMerger coverageMerger)
        {
            if (traceFile.IsEmpty())
            {
                logger.Info("Archiving {trace} because it does not contain any coverage", traceFile.FilePath);
                archive.ArchiveEmptyFile(traceFile.FilePath);
                return;
            }
            string processPath = traceFile.FindProcessPath();
            if (processPath == null)
            {
                logger.Info("Archiving {trace} because it does not contain a Process= line", traceFile.FilePath);
                archive.ArchiveFileWithoutProcess(traceFile.FilePath);
                return;
            }
            AssemblyExtractor assemblyExtractor = new AssemblyExtractor();
            assemblyExtractor.ExtractAssemblies(traceFile.Lines);
            Config.ConfigForProcess processConfig = config.CreateConfigForProcess(processPath, assemblyExtractor.Assemblies);
            IUpload upload = uploadFactory.CreateUpload(processConfig, fileSystem);

            ProcessLineCoverage(traceFile, assemblyExtractor, archive, config, processConfig, upload, coverageMerger);
        }

        private void ProcessLineCoverage(TraceFile traceFile, AssemblyExtractor assemblyExtractor, Archive archive, Config config, Config.ConfigForProcess processConfig, IUpload upload, LineCoverageMerger coverageMerger)
        {
            logger.Debug("Preparing line coverage from {traceFile} for {upload}", traceFile.FilePath, upload.Describe());
            RevisionFileUtils.RevisionOrTimestamp timestampOrRevision = ParseRevisionFile(traceFile.FilePath, processConfig, assemblyExtractor);
            ICoverageReport coverageReport = ConvertTraceToCoverageReport(traceFile, archive, processConfig, assemblyExtractor);
            if (timestampOrRevision == null)
            {
                logger.Debug("No timestamp or revision found for {traceFile}, will retry", traceFile.FilePath);
                return;
            }
                
            if (coverageReport == null)
            {
                logger.Debug("Failed to parse {traceFile}, will retry", traceFile.FilePath);
                return;
            }

            if (config.ArchiveLineCoverage)
            {
                archive.ArchiveCoverageReport(Path.GetFileName(traceFile.FilePath), coverageReport);
            }

            if (processConfig.MergeLineCoverage)
            {
                logger.Debug("Merging line coverage from {traceFile} into previous line coverage", traceFile.FilePath);
                coverageMerger.AddLineCoverage(traceFile.FilePath, timestampOrRevision, upload, coverageReport);
                return;
            }

            logger.Debug("Uploading line coverage from {traceFile} to {upload}", traceFile.FilePath, upload.Describe());
            if (RunSync(upload.UploadLineCoverageAsync(traceFile.FilePath, coverageReport, timestampOrRevision)))
            {
                archive.ArchiveUploadedFile(traceFile.FilePath);
            }
            else
            {
                logger.Error("Failed to upload line coverage from {traceFile} to {upload}. Will retry later", traceFile.FilePath, upload.Describe());
            }
        }


        /// <summary>
        /// Tries to read the revision or upload target file based on the config (absolute path, or relative to loaded assemblies).
        /// </summary>
        private RevisionFileUtils.RevisionOrTimestamp ParseRevisionFile(string traceFilePath, Config.ConfigForProcess processConfig, AssemblyExtractor assemblyExtractor)
        {
            string revisionFile = processConfig.RevisionFile;
            if (revisionFile == null)
            {
                logger.Info("No revision file found.");
                return null;
            }
            if (Config.IsAssemblyRelativePath(revisionFile))
            {
                foreach (KeyValuePair<uint, (string, string)> entry in assemblyExtractor.Assemblies)
                {
                    string resolvedRevisionFile = Config.ResolveAssemblyRelativePath(revisionFile, entry.Value.Item2);
                    if (File.Exists(resolvedRevisionFile))
                    {
                        logger.Info("Using revision file {revisionFile} while processing {traceFile}.", resolvedRevisionFile, traceFilePath);
                        return ParseRevisionFile(resolvedRevisionFile, traceFilePath);
                    }
                }
            }
            return ParseRevisionFile(revisionFile, traceFilePath);
        }

        /// <summary>
        /// Tries to read the revision file. Logs and returns null if this fails.
        /// </summary>
        private RevisionFileUtils.RevisionOrTimestamp ParseRevisionFile(string revisionFile, string traceFile)
        {
            try
            {
                return RevisionFileUtils.Parse(fileSystem.File.ReadAllLines(revisionFile), revisionFile);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to read revision file {revisionFile} while processing {traceFile}. Will retry later",
                    revisionFile, traceFile);
                return null;
            }
        }

        /// <summary>
        /// Tries to read and convert the trace file. Logs and returns null if this fails.
        /// Empty trace files are archived and null is returned as well.
        /// </summary>
        private ICoverageReport ConvertTraceToCoverageReport(TraceFile traceFile, Archive archive, Config.ConfigForProcess processConfig, AssemblyExtractor assemblyExtractor)
        {
            ICoverageReport report;
            try
            {
                ILineCoverageSynthesizer synthesizer = lineCoverageSynthesizerFactory.Create(assemblyExtractor, processConfig.PdbDirectory, processConfig.AssemblyPatterns);
                report = new TraceFileParser(traceFile, assemblyExtractor.Assemblies, synthesizer, processConfig.PartialCoverageReport).ParseTraceFile();
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to convert {traceFile} to line coverage. Will retry later", traceFile.FilePath);
                return null;
            }

            if (report.IsEmpty)
            {
                logger.Info("Archiving {trace} because it did not produce any line coverage after conversion", traceFile.FilePath);
                archive.ArchiveFileWithoutLineCoverage(traceFile.FilePath);
                return null;
            }

            return report;
        }

        /// <summary>
        /// Runs a task synchronously. We want to be blocking until uploads finish, because otherwise uploads may not
        /// finish before the daemon terminates or may happen in parallel. This was always our intention, but we kept
        /// forgetting awaits all over the place. Therefore, we explicitly wait for uploads now here and there's no
        /// need to await anything further up the call stack.
        /// </summary>
        static T RunSync<T>(Task<T> task)
        {
            task.Wait();
            return task.Result;
        }
    }
}
