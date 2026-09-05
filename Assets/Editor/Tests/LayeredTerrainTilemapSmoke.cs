using System;
using System.IO;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public static class LayeredTerrainTilemapSmoke
    {
        public static void Validate()
        {
            LayeredTerrainArtSetup.RequirePaletteAssets();
            ValidateSocketContinuity(LayeredTerrainArtSetup.Root + "/LandformGrass");
            ValidateSocketContinuity(LayeredTerrainArtSetup.Root + "/LandformSoil");
            ValidateSocketContinuity(LayeredTerrainArtSetup.Root + "/EdgeGrassOnSoilRefined");
            var root = new GameObject("LayeredTerrainTilemapSmoke");
            var markerA = ScriptableObject.CreateInstance<Tile>();
            var markerB = ScriptableObject.CreateInstance<Tile>();
            var edgeMarker = ScriptableObject.CreateInstance<Tile>();
            try
            {
                var gridObject = new GameObject("Grid");
                gridObject.transform.SetParent(root.transform, false);
                gridObject.AddComponent<Grid>();
                var baseLogical = AddTilemap(gridObject.transform, "base source");
                var landformLogical = AddTilemap(gridObject.transform, "landform source");
                var edgeLogical = AddTilemap(gridObject.transform, "edge source");
                var baseOutput = AddTilemap(gridObject.transform, "base output");
                var landformAOutput = AddTilemap(gridObject.transform, "landform A output");
                var landformBOutput = AddTilemap(gridObject.transform, "landform B output");
                var edgeAOnBOutput = AddTilemap(gridObject.transform, "edge A on B output");
                var edgeBOnAOutput = AddTilemap(gridObject.transform, "edge B on A output");

                var component = root.AddComponent<LayeredTerrainTilemap>();
                var grassSet = Require<DualGridTileSet>(LayeredTerrainArtSetup.GrassLandformTileSetPath);
                var soilSet = Require<DualGridTileSet>(LayeredTerrainArtSetup.SoilLandformTileSetPath);
                var grassEdge = Require<DualGridTileSet>(LayeredTerrainArtSetup.GrassOnSoilEdgeTileSetPath);
                component.Configure(baseLogical, landformLogical, edgeLogical,
                    baseOutput, landformAOutput, landformBOutput,
                    edgeAOnBOutput, edgeBOnAOutput,
                    markerA, markerB, edgeMarker,
                    Require<TileBase>(LayeredTerrainArtSetup.Root + "/Authoring/GrassBase.asset"),
                    Require<TileBase>(LayeredTerrainArtSetup.Root + "/Authoring/SoilBase.asset"),
                    grassSet, soilSet, grassEdge, null);
                ConfigurePresentation(component);

                Assert(component.ValidateConfiguration(out var reason), reason);
                Assert(component.ValidateAuthoringPresentation(out reason), reason);
                Assert(component.Rebuild(out reason) && component.HasExpectedAlignment(),
                    "empty configured authoring stack rebuilds with half-cell outputs: " + reason);

                var baseOnly = new Vector3Int(0, 0, 0);
                Assert(component.PaintBase(baseOnly, LayeredTerrainMaterial.A, out reason), reason);
                Assert(baseOutput.HasTile(baseOnly) && !landformLogical.HasTile(baseOnly)
                    && !edgeLogical.HasTile(baseOnly),
                    "pure-base painting clears optional landform and edge state");

                var aOnB = new Vector3Int(1, 0, 0);
                Assert(component.PaintPair(aOnB, LayeredTerrainMaterial.A,
                    LayeredTerrainMaterial.B, true, out reason), reason);
                Assert(baseLogical.GetTile(aOnB) == markerB
                    && landformLogical.GetTile(aOnB) == markerA
                    && edgeLogical.GetTile(aOnB) == edgeMarker
                    && edgeAOnBOutput.GetUsedTilesCount() > 0
                    && edgeBOnAOutput.GetUsedTilesCount() == 0,
                    "A on B produces only its exact directed refined edge output");

                var bOnA = new Vector3Int(3, 0, 0);
                Assert(component.PaintPair(bOnA, LayeredTerrainMaterial.B,
                    LayeredTerrainMaterial.A, true, out reason), reason);
                Assert(edgeBOnAOutput.GetUsedTilesCount() > 0
                    && component.TryResolveEdgeTileSet(LayeredTerrainMaterial.B,
                        LayeredTerrainMaterial.A, out var reverseEdge,
                        out var complementReverseMask)
                    && ReferenceEquals(reverseEdge, grassEdge)
                    && complementReverseMask,
                    "B on A reuses the current edge TileSet with complemented masks");
                foreach (var cell in new[]
                {
                    new Vector3Int(4, 0, 0),
                    new Vector3Int(3, 1, 0),
                    new Vector3Int(4, 1, 0),
                })
                    Assert(component.PaintPair(cell, LayeredTerrainMaterial.B,
                        LayeredTerrainMaterial.A, true, out reason), reason);
                var reverseCenterVertex = new Vector3Int(4, 1, 0);
                Assert(DualGridMaskUtility.TryResolveSharedEdgeMask(DualGridMask.Full,
                        true, out var reverseCenterMask)
                    && reverseCenterMask == DualGridMask.Empty
                    && edgeBOnAOutput.GetTile(reverseCenterVertex)
                        == grassEdge.GetTile(DualGridMask.Empty),
                    "B on A keeps its full interior by rendering the shared mask-00 endpoint");
                Assert(!DualGridMaskUtility.TryResolveSharedEdgeMask(DualGridMask.Empty,
                        true, out _),
                    "an unoccupied source vertex remains empty before reverse complementation");
                Assert(!component.CanPaintPair(LayeredTerrainMaterial.A,
                    LayeredTerrainMaterial.A, false, out reason),
                    "unsupported same-material stack is rejected");

                Assert(component.EraseLandform(aOnB, out reason), reason);
                Assert(baseLogical.HasTile(aOnB) && baseOutput.HasTile(aOnB)
                    && !landformLogical.HasTile(aOnB) && !edgeLogical.HasTile(aOnB),
                    "landform erase returns a pair to pure-base state");
                Assert(component.EraseCell(baseOnly, out reason), reason);
                Assert(!baseLogical.HasTile(baseOnly) && !baseOutput.HasTile(baseOnly),
                    "whole-cell erase clears canonical and generated base state");
                Assert(!component.PaintLandform(new Vector3Int(9, 9, 0),
                    LayeredTerrainMaterial.A, false, out reason)
                    && reason.Contains("base"),
                    "landform-only painting requires an authored base");
                Assert(!component.RefreshIfSourceChanged(out reason)
                    && reason == "unchanged",
                    "incremental refresh skips unchanged canonical state");

                var undoCell = new Vector3Int(6, 2, 0);
                Assert(component.PaintBase(undoCell, LayeredTerrainMaterial.B, out reason), reason);
                Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[]
                {
                    baseLogical, landformLogical, edgeLogical,
                    baseOutput, landformAOutput, landformBOutput,
                    edgeAOnBOutput, edgeBOnAOutput,
                }, "Layered terrain smoke undo");
                Assert(component.PaintPair(undoCell, LayeredTerrainMaterial.A,
                    LayeredTerrainMaterial.B, true, out reason), reason);
                Undo.PerformUndo();
                Assert(baseLogical.GetTile(undoCell) == markerB
                    && !landformLogical.HasTile(undoCell) && !edgeLogical.HasTile(undoCell),
                    "registered brush mutation restores canonical state through Undo");
                Undo.ClearAll();

                var missingDirectionObject = new GameObject("MissingDirectionAuthoring");
                missingDirectionObject.transform.SetParent(root.transform, false);
                var missingDirection = missingDirectionObject.AddComponent<LayeredTerrainTilemap>();
                missingDirection.Configure(baseLogical, landformLogical, edgeLogical,
                    AddTilemap(gridObject.transform, "missing base output"),
                    AddTilemap(gridObject.transform, "missing landform A output"),
                    AddTilemap(gridObject.transform, "missing landform B output"),
                    AddTilemap(gridObject.transform, "missing edge A output"),
                    AddTilemap(gridObject.transform, "missing edge B output"),
                    markerA, markerB, edgeMarker,
                    Require<TileBase>(LayeredTerrainArtSetup.Root + "/Authoring/GrassBase.asset"),
                    Require<TileBase>(LayeredTerrainArtSetup.Root + "/Authoring/SoilBase.asset"),
                    grassSet, soilSet, null, null);
                ConfigurePresentation(missingDirection);
                Assert(!missingDirection.CanPaintPair(LayeredTerrainMaterial.A,
                        LayeredTerrainMaterial.B, true, out reason)
                    && !missingDirection.CanPaintPair(LayeredTerrainMaterial.B,
                        LayeredTerrainMaterial.A, true, out _),
                    "both brushes are disabled only when the pair has no edge resource");

                Debug.Log("FRUIT_DEFENSE_LAYERED_TERRAIN_TILEMAP_OK");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(markerA);
                UnityEngine.Object.DestroyImmediate(markerB);
                UnityEngine.Object.DestroyImmediate(edgeMarker);
            }
        }

        private static Tilemap AddTilemap(Transform parent, string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.AddComponent<Tilemap>();
        }

        private static void ValidateSocketContinuity(string folder)
        {
            const int tileSize = 32;
            var pixels = new Color32[DualGridMaskUtility.MaskCount][];
            var textures = new Texture2D[DualGridMaskUtility.MaskCount];
            try
            {
                for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
                {
                    var path = folder + "/Mask-" + mask.ToString("00") + ".png";
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    Assert(texture.LoadImage(File.ReadAllBytes(path), false)
                        && texture.width == tileSize && texture.height == tileSize,
                        "socket image is a 32x32 PNG: " + path);
                    textures[mask] = texture;
                    pixels[mask] = texture.GetPixels32();
                }

                for (var first = 0; first < DualGridMaskUtility.MaskCount; first++)
                for (var second = 0; second < DualGridMaskUtility.MaskCount; second++)
                {
                    var left = (DualGridMask)first;
                    var right = (DualGridMask)second;
                    if (Same(left, DualGridMask.NorthEast, right, DualGridMask.NorthWest)
                        && Same(left, DualGridMask.SouthEast, right, DualGridMask.SouthWest))
                        for (var y = 0; y < tileSize; y++)
                            Assert(pixels[first][tileSize - 1 + y * tileSize]
                                    .Equals(pixels[second][y * tileSize]),
                                "horizontal RGBA socket mismatch in " + folder
                                + " masks " + first + "/" + second + " at " + y);

                    var bottom = (DualGridMask)first;
                    var top = (DualGridMask)second;
                    if (Same(bottom, DualGridMask.NorthWest, top, DualGridMask.SouthWest)
                        && Same(bottom, DualGridMask.NorthEast, top, DualGridMask.SouthEast))
                        for (var x = 0; x < tileSize; x++)
                            Assert(pixels[first][x + (tileSize - 1) * tileSize]
                                    .Equals(pixels[second][x]),
                                "vertical RGBA socket mismatch in " + folder
                                + " masks " + first + "/" + second + " at " + x);
                }

                if (folder.Contains("Edge"))
                    Assert(Array.TrueForAll(pixels[(int)DualGridMask.Full], pixel => pixel.a == 0),
                        "full occupancy contains no contact ribbon in " + folder);
            }
            finally
            {
                foreach (var texture in textures)
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static bool Same(DualGridMask first, DualGridMask firstCorner,
            DualGridMask second, DualGridMask secondCorner)
        {
            return ((first & firstCorner) != 0) == ((second & secondCorner) != 0);
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert(asset != null, typeof(T).Name + " asset is missing: " + path);
            return asset;
        }

        private static void ConfigurePresentation(LayeredTerrainTilemap component)
        {
            var grass = Require<Tile>(LayeredTerrainArtSetup.GrassBaseTilePath);
            var soil = Require<Tile>(LayeredTerrainArtSetup.SoilBaseTilePath);
            component.ConfigureAuthoringPresentation("草地", grass.sprite,
                new Color(.31f, .76f, .24f, 1f), "泥土", soil.sprite,
                new Color(.61f, .38f, .2f, 1f));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Layered terrain authoring smoke failed: " + message);
        }
    }
}
