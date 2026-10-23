using System;
using System.IO;

namespace NCBOX.Core.System;

public static class SymlinkHelper
{
    public static bool IsSymlink(string path)
    {
        if (!Path.Exists(path))
        {
            // Path might be a broken symlink
            try
            {
                var fileInfo = new FileInfo(path);
                return fileInfo.LinkTarget != null;
            }
            catch
            {
                return false;
            }
        }

        try
        {
            var attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return false;
        }
    }

    public static string? GetLinkTarget(string linkPath)
    {
        try
        {
            var target = File.ResolveLinkTarget(linkPath, returnFinalTarget: true);
            return target?.FullName;
        }
        catch
        {
            try
            {
                var fileInfo = new FileInfo(linkPath);
                return fileInfo.LinkTarget;
            }
            catch
            {
                return null;
            }
        }
    }

    public static void CreateSymlink(string linkPath, string targetPath)
    {
        string? linkDir = Path.GetDirectoryName(linkPath);
        if (!string.IsNullOrEmpty(linkDir))
        {
            Directory.CreateDirectory(linkDir);
        }

        if (IsSymlink(linkPath) || File.Exists(linkPath))
        {
            DeleteSymlink(linkPath);
        }

        File.CreateSymbolicLink(linkPath, targetPath);
    }

    public static bool DeleteSymlink(string linkPath)
    {
        try
        {
            var info = new FileInfo(linkPath);
            if (info.Exists || info.LinkTarget != null)
            {
                info.Delete();
                return true;
            }

            if (File.Exists(linkPath))
            {
                File.Delete(linkPath);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
