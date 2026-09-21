using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace BatchCraft.App.Services;

public sealed class UpdateService
{
    internal const string LatestReleaseApi = "https://api.github.com/repos/thenikonboy/BatchCraft-Downloads/releases/latest";
    private readonly HttpClient client;

    public UpdateService(HttpClient? client = null)
    {
        this.client = client ?? new HttpClient();
        if (!this.client.DefaultRequestHeaders.UserAgent.Any()) this.client.DefaultRequestHeaders.UserAgent.ParseAdd("BatchCraft-Updater/1.4.1");
        this.client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        CleanupOldUpdates();
        using var response = await client.GetAsync(LatestReleaseApi, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var latest)) return null;
        var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
        if (latest <= current) return null;
        var kind = IsInstalledBuild() ? UpdatePackageKind.Installer : UpdatePackageKind.Portable;
        var prefix = kind == UpdatePackageKind.Installer ? "BatchCraft-Setup-" : "BatchCraft-v";
        var suffix = kind == UpdatePackageKind.Installer ? "-win-x64.exe" : "-win-x64.zip";
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
            var digest = asset.TryGetProperty("digest", out var node) ? node.GetString() : null;
            ValidateDigest(digest);
            return new UpdateInfo(latest, tag, name,
                asset.GetProperty("browser_download_url").GetString() ?? throw new InvalidDataException("Release URL missing."),
                digest!, root.GetProperty("html_url").GetString() ?? "", kind);
        }
        throw new InvalidDataException(kind == UpdatePackageKind.Installer ? "The release does not contain a Windows installer." : "The release does not contain a portable Windows package.");
    }

    public async Task DownloadAndInstallAsync(UpdateInfo update, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        ValidateDigest(update.Digest);
        var updateRoot = GetUpdateFolder(update.Tag);
        Directory.CreateDirectory(updateRoot);
        var packagePath = Path.Combine(updateRoot, update.AssetName);
        await DownloadAsync(update.DownloadUrl, packagePath, progress, cancellationToken);
        await VerifyDigestAsync(packagePath, update.Digest, cancellationToken);
        if (update.Kind == UpdatePackageKind.Installer)
        {
            var start = new ProcessStartInfo { FileName = packagePath, Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS", UseShellExecute = true };
            _ = Process.Start(start) ?? throw new InvalidOperationException("Could not start the installer.");
            return;
        }
        await StartPortableUpdaterAsync(packagePath, updateRoot, cancellationToken);
    }

    internal static bool IsInstalledBuild() => File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));

    internal static void ValidateDigest(string? digest)
    {
        if (string.IsNullOrWhiteSpace(digest) || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) || digest.Length != 71 || !digest[7..].All(Uri.IsHexDigit))
            throw new InvalidDataException("The release is missing a valid SHA-256 digest. Update cancelled.");
    }

    internal static async Task VerifyDigestAsync(string path, string digest, CancellationToken cancellationToken = default)
    {
        ValidateDigest(digest);
        await using var file = File.OpenRead(path);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken));
        if (!actual.Equals(digest[7..], StringComparison.OrdinalIgnoreCase))
        {
            file.Close();
            try { File.Delete(path); } catch { }
            throw new InvalidDataException("Update checksum verification failed. The downloaded file was deleted.");
        }
    }

    private async Task DownloadAsync(string url, string destination, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        var partial = destination + ".partial";
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength;
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = File.Create(partial);
            var buffer = new byte[1024 * 128]; long received = 0; int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken); received += read;
                if (total > 0) progress?.Report((int)(received * 100 / total.Value));
            }
            output.Close(); File.Move(partial, destination, true);
        }
        catch { try { File.Delete(partial); } catch { } throw; }
    }

    private static async Task StartPortableUpdaterAsync(string zipPath, string updateRoot, CancellationToken cancellationToken)
    {
        var target = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        if (!CanWriteTo(target)) throw new UnauthorizedAccessException("Portable update cannot write to the application folder.");
        var executable = Environment.ProcessPath ?? Path.Combine(target, "BatchCraft.exe");
        var script = Path.Combine(updateRoot, "install-portable-update.ps1");
        var staging = Path.Combine(updateRoot, "expanded"); var backup = Path.Combine(updateRoot, "backup");
        var scriptText = """
            param([int]$ProcessId, [string]$Zip, [string]$Target, [string]$Staging, [string]$Backup, [string]$Executable)
            $ErrorActionPreference = 'Stop'
            Wait-Process -Id $ProcessId -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $Staging -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $Backup -Recurse -Force -ErrorAction SilentlyContinue
            New-Item -ItemType Directory -Path $Staging,$Backup -Force | Out-Null
            Expand-Archive -LiteralPath $Zip -DestinationPath $Staging -Force
            Get-ChildItem -LiteralPath $Target -Force | Copy-Item -Destination $Backup -Recurse -Force
            try {
              Copy-Item -Path (Join-Path $Staging '*') -Destination $Target -Recurse -Force
              Start-Process -FilePath $Executable
              Remove-Item -LiteralPath $Backup -Recurse -Force -ErrorAction SilentlyContinue
            } catch {
              Copy-Item -Path (Join-Path $Backup '*') -Destination $Target -Recurse -Force
              Start-Process -FilePath $Executable
              exit 1
            }
            """;
        await File.WriteAllTextAsync(script, scriptText, cancellationToken);
        var start = new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -ProcessId {Environment.ProcessId} -Zip \"{zipPath}\" -Target \"{target}\" -Staging \"{staging}\" -Backup \"{backup}\" -Executable \"{executable}\"", UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden };
        _ = Process.Start(start) ?? throw new InvalidOperationException("Could not start the portable updater.");
    }

    private static string GetUpdateFolder(string tag) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BatchCraft", "Updates", tag);
    private static void CleanupOldUpdates()
    {
        try
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BatchCraft", "Updates");
            if (!Directory.Exists(root)) return;
            foreach (var directory in Directory.GetDirectories(root)) if (Directory.GetCreationTimeUtc(directory) < DateTime.UtcNow.AddDays(-14)) Directory.Delete(directory, true);
            foreach (var partial in Directory.GetFiles(root, "*.partial", SearchOption.AllDirectories)) File.Delete(partial);
        }
        catch { }
    }
    private static bool CanWriteTo(string folder) { try { var probe = Path.Combine(folder, $".update-test-{Guid.NewGuid():N}"); File.WriteAllText(probe, ""); File.Delete(probe); return true; } catch { return false; } }
}

public enum UpdatePackageKind { Installer, Portable }
public sealed record UpdateInfo(Version Version, string Tag, string AssetName, string DownloadUrl, string Digest, string ReleaseUrl, UpdatePackageKind Kind);
