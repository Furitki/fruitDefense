using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class CanonicalBattlefieldMapAuthoringSmoke
    {
        private const string FixtureFolder =
            "Assets/Editor/Tests/Fixtures/CanonicalBattlefieldMap";
        private const string RoundTripAssetPath = FixtureFolder + "/RoundTripSmoke.asset";

        public static void Validate()
        {
            ValidateBlankCreationAndBounds();
            ValidateResizeDefaultsAndRemovalReport();
            ValidateSaveReloadRoundTrip();
            ValidateLayerIndependenceAndRecommendation();
            ValidateContourSerializationAndComponents();
            ValidateRouteAndTypedMarkers();
            Debug.Log("CANONICAL_BATTLEFIELD_MAP_AUTHORING_SMOKE_OK");
        }

        private static void ValidateBlankCreationAndBounds()
        {
            var asset = BattlefieldMapAuthoringAsset.Create("map.smoke.blank", 8, 7, 2.5f);
            try
            {
                Assert(asset.MapId == "map.smoke.blank" && asset.GridWidth == 8
                    && asset.GridHeight == 7 && Mathf.Approximately(asset.MapUnitsPerCell, 2.5f),
                    "blank asset preserves identity and dimensions");
                Assert(asset.VisualCells.Count == 56 && asset.GameplayCells.Count == 56,
                    "blank asset has exact visual/gameplay coverage");
                Assert(asset.VisualCells.All(cell => cell != null
                        && cell.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Soil
                        && string.IsNullOrEmpty(cell.LandformSurfaceId)
                        && string.IsNullOrEmpty(cell.ContourStyleId)
                        && string.IsNullOrEmpty(cell.EdgeStyleId)),
                    "blank asset explicitly fills soil presentation");
                Assert(asset.GameplayCells.All(cell => cell != null
                        && cell.CapabilityIds.Count == 0 && cell.CollisionIds.Count == 0),
                    "blank asset explicitly fills empty gameplay records");
                Assert(!asset.InBounds(new Vector2Int(-1, 0))
                    && !asset.InBounds(new Vector2Int(0, -1))
                    && !asset.InBounds(new Vector2Int(8, 0))
                    && !asset.InBounds(new Vector2Int(0, 7)),
                    "all four grid boundaries are exclusive");

                var before = EditorJsonUtility.ToJson(asset, true);
                string reason;
                Assert(!asset.TrySetVisual(new Vector2Int(-1, 0),
                        BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.Surfaces.Grass, string.Empty, out reason),
                    "negative presentation coordinate is rejected");
                Assert(!asset.TrySetVisualCells(new[]
                    {
                        new Vector2Int(1, 1), new Vector2Int(8, 1),
                    }, BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass, string.Empty, out reason),
                    "mixed valid/out-of-bounds presentation batch is rejected atomically");
                Assert(!asset.TrySetGameplay(new Vector2Int(0, 7),
                        new[] { BattlefieldLayerIds.Capabilities.Plantable },
                        Array.Empty<string>(), out reason),
                    "gameplay coordinate at height is rejected");
                Assert(!asset.TryAppendRouteCell(new Vector2Int(8, 6), out reason),
                    "out-of-bounds route cell is rejected");
                string markerId;
                Assert(!asset.TryPlaceMarker(BattlefieldMarkerKind.Core,
                        new Vector2Int(4, -1), null, out markerId, out reason),
                    "out-of-bounds marker is rejected");
                BattlefieldMapResizeReport report;
                Assert(!asset.TryResize(0, 7, out report, out reason),
                    "non-positive resize is rejected");
                Assert(before == EditorJsonUtility.ToJson(asset, true),
                    "every rejected bounded mutation leaves the aggregate byte-equivalent");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static void ValidateResizeDefaultsAndRemovalReport()
        {
            var asset = BattlefieldMapAuthoringAsset.Create("map.smoke.resize", 4, 3);
            try
            {
                string reason;
                Assert(asset.TrySetVisual(new Vector2Int(1, 1),
                        BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.Surfaces.Grass, string.Empty, out reason), reason);
                Assert(asset.TrySetGameplay(new Vector2Int(1, 1),
                        new[] { BattlefieldLayerIds.Capabilities.Plantable },
                        Array.Empty<string>(), out reason), reason);
                foreach (var cell in new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0),
                    new Vector2Int(2, 0), new Vector2Int(3, 0),
                    new Vector2Int(3, 1), new Vector2Int(3, 2),
                })
                    Assert(asset.TryAppendRouteCell(cell, out reason), reason);
                string coreId;
                Assert(asset.TryPlaceMarker(BattlefieldMarkerKind.Core,
                    new Vector2Int(3, 1), null, out coreId, out reason), reason);

                BattlefieldMapResizeReport report;
                Assert(asset.TryResize(3, 2, out report, out reason), reason);
                Assert(asset.GridWidth == 3 && asset.GridHeight == 2
                    && asset.VisualCells.Count == 6 && asset.GameplayCells.Count == 6,
                    "shrink preserves exact coverage");
                Assert(report.RemovedRouteCells.SequenceEqual(new[]
                    {
                        new Vector2Int(3, 0), new Vector2Int(3, 1),
                        new Vector2Int(3, 2),
                    }) && report.RemovedMarkerIds.SequenceEqual(new[] { coreId }),
                    "shrink explicitly reports every removed route cell and marker");
                Assert(asset.TryGetVisual(new Vector2Int(1, 1), out var retainedVisual)
                    && retainedVisual.LandformSurfaceId == BattlefieldLayerIds.Surfaces.Grass
                    && retainedVisual.ContourStyleId == BattlefieldLayerIds.ContourStyles.Square
                    && asset.TryGetGameplay(new Vector2Int(1, 1), out var retainedGameplay)
                    && retainedGameplay.HasCapability(BattlefieldLayerIds.Capabilities.Plantable),
                    "shrink retains in-bounds visual and gameplay data");

                Assert(asset.TryResize(5, 4, out report, out reason), reason);
                Assert(asset.VisualCells.Count == 20 && asset.GameplayCells.Count == 20,
                    "expand preserves exact coverage");
                Assert(asset.TryGetVisual(new Vector2Int(4, 3), out var newVisual)
                    && newVisual.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Soil
                    && string.IsNullOrEmpty(newVisual.LandformSurfaceId)
                    && string.IsNullOrEmpty(newVisual.ContourStyleId)
                    && asset.TryGetGameplay(new Vector2Int(4, 3), out var newGameplay)
                    && newGameplay.CapabilityIds.Count == 0
                    && newGameplay.CollisionIds.Count == 0,
                    "expanded cells receive explicit defaults");
                Assert(asset.TryGetVisual(new Vector2Int(1, 1), out retainedVisual)
                    && retainedVisual.LandformSurfaceId == BattlefieldLayerIds.Surfaces.Grass,
                    "expand retains earlier in-bounds presentation");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static void ValidateSaveReloadRoundTrip()
        {
            EnsureFolder(FixtureFolder);
            var authored = CreateValidMap("map.smoke.roundtrip");
            authored.name = "RoundTripSmoke";
            var expectedSource = SerializeSource(authored.ToSource());
            var expectedDiagnostics = SerializeDiagnostics(authored.CollectDiagnostics());
            var expectedCompiled = Compile(authored);
            try
            {
                var persistent = AssetDatabase.LoadAssetAtPath<BattlefieldMapAuthoringAsset>(
                    RoundTripAssetPath);
                if (persistent == null)
                {
                    AssetDatabase.CreateAsset(authored, RoundTripAssetPath);
                    persistent = authored;
                }
                else
                {
                    EditorUtility.CopySerialized(authored, persistent);
                    UnityEngine.Object.DestroyImmediate(authored);
                }
                EditorUtility.SetDirty(persistent);
                AssetDatabase.SaveAssets();
                var guid = AssetDatabase.AssetPathToGUID(RoundTripAssetPath);
                Assert(!string.IsNullOrWhiteSpace(guid), "round-trip asset obtains a stable GUID");
                AssetDatabase.ImportAsset(RoundTripAssetPath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var reopened = AssetDatabase.LoadAssetAtPath<BattlefieldMapAuthoringAsset>(
                    RoundTripAssetPath);
                Assert(reopened != null && AssetDatabase.AssetPathToGUID(RoundTripAssetPath) == guid,
                    "saved authoring asset reopens under the same GUID");
                Assert(SerializeSource(reopened.ToSource()) == expectedSource,
                    "save/import/reload preserves header, cells, route order, groups, and markers");
                Assert(SerializeDiagnostics(reopened.CollectDiagnostics()) == expectedDiagnostics,
                    "save/import/reload preserves structured compiler diagnostics");
                var reopenedCompiled = Compile(reopened);
                Assert(reopenedCompiled.GameplayFingerprint == expectedCompiled.GameplayFingerprint,
                    "save/import/reload preserves the gameplay fingerprint");
            }
            finally { }
        }

        private static void ValidateLayerIndependenceAndRecommendation()
        {
            var asset = CreateValidMap("map.smoke.layers");
            try
            {
                string reason;
                var presentationBeforeGameplay = SerializePresentation(asset.ToSource());
                Assert(asset.TrySetGameplay(new Vector2Int(1, 1),
                        new[] { BattlefieldLayerIds.Capabilities.Plantable },
                        new[] { BattlefieldLayerIds.Collisions.BlocksProjectile }, out reason),
                    reason);
                Assert(SerializePresentation(asset.ToSource()) == presentationBeforeGameplay,
                    "gameplay painting does not change presentation");

                var gameplayBeforePresentation = SerializeGameplayAndTopology(asset.ToSource());
                var fingerprintBeforePresentation = Compile(asset).GameplayFingerprint;
                Assert(asset.TrySetVisual(new Vector2Int(1, 1),
                        BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.EdgeStyles.Refined, out reason), reason);
                Assert(SerializeGameplayAndTopology(asset.ToSource())
                        == gameplayBeforePresentation,
                    "presentation painting does not change gameplay, route, or markers");
                Assert(Compile(asset).GameplayFingerprint == fingerprintBeforePresentation,
                    "presentation painting leaves gameplay fingerprint stable");

                gameplayBeforePresentation = SerializeGameplayAndTopology(asset.ToSource());
                fingerprintBeforePresentation = Compile(asset).GameplayFingerprint;
                Assert(asset.ApplyRecommendedPresentation(out reason), reason);
                Assert(SerializeGameplayAndTopology(asset.ToSource())
                        == gameplayBeforePresentation,
                    "recommended presentation changes no gameplay, route, group, or marker bytes");
                Assert(Compile(asset).GameplayFingerprint == fingerprintBeforePresentation,
                    "recommended presentation leaves gameplay fingerprint stable");
                foreach (var routeCell in asset.PrimaryRoute.Cells)
                {
                    Assert(asset.TryGetVisual(routeCell, out var visual)
                        && visual.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Soil
                        && string.IsNullOrEmpty(visual.LandformSurfaceId)
                        && string.IsNullOrEmpty(visual.ContourStyleId)
                        && string.IsNullOrEmpty(visual.EdgeStyleId),
                        "recommendation leaves monster-route cells as base-only dirt");
                }
                Assert(asset.TryGetVisual(new Vector2Int(0, 1), out var plantableVisual)
                    && plantableVisual.LandformSurfaceId == BattlefieldLayerIds.Surfaces.Grass
                    && plantableVisual.ContourStyleId
                        == BattlefieldLayerIds.ContourStyles.Square,
                    "recommendation paints plantable cells as grass");
                Assert(asset.TryGetVisual(new Vector2Int(3, 2), out var remainingVisual)
                    && string.IsNullOrEmpty(remainingVisual.LandformSurfaceId),
                    "recommendation leaves remaining cells as soil");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static void ValidateContourSerializationAndComponents()
        {
            var asset = BattlefieldMapAuthoringAsset.Create("map.smoke.contours", 3, 3);
            try
            {
                var component = new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0),
                    new Vector2Int(1, 1), new Vector2Int(2, 2),
                };
                Assert(asset.TrySetVisualCells(component,
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square, string.Empty,
                    out var reason), reason);
                Assert(component.All(cell => asset.TryGetVisual(cell, out var visual)
                        && visual.ContourStyleId == BattlefieldLayerIds.ContourStyles.Square),
                    "new landforms default to explicit square contour serialization");

                Assert(asset.TrySetVisual(new Vector2Int(1, 0),
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined, out reason), reason);
                Assert(component.All(cell => asset.TryGetVisual(cell, out var visual)
                        && visual.EdgeStyleId == BattlefieldLayerIds.EdgeStyles.Refined),
                    "one edge gesture updates the complete shared-vertex exact region");

                Assert(asset.TrySetVisual(new Vector2Int(0, 0),
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Organic, string.Empty, out reason), reason);
                Assert(component.All(cell => asset.TryGetVisual(cell, out var visual)
                        && visual.ContourStyleId == BattlefieldLayerIds.ContourStyles.Organic
                        && string.IsNullOrEmpty(visual.EdgeStyleId)),
                    "one contour change updates the complete component and its exact edge region");

                Assert(asset.TrySetVisual(new Vector2Int(2, 2),
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.ContourStyles.Organic,
                    BattlefieldLayerIds.EdgeStyles.Refined, out reason), reason);
                Assert(asset.TrySetVisual(new Vector2Int(0, 0),
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square, string.Empty, out reason), reason);
                Assert(component.All(cell => asset.TryGetVisual(cell, out var visual)
                        && visual.ContourStyleId == BattlefieldLayerIds.ContourStyles.Square)
                    && asset.TryGetVisual(new Vector2Int(2, 2), out var reverse)
                    && string.IsNullOrEmpty(reverse.EdgeStyleId),
                    "component contour switching clears a non-selected exact edge that may be unavailable in the target contour");

                Assert(!asset.TrySetVisual(new Vector2Int(0, 2),
                        BattlefieldLayerIds.Surfaces.Soil, string.Empty,
                        BattlefieldLayerIds.ContourStyles.Square, string.Empty, out reason)
                    && reason.Contains("reviewed"),
                    "base-only authoring refuses contour metadata instead of retaining it");
                var json = EditorJsonUtility.ToJson(asset, true);
                Assert(json.Contains("contourStyleId")
                    && json.Contains(BattlefieldLayerIds.ContourStyles.Square),
                    "contour identity is serialized explicitly in the authoring aggregate");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static void ValidateRouteAndTypedMarkers()
        {
            var asset = BattlefieldMapAuthoringAsset.Create("map.smoke.route", 5, 4);
            try
            {
                string reason;
                Assert(asset.TryAppendRouteCell(new Vector2Int(0, 0), out reason), reason);
                Assert(asset.TryAppendRouteCell(new Vector2Int(1, 0), out reason), reason);
                var beforeDisconnected = EditorJsonUtility.ToJson(asset, true);
                Assert(!asset.TryAppendRouteCell(new Vector2Int(2, 1), out reason),
                    "diagonal route append is rejected");
                Assert(beforeDisconnected == EditorJsonUtility.ToJson(asset, true),
                    "disconnected route append is atomic");
                Assert(asset.TryAppendRouteCell(new Vector2Int(2, 0), out reason), reason);
                Assert(asset.TrySynchronizeRouteEndpoints(out reason), reason);
                var spawn = asset.Markers.Single(marker =>
                    marker.Kind == BattlefieldMarkerKind.EnemySpawn);
                var goal = asset.Markers.Single(marker =>
                    marker.Kind == BattlefieldMarkerKind.RouteGoal);
                Assert(spawn.MarkerId == "marker.enemy-spawn.main"
                    && spawn.RouteId == BattlefieldLayerIds.PrimaryRoute
                    && spawn.Cell == new Vector2Int(0, 0)
                    && goal.MarkerId == "marker.route-goal.main"
                    && goal.RouteId == BattlefieldLayerIds.PrimaryRoute
                    && goal.Cell == new Vector2Int(2, 0),
                    "route synchronization creates stable typed endpoint markers");

                string markerId;
                Assert(asset.TryPlaceMarker(BattlefieldMarkerKind.Core,
                    new Vector2Int(3, 0), null, out markerId, out reason), reason);
                Assert(markerId == "marker.core.main", "core marker identity is stable");
                Assert(asset.TryPlaceMarker(BattlefieldMarkerKind.Core,
                    new Vector2Int(3, 1), null, out var movedCoreId, out reason), reason);
                Assert(movedCoreId == markerId && asset.Markers.Count(marker =>
                    marker.Kind == BattlefieldMarkerKind.Core) == 1,
                    "moving the core retains one stable typed marker");

                Assert(asset.TrySetMarkerGroup("pots.smoke", 1, out reason), reason);
                Assert(asset.TryPlaceMarker(BattlefieldMarkerKind.InitialPotCandidate,
                    new Vector2Int(0, 1), "pots.smoke", out var firstPotId, out reason), reason);
                Assert(asset.TryRemoveMarker(firstPotId, out reason), reason);
                Assert(asset.TryPlaceMarker(BattlefieldMarkerKind.InitialPotCandidate,
                    new Vector2Int(0, 1), "pots.smoke", out var reopenedPotId, out reason), reason);
                Assert(firstPotId == reopenedPotId
                    && firstPotId == "marker.initial-pot.pots-smoke.x0-y1",
                    "initial-pot identity is stable across remove/re-place");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        internal static BattlefieldMapAuthoringAsset CreateValidMap(string mapId)
        {
            var asset = BattlefieldMapAuthoringAsset.Create(mapId, 4, 3);
            string reason;
            foreach (var cell in new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
            })
            {
                Assert(asset.TrySetGameplay(cell,
                    new[] { BattlefieldLayerIds.Capabilities.EnemyTraversable },
                    Array.Empty<string>(), out reason), reason);
                Assert(asset.TryAppendRouteCell(cell, out reason), reason);
            }
            Assert(asset.TrySynchronizeRouteEndpoints(out reason), reason);
            string markerId;
            Assert(asset.TryPlaceMarker(BattlefieldMarkerKind.Core,
                new Vector2Int(3, 0), null, out markerId, out reason), reason);
            Assert(asset.TrySetGameplay(new Vector2Int(0, 1),
                new[] { BattlefieldLayerIds.Capabilities.Plantable },
                Array.Empty<string>(), out reason), reason);
            Assert(asset.TrySetMarkerGroup("pots.smoke", 1, out reason), reason);
            Assert(asset.TryPlaceMarker(BattlefieldMarkerKind.InitialPotCandidate,
                new Vector2Int(0, 1), "pots.smoke", out markerId, out reason), reason);
            return asset;
        }

        internal static CompiledBattlefieldMap Compile(BattlefieldMapAuthoringAsset asset)
        {
            Assert(BattlefieldLayeredMapCompiler.TryCompile(asset.ToSource(),
                out var compiled, out var validation),
                "canonical authored map compiles: " + string.Join(" | ",
                    validation.Issues.Select(issue => issue.ToString()).ToArray()));
            return compiled;
        }

        internal static string SerializeSource(BattlefieldLayeredMapSource source)
        {
            var builder = new StringBuilder();
            builder.Append(source.SchemaVersion).Append('|').Append(source.MapId).Append('|')
                .Append(source.GridWidth).Append('x').Append(source.GridHeight).Append('|')
                .Append(source.MapUnitsPerCell.ToString("R",
                    System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(source.PrimaryRouteId).AppendLine();
            foreach (var cell in source.VisualCells)
                builder.Append(cell == null ? "<null>" : cell.BaseSurfaceId + ","
                    + cell.LandformSurfaceId + "," + cell.ContourStyleId + ","
                    + cell.EdgeStyleId).AppendLine();
            builder.AppendLine("--gameplay--");
            foreach (var cell in source.GameplayCells)
                builder.Append(cell == null ? "<null>" : string.Join(",", cell.CapabilityIds)
                    + "/" + string.Join(",", cell.CollisionIds)).AppendLine();
            builder.AppendLine("--routes--");
            foreach (var route in source.Routes)
                builder.Append(route == null ? "<null>" : route.RouteId + ":"
                    + string.Join(";", route.Cells.Select(Cell))).AppendLine();
            builder.AppendLine("--groups--");
            foreach (var group in source.MarkerGroups)
                builder.Append(group == null ? "<null>" : group.GroupId + ":"
                    + group.MarkerKind + ":" + group.SelectionCount).AppendLine();
            builder.AppendLine("--markers--");
            foreach (var marker in source.Markers)
                builder.Append(marker == null ? "<null>" : marker.MarkerId + ":"
                    + marker.Kind + ":" + Cell(marker.Cell) + ":" + marker.RouteId
                    + ":" + marker.GroupId + ":" + marker.ContentId + ":" + marker.Facing)
                    .AppendLine();
            return builder.ToString();
        }

        private static string SerializePresentation(BattlefieldLayeredMapSource source)
        {
            return string.Join("|", source.VisualCells.Select(cell => cell == null
                ? "<null>" : cell.BaseSurfaceId + "," + cell.LandformSurfaceId
                    + "," + cell.ContourStyleId + "," + cell.EdgeStyleId));
        }

        private static string SerializeGameplayAndTopology(BattlefieldLayeredMapSource source)
        {
            var withoutPresentation = new BattlefieldLayeredMapSource(source.SchemaVersion,
                source.MapId, source.GridWidth, source.GridHeight, source.MapUnitsPerCell,
                source.PrimaryRouteId,
                Enumerable.Repeat(new BattlefieldVisualCellSource(
                    BattlefieldLayerIds.Surfaces.Soil), source.VisualCells.Count),
                source.GameplayCells, source.Routes, source.MarkerGroups, source.Markers,
                source.ExecutionProfile);
            return SerializeSource(withoutPresentation);
        }

        private static string SerializeDiagnostics(
            IReadOnlyList<BattlefieldMapAuthoringDiagnostic> diagnostics)
        {
            return string.Join("|", diagnostics.Select(value => value.Severity + ":"
                + value.Code + ":" + value.Field + ":" + value.HasCell + ":"
                + Cell(value.Cell) + ":" + value.MarkerId));
        }

        private static string Cell(Vector2Int value)
        {
            return value.x + "," + value.y;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var split = path.LastIndexOf('/');
            var parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Canonical battlefield map authoring smoke failed: " + message);
        }
    }
}
