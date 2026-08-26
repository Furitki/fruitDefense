using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        public BattleSnapshotRestoreResult RestoreSnapshot(
            BattleSnapshot snapshot, CompiledLevelCatalog availableCatalog)
        {
            try
            {
                ResolvedLevelDefinition resolved;
                var sourceResult = ValidateCurrentSnapshotSource(snapshot, availableCatalog,
                    out resolved);
                if (!sourceResult.Succeeded) return sourceResult;

                GameState candidate;
                var candidateResult = TryBuildCurrentSnapshotCandidate(snapshot, resolved,
                    out candidate);
                if (!candidateResult.Succeeded) return candidateResult;

                CommitCurrentSnapshotCandidate(candidate, snapshot.randomState);
                return BattleSnapshotRestoreResult.Ok();
            }
            catch (Exception exception)
            {
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidPayload,
                    "$", exception.Message);
            }
        }

        private BattleSnapshotRestoreResult TryBuildCurrentSnapshotCandidate(
            BattleSnapshot snapshot, ResolvedLevelDefinition resolved, out GameState candidate)
        {
            candidate = null;
            var content = resolved.BattleContent;
            if (snapshot.equipment == null || snapshot.pots == null || snapshot.plants == null
                || snapshot.enemies == null || snapshot.projectiles == null
                || snapshot.combatRuntime == null
                || snapshot.combatRuntime.entities == null)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidPayload, "$",
                    "Snapshot collections and combat runtime are required.");
            if (!Enum.IsDefined(typeof(GamePhase), snapshot.phase))
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                    "phase", "Unknown battle phase.");
            if (snapshot.logicStep < 0 || snapshot.randomState == 0u
                || snapshot.speed < 1 || snapshot.speed > 2
                || !FiniteNonNegative(snapshot.elapsed) || snapshot.sun < 0
                || snapshot.lives < 0 || snapshot.refreshCount < 0
                || snapshot.waveIndex < 0 || snapshot.waveSpawned < 0
                || snapshot.waveTotal < 0
                || !Finite(snapshot.spawnCooldown) || !FiniteNonNegative(snapshot.betweenTimer)
                || snapshot.escapedEnemyCount < 0 || snapshot.availablePots < 0)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                    "battleState", "Battle counters, timers, random state, speed, or resources are invalid.");
            var waveResult = ValidateCurrentWaveState(snapshot, resolved);
            if (!waveResult.Succeeded) return waveResult;

            var allEntityIds = new HashSet<int>();
            var maxEntityId = 0;
            foreach (var entry in EnumerateCurrentEntityIds(snapshot))
            {
                if (entry.Id <= 0 || !allEntityIds.Add(entry.Id))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidIdentity,
                        entry.Path, "Entity IDs must be positive and globally unique.");
                maxEntityId = Math.Max(maxEntityId, entry.Id);
            }
            if (snapshot.nextEntityId <= maxEntityId || snapshot.nextEntityId <= 0)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidIdentity,
                    "nextEntityId", "Next entity ID must exceed every live entity ID.");

            var pots = snapshot.pots.OrderBy(value => value.entityId).ToArray();
            var plants = snapshot.plants.OrderBy(value => value.entityId).ToArray();
            var enemies = snapshot.enemies.OrderBy(value => value.entityId).ToArray();
            var projectiles = snapshot.projectiles.OrderBy(value => value.entityId).ToArray();
            var potIds = new HashSet<int>(pots.Select(value => value.entityId));
            var plantIds = new HashSet<int>(plants.Select(value => value.entityId));
            var enemyIds = new HashSet<int>(enemies.Select(value => value.entityId));

            var next = new GameState
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
                EscapedEnemies = snapshot.escapedEnemyCount,
                NextId = snapshot.nextEntityId,
                RandomSeed = snapshot.randomSeed,
                LogicTick = snapshot.logicStep,
                NextStatusSequence = snapshot.nextStatusSequence,
            };

            var inventoryResult = RestoreCurrentInventory(snapshot, content, next.Inventory);
            if (!inventoryResult.Succeeded) return inventoryResult;

            var occupiedCells = new HashSet<Vector2Int>();
            foreach (var value in pots)
            {
                var cell = new Vector2Int(value.cellX, value.cellY);
                if (!resolved.Map.IsPlantable(cell) || !occupiedCells.Add(cell))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                        "pots[" + value.entityId + "].cell",
                        "Pot cell must be a unique plantable cell in the resolved map.");
                next.Pots.Add(new Pot { Id = value.entityId, Cell = cell, Active = value.active });
            }

            var occupiedPots = new HashSet<int>();
            var occupiedNurserySlots = new HashSet<int>();
            foreach (var value in plants)
            {
                if (string.IsNullOrEmpty(value.definitionId)
                    || !content.Plants.ContainsKey(value.definitionId))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnknownDefinition,
                        "plants[" + value.entityId + "].definitionId",
                        "Plant definition is unavailable.");
                if (value.star < 1 || value.star > 4 || !FiniteNonNegative(value.moveCooldown))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "plants[" + value.entityId + "]", "Plant runtime values are invalid.");
                if (value.potEntityId >= 0 == (value.nurseryIndex >= 0))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                        "plants[" + value.entityId + "].location",
                        "Plant must occupy exactly one pot or nursery slot.");
                if (value.potEntityId >= 0)
                {
                    if (!potIds.Contains(value.potEntityId)
                        || !next.Pots.Any(pot => pot.Id == value.potEntityId && pot.Active)
                        || !occupiedPots.Add(value.potEntityId))
                        return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                            "plants[" + value.entityId + "].potEntityId",
                            "Plant pot reference is missing, inactive, or occupied.");
                }
                else if (value.nurseryIndex < 0
                    || value.nurseryIndex >= resolved.RuleSet.NurserySlotCount
                    || !occupiedNurserySlots.Add(value.nurseryIndex))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                        "plants[" + value.entityId + "].nurseryIndex",
                        "Nursery slot is invalid or occupied.");

                var equipmentResult = ValidateCurrentPlantEquipment(value, content);
                if (!equipmentResult.Succeeded) return equipmentResult;
                next.Plants.Add(new Plant
                {
                    Id = value.entityId,
                    DefinitionId = value.definitionId,
                    Star = value.star,
                    PotId = value.potEntityId,
                    NurseryIndex = value.nurseryIndex,
                    EquipmentId = value.equipmentDefinitionId ?? string.Empty,
                    MoveCooldown = value.moveCooldown,
                });
            }

            foreach (var value in enemies)
            {
                if (string.IsNullOrEmpty(value.definitionId)
                    || !content.Enemies.ContainsKey(value.definitionId))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnknownDefinition,
                        "enemies[" + value.entityId + "].definitionId",
                        "Enemy definition is unavailable.");
                if (!Finite(value.hp) || !Finite(value.maxHp) || value.hp <= 0f
                    || value.maxHp <= 0f || value.hp > value.maxHp
                    || !FiniteNonNegative(value.speed) || !FiniteNonNegative(value.pathProgress)
                    || value.pathProgress >= resolved.Map.RouteLength(resolved.Map.PrimaryRouteId)
                    || value.reward < 0 || value.threat <= 0)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "enemies[" + value.entityId + "]", "Enemy runtime values are invalid.");
                next.Zombies.Add(new Zombie
                {
                    Id = value.entityId,
                    DefinitionId = value.definitionId,
                    RouteId = resolved.Map.PrimaryRouteId,
                    Hp = value.hp,
                    MaxHp = value.maxHp,
                    Speed = value.speed,
                    PathProgress = value.pathProgress,
                    Reward = value.reward,
                    Threat = value.threat,
                });
            }

            foreach (var value in projectiles)
            {
                var result = RestoreCurrentProjectile(value, content, next, plantIds, enemyIds);
                if (!result.Succeeded) return result;
            }

            var runtimeResult = RestoreCurrentCombatRuntime(snapshot.combatRuntime, content, next);
            if (!runtimeResult.Succeeded) return runtimeResult;
            candidate = next;
            return BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult RestoreCurrentInventory(
            BattleSnapshot snapshot, CompiledBattleContentCatalog content, Inventory inventory)
        {
            var definitions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in snapshot.equipment)
            {
                if (value == null || string.IsNullOrEmpty(value.definitionId)
                    || !content.Equipment.ContainsKey(value.definitionId))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnknownDefinition,
                        "equipment", "Inventory equipment definition is unavailable.");
                if (!definitions.Add(value.definitionId))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidIdentity,
                        "equipment", "Equipment definitions must be unique.");
                if (value.count < 0)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "equipment[" + value.definitionId + "].count",
                        "Inventory counts must be nonnegative.");
                inventory.Add(value.definitionId, value.count);
            }
            inventory.Pots = snapshot.availablePots;
            return BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult ValidateCurrentPlantEquipment(
            BattleSnapshotPlant value, CompiledBattleContentCatalog content)
        {
            var equipmentId = value.equipmentDefinitionId ?? string.Empty;
            if (string.IsNullOrEmpty(equipmentId)) return BattleSnapshotRestoreResult.Ok();
            EquipmentDefinitionDto equipment;
            if (!content.Equipment.TryGetValue(equipmentId, out equipment))
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnknownDefinition,
                    "plants[" + value.entityId + "].equipmentDefinitionId",
                    "Equipment definition is unavailable.");
            if (!equipment.compatiblePlantIds.Contains(value.definitionId))
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                    "plants[" + value.entityId + "].equipmentDefinitionId",
                    "Equipment is incompatible with the plant definition.");
            return BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult RestoreCurrentProjectile(
            BattleSnapshotProjectile value, CompiledBattleContentCatalog content,
            GameState next, HashSet<int> plantIds, HashSet<int> enemyIds)
        {
            CompiledProjectileDefinition definition;
            var path = "projectiles[" + value.entityId + "]";
            if (string.IsNullOrEmpty(value.definitionId)
                || !content.RuntimeProjectiles.TryGetValue(value.definitionId, out definition))
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnknownDefinition,
                    path + ".definitionId", "Projectile definition is unavailable.");
            if (value.sourceEntityId != 0 && !plantIds.Contains(value.sourceEntityId))
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                    path + ".sourceEntityId", "Projectile source plant is missing.");
            if (string.IsNullOrEmpty(value.sourceDefinitionId)
                || !content.Plants.ContainsKey(value.sourceDefinitionId))
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnknownDefinition,
                    path + ".sourceDefinitionId", "Projectile source definition is unavailable.");
            if (value.sourceEntityId != 0)
            {
                var source = next.Plants.First(plant => plant.Id == value.sourceEntityId);
                if (!StringEquals(source.DefinitionId, value.sourceDefinitionId)
                    || !StringEquals(source.EquipmentId ?? string.Empty,
                        value.sourceEquipmentId ?? string.Empty))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                        path + ".sourceDefinitionId",
                        "Projectile source identity does not match the live source plant.");
            }
            IReadOnlyList<CompiledAbilityDefinition> loadout;
            try
            {
                loadout = content.ResolvePlantAbilities(value.sourceDefinitionId,
                    value.sourceEquipmentId ?? string.Empty);
            }
            catch (Exception exception)
            {
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnknownDefinition,
                    path + ".sourceEquipmentId", exception.Message);
            }
            var ability = loadout.FirstOrDefault(item => item.Id == value.abilityId);
            if (ability == null || value.deliveryIndex < 0
                || value.deliveryIndex >= ability.Deliveries.Count
                || ability.Deliveries[value.deliveryIndex].Kind != AbilityDeliveryKind.Projectile
                || ability.Deliveries[value.deliveryIndex].ProjectileId != value.definitionId)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnknownDefinition,
                    path + ".abilityId", "Projectile ability delivery is unavailable.");
            if (value.targetEntityId != -1 && !enemyIds.Contains(value.targetEntityId))
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                    path + ".targetEntityId", "Projectile target enemy is missing.");
            if (value.hitEntityIds == null)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidPayload,
                    path + ".hitEntityIds", "Projectile hit history is required.");
            if (!Finite(value.originX) || !Finite(value.originY)
                || !Finite(value.positionX) || !Finite(value.positionY)
                || !Finite(value.targetX) || !Finite(value.targetY)
                || !Finite(value.directionX) || !Finite(value.directionY)
                || !FiniteNonNegative(value.maxDistance) || !FiniteNonNegative(value.progress)
                || !FiniteNonNegative(value.damageBasis) || value.ticksRemaining <= 0
                || value.flightTicks < 0)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                    path, "Projectile geometry, damage, or counters are invalid.");
            if (definition.Mode == BattleProjectileMode.LinearReturn
                && value.progress > value.maxDistance + .0001f)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                    path + ".progress", "Returning projectile progress exceeds its range.");
            var hitCounts = new Dictionary<int, int>();
            foreach (var hitId in value.hitEntityIds)
            {
                int count;
                if (!enemyIds.Contains(hitId))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                        path + ".hitEntityIds", "Hit history references a missing enemy.");
                hitCounts.TryGetValue(hitId, out count);
                if (++count > definition.MaxHitsPerTarget)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        path + ".hitEntityIds", "Hit history exceeds the per-target limit.");
                hitCounts[hitId] = count;
            }
            var projectile = new ProjectileFlash
            {
                Id = value.entityId,
                SourceEntityId = value.sourceEntityId,
                TargetId = value.targetEntityId,
                SourceDefinitionId = value.sourceDefinitionId,
                SourceEquipmentId = value.sourceEquipmentId ?? string.Empty,
                AbilityId = value.abilityId,
                DeliveryIndex = value.deliveryIndex,
                ProjectileId = value.definitionId,
                Origin = new Vector2(value.originX, value.originY),
                Position = new Vector2(value.positionX, value.positionY),
                TargetPoint = new Vector2(value.targetX, value.targetY),
                Direction = new Vector2(value.directionX, value.directionY),
                MaxDistance = value.maxDistance,
                Progress = value.progress,
                Returning = value.returning,
                DamageBasis = value.damageBasis,
                TicksRemaining = value.ticksRemaining,
                FlightTicks = value.flightTicks,
            };
            projectile.HitIds.AddRange(value.hitEntityIds);
            next.Projectiles.Add(projectile);
            return BattleSnapshotRestoreResult.Ok();
        }

        private void CommitCurrentSnapshotCandidate(GameState candidate, uint randomState)
        {
            _random.RestoreState(randomState);
            State = candidate;
            _lastNurseryPotSlots.Clear();
            ResetFrameAccumulator();
            _presentationEvents.Reset();
            _abilityActivationKeys.Clear();
            _abilityDispatchDepth = 0;
            _abilityActivationCount = 0;
            _abilityRootEventSequence = 0L;
        }

        private static IEnumerable<EntityIdentity> EnumerateCurrentEntityIds(
            BattleSnapshot snapshot)
        {
            foreach (var value in snapshot.pots)
                yield return new EntityIdentity(value == null ? 0 : value.entityId, "pots.entityId");
            foreach (var value in snapshot.plants)
                yield return new EntityIdentity(value == null ? 0 : value.entityId, "plants.entityId");
            foreach (var value in snapshot.enemies)
                yield return new EntityIdentity(value == null ? 0 : value.entityId, "enemies.entityId");
            foreach (var value in snapshot.projectiles)
                yield return new EntityIdentity(value == null ? 0 : value.entityId,
                    "projectiles.entityId");
        }

        private static BattleSnapshotRestoreResult CurrentSnapshotFailure(
            BattleSnapshotRestoreCode code, string path, string message)
        {
            return new BattleSnapshotRestoreResult(code, path, message);
        }

    }
}
