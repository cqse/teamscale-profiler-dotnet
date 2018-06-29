using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class TimerActionTest
{
    private const string TraceDirectory = @"C:\users\public\traces";
    private const string VersionAssembly = "VersionAssembly";
    private static readonly Config config = new Config()
    {
        VersionAssembly = VersionAssembly
    };

    [TestMethod]
    public void TestSuccessfulUpload()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050" },
        });

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(true), fileSystem).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);
        files.Should().HaveCount(1).And.Contain(new string[] {
            FileInTraceDirectory(@"uploaded\coverage_1_1.txt"),
        });
    }

    [TestMethod]
    public void TestFailedUpload()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050" },
        });

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(false), fileSystem).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);
        files.Should().HaveCount(1).And.Contain(new string[] {
            FileInTraceDirectory(@"coverage_1_1.txt"),
        });
    }

    [TestMethod]
    public void TestArchivingTraceWithMissingVersion()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=OtherAssembly:1 Version:4.0.0.0
Inlined=1:33555646:100678050" },
        });

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(false), fileSystem).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);
        files.Should().HaveCount(1).And.Contain(new string[] {
            FileInTraceDirectory(@"missing-version\coverage_1_1.txt"),
        });
    }

    [TestMethod]
    public void TestUnfinishedTrace()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("coverage_1_1.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0" },
        });

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(false), fileSystem).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);
        files.Should().HaveCount(1).And.Contain(new string[] {
            FileInTraceDirectory(@"coverage_1_1.txt"),
        });
    }

    [TestMethod]
    public void TestUnrelatedFile()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { FileInTraceDirectory("unrelated.txt"), @"Assembly=VersionAssembly:1 Version:4.0.0.0" },
        });

        config.VersionAssembly = VersionAssembly;

        new TimerAction(TraceDirectory, config, new MockUpload(false), fileSystem).Run();

        string[] files = fileSystem.Directory.GetFiles(TraceDirectory, "*.txt", SearchOption.AllDirectories);
        files.Should().HaveCount(1).And.Contain(new string[] {
            FileInTraceDirectory(@"unrelated.txt"),
        });
    }

    private class MockUpload : IUpload
    {
        private readonly bool returnValue;

        public MockUpload(bool returnValue)
        {
            this.returnValue = returnValue;
        }

        /// <summary>
        /// Fakes an upload and returns the result passed to the constructor.
        /// </summary>
        public Task<bool> UploadAsync(string filePath, string version)
        {
            return Task.FromResult(returnValue);
        }
    }

    /// <summary>
    /// Returns a file with the given name in the trace directory.
    /// </summary>
    private string FileInTraceDirectory(string fileName)
    {
        return Path.Combine(TraceDirectory, fileName);
    }

}

