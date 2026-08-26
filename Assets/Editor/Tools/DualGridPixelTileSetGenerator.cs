using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public static class DualGridPixelTileSetGenerator
    {
        public const string RootFolder = "Assets/DualGridDemo/PixelGrass";
        public const string SourceFolder = RootFolder + "/Sources";
        public const string GrassSourcePath = SourceFolder + "/PixelGrassSource.png";
        public const string SoilSourcePath = SourceFolder + "/PixelSoilSource.png";
        public const string OutputFolder = RootFolder + "/Generated";
        public const string DefaultProfilePath = RootFolder + "/PixelGrassBakeProfile.asset";
        public const string TileSetPath = OutputFolder + "/PixelGrassDualGridTileSet.asset";
        public const string AtlasEvidencePath =
            "Builds/Evidence/pixel-grass-dual-grid-16-mask-atlas.png";
        public const string ValidationEvidencePath =
            "Builds/Evidence/pixel-grass-dual-grid-validation.json";

        private const string BakerVersion = "pixel-texture-guided-sockets-v2";
        private const float TerrainThreshold = .5f;
        private const float OppositeCornerSeparation = .14f;
        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

        [Serializable]
        private sealed class PixelBakeValidationReport
        {
            public string grassSource;
            public string soilSource;
            public string profile;
            public string sourceOrigin;
            public string sourceLayout;
            public string bakerVersion;
            public string profileHash;
            public string pixelHash;
            public string tileSet;
            public int tileSize;
            public int outlinePixels;
            public int soilRimPixels;
            public int textureGuidancePixels;
            public bool solidOutlineActive;
            public bool sourceGuidanceAvailable;
            public int textureGuidedChangedPixels;
            public int generatedMasks;
            public int horizontalCompatiblePairs;
            public int verticalCompatiblePairs;
            public int maximumRgbaDifference;
            public int invalidAlphaPixels;
            public int invalidPalettePixels;
            public bool emptyMaskTransparent;
            public bool fullMaskOpaque;
            public bool oppositeCornerCentersTransparent;
            public int oppositeCornerComponentCount05;
            public int oppositeCornerComponentCount10;
            public int invalidTopologyMasks;
            public string deterministicRepeatResult;
            public string importerResult;
            public string tileSetResult;
            public string atlasPreview;
            public string result;
        }

        private sealed class PixelSource
        {
            public readonly int Width;
            public readonly int Height;
            public readonly Color32[] Pixels;
            public readonly float MinimumLuminance;
            public readonly float MaximumLuminance;

            public bool HasGuidanceRange
            {
                get { return MaximumLuminance - MinimumLuminance >= 1f; }
            }

            public PixelSource(int width, int height, Color32[] pixels)
            {
                Width = width;
                Height = height;
                Pixels = pixels;
                MinimumLuminance = 255f;
                MaximumLuminance = 0f;
                for (var index = 0; index < pixels.Length; index++)
                {
                    var luminance = Luminance(pixels[index]);
                    MinimumLuminance = Mathf.Min(MinimumLuminance, luminance);
                    MaximumLuminance = Mathf.Max(MaximumLuminance, luminance);
                }
            }

            public Color32 Sample(int x, int y, int targetSize, int seed)
            {
                var phaseX = PositiveMod(seed * 17 + 11, Width);
                var phaseY = PositiveMod(seed * 29 + 7, Height);
                var scaledX = (int)(((long)(x * 2 + 1) * Width) / (targetSize * 2L));
                var scaledY = (int)(((long)(y * 2 + 1) * Height) / (targetSize * 2L));
                var sourceX = PositiveMod(scaledX + phaseX, Width);
                var sourceY = PositiveMod(scaledY + phaseY, Height);
                var color = Pixels[sourceX + sourceY * Width];
                color.a = 255;
                return color;
            }

            public float SampleGuidance(int x, int y, int targetSize, int seed)
            {
                if (!HasGuidanceRange) return 0f;
                var luminance = 0f;
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                    luminance += Luminance(Sample(
                        x + offsetX, y + offsetY, targetSize, seed));
                luminance /= 9f;
                var midpoint = (MinimumLuminance + MaximumLuminance) * .5f;
                var halfRange = Mathf.Max(.5f,
                    (MaximumLuminance - MinimumLuminance) * .5f);
                return Mathf.Clamp((luminance - midpoint) / halfRange, -1f, 1f);
            }

            private static float Luminance(Color32 color)
            {
                return (54f * color.r + 183f * color.g + 19f * color.b) / 256f;
            }
        }

        private struct PixelValidation
        {
            public int HorizontalPairs;
            public int VerticalPairs;
            public int MaximumRgbaDifference;
            public int InvalidAlphaPixels;
            public int InvalidPalettePixels;
            public bool EmptyMaskTransparent;
            public bool FullMaskOpaque;
            public bool CentersTransparent;
            public int Components05;
            public int Components10;
            public bool SourceGuidanceAvailable;
            public bool GuidanceExpected;
            public int GuidanceChangedPixels;
            public int InvalidTopologyMasks;

            public bool Passed
            {
                get
                {
                    return HorizontalPairs == 64
                        && VerticalPairs == 64
                        && MaximumRgbaDifference == 0
                        && InvalidAlphaPixels == 0
                        && InvalidPalettePixels == 0
                        && EmptyMaskTransparent
                        && FullMaskOpaque
                        && CentersTransparent
                        && Components05 == 2
                        && Components10 == 2
                        && InvalidTopologyMasks == 0
                        && (!GuidanceExpected || GuidanceChangedPixels > 0);
                }
            }
        }

        [MenuItem("Fruit Defense/Dual Grid/Generate Pixel Grass Tile Set")]
        public static void GeneratePixelGrassTileSet()
        {
            var profile = LoadOrCreateDefaultProfile();
            Bake(profile);
        }

        [MenuItem("Fruit Defense/Dual Grid/Rebake All Pixel Terrain Profiles")]
        public static void RebakeAllPixelTerrainProfiles()
        {
            var profileGuids = AssetDatabase.FindAssets("t:DualGridPixelTerrainProfile");
            var profilePaths = new List<string>(profileGuids.Length);
            foreach (var guid in profileGuids)
                profilePaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            profilePaths.Sort(StringComparer.Ordinal);
            foreach (var profilePath in profilePaths)
            {
                var profile = AssetDatabase.LoadAssetAtPath<DualGridPixelTerrainProfile>(
                    profilePath);
                if (profile == null)
                    throw new InvalidOperationException(
                        "Pixel terrain profile could not be loaded: " + profilePath);
                Bake(profile);
            }
            Debug.Log("FRUIT_DEFENSE_DUAL_GRID_PIXEL_ALL_PROFILES_BAKED count="
                + profilePaths.Count);
        }

        public static void EnsureValidationEvidence()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var profileGuids = AssetDatabase.FindAssets("t:DualGridPixelTerrainProfile");
            var profilePaths = new List<string>(profileGuids.Length);
            foreach (var guid in profileGuids)
                profilePaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            profilePaths.Sort(StringComparer.Ordinal);
            foreach (var profilePath in profilePaths)
            {
                var profile = AssetDatabase.LoadAssetAtPath<DualGridPixelTerrainProfile>(
                    profilePath);
                if (profile == null)
                    throw new InvalidOperationException(
                        "Pixel terrain profile could not be loaded: " + profilePath);
                var validationPath = ToAbsolutePath(projectRoot,
                    GetValidationEvidencePath(profile));
                var atlasPath = ToAbsolutePath(projectRoot, GetAtlasEvidencePath(profile));
                if (!File.Exists(validationPath) || !File.Exists(atlasPath)) Bake(profile);
            }
        }

        public static string GetTileSetAssetPath(DualGridPixelTerrainProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            return profile.OutputFolder + "/" + profile.TerrainId + "DualGridTileSet.asset";
        }

        public static string GetAtlasEvidencePath(DualGridPixelTerrainProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            return "Builds/Evidence/" + ToEvidenceStem(profile.TerrainId)
                + "-dual-grid-16-mask-atlas.png";
        }

        public static string GetValidationEvidencePath(DualGridPixelTerrainProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            return "Builds/Evidence/" + ToEvidenceStem(profile.TerrainId)
                + "-dual-grid-validation.json";
        }

        public static string ToEvidenceStem(string terrainId)
        {
            if (string.IsNullOrWhiteSpace(terrainId)) return "pixel-terrain";
            var builder = new StringBuilder(terrainId.Length + 8);
            var previousWasSeparator = true;
            var previousWasLowerOrDigit = false;
            foreach (var character in terrainId.Trim())
            {
                if (!char.IsLetterOrDigit(character))
                {
                    if (!previousWasSeparator && builder.Length > 0) builder.Append('-');
                    previousWasSeparator = true;
                    previousWasLowerOrDigit = false;
                    continue;
                }

                if (char.IsUpper(character) && previousWasLowerOrDigit
                    && !previousWasSeparator && builder.Length > 0)
                    builder.Append('-');
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                previousWasLowerOrDigit = char.IsLower(character) || char.IsDigit(character);
            }
            var result = builder.ToString().Trim('-');
            return result.Length == 0 ? "pixel-terrain" : result;
        }

        public static void Bake(DualGridPixelTerrainProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (!profile.Validate(out var profileReason))
                throw new InvalidOperationException(
                    "Dual-Grid pixel bake profile is invalid: " + profileReason);

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var grassAssetPath = AssetDatabase.GetAssetPath(profile.GrassTexture);
            var soilAssetPath = AssetDatabase.GetAssetPath(profile.SoilTexture);
            var grassFile = ToAbsolutePath(projectRoot, grassAssetPath);
            var soilFile = ToAbsolutePath(projectRoot, soilAssetPath);
            RequireAuthoringSource(grassFile, "grass", profile.SourceOrigin);
            RequireAuthoringSource(soilFile, "soil", profile.SourceOrigin);
            ConfigureSourceImporter(grassAssetPath);
            if (!string.Equals(grassAssetPath, soilAssetPath, StringComparison.Ordinal))
                ConfigureSourceImporter(soilAssetPath);

            var grass = LoadPixelSource(grassFile, "pixel-grass-authoring-source");
            var soil = LoadPixelSource(soilFile, "pixel-soil-authoring-source");
            ValidateOpaqueSource(grass, "grass", profile.SourceOrigin);
            ValidateOpaqueSource(soil, "soil", profile.SourceOrigin);

            var generated = GenerateAllMasks(profile, grass, soil);
            var repeated = GenerateAllMasks(profile, grass, soil);
            var pixelHash = ComputePixelHash(generated);
            var repeatedHash = ComputePixelHash(repeated);
            var repeatDeterministic = string.Equals(
                pixelHash, repeatedHash, StringComparison.Ordinal);
            var validation = ValidatePixels(profile, grass, soil, generated);
            if (!validation.Passed)
                throw new InvalidOperationException(DescribeValidationFailure(validation));
            if (!repeatDeterministic)
                throw new InvalidOperationException(
                    "Pixel Dual-Grid repeat generation produced a different pixel hash.");

            Directory.CreateDirectory(ToAbsolutePath(projectRoot, profile.OutputFolder));
            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                WritePng(projectRoot, GetTexturePath(profile, mask), profile.TileSize,
                    profile.TileSize, generated[mask]);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var tileSetPath = GetTileSetPath(profile);
            var tileSet = LoadOrCreateAsset<DualGridTileSet>(tileSetPath);
            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var texturePath = GetTexturePath(profile, mask);
                ConfigureGeneratedSpriteImporter(texturePath, profile.TileSize);
                var tile = ConfigureTileAsset(GetTileAssetPath(profile, mask), texturePath);
                tileSet.SetTile((DualGridMask)mask, tile);
            }
            EditorUtility.SetDirty(tileSet);
            AssetDatabase.SaveAssets();

            ValidateImportersAndTileSet(profile, tileSet);
            var atlasEvidencePath = GetAtlasEvidencePath(profile);
            var validationEvidencePath = GetValidationEvidencePath(profile);
            WriteAtlas(projectRoot, generated, profile.TileSize, atlasEvidencePath);
            var profileHash = ComputeProfileHash(profile, grassFile, soilFile);
            WriteValidationReport(projectRoot, profile, grassAssetPath, soilAssetPath,
                tileSetPath, profileHash, pixelHash, validation, repeatDeterministic,
                atlasEvidencePath, validationEvidencePath);

            Debug.Log("FRUIT_DEFENSE_DUAL_GRID_PIXEL_TILESET_OK masks=16 size="
                + profile.TileSize + " point=true binaryAlpha=true path=" + tileSetPath);
        }

        public static void ValidateGeneratedPixelTileSet()
        {
            var profileGuids = AssetDatabase.FindAssets("t:DualGridPixelTerrainProfile");
            if (profileGuids.Length == 0)
                throw new InvalidOperationException("No Dual-Grid pixel terrain profiles were found.");

            var profilePaths = new List<string>(profileGuids.Length);
            foreach (var guid in profileGuids) profilePaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            profilePaths.Sort(StringComparer.Ordinal);
            var evidenceOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var profilePath in profilePaths)
            {
                var profile = AssetDatabase.LoadAssetAtPath<DualGridPixelTerrainProfile>(profilePath);
                if (profile == null)
                    throw new InvalidOperationException(
                        "Pixel terrain profile could not be loaded: " + profilePath);
                var evidencePath = GetValidationEvidencePath(profile);
                if (evidenceOwners.TryGetValue(evidencePath, out var existingOwner))
                    throw new InvalidOperationException(
                        "Pixel terrain evidence path collision between " + existingOwner + " and "
                        + profilePath + ": " + evidencePath);
                evidenceOwners.Add(evidencePath, profilePath);
                try
                {
                    ValidateGeneratedPixelTileSet(profile);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        "Pixel terrain validation failed for profile " + profilePath + ": "
                        + exception.Message, exception);
                }
            }
            Debug.Log("FRUIT_DEFENSE_DUAL_GRID_PIXEL_ALL_PROFILES_OK count="
                + profilePaths.Count);
        }

        public static void ValidateGeneratedPixelTileSet(DualGridPixelTerrainProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (!profile.Validate(out var reason))
                throw new InvalidOperationException("Generated pixel terrain profile is invalid: " + reason);

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var grassPath = AssetDatabase.GetAssetPath(profile.GrassTexture);
            var soilPath = AssetDatabase.GetAssetPath(profile.SoilTexture);
            var grassFile = ToAbsolutePath(projectRoot, grassPath);
            var soilFile = ToAbsolutePath(projectRoot, soilPath);
            RequireAuthoringSource(grassFile, "grass", profile.SourceOrigin);
            RequireAuthoringSource(soilFile, "soil", profile.SourceOrigin);
            var grass = LoadPixelSource(grassFile, "pixel-grass-validation-source");
            var soil = LoadPixelSource(soilFile, "pixel-soil-validation-source");
            ValidateOpaqueSource(grass, "grass", profile.SourceOrigin);
            ValidateOpaqueSource(soil, "soil", profile.SourceOrigin);

            var generated = new Color32[DualGridMaskUtility.MaskCount][];
            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var texturePath = GetTexturePath(profile, mask);
                var absolutePath = ToAbsolutePath(projectRoot, texturePath);
                if (!File.Exists(absolutePath))
                    throw new FileNotFoundException(
                        "Generated pixel terrain mask is missing for " + profile.TerrainId + ".",
                        absolutePath);
                var source = LoadPixelSource(absolutePath, "pixel-mask-validation-" + mask);
                if (source.Width != profile.TileSize || source.Height != profile.TileSize)
                    throw new InvalidOperationException(
                        "Generated pixel terrain mask " + profile.TerrainId + "/" + mask
                        + " has unexpected dimensions "
                        + source.Width + "x" + source.Height + ".");
                generated[mask] = source.Pixels;
            }

            var validation = ValidatePixels(profile, grass, soil, generated);
            if (!validation.Passed)
                throw new InvalidOperationException(DescribeValidationFailure(validation));
            var tileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(GetTileSetPath(profile));
            ValidateImportersAndTileSet(profile, tileSet);

            var validationEvidencePath = GetValidationEvidencePath(profile);
            var atlasEvidencePath = GetAtlasEvidencePath(profile);
            var evidencePath = ToAbsolutePath(projectRoot, validationEvidencePath);
            if (!File.Exists(evidencePath))
                throw new FileNotFoundException(
                    "Pixel Dual-Grid validation evidence is missing for "
                    + profile.TerrainId + ".", evidencePath);
            var atlasPath = ToAbsolutePath(projectRoot, atlasEvidencePath);
            if (!File.Exists(atlasPath))
                throw new FileNotFoundException(
                    "Pixel Dual-Grid atlas evidence is missing for "
                    + profile.TerrainId + ".", atlasPath);
            var evidence = JsonUtility.FromJson<PixelBakeValidationReport>(
                File.ReadAllText(evidencePath));
            var actualPixelHash = ComputePixelHash(generated);
            var actualProfileHash = ComputeProfileHash(profile, grassFile, soilFile);
            if (evidence == null || evidence.result != "pass"
                || evidence.bakerVersion != BakerVersion
                || evidence.profile != AssetDatabase.GetAssetPath(profile)
                || evidence.sourceOrigin != profile.SourceOrigin.ToString()
                || evidence.sourceLayout != profile.SourceLayout.ToString()
                || evidence.profileHash != actualProfileHash
                || evidence.tileSet != GetTileSetPath(profile)
                || evidence.atlasPreview != atlasEvidencePath
                || evidence.pixelHash != actualPixelHash
                || evidence.outlinePixels != profile.OutlinePixels
                || evidence.soilRimPixels != profile.SoilRimPixels
                || evidence.textureGuidancePixels != profile.TextureGuidancePixels
                || evidence.solidOutlineActive != (profile.OutlinePixels > 0)
                || evidence.sourceGuidanceAvailable != validation.SourceGuidanceAvailable
                || evidence.textureGuidedChangedPixels != validation.GuidanceChangedPixels
                || evidence.horizontalCompatiblePairs != 64
                || evidence.verticalCompatiblePairs != 64
                || evidence.maximumRgbaDifference != 0
                || evidence.invalidAlphaPixels != 0
                || evidence.invalidPalettePixels != 0
                || !evidence.oppositeCornerCentersTransparent
                || evidence.oppositeCornerComponentCount05 != 2
                || evidence.oppositeCornerComponentCount10 != 2
                || evidence.invalidTopologyMasks != 0
                || evidence.deterministicRepeatResult != "pass"
                || evidence.importerResult != "pass"
                || evidence.tileSetResult != "pass")
                throw new InvalidOperationException(
                    "Pixel Dual-Grid validation evidence does not match generated assets for "
                    + profile.TerrainId + ".");
        }

        private static DualGridPixelTerrainProfile LoadOrCreateDefaultProfile()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            RequireAuthoringSource(ToAbsolutePath(projectRoot, GrassSourcePath), "grass",
                DualGridPixelSourceOrigin.Imagegen);
            RequireAuthoringSource(ToAbsolutePath(projectRoot, SoilSourcePath), "soil",
                DualGridPixelSourceOrigin.Imagegen);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureSourceImporter(GrassSourcePath);
            ConfigureSourceImporter(SoilSourcePath);
            EnsureAssetFolder(RootFolder);
            EnsureAssetFolder(OutputFolder);

            var grass = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassSourcePath);
            var soil = AssetDatabase.LoadAssetAtPath<Texture2D>(SoilSourcePath);
            if (grass == null || soil == null)
                throw new InvalidOperationException(
                    "Imagegen PixelGrass source textures could not be imported. No fallback art is generated.");

            var profile = AssetDatabase.LoadAssetAtPath<DualGridPixelTerrainProfile>(
                DefaultProfilePath);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<DualGridPixelTerrainProfile>();
            profile.ConfigureDefaults(grass, soil, OutputFolder);
            AssetDatabase.CreateAsset(profile, DefaultProfilePath);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static Color32[][] GenerateAllMasks(DualGridPixelTerrainProfile profile,
            PixelSource grass, PixelSource soil)
        {
            var result = new Color32[DualGridMaskUtility.MaskCount][];
            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var land = BuildLandMask(profile, grass, (DualGridMask)mask, true);
                result[mask] = ComposePixels(profile, grass, soil, (DualGridMask)mask, land);
            }
            return result;
        }

        private static bool[] BuildLandMask(DualGridPixelTerrainProfile profile,
            PixelSource grass, DualGridMask mask, bool applyTextureGuidance)
        {
            var size = profile.TileSize;
            var land = new bool[size * size];
            var bitCount = CountBits((int)mask);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                bool occupied;
                if (bitCount == 0)
                {
                    occupied = false;
                }
                else if (bitCount == 4)
                {
                    occupied = true;
                }
                else
                {
                    var distance = CornerFieldDistance(mask, x, y, size);
                    if (applyTextureGuidance && profile.TextureGuidancePixels > 0)
                        distance += grass.SampleGuidance(x, y, size,
                            profile.DeterministicSeed) * profile.TextureGuidancePixels;
                    occupied = distance >= 0f;
                }
                land[x + y * size] = occupied;
            }

            if (mask == (DualGridMask.NorthWest | DualGridMask.SouthEast)
                || mask == (DualGridMask.NorthEast | DualGridMask.SouthWest))
            {
                var half = size / 2;
                for (var y = half - 1; y <= half; y++)
                for (var x = half - 1; x <= half; x++)
                    land[x + y * size] = false;
            }
            return land;
        }

        private static float CornerFieldDistance(DualGridMask mask, int x, int y, int size)
        {
            var u = (x + .5f) / size;
            var v = (y + .5f) / size;
            var southWest = HasCorner(mask, DualGridMask.SouthWest) ? 1f : 0f;
            var southEast = HasCorner(mask, DualGridMask.SouthEast) ? 1f : 0f;
            var northWest = HasCorner(mask, DualGridMask.NorthWest) ? 1f : 0f;
            var northEast = HasCorner(mask, DualGridMask.NorthEast) ? 1f : 0f;
            var south = Mathf.Lerp(southWest, southEast, u);
            var north = Mathf.Lerp(northWest, northEast, u);
            var field = Mathf.Lerp(south, north, v);
            var derivativeU = Mathf.Lerp(southEast - southWest,
                northEast - northWest, v);
            var derivativeV = north - south;

            if (mask == (DualGridMask.NorthWest | DualGridMask.SouthEast)
                || mask == (DualGridMask.NorthEast | DualGridMask.SouthWest))
            {
                var saddle = 16f * u * (1f - u) * v * (1f - v);
                field -= saddle * OppositeCornerSeparation;
                derivativeU -= 16f * (1f - 2f * u) * v * (1f - v)
                    * OppositeCornerSeparation;
                derivativeV -= 16f * u * (1f - u) * (1f - 2f * v)
                    * OppositeCornerSeparation;
            }

            var gradient = Mathf.Max(.075f,
                Mathf.Sqrt(derivativeU * derivativeU + derivativeV * derivativeV));
            return Mathf.Clamp((field - TerrainThreshold) / gradient * size,
                -size * 2f, size * 2f);
        }

        private static Color32[] ComposePixels(DualGridPixelTerrainProfile profile,
            PixelSource grass, PixelSource soil, DualGridMask mask, bool[] land)
        {
            var size = profile.TileSize;
            var pixels = new Color32[size * size];
            var bandWidth = profile.OutlinePixels + profile.SoilRimPixels;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                if (!land[x + y * size])
                {
                    pixels[x + y * size] = Transparent;
                    continue;
                }

                var distance = DistanceToTransparent(land, size, x, y, bandWidth);
                Color32 color;
                if (distance <= profile.OutlinePixels)
                    color = profile.EdgeColor;
                else if (distance <= bandWidth)
                    color = soil.Sample(x, y, size, profile.DeterministicSeed + 101);
                else
                    color = grass.Sample(x, y, size, profile.DeterministicSeed);
                color.a = 255;
                pixels[x + y * size] = color;
            }

            RewriteCanonicalSockets(profile, grass, soil, mask, pixels);
            return pixels;
        }

        private static int DistanceToTransparent(bool[] land, int size, int x, int y,
            int maximumDistance)
        {
            for (var radius = 1; radius <= maximumDistance; radius++)
            {
                for (var offsetY = -radius; offsetY <= radius; offsetY++)
                for (var offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    if (Mathf.Max(Mathf.Abs(offsetX), Mathf.Abs(offsetY)) != radius) continue;
                    if (!IsLandWithClampedContinuation(
                            land, size, x + offsetX, y + offsetY))
                        return radius;
                }
            }
            return maximumDistance + 1;
        }

        private static bool IsLandWithClampedContinuation(bool[] land, int size, int x, int y)
        {
            var clampedX = Mathf.Clamp(x, 0, size - 1);
            var clampedY = Mathf.Clamp(y, 0, size - 1);
            return land[clampedX + clampedY * size];
        }

        private static void RewriteCanonicalSockets(DualGridPixelTerrainProfile profile,
            PixelSource grass, PixelSource soil, DualGridMask mask, Color32[] pixels)
        {
            var size = profile.TileSize;
            var verticalCut = FindSocketCut(profile, grass, true);
            var horizontalCut = FindSocketCut(profile, grass, false);
            for (var index = 0; index < size; index++)
            {
                pixels[index * size] = SocketPixel(profile, grass, soil,
                    HasCorner(mask, DualGridMask.SouthWest),
                    HasCorner(mask, DualGridMask.NorthWest), index, verticalCut, true);
                pixels[size - 1 + index * size] = SocketPixel(profile, grass, soil,
                    HasCorner(mask, DualGridMask.SouthEast),
                    HasCorner(mask, DualGridMask.NorthEast), index, verticalCut, true);
                pixels[index] = SocketPixel(profile, grass, soil,
                    HasCorner(mask, DualGridMask.SouthWest),
                    HasCorner(mask, DualGridMask.SouthEast), index, horizontalCut, false);
                pixels[index + (size - 1) * size] = SocketPixel(profile, grass, soil,
                    HasCorner(mask, DualGridMask.NorthWest),
                    HasCorner(mask, DualGridMask.NorthEast), index, horizontalCut, false);
            }

            RewriteCorner(profile, grass, pixels, 0, size - 1,
                HasCorner(mask, DualGridMask.NorthWest));
            RewriteCorner(profile, grass, pixels, size - 1, size - 1,
                HasCorner(mask, DualGridMask.NorthEast));
            RewriteCorner(profile, grass, pixels, size - 1, 0,
                HasCorner(mask, DualGridMask.SouthEast));
            RewriteCorner(profile, grass, pixels, 0, 0,
                HasCorner(mask, DualGridMask.SouthWest));
        }

        private static Color32 SocketPixel(DualGridPixelTerrainProfile profile,
            PixelSource grass, PixelSource soil, bool startOccupied, bool endOccupied,
            int index, int transitionIndex, bool vertical)
        {
            var size = profile.TileSize;
            var occupied = startOccupied && endOccupied;
            var distance = size;
            if (startOccupied != endOccupied)
            {
                occupied = startOccupied
                    ? index < transitionIndex
                    : index >= transitionIndex;
                distance = startOccupied
                    ? transitionIndex - index
                    : index - transitionIndex + 1;
            }
            if (!occupied) return Transparent;

            Color32 color;
            if (distance <= profile.OutlinePixels)
                color = profile.EdgeColor;
            else if (distance <= profile.OutlinePixels + profile.SoilRimPixels)
                color = vertical
                    ? soil.Sample(0, index, size, profile.DeterministicSeed + 101)
                    : soil.Sample(index, 0, size, profile.DeterministicSeed + 101);
            else
                color = vertical
                    ? grass.Sample(0, index, size, profile.DeterministicSeed)
                    : grass.Sample(index, 0, size, profile.DeterministicSeed);
            color.a = 255;
            return color;
        }

        private static int FindSocketCut(DualGridPixelTerrainProfile profile,
            PixelSource grass, bool vertical)
        {
            var size = profile.TileSize;
            var half = size / 2;
            var guidanceWidth = profile.TextureGuidancePixels;
            if (guidanceWidth <= 0 || !grass.HasGuidanceRange) return half;

            var bestCut = half;
            var bestScore = float.MaxValue;
            var minimum = Mathf.Max(1, half - guidanceWidth);
            var maximum = Mathf.Min(size - 1, half + guidanceWidth);
            for (var cut = minimum; cut <= maximum; cut++)
            {
                var guidance = vertical
                    ? grass.SampleGuidance(0, cut, size, profile.DeterministicSeed)
                    : grass.SampleGuidance(cut, 0, size, profile.DeterministicSeed);
                var score = guidance + Mathf.Abs(cut - half) * .15f;
                if (score >= bestScore) continue;
                bestScore = score;
                bestCut = cut;
            }
            return bestCut;
        }

        private static void RewriteCorner(DualGridPixelTerrainProfile profile,
            PixelSource grass, Color32[] pixels, int x, int y, bool occupied)
        {
            if (!occupied)
            {
                pixels[x + y * profile.TileSize] = Transparent;
                return;
            }
            var color = grass.Sample(0, 0, profile.TileSize, profile.DeterministicSeed);
            color.a = 255;
            pixels[x + y * profile.TileSize] = color;
        }

        private static PixelValidation ValidatePixels(DualGridPixelTerrainProfile profile,
            PixelSource grass, PixelSource soil, Color32[][] generated)
        {
            var allowed = new HashSet<uint>();
            AddPalette(allowed, grass.Pixels);
            AddPalette(allowed, soil.Pixels);
            if (profile.OutlinePixels > 0) allowed.Add(ColorKey(profile.EdgeColor));

            var result = new PixelValidation
            {
                EmptyMaskTransparent = true,
                FullMaskOpaque = true,
                CentersTransparent = true,
                SourceGuidanceAvailable = grass.HasGuidanceRange,
                GuidanceExpected = profile.TextureGuidancePixels > 0
                    && grass.HasGuidanceRange,
            };
            for (var mask = 0; mask < generated.Length; mask++)
            {
                var pixels = generated[mask];
                for (var index = 0; index < pixels.Length; index++)
                {
                    var color = pixels[index];
                    if (color.a != 0 && color.a != 255) result.InvalidAlphaPixels++;
                    if (color.a == 255 && !allowed.Contains(ColorKey(color)))
                        result.InvalidPalettePixels++;
                    if (mask == 0 && color.a != 0) result.EmptyMaskTransparent = false;
                    if (mask == 15 && color.a != 255) result.FullMaskOpaque = false;
                }
            }

            ValidateCompatibleBorders(generated, profile.TileSize, ref result);
            var half = profile.TileSize / 2;
            for (var maskIndex = 0; maskIndex < 2; maskIndex++)
            {
                var pixels = generated[maskIndex == 0 ? 5 : 10];
                for (var y = half - 1; y <= half; y++)
                for (var x = half - 1; x <= half; x++)
                    if (pixels[x + y * profile.TileSize].a != 0)
                        result.CentersTransparent = false;
            }
            result.Components05 = CountOpaqueComponents(generated[5], profile.TileSize);
            result.Components10 = CountOpaqueComponents(generated[10], profile.TileSize);
            for (var mask = 0; mask < generated.Length; mask++)
            {
                var expectedComponents = mask == 0 ? 0 : mask == 5 || mask == 10 ? 2 : 1;
                if (CountOpaqueComponents(generated[mask], profile.TileSize)
                    != expectedComponents)
                    result.InvalidTopologyMasks++;
            }
            result.GuidanceChangedPixels = CountGuidanceChangedPixels(profile, grass);
            return result;
        }

        private static int CountGuidanceChangedPixels(DualGridPixelTerrainProfile profile,
            PixelSource grass)
        {
            if (profile.TextureGuidancePixels <= 0 || !grass.HasGuidanceRange) return 0;
            var changed = 0;
            var size = profile.TileSize;
            for (var mask = 1; mask < DualGridMaskUtility.MaskCount - 1; mask++)
            {
                var guided = BuildLandMask(profile, grass, (DualGridMask)mask, true);
                var unguided = BuildLandMask(profile, grass, (DualGridMask)mask, false);
                for (var y = 1; y < size - 1; y++)
                for (var x = 1; x < size - 1; x++)
                {
                    var index = x + y * size;
                    if (guided[index] != unguided[index]) changed++;
                }
            }
            return changed;
        }

        private static void ValidateCompatibleBorders(Color32[][] pixels, int size,
            ref PixelValidation result)
        {
            for (var leftMask = 0; leftMask < DualGridMaskUtility.MaskCount; leftMask++)
            for (var rightMask = 0; rightMask < DualGridMaskUtility.MaskCount; rightMask++)
            {
                if (HasCorner((DualGridMask)leftMask, DualGridMask.NorthEast)
                        != HasCorner((DualGridMask)rightMask, DualGridMask.NorthWest)
                    || HasCorner((DualGridMask)leftMask, DualGridMask.SouthEast)
                        != HasCorner((DualGridMask)rightMask, DualGridMask.SouthWest))
                    continue;
                result.HorizontalPairs++;
                for (var y = 0; y < size; y++)
                {
                    var left = pixels[leftMask][size - 1 + y * size];
                    var right = pixels[rightMask][y * size];
                    result.MaximumRgbaDifference = Mathf.Max(result.MaximumRgbaDifference,
                        MaximumComponentDifference(left, right));
                }
            }

            for (var upperMask = 0; upperMask < DualGridMaskUtility.MaskCount; upperMask++)
            for (var lowerMask = 0; lowerMask < DualGridMaskUtility.MaskCount; lowerMask++)
            {
                if (HasCorner((DualGridMask)upperMask, DualGridMask.SouthWest)
                        != HasCorner((DualGridMask)lowerMask, DualGridMask.NorthWest)
                    || HasCorner((DualGridMask)upperMask, DualGridMask.SouthEast)
                        != HasCorner((DualGridMask)lowerMask, DualGridMask.NorthEast))
                    continue;
                result.VerticalPairs++;
                for (var x = 0; x < size; x++)
                {
                    var upper = pixels[upperMask][x];
                    var lower = pixels[lowerMask][x + (size - 1) * size];
                    result.MaximumRgbaDifference = Mathf.Max(result.MaximumRgbaDifference,
                        MaximumComponentDifference(upper, lower));
                }
            }
        }

        private static int CountOpaqueComponents(Color32[] pixels, int size)
        {
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();
            var components = 0;
            for (var index = 0; index < pixels.Length; index++)
            {
                if (visited[index] || pixels[index].a == 0) continue;
                components++;
                visited[index] = true;
                queue.Enqueue(index);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var x = current % size;
                    var y = current / size;
                    EnqueueOpaque(pixels, visited, queue, size, x - 1, y);
                    EnqueueOpaque(pixels, visited, queue, size, x + 1, y);
                    EnqueueOpaque(pixels, visited, queue, size, x, y - 1);
                    EnqueueOpaque(pixels, visited, queue, size, x, y + 1);
                }
            }
            return components;
        }

        private static void EnqueueOpaque(Color32[] pixels, bool[] visited, Queue<int> queue,
            int size, int x, int y)
        {
            if (x < 0 || x >= size || y < 0 || y >= size) return;
            var index = x + y * size;
            if (visited[index] || pixels[index].a == 0) return;
            visited[index] = true;
            queue.Enqueue(index);
        }

        private static void ValidateImportersAndTileSet(DualGridPixelTerrainProfile profile,
            DualGridTileSet tileSet)
        {
            if (tileSet == null)
                throw new InvalidOperationException(
                    "Generated pixel terrain TileSet is missing for " + profile.TerrainId + ".");
            if (!tileSet.Validate(out var reason))
                throw new InvalidOperationException(
                    "Generated pixel terrain TileSet is invalid for " + profile.TerrainId
                    + ": " + reason);

            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var texturePath = GetTexturePath(profile, mask);
                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                var importerSettings = new TextureImporterSettings();
                if (importer != null) importer.ReadTextureSettings(importerSettings);
                if (importer == null
                    || importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.filterMode != FilterMode.Point
                    || importer.mipmapEnabled
                    || importer.wrapMode != TextureWrapMode.Clamp
                    || importer.textureCompression != TextureImporterCompression.Uncompressed
                    || importerSettings.spriteMeshType != SpriteMeshType.FullRect
                    || Mathf.Abs(importer.spritePixelsPerUnit - profile.TileSize) > .001f)
                    throw new InvalidOperationException(
                        "Generated pixel terrain importer is not pixel-safe: " + texturePath);

                var expectedTilePath = GetTileAssetPath(profile, mask);
                var tile = tileSet.GetTile((DualGridMask)mask);
                if (tile == null || AssetDatabase.GetAssetPath(tile) != expectedTilePath)
                    throw new InvalidOperationException(
                        "Generated pixel terrain TileSet slot " + mask
                        + " is not stable for " + profile.TerrainId + ".");
            }
        }

        private static void WriteValidationReport(string projectRoot,
            DualGridPixelTerrainProfile profile, string grassSource, string soilSource,
            string tileSetPath, string profileHash, string pixelHash,
            PixelValidation validation, bool repeatDeterministic, string atlasEvidencePath,
            string validationEvidencePath)
        {
            var report = new PixelBakeValidationReport
            {
                grassSource = grassSource,
                soilSource = soilSource,
                profile = AssetDatabase.GetAssetPath(profile),
                sourceOrigin = profile.SourceOrigin.ToString(),
                sourceLayout = profile.SourceLayout.ToString(),
                bakerVersion = BakerVersion,
                profileHash = profileHash,
                pixelHash = pixelHash,
                tileSet = tileSetPath,
                tileSize = profile.TileSize,
                outlinePixels = profile.OutlinePixels,
                soilRimPixels = profile.SoilRimPixels,
                textureGuidancePixels = profile.TextureGuidancePixels,
                solidOutlineActive = profile.OutlinePixels > 0,
                sourceGuidanceAvailable = validation.SourceGuidanceAvailable,
                textureGuidedChangedPixels = validation.GuidanceChangedPixels,
                generatedMasks = DualGridMaskUtility.MaskCount,
                horizontalCompatiblePairs = validation.HorizontalPairs,
                verticalCompatiblePairs = validation.VerticalPairs,
                maximumRgbaDifference = validation.MaximumRgbaDifference,
                invalidAlphaPixels = validation.InvalidAlphaPixels,
                invalidPalettePixels = validation.InvalidPalettePixels,
                emptyMaskTransparent = validation.EmptyMaskTransparent,
                fullMaskOpaque = validation.FullMaskOpaque,
                oppositeCornerCentersTransparent = validation.CentersTransparent,
                oppositeCornerComponentCount05 = validation.Components05,
                oppositeCornerComponentCount10 = validation.Components10,
                invalidTopologyMasks = validation.InvalidTopologyMasks,
                deterministicRepeatResult = repeatDeterministic ? "pass" : "fail",
                importerResult = "pass",
                tileSetResult = "pass",
                atlasPreview = atlasEvidencePath,
                result = validation.Passed && repeatDeterministic ? "pass" : "fail",
            };
            var absolutePath = ToAbsolutePath(projectRoot, validationEvidencePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, JsonUtility.ToJson(report, true));
        }

        private static string DescribeValidationFailure(PixelValidation validation)
        {
            return "Pixel Dual-Grid validation failed: horizontalPairs="
                + validation.HorizontalPairs + ", verticalPairs=" + validation.VerticalPairs
                + ", maxRgba=" + validation.MaximumRgbaDifference
                + ", invalidAlpha=" + validation.InvalidAlphaPixels
                + ", invalidPalette=" + validation.InvalidPalettePixels
                + ", empty=" + validation.EmptyMaskTransparent
                + ", full=" + validation.FullMaskOpaque
                + ", centers=" + validation.CentersTransparent
                + ", components05=" + validation.Components05
                + ", components10=" + validation.Components10
                + ", invalidTopologyMasks=" + validation.InvalidTopologyMasks
                + ", guidanceExpected=" + validation.GuidanceExpected
                + ", guidanceChanged=" + validation.GuidanceChangedPixels + ".";
        }

        private static void AddPalette(HashSet<uint> palette, Color32[] colors)
        {
            for (var index = 0; index < colors.Length; index++)
            {
                var color = colors[index];
                color.a = 255;
                palette.Add(ColorKey(color));
            }
        }

        private static uint ColorKey(Color32 color)
        {
            return ((uint)color.r << 24) | ((uint)color.g << 16)
                | ((uint)color.b << 8) | color.a;
        }

        private static int MaximumComponentDifference(Color32 left, Color32 right)
        {
            return Mathf.Max(Mathf.Abs(left.r - right.r), Mathf.Abs(left.g - right.g),
                Mathf.Abs(left.b - right.b), Mathf.Abs(left.a - right.a));
        }

        private static string ComputeProfileHash(DualGridPixelTerrainProfile profile,
            string grassFile, string soilFile)
        {
            var value = BakerVersion + "|" + profile.TerrainId + "|" + profile.OutputFolder
                + "|" + profile.TileSize + "|" + profile.OutlinePixels + "|"
                + profile.SoilRimPixels + "|" + profile.TextureGuidancePixels + "|"
                + ColorKey(profile.EdgeColor) + "|"
                + profile.DeterministicSeed + "|" + ComputeFileHash(grassFile) + "|"
                + ComputeFileHash(soilFile);
            return ComputeHash(Encoding.UTF8.GetBytes(value));
        }

        private static string ComputePixelHash(Color32[][] generated)
        {
            var bytes = new byte[generated.Length * generated[0].Length * 4];
            var offset = 0;
            for (var mask = 0; mask < generated.Length; mask++)
            for (var index = 0; index < generated[mask].Length; index++)
            {
                var color = generated[mask][index];
                bytes[offset++] = color.r;
                bytes[offset++] = color.g;
                bytes[offset++] = color.b;
                bytes[offset++] = color.a;
            }
            return ComputeHash(bytes);
        }

        private static string ComputeFileHash(string path)
        {
            return ComputeHash(File.ReadAllBytes(path));
        }

        private static string ComputeHash(byte[] bytes)
        {
            using (var algorithm = SHA256.Create())
            {
                var hash = algorithm.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var index = 0; index < hash.Length; index++)
                    builder.Append(hash[index].ToString("x2"));
                return builder.ToString();
            }
        }

        private static void WriteAtlas(string projectRoot, Color32[][] masks, int size,
            string atlasEvidencePath)
        {
            var atlasSize = size * 4;
            var atlas = new Color32[atlasSize * atlasSize];
            for (var mask = 0; mask < masks.Length; mask++)
            {
                var tileX = mask % 4;
                var tileY = 3 - mask / 4;
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var atlasX = tileX * size + x;
                    var atlasY = tileY * size + y;
                    atlas[atlasX + atlasY * atlasSize] = masks[mask][x + y * size];
                }
            }
            WritePng(projectRoot, atlasEvidencePath, atlasSize, atlasSize, atlas);
        }

        private static void WritePng(string projectRoot, string assetPath, int width,
            int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
            };
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                var absolutePath = ToAbsolutePath(projectRoot, assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static PixelSource LoadPixelSource(string absolutePath, string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
            {
                name = name,
                filterMode = FilterMode.Point,
            };
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePath), false))
                    throw new InvalidOperationException("Could not decode pixel source " + absolutePath);
                return new PixelSource(texture.width, texture.height, texture.GetPixels32());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ValidateOpaqueSource(PixelSource source, string label,
            DualGridPixelSourceOrigin origin)
        {
            for (var index = 0; index < source.Pixels.Length; index++)
                if (source.Pixels[index].a != 255)
                    throw new InvalidOperationException(
                        SourceLabel(origin) + " " + label + " source must be fully opaque; pixel "
                        + index + " has alpha " + source.Pixels[index].a + ".");
        }

        private static void RequireAuthoringSource(string absolutePath, string label,
            DualGridPixelSourceOrigin origin)
        {
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException(
                    SourceLabel(origin) + " " + label
                    + " source is missing. The pixel baker does not draw fallback art.",
                    absolutePath);
        }

        private static string SourceLabel(DualGridPixelSourceOrigin origin)
        {
            return origin == DualGridPixelSourceOrigin.Imagegen ? "Imagegen" : "Manual";
        }

        private static void ConfigureSourceImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Authoring source importer is unavailable for " + path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        private static void ConfigureGeneratedSpriteImporter(string path, int tileSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("TextureImporter is unavailable for " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = tileSize;
            var importerSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(importerSettings);
            importerSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(importerSettings);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = tileSize;
            importer.SaveAndReimport();
        }

        private static Tile ConfigureTileAsset(string tilePath, string texturePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (sprite == null)
                throw new InvalidOperationException("Generated Sprite is unavailable for " + texturePath);
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.transform = Matrix4x4.identity;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureAssetFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static string GetTexturePath(DualGridPixelTerrainProfile profile, int mask)
        {
            return profile.OutputFolder + "/Mask-" + mask.ToString("00") + ".png";
        }

        private static string GetTileAssetPath(DualGridPixelTerrainProfile profile, int mask)
        {
            return profile.OutputFolder + "/Mask-" + mask.ToString("00") + ".asset";
        }

        private static string GetTileSetPath(DualGridPixelTerrainProfile profile)
        {
            return GetTileSetAssetPath(profile);
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            var normalized = assetFolder.Replace('\\', '/').TrimEnd('/');
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

        private static string ToAbsolutePath(string projectRoot, string projectPath)
        {
            return Path.GetFullPath(Path.Combine(projectRoot,
                projectPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static int CountBits(int value)
        {
            var count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }
            return count;
        }

        private static bool HasCorner(DualGridMask mask, DualGridMask corner)
        {
            return (mask & corner) != 0;
        }

        private static DualGridMask FirstSetCorner(DualGridMask mask)
        {
            if (HasCorner(mask, DualGridMask.NorthWest)) return DualGridMask.NorthWest;
            if (HasCorner(mask, DualGridMask.NorthEast)) return DualGridMask.NorthEast;
            if (HasCorner(mask, DualGridMask.SouthEast)) return DualGridMask.SouthEast;
            return DualGridMask.SouthWest;
        }

        private static DualGridMask FirstMissingCorner(DualGridMask mask)
        {
            if (!HasCorner(mask, DualGridMask.NorthWest)) return DualGridMask.NorthWest;
            if (!HasCorner(mask, DualGridMask.NorthEast)) return DualGridMask.NorthEast;
            if (!HasCorner(mask, DualGridMask.SouthEast)) return DualGridMask.SouthEast;
            return DualGridMask.SouthWest;
        }

        private static int PositiveMod(int value, int divisor)
        {
            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }
}
