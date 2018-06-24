using NLog;
using System;
using System.IO;
using System.IO.Abstractions;

public class Archiver
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private readonly IFileSystem fileSystem;
    private readonly string uploadedDirectory;
    private readonly string missingVersionDirectory;

    public Archiver(string traceDirectory, IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
        this.uploadedDirectory = Path.Combine(traceDirectory, "uploaded");
        this.missingVersionDirectory = Path.Combine(traceDirectory, "missing-version");
    }

    public void ArchiveUploadedFile(string tracePath)
    {
        Archive(tracePath, uploadedDirectory);
    }

    public void ArchiveFileWithoutVersionAssembly(string tracePath)
    {
        Archive(tracePath, missingVersionDirectory);
    }

    private void Archive(string tracePath, string targetDirectory)
    {
        string targetPath = Path.Combine(targetDirectory, Path.GetFileName(tracePath));
        try
        {
            fileSystem.File.Move(tracePath, targetPath);
        }
        catch (Exception e)
        {
            logger.Error(e, "Unable to archive {tracePath} to {archivePath}. The file will remain there" +
                " which may lead to it being uploaded multiple times", tracePath, targetDirectory);
        }
    }
}
