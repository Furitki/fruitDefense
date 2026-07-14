using FruitDefense.App;
using FruitDefense.Core;
using FruitDefense.Platform;

namespace FruitDefense.Battle
{
    public enum BattleOutcome
    {
        Victory,
        Defeat,
    }

    public sealed class BattleLaunchRequest
    {
        public BattleLaunchRequest(string sessionId, string levelId, int seed, string contentVersion)
        {
            SessionId = sessionId;
            LevelId = levelId;
            Seed = seed;
            ContentVersion = contentVersion;
        }

        public string SessionId { get; }
        public string LevelId { get; }
        public int Seed { get; }
        public string ContentVersion { get; }

        public bool TryValidate(out string errorCode)
        {
            if (string.IsNullOrWhiteSpace(SessionId))
            {
                errorCode = BattleSessionInitializationResult.InvalidSessionId;
                return false;
            }

            if (string.IsNullOrWhiteSpace(LevelId))
            {
                errorCode = BattleSessionInitializationResult.InvalidLevelId;
                return false;
            }

            if (string.IsNullOrWhiteSpace(ContentVersion))
            {
                errorCode = BattleSessionInitializationResult.InvalidContentVersion;
                return false;
            }

            errorCode = string.Empty;
            return true;
        }
    }

    public sealed class BattleResult
    {
        public BattleResult(
            string sessionId,
            string levelId,
            int seed,
            BattleOutcome outcome,
            int reachedWave,
            int remainingLives)
        {
            SessionId = sessionId;
            LevelId = levelId;
            Seed = seed;
            Outcome = outcome;
            ReachedWave = reachedWave;
            RemainingLives = remainingLives;
        }

        public string SessionId { get; }
        public string LevelId { get; }
        public int Seed { get; }
        public BattleOutcome Outcome { get; }
        public int ReachedWave { get; }
        public int RemainingLives { get; }
    }

    public readonly struct BattleSessionInitializationResult
    {
        public const string AlreadyInitialized = "battle-session-already-initialized";
        public const string InvalidRequest = "battle-launch-request-required";
        public const string InvalidSessionId = "battle-session-id-required";
        public const string InvalidLevelId = "battle-level-id-required";
        public const string InvalidContentVersion = "battle-content-version-required";
        public const string NavigatorRequired = "battle-navigator-required";
        public const string ResultSinkRequired = "battle-result-sink-required";
        public const string SimulationConstructionFailed = "battle-simulation-construction-failed";

        private BattleSessionInitializationResult(bool success, string errorCode)
        {
            Success = success;
            ErrorCode = success ? string.Empty : errorCode;
        }

        public bool Success { get; }
        public string ErrorCode { get; }

        public static BattleSessionInitializationResult Succeeded()
        {
            return new BattleSessionInitializationResult(true, string.Empty);
        }

        public static BattleSessionInitializationResult Failed(string errorCode)
        {
            return new BattleSessionInitializationResult(
                false,
                string.IsNullOrWhiteSpace(errorCode) ? InvalidRequest : errorCode);
        }
    }

    public interface IBattleResultSink
    {
        bool TrySubmitResult(BattleResult result, out string errorCode);
    }

    public interface IBattleSessionHost
    {
        bool IsInitialized { get; }
        bool HasSubmittedResult { get; }
        BattleLaunchRequest CurrentRequest { get; }
        GameSimulation Simulation { get; }

        BattleSessionInitializationResult Initialize(
            BattleLaunchRequest request,
            IAppNavigator navigator,
            IBattleResultSink resultSink,
            BattlefieldMapDefinition map = null);

        void HandlePlatformVisibility(PlatformVisibility visibility);
        bool RestartCurrentSession(out string errorCode);
        bool TrySubmitTerminalResult();
        void DisposeSession();
    }
}
