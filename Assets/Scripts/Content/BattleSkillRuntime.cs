using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FruitDefense.Content
{
    public enum BattleTriggerKind { CooldownReady, Periodic, AfterDamageDealt, WaveFirstSpawned }
    public enum BattleTargetKind { Self, EventTarget, FrontmostEnemyInRange, AllEnemiesInRadius, AllEnemies, LineFromCaster }
    public enum BattleEffectKind { Damage, LaunchProjectile, GrantResource, ApplyStatus, EmitCue }
    public enum BattleProjectileMode { Tracking, TimedArc, LinearReturn }
    public enum BattleStatusKind { Modifier, Slow, Stun, Freeze, HitCount, Burn }
    public enum BattleStatusStackingKind { Refresh, Independent, ProcAfterHits, Additive }

    public static class BattleSkillTiming
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

    public sealed class CompiledSkillEffect
    {
        public BattleEffectKind Kind { get; internal set; }
        public string ProjectileId { get; internal set; }
        public string StatusId { get; internal set; }
        public float Magnitude { get; internal set; }
        public float Radius { get; internal set; }
        public int ResourceAmount { get; internal set; }
        public string CueId { get; internal set; }
    }

    public sealed class CompiledBattleSkill
    {
        public string Id { get; internal set; }
        public BattleTriggerKind Trigger { get; internal set; }
        public BattleTargetKind Target { get; internal set; }
        public int CooldownTicks { get; internal set; }
        public int BurstCount { get; internal set; }
        public int BurstIntervalTicks { get; internal set; }
        public int ActionTicks { get; internal set; }
        public float DamageMultiplier { get; internal set; }
        public int ResourceAmount { get; internal set; }
        public string VisualId { get; internal set; }
        public string CueId { get; internal set; }
        public IReadOnlyList<string> Tags { get; internal set; }
        public IReadOnlyList<CompiledSkillEffect> Effects { get; internal set; }

        internal CompiledBattleSkill Clone()
        {
            return new CompiledBattleSkill
            {
                Id = Id,
                Trigger = Trigger,
                Target = Target,
                CooldownTicks = CooldownTicks,
                BurstCount = BurstCount,
                BurstIntervalTicks = BurstIntervalTicks,
                ActionTicks = ActionTicks,
                DamageMultiplier = DamageMultiplier,
                ResourceAmount = ResourceAmount,
                VisualId = VisualId,
                CueId = CueId,
                Tags = Tags,
                Effects = Effects,
            };
        }
    }

    public sealed class CompiledProjectileDefinition
    {
        public string Id { get; internal set; }
        public BattleProjectileMode Mode { get; internal set; }
        public float Speed { get; internal set; }
        public int FlightTicks { get; internal set; }
        public float BlastRadius { get; internal set; }
        public float RangeMultiplier { get; internal set; }
        public float HitRadius { get; internal set; }
        public int MaxHitsPerTarget { get; internal set; }
        public string VisualId { get; internal set; }
        public string ImpactCueId { get; internal set; }
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
        public string CueId { get; internal set; }
        public CombatStatusPolarity Polarity { get; internal set; }
        public IReadOnlyList<string> Tags { get; internal set; }
        public bool BlocksMovement { get; internal set; }
        public IReadOnlyList<CompiledStatusModifier> Modifiers { get; internal set; }
        public CombatPeriodicEffectKind PeriodicEffect { get; internal set; }
    }

    internal static class BattleSkillCompiler
    {
        private static readonly IReadOnlyDictionary<string, BattleTriggerKind> Triggers =
            new ReadOnlyDictionary<string, BattleTriggerKind>(new Dictionary<string, BattleTriggerKind>(StringComparer.Ordinal)
            {
                { "trigger.cooldown", BattleTriggerKind.CooldownReady },
                { "trigger.periodic", BattleTriggerKind.Periodic },
                { "trigger.after-damage", BattleTriggerKind.AfterDamageDealt },
                { "trigger.wave-first-spawned", BattleTriggerKind.WaveFirstSpawned },
            });

        private static readonly IReadOnlyDictionary<string, BattleTargetKind> Targets =
            new ReadOnlyDictionary<string, BattleTargetKind>(new Dictionary<string, BattleTargetKind>(StringComparer.Ordinal)
            {
                { "target.self", BattleTargetKind.Self },
                { "target.event", BattleTargetKind.EventTarget },
                { "target.front", BattleTargetKind.FrontmostEnemyInRange },
                { "target.area", BattleTargetKind.AllEnemiesInRadius },
                { "target.all-enemies", BattleTargetKind.AllEnemies },
                { "target.line", BattleTargetKind.LineFromCaster },
            });

        private static readonly IReadOnlyDictionary<string, BattleEffectKind> Effects =
            new ReadOnlyDictionary<string, BattleEffectKind>(new Dictionary<string, BattleEffectKind>(StringComparer.Ordinal)
            {
                { "effect.damage", BattleEffectKind.Damage },
                { "effect.launch-projectile", BattleEffectKind.LaunchProjectile },
                { "effect.grant-resource", BattleEffectKind.GrantResource },
                { "effect.apply-status", BattleEffectKind.ApplyStatus },
                { "effect.emit-cue", BattleEffectKind.EmitCue },
            });

        private static readonly IReadOnlyDictionary<string, BattleProjectileMode> ProjectileModes =
            new ReadOnlyDictionary<string, BattleProjectileMode>(new Dictionary<string, BattleProjectileMode>(StringComparer.Ordinal)
            {
                { "travel.tracking", BattleProjectileMode.Tracking },
                { "travel.timed-arc", BattleProjectileMode.TimedArc },
                { "travel.linear-return", BattleProjectileMode.LinearReturn },
            });

        private static readonly IReadOnlyDictionary<string, BattleStatusKind> StatusKinds =
            new ReadOnlyDictionary<string, BattleStatusKind>(new Dictionary<string, BattleStatusKind>(StringComparer.Ordinal)
            {
                { "status-kind.modifier", BattleStatusKind.Modifier },
                { "status-kind.slow", BattleStatusKind.Slow },
                { "status-kind.stun", BattleStatusKind.Stun },
                { "status-kind.freeze", BattleStatusKind.Freeze },
                { "status-kind.hit-count", BattleStatusKind.HitCount },
                { "status-kind.burn", BattleStatusKind.Burn },
            });

        private static readonly IReadOnlyDictionary<string, BattleStatusStackingKind> StackModes =
            new ReadOnlyDictionary<string, BattleStatusStackingKind>(new Dictionary<string, BattleStatusStackingKind>(StringComparer.Ordinal)
            {
                { "stacking.refresh", BattleStatusStackingKind.Refresh },
                { "stacking.independent", BattleStatusStackingKind.Independent },
                { "stacking.proc-after-hits", BattleStatusStackingKind.ProcAfterHits },
                { "stacking.additive", BattleStatusStackingKind.Additive },
            });

        public static bool SupportsTrigger(string id) { return Triggers.ContainsKey(id); }
        public static bool SupportsTarget(string id) { return Targets.ContainsKey(id); }
        public static bool SupportsEffect(string id) { return Effects.ContainsKey(id); }
        public static bool SupportsProjectileMode(string id) { return ProjectileModes.ContainsKey(id); }
        public static bool SupportsStatusKind(string id) { return StatusKinds.ContainsKey(id); }
        public static bool SupportsStackMode(string id) { return StackModes.ContainsKey(id); }

        public static CompiledBattleSkill Compile(SkillDefinitionDto source)
        {
            var effects = source.effects.Select(CompileEffect).ToArray();
            return new CompiledBattleSkill
            {
                Id = source.id,
                Trigger = Triggers[source.triggerId],
                Target = Targets[source.targetId],
                CooldownTicks = BattleSkillTiming.SecondsToTicks(source.cooldownSeconds),
                BurstCount = Math.Max(1, source.burstCount),
                BurstIntervalTicks = BattleSkillTiming.SecondsToTicks(source.burstIntervalSeconds),
                ActionTicks = BattleSkillTiming.SecondsToTicks(source.actionSeconds),
                DamageMultiplier = source.damageMultiplier,
                ResourceAmount = source.resourceAmount,
                VisualId = source.visualId,
                CueId = source.cueId,
                Tags = Array.AsReadOnly((string[])source.tags.Clone()),
                Effects = Array.AsReadOnly(effects),
            };
        }

        internal static CompiledSkillEffect CompileEffect(SkillEffectDefinitionDto effect)
        {
            return new CompiledSkillEffect
            {
                Kind = Effects[effect.kindId],
                ProjectileId = effect.projectileId,
                StatusId = effect.statusId,
                Magnitude = effect.magnitude,
                Radius = effect.radiusLegacyUnits,
                ResourceAmount = effect.resourceAmount,
                CueId = effect.cueId,
            };
        }

        public static CompiledProjectileDefinition Compile(ProjectileDefinitionDto source)
        {
            return new CompiledProjectileDefinition
            {
                Id = source.id,
                Mode = ProjectileModes[source.travelMode],
                Speed = source.speedLegacyUnits,
                FlightTicks = BattleSkillTiming.SecondsToTicks(source.flightSeconds),
                BlastRadius = source.blastRadiusLegacyUnits,
                RangeMultiplier = source.rangeMultiplier,
                HitRadius = source.hitRadiusLegacyUnits,
                MaxHitsPerTarget = source.maxHitsPerTarget,
                VisualId = source.visualId,
                ImpactCueId = source.impactCueId,
            };
        }

        public static CompiledStatusDefinition Compile(StatusDefinitionDto source)
        {
            return new CompiledStatusDefinition
            {
                Id = source.id,
                Kind = StatusKinds[source.kindId],
                Stacking = StackModes[source.stackingMode],
                DurationTicks = BattleSkillTiming.SecondsToTicks(source.durationSeconds),
                TickIntervalTicks = BattleSkillTiming.SecondsToTicks(source.tickIntervalSeconds),
                Magnitude = source.magnitude,
                MaxStacks = source.maxStacks,
                HitsToProc = source.hitsToProc,
                ProcStatusId = source.procStatusId,
                CueId = source.cueId,
                Polarity = CombatFrameworkCompiler.CompilePolarity(source.polarityId),
                Tags = Array.AsReadOnly((string[])source.tags.Clone()),
                BlocksMovement = source.blocksMovement,
                Modifiers = Array.AsReadOnly(source.modifiers.Select(CombatFrameworkCompiler.Compile).ToArray()),
                PeriodicEffect = CombatFrameworkCompiler.CompilePeriodicEffect(source.periodicEffectId),
            };
        }
    }
}
