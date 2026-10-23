using System.Threading.Tasks;
using cbc.Engine;
using NCBOX.Core.Logging;
using NCBOX.Core.Process;

namespace cbc.Services;

internal sealed class CertbotService
{
    public async Task<CbcCommandResult> CheckHealthAsync()
    {
        var result = await ProcessHelper.RunAsync("certbot", "--version").ConfigureAwait(false);
        if (result.Success)
            return CbcCommandResult.One(0, $"[OK] Certbot found: {result.StandardOutput.Trim()}", TerminalMessageKind.Success);
        return CbcCommandResult.One(1, "[Failed] Certbot is not installed or not in system PATH.", TerminalMessageKind.Error);
    }

    public async Task<CbcCommandResult> ListAsync()
    {
        var result = await ProcessHelper.RunAsync("certbot", "certificates").ConfigureAwait(false);
        if (result.Success) return CbcCommandResult.One(0, result.StandardOutput.TrimEnd());
        return CbcCommandResult.One(1, $"Failed to query certificates:{System.Environment.NewLine}{result.StandardError.TrimEnd()}", TerminalMessageKind.Error);
    }

    public async Task<CbcCommandResult> InspectAsync(string domain)
    {
        var result = await ProcessHelper.RunAsync("certbot", $"certificates --cert-name \"{domain}\"").ConfigureAwait(false);
        if (result.Success && !string.IsNullOrWhiteSpace(result.StandardOutput))
            return CbcCommandResult.One(0, result.StandardOutput.TrimEnd());

        CbcCommandResult listResult = await ListAsync().ConfigureAwait(false);
        var messages = new System.Collections.Generic.List<TerminalMessage>
        {
            new($"No dedicated certificate lineage found for {domain}. Showing full list.", TerminalMessageKind.Warning)
        };
        messages.AddRange(listResult.Messages);
        return new CbcCommandResult(listResult.ExitCode, messages);
    }

    public CbcCommandResult Create(string[] domains)
    {
        string domainArgs = string.Join(" -d ", domains);
        int exitCode = ProcessHelper.RunInteractive("certbot", $"certonly --nginx -d {domainArgs}");
        return CbcCommandResult.Empty(exitCode);
    }

    public CbcCommandResult Merge(string[] domains)
    {
        string primaryDomain = domains[0];
        string domainArgs = string.Join(" -d ", domains);
        string certbotArgs = $"certonly --nginx --cert-name \"{primaryDomain}\" --expand -d {domainArgs}";
        int exitCode = ProcessHelper.RunInteractive("certbot", certbotArgs);
        return CbcCommandResult.Empty(exitCode);
    }

    public CbcCommandResult Renew() => CbcCommandResult.Empty(ProcessHelper.RunInteractive("certbot", "renew --dry-run"));

    public CbcCommandResult Remove(string domain) =>
        CbcCommandResult.Empty(ProcessHelper.RunInteractive("certbot", $"delete --cert-name \"{domain}\""));
}
