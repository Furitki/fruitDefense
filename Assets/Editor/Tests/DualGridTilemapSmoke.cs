using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public static class DualGridTilemapSmoke
    {
        public static void Run()
        {
            Validate();
        }

        public static void Validate()
        {
            ValidateEveryMask();
            ValidateTileSetContract();
            ValidateTileSetGallery();
            ValidateGenerationAndRefresh();
            ValidateGeneratedArtPipeline();
            DualGridPixelTileSetGenerator.ValidateGeneratedPixelTileSet();
            DualGridPixelTerrainWizardSmoke.Validate();
            ValidateReleaseSceneIsolation();
            Debug.Log("FRUIT_DEFENSE_DUAL_GRID_SMOKE_OK");
        }

        private static void ValidateTileSetGallery()
        {
            var expectedPaths = AssetDatabase.FindAssets("t:DualGridTileSet", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct()
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            DualGridTileSetGalleryUtility.Refresh();
            var discovered = DualGridTileSetGalleryUtility.GetTileSets();
            Assert(discovered.Count == expectedPaths.Length,
                "TileSet gallery discovers every project TileSet exactly once");

            for (var index = 0; index < expectedPaths.Length; index++)
            {
                var actualPath = AssetDatabase.GetAssetPath(discovered[index]);
                Assert(actualPath == expectedPaths[index],
                    "TileSet gallery uses deterministic asset-path order at index " + index);
                if (!discovered[index].Validate(out _)) continue;
                for (var quadrant = 0; quadrant < 4; quadrant++)
                    Assert(DualGridTileSetGalleryUtility.GetPreviewTile(
                            discovered[index], quadrant) != null,
                        "valid TileSet has preview source for quadrant " + quadrant
                        + ": " + actualPath);
            }

            var knownPaths = new[]
            {
                "Assets/DualGridDemo/PixelGrass/Generated/PixelGrassDualGridTileSet.asset",
                "Assets/DualGridTerrain/StoneFloor/Generated/StoneFloorDualGridTileSet.asset",
            };
            foreach (var path in knownPaths)
            {
                var known = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(path);
                Assert(known != null && discovered.Contains(known),
                    "TileSet gallery includes known generated set: " + path);
                for (var quadrant = 0; quadrant < 4; quadrant++)
                {
                    var previewTile = DualGridTileSetGalleryUtility.GetPreviewTile(known, quadrant);
                    Assert(DualGridTileSetGalleryUtility.TryGetPreviewSprite(
                            previewTile, out var previewSprite)
                        && previewSprite.texture != null,
                        "generated TileSet preview resolves a real Sprite for quadrant "
                        + quadrant + ": " + path);
                }
            }
        }

        private static void ValidateGeneratedArtPipeline()
        {
            var profile = AssetDatabase.LoadAssetAtPath<DualGridTerrainBakeProfile>(
                DualGridTextureTileSetGenerator.DefaultProfilePath);
            Assert(profile != null, "production bake profile exists");
            Assert(profile.Validate(out var profileReason),
                "production bake profile is valid: " + profileReason);
            Assert(profile.SupersampleScale >= 4,
                "production art uses at least four-times supersampling");
            Assert(profile.AlphaAntialiasPixels <= 2f,
                "outer alpha antialiasing remains a narrow pixel-space band");

            var tileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(
                DualGridTextureTileSetGenerator.TileSetPath);
            Assert(tileSet != null, "generated cartoon terrain TileSet exists");
            Assert(tileSet.Validate(out var tileSetReason),
                "generated cartoon terrain TileSet is valid: " + tileSetReason);
            Assert(AssetDatabase.LoadAssetAtPath<TileBase>(
                    DualGridTextureTileSetGenerator.SoilBaseTilePath) != null,
                "generated soil base Tile is available for layered terrain");

            var maskImporter = AssetImporter.GetAtPath(
                DualGridTextureTileSetGenerator.OutputFolder + "/Mask-15.png") as TextureImporter;
            Assert(maskImporter != null
                && maskImporter.textureType == TextureImporterType.Sprite
                && !maskImporter.mipmapEnabled
                && maskImporter.wrapMode == TextureWrapMode.Clamp
                && maskImporter.filterMode == FilterMode.Bilinear,
                "generated mask import settings preserve alpha without atlas wrapping");

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var sceneText = File.ReadAllText(Path.Combine(projectRoot,
                DualGridDemoSetup.DemoScenePath.Replace('/', Path.DirectorySeparatorChar)));
            Assert(sceneText.Contains("Soil Base - author-owned ground"),
                "demo scene contains the lower soil base layer");
        }

        private static void ValidateEveryMask()
        {
            var resolvedMasks = new HashSet<int>();
            var vertex = new Vector3Int(9, 7, 0);
            for (var numericMask = 0; numericMask < DualGridMaskUtility.MaskCount; numericMask++)
            {
                var expectedMask = (DualGridMask)numericMask;
                var occupied = new HashSet<Vector3Int>();
                AddCornerIfSet(occupied, vertex, expectedMask, DualGridMask.NorthWest);
                AddCornerIfSet(occupied, vertex, expectedMask, DualGridMask.NorthEast);
                AddCornerIfSet(occupied, vertex, expectedMask, DualGridMask.SouthEast);
                AddCornerIfSet(occupied, vertex, expectedMask, DualGridMask.SouthWest);
                var actualMask = DualGridMaskUtility.Resolve(occupied.Contains, vertex);
                Assert(actualMask == expectedMask,
                    "mask " + numericMask + " resolves as " + (int)actualMask);
                resolvedMasks.Add((int)actualMask);
            }
            Assert(resolvedMasks.Count == DualGridMaskUtility.MaskCount,
                "all sixteen corner masks resolve uniquely");
        }

        private static void ValidateTileSetContract()
        {
            var set = ScriptableObject.CreateInstance<DualGridTileSet>();
            var tiles = new List<Tile>();
            var sprites = new List<Sprite>();
            var textures = new List<Texture2D>();
            try
            {
                for (var mask = 1; mask < DualGridMaskUtility.MaskCount; mask++)
                {
                    var tile = CreateRenderableTile("contract-" + mask,
                        8, 8, 8f, new Vector2(.5f, .5f),
                        tiles, sprites, textures);
                    set.SetTile((DualGridMask)mask, tile);
                }
                var hasRenderableMask = set.TryGetSprite(DualGridMask.NorthWest,
                    out var rendered) && rendered != null && rendered.texture != null;
                var valid = set.Validate(out var validReason);
                Assert(set.GetTile(DualGridMask.Empty) == null
                    && hasRenderableMask && valid,
                    "mask 0 may be transparent while 1-15 validate: " + validReason);

                var original = set.GetTile((DualGridMask)7);
                set.SetTile((DualGridMask)7, null);
                Assert(!set.Validate(out var invalidReason) && invalidReason.Contains("7"),
                    "missing required mask is identified");
                set.SetTile((DualGridMask)7, original);

                var wrongNativeSize = CreateRenderableTile("wrong-native-size",
                    16, 8, 8f, new Vector2(.5f, .5f),
                    tiles, sprites, textures);
                set.SetTile((DualGridMask)7, wrongNativeSize);
                Assert(!set.Validate(out var nativeReason)
                    && nativeReason.Contains("7")
                    && nativeReason.Contains("native dimensions"),
                    "native-size mismatch is identified without weakening masks 1-15");

                var wrongNormalizedSize = CreateRenderableTile("wrong-normalized-size",
                    8, 8, 4f, new Vector2(.5f, .5f),
                    tiles, sprites, textures);
                set.SetTile((DualGridMask)7, wrongNormalizedSize);
                Assert(!set.Validate(out var normalizedReason)
                    && normalizedReason.Contains("7")
                    && normalizedReason.Contains("normalized size"),
                    "normalized-size mismatch is identified independently");

                var wrongPivot = CreateRenderableTile("wrong-pivot",
                    8, 8, 8f, Vector2.zero, tiles, sprites, textures);
                set.SetTile((DualGridMask)7, wrongPivot);
                Assert(!set.Validate(out var pivotReason)
                    && pivotReason.Contains("7")
                    && pivotReason.Contains("pivot socket frame"),
                    "normalized-pivot mismatch is identified independently");

                set.SetTile((DualGridMask)7, original);
                Assert(set.Validate(out var restoredReason),
                    "restored real-sprite fixture remains valid: " + restoredReason);
            }
            finally
            {
                foreach (var tile in tiles) UnityEngine.Object.DestroyImmediate(tile);
                foreach (var sprite in sprites) UnityEngine.Object.DestroyImmediate(sprite);
                foreach (var texture in textures) UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(set);
            }
        }

        private static void ValidateGenerationAndRefresh()
        {
            var root = new GameObject("DualGridSmoke", typeof(Grid));
            var logicalObject = new GameObject("Logical", typeof(Tilemap));
            var outputObject = new GameObject("Generated", typeof(Tilemap));
            logicalObject.transform.SetParent(root.transform, false);
            outputObject.transform.SetParent(root.transform, false);
            logicalObject.transform.localPosition = new Vector3(2f, 3f, 0f);

            var logical = logicalObject.GetComponent<Tilemap>();
            var output = outputObject.GetComponent<Tilemap>();
            var component = root.AddComponent<DualGridTilemap>();
            var set = ScriptableObject.CreateInstance<DualGridTileSet>();
            var alternateSet = ScriptableObject.CreateInstance<DualGridTileSet>();
            var logicalTile = ScriptableObject.CreateInstance<Tile>();
            var visualTiles = new List<Tile>();
            var alternateTiles = new List<Tile>();
            var sprites = new List<Sprite>();
            var textures = new List<Texture2D>();
            try
            {
                for (var mask = 1; mask < DualGridMaskUtility.MaskCount; mask++)
                {
                    var tile = CreateRenderableTile("visual-" + mask,
                        8, 8, 8f, new Vector2(.5f, .5f),
                        visualTiles, sprites, textures);
                    set.SetTile((DualGridMask)mask, tile);

                    var alternateTile = CreateRenderableTile("alternate-" + mask,
                        8, 8, 8f, new Vector2(.5f, .5f),
                        alternateTiles, sprites, textures);
                    alternateSet.SetTile((DualGridMask)mask, alternateTile);
                }
                component.Configure(logical, output, set, true);
                Assert(component.AlignGeneratedTilemap() && component.HasExpectedAlignment(),
                    "generated Tilemap aligns by negative half a cell");
                Assert(Vector3.Distance(output.transform.localPosition, new Vector3(1.5f, 2.5f, 0f)) < .0001f,
                    "half-cell alignment preserves logical transform offset");

                logical.SetTile(Vector3Int.zero, logicalTile);
                logical.SetTile(Vector3Int.right, logicalTile);
                output.SetTile(new Vector3Int(12, 12, 0), visualTiles[1]);
                Assert(component.Rebuild(out var rebuildReason), "full rebuild succeeds: " + rebuildReason);
                Assert(!output.HasTile(new Vector3Int(12, 12, 0)), "full rebuild clears stale generated tiles");
                var vertexBounds = new BoundsInt(0, 0, 0, 3, 2, 1);
                foreach (var vertex in vertexBounds.allPositionsWithin)
                {
                    var mask = DualGridMaskUtility.Resolve(logical, vertex);
                    Assert(output.GetTile(vertex) == set.GetTile(mask),
                        "full rebuild writes configured mask " + (int)mask + " at " + vertex);
                }
                Assert(output.cellBounds.position == vertexBounds.position
                    && output.cellBounds.size == vertexBounds.size,
                    "generated bounds add one visual vertex on each source axis");

                var unrelated = output.GetTile(Vector3Int.zero);
                var addedCell = new Vector3Int(5, 5, 0);
                Assert(component.SetLogicalTile(addedCell, logicalTile, out var incrementalReason),
                    "runtime logical mutation succeeds: " + incrementalReason);
                Assert(output.GetTile(Vector3Int.zero) == unrelated,
                    "incremental refresh preserves unrelated generated cells");
                var affected = new Vector3Int[4];
                DualGridMaskUtility.GetAffectedVertices(addedCell, affected);
                foreach (var vertex in affected)
                    Assert(output.GetTile(vertex) == set.GetTile(DualGridMaskUtility.Resolve(logical, vertex)),
                        "incremental refresh updates affected vertex " + vertex);

                var directPaint = new Vector3Int(6, 5, 0);
                logical.SetTile(directPaint, logicalTile);
                Assert(component.RefreshIfSourceChanged(out var automaticReason),
                    "automatic source-signature refresh detects editor-style paint: " + automaticReason);
                Assert(output.HasTile(new Vector3Int(7, 6, 0)),
                    "automatic refresh writes the newly affected outer vertex");

                component.AutomaticRefresh = false;
                var disabledPaint = new Vector3Int(8, 8, 0);
                logical.SetTile(disabledPaint, logicalTile);
                Assert(!component.RefreshIfSourceChanged(out var disabledReason)
                    && disabledReason.Contains("disabled")
                    && !output.HasTile(new Vector3Int(9, 9, 0)),
                    "disabled automatic refresh leaves generated output untouched");
                Assert(component.Rebuild(out var explicitReason)
                    && output.HasTile(new Vector3Int(9, 9, 0)),
                    "explicit rebuild remains available when automatic refresh is disabled: " + explicitReason);

                var manualPaintingBeforeSelection = DualGridTilemapEditor.ManualPaintingEnabled;
                Assert(DualGridTileSetGalleryUtility.AssignAndRebuild(
                        component, alternateSet, false, out var galleryReason),
                    "gallery TileSet assignment succeeds: " + galleryReason);
                Assert(component.TileSet == alternateSet,
                    "gallery assignment updates the component-wide TileSet");
                var sampleVertex = Vector3Int.zero;
                var sampleMask = DualGridMaskUtility.Resolve(logical, sampleVertex);
                Assert(output.GetTile(sampleVertex) == alternateSet.GetTile(sampleMask),
                    "gallery assignment immediately rebuilds generated output");
                Assert(DualGridTilemapEditor.ManualPaintingEnabled == manualPaintingBeforeSelection,
                    "gallery assignment preserves manual paint mode");

                component.Configure(logical, logical, set, true);
                Assert(!component.Rebuild(out var safetyReason)
                    && safetyReason.Contains("different")
                    && logical.HasTile(Vector3Int.zero),
                    "source/output safety rejects destructive self-generation");

                component.Configure(logical, output, set, true);
                logical.ClearAllTiles();
                output.SetTile(Vector3Int.zero, visualTiles[1]);
                Assert(component.Rebuild(out var emptyReason)
                    && output.GetUsedTilesCount() == 0,
                    "empty logical source clears generated output: " + emptyReason);
            }
            finally
            {
                foreach (var tile in visualTiles) UnityEngine.Object.DestroyImmediate(tile);
                foreach (var tile in alternateTiles) UnityEngine.Object.DestroyImmediate(tile);
                foreach (var sprite in sprites) UnityEngine.Object.DestroyImmediate(sprite);
                foreach (var texture in textures) UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(logicalTile);
                UnityEngine.Object.DestroyImmediate(alternateSet);
                UnityEngine.Object.DestroyImmediate(set);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Tile CreateRenderableTile(string name, int width, int height,
            float pixelsPerUnit, Vector2 normalizedPivot, ICollection<Tile> tiles,
            ICollection<Sprite> sprites, ICollection<Texture2D> textures)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "DualGridSmokeTexture-" + name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(Enumerable.Repeat(
                new Color32(255, 255, 255, 255), width * height).ToArray());
            texture.Apply(false, false);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                normalizedPivot, pixelsPerUnit, 0u, SpriteMeshType.FullRect);
            sprite.name = "DualGridSmokeSprite-" + name;
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = "DualGridSmokeTile-" + name;
            tile.sprite = sprite;
            textures.Add(texture);
            sprites.Add(sprite);
            tiles.Add(tile);
            return tile;
        }

        private static void ValidateReleaseSceneIsolation()
        {
            var expected = new[]
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Lobby.unity",
                "Assets/Scenes/Battle.unity",
                "Assets/Scenes/Settlement.unity",
            };
            var configured = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
            Assert(configured.SequenceEqual(expected),
                "DualGridDemo remains outside the four-scene release flow");
        }

        private static void AddCornerIfSet(HashSet<Vector3Int> occupied, Vector3Int vertex,
            DualGridMask completeMask, DualGridMask corner)
        {
            if ((completeMask & corner) != 0)
                occupied.Add(DualGridMaskUtility.LogicalCell(vertex, corner));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Dual-Grid smoke failed: " + message);
        }
    }
}
