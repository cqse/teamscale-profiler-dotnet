using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using UploadDaemon;
using UploadDaemon.Configuration;
using UploadDaemon.Report;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Upload;

namespace ManualTestwiseCoverageConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: ManualTestwiseCoverageConverter.exe <path-to-testwise-traces> <path-to-pdbs> <path-to-revision-txt> <path-to-write-reports-to>");
                Environment.Exit(1);
            }

            string pathToTestwiseTraces = args[0];
            string pathToPDBs = args[1];
            string pathToRevisionTxt = args[2];
            string pathToReports = args[3];

            var fileSystem = new FileSystem();

            ConvertEachFolderToASeparateTestwiseReport(fileSystem, pathToTestwiseTraces, pathToPDBs, pathToRevisionTxt, pathToReports);
            MergeIndividualReportsIntoOne(fileSystem, pathToReports);
        }

        private static void ConvertEachFolderToASeparateTestwiseReport(FileSystem fileSystem, string manualTestwiseCoverageBaseDirectory, string pdbDirectory, string revisionFile, string manualTestwiseCoverageReportBaseDirectory)
        {
            Console.WriteLine("Converting coverage from each test into an individual report...");

            var uploadTask = new UploadTask(fileSystem, new UploadFactory(), new LineCoverageSynthesizer());

            var manualTestwiseCoverageDirectories = fileSystem.Directory.GetDirectories(manualTestwiseCoverageBaseDirectory);
            for (int i = 0; i < manualTestwiseCoverageDirectories.Length; i++)
            {
                string manualTestwiseCoverageDirectory = manualTestwiseCoverageDirectories[i];
                string testName = Path.GetFileName(manualTestwiseCoverageDirectory);
                Console.WriteLine(string.Format("({0:000}/{1:000}) {2}", i, manualTestwiseCoverageDirectories.Length, testName));
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
            Console.WriteLine("Merging individual reports into a single report...");

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

            var reportPath = Path.Combine(manualTestwiseCoverageReportBaseDirectory, "report.testwise");
            fileSystem.File.WriteAllText(reportPath, JsonConvert.SerializeObject(accumulatedReport));
            Console.WriteLine("Final report written to " + reportPath);

            if (testsWithUnknownDuration.Count > 0)
            {
                var missingDurationsPath = Path.Combine(manualTestwiseCoverageReportBaseDirectory, "testsWithUnknownDuration.json");
                fileSystem.File.WriteAllText(missingDurationsPath, JsonConvert.SerializeObject(testsWithUnknownDuration));
                Console.WriteLine("[WARN] For at least some of the test no duration was given. A list of these tests was written to " + missingDurationsPath);
            }
        }

        private static IDictionary<string, int> ReadDurationsIfExists(FileSystem fileSystem, string manualTestwiseReportBaseDirectory)
        {
            var durations = new Dictionary<string, int>();

            var durationsFile = Path.Combine(manualTestwiseReportBaseDirectory, "durations.csv");
            if (fileSystem.File.Exists(durationsFile))
            {
                Console.WriteLine("Reading test durations from " + durationsFile);
                using (var reader = new StreamReader(durationsFile))
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
                            throw new ArgumentException($"multiple durations for test name {cells[0]}");
                        }

                        durations[testName] = testDurationInSeconds;
                    }
                }
            }
            else
            {
                Console.WriteLine("[WARN] No durations provided. All tests will be assign a constant duration of 1s. Consider providing durations in " + durationsFile);
            }

            return durations;
        }
    }
}
