using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.IO.Abstractions;

/// <summary>
/// Data class that is deserialized from the JSON configuration file.
/// </summary>
public class Config
{
    private const string CONFIG_FILE_NAME = "Uploader.json";
    public static readonly string CONFIG_FILE_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The Teamscale server to upload to.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public TeamscaleServer Teamscale;

    /// <summary>
    /// The assembly from which to read the version number.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string VersionAssembly;

    /// <summary>
    /// Partition within the Teamscale project to which to upload.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string Partition;

    /// <summary>
    /// Tries to read the config JSON file.
    /// </summary>
    /// <exception cref="Exception">Throws an exception in case reading or deserializing goes wrong.</exception>
    public static Config ReadConfig(IFileSystem fileSystem)
    {
        string json = fileSystem.File.ReadAllText(CONFIG_FILE_PATH);
        return JsonConvert.DeserializeObject<Config>(json);
    }
}
