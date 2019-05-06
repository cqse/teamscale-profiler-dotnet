using System.Threading.Tasks;
using UploadDaemon.Upload;
using UploadDaemon.SymbolAnalysis;

/// <summary>
/// Mock for the IUpload interface for testing.
/// </summary>
public class MockUpload : IUpload
{
    private readonly bool returnValue;

    /// <summary>
    /// The last version that was passed to the UploadAsnyc method or null if that method was never called.
    /// </summary>
    public string LastUsedVersion { get; private set; } = null;

    public MockUpload(bool returnValue)
    {
        this.returnValue = returnValue;
    }

    /// <summary>
    /// Fakes an upload and returns the result passed to the constructor.
    /// </summary>
    public Task<bool> UploadAsync(string filePath, string version)
    {
        LastUsedVersion = version;
        return Task.FromResult(returnValue);
    }

    /// <inheritdoc/>
    public string Describe()
    {
        return "MockUpload";
    }

    /// <inheritdoc/>
    public Task<bool> UploadLineCoverageAsync(string originalTraceFilePath, string lineCoverageReport, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)
    {
        return Task.FromResult(returnValue);
    }

    /// <summary>
    /// Returns the returnValue as the dictionary key, i.e. uploads are equal
    /// if they have the same return value.
    /// </summary>
    /// <returns></returns>
    public object GetTargetId()
    {
        return returnValue;
    }
}
