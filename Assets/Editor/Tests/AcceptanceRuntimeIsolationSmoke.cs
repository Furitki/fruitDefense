using System;
using System.IO;
using System.Reflection;
using FruitDefense.App;
using FruitDefense.Shell;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class AcceptanceRuntimeIsolationSmoke
    {
        public static void Run()
        {
            ValidateAcceptanceQuerySemantics();
#if FRUIT_DEFENSE_ACCEPTANCE
            ValidateAcceptanceSurfacePresent();
#else
            ValidateReleaseSurfaceAbsent();
#endif
            ValidateAcceptanceSourcesRemainDedicated();
            Debug.Log("FRUIT_DEFENSE_ACCEPTANCE_RUNTIME_ISOLATION_OK");
        }

        private static void ValidateAcceptanceQuerySemantics()
        {
            Assert(AcceptanceLaunchQuery.IsEnabled(
                    "https://fruit.example/?acceptance=1&route=battle"),
                "the exact value 1 activates acceptance");
            Assert(!AcceptanceLaunchQuery.IsEnabled(
                    "https://fruit.example/?acceptance=0&route=battle"),
                "the value 0 does not activate acceptance");
            Assert(!AcceptanceLaunchQuery.IsEnabled(
                    "https://fruit.example/?acceptance=&route=battle"),
                "an empty value does not activate acceptance");
            Assert(!AcceptanceLaunchQuery.IsEnabled(
                    "https://fruit.example/?acceptance=false&route=battle"),
                "the value false does not activate acceptance");
            Assert(AcceptanceLaunchQuery.IsEnabled(
                    "https://fruit.example/?%61cceptance=%31&route=battle"),
                "percent-encoded key and exact value decode to acceptance=1");
            Assert(!AcceptanceLaunchQuery.IsEnabled(
                    "https://fruit.example/?acceptance=%30&route=battle"),
                "an encoded value 0 remains inactive");
            Assert(!AcceptanceLaunchQuery.IsEnabled(
                    "https://fruit.example/?acceptance=0&acceptance=1"),
                "duplicate acceptance parameters use the first inactive value");
            Assert(AcceptanceLaunchQuery.IsEnabled(
                    "https://fruit.example/?acceptance=1&acceptance=0"),
                "duplicate acceptance parameters use the first active value");
            Assert(!AcceptanceLaunchQuery.IsEnabled(
                    "https://fruit.example/?Acceptance=1"),
                "acceptance query key matching follows URLSearchParams case semantics");
        }

#if FRUIT_DEFENSE_ACCEPTANCE
        private static void ValidateAcceptanceSurfacePresent()
        {
            Assert(typeof(IAcceptanceBattlePort).IsInterface,
                "acceptance build exposes its dedicated battle port");
            Assert(typeof(IAcceptanceBattlePort).GetMethod(
                    nameof(IAcceptanceBattlePort.TryConfigureNamedState)) != null
                && typeof(IAcceptanceBattlePort).GetMethod(
                    nameof(IAcceptanceBattlePort.TryConfigureTerminalFixture)) != null,
                "acceptance build exposes finite named-state and terminal commands");
            Assert(typeof(AppFlowCoordinator).GetMethod(
                    "ConfigureAcceptanceFlow",
                    BindingFlags.Instance | BindingFlags.Public) != null,
                "acceptance build retains the ConfigureAcceptanceFlow message entry");
            Assert(typeof(SettlementPresenter).GetMethod(
                    "FruitDefensePublishSettlementOutcomeReveal",
                    BindingFlags.Static | BindingFlags.NonPublic) != null,
                "acceptance build retains the read-only settlement reveal publisher");

            var systemSafeArea = new Rect(0f, 0f, 402f, 874f);
            var inset = AcceptanceSafeAreaDecorator.Resolve(
                systemSafeArea,
                402f,
                874f,
                "https://fruit.example/?acceptance=1&safeTop=44&safeBottom=34");
            Assert(inset == new Rect(0f, 34f, 402f, 796f),
                "acceptance decorator retains the existing synthetic inset contract");
        }
#else
        private static void ValidateReleaseSurfaceAbsent()
        {
            var runtimeAssembly = typeof(FruitDefenseGame).Assembly;
            Assert(runtimeAssembly.GetType("FruitDefense.IAcceptanceBattlePort") == null,
                "default runtime omits the acceptance port type");
            Assert(runtimeAssembly.GetType("FruitDefense.AcceptanceCommandResult") == null,
                "default runtime omits acceptance command results");
            Assert(runtimeAssembly.GetType("FruitDefense.AcceptanceSafeAreaDecorator") == null,
                "default runtime omits the synthetic safe-area decorator");
            Assert(typeof(FruitDefenseGame).GetMethod(
                    "ConfigureAcceptanceState",
                    BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic) == null,
                "default runtime omits ConfigureAcceptanceState");
            Assert(typeof(FruitDefenseGame).GetProperty(
                    "CombatFeedbackAcceptanceTelemetryJson",
                    BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic) == null,
                "default runtime omits acceptance telemetry access");
            Assert(typeof(AppFlowCoordinator).GetMethod(
                    "ConfigureAcceptanceFlow",
                    BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic) == null,
                "default runtime omits ConfigureAcceptanceFlow");
            Assert(typeof(SettlementPresenter).GetMethod(
                    "FruitDefensePublishSettlementOutcomeReveal",
                    BindingFlags.Static | BindingFlags.NonPublic) == null,
                "default runtime omits the settlement reveal publisher");
        }
#endif

        private static void ValidateAcceptanceSourcesRemainDedicated()
        {
            var gameSource = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            var flowSource = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/App/AppFlowCoordinator.cs"));
            var safeAreaSource = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/RuntimeSafeAreaResolver.cs"));
            var bridgeSource = File.ReadAllText(Path.GetFullPath(
                "Assets/Plugins/WebGL/FruitDefenseAcceptance.jslib"));

            Require(gameSource, "#if FRUIT_DEFENSE_ACCEPTANCE");
            Require(gameSource, "ConfigureAcceptanceState");
            Require(gameSource, "FruitDefensePublishCombatFeedbackTelemetry");
            Require(flowSource, "#if FRUIT_DEFENSE_ACCEPTANCE");
            Require(flowSource, "ConfigureAcceptanceFlow");
            Require(flowSource, "FruitDefenseAcceptanceReady");
            Require(flowSource, "as IAcceptanceBattlePort");
            Require(flowSource, "TryConfigureTerminalFixture");
            Reject(flowSource, "_activeBattleHost.Simulation");
            Require(safeAreaSource, "AcceptanceSafeAreaDecorator");
            Require(safeAreaSource, "return Screen.safeArea;");
            Require(bridgeSource, "FruitDefenseAcceptanceReady");
            Require(bridgeSource, "FruitDefensePublishCombatFeedbackTelemetry");
            Require(bridgeSource, "FruitDefensePublishSettlementOutcomeReveal");
            Require(bridgeSource, "fruitDefenseSettlementOutcomeRevealHistory");
            Require(bridgeSource, "window.fruitDefenseAppRoute !== 2");
            Require(bridgeSource, "identity.route !== 2");
            Require(bridgeSource, "identity.routeName !== 'settlement'");
            Require(bridgeSource, "sessionId: identity.sessionId");

            Assert(WebBuildProfile.Acceptance.CreateExtraScriptingDefines().Length == 1
                && string.Equals(
                    WebBuildProfile.Acceptance.CreateExtraScriptingDefines()[0],
                    WebBuildProfile.AcceptanceScriptingDefine,
                    StringComparison.Ordinal),
                "dedicated build profile is the only compiler input that enables acceptance");
        }

        private static void Require(string source, string token)
        {
            Assert(source.Contains(token, StringComparison.Ordinal),
                "acceptance source contract token is present: " + token);
        }

        private static void Reject(string source, string token)
        {
            Assert(!source.Contains(token, StringComparison.Ordinal),
                "production orchestration source omits mutable host access: " + token);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Acceptance runtime isolation smoke failed: " + message);
        }
    }
}
