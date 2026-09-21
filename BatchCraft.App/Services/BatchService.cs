using System.Diagnostics;
using System.IO;
using BatchCraft.App.Models;

namespace BatchCraft.App.Services;

public sealed class BatchService : IBatchService
{
    public async Task<int> RunAsync(BatchJob job, string workingDirectory, Action<string> output, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(job.Command)) throw new ArgumentException("Command cannot be empty.");
        if (!Directory.Exists(workingDirectory)) throw new DirectoryNotFoundException(workingDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe",
            Arguments = $"/d /s /c \"{job.Command}\"",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) output(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) output("ERROR: " + e.Data); };
        if (!process.Start()) throw new InvalidOperationException("Could not start the command.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
