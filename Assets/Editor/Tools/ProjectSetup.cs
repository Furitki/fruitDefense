using System.Collections.Generic;
using FruitDefense.App;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Presentation;
using FruitDefense.Shell;
using FruitDefense.Tilemaps;
using FruitDefense.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FruitDefense.Editor
{
    public static class ProjectSetup
    {
        internal const string BattlefieldGrassTileSetPath =
            "Assets/LayeredTerrain/CompositeBrushes/GrassSoil/GrassSoilCompositeTileSet.asset";
        internal const string BattlefieldRouteTileSetPath =
            SquareTerrainArtProfile.StoneRoadLandformTileSetPath;
        internal const string BattlefieldTerrainBaseTexturePath =
            "Assets/LayeredTerrain/CompositeBrushes/GrassSoil/Runtime64/Mask-00.png";
        internal const string BattlefieldTerrainPalettePath =
            "Assets/Battlefield/Terrain/OrchardDefaultTerrainPalette.asset";
        internal const string ReleaseRuntimeUiThemePath =
            "Assets/UI/Theme/ReleaseRuntimeUiTheme.asset";
        internal const string ReleaseRuntimeUiArtSetPath =
            "Assets/UI/Art/Sets/SunnyOrchardPaintedRuntimeUiArtSet.asset";
        internal const string PackagedRuntimeUiFontPath =
            "Assets/Resources/Fonts/NotoSansSC-UI.ttf";
        [MenuItem("Fruit Defense/Configure Project")]
        public static void Configure()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            var runtimeUiTheme = EnsureReleaseRuntimeUiTheme();
            CreateBootstrapScene(runtimeUiTheme);
            CreateComponentScene<LobbyPresenter>("Lobby", "LobbyPresenter");
            CreateBattleScene();
            CreateComponentScene<SettlementPresenter>("Settlement", "SettlementPresenter");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Lobby.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Battle.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Settlement.unity", true),
            };

            PlayerSettings.companyName = "Fruit Defense";
            PlayerSettings.productName = "水果塔防";
            PlayerSettings.defaultScreenWidth = 1206;
            PlayerSettings.defaultScreenHeight = 2622;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.fruitdefense.game");
            QualitySettings.vSyncCount = 1;
            AssetDatabase.SaveAssets();
            Debug.Log("Fruit Defense project configured: Bootstrap, Lobby, Battle, Settlement");
        }

        private static void CreateBootstrapScene(RuntimeUiTheme runtimeUiTheme)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("AppBootstrap");
            root.AddComponent<AppBootstrap>();
            AssignRuntimeUiTheme(root.AddComponent<AppFlowCoordinator>(), runtimeUiTheme);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Bootstrap.unity");
        }

        public static void ConfigureReleaseRuntimeUiTheme()
        {
            var runtimeUiTheme = EnsureReleaseRuntimeUiTheme();
            var bootstrap = SceneManager.GetSceneByPath("Assets/Scenes/Bootstrap.unity");
            var openedForBinding = false;
            if (!bootstrap.IsValid() || !bootstrap.isLoaded)
            {
                bootstrap = EditorSceneManager.OpenScene(
                    "Assets/Scenes/Bootstrap.unity", OpenSceneMode.Additive);
                openedForBinding = true;
            }

            try
            {
                AppFlowCoordinator coordinator = null;
                foreach (var root in bootstrap.GetRootGameObjects())
                {
                    coordinator = root.GetComponentInChildren<AppFlowCoordinator>(true);
                    if (coordinator != null) break;
                }
                if (coordinator == null)
                    throw new System.InvalidOperationException(
                        "Release Bootstrap scene does not contain AppFlowCoordinator.");

                AssignRuntimeUiTheme(coordinator, runtimeUiTheme);
                EditorSceneManager.MarkSceneDirty(bootstrap);
                if (!EditorSceneManager.SaveScene(bootstrap))
                    throw new System.InvalidOperationException(
                        "Failed to save the release Bootstrap theme binding.");
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (openedForBinding && bootstrap.IsValid() && bootstrap.isLoaded)
                    EditorSceneManager.CloseScene(bootstrap, true);
            }
        }

        internal static RuntimeUiTheme RequireReleaseRuntimeUiTheme()
        {
            var runtimeUiTheme = AssetDatabase.LoadAssetAtPath<RuntimeUiTheme>(
                ReleaseRuntimeUiThemePath);
            if (runtimeUiTheme == null)
                throw new System.InvalidOperationException(
                    "Release runtime UI theme is missing: " + ReleaseRuntimeUiThemePath);

            var validation = runtimeUiTheme.Validate();
            if (!validation.IsValid)
                throw new System.InvalidOperationException(
                    "Release runtime UI theme is invalid: " + validation.Issues[0]);
            if (runtimeUiTheme.ThemeId != "ui.sunny-orchard" || runtimeUiTheme.Revision != "1")
                throw new System.InvalidOperationException(
                    "Release runtime UI theme identity must be ui.sunny-orchard@1.");
            if (!ReferenceEquals(runtimeUiTheme.PackagedChineseFont,
                    AssetDatabase.LoadAssetAtPath<Font>(PackagedRuntimeUiFontPath)))
                throw new System.InvalidOperationException(
                    "Release runtime UI theme must use the packaged NotoSansSC-UI font.");
            if (runtimeUiTheme.ActiveArtSet == null
                || runtimeUiTheme.ActiveArtSet.SetId != "sunny-orchard-painted"
                || runtimeUiTheme.ActiveArtSet.Revision != "1"
                || AssetDatabase.GetAssetPath(runtimeUiTheme.ActiveArtSet)
                    != ReleaseRuntimeUiArtSetPath)
                throw new System.InvalidOperationException(
                    "Release runtime UI theme must activate sunny-orchard-painted@1.");
            return runtimeUiTheme;
        }

        private static RuntimeUiTheme EnsureReleaseRuntimeUiTheme()
        {
            EnsureFolder("Assets/UI");
            EnsureFolder("Assets/UI/Theme");

            var runtimeUiTheme = AssetDatabase.LoadAssetAtPath<RuntimeUiTheme>(
                ReleaseRuntimeUiThemePath);
            if (runtimeUiTheme == null)
            {
                runtimeUiTheme = ScriptableObject.CreateInstance<RuntimeUiTheme>();
                runtimeUiTheme.name = "ReleaseRuntimeUiTheme";
                AssetDatabase.CreateAsset(runtimeUiTheme, ReleaseRuntimeUiThemePath);
            }

            var packagedFont = AssetDatabase.LoadAssetAtPath<Font>(PackagedRuntimeUiFontPath);
            var artSet = AssetDatabase.LoadAssetAtPath<RuntimeUiArtSet>(
                ReleaseRuntimeUiArtSetPath);
            if (packagedFont == null)
                throw new System.InvalidOperationException(
                    "Packaged runtime UI font is missing: " + PackagedRuntimeUiFontPath);
            if (artSet == null)
                throw new System.InvalidOperationException(
                    "Release runtime UI art set is missing: "
                    + ReleaseRuntimeUiArtSetPath);

            var serializedTheme = new SerializedObject(runtimeUiTheme);
            serializedTheme.FindProperty("themeId").stringValue = "ui.sunny-orchard";
            serializedTheme.FindProperty("revision").stringValue = "1";
            serializedTheme.FindProperty("packagedChineseFont").objectReferenceValue = packagedFont;
            serializedTheme.FindProperty("activeArtSet").objectReferenceValue = artSet;
            serializedTheme.FindProperty("colors").FindPropertyRelative("primaryAction")
                .colorValue = new Color32(85, 154, 57, 255);
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(runtimeUiTheme);
            AssetDatabase.SaveAssetIfDirty(runtimeUiTheme);
            return RequireReleaseRuntimeUiTheme();
        }

        private static void AssignRuntimeUiTheme(
            AppFlowCoordinator coordinator, RuntimeUiTheme runtimeUiTheme)
        {
            if (coordinator == null)
                throw new System.ArgumentNullException(nameof(coordinator));
            if (runtimeUiTheme == null)
                throw new System.ArgumentNullException(nameof(runtimeUiTheme));

            var serializedCoordinator = new SerializedObject(coordinator);
            var property = serializedCoordinator.FindProperty("runtimeUiTheme");
            if (property == null)
                throw new System.InvalidOperationException(
                    "AppFlowCoordinator runtimeUiTheme binding field is missing.");
            property.objectReferenceValue = runtimeUiTheme;
            serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(coordinator);
        }

        private static void CreateComponentScene<T>(string sceneName, string objectName) where T : Component
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(objectName);
            root.AddComponent<T>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + sceneName + ".unity");
        }

        private static void CreateBattleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("FruitDefenseGame");
            var game = root.AddComponent<FruitDefenseGame>();
            ConfigureBattlefieldTerrain(game);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Battle.unity");
        }

        internal static void ConfigureBattlefieldTerrain(FruitDefenseGame game)
        {
            if (game == null) throw new System.ArgumentNullException(nameof(game));
            EnsureFolder("Assets/Battlefield");
            EnsureFolder("Assets/Battlefield/Terrain");
            var palette = LayeredTerrainArtSetup.EnsurePaletteAssets();
            game.ConfigureBattlefieldTerrain(new[] { palette });
            EditorUtility.SetDirty(game);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var separator = path.LastIndexOf('/');
            if (separator <= 0) throw new System.ArgumentException("Invalid asset folder path: " + path);
            var parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }

        [MenuItem("Fruit Defense/Validation/Run Project Smoke Validation")]
        public static void SmokeValidate()
        {
            ValidateLegacyMigrationBaseline();
            SquareTerrainArtSmoke.Run();
            ValidateBattlefieldDefinition();
            ValidateBattlefieldDeterminism();
            ValidateBattlefieldProjectionGeometry();
            ValidateBattlefieldViewportMatrix();
            ValidateDragRegressionCoverage();
            BattlefieldLayeredMapSmoke.Validate();
            CanonicalBattlefieldMapAuthoringSmoke.Validate();
            CanonicalBattlefieldMapEditorSmoke.Validate();
            CanonicalBattlefieldMapPublicationSmoke.Validate();
            DualGridTilemapSmoke.Validate();
            LayeredTerrainTilemapSmoke.Validate();
            LayeredTerrainPainterSmoke.Validate();
            TerrainBrushRegistrySmoke.Validate();
            BattlefieldDualGridTerrainSmoke.Validate();
            PlantInteractionPresentationSmoke.Run();
            var simulation = new GameSimulation(12345);
            Assert(simulation.State.Pots.Count == 8, "initial pot count");
            foreach (var group in simulation.Map.InitialPotGroups.Values)
                Assert(simulation.State.Pots.FindAll(pot => System.Linq.Enumerable.Contains(group.Cells, pot.Cell)).Count == group.InitialCount,
                    "initial pot distribution in semantic group " + group.Name);
            Assert(simulation.State.Sun == 10 && simulation.State.Lives == 10, "initial resources");
            Assert(GameConfig.GetWave(1).Sequence.Count == 5, "wave 1 count");
            Assert(GameConfig.GetWave(6).Sequence.Count == (9 + 5 + 2) * 3, "wave 6 count scaling");
            Assert(Mathf.Approximately(GameConfig.WaveHpMultiplier(3), 2f), "wave health scaling");
            Assert(GameConfig.PlantingCells.Count == 35 && simulation.State.Pots.Count < GameConfig.PlantingCells.Count,
                "orchard-01 exposes 35 plantable cells with visible empty cells");
            Assert(simulation.RefreshNursery(out _), "first nursery refresh");
            Assert(simulation.State.Plants.Count + simulation.LastNurseryPotSlots.Count == 5, "nursery result count includes pots");
            var plant = simulation.State.Plants[0];
            var pot = simulation.State.Pots[0];
            var plantDrop = simulation.GetPlantDropStatus(plant.Id, pot.Id);
            Assert(plantDrop.Legal && plantDrop.Action == PlantDropAction.Plant, "nursery plant can be dragged to pot");
            Assert(simulation.MoveOrMergePlant(plant.Id, pot.Id, out _), "plant drop commits");
            var nurseryDrop = simulation.GetNurseryDropStatus(plant.Id, 0);
            Assert(nurseryDrop.Legal && nurseryDrop.Action == PlantDropAction.Move, "planted fruit can return to nursery");
            simulation.State.Inventory.Ice = 1;
            Assert(simulation.GetWeaponInstallStatus(plant.Id, WeaponKind.Ice).Legal, "weapon can be dragged to plant");
            simulation.State.Inventory.Pots = 1;
            var expansion = System.Linq.Enumerable.First(GameConfig.PlantingCells, cell => simulation.CanExpand(cell));
            Assert(simulation.CanExpand(expansion), "pot can be dragged to legal expansion cell");
            simulation.State.Phase = GamePhase.Playing;
            plant.MoveCooldown = 1f;
            var otherPot = simulation.State.Pots.Find(candidate => candidate.Id != pot.Id);
            Assert(!simulation.GetPlantDropStatus(plant.Id, otherPot.Id).Legal, "move cooldown blocks drag drop");
            var overwritten = simulation.State.Plants.Find(candidate => candidate.NurseryIndex >= 0);
            Assert(overwritten != null, "occupied nursery has a refresh replacement target");
            overwritten.Weapon = WeaponKind.Ice;
            var iceBeforeRefresh = simulation.State.Inventory.Ice;
            simulation.State.Sun = 100;
            Assert(simulation.RefreshNursery(out _), "occupied nursery can refresh");
            Assert(simulation.PlantById(overwritten.Id) == null, "refresh replaces occupied nursery fruit");
            Assert(simulation.State.Inventory.Ice == iceBeforeRefresh + 1, "refresh recovers overwritten weapon");
            Assert(simulation.State.Plants.FindAll(candidate => candidate.NurseryIndex >= 0).Count
                + simulation.LastNurseryPotSlots.Count == 5, "replacement refresh fills five result slots");

            var foundPotReward = false;
            for (var seed = 1; seed <= 256 && !foundPotReward; seed++)
            {
                var potRoll = new GameSimulation(seed);
                Assert(potRoll.RefreshNursery(out _), "pot roll refresh succeeds");
                foundPotReward = potRoll.LastNurseryPotSlots.Count > 0
                    && potRoll.State.Inventory.Pots == potRoll.LastNurseryPotSlots.Count;
            }
            Assert(foundPotReward, "nursery refresh can roll and auto-store flowerpots");
            ValidateMigrationBehavior();
            ValidateCombatActions();
            CombatFrameworkSmoke.Run();
            var collisionTarget = new Rect(100f, 100f, 40f, 40f);
            var cursorOutsideTarget = new Vector2(160f, 160f);
            var preview = DragGeometry.PreviewRect(cursorOutsideTarget);
            Assert(!collisionTarget.Contains(cursorOutsideTarget), "drag cursor remains outside collision target");
            Assert(DragGeometry.OverlapArea(preview, collisionTarget) > 0f, "drag preview overlaps target independently of cursor");
            var bestTarget = DragGeometry.BestOverlapIndex(preview, new[]
            {
                collisionTarget,
                new Rect(142f, 142f, 30f, 30f),
            });
            Assert(bestTarget == 0, "largest preview overlap wins drop target selection");
            Assert(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/TempArt/fruit-defense-temp-atlas.png") != null,
                "temporary art atlas imported");
            Assert(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/TempArt/combat-vfx-atlas.png") != null,
                "temporary combat effect atlas imported");
            var uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/NotoSansSC-UI.ttf");
            Assert(uiFont != null, "bundled WebGL UI font imported");
            Assert(RuntimeUiChineseGlyphCoverage.TryFindMissingGlyph(uiFont,
                    out var missingRuntimeUiGlyph),
                "bundled WebGL UI font covers the authoritative release UI glyph probe; missing: "
                + missingRuntimeUiGlyph);
            Assert(FruitDefenseGame.ValidatePortraitLayout(out var portraitLayoutReason),
                "portrait layout geometry: " + portraitLayoutReason);
            Assert(FruitDefenseGame.ValidateInspectionOnlyInteraction(out var inspectionReason),
                "inspection-only interaction contract: " + inspectionReason);
            Assert(FruitDefenseGame.ValidateSessionControlContract(out var sessionControlReason),
                "session control contract: " + sessionControlReason);
            ValidateP1LevelCatalogPath();
            ValidateP0SceneConfiguration();
            ValidateBootstrapRuntimeUiPresentation();
            BattleUiLayoutSmoke.Run();
            RuntimeUiQualitySmoke.Run();
            RuntimeUiFeedbackTimingSmoke.Run();
            RuntimeUiGlyphCoverageSmoke.Run();
            RuntimeUiPerformanceSmoke.Run();
            RuntimeUiVisualSystemValidator.ValidateReleaseOrThrow();
            RuntimeUiVisualSystemSmoke.Run();
            Debug.Log("FRUIT_DEFENSE_SMOKE_OK");
        }

        private static void ValidateP1LevelCatalogPath()
        {
            LevelMapCatalogSmoke.Run();
            MultiLevelSimulationSmoke.Run();
            BattleSnapshotV2Smoke.Run();
            BattleSessionHostSmoke.Run();
            ShellFlowValidation.SmokeValidate(RequireReleaseRuntimeUiTheme());
        }

        private static void ValidateP0SceneConfiguration()
        {
            var runtimeUiTheme = RequireReleaseRuntimeUiTheme();
            var themeGuids = AssetDatabase.FindAssets("t:RuntimeUiTheme", new[] { "Assets" });
            Assert(themeGuids.Length == 1,
                "exactly one release RuntimeUiTheme asset exists under Assets");
            var expected = new[]
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Lobby.unity",
                "Assets/Scenes/Battle.unity",
                "Assets/Scenes/Settlement.unity",
            };
            Assert(EditorBuildSettings.scenes.Length == expected.Length, "four release scenes configured");
            for (var index = 0; index < expected.Length; index++)
            {
                Assert(EditorBuildSettings.scenes[index].enabled, "release scene enabled: " + expected[index]);
                Assert(EditorBuildSettings.scenes[index].path == expected[index], "release scene order: " + expected[index]);
                Assert(AssetDatabase.LoadAssetAtPath<SceneAsset>(expected[index]) != null,
                    "release scene exists: " + expected[index]);
            }

            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            var canRestoreSetup = System.Array.Exists(previousSetup, setup => setup.isLoaded && setup.isActive);
            var bootstrap = default(Scene);
            try
            {
                bootstrap = SceneManager.GetSceneByPath(expected[0]);
                if (!bootstrap.IsValid() || !bootstrap.isLoaded)
                    bootstrap = EditorSceneManager.OpenScene(expected[0], OpenSceneMode.Additive);
                var bootstrapCount = 0;
                var coordinatorCount = 0;
                AppFlowCoordinator coordinator = null;
                foreach (var root in bootstrap.GetRootGameObjects())
                {
                    bootstrapCount += root.GetComponentsInChildren<AppBootstrap>(true).Length;
                    coordinatorCount += root.GetComponentsInChildren<AppFlowCoordinator>(true).Length;
                    if (coordinator == null)
                        coordinator = root.GetComponentInChildren<AppFlowCoordinator>(true);
                }
                Assert(bootstrapCount == 1, "exactly one AppBootstrap in release Bootstrap scene");
                Assert(coordinatorCount == 1, "exactly one AppFlowCoordinator in release Bootstrap scene");
                Assert(coordinator != null
                    && ReferenceEquals(coordinator.RuntimeUiTheme, runtimeUiTheme),
                    "release Bootstrap coordinator owns the one release runtime UI theme");
            }
            finally
            {
                if (canRestoreSetup)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else if (bootstrap.IsValid() && bootstrap.isLoaded)
                    EditorSceneManager.CloseScene(bootstrap, true);
            }

            for (var sceneIndex = 1; sceneIndex < expected.Length; sceneIndex++)
            {
                var dependencies = AssetDatabase.GetDependencies(expected[sceneIndex], false);
                for (var dependencyIndex = 0;
                     dependencyIndex < dependencies.Length;
                     dependencyIndex++)
                {
                    var dependency = AssetDatabase.LoadMainAssetAtPath(
                        dependencies[dependencyIndex]);
                    Assert(!(dependency is RuntimeUiTheme) && !(dependency is RuntimeUiArtSet),
                        "release route scene has no direct runtime UI theme/art set reference: "
                        + expected[sceneIndex]);
                }
            }
        }

        private static void ValidateBootstrapRuntimeUiPresentation()
        {
            var runtimeUiTheme = RequireReleaseRuntimeUiTheme();
            for (var index = 0;
                 index < RuntimeUiQualityProfile.Viewports.Count; index++)
            {
                var viewportCase = RuntimeUiQualityProfile.Viewports[index];
                ValidateBootstrapPresentationCase(runtimeUiTheme,
                    viewportCase.Viewport, viewportCase.FullSafeArea,
                    "full-safe-area");
                ValidateBootstrapPresentationCase(runtimeUiTheme,
                    viewportCase.Viewport, viewportCase.InsetSafeArea,
                    "top-" + viewportCase.SafeTop
                    + "-bottom-" + viewportCase.SafeBottom);
            }

            AssertBlockingErrorCopy(RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapLevelUnavailable).Text,
                AppFlowCoordinator.LevelResolutionFailed);
            AssertBlockingErrorCopy(RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapConfigurationUnavailable).Text,
                AppFlowCoordinator.RuntimeConfigInvalid,
                AppFlowCoordinator.RuntimeUiThemeInvalid + ":invalid-theme");
            AssertBlockingErrorCopy(RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapContentUnavailable).Text,
                AppFlowCoordinator.BundledContentInvalid,
                AppFlowCoordinator.BundledLevelCatalogInvalid,
                AppFlowCoordinator.BundledContentMismatch);
            AssertBlockingErrorCopy(RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapPageUnavailable).Text,
                AppFlowCoordinator.SceneUnavailable,
                AppFlowCoordinator.SceneLoadFailed,
                AppFlowCoordinator.BattleHostMissing,
                AppFlowCoordinator.LobbyPresenterMissing,
                AppFlowCoordinator.SettlementPresenterMissing);
            AssertBlockingErrorCopy(RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapUnknownFailure).Text,
                "unknown-error");
            ValidateBootstrapSharedStateSource();
        }

        private static void ValidateBootstrapPresentationCase(RuntimeUiTheme runtimeUiTheme,
            Vector2Int viewport, Rect safeArea, string caseName)
        {
            var noActionLayout = AppFlowCoordinator.CreateBootstrapPresentationLayout(
                viewport.x, viewport.y, safeArea);
            var layout = AppFlowCoordinator.CreateBootstrapPresentationLayout(
                viewport.x, viewport.y, safeArea, true);
            var viewportRect = new Rect(0f, 0f, viewport.x, viewport.y);
            Assert(noActionLayout.Scale > 0f
                && ContainsRect(viewportRect, noActionLayout.SafeArea)
                && ContainsRect(noActionLayout.SafeArea, noActionLayout.Modal)
                && ContainsRect(noActionLayout.Modal, noActionLayout.Title)
                && ContainsRect(noActionLayout.Modal, noActionLayout.Status)
                && noActionLayout.RetryAction.width == 0f
                && noActionLayout.RetryAction.height == 0f,
                "Bootstrap no-action presentation remains finite for "
                + viewport + " " + caseName);
            Assert(layout.Scale > 0f
                && ContainsRect(viewportRect, layout.SafeArea)
                && ContainsRect(layout.SafeArea, layout.Modal)
                && ContainsRect(layout.Modal, layout.Title)
                && ContainsRect(layout.Modal, layout.Status)
                && ContainsRect(layout.Modal, layout.RetryAction)
                && ContainsRect(layout.SafeArea, layout.RecoverableStatus),
                "Bootstrap presentation remains contained for " + viewport + " " + caseName);
            Assert(Mathf.Min(layout.RetryAction.width, layout.RetryAction.height)
                    >= runtimeUiTheme.Metrics.TouchTargetMinimum * layout.Scale - .001f,
                "Bootstrap retry action retains the theme touch target for "
                + viewport + " " + caseName);

            if (viewport == new Vector2Int(402, 874)
                && RectApproximately(safeArea, new Rect(0f, 0f, 402f, 874f)))
            {
                Assert(RectApproximately(noActionLayout.Modal,
                        new Rect(21f, 262f, 360f, 142f), .01f)
                    && RectApproximately(noActionLayout.Title,
                        new Rect(41f, 278f, 320f, 34f), .01f)
                    && RectApproximately(noActionLayout.Status,
                        new Rect(41f, 318f, 320f, 45f), .01f)
                    && RectApproximately(layout.Modal,
                        new Rect(21f, 262f, 360f, 190f), .01f)
                    && RectApproximately(layout.RetryAction,
                        new Rect(41f, 367f, 320f, 52f), .01f),
                    "Bootstrap 402 full geometry matches the approved quality audit");
            }

        }

        private static void AssertBlockingErrorCopy(string expected, params string[] rawErrors)
        {
            for (var index = 0; index < rawErrors.Length; index++)
            {
                Assert(AppFlowCoordinator.FormatBootstrapBlockingError(rawErrors[index]) == expected,
                    "Bootstrap blocking error uses finite shared copy: " + rawErrors[index]);
            }
        }

        private static void ValidateBootstrapSharedStateSource()
        {
            var sourcePath = System.IO.Path.Combine(
                Application.dataPath, "Scripts/App/AppFlowCoordinator.cs");
            var source = System.IO.File.ReadAllText(sourcePath);
            const string startToken = "private void OnGUI(";
            const string endToken = "private readonly struct SceneLoadResult";
            var start = source.IndexOf(startToken, System.StringComparison.Ordinal);
            var end = source.IndexOf(endToken, start + startToken.Length,
                System.StringComparison.Ordinal);
            Assert(start >= 0 && end > start,
                "Bootstrap shared presentation source boundaries are present");
            var presentation = source.Substring(start, end - start);
            var requiredTokens = new[]
            {
                "RuntimeUiGui.RequireContext",
                "RuntimeUiGui.DrawScreenBackground",
                "RuntimeUiGui.DrawSafeArea",
                "RuntimeUiGui.DrawScreenCorners",
                "RuntimeUiGui.DrawBlockingModal",
                "RuntimeUiGui.DrawSingleLineText",
                "RuntimeUiGui.DrawStatus",
                "RuntimeUiGui.DrawAction",
                "RuntimeUiInteractionState.Warning",
                "RuntimeUiInteractionState.Error",
                "RuntimeUiInteractionState.Loading",
                "RuntimeUiActionKind.Primary",
                "RuntimeUiInteractionState.Normal",
                "RuntimeUiArtSlot.IconControlRetry",
                "FormatBootstrapBlockingError",
                "_bootstrap.TryRetryInitialization()",
            };
            for (var index = 0; index < requiredTokens.Length; index++)
            {
                Assert(presentation.Contains(requiredTokens[index]),
                    "Bootstrap Loading/Error/Retry presentation uses shared state: "
                    + requiredTokens[index]);
            }

            var forbiddenTokens = new[]
            {
                "GUI.Box(",
                "GUI.Label(",
                "GUI.Button(",
                "GUI.skin",
                "GUIStyle.none",
                "Texture2D.whiteTexture",
                "Resources.Load",
            };
            for (var index = 0; index < forbiddenTokens.Length; index++)
            {
                Assert(!presentation.Contains(forbiddenTokens[index]),
                    "Bootstrap presentation has no default-skin/source fallback: "
                    + forbiddenTokens[index]);
            }
        }

        private static void ValidateLegacyMigrationBaseline()
        {
            const float legacyRouteLength = 228f;
            const float legacyNormalSpeed = 4.4f;
            const float expectedTraversalSeconds = legacyRouteLength / legacyNormalSpeed;
            const float previousBoardWidth = 386f;
            const float previousBoardHeight = 320f;
            const float previousBoardScale = previousBoardWidth / 1050f;
            const float previousPotSize = 62f * previousBoardScale;
            const float previousHorizontalCellPitch = previousBoardWidth * 8f / 100f;
            const float previousVerticalCellPitch = previousBoardHeight * 10f / 100f;

            Assert(Mathf.Approximately(previousPotSize, 22.79238f), "legacy reference flowerpot geometry recorded");
            Assert(Mathf.Approximately(previousHorizontalCellPitch, 30.88f)
                && Mathf.Approximately(previousVerticalCellPitch, 32f), "legacy reference cell pitch recorded");
            Assert(Mathf.Approximately(GameConfig.PathLength / GameConfig.Zombie(ZombieKind.Normal).Speed, expectedTraversalSeconds),
                "normal zombie route duration preserved from legacy baseline");

            var legacyNear = Vector2.Distance(new Vector2(17f, 25f), new Vector2(17f, 12f));
            var legacyRepresentative = Mathf.Min(
                Vector2.Distance(new Vector2(41f, 55f), new Vector2(41f, 12f)),
                Vector2.Distance(new Vector2(41f, 55f), new Vector2(41f, 88f)));
            Assert(legacyNear <= 18f && legacyRepresentative <= 44f && legacyRepresentative > 18f,
                "legacy representative target coverage recorded");

            var map = GameConfig.DefaultBattlefield;
            var nearDistance = DistanceToRoute(map, map.CellToMap(new Vector2Int(0, 0)));
            var representativeDistance = DistanceToRoute(map, map.CellToMap(new Vector2Int(3, 3)));
            Assert(nearDistance <= GameConfig.Plant(PlantKind.Durian).Range
                && representativeDistance <= GameConfig.Plant(PlantKind.Pea).Range
                && representativeDistance > GameConfig.Plant(PlantKind.Durian).Range,
                "representative target coverage preserved after map-unit migration");
        }

        private static void ValidateBattlefieldDefinition()
        {
            var map = GameConfig.DefaultBattlefield;
            Assert(map.Validate(out var reason), "default battlefield topology: " + reason);
            Assert(map.MapId == BattlefieldMapDefinition.DefaultMapId
                && map.GridWidth == 8 && map.GridHeight == 7
                && map.RouteCells.Count == 20 && map.PlantableCells.Count == 35,
                "orchard-01 is an 8-by-7 grid with 20 route cells and 35 plantable cells");
            Assert(map.UsesLayeredMap
                && System.Linq.Enumerable.Count(map.VisualSurfaceIds,
                    surface => surface == BattlefieldLayerIds.Surfaces.Grass) == 35
                && System.Linq.Enumerable.Count(map.VisualSurfaceIds,
                    surface => surface == BattlefieldLayerIds.Surfaces.StoneRoad) == 0
                && System.Linq.Enumerable.Count(map.VisualSurfaceIds,
                    surface => surface == BattlefieldLayerIds.Surfaces.Soil) == 21
                && System.Linq.Enumerable.Count(map.GameplayCells,
                    cell => cell.Has(BattlefieldCellCapabilities.Plantable)) == 35
                && System.Linq.Enumerable.Count(map.GameplayCells,
                    cell => cell.Has(BattlefieldCellCapabilities.EnemyTraversable)) == 20,
                "orchard-01 uses grass landforms and base-only dirt routes independently from gameplay capabilities");
            Assert(map.PlantableCells.Count == System.Linq.Enumerable.Count(System.Linq.Enumerable.Distinct(map.PlantableCells)),
                "default battlefield cells are unique");

            var expectedRoute = new List<Vector2Int>(20);
            for (var column = 0; column < 8; column++) expectedRoute.Add(new Vector2Int(column, 0));
            for (var row = 1; row < 7; row++) expectedRoute.Add(new Vector2Int(7, row));
            for (var column = 6; column >= 1; column--) expectedRoute.Add(new Vector2Int(column, 6));
            Assert(map.RouteTileDescriptors.Count == expectedRoute.Count,
                "orchard-01 derives one descriptor for every ordered route cell");
            for (var index = 0; index < expectedRoute.Count; index++)
            {
                Assert(map.RouteCells[index] == expectedRoute[index],
                    "orchard-01 ordered route cell at index " + index);
                Assert(map.RouteTileDescriptors[index].Cell == expectedRoute[index]
                    && map.TryGetRouteTile(expectedRoute[index], out var descriptor)
                    && descriptor.Cell == expectedRoute[index],
                    "orchard-01 route descriptor lookup at index " + index);
            }
            Assert(map.EntryCell == new Vector2Int(0, 0) && map.ExitCell == new Vector2Int(1, 6)
                && map.CoreCell == new Vector2Int(0, 6)
                && map.EnemySpawnCell == map.EntryCell && map.RouteGoalCell == map.ExitCell
                && System.Linq.Enumerable.Count(map.Markers,
                    marker => marker.Kind == BattlefieldMarkerKind.EnemySpawn) == 1
                && System.Linq.Enumerable.Count(map.Markers,
                    marker => marker.Kind == BattlefieldMarkerKind.RouteGoal) == 1
                && System.Linq.Enumerable.Count(map.Markers,
                    marker => marker.Kind == BattlefieldMarkerKind.Core) == 1,
                "orchard-01 spawn, route goal, and core resolve from typed markers");

            AssertRouteDescriptor(map, 0, BattlefieldRouteTileKind.Entry,
                BattlefieldDirection.None, BattlefieldDirection.East);
            AssertRouteDescriptor(map, 1, BattlefieldRouteTileKind.Horizontal,
                BattlefieldDirection.West, BattlefieldDirection.East);
            AssertRouteDescriptor(map, 7, BattlefieldRouteTileKind.CornerSouthWest,
                BattlefieldDirection.West, BattlefieldDirection.South);
            AssertRouteDescriptor(map, 8, BattlefieldRouteTileKind.Vertical,
                BattlefieldDirection.North, BattlefieldDirection.South);
            AssertRouteDescriptor(map, 13, BattlefieldRouteTileKind.CornerNorthWest,
                BattlefieldDirection.North, BattlefieldDirection.West);
            AssertRouteDescriptor(map, 19, BattlefieldRouteTileKind.Exit,
                BattlefieldDirection.East, BattlefieldDirection.None);
            ValidateRouteDescriptorOrientations();

            Assert(map.InitialPotGroups.Count == 3
                && System.Linq.Enumerable.Sum(map.InitialPotGroups.Values, group => group.InitialCount) == GameConfig.InitialPotCount,
                "semantic groups place eight initial flowerpots");
            var initialCells = new HashSet<Vector2Int>();
            foreach (var groupName in map.InitialPotGroupOrder)
            {
                Assert(map.InitialPotGroups.TryGetValue(groupName, out var group),
                    "initial flowerpot group is addressable: " + groupName);
                foreach (var cell in group.Cells)
                    Assert(map.IsPlantable(cell) && initialCells.Add(cell),
                        "initial flowerpot cell is unique and plantable: " + groupName + " " + cell);
            }
            Assert(initialCells.Count == P0GameplayParityBaseline.InitialPotCount,
                "orchard-01 exposes exactly eight unique plantable initial flowerpot cells");

            var center = new Vector2Int(3, 3);
            Assert(System.Linq.Enumerable.Count(map.Topology.CardinalNeighbors(center)) == 4
                && map.Topology.AreCardinalNeighbors(center, center + Vector2Int.right)
                && !map.Topology.AreCardinalNeighbors(center, center + Vector2Int.one),
                "topology exposes cardinal neighbors and rejects diagonals");
        }

        private static void AssertRouteDescriptor(BattlefieldMapDefinition map, int routeIndex,
            BattlefieldRouteTileKind kind, BattlefieldDirection previous, BattlefieldDirection next)
        {
            Assert(map.Topology.TryDescribeRouteCell(routeIndex, out var descriptor, out var reason),
                "route descriptor can be derived at index " + routeIndex + ": " + reason);
            Assert(descriptor.Kind == kind && descriptor.PreviousConnection == previous
                && descriptor.NextConnection == next,
                "route descriptor kind and directions at index " + routeIndex);
            if (kind == BattlefieldRouteTileKind.Entry || kind == BattlefieldRouteTileKind.Exit)
                Assert(descriptor.Orientation != BattlefieldDirection.None,
                    "route endpoint has a cardinal orientation at index " + routeIndex);
        }

        private static void ValidateRouteDescriptorOrientations()
        {
            AssertEndpointDescriptors(
                new[] { new Vector2Int(0, 1), new Vector2Int(1, 1) }, new Vector2Int(2, 1),
                BattlefieldDirection.East, BattlefieldDirection.West);
            AssertEndpointDescriptors(
                new[] { new Vector2Int(2, 1), new Vector2Int(1, 1) }, new Vector2Int(0, 1),
                BattlefieldDirection.West, BattlefieldDirection.East);
            AssertEndpointDescriptors(
                new[] { new Vector2Int(1, 0), new Vector2Int(1, 1) }, new Vector2Int(1, 2),
                BattlefieldDirection.South, BattlefieldDirection.North);
            AssertEndpointDescriptors(
                new[] { new Vector2Int(1, 2), new Vector2Int(1, 1) }, new Vector2Int(1, 0),
                BattlefieldDirection.North, BattlefieldDirection.South);

            AssertInternalDescriptor(
                new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) },
                new Vector2Int(3, 1), BattlefieldRouteTileKind.Horizontal,
                BattlefieldDirection.West, BattlefieldDirection.East);
            AssertInternalDescriptor(
                new[] { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2) },
                new Vector2Int(1, 3), BattlefieldRouteTileKind.Vertical,
                BattlefieldDirection.North, BattlefieldDirection.South);
            AssertInternalDescriptor(
                new[] { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) },
                new Vector2Int(3, 1), BattlefieldRouteTileKind.CornerNorthEast,
                BattlefieldDirection.North, BattlefieldDirection.East);
            AssertInternalDescriptor(
                new[] { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1) },
                new Vector2Int(0, 2), BattlefieldRouteTileKind.CornerNorthWest,
                BattlefieldDirection.North, BattlefieldDirection.West);
            AssertInternalDescriptor(
                new[] { new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(2, 1) },
                new Vector2Int(3, 1), BattlefieldRouteTileKind.CornerSouthEast,
                BattlefieldDirection.South, BattlefieldDirection.East);
            AssertInternalDescriptor(
                new[] { new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(0, 1) },
                new Vector2Int(0, 0), BattlefieldRouteTileKind.CornerSouthWest,
                BattlefieldDirection.South, BattlefieldDirection.West);
        }

        private static void AssertEndpointDescriptors(Vector2Int[] route, Vector2Int core,
            BattlefieldDirection entryOrientation, BattlefieldDirection exitOrientation)
        {
            var map = CreateCanonicalTestMap(route, route[0], route[route.Length - 1], core, null);
            Assert(map.Validate(out var reason), "endpoint descriptor test map: " + reason);
            AssertRouteDescriptor(map, 0, BattlefieldRouteTileKind.Entry,
                BattlefieldDirection.None, entryOrientation);
            AssertRouteDescriptor(map, route.Length - 1, BattlefieldRouteTileKind.Exit,
                exitOrientation, BattlefieldDirection.None);
        }

        private static void AssertInternalDescriptor(Vector2Int[] route, Vector2Int core,
            BattlefieldRouteTileKind kind, BattlefieldDirection previous, BattlefieldDirection next)
        {
            var map = CreateCanonicalTestMap(route, route[0], route[route.Length - 1], core, null);
            Assert(map.Validate(out var reason), "internal descriptor test map: " + reason);
            AssertRouteDescriptor(map, 1, kind, previous, next);
        }

        private static BattlefieldMapDefinition CreateCanonicalTestMap(
            Vector2Int[] route, Vector2Int entry, Vector2Int exit, Vector2Int core,
            Vector2Int? initialCell)
        {
            const int width = 4;
            const int height = 4;
            Assert(entry == route[0] && exit == route[route.Length - 1],
                "descriptor test markers match route endpoints");
            var routeLookup = new HashSet<Vector2Int>(route);
            var resolvedInitialCell = initialCell ?? Vector2Int.zero;
            if (!initialCell.HasValue)
            {
                for (var index = 0; index < width * height; index++)
                {
                    var candidate = new Vector2Int(index % width, index / width);
                    if (candidate == core || routeLookup.Contains(candidate)) continue;
                    resolvedInitialCell = candidate;
                    break;
                }
            }
            return new BattlefieldMapDefinition(BattlefieldLayeredMapFactory.CreateSingleRouteMap(
                "canonical-test", width, height, 1f, route, core,
                CreateInitialGroup(resolvedInitialCell)));
        }

        private static InitialPotGroup[] CreateInitialGroup(Vector2Int cell)
        {
            return new[] { new InitialPotGroup("test", 1, new[] { cell }) };
        }

        private static float DistanceToRoute(BattlefieldMapDefinition map, Vector2 point)
        {
            var best = float.MaxValue;
            var step = Mathf.Max(.001f, map.Route.TotalLength / 1000f);
            for (var progress = 0f; progress <= map.Route.TotalLength; progress += step)
                best = Mathf.Min(best, Vector2.Distance(point, map.Route.Sample(progress)));
            return best;
        }

        private static void ValidateBattlefieldDeterminism()
        {
            var map = GameConfig.DefaultBattlefield;
            Assert(map.Route.SegmentCount == BattlefieldMapDefinition.DefaultRouteSegmentCount
                && map.Route.CumulativeLengths.Count == map.RouteCells.Count
                && Mathf.Approximately(map.MapUnitsPerCell,
                    BattlefieldMapDefinition.DefaultRouteLength / BattlefieldMapDefinition.DefaultRouteSegmentCount),
                "orchard-01 uses 19 uniform center-to-center route segments");
            for (var index = 0; index < map.RouteCells.Count; index++)
            {
                var expectedCenter = map.CellToMap(map.RouteCells[index]);
                var boundarySample = map.Route.Sample(map.Route.CumulativeLengths[index]);
                Assert(Vector2.Distance(boundarySample, expectedCenter) <= .000001f,
                    "route cumulative boundary samples the exact cell center at index " + index);
            }
            Assert(Mathf.Approximately(map.Route.TotalLength, P0GameplayParityBaseline.RouteLength),
                "orchard-01 route remains exactly 23 map units");
            Assert(map.Entry == map.Route.Sample(0f) && map.Exit == map.Route.Sample(map.Route.TotalLength),
                "route endpoint sampling matches entry and exit centers");
            AssertCornerContinuity(map, 7);
            AssertCornerContinuity(map, 13);

            var normalTraversalSeconds = map.Route.TotalLength / GameConfig.Zombie(ZombieKind.Normal).Speed;
            Assert(Mathf.Abs(normalTraversalSeconds - P0GameplayParityBaseline.NormalEnemyTraversalSeconds) <= .0001f,
                "normal enemy traversal duration matches the P0 parity baseline");
            var nearDistance = DistanceToRoute(map, map.CellToMap(new Vector2Int(6, 1)));
            var representativeDistance = DistanceToRoute(map, map.CellToMap(new Vector2Int(3, 3)));
            Assert(nearDistance <= GameConfig.Plant(PlantKind.Durian).Range
                && representativeDistance <= GameConfig.Plant(PlantKind.Pea).Range
                && representativeDistance > GameConfig.Plant(PlantKind.Durian).Range,
                "representative near and mid-range target coverage matches the P0 baseline");

            Assert(GameConfig.MaxWaves == P0GameplayParityBaseline.WaveCount
                && BuildWaveContentSignature() == P0GameplayParityBaseline.WaveContentSignature,
                "all fifteen ordered waves match the P0 content signature");
            Assert(BuildCombatNumericSignature() == P0GameplayParityBaseline.CombatNumericSignature,
                "plant, enemy, star, wave, and initial-pot values match the P0 numeric signature");

            var simulation = new GameSimulation(20260715);
            var snapshot = simulation.ExportSnapshot();
            Assert(simulation.Map.MapId == P0GameplayParityBaseline.MapId
                && simulation.MapId == P0GameplayParityBaseline.MapId
                && snapshot.mapId == P0GameplayParityBaseline.MapId,
                "active definition and exported snapshot use the orchard-01 map identity");
            Assert(simulation.State.Pots.Count == P0GameplayParityBaseline.InitialPotCount,
                "P0 parity baseline retains eight initial flowerpots");
        }

        private static void ValidateBattlefieldProjectionGeometry()
        {
            var map = GameConfig.DefaultBattlefield;
            var projection = new BattleUiLayout(map).Battlefield;
            var legacyProjection = new BattlefieldProjection(map, new Rect(4f, 76f, 394f, 398f));
            Assert(Mathf.Approximately(BattlefieldProjection.PotVisualRatio, .88f)
                && projection.TileSize > 0f
                && projection.TileSize > legacyProjection.TileSize
                && Mathf.Abs(projection.GridRect.center.x - projection.ContentRect.center.x) <= .001f
                && Mathf.Abs(projection.GridRect.center.y - projection.ContentRect.center.y) <= .001f
                && Mathf.Abs(projection.GridRect.width - projection.TileSize * map.GridWidth) <= .001f
                && Mathf.Abs(projection.GridRect.height - projection.TileSize * map.GridHeight) <= .001f
                && ContainsRect(projection.ContentRect, projection.GridRect),
                "square orchard grid is centered and contained in battlefield content");

            for (var row = 0; row < map.GridHeight; row++)
            {
                for (var column = 0; column < map.GridWidth; column++)
                {
                    var cell = new Vector2Int(column, row);
                    var tile = projection.TileRect(cell);
                    Assert(Mathf.Abs(tile.width - tile.height) <= .001f
                        && Mathf.Abs(tile.width - projection.TileSize) <= .001f
                        && ContainsRect(projection.GridRect, tile)
                        && Vector2.Distance(tile.center, projection.MapToScreen(map.CellToMap(cell))) <= .001f,
                        "tile is square, contained, and center-aligned at " + cell);
                    Assert(RectApproximately(tile, projection.CellRect(cell)),
                        "cell compatibility rectangle remains the full tile at " + cell);
                }
            }

            for (var routeIndex = 0; routeIndex < map.RouteCells.Count; routeIndex++)
            {
                var cell = map.RouteCells[routeIndex];
                var tile = projection.TileRect(cell);
                var routeTile = projection.RouteTileRect(cell);
                var sampledCenter = projection.MapToScreen(
                    map.Route.Sample(map.Route.CumulativeLengths[routeIndex]));
                Assert(RectApproximately(routeTile, tile) && ContainsRect(tile, routeTile)
                    && Vector2.Distance(sampledCenter, routeTile.center) <= .001f,
                    "route tile and cumulative route sample share the tile center at index " + routeIndex);
            }

            var coreTile = projection.TileRect(map.CoreCell);
            Assert(ContainsRect(coreTile, projection.CoreRect)
                && Vector2.Distance(coreTile.center, projection.CoreRect.center) <= .001f
                && Mathf.Abs(projection.CoreRect.width / coreTile.width
                    - BattlefieldProjection.CoreVisualRatio) <= .001f,
                "core visual remains centered and contained inside its semantic tile");

            var hitRects = new List<Rect>(map.PlantableCells.Count);
            var denseNeighborCount = 0;
            foreach (var cell in map.PlantableCells)
            {
                var tile = projection.TileRect(cell);
                var hit = projection.PotHitRect(cell);
                var visual = projection.PotVisualRect(cell);
                Assert(RectApproximately(hit, tile) && RectApproximately(projection.PotRect(cell), hit),
                    "flowerpot selection, drag, and drop target uses the full tile at " + cell);
                Assert(ContainsRect(hit, visual)
                    && Vector2.Distance(hit.center, visual.center) <= .001f
                    && Mathf.Abs(visual.width / hit.width - .88f) <= .001f
                    && Mathf.Abs(visual.height / hit.height - .88f) <= .001f
                    && !RectApproximately(visual, hit),
                    "frameless flowerpot visual is centered and contained at the 0.88 ratio at " + cell);
                foreach (var previousHit in hitRects)
                    Assert(!RectInteriorOverlaps(previousHit, hit),
                        "full-tile flowerpot targets remain independently addressable at " + cell);
                hitRects.Add(hit);

                var right = cell + Vector2Int.right;
                var down = cell + Vector2Int.up;
                if (map.IsPlantable(right))
                {
                    denseNeighborCount++;
                    Assert(!RectInteriorOverlaps(visual, projection.PotVisualRect(right)),
                        "horizontal adjacent flowerpot visuals remain disjoint at " + cell);
                }
                if (map.IsPlantable(down))
                {
                    denseNeighborCount++;
                    Assert(!RectInteriorOverlaps(visual, projection.PotVisualRect(down)),
                        "vertical adjacent flowerpot visuals remain disjoint at " + cell);
                }
            }
            Assert(denseNeighborCount > 0 && hitRects.Count == 35,
                "dense adjacent flowerpot coverage exercises all 35 independent targets");
            Assert(projection.ValidatePlantingGeometry(out var plantingReason),
                "projection planting geometry contract: " + plantingReason);
        }

        private static bool RectApproximately(Rect first, Rect second, float tolerance = .001f)
        {
            return Mathf.Abs(first.x - second.x) <= tolerance
                && Mathf.Abs(first.y - second.y) <= tolerance
                && Mathf.Abs(first.width - second.width) <= tolerance
                && Mathf.Abs(first.height - second.height) <= tolerance;
        }

        private static bool ContainsRect(Rect outer, Rect inner, float tolerance = .001f)
        {
            return inner.xMin >= outer.xMin - tolerance && inner.yMin >= outer.yMin - tolerance
                && inner.xMax <= outer.xMax + tolerance && inner.yMax <= outer.yMax + tolerance;
        }

        private static bool RectInteriorOverlaps(Rect first, Rect second, float tolerance = .001f)
        {
            return Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin) > tolerance
                && Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin) > tolerance;
        }

        private static void ValidateBattlefieldViewportMatrix()
        {
            var battleUiLayout = new BattleUiLayout(GameConfig.DefaultBattlefield);
            Assert(BattlefieldProjection.RequiredPortraitViewports.Count
                    == RuntimeUiQualityProfile.Viewports.Count,
                "required portrait viewport matrix has four cases");
            for (var index = 0;
                 index < RuntimeUiQualityProfile.Viewports.Count; index++)
            {
                var viewportCase = RuntimeUiQualityProfile.Viewports[index];
                var viewport = viewportCase.Viewport;
                Assert(BattlefieldProjection.RequiredPortraitViewports[index] == viewport,
                    "required portrait viewport is exact at index " + index);
                ValidateViewportLayoutCase(battleUiLayout, viewport,
                    viewportCase.FullSafeArea, "full-safe-area");
                ValidateViewportLayoutCase(battleUiLayout, viewport,
                    viewportCase.InsetSafeArea,
                    "top-" + viewportCase.SafeTop
                    + "-bottom-" + viewportCase.SafeBottom);
            }

            ValidateViewportLayoutCase(battleUiLayout, new Vector2Int(402, 874),
                new Rect(0f, 0f, 402f, 827f), "top-47");
            ValidateViewportLayoutCase(battleUiLayout, new Vector2Int(402, 874),
                new Rect(0f, 34f, 402f, 840f), "bottom-34");
        }

        private static void ValidateViewportLayoutCase(
            BattleUiLayout battleUiLayout, Vector2Int viewport,
            Rect safeArea, string caseName)
        {
            var layout = BattlefieldProjection.CalculateViewportLayout(
                viewport.x, viewport.y, safeArea,
                BattleUiLayout.DesignWidth, BattleUiLayout.DesignHeight);
            var viewportRect = new Rect(0f, 0f, viewport.x, viewport.y);
            Assert(layout.Scale > 0f
                && ContainsRect(viewportRect, layout.SafeAreaInGuiSpace)
                && ContainsRect(layout.SafeAreaInGuiSpace, layout.DesignViewportRect)
                && Mathf.Abs(layout.DesignViewportRect.center.x - layout.SafeAreaInGuiSpace.center.x) <= .001f
                && Mathf.Abs(layout.DesignViewportRect.center.y - layout.SafeAreaInGuiSpace.center.y) <= .001f,
                "design viewport is centered and contained for " + viewport + " " + caseName);

            var designRegions = new[]
            {
                battleUiLayout.Header,
                battleUiLayout.BattleSurface,
                battleUiLayout.Board,
                battleUiLayout.ToolTray,
                battleUiLayout.NurseryTray,
                battleUiLayout.RefreshAction,
                battleUiLayout.Detail,
            };
            foreach (var region in designRegions)
            {
                var projectedRegion = layout.ProjectDesignRect(region);
                Assert(ContainsRect(layout.SafeAreaInGuiSpace, projectedRegion)
                    && ContainsRect(viewportRect, projectedRegion),
                    "design control region remains inside the safe viewport for " + viewport + " " + caseName);
            }

            var map = GameConfig.DefaultBattlefield;
            var projection = battleUiLayout.Battlefield;
            Assert(projection.ValidateControlInset(out var controlReason),
                "battlefield control inset contract for " + viewport + " " + caseName + ": " + controlReason);
            var projectedBoard = layout.ProjectDesignRect(projection.BoardRect);
            var projectedGrid = layout.ProjectDesignRect(projection.GridRect);
            var projectedControlStrip = layout.ProjectDesignRect(projection.ControlStripRect);
            var projectedWaveAction = layout.ProjectDesignRect(projection.WaveActionRect);
            var projectedCore = layout.ProjectDesignRect(projection.CoreRect);
            Assert(ContainsRect(projectedBoard, projectedGrid)
                && ContainsRect(projectedBoard, projectedControlStrip)
                && ContainsRect(projectedControlStrip, projectedWaveAction)
                && ContainsRect(layout.SafeAreaInGuiSpace, projectedBoard)
                && !RectInteriorOverlaps(projectedGrid, projectedControlStrip)
                && !RectInteriorOverlaps(projectedGrid, projectedWaveAction),
                "grid and battlefield controls are contained and disjoint for " + viewport + " " + caseName);

            var projectedRouteTiles = new List<Rect>(map.RouteCells.Count);
            foreach (var routeCell in map.RouteCells)
            {
                var routeTile = layout.ProjectDesignRect(projection.RouteTileRect(routeCell));
                Assert(Mathf.Abs(routeTile.width - routeTile.height) <= .001f
                    && ContainsRect(projectedGrid, routeTile)
                    && !RectInteriorOverlaps(routeTile, projectedControlStrip)
                    && !RectInteriorOverlaps(routeTile, projectedWaveAction)
                    && !RectInteriorOverlaps(routeTile, projectedCore),
                    "route tile is square and clear of core and controls at " + routeCell
                        + " for " + viewport + " " + caseName);
                projectedRouteTiles.Add(routeTile);
            }

            Assert(ContainsRect(projectedGrid, projectedCore)
                && !RectInteriorOverlaps(projectedCore, projectedControlStrip)
                && !RectInteriorOverlaps(projectedCore, projectedWaveAction),
                "core is grid-local and clear of controls for " + viewport + " " + caseName);

            var projectedPotTargets = new List<Rect>(map.PlantableCells.Count);
            foreach (var plantableCell in map.PlantableCells)
            {
                var hit = layout.ProjectDesignRect(projection.PotHitRect(plantableCell));
                var visual = layout.ProjectDesignRect(projection.PotVisualRect(plantableCell));
                Assert(Mathf.Abs(hit.width - hit.height) <= .001f
                    && ContainsRect(projectedGrid, hit) && ContainsRect(hit, visual)
                    && Mathf.Abs(visual.width / hit.width - .88f) <= .001f
                    && !RectInteriorOverlaps(hit, projectedCore)
                    && !RectInteriorOverlaps(hit, projectedControlStrip)
                    && !RectInteriorOverlaps(hit, projectedWaveAction),
                    "flowerpot target and visual are contained and clear of core and controls at "
                        + plantableCell + " for " + viewport + " " + caseName);
                foreach (var routeTile in projectedRouteTiles)
                    Assert(!RectInteriorOverlaps(hit, routeTile),
                        "flowerpot target remains disjoint from route at " + plantableCell
                            + " for " + viewport + " " + caseName);
                foreach (var previousTarget in projectedPotTargets)
                    Assert(!RectInteriorOverlaps(hit, previousTarget),
                        "flowerpot targets remain independently addressable at " + plantableCell
                            + " for " + viewport + " " + caseName);
                projectedPotTargets.Add(hit);
            }
        }

        private static void AssertCornerContinuity(BattlefieldMapDefinition map, int routeIndex)
        {
            var boundary = map.Route.CumulativeLengths[routeIndex];
            var epsilon = map.MapUnitsPerCell * .001f;
            var center = map.CellToMap(map.RouteCells[routeIndex]);
            var before = map.Route.Sample(boundary - epsilon);
            var at = map.Route.Sample(boundary);
            var after = map.Route.Sample(boundary + epsilon);
            Assert(Vector2.Distance(at, center) <= .000001f
                && Mathf.Abs(Vector2.Distance(before, at) - epsilon) <= .000001f
                && Mathf.Abs(Vector2.Distance(at, after) - epsilon) <= .000001f,
                "enemy route reaches and leaves the exact corner center continuously at index " + routeIndex);
            var incoming = (at - before).normalized;
            var outgoing = (after - at).normalized;
            Assert(Mathf.Abs(Vector2.Dot(incoming, outgoing)) <= .0001f,
                "route changes cardinal direction at corner index " + routeIndex);
        }

        private static string BuildWaveContentSignature()
        {
            var rows = new List<string>(GameConfig.MaxWaves);
            for (var waveIndex = 1; waveIndex <= GameConfig.MaxWaves; waveIndex++)
            {
                var counts = new int[4];
                foreach (var kind in GameConfig.GetWave(waveIndex).Sequence) counts[(int)kind]++;
                var multiplier = GameConfig.WaveCountMultiplier(waveIndex);
                for (var kindIndex = 0; kindIndex < counts.Length; kindIndex++)
                {
                    Assert(counts[kindIndex] % multiplier == 0,
                        "wave " + waveIndex + " count is divisible by its scaling multiplier");
                    counts[kindIndex] /= multiplier;
                }
                rows.Add(string.Join(",", counts));
            }
            return string.Join("|", rows);
        }

        private static string BuildCombatNumericSignature()
        {
            var pea = GameConfig.Plant(PlantKind.Pea);
            var watermelon = GameConfig.Plant(PlantKind.Watermelon);
            var banana = GameConfig.Plant(PlantKind.Banana);
            var durian = GameConfig.Plant(PlantKind.Durian);
            var sunflower = GameConfig.Plant(PlantKind.Sunflower);
            var normal = GameConfig.Zombie(ZombieKind.Normal);
            var runner = GameConfig.Zombie(ZombieKind.Runner);
            var armored = GameConfig.Zombie(ZombieKind.Armored);
            var boss = GameConfig.Zombie(ZombieKind.Boss);
            return "plants:pea=" + PlantNumericSignature(pea)
                + ",watermelon=" + PlantNumericSignature(watermelon)
                + ",banana=" + PlantNumericSignature(banana)
                + ",durian=" + PlantNumericSignature(durian)
                + ",sunflower=" + PlantNumericSignature(sunflower)
                + ";enemies:normal=" + EnemyNumericSignature(normal)
                + ",runner=" + EnemyNumericSignature(runner)
                + ",armored=" + EnemyNumericSignature(armored)
                + ",boss=" + EnemyNumericSignature(boss)
                + ";stars:damage=" + StarNumericSignature(GameConfig.StarDamage)
                + ",speed=" + StarNumericSignature(GameConfig.StarSpeed)
                + ",range=" + StarNumericSignature(GameConfig.StarRange)
                + ";waves=" + GameConfig.MaxWaves
                + ",between=" + Number(GameConfig.BetweenWaveSeconds)
                + ",initial-pots=" + GameConfig.InitialPotCount;
        }

        private static string PlantNumericSignature(PlantStats stats)
        {
            return Number(stats.Damage) + "/" + Number(stats.Interval) + "/"
                + Number(GameConfig.LegacyDistance(stats.Range));
        }

        private static string EnemyNumericSignature(ZombieStats stats)
        {
            return Number(stats.Hp) + "/" + Number(GameConfig.LegacyDistance(stats.Speed))
                + "/" + stats.Reward + "/" + stats.Threat;
        }

        private static string StarNumericSignature(System.Func<int, float> valueAtStar)
        {
            return Number(valueAtStar(1)) + "/" + Number(valueAtStar(2)) + "/"
                + Number(valueAtStar(3)) + "/" + Number(valueAtStar(4));
        }

        private static string Number(float value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void ValidateMigrationBehavior()
        {
            var expansion = new GameSimulation(2468);
            expansion.State.Pots.Clear();
            expansion.State.Pots.Add(new Pot { Id = 1, Cell = new Vector2Int(3, 3), Active = true });
            expansion.State.Inventory.Pots = 1;
            Assert(expansion.CanExpand(new Vector2Int(4, 3)), "cardinal expansion remains legal");
            Assert(!expansion.CanExpand(new Vector2Int(4, 4)), "diagonal-only expansion remains illegal");

            var traversal = new GameSimulation(1357);
            traversal.State.Plants.Clear();
            traversal.State.Zombies.Clear();
            traversal.DiscardPendingPresentationEvents();
            traversal.State.Phase = GamePhase.Playing;
            traversal.State.WaveIndex = 1;
            traversal.State.WaveTotal = 0;
            traversal.State.WaveSpawned = 0;
            traversal.State.Zombies.Add(new Zombie
            {
                Id = 999,
                Kind = ZombieKind.Normal,
                Hp = 1000f,
                MaxHp = 1000f,
                Speed = GameConfig.Zombie(ZombieKind.Normal).Speed,
                Reward = 0,
                Threat = 1,
            });
            var lives = traversal.State.Lives;
            for (var step = 0; step < 2000 && traversal.State.Lives == lives; step++) traversal.Tick(.05f);
            var expectedSeconds = P0GameplayParityBaseline.NormalEnemyTraversalSeconds;
            Assert(traversal.State.Lives == lives - 1
                && Mathf.Abs(traversal.State.Elapsed - expectedSeconds) <= .1f,
                "normal zombie traversal timing remains within migration tolerance");
        }

        private static void ValidateDragRegressionCoverage()
        {
            var simulation = new GameSimulation(4242);
            simulation.State.Plants.Clear();
            simulation.DiscardPendingPresentationEvents();
            var firstPot = simulation.State.Pots[0];
            var secondPot = simulation.State.Pots[1];
            var source = new Plant
            {
                Id = 7001,
                Kind = PlantKind.Pea,
                Star = 1,
                PotId = -1,
                NurseryIndex = 0,
            };
            simulation.State.Plants.Add(source);

            var placement = simulation.GetPlantDropStatus(source.Id, firstPot.Id);
            Assert(placement.Legal && placement.Action == PlantDropAction.Plant
                && simulation.MoveOrMergePlant(source.Id, firstPot.Id, out _)
                && source.PotId == firstPot.Id && source.NurseryIndex == -1,
                "drag placement remains available");

            var movement = simulation.GetPlantDropStatus(source.Id, secondPot.Id);
            Assert(movement.Legal && movement.Action == PlantDropAction.Move
                && simulation.MoveOrMergePlant(source.Id, secondPot.Id, out _)
                && source.PotId == secondPot.Id,
                "drag movement remains available");

            var nurseryReturn = simulation.GetNurseryDropStatus(source.Id, 0);
            Assert(nurseryReturn.Legal && nurseryReturn.Action == PlantDropAction.Move
                && simulation.MoveToNursery(source.Id, 0, out _)
                && source.PotId == -1 && source.NurseryIndex == 0,
                "drag return to nursery remains available");

            Assert(simulation.MoveOrMergePlant(source.Id, firstPot.Id, out _),
                "merge source can be planted before drag merge");
            var mergeTarget = new Plant
            {
                Id = 7002,
                Kind = PlantKind.Pea,
                Star = 1,
                PotId = secondPot.Id,
                NurseryIndex = -1,
            };
            simulation.State.Plants.Add(mergeTarget);
            var merge = simulation.GetPlantDropStatus(source.Id, secondPot.Id);
            Assert(merge.Legal && merge.Action == PlantDropAction.Merge
                && simulation.MoveOrMergePlant(source.Id, secondPot.Id, out _)
                && simulation.PlantById(source.Id) == null && mergeTarget.Star == 2,
                "drag merge remains available");

            var invalidSource = new Plant
            {
                Id = 7003,
                Kind = PlantKind.Banana,
                Star = 1,
                PotId = firstPot.Id,
                NurseryIndex = -1,
            };
            simulation.State.Plants.Add(invalidSource);
            var swap = simulation.GetPlantDropStatus(invalidSource.Id, secondPot.Id);
            Assert(swap.Legal && swap.Action == PlantDropAction.Swap
                && simulation.MoveOrMergePlant(invalidSource.Id, secondPot.Id, out _)
                && invalidSource.PotId == secondPot.Id && mergeTarget.PotId == firstPot.Id,
                "different occupied plants swap through drag");

            simulation.State.Inventory.Ice = 1;
            var weaponStatus = simulation.GetWeaponInstallStatus(invalidSource.Id, WeaponKind.Ice);
            Assert(weaponStatus.Legal
                && simulation.InstallWeapon(invalidSource.Id, WeaponKind.Ice, out _)
                && invalidSource.Weapon == WeaponKind.Ice
                && simulation.State.Inventory.Ice == 0,
                "explicit weapon tool installation remains available");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.InvalidOperationException("Smoke validation failed: " + message);
        }

        private static void ValidateCombatActions()
        {
            var pea = CreateCombatScenario(PlantKind.Pea);
            pea.Step();
            Assert(pea.State.Projectiles.Count == 1 && Mathf.Approximately(pea.State.Zombies[0].Hp, 1000f),
                "pea creates a delayed tracking projectile");
            TickUntilProjectilesFinish(pea, 40);
            Assert(pea.State.Zombies[0].Hp < 1000f && HasCombatEffect(pea, CombatEffectKind.PeaImpact),
                "pea projectile tracks and creates an impact action");

            var watermelon = CreateCombatScenario(PlantKind.Watermelon);
            watermelon.Step();
            Assert(watermelon.State.Projectiles.Count == 1 && watermelon.State.Projectiles[0].Progress > 0f,
                "watermelon starts a timed arc projectile");
            for (var step = 0; step < 12; step++) watermelon.Tick(.05f);
            Assert(HasCombatEffect(watermelon, CombatEffectKind.WatermelonBlast)
                && watermelon.State.Zombies[0].Hp < 1000f, "watermelon lands and creates an area blast");

            var banana = CreateCombatScenario(PlantKind.Banana);
            banana.Step();
            var bananaCooldownTicks = BattleSkillTiming.SecondsToTicks(999f);
            foreach (var runtime in banana.State.Plants[0].SkillRuntimes)
                if (runtime.SkillId == BattleContentIds.Skills.BananaAttack)
                    runtime.CooldownTicks = bananaCooldownTicks;
            banana.State.Plants[0].AttackCooldown = BattleSkillTiming.TicksToSeconds(bananaCooldownTicks);
            TickUntilProjectilesFinish(banana, 90);
            Assert(Mathf.Approximately(banana.State.Zombies[0].Hp, 988f),
                "banana hits once outbound and once while returning");

            var durian = CreateCombatScenario(PlantKind.Durian);
            durian.Step();
            Assert(HasCombatEffect(durian, CombatEffectKind.DurianDrop)
                && durian.State.Zombies[0].Hp < 1000f, "durian uses a melee drop and shockwave action");

            var sunflower = CreateCombatScenario(PlantKind.Sunflower);
            sunflower.State.Plants[0].ProductionProgress = 9.99f;
            sunflower.State.Sun = 0;
            sunflower.Step();
            Assert(sunflower.State.Sun == 1
                && HasCombatEffect(sunflower, CombatEffectKind.SunBurst),
                "sunflower production creates a visible sun burst");

            var iceSunflower = CreateCombatScenario(PlantKind.Sunflower, WeaponKind.Ice);
            iceSunflower.State.Zombies.Clear();
            iceSunflower.State.WaveSpawned = 0;
            iceSunflower.State.WaveTotal = GameConfig.GetWave(1).Sequence.Count;
            iceSunflower.State.SpawnCooldown = 0f;
            iceSunflower.Step();
            Assert(iceSunflower.State.Zombies.Count > 0
                && iceSunflower.State.Zombies[0].SlowUntil > iceSunflower.State.Elapsed,
                "ice sunflower slows the battlefield on the first wave spawn");

            var gatling = CreateCombatScenario(PlantKind.Pea, WeaponKind.Gatling);
            gatling.Step();
            Assert(gatling.State.Plants[0].BurstShotsRemaining == 3, "gatling starts a four-shot burst");
            for (var step = 0; step < 5; step++) gatling.Tick(.05f);
            Assert(gatling.State.Plants[0].BurstShotsRemaining == 2
                && HasCombatEffect(gatling, CombatEffectKind.GatlingMuzzle),
                "gatling spaces burst shots by 0.2 seconds");

            var ice = CreateCombatScenario(PlantKind.Pea, WeaponKind.Ice);
            ice.Step();
            TickUntilProjectilesFinish(ice, 40);
            Assert(ice.State.Zombies[0].SlowUntil > ice.State.Elapsed
                && HasCombatEffect(ice, CombatEffectKind.IceImpact),
                "ice weapon adds slow and a crystal impact");

            var chili = CreateCombatScenario(PlantKind.Pea, WeaponKind.Chili);
            chili.Step();
            TickUntilProjectilesFinish(chili, 40);
            Assert(chili.State.Zombies[0].Burns.Count == 1
                && HasCombatEffect(chili, CombatEffectKind.ChiliImpact),
                "chili weapon adds a burn stack and flame impact");
        }

        private static GameSimulation CreateCombatScenario(PlantKind kind, WeaponKind weapon = WeaponKind.None)
        {
            var simulation = new GameSimulation(9876 + (int)kind * 17 + (int)weapon);
            simulation.State.Plants.Clear();
            simulation.State.Zombies.Clear();
            simulation.State.Projectiles.Clear();
            simulation.DiscardPendingPresentationEvents();
            simulation.State.Phase = GamePhase.Playing;
            simulation.State.WaveIndex = 1;
            simulation.State.WaveTotal = 1;
            simulation.State.WaveSpawned = 1;
            simulation.State.NextId = 10000;
            var pot = simulation.State.Pots[0];
            simulation.State.Plants.Add(new Plant
            {
                Id = 9001,
                Kind = kind,
                Star = 1,
                PotId = pot.Id,
                NurseryIndex = -1,
                Weapon = weapon,
            });
            simulation.State.Zombies.Add(new Zombie
            {
                Id = 9002,
                Kind = ZombieKind.Normal,
                Hp = 1000f,
                MaxHp = 1000f,
                Speed = 0f,
                PathProgress = NearestPathProgress(simulation, simulation.PotPoint(pot)),
                Reward = 0,
                Threat = 1,
            });
            return simulation;
        }

        private static bool HasCombatEffect(GameSimulation simulation, CombatEffectKind kind)
        {
            var events = new List<BattlePresentationEvent>();
            simulation.DrainPresentationEvents(events);
            foreach (var value in events)
                if (value.Kind == BattlePresentationEventKind.Cue
                    && value.HasCombatEffect && value.CombatEffectKind == kind)
                    return true;
            return false;
        }

        private static float NearestPathProgress(GameSimulation simulation, Vector2 point)
        {
            var bestProgress = 0f;
            var bestDistance = float.MaxValue;
            var step = GameConfig.MapDistance(.25f);
            for (var progress = 0f; progress <= simulation.Map.Route.TotalLength; progress += step)
            {
                var distance = Vector2.SqrMagnitude(simulation.Map.Route.Sample(progress) - point);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestProgress = progress;
            }
            return bestProgress;
        }

        private static void TickUntilProjectilesFinish(GameSimulation simulation, int maxSteps)
        {
            for (var step = 0; step < maxSteps && simulation.State.Projectiles.Count > 0; step++)
                simulation.Tick(.05f);
        }
    }
}
