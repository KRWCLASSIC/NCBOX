using System;
using System.IO;
using NCBOX.Core.Platform;

namespace NCBOX.Core.Paths;

public static class ServerPaths
{
    public static string DefaultSitesAvailable =>
        PlatformInfo.IsWindows
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "nginx", "sites-available")
            : "/etc/nginx/sites-available";

    public static string DefaultSitesEnabled =>
        PlatformInfo.IsWindows
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "nginx", "sites-enabled")
            : "/etc/nginx/sites-enabled";

    public static string DefaultLetsEncryptLive =>
        PlatformInfo.IsWindows
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "letsencrypt", "live")
            : "/etc/letsencrypt/live";

    public static string GetUserConfigDir(string toolName)
    {
        if (PlatformInfo.IsWindows)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, toolName.ToUpperInvariant());
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config", toolName.ToLowerInvariant());
    }
}
