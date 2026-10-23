using System.IO;
using System.Threading.Tasks;
using cbc.Engine;
using NCBOX.Core.Logging;
using NCBOX.Core.Platform;
using NCBOX.Core.Process;

namespace cbc.Services;

internal sealed class RenewalSchedulerService
{
    public async Task<CbcCommandResult> EnsureAsync()
    {
        if (!PlatformInfo.IsLinux)
            return CbcCommandResult.One(0, "Windows platform: Scheduled task configuration is planned.", TerminalMessageKind.Warning);

        var timer = await ProcessHelper.RunAsync("systemctl", "is-active certbot.timer").ConfigureAwait(false);
        if (timer.StandardOutput.Trim() == "active")
            return CbcCommandResult.One(0, "[OK] certbot.timer is active and running.", TerminalMessageKind.Success);

        var enable = await ProcessHelper.RunAsync("systemctl", "enable --now certbot.timer").ConfigureAwait(false);
        if (enable.Success)
            return CbcCommandResult.One(0, "[OK] certbot.timer enabled successfully.", TerminalMessageKind.Success);

        if (File.Exists("/etc/cron.d/certbot"))
            return CbcCommandResult.One(0, "[OK] Cron job /etc/cron.d/certbot exists.", TerminalMessageKind.Success);

        return CbcCommandResult.One(1, "No active systemd timer or cron job found for Certbot renewal.", TerminalMessageKind.Error);
    }
}
