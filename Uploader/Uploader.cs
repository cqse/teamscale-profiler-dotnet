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

/// <summary>
/// Main entry point of the program.
/// </summary>
public class Uploader
{
    private const long TimerIntervalInMilliseconds = 1000 * 60 * 5;
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private static readonly List<string> HELP_COMMAND_LINE_ARGUMENTS = new List<string>()
    {
        "--help", "/h", "/?", "-h", "/help"
    };

    private readonly string traceDirectory;
    private readonly Config config;
    private readonly TimerAction timerAction;

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

        new Uploader(args).Run();
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

    private Uploader(string[] args)
    {
        traceDirectory = ParseArguments(args);
        FileSystem fileSystem = new FileSystem();
        config = ReadConfig(fileSystem);
        IUpload upload = config.CreateUpload(fileSystem);
        timerAction = new TimerAction(traceDirectory, config, upload, fileSystem);
    }

    private static Config ReadConfig(FileSystem fileSystem)
    {
        logger.Debug("Reading config from {configFile}", Config.ConfigFilePath);

        Config config;
        try
        {
            config = Config.ReadConfig(fileSystem);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to read config file {configPath}", Config.ConfigFilePath);
            Environment.Exit(1);
            return null;
        }

        List<string> errorMessages = config.Validate().ToList();
        if (errorMessages.Count == 0)
        {
            return config;
        }

        logger.Error("Invalid config file {configPath}: {errorMessages}", Config.ConfigFilePath, String.Join("; ", errorMessages));
        Environment.Exit(1);
        return null;
    }

    /// <summary>
    /// Parses the command line arguments and returns the trace directory.
    /// </summary>
    private string ParseArguments(string[] args)
    {
        logger.Debug("Parsing arguments {arguments}", args);

        if (args.Length != 1 || HELP_COMMAND_LINE_ARGUMENTS.Contains(args[0]))
        {
            Console.Error.WriteLine("Usage: Uploader.exe [DIR]");
            Console.Error.WriteLine("DIR: the directory that contains the trace files to upload.");
            Console.Error.WriteLine($"The uploader reads its configuration from {Config.ConfigFilePath}");
            Environment.Exit(1);
        }

        string traceDirectory = Path.GetFullPath(args[0]);
        if (!Directory.Exists(traceDirectory))
        {
            logger.Error("Directory {traceDirectory} does not exist", traceDirectory);
            Environment.Exit(1);
        }

        return traceDirectory;
    }

    private void Run()
    {
        logger.Info("Starting uploader v{uploaderVersion}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
        logger.Info("Reading traces with version assembly {versionAssembly} from {traceDirectory} and uploading them to {teamscale}",
            config.VersionAssembly, traceDirectory, config.Teamscale);

        timerAction.Run();

        System.Timers.Timer timer = new System.Timers.Timer();
        timer.Elapsed += new ElapsedEventHandler(timerAction.HandleTimerEvent);
        timer.Interval = TimerIntervalInMilliseconds;
        timer.Enabled = true;

        SuspendThread();
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