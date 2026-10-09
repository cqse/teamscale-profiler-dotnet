using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Timers;
using UploadDaemon.Archiving;
using UploadDaemon.Upload;
using UploadDaemon.Configuration;

namespace UploadDaemon
{
    /// <summary>
    /// Main entry point of the program.
    /// </summary>
    public class UploadDaemon
    {
        /// <summary>
        /// Path to the config file. It's located one directory above the uploader's DLLs.
        /// </summary>
        private static string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\Profiler.yml");

        private const string DaemonControlPipeName = "UploadDaemon/ControlPipe";

        private const string DaemonControlCommandRunNow = "run";

        /// <summary>
        /// Length of the line sent by <see cref="NotifyRunningDaemon"/>, including the newline.
        /// </summary>
        private static readonly int DaemonControlCommandLength = DaemonControlCommandRunNow.Length + Environment.NewLine.Length;

        private static readonly PipeSecurity DaemonControlPipeSecurity = CreateControlPipeSecurity();

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
            ParseCommandline(args);
            logger.Info("Using config file from {configFilePath}", ConfigFilePath);

            if (IsAlreadyRunning())
            {
                // Writing to console on purpose, to explain to users why the exe terminates immediately.
                Console.WriteLine("Another instance is already running. Sending notification to trigger upload.");
                logger.Info("Another instance is already running. Sending notification to trigger upload.");
                try
                {
                    NotifyRunningDaemon();
                }
                catch (TimeoutException e)
                {
                    logger.Error(e, "Could not send notification trigger");
                }
                catch (Exception e) when (e is UnauthorizedAccessException || e is IOException)
                {
                    // E.g. if this process runs with a network logon, which is denied access to the control pipe
                    logger.Error(e, "Could not notify the running UploadDaemon. The upload will happen at its next scheduled interval.");
                }
                return;
            }

            logger.Info("Starting upload daemon v{uploaderVersion}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            var uploader = new UploadDaemon();
            Config config = uploader.RunOnce();

            if (config != null && config.UploadInterval > TimeSpan.Zero)
            {
                uploader.ScheduleRegularRuns(config.UploadInterval);
                uploader.WaitForNotifications();
            }
        }

        private static void ParseCommandline(string[] args)
        {
            if (Environment.GetEnvironmentVariable("COR_PROFILER_CONFIG") != null)
            {
                ConfigFilePath = Environment.GetEnvironmentVariable("COR_PROFILER_CONFIG");
            }
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--config":
                        if (i + 1 < args.Length)
                        {
                            ConfigFilePath = args[i + 1];
                            i++;
                        }
                        break;
                }
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
            logger.Debug("Reading config from {configFile}", ConfigFilePath);
            Config config = Config.ReadConfigFile(ConfigFilePath);
            HttpClientUtils.ConfigureHttpStack(config.DisableSslValidation);
            return config;
        }

        /// <summary>
        /// Runs the daemon tasks once, synchronously.
        /// </summary>
        public Config RunOnce()
        {
            try
            {
                Config config = ReadConfig();
                RunOnce(config);
                return config;
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to read config file {configPath}", ConfigFilePath);
                return null;
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
                new UploadTask(fileSystem, new UploadFactory(), new SymbolAnalysis.LineCoverageSynthesizerFactory()).Run(config);
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
                using (var pipeServerStream = new NamedPipeServerStream(DaemonControlPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, DaemonControlPipeSecurity))
                {
                    pipeServerStream.WaitForConnection();
                    // There is currently only one command (DaemonControlCommandRunNow), hence, we trigger an upload without
                    // checking what we received. The read is bounded on purpose: any local user can write to this pipe, so an
                    // unbounded read like StreamReader.ReadLine() would let a client make the daemon run out of memory.
                    pipeServerStream.Read(new byte[DaemonControlCommandLength], 0, DaemonControlCommandLength);
                    RunOnce();
                }
            }
        }

        /// <summary>
        /// Creates the access rules for the control pipe. Local users may write to it, so that the daemon can be notified
        /// even if it runs elevated or as a different user (e.g. as a service). Network logons are denied, since
        /// .NET Framework does not support PIPE_REJECT_REMOTE_CLIENTS.
        /// </summary>
        private static PipeSecurity CreateControlPipeSecurity()
        {
            var pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));
            using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
            {
                pipeSecurity.AddAccessRule(new PipeAccessRule(currentIdentity.User, PipeAccessRights.FullControl, AccessControlType.Allow));
            }
            pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
            // Must match the access requested in NotifyRunningDaemon. CreateFile always adds ReadAttributes.
            pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), PipeAccessRights.WriteData | PipeAccessRights.ReadAttributes, AccessControlType.Allow));
            return pipeSecurity;
        }

        /// <summary>
        /// Forwards a command to the existing UploadDaemon process.
        /// </summary>
        private static void NotifyRunningDaemon()
        {
            using (var pipeClientStream = new NamedPipeClientStream(".", DaemonControlPipeName, PipeAccessRights.WriteData, PipeOptions.Asynchronous,
                TokenImpersonationLevel.None, HandleInheritability.None))
            {
                pipeClientStream.Connect(1000);

                using (var pipeStream = new StreamWriter(pipeClientStream))
                {
                    pipeStream.WriteLine(DaemonControlCommandRunNow);
                }
            }
        }
    }
}
