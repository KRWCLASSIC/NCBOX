using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NCBOX.Core.Logging;
using NCBOX.Core.System;

namespace ngc.Engine;

internal sealed class SiteLinkService
{
    private readonly string _sitesAvailable;
    private readonly string _sitesEnabled;
    private readonly NginxService _nginx;

    public SiteLinkService(string sitesAvailable, string sitesEnabled, NginxService nginx)
    {
        _sitesAvailable = sitesAvailable;
        _sitesEnabled = sitesEnabled;
        _nginx = nginx;
    }

    public NgcCommandResult Enable(string domain)
    {
        string config = Path.Combine(_sitesAvailable, domain);
        string link = Path.Combine(_sitesEnabled, domain);
        if (!File.Exists(config)) return NgcCommandResult.One(1, $"Config file {config} does not exist.", TerminalMessageKind.Error);

        Directory.CreateDirectory(_sitesEnabled);
        if (SymlinkHelper.IsSymlink(link)) return NgcCommandResult.One(0, $"Symlink for {domain} already exists.", TerminalMessageKind.Success);
        if (File.Exists(link)) return NgcCommandResult.One(1, $"{link} exists and is not a symlink. Please resolve manually.", TerminalMessageKind.Error);

        SymlinkHelper.CreateSymlink(link, config);
        return NgcCommandResult.One(0, $"Enabled {domain} (symlink created)", TerminalMessageKind.Success);
    }

    public NgcCommandResult Disable(string domain)
    {
        string link = Path.Combine(_sitesEnabled, domain);
        if (!SymlinkHelper.IsSymlink(link)) return NgcCommandResult.One(1, $"Symlink {link} does not exist.", TerminalMessageKind.Error);
        SymlinkHelper.DeleteSymlink(link);
        return NgcCommandResult.One(0, $"Disabled {domain} (symlink removed)", TerminalMessageKind.Success);
    }

    public async Task<NgcCommandResult> CleanAsync()
    {
        if (!Directory.Exists(_sitesEnabled))
            return NgcCommandResult.One(1, $"Enabled folder {_sitesEnabled} does not exist. Aborting.", TerminalMessageKind.Error);

        var messages = new List<TerminalMessage>();
        foreach (string file in Directory.GetFiles(_sitesEnabled)) SymlinkHelper.DeleteSymlink(file);
        messages.Add(new TerminalMessage($"All files removed from {_sitesEnabled}.", TerminalMessageKind.Success));
        NgcCommandResult runResult = await RunAllAsync().ConfigureAwait(false);
        messages.AddRange(runResult.Messages);
        return new NgcCommandResult(runResult.ExitCode, messages);
    }

    public async Task<NgcCommandResult> RunAllAsync()
    {
        var messages = new List<TerminalMessage>();
        if (!Directory.Exists(_sitesAvailable))
        {
            messages.Add(new TerminalMessage($"Available folder {_sitesAvailable} does not exist. Aborting.", TerminalMessageKind.Error));
            return new NgcCommandResult(1, messages);
        }
        if (!Directory.Exists(_sitesEnabled))
        {
            messages.Add(new TerminalMessage($"Enabled folder {_sitesEnabled} does not exist. Aborting.", TerminalMessageKind.Error));
            return new NgcCommandResult(1, messages);
        }

        foreach (string config in Directory.GetFiles(_sitesAvailable))
        {
            string domain = Path.GetFileName(config);
            if (domain.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) || domain.StartsWith('.')) continue;
            string link = Path.Combine(_sitesEnabled, domain);

            if (SymlinkHelper.IsSymlink(link))
            {
                string? target = SymlinkHelper.GetLinkTarget(link);
                if (string.Equals(target, config, StringComparison.OrdinalIgnoreCase)) continue;
                SymlinkHelper.DeleteSymlink(link);
                SymlinkHelper.CreateSymlink(link, config);
                messages.Add(new TerminalMessage($"Fixed symlink for {domain}"));
            }
            else if (File.Exists(link))
            {
                messages.Add(new TerminalMessage($"{link} exists and is not a symlink. Please resolve manually.", TerminalMessageKind.Error));
            }
            else
            {
                SymlinkHelper.CreateSymlink(link, config);
                messages.Add(new TerminalMessage($"Linked {domain}", TerminalMessageKind.Success));
            }
        }

        NgcCommandResult nginxResult = await _nginx.TestAndReloadAsync().ConfigureAwait(false);
        messages.AddRange(nginxResult.Messages);
        return new NgcCommandResult(nginxResult.ExitCode, messages);
    }
}
