using System;
using FruitDefense.App;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Shell
{
    [DisallowMultipleComponent]
    public sealed class SettlementPresenter : MonoBehaviour
    {
        public const string MissingContext = "shell-context-missing";
        public const string MissingNavigator = "shell-navigator-missing";
        public const string MissingResult = "settlement-result-missing";
        public const string InvalidResult = "settlement-result-invalid";

        private IShellFlowContext _context;
        private RuntimeUiTheme _runtimeUiTheme;
        private RuntimeUiDrawContext _drawContext;
        private bool _recoveryAttempted;
        private RuntimeUiFeedbackPulse _focusPulse;
        private RuntimeUiFeedbackPulse _pressPulse;
        private RuntimeUiFeedbackPulse _transitionPulse;
        private RuntimeUiFeedbackPulse _statusPulse;
        private string _focusTarget = string.Empty;
        private string _pressTarget = string.Empty;
        private string _transitionTarget = string.Empty;
        private string _observedErrorCode = string.Empty;
        private bool _wasTransitioning;

        private const string RetryFeedbackTarget = "retry";
        private const string ReturnFeedbackTarget = "return";

        public SettlementViewData ViewData { get; private set; }
        public bool HasViewData { get; private set; }
        public ShellFlowError LastError { get; private set; }

        public void Initialize(IShellFlowContext context, RuntimeUiTheme runtimeUiTheme)
        {
            if (runtimeUiTheme == null)
                throw new ArgumentNullException(nameof(runtimeUiTheme));
            var validation = runtimeUiTheme.Validate();
            if (!validation.IsValid)
                throw new ArgumentException(validation.Issues[0].ToString(), nameof(runtimeUiTheme));

            _context = context;
            _runtimeUiTheme = runtimeUiTheme;
            _drawContext = null;
            ViewData = default;
            HasViewData = false;
            LastError = ShellFlowError.None;
            _recoveryAttempted = false;
            _focusPulse = default;
            _pressPulse = default;
            _transitionPulse = default;
            _statusPulse = default;
            _focusTarget = string.Empty;
            _pressTarget = string.Empty;
            _transitionTarget = string.Empty;
            _observedErrorCode = string.Empty;
            _wasTransitioning = context?.Navigator == null
                || context.Navigator.TransitionState != AppTransitionState.Idle;
            BindResultOrRecover();
        }

        public bool TryReturn()
        {
            if (!CanSendCommand()) return false;
            if (!_context.TryReturnToLobby(out var error)) return Fail(error);
            LastError = ShellFlowError.None;
            return true;
        }

        public bool TryRetry()
        {
            if (!HasViewData || !CanSendCommand()) return false;
            if (!_context.TryRetryBattle(out var error)) return Fail(error);
            LastError = ShellFlowError.None;
            return true;
        }

        public bool TryActivateAt(Vector2 guiPoint, float viewportWidth, float viewportHeight, Rect safeArea)
        {
            var layout = PortraitShellLayout.CreateSettlement(viewportWidth, viewportHeight, safeArea);
            var transitioning = _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
            switch (PortraitShellLayout.HitTest(layout, guiPoint, transitioning))
            {
                case ShellHitTarget.Retry: return TryRetry();
                case ShellHitTarget.Return: return TryReturn();
                default: return false;
            }
        }

        private void BindResultOrRecover()
        {
            if (_context == null)
            {
                Fail(new ShellFlowError(MissingContext));
                return;
            }

            if (_context.Navigator == null)
            {
                Recover(new ShellFlowError(MissingNavigator));
                return;
            }

            if (!_context.TryGetSettlementViewData(out var viewData, out var error))
            {
                Recover(error.IsEmpty ? new ShellFlowError(MissingResult) : error);
                return;
            }

            if (viewData.ReachedWave < 0 || viewData.RemainingLives < 0)
            {
                Recover(new ShellFlowError(InvalidResult));
                return;
            }

            ViewData = viewData;
            HasViewData = true;
            LastError = ShellFlowError.None;
        }

        private void Recover(ShellFlowError error)
        {
            if (_recoveryAttempted) return;
            _recoveryAttempted = true;
            Fail(error);
            _context.ReportRecoverableError(LastError);
            if (!_context.TryReturnToLobby(out var navigationError) && !navigationError.IsEmpty)
                LastError = navigationError;
        }

        private bool CanSendCommand()
        {
            if (_context == null) return Fail(new ShellFlowError(MissingContext));
            if (_context.Navigator == null) return Fail(new ShellFlowError(MissingNavigator));
            return _context.Navigator.TransitionState == AppTransitionState.Idle;
        }

        private void OnGUI()
        {
            if (_runtimeUiTheme == null) return;
            var unscaledTime = Time.unscaledTime;
            var layout = PortraitShellLayout.CreateSettlement(
                Screen.width, Screen.height, RuntimeSafeAreaResolver.ResolveCurrent());
            _drawContext = RuntimeUiGui.RequireContext(
                _drawContext, _runtimeUiTheme, layout.Frame.Scale);

            RuntimeUiGui.DrawScreenBackground(_drawContext,
                new Rect(0f, 0f, Screen.width, Screen.height));
            RuntimeUiGui.DrawSafeArea(_drawContext, layout.Frame.SafeArea);
            RuntimeUiGui.DrawScreenCorners(_drawContext, layout.Frame.SafeArea);
            RuntimeUiGui.DrawSectionRibbon(_drawContext, layout.Title);
            var titleCopy = RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.SettlementTitle);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.Title, titleCopy.Text,
                titleCopy.Role, titleCopy.Tone, titleCopy.Alignment);

            var transitioning = _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
            RefreshTransitionFeedback(transitioning, unscaledTime);
            RefreshStatusFeedback(unscaledTime);
            var resultState = ResolveResultState(HasViewData,
                HasViewData && ViewData.Victory);
            RuntimeUiGui.DrawResultCard(_drawContext, layout.ResultCard,
                RuntimeUiInteractionState.Normal);
            RuntimeUiGui.DrawResultBanner(_drawContext, layout.ResultBanner);
            RuntimeUiGui.DrawOrchardVista(_drawContext, layout.OrchardVista);

            if (HasViewData)
            {
                var outcomeCopy = RuntimeUiCopyCatalog.Get(ViewData.Victory
                    ? RuntimeUiCopyId.SettlementVictory
                    : RuntimeUiCopyId.SettlementDefeat);
                RuntimeUiGui.DrawSingleLineText(_drawContext, layout.Outcome,
                    outcomeCopy.Text, outcomeCopy.Role, outcomeCopy.Tone,
                    outcomeCopy.Alignment, resultState);
                DrawResultMetric(layout.CompletedLevel, RuntimeUiArtSlot.IconResourceSun,
                    RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.SettlementCompletedLevel).Text,
                    RuntimeUiCopyCatalog.LevelDisplayName(ViewData.LevelId));
                DrawResultMetric(layout.ReachedWave, RuntimeUiArtSlot.IconResourceWave,
                    RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.SettlementReachedWave).Text,
                    ViewData.ReachedWave.ToString());
                DrawResultMetric(layout.RemainingLives, RuntimeUiArtSlot.IconResourceCore,
                    RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.SettlementRemainingLives).Text,
                    ViewData.RemainingLives.ToString());
            }
            else
            {
                var returningCopy = RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.SettlementReturning);
                RuntimeUiGui.DrawSingleLineText(_drawContext, layout.Outcome,
                    returningCopy.Text,
                    returningCopy.Role, returningCopy.Tone,
                    returningCopy.Alignment, resultState);
            }
            RuntimeUiGui.DrawIndicator(_drawContext, layout.ResultIndicator,
                !HasViewData
                    ? RuntimeUiIndicatorKind.Loading
                    : ViewData.Victory
                        ? RuntimeUiIndicatorKind.Success
                        : RuntimeUiIndicatorKind.Error);

            var retryHovered = ContainsPointer(layout.RetryButton);
            if (retryHovered)
                BeginFocus(RetryFeedbackTarget, unscaledTime);
            var retryState = ResolveActionState(transitioning, HasViewData,
                retryHovered || IsFeedbackActive(_focusPulse, _focusTarget,
                    RetryFeedbackTarget, unscaledTime),
                IsPointerPress(layout.RetryButton)
                    || IsFeedbackActive(_pressPulse, _pressTarget,
                        RetryFeedbackTarget, unscaledTime));
            var retryCopy = RuntimeUiCopyCatalog.Get(transitioning
                ? RuntimeUiCopyId.SettlementTransitioning
                : RuntimeUiCopyId.SettlementRetry);
            if (RuntimeUiGui.DrawAction(_drawContext, layout.RetryButton,
                    retryCopy.Text,
                    RuntimeUiActionKind.Primary, retryState,
                    RuntimeUiArtSlot.IconControlRetry,
                    retryCopy.Role,
                    IsTransitionEmphasized(RetryFeedbackTarget, unscaledTime)))
            {
                BeginPress(RetryFeedbackTarget, unscaledTime);
                if (TryRetry())
                    BeginTransition(RetryFeedbackTarget, unscaledTime);
            }

            var returnHovered = ContainsPointer(layout.ReturnButton);
            if (returnHovered)
                BeginFocus(ReturnFeedbackTarget, unscaledTime);
            var returnState = ResolveActionState(transitioning, true,
                returnHovered || IsFeedbackActive(_focusPulse, _focusTarget,
                    ReturnFeedbackTarget, unscaledTime),
                IsPointerPress(layout.ReturnButton)
                    || IsFeedbackActive(_pressPulse, _pressTarget,
                        ReturnFeedbackTarget, unscaledTime));
            var returnCopy = RuntimeUiCopyCatalog.Get(transitioning
                ? RuntimeUiCopyId.SettlementTransitioning
                : RuntimeUiCopyId.SettlementReturn);
            if (RuntimeUiGui.DrawAction(_drawContext, layout.ReturnButton,
                    returnCopy.Text,
                    RuntimeUiActionKind.Quiet, returnState,
                    RuntimeUiArtSlot.IconControlReturn,
                    returnCopy.Role,
                    IsTransitionEmphasized(ReturnFeedbackTarget, unscaledTime)))
            {
                BeginPress(ReturnFeedbackTarget, unscaledTime);
                if (TryReturn())
                    BeginTransition(ReturnFeedbackTarget, unscaledTime);
            }

            if (!LastError.IsEmpty)
            {
                var errorCopy = RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.SettlementRecoveredError);
                RuntimeUiGui.DrawStatus(_drawContext, layout.Status,
                    RuntimeUiCopyCatalog.FormatSettlementRecoveredError(
                        LastError.Code),
                    RuntimeUiInteractionState.Warning,
                    errorCopy.Role,
                    RuntimeUiCopyCatalog.StatusTextMode(errorCopy),
                    _statusPulse.IsActive(unscaledTime));
            }
        }

        private void BeginFocus(string target, float unscaledTime)
        {
            _focusTarget = target;
            _focusPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledFocusSeconds);
        }

        private void BeginPress(string target, float unscaledTime)
        {
            _pressTarget = target;
            _pressPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledPressSeconds);
        }

        private void BeginTransition(string target, float unscaledTime)
        {
            _transitionTarget = target;
            _transitionPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledTransitionSeconds);
        }

        private void RefreshTransitionFeedback(bool transitioning, float unscaledTime)
        {
            if (transitioning && !_wasTransitioning)
            {
                var target = string.IsNullOrEmpty(_transitionTarget)
                    ? ReturnFeedbackTarget
                    : _transitionTarget;
                BeginTransition(target, unscaledTime);
            }
            _wasTransitioning = transitioning;
        }

        private void RefreshStatusFeedback(float unscaledTime)
        {
            var errorCode = LastError.Code ?? string.Empty;
            if (string.Equals(errorCode, _observedErrorCode, StringComparison.Ordinal))
                return;
            _observedErrorCode = errorCode;
            if (!string.IsNullOrEmpty(errorCode))
            {
                _statusPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                    _runtimeUiTheme.Feedback.UnscaledStatusSeconds);
            }
        }

        private bool IsTransitionEmphasized(string target, float unscaledTime)
        {
            return IsFeedbackActive(_transitionPulse, _transitionTarget,
                target, unscaledTime);
        }

        private static bool IsFeedbackActive(RuntimeUiFeedbackPulse pulse,
            string activeTarget, string target, float unscaledTime)
        {
            return string.Equals(activeTarget, target, StringComparison.Ordinal)
                && pulse.IsActive(unscaledTime);
        }

        private void DrawResultMetric(Rect rect, RuntimeUiArtSlot icon,
            string label, string value)
        {
            RuntimeUiGui.DrawMetric(_drawContext, rect, icon, label, value,
                compactInline: true);
        }

        internal static RuntimeUiInteractionState ResolveResultState(bool hasViewData,
            bool victory)
        {
            if (!hasViewData) return RuntimeUiInteractionState.Loading;
            return victory
                ? RuntimeUiInteractionState.Success
                : RuntimeUiInteractionState.Error;
        }

        internal static RuntimeUiInteractionState ResolveActionState(bool transitioning,
            bool available, bool pointerInside, bool pointerPressed)
        {
            if (transitioning) return RuntimeUiInteractionState.Loading;
            if (!available) return RuntimeUiInteractionState.Disabled;
            if (pointerPressed) return RuntimeUiInteractionState.Pressed;
            return pointerInside
                ? RuntimeUiInteractionState.HoveredOrFocused
                : RuntimeUiInteractionState.Normal;
        }

        private static bool ContainsPointer(Rect rect)
        {
            return Event.current != null && rect.Contains(Event.current.mousePosition);
        }

        private static bool IsPointerPress(Rect rect)
        {
            return ContainsPointer(rect) && Event.current.button == 0
                && (Event.current.rawType == EventType.MouseDown
                    || Event.current.rawType == EventType.MouseDrag);
        }

        private bool Fail(ShellFlowError error)
        {
            LastError = error.IsEmpty ? new ShellFlowError("shell-command-rejected") : error;
            return false;
        }
    }
}
