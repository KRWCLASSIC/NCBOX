using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NCBOX.Builder.Pipeline;

public static class DotnetPublisher
{
    public static async Task<int> PublishAsync(
        BuildContext ctx,
        string csprojPath,
        string outputDirectory,
        IReadOnlyDictionary<string, string>? additionalProperties = null,
        bool noRestore = false)
    {
        var args = new List<string>
        {
            "publish",
            $"\"{csprojPath}\"",
            "--nologo",
            "-tl:off",
            "-c", ctx.ConfigName,
            "-r", ctx.TargetRid,
            "--self-contained", "true",
            $"-o \"{outputDirectory}\""
        };

        if (noRestore)
        {
            args.Add("--no-restore");
        }

        var configProps = ctx.Config.ToMsBuildProperties(ctx.IsDebug);
        foreach (var kvp in configProps)
        {
            args.Add($"-p:{kvp.Key}=\"{kvp.Value}\"");
        }

        if (additionalProperties != null)
        {
            foreach (var kvp in additionalProperties)
            {
                args.Add($"-p:{kvp.Key}=\"{kvp.Value}\"");
            }
        }

        return await ProcessRunner.RunAsync("dotnet", string.Join(" ", args), ctx.RootDir).ConfigureAwait(false);
    }
}
