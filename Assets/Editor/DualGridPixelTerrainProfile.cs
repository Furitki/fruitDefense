using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public enum DualGridPixelSourceLayout
    {
        GrassAndSoil = 0,
        Single = 1,
    }

    public enum DualGridPixelSourceOrigin
    {
        Imagegen = 0,
        Manual = 1,
    }

    [CreateAssetMenu(fileName = "DualGridPixelTerrainProfile",
        menuName = "Fruit Defense/Dual-Grid Pixel Terrain Profile")]
    public sealed class DualGridPixelTerrainProfile : ScriptableObject
    {
        [Header("Authoring sources")]
        [SerializeField] private DualGridPixelSourceOrigin sourceOrigin =
            DualGridPixelSourceOrigin.Imagegen;
        [SerializeField] private DualGridPixelSourceLayout sourceLayout =
            DualGridPixelSourceLayout.GrassAndSoil;
        [SerializeField] private Texture2D grassTexture;
        [SerializeField] private Texture2D soilTexture;
        [SerializeField] private string terrainId = "PixelGrass";
        [SerializeField] private string outputFolder =
            "Assets/DualGridDemo/PixelGrass/Generated";

        [Header("Native pixel rasterization")]
        [SerializeField, Range(8, 128)] private int tileSize = 32;
        [SerializeField, Range(0, 8)] private int outlinePixels;
        [SerializeField, Range(1, 12)] private int soilRimPixels = 2;
        [SerializeField, Range(0, 4)] private int textureGuidancePixels = 2;
        [SerializeField] private Color32 edgeColor = new Color32(65, 42, 27, 255);
        [SerializeField] private int deterministicSeed = 9137;

        public DualGridPixelSourceOrigin SourceOrigin { get { return sourceOrigin; } }
        public DualGridPixelSourceLayout SourceLayout { get { return sourceLayout; } }
        public Texture2D GrassTexture { get { return grassTexture; } }
        public Texture2D SoilTexture
        {
            get { return sourceLayout == DualGridPixelSourceLayout.Single ? grassTexture : soilTexture; }
        }
        public Texture2D AuthoredSoilTexture { get { return soilTexture; } }
        public string TerrainId { get { return terrainId; } }
        public string OutputFolder { get { return outputFolder; } }
        public int TileSize { get { return tileSize; } }
        public int OutlinePixels { get { return outlinePixels; } }
        public int SoilRimPixels { get { return soilRimPixels; } }
        public int TextureGuidancePixels { get { return textureGuidancePixels; } }
        public Color32 EdgeColor { get { return edgeColor; } }
        public int DeterministicSeed { get { return deterministicSeed; } }

        public void ConfigureDefaults(Texture2D grass, Texture2D soil, string generatedFolder)
        {
            sourceOrigin = DualGridPixelSourceOrigin.Imagegen;
            sourceLayout = DualGridPixelSourceLayout.GrassAndSoil;
            grassTexture = grass;
            soilTexture = soil;
            terrainId = "PixelGrass";
            outputFolder = generatedFolder;
            tileSize = 32;
            outlinePixels = 0;
            soilRimPixels = 2;
            textureGuidancePixels = 2;
            edgeColor = new Color32(65, 42, 27, 255);
            deterministicSeed = 9137;
        }

        public void Configure(DualGridPixelSourceOrigin origin,
            DualGridPixelSourceLayout layout, Texture2D grass, Texture2D soil,
            string id, string generatedFolder, int nativeTileSize, int outlineWidth,
            int soilRimWidth, Color32 outlineColor, int seed)
        {
            Configure(origin, layout, grass, soil, id, generatedFolder, nativeTileSize,
                outlineWidth, soilRimWidth, outlineColor, seed, 2);
        }

        public void Configure(DualGridPixelSourceOrigin origin,
            DualGridPixelSourceLayout layout, Texture2D grass, Texture2D soil,
            string id, string generatedFolder, int nativeTileSize, int outlineWidth,
            int soilRimWidth, Color32 outlineColor, int seed, int guidanceWidth)
        {
            sourceOrigin = origin;
            sourceLayout = layout;
            grassTexture = grass;
            soilTexture = layout == DualGridPixelSourceLayout.Single ? null : soil;
            terrainId = id == null ? string.Empty : id.Trim();
            outputFolder = generatedFolder == null
                ? string.Empty
                : generatedFolder.Replace('\\', '/').TrimEnd('/');
            tileSize = nativeTileSize;
            outlinePixels = outlineWidth;
            soilRimPixels = soilRimWidth;
            textureGuidancePixels = guidanceWidth;
            edgeColor = outlineColor;
            deterministicSeed = seed;
            OnValidate();
        }

        public bool Validate(out string reason)
        {
            if (grassTexture == null)
            {
                reason = "Grass source texture is required.";
                return false;
            }
            if (sourceLayout == DualGridPixelSourceLayout.GrassAndSoil && soilTexture == null)
            {
                reason = "Soil source texture is required in grass-and-soil mode.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(outputFolder)
                || !outputFolder.StartsWith("Assets/", StringComparison.Ordinal))
            {
                reason = "Output folder must be project-relative and start with Assets/.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(terrainId)
                || terrainId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                reason = "Terrain id must be a valid asset filename stem.";
                return false;
            }
            if (tileSize < 8 || tileSize % 2 != 0)
            {
                reason = "Pixel tile size must be even and at least 8 pixels.";
                return false;
            }
            if (outlinePixels < 0)
            {
                reason = "Pixel outline cannot be negative.";
                return false;
            }
            if (soilRimPixels < 1)
            {
                reason = "Pixel soil rim must be at least 1 pixel.";
                return false;
            }
            if (textureGuidancePixels < 0 || textureGuidancePixels > 4)
            {
                reason = "Pixel texture guidance must be between 0 and 4 pixels.";
                return false;
            }
            if (outlinePixels + soilRimPixels + 1 >= tileSize / 2)
            {
                reason = "Outline and soil rim leave no interior pixel region.";
                return false;
            }
            if (edgeColor.a != 255)
            {
                reason = "Pixel edge color must be fully opaque.";
                return false;
            }

            reason = "ok";
            return true;
        }

        private void OnValidate()
        {
            tileSize = Mathf.Clamp(tileSize, 8, 128);
            if (tileSize % 2 != 0) tileSize++;
            outlinePixels = Mathf.Max(0, outlinePixels);
            soilRimPixels = Mathf.Max(1, soilRimPixels);
            textureGuidancePixels = Mathf.Clamp(textureGuidancePixels, 0, 4);
            edgeColor.a = 255;
        }
    }

    [CustomEditor(typeof(DualGridPixelTerrainProfile))]
    public sealed class DualGridPixelTerrainProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            var profile = (DualGridPixelTerrainProfile)target;
            if (!profile.Validate(out var reason))
                EditorGUILayout.HelpBox(reason, MessageType.Error);
            else
                EditorGUILayout.HelpBox(
                    profile.SourceOrigin == DualGridPixelSourceOrigin.Imagegen
                        ? "Source art is imagegen-owned. The baker only derives native-grid masks, Tiles, and evidence."
                        : "Source art is author-owned. The baker never copies or overwrites it.",
                    MessageType.Info);

            using (new EditorGUI.DisabledScope(!profile.Validate(out reason)))
            {
                if (GUILayout.Button("Bake sixteen pixel Dual-Grid terrain tiles"))
                    DualGridPixelTileSetGenerator.Bake(profile);
            }
        }
    }
}
