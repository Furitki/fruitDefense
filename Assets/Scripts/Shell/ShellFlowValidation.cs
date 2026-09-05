using System;
using System.Collections;
using System.Collections.Generic;
using FruitDefense.App;
using FruitDefense.App.Services;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Shell
{
    public static class ShellFlowValidation
    {
        public static void SmokeValidate(RuntimeUiTheme runtimeUiTheme)
        {
            Validate(runtimeUiTheme);
            Debug.Log("FRUIT_DEFENSE_SHELL_OK");
        }

        public static void Validate(RuntimeUiTheme runtimeUiTheme)
        {
            if (runtimeUiTheme == null)
                throw new ArgumentNullException(nameof(runtimeUiTheme));
            var validation = runtimeUiTheme.Validate();
            if (!validation.IsValid)
                throw new ArgumentException(validation.Issues[0].ToString(), nameof(runtimeUiTheme));

            ShellLayoutValidation.ValidateReferenceGeometry();
            ValidateHubNavigationIsolation();
            ValidateProfileStartupClassification();
            ValidateHubPageStateModels(runtimeUiTheme);
            ValidateHubPresenterLifecycle(runtimeUiTheme);
            ValidateLobbyVisualContract(runtimeUiTheme);
            ValidateThreeCardSelectionAndSelectedStart(runtimeUiTheme);
            ValidateUnavailableProfileRecovery(runtimeUiTheme);
            ValidateSettlementVisualContract(runtimeUiTheme);
            ValidateSettlementDisplay(runtimeUiTheme);
            ValidateReturnAndRetry(runtimeUiTheme);
            ValidateInvalidResultRecovery(runtimeUiTheme);
        }

        private static void ValidateHubNavigationIsolation()
        {
            IAppNavigator navigator = new AppNavigator();
            var routeChangedCount = 0;
            var transitionChangedCount = 0;
            navigator.RouteChanged += _ => routeChangedCount++;
            navigator.TransitionStateChanged += _ => transitionChangedCount++;

            var router = new HubPageRouter();
            Assert(router.CurrentPage == HubPageId.Home
                && router.CurrentGrowthPage == GrowthPageId.Equipment
                && router.Revision == 0,
                "Hub router starts on Home and Equipment with no transition history");
            Assert(!router.TrySelectPage(HubPageId.Home)
                && !router.TrySelectPage((HubPageId)(-1))
                && !router.TrySelectGrowthPage(GrowthPageId.Equipment)
                && !router.TrySelectGrowthPage((GrowthPageId)99)
                && router.Revision == 0,
                "Hub router rejects duplicate and undefined destinations");

            Assert(router.TrySelectPage(HubPageId.Activity)
                && router.CurrentPage == HubPageId.Activity
                && router.TrySelectPage(HubPageId.Growth)
                && router.CurrentPage == HubPageId.Growth
                && router.TrySelectGrowthPage(GrowthPageId.Cultivation)
                && router.CurrentGrowthPage == GrowthPageId.Cultivation,
                "Hub router accepts every finite primary and Growth destination");
            Assert(router.ResetToHome()
                && router.CurrentPage == HubPageId.Home
                && router.CurrentGrowthPage == GrowthPageId.Cultivation
                && router.Revision == 4
                && !router.ResetToHome(),
                "Hub reset selects Home while preserving Lobby-local Growth selection");

            Assert(navigator.CurrentRoute == AppRoute.Lobby
                && navigator.PendingRoute == AppRoute.Lobby
                && !navigator.HasPendingRoute
                && navigator.TransitionState == AppTransitionState.Idle
                && string.IsNullOrEmpty(navigator.LastError)
                && routeChangedCount == 0
                && transitionChangedCount == 0,
                "Hub page switches do not touch an independent idle AppNavigator");

            Assert(navigator.TryBeginTransition(AppRoute.Battle, out var errorCode)
                && string.IsNullOrEmpty(errorCode),
                "AppNavigator can independently enter its normal loading state");
            routeChangedCount = 0;
            transitionChangedCount = 0;
            Assert(router.TrySelectPage(HubPageId.Growth)
                && router.TrySelectGrowthPage(GrowthPageId.Equipment),
                "Hub navigation remains available as pure local state");
            Assert(navigator.CurrentRoute == AppRoute.Lobby
                && navigator.PendingRoute == AppRoute.Battle
                && navigator.HasPendingRoute
                && navigator.TransitionState == AppTransitionState.Loading
                && string.IsNullOrEmpty(navigator.LastError)
                && routeChangedCount == 0
                && transitionChangedCount == 0,
                "Hub page switches do not alter an independent loading AppNavigator");
        }

        private static void ValidateProfileStartupClassification()
        {
            var profile = PlayerProfile.CreateDefault();
            Assert(AppFlowCoordinator.ClassifyProfileLoad(
                        new ProfileLoadResult(ProfileLoadStatus.Success,
                            profile))
                    == ProfileStartupDisposition.Interactive
                && AppFlowCoordinator.ClassifyProfileLoad(
                        new ProfileLoadResult(ProfileLoadStatus.DefaultCreated,
                            profile))
                    == ProfileStartupDisposition.Interactive
                && AppFlowCoordinator.ClassifyProfileLoad(
                        new ProfileLoadResult(ProfileLoadStatus.StorageError,
                            profile, "primary unavailable"))
                    == ProfileStartupDisposition.Interactive,
                "profile startup accepts only explicit load results that carry a usable profile");
            Assert(AppFlowCoordinator.ClassifyProfileLoad(
                        new ProfileLoadResult(
                            ProfileLoadStatus.UnsupportedSchema, profile,
                            "schema newer than code"))
                    == ProfileStartupDisposition.UnsupportedSchema
                && AppFlowCoordinator.ClassifyProfileLoad(
                        new ProfileLoadResult(ProfileLoadStatus.StorageError,
                            null, "profile unavailable"))
                    == ProfileStartupDisposition.Unavailable
                && AppFlowCoordinator.ClassifyProfileLoad(null)
                    == ProfileStartupDisposition.Unavailable,
                "unsupported schema and unusable loads cannot silently create an interactive profile");
            Assert(!string.IsNullOrWhiteSpace(
                    AppFlowCoordinator.FormatBootstrapBlockingError(
                        AppFlowCoordinator.ProfileSchemaUnsupported))
                && !string.IsNullOrWhiteSpace(
                    AppFlowCoordinator.FormatBootstrapBlockingError(
                        AppFlowCoordinator.ProfileResetFailed)),
                "unsupported schema and reset failure own explicit recovery copy");
        }

        private static void ValidateHubPageStateModels(
            RuntimeUiTheme runtimeUiTheme)
        {
            if (!BundledGameContentLoader.TryLoadBundle(out var bundle,
                    out var validation))
                throw new InvalidOperationException(validation?.Issues[0].message
                    ?? "Bundled content is unavailable.");
            var catalog = bundle.Outgame;
            var activity = ActivityHubPageModel.SelectPrimaryActivity(catalog);
            var equipment = catalog.ResolveGrowthEquipment(
                OutgameContentIds.GrowthEquipment.SunleafEmblem);
            var cultivation = catalog.ResolveCultivationNode(
                OutgameContentIds.CultivationNodes.VitalRoots);
            var empty = CreateProgression(catalog, 0);
            var rewarded = CreateProgression(catalog, 6,
                equipmentRank: 0);
            var equipped = CreateProgression(catalog, 6,
                equipmentRank: 0, equipmentEquipped: true);
            var claimed = CreateProgression(catalog, 6,
                equipmentRank: 0, activityClaimed: true);

            Assert(activity != null
                && ActivityHubPageModel.ResolveState(activity, empty,
                    false, null) == HubActivityState.Claimable
                && ActivityHubPageModel.ResolveState(activity, empty,
                    true, null) == HubActivityState.Claiming
                && ActivityHubPageModel.ResolveState(activity, claimed,
                    false, null) == HubActivityState.Claimed
                && ActivityHubPageModel.ResolveState(activity, null,
                    false, null) == HubActivityState.InsufficientContext,
                "starter Activity resolves claimable, claiming, claimed, and insufficient-context from immutable progression state");
            var unavailable = new ActivityDefinitionDto
            {
                id = activity.id,
                receiptId = activity.receiptId,
                bundledAvailable = false,
            };
            var claimFailure = CommandResult(
                PlayerProgressionCommandKind.ClaimActivity,
                PlayerProgressionCommandStatus.PersistenceFailed,
                activity.id, empty);
            Assert(ActivityHubPageModel.ResolveState(unavailable, empty,
                        false, null) == HubActivityState.Locked
                && ActivityHubPageModel.ResolveState(activity, empty,
                        false, claimFailure) == HubActivityState.Error
                && ActivityHubPageModel.VisualState(HubActivityState.Available)
                    == RuntimeUiInteractionState.Normal
                && ActivityHubPageModel.VisualState(HubActivityState.Claiming)
                    == RuntimeUiInteractionState.Loading
                && ActivityHubPageModel.VisualState(HubActivityState.Claimed)
                    == RuntimeUiInteractionState.Success
                && ActivityHubPageModel.VisualState(HubActivityState.Locked)
                    == RuntimeUiInteractionState.Disabled
                && ActivityHubPageModel.VisualState(HubActivityState.Error)
                    == RuntimeUiInteractionState.Error,
                "Activity available, loading, claimed, locked, and error states have finite non-color visual semantics");
            var rewards = ActivityHubPageModel.ResolveRewards(activity, catalog);
            Assert(!string.IsNullOrWhiteSpace(rewards.Equipment)
                && !string.IsNullOrWhiteSpace(rewards.Item),
                "Activity reward model resolves catalog-backed equipment and item copy");

            Assert(GrowthHubPageModel.ResolveEquipmentState(equipment, empty,
                        false, null) == HubGrowthState.Locked
                && GrowthHubPageModel.ResolveEquipmentState(equipment,
                        rewarded, false, null) == HubGrowthState.Owned
                && GrowthHubPageModel.ResolveEquipmentState(equipment,
                        rewarded, true, null) == HubGrowthState.Loading
                && GrowthHubPageModel.ResolveEquipmentState(equipment,
                        equipped, false, null) == HubGrowthState.Upgradeable,
                "equipment state distinguishes locked, owned, loading, and upgradeable");
            var insufficientEquipment = CreateProgression(catalog, 0,
                equipmentRank: 0, equipmentEquipped: true);
            var maximumEquipment = CreateProgression(catalog, 0,
                equipmentRank: 2, equipmentEquipped: true);
            var equipmentFailure = CommandResult(
                PlayerProgressionCommandKind.UpgradeGrowthEquipment,
                PlayerProgressionCommandStatus.PersistenceFailed,
                equipment.id, equipped);
            var equipmentAfterSuccess = CreateProgression(catalog, 0,
                equipmentRank: 1, equipmentEquipped: true);
            var equipmentSuccess = CommandResult(
                PlayerProgressionCommandKind.UpgradeGrowthEquipment,
                PlayerProgressionCommandStatus.Success,
                equipment.id, equipmentAfterSuccess);
            Assert(GrowthHubPageModel.ResolveEquipmentState(equipment,
                        insufficientEquipment, false, null)
                    == HubGrowthState.Insufficient
                && GrowthHubPageModel.ResolveEquipmentState(equipment,
                        maximumEquipment, false, null)
                    == HubGrowthState.Maximum
                && GrowthHubPageModel.ResolveEquipmentState(equipment,
                        equipped, false, equipmentFailure)
                    == HubGrowthState.Error
                && GrowthHubPageModel.ResolveEquipmentState(equipment,
                        equipmentAfterSuccess, false, equipmentSuccess)
                    == HubGrowthState.Success
                && GrowthHubPageModel.ResolveEquipmentEligibility(equipment,
                        equipmentAfterSuccess, false)
                    == HubGrowthState.Insufficient
                && GrowthHubPageModel.ResolvePrimaryAction(
                        GrowthPageId.Equipment,
                        GrowthHubPageModel.ResolveEquipmentEligibility(
                            equipment, equipmentAfterSuccess, false), true)
                    == HubGrowthPrimaryAction.None
                && GrowthHubPageModel.ResolvePrimaryAction(
                        GrowthPageId.Equipment, HubGrowthState.Owned, false)
                    == HubGrowthPrimaryAction.Equip
                && GrowthHubPageModel.ResolvePrimaryAction(
                        GrowthPageId.Equipment,
                        HubGrowthState.Upgradeable, true)
                    == HubGrowthPrimaryAction.UpgradeEquipment,
                "equipment feedback remains distinct from current cost/rank eligibility after a successful upgrade");

            var cultivationReady = CreateProgression(catalog, 6);
            var cultivationInsufficient = CreateProgression(catalog, 0);
            var cultivationMaximum = CreateProgression(catalog, 0,
                cultivationRank: 2);
            var lockedCultivation = new CultivationNodeDefinitionDto
            {
                id = cultivation.id,
                prerequisites = new[]
                {
                    new CultivationPrerequisiteDto
                    {
                        nodeId = "cultivation.not-earned",
                        requiredRank = 1,
                    },
                },
                ranks = cultivation.ranks,
            };
            var cultivationFailure = CommandResult(
                PlayerProgressionCommandKind.UpgradeCultivation,
                PlayerProgressionCommandStatus.PersistenceFailed,
                cultivation.id, cultivationReady);
            var cultivationAfterSuccess = CreateProgression(catalog, 0,
                cultivationRank: 1);
            var cultivationSuccess = CommandResult(
                PlayerProgressionCommandKind.UpgradeCultivation,
                PlayerProgressionCommandStatus.Success,
                cultivation.id, cultivationAfterSuccess);
            Assert(GrowthHubPageModel.ResolveCultivationState(cultivation,
                        cultivationReady, false, null)
                    == HubGrowthState.Upgradeable
                && GrowthHubPageModel.ResolveCultivationState(cultivation,
                        cultivationInsufficient, false, null)
                    == HubGrowthState.Insufficient
                && GrowthHubPageModel.ResolveCultivationState(cultivation,
                        cultivationMaximum, false, null)
                    == HubGrowthState.Maximum
                && GrowthHubPageModel.ResolveCultivationState(cultivation,
                        cultivationReady, true, null)
                    == HubGrowthState.Loading
                && GrowthHubPageModel.ResolveCultivationState(
                        lockedCultivation, cultivationReady, false, null)
                    == HubGrowthState.Locked
                && GrowthHubPageModel.ResolveCultivationState(cultivation,
                        cultivationReady, false, cultivationFailure)
                    == HubGrowthState.Error
                && GrowthHubPageModel.ResolveCultivationState(cultivation,
                        cultivationAfterSuccess, false, cultivationSuccess)
                    == HubGrowthState.Success
                && GrowthHubPageModel.ResolveCultivationEligibility(
                        cultivation, cultivationAfterSuccess, false)
                    == HubGrowthState.Insufficient
                && GrowthHubPageModel.ResolvePrimaryAction(
                        GrowthPageId.Cultivation,
                        GrowthHubPageModel.ResolveCultivationEligibility(
                            cultivation, cultivationAfterSuccess, false), false)
                    == HubGrowthPrimaryAction.None
                && GrowthHubPageModel.ResolvePrimaryAction(
                        GrowthPageId.Cultivation,
                        HubGrowthState.Upgradeable, false)
                    == HubGrowthPrimaryAction.UpgradeCultivation
                && GrowthHubPageModel.ActionCopy(
                        HubGrowthPrimaryAction.None,
                        HubGrowthState.Locked, true)
                    == RuntimeUiCopyId.HubCultivationLockedAction
                && RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.HubCultivationLockedAction).Text
                    == "前置未满足"
                && GrowthHubPageModel.FormatPrerequisite(lockedCultivation,
                        catalog, cultivationReady)
                    .Contains("cultivation.not-earned"),
                "cultivation state distinguishes feedback from current command eligibility, including a successful rank-one upgrade that leaves zero balance and no executable next action");

            Assert(HomeHubPageModel.ResolvePreviewState(default, false)
                    == RuntimeUiInteractionState.Error
                && HomeHubPageModel.ResolvePreviewState(default, true)
                    == RuntimeUiInteractionState.Loading
                && HomeHubPageModel.FormatPreview(default, catalog)
                    == RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.HubGrowthPreviewError).Text,
                "Home growth preview exposes explicit loading and resolution failure states");

            ValidateMeasuredHubCopy(runtimeUiTheme, activity.description,
                new Rect(0f, 0f, 402f, 874f), "402-full");
            ValidateMeasuredHubCopy(runtimeUiTheme, activity.description,
                new Rect(0f, 44f, 402f, 796f), "402-inset-44-34");
        }

        private static void ValidateMeasuredHubCopy(RuntimeUiTheme theme,
            string copy, Rect safeArea, string caseName)
        {
            var layout = PortraitHubLayout.Create(402f, 874f, safeArea);
            var context = RuntimeUiDrawContext.Create(theme,
                layout.Frame.Scale);
            var textLayout = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                context, layout.ActivityPage.Description,
                RuntimeUiTypographyRole.Body, TextAnchor.MiddleLeft);
            var lines = RuntimeUiGui.ResolveStatusTextLines(textLayout, copy);
            Assert(lines.HasSecondLine
                && string.Equals(lines.FirstLine + lines.SecondLine, copy,
                    StringComparison.Ordinal)
                && textLayout.Style.CalcSize(
                    new GUIContent(lines.FirstLine)).x
                    <= textLayout.FirstLineRect.width + 0.5f
                && textLayout.Style.CalcSize(
                    new GUIContent(lines.SecondLine)).x
                    <= textLayout.SecondLineRect.width + 0.5f,
                caseName + " Activity description uses the measured two-line authority without truncation");
        }

        private static PlayerProgressionCommandResult CommandResult(
            PlayerProgressionCommandKind kind,
            PlayerProgressionCommandStatus status, string identity,
            PlayerProgressionProjection projection)
        {
            return new PlayerProgressionCommandResult(kind, status, identity,
                projection);
        }

        private static PlayerProgressionProjection CreateProgression(
            CompiledOutgameContentCatalog catalog, long morningDew,
            int? equipmentRank = null, bool equipmentEquipped = false,
            int? cultivationRank = null, bool activityClaimed = false)
        {
            var profile = PlayerProfile.CreateDefault();
            profile.itemBalances = morningDew > 0
                ? new[]
                {
                    new PlayerItemBalance
                    {
                        itemId = OutgameContentIds.Items.MorningDew,
                        quantity = morningDew,
                    },
                }
                : Array.Empty<PlayerItemBalance>();
            profile.activityReceipts = activityClaimed
                ? new[]
                {
                    new PlayerActivityReceipt
                    {
                        receiptId = OutgameContentIds.Receipts.StarterSupplies,
                    },
                }
                : Array.Empty<PlayerActivityReceipt>();
            profile.ownedGrowthEquipment = equipmentRank.HasValue
                ? new[]
                {
                    new PlayerGrowthEquipment
                    {
                        growthEquipmentId = OutgameContentIds.GrowthEquipment
                            .SunleafEmblem,
                        rank = equipmentRank.Value,
                    },
                }
                : Array.Empty<PlayerGrowthEquipment>();
            profile.growthLoadout = equipmentEquipped
                ? new[]
                {
                    new PlayerGrowthLoadoutEntry
                    {
                        slotId = OutgameContentIds.GrowthSlots.Offense,
                        growthEquipmentId = OutgameContentIds.GrowthEquipment
                            .SunleafEmblem,
                    },
                }
                : Array.Empty<PlayerGrowthLoadoutEntry>();
            profile.cultivationRanks = cultivationRank.HasValue
                ? new[]
                {
                    new PlayerCultivationRank
                    {
                        cultivationNodeId = OutgameContentIds.CultivationNodes
                            .VitalRoots,
                        rank = cultivationRank.Value,
                    },
                }
                : Array.Empty<PlayerCultivationRank>();
            return PlayerProgressionProjection.Create(profile, catalog);
        }

        private static void ValidateHubPresenterLifecycle(
            RuntimeUiTheme runtimeUiTheme)
        {
            var context = FakeShellFlowContext.AtLobby("builtin-test-v1",
                LobbyHubPresenter.Orchard01LevelId);
            var presenter = CreatePresenter<LobbyHubPresenter>(
                "LobbyHubLifecycleValidation");
            try
            {
                presenter.Initialize(context, runtimeUiTheme);
                var safeArea = new Rect(0f, 0f, 402f, 874f);
                var layout = PortraitHubLayout.Create(402f, 874f, safeArea);
                Assert(presenter.CurrentPage == HubPageId.Home
                    && presenter.CurrentGrowthPage == GrowthPageId.Equipment,
                    "a new Lobby lifecycle starts on Home and Equipment");

                Assert(presenter.TryActivateAt(
                        layout.PrimaryNavigation.Activity.center,
                        402f, 874f, safeArea)
                    && presenter.CurrentPage == HubPageId.Activity
                    && !presenter.TryActivateAt(
                        layout.PrimaryNavigation.Activity.center,
                        402f, 874f, safeArea),
                    "Hub accepts one page activation and rejects its duplicate");
                Assert(!presenter.TryActivateAt(
                        layout.ActivityPage.Title.center,
                        402f, 874f, safeArea)
                    && context.SelectionCount == 0
                    && context.StartCount == 0,
                    "Activity unavailable content exposes no command");

                Assert(presenter.TryActivateAt(
                        layout.PrimaryNavigation.Growth.center,
                        402f, 874f, safeArea)
                    && presenter.TryActivateAt(
                        layout.GrowthPage.Navigation.Cultivation.center,
                        402f, 874f, safeArea)
                    && presenter.CurrentGrowthPage == GrowthPageId.Cultivation
                    && !presenter.TryActivateAt(
                        layout.GrowthPage.DetailTitle.center,
                        402f, 874f, safeArea),
                    "Growth tabs switch locally while detail copy owns no action");
                Assert(presenter.TrySelectHubPage(HubPageId.Activity)
                    && presenter.TrySelectHubPage(HubPageId.Growth)
                    && presenter.CurrentGrowthPage == GrowthPageId.Cultivation,
                    "Growth subpage state survives primary Hub page switches");
                Assert(context.Navigator.CurrentRoute == AppRoute.Lobby
                    && context.Navigator.TransitionState == AppTransitionState.Idle,
                    "Hub presenter navigation leaves AppNavigator on idle Lobby");

                Assert(presenter.TrySelectHubPage(HubPageId.Home)
                    && presenter.TryStart()
                    && !presenter.TrySelectHubPage(HubPageId.Activity)
                    && !presenter.TrySelectGrowthPage(GrowthPageId.Equipment)
                    && !presenter.TryActivateAt(
                        layout.PrimaryNavigation.Activity.center,
                        402f, 874f, safeArea),
                    "App transition rejects Hub and Growth duplicate navigation");
            }
            finally
            {
                DestroyPresenter(presenter);
            }

            var freshContext = FakeShellFlowContext.AtLobby("builtin-test-v1",
                LobbyHubPresenter.Orchard03LevelId);
            var freshPresenter = CreatePresenter<LobbyHubPresenter>(
                "LobbyHubFreshLifecycleValidation");
            try
            {
                freshPresenter.Initialize(freshContext, runtimeUiTheme);
                Assert(freshPresenter.CurrentPage == HubPageId.Home
                    && freshPresenter.CurrentGrowthPage == GrowthPageId.Equipment,
                    "a later Lobby instance does not retain previous local Hub state");
            }
            finally
            {
                DestroyPresenter(freshPresenter);
            }
        }

        private static void ValidateLobbyVisualContract(RuntimeUiTheme runtimeUiTheme)
        {
            var drawContext = RuntimeUiDrawContext.Create(runtimeUiTheme, 1f);
            Assert(ReferenceEquals(drawContext.Theme, runtimeUiTheme)
                && ReferenceEquals(drawContext.ArtSet, runtimeUiTheme.ActiveArtSet)
                && ReferenceEquals(drawContext.Styles.HitTarget.font, null),
                "Lobby draw context is theme-bound and its transparent hit style has no fallback font");

            Assert(LobbyHubPresenter.ResolveCardState(false, true, true, false, false)
                    == RuntimeUiInteractionState.Selected
                && LobbyHubPresenter.ResolveCardState(false, false, true, false, false)
                    == RuntimeUiInteractionState.Disabled
                && LobbyHubPresenter.ResolveCardState(true, true, true, false, false)
                    == RuntimeUiInteractionState.Loading
                && LobbyHubPresenter.ResolveCardState(false, true, false, true, false)
                    == RuntimeUiInteractionState.HoveredOrFocused
                && LobbyHubPresenter.ResolveCardState(false, true, false, true, true)
                    == RuntimeUiInteractionState.Pressed,
                "Lobby cards map selection, unavailable, transition, focus, and press to shared states");
            Assert(LobbyHubPresenter.ResolveActionState(true, true, false, false)
                    == RuntimeUiInteractionState.Loading
                && LobbyHubPresenter.ResolveActionState(false, false, false, false)
                    == RuntimeUiInteractionState.Disabled
                && LobbyHubPresenter.ResolveActionState(false, true, true, true)
                    == RuntimeUiInteractionState.Pressed,
                "Lobby Start maps transition and unavailable states before pointer feedback");
            Assert(LobbyHubPresenter.ResolveNavigationState(
                        true, true, true, true)
                    == RuntimeUiInteractionState.Loading
                && LobbyHubPresenter.ResolveNavigationState(
                        false, true, false, false)
                    == RuntimeUiInteractionState.Selected
                && LobbyHubPresenter.ResolveNavigationState(
                        false, true, true, true)
                    == RuntimeUiInteractionState.Pressed
                && LobbyHubPresenter.ResolveNavigationState(
                        false, false, true, false)
                    == RuntimeUiInteractionState.HoveredOrFocused,
                "Hub navigation keeps transition, press, selection, and focus priority finite");

            var artSet = runtimeUiTheme.ActiveArtSet;
            Assert(HasDistinctCue(artSet, RuntimeUiArtSlot.MarkerSelected,
                    RuntimeUiArtSlot.IndicatorDisabled)
                && HasDistinctCue(artSet, RuntimeUiArtSlot.IndicatorLoading,
                    RuntimeUiArtSlot.IndicatorError),
                "Lobby selected, disabled, loading, and error states own distinct non-color cues");
        }

        private static bool HasDistinctCue(RuntimeUiArtSet artSet,
            RuntimeUiArtSlot first, RuntimeUiArtSlot second)
        {
            return artSet != null
                && artSet.TryGetBinding(first, out var firstBinding)
                && artSet.TryGetBinding(second, out var secondBinding)
                && firstBinding.Sprite != null && secondBinding.Sprite != null
                && firstBinding.Sprite != secondBinding.Sprite;
        }

        private static void ValidateSettlementVisualContract(RuntimeUiTheme runtimeUiTheme)
        {
            var drawContext = RuntimeUiDrawContext.Create(runtimeUiTheme, 1f);
            Assert(ReferenceEquals(drawContext.Theme, runtimeUiTheme)
                && ReferenceEquals(drawContext.ArtSet, runtimeUiTheme.ActiveArtSet)
                && ReferenceEquals(drawContext.Styles.HitTarget.font, null),
                "Settlement draw context is theme-bound and has no fallback font");

            Assert(SettlementPresenter.ResolveResultState(true, true)
                    == RuntimeUiInteractionState.Success
                && SettlementPresenter.ResolveResultState(true, false)
                    == RuntimeUiInteractionState.Error
                && SettlementPresenter.ResolveResultState(false, false)
                    == RuntimeUiInteractionState.Loading,
                "Settlement outcome maps victory, defeat, and recovery to shared states");
            Assert(SettlementPresenter.ResolveActionState(true, true, false, false)
                    == RuntimeUiInteractionState.Loading
                && SettlementPresenter.ResolveActionState(false, false, false, false)
                    == RuntimeUiInteractionState.Disabled
                && SettlementPresenter.ResolveActionState(false, true, true, false)
                    == RuntimeUiInteractionState.HoveredOrFocused
                && SettlementPresenter.ResolveActionState(false, true, true, true)
                    == RuntimeUiInteractionState.Pressed,
                "Settlement actions map transition and availability before pointer feedback");

            var artSet = runtimeUiTheme.ActiveArtSet;
            Assert(HasDistinctCue(artSet, RuntimeUiArtSlot.IndicatorSuccess,
                    RuntimeUiArtSlot.IndicatorError)
                && HasDistinctCue(artSet, RuntimeUiArtSlot.IndicatorLoading,
                    RuntimeUiArtSlot.IndicatorDisabled)
                && HasDistinctCue(artSet, RuntimeUiArtSlot.IndicatorWarning,
                    RuntimeUiArtSlot.IndicatorError),
                "Settlement outcome, transition, disabled, and recoverable states own distinct non-color cues");
        }

        private static void ValidateThreeCardSelectionAndSelectedStart(RuntimeUiTheme runtimeUiTheme)
        {
            var context = FakeShellFlowContext.AtLobby("builtin-test-v1",
                LobbyHubPresenter.Orchard01LevelId);
            var presenter = CreatePresenter<LobbyHubPresenter>("LobbyMultiLevelValidation");
            try
            {
                presenter.Initialize(context, runtimeUiTheme);
                Assert(presenter.SelectedLevelId == LobbyHubPresenter.Orchard01LevelId,
                    "Lobby visibly restores the context selection");

                var safeArea = new Rect(0f, 0f, 402f, 874f);
                var layout = PortraitHubLayout.Create(402f, 874f, safeArea);
                Assert(presenter.TryActivateAt(layout.HomePage.Orchard02Card.center,
                        402f, 874f, safeArea),
                    "orchard-02 drawn card accepts input");
                Assert(context.SelectionCount == 1
                    && context.SelectedLevelId == LobbyHubPresenter.Orchard02LevelId
                    && presenter.SelectedLevelId == LobbyHubPresenter.Orchard02LevelId,
                    "card selection updates both persisted context and visible selection");
                Assert(context.StartCount == 0
                    && context.Navigator.TransitionState == AppTransitionState.Idle,
                    "selecting a card does not navigate");

                Assert(presenter.TryActivateAt(layout.HomePage.StartButton.center,
                        402f, 874f, safeArea),
                    "Start drawn rectangle accepts input");
                Assert(context.StartCount == 1
                    && context.StartLevelId == LobbyHubPresenter.Orchard02LevelId,
                    "Start submits only the visibly selected orchard-02 ID");
                Assert(Guid.TryParse(context.StartSessionId, out _)
                    && context.StartSeed != 0
                    && context.StartContentVersion == "builtin-test-v1",
                    "Start creates a valid session identity, seed, and content identity");
                Assert(!presenter.TryStart() && context.StartCount == 1,
                    "duplicate Start is ignored while navigation loads");
                Assert(!presenter.TrySelectLevel(LobbyHubPresenter.Orchard03LevelId)
                    && context.SelectionCount == 1,
                    "selection is also guarded during transition");
            }
            finally
            {
                DestroyPresenter(presenter);
            }

            var strictContext = FakeShellFlowContext.AtLobby("builtin-test-v1",
                LobbyHubPresenter.Orchard03LevelId);
            var strictPresenter = CreatePresenter<LobbyHubPresenter>("LobbyStrictSelectionValidation");
            try
            {
                strictPresenter.Initialize(strictContext, runtimeUiTheme);
                Assert(!strictPresenter.TrySelectLevel("orchard-missing")
                    && strictPresenter.SelectedLevelId == LobbyHubPresenter.Orchard03LevelId,
                    "unknown selection is rejected without changing or defaulting the visible level");
                Assert(strictPresenter.TryStart()
                    && strictContext.StartLevelId == LobbyHubPresenter.Orchard03LevelId,
                    "a rejected selection cannot silently launch orchard-01");
            }
            finally
            {
                DestroyPresenter(strictPresenter);
            }
        }

        private static void ValidateUnavailableProfileRecovery(
            RuntimeUiTheme runtimeUiTheme)
        {
            var recovered = FakeShellFlowContext.AtRecoveredLobby(
                "builtin-test-v1", "orchard-removed", LobbyHubPresenter.Orchard01LevelId);
            var presenter = CreatePresenter<LobbyHubPresenter>("LobbyProfileRecoveryValidation");
            try
            {
                presenter.Initialize(recovered, runtimeUiTheme);
                Assert(recovered.RecoveredUnavailableLevelId == "orchard-removed"
                    && presenter.SelectedLevelId == LobbyHubPresenter.Orchard01LevelId,
                    "unavailable stored identity remains observable while safe UI default is selected");
                Assert(presenter.TryStart()
                    && recovered.StartLevelId == LobbyHubPresenter.Orchard01LevelId,
                    "profile recovery starts only the declared visible default");
            }
            finally
            {
                DestroyPresenter(presenter);
            }
        }

        private static void ValidateSettlementDisplay(RuntimeUiTheme runtimeUiTheme)
        {
            var context = FakeShellFlowContext.AtSettlement(
                new SettlementViewData(LobbyHubPresenter.Orchard03LevelId, true, 12, 3));
            var presenter = CreatePresenter<SettlementPresenter>("SettlementDisplayValidation");
            try
            {
                presenter.Initialize(context, runtimeUiTheme);
                Assert(presenter.HasViewData, "valid Settlement binds view data");
                Assert(presenter.ViewData.LevelId == LobbyHubPresenter.Orchard03LevelId
                    && presenter.ViewData.Victory
                    && presenter.ViewData.ReachedWave == 12
                    && presenter.ViewData.RemainingLives == 3,
                    "Settlement displays completed level, outcome, wave, and lives exactly");
            }
            finally
            {
                DestroyPresenter(presenter);
            }
        }

        private static void ValidateReturnAndRetry(RuntimeUiTheme runtimeUiTheme)
        {
            var returnContext = FakeShellFlowContext.AtSettlement(
                new SettlementViewData(LobbyHubPresenter.Orchard03LevelId, false, 7, 0));
            var returnPresenter = CreatePresenter<SettlementPresenter>("SettlementReturnValidation");
            try
            {
                returnPresenter.Initialize(returnContext, runtimeUiTheme);
                Assert(returnPresenter.TryReturn(), "Return command is accepted");
                Assert(returnContext.ReturnCount == 1 && returnContext.ClearedBeforeReturn,
                    "Return clears completed session/result before navigation");
                Assert(returnContext.SelectedLevelId == LobbyHubPresenter.Orchard03LevelId
                    && returnContext.PersistedSelectedLevelId == LobbyHubPresenter.Orchard03LevelId,
                    "Return restores the completed level as the Lobby selection");
                Assert(returnContext.Navigator.HasPendingRoute
                    && returnContext.Navigator.PendingRoute == AppRoute.Lobby,
                    "Return requests Lobby");
                Assert(!returnPresenter.TryReturn() && returnContext.ReturnCount == 1,
                    "duplicate Return is ignored while navigation loads");
            }
            finally
            {
                DestroyPresenter(returnPresenter);
            }

            var retryContext = FakeShellFlowContext.AtSettlement(
                new SettlementViewData(LobbyHubPresenter.Orchard03LevelId, false, 9, 0));
            var completedSessionId = retryContext.CompletedSessionId;
            var completedSeed = retryContext.CompletedSeed;
            var retryPresenter = CreatePresenter<SettlementPresenter>("SettlementRetryValidation");
            try
            {
                retryPresenter.Initialize(retryContext, runtimeUiTheme);
                Assert(retryPresenter.TryRetry(), "Retry command is accepted");
                Assert(retryContext.RetryCount == 1,
                    "Retry issues exactly one flow command");
                Assert(retryContext.RetrySessionId != completedSessionId
                    && Guid.TryParse(retryContext.RetrySessionId, out _),
                    "Retry creates a fresh session identity");
                Assert(retryContext.RetrySeed != 0 && retryContext.RetrySeed != completedSeed,
                    "Retry creates a fresh nonzero seed");
                Assert(retryContext.RetryLevelId == LobbyHubPresenter.Orchard03LevelId
                    && retryContext.RetryContentVersion == "builtin-test-v1",
                    "Retry retains the completed level and content version");
                Assert(!retryPresenter.TryRetry() && retryContext.RetryCount == 1,
                    "duplicate Retry is ignored while navigation loads");
            }
            finally
            {
                DestroyPresenter(retryPresenter);
            }
        }

        private static void ValidateInvalidResultRecovery(RuntimeUiTheme runtimeUiTheme)
        {
            ValidateRecovery(runtimeUiTheme, false, SettlementPresenter.MissingResult);
            ValidateRecovery(runtimeUiTheme, true, "settlement-result-level-mismatch");
        }

        private static void ValidateRecovery(RuntimeUiTheme runtimeUiTheme,
            bool mismatch, string expectedErrorCode)
        {
            var context = FakeShellFlowContext.AtSettlement(
                new SettlementViewData(LobbyHubPresenter.Orchard02LevelId, true, 1, 1));
            context.HasSettlementResult = false;
            context.ResultMismatch = mismatch;
            var presenter = CreatePresenter<SettlementPresenter>(
                mismatch ? "SettlementMismatchValidation" : "SettlementMissingValidation");
            try
            {
                presenter.Initialize(context, runtimeUiTheme);
                Assert(!presenter.HasViewData,
                    "invalid Settlement does not bind fabricated view data");
                Assert(context.ReportedError.Code == expectedErrorCode,
                    "invalid Settlement reports a structured recoverable error");
                Assert(context.ReturnCount == 1
                    && context.Navigator.HasPendingRoute
                    && context.Navigator.PendingRoute == AppRoute.Lobby,
                    "invalid Settlement safely requests Lobby once");
            }
            finally
            {
                DestroyPresenter(presenter);
            }
        }

        private static T CreatePresenter<T>(string name) where T : MonoBehaviour
        {
            var gameObject = new GameObject(name);
            gameObject.SetActive(false);
            return gameObject.AddComponent<T>();
        }

        private static void DestroyPresenter(MonoBehaviour presenter)
        {
            if (presenter != null) UnityEngine.Object.DestroyImmediate(presenter.gameObject);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Shell flow validation failed: " + message);
        }

        private sealed class FakeShellFlowContext : IShellFlowContext,
            ILevelSelectionFlowContext, IHubProgressionReadContext,
            IHubProgressionCommandContext
        {
            private static readonly IReadOnlyList<LevelDefinition> Levels = Array.AsReadOnly(new[]
            {
                new LevelDefinition("orchard-01", "map-01", "waves-01", "rules-01", "theme-01",
                    OutgameContentIds.GrowthPolicies.Orchard01),
                new LevelDefinition("orchard-02", "map-02", "waves-02", "rules-02", "theme-02",
                    OutgameContentIds.GrowthPolicies.Orchard02),
                new LevelDefinition("orchard-03", "map-03", "waves-03", "rules-03", "theme-03",
                    OutgameContentIds.GrowthPolicies.Orchard03),
            });

            private readonly AppNavigator _navigator;
            private readonly CompiledLevelCatalog _compiledLevels;
            private SettlementViewData _settlementViewData;

            private FakeShellFlowContext(AppNavigator navigator, string bundledContentVersion,
                string selectedLevelId)
            {
                _navigator = navigator;
                BundledContentVersion = bundledContentVersion;
                SelectedLevelId = selectedLevelId;
                PersistedSelectedLevelId = selectedLevelId;
                CompletedSessionId = Guid.NewGuid().ToString("N");
                CompletedSeed = 301;
                CompletedLevelId = selectedLevelId;
                CompletedContentVersion = bundledContentVersion;
                if (!BundledGameContentLoader.TryLoadBundle(out var bundle,
                        out var contentValidation))
                    throw new InvalidOperationException(
                        contentValidation?.Issues[0].message
                        ?? "Bundled content is unavailable.");
                OutgameContent = bundle.Outgame;
                if (!LevelCatalogCompiler.TryCompile(
                        BundledLevelCatalogFactory.CreateSource(), bundle.Battle,
                        out _compiledLevels, out var levelValidation))
                    throw new InvalidOperationException(
                        levelValidation?.Issues[0].Message
                        ?? "Bundled levels are unavailable.");
                Progression = PlayerProgressionProjection.Create(
                    PlayerProfile.CreateDefault(), OutgameContent);
                TryRefreshSelectedGrowthPreview(out _currentGrowthPreview);
            }

            public IAppNavigator Navigator => _navigator;
            public string BundledContentVersion { get; }
            public IReadOnlyList<LevelDefinition> PlayableLevels => Levels;
            public string SelectedLevelId { get; private set; }
            public string PersistedSelectedLevelId { get; private set; }
            public string RecoveredUnavailableLevelId { get; private set; }
            public int SelectionCount { get; private set; }
            public bool HasSettlementResult { get; set; }
            public bool ResultMismatch { get; set; }
            public string CompletedSessionId { get; private set; }
            public int CompletedSeed { get; private set; }
            public string CompletedLevelId { get; private set; }
            public string CompletedContentVersion { get; private set; }
            public int StartCount { get; private set; }
            public string StartLevelId { get; private set; }
            public string StartSessionId { get; private set; }
            public int StartSeed { get; private set; }
            public string StartContentVersion { get; private set; }
            public int ReturnCount { get; private set; }
            public bool ClearedBeforeReturn { get; private set; }
            public int RetryCount { get; private set; }
            public string RetryLevelId { get; private set; }
            public string RetrySessionId { get; private set; }
            public int RetrySeed { get; private set; }
            public string RetryContentVersion { get; private set; }
            public ShellFlowError ReportedError { get; private set; }
            public CompiledOutgameContentCatalog OutgameContent { get; }
            public PlayerProgressionProjection Progression { get; private set; }
            private BattleGrowthResolution _currentGrowthPreview;
            public BattleGrowthResolution CurrentGrowthPreview =>
                _currentGrowthPreview;
            public bool ProgressionCommandInProgress => false;

            public static FakeShellFlowContext AtLobby(string bundledContentVersion,
                string selectedLevelId)
            {
                return new FakeShellFlowContext(new AppNavigator(), bundledContentVersion,
                    selectedLevelId);
            }

            public static FakeShellFlowContext AtRecoveredLobby(string bundledContentVersion,
                string unavailableLevelId, string defaultLevelId)
            {
                var context = AtLobby(bundledContentVersion, defaultLevelId);
                context.RecoveredUnavailableLevelId = unavailableLevelId;
                return context;
            }

            public static FakeShellFlowContext AtSettlement(SettlementViewData viewData)
            {
                var navigator = new AppNavigator();
                Transition(navigator, AppRoute.Battle);
                Transition(navigator, AppRoute.Settlement);
                var levelId = string.IsNullOrEmpty(viewData.LevelId)
                    ? LobbyHubPresenter.Orchard01LevelId
                    : viewData.LevelId;
                return new FakeShellFlowContext(navigator, "builtin-test-v1", levelId)
                {
                    _settlementViewData = viewData,
                    HasSettlementResult = true,
                    CompletedLevelId = levelId,
                };
            }

            public bool TrySelectLevel(string levelId, out ShellFlowError error)
            {
                if (_navigator.TransitionState != AppTransitionState.Idle)
                {
                    error = new ShellFlowError("app-transition-in-progress");
                    return false;
                }
                if (!ContainsLevel(levelId))
                {
                    error = new ShellFlowError("battle-level-resolution-failed", levelId);
                    return false;
                }

                SelectedLevelId = levelId;
                PersistedSelectedLevelId = levelId;
                SelectionCount++;
                TryRefreshSelectedGrowthPreview(out _currentGrowthPreview);
                error = ShellFlowError.None;
                return true;
            }

            public bool TryRefreshSelectedGrowthPreview(
                out BattleGrowthResolution preview)
            {
                if (!_compiledLevels.TryResolve(SelectedLevelId,
                        out var resolved, out _))
                {
                    preview = default;
                    _currentGrowthPreview = preview;
                    return false;
                }
                preview = BattleGrowthResolver.Resolve(OutgameContent,
                    resolved, Progression);
                _currentGrowthPreview = preview;
                return preview.Succeeded;
            }

            public IEnumerator TryClaimActivity(string activityId,
                Action<PlayerProgressionCommandResult> completed)
            {
                return CompleteRejected(PlayerProgressionCommandKind.ClaimActivity,
                    activityId, completed);
            }

            public IEnumerator TryEquipGrowthEquipment(string growthEquipmentId,
                string slotId, Action<PlayerProgressionCommandResult> completed)
            {
                return CompleteRejected(
                    PlayerProgressionCommandKind.EquipGrowthEquipment,
                    growthEquipmentId, completed);
            }

            public IEnumerator TryUpgradeGrowthEquipment(
                string growthEquipmentId,
                Action<PlayerProgressionCommandResult> completed)
            {
                return CompleteRejected(
                    PlayerProgressionCommandKind.UpgradeGrowthEquipment,
                    growthEquipmentId, completed);
            }

            public IEnumerator TryUpgradeCultivation(string cultivationNodeId,
                Action<PlayerProgressionCommandResult> completed)
            {
                return CompleteRejected(
                    PlayerProgressionCommandKind.UpgradeCultivation,
                    cultivationNodeId, completed);
            }

            private IEnumerator CompleteRejected(
                PlayerProgressionCommandKind kind, string identity,
                Action<PlayerProgressionCommandResult> completed)
            {
                completed?.Invoke(new PlayerProgressionCommandResult(kind,
                    PlayerProgressionCommandStatus.InvalidRequest, identity,
                    Progression, message: "Shell validation fake is read-only."));
                yield break;
            }

            public bool TryStartDefaultBattle(
                string levelId,
                string sessionId,
                int seed,
                string contentVersion,
                out ShellFlowError error)
            {
                if (!ContainsLevel(levelId)
                    || !string.Equals(levelId, SelectedLevelId, StringComparison.Ordinal))
                {
                    error = new ShellFlowError("battle-level-resolution-failed", levelId);
                    return false;
                }
                if (!_navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                {
                    error = new ShellFlowError(navigationError);
                    return false;
                }

                StartCount++;
                StartLevelId = levelId;
                StartSessionId = sessionId;
                StartSeed = seed;
                StartContentVersion = contentVersion;
                error = ShellFlowError.None;
                return true;
            }

            public bool TryGetSettlementViewData(out SettlementViewData viewData,
                out ShellFlowError error)
            {
                if (ResultMismatch)
                {
                    viewData = default;
                    error = new ShellFlowError("settlement-result-level-mismatch");
                    return false;
                }

                if (!HasSettlementResult)
                {
                    viewData = default;
                    error = new ShellFlowError(SettlementPresenter.MissingResult);
                    return false;
                }
                if (!string.Equals(_settlementViewData.LevelId, CompletedLevelId,
                        StringComparison.Ordinal))
                {
                    viewData = default;
                    error = new ShellFlowError("settlement-result-level-mismatch");
                    return false;
                }

                viewData = _settlementViewData;
                error = ShellFlowError.None;
                return true;
            }

            public bool TryReturnToLobby(out ShellFlowError error)
            {
                if (_navigator.TransitionState != AppTransitionState.Idle)
                {
                    error = new ShellFlowError("app-transition-in-progress");
                    return false;
                }

                ReturnCount++;
                if (!string.IsNullOrEmpty(CompletedLevelId))
                {
                    SelectedLevelId = CompletedLevelId;
                    PersistedSelectedLevelId = CompletedLevelId;
                }
                HasSettlementResult = false;
                CompletedSessionId = string.Empty;
                ClearedBeforeReturn = !HasSettlementResult && string.IsNullOrEmpty(CompletedSessionId);
                if (!_navigator.TryBeginTransition(AppRoute.Lobby, out var navigationError))
                {
                    error = new ShellFlowError(navigationError);
                    return false;
                }

                error = ShellFlowError.None;
                return true;
            }

            public bool TryRetryBattle(out ShellFlowError error)
            {
                if (_navigator.TransitionState != AppTransitionState.Idle)
                {
                    error = new ShellFlowError("app-transition-in-progress");
                    return false;
                }

                var previousSession = CompletedSessionId;
                var previousSeed = CompletedSeed;
                var sessionId = Guid.NewGuid().ToString("N");
                var seed = LobbyHubPresenter.CreateNonzeroSeed();
                if (seed == previousSeed) seed = seed == int.MaxValue ? 1 : seed + 1;
                if (!_navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                {
                    error = new ShellFlowError(navigationError);
                    return false;
                }

                RetryCount++;
                RetryLevelId = CompletedLevelId;
                RetryContentVersion = CompletedContentVersion;
                RetrySessionId = sessionId == previousSession
                    ? Guid.NewGuid().ToString("N")
                    : sessionId;
                RetrySeed = seed;
                HasSettlementResult = false;
                error = ShellFlowError.None;
                return true;
            }

            public void ReportRecoverableError(ShellFlowError error)
            {
                ReportedError = error;
            }

            private static bool ContainsLevel(string levelId)
            {
                for (var i = 0; i < Levels.Count; i++)
                {
                    if (string.Equals(Levels[i].LevelId, levelId, StringComparison.Ordinal))
                        return true;
                }
                return false;
            }

            private static void Transition(AppNavigator navigator, AppRoute route)
            {
                if (!navigator.TryBeginTransition(route, out var error)
                    || !navigator.TryCompleteTransition(out error))
                    throw new InvalidOperationException("Fake navigation setup failed: " + error);
            }
        }
    }
}
