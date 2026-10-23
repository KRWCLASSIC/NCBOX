using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NCBOX.Builder.Pipeline;

public static class ProcessRunner
{
    public static async Task<int> RunAsync(string fileName, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null) return -1;

        await process.WaitForExitAsync().ConfigureAwait(false);
        return process.ExitCode;
    }

    public static void RunSilently(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(2000);
        }
        catch
        {
            // Best effort
        }
    }
}
