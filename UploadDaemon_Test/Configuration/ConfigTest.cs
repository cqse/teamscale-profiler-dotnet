using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UploadDaemon.Scanning;

namespace UploadDaemon.Configuration
{
    [TestFixture]
    public class ConfigTest
    {
        private const uint ExistingMethodToken = 100663427;

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
            Assert.That(config.DisableSslValidation, Is.True, "SSL verification disabled by default");
            Assert.That(config.UploadInterval, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.Multiple(() =>
            {
                Assert.That(fooConfig.Enabled, Is.True);
                Assert.That(fooConfig.MergeLineCoverage, Is.True);
                Assert.That(fooConfig.PartialCoverageReport, Is.False);
                Assert.That(fooConfig.TestPathPrefix, Is.Empty);
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
        public void TestLoadedAssemblyPathRegexWithMatch()
        {
            Config config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - loadedAssemblyPathRegex: .*\\foo.dll
                    uploader:
                      directory: C:\upload\foo
                      versionAssembly: foo
            ");

            TraceFile traceFile = new TraceFile("coverage_1_1.txt", new[] {
                @"Assembly=foo:2 Version:1.0.0.0 Path:C:\bla\foo.dll",
                $@"Inlined=2:{ExistingMethodToken}",
            });

            AssemblyExtractor assemblyExtractor = new AssemblyExtractor();
            assemblyExtractor.ExtractAssemblies(traceFile.Lines);

            Config.ConfigForProcess fooConfig = config.CreateConfigForProcess("C:\\test\\foo.exe", assemblyExtractor.Assemblies);
            Assert.That(fooConfig, Is.Not.Null);
            Assert.That(fooConfig.VersionAssembly, Is.EqualTo("foo"));
        }

        [Test]
        public void TestEmbeddedUploadInformation()
        {
            Config config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - loadedAssemblyPathRegex: .*\\NetFrameworkEmbeddedLibrary.dll
                    uploader:
                      directory: C:\upload\foo
                      versionAssembly: foo
            ");
            string targetAssembly = Path.Combine(TestUtils.SolutionRoot.FullName, "test-data", "test-programs", "NetFrameworkEmbeddedLibrary.dll");
            TraceFile traceFile = new TraceFile("coverage_1_1.txt", new[] {
                $@"Assembly=foo:2 Version:1.0.0.0 Path:{targetAssembly}",
                $@"Inlined=2:{ExistingMethodToken}",
            });

            AssemblyExtractor assemblyExtractor = new AssemblyExtractor();
            assemblyExtractor.ExtractAssemblies(traceFile.Lines);

            Config.ConfigForProcess fooConfig = config.CreateConfigForProcess("C:\\test\\foo.exe", assemblyExtractor.Assemblies);
            Assert.That(assemblyExtractor.EmbeddedUploadTargets.Count, Is.AtLeast(1));
            Assert.That(fooConfig, Is.Not.Null);
            Assert.That(fooConfig.VersionAssembly, Is.EqualTo("foo"));
        }

        [Test]
        public void TestLoadedAssemblyPathRegexWithNoMatch()
        {
            Config config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - loadedAssemblyPathRegex: .*\\foo.dll
                    uploader:
                      directory: C:\upload\foo
                      versionAssembly: foo
            ");

            TraceFile traceFile = new TraceFile("coverage_1_1.txt", new[] {
                @"Assembly=nomatch:2 Version:1.0.0.0 Path:C:\bla\nomatch.dll",
                @"Inlined=2:{ExistingMethodToken}",
            });

            AssemblyExtractor assemblyExtractor = new AssemblyExtractor();
            assemblyExtractor.ExtractAssemblies(traceFile.Lines);

            Assert.Throws<Config.InvalidConfigException>(() => config.CreateConfigForProcess("C:\\test\\foo.exe", assemblyExtractor.Assemblies));
        }

        [Test]
        public void TestLoadedAssemblyPathRegexWillPickLastMatchingSection()
        {
            Config config = Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test1
                  - loadedAssemblyPathRegex: .*\\foo.dll
                    uploader:
                      directory: C:\upload\foo
                      versionAssembly: foo
                  - loadedAssemblyPathRegex: .*\\bar.dll
                    uploader:
                      directory: C:\upload\bar
                      versionAssembly: bar
            ");

            TraceFile traceFile1 = new TraceFile("coverage_1_1.txt", new[] {
                @"Assembly=foo:1 Version:1.0.0.0 Path:C:\bla\foo.dll",
                @"Assembly=bar:2 Version:1.0.0.0 Path:C:\bla\bar.dll",
                $@"Inlined=2:{ExistingMethodToken}",
            });

            TraceFile traceFile2 = new TraceFile("coverage_1_1.txt", new[] {
                @"Assembly=bar:1 Version:1.0.0.0 Path:C:\bla\bar.dll",
                @"Assembly=foo:2 Version:1.0.0.0 Path:C:\bla\foo.dll",
                $@"Inlined=2:{ExistingMethodToken}",
            });

            AssemblyExtractor assemblyExtractor1 = new AssemblyExtractor();
            assemblyExtractor1.ExtractAssemblies(traceFile1.Lines);

            Config.ConfigForProcess config1 = config.CreateConfigForProcess("C:\\test\\foo.exe", assemblyExtractor1.Assemblies);
            Assert.That(config1, Is.Not.Null);
            Assert.That(config1.VersionAssembly, Is.EqualTo("bar"));

            AssemblyExtractor assemblyExtractor2 = new AssemblyExtractor();
            assemblyExtractor2.ExtractAssemblies(traceFile2.Lines);

            Config.ConfigForProcess config2 = config.CreateConfigForProcess("C:\\test\\foo.exe", assemblyExtractor1.Assemblies);
            Assert.That(config2, Is.Not.Null);
            Assert.That(config2.VersionAssembly, Is.EqualTo("bar"));
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
        public void TestPartialCoverageAndTestPrefix()
        {
            Config.ConfigForProcess config = Config.Read(@"
                match:
                    - profiler:
                        targetdir: C:\test1
                    - uploader:
                        directory: C:\target
                        pdbDirectory: C:\pdbs
                        revisionFile: C:\revision
                        partialCoverageReport: true
                        testPathPrefix: MT

            ").CreateConfigForProcess("foo.exe");

            Assert.That(config.PartialCoverageReport, Is.True);
            Assert.That(config.TestPathPrefix, Is.EqualTo("MT"));
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
                disableSslValidation: false
                match:
                    - profiler:
                        targetdir: C:\test1
            ");

            Assert.That(config.DisableSslValidation, Is.False, "Enabling of SSL validation");
        }

        [Test]
        public void TestConfigureUploadInterval()
        {
            Config config = Config.Read(@"
                uploadIntervalInMinutes: 42
                match:
                    - profiler:
                        targetdir: C:\test1
            ");

            Assert.That(config.UploadInterval, Is.EqualTo(TimeSpan.FromMinutes(42)));
        }

        [Test]
        public void TestConfigurePurgeThresholds()
        {
            Config config = Config.Read(@"
                archivePurgingThresholdsInDays:
                  uploadedTraces: 1
                  emptyTraces: 3
                  incompleteTraces: 7
                match:
                    - profiler:
                        targetdir: C:\test1
            ");

            Assert.That(config.ArchivePurgingThresholds.UploadedTraces, Is.EqualTo(TimeSpan.FromDays(1)));
            Assert.That(config.ArchivePurgingThresholds.EmptyTraces, Is.EqualTo(TimeSpan.FromDays(3)));
            Assert.That(config.ArchivePurgingThresholds.IncompleteTraces, Is.EqualTo(TimeSpan.FromDays(7)));
        }

        [Test]
        public void TestConfigureSomePurgeThreshold()
        {
            Config config = Config.Read(@"
                archivePurgingThresholdsInDays:
                  uploadedTraces: 1
                match:
                    - profiler:
                        targetdir: C:\test1
            ");

            Assert.That(config.ArchivePurgingThresholds.UploadedTraces, Is.EqualTo(TimeSpan.FromDays(1)));
            Assert.That(config.ArchivePurgingThresholds.EmptyTraces, Is.Null);
            Assert.That(config.ArchivePurgingThresholds.IncompleteTraces, Is.Null);
        }

        [Test]
        public void TestConfigureNoPurgeThreshold()
        {
            Config config = Config.Read(@"
                match:
                    - profiler:
                        targetdir: C:\test1
            ");

            Assert.That(config.ArchivePurgingThresholds.UploadedTraces, Is.Null);
            Assert.That(config.ArchivePurgingThresholds.EmptyTraces, Is.Null);
            Assert.That(config.ArchivePurgingThresholds.IncompleteTraces, Is.Null);
        }

        [Test]
        public void TestIsAssemblyRelativePath()
        {
            Assert.That(Config.IsAssemblyRelativePath("@AssemblyDir"), Is.True);
            Assert.That(Config.IsAssemblyRelativePath("@assemblydir"), Is.True);
            Assert.That(Config.IsAssemblyRelativePath("@AssemblyDir\\foo"), Is.True);
            Assert.That(Config.IsAssemblyRelativePath("@AssemblyDir/foo"), Is.True);
            Assert.That(Config.IsAssemblyRelativePath("Foo/@AssemblyDir"), Is.False);
        }

        [Test]
        public void TestResolveAssemblyRelativePath()
        {
            string assemblyPath = "c:\\path\\assembly.dll";
            Assert.That(Config.ResolveAssemblyRelativePath("@AssemblyDir", assemblyPath), Is.EqualTo("c:\\path"));
            Assert.That(Config.ResolveAssemblyRelativePath("@assemblydir", assemblyPath), Is.EqualTo("c:\\path"));
            Assert.That(Config.ResolveAssemblyRelativePath("@AssemblyDir\\foo", assemblyPath), Is.EqualTo("c:\\path\\foo"));
            Assert.That(Config.ResolveAssemblyRelativePath("@AssemblyDir/foo", assemblyPath), Is.EqualTo("c:\\path/foo"));
            Assert.That(Config.ResolveAssemblyRelativePath("Foo/@AssemblyDir", assemblyPath), Is.Null);
        }
    }
}
