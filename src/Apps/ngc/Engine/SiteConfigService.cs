using System;
using System.IO;
using NCBOX.Core.Config;
using NCBOX.Core.Logging;
using NCBOX.Core.Platform;
using NCBOX.Core.Process;
using NCBOX.Core.System;

namespace ngc.Engine;

internal sealed class SiteConfigService
{
    private readonly NgcSettings _settings;

    public SiteConfigService(NgcSettings settings) => _settings = settings;

    public NgcCommandResult Edit(string domain)
    {
        Directory.CreateDirectory(_settings.SitesAvailable);
        string config = Path.Combine(_settings.SitesAvailable, domain);
        string editor = Environment.GetEnvironmentVariable("EDITOR") ?? (PlatformInfo.IsWindows ? "notepad.exe" : "nano");
        return NgcCommandResult.Empty(ProcessHelper.RunInteractive(editor, $"\"{config}\""));
    }

    public NgcCommandResult Remove(string domain)
    {
        string config = Path.Combine(_settings.SitesAvailable, domain);
        string backup = $"{config}.bak";
        string link = Path.Combine(_settings.SitesEnabled, domain);
        if (!File.Exists(config)) return NgcCommandResult.One(1, $"Config file {config} does not exist.", TerminalMessageKind.Error);

        if (File.Exists(backup)) File.Delete(backup);
        File.Move(config, backup);
        var messages = new System.Collections.Generic.List<TerminalMessage>
        {
            new($"Backed up {config} to {backup}", TerminalMessageKind.Success)
        };
        if (SymlinkHelper.IsSymlink(link))
        {
            SymlinkHelper.DeleteSymlink(link);
            messages.Add(new TerminalMessage($"Removed symlink {link}", TerminalMessageKind.Success));
        }
        else
        {
            messages.Add(new TerminalMessage($"Symlink {link} does not exist.", TerminalMessageKind.Error));
        }
        return new NgcCommandResult(0, messages);
    }

    public NgcCommandResult Restore(string domain)
    {
        string backup = Path.Combine(_settings.SitesAvailable, $"{domain}.bak");
        string config = Path.Combine(_settings.SitesAvailable, domain);
        if (!File.Exists(backup)) return NgcCommandResult.One(1, $"Backup file {backup} does not exist.", TerminalMessageKind.Error);
        if (File.Exists(config)) File.Delete(config);
        File.Move(backup, config);
        return NgcCommandResult.One(0, $"Restored {config} from {backup}", TerminalMessageKind.Success);
    }

    public NgcCommandResult Rename(string oldDomain, string newDomain)
    {
        string oldConfig = Path.Combine(_settings.SitesAvailable, oldDomain);
        string newConfig = Path.Combine(_settings.SitesAvailable, newDomain);
        string oldLink = Path.Combine(_settings.SitesEnabled, oldDomain);
        string newLink = Path.Combine(_settings.SitesEnabled, newDomain);
        if (!File.Exists(oldConfig)) return NgcCommandResult.One(1, $"Config file {oldConfig} does not exist.", TerminalMessageKind.Error);
        if (File.Exists(newConfig)) return NgcCommandResult.One(1, $"Config file {newConfig} already exists. Aborting to avoid overwriting.", TerminalMessageKind.Error);
        if (SymlinkHelper.IsSymlink(newLink) || File.Exists(newLink)) return NgcCommandResult.One(1, $"Symlink {newLink} already exists. Aborting to avoid overwriting.", TerminalMessageKind.Error);

        File.Move(oldConfig, newConfig);
        var messages = new System.Collections.Generic.List<TerminalMessage>
        {
            new($"Renamed {oldConfig} to {newConfig}", TerminalMessageKind.Success)
        };
        if (SymlinkHelper.IsSymlink(oldLink))
        {
            SymlinkHelper.DeleteSymlink(oldLink);
            messages.Add(new TerminalMessage($"Removed old symlink {oldLink}", TerminalMessageKind.Success));
        }
        messages.Add(new TerminalMessage($"To apply changes, run: 'ngc enable {newDomain}' or 'ngc fullrun'", TerminalMessageKind.Warning));
        return new NgcCommandResult(0, messages);
    }
}
