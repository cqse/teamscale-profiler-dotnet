using System.Collections.Generic;

namespace UploadDaemon.Configuration
{
    /// <summary>
    /// Data class that holds all details needed to connect to Artifactory.
    /// </summary>
    public class Artifactory
    {
        /// <summary>
        /// URL of the Artifactory server. This shall be the entire path down to the directory to which the coverage should be uploaded to, not only the base url of artifactory.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Username to authenticate with.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password to authenticate with.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// API key to authenticate with.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Partition within the Teamscale project to which to upload.
        /// </summary>
        public string Partition { get; set; }

        /// <summary>
        /// Option that specifies intermediate directories which should be appended.
        /// </summary>
        public string PathSuffix { get; set; }

        public override string ToString()
        {
            return $"Artifactory {Url} upload with user {Username}, partition {Partition}";
        }

        /// <summary>
        /// Returns all error messages from a validation of this object.
        /// An empty list means the object is valid.
        /// </summary>
        public IEnumerable<string> Validate()
        {
            if (Url == null)
            {
                yield return @"You must provide a valid URL to connect to Artifactory";
            }
            if ((Username == null || Password == null) && ApiKey == null)
            {
                yield return @"You must provide either a username and password or a api key to connect to Artifactory";
            }
            if (Partition == null)
            {
                yield return @"You must provide a partition into which the coverage will be uploaded";
            }
        }
    }
}
