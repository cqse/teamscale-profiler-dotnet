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
            {  File("coverage_1_1.txt"), new MockFileData(@"
Assembly=VersionAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050
") },
            { File("coverage_1_2.txt"), new MockFileData(@"
Assembly=VersionAssembly:1 Version:4.0.0.0
") },
            {  File("coverage_1_3.txt"), new MockFileData(@"
Assembly=OtherAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050
") },
            {  File("unrelated.txt"), new MockFileData(@"
Assembly=VersionAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050
") },
        });

        IEnumerable<TraceFileScanner.ScannedFile> files = new TraceFileScanner(TRACE_DIRECTORY, VERSION_ASSEMBLY, fileSystem).ListTraceFilesReadyForUpload();

        files.Should().HaveCount(2).And.Contain(new TraceFileScanner.ScannedFile[]
        {
            new TraceFileScanner.ScannedFile()
            {
                FilePath = File("coverage_1_1.txt"),
                Version = "4.0.0.0",
            },
            new TraceFileScanner.ScannedFile()
            {
                FilePath = File("coverage_1_3.txt"),
                Version = null,
            }
        });
    }

    [TestMethod]
    public void ExceptionsShouldLeadToFileBeingIgnored()
    {
        Mock<IFileSystem> fileSystemMock = new Mock<IFileSystem>();
        Mock<FileBase> fileMock = new Mock<FileBase>();
        Mock<DirectoryBase> directoryMock = new Mock<DirectoryBase>();

        fileMock.Setup(file => file.ReadAllLines("coverage_1_1.txt")).Throws<IOException>();
        fileMock.Setup(file => file.ReadAllLines("coverage_1_2.txt")).Returns(new string[] {
            "Assembly=VersionAssembly:1 Version:4.0.0.0",
            "Inlined=1:33555646:100678050",
        });
        directoryMock.Setup(directory => directory.EnumerateFiles(It.IsAny<string>()))
            .Returns(new string[] { "coverage_1_1.txt", "coverage_1_2.txt" });

        fileSystemMock.Setup(fileSystem => fileSystem.File).Returns(fileMock.Object);
        fileSystemMock.Setup(fileSystem => fileSystem.Directory).Returns(directoryMock.Object);

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
    private string File(string fileName)
    {
        return Path.Combine(TRACE_DIRECTORY, fileName);
    }

    public class ThrowingMockFileData : MockFileData
    {
        public ThrowingMockFileData() : base("")
        {
            // nothing to do
        }

        new byte[] Contents
        {
            get
            {
                throw new IOException("IO failed");
            }

            set
            {
                throw new IOException("IO failed");
            }
        }
    }
}

