using System.Collections.Generic;
using FruitDefense.App;
using FruitDefense.Content;

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

    // Optional extension consumed by the multi-level Lobby. Keeping selection outside the
    // base flow contract preserves compatibility with existing shell fakes and presenters.
    public interface ILevelSelectionFlowContext
    {
        IReadOnlyList<LevelDefinition> PlayableLevels { get; }
        string SelectedLevelId { get; }
        bool TrySelectLevel(string levelId, out ShellFlowError error);
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
