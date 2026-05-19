using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Proto;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Gacha;

[CallGSApi("Gacha_UpSelect")]
public class Gacha_UpSelect : ICallGSHandler
{
    private const uint GachaStrGid = 42;
    private const int UpSelectIndex = 0;
    private const int UpSelectGetFlagIndex = 1;
    private const int UpPickPoolIndex = 2;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<GachaUpSelectParam>(param);
        var player = connection.Player!;
        if (req == null || req.NId == 0 || req.Gdpl == null || req.Gdpl.Count < 4)
        {
            await CallGSRouter.SendScript(connection, "Gacha_UpSelect", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        if (!GameData.GachaData.TryGetValue((uint)req.NId, out var gachaCfg) || gachaCfg.UpSelect != 1)
        {
            await CallGSRouter.SendScript(connection, "Gacha_UpSelect", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var valid = (gachaCfg.Pool ?? [])
            .Where(GameData.GachaPoolData.ContainsKey)
            .SelectMany(name => GameData.GachaPoolData[name])
            .Any(item =>
                item.UPSelectTag == 1 &&
                item.GDPL.Count >= 4 &&
                item.GDPL[0] == req.Gdpl[0] &&
                item.GDPL[1] == req.Gdpl[1] &&
                item.GDPL[2] == req.Gdpl[2] &&
                item.GDPL[3] == req.Gdpl[3]);

        if (!valid)
        {
            await CallGSRouter.SendScript(connection, "Gacha_UpSelect", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var existing = player.Data.StrAttrs.FirstOrDefault(x => x.Gid == GachaStrGid && x.Sid == (uint)req.NId)?.Val;
        var state = string.IsNullOrWhiteSpace(existing) ? new JArray() : JArray.Parse(existing);

        EnsureArraySize(state, 3);
        state[UpSelectIndex] = new JArray(req.Gdpl);
        state[UpSelectGetFlagIndex] = 0;
        if (state[UpPickPoolIndex] == null)
            state[UpPickPoolIndex] = 0;

        player.SetStrAttr(GachaStrGid, (uint)req.NId, state.ToString(Newtonsoft.Json.Formatting.None));
        DatabaseHelper.SaveDatabaseType(player.Data);

        var sync = new NtfSyncPlayer();
        sync.CustomStr[player.ToShiftedAttrKey(GachaStrGid, (uint)req.NId)] = state.ToString(Newtonsoft.Json.Formatting.None);
        await CallGSRouter.SendScript(connection, "Gacha_UpSelect", "{}", sync);
    }

    private static void EnsureArraySize(JArray state, int size)
    {
        while (state.Count < size)
            state.Add(JValue.CreateNull());
    }
}

internal sealed class GachaUpSelectParam
{
    [JsonPropertyName("nId")]
    public int NId { get; set; }

    [JsonPropertyName("gdpl")]
    public List<uint>? Gdpl { get; set; }
}
