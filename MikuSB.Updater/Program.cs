using System.Diagnostics;
using System.IO.Compression;

var argsMap = ParseArgs(args);
if (!argsMap.TryGetValue("--package", out var packagePath)
    || !argsMap.TryGetValue("--target", out var targetDirectory)
    || !argsMap.TryGetValue("--restart", out var restartExecutable))
{
    Console.Error.WriteLine("Missing required arguments.");
    return 1;
}

argsMap.TryGetValue("--pid", out var pidValue);

try
{
    if (int.TryParse(pidValue, out var pid))
        WaitForExit(pid);

    var stagingDirectory = Path.Combine(Path.GetTempPath(), "MikuSB", "staging", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(stagingDirectory);

    ZipFile.ExtractToDirectory(packagePath, stagingDirectory, overwriteFiles: true);
    CopyDirectory(stagingDirectory, targetDirectory);

    Process.Start(new ProcessStartInfo
    {
        FileName = restartExecutable,
        UseShellExecute = false,
        WorkingDirectory = targetDirectory
    });

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

static Dictionary<string, string> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length - 1; i += 2)
    {
        result[args[i]] = args[i + 1];
    }

    return result;
}

static void WaitForExit(int pid)
{
    try
    {
        using var process = Process.GetProcessById(pid);
        process.WaitForExit(30000);
    }
    catch
    {
    }
}

static void CopyDirectory(string sourceDirectory, string targetDirectory)
{
    foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(sourceDirectory, directory);
        if (ShouldSkip(relativePath))
            continue;

        Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
    }

    foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(sourceDirectory, file);
        if (ShouldSkip(relativePath))
            continue;

        var destinationPath = Path.Combine(targetDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(file, destinationPath, overwrite: true);
    }
}

static bool ShouldSkip(string relativePath)
{
    return relativePath.StartsWith("Config", StringComparison.OrdinalIgnoreCase);
}
