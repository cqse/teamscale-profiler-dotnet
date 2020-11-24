using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Upload;

namespace UploadDaemon.Upload
{
    [TestFixture]
    public class LineCoverageMergerTest
    {
        private static readonly RevisionFileUtils.RevisionOrTimestamp Revision1 = new RevisionFileUtils.RevisionOrTimestamp
        {
            IsRevision = true,
            Value = "rev1"
        };

        private static readonly RevisionFileUtils.RevisionOrTimestamp Revision2 = new RevisionFileUtils.RevisionOrTimestamp
        {
            IsRevision = true,
            Value = "rev2"
        };

        private static IUpload Upload;

        [SetUp]
        public void SetUpMocks()
        {
            var uploadMock = new Mock<IUpload>();
            uploadMock.Setup(upload => upload.GetTargetId()).Returns("constant-string");
            Upload = uploadMock.Object;
        }

        [Test]
        public void TestMergingOfEqualRanges()
        {
            LineCoverageMerger merger = new LineCoverageMerger();
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((10, 20)) }
        }));
            merger.AddLineCoverage("trace2.txt", Revision1, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((10, 20)) }
        }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(1), "Number of batches");
            Assert.That(batches.First().LineCoverage.FileNames, Is.EquivalentTo(new string[] { "file1.cs" }), "Files");
            Assert.That(batches.First().LineCoverage["file1.cs"], Is.EqualTo(new FileCoverage((10, 20))), "Covered line ranges");
        }

        [Test]
        public void TestMergingOfUnequalRanges()
        {
            LineCoverageMerger merger = new LineCoverageMerger();
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((10, 20)) }
        }));
            merger.AddLineCoverage("trace2.txt", Revision1, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((30, 40)) }
        }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(1), "Number of batches");
            Assert.That(batches.First().LineCoverage.FileNames, Is.EquivalentTo(new string[] { "file1.cs" }), "Files");
            Assert.That(batches.First().LineCoverage["file1.cs"], Is.EqualTo(new FileCoverage((10, 20), (30, 40))), "Covered line ranges");
        }

        /// <summary>
        /// We decided for the moment not to implement merging overlapping line regions as we estimate
        /// that this doesn't happen very often (compared to the total amount of line ranges) in real
        /// world scenarios. Teamscale will take care of merging these when processing the generated report.
        /// </summary>
        [Test]
        public void TestMergingOfOverlappingRanges()
        {
            LineCoverageMerger merger = new LineCoverageMerger();
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((10, 20)) }
        }));
            merger.AddLineCoverage("trace2.txt", Revision1, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((15, 17)) }
        }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(1), "Number of batches");
            Assert.That(batches.First().LineCoverage.FileNames, Is.EquivalentTo(new string[] { "file1.cs" }), "Files");
            Assert.That(batches.First().LineCoverage["file1.cs"], Is.EqualTo(new FileCoverage((10, 20), (15, 17))), "Covered line ranges");
        }

        [Test]
        public void TestMergingOfDifferentFiles()
        {
            LineCoverageMerger merger = new LineCoverageMerger();
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((10, 20)) }
        }));
            merger.AddLineCoverage("trace2.txt", Revision1, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file2.cs",  new FileCoverage((10, 20)) }
        }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(1), "Number of batches");
            Assert.That(batches.First().LineCoverage.FileNames, Is.EquivalentTo(new string[] { "file1.cs", "file2.cs" }), "Files");
            Assert.That(batches.First().LineCoverage["file1.cs"], Is.EqualTo(new FileCoverage((10, 20))), "Covered line ranges");
            Assert.That(batches.First().LineCoverage["file2.cs"], Is.EqualTo(new FileCoverage((10, 20))), "Covered line ranges");
        }

        [Test]
        public void ShouldNotMergeWhenRevisionIsDifferent()
        {
            LineCoverageMerger merger = new LineCoverageMerger();
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((10, 20)) }
        }));
            merger.AddLineCoverage("trace2.txt", Revision2, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((10, 20)) }
        }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(2), "Number of batches");
        }

        [Test]
        public void ShouldNotMergeWhenUploadIsDifferent()
        {
            var uploadMock = new Mock<IUpload>();
            uploadMock.Setup(upload => upload.GetTargetId()).Returns("otherTargetId");
            IUpload uploadWithDifferentTarget = uploadMock.Object;

            LineCoverageMerger merger = new LineCoverageMerger();
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((10, 20)) }
        }));
            merger.AddLineCoverage("trace2.txt", Revision1, uploadWithDifferentTarget, new LineCoverageReport(new Dictionary<string, FileCoverage>()
        {
            { "file1.cs",  new FileCoverage((10, 20)) }
        }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(2), "Number of batches");
        }
    }

}