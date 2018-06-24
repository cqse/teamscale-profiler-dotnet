using System;
using System.Timers;

/// <summary>
/// Triggered any time the timer goes off. Performs the scan and upload/archiving of trace files.
/// </summary>
class TimerAction
{

    private readonly Config config;
    private readonly TraceFileScanner scanner;
    private readonly IUpload upload;

    public TimerAction(string traceDirectory, Config config)
    {
        this.config = config;
        this.scanner = new TraceFileScanner(traceDirectory, config.VersionAssembly);
        this.upload = new TeamscaleUpload(config.Teamscale);
    }

    public void Run(object sender, ElapsedEventArgs arguments)
    {
        foreach (TraceFileScanner.ScannedFile file in scanner.ListTraceFilesReadyForUpload())
        {
            if (file.Version == null)
            {
                Archive(file);
            }
            else
            {
                upload.UploadAsync(file.FilePath, file.Version, "TODO", config.Partition);
            }
        }
    }


    private void Archive(TraceFileScanner.ScannedFile file)
    {
        throw new NotImplementedException();
    }

}
