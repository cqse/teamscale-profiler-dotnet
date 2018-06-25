using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class ArchiverTest
{
    private const string TRACE_DIRECTORY = @"C:\users\public\traces";
    private const string VERSION_ASSEMBLY = "VersionAssembly";

    [TestMethod]
    public void ShouldMoveFilesToCorrectSubfolders()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"uploaded" },
            { FileInTraceDirectory("coverage_1_2.txt"), @"missing version" },
        });

        new Archiver(TRACE_DIRECTORY, fileSystem).ArchiveUploadedFile(FileInTraceDirectory("coverage_1_1.txt"));
        new Archiver(TRACE_DIRECTORY, fileSystem).ArchiveFileWithoutVersionAssembly(FileInTraceDirectory("coverage_1_2.txt"));

        string[] files = fileSystem.Directory.GetFiles(TRACE_DIRECTORY, "*.txt", SearchOption.AllDirectories);
        files.Should().HaveCount(2).And.Contain(new string[] {
            FileInTraceDirectory(@"uploaded\coverage_1_1.txt"),
            FileInTraceDirectory(@"missing-version\coverage_1_2.txt"),
        });
    }

    [TestMethod]
    public void ShouldHandleExceptionsGracefully()
    {

        Mock<IFileSystem> fileSystemMock = FileSystemMockingUtils.MockFileSystem(fileMock =>
        {
            fileMock.Setup(file => file.ReadAllLines(FileInTraceDirectory("coverage_1_1.txt"))).Throws<IOException>();
        }, directoryMock => { });
        
        new Archiver(TRACE_DIRECTORY, fileSystemMock.Object).ArchiveUploadedFile(FileInTraceDirectory("coverage_1_1.txt"));
    }

    /// <summary>
    /// Returns a file with the given name in the trace directory.
    /// </summary>
    private string FileInTraceDirectory(string fileName)
    {
        return Path.Combine(TRACE_DIRECTORY, fileName);
    }

}

