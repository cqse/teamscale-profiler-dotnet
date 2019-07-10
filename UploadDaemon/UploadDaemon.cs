using Common;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Timers;
using UploadDaemon.Archiving;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Upload;

namespace UploadDaemon
{
    /// <summary>
    /// Main entry point of the program.
    /// </summary>
    public class UploadDaemon
    {
        private const string DaemonControlPipeName = "UploadDaemon/ControlPipe";

        private const string DaemonControlCommandRunNow = "run";

        /// <summary>
        /// Lock used to ensure that no two uploads happen in parallel.
        /// </summary>
        private static readonly object SequentialUploadsLock = new object();

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Main entry point. Expects a single argument: the path to a directory that contains the trace files to upload.
        /// </summary>
        public static void Main(string[] args)
        {
            if (IsAlreadyRunning())
            {
                // Writing to console on purpose, to explain to users why the exe terminates immediately.
                Console.WriteLine("Another instance is already running. Sending notification to trigger upload.");
                NotifyRunningDaemon();
                return;
            }

            logger.Info("Starting upload daemon v{uploaderVersion}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            var uploader = new UploadDaemon();
            uploader.RunOnce();

            Config config = ReadConfig();
            if (config.UploadInterval > TimeSpan.Zero)
            {
                uploader.ScheduleRegularRuns(config.UploadInterval);
                uploader.WaitForNotifications();
            }
        }

        private static bool IsAlreadyRunning()
        {
            Process current = Process.GetCurrentProcess();
            // If this process is not running from the exe file, we allow it
            // Not sure when this happens, though
            if (Assembly.GetExecutingAssembly().Location.Replace("/", "\\") != current.MainModule.FileName)
            {
                return false;
            }

            return Process.GetProcessesByName(current.ProcessName).Where(process => process.Id != current.Id).Any();
        }

        private static Config ReadConfig()
        {
            logger.Debug("Reading config from {configFile}", Config.ConfigFilePath);
            Config config = Config.ReadFromCentralConfigFile();

            if (config.DisableSslValidation)
            {
                HttpClientUtils.DisableSslValidation();
            }

            return config;
        }

        /// <summary>
        /// Runs the daemon tasks once, synchronously.
        /// </summary>
        public void RunOnce()
        {
            try
            {
                RunOnce(ReadConfig());
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to read config file {configPath}", Config.ConfigFilePath);
            }
        }

        /// <summary>
        /// Runs the daemon tasks once, synchronously, with the given config.
        /// </summary>
        public void RunOnce(Config config)
        {
            lock (SequentialUploadsLock)
            {
                var fileSystem = new FileSystem();
                new UploadTask(fileSystem, new UploadFactory(), new LineCoverageSynthesizer()).Run(config).Wait();
                new PurgeArchiveTask(new ArchiveFactory(fileSystem, new DefaultDateTimeProvider())).Run(config);
            }
        }

        /// <summary>
        /// Schedules uploader runs on a regular intervall.
        /// </summary>
        private void ScheduleRegularRuns(TimeSpan runInterval)
        {
            Timer timer = new Timer();
            timer.Elapsed += (sender, args) => RunOnce();
            timer.Interval = runInterval.TotalMilliseconds;
            timer.Enabled = true;
        }

        /// <summary>
        /// Waits for notifications from subsequent executions of the Daemon.
        /// </summary>
        private void WaitForNotifications()
        {
            while (true) // wait for indefinitely many commands
            {
                using (var pipeServerStream = new NamedPipeServerStream(DaemonControlPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    pipeServerStream.WaitForConnection();
                    using (var pipeStream = new StreamReader(pipeServerStream))
                    {
                        // There is currently only one command (DaemonControlCommandUpload), hence,
                        // we immediately trigger an upload without checking what we received.
                        pipeStream.ReadLine();
                        RunOnce();
                    }
                }
            }
        }

        /// <summary>
        /// Forwards a command to the existing UploadDaemon process.
        /// </summary>
        private static void NotifyRunningDaemon()
        {
            using (var pipeClientStream = new NamedPipeClientStream(".", DaemonControlPipeName, PipeDirection.Out, PipeOptions.Asynchronous))
            {
                pipeClientStream.Connect();

                using (var pipeStream = new StreamWriter(pipeClientStream))
                {
                    pipeStream.WriteLine(DaemonControlCommandRunNow);
                }
            }
        }
    }
}
