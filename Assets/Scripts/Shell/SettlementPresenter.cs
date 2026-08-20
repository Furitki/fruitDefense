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
        private RuntimeUiFeedbackPulse _routeRevealPulse;
        private RuntimeUiFeedbackPulse _resultEmphasisPulse;
        private RuntimeUiPressTracker _pressTracker;
        private string _focusTarget = string.Empty;
        private string _pressTarget = string.Empty;
        private string _transitionTarget = string.Empty;
        private string _observedErrorCode = string.Empty;
        private bool _wasTransitioning;

        private const string RetryFeedbackTarget = "retry";
        private const string ReturnFeedbackTarget = "return";
        private const int RetryControlId = 2101;
        private const int ReturnControlId = 2102;

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
            _routeRevealPulse = default;
            _resultEmphasisPulse = default;
            _pressTracker.Cancel();
            _focusTarget = string.Empty;
            _pressTarget = string.Empty;
            _transitionTarget = string.Empty;
            _observedErrorCode = string.Empty;
            _wasTransitioning = context?.Navigator == null
                || context.Navigator.TransitionState != AppTransitionState.Idle;
            BindResultOrRecover();
            _routeRevealPulse = RuntimeUiMotion.BeginReveal(Time.unscaledTime,
                runtimeUiTheme.Feedback, 5);
            if (HasViewData)
            {
                _resultEmphasisPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                    runtimeUiTheme.Feedback.UnscaledTransitionSeconds
                    + runtimeUiTheme.Feedback.UnscaledSelectionSeconds);
            }
        }

        private void OnDisable()
        {
            _pressTracker.Cancel();
            _routeRevealPulse = default;
            _resultEmphasisPulse = default;
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
            var pointer = RuntimeUiPointerSample.FromEvent(Event.current);
            var layout = PortraitShellLayout.CreateSettlement(
                Screen.width, Screen.height, RuntimeSafeAreaResolver.ResolveCurrent());
            _drawContext = RuntimeUiGui.RequireContext(
                _drawContext, _runtimeUiTheme, layout.Frame.Scale);

            RuntimeUiGui.DrawScreenBackground(_drawContext,
                new Rect(0f, 0f, Screen.width, Screen.height));
            RuntimeUiGui.DrawSafeArea(_drawContext, layout.Frame.SafeArea);
            RuntimeUiGui.DrawScreenCorners(_drawContext, layout.Frame.SafeArea);
            var titleMotion = RuntimeUiMotion.Evaluate(_routeRevealPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Stagger, 0);
            var titleRect = titleMotion.Transform(layout.Title);
            var previousTitleColor = GUI.color;
            GUI.color = new Color(previousTitleColor.r, previousTitleColor.g,
                previousTitleColor.b, previousTitleColor.a * titleMotion.Alpha);
            RuntimeUiGui.DrawSectionRibbon(_drawContext, titleRect);
            var titleCopy = RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.SettlementTitle);
            RuntimeUiGui.DrawSingleLineText(_drawContext, titleRect, titleCopy.Text,
                titleCopy.Role, titleCopy.Tone, titleCopy.Alignment);
            GUI.color = previousTitleColor;

            var transitioning = _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
            RefreshTransitionFeedback(transitioning, unscaledTime);
            RefreshStatusFeedback(unscaledTime);
            if (transitioning) _pressTracker.Cancel();
            var resultState = ResolveResultState(HasViewData,
                HasViewData && ViewData.Victory);
            var resultReveal = RuntimeUiMotion.Evaluate(_routeRevealPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Stagger, 1);
            var resultPop = RuntimeUiMotion.Evaluate(_resultEmphasisPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.StrongPop);
            var resultMotion = RuntimeUiMotionSample.Combine(resultReveal, resultPop);
            var resultCardRect = resultMotion.Transform(layout.ResultCard);
            var previousResultColor = GUI.color;
            GUI.color = new Color(previousResultColor.r, previousResultColor.g,
                previousResultColor.b, previousResultColor.a * resultMotion.Alpha);
            RuntimeUiGui.DrawResultCard(_drawContext, resultCardRect,
                RuntimeUiInteractionState.Normal);
            RuntimeUiGui.DrawResultBanner(_drawContext,
                TransformInside(layout.ResultBanner, layout.ResultCard, resultCardRect));
            RuntimeUiGui.DrawOrchardVista(_drawContext,
                TransformInside(layout.OrchardVista, layout.ResultCard, resultCardRect));
            GUI.color = previousResultColor;

            if (HasViewData)
            {
                var outcomeCopy = RuntimeUiCopyCatalog.Get(ViewData.Victory
                    ? RuntimeUiCopyId.SettlementVictory
                    : RuntimeUiCopyId.SettlementDefeat);
                var outcomeRect = TransformInside(
                    layout.Outcome, layout.ResultCard, resultCardRect);
                var previousOutcomeColor = GUI.color;
                GUI.color = new Color(previousOutcomeColor.r, previousOutcomeColor.g,
                    previousOutcomeColor.b,
                    previousOutcomeColor.a * resultMotion.Alpha);
                try
                {
                    RuntimeUiGui.DrawSingleLineText(_drawContext, outcomeRect,
                        outcomeCopy.Text, outcomeCopy.Role, outcomeCopy.Tone,
                        outcomeCopy.Alignment, resultState);
                }
                finally
                {
                    GUI.color = previousOutcomeColor;
                }
                DrawResultMetric(layout.CompletedLevel, RuntimeUiArtSlot.IconResourceSun,
                    RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.SettlementCompletedLevel).Text,
                    RuntimeUiCopyCatalog.LevelDisplayName(ViewData.LevelId), 2, unscaledTime);
                DrawResultMetric(layout.ReachedWave, RuntimeUiArtSlot.IconResourceWave,
                    RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.SettlementReachedWave).Text,
                    ViewData.ReachedWave.ToString(), 3, unscaledTime);
                DrawResultMetric(layout.RemainingLives, RuntimeUiArtSlot.IconResourceCore,
                    RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.SettlementRemainingLives).Text,
                    ViewData.RemainingLives.ToString(), 4, unscaledTime);
            }
            else
            {
                var returningCopy = RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.SettlementReturning);
                var returningRect = TransformInside(
                    layout.Outcome, layout.ResultCard, resultCardRect);
                var previousReturningColor = GUI.color;
                GUI.color = new Color(previousReturningColor.r,
                    previousReturningColor.g, previousReturningColor.b,
                    previousReturningColor.a * resultMotion.Alpha);
                try
                {
                    RuntimeUiGui.DrawSingleLineText(_drawContext, returningRect,
                        returningCopy.Text,
                        returningCopy.Role, returningCopy.Tone,
                        returningCopy.Alignment, resultState);
                }
                finally
                {
                    GUI.color = previousReturningColor;
                }
            }
            var indicatorRect = TransformInside(
                layout.ResultIndicator, layout.ResultCard, resultCardRect);
            var previousIndicatorColor = GUI.color;
            GUI.color = new Color(previousIndicatorColor.r, previousIndicatorColor.g,
                previousIndicatorColor.b,
                previousIndicatorColor.a * resultMotion.Alpha);
            try
            {
                RuntimeUiGui.DrawIndicator(_drawContext, indicatorRect,
                    !HasViewData
                        ? RuntimeUiIndicatorKind.Loading
                        : ViewData.Victory
                            ? RuntimeUiIndicatorKind.Success
                            : RuntimeUiIndicatorKind.Error);
            }
            finally
            {
                GUI.color = previousIndicatorColor;
            }

            var retryPress = _pressTracker.Update(RetryControlId, layout.RetryButton,
                !transitioning && HasViewData, pointer,
                _runtimeUiTheme.Feedback.DragCancelDistance);
            var retryHovered = retryPress.Hovered;
            if (retryHovered)
                BeginFocus(RetryFeedbackTarget, unscaledTime);
            var retryState = ResolveActionState(transitioning, HasViewData,
                retryHovered || IsFeedbackActive(_focusPulse, _focusTarget,
                    RetryFeedbackTarget, unscaledTime),
                retryPress.Pressed);
            var retryCopy = RuntimeUiCopyCatalog.Get(transitioning
                ? RuntimeUiCopyId.SettlementTransitioning
                : RuntimeUiCopyId.SettlementRetry);
            var retryReveal = RuntimeUiMotion.Evaluate(_routeRevealPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Stagger, 5);
            var retryMotion = IsFeedbackActive(_pressPulse, _pressTarget,
                    RetryFeedbackTarget, unscaledTime)
                ? RuntimeUiMotion.Evaluate(_pressPulse, unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Press)
                : RuntimeUiMotionSample.Rest;
            RuntimeUiGui.DrawActionVisual(_drawContext, layout.RetryButton,
                retryCopy.Text, RuntimeUiActionKind.Primary, retryState,
                RuntimeUiArtSlot.IconControlRetry, retryCopy.Role,
                IsTransitionEmphasized(RetryFeedbackTarget, unscaledTime),
                RuntimeUiMotionSample.Combine(retryReveal, retryMotion));
            if (retryPress.Activated)
            {
                BeginPress(RetryFeedbackTarget, unscaledTime);
                if (TryRetry())
                    BeginTransition(RetryFeedbackTarget, unscaledTime);
            }

            var returnPress = _pressTracker.Update(ReturnControlId, layout.ReturnButton,
                !transitioning, pointer, _runtimeUiTheme.Feedback.DragCancelDistance);
            var returnHovered = returnPress.Hovered;
            if (returnHovered)
                BeginFocus(ReturnFeedbackTarget, unscaledTime);
            var returnState = ResolveActionState(transitioning, true,
                returnHovered || IsFeedbackActive(_focusPulse, _focusTarget,
                    ReturnFeedbackTarget, unscaledTime),
                returnPress.Pressed);
            var returnCopy = RuntimeUiCopyCatalog.Get(transitioning
                ? RuntimeUiCopyId.SettlementTransitioning
                : RuntimeUiCopyId.SettlementReturn);
            var returnReveal = RuntimeUiMotion.Evaluate(_routeRevealPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Stagger, 5);
            var returnMotion = IsFeedbackActive(_pressPulse, _pressTarget,
                    ReturnFeedbackTarget, unscaledTime)
                ? RuntimeUiMotion.Evaluate(_pressPulse, unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Press)
                : RuntimeUiMotionSample.Rest;
            RuntimeUiGui.DrawActionVisual(_drawContext, layout.ReturnButton,
                returnCopy.Text, RuntimeUiActionKind.Quiet, returnState,
                RuntimeUiArtSlot.IconControlReturn, returnCopy.Role,
                IsTransitionEmphasized(ReturnFeedbackTarget, unscaledTime),
                RuntimeUiMotionSample.Combine(returnReveal, returnMotion));
            if (returnPress.Activated)
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
            string label, string value, int revealIndex, float unscaledTime)
        {
            var motion = RuntimeUiMotion.Evaluate(_routeRevealPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Stagger, revealIndex);
            RuntimeUiGui.DrawMetric(_drawContext, rect, icon, label, value,
                compactInline: true, motion: motion);
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

        private static Rect TransformInside(Rect child, Rect sourceParent,
            Rect visualParent)
        {
            if (sourceParent.width <= 0f || sourceParent.height <= 0f) return child;
            var scaleX = visualParent.width / sourceParent.width;
            var scaleY = visualParent.height / sourceParent.height;
            return new Rect(
                visualParent.x + (child.x - sourceParent.x) * scaleX,
                visualParent.y + (child.y - sourceParent.y) * scaleY,
                child.width * scaleX,
                child.height * scaleY);
        }

        private bool Fail(ShellFlowError error)
        {
            LastError = error.IsEmpty ? new ShellFlowError("shell-command-rejected") : error;
            return false;
        }
    }
}
