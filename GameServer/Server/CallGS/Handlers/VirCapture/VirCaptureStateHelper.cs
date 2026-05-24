using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

internal static class VirCaptureStateHelper
{
    public const uint GroupId = 128;
    public const uint MapDataStart = 10000;
    public const uint MapDataEnd = 19000;
    public const uint MaxMapCount = 3;
    public const uint MaxMapDataLen = 3000;
    public const uint MaxPatrolPoint = 500;
    public const uint MaxOtherPoint = 2500;
    public const uint MinMaterialId = 50000;
    public const uint MaxMaterialId = 51500;

    public const uint OffMapId = 1;
    public const uint OffTurnNum = 2;
    public const uint OffPosX = 3;
    public const uint OffPosY = 4;
    public const uint OffPosZ = 5;
    public const uint OffToward = 6;
    public const uint OffDayNight = 7;
    public const uint OffMapLevel = 8;
    public const uint OffPatrolStart = 51;
    public const uint OffPatrolEnd = 1000;
    public const uint OffOtherStart = 1001;
    public const uint OffOtherEnd = 1500;
    public const uint OffMaterialStart = 1501;
    public const uint OffMaterialEnd = 3000;

    public static uint FindOrAllocateMapSlot(PlayerInstance player, uint levelId)
    {
        uint? emptySlot = null;
        for (uint i = 0; i < MaxMapCount; i++)
        {
            var slotStart = MapDataStart + (i * MaxMapDataLen);
            var mapIdAttr = player.Data.Attrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == slotStart + OffMapId);
            if (mapIdAttr?.Val == levelId)
                return slotStart;

            if (emptySlot == null && (mapIdAttr == null || mapIdAttr.Val == 0))
                emptySlot = slotStart;
        }

        return emptySlot ?? 0;
    }

    public static void EnsureBaseMapState(PlayerInstance player, uint levelId, NtfSyncPlayer sync)
    {
        var slotStart = FindOrAllocateMapSlot(player, levelId);
        if (slotStart == 0)
            return;

        EnsureUnsignedAttr(player, slotStart + OffMapId, levelId, sync);
        EnsureUnsignedAttr(player, slotStart + OffDayNight, 1, sync);
        EnsureUnsignedAttr(player, slotStart + OffMapLevel, 1, sync);
    }

    public static void SetSignedMapOffset(PlayerInstance player, uint levelId, uint offset, int value, NtfSyncPlayer sync)
    {
        var slotStart = FindOrAllocateMapSlot(player, levelId);
        if (slotStart == 0)
            return;

        EnsureBaseMapState(player, levelId, sync);
        SetUnsignedAttr(player, slotStart + offset, unchecked((uint)value), sync);
    }

    public static void SetPointState(PlayerInstance player, uint levelId, uint pointId, uint value, NtfSyncPlayer sync)
    {
        var slotStart = FindOrAllocateMapSlot(player, levelId);
        if (slotStart == 0 || pointId == 0)
            return;

        EnsureBaseMapState(player, levelId, sync);

        if (pointId <= MaxPatrolPoint)
        {
            var sid = slotStart + (OffPatrolStart - 1) + pointId;
            SetUnsignedAttr(player, sid, value, sync);
            return;
        }

        if (pointId <= MaxOtherPoint)
        {
            var relative = pointId - MaxPatrolPoint;
            var sid = slotStart + (uint)Math.Floor(relative / 30d) + OffOtherStart;
            if (sid > slotStart + OffOtherEnd)
                return;

            var bit = (int)(relative % 30);
            var attr = GetOrCreateAttr(player, sid);
            var next = value > 0
                ? attr.Val | (1u << bit)
                : attr.Val & ~(1u << bit);
            if (next != attr.Val)
            {
                attr.Val = next;
                SyncAttr(player, sync, sid, next);
            }
            return;
        }

        if (pointId > MinMaterialId && pointId <= MaxMaterialId)
        {
            var sid = slotStart + (OffMaterialStart - 1) + (pointId - MinMaterialId);
            if (sid >= slotStart + OffMaterialEnd)
                return;

            SetUnsignedAttr(player, sid, value, sync);
        }
    }

    public static void EnsureUnsignedAttr(PlayerInstance player, uint sid, uint minValue, NtfSyncPlayer sync)
    {
        var attr = GetOrCreateAttr(player, sid);
        if (attr.Val < minValue)
        {
            attr.Val = minValue;
            SyncAttr(player, sync, sid, attr.Val);
        }
    }

    public static void SetUnsignedAttr(PlayerInstance player, uint sid, uint value, NtfSyncPlayer sync)
    {
        var attr = GetOrCreateAttr(player, sid);
        if (attr.Val != value)
        {
            attr.Val = value;
            SyncAttr(player, sync, sid, value);
        }
    }

    private static PlayerAttr GetOrCreateAttr(PlayerInstance player, uint sid)
    {
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == sid);
        if (attr != null)
            return attr;

        attr = new PlayerAttr
        {
            Gid = GroupId,
            Sid = sid
        };
        player.Data.Attrs.Add(attr);
        return attr;
    }

    private static void SyncAttr(PlayerInstance player, NtfSyncPlayer sync, uint sid, uint value)
    {
        sync.Custom[player.ToPackedAttrKey(GroupId, sid)] = value;
        sync.Custom[player.ToShiftedAttrKey(GroupId, sid)] = value;
    }
}
