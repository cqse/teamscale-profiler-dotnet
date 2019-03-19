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
            Assert.False(config.EnableSslValidation, "SSL verification disabled by default");
            Assert.Multiple(() =>
            {
                Assert.That(fooConfig.Enabled, Is.True);
                Assert.That(fooConfig.AzureFileStorage, Is.Null);
                Assert.That(fooConfig.Teamscale, Is.Null);
                Assert.That(fooConfig.VersionPrefix, Is.Empty);
                Assert.That(fooConfig.PdbDirectory, Is.Null);
                Assert.That(fooConfig.AssemblyPatterns, Is.Not.Null);
                Assert.That(fooConfig.RevisionFile, Is.Null);
                Assert.That(barConfig.Directory, Is.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(fooConfig.AssemblyPatterns.Matches("ProfilerGUI"), Is.True);
                Assert.That(fooConfig.AssemblyPatterns.Matches("System"), Is.False);
                Assert.That(fooConfig.AssemblyPatterns.Matches("Microsoft.Something"), Is.False);
                Assert.That(fooConfig.AssemblyPatterns.Matches("mscorlib"), Is.False);
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
        public void TargetDirOptionIsCaseInsensitive()
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
        public void MustSpecifyAtLeastOneTargetDir()
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
            Exception exception = Assert.Throws<Config.InvalidConfigException>(() =>
            {
                Config.Read(@"
                    match:
                      - profiler:
                          targetdir: C:\test1
                      - uploader:
                          versionAssembly: Assembly
                ").CreateConfigForProcess("foo.exe");
            });

            Assert.That(exception.Message, Contains.Substring("teamscale"));
        }

        [Test]
        public void MissingVersionAssembly()
        {
            Exception exception = Assert.Throws<Config.InvalidConfigException>(() =>
            {
                Config.Read(@"
                    match:
                      - profiler:
                          targetdir: C:\test1
                      - uploader:
                          directory: .
                ").CreateConfigForProcess("foo.exe");
            });

            Assert.That(exception.Message, Contains.Substring("versionAssembly"));
        }

        [Test]
        public void ValidLineCoverageUpload()
        {
            IEnumerable<string> errors = Config.Read(@"
                match:
                    - profiler:
                        targetdir: C:\test1
                    - uploader:
                        directory: C:\target
                        pdbDirectory: C:\pdbs
                        revisionFile: C:\revision
            ").CreateConfigForProcess("foo.exe").Validate();

            Assert.That(errors, Is.Empty, "valid configuration must not raise any errors");
        }

        [Test]
        public void MissingRevisionFile()
        {
            Exception exception = Assert.Throws<Config.InvalidConfigException>(() =>
            {
                Config.Read(@"
                match:
                    - profiler:
                        targetdir: C:\test1
                    - uploader:
                        directory: C:\target
                        pdbDirectory: C:\pdbs
                ").CreateConfigForProcess("foo.exe");
            });

            Assert.That(exception.Message, Contains.Substring("revisionFile"));
        }

        [Test]
        public void BothVersionAssemblyAndPdbDirectoryConfigured()
        {
            Exception exception = Assert.Throws<Config.InvalidConfigException>(() =>
            {
                Config.Read(@"
                match:
                    - profiler:
                        targetdir: C:\test1
                    - uploader:
                        directory: C:\target
                        pdbDirectory: C:\pdbs
                        versionAssembly: Assembly
                        revisionFile: C:\revision.txt
                ").CreateConfigForProcess("foo.exe");
            });

            Assert.That(exception.Message, Contains.Substring("pdbDirectory").And.Contains("versionAssembly"));
        }

        [Test]
        public void NotSpecifyingIncludePatternMeansEverythingIsIncluded()
        {
            Config.ConfigForProcess config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - uploader:
                      directory: C:\dir
                      revisionFile: C:\rev.txt
                      pdbDirectory: C:\pdbs
                      assemblyPatterns:
                        exclude: [ 'Bar' ]
            ").CreateConfigForProcess("foo.exe");

            Assert.That(config.AssemblyPatterns.Describe(), Is.EqualTo("include=* exclude=Bar"));
        }

        [Test]
        public void NotSpecifyingExcludePatternMeansDefaultExcludesAreUsed()
        {
            Config.ConfigForProcess config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - uploader:
                      directory: C:\dir
                      revisionFile: C:\rev.txt
                      pdbDirectory: C:\pdbs
                      assemblyPatterns:
                        include: [ 'Bar' ]
            ").CreateConfigForProcess("foo.exe");

            Assert.That(config.AssemblyPatterns.Describe(), Does.StartWith("include=Bar exclude=").And.Contains("mscorlib"));
        }

        [Test]
        public void TestEnableSslVerification()
        {
            Config config = Config.Read(@"
                enableSslValidation: true
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

            Assert.True(config.EnableSslValidation, "Enabling of SSL validation");
        }
    }
}
