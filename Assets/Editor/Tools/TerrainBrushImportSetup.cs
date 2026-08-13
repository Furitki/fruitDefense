using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public static class TerrainBrushImportSetup
    {
        internal const string AssetRoot = "Assets/LayeredTerrain/CompositeBrushes";
        internal const string DescriptorFileName = "BrushImport.json";
        internal const string DefinitionAssetSuffix = "Brush.asset";
        private const string DescriptorSchemaV1 = "fruit-defense.terrain-brush-import.v1";
        private const string DescriptorSchemaV2 = "fruit-defense.terrain-brush-import.v2";
        private const string PipelineSchema = "fruit-defense.dual-grid-art-pipeline.v3";

        [Serializable]
        internal sealed class BrushImportDescriptor
        {
            public string schema;
            public string profileId;
            public string brushId;
            public string assetFolderName;
            public string displayName;
            public string landformDisplayName;
            public string baseDisplayName;
            public string landformSurfaceId;
            public string baseSurfaceId;
            public string contourStyleId;
            public string edgeStyleId;
            public int foregroundMask = 15;
            public int backgroundMask;
            public int runtimeTileSize = 32;
            public string runtimeMaskDirectory;
            public string sourceManifest;
        }

        [Serializable]
        private sealed class PipelineManifestHeader
        {
            public string schema;
            public string profileId;
            public int runtimeMaskCount;
            public int runtimeTileSize;
            public string runtimeMaskDirectory;
        }

        [MenuItem("Fruit Defense/地图工具/导入 Dual-Grid 笔刷包...", priority = 30)]
        public static void ImportCandidateFromMenu()
        {
            var selected = EditorUtility.OpenFolderPanel("选择管线候选或 candidate 目录",
                Path.Combine(ProjectRoot(), "Builds", "Evidence"), string.Empty);
            if (string.IsNullOrWhiteSpace(selected)) return;
            try
            {
                var definition = ImportCandidate(ResolveCandidateRoot(selected));
                EditorGUIUtility.PingObject(definition);
                Selection.activeObject = definition;
                Debug.Log("FRUIT_DEFENSE_TERRAIN_BRUSH_IMPORTED id=" + definition.BrushId
                    + " asset=" + AssetDatabase.GetAssetPath(definition));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("笔刷导入失败", exception.Message, "确定");
            }
        }

        internal static TerrainBrushDefinition ImportCandidate(string candidateRoot)
        {
            var sourceRoot = Path.GetFullPath(candidateRoot ?? string.Empty);
            var descriptorPath = ContainedPath(sourceRoot, DescriptorFileName);
            if (!File.Exists(descriptorPath))
                throw new FileNotFoundException("BrushImport.json 不存在。请先运行管线 Package 阶段。",
                    descriptorPath);
            var descriptor = JsonUtility.FromJson<BrushImportDescriptor>(
                File.ReadAllText(descriptorPath));
            ValidateDescriptor(descriptor);

            var manifestPath = ContainedPath(sourceRoot, descriptor.sourceManifest);
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("管线 manifest 不存在。", manifestPath);
            var manifest = JsonUtility.FromJson<PipelineManifestHeader>(
                File.ReadAllText(manifestPath));
            ValidateManifest(descriptor, manifest, manifestPath);

            var runtimeRoot = ContainedPath(sourceRoot, descriptor.runtimeMaskDirectory);
            var sourceMasks = Enumerable.Range(0, 16)
                .Select(mask => ContainedPath(runtimeRoot, MaskFileName(mask))).ToArray();
            var missing = sourceMasks.Where(path => !File.Exists(path)).ToArray();
            if (missing.Length > 0)
                throw new FileNotFoundException("笔刷必须包含完整 Mask-00..15："
                    + string.Join(", ", missing.Select(Path.GetFileName)));

            var assetBrushRoot = AssetRoot + "/" + descriptor.assetFolderName;
            var runtimeAssetRoot = assetBrushRoot + "/" + descriptor.runtimeMaskDirectory;
            var definitionPath = assetBrushRoot + "/" + descriptor.assetFolderName
                + DefinitionAssetSuffix;
            var sameId = TerrainBrushRegistry.FindAll().FirstOrDefault(value =>
                string.Equals(value.BrushId, descriptor.brushId, StringComparison.Ordinal));
            if (sameId != null && !string.Equals(AssetDatabase.GetAssetPath(sameId),
                    definitionPath, StringComparison.Ordinal))
                throw new InvalidOperationException("brushId 已由另一生产目录注册："
                    + AssetDatabase.GetAssetPath(sameId));
            var definition = AssetDatabase.LoadAssetAtPath<TerrainBrushDefinition>(definitionPath);
            if (definition != null && !string.IsNullOrEmpty(definition.BrushId)
                && !string.Equals(definition.BrushId, descriptor.brushId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("目标生产目录已属于另一 brushId："
                    + definition.BrushId);
            EnsureFolder(runtimeAssetRoot);
            var projectRoot = ProjectRoot();
            for (var mask = 0; mask < 16; mask++)
                File.Copy(sourceMasks[mask], AbsoluteAssetPath(projectRoot,
                    runtimeAssetRoot + "/" + MaskFileName(mask)), true);
            File.Copy(manifestPath, AbsoluteAssetPath(projectRoot,
                assetBrushRoot + "/SourceManifest.json"), true);
            File.Copy(descriptorPath, AbsoluteAssetPath(projectRoot,
                assetBrushRoot + "/" + DescriptorFileName), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var tileSet = BuildTileSet(descriptor, assetBrushRoot, runtimeAssetRoot);
            var reverseLandform = BuildComplementedTileSet(
                assetBrushRoot + "/" + descriptor.assetFolderName
                    + "ReverseLandformTileSet.asset", tileSet);
            definition = AssetDatabase.LoadAssetAtPath<TerrainBrushDefinition>(definitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<TerrainBrushDefinition>();
                definition.name = descriptor.displayName;
                AssetDatabase.CreateAsset(definition, definitionPath);
            }
            definition.name = descriptor.displayName;
            var foregroundPath = runtimeAssetRoot + "/"
                + MaskFileName(descriptor.foregroundMask);
            var backgroundPath = runtimeAssetRoot + "/"
                + MaskFileName(descriptor.backgroundMask);
            definition.Configure(descriptor.brushId, descriptor.displayName,
                descriptor.profileId, descriptor.landformDisplayName,
                descriptor.baseDisplayName, descriptor.landformSurfaceId,
                descriptor.baseSurfaceId, descriptor.contourStyleId,
                descriptor.edgeStyleId, descriptor.foregroundMask,
                descriptor.backgroundMask, descriptor.runtimeTileSize, tileSet,
                reverseLandform, tileSet.GetTile((DualGridMask)descriptor.foregroundMask),
                tileSet.GetTile((DualGridMask)descriptor.backgroundMask),
                RequireAsset<Texture2D>(foregroundPath),
                RequireAsset<Texture2D>(backgroundPath),
                RequireAsset<TextAsset>(assetBrushRoot + "/SourceManifest.json"), true);
            if (!definition.Validate(out var reason))
                throw new InvalidOperationException("导入后的笔刷定义无效：" + reason);
            EditorUtility.SetDirty(definition);
            RemoveObsoleteRuntimeFolders(assetBrushRoot, runtimeAssetRoot);
            AssetDatabase.SaveAssets();

            TerrainBrushRegistry.Invalidate();
            var palette = LayeredTerrainArtSetup.RefreshPaletteFromRegisteredBrushes();
            if (!TerrainBrushRegistry.IsAvailable(definition, palette, out reason))
                throw new InvalidOperationException("笔刷未能完成 Palette 注册：" + reason);
            AssetDatabase.SaveAssets();
            return definition;
        }

        internal static void EnsureComplementedViewsForRegisteredBrushes()
        {
            var changed = false;
            foreach (var definition in TerrainBrushRegistry.FindAll())
            {
                if (definition == null || definition.CompositeTileSet == null) continue;
                var reversePath = ComplementedTileSetPath(definition);
                var reverse = BuildComplementedTileSet(reversePath,
                    definition.CompositeTileSet);
                changed = true;
                if (definition.ReverseLandformTileSet == reverse
                    && definition.ForegroundBaseTile != null
                    && definition.BackgroundBaseTile != null) continue;
                definition.Configure(definition.BrushId, definition.DisplayName,
                    definition.SourceProfileId, definition.LandformDisplayName,
                    definition.BaseDisplayName, definition.LandformSurfaceId,
                    definition.BaseSurfaceId, definition.ContourStyleId,
                    definition.EdgeStyleId, definition.ForegroundMask,
                    definition.BackgroundMask, definition.RuntimeTileSize,
                    definition.CompositeTileSet, reverse,
                    definition.ForegroundBaseTile
                        ?? definition.CompositeTileSet.GetTile(
                            (DualGridMask)definition.ForegroundMask),
                    definition.BackgroundBaseTile
                        ?? definition.CompositeTileSet.GetTile(
                            (DualGridMask)definition.BackgroundMask),
                    definition.ForegroundTexture, definition.BackgroundTexture,
                    definition.SourceManifest, true);
                EditorUtility.SetDirty(definition);
            }
            if (!changed) return;
            AssetDatabase.SaveAssets();
            TerrainBrushRegistry.Invalidate();
        }

        internal static string ResolveCandidateRoot(string selectedFolder)
        {
            var root = Path.GetFullPath(selectedFolder ?? string.Empty);
            if (File.Exists(Path.Combine(root, DescriptorFileName))) return root;
            var candidate = Path.Combine(root, "candidate");
            if (File.Exists(Path.Combine(candidate, DescriptorFileName))) return candidate;
            throw new FileNotFoundException("所选目录及其 candidate 子目录都没有 BrushImport.json。",
                Path.Combine(root, DescriptorFileName));
        }

        private static DualGridTileSet BuildTileSet(BrushImportDescriptor descriptor,
            string assetBrushRoot, string runtimeAssetRoot)
        {
            var tileSetPath = assetBrushRoot + "/" + descriptor.assetFolderName
                + "CompositeTileSet.asset";
            var tileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(tileSetPath);
            if (tileSet == null)
            {
                tileSet = ScriptableObject.CreateInstance<DualGridTileSet>();
                tileSet.name = descriptor.displayName + " Composite TileSet";
                AssetDatabase.CreateAsset(tileSet, tileSetPath);
            }
            for (var mask = 0; mask < 16; mask++)
            {
                var texturePath = runtimeAssetRoot + "/" + MaskFileName(mask);
                ConfigureTexture(texturePath, mask == descriptor.foregroundMask
                    || mask == descriptor.backgroundMask, descriptor.runtimeTileSize);
                var sprite = RequireAsset<Sprite>(texturePath);
                var tilePath = assetBrushRoot + "/Mask-" + mask.ToString("00") + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.name = descriptor.brushId + " Mask-" + mask.ToString("00");
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.transform = Matrix4x4.identity;
                tile.flags = TileFlags.LockAll;
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                tileSet.SetTile((DualGridMask)mask, tile);
            }
            EditorUtility.SetDirty(tileSet);
            AssetDatabase.SaveAssets();
            if (!tileSet.Validate(out var reason))
                throw new InvalidOperationException("导入后的 TileSet 无效：" + reason);
            return tileSet;
        }

        internal static DualGridTileSet BuildComplementedTileSet(string assetPath,
            DualGridTileSet primary)
        {
            if (primary == null) throw new ArgumentNullException(nameof(primary));
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var tileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(assetPath);
            if (tileSet == null)
            {
                tileSet = ScriptableObject.CreateInstance<DualGridTileSet>();
                tileSet.name = assetName;
                AssetDatabase.CreateAsset(tileSet, assetPath);
            }
            tileSet.name = assetName;
            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
                tileSet.SetTile((DualGridMask)mask, primary.GetTile(
                    DualGridMaskUtility.Complement((DualGridMask)mask)));
            EditorUtility.SetDirty(tileSet);
            if (!tileSet.Validate(out var reason))
                throw new InvalidOperationException(
                    "Complemented terrain brush TileSet is invalid: " + reason);
            return tileSet;
        }

        private static string ComplementedTileSetPath(TerrainBrushDefinition definition)
        {
            var definitionPath = AssetDatabase.GetAssetPath(definition);
            var folder = Path.GetDirectoryName(definitionPath).Replace('\\', '/');
            var stem = Path.GetFileNameWithoutExtension(definitionPath);
            if (stem.EndsWith("Brush", StringComparison.Ordinal))
                stem = stem.Substring(0, stem.Length - "Brush".Length);
            return folder + "/" + stem + "ReverseLandformTileSet.asset";
        }

        private static void ConfigureTexture(string path, bool repeatAsEndpoint,
            int runtimeTileSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("无法读取贴图导入器：" + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = runtimeTileSize;
            importer.spritePivot = new Vector2(.5f, .5f);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.wrapMode = repeatAsEndpoint ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
                AssetDatabase.ImportAsset(path,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void ValidateDescriptor(BrushImportDescriptor value)
        {
            if (value == null || (value.schema != DescriptorSchemaV1
                    && value.schema != DescriptorSchemaV2))
                throw new InvalidOperationException("BrushImport.json schema 不受支持。");
            var required = new Dictionary<string, string>
            {
                { "profileId", value.profileId }, { "brushId", value.brushId },
                { "assetFolderName", value.assetFolderName }, { "displayName", value.displayName },
                { "landformDisplayName", value.landformDisplayName },
                { "baseDisplayName", value.baseDisplayName },
                { "landformSurfaceId", value.landformSurfaceId },
                { "baseSurfaceId", value.baseSurfaceId },
                { "contourStyleId", value.contourStyleId }, { "edgeStyleId", value.edgeStyleId },
                { "runtimeMaskDirectory", value.runtimeMaskDirectory },
                { "sourceManifest", value.sourceManifest },
            };
            var missing = required.Where(pair => string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => pair.Key).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException("BrushImport.json 缺少："
                    + string.Join(", ", missing));
            if (value.assetFolderName.Any(character => !char.IsLetterOrDigit(character)
                    && character != '-' && character != '_'))
                throw new InvalidOperationException("assetFolderName 只能包含字母、数字、-、_。");
            if (value.foregroundMask < 0 || value.foregroundMask > 15
                || value.backgroundMask < 0 || value.backgroundMask > 15
                || value.foregroundMask == value.backgroundMask)
                throw new InvalidOperationException("前景/背景 Mask 必须是 0..15 内的不同值。");
            if (value.runtimeTileSize != 32 && value.runtimeTileSize != 64)
                throw new InvalidOperationException("runtimeTileSize 必须是 32 或 64。");
            if (!string.Equals(value.runtimeMaskDirectory,
                    "Runtime" + value.runtimeTileSize, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "runtimeMaskDirectory 必须匹配 runtimeTileSize。");
            if (string.Equals(value.landformSurfaceId, value.baseSurfaceId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("前景与背景 Surface 不能相同。");
        }

        private static void ValidateManifest(BrushImportDescriptor descriptor,
            PipelineManifestHeader manifest, string path)
        {
            if (manifest == null || manifest.schema != PipelineSchema)
                throw new InvalidOperationException("管线 manifest schema 不受支持：" + path);
            if (manifest.profileId != descriptor.profileId)
                throw new InvalidOperationException("BrushImport 与 manifest 的 profileId 不一致。");
            if (manifest.runtimeMaskCount != 16)
                throw new InvalidOperationException("管线 manifest 必须声明 16 张 Runtime Mask。");
            var manifestRuntimeSize = manifest.runtimeTileSize <= 0
                ? 32 : manifest.runtimeTileSize;
            var manifestRuntimeDirectory = string.IsNullOrEmpty(manifest.runtimeMaskDirectory)
                ? "Runtime32" : manifest.runtimeMaskDirectory;
            if (manifestRuntimeSize != descriptor.runtimeTileSize
                || !string.Equals(manifestRuntimeDirectory,
                    descriptor.runtimeMaskDirectory, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "BrushImport.json 的运行时尺寸与管线 manifest 不一致。");
        }

        private static void RemoveObsoleteRuntimeFolders(string assetBrushRoot,
            string currentRuntimeRoot)
        {
            foreach (var folder in AssetDatabase.GetSubFolders(assetBrushRoot))
            {
                var name = Path.GetFileName(folder);
                if (!name.StartsWith("Runtime", StringComparison.Ordinal)
                    || string.Equals(folder, currentRuntimeRoot, StringComparison.Ordinal))
                    continue;
                if (!AssetDatabase.DeleteAsset(folder))
                    throw new InvalidOperationException("无法移除旧运行时目录：" + folder);
            }
        }

        private static string MaskFileName(int mask)
        {
            return "Mask-" + mask.ToString("00") + ".png";
        }

        private static string ContainedPath(string root, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
                throw new InvalidOperationException("笔刷包内路径必须是相对路径：" + relative);
            var normalizedRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var resolved = Path.GetFullPath(Path.Combine(normalizedRoot, relative));
            if (!resolved.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("笔刷包路径越界：" + relative);
            return resolved;
        }

        private static string ProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static string AbsoluteAssetPath(string projectRoot, string assetPath)
        {
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("资源导入失败：" + path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var separator = path.LastIndexOf('/');
            if (separator <= 0) throw new ArgumentException("无效资源目录：" + path);
            var parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }

    internal sealed class TerrainBrushPaintChoice
    {
        internal TerrainBrushPaintChoice(TerrainBrushDefinition definition, bool reverse)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Reverse = reverse;
        }

        internal TerrainBrushDefinition Definition { get; private set; }
        internal bool Reverse { get; private set; }
        internal string ChoiceId
        {
            get { return Definition.BrushId + (Reverse ? ".reverse" : ".forward"); }
        }
        internal string LandformDisplayName
        {
            get
            {
                return Reverse ? Definition.BaseDisplayName
                    : Definition.LandformDisplayName;
            }
        }
        internal string BaseDisplayName
        {
            get
            {
                return Reverse ? Definition.LandformDisplayName
                    : Definition.BaseDisplayName;
            }
        }
        internal string DisplayName
        {
            get
            {
                return Definition.DisplayName + " · "
                    + LandformDisplayName + " 覆盖 " + BaseDisplayName;
            }
        }
        internal LayeredTerrainPainterTool Tool
        {
            get
            {
                return Reverse ? LayeredTerrainPainterTool.BOnA
                    : LayeredTerrainPainterTool.AOnB;
            }
        }
    }

    internal static class TerrainBrushRegistry
    {
        private static TerrainBrushDefinition[] cached;

        static TerrainBrushRegistry()
        {
            EditorApplication.projectChanged += Invalidate;
        }

        internal static void Invalidate()
        {
            cached = null;
        }

        internal static IReadOnlyList<TerrainBrushDefinition> FindAll()
        {
            if (cached != null) return cached;
            cached = AssetDatabase.FindAssets("t:TerrainBrushDefinition",
                    new[] { TerrainBrushImportSetup.AssetRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<TerrainBrushDefinition>)
                .Where(value => value != null)
                .OrderBy(value => value.BrushId, StringComparer.Ordinal)
                .ToArray();
            return cached;
        }

        internal static IReadOnlyList<TerrainBrushPaintChoice> FindPaintChoices()
        {
            return FindAll()
                .OrderBy(definition => string.Equals(definition.BrushId,
                    LayeredTerrainArtSetup.OriginalBrushId, StringComparison.Ordinal) ? 0 : 1)
                .ThenBy(definition => definition.BrushId, StringComparer.Ordinal)
                .SelectMany(definition => new[]
                {
                    new TerrainBrushPaintChoice(definition, false),
                    new TerrainBrushPaintChoice(definition, true),
                })
                .ToArray();
        }

        internal static bool IsAvailable(TerrainBrushDefinition definition,
            BattlefieldTerrainPalette palette, out string reason)
        {
            if (definition == null)
            {
                reason = "没有笔刷定义。";
                return false;
            }
            if (!definition.Validate(out reason)) return false;
            if (palette == null)
            {
                reason = "没有可用的地形 Palette。";
                return false;
            }
            if (!palette.TryGetBaseTexture(definition.BaseSurfaceId, out var background)
                || background != definition.BackgroundTexture)
            {
                reason = "Palette 未绑定笔刷背景端点。";
                return false;
            }
            if (!palette.TryGetBaseTexture(definition.LandformSurfaceId, out var foreground)
                || foreground != definition.ForegroundTexture)
            {
                reason = "Palette 未绑定笔刷前景端点。";
                return false;
            }
            if (!palette.TryGetLandformTileSet(definition.LandformSurfaceId,
                    definition.ContourStyleId, out _))
            {
                reason = "Palette 缺少笔刷前景地貌。";
                return false;
            }
            if (!palette.HasExactEdgeBinding(definition.LandformSurfaceId,
                    definition.BaseSurfaceId, definition.ContourStyleId,
                    definition.EdgeStyleId)
                || !palette.TryGetEdgeTileSet(definition.LandformSurfaceId,
                    definition.BaseSurfaceId, definition.ContourStyleId,
                    definition.EdgeStyleId, out var edge)
                || edge != definition.CompositeTileSet)
            {
                reason = "Palette 未绑定笔刷组合边缘。";
                return false;
            }
            reason = "ok";
            return true;
        }

        internal static bool IsDirectionAvailable(TerrainBrushDefinition definition,
            BattlefieldTerrainPalette palette, bool reverse, out string reason)
        {
            return IsLaboratoryAvailable(definition, palette, out reason);
        }

        internal static bool IsPaintChoiceAvailable(TerrainBrushPaintChoice choice,
            BattlefieldTerrainPalette palette, out string reason)
        {
            if (choice == null)
            {
                reason = "没有可绘制的注册笔刷。";
                return false;
            }
            return IsLaboratoryAvailable(choice.Definition, palette, out reason);
        }

        internal static bool IsLaboratoryAvailable(TerrainBrushDefinition definition,
            BattlefieldTerrainPalette palette, out string reason)
        {
            if (definition == null)
            {
                reason = "没有笔刷定义。";
                return false;
            }
            if (!definition.Validate(out reason)) return false;
            if (palette == null)
            {
                reason = "没有可用的地形 Palette。";
                return false;
            }
            if (!TryResolveLaboratoryLandforms(definition, palette, out _, out _,
                    out reason)) return false;
            reason = "ok";
            return true;
        }

        internal static bool TryResolveLaboratoryLandforms(
            TerrainBrushDefinition definition, BattlefieldTerrainPalette palette,
            out DualGridTileSet foregroundLandform,
            out DualGridTileSet backgroundLandform, out string reason)
        {
            foregroundLandform = null;
            backgroundLandform = null;
            if (definition == null || palette == null)
            {
                reason = "笔刷定义与地形 Palette 均不能为空。";
                return false;
            }
            if (!palette.TryGetLandformTileSet(definition.LandformSurfaceId,
                    definition.ContourStyleId, out foregroundLandform))
            {
                reason = "Palette 缺少笔刷前景地貌。";
                return false;
            }
            if (!palette.TryGetLandformTileSet(definition.BaseSurfaceId,
                    definition.ContourStyleId, out backgroundLandform))
                backgroundLandform = definition.ReverseLandformTileSet;
            if (backgroundLandform == null)
            {
                reason = "笔刷缺少可绘制的反向地貌。";
                return false;
            }
            reason = "ok";
            return true;
        }

        internal static bool Validate(out string reason)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in FindAll())
            {
                if (!definition.Validate(out reason)) return false;
                if (!ids.Add(definition.BrushId))
                {
                    reason = "Duplicate terrain brush id: " + definition.BrushId;
                    return false;
                }
            }
            reason = "ok";
            return true;
        }
    }

    internal static class TerrainBrushLaboratoryRegistration
    {
        internal static bool TryApply(TerrainBrushDefinition definition,
            LayeredTerrainTilemap target, BattlefieldTerrainPalette palette,
            out string reason)
        {
            if (target == null)
            {
                reason = "请先选择地貌实验目标。";
                return false;
            }
            if (!TerrainBrushRegistry.IsLaboratoryAvailable(definition, palette,
                    out reason)) return false;
            if (Matches(definition, target, palette))
            {
                reason = "已使用“" + definition.DisplayName + "”。";
                return true;
            }
            if (!palette.TryGetLandformTileSet(definition.LandformSurfaceId,
                    definition.ContourStyleId, out var foregroundLandform))
            {
                reason = "Palette 缺少笔刷前景地貌。";
                return false;
            }
            palette.TryGetLandformTileSet(definition.BaseSurfaceId,
                definition.ContourStyleId, out var backgroundLandform);
            if (backgroundLandform == null)
                backgroundLandform = definition.ReverseLandformTileSet;
            var edge = definition.CompositeTileSet;
            var foregroundBase = definition.ForegroundBaseTile;
            var backgroundBase = definition.BackgroundBaseTile;
            var foregroundTile = foregroundBase as Tile;
            var backgroundTile = backgroundBase as Tile;
            var foregroundPreview = foregroundTile == null ? null : foregroundTile.sprite;
            var backgroundPreview = backgroundTile == null ? null : backgroundTile.sprite;
            Undo.RegisterCompleteObjectUndo(LaboratoryObjects(target),
                "切换地貌实验笔刷：" + definition.DisplayName);
            if (!target.TryConfigureLaboratoryBrush(definition.ContourStyleId,
                    foregroundBase, backgroundBase, foregroundLandform, backgroundLandform,
                    edge, definition.LandformDisplayName, foregroundPreview, Color.white,
                    definition.BaseDisplayName, backgroundPreview, Color.white, out reason))
                return false;
            MarkLaboratoryDirty(target);
            reason = "已切换为“" + definition.DisplayName + "”。";
            return true;
        }

        internal static bool Matches(TerrainBrushDefinition definition,
            LayeredTerrainTilemap target, BattlefieldTerrainPalette palette)
        {
            if (definition == null || target == null || palette == null) return false;
            if (!TerrainBrushRegistry.TryResolveLaboratoryLandforms(definition,
                    palette, out var foregroundLandform, out var backgroundLandform,
                    out _)) return false;
            var edge = definition.CompositeTileSet;
            return target.MatchesLaboratoryBrush(definition.ContourStyleId,
                definition.ForegroundBaseTile, definition.BackgroundBaseTile,
                foregroundLandform, backgroundLandform, edge,
                definition.LandformDisplayName, definition.BaseDisplayName);
        }

        internal static bool TryClear(LayeredTerrainTilemap target, out string reason)
        {
            if (target == null)
            {
                reason = "请先选择地貌实验目标。";
                return false;
            }
            Undo.RegisterCompleteObjectUndo(LaboratoryObjects(target), "清空地貌实验画布");
            if (!target.ClearAuthoring(out reason)) return false;
            MarkLaboratoryDirty(target);
            reason = "实验画布已清空，可以切换注册笔刷。";
            return true;
        }

        private static UnityEngine.Object[] LaboratoryObjects(LayeredTerrainTilemap target)
        {
            return new UnityEngine.Object[]
            {
                target, target.BaseLogicalTilemap, target.LandformLogicalTilemap,
                target.EdgeLogicalTilemap, target.BaseOutputTilemap,
                target.LandformAOutputTilemap, target.LandformBOutputTilemap,
                target.EdgeAOnBOutputTilemap, target.EdgeBOnAOutputTilemap,
            };
        }

        private static void MarkLaboratoryDirty(LayeredTerrainTilemap target)
        {
            foreach (var value in LaboratoryObjects(target))
                if (value != null) EditorUtility.SetDirty(value);
            if (target.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        }
    }
}
