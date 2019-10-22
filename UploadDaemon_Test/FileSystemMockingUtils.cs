using Moq;
using System;
using System.IO.Abstractions;

/// <summary>
/// Utility functions for mocking System.IO.Abstractions classes.
/// </summary>
internal static class FileSystemMockingUtils
{
    /// <summary>
    /// Encapsulates mocks for a file system and its relevant properties.
    /// </summary>
    internal class FileSystemMock
    {
        /// <summary>
        /// Mock of the file system.
        /// </summary>
        public Mock<IFileSystem> Mock { get; set; }

        /// <summary>
        /// The mocked file system.
        /// </summary>
        public IFileSystem Object { get; set; }

        /// <summary>
        /// Mock of the File property.
        /// </summary>
        public Mock<FileBase> FileMock { get; set; }

        /// <summary>
        /// Mock of the Directory property.
        /// </summary>
        public Mock<DirectoryBase> DirectoryMock { get; set; }
    }

    /// <summary>
    /// Mocks an IFileSystem's File and Directory properties.
    /// </summary>
    public static FileSystemMock MockFileSystem(Action<Mock<FileBase>> fileMocker, Action<Mock<DirectoryBase>> directoryMocker)
    {
        Mock<IFileSystem> fileSystemMock = new Mock<IFileSystem>();
        Mock<FileBase> fileMock = new Mock<FileBase>();
        Mock<DirectoryBase> directoryMock = new Mock<DirectoryBase>();

        fileMocker(fileMock);
        directoryMocker(directoryMock);

        fileSystemMock.Setup(fileSystem => fileSystem.File).Returns(fileMock.Object);
        fileSystemMock.Setup(fileSystem => fileSystem.Directory).Returns(directoryMock.Object);

        return new FileSystemMock()
        {
            Mock = fileSystemMock,
            Object = fileSystemMock.Object,
            FileMock = fileMock,
            DirectoryMock = directoryMock,
        };
    }
}