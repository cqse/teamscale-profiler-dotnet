using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UploadDaemon.Report.Simple;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Upload
{
    [TestFixture]
    public class LineCoverageMergerTest
    {
        private static readonly RevisionFileUtils.RevisionOrTimestamp Revision1 = new RevisionFileUtils.RevisionOrTimestamp("rev1", false);

        private static readonly RevisionFileUtils.RevisionOrTimestamp Revision2 = new RevisionFileUtils.RevisionOrTimestamp("rev2", true);

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
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20)) }
            }));
            merger.AddLineCoverage("trace2.txt", Revision1, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20)) }
            }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(1), "Number of batches");
            Assert.That(batches.First().AggregatedCoverageReport.ToString(), Is.EqualTo(new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20)) }
            }).ToString()));
        }

        [Test]
        public void TestMergingOfUnequalRanges()
        {
            LineCoverageMerger merger = new LineCoverageMerger();
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20)) }
            }));
            merger.AddLineCoverage("trace2.txt", Revision1, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((30, 40)) }
            }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(1), "Number of batches");
            Assert.That(batches.First().AggregatedCoverageReport.ToString(), Is.EqualTo(new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20), (30,40)) }
            }).ToString()));
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
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20)) }
            }));
            merger.AddLineCoverage("trace2.txt", Revision1, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((15, 17)) }
            }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(1), "Number of batches");
            Assert.That(batches.First().AggregatedCoverageReport.ToString(), Is.EqualTo(new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20), (15, 17)) }
            }).ToString()));
        }

        [Test]
        public void TestMergingOfDifferentFiles()
        {
            LineCoverageMerger merger = new LineCoverageMerger();
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20)) }
            }));
            merger.AddLineCoverage("trace2.txt", Revision1, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file2.cs",  new FileCoverage((10, 20)) }
            }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(1), "Number of batches");
            Assert.That(batches.First().AggregatedCoverageReport.ToString(), Is.EqualTo(new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20)) },
                { "file2.cs",  new FileCoverage((10, 20)) },
            }).ToString()));
        }

        [Test]
        public void ShouldNotMergeWhenRevisionIsDifferent()
        {
            LineCoverageMerger merger = new LineCoverageMerger();
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20)) }
            }));
            merger.AddLineCoverage("trace2.txt", Revision2, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
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
            merger.AddLineCoverage("trace1.txt", Revision1, Upload, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20)) }
            }));
            merger.AddLineCoverage("trace2.txt", Revision1, uploadWithDifferentTarget, new SimpleCoverageReport(new Dictionary<string, FileCoverage>()
            {
                { "file1.cs",  new FileCoverage((10, 20)) }
            }));

            IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
            Assert.That(batches, Has.Count.EqualTo(2), "Number of batches");
        }
    }

}