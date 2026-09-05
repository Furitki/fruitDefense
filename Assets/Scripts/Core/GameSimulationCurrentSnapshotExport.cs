using System;
using System.Collections.Generic;
using System.Linq;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        public BattleSnapshotExportResult ExportSnapshot()
        {
            if (Mode != BattleSimulationMode.Standard || ResolvedSourceIdentity == null
                || ActiveLevel == null || Identity == null
                || LaunchGrowthSnapshot == null)
                return BattleSnapshotExportResult.Unsupported(
                    "Current battle snapshots require a catalog-resolved Standard session.");

            var livePlantIds = new HashSet<int>(State.Plants.Select(value => value.Id));
            var liveEnemyIds = new HashSet<int>(State.Zombies.Select(value => value.Id));
            var liveEntityIds = new HashSet<int>(livePlantIds);
            liveEntityIds.UnionWith(liveEnemyIds);
            var source = ResolvedSourceIdentity;
            var snapshot = new BattleSnapshot
            {
                schemaId = BattleSnapshotSchema.Id,
                schemaVersion = BattleSnapshotSchema.Version,
                levelCatalogId = source.LevelCatalogId,
                contentCatalogId = source.ContentCatalogId,
                contentVersion = source.ContentVersion,
                levelId = source.LevelId,
                mapId = source.MapId,
                gameplayMapFingerprint = source.GameplayMapFingerprint,
                waveSetId = source.WaveSetId,
                ruleSetId = source.RuleSetId,
                themeId = source.ThemeId,
                growthPolicyId = source.GrowthPolicyId,
                growthContentCatalogId = source.GrowthContentCatalogId,
                growthContentVersion = source.GrowthContentVersion,
                growthContentFingerprint = source.GrowthContentFingerprint,
                growthProfileId = source.GrowthProfileId,
                growthProfileRevision = source.GrowthProfileRevision,
                growthFingerprint = source.GrowthFingerprint,
                resolvedSourceDefinitionFingerprint = source.DefinitionFingerprint,
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
                escapedEnemyCount = State.EscapedEnemies,
                nextEntityId = State.NextId,
                nextStatusSequence = State.NextStatusSequence,
                availablePots = State.Inventory.Pots,
                equipment = State.Inventory.Equipment.Select(value =>
                    new BattleSnapshotEquipment
                    {
                        definitionId = value.Key,
                        count = value.Value,
                    }).ToArray(),
                pots = State.Pots.OrderBy(value => value.Id).Select(value =>
                    new BattleSnapshotPot
                    {
                        entityId = value.Id,
                        cellX = value.Cell.x,
                        cellY = value.Cell.y,
                        active = value.Active,
                    }).ToArray(),
                plants = State.Plants.OrderBy(value => value.Id).Select(value =>
                    new BattleSnapshotPlant
                    {
                        entityId = value.Id,
                        definitionId = value.DefinitionId,
                        star = value.Star,
                        potEntityId = value.PotId,
                        nurseryIndex = value.NurseryIndex,
                        equipmentDefinitionId = value.EquipmentId ?? string.Empty,
                        moveCooldown = value.MoveCooldown,
                    }).ToArray(),
                enemies = State.Zombies.OrderBy(value => value.Id).Select(value =>
                    new BattleSnapshotEnemy
                    {
                        entityId = value.Id,
                        definitionId = value.DefinitionId,
                        hp = value.Hp,
                        maxHp = value.MaxHp,
                        speed = value.Speed,
                        pathProgress = value.PathProgress,
                        reward = value.Reward,
                        threat = value.Threat,
                    }).ToArray(),
                projectiles = State.Projectiles.OrderBy(value => value.Id)
                    .Select(value => ExportCurrentProjectile(value, livePlantIds, liveEnemyIds))
                    .ToArray(),
                combatRuntime = new BattleSnapshotCombatRuntime
                {
                    nextCombatEventSequence = State.NextCombatEventSequence,
                    entities = CombatEntities().Select(entity => new BattleSnapshotEntityRuntime
                    {
                        entityId = entity.Id,
                        abilities = ExportCurrentAbilityRuntimes(entity, liveEntityIds),
                        statuses = entity.Statuses.OrderBy(value => value.Sequence)
                            .Select(value => ExportCurrentStatus(value, liveEntityIds)).ToArray(),
                    }).ToArray(),
                },
            };
            return BattleSnapshotExportResult.Success(snapshot);
        }

        private BattleSnapshotAbilityRuntime[] ExportCurrentAbilityRuntimes(
            CombatEntityState entity, HashSet<int> liveEntityIds)
        {
            var loadout = ResolveAbilities(entity);
            var result = new BattleSnapshotAbilityRuntime[loadout.Count];
            for (var index = 0; index < loadout.Count; index++)
            {
                var definition = loadout[index];
                var runtime = entity.AbilityRuntimes.FirstOrDefault(value =>
                    value.AbilityId == definition.Id)
                    ?? new AbilityRuntimeState { AbilityId = definition.Id };
                result[index] = new BattleSnapshotAbilityRuntime
                {
                    definitionId = runtime.AbilityId,
                    phase = (int)runtime.Phase,
                    cooldownTicks = runtime.CooldownTicks,
                    periodicProgressTicks = runtime.PeriodicProgressTicks,
                    windupTicksRemaining = runtime.WindupTicksRemaining,
                    recoveryTicksRemaining = runtime.RecoveryTicksRemaining,
                    burstShotsRemaining = runtime.BurstShotsRemaining,
                    burstIntervalTicks = runtime.BurstIntervalTicks,
                    pendingSourceEntityId = CanonicalLiveEntityReference(
                        runtime.PendingSourceEntityId, liveEntityIds),
                    pendingTargetEntityId = CanonicalLiveEntityReference(
                        runtime.PendingTargetEntityId, liveEntityIds),
                    pendingEventMagnitude = runtime.PendingEventMagnitude,
                    pendingRootEventSequence = runtime.PendingRootEventSequence,
                    lastRootEventSequence = runtime.LastRootEventSequence,
                };
            }
            return result;
        }

        private static BattleSnapshotStatus ExportCurrentStatus(StatusInstance value,
            HashSet<int> liveEntityIds)
        {
            return new BattleSnapshotStatus
            {
                definitionId = value.DefinitionId,
                sourceEntityId = value.SourceEntityId > 0
                    && liveEntityIds.Contains(value.SourceEntityId) ? value.SourceEntityId : 0,
                remainingTicks = value.RemainingTicks,
                stackCount = value.StackCount,
                magnitude = value.Magnitude,
                sequence = value.Sequence,
                tickProgress = value.TickProgress,
            };
        }

        private static BattleSnapshotProjectile ExportCurrentProjectile(ProjectileFlash value,
            HashSet<int> livePlantIds, HashSet<int> liveEnemyIds)
        {
            return new BattleSnapshotProjectile
            {
                entityId = value.Id,
                sourceEntityId = value.SourceEntityId > 0
                    && livePlantIds.Contains(value.SourceEntityId) ? value.SourceEntityId : 0,
                targetEntityId = value.TargetId > 0
                    && liveEnemyIds.Contains(value.TargetId) ? value.TargetId : -1,
                definitionId = value.ProjectileId,
                sourceDefinitionId = value.SourceDefinitionId,
                sourceEquipmentId = value.SourceEquipmentId ?? string.Empty,
                abilityId = value.AbilityId,
                deliveryIndex = value.DeliveryIndex,
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
                damageBasis = value.DamageBasis,
                ticksRemaining = value.TicksRemaining,
                flightTicks = value.FlightTicks,
                hitEntityIds = value.HitIds.Where(liveEnemyIds.Contains).ToArray(),
            };
        }
    }
}
