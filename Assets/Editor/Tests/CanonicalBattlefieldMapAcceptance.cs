using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class CanonicalBattlefieldMapAcceptance
    {
        public const string MapId = "map.canonical-editor-acceptance";
        public const string LevelId = "level.canonical-editor-acceptance";
        public const string MapAssetPath =
            "Assets/Editor/Tests/Fixtures/CanonicalBattlefieldMap/CanonicalEditorAcceptanceMap.asset";
        public const string ManifestAssetPath =
            "Assets/Editor/Tests/Fixtures/CanonicalBattlefieldMap/BattlefieldMapPublicationManifest.asset";
        public const string GeneratedCatalogAssetPath =
            "Assets/Editor/Tests/Fixtures/CanonicalBattlefieldMap/CanonicalEditorAcceptanceCatalog.asset";
        public const string EvidenceRoot =
            "Builds/Evidence/canonical-map-editor";

        public static void PrepareAndCapture()
        {
            var map = BuildAcceptanceMap();
            EnsureFolder("Assets/Editor/Tests/Fixtures/CanonicalBattlefieldMap");
            var persistentMap = AssetDatabase.LoadAssetAtPath<BattlefieldMapAuthoringAsset>(
                MapAssetPath);
            if (persistentMap == null)
            {
                map.name = "CanonicalEditorAcceptanceMap";
                AssetDatabase.CreateAsset(map, MapAssetPath);
                persistentMap = map;
            }
            else
            {
                EditorUtility.CopySerialized(map, persistentMap);
                UnityEngine.Object.DestroyImmediate(map);
                persistentMap.name = "CanonicalEditorAcceptanceMap";
                EditorUtility.SetDirty(persistentMap);
            }

            var manifest = AssetDatabase.LoadAssetAtPath<BattlefieldMapPublicationManifest>(
                ManifestAssetPath);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<BattlefieldMapPublicationManifest>();
                manifest.name = "BattlefieldMapPublicationManifest";
                AssetDatabase.CreateAsset(manifest, ManifestAssetPath);
            }
            manifest.Configure(new[]
            {
                new BattlefieldMapPublicationManifestEntry(0, LevelId,
                    BundledLevelCatalogIds.Levels.Orchard01, persistentMap),
            });
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MapAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ManifestAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var result = BattlefieldMapPublicationExporter.Rebuild(manifest,
                GeneratedCatalogAssetPath,
                BattlefieldMapPublicationExporter.LoadReleaseRegisteredPalettes());
            if (!result.Succeeded)
                throw new InvalidOperationException("Acceptance publication failed: "
                    + string.Join(" | ", result.Diagnostics.Select(value => value.ToString())));
            ValidateIsolatedRuntimeIdentity(result.GeneratedCatalog);
            WriteDiagnostics(persistentMap, result.Diagnostics);
            PrepareWindow(persistentMap, manifest);
        }

        public static void PreparePlaytest()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<BattlefieldMapPublicationManifest>(
                ManifestAssetPath);
            if (!CanonicalBattlefieldMapPlaytest.TryPrepare(manifest, LevelId,
                    out var reason))
                throw new InvalidOperationException("Acceptance Playtest preparation failed: "
                    + reason);
            File.WriteAllText(Path.Combine(EvidenceRoot, "playtest-prepared.log"),
                "CANONICAL_BATTLEFIELD_MAP_PLAYTEST_PREPARED_OK\n"
                + "levelId=" + LevelId + "\nmapId=" + MapId + "\n");
            Debug.Log("CANONICAL_BATTLEFIELD_MAP_PLAYTEST_PREPARED_OK levelId="
                + LevelId + " mapId=" + MapId);
        }

        private static BattlefieldMapAuthoringAsset BuildAcceptanceMap()
        {
            var map = BattlefieldMapAuthoringAsset.Create(MapId, 8, 7,
                BattlefieldMapDefinition.DefaultRouteLength
                / BattlefieldMapDefinition.DefaultRouteSegmentCount);
            string reason;
            for (var x = 0; x <= 6; x++)
            {
                var cell = new Vector2Int(x, 0);
                Require(map.TrySetGameplay(cell,
                    new[] { BattlefieldLayerIds.Capabilities.EnemyTraversable },
                    Array.Empty<string>(), out reason), reason);
                Require(map.TryAppendRouteCell(cell, out reason), reason);
            }
            Require(map.TrySynchronizeRouteEndpoints(out reason), reason);
            string markerId;
            Require(map.TryPlaceMarker(BattlefieldMarkerKind.Core,
                new Vector2Int(7, 0), null, out markerId, out reason), reason);

            for (var y = 1; y < map.GridHeight; y++)
            for (var x = 0; x < map.GridWidth; x++)
                Require(map.TrySetGameplay(new Vector2Int(x, y),
                    new[] { BattlefieldLayerIds.Capabilities.Plantable },
                    Array.Empty<string>(), out reason), reason);
            Require(map.TrySetMarkerGroup("group.acceptance-pots", 8, out reason), reason);
            for (var x = 0; x < map.GridWidth; x++)
                Require(map.TryPlaceMarker(BattlefieldMarkerKind.InitialPotCandidate,
                    new Vector2Int(x, 2), "group.acceptance-pots",
                    out markerId, out reason), reason);

            Require(map.ApplyRecommendedPresentation(out reason), reason);
            Require(map.TrySetVisual(new Vector2Int(0, 1),
                BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.EdgeStyles.Refined, out reason), reason);
            Require(map.VisualCells.Where(cell => cell != null
                    && cell.LandformSurfaceId == BattlefieldLayerIds.Surfaces.Grass)
                .All(cell => cell.EdgeStyleId == BattlefieldLayerIds.EdgeStyles.Refined),
                "acceptance grass region must use one exact edge style");
            Require(BattlefieldLayeredMapCompiler.TryCompile(map.ToSource(),
                out _, out var validation), "canonical compiler: "
                + string.Join(" | ", validation.Issues.Select(issue => issue.ToString())));
            return map;
        }

        private static void ValidateIsolatedRuntimeIdentity(
            PublishedBattlefieldMapCatalog generated)
        {
            var reloaded = AssetDatabase.LoadAssetAtPath<PublishedBattlefieldMapCatalog>(
                GeneratedCatalogAssetPath);
            Require(reloaded != null && generated != null
                && reloaded.Entries.Count == 1
                && reloaded.Entries[0].LevelId == LevelId
                && reloaded.Entries[0].Map != null
                && reloaded.Entries[0].Map.MapId == MapId,
                "generated editor fixture reloads expected levelId/mapId");
            Require(BattleContentCompiler.TryCompile(
                    BundledBattleContentFactory.Create(), out var battleContent,
                    out var contentValidation),
                "bundled battle content compilation: "
                + (contentValidation == null ? string.Empty : string.Join(" | ",
                    contentValidation.Issues.Select(issue => issue.ToString()))));
            var bundled = BundledLevelCatalogFactory.CreateBundledSource();
            var isolatedSource = BundledLevelCatalogFactory.ComposePublished(
                bundled, reloaded);
            Require(LevelCatalogCompiler.TryCompile(isolatedSource, battleContent,
                    out var compiled, out var levelValidation),
                "isolated acceptance catalog compilation: "
                + (levelValidation == null ? string.Empty : string.Join(" | ",
                    levelValidation.Issues.Select(issue => issue.ToString()))));
            var resolution = compiled.Resolve(LevelId);
            Require(resolution.Succeeded
                && resolution.Value.Identity.LevelId == LevelId
                && resolution.Value.Identity.MapId == MapId,
                "explicitly injected acceptance catalog resolves expected authored identity");

            var production = BundledLevelCatalogFactory.CreateCompiled();
            Require(production.PlayableLevels.Select(level => level.LevelId)
                    .SequenceEqual(bundled.Levels.Select(level => level.LevelId))
                && !production.Resolve(LevelId).Succeeded,
                "acceptance fixture stays outside the production playable catalog");
        }

        private static void WriteDiagnostics(BattlefieldMapAuthoringAsset map,
            IReadOnlyList<BattlefieldMapPublicationDiagnostic> publicationDiagnostics)
        {
            Directory.CreateDirectory(EvidenceRoot);
            var authoring = map.CollectDiagnostics();
            var lines = new List<string>
            {
                "CANONICAL_BATTLEFIELD_MAP_ACCEPTANCE_READY",
                "levelId=" + LevelId,
                "mapId=" + MapId,
                "grid=" + map.GridWidth + "x" + map.GridHeight,
                "visualCells=" + map.VisualCells.Count,
                "gameplayCells=" + map.GameplayCells.Count,
                "routeCells=" + map.PrimaryRoute.Cells.Count,
                "markers=" + map.Markers.Count,
                "authoringBlocking=" + authoring.Count(value => value.IsBlocking),
                "publicationBlocking=" + publicationDiagnostics.Count(value => value.IsBlocking),
            };
            lines.AddRange(authoring.Select(value => "authoring=" + value));
            lines.AddRange(publicationDiagnostics.Select(value => "publication=" + value));
            File.WriteAllLines(Path.Combine(EvidenceRoot, "final-diagnostics.log"), lines);
        }

        private static void PrepareWindow(BattlefieldMapAuthoringAsset map,
            BattlefieldMapPublicationManifest manifest)
        {
            var state = CanonicalBattlefieldMapEditorState.instance;
            state.mapGuid = AssetDatabase.AssetPathToGUID(MapAssetPath);
            state.manifestGuid = AssetDatabase.AssetPathToGUID(ManifestAssetPath);
            state.workspace = CanonicalBattlefieldMapWorkspace.Gameplay;
            state.tool = CanonicalBattlefieldMapTool.SingleCell;
            state.zoom = .82f;
            state.scroll = Vector2.zero;
            state.selectedCell = new Vector2Int(0, 2);
            state.Persist();

            CanonicalBattlefieldMapEditorWindow.Open(map);
            var window = EditorWindow.GetWindow<CanonicalBattlefieldMapEditorWindow>();
            window.position = new Rect(35f, 55f, 1240f, 790f);
            var refresh = typeof(CanonicalBattlefieldMapEditorWindow).GetMethod(
                "RefreshDiagnostics", BindingFlags.Instance | BindingFlags.NonPublic);
            if (refresh == null)
                throw new MissingMethodException("Canonical editor diagnostic refresh hook missing.");
            refresh.Invoke(window, new object[] { true });
            Selection.activeObject = map;
            CaptureWorkspaces(window, 0);
        }

        private static void CaptureWorkspaces(CanonicalBattlefieldMapEditorWindow window,
            int index)
        {
            var workspaces = new[]
            {
                CanonicalBattlefieldMapWorkspace.Gameplay,
                CanonicalBattlefieldMapWorkspace.RouteAndMarkers,
                CanonicalBattlefieldMapWorkspace.Presentation,
                CanonicalBattlefieldMapWorkspace.Validation,
            };
            var names = new[]
            {
                "editor-gameplay.png", "editor-route-markers.png",
                "editor-presentation.png", "editor-validation-publish-ready.png",
            };
            if (index >= workspaces.Length)
            {
                File.WriteAllText(Path.Combine(EvidenceRoot,
                    "editor-evidence-ready.log"),
                    "CANONICAL_BATTLEFIELD_MAP_EDITOR_EVIDENCE_OK\n");
                Debug.Log("CANONICAL_BATTLEFIELD_MAP_EDITOR_EVIDENCE_OK path="
                    + EvidenceRoot);
                return;
            }
            CanonicalBattlefieldMapEditorState.instance.workspace = workspaces[index];
            CanonicalBattlefieldMapEditorState.instance.Persist();
            window.Repaint();
            EditorApplication.delayCall += () =>
            {
                window.Repaint();
                EditorApplication.delayCall += () =>
                {
                    var path = Path.Combine(EvidenceRoot, names[index]);
                    CaptureWindow(window.position, path);
                    if (workspaces[index] == CanonicalBattlefieldMapWorkspace.Validation)
                        File.Copy(path, Path.Combine(EvidenceRoot,
                            "canonical-editor.png"), true);
                    CaptureWorkspaces(window, index + 1);
                };
            };
        }

        private static void CaptureWindow(Rect rect, string path)
        {
            var width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            var height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
            var pixels = InternalEditorUtility.ReadScreenPixel(
                new Vector2(rect.x, rect.y), width, height);
            if (pixels == null || pixels.Length != width * height)
                throw new InvalidOperationException("Canonical editor screen capture returned no pixels.");
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                image.SetPixels(pixels);
                image.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var separator = path.LastIndexOf('/');
            var parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Canonical battlefield acceptance failed: " + message);
        }
    }
}
