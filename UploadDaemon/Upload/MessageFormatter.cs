using System;
using System.Globalization;
using UploadDaemon.Configuration;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Upload
{
    /// <summary>
    /// Formats the message for an upload commit.
    /// </summary>
    public class MessageFormatter
    {
        private readonly TeamscaleServer server;

        public MessageFormatter(TeamscaleServer server)
        {
            this.server = server;
        }

        /// <summary>
        /// Formats the configured message template by replacing all placeholders with actual values.
        /// </summary>
        /// <param name="assemblyVersion">The version read from the version assembly</param>
        /// <returns></returns>
        public string Format(string assemblyVersion)
        {
            string formattedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return server.Message.Replace("%v", assemblyVersion).Replace("%p", server.Partition).Replace("%t", formattedTime);
        }

        /// <summary>
        /// Formats the configured message template by replacing all placeholders with actual values.
        /// </summary>
        /// <param name="revision"></param> Can be either a revision or a timestamp.
        /// <returns></returns>
        public string Format(RevisionFileUtils.RevisionOrTimestamp revision)
        {
            string formattedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return server.Message.Replace("version %v", revision.GetType() + " " + revision.Value).Replace("%p", server.Partition).Replace("%t", formattedTime);
        }
    }
}
