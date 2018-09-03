using Newtonsoft.Json;
using ProfilerGUI.Source.Configurator;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProfilerGUI.Source.Shared
{
    /// <summary> Collects all options needed to run the profiler. </summary>
    public class ProfilerConfiguration
    {
        public string TraceTargetFolder { get; set; }

        public string TraceFolderToCopyTo { get; set; }

        public string TargetApplicationPath { get; set; }

        public string TargetApplicationArguments { get; set; }

        public List<string> ExecutablesToWaitFor { get; set; } = new List<string>();

        public string WorkingDirectory { get; set; }

        public bool QuitToolAfterProcessesEnded { get; set; } = true;

        public EApplicationType ApplicationType { get; set; }

        public static ProfilerConfiguration ReadFromFile(string filePath)
        {
            // TODO (FS) below you explicitly pass the encoding. do we need to do the same here?
            string fileContent = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<ProfilerConfiguration>(fileContent);
        }

        public void WriteToFile(string filePath)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
    }
}
