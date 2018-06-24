using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TraceFileScannerTest
{
    private const string TRACE_DIRECTORY = @"C:\users\public\traces";
    private const string VERSION_ASSEMBLY = "VersionAssembly";

    [TestMethod]
    public void TestAllScenarios()
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

    /// <summary>
    /// Returns a file with the given name in the trace directory.
    /// </summary>
    private string File(string fileName)
    {
        return Path.Combine(TRACE_DIRECTORY, fileName);
    }
}

