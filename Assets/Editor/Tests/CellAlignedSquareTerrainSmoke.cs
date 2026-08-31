using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public static class CellAlignedSquareTerrainSmoke
    {
        [MenuItem("Fruit Defense/Validation/Run Cell-Aligned Square Terrain Smoke")]
        public static void Run()
        {
            ValidatePresetSemantics();
            ValidateRepresentationCompatibility();
            CellAlignedSquareTerrainTrial.GenerateArtifacts();
            Assert(CellAlignedSquareTerrainTrial.Validate(out var trialReason),
                "trial art and isolation: " + trialReason);
            ValidateComparisonBoard();
            Debug.Log("FRUIT_DEFENSE_CELL_ALIGNED_SQUARE_TERRAIN_OK");
        }

        private static void ValidatePresetSemantics()
        {
            Assert(CellAlignedSquareTerrainPresets.All.Count == 2,
                "grass and soil pure-square presets are explicit");
            var map = BattlefieldMapAuthoringAsset.Create("smoke.square-presets", 2, 1);
            try
            {
                Assert(map.TrySetVisual(new Vector2Int(0, 0),
                        BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.ContourStyles.Square,
                        BattlefieldLayerIds.EdgeStyles.Refined, out var layeredReason),
                    "layered preset fixture: " + layeredReason);
                Assert(CellAlignedSquareTerrainPresets.TryApply(map,
                        new[] { new Vector2Int(0, 0) },
                        BattlefieldLayerIds.Surfaces.Soil, out var soilReason),
                    "soil-square preset application: " + soilReason);
                Assert(map.TryGetVisual(new Vector2Int(0, 0), out var soil)
                    && soil.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Soil
                    && string.IsNullOrEmpty(soil.LandformSurfaceId)
                    && string.IsNullOrEmpty(soil.ContourStyleId)
                    && string.IsNullOrEmpty(soil.EdgeStyleId),
                    "soil-square preset clears every optional layer identity");

                Assert(CellAlignedSquareTerrainPresets.TryApply(map,
                        new[] { new Vector2Int(1, 0) },
                        BattlefieldLayerIds.Surfaces.Grass, out var grassReason),
                    "grass-square preset application: " + grassReason);
                Assert(map.TryGetVisual(new Vector2Int(1, 0), out var grass)
                    && grass.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Grass
                    && string.IsNullOrEmpty(grass.LandformSurfaceId)
                    && string.IsNullOrEmpty(grass.ContourStyleId)
                    && string.IsNullOrEmpty(grass.EdgeStyleId),
                    "grass-square preset writes the base-only representation");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(map);
            }
        }

        private static void ValidateRepresentationCompatibility()
        {
            var edgeCells = BaseCells(BattlefieldLayerIds.Surfaces.Soil);
            edgeCells[0] = new BattlefieldVisualCellSource(
                BattlefieldLayerIds.Surfaces.Grass);
            edgeCells[1] = Landform(BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass);
            ExpectRepresentationIssue(edgeCells, "edge-contact grass representation mix");

            var diagonalCells = BaseCells(BattlefieldLayerIds.Surfaces.Grass);
            diagonalCells[0] = new BattlefieldVisualCellSource(
                BattlefieldLayerIds.Surfaces.Soil);
            diagonalCells[5] = Landform(BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.Surfaces.Soil);
            ExpectRepresentationIssue(diagonalCells, "vertex-contact soil representation mix");

            var disconnectedCells = BaseCells(BattlefieldLayerIds.Surfaces.Soil);
            disconnectedCells[0] = new BattlefieldVisualCellSource(
                BattlefieldLayerIds.Surfaces.Grass);
            disconnectedCells[11] = Landform(BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass);
            ExpectValid(disconnectedCells, "disconnected square and Dual-Grid grass");

            var unlikeCells = BaseCells(BattlefieldLayerIds.Surfaces.Soil);
            unlikeCells[0] = new BattlefieldVisualCellSource(
                BattlefieldLayerIds.Surfaces.Grass);
            ExpectValid(unlikeCells, "touching unlike square surfaces");
        }

        private static void ValidateComparisonBoard()
        {
            var previousActive = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(CellAlignedSquareTerrainTrial.ScenePath,
                OpenSceneMode.Additive);
            try
            {
                var renderer = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<LayeredTerrainTilemap>(true))
                    .Single();
                var grassMarker = AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/LayeredTerrain/GrassSoil/Authoring/MarkerA.asset");
                var soilMarker = AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/LayeredTerrain/GrassSoil/Authoring/MarkerB.asset");
                var edgeMarker = AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/LayeredTerrain/GrassSoil/Authoring/EdgeEnabled.asset");
                Assert(grassMarker != null && soilMarker != null && edgeMarker != null,
                    "comparison-board semantic markers are available");

                var dualGridCells = new HashSet<Vector3Int>
                {
                    new Vector3Int(2, 1, 0), new Vector3Int(3, 1, 0),
                    new Vector3Int(4, 1, 0), new Vector3Int(5, 1, 0),
                    new Vector3Int(1, 2, 0), new Vector3Int(2, 2, 0),
                    new Vector3Int(3, 2, 0), new Vector3Int(4, 2, 0),
                    new Vector3Int(5, 2, 0), new Vector3Int(6, 2, 0),
                    new Vector3Int(2, 3, 0), new Vector3Int(3, 3, 0),
                    new Vector3Int(4, 3, 0), new Vector3Int(5, 3, 0),
                };
                var expectedBaseCount = 0;
                var approvedGrassCount = 0;
                var approvedSoilCount = 0;
                for (var y = 0; y < CellAlignedSquareTerrainTrial.BoardHeight; y++)
                for (var x = 0; x < CellAlignedSquareTerrainTrial.BoardWidth; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    var inDualGridReference = y < 5;
                    var inApprovedGrid = y >= 7;
                    var isApprovedGrass = inApprovedGrid && x < 7 && y > 7
                        && y < CellAlignedSquareTerrainTrial.BoardHeight - 1;
                    var expectedBase = isApprovedGrass ? grassMarker
                        : inDualGridReference || inApprovedGrid ? soilMarker : null;
                    Assert(renderer.BaseLogicalTilemap.GetTile(cell) == expectedBase,
                        "comparison-board base semantic at " + cell);
                    Assert(renderer.LandformLogicalTilemap.GetTile(cell)
                            == (dualGridCells.Contains(cell) ? grassMarker : null),
                        "comparison-board landform semantic at " + cell);
                    Assert(renderer.EdgeLogicalTilemap.GetTile(cell)
                            == (dualGridCells.Contains(cell) ? edgeMarker : null),
                        "comparison-board edge semantic at " + cell);
                    if (expectedBase != null) expectedBaseCount++;
                    if (isApprovedGrass) approvedGrassCount++;
                    else if (inApprovedGrid) approvedSoilCount++;
                }
                Assert(renderer.BaseOutputTilemap.GetTilesBlock(new BoundsInt(0, 0, 0,
                            CellAlignedSquareTerrainTrial.BoardWidth,
                            CellAlignedSquareTerrainTrial.BoardHeight, 1))
                        .Count(tile => tile != null)
                    == expectedBaseCount,
                    "comparison board renders one opaque base tile per gameplay cell");
                Assert(approvedGrassCount == 35 && approvedSoilCount == 21,
                    "approved board preserves the exact 7x5 grass and 21-cell soil frame");
                Assert(renderer.LandformAOutputTilemap.cellBounds.size.x > 0,
                    "comparison board renders the separated Dual-Grid grass reference");
                var camera = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
                Assert(Mathf.Approximately(874f / (camera.orthographicSize * 2f), 46f),
                    "comparison board uses the 46-pixel battlefield cell scale");
                Assert(Mathf.Approximately(camera.transform.position.x,
                        CellAlignedSquareTerrainTrial.BoardWidth * .5f)
                    && Mathf.Approximately(camera.transform.position.y,
                        CellAlignedSquareTerrainTrial.BoardHeight * .5f),
                    "comparison board camera is centered on the complete grid bounds");
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static BattlefieldVisualCellSource[] BaseCells(string surfaceId)
        {
            return Enumerable.Range(0, 12)
                .Select(_ => new BattlefieldVisualCellSource(surfaceId)).ToArray();
        }

        private static BattlefieldVisualCellSource Landform(string baseSurfaceId,
            string landformSurfaceId)
        {
            return new BattlefieldVisualCellSource(baseSurfaceId, landformSurfaceId,
                BattlefieldLayerIds.ContourStyles.Square, string.Empty);
        }

        private static void ExpectRepresentationIssue(
            IReadOnlyList<BattlefieldVisualCellSource> visualCells, string label)
        {
            var source = CreateSource(visualCells);
            var success = BattlefieldLayeredMapCompiler.TryCompile(source,
                out var compiled, out var validation);
            var issue = validation.Issues.FirstOrDefault(value => value.Code
                == "surface.shared-representation-mix");
            Assert(!success && compiled == null && issue != null
                && issue.Message.Contains("share an edge or vertex"),
                label + " did not report the focused representation diagnostic: "
                + string.Join(" | ", validation.Issues.Select(value => value.ToString()).ToArray()));
        }

        private static void ExpectValid(IReadOnlyList<BattlefieldVisualCellSource> visualCells,
            string label)
        {
            var source = CreateSource(visualCells);
            Assert(BattlefieldLayeredMapCompiler.TryCompile(source,
                    out var compiled, out var validation) && compiled != null
                    && validation.IsValid,
                label + " failed: "
                + string.Join(" | ", validation.Issues.Select(value => value.ToString()).ToArray()));
        }

        private static BattlefieldLayeredMapSource CreateSource(
            IEnumerable<BattlefieldVisualCellSource> visualCells)
        {
            var baseline = BattlefieldLayeredMapFactory.CreateSingleRouteMap(
                "smoke.cell-aligned-square", 4, 3, 1f,
                new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0),
                    new Vector2Int(2, 0), new Vector2Int(3, 0),
                    new Vector2Int(3, 1),
                }, new Vector2Int(3, 2),
                new[]
                {
                    new InitialPotGroup("pot-group", 1,
                        new[] { new Vector2Int(1, 1) }),
                });
            return new BattlefieldLayeredMapSource(baseline.SchemaVersion,
                baseline.MapId, baseline.GridWidth, baseline.GridHeight,
                baseline.MapUnitsPerCell, baseline.PrimaryRouteId, visualCells,
                baseline.GameplayCells, baseline.Routes, baseline.MarkerGroups,
                baseline.Markers, baseline.ExecutionProfile);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Cell-aligned square terrain smoke failed: " + message);
        }
    }
}
