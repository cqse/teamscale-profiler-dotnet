using NLog;
using System;
using System.IO.Abstractions;
using System.Timers;

/// <summary>
/// Triggered any time the timer goes off. Performs the scan and upload/archiving of trace files.
/// </summary>
class TimerAction
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private readonly Config config;
    private readonly TraceFileScanner scanner;
    private readonly IUpload upload;
    private readonly Archiver archiver;
    private readonly MessageFormatter messageFormatter;

    public TimerAction(string traceDirectory, Config config, IFileSystem fileSystem)
    {
        this.config = config;
        this.scanner = new TraceFileScanner(traceDirectory, config.VersionAssembly, fileSystem);
        this.upload = new TeamscaleUpload(config.Teamscale);
        this.archiver = new Archiver(traceDirectory, fileSystem);
        this.messageFormatter = new MessageFormatter(config);
    }

    public async void Run(object sender, ElapsedEventArgs arguments)
    {
        foreach (TraceFileScanner.ScannedFile file in scanner.ListTraceFilesReadyForUpload())
        {
            if (file.Version == null)
            {
                archiver.ArchiveFileWithoutVersionAssembly(file.FilePath);
            }
            else
            {
                bool success = await upload.UploadAsync(file.FilePath, file.Version, messageFormatter.Format(file.Version), config.Partition);
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
    }

}
