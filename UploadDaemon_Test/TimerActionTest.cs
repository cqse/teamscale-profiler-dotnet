using System;
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

    private static readonly Config configWithSpace = Config.Read($@"
        match:
          - uploader:
              versionAssembly: {VersionAssembly}
              directory: C:\store
            profiler:
              targetdir: {TraceDirectoryWithSpace}
    ");

    [Test]
    public void TestSuccessfulUpload()

    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646:100678050" },
        });

        new TimerAction(config, fileSystem, new MockUploadFactory(true)).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);

        Assert.That(files, Is.EquivalentTo(new string[] {
            FileInTraceDirectory(@"uploaded\coverage_1_1.txt"),
        }));
    }

    [Test]
    public void TestFailedUpload()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646:100678050" },
        });

        new TimerAction(config, fileSystem, new MockUploadFactory(false)).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);

        Assert.That(files, Is.EquivalentTo(new string[] {
            FileInTraceDirectory(@"coverage_1_1.txt"),
        }));
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

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);

        Assert.That(files, Is.EquivalentTo(new string[] {
            FileInTraceDirectory(@"missing-version\coverage_1_1.txt"),
        }));
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

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);

        Assert.That(files, Is.EquivalentTo(new string[] {
            FileInTraceDirectory(@"missing-process\coverage_1_1.txt"),
        }));
    }

    [Test]
    public void TestEmptyTrace()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe" },
        });

        new TimerAction(config, fileSystem, new MockUploadFactory(true)).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);

        Assert.That(files, Is.EquivalentTo(new string[] {
            FileInTraceDirectory(@"empty-traces\coverage_1_1.txt"),
        }));
    }

    [Test]
    public void TestUnfinishedTrace()
    {
        FileSystemMockingUtils.FileSystemMock fileSystemMock = FileSystemMockingUtils.MockFileSystem(fileMock =>
        {
            fileMock.Setup(file => file.Open("coverage_1_1.txt", It.IsAny<FileMode>())).Throws<IOException>();
        }, directoryMock =>
        {
            directoryMock.Setup(directory => directory.EnumerateFiles(It.IsAny<string>()))
                .Returns(new string[] { "coverage_1_1.txt" });
        });
        IFileSystem fileSystem = fileSystemMock.Object;

        new TimerAction(config, fileSystem, new MockUploadFactory(false)).Run();

        fileSystemMock.FileMock.Verify(file => file.Open("coverage_1_1.txt", It.IsAny<FileMode>()));
        fileSystemMock.FileMock.VerifyNoOtherCalls();
    }

    [Test]
    public void TestUnrelatedFile()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("unrelated.txt"), @"foobar" },
        });

        new TimerAction(config, fileSystem, new MockUploadFactory(false)).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);

        Assert.That(files, Is.EquivalentTo(new string[] {
            FileInTraceDirectory(@"unrelated.txt"),
        }));
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

        new TimerAction(configWithSpace, fileSystem, new MockUploadFactory(true)).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectoryWithSpace, "*.txt", SearchOption.AllDirectories);

        Assert.That(files, Is.EquivalentTo(new string[] {
            FileInTraceDirectoryWithSpace(@"uploaded\coverage_1_1.txt"),
        }));
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
}