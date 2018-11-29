using Newtonsoft.Json;

/// <summary>
/// Data class that holds all details necessary to connect to an Azure File Storage.
/// </summary>
public class AzureFileStorage
{
	/// <summary>
	/// Connection string for a file storage. For details on how to create connection strings,
	/// refer to https://docs.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string.
	/// </summary>
	[JsonProperty(Required = Required.Always)]
	public string ConnectionString { get; set; }

	/// <summary>
	/// Name of the file-storage share to write to.
	/// </summary>
	[JsonProperty(Required = Required.Always)]
	public string ShareName { get; set; }

	/// <summary>
	/// Directory within the storage share to write into.
	/// </summary>
	[JsonProperty(Required = Required.Default)]
	public string Directory { get; set; }
}