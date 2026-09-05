using System;
using System.IO;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class CanonicalBattlefieldMapEditorSmoke
    {
        public static void Validate()
        {
            var asset = CreateTransientAsset();
            try
            {
                ValidateSharedCanvas(asset);
                ValidateRectangleEyedropperAndUndo(asset);
                ValidateFloodFillAndUndo(asset);
                ValidateRecommendationAndResizeUndo(asset);
                ValidateContourPopupAvailability();
                ValidateEditorBattleTerrainParity();
                Debug.Log("CANONICAL_BATTLEFIELD_MAP_EDITOR_SMOKE_OK");
            }
            finally
            {
                Undo.ClearAll();
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static void ValidateContourPopupAvailability()
        {
            var palette = LoadCurrentReleasePalette();
            var registeredContours = palette.LandformBindings
                .Where(binding => binding != null && binding.TileSet != null
                    && binding.SurfaceId == BattlefieldLayerIds.Surfaces.Soil)
                .Select(binding => binding.ContourStyleId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var baseContours = CanonicalBattlefieldMapEditorWindow.AvailableContourStyles(
                palette, BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass, string.Empty);
            Assert(baseContours.SequenceEqual(registeredContours)
                && baseContours.Contains(BattlefieldLayerIds.ContourStyles.Organic)
                && baseContours.Contains(BattlefieldLayerIds.ContourStyles.Square)
                && baseContours.SequenceEqual(baseContours.OrderBy(value => value,
                    StringComparer.Ordinal)),
                "contour popup exposes registered landforms in stable order when no edge is selected; registered="
                + string.Join(",", registeredContours) + "; actual="
                + string.Join(",", baseContours));

            var reverseRefined = CanonicalBattlefieldMapEditorWindow.AvailableContourStyles(
                palette, BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.EdgeStyles.Refined);
            var compatibleContours = registeredContours
                .Where(contour => palette.TryGetEdgeTileSet(
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass, contour,
                    BattlefieldLayerIds.EdgeStyles.Refined, out _, out _))
                .ToArray();
            Assert(reverseRefined.SequenceEqual(compatibleContours)
                && reverseRefined.Contains(BattlefieldLayerIds.ContourStyles.Organic)
                && reverseRefined.Contains(BattlefieldLayerIds.ContourStyles.Square),
                "selected edge filters contour choices to a same-contour resource in either material direction; registered="
                + string.Join(",", compatibleContours) + "; actual="
                + string.Join(",", reverseRefined));

            var unavailable = CanonicalBattlefieldMapEditorWindow.AvailableContourStyles(
                palette, BattlefieldLayerIds.Surfaces.StoneRoad,
                BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.EdgeStyles.Refined);
            Assert(unavailable.Length == 0,
                "contour popup never enables an unavailable same-contour edge combination");
        }

        private static BattlefieldTerrainPalette LoadCurrentReleasePalette()
        {
            var persisted = LayeredTerrainArtSetup.RequirePaletteAssets();
            Assert(persisted != null && persisted.LandformBindings.Count > 0,
                "current contour registry is persisted in the release palette asset");

            var palette = BattlefieldMapPublicationExporter.LoadReleaseRegisteredPalettes()
                .Single(value => value.PaletteId
                    == BundledLevelCatalogIds.TerrainPalettes.OrchardDefault);
            Assert(AssetDatabase.GetAssetPath(palette)
                    == ProjectSetup.BattlefieldTerrainPalettePath,
                "contour popup reads the persisted palette registered by the Battle scene");
            foreach (var surface in new[]
            {
                BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.Surfaces.StoneRoad,
            })
            foreach (var contour in new[]
            {
                BattlefieldLayerIds.ContourStyles.Organic,
                BattlefieldLayerIds.ContourStyles.Square,
            })
                Assert(palette.TryGetLandformTileSet(surface, contour, out _),
                    "release palette persists exact landform registry key "
                    + surface + " / " + contour);
            Assert(palette.EdgeBindings.All(binding => binding != null
                    && !string.IsNullOrWhiteSpace(binding.ContourStyleId))
                && palette.TryGetEdgeTileSet(BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Organic,
                    BattlefieldLayerIds.EdgeStyles.Refined, out _)
                && palette.TryGetEdgeTileSet(BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined, out _),
                "release palette resolves contour-specific edge resources in both material directions");
            Assert(palette.Validate(out var reason),
                "release contour popup palette is current and valid: " + reason);
            return palette;
        }

        private static void ValidateSharedCanvas(BattlefieldMapAuthoringAsset asset)
        {
            var layout = new CanonicalBattlefieldCanvasLayout(
                new Rect(18f, 27f, 320f, 240f), asset.GridWidth, asset.GridHeight, 1.25f);
            foreach (CanonicalBattlefieldMapWorkspace workspace in
                     Enum.GetValues(typeof(CanonicalBattlefieldMapWorkspace)))
            {
                foreach (var cell in new[]
                {
                    Vector2Int.zero,
                    new Vector2Int(asset.GridWidth - 1, asset.GridHeight - 1),
                    new Vector2Int(2, 1),
                })
                {
                    var rect = layout.CellRect(cell);
                    Assert(layout.TryHit(rect.center, out var hit) && hit == cell,
                        "workspace " + workspace + " shares one aligned cell/hit layout");
                }
            }
            Assert(!layout.TryHit(new Vector2(layout.CanvasRect.x - .1f,
                    layout.CanvasRect.y), out _)
                && !layout.TryHit(new Vector2(layout.CanvasRect.x
                    + asset.GridWidth * layout.CellSize + .1f,
                    layout.CanvasRect.y), out _),
                "shared canvas rejects pointer hits beyond both horizontal bounds");
        }

        private static void ValidateRectangleEyedropperAndUndo(
            BattlefieldMapAuthoringAsset asset)
        {
            Reset(asset);
            Assert(CanonicalBattlefieldMapEditorOperations.TryResolveRectangle(asset,
                    new Vector2Int(2, 2), new Vector2Int(1, 0), out var rectangle,
                    out var reason), reason);
            Assert(rectangle.SequenceEqual(new[]
                {
                    new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(1, 1), new Vector2Int(2, 1),
                    new Vector2Int(1, 2), new Vector2Int(2, 2),
                }), "rectangle resolves the exact bounded row-major cell set");
            var before = EditorJsonUtility.ToJson(asset, true);
            var undoGroup = BeginUndo(asset, "Smoke rectangle presentation");
            Assert(CanonicalBattlefieldMapEditorOperations.TryApplyVisual(asset, rectangle,
                BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.EdgeStyles.Refined, out reason), reason);
            CompleteUndo(asset, undoGroup);
            var after = EditorJsonUtility.ToJson(asset, true);
            Assert(after != before && rectangle.All(cell => asset.TryGetVisual(cell,
                    out var visual)
                && visual.LandformSurfaceId == BattlefieldLayerIds.Surfaces.Grass
                && visual.ContourStyleId == BattlefieldLayerIds.ContourStyles.Square
                && visual.EdgeStyleId == BattlefieldLayerIds.EdgeStyles.Refined),
                "rectangle changes only the requested presentation cells");
            Undo.PerformUndo();
            Assert(EditorJsonUtility.ToJson(asset, true) == before,
                "one Undo restores the complete rectangle gesture");
            Undo.PerformRedo();
            Assert(EditorJsonUtility.ToJson(asset, true) == after,
                "one Redo reapplies the complete rectangle gesture");

            Assert(asset.TryGetVisual(new Vector2Int(1, 1), out var picked),
                "eyedropper resolves an authored visual record");
            Assert(CanonicalBattlefieldMapEditorOperations.TryApplyVisual(asset,
                new[] { new Vector2Int(3, 2) }, picked.BaseSurfaceId,
                picked.LandformSurfaceId, picked.ContourStyleId,
                picked.EdgeStyleId, out reason), reason);
            Assert(asset.TryGetVisual(new Vector2Int(3, 2), out var applied)
                && applied.BaseSurfaceId == picked.BaseSurfaceId
                && applied.LandformSurfaceId == picked.LandformSurfaceId
                && applied.ContourStyleId == picked.ContourStyleId
                && applied.EdgeStyleId == picked.EdgeStyleId,
                "visual eyedropper applies the exact surface/contour/edge identities");

            var beforeContour = EditorJsonUtility.ToJson(asset, true);
            var contourGroup = BeginUndo(asset, "Smoke connected contour change");
            Assert(CanonicalBattlefieldMapEditorOperations.TryApplyVisual(asset,
                new[] { new Vector2Int(1, 1) }, picked.BaseSurfaceId,
                picked.LandformSurfaceId, BattlefieldLayerIds.ContourStyles.Organic,
                picked.EdgeStyleId, out reason), reason);
            CompleteUndo(asset, contourGroup);
            Assert(rectangle.All(cell => asset.TryGetVisual(cell, out var visual)
                    && visual.ContourStyleId == BattlefieldLayerIds.ContourStyles.Organic),
                "one contour gesture updates the complete connected component");
            Undo.PerformUndo();
            Assert(EditorJsonUtility.ToJson(asset, true) == beforeContour,
                "one Undo restores the complete connected contour update");
        }

        private static void ValidateFloodFillAndUndo(BattlefieldMapAuthoringAsset asset)
        {
            Reset(asset);
            string reason;
            var barrier = new[]
            {
                new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2),
            };
            Assert(asset.TrySetVisualCells(barrier, BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass, string.Empty, out reason), reason);
            Assert(CanonicalBattlefieldMapEditorOperations.TryResolveVisualFlood(asset,
                new Vector2Int(0, 1), out var visualFlood, out reason), reason);
            Assert(visualFlood.SequenceEqual(new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2),
                }), "visual flood remains in the bounded cardinally connected region");

            var before = EditorJsonUtility.ToJson(asset, true);
            var group = BeginUndo(asset, "Smoke visual flood");
            Assert(CanonicalBattlefieldMapEditorOperations.TryApplyVisual(asset, visualFlood,
                BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.StoneRoad, string.Empty, out reason), reason);
            CompleteUndo(asset, group);
            var after = EditorJsonUtility.ToJson(asset, true);
            Assert(after != before, "visual flood changes its resolved region");
            Undo.PerformUndo();
            Assert(EditorJsonUtility.ToJson(asset, true) == before,
                "one Undo restores the entire visual flood batch");
            Undo.PerformRedo();
            Assert(EditorJsonUtility.ToJson(asset, true) == after,
                "one Redo reapplies the entire visual flood batch");

            Reset(asset);
            Assert(asset.TrySetGameplayCells(barrier,
                new[] { BattlefieldLayerIds.Capabilities.Plantable },
                Array.Empty<string>(), out reason), reason);
            Assert(CanonicalBattlefieldMapEditorOperations.TryResolveGameplayFlood(asset,
                new Vector2Int(0, 1), out var gameplayFlood, out reason), reason);
            Assert(gameplayFlood.SequenceEqual(new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2),
                }), "gameplay flood compares capabilities/collisions and remains bounded");
            Assert(asset.TryGetGameplay(new Vector2Int(1, 1), out var gameplayPicked)
                && CanonicalBattlefieldMapEditorOperations.TryApplyGameplay(asset,
                    new[] { new Vector2Int(3, 2) }, gameplayPicked.CapabilityIds,
                    gameplayPicked.CollisionIds, out reason)
                && asset.TryGetGameplay(new Vector2Int(3, 2), out var gameplayApplied)
                && gameplayApplied.CapabilityIds.SequenceEqual(gameplayPicked.CapabilityIds)
                && gameplayApplied.CollisionIds.SequenceEqual(gameplayPicked.CollisionIds),
                "gameplay eyedropper applies the exact reviewed capability/collision record");
        }

        private static void ValidateRecommendationAndResizeUndo(
            BattlefieldMapAuthoringAsset asset)
        {
            Reset(asset);
            var valid = CanonicalBattlefieldMapAuthoringSmoke.CreateValidMap(
                "map.smoke.editor-undo-source");
            EditorUtility.CopySerialized(valid, asset);
            UnityEngine.Object.DestroyImmediate(valid);
            asset.name = "EditorOperationsSmoke";

            var beforeRecommendation = EditorJsonUtility.ToJson(asset, true);
            var gameplayBefore = GameplayBytes(asset);
            var recommendationGroup = BeginUndo(asset, "Smoke recommendation");
            Assert(asset.ApplyRecommendedPresentation(out var reason), reason);
            CompleteUndo(asset, recommendationGroup);
            var afterRecommendation = EditorJsonUtility.ToJson(asset, true);
            Assert(GameplayBytes(asset) == gameplayBefore,
                "undoable recommendation keeps gameplay/topology byte-equivalent");
            Undo.PerformUndo();
            Assert(EditorJsonUtility.ToJson(asset, true) == beforeRecommendation,
                "one Undo restores the whole recommendation batch");
            Undo.PerformRedo();
            Assert(EditorJsonUtility.ToJson(asset, true) == afterRecommendation,
                "one Redo reapplies the whole recommendation batch");

            var beforeResize = EditorJsonUtility.ToJson(asset, true);
            var resizeGroup = BeginUndo(asset, "Smoke resize");
            Assert(asset.TryResize(6, 5, out var report, out reason), reason);
            CompleteUndo(asset, resizeGroup);
            Assert(report.RemovedMarkerIds.Count == 0 && report.RemovedRouteCells.Count == 0
                && asset.GridWidth == 6 && asset.GridHeight == 5,
                "resize expands one complete aggregate with explicit report");
            Undo.PerformUndo();
            Assert(EditorJsonUtility.ToJson(asset, true) == beforeResize,
                "one Undo restores the previous aggregate dimensions and records");
        }

        private static void ValidateEditorBattleTerrainParity()
        {
            var asset = CanonicalBattlefieldMapAuthoringSmoke.CreateValidMap(
                "map.smoke.terrain-parity");
            try
            {
                string reason;
                Assert(asset.ApplyRecommendedPresentation(out reason), reason);
                Assert(asset.TrySetVisual(new Vector2Int(0, 1),
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.EdgeStyles.Refined, out reason), reason);
                var definition = new BattlefieldMapDefinition(
                    CanonicalBattlefieldMapAuthoringSmoke.Compile(asset));
                var palette = BattlefieldMapPublicationExporter.LoadReleaseRegisteredPalettes()
                    .Single(value => value.PaletteId
                        == BundledLevelCatalogIds.TerrainPalettes.OrchardDefault);
                Assert(palette.TryGetBaseTexture(BattlefieldLayerIds.Surfaces.Soil,
                        out var soilBase) && soilBase != null,
                    "editor preview resolves the template's real soil base texture");
                Assert(palette.TryGetLandformTileSet(BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.ContourStyles.Square,
                        out var grassTiles) && grassTiles != null,
                    "editor preview resolves the template's real grass landform TileSet");
                Assert(palette.TryGetEdgeTileSet(BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.ContourStyles.Square,
                        BattlefieldLayerIds.EdgeStyles.Refined, out var edgeTiles)
                    && edgeTiles != null,
                    "editor preview resolves the exact directed refined edge TileSet");

                var sawLandformTransition = false;
                var sawEdge = false;
                for (var vertexY = 0; vertexY <= asset.GridHeight; vertexY++)
                for (var vertexX = 0; vertexX <= asset.GridWidth; vertexX++)
                {
                    var authoredLandform = ResolveAuthoredMask(asset, vertexX, vertexY,
                        visual => visual.LandformSurfaceId
                                == BattlefieldLayerIds.Surfaces.Grass
                            && visual.ContourStyleId
                                == BattlefieldLayerIds.ContourStyles.Square);
                    var battleLandform = BattlefieldDualGridTerrain.ResolveLandformMask(
                        definition, vertexX, vertexY, BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.ContourStyles.Square);
                    Assert(authoredLandform == battleLandform,
                        "editor-authored and Battle landform masks match at vertex "
                        + vertexX + "," + vertexY);
                    if (battleLandform != DualGridMask.Empty
                        && battleLandform != DualGridMask.Full) sawLandformTransition = true;
                    if (battleLandform != DualGridMask.Empty)
                    {
                        Assert(grassTiles.TryGetSprite(battleLandform, out var sprite)
                            && sprite != null,
                            "real palette resolves the same Battle landform mask sprite");
                    }

                    var authoredEdge = ResolveAuthoredMask(asset, vertexX, vertexY,
                        visual => visual.LandformSurfaceId
                                == BattlefieldLayerIds.Surfaces.Grass
                            && visual.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Soil
                            && visual.ContourStyleId
                                == BattlefieldLayerIds.ContourStyles.Square
                            && visual.EdgeStyleId == BattlefieldLayerIds.EdgeStyles.Refined);
                    var battleEdge = BattlefieldDualGridTerrain.ResolveEdgeMask(definition,
                        vertexX, vertexY, BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.ContourStyles.Square,
                        BattlefieldLayerIds.EdgeStyles.Refined);
                    Assert(authoredEdge == battleEdge,
                        "editor-authored and Battle exact edge masks match at vertex "
                        + vertexX + "," + vertexY);
                    if (battleEdge != DualGridMask.Empty)
                    {
                        sawEdge = true;
                        Assert(edgeTiles.TryGetSprite(battleEdge, out var edgeSprite)
                            && edgeSprite != null,
                            "real palette resolves the same exact-edge mask sprite");
                    }
                }
                Assert(sawLandformTransition && sawEdge,
                    "terrain parity fixture exercises base, transition masks, and exact edges");

                var editorSource = File.ReadAllText(
                    "Assets/Editor/Tools/CanonicalBattlefieldMapEditorWindow.cs");
                Assert(editorSource.Contains(
                        "BattlefieldDualGridTerrain.ResolveLandformMask")
                    && editorSource.Contains("BattlefieldDualGridTerrain.ResolveEdgeMask")
                    && editorSource.Contains("palette.TryGetBaseTexture"),
                    "official editor preview delegates base/landform/edge resolution to the same Battle path");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static DualGridMask ResolveAuthoredMask(BattlefieldMapAuthoringAsset asset,
            int vertexX, int vertexY,
            Func<BattlefieldVisualCellAuthoringRecord, bool> matches)
        {
            return DualGridMaskUtility.Resolve(logicalCell =>
            {
                var cell = new Vector2Int(logicalCell.x, -logicalCell.y - 1);
                return asset.TryGetVisual(cell, out var visual) && matches(visual);
            }, new Vector3Int(vertexX, -vertexY, 0));
        }

        private static BattlefieldMapAuthoringAsset CreateTransientAsset()
        {
            var asset = BattlefieldMapAuthoringAsset.Create(
                "map.smoke.editor-operations", 4, 3);
            asset.name = "EditorOperationsSmoke";
            return asset;
        }

        private static void Reset(BattlefieldMapAuthoringAsset asset)
        {
            Undo.ClearAll();
            var fresh = BattlefieldMapAuthoringAsset.Create("map.smoke.editor-operations", 4, 3);
            EditorUtility.CopySerialized(fresh, asset);
            UnityEngine.Object.DestroyImmediate(fresh);
            asset.name = "EditorOperationsSmoke";
        }

        private static int BeginUndo(BattlefieldMapAuthoringAsset asset, string label)
        {
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(label);
            Undo.RegisterCompleteObjectUndo(asset, label);
            return group;
        }

        private static void CompleteUndo(BattlefieldMapAuthoringAsset asset, int group)
        {
            EditorUtility.SetDirty(asset);
            Undo.CollapseUndoOperations(group);
        }

        private static string GameplayBytes(BattlefieldMapAuthoringAsset asset)
        {
            var source = asset.ToSource();
            return string.Join("|", source.GameplayCells.Select(cell =>
                       string.Join(",", cell.CapabilityIds) + "/"
                       + string.Join(",", cell.CollisionIds)))
                + "#" + string.Join("|", source.Routes.Select(route => route.RouteId + ":"
                    + string.Join(";", route.Cells.Select(cell => cell.x + "," + cell.y))))
                + "#" + string.Join("|", source.MarkerGroups.Select(group => group.GroupId
                    + ":" + group.MarkerKind + ":" + group.SelectionCount))
                + "#" + string.Join("|", source.Markers.Select(marker => marker.MarkerId
                    + ":" + marker.Kind + ":" + marker.Cell.x + "," + marker.Cell.y
                    + ":" + marker.RouteId + ":" + marker.GroupId));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Canonical battlefield editor smoke failed: " + message);
        }
    }
}
