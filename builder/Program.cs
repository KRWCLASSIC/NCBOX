using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NCBOX.Builder.Apps;
using NCBOX.Builder.Pipeline;

namespace NCBOX.Builder;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var ctx = ParseCommandLine(args);

        if (ctx.Command.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            PrintHelp();
            return 0;
        }

        PrintHeader(ctx);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            int result = ctx.Command switch
            {
                "project" => await BuildProjectCommandAsync(ctx).ConfigureAwait(false),
                "all" => await AppRegistry.BuildAllAsync(ctx).ConfigureAwait(false),
                "dev" => await AppRegistry.BuildAllAsync(ctx).ConfigureAwait(false),
                "clean" => Clean(ctx),
                "version" or "ver" => HandleVersionCommand(ctx),
                _ => HandleUnknownCommand(ctx.Command)
            };

            stopwatch.Stop();
            if (result == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[Success] Build pipeline completed successfully in {stopwatch.Elapsed.TotalSeconds:F2}s.\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Failed] Build pipeline failed with exit code {result}.\n");
                Console.ResetColor();
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Error] Unhandled build exception: {ex.Message}\n");
            Console.ResetColor();
            return 1;
        }
    }

    private static BuildContext ParseCommandLine(string[] args)
    {
        string rootDir = BuildContext.FindRepoRoot();
        string targetRid = BuildContext.GetDefaultRid();
        string? command = null;
        string? targetProject = null;
        bool isDebug = false;
        string? extraArg = null;

        for (int i = 0; i < args.Length; i++)
        {
            string token = args[i].Trim().Trim('"', '\'');
            if (string.IsNullOrEmpty(token)) continue;

            if (token.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                token == "/?")
            {
                return new BuildContext(rootDir, targetRid, false, "help", null, null);
            }

            if (token.Equals("clean", StringComparison.OrdinalIgnoreCase))
            {
                command = "clean";
                continue;
            }

            if (token.Equals("version", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("ver", StringComparison.OrdinalIgnoreCase))
            {
                command = "version";
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    extraArg = args[++i].Trim();
                }
                continue;
            }

            if (token.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                command = "all";
                continue;
            }

            if (token.Equals("dev", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("debug", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("-d", StringComparison.OrdinalIgnoreCase))
            {
                isDebug = true;
                if (command == null) command = "dev";
                continue;
            }

            // Check project:<Name> or -p:<Name>
            string? projCandidate = null;
            if (token.StartsWith("project:", StringComparison.OrdinalIgnoreCase))
            {
                projCandidate = token.Substring("project:".Length);
            }
            else if (token.StartsWith("-p:", StringComparison.OrdinalIgnoreCase))
            {
                projCandidate = token.Substring("-p:".Length);
            }
            else if (token.Equals("-p", StringComparison.OrdinalIgnoreCase) || token.Equals("--project", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    projCandidate = args[++i].Trim();
                }
            }

            if (projCandidate != null)
            {
                int subColon = projCandidate.IndexOf(':');
                if (subColon > 0)
                {
                    string subConfig = projCandidate.Substring(subColon + 1);
                    projCandidate = projCandidate.Substring(0, subColon);
                    if (subConfig.Equals("dev", StringComparison.OrdinalIgnoreCase) || subConfig.Equals("debug", StringComparison.OrdinalIgnoreCase))
                    {
                        isDebug = true;
                    }
                }

                command = "project";
                targetProject = projCandidate;
                continue;
            }

            if (token.StartsWith("win-", StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith("linux-", StringComparison.OrdinalIgnoreCase))
            {
                targetRid = token;
                continue;
            }
        }

        command ??= "all";
        return new BuildContext(rootDir, targetRid, isDebug, command, targetProject, extraArg);
    }

    private static void PrintHeader(BuildContext ctx)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine("                 NCBOX BUILD PIPELINE                    ");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        string displayCommand = ctx.TargetProject != null
            ? $"project:{ctx.TargetProject} (targeted)"
            : ctx.Command;

        Console.WriteLine($"  Target Command : \x1b[1;37m{displayCommand}\x1b[0m");
        Console.WriteLine($"  Target Runtime : \x1b[1;36m{ctx.TargetRid}\x1b[0m");
        Console.WriteLine($"  Configuration  : \x1b[1;35m{ctx.ConfigName} (NativeAOT)\x1b[0m");
        string optSummary = ctx.IsDebug ? "Debug Defaults" : $"{ctx.Config.Optimization.Preference} (Fold: {ctx.Config.Optimization.FoldIdenticalMethodBodies}, Trim: {ctx.Config.Optimization.TrimMetadata}, Strip: {ctx.Config.Optimization.StripSymbols}, Invariant: {ctx.Config.Optimization.InvariantGlobalization})";
        Console.WriteLine($"  Optimization   : \x1b[1;33m{optSummary}\x1b[0m");
        Console.WriteLine($"  Suite Version  : \x1b[1;32mv{GetCurrentVersion(ctx.RootDir)}\x1b[0m");
        Console.WriteLine($"  Target Platform: \x1b[1;34m.NET 10 NativeAOT\x1b[0m");
        Console.WriteLine($"  Repository Root: \x1b[90m{ctx.RootDir}\x1b[0m\n");
    }

    private static void PrintHelp()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine("                 NCBOX BUILD RUNNER                      ");
        Console.WriteLine("=========================================================");
        Console.ResetColor();
        Console.WriteLine("\nUsage: build [target] [options]\n");
        Console.WriteLine("Targets:");
        Console.WriteLine("  all                 Release NativeAOT build of all apps (ngc, cbc) [Default]");
        Console.WriteLine("  project:<name>      Targeted rebuild of single application (e.g. project:ngc, project:cbc)");
        Console.WriteLine("  dev                 Quick debug build for fast development/testing");
        Console.WriteLine("  clean               Clean all dist/, .temp/, bin/, and obj/ directories");
        Console.WriteLine("  version [new_ver]   Inspect or bump suite version in Directory.Build.props");
        Console.WriteLine("  help                Display this help screen\n");
        Console.WriteLine("Target Structure in dist/:");
        Console.WriteLine("  dist/");
        Console.WriteLine("    linux-x64/ (or win-x64/)");
        Console.WriteLine("      ngc           -> Standalone Nginx Configurator");
        Console.WriteLine("      cbc           -> Standalone Certbot Configurator");
        Console.WriteLine("      Intermediate/ -> All debug symbols (.pdb) and intermediate outputs\n");
    }

    private static async Task<int> BuildProjectCommandAsync(BuildContext ctx)
    {
        string name = ctx.TargetProject?.ToLowerInvariant() ?? string.Empty;

        if (AppRegistry.TryGet(name, out var app) && app != null)
        {
            int res = await app.BuildAsync(ctx).ConfigureAwait(false);
            if (res == 0)
            {
                DistManager.PrintStructureTree(ctx);
            }
            return res;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Error] Unknown project '{ctx.TargetProject}'. Available apps: {string.Join(", ", AppRegistry.All.Select(a => a.Name))}");
        Console.ResetColor();
        return 1;
    }

    private static int Clean(BuildContext ctx)
    {
        Console.WriteLine("\x1b[1;36m>>> Cleaning artifacts and build output directories...\x1b[0m\n");

        string[] dirsToClean =
        [
            Path.Combine(ctx.RootDir, "dist"),
            Path.Combine(ctx.RootDir, ".temp")
        ];

        foreach (var dir in dirsToClean)
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                    Console.WriteLine($"\x1b[1;32m[Removed]\x1b[0m {Path.GetRelativePath(ctx.RootDir, dir)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\x1b[1;33m[Warning]\x1b[0m Could not delete {dir}: {ex.Message}");
                }
            }
        }

        CleanBinAndObj(Path.Combine(ctx.RootDir, "src"), ctx.RootDir);
        CleanBinAndObj(Path.Combine(ctx.RootDir, "builder"), ctx.RootDir);

        Console.WriteLine("\n\x1b[1;32m[OK]\x1b[0m Clean complete.");
        return 0;
    }

    private static void CleanBinAndObj(string startDir, string rootDir)
    {
        if (!Directory.Exists(startDir)) return;

        foreach (var sub in Directory.GetDirectories(startDir, "*", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(sub);
            if (name.Equals("bin", StringComparison.OrdinalIgnoreCase) || name.Equals("obj", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Directory.Delete(sub, recursive: true);
                    Console.WriteLine($"\x1b[1;32m[Removed]\x1b[0m {Path.GetRelativePath(rootDir, sub)}");
                }
                catch { }
            }
        }
    }

    private static string GetCurrentVersion(string rootDir)
    {
        string propsPath = Path.Combine(rootDir, "Directory.Build.props");
        if (File.Exists(propsPath))
        {
            var content = File.ReadAllText(propsPath);
            var m = Regex.Match(content, @"<Version>(.*?)</Version>");
            if (m.Success && !string.IsNullOrWhiteSpace(m.Groups[1].Value))
            {
                return m.Groups[1].Value.Trim();
            }
        }
        return "1.0.0";
    }

    private static int HandleVersionCommand(BuildContext ctx)
    {
        string propsPath = Path.Combine(ctx.RootDir, "Directory.Build.props");
        if (!File.Exists(propsPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[Error] Directory.Build.props not found.");
            Console.ResetColor();
            return 1;
        }

        string currentVer = GetCurrentVersion(ctx.RootDir);

        if (string.IsNullOrWhiteSpace(ctx.ExtraArg))
        {
            Console.WriteLine($"\nCurrent NCBOX Suite Version: \x1b[1;32mv{currentVer}\x1b[0m\n");
            return 0;
        }

        string newVer = ctx.ExtraArg.TrimStart('v');
        if (!Regex.IsMatch(newVer, @"^\d+\.\d+\.\d+.*$"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error] Invalid version format '{newVer}'. Expected format: X.Y.Z (e.g. 1.2.0)");
            Console.ResetColor();
            return 1;
        }

        string content = File.ReadAllText(propsPath);
        content = Regex.Replace(content, @"<Version>.*?</Version>", $"<Version>{newVer}</Version>");
        content = Regex.Replace(content, @"<AssemblyVersion>.*?</AssemblyVersion>", $"<AssemblyVersion>{newVer.Split('-')[0]}.0</AssemblyVersion>");
        content = Regex.Replace(content, @"<FileVersion>.*?</FileVersion>", $"<FileVersion>{newVer.Split('-')[0]}.0</FileVersion>");

        File.WriteAllText(propsPath, content);
        Console.WriteLine($"\n\x1b[1;32m[Updated]\x1b[0m NCBOX Version: \x1b[1;33mv{currentVer}\x1b[0m -> \x1b[1;32mv{newVer}\x1b[0m\n");
        return 0;
    }

    private static int HandleUnknownCommand(string cmd)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Error] Unknown command '{cmd}'. Run 'build help' for usage.");
        Console.ResetColor();
        return 1;
    }
}
