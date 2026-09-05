#if FRUIT_DEFENSE_ACCEPTANCE
using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using FruitDefense.App;
using FruitDefense.App.Services;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Shell;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FruitDefense
{
    [DisallowMultipleComponent]
    public sealed class OutgameHubAcceptanceBridge : MonoBehaviour,
        IAcceptanceHubPort
    {
        private const string FixtureLockedCultivationId =
            "cultivation.acceptance-locked";
        private const string FixtureProfileId =
            "00000000000000000000000000000001";
        private const string FixtureTimestamp = "2026-09-01T00:00:00+00:00";

        private AppFlowCoordinator _coordinator;
        private LobbyHubPresenter _presenter;
        private IHubProgressionReadContext _readContext;
        private AcceptanceHubFixtureContext _fixtureContext;
        private string _stateId = "home-fresh";
        private string _telemetryJson = string.Empty;
        private string _publishedJson = string.Empty;
        private int _committedRewardRevisionCount;
        private int _committedGrowthRevisionCount;
        private long _lastObservedRevision = -1;
        private PlayerProgressionCommandResult _lastObservedCommand;
        private bool _configureRoutineActive;

        public string HubAcceptanceTelemetryJson => _telemetryJson;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void FruitDefensePublishHubTelemetry(string json);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureInstalled();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInstalled();
        }

        private static void EnsureInstalled()
        {
            if (!AcceptanceLaunchQuery.IsEnabled(Application.absoluteURL)) return;
            var coordinator = FindFirstObjectByType<AppFlowCoordinator>();
            if (coordinator == null) return;
            var bridge = coordinator.GetComponent<OutgameHubAcceptanceBridge>();
            if (bridge == null)
                bridge = coordinator.gameObject.AddComponent<
                    OutgameHubAcceptanceBridge>();
            bridge._coordinator = coordinator;
        }

        private void Update()
        {
            if (!AcceptanceLaunchQuery.IsEnabled(Application.absoluteURL)) return;
            if (_coordinator == null)
                _coordinator = FindFirstObjectByType<AppFlowCoordinator>();
            if (_coordinator == null) return;

            var presenter = FindFirstObjectByType<LobbyHubPresenter>();
            if (presenter != null && presenter != _presenter)
            {
                BindRealPresenter(presenter);
                if (AcceptanceLaunchQuery.TryGetFirstValue(
                        Application.absoluteURL, "hubState", out var stateId)
                    && !string.IsNullOrWhiteSpace(stateId))
                    ConfigureAcceptanceHubState(stateId);
            }
            PublishIfChanged();
        }

        public void ConfigureAcceptanceHubState(string stateId)
        {
            var result = TryConfigureNamedHubState(stateId);
            if (!result.Succeeded) Debug.LogError(result.ErrorCode);
        }

        public AcceptanceCommandResult TryConfigureNamedHubState(string stateId)
        {
            if (!AcceptanceLaunchQuery.IsEnabled(Application.absoluteURL))
                return AcceptanceCommandResult.Failure(
                    AcceptanceCommandResult.LaunchRequired);
            if (_coordinator == null || _presenter == null)
                return AcceptanceCommandResult.Failure(
                    AcceptanceCommandResult.SessionUnavailable);
            if (!AcceptanceHubStateCatalog.TryGet(stateId, out var definition))
                return AcceptanceCommandResult.Failure(
                    AcceptanceCommandResult.NamedStateUnknown);
            if (definition.EvidenceKind
                == AcceptanceHubEvidenceKind.RealInteractionSequence)
            {
                _presenter.Initialize(_coordinator,
                    _coordinator.RuntimeUiTheme);
                BindRealPresenter(_presenter);
                _stateId = definition.Id;
                PublishIfChanged(force: true);
                return AcceptanceCommandResult.Success();
            }
            if (_configureRoutineActive)
                return AcceptanceCommandResult.Failure(
                    "acceptance-hub-state-configuration-active");

            AcceptanceHubFixtureContext fixture;
            try
            {
                fixture = AcceptanceHubFixtureContext.Create(
                    _coordinator, definition, FixtureLockedCultivationId,
                    FixtureProfileId, FixtureTimestamp);
            }
            catch (Exception exception)
            {
                return AcceptanceCommandResult.Failure(
                    "acceptance-hub-fixture-invalid:" + exception.Message);
            }

            _fixtureContext = fixture;
            _readContext = fixture;
            _stateId = definition.Id;
            _presenter.Initialize(fixture, _coordinator.RuntimeUiTheme);
            ConfigurePage(definition.Page);
            if (definition.EvidenceKind
                    == AcceptanceHubEvidenceKind.PersistenceFailure
                || string.Equals(definition.State, "error",
                    StringComparison.Ordinal))
            {
                StartCoroutine(ConfigureFailureState(definition));
            }
            PublishIfChanged(force: true);
            return AcceptanceCommandResult.Success();
        }

        private void BindRealPresenter(LobbyHubPresenter presenter)
        {
            _presenter = presenter;
            _fixtureContext = null;
            _readContext = _coordinator;
            _stateId = DeriveRealStateId();
            _publishedJson = string.Empty;
            var progression = _readContext.Progression;
            _lastObservedRevision = progression?.Revision ?? -1;
            _lastObservedCommand = presenter.LastProgressionResult;
        }

        private IEnumerator ConfigureFailureState(
            AcceptanceHubStateDefinition definition)
        {
            _configureRoutineActive = true;
            yield return null;
            bool started;
            switch (definition.Page)
            {
                case "activity":
                    started = _presenter.TryClaimStarterActivity();
                    break;
                case "equipment":
                    started = _presenter.TryUpgradeSelectedEquipment();
                    break;
                case "cultivation":
                    started = _presenter.TryUpgradeSelectedCultivation();
                    break;
                default:
                    started = false;
                    break;
            }
            if (!started)
                Debug.LogError("acceptance-hub-failure-command-not-started:"
                    + definition.Id);
            yield return null;
            _configureRoutineActive = false;
            PublishIfChanged(force: true);
        }

        private void ConfigurePage(string page)
        {
            switch (page)
            {
                case "home":
                    _presenter.TrySelectHubPage(HubPageId.Home);
                    break;
                case "activity":
                    _presenter.TrySelectHubPage(HubPageId.Activity);
                    break;
                case "equipment":
                    _presenter.TrySelectHubPage(HubPageId.Growth);
                    _presenter.TrySelectGrowthPage(GrowthPageId.Equipment);
                    break;
                case "cultivation":
                    _presenter.TrySelectHubPage(HubPageId.Growth);
                    _presenter.TrySelectGrowthPage(GrowthPageId.Cultivation);
                    break;
            }
        }

        private void PublishIfChanged(bool force = false)
        {
            if (_coordinator == null || _readContext?.Progression == null) return;
            if (_presenter != null) ObserveCommittedCommand();
            if (_presenter != null && _fixtureContext == null
                && !string.Equals(_stateId, "reward-to-battle",
                    StringComparison.Ordinal))
                _stateId = DeriveRealStateId();
            _telemetryJson = JsonUtility.ToJson(CreateTelemetry(), false);
            if (!force && string.Equals(_telemetryJson, _publishedJson,
                    StringComparison.Ordinal)) return;
            _publishedJson = _telemetryJson;
#if UNITY_WEBGL && !UNITY_EDITOR
            FruitDefensePublishHubTelemetry(_telemetryJson);
#endif
        }

        private void ObserveCommittedCommand()
        {
            var progression = _readContext.Progression;
            var result = _presenter.LastProgressionResult;
            if (progression == null || progression.Revision == _lastObservedRevision
                || result == null || ReferenceEquals(result, _lastObservedCommand)
                || !result.Succeeded) return;
            if (result.Kind == PlayerProgressionCommandKind.ClaimActivity)
                _committedRewardRevisionCount++;
            else
                _committedGrowthRevisionCount++;
            _lastObservedRevision = progression.Revision;
            _lastObservedCommand = result;
        }

        private AcceptanceHubIdentityTelemetry CreateTelemetry()
        {
            var progression = _readContext.Progression;
            var preview = _readContext.CurrentGrowthPreview;
            var snapshot = preview.Succeeded ? preview.Snapshot : null;
            var request = _fixtureContext == null
                ? _coordinator.CurrentRequest
                : null;
            var launchGrowth = request?.GrowthSnapshot;
            var route = _coordinator.Navigator == null
                ? AppRoute.Lobby
                : _coordinator.Navigator.CurrentRoute;
            var manifest = LoadManifest();
            var lastResult = _presenter == null
                ? _lastObservedCommand
                : _presenter.LastProgressionResult;
            var telemetry = new AcceptanceHubIdentityTelemetry
            {
                stateId = _stateId,
                fixtureActive = _fixtureContext != null,
                fixtureId = _fixtureContext == null
                    ? string.Empty
                    : "acceptance-hub/" + _fixtureContext.Definition.Id + "/v1",
                evidenceKind = _fixtureContext == null
                    ? AcceptanceHubEvidenceKind.RealInteractionSequence
                        .ToString()
                    : _fixtureContext.Definition.EvidenceKind.ToString(),
                route = (int)route,
                routeName = route.ToString().ToLowerInvariant(),
                sessionId = request?.SessionId ?? string.Empty,
                seed = request?.Seed ?? 0,
                page = _presenter == null
                    ? string.Empty
                    : _presenter.CurrentPage.ToString().ToLowerInvariant(),
                growthPage = _presenter == null
                    ? string.Empty
                    : _presenter.CurrentGrowthPage.ToString()
                        .ToLowerInvariant(),
                resolvedState = ResolveVisibleState(lastResult, snapshot, route),
                selectedLevelId = _presenter == null
                    ? _coordinator.SelectedLevelId
                    : _presenter.SelectedLevelId,
                manifestId = manifest.manifestId,
                manifestVersion = manifest.schemaVersion.ToString(),
                manifestFingerprint = ComputeSha256(
                    GameContentManifestJson.SerializeCanonicalUtf8(manifest)),
                outgameContentId = _readContext.OutgameContent.Header.catalogId,
                outgameContentVersion =
                    _readContext.OutgameContent.Header.contentVersion,
                outgameContentFingerprint =
                    _readContext.OutgameContent.Fingerprint,
                battleContentId = manifest.battleCatalogId,
                battleContentVersion = manifest.battleContentVersion,
                battleContentFingerprint = BattleResourceFingerprint(
                    manifest.battleCatalogResourcePath),
                profileId = progression.ProfileId,
                profileRevision = progression.Revision,
                growthPolicyId = snapshot?.PolicyId ?? string.Empty,
                growthFingerprint = snapshot?.Fingerprint ?? string.Empty,
                launchGrowthProfileRevision = launchGrowth?.ProfileRevision ?? 0,
                launchGrowthPolicyId = launchGrowth?.PolicyId ?? string.Empty,
                launchGrowthFingerprint = launchGrowth?.Fingerprint
                    ?? string.Empty,
                appliedSourceCount = snapshot == null ? 0
                    : snapshot.SourceRecords.Count(value => value.Disposition
                        == BattleGrowthSourceDisposition.Applied),
                suppressedSourceCount = snapshot == null ? 0
                    : snapshot.SourceRecords.Count(value => value.Disposition
                        == BattleGrowthSourceDisposition.Suppressed),
                receiptCount = progression.ActivityReceiptIds.Count,
                committedRewardRevisionCount =
                    _committedRewardRevisionCount,
                committedGrowthRevisionCount =
                    _committedGrowthRevisionCount,
                commandInProgress = _fixtureContext == null
                    ? _coordinator.ProgressionCommandInProgress
                    : _fixtureContext.ProgressionCommandInProgress,
                lastCommand = lastResult?.Kind.ToString() ?? string.Empty,
                lastCommandStatus = lastResult?.Status.ToString()
                    ?? string.Empty,
                lastCommandError = lastResult != null && !lastResult.Succeeded
                    ? lastResult.Message : string.Empty,
                itemBalances = progression.ItemBalances.Select(value =>
                    new AcceptanceHubItemBalanceTelemetry
                    {
                        itemId = value.ItemId,
                        quantity = value.Quantity,
                    }).ToArray(),
                growthEquipment = progression.OwnedGrowthEquipment.Select(value =>
                    new AcceptanceHubGrowthEquipmentTelemetry
                    {
                        growthEquipmentId = value.GrowthEquipmentId,
                        rank = value.Rank,
                    }).ToArray(),
                loadout = progression.GrowthLoadout.Select(value =>
                    new AcceptanceHubLoadoutTelemetry
                    {
                        slotId = value.SlotId,
                        growthEquipmentId = value.GrowthEquipmentId,
                    }).ToArray(),
                cultivation = progression.CultivationRanks.Select(value =>
                    new AcceptanceHubCultivationTelemetry
                    {
                        cultivationNodeId = value.CultivationNodeId,
                        rank = value.Rank,
                    }).ToArray(),
            };
            return telemetry;
        }

        private string ResolveVisibleState(PlayerProgressionCommandResult lastResult,
            BattleGrowthSnapshot snapshot, AppRoute route)
        {
            if (_presenter == null) return route.ToString().ToLowerInvariant();
            var commandBusy = _fixtureContext == null
                ? _coordinator.ProgressionCommandInProgress
                : _fixtureContext.ProgressionCommandInProgress;
            switch (_presenter.CurrentPage)
            {
                case HubPageId.Activity:
                    var activity = ActivityHubPageModel.SelectPrimaryActivity(
                        _readContext.OutgameContent);
                    return ActivityHubPageModel.ResolveState(activity,
                            _readContext.Progression, commandBusy, lastResult)
                        .ToString().ToLowerInvariant();
                case HubPageId.Growth:
                    if (_presenter.CurrentGrowthPage
                        == GrowthPageId.Cultivation)
                    {
                        _readContext.OutgameContent.CultivationNodes.TryGetValue(
                            _presenter.SelectedCultivationId, out var cultivation);
                        return GrowthHubPageModel.ResolveCultivationState(
                                cultivation, _readContext.Progression, commandBusy,
                                lastResult)
                            .ToString().ToLowerInvariant();
                    }
                    _readContext.OutgameContent.GrowthEquipment.TryGetValue(
                        _presenter.SelectedEquipmentId, out var equipment);
                    return GrowthHubPageModel.ResolveEquipmentState(equipment,
                            _readContext.Progression, commandBusy, lastResult)
                        .ToString().ToLowerInvariant();
                default:
                    if (snapshot == null || snapshot.SourceRecords.Count == 0)
                        return "fresh";
                    var applied = snapshot.SourceRecords.Any(value =>
                        value.Disposition
                        == BattleGrowthSourceDisposition.Applied);
                    var suppressed = snapshot.SourceRecords.Any(value =>
                        value.Disposition
                        == BattleGrowthSourceDisposition.Suppressed);
                    if (applied && suppressed) return "applied-suppressed";
                    return applied ? "applied" : "suppressed";
            }
        }

        private string DeriveRealStateId()
        {
            if (_presenter == null) return "home-fresh";
            switch (_presenter.CurrentPage)
            {
                case HubPageId.Activity:
                    return "activity-live";
                case HubPageId.Growth:
                    return _presenter.CurrentGrowthPage == GrowthPageId.Equipment
                        ? "equipment-live" : "cultivation-live";
                default:
                    return _readContext != null
                        && _readContext.CurrentGrowthPreview.Succeeded
                        && _readContext.CurrentGrowthPreview.Snapshot
                            .SourceRecords.Count > 0
                            ? "home-policy-preview" : "home-fresh";
            }
        }

        private static GameContentManifestDto LoadManifest()
        {
            var resource = Resources.Load<TextAsset>(
                BundledGameContentLoader.ManifestResourcePath);
            if (resource == null)
                throw new InvalidOperationException(
                    "Bundled manifest resource is unavailable.");
            return GameContentManifestJson.Deserialize(resource.text);
        }

        private static string BattleResourceFingerprint(string resourcePath)
        {
            var resource = Resources.Load<TextAsset>(resourcePath);
            return resource == null
                ? string.Empty
                : ComputeSha256(BattleContentJson.SerializeCanonicalUtf8(
                    BattleContentJson.Deserialize(resource.text)));
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes ?? Array.Empty<byte>());
                var builder = new StringBuilder(hash.Length * 2);
                for (var index = 0; index < hash.Length; index++)
                    builder.Append(hash[index].ToString("x2"));
                return builder.ToString();
            }
        }

        private sealed class AcceptanceHubFixtureContext : IShellFlowContext,
            ILevelSelectionFlowContext, IHubProgressionReadContext,
            IHubProgressionCommandContext
        {
            private readonly AppFlowCoordinator _coordinator;
            private readonly AcceptanceHubStateDefinition _definition;
            private readonly PlayerProgressionCommandStatus _failureStatus;
            private BattleGrowthResolution _preview;
            private string _selectedLevelId;

            private AcceptanceHubFixtureContext(AppFlowCoordinator coordinator,
                AcceptanceHubStateDefinition definition,
                CompiledOutgameContentCatalog content,
                PlayerProgressionProjection progression,
                PlayerProgressionCommandStatus failureStatus,
                string selectedLevelId)
            {
                _coordinator = coordinator;
                _definition = definition;
                OutgameContent = content;
                Progression = progression;
                _failureStatus = failureStatus;
                _selectedLevelId = selectedLevelId;
                TryRefreshSelectedGrowthPreview(out _preview);
            }

            public IAppNavigator Navigator => _coordinator.Navigator;
            public string BundledContentVersion =>
                _coordinator.BundledContentVersion;
            public System.Collections.Generic.IReadOnlyList<LevelDefinition>
                PlayableLevels => _coordinator.PlayableLevels;
            public string SelectedLevelId => _selectedLevelId;
            public CompiledOutgameContentCatalog OutgameContent { get; }
            public PlayerProgressionProjection Progression { get; }
            public BattleGrowthResolution CurrentGrowthPreview => _preview;
            public bool ProgressionCommandInProgress =>
                string.Equals(_definition.State, "claiming",
                    StringComparison.Ordinal)
                || string.Equals(_definition.State, "loading",
                    StringComparison.Ordinal);
            public AcceptanceHubStateDefinition Definition => _definition;

            public static AcceptanceHubFixtureContext Create(
                AppFlowCoordinator coordinator,
                AcceptanceHubStateDefinition definition,
                string lockedCultivationId, string profileId,
                string timestamp)
            {
                var manifest = LoadManifest();
                var resource = Resources.Load<TextAsset>(
                    manifest.outgameCatalogResourcePath);
                if (resource == null)
                    throw new InvalidOperationException(
                        "Bundled outgame resource is unavailable.");
                var source = OutgameContentJson.Deserialize(resource.text);
                if (string.Equals(definition.Id, "cultivation-locked",
                        StringComparison.Ordinal))
                {
                    source.header.catalogId = "catalog.outgame.acceptance";
                    source.header.contentVersion = "acceptance.1";
                    source.cultivationNodes = source.cultivationNodes.Concat(new[]
                    {
                        new CultivationNodeDefinitionDto
                        {
                            id = lockedCultivationId,
                            presentationId =
                                "presentation.acceptance.cultivation.locked",
                            displayName = "待解锁培育",
                            description = "完成壮根培育后解锁。",
                            prerequisites = new[]
                            {
                                new CultivationPrerequisiteDto
                                {
                                    nodeId = OutgameContentIds.CultivationNodes.VitalRoots,
                                    requiredRank = 1,
                                },
                            },
                            ranks = new[]
                            {
                                new CultivationRankDefinitionDto
                                {
                                    rank = 1,
                                    costs = new[]
                                    {
                                        new GrowthCostDto
                                        {
                                            itemId = OutgameContentIds.Items
                                                .MorningDew,
                                            quantity = 1,
                                        },
                                    },
                                    contributions = new[]
                                    {
                                        new GrowthContributionDto
                                        {
                                            domainId = OutgameContentIds
                                                .GrowthDomains.Cultivation,
                                            attributeId = "attribute.resource-gain",
                                            operationId = "modifier.additive",
                                            value = .01f,
                                        },
                                    },
                                },
                            },
                        },
                    }).ToArray();
                }
                if (!OutgameContentCompiler.TryCompile(source,
                        out var content, out var validation))
                {
                    var issue = validation.Issues.FirstOrDefault();
                    throw new InvalidOperationException(issue == null
                        ? "Acceptance outgame fixture is invalid."
                        : issue.ToString());
                }

                var profile = CreateProfile(definition, profileId, timestamp);
                var profileValidation = PlayerProfileCodec.Validate(
                    profile, content);
                if (!profileValidation.Success)
                    throw new InvalidOperationException(
                        profileValidation.Message);
                var selectedLevel = string.Equals(definition.Id,
                    "home-policy-preview", StringComparison.Ordinal)
                    ? LobbyHubPresenter.Orchard02LevelId
                    : LobbyHubPresenter.Orchard01LevelId;
                var failureStatus = definition.EvidenceKind
                    == AcceptanceHubEvidenceKind.PersistenceFailure
                        ? PlayerProgressionCommandStatus.PersistenceFailed
                        : PlayerProgressionCommandStatus.InvalidProfile;
                return new AcceptanceHubFixtureContext(coordinator, definition,
                    content, PlayerProgressionProjection.Create(profile, content),
                    failureStatus, selectedLevel);
            }

            public bool TrySelectLevel(string levelId,
                out ShellFlowError error)
            {
                if (!_coordinator.CurrentLevelCatalog.TryResolve(
                        levelId, out _, out var validation))
                {
                    error = new ShellFlowError(
                        AppFlowCoordinator.LevelResolutionFailed,
                        validation?.ToString() ?? "unknown");
                    return false;
                }
                _selectedLevelId = levelId;
                TryRefreshSelectedGrowthPreview(out _preview);
                error = ShellFlowError.None;
                return true;
            }

            public bool TryRefreshSelectedGrowthPreview(
                out BattleGrowthResolution preview)
            {
                if (!_coordinator.CurrentLevelCatalog.TryResolve(
                        _selectedLevelId, out var level, out _))
                {
                    preview = default;
                    _preview = preview;
                    return false;
                }
                preview = BattleGrowthResolver.Resolve(
                    OutgameContent, level, Progression);
                _preview = preview;
                return preview.Succeeded;
            }

            public IEnumerator TryClaimActivity(string activityId,
                Action<PlayerProgressionCommandResult> completed)
            {
                return Failure(PlayerProgressionCommandKind.ClaimActivity,
                    activityId, completed);
            }

            public IEnumerator TryEquipGrowthEquipment(
                string growthEquipmentId, string slotId,
                Action<PlayerProgressionCommandResult> completed)
            {
                return Failure(
                    PlayerProgressionCommandKind.EquipGrowthEquipment,
                    growthEquipmentId, completed);
            }

            public IEnumerator TryUpgradeGrowthEquipment(
                string growthEquipmentId,
                Action<PlayerProgressionCommandResult> completed)
            {
                return Failure(
                    PlayerProgressionCommandKind.UpgradeGrowthEquipment,
                    growthEquipmentId, completed);
            }

            public IEnumerator TryUpgradeCultivation(string cultivationNodeId,
                Action<PlayerProgressionCommandResult> completed)
            {
                return Failure(PlayerProgressionCommandKind.UpgradeCultivation,
                    cultivationNodeId, completed);
            }

            public bool TryStartDefaultBattle(string levelId,
                string sessionId, int seed, string contentVersion,
                out ShellFlowError error)
            {
                error = new ShellFlowError(
                    "acceptance-hub-static-fixture-cannot-launch");
                return false;
            }

            public bool TryGetSettlementViewData(out SettlementViewData viewData,
                out ShellFlowError error)
            {
                viewData = default;
                error = new ShellFlowError(
                    "acceptance-hub-static-fixture-no-settlement");
                return false;
            }

            public bool TryReturnToLobby(out ShellFlowError error)
            {
                error = new ShellFlowError(
                    "acceptance-hub-static-fixture-no-route-transition");
                return false;
            }

            public bool TryRetryBattle(out ShellFlowError error)
            {
                error = new ShellFlowError(
                    "acceptance-hub-static-fixture-no-retry");
                return false;
            }

            public void ReportRecoverableError(ShellFlowError error)
            {
                _coordinator.ReportRecoverableError(error);
            }

            private IEnumerator Failure(PlayerProgressionCommandKind kind,
                string identity,
                Action<PlayerProgressionCommandResult> completed)
            {
                yield return null;
                completed?.Invoke(new PlayerProgressionCommandResult(
                    kind, _failureStatus, identity, Progression,
                    message: _failureStatus
                        == PlayerProgressionCommandStatus.PersistenceFailed
                            ? "Acceptance fixture rejected persistence."
                            : "Acceptance fixture returned a recoverable command error."));
            }

            private static PlayerProfile CreateProfile(
                AcceptanceHubStateDefinition definition,
                string profileId, string timestamp)
            {
                var profile = PlayerProfile.CreateDefault();
                profile.profileId = profileId;
                profile.createdAtUtc = timestamp;
                profile.updatedAtUtc = timestamp;
                profile.revision = 0;
                var claimed = definition.Page != "home"
                    && !string.Equals(definition.Id, "activity-claimable",
                        StringComparison.Ordinal)
                    && !string.Equals(definition.Id, "activity-claiming",
                        StringComparison.Ordinal)
                    && !string.Equals(definition.Id, "activity-error",
                        StringComparison.Ordinal)
                    && !string.Equals(definition.Id, "activity-save-failure",
                        StringComparison.Ordinal)
                    && !string.Equals(definition.Id, "equipment-locked",
                        StringComparison.Ordinal)
                    && !string.Equals(definition.Id, "cultivation-locked",
                        StringComparison.Ordinal);
                if (string.Equals(definition.Id, "activity-claimed",
                        StringComparison.Ordinal)
                    || string.Equals(definition.Id, "home-policy-preview",
                        StringComparison.Ordinal))
                    claimed = true;
                if (claimed)
                {
                    profile.revision = 1;
                    profile.itemBalances = new[]
                    {
                        new PlayerItemBalance
                        {
                            itemId = OutgameContentIds.Items.MorningDew,
                            quantity = 6,
                        },
                    };
                    profile.activityReceipts = new[]
                    {
                        new PlayerActivityReceipt
                        {
                            receiptId = OutgameContentIds.Receipts.StarterSupplies,
                        },
                    };
                    profile.ownedGrowthEquipment = new[]
                    {
                        new PlayerGrowthEquipment
                        {
                            growthEquipmentId = OutgameContentIds
                                .GrowthEquipment.SunleafEmblem,
                            rank = 0,
                        },
                    };
                }

                if (string.Equals(definition.Id, "home-policy-preview",
                        StringComparison.Ordinal))
                {
                    profile.revision = 3;
                    profile.itemBalances[0].quantity = 0;
                    profile.ownedGrowthEquipment[0].rank = 1;
                    profile.growthLoadout = EquipmentLoadout();
                    profile.cultivationRanks = new[]
                    {
                        new PlayerCultivationRank
                        {
                            cultivationNodeId = OutgameContentIds.CultivationNodes
                                .VitalRoots,
                            rank = 1,
                        },
                    };
                    profile.lastSelectedLevelId =
                        LobbyHubPresenter.Orchard02LevelId;
                }
                else if (string.Equals(definition.Id, "equipment-insufficient",
                             StringComparison.Ordinal))
                {
                    profile.itemBalances[0].quantity = 2;
                    profile.ownedGrowthEquipment[0].rank = 1;
                    profile.growthLoadout = EquipmentLoadout();
                }
                else if (string.Equals(definition.Id, "equipment-selected",
                             StringComparison.Ordinal))
                {
                    profile.growthLoadout = EquipmentLoadout();
                }
                else if (string.Equals(definition.Id, "equipment-maximum",
                             StringComparison.Ordinal))
                {
                    profile.itemBalances[0].quantity = 0;
                    profile.ownedGrowthEquipment[0].rank = 2;
                    profile.growthLoadout = EquipmentLoadout();
                }
                else if (string.Equals(definition.Id, "equipment-loading",
                             StringComparison.Ordinal)
                         || string.Equals(definition.Id, "equipment-error",
                             StringComparison.Ordinal)
                         || string.Equals(definition.Id,
                             "equipment-save-failure", StringComparison.Ordinal))
                {
                    profile.growthLoadout = EquipmentLoadout();
                }

                if (string.Equals(definition.Id, "cultivation-insufficient",
                        StringComparison.Ordinal))
                    profile.itemBalances[0].quantity = 2;
                else if (string.Equals(definition.Id, "cultivation-maximum",
                             StringComparison.Ordinal))
                {
                    profile.itemBalances[0].quantity = 0;
                    profile.cultivationRanks = new[]
                    {
                        new PlayerCultivationRank
                        {
                            cultivationNodeId = OutgameContentIds.CultivationNodes
                                .VitalRoots,
                            rank = 2,
                        },
                    };
                }
                return profile;
            }

            private static PlayerGrowthLoadoutEntry[] EquipmentLoadout()
            {
                return new[]
                {
                    new PlayerGrowthLoadoutEntry
                    {
                        slotId = OutgameContentIds.GrowthSlots.Offense,
                        growthEquipmentId = OutgameContentIds.GrowthEquipment
                            .SunleafEmblem,
                    },
                };
            }
        }
    }
}
#endif
