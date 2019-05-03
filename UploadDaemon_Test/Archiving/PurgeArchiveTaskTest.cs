using Common;
using Moq;
using NUnit.Framework;
using System;

namespace UploadDaemon.Archiving
{
    [TestFixture]
    class PurgeArchiveTaskTest
    {
        const string TestArchiveDirectoryPath = @"c:\tracepath";

        Mock<IArchive> archive;
        Mock<IArchiveFactory> archiveFactory;

        [SetUp]
        public void SetUp()
        {
            archive = new Mock<IArchive>();
            archiveFactory = new Mock<IArchiveFactory>();
            archiveFactory.Setup(f => f.CreateArchive(TestArchiveDirectoryPath)).Returns(archive.Object);
        }

        [Test]
        public void ShouldNotPurgeAnythingIfDisabledInConfig()
        {
            var config = CreateMinimalValidConfigWithPurgingThresholdsSection("");

            new PurgeArchiveTask(archiveFactory.Object).Run(config);

            archive.VerifyNoOtherCalls();
        }

        [Test]
        public void ShouldPurgeUploadedFiles()
        {
            var config = CreateMinimalValidConfigWithPurgingThresholdsSection(@"
                archivePurgingThresholdsInDays:
                  uploadedTraces: 1
            ");

            new PurgeArchiveTask(archiveFactory.Object).Run(config);

            archive.Verify(a => a.PurgeUploadedFiles(TimeSpan.FromDays(1)));
        }

        private Config CreateMinimalValidConfigWithPurgingThresholdsSection(string purgingThresholdsSection)
        {
            return Config.Read($@"
                {purgingThresholdsSection}
                match:
                    - profiler:
                        targetdir: {TestArchiveDirectoryPath}
            ");
        }
    }
}
