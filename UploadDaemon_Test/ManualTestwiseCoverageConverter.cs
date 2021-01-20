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

            // Debugging Input
            /*
            var manualTestwiseCoverageBaseDirectory = @"V:\Coverage\ManualTestwiseCoverage_Test\TracesByTest";
            var pdbDirectory = @"V:\Coverage\ManualTestwiseCoverage_Test\PDBs";
            var revisionFile = @"V:\Coverage\revision.txt";
            var manualTestwiseCoverageReportBaseDirectory = @"V:\Coverage\ManualTestwiseCoverage_Test\Reports";
            */

            // SOM8 -> Syngo POC
            /*
            var manualTestwiseCoverageBaseDirectory = @"Z:\Coverage\SOM8_Syngo_POC\TestCoverge_SOM8_Pine";
            var pdbDirectory = @"Z:\Coverage\SOM8_Syngo_POC\PDBs";
            var revisionFile = @"Z:\Coverage\revision.txt";
            var manualTestwiseCoverageReportBaseDirectory = @"Z:\Coverage\SOM8_Syngo_POC\2020-12-07 Reports";
            */

            var manualTestwiseCoverageBaseDirectory = @"Z:\Coverage\SOM8_Syngo_20201216\TestwiseCoverage_CaSc";
            var pdbDirectory = @"Z:\Coverage\SOM8_Syngo_20201216\PDBs";
            var revisionFile = @"Z:\Coverage\revision.txt";
            var manualTestwiseCoverageReportBaseDirectory = @"Z:\Coverage\SOM8_Syngo_20201216\2020-12-16 Reports CaSc";

            //ConvertEachFolderToASeparateTestwiseReport(fileSystem, manualTestwiseCoverageBaseDirectory, pdbDirectory, revisionFile, manualTestwiseCoverageReportBaseDirectory);
            MergeIndividualReportsIntoOne(fileSystem, manualTestwiseCoverageReportBaseDirectory);

            manualTestwiseCoverageBaseDirectory = @"Z:\Coverage\SOM8_Syngo_20201216\TestwiseCoverage_TP";
            manualTestwiseCoverageReportBaseDirectory = @"Z:\Coverage\SOM8_Syngo_20201216\2020-12-16 Reports TP";

            //ConvertEachFolderToASeparateTestwiseReport(fileSystem, manualTestwiseCoverageBaseDirectory, pdbDirectory, revisionFile, manualTestwiseCoverageReportBaseDirectory);
            MergeIndividualReportsIntoOne(fileSystem, manualTestwiseCoverageReportBaseDirectory);
        }

        private static void ConvertEachFolderToASeparateTestwiseReport(FileSystem fileSystem, string manualTestwiseCoverageBaseDirectory, string pdbDirectory, string revisionFile, string manualTestwiseCoverageReportBaseDirectory)
        {
            var uploadTask = new UploadTask(fileSystem, new UploadFactory(), new LineCoverageSynthesizer());

            foreach (string manualTestwiseCoverageDirectory in fileSystem.Directory.GetDirectories(manualTestwiseCoverageBaseDirectory))
            {
                string testName = Path.GetFileName(manualTestwiseCoverageDirectory);
                string outputDirectory = Path.Combine(manualTestwiseCoverageReportBaseDirectory, testName);

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
                            Directory = outputDirectory,
                        }
                    }
                }
                });

                fileSystem.Directory.CreateDirectory(outputDirectory);
                uploadTask.Run(config);
            }
        }

        private static void MergeIndividualReportsIntoOne(FileSystem fileSystem, string manualTestwiseCoverageReportBaseDirectory)
        {
            IDictionary<string, int> durations = ReadDurationsIfExists(fileSystem, manualTestwiseCoverageReportBaseDirectory);
            IList<string> testsWithUnknownDuration = new List<string>();

            var accumulatedReport = new TestwiseCoverageReport();
            accumulatedReport.Tests = new List<TestwiseCoverageReport.Test>();

            foreach (string manualTestwiseCoverageReportDirectory in fileSystem.Directory.GetDirectories(manualTestwiseCoverageReportBaseDirectory))
            {
                string testName = Path.GetFileName(manualTestwiseCoverageReportDirectory);

                int testNameParameterStartIndex = testName.IndexOf('(');
                string durationLookupName = testNameParameterStartIndex == -1 ? testName : testName.Substring(0, testNameParameterStartIndex);
                int testDurationInSeconds = durations.TryGetValue(durationLookupName, out int durationInSeconds) ? durationInSeconds : 1;

                if (testDurationInSeconds == 1)
                {
                    testsWithUnknownDuration.Add(testName);
                }

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
                    test.Duration = testDurationInSeconds;
                    accumulatedReport.Tests.Add(test);
                }
            }

            fileSystem.File.WriteAllText(Path.Combine(manualTestwiseCoverageReportBaseDirectory, "testsWithUnknownDuration.json"), JsonConvert.SerializeObject(testsWithUnknownDuration));
            fileSystem.File.WriteAllText(Path.Combine(manualTestwiseCoverageReportBaseDirectory, "report.testwise"), JsonConvert.SerializeObject(accumulatedReport));
        }

        private static IDictionary<string, int> ReadDurationsIfExists(FileSystem fileSystem, string manualTestwiseReportBaseDirectory)
        {
            var durations = new Dictionary<string, int>();

            var durationsFile = Path.Combine(manualTestwiseReportBaseDirectory, "durations.csv");
            if (fileSystem.File.Exists(durationsFile))
            {
                using(var reader = new StreamReader(durationsFile))
                {
                    reader.ReadLine(); // skip header
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();

                        if (line.Trim().Length == 0) { continue; }

                        string[] cells = line.Split(',');
                        string testName = cells[0];
                        int testDurationInSeconds = int.Parse(cells[1]);

                        if (durations.TryGetValue(testName, out int durationInSeconds) && durationInSeconds != testDurationInSeconds)
                        {
                            throw new AssertionException($"multiple durations for test name {cells[0]}");
                        }

                        durations[testName] = testDurationInSeconds;
                    }
                }
            }

            return durations;
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
