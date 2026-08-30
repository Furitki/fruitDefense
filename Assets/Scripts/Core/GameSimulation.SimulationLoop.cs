using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        public void Tick(float unscaledDelta)
        {
            AdvanceFrame(unscaledDelta);
        }

        public int AdvanceFrame(float unscaledDelta)
        {
            if (State.Paused || State.Phase == GamePhase.Victory || State.Phase == GamePhase.Defeat)
            {
                ResetFrameAccumulator();
                return 0;
            }

            var safeDelta = float.IsNaN(unscaledDelta) || float.IsInfinity(unscaledDelta)
                ? 0f
                : Mathf.Clamp(unscaledDelta, 0f, MaxFrameDeltaSeconds);
            var scaledDelta = safeDelta * Mathf.Clamp(State.Speed, 1, 2);
            _frameAccumulator = Math.Min(MaxFrameDeltaSeconds, _frameAccumulator + scaledDelta);

            var steps = 0;
            while (steps < MaxStepsPerFrame && _frameAccumulator + AccumulatorEpsilon >= FixedStepSeconds)
            {
                if (!Step())
                {
                    ResetFrameAccumulator();
                    break;
                }

                _frameAccumulator -= FixedStepSeconds;
                if (_frameAccumulator < AccumulatorEpsilon) _frameAccumulator = 0d;
                steps++;
            }
            return steps;
        }

        public bool Step()
        {
            if (State.Paused || State.Phase == GamePhase.Victory || State.Phase == GamePhase.Defeat) return false;
            const float delta = FixedStepSeconds;
            State.LogicTick++;
            State.Elapsed += delta;
            foreach (var plant in State.Plants) plant.MoveCooldown = Mathf.Max(0f, plant.MoveCooldown - delta);
            if (State.Phase == GamePhase.BetweenWaves)
            {
                State.BetweenTimer = Mathf.Max(0f, State.BetweenTimer - delta);
                if (State.BetweenTimer <= 0f) StartWave(out _);
                return true;
            }
            if (State.Phase != GamePhase.Playing) return true;
            var fixedStepStarting = FixedStepStarting;
            if (fixedStepStarting != null) fixedStepStarting(this);
            AdvanceCombatRuntime();
            if (Mode == BattleSimulationMode.Standard) Spawn(delta);
            AdvanceZombies(delta);
            RunPlants(delta);
            AdvanceProjectiles(delta);
            if (Mode == BattleSimulationMode.Standard) SettleWave();
            return true;
        }

        public void ResetFrameAccumulator()
        {
            _frameAccumulator = 0d;
        }

        public void RestoreRandomState(uint state)
        {
            _random.RestoreState(state);
        }

        public int DrainPresentationEvents(ICollection<BattlePresentationEvent> destination)
        {
            return _presentationEvents.DrainTo(destination);
        }

        public void DiscardPendingPresentationEvents()
        {
            _presentationEvents.DiscardPending();
        }

        private void Spawn(float delta)
        {
            if (State.WaveSpawned >= State.WaveTotal) return;
            var firstSpawnOfWave = State.WaveSpawned == 0;
            var wave = Wave(State.WaveIndex);
            State.SpawnCooldown -= delta;
            while (State.WaveSpawned < State.WaveTotal && State.SpawnCooldown <= 0f)
            {
                var enemyId = wave.enemyIds[State.WaveSpawned];
                SpawnEnemy(enemyId, Map.PrimaryRouteId, wave.healthMultiplier,
                    wave.speedMultiplier);
                State.WaveSpawned++;
                State.SpawnCooldown += wave.spawnIntervalSeconds;
            }
            if (!firstSpawnOfWave || State.WaveSpawned <= 0) return;
            RaiseCombatEvent(CombatEventKind.WaveFirstSpawned, null,
                State.Zombies.OrderBy(value => value.Id).FirstOrDefault(), 0f);
        }

        private Zombie SpawnEnemy(string enemyDefinitionId, string routeId,
            float healthMultiplier, float speedMultiplier)
        {
            EnemyDefinitionDto definition;
            if (string.IsNullOrWhiteSpace(enemyDefinitionId)
                || !_content.Enemies.TryGetValue(enemyDefinitionId, out definition))
                throw new ArgumentException("Unknown enemy definition ID '" + enemyDefinitionId + "'.",
                    nameof(enemyDefinitionId));
            BattlefieldRouteMetrics route;
            if (!Map.TryGetRoute(routeId, out route))
                throw new ArgumentException("Unknown battlefield route ID '" + routeId + "'.",
                    nameof(routeId));
            if (healthMultiplier <= 0f || float.IsNaN(healthMultiplier)
                || float.IsInfinity(healthMultiplier))
                throw new ArgumentOutOfRangeException(nameof(healthMultiplier));
            if (speedMultiplier <= 0f || float.IsNaN(speedMultiplier)
                || float.IsInfinity(speedMultiplier))
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier));

            var hp = Mathf.Round(definition.health * healthMultiplier);
            var zombie = new Zombie
            {
                Id = State.NextId++,
                DefinitionId = enemyDefinitionId,
                RouteId = routeId,
                Hp = hp,
                MaxHp = hp,
                Speed = Map.FromLegacyDistance(definition.speedLegacyUnits) * speedMultiplier,
                Reward = definition.killReward,
                Threat = definition.threat,
            };
            State.Zombies.Add(zombie);
            return zombie;
        }

        private void AdvanceZombies(float delta)
        {
            for (var index = State.Zombies.Count - 1; index >= 0; index--)
            {
                var zombie = State.Zombies[index];
                if (zombie.Hp <= 0f) { KillZombie(index, zombie); continue; }
                var speed = GetEffectiveAttribute(zombie, CombatAttributeKind.MoveSpeed, zombie.Speed);
                if (!CombatStatusRuntime.BlocksMovement(zombie, _content)) zombie.PathProgress += speed * delta;
                if (zombie.PathProgress < Map.RouteLength(zombie.RouteId)) continue;
                State.Zombies.RemoveAt(index);
                State.EscapedEnemies++;
                if (Mode == BattleSimulationMode.GmStress)
                {
                    continue;
                }
                State.Lives -= zombie.Threat;
                _presentationEvents.EmitBattleStateChanged(State.LogicTick,
                    BattleContentIds.BattleStates.CoreDamaged, Map.Core, zombie.Threat,
                    State.Lives);
                if (State.Lives <= 0) { State.Lives = 0; State.Phase = GamePhase.Defeat; }
            }
        }

        private void RunPlants(float delta)
        {
            foreach (var plant in State.Plants.OrderBy(value => value.Id))
            {
                if (plant.PotId < 0 || plant.MoveCooldown > 0f) continue;
                var abilities = ResolveAbilities(plant);
                EnsureAbilityRuntimes(plant, abilities);
                for (var index = 0; index < abilities.Count; index++)
                {
                    var ability = abilities[index];
                    if (ability.Activation.Kind == AbilityActivationKind.CombatEvent) continue;
                    var runtime = plant.AbilityRuntimes[index];
                    if (ability.Activation.Kind == AbilityActivationKind.Periodic)
                    {
                        runtime.PeriodicProgressTicks++;
                        if (runtime.PeriodicProgressTicks < ability.Activation.PeriodTicks
                            || !CanAcceptActivation(runtime)) continue;
                        var target = FindActivationTarget(plant, ability, null, null);
                        if (target == null && !CanActivateWithoutTarget(ability)) continue;
                        runtime.PeriodicProgressTicks -= ability.Activation.PeriodTicks;
                        AcceptAbility(plant, ability, runtime, plant, target, 0f, 0L);
                        continue;
                    }
                    if (runtime.CooldownTicks > 0 || !CanAcceptActivation(runtime)) continue;
                    var cooldownTarget = FindActivationTarget(plant, ability, null, null);
                    if (cooldownTarget == null && !CanActivateWithoutTarget(ability)) continue;
                    AcceptAbility(plant, ability, runtime, plant, cooldownTarget, 0f, 0L);
                }
            }
        }


        private void SettleWave()
        {
            if (State.Phase != GamePhase.Playing || State.WaveSpawned < State.WaveTotal || State.Zombies.Count > 0) return;
            var completed = State.WaveIndex;
            var wave = Wave(completed);
            State.Sun += wave.completionReward;
            GrantMilestone(completed);
            _presentationEvents.EmitBattleStateChanged(State.LogicTick,
                BattleContentIds.BattleStates.WaveCompleted, Map.Core,
                wave.completionReward, completed);
            if (completed >= _ruleSet.MaxWaves) State.Phase = GamePhase.Victory;
            else { State.Phase = GamePhase.BetweenWaves; State.BetweenTimer = _ruleSet.BetweenWaveSeconds; }
        }

        private void GrantMilestone(int wave)
        {
            var reward = _ruleSet.MilestoneRewards.FirstOrDefault(value => value.Wave == wave);
            if (reward == null) return;
            foreach (var equipmentId in reward.EquipmentIds)
                State.Inventory.Add(equipmentId, 1);
            State.Inventory.Pots += reward.PotCount;
            _presentationEvents.EmitBattleStateChanged(State.LogicTick,
                BattleContentIds.BattleStates.MilestoneReward, Map.Core,
                reward.PotCount, reward.EquipmentIds.Count);
        }

        private WaveDefinitionDto Wave(int index)
        {
            if (index <= 0 || index > _orderedWaves.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _orderedWaves[index - 1];
        }

        private UpgradeTierDefinitionDto UpgradeTier(Plant plant)
        {
            return _content.ResolvePlantUpgradeTier(plant.DefinitionId, plant.Star);
        }

        private float PlantDamage(Plant plant)
        {
            var baseValue = _content.Plants[plant.DefinitionId].damage
                * UpgradeTier(plant).damageMultiplier;
            return GetEffectiveAttribute(plant, CombatAttributeKind.Damage, baseValue);
        }

        private float PlantRange(Plant plant)
        {
            var baseValue = Map.FromLegacyDistance(_content.Plants[plant.DefinitionId].rangeLegacyUnits)
                * UpgradeTier(plant).rangeMultiplier;
            return GetEffectiveAttribute(plant, CombatAttributeKind.Range, baseValue);
        }

        private Vector2 PlantPoint(Plant plant)
        {
            return plant == null ? DefaultBattleAnchor() : PotPoint(PotById(plant.PotId));
        }

    }
}
