using System;
using System.Collections.Generic;

namespace UploadDaemon.Configuration
{
    /// <summary>
    /// Data class that holds all details needed to connect to Teamscale.
    /// </summary>
    public class TeamscaleServer
    {
        /// <summary>
        /// Environment variable that may be used to provide the username instead of the YAML config.
        /// </summary>
        public const string USERNAME_ENVIRONMENT_VARIABLE = "TEAMSCALE_USERNAME";

        /// <summary>
        /// Environment variable that may be used to provide the access key instead of the YAML config.
        /// </summary>
        public const string ACCESS_KEY_ENVIRONMENT_VARIABLE = "TEAMSCALE_ACCESS_KEY";

        private string url;
        private string username;
        private string accessKey;

        /// <summary>
        /// URL of the Teamscale server.
        /// </summary>
        public string Url
        {
            get { return url; }
            set { url = value.Trim('/'); }
        }

        /// <summary>
        /// Teamscale project to which to upload.
        /// </summary>
        public string Project { get; set; }

        /// <summary>
        /// Username to authenticate with. The environment variable TEAMSCALE_USERNAME takes
        /// precedence over the value configured in the YAML config.
        /// </summary>
        public string Username
        {
            get { return ReadEnvironmentVariable(USERNAME_ENVIRONMENT_VARIABLE) ?? username; }
            set { username = value; }
        }

        /// <summary>
        /// Access key to authenticate with. The environment variable TEAMSCALE_ACCESS_KEY takes
        /// precedence over the value configured in the YAML config.
        /// </summary>
        public string AccessKey
        {
            get { return ReadEnvironmentVariable(ACCESS_KEY_ENVIRONMENT_VARIABLE) ?? accessKey; }
            set { accessKey = value; }
        }

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
            return $"Teamscale {Url} project {Project} with user {Username}, partition {Partition}";
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
                yield return $@"You must provide a username to connect to Teamscale, either in the config file or via the environment variable {USERNAME_ENVIRONMENT_VARIABLE}";
            }
            if (AccessKey == null)
            {
                yield return $@"You must provide an access key to connect to Teamscale, either in the config file or via the environment variable {ACCESS_KEY_ENVIRONMENT_VARIABLE}. Obtain it from Access Keys in Teamscale";
            }
            if (Partition == null)
            {
                yield return @"You must provide a partition into which the coverage will be uploaded";
            }
        }

        /// <summary>
        /// Returns the value of the given environment variable or null if it is not set or blank.
        /// </summary>
        private static string ReadEnvironmentVariable(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            return value;
        }
    }
}
