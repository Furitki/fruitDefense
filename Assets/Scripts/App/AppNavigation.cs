using System;

namespace FruitDefense.App
{
    public enum AppRoute
    {
        Lobby,
        Battle,
        Settlement,
    }

    public enum AppTransitionState
    {
        Idle,
        Loading,
        Failed,
    }

    public interface IAppNavigator
    {
        AppRoute CurrentRoute { get; }
        AppRoute PendingRoute { get; }
        bool HasPendingRoute { get; }
        AppTransitionState TransitionState { get; }
        string LastError { get; }

        event Action<AppRoute> RouteChanged;
        event Action<AppTransitionState> TransitionStateChanged;

        bool TryBeginTransition(AppRoute destination, out string errorCode);
        bool TryCompleteTransition(out string errorCode);
        bool TryFailTransition(string transitionErrorCode, out string errorCode);
    }

    public interface IAppRecoveryNavigator
    {
        bool TryRecoverToLobby(string recoveryErrorCode, out string errorCode);
        bool TryRestoreCurrentRoute(string recoveryErrorCode, out string errorCode);
    }

    public sealed class AppNavigator : IAppNavigator, IAppRecoveryNavigator
    {
        public const string RouteNotAllowed = "route-not-allowed";
        public const string TransitionInProgress = "transition-in-progress";
        public const string NoTransitionInProgress = "no-transition-in-progress";
        public const string TransitionFailed = "transition-failed";

        public AppNavigator()
        {
            CurrentRoute = AppRoute.Lobby;
            PendingRoute = AppRoute.Lobby;
            TransitionState = AppTransitionState.Idle;
            LastError = string.Empty;
        }

        public AppRoute CurrentRoute { get; private set; }
        public AppRoute PendingRoute { get; private set; }
        public bool HasPendingRoute { get; private set; }
        public AppTransitionState TransitionState { get; private set; }
        public string LastError { get; private set; }

        public event Action<AppRoute> RouteChanged;
        public event Action<AppTransitionState> TransitionStateChanged;

        public bool TryBeginTransition(AppRoute destination, out string errorCode)
        {
            if (TransitionState == AppTransitionState.Loading)
            {
                errorCode = TransitionInProgress;
                return false;
            }

            if (!IsAllowed(CurrentRoute, destination))
            {
                errorCode = RouteNotAllowed;
                LastError = errorCode;
                return false;
            }

            PendingRoute = destination;
            HasPendingRoute = true;
            LastError = string.Empty;
            SetTransitionState(AppTransitionState.Loading);
            errorCode = string.Empty;
            return true;
        }

        public bool TryCompleteTransition(out string errorCode)
        {
            if (TransitionState != AppTransitionState.Loading || !HasPendingRoute)
            {
                errorCode = NoTransitionInProgress;
                return false;
            }

            var destination = PendingRoute;
            CurrentRoute = destination;
            PendingRoute = CurrentRoute;
            HasPendingRoute = false;
            LastError = string.Empty;
            SetTransitionState(AppTransitionState.Idle);
            RouteChanged?.Invoke(destination);
            errorCode = string.Empty;
            return true;
        }

        public bool TryFailTransition(string transitionErrorCode, out string errorCode)
        {
            if (TransitionState != AppTransitionState.Loading || !HasPendingRoute)
            {
                errorCode = NoTransitionInProgress;
                return false;
            }

            PendingRoute = CurrentRoute;
            HasPendingRoute = false;
            LastError = string.IsNullOrWhiteSpace(transitionErrorCode)
                ? TransitionFailed
                : transitionErrorCode;
            SetTransitionState(AppTransitionState.Failed);
            errorCode = string.Empty;
            return true;
        }

        public bool TryRecoverToLobby(string recoveryErrorCode, out string errorCode)
        {
            CurrentRoute = AppRoute.Lobby;
            PendingRoute = AppRoute.Lobby;
            HasPendingRoute = false;
            LastError = string.IsNullOrWhiteSpace(recoveryErrorCode)
                ? TransitionFailed
                : recoveryErrorCode;
            SetTransitionState(AppTransitionState.Idle);
            RouteChanged?.Invoke(AppRoute.Lobby);
            errorCode = string.Empty;
            return true;
        }

        public bool TryRestoreCurrentRoute(string recoveryErrorCode, out string errorCode)
        {
            PendingRoute = CurrentRoute;
            HasPendingRoute = false;
            LastError = string.IsNullOrWhiteSpace(recoveryErrorCode)
                ? TransitionFailed
                : recoveryErrorCode;
            SetTransitionState(AppTransitionState.Idle);
            errorCode = string.Empty;
            return true;
        }

        public static bool IsAllowed(AppRoute source, AppRoute destination)
        {
            switch (source)
            {
                case AppRoute.Lobby:
                    return destination == AppRoute.Battle;
                case AppRoute.Battle:
                    return destination == AppRoute.Settlement;
                case AppRoute.Settlement:
                    return destination == AppRoute.Lobby || destination == AppRoute.Battle;
                default:
                    return false;
            }
        }

        private void SetTransitionState(AppTransitionState state)
        {
            if (TransitionState == state) return;
            TransitionState = state;
            TransitionStateChanged?.Invoke(state);
        }
    }
}
