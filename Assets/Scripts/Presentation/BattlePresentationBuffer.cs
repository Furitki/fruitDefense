using System;
using System.Collections.Generic;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Presentation
{
    public sealed class PresentationCombatEffect
    {
        public CombatEffectKind Kind;
        public Vector2 Position;
        public float Ttl;
        public float Duration;
        public string VisualId = string.Empty;
        public string CueId = string.Empty;
    }

    public sealed class PresentationFeedback
    {
        public string Text = string.Empty;
        public Vector2 Point;
        public Color Color;
        public float Ttl;
    }

    public sealed class BattlePresentationBuffer
    {
        private readonly List<BattlePresentationEvent> _drainBuffer = new List<BattlePresentationEvent>(32);
        private readonly List<PresentationCombatEffect> _combatEffects = new List<PresentationCombatEffect>();
        private readonly List<PresentationFeedback> _feedback = new List<PresentationFeedback>();

        public IReadOnlyList<PresentationCombatEffect> CombatEffects { get { return _combatEffects; } }
        public IReadOnlyList<PresentationFeedback> Feedback { get { return _feedback; } }

        public int Consume(GameSimulation simulation)
        {
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));
            _drainBuffer.Clear();
            var count = simulation.DrainPresentationEvents(_drainBuffer);
            Consume(_drainBuffer);
            return count;
        }

        public void Consume(IEnumerable<BattlePresentationEvent> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            foreach (var value in events)
            {
                if (value == null) continue;
                if (value.Kind == BattlePresentationEventKind.Cue && value.HasCombatEffect)
                {
                    _combatEffects.Add(new PresentationCombatEffect
                    {
                        Kind = value.CombatEffectKind,
                        Position = value.Position,
                        Ttl = value.Duration,
                        Duration = value.Duration,
                        CueId = value.CueId,
                        VisualId = value.VisualId,
                    });
                }
                else if (value.Kind == BattlePresentationEventKind.Feedback)
                {
                    _feedback.Add(new PresentationFeedback
                    {
                        Text = value.Text,
                        Point = value.Position,
                        Color = value.Color,
                        Ttl = value.Duration,
                    });
                }
            }
        }

        public void Advance(float unscaledDelta)
        {
            var delta = float.IsNaN(unscaledDelta) || float.IsInfinity(unscaledDelta)
                ? 0f
                : Mathf.Max(0f, unscaledDelta);
            for (var index = _combatEffects.Count - 1; index >= 0; index--)
            {
                _combatEffects[index].Ttl -= delta;
                if (_combatEffects[index].Ttl <= 0f) _combatEffects.RemoveAt(index);
            }
            for (var index = _feedback.Count - 1; index >= 0; index--)
            {
                _feedback[index].Ttl -= delta;
                if (_feedback[index].Ttl <= 0f) _feedback.RemoveAt(index);
            }
        }

        public void Clear()
        {
            _drainBuffer.Clear();
            _combatEffects.Clear();
            _feedback.Clear();
        }
    }
}
