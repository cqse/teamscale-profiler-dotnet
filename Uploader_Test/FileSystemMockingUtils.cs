using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Utility functions for mocking System.IO.Abstractions classes.
/// </summary>
static class FileSystemMockingUtils
{
    /// <summary>
    /// Mocks an IFileSystem's File and Directory properties.
    /// </summary>
    public static Mock<IFileSystem> MockFileSystem(Action<Mock<FileBase>> fileMocker, Action<Mock<DirectoryBase>> directoryMocker)
    {
        Mock<IFileSystem> fileSystemMock = new Mock<IFileSystem>();
        Mock<FileBase> fileMock = new Mock<FileBase>();
        Mock<DirectoryBase> directoryMock = new Mock<DirectoryBase>();

        fileMocker(fileMock);
        directoryMocker(directoryMock);

        fileSystemMock.Setup(fileSystem => fileSystem.File).Returns(fileMock.Object);
        fileSystemMock.Setup(fileSystem => fileSystem.Directory).Returns(directoryMock.Object);
        return fileSystemMock;
    }
}
