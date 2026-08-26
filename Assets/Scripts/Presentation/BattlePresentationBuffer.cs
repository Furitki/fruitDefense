using System;
using System.Collections.Generic;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Presentation
{
    public sealed class PresentationCombatEffect
    {
        public PresentationVfxKind Kind;
        public Vector2 Position;
        public Vector2 Direction;
        public float Ttl;
        public float Duration;
        public CombatFeedbackPriority Priority;
        public long EventSequence;
    }

    public sealed class PresentationEntityReaction
    {
        public int EntityId;
        public Vector2 Direction;
        public float Recoil;
        public float Flash;
        public float Squash;
        public float Displacement;
        public float Ttl;
        public float Duration;
        public CombatFeedbackPriority Priority;

        public float Progress
        {
            get { return Duration <= 0f ? 1f : 1f - Mathf.Clamp01(Ttl / Duration); }
        }
    }

    public sealed class PresentationFeedback
    {
        public BattlePresentationEventKind Kind;
        public string SemanticId = string.Empty;
        public string ProfileId = string.Empty;
        public int TargetEntityId;
        public Vector2 EventPoint;
        public Vector2 Point;
        public CombatFloatingTextRole Role;
        public float Magnitude;
        public int Count;
        public float Ttl;
        public float Duration;
        public int LastLogicTick;
        public int MergeWindowTicks;
        public CombatFeedbackPriority Priority;
        public long EventSequence;
        public int VisualLane;
        public float FollowDuration;
        public float FollowElapsed;
        public string Text = string.Empty;
        public bool TextDirty;

        public float LifetimeProgress
        {
            get { return Duration <= 0f ? 1f : 1f - Mathf.Clamp01(Ttl / Duration); }
        }

        public float DetachedProgress
        {
            get
            {
                var detachedDuration = Duration - FollowDuration;
                if (detachedDuration <= 0f) return 1f;
                var elapsed = Duration - Ttl - FollowDuration;
                return Mathf.Clamp01(elapsed / detachedDuration);
            }
        }

        public bool IsFollowingTarget
        {
            get
            {
                return TargetEntityId != 0 && FollowDuration > 0f
                    && FollowElapsed < FollowDuration;
            }
        }

        public void Initialize(BattlePresentationEvent value,
            CombatFeedbackProfile profile, CombatFloatingTextStyle style,
            int visualLane, string text)
        {
            Kind = value.Kind;
            SemanticId = value.SemanticId ?? string.Empty;
            ProfileId = profile.Id;
            TargetEntityId = value.TargetEntityId;
            EventPoint = value.Position;
            Point = value.Position;
            Role = profile.FloatingTextRole;
            Magnitude = Mathf.Abs(value.Magnitude);
            Count = Mathf.Max(1, value.Count);
            Ttl = style.Duration;
            Duration = style.Duration;
            LastLogicTick = value.LogicTick;
            MergeWindowTicks = BattlePresentationBuffer.SecondsToLogicTicks(
                profile.MergeWindow);
            Priority = profile.Priority;
            EventSequence = value.Sequence;
            VisualLane = Mathf.Clamp(visualLane, 0,
                CombatFloatingTextStyleCatalog.VisualLaneCount - 1);
            FollowDuration = value.Kind == BattlePresentationEventKind.DamageResolved
                    || value.Kind == BattlePresentationEventKind.EntityDefeated
                ? CombatFloatingTextStyleCatalog.FollowSeconds
                : 0f;
            FollowElapsed = 0f;
            Text = text ?? string.Empty;
            TextDirty = false;
        }

        public void UpdateFollowPoint(Vector2 point)
        {
            if (!IsFollowingTarget || float.IsNaN(point.x) || float.IsInfinity(point.x)
                || float.IsNaN(point.y) || float.IsInfinity(point.y))
                return;
            Point = point;
        }

        public void DetachFromTarget()
        {
            if (FollowDuration <= 0f) return;
            TargetEntityId = 0;
            FollowElapsed = FollowDuration;
        }

        public void AdvanceFollow(float delta)
        {
            if (FollowDuration <= 0f || FollowElapsed >= FollowDuration) return;
            FollowElapsed = Mathf.Min(FollowDuration,
                FollowElapsed + Mathf.Max(0f, delta));
        }

        public void Reset()
        {
            Kind = default;
            SemanticId = string.Empty;
            ProfileId = string.Empty;
            TargetEntityId = 0;
            EventPoint = Vector2.zero;
            Point = Vector2.zero;
            Role = CombatFloatingTextRole.None;
            Magnitude = 0f;
            Count = 0;
            Ttl = 0f;
            Duration = 0f;
            LastLogicTick = 0;
            MergeWindowTicks = 0;
            Priority = default;
            EventSequence = 0;
            VisualLane = 0;
            FollowDuration = 0f;
            FollowElapsed = 0f;
            Text = string.Empty;
            TextDirty = false;
        }
    }

    public sealed class PresentationAudioRequest
    {
        public CombatAudioRoute Route;
        public Vector2 Position;
        public float Ttl;
        public CombatFeedbackPriority Priority;
        public long EventSequence;
    }

    public interface ICombatAudioRouter
    {
        void Route(PresentationAudioRequest request);
    }

    /// <summary>
    /// Bundled content currently has no approved audio assets. The view still
    /// consumes validated routing records explicitly through this silent sink.
    /// </summary>
    public sealed class SilentCombatAudioRouter : ICombatAudioRouter
    {
        public static readonly SilentCombatAudioRouter Instance =
            new SilentCombatAudioRouter();

        private SilentCombatAudioRouter() { }

        public void Route(PresentationAudioRequest request) { }
    }

    public sealed class PresentationImpactBeat
    {
        public CombatImpactBeatRole Role { get; private set; }
        public float Amplitude { get; private set; }
        public float Flash { get; private set; }
        public float Ttl { get; private set; }
        public float Duration { get; private set; }
        public float Oscillations { get; private set; }
        public long EventSequence { get; private set; }

        public float Progress
        {
            get { return Duration <= 0f ? 1f : 1f - Mathf.Clamp01(Ttl / Duration); }
        }

        internal void Start(CombatImpactBeatStyle style, long eventSequence)
        {
            Role = style.Role;
            Amplitude = style.Amplitude;
            Flash = style.Flash;
            Ttl = style.Duration;
            Duration = style.Duration;
            Oscillations = style.Oscillations;
            EventSequence = eventSequence;
        }

        internal bool Advance(float delta)
        {
            Ttl -= Mathf.Max(0f, delta);
            return Ttl > 0f;
        }

        internal void Clear()
        {
            Role = CombatImpactBeatRole.None;
            Amplitude = 0f;
            Flash = 0f;
            Ttl = 0f;
            Duration = 0f;
            Oscillations = 0f;
            EventSequence = 0;
        }
    }

    public readonly struct PresentationReactionSample
    {
        public PresentationReactionSample(Vector2 offset, Vector2 scale, float flash)
        {
            Offset = offset;
            Scale = scale;
            Flash = flash;
        }

        public Vector2 Offset { get; }
        public Vector2 Scale { get; }
        public float Flash { get; }
    }

    internal sealed class CombatFloatingTextCache
    {
        private const int Capacity = 256;
        private readonly Dictionary<long, string> _values =
            new Dictionary<long, string>();

        public int Count { get { return _values.Count; } }

        public string Format(CombatFloatingTextRole role, float magnitude, int count)
        {
            if (role == CombatFloatingTextRole.Defeat)
            {
                if (count <= 1) return "击败";
                var defeatKey = ((long)(int)role << 32) | (uint)count;
                string defeatText;
                if (_values.TryGetValue(defeatKey, out defeatText)) return defeatText;
                defeatText = "击败×" + count;
                if (_values.Count < Capacity) _values.Add(defeatKey, defeatText);
                return defeatText;
            }
            if (role == CombatFloatingTextRole.Control) return "冻结";
            var amount = Mathf.Max(0, Mathf.RoundToInt(Mathf.Abs(magnitude)));
            var key = ((long)(int)role << 32) | (uint)amount;
            string text;
            if (_values.TryGetValue(key, out text)) return text;
            switch (role)
            {
                case CombatFloatingTextRole.Resource:
                    text = "+" + amount + " 阳光";
                    break;
                case CombatFloatingTextRole.NormalDamage:
                case CombatFloatingTextRole.HeavyDamage:
                case CombatFloatingTextRole.PeriodicDamage:
                    text = "-" + amount;
                    break;
                default:
                    return string.Empty;
            }
            if (_values.Count < Capacity) _values.Add(key, text);
            return text;
        }
    }

    /// <summary>
    /// The local feedback director. It owns lifetime, prioritization, merging,
    /// rate limiting, and channel caps, and never writes back to simulation state.
    /// </summary>
    public sealed class BattlePresentationBuffer
    {
        private enum FeedbackChannel
        {
            Vfx,
            Audio,
        }

        private readonly struct RateLimitKey : IEquatable<RateLimitKey>
        {
            public RateLimitKey(string profileId, FeedbackChannel channel)
            {
                ProfileId = profileId ?? string.Empty;
                Channel = channel;
            }

            public string ProfileId { get; }
            public FeedbackChannel Channel { get; }

            public bool Equals(RateLimitKey other)
            {
                return Channel == other.Channel
                    && string.Equals(ProfileId, other.ProfileId,
                        StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is RateLimitKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(ProfileId) * 397)
                        ^ (int)Channel;
                }
            }
        }

        public const int CombatEffectCapacity = 48;
        public const int ReactionCapacity = 32;
        public const int FloatingTextCapacity = CombatFloatingTextStyleCatalog.TotalCapacity;
        public const int OrdinaryFloatingTextCapacity =
            CombatFloatingTextStyleCatalog.OrdinaryCapacity;
        public const int AudioCapacity = 8;

        private const float AudioRequestLifetime = .12f;
        private readonly CombatFeedbackCatalog _catalog;
        private readonly CombatFloatingTextStyleCatalog _floatingTextStyles;
        private readonly CombatFloatingTextCache _floatingTextCache =
            new CombatFloatingTextCache();
        private readonly List<BattlePresentationEvent> _drainBuffer =
            new List<BattlePresentationEvent>(32);
        private readonly List<PresentationCombatEffect> _combatEffects =
            new List<PresentationCombatEffect>(CombatEffectCapacity);
        private readonly List<PresentationEntityReaction> _reactions =
            new List<PresentationEntityReaction>(ReactionCapacity);
        private readonly List<PresentationFeedback> _feedback =
            new List<PresentationFeedback>(FloatingTextCapacity);
        private readonly Stack<PresentationFeedback> _feedbackPool =
            new Stack<PresentationFeedback>(FloatingTextCapacity);
        private readonly List<PresentationAudioRequest> _audio =
            new List<PresentationAudioRequest>(AudioCapacity);
        private readonly Dictionary<RateLimitKey, int> _lastChannelTicks =
            new Dictionary<RateLimitKey, int>();
        private readonly PresentationImpactBeat _impactBeat =
            new PresentationImpactBeat();
        private bool _hasActiveImpactBeat;
        private float _battleClock;
        private float _impactClock;
        private float _lastImpactBeatAcceptedAt = float.NegativeInfinity;
        private CombatImpactBeatRole _lastAcceptedBeatRole;
        private float _clusterWindowStartedAt = float.NegativeInfinity;
        private int _clusterDefeatCount;
        private int _allocatedFeedbackCount;

        public BattlePresentationBuffer()
            : this(CombatFeedbackCatalog.CreateBundled())
        {
        }

        public BattlePresentationBuffer(CombatFeedbackCatalog catalog)
            : this(catalog, CombatFloatingTextStyleCatalog.CreateBundled())
        {
        }

        public BattlePresentationBuffer(CombatFeedbackCatalog catalog,
            CombatFloatingTextStyleCatalog floatingTextStyles)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _floatingTextStyles = floatingTextStyles
                ?? throw new ArgumentNullException(nameof(floatingTextStyles));
        }

        public CombatFeedbackCatalog Catalog { get { return _catalog; } }
        public CombatFloatingTextStyleCatalog FloatingTextStyles
        {
            get { return _floatingTextStyles; }
        }
        public IReadOnlyList<PresentationCombatEffect> CombatEffects { get { return _combatEffects; } }
        public IReadOnlyList<PresentationEntityReaction> Reactions { get { return _reactions; } }
        public IReadOnlyList<PresentationFeedback> Feedback { get { return _feedback; } }
        public IReadOnlyList<PresentationAudioRequest> AudioRequests { get { return _audio; } }
        public PresentationImpactBeat ActiveImpactBeat
        {
            get { return _hasActiveImpactBeat ? _impactBeat : null; }
        }
        public float BattleClock { get { return _battleClock; } }
        public float ImpactClock { get { return _impactClock; } }
        public int OrdinaryFeedbackCount { get { return CountOrdinaryFeedback(); } }
        public int AllocatedFeedbackCount { get { return _allocatedFeedbackCount; } }
        public int PooledFeedbackCount { get { return _feedbackPool.Count; } }
        public int CachedFloatingTextCount { get { return _floatingTextCache.Count; } }
        public int MissingProfileCount { get; private set; }

        public int Consume(GameSimulation simulation)
        {
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));
            _drainBuffer.Clear();
            var count = simulation.DrainPresentationEvents(_drainBuffer);
            Consume(_drainBuffer);
            return count;
        }

        public void Consume(IReadOnlyList<BattlePresentationEvent> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            for (var index = 0; index < events.Count; index++)
            {
                var value = events[index];
                if (value == null) continue;
                CombatFeedbackCatalogEntry entry;
                if (!_catalog.TryResolve(value, out entry))
                {
                    MissingProfileCount++;
                    continue;
                }
                if (entry.Policy == CombatFeedbackPolicy.None) continue;
                Route(value, entry.Profile);
            }
            RefreshDirtyFeedbackText();
        }

        /// <summary>
        /// Local presentation lifetime uses unscaled display time. Logic ticks still
        /// own merge eligibility. Pause freezes all channels and 2x keeps an 80%
        /// real-time reading floor instead of halving feedback lifetime.
        /// </summary>
        public void Advance(float unscaledDisplayDelta, bool paused, int battleSpeed)
        {
            var unscaledDelta = float.IsNaN(unscaledDisplayDelta)
                        || float.IsInfinity(unscaledDisplayDelta)
                ? 0f
                : Mathf.Max(0f, unscaledDisplayDelta);
            if (paused || unscaledDelta <= 0f) return;

            _impactClock += unscaledDelta;
            AdvanceImpactBeat(unscaledDelta);

            var displayDelta = unscaledDelta * DisplayClockScale(battleSpeed);
            _battleClock += displayDelta;
            AdvanceEffects(displayDelta);
            AdvanceReactions(displayDelta);
            AdvanceFeedback(displayDelta);
            AdvanceAudio(displayDelta);
        }

        public static float DisplayClockScale(int battleSpeed)
        {
            return battleSpeed >= 2 ? 1.25f : 1f;
        }

        public int RoutePendingAudio(ICombatAudioRouter router)
        {
            if (router == null) throw new ArgumentNullException(nameof(router));
            var count = _audio.Count;
            for (var index = 0; index < _audio.Count; index++)
                router.Route(_audio[index]);
            _audio.Clear();
            return count;
        }

        public PresentationReactionSample ReactionFor(int entityId)
        {
            var offset = Vector2.zero;
            var scale = Vector2.one;
            var flash = 0f;
            for (var index = 0; index < _reactions.Count; index++)
            {
                var value = _reactions[index];
                if (value.EntityId != entityId) continue;
                var pulse = Mathf.Sin(value.Progress * Mathf.PI);
                var direction = value.Direction.sqrMagnitude <= .0001f
                    ? Vector2.right
                    : value.Direction.normalized;
                offset += direction * (value.Displacement - value.Recoil) * pulse;
                scale.x *= 1f + value.Squash * pulse;
                scale.y *= 1f - value.Squash * pulse;
                flash = Mathf.Max(flash, value.Flash * pulse);
            }
            return new PresentationReactionSample(offset, scale, Mathf.Clamp01(flash));
        }

        public Vector2 BattlefieldOffset
        {
            get
            {
                var value = ActiveImpactBeat;
                if (value == null || value.Duration <= 0f) return Vector2.zero;
                var progress = value.Progress;
                var envelope = 1f - progress;
                envelope *= envelope;
                var initialPhase = value.EventSequence * .754877666f
                    * Mathf.PI * 2f;
                var phase = initialPhase + progress * value.Oscillations
                    * Mathf.PI * 2f;
                return new Vector2(Mathf.Cos(phase), Mathf.Sin(phase))
                    * Mathf.Min(CombatImpactBeatCatalog.MaximumAmplitude,
                        value.Amplitude) * envelope;
            }
        }

        public float BattlefieldFlash
        {
            get
            {
                var value = ActiveImpactBeat;
                if (value == null || value.Duration <= 0f) return 0f;
                var envelope = 1f - value.Progress;
                envelope *= envelope;
                return Mathf.Clamp(value.Flash * envelope, 0f,
                    CombatImpactBeatCatalog.MaximumFlash);
            }
        }

        public void Clear()
        {
            _drainBuffer.Clear();
            _combatEffects.Clear();
            _reactions.Clear();
            for (var index = _feedback.Count - 1; index >= 0; index--)
                RecycleFeedbackAt(index);
            _audio.Clear();
            _hasActiveImpactBeat = false;
            _impactBeat.Clear();
            _lastChannelTicks.Clear();
            _battleClock = 0f;
            _impactClock = 0f;
            _lastImpactBeatAcceptedAt = float.NegativeInfinity;
            _lastAcceptedBeatRole = CombatImpactBeatRole.None;
            _clusterWindowStartedAt = float.NegativeInfinity;
            _clusterDefeatCount = 0;
            MissingProfileCount = 0;
        }

        private void Route(BattlePresentationEvent value, CombatFeedbackProfile profile)
        {
            if (profile == null) return;
            if (profile.VfxKind != PresentationVfxKind.None
                && PassesRateLimit(profile, FeedbackChannel.Vfx, value.LogicTick))
            {
                AddBounded(_combatEffects, new PresentationCombatEffect
                {
                    Kind = profile.VfxKind,
                    Position = value.Position,
                    Direction = value.Direction,
                    Ttl = profile.Duration,
                    Duration = profile.Duration,
                    Priority = profile.Priority,
                    EventSequence = value.Sequence,
                }, CombatEffectCapacity, effect => effect.Priority);
            }

            if (profile.AttackerRecoil > 0f && value.SourceEntityId != 0)
                AddReaction(value.SourceEntityId, value.Direction, profile,
                    profile.AttackerRecoil, 0f, 0f, 0f);
            if ((profile.TargetFlash > 0f || profile.TargetSquash > 0f
                    || profile.TargetDisplacement > 0f) && value.TargetEntityId != 0
                && !(value.Kind == BattlePresentationEventKind.DamageResolved
                    && value.Defeated))
                AddReaction(value.TargetEntityId, value.Direction, profile,
                    0f, profile.TargetFlash, profile.TargetSquash,
                    profile.TargetDisplacement);

            if (ShouldEmitFloatingText(value, profile.FloatingTextRole))
                AddOrMergeFeedback(value, profile);

            if (profile.AudioRoute != CombatAudioRoute.None
                && PassesRateLimit(profile, FeedbackChannel.Audio, value.LogicTick))
            {
                AddBounded(_audio, new PresentationAudioRequest
                {
                    Route = profile.AudioRoute,
                    Position = value.Position,
                    Ttl = AudioRequestLifetime,
                    Priority = profile.Priority,
                    EventSequence = value.Sequence,
                }, AudioCapacity, audio => audio.Priority);
            }

            RequestImpactBeat(value, profile.BeatRole);
        }

        private void RequestImpactBeat(BattlePresentationEvent value,
            CombatImpactBeatRole role)
        {
            if (role == CombatImpactBeatRole.None) return;
            if (role != CombatImpactBeatRole.Cluster)
            {
                TryAcceptImpactBeat(role, value.Sequence);
                return;
            }

            if (value.Kind != BattlePresentationEventKind.EntityDefeated) return;
            if (_impactClock - _clusterWindowStartedAt
                > CombatImpactBeatCatalog.ClusterWindowSeconds)
            {
                _clusterWindowStartedAt = _impactClock;
                _clusterDefeatCount = 0;
            }
            _clusterDefeatCount += Mathf.Max(1, value.Count);
            if (_clusterDefeatCount < CombatImpactBeatCatalog.ClusterMinimumCount)
                return;
            _clusterWindowStartedAt = _impactClock;
            _clusterDefeatCount = 0;
            TryAcceptImpactBeat(CombatImpactBeatRole.Cluster, value.Sequence);
        }

        private bool TryAcceptImpactBeat(CombatImpactBeatRole role, long eventSequence)
        {
            var insideCooldown = _impactClock - _lastImpactBeatAcceptedAt
                < CombatImpactBeatCatalog.CooldownSeconds;
            if (insideCooldown && role <= _lastAcceptedBeatRole) return false;

            var style = CombatImpactBeatCatalog.Resolve(role);
            _impactBeat.Start(style, eventSequence);
            _hasActiveImpactBeat = true;
            _lastImpactBeatAcceptedAt = _impactClock;
            _lastAcceptedBeatRole = role;
            return true;
        }

        private bool PassesRateLimit(CombatFeedbackProfile profile,
            FeedbackChannel channel,
            int logicTick)
        {
            var key = new RateLimitKey(profile.Id, channel);
            int last;
            var minimumTicks = SecondsToLogicTicks(profile.MinimumInterval);
            if (_lastChannelTicks.TryGetValue(key, out last)
                && logicTick - last < minimumTicks)
                return false;
            _lastChannelTicks[key] = logicTick;
            return true;
        }

        private void AddReaction(int entityId, Vector2 direction,
            CombatFeedbackProfile profile, float recoil, float flash,
            float squash, float displacement)
        {
            AddBounded(_reactions, new PresentationEntityReaction
            {
                EntityId = entityId,
                Direction = direction,
                Recoil = recoil,
                Flash = flash,
                Squash = squash,
                Displacement = displacement,
                Ttl = profile.Duration,
                Duration = profile.Duration,
                Priority = profile.Priority,
            }, ReactionCapacity, reaction => reaction.Priority);
        }

        private void AddOrMergeFeedback(BattlePresentationEvent value,
            CombatFeedbackProfile profile)
        {
            var style = _floatingTextStyles.Resolve(profile.FloatingTextRole);
            if (profile.FloatingTextRole == CombatFloatingTextRole.Defeat)
            {
                for (var defeatIndex = _feedback.Count - 1;
                     defeatIndex >= 0; defeatIndex--)
                {
                    var defeat = _feedback[defeatIndex];
                    if (defeat.Role != CombatFloatingTextRole.Defeat
                        || defeat.LastLogicTick != value.LogicTick)
                        continue;
                    var incomingCount = Mathf.Max(1, value.Count);
                    var totalCount = defeat.Count + incomingCount;
                    var centroid = (defeat.EventPoint * defeat.Count
                        + value.Position * incomingCount) / totalCount;
                    defeat.Count = totalCount;
                    defeat.Magnitude += Mathf.Abs(value.Magnitude);
                    defeat.TargetEntityId = 0;
                    defeat.EventPoint = centroid;
                    defeat.Point = centroid;
                    defeat.FollowElapsed = defeat.FollowDuration;
                    defeat.Ttl = style.Duration;
                    defeat.EventSequence = value.Sequence;
                    defeat.TextDirty = true;
                    return;
                }
            }
            for (var index = _feedback.Count - 1; index >= 0; index--)
            {
                var current = _feedback[index];
                if (current.Kind != value.Kind
                    || current.TargetEntityId != value.TargetEntityId
                    || !string.Equals(current.SemanticId, value.SemanticId,
                        StringComparison.Ordinal)
                    || !string.Equals(current.ProfileId, profile.Id,
                        StringComparison.Ordinal)
                    || current.Role != profile.FloatingTextRole
                    || value.LogicTick - current.LastLogicTick
                        > current.MergeWindowTicks)
                    continue;
                current.Magnitude += Mathf.Abs(value.Magnitude);
                current.Count += Mathf.Max(1, value.Count);
                if (current.FollowDuration <= 0f
                    || current.FollowElapsed < current.FollowDuration)
                {
                    current.EventPoint = value.Position;
                    current.Point = value.Position;
                }
                current.Ttl = Mathf.Max(current.Ttl, style.Duration * .72f);
                current.Duration = style.Duration;
                current.LastLogicTick = value.LogicTick;
                current.EventSequence = value.Sequence;
                current.TextDirty = true;
                return;
            }

            var sameProfileTickCount = CountSameProfileTick(
                profile.Id, value.LogicTick);
            if (sameProfileTickCount
                >= CombatFloatingTextStyleCatalog.SameProfileTickCapacity)
                return;

            var replacement = FindFeedbackReplacement(style.CountsAsOrdinary,
                profile.Priority, value.Sequence);
            if (replacement == -2) return;
            PresentationFeedback feedback;
            if (replacement >= 0)
            {
                feedback = _feedback[replacement];
                _feedback.RemoveAt(replacement);
            }
            else
            {
                feedback = AcquireFeedback();
            }
            feedback.Initialize(value, profile, style, sameProfileTickCount,
                _floatingTextCache.Format(profile.FloatingTextRole,
                    value.Magnitude, Mathf.Max(1, value.Count)));
            _feedback.Add(feedback);
        }

        private int FindFeedbackReplacement(bool incomingOrdinary,
            CombatFeedbackPriority incomingPriority, long incomingSequence)
        {
            if (incomingOrdinary
                && CountOrdinaryFeedback() >= OrdinaryFloatingTextCapacity)
                return LowestFeedbackIndex(true, incomingPriority, incomingSequence);
            if (_feedback.Count >= FloatingTextCapacity)
                return LowestFeedbackIndex(false, incomingPriority, incomingSequence);
            return -1;
        }

        private int LowestFeedbackIndex(bool ordinaryOnly,
            CombatFeedbackPriority incomingPriority, long incomingSequence)
        {
            var selected = -1;
            for (var index = 0; index < _feedback.Count; index++)
            {
                var candidate = _feedback[index];
                if (ordinaryOnly
                    && !_floatingTextStyles.Resolve(candidate.Role).CountsAsOrdinary)
                    continue;
                if (selected < 0
                    || candidate.Priority < _feedback[selected].Priority
                    || candidate.Priority == _feedback[selected].Priority
                    && candidate.EventSequence < _feedback[selected].EventSequence)
                    selected = index;
            }
            if (selected < 0) return -2;
            var current = _feedback[selected];
            if (incomingPriority < current.Priority) return -2;
            if (incomingPriority == current.Priority
                && incomingSequence < current.EventSequence) return -2;
            return selected;
        }

        private int CountOrdinaryFeedback()
        {
            var count = 0;
            for (var index = 0; index < _feedback.Count; index++)
                if (_floatingTextStyles.Resolve(_feedback[index].Role).CountsAsOrdinary)
                    count++;
            return count;
        }

        private int CountSameProfileTick(string profileId, int logicTick)
        {
            var count = 0;
            for (var index = 0; index < _feedback.Count; index++)
            {
                var feedback = _feedback[index];
                if (feedback.LastLogicTick == logicTick
                    && string.Equals(feedback.ProfileId, profileId,
                        StringComparison.Ordinal))
                    count++;
            }
            return count;
        }

        private PresentationFeedback AcquireFeedback()
        {
            if (_feedbackPool.Count > 0) return _feedbackPool.Pop();
            _allocatedFeedbackCount++;
            return new PresentationFeedback();
        }

        private void RefreshDirtyFeedbackText()
        {
            for (var index = 0; index < _feedback.Count; index++)
            {
                var feedback = _feedback[index];
                if (!feedback.TextDirty) continue;
                feedback.Text = _floatingTextCache.Format(
                    feedback.Role, feedback.Magnitude, feedback.Count);
                feedback.TextDirty = false;
            }
        }

        private void RecycleFeedbackAt(int index)
        {
            var feedback = _feedback[index];
            _feedback.RemoveAt(index);
            feedback.Reset();
            if (_feedbackPool.Count < FloatingTextCapacity)
                _feedbackPool.Push(feedback);
        }

        private static bool ShouldEmitFloatingText(BattlePresentationEvent value,
            CombatFloatingTextRole role)
        {
            if (role == CombatFloatingTextRole.None) return false;
            if (value.Kind == BattlePresentationEventKind.DamageResolved
                && value.Defeated) return false;
            return role == CombatFloatingTextRole.Control
                || role == CombatFloatingTextRole.Defeat
                || Mathf.Abs(value.Magnitude) > .0001f;
        }

        private static void AddBounded<T>(List<T> list, T value, int capacity,
            Func<T, CombatFeedbackPriority> priority)
        {
            if (list.Count < capacity)
            {
                list.Add(value);
                return;
            }
            var replacement = 0;
            var lowest = priority(list[0]);
            for (var index = 1; index < list.Count; index++)
            {
                var candidate = priority(list[index]);
                if (candidate >= lowest) continue;
                lowest = candidate;
                replacement = index;
            }
            if (priority(value) < lowest) return;
            list.RemoveAt(replacement);
            list.Add(value);
        }

        internal static int SecondsToLogicTicks(float seconds)
        {
            return seconds <= 0f
                ? 0
                : Mathf.Max(1, Mathf.CeilToInt(seconds / GameSimulation.FixedStepSeconds
                    - .000001f));
        }

        private void AdvanceEffects(float delta)
        {
            for (var index = _combatEffects.Count - 1; index >= 0; index--)
            {
                _combatEffects[index].Ttl -= delta;
                if (_combatEffects[index].Ttl <= 0f) _combatEffects.RemoveAt(index);
            }
        }

        private void AdvanceReactions(float delta)
        {
            for (var index = _reactions.Count - 1; index >= 0; index--)
            {
                _reactions[index].Ttl -= delta;
                if (_reactions[index].Ttl <= 0f) _reactions.RemoveAt(index);
            }
        }

        private void AdvanceFeedback(float delta)
        {
            for (var index = _feedback.Count - 1; index >= 0; index--)
            {
                var feedback = _feedback[index];
                feedback.AdvanceFollow(delta);
                feedback.Ttl -= delta;
                if (feedback.Ttl <= 0f) RecycleFeedbackAt(index);
            }
        }

        private void AdvanceAudio(float delta)
        {
            for (var index = _audio.Count - 1; index >= 0; index--)
            {
                _audio[index].Ttl -= delta;
                if (_audio[index].Ttl <= 0f) _audio.RemoveAt(index);
            }
        }

        private void AdvanceImpactBeat(float unscaledDelta)
        {
            if (!_hasActiveImpactBeat) return;
            if (_impactBeat.Advance(unscaledDelta)) return;
            _hasActiveImpactBeat = false;
            _impactBeat.Clear();
        }
    }
}
