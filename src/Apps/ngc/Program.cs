using System;
using System.Threading.Tasks;
using ngc.Cli;
using ngc.Engine;
using NCBOX.Core.Config;
using NCBOX.Core.Logging;

namespace ngc;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var settings = NgcSettings.Load();
            var executor = new NgcCommandExecutor(settings);
            NgcAction action = NgcCliParser.Parse(args);
            switch (action)
            {
                case HelpAction:
                    NgcConsoleUi.ShowHelp();
                    return 0;
                case VersionAction:
                    return NgcConsoleUi.ShowVersion();
                case MissingArgumentAction missing:
                    return NgcConsoleUi.MissingArgument(missing.Expected);
                case UnknownCommandAction unknown:
                    return NgcConsoleUi.UnknownCommand(unknown.Command);
                default:
                    NgcConsoleUi.ShowProgress(action);
                    NgcCommandResult result = await executor.ExecuteAsync(action).ConfigureAwait(false);
                    NgcConsoleUi.Render(result);
                    return result.ExitCode;
            }
        }
        catch (Exception error)
        {
            TerminalConsole.WriteLine(new TerminalMessage($"[Error] {error.Message}", TerminalMessageKind.Error));
            return 1;
        }
    }
}
