using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        public string MapId { get; private set; } = BattleSnapshotSchema.DefaultMapId;

        public BattleSnapshotV1 ExportSnapshot()
        {
            var livePlantIds = new HashSet<int>(State.Plants.Select(value => value.Id));
            var liveEnemyIds = new HashSet<int>(State.Zombies.Select(value => value.Id));
            return new BattleSnapshotV1
            {
                catalogId = _content.Header.catalogId,
                contentVersion = _content.Header.contentVersion,
                mapId = MapId,
                logicStep = State.LogicTick,
                randomState = _random.State,
                randomSeed = State.RandomSeed,
                phase = (int)State.Phase,
                paused = State.Paused,
                speed = State.Speed,
                elapsed = State.Elapsed,
                sun = State.Sun,
                lives = State.Lives,
                refreshCount = State.RefreshCount,
                waveIndex = State.WaveIndex,
                waveSpawned = State.WaveSpawned,
                waveTotal = State.WaveTotal,
                spawnCooldown = State.SpawnCooldown,
                betweenTimer = State.BetweenTimer,
                nextEntityId = State.NextId,
                nextStatusSequence = State.NextStatusSequence,
                availablePots = State.Inventory.Pots,
                equipment = ExportEquipment(),
                pots = State.Pots.OrderBy(value => value.Id).Select(ExportPot).ToArray(),
                plants = State.Plants.OrderBy(value => value.Id).Select(ExportPlant).ToArray(),
                enemies = State.Zombies.OrderBy(value => value.Id)
                    .Select(value => ExportEnemy(value, livePlantIds)).ToArray(),
                projectiles = State.Projectiles.OrderBy(value => value.Id)
                    .Select(value => ExportProjectile(value, livePlantIds, liveEnemyIds)).ToArray(),
            };
        }

        public string ExportSnapshotJson(bool prettyPrint = false)
        {
            return JsonUtility.ToJson(ExportSnapshot(), prettyPrint);
        }

        public string OutcomeStateChecksum()
        {
            var canonicalJson = JsonUtility.ToJson(ExportSnapshot(), false);
            using (var hash = SHA256.Create())
            {
                var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(canonicalJson));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        public BattleSnapshotRestoreResult RestoreSnapshotJson(string json)
        {
            return RestoreSnapshotJson(json, _content);
        }

        public BattleSnapshotRestoreResult RestoreSnapshotJson(string json, CompiledBattleContentCatalog availableContent)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Failure(BattleSnapshotRestoreCode.InvalidPayload, "$", "Snapshot JSON is empty.");
            BattleSnapshotV1 snapshot;
            try
            {
                snapshot = JsonUtility.FromJson<BattleSnapshotV1>(json);
            }
            catch (Exception exception)
            {
                return Failure(BattleSnapshotRestoreCode.InvalidPayload, "$", exception.Message);
            }
            if (snapshot == null)
                return Failure(BattleSnapshotRestoreCode.InvalidPayload, "$", "Snapshot JSON produced no envelope.");
            return RestoreSnapshot(snapshot, availableContent);
        }

        public BattleSnapshotRestoreResult RestoreSnapshot(BattleSnapshotV1 snapshot)
        {
            return RestoreSnapshot(snapshot, _content);
        }

        public BattleSnapshotRestoreResult RestoreSnapshot(BattleSnapshotV1 snapshot,
            CompiledBattleContentCatalog availableContent)
        {
            GameState candidate;
            var result = TryBuildCandidate(snapshot, availableContent, out candidate);
            if (!result.Succeeded) return result;

            _random.RestoreState(snapshot.randomState);
            State = candidate;
            _lastNurseryPotSlots.Clear();
            ResetFrameAccumulator();
            _presentationEvents.Reset();
            return BattleSnapshotRestoreResult.Ok();
        }

        private BattleSnapshotEquipmentV1[] ExportEquipment()
        {
            return new[]
            {
                new BattleSnapshotEquipmentV1
                {
                    definitionId = BattleContentIds.Equipment.Chili,
                    count = State.Inventory.Chili,
                },
                new BattleSnapshotEquipmentV1
                {
                    definitionId = BattleContentIds.Equipment.Gatling,
                    count = State.Inventory.Gatling,
                },
                new BattleSnapshotEquipmentV1
                {
                    definitionId = BattleContentIds.Equipment.Ice,
                    count = State.Inventory.Ice,
                },
            }.OrderBy(value => value.definitionId, StringComparer.Ordinal).ToArray();
        }

        private static BattleSnapshotPotV1 ExportPot(Pot value)
        {
            return new BattleSnapshotPotV1
            {
                entityId = value.Id,
                cellX = value.Cell.x,
                cellY = value.Cell.y,
                active = value.Active,
            };
        }

        private static BattleSnapshotPlantV1 ExportPlant(Plant value)
        {
            var plantDefinitionId = string.IsNullOrEmpty(value.ContentId)
                ? LegacyBattleContentIds.Plant(value.Kind)
                : value.ContentId;
            var equipmentDefinitionId = value.EquipmentId ?? string.Empty;
            if (string.IsNullOrEmpty(equipmentDefinitionId))
            {
                string legacyEquipmentId;
                if (LegacyBattleContentIds.TryEquipment(value.Weapon, out legacyEquipmentId))
                    equipmentDefinitionId = legacyEquipmentId;
            }
            return new BattleSnapshotPlantV1
            {
                entityId = value.Id,
                definitionId = plantDefinitionId,
                legacyKind = (int)value.Kind,
                star = value.Star,
                potEntityId = value.PotId,
                nurseryIndex = value.NurseryIndex,
                equipmentDefinitionId = equipmentDefinitionId,
                legacyWeapon = (int)value.Weapon,
                attackCooldown = value.AttackCooldown,
                productionProgress = value.ProductionProgress,
                moveCooldown = value.MoveCooldown,
                burstShotsRemaining = value.BurstShotsRemaining,
                burstShotCooldown = value.BurstShotCooldown,
                skills = value.SkillRuntimes.OrderBy(runtime => runtime.SkillId, StringComparer.Ordinal)
                    .Select(runtime => new BattleSnapshotSkillRuntimeV1
                    {
                        definitionId = runtime.SkillId,
                        cooldownTicks = runtime.CooldownTicks,
                        periodicProgressTicks = runtime.PeriodicProgressTicks,
                        burstShotsRemaining = runtime.BurstShotsRemaining,
                        burstIntervalTicks = runtime.BurstIntervalTicks,
                    }).ToArray(),
            };
        }

        private static BattleSnapshotEnemyV1 ExportEnemy(Zombie value, HashSet<int> livePlantIds)
        {
            return new BattleSnapshotEnemyV1
            {
                entityId = value.Id,
                definitionId = value.ContentId,
                hp = value.Hp,
                maxHp = value.MaxHp,
                speed = value.Speed,
                pathProgress = value.PathProgress,
                reward = value.Reward,
                threat = value.Threat,
                statuses = value.Statuses.OrderBy(status => status.Sequence)
                    .Select(status => new BattleSnapshotStatusV1
                    {
                        definitionId = status.DefinitionId,
                        sourceEntityId = status.SourceEntityId > 0 && livePlantIds.Contains(status.SourceEntityId)
                            ? status.SourceEntityId : 0,
                        remainingTicks = status.RemainingTicks,
                        stackCount = status.StackCount,
                        magnitude = status.Magnitude,
                        sequence = status.Sequence,
                        tickProgress = status.TickProgress,
                    }).ToArray(),
            };
        }

        private static BattleSnapshotProjectileV1 ExportProjectile(ProjectileFlash value,
            HashSet<int> livePlantIds, HashSet<int> liveEnemyIds)
        {
            return new BattleSnapshotProjectileV1
            {
                entityId = value.Id,
                plantEntityId = value.PlantId > 0 && livePlantIds.Contains(value.PlantId) ? value.PlantId : 0,
                targetEntityId = value.TargetId > 0 && liveEnemyIds.Contains(value.TargetId) ? value.TargetId : -1,
                definitionId = value.ProjectileId,
                originX = value.Origin.x,
                originY = value.Origin.y,
                positionX = value.Position.x,
                positionY = value.Position.y,
                targetX = value.TargetPoint.x,
                targetY = value.TargetPoint.y,
                directionX = value.Direction.x,
                directionY = value.Direction.y,
                maxDistance = value.MaxDistance,
                progress = value.Progress,
                returning = value.Returning,
                damage = value.Damage,
                ticksRemaining = value.TicksRemaining,
                flightTicks = value.FlightTicks,
                hitEntityIds = value.HitIds.Where(liveEnemyIds.Contains).ToArray(),
            };
        }

        private BattleSnapshotRestoreResult TryBuildCandidate(BattleSnapshotV1 snapshot,
            CompiledBattleContentCatalog availableContent, out GameState candidate)
        {
            candidate = null;
            if (snapshot == null)
                return Failure(BattleSnapshotRestoreCode.InvalidPayload, "$", "Snapshot is null.");
            if (snapshot.schemaVersion != BattleSnapshotSchema.Version)
                return Failure(BattleSnapshotRestoreCode.UnsupportedSchema, "schemaVersion",
                    "Only battle snapshot schema V1 is supported.");
            if (availableContent == null)
                return Failure(BattleSnapshotRestoreCode.ContentUnavailable, "contentVersion",
                    "The pinned compiled catalog is unavailable.");
            if (availableContent.Header == null
                || !StringEquals(snapshot.catalogId, availableContent.Header.catalogId)
                || !StringEquals(snapshot.contentVersion, availableContent.Header.contentVersion))
                return Failure(BattleSnapshotRestoreCode.IncompatibleContent, "contentVersion",
                    "Snapshot catalog/content identity does not match the supplied catalog.");
            if (!StringEquals(availableContent.Header.catalogId, _content.Header.catalogId)
                || !StringEquals(availableContent.Header.contentVersion, _content.Header.contentVersion))
                return Failure(BattleSnapshotRestoreCode.IncompatibleContent, "contentVersion",
                    "The active simulation uses a different catalog/content identity.");
            if (string.IsNullOrEmpty(snapshot.mapId) || !StringEquals(snapshot.mapId, MapId))
                return Failure(BattleSnapshotRestoreCode.IncompatibleMap, "mapId",
                    "Snapshot map identity does not match the active simulation map.");
            if (snapshot.equipment == null || snapshot.pots == null || snapshot.plants == null
                || snapshot.enemies == null || snapshot.projectiles == null)
                return Failure(BattleSnapshotRestoreCode.InvalidPayload, "$",
                    "Snapshot arrays must not be null.");
            if (!Enum.IsDefined(typeof(GamePhase), snapshot.phase))
                return Failure(BattleSnapshotRestoreCode.InvalidNumericValue, "phase", "Unknown battle phase.");
            if (snapshot.logicStep < 0 || snapshot.randomState == 0u || snapshot.speed < 1 || snapshot.speed > 2
                || !FiniteNonNegative(snapshot.elapsed) || snapshot.sun < 0 || snapshot.lives < 0
                || snapshot.refreshCount < 0 || snapshot.waveIndex < 0
                || snapshot.waveIndex > availableContent.BattleRules.maxWaves
                || snapshot.waveSpawned < 0 || snapshot.waveTotal < 0
                || snapshot.waveSpawned > snapshot.waveTotal || !Finite(snapshot.spawnCooldown)
                || !FiniteNonNegative(snapshot.betweenTimer) || snapshot.availablePots < 0)
                return Failure(BattleSnapshotRestoreCode.InvalidNumericValue, "battleState",
                    "Battle counters, timers, random state, speed, or resources are outside legal ranges.");

            var allEntityIds = new HashSet<int>();
            var maxEntityId = 0;
            var identity = ValidateEntityIdentities(snapshot, allEntityIds, ref maxEntityId);
            if (!identity.Succeeded) return identity;
            if (snapshot.nextEntityId <= maxEntityId || snapshot.nextEntityId <= 0)
                return Failure(BattleSnapshotRestoreCode.InvalidIdentity, "nextEntityId",
                    "Next entity ID must be positive and greater than every live entity ID.");

            var potDtos = snapshot.pots.OrderBy(value => value.entityId).ToArray();
            var plantDtos = snapshot.plants.OrderBy(value => value.entityId).ToArray();
            var enemyDtos = snapshot.enemies.OrderBy(value => value.entityId).ToArray();
            var projectileDtos = snapshot.projectiles.OrderBy(value => value.entityId).ToArray();
            var potIds = new HashSet<int>(potDtos.Select(value => value.entityId));
            var plantIds = new HashSet<int>(plantDtos.Select(value => value.entityId));
            var enemyIds = new HashSet<int>(enemyDtos.Select(value => value.entityId));

            var newState = new GameState
            {
                Phase = (GamePhase)snapshot.phase,
                Paused = snapshot.paused,
                Speed = snapshot.speed,
                Elapsed = snapshot.elapsed,
                Sun = snapshot.sun,
                Lives = snapshot.lives,
                RefreshCount = snapshot.refreshCount,
                WaveIndex = snapshot.waveIndex,
                WaveSpawned = snapshot.waveSpawned,
                WaveTotal = snapshot.waveTotal,
                SpawnCooldown = snapshot.spawnCooldown,
                BetweenTimer = snapshot.betweenTimer,
                NextId = snapshot.nextEntityId,
                RandomSeed = snapshot.randomSeed,
                LogicTick = snapshot.logicStep,
                NextStatusSequence = snapshot.nextStatusSequence,
            };

            var inventoryResult = RestoreInventory(snapshot, availableContent, newState.Inventory);
            if (!inventoryResult.Succeeded) return inventoryResult;

            var occupiedCells = new HashSet<Vector2Int>();
            foreach (var value in potDtos)
            {
                var cell = new Vector2Int(value.cellX, value.cellY);
                if (!Map.IsPlantable(cell))
                    return Failure(BattleSnapshotRestoreCode.InvalidReference,
                        "pots[" + value.entityId + "].cell", "Pot cell is not present in the pinned map.");
                if (!occupiedCells.Add(cell))
                    return Failure(BattleSnapshotRestoreCode.InvalidIdentity,
                        "pots[" + value.entityId + "].cell", "Pot cells must be unique.");
                newState.Pots.Add(new Pot { Id = value.entityId, Cell = cell, Active = value.active });
            }

            var occupiedPots = new HashSet<int>();
            var occupiedNurserySlots = new HashSet<int>();
            foreach (var value in plantDtos)
            {
                if (string.IsNullOrEmpty(value.definitionId) || !availableContent.Plants.ContainsKey(value.definitionId))
                    return Failure(BattleSnapshotRestoreCode.UnknownDefinition,
                        "plants[" + value.entityId + "].definitionId", "Plant definition is unavailable.");
                if (!Enum.IsDefined(typeof(PlantKind), value.legacyKind)
                    || !Enum.IsDefined(typeof(WeaponKind), value.legacyWeapon) || value.star < 1 || value.star > 4
                    || !FiniteNonNegative(value.attackCooldown) || !FiniteNonNegative(value.productionProgress)
                    || !FiniteNonNegative(value.moveCooldown) || value.burstShotsRemaining < 0
                    || !FiniteNonNegative(value.burstShotCooldown))
                    return Failure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "plants[" + value.entityId + "]", "Plant kind, star, cooldown, or burst state is invalid.");
                if (value.potEntityId >= 0 == (value.nurseryIndex >= 0))
                    return Failure(BattleSnapshotRestoreCode.InvalidReference,
                        "plants[" + value.entityId + "].location", "Plant must occupy exactly one pot or nursery slot.");
                if (value.potEntityId >= 0)
                {
                    if (!potIds.Contains(value.potEntityId)
                        || !newState.Pots.Any(pot => pot.Id == value.potEntityId && pot.Active))
                        return Failure(BattleSnapshotRestoreCode.InvalidReference,
                            "plants[" + value.entityId + "].potEntityId", "Plant references a missing or inactive pot.");
                    if (!occupiedPots.Add(value.potEntityId))
                        return Failure(BattleSnapshotRestoreCode.InvalidIdentity,
                            "plants[" + value.entityId + "].potEntityId", "Only one plant may occupy a pot.");
                }
                else
                {
                    if (value.nurseryIndex < 0 || value.nurseryIndex >= availableContent.BattleRules.nurserySlotCount)
                        return Failure(BattleSnapshotRestoreCode.InvalidReference,
                            "plants[" + value.entityId + "].nurseryIndex", "Nursery index is outside the catalog rules.");
                    if (!occupiedNurserySlots.Add(value.nurseryIndex))
                        return Failure(BattleSnapshotRestoreCode.InvalidIdentity,
                            "plants[" + value.entityId + "].nurseryIndex", "Only one plant may occupy a nursery slot.");
                }

                var equipmentResult = ValidatePlantEquipment(value, availableContent);
                if (!equipmentResult.Succeeded) return equipmentResult;
                if (value.skills == null)
                    return Failure(BattleSnapshotRestoreCode.InvalidPayload,
                        "plants[" + value.entityId + "].skills", "Skill runtime array must not be null.");
                IReadOnlyList<CompiledBattleSkill> resolvedSkills;
                try
                {
                    resolvedSkills = availableContent.ResolvePlantSkills(value.definitionId,
                        value.equipmentDefinitionId ?? string.Empty);
                }
                catch (Exception exception)
                {
                    return Failure(BattleSnapshotRestoreCode.UnknownDefinition,
                        "plants[" + value.entityId + "].skills", exception.Message);
                }
                var allowedSkillIds = new HashSet<string>(resolvedSkills.Select(skill => skill.Id), StringComparer.Ordinal);
                var runtimeIds = new HashSet<string>(StringComparer.Ordinal);
                var plant = new Plant
                {
                    Id = value.entityId,
                    ContentId = value.definitionId,
                    Kind = (PlantKind)value.legacyKind,
                    Star = value.star,
                    PotId = value.potEntityId,
                    NurseryIndex = value.nurseryIndex,
                    EquipmentId = value.equipmentDefinitionId ?? string.Empty,
                    Weapon = (WeaponKind)value.legacyWeapon,
                    AttackCooldown = value.attackCooldown,
                    ProductionProgress = value.productionProgress,
                    MoveCooldown = value.moveCooldown,
                    BurstShotsRemaining = value.burstShotsRemaining,
                    BurstShotCooldown = value.burstShotCooldown,
                };
                foreach (var runtime in value.skills.OrderBy(skill => skill.definitionId, StringComparer.Ordinal))
                {
                    if (runtime == null || string.IsNullOrEmpty(runtime.definitionId)
                        || !availableContent.RuntimeSkills.ContainsKey(runtime.definitionId)
                        || !allowedSkillIds.Contains(runtime.definitionId))
                        return Failure(BattleSnapshotRestoreCode.UnknownDefinition,
                            "plants[" + value.entityId + "].skills", "Skill runtime is not defined for this loadout.");
                    if (!runtimeIds.Add(runtime.definitionId))
                        return Failure(BattleSnapshotRestoreCode.InvalidIdentity,
                            "plants[" + value.entityId + "].skills", "Skill runtime IDs must be unique per plant.");
                    if (runtime.cooldownTicks < 0 || runtime.periodicProgressTicks < 0
                        || runtime.burstShotsRemaining < 0 || runtime.burstIntervalTicks < 0)
                        return Failure(BattleSnapshotRestoreCode.InvalidNumericValue,
                            "plants[" + value.entityId + "].skills", "Skill counters must be nonnegative.");
                    plant.SkillRuntimes.Add(new SkillRuntimeState
                    {
                        SkillId = runtime.definitionId,
                        CooldownTicks = runtime.cooldownTicks,
                        PeriodicProgressTicks = runtime.periodicProgressTicks,
                        BurstShotsRemaining = runtime.burstShotsRemaining,
                        BurstIntervalTicks = runtime.burstIntervalTicks,
                    });
                }
                newState.Plants.Add(plant);
            }

            var statusSequences = new HashSet<int>();
            var maxStatusSequence = 0;
            foreach (var value in enemyDtos)
            {
                if (string.IsNullOrEmpty(value.definitionId) || !availableContent.Enemies.ContainsKey(value.definitionId))
                    return Failure(BattleSnapshotRestoreCode.UnknownDefinition,
                        "enemies[" + value.entityId + "].definitionId", "Enemy definition is unavailable.");
                if (!Finite(value.hp) || !Finite(value.maxHp) || !FiniteNonNegative(value.speed)
                    || !FiniteNonNegative(value.pathProgress) || value.hp <= 0f || value.maxHp <= 0f
                    || value.hp > value.maxHp || value.pathProgress >= Map.Route.TotalLength
                    || value.reward < 0 || value.threat <= 0)
                    return Failure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "enemies[" + value.entityId + "]", "Enemy health, movement, reward, or threat is invalid.");
                if (value.statuses == null)
                    return Failure(BattleSnapshotRestoreCode.InvalidPayload,
                        "enemies[" + value.entityId + "].statuses", "Status array must not be null.");
                var enemy = new Zombie
                {
                    Id = value.entityId,
                    ContentId = value.definitionId,
                    Kind = LegacyEnemyKind(value.definitionId),
                    Hp = value.hp,
                    MaxHp = value.maxHp,
                    Speed = value.speed,
                    PathProgress = value.pathProgress,
                    Reward = value.reward,
                    Threat = value.threat,
                };
                foreach (var status in value.statuses.OrderBy(item => item.sequence))
                {
                    if (status == null || string.IsNullOrEmpty(status.definitionId)
                        || !availableContent.RuntimeStatuses.ContainsKey(status.definitionId))
                        return Failure(BattleSnapshotRestoreCode.UnknownDefinition,
                            "enemies[" + value.entityId + "].statuses", "Status definition is unavailable.");
                    if (status.sourceEntityId != 0 && !plantIds.Contains(status.sourceEntityId))
                        return Failure(BattleSnapshotRestoreCode.InvalidReference,
                            "enemies[" + value.entityId + "].statuses.sourceEntityId",
                            "Status source must reference a live plant or the system source 0.");
                    if (status.remainingTicks <= 0 || status.stackCount <= 0 || !Finite(status.magnitude)
                        || status.magnitude < 0f || status.sequence <= 0 || status.tickProgress < 0)
                        return Failure(BattleSnapshotRestoreCode.InvalidNumericValue,
                            "enemies[" + value.entityId + "].statuses", "Status counters or magnitude are invalid.");
                    if (!statusSequences.Add(status.sequence))
                        return Failure(BattleSnapshotRestoreCode.InvalidIdentity,
                            "enemies[" + value.entityId + "].statuses.sequence", "Status sequences must be globally unique.");
                    maxStatusSequence = Math.Max(maxStatusSequence, status.sequence);
                    var definition = availableContent.RuntimeStatuses[status.definitionId];
                    if (definition.Kind == BattleStatusKind.HitCount && status.stackCount >= definition.HitsToProc)
                        return Failure(BattleSnapshotRestoreCode.InvalidNumericValue,
                            "enemies[" + value.entityId + "].statuses.stackCount",
                            "Hit-count status must proc before reaching its configured threshold.");
                    enemy.Statuses.Add(new StatusInstance
                    {
                        DefinitionId = status.definitionId,
                        SourceEntityId = status.sourceEntityId,
                        RemainingTicks = status.remainingTicks,
                        StackCount = status.stackCount,
                        Magnitude = status.magnitude,
                        Sequence = status.sequence,
                        TickProgress = status.tickProgress,
                    });
                }
                RebuildLegacyStatusViews(newState.Elapsed, enemy, availableContent);
                newState.Zombies.Add(enemy);
            }
            if (snapshot.nextStatusSequence <= maxStatusSequence || snapshot.nextStatusSequence <= 0)
                return Failure(BattleSnapshotRestoreCode.InvalidIdentity, "nextStatusSequence",
                    "Next status sequence must exceed every live status sequence.");

            var plantLookup = newState.Plants.ToDictionary(value => value.Id);
            foreach (var value in projectileDtos)
            {
                CompiledProjectileDefinition definition;
                if (string.IsNullOrEmpty(value.definitionId)
                    || !availableContent.RuntimeProjectiles.TryGetValue(value.definitionId, out definition))
                    return Failure(BattleSnapshotRestoreCode.UnknownDefinition,
                        "projectiles[" + value.entityId + "].definitionId", "Projectile definition is unavailable.");
                if (value.plantEntityId != 0 && !plantIds.Contains(value.plantEntityId))
                    return Failure(BattleSnapshotRestoreCode.InvalidReference,
                        "projectiles[" + value.entityId + "].plantEntityId", "Projectile source plant is missing.");
                if (value.targetEntityId != -1 && !enemyIds.Contains(value.targetEntityId))
                    return Failure(BattleSnapshotRestoreCode.InvalidReference,
                        "projectiles[" + value.entityId + "].targetEntityId", "Projectile target enemy is missing.");
                if (value.hitEntityIds == null)
                    return Failure(BattleSnapshotRestoreCode.InvalidPayload,
                        "projectiles[" + value.entityId + "].hitEntityIds", "Projectile hit history must not be null.");
                if (!Finite(value.originX) || !Finite(value.originY) || !Finite(value.positionX)
                    || !Finite(value.positionY) || !Finite(value.targetX) || !Finite(value.targetY)
                    || !Finite(value.directionX) || !Finite(value.directionY)
                    || !FiniteNonNegative(value.maxDistance) || !FiniteNonNegative(value.progress)
                    || !FiniteNonNegative(value.damage) || value.ticksRemaining <= 0 || value.flightTicks < 0)
                    return Failure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "projectiles[" + value.entityId + "]", "Projectile geometry, damage, or tick counters are invalid.");
                if (definition.Mode == BattleProjectileMode.LinearReturn && value.progress > value.maxDistance + .0001f)
                    return Failure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "projectiles[" + value.entityId + "].progress", "Returning projectile progress exceeds its range.");
                var hitCounts = new Dictionary<int, int>();
                foreach (var hitId in value.hitEntityIds)
                {
                    if (!enemyIds.Contains(hitId))
                        return Failure(BattleSnapshotRestoreCode.InvalidReference,
                            "projectiles[" + value.entityId + "].hitEntityIds", "Projectile hit history references a missing enemy.");
                    int count;
                    hitCounts.TryGetValue(hitId, out count);
                    count++;
                    if (count > definition.MaxHitsPerTarget)
                        return Failure(BattleSnapshotRestoreCode.InvalidNumericValue,
                            "projectiles[" + value.entityId + "].hitEntityIds",
                            "Projectile hit history exceeds the definition's per-target limit.");
                    hitCounts[hitId] = count;
                }
                Plant sourcePlant = null;
                if (value.plantEntityId > 0) plantLookup.TryGetValue(value.plantEntityId, out sourcePlant);
                var projectile = new ProjectileFlash
                {
                    Id = value.entityId,
                    PlantId = value.plantEntityId,
                    TargetId = value.targetEntityId,
                    Kind = sourcePlant == null ? PlantKind.Pea : sourcePlant.Kind,
                    Weapon = sourcePlant == null ? WeaponKind.None : sourcePlant.Weapon,
                    ProjectileId = value.definitionId,
                    VisualId = definition.VisualId,
                    ImpactCueId = definition.ImpactCueId,
                    Mode = definition.Mode,
                    Origin = new Vector2(value.originX, value.originY),
                    Position = new Vector2(value.positionX, value.positionY),
                    TargetPoint = new Vector2(value.targetX, value.targetY),
                    Direction = new Vector2(value.directionX, value.directionY),
                    MaxDistance = value.maxDistance,
                    Progress = value.progress,
                    Returning = value.returning,
                    Damage = value.damage,
                    TicksRemaining = value.ticksRemaining,
                    FlightTicks = value.flightTicks,
                    Ttl = BattleSkillTiming.TicksToSeconds(value.ticksRemaining),
                };
                projectile.HitIds.AddRange(value.hitEntityIds);
                newState.Projectiles.Add(projectile);
            }

            candidate = newState;
            return BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult ValidateEntityIdentities(BattleSnapshotV1 snapshot,
            HashSet<int> ids, ref int maxEntityId)
        {
            foreach (var entry in EnumerateEntityIds(snapshot))
            {
                if (entry.Id <= 0)
                    return Failure(BattleSnapshotRestoreCode.InvalidIdentity, entry.Path,
                        "Entity IDs must be positive.");
                if (!ids.Add(entry.Id))
                    return Failure(BattleSnapshotRestoreCode.InvalidIdentity, entry.Path,
                        "Entity IDs must be globally unique.");
                maxEntityId = Math.Max(maxEntityId, entry.Id);
            }
            return BattleSnapshotRestoreResult.Ok();
        }

        private static IEnumerable<EntityIdentity> EnumerateEntityIds(BattleSnapshotV1 snapshot)
        {
            foreach (var value in snapshot.pots)
            {
                if (value == null) yield return new EntityIdentity(0, "pots");
                else yield return new EntityIdentity(value.entityId, "pots[" + value.entityId + "].entityId");
            }
            foreach (var value in snapshot.plants)
            {
                if (value == null) yield return new EntityIdentity(0, "plants");
                else yield return new EntityIdentity(value.entityId, "plants[" + value.entityId + "].entityId");
            }
            foreach (var value in snapshot.enemies)
            {
                if (value == null) yield return new EntityIdentity(0, "enemies");
                else yield return new EntityIdentity(value.entityId, "enemies[" + value.entityId + "].entityId");
            }
            foreach (var value in snapshot.projectiles)
            {
                if (value == null) yield return new EntityIdentity(0, "projectiles");
                else yield return new EntityIdentity(value.entityId, "projectiles[" + value.entityId + "].entityId");
            }
        }

        private static BattleSnapshotRestoreResult RestoreInventory(BattleSnapshotV1 snapshot,
            CompiledBattleContentCatalog content, Inventory inventory)
        {
            var definitions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in snapshot.equipment)
            {
                if (value == null || string.IsNullOrEmpty(value.definitionId)
                    || !content.Equipment.ContainsKey(value.definitionId))
                    return Failure(BattleSnapshotRestoreCode.UnknownDefinition, "equipment",
                        "Inventory equipment definition is unavailable.");
                if (!definitions.Add(value.definitionId))
                    return Failure(BattleSnapshotRestoreCode.InvalidIdentity, "equipment",
                        "Inventory equipment definitions must be unique.");
                if (value.count < 0)
                    return Failure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "equipment[" + value.definitionId + "].count", "Inventory counts must be nonnegative.");
                WeaponKind kind;
                if (!TryLegacyWeapon(value.definitionId, out kind))
                    return Failure(BattleSnapshotRestoreCode.UnknownDefinition,
                        "equipment[" + value.definitionId + "]", "Runtime inventory does not support this equipment definition.");
                inventory.Add(kind, value.count);
            }
            inventory.Pots = snapshot.availablePots;
            return BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult ValidatePlantEquipment(BattleSnapshotPlantV1 value,
            CompiledBattleContentCatalog content)
        {
            var equipmentId = value.equipmentDefinitionId ?? string.Empty;
            var legacyWeapon = (WeaponKind)value.legacyWeapon;
            if (string.IsNullOrEmpty(equipmentId))
            {
                return legacyWeapon == WeaponKind.None
                    ? BattleSnapshotRestoreResult.Ok()
                    : Failure(BattleSnapshotRestoreCode.InvalidReference,
                        "plants[" + value.entityId + "].equipmentDefinitionId",
                        "Legacy weapon state requires an equipment definition.");
            }
            EquipmentDefinitionDto equipment;
            if (!content.Equipment.TryGetValue(equipmentId, out equipment))
                return Failure(BattleSnapshotRestoreCode.UnknownDefinition,
                    "plants[" + value.entityId + "].equipmentDefinitionId", "Equipment definition is unavailable.");
            if (!equipment.compatiblePlantIds.Contains(value.definitionId))
                return Failure(BattleSnapshotRestoreCode.InvalidReference,
                    "plants[" + value.entityId + "].equipmentDefinitionId", "Equipment is incompatible with the plant definition.");
            WeaponKind expectedLegacy;
            if (TryLegacyWeapon(equipmentId, out expectedLegacy) && expectedLegacy != legacyWeapon)
                return Failure(BattleSnapshotRestoreCode.InvalidReference,
                    "plants[" + value.entityId + "].legacyWeapon", "Legacy weapon and equipment definition disagree.");
            return BattleSnapshotRestoreResult.Ok();
        }

        private static void RebuildLegacyStatusViews(float elapsed, Zombie zombie,
            CompiledBattleContentCatalog content)
        {
            zombie.SlowUntil = elapsed;
            zombie.FreezeUntil = elapsed;
            zombie.HitStunUntil = elapsed;
            zombie.IceHits = 0;
            zombie.Burns.Clear();
            foreach (var status in zombie.Statuses.OrderBy(value => value.Sequence))
            {
                var definition = content.RuntimeStatuses[status.DefinitionId];
                var until = elapsed + BattleSkillTiming.TicksToSeconds(status.RemainingTicks);
                if (definition.Kind == BattleStatusKind.Slow) zombie.SlowUntil = Mathf.Max(zombie.SlowUntil, until);
                else if (definition.Kind == BattleStatusKind.Freeze) zombie.FreezeUntil = Mathf.Max(zombie.FreezeUntil, until);
                else if (definition.Kind == BattleStatusKind.Stun) zombie.HitStunUntil = Mathf.Max(zombie.HitStunUntil, until);
                else if (definition.Kind == BattleStatusKind.HitCount) zombie.IceHits = status.StackCount;
                else if (definition.Kind == BattleStatusKind.Burn)
                    zombie.Burns.Add(new BurnStack
                    {
                        Remaining = BattleSkillTiming.TicksToSeconds(status.RemainingTicks),
                        DamagePerSecond = status.Magnitude,
                    });
            }
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool FiniteNonNegative(float value)
        {
            return Finite(value) && value >= 0f;
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        private static string ResolveMapIdentity(BattlefieldMapDefinition map, bool bundledDefault)
        {
            if (bundledDefault) return BattleSnapshotSchema.DefaultMapId;
            const ulong offset = 14695981039346656037ul;
            var hash = offset;
            AddMapHash(ref hash, map.GridWidth);
            AddMapHash(ref hash, map.GridHeight);
            AddMapHash(ref hash, map.LegacyToMapScale);
            foreach (var cell in map.PlantableCells.OrderBy(value => value.x).ThenBy(value => value.y))
            {
                AddMapHash(ref hash, cell.x);
                AddMapHash(ref hash, cell.y);
            }
            foreach (var node in map.RouteNodes)
            {
                AddMapHash(ref hash, node.x);
                AddMapHash(ref hash, node.y);
            }
            AddMapHash(ref hash, map.Core.x);
            AddMapHash(ref hash, map.Core.y);
            foreach (var groupName in map.InitialPotGroupOrder)
            {
                AddMapHash(ref hash, groupName);
                var group = map.InitialPotGroups[groupName];
                AddMapHash(ref hash, group.InitialCount);
                foreach (var cell in group.Cells)
                {
                    AddMapHash(ref hash, cell.x);
                    AddMapHash(ref hash, cell.y);
                }
            }
            return "map." + hash.ToString("x16", CultureInfo.InvariantCulture);
        }

        private static void AddMapHash(ref ulong hash, float value)
        {
            AddMapHash(ref hash, BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        }

        private static void AddMapHash(ref ulong hash, int value)
        {
            var raw = unchecked((uint)value);
            for (var shift = 0; shift < 32; shift += 8)
            {
                hash ^= (byte)(raw >> shift);
                hash *= 1099511628211ul;
            }
        }

        private static void AddMapHash(ref ulong hash, string value)
        {
            foreach (var part in Encoding.UTF8.GetBytes(value ?? string.Empty))
            {
                hash ^= part;
                hash *= 1099511628211ul;
            }
            hash ^= 0xff;
            hash *= 1099511628211ul;
        }

        private static bool TryLegacyWeapon(string definitionId, out WeaponKind kind)
        {
            if (definitionId == BattleContentIds.Equipment.Gatling) { kind = WeaponKind.Gatling; return true; }
            if (definitionId == BattleContentIds.Equipment.Ice) { kind = WeaponKind.Ice; return true; }
            if (definitionId == BattleContentIds.Equipment.Chili) { kind = WeaponKind.Chili; return true; }
            kind = WeaponKind.None;
            return false;
        }

        private static ZombieKind LegacyEnemyKind(string definitionId)
        {
            if (definitionId == BattleContentIds.Enemies.Runner) return ZombieKind.Runner;
            if (definitionId == BattleContentIds.Enemies.Armored) return ZombieKind.Armored;
            if (definitionId == BattleContentIds.Enemies.Boss) return ZombieKind.Boss;
            return ZombieKind.Normal;
        }

        private static BattleSnapshotRestoreResult Failure(BattleSnapshotRestoreCode code, string path, string message)
        {
            return new BattleSnapshotRestoreResult(code, path, message);
        }

        private readonly struct EntityIdentity
        {
            public int Id { get; }
            public string Path { get; }

            public EntityIdentity(int id, string path)
            {
                Id = id;
                Path = path;
            }
        }
    }
}
