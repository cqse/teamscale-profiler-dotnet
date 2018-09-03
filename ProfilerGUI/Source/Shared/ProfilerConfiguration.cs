﻿using Newtonsoft.Json;
using ProfilerGUI.Source.Configurator;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProfilerGUI.Source.Shared
{
    /// <summary> Collects all options needed to run the profiler. </summary>
    public class ProfilerConfiguration
    {
        /// <summary>
        /// The name of the config file.
        /// </summary>
        private const string ConfigFile = "ProfilerGUI.json";

        /// <summary>
        /// Folder to which the profiler should write trace files.
        /// </summary>
        public string TraceTargetFolder { get; set; }

        /// <summary>
        /// The profiled application's executable.
        /// </summary>
        public string TargetApplicationPath { get; set; }

        /// <summary>
        /// The command line arguments to the profiled application.
        /// </summary>
        public string TargetApplicationArguments { get; set; }

        /// <summary>
        /// The working directory of the target application.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The bitness (32bit vs 64bit) of the target application.
        /// </summary>
        public EApplicationType ApplicationType { get; set; }

        /// <summary>
        /// Tries to read the config from the given JSON file.
        /// </summary>
        /// <exception cref="JsonException">If JSON deserialization fails</exception>
        /// <exception cref="IOException">If reading the file fails</exception>
        public static ProfilerConfiguration ReadFromFile(string filePath)
        {
            // TODO (FS) should determine assembly dir and read from fixed config. write as well
            string fileContent = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<ProfilerConfiguration>(fileContent);
        }

        /// <summary>
        /// Saves the config to the given file as JSON.
        /// </summary>
        /// <exception cref="JsonException">If JSON serialization fails</exception>
        /// <exception cref="IOException">If writing the file fails</exception>
        public void WriteToFile(string filePath)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
    }
}