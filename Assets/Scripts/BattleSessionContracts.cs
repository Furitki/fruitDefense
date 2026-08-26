using System;
using FruitDefense.App;
using FruitDefense.Content;
using FruitDefense.UI;
using FruitDefense.Core;
using FruitDefense.Platform;

namespace FruitDefense.Battle
{
    public enum BattleSessionMode
    {
        Standard,
        GmStress,
    }

    public enum BattleOutcome
    {
        Victory,
        Defeat,
    }

    public sealed class BattleLaunchRequest
    {
        public BattleLaunchRequest(string sessionId, string levelId, int seed,
            string contentVersion, BattleSessionMode mode)
        {
            SessionId = sessionId;
            LevelId = levelId;
            Seed = seed;
            ContentVersion = contentVersion;
            Mode = mode;
        }

        public string SessionId { get; }
        public string LevelId { get; }
        public int Seed { get; }
        public string ContentVersion { get; }
        public BattleSessionMode Mode { get; }

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
            if (!Enum.IsDefined(typeof(BattleSessionMode), Mode))
            {
                errorCode = BattleSessionInitializationResult.InvalidSessionMode;
                return false;
            }

            errorCode = string.Empty;
            return true;
        }
    }

    public sealed class BattleResult
    {
        public const string MissingRequest = "battle-result-request-missing";
        public const string SessionMismatch = "battle-result-session-mismatch";
        public const string LevelMismatch = "battle-result-level-mismatch";
        public const string SeedMismatch = "battle-result-seed-mismatch";
        public const string InvalidOutcome = "battle-result-outcome-invalid";
        public const string InvalidReachedWave = "battle-result-wave-invalid";
        public const string InvalidRemainingLives = "battle-result-lives-invalid";

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

        public bool TryValidate(BattleLaunchRequest request, out string errorCode)
        {
            if (request == null)
            {
                errorCode = MissingRequest;
                return false;
            }
            if (!string.Equals(SessionId, request.SessionId, StringComparison.Ordinal))
            {
                errorCode = SessionMismatch;
                return false;
            }
            if (!string.Equals(LevelId, request.LevelId, StringComparison.Ordinal))
            {
                errorCode = LevelMismatch;
                return false;
            }
            if (Seed != request.Seed)
            {
                errorCode = SeedMismatch;
                return false;
            }
            if (!Enum.IsDefined(typeof(BattleOutcome), Outcome))
            {
                errorCode = InvalidOutcome;
                return false;
            }
            if (ReachedWave < 0)
            {
                errorCode = InvalidReachedWave;
                return false;
            }
            if (RemainingLives < 0)
            {
                errorCode = InvalidRemainingLives;
                return false;
            }
            errorCode = string.Empty;
            return true;
        }
    }

    public readonly struct BattleSessionInitializationResult
    {
        public const string AlreadyInitialized = "battle-session-already-initialized";
        public const string InvalidRequest = "battle-launch-request-required";
        public const string InvalidSessionId = "battle-session-id-required";
        public const string InvalidLevelId = "battle-level-id-required";
        public const string InvalidContentVersion = "battle-content-version-required";
        public const string InvalidSessionMode = "battle-session-mode-invalid";
        public const string NavigatorRequired = "battle-navigator-required";
        public const string ResultSinkRequired = "battle-result-sink-required";
        public const string RuntimeUiThemeRequired = "battle-runtime-ui-theme-required";
        public const string RuntimeUiThemeInvalid = "battle-runtime-ui-theme-invalid";
        public const string LevelCatalogRequired = "battle-level-catalog-required";
        public const string LevelResolutionFailed = "battle-level-resolution-failed";
        public const string ContentVersionMismatch = "battle-content-version-mismatch";
        public const string SimulationConstructionFailed = "battle-simulation-construction-failed";
        public const string SessionModeMismatch = "battle-session-mode-mismatch";

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

    public readonly struct BattleSessionStatus
    {
        public BattleSessionStatus(bool isInitialized, GamePhase phase,
            int waveIndex, int lives, bool isPaused, bool hasSubmittedResult)
        {
            IsInitialized = isInitialized;
            Phase = phase;
            WaveIndex = waveIndex;
            Lives = lives;
            IsPaused = isPaused;
            HasSubmittedResult = hasSubmittedResult;
        }

        public bool IsInitialized { get; }
        public GamePhase Phase { get; }
        public int WaveIndex { get; }
        public int Lives { get; }
        public bool IsPaused { get; }
        public bool HasSubmittedResult { get; }
        public bool IsTerminal
        {
            get
            {
                return IsInitialized
                    && (Phase == GamePhase.Victory || Phase == GamePhase.Defeat);
            }
        }

        public static BattleSessionStatus Uninitialized
        {
            get { return default; }
        }
    }

    public interface IBattleResultSink
    {
        bool TrySubmitResult(BattleResult result, out string errorCode);
    }

    public interface IBattleSessionHost
    {
        BattleSessionStatus Status { get; }

        BattleSessionInitializationResult Initialize(
            BattleLaunchRequest request,
            IAppNavigator navigator,
            IBattleResultSink resultSink,
            RuntimeUiTheme runtimeUiTheme,
            CompiledLevelCatalog levelCatalog);

        void HandlePlatformVisibility(PlatformVisibility visibility);
        bool RestartCurrentSession(out string errorCode);
        BattleSnapshotExportResult ExportCurrentSessionSnapshot();
        BattleSnapshotRestoreResult RestoreCurrentSessionSnapshot(
            BattleSnapshot snapshot, CompiledLevelCatalog levelCatalog);
        bool TrySubmitTerminalResult();
        void DisposeSession();
    }
}
