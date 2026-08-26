using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public ContentValidationResult() { readOnlyIssues = issues.AsReadOnly(); }
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
            RequireCollection(catalog.abilities, "abilities", result);
            RequireCollection(catalog.projectiles, "projectiles", result);
            RequireCollection(catalog.statuses, "statuses", result);
            RequireCollection(catalog.waves, "waves", result);
            RequireCollection(catalog.starTiers, "starTiers", result);

            var plantIds = ValidateIds(catalog.plants, "plants", value => value.id, result);
            var enemyIds = ValidateIds(catalog.enemies, "enemies", value => value.id, result);
            var equipmentIds = ValidateIds(catalog.equipment, "equipment", value => value.id, result);
            var abilityIds = ValidateIds(catalog.abilities, "abilities", value => value.id, result);
            var projectileIds = ValidateIds(catalog.projectiles, "projectiles", value => value.id, result);
            var statusIds = ValidateIds(catalog.statuses, "statuses", value => value.id, result);
            ValidateIds(catalog.waves, "waves", value => value.id, result);
            ValidateIds(catalog.starTiers, "starTiers", value => value.id, result);

            ValidatePlants(catalog.plants, abilityIds, equipmentIds, result);
            ValidateEnemies(catalog.enemies, abilityIds, result);
            ValidateEquipment(catalog.equipment, abilityIds, plantIds, result);
            ValidateAbilities(catalog.abilities, projectileIds, statusIds, result);
            ValidateAbilityExecutionSupport(catalog, result);
            ValidateProjectiles(catalog.projectiles, result);
            ValidateStatuses(catalog.statuses, statusIds, result);
            ValidateStatusProcCycles(catalog.statuses, result);
            ValidateEquipmentAbilityBindings(catalog, result);
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
                BattleContentIds.Plants.Pea, BattleContentIds.Plants.Watermelon,
                BattleContentIds.Plants.Banana, BattleContentIds.Plants.Durian,
                BattleContentIds.Plants.Sunflower,
            }, result);
            RequireIds(catalog.enemies, value => value.id, "enemies", new[]
            {
                BattleContentIds.Enemies.Normal, BattleContentIds.Enemies.Runner,
                BattleContentIds.Enemies.Armored, BattleContentIds.Enemies.Boss,
            }, result);
            RequireIds(catalog.equipment, value => value.id, "equipment", new[]
            {
                BattleContentIds.Equipment.Gatling, BattleContentIds.Equipment.Ice,
                BattleContentIds.Equipment.Chili,
            }, result);
            RequireIds(catalog.abilities, value => value.id, "abilities", new[]
            {
                BattleContentIds.Abilities.PeaAttack, BattleContentIds.Abilities.WatermelonAttack,
                BattleContentIds.Abilities.BananaAttack, BattleContentIds.Abilities.DurianAttack,
                BattleContentIds.Abilities.SunflowerProduce, BattleContentIds.Abilities.IceOnHit,
                BattleContentIds.Abilities.IceProducerOpening, BattleContentIds.Abilities.ChiliOnHit,
            }, result);
            if (catalog.header != null && catalog.header.catalogId != BattleContentSchema.BundledCatalogId)
                result.Add("bundled.header.mismatch", "header", string.Empty, "catalogId",
                    "Bundled catalog ID does not match the required baseline.");
            if (catalog.battleRules == null || catalog.battleRules.maxWaves != 15)
                result.Add("bundled.rules.mismatch", "battleRules", string.Empty, "maxWaves",
                    "Bundled rules must contain fifteen waves.");
            return result;
        }

        private static void ValidateHeader(BattleContentHeaderDto header, ContentValidationResult result)
        {
            if (header == null)
            {
                result.Add("header.missing", "header", string.Empty, string.Empty,
                    "Catalog header is required.");
                return;
            }
            if (header.schemaVersion != BattleContentSchema.CurrentSchemaVersion)
                result.Add("header.schema.unsupported", "header", string.Empty, "schemaVersion",
                    "Unsupported schema version '" + header.schemaVersion + "'.");
            RequireText(header.catalogId, "header", string.Empty, "catalogId", result);
            RequireText(header.contentVersion, "header", string.Empty, "contentVersion", result);
            RequireText(header.minCodeVersion, "header", string.Empty, "minCodeVersion", result);
        }

        private static void ValidatePlants(PlantDefinitionDto[] values, HashSet<string> abilityIds,
            HashSet<string> equipmentIds, ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                RequireText(value.displayName, "plants", value.id, "displayName", result);
                RequireFiniteAtLeast(value.damage, 0f, "plants", value.id, "damage", result);
                RequireFiniteGreater(value.attackIntervalSeconds, 0f, "plants", value.id,
                    "attackIntervalSeconds", result);
                RequireFiniteAtLeast(value.rangeLegacyUnits, 0f, "plants", value.id,
                    "rangeLegacyUnits", result);
                RequireFiniteAtLeast(value.potVisualHeightOffset, 0f, "plants", value.id,
                    "potVisualHeightOffset", result);
                RequireReferences(value.abilityIds, abilityIds, "plants", value.id,
                    "abilityIds", true, result);
                RequireReferences(value.allowedEquipmentIds, equipmentIds, "plants", value.id,
                    "allowedEquipmentIds", false, result);
                RequireStableNames(value.tags, "plants", value.id, "tags", true, result);
            }
        }

        private static void ValidateEnemies(EnemyDefinitionDto[] values, HashSet<string> abilityIds,
            ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                RequireText(value.displayName, "enemies", value.id, "displayName", result);
                RequireFiniteGreater(value.health, 0f, "enemies", value.id, "health", result);
                RequireFiniteGreater(value.speedLegacyUnits, 0f, "enemies", value.id,
                    "speedLegacyUnits", result);
                RequireIntAtLeast(value.killReward, 0, "enemies", value.id, "killReward", result);
                RequireIntAtLeast(value.threat, 1, "enemies", value.id, "threat", result);
                RequireReferences(value.abilityIds, abilityIds, "enemies", value.id,
                    "abilityIds", false, result);
                RequireStableNames(value.tags, "enemies", value.id, "tags", true, result);
            }
        }

        private static void ValidateEquipment(EquipmentDefinitionDto[] values,
            HashSet<string> abilityIds, HashSet<string> plantIds, ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                RequireText(value.displayName, "equipment", value.id, "displayName", result);
                RequireReferences(value.compatiblePlantIds, plantIds, "equipment", value.id,
                    "compatiblePlantIds", true, result);
                if (value.grants == null)
                    result.Add("collection.required", "equipment", value.id, "grants",
                        "Grant collection cannot be null.");
                else foreach (var grant in value.grants)
                {
                    if (grant == null)
                    {
                        result.Add("definition.null", "equipment", value.id, "grants",
                            "Grant entry is null.");
                        continue;
                    }
                    RequireOptionalReference(grant.abilityId, abilityIds, "equipment", value.id,
                        "grants.abilityId", result);
                    if (string.IsNullOrEmpty(grant.abilityId))
                        result.Add("reference.required", "equipment", value.id, "grants.abilityId",
                            "Granted Ability is required.");
                    RequireOptionalStableName(grant.requiredPlantTag, "equipment", value.id,
                        "grants.requiredPlantTag", result);
                }
                if (value.modifiers == null)
                {
                    result.Add("collection.required", "equipment", value.id, "modifiers",
                        "Modifier collection cannot be null.");
                    continue;
                }
                foreach (var modifier in value.modifiers)
                {
                    if (modifier == null)
                    {
                        result.Add("definition.null", "equipment", value.id, "modifiers",
                            "Modifier entry is null.");
                        continue;
                    }
                    RequireStableReferenceName(modifier.id, "equipment", value.id,
                        "modifiers.id", result);
                    RequireOptionalStableName(modifier.requiredPlantTag, "equipment", value.id,
                        "modifiers.requiredPlantTag", result);
                    RequireOptionalReference(modifier.targetAbilityId, abilityIds, "equipment", value.id,
                        "modifiers.targetAbilityId", result);
                    RequireOptionalStableName(modifier.targetAbilityTag, "equipment", value.id,
                        "modifiers.targetAbilityTag", result);
                    if (string.IsNullOrEmpty(modifier.targetAbilityId)
                        == string.IsNullOrEmpty(modifier.targetAbilityTag))
                        result.Add("modifier.selector.invalid", "equipment", value.id, modifier.id,
                            "Modifier must select by exactly one Ability ID or tag.");
                    if (!BattleAbilityCompiler.SupportsModifierAttribute(modifier.attributeId))
                        result.Add("mechanism.unknown", "equipment", value.id,
                            "modifiers.attributeId", "Unsupported Ability modifier attribute '"
                            + modifier.attributeId + "'.");
                    if (!BattleAbilityCompiler.SupportsModifierOperation(modifier.operationId))
                        result.Add("mechanism.unknown", "equipment", value.id,
                            "modifiers.operationId", "Unsupported Ability modifier operation '"
                            + modifier.operationId + "'.");
                    RequireFinite(modifier.value, "equipment", value.id, "modifiers.value", result);
                }
            }
        }

        private static void ValidateAbilities(AbilityDefinitionDto[] values,
            HashSet<string> projectileIds, HashSet<string> statusIds, ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                if (value.activation == null)
                {
                    result.Add("definition.required", "abilities", value.id, "activation",
                        "Ability activation is required.");
                    continue;
                }
                if (!BattleAbilityCompiler.SupportsActivation(value.activation.kindId))
                    result.Add("mechanism.unknown", "abilities", value.id, "activation.kindId",
                        "Unsupported activation '" + value.activation.kindId + "'.");
                if (!BattleAbilityCompiler.SupportsOwnerRole(value.activation.ownerRoleId))
                    result.Add("mechanism.unknown", "abilities", value.id, "activation.ownerRoleId",
                        "Unsupported owner role '" + value.activation.ownerRoleId + "'.");
                RequireFiniteAtLeast(value.activation.cooldownSeconds, 0f, "abilities", value.id,
                    "activation.cooldownSeconds", result);
                RequireFiniteAtLeast(value.activation.periodSeconds, 0f, "abilities", value.id,
                    "activation.periodSeconds", result);
                if (value.activation.kindId == "activation.cooldown"
                    && value.activation.cooldownSeconds <= 0f)
                    result.Add("activation.timing.invalid", "abilities", value.id,
                        "activation.cooldownSeconds", "Cooldown activation requires a positive cooldown.");
                if (value.activation.kindId == "activation.periodic"
                    && value.activation.periodSeconds <= 0f)
                    result.Add("activation.timing.invalid", "abilities", value.id,
                        "activation.periodSeconds", "Periodic activation requires a positive period.");
                if (value.activation.kindId == "activation.combat-event")
                {
                    if (!BattleAbilityCompiler.SupportsEvent(value.activation.eventId))
                        result.Add("mechanism.unknown", "abilities", value.id, "activation.eventId",
                            "Unsupported combat event '" + value.activation.eventId + "'.");
                }
                else if (!string.IsNullOrEmpty(value.activation.eventId))
                    result.Add("activation.event.invalid", "abilities", value.id, "activation.eventId",
                        "Only combat-event activation may declare an event.");

                if (value.timeline == null)
                    result.Add("definition.required", "abilities", value.id, "timeline",
                        "Ability timeline is required.");
                else
                {
                    RequireFiniteAtLeast(value.timeline.windupSeconds, 0f, "abilities", value.id,
                        "timeline.windupSeconds", result);
                    RequireFiniteAtLeast(value.timeline.recoverySeconds, 0f, "abilities", value.id,
                        "timeline.recoverySeconds", result);
                }
                RequireFiniteAtLeast(value.damageMultiplier, 0f, "abilities", value.id,
                    "damageMultiplier", result);
                RequireIntAtLeast(value.burstCount, 1, "abilities", value.id, "burstCount", result);
                RequireFiniteAtLeast(value.burstIntervalSeconds, 0f, "abilities", value.id,
                    "burstIntervalSeconds", result);
                if (value.burstCount > 1 && value.burstIntervalSeconds <= 0f)
                    result.Add("ability.burst.invalid", "abilities", value.id, "burstIntervalSeconds",
                        "Burst Ability requires a positive interval.");
                RequireStableNames(value.tags, "abilities", value.id, "tags", true, result);
                if (value.deliveries == null || value.deliveries.Length == 0)
                {
                    result.Add("collection.required", "abilities", value.id, "deliveries",
                        "At least one delivery is required.");
                    continue;
                }
                for (var index = 0; index < value.deliveries.Length; index++)
                    ValidateDelivery(value, value.deliveries[index], index, projectileIds, statusIds, result);
            }
        }

        private static void ValidateDelivery(AbilityDefinitionDto ability,
            AbilityDeliveryDefinitionDto delivery, int index, HashSet<string> projectileIds,
            HashSet<string> statusIds, ContentValidationResult result)
        {
            var field = "deliveries[" + index + "]";
            if (delivery == null)
            {
                result.Add("definition.null", "abilities", ability.id, field,
                    "Delivery entry is null.");
                return;
            }
            if (!BattleAbilityCompiler.SupportsTarget(delivery.targetId))
                result.Add("mechanism.unknown", "abilities", ability.id, field + ".targetId",
                    "Unsupported target selector '" + delivery.targetId + "'.");
            if (!BattleAbilityCompiler.SupportsDelivery(delivery.modeId))
                result.Add("mechanism.unknown", "abilities", ability.id, field + ".modeId",
                    "Unsupported delivery mode '" + delivery.modeId + "'.");
            RequireFiniteAtLeast(delivery.radiusLegacyUnits, 0f, "abilities", ability.id,
                field + ".radiusLegacyUnits", result);
            if (delivery.targetId == "target.area" && delivery.radiusLegacyUnits <= 0f)
                result.Add("delivery.radius.invalid", "abilities", ability.id,
                    field + ".radiusLegacyUnits", "Area delivery requires a positive authored radius.");
            if (delivery.modeId == "delivery.projectile")
            {
                RequireOptionalReference(delivery.projectileId, projectileIds, "abilities", ability.id,
                    field + ".projectileId", result);
                if (string.IsNullOrEmpty(delivery.projectileId))
                    result.Add("reference.required", "abilities", ability.id, field + ".projectileId",
                        "Projectile delivery requires a projectile.");
            }
            else if (!string.IsNullOrEmpty(delivery.projectileId))
                result.Add("delivery.projectile.invalid", "abilities", ability.id,
                    field + ".projectileId", "Instant delivery cannot declare a projectile.");
            if (delivery.payload == null || delivery.payload.Length == 0)
            {
                result.Add("collection.required", "abilities", ability.id, field + ".payload",
                    "Delivery payload cannot be empty.");
                return;
            }
            for (var payloadIndex = 0; payloadIndex < delivery.payload.Length; payloadIndex++)
            {
                var effect = delivery.payload[payloadIndex];
                var effectField = field + ".payload[" + payloadIndex + "]";
                if (effect == null)
                {
                    result.Add("definition.null", "abilities", ability.id, effectField,
                        "Payload effect is null.");
                    continue;
                }
                if (!BattleAbilityCompiler.SupportsEffect(effect.kindId))
                    result.Add("mechanism.unknown", "abilities", ability.id, effectField + ".kindId",
                        "Unsupported payload effect '" + effect.kindId + "'.");
                RequireFiniteAtLeast(effect.magnitude, 0f, "abilities", ability.id,
                    effectField + ".magnitude", result);
                RequireIntAtLeast(effect.resourceAmount, 0, "abilities", ability.id,
                    effectField + ".resourceAmount", result);
                if (effect.kindId == "effect.apply-status")
                {
                    RequireOptionalReference(effect.statusId, statusIds, "abilities", ability.id,
                        effectField + ".statusId", result);
                    if (string.IsNullOrEmpty(effect.statusId))
                        result.Add("reference.required", "abilities", ability.id,
                            effectField + ".statusId", "Status effect requires a status.");
                }
                else if (!string.IsNullOrEmpty(effect.statusId))
                    result.Add("effect.status.invalid", "abilities", ability.id,
                        effectField + ".statusId", "Only status effects may declare a status.");
                if (effect.kindId == "effect.grant-resource" && effect.resourceAmount <= 0)
                    result.Add("effect.resource.invalid", "abilities", ability.id,
                        effectField + ".resourceAmount", "Resource effect requires a positive amount.");
            }
        }

        private static void ValidateProjectiles(ProjectileDefinitionDto[] values,
            ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                if (!BattleAbilityCompiler.SupportsProjectileMode(value.travelMode))
                    result.Add("mechanism.unknown", "projectiles", value.id, "travelMode",
                        "Unsupported projectile mode '" + value.travelMode + "'.");
                RequireFiniteAtLeast(value.speedLegacyUnits, 0f, "projectiles", value.id,
                    "speedLegacyUnits", result);
                RequireFiniteAtLeast(value.flightSeconds, 0f, "projectiles", value.id,
                    "flightSeconds", result);
                if (value.travelMode == "travel.timed-arc" && value.flightSeconds <= 0f)
                    result.Add("projectile.timing.invalid", "projectiles", value.id, "flightSeconds",
                        "Timed arc projectile requires positive flight time.");
                if (value.travelMode != "travel.timed-arc" && value.speedLegacyUnits <= 0f)
                    result.Add("projectile.speed.invalid", "projectiles", value.id,
                        "speedLegacyUnits", "Moving projectile requires positive speed.");
                RequireFiniteGreater(value.rangeMultiplier, 0f, "projectiles", value.id,
                    "rangeMultiplier", result);
                RequireFiniteGreater(value.hitRadiusLegacyUnits, 0f, "projectiles", value.id,
                    "hitRadiusLegacyUnits", result);
                RequireIntAtLeast(value.maxHitsPerTarget, 1, "projectiles", value.id,
                    "maxHitsPerTarget", result);
            }
        }

        private static void ValidateStatuses(StatusDefinitionDto[] values, HashSet<string> statusIds,
            ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                if (!BattleAbilityCompiler.SupportsStackMode(value.stackingMode))
                    result.Add("mechanism.unknown", "statuses", value.id, "stackingMode",
                        "Unsupported stacking mode '" + value.stackingMode + "'.");
                if (!BattleAbilityCompiler.SupportsStatusKind(value.kindId))
                    result.Add("mechanism.unknown", "statuses", value.id, "kindId",
                        "Unsupported status kind '" + value.kindId + "'.");
                RequireFiniteGreater(value.durationSeconds, 0f, "statuses", value.id,
                    "durationSeconds", result);
                RequireFiniteAtLeast(value.tickIntervalSeconds, 0f, "statuses", value.id,
                    "tickIntervalSeconds", result);
                RequireFiniteAtLeast(value.magnitude, 0f, "statuses", value.id, "magnitude", result);
                RequireIntAtLeast(value.maxStacks, 1, "statuses", value.id, "maxStacks", result);
                RequireIntAtLeast(value.hitsToProc, 0, "statuses", value.id, "hitsToProc", result);
                RequireOptionalReference(value.procStatusId, statusIds, "statuses", value.id,
                    "procStatusId", result);
                if (!CombatFrameworkCompiler.SupportsPolarity(value.polarityId))
                    result.Add("mechanism.unknown", "statuses", value.id, "polarityId",
                        "Unsupported status polarity '" + value.polarityId + "'.");
                if (!CombatFrameworkCompiler.SupportsPeriodicEffect(value.periodicEffectId))
                    result.Add("mechanism.unknown", "statuses", value.id, "periodicEffectId",
                        "Unsupported periodic effect '" + value.periodicEffectId + "'.");
                RequireStableNames(value.tags, "statuses", value.id, "tags", true, result);
                if (value.periodicEffectId != "periodic.none" && value.tickIntervalSeconds <= 0f)
                    result.Add("status.periodic.invalid", "statuses", value.id,
                        "tickIntervalSeconds", "Periodic status requires a positive tick interval.");
                if (value.stackingMode == "stacking.proc-after-hits"
                    && (value.hitsToProc <= 0 || string.IsNullOrEmpty(value.procStatusId)))
                    result.Add("status.proc.invalid", "statuses", value.id, "procStatusId",
                        "Hit-count status requires hitsToProc and procStatusId.");
                else if (value.stackingMode != "stacking.proc-after-hits"
                    && !string.IsNullOrEmpty(value.procStatusId))
                    result.Add("status.proc.invalid", "statuses", value.id, "procStatusId",
                        "Only hit-count statuses may declare a proc status.");
                if (value.modifiers == null)
                {
                    result.Add("collection.required", "statuses", value.id, "modifiers",
                        "Status modifier collection cannot be null.");
                    continue;
                }
                foreach (var modifier in value.modifiers)
                {
                    if (modifier == null)
                    {
                        result.Add("definition.null", "statuses", value.id, "modifiers",
                            "Status modifier entry is null.");
                        continue;
                    }
                    if (!CombatFrameworkCompiler.SupportsAttribute(modifier.attributeId))
                        result.Add("mechanism.unknown", "statuses", value.id,
                            "modifiers.attributeId", "Unsupported status modifier attribute '"
                            + modifier.attributeId + "'.");
                    if (!CombatFrameworkCompiler.SupportsOperation(modifier.operationId))
                        result.Add("mechanism.unknown", "statuses", value.id,
                            "modifiers.operationId", "Unsupported status modifier operation '"
                            + modifier.operationId + "'.");
                    RequireFinite(modifier.value, "statuses", value.id, "modifiers.value", result);
                }
            }
        }

        private static void ValidateStatusProcCycles(StatusDefinitionDto[] values,
            ContentValidationResult result)
        {
            if (values == null) return;
            var statuses = values.Where(value => value != null && !string.IsNullOrEmpty(value.id))
                .GroupBy(value => value.id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var states = new Dictionary<string, int>(StringComparer.Ordinal);
            var reported = new HashSet<string>(StringComparer.Ordinal);
            foreach (var statusId in statuses.Keys.OrderBy(value => value, StringComparer.Ordinal))
                VisitStatusProc(statusId, statuses, states, reported, result);
        }

        private static void VisitStatusProc(string statusId,
            IReadOnlyDictionary<string, StatusDefinitionDto> statuses,
            IDictionary<string, int> states, ISet<string> reported,
            ContentValidationResult result)
        {
            int state;
            if (states.TryGetValue(statusId, out state) && state == 2) return;
            if (state == 1) return;
            states[statusId] = 1;
            var status = statuses[statusId];
            if (status.stackingMode == "stacking.proc-after-hits"
                && !string.IsNullOrEmpty(status.procStatusId)
                && statuses.ContainsKey(status.procStatusId))
            {
                int targetState;
                states.TryGetValue(status.procStatusId, out targetState);
                if (targetState == 1)
                {
                    if (reported.Add(statusId))
                        result.Add("status.proc.cycle", "statuses", status.id, "procStatusId",
                            "Status proc graph must be acyclic; cycle reaches '"
                            + status.procStatusId + "'.");
                }
                else VisitStatusProc(status.procStatusId, statuses, states, reported, result);
            }
            states[statusId] = 2;
        }

        private static void ValidateAbilityExecutionSupport(BattleContentCatalogDto catalog,
            ContentValidationResult result)
        {
            if (catalog.abilities == null) return;
            var abilities = catalog.abilities.Where(value => value != null
                    && !string.IsNullOrEmpty(value.id))
                .GroupBy(value => value.id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (var ability in abilities.Values)
            {
                if (ability.activation == null || ability.deliveries == null) continue;
                var eventActivated = ability.activation.kindId == "activation.combat-event";
                if (!eventActivated && ability.activation.ownerRoleId != "owner.any")
                    result.Add("ability.owner.unsupported", "abilities", ability.id,
                        "activation.ownerRoleId",
                        "Cooldown and periodic Abilities have no event owner context and require owner.any.");
                if (eventActivated) continue;
                for (var index = 0; index < ability.deliveries.Length; index++)
                {
                    var delivery = ability.deliveries[index];
                    if (delivery == null || (delivery.targetId != "target.event-source"
                            && delivery.targetId != "target.event-target")) continue;
                    result.Add("ability.target.unsupported", "abilities", ability.id,
                        "deliveries[" + index + "].targetId",
                        "Event-context targets require combat-event activation.");
                }
            }

            if (catalog.enemies == null) return;
            foreach (var enemy in catalog.enemies.Where(value => value != null))
            foreach (var abilityId in enemy.abilityIds ?? Array.Empty<string>())
            {
                AbilityDefinitionDto ability;
                if (!abilities.TryGetValue(abilityId, out ability) || ability.activation == null) continue;
                if (ability.activation.kindId != "activation.combat-event")
                {
                    result.Add("ability.owner.unsupported", "enemies", enemy.id, "abilityIds",
                        "Enemy Abilities currently require combat-event activation.");
                    continue;
                }
                if (ability.activation.ownerRoleId != "owner.event-target")
                    result.Add("ability.owner.unsupported", "enemies", enemy.id, "abilityIds",
                        "Enemy combat-event Abilities require owner.event-target.");
                if (ability.activation.eventId != "event.after-damage-taken")
                    result.Add("ability.event.unsupported", "enemies", enemy.id, "abilityIds",
                        "Enemy combat-event Abilities currently support only event.after-damage-taken.");
                foreach (var delivery in ability.deliveries ?? Array.Empty<AbilityDeliveryDefinitionDto>())
                {
                    if (delivery == null) continue;
                    if (delivery.modeId != "delivery.instant")
                        result.Add("ability.delivery.unsupported", "enemies", enemy.id, "abilityIds",
                            "Enemy combat-event Abilities support only instant delivery.");
                    if (delivery.targetId != "target.self"
                        && delivery.targetId != "target.event-target")
                        result.Add("ability.target.unsupported", "enemies", enemy.id, "abilityIds",
                            "Enemy combat-event Abilities support only self or event-target selection.");
                    if ((delivery.payload ?? Array.Empty<AbilityPayloadEffectDefinitionDto>())
                        .Any(effect => effect != null && effect.kindId != "effect.apply-status"))
                        result.Add("ability.effect.unsupported", "enemies", enemy.id, "abilityIds",
                            "Enemy combat-event Abilities support only apply-status payloads.");
                }
            }
        }

        private static void ValidateEquipmentAbilityBindings(BattleContentCatalogDto catalog,
            ContentValidationResult result)
        {
            if (catalog.plants == null || catalog.abilities == null || catalog.equipment == null) return;
            var plants = catalog.plants.Where(value => value != null)
                .GroupBy(value => value.id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var abilities = catalog.abilities.Where(value => value != null)
                .GroupBy(value => value.id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (var equipment in catalog.equipment.Where(value => value != null))
            {
                var grants = equipment.grants ?? Array.Empty<AbilityGrantDefinitionDto>();
                var modifiers = (equipment.modifiers ?? Array.Empty<AbilityModifierDefinitionDto>())
                    .OrderBy(value => value == null ? string.Empty : value.id,
                        StringComparer.Ordinal).ToArray();
                foreach (var plantId in equipment.compatiblePlantIds ?? Array.Empty<string>())
                {
                    PlantDefinitionDto plant;
                    if (!plants.TryGetValue(plantId, out plant)) continue;
                    var plantTags = new HashSet<string>(plant.tags ?? Array.Empty<string>(),
                        StringComparer.Ordinal);
                    var resolvedIds = new List<string>(plant.abilityIds ?? Array.Empty<string>());
                    foreach (var grant in grants)
                    {
                        if (grant == null || !abilities.ContainsKey(grant.abilityId)) continue;
                        if (!string.IsNullOrEmpty(grant.requiredPlantTag)
                            && !plantTags.Contains(grant.requiredPlantTag)) continue;
                        if (!resolvedIds.Contains(grant.abilityId)) resolvedIds.Add(grant.abilityId);
                    }
                    foreach (var modifier in modifiers)
                    {
                        if (modifier == null) continue;
                        if (!string.IsNullOrEmpty(modifier.requiredPlantTag)
                            && !plantTags.Contains(modifier.requiredPlantTag)) continue;
                        var matches = resolvedIds.Where(id => abilities.ContainsKey(id)
                            && (string.IsNullOrEmpty(modifier.targetAbilityId)
                                || string.Equals(id, modifier.targetAbilityId, StringComparison.Ordinal))
                            && (string.IsNullOrEmpty(modifier.targetAbilityTag)
                                || (abilities[id].tags ?? Array.Empty<string>())
                                    .Contains(modifier.targetAbilityTag))).ToArray();
                        if (matches.Length == 0)
                            result.Add("modifier.match.zero", "equipment", equipment.id, modifier.id,
                                "Modifier matches no Ability on compatible plant '" + plant.id + "'.");
                        else if (matches.Length > 1 && !modifier.allowMultipleMatches)
                            result.Add("modifier.match.ambiguous", "equipment", equipment.id, modifier.id,
                                "Modifier matches multiple Abilities on compatible plant '" + plant.id + "'.");
                        if (modifier.attributeId == "ability-attribute.resource-amount"
                            && matches.Any(id => !AbilityPayloads(abilities[id])
                                .Any(effect => effect.kindId == "effect.grant-resource")))
                            result.Add("modifier.attribute.inapplicable", "equipment", equipment.id,
                                modifier.id, "Resource modifier matched an Ability without a resource payload.");
                        if (modifier.attributeId == "ability-attribute.damage-multiplier"
                            && matches.Any(id => !AbilityPayloads(abilities[id])
                                .Any(effect => effect.kindId == "effect.damage")))
                            result.Add("modifier.attribute.inapplicable", "equipment", equipment.id,
                                modifier.id, "Damage modifier matched an Ability without a damage payload.");
                        if (modifier.attributeId == "ability-attribute.period"
                            && matches.Any(id => abilities[id].activation == null
                                || abilities[id].activation.kindId != "activation.periodic"))
                            result.Add("modifier.attribute.inapplicable", "equipment", equipment.id,
                                modifier.id, "Period modifier matched a non-periodic Ability.");
                        if (modifier.attributeId == "ability-attribute.cooldown"
                            && matches.Any(id => abilities[id].activation == null
                                || (abilities[id].activation.kindId != "activation.cooldown"
                                    && abilities[id].activation.kindId != "activation.combat-event")))
                            result.Add("modifier.attribute.inapplicable", "equipment", equipment.id,
                                modifier.id,
                                "Cooldown modifier matched an Ability whose activation does not consume cooldown.");
                    }
                    ValidateResolvedModifierResults(equipment, plant, resolvedIds,
                        abilities, modifiers, result);
                }
            }
        }

        private static IEnumerable<AbilityPayloadEffectDefinitionDto> AbilityPayloads(
            AbilityDefinitionDto ability)
        {
            if (ability == null || ability.deliveries == null)
                return Array.Empty<AbilityPayloadEffectDefinitionDto>();
            return ability.deliveries.Where(value => value != null && value.payload != null)
                .SelectMany(value => value.payload).Where(value => value != null);
        }

        private static void ValidateResolvedModifierResults(EquipmentDefinitionDto equipment,
            PlantDefinitionDto plant, IEnumerable<string> resolvedIds,
            IReadOnlyDictionary<string, AbilityDefinitionDto> abilities,
            IEnumerable<AbilityModifierDefinitionDto> modifiers, ContentValidationResult result)
        {
            var resolved = new Dictionary<string, CompiledAbilityDefinition>(StringComparer.Ordinal);
            foreach (var abilityId in resolvedIds.Distinct(StringComparer.Ordinal))
            {
                AbilityDefinitionDto ability;
                if (!abilities.TryGetValue(abilityId, out ability)
                    || !CanCompileForModifierValidation(ability)) continue;
                resolved.Add(abilityId, BattleAbilityCompiler.Compile(ability));
            }

            var plantTags = new HashSet<string>(plant.tags ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            foreach (var modifier in modifiers)
            {
                if (modifier == null || !IsFinite(modifier.value)
                    || !BattleAbilityCompiler.SupportsModifierAttribute(modifier.attributeId)
                    || !BattleAbilityCompiler.SupportsModifierOperation(modifier.operationId)
                    || (!string.IsNullOrEmpty(modifier.requiredPlantTag)
                        && !plantTags.Contains(modifier.requiredPlantTag))) continue;
                foreach (var pair in resolved)
                {
                    var ability = pair.Value;
                    if (!string.IsNullOrEmpty(modifier.targetAbilityId)
                        && modifier.targetAbilityId != pair.Key) continue;
                    if (!string.IsNullOrEmpty(modifier.targetAbilityTag)
                        && !ability.Tags.Contains(modifier.targetAbilityTag)) continue;
                    CompiledAbilityModifierApplicator.Apply(ability,
                        BattleAbilityCompiler.Compile(modifier));
                }
            }

            foreach (var pair in resolved)
            {
                var ability = pair.Value;
                var payload = ability.Deliveries.SelectMany(value => value.Payload).ToArray();
                var hasDamage = payload.Any(value => value.Kind == AbilityPayloadEffectKind.Damage);
                var invalid = !IsFinite(ability.DamageMultiplier)
                    || (hasDamage && ability.DamageMultiplier <= 0f)
                    || (ability.Activation.Kind == AbilityActivationKind.Cooldown
                        && ability.Activation.CooldownTicks <= 0)
                    || (ability.Activation.Kind == AbilityActivationKind.Periodic
                        && ability.Activation.PeriodTicks <= 0)
                    || ability.BurstCount < 1
                    || (ability.BurstCount > 1 && ability.BurstIntervalTicks <= 0)
                    || payload.Any(value => value.Kind == AbilityPayloadEffectKind.GrantResource
                        && value.ResourceAmount <= 0);
                if (!invalid) continue;
                result.Add("modifier.result.invalid", "equipment", equipment.id,
                    plant.id + "." + pair.Key,
                    "Resolved Ability values must retain positive timing, damage, burst, and resource invariants.");
            }
        }

        private static bool CanCompileForModifierValidation(AbilityDefinitionDto ability)
        {
            if (ability == null || ability.activation == null || ability.timeline == null
                || ability.tags == null || ability.deliveries == null
                || !IsFinite(ability.damageMultiplier)
                || !IsFinite(ability.activation.cooldownSeconds)
                || !IsFinite(ability.activation.periodSeconds)
                || !IsFinite(ability.burstIntervalSeconds)
                || !BattleAbilityCompiler.SupportsActivation(ability.activation.kindId)
                || !BattleAbilityCompiler.SupportsOwnerRole(ability.activation.ownerRoleId)
                || (!string.IsNullOrEmpty(ability.activation.eventId)
                    && !BattleAbilityCompiler.SupportsEvent(ability.activation.eventId))) return false;
            foreach (var delivery in ability.deliveries)
            {
                if (delivery == null || delivery.payload == null
                    || !BattleAbilityCompiler.SupportsTarget(delivery.targetId)
                    || !BattleAbilityCompiler.SupportsDelivery(delivery.modeId)) return false;
                foreach (var effect in delivery.payload)
                    if (effect == null || !BattleAbilityCompiler.SupportsEffect(effect.kindId)) return false;
            }
            return true;
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
                    result.Add("wave.order.invalid", "waves", value.id, "index",
                        "Wave indexes must be unique positive values.");
                RequireFiniteGreater(value.healthMultiplier, 0f, "waves", value.id,
                    "healthMultiplier", result);
                RequireFiniteGreater(value.speedMultiplier, 0f, "waves", value.id,
                    "speedMultiplier", result);
                RequireFiniteGreater(value.spawnIntervalSeconds, 0f, "waves", value.id,
                    "spawnIntervalSeconds", result);
                RequireIntAtLeast(value.completionReward, 0, "waves", value.id,
                    "completionReward", result);
                RequireSequenceReferences(value.enemyIds, enemyIds, "waves", value.id,
                    "enemyIds", result);
            }
            var expected = rules == null ? values.Length : rules.maxWaves;
            for (var index = 1; index <= expected; index++)
                if (!indexes.Contains(index)) result.Add("wave.order.invalid", "waves",
                    "wave." + index, "index", "Missing ordered wave index " + index + ".");
        }

        private static void ValidateStarTiers(StarTierDefinitionDto[] values,
            ContentValidationResult result)
        {
            if (values == null) return;
            var stars = new HashSet<int>();
            foreach (var value in values)
            {
                if (value == null) continue;
                if (value.star <= 0 || !stars.Add(value.star))
                    result.Add("star.duplicate", "starTiers", value.id, "star",
                        "Star levels must be unique positive values.");
                RequireFiniteGreater(value.damageMultiplier, 0f, "starTiers", value.id,
                    "damageMultiplier", result);
                RequireFiniteGreater(value.attackSpeedMultiplier, 0f, "starTiers", value.id,
                    "attackSpeedMultiplier", result);
                RequireFiniteGreater(value.rangeMultiplier, 0f, "starTiers", value.id,
                    "rangeMultiplier", result);
            }
        }

        private static void ValidateBattleRules(BattleRulesDto rules, HashSet<string> equipmentIds,
            ContentValidationResult result)
        {
            if (rules == null)
            {
                result.Add("collection.required", "battleRules", string.Empty, string.Empty,
                    "Battle rules are required.");
                return;
            }
            ValidateStableId(rules.id, "battleRules", rules.id, 0, result, null);
            RequireIntAtLeast(rules.initialSun, 0, "battleRules", rules.id, "initialSun", result);
            RequireIntAtLeast(rules.initialLives, 1, "battleRules", rules.id, "initialLives", result);
            RequireIntAtLeast(rules.maxWaves, 1, "battleRules", rules.id, "maxWaves", result);
            RequireIntAtLeast(rules.initialPotCount, 1, "battleRules", rules.id,
                "initialPotCount", result);
            RequireFiniteGreater(rules.betweenWaveSeconds, 0f, "battleRules", rules.id,
                "betweenWaveSeconds", result);
            RequireIntAtLeast(rules.nurserySlotCount, 1, "battleRules", rules.id,
                "nurserySlotCount", result);
            RequireFiniteRange(rules.nurseryPotChance, 0f, 1f, "battleRules", rules.id,
                "nurseryPotChance", result);
            RequireIntAtLeast(rules.refreshBaseCost, 0, "battleRules", rules.id,
                "refreshBaseCost", result);
            RequireIntAtLeast(rules.refreshCostStep, 0, "battleRules", rules.id,
                "refreshCostStep", result);
            if (rules.milestoneRewards == null) return;
            var waves = new HashSet<int>();
            foreach (var reward in rules.milestoneRewards)
            {
                if (reward == null)
                {
                    result.Add("definition.null", "milestoneRewards", string.Empty, string.Empty,
                        "Milestone reward entry is null.");
                    continue;
                }
                if (reward.wave <= 0 || reward.wave > rules.maxWaves || !waves.Add(reward.wave))
                    result.Add("reward.wave.invalid", "milestoneRewards", "wave." + reward.wave,
                        "wave", "Milestone wave must be unique and inside battle bounds.");
                RequireIntAtLeast(reward.potCount, 0, "milestoneRewards", "wave." + reward.wave,
                    "potCount", result);
                RequireReferences(reward.equipmentIds, equipmentIds, "milestoneRewards",
                    "wave." + reward.wave, "equipmentIds", true, result);
            }
        }

        private static HashSet<string> ValidateIds<T>(T[] values, string category,
            Func<T, string> getId, ContentValidationResult result) where T : class
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (values == null) return ids;
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index];
                if (value == null)
                {
                    result.Add("definition.null", category, "#" + index, string.Empty,
                        "Definition entry is null.");
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
                result.Add("definition.id.invalid", category,
                    string.IsNullOrEmpty(itemId) ? "#" + index : itemId, "id",
                    "ID must be lowercase ASCII segments separated by '.', '-' or '_'.");
                return;
            }
            if (ids != null && !ids.Add(id))
                result.Add("definition.id.duplicate", category, id, "id",
                    "ID is duplicated in this category.");
        }

        private static void RequireStableReferenceName(string value, string category, string id,
            string field, ContentValidationResult result)
        {
            if (string.IsNullOrEmpty(value) || !StableIdPattern.IsMatch(value))
                result.Add("reference.id.invalid", category, id, field,
                    "Reference name must use the stable ID format.");
        }

        private static void RequireOptionalStableName(string value, string category, string id,
            string field, ContentValidationResult result)
        {
            if (!string.IsNullOrEmpty(value))
                RequireStableReferenceName(value, category, id, field, result);
        }

        private static void RequireStableNames(string[] values, string category, string id,
            string field, bool requireAtLeastOne, ContentValidationResult result)
        {
            if (values == null || values.Length == 0)
            {
                if (requireAtLeastOne) result.Add("reference.required", category, id, field,
                    "At least one stable tag is required.");
                return;
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                RequireStableReferenceName(value, category, id, field, result);
                if (!seen.Add(value)) result.Add("reference.duplicate", category, id, field,
                    "Tag '" + value + "' is duplicated.");
            }
        }

        private static void RequireReferences(string[] values, HashSet<string> targets,
            string category, string id, string field, bool requireAtLeastOne,
            ContentValidationResult result)
        {
            if (values == null || values.Length == 0)
            {
                if (requireAtLeastOne) result.Add("reference.required", category, id, field,
                    "At least one reference is required.");
                return;
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value) || !targets.Contains(value))
                    result.Add("reference.missing", category, id, field,
                        "Referenced ID '" + value + "' does not exist.");
                else if (!seen.Add(value))
                    result.Add("reference.duplicate", category, id, field,
                        "Referenced ID '" + value + "' is duplicated.");
            }
        }

        private static void RequireOptionalReference(string value, HashSet<string> targets,
            string category, string id, string field, ContentValidationResult result)
        {
            if (!string.IsNullOrEmpty(value) && !targets.Contains(value))
                result.Add("reference.missing", category, id, field,
                    "Referenced ID '" + value + "' does not exist.");
        }

        private static void RequireSequenceReferences(string[] values, HashSet<string> targets,
            string category, string id, string field, ContentValidationResult result)
        {
            if (values == null || values.Length == 0)
            {
                result.Add("reference.required", category, id, field,
                    "At least one sequence entry is required.");
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

        private static void RequireCollection<T>(T[] values, string category,
            ContentValidationResult result)
        {
            if (values == null || values.Length == 0)
                result.Add("collection.required", category, string.Empty, string.Empty,
                    "At least one definition is required.");
        }

        private static void ExpectCount<T>(T[] values, int count, string category,
            ContentValidationResult result)
        {
            var actual = values == null ? 0 : values.Length;
            if (actual != count) result.Add("bundled.count.mismatch", category, string.Empty,
                string.Empty, "Expected " + count + " definitions but found " + actual + ".");
        }

        private static void RequireIds<T>(T[] values, Func<T, string> getId, string category,
            string[] requiredIds, ContentValidationResult result) where T : class
        {
            var actual = new HashSet<string>(StringComparer.Ordinal);
            if (values != null)
                foreach (var value in values)
                    if (value != null) actual.Add(getId(value));
            foreach (var id in requiredIds)
                if (!actual.Contains(id)) result.Add("bundled.id.missing", category, id, "id",
                    "Required bundled definition is missing.");
        }

        private static void RequireText(string value, string category, string id, string field,
            ContentValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.Add("definition.text.required", category, id, field, "Value is required.");
        }

        private static void RequireFinite(float value, string category, string id, string field,
            ContentValidationResult result)
        {
            if (!IsFinite(value)) result.Add("definition.numeric.invalid", category, id, field,
                "Value must be finite.");
        }

        private static void RequireFiniteGreater(float value, float minimum, string category,
            string id, string field, ContentValidationResult result)
        {
            if (!IsFinite(value) || value <= minimum)
                result.Add("definition.numeric.invalid", category, id, field,
                    "Value must be finite and greater than " + minimum + ".");
        }

        private static void RequireFiniteAtLeast(float value, float minimum, string category,
            string id, string field, ContentValidationResult result)
        {
            if (!IsFinite(value) || value < minimum)
                result.Add("definition.numeric.invalid", category, id, field,
                    "Value must be finite and at least " + minimum + ".");
        }

        private static void RequireFiniteRange(float value, float minimum, float maximum,
            string category, string id, string field, ContentValidationResult result)
        {
            if (!IsFinite(value) || value < minimum || value > maximum)
                result.Add("definition.numeric.invalid", category, id, field,
                    "Value must be between " + minimum + " and " + maximum + ".");
        }

        private static void RequireIntAtLeast(int value, int minimum, string category, string id,
            string field, ContentValidationResult result)
        {
            if (value < minimum) result.Add("definition.numeric.invalid", category, id, field,
                "Value must be at least " + minimum + ".");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
