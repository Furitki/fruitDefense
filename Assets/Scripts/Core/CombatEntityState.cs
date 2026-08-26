using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Core
{
    public enum CombatFaction
    {
        Player,
        Enemy,
    }

    [Serializable]
    public abstract class CombatEntityState
    {
        public int Id;
        public string DefinitionId = string.Empty;
        public readonly List<AbilityRuntimeState> AbilityRuntimes = new List<AbilityRuntimeState>();
        public readonly List<StatusInstance> Statuses = new List<StatusInstance>();

        public int EntityId { get { return Id; } }
        public abstract CombatFaction Faction { get; }
        public abstract bool IsAlive { get; }
    }

    public readonly struct CombatEffectContext
    {
        public long RootEventSequence { get; }
        public CombatEntityState Owner { get; }
        public CombatEntityState EventSource { get; }
        public CombatEntityState EventTarget { get; }
        public Vector2 Origin { get; }
        public float Range { get; }
        public float EventMagnitude { get; }

        public CombatEffectContext(long rootEventSequence, CombatEntityState owner,
            CombatEntityState eventSource, CombatEntityState eventTarget,
            Vector2 origin, float range, float eventMagnitude)
        {
            RootEventSequence = rootEventSequence;
            Owner = owner;
            EventSource = eventSource;
            EventTarget = eventTarget;
            Origin = origin;
            Range = range;
            EventMagnitude = eventMagnitude;
        }
    }

    public static class CombatAttributeResolver
    {
        public static float Resolve(float baseValue, CombatEntityState entity,
            CombatAttributeKind attribute, CompiledBattleContentCatalog content)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (content == null) throw new ArgumentNullException(nameof(content));
            var value = baseValue;
            foreach (var status in entity.Statuses.OrderBy(item => item.Sequence))
            {
                CompiledStatusDefinition definition;
                if (status.RemainingTicks <= 0
                    || !content.RuntimeStatuses.TryGetValue(status.DefinitionId, out definition)) continue;
                foreach (var modifier in definition.Modifiers)
                {
                    if (modifier.Attribute != attribute) continue;
                    var stacks = Mathf.Max(1, status.StackCount);
                    var amount = modifier.Value;
                    if (modifier.ScaleWithMagnitude) amount *= status.Magnitude;
                    if (modifier.Operation == CombatModifierOperation.Flat) value += amount * stacks;
                    else if (modifier.Operation == CombatModifierOperation.Additive) value *= 1f + amount * stacks;
                    else value *= Mathf.Pow(amount, stacks);
                }
            }
            if (float.IsNaN(value) || float.IsInfinity(value)) return Clamp(attribute, baseValue);
            return Clamp(attribute, value);
        }

        private static float Clamp(CombatAttributeKind attribute, float value)
        {
            if (attribute == CombatAttributeKind.AttackInterval)
                return Mathf.Max(BattleAbilityTiming.FixedStepSeconds, value);
            return Mathf.Max(0f, value);
        }
    }

    public static class CombatStatusRuntime
    {
        public static int Remove(CombatEntityState entity, CompiledBattleContentCatalog content,
            string definitionId = "", CombatStatusPolarity? polarity = null, string tag = "")
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (content == null) throw new ArgumentNullException(nameof(content));
            var matches = entity.Statuses
                .Where(status => Matches(status, content, definitionId, polarity, tag))
                .OrderBy(status => status.Sequence).ToArray();
            foreach (var status in matches) entity.Statuses.Remove(status);
            return matches.Length;
        }

        public static bool BlocksMovement(CombatEntityState entity, CompiledBattleContentCatalog content)
        {
            return entity.Statuses.Any(status =>
            {
                CompiledStatusDefinition definition;
                return status.RemainingTicks > 0
                    && content.RuntimeStatuses.TryGetValue(status.DefinitionId, out definition)
                    && definition.BlocksMovement;
            });
        }

        private static bool Matches(StatusInstance status, CompiledBattleContentCatalog content,
            string definitionId, CombatStatusPolarity? polarity, string tag)
        {
            CompiledStatusDefinition definition;
            if (!content.RuntimeStatuses.TryGetValue(status.DefinitionId, out definition)) return false;
            if (!string.IsNullOrEmpty(definitionId)
                && !string.Equals(status.DefinitionId, definitionId, StringComparison.Ordinal)) return false;
            if (polarity.HasValue && definition.Polarity != polarity.Value) return false;
            return string.IsNullOrEmpty(tag) || definition.Tags.Contains(tag);
        }
    }
}
