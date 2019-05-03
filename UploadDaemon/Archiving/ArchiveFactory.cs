using System.IO.Abstractions;

namespace UploadDaemon.Archiving
{
    /// <summary>
    /// A factory creating <see cref="Archive"/>s.
    /// </summary>
    public class ArchiveFactory : IArchiveFactory
    {
        private readonly IFileSystem fileSystem;
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>
        /// Constructor
        /// </summary>
        public ArchiveFactory(IFileSystem fileSystem, IDateTimeProvider dateTimeProvider)
        {
            this.fileSystem = fileSystem;
            this.dateTimeProvider = dateTimeProvider;
        }

        /// <summary>
        /// Creates an archive based in the given directory.
        /// </summary>
        public IArchive CreateArchive(string baseDirectoryPath)
        {
            return new Archive(baseDirectoryPath, fileSystem, dateTimeProvider);
        }
    }
}
