using System;
using System.Threading.Tasks;
using cbc.Cli;
using cbc.Engine;
using NCBOX.Core.Logging;

namespace cbc;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            CbcAction action = CbcCliParser.Parse(args);
            switch (action)
            {
                case HelpAction:
                    CbcConsoleUi.ShowHelp();
                    return 0;
                case VersionAction:
                    return CbcConsoleUi.ShowVersion();
                case MissingArgumentAction missing:
                    return CbcConsoleUi.MissingArgument(missing.Expected);
                case UnknownCommandAction unknown:
                    return CbcConsoleUi.UnknownCommand(unknown.Command);
                default:
                    bool removalConfirmed = action is RemoveAction remove && CbcConsoleUi.ConfirmRemoval(remove.Domain);
                    CbcConsoleUi.ShowProgress(action);
                    CbcCommandResult result = await new CbcCommandExecutor()
                        .ExecuteAsync(action, removalConfirmed)
                        .ConfigureAwait(false);
                    CbcConsoleUi.Render(result);
                    return result.ExitCode;
            }
        }
        catch (Exception error)
        {
            NCBOX.Core.Logging.TerminalConsole.WriteLine(
                new NCBOX.Core.Logging.TerminalMessage($"[Error] {error.Message}", NCBOX.Core.Logging.TerminalMessageKind.Error));
            return 1;
        }
    }
}
