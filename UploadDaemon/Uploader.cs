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
using System.Threading;
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

            WaitForNotificationsUntilCancellation();
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
        private void WaitForNotificationsUntilCancellation()
        {
            // TODO (SA) I took this over from the original suspendThread() method, however, I'm
            // not sure about its purpose, since the console (CMD) seems to disconnect immediately
            // anyways and I cannot ever issue Ctrl+C...
            AutoResetEvent cancelEvent = new AutoResetEvent(false);
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                cancelEvent.Set();
                eArgs.Cancel = true;
            };

            while (true) // wait for indefinitely many commands
            {
                using (var pipeServerStream = new NamedPipeServerStream(DaemonControlPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    if (pipeServerStream.WaitForConnection(cancelEvent))
                    {
                        using (var streamReader = new StreamReader(pipeServerStream))
                        {
                            var command = streamReader.ReadLine();
                            ExecuteCommand(command);
                        }
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

static class NamedPipeServerStreamEx
{
    /// <summary>
    /// Waits for a connection to this pipe server stream or a cancellation event.
    /// Based on https://stackoverflow.com/a/10485210
    /// </summary>
    public static bool WaitForConnection(this NamedPipeServerStream pipeStream, WaitHandle cancelEvent)
    {
        // capture interrupt from a connection to the pipe stream
        Exception e = null;
        AutoResetEvent connectEvent = new AutoResetEvent(false);
        pipeStream.BeginWaitForConnection(ar =>
        {
            try
            {
                pipeStream.EndWaitForConnection(ar);
            }
            catch (Exception er)
            {
                e = er;
            }
            connectEvent.Set();
        }, null);

        // Wait for connection or cancellation
        if (WaitHandle.WaitAny(new[] { connectEvent, cancelEvent }) == 1)
        {
            return false; // cancellation occurred
        }
        else if (e != null)
        {
            throw e; // rethrow exception
        }
        else
        {
            return true; // connection established
        }
    }
}