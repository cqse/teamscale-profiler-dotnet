using System;
using System.Collections.Generic;
using UploadDaemon.Report;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Upload
{
    /// <summary>
    /// Merges all line coverage that will be uploaded for the same timestamp/revision and upload destination into one "batch".
    /// This allows uploading the coverage in one go instead of splitting it into multiple requests and thus
    /// commits in Teamscale.
    /// </summary>
    public class LineCoverageMerger
    {
        /// <summary>
        /// Key to a dictionary holding all batches of coverage.
        /// Each key identifies a destination to which coverage should be uploaded.
        /// Coverage that has the same destination, i.e. the same MergeKey, will
        /// be merged.
        /// </summary>
        private class MergeKey
        {
            /// <summary>
            /// Revision or timestamp to which the coverage should be uploaded.
            /// </summary>
            public RevisionFileUtils.RevisionOrTimestamp RevisionOrTimestamp;

            /// <summary>
            /// Key provided by the IUploader to group coverage with the same destination.
            /// </summary>
            public object UploadDictionaryKey;

            /// <summary>
            /// The type of IUploader to use for the coverage.
            /// </summary>
            public Type UploadType;

            public override bool Equals(object other) =>
                other is MergeKey key && key.RevisionOrTimestamp.Equals(RevisionOrTimestamp)
                && key.UploadType.Equals(UploadType) && key.UploadDictionaryKey.Equals(UploadDictionaryKey);

            public override int GetHashCode() =>
                (RevisionOrTimestamp, UploadType, UploadDictionaryKey).GetHashCode();
        }

        /// <summary>
        /// Groups all line coverage that should be uploaded in one batch.
        /// </summary>
        public class CoverageBatch
        {
            /// <summary>
            /// The revision or timestamp to which the coverage should be uploaded.
            /// </summary>
            public RevisionFileUtils.RevisionOrTimestamp RevisionOrTimestamp { get; }

            /// <summary>
            /// The upload to use for this batch.
            /// </summary>
            public IUpload Upload { get; }

            /// <summary>
            /// The aggregated coverage report in this batch.
            /// </summary>
            public ICoverageReport AggregatedCoverageReport;

            /// <summary>
            /// The original trace files from which the line coverage was generated.
            /// </summary>
            public List<string> TraceFilePaths { get; } = new List<string>();

            public CoverageBatch(IUpload upload, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)
            {
                this.Upload = upload;
                this.RevisionOrTimestamp = revisionOrTimestamp;
            }
        }

        private readonly Dictionary<MergeKey, CoverageBatch> mergedCoverage = new Dictionary<MergeKey, CoverageBatch>();

        /// <summary>
        /// Adds the given line coverage from the given file, that should be uploaded to the
        /// given revision/timestamp to the given upload to the merger. The coverage will be merged with
        /// any existing coverage that should be uploaded to the same destination.
        /// </summary>
        public void AddLineCoverage(string traceFilePath, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp, IUpload upload, ICoverageReport coverageReport)
        {
            MergeKey key = new MergeKey
            {
                RevisionOrTimestamp = revisionOrTimestamp,
                UploadDictionaryKey = upload.GetTargetId(),
                UploadType = upload.GetType()
            };

            if (!mergedCoverage.TryGetValue(key, out CoverageBatch batch))
            {
                batch = new CoverageBatch(upload, key.RevisionOrTimestamp);
                batch.AggregatedCoverageReport = coverageReport;
                mergedCoverage[key] = batch;
            }
            else
            {
                batch.AggregatedCoverageReport = batch.AggregatedCoverageReport.UnionWith(coverageReport);
            }

            batch.TraceFilePaths.Add(traceFilePath);
        }

        /// <summary>
        /// Returns all batches the merger has created.
        /// </summary>
        public IEnumerable<CoverageBatch> GetBatches()
        {
            return mergedCoverage.Values;
        }
    }
}
