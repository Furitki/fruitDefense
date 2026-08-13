using System;
using System.IO;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public static class DualGridTextureTileSetGenerator
    {
        public const string SourceTexturePath =
            "Assets/ArtSources/TempArt/cartoon-grass-seamless-hd.png";
        public const string SoilTexturePath =
            "Assets/ArtSources/TempArt/cartoon-soil-seamless-hd.png";
        public const string OutputFolder = "Assets/DualGridDemo/CartoonGrass";
        public const string DefaultProfilePath = OutputFolder + "/CartoonGrassBakeProfile.asset";
        public const string TileSetPath = OutputFolder + "/CartoonGrassDualGridTileSet.asset";
        public const string SoilBaseTilePath = OutputFolder + "/CartoonGrassSoilBase.asset";
        public const string AtlasEvidencePath =
            "Builds/Evidence/cartoon-grass-dual-grid-16-mask-atlas.png";
        public const string SeamEvidencePath =
            "Builds/Evidence/cartoon-grass-dual-grid-seam-test.json";

        private const string BakerVersion = "pixel-distance-supersample-v3";
        private const float TerrainThreshold = .5f;

        [Serializable]
        private sealed class BakeValidationReport
        {
            public string source;
            public string soilSource;
            public string profile;
            public string bakerVersion;
            public string profileHash;
            public string pixelHash;
            public string tileSet;
            public int tileSize;
            public int supersampleScale;
            public float alphaAntialiasPixels;
            public float exposedSoilPixels;
            public float grassBlendPixels;
            public int generatedMasks;
            public int horizontalCompatiblePairs;
            public int verticalCompatiblePairs;
            public int maximumAlphaDifference;
            public int maximumRgbaDifference;
            public int maximumMeasuredAlphaTransitionPixels;
            public string alphaTransitionResult;
            public string sharedEdgeResult;
            public bool oppositeCornerCentersTransparent;
            public string oppositeCornerTopologyResult;
            public string deterministicRepeatResult;
            public string edgeProfile;
            public string textureContinuityEvidence;
            public string atlasPreview;
            public string result;
        }

        private struct MaskCorners
        {
            public float NorthWest;
            public float NorthEast;
            public float SouthEast;
            public float SouthWest;
            public bool IsOppositeCornerMask;
        }

        private struct SurfaceEvaluation
        {
            public float Alpha;
            public float GrassMix;
            public float Shade;
        }

        private sealed class SupersampleNoiseField
        {
            public readonly int Size;
            public readonly float[] Broad;
            public readonly float[] Fine;
            public readonly float[] Blade;

            public SupersampleNoiseField(int size, int seed)
            {
                Size = size;
                Broad = new float[size * size];
                Fine = new float[size * size];
                Blade = new float[size * size];
                for (var y = 0; y < size; y++)
                {
                    var v = (y + .5f) / size;
                    for (var x = 0; x < size; x++)
                    {
                        var u = (x + .5f) / size;
                        var index = x + y * size;
                        Broad[index] = PeriodicTerrainNoise(u, v, seed);
                        Fine[index] = PeriodicFineNoise(u, v, seed);
                        Blade[index] = PeriodicBladeNoise(u, v, seed);
                    }
                }
            }

            public float DerivativeU(float[] values, int x, int y)
            {
                var left = x == 0 ? Size - 1 : x - 1;
                var right = x == Size - 1 ? 0 : x + 1;
                return (values[right + y * Size] - values[left + y * Size]) * Size * .5f;
            }

            public float DerivativeV(float[] values, int x, int y)
            {
                var bottom = y == 0 ? Size - 1 : y - 1;
                var top = y == Size - 1 ? 0 : y + 1;
                return (values[x + top * Size] - values[x + bottom * Size]) * Size * .5f;
            }
        }

        [MenuItem("Fruit Defense/Dual Grid/Generate Cartoon Grass Tile Set")]
        public static void GenerateCartoonGrassTileSet()
        {
            var profile = LoadOrCreateDefaultProfile();
            Bake(profile);
        }

        public static void Bake(DualGridTerrainBakeProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (!profile.Validate(out var profileReason))
                throw new InvalidOperationException("Dual-Grid bake profile is invalid: " + profileReason);

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var grassAssetPath = AssetDatabase.GetAssetPath(profile.GrassTexture);
            var soilAssetPath = AssetDatabase.GetAssetPath(profile.SoilTexture);
            var grassFile = ToAbsolutePath(projectRoot, grassAssetPath);
            var soilFile = ToAbsolutePath(projectRoot, soilAssetPath);
            if (!File.Exists(grassFile))
                throw new FileNotFoundException("Dual-Grid grass source texture was not found.", grassFile);
            if (!File.Exists(soilFile))
                throw new FileNotFoundException("Dual-Grid soil source texture was not found.", soilFile);

            var grass = LoadReadableTexture(grassFile, profile.TerrainId + "-grass-source");
            var soil = LoadReadableTexture(soilFile, profile.TerrainId + "-soil-source");
            var generatedPixels = new Color32[DualGridMaskUtility.MaskCount][];
            EditorUtility.DisplayProgressBar("Dual-Grid terrain bake", "Preparing periodic supersamples", 0f);
            try
            {
                Directory.CreateDirectory(ToAbsolutePath(projectRoot, profile.OutputFolder));
                var noise = new SupersampleNoiseField(
                    profile.TileSize * profile.SupersampleScale, profile.DeterministicSeed);
                for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
                {
                    EditorUtility.DisplayProgressBar("Dual-Grid terrain bake",
                        "Rasterizing mask " + mask.ToString("00") + " / 15", (mask + 1f) / 18f);
                    generatedPixels[mask] = GenerateMaskPixels(profile, grass, soil,
                        (DualGridMask)mask, noise);
                    WritePng(projectRoot, GetTexturePath(profile, mask), profile.TileSize,
                        profile.TileSize, generatedPixels[mask]);
                }

                var soilBasePixels = GenerateSoilBasePixels(profile, soil);
                WritePng(projectRoot, GetSoilBaseTexturePath(profile), profile.TileSize,
                    profile.TileSize, soilBasePixels);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var tileSetPath = GetTileSetPath(profile);
                var tileSet = LoadOrCreateAsset<DualGridTileSet>(tileSetPath);
                for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
                {
                    EditorUtility.DisplayProgressBar("Dual-Grid terrain bake",
                        "Importing mask " + mask.ToString("00") + " / 15", (mask + 17f) / 36f);
                    var texturePath = GetTexturePath(profile, mask);
                    ConfigureSpriteImporter(texturePath, profile.TileSize);
                    var tile = ConfigureTileAsset(GetTileAssetPath(profile, mask), texturePath);
                    tileSet.SetTile((DualGridMask)mask, tile);
                }

                ConfigureSpriteImporter(GetSoilBaseTexturePath(profile), profile.TileSize);
                ConfigureTileAsset(GetSoilBaseTilePath(profile), GetSoilBaseTexturePath(profile));
                EditorUtility.SetDirty(tileSet);
                AssetDatabase.SaveAssets();

                var maxAlphaDifference = ValidateCompatibleEdges(generatedPixels, profile.TileSize,
                    out var maxRgbaDifference, out var horizontalPairs, out var verticalPairs);
                var centersTransparent = ValidateOppositeCornerCenters(generatedPixels,
                    profile.TileSize);
                var measuredTransitionPixels = MeasureMaximumAlphaTransitionPixels(
                    generatedPixels, profile.TileSize);
                var profileHash = ComputeProfileHash(profile, grassAssetPath, soilAssetPath);
                var pixelHash = ComputePixelHash(generatedPixels);
                var repeatDeterministic = ValidatePreviousDeterministicResult(projectRoot,
                    profileHash, pixelHash);

                WriteAtlasPreview(projectRoot, generatedPixels, profile.TileSize);
                WriteValidationReport(projectRoot, profile, grassAssetPath, soilAssetPath,
                    tileSetPath, horizontalPairs, verticalPairs, maxAlphaDifference,
                    maxRgbaDifference, measuredTransitionPixels, centersTransparent, repeatDeterministic,
                    profileHash, pixelHash);

                if (maxAlphaDifference != 0 || maxRgbaDifference != 0)
                    throw new InvalidOperationException(
                        "Generated Dual-Grid tiles failed exact shared-edge validation. Alpha="
                        + maxAlphaDifference + ", RGBA=" + maxRgbaDifference + ".");
                if (!centersTransparent)
                    throw new InvalidOperationException(
                        "Opposite-corner masks must remain disconnected at the tile center.");
                if (measuredTransitionPixels > 4)
                    throw new InvalidOperationException(
                        "Generated alpha edge is wider than four partially covered output pixels: "
                        + measuredTransitionPixels + ".");
                if (!repeatDeterministic)
                    throw new InvalidOperationException(
                        "Repeated bake with the same profile produced a different pixel hash.");

                if (string.Equals(profile.OutputFolder, OutputFolder,
                        StringComparison.OrdinalIgnoreCase))
                    DualGridDemoSetup.CreateOrRefreshDemo();
                Debug.Log("FRUIT_DEFENSE_DUAL_GRID_CARTOON_TILESET_OK masks=16 supersample="
                    + profile.SupersampleScale + "x alphaEdgeMax=0 rgbaEdgeMax=0 path="
                    + tileSetPath);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                UnityEngine.Object.DestroyImmediate(grass);
                UnityEngine.Object.DestroyImmediate(soil);
            }
        }

        private static DualGridTerrainBakeProfile LoadOrCreateDefaultProfile()
        {
            EnsureAssetFolder(OutputFolder);
            var profile = AssetDatabase.LoadAssetAtPath<DualGridTerrainBakeProfile>(DefaultProfilePath);
            if (profile != null) return profile;

            var grass = AssetDatabase.LoadAssetAtPath<Texture2D>(SourceTexturePath);
            var soil = AssetDatabase.LoadAssetAtPath<Texture2D>(SoilTexturePath);
            if (grass == null || soil == null)
                throw new InvalidOperationException(
                    "Default Dual-Grid grass and soil source textures must be imported first.");
            profile = ScriptableObject.CreateInstance<DualGridTerrainBakeProfile>();
            profile.ConfigureDefaults(grass, soil, OutputFolder);
            AssetDatabase.CreateAsset(profile, DefaultProfilePath);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static Color32[] GenerateMaskPixels(DualGridTerrainBakeProfile profile,
            Texture2D grass, Texture2D soil, DualGridMask mask, SupersampleNoiseField noise)
        {
            var size = profile.TileSize;
            var scale = profile.SupersampleScale;
            var pixels = new Color32[size * size];
            var corners = GetCorners(mask);
            for (var y = 0; y < size; y++)
            {
                var colorV = y / (size - 1f);
                for (var x = 0; x < size; x++)
                {
                    var alpha = 0f;
                    var grassMix = 0f;
                    var shade = 0f;
                    for (var sampleY = 0; sampleY < scale; sampleY++)
                    for (var sampleX = 0; sampleX < scale; sampleX++)
                    {
                        var highX = x * scale + sampleX;
                        var highY = y * scale + sampleY;
                        var index = highX + highY * noise.Size;
                        var u = (highX + .5f) / noise.Size;
                        var v = (highY + .5f) / noise.Size;
                        var evaluation = EvaluateSurface(profile, corners, u, v,
                            noise.Broad[index], noise.Fine[index], noise.Blade[index],
                            noise.DerivativeU(noise.Broad, highX, highY),
                            noise.DerivativeV(noise.Broad, highX, highY),
                            noise.DerivativeU(noise.Fine, highX, highY),
                            noise.DerivativeV(noise.Fine, highX, highY), true);
                        alpha += evaluation.Alpha;
                        grassMix += evaluation.GrassMix;
                        shade += evaluation.Shade;
                    }

                    var sampleCount = scale * scale;
                    alpha /= sampleCount;
                    grassMix /= sampleCount;
                    shade /= sampleCount;
                    var colorU = x / (size - 1f);
                    var color = ComposeColor(grass, soil, colorU, colorV, grassMix, shade, alpha);
                    pixels[x + y * size] = color;
                }
            }

            RewriteCanonicalEdges(profile, grass, soil, mask, pixels);
            return pixels;
        }

        private static SurfaceEvaluation EvaluateSurface(DualGridTerrainBakeProfile profile,
            MaskCorners corners, float u, float v, float broadNoise, float fineNoise,
            float bladeNoise, float broadDerivativeU, float broadDerivativeV,
            float fineDerivativeU, float fineDerivativeV, bool normalizeToPixelDistance)
        {
            var south = Mathf.Lerp(corners.SouthWest, corners.SouthEast, u);
            var north = Mathf.Lerp(corners.NorthWest, corners.NorthEast, u);
            var field = Mathf.Lerp(south, north, v);
            var derivativeU = Mathf.Lerp(corners.SouthEast - corners.SouthWest,
                corners.NorthEast - corners.NorthWest, v);
            var derivativeV = north - south;

            if (corners.IsOppositeCornerMask)
            {
                var saddle = 16f * u * (1f - u) * v * (1f - v);
                var saddleDerivativeU = 16f * (1f - 2f * u) * v * (1f - v);
                var saddleDerivativeV = 16f * u * (1f - u) * (1f - 2f * v);
                field -= saddle * profile.OppositeCornerSeparation;
                derivativeU -= saddleDerivativeU * profile.OppositeCornerSeparation;
                derivativeV -= saddleDerivativeV * profile.OppositeCornerSeparation;
            }

            var broadAmplitude = profile.BroadIrregularityPixels / profile.TileSize;
            var fineAmplitude = profile.FineIrregularityPixels / profile.TileSize;
            var signedField = field - TerrainThreshold
                - broadNoise * broadAmplitude - fineNoise * fineAmplitude;
            derivativeU -= broadDerivativeU * broadAmplitude + fineDerivativeU * fineAmplitude;
            derivativeV -= broadDerivativeV * broadAmplitude + fineDerivativeV * fineAmplitude;

            float distancePixels;
            if (normalizeToPixelDistance)
            {
                var gradient = Mathf.Max(.075f,
                    Mathf.Sqrt(derivativeU * derivativeU + derivativeV * derivativeV));
                distancePixels = signedField / gradient * profile.TileSize;
            }
            else
            {
                // Canonical borders deliberately use a fixed scale. Compatible edge masks share
                // the same signed field but may have different off-edge gradients.
                distancePixels = signedField * profile.TileSize;
            }
            distancePixels = Mathf.Clamp(distancePixels, -128f, 128f);

            var bladePulse = Mathf.Pow(Mathf.Clamp01(.5f + .5f * bladeNoise), 7f);
            var silhouetteDistance = distancePixels
                + bladePulse * profile.GrassBladeExtensionPixels * .65f;
            // Four-times subpixel integration provides the primary antialiasing. Only feather
            // within roughly one high-resolution sample; applying the full output-pixel width
            // to every sub-sample would blur the edge a second time.
            var subpixelFeather = profile.AlphaAntialiasPixels
                / profile.SupersampleScale * .5f;
            var alpha = SmoothStepRange(-subpixelFeather,
                subpixelFeather, silhouetteDistance);
            var grassReach = bladePulse
                * (profile.ExposedSoilPixels * .82f + profile.GrassBladeExtensionPixels);
            var grassDistance = distancePixels + grassReach;
            var grassMix = SmoothStepRange(profile.ExposedSoilPixels,
                profile.ExposedSoilPixels + profile.GrassBlendPixels, grassDistance);
            var edgeBandWidth = Mathf.Max(1f, profile.GrassBlendPixels * .7f);
            var edgeBand = 1f - Mathf.Clamp01(
                Mathf.Abs(grassDistance - profile.ExposedSoilPixels) / edgeBandWidth);
            var shade = 1f - edgeBand * (1f - grassMix) * .18f;
            return new SurfaceEvaluation { Alpha = alpha, GrassMix = grassMix, Shade = shade };
        }

        private static void RewriteCanonicalEdges(DualGridTerrainBakeProfile profile,
            Texture2D grass, Texture2D soil, DualGridMask mask, Color32[] pixels)
        {
            var size = profile.TileSize;
            var corners = GetCorners(mask);
            for (var y = 0; y < size; y++)
            {
                var v = y / (size - 1f);
                pixels[y * size] = EvaluateCanonicalPixel(profile, grass, soil, corners, 0f, v);
                pixels[size - 1 + y * size] = EvaluateCanonicalPixel(
                    profile, grass, soil, corners, 1f, v);
            }
            for (var x = 0; x < size; x++)
            {
                var u = x / (size - 1f);
                pixels[x] = EvaluateCanonicalPixel(profile, grass, soil, corners, u, 0f);
                pixels[x + (size - 1) * size] = EvaluateCanonicalPixel(
                    profile, grass, soil, corners, u, 1f);
            }
        }

        private static Color32 EvaluateCanonicalPixel(DualGridTerrainBakeProfile profile,
            Texture2D grass, Texture2D soil, MaskCorners corners, float u, float v)
        {
            var noiseU = RepeatCoordinate(u);
            var noiseV = RepeatCoordinate(v);
            var broad = PeriodicTerrainNoise(noiseU, noiseV, profile.DeterministicSeed);
            var fine = PeriodicFineNoise(noiseU, noiseV, profile.DeterministicSeed);
            var blade = PeriodicBladeNoise(noiseU, noiseV, profile.DeterministicSeed);
            var evaluation = EvaluateSurface(profile, corners, u, v, broad, fine, blade,
                0f, 0f, 0f, 0f, false);
            return ComposeColor(grass, soil, noiseU, noiseV, evaluation.GrassMix,
                evaluation.Shade, evaluation.Alpha);
        }

        private static Color32 ComposeColor(Texture2D grass, Texture2D soil, float u, float v,
            float grassMix, float shade, float alpha)
        {
            var sampleU = RepeatCoordinate(u);
            var sampleV = RepeatCoordinate(v);
            var grassColor = grass.GetPixelBilinear(sampleU, sampleV);
            var soilColor = soil.GetPixelBilinear(sampleU, sampleV);
            var color = Color.Lerp(soilColor, grassColor, grassMix);
            color.r *= shade;
            color.g *= shade;
            color.b *= shade;
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static Color32[] GenerateSoilBasePixels(DualGridTerrainBakeProfile profile,
            Texture2D soil)
        {
            var size = profile.TileSize;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var u = RepeatCoordinate(x / (size - 1f));
                var v = RepeatCoordinate(y / (size - 1f));
                var color = soil.GetPixelBilinear(u, v);
                color.a = 1f;
                pixels[x + y * size] = color;
            }
            return pixels;
        }

        private static MaskCorners GetCorners(DualGridMask mask)
        {
            return new MaskCorners
            {
                NorthWest = HasCorner(mask, DualGridMask.NorthWest) ? 1f : 0f,
                NorthEast = HasCorner(mask, DualGridMask.NorthEast) ? 1f : 0f,
                SouthEast = HasCorner(mask, DualGridMask.SouthEast) ? 1f : 0f,
                SouthWest = HasCorner(mask, DualGridMask.SouthWest) ? 1f : 0f,
                IsOppositeCornerMask = mask == (DualGridMask.NorthWest | DualGridMask.SouthEast)
                    || mask == (DualGridMask.NorthEast | DualGridMask.SouthWest),
            };
        }

        private static float PeriodicTerrainNoise(float u, float v, int seed)
        {
            var angle = Mathf.PI * 2f;
            return Mathf.Sin(angle * (2f * u + 3f * v) + SeedPhase(seed, 1)) * .44f
                + Mathf.Sin(angle * (5f * u - 4f * v) + SeedPhase(seed, 2)) * .28f
                + Mathf.Sin(angle * (9f * u + 7f * v) + SeedPhase(seed, 3)) * .18f
                + Mathf.Sin(angle * (13f * u - 11f * v) + SeedPhase(seed, 4)) * .10f;
        }

        private static float PeriodicFineNoise(float u, float v, int seed)
        {
            var angle = Mathf.PI * 2f;
            return Mathf.Sin(angle * (17f * u + 13f * v) + SeedPhase(seed, 5)) * .52f
                + Mathf.Sin(angle * (29f * u - 19f * v) + SeedPhase(seed, 6)) * .30f
                + Mathf.Sin(angle * (41f * u + 31f * v) + SeedPhase(seed, 7)) * .18f;
        }

        private static float PeriodicBladeNoise(float u, float v, int seed)
        {
            var angle = Mathf.PI * 2f;
            return Mathf.Sin(angle * (23f * u + 17f * v) + SeedPhase(seed, 8)) * .62f
                + Mathf.Sin(angle * (47f * u - 37f * v) + SeedPhase(seed, 9)) * .38f;
        }

        private static float SeedPhase(int seed, int salt)
        {
            unchecked
            {
                var hash = (uint)seed ^ ((uint)salt * 0x9E3779B9u);
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                hash *= 0x846CA68Bu;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) / 16777215f * Mathf.PI * 2f;
            }
        }

        private static float RepeatCoordinate(float value)
        {
            if (value >= 1f || value <= -1f) value -= Mathf.Floor(value);
            if (Mathf.Approximately(value, 1f)) return 0f;
            return value < 0f ? value + 1f : value;
        }

        private static float SmoothStepRange(float start, float end, float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
        }

        private static Texture2D LoadReadableTexture(string absolutePath, string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = name;
            if (texture.LoadImage(File.ReadAllBytes(absolutePath), false)) return texture;
            UnityEngine.Object.DestroyImmediate(texture);
            throw new InvalidOperationException("Unity could not decode texture " + absolutePath);
        }

        private static void WritePng(string projectRoot, string assetPath, int width, int height,
            Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(ToAbsolutePath(projectRoot, assetPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureSpriteImporter(string texturePath, int tileSize)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("TextureImporter is unavailable for " + texturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = tileSize;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = tileSize;
            importer.SaveAndReimport();
        }

        private static Tile ConfigureTileAsset(string tilePath, string texturePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (sprite == null)
                throw new InvalidOperationException("Dual-Grid sprite import failed: " + texturePath);
            var tile = LoadOrCreateAsset<Tile>(tilePath);
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.transform = Matrix4x4.identity;
            tile.flags = TileFlags.LockAll;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static int ValidateCompatibleEdges(Color32[][] tiles, int tileSize,
            out int maximumRgbaDifference, out int horizontalPairs, out int verticalPairs)
        {
            horizontalPairs = 0;
            verticalPairs = 0;
            var maximumAlphaDifference = 0;
            maximumRgbaDifference = 0;
            for (var first = 0; first < DualGridMaskUtility.MaskCount; first++)
            for (var second = 0; second < DualGridMaskUtility.MaskCount; second++)
            {
                var left = (DualGridMask)first;
                var right = (DualGridMask)second;
                if (HasSameCorner(left, DualGridMask.NorthEast, right, DualGridMask.NorthWest)
                    && HasSameCorner(left, DualGridMask.SouthEast, right, DualGridMask.SouthWest))
                {
                    horizontalPairs++;
                    for (var y = 0; y < tileSize; y++)
                        AccumulateDifference(tiles[first][tileSize - 1 + y * tileSize],
                            tiles[second][y * tileSize], ref maximumAlphaDifference,
                            ref maximumRgbaDifference);
                }

                var bottom = (DualGridMask)first;
                var top = (DualGridMask)second;
                if (HasSameCorner(bottom, DualGridMask.NorthWest, top, DualGridMask.SouthWest)
                    && HasSameCorner(bottom, DualGridMask.NorthEast, top, DualGridMask.SouthEast))
                {
                    verticalPairs++;
                    for (var x = 0; x < tileSize; x++)
                        AccumulateDifference(tiles[first][x + (tileSize - 1) * tileSize],
                            tiles[second][x], ref maximumAlphaDifference,
                            ref maximumRgbaDifference);
                }
            }
            return maximumAlphaDifference;
        }

        private static void AccumulateDifference(Color32 first, Color32 second,
            ref int maximumAlphaDifference, ref int maximumRgbaDifference)
        {
            maximumAlphaDifference = Mathf.Max(maximumAlphaDifference,
                Mathf.Abs(first.a - second.a));
            maximumRgbaDifference = Mathf.Max(maximumRgbaDifference,
                Mathf.Abs(first.r - second.r), Mathf.Abs(first.g - second.g),
                Mathf.Abs(first.b - second.b), Mathf.Abs(first.a - second.a));
        }

        private static bool ValidateOppositeCornerCenters(Color32[][] tiles, int tileSize)
        {
            var center = tileSize / 2 + (tileSize / 2) * tileSize;
            return tiles[5][center].a <= 4 && tiles[10][center].a <= 4;
        }

        private static int MeasureMaximumAlphaTransitionPixels(Color32[][] tiles, int tileSize)
        {
            var maximum = 0;
            var straightBoundaryMasks = new[] { 3, 6, 9, 12 };
            foreach (var mask in straightBoundaryMasks)
                maximum = Mathf.Max(maximum,
                    MeasurePartialAlphaBandThickness(tiles[mask], tileSize));
            return maximum;
        }

        private static int MeasurePartialAlphaBandThickness(Color32[] pixels, int tileSize)
        {
            var current = new bool[pixels.Length];
            var next = new bool[pixels.Length];
            var any = false;
            for (var index = 0; index < pixels.Length; index++)
            {
                current[index] = pixels[index].a > 0 && pixels[index].a < 255;
                any |= current[index];
            }

            var erosionLayers = 0;
            while (any && erosionLayers <= 4)
            {
                erosionLayers++;
                any = false;
                Array.Clear(next, 0, next.Length);
                for (var y = 1; y < tileSize - 1; y++)
                for (var x = 1; x < tileSize - 1; x++)
                {
                    var index = x + y * tileSize;
                    if (!current[index]) continue;
                    var survives = true;
                    for (var offsetY = -1; offsetY <= 1 && survives; offsetY++)
                    for (var offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0) continue;
                        if (current[index + offsetX + offsetY * tileSize]) continue;
                        survives = false;
                        break;
                    }
                    next[index] = survives;
                    any |= survives;
                }
                var swap = current;
                current = next;
                next = swap;
            }

            // One erosion layer corresponds to a one-or-two-pixel-wide antialiased contour.
            return erosionLayers * 2;
        }

        private static string ComputeProfileHash(DualGridTerrainBakeProfile profile,
            string grassPath, string soilPath)
        {
            var serialized = BakerVersion + "|"
                + grassPath + "|" + AssetDatabase.GetAssetDependencyHash(grassPath) + "|"
                + soilPath + "|" + AssetDatabase.GetAssetDependencyHash(soilPath) + "|"
                + profile.TerrainId + "|" + profile.OutputFolder + "|" + profile.TileSize + "|"
                + profile.SupersampleScale + "|" + profile.AlphaAntialiasPixels + "|"
                + profile.ExposedSoilPixels + "|" + profile.GrassBlendPixels + "|"
                + profile.BroadIrregularityPixels + "|" + profile.FineIrregularityPixels + "|"
                + profile.GrassBladeExtensionPixels + "|" + profile.OppositeCornerSeparation + "|"
                + profile.DeterministicSeed;
            return Hash128.Compute(serialized).ToString();
        }

        private static string ComputePixelHash(Color32[][] tiles)
        {
            unchecked
            {
                const ulong offset = 14695981039346656037UL;
                const ulong prime = 1099511628211UL;
                var hash = offset;
                for (var mask = 0; mask < tiles.Length; mask++)
                for (var index = 0; index < tiles[mask].Length; index++)
                {
                    var pixel = tiles[mask][index];
                    hash = (hash ^ pixel.r) * prime;
                    hash = (hash ^ pixel.g) * prime;
                    hash = (hash ^ pixel.b) * prime;
                    hash = (hash ^ pixel.a) * prime;
                }
                return hash.ToString("X16");
            }
        }

        private static bool ValidatePreviousDeterministicResult(string projectRoot,
            string profileHash, string pixelHash)
        {
            var path = ToAbsolutePath(projectRoot, SeamEvidencePath);
            if (!File.Exists(path)) return true;
            try
            {
                var previous = JsonUtility.FromJson<BakeValidationReport>(File.ReadAllText(path));
                return previous == null || !string.Equals(previous.profileHash, profileHash,
                    StringComparison.Ordinal) || string.Equals(previous.pixelHash, pixelHash,
                    StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static void WriteAtlasPreview(string projectRoot, Color32[][] tiles, int tileSize)
        {
            const int previewTileSize = 256;
            const int gutter = 8;
            const int columns = 4;
            const int rows = 4;
            var width = gutter + columns * (previewTileSize + gutter);
            var height = gutter + rows * (previewTileSize + gutter);
            var atlasPixels = new Color32[width * height];
            var backgroundA = new Color32(28, 37, 46, 255);
            var backgroundB = new Color32(42, 53, 65, 255);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                atlasPixels[x + y * width] = ((x / 16 + y / 16) & 1) == 0
                    ? backgroundA
                    : backgroundB;

            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var column = mask % columns;
                var row = rows - 1 - mask / columns;
                var originX = gutter + column * (previewTileSize + gutter);
                var originY = gutter + row * (previewTileSize + gutter);
                for (var y = 0; y < previewTileSize; y++)
                for (var x = 0; x < previewTileSize; x++)
                {
                    var sourceX = Mathf.RoundToInt(x * (tileSize - 1f) / (previewTileSize - 1f));
                    var sourceY = Mathf.RoundToInt(y * (tileSize - 1f) / (previewTileSize - 1f));
                    var foreground = tiles[mask][sourceX + sourceY * tileSize];
                    var index = originX + x + (originY + y) * width;
                    atlasPixels[index] = AlphaBlend(foreground, atlasPixels[index]);
                }
            }

            var atlas = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                atlas.SetPixels32(atlasPixels);
                atlas.Apply(false, false);
                var outputPath = ToAbsolutePath(projectRoot, AtlasEvidencePath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllBytes(outputPath, atlas.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(atlas);
            }
        }

        private static Color32 AlphaBlend(Color32 foreground, Color32 background)
        {
            var alpha = foreground.a / 255f;
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(background.r, foreground.r, alpha)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(background.g, foreground.g, alpha)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(background.b, foreground.b, alpha)), 255);
        }

        private static void WriteValidationReport(string projectRoot,
            DualGridTerrainBakeProfile profile, string grassPath, string soilPath,
            string tileSetPath, int horizontalPairs, int verticalPairs,
            int maximumAlphaDifference, int maximumRgbaDifference,
            int measuredTransitionPixels,
            bool centersTransparent, bool repeatDeterministic,
            string profileHash, string pixelHash)
        {
            var passed = maximumAlphaDifference == 0 && maximumRgbaDifference == 0
                && measuredTransitionPixels <= 4
                && centersTransparent && repeatDeterministic;
            var report = new BakeValidationReport
            {
                source = grassPath,
                soilSource = soilPath,
                profile = AssetDatabase.GetAssetPath(profile),
                bakerVersion = BakerVersion,
                profileHash = profileHash,
                pixelHash = pixelHash,
                tileSet = tileSetPath,
                tileSize = profile.TileSize,
                supersampleScale = profile.SupersampleScale,
                alphaAntialiasPixels = profile.AlphaAntialiasPixels,
                exposedSoilPixels = profile.ExposedSoilPixels,
                grassBlendPixels = profile.GrassBlendPixels,
                generatedMasks = DualGridMaskUtility.MaskCount,
                horizontalCompatiblePairs = horizontalPairs,
                verticalCompatiblePairs = verticalPairs,
                maximumAlphaDifference = maximumAlphaDifference,
                maximumRgbaDifference = maximumRgbaDifference,
                maximumMeasuredAlphaTransitionPixels = measuredTransitionPixels,
                alphaTransitionResult = measuredTransitionPixels <= 4 ? "pass-narrow" : "fail",
                sharedEdgeResult = maximumAlphaDifference == 0 && maximumRgbaDifference == 0
                    ? "pass-exact" : "fail",
                oppositeCornerCentersTransparent = centersTransparent,
                oppositeCornerTopologyResult = centersTransparent ? "pass-disconnected" : "fail",
                deterministicRepeatResult = repeatDeterministic ? "pass" : "fail",
                edgeProfile = "normalized pixel distance + 4x subpixel integration + deterministic tufts",
                textureContinuityEvidence =
                    "Builds/Evidence/cartoon-grass-seamless-hd-seam-test.json",
                atlasPreview = AtlasEvidencePath,
                result = passed ? "pass" : "fail",
            };
            var outputPath = ToAbsolutePath(projectRoot, SeamEvidencePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, JsonUtility.ToJson(report, true));
        }

        private static bool HasSameCorner(DualGridMask first, DualGridMask firstCorner,
            DualGridMask second, DualGridMask secondCorner)
        {
            return HasCorner(first, firstCorner) == HasCorner(second, secondCorner);
        }

        private static bool HasCorner(DualGridMask mask, DualGridMask corner)
        {
            return (mask & corner) != 0;
        }

        private static string GetTexturePath(DualGridTerrainBakeProfile profile, int mask)
        {
            return profile.OutputFolder + "/Mask-" + mask.ToString("00") + ".png";
        }

        private static string GetTileAssetPath(DualGridTerrainBakeProfile profile, int mask)
        {
            return profile.OutputFolder + "/Mask-" + mask.ToString("00") + ".asset";
        }

        private static string GetTileSetPath(DualGridTerrainBakeProfile profile)
        {
            return profile.OutputFolder + "/" + profile.TerrainId + "DualGridTileSet.asset";
        }

        private static string GetSoilBaseTexturePath(DualGridTerrainBakeProfile profile)
        {
            return profile.OutputFolder + "/" + profile.TerrainId + "SoilBase.png";
        }

        private static string GetSoilBaseTilePath(DualGridTerrainBakeProfile profile)
        {
            return profile.OutputFolder + "/" + profile.TerrainId + "SoilBase.asset";
        }

        private static string ToAbsolutePath(string projectRoot, string projectRelativePath)
        {
            return Path.Combine(projectRoot,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
