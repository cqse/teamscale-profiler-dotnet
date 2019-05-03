using System.IO.Abstractions;

namespace UploadDaemon.Archiving
{
    public interface IArchiveFactory
    {
        IArchive CreateArchive(string baseDirectoryPath);
    }

    public class ArchiveFactory : IArchiveFactory
    {
        private readonly IFileSystem fileSystem;
        private readonly IDateTimeProvider dateTimeProvider;

        public ArchiveFactory(IFileSystem fileSystem, IDateTimeProvider dateTimeProvider)
        {
            this.fileSystem = fileSystem;
            this.dateTimeProvider = dateTimeProvider;
        }

        public IArchive CreateArchive(string baseDirectoryPath)
        {
            return new Archive(baseDirectoryPath, fileSystem, dateTimeProvider);
        }
    }
}
