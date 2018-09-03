using ProfilerGUI.Source.Configurator;
using ProfilerGUI.Source.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ProfilerGUI.Source.Runner
{
    public class ProfilerRunner
    {
        private readonly ProfilerConfiguration configuration;

        /// <summary> The time interval used for regular trace copy operations (10 minutes) </summary>
        private static readonly TimeSpan TraceCopyInterval = TimeSpan.FromMinutes(10);

        /// <summary> Called if the profiling ended, i.e. all processes to profile were stopped. </summary>
        // TODO (FS) change the name to make it clear that this is a handler/callback/delegate? E.g. ProfilingEndHandler
        // From outside this class it's not immediately clear that this is an event handler
        public event EventHandler<EventArgs> ProfilingEnd;

        /// <summary> Called if the profiling ended, i.e. all processes to profile were stopped. </summary>
        // TODO (FS) change the name to make it clear that this is a handler/callback/delegate?
        // From outside this class it's not immediately clear that this is an event handler
        // Also: the comment seems copy-pasted
        public event EventHandler<TraceFileCopyEventArgs> TraceFileCopy;

        internal ProfilerRunner(ProfilerConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public Task Run()
        {
            return Task.Factory.StartNew(() =>
            {
                Tuple<string, string> dllEnvironmentTuple = CreateProfilerDllVariableTuple();
                Tuple<string, string> enableProfilingTuple = Tuple.Create(ProfilerConstants.EnableProfilingArg, "1");
                Tuple<string, string> targetDirTuple = Tuple.Create(ProfilerConstants.TargetDirectoryForTracesArg, configuration.TraceTargetFolder);


                Process process = SystemUtils.RunNonBlocking(configuration.TargetApplicationPath, configuration.TargetApplicationArguments, configuration.WorkingDirectory, dllEnvironmentTuple, enableProfilingTuple, targetDirTuple);

                HookScheduledTraceCopying();
                process.Exited += OnTargetProcessEnd;
            });
        }

        private void HookScheduledTraceCopying()
        {
            Timer timer = new Timer(TraceCopyInterval.TotalMilliseconds)
            {
                AutoReset = true
            };
            timer.Elapsed += RunScheduledTraceCopy;
            timer.Start();
        }

        private void RunScheduledTraceCopy(object sender, ElapsedEventArgs e)
        {
            CopyTraceFiles();
        }

        private Tuple<string, string> CreateProfilerDllVariableTuple()
        {
            string profilerDll = ProfilerConstants.ProfilerDll64Bit;
            if (configuration.ApplicationType == EApplicationType.Type32Bit)
            {
                profilerDll = ProfilerConstants.ProfilerDll32Bit;
            }
            return Tuple.Create(ProfilerConstants.ProfilerPathArg, Environment.CurrentDirectory + @"\" + profilerDll);
        }

        private void OnTargetProcessEnd(object sender, EventArgs e)
        {
            bool relevantProcessesRunning = AttachExitHookToKnownProcess();

            if (!relevantProcessesRunning)
            {
                // TODO (FS) you are not checking for that when copying from the timer
                // please make this consistent by either removing this check here if
                // it's not necessary or moving it into CopyTraceFiles if it is
                if (!string.IsNullOrEmpty(configuration.TraceFolderToCopyTo))
                {
                    CopyTraceFiles();
                }
                ProfilingEnd?.Invoke(this, EventArgs.Empty);
            }
        }

        // TODO (FS) please document the return value
        private bool AttachExitHookToKnownProcess()
        {
            // TODO (FS) is this condition not exactly the inverse of what it should be?
            // if there are any executables to wait for then don't just return but attach
            // the hook?
            if (configuration.ExecutablesToWaitFor.Any())
            {
                return false;
            }

            bool anyRelevantProcessesRunning = false;
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                foreach (string processToWaitFor in configuration.ExecutablesToWaitFor)
                {
                    string processPath;
                    try
                    {
                        processPath = process.MainModule.FileName;
                    }
                    catch (Exception)
                    {
                        // For system processes, trying to access the MainModule will cause a 'Access denied' exception, which is not a problem here.
                        // In addition, we will see exceptions if the process has already ended.
                        continue;
                    }
                    if (processPath.ToLower().EndsWith(processToWaitFor.ToLower()))
                    {
                        process.EnableRaisingEvents = true;
                        process.Exited += OnTargetProcessEnd;
                        anyRelevantProcessesRunning = true;
                    }
                }

            }

            return anyRelevantProcessesRunning;
        }

        private void CopyTraceFiles()
        {
            // TODO (FS) I think we should try to create this folder when the user tries to start the profiled application
            // and fail to start it if we can't. That way the user get's early feedback about permission problems
            if (!Directory.Exists(configuration.TraceTargetFolder))
            {
                Console.WriteLine("Error: Trace directory " + configuration.TraceTargetFolder + " does not exist or cannot be accessed.");
                return;
            }

            List<string> traceFiles = Directory.GetFiles(configuration.TraceTargetFolder, "*.txt").Select(fullPath => Path.GetFileName(fullPath)).ToList();
            if (!traceFiles.Any())
            {
                Console.WriteLine("No trace files (*.txt) found in " + configuration.TraceTargetFolder);
                return;
            }

            CopyFilesAndRemoveFromSource(configuration.TraceTargetFolder, traceFiles, configuration.TraceFolderToCopyTo);
        }

        private void CopyFilesAndRemoveFromSource(string traceFolder, IEnumerable<string> traceFilesNames, string targetFolderToCopyTo)
        {
            if (!Directory.Exists(targetFolderToCopyTo))
            {
                // TODO (FS) handle exceptions?
                Directory.CreateDirectory(targetFolderToCopyTo);
                if (!Directory.Exists(targetFolderToCopyTo))
                {
                    Console.WriteLine("Error: Could not create target directory: " + targetFolderToCopyTo);
                    return;
                }
            }
            TraceFileCopyEventArgs copyEventArgs = new TraceFileCopyEventArgs();
            foreach (string traceFile in traceFilesNames)
            {
                CopyAndRemoveFile(traceFolder, traceFile, targetFolderToCopyTo, copyEventArgs);
            }
            TraceFileCopy?.Invoke(this, copyEventArgs);
        }

        private void CopyAndRemoveFile(string traceFolder, string traceFileName, string traceFolderToCopyTo, TraceFileCopyEventArgs copyEventArgs)
        {
            string fullSourceFilePath = Path.Combine(traceFolder, traceFileName);
            try
            {
                File.Copy(fullSourceFilePath, Path.Combine(traceFolderToCopyTo, traceFileName), overwrite: true);
                copyEventArgs.SuccessfulCopiedFiles.Add(fullSourceFilePath);
            }
            catch
            {
                copyEventArgs.FailedCopiedFiles.Add(fullSourceFilePath);
                return;
            }

            try
            {
                File.Delete(fullSourceFilePath);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Warning: Could not remove file: " + traceFileName + "\n > " + e.Message);
                Debug.WriteLine("This can happen if multiple profilers are running at the same time. Continuing... ");
            }
        }
    }
}
