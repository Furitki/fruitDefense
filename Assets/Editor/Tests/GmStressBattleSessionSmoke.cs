using System;
using FruitDefense.App;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Development.GmStress;
using FruitDefense.Platform;
using FruitDefense.Tilemaps;
using FruitDefense.UI;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class GmStressBattleSessionSmoke
    {
        private const string ReleaseThemePath =
            "Assets/UI/Theme/ReleaseRuntimeUiTheme.asset";
        private const string ReleaseTerrainPalettePath =
            "Assets/Battlefield/Terrain/OrchardDefaultTerrainPalette.asset";

        public static void Validate()
        {
            var theme = AssetDatabase.LoadAssetAtPath<RuntimeUiTheme>(ReleaseThemePath);
            Assert(theme != null && theme.Validate().IsValid,
                "release RuntimeUiTheme is available to the GM presenter");
            var terrainPalette = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(
                ReleaseTerrainPalettePath);
            Assert(terrainPalette != null,
                "registered orchard terrain palette asset is available");
            Assert(GmStressBattleFactory.ValidateTerrainPalette(
                    GmStressBattleFactory.CreateMap(), terrainPalette, out var terrainReason),
                "registered orchard terrain palette is available to the GM presenter: "
                + terrainReason);
            var contentVersion = GmStressBattleFactory.CreateContent()
                .Header.contentVersion;
            ValidateRequestIdentityBoundary(theme, terrainPalette, contentVersion);
            ValidateTerrainDependencyBoundary(theme, terrainPalette, contentVersion);
            ValidatePersistenceAndResultRejection(theme, terrainPalette, contentVersion);
            Debug.Log("FRUIT_DEFENSE_GM_STRESS_SESSION_OK");
        }

        private static void ValidateRequestIdentityBoundary(RuntimeUiTheme theme,
            BattlefieldTerrainPalette terrainPalette, string contentVersion)
        {
            WithHost(host =>
            {
                var standard = new BattleLaunchRequest("gm-standard-rejected",
                    GmStressBattleIds.LevelId, 21001, contentVersion,
                    BattleSessionMode.Standard);
                var result = host.InitializeGm(standard, new AppNavigator(),
                    new RecordingResultSink(), theme, terrainPalette);
                Assert(!result.Success
                    && result.ErrorCode
                        == BattleSessionInitializationResult.SessionModeMismatch
                    && !host.Status.IsInitialized,
                    "GM host rejects a Standard battle-session request");
            });

            WithHost(host =>
            {
                var wrongLevel = new BattleLaunchRequest("gm-level-rejected",
                    "development.gm-stress.missing", 21002, contentVersion,
                    BattleSessionMode.GmStress);
                var result = host.InitializeGm(wrongLevel, new AppNavigator(),
                    new RecordingResultSink(), theme, terrainPalette);
                Assert(!result.Success
                    && result.ErrorCode == GmStressBattlePresenter.LevelMismatch
                    && !host.Status.IsInitialized,
                    "GM host rejects any non-canonical GM level identity");
            });

            WithHost(host =>
            {
                var wrongVersion = new BattleLaunchRequest("gm-content-rejected",
                    GmStressBattleIds.LevelId, 21003, "content.missing",
                    BattleSessionMode.GmStress);
                var result = host.InitializeGm(wrongVersion, new AppNavigator(),
                    new RecordingResultSink(), theme, terrainPalette);
                Assert(!result.Success
                    && result.ErrorCode
                        == BattleSessionInitializationResult.ContentVersionMismatch
                    && !host.Status.IsInitialized,
                    "GM host rejects a request with a mismatched battle-content version");
            });
        }

        private static void ValidateTerrainDependencyBoundary(RuntimeUiTheme theme,
            BattlefieldTerrainPalette terrainPalette, string contentVersion)
        {
            WithHost(host =>
            {
                var request = new BattleLaunchRequest("gm-terrain-required",
                    GmStressBattleIds.LevelId, 21005, contentVersion,
                    BattleSessionMode.GmStress);
                var result = host.InitializeGm(request, new AppNavigator(),
                    new RecordingResultSink(), theme, null);
                Assert(!result.Success
                    && result.ErrorCode == GmStressBattlePresenter.TerrainPaletteRequired
                    && !host.Status.IsInitialized,
                    "GM host fails closed when its registered terrain palette is missing");
            });

            Assert(terrainPalette != null
                && terrainPalette.PaletteId == GmStressBattleIds.TerrainPaletteId,
                "GM terrain dependency uses the stable orchard palette identity");
        }

        private static void ValidatePersistenceAndResultRejection(
            RuntimeUiTheme theme, BattlefieldTerrainPalette terrainPalette,
            string contentVersion)
        {
            WithHost(host =>
            {
                var sink = new RecordingResultSink();
                var navigator = new AppNavigator();
                var request = new BattleLaunchRequest("gm-valid-session",
                    GmStressBattleIds.LevelId, 21004, contentVersion,
                    BattleSessionMode.GmStress);
                var initialization = host.InitializeGm(request, navigator, sink,
                    theme, terrainPalette);
                Assert(initialization.Success
                    && host.Status.IsInitialized
                    && host.Status.Phase == GamePhase.Playing
                    && !host.Status.IsPaused
                    && !host.Status.HasSubmittedResult,
                    "valid GM request creates an isolated GM simulation without a released level");

                var export = host.ExportCurrentSessionSnapshot();
                Assert(!export.Succeeded
                    && export.Code == BattleSnapshotExportCode.UnsupportedSessionSource
                    && export.Message == GmStressBattlePresenter.SnapshotUnsupported,
                    "GM snapshot export returns an explicit unsupported result");

                var restore = host.RestoreCurrentSessionSnapshot(null, null);
                Assert(!restore.Succeeded
                    && restore.Code == BattleSnapshotRestoreCode.UnsupportedSessionSource
                    && restore.Path == "session.source"
                    && restore.Message == GmStressBattlePresenter.SnapshotUnsupported,
                    "GM snapshot restore returns an explicit structured rejection");

                Assert(!host.TrySubmitTerminalResult()
                    && !host.Status.HasSubmittedResult && sink.SubmitCount == 0,
                    "GM result submission is explicitly rejected before the result sink");

                host.HandlePlatformVisibility(PlatformVisibility.Background);
                Assert(host.Status.IsPaused,
                    "GM host exposes background pause only through immutable status");
                Assert(host.RestartCurrentSession(out var restartError)
                    && string.IsNullOrEmpty(restartError)
                    && host.Status.IsInitialized
                    && host.Status.Phase == GamePhase.Playing
                    && !host.Status.IsPaused
                    && !host.Status.IsTerminal,
                    "GM restart resets stress state without entering settlement");

                host.DisposeSession();
                Assert(!host.Status.IsInitialized && sink.SubmitCount == 0,
                    "disposing a GM session releases it without submitting a result");
            });
        }

        private static void WithHost(Action<GmStressBattlePresenter> validate)
        {
            var gameObject = new GameObject("GmStressBattleSessionSmoke");
            try
            {
                validate(gameObject.AddComponent<GmStressBattlePresenter>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private sealed class RecordingResultSink : IBattleResultSink
        {
            public int SubmitCount { get; private set; }

            public bool TrySubmitResult(BattleResult result, out string errorCode)
            {
                SubmitCount++;
                errorCode = "unexpected-gm-result-submission";
                return false;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) Fail(message);
        }

        private static void Fail(string message)
        {
            throw new InvalidOperationException(
                "GM stress session validation failed: " + message);
        }
    }
}
