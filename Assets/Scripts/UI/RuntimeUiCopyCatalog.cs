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
        public const int Count = 56;

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
                    FormatLobbyStart("orchard-03"), RuntimeUiTypographyRole.ControlLabel,
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
                    RuntimeUiTypographyRole.SectionTitle, TextAnchor.MiddleCenter,
                    RuntimeUiTextTone.State);
                case RuntimeUiCopyId.SettlementDefeat: return Single(id, "失败",
                    RuntimeUiTypographyRole.SectionTitle, TextAnchor.MiddleCenter,
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
                default: throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        public static string FormatBootstrapRecoverableError(string errorCode)
        {
            return "可恢复错误：" + (errorCode ?? string.Empty);
        }

        public static string FormatLobbyStart(string selectedLevelId)
        {
            return "开始战斗 · " + LevelDisplayName(selectedLevelId);
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
