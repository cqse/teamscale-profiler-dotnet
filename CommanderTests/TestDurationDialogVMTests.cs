using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cqse.Teamscale.Profiler.Commander.Tests
{
    /// <summary>
    /// Tests the conversion between milliseconds (long) and human-readable duration strings.
    /// </summary>
    [TestClass()]
    public class TestDurationDialogVMTests
    {
        [TestMethod()]
        public void MillisecondsToStringTest()
        {
            Assert.AreEqual("1s", TestDurationDialogViewModel.MillisecondsToString(1000));
            Assert.AreEqual("1s", TestDurationDialogViewModel.MillisecondsToString(1));
            Assert.AreEqual("1s", TestDurationDialogViewModel.MillisecondsToString(0));
            Assert.AreEqual("5m 6s", TestDurationDialogViewModel.MillisecondsToString(6 * 1000 + 5 * 1000 * 60));
            Assert.AreEqual("1h 5m 6s", TestDurationDialogViewModel.MillisecondsToString(6 * 1000 + 5 * 1000 * 60 + 1 * 1000 * 60 * 60));
            Assert.AreEqual("59m 59s", TestDurationDialogViewModel.MillisecondsToString(1 * 1000 * 60 * 60 - 1));
            Assert.AreEqual("1h", TestDurationDialogViewModel.MillisecondsToString(1 * 1000 * 60 * 60));
            Assert.AreEqual("1h", TestDurationDialogViewModel.MillisecondsToString(1 * 1000 * 60 * 60 + 1));
            Assert.AreEqual("1h 1s", TestDurationDialogViewModel.MillisecondsToString(1 * 1000 * 60 * 60 + 1000));
        }

        [TestMethod()]
        public void StringToMillisecondsErrorCasesTest()
        {
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds(""));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("xyz"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds(" "));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("12"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("1m 12"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("12 1m"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("m"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("m 12s"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("60m"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("60s"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("-1s"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("0s"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("0m"));
            Assert.AreEqual(null, TestDurationDialogViewModel.StringToMilliseconds("0h"));
        }

        [TestMethod()]
        public void StringToMillisecondsTest()
        {
            Assert.AreEqual(1000, TestDurationDialogViewModel.StringToMilliseconds("1s"));
            Assert.AreEqual(1000 * 60, TestDurationDialogViewModel.StringToMilliseconds("1m"));
            Assert.AreEqual(1000 * 60 * 60, TestDurationDialogViewModel.StringToMilliseconds("1h"));
            Assert.AreEqual(1000 * 60 * 60 + 2 * 1000 * 60 + 3 * 1000, TestDurationDialogViewModel.StringToMilliseconds("1h 2m 3s"));
            Assert.AreEqual(1000 * 60 * 60 + 2 * 1000 * 60 + 3 * 1000, TestDurationDialogViewModel.StringToMilliseconds("1h2m3s"));
            Assert.AreEqual(1000 * 60 * 60 * 120, TestDurationDialogViewModel.StringToMilliseconds("120h"));
        }
    }
}