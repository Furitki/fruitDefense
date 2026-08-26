using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        private static BattleSnapshotRestoreResult RestoreCurrentCombatRuntime(
            BattleSnapshotCombatRuntime runtime, CompiledBattleContentCatalog content,
            GameState next)
        {
            if (runtime.nextCombatEventSequence <= 0)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidIdentity,
                    "combatRuntime.nextCombatEventSequence",
                    "Next combat event sequence must be positive.");
            var entities = next.Plants.Cast<CombatEntityState>().Concat(next.Zombies)
                .ToDictionary(value => value.Id);
            if (runtime.entities.Length != entities.Count)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                    "combatRuntime.entities", "Runtime sidecars must cover every combat entity.");
            var seenEntities = new HashSet<int>();
            var seenSequences = new HashSet<int>();
            var maxStatusSequence = 0;
            foreach (var sidecar in runtime.entities.OrderBy(value =>
                value == null ? 0 : value.entityId))
            {
                if (sidecar == null || !seenEntities.Add(sidecar.entityId)
                    || !entities.ContainsKey(sidecar.entityId))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                        "combatRuntime.entities", "Runtime entity identity is missing, duplicate, or unknown.");
                if (sidecar.abilities == null || sidecar.statuses == null)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidPayload,
                        "combatRuntime.entities[" + sidecar.entityId + "]",
                        "Ability and status collections are required.");
                var entity = entities[sidecar.entityId];
                var allowed = ResolveCandidateAbilities(entity, content);
                if (sidecar.abilities.Length != allowed.Count)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                        "combatRuntime.entities[" + sidecar.entityId + "].abilities",
                        "Ability runtimes must cover the resolved loadout exactly once.");
                var abilities = new Dictionary<string, BattleSnapshotAbilityRuntime>(
                    StringComparer.Ordinal);
                foreach (var ability in sidecar.abilities)
                {
                    if (ability == null || string.IsNullOrEmpty(ability.definitionId)
                        || allowed.All(item => item.Id != ability.definitionId)
                        || abilities.ContainsKey(ability.definitionId))
                        return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnknownDefinition,
                            "combatRuntime.entities[" + sidecar.entityId + "].abilities",
                            "Ability runtime identity is missing, duplicate, or unavailable.");
                    if (ability.phase < (int)AbilityRuntimePhase.Idle
                        || ability.phase > (int)AbilityRuntimePhase.Recovery
                        || ability.cooldownTicks < 0 || ability.periodicProgressTicks < 0
                        || ability.windupTicksRemaining < 0 || ability.recoveryTicksRemaining < 0
                        || ability.burstShotsRemaining < 0 || ability.burstIntervalTicks < 0
                        || ability.pendingSourceEntityId < 0 || ability.pendingTargetEntityId < 0
                        || !FiniteNonNegative(ability.pendingEventMagnitude)
                        || ability.pendingRootEventSequence < 0
                        || ability.pendingRootEventSequence >= runtime.nextCombatEventSequence
                        || ability.lastRootEventSequence < 0
                        || ability.lastRootEventSequence >= runtime.nextCombatEventSequence)
                        return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                            "combatRuntime.entities[" + sidecar.entityId + "].abilities",
                            "Ability runtime values are invalid.");
                    var abilityPath = "combatRuntime.entities[" + sidecar.entityId
                        + "].abilities[" + ability.definitionId + "]";
                    if (ability.pendingSourceEntityId > 0
                        && !entities.ContainsKey(ability.pendingSourceEntityId))
                        return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                            abilityPath + ".pendingSourceEntityId",
                            "Pending source must be zero or a live combat entity.");
                    if (ability.pendingTargetEntityId > 0
                        && !entities.ContainsKey(ability.pendingTargetEntityId))
                        return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                            abilityPath + ".pendingTargetEntityId",
                            "Pending target must be zero or a live combat entity.");
                    var phase = (AbilityRuntimePhase)ability.phase;
                    if ((phase == AbilityRuntimePhase.Windup
                            && (ability.windupTicksRemaining <= 0 || ability.burstShotsRemaining <= 0))
                        || (phase == AbilityRuntimePhase.Recovery
                            && (ability.recoveryTicksRemaining <= 0
                                || ability.burstShotsRemaining != 0))
                        || (phase == AbilityRuntimePhase.Idle
                            && (ability.windupTicksRemaining != 0
                                || ability.recoveryTicksRemaining != 0)))
                        return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                            "combatRuntime.entities[" + sidecar.entityId + "].abilities",
                            "Ability phase does not match its timeline counters.");
                    abilities.Add(ability.definitionId, ability);
                }
                foreach (var definition in allowed)
                {
                    var ability = abilities[definition.Id];
                    entity.AbilityRuntimes.Add(new AbilityRuntimeState
                    {
                        AbilityId = ability.definitionId,
                        Phase = (AbilityRuntimePhase)ability.phase,
                        CooldownTicks = ability.cooldownTicks,
                        PeriodicProgressTicks = ability.periodicProgressTicks,
                        WindupTicksRemaining = ability.windupTicksRemaining,
                        RecoveryTicksRemaining = ability.recoveryTicksRemaining,
                        BurstShotsRemaining = ability.burstShotsRemaining,
                        BurstIntervalTicks = ability.burstIntervalTicks,
                        PendingSourceEntityId = ability.pendingSourceEntityId,
                        PendingTargetEntityId = ability.pendingTargetEntityId,
                        PendingEventMagnitude = ability.pendingEventMagnitude,
                        PendingRootEventSequence = ability.pendingRootEventSequence,
                        LastRootEventSequence = ability.lastRootEventSequence,
                    });
                }
                foreach (var status in sidecar.statuses.OrderBy(value =>
                    value == null ? 0 : value.sequence))
                {
                    CompiledStatusDefinition definition;
                    if (status == null || string.IsNullOrEmpty(status.definitionId)
                        || !content.RuntimeStatuses.TryGetValue(status.definitionId, out definition))
                        return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnknownDefinition,
                            "combatRuntime.entities[" + sidecar.entityId + "].statuses",
                            "Status definition is unavailable.");
                    if (status.sourceEntityId != 0 && !entities.ContainsKey(status.sourceEntityId))
                        return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidReference,
                            "combatRuntime.entities[" + sidecar.entityId + "].statuses.sourceEntityId",
                            "Status source entity is unavailable.");
                    if (status.remainingTicks <= 0 || status.stackCount <= 0
                        || status.stackCount > definition.MaxStacks
                        || !FiniteNonNegative(status.magnitude) || status.sequence <= 0
                        || status.tickProgress < 0
                        || (definition.TickIntervalTicks > 0
                            && status.tickProgress >= definition.TickIntervalTicks)
                        || (definition.Stacking == BattleStatusStackingKind.ProcAfterHits
                            && status.stackCount >= definition.HitsToProc))
                        return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                            "combatRuntime.entities[" + sidecar.entityId + "].statuses",
                            "Status runtime values are invalid.");
                    if (!seenSequences.Add(status.sequence))
                        return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidIdentity,
                            "combatRuntime.entities[" + sidecar.entityId + "].statuses.sequence",
                            "Status sequences must be globally unique.");
                    maxStatusSequence = Math.Max(maxStatusSequence, status.sequence);
                    entity.Statuses.Add(new StatusInstance
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
            }
            if (next.NextStatusSequence <= maxStatusSequence || next.NextStatusSequence <= 0)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidIdentity,
                    "nextStatusSequence", "Next status sequence must exceed all live statuses.");
            next.NextCombatEventSequence = runtime.nextCombatEventSequence;
            return BattleSnapshotRestoreResult.Ok();
        }
    }
}
