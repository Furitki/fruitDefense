using System;
using System.Collections.Generic;
using System.IO;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    /// <summary>
    /// Deterministic square-contour guide and packaging pipeline. It creates topology and
    /// runtime packaging only; painterly transition pixels must come from the retained source.
    /// </summary>
    public static class SquareTerrainArtGenerator
    {
        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        private static readonly Color32 GuideGreen = new Color32(78, 166, 62, 255);
        private static readonly Color32 ChromaKey = new Color32(255, 0, 255, 255);
        private const int RibbonSourceWidth = 2172;
        private const int RibbonSourceHeight = 724;
        private const int RibbonPhaseOffset = 173;
        private const int RibbonTangentSpan = 768;

        private sealed class BoundaryField
        {
            public int[] Distance;
            public int[] Phase;
        }

        [MenuItem("Fruit Defense/Terrain Contours/Generate Square Topology Guide")]
        public static void GenerateTopologyGuide()
        {
            SquareTerrainArtProfile.ValidateContract();
            EnsureAssetFolder(SquareTerrainArtProfile.TopologyFolder);
            var masks = BuildAllTopologyMasks();
            WriteAtlas(SquareTerrainArtProfile.TopologyGuidePath, masks, true);
            WriteImagegenReference(masks);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureGuideImporter(SquareTerrainArtProfile.TopologyGuidePath);
            ConfigureGuideImporter(SquareTerrainArtProfile.ImagegenReferencePath);
            Debug.Log("Square contour topology guide generated: "
                + SquareTerrainArtProfile.TopologyGuidePath);
        }

        [MenuItem("Fruit Defense/Terrain Contours/Generate Square Landforms")]
        public static void GenerateDeterministicLandforms()
        {
            SquareTerrainArtProfile.ValidateContract();
            var masks = BuildAllTopologyMasks();
            var grassSource = LoadPng(SquareTerrainArtProfile.GrassBaseSourcePath);
            try
            {
                BuildTexturedLandform(grassSource, masks,
                    SquareTerrainArtProfile.GrassLandformFolder,
                    SquareTerrainArtProfile.GrassLandformTileSetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(grassSource);
            }

            var soilSource = LoadPng(SquareTerrainArtProfile.SoilBaseSourcePath);
            try
            {
                BuildTexturedLandform(soilSource, masks,
                    SquareTerrainArtProfile.SoilLandformFolder,
                    SquareTerrainArtProfile.SoilLandformTileSetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(soilSource);
            }

            var stoneSource = LoadPng(SquareTerrainArtProfile.StoneRoadSourcePath);
            try
            {
                BuildTexturedLandform(stoneSource, masks,
                    SquareTerrainArtProfile.StoneRoadLandformFolder,
                    SquareTerrainArtProfile.StoneRoadLandformTileSetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stoneSource);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Native 256 px square grass, soil and stone-road landforms generated.");
        }

        [MenuItem("Fruit Defense/Terrain Contours/Package Accepted Continuous Grass On Soil Ribbon")]
        public static void PackageAcceptedGrassOnSoilCandidate()
        {
            SquareTerrainArtProfile.ValidateContract();
            RequireFile(SquareTerrainArtProfile.ContinuousRibbonPath,
                "Imagegen continuous grass-on-soil ribbon");
            RequireFile(SquareTerrainArtProfile.ContinuousRibbonProvenancePath,
                "Imagegen continuous grass-on-soil ribbon provenance");
            SquareTerrainArtValidator.ValidateProvenanceContract();

            var ribbon = LoadPng(SquareTerrainArtProfile.ContinuousRibbonPath);
            var grass = LoadPng(SquareTerrainArtProfile.GrassBaseSourcePath);
            try
            {
                if (ribbon.width != RibbonSourceWidth || ribbon.height != RibbonSourceHeight)
                    throw new InvalidOperationException(
                        "Continuous grass-on-soil ribbon must be exactly 2172x724 RGB.");

                var topology = BuildAllTopologyMasks();
                var packaged = new Color32[SquareTerrainArtProfile.MaskCount][];
                for (var mask = 0; mask < SquareTerrainArtProfile.MaskCount; mask++)
                    packaged[mask] = RemapTopDownGrassFeather(
                        ribbon, grass, topology[mask], mask);
                LockCompatibleSockets(packaged);
                SquareTerrainArtValidator.ValidateEdgeRgbComesFromGrassBase(
                    packaged, grass.GetPixels32());
                SquareTerrainArtValidator.ValidatePackagedEdgePixels(packaged, topology);
                BuildTileSet(packaged, SquareTerrainArtProfile.GrassOnSoilEdgeFolder,
                    SquareTerrainArtProfile.GrassOnSoilEdgeTileSetPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(grass);
                UnityEngine.Object.DestroyImmediate(ribbon);
            }
            Debug.Log("Accepted ribbon lip profile packaged as a top-down grass feather with "
                + "base-grass RGB, guide-owned topology and no soil wall or outline.");
        }

        [MenuItem("Fruit Defense/Terrain Contours/Generate Available Square Assets")]
        public static void GenerateAvailableSquareAssets()
        {
            GenerateTopologyGuide();
            GenerateDeterministicLandforms();
            if (File.Exists(AbsolutePath(SquareTerrainArtProfile.ContinuousRibbonPath))
                && File.Exists(AbsolutePath(
                    SquareTerrainArtProfile.ContinuousRibbonProvenancePath)))
                PackageAcceptedGrassOnSoilCandidate();
            else
                Debug.LogWarning("Square landforms and topology are ready. Painted edge packaging "
                    + "is waiting for the retained continuous imagegen ribbon and provenance at "
                    + SquareTerrainArtProfile.ContinuousRibbonPath + ".");
        }

        internal static Color32[][] BuildAllTopologyMasks()
        {
            var masks = new Color32[SquareTerrainArtProfile.MaskCount][];
            for (var mask = 0; mask < masks.Length; mask++)
                masks[mask] = BuildTopologyMask(mask);
            return masks;
        }

        internal static Color32[] BuildTopologyMask(int mask)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                pixels[x + y * size] = TopologyContains(mask, x, y) ? GuideGreen : Transparent;
            return pixels;
        }

        internal static bool TopologyContains(int mask, int x, int y)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var half = size / 2;
            var radius = SquareTerrainArtProfile.CornerRadius;
            int bit;
            int adjacent;
            int centerX;
            int centerY;
            bool outsideRoundBox;

            if (x < half && y >= half)
            {
                bit = 1; // NW
                adjacent = 2 | 8;
                centerX = half - radius;
                centerY = half + radius - 1;
                outsideRoundBox = x <= half - radius || y >= half + radius - 1;
            }
            else if (x >= half && y >= half)
            {
                bit = 2; // NE
                adjacent = 1 | 4;
                centerX = half + radius - 1;
                centerY = half + radius - 1;
                outsideRoundBox = x >= half + radius - 1 || y >= half + radius - 1;
            }
            else if (x >= half)
            {
                bit = 4; // SE
                adjacent = 2 | 8;
                centerX = half + radius - 1;
                centerY = half - radius;
                outsideRoundBox = x >= half + radius - 1 || y <= half - radius;
            }
            else
            {
                bit = 8; // SW
                adjacent = 1 | 4;
                centerX = half - radius;
                centerY = half - radius;
                outsideRoundBox = x <= half - radius || y <= half - radius;
            }

            if ((mask & bit) == 0) return false;
            if ((mask & adjacent) != 0 || outsideRoundBox) return true;
            var dx = x - centerX;
            var dy = y - centerY;
            return dx * dx + dy * dy <= radius * radius;
        }

        internal static Color32[] ReadMaskTexture(string folder, int mask)
        {
            var texture = LoadPng(SquareTerrainArtProfile.MaskTexturePath(folder, mask));
            try
            {
                if (texture.width != SquareTerrainArtProfile.TileSize
                    || texture.height != SquareTerrainArtProfile.TileSize)
                    throw new InvalidOperationException("Square mask has wrong native dimensions: "
                        + SquareTerrainArtProfile.MaskTexturePath(folder, mask));
                return texture.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        internal static void WritePng(string path, int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                var absolute = AbsolutePath(path);
                var directory = Path.GetDirectoryName(absolute);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllBytes(absolute, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void BuildTexturedLandform(Texture2D source, Color32[][] topology,
            string folder, string tileSetPath)
        {
            var sourcePixels = source.GetPixels32();
            var generated = new Color32[SquareTerrainArtProfile.MaskCount][];
            var size = SquareTerrainArtProfile.TileSize;
            for (var mask = 0; mask < generated.Length; mask++)
            {
                generated[mask] = new Color32[size * size];
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    if (topology[mask][x + y * size].a == 0) continue;
                    var color = sourcePixels[(x % source.width) + (y % source.height) * source.width];
                    color.a = 255;
                    generated[mask][x + y * size] = color;
                }
            }
            LockLandformSockets(generated, topology, sourcePixels, source.width, source.height);
            BuildTileSet(generated, folder, tileSetPath);
        }

        private static void BuildRibbonGrassLandform(Texture2D source, Color32[][] topology,
            string folder, string tileSetPath)
        {
            if (source.width != RibbonSourceWidth || source.height != RibbonSourceHeight)
                throw new InvalidOperationException(
                    "Continuous ribbon grass surface must remain 2172x724.");
            var sourcePixels = source.GetPixels32();
            var generated = new Color32[SquareTerrainArtProfile.MaskCount][];
            var size = SquareTerrainArtProfile.TileSize;
            var surface = new Color32[size * size];
            var mainGrassRow = source.height - 1 - 160;
            var mainGrass = sourcePixels[source.width / 2 + mainGrassRow * source.width];
            for (var index = 0; index < surface.Length; index++) surface[index] = mainGrass;
            for (var mask = 0; mask < generated.Length; mask++)
            {
                generated[mask] = new Color32[size * size];
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var index = x + y * size;
                    if (topology[mask][index].a == 0) continue;
                    generated[mask][index] = surface[index];
                }
            }
            LockLandformSockets(generated, topology, surface, size, size);
            BuildTileSet(generated, folder, tileSetPath);
        }

        private static void LockLandformSockets(Color32[][] masks, Color32[][] topology,
            Color32[] source, int sourceWidth, int sourceHeight)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var socket = SquareTerrainArtProfile.ProtectedSocketPixels;
            for (var mask = 0; mask < masks.Length; mask++)
            {
                for (var y = 0; y < size; y++)
                for (var depth = 0; depth < socket; depth++)
                {
                    SetLandformSocketPixel(masks[mask], topology[mask], depth, y,
                        source[depth % sourceWidth + (y % sourceHeight) * sourceWidth]);
                    SetLandformSocketPixel(masks[mask], topology[mask], size - 1 - depth, y,
                        source[depth % sourceWidth + (y % sourceHeight) * sourceWidth]);
                }
                for (var x = 0; x < size; x++)
                for (var depth = 0; depth < socket; depth++)
                {
                    SetLandformSocketPixel(masks[mask], topology[mask], x, depth,
                        source[(x % sourceWidth) + (depth % sourceHeight) * sourceWidth]);
                    SetLandformSocketPixel(masks[mask], topology[mask], x, size - 1 - depth,
                        source[(x % sourceWidth) + (depth % sourceHeight) * sourceWidth]);
                }
                for (var depthY = 0; depthY < socket; depthY++)
                for (var depthX = 0; depthX < socket; depthX++)
                {
                    var color = source[(depthX % sourceWidth)
                        + (depthY % sourceHeight) * sourceWidth];
                    SetLandformSocketPixel(masks[mask], topology[mask], depthX, depthY, color);
                    SetLandformSocketPixel(masks[mask], topology[mask],
                        size - 1 - depthX, depthY, color);
                    SetLandformSocketPixel(masks[mask], topology[mask], depthX,
                        size - 1 - depthY, color);
                    SetLandformSocketPixel(masks[mask], topology[mask],
                        size - 1 - depthX, size - 1 - depthY, color);
                }
            }
        }

        private static void SetLandformSocketPixel(Color32[] pixels, Color32[] topology,
            int x, int y, Color32 color)
        {
            var index = x + y * SquareTerrainArtProfile.TileSize;
            if (topology[index].a == 0)
            {
                pixels[index] = Transparent;
                return;
            }
            color.a = 255;
            pixels[index] = color;
        }

        private static void BuildTileSet(Color32[][] masks, string folder, string tileSetPath)
        {
            EnsureAssetFolder(folder);
            for (var mask = 0; mask < masks.Length; mask++)
                WritePng(SquareTerrainArtProfile.MaskTexturePath(folder, mask),
                    SquareTerrainArtProfile.TileSize, SquareTerrainArtProfile.TileSize, masks[mask]);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var tileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(tileSetPath);
            if (tileSet == null)
            {
                tileSet = ScriptableObject.CreateInstance<DualGridTileSet>();
                AssetDatabase.CreateAsset(tileSet, tileSetPath);
            }
            for (var mask = 0; mask < masks.Length; mask++)
            {
                var texturePath = SquareTerrainArtProfile.MaskTexturePath(folder, mask);
                ConfigureRuntimeSpriteImporter(texturePath);
                var tile = LoadOrCreateTile(SquareTerrainArtProfile.MaskTilePath(folder, mask));
                tile.sprite = RequireAsset<Sprite>(texturePath);
                tile.color = Color.white;
                tile.transform = Matrix4x4.identity;
                tile.flags = TileFlags.LockAll;
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                tileSet.SetTile((DualGridMask)mask, tile);
            }
            EditorUtility.SetDirty(tileSet);
        }

        private static Color32[] RemapTopDownGrassFeather(Texture2D ribbon,
            Texture2D grass, Color32[] topology, int mask)
        {
            var result = new Color32[topology.Length];
            if (mask == 0 || mask == 15) return result;
            var field = BuildBoundaryField(topology);
            var source = ribbon.GetPixels32();
            var grassPixels = grass.GetPixels32();
            int medianLip;
            var grassLipRows = BuildGrassLipRows(source, ribbon.width, ribbon.height,
                out medianLip);
            var grassDripDepths = BuildGrassDripDepths(grassLipRows, medianLip);
            var size = SquareTerrainArtProfile.TileSize;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var index = x + y * size;
                var distance = field.Distance[index];
                var tangent = PositiveModulo(field.Phase[index], size);
                var insideTopology = topology[index].a != 0;
                var normal = insideTopology ? -distance - 1 : distance;
                var layer = normal;
                if (layer < -SquareTerrainArtProfile.GrassBlendInsidePixels) continue;
                var color = grassPixels[(x % grass.width) + (y % grass.height) * grass.width];
                if (layer < 0)
                {
                    color.a = 255;
                }
                else
                {
                    var extent = SquareTerrainArtProfile.GrassFeatherBasePixels
                        + grassDripDepths[tangent];
                    if (layer >= extent) continue;
                    color.a = InterpolateAlpha(
                        SquareTerrainArtProfile.GrassFeatherAlphaNear,
                        SquareTerrainArtProfile.GrassFeatherAlphaFar, layer, extent);
                }
                result[index] = color;
            }
            RemoveDetachedPaint(result, topology);
            return result;
        }

        private static int[] BuildGrassLipRows(Color32[] source, int width, int height,
            out int median)
        {
            var rows = new int[width];
            for (var x = 0; x < width; x++)
            {
                var lip = 205;
                for (var rowFromTop = 180; rowFromTop <= 240; rowFromTop++)
                {
                    var y = height - 1 - rowFromTop;
                    var color = source[x + y * width];
                    if (color.g >= color.r + 15 && color.g > 140 && color.b < 130)
                        lip = rowFromTop;
                }
                rows[x] = lip;
            }
            var sorted = (int[])rows.Clone();
            Array.Sort(sorted);
            median = sorted[sorted.Length / 2];
            return rows;
        }

        private static int TileablePingPong(int coordinate, int sourceSpan)
        {
            var last = SquareTerrainArtProfile.TileSize - 1;
            var doubled = coordinate * sourceSpan * 2 / last;
            return doubled <= sourceSpan ? doubled : sourceSpan * 2 - doubled;
        }

        private static int[] BuildGrassDripDepths(int[] sourceLipRows, int medianLip)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var raw = new int[size];
            for (var tangent = 0; tangent < size; tangent++)
            {
                var sourceX = RibbonPhaseOffset
                    + TileablePingPong(tangent, RibbonTangentSpan);
                raw[tangent] = Mathf.Clamp((sourceLipRows[sourceX] - medianLip) / 2,
                    0, SquareTerrainArtProfile.GrassFeatherVariationPixels);
            }

            var requiredPeak = Math.Min(4,
                SquareTerrainArtProfile.GrassFeatherVariationPixels);
            if (Maximum(raw, 50, 76) < requiredPeak
                || Maximum(raw, 176, 207) < requiredPeak)
                throw new InvalidOperationException(
                    "Continuous ribbon no longer contains the two retained grass-drip peaks.");
            var result = new int[size];
            for (var tangent = 0; tangent < size; tangent++)
            {
                var total = 0;
                var samples = 0;
                for (var offset = -2; offset <= 2; offset++)
                {
                    var sample = Mathf.Clamp(tangent + offset, 0, size - 1);
                    total += raw[sample];
                    samples++;
                }
                var depth = Mathf.RoundToInt(total / (float)samples);
                var socketDistance = Math.Min(tangent, size - 1 - tangent);
                if (socketDistance < SquareTerrainArtProfile.GrassDripSocketTaperPixels)
                    depth = depth * socketDistance
                        / SquareTerrainArtProfile.GrassDripSocketTaperPixels;
                result[tangent] = depth;
            }
            return result;
        }

        private static int Maximum(int[] values, int startInclusive, int endExclusive)
        {
            var result = int.MinValue;
            for (var index = startInclusive; index < endExclusive; index++)
                result = Math.Max(result, values[index]);
            return result;
        }

        private static byte InterpolateAlpha(int near, int far, int layer, int layerCount)
        {
            if (layerCount <= 1) return (byte)far;
            return (byte)Mathf.RoundToInt(Mathf.Lerp(near, far,
                layer / (float)(layerCount - 1)));
        }

        private static bool IsGrassPaint(Color32 color)
        {
            return color.g >= color.r + 15 && color.g > 140 && color.b < 130;
        }

        private static BoundaryField BuildBoundaryField(Color32[] topology)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var field = new BoundaryField
            {
                Distance = new int[topology.Length],
                Phase = new int[topology.Length],
            };
            var queue = new Queue<int>(topology.Length);
            for (var index = 0; index < field.Distance.Length; index++)
                field.Distance[index] = int.MaxValue;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var index = x + y * size;
                if (topology[index].a == 0) continue;
                SeedBoundary(topology, field, queue, x, y, 0, 1, x);
                SeedBoundary(topology, field, queue, x, y, 1, 0,
                    size + size - 1 - y);
                SeedBoundary(topology, field, queue, x, y, 0, -1,
                    size * 2 + size - 1 - x);
                SeedBoundary(topology, field, queue, x, y, -1, 0,
                    size * 3 + y);
            }
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % size;
                var y = index / size;
                PropagateBoundary(index - 1, x > 0, index, field, queue);
                PropagateBoundary(index + 1, x + 1 < size, index, field, queue);
                PropagateBoundary(index - size, y > 0, index, field, queue);
                PropagateBoundary(index + size, y + 1 < size, index, field, queue);
            }
            return field;
        }

        private static void SeedBoundary(Color32[] topology, BoundaryField field,
            Queue<int> queue, int x, int y, int dx, int dy, int phase)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var outsideX = x + dx;
            var outsideY = y + dy;
            if (outsideX < 0 || outsideX >= size || outsideY < 0 || outsideY >= size)
                return;
            var outsideIndex = outsideX + outsideY * size;
            if (topology[outsideIndex].a != 0) return;
            var insideIndex = x + y * size;
            SetBoundarySeed(field, queue, insideIndex, phase);
            SetBoundarySeed(field, queue, outsideIndex, phase);
        }

        private static void SetBoundarySeed(BoundaryField field, Queue<int> queue,
            int index, int phase)
        {
            if (field.Distance[index] != int.MaxValue) return;
            field.Distance[index] = 0;
            field.Phase[index] = phase;
            queue.Enqueue(index);
        }

        private static void PropagateBoundary(int index, bool valid, int from,
            BoundaryField field, Queue<int> queue)
        {
            if (!valid || field.Distance[from] + 1 >= field.Distance[index]) return;
            field.Distance[index] = field.Distance[from] + 1;
            field.Phase[index] = field.Phase[from];
            queue.Enqueue(index);
        }

        private static int PositiveModulo(int value, int divisor)
        {
            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private static void RemoveDetachedPaint(Color32[] paint, Color32[] topology)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var reachable = new bool[paint.Length];
            var queue = new Queue<int>();
            for (var index = 0; index < topology.Length; index++)
            {
                if (topology[index].a == 0) continue;
                reachable[index] = true;
                queue.Enqueue(index);
            }
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % size;
                var y = index / size;
                VisitPaint(index - 1, x > 0, paint, topology, reachable, queue);
                VisitPaint(index + 1, x + 1 < size, paint, topology, reachable, queue);
                VisitPaint(index - size, y > 0, paint, topology, reachable, queue);
                VisitPaint(index + size, y + 1 < size, paint, topology, reachable, queue);
            }
            for (var index = 0; index < paint.Length; index++)
                if (!reachable[index]) paint[index] = Transparent;
        }

        private static void VisitPaint(int index, bool valid, Color32[] paint, Color32[] topology,
            bool[] reachable, Queue<int> queue)
        {
            if (!valid || reachable[index]
                || (paint[index].a == 0 && topology[index].a == 0)) return;
            reachable[index] = true;
            queue.Enqueue(index);
        }

        private static int[] BoundaryDistances(Color32[] topology)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var distances = new int[topology.Length];
            var queue = new Queue<int>(topology.Length);
            for (var i = 0; i < distances.Length; i++) distances[i] = int.MaxValue;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var index = x + y * size;
                var filled = topology[index].a != 0;
                if ((x > 0 && (topology[index - 1].a != 0) != filled)
                    || (x + 1 < size && (topology[index + 1].a != 0) != filled)
                    || (y > 0 && (topology[index - size].a != 0) != filled)
                    || (y + 1 < size && (topology[index + size].a != 0) != filled))
                {
                    distances[index] = 0;
                    queue.Enqueue(index);
                }
            }
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % size;
                var y = index / size;
                VisitDistance(index - 1, x > 0, distances, queue, distances[index] + 1);
                VisitDistance(index + 1, x + 1 < size, distances, queue, distances[index] + 1);
                VisitDistance(index - size, y > 0, distances, queue, distances[index] + 1);
                VisitDistance(index + size, y + 1 < size, distances, queue, distances[index] + 1);
            }
            return distances;
        }

        private static void VisitDistance(int index, bool valid, int[] distances,
            Queue<int> queue, int proposed)
        {
            if (!valid || proposed >= distances[index]) return;
            distances[index] = proposed;
            queue.Enqueue(index);
        }

        private static void KeyChroma(Color32[] pixels)
        {
            var toleranceSquared = SquareTerrainArtProfile.ChromaTolerance
                * SquareTerrainArtProfile.ChromaTolerance;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                var dr = pixel.r - ChromaKey.r;
                var dg = pixel.g - ChromaKey.g;
                var db = pixel.b - ChromaKey.b;
                if (pixel.a < 8 || dr * dr + dg * dg + db * db <= toleranceSquared)
                    pixels[index] = Transparent;
            }
        }

        private static void LockCompatibleSockets(Color32[][] masks)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var socket = SquareTerrainArtProfile.ProtectedSocketPixels;
            var vertical = new Color32[4][];
            var horizontal = new Color32[4][];
            for (var signature = 0; signature < 4; signature++)
            {
                var verticalMask = ((signature & 1) != 0 ? 1 : 0)
                    | ((signature & 2) != 0 ? 8 : 0);
                var horizontalMask = ((signature & 1) != 0 ? 8 : 0)
                    | ((signature & 2) != 0 ? 4 : 0);
                vertical[signature] = new Color32[size * socket];
                horizontal[signature] = new Color32[size * socket];
                for (var y = 0; y < size; y++)
                for (var depth = 0; depth < socket; depth++)
                    vertical[signature][depth + y * socket] =
                        masks[verticalMask][depth + y * size];
                for (var x = 0; x < size; x++)
                for (var depth = 0; depth < socket; depth++)
                    horizontal[signature][x + depth * size] =
                        masks[horizontalMask][x + depth * size];
            }

            for (var mask = 0; mask < masks.Length; mask++)
            {
                var leftSignature = ((mask & 1) != 0 ? 1 : 0) | ((mask & 8) != 0 ? 2 : 0);
                var rightSignature = ((mask & 2) != 0 ? 1 : 0) | ((mask & 4) != 0 ? 2 : 0);
                var bottomSignature = ((mask & 8) != 0 ? 1 : 0) | ((mask & 4) != 0 ? 2 : 0);
                var topSignature = ((mask & 1) != 0 ? 1 : 0) | ((mask & 2) != 0 ? 2 : 0);
                for (var y = 0; y < size; y++)
                for (var depth = 0; depth < socket; depth++)
                {
                    masks[mask][depth + y * size] = vertical[leftSignature][depth + y * socket];
                    masks[mask][size - 1 - depth + y * size] =
                        vertical[rightSignature][depth + y * socket];
                }
                for (var x = 0; x < size; x++)
                for (var depth = 0; depth < socket; depth++)
                {
                    masks[mask][x + depth * size] = horizontal[bottomSignature][x + depth * size];
                    masks[mask][x + (size - 1 - depth) * size] =
                        horizontal[topSignature][x + depth * size];
                }
                for (var depthY = 0; depthY < socket; depthY++)
                for (var depthX = 0; depthX < socket; depthX++)
                {
                    masks[mask][depthX + depthY * size] = Transparent;
                    masks[mask][size - 1 - depthX + depthY * size] = Transparent;
                    masks[mask][depthX + (size - 1 - depthY) * size] = Transparent;
                    masks[mask][size - 1 - depthX + (size - 1 - depthY) * size] = Transparent;
                }
            }
        }

        private static Color32[] CropAtlasTile(Texture2D atlas, int mask)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var rowFromTop = mask / SquareTerrainArtProfile.AtlasColumns;
            var column = mask % SquareTerrainArtProfile.AtlasColumns;
            var atlasPixels = atlas.GetPixels32();
            var tile = new Color32[size * size];
            var baseX = column * size;
            var baseY = atlas.height - (rowFromTop + 1) * size;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tile[x + y * size] = atlasPixels[baseX + x + (baseY + y) * atlas.width];
            return tile;
        }

        private static void WriteAtlas(string path, Color32[][] masks, bool guideColors)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var atlas = new Color32[SquareTerrainArtProfile.AtlasSize
                * SquareTerrainArtProfile.AtlasSize];
            for (var mask = 0; mask < masks.Length; mask++)
            {
                var column = mask % SquareTerrainArtProfile.AtlasColumns;
                var rowFromTop = mask / SquareTerrainArtProfile.AtlasColumns;
                var baseY = (SquareTerrainArtProfile.AtlasRows - 1 - rowFromTop) * size;
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var source = masks[mask][x + y * size];
                    atlas[column * size + x + (baseY + y) * SquareTerrainArtProfile.AtlasSize] =
                        guideColors && source.a != 0 ? GuideGreen : source;
                }
            }
            if (PngPixelsMatch(path, SquareTerrainArtProfile.AtlasSize,
                    SquareTerrainArtProfile.AtlasSize, atlas)) return;
            WritePng(path, SquareTerrainArtProfile.AtlasSize,
                SquareTerrainArtProfile.AtlasSize, atlas);
        }

        private static bool PngPixelsMatch(string path, int width, int height, Color32[] expected)
        {
            if (!File.Exists(AbsolutePath(path))) return false;
            var texture = LoadPng(path);
            try
            {
                if (texture.width != width || texture.height != height) return false;
                var actual = texture.GetPixels32();
                if (actual.Length != expected.Length) return false;
                for (var index = 0; index < actual.Length; index++)
                    if (actual[index].r != expected[index].r
                        || actual[index].g != expected[index].g
                        || actual[index].b != expected[index].b
                        || actual[index].a != expected[index].a) return false;
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void WriteImagegenReference(Color32[][] masks)
        {
            const int gutter = 24;
            var size = SquareTerrainArtProfile.TileSize;
            var width = SquareTerrainArtProfile.AtlasColumns * size
                + (SquareTerrainArtProfile.AtlasColumns + 1) * gutter;
            var pixels = new Color32[width * width];
            for (var index = 0; index < pixels.Length; index++)
                pixels[index] = new Color32(31, 34, 42, 255);
            for (var mask = 0; mask < masks.Length; mask++)
            {
                var column = mask % 4;
                var rowFromTop = mask / 4;
                var baseX = gutter + column * (size + gutter);
                var baseY = width - gutter - size - rowFromTop * (size + gutter);
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var source = masks[mask][x + y * size];
                    pixels[baseX + x + (baseY + y) * width] = source.a == 0
                        ? ChromaKey : GuideGreen;
                }
                DrawFrame(pixels, width, baseX, baseY, size);
                DrawMaskNumber(pixels, width, baseX + 8, baseY + size - 25, mask);
            }
            WritePng(SquareTerrainArtProfile.ImagegenReferencePath, width, width, pixels);
        }

        private static void DrawFrame(Color32[] pixels, int width, int x0, int y0, int size)
        {
            var white = new Color32(235, 235, 235, 255);
            for (var offset = 0; offset < size; offset++)
            {
                pixels[x0 + offset + y0 * width] = white;
                pixels[x0 + offset + (y0 + size - 1) * width] = white;
                pixels[x0 + (y0 + offset) * width] = white;
                pixels[x0 + size - 1 + (y0 + offset) * width] = white;
            }
        }

        private static void DrawMaskNumber(Color32[] pixels, int width, int x0, int y0, int mask)
        {
            for (var y = 0; y < 19; y++)
            for (var x = 0; x < 49; x++)
                pixels[x0 + x + (y0 + y) * width] = new Color32(20, 20, 20, 255);
            DrawDigit(pixels, width, x0 + 7, y0 + 4, mask / 10);
            DrawDigit(pixels, width, x0 + 23, y0 + 4, mask % 10);
        }

        private static void DrawDigit(Color32[] pixels, int width, int x0, int y0, int digit)
        {
            var rows = DigitRows(digit);
            var white = new Color32(255, 255, 255, 255);
            for (var row = 0; row < 5; row++)
            for (var column = 0; column < 3; column++)
            {
                if ((rows[row] & (1 << (2 - column))) == 0) continue;
                for (var py = 0; py < 2; py++)
                for (var px = 0; px < 2; px++)
                    pixels[x0 + column * 3 + px + (y0 + (4 - row) * 2 + py) * width] = white;
            }
        }

        private static int[] DigitRows(int digit)
        {
            switch (digit)
            {
                case 0: return new[] { 7, 5, 5, 5, 7 };
                case 1: return new[] { 2, 6, 2, 2, 7 };
                case 2: return new[] { 7, 1, 7, 4, 7 };
                case 3: return new[] { 7, 1, 7, 1, 7 };
                case 4: return new[] { 5, 5, 7, 1, 1 };
                case 5: return new[] { 7, 4, 7, 1, 7 };
                case 6: return new[] { 7, 4, 7, 5, 7 };
                case 7: return new[] { 7, 1, 1, 1, 1 };
                case 8: return new[] { 7, 5, 7, 5, 7 };
                default: return new[] { 7, 5, 7, 1, 7 };
            }
        }

        private static Texture2D LoadPng(string assetPath)
        {
            RequireFile(assetPath, "PNG source");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (texture.LoadImage(File.ReadAllBytes(AbsolutePath(assetPath)), false)) return texture;
            UnityEngine.Object.DestroyImmediate(texture);
            throw new InvalidOperationException("PNG could not be decoded: " + assetPath);
        }

        private static Tile LoadOrCreateTile(string path)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static void ConfigureGuideImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureRuntimeSpriteImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Square sprite importer is unavailable: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SquareTerrainArtProfile.TileSize;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = SquareTerrainArtProfile.TileSize;
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
                AssetDatabase.ImportAsset(path,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            throw new InvalidOperationException(typeof(T).Name + " is unavailable: " + path);
        }

        private static void RequireFile(string path, string label)
        {
            if (!File.Exists(AbsolutePath(path)))
                throw new FileNotFoundException(label + " is missing.", path);
        }

        private static void EnsureAssetFolder(string path)
        {
            var normalized = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalized)) return;
            var parts = normalized.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        internal static string AbsolutePath(string projectPath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                projectPath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
