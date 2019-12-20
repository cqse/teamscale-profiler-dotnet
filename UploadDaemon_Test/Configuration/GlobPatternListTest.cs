using NUnit.Framework;
using System.Collections.Generic;

namespace UploadDaemon.Configuration
{
    [TestFixture]
    public class GlobPatternListTest
    {
        [Test]
        public void StarMatchesAnyNumberOfCharacters()
        {
            GlobPatternList pattern = new GlobPatternList(new List<string> { "foo*bar" }, new List<string> { });
            Assert.Multiple(() =>
            {
                Assert.That(pattern.Matches("foobar"), Is.True, "foobar");
                Assert.That(pattern.Matches("foo_bar"), Is.True, "foo_bar");
                Assert.That(pattern.Matches("foobarbar"), Is.True, "foobarbar");
                Assert.That(pattern.Matches("foofoobar"), Is.True, "foofoobar");
                Assert.That(pattern.Matches("foobarX"), Is.False, "foobarX");
            });
        }

        [Test]
        public void QuestionMarkMatchesOneCharacter()
        {
            GlobPatternList pattern = new GlobPatternList(new List<string> { "foo?bar" }, new List<string> { });
            Assert.Multiple(() =>
            {
                Assert.That(pattern.Matches("foobar"), Is.False, "foobar");
                Assert.That(pattern.Matches("foo_bar"), Is.True, "foo_bar");
                Assert.That(pattern.Matches("Xfoo_bar"), Is.False, "Xfoo_bar");
            });
        }

        [Test]
        public void ExcludesTrumpIncludes()
        {
            GlobPatternList pattern = new GlobPatternList(new List<string> { "foo*" }, new List<string> { "*bar" });
            Assert.Multiple(() =>
            {
                Assert.That(pattern.Matches("foobar"), Is.False, "foobar");
                Assert.That(pattern.Matches("foo_bar"), Is.False, "foo_bar");
                Assert.That(pattern.Matches("foobarX"), Is.True, "foobarX");
            });
        }

        [Test]
        public void AreEqualIfIdentical()
        {
            GlobPatternList pattern1 = new GlobPatternList(new List<string> { "foo*" }, new List<string> { "*bar" });
            GlobPatternList pattern2 = new GlobPatternList(new List<string> { "foo*" }, new List<string> { "*bar" });
            
            Assert.That(pattern1, Is.EqualTo(pattern2));
        }

        [Test]
        public void AreNotEqualIfDifferent()
        {
            GlobPatternList pattern1 = new GlobPatternList(new List<string> { "foo*" }, new List<string> { "*bar" });
            GlobPatternList pattern2 = new GlobPatternList(new List<string> { "baz*" }, new List<string> { "*fom" });

            Assert.That(pattern1, Is.Not.EqualTo(pattern2));
        }
    }
}