using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FruitDefense.Editor
{
    public sealed class BattlefieldMapPublicationDiagnostic
    {
        public BattlefieldMapAuthoringDiagnosticSeverity Severity { get; private set; }
        public string Code { get; private set; }
        public string EntryLevelId { get; private set; }
        public string MapId { get; private set; }
        public string TemplateLevelId { get; private set; }
        public string Field { get; private set; }
        public string Message { get; private set; }
        public bool HasCell { get; private set; }
        public Vector2Int Cell { get; private set; }
        public string LandformSurfaceId { get; private set; }
        public string BaseSurfaceId { get; private set; }
        public string ContourStyleId { get; private set; }
        public string EdgeStyleId { get; private set; }
        public bool IsBlocking
        {
            get { return Severity == BattlefieldMapAuthoringDiagnosticSeverity.Error; }
        }

        public BattlefieldMapPublicationDiagnostic(
            BattlefieldMapAuthoringDiagnosticSeverity severity, string code,
            string levelId, string mapId, string templateLevelId, string field,
            string message, Vector2Int? cell = null, string landformSurfaceId = null,
            string baseSurfaceId = null, string contourStyleId = null,
            string edgeStyleId = null)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            EntryLevelId = levelId ?? string.Empty;
            MapId = mapId ?? string.Empty;
            TemplateLevelId = templateLevelId ?? string.Empty;
            Field = field ?? string.Empty;
            Message = message ?? string.Empty;
            HasCell = cell.HasValue;
            Cell = cell.GetValueOrDefault();
            LandformSurfaceId = landformSurfaceId ?? string.Empty;
            BaseSurfaceId = baseSurfaceId ?? string.Empty;
            ContourStyleId = contourStyleId ?? string.Empty;
            EdgeStyleId = edgeStyleId ?? string.Empty;
        }

        public override string ToString()
        {
            return Severity + " " + Code + " [" + EntryLevelId + ":" + MapId
                + "." + Field + "] " + Message;
        }
    }

    public sealed class BattlefieldMapPublicationResult
    {
        public bool Succeeded { get; private set; }
        public PublishedBattlefieldMapCatalog GeneratedCatalog { get; private set; }
        public IReadOnlyList<BattlefieldMapPublicationDiagnostic> Diagnostics { get; private set; }

        public BattlefieldMapPublicationResult(bool succeeded,
            PublishedBattlefieldMapCatalog generated,
            IEnumerable<BattlefieldMapPublicationDiagnostic> diagnostics)
        {
            Succeeded = succeeded;
            GeneratedCatalog = generated;
            Diagnostics = new ReadOnlyCollection<BattlefieldMapPublicationDiagnostic>(
                (diagnostics ?? Enumerable.Empty<BattlefieldMapPublicationDiagnostic>()).ToList());
        }
    }

    public static class BattlefieldMapPublicationExporter
    {
        public const string DefaultGeneratedAssetPath =
            "Assets/Resources/Generated/PublishedBattlefieldMapCatalog.asset";
        public const string ReleaseBattleScenePath = "Assets/Scenes/Battle.unity";

        public static BattlefieldMapPublicationResult Rebuild(
            BattlefieldMapPublicationManifest manifest)
        {
            return Rebuild(manifest, DefaultGeneratedAssetPath,
                LoadReleaseRegisteredPalettes());
        }

        public static BattlefieldMapPublicationResult Rebuild(
            BattlefieldMapPublicationManifest manifest, string outputAssetPath,
            IEnumerable<BattlefieldTerrainPalette> registeredPalettes)
        {
            PublishedBattlefieldMapCatalog generated;
            IReadOnlyList<BattlefieldMapPublicationDiagnostic> diagnostics;
            if (!TryBuildCatalog(manifest, registeredPalettes,
                    out generated, out diagnostics))
                return new BattlefieldMapPublicationResult(false, null, diagnostics);

            var normalizedPath = (outputAssetPath ?? string.Empty).Replace('\\', '/');
            if (!normalizedPath.StartsWith("Assets/", StringComparison.Ordinal)
                || !normalizedPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                DestroyTransient(generated);
                var invalid = diagnostics.Concat(new[]
                {
                    Error("publication.output-path", string.Empty, string.Empty,
                        string.Empty, "outputAssetPath",
                        "Generated catalog path must be an Assets/*.asset path."),
                });
                return new BattlefieldMapPublicationResult(false, null, invalid);
            }

            PublishedBattlefieldMapCatalog existing = null;
            PublishedBattlefieldMapCatalog backup = null;
            var createdNewOutput = false;
            try
            {
                var directory = Path.GetDirectoryName(normalizedPath);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                generated.name = Path.GetFileNameWithoutExtension(normalizedPath);
                existing = AssetDatabase.LoadAssetAtPath<PublishedBattlefieldMapCatalog>(
                    normalizedPath);
                if (existing == null)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(normalizedPath) != null)
                        throw new InvalidOperationException(
                            "Generated output path contains a different asset type.");
                    AssetDatabase.CreateAsset(generated, normalizedPath);
                    existing = generated;
                    generated = null;
                    createdNewOutput = true;
                }
                else
                {
                    backup = ScriptableObject.CreateInstance<PublishedBattlefieldMapCatalog>();
                    EditorUtility.CopySerialized(existing, backup);
                    EditorUtility.CopySerialized(generated, existing);
                    existing.name = Path.GetFileNameWithoutExtension(normalizedPath);
                    EditorUtility.SetDirty(existing);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(normalizedPath,
                    ImportAssetOptions.ForceSynchronousImport
                    | ImportAssetOptions.ForceUpdate);
                PublishedBattlefieldMapCatalog reloaded;
                if (string.Equals(normalizedPath, DefaultGeneratedAssetPath,
                        StringComparison.Ordinal))
                {
                    string reloadReason;
                    if (!TryReloadGeneratedResource(out reloaded, out reloadReason))
                        throw new InvalidOperationException(reloadReason);
                }
                else reloaded = AssetDatabase.LoadAssetAtPath<PublishedBattlefieldMapCatalog>(
                    normalizedPath);
                if (reloaded == null)
                    throw new InvalidOperationException(
                        "Generated catalog could not be reloaded after import.");

                CompiledLevelCatalog compiled;
                LevelCatalogValidationResult levelValidation;
                ContentValidationResult contentValidation;
                if (!TryCompileWith(reloaded, out compiled,
                        out levelValidation, out contentValidation))
                    throw new InvalidOperationException("Reloaded generated catalog failed normal compilation: "
                        + FirstIssue(levelValidation, contentValidation));
                return new BattlefieldMapPublicationResult(true, reloaded, diagnostics);
            }
            catch (Exception exception)
            {
                try
                {
                    if (backup != null)
                    {
                        var rollbackTarget = existing != null ? existing
                            : AssetDatabase.LoadAssetAtPath<PublishedBattlefieldMapCatalog>(
                                normalizedPath);
                        if (rollbackTarget == null)
                            throw new InvalidOperationException(
                                "Last valid generated catalog could not be reopened for rollback.");
                        EditorUtility.CopySerialized(backup, rollbackTarget);
                        EditorUtility.SetDirty(rollbackTarget);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.ImportAsset(normalizedPath,
                            ImportAssetOptions.ForceSynchronousImport
                            | ImportAssetOptions.ForceUpdate);
                    }
                    else if (createdNewOutput)
                    {
                        AssetDatabase.DeleteAsset(normalizedPath);
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    }
                }
                catch (Exception rollbackException)
                {
                    Debug.LogError("Generated catalog rollback failed: "
                        + rollbackException.Message);
                }
                var failed = diagnostics.Concat(new[]
                {
                    Error("publication.write-failed", string.Empty, string.Empty,
                        string.Empty, "generatedCatalog", exception.Message),
                });
                return new BattlefieldMapPublicationResult(false, null, failed);
            }
            finally
            {
                DestroyTransient(generated);
                DestroyTransient(backup);
            }
        }

        public static bool TryBuildCatalog(BattlefieldMapPublicationManifest manifest,
            IEnumerable<BattlefieldTerrainPalette> registeredPalettes,
            out PublishedBattlefieldMapCatalog generated,
            out IReadOnlyList<BattlefieldMapPublicationDiagnostic> diagnostics)
        {
            generated = null;
            var issues = new List<BattlefieldMapPublicationDiagnostic>();
            if (manifest == null)
            {
                issues.Add(Error("publication.manifest-null", string.Empty, string.Empty,
                    string.Empty, "manifest", "A publication manifest is required."));
                diagnostics = issues.AsReadOnly();
                return false;
            }

            var bundled = BundledLevelCatalogFactory.CreateBundledSource();
            var palettes = (registeredPalettes ?? Enumerable.Empty<BattlefieldTerrainPalette>())
                .Where(palette => palette != null).ToArray();
            var bundledLevelIds = new HashSet<string>(bundled.Levels.Select(level => level.LevelId),
                StringComparer.Ordinal);
            var bundledMapIds = new HashSet<string>(bundled.Maps.Select(map => map.MapId),
                StringComparer.Ordinal);
            var levels = new HashSet<string>(StringComparer.Ordinal);
            var maps = new HashSet<string>(StringComparer.Ordinal);
            var templates = bundled.Levels.ToDictionary(level => level.LevelId,
                StringComparer.Ordinal);
            var themes = bundled.Themes.ToDictionary(theme => theme.ThemeId,
                StringComparer.Ordinal);
            var waveSets = new HashSet<string>(bundled.WaveSets.Select(value => value.WaveSetId),
                StringComparer.Ordinal);
            var ruleSets = new HashSet<string>(bundled.RuleSets.Select(value => value.RuleSetId),
                StringComparer.Ordinal);
            var publishedEntries = new List<PublishedBattlefieldMapEntry>();

            var orderedEntries = manifest.Entries.Select((entry, sourceIndex) =>
                    new { entry, sourceIndex })
                .OrderBy(value => value.entry == null ? int.MaxValue : value.entry.Order)
                .ThenBy(value => value.entry == null ? string.Empty : value.entry.LevelId,
                    StringComparer.Ordinal).ToArray();
            foreach (var item in orderedEntries)
            {
                var entry = item.entry;
                if (entry == null)
                {
                    issues.Add(Error("publication.entry-null", string.Empty, string.Empty,
                        string.Empty, "entries[" + item.sourceIndex + "]",
                        "Publication entry is required."));
                    continue;
                }
                var mapId = entry.Map == null ? string.Empty : entry.Map.MapId;
                if (string.IsNullOrWhiteSpace(entry.LevelId))
                    issues.Add(Error("publication.level-id", entry.LevelId, mapId,
                        entry.TemplateLevelId, "levelId", "Stable level identity is required."));
                if (!levels.Add(entry.LevelId) || bundledLevelIds.Contains(entry.LevelId))
                    issues.Add(Error("publication.level-id-duplicate", entry.LevelId, mapId,
                        entry.TemplateLevelId, "levelId",
                        "Level identity conflicts with a bundled or published entry."));
                if (entry.Map == null)
                {
                    issues.Add(Error("publication.map-null", entry.LevelId, string.Empty,
                        entry.TemplateLevelId, "map", "A map authoring asset is required."));
                    continue;
                }
                if (!maps.Add(mapId) || bundledMapIds.Contains(mapId))
                    issues.Add(Error("publication.map-id-duplicate", entry.LevelId, mapId,
                        entry.TemplateLevelId, "map.mapId",
                        "Map identity conflicts with a bundled or published map."));

                LevelDefinition template;
                if (!templates.TryGetValue(entry.TemplateLevelId, out template))
                {
                    issues.Add(Error("publication.template-missing", entry.LevelId, mapId,
                        entry.TemplateLevelId, "templateLevelId",
                        "Template level does not exist in the bundled reviewed catalog."));
                    AppendAuthoringDiagnostics(entry, issues);
                    continue;
                }
                if (!waveSets.Contains(template.WaveSetId))
                    issues.Add(Error("publication.template-wave-set-missing", entry.LevelId,
                        mapId, entry.TemplateLevelId, "templateLevelId.waveSetId",
                        "Template wave-set reference is missing: " + template.WaveSetId + "."));
                if (!ruleSets.Contains(template.RuleSetId))
                    issues.Add(Error("publication.template-rule-set-missing", entry.LevelId,
                        mapId, entry.TemplateLevelId, "templateLevelId.ruleSetId",
                        "Template rule-set reference is missing: " + template.RuleSetId + "."));
                LevelPresentationThemeDefinition theme;
                if (!themes.TryGetValue(template.ThemeId, out theme))
                {
                    issues.Add(Error("publication.template-theme-missing", entry.LevelId,
                        mapId, entry.TemplateLevelId, "templateLevelId.themeId",
                        "Template theme reference is missing: " + template.ThemeId + "."));
                }
                else
                {
                    ValidatePalette(entry, theme, palettes, issues);
                }
                AppendAuthoringDiagnostics(entry, issues);
                publishedEntries.Add(new PublishedBattlefieldMapEntry(entry.Order,
                    entry.LevelId, entry.TemplateLevelId,
                    new PublishedBattlefieldMapRecord(entry.Map)));
            }

            if (issues.Any(issue => issue.IsBlocking))
            {
                diagnostics = issues.AsReadOnly();
                return false;
            }

            generated = ScriptableObject.CreateInstance<PublishedBattlefieldMapCatalog>();
            generated.Configure(bundled.CatalogId, bundled.ContentVersion, publishedEntries);
            try
            {
                CompiledLevelCatalog compiled;
                LevelCatalogValidationResult levelValidation;
                ContentValidationResult contentValidation;
                if (!TryCompileWith(generated, out compiled,
                        out levelValidation, out contentValidation))
                {
                    if (levelValidation != null)
                        foreach (var issue in levelValidation.Issues)
                            issues.Add(Error("catalog." + issue.Code, issue.ItemId,
                                string.Empty, string.Empty, issue.Field, issue.Message));
                    if (contentValidation != null)
                        foreach (var issue in contentValidation.Issues)
                            issues.Add(Error("content." + issue.code, string.Empty,
                                string.Empty, string.Empty, issue.field, issue.message));
                }
            }
            catch (Exception exception)
            {
                issues.Add(Error("publication.catalog-compose", string.Empty,
                    string.Empty, string.Empty, "catalog", exception.Message));
            }
            if (issues.Any(issue => issue.IsBlocking))
            {
                DestroyTransient(generated);
                generated = null;
                diagnostics = issues.AsReadOnly();
                return false;
            }

            diagnostics = issues.AsReadOnly();
            return true;
        }

        public static IReadOnlyList<BattlefieldTerrainPalette> LoadReleaseRegisteredPalettes()
        {
            var loaded = SceneManager.GetSceneByPath(ReleaseBattleScenePath);
            var opened = !loaded.IsValid() || !loaded.isLoaded;
            var scene = opened
                ? EditorSceneManager.OpenScene(ReleaseBattleScenePath, OpenSceneMode.Additive)
                : loaded;
            try
            {
                return scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<FruitDefenseGame>(true))
                    .SelectMany(game => game.BattlefieldTerrainPalettes)
                    .Where(palette => palette != null)
                    .Distinct().ToArray();
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static bool TryReloadGeneratedResource(
            out PublishedBattlefieldMapCatalog reloaded, out string reason)
        {
            reloaded = null;
            var previouslyLoaded = PublishedBattlefieldMapCatalog.LoadGenerated();
            if (previouslyLoaded != null) Resources.UnloadAsset(previouslyLoaded);
            AssetDatabase.ImportAsset(DefaultGeneratedAssetPath,
                ImportAssetOptions.ForceSynchronousImport
                | ImportAssetOptions.ForceUpdate);
            reloaded = PublishedBattlefieldMapCatalog.LoadGenerated();
            if (reloaded == null)
            {
                reason = "Generated published-map resource did not reload from '"
                    + PublishedBattlefieldMapCatalog.ResourcePath + "'.";
                return false;
            }
            reason = "ok";
            return true;
        }

        public static bool TryResolvePublicationContext(BattlefieldMapAuthoringAsset map,
            BattlefieldMapPublicationManifest manifest,
            out BattlefieldMapPublicationManifestEntry entry,
            out LevelDefinition template, out LevelPresentationThemeDefinition theme,
            out BattlefieldTerrainPalette palette, out string reason)
        {
            entry = manifest == null || map == null ? null : manifest.FindByMap(map);
            template = null;
            theme = null;
            palette = null;
            if (entry == null)
            {
                reason = "Map is not referenced by the selected publication manifest.";
                return false;
            }
            var bundled = BundledLevelCatalogFactory.CreateBundledSource();
            var resolvedEntry = entry;
            template = bundled.Levels.FirstOrDefault(level => string.Equals(level.LevelId,
                resolvedEntry.TemplateLevelId, StringComparison.Ordinal));
            if (template == null)
            {
                reason = "Template level does not exist: " + entry.TemplateLevelId + ".";
                return false;
            }
            var resolvedTemplate = template;
            theme = bundled.Themes.FirstOrDefault(value => string.Equals(value.ThemeId,
                resolvedTemplate.ThemeId, StringComparison.Ordinal));
            if (theme == null)
            {
                reason = "Template theme does not exist: " + template.ThemeId + ".";
                return false;
            }
            var resolvedTheme = theme;
            palette = LoadReleaseRegisteredPalettes().FirstOrDefault(value =>
                string.Equals(value.PaletteId, resolvedTheme.TerrainPaletteId, StringComparison.Ordinal));
            if (palette == null)
            {
                reason = "Release Battle registry does not contain palette '"
                    + theme.TerrainPaletteId + "'.";
                return false;
            }
            reason = "ok";
            return true;
        }

        private static void ValidatePalette(BattlefieldMapPublicationManifestEntry entry,
            LevelPresentationThemeDefinition theme,
            IEnumerable<BattlefieldTerrainPalette> registeredPalettes,
            ICollection<BattlefieldMapPublicationDiagnostic> issues)
        {
            var palette = registeredPalettes.FirstOrDefault(value => string.Equals(
                value.PaletteId, theme.TerrainPaletteId, StringComparison.Ordinal));
            if (palette == null)
            {
                issues.Add(Error("publication.palette-not-registered", entry.LevelId,
                    entry.Map.MapId, entry.TemplateLevelId, "theme.terrainPaletteId",
                    "Release Battle registry does not contain palette '"
                    + theme.TerrainPaletteId + "'."));
                return;
            }
            string paletteReason;
            if (!palette.Validate(out paletteReason))
            {
                issues.Add(Error("publication.palette-invalid", entry.LevelId,
                    entry.Map.MapId, entry.TemplateLevelId, "theme.terrainPaletteId",
                    paletteReason));
                return;
            }
            for (var index = 0; index < entry.Map.VisualCells.Count; index++)
            {
                var visual = entry.Map.VisualCells[index];
                if (visual == null) continue;
                var cell = entry.Map.GridWidth > 0
                    ? new Vector2Int(index % entry.Map.GridWidth, index / entry.Map.GridWidth)
                    : Vector2Int.zero;
                Texture2D baseTexture;
                if (!palette.TryGetBaseTexture(visual.BaseSurfaceId, out baseTexture))
                    issues.Add(PaletteError("publication.palette-base-missing", entry,
                        index, cell, visual, "Palette has no renderable base surface binding."));
                if (!string.IsNullOrWhiteSpace(visual.LandformSurfaceId))
                {
                    DualGridTileSet tileSet;
                    if (!palette.TryGetLandformTileSet(visual.LandformSurfaceId,
                            visual.ContourStyleId, out tileSet))
                        issues.Add(PaletteError("publication.palette-landform-missing", entry,
                            index, cell, visual,
                            "Palette has no exact surface-plus-contour landform binding."));
                }
                if (!string.IsNullOrWhiteSpace(visual.EdgeStyleId))
                {
                    DualGridTileSet edge;
                    if (!palette.TryGetEdgeTileSet(visual.LandformSurfaceId,
                            visual.BaseSurfaceId, visual.ContourStyleId,
                            visual.EdgeStyleId, out edge))
                        issues.Add(PaletteError("publication.palette-edge-missing", entry,
                            index, cell, visual,
                            "Palette lacks a same-contour landform/base edge resource in either material direction."));
                }
            }
        }

        private static void AppendAuthoringDiagnostics(
            BattlefieldMapPublicationManifestEntry entry,
            ICollection<BattlefieldMapPublicationDiagnostic> issues)
        {
            foreach (var issue in entry.Map.CollectDiagnostics())
                issues.Add(new BattlefieldMapPublicationDiagnostic(issue.Severity,
                    issue.Code, entry.LevelId, entry.Map.MapId, entry.TemplateLevelId,
                    issue.Field, issue.Message,
                    issue.HasCell ? issue.Cell : (Vector2Int?)null));
        }

        private static BattlefieldMapPublicationDiagnostic PaletteError(string code,
            BattlefieldMapPublicationManifestEntry entry, int index, Vector2Int cell,
            BattlefieldVisualCellAuthoringRecord visual, string message)
        {
            return new BattlefieldMapPublicationDiagnostic(
                BattlefieldMapAuthoringDiagnosticSeverity.Error, code, entry.LevelId,
                entry.Map.MapId, entry.TemplateLevelId, "visualCells[" + index + "]",
                message, cell, visual.LandformSurfaceId, visual.BaseSurfaceId,
                visual.ContourStyleId, visual.EdgeStyleId);
        }

        private static BattlefieldMapPublicationDiagnostic Error(string code,
            string levelId, string mapId, string templateLevelId, string field,
            string message)
        {
            return new BattlefieldMapPublicationDiagnostic(
                BattlefieldMapAuthoringDiagnosticSeverity.Error, code, levelId, mapId,
                templateLevelId, field, message);
        }

        private static bool TryCompileWith(PublishedBattlefieldMapCatalog generated,
            out CompiledLevelCatalog compiled,
            out LevelCatalogValidationResult levelValidation,
            out ContentValidationResult contentValidation)
        {
            CompiledBattleContentCatalog battleContent;
            if (!BattleContentCompiler.TryCompile(BundledBattleContentFactory.Create(),
                    out battleContent, out contentValidation))
            {
                compiled = null;
                levelValidation = null;
                return false;
            }
            var source = BundledLevelCatalogFactory.ComposePublished(
                BundledLevelCatalogFactory.CreateBundledSource(), generated);
            return LevelCatalogCompiler.TryCompile(source, battleContent,
                out compiled, out levelValidation);
        }

        private static string FirstIssue(LevelCatalogValidationResult levels,
            ContentValidationResult content)
        {
            if (levels != null && levels.Issues.Count > 0) return levels.Issues[0].ToString();
            if (content != null && content.Issues.Count > 0) return content.Issues[0].ToString();
            return "unknown";
        }

        private static void DestroyTransient(UnityEngine.Object value)
        {
            if (value != null && !EditorUtility.IsPersistent(value))
                UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
