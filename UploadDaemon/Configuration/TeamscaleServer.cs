using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UploadDaemon.Configuration
{
    /// <summary>
    /// Data class that holds all details needed to connect to Teamscale.
    /// </summary>
    public class TeamscaleServer
    {
        private string url;

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
        /// Additional Teamscale projects to upload to
        /// </summary>
        public List<string> AdditionalProjects { get; set; } = new List<string>();

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

        public TeamscaleServer(string targetProject, TeamscaleServer previous, Logger logger)
        {
            url = previous.Url;
            AdditionalProjects = previous.AdditionalProjects;
            if (targetProject == null)
            {
                targetProject = GetNextTargetProject();
                if (targetProject == null)
                {
                    logger.Warn("No more additional projects as upload targets found. Will use last found project as upload target. This is a configuration error, please check that there is an equal number of projects and revisions declared.");
                    targetProject = previous.Project;
                }
            }
            Project = targetProject;
            Partition = previous.Partition;
            Username = previous.Username;
            AccessKey = previous.AccessKey;
            Message = previous.Message;
        }
        /// <summary>
        /// Needed for automatic creation via yaml file.
        /// </summary>
        public TeamscaleServer() {}
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
        private string GetNextTargetProject()
        {
            try
            {
                string nextProject = AdditionalProjects.First();
                AdditionalProjects.Remove(nextProject);
                return nextProject;
            }
    
            catch(InvalidOperationException e){
                return null;
            }
        }
    }
}
