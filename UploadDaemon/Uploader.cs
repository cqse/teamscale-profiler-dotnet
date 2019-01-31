using System;
using NLog;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Timers;
using Newtonsoft.Json;
using System.IO.Abstractions;
using System.Collections.Generic;

using System.Linq;
using UploadDaemon.Upload;
using Common;
using System.Net;

namespace UploadDaemon
{
    /// <summary>
    /// Main entry point of the program.
    /// </summary>
    public class Uploader
    {
        private const long TimerIntervalInMilliseconds = 1000 * 60 * 5;
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

            timerAction.Run();

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(timerAction.HandleTimerEvent);
            timer.Interval = TimerIntervalInMilliseconds;
            timer.Enabled = true;

            SuspendThread();
        }

        /// <summary>
        /// For testing. Runs the timer action once synchronously.
        /// </summary>
        public void RunOnce()
        {
            timerAction.Run();
        }

        /// <summary>
        /// Suspends this thread indefinitely but still reacts to interruptions from the console's cancel key.
        /// </summary>
        private static void SuspendThread()
        {
            AutoResetEvent cancelEvent = new AutoResetEvent(false);
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                cancelEvent.Set();
                eArgs.Cancel = true;
            };
            cancelEvent.WaitOne(Timeout.Infinite);
        }
    }
}