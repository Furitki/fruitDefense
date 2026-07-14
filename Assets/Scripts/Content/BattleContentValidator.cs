using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace FruitDefense.Content
{
    [Serializable]
    public sealed class ContentValidationIssue
    {
        public string code;
        public string category;
        public string itemId;
        public string field;
        public string message;

        public ContentValidationIssue(string code, string category, string itemId, string field, string message)
        {
            this.code = code;
            this.category = category;
            this.itemId = itemId;
            this.field = field;
            this.message = message;
        }

        public override string ToString()
        {
            return code + " [" + category + ":" + (string.IsNullOrEmpty(itemId) ? "<catalog>" : itemId)
                + "." + field + "] " + message;
        }
    }

    public sealed class ContentValidationResult
    {
        private readonly List<ContentValidationIssue> issues = new List<ContentValidationIssue>();
        private readonly ReadOnlyCollection<ContentValidationIssue> readOnlyIssues;

        public ContentValidationResult()
        {
            readOnlyIssues = issues.AsReadOnly();
        }

        public bool IsValid { get { return issues.Count == 0; } }
        public IReadOnlyList<ContentValidationIssue> Issues { get { return readOnlyIssues; } }

        internal void Add(string code, string category, string itemId, string field, string message)
        {
            issues.Add(new ContentValidationIssue(code, category, itemId, field, message));
        }

        internal void Append(ContentValidationResult other)
        {
            if (other == null) return;
            foreach (var issue in other.issues) issues.Add(issue);
        }
    }

    public static class BattleContentValidator
    {
        private static readonly Regex StableIdPattern = new Regex(
            "^[a-z][a-z0-9]*(?:[._-][a-z0-9]+)*$", RegexOptions.CultureInvariant);

        public static ContentValidationResult Validate(BattleContentCatalogDto catalog)
        {
            var result = new ContentValidationResult();
            if (catalog == null)
            {
                result.Add("catalog.null", "catalog", string.Empty, string.Empty, "Catalog is null.");
                return result;
            }

            ValidateHeader(catalog.header, result);
            RequireCollection(catalog.plants, "plants", result);
            RequireCollection(catalog.enemies, "enemies", result);
            RequireCollection(catalog.equipment, "equipment", result);
            RequireCollection(catalog.skills, "skills", result);
            RequireCollection(catalog.projectiles, "projectiles", result);
            RequireCollection(catalog.statuses, "statuses", result);
            RequireCollection(catalog.waves, "waves", result);
            RequireCollection(catalog.starTiers, "starTiers", result);

            var plantIds = ValidateIds(catalog.plants, "plants", value => value.id, result);
            var enemyIds = ValidateIds(catalog.enemies, "enemies", value => value.id, result);
            var equipmentIds = ValidateIds(catalog.equipment, "equipment", value => value.id, result);
            var skillIds = ValidateIds(catalog.skills, "skills", value => value.id, result);
            var projectileIds = ValidateIds(catalog.projectiles, "projectiles", value => value.id, result);
            var statusIds = ValidateIds(catalog.statuses, "statuses", value => value.id, result);
            ValidateIds(catalog.waves, "waves", value => value.id, result);
            ValidateIds(catalog.starTiers, "starTiers", value => value.id, result);

            ValidatePlants(catalog.plants, skillIds, projectileIds, equipmentIds, result);
            ValidateEnemies(catalog.enemies, result);
            ValidateEquipment(catalog.equipment, skillIds, statusIds, plantIds, result);
            ValidateSkills(catalog.skills, projectileIds, statusIds, result);
            ValidateProjectiles(catalog.projectiles, result);
            ValidateStatuses(catalog.statuses, result);
            ValidateWaves(catalog.waves, enemyIds, catalog.battleRules, result);
            ValidateStarTiers(catalog.starTiers, result);
            ValidateBattleRules(catalog.battleRules, equipmentIds, result);
            return result;
        }

        public static ContentValidationResult ValidateBundledBaseline(BattleContentCatalogDto catalog)
        {
            var result = Validate(catalog);
            if (catalog == null) return result;

            ExpectCount(catalog.plants, 5, "plants", result);
            ExpectCount(catalog.enemies, 4, "enemies", result);
            ExpectCount(catalog.equipment, 3, "equipment", result);
            ExpectCount(catalog.waves, 15, "waves", result);
            ExpectCount(catalog.starTiers, 4, "starTiers", result);

            RequireIds(catalog.plants, value => value.id, "plants", new[]
            {
                BattleContentIds.Plants.Pea, BattleContentIds.Plants.Watermelon, BattleContentIds.Plants.Banana,
                BattleContentIds.Plants.Durian, BattleContentIds.Plants.Sunflower,
            }, result);
            RequireIds(catalog.enemies, value => value.id, "enemies", new[]
            {
                BattleContentIds.Enemies.Normal, BattleContentIds.Enemies.Runner,
                BattleContentIds.Enemies.Armored, BattleContentIds.Enemies.Boss,
            }, result);
            RequireIds(catalog.equipment, value => value.id, "equipment", new[]
            {
                BattleContentIds.Equipment.Gatling, BattleContentIds.Equipment.Ice, BattleContentIds.Equipment.Chili,
            }, result);

            if (catalog.header != null && catalog.header.catalogId != BattleContentSchema.BundledCatalogId)
                result.Add("bundled.header.mismatch", "header", string.Empty, "catalogId", "Bundled catalog ID does not match the required baseline.");
            if (catalog.battleRules == null || catalog.battleRules.maxWaves != 15)
                result.Add("bundled.rules.mismatch", "battleRules", string.Empty, "maxWaves", "Bundled rules must contain fifteen waves.");
            return result;
        }

        private static void ValidateHeader(BattleContentHeaderDto header, ContentValidationResult result)
        {
            if (header == null)
            {
                result.Add("header.missing", "header", string.Empty, string.Empty, "Catalog header is required.");
                return;
            }
            if (header.schemaVersion != BattleContentSchema.CurrentSchemaVersion)
                result.Add("header.schema.unsupported", "header", string.Empty, "schemaVersion", "Unsupported schema version '" + header.schemaVersion + "'.");
            RequireText(header.catalogId, "header", string.Empty, "catalogId", result);
            RequireText(header.contentVersion, "header", string.Empty, "contentVersion", result);
            RequireText(header.minCodeVersion, "header", string.Empty, "minCodeVersion", result);
        }

        private static void ValidatePlants(PlantDefinitionDto[] values, HashSet<string> skillIds,
            HashSet<string> projectileIds, HashSet<string> equipmentIds, ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                RequireText(value.displayName, "plants", value.id, "displayName", result);
                RequireFiniteAtLeast(value.damage, 0f, "plants", value.id, "damage", result);
                RequireFiniteGreater(value.attackIntervalSeconds, 0f, "plants", value.id, "attackIntervalSeconds", result);
                RequireFiniteAtLeast(value.rangeLegacyUnits, 0f, "plants", value.id, "rangeLegacyUnits", result);
                RequireReferences(value.skillIds, skillIds, "plants", value.id, "skillIds", true, result);
                RequireOptionalReference(value.projectileId, projectileIds, "plants", value.id, "projectileId", result);
                RequireReferences(value.allowedEquipmentIds, equipmentIds, "plants", value.id, "allowedEquipmentIds", false, result);
            }
        }

        private static void ValidateEnemies(EnemyDefinitionDto[] values, ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                RequireText(value.displayName, "enemies", value.id, "displayName", result);
                RequireFiniteGreater(value.health, 0f, "enemies", value.id, "health", result);
                RequireFiniteGreater(value.speedLegacyUnits, 0f, "enemies", value.id, "speedLegacyUnits", result);
                RequireIntAtLeast(value.killReward, 0, "enemies", value.id, "killReward", result);
                RequireIntAtLeast(value.threat, 1, "enemies", value.id, "threat", result);
            }
        }

        private static void ValidateEquipment(EquipmentDefinitionDto[] values, HashSet<string> skillIds,
            HashSet<string> statusIds, HashSet<string> plantIds, ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                RequireText(value.displayName, "equipment", value.id, "displayName", result);
                RequireReferences(value.skillIds, skillIds, "equipment", value.id, "skillIds", true, result);
                RequireReferences(value.statusIds, statusIds, "equipment", value.id, "statusIds", false, result);
                RequireReferences(value.compatiblePlantIds, plantIds, "equipment", value.id, "compatiblePlantIds", true, result);
            }
        }

        private static void ValidateSkills(SkillDefinitionDto[] values, HashSet<string> projectileIds,
            HashSet<string> statusIds, ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                RequireStableReferenceName(value.triggerId, "skills", value.id, "triggerId", result);
                RequireStableReferenceName(value.targetId, "skills", value.id, "targetId", result);
                RequireOptionalReference(value.projectileId, projectileIds, "skills", value.id, "projectileId", result);
                RequireOptionalReference(value.statusId, statusIds, "skills", value.id, "statusId", result);
                RequireFiniteAtLeast(value.cooldownSeconds, 0f, "skills", value.id, "cooldownSeconds", result);
                RequireFiniteAtLeast(value.damageMultiplier, 0f, "skills", value.id, "damageMultiplier", result);
                RequireIntAtLeast(value.resourceAmount, 0, "skills", value.id, "resourceAmount", result);
                RequireIntAtLeast(value.burstCount, 1, "skills", value.id, "burstCount", result);
                RequireFiniteAtLeast(value.burstIntervalSeconds, 0f, "skills", value.id, "burstIntervalSeconds", result);
            }
        }

        private static void ValidateProjectiles(ProjectileDefinitionDto[] values, ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                RequireStableReferenceName(value.travelMode, "projectiles", value.id, "travelMode", result);
                RequireFiniteAtLeast(value.speedLegacyUnits, 0f, "projectiles", value.id, "speedLegacyUnits", result);
                RequireFiniteAtLeast(value.flightSeconds, 0f, "projectiles", value.id, "flightSeconds", result);
                if (value.speedLegacyUnits <= 0f && value.flightSeconds <= 0f)
                    result.Add("definition.numeric.invalid", "projectiles", value.id, "speedLegacyUnits", "Projectile requires speed or flight time.");
                RequireFiniteAtLeast(value.blastRadiusLegacyUnits, 0f, "projectiles", value.id, "blastRadiusLegacyUnits", result);
                RequireFiniteGreater(value.rangeMultiplier, 0f, "projectiles", value.id, "rangeMultiplier", result);
                RequireFiniteGreater(value.hitRadiusLegacyUnits, 0f, "projectiles", value.id, "hitRadiusLegacyUnits", result);
                RequireIntAtLeast(value.maxHitsPerTarget, 1, "projectiles", value.id, "maxHitsPerTarget", result);
            }
        }

        private static void ValidateStatuses(StatusDefinitionDto[] values, ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                RequireStableReferenceName(value.stackingMode, "statuses", value.id, "stackingMode", result);
                RequireFiniteGreater(value.durationSeconds, 0f, "statuses", value.id, "durationSeconds", result);
                RequireFiniteAtLeast(value.tickIntervalSeconds, 0f, "statuses", value.id, "tickIntervalSeconds", result);
                RequireFiniteAtLeast(value.magnitude, 0f, "statuses", value.id, "magnitude", result);
                RequireIntAtLeast(value.maxStacks, 1, "statuses", value.id, "maxStacks", result);
                RequireIntAtLeast(value.hitsToProc, 0, "statuses", value.id, "hitsToProc", result);
            }
        }

        private static void ValidateWaves(WaveDefinitionDto[] values, HashSet<string> enemyIds,
            BattleRulesDto rules, ContentValidationResult result)
        {
            if (values == null) return;
            var indexes = new HashSet<int>();
            foreach (var value in values)
            {
                if (value == null) continue;
                if (value.index <= 0 || !indexes.Add(value.index))
                    result.Add("wave.order.invalid", "waves", value.id, "index", "Wave indexes must be unique positive values.");
                RequireFiniteGreater(value.healthMultiplier, 0f, "waves", value.id, "healthMultiplier", result);
                RequireFiniteGreater(value.speedMultiplier, 0f, "waves", value.id, "speedMultiplier", result);
                RequireFiniteGreater(value.spawnIntervalSeconds, 0f, "waves", value.id, "spawnIntervalSeconds", result);
                RequireIntAtLeast(value.completionReward, 0, "waves", value.id, "completionReward", result);
                RequireSequenceReferences(value.enemyIds, enemyIds, "waves", value.id, "enemyIds", result);
            }
            var expected = rules == null ? values.Length : rules.maxWaves;
            for (var index = 1; index <= expected; index++)
                if (!indexes.Contains(index)) result.Add("wave.order.invalid", "waves", "wave." + index, "index", "Missing ordered wave index " + index + ".");
        }

        private static void ValidateStarTiers(StarTierDefinitionDto[] values, ContentValidationResult result)
        {
            if (values == null) return;
            var stars = new HashSet<int>();
            foreach (var value in values)
            {
                if (value == null) continue;
                if (value.star <= 0 || !stars.Add(value.star))
                    result.Add("star.duplicate", "starTiers", value.id, "star", "Star levels must be unique positive values.");
                RequireFiniteGreater(value.damageMultiplier, 0f, "starTiers", value.id, "damageMultiplier", result);
                RequireFiniteGreater(value.attackSpeedMultiplier, 0f, "starTiers", value.id, "attackSpeedMultiplier", result);
                RequireFiniteGreater(value.rangeMultiplier, 0f, "starTiers", value.id, "rangeMultiplier", result);
            }
        }

        private static void ValidateBattleRules(BattleRulesDto rules, HashSet<string> equipmentIds,
            ContentValidationResult result)
        {
            if (rules == null)
            {
                result.Add("collection.required", "battleRules", string.Empty, string.Empty, "Battle rules are required.");
                return;
            }
            ValidateStableId(rules.id, "battleRules", rules.id, 0, result, null);
            RequireIntAtLeast(rules.initialSun, 0, "battleRules", rules.id, "initialSun", result);
            RequireIntAtLeast(rules.initialLives, 1, "battleRules", rules.id, "initialLives", result);
            RequireIntAtLeast(rules.maxWaves, 1, "battleRules", rules.id, "maxWaves", result);
            RequireIntAtLeast(rules.initialPotCount, 1, "battleRules", rules.id, "initialPotCount", result);
            RequireFiniteGreater(rules.betweenWaveSeconds, 0f, "battleRules", rules.id, "betweenWaveSeconds", result);
            RequireIntAtLeast(rules.nurserySlotCount, 1, "battleRules", rules.id, "nurserySlotCount", result);
            RequireFiniteRange(rules.nurseryPotChance, 0f, 1f, "battleRules", rules.id, "nurseryPotChance", result);
            RequireIntAtLeast(rules.refreshBaseCost, 0, "battleRules", rules.id, "refreshBaseCost", result);
            RequireIntAtLeast(rules.refreshCostStep, 0, "battleRules", rules.id, "refreshCostStep", result);

            if (rules.milestoneRewards == null) return;
            var waves = new HashSet<int>();
            foreach (var reward in rules.milestoneRewards)
            {
                if (reward == null)
                {
                    result.Add("definition.null", "milestoneRewards", string.Empty, string.Empty, "Milestone reward entry is null.");
                    continue;
                }
                if (reward.wave <= 0 || reward.wave > rules.maxWaves || !waves.Add(reward.wave))
                    result.Add("reward.wave.invalid", "milestoneRewards", "wave." + reward.wave, "wave", "Milestone wave must be unique and inside battle bounds.");
                RequireIntAtLeast(reward.potCount, 0, "milestoneRewards", "wave." + reward.wave, "potCount", result);
                RequireReferences(reward.equipmentIds, equipmentIds, "milestoneRewards", "wave." + reward.wave, "equipmentIds", true, result);
            }
        }

        private static HashSet<string> ValidateIds<T>(T[] values, string category, Func<T, string> getId,
            ContentValidationResult result) where T : class
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (values == null) return ids;
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index];
                if (value == null)
                {
                    result.Add("definition.null", category, "#" + index, string.Empty, "Definition entry is null.");
                    continue;
                }
                ValidateStableId(getId(value), category, getId(value), index, result, ids);
            }
            return ids;
        }

        private static void ValidateStableId(string id, string category, string itemId, int index,
            ContentValidationResult result, HashSet<string> ids)
        {
            if (string.IsNullOrEmpty(id) || !StableIdPattern.IsMatch(id))
            {
                result.Add("definition.id.invalid", category, string.IsNullOrEmpty(itemId) ? "#" + index : itemId,
                    "id", "ID must be lowercase ASCII segments separated by '.', '-' or '_'.");
                return;
            }
            if (ids != null && !ids.Add(id))
                result.Add("definition.id.duplicate", category, id, "id", "ID is duplicated in this category.");
        }

        private static void RequireStableReferenceName(string value, string category, string id, string field,
            ContentValidationResult result)
        {
            if (string.IsNullOrEmpty(value) || !StableIdPattern.IsMatch(value))
                result.Add("reference.id.invalid", category, id, field, "Reference name must use the stable ID format.");
        }

        private static void RequireReferences(string[] values, HashSet<string> targets, string category,
            string id, string field, bool requireAtLeastOne, ContentValidationResult result)
        {
            if (values == null || values.Length == 0)
            {
                if (requireAtLeastOne) result.Add("reference.required", category, id, field, "At least one reference is required.");
                return;
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value) || !targets.Contains(value))
                    result.Add("reference.missing", category, id, field, "Referenced ID '" + value + "' does not exist.");
                else if (!seen.Add(value))
                    result.Add("reference.duplicate", category, id, field, "Referenced ID '" + value + "' is duplicated.");
            }
        }

        private static void RequireOptionalReference(string value, HashSet<string> targets, string category,
            string id, string field, ContentValidationResult result)
        {
            if (!string.IsNullOrEmpty(value) && !targets.Contains(value))
                result.Add("reference.missing", category, id, field, "Referenced ID '" + value + "' does not exist.");
        }

        private static void RequireSequenceReferences(string[] values, HashSet<string> targets, string category,
            string id, string field, ContentValidationResult result)
        {
            if (values == null || values.Length == 0)
            {
                result.Add("reference.required", category, id, field, "At least one sequence entry is required.");
                return;
            }
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index];
                if (string.IsNullOrEmpty(value) || !targets.Contains(value))
                    result.Add("reference.missing", category, id, field + "[" + index + "]",
                        "Referenced ID '" + value + "' does not exist.");
            }
        }

        private static void RequireCollection<T>(T[] values, string category, ContentValidationResult result)
        {
            if (values == null || values.Length == 0)
                result.Add("collection.required", category, string.Empty, string.Empty, "At least one definition is required.");
        }

        private static void ExpectCount<T>(T[] values, int count, string category, ContentValidationResult result)
        {
            var actual = values == null ? 0 : values.Length;
            if (actual != count) result.Add("bundled.count.mismatch", category, string.Empty, string.Empty,
                "Expected " + count + " definitions but found " + actual + ".");
        }

        private static void RequireIds<T>(T[] values, Func<T, string> getId, string category,
            string[] requiredIds, ContentValidationResult result) where T : class
        {
            var actual = new HashSet<string>(StringComparer.Ordinal);
            if (values != null)
                foreach (var value in values) if (value != null) actual.Add(getId(value));
            foreach (var id in requiredIds)
                if (!actual.Contains(id)) result.Add("bundled.id.missing", category, id, "id", "Required bundled definition is missing.");
        }

        private static void RequireText(string value, string category, string id, string field, ContentValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value)) result.Add("definition.text.required", category, id, field, "Value is required.");
        }

        private static void RequireFiniteGreater(float value, float minimum, string category, string id,
            string field, ContentValidationResult result)
        {
            if (!IsFinite(value) || value <= minimum)
                result.Add("definition.numeric.invalid", category, id, field, "Value must be finite and greater than " + minimum + ".");
        }

        private static void RequireFiniteAtLeast(float value, float minimum, string category, string id,
            string field, ContentValidationResult result)
        {
            if (!IsFinite(value) || value < minimum)
                result.Add("definition.numeric.invalid", category, id, field, "Value must be finite and at least " + minimum + ".");
        }

        private static void RequireFiniteRange(float value, float minimum, float maximum, string category,
            string id, string field, ContentValidationResult result)
        {
            if (!IsFinite(value) || value < minimum || value > maximum)
                result.Add("definition.numeric.invalid", category, id, field, "Value must be between " + minimum + " and " + maximum + ".");
        }

        private static void RequireIntAtLeast(int value, int minimum, string category, string id,
            string field, ContentValidationResult result)
        {
            if (value < minimum) result.Add("definition.numeric.invalid", category, id, field, "Value must be at least " + minimum + ".");
        }

        private static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
