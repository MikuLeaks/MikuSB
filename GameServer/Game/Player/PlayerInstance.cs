using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Database.Account;
using MikuSB.Database.Player;
using MikuSB.Enums.Item;
using MikuSB.GameServer.Game.Character;
using MikuSB.GameServer.Game.Inventory;
using MikuSB.GameServer.Server;
using MikuSB.GameServer.Game.Lineup;
using MikuSB.TcpSharp;
using MikuSB.Util.Extensions;

namespace MikuSB.GameServer.Game.Player;

public class PlayerInstance(PlayerGameData data)
{
    #region Property
    public Connection? Connection { get; set; }

    public static readonly List<PlayerInstance> _playerInstances = [];
    public int Uid { get; set; }
    public bool Initialized { get; set; }
    public bool IsNewPlayer { get; set; }

    #endregion

    #region Data & Manager

    public PlayerGameData Data { get; set; } = data;
    public CharacterManager CharacterManager { get; set; } = null!;
    public InventoryManager InventoryManager { get; set; } = null!;
    public LineupManager LineupManager { get; set; } = null!;

    #endregion

    #region Initializers
    public PlayerInstance(int uid) : this(new PlayerGameData { Uid = uid })
    {
        // new player
        IsNewPlayer = true;
        Data.Name = AccountData.GetAccountByUid(uid)?.Username;

        DatabaseHelper.CreateInstance(Data);

        var t = Task.Run(async () =>
        {
            await InitialPlayerManager();
            foreach (var skinCard in GameData.CardSkinData.Values)
            {
                await InventoryManager.AddSkinItem((ItemTypeEnum)skinCard.Genre, skinCard.Detail, skinCard.Particular, skinCard.Level);
            }
            foreach (var weapon in GameData.WeaponData.Values)
            {
                await InventoryManager.AddWeaponItem((ItemTypeEnum)weapon.Genre, weapon.Detail, weapon.Particular, weapon.Level);
            }
            foreach (var ar in GameData.ArItemData.Values)
            {
                await InventoryManager.AddArItem((ItemTypeEnum)ar.Genre, ar.Detail, ar.Particular, ar.Level);
            }
            foreach (var manifest in GameData.ManifestationData.Values)
            {
                await InventoryManager.AddManifestationItem((ItemTypeEnum)manifest.Genre, manifest.Detail, manifest.Particular, manifest.Level);
            }
            foreach (var card in GameData.CardData.Values)
            {
                await CharacterManager.AddCharacter((ItemTypeEnum)card.Genre, card.Detail, card.Particular, card.Level);
            }

            var selected = CharacterManager.CharacterData.Characters
                .OrderBy(_ => Guid.NewGuid())
                .Take(3)
                .Select(x => x.Guid)
                .ToList();

            await LineupManager.UpdateLineup(1, selected[0], selected[1], selected[2]);

            var bootstrapAttrs = BuildLobbyBootstrapAttrs();
            var existingAttrs = Data.Attrs
                .ToDictionary(x => (x.Gid, x.Sid));
            var seenAttrs = new HashSet<(uint Gid, uint Sid)>();

            foreach (var (gid, sid, value) in bootstrapAttrs)
            {
                if (!seenAttrs.Add((gid, sid)))
                    continue;

                if (existingAttrs.TryGetValue((gid, sid), out var attr))
                {
                    if (attr.Val < value)
                        attr.Val = value;

                    continue;
                }

                var newAttr = new PlayerAttr
                {
                    Gid = gid,
                    Sid = sid,
                    Val = value
                };

                Data.Attrs.Add(newAttr);
                existingAttrs[(gid, sid)] = newAttr;
            }
        });
        t.Wait();

        Initialized = true;
    }
    private async ValueTask InitialPlayerManager()
    {
        Uid = Data.Uid;
        Data.LastActiveTime = Extensions.GetUnixSec();
        InventoryManager = new InventoryManager(this);
        LineupManager = new LineupManager(this);
        CharacterManager = new CharacterManager(this);

        await Task.CompletedTask;
    }
    public T InitializeDatabase<T>() where T : BaseDatabaseDataHelper, new()
    {
        var instance = DatabaseHelper.GetInstanceOrCreateNew<T>(Uid);
        return instance!;
    }

    #endregion

    #region Network
    public async ValueTask OnEnterGame()
    {
        if (!Initialized) await InitialPlayerManager();
        await CharacterManager.RepairCharacterWeapons();
    }

