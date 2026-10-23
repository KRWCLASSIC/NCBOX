using System.Threading.Tasks;
using ngc.Cli;
using NCBOX.Core.Config;

namespace ngc.Engine;

internal sealed class NgcCommandExecutor
{
    private readonly SiteConfigService _sites;
    private readonly SiteCatalogService _catalog;
    private readonly NgcSettingsService _settings;
    private readonly SiteLinkService _links;
    private readonly NginxService _nginx;

    public NgcCommandExecutor(NgcSettings settings)
    {
        _nginx = new NginxService();
        _sites = new SiteConfigService(settings);
        _catalog = new SiteCatalogService(settings, _nginx);
        _settings = new NgcSettingsService(settings);
        _links = new SiteLinkService(settings.SitesAvailable, settings.SitesEnabled, _nginx);
    }

    public Task<NgcCommandResult> ExecuteAsync(NgcAction action) => action switch
    {
        EditAction item => Task.FromResult(_sites.Edit(item.Domain)),
        RunAction => _nginx.TestAndReloadAsync(),
        FullRunAction => _links.RunAllAsync(),
        RemoveAction item => Task.FromResult(_sites.Remove(item.Domain)),
        ListAction => _catalog.ListAsync(),
        ListBakAction => Task.FromResult(_catalog.ListBackups()),
        RestoreAction item => Task.FromResult(_sites.Restore(item.Domain)),
        RenameAction item => Task.FromResult(_sites.Rename(item.OldDomain, item.NewDomain)),
        EnableAction item => Task.FromResult(_links.Enable(item.Domain)),
        DisableAction item => Task.FromResult(_links.Disable(item.Domain)),
        CleanAction => _links.CleanAsync(),
        ListFolderAction item => Task.FromResult(_catalog.ListFolder(item.FolderType)),
        SetPathAction item => Task.FromResult(_settings.SetPath(item.Target, item.Path)),
        TestAction => _nginx.TestAsync(),
        _ => Task.FromResult(NgcCommandResult.Empty(1))
    };
}
