namespace UploadDaemon.Archiving
{
    /// <summary>
    /// A factory for <see cref="IArchive"/>s.
    /// </summary>
    public interface IArchiveFactory
    {
        /// <summary>
        /// Creates an archive based in the given directory.
        /// </summary>
        IArchive CreateArchive(string baseDirectoryPath);
    }
}
