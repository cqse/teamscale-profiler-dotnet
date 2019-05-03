using Common;
using System;

namespace UploadDaemon.Archiving
{
    /// <summary>
    /// Task that purges old trace files from the archives.
    /// </summary>
    public class PurgeArchiveTask
    {
        private readonly IArchiveFactory archiveFactory;

        public PurgeArchiveTask(IArchiveFactory archiveFactory)
        {
            this.archiveFactory = archiveFactory;
        }

        /// <summary>
        /// Runs this task, purging the archives.
        /// </summary>
        public void Run(Config config)
        {
            foreach (string traceDirectory in config.TraceDirectoriesToWatch)
            {
                IArchive archive = archiveFactory.CreateArchive(traceDirectory);
                PurgeUploadedFiles(config, archive);
                PurgeEmptyFiles(config, archive);
                PurgeIncompleteFiles(config, archive);
            }
        }

        private static void PurgeIncompleteFiles(Config config, IArchive archive)
        {
            TimeSpan threshold = config.ArchivePurgingThresholds.IncompleteTraces;
            if (threshold >= TimeSpan.Zero)
            {
                archive.PurgeFilesWithoutProcess(threshold);
                archive.PurgeFilesWithoutVersionAssembly(threshold);
            }
        }

        private static void PurgeEmptyFiles(Config config, IArchive archive)
        {
            TimeSpan threshold = config.ArchivePurgingThresholds.EmptyTraces;
            if (threshold >= TimeSpan.Zero)
            {
                archive.PurgeEmptyFiles(threshold);
                archive.PurgeFilesWithoutLineCoverage(threshold);
            }
        }

        private static void PurgeUploadedFiles(Config config, IArchive archive)
        {
            TimeSpan threshold = config.ArchivePurgingThresholds.UploadedTraces;
            if (threshold >= TimeSpan.Zero)
            {
                archive.PurgeUploadedFiles(threshold);
            }
        }
    }
}
