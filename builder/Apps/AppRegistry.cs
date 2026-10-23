using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NCBOX.Builder.Pipeline;

namespace NCBOX.Builder.Apps;

public static class AppRegistry
{
    private static readonly Dictionary<string, IAppBuilder> _registry =
        new(StringComparer.OrdinalIgnoreCase);

    static AppRegistry()
    {
        Register(new NgcAppBuilder());
        Register(new CbcAppBuilder());
    }

    public static void Register(IAppBuilder app)
    {
        _registry[app.Name] = app;
    }

    public static bool TryGet(string name, out IAppBuilder? app) =>
        _registry.TryGetValue(name, out app);

    public static IReadOnlyCollection<IAppBuilder> All => _registry.Values;

    public static async Task<int> BuildAllAsync(BuildContext ctx)
    {
        Console.WriteLine("\x1b[1;36m>>> Starting full NCBOX suite compilation...\x1b[0m\n");

        foreach (var app in All)
        {
            int res = await app.BuildAsync(ctx).ConfigureAwait(false);
            if (res != 0)
            {
                return res;
            }
        }

        DistManager.PrintStructureTree(ctx);
        return 0;
    }
}
