using System.Text.RegularExpressions;

namespace MikuSB.GameServer.Game.Player;

public static partial class ChatMessageHelper
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiWhitespaceRegex();

    public static uint BuildClientTimestamp()
    {
        return (uint)MikuSB.Util.Extensions.Extensions.GetUnixSec();
    }

    public static string NormalizeForClient(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ');

        return MultiWhitespaceRegex().Replace(normalized, " ").Trim();
    }
}
