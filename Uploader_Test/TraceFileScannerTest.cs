using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Moq;
using NUnit.Framework;

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
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050" },
            { FileInTraceDirectory("coverage_1_2.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0" },
            { FileInTraceDirectory("coverage_1_3.txt"), @"Assembly=OtherAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050" },
            { FileInTraceDirectory("unrelated.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050" },
        });

        List<TraceFileScanner.ScannedFile> files = new TraceFileScanner(TraceDirectory, VersionAssembly, fileSystem).ListTraceFilesReadyForUpload().ToList();

        Assert.AreEqual(2, files.Count, "Expecting exactly 2 scanned files");
        Assert.Contains(new TraceFileScanner.ScannedFile()
        {
            FilePath = FileInTraceDirectory("coverage_1_1.txt"),
            Version = "4.0.0.0",
        }, files);
        Assert.Contains(new TraceFileScanner.ScannedFile()
        {
            FilePath = FileInTraceDirectory("coverage_1_3.txt"),
            Version = null,
        }, files);
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
       });

        List<TraceFileScanner.ScannedFile> files =
            new TraceFileScanner(TraceDirectory, VersionAssembly, fileSystemMock).ListTraceFilesReadyForUpload().ToList();

        Assert.AreEqual(1, files.Count, "Expecting exactly 1 scanned file");
        Assert.Contains(new TraceFileScanner.ScannedFile()
        {
            FilePath = "coverage_1_2.txt",
            Version = "4.0.0.0",
        }, files);
    }

    /// <summary>
    /// Returns a file with the given name in the trace directory.
    /// </summary>
    private string FileInTraceDirectory(string fileName)
    {
        return Path.Combine(TraceDirectory, fileName);
    }
}