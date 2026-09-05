using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Content
{
    public sealed class CompiledLevelCatalog
    {
        private readonly IReadOnlyDictionary<string, LevelDefinition> _levels;
        private readonly IReadOnlyDictionary<string, BattlefieldMapDefinition> _maps;
        private readonly IReadOnlyDictionary<string, LevelWaveSetDefinition> _waveSets;
        private readonly IReadOnlyDictionary<string, LevelRuleSetDefinition> _ruleSets;
        private readonly IReadOnlyDictionary<string, LevelPresentationThemeDefinition> _themes;
        private readonly IReadOnlyCollection<string> _terrainPaletteIds;

        public string CatalogId { get; private set; }
        public string ContentCatalogId { get; private set; }
        public string ContentVersion { get; private set; }
        public string DefaultLevelId { get; private set; }
        public IReadOnlyList<LevelDefinition> PlayableLevels { get; private set; }
        public IReadOnlyDictionary<string, LevelDefinition> Levels { get { return _levels; } }
        public IReadOnlyDictionary<string, BattlefieldMapDefinition> Maps { get { return _maps; } }
        public IReadOnlyDictionary<string, LevelWaveSetDefinition> WaveSets { get { return _waveSets; } }
        public IReadOnlyDictionary<string, LevelRuleSetDefinition> RuleSets { get { return _ruleSets; } }
        public IReadOnlyDictionary<string, LevelPresentationThemeDefinition> Themes { get { return _themes; } }
        public IReadOnlyCollection<string> TerrainPaletteIds { get { return _terrainPaletteIds; } }
        public CompiledBattleContentCatalog BattleContent { get; private set; }

        internal CompiledLevelCatalog(LevelCatalogSource source, CompiledBattleContentCatalog battleContent)
        {
            CatalogId = source.CatalogId;
            ContentCatalogId = source.ContentCatalogId;
            ContentVersion = source.ContentVersion;
            DefaultLevelId = source.DefaultLevelId;
            PlayableLevels = Array.AsReadOnly(source.Levels.ToArray());
            _levels = Index(source.Levels, value => value.LevelId);
            _maps = Index(source.Maps, value => value.MapId);
            _waveSets = Index(source.WaveSets, value => value.WaveSetId);
            _ruleSets = Index(source.RuleSets, value => value.RuleSetId);
            _themes = Index(source.Themes, value => value.ThemeId);
            _terrainPaletteIds = Array.AsReadOnly(source.TerrainPaletteIds.ToArray());
            BattleContent = battleContent;
        }

        public LevelResolutionResult Resolve(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
                return LevelResolutionResult.Failure(LevelResolutionErrorCode.InvalidLevelId,
                    levelId, "levelId", levelId, "Level identity must not be empty.");

            LevelDefinition level;
            if (!_levels.TryGetValue(levelId, out level))
                return LevelResolutionResult.Failure(LevelResolutionErrorCode.UnknownLevel,
                    levelId, "levelId", levelId, "Unknown level identity '" + levelId + "'.");

            BattlefieldMapDefinition map;
            if (!_maps.TryGetValue(level.MapId, out map))
                return Missing(LevelResolutionErrorCode.MissingMap, level, "mapId", level.MapId);
            LevelWaveSetDefinition waveSet;
            if (!_waveSets.TryGetValue(level.WaveSetId, out waveSet))
                return Missing(LevelResolutionErrorCode.MissingWaveSet, level, "waveSetId", level.WaveSetId);
            LevelRuleSetDefinition ruleSet;
            if (!_ruleSets.TryGetValue(level.RuleSetId, out ruleSet))
                return Missing(LevelResolutionErrorCode.MissingRuleSet, level, "ruleSetId", level.RuleSetId);
            LevelPresentationThemeDefinition theme;
            if (!_themes.TryGetValue(level.ThemeId, out theme))
                return Missing(LevelResolutionErrorCode.MissingTheme, level, "themeId", level.ThemeId);

            var orderedWaves = waveSet.WaveIds.Select(id => BattleContent.Waves[id]).ToArray();
            return LevelResolutionResult.Success(new ResolvedLevelDefinition(level, map, waveSet,
                orderedWaves, ruleSet, theme, BattleContent));
        }

        public bool TryResolve(string levelId, out ResolvedLevelDefinition resolved,
            out LevelResolutionError error)
        {
            var result = Resolve(levelId);
            resolved = result.Value;
            error = result.Error;
            return result.Succeeded;
        }

        private static LevelResolutionResult Missing(LevelResolutionErrorCode code,
            LevelDefinition level, string field, string referencedId)
        {
            return LevelResolutionResult.Failure(code, level.LevelId, field, referencedId,
                "Level '" + level.LevelId + "' references missing " + field + " '" + referencedId + "'.");
        }

        private static IReadOnlyDictionary<string, T> Index<T>(IEnumerable<T> values, Func<T, string> getId)
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var value in values) result.Add(getId(value), value);
            return new ReadOnlyDictionary<string, T>(result);
        }
    }

    public static class LevelCatalogCompiler
    {
        public static bool TryCompile(LevelCatalogSource source, CompiledBattleContentCatalog battleContent,
            out CompiledLevelCatalog compiled, out LevelCatalogValidationResult validation)
        {
            validation = LevelCatalogValidator.Validate(source, battleContent);
            if (!validation.IsValid)
            {
                compiled = null;
                return false;
            }

            compiled = new CompiledLevelCatalog(source, battleContent);
            return true;
        }
    }

    public static class LevelCatalogValidator
    {
        private static readonly Regex StableIdPattern = new Regex(
            "^[a-z][a-z0-9]*(?:[._-][a-z0-9]+)*$", RegexOptions.CultureInvariant);

        public static LevelCatalogValidationResult Validate(LevelCatalogSource source,
            CompiledBattleContentCatalog battleContent)
        {
            var result = new LevelCatalogValidationResult();
            if (source == null)
            {
                result.Add("catalog.null", "catalog", string.Empty, string.Empty, "Level catalog is null.");
                return result;
            }
            if (battleContent == null)
            {
                result.Add("content.null", "catalog", source.CatalogId, "battleContent",
                    "Compiled battle content is required.");
                return result;
            }

            RequireStableId(source.CatalogId, "catalog", source.CatalogId, "catalogId", result);
            RequireStableId(source.ContentCatalogId, "catalog", source.CatalogId, "contentCatalogId", result);
            RequireStableId(source.DefaultLevelId, "catalog", source.CatalogId, "defaultLevelId", result);
            if (string.IsNullOrWhiteSpace(source.ContentVersion))
                result.Add("identity.invalid", "catalog", source.CatalogId, "contentVersion",
                    "Content version must not be empty.");
            if (battleContent.Header == null
                || !string.Equals(source.ContentCatalogId, battleContent.Header.catalogId, StringComparison.Ordinal)
                || !string.Equals(source.ContentVersion, battleContent.Header.contentVersion, StringComparison.Ordinal))
                result.Add("catalog.content-mismatch", "catalog", source.CatalogId, "battleContent",
                    "Level catalog content identity does not match compiled battle content.");

            var levels = Index(source.Levels, value => value == null ? string.Empty : value.LevelId,
                "levels", "levelId", result);
            var maps = Index(source.Maps, value => value == null ? string.Empty : value.MapId,
                "maps", "mapId", result);
            var waveSets = Index(source.WaveSets, value => value == null ? string.Empty : value.WaveSetId,
                "waveSets", "waveSetId", result);
            var ruleSets = Index(source.RuleSets, value => value == null ? string.Empty : value.RuleSetId,
                "ruleSets", "ruleSetId", result);
            var themes = Index(source.Themes, value => value == null ? string.Empty : value.ThemeId,
                "themes", "themeId", result);
            var terrainPaletteIds = IndexIds(source.TerrainPaletteIds, "terrainPalettes", result);

            RequireNonEmpty(source.Levels.Count, "levels", result);
            RequireNonEmpty(source.Maps.Count, "maps", result);
            RequireNonEmpty(source.WaveSets.Count, "waveSets", result);
            RequireNonEmpty(source.RuleSets.Count, "ruleSets", result);
            RequireNonEmpty(source.Themes.Count, "themes", result);
            RequireNonEmpty(source.TerrainPaletteIds.Count, "terrainPalettes", result);

            ValidateMaps(source.Maps, result);
            ValidateWaveSets(source.WaveSets, battleContent, result);
            ValidateRuleSets(source.RuleSets, battleContent, result);
            ValidateThemes(source.Themes, terrainPaletteIds, result);
            ValidateLevels(source, levels, maps, waveSets, ruleSets, themes, result);
            return result;
        }

        private static void ValidateMaps(IReadOnlyList<BattlefieldMapDefinition> maps,
            LevelCatalogValidationResult result)
        {
            foreach (var map in maps)
            {
                if (map == null) continue;
                string reason;
                if (!map.Validate(out reason))
                    result.Add("map.topology.invalid", "maps", map.MapId, "topology", reason);
                if (!map.UsesLayeredMap)
                    result.Add("map.layers.legacy", "maps", map.MapId, "layers",
                        "Level maps must use the layered visual/gameplay/marker contract.");
            }
        }

        private static void ValidateWaveSets(IReadOnlyList<LevelWaveSetDefinition> waveSets,
            CompiledBattleContentCatalog battleContent, LevelCatalogValidationResult result)
        {
            foreach (var waveSet in waveSets)
            {
                if (waveSet == null) continue;
                if (waveSet.WaveIds.Count == 0)
                    result.Add("waveSet.empty", "waveSets", waveSet.WaveSetId, "waveIds",
                        "Wave set must reference at least one wave.");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var previousIndex = 0;
                for (var index = 0; index < waveSet.WaveIds.Count; index++)
                {
                    var waveId = waveSet.WaveIds[index];
                    RequireStableId(waveId, "waveSets", waveSet.WaveSetId,
                        "waveIds[" + index + "]", result);
                    if (!seen.Add(waveId))
                        result.Add("wave.reference.duplicate", "waveSets", waveSet.WaveSetId,
                            "waveIds[" + index + "]", "Wave identity '" + waveId + "' is repeated.");
                    WaveDefinitionDto wave;
                    if (!battleContent.Waves.TryGetValue(waveId, out wave))
                    {
                        result.Add("reference.missing", "waveSets", waveSet.WaveSetId,
                            "waveIds[" + index + "]", "Missing wave definition '" + waveId + "'.");
                        continue;
                    }
                    if (wave.index <= previousIndex)
                        result.Add("wave.order.invalid", "waveSets", waveSet.WaveSetId,
                            "waveIds[" + index + "]", "Wave indices must be strictly increasing.");
                    previousIndex = wave.index;
                    if (wave.enemyIds == null || wave.enemyIds.Length == 0)
                        result.Add("wave.enemy.empty", "waves", wave.id, "enemyIds",
                            "Referenced wave must contain at least one enemy.");
                    else
                    {
                        for (var enemyIndex = 0; enemyIndex < wave.enemyIds.Length; enemyIndex++)
                        {
                            var enemyId = wave.enemyIds[enemyIndex];
                            if (!battleContent.Enemies.ContainsKey(enemyId))
                                result.Add("wave.enemy.missing", "waves", wave.id,
                                    "enemyIds[" + enemyIndex + "]", "Missing enemy definition '" + enemyId + "'.");
                        }
                    }
                }
            }
        }

        private static void ValidateRuleSets(IReadOnlyList<LevelRuleSetDefinition> ruleSets,
            CompiledBattleContentCatalog battleContent, LevelCatalogValidationResult result)
        {
            foreach (var rules in ruleSets)
            {
                if (rules == null) continue;
                if (rules.InitialSun < 0 || rules.InitialLives <= 0 || rules.MaxWaves <= 0
                    || rules.InitialPotCount <= 0 || rules.BetweenWaveSeconds <= 0f
                    || rules.NurserySlotCount <= 0 || rules.RelocationCooldownSeconds < 0f
                    || rules.RefreshBaseCost < 0
                    || rules.RefreshCostStep < 0)
                    result.Add("rule.numeric.invalid", "ruleSets", rules.RuleSetId, "values",
                        "Rule values are outside supported battle bounds.");
                if (string.IsNullOrWhiteSpace(rules.NurseryProfileId)
                    || !battleContent.NurseryProfiles.ContainsKey(rules.NurseryProfileId))
                    result.Add("reference.missing", "ruleSets", rules.RuleSetId,
                        "nurseryProfileId", "Missing nursery profile '"
                        + rules.NurseryProfileId + "'.");

                var previousWave = 0;
                var seenWaves = new HashSet<int>();
                for (var index = 0; index < rules.MilestoneRewards.Count; index++)
                {
                    var milestone = rules.MilestoneRewards[index];
                    if (milestone.Wave <= previousWave || milestone.Wave <= 0
                        || milestone.Wave > rules.MaxWaves || !seenWaves.Add(milestone.Wave)
                        || milestone.PotCount < 0)
                        result.Add("rule.milestone.invalid", "ruleSets", rules.RuleSetId,
                            "milestoneRewards[" + index + "]",
                            "Milestones must be unique, strictly increasing, and within the configured wave count.");
                    previousWave = milestone.Wave;
                    for (var equipmentIndex = 0; equipmentIndex < milestone.EquipmentIds.Count; equipmentIndex++)
                    {
                        var equipmentId = milestone.EquipmentIds[equipmentIndex];
                        if (!battleContent.Equipment.ContainsKey(equipmentId))
                            result.Add("reference.missing", "ruleSets", rules.RuleSetId,
                                "milestoneRewards[" + index + "].equipmentIds[" + equipmentIndex + "]",
                                "Missing equipment definition '" + equipmentId + "'.");
                    }
                }
            }
        }

        private static void ValidateThemes(IReadOnlyList<LevelPresentationThemeDefinition> themes,
            IReadOnlyCollection<string> terrainPaletteIds,
            LevelCatalogValidationResult result)
        {
            foreach (var theme in themes)
            {
                if (theme == null) continue;
                if (string.IsNullOrWhiteSpace(theme.DisplayName))
                    result.Add("theme.incomplete", "themes", theme.ThemeId, "displayName",
                        "Theme display name must not be empty.");
                for (var index = 0; index < theme.RequiredColors.Count; index++)
                {
                    Color ignored;
                    var value = theme.RequiredColors[index];
                    if (string.IsNullOrWhiteSpace(value) || !ColorUtility.TryParseHtmlString(value, out ignored))
                        result.Add("theme.incomplete", "themes", theme.ThemeId,
                            "colors[" + index + "]", "Theme color is missing or invalid: '" + value + "'.");
                }
                RequireStableId(theme.TerrainPaletteId, "themes", theme.ThemeId,
                    "terrainPaletteId", result);
                if (!terrainPaletteIds.Contains(theme.TerrainPaletteId))
                    result.Add("reference.missing", "themes", theme.ThemeId, "terrainPaletteId",
                        "Theme references missing terrain palette '" + theme.TerrainPaletteId + "'.");
            }
        }

        private static IReadOnlyCollection<string> IndexIds(IReadOnlyList<string> values,
            string category, LevelCatalogValidationResult result)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < values.Count; index++)
            {
                var id = values[index] ?? string.Empty;
                RequireStableId(id, category, id, "ids[" + index + "]", result);
                if (!ids.Add(id))
                    result.Add("identity.duplicate", category, id, "ids[" + index + "]",
                        "Duplicate identity '" + id + "'.");
            }
            return ids;
        }

        private static void ValidateLevels(LevelCatalogSource source,
            IReadOnlyDictionary<string, LevelDefinition> levels,
            IReadOnlyDictionary<string, BattlefieldMapDefinition> maps,
            IReadOnlyDictionary<string, LevelWaveSetDefinition> waveSets,
            IReadOnlyDictionary<string, LevelRuleSetDefinition> ruleSets,
            IReadOnlyDictionary<string, LevelPresentationThemeDefinition> themes,
            LevelCatalogValidationResult result)
        {
            if (!levels.ContainsKey(source.DefaultLevelId))
                result.Add("reference.missing", "catalog", source.CatalogId, "defaultLevelId",
                    "Default level '" + source.DefaultLevelId + "' is not defined.");

            foreach (var level in source.Levels)
            {
                if (level == null) continue;
                BattlefieldMapDefinition map;
                LevelWaveSetDefinition waveSet;
                LevelRuleSetDefinition ruleSet;
                LevelPresentationThemeDefinition theme;
                var hasMap = maps.TryGetValue(level.MapId, out map);
                var hasWaves = waveSets.TryGetValue(level.WaveSetId, out waveSet);
                var hasRules = ruleSets.TryGetValue(level.RuleSetId, out ruleSet);
                var hasTheme = themes.TryGetValue(level.ThemeId, out theme);
                RequireStableId(level.GrowthPolicyId, "levels", level.LevelId,
                    "growthPolicyId", result);
                if (!hasMap) MissingLevelReference(level, "mapId", level.MapId, result);
                if (!hasWaves) MissingLevelReference(level, "waveSetId", level.WaveSetId, result);
                if (!hasRules) MissingLevelReference(level, "ruleSetId", level.RuleSetId, result);
                if (!hasTheme) MissingLevelReference(level, "themeId", level.ThemeId, result);
                if (!hasMap || !hasWaves || !hasRules || !hasTheme) continue;

                if (waveSet.WaveIds.Count != ruleSet.MaxWaves)
                    result.Add("wave.rule.count-mismatch", "levels", level.LevelId, "waveSetId",
                        "Wave set '" + waveSet.WaveSetId + "' has " + waveSet.WaveIds.Count
                        + " waves but rule set '" + ruleSet.RuleSetId + "' requires " + ruleSet.MaxWaves + ".");
                var authoredInitialPots = map.InitialPotGroupOrder.Sum(id => map.InitialPotGroups[id].InitialCount);
                if (authoredInitialPots != ruleSet.InitialPotCount)
                    result.Add("map.rule.pot-count-mismatch", "levels", level.LevelId, "ruleSetId",
                        "Map initial pot count " + authoredInitialPots + " does not match rule set count "
                        + ruleSet.InitialPotCount + ".");
            }
        }

        private static void MissingLevelReference(LevelDefinition level, string field,
            string value, LevelCatalogValidationResult result)
        {
            result.Add("reference.missing", "levels", level.LevelId, field,
                "Level references missing " + field + " '" + value + "'.");
        }

        private static Dictionary<string, T> Index<T>(IReadOnlyList<T> values,
            Func<T, string> getId, string category, string field,
            LevelCatalogValidationResult result) where T : class
        {
            var index = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var position = 0; position < values.Count; position++)
            {
                var value = values[position];
                if (value == null)
                {
                    result.Add("definition.null", category, string.Empty,
                        "[" + position + "]", "Definition is null.");
                    continue;
                }
                var id = getId(value) ?? string.Empty;
                RequireStableId(id, category, id, field, result);
                if (index.ContainsKey(id))
                    result.Add("identity.duplicate", category, id, field,
                        "Identity '" + id + "' is duplicated.");
                else index.Add(id, value);
            }
            return index;
        }

        private static void RequireStableId(string value, string category, string itemId,
            string field, LevelCatalogValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(value) && StableIdPattern.IsMatch(value)) return;
            result.Add("identity.invalid", category, itemId, field,
                "Identity must be a stable lowercase semantic ID.");
        }

        private static void RequireNonEmpty(int count, string category,
            LevelCatalogValidationResult result)
        {
            if (count > 0) return;
            result.Add("collection.empty", category, string.Empty, string.Empty,
                "Catalog collection must not be empty.");
        }
    }
}
