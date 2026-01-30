using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using UploadDaemon.Archiving;
using UploadDaemon.Configuration;
using UploadDaemon.Scanning;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Upload;
using UploadDaemon.Report;
using UploadDaemon.Report.Testwise;
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;
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

        public UploadTask(IFileSystem fileSystem, IUploadFactory uploadFactory)
        {
            this.fileSystem = fileSystem;
            this.uploadFactory = uploadFactory;
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

            IEnumerable<TraceFile> traces = scanner.ListTraceFilesReadyForUpload();
            List<string> errorTraceFilePaths = new List<string>();
            foreach (TraceFile traceFile in traces)
            {
                try
                {
                    ProcessTraceFile(traceFile, archive, config, coverageMerger);
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

            UploadMergedCoverage(archive, coverageMerger, config);

            logger.Debug("Finished scan");
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

            if (processConfig.PdbDirectory == null)
            {
                ProcessMethodCoverage(traceFile, archive, processConfig, upload);
            }
            else
            {
                ProcessLineCoverage(traceFile, assemblyExtractor, archive, config, processConfig, upload, coverageMerger);
            }
        }

        private void ProcessLineCoverage(TraceFile traceFile, AssemblyExtractor assemblyExtractor, Archive archive, Config config, Config.ConfigForProcess processConfig, IUpload upload, LineCoverageMerger coverageMerger)
        {
            logger.Debug("Preparing line coverage from {traceFile} for {upload}", traceFile.FilePath, upload.Describe());
            ICoverageReport coverageReport = ConvertTraceToCoverageReport(traceFile, archive, processConfig, assemblyExtractor);
            if (coverageReport == null)
            {
                return;
            }
            if (config.ArchiveLineCoverage)
            {
                archive.ArchiveCoverageReport(Path.GetFileName(traceFile.FilePath), coverageReport);
            }

            // TODO As we pass in the processConfig to the ConvertTraceToCoverageReport, could we just set these there instead of reinstantiating the whole thing?
            if (coverageReport is TestwiseCoverageReport testwiseCoverageReport)
            {
                if (processConfig.PartialCoverageReport)    
                {
                    coverageReport = new TestwiseCoverageReport(true, testwiseCoverageReport.Tests);
                }
            }

            List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets = ParseRevisionFile(traceFile.FilePath, processConfig, assemblyExtractor);
            uploadTargets.AddRange(assemblyExtractor.EmbeddedUploadTargets);


            
            if (uploadTargets.Any())
            {
                ProcessUploadTargets(uploadTargets, traceFile, archive, coverageMerger, processConfig, upload, coverageReport);
            }
            else
            {
                logger.Error("Could not identify any revision target for {traceFile}.", traceFile.FilePath);
            }
        }

        /// <summary>
        /// Tries to read the revision or upload target file based on the config (absolute path, or relative to loaded assemblies).
        /// </summary>
        private List<(string project, RevisionOrTimestamp revisionOrTimestamp)> ParseRevisionFile(string traceFilePath, Config.ConfigForProcess processConfig, AssemblyExtractor assemblyExtractor)
        {
            string revisionFile = processConfig.RevisionFile;
            if (revisionFile == null)
            {
                logger.Info("No revision file found.");
                return new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>();
            }
            if (Config.IsAssemblyRelativePath(revisionFile))
            {
                foreach (KeyValuePair<uint, (string, string)> entry in assemblyExtractor.Assemblies)
                {
                    string resolvedRevisionFile = Config.ResolveAssemblyRelativePath(revisionFile, entry.Value.Item2);
                    if (File.Exists(resolvedRevisionFile))
                    {
                        logger.Info("Using revision file {revisionFile} while processing {traceFile}.", resolvedRevisionFile, traceFilePath);
                        revisionFile = resolvedRevisionFile;
                        break;
                    }
                }
            }
            return new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>()
                {
                    ("", ParseRevisionFile(revisionFile, traceFilePath))
                };
        }

        private void ProcessUploadTargets(List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets, TraceFile traceFile, Archive archive, LineCoverageMerger coverageMerger, Config.ConfigForProcess processConfig, IUpload upload, ICoverageReport coverageReport)
        {
            foreach ((string project, RevisionOrTimestamp revisionOrTimestamp) in uploadTargets)
            {
                if (revisionOrTimestamp == null)
                {
                    logger.Error("Invalid revision for {traceFile}.", traceFile.FilePath);
                    continue;
                }
                if (!String.IsNullOrEmpty(project))
                {
                    upload = (upload as TeamscaleUpload).CopyWithNewProject(project);
                }
                ProcessForRevisionOrTimestamp(revisionOrTimestamp, traceFile, archive, coverageMerger, processConfig, upload, coverageReport);
            }
        }

        private void ProcessForRevisionOrTimestamp(RevisionOrTimestamp revisionOrTimestamp, TraceFile parsedTraceFile, Archive archive, LineCoverageMerger coverageMerger, Config.ConfigForProcess processConfig, IUpload upload, ICoverageReport coverageReport)
        {
            if (processConfig.MergeLineCoverage)
            {
                logger.Debug("Merging line coverage from {traceFile} into previous line coverage", parsedTraceFile.FilePath);
                coverageMerger.AddLineCoverage(parsedTraceFile.FilePath, revisionOrTimestamp, upload, coverageReport);
                return;
            }

            logger.Debug("Uploading line coverage from {traceFile} to {upload}", parsedTraceFile.FilePath, upload.Describe());
            if (RunSync(upload.UploadLineCoverageAsync(parsedTraceFile.FilePath, coverageReport, revisionOrTimestamp)))
            {
                archive.ArchiveUploadedFile(parsedTraceFile.FilePath);
            }
            else
            {
                logger.Error("Failed to upload line coverage from {traceFile} to {upload}. Will retry later", parsedTraceFile.FilePath, upload.Describe());
            }
        }

        /// <summary>
        /// Tries to read the revision file. Logs and returns null if this fails.
        /// </summary>
        private RevisionOrTimestamp ParseRevisionFile(string revisionFile, string traceFile)
        {
            try
            {
                return Parse(fileSystem.File.ReadAllLines(revisionFile), revisionFile);
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
                LineCoverageSynthesizer lineCoverageSynthesizer = new LineCoverageSynthesizer(assemblyExtractor, processConfig.PdbDirectory, processConfig.AssemblyPatterns);
                report = new TraceFileParser(traceFile, assemblyExtractor.Assemblies, lineCoverageSynthesizer).ParseTraceFile();
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

        private static void ProcessMethodCoverage(TraceFile trace, Archive archive, Config.ConfigForProcess processConfig, IUpload upload)
        {
            string version = trace.FindVersion(processConfig.VersionAssembly);
            if (version == null)
            {
                logger.Info("Archiving {trace} because it does not contain the version assembly {versionAssembly}",
                    trace.FilePath, processConfig.VersionAssembly);
                archive.ArchiveFileWithoutVersionAssembly(trace.FilePath);
                return;
            }

            string prefixedVersion = processConfig.VersionPrefix + version;
            logger.Info("Uploading {trace} to {upload} with version {version}", trace.FilePath, upload.Describe(), prefixedVersion);

            if (RunSync(upload.UploadAsync(trace.FilePath, prefixedVersion)))
            {
                archive.ArchiveUploadedFile(trace.FilePath);
            }
            else
            {
                logger.Error("Upload of {trace} to {upload} failed. Will retry later", trace.FilePath, upload.Describe());
            }
        }

        /// <summary>
        /// Runs a task synchronously. We want to be blocking until uploads finish, because otherwise uploads may not
        /// finish before the daemon terminates or may happen in parallel. This was always our intention, but we kept
        /// forgetting awaits all over the place. Therefore, we explicitly wait for uploads now here and there's no
        /// need to await anything further up the call stack.
        /// </summary>
        private static T RunSync<T>(Task<T> task)
        {
            task.Wait();
            return task.Result;
        }
    }
}
