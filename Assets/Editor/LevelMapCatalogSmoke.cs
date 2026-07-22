using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class LevelMapCatalogSmoke
    {
        [MenuItem("Fruit Defense/Validate Level Map Catalog")]
        public static void Run()
        {
            var battleContent = CreateBattleContent();
            var source = BundledLevelCatalogFactory.CreateSource();
            CompiledLevelCatalog catalog;
            LevelCatalogValidationResult validation;
            Expect(LevelCatalogCompiler.TryCompile(source, battleContent, out catalog, out validation),
                "Bundled level catalog failed compilation:\n" + Format(validation));

            ValidateOrderedResolution(catalog);
            ValidateMapTopology(catalog);
            ValidateWavePressure(catalog);
            ValidateResolutionFailures(catalog);
            ValidateInvalidCatalogs(source);
            Debug.Log("FRUIT_DEFENSE_LEVEL_MAP_CATALOG_OK");
        }

        private static void ValidateOrderedResolution(CompiledLevelCatalog catalog)
        {
            var expected = new[]
            {
                BundledLevelCatalogIds.Levels.Orchard01,
                BundledLevelCatalogIds.Levels.Orchard02,
                BundledLevelCatalogIds.Levels.Orchard03,
            };
            Expect(catalog.PlayableLevels.Select(value => value.LevelId).SequenceEqual(expected),
                "Playable level order is not orchard-01, orchard-02, orchard-03.");
            Expect(catalog.DefaultLevelId == BundledLevelCatalogIds.Levels.Orchard01,
                "Safe UI default is not orchard-01.");

            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var levelId in expected)
            {
                var result = catalog.Resolve(levelId);
                Expect(result.Succeeded, "Resolution failed for " + levelId + ": " + result.Error);
                var resolved = result.Value;
                Expect(resolved.Level.LevelId == levelId && resolved.Identity.LevelId == levelId,
                    "Resolved level identity changed for " + levelId + ".");
                Expect(resolved.Map.MapId == resolved.Identity.MapId
                    && resolved.WaveSet.WaveSetId == resolved.Identity.WaveSetId
                    && resolved.RuleSet.RuleSetId == resolved.Identity.RuleSetId
                    && resolved.Theme.ThemeId == resolved.Identity.ThemeId,
                    "Composite identity does not match concrete definitions for " + levelId + ".");
                Expect(resolved.OrderedWaves.Count == resolved.WaveSet.WaveIds.Count,
                    "Resolved wave count mismatch for " + levelId + ".");
                Expect(ReferenceEquals(resolved.BattleContent, catalog.BattleContent),
                    "Resolved bundle does not retain shared battle content for " + levelId + ".");
                Expect(identities.Add(resolved.Identity.ToString()),
                    "Composite identity is duplicated for " + levelId + ".");
            }
        }

        private static void ValidateMapTopology(CompiledLevelCatalog catalog)
        {
            var teaching = catalog.Resolve(BundledLevelCatalogIds.Levels.Orchard01).Value.Map;
            var coverage = catalog.Resolve(BundledLevelCatalogIds.Levels.Orchard02).Value.Map;
            var pressure = catalog.Resolve(BundledLevelCatalogIds.Levels.Orchard03).Value.Map;
            var expectedPitch = BattlefieldMapDefinition.DefaultRouteLength
                / BattlefieldMapDefinition.DefaultRouteSegmentCount;
            foreach (var map in new[] { teaching, coverage, pressure })
            {
                string reason;
                var topologyValid = map.Validate(out reason);
                Expect(map.UsesLayeredMap && topologyValid,
                    map.MapId + " does not satisfy the layered map contract: " + reason);
                Expect(Mathf.Abs(map.MapUnitsPerCell - expectedPitch) <= .0001f,
                    map.MapId + " changed MapUnitsPerCell.");
                Expect(map.RouteTileDescriptors.Count == map.RouteCells.Count,
                    map.MapId + " did not derive one route tile per route cell.");
            }

            Expect(RouteSignature(teaching) != RouteSignature(coverage)
                && RouteSignature(teaching) != RouteSignature(pressure)
                && RouteSignature(coverage) != RouteSignature(pressure),
                "Bundled route signatures are not distinct.");
            Expect(teaching.RouteCells.Count == 20
                && teaching.EntryCell == new Vector2Int(0, 0)
                && teaching.RouteCells[7] == new Vector2Int(7, 0)
                && teaching.RouteCells[13] == new Vector2Int(7, 6)
                && teaching.ExitCell == new Vector2Int(1, 6)
                && teaching.CoreCell == new Vector2Int(0, 6)
                && MovementSignature(teaching) == "ESW",
                "orchard-01 is not the required U-shaped teaching route.");
            Expect(coverage.RouteCells.Count == 20
                && coverage.EntryCell == new Vector2Int(0, 0)
                && coverage.RouteCells[7] == new Vector2Int(7, 0)
                && coverage.RouteCells[10] == new Vector2Int(7, 3)
                && coverage.RouteCells[16] == new Vector2Int(1, 3)
                && coverage.ExitCell == new Vector2Int(1, 6)
                && coverage.CoreCell == new Vector2Int(0, 6)
                && MovementSignature(coverage) == "ESWS",
                "orchard-02 is not the required alternating S-shaped coverage route.");
            Expect(pressure.RouteCells.Count == 9
                && pressure.RouteCells.Count < teaching.RouteCells.Count
                && pressure.EntryCell == new Vector2Int(7, 3)
                && pressure.ExitCell == new Vector2Int(1, 5)
                && pressure.CoreCell == new Vector2Int(0, 5)
                && BattlefieldTopology.AreCoordinatesCardinalNeighbors(pressure.ExitCell, pressure.CoreCell)
                && MovementSignature(pressure) == "WSW",
                "orchard-03 is not the required short core corridor.");
        }

        private static void ValidateWavePressure(CompiledLevelCatalog catalog)
        {
            var teaching = catalog.Resolve(BundledLevelCatalogIds.Levels.Orchard01).Value;
            var coverage = catalog.Resolve(BundledLevelCatalogIds.Levels.Orchard02).Value;
            var pressure = catalog.Resolve(BundledLevelCatalogIds.Levels.Orchard03).Value;
            Expect(teaching.OrderedWaves.Count == 15
                && teaching.RuleSet.MaxWaves == teaching.OrderedWaves.Count,
                "Teaching wave/rule composition mismatch.");
            var coverageEnemies = new HashSet<string>(
                coverage.OrderedWaves.SelectMany(value => value.enemyIds), StringComparer.Ordinal);
            Expect(coverageEnemies.Contains(BattleContentIds.Enemies.Runner)
                && coverageEnemies.Contains(BattleContentIds.Enemies.Armored),
                "Coverage wave set does not include both fast and armored enemies.");
            Expect(coverage.RuleSet.MaxWaves == coverage.OrderedWaves.Count,
                "Coverage wave/rule composition mismatch.");
            var finalPressureWave = pressure.OrderedWaves[pressure.OrderedWaves.Count - 1];
            Expect(finalPressureWave.enemyIds.Contains(BattleContentIds.Enemies.Boss),
                "Pressure final wave does not include the existing boss enemy.");
            Expect(pressure.RuleSet.MaxWaves == pressure.OrderedWaves.Count,
                "Pressure wave/rule composition mismatch.");
        }

        private static void ValidateResolutionFailures(CompiledLevelCatalog catalog)
        {
            var unknown = catalog.Resolve("orchard-missing");
            Expect(!unknown.Succeeded && unknown.Value == null && unknown.Error != null
                && unknown.Error.Code == LevelResolutionErrorCode.UnknownLevel
                && unknown.Error.ReferencedId == "orchard-missing",
                "Unknown level did not return a structured unknown-level error.");
            var empty = catalog.Resolve(string.Empty);
            Expect(!empty.Succeeded && empty.Error.Code == LevelResolutionErrorCode.InvalidLevelId,
                "Empty level identity did not return a structured invalid-level error.");
        }

        private static void ValidateInvalidCatalogs(LevelCatalogSource valid)
        {
            ExpectInvalid(Copy(valid, levels: valid.Levels.Concat(new[] { valid.Levels[0] })),
                CreateBattleContent(), "identity.duplicate", "duplicate level identity");
            ExpectInvalid(Copy(valid, maps: valid.Maps.Concat(new[] { valid.Maps[0] })),
                CreateBattleContent(), "identity.duplicate", "duplicate map identity");
            ExpectInvalid(Copy(valid, waveSets: valid.WaveSets.Concat(new[] { valid.WaveSets[0] })),
                CreateBattleContent(), "identity.duplicate", "duplicate wave-set identity");
            ExpectInvalid(Copy(valid, ruleSets: valid.RuleSets.Concat(new[] { valid.RuleSets[0] })),
                CreateBattleContent(), "identity.duplicate", "duplicate rule-set identity");
            ExpectInvalid(Copy(valid, themes: valid.Themes.Concat(new[] { valid.Themes[0] })),
                CreateBattleContent(), "identity.duplicate", "duplicate theme identity");

            var first = valid.Levels[0];
            var unstableLevel = new LevelDefinition("Orchard Invalid", first.MapId,
                first.WaveSetId, first.RuleSetId, first.ThemeId);
            ExpectInvalid(Copy(valid, levels: valid.Levels.Select(value =>
                    value.LevelId == first.LevelId ? unstableLevel : value)),
                CreateBattleContent(), "identity.invalid", "unstable level identity");
            ExpectInvalid(ReplaceLevel(valid, new LevelDefinition(first.LevelId, "map.missing",
                    first.WaveSetId, first.RuleSetId, first.ThemeId)),
                CreateBattleContent(), "reference.missing", "missing map reference", "mapId");
            ExpectInvalid(ReplaceLevel(valid, new LevelDefinition(first.LevelId, first.MapId,
                    "waves.missing", first.RuleSetId, first.ThemeId)),
                CreateBattleContent(), "reference.missing", "missing wave-set reference", "waveSetId");
            ExpectInvalid(ReplaceLevel(valid, new LevelDefinition(first.LevelId, first.MapId,
                    first.WaveSetId, "rules.missing", first.ThemeId)),
                CreateBattleContent(), "reference.missing", "missing rule-set reference", "ruleSetId");
            ExpectInvalid(ReplaceLevel(valid, new LevelDefinition(first.LevelId, first.MapId,
                    first.WaveSetId, first.RuleSetId, "theme.missing")),
                CreateBattleContent(), "reference.missing", "missing theme reference", "themeId");

            var invalidMap = CreateInvalidLegacyMap();
            ExpectInvalid(Copy(valid, maps: valid.Maps.Select((map, index) =>
                    index == 0 ? invalidMap : map)),
                CreateBattleContent(), "map.topology.invalid", "invalid legacy topology");
            var missingWaveSet = new LevelWaveSetDefinition(BundledLevelCatalogIds.WaveSets.Teaching,
                valid.WaveSets[0].WaveIds.Take(14).Concat(new[] { "wave.missing" }));
            ExpectInvalid(Copy(valid, waveSets: Replace(valid.WaveSets, value => value.WaveSetId,
                    missingWaveSet.WaveSetId, missingWaveSet)),
                CreateBattleContent(), "reference.missing", "missing wave definition");
            var unorderedWaveSet = new LevelWaveSetDefinition(BundledLevelCatalogIds.WaveSets.Coverage,
                new[] { "wave.04", "wave.02", "wave.05", "wave.06", "wave.08", "wave.09", "wave.12", "wave.13" });
            ExpectInvalid(Copy(valid, waveSets: Replace(valid.WaveSets, value => value.WaveSetId,
                    unorderedWaveSet.WaveSetId, unorderedWaveSet)),
                CreateBattleContent(), "wave.order.invalid", "unordered wave set");

            var invalidEnemyContent = CreateBattleContent();
            invalidEnemyContent.Waves["wave.01"].enemyIds = new[] { "enemy.missing" };
            ExpectInvalid(valid, invalidEnemyContent, "wave.enemy.missing", "missing enemy definition");

            var countRules = CopyRules(valid.RuleSets[0], rules => rules.maxWaves = 14);
            ExpectInvalid(Copy(valid, ruleSets: Replace(valid.RuleSets, value => value.RuleSetId,
                    countRules.RuleSetId, countRules)),
                CreateBattleContent(), "wave.rule.count-mismatch", "wave/rule count mismatch");
            var milestoneRules = CopyRules(valid.RuleSets[0], rules =>
                rules.milestoneRewards[0].wave = rules.maxWaves + 1);
            ExpectInvalid(Copy(valid, ruleSets: Replace(valid.RuleSets, value => value.RuleSetId,
                    milestoneRules.RuleSetId, milestoneRules)),
                CreateBattleContent(), "rule.milestone.invalid", "out-of-range milestone");
            var equipmentRules = CopyRules(valid.RuleSets[0], rules =>
                rules.milestoneRewards[0].equipmentIds = new[] { "equipment.missing" });
            ExpectInvalid(Copy(valid, ruleSets: Replace(valid.RuleSets, value => value.RuleSetId,
                    equipmentRules.RuleSetId, equipmentRules)),
                CreateBattleContent(), "reference.missing", "missing milestone equipment");

            var theme = valid.Themes[0];
            var incompleteTheme = new LevelPresentationThemeDefinition(theme.ThemeId, string.Empty,
                theme.BackgroundColor, theme.GroundColor, theme.RouteColor, theme.RouteEdgeColor,
                theme.PlantableColor, theme.BlockedColor, theme.CoreColor, "not-a-color",
                theme.TerrainPaletteId);
            ExpectInvalid(Copy(valid, themes: Replace(valid.Themes, value => value.ThemeId,
                    incompleteTheme.ThemeId, incompleteTheme)),
                CreateBattleContent(), "theme.incomplete", "incomplete presentation theme");
            var missingPaletteTheme = new LevelPresentationThemeDefinition(theme.ThemeId, theme.DisplayName,
                theme.BackgroundColor, theme.GroundColor, theme.RouteColor, theme.RouteEdgeColor,
                theme.PlantableColor, theme.BlockedColor, theme.CoreColor, theme.AccentColor,
                "palette.missing");
            ExpectInvalid(Copy(valid, themes: Replace(valid.Themes, value => value.ThemeId,
                    missingPaletteTheme.ThemeId, missingPaletteTheme)),
                CreateBattleContent(), "reference.missing", "missing terrain palette", "terrainPaletteId");
        }

        private static CompiledBattleContentCatalog CreateBattleContent()
        {
            CompiledBattleContentCatalog compiled;
            ContentValidationResult validation;
            Expect(BattleContentCompiler.TryCompile(BundledBattleContentFactory.Create(),
                    out compiled, out validation),
                "Bundled battle content failed compilation before level validation.");
            return compiled;
        }

        private static LevelRuleSetDefinition CopyRules(LevelRuleSetDefinition source,
            Action<BattleRulesDto> mutate)
        {
            var copy = source.CreateBattleRules();
            mutate(copy);
            return new LevelRuleSetDefinition(copy);
        }

        private static BattlefieldMapDefinition CreateInvalidLegacyMap()
        {
            const int width = 8;
            const int height = 7;
            return new BattlefieldMapDefinition(width, height, 1f,
                Enumerable.Range(0, width * height)
                    .Select(index => new Vector2Int(index % width, index / width)),
                new[] { Vector2.zero, Vector2.zero }, new Vector2(3f, 0f),
                new[] { new InitialPotGroup("invalid-map-pots", 8,
                    new[]
                    {
                        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                        new Vector2Int(4, 1), new Vector2Int(5, 1), new Vector2Int(6, 1), new Vector2Int(7, 1),
                    }) });
        }

        private static LevelCatalogSource ReplaceLevel(LevelCatalogSource source,
            LevelDefinition replacement)
        {
            return Copy(source, levels: source.Levels.Select(value =>
                value.LevelId == replacement.LevelId ? replacement : value));
        }

        private static IEnumerable<T> Replace<T>(IEnumerable<T> source, Func<T, string> getId,
            string id, T replacement)
        {
            return source.Select(value => string.Equals(getId(value), id, StringComparison.Ordinal)
                ? replacement : value);
        }

        private static LevelCatalogSource Copy(LevelCatalogSource source,
            IEnumerable<LevelDefinition> levels = null,
            IEnumerable<BattlefieldMapDefinition> maps = null,
            IEnumerable<LevelWaveSetDefinition> waveSets = null,
            IEnumerable<LevelRuleSetDefinition> ruleSets = null,
            IEnumerable<LevelPresentationThemeDefinition> themes = null,
            IEnumerable<string> terrainPaletteIds = null)
        {
            return new LevelCatalogSource(source.CatalogId, source.ContentCatalogId,
                source.ContentVersion, source.DefaultLevelId,
                levels ?? source.Levels, maps ?? source.Maps, waveSets ?? source.WaveSets,
                ruleSets ?? source.RuleSets, themes ?? source.Themes,
                terrainPaletteIds ?? source.TerrainPaletteIds);
        }

        private static void ExpectInvalid(LevelCatalogSource source,
            CompiledBattleContentCatalog content, string expectedCode, string label,
            string expectedField = null)
        {
            CompiledLevelCatalog compiled;
            LevelCatalogValidationResult validation;
            Expect(!LevelCatalogCompiler.TryCompile(source, content, out compiled, out validation)
                && compiled == null, label + " unexpectedly compiled.");
            Expect(validation.Issues.Any(issue => issue.Code == expectedCode
                && (expectedField == null || issue.Field == expectedField)),
                label + " did not report " + expectedCode + ". Issues:\n" + Format(validation));
        }

        private static string RouteSignature(BattlefieldMapDefinition map)
        {
            return string.Join(";", map.RouteCells.Select(cell => cell.x + "," + cell.y).ToArray());
        }

        private static string MovementSignature(BattlefieldMapDefinition map)
        {
            var segments = new List<char>();
            Vector2Int previousDirection = Vector2Int.zero;
            for (var index = 1; index < map.RouteCells.Count; index++)
            {
                var direction = map.RouteCells[index] - map.RouteCells[index - 1];
                if (direction == previousDirection) continue;
                previousDirection = direction;
                if (direction == Vector2Int.right) segments.Add('E');
                else if (direction == Vector2Int.left) segments.Add('W');
                else if (direction == Vector2Int.up) segments.Add('S');
                else if (direction == Vector2Int.down) segments.Add('N');
            }
            return new string(segments.ToArray());
        }

        private static string Format(LevelCatalogValidationResult result)
        {
            return result == null ? "<missing validation>"
                : string.Join("\n", result.Issues.Select(issue => issue.ToString()).ToArray());
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Level-map catalog validation failed: " + message);
        }
    }
}
