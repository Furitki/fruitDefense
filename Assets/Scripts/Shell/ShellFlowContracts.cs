using FruitDefense.App;

namespace FruitDefense.Shell
{
    public readonly struct SettlementViewData
    {
        public SettlementViewData(bool victory, int reachedWave, int remainingLives)
        {
            Victory = victory;
            ReachedWave = reachedWave;
            RemainingLives = remainingLives;
        }

        public bool Victory { get; }
        public int ReachedWave { get; }
        public int RemainingLives { get; }
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
