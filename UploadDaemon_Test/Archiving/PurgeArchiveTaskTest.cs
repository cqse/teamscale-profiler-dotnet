using Moq;
using NUnit.Framework;
using System;
using UploadDaemon.Configuration;

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
            archive.VerifyNoOtherCalls();
        }

        [Test]
        public void ShouldPurgeEmptyFiles()
        {
            var config = CreateMinimalValidConfigWithPurgingThresholdsSection(@"
                archivePurgingThresholdsInDays:
                  emptyTraces: 2
            ");

            new PurgeArchiveTask(archiveFactory.Object).Run(config);

            archive.Verify(a => a.PurgeFilesWithoutLineCoverage(TimeSpan.FromDays(2)));
            archive.Verify(a => a.PurgeEmptyFiles(TimeSpan.FromDays(2)));
            archive.VerifyNoOtherCalls();
        }

        [Test]
        public void ShouldPurgeIncompleteFiles()
        {
            var config = CreateMinimalValidConfigWithPurgingThresholdsSection(@"
                archivePurgingThresholdsInDays:
                  incompleteTraces: 3
            ");

            new PurgeArchiveTask(archiveFactory.Object).Run(config);

            archive.Verify(a => a.PurgeFilesWithoutProcess(TimeSpan.FromDays(3)));
            archive.VerifyNoOtherCalls();
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
