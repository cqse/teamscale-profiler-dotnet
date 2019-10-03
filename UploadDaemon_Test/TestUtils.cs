using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Utilities for accessing test folders.
/// </summary>
public class TestUtils
{
    /// <summary>
    /// Root of this solution.
    /// </summary>
    public static DirectoryInfo SolutionRoot =>
        new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../"));

    /// <summary>
    /// Test data directory of the currently running test class.
    /// </summary>
    public static string TestDataDirectory =>
        Path.Combine(SolutionRoot.FullName, "test-data", GetSanitizedTestClassName());

    /// <summary>
    /// Temporary directory for the currently running test method.
    /// </summary>
    public static string TestTempDirectory =>
        Path.Combine(SolutionRoot.FullName, "test-tmp", GetSanitizedTestName());

    /// <summary>
    /// Returns the path to a file in the current tests's test data directory.
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    public static string GetTestFile(string relativePath) => Path.Combine(TestDataDirectory, relativePath);

    /// <summary>
    /// Returns the current test's name with characters removed that are not allowed in paths.
    /// </summary>
    public static string GetSanitizedTestName()
    {
        var testDirName = TestContext.CurrentContext.Test.FullName;
        char[] invalidChars = Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()).Distinct().ToArray();
        return string.Join("", testDirName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Returns the current test class's name with characters removed that are not allowed in paths.
    /// </summary>
    public static string GetSanitizedTestClassName()
    {
        var testDirName = TestContext.CurrentContext.Test.ClassName;
        char[] invalidChars = Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()).Distinct().ToArray();
        return string.Join("", testDirName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}