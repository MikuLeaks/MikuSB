using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MikuSB.GameServer.Game.BossPvp;
using MikuSB.Proto;
using MikuSB.GameServer.Server.CallGS.Handlers.DreamCard;
using MikuSB.GameServer.Server.CallGS.Handlers.Tower;
using MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Chapter;

[CallGSApi("Chapter_DealLevelSettlement")]
public class Chapter_DealLevelSettlement : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<DealLevelSettlementParam>(param);
        NtfSyncPlayer? extraSync = null;
        var response = new JsonObject
        {
            ["sCmd"] = req?.SCmd ?? "Chapter_LevelSettlement",
            ["tbParam"] = BuildSettlementPayload(connection, req?.SCmd, req?.TbParam, out extraSync)
        };

        await CallGSRouter.SendScript(connection, "Chapter_DealLevelSettlement", response.ToJsonString(), extraSync!);
    }

    private static JsonNode BuildSettlementPayload(Connection connection, string? sCmd, JsonNode? tbParam, out NtfSyncPlayer? extraSync)
    {
        extraSync = null;

        if (string.Equals(sCmd, "Chapter_LevelSettlement", StringComparison.Ordinal))
        {
            return new JsonArray();
        }

        if (string.Equals(sCmd, "Chapter_NewPrologueSettlement", StringComparison.Ordinal))
        {
            var result = new JsonObject();
            if (tbParam is JsonObject source && source.TryGetPropertyValue("bWaitServer", out var bWaitServer))
            {
                result["bWaitServer"] = bWaitServer?.DeepClone();
            }
            result["tbShowAward"] = new JsonArray();
            return result;
        }

        if (string.Equals(sCmd, "BossPvpLogic_LevelSettlement", StringComparison.Ordinal))
        {
            var normalized = NormalizeBossPvpSettlement(tbParam);
            var (response, sync) = BossPvpService.HandleSettlement(connection.Player!, normalized);
            extraSync = sync;
            return response;
        }

        if (string.Equals(sCmd, "BossPvpLogic_LevelFail", StringComparison.Ordinal))
        {
            var (response, sync) = BossPvpService.HandleFail(connection.Player!, tbParam);
            extraSync = sync;
            return response;
        }

        if (string.Equals(sCmd, "TowerLevel_LevelSettlement", StringComparison.Ordinal))
        {
            var (response, sync) = TowerLevel_LevelSettlement.HandleSettlement(connection.Player!, tbParam);
            extraSync = sync;
            return response;
        }

        if (string.Equals(sCmd, "TowerEventChapter_LevelSettlement", StringComparison.Ordinal))
        {
            var (response, sync) = TowerEventChapter_LevelSettlement.HandleSettlement(connection.Player!, tbParam);
            extraSync = sync;
            return response;
        }

        if (string.Equals(sCmd, "VirCaptureTower_LevelSettlement", StringComparison.Ordinal))
        {
            var (response, sync) = VirCaptureTower_LevelSettlement.HandleSettlement(connection.Player!, tbParam);
            extraSync = sync;
            return response;
        }

        if (string.Equals(sCmd, "DreamCard_LevelSettlement", StringComparison.Ordinal))
        {
            var (response, sync) = DreamCard_LevelSettlement.HandleSettlement(connection.Player!, tbParam);
            extraSync = sync;
            return response;
        }

        return tbParam?.DeepClone() ?? new JsonObject();
    }

    private static JsonNode? NormalizeBossPvpSettlement(JsonNode? tbParam)
    {
        if (tbParam is not JsonObject obj)
            return tbParam;

        var clone = obj.DeepClone() as JsonObject ?? obj;
        if (clone.TryGetPropertyValue("ResidueTime", out var residueNode) &&
            residueNode is JsonValue residueValue &&
            residueValue.TryGetValue<double>(out var residueTime))
        {
            clone["ResidueTime"] = (int)Math.Max(0, Math.Round(residueTime, MidpointRounding.AwayFromZero));
        }

        return clone;
    }
}

internal sealed class DealLevelSettlementParam
{
    [JsonPropertyName("sCmd")]
    public string? SCmd { get; set; }

    [JsonPropertyName("tbParam")]
    public JsonNode? TbParam { get; set; }
}
