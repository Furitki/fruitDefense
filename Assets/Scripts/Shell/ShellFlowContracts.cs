using System;
using System.Collections;
using System.Collections.Generic;
using FruitDefense.App;
using FruitDefense.App.Services;
using FruitDefense.Content;
using FruitDefense.Core;

namespace FruitDefense.Shell
{
    public readonly struct SettlementViewData
    {
        public SettlementViewData(bool victory, int reachedWave, int remainingLives)
            : this(string.Empty, victory, reachedWave, remainingLives)
        {
        }

        public SettlementViewData(string levelId, bool victory, int reachedWave, int remainingLives)
        {
            LevelId = levelId ?? string.Empty;
            Victory = victory;
            ReachedWave = reachedWave;
            RemainingLives = remainingLives;
        }

        public string LevelId { get; }
        public bool Victory { get; }
        public int ReachedWave { get; }
        public int RemainingLives { get; }
    }

    // Required by the multi-level Lobby while Settlement consumes the base flow contract.
    public interface ILevelSelectionFlowContext
    {
        IReadOnlyList<LevelDefinition> PlayableLevels { get; }
        string SelectedLevelId { get; }
        bool TrySelectLevel(string levelId, out ShellFlowError error);
    }

    /// <summary>
    /// Read-only Lobby projection boundary. Presenters may inspect compiled
    /// definitions and immutable player/growth projections, but never a mutable
    /// player profile aggregate.
    /// </summary>
    public interface IHubProgressionReadContext
    {
        CompiledOutgameContentCatalog OutgameContent { get; }
        PlayerProgressionProjection Progression { get; }
        BattleGrowthResolution CurrentGrowthPreview { get; }
        bool TryRefreshSelectedGrowthPreview(out BattleGrowthResolution preview);
    }

    /// <summary>
    /// Finite persistence-changing commands available to Lobby. Implementations
    /// serialize these operations and publish projection changes only after save.
    /// </summary>
    public interface IHubProgressionCommandContext
    {
        bool ProgressionCommandInProgress { get; }

        IEnumerator TryClaimActivity(string activityId,
            Action<PlayerProgressionCommandResult> completed);
        IEnumerator TryEquipGrowthEquipment(string growthEquipmentId,
            string slotId, Action<PlayerProgressionCommandResult> completed);
        IEnumerator TryUpgradeGrowthEquipment(string growthEquipmentId,
            Action<PlayerProgressionCommandResult> completed);
        IEnumerator TryUpgradeCultivation(string cultivationNodeId,
            Action<PlayerProgressionCommandResult> completed);
    }

    public interface IProfileRecoveryCommandContext
    {
        bool ProfileRecoveryRequired { get; }
        bool ProfileRecoveryInProgress { get; }
        bool TryResetUnsupportedProfile(out ShellFlowError error);
    }

    public readonly struct ShellFlowError
    {
        public ShellFlowError(string code, string detail = "")
        {
            Code = code ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Code { get; }
        public string Detail { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Code);

        public static ShellFlowError None => new ShellFlowError(string.Empty);
    }

    public interface IShellFlowContext
    {
        IAppNavigator Navigator { get; }
        string BundledContentVersion { get; }

        bool TryStartDefaultBattle(
            string levelId,
            string sessionId,
            int seed,
            string contentVersion,
            out ShellFlowError error);

        bool TryGetSettlementViewData(out SettlementViewData viewData, out ShellFlowError error);

        // Implementations clear the completed session and result before beginning the Lobby transition.
        bool TryReturnToLobby(out ShellFlowError error);

        // Implementations retain level/content identity while creating a fresh session ID and nonzero seed.
        bool TryRetryBattle(out ShellFlowError error);
        void ReportRecoverableError(ShellFlowError error);
    }
}
