using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Common;
using Moq;
using NUnit.Framework;
using UploadDaemon;
using UploadDaemon.Upload;

[TestFixture]
public class TimerActionTest
{
    private const string TraceDirectory = @"C:\users\public\traces";
    private const string TraceDirectoryWithSpace = @"C:\users\user with spaces\traces";
    private const string VersionAssembly = "VersionAssembly";

    private static readonly Config config = Config.Read($@"
        match:
          - uploader:
              versionAssembly: {VersionAssembly}
              directory: C:\store
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

        new TimerAction(config, fileSystem, new MockUploadFactory(true)).Run();

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

        new TimerAction(config, fileSystem, new MockUploadFactory(false)).Run();

        AssertFilesInDirectory(fileSystem, TraceDirectory, @"coverage_1_1.txt");
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

        new TimerAction(config, fileSystem, new MockUploadFactory(false)).Run();

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

        new TimerAction(config, fileSystem, new MockUploadFactory(false)).Run();

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

        new TimerAction(config, fileSystem, new MockUploadFactory(true)).Run();

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

        new TimerAction(configWithSpaceInTraceDirectory, fileSystem, new MockUploadFactory(true)).Run();

        AssertFilesInDirectory(fileSystem, TraceDirectoryWithSpace, @"uploaded\coverage_1_1.txt");
    }

    private class MockUploadFactory : IUploadFactory
    {
        private readonly bool returnValue;

        public MockUploadFactory(bool returnValue)
        {
            this.returnValue = returnValue;
        }

        public IUpload CreateUpload(Config.ConfigForProcess config, IFileSystem fileSystem)
        {
            return new MockUpload(returnValue);
        }
    }

    private class MockUpload : IUpload
    {
        private readonly bool returnValue;

        public MockUpload(bool returnValue)
        {
            this.returnValue = returnValue;
        }

        /// <summary>
        /// Fakes an upload and returns the result passed to the constructor.
        /// </summary>
        public Task<bool> UploadAsync(string filePath, string version)
        {
            return Task.FromResult(returnValue);
        }

        public string Describe()
        {
            return "MockUpload";
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