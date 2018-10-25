using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using NLog;
using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Uploads trace files to an Azure File Storage.
/// </summary>
public class AzureUpload : IUpload
{
	private static readonly Logger logger = LogManager.GetCurrentClassLogger();

	private readonly string storageConnectionString;
	private readonly string shareName;
	private readonly string directoryPath;

	/// <param name="storageConnectionString">An Azure File Storage connection string. For details on how to craete
	/// connection strings, please refer to https://docs.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string.</param>
	/// <param name="shareName">The name of a file share on the Azure connection</param>
	/// <param name="directoryPath">A directory path below the file share to store coverage data in</param>
	public AzureUpload(string storageConnectionString, string shareName, string directoryPath)
	{
		this.storageConnectionString = storageConnectionString;
		this.shareName = shareName;
		this.directoryPath = directoryPath;
	}

	public async Task<bool> UploadAsync(string filePath, string version)
	{
		try
		{
			CloudStorageAccount account = GetStorageAccount();

			logger.Debug("Uploading {trace} to {azure}/{path}/", filePath, account.FileStorageUri, directoryPath);

			CloudFileShare share = await GetOrCreateShare(account);
			CloudFileDirectory directory = await GetOrCreateTargetDirectory(share);
			await UploadFileAsync(filePath, directory);

			logger.Info("Successfully uploaded {trace} to {azure}/{path}", filePath, account.FileStorageUri, directoryPath);

			return true;
		}
		catch (Exception e)
		{
			logger.Error(e, "Upload of {trace} to Azure File Storage failed", filePath);
			return false;
		}
	}

	private CloudStorageAccount GetStorageAccount()
	{
		CloudStorageAccount account;
		try
		{
			account = CloudStorageAccount.Parse(storageConnectionString);
		}
		catch (Exception e) when (e is ArgumentNullException || e is ArgumentException || e is FormatException)
		{
			// Do not include the connection string to the message as it contains the private connection key!
			throw new UploadFailedException("Invalid Azure File Storage connection string provided.", e);
		}

		return account;
	}

	private async Task<CloudFileShare> GetOrCreateShare(CloudStorageAccount account)
	{
		CloudFileClient client = account.CreateCloudFileClient();
		CloudFileShare share = client.GetShareReference(shareName);

		await share.CreateIfNotExistsAsync();
		if (!await share.ExistsAsync())
		{
			throw new UploadFailedException($"Share {shareName} does not exist and could not be created.");
		}

		return share;
	}

	private async Task<CloudFileDirectory> GetOrCreateTargetDirectory(CloudFileShare share)
	{
		CloudFileDirectory directory = share.GetRootDirectoryReference();

		if (!string.IsNullOrEmpty(directoryPath))
		{
			directory = directory.GetDirectoryReference(directoryPath);
		}

		await directory.CreateIfNotExistsAsync();
		if (!await directory.ExistsAsync())
		{
			throw new UploadFailedException($"Directory {directoryPath} does not exist and could not be created on {share}.");
		}

		return directory;
	}

	private static async Task UploadFileAsync(string sourceFilePath, CloudFileDirectory targetDirectory)
	{
		string fileName = Path.GetFileName(sourceFilePath);
		CloudFile file = targetDirectory.GetFileReference(fileName);
		await file.UploadFromFileAsync(sourceFilePath);
	}

	private class UploadFailedException : Exception
	{
		public UploadFailedException(string message) : base(message)
		{
		}

		public UploadFailedException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}