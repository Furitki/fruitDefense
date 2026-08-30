using System;
using System.Collections.Generic;
using FruitDefense.Content;
using FruitDefense.Presentation;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Editor
{
    internal enum RuntimeUiTextInspectionTarget
    {
        BootstrapTitle,
        BootstrapStatus,
        BootstrapRetry,
        BootstrapRecoverableStatus,
        LobbyTitle,
        LobbyOrchard01Title,
        LobbyOrchard01Body,
        LobbyOrchard02Title,
        LobbyOrchard02Body,
        LobbyOrchard03Title,
        LobbyOrchard03Body,
        LobbyStart,
        LobbyStatus,
        BattleHeaderTitle,
        BattleSunMetric,
        BattleCoreMetric,
        BattleWaveMetric,
        BattlePhaseStatus,
        BattlePhaseStatusFull,
        BattleWaveAction,
        BattleContextTrayTitle,
        BattleNurseryTrayTitle,
        BattleNurserySlot,
        BattleToolCount,
        BattlePotCount,
        BattleNurseryStars,
        BattleRefreshAction,
        BattleDetailTitle,
        BattleDetailBody,
        BattleMergeHint,
        BattleModalTitle,
        BattleModalMessage,
        BattleModalResultBanner,
        BattleModalTerminalMessage,
        BattleModalPrimaryAction,
        BattleModalSecondaryAction,
        BattleModalTerminalAction,
        SettlementTitle,
        SettlementOutcome,
        SettlementCompletedLevel,
        SettlementReachedWave,
        SettlementRemainingLives,
        SettlementRetry,
        SettlementReturn,
        SettlementStatus,
    }

    internal readonly struct RuntimeUiTextInspectionCase
    {
        public RuntimeUiTextInspectionCase(string id, RuntimeUiCopyId copyId,
            RuntimeUiTextInspectionTarget target, RuntimeUiInteractionState state,
            RuntimeUiActionKind actionKind = RuntimeUiActionKind.Primary,
            RuntimeUiArtSlot? iconSlot = null,
            BattleUiActionSemantic? actionSemantic = null,
            string metricValue = null)
            : this(id, RuntimeUiCopyCatalog.Get(copyId), target, state,
                true, actionKind, iconSlot, actionSemantic, metricValue)
        {
        }

        public RuntimeUiTextInspectionCase(string id,
            RuntimeUiCopyDefinition copy, RuntimeUiTextInspectionTarget target,
            RuntimeUiInteractionState state, bool coversCatalogCopy,
            RuntimeUiActionKind actionKind = RuntimeUiActionKind.Primary,
            RuntimeUiArtSlot? iconSlot = null,
            BattleUiActionSemantic? actionSemantic = null,
            string metricValue = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A stable text inspection ID is required.", nameof(id));
            Id = id;
            Copy = copy;
            Target = target;
            State = state;
            CoversCatalogCopy = coversCatalogCopy;
            ActionKind = actionKind;
            IconSlot = iconSlot;
            ActionSemantic = actionSemantic;
            MetricValue = metricValue ?? string.Empty;
        }

        public string Id { get; }
        public RuntimeUiCopyId CopyId => Copy.Id;
        public RuntimeUiCopyDefinition Copy { get; }
        public RuntimeUiTextInspectionTarget Target { get; }
        public RuntimeUiInteractionState State { get; }
        public bool CoversCatalogCopy { get; }
        public RuntimeUiActionKind ActionKind { get; }
        public RuntimeUiArtSlot? IconSlot { get; }
        public BattleUiActionSemantic? ActionSemantic { get; }
        public string MetricValue { get; }
        public bool HasIcon => IconSlot.HasValue;

        public RuntimeUiActionSpec ActionSpec => ActionSemantic.HasValue
            ? BattleUiPresentationState.ResolveActionSpec(ActionSemantic.Value)
            : new RuntimeUiActionSpec(ActionKind,
                HasIcon ? RuntimeUiActionContentForm.IconLabel
                    : RuntimeUiActionContentForm.Text,
                RuntimeUiActionBehavior.Instantaneous);
    }

    /// <summary>
    /// Finite inspection matrix for stable player-facing product copy. Dynamic
    /// simulation reasons and content names are added by the owning route smoke
    /// as representative boundary cases rather than becoming product-copy tokens.
    /// </summary>
    internal static class RuntimeUiTextInspectionCatalog
    {
        private static readonly RuntimeUiTextInspectionCase[] InspectionCases =
            BuildInspectionCases();

        private static RuntimeUiTextInspectionCase[] BuildInspectionCases()
        {
            var cases = new List<RuntimeUiTextInspectionCase>
            {
                Case("bootstrap.title", RuntimeUiCopyId.ProductTitle,
                RuntimeUiTextInspectionTarget.BootstrapTitle,
                RuntimeUiInteractionState.Loading),
            Case("bootstrap.loading", RuntimeUiCopyId.BootstrapLoading,
                RuntimeUiTextInspectionTarget.BootstrapStatus,
                RuntimeUiInteractionState.Loading),
            Case("bootstrap.level-error", RuntimeUiCopyId.BootstrapLevelUnavailable,
                RuntimeUiTextInspectionTarget.BootstrapStatus,
                RuntimeUiInteractionState.Error),
            Case("bootstrap.configuration-error",
                RuntimeUiCopyId.BootstrapConfigurationUnavailable,
                RuntimeUiTextInspectionTarget.BootstrapStatus,
                RuntimeUiInteractionState.Error),
            Case("bootstrap.content-error", RuntimeUiCopyId.BootstrapContentUnavailable,
                RuntimeUiTextInspectionTarget.BootstrapStatus,
                RuntimeUiInteractionState.Error),
            Case("bootstrap.page-error", RuntimeUiCopyId.BootstrapPageUnavailable,
                RuntimeUiTextInspectionTarget.BootstrapStatus,
                RuntimeUiInteractionState.Error),
            Case("bootstrap.unknown-error", RuntimeUiCopyId.BootstrapUnknownFailure,
                RuntimeUiTextInspectionTarget.BootstrapStatus,
                RuntimeUiInteractionState.Error),
            Action("bootstrap.retry", RuntimeUiCopyId.BootstrapRetry,
                RuntimeUiTextInspectionTarget.BootstrapRetry,
                RuntimeUiInteractionState.Normal, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlRetry),
            Action("bootstrap.retry-pressed", RuntimeUiCopyId.BootstrapRetry,
                RuntimeUiTextInspectionTarget.BootstrapRetry,
                RuntimeUiInteractionState.Pressed, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlRetry),
            Case("bootstrap.recoverable", RuntimeUiCopyId.BootstrapRecoverableError,
                RuntimeUiTextInspectionTarget.BootstrapRecoverableStatus,
                RuntimeUiInteractionState.Warning),

            Case("lobby.title", RuntimeUiCopyId.LobbyTitle,
                RuntimeUiTextInspectionTarget.LobbyTitle,
                RuntimeUiInteractionState.Normal),
            Case("lobby.orchard-01.title", RuntimeUiCopyId.LobbyOrchard01Title,
                RuntimeUiTextInspectionTarget.LobbyOrchard01Title,
                RuntimeUiInteractionState.Selected),
            Case("lobby.orchard-01.body", RuntimeUiCopyId.LobbyOrchard01Body,
                RuntimeUiTextInspectionTarget.LobbyOrchard01Body,
                RuntimeUiInteractionState.Selected),
            Case("lobby.orchard-02.title", RuntimeUiCopyId.LobbyOrchard02Title,
                RuntimeUiTextInspectionTarget.LobbyOrchard02Title,
                RuntimeUiInteractionState.Disabled),
            Case("lobby.orchard-02.body", RuntimeUiCopyId.LobbyOrchard02Body,
                RuntimeUiTextInspectionTarget.LobbyOrchard02Body,
                RuntimeUiInteractionState.Disabled),
            Case("lobby.orchard-03.title", RuntimeUiCopyId.LobbyOrchard03Title,
                RuntimeUiTextInspectionTarget.LobbyOrchard03Title,
                RuntimeUiInteractionState.Loading),
            Case("lobby.orchard-03.body", RuntimeUiCopyId.LobbyOrchard03Body,
                RuntimeUiTextInspectionTarget.LobbyOrchard03Body,
                RuntimeUiInteractionState.Loading),
            Action("lobby.start", RuntimeUiCopyId.LobbyStart,
                RuntimeUiTextInspectionTarget.LobbyStart,
                RuntimeUiInteractionState.Normal, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlStart),
            Action("lobby.start-pressed", RuntimeUiCopyId.LobbyStart,
                RuntimeUiTextInspectionTarget.LobbyStart,
                RuntimeUiInteractionState.Pressed, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlStart),
            Action("lobby.entering", RuntimeUiCopyId.LobbyTransitioning,
                RuntimeUiTextInspectionTarget.LobbyStart,
                RuntimeUiInteractionState.Loading, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlStart),
            Case("lobby.error", RuntimeUiCopyId.LobbyError,
                RuntimeUiTextInspectionTarget.LobbyStatus,
                RuntimeUiInteractionState.Error),

            Case("battle.title", RuntimeUiCopyId.BattleTitle,
                RuntimeUiTextInspectionTarget.BattleHeaderTitle,
                RuntimeUiInteractionState.Normal),
            Case("battle.sun", RuntimeUiCopyId.BattleSun,
                RuntimeUiTextInspectionTarget.BattleSunMetric,
                RuntimeUiInteractionState.Normal),
            Case("battle.core", RuntimeUiCopyId.BattleCore,
                RuntimeUiTextInspectionTarget.BattleCoreMetric,
                RuntimeUiInteractionState.Warning),
            Case("battle.wave", RuntimeUiCopyId.BattleWave,
                RuntimeUiTextInspectionTarget.BattleWaveMetric,
                RuntimeUiInteractionState.Normal),
            Case("battle.ready", RuntimeUiCopyId.BattleReady,
                RuntimeUiTextInspectionTarget.BattlePhaseStatus,
                RuntimeUiInteractionState.Normal),
            Case("battle.default-guidance", RuntimeUiCopyId.BattleDefaultGuidance,
                RuntimeUiTextInspectionTarget.BattlePhaseStatusFull,
                RuntimeUiInteractionState.Normal),
            Case("battle.between", RuntimeUiCopyId.BattleBetweenWave,
                RuntimeUiTextInspectionTarget.BattlePhaseStatus,
                RuntimeUiInteractionState.Warning),
            Case("battle.victory-status", RuntimeUiCopyId.BattleVictoryStatus,
                RuntimeUiTextInspectionTarget.BattlePhaseStatusFull,
                RuntimeUiInteractionState.Success),
            Case("battle.defeat-status", RuntimeUiCopyId.BattleDefeatStatus,
                RuntimeUiTextInspectionTarget.BattlePhaseStatusFull,
                RuntimeUiInteractionState.Error),
            Action("battle.start-wave", RuntimeUiCopyId.BattleStartWave,
                RuntimeUiTextInspectionTarget.BattleWaveAction,
                RuntimeUiInteractionState.Normal, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlStartWave),
            Action("battle.start-next-wave", RuntimeUiCopyId.BattleStartNextWave,
                RuntimeUiTextInspectionTarget.BattleWaveAction,
                RuntimeUiInteractionState.Pressed, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlStartWave),
            Case("battle.context-tray", RuntimeUiCopyId.BattleContextTray,
                RuntimeUiTextInspectionTarget.BattleContextTrayTitle,
                RuntimeUiInteractionState.Normal),
            Case("battle.nursery-tray", RuntimeUiCopyId.BattleNurseryTray,
                RuntimeUiTextInspectionTarget.BattleNurseryTrayTitle,
                RuntimeUiInteractionState.Normal),
            Case("battle.pot-stored", RuntimeUiCopyId.BattleNurseryPotStored,
                RuntimeUiTextInspectionTarget.BattleNurserySlot,
                RuntimeUiInteractionState.Success),
            Case("battle.empty", RuntimeUiCopyId.BattleNurseryEmpty,
                RuntimeUiTextInspectionTarget.BattleNurserySlot,
                RuntimeUiInteractionState.Normal),
            SemanticAction("battle.refresh", RuntimeUiCopyId.BattleRefresh,
                RuntimeUiTextInspectionTarget.BattleRefreshAction,
                RuntimeUiInteractionState.Normal,
                BattleUiActionSemantic.NurseryRefresh,
                RuntimeUiArtSlot.IconControlRefresh),
            Case("battle.pause-title", RuntimeUiCopyId.BattlePausedTitle,
                RuntimeUiTextInspectionTarget.BattleModalTitle,
                RuntimeUiInteractionState.Warning),
            Case("battle.pause-message", RuntimeUiCopyId.BattlePausedMessage,
                RuntimeUiTextInspectionTarget.BattleModalMessage,
                RuntimeUiInteractionState.Warning),
            Action("battle.continue", RuntimeUiCopyId.BattleContinue,
                RuntimeUiTextInspectionTarget.BattleModalPrimaryAction,
                RuntimeUiInteractionState.Normal, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlContinue),
            Action("battle.restart-secondary", RuntimeUiCopyId.BattleRestart,
                RuntimeUiTextInspectionTarget.BattleModalSecondaryAction,
                RuntimeUiInteractionState.Normal, RuntimeUiActionKind.Danger,
                RuntimeUiArtSlot.IconControlRetry),
            Case("battle.victory-title", RuntimeUiCopyId.BattleVictoryTitle,
                RuntimeUiTextInspectionTarget.BattleModalTitle,
                RuntimeUiInteractionState.Success),
            Case("battle.victory-message", RuntimeUiCopyId.BattleVictoryMessage,
                RuntimeUiTextInspectionTarget.BattleModalTerminalMessage,
                RuntimeUiInteractionState.Success),
            Case("battle.victory-outcome", RuntimeUiCopyId.BattleVictoryOutcome,
                RuntimeUiTextInspectionTarget.BattleModalResultBanner,
                RuntimeUiInteractionState.Success),
            Case("battle.defeat-title", RuntimeUiCopyId.BattleDefeatTitle,
                RuntimeUiTextInspectionTarget.BattleModalTitle,
                RuntimeUiInteractionState.Error),
            Case("battle.defeat-message", RuntimeUiCopyId.BattleDefeatMessage,
                RuntimeUiTextInspectionTarget.BattleModalTerminalMessage,
                RuntimeUiInteractionState.Error),
            Case("battle.defeat-outcome", RuntimeUiCopyId.BattleDefeatOutcome,
                RuntimeUiTextInspectionTarget.BattleModalResultBanner,
                RuntimeUiInteractionState.Error),
            Action("battle.restart-terminal", RuntimeUiCopyId.BattleRestart,
                RuntimeUiTextInspectionTarget.BattleModalTerminalAction,
                RuntimeUiInteractionState.Normal, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlRetry),

            Case("settlement.title", RuntimeUiCopyId.SettlementTitle,
                RuntimeUiTextInspectionTarget.SettlementTitle,
                RuntimeUiInteractionState.Normal),
            Case("settlement.victory", RuntimeUiCopyId.SettlementVictory,
                RuntimeUiTextInspectionTarget.SettlementOutcome,
                RuntimeUiInteractionState.Success),
            Case("settlement.defeat", RuntimeUiCopyId.SettlementDefeat,
                RuntimeUiTextInspectionTarget.SettlementOutcome,
                RuntimeUiInteractionState.Error),
            Case("settlement.returning", RuntimeUiCopyId.SettlementReturning,
                RuntimeUiTextInspectionTarget.SettlementOutcome,
                RuntimeUiInteractionState.Loading),
            Case("settlement.completed", RuntimeUiCopyId.SettlementCompletedLevel,
                RuntimeUiTextInspectionTarget.SettlementCompletedLevel,
                RuntimeUiInteractionState.Normal),
            Case("settlement.wave", RuntimeUiCopyId.SettlementReachedWave,
                RuntimeUiTextInspectionTarget.SettlementReachedWave,
                RuntimeUiInteractionState.Normal),
            Case("settlement.lives", RuntimeUiCopyId.SettlementRemainingLives,
                RuntimeUiTextInspectionTarget.SettlementRemainingLives,
                RuntimeUiInteractionState.Normal),
            Action("settlement.retry", RuntimeUiCopyId.SettlementRetry,
                RuntimeUiTextInspectionTarget.SettlementRetry,
                RuntimeUiInteractionState.Normal, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlRetry),
            Action("settlement.return", RuntimeUiCopyId.SettlementReturn,
                RuntimeUiTextInspectionTarget.SettlementReturn,
                RuntimeUiInteractionState.Normal, RuntimeUiActionKind.Quiet,
                RuntimeUiArtSlot.IconControlReturn),
            Action("settlement.transitioning", RuntimeUiCopyId.SettlementTransitioning,
                RuntimeUiTextInspectionTarget.SettlementRetry,
                RuntimeUiInteractionState.Loading, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlRetry),
            Case("settlement.recovered-error",
                RuntimeUiCopyId.SettlementRecoveredError,
                RuntimeUiTextInspectionTarget.SettlementStatus,
                RuntimeUiInteractionState.Warning),
            };

            AddBattleBoundaryCases(cases);
            return cases.ToArray();
        }

        public static IReadOnlyList<RuntimeUiTextInspectionCase> Cases => InspectionCases;

        private static RuntimeUiTextInspectionCase Case(string id,
            RuntimeUiCopyId copyId, RuntimeUiTextInspectionTarget target,
            RuntimeUiInteractionState state)
        {
            return new RuntimeUiTextInspectionCase(id, copyId, target, state);
        }

        private static RuntimeUiTextInspectionCase Action(string id,
            RuntimeUiCopyId copyId, RuntimeUiTextInspectionTarget target,
            RuntimeUiInteractionState state, RuntimeUiActionKind actionKind,
            RuntimeUiArtSlot iconSlot)
        {
            return new RuntimeUiTextInspectionCase(id, copyId, target, state,
                actionKind, iconSlot);
        }

        private static RuntimeUiTextInspectionCase SemanticAction(string id,
            RuntimeUiCopyId copyId, RuntimeUiTextInspectionTarget target,
            RuntimeUiInteractionState state, BattleUiActionSemantic semantic,
            RuntimeUiArtSlot iconSlot)
        {
            return new RuntimeUiTextInspectionCase(id, copyId, target, state,
                iconSlot: iconSlot, actionSemantic: semantic);
        }

        private static RuntimeUiTextInspectionCase Boundary(string id,
            RuntimeUiCopyId anatomyCopyId, RuntimeUiTextInspectionTarget target,
            RuntimeUiInteractionState state, string text,
            BattleUiActionSemantic? actionSemantic = null,
            RuntimeUiArtSlot? iconSlot = null, string metricValue = null)
        {
            var anatomy = RuntimeUiCopyCatalog.Get(anatomyCopyId);
            var copy = new RuntimeUiCopyDefinition(anatomy.Id, text,
                anatomy.Role, anatomy.Tone, anatomy.Alignment,
                anatomy.LinePolicy, anatomy.MaximumLineCount);
            return new RuntimeUiTextInspectionCase(id, copy, target, state,
                false, iconSlot: iconSlot, actionSemantic: actionSemantic,
                metricValue: metricValue);
        }

        private static void AddBattleBoundaryCases(
            ICollection<RuntimeUiTextInspectionCase> cases)
        {
            cases.Add(Boundary("battle.metric.sun.max", RuntimeUiCopyId.BattleSun,
                RuntimeUiTextInspectionTarget.BattleSunMetric,
                RuntimeUiInteractionState.Normal, "阳光", metricValue: "999"));
            cases.Add(Boundary("battle.metric.core.max", RuntimeUiCopyId.BattleCore,
                RuntimeUiTextInspectionTarget.BattleCoreMetric,
                RuntimeUiInteractionState.Warning, "核心", metricValue: "99"));
            cases.Add(Boundary("battle.metric.wave.max", RuntimeUiCopyId.BattleWave,
                RuntimeUiTextInspectionTarget.BattleWaveMetric,
                RuntimeUiInteractionState.Normal, "波次", metricValue: "15"));
            cases.Add(Boundary("battle.tool-count.max", RuntimeUiCopyId.BattleContextTray,
                RuntimeUiTextInspectionTarget.BattleToolCount,
                RuntimeUiInteractionState.Normal, "99"));
            cases.Add(Boundary("battle.pot-count.max", RuntimeUiCopyId.BattleContextTray,
                RuntimeUiTextInspectionTarget.BattlePotCount,
                RuntimeUiInteractionState.Normal, "99"));
            cases.Add(Boundary("battle.nursery-stars.max",
                RuntimeUiCopyId.BattleNurseryPotStored,
                RuntimeUiTextInspectionTarget.BattleNurseryStars,
                RuntimeUiInteractionState.Selected, "★★★★"));
            cases.Add(Boundary("battle.refresh-cost.max", RuntimeUiCopyId.BattleRefresh,
                RuntimeUiTextInspectionTarget.BattleRefreshAction,
                RuntimeUiInteractionState.Normal,
                RuntimeUiCopyCatalog.FormatRefreshAction(999),
                BattleUiActionSemantic.NurseryRefresh,
                RuntimeUiArtSlot.IconControlRefresh));
            cases.Add(Boundary("battle.status.active-wave.max", RuntimeUiCopyId.BattleReady,
                RuntimeUiTextInspectionTarget.BattlePhaseStatusFull,
                RuntimeUiInteractionState.Normal,
                RuntimeUiCopyCatalog.FormatActiveWaveStatus(15, 99)));
            cases.Add(Boundary("battle.status.between-wave.max", RuntimeUiCopyId.BattleBetweenWave,
                RuntimeUiTextInspectionTarget.BattlePhaseStatus,
                RuntimeUiInteractionState.Warning,
                RuntimeUiCopyCatalog.FormatBetweenWaveStatus(10)));
            cases.Add(Boundary("battle.status.success-prefix.max",
                RuntimeUiCopyId.BattleDefaultGuidance,
                RuntimeUiTextInspectionTarget.BattlePhaseStatusFull,
                RuntimeUiInteractionState.Success,
                BattleUiPresentationState.FormatTransientStatus(true,
                    "刷新完成：水果 5 株，花盆×5 已入库")));
            cases.Add(Boundary("battle.status.error-prefix.max",
                RuntimeUiCopyId.BattleDefaultGuidance,
                RuntimeUiTextInspectionTarget.BattlePhaseStatusFull,
                RuntimeUiInteractionState.Error,
                BattleUiPresentationState.FormatTransientStatus(false,
                    "目标植物移动冷却 10.0 秒")));
            cases.Add(Boundary("battle.merge-hint.max", RuntimeUiCopyId.BattleContextTray,
                RuntimeUiTextInspectionTarget.BattleMergeHint,
                RuntimeUiInteractionState.Warning, "可合成为 4 星"));

            var content = BundledBattleContentFactory.Create();
            for (var index = 0; index < content.plants.Length; index++)
            {
                var plant = content.plants[index];
                cases.Add(Boundary("battle.detail-title.plant." + plant.id,
                    RuntimeUiCopyId.BattleTitle,
                    RuntimeUiTextInspectionTarget.BattleDetailTitle,
                    RuntimeUiInteractionState.Selected,
                    plant.displayName + " · 4 星"));
            }
            for (var index = 0; index < content.equipment.Length; index++)
            {
                var equipment = content.equipment[index];
                cases.Add(Boundary("battle.detail-body.equipment." + equipment.id,
                    RuntimeUiCopyId.BattleContextTray,
                    RuntimeUiTextInspectionTarget.BattleDetailBody,
                    RuntimeUiInteractionState.Selected,
                    "伤害 999 · 范围 999 · 装备 " + equipment.displayName));
            }
        }
    }
}
