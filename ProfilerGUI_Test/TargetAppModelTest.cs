using NUnit.Framework;
using ProfilerGUI.Source.Configurator;
using ProfilerGUI.Source.Shared;
using System.IO;

namespace Cqse.Teamscale.Profiler.Dotnet
{
    [TestFixture]
    public class TargetAppModelTest
    {
        [Test]
        public void TestDetectDotNETCoreApplicationType()
        {
            TargetAppModel targetAppModel = CreateModelForTestApplication("DotNetCoreApp.dll");

            Assert.AreEqual(EApplicationType.DotNetCore, targetAppModel.ApplicationType);
        }

        [Test]
        public void TestDetectDotNETFramework32BitApplicationType()
        {
            TargetAppModel targetAppModel = CreateModelForTestApplication("DotNetFW32BitApp.exe");

            Assert.AreEqual(EApplicationType.DotNetFramework32Bit, targetAppModel.ApplicationType);
        }

        [Test]
        public void TestDetectDotNETFramework64BitApplicationType()
        {
            TargetAppModel targetAppModel = CreateModelForTestApplication("DotNetFW64BitApp.exe");

            Assert.AreEqual(EApplicationType.DotNetFramework64Bit, targetAppModel.ApplicationType);
        }

        [Test]
        public void TestDetectNonDotNet32BitApplicationType()
        {
            TargetAppModel targetAppModel = CreateModelForTestApplication("NonDotNet32BitApp.exe");

            Assert.AreEqual(EApplicationType.DotNetFramework32Bit, targetAppModel.ApplicationType);
        }

        [Test]
        public void TestDetectNonDotNet64BitApplicationType()
        {
            TargetAppModel targetAppModel = CreateModelForTestApplication("NonDotNet64BitApp.exe");

            Assert.AreEqual(EApplicationType.DotNetFramework64Bit, targetAppModel.ApplicationType);
        }

        private static TargetAppModel CreateModelForTestApplication(string application)
        {
            var targetAppModel = new TargetAppModel(new ProfilerConfiguration());
            targetAppModel.ApplicationPath = GetTestDataPath("test-programs", application);
            return targetAppModel;
        }

        /// <summary>
        /// The directory containing the profiler solution.
        /// </summary>
        public static DirectoryInfo SolutionRoot =>
            new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../.."));

        /// <summary>
        /// Returns the absolute path to a test data file.
        /// </summary>
        protected static string GetTestDataPath(params string[] path)
            => Path.Combine(SolutionRoot.FullName, "test-data", Path.Combine(path));
    }
}