using System;
using NCBOX.Core.Config;
using NCBOX.Core.Logging;

namespace ngc.Engine;

internal sealed class NgcSettingsService
{
    private readonly NgcSettings _settings;

    public NgcSettingsService(NgcSettings settings) => _settings = settings;

    public NgcCommandResult SetPath(string target, string path)
    {
        if (target.Equals("av", StringComparison.OrdinalIgnoreCase))
        {
            _settings.SitesAvailable = path;
            _settings.Save();
            return NgcCommandResult.One(0, $"SITES_AVAILABLE set to {path}", TerminalMessageKind.Success);
        }
        if (target.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            _settings.SitesEnabled = path;
            _settings.Save();
            return NgcCommandResult.One(0, $"SITES_ENABLED set to {path}", TerminalMessageKind.Success);
        }
        return NgcCommandResult.One(1, "Usage: ngc set av|en <path>", TerminalMessageKind.Error);
    }
}
