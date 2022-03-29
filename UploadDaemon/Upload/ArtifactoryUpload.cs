using NLog;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Configuration;
using System.Web;
using System.IO.Compression;
using System.IO;
using System.Text;

namespace UploadDaemon.Upload
{

    /// <summary>
    /// Uploads trace files to an Artifactory File Storage.
    /// </summary>
    class ArtifactoryUpload : IUpload
    {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private String uploadPath;

        private readonly ArtifactoryServer artifactoryServer;

        private readonly HttpClient client = new HttpClient();

        public ArtifactoryUpload(ArtifactoryServer server)
        {
            this.artifactoryServer = server;
            if(artifactoryServer.Password != null)
            {
                HttpClientUtils.SetUpBasicAuthentication(client, server);
            } else
            {
                HttpClientUtils.SetUpArtifactoryApiHeader(client, server);
            }
        }

        public string Describe()
        {
            return artifactoryServer.ToString();
        }

        public object GetTargetId()
        {
            return (artifactoryServer.Url, artifactoryServer.Project, artifactoryServer.ZipPath);
        }

        public Task<bool> UploadAsync(string filePath, string version)
        {
            throw new NotImplementedException("You need to convert coverage files to simple coverage first to perform artifactory uploads.");
        }

        public async Task<bool> UploadLineCoverageAsync(string originalTraceFilePath, string lineCoverageReport, RevisionFileUtils.RevisionAndTimestamp revisionAndTimestamp)
        {
            byte[] compressedBytes = CreateZipFile(lineCoverageReport);

            uploadPath = String.Join("/", artifactoryServer.Project, revisionAndTimestamp.BranchName, revisionAndTimestamp.TimestampValue + "-" + revisionAndTimestamp.RevisionValue, "tga", "coverage.zip");
            string url = $"{artifactoryServer.Url}/artifactory/{uploadPath}";
            logger.Info("Uploading line coverage from {uploadPath} to {artifactory} ({url})", uploadPath, artifactoryServer.ToString(), url);

            try
            {
                using (HttpResponseMessage response = await HttpClientUtils.UploadArtifactoryZip(client, url, compressedBytes, uploadPath))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        logger.Info("Successfully uploaded line coverage from {trace} with {parameter}={parameterValue} to {teamscale}",
                            originalTraceFilePath, revisionAndTimestamp.TimestampValue, artifactoryServer.ToString());
                        return true;
                    }
                    else
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        logger.Error("Upload of line coverage to {teamscale} failed with status code {statusCode} due to {reason}. This coverage is lost." +
                            "\n{responseBody}", artifactoryServer.ToString(), response.StatusCode, response.ReasonPhrase, body);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of {trace} to {teamscale} failed due to an exception",
                    originalTraceFilePath, artifactoryServer.ToString());
                return false;
            }
        }

        private static byte[] CreateZipFile(string lineCoverageReport)
        {
            byte[] compressedBytes;
            byte[] reportBytes = Encoding.UTF8.GetBytes(lineCoverageReport);
            using (var outStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
                {
                    var fileInArchive = archive.CreateEntry("coverage.txt", CompressionLevel.Optimal);
                    using (var entryStream = fileInArchive.Open())
                    using (var fileToCompressStream = new MemoryStream(reportBytes))
                    {
                        fileToCompressStream.CopyTo(entryStream);
                    }
                }
                compressedBytes = outStream.ToArray();
            }
            return compressedBytes;
        }
    }
}
