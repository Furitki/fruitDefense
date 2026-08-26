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

    public sealed class CompiledStatusModifier
    {
        public CombatAttributeKind Attribute { get; internal set; }
        public CombatModifierOperation Operation { get; internal set; }
        public float Value { get; internal set; }
        public bool ScaleWithMagnitude { get; internal set; }
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

        public static bool SupportsAttribute(string id) { return Attributes.ContainsKey(id); }
        public static bool SupportsOperation(string id) { return Operations.ContainsKey(id); }
        public static bool SupportsPolarity(string id) { return Polarities.ContainsKey(id); }
        public static bool SupportsPeriodicEffect(string id) { return PeriodicEffects.ContainsKey(id); }

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

        private static IReadOnlyDictionary<string, T> ReadOnly<T>(Dictionary<string, T> values)
        {
            return new ReadOnlyDictionary<string, T>(values);
        }
    }
}
