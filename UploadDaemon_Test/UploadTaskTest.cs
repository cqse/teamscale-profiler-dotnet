using Common;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using UploadDaemon;
using UploadDaemon.Upload;
using UploadDaemon.SymbolAnalysis;

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

    [Test]
    public void TracesShouldBeArchivedAfterASuccessfulUpload()

    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646:100678050" },
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
Inlined=1:33555646:100678050" },
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
Inlined=1:33555646:100678050" },
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
    public void TestArchivingTraceWithMissingVersion()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=OtherAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646:100678050" },
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
Inlined=1:33555646:100678050" },
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
Inlined=1:33555646:100678050" },
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
Inlined=1:33555646:100678050" },
        });

        MockUploadFactory uploadFactory = new MockUploadFactory(true);
        new UploadTask(fileSystem, uploadFactory, new MockLineCoverageSynthesizer()).Run(config);

        Assert.That(uploadFactory.mockUpload.LastUsedVersion, Is.EqualTo("prefix_4.0.0.0"));
    }

    private class MockUploadFactory : IUploadFactory
    {
        public readonly MockUpload mockUpload;

        public MockUploadFactory(bool returnValue)
        {
            this.mockUpload = new MockUpload(returnValue);
        }

        public IUpload CreateUpload(Config.ConfigForProcess config, IFileSystem fileSystem)
        {
            return mockUpload;
        }
    }

    private class MockUpload : IUpload
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

        public string Describe()
        {
            return "MockUpload";
        }

        public Task<bool> UploadLineCoverageAsync(string originalTraceFilePath, string lineCoverageReport, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)
        {
            return Task.FromResult(returnValue);
        }
    }

    private class MockLineCoverageSynthesizer : ILineCoverageSynthesizer
    {
        private readonly bool shouldProduceCoverage;

        public MockLineCoverageSynthesizer(bool shouldProduceCoverage = true)
        {
            this.shouldProduceCoverage = shouldProduceCoverage;
        }

        public string ConvertToLineCoverageReport(ParsedTraceFile traceFile, string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            if (shouldProduceCoverage)
            {
                return "file1.cs\n12-33";
            }
            return null;
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
