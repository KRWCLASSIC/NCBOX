using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NCBOX.Core.Process;

public record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;
}

public static class ProcessHelper
{
    public static async Task<ProcessResult> RunAsync(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = global::System.Diagnostics.Process.Start(psi);
            if (process == null)
            {
                return new ProcessResult(-1, string.Empty, "Failed to start process.");
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync().ConfigureAwait(false);

            string stdout = await stdoutTask.ConfigureAwait(false);
            string stderr = await stderrTask.ConfigureAwait(false);

            return new ProcessResult(process.ExitCode, stdout, stderr);
        }
        catch (Exception ex)
        {
            return new ProcessResult(-1, string.Empty, ex.Message);
        }
    }

    public static int RunInteractive(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false
        };

        try
        {
            using var process = global::System.Diagnostics.Process.Start(psi);
            if (process == null) return -1;
            process.WaitForExit();
            return process.ExitCode;
        }
        catch
        {
            return -1;
        }
    }
}
