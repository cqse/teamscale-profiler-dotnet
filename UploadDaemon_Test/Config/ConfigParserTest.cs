using NUnit.Framework;
using System;
using UploadDaemon.Configuration;

namespace UploadDaemon
{
    [TestFixture]
    public class ConfigParserTest
    {
        [Test]
        public void TestParsing()
        {
            ConfigParser.YamlConfig config = ConfigParser.Parse(@"
                match:
                  - executableName: foo.exe
                    executablePathRegex: .*test.*
                    profiler:
                      foo: 1
                    uploader:
                      versionAssembly: Bla
            ");

            Assert.That(config, Is.Not.Null, "config not null");
            Assert.That(config.Match, Has.Count.EqualTo(1), "section count");
            Assert.Multiple(() =>
            {
                Assert.That(config.Match[0].ExecutableName, Is.EqualTo("foo.exe"), "section 0 executable name");
                Assert.That(config.Match[0].ExecutablePathRegex, Is.EqualTo(".*test.*"), "section 0 executable regex");
                Assert.That(config.Match[0].Profiler, Has.Count.EqualTo(1), "section 0 profiler option count");
                Assert.That(config.Match[0].Profiler, Contains.Key("foo"), "section 0 profiler option 'foo'");
                Assert.That(config.Match[0].Profiler["foo"], Is.EqualTo("1"), "section 0 profiler option 'foo' value");
                Assert.That(config.Match[0].Uploader, Is.Not.Null, "section 0 uploader options");
                Assert.That(config.Match[0].Uploader.VersionAssembly, Is.EqualTo("Bla"), "section 0 version assembly");
            });
        }

        [Test]
        public void InvalidYaml()
        {
            Exception exception = Assert.Catch(() =>
            {
                ConfigParser.YamlConfig config = ConfigParser.Parse(@"uiae$1/#");
            });

            Assert.That(exception, Is.Not.Null);
        }

        [Test]
        public void TestMissingKeys()
        {
            ConfigParser.YamlConfig config = ConfigParser.Parse(@"
                match:
                  - executableName: foo.exe
                  - executablePathRegex: foo.exe
            ");

            Assert.That(config, Is.Not.Null, "config not null");
            Assert.That(config.Match, Has.Count.EqualTo(2), "section count");
            Assert.Multiple(() =>
            {
                Assert.That(config.Match[0].ExecutablePathRegex, Is.Null, "section 0 executable regex");
                Assert.That(config.Match[0].Uploader, Is.Not.Null, "section 0 uploader options");
                Assert.That(config.Match[0].Profiler, Is.Not.Null, "section 0 profiler options");
                Assert.That(config.Match[0].Profiler, Is.Empty, "section 0 profiler options");
                Assert.That(config.Match[1].ExecutableName, Is.Null, "section 1 executable name");
            });
        }

        [Test]
        public void TestMissingUploaderKeys()
        {
            ConfigParser.YamlConfig config = ConfigParser.Parse(@"
                match:
                  - uploader:
                      versionAssembly: foo
                  - uploader:
                      enabled: false
            ");

            Assert.That(config, Is.Not.Null, "config not null");
            Assert.That(config.Match, Has.Count.EqualTo(2), "section count");
            Assert.Multiple(() =>
            {
                Assert.That(config.Match[0].Uploader, Is.Not.Null, "section 0 uploader options");
                Assert.That(config.Match[0].Uploader.Enabled, Is.Null, "section 0 enabled");
                Assert.That(config.Match[0].Uploader.AzureFileStorage, Is.Null, "section 0 azure");
                Assert.That(config.Match[0].Uploader.Teamscale, Is.Null, "section 0 teamscale");
                Assert.That(config.Match[0].Uploader.VersionPrefix, Is.Null, "section 0 version prefix");
                Assert.That(config.Match[0].Uploader.Directory, Is.Null, "section 0 directory");
                Assert.That(config.Match[0].Uploader.PdbDirectory, Is.Null, "section 0 pdb directory");
                Assert.That(config.Match[0].Uploader.RevisionFile, Is.Null, "section 0 revision file");
                Assert.That(config.Match[0].Uploader.AssemblyPatterns, Is.Null, "section 0 assembly patterns");
                Assert.That(config.Match[0].Uploader.MergeLineCoverage, Is.Null, "section 0 merge line coverage");
                Assert.That(config.Match[1].Uploader.VersionAssembly, Is.Null, "section 1 version assembly");
                Assert.That(config.Match[1].Uploader.Enabled, Is.False, "section 1 enabled");
            });
        }
    }
}
