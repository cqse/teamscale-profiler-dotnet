using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// Utils file to handle target file interactions: Upload target files consist of a list of tuples that can be either 
    ///{project: myproject, revision: mycoolrevision}
    /// or
    /// {project: myproject, timestamp: 12345}
    /// </summary>
    public class UploadTargetFileUtils
    {

        /// <summary>
        /// Serializes the provided list of upload targets to JSON and writes it into a json file provided by the file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="uploadTargets"></param>
        public static void SerializeToFile(string filePath, List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets)
        {
            List<UploadTargetFileEntry> UploadTargetFileEnties = new List<UploadTargetFileEntry>();

            foreach ((string project, RevisionOrTimestamp RevisionOrTimestamp) in uploadTargets)
            {
                if (RevisionOrTimestamp.IsRevision)
                {
                    UploadTargetFileEnties.Add(new UploadTargetFileEntry { Project = project, Revision = RevisionOrTimestamp.Value });
                } else
                {
                    UploadTargetFileEnties.Add(new UploadTargetFileEntry { Project = project, Timestamp = RevisionOrTimestamp.Value });
                }
            }
            string jsonContent = JsonConvert.SerializeObject(UploadTargetFileEnties, Formatting.Indented);
            System.IO.File.WriteAllText(filePath, jsonContent);
        }

        /// <summary>
        /// Parses a provided .JSON file and creates a list of upload targets.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<(string project, RevisionOrTimestamp revisionOrTimestamp)> Parse(string filePath)
        {
            string jsonContent = System.IO.File.ReadAllText(filePath);

            List<UploadTargetFileEntry> uploadTargetFileEntries = JsonConvert.DeserializeObject<List<UploadTargetFileEntry>>(jsonContent);

            List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets = new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>();

            foreach (UploadTargetFileEntry uploadTargetFileEntry in uploadTargetFileEntries)
            {
                string project = uploadTargetFileEntry.Project;
                string revision = uploadTargetFileEntry.Revision ?? uploadTargetFileEntry.Timestamp; // Use Revision if available, otherwise use Timestamp

                uploadTargets.Add((project, new RevisionOrTimestamp(revision, uploadTargetFileEntry.Revision != null)));
            }

            return uploadTargets;
        }
    }
   class UploadTargetFileEntry
    {
        public string Project { get; set; }
        public string Revision { get; set; }
        public string Timestamp { get; set; }
    }
}
