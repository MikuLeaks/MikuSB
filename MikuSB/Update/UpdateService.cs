using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using MikuSB.Util;

namespace MikuSB.MikuSB.Update;

public static class UpdateService
{
    private static readonly Logger Logger = new("Updater");
    private static readonly bool UpdateEnabled = true;
    private static readonly bool AskBeforeUpdate = true;
    private static readonly string RepositoryOwner = "DevilProMT";
    private static readonly string RepositoryName = "MikuSB";
    private static readonly string AssetName = "MikuSB-win-x64.zip";
    private static readonly int TimeoutSeconds = 5;

    public static async Task<bool> TryStartSelfUpdateAsync()
    {
        if (!UpdateEnabled)
            return false;

        if (string.IsNullOrWhiteSpace(RepositoryOwner)
            || string.IsNullOrWhiteSpace(RepositoryName)
            || string.IsNullOrWhiteSpace(AssetName))
        {
            Logger.Debug("Auto update skipped because the GitHub release source is not configured.");
            return false;
        }

        var updaterPath = Path.Combine(AppContext.BaseDirectory, "MikuSB.Updater.exe");
        if (!File.Exists(updaterPath))
        {
            Logger.Debug("Auto update skipped because MikuSB.Updater.exe was not found.");
            return false;
        }

        try
        {
            Logger.Info($"Current build version: {BuildVersion.Current}");

            using var client = CreateHttpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, TimeoutSeconds)));
            var release = await GetLatestReleaseAsync(client, cts.Token);
            if (release == null)
                return false;

            var latestVersion = BuildVersion.Normalize(release.TagName);
            if (!BuildVersion.IsNewer(latestVersion, BuildVersion.Current))
                return false;

            var asset = release.Assets.FirstOrDefault(x =>
                string.Equals(x.Name, AssetName, StringComparison.OrdinalIgnoreCase));
            if (asset == null)
            {
                Logger.Warn($"Latest release {release.TagName} does not contain asset {AssetName}.");
                return false;
            }

            if (AskBeforeUpdate && !ConfirmUpdate(latestVersion))
            {
                Logger.Info($"Skipped update {latestVersion} by user choice.");
                return false;
            }

            var tempRoot = Path.Combine(Path.GetTempPath(), "MikuSB", "updates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            var packagePath = Path.Combine(tempRoot, asset.Name);
            Logger.Info($"Downloading update {release.TagName}.");
            await DownloadFileAsync(client, asset.DownloadUrl, packagePath, cts.Token);

            var checksumAsset = release.Assets.FirstOrDefault(x =>
                string.Equals(x.Name, AssetName + ".sha256", StringComparison.OrdinalIgnoreCase));
            if (checksumAsset != null)
            {
                var checksumPath = Path.Combine(tempRoot, checksumAsset.Name);
                await DownloadFileAsync(client, checksumAsset.DownloadUrl, checksumPath, cts.Token);
                VerifySha256(packagePath, checksumPath);
            }

            var stagedUpdaterPath = StageUpdaterExecutable();
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = stagedUpdaterPath,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(stagedUpdaterPath)!,
                ArgumentList =
                {
                    "--package", packagePath,
                    "--target", AppContext.BaseDirectory,
                    "--restart", Path.Combine(AppContext.BaseDirectory, "MikuSB.exe"),
                    "--pid", Environment.ProcessId.ToString()
                }
            });

            if (process == null)
            {
                Logger.Warn("Failed to start MikuSB.Updater.exe.");
                return false;
            }

            Logger.Warn($"Update {latestVersion} found. Handing over to updater and shutting down.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn("Auto update check failed. Continuing normal startup.", ex);
            return false;
        }
    }

    private static string StageUpdaterExecutable()
    {
        var sourceDirectory = AppContext.BaseDirectory;
        var stagingDirectory = Path.Combine(Path.GetTempPath(), "MikuSB", "updater", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingDirectory);

        foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectory, "MikuSB.Updater*", SearchOption.TopDirectoryOnly))
        {
            var destinationPath = Path.Combine(stagingDirectory, Path.GetFileName(sourcePath));
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }

        var stagedUpdaterPath = Path.Combine(stagingDirectory, "MikuSB.Updater.exe");
        if (!File.Exists(stagedUpdaterPath))
            throw new FileNotFoundException("Failed to stage MikuSB.Updater.exe.", stagedUpdaterPath);

        return stagedUpdaterPath;
    }

    private static bool ConfirmUpdate(string latestVersion)
    {
        Console.Write($"New version found: {BuildVersion.Current} -> {latestVersion}. Update now? [Y/n]: ");

        try
        {
            var key = Console.ReadKey(intercept: true);
            Console.WriteLine();
            return key.Key is ConsoleKey.Enter or ConsoleKey.Y;
        }
        catch
        {
            Console.WriteLine();
            return false;
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(1, TimeoutSeconds))
        };

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("MikuSB-Updater", BuildVersion.Current));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        return client;
    }

    private static async Task<GitHubReleaseResponse?> GetLatestReleaseAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var requestUri =
            $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";
        using var response = await client.GetAsync(requestUri, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            Logger.Warn("Latest GitHub release is not accessible. This is expected while the repository remains private.");
            return null;
        }

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<GitHubReleaseResponse>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, cancellationToken);
    }

    private static async Task DownloadFileAsync(
        HttpClient client,
        string downloadUrl,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, cancellationToken);
    }

    private static void VerifySha256(string packagePath, string checksumPath)
    {
        var expected = File.ReadAllText(checksumPath).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(packagePath)))
            .ToLowerInvariant();

        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Downloaded update package checksum does not match the release checksum.");
    }
}

public sealed class GitHubReleaseResponse
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("assets")]
    public List<GitHubReleaseAssetResponse> Assets { get; set; } = [];
}

public sealed class GitHubReleaseAssetResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string DownloadUrl { get; set; } = "";
}
