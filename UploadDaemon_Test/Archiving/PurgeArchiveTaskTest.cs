using Common;
using Moq;
using NUnit.Framework;
using System;

namespace UploadDaemon.Archiving
{
    [TestFixture]
    class PurgeArchiveTaskTest
    {
        [Test]
        public void ShouldNotPurgeAnythingIfDisabledInConfig()
        {
            var archive = new Mock<IArchive>();
            var archiveFactory = new Mock<IArchiveFactory>();
            archiveFactory.Setup(f => f.CreateArchive(@"c:\tracepath")).Returns(archive.Object);
            var config = Config.Read(@"
                match:
                    - profiler:
                        targetdir: c:\tracepath
            ");

            new PurgeArchiveTask(archiveFactory.Object).Run(config);

            archive.VerifyNoOtherCalls();
        }

        [Test]
        public void ShouldPurgeUploadedFiles()
        {
            var archive = new Mock<IArchive>();
            var archiveFactory = new Mock<IArchiveFactory>();
            archiveFactory.Setup(f => f.CreateArchive(@"c:\tracepath")).Returns(archive.Object);
            var config = Config.Read(@"
                archivePurgingThresholdsInDays:
                  uploadedTraces: 1
                match:
                    - profiler:
                        targetdir: c:\tracepath
            ");

            new PurgeArchiveTask(archiveFactory.Object).Run(config);

            archive.Verify(a => a.PurgeUploadedFiles(TimeSpan.FromDays(1)));
        }
    }
}
