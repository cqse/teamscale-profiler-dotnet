using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UploadDaemon.Configuration;
using UploadDaemon_Test.Upload;
using static UploadDaemon.Configuration.Config;
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;

namespace UploadDaemon
{
    [TestFixture]
    public class UploadDaemonSystemTest
    {
        // 100663427 corresponds to MainViewModel#get_SelectedBitnessIndex in ProfilerGUI.pdb
        // obtained with cvdump.exe
        private static readonly uint ExistingMethodToken = 100663427;

        private static string TargetDir => Path.Combine(TestUtils.TestTempDirectory, "targetdir");
        private static string UploadDir => Path.Combine(TestUtils.TestTempDirectory, "upload");
        private static string RevisionFile => Path.Combine(TestUtils.TestTempDirectory, "revision.txt");
        private static string PdbDirectory => TestUtils.TestDataDirectory;

        [SetUp]
        public void CreateTemporaryTestDir()
        {
            var testDir = new DirectoryInfo(TestUtils.TestTempDirectory);
            if (testDir.Exists)
            {
                testDir.Delete(true);
            }

            testDir.Create();
            new DirectoryInfo(TargetDir).Create();
            new DirectoryInfo(UploadDir).Create();
        }

        [Test]
        public void TestTrailingSlashInUrl()
        {
            ConfigForProcess configWithTrailingSlash = Config.Read($@"
                match:
                  - profiler:
                      targetdir: {TargetDir}
                    uploader:
                      versionAssembly: VersionAssembly
                      directory: {UploadDir}
                      teamscale:
                        url: http://localhost:8080/
            ").CreateConfigForProcess("test.exe");

            Assert.That(configWithTrailingSlash.Teamscale.Url, Is.EqualTo("http://localhost:8080"));

            ConfigForProcess configWithoutTrailingSlash = Config.Read($@"
                match:
                  - profiler:
                      targetdir: {TargetDir}
                    uploader:
                      versionAssembly: VersionAssembly
                      directory: {UploadDir}
                      teamscale:
                        url: http://localhost:8080
            ").CreateConfigForProcess("test.exe");

            Assert.That(configWithoutTrailingSlash.Teamscale.Url, Is.EqualTo("http://localhost:8080"));
        }

        [Test]
        public void TestSimpleDirectoryUpload()
        {
            string coverageFileName = "coverage_1_1.txt";
            File.WriteAllText(Path.Combine(TargetDir, coverageFileName), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646:100678050");

            new UploadDaemon().RunOnce(Config.Read($@"
            match:
              - profiler:
                  targetdir: {TargetDir}
                uploader:
                  versionAssembly: VersionAssembly
                  directory: {UploadDir}
        "));

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(Path.Combine(UploadDir, coverageFileName)), Is.True, "file was uploaded successfully");
                Assert.That(File.Exists(Path.Combine(TargetDir, coverageFileName)), Is.False, "file was removed from profiler output dir");
                Assert.That(File.Exists(Path.Combine(TargetDir, "uploaded", coverageFileName)), Is.True, "file was archived");
            });
        }

        [Test]
        public void TestSimpleDirectoryLineCoverageUpload()
        {
            string coverageFileName = "coverage_1_1.txt";
            File.WriteAllText(Path.Combine(TargetDir, coverageFileName), $@"Assembly=ProfilerGUI:2 Version:1.0.0.0
Process=foo.exe
Inlined=2:{ExistingMethodToken}");

            File.WriteAllText(RevisionFile, "revision: 12345");

            new UploadDaemon().RunOnce(Config.Read($@"
            match:
              - profiler:
                  targetdir: {TargetDir}
                uploader:
                  directory: {UploadDir}
                  revisionFile: {RevisionFile}
                  pdbDirectory: {PdbDirectory}
        "));

            IEnumerable<string> uploadedFiles = Directory.GetFiles(UploadDir);

            Assert.Multiple(() =>
            {
                Assert.That(uploadedFiles.Select(file => Path.GetExtension(file)), Is.EquivalentTo(new string[] { ".simple", ".metadata" }));
                Assert.That(File.Exists(Path.Combine(TargetDir, coverageFileName)), Is.False, "file was removed from profiler output dir");
                Assert.That(File.Exists(Path.Combine(TargetDir, "uploaded", coverageFileName)), Is.True, "file was archived");
            });
        }

        [Test]
        public void TestArchivePurging()
        {
            File.WriteAllText(Path.Combine(TargetDir, "coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646:100678050");
            File.WriteAllText(Path.Combine(TargetDir, "coverage_1_2.txt"), @"");

            new UploadDaemon().RunOnce(Config.Read($@"
            archivePurgingThresholdsInDays:
              uploadedTraces: 0
              emptyTraces: 0
            match:
              - profiler:
                  targetdir: {TargetDir}
                uploader:
                  versionAssembly: VersionAssembly
                  directory: {UploadDir}
        "));

            IEnumerable<string> archivedFiles = Directory.GetFiles(TargetDir, "*.txt", SearchOption.AllDirectories);
            Assert.That(archivedFiles, Is.Empty);
        }

        [Test]
        public void TestNet6EmbeddedAssembly()
        {
            string coverageFileName = "coverage_1_1.txt";
            string targetAssembly = Path.Combine(TestUtils.SolutionRoot.FullName, "test-data", "test-programs", "Net6ConsoleApp.dll");
            File.WriteAllText(Path.Combine(TargetDir, coverageFileName), $@"Assembly=ProfilerTestee:2 Version:1.0.0.0 Path:{targetAssembly}
Process={targetAssembly}
Inlined=2:100663298");
            TeamscaleMockServer mockServer = new TeamscaleMockServer(1337);
            mockServer.SetResponse(200);
            new UploadDaemon().RunOnce(Config.Read($@"
            match:
              - profiler:
                  targetdir: {TargetDir}
                uploader:
                  directory: {UploadDir}
                  pdbDirectory: {PdbDirectory}\ProfilerTestee
                  teamscale:
                    url: http://localhost:1337
                    username: admin
                    accessKey: fookey
                    partition: my_partition

        "));

            List<string> requests = mockServer.GetRecievedRequests();
            mockServer.StopServer();
            Assert.Multiple(() =>
            {
                StringAssert.Contains("/p/TestProject/", requests[0]);
                StringAssert.Contains("revision=master%3aHEAD", requests[0]);
                Assert.That(File.Exists(Path.Combine(TargetDir, coverageFileName)), Is.False, "File is in upload folder.");
                Assert.That(File.Exists(Path.Combine(TargetDir, "uploaded", coverageFileName)), Is.True, "File was properly archived.");
            });
        }

        [Test]
        public void TestMultiProjectCoverageUpload()
        {
            string coverageFileName = "coverage_1_1.txt";
            string targetAssembly = Path.Combine(TestUtils.SolutionRoot.FullName, "test-data", "test-programs", "ProfilerTestee.exe");
            File.WriteAllText(Path.Combine(TargetDir, coverageFileName), $@"Assembly=ProfilerTestee:2 Version:1.0.0.0 Path:{targetAssembly}
Process={targetAssembly}
Inlined=2:100663298");
            string uploadTargetFile = Path.Combine(TestUtils.TestTempDirectory, "uploadTargets.json");
            List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets = new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>
            {
                ("projectA", new RevisionOrTimestamp("12345", true)),
                ("projectB", new RevisionOrTimestamp("55555", true)),
                ("projectC", new RevisionOrTimestamp("1234657651", false))
            };
            string jsonContent = JsonConvert.SerializeObject(uploadTargets);
            File.WriteAllText(uploadTargetFile, jsonContent);
            TeamscaleMockServer mockServer = new TeamscaleMockServer(1337);
            mockServer.SetResponse(200);
            new UploadDaemon().RunOnce(Config.Read($@"
            match:
              - profiler:
                  targetdir: {TargetDir}
                uploader:
                  directory: {UploadDir}
                  pdbDirectory: {PdbDirectory}\ProfilerTestee
                  uploadTargetFile: {uploadTargetFile}
                  teamscale:
                    url: http://localhost:1337
                    username: admin
                    accessKey: fookey
                    partition: my_partition

        "));

            List<string> requests = mockServer.GetRecievedRequests();
            mockServer.StopServer();
            AssertMultiProjectRequests(requests, coverageFileName);
        }

        private void AssertMultiProjectRequests(List<string> requests, string coverageFileName)
        {
            Assert.Multiple(() =>
            {
                StringAssert.Contains("/p/projectA/", requests[0]);
                StringAssert.Contains("revision=12345", requests[0]);
                StringAssert.Contains("/p/projectB/", requests[1]);
                StringAssert.Contains("revision=55555", requests[1]);
                StringAssert.Contains("/p/projectC/", requests[2]);
                StringAssert.Contains("t=1234657651", requests[2]);
                // Check that the embedded resource has been uploaded as well.
                StringAssert.Contains("/p/ProjectA/", requests[3]);
                StringAssert.Contains("revision=HEAD", requests[3]);
                Assert.That(File.Exists(Path.Combine(TargetDir, coverageFileName)), Is.False, "File is in upload folder.");
                Assert.That(File.Exists(Path.Combine(TargetDir, "uploaded", coverageFileName)), Is.True, "File was properly archived.");
            });
        }
    }
}
