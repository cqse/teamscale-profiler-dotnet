using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using UploadDaemon.Configuration;
using UploadDaemon.Report;
using UploadDaemon.Report.Simple;
using UploadDaemon.Scanning;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Upload;

namespace UploadDaemon
{
    [TestFixture]
    public class UploadTaskTest
    {
        private const string TraceDirectory = @"C:\users\public\traces";
        private const string TraceDirectoryWithSpace = @"C:\users\user with spaces\traces";
        private const string PdbDirectory = @"C:\pdbs";
        private const string RevisionFile = @"C:\revision.txt";
        private const string VersionAssembly = "VersionAssembly";

        private static readonly Config config = Config.Read($@"
        match:
          - uploader:
              versionAssembly: {VersionAssembly}
              directory: C:\store
            profiler:
              targetdir: {TraceDirectory}
    ");

        private static readonly Config lineCoverageConfig = Config.Read($@"
        match:
          - uploader:
              directory: C:\store
              pdbDirectory: {PdbDirectory}
              revisionFile: {RevisionFile}
            profiler:
              targetdir: {TraceDirectory}
    ");

        private static readonly Config archiveLineCoverageConfig = Config.Read($@"
        archiveLineCoverage: true
        match:
          - uploader:
              directory: C:\store
              pdbDirectory: {PdbDirectory}
              revisionFile: {RevisionFile}
              mergeLineCoverage: false
            profiler:
              targetdir: {TraceDirectory}
    ");

        [Test]
        public void TracesShouldBeArchivedAfterASuccessfulUpload()

        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646" },
        });

            new UploadTask(fileSystem, new MockUploadFactory(true), new MockLineCoverageSynthesizer()).Run(config);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"uploaded\coverage_1_1.txt");
        }

        [Test]
        public void TracesShouldBeArchivedAfterASuccessfulLineCoverageUpload()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646" },
            { RevisionFile, @"revision: 12345" },
            { PdbDirectory, new MockDirectoryData() },
        });

            new UploadTask(fileSystem, new MockUploadFactory(true), new MockLineCoverageSynthesizer()).Run(lineCoverageConfig);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"uploaded\coverage_1_1.txt");
        }

        [Test]
        public void TracesShouldNotBeArchivedAfterAFailedUpload()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646" },
        });

            new UploadTask(fileSystem, new MockUploadFactory(false), new MockLineCoverageSynthesizer()).Run(config);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"coverage_1_1.txt");
        }

        [Test]
        public void TracesShouldNotBeArchivedAfterAFailedLineCoverageUpload()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646" },
            { RevisionFile, @"revision: 12345" },
            { PdbDirectory, new MockDirectoryData() },
        });

            new UploadTask(fileSystem, new MockUploadFactory(false), new MockLineCoverageSynthesizer()).Run(lineCoverageConfig);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"coverage_1_1.txt");
        }

        [Test]
        public void TestArchivingTracesThatProduceNoLineCoverage()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646" },
            { RevisionFile, @"revision: 12345" },
            { PdbDirectory, new MockDirectoryData() },
        });

            new UploadTask(fileSystem, new MockUploadFactory(true), new MockLineCoverageSynthesizer(false)).Run(lineCoverageConfig);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"no-line-coverage\coverage_1_1.txt");
        }

        [Test]
        public void TestArchivingUploadedLineCoverage()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646" },
            { RevisionFile, @"revision: 12345" },
            { PdbDirectory, new MockDirectoryData() },
        });

            new UploadTask(fileSystem, new MockUploadFactory(true), new MockLineCoverageSynthesizer()).Run(archiveLineCoverageConfig);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"uploaded\coverage_1_1.txt", @"converted-coverage\coverage_1_1.txt_1.simple");
        }

        [Test]
        public void TestArchivingTraceWithMissingVersion()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=OtherAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646" },
        });

            new UploadTask(fileSystem, new MockUploadFactory(false), new MockLineCoverageSynthesizer()).Run(config);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"missing-version\coverage_1_1.txt");
        }

        [Test]
        public void TestArchivingTraceWithMissingProcess()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=OtherAssembly:1 Version:4.0.0.0
