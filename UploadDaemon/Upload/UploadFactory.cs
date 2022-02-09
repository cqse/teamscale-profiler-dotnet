using System.IO.Abstractions;
using UploadDaemon.Configuration;

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
            if (config.AzureFileStorage != null)
            {
                return new AzureUpload(config.AzureFileStorage);
            }
            if (config.ArtifactoryServer != null)
            {
                return new ArtifactoryUpload(config.ArtifactoryServer);
            }
            return new FileSystemUpload(config.Directory, fileSystem);
        }
    }
}