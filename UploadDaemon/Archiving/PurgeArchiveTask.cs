using Common;
using System;

namespace UploadDaemon.Archiving
{
    public class PurgeArchiveTask
    {
        private readonly IArchiveFactory archiveFactory;

        public PurgeArchiveTask(IArchiveFactory archiveFactory)
        {
            this.archiveFactory = archiveFactory;
        }

        public void Run(Config config)
        {
            foreach (string traceDirectory in config.TraceDirectoriesToWatch)
            {
                IArchive archive = archiveFactory.CreateArchive(traceDirectory);
                if (config.ArchivePurgingThresholds.UploadedTraces != TimeSpan.Zero)
                {
                    archive.PurgeUploadedFiles(config.ArchivePurgingThresholds.UploadedTraces);
                }
            }
        }
    }
}
