using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using NUnit.Framework;

[TestFixture]
public class ArchiverTest
{
    private const string TraceDirectory = @"C:\users\public\traces";

    [Test]
    public void ShouldMoveFilesToCorrectSubfolders()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"uploaded" },
            { FileInTraceDirectory("coverage_1_2.txt"), @"missing version" },
        });

        new Archiver(TraceDirectory, fileSystem).ArchiveUploadedFile(FileInTraceDirectory("coverage_1_1.txt"));
        new Archiver(TraceDirectory, fileSystem).ArchiveFileWithoutVersionAssembly(FileInTraceDirectory("coverage_1_2.txt"));

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);

        Assert.AreEqual(2, files.Length, "Expecting 2 traces under the root directory");
        Assert.Contains(FileInTraceDirectory(@"uploaded\coverage_1_1.txt"), files);
        Assert.Contains(FileInTraceDirectory(@"missing-version\coverage_1_2.txt"), files);
    }

    [Test]
    public void ShouldHandleExceptionsGracefully()
    {
        IFileSystem fileSystemMock = FileSystemMockingUtils.MockFileSystem(fileMock =>
        {
            fileMock.Setup(file => file.ReadAllLines(FileInTraceDirectory("coverage_1_1.txt"))).Throws<IOException>();
        }, directoryMock =>
        {
            // not needed
        });

        new Archiver(TraceDirectory, fileSystemMock).ArchiveUploadedFile(FileInTraceDirectory("coverage_1_1.txt"));
    }

    /// <summary>
    /// Returns a file with the given name in the trace directory.
    /// </summary>
    private string FileInTraceDirectory(string fileName)
    {
        return Path.Combine(TraceDirectory, fileName);
    }
}