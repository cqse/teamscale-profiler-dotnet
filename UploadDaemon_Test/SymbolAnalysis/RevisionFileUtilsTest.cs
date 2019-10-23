using NUnit.Framework;

namespace UploadDaemon.SymbolAnalysis
{
    [TestFixture]
    public class RevisionFileUtilsTest
    {
        [Test]
        public void TestTimestamp()
        {
            RevisionFileUtils.RevisionOrTimestamp result = RevisionFileUtils.Parse(
                new string[] { "timestamp: 1234" }, "rev.txt");
            Assert.Multiple(() =>
            {
                Assert.That(result.IsRevision, Is.False, "is timestamp");
                Assert.That(result.Value, Is.EqualTo("1234"), "value");
            });
        }

        [Test]
        public void TestRevision()
        {
            RevisionFileUtils.RevisionOrTimestamp result = RevisionFileUtils.Parse(
                new string[] { "revision: 1234" }, "rev.txt");
            Assert.Multiple(() =>
            {
                Assert.That(result.IsRevision, Is.True, "is revision");
                Assert.That(result.Value, Is.EqualTo("1234"), "value");
            });
        }

        [Test]
        public void TimestampMustBeCaseInsensitive()
        {
            RevisionFileUtils.RevisionOrTimestamp result = RevisionFileUtils.Parse(
                new string[] { "TimeStamP: 1234" }, "rev.txt");
            Assert.Multiple(() =>
            {
                Assert.That(result.IsRevision, Is.False, "is timestamp");
                Assert.That(result.Value, Is.EqualTo("1234"), "value");
            });
        }

        [Test]
        public void RevisionMustBeCaseInsensitive()
        {
            RevisionFileUtils.RevisionOrTimestamp result = RevisionFileUtils.Parse(
                new string[] { "ReViSion: 1234" }, "rev.txt");
            Assert.Multiple(() =>
            {
                Assert.That(result.IsRevision, Is.True, "is revision");
                Assert.That(result.Value, Is.EqualTo("1234"), "value");
            });
        }

        [Test]
        public void WhitespaceMustNotMatter()
        {
            RevisionFileUtils.RevisionOrTimestamp result = RevisionFileUtils.Parse(
                new string[] { "\t\trevision: \t1234  " }, "rev.txt");
            Assert.Multiple(() =>
            {
                Assert.That(result.IsRevision, Is.True, "is revision");
                Assert.That(result.Value, Is.EqualTo("1234"), "value");
            });
        }

        [Test]
        public void NonMatchingLinesMustBeIgnored()
        {
            RevisionFileUtils.RevisionOrTimestamp result = RevisionFileUtils.Parse(
                new string[] { "", "revision: 1234", "\t ", "# comment" }, "rev.txt");
            Assert.Multiple(() =>
            {
                Assert.That(result.IsRevision, Is.True, "is revision");
                Assert.That(result.Value, Is.EqualTo("1234"), "value");
            });
        }
    }
}