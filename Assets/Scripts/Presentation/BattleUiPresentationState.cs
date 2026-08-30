using System;
using FruitDefense.Core;
using FruitDefense.UI;

namespace FruitDefense.Presentation
{
    public enum BattleUiChromeMode
    {
        Ready = 0,
        ActiveWave = 1,
        BetweenWaves = 2,
        Paused = 3,
        Victory = 4,
        Defeat = 5,
    }

    public enum BattleUiPhaseMode
    {
        Ready = 0,
        ActiveWave = 1,
        BetweenWaves = 2,
        Victory = 3,
        Defeat = 4,
    }

    public enum BattleUiDropCue
    {
        None = 0,
        Legal = 1,
        Illegal = 2,
        Merge = 3,
        Swap = 4,
    }

    public enum BattleUiActionSemantic
    {
        StartWave = 0,
        NurseryRefresh = 1,
        PauseContinue = 2,
        Speed = 3,
        Close = 4,
    }

    public readonly struct BattleUiModalContent
    {
        public BattleUiModalContent(string title, string resultBannerText,
            RuntimeUiStatusTextLines messageLines,
            string primaryAction, string secondaryAction,
            RuntimeUiInteractionState surfaceState, bool usesResultCard,
            RuntimeUiActionKind primaryActionKind,
            RuntimeUiArtSlot primaryActionIcon,
            RuntimeUiActionKind secondaryActionKind,
            RuntimeUiArtSlot secondaryActionIcon)
        {
            Title = title;
            ResultBannerText = resultBannerText;
            MessageLines = messageLines;
            PrimaryAction = primaryAction;
            SecondaryAction = secondaryAction;
            SurfaceState = surfaceState;
            UsesResultCard = usesResultCard;
            PrimaryActionKind = primaryActionKind;
            PrimaryActionIcon = primaryActionIcon;
            SecondaryActionKind = secondaryActionKind;
            SecondaryActionIcon = secondaryActionIcon;
        }

        public string Title { get; }
        public string ResultBannerText { get; }
        public RuntimeUiStatusTextLines MessageLines { get; }
        public string PrimaryAction { get; }
        public string SecondaryAction { get; }
        public RuntimeUiInteractionState SurfaceState { get; }
        public bool UsesResultCard { get; }
        public RuntimeUiActionKind PrimaryActionKind { get; }
        public RuntimeUiArtSlot PrimaryActionIcon { get; }
        public RuntimeUiActionKind SecondaryActionKind { get; }
        public RuntimeUiArtSlot SecondaryActionIcon { get; }
        public int ActionCount => string.IsNullOrEmpty(SecondaryAction) ? 1 : 2;
    }

    /// <summary>
    /// Finite, presentation-only interpretation of authoritative battle phase state.
    /// Commands and simulation mutations remain owned by FruitDefenseGame/GameSimulation.
    /// </summary>
    public readonly struct BattleUiPresentationState
    {
        private BattleUiPresentationState(
            BattleUiChromeMode mode, BattleUiPhaseMode phaseMode, bool isPaused)
        {
            Mode = mode;
            PhaseMode = phaseMode;
            IsPaused = isPaused;
        }

        public BattleUiChromeMode Mode { get; }
        public BattleUiPhaseMode PhaseMode { get; }
        public bool IsPaused { get; }
        public bool BlocksDrag => Mode == BattleUiChromeMode.Paused
            || Mode == BattleUiChromeMode.Victory
            || Mode == BattleUiChromeMode.Defeat;
        public bool ShowsOverlay => Mode == BattleUiChromeMode.Paused
            || Mode == BattleUiChromeMode.Victory
            || Mode == BattleUiChromeMode.Defeat;
        public bool BlocksBackgroundInput => ShowsOverlay;
        public bool ShowsWaveAction => Mode == BattleUiChromeMode.Ready
            || Mode == BattleUiChromeMode.BetweenWaves;
        public int ModalActionCount => Mode == BattleUiChromeMode.Paused ? 2
            : Mode == BattleUiChromeMode.Victory || Mode == BattleUiChromeMode.Defeat ? 1 : 0;
        public RuntimeUiArtSlot PauseActionIcon => IsPaused
            ? RuntimeUiArtSlot.IconControlContinue
            : RuntimeUiArtSlot.IconControlPause;
        public string WaveActionLabel => Mode == BattleUiChromeMode.Ready
            ? RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleStartWave).Text
            : Mode == BattleUiChromeMode.BetweenWaves
                ? RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleStartNextWave).Text
            : string.Empty;
        public RuntimeUiInteractionState PhaseStatusInteractionState =>
            PhaseMode == BattleUiPhaseMode.Ready
                || PhaseMode == BattleUiPhaseMode.BetweenWaves
                ? RuntimeUiInteractionState.Warning
            : PhaseMode == BattleUiPhaseMode.Victory ? RuntimeUiInteractionState.Success
            : PhaseMode == BattleUiPhaseMode.Defeat ? RuntimeUiInteractionState.Error
            : RuntimeUiInteractionState.Normal;

        public static BattleUiPresentationState Create(GamePhase phase, bool paused)
        {
            var phaseMode = phase == GamePhase.Playing ? BattleUiPhaseMode.ActiveWave
                : phase == GamePhase.BetweenWaves ? BattleUiPhaseMode.BetweenWaves
                : phase == GamePhase.Victory ? BattleUiPhaseMode.Victory
                : phase == GamePhase.Defeat ? BattleUiPhaseMode.Defeat
                : BattleUiPhaseMode.Ready;
            if (phase == GamePhase.Victory)
                return new BattleUiPresentationState(
                    BattleUiChromeMode.Victory, phaseMode, paused);
            if (phase == GamePhase.Defeat)
                return new BattleUiPresentationState(
                    BattleUiChromeMode.Defeat, phaseMode, paused);
            if (paused)
                return new BattleUiPresentationState(
                    BattleUiChromeMode.Paused, phaseMode, true);
            if (phase == GamePhase.Playing)
                return new BattleUiPresentationState(
                    BattleUiChromeMode.ActiveWave, phaseMode, false);
            if (phase == GamePhase.BetweenWaves)
                return new BattleUiPresentationState(
                    BattleUiChromeMode.BetweenWaves, phaseMode, false);
            return new BattleUiPresentationState(BattleUiChromeMode.Ready, phaseMode, false);
        }

        public string PhaseStatusText(int waveIndex, int zombieCount, float betweenTimer)
        {
            switch (PhaseMode)
            {
                case BattleUiPhaseMode.ActiveWave:
                    return RuntimeUiCopyCatalog.FormatActiveWaveStatus(
                        waveIndex, zombieCount);
                case BattleUiPhaseMode.BetweenWaves:
                    return RuntimeUiCopyCatalog.FormatBetweenWaveStatus(
                        UnityEngine.Mathf.CeilToInt(betweenTimer));
                case BattleUiPhaseMode.Victory:
                    return RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.BattleVictoryStatus).Text;
                case BattleUiPhaseMode.Defeat:
                    return RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.BattleDefeatStatus).Text;
                default:
                    return RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleReady).Text;
            }
        }

        public static RuntimeUiInteractionState ResolveActionState(
            bool selected, bool pointerInside, bool pointerPressed)
        {
            if (pointerPressed) return RuntimeUiInteractionState.Pressed;
            if (selected) return RuntimeUiInteractionState.Selected;
            return pointerInside
                ? RuntimeUiInteractionState.HoveredOrFocused
                : RuntimeUiInteractionState.Normal;
        }

        public static RuntimeUiActionSpec ResolveActionSpec(
            BattleUiActionSemantic semantic)
        {
            switch (semantic)
            {
                case BattleUiActionSemantic.StartWave:
                    return new RuntimeUiActionSpec(RuntimeUiActionKind.Primary,
                        RuntimeUiActionContentForm.IconLabel,
                        RuntimeUiActionBehavior.Instantaneous);
                case BattleUiActionSemantic.NurseryRefresh:
                    return new RuntimeUiActionSpec(RuntimeUiActionKind.Secondary,
                        RuntimeUiActionContentForm.IconLabel,
                        RuntimeUiActionBehavior.Instantaneous);
                case BattleUiActionSemantic.PauseContinue:
                    return new RuntimeUiActionSpec(RuntimeUiActionKind.Quiet,
                        RuntimeUiActionContentForm.IconOnly,
                        RuntimeUiActionBehavior.PersistentMode);
                case BattleUiActionSemantic.Speed:
                    return new RuntimeUiActionSpec(RuntimeUiActionKind.Quiet,
                        RuntimeUiActionContentForm.CompactMultiplier,
                        RuntimeUiActionBehavior.PersistentMode);
                case BattleUiActionSemantic.Close:
                    return new RuntimeUiActionSpec(RuntimeUiActionKind.Quiet,
                        RuntimeUiActionContentForm.IconOnly,
                        RuntimeUiActionBehavior.Instantaneous);
                default:
                    throw new ArgumentOutOfRangeException(nameof(semantic), semantic, null);
            }
        }

        public static RuntimeUiInteractionState ResolveSlotState(
            bool enabled, bool selected, bool pointerInside, bool pointerPressed)
        {
            if (!enabled) return RuntimeUiInteractionState.Disabled;
            return ResolveActionState(selected, pointerInside, pointerPressed);
        }

        public static BattleUiDropCue ResolveDropCue(
            bool legal, bool merge, bool swap)
        {
            if (!legal) return BattleUiDropCue.Illegal;
            if (merge) return BattleUiDropCue.Merge;
            if (swap) return BattleUiDropCue.Swap;
            return BattleUiDropCue.Legal;
        }

        public static RuntimeUiInteractionState DropInteractionState(BattleUiDropCue cue)
        {
            switch (cue)
            {
                case BattleUiDropCue.Legal: return RuntimeUiInteractionState.Success;
                case BattleUiDropCue.Illegal: return RuntimeUiInteractionState.Error;
                case BattleUiDropCue.Merge: return RuntimeUiInteractionState.Warning;
                case BattleUiDropCue.Swap: return RuntimeUiInteractionState.Selected;
                case BattleUiDropCue.None: return RuntimeUiInteractionState.Normal;
                default: throw new ArgumentOutOfRangeException(nameof(cue), cue, null);
            }
        }

        public static bool SnapsPlantDragFeedback(BattleUiDropCue cue)
        {
            return cue == BattleUiDropCue.Legal
                || cue == BattleUiDropCue.Merge
                || cue == BattleUiDropCue.Swap;
        }

        public static RuntimeUiIndicatorKind DropIndicatorKind(BattleUiDropCue cue)
        {
            switch (cue)
            {
                case BattleUiDropCue.Legal: return RuntimeUiIndicatorKind.DragLegal;
                case BattleUiDropCue.Illegal: return RuntimeUiIndicatorKind.DragIllegal;
                case BattleUiDropCue.Merge: return RuntimeUiIndicatorKind.Merge;
                case BattleUiDropCue.Swap: return RuntimeUiIndicatorKind.Swap;
                default: throw new ArgumentOutOfRangeException(nameof(cue), cue,
                    "A visible drop cue is required before resolving its indicator.");
            }
        }

        public static RuntimeUiInteractionState ResolveTransientStatusState(
            RuntimeUiInteractionState statusState, BattleUiDropCue dropCue)
        {
            if (dropCue != BattleUiDropCue.None)
                return DropInteractionState(dropCue);
            return statusState;
        }

        public static string FormatTransientStatus(bool success, string message)
        {
            return (success ? "✓ " : "! ") + (message ?? string.Empty);
        }

        public BattleUiModalContent ModalContent(int waveIndex, int maxWaves)
        {
            switch (Mode)
            {
                case BattleUiChromeMode.Paused:
                    return new BattleUiModalContent(
                        RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattlePausedTitle).Text,
                        string.Empty,
                        new RuntimeUiStatusTextLines(RuntimeUiCopyCatalog.Get(
                            RuntimeUiCopyId.BattlePausedMessage).Text),
                        RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleContinue).Text,
                        RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleRestart).Text,
                        RuntimeUiInteractionState.Warning, false,
                        RuntimeUiActionKind.Primary, RuntimeUiArtSlot.IconControlContinue,
                        RuntimeUiActionKind.Danger, RuntimeUiArtSlot.IconControlRetry);
                case BattleUiChromeMode.Victory:
                    return new BattleUiModalContent(
                        RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleVictoryTitle).Text,
                        RuntimeUiCopyCatalog.Get(
                            RuntimeUiCopyId.BattleVictoryOutcome).Text,
                        RuntimeUiCopyCatalog.FormatVictoryMessageLines(maxWaves),
                        RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleRestart).Text,
                        null, RuntimeUiInteractionState.Success, true,
                        RuntimeUiActionKind.Primary, RuntimeUiArtSlot.IconControlRetry,
                        RuntimeUiActionKind.Danger, RuntimeUiArtSlot.IconControlRetry);
                case BattleUiChromeMode.Defeat:
                    return new BattleUiModalContent(
                        RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleDefeatTitle).Text,
                        RuntimeUiCopyCatalog.Get(
                            RuntimeUiCopyId.BattleDefeatOutcome).Text,
                        new RuntimeUiStatusTextLines(
                            RuntimeUiCopyCatalog.FormatDefeatMessage(waveIndex)),
                        RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleRestart).Text,
                        null, RuntimeUiInteractionState.Error, true,
                        RuntimeUiActionKind.Primary, RuntimeUiArtSlot.IconControlRetry,
                        RuntimeUiActionKind.Danger, RuntimeUiArtSlot.IconControlRetry);
                default:
                    return default;
            }
        }
    }
}
