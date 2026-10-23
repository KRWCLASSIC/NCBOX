using System.Threading.Tasks;
using cbc.Cli;
using cbc.Services;

namespace cbc.Engine;

internal sealed class CbcCommandExecutor
{
    private readonly CertbotService _certbot = new();
    private readonly RenewalSchedulerService _renewal = new();

    public Task<CbcCommandResult> ExecuteAsync(CbcAction action, bool removalConfirmed = false) => action switch
    {
        ListAction => _certbot.ListAsync(),
        InspectAction item => _certbot.InspectAsync(item.Domain),
        CreateAction item => Task.FromResult(_certbot.Create(item.Domains)),
        MergeAction item => Task.FromResult(_certbot.Merge(item.Domains)),
        RenewAction => Task.FromResult(_certbot.Renew()),
        AutoRenewAction => _renewal.EnsureAsync(),
        CheckAction => _certbot.CheckHealthAsync(),
        RemoveAction item => Task.FromResult(removalConfirmed
            ? _certbot.Remove(item.Domain)
            : CbcCommandResult.One(0, "Aborted.", NCBOX.Core.Logging.TerminalMessageKind.Info)),
        _ => Task.FromResult(CbcCommandResult.Empty(1))
    };
}
