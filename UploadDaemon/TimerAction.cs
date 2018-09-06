using Common;
using NLog;
using System;
using System.IO.Abstractions;
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

        private readonly UploadConfig config;
        private readonly TraceFileScanner scanner;
        private readonly IUpload upload;
        private readonly Archiver archiver;

        public TimerAction(string traceDirectory, UploadConfig config, IUpload upload, IFileSystem fileSystem)
        {
            this.config = config;
            this.scanner = new TraceFileScanner(traceDirectory, config.VersionAssembly, fileSystem);
            this.upload = upload;
            this.archiver = new Archiver(traceDirectory, fileSystem);
        }

        /// <summary>
        /// Event handler for the timer event. Runs the action.
        /// </summary>
        public void HandleTimerEvent(object sender, ElapsedEventArgs arguments)
        {
            Run();
        }

        /// <summary>
        /// Scans the trace directory for traces to process and either tries to upload or archive them.
        /// </summary>
        public async void Run()
        {
            logger.Info("Scanning for coverage files");

            foreach (TraceFileScanner.ScannedFile file in scanner.ListTraceFilesReadyForUpload())
            {
                if (file.Version == null)
                {
                    logger.Info("Archiving {tracePath} because it does not contain the version assembly", file.FilePath);
                    archiver.ArchiveFileWithoutVersionAssembly(file.FilePath);
                }
                else if (file.IsEmpty)
                {
                    logger.Info("Archiving {tracePath} because it does not contain any coverage", file.FilePath);
                    archiver.ArchiveEmptyFile(file.FilePath);
                }
                else
                {
                    logger.Info("Uploading {tracePath}", file.FilePath);
                    bool success = await upload.UploadAsync(file.FilePath, file.Version);
                    if (success)
                    {
                        archiver.ArchiveUploadedFile(file.FilePath);
                    }
                    else
                    {
                        logger.Error("Upload of {tracePath} failed. Will retry later", file.FilePath);
                    }
                }
            }

            logger.Info("Finished scan");
        }
    }
}