using System.IO.Abstractions;
using Common;

namespace UploadDaemon.Upload
{
    /// <summary>
    /// Creates IUpload objects based on a configuration.
    /// </summary>
    public class UploadFactory : IUploadFactory
    {
        public IUpload CreateUpload(Config.ConfigForProcess config, IFileSystem fileSystem)
        {
            if (config.Teamscale != null)
            {
                return new TeamscaleUpload(config.Teamscale);
            }
            if (config.FileUpload != null)
            {
                return new UploadServiceUpload(config.FileUpload);
            }
            if (config.AzureFileStorage != null)
            {
                return new AzureUpload(config.AzureFileStorage);
            }
            return new FileSystemUpload(config.Directory, fileSystem);
        }
    }
}