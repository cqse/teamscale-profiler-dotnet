using Newtonsoft.Json;
using System.Collections.Generic;

namespace UploadDaemon
{
    /// <summary>
    /// Data class that holds all details needed to connect to Teamscale.
    /// </summary>
    public class TeamscaleServer
    {
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
        /// Partition within the Teamscale project to which to upload.
        /// </summary>
        public string Partition { get; set; }

        /// <summary>
        /// Template for the commit message for the upload commit.
        /// </summary>
        public string Message { get; set; } = "Test coverage for version %v from %p created at %t";

        public override string ToString()
        {
            return $"Teamscale {Url} project {Project} with user {Username} into partition {Partition}";
        }

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
                yield return @"You must provide a username to connect to Teamscale";
            }
            if (AccessKey == null)
            {
                yield return @"You must provide an access key to connect to Teamscale. Obtain it from the user's profile in Teamscale";
            }
            if (Partition == null)
            {
                yield return @"You must provide a partition into which the coverage will be uploaded";
            }
        }

        /// <summary>
        /// Returns a deep copy of this object.
        /// </summary>
        public TeamscaleServer Clone()
        {
            return (TeamscaleServer)MemberwiseClone();
        }
    }
}