using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NCBOX.Core.Config;
using NCBOX.Core.Logging;
using NCBOX.Core.System;

namespace ngc.Engine;

internal sealed class SiteCatalogService
{
    private readonly NgcSettings _settings;
    private readonly NginxService _nginx;

    public SiteCatalogService(NgcSettings settings, NginxService nginx)
    {
        _settings = settings;
        _nginx = nginx;
    }

    public async Task<NgcCommandResult> ListAsync()
    {
        var messages = new List<TerminalMessage>
        {
            new("Nginx config overview:", TerminalMessageKind.Success)
        };
        if (!Directory.Exists(_settings.SitesAvailable))
        {
            messages.Add(new TerminalMessage("No configs found (available folder missing)", TerminalMessageKind.Error));
            return new NgcCommandResult(0, messages);
        }

        int found = 0;
        foreach (string file in Directory.GetFiles(_settings.SitesAvailable))
        {
            string domain = Path.GetFileName(file);
            if (domain.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) || domain.StartsWith('.')) continue;
            found++;
            string link = Path.Combine(_settings.SitesEnabled, domain);
            bool linked = SymlinkHelper.IsSymlink(link);
            messages.Add(TerminalMessage.FromSegments(
                new TerminalSegment(domain, TerminalStyle.Yellow),
                new TerminalSegment(": "),
                new TerminalSegment(linked ? "Linked" : "Not linked", linked ? TerminalStyle.Green : TerminalStyle.Red)));
        }
        if (found == 0) messages.Add(new TerminalMessage("No configs found.", TerminalMessageKind.Error));
        TerminalMessage? status = await _nginx.GetStatusAsync().ConfigureAwait(false);
        if (status is not null) messages.Add(status);
        return new NgcCommandResult(0, messages);
    }

    public NgcCommandResult ListBackups()
    {
        var messages = new List<TerminalMessage>
        {
            new("Backed up Nginx configs:", TerminalMessageKind.Success)
        };
        if (!Directory.Exists(_settings.SitesAvailable))
        {
            messages.Add(new TerminalMessage("No backups found (available folder missing)", TerminalMessageKind.Error));
            return new NgcCommandResult(0, messages);
        }
        string[] files = Directory.GetFiles(_settings.SitesAvailable, "*.bak");
        if (files.Length == 0)
        {
            messages.Add(new TerminalMessage("No backups found.", TerminalMessageKind.Error));
            return new NgcCommandResult(0, messages);
        }
        foreach (string file in files)
        {
            string domain = Path.GetFileNameWithoutExtension(file);
            messages.Add(TerminalMessage.FromSegments(
                new TerminalSegment(domain, TerminalStyle.Yellow),
                new TerminalSegment(": "),
                new TerminalSegment("Backed up", TerminalStyle.Green)));
        }
        return new NgcCommandResult(0, messages);
    }

    public NgcCommandResult ListFolder(string folderType)
    {
        string folder;
        string label;
        if (folderType.Equals("av", StringComparison.OrdinalIgnoreCase))
        {
            folder = _settings.SitesAvailable;
            label = "Available folder";
        }
        else if (folderType.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            folder = _settings.SitesEnabled;
            label = "Enabled folder";
        }
        else return NgcCommandResult.One(1, "Usage: ngc ls av|en", TerminalMessageKind.Error);

        var messages = new List<TerminalMessage>
        {
            TerminalMessage.FromSegments(
                new TerminalSegment(label, TerminalStyle.BoldGray),
                new TerminalSegment(": "),
                new TerminalSegment(folder))
        };
        if (!Directory.Exists(folder))
        {
            messages.Add(new TerminalMessage($"Folder does not exist: {folder}", TerminalMessageKind.Error));
            return new NgcCommandResult(1, messages);
        }
        foreach (string file in Directory.GetFiles(folder))
        {
            var info = new FileInfo(file);
            string name = Path.GetFileName(file);
            string? target = SymlinkHelper.GetLinkTarget(file);
            if (!string.IsNullOrEmpty(target))
                messages.Add(TerminalMessage.FromSegments(
                    new TerminalSegment($"  {name}", TerminalStyle.Cyan),
                    new TerminalSegment($" -> {target}")));
            else
                messages.Add(new TerminalMessage($"  {name} ({info.Length} bytes, modified {info.LastWriteTime:yyyy-MM-dd HH:mm})"));
        }
        return new NgcCommandResult(0, messages);
    }
}
