using System.Reflection;

namespace MikuSB.Util;

public static class BuildVersion
{
    public static string Current
    {
        get
        {
            var assembly = Assembly.GetEntryAssembly() ?? typeof(BuildVersion).Assembly;
            var value = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return string.IsNullOrWhiteSpace(value) ? "0.0.0" : Normalize(value);
        }
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "0.0.0";

        var trimmed = value.Trim();
        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[1..];

        var separatorIndex = trimmed.IndexOfAny(['-', '+']);
        return separatorIndex >= 0 ? trimmed[..separatorIndex] : trimmed;
    }

    public static bool IsNewer(string candidate, string current)
    {
        return ToComparableVersion(candidate) > ToComparableVersion(current);
    }

    private static Version ToComparableVersion(string? value)
    {
        var normalized = Normalize(value);
        var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var padded = new int[4];

        for (var i = 0; i < padded.Length; i++)
        {
            if (i < parts.Length && int.TryParse(parts[i], out var parsed))
                padded[i] = parsed;
        }

        return new Version(padded[0], padded[1], padded[2], padded[3]);
    }
}
