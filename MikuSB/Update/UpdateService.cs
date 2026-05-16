using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using MikuSB.Util;

namespace MikuSB.MikuSB.Update;

public static class UpdateService
{
    private static readonly Logger Logger = new("Updater");
    private static readonly bool AskBeforeUpdate = true;
    private static readonly string RepositoryOwner = "MikuLeaks";
    private static readonly string RepositoryName = "MikuSB";
    private static readonly string AssetName = "MikuSB-win-x64.zip";
    private static readonly int ReleaseCheckTimeoutSeconds = 10;
    private static readonly int PackageDownloadTimeoutSeconds = 300;
    private static readonly int ResourceDownloadTimeoutSeconds = 300;
    private static readonly int ChecksumDownloadTimeoutSeconds = 30;
    private static readonly string ResourceArchiveUrl =
        "https://github.com/Kei-Luna/MikuSB-Resource/archive/refs/heads/main.zip";
    private static readonly string[] RequiredResourceFiles =
    [
        "item/templates/card.json",
        "item/templates/weapon.json"
    ];

    public static async Task<bool> TryStartSelfUpdateAsync()
    {
        if (!ConfigManager.Config.ServerOption.EnableAutoUpdate)
        {
            Logger.Debug("Auto update skipped because it is disabled in Config.json.");
            return false;
        }

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
            using var releaseCts =
                new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, ReleaseCheckTimeoutSeconds)));
            var release = await GetLatestReleaseAsync(client, releaseCts.Token);
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
            await DownloadFileAsync(client, asset.DownloadUrl, packagePath, PackageDownloadTimeoutSeconds);

            var resourcePackagePath = Path.Combine(tempRoot, "MikuSB-Resource-main.zip");
            Logger.Info("Downloading resource package.");
            await DownloadFileAsync(client, ResourceArchiveUrl, resourcePackagePath, ResourceDownloadTimeoutSeconds);

            var checksumAsset = release.Assets.FirstOrDefault(x =>
                string.Equals(x.Name, AssetName + ".sha256", StringComparison.OrdinalIgnoreCase));
            if (checksumAsset != null)
            {
                var checksumPath = Path.Combine(tempRoot, checksumAsset.Name);
                await DownloadFileAsync(client, checksumAsset.DownloadUrl, checksumPath, ChecksumDownloadTimeoutSeconds);
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
                    "--resource-package", resourcePackagePath,
                    "--target", AppContext.BaseDirectory,
                    "--resource-target", Path.Combine(AppContext.BaseDirectory, ConfigManager.Config.Path.ResourcePath),
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

    public static async Task EnsureResourcesPresentAsync()
    {
        if (!AreRequiredResourcesPresent())
        {
            Logger.Warn("Required resources are missing. Downloading resource package.");
            await DownloadAndInstallResourcesAsync();
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

    private static bool AreRequiredResourcesPresent()
    {
        var resourcePath = Path.Combine(AppContext.BaseDirectory, ConfigManager.Config.Path.ResourcePath);
        if (!Directory.Exists(resourcePath))
            return false;

        return RequiredResourceFiles.All(fileName => File.Exists(Path.Combine(resourcePath, fileName)));
    }


    private static async Task DownloadAndInstallResourcesAsync()
    {
        using var client = CreateHttpClient();
        var tempRoot = Path.Combine(Path.GetTempPath(), "MikuSB", "resources", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var resourcePackagePath = Path.Combine(tempRoot, "MikuSB-Resource-main.zip");
        await DownloadFileAsync(client, ResourceArchiveUrl, resourcePackagePath, ResourceDownloadTimeoutSeconds);
        InstallResourcesFromArchive(resourcePackagePath,
            Path.Combine(AppContext.BaseDirectory, ConfigManager.Config.Path.ResourcePath));
    }

    private static void InstallResourcesFromArchive(string resourcePackagePath, string resourceTargetDirectory)
    {
        var resourceStagingDirectory = Path.Combine(Path.GetTempPath(), "MikuSB", "resource-staging", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(resourceStagingDirectory);

        ZipFile.ExtractToDirectory(resourcePackagePath, resourceStagingDirectory, overwriteFiles: true);

        var extractedRoot = Directory.GetDirectories(resourceStagingDirectory).FirstOrDefault() ?? resourceStagingDirectory;
        Directory.CreateDirectory(resourceTargetDirectory);
        CopyDirectory(extractedRoot, resourceTargetDirectory);
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
            Timeout = Timeout.InfiniteTimeSpan
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
        int timeoutSeconds)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));
        using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cts.Token);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, cts.Token);
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destinationPath = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
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
