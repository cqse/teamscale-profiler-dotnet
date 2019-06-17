using System;

namespace UploadDaemon.Archiving
{
    /// <summary>
    /// A trace-file archive.
    /// </summary>
    public interface IArchive
    {
        /// <summary>
        /// Archives a file that was successfully uploaded.
        /// </summary>
        void ArchiveUploadedFile(string tracePath);

        /// <summary>
        /// Deletes all uploaded files that are older than the given maximum age from the archive.
        /// </summary>
        void PurgeUploadedFiles(TimeSpan maximumAge);

        /// <summary>
        /// Archives a file that has no version assembly.
        /// </summary>
        void ArchiveFileWithoutVersionAssembly(string tracePath);

        /// <summary>
        /// Deletes all files without version assembly that are older than the given maximum age from the archive.
        /// </summary>
        void PurgeFilesWithoutVersionAssembly(TimeSpan maximumAge);

        /// <summary>
        /// Archives a file that has no profiled process path.
        /// </summary>
        void ArchiveFileWithoutProcess(string tracePath);

        /// <summary>
        /// Archives a line coverage report for debugging.
        /// </summary>
        void ArchiveLineCoverage(string fileName, string lineCoverageReport);

        /// <summary>
        /// Deletes all files without a profiled process that are older than the given maximum age from the archive.
        /// </summary>
        void PurgeFilesWithoutProcess(TimeSpan maximumAge);

        /// <summary>
        /// Archives a file that has no coverage data (Jitted=, Inlined= lines).
        /// </summary>
        void ArchiveEmptyFile(string tracePath);

        /// <summary>
        /// Deletes all files without coverage that are older than the given maximum age from the archive.
        /// </summary>
        void PurgeEmptyFiles(TimeSpan maximumAge);

        /// <summary>
        /// Archives a file that, after being converted to line coverage, did not produce any coverage.
        /// </summary>
        void ArchiveFileWithoutLineCoverage(string tracePath);

        /// <summary>
        /// Deletes all files without line coverage that are older than the given maximum age from the archive.
        /// </summary>
        void PurgeFilesWithoutLineCoverage(TimeSpan maximumAge);
    }
}
