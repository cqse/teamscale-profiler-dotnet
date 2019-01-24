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
    public class Uploader
    {
        private const long TimerIntervalInMilliseconds = 1000 * 60 * 5;

        private const string DaemonControlPipeName = "UploadDaemon/ControlPipe";

        private const string DaemonControlCommandUpload = "upload";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Config config;
        private readonly TimerAction timerAction;
        private readonly FileSystem fileSystem;

        /// <summary>
        /// Main entry point. Expects a single argument: the path to a directory that contains the trace files to upload.
        /// </summary>
        public static void Main(string[] args)
        {
            if (IsAlreadyRunning())
            {
                Console.WriteLine("Another instance is already running.");
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

            new Uploader(config).Run();
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

        public Uploader(Config config)
        {
            fileSystem = new FileSystem();

            this.config = config;
            if (config.DisableSslValidation)
            {
                HttpClientUtils.DisableSslValidation();
            }

            timerAction = new TimerAction(config, fileSystem, new UploadFactory());
        }

        private void Run()
        {
            logger.Info("Starting upload daemon v{uploaderVersion}", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            RunOnce();

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(timerAction.HandleTimerEvent);
            timer.Interval = TimerIntervalInMilliseconds;
            timer.Enabled = true;

            WaitForNotifications();
        }

        /// <summary>
        /// Runs the timer action once synchronously.
        /// </summary>
        public void RunOnce()
        {
            timerAction.Run();
        }

        /// <summary>
        /// Waits for notifications from subsequent executions of the Daemon. Terminates on
        /// interrupt from the console's cancel key.
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
                    // TODO (SA) we probably need a lock here, to avoid triggering a second upload
                    // in parallel. Should we just skip in this case or enqueue a subsequent upload?
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