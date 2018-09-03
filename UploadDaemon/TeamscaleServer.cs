using Newtonsoft.Json;

/// <summary>
/// Data class that holds all details needed to connect to Teamscale.
/// </summary>
public class TeamscaleServer
{
    /// <summary>
    /// URL of the Teamscale server.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string Url { get; set; }

    /// <summary>
    /// Username to authenticate with.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string Username { get; set; }

    /// <summary>
    /// Access token to authenticate with.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string AccessToken { get; set; }

    /// <summary>
    /// Teamscale project to which to upload.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string Project { get; set; }

    /// <summary>
    /// Partition within the Teamscale project to which to upload.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string Partition { get; set; }

    /// <summary>
    /// Template for the commit message for the upload commit.
    /// </summary>
    public string Message = "Test coverage for version %v from %p created at %t";

    public override string ToString()
    {
        return $"Teamscale {Url} project {Project} with user {Username} into partition {Partition}";
    }
}