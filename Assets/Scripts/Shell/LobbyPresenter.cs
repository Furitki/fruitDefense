using System;
using FruitDefense.App;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Shell
{
    [DisallowMultipleComponent]
    public sealed class LobbyPresenter : MonoBehaviour
    {
        public const string Orchard01LevelId = "orchard-01";
        public const string Orchard02LevelId = "orchard-02";
        public const string Orchard03LevelId = "orchard-03";
        public const string MissingContext = "shell-context-missing";
        public const string MissingNavigator = "shell-navigator-missing";
        public const string MissingContentVersion = "bundled-content-version-missing";
        public const string MissingSelectedLevel = "lobby-selected-level-missing";
        public const string LevelSelectionMismatch = "lobby-level-selection-mismatch";

        private IShellFlowContext _context;
        private RuntimeUiTheme _runtimeUiTheme;
        private string _visibleSelectedLevelId = string.Empty;
        private RuntimeUiDrawContext _drawContext;
        private RuntimeUiFeedbackPulse _focusPulse;
        private RuntimeUiFeedbackPulse _pressPulse;
        private RuntimeUiFeedbackPulse _selectionPulse;
        private RuntimeUiFeedbackPulse _transitionPulse;
        private RuntimeUiFeedbackPulse _statusPulse;
        private RuntimeUiFeedbackPulse _routeRevealPulse;
        private RuntimeUiPressTracker _pressTracker;
        private string _focusTarget = string.Empty;
        private string _pressTarget = string.Empty;
        private string _selectionTarget = string.Empty;
        private string _transitionTarget = string.Empty;
        private string _observedErrorCode = string.Empty;
        private bool _wasTransitioning;

        private const string StartFeedbackTarget = "start";
        private const int Orchard01ControlId = 1101;
        private const int Orchard02ControlId = 1102;
        private const int Orchard03ControlId = 1103;
        private const int StartControlId = 1104;

        public ShellFlowError LastError { get; private set; }
        public string LastSessionId { get; private set; } = string.Empty;
        public int LastSeed { get; private set; }
        public string LastContentVersion { get; private set; } = string.Empty;
        public string SelectedLevelId => _visibleSelectedLevelId;

        public void Initialize(IShellFlowContext context, RuntimeUiTheme runtimeUiTheme)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!(context is ILevelSelectionFlowContext selection))
                throw new ArgumentException(
                    "Lobby requires a level-selection flow context.", nameof(context));
            if (runtimeUiTheme == null)
                throw new ArgumentNullException(nameof(runtimeUiTheme));
            var validation = runtimeUiTheme.Validate();
            if (!validation.IsValid)
                throw new ArgumentException(validation.Issues[0].ToString(), nameof(runtimeUiTheme));

            _context = context;
            _runtimeUiTheme = runtimeUiTheme;
            _drawContext = null;
            _visibleSelectedLevelId = selection.SelectedLevelId ?? string.Empty;
            LastError = ShellFlowError.None;
            _focusPulse = default;
            _pressPulse = default;
            _selectionPulse = default;
            _transitionPulse = default;
            _statusPulse = default;
            _routeRevealPulse = RuntimeUiMotion.BeginReveal(Time.unscaledTime,
                runtimeUiTheme.Feedback, 4);
            _pressTracker.Cancel();
            _focusTarget = string.Empty;
            _pressTarget = string.Empty;
            _selectionTarget = string.Empty;
            _transitionTarget = string.Empty;
            _observedErrorCode = string.Empty;
            _wasTransitioning = context.Navigator == null
                || context.Navigator.TransitionState != AppTransitionState.Idle;
        }

        private void OnDisable()
        {
            _pressTracker.Cancel();
            _routeRevealPulse = default;
        }

        public bool TrySelectLevel(string levelId)
        {
            if (_context == null)
                return Fail(new ShellFlowError(MissingContext));
            if (_context.Navigator == null)
                return Fail(new ShellFlowError(MissingNavigator));
            if (_context.Navigator.TransitionState != AppTransitionState.Idle)
                return false;

            var selection = (ILevelSelectionFlowContext)_context;
            if (!selection.TrySelectLevel(levelId, out var error))
                return Fail(error);
            if (!string.Equals(selection.SelectedLevelId, levelId, StringComparison.Ordinal))
                return Fail(new ShellFlowError(LevelSelectionMismatch,
                    levelId + ":" + (selection.SelectedLevelId ?? string.Empty)));

            _visibleSelectedLevelId = levelId;
            LastError = ShellFlowError.None;
            return true;
        }

        public bool TryStart()
        {
            if (_context == null)
                return Fail(new ShellFlowError(MissingContext));
            if (_context.Navigator == null)
                return Fail(new ShellFlowError(MissingNavigator));
            if (_context.Navigator.TransitionState != AppTransitionState.Idle)
                return false;

            var selectedLevelId = _visibleSelectedLevelId;
            if (string.IsNullOrWhiteSpace(selectedLevelId))
                return Fail(new ShellFlowError(MissingSelectedLevel));
            if (!IsPlayable((ILevelSelectionFlowContext)_context, selectedLevelId))
                return Fail(new ShellFlowError(MissingSelectedLevel, selectedLevelId));

            var contentVersion = _context.BundledContentVersion;
            if (string.IsNullOrWhiteSpace(contentVersion))
                return Fail(new ShellFlowError(MissingContentVersion));

            var sessionId = Guid.NewGuid().ToString("N");
            var seed = CreateNonzeroSeed();
            if (!_context.TryStartDefaultBattle(
                    selectedLevelId,
                    sessionId,
                    seed,
                    contentVersion,
                    out var error))
                return Fail(error);

            LastSessionId = sessionId;
            LastSeed = seed;
            LastContentVersion = contentVersion;
            LastError = ShellFlowError.None;
            return true;
        }

        public bool TryActivateAt(Vector2 guiPoint, float viewportWidth, float viewportHeight, Rect safeArea)
        {
            var layout = PortraitShellLayout.CreateLobby(viewportWidth, viewportHeight, safeArea);
            var transitioning = _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
            switch (PortraitShellLayout.HitTest(layout, guiPoint, transitioning))
            {
                case ShellHitTarget.LevelOrchard01: return TrySelectLevel(Orchard01LevelId);
                case ShellHitTarget.LevelOrchard02: return TrySelectLevel(Orchard02LevelId);
                case ShellHitTarget.LevelOrchard03: return TrySelectLevel(Orchard03LevelId);
                case ShellHitTarget.Start: return TryStart();
                default: return false;
            }
        }

        private void OnGUI()
        {
            if (_runtimeUiTheme == null) return;
            var unscaledTime = Time.unscaledTime;
            var pointer = RuntimeUiPointerSample.FromEvent(Event.current);
            var layout = PortraitShellLayout.CreateLobby(
                Screen.width, Screen.height, RuntimeSafeAreaResolver.ResolveCurrent());
            _drawContext = RuntimeUiGui.RequireContext(
                _drawContext, _runtimeUiTheme, layout.Frame.Scale);

            RuntimeUiGui.DrawScreenBackground(_drawContext,
                new Rect(0f, 0f, Screen.width, Screen.height));
            RuntimeUiGui.DrawSafeArea(_drawContext, layout.Frame.SafeArea);
            RuntimeUiGui.DrawShellOrchardDepth(_drawContext,
                layout.Frame.SafeArea, .56f);
            RuntimeUiGui.DrawScreenCorners(_drawContext, layout.Frame.SafeArea);
            var titleMotion = RuntimeUiMotion.Evaluate(_routeRevealPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Stagger, 0);
            var titleRect = titleMotion.Transform(layout.Title);
            var previousTitleColor = GUI.color;
            GUI.color = new Color(previousTitleColor.r, previousTitleColor.g,
                previousTitleColor.b, previousTitleColor.a * titleMotion.Alpha);
            RuntimeUiGui.DrawSectionRibbon(_drawContext, titleRect);
            var titleCopy = RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.LobbyTitle);
            RuntimeUiGui.DrawSingleLineText(_drawContext, titleRect, titleCopy.Text,
                titleCopy.Role, titleCopy.Tone, titleCopy.Alignment);
            GUI.color = previousTitleColor;

            var transitioning = _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
            RefreshTransitionFeedback(transitioning, unscaledTime);
            RefreshStatusFeedback(unscaledTime);
            if (transitioning) _pressTracker.Cancel();
            var transitionEmphasized = _transitionPulse.IsActive(unscaledTime);
            DrawLevelCard(layout.Orchard01Card, Orchard01LevelId,
                RuntimeUiCopyId.LobbyOrchard01Title,
                RuntimeUiCopyId.LobbyOrchard01Body, transitioning,
                transitionEmphasized, unscaledTime, RuntimeUiLobbyThumbnail.Orchard01,
                1, pointer);
            DrawLevelCard(layout.Orchard02Card, Orchard02LevelId,
                RuntimeUiCopyId.LobbyOrchard02Title,
                RuntimeUiCopyId.LobbyOrchard02Body, transitioning,
                transitionEmphasized, unscaledTime, RuntimeUiLobbyThumbnail.Orchard02,
                2, pointer);
            DrawLevelCard(layout.Orchard03Card, Orchard03LevelId,
                RuntimeUiCopyId.LobbyOrchard03Title,
                RuntimeUiCopyId.LobbyOrchard03Body, transitioning,
                transitionEmphasized, unscaledTime, RuntimeUiLobbyThumbnail.Orchard03,
                3, pointer);

            var startAvailable = !transitioning
                && !string.IsNullOrWhiteSpace(_visibleSelectedLevelId);
            var startPress = _pressTracker.Update(StartControlId, layout.StartButton,
                startAvailable, pointer, _runtimeUiTheme.Feedback.DragCancelDistance);
            var startHovered = startPress.Hovered;
            if (startHovered)
                BeginFocus(StartFeedbackTarget, unscaledTime);
            var startPressed = startPress.Pressed;
            var startState = ResolveActionState(transitioning,
                !string.IsNullOrWhiteSpace(_visibleSelectedLevelId),
                startHovered || IsFeedbackActive(_focusPulse, _focusTarget,
                    StartFeedbackTarget, unscaledTime), startPressed);
            var startCopy = RuntimeUiCopyCatalog.Get(transitioning
                ? RuntimeUiCopyId.LobbyTransitioning
                : RuntimeUiCopyId.LobbyStart);
            var startReveal = RuntimeUiMotion.Evaluate(_routeRevealPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Stagger, 4);
            var startPressMotion = string.Equals(_pressTarget, StartFeedbackTarget,
                    StringComparison.Ordinal)
                ? RuntimeUiMotion.Evaluate(_pressPulse, unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Press)
                : RuntimeUiMotionSample.Rest;
            RuntimeUiGui.DrawActionVisual(_drawContext, layout.StartButton,
                transitioning ? startCopy.Text
                    : RuntimeUiCopyCatalog.FormatLobbyStart(_visibleSelectedLevelId),
                new RuntimeUiActionSpec(RuntimeUiActionKind.Primary,
                    RuntimeUiActionContentForm.IconLabel,
                    RuntimeUiActionBehavior.Instantaneous), startState,
                RuntimeUiArtSlot.IconControlStart,
                startCopy.Role,
                transitionEmphasized
                    && string.Equals(_transitionTarget, StartFeedbackTarget,
                        StringComparison.Ordinal),
                RuntimeUiMotionSample.Combine(startReveal, startPressMotion));
            if (startPress.Activated)
            {
                BeginPress(StartFeedbackTarget, unscaledTime);
                if (TryStart())
                    BeginTransition(StartFeedbackTarget, unscaledTime);
            }

            var status = LastError.IsEmpty
                ? string.Empty
                : RuntimeUiCopyCatalog.FormatLobbyError(LastError.Code);
            if (!string.IsNullOrEmpty(status))
            {
                var statusCopy = RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.LobbyError);
                RuntimeUiGui.DrawStatus(_drawContext, layout.Status, status,
                    RuntimeUiInteractionState.Error,
                    statusCopy.Role,
                    RuntimeUiCopyCatalog.StatusTextMode(statusCopy),
                    _statusPulse.IsActive(unscaledTime));
            }
        }

        private void DrawLevelCard(Rect rect, string levelId,
            RuntimeUiCopyId titleCopyId, RuntimeUiCopyId bodyCopyId,
            bool transitioning, bool transitionEmphasized, float unscaledTime,
            RuntimeUiLobbyThumbnail thumbnail, int revealIndex,
            RuntimeUiPointerSample pointer)
        {
            var selected = string.Equals(_visibleSelectedLevelId, levelId, StringComparison.Ordinal);
            var available = IsPlayable((ILevelSelectionFlowContext)_context, levelId);
            var press = _pressTracker.Update(LevelControlId(levelId), rect,
                !transitioning && available, pointer,
                _runtimeUiTheme.Feedback.DragCancelDistance);
            var pointerInside = press.Hovered;
            if (pointerInside)
                BeginFocus(levelId, unscaledTime);
            var pointerPressed = press.Pressed;
            var state = ResolveCardState(transitioning, available, selected,
                pointerInside || IsFeedbackActive(
                    _focusPulse, _focusTarget, levelId, unscaledTime), pointerPressed);
            var selectionEmphasized = selected && IsFeedbackActive(
                _selectionPulse, _selectionTarget, levelId, unscaledTime);
            var revealMotion = RuntimeUiMotion.Evaluate(_routeRevealPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Stagger, revealIndex);
            var selectionMotion = RuntimeUiMotion.Evaluate(_selectionPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Pop);
            var pressMotion = RuntimeUiMotion.Evaluate(_pressPulse, unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Press);
            var feedbackMotion = string.Equals(_selectionTarget, levelId,
                    StringComparison.Ordinal) ? selectionMotion
                : string.Equals(_pressTarget, levelId, StringComparison.Ordinal)
                    ? pressMotion : RuntimeUiMotionSample.Rest;
            var heldMotion = press.Pressed
                ? RuntimeUiMotion.InteractionState(
                    RuntimeUiInteractionState.Pressed, _runtimeUiTheme.Feedback)
                : RuntimeUiMotionSample.Rest;
            var motion = RuntimeUiMotionSample.Combine(revealMotion,
                RuntimeUiMotionSample.Combine(feedbackMotion, heldMotion));
            var visualRect = motion.Transform(rect);
            RuntimeUiGui.DrawSelectableCard(_drawContext, rect, state,
                selectionEmphasized || transitionEmphasized,
                drawStateIndicator: false, motion: motion);
            var previousColor = GUI.color;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b,
                previousColor.a * motion.Alpha);

            var cardLayout = PortraitShellLayout.CreateLobbyLevelCard(
                visualRect, _drawContext.Scale);
            RuntimeUiGui.DrawLobbyThumbnail(_drawContext, cardLayout.Thumbnail,
                thumbnail);
            RuntimeUiGui.DrawIllustrationFrame(_drawContext, cardLayout.Frame);
            var titleCopy = RuntimeUiCopyCatalog.Get(titleCopyId);
            var bodyCopy = RuntimeUiCopyCatalog.Get(bodyCopyId);
            RuntimeUiGui.DrawSingleLineText(_drawContext,
                cardLayout.Title, titleCopy.Text, titleCopy.Role,
                titleCopy.Tone, titleCopy.Alignment, state);
            RuntimeUiGui.DrawSingleLineText(_drawContext, cardLayout.Body,
                bodyCopy.Text, bodyCopy.Role,
                bodyCopy.Tone, bodyCopy.Alignment, state);

            if (selected)
            {
                RuntimeUiGui.DrawIndicator(_drawContext, cardLayout.SelectedMarker,
                    RuntimeUiIndicatorKind.Selected);
            }
            if (state == RuntimeUiInteractionState.Disabled)
                RuntimeUiGui.DrawIndicator(_drawContext, cardLayout.TransientIndicator,
                    RuntimeUiIndicatorKind.Disabled);
            else if (state == RuntimeUiInteractionState.Loading)
                RuntimeUiGui.DrawIndicator(_drawContext, cardLayout.TransientIndicator,
                    RuntimeUiIndicatorKind.Loading);
            GUI.color = previousColor;

            if (press.Activated)
            {
                BeginPress(levelId, unscaledTime);
                if (TrySelectLevel(levelId))
                {
                    _selectionTarget = levelId;
                    _selectionPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                        _runtimeUiTheme.Feedback.UnscaledSelectionSeconds);
                }
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
                BeginTransition(StartFeedbackTarget, unscaledTime);
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

        private static bool IsFeedbackActive(RuntimeUiFeedbackPulse pulse,
            string activeTarget, string target, float unscaledTime)
        {
            return string.Equals(activeTarget, target, StringComparison.Ordinal)
                && pulse.IsActive(unscaledTime);
        }

        internal static RuntimeUiInteractionState ResolveCardState(bool transitioning,
            bool available, bool selected, bool pointerInside, bool pointerPressed)
        {
            if (transitioning) return RuntimeUiInteractionState.Loading;
            if (!available) return RuntimeUiInteractionState.Disabled;
            if (pointerPressed) return RuntimeUiInteractionState.Pressed;
            if (selected) return RuntimeUiInteractionState.Selected;
            return pointerInside
                ? RuntimeUiInteractionState.HoveredOrFocused
                : RuntimeUiInteractionState.Normal;
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

        private static bool IsPlayable(ILevelSelectionFlowContext selection, string levelId)
        {
            if (selection?.PlayableLevels == null || string.IsNullOrEmpty(levelId)) return false;
            for (var i = 0; i < selection.PlayableLevels.Count; i++)
            {
                var level = selection.PlayableLevels[i];
                if (level != null && string.Equals(level.LevelId, levelId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static int LevelControlId(string levelId)
        {
            if (string.Equals(levelId, Orchard01LevelId, StringComparison.Ordinal))
                return Orchard01ControlId;
            if (string.Equals(levelId, Orchard02LevelId, StringComparison.Ordinal))
                return Orchard02ControlId;
            if (string.Equals(levelId, Orchard03LevelId, StringComparison.Ordinal))
                return Orchard03ControlId;
            throw new ArgumentOutOfRangeException(nameof(levelId), levelId,
                "Lobby level has no stable press control ID.");
        }

        private bool Fail(ShellFlowError error)
        {
            LastError = error.IsEmpty ? new ShellFlowError("shell-command-rejected") : error;
            return false;
        }

        internal static int CreateNonzeroSeed()
        {
            var bytes = Guid.NewGuid().ToByteArray();
            var seed = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
            return seed == 0 ? 1 : seed;
        }
    }
}
