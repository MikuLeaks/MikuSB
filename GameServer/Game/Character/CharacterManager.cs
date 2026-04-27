using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database;
using MikuSB.Database.Character;
using MikuSB.Enums.Item;
using MikuSB.GameServer.Game.Player;
using MikuSB.GameServer.Server.Packet.Send.Login;
using MikuSB.Proto;
using MikuSB.Util.Extensions;

namespace MikuSB.GameServer.Game.Character;

public class CharacterManager(PlayerInstance player) : BasePlayerManager(player)
{
    public CharacterData CharacterData { get; } = DatabaseHelper.GetInstanceOrCreateNew<CharacterData>(player.Uid);
    public async ValueTask<CharacterInfo?> AddCharacter(ItemTypeEnum genre, uint detail, uint particular, uint level = 1, uint star = 1, bool sendPacket = true)
    {
        var characterId = GameResourceTemplateId.FromGdpl((uint)genre,detail,particular,1);
        if (CharacterData.Characters.Any(a => a.TemplateId == characterId)) return null;
        var CharacterExcel = GameData.CardData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular);
        if (CharacterExcel == null) return null;

        var character = new CharacterInfo
        {
            Guid = CharacterData.NextCharacterGuid++,
            TemplateId = characterId,
            Level = level,
            Break = star,
            Timestamp = Extensions.GetUnixSec(),
            Flag = ItemFlagEnum.FLAG_READED
        };

        var weaponInfo = await Player.InventoryManager!.AddWeaponItem((ItemTypeEnum)CharacterExcel.DefaultWeaponGPDL[0], CharacterExcel.DefaultWeaponGPDL[1], CharacterExcel.DefaultWeaponGPDL[2], (uint)CharacterExcel.DefaultWeaponGPDL[3]);
        if (weaponInfo != null) character.WeaponUniqueId = weaponInfo.UniqueId;

        var skinInfo = Player.InventoryManager!.GetSkinItemGDPL(ItemTypeEnum.TYPE_CARD_SKIN, detail, particular, level)
              ?? await Player.InventoryManager!.AddSkinItem(ItemTypeEnum.TYPE_CARD_SKIN, detail, particular, level);
        if (skinInfo != null) character.SkinId = skinInfo.UniqueId;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([character]));

        CharacterData.Characters.Add(character);
        return character;
    }

    public CharacterInfo? GetCharacter(ulong TemplateId)
    {
        return CharacterData.Characters.Find(Character => Character.TemplateId == TemplateId);
    }

    public CharacterInfo? GetCharacterByGUID(uint guid)
    {
        return CharacterData.Characters.Find(Character => Character.Guid == guid);
    }

    public CharacterInfo? GetCharacterGDPL(ItemTypeEnum genre, int detail, int particular)
    {
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre,(uint)detail,(uint)particular,1);
        return CharacterData.Characters.Find(Character => Character.TemplateId == templateId);
    }

    public async ValueTask RepairCharacterWeapons()
    {
        var changed = false;
        var equippedWeaponIds = new HashSet<uint>();

        foreach (var character in CharacterData.Characters)
        {
            var weapon = Player.InventoryManager.GetWeaponItem(character.WeaponUniqueId);
            if (weapon == null)
            {
                var cardData = GameData.CardData.Values.FirstOrDefault(x =>
                    GameResourceTemplateId.FromGdpl(x.Genre, x.Detail, x.Particular, x.Level) == character.TemplateId);
                if (cardData?.DefaultWeaponGPDL.Count >= 4)
                {
                    weapon = await Player.InventoryManager.AddWeaponItem(
                        (ItemTypeEnum)cardData.DefaultWeaponGPDL[0],
                        cardData.DefaultWeaponGPDL[1],
                        cardData.DefaultWeaponGPDL[2],
                        cardData.DefaultWeaponGPDL[3]);
                    if (weapon != null)
                    {
                        character.WeaponUniqueId = weapon.UniqueId;
                        changed = true;
                    }
                }
            }

            if (weapon == null)
                continue;

            if (weapon.EquipAvatarId != character.Guid)
            {
                weapon.EquipAvatarId = character.Guid;
                changed = true;
            }

            equippedWeaponIds.Add(weapon.UniqueId);
        }

        foreach (var weapon in Player.InventoryManager.InventoryData.Weapons.Values)
        {
            if (!equippedWeaponIds.Contains(weapon.UniqueId) && weapon.EquipAvatarId != 0)
            {
                weapon.EquipAvatarId = 0;
                changed = true;
            }
        }

        if (!changed)
            return;

        DatabaseHelper.SaveDatabaseType(CharacterData);
        DatabaseHelper.SaveDatabaseType(Player.InventoryManager.InventoryData);
    }
}
