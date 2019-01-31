﻿using Common;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

[TestFixture]
public class UploadDaemonSystemTest
{
    private static string TargetDir => Path.Combine(TestUtils.TestTempDirectory, "targetdir");
    private static string UploadDir => Path.Combine(TestUtils.TestTempDirectory, "upload");

    [SetUp]
    public void CreateTemporaryTestDir()
    {
        var testDir = new DirectoryInfo(TestUtils.TestTempDirectory);
        if (testDir.Exists)
        {
            testDir.Delete(true);
        }

        testDir.Create();
        new DirectoryInfo(TargetDir).Create();
        new DirectoryInfo(UploadDir).Create();
    }

    [Test]
    public void TestSimpleDirectoryUpload()
    {
        string coverageFileName = "coverage_1_1.txt";
        File.WriteAllText(Path.Combine(TargetDir, coverageFileName), @"Assembly=VersionAssembly:1 Version:4.0.0.0
Process=foo.exe
Inlined=1:33555646:100678050");

        new UploadDaemon.UploadDaemon(Config.Read($@"
            match:
              - profiler:
                  targetdir: {TargetDir}
                uploader:
                  versionAssembly: VersionAssembly
                  directory: {UploadDir}
        ")).UploadOnce();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(UploadDir, coverageFileName)), Is.True, "file was uploaded successfully");
            Assert.That(File.Exists(Path.Combine(TargetDir, coverageFileName)), Is.False, "file was removed from profiler output dir");
            Assert.That(File.Exists(Path.Combine(TargetDir, "uploaded", coverageFileName)), Is.False, "file was archived");
        });
    }
}