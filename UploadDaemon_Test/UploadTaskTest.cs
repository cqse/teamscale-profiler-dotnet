using UploadDaemon;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Upload;
using UploadDaemon.Configuration;

namespace UploadDaemon
{
    [TestFixture]
    public class UploadTaskTest
    {
        private const string TraceDirectory = @"C:\users\public\traces";
        private const string TraceDirectoryWithSpace = @"C:\users\user with spaces\traces";
        private const string PdbDirectory = @"C:\pdbs";
        private const string RevisionFile = @"C:\revision.txt";

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
        public void TracesShouldBeArchivedAfterASuccessfulLineCoverageUpload()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646:100678050" },
            { RevisionFile, @"revision: 12345" },
            { PdbDirectory, new MockDirectoryData() },
        });

            new UploadTask(fileSystem, new MockUploadFactory(true), new MockLineCoverageSynthesizer()).Run(lineCoverageConfig);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"uploaded\coverage_1_1.txt");
        }

        [Test]
        public void TracesShouldNotBeArchivedAfterAFailedLineCoverageUpload()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646:100678050" },
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
Inlined=1:33555646:100678050" },
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
Inlined=1:33555646:100678050" },
            { RevisionFile, @"revision: 12345" },
            { PdbDirectory, new MockDirectoryData() },
        });

            new UploadTask(fileSystem, new MockUploadFactory(true), new MockLineCoverageSynthesizer()).Run(archiveLineCoverageConfig);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"uploaded\coverage_1_1.txt", @"converted-line-coverage\coverage_1_1.txt.simple");
        }

        [Test]
        public void TestArchivingTraceWithMissingProcess()
        {
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=OtherAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050" },
        });

            new UploadTask(fileSystem, new MockUploadFactory(false), new MockLineCoverageSynthesizer()).Run(lineCoverageConfig);

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

            new UploadTask(fileSystem, new MockUploadFactory(true), new MockLineCoverageSynthesizer()).Run(lineCoverageConfig);

            AssertFilesInDirectory(fileSystem, TraceDirectory, @"empty-traces\coverage_1_1.txt");
        }

        [Test]
        public void TestPathsWithSpaces()
        {
            string mockedRevisionFile = FileInTraceDirectoryWithSpace("revision.txt");
            IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectoryWithSpace("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646:100678050" },
            { mockedRevisionFile, "timestamp: 123456" }
        });

            Config configWithSpaceInTraceDirectory = Config.Read($@"
            match:
              - uploader:
                  pdbDirectory: {PdbDirectory}
                  revisionFile: {mockedRevisionFile}
                  directory: C:\store
                profiler:
                  targetdir: {TraceDirectoryWithSpace}
        ");

            new UploadTask(fileSystem, new MockUploadFactory(true), new MockLineCoverageSynthesizer()).Run(configWithSpaceInTraceDirectory);

            AssertFilesInDirectory(fileSystem, TraceDirectoryWithSpace, @"uploaded\coverage_1_1.txt", "revision.txt");
        }

        private class MockUploadFactory : IUploadFactory
        {
            public readonly Mock<IUpload> uploadMock;

            public MockUploadFactory(bool successful)
            {
                this.uploadMock = new Mock<IUpload>();
                uploadMock.Setup(upload => upload.UploadLineCoverageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RevisionFileUtils.RevisionOrTimestamp>()))
                    .Returns(Task.FromResult(successful));
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
            public Dictionary<string, FileCoverage> ConvertToLineCoverage(ParsedTraceFile traceFile, string symbolDirectory, GlobPatternList assemblyPatterns)
            {
                if (!shouldProduceCoverage)
                {
                    return null;
                }

                // Return some arbitrary coverage
                FileCoverage fileCoverage = new FileCoverage();
                fileCoverage.CoveredLineRanges.Add((12, 33));
                return new Dictionary<string, FileCoverage>()
            {
                { "file1.cs", fileCoverage }
            };
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
            Assert.That(relativePaths, Is.EquivalentTo(expectedFileNames));
        }
    }
}
