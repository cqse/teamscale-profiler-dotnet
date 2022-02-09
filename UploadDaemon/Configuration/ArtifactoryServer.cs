using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadDaemon.Configuration
{
    public class ArtifactoryServer
    {
        /// <summary>
        /// Header that can be used as an alternative to basic authentication to authenticate requests against artifactory.
        /// </summary>
        public readonly string ARTIFACTORY_API_HEADER = "X-JFrog-Art-Api";

        /// <summary>
        /// URL of the Teamscale server.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Teamscale project to which to upload.
        /// </summary>
        public string Project { get; set; }

        /// <summary>
        /// Username to authenticate with.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Access key to authenticate with.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Password to authenticate with.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Partition within the Teamscale project to which to upload.
        /// </summary>
        public string ZipPath { get; set; }

        /// <summary>
        /// Returns all error messages from a validation of this object.
        /// An empty list means the object is valid.
        /// </summary>
        public IEnumerable<string> Validate()
        {
            if (Url == null)
            {
                yield return @"You must provide a valid URL to connect to Teamscale";
            }
            if (Project == null)
            {
                yield return @"You must provide a project into which the coverage will be uploaded";
            }
            if (Username == null)
            {
                yield return @"You must provide a username to connect to Artifactory";
            }
            if (AccessKey == null && Password == null)
            {
                yield return @"You must provide an access key or a password to connect to Artifactory";
            }
            if (ZipPath == null)
            {
                yield return @"You must provide a Zip Path into which the coverage will be uploaded";
            }
        }
    }
}