Inlined=1:33555646" },
        });

            new UploadTask(fileSystem, new MockUploadFactory(false), new MockLineCoverageSynthesizer()).Run(config);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"missing-process\coverage_1_1.txt");
        }

        [Test]
        public void TestArchivingEmptyTrace()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe" },
        });

            new UploadTask(fileSystem, new MockUploadFactory(true), new MockLineCoverageSynthesizer()).Run(config);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"empty-traces\coverage_1_1.txt");
        }

        [Test]
        public void TestPathsWithSpaces()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectoryWithSpace("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646" },
        });

            Config configWithSpaceInTraceDirectory = Config.Read($@"
            match:
              - uploader:
                  versionAssembly: {VersionAssembly}
                  directory: C:\store
                profiler:
                  targetdir: {TraceDirectoryWithSpace}
        ");

            new UploadTask(fileSystem, new MockUploadFactory(true), new MockLineCoverageSynthesizer()).Run(configWithSpaceInTraceDirectory);

            AssertFilesInDirectory(fileSystem, TraceDirectoryWithSpace, @"uploaded\coverage_1_1.txt");
        }

        [Test]
        public void TestVersionPrefix()
        {
            Config config = Config.Read($@"
            match:
              - uploader:
                  versionAssembly: {VersionAssembly}
                  versionPrefix: prefix_
                  directory: C:\store
                profiler:
                  targetdir: {TraceDirectoryWithSpace}
        ");

            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectoryWithSpace("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646" },
        });

            MockUploadFactory uploadFactory = new MockUploadFactory(true);
            new UploadTask(fileSystem, uploadFactory, new MockLineCoverageSynthesizer()).Run(config);

            uploadFactory.uploadMock.Verify(upload => upload.UploadAsync(It.IsAny<string>(), "prefix_4.0.0.0"));
        }

        private class MockUploadFactory : IUploadFactory
        {
            public readonly Mock<IUpload> uploadMock;

            public MockUploadFactory(bool successfull)
            {
                this.uploadMock = new Mock<IUpload>();
                uploadMock.Setup(upload => upload.UploadAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(successfull));
                uploadMock.Setup(upload => upload.UploadLineCoverageAsync(It.IsAny<string>(), It.IsAny<ICoverageReport>(), It.IsAny<RevisionFileUtils.RevisionOrTimestamp>()))
                    .Returns(Task.FromResult(successfull));
            }

            /// <inheritdoc/>
            public IUpload CreateUpload(Config.ConfigForProcess config, IFileSystem fileSystem)
            {
                return uploadMock.Object;
            }
        }

        private class MockLineCoverageSynthesizer : ILineCoverageSynthesizer
        {
            private readonly bool shouldProduceCoverage;

            public MockLineCoverageSynthesizer(bool shouldProduceCoverage = true)
            {
                this.shouldProduceCoverage = shouldProduceCoverage;
            }

            /// <inheritdoc/>
            public SimpleCoverageReport ConvertToLineCoverage(Trace trace, string symbolDirectory, GlobPatternList assemblyPatterns)
            {
                var coverage = new Dictionary<string, FileCoverage>();

                if (shouldProduceCoverage)
                {
                    // Return some arbitrary coverage
                    coverage["file1.cs"] = new FileCoverage((12, 33));
                }

                return new SimpleCoverageReport(coverage);
            }
        }

        /// <summary>
        /// Returns a file with the given name in the trace directory.
        /// </summary>
        private string FileInTraceDirectory(string fileName)
        {
            return Path.Combine(TraceDirectory, fileName);
        }

        /// <summary>
        /// Returns a file with the given name in the trace directory (which contains a space in the path).
        /// </summary>
        private string FileInTraceDirectoryWithSpace(string fileName)
        {
            return Path.Combine(TraceDirectoryWithSpace, fileName);
        }

        private void AssertFilesInDirectory(IFileSystem fileSystem, string directory, params string[] expectedFileNames)
        {
            string[] files = fileSystem.Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
            IEnumerable<string> relativePaths = files.Select(path => path.Substring(directory.Length + 1));
            CollectionAssert.AreEquivalent(relativePaths, expectedFileNames);
        }
    }
}
