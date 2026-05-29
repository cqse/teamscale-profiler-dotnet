using NUnit.Framework;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace UploadDaemon.Upload
{
    [TestFixture]
    public class ArtifactoryUploadTest
    {
        [Test]
        public void CreatesValidZipFileContainingTheReport()
        {
            string reportContent = "# isMethodAccurate=true\nfile1.cs\n1-5\n";
            string entryName = "partition/simple_1.txt";

            byte[] zipBytes = ArtifactoryUpload.CreateZipFile(reportContent, entryName);

            using (MemoryStream zipStream = new MemoryStream(zipBytes))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                Assert.That(archive.Entries, Has.Count.EqualTo(1));

                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.That(entry, Is.Not.Null);

                using (StreamReader reader = new StreamReader(entry.Open(), Encoding.UTF8))
                {
                    Assert.That(reader.ReadToEnd(), Is.EqualTo(reportContent));
                }
            }
        }
    }
}
