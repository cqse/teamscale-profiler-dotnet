using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
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

    private static readonly Config config = new Config()
    {
        VersionAssembly = VersionAssembly
    };

    [Test]
    public void TestSuccessfulUpload()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050" },
        });

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(true), fileSystem).Run();

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
Inlined=1:33555646:100678050" },
        });

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(false), fileSystem).Run();

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
Inlined=1:33555646:100678050" },
        });

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(false), fileSystem).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);

        Assert.That(files, Is.EquivalentTo(new string[] {
            FileInTraceDirectory(@"missing-version\coverage_1_1.txt"),
        }));
    }

    [Test]
    public void TestEmptyTrace()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0" },
        });

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(true), fileSystem).Run();

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

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(false), fileSystem).Run();

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

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(false), fileSystem).Run();

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
Inlined=1:33555646:100678050
Stopped=1234556" },
        });

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectoryWithSpace, config, new MockUpload(true), fileSystem).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectoryWithSpace, "*.txt", SearchOption.AllDirectories);

        Assert.That(files, Is.EquivalentTo(new string[] {
            FileInTraceDirectoryWithSpace(@"uploaded\coverage_1_1.txt"),
        }));
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