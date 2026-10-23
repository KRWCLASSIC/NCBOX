using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NCBOX.Builder.Pipeline;

public sealed class BuildContext
{
    public string RootDir { get; }
    public string TargetRid { get; }
    public bool IsDebug { get; }
    public string Command { get; }
    public string? TargetProject { get; }
    public string? ExtraArg { get; }
    public CompileConfig Config { get; }

    public string ConfigName => IsDebug ? "Debug" : "Release";

    public string DistDir => Path.Combine(RootDir, "dist", TargetRid);
    public string IntermediateDir => Path.Combine(DistDir, "Intermediate");
    public string TempStagingDir => Path.Combine(RootDir, ".temp", "staging", TargetRid);

    public bool IsTargetWindows => TargetRid.StartsWith("win-", StringComparison.OrdinalIgnoreCase);
    public string ExeExtension => IsTargetWindows ? ".exe" : string.Empty;

    public BuildContext(
        string rootDir,
        string targetRid,
        bool isDebug,
        string command,
        string? targetProject,
        string? extraArg,
        CompileConfig? config = null)
    {
        RootDir = rootDir;
        TargetRid = targetRid;
        IsDebug = isDebug;
        Command = command;
        TargetProject = targetProject;
        ExtraArg = extraArg;
        Config = config ?? CompileConfig.Load(rootDir);
    }

    public static string FindRepoRoot()
    {
        string cwd = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(cwd, "Directory.Build.props")) ||
            File.Exists(Path.Combine(cwd, "ncbox.slnx")) ||
            File.Exists(Path.Combine(cwd, "ncbox.sln")))
        {
            return cwd;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")) ||
                File.Exists(Path.Combine(dir.FullName, "ncbox.slnx")) ||
                File.Exists(Path.Combine(dir.FullName, "ncbox.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        return cwd;
    }

    public static string GetDefaultRid()
    {
        if (OperatingSystem.IsWindows()) return "win-x64";
        if (OperatingSystem.IsLinux()) return "linux-x64";
        return RuntimeInformation.RuntimeIdentifier;
    }
}
