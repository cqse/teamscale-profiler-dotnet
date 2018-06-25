using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class TraceFileScannerTest
{
    private const string TRACE_DIRECTORY = @"C:\users\public\traces";
    private const string VERSION_ASSEMBLY = "VersionAssembly";

    [TestMethod]
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

        IEnumerable<TraceFileScanner.ScannedFile> files = new TraceFileScanner(TRACE_DIRECTORY, VERSION_ASSEMBLY, fileSystem).ListTraceFilesReadyForUpload();

        files.Should().HaveCount(2).And.Contain(new TraceFileScanner.ScannedFile[]
        {
            new TraceFileScanner.ScannedFile()
            {
                FilePath = FileInTraceDirectory("coverage_1_1.txt"),
                Version = "4.0.0.0",
            },
            new TraceFileScanner.ScannedFile()
            {
                FilePath = FileInTraceDirectory("coverage_1_3.txt"),
                Version = null,
            }
        });
    }

    [TestMethod]
    public void ExceptionsShouldLeadToFileBeingIgnored()
    {
        Mock<IFileSystem> fileSystemMock = FileSystemMockingUtils.MockFileSystem(fileMock =>
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

        IEnumerable<TraceFileScanner.ScannedFile> files =
            new TraceFileScanner(TRACE_DIRECTORY, VERSION_ASSEMBLY, fileSystemMock.Object).ListTraceFilesReadyForUpload();

        files.Should().HaveCount(1).And.Contain(new TraceFileScanner.ScannedFile[]
        {
            new TraceFileScanner.ScannedFile()
            {
                FilePath = "coverage_1_2.txt",
                Version = "4.0.0.0",
            },
        });
    }

    /// <summary>
    /// Returns a file with the given name in the trace directory.
    /// </summary>
    private string FileInTraceDirectory(string fileName)
    {
        return Path.Combine(TRACE_DIRECTORY, fileName);
    }

}

