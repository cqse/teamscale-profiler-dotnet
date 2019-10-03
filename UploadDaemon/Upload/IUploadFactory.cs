using UploadDaemon;
using System.IO.Abstractions;
using UploadDaemon.Configuration;

namespace UploadDaemon.Upload
{
    /// <summary>
    /// Interface used to allow the tests to use a mock upload implementation.
    /// </summary>
    public interface IUploadFactory
    {
        /// <summary>
        /// Creates the upload for the given configuration. Never returns null.
        /// </summary>
        IUpload CreateUpload(Config.ConfigForProcess config, IFileSystem fileSystem);
    }
}