using Common;
using NLog;
using System;

namespace UploadDaemon.Archiving
{
    /// <summary>
    /// Task that purges old trace files from the archives.
    /// </summary>
    public class PurgeArchiveTask
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

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
            logger.Debug("Purging archives");
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
            TimeSpan? threshold = config.ArchivePurgingThresholds.IncompleteTraces;
            if (threshold != null)
            {
                archive.PurgeFilesWithoutProcess((TimeSpan)threshold);
                archive.PurgeFilesWithoutVersionAssembly((TimeSpan)threshold);
            }
        }

        private static void PurgeEmptyFiles(Config config, IArchive archive)
        {
            TimeSpan? threshold = config.ArchivePurgingThresholds.EmptyTraces;
            if (threshold != null)
            {
                archive.PurgeEmptyFiles((TimeSpan)threshold);
                archive.PurgeFilesWithoutLineCoverage((TimeSpan)threshold);
            }
        }

        private static void PurgeUploadedFiles(Config config, IArchive archive)
        {
            TimeSpan? threshold = config.ArchivePurgingThresholds.UploadedTraces;
            if (threshold != null)
            {
                archive.PurgeUploadedFiles((TimeSpan)threshold);
            }
        }
    }
}
