using Common;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using UploadDaemon.Upload;

namespace UploadDaemon
{
    /// <summary>
    /// Triggered any time the timer goes off. Performs the scan and upload/archiving of trace files.
    /// </summary>
    public class TimerAction
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Config config;
        private readonly IFileSystem fileSystem;
        private readonly IUploadFactory uploadFactory;

        public TimerAction(Config config, IFileSystem fileSystem, IUploadFactory uploadFactory)
        {
            this.config = config;
            this.fileSystem = fileSystem;
            this.uploadFactory = uploadFactory;
        }

        /// <summary>
        /// Event handler for the timer event. Runs the action.
        /// </summary>
        public void HandleTimerEvent(object sender, ElapsedEventArgs arguments)
        {
            Run();
        }

        /// <summary>
        /// Scans the trace directories for traces to process and either tries to upload or archive them.
        /// </summary>
        public async void Run()
        {
            foreach (string traceDirectory in config.TraceDirectoriesToWatch)
            {
                await ScanDirectory(traceDirectory);
            }
        }

        private async Task ScanDirectory(string traceDirectory)
        {
            logger.Debug("Scanning trace directory {traceDirectory}", traceDirectory);

            TraceFileScanner scanner = new TraceFileScanner(traceDirectory, fileSystem);
            Archiver archiver = new Archiver(traceDirectory, fileSystem);

            IEnumerable<TraceFile> traces = scanner.ListTraceFilesReadyForUpload();
            foreach (TraceFile trace in traces)
            {
                try
                {
                    await ProcessTraceFile(trace, archiver);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to process trace file {trace}. Will retry later", trace.FilePath);
                }
            }

            logger.Debug("Finished scan");
        }

        private async Task ProcessTraceFile(TraceFile trace, Archiver archiver)
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
            string version = trace.FindVersion(processConfig.VersionAssembly);
            if (version == null)
            {
                logger.Info("Archiving {trace} because it does not contain the version assembly {versionAssembly}", trace.FilePath, processConfig.VersionAssembly);
                archiver.ArchiveFileWithoutVersionAssembly(trace.FilePath);
                return;
            }

            IUpload upload = uploadFactory.CreateUpload(processConfig, fileSystem);
            logger.Info("Uploading {trace} to {upload}", trace.FilePath, upload.Describe());

            bool success = await upload.UploadAsync(trace.FilePath, version);
            if (success)
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