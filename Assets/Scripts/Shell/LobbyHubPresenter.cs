using System;
using System.Collections;
using System.Linq;
using FruitDefense.App;
using FruitDefense.App.Services;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Shell
{
    [DisallowMultipleComponent]
    public sealed class LobbyHubPresenter : MonoBehaviour
    {
        public const string Orchard01LevelId = "orchard-01";
        public const string Orchard02LevelId = "orchard-02";
        public const string Orchard03LevelId = "orchard-03";
        public const string MissingContext = "shell-context-missing";
        public const string MissingNavigator = "shell-navigator-missing";
        public const string MissingContentVersion = "bundled-content-version-missing";
        public const string MissingSelectedLevel = "lobby-selected-level-missing";
        public const string LevelSelectionMismatch = "lobby-level-selection-mismatch";
        public const string GrowthPreviewUnavailable =
            "lobby-growth-preview-unavailable";
        public const string ProgressionCommandActive =
            "lobby-progression-command-active";

        private IShellFlowContext _context;
        private IHubProgressionReadContext _hubRead;
        private IHubProgressionCommandContext _hubCommands;
        private RuntimeUiTheme _runtimeUiTheme;
        private string _visibleSelectedLevelId = string.Empty;
        private RuntimeUiDrawContext _drawContext;
        private RuntimeUiFeedbackPulse _focusPulse;
        private RuntimeUiFeedbackPulse _pressPulse;
        private RuntimeUiFeedbackPulse _selectionPulse;
        private RuntimeUiFeedbackPulse _transitionPulse;
        private RuntimeUiFeedbackPulse _routeRevealPulse;
        private RuntimeUiPressTracker _pressTracker;
        private string _focusTarget = string.Empty;
        private string _pressTarget = string.Empty;
        private string _selectionTarget = string.Empty;
        private string _transitionTarget = string.Empty;
        private bool _wasTransitioning;
        private bool _presenterCommandActive;
        private string _selectedEquipmentId = string.Empty;
        private string _selectedCultivationId = string.Empty;
        private PlayerProgressionCommandResult _lastProgressionResult;
        private readonly HubPageRouter _hubRouter = new HubPageRouter();

        private const string StartFeedbackTarget = "start";
        private const int Orchard01ControlId = 1101;
        private const int Orchard02ControlId = 1102;
        private const int Orchard03ControlId = 1103;
        private const int StartControlId = 1104;
        private const int HomeNavigationControlId = 1201;
        private const int ActivityNavigationControlId = 1202;
        private const int GrowthNavigationControlId = 1203;
        private const int EquipmentNavigationControlId = 1211;
        private const int CultivationNavigationControlId = 1212;
        private const int ActivityClaimControlId = 1301;
        private const int EquipmentEntryControlId = 1401;
        private const int CultivationEntryControlId = 1402;
        private const int EquipmentPrimaryActionControlId = 1410;
        private const int CultivationPrimaryActionControlId = 1411;

        public ShellFlowError LastError { get; private set; }
        public string LastSessionId { get; private set; } = string.Empty;
        public int LastSeed { get; private set; }
        public string LastContentVersion { get; private set; } = string.Empty;
        public string SelectedLevelId => _visibleSelectedLevelId;
        public HubPageId CurrentPage => _hubRouter.CurrentPage;
        public GrowthPageId CurrentGrowthPage => _hubRouter.CurrentGrowthPage;
        public string SelectedEquipmentId => _selectedEquipmentId;
        public string SelectedCultivationId => _selectedCultivationId;
        public PlayerProgressionCommandResult LastProgressionResult =>
            _lastProgressionResult;

        public void Initialize(IShellFlowContext context, RuntimeUiTheme runtimeUiTheme)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!(context is ILevelSelectionFlowContext selection))
                throw new ArgumentException(
                    "Lobby requires a level-selection flow context.", nameof(context));
            if (!(context is IHubProgressionReadContext hubRead)
                || hubRead.OutgameContent == null
                || hubRead.Progression == null)
            {
                throw new ArgumentException(
                    "Lobby requires a valid Hub progression read context.",
                    nameof(context));
            }
            if (!(context is IHubProgressionCommandContext hubCommands))
            {
                throw new ArgumentException(
                    "Lobby requires a Hub progression command context.",
                    nameof(context));
            }
            if (runtimeUiTheme == null)
                throw new ArgumentNullException(nameof(runtimeUiTheme));
            var validation = runtimeUiTheme.Validate();
            if (!validation.IsValid)
                throw new ArgumentException(validation.Issues[0].ToString(), nameof(runtimeUiTheme));

            _context = context;
            _hubRead = hubRead;
            _hubCommands = hubCommands;
            _runtimeUiTheme = runtimeUiTheme;
            _drawContext = null;
            _visibleSelectedLevelId = selection.SelectedLevelId ?? string.Empty;
            _selectedEquipmentId = hubRead.OutgameContent.GrowthEquipment.Keys
                .OrderBy(value => value, StringComparer.Ordinal)
                .FirstOrDefault() ?? string.Empty;
            _selectedCultivationId = hubRead.OutgameContent.CultivationNodes.Keys
                .OrderBy(value => value, StringComparer.Ordinal)
                .FirstOrDefault() ?? string.Empty;
            _lastProgressionResult = null;
            _presenterCommandActive = false;
            LastError = ShellFlowError.None;
            _focusPulse = default;
            _pressPulse = default;
            _selectionPulse = default;
            _transitionPulse = default;
            _routeRevealPulse = RuntimeUiMotion.BeginReveal(Time.unscaledTime,
                runtimeUiTheme.Feedback, 5);
            _pressTracker.Cancel();
            _focusTarget = string.Empty;
            _pressTarget = string.Empty;
            _selectionTarget = string.Empty;
            _transitionTarget = string.Empty;
            _wasTransitioning = context.Navigator == null
                || context.Navigator.TransitionState != AppTransitionState.Idle;
        }

        private void OnDisable()
        {
            CancelPressOwner();
            _routeRevealPulse = default;
            _presenterCommandActive = false;
        }

        public bool TrySelectLevel(string levelId)
        {
            if (_hubRouter.CurrentPage != HubPageId.Home) return false;
            if (_context == null)
                return Fail(new ShellFlowError(MissingContext));
            if (_context.Navigator == null)
                return Fail(new ShellFlowError(MissingNavigator));
            if (_context.Navigator.TransitionState != AppTransitionState.Idle)
                return false;
            if (_presenterCommandActive || _hubCommands.ProgressionCommandInProgress)
                return Fail(new ShellFlowError(ProgressionCommandActive));

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
            if (_hubRouter.CurrentPage != HubPageId.Home) return false;
            if (_context == null)
                return Fail(new ShellFlowError(MissingContext));
            if (_context.Navigator == null)
                return Fail(new ShellFlowError(MissingNavigator));
            if (_context.Navigator.TransitionState != AppTransitionState.Idle)
                return false;

            if (_presenterCommandActive || _hubCommands.ProgressionCommandInProgress)
                return Fail(new ShellFlowError(ProgressionCommandActive));

            var selectedLevelId = _visibleSelectedLevelId;
            if (string.IsNullOrWhiteSpace(selectedLevelId))
                return Fail(new ShellFlowError(MissingSelectedLevel));
            if (!IsPlayable((ILevelSelectionFlowContext)_context, selectedLevelId))
                return Fail(new ShellFlowError(MissingSelectedLevel, selectedLevelId));
            if (!_hubRead.CurrentGrowthPreview.Succeeded
                && !_hubRead.TryRefreshSelectedGrowthPreview(out var preview))
            {
                return Fail(new ShellFlowError(GrowthPreviewUnavailable,
                    preview.Code + ":" + preview.Path));
            }

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

        public bool TrySelectHubPage(HubPageId page)
        {
            if (IsAppTransitioning()) return false;
            if (!_hubRouter.TrySelectPage(page)) return false;
            CancelPressOwner();
            BeginPageReveal(Time.unscaledTime);
            return true;
        }

        public bool TrySelectGrowthPage(GrowthPageId page)
        {
            if (IsAppTransitioning() || _hubRouter.CurrentPage != HubPageId.Growth)
                return false;
            if (!_hubRouter.TrySelectGrowthPage(page)) return false;
            CancelPressOwner();
            BeginPageReveal(Time.unscaledTime);
            return true;
        }

        public bool TrySelectEquipment(string growthEquipmentId)
        {
            if (_hubRouter.CurrentPage != HubPageId.Growth
                || _hubRouter.CurrentGrowthPage != GrowthPageId.Equipment
                || !_hubRead.OutgameContent.GrowthEquipment.ContainsKey(
                    growthEquipmentId ?? string.Empty))
                return false;
            _selectedEquipmentId = growthEquipmentId;
            return true;
        }

        public bool TrySelectCultivation(string cultivationNodeId)
        {
            if (_hubRouter.CurrentPage != HubPageId.Growth
                || _hubRouter.CurrentGrowthPage != GrowthPageId.Cultivation
                || !_hubRead.OutgameContent.CultivationNodes.ContainsKey(
                    cultivationNodeId ?? string.Empty))
                return false;
            _selectedCultivationId = cultivationNodeId;
            return true;
        }

        public bool TryClaimStarterActivity()
        {
            var activity = ActivityHubPageModel.SelectPrimaryActivity(
                _hubRead.OutgameContent);
            if (activity == null) return false;
            var state = ActivityHubPageModel.ResolveState(activity,
                _hubRead.Progression,
                CommandBusy, _lastProgressionResult);
            if (state != HubActivityState.Claimable
                && state != HubActivityState.Error)
                return false;
            return BeginProgressionCommand(_hubCommands.TryClaimActivity(
                activity.id, OnProgressionCommandCompleted));
        }

        public bool TryEquipSelectedEquipment()
        {
            if (!_hubRead.OutgameContent.GrowthEquipment.TryGetValue(
                    _selectedEquipmentId, out var definition)) return false;
            var state = GrowthHubPageModel.ResolveEquipmentEligibility(
                definition, _hubRead.Progression, CommandBusy);
            if (state != HubGrowthState.Owned)
                return false;
            return BeginProgressionCommand(
                _hubCommands.TryEquipGrowthEquipment(definition.id,
                    definition.slotId, OnProgressionCommandCompleted));
        }

        public bool TryUpgradeSelectedEquipment()
        {
            if (!_hubRead.OutgameContent.GrowthEquipment.TryGetValue(
                    _selectedEquipmentId, out var definition)) return false;
            var state = GrowthHubPageModel.ResolveEquipmentEligibility(
                definition, _hubRead.Progression, CommandBusy);
            if (state != HubGrowthState.Upgradeable)
                return false;
            return BeginProgressionCommand(
                _hubCommands.TryUpgradeGrowthEquipment(definition.id,
                    OnProgressionCommandCompleted));
        }

        public bool TryUpgradeSelectedCultivation()
        {
            if (!_hubRead.OutgameContent.CultivationNodes.TryGetValue(
                    _selectedCultivationId, out var definition)) return false;
            var state = GrowthHubPageModel.ResolveCultivationEligibility(
                definition, _hubRead.Progression, CommandBusy);
            if (state != HubGrowthState.Upgradeable)
                return false;
            return BeginProgressionCommand(
                _hubCommands.TryUpgradeCultivation(definition.id,
                    OnProgressionCommandCompleted));
        }

        private bool CommandBusy => _presenterCommandActive
            || _hubCommands.ProgressionCommandInProgress;

        private bool BeginProgressionCommand(IEnumerator routine)
        {
            if (routine == null || CommandBusy) return false;
            _presenterCommandActive = true;
            _lastProgressionResult = null;
            CancelPressOwner();
            StartCoroutine(RunProgressionCommand(routine));
            return true;
        }

        private IEnumerator RunProgressionCommand(IEnumerator routine)
        {
            try
            {
                yield return routine;
            }
            finally
            {
                _presenterCommandActive = false;
            }
        }

        private void OnProgressionCommandCompleted(
            PlayerProgressionCommandResult result)
        {
            _lastProgressionResult = result;
            if (result != null && result.Succeeded)
            {
                _hubRead.TryRefreshSelectedGrowthPreview(out _);
                LastError = ShellFlowError.None;
            }
        }

        public bool TryActivateAt(Vector2 guiPoint, float viewportWidth, float viewportHeight, Rect safeArea)
        {
            var layout = PortraitHubLayout.Create(viewportWidth, viewportHeight, safeArea);
            var transitioning = IsAppTransitioning();
            switch (PortraitHubLayout.HitTest(layout, guiPoint,
                        _hubRouter.CurrentPage, transitioning,
                        _hubRouter.CurrentGrowthPage))
            {
                case HubHitTarget.Home: return TrySelectHubPage(HubPageId.Home);
                case HubHitTarget.Activity: return TrySelectHubPage(HubPageId.Activity);
                case HubHitTarget.Growth: return TrySelectHubPage(HubPageId.Growth);
                case HubHitTarget.Equipment:
                    return TrySelectGrowthPage(GrowthPageId.Equipment);
                case HubHitTarget.Cultivation:
                    return TrySelectGrowthPage(GrowthPageId.Cultivation);
                case HubHitTarget.LevelOrchard01: return TrySelectLevel(Orchard01LevelId);
                case HubHitTarget.LevelOrchard02: return TrySelectLevel(Orchard02LevelId);
                case HubHitTarget.LevelOrchard03: return TrySelectLevel(Orchard03LevelId);
                case HubHitTarget.Start: return TryStart();
                case HubHitTarget.ActivityClaim:
                    return TryClaimStarterActivity();
                case HubHitTarget.EquipmentEntry:
                    return TrySelectEquipment(_selectedEquipmentId);
                case HubHitTarget.CultivationEntry:
                    return TrySelectCultivation(_selectedCultivationId);
                case HubHitTarget.GrowthPrimaryAction:
                    return TryActivateGrowthPrimaryAction();
                default: return false;
            }
        }

        private bool TryActivateGrowthPrimaryAction()
        {
            if (_hubRouter.CurrentGrowthPage == GrowthPageId.Cultivation)
                return TryUpgradeSelectedCultivation();
            if (!_hubRead.Progression.TryGetEquipped(
                    OutgameContentIds.GrowthSlots.Offense,
                    out var equipped)
                || !string.Equals(equipped, _selectedEquipmentId,
                    StringComparison.Ordinal))
                return TryEquipSelectedEquipment();
            return TryUpgradeSelectedEquipment();
        }

        private void OnGUI()
        {
            if (_runtimeUiTheme == null) return;
            var unscaledTime = Time.unscaledTime;
            var pointer = RuntimeUiPointerSample.FromEvent(Event.current);
            var layout = PortraitHubLayout.Create(
                Screen.width, Screen.height, RuntimeSafeAreaResolver.ResolveCurrent());
            _drawContext = RuntimeUiGui.RequireContext(
                _drawContext, _runtimeUiTheme, layout.Frame.Scale);

            RuntimeUiGui.DrawHubScreenBackground(_drawContext,
                new Rect(0f, 0f, Screen.width, Screen.height));
            RuntimeUiGui.DrawScreenCorners(_drawContext, layout.Frame.SafeArea);

            var transitioning = IsAppTransitioning();
            RefreshTransitionFeedback(transitioning, unscaledTime);
            if (transitioning) CancelPressOwner();

            var topBarCopy = RuntimeUiCopyCatalog.Get(TitleCopyFor(
                _hubRouter.CurrentPage));
            RuntimeUiGui.DrawHubTopBarWithBalance(_drawContext, layout.TopBar,
                layout.TopBarContent.Title,
                layout.TopBarContent.ResourceBalance,
                topBarCopy.Text,
                RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.HubResourceMorningDew).Text,
                _hubRead.Progression.ItemQuantity(
                    OutgameContentIds.Items.MorningDew).ToString(),
                transitioning
                    ? RuntimeUiInteractionState.Loading
                    : RuntimeUiInteractionState.Normal);
            RuntimeUiGui.DrawHubPageSurface(_drawContext, layout.PageSurface,
                transitioning
                    ? RuntimeUiInteractionState.Loading
                    : RuntimeUiInteractionState.Normal);

            switch (_hubRouter.CurrentPage)
            {
                case HubPageId.Home:
                    DrawHomePage(layout.HomePage, transitioning,
                        unscaledTime, pointer);
                    break;
                case HubPageId.Activity:
                    DrawActivityPage(layout.ActivityPage, transitioning,
                        pointer);
                    break;
                case HubPageId.Growth:
                    DrawGrowthPage(layout.GrowthPage, transitioning,
                        pointer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            RuntimeUiGui.DrawHubNavigationTray(_drawContext,
                layout.NavigationTray, transitioning
                    ? RuntimeUiInteractionState.Loading
                    : RuntimeUiInteractionState.Normal);
            DrawPrimaryNavigation(layout.PrimaryNavigation, transitioning,
                pointer);
        }

        private void DrawHomePage(HubHomePageLayout layout, bool transitioning,
            float unscaledTime, RuntimeUiPointerSample pointer)
        {
            var transitionEmphasized = _transitionPulse.IsActive(unscaledTime);
            DrawLevelCard(layout.Orchard01Card, Orchard01LevelId,
                RuntimeUiCopyId.LobbyOrchard01Title,
                RuntimeUiCopyId.LobbyOrchard01Body, transitioning,
                unscaledTime, RuntimeUiLobbyThumbnail.Orchard01,
                1, pointer);
            DrawLevelCard(layout.Orchard02Card, Orchard02LevelId,
                RuntimeUiCopyId.LobbyOrchard02Title,
                RuntimeUiCopyId.LobbyOrchard02Body, transitioning,
                unscaledTime, RuntimeUiLobbyThumbnail.Orchard02,
                2, pointer);
            DrawLevelCard(layout.Orchard03Card, Orchard03LevelId,
                RuntimeUiCopyId.LobbyOrchard03Title,
                RuntimeUiCopyId.LobbyOrchard03Body, transitioning,
                unscaledTime, RuntimeUiLobbyThumbnail.Orchard03,
                3, pointer);

            var previewTitle = RuntimeUiCopyCatalog.Get(
                RuntimeUiCopyId.HubHomeGrowthPreviewTitle);
            var preview = _hubRead.CurrentGrowthPreview;
            var previewState = HomeHubPageModel.ResolvePreviewState(
                preview, transitioning);
            RuntimeUiGui.DrawHubHomeGrowthPreview(_drawContext,
                layout.GrowthPreview, previewTitle.Text,
                HomeHubPageModel.FormatPreview(preview,
                    _hubRead.OutgameContent),
                previewState);

            var selectedPlayable = IsPlayable(
                (ILevelSelectionFlowContext)_context, _visibleSelectedLevelId);
            var startAvailable = !transitioning && selectedPlayable
                && preview.Succeeded && !CommandBusy;
            var startPress = _pressTracker.Update(StartControlId, layout.StartButton,
                startAvailable, pointer, _runtimeUiTheme.Feedback.DragCancelDistance);
            var startHovered = startPress.Hovered;
            if (startHovered)
                BeginFocus(StartFeedbackTarget, unscaledTime);
            var startPressed = startPress.Pressed;
            var startState = ResolveActionState(
                transitioning || CommandBusy,
                selectedPlayable && preview.Succeeded,
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
                startCopy.Text,
                new RuntimeUiActionSpec(RuntimeUiActionKind.Primary,
                    RuntimeUiActionContentForm.Text,
                    RuntimeUiActionBehavior.Instantaneous), startState,
                null,
                transitioning
                    ? startCopy.Role
                    : RuntimeUiTypographyRole.SectionTitle,
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

        }

        private void DrawActivityPage(HubActivityPageLayout layout,
            bool transitioning, RuntimeUiPointerSample pointer)
        {
            var activity = ActivityHubPageModel.SelectPrimaryActivity(
                _hubRead.OutgameContent);
            if (activity == null)
            {
                RuntimeUiGui.DrawHubActivityBanner(_drawContext, layout.Title,
                    RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.HubUnavailableTitle).Text,
                    RuntimeUiInteractionState.Disabled);
                DrawTwoLines(layout.Description,
                    RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.HubActivityUnavailableBody).Text,
                    RuntimeUiTextTone.Secondary,
                    RuntimeUiInteractionState.Disabled);
                RuntimeUiGui.DrawHubActivityStatus(_drawContext, layout.Status,
                    layout.StateIndicator,
                    RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.HubActivityLocked).Text,
                    RuntimeUiInteractionState.Disabled);
                return;
            }

            var activityState = transitioning
                ? HubActivityState.Claiming
                : ActivityHubPageModel.ResolveState(activity,
                    _hubRead.Progression,
                    CommandBusy, _lastProgressionResult);
            var visualState = ActivityHubPageModel.VisualState(activityState);
            RuntimeUiGui.DrawHubActivityBanner(_drawContext, layout.Title,
                activity.displayName, visualState);
            DrawTwoLines(layout.Description, activity.description,
                RuntimeUiTextTone.Secondary, visualState);

            RuntimeUiGui.DrawHubActivityRewardIllustration(_drawContext,
                layout.Illustration);
            RuntimeUiGui.DrawIllustrationFrame(_drawContext,
                layout.Illustration);

            RuntimeUiGui.DrawHubRewardPanel(_drawContext, layout.RewardPanel,
                visualState);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.RewardTitle,
                RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.HubActivityRewardTitle).Text,
                RuntimeUiTypographyRole.SectionTitle,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleLeft, visualState);
            var rewards = ActivityHubPageModel.ResolveRewards(activity,
                _hubRead.OutgameContent);
            RuntimeUiGui.DrawHubRewardTile(_drawContext,
                layout.RewardEquipment, RuntimeUiArtSlot.IconHubGrowth,
                rewards.Equipment, visualState);
            RuntimeUiGui.DrawHubRewardTile(_drawContext, layout.RewardItem,
                RuntimeUiArtSlot.IconResourceCore, rewards.Item, visualState);

            var statusCopy = RuntimeUiCopyCatalog.Get(
                ActivityHubPageModel.StatusCopy(activityState));
            RuntimeUiGui.DrawHubActivityStatus(_drawContext, layout.Status,
                layout.StateIndicator, statusCopy.Text, visualState);

            var actionEnabled = !transitioning
                && (activityState == HubActivityState.Claimable
                    || activityState == HubActivityState.Error);
            var press = _pressTracker.Update(ActivityClaimControlId,
                layout.PrimaryAction, actionEnabled, pointer,
                _runtimeUiTheme.Feedback.DragCancelDistance);
            var actionState = activityState == HubActivityState.Claiming
                ? RuntimeUiInteractionState.Loading
                : activityState == HubActivityState.Claimed
                    ? RuntimeUiInteractionState.Success
                    : activityState == HubActivityState.Locked
                        ? RuntimeUiInteractionState.Disabled
                        : activityState == HubActivityState.Error
                            ? RuntimeUiInteractionState.Error
                            : press.Pressed
                                ? RuntimeUiInteractionState.Pressed
                                : press.Hovered
                                    ? RuntimeUiInteractionState.HoveredOrFocused
                                    : RuntimeUiInteractionState.Normal;
            var activityActionCopy = RuntimeUiCopyCatalog.Get(
                ActivityHubPageModel.ActionCopy(activityState));
            RuntimeUiGui.DrawActionVisual(_drawContext, layout.PrimaryAction,
                activityActionCopy.Text,
                new RuntimeUiActionSpec(RuntimeUiActionKind.Primary,
                    RuntimeUiActionContentForm.Text,
                    RuntimeUiActionBehavior.Instantaneous), actionState,
                null,
                activityState == HubActivityState.Claimable
                    ? RuntimeUiTypographyRole.SectionTitle
                    : activityActionCopy.Role);
            if (press.Activated) TryClaimStarterActivity();
        }

        private void DrawGrowthPage(HubGrowthPageLayout layout,
            bool transitioning, RuntimeUiPointerSample pointer)
        {
            DrawGrowthNavigationItem(layout.Navigation.Equipment,
                GrowthPageId.Equipment, EquipmentNavigationControlId,
                RuntimeUiCopyId.HubGrowthEquipmentTab,
                transitioning, pointer);
            DrawGrowthNavigationItem(layout.Navigation.Cultivation,
                GrowthPageId.Cultivation, CultivationNavigationControlId,
                RuntimeUiCopyId.HubGrowthCultivationTab,
                transitioning, pointer);

            if (_hubRouter.CurrentGrowthPage == GrowthPageId.Equipment)
                DrawEquipment(layout, transitioning, pointer);
            else
                DrawCultivation(layout, transitioning, pointer);
        }

        private void DrawEquipment(HubGrowthPageLayout layout,
            bool transitioning, RuntimeUiPointerSample pointer)
        {
            if (!_hubRead.OutgameContent.GrowthEquipment.TryGetValue(
                    _selectedEquipmentId, out var definition)) return;
            var state = transitioning
                ? HubGrowthState.Loading
                : GrowthHubPageModel.ResolveEquipmentState(definition,
                    _hubRead.Progression,
                    CommandBusy, _lastProgressionResult);
            var eligibilityState = transitioning
                ? HubGrowthState.Loading
                : GrowthHubPageModel.ResolveEquipmentEligibility(definition,
                    _hubRead.Progression, CommandBusy);
            var visualState = GrowthHubPageModel.VisualState(state);
            var entryPress = _pressTracker.Update(EquipmentEntryControlId,
                layout.EntryCard, !transitioning, pointer,
                _runtimeUiTheme.Feedback.DragCancelDistance);
            RuntimeUiGui.DrawHubGrowthEntry(_drawContext, layout.EntryCard,
                true, entryPress.Pressed
                    ? RuntimeUiInteractionState.Pressed : visualState);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.EntryTitle,
                definition.displayName, RuntimeUiTypographyRole.SectionTitle,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleLeft, visualState);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.EntryStatus,
                RuntimeUiCopyCatalog.Get(
                    GrowthHubPageModel.StatusCopy(state, false)).Text,
                RuntimeUiTypographyRole.ControlLabel, RuntimeUiTextTone.State,
                TextAnchor.MiddleLeft, visualState);
            if (entryPress.Activated) TrySelectEquipment(definition.id);

            RuntimeUiGui.DrawHubGrowthDetail(_drawContext, layout.DetailPanel,
                visualState);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.DetailTitle,
                definition.displayName, RuntimeUiTypographyRole.SectionTitle,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleLeft, visualState);
            DrawTwoLines(layout.Description, definition.description,
                RuntimeUiTextTone.Secondary, visualState);
            _hubRead.Progression.TryGetGrowthEquipmentRank(definition.id,
                out var rank);
            var maximumRank = GrowthHubPageModel.MaximumRank(definition);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.Rank,
                RuntimeUiCopyCatalog.FormatHubRank(rank, maximumRank),
                RuntimeUiTypographyRole.Body, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleLeft, visualState);
            var shownRank = GrowthHubPageModel.FindEquipmentRank(definition,
                Mathf.Min(rank + 1, maximumRank))
                ?? GrowthHubPageModel.FindEquipmentRank(definition, rank);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.Effect,
                GrowthHubPageModel.FormatContribution(
                    shownRank?.contributions),
                RuntimeUiTypographyRole.Body, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleLeft, visualState);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.Cost,
                GrowthHubPageModel.FormatCost(shownRank?.costs,
                    _hubRead.OutgameContent, _hubRead.Progression),
                RuntimeUiTypographyRole.Body,
                RuntimeUiTextTone.Secondary, TextAnchor.MiddleLeft, visualState);
            DrawGrowthStatusAndAction(layout, state, eligibilityState, false,
                definition.slotId, transitioning, pointer);
        }

        private void DrawCultivation(HubGrowthPageLayout layout,
            bool transitioning, RuntimeUiPointerSample pointer)
        {
            if (!_hubRead.OutgameContent.CultivationNodes.TryGetValue(
                    _selectedCultivationId, out var definition)) return;
            var state = transitioning
                ? HubGrowthState.Loading
                : GrowthHubPageModel.ResolveCultivationState(definition,
                    _hubRead.Progression,
                    CommandBusy, _lastProgressionResult);
            var eligibilityState = transitioning
                ? HubGrowthState.Loading
                : GrowthHubPageModel.ResolveCultivationEligibility(definition,
                    _hubRead.Progression, CommandBusy);
            var visualState = GrowthHubPageModel.VisualState(state);
            var entryPress = _pressTracker.Update(CultivationEntryControlId,
                layout.EntryCard, !transitioning, pointer,
                _runtimeUiTheme.Feedback.DragCancelDistance);
            RuntimeUiGui.DrawHubGrowthEntry(_drawContext, layout.EntryCard,
                true, entryPress.Pressed
                    ? RuntimeUiInteractionState.Pressed : visualState);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.EntryTitle,
                definition.displayName, RuntimeUiTypographyRole.SectionTitle,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleLeft, visualState);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.EntryStatus,
                RuntimeUiCopyCatalog.Get(
                    GrowthHubPageModel.StatusCopy(state, true)).Text,
                RuntimeUiTypographyRole.ControlLabel, RuntimeUiTextTone.State,
                TextAnchor.MiddleLeft, visualState);
            if (entryPress.Activated) TrySelectCultivation(definition.id);

            RuntimeUiGui.DrawHubGrowthDetail(_drawContext, layout.DetailPanel,
                visualState);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.DetailTitle,
                definition.displayName, RuntimeUiTypographyRole.SectionTitle,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleLeft, visualState);
            DrawTwoLines(layout.Description, definition.description,
                RuntimeUiTextTone.Secondary, visualState);
            var rank = _hubRead.Progression.CultivationRank(definition.id);
            var maximumRank = GrowthHubPageModel.MaximumRank(definition);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.Rank,
                RuntimeUiCopyCatalog.FormatHubRank(rank, maximumRank),
                RuntimeUiTypographyRole.Body, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleLeft, visualState);
            var shownRank = GrowthHubPageModel.FindCultivationRank(definition,
                Mathf.Min(rank + 1, maximumRank))
                ?? GrowthHubPageModel.FindCultivationRank(definition, rank);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.Effect,
                GrowthHubPageModel.FormatContribution(
                    shownRank?.contributions),
                RuntimeUiTypographyRole.Body, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleLeft, visualState);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.Cost,
                state == HubGrowthState.Locked
                    ? GrowthHubPageModel.FormatPrerequisite(definition,
                        _hubRead.OutgameContent, _hubRead.Progression)
                    : GrowthHubPageModel.FormatCost(shownRank?.costs,
                        _hubRead.OutgameContent, _hubRead.Progression),
                RuntimeUiTypographyRole.Body,
                RuntimeUiTextTone.Secondary, TextAnchor.MiddleLeft, visualState);
            DrawGrowthStatusAndAction(layout, state, eligibilityState, true,
                string.Empty, transitioning, pointer);
        }

        private void DrawGrowthStatusAndAction(HubGrowthPageLayout layout,
            HubGrowthState state, HubGrowthState eligibilityState,
            bool cultivation, string slotId, bool transitioning,
            RuntimeUiPointerSample pointer)
        {
            var visualState = GrowthHubPageModel.VisualState(state);
            RuntimeUiGui.DrawSingleLineText(_drawContext, layout.Status,
                RuntimeUiCopyCatalog.Get(
                    GrowthHubPageModel.StatusCopy(state, cultivation)).Text,
                RuntimeUiTypographyRole.ControlLabel,
                RuntimeUiTextTone.State, TextAnchor.MiddleLeft, visualState);

            var equipped = !cultivation
                && _hubRead.Progression.TryGetEquipped(slotId, out var equipmentId)
                && string.Equals(equipmentId, _selectedEquipmentId,
                    StringComparison.Ordinal);
            var action = GrowthHubPageModel.ResolvePrimaryAction(
                cultivation ? GrowthPageId.Cultivation
                    : GrowthPageId.Equipment,
                eligibilityState, equipped);
            var actionEnabled = !transitioning
                && action != HubGrowthPrimaryAction.None;
            var eligibilityVisualState = GrowthHubPageModel.VisualState(
                eligibilityState);
            var actionRect = layout.PrimaryActionFor(cultivation
                ? GrowthPageId.Cultivation : GrowthPageId.Equipment);
            var controlId = cultivation ? CultivationPrimaryActionControlId
                : EquipmentPrimaryActionControlId;
            var press = _pressTracker.Update(controlId,
                actionRect, actionEnabled, pointer,
                _runtimeUiTheme.Feedback.DragCancelDistance);
            var actionState = actionEnabled
                ? press.Pressed ? RuntimeUiInteractionState.Pressed
                    : press.Hovered
                        ? RuntimeUiInteractionState.HoveredOrFocused
                        : RuntimeUiInteractionState.Normal
                : eligibilityVisualState == RuntimeUiInteractionState.Error
                    ? RuntimeUiInteractionState.Error
                    : eligibilityVisualState == RuntimeUiInteractionState.Loading
                        ? RuntimeUiInteractionState.Loading
                        : eligibilityVisualState == RuntimeUiInteractionState.Success
                            ? RuntimeUiInteractionState.Success
                            : RuntimeUiInteractionState.Disabled;
            RuntimeUiGui.DrawActionVisual(_drawContext, actionRect,
                RuntimeUiCopyCatalog.Get(
                    GrowthHubPageModel.ActionCopy(
                        action, eligibilityState, cultivation)).Text,
                new RuntimeUiActionSpec(RuntimeUiActionKind.Primary,
                    RuntimeUiActionContentForm.Text,
                    RuntimeUiActionBehavior.Instantaneous), actionState,
                null,
                RuntimeUiTypographyRole.ControlLabel);
            RuntimeUiGui.DrawStateIndicator(_drawContext,
                layout.StateIndicator, visualState);
            if (press.Activated) TryActivateGrowthPrimaryAction();
        }

        private void DrawTwoLines(Rect rect, string text,
            RuntimeUiTextTone tone, RuntimeUiInteractionState state)
        {
            var layout = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                _drawContext, rect, RuntimeUiTypographyRole.Body,
                TextAnchor.MiddleLeft, state);
            var lines = RuntimeUiGui.ResolveStatusTextLines(layout, text);
            RuntimeUiGui.DrawControlledTwoLineText(_drawContext, rect, lines,
                RuntimeUiTypographyRole.Body, tone,
                TextAnchor.MiddleLeft, state);
        }

        private void DrawPrimaryNavigation(HubPrimaryNavigationLayout layout,
            bool transitioning, RuntimeUiPointerSample pointer)
        {
            DrawPrimaryNavigationItem(layout.Home, HubPageId.Home,
                HomeNavigationControlId, RuntimeUiCopyId.HubNavHome,
                transitioning, pointer);
            DrawPrimaryNavigationItem(layout.Activity, HubPageId.Activity,
                ActivityNavigationControlId, RuntimeUiCopyId.HubNavActivity,
                transitioning, pointer);
            DrawPrimaryNavigationItem(layout.Growth, HubPageId.Growth,
                GrowthNavigationControlId, RuntimeUiCopyId.HubNavGrowth,
                transitioning, pointer);
        }

        private void DrawPrimaryNavigationItem(Rect rect, HubPageId page,
            int controlId, RuntimeUiCopyId copyId, bool transitioning,
            RuntimeUiPointerSample pointer)
        {
            var press = _pressTracker.Update(controlId, rect, !transitioning,
                pointer, _runtimeUiTheme.Feedback.DragCancelDistance);
            var selected = _hubRouter.CurrentPage == page;
            var state = ResolveNavigationState(transitioning, selected,
                press.Hovered, press.Pressed);
            RuntimeUiGui.DrawHubNavigationItem(_drawContext, rect,
                PrimaryNavigationIconFor(page),
                RuntimeUiCopyCatalog.Get(copyId).Text, selected, state);
            if (press.Activated)
                TrySelectHubPage(page);
        }

        private void DrawGrowthNavigationItem(Rect rect, GrowthPageId page,
            int controlId, RuntimeUiCopyId copyId, bool transitioning,
            RuntimeUiPointerSample pointer)
        {
            var press = _pressTracker.Update(controlId, rect, !transitioning,
                pointer, _runtimeUiTheme.Feedback.DragCancelDistance);
            var selected = _hubRouter.CurrentGrowthPage == page;
            var state = ResolveNavigationState(transitioning, selected,
                press.Hovered, press.Pressed);
            RuntimeUiGui.DrawHubGrowthTab(_drawContext, rect,
                RuntimeUiCopyCatalog.Get(copyId).Text, selected, state);
            if (press.Activated)
                TrySelectGrowthPage(page);
        }

        private bool IsAppTransitioning()
        {
            return _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
        }

        private void CancelPressOwner()
        {
            _pressTracker.Cancel();
        }

        private void BeginPageReveal(float unscaledTime)
        {
            _routeRevealPulse = RuntimeUiMotion.BeginReveal(unscaledTime,
                _runtimeUiTheme.Feedback, 5);
        }

        private static RuntimeUiCopyId TitleCopyFor(HubPageId page)
        {
            switch (page)
            {
                case HubPageId.Home: return RuntimeUiCopyId.HubHomeTitle;
                case HubPageId.Activity: return RuntimeUiCopyId.HubActivityTitle;
                case HubPageId.Growth: return RuntimeUiCopyId.HubGrowthTitle;
                default: throw new ArgumentOutOfRangeException(nameof(page), page, null);
            }
        }

        private static RuntimeUiArtSlot PrimaryNavigationIconFor(HubPageId page)
        {
            switch (page)
            {
                case HubPageId.Home: return RuntimeUiArtSlot.IconHubHome;
                case HubPageId.Activity: return RuntimeUiArtSlot.IconHubActivity;
                case HubPageId.Growth: return RuntimeUiArtSlot.IconHubGrowth;
                default: throw new ArgumentOutOfRangeException(nameof(page), page, null);
            }
        }

        internal static RuntimeUiInteractionState ResolveNavigationState(
            bool transitioning, bool selected, bool pointerInside,
            bool pointerPressed)
        {
            if (transitioning) return RuntimeUiInteractionState.Loading;
            if (pointerPressed) return RuntimeUiInteractionState.Pressed;
            if (selected) return RuntimeUiInteractionState.Selected;
            return pointerInside
                ? RuntimeUiInteractionState.HoveredOrFocused
                : RuntimeUiInteractionState.Normal;
        }

        private void DrawLevelCard(Rect rect, string levelId,
            RuntimeUiCopyId titleCopyId, RuntimeUiCopyId bodyCopyId,
            bool transitioning, float unscaledTime,
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
            RuntimeUiGui.DrawHubLevelCardSurface(_drawContext, rect, selected,
                state, motion);
            var previousColor = GUI.color;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b,
                previousColor.a * motion.Alpha);

            var cardLayout = PortraitHubLayout.CreateHomeLevelCard(
                visualRect, _drawContext.Scale);
            RuntimeUiGui.DrawHubLevelIllustration(_drawContext,
                cardLayout.Thumbnail, thumbnail);
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
