using Common;
using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using UploadDaemon.Archiving;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Upload;

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
        public async void Run(Config config)
        {
            foreach (string traceDirectory in config.TraceDirectoriesToWatch)
            {
                await ScanDirectory(traceDirectory, config);
            }
        }

        private async Task ScanDirectory(string traceDirectory, Config config)
        {
            logger.Debug("Scanning trace directory {traceDirectory}", traceDirectory);

            TraceFileScanner scanner = new TraceFileScanner(traceDirectory, fileSystem);
            Archive archive = new Archive(traceDirectory, fileSystem, new DefaultDateTimeProvider());
            LineCoverageMerger coverageMerger = new LineCoverageMerger();

            IEnumerable<TraceFile> traces = scanner.ListTraceFilesReadyForUpload();
            foreach (TraceFile trace in traces)
            {
                try
                {
                    await ProcessTraceFile(trace, archive, config, coverageMerger);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to process trace file {trace}. Will retry later", trace.FilePath);
                }
            }

            await UploadMergedCoverage(archive, coverageMerger);

            logger.Debug("Finished scan");
        }

        private static async Task UploadMergedCoverage(Archive archive, LineCoverageMerger coverageMerger)
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
                logger.Debug("Uploading merged line coverage from {traceFile} to {upload}",
                    string.Join(", ", batch.TraceFilePaths), batch.Upload.Describe());
                string report = LineCoverageSynthesizer.ConvertToLineCoverageReport(batch.LineCoverage);

                string traceFilePaths = string.Join(", ", batch.TraceFilePaths);
                if (await batch.Upload.UploadLineCoverageAsync(traceFilePaths, report, batch.RevisionOrTimestamp))
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
        }

        private async Task ProcessTraceFile(TraceFile trace, Archive archive, Config config, LineCoverageMerger coverageMerger)
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

            Config.ConfigForProcess processConfig = config.CreateConfigForProcess(processPath);
            IUpload upload = uploadFactory.CreateUpload(processConfig, fileSystem);

            if (processConfig.PdbDirectory == null)
            {
                await ProcessMethodCoverage(trace, archive, processConfig, upload);
            }
            else
            {
                await ProcessLineCoverage(trace, archive, processConfig, upload, coverageMerger);
            }
        }

        private async Task ProcessLineCoverage(TraceFile trace, Archive archive, Config.ConfigForProcess processConfig, IUpload upload, LineCoverageMerger coverageMerger)
        {
            logger.Debug("Uploading line coverage from {traceFile} to {upload}", trace.FilePath, upload.Describe());
            RevisionFileUtils.RevisionOrTimestamp timestampOrRevision = ParseRevisionFile(trace, processConfig);
            if (timestampOrRevision == null)
            {
                return;
            }

            Dictionary<string, FileCoverage> lineCoverage = ConvertTraceFileToLineCoverage(trace, archive, processConfig);
            if (lineCoverage == null)
            {
                return;
            }

            if (processConfig.MergeLineCoverage)
            {
                logger.Debug("Merging line coverage from {traceFile} into previous line coverage", trace.FilePath);
                coverageMerger.AddLineCoverage(trace.FilePath, timestampOrRevision, upload, lineCoverage);
                return;
            }

            logger.Debug("Uploading line coverage from {traceFile} to {upload}", trace.FilePath, upload.Describe());
            string report = LineCoverageSynthesizer.ConvertToLineCoverageReport(lineCoverage);
            if (await upload.UploadLineCoverageAsync(trace.FilePath, report, timestampOrRevision))
            {
                archive.ArchiveUploadedFile(trace.FilePath);
            }
            else
            {
                logger.Error("Failed to upload line coverage from {traceFile} to {upload}. Will retry later", trace.FilePath, upload.Describe());
            }
        }

        /// <summary>
        /// Tries to read the revision file based on the config. Logs and returns null if this fails.
        /// </summary>
        private RevisionFileUtils.RevisionOrTimestamp ParseRevisionFile(TraceFile trace, Config.ConfigForProcess processConfig)
        {
            try
            {
                return RevisionFileUtils.Parse(fileSystem.File.ReadAllLines(processConfig.RevisionFile), processConfig.RevisionFile);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to read revision file {revisionFile} while processing {traceFile}. Will retry later",
                    processConfig.RevisionFile, trace.FilePath);
                return null;
            }
        }

        /// <summary>
        /// Tries to read and convert the trace file. Logs and returns null if this fails.
        /// Empty trace files are archived and null is returned as well.
        /// </summary>
        private Dictionary<string, FileCoverage> ConvertTraceFileToLineCoverage(TraceFile trace, Archive archive, Config.ConfigForProcess processConfig)
        {
            ParsedTraceFile parsedTraceFile = new ParsedTraceFile(trace.Lines, trace.FilePath);
            Dictionary<string, FileCoverage> lineCoverage;
            try
            {
                lineCoverage = lineCoverageSynthesizer.ConvertToLineCoverage(parsedTraceFile, processConfig.PdbDirectory, processConfig.AssemblyPatterns);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to convert {traceFile} to line coverage. Will retry later", trace.FilePath);
                return null;
            }

            if (lineCoverage == null)
            {
                logger.Info("Archiving {trace} because it did not produce any line coverage after conversion", trace.FilePath);
                archive.ArchiveFileWithoutLineCoverage(trace.FilePath);
                return null;
            }

            return lineCoverage;
        }

        private static async Task ProcessMethodCoverage(TraceFile trace, Archive archive, Config.ConfigForProcess processConfig, IUpload upload)
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

            if (await upload.UploadAsync(trace.FilePath, prefixedVersion))
            {
                archive.ArchiveUploadedFile(trace.FilePath);
            }
            else
            {
                logger.Error("Upload of {trace} to {upload} failed. Will retry later", trace.FilePath, upload.Describe());
            }
        }
    }
}
