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

    public static Config ReadConfig(IFileSystem fileSystem)
    {
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploader.json");

        try
        {
            string json = fileSystem.File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<Config>(json);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to read config file {configPath}", configPath);
            Environment.Exit(1);
            return null;
        }
    }

}
