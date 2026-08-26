using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitDefense.Core
{
    public enum BattlePresentationEventKind
    {
        AbilityStarted,
        AbilityReleased,
        ProjectileLaunched,
        DamageResolved,
        StatusApplied,
        StatusProcced,
        ResourceGranted,
        EntityDefeated,
        BattleStateChanged,
    }

    /// <summary>
    /// A transient gameplay fact crossing the one-way simulation-to-view boundary.
    /// Rendering policy deliberately does not belong in this contract.
    /// </summary>
    public sealed class BattlePresentationEvent
    {
        public long Sequence { get; }
        public int LogicTick { get; }
        public BattlePresentationEventKind Kind { get; }
        public string SemanticId { get; }
        public string AbilityId { get; }
        public string ProjectileId { get; }
        public string StatusId { get; }
        public string SourceContentId { get; }
        public string SourceEquipmentId { get; }
        public string TargetContentId { get; }
        public int SourceEntityId { get; }
        public int TargetEntityId { get; }
        public Vector2 Position { get; }
        public Vector2 Direction { get; }
        public float Magnitude { get; }
        public int Count { get; }
        public bool Defeated { get; }

        internal BattlePresentationEvent(long sequence, int logicTick,
            BattlePresentationEventKind kind, string semanticId, string abilityId,
            string projectileId, string statusId, string sourceContentId,
            string sourceEquipmentId, string targetContentId,
            int sourceEntityId, int targetEntityId,
            Vector2 position, Vector2 direction, float magnitude, int count,
            bool defeated)
        {
            Sequence = sequence;
            LogicTick = logicTick;
            Kind = kind;
            SemanticId = semanticId ?? string.Empty;
            AbilityId = abilityId ?? string.Empty;
            ProjectileId = projectileId ?? string.Empty;
            StatusId = statusId ?? string.Empty;
            SourceContentId = sourceContentId ?? string.Empty;
            SourceEquipmentId = sourceEquipmentId ?? string.Empty;
            TargetContentId = targetContentId ?? string.Empty;
            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            Position = Finite(position);
            Direction = Finite(direction);
            Magnitude = float.IsNaN(magnitude) || float.IsInfinity(magnitude)
                ? 0f
                : magnitude;
            Count = count;
            Defeated = defeated;
        }

        private static Vector2 Finite(Vector2 value)
        {
            return float.IsNaN(value.x) || float.IsInfinity(value.x)
                || float.IsNaN(value.y) || float.IsInfinity(value.y)
                ? Vector2.zero
                : value;
        }
    }

    public sealed class BattlePresentationEventStream
    {
        public const int DefaultCapacity = 2048;

        private readonly int _capacity;
        private readonly Queue<BattlePresentationEvent> _pending;
        private long _nextSequence = 1;

        public int Capacity { get { return _capacity; } }
        public int PendingCount { get { return _pending.Count; } }
        public long LastIssuedSequence { get { return _nextSequence - 1; } }
        public long DroppedCount { get; private set; }

        public BattlePresentationEventStream(int capacity = DefaultCapacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _pending = new Queue<BattlePresentationEvent>(Math.Min(capacity, 64));
        }

        public BattlePresentationEvent EmitAbilityStarted(int logicTick, string abilityId,
            int sourceEntityId, int targetEntityId, Vector2 position, Vector2 direction,
            string sourceEquipmentId = "")
        {
            return Emit(logicTick, BattlePresentationEventKind.AbilityStarted, abilityId,
                abilityId, string.Empty, string.Empty, string.Empty, sourceEquipmentId, string.Empty,
                sourceEntityId, targetEntityId, position, direction, 0f, 0, false);
        }

        public BattlePresentationEvent EmitAbilityReleased(int logicTick, string abilityId,
            int sourceEntityId, int targetEntityId, Vector2 position, Vector2 direction,
            string sourceEquipmentId = "")
        {
            return Emit(logicTick, BattlePresentationEventKind.AbilityReleased, abilityId,
                abilityId, string.Empty, string.Empty, string.Empty, sourceEquipmentId, string.Empty,
                sourceEntityId, targetEntityId, position, direction, 0f, 0, false);
        }

        public BattlePresentationEvent EmitProjectileLaunched(int logicTick, string abilityId,
            string projectileId, int sourceEntityId, int targetEntityId,
            Vector2 position, Vector2 direction, string sourceEquipmentId = "")
        {
            return Emit(logicTick, BattlePresentationEventKind.ProjectileLaunched, projectileId,
                abilityId, projectileId, string.Empty, string.Empty, sourceEquipmentId, string.Empty,
                sourceEntityId, targetEntityId, position, direction, 0f, 0, false);
        }

        public BattlePresentationEvent EmitDamageResolved(int logicTick, string abilityId,
            string projectileId, string sourceContentId, string targetContentId,
            int sourceEntityId, int targetEntityId, Vector2 position, Vector2 direction,
            float damage, bool defeated, string sourceEquipmentId = "")
        {
            var semanticId = !string.IsNullOrEmpty(abilityId) ? abilityId
                : !string.IsNullOrEmpty(projectileId) ? projectileId
                : sourceContentId;
            return Emit(logicTick, BattlePresentationEventKind.DamageResolved, semanticId,
                abilityId, projectileId, string.Empty, sourceContentId,
                sourceEquipmentId, targetContentId,
                sourceEntityId, targetEntityId, position, direction, damage, 1, defeated);
        }

        public BattlePresentationEvent EmitStatusApplied(int logicTick, string abilityId,
            string statusId, int sourceEntityId, int targetEntityId, Vector2 position,
            Vector2 direction, float magnitude, int count = 1,
            string sourceEquipmentId = "")
        {
            return EmitStatus(logicTick, BattlePresentationEventKind.StatusApplied,
                abilityId, statusId, sourceEntityId, targetEntityId, position,
                direction, magnitude, count, sourceEquipmentId);
        }

        public BattlePresentationEvent EmitStatusProcced(int logicTick, string abilityId,
            string statusId, int sourceEntityId, int targetEntityId, Vector2 position,
            Vector2 direction, float magnitude, int count = 1,
            string sourceEquipmentId = "")
        {
            return EmitStatus(logicTick, BattlePresentationEventKind.StatusProcced,
                abilityId, statusId, sourceEntityId, targetEntityId, position,
                direction, magnitude, count, sourceEquipmentId);
        }

        public BattlePresentationEvent EmitResourceGranted(int logicTick, string abilityId,
            string resourceId, int sourceEntityId, int targetEntityId,
            Vector2 position, float amount, string sourceEquipmentId = "")
        {
            return Emit(logicTick, BattlePresentationEventKind.ResourceGranted, resourceId,
                abilityId, string.Empty, string.Empty, string.Empty, sourceEquipmentId, string.Empty,
                sourceEntityId, targetEntityId, position, Vector2.zero,
                amount, 1, false);
        }

        public BattlePresentationEvent EmitEntityDefeated(int logicTick, string abilityId,
            string entityContentId, int sourceEntityId, int targetEntityId,
            Vector2 position, Vector2 direction, float reward,
            string sourceEquipmentId = "")
        {
            return Emit(logicTick, BattlePresentationEventKind.EntityDefeated, entityContentId,
                abilityId, string.Empty, string.Empty, string.Empty,
                sourceEquipmentId, entityContentId,
                sourceEntityId, targetEntityId, position, direction, reward, 1, true);
        }

        public BattlePresentationEvent EmitBattleStateChanged(int logicTick, string semanticId,
            Vector2 position, float magnitude = 0f, int count = 0)
        {
            return Emit(logicTick, BattlePresentationEventKind.BattleStateChanged, semanticId,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                0, 0, position, Vector2.zero, magnitude, count, false);
        }

        public int DrainTo(ICollection<BattlePresentationEvent> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            var count = _pending.Count;
            while (_pending.Count > 0) destination.Add(_pending.Dequeue());
            return count;
        }

        public void DiscardPending()
        {
            _pending.Clear();
        }

        public void Reset()
        {
            _pending.Clear();
            _nextSequence = 1;
            DroppedCount = 0;
        }

        private BattlePresentationEvent EmitStatus(int logicTick,
            BattlePresentationEventKind kind, string abilityId, string statusId,
            int sourceEntityId, int targetEntityId, Vector2 position,
            Vector2 direction, float magnitude, int count, string sourceEquipmentId)
        {
            return Emit(logicTick, kind, statusId, abilityId, string.Empty, statusId,
                string.Empty, sourceEquipmentId, string.Empty, sourceEntityId, targetEntityId,
                position, direction, magnitude, count, false);
        }

        private BattlePresentationEvent Emit(int logicTick, BattlePresentationEventKind kind,
            string semanticId, string abilityId, string projectileId, string statusId,
            string sourceContentId, string sourceEquipmentId, string targetContentId,
            int sourceEntityId, int targetEntityId, Vector2 position,
            Vector2 direction, float magnitude,
            int count, bool defeated)
        {
            var value = new BattlePresentationEvent(_nextSequence++, logicTick, kind,
                semanticId, abilityId, projectileId, statusId, sourceContentId,
                sourceEquipmentId, targetContentId, sourceEntityId, targetEntityId,
                position, direction,
                magnitude, count, defeated);
            Append(value);
            return value;
        }

        private void Append(BattlePresentationEvent value)
        {
            if (_pending.Count >= _capacity)
            {
                _pending.Dequeue();
                DroppedCount++;
            }
            _pending.Enqueue(value);
        }
    }
}
