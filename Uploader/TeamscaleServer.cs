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
    public string Url;

    /// <summary>
    /// Username to authenticate with.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string Username;

    /// <summary>
    /// Access token to authenticate with.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string AccessToken;

    /// <summary>
    /// Teamscale project to which to upload.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string Project;

}