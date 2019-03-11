using NUnit.Framework;
using Common;
using System.Collections.Generic;

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
}