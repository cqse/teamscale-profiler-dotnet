using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using UploadDaemon;
using UploadDaemon.Configuration;
using UploadDaemon.Report;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Upload;

namespace UploadDaemon_Test
{
    [TestFixture]
    class ManualTestwiseCoverageConverter
    {
        [Test]
        public void ConvertFromTestwiseDirectories()
        {
            var fileSystem = new FileSystem();
            var manualTestwiseCoverageBaseDirectory = @"V:\Coverage\ManualTestwiseCoverage_Test\TracesByTest"; // @"V:\Coverage\TeamScale_TestcasewiseCov_Debug";
            var pdbDirectory = @"V:\Coverage\ManualTestwiseCoverage_Test\PDBs"; // @"V:\Coverage\PDB_SOM8_Pine"
            var revisionFile = @"V:\Coverage\revision.txt";
            var manualTestwiseCoverageReportBaseDirectory = @"V:\Coverage\ManualTestwiseCoverage_Test\Reports"; // @"V:\Coverage\testwise";

            //ConvertEachFolderToASeparateTestwiseReport(fileSystem, manualTestwiseCoverageBaseDirectory, pdbDirectory, revisionFile, manualTestwiseCoverageReportBaseDirectory);
            MergeIndividualReportsIntoOne(fileSystem, manualTestwiseCoverageReportBaseDirectory);
        }

        private static void ConvertEachFolderToASeparateTestwiseReport(FileSystem fileSystem, string manualTestwiseCoverageBaseDirectory, string pdbDirectory, string revisionFile, string manualTestwiseCoverageReportBaseDirectory)
        {
            var uploadTask = new UploadTask(fileSystem, new UploadFactory(), new LineCoverageSynthesizer());

            foreach (string manualTestwiseCoverageDirectory in fileSystem.Directory.GetDirectories(manualTestwiseCoverageBaseDirectory))
            {
                string testName = Path.GetFileName(manualTestwiseCoverageDirectory);

                var config = new Config(new ConfigParser.YamlConfig()
                {
                    Match = new List<ConfigParser.ProcessSection>() {
                    new ConfigParser.ProcessSection()
                    {
                        Profiler = new Dictionary<string, string>()
                        {
                            { "enabled", "true" },
                            { "targetdir", manualTestwiseCoverageDirectory },
                        },
                        Uploader = new ConfigParser.UploaderSubsection()
                        {
                            Enabled = true,
                            PdbDirectory = pdbDirectory,
                            MergeLineCoverage = true,
                            RevisionFile = revisionFile,
                            Directory = Path.Combine(manualTestwiseCoverageReportBaseDirectory, testName),
                        }
                    }
                }
                });

                uploadTask.Run(config);
            }
        }

        private static void MergeIndividualReportsIntoOne(FileSystem fileSystem, string manualTestwiseCoverageReportBaseDirectory)
        {
            var accumulatedReport = new TestwiseCoverageReport();
            accumulatedReport.Tests = new List<TestwiseCoverageReport.Test>();

            foreach (string manualTestwiseCoverageReportDirectory in fileSystem.Directory.GetDirectories(manualTestwiseCoverageReportBaseDirectory))
            {
                string testName = Path.GetFileName(manualTestwiseCoverageReportDirectory);

                foreach (string reportFile in fileSystem.Directory.GetFiles(manualTestwiseCoverageReportDirectory))
                {
                    if (!reportFile.EndsWith(".simple"))
                    {
                        continue;
                    }

                    string json = File.ReadAllText(reportFile);
                    TestwiseCoverageReport report = JsonConvert.DeserializeObject<TestwiseCoverageReport>(json);
                    TestwiseCoverageReport.Test test = report.Tests.First();
                    test.UniformPath = testName;
                    accumulatedReport.Tests.Add(test);
                }
            }

            File.WriteAllText(Path.Combine(manualTestwiseCoverageReportBaseDirectory, "report.testwise"), JsonConvert.SerializeObject(accumulatedReport));
        }

        [Test, Ignore("for manual debugging")]
        public void DebugConfig()
        {
            Config config = Config.Read(File.ReadAllText(@"V:\Profiler.yml"));
            Config.ConfigForProcess configForProcess = config.CreateConfigForProcess("foo");
            GlobPatternList assemblyPatterns = configForProcess.AssemblyPatterns;
        }
    }
}
