using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;

[TestFixture]
public class TraceFileScannerTest
{
    private const string TraceDirectory = @"C:\users\public\traces";
    private const string VersionAssembly = "VersionAssembly";

    [Test]
    public void TestAllFileContents()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            // finished trace
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050" },
            // empty trace
            { FileInTraceDirectory("coverage_1_2.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0" },
            // no version assembly
            { FileInTraceDirectory("coverage_1_3.txt"), @"Assembly=OtherAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050" },
            // unrelated file
            { FileInTraceDirectory("unrelated.txt"), @"whatever" },
        });

        List<TraceFileScanner.ScannedFile> files = new TraceFileScanner(TraceDirectory, VersionAssembly, fileSystem).ListTraceFilesReadyForUpload().ToList();

        Assert.That(files, Is.EquivalentTo(new TraceFileScanner.ScannedFile[] {
            new TraceFileScanner.ScannedFile()
            {
                FilePath = FileInTraceDirectory("coverage_1_1.txt"),
                Version = "4.0.0.0",
                IsEmpty = false,
            },
            new TraceFileScanner.ScannedFile()
            {
                FilePath = FileInTraceDirectory("coverage_1_2.txt"),
                Version = "4.0.0.0",
                IsEmpty = true,
            },
            new TraceFileScanner.ScannedFile()
            {
                FilePath = FileInTraceDirectory("coverage_1_3.txt"),
                Version = null,
                IsEmpty = false,
            },
        }));
    }

    [Test]
    public void LockedFileShouldBeIgnored()
    {
        IFileSystem fileSystemMock = FileSystemMockingUtils.MockFileSystem(fileMock =>
        {
            fileMock.Setup(file => file.Open("coverage_1_1.txt", It.IsAny<FileMode>())).Throws<IOException>();
        }, directoryMock =>
        {
            directoryMock.Setup(directory => directory.EnumerateFiles(It.IsAny<string>()))
                .Returns(new string[] { "coverage_1_1.txt" });
        }).Object;

        List<TraceFileScanner.ScannedFile> files =
            new TraceFileScanner(TraceDirectory, VersionAssembly, fileSystemMock).ListTraceFilesReadyForUpload().ToList();

        Assert.That(files, Is.Empty);
    }

    [Test]
    public void ExceptionsShouldLeadToFileBeingIgnored()
    {
        IFileSystem fileSystemMock = FileSystemMockingUtils.MockFileSystem(fileMock =>
        {
            fileMock.Setup(file => file.ReadAllLines("coverage_1_1.txt")).Throws<IOException>();
            fileMock.Setup(file => file.ReadAllLines("coverage_1_2.txt")).Returns(new string[] {
                "Assembly=VersionAssembly:1 Version:4.0.0.0",
                "Inlined=1:33555646:100678050",
            });
        }, directoryMock =>
        {
            directoryMock.Setup(directory => directory.EnumerateFiles(It.IsAny<string>()))
                .Returns(new string[] { "coverage_1_1.txt", "coverage_1_2.txt" });
        }).Object;

        List<TraceFileScanner.ScannedFile> files =
            new TraceFileScanner(TraceDirectory, VersionAssembly, fileSystemMock).ListTraceFilesReadyForUpload().ToList();

        Assert.That(files, Is.EquivalentTo(new TraceFileScanner.ScannedFile[] {
            new TraceFileScanner.ScannedFile()
            {
                FilePath = "coverage_1_2.txt",
                Version = "4.0.0.0",
                IsEmpty = false,
            },
        }));
    }

    /// <summary>
    /// Returns a file with the given name in the trace directory.
    /// </summary>
    private string FileInTraceDirectory(string fileName)
    {
        return Path.Combine(TraceDirectory, fileName);
    }
}