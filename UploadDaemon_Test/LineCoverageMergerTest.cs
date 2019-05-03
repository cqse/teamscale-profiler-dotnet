using Common;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using UploadDaemon;
using UploadDaemon.Upload;
using UploadDaemon.SymbolAnalysis;

[TestFixture]
public class LineCoverageMergerTest
{
    private static readonly RevisionFileUtils.RevisionOrTimestamp Revision = new RevisionFileUtils.RevisionOrTimestamp
    {
        IsRevision = true,
        Value = "rev"
    };

    private static readonly IUpload Upload = new MockUpload(true);

    [Test]
    public void TestMergingOfEqualRanges()
    {
        string sourceFile = "file1.cs";
        (uint, uint) lineRange = (10, 20);

        LineCoverageMerger merger = new LineCoverageMerger();
        merger.AddLineCoverage("trace1.txt", Revision, Upload, new Dictionary<string, FileCoverage>()
        {
            { sourceFile,  new FileCoverage(lineRange) }
        });
        merger.AddLineCoverage("trace2.txt", Revision, Upload, new Dictionary<string, FileCoverage>()
        {
            { sourceFile,  new FileCoverage(lineRange) }
        });

        IEnumerable<LineCoverageMerger.CoverageBatch> batches = merger.GetBatches();
        Assert.That(batches, Has.Count.EqualTo(1), "Number of batches");
        Assert.That(batches.First().LineCoverage.Keys, Is.EquivalentTo(new string[] { sourceFile }), "Files");
        Assert.That(batches.First().LineCoverage.Values, Is.EquivalentTo(
            new FileCoverage[] { new FileCoverage(lineRange) }), "Covered line ranges");
    }
}
