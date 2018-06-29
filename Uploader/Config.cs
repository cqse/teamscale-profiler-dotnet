using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

/// <summary>
/// Data class that is deserialized from the JSON configuration file.
/// </summary>
public class Config
{
    private const string ConfigFileName = "Uploader.json";
    public static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The Teamscale server to upload to.
    /// </summary>
    public TeamscaleServer Teamscale = null;

    /// <summary>
    /// The assembly from which to read the version number.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string VersionAssembly;

    /// <summary>
    /// The directory to upload the traces to.
    /// </summary>
    public string Directory = null;

    /// <summary>
    /// Validates the configuration and returns all collected error messages. An empty list
    /// means the configuration is valid.
    /// </summary>
    /// <returns></returns>
    public List<string> Validate()
    {
        List<string> errorMessages = new List<string>();
        if (Teamscale == null && Directory == null)
        {
            errorMessages.Add(@"You must provide either a Teamscale server (property ""teamscale"") or a directory (property ""directory"") to upload trace files to.");
        }
        return errorMessages;
    }

    /// <summary>
    /// Creates an IUpload based on this configuration.
    /// </summary>
    /// <param name="fileSystem">The file system to use</param>
    public IUpload CreateUpload(IFileSystem fileSystem)
    {
        if (Teamscale != null)
        {
            return new TeamscaleUpload(Teamscale);
        }
        return new FileSystemUpload(Directory, fileSystem);
    }

    /// <summary>
    /// Tries to read the config JSON file.
    /// </summary>
    /// <exception cref="Exception">Throws an exception in case reading or deserializing goes wrong.</exception>
    public static Config ReadConfig(IFileSystem fileSystem)
    {
        string json = fileSystem.File.ReadAllText(ConfigFilePath);
        return JsonConvert.DeserializeObject<Config>(json);
    }
}
