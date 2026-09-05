using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        private IReadOnlyList<CompiledAbilityDefinition> ResolveAbilities(CombatEntityState entity)
        {
            var plant = entity as Plant;
            if (plant != null)
                return _content.ResolvePlantAbilities(plant.DefinitionId, plant.EquipmentId);
            var zombie = entity as Zombie;
            if (zombie != null) return _content.ResolveEnemyAbilities(zombie.DefinitionId);
            throw new InvalidOperationException("Unsupported combat entity type '" + entity.GetType().Name + "'.");
        }

        private static bool CanAcceptActivation(AbilityRuntimeState runtime)
        {
            return runtime.Phase == AbilityRuntimePhase.Idle
                && runtime.WindupTicksRemaining <= 0
                && runtime.RecoveryTicksRemaining <= 0
                && runtime.BurstShotsRemaining <= 0;
        }

        private static bool CanActivateWithoutTarget(CompiledAbilityDefinition ability)
        {
            return ability.Deliveries.All(value => value.Target == AbilityTargetKind.Self);
        }

        private void EnsureAbilityRuntimes(CombatEntityState entity,
            IReadOnlyList<CompiledAbilityDefinition> abilities)
        {
            var matches = entity.AbilityRuntimes.Count == abilities.Count;
            if (matches)
                for (var index = 0; index < abilities.Count; index++)
                    if (!string.Equals(entity.AbilityRuntimes[index].AbilityId,
                            abilities[index].Id, StringComparison.Ordinal))
                    {
                        matches = false;
                        break;
                    }
            if (matches) return;
            entity.AbilityRuntimes.Clear();
            foreach (var ability in abilities)
                entity.AbilityRuntimes.Add(new AbilityRuntimeState { AbilityId = ability.Id });
        }

        private void AdvanceCombatRuntime()
        {
            foreach (var entity in CombatEntities())
            {
                var abilities = ResolveAbilities(entity);
                EnsureAbilityRuntimes(entity, abilities);
                for (var index = 0; index < abilities.Count; index++)
                {
                    var ability = abilities[index];
                    var runtime = entity.AbilityRuntimes[index];
                    if (runtime.CooldownTicks > 0) runtime.CooldownTicks--;
                    if (runtime.Phase == AbilityRuntimePhase.Windup)
                    {
                        if (runtime.WindupTicksRemaining > 0) runtime.WindupTicksRemaining--;
                        if (runtime.WindupTicksRemaining <= 0) ReleaseAbilityShot(entity, ability, runtime);
                    }
                    else if (runtime.Phase == AbilityRuntimePhase.Recovery)
                    {
                        if (runtime.RecoveryTicksRemaining > 0) runtime.RecoveryTicksRemaining--;
                        if (runtime.RecoveryTicksRemaining <= 0) runtime.Phase = AbilityRuntimePhase.Idle;
                    }
                    else if (runtime.BurstShotsRemaining > 0)
                    {
                        if (runtime.BurstIntervalTicks > 0) runtime.BurstIntervalTicks--;
                        if (runtime.BurstIntervalTicks <= 0) ReleaseAbilityShot(entity, ability, runtime);
                    }
                }

                foreach (var status in entity.Statuses.OrderBy(value => value.Sequence).ToArray())
                {
                    CompiledStatusDefinition definition;
                    if (!_content.RuntimeStatuses.TryGetValue(status.DefinitionId, out definition))
                    {
                        entity.Statuses.Remove(status);
                        continue;
                    }
                    if (definition.PeriodicEffect == CombatPeriodicEffectKind.Damage && entity is Zombie)
                    {
                        status.TickProgress++;
                        var intervalTicks = Math.Max(1, definition.TickIntervalTicks);
                        while (status.TickProgress >= intervalTicks && entity.IsAlive)
                        {
                            status.TickProgress -= intervalTicks;
                            DealPeriodicDamage((Zombie)entity, status,
                                status.Magnitude * BattleAbilityTiming.TicksToSeconds(intervalTicks));
                        }
                    }
                    if (definition.Stacking != BattleStatusStackingKind.ProcAfterHits)
                        status.RemainingTicks--;
                    if (definition.Stacking != BattleStatusStackingKind.ProcAfterHits
                        && status.RemainingTicks <= 0) entity.Statuses.Remove(status);
                    if (!entity.IsAlive) break;
                }
            }
        }

        private void DealPeriodicDamage(Zombie target, StatusInstance status, float rawDamage)
        {
            if (target == null || target.Hp <= 0f || rawDamage <= 0f) return;
            DamageEntity(EntityById(status.SourceEntityId), target, rawDamage,
                string.Empty, string.Empty, status.DefinitionId, string.Empty,
                status.SourceEntityId, DamageEventPolicy.IndirectPeriodic);
        }

        private void RaiseCombatEvent(CombatEventKind trigger, CombatEntityState eventSource,
            CombatEntityState eventTarget, float eventMagnitude)
        {
            RaiseCombatEvents(trigger, null, eventSource, eventTarget, eventMagnitude);
        }

        private void RaiseCombatEvents(CombatEventKind first, CombatEventKind? second,
            CombatEntityState eventSource, CombatEntityState eventTarget, float eventMagnitude)
        {
            var isRoot = _abilityDispatchDepth == 0;
            if (isRoot)
            {
                _abilityRootEventSequence = State.NextCombatEventSequence++;
                _abilityActivationCount = 0;
                _abilityActivationKeys.Clear();
            }
            _abilityDispatchDepth++;
            try
            {
                foreach (var owner in CombatEntities().Where(entity => IsAbilityOwnerActive(entity)
                             || ReferenceEquals(entity, eventSource) || ReferenceEquals(entity, eventTarget))
                             .OrderBy(entity => entity.Id))
                {
                    var abilities = ResolveAbilities(owner);
                    EnsureAbilityRuntimes(owner, abilities);
                    for (var index = 0; index < abilities.Count; index++)
                    {
                        var ability = abilities[index];
                        if (ability.Activation.Kind != AbilityActivationKind.CombatEvent
                            || (ability.Activation.Event != first
                                && (!second.HasValue || ability.Activation.Event != second.Value))) continue;
                        if (!OwnerRoleMatches(ability.Activation.OwnerRole, owner,
                                eventSource, eventTarget)) continue;
                        var runtime = owner.AbilityRuntimes[index];
                        if (runtime.CooldownTicks > 0 || !CanAcceptActivation(runtime)) continue;
                        var activationKey = _abilityRootEventSequence + "|" + owner.Id + "|" + ability.Id;
                        if (!_abilityActivationKeys.Add(activationKey)) continue;
                        _abilityActivationCount++;
                        if (_abilityActivationCount > MaxAbilityActivationsPerRootEvent)
                            throw new InvalidOperationException("Ability activation budget exceeded for root event "
                                + _abilityRootEventSequence + ". Check authored combat-event loops.");
                        var target = FindActivationTarget(owner, ability, eventSource, eventTarget);
                        if (target == null && !CanActivateWithoutTarget(ability)) continue;
                        runtime.LastRootEventSequence = _abilityRootEventSequence;
                        AcceptAbility(owner, ability, runtime, eventSource, target,
                            eventMagnitude, _abilityRootEventSequence);
                    }
                }
            }
            finally
            {
                _abilityDispatchDepth--;
                if (isRoot)
                {
                    _abilityRootEventSequence = 0;
                    _abilityActivationCount = 0;
                    _abilityActivationKeys.Clear();
                }
            }
        }

        private static bool OwnerRoleMatches(AbilityOwnerRole role, CombatEntityState owner,
            CombatEntityState eventSource, CombatEntityState eventTarget)
        {
            if (role == AbilityOwnerRole.Any) return true;
            if (role == AbilityOwnerRole.EventSource) return ReferenceEquals(owner, eventSource);
            return ReferenceEquals(owner, eventTarget);
        }

        private static bool IsAbilityOwnerActive(CombatEntityState entity)
        {
            var plant = entity as Plant;
            return plant != null ? plant.PotId >= 0 : entity.IsAlive;
        }

        private CombatEntityState FindActivationTarget(CombatEntityState owner,
            CompiledAbilityDefinition ability, CombatEntityState eventSource,
            CombatEntityState eventTarget)
        {
            var origin = EntityPoint(owner);
            var range = owner is Plant ? PlantRange((Plant)owner) : 0f;
            foreach (var delivery in ability.Deliveries)
            {
                var targets = SelectAbilityTargets(delivery.Target, owner, eventSource,
                    eventTarget, origin, range, delivery.Radius);
                if (targets.Count > 0) return targets[0];
            }
            return null;
        }

        private void AcceptAbility(CombatEntityState owner, CompiledAbilityDefinition ability,
            AbilityRuntimeState runtime, CombatEntityState eventSource,
            CombatEntityState eventTarget, float eventMagnitude, long rootEventSequence)
        {
            runtime.PendingSourceEntityId = eventSource == null ? 0 : eventSource.Id;
            runtime.PendingTargetEntityId = eventTarget == null ? 0 : eventTarget.Id;
            runtime.PendingEventMagnitude = eventMagnitude;
            runtime.PendingRootEventSequence = rootEventSequence;
            runtime.BurstShotsRemaining = ability.BurstCount;
            runtime.BurstIntervalTicks = 0;
            runtime.WindupTicksRemaining = ability.Timeline.WindupTicks;
            runtime.RecoveryTicksRemaining = 0;
            if (ability.Activation.Kind == AbilityActivationKind.Cooldown)
            {
                var seconds = BattleAbilityTiming.TicksToSeconds(ability.Activation.CooldownTicks);
                var plant = owner as Plant;
                runtime.CooldownTicks = Math.Max(1, BattleAbilityTiming.SecondsToTicks(
                    GetEffectiveAttribute(owner, CombatAttributeKind.AttackInterval,
                        seconds, plant == null
                            ? 1f
                            : 1f / UpgradeTier(plant).attackSpeedMultiplier)));
            }
            else runtime.CooldownTicks = ability.Activation.CooldownTicks;
            var position = EntityPoint(owner);
            var direction = DirectionTo(position, eventTarget == null ? null : EntityPoint(eventTarget));
            _presentationEvents.EmitAbilityStarted(State.LogicTick, ability.Id, owner.Id,
                eventTarget == null ? 0 : eventTarget.Id, position, direction,
                EquipmentIdFor(owner));
            if (runtime.WindupTicksRemaining > 0) runtime.Phase = AbilityRuntimePhase.Windup;
            else ReleaseAbilityShot(owner, ability, runtime);
        }

        private void ReleaseAbilityShot(CombatEntityState owner, CompiledAbilityDefinition ability,
            AbilityRuntimeState runtime)
        {
            runtime.Phase = AbilityRuntimePhase.Idle;
            var eventSource = EntityById(runtime.PendingSourceEntityId);
            var eventTarget = EntityById(runtime.PendingTargetEntityId);
            var origin = EntityPoint(owner);
            var range = owner is Plant ? PlantRange((Plant)owner) : 0f;
            var direction = DirectionTo(origin, eventTarget == null ? null : EntityPoint(eventTarget));
            _presentationEvents.EmitAbilityReleased(State.LogicTick, ability.Id, owner.Id,
                eventTarget == null ? 0 : eventTarget.Id, origin, direction,
                EquipmentIdFor(owner));
            var context = new CombatEffectContext(runtime.PendingRootEventSequence, owner,
                eventSource, eventTarget, origin, range, runtime.PendingEventMagnitude);
            for (var index = 0; index < ability.Deliveries.Count; index++)
                ExecuteDelivery(ability, index, context);
            runtime.BurstShotsRemaining = Math.Max(0, runtime.BurstShotsRemaining - 1);
            if (runtime.BurstShotsRemaining > 0)
            {
                runtime.BurstIntervalTicks = Math.Max(1, ability.BurstIntervalTicks);
                return;
            }
            runtime.BurstIntervalTicks = 0;
            runtime.PendingSourceEntityId = 0;
            runtime.PendingTargetEntityId = 0;
            runtime.PendingEventMagnitude = 0f;
            runtime.PendingRootEventSequence = 0L;
            runtime.RecoveryTicksRemaining = ability.Timeline.RecoveryTicks;
            runtime.Phase = runtime.RecoveryTicksRemaining > 0
                ? AbilityRuntimePhase.Recovery : AbilityRuntimePhase.Idle;
        }

        private void ExecuteDelivery(CompiledAbilityDefinition ability, int deliveryIndex,
            CombatEffectContext context)
        {
            var delivery = ability.Deliveries[deliveryIndex];
            var targets = SelectAbilityTargets(delivery.Target, context.Owner,
                context.EventSource, context.EventTarget, context.Origin, context.Range,
                delivery.Radius);
            if (delivery.Kind == AbilityDeliveryKind.Projectile)
            {
                var plant = context.Owner as Plant;
                var target = targets.OfType<Zombie>().FirstOrDefault();
                if (plant != null && target != null)
                    SpawnProjectile(plant, ability, deliveryIndex, context.Origin, target,
                        PlantDamage(plant) * ability.DamageMultiplier, context.Range);
                return;
            }
            ResolvePayload(ability, delivery, context, targets, string.Empty,
                ability.DamageMultiplier * (context.Owner is Plant ? PlantDamage((Plant)context.Owner) : 1f));
        }

        private void ResolvePayload(CompiledAbilityDefinition ability,
            CompiledAbilityDelivery delivery, CombatEffectContext context,
            IReadOnlyList<CombatEntityState> targets, string projectileId, float damageBasis,
            int sourceEntityId = 0, string sourceDefinitionId = "", string sourceEquipmentId = "")
        {
            if (sourceEntityId <= 0 && context.Owner != null) sourceEntityId = context.Owner.Id;
            if (string.IsNullOrEmpty(sourceDefinitionId) && context.Owner != null)
                sourceDefinitionId = context.Owner.DefinitionId;
            if (string.IsNullOrEmpty(sourceEquipmentId))
                sourceEquipmentId = EquipmentIdFor(context.Owner);
            foreach (var effect in delivery.Payload)
            {
                if (effect.Kind == AbilityPayloadEffectKind.Damage)
                {
                    var basis = context.Owner is Plant ? damageBasis
                        : context.EventMagnitude > 0f ? context.EventMagnitude : damageBasis;
                    foreach (var target in targets.OfType<Zombie>().ToArray())
                        DamageEntity(context.Owner, target, basis * effect.Magnitude,
                            ability.Id, projectileId, sourceDefinitionId, sourceEquipmentId,
                            sourceEntityId, DamageEventPolicy.DirectAbilityHit);
                }
                else if (effect.Kind == AbilityPayloadEffectKind.GrantResource)
                {
                    var resource = context.Owner == null ? effect.ResourceAmount
                        : Mathf.RoundToInt(GetEffectiveAttribute(context.Owner,
                            CombatAttributeKind.ResourceGain, effect.ResourceAmount));
                    State.Sun += resource;
                    _presentationEvents.EmitResourceGranted(State.LogicTick, ability.Id,
                        BattleContentIds.Resources.Sun, context.Owner.Id,
                        targets.Count == 0 ? 0 : targets[0].Id, context.Origin, resource,
                        EquipmentIdFor(context.Owner));
                }
                else if (effect.Kind == AbilityPayloadEffectKind.ApplyStatus)
                {
                    foreach (var target in targets.ToArray())
                    {
                        var magnitude = effect.Magnitude;
                        CompiledStatusDefinition statusDefinition;
                        if (_content.RuntimeStatuses.TryGetValue(effect.StatusId, out statusDefinition)
                            && statusDefinition.PeriodicEffect == CombatPeriodicEffectKind.Damage
                            && context.EventMagnitude > 0f) magnitude *= context.EventMagnitude;
                        ApplyStatus(target, effect.StatusId, sourceEntityId, magnitude,
                            ability.Id, sourceEquipmentId);
                    }
                }
                else throw new InvalidOperationException("No payload executor registered for "
                    + effect.Kind + ".");
            }
        }

        private List<CombatEntityState> SelectAbilityTargets(AbilityTargetKind targetKind,
            CombatEntityState owner, CombatEntityState eventSource, CombatEntityState eventTarget,
            Vector2 origin, float range, float radiusLegacyUnits)
        {
            if (targetKind == AbilityTargetKind.Self)
                return new List<CombatEntityState> { owner };
            if (targetKind == AbilityTargetKind.EventSource)
                return eventSource == null || !eventSource.IsAlive ? new List<CombatEntityState>()
                    : new List<CombatEntityState> { eventSource };
            if (targetKind == AbilityTargetKind.EventTarget)
                return eventTarget == null || !eventTarget.IsAlive ? new List<CombatEntityState>()
                    : new List<CombatEntityState> { eventTarget };
            if (targetKind == AbilityTargetKind.FrontmostEnemyInRange
                || targetKind == AbilityTargetKind.LineFromCaster)
            {
                var front = SelectTarget(origin, range);
                return front == null ? new List<CombatEntityState>()
                    : new List<CombatEntityState> { front };
            }
            if (targetKind == AbilityTargetKind.AllEnemiesInRadius)
            {
                var radius = Map.FromLegacyDistance(radiusLegacyUnits);
                return OrderedLivingEnemies().Where(zombie => Vector2.Distance(origin,
                        ZombiePoint(zombie)) <= radius)
                    .Cast<CombatEntityState>().ToList();
            }
            var allies = targetKind == AbilityTargetKind.AllAllies;
            return CombatEntities().Where(entity => IsAbilityOwnerActive(entity)
                    && (entity.Faction == owner.Faction) == allies)
                .OrderBy(entity => entity.Id).ToList();
        }

        private Vector2 EntityPoint(CombatEntityState entity)
        {
            var plant = entity as Plant;
            if (plant != null) return plant.PotId < 0 ? DefaultBattleAnchor() : PlantPoint(plant);
            var zombie = entity as Zombie;
            return zombie == null ? DefaultBattleAnchor() : ZombiePoint(zombie);
        }

        private static string EquipmentIdFor(CombatEntityState entity)
        {
            var plant = entity as Plant;
            return plant == null ? string.Empty : plant.EquipmentId;
        }

        private static Vector2 DirectionTo(Vector2 origin, Vector2? target)
        {
            if (!target.HasValue) return Vector2.zero;
            var direction = target.Value - origin;
            return direction.sqrMagnitude <= .0001f ? Vector2.zero : direction.normalized;
        }

        private IEnumerable<Zombie> OrderedLivingEnemies()
        {
            return State.Zombies.Where(zombie => zombie.Hp > 0f)
                .OrderBy(RemainingRouteDistance)
                .ThenBy(zombie => zombie.Hp)
                .ThenBy(zombie => zombie.Id);
        }

        private float RemainingRouteDistance(Zombie zombie)
        {
            return Mathf.Max(0f, Map.RouteLength(zombie.RouteId) - zombie.PathProgress);
        }

        private Zombie SelectTarget(Vector2 origin, float range)
        {
            return State.Zombies
                .Where(zombie => zombie.Hp > 0f
                    && Vector2.Distance(origin, ZombiePoint(zombie)) <= range)
                .OrderBy(RemainingRouteDistance)
                .ThenBy(zombie => zombie.Hp)
                .ThenBy(zombie => zombie.Id)
                .FirstOrDefault();
        }

        private void SpawnProjectile(Plant plant, CompiledAbilityDefinition ability,
            int deliveryIndex, Vector2 origin, Zombie target, float damageBasis, float range)
        {
            var delivery = ability.Deliveries[deliveryIndex];
            var definition = _content.RuntimeProjectiles[delivery.ProjectileId];
            var targetPoint = ZombiePoint(target);
            var distance = Mathf.Max(.001f, Vector2.Distance(origin, targetPoint));
            var direction = (targetPoint - origin) / distance;
            var totalTicks = definition.Mode == BattleProjectileMode.TimedArc
                ? definition.FlightTicks
                : definition.Mode == BattleProjectileMode.LinearReturn
                    ? BattleAbilityTiming.SecondsToTicks(range * definition.RangeMultiplier * 2f
                        / Map.FromLegacyDistance(definition.Speed) + .3f)
                    : BattleAbilityTiming.SecondsToTicks(3f);
            State.Projectiles.Add(new ProjectileFlash
            {
                Id = State.NextId++,
                SourceEntityId = plant.Id,
                TargetId = definition.Mode == BattleProjectileMode.TimedArc ? -1 : target.Id,
                SourceDefinitionId = plant.DefinitionId,
                SourceEquipmentId = plant.EquipmentId,
                AbilityId = ability.Id,
                DeliveryIndex = deliveryIndex,
                ProjectileId = definition.Id,
                Origin = origin,
                Position = origin,
                TargetPoint = definition.Mode == BattleProjectileMode.LinearReturn ? origin : targetPoint,
                Direction = direction,
                MaxDistance = definition.Mode == BattleProjectileMode.LinearReturn ? range * definition.RangeMultiplier : distance,
                DamageBasis = damageBasis,
                TicksRemaining = totalTicks,
                FlightTicks = definition.FlightTicks,
            });
            _presentationEvents.EmitProjectileLaunched(State.LogicTick, ability.Id,
                definition.Id, plant.Id, target.Id, origin, direction, plant.EquipmentId);
        }

        private void AdvanceProjectiles(float delta)
        {
            for (var index = State.Projectiles.Count - 1; index >= 0; index--)
            {
                var projectile = State.Projectiles[index];
                var definition = _content.RuntimeProjectiles[projectile.ProjectileId];
                projectile.TicksRemaining--;
                if (definition.Mode == BattleProjectileMode.Tracking)
                {
                    var target = State.Zombies.FirstOrDefault(zombie => zombie.Id == projectile.TargetId && zombie.Hp > 0f)
                        ?? State.Zombies.Where(zombie => zombie.Hp > 0f)
                            .OrderBy(zombie => Vector2.Distance(projectile.Position,
                                ZombiePoint(zombie)))
                            .ThenBy(RemainingRouteDistance)
                            .ThenBy(zombie => zombie.Id)
                            .FirstOrDefault();
                    var targetPoint = target == null ? projectile.TargetPoint : ZombiePoint(target);
                    var gap = Vector2.Distance(projectile.Position, targetPoint);
                    var travel = Map.FromLegacyDistance(definition.Speed) * delta;
                    if (gap <= travel + Map.FromLegacyDistance(definition.HitRadius))
                    {
                        if (target != null) ResolveProjectileImpact(projectile,
                            new CombatEntityState[] { target }, targetPoint);
                        State.Projectiles.RemoveAt(index);
                        continue;
                    }
                    if (gap > .001f) projectile.Position += (targetPoint - projectile.Position) * (travel / gap);
                    projectile.TargetId = target == null ? -1 : target.Id;
                    projectile.TargetPoint = targetPoint;
                }
                else if (definition.Mode == BattleProjectileMode.TimedArc)
                {
                    projectile.Progress = Mathf.Clamp01((float)(projectile.FlightTicks - projectile.TicksRemaining)
                        / Mathf.Max(1, projectile.FlightTicks));
                    projectile.Position = SampleWatermelonArc(projectile.Origin, projectile.TargetPoint, projectile.Progress);
                    if (projectile.Progress >= 1f || projectile.TicksRemaining <= 0)
                    {
                        var ability = ResolveProjectileAbility(projectile);
                        var delivery = ability.Deliveries[projectile.DeliveryIndex];
                        var radius = Map.FromLegacyDistance(delivery.Radius);
                        var targets = OrderedLivingEnemies().Where(zombie => radius <= 0f
                                ? Vector2.Distance(projectile.TargetPoint,
                                    ZombiePoint(zombie)) <= Map.FromLegacyDistance(definition.HitRadius)
                                : Vector2.Distance(projectile.TargetPoint,
                                    ZombiePoint(zombie)) <= radius)
                            .Cast<CombatEntityState>().ToArray();
                        ResolveProjectileImpact(projectile, targets, projectile.TargetPoint);
                        State.Projectiles.RemoveAt(index);
                        continue;
                    }
                }
                else
                {
                    var previous = projectile.Position;
                    var speed = Map.FromLegacyDistance(definition.Speed);
                    if (projectile.Returning)
                        projectile.Progress = Mathf.Max(0f, projectile.Progress - speed * delta);
                    else
                    {
                        projectile.Progress = Mathf.Min(projectile.MaxDistance, projectile.Progress + speed * delta);
                        if (projectile.Progress >= projectile.MaxDistance) projectile.Returning = true;
                    }
                    projectile.Position = projectile.Origin + projectile.Direction * projectile.Progress;
                    foreach (var zombie in State.Zombies.ToArray())
                    {
                        if (zombie.Hp <= 0f) continue;
                        var hitCount = projectile.HitIds.Count(id => id == zombie.Id);
                        var canHit = projectile.Returning ? hitCount < definition.MaxHitsPerTarget : hitCount == 0;
                        if (!canHit || PointToSegmentDistance(ZombiePoint(zombie), previous,
                                projectile.Position)
                            > Map.FromLegacyDistance(definition.HitRadius)) continue;
                        ResolveProjectileImpact(projectile,
                            new CombatEntityState[] { zombie }, ZombiePoint(zombie));
                        if (projectile.Returning)
                            while (projectile.HitIds.Count(id => id == zombie.Id) < definition.MaxHitsPerTarget)
                                projectile.HitIds.Add(zombie.Id);
                        else projectile.HitIds.Add(zombie.Id);
                    }
                    if (projectile.Returning && projectile.Progress <= 0f)
                    {
                        State.Projectiles.RemoveAt(index);
                        continue;
                    }
                }
                if (projectile.TicksRemaining <= 0) State.Projectiles.RemoveAt(index);
            }
        }

        private CompiledAbilityDefinition ResolveProjectileAbility(ProjectileFlash projectile)
        {
            IReadOnlyList<CompiledAbilityDefinition> loadout;
            if (!string.IsNullOrEmpty(projectile.SourceDefinitionId)
                && _content.Plants.ContainsKey(projectile.SourceDefinitionId))
                loadout = _content.ResolvePlantAbilities(projectile.SourceDefinitionId,
                    projectile.SourceEquipmentId);
            else return _content.RuntimeAbilities[projectile.AbilityId];
            for (var index = 0; index < loadout.Count; index++)
                if (string.Equals(loadout[index].Id, projectile.AbilityId,
                        StringComparison.Ordinal)) return loadout[index];
            throw new InvalidOperationException("Projectile Ability '" + projectile.AbilityId
                + "' is not present in its resolved source loadout.");
        }

        private void ResolveProjectileImpact(ProjectileFlash projectile,
            IReadOnlyList<CombatEntityState> targets, Vector2 impactPoint)
        {
            var ability = ResolveProjectileAbility(projectile);
            var delivery = ability.Deliveries[projectile.DeliveryIndex];
            var source = EntityById(projectile.SourceEntityId);
            var eventTarget = targets.Count == 0 ? null : targets[0];
            var context = new CombatEffectContext(0L, source, source, eventTarget,
                impactPoint, 0f, 0f);
            ResolvePayload(ability, delivery, context, targets, projectile.ProjectileId,
                projectile.DamageBasis, projectile.SourceEntityId,
                projectile.SourceDefinitionId, projectile.SourceEquipmentId);
        }

        private Vector2 SampleWatermelonArc(Vector2 origin, Vector2 target, float progress)
        {
            var ratio = Mathf.Clamp01(progress);
            var arcHeight = Mathf.Min(Map.FromLegacyDistance(18f),
                Mathf.Max(Map.FromLegacyDistance(8f), Vector2.Distance(origin, target) * .35f));
            return Vector2.Lerp(origin, target, ratio) + Vector2.up * (-arcHeight * 4f * ratio * (1f - ratio));
        }

        private static float PointToSegmentDistance(Vector2 point, Vector2 from, Vector2 to)
        {
            var segment = to - from;
            if (segment.sqrMagnitude <= .0001f) return Vector2.Distance(point, from);
            var ratio = Mathf.Clamp01(Vector2.Dot(point - from, segment) / segment.sqrMagnitude);
            return Vector2.Distance(point, from + segment * ratio);
        }

        private void DamageEntity(CombatEntityState source, Zombie zombie, float rawDamage,
            string abilityId, string projectileId, string sourceDefinitionId,
            string sourceEquipmentId, int sourceEntityId, DamageEventPolicy eventPolicy)
        {
            if (zombie == null || zombie.Hp <= 0f) return;
            if (sourceEntityId <= 0 && source != null) sourceEntityId = source.Id;
            if (string.IsNullOrEmpty(sourceDefinitionId) && source != null)
                sourceDefinitionId = source.DefinitionId;
            if (string.IsNullOrEmpty(sourceEquipmentId))
                sourceEquipmentId = EquipmentIdFor(source);
            var damage = GetEffectiveAttribute(zombie, CombatAttributeKind.DamageTaken, rawDamage);
            zombie.Hp = Mathf.Max(0f, zombie.Hp - damage);
            var impactPoint = ZombiePoint(zombie);
            var sourcePoint = source == null ? impactPoint : EntityPoint(source);
            var direction = DirectionTo(sourcePoint, impactPoint);
            var defeated = zombie.Hp <= 0f;
            _presentationEvents.EmitDamageResolved(State.LogicTick, abilityId, projectileId,
                sourceDefinitionId, zombie.DefinitionId, sourceEntityId, zombie.Id,
                impactPoint, direction, damage, defeated, sourceEquipmentId);
            if (eventPolicy == DamageEventPolicy.DirectAbilityHit)
                RaiseCombatEvents(CombatEventKind.AfterDamageDealt,
                    CombatEventKind.AfterDamageTaken, source, zombie, damage);
            else RaiseCombatEvent(CombatEventKind.AfterDamageTaken, source, zombie, damage);
            if (defeated)
            {
                var index = State.Zombies.IndexOf(zombie);
                if (index >= 0) KillZombie(index, zombie, source, abilityId,
                    sourceEquipmentId, sourceEntityId);
            }
        }

        public void ApplyStatus(CombatEntityState target, string statusId, int sourceEntityId,
            float magnitudeMultiplier = 1f, string abilityId = "",
            string sourceEquipmentId = "")
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var definition = _content.RuntimeStatuses[statusId];
            var magnitude = definition.PeriodicEffect == CombatPeriodicEffectKind.Damage
                ? magnitudeMultiplier
                : definition.Magnitude * magnitudeMultiplier;
            var procStatusId = string.Empty;
            if (definition.Stacking == BattleStatusStackingKind.Refresh)
            {
                var existing = target.Statuses.FirstOrDefault(value => value.DefinitionId == statusId);
                if (existing == null)
                {
                    target.Statuses.Add(CreateStatus(definition, sourceEntityId, magnitude));
                }
                else
                {
                    existing.RemainingTicks = Math.Max(existing.RemainingTicks, definition.DurationTicks);
                    existing.Magnitude = magnitude;
                    existing.SourceEntityId = sourceEntityId;
                }
            }
            else if (definition.Stacking == BattleStatusStackingKind.Independent)
            {
                target.Statuses.Add(CreateStatus(definition, sourceEntityId, magnitude));
                var same = target.Statuses.Where(value => value.DefinitionId == statusId)
                    .OrderBy(value => value.Sequence).ToList();
                while (same.Count > definition.MaxStacks)
                {
                    target.Statuses.Remove(same[0]);
                    same.RemoveAt(0);
                }
            }
            else if (definition.Stacking == BattleStatusStackingKind.Additive)
            {
                var existing = target.Statuses.FirstOrDefault(value => value.DefinitionId == statusId);
                if (existing == null) target.Statuses.Add(CreateStatus(definition, sourceEntityId, magnitude));
                else
                {
                    existing.StackCount = Math.Min(definition.MaxStacks, existing.StackCount + 1);
                    existing.RemainingTicks = Math.Max(existing.RemainingTicks, definition.DurationTicks);
                    existing.Magnitude = magnitude;
                    existing.SourceEntityId = sourceEntityId;
                }
            }
            else
            {
                var counter = target.Statuses.FirstOrDefault(value => value.DefinitionId == statusId);
                if (counter == null)
                {
                    counter = CreateStatus(definition, sourceEntityId, magnitude);
                    counter.StackCount = 0;
                    target.Statuses.Add(counter);
                }
                counter.StackCount++;
                counter.SourceEntityId = sourceEntityId;
                if (counter.StackCount >= definition.HitsToProc)
                {
                    target.Statuses.Remove(counter);
                    procStatusId = definition.ProcStatusId;
                }
            }
            var source = EntityById(sourceEntityId);
            if (string.IsNullOrEmpty(sourceEquipmentId)) sourceEquipmentId = EquipmentIdFor(source);
            var targetPoint = EntityPoint(target);
            var sourcePoint = source == null ? targetPoint : EntityPoint(source);
            var count = target.Statuses.Where(value => value.DefinitionId == statusId)
                .Sum(value => Math.Max(1, value.StackCount));
            _presentationEvents.EmitStatusApplied(State.LogicTick, abilityId, statusId,
                sourceEntityId, target.Id, targetPoint, DirectionTo(sourcePoint, targetPoint),
                magnitude, Math.Max(1, count), sourceEquipmentId);
            RaiseCombatEvent(CombatEventKind.StatusApplied, source, target, magnitude);
            if (!string.IsNullOrEmpty(procStatusId))
            {
                _presentationEvents.EmitStatusProcced(State.LogicTick, abilityId, procStatusId,
                    sourceEntityId, target.Id, targetPoint, DirectionTo(sourcePoint, targetPoint),
                    1f, 1, sourceEquipmentId);
                ApplyStatus(target, procStatusId, sourceEntityId, 1f, abilityId,
                    sourceEquipmentId);
            }
        }

        private StatusInstance CreateStatus(CompiledStatusDefinition definition, int sourceEntityId, float magnitude)
        {
            return new StatusInstance
            {
                DefinitionId = definition.Id,
                SourceEntityId = sourceEntityId,
                RemainingTicks = definition.DurationTicks,
                StackCount = 1,
                Magnitude = magnitude,
                Sequence = State.NextStatusSequence++,
            };
        }

        private void KillZombie(int index, Zombie zombie, CombatEntityState killer = null,
            string abilityId = "", string sourceEquipmentId = "", int sourceEntityId = 0)
        {
            if (sourceEntityId <= 0 && killer != null) sourceEntityId = killer.Id;
            RaiseCombatEvent(CombatEventKind.EntityDefeated, killer, zombie, 0f);
            var reward = Mode == BattleSimulationMode.Standard ? zombie.Reward : 0;
            State.Sun += reward;
            var point = ZombiePoint(zombie);
            var sourcePoint = killer == null ? point : EntityPoint(killer);
            _presentationEvents.EmitEntityDefeated(State.LogicTick, abilityId,
                zombie.DefinitionId, sourceEntityId, zombie.Id, point,
                DirectionTo(sourcePoint, point), reward, sourceEquipmentId);
            State.Zombies.RemoveAt(index);
        }

    }
}
