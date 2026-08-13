using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    [CreateAssetMenu(fileName = "DualGridTerrainBakeProfile",
        menuName = "Fruit Defense/Dual-Grid Terrain Bake Profile")]
    public sealed class DualGridTerrainBakeProfile : ScriptableObject
    {
        [Header("Authoring sources")]
        [SerializeField] private Texture2D grassTexture;
        [SerializeField] private Texture2D soilTexture;
        [SerializeField] private string terrainId = "CartoonGrass";
        [SerializeField] private string outputFolder = "Assets/DualGridDemo/CartoonGrass";

        [Header("Rasterization")]
        [SerializeField, Min(64)] private int tileSize = 512;
        [SerializeField, Range(4, 8)] private int supersampleScale = 4;
        [SerializeField, Range(.5f, 2f)] private float alphaAntialiasPixels = 1.1f;

        [Header("Terrain edge in output pixels")]
        [SerializeField, Range(1f, 24f)] private float exposedSoilPixels = 9f;
        [SerializeField, Range(1f, 20f)] private float grassBlendPixels = 6f;
        [SerializeField, Range(0f, 40f)] private float broadIrregularityPixels = 20f;
        [SerializeField, Range(0f, 16f)] private float fineIrregularityPixels = 5f;
        [SerializeField, Range(0f, 8f)] private float grassBladeExtensionPixels = 3.25f;
        [SerializeField, Range(0f, .25f)] private float oppositeCornerSeparation = .14f;
        [SerializeField] private int deterministicSeed = 7319;

        public Texture2D GrassTexture { get { return grassTexture; } }
        public Texture2D SoilTexture { get { return soilTexture; } }
        public string TerrainId { get { return terrainId; } }
        public string OutputFolder { get { return outputFolder; } }
        public int TileSize { get { return tileSize; } }
        public int SupersampleScale { get { return supersampleScale; } }
        public float AlphaAntialiasPixels { get { return alphaAntialiasPixels; } }
        public float ExposedSoilPixels { get { return exposedSoilPixels; } }
        public float GrassBlendPixels { get { return grassBlendPixels; } }
        public float BroadIrregularityPixels { get { return broadIrregularityPixels; } }
        public float FineIrregularityPixels { get { return fineIrregularityPixels; } }
        public float GrassBladeExtensionPixels { get { return grassBladeExtensionPixels; } }
        public float OppositeCornerSeparation { get { return oppositeCornerSeparation; } }
        public int DeterministicSeed { get { return deterministicSeed; } }

        public void ConfigureDefaults(Texture2D grass, Texture2D soil, string generatedFolder)
        {
            grassTexture = grass;
            soilTexture = soil;
            terrainId = "CartoonGrass";
            outputFolder = generatedFolder;
            tileSize = 512;
            supersampleScale = 4;
            alphaAntialiasPixels = 1.1f;
            exposedSoilPixels = 9f;
            grassBlendPixels = 6f;
            broadIrregularityPixels = 20f;
            fineIrregularityPixels = 5f;
            grassBladeExtensionPixels = 3.25f;
            oppositeCornerSeparation = .14f;
            deterministicSeed = 7319;
        }

        public bool Validate(out string reason)
        {
            if (grassTexture == null)
            {
                reason = "Grass source texture is required.";
                return false;
            }
            if (soilTexture == null)
            {
                reason = "Soil source texture is required.";
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
            if (tileSize < 64)
            {
                reason = "Tile size must be at least 64 pixels.";
                return false;
            }
            if (supersampleScale < 4)
            {
                reason = "Production Dual-Grid baking requires at least four-times supersampling.";
                return false;
            }

            reason = "ok";
            return true;
        }

        private void OnValidate()
        {
            tileSize = Mathf.Max(64, tileSize);
            supersampleScale = Mathf.Clamp(supersampleScale, 4, 8);
            alphaAntialiasPixels = Mathf.Clamp(alphaAntialiasPixels, .5f, 2f);
            exposedSoilPixels = Mathf.Max(1f, exposedSoilPixels);
            grassBlendPixels = Mathf.Max(1f, grassBlendPixels);
            broadIrregularityPixels = Mathf.Max(0f, broadIrregularityPixels);
            fineIrregularityPixels = Mathf.Max(0f, fineIrregularityPixels);
            grassBladeExtensionPixels = Mathf.Max(0f, grassBladeExtensionPixels);
            oppositeCornerSeparation = Mathf.Clamp(oppositeCornerSeparation, 0f, .25f);
        }
    }

    [CustomEditor(typeof(DualGridTerrainBakeProfile))]
    public sealed class DualGridTerrainBakeProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            var profile = (DualGridTerrainBakeProfile)target;
            if (!profile.Validate(out var reason))
                EditorGUILayout.HelpBox(reason, MessageType.Error);
            else
                EditorGUILayout.HelpBox(
                    "Generated PNG, Tile, TileSet, preview, and seam evidence are derived from this profile.",
                    MessageType.Info);

            using (new EditorGUI.DisabledScope(!profile.Validate(out reason)))
            {
                if (GUILayout.Button("Bake sixteen Dual-Grid terrain tiles"))
                    DualGridTextureTileSetGenerator.Bake(profile);
            }
        }
    }
}
