using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace NCBOX.Core.Platform;

public static class PlatformInfo
{
    public static bool IsLinux => OperatingSystem.IsLinux();
    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsMacOS => OperatingSystem.IsMacOS();

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint GetEuid();

    public static bool IsPrivileged()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            try
            {
                return GetEuid() == 0;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
