using System;

/// <summary>
/// Data class that is deserialized from the JSON configuration file.
/// </summary>
public class Config
{
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

}
