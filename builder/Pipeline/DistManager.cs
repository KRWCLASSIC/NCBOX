using System;
using System.IO;

namespace NCBOX.Builder.Pipeline;

public static class DistManager
{
    public static void EnsureDistLayout(BuildContext ctx)
    {
        Directory.CreateDirectory(ctx.DistDir);
        Directory.CreateDirectory(ctx.IntermediateDir);
        CleanupStrayFilesInDistRoot(ctx);
    }

    public static string DeployApp(BuildContext ctx, string stagingDir, string primaryExeName)
    {
        EnsureDistLayout(ctx);

        string targetExePath = Path.Combine(ctx.DistDir, primaryExeName);

        if (Directory.Exists(stagingDir))
        {
            foreach (var file in Directory.GetFiles(stagingDir))
            {
                string fileName = Path.GetFileName(file);

                if (string.Equals(fileName, primaryExeName, StringComparison.OrdinalIgnoreCase))
                {
                    CopyOrOverwrite(file, targetExePath);
                }
                else if (fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase))
                {
                    string pdbDest = Path.Combine(ctx.IntermediateDir, fileName);
                    CopyOrOverwrite(file, pdbDest);
                }
                else
                {
                    string intermediateDest = Path.Combine(ctx.IntermediateDir, fileName);
                    CopyOrOverwrite(file, intermediateDest);
                }
            }
        }

        CleanupStrayFilesInDistRoot(ctx);
        return targetExePath;
    }

    public static void CleanupStrayFilesInDistRoot(BuildContext ctx)
    {
        if (!Directory.Exists(ctx.DistDir)) return;

        foreach (var file in Directory.GetFiles(ctx.DistDir))
        {
            string fileName = Path.GetFileName(file);
            if (fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                string dest = Path.Combine(ctx.IntermediateDir, fileName);
                try
                {
                    CopyOrOverwrite(file, dest);
                    File.Delete(file);
                }
                catch
                {
                    // Ignore if locked
                }
            }
        }
    }

    public static void PrintStructureTree(BuildContext ctx)
    {
        if (!Directory.Exists(ctx.DistDir)) return;

        Console.WriteLine("\x1b[1;36m>>> Distribution Layout:\x1b[0m");
        string relDist = Path.GetRelativePath(ctx.RootDir, ctx.DistDir).Replace('\\', '/');
        Console.WriteLine($"\x1b[1;37m{relDist}/\x1b[0m");

        var files = Directory.GetFiles(ctx.DistDir);
        for (int i = 0; i < files.Length; i++)
        {
            var f = files[i];
            long size = new FileInfo(f).Length;
            string prefix = "  ├── ";
            Console.WriteLine($"{prefix}\x1b[1;32m{Path.GetFileName(f)}\x1b[0m \x1b[90m({FormatSize(size)})\x1b[0m");
        }

        var subDirs = Directory.GetDirectories(ctx.DistDir);
        for (int i = 0; i < subDirs.Length; i++)
        {
            var d = subDirs[i];
            string dirName = Path.GetFileName(d);
            bool isLast = i == subDirs.Length - 1;
            string prefix = isLast ? "  └── " : "  ├── ";

            var innerFiles = Directory.GetFiles(d);
            Console.WriteLine($"{prefix}\x1b[90m{dirName}/ ({innerFiles.Length} files)\x1b[0m");
        }
        Console.WriteLine();
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }
        if (bytes >= 1024)
        {
            return $"{bytes / 1024.0:F1} KB";
        }
        return $"{bytes} B";
    }

    private static void CopyOrOverwrite(string source, string destination)
    {
        try
        {
            string? destDir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            File.Copy(source, destination, overwrite: true);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Warning] Failed to stage {Path.GetFileName(source)}: {ex.Message}");
            Console.ResetColor();
        }
    }
}
