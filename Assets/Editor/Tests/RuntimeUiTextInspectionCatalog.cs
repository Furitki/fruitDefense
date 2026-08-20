using System;
using System.Collections.Generic;
using FruitDefense.UI;

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
        BattleBoardStatus,
        BattleBoardStatusFull,
        BattleWaveAction,
        BattleToolTrayTitle,
        BattleNurseryTrayTitle,
        BattleNurserySlot,
        BattleRefreshAction,
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
            RuntimeUiArtSlot? iconSlot = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A stable text inspection ID is required.", nameof(id));
            Id = id;
            CopyId = copyId;
            Target = target;
            State = state;
            ActionKind = actionKind;
            IconSlot = iconSlot;
        }

        public string Id { get; }
        public RuntimeUiCopyId CopyId { get; }
        public RuntimeUiTextInspectionTarget Target { get; }
        public RuntimeUiInteractionState State { get; }
        public RuntimeUiActionKind ActionKind { get; }
        public RuntimeUiArtSlot? IconSlot { get; }
        public bool HasIcon => IconSlot.HasValue;
        public RuntimeUiCopyDefinition Copy => RuntimeUiCopyCatalog.Get(CopyId);
    }

    /// <summary>
    /// Finite inspection matrix for stable player-facing product copy. Dynamic
    /// simulation reasons and content names are added by the owning route smoke
    /// as representative boundary cases rather than becoming product-copy tokens.
    /// </summary>
    internal static class RuntimeUiTextInspectionCatalog
    {
        private static readonly RuntimeUiTextInspectionCase[] InspectionCases =
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
                RuntimeUiTextInspectionTarget.BattleBoardStatus,
                RuntimeUiInteractionState.Normal),
            Case("battle.default-guidance", RuntimeUiCopyId.BattleDefaultGuidance,
                RuntimeUiTextInspectionTarget.BattleBoardStatusFull,
                RuntimeUiInteractionState.Normal),
            Case("battle.between", RuntimeUiCopyId.BattleBetweenWave,
                RuntimeUiTextInspectionTarget.BattleBoardStatus,
                RuntimeUiInteractionState.Warning),
            Case("battle.victory-status", RuntimeUiCopyId.BattleVictoryStatus,
                RuntimeUiTextInspectionTarget.BattleBoardStatusFull,
                RuntimeUiInteractionState.Success),
            Case("battle.defeat-status", RuntimeUiCopyId.BattleDefeatStatus,
                RuntimeUiTextInspectionTarget.BattleBoardStatusFull,
                RuntimeUiInteractionState.Error),
            Action("battle.start-wave", RuntimeUiCopyId.BattleStartWave,
                RuntimeUiTextInspectionTarget.BattleWaveAction,
                RuntimeUiInteractionState.Normal, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlStartWave),
            Action("battle.start-next-wave", RuntimeUiCopyId.BattleStartNextWave,
                RuntimeUiTextInspectionTarget.BattleWaveAction,
                RuntimeUiInteractionState.Pressed, RuntimeUiActionKind.Primary,
                RuntimeUiArtSlot.IconControlStartWave),
            Case("battle.tool-tray", RuntimeUiCopyId.BattleToolTray,
                RuntimeUiTextInspectionTarget.BattleToolTrayTitle,
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
            Action("battle.refresh", RuntimeUiCopyId.BattleRefresh,
                RuntimeUiTextInspectionTarget.BattleRefreshAction,
                RuntimeUiInteractionState.Normal, RuntimeUiActionKind.Primary,
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
    }
}
