using Cqse.Teamscale.Profiler.Commons.Ipc;
using System.Reflection;

var profilerIpc = new ProfilerIpc(new IpcConfig());

while(true)
{
    Command nextCommand = Command.Start;
    if (!string.IsNullOrEmpty(profilerIpc.TestName))
    {
        nextCommand = Command.Stop;
    }

    (Command command, string? arg) = WaitForCommand(nextCommand);
    if (ExecuteCommand(command, arg))
    {
        return;
    }
}

bool ExecuteCommand(Command command, string? arg)
{
    switch(command)
    {
        case Command.Start:
            profilerIpc.StartTest(arg);
            Help($"Started test {arg}");
            return false;
        case Command.Stop:
            if (arg?.Length > 1)
            {
                arg = char.ToUpper(arg[0]) + arg.Substring(1).ToLower();
                if (Enum.TryParse(arg, out TestExecutionResult result))
                {
                    profilerIpc.EndTest(result);
                    Help($"Stopped test with result {arg}");
                }

                return false;
            }
            Help($"Unknown test result {arg}, use one of these values: {CommandAttribute.ValidResultValues}");
            return false;
        case Command.Exit:
            return true;
        default:
            return false;
    }
}

(Command, string?) WaitForCommand(params Command[] commands)
{
    Dictionary<string, Command> availableCommands = commands.Append(Command.Exit).ToDictionary(command => command.ToString().ToLower(), command => command);
    Help("Available commands:");
    
    foreach(Command command in availableCommands.Values) {
        CommandAttribute attribute = CommandAttribute.GetAttribute(command);
        string synopsis = string.Join(" ", command.ToString().ToLower(), attribute.Argument);
        Help($" * {synopsis}: {attribute.Description}");
    }

    Help(string.Empty);

    while(true) {
        string[]? input = Console.ReadLine()?.Split(" ", 2);
        if (input == null)
        {
            continue;
        }

        Help(string.Empty);

        if (availableCommands.TryGetValue(input[0].ToLower(), out Command command)) {
            string? arg = null;
            if (input.Length > 1)
            {
                arg = input[1];
            }
            return (command, arg);
        }

        Help($"Unknown command: {input[0]}");
    }
}

void Help(string message)
{
    Console.WriteLine(message);
}

class CommandAttribute : Attribute
{
    public const string ValidResultValues = "passed, ignored, skipped, failure, error";

    public string Description { get; }
    public string? Argument { get; }

    public CommandAttribute(string description, string? argument = null)
    {
        Description = description;
        Argument = argument;
    }

    public static CommandAttribute GetAttribute(Command command)
    {
        MemberInfo memberInfo = typeof(Command).GetMember(command.ToString()).First();
        return memberInfo.GetCustomAttribute<CommandAttribute>();
    }

}

enum Command
{
    [Command("Starts a test named 'name-of-test'", "name-of-test")]
    Start,
    [Command($"Stops the test with result being one of {CommandAttribute.ValidResultValues}", "result")]
    Stop,
    [Command("Closes the commander")]
    Exit
}