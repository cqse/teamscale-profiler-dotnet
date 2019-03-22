using Common;
using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
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
            Archiver archiver = new Archiver(traceDirectory, fileSystem);

            IEnumerable<TraceFile> traces = scanner.ListTraceFilesReadyForUpload();
            foreach (TraceFile trace in traces)
            {
                try
                {
                    await ProcessTraceFile(trace, archiver, config);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to process trace file {trace}. Will retry later", trace.FilePath);
                }
            }

            logger.Debug("Finished scan");
        }

        private async Task ProcessTraceFile(TraceFile trace, Archiver archiver, Config config)
        {
            if (trace.IsEmpty())
            {
                logger.Info("Archiving {trace} because it does not contain any coverage", trace.FilePath);
                archiver.ArchiveEmptyFile(trace.FilePath);
                return;
            }

            string processPath = trace.FindProcessPath();
            if (processPath == null)
            {
                logger.Info("Archiving {trace} because it does not contain a Process= line", trace.FilePath);
                archiver.ArchiveFileWithoutProcess(trace.FilePath);
                return;
            }

            Config.ConfigForProcess processConfig = config.CreateConfigForProcess(processPath);
            IUpload upload = uploadFactory.CreateUpload(processConfig, fileSystem);

            if (processConfig.PdbDirectory == null)
            {
                await ProcessMethodCoverage(trace, archiver, processConfig, upload);
            }
            else
            {
                ProcessLineCoverage(trace, archiver, processConfig, upload);
            }
        }

        private async void ProcessLineCoverage(TraceFile trace, Archiver archiver, Config.ConfigForProcess processConfig, IUpload upload)
        {
            logger.Debug("Uploading line coverage from {traceFile} to {upload}", trace.FilePath, upload.Describe());
            ParsedTraceFile parsedTraceFile = new ParsedTraceFile(trace.Lines, trace.FilePath);

            RevisionFileUtils.RevisionOrTimestamp timestampOrRevision;
            try
            {
                timestampOrRevision = RevisionFileUtils.Parse(fileSystem.File.ReadAllLines(processConfig.RevisionFile), processConfig.RevisionFile);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to read revision file {revisionFile} while processing {traceFile}. Will retry later",
                    processConfig.RevisionFile, trace.FilePath);
                return;
            }

            string report;
            try
            {
                report = lineCoverageSynthesizer.ConvertToLineCoverageReport(parsedTraceFile, processConfig.PdbDirectory, processConfig.AssemblyPatterns);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to convert {traceFile} to line coverage. Will retry later", trace.FilePath);
                return;
            }

            if (report == null)
            {
                logger.Info("Archiving {trace} because it did not produce any line coverage after conversion", trace.FilePath);
                archiver.ArchiveFileWithoutLineCoverage(trace.FilePath);
                return;
            }

            if (await upload.UploadLineCoverageAsync("trace.txt", report, timestampOrRevision))
            {
                archiver.ArchiveUploadedFile(trace.FilePath);
            }
            else
            {
                logger.Error("Failed to upload line coverage from {traceFile} to {upload}. Will retry later", trace.FilePath, upload.Describe());
            }
        }

        private static async Task ProcessMethodCoverage(TraceFile trace, Archiver archiver, Config.ConfigForProcess processConfig, IUpload upload)
        {
            string version = trace.FindVersion(processConfig.VersionAssembly);
            if (version == null)
            {
                logger.Info("Archiving {trace} because it does not contain the version assembly {versionAssembly}",
                    trace.FilePath, processConfig.VersionAssembly);
                archiver.ArchiveFileWithoutVersionAssembly(trace.FilePath);
                return;
            }

            string prefixedVersion = processConfig.VersionPrefix + version;
            logger.Info("Uploading {trace} to {upload} with version {version}", trace.FilePath, upload.Describe(), prefixedVersion);

            if (await upload.UploadAsync(trace.FilePath, prefixedVersion))
            {
                archiver.ArchiveUploadedFile(trace.FilePath);
            }
            else
            {
                logger.Error("Upload of {trace} to {upload} failed. Will retry later", trace.FilePath, upload.Describe());
            }
        }
    }
}
