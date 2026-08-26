using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        private const int OutcomeProjectionVersion = 4;

        public string OutcomeStateChecksum()
        {
            var projection = CreateDeterministicOutcomeProjection();
            using (var hash = SHA256.Create())
            {
                var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(projection));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                    result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private string CreateDeterministicOutcomeProjection()
        {
            var routeState = new StringBuilder();
            routeState.Append((int)Mode).Append('\n')
                .Append(State.EscapedEnemies.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var zombie in State.Zombies.OrderBy(value => value.Id))
                routeState.Append(zombie.Id.ToString(CultureInfo.InvariantCulture)).Append(':')
                    .Append(zombie.RouteId).Append('\n');
            var projection = routeState + JsonUtility.ToJson(CreateOutcomeProjection(), false);
            if (ActiveLevel != null)
                projection = Identity.MapId + "\n" + Identity.WaveSetId + "\n"
                    + Identity.RuleSetId + "\n" + projection;
            return projection;
        }

        private OutcomeSnapshotProjection CreateOutcomeProjection()
        {
            var livePlants = new System.Collections.Generic.HashSet<int>(
                State.Plants.Select(value => value.Id));
            var liveEnemies = new System.Collections.Generic.HashSet<int>(
                State.Zombies.Select(value => value.Id));
            var liveEntities = new System.Collections.Generic.HashSet<int>(livePlants);
            liveEntities.UnionWith(liveEnemies);
            return new OutcomeSnapshotProjection
            {
                projectionVersion = OutcomeProjectionVersion,
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
                equipment = State.Inventory.Equipment.Select(value => new OutcomeEquipment
                {
                    definitionId = value.Key,
                    count = value.Value,
                }).ToArray(),
                pots = State.Pots.OrderBy(value => value.Id).Select(value => new OutcomePot
                {
                    entityId = value.Id,
                    cellX = value.Cell.x,
                    cellY = value.Cell.y,
                    active = value.Active,
                }).ToArray(),
                plants = State.Plants.OrderBy(value => value.Id).Select(value => new OutcomePlant
                {
                    entityId = value.Id,
                    definitionId = value.DefinitionId,
                    star = value.Star,
                    potEntityId = value.PotId,
                    nurseryIndex = value.NurseryIndex,
                    equipmentDefinitionId = value.EquipmentId ?? string.Empty,
                    moveCooldown = value.MoveCooldown,
                }).ToArray(),
                enemies = State.Zombies.OrderBy(value => value.Id).Select(value => new OutcomeEnemy
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
                    .Select(value => CreateOutcomeProjectile(value, livePlants, liveEnemies))
                    .ToArray(),
                combatRuntime = new OutcomeCombatRuntime
                {
                    nextCombatEventSequence = State.NextCombatEventSequence,
                    entities = CombatEntities().Select(entity => new OutcomeEntityRuntime
                    {
                        entityId = entity.Id,
                        abilities = CreateOutcomeAbilities(entity, liveEntities),
                        statuses = entity.Statuses.OrderBy(status => status.Sequence)
                            .Select(status => CreateOutcomeStatus(status, liveEntities)).ToArray(),
                    }).ToArray(),
                },
            };
        }

        private OutcomeAbilityRuntime[] CreateOutcomeAbilities(CombatEntityState entity,
            System.Collections.Generic.HashSet<int> liveEntityIds)
        {
            var loadout = ResolveAbilities(entity);
            return loadout.Select(definition =>
            {
                var runtime = entity.AbilityRuntimes.FirstOrDefault(value =>
                    value.AbilityId == definition.Id)
                    ?? new AbilityRuntimeState { AbilityId = definition.Id };
                return new OutcomeAbilityRuntime
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
            }).ToArray();
        }

        private static OutcomeStatus CreateOutcomeStatus(StatusInstance value,
            System.Collections.Generic.HashSet<int> liveEntityIds)
        {
            return new OutcomeStatus
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

        private static OutcomeProjectile CreateOutcomeProjectile(ProjectileFlash value,
            System.Collections.Generic.HashSet<int> livePlants,
            System.Collections.Generic.HashSet<int> liveEnemies)
        {
            return new OutcomeProjectile
            {
                entityId = value.Id,
                sourceEntityId = value.SourceEntityId > 0 && livePlants.Contains(value.SourceEntityId)
                    ? value.SourceEntityId : 0,
                targetEntityId = value.TargetId > 0 && liveEnemies.Contains(value.TargetId)
                    ? value.TargetId : -1,
                definitionId = value.ProjectileId,
                sourceDefinitionId = value.SourceDefinitionId,
                sourceEquipmentId = value.SourceEquipmentId,
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
                hitEntityIds = value.HitIds.Where(liveEnemies.Contains).ToArray(),
            };
        }

        [Serializable]
        private sealed class OutcomeSnapshotProjection
        {
            public int projectionVersion;
            public string catalogId;
            public string contentVersion;
            public string mapId;
            public int logicStep;
            public uint randomState;
            public int randomSeed;
            public int phase;
            public bool paused;
            public int speed;
            public float elapsed;
            public int sun;
            public int lives;
            public int refreshCount;
            public int waveIndex;
            public int waveSpawned;
            public int waveTotal;
            public float spawnCooldown;
            public float betweenTimer;
            public int nextEntityId;
            public int nextStatusSequence;
            public int availablePots;
            public OutcomeEquipment[] equipment;
            public OutcomePot[] pots;
            public OutcomePlant[] plants;
            public OutcomeEnemy[] enemies;
            public OutcomeProjectile[] projectiles;
            public OutcomeCombatRuntime combatRuntime;
        }

        [Serializable] private sealed class OutcomeEquipment { public string definitionId; public int count; }
        [Serializable] private sealed class OutcomePot { public int entityId; public int cellX; public int cellY; public bool active; }
        [Serializable] private sealed class OutcomePlant
        {
            public int entityId; public string definitionId; public int star;
            public int potEntityId; public int nurseryIndex;
            public string equipmentDefinitionId; public float moveCooldown;
        }
        [Serializable] private sealed class OutcomeEnemy
        {
            public int entityId; public string definitionId; public float hp; public float maxHp;
            public float speed; public float pathProgress; public int reward; public int threat;
        }
        [Serializable] private sealed class OutcomeProjectile
        {
            public int entityId; public int sourceEntityId; public int targetEntityId;
            public string definitionId; public string sourceDefinitionId;
            public string sourceEquipmentId; public string abilityId; public int deliveryIndex;
            public float originX; public float originY; public float positionX; public float positionY;
            public float targetX; public float targetY; public float directionX; public float directionY;
            public float maxDistance; public float progress; public bool returning;
            public float damageBasis; public int ticksRemaining; public int flightTicks;
            public int[] hitEntityIds;
        }
        [Serializable] private sealed class OutcomeCombatRuntime
        {
            public long nextCombatEventSequence;
            public OutcomeEntityRuntime[] entities;
        }
        [Serializable] private sealed class OutcomeEntityRuntime
        {
            public int entityId; public OutcomeAbilityRuntime[] abilities;
            public OutcomeStatus[] statuses;
        }
        [Serializable] private sealed class OutcomeAbilityRuntime
        {
            public string definitionId; public int phase; public int cooldownTicks;
            public int periodicProgressTicks; public int windupTicksRemaining;
            public int recoveryTicksRemaining; public int burstShotsRemaining;
            public int burstIntervalTicks; public int pendingSourceEntityId;
            public int pendingTargetEntityId; public float pendingEventMagnitude;
            public long pendingRootEventSequence; public long lastRootEventSequence;
        }
        [Serializable] private sealed class OutcomeStatus
        {
            public string definitionId; public int sourceEntityId; public int remainingTicks;
            public int stackCount; public float magnitude; public int sequence;
            public int tickProgress;
        }
    }
}
