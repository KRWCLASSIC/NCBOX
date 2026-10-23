using System;
using System.IO;
using System.Threading.Tasks;
using NCBOX.Builder.Pipeline;

namespace NCBOX.Builder.Apps;

public sealed class NgcAppBuilder : IAppBuilder
{
    public string Name => "ngc";
    public string Description => "Nginx configuration generator and site manager";

    public async Task<int> BuildAsync(BuildContext ctx)
    {
        Console.WriteLine($"\n\x1b[1;36m>>> Building ngc CLI (Runtime: {ctx.TargetRid}, Mode: {ctx.ConfigName})...\x1b[0m");

        string ngcCsproj = Path.Combine(ctx.RootDir, "src", "Apps", "ngc", "ngc.csproj");
        string stagingDir = Path.Combine(ctx.TempStagingDir, "ngc");

        if (Directory.Exists(stagingDir))
        {
            try { Directory.Delete(stagingDir, recursive: true); } catch { }
        }
        Directory.CreateDirectory(stagingDir);

        Console.WriteLine("\x1b[1;33m[App] Compiling ngc CLI (NativeAOT)...\x1b[0m");

        int exitCode = await DotnetPublisher.PublishAsync(
            ctx,
            ngcCsproj,
            stagingDir,
            additionalProperties: null,
            noRestore: false).ConfigureAwait(false);

        if (exitCode != 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error] Failed to compile ngc CLI. Exit code: {exitCode}");
            Console.ResetColor();
            return exitCode;
        }

        string exeName = $"ngc{ctx.ExeExtension}";
        string finalExe = DistManager.DeployApp(ctx, stagingDir, exeName);

        if (File.Exists(finalExe))
        {
            long size = new FileInfo(finalExe).Length;
            Console.WriteLine($"\x1b[1;32m[SUCCESS]\x1b[0m ngc binary ready:");
            Console.WriteLine($"  Path: \x1b[1;37m{Path.GetRelativePath(ctx.RootDir, finalExe)}\x1b[0m");
            Console.WriteLine($"  Size: \x1b[1;32m{size / (1024.0 * 1024.0):F2} MB\x1b[0m");
        }

        return 0;
    }
}
