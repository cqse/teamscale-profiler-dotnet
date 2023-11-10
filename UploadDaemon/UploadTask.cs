using Newtonsoft.Json;
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
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;

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
        private readonly ILineCoverageSynthesizer lineCoverageSynthesizer;

        public UploadTask(IFileSystem fileSystem, IUploadFactory uploadFactory, ILineCoverageSynthesizer lineCoverageSynthesizer)
        {
            this.fileSystem = fileSystem;
            this.uploadFactory = uploadFactory;
            this.lineCoverageSynthesizer = lineCoverageSynthesizer;
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
            foreach (TraceFile trace in traces)
            {
                try
                {
                    ProcessTraceFile(trace, archive, config, coverageMerger);
                }
                catch (Exception e)
                {
                    logger.Debug(e, "Failed to process trace file {trace}. Will retry later", trace.FilePath);
                    errorTraceFilePaths.Add(trace.FilePath);
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
            string report = LineCoverageSynthesizer.ConvertToLineCoverageReport(batch.LineCoverage);

            string traceFilePaths = string.Join(", ", batch.TraceFilePaths);

            if (config.ArchiveLineCoverage)
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                archive.ArchiveLineCoverage($"merged_{timestamp}.simple", report);
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

        private void ProcessTraceFile(TraceFile trace, Archive archive, Config config, LineCoverageMerger coverageMerger)
        {
            if (trace.IsEmpty())
            {
                logger.Info("Archiving {trace} because it does not contain any coverage", trace.FilePath);
                archive.ArchiveEmptyFile(trace.FilePath);
                return;
            }

            string processPath = trace.FindProcessPath();
            if (processPath == null)
            {
                logger.Info("Archiving {trace} because it does not contain a Process= line", trace.FilePath);
                archive.ArchiveFileWithoutProcess(trace.FilePath);
                return;
            }

            ParsedTraceFile parsedTraceFile = new ParsedTraceFile(trace.Lines, trace.FilePath);
            Config.ConfigForProcess processConfig = config.CreateConfigForProcess(processPath, parsedTraceFile);

            IUpload upload = uploadFactory.CreateUpload(processConfig, fileSystem);

            if (processConfig.PdbDirectory == null)
            {
                ProcessMethodCoverage(trace, archive, processConfig, upload);
            }
            else
            {
                ProcessLineCoverage(parsedTraceFile, archive, config, processConfig, upload, coverageMerger);
            }
        }

        private void ProcessLineCoverage(ParsedTraceFile parsedTraceFile, Archive archive, Config config, Config.ConfigForProcess processConfig, IUpload upload, LineCoverageMerger coverageMerger)
        {
            logger.Debug("Preparing line coverage from {traceFile} for {upload}", parsedTraceFile.FilePath, upload.Describe());
            Dictionary<string, FileCoverage> lineCoverage = ConvertTraceFileToLineCoverage(parsedTraceFile, archive, processConfig);
            if (lineCoverage == null)
            {
                return;
            }
            if (config.ArchiveLineCoverage)
            {
                archive.ArchiveLineCoverage(Path.GetFileName(parsedTraceFile.FilePath) + ".simple",
                    LineCoverageSynthesizer.ConvertToLineCoverageReport(lineCoverage));
            }
            List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets = ParseRevisionOrUploadTargetFile(parsedTraceFile, processConfig);
            uploadTargets.AddRange(parsedTraceFile.embeddedUploadTargets);
            if (uploadTargets.Any())
            {
                ProcessUploadTargets(uploadTargets, parsedTraceFile, archive, coverageMerger, processConfig, upload, lineCoverage);
            }
            else
            {
                logger.Error("Could not identify any revision target for {traceFile}.", parsedTraceFile.FilePath);
            }
        }
        /// <summary>
        /// Tries to read the revision or upload target file based on the config (absolute path, or relative to loaded assemblies).
        /// </summary>
        private List<(string project, RevisionOrTimestamp revisionOrTimestamp)> ParseRevisionOrUploadTargetFile(ParsedTraceFile parsedTraceFile, Config.ConfigForProcess processConfig)
        {
            string targetFile = processConfig.UploadTargetFile ?? processConfig.RevisionFile;
            if(targetFile == null)
            {
                logger.Warn("No revision or upload target file found.");
                return new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>();
            }
            if (Config.IsAssemblyRelativePath(targetFile))
            {
                foreach ((_, string path) in parsedTraceFile.LoadedAssemblies)
                {
                    string resolvedTargetFile = Config.ResolveAssemblyRelativePath(targetFile, path);
                    if (File.Exists(resolvedTargetFile))
                    {
                        logger.Info("Using  upload target file {uploadTargetFile} while processing {traceFile}.", resolvedTargetFile, parsedTraceFile.FilePath);
                        targetFile = resolvedTargetFile;
                        break;
                    }
                }
            }
            if (processConfig.RevisionFile != null)
            {
                return new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>()
                {
                    ("", ParseRevisionFile(targetFile, parsedTraceFile.FilePath))
                };
            }
            return ParseUploadTargetFile(targetFile, parsedTraceFile.FilePath);
        }

        private void ProcessUploadTargets(List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets, ParsedTraceFile parsedTraceFile, Archive archive, LineCoverageMerger coverageMerger, Config.ConfigForProcess processConfig, IUpload upload, Dictionary<string, FileCoverage> lineCoverage)
        {
            foreach ((string project, RevisionOrTimestamp revisionOrTimestamp) in uploadTargets)
            {
                if (revisionOrTimestamp == null)
                {
                    logger.Error("Invalid revision for {traceFile}.", parsedTraceFile.FilePath);
                    continue;
                }
                if (!String.IsNullOrEmpty(project))
                {
                    upload = new TeamscaleUpload(project, (upload as TeamscaleUpload).Server);
                }
                ProcessForRevisionOrTimestamp(revisionOrTimestamp, parsedTraceFile, archive, coverageMerger, processConfig, upload, lineCoverage);
            }
        }

        private void ProcessForRevisionOrTimestamp(RevisionOrTimestamp revisionOrTimestamp, ParsedTraceFile parsedTraceFile, Archive archive, LineCoverageMerger coverageMerger, Config.ConfigForProcess processConfig, IUpload upload, Dictionary<string, FileCoverage> lineCoverage)
        {
            if (processConfig.MergeLineCoverage)
            {
                logger.Debug("Merging line coverage from {traceFile} into previous line coverage", parsedTraceFile.FilePath);
                coverageMerger.AddLineCoverage(parsedTraceFile.FilePath, revisionOrTimestamp, upload, lineCoverage);
                return;
            }

            logger.Debug("Uploading line coverage from {traceFile} to {upload}", parsedTraceFile.FilePath, upload.Describe());
            string report = LineCoverageSynthesizer.ConvertToLineCoverageReport(lineCoverage);
            if (RunSync(upload.UploadLineCoverageAsync(parsedTraceFile.FilePath, report, revisionOrTimestamp)))
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
        /// Tries to read the upload target file. Logs and returns null if this fails.
        /// </summary>
        private List<(string project, RevisionOrTimestamp revisionOrTimestamp)> ParseUploadTargetFile(string uploadTargetFile, string traceFile)
        {
            try
            {
                string fileContent = File.ReadAllText(uploadTargetFile);
                return JsonConvert.DeserializeObject<List<(string project, RevisionOrTimestamp revisionOrTimestamp)>>(fileContent);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to read the upload target file {uploadTargetFile} while processing {traceFile}. Will retry later",
                    uploadTargetFile, traceFile);
                return null;
            }
        }

        /// <summary>
        /// Tries to read and convert the trace file. Logs and returns null if this fails.
        /// Empty trace files are archived and null is returned as well.
        /// </summary>
        private Dictionary<string, FileCoverage> ConvertTraceFileToLineCoverage(ParsedTraceFile parsedTraceFile, Archive archive, Config.ConfigForProcess processConfig)
        {
            Dictionary<string, FileCoverage> lineCoverage;
            try
            {
                lineCoverage = lineCoverageSynthesizer.ConvertToLineCoverage(parsedTraceFile, processConfig.PdbDirectory, processConfig.AssemblyPatterns);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to convert {traceFile} to line coverage. Will retry later", parsedTraceFile.FilePath);
                return null;
            }

            if (lineCoverage == null)
            {
                logger.Info("Archiving {trace} because it did not produce any line coverage after conversion", parsedTraceFile.FilePath);
                archive.ArchiveFileWithoutLineCoverage(parsedTraceFile.FilePath);
                return null;
            }

            return lineCoverage;
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
