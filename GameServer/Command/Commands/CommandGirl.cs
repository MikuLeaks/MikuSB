using MikuSB.Data;
using MikuSB.Database.Character;
using MikuSB.Enums.Item;
using MikuSB.Enums.Player;
using MikuSB.GameServer.Server.Packet.Send.Misc;
using MikuSB.Internationalization;

namespace MikuSB.GameServer.Command.Commands;

[CommandInfo("girl", "Game.Command.Girl.Desc", "Game.Command.Girl.Usage", ["g"], [PermEnum.Admin, PermEnum.Support])]
public class CommandGirl : ICommands
{
    [CommandMethod("add")]
    public async ValueTask AddGirl(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        if (await arg.GetOption('p') is not int particular) return;
        if (await arg.GetOption('l') is not int level) return;
        if (await arg.GetOption('s') is not int star) return;

        var detail = arg.GetInt(0);
        level = Math.Clamp(level, 1, 80);
        star = Math.Clamp(star, 1, 9);
        var player = arg.Target!.Player!;
        List<CharacterInfo> girls = [];
        if (detail == -1)
        {
            // add all
            foreach (var config in GameData.CardData.Values)
            {
                var character = await player.CharacterManager!
                    .AddCharacter((ItemTypeEnum)config.Genre, config.Detail, config.Particular, config.Level,(uint)star,false);
                if (character != null) girls.Add(character);
            }
        }
        else
        {
            var girl = await player.CharacterManager!.AddCharacter(ItemTypeEnum.TYPE_CARD,(uint)detail,(uint)particular,1,(uint)star, false);
            if (girl == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Girl.NotFound"));
                return;
            }
            girls.Add(girl);
        }
        if (girls.Count > 0) await player.SendPacket(new PacketNtfCallScript(girls));
        await arg.SendMsg(I18NManager.Translate("Game.Command.Girl.Added", girls.Count.ToString()));
    }

    [CommandMethod("level")]
    public async ValueTask UpdateGirlLevel(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        if (!await arg.CheckArgCnt(2)) return;

        var guid = arg.GetInt(0);
        var level = arg.GetInt(1);
        level = Math.Clamp(level, 1, 80);

        var player = arg.Target!.Player!;
        List<CharacterInfo> girls = [];
        if (guid == -1)
        {
            // update all
            foreach(var girl in player.CharacterManager.CharacterData.Characters)
            {
                girl.Level = (uint)level;
                girls.Add(girl);
            }
        }
        else
        {
            var girl = player.CharacterManager!.GetCharacterByGUID((uint)guid);
            if (girl == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Girl.NotFound"));
                return;
            }
            girl.Level = (uint)level;
            girls.Add(girl);
        }
        if (girls.Count > 0) await player.SendPacket(new PacketNtfCallScript(girls));
        await arg.SendMsg(I18NManager.Translate("Game.Command.Girl.UpdateLevel",
                level.ToString(),
                girls.Count.ToString()));
    }

    [CommandMethod("neuronic")]
    public async ValueTask UpdateNeuronicLevel(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        if (!await arg.CheckArgCnt(2)) return;

        var guid = arg.GetInt(0);
        var level = Math.Clamp(arg.GetInt(1), 0, 6);
        var player = arg.Target!.Player!;
        List<CharacterInfo> girls = [];

        List<uint> spines = new List<uint>();
        for (int i = 0; i < 6; i++)
            spines.Add(i < level ? 511u : 0u);

        uint proLevel = (uint)(spines.Count(x => x == 511) / 2);

        if (guid == -1)
        {
            foreach (var girl in player.CharacterManager.CharacterData.Characters)
            {
                girl.Spines = spines;
                girl.ProLevel = proLevel;
                girls.Add(girl);
            }
        }
        else
        {
            var girl = player.CharacterManager.GetCharacterByGUID((uint)guid);
            if (girl == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Girl.NotFound"));
                return;
            }
            girl.Spines = spines;
            girl.ProLevel = proLevel;
            girls.Add(girl);
        }

        if (girls.Count > 0) await player.SendPacket(new PacketNtfCallScript(girls));
        await arg.SendMsg(I18NManager.Translate("Game.Command.Girl.UpdateNeuronicLevel",
                    level.ToString(), 
                    girls.Count.ToString()));
    }
}