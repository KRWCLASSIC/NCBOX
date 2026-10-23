using System;
using System.Reflection;
using NCBOX.Core.Logging;
using ngc.Engine;

namespace ngc.Cli;

internal static class NgcConsoleUi
{
    public static void ShowHelp()
    {
        Line(S("ngc", TerminalStyle.Magenta), S(" | "), S("NCBOX", TerminalStyle.BoldGray));
        Line(S("Usage:", TerminalStyle.Cyan));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("-n", TerminalStyle.Yellow), S(" "), S("<domain>", TerminalStyle.Green), S("       Open or create a site config in the configured editor"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("run", TerminalStyle.Yellow), S("               Test configs and reload Nginx "), S("(starts it if stopped)", TerminalStyle.BoldGray));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("fullrun", TerminalStyle.Yellow), S("           Ensure site links, test configs, and reload Nginx"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("rm", TerminalStyle.Yellow), S(" "), S("<domain>", TerminalStyle.Green), S("       Back up and remove a site config and its link"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("list", TerminalStyle.Yellow), S("              List site configs and link status"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("listbak", TerminalStyle.Yellow), S("           List backed-up configs "), S("(.bak)", TerminalStyle.BoldGray));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("restore", TerminalStyle.Yellow), S(" "), S("<domain>", TerminalStyle.Green), S("  Restore a config from its backup"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("ren", TerminalStyle.Yellow), S(" "), S("<old> <new>", TerminalStyle.Green), S("   Rename a site config"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("enable", TerminalStyle.Yellow), S(" "), S("<domain>", TerminalStyle.Green), S("   Enable a site by creating its link"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("disable", TerminalStyle.Yellow), S(" "), S("<domain>", TerminalStyle.Green), S("  Disable a site by removing its link"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("clean", TerminalStyle.Yellow), S("             Clear enabled entries and relink available configs"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("ls", TerminalStyle.Yellow), S(" "), S("av|en", TerminalStyle.Magenta), S("             List available or enabled configs and their path"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("set", TerminalStyle.Yellow), S(" "), S("av|en", TerminalStyle.Magenta), S(" "), S("<path>", TerminalStyle.Green), S("   Set an available or enabled config path"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("test", TerminalStyle.Yellow), S("                Test configs with nginx -t"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("version", TerminalStyle.Yellow), S("             Show the app version"));
        Line(S("  "), S("ngc", TerminalStyle.Cyan), S(" "), S("help", TerminalStyle.Yellow), S("                Show this help"));
    }

    public static int ShowVersion()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";
        Write($"ngc v{version}");
        return 0;
    }

    public static int MissingArgument(string expected)
    {
        Write($"[Error] Missing required argument: {expected}", TerminalMessageKind.Error);
        ShowHelp();
        return 1;
    }

    public static int UnknownCommand(string command)
    {
        Write($"[Error] Unknown command: {command}", TerminalMessageKind.Error);
        ShowHelp();
        return 1;
    }

    public static void ShowProgress(NgcAction action)
    {
        switch (action)
        {
            case RunAction:
            case TestAction:
                Write("Testing all configs with nginx -t...", TerminalMessageKind.Info);
                break;
            case FullRunAction:
                Write("Ensuring symlinks exist for all configs...", TerminalMessageKind.Success);
                Write("Testing all configs with nginx -t...", TerminalMessageKind.Info);
                break;
            case CleanAction:
                Write("Cleaning enabled site entries before relinking...", TerminalMessageKind.Warning);
                break;
        }
    }

    public static void Render(NgcCommandResult result)
    {
        foreach (TerminalMessage message in result.Messages) TerminalConsole.WriteLine(message);
    }

    private static void Line(params TerminalSegment[] segments) => TerminalConsole.WriteLine(segments);

    private static TerminalSegment S(string text, TerminalStyle style = TerminalStyle.Plain) => new(text, style);

    private static void Write(string text, TerminalMessageKind kind = TerminalMessageKind.Plain) =>
        TerminalConsole.WriteLine(new TerminalMessage(text, kind));
}
