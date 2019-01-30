using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Common
{
    [TestFixture]
    public class ConfigTest
    {
        [Test]
        public void TestDefaultValues()
        {
            Config config = Config.Read(@"
                match:
                    - profiler:
                        targetdir: C:\test1
                    - uploader:
                        versionAssembly: assembly
                    - executableName: foo.exe
                      uploader:
                        directory: dir
                    - executableName: bar.exe
                      uploader:
                        teamscale:
                          url: ts
            ");

            Config.ConfigForProcess fooConfig = config.CreateConfigForProcess("C:\\test\\foo.exe");
            Config.ConfigForProcess barConfig = config.CreateConfigForProcess("C:\\test\\bar.exe");
            Assert.That(fooConfig, Is.Not.Null, "foo config not null");
            Assert.Multiple(() =>
            {
                Assert.That(fooConfig.Enabled, Is.True);
                Assert.That(fooConfig.AzureFileStorage, Is.Null);
                Assert.That(fooConfig.FileUpload, Is.Null);
                Assert.That(fooConfig.Teamscale, Is.Null);
                Assert.That(barConfig.Directory, Is.Null);
            });
        }

        [Test]
        public void ExecutableNameMatchingMustBeCaseInsensitive()
        {
            Config config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - executableName: FoO.exe
                    uploader:
                      versionAssembly: Bla
                      directory: C:\upload
            ");

            Config.ConfigForProcess fooConfig = config.CreateConfigForProcess("C:\\test\\foo.exe");
            Assert.That(fooConfig, Is.Not.Null, "foo config not null");
            Assert.That(fooConfig.VersionAssembly, Is.EqualTo("Bla"));
        }

        [Test]
        public void MatchingSectionsAreAppliedInOrder()
        {
            Config config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - executableName: foo.exe
                    uploader:
                      versionAssembly: Bla
                      directory: C:\upload
                  - executableName: foo.exe
                    uploader:
                      versionAssembly: Blu
                      directory: C:\upload
            ");

            Config.ConfigForProcess fooConfig = config.CreateConfigForProcess("C:\\test\\foo.exe");
            Assert.That(fooConfig, Is.Not.Null, "foo config not null");
            Assert.That(fooConfig.VersionAssembly, Is.EqualTo("Blu"));
        }

        [Test]
        public void NonMatchingSectionsAreIgnored()
        {
            Config config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - executableName: foo.exe
                    uploader:
                      versionAssembly: Bla
                      directory: C:\upload
                  - executableName: bar.exe
                    uploader:
                      versionAssembly: Blu
                      directory: C:\upload
            ");

            Config.ConfigForProcess fooConfig = config.CreateConfigForProcess("C:\\test\\foo.exe");
            Assert.That(fooConfig, Is.Not.Null, "foo config not null");
            Assert.That(fooConfig.VersionAssembly, Is.EqualTo("Bla"));
        }

        [Test]
        public void TestPathRegex()
        {
            Config config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - executablePathRegex: .*foo.exe
                    uploader:
                      versionAssembly: Bla
                      directory: C:\upload
                  - executablePathRegex: .*bar.exe
                    uploader:
                      versionAssembly: Blu
                      directory: C:\upload
            ");

            Config.ConfigForProcess fooConfig = config.CreateConfigForProcess("C:\\test\\foo.exe");
            Assert.That(fooConfig, Is.Not.Null, "foo config not null");
            Assert.That(fooConfig.VersionAssembly, Is.EqualTo("Bla"));
        }

        [Test]
        public void IfBothRegexAndExecutableNameAreGivenBothMustMatch()
        {
            Config config = Config.Read(@"
                match:
                    - profiler:
                        targetdir: C:\test1
                    - executablePathRegex: .*foo.exe
                      executableName: foo.exe
                      uploader:
                        versionAssembly: foofoo
                        directory: C:\upload
                    - executablePathRegex: .*bar.exe
                      executableName: foo.exe
                      uploader:
                        versionAssembly: barfoo
                        directory: C:\upload
                    - executablePathRegex: .*foo.exe
                      executableName: bar.exe
                      uploader:
                        versionAssembly: foobar
                        directory: C:\upload
            ");

            Config.ConfigForProcess fooConfig = config.CreateConfigForProcess("C:\\test\\foo.exe");
            Assert.That(fooConfig, Is.Not.Null, "foo config not null");
            Assert.That(fooConfig.VersionAssembly, Is.EqualTo("foofoo"));
        }

        [Test]
        public void NoMatchersMeansSectionAlwaysApplies()
        {
            Config config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                    uploader:
                      versionAssembly: Bla
                      directory: C:\upload
            ");

            Config.ConfigForProcess fooConfig = config.CreateConfigForProcess("C:\\test\\foo.exe");
            Assert.That(fooConfig, Is.Not.Null, "foo config not null");
            Assert.That(fooConfig.VersionAssembly, Is.EqualTo("Bla"));
        }

        [Test]
        public void TraceDirectoryOptionIsCaseInsensitive()
        {
            Config config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - profiler:
                      TargetDir: C:\test2
            ");

            Assert.That(config.TraceDirectoriesToWatch, Is.EquivalentTo(new string[] { "C:\\test1", "C:\\test2" }));
        }

        [Test]
        public void MustSpecifyAtLeastOneTraceDirectory()
        {
            Assert.Throws<Config.InvalidConfigException>(() =>
            {
                Config config = Config.Read(@"
                    match:
                      - uploader:
                          versionAssembly: Bla
                          directory: C:\upload
                ");
            });
        }

        [Test]
        public void ValidTeamscaleConfiguration()
        {
            IEnumerable<string> errors = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                    uploader:
                      versionAssembly: Assembly
                      teamscale:
                        url: url
                        username: user
                        accessKey: token
                        project: project
                        partition: partition
            ").CreateConfigForProcess("foo.exe").Validate();

            Assert.That(errors, Is.Empty, "valid configuration must not raise any errors");
        }

        [Test]
        public void AzureFileStorageUploadConfigurationIsValid()
        {
            IEnumerable<string> errors = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - uploader:
                      versionAssembly: Assembly
                      azureFileStorage:
                        connectionString: connection-string
                        shareName: share
                        directory: dir
            ").CreateConfigForProcess("foo.exe").Validate();

            Assert.That(errors, Is.Empty, "valid configuration must not raise any errors");
        }

        [Test]
        public void ValidDirectoryConfig()
        {
            IEnumerable<string> errors = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - uploader:
                      versionAssembly: Assembly
                      directory: .
            ").CreateConfigForProcess("foo.exe").Validate();

            Assert.That(errors, Is.Empty, "valid configuration must not raise any errors");
        }

        [Test]
        public void MissingUploadMethod()
        {
            Assert.Throws<Config.InvalidConfigException>(() =>
            {
                Config.Read(@"
                    match:
                      - uploader:
                          versionAssembly: Assembly
                ").CreateConfigForProcess("foo.exe");
            });
        }

        [Test]
        public void MissingVersionAssembly()
        {
            Assert.Throws<Config.InvalidConfigException>(() =>
            {
                Config.Read(@"
                    match:
                      - uploader:
                          directory: .
                ").CreateConfigForProcess("foo.exe");
            });
        }
    }
}