#if FRUIT_DEFENSE_ACCEPTANCE
namespace FruitDefense
{
    public enum AcceptanceTerminalFixture
    {
        Victory = 1,
        Defeat = 2,
    }

    public readonly struct AcceptanceCommandResult
    {
        public const string SessionUnavailable = "acceptance-session-unavailable";
        public const string LaunchRequired = "acceptance-launch-required";
        public const string NamedStateRequired = "acceptance-named-state-required";
        public const string NamedStateUnknown = "acceptance-named-state-unknown";
        public const string TerminalFixtureUnknown = "acceptance-terminal-fixture-unknown";

        private AcceptanceCommandResult(bool succeeded, string errorCode)
        {
            Succeeded = succeeded;
            ErrorCode = errorCode ?? string.Empty;
        }

        public bool Succeeded { get; }
        public string ErrorCode { get; }

        public static AcceptanceCommandResult Success()
        {
            return new AcceptanceCommandResult(true, string.Empty);
        }

        public static AcceptanceCommandResult Failure(string errorCode)
        {
            return new AcceptanceCommandResult(false, errorCode);
        }
    }

    public interface IAcceptanceBattlePort
    {
        string CombatFeedbackAcceptanceTelemetryJson { get; }
        AcceptanceCommandResult TryConfigureNamedState(string stateName);
        AcceptanceCommandResult TryConfigureTerminalFixture(
            AcceptanceTerminalFixture fixture);
    }
}
#endif
