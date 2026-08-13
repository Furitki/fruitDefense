using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Content
{
    public static class BundledLevelCatalogIds
    {
        public const string Catalog = "catalog.levels.orchard";

        public static class Levels
        {
            public const string Orchard01 = "orchard-01";
            public const string Orchard02 = "orchard-02";
            public const string Orchard03 = "orchard-03";
        }

        public static class Maps
        {
            public const string Orchard01 = BattlefieldMapDefinition.DefaultMapId;
            public const string Orchard02 = "orchard-02";
            public const string Orchard03 = "orchard-03";
        }

        public static class WaveSets
        {
            public const string Teaching = "waves.orchard-01.teaching";
            public const string Coverage = "waves.orchard-02.coverage";
            public const string Pressure = "waves.orchard-03.pressure";
        }

        public static class RuleSets
        {
            public const string Baseline = "rules.orchard-01.baseline";
            public const string Coverage = "rules.orchard-02.coverage";
            public const string Pressure = "rules.orchard-03.pressure";
        }

        public static class Themes
        {
            public const string DayOrchard = "theme.orchard-01.day";
            public const string Creek = "theme.orchard-02.creek";
            public const string Dusk = "theme.orchard-03.dusk";
        }

        public static class TerrainPalettes
        {
            public const string OrchardDefault = "palette.orchard.default";
        }
    }

    public static class BundledLevelCatalogFactory
    {
        private const int GridWidth = 8;
        private const int GridHeight = 7;
        private const float MapUnitsPerCell = BattlefieldMapDefinition.DefaultRouteLength
            / BattlefieldMapDefinition.DefaultRouteSegmentCount;

        public static LevelCatalogSource CreateSource()
        {
            return ComposePublished(CreateBundledSource(),
                PublishedBattlefieldMapCatalog.LoadGenerated());
        }

        public static LevelCatalogSource CreateBundledSource()
        {
            var maps = new[]
            {
                BattlefieldMapDefinition.CreateDefault(),
                CreateCoverageMap(),
                CreatePressureMap(),
            };
            var waveSets = new[]
            {
                new LevelWaveSetDefinition(BundledLevelCatalogIds.WaveSets.Teaching,
                    Enumerable.Range(1, 15).Select(WaveId)),
                new LevelWaveSetDefinition(BundledLevelCatalogIds.WaveSets.Coverage,
                    WaveIds(2, 4, 5, 6, 8, 9, 12, 13)),
                new LevelWaveSetDefinition(BundledLevelCatalogIds.WaveSets.Pressure,
                    WaveIds(3, 5, 8, 10, 14, 15)),
            };
            var ruleSets = new[]
            {
                CreateRules(BundledLevelCatalogIds.RuleSets.Baseline,
                    10, 10, 15, 8, 15f,
                    Milestone(3, BattleContentIds.Equipment.Gatling),
                    Milestone(6, BattleContentIds.Equipment.Ice),
                    Milestone(9, BattleContentIds.Equipment.Chili),
                    Milestone(12, BattleContentIds.Equipment.Gatling,
                        BattleContentIds.Equipment.Ice, BattleContentIds.Equipment.Chili)),
                CreateRules(BundledLevelCatalogIds.RuleSets.Coverage,
                    12, 8, 8, 8, 12f,
                    Milestone(3, BattleContentIds.Equipment.Ice),
                    Milestone(6, BattleContentIds.Equipment.Gatling, BattleContentIds.Equipment.Chili)),
                CreateRules(BundledLevelCatalogIds.RuleSets.Pressure,
                    16, 6, 6, 6, 8f,
                    Milestone(2, BattleContentIds.Equipment.Chili),
                    Milestone(4, BattleContentIds.Equipment.Gatling, BattleContentIds.Equipment.Ice)),
            };
            var themes = new[]
            {
                new LevelPresentationThemeDefinition(BundledLevelCatalogIds.Themes.DayOrchard,
                    "Day Orchard", "#DDF3C4", "#B8D98A", "#D7BE86", "#8A6B3E",
                    "#94C973", "#65725A", "#E66D4A", "#F2C94C",
                    BundledLevelCatalogIds.TerrainPalettes.OrchardDefault),
                new LevelPresentationThemeDefinition(BundledLevelCatalogIds.Themes.Creek,
                    "Creek Orchard", "#CBE8EC", "#86C8B2", "#CFB98D", "#5A7D72",
                    "#70BFA1", "#4F6A66", "#D95F59", "#4EA5D9",
                    BundledLevelCatalogIds.TerrainPalettes.OrchardDefault),
                new LevelPresentationThemeDefinition(BundledLevelCatalogIds.Themes.Dusk,
                    "Dusk Orchard", "#3E3653", "#665A72", "#B09A7A", "#332C40",
                    "#7D8C68", "#4C4654", "#F15A5A", "#F2994A",
                    BundledLevelCatalogIds.TerrainPalettes.OrchardDefault),
            };
            var levels = new[]
            {
                new LevelDefinition(BundledLevelCatalogIds.Levels.Orchard01,
                    maps[0].MapId, waveSets[0].WaveSetId, ruleSets[0].RuleSetId, themes[0].ThemeId),
                new LevelDefinition(BundledLevelCatalogIds.Levels.Orchard02,
                    maps[1].MapId, waveSets[1].WaveSetId, ruleSets[1].RuleSetId, themes[1].ThemeId),
                new LevelDefinition(BundledLevelCatalogIds.Levels.Orchard03,
                    maps[2].MapId, waveSets[2].WaveSetId, ruleSets[2].RuleSetId, themes[2].ThemeId),
            };
            return new LevelCatalogSource(BundledLevelCatalogIds.Catalog,
                BattleContentSchema.BundledCatalogId, BattleContentSchema.BundledContentVersion,
                BundledLevelCatalogIds.Levels.Orchard01, levels, maps, waveSets, ruleSets, themes,
                new[] { BundledLevelCatalogIds.TerrainPalettes.OrchardDefault });
        }

        public static LevelCatalogSource ComposePublished(LevelCatalogSource bundled,
            PublishedBattlefieldMapCatalog published)
        {
            if (bundled == null) throw new ArgumentNullException(nameof(bundled));
            if (published == null) return bundled;
            if (published.SchemaVersion != PublishedBattlefieldMapCatalog.CurrentSchemaVersion)
                throw new InvalidOperationException("Unsupported published battlefield catalog schema "
                    + published.SchemaVersion + ". Expected "
                    + PublishedBattlefieldMapCatalog.CurrentSchemaVersion
                    + "; rebuild it from current-schema authoring assets.");
            if (!string.Equals(published.SourceCatalogId, bundled.CatalogId,
                    StringComparison.Ordinal)
                || !string.Equals(published.ContentVersion, bundled.ContentVersion,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Published battlefield catalog does not match the bundled catalog/version.");

            var maps = bundled.Maps.ToList();
            var levels = bundled.Levels.ToList();
            var templateLevels = bundled.Levels.ToDictionary(level => level.LevelId,
                StringComparer.Ordinal);
            foreach (var entry in published.Entries
                         .Where(value => value != null)
                         .OrderBy(value => value.Order)
                         .ThenBy(value => value.LevelId, StringComparer.Ordinal))
            {
                if (entry.Map == null)
                    throw new InvalidOperationException("Published level '" + entry.LevelId
                        + "' has no map snapshot.");
                LevelDefinition template;
                if (!templateLevels.TryGetValue(entry.TemplateLevelId, out template))
                    throw new InvalidOperationException("Published level '" + entry.LevelId
                        + "' references unknown template level '" + entry.TemplateLevelId + "'.");

                CompiledBattlefieldMap compiledMap;
                BattlefieldLayeredMapValidationResult mapValidation;
                if (!BattlefieldLayeredMapCompiler.TryCompile(entry.Map.ToSource(),
                        out compiledMap, out mapValidation))
                    throw new InvalidOperationException("Published map '" + entry.Map.MapId
                        + "' is invalid: " + string.Join("\n", mapValidation.Issues
                            .Select(issue => issue.ToString()).ToArray()));
                maps.Add(new BattlefieldMapDefinition(compiledMap));
                levels.Add(new LevelDefinition(entry.LevelId, entry.Map.MapId,
                    template.WaveSetId, template.RuleSetId, template.ThemeId));
            }

            return new LevelCatalogSource(bundled.CatalogId, bundled.ContentCatalogId,
                bundled.ContentVersion, bundled.DefaultLevelId, levels, maps,
                bundled.WaveSets, bundled.RuleSets, bundled.Themes,
                bundled.TerrainPaletteIds);
        }

        public static bool TryCompile(out CompiledLevelCatalog compiled,
            out LevelCatalogValidationResult levelValidation,
            out ContentValidationResult contentValidation)
        {
            CompiledBattleContentCatalog battleContent;
            if (!BattleContentCompiler.TryCompile(BundledBattleContentFactory.Create(),
                    out battleContent, out contentValidation))
            {
                levelValidation = LevelCatalogValidator.Validate(CreateSource(), null);
                compiled = null;
                return false;
            }
            return LevelCatalogCompiler.TryCompile(CreateSource(), battleContent,
                out compiled, out levelValidation);
        }

        public static CompiledLevelCatalog CreateCompiled()
        {
            CompiledLevelCatalog compiled;
            LevelCatalogValidationResult levelValidation;
            ContentValidationResult contentValidation;
            if (TryCompile(out compiled, out levelValidation, out contentValidation)) return compiled;
            var contentIssues = contentValidation == null
                ? Array.Empty<string>()
                : contentValidation.Issues.Select(issue => issue.ToString()).ToArray();
            var levelIssues = levelValidation == null
                ? Array.Empty<string>()
                : levelValidation.Issues.Select(issue => issue.ToString()).ToArray();
            throw new InvalidOperationException("Bundled level catalog compilation failed:\n"
                + string.Join("\n", contentIssues.Concat(levelIssues).ToArray()));
        }

        private static BattlefieldMapDefinition CreateCoverageMap()
        {
            var route = new List<Vector2Int>();
            for (var column = 0; column < GridWidth; column++) route.Add(new Vector2Int(column, 0));
            for (var row = 1; row <= 3; row++) route.Add(new Vector2Int(GridWidth - 1, row));
            for (var column = GridWidth - 2; column >= 1; column--) route.Add(new Vector2Int(column, 3));
            for (var row = 4; row < GridHeight; row++) route.Add(new Vector2Int(1, row));
            return CreateMap(BundledLevelCatalogIds.Maps.Orchard02, route, new Vector2Int(0, 6),
                new[]
                {
                    new InitialPotGroup("north-coverage", 3, Cells(1, 1, 3, 1, 5, 1)),
                    new InitialPotGroup("turn-coverage", 2, Cells(2, 2, 5, 2)),
                    new InitialPotGroup("south-coverage", 3, Cells(2, 5, 4, 5, 6, 5)),
                });
        }

        private static BattlefieldMapDefinition CreatePressureMap()
        {
            var route = new[]
            {
                new Vector2Int(7, 3), new Vector2Int(6, 3), new Vector2Int(5, 3),
                new Vector2Int(4, 3), new Vector2Int(3, 3), new Vector2Int(2, 3),
                new Vector2Int(2, 4), new Vector2Int(2, 5), new Vector2Int(1, 5),
            };
            return CreateMap(BundledLevelCatalogIds.Maps.Orchard03, route, new Vector2Int(0, 5),
                new[]
                {
                    new InitialPotGroup("north-pressure", 3, Cells(2, 2, 4, 2, 6, 2)),
                    new InitialPotGroup("south-pressure", 3, Cells(3, 4, 5, 4, 7, 4)),
                });
        }

        private static BattlefieldMapDefinition CreateMap(string mapId,
            IEnumerable<Vector2Int> orderedRoute, Vector2Int core,
            IEnumerable<InitialPotGroup> initialPotGroups)
        {
            return new BattlefieldMapDefinition(BattlefieldLayeredMapFactory.CreateSingleRouteMap(
                mapId, GridWidth, GridHeight, MapUnitsPerCell,
                orderedRoute, core, initialPotGroups));
        }

        private static LevelRuleSetDefinition CreateRules(string id, int initialSun,
            int initialLives, int maxWaves, int initialPotCount, float betweenWaveSeconds,
            params MilestoneRewardDefinitionDto[] milestones)
        {
            return new LevelRuleSetDefinition(new BattleRulesDto
            {
                id = id,
                initialSun = initialSun,
                initialLives = initialLives,
                maxWaves = maxWaves,
                initialPotCount = initialPotCount,
                betweenWaveSeconds = betweenWaveSeconds,
                nurserySlotCount = 5,
                nurseryPotChance = .1f,
                refreshBaseCost = 10,
                refreshCostStep = 5,
                milestoneRewards = milestones,
            });
        }

        private static MilestoneRewardDefinitionDto Milestone(int wave, params string[] equipmentIds)
        {
            return new MilestoneRewardDefinitionDto
            {
                wave = wave,
                potCount = 1,
                equipmentIds = equipmentIds,
            };
        }

        private static string WaveId(int index)
        {
            return "wave." + index.ToString("00");
        }

        private static string[] WaveIds(params int[] indices)
        {
            return indices.Select(WaveId).ToArray();
        }

        private static Vector2Int[] Cells(params int[] coordinates)
        {
            var cells = new Vector2Int[coordinates.Length / 2];
            for (var index = 0; index < cells.Length; index++)
                cells[index] = new Vector2Int(coordinates[index * 2], coordinates[index * 2 + 1]);
            return cells;
        }
    }
}
