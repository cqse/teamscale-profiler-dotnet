/// <summary>
/// Data class that holds all details needed to connect to Teamscale.
/// </summary>
class TeamscaleServer
{
    /// <summary>
    /// URL of the Teamscale server.
    /// </summary>
    public string Url;

    /// <summary>
    /// Username to authenticate with.
    /// </summary>
    public string Username;

    /// <summary>
    /// Access token to authenticate with.
    /// </summary>
    public string AccessToken;

    /// <summary>
    /// Teamscale project to which to upload.
    /// </summary>
    public string Project;
}