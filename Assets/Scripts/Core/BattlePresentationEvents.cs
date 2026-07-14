using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitDefense.Core
{
    public enum BattlePresentationEventKind
    {
        Cue,
        Feedback,
    }

    public sealed class BattlePresentationEvent
    {
        public long Sequence { get; }
        public int LogicTick { get; }
        public BattlePresentationEventKind Kind { get; }
        public string CueId { get; }
        public string VisualId { get; }
        public int SourceEntityId { get; }
        public int TargetEntityId { get; }
        public Vector2 Position { get; }
        public bool HasCombatEffect { get; }
        public CombatEffectKind CombatEffectKind { get; }
        public float Duration { get; }
        public string Text { get; }
        public Color Color { get; }

        internal BattlePresentationEvent(long sequence, int logicTick,
            BattlePresentationEventKind kind, string cueId, string visualId,
            int sourceEntityId, int targetEntityId, Vector2 position,
            bool hasCombatEffect, CombatEffectKind combatEffectKind, float duration,
            string text, Color color)
        {
            Sequence = sequence;
            LogicTick = logicTick;
            Kind = kind;
            CueId = cueId ?? string.Empty;
            VisualId = visualId ?? string.Empty;
            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            Position = position;
            HasCombatEffect = hasCombatEffect;
            CombatEffectKind = combatEffectKind;
            Duration = Mathf.Max(0f, duration);
            Text = text ?? string.Empty;
            Color = color;
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

        public BattlePresentationEvent EmitCue(int logicTick, string cueId, string visualId,
            int sourceEntityId, int targetEntityId, Vector2 position,
            bool hasCombatEffect, CombatEffectKind combatEffectKind, float duration)
        {
            var value = new BattlePresentationEvent(_nextSequence++, logicTick,
                BattlePresentationEventKind.Cue, cueId, visualId,
                sourceEntityId, targetEntityId, position, hasCombatEffect,
                combatEffectKind, duration, string.Empty, default(Color));
            Append(value);
            return value;
        }

        public BattlePresentationEvent EmitFeedback(int logicTick, string text,
            Vector2 position, Color color, float duration)
        {
            var value = new BattlePresentationEvent(_nextSequence++, logicTick,
                BattlePresentationEventKind.Feedback, string.Empty, string.Empty,
                0, 0, position, false, default(CombatEffectKind), duration, text, color);
            Append(value);
            return value;
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
