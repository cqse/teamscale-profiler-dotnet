using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Uploads data to a storage location, e.g. a directory on disk or a remote endpoint.
/// </summary>
public interface IUpload
{
    /// <summary>
    /// Performs the upload asynchronously (may also be synchronous).
    /// Must not throw any exceptions.
    /// </summary>
    /// <param name="filePath">The path of the file to upload.</param>
    /// <param name="version">The parsed version number.</param>
    /// <returns>Whether the upload succeeded</returns>
    Task<bool> UploadAsync(string filePath, string version);
}
