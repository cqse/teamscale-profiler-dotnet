using NUnit.Framework;

namespace UploadDaemon.SymbolAnalysis
{
    [TestFixture]
    public class RevisionFileUtilsTest
    {
        [Test]
        public void TestTimestamp()
        {
            RevisionFileUtils.RevisionAndTimestamp result = RevisionFileUtils.Parse(
                new string[] { "timestamp: 1234" }, "rev.txt");

            Assert.That(result.TimestampValue, Is.EqualTo("1234"), "value");
        }

        [Test]
        public void TestRevision()
        {
            RevisionFileUtils.RevisionAndTimestamp result = RevisionFileUtils.Parse(
                new string[] { "revision: 1234" }, "rev.txt");
            Assert.That(result.RevisionValue, Is.EqualTo("1234"), "value");
        }

        [Test]
        public void TimestampMustBeCaseInsensitive()
        {
            RevisionFileUtils.RevisionAndTimestamp result = RevisionFileUtils.Parse(
                new string[] { "TimeStamP: 1234" }, "rev.txt");
            Assert.That(result.TimestampValue, Is.EqualTo("1234"), "value");
        }

        [Test]
        public void RevisionMustBeCaseInsensitive()
        {
            RevisionFileUtils.RevisionAndTimestamp result = RevisionFileUtils.Parse(
                new string[] { "ReViSion: 1234" }, "rev.txt");
            Assert.That(result.RevisionValue, Is.EqualTo("1234"), "value");
        }

        [Test]
        public void WhitespaceMustNotMatter()
        {
            RevisionFileUtils.RevisionAndTimestamp result = RevisionFileUtils.Parse(
                new string[] { "\t\trevision: \t1234  " }, "rev.txt");
            Assert.That(result.RevisionValue, Is.EqualTo("1234"), "value");
        }

        [Test]
        public void NonMatchingLinesMustBeIgnored()
        {
            RevisionFileUtils.RevisionAndTimestamp result = RevisionFileUtils.Parse(
                new string[] { "", "revision: 1234", "\t ", "# comment" }, "rev.txt");
            Assert.That(result.RevisionValue, Is.EqualTo("1234"), "value");
        }
    }
}