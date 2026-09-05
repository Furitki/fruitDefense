using System;
using UnityEngine;

namespace FruitDefense.UI
{
    public enum RuntimeUiCopyLinePolicy
    {
        SingleLine = 0,
        ControlledTwoLines = 1,
    }

    public enum RuntimeUiCopyId
    {
        ProductTitle = 0,
        BootstrapLoading = 1,
        BootstrapRetry = 2,
        BootstrapLevelUnavailable = 3,
        BootstrapConfigurationUnavailable = 4,
        BootstrapContentUnavailable = 5,
        BootstrapPageUnavailable = 6,
        BootstrapUnknownFailure = 7,
        BootstrapRecoverableError = 8,
        LobbyTitle = 9,
        LobbyOrchard01Title = 10,
        LobbyOrchard01Body = 11,
        LobbyOrchard02Title = 12,
        LobbyOrchard02Body = 13,
        LobbyOrchard03Title = 14,
        LobbyOrchard03Body = 15,
        LobbyStart = 16,
        LobbyTransitioning = 17,
        LobbyError = 18,
        BattleTitle = 19,
        BattleSun = 20,
        BattleCore = 21,
        BattleWave = 22,
        BattleReady = 23,
        BattleStartWave = 24,
        BattleBetweenWave = 25,
        BattleStartNextWave = 26,
        BattleVictoryStatus = 27,
        BattleDefeatStatus = 28,
        BattlePausedTitle = 29,
        BattlePausedMessage = 30,
        BattleContinue = 31,
        BattleRestart = 32,
        BattleVictoryTitle = 33,
        BattleVictoryMessage = 34,
        BattleDefeatTitle = 35,
        BattleDefeatMessage = 36,
        BattleContextTray = 37,
        BattleNurseryTray = 38,
        BattleNurseryPotStored = 39,
        BattleNurseryEmpty = 40,
        BattleRefresh = 41,
        BattleDefaultGuidance = 42,
        SettlementTitle = 43,
        SettlementVictory = 44,
        SettlementDefeat = 45,
        SettlementCompletedLevel = 46,
        SettlementReachedWave = 47,
        SettlementRemainingLives = 48,
        SettlementReturning = 49,
        SettlementRetry = 50,
        SettlementReturn = 51,
        SettlementTransitioning = 52,
        SettlementRecoveredError = 53,
        BattleVictoryOutcome = 54,
        BattleDefeatOutcome = 55,
        HubHomeTitle = 56,
        HubActivityTitle = 57,
        HubGrowthTitle = 58,
        HubNavHome = 59,
        HubNavActivity = 60,
        HubNavGrowth = 61,
        HubGrowthEquipmentTab = 62,
        HubGrowthCultivationTab = 63,
        HubUnavailableTitle = 64,
        HubActivityUnavailableBody = 65,
        HubEquipmentUnavailableBody = 66,
        HubCultivationUnavailableBody = 67,
        HubHomeGrowthPreviewTitle = 68,
        HubHomeGrowthPreviewUnavailableBody = 69,
        BootstrapProfileUnsupported = 70,
        BootstrapProfileReset = 71,
        BootstrapProfileResetting = 72,
        HubResourceMorningDew = 73,
        HubActivityRewardTitle = 74,
        HubActivityClaim = 75,
        HubActivityClaiming = 76,
        HubActivityClaimed = 77,
        HubActivityLocked = 78,
        HubActivityClaimable = 79,
        HubActivityError = 80,
        HubGrowthOwned = 81,
        HubGrowthLocked = 82,
        HubGrowthEquipped = 83,
        HubGrowthEquip = 84,
        HubGrowthUpgrade = 85,
        HubGrowthMaximum = 86,
        HubGrowthInsufficient = 87,
        HubGrowthLoading = 88,
        HubGrowthError = 89,
        HubGrowthRank = 90,
        HubGrowthEffect = 91,
        HubGrowthCost = 92,
        HubCultivationReady = 93,
        HubCultivationLocked = 94,
        HubCultivationUpgrade = 95,
        HubCultivationMaximum = 96,
        HubGrowthPreviewApplied = 97,
        HubGrowthPreviewSuppressed = 98,
        HubGrowthPreviewEmpty = 99,
        HubGrowthPreviewError = 100,
        HubCultivationLockedAction = 101,
    }

    public readonly struct RuntimeUiCopyDefinition
    {
        public RuntimeUiCopyDefinition(RuntimeUiCopyId id, string text,
            RuntimeUiTypographyRole role, RuntimeUiTextTone tone,
            TextAnchor alignment, RuntimeUiCopyLinePolicy linePolicy,
            int maximumLineCount)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Runtime UI copy cannot be empty.", nameof(text));
            if (maximumLineCount <= 0 || maximumLineCount > 2)
                throw new ArgumentOutOfRangeException(nameof(maximumLineCount));
            Id = id;
            Text = text;
            Role = role;
            Tone = tone;
            Alignment = alignment;
            LinePolicy = linePolicy;
            MaximumLineCount = maximumLineCount;
        }

        public RuntimeUiCopyId Id { get; }
        public string Text { get; }
        public RuntimeUiTypographyRole Role { get; }
        public RuntimeUiTextTone Tone { get; }
        public TextAnchor Alignment { get; }
        public RuntimeUiCopyLinePolicy LinePolicy { get; }
        public int MaximumLineCount { get; }
    }

    /// <summary>
    /// Finite authority for stable product chrome copy. Simulation reasons,
    /// content names and numeric values remain owned by their existing systems.
    /// </summary>
    public static class RuntimeUiCopyCatalog
    {
        public const int Count = 102;

        public static RuntimeUiCopyDefinition Get(RuntimeUiCopyId id)
        {
            switch (id)
            {
                case RuntimeUiCopyId.ProductTitle: return Single(id, "果园防线",
                    RuntimeUiTypographyRole.SectionTitle, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BootstrapLoading: return Single(id, "正在启动果园防线",
                    RuntimeUiTypographyRole.Body, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BootstrapRetry: return Single(id, "重试",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.BootstrapLevelUnavailable: return Single(id,
                    "启动失败：所选关卡不可用", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BootstrapConfigurationUnavailable: return Single(id,
                    "启动失败：运行配置不可用", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BootstrapContentUnavailable: return Single(id,
                    "启动失败：关卡内容不可用", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BootstrapPageUnavailable: return Single(id,
                    "启动失败：页面不可用", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BootstrapUnknownFailure: return Single(id,
                    "启动失败，请重试", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BootstrapRecoverableError: return Single(id,
                    "可恢复错误：scene-load-failed", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.LobbyTitle: return Single(id, "果园防线",
                    RuntimeUiTypographyRole.ScreenTitle, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.LobbyOrchard01Title: return Single(id,
                    "第一关 · U形教学", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.LobbyOrchard01Body: return Single(id,
                    "宽松路线｜种植合成", RuntimeUiTypographyRole.Supplemental,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.LobbyOrchard02Title: return Single(id,
                    "第二关 · S形覆盖", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.LobbyOrchard02Body: return Single(id,
                    "连续转弯｜快攻护甲", RuntimeUiTypographyRole.Supplemental,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.LobbyOrchard03Title: return Single(id,
                    "第三关 · 核心走廊", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.LobbyOrchard03Body: return Single(id,
                    "短线压迫｜首领冲击", RuntimeUiTypographyRole.Supplemental,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.LobbyStart: return Single(id,
                    "开始战斗", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.LobbyTransitioning: return Single(id, "正在进入…",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.LobbyError: return TwoLines(id,
                    FormatLobbyError("lobby-level-selection-mismatch"),
                    RuntimeUiTypographyRole.Body, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.BattleTitle: return Single(id, "水果塔防",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BattleSun: return Single(id, "阳光",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BattleCore: return Single(id, "核心",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BattleWave: return Single(id, "波次",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BattleReady: return Single(id, "准备阶段",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.BattleStartWave: return Single(id, "开始波次",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.BattleBetweenWave: return Single(id,
                    FormatBetweenWaveStatus(10), RuntimeUiTypographyRole.Supplemental,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.BattleStartNextWave: return Single(id,
                    "立即开始下一波", RuntimeUiTypographyRole.Supplemental,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.BattleVictoryStatus: return Single(id, "防守成功",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.BattleDefeatStatus: return Single(id, "核心失守",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.BattlePausedTitle: return Single(id, "游戏暂停",
                    RuntimeUiTypographyRole.SectionTitle, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.BattlePausedMessage: return Single(id,
                    "按空格或选择操作", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.BattleContinue: return Single(id, "继续游戏",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.BattleRestart: return Single(id, "重新开始",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.BattleVictoryTitle: return Single(id, "果园守住了！",
                    RuntimeUiTypographyRole.SectionTitle, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.BattleVictoryMessage: return TwoLines(id,
                    FormatVictoryMessage(15), RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.BattleDefeatTitle: return Single(id,
                    "僵尸闯进果园了", RuntimeUiTypographyRole.SectionTitle,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.BattleDefeatMessage: return Single(id,
                    FormatDefeatMessage(15), RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.BattleContextTray: return Single(id, "构筑栏",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.BattleNurseryTray: return Single(id, "刷新栏",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.BattleNurseryPotStored: return Single(id, "花盆入库",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.BattleNurseryEmpty: return Single(id, "空位",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.BattleRefresh: return Single(id,
                    FormatRefreshAction(99), RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.BattleDefaultGuidance: return TwoLines(id,
                    "点击水果查看信息；拖动完成种植、移动、返回与合成",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.SettlementTitle: return Single(id, "战斗结算",
                    RuntimeUiTypographyRole.ScreenTitle, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.SettlementVictory: return Single(id, "胜利",
                    RuntimeUiTypographyRole.Display, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.SettlementDefeat: return Single(id, "失败",
                    RuntimeUiTypographyRole.Display, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.SettlementCompletedLevel: return Single(id, "完成关卡",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.SettlementReachedWave: return Single(id, "到达波次",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.SettlementRemainingLives: return Single(id, "剩余生命",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.SettlementReturning: return Single(id, "正在返回大厅",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.SettlementRetry: return Single(id, "再来一局",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.SettlementReturn: return Single(id, "返回大厅",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.SettlementTransitioning: return Single(id, "正在切换…",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.SettlementRecoveredError: return TwoLines(id,
                    FormatSettlementRecoveredError("settlement-result-level-mismatch"),
                    RuntimeUiTypographyRole.Body, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.BattleVictoryOutcome: return Single(id, "胜利",
                    RuntimeUiTypographyRole.SectionTitle, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.BattleDefeatOutcome: return Single(id, "失败",
                    RuntimeUiTypographyRole.SectionTitle, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubHomeTitle: return Single(id, "果园守卫",
                    RuntimeUiTypographyRole.ScreenTitle, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.HubActivityTitle: return Single(id, "活动",
                    RuntimeUiTypographyRole.ScreenTitle, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.HubGrowthTitle: return Single(id, "成长",
                    RuntimeUiTypographyRole.ScreenTitle, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.HubNavHome: return Single(id, "主页",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.HubNavActivity: return Single(id, "活动",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.HubNavGrowth: return Single(id, "成长",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.HubGrowthEquipmentTab: return Single(id, "装备",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.HubGrowthCultivationTab: return Single(id, "养成",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.HubUnavailableTitle: return Single(id, "暂未开放",
                    RuntimeUiTypographyRole.SectionTitle, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubActivityUnavailableBody: return TwoLines(id,
                    "当前版本没有可领取的活动内容", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.HubEquipmentUnavailableBody: return TwoLines(id,
                    "当前版本尚无可用的账号装备内容", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.HubCultivationUnavailableBody: return TwoLines(id,
                    "当前版本尚无可用的养成节点", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.HubHomeGrowthPreviewTitle: return Single(id,
                    "战前成长", RuntimeUiTypographyRole.SectionTitle,
                    TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.HubHomeGrowthPreviewUnavailableBody: return TwoLines(id,
                    "当前玩法尚未载入可用的成长强化", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.BootstrapProfileUnsupported: return TwoLines(id,
                    "存档版本不兼容，请重置本地存档", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.BootstrapProfileReset: return Single(id, "重置本地存档",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter);
                case RuntimeUiCopyId.BootstrapProfileResetting: return Single(id,
                    "正在重置存档…", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubResourceMorningDew: return Single(id, "晨露",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.HubActivityRewardTitle: return Single(id, "奖励预览",
                    RuntimeUiTypographyRole.SectionTitle, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.HubActivityClaim: return Single(id, "领取奖励",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.HubActivityClaiming: return Single(id, "领取中…",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubActivityClaimed: return Single(id, "[完成] 已领取",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubActivityLocked: return Single(id, "[锁定] 活动未开放",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubActivityClaimable: return Single(id, "可领取",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubActivityError: return Single(id, "[错误] 保存失败，可重试",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthOwned: return Single(id, "已拥有",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthLocked: return Single(id, "[锁定] 尚未获得",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthEquipped: return Single(id, "[完成] 已装备",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthEquip: return Single(id, "装备",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.HubGrowthUpgrade: return Single(id, "强化",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.HubGrowthMaximum: return Single(id, "[完成] 已满级",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthInsufficient: return Single(id,
                    "材料不足", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthLoading: return Single(id, "保存中…",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthError: return Single(id,
                    "[错误] 保存失败，可重试", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthRank: return Single(id, "等级",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.HubGrowthEffect: return Single(id, "效果",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.HubGrowthCost: return Single(id, "消耗",
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
                case RuntimeUiCopyId.HubCultivationReady: return Single(id, "可培育",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleLeft,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubCultivationLocked: return Single(id,
                    "[锁定] 前置条件未满足", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubCultivationLockedAction: return Single(id,
                    "前置未满足", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubCultivationUpgrade: return Single(id, "培育",
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.Inverse);
                case RuntimeUiCopyId.HubCultivationMaximum: return Single(id,
                    "[完成] 培育完成", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleCenter, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthPreviewApplied: return Single(id,
                    "[生效] 已生效", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthPreviewSuppressed: return Single(id,
                    "[受限] 本关受限", RuntimeUiTypographyRole.ControlLabel,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.State);
                case RuntimeUiCopyId.HubGrowthPreviewEmpty: return TwoLines(id,
                    "当前没有已装备或已培育的成长效果", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.Secondary);
                case RuntimeUiCopyId.HubGrowthPreviewError: return TwoLines(id,
                    "成长载入失败，暂时不能开始战斗", RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft, RuntimeUiTextTone.State);
                default: throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        public static string FormatBootstrapRecoverableError(string errorCode)
        {
            return "可恢复错误：" + (errorCode ?? string.Empty);
        }

        public static string LevelDisplayName(string levelId)
        {
            switch (levelId)
            {
                case "orchard-01": return "第一关";
                case "orchard-02": return "第二关";
                case "orchard-03": return "第三关";
                default: return "当前关卡";
            }
        }

        public static string FormatLobbyError(string errorCode)
        {
            return "暂时无法继续：" + (errorCode ?? string.Empty);
        }

        public static string FormatHubBalance(long quantity)
        {
            return "晨露 " + quantity;
        }

        public static string FormatHubRank(int rank, int maximumRank)
        {
            return "等级 " + rank + "/" + maximumRank;
        }

        public static string FormatHubCost(string itemName, long required,
            long available)
        {
            return "消耗 " + required + " " + (itemName ?? string.Empty)
                + " · 持有 " + available;
        }

        public static string FormatHubPercentEffect(string label, float value)
        {
            return (label ?? string.Empty) + " +"
                + Mathf.RoundToInt(value * 100f) + "%";
        }

        public static string FormatActiveWaveStatus(int waveIndex, int zombieCount)
        {
            return "第 " + waveIndex + " 波 · " + zombieCount + " 个敌人";
        }

        public static string FormatBetweenWaveStatus(int seconds)
        {
            return "下一波倒计时 " + seconds + " 秒";
        }

        public static string FormatVictoryMessage(int maximumWaveCount)
        {
            return "成功抵御全部 " + maximumWaveCount + " 波僵尸";
        }

        public static RuntimeUiStatusTextLines FormatVictoryMessageLines(
            int maximumWaveCount)
        {
            return new RuntimeUiStatusTextLines("成功抵御全部",
                maximumWaveCount + " 波僵尸");
        }

        public static string FormatDefeatMessage(int reachedWave)
        {
            return "坚持到第 " + reachedWave + " 波";
        }

        public static string FormatRefreshAction(int cost)
        {
            return "刷新五株水果 · 消耗 " + cost + " 阳光";
        }

        public static string FormatSettlementRecoveredError(string errorCode)
        {
            return "已安全处理：" + (errorCode ?? string.Empty);
        }

        public static RuntimeUiStatusTextMode StatusTextMode(
            RuntimeUiCopyDefinition copy)
        {
            switch (copy.LinePolicy)
            {
                case RuntimeUiCopyLinePolicy.SingleLine:
                    return RuntimeUiStatusTextMode.SingleLine;
                case RuntimeUiCopyLinePolicy.ControlledTwoLines:
                    return RuntimeUiStatusTextMode.CompactTwoLines;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(copy), copy.LinePolicy, null);
            }
        }

        private static RuntimeUiCopyDefinition Single(RuntimeUiCopyId id, string text,
            RuntimeUiTypographyRole role, TextAnchor alignment,
            RuntimeUiTextTone tone = RuntimeUiTextTone.Primary)
        {
            return new RuntimeUiCopyDefinition(id, text, role, tone, alignment,
                RuntimeUiCopyLinePolicy.SingleLine, 1);
        }

        private static RuntimeUiCopyDefinition TwoLines(RuntimeUiCopyId id, string text,
            RuntimeUiTypographyRole role, TextAnchor alignment,
            RuntimeUiTextTone tone = RuntimeUiTextTone.Primary)
        {
            return new RuntimeUiCopyDefinition(id, text, role, tone, alignment,
                RuntimeUiCopyLinePolicy.ControlledTwoLines, 2);
        }
    }
}
