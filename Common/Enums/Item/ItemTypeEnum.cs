namespace MikuSB.Enums.Item;

public enum ItemTypeEnum
{
    TYPE_CARD = 1, // 角色卡
    TYPE_WEAPON = 2, // 武器卡
    TYPE_SUPPORT = 3, // 后勤卡
    TYPE_USEABLE = 4, // 可使用道具
    TYPE_SUPPLIES = 5, // 消耗类道具
    TYPE_WEAPON_PART = 6, // 武器配件
    TYPE_CARD_SKIN = 7, // 角色皮肤
    TYPE_HOUSE = 8, // 宿舍家具
    TYPE_PROFILE = 9, // 头像
    TYPE_FRAME = 10, // 头像框
    TYPE_BADGE = 11, // 勋章
    TYPE_COVER = 12, // 封面
    TYPE_NAMECARD = 13, // 名片
    TYPE_EXPRESSION = 14, // 表情
    TYPE_BUBBLE = 15, // 聊天气泡
    TYPE_ANALYST = 16, // 墨镜分析员
    TYPE_WEAPON_SKIN = 17, //武器皮肤
    TYPE_MONSTER_CARD = 18, //怪物卡
    TYPE_MANIFESTATION = 19, //角色皮肤互动场景道具
    TYPE_CARD_SKIN_PART = 20, //角色皮肤部件
    TYPE_MAIN_SCENE = 21, //主界面场景道具
    TYPE_AR = 24, //AR道具
    TYPE_CALL = 25, //电话陪伴道具
}

public enum ItemCardSlotTypeEnum
{
    SLOT_SUPPORTERCARD1 = 1, // 后勤卡
    SLOT_SUPPORTERCARD2 = 2, // 后勤卡
    SLOT_SUPPORTERCARD3 = 3, // 后勤卡
    SLOT_WEAPON = 4, // 武器
    SLOT_SKIN = 5, // 时装
    SLOT_WEAPON_SKIN = 6, // 武器时装
    SLOT_SUPPORTERINDEX = 7, // 当前使用的后勤组
    SLOT_SUPPORTERCARD4 = 8, // 后勤卡
    SLOT_SUPPORTERCARD5 = 9, // 后勤卡
    SLOT_SUPPORTERCARD6 = 10, // 后勤卡
    SLOT_SUPPORTERCARD7 = 11, // 后勤卡
    SLOT_SUPPORTERCARD8 = 12, // 后勤卡
    SLOT_SUPPORTERCARD9 = 13, // 后勤卡
}

public enum ItemSkinPartSlotTypeEnum
{
    SLOT_SkinPartSlot1 = 1,
    SLOT_SkinPartSlot2 = 2,
    SLOT_SkinPartSlot3 = 3,
    SLOT_SkinPartSlot4 = 4,
    SLOT_SkinPartSlot5 = 5,
    SLOT_SkinPartSlot6 = 6,
    SLOT_SkinPartSlot7 = 7,
    SLOT_SkinPartSlot8 = 8,
    SLOT_SkinPartSlot9 = 9,
    SLOT_SkinPartSlot10 = 10,
}

public enum ItemSkinSlotTypeEnum
{
    SLOT_CARD_SKIL_TYPE = 11
}

public enum ItemSupportCardSlotTypeEnum
{
    SLOT_AFFIXINDEX = 1 // 可洗练的初始词缀索引
}

public enum ItemFlagEnum
{
    FLAG_USE = 1, // 使用中
    FLAG_LOCK = 2, // 锁定中
    FLAG_READED = 4, // 道具已查看
    FLAG_LEAVE = 8, // 角色大招后离场
    FLAG_WEAPON_DEFAULT = 16, // 武器显示原始样式
    FLAG_WEAPON_AUDIO = 32, // 武器消音器音效
    FLAG_ROLE_LIKE = 64, // 心选角色
}