using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cqse.Teamscale.Profiler.Commander.Tests
{
    [TestClass()]
    public class TestDurationDialogVMTests
    {
        [TestMethod()]
        public void MillisecondsToStringTest()
        {
            Assert.AreEqual("1s", TestDurationDialogVM.MillisecondsToString(1000));
            Assert.AreEqual("1s", TestDurationDialogVM.MillisecondsToString(1));
            Assert.AreEqual("1s", TestDurationDialogVM.MillisecondsToString(0));
            Assert.AreEqual("5m 6s", TestDurationDialogVM.MillisecondsToString(6 * 1000 + 5 * 1000 * 60));
            Assert.AreEqual("1h 5m 6s", TestDurationDialogVM.MillisecondsToString(6 * 1000 + 5 * 1000 * 60 + 1 * 1000 * 60 * 60));
            Assert.AreEqual("59m 59s", TestDurationDialogVM.MillisecondsToString(1 * 1000 * 60 * 60 - 1));
            Assert.AreEqual("1h", TestDurationDialogVM.MillisecondsToString(1 * 1000 * 60 * 60));
            Assert.AreEqual("1h", TestDurationDialogVM.MillisecondsToString(1 * 1000 * 60 * 60 + 1));
            Assert.AreEqual("1h 1s", TestDurationDialogVM.MillisecondsToString(1 * 1000 * 60 * 60 + 1000));
        }

        [TestMethod()]
        public void StringToMillisecondsErrorCasesTest()
        {
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds(""));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("xyz"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds(" "));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("12"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("1m 12"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("12 1m"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("m"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("m 12s"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("60m"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("60s"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("-1s"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("0s"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("0m"));
            Assert.AreEqual(null, TestDurationDialogVM.StringToMilliseconds("0h"));
        }

        [TestMethod()]
        public void StringToMillisecondsTest()
        {
            Assert.AreEqual(1000, TestDurationDialogVM.StringToMilliseconds("1s"));
            Assert.AreEqual(1000 * 60, TestDurationDialogVM.StringToMilliseconds("1m"));
            Assert.AreEqual(1000 * 60 * 60, TestDurationDialogVM.StringToMilliseconds("1h"));
            Assert.AreEqual(1000 * 60 * 60 + 2 * 1000 * 60 + 3 * 1000, TestDurationDialogVM.StringToMilliseconds("1h 2m 3s"));
            Assert.AreEqual(1000 * 60 * 60 + 2 * 1000 * 60 + 3 * 1000, TestDurationDialogVM.StringToMilliseconds("1h2m3s"));
            Assert.AreEqual(1000 * 60 * 60 * 120, TestDurationDialogVM.StringToMilliseconds("120h"));
        }
    }
}