using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NCBOX.Core.Logging;
using NCBOX.Core.Platform;
using NCBOX.Core.Process;

namespace ngc.Engine;

internal sealed class NginxService
{
    public async Task<NgcCommandResult> TestAsync()
    {
        var messages = new List<TerminalMessage>();
        var result = await ProcessHelper.RunAsync("nginx", "-t").ConfigureAwait(false);
        messages.Add(result.Success
            ? new TerminalMessage("Nginx configuration test passed.", TerminalMessageKind.Success)
            : new TerminalMessage(Combine("Config test failed. Check output below:", result.StandardError), TerminalMessageKind.Error));
        return new NgcCommandResult(result.Success ? 0 : 1, messages);
    }

    public async Task<NgcCommandResult> TestAndReloadAsync()
    {
        var messages = new List<TerminalMessage>();
        var testResult = await ProcessHelper.RunAsync("nginx", "-t").ConfigureAwait(false);
        if (!testResult.Success)
        {
            messages.Add(new TerminalMessage("Config test failed. Check output above.", TerminalMessageKind.Error));
            AddOutput(messages, testResult.StandardError, TerminalMessageKind.Plain);
            return new NgcCommandResult(1, messages);
        }

        messages.Add(new TerminalMessage("All configs valid. Reloading Nginx...", TerminalMessageKind.Success));
        if (!PlatformInfo.IsLinux)
        {
            messages.Add(new TerminalMessage("Non-Linux platform: Run 'nginx -s reload' to apply changes.", TerminalMessageKind.Info));
            return new NgcCommandResult(0, messages);
        }

        var reloadResult = await ProcessHelper.RunAsync("systemctl", "reload nginx").ConfigureAwait(false);
        if (reloadResult.Success) messages.Add(new TerminalMessage("Nginx reloaded.", TerminalMessageKind.Success));

        var statusResult = await ProcessHelper.RunAsync("systemctl", "is-active nginx").ConfigureAwait(false);
        if (!statusResult.StandardOutput.Trim().Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            messages.Add(new TerminalMessage("Nginx is not running. Attempting to start...", TerminalMessageKind.Error));
            var startResult = await ProcessHelper.RunAsync("systemctl", "start nginx").ConfigureAwait(false);
            if (startResult.Success) messages.Add(new TerminalMessage("Nginx started.", TerminalMessageKind.Success));
        }

        return new NgcCommandResult(0, messages);
    }

    public async Task<TerminalMessage?> GetStatusAsync()
    {
        if (!PlatformInfo.IsLinux) return null;
        var result = await ProcessHelper.RunAsync("systemctl", "is-active nginx").ConfigureAwait(false);
        string status = result.StandardOutput.Trim();
        var kind = status.Equals("active", StringComparison.OrdinalIgnoreCase)
            ? TerminalMessageKind.Success
            : TerminalMessageKind.Error;
        return new TerminalMessage($"Nginx service status: {status}", kind);
    }

    private static string Combine(string heading, string detail) =>
        string.IsNullOrWhiteSpace(detail) ? heading : $"{heading}{Environment.NewLine}{detail.TrimEnd()}";

    private static void AddOutput(List<TerminalMessage> messages, string output, TerminalMessageKind kind)
    {
        if (!string.IsNullOrWhiteSpace(output)) messages.Add(new TerminalMessage(output.TrimEnd(), kind));
    }
}
