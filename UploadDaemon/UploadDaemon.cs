using Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Timers;
using UploadDaemon.Upload;

namespace UploadDaemon
{
    /// <summary>
    /// Main entry point of the program.
    /// </summary>
    public class UploadDaemon
    {
        private const long TimerIntervalInMilliseconds = 1000 * 60 * 5;

        private const string DaemonControlPipeName = "UploadDaemon/ControlPipe";

        private const string DaemonControlCommandUpload = "upload";

        /// <summary>
        /// Lock used to ensure that no two uploads happen in parallel.
        /// </summary>
        private static readonly object SequentialUploadsLock = new object();

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Config config;
        private readonly UploadTask uploadTask;
        private readonly FileSystem fileSystem;

        /// <summary>
        /// Main entry point. Expects a single argument: the path to a directory that contains the trace files to upload.
        /// </summary>
        public static void Main(string[] args)
        {
            if (IsAlreadyRunning())
            {
                logger.Debug("Another instance is already running. Sending notification to trigger upload.");
                NotifyRunningDaemon();
                return;
            }

            Config config;
            try
            {
                logger.Debug("Reading config from {configFile}", Config.ConfigFilePath);
                config = Config.ReadFromCentralConfigFile();
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to read config file {configPath}", Config.ConfigFilePath);
                Environment.Exit(1);
                return;
            }

            logger.Info("Starting upload daemon v{uploaderVersion}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            var uploader = new UploadDaemon(config);
            uploader.RunOnce();
            uploader.ScheduleRegularUploads();
            uploader.WaitForNotifications();
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

        public UploadDaemon(Config config)
        {
            fileSystem = new FileSystem();

            this.config = config;
            if (config.DisableSslValidation)
            {
                HttpClientUtils.DisableSslValidation();
            }

            uploadTask = new UploadTask(config, fileSystem, new UploadFactory());
        }

        /// <summary>
        /// Runs the timer action once synchronously.
        /// </summary>
        public void RunOnce()
        {
            lock(SequentialUploadsLock)
            {
                uploadTask.Run();
            }
        }

        /// <summary>
        /// Schedule uploads on a regular intervall.
        /// </summary>
        private void ScheduleRegularUploads()
        {
            Timer timer = new Timer();
            timer.Elapsed += (sender, args) => RunOnce();
            timer.Interval = TimerIntervalInMilliseconds;
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
                    using (var streamReader = new StreamReader(pipeServerStream))
                    {
                        var command = streamReader.ReadLine();
                        ExecuteCommand(command);
                    }
                }
            }
        }

        /// <summary>
        /// Executes a command forwarded to this UploadDaemon process.
        /// </summary>
        /// <param name="command"></param>
        private void ExecuteCommand(string command)
        {
            switch (command)
            {
                case DaemonControlCommandUpload:
                    RunOnce();
                    break;
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

                using (var w = new StreamWriter(pipeClientStream))
                {
                    w.WriteLine(DaemonControlCommandUpload);
                }
            }
        }
    }
}