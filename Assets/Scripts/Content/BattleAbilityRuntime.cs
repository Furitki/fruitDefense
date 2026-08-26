using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FruitDefense.Content
{
    public enum AbilityActivationKind { Cooldown, Periodic, CombatEvent }
    public enum CombatEventKind
    {
        BattleStarted,
        WaveFirstSpawned,
        AfterDamageDealt,
        AfterDamageTaken,
        StatusApplied,
        EntityDefeated,
    }
    public enum AbilityOwnerRole { Any, EventSource, EventTarget }
    public enum AbilityTargetKind
    {
        Self,
        EventSource,
        EventTarget,
        FrontmostEnemyInRange,
        AllAllies,
        AllEnemies,
        AllEnemiesInRadius,
        LineFromCaster,
    }
    public enum AbilityDeliveryKind { Instant, Projectile }
    public enum AbilityPayloadEffectKind { Damage, GrantResource, ApplyStatus }
    public enum AbilityModifierAttributeKind
    {
        DamageMultiplier,
        Cooldown,
        Period,
        BurstCount,
        BurstInterval,
        ResourceAmount,
    }
    public enum AbilityModifierOperationKind { Add, Multiply, Override }
    public enum BattleProjectileMode { Tracking, TimedArc, LinearReturn }
    public enum BattleStatusKind { Modifier, Slow, Stun, Freeze, HitCount, Burn }
    public enum BattleStatusStackingKind { Refresh, Independent, ProcAfterHits, Additive }

    public static class BattleAbilityTiming
    {
        public const float FixedStepSeconds = .05f;

        public static int SecondsToTicks(float seconds)
        {
            if (seconds <= 0f) return 0;
            return Math.Max(1, (int)Math.Ceiling(seconds / FixedStepSeconds - .000001d));
        }

        public static float TicksToSeconds(int ticks)
        {
            return Math.Max(0, ticks) * FixedStepSeconds;
        }
    }

    public sealed class CompiledAbilityActivation
    {
        public AbilityActivationKind Kind { get; internal set; }
        public CombatEventKind Event { get; internal set; }
        public AbilityOwnerRole OwnerRole { get; internal set; }
        public int Priority { get; internal set; }
        public int CooldownTicks { get; internal set; }
        public int PeriodTicks { get; internal set; }
    }

    public sealed class CompiledAbilityTimeline
    {
        public int WindupTicks { get; internal set; }
        public int RecoveryTicks { get; internal set; }
    }

    public sealed class CompiledAbilityPayloadEffect
    {
        public AbilityPayloadEffectKind Kind { get; internal set; }
        public string StatusId { get; internal set; }
        public float Magnitude { get; internal set; }
        public int ResourceAmount { get; internal set; }

        internal CompiledAbilityPayloadEffect Clone()
        {
            return new CompiledAbilityPayloadEffect
            {
                Kind = Kind,
                StatusId = StatusId,
                Magnitude = Magnitude,
                ResourceAmount = ResourceAmount,
            };
        }
    }

    public sealed class CompiledAbilityDelivery
    {
        public AbilityTargetKind Target { get; internal set; }
        public AbilityDeliveryKind Kind { get; internal set; }
        public string ProjectileId { get; internal set; }
        public float Radius { get; internal set; }
        public IReadOnlyList<CompiledAbilityPayloadEffect> Payload { get; internal set; }

        internal CompiledAbilityDelivery Clone()
        {
            return new CompiledAbilityDelivery
            {
                Target = Target,
                Kind = Kind,
                ProjectileId = ProjectileId,
                Radius = Radius,
                Payload = Array.AsReadOnly(Payload.Select(value => value.Clone()).ToArray()),
            };
        }
    }

    public sealed class CompiledAbilityDefinition
    {
        public string Id { get; internal set; }
        public CompiledAbilityActivation Activation { get; internal set; }
        public CompiledAbilityTimeline Timeline { get; internal set; }
        public float DamageMultiplier { get; internal set; }
        public int BurstCount { get; internal set; }
        public int BurstIntervalTicks { get; internal set; }
        public IReadOnlyList<string> Tags { get; internal set; }
        public IReadOnlyList<CompiledAbilityDelivery> Deliveries { get; internal set; }

        internal CompiledAbilityDefinition Clone()
        {
            return new CompiledAbilityDefinition
            {
                Id = Id,
                Activation = new CompiledAbilityActivation
                {
                    Kind = Activation.Kind,
                    Event = Activation.Event,
                    OwnerRole = Activation.OwnerRole,
                    Priority = Activation.Priority,
                    CooldownTicks = Activation.CooldownTicks,
                    PeriodTicks = Activation.PeriodTicks,
                },
                Timeline = new CompiledAbilityTimeline
                {
                    WindupTicks = Timeline.WindupTicks,
                    RecoveryTicks = Timeline.RecoveryTicks,
                },
                DamageMultiplier = DamageMultiplier,
                BurstCount = BurstCount,
                BurstIntervalTicks = BurstIntervalTicks,
                Tags = Tags,
                Deliveries = Array.AsReadOnly(Deliveries.Select(value => value.Clone()).ToArray()),
            };
        }
    }

    public sealed class CompiledAbilityModifier
    {
        public string Id { get; internal set; }
        public string RequiredPlantTag { get; internal set; }
        public string TargetAbilityId { get; internal set; }
        public string TargetAbilityTag { get; internal set; }
        public bool AllowMultipleMatches { get; internal set; }
        public AbilityModifierAttributeKind Attribute { get; internal set; }
        public AbilityModifierOperationKind Operation { get; internal set; }
        public float Value { get; internal set; }
    }

    internal static class CompiledAbilityModifierApplicator
    {
        public static void Apply(CompiledAbilityDefinition ability, CompiledAbilityModifier modifier)
        {
            switch (modifier.Attribute)
            {
                case AbilityModifierAttributeKind.DamageMultiplier:
                    ability.DamageMultiplier = ApplyNumber(ability.DamageMultiplier, modifier);
                    break;
                case AbilityModifierAttributeKind.Cooldown:
                    ability.Activation.CooldownTicks = BattleAbilityTiming.SecondsToTicks(ApplyNumber(
                        BattleAbilityTiming.TicksToSeconds(ability.Activation.CooldownTicks), modifier));
                    break;
                case AbilityModifierAttributeKind.Period:
                    ability.Activation.PeriodTicks = BattleAbilityTiming.SecondsToTicks(ApplyNumber(
                        BattleAbilityTiming.TicksToSeconds(ability.Activation.PeriodTicks), modifier));
                    break;
                case AbilityModifierAttributeKind.BurstCount:
                    ability.BurstCount = Math.Max(1,
                        (int)Math.Round(ApplyNumber(ability.BurstCount, modifier)));
                    break;
                case AbilityModifierAttributeKind.BurstInterval:
                    ability.BurstIntervalTicks = BattleAbilityTiming.SecondsToTicks(ApplyNumber(
                        BattleAbilityTiming.TicksToSeconds(ability.BurstIntervalTicks), modifier));
                    break;
                case AbilityModifierAttributeKind.ResourceAmount:
                    foreach (var effect in ability.Deliveries.SelectMany(value => value.Payload)
                                 .Where(value => value.Kind == AbilityPayloadEffectKind.GrantResource))
                        effect.ResourceAmount = Math.Max(0,
                            (int)Math.Round(ApplyNumber(effect.ResourceAmount, modifier)));
                    break;
                default:
                    throw new InvalidOperationException("No modifier executor registered for "
                        + modifier.Attribute + ".");
            }
        }

        private static float ApplyNumber(float current, CompiledAbilityModifier modifier)
        {
            if (modifier.Operation == AbilityModifierOperationKind.Add) return current + modifier.Value;
            if (modifier.Operation == AbilityModifierOperationKind.Multiply) return current * modifier.Value;
            return modifier.Value;
        }
    }

    public sealed class CompiledProjectileDefinition
    {
        public string Id { get; internal set; }
        public BattleProjectileMode Mode { get; internal set; }
        public float Speed { get; internal set; }
        public int FlightTicks { get; internal set; }
        public float RangeMultiplier { get; internal set; }
        public float HitRadius { get; internal set; }
        public int MaxHitsPerTarget { get; internal set; }
    }

    public sealed class CompiledStatusDefinition
    {
        public string Id { get; internal set; }
        public BattleStatusKind Kind { get; internal set; }
        public BattleStatusStackingKind Stacking { get; internal set; }
        public int DurationTicks { get; internal set; }
        public int TickIntervalTicks { get; internal set; }
        public float Magnitude { get; internal set; }
        public int MaxStacks { get; internal set; }
        public int HitsToProc { get; internal set; }
        public string ProcStatusId { get; internal set; }
        public CombatStatusPolarity Polarity { get; internal set; }
        public IReadOnlyList<string> Tags { get; internal set; }
        public bool BlocksMovement { get; internal set; }
        public IReadOnlyList<CompiledStatusModifier> Modifiers { get; internal set; }
        public CombatPeriodicEffectKind PeriodicEffect { get; internal set; }
    }

    internal static class BattleAbilityCompiler
    {
        private static readonly IReadOnlyDictionary<string, AbilityActivationKind> Activations = ReadOnly(
            new Dictionary<string, AbilityActivationKind>(StringComparer.Ordinal)
            {
                { "activation.cooldown", AbilityActivationKind.Cooldown },
                { "activation.periodic", AbilityActivationKind.Periodic },
                { "activation.combat-event", AbilityActivationKind.CombatEvent },
            });

        private static readonly IReadOnlyDictionary<string, CombatEventKind> Events = ReadOnly(
            new Dictionary<string, CombatEventKind>(StringComparer.Ordinal)
            {
                { "event.battle-started", CombatEventKind.BattleStarted },
                { "event.wave-first-spawned", CombatEventKind.WaveFirstSpawned },
                { "event.after-damage-dealt", CombatEventKind.AfterDamageDealt },
                { "event.after-damage-taken", CombatEventKind.AfterDamageTaken },
                { "event.status-applied", CombatEventKind.StatusApplied },
                { "event.entity-defeated", CombatEventKind.EntityDefeated },
            });

        private static readonly IReadOnlyDictionary<string, AbilityOwnerRole> OwnerRoles = ReadOnly(
            new Dictionary<string, AbilityOwnerRole>(StringComparer.Ordinal)
            {
                { "owner.any", AbilityOwnerRole.Any },
                { "owner.event-source", AbilityOwnerRole.EventSource },
                { "owner.event-target", AbilityOwnerRole.EventTarget },
            });

        private static readonly IReadOnlyDictionary<string, AbilityTargetKind> Targets = ReadOnly(
            new Dictionary<string, AbilityTargetKind>(StringComparer.Ordinal)
            {
                { "target.self", AbilityTargetKind.Self },
                { "target.event-source", AbilityTargetKind.EventSource },
                { "target.event-target", AbilityTargetKind.EventTarget },
                { "target.front", AbilityTargetKind.FrontmostEnemyInRange },
                { "target.all-allies", AbilityTargetKind.AllAllies },
                { "target.all-enemies", AbilityTargetKind.AllEnemies },
                { "target.area", AbilityTargetKind.AllEnemiesInRadius },
                { "target.line", AbilityTargetKind.LineFromCaster },
            });

        private static readonly IReadOnlyDictionary<string, AbilityDeliveryKind> Deliveries = ReadOnly(
            new Dictionary<string, AbilityDeliveryKind>(StringComparer.Ordinal)
            {
                { "delivery.instant", AbilityDeliveryKind.Instant },
                { "delivery.projectile", AbilityDeliveryKind.Projectile },
            });

        private static readonly IReadOnlyDictionary<string, AbilityPayloadEffectKind> Effects = ReadOnly(
            new Dictionary<string, AbilityPayloadEffectKind>(StringComparer.Ordinal)
            {
                { "effect.damage", AbilityPayloadEffectKind.Damage },
                { "effect.grant-resource", AbilityPayloadEffectKind.GrantResource },
                { "effect.apply-status", AbilityPayloadEffectKind.ApplyStatus },
            });

        private static readonly IReadOnlyDictionary<string, BattleProjectileMode> ProjectileModes = ReadOnly(
            new Dictionary<string, BattleProjectileMode>(StringComparer.Ordinal)
            {
                { "travel.tracking", BattleProjectileMode.Tracking },
                { "travel.timed-arc", BattleProjectileMode.TimedArc },
                { "travel.linear-return", BattleProjectileMode.LinearReturn },
            });

        private static readonly IReadOnlyDictionary<string, BattleStatusKind> StatusKinds = ReadOnly(
            new Dictionary<string, BattleStatusKind>(StringComparer.Ordinal)
            {
                { "status-kind.modifier", BattleStatusKind.Modifier },
                { "status-kind.slow", BattleStatusKind.Slow },
                { "status-kind.stun", BattleStatusKind.Stun },
                { "status-kind.freeze", BattleStatusKind.Freeze },
                { "status-kind.hit-count", BattleStatusKind.HitCount },
                { "status-kind.burn", BattleStatusKind.Burn },
            });

        private static readonly IReadOnlyDictionary<string, BattleStatusStackingKind> StackModes = ReadOnly(
            new Dictionary<string, BattleStatusStackingKind>(StringComparer.Ordinal)
            {
                { "stacking.refresh", BattleStatusStackingKind.Refresh },
                { "stacking.independent", BattleStatusStackingKind.Independent },
                { "stacking.proc-after-hits", BattleStatusStackingKind.ProcAfterHits },
                { "stacking.additive", BattleStatusStackingKind.Additive },
            });

        private static readonly IReadOnlyDictionary<string, AbilityModifierAttributeKind> ModifierAttributes = ReadOnly(
            new Dictionary<string, AbilityModifierAttributeKind>(StringComparer.Ordinal)
            {
                { "ability-attribute.damage-multiplier", AbilityModifierAttributeKind.DamageMultiplier },
                { "ability-attribute.cooldown", AbilityModifierAttributeKind.Cooldown },
                { "ability-attribute.period", AbilityModifierAttributeKind.Period },
                { "ability-attribute.burst-count", AbilityModifierAttributeKind.BurstCount },
                { "ability-attribute.burst-interval", AbilityModifierAttributeKind.BurstInterval },
                { "ability-attribute.resource-amount", AbilityModifierAttributeKind.ResourceAmount },
            });

        private static readonly IReadOnlyDictionary<string, AbilityModifierOperationKind> ModifierOperations = ReadOnly(
            new Dictionary<string, AbilityModifierOperationKind>(StringComparer.Ordinal)
            {
                { "ability-modifier.add", AbilityModifierOperationKind.Add },
                { "ability-modifier.multiply", AbilityModifierOperationKind.Multiply },
                { "ability-modifier.override", AbilityModifierOperationKind.Override },
            });

        public static bool SupportsActivation(string id) { return Activations.ContainsKey(id); }
        public static bool SupportsEvent(string id) { return Events.ContainsKey(id); }
        public static bool SupportsOwnerRole(string id) { return OwnerRoles.ContainsKey(id); }
        public static bool SupportsTarget(string id) { return Targets.ContainsKey(id); }
        public static bool SupportsDelivery(string id) { return Deliveries.ContainsKey(id); }
        public static bool SupportsEffect(string id) { return Effects.ContainsKey(id); }
        public static bool SupportsProjectileMode(string id) { return ProjectileModes.ContainsKey(id); }
        public static bool SupportsStatusKind(string id) { return StatusKinds.ContainsKey(id); }
        public static bool SupportsStackMode(string id) { return StackModes.ContainsKey(id); }
        public static bool SupportsModifierAttribute(string id) { return ModifierAttributes.ContainsKey(id); }
        public static bool SupportsModifierOperation(string id) { return ModifierOperations.ContainsKey(id); }

        public static CompiledAbilityDefinition Compile(AbilityDefinitionDto source)
        {
            return new CompiledAbilityDefinition
            {
                Id = source.id,
                Activation = new CompiledAbilityActivation
                {
                    Kind = Activations[source.activation.kindId],
                    Event = string.IsNullOrEmpty(source.activation.eventId)
                        ? default(CombatEventKind) : Events[source.activation.eventId],
                    OwnerRole = OwnerRoles[source.activation.ownerRoleId],
                    Priority = source.activation.priority,
                    CooldownTicks = BattleAbilityTiming.SecondsToTicks(source.activation.cooldownSeconds),
                    PeriodTicks = BattleAbilityTiming.SecondsToTicks(source.activation.periodSeconds),
                },
                Timeline = new CompiledAbilityTimeline
                {
                    WindupTicks = BattleAbilityTiming.SecondsToTicks(source.timeline.windupSeconds),
                    RecoveryTicks = BattleAbilityTiming.SecondsToTicks(source.timeline.recoverySeconds),
                },
                DamageMultiplier = source.damageMultiplier,
                BurstCount = Math.Max(1, source.burstCount),
                BurstIntervalTicks = BattleAbilityTiming.SecondsToTicks(source.burstIntervalSeconds),
                Tags = Array.AsReadOnly((string[])source.tags.Clone()),
                Deliveries = Array.AsReadOnly(source.deliveries.Select(CompileDelivery).ToArray()),
            };
        }

        private static CompiledAbilityDelivery CompileDelivery(AbilityDeliveryDefinitionDto source)
        {
            return new CompiledAbilityDelivery
            {
                Target = Targets[source.targetId],
                Kind = Deliveries[source.modeId],
                ProjectileId = source.projectileId,
                Radius = source.radiusLegacyUnits,
                Payload = Array.AsReadOnly(source.payload.Select(CompilePayload).ToArray()),
            };
        }

        private static CompiledAbilityPayloadEffect CompilePayload(AbilityPayloadEffectDefinitionDto source)
        {
            return new CompiledAbilityPayloadEffect
            {
                Kind = Effects[source.kindId],
                StatusId = source.statusId,
                Magnitude = source.magnitude,
                ResourceAmount = source.resourceAmount,
            };
        }

        public static CompiledAbilityModifier Compile(AbilityModifierDefinitionDto source)
        {
            return new CompiledAbilityModifier
            {
                Id = source.id,
                RequiredPlantTag = source.requiredPlantTag,
                TargetAbilityId = source.targetAbilityId,
                TargetAbilityTag = source.targetAbilityTag,
                AllowMultipleMatches = source.allowMultipleMatches,
                Attribute = ModifierAttributes[source.attributeId],
                Operation = ModifierOperations[source.operationId],
                Value = source.value,
            };
        }

        public static CompiledProjectileDefinition Compile(ProjectileDefinitionDto source)
        {
            return new CompiledProjectileDefinition
            {
                Id = source.id,
                Mode = ProjectileModes[source.travelMode],
                Speed = source.speedLegacyUnits,
                FlightTicks = BattleAbilityTiming.SecondsToTicks(source.flightSeconds),
                RangeMultiplier = source.rangeMultiplier,
                HitRadius = source.hitRadiusLegacyUnits,
                MaxHitsPerTarget = source.maxHitsPerTarget,
            };
        }

        public static CompiledStatusDefinition Compile(StatusDefinitionDto source)
        {
            return new CompiledStatusDefinition
            {
                Id = source.id,
                Kind = StatusKinds[source.kindId],
                Stacking = StackModes[source.stackingMode],
                DurationTicks = BattleAbilityTiming.SecondsToTicks(source.durationSeconds),
                TickIntervalTicks = BattleAbilityTiming.SecondsToTicks(source.tickIntervalSeconds),
                Magnitude = source.magnitude,
                MaxStacks = source.maxStacks,
                HitsToProc = source.hitsToProc,
                ProcStatusId = source.procStatusId,
                Polarity = CombatFrameworkCompiler.CompilePolarity(source.polarityId),
                Tags = Array.AsReadOnly((string[])source.tags.Clone()),
                BlocksMovement = source.blocksMovement,
                Modifiers = Array.AsReadOnly(source.modifiers.Select(CombatFrameworkCompiler.Compile).ToArray()),
                PeriodicEffect = CombatFrameworkCompiler.CompilePeriodicEffect(source.periodicEffectId),
            };
        }

        private static IReadOnlyDictionary<string, T> ReadOnly<T>(Dictionary<string, T> values)
        {
            return new ReadOnlyDictionary<string, T>(values);
        }
    }
}
