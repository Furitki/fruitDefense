using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FruitDefense.Content
{
    public enum CombatAttributeKind
    {
        Damage,
        AttackInterval,
        Range,
        MoveSpeed,
        DamageTaken,
        ResourceGain,
    }

    public enum CombatModifierOperation
    {
        Flat,
        Additive,
        Multiplicative,
    }

    public enum CombatStatusPolarity
    {
        Neutral,
        Buff,
        Debuff,
    }

    public enum CombatPeriodicEffectKind
    {
        None,
        Damage,
    }

    public enum BattlePassiveTriggerKind
    {
        BattleStarted,
        WaveFirstSpawned,
        AfterDamageDealt,
        AfterDamageTaken,
        StatusApplied,
        EntityDefeated,
    }

    public enum BattlePassiveOwnerRole
    {
        Any,
        EventSource,
        EventTarget,
    }

    public enum BattlePassiveTargetKind
    {
        Self,
        EventSource,
        EventTarget,
        AllAllies,
        AllEnemies,
    }

    public sealed class CompiledStatusModifier
    {
        public CombatAttributeKind Attribute { get; internal set; }
        public CombatModifierOperation Operation { get; internal set; }
        public float Value { get; internal set; }
        public bool ScaleWithMagnitude { get; internal set; }
    }

    public sealed class CompiledBattlePassive
    {
        public string Id { get; internal set; }
        public BattlePassiveTriggerKind Trigger { get; internal set; }
        public BattlePassiveOwnerRole OwnerRole { get; internal set; }
        public BattlePassiveTargetKind Target { get; internal set; }
        public int Priority { get; internal set; }
        public int CooldownTicks { get; internal set; }
        public IReadOnlyList<string> Tags { get; internal set; }
        public IReadOnlyList<CompiledSkillEffect> Effects { get; internal set; }
    }

    internal static class CombatFrameworkCompiler
    {
        private static readonly IReadOnlyDictionary<string, CombatAttributeKind> Attributes =
            ReadOnly(new Dictionary<string, CombatAttributeKind>(StringComparer.Ordinal)
            {
                { "attribute.damage", CombatAttributeKind.Damage },
                { "attribute.attack-interval", CombatAttributeKind.AttackInterval },
                { "attribute.range", CombatAttributeKind.Range },
                { "attribute.move-speed", CombatAttributeKind.MoveSpeed },
                { "attribute.damage-taken", CombatAttributeKind.DamageTaken },
                { "attribute.resource-gain", CombatAttributeKind.ResourceGain },
            });

        private static readonly IReadOnlyDictionary<string, CombatModifierOperation> Operations =
            ReadOnly(new Dictionary<string, CombatModifierOperation>(StringComparer.Ordinal)
            {
                { "modifier.flat", CombatModifierOperation.Flat },
                { "modifier.additive", CombatModifierOperation.Additive },
                { "modifier.multiplicative", CombatModifierOperation.Multiplicative },
            });

        private static readonly IReadOnlyDictionary<string, CombatStatusPolarity> Polarities =
            ReadOnly(new Dictionary<string, CombatStatusPolarity>(StringComparer.Ordinal)
            {
                { "polarity.neutral", CombatStatusPolarity.Neutral },
                { "polarity.buff", CombatStatusPolarity.Buff },
                { "polarity.debuff", CombatStatusPolarity.Debuff },
            });

        private static readonly IReadOnlyDictionary<string, CombatPeriodicEffectKind> PeriodicEffects =
            ReadOnly(new Dictionary<string, CombatPeriodicEffectKind>(StringComparer.Ordinal)
            {
                { "periodic.none", CombatPeriodicEffectKind.None },
                { "periodic.damage", CombatPeriodicEffectKind.Damage },
            });

        private static readonly IReadOnlyDictionary<string, BattlePassiveTriggerKind> PassiveTriggers =
            ReadOnly(new Dictionary<string, BattlePassiveTriggerKind>(StringComparer.Ordinal)
            {
                { "passive-trigger.battle-started", BattlePassiveTriggerKind.BattleStarted },
                { "passive-trigger.wave-first-spawned", BattlePassiveTriggerKind.WaveFirstSpawned },
                { "passive-trigger.after-damage-dealt", BattlePassiveTriggerKind.AfterDamageDealt },
                { "passive-trigger.after-damage-taken", BattlePassiveTriggerKind.AfterDamageTaken },
                { "passive-trigger.status-applied", BattlePassiveTriggerKind.StatusApplied },
                { "passive-trigger.entity-defeated", BattlePassiveTriggerKind.EntityDefeated },
            });

        private static readonly IReadOnlyDictionary<string, BattlePassiveOwnerRole> OwnerRoles =
            ReadOnly(new Dictionary<string, BattlePassiveOwnerRole>(StringComparer.Ordinal)
            {
                { "owner.any", BattlePassiveOwnerRole.Any },
                { "owner.event-source", BattlePassiveOwnerRole.EventSource },
                { "owner.event-target", BattlePassiveOwnerRole.EventTarget },
            });

        private static readonly IReadOnlyDictionary<string, BattlePassiveTargetKind> PassiveTargets =
            ReadOnly(new Dictionary<string, BattlePassiveTargetKind>(StringComparer.Ordinal)
            {
                { "passive-target.self", BattlePassiveTargetKind.Self },
                { "passive-target.event-source", BattlePassiveTargetKind.EventSource },
                { "passive-target.event-target", BattlePassiveTargetKind.EventTarget },
                { "passive-target.all-allies", BattlePassiveTargetKind.AllAllies },
                { "passive-target.all-enemies", BattlePassiveTargetKind.AllEnemies },
            });

        public static bool SupportsAttribute(string id) { return Attributes.ContainsKey(id); }
        public static bool SupportsOperation(string id) { return Operations.ContainsKey(id); }
        public static bool SupportsPolarity(string id) { return Polarities.ContainsKey(id); }
        public static bool SupportsPeriodicEffect(string id) { return PeriodicEffects.ContainsKey(id); }
        public static bool SupportsPassiveTrigger(string id) { return PassiveTriggers.ContainsKey(id); }
        public static bool SupportsOwnerRole(string id) { return OwnerRoles.ContainsKey(id); }
        public static bool SupportsPassiveTarget(string id) { return PassiveTargets.ContainsKey(id); }

        public static CompiledStatusModifier Compile(StatusModifierDefinitionDto source)
        {
            return new CompiledStatusModifier
            {
                Attribute = Attributes[source.attributeId],
                Operation = Operations[source.operationId],
                Value = source.value,
                ScaleWithMagnitude = source.scaleWithMagnitude,
            };
        }

        public static CombatStatusPolarity CompilePolarity(string id) { return Polarities[id]; }
        public static CombatPeriodicEffectKind CompilePeriodicEffect(string id) { return PeriodicEffects[id]; }

        public static CompiledBattlePassive Compile(PassiveDefinitionDto source)
        {
            return new CompiledBattlePassive
            {
                Id = source.id,
                Trigger = PassiveTriggers[source.triggerId],
                OwnerRole = OwnerRoles[source.ownerRoleId],
                Target = PassiveTargets[source.targetId],
                Priority = source.priority,
                CooldownTicks = BattleSkillTiming.SecondsToTicks(source.cooldownSeconds),
                Tags = Array.AsReadOnly((string[])source.tags.Clone()),
                Effects = Array.AsReadOnly(source.effects.Select(BattleSkillCompiler.CompileEffect).ToArray()),
            };
        }

        private static IReadOnlyDictionary<string, T> ReadOnly<T>(Dictionary<string, T> values)
        {
            return new ReadOnlyDictionary<string, T>(values);
        }
    }
}
