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
    public const string CONFIG_FILE_NAME = "Uploader.json";
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The Teamscale server to upload to.
    /// </summary>
    public TeamscaleServer Teamscale;

    /// <summary>
    /// The assembly from which to read the version number.
    /// </summary>
    public string VersionAssembly;

    /// <summary>
    /// Partition within the Teamscale project to which to upload.
    /// </summary>
    public string Partition;

    /// <summary>
    /// Tries to read the config JSON file.
    /// </summary>
    /// <exception cref="Exception">Throws an exception in case reading or deserializing goes wrong.</exception>
    public static Config ReadConfig(IFileSystem fileSystem)
    {
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);
        string json = fileSystem.File.ReadAllText(configPath);
        return JsonConvert.DeserializeObject<Config>(json);
    }

}
