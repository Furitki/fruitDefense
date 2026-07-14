using System;
using System.Collections;
using FruitDefense.Platform;
using UnityEngine;

namespace FruitDefense.App
{
    public static class AppFrameworkValidation
    {
        public static void SmokeValidate()
        {
            if (!Validate(out var reason))
                throw new InvalidOperationException("App framework validation failed: " + reason);
            Debug.Log("FRUIT_DEFENSE_APP_FRAMEWORK_OK");
        }

        public static bool Validate(out string reason)
        {
            try
            {
                ValidateLaunchContext();
                ValidateAdapters();
                ValidateNavigation();
                ValidateDuplicatePolicy();
                reason = "ok";
                return true;
            }
            catch (Exception exception)
            {
                reason = exception.Message;
                return false;
            }
        }

        private static void ValidateLaunchContext()
        {
            var context = PlatformLaunchContext.FromUrl(
                PlatformId.Web,
                "https://fruit.example/game?route=battle&text=hello+world&asset=a%2Fb&flag#ignored");
            Assert(context.Platform == PlatformId.Web, "launch context keeps platform identity");
            Assert(context.Query.Count == 4, "launch context parses query entries before fragment");
            Assert(context.TryGetQuery("route", out var route) && route == "battle",
                "launch context parses route");
            Assert(context.TryGetQuery("text", out var text) && text == "hello world",
                "launch context decodes form spaces");
            Assert(context.TryGetQuery("asset", out var asset) && asset == "a/b",
                "launch context decodes escaped values");
            Assert(context.TryGetQuery("flag", out var flag) && flag == string.Empty,
                "launch context supports flag parameters");
        }

        private static void ValidateAdapters()
        {
            using (var editor = PlatformAdapterFactory.Create(PlatformId.Editor))
            {
                var result = Initialize(editor);
                Assert(editor is EditorPlatformAdapter && editor.Id == PlatformId.Editor && result.Success,
                    "editor adapter initializes through the common contract");
            }

            using (var web = PlatformAdapterFactory.Create(PlatformId.Web, "https://fruit.example/?acceptance=1"))
            {
                var visibilityEvents = 0;
                web.VisibilityChanged += _ => visibilityEvents++;
                var result = Initialize(web);
                Assert(web is WebPlatformAdapter && web.Id == PlatformId.Web && result.Success,
                    "web adapter initializes through the common contract");
                Assert(web.LaunchContext.TryGetQuery("acceptance", out var acceptance) && acceptance == "1",
                    "web adapter exposes launch query");
                Assert(PlatformAdapterFactory.TryForwardVisibility(web, PlatformVisibility.Background),
                    "web adapter accepts host visibility");
                PlatformAdapterFactory.TryForwardVisibility(web, PlatformVisibility.Background);
                PlatformAdapterFactory.TryForwardVisibility(web, PlatformVisibility.Foreground);
                Assert(visibilityEvents == 2, "visibility events are deduplicated");
            }

            ValidateUnavailable(PlatformId.DouyinMiniGame);
            ValidateUnavailable(PlatformId.WeChatMiniGame);
        }

        private static void ValidateUnavailable(PlatformId platform)
        {
            using (var adapter = PlatformAdapterFactory.Create(platform))
            {
                var result = Initialize(adapter);
                Assert(adapter is UnavailablePlatformAdapter, platform + " uses an explicit unavailable slot");
                Assert(!(adapter is WebPlatformAdapter), platform + " never falls back to Web");
                Assert(adapter.Id == platform, platform + " keeps requested identity");
                Assert(!result.Success && result.ErrorCode == UnavailablePlatformAdapter.AdapterNotInstalled,
                    platform + " reports adapter-not-installed");
            }
        }

        private static PlatformInitResult Initialize(IPlatformAdapter adapter)
        {
            var completionCount = 0;
            var result = PlatformInitResult.Failed("validation-callback-missing");
            var routine = adapter.Initialize(value =>
            {
                completionCount++;
                result = value;
            });
            Run(routine);
            Assert(completionCount == 1, adapter.Id + " completes initialization exactly once");
            return result;
        }

        private static void Run(IEnumerator routine)
        {
            Assert(routine != null, "adapter returns an initialization enumerator");
            while (routine.MoveNext())
            {
                if (routine.Current is IEnumerator nested) Run(nested);
            }
        }

        private static void ValidateNavigation()
        {
            var navigator = new AppNavigator();
            var routeEvents = 0;
            var stateEvents = 0;
            navigator.RouteChanged += _ => routeEvents++;
            navigator.TransitionStateChanged += _ => stateEvents++;

            Assert(navigator.CurrentRoute == AppRoute.Lobby
                && navigator.TransitionState == AppTransitionState.Idle
                && !navigator.HasPendingRoute
                && navigator.LastError == string.Empty,
                "navigator starts idle at Lobby");

            Assert(!navigator.TryBeginTransition(AppRoute.Settlement, out var error)
                && error == AppNavigator.RouteNotAllowed
                && navigator.CurrentRoute == AppRoute.Lobby,
                "navigator rejects an invalid edge");

            Assert(navigator.TryBeginTransition(AppRoute.Battle, out error)
                && navigator.TransitionState == AppTransitionState.Loading
                && navigator.HasPendingRoute
                && navigator.PendingRoute == AppRoute.Battle
                && navigator.CurrentRoute == AppRoute.Lobby,
                "navigator begins valid transition without changing current route");
            Assert(!navigator.TryBeginTransition(AppRoute.Battle, out error)
                && error == AppNavigator.TransitionInProgress
                && navigator.PendingRoute == AppRoute.Battle,
                "navigator guards duplicate transitions");
            Assert(navigator.TryCompleteTransition(out error)
                && navigator.CurrentRoute == AppRoute.Battle
                && navigator.TransitionState == AppTransitionState.Idle
                && routeEvents == 1,
                "navigator commits a completed transition once");

            Assert(navigator.TryBeginTransition(AppRoute.Settlement, out error)
                && navigator.TryFailTransition("scene-load-failed", out error)
                && navigator.CurrentRoute == AppRoute.Battle
                && navigator.TransitionState == AppTransitionState.Failed
                && navigator.LastError == "scene-load-failed",
                "navigator retains current route on failure");
            Assert(navigator.TryBeginTransition(AppRoute.Settlement, out error)
                && navigator.LastError == string.Empty
                && navigator.TryCompleteTransition(out error)
                && navigator.CurrentRoute == AppRoute.Settlement,
                "navigator retries after failure");
            Assert(navigator.TryBeginTransition(AppRoute.Lobby, out error)
                && navigator.TryCompleteTransition(out error)
                && navigator.CurrentRoute == AppRoute.Lobby
                && routeEvents == 3
                && stateEvents == 8,
                "navigator completes the full route cycle");

            Assert(navigator.TryBeginTransition(AppRoute.Battle, out error)
                && navigator.TryRecoverToLobby("missing-battle-scene", out error)
                && navigator.CurrentRoute == AppRoute.Lobby
                && navigator.TransitionState == AppTransitionState.Idle
                && !navigator.HasPendingRoute
                && navigator.LastError == "missing-battle-scene",
                "navigator can recover any failed or loading route to a usable Lobby state");

            Assert(navigator.TryBeginTransition(AppRoute.Battle, out error)
                && navigator.TryRestoreCurrentRoute("lobby-load-failed", out error)
                && navigator.CurrentRoute == AppRoute.Lobby
                && navigator.TransitionState == AppTransitionState.Idle
                && !navigator.HasPendingRoute
                && navigator.LastError == "lobby-load-failed",
                "navigator can restore the current route after a destination load failure");
        }

        private static void ValidateDuplicatePolicy()
        {
            var firstObject = new GameObject("AppBootstrapValidationFirst");
            var secondObject = new GameObject("AppBootstrapValidationSecond");
            firstObject.SetActive(false);
            secondObject.SetActive(false);
            try
            {
                var first = firstObject.AddComponent<AppBootstrap>();
                var second = secondObject.AddComponent<AppBootstrap>();
                Assert(!AppBootstrap.ShouldRejectDuplicate(null, first),
                    "first bootstrap can claim composition-root ownership");
                Assert(!AppBootstrap.ShouldRejectDuplicate(first, first),
                    "active bootstrap does not reject itself");
                Assert(AppBootstrap.ShouldRejectDuplicate(first, second),
                    "active bootstrap rejects a second instance");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(secondObject);
                UnityEngine.Object.DestroyImmediate(firstObject);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