    public async ValueTask OnLogin()
    {
        _playerInstances.Add(this);
        await Task.CompletedTask;
    }

    public static PlayerInstance? GetPlayerInstanceByUid(long uid)
        => _playerInstances.FirstOrDefault(player => player.Uid == uid);
    public void OnLogoutAsync()
    {
        _playerInstances.Remove(this);
    }
    public async ValueTask SendPacket(BasePacket packet)
    {
        if (Connection?.IsOnline == true) await Connection.SendPacket(packet);
    }

    #endregion

    #region Actions
    public async ValueTask OnHeartBeat()
    {
        DatabaseHelper.ToSaveUidList.SafeAdd(Uid);
        await Task.CompletedTask;
    }

    #endregion

    #region Serialization

    public Proto.Player ToPlayerProto()
    {
        var proto = new Proto.Player
        {
            Pid = (ulong)Data.Uid,
            Account = Data.Name,
            Provider = Data.Name,
            Name = Data.Name,
            Level = Data.Level,
            Sex = Data.Gender,
            Vigor = Data.Vigor,
            Solutions = { LineupManager.LineupData.LineupInfo.Values.Select(x => x.ToProto()) },
        };

        foreach (var item in InventoryManager.InventoryData.Items.Values) proto.Items.Add(item.ToProto());
        foreach (var weapon in InventoryManager.InventoryData.Weapons.Values) proto.Items.Add(weapon.ToProto());
        foreach (var skin in InventoryManager.InventoryData.Skins.Values) proto.Items.Add(skin.ToProto());
        foreach (var chara in CharacterManager.CharacterData.Characters) proto.Items.Add(chara.ToProto());

        foreach (var x in Data.Attrs)
        {
            uint gid = x.Gid;
            uint sid = x.Sid;
            uint val = x.Val;

            if (gid == 0)
            {
                proto.Attrs[sid] = val;
                continue;
            }

            proto.Attrs[ToPackedAttrKey(gid, sid)] = val;
            proto.Attrs[ToShiftedAttrKey(gid, sid)] = val;
        }

        proto.ShowItems.AddRange(Data.ShowItems);

        return proto;
    }

    public void SetShowItem(int index, ulong itemId)
    {
        if (index <= 0)
            return;

        while (Data.ShowItems.Count < index)
            Data.ShowItems.Add(0);

        Data.ShowItems[index - 1] = itemId;
    }

    public uint ToPackedAttrKey(uint gid, uint sid)
    {
        if (gid == 0)
            return sid;

        return (gid * 10000) + sid;
    }

    public uint ToShiftedAttrKey(uint gid, uint sid)
    {
        if (gid == 0)
            return sid;

        return (gid << 16) | sid;
    }

    private static IEnumerable<(uint Gid, uint Sid, uint Value)> BuildLobbyBootstrapAttrs()
    {
        // GuideLogic uses group 4. Value 999 is safely above every configured step count,
        // so the client treats these guides as already completed.
        yield return (4, 0, 5);
        yield return (11, 1, 1);
        yield return (57, 0, 1);
        yield return (99, 3, 30);
        yield return (110, 1, 1);
        yield return (178, 1, 1_700_000_000);
        yield return (187, 1, 2);

        // Cash.GetMoneyCount uses group 1 with sid = moneyId * 2 + 1 for most currencies.
        // Fill a wide currency id range so every in-game currency starts effectively unlimited.
        for (uint moneyId = 1; moneyId <= 200; moneyId++)
            yield return (1, moneyId * 2 + 1, 999_999_999);

        for (uint guideId = 1; guideId <= 150; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 10_000; guideId <= 10_300; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 11_000; guideId <= 11_300; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 12_000; guideId <= 12_100; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 22_000; guideId <= 22_100; guideId++)
            yield return (4, guideId, 999);

        // Additional guide ids referenced directly by the Lua scripts and observed client logs.
        foreach (var guideId in new uint[] { 10_031, 10_041, 10_061, 10_081, 10_101, 10_224, 11_006, 11_202, 11_210, 22_002 })
            yield return (4, guideId, 999);

        // Launch.GPASSID = 22 stores pass counts. ChapterLevel.GID = 21 stores star flags.
        // Unlock every level defined in level.json so all chapters are accessible from the start.
        foreach (var levelId in GameData.ChapterLevelData.Keys)
        {
            yield return (21, levelId, 7);
            yield return (22, levelId, 1_700_000_000);
        }

        // Main Scene 0 mean default scene
        yield return (132, 1, 0);
    }
    #endregion
}