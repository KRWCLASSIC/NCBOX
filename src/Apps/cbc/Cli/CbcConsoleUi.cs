using System;
using System.Reflection;
using cbc.Engine;
using NCBOX.Core.Logging;

namespace cbc.Cli;

internal static class CbcConsoleUi
{
    public static void ShowHelp()
    {
        Line(S("cbc", TerminalStyle.Magenta), S(" | "), S("NCBOX", TerminalStyle.BoldGray));
        Line(S("Usage:", TerminalStyle.Cyan));
        Line(S("  "), S("cbc", TerminalStyle.Cyan), S(" "), S("list", TerminalStyle.Yellow), S("                 List installed certificates, domains, and expiry"));
        Line(S("  "), S("cbc", TerminalStyle.Cyan), S(" "), S("inspect", TerminalStyle.Yellow), S(" "), S("<domain>", TerminalStyle.Green), S("     Inspect the certificate covering a domain"));
        Line(S("  "), S("cbc", TerminalStyle.Cyan), S(" "), S("create", TerminalStyle.Yellow), S(" "), S("<domain...>", TerminalStyle.Green), S("   Request a certificate using the Nginx plugin"));
        Line(S("  "), S("cbc", TerminalStyle.Cyan), S(" "), S("merge", TerminalStyle.Yellow), S(" "), S("<domain...>", TerminalStyle.Green), S("    Request one multi-domain certificate"));
        Line(S("  "), S("cbc", TerminalStyle.Cyan), S(" "), S("renew", TerminalStyle.Yellow), S("                Run a certificate renewal dry-run"));
        Line(S("  "), S("cbc", TerminalStyle.Cyan), S(" "), S("autorenew", TerminalStyle.Yellow), S("            Check or enable automatic renewal"));
        Line(S("  "), S("cbc", TerminalStyle.Cyan), S(" "), S("check", TerminalStyle.Yellow), S("                Check the Certbot installation"));
        Line(S("  "), S("cbc", TerminalStyle.Cyan), S(" "), S("remove", TerminalStyle.Yellow), S(" "), S("<domain>", TerminalStyle.Green), S("       Remove a certificate after confirmation"));
        Line(S("  "), S("cbc", TerminalStyle.Cyan), S(" "), S("version", TerminalStyle.Yellow), S("              Show the app version"));
        Line(S("  "), S("cbc", TerminalStyle.Cyan), S(" "), S("help", TerminalStyle.Yellow), S("                 Show this help"));
    }

    public static int ShowVersion()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";
        Write($"cbc v{version}");
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

    public static bool ConfirmRemoval(string domain)
    {
        Write($"Are you sure you want to delete the certificate for {domain}? (y/N)", TerminalMessageKind.Warning);
        return Console.ReadLine()?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static void ShowProgress(CbcAction action)
    {
        switch (action)
        {
            case ListAction:
                Write("Querying certificates via Certbot...", TerminalMessageKind.Info);
                break;
            case InspectAction item:
                Write($"Inspecting certificates covering {item.Domain}...", TerminalMessageKind.Info);
                break;
            case CreateAction item:
                Write($"Requesting certificate for: {string.Join(", ", item.Domains)} via --nginx plugin...", TerminalMessageKind.Info);
                break;
            case MergeAction item:
            {
                Write($"Planning consolidation for: {string.Join(", ", item.Domains)}...", TerminalMessageKind.Info);
                Write("This will request a replacement multi-domain (SAN) certificate.", TerminalMessageKind.Warning);
                string domainArgs = string.Join(" -d ", item.Domains);
                Write($"Executing: certbot certonly --nginx --cert-name \"{item.Domains[0]}\" --expand -d {domainArgs}", TerminalMessageKind.Info);
                break;
            }
            case CheckAction:
                Write("Checking Certbot installation...", TerminalMessageKind.Info);
                break;
            case AutoRenewAction:
                Write("Checking automatic renewal mechanism...", TerminalMessageKind.Info);
                break;
        }
    }

    public static void Render(CbcCommandResult result)
    {
        foreach (TerminalMessage message in result.Messages) TerminalConsole.WriteLine(message);
    }

    private static void Write(string text, TerminalMessageKind kind = TerminalMessageKind.Plain) =>
        TerminalConsole.WriteLine(new TerminalMessage(text, kind));

    private static void Line(params TerminalSegment[] segments) => TerminalConsole.WriteLine(segments);

    private static TerminalSegment S(string text, TerminalStyle style = TerminalStyle.Plain) => new(text, style);
}
