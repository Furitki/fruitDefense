using System;
using System.Collections.Generic;
using System.IO;
using FruitDefense.Core;
using FruitDefense.Presentation;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class CombatFeedbackImpactRhythmSmoke
    {
        private const string NormalId = "ability.test.rhythm.normal";
        private const string PeriodicId = "ability.test.rhythm.periodic";
        private const string ResourceId = "resource.test.rhythm";
        private const string StatusId = "status.test.rhythm";
        private const string HeavyId = "ability.test.rhythm.heavy";
        private const string OrdinaryEnemyId = "enemy.test.rhythm";
        private const string BossEnemyId = "enemy.test.rhythm.boss";

        public static void Run()
        {
            ValidateFiniteBeatCatalogAndLegacyRemoval();
            ValidateOrdinaryEventsDoNotShake();
            ValidateClusterPromotionAndSemanticWinner();
            ValidateRealTimeCooldownPauseAndSpeed();
            ValidateBoundedLowCycleEnvelopeAndGeometry();
            Debug.Log("FRUIT_DEFENSE_COMBAT_FEEDBACK_IMPACT_RHYTHM_OK");
        }

        private static void ValidateFiniteBeatCatalogAndLegacyRemoval()
        {
            Assert(Mathf.Approximately(CombatImpactBeatCatalog.CooldownSeconds, .16f)
                && Mathf.Approximately(CombatImpactBeatCatalog.ClusterWindowSeconds, .12f)
                && CombatImpactBeatCatalog.ClusterMinimumCount == 3
                && Mathf.Approximately(CombatImpactBeatCatalog.MaximumAmplitude, 3f),
                "impact beats use the reviewed real-time cooldown, cluster, and amplitude limits");
            foreach (CombatImpactBeatRole role in Enum.GetValues(
                         typeof(CombatImpactBeatRole)))
            {
                var style = CombatImpactBeatCatalog.Resolve(role);
                Assert(style.Role == role
                    && style.Amplitude >= 0f
                    && style.Amplitude <= CombatImpactBeatCatalog.MaximumAmplitude
                    && style.Duration >= 0f
                    && style.Duration <= CombatImpactBeatCatalog.MaximumDuration
                    && style.Flash >= 0f
                    && style.Flash <= CombatImpactBeatCatalog.MaximumFlash
                    && style.Oscillations >= 0f && style.Oscillations <= 2f,
                    "beat role has finite bounded catalog values: " + role);
            }

            var presentationType = typeof(BattlePresentationBuffer);
            Assert(presentationType.GetProperty("SurfaceMotion") == null
                && presentationType.GetField("SurfaceMotionCapacity") == null
                && presentationType.Assembly.GetType(
                    "FruitDefense.Presentation.PresentationSurfaceMotion") == null,
                "additive surface-motion compatibility types are absent");
            var source = File.ReadAllText(Path.Combine(Application.dataPath,
                "Scripts", "Presentation", "BattlePresentationBuffer.cs"));
            Assert(!source.Contains("_surfaceMotion")
                && !source.Contains("ShakeAmplitude")
                && !source.Contains("ShakeDuration"),
                "the scheduler has no raw or additive shake path");
        }

        private static void ValidateOrdinaryEventsDoNotShake()
        {
            var stream = new BattlePresentationEventStream(32);
            for (var index = 0; index < 8; index++)
            {
                stream.EmitDamageResolved(index, NormalId, string.Empty,
                    "plant.test", OrdinaryEnemyId, 1, 100 + index,
                    Vector2.one * index, Vector2.right, 2f, false);
                stream.EmitDamageResolved(index, PeriodicId, string.Empty,
                    "plant.test", OrdinaryEnemyId, 1, 200 + index,
                    Vector2.one * index, Vector2.right, 1f, false);
            }
            stream.EmitResourceGranted(9, string.Empty, ResourceId,
                1, 1, Vector2.zero, 15f);
            stream.EmitStatusApplied(10, string.Empty, StatusId,
                1, 2, Vector2.zero, Vector2.right, 1f);
            var buffer = new BattlePresentationBuffer(CreateCatalog());
            Drain(stream, buffer);
            Assert(buffer.ActiveImpactBeat == null
                && buffer.BattlefieldOffset == Vector2.zero
                && Mathf.Approximately(buffer.BattlefieldFlash, 0f),
                "normal, periodic, resource, and ordinary status events request no beat");

            var isolated = new BattlePresentationEventStream();
            isolated.EmitEntityDefeated(11, string.Empty, OrdinaryEnemyId,
                1, 300, Vector2.zero, Vector2.right, 0f);
            Drain(isolated, buffer);
            Assert(buffer.ActiveImpactBeat == null,
                "an isolated ordinary-enemy defeat does not shake");
        }

        private static void ValidateClusterPromotionAndSemanticWinner()
        {
            var buffer = new BattlePresentationBuffer(CreateCatalog());
            var stream = new BattlePresentationEventStream(16);
            stream.EmitEntityDefeated(1, string.Empty, OrdinaryEnemyId,
                1, 10, new Vector2(0f, 0f), Vector2.right, 0f);
            stream.EmitEntityDefeated(1, string.Empty, OrdinaryEnemyId,
                1, 11, new Vector2(3f, 0f), Vector2.right, 0f);
            Drain(stream, buffer);
            Assert(buffer.ActiveImpactBeat == null,
                "fewer than three compact defeats do not promote a cluster beat");

            stream.EmitEntityDefeated(1, string.Empty, OrdinaryEnemyId,
                1, 12, new Vector2(0f, 3f), Vector2.right, 0f);
            Drain(stream, buffer);
            var cluster = buffer.ActiveImpactBeat;
            Assert(cluster != null && cluster.Role == CombatImpactBeatRole.Cluster,
                "the third compact defeat promotes exactly one cluster beat");
            var clusterSequence = cluster.EventSequence;

            stream.EmitDamageResolved(2, HeavyId, string.Empty,
                "plant.test", OrdinaryEnemyId, 1, 20,
                Vector2.zero, Vector2.right, 20f, false);
            Drain(stream, buffer);
            Assert(buffer.ActiveImpactBeat != null
                && buffer.ActiveImpactBeat.EventSequence == clusterSequence,
                "lower-priority Heavy cannot restart or replace an active Cluster beat");

            stream.EmitEntityDefeated(2, string.Empty, BossEnemyId,
                1, 21, Vector2.zero, Vector2.right, 0f);
            Drain(stream, buffer);
            Assert(buffer.ActiveImpactBeat != null
                && buffer.ActiveImpactBeat.Role == CombatImpactBeatRole.Terminal
                && buffer.ActiveImpactBeat.EventSequence != clusterSequence,
                "Terminal strictly replaces Cluster inside the cooldown without adding");
            Assert(buffer.BattlefieldOffset.magnitude
                    <= CombatImpactBeatCatalog.MaximumAmplitude + .0001f,
                "semantic replacement remains bounded by one catalog amplitude");
        }

        private static void ValidateRealTimeCooldownPauseAndSpeed()
        {
            var one = new BattlePresentationBuffer(CreateCatalog());
            var two = new BattlePresentationBuffer(CreateCatalog());
            var oneStream = new BattlePresentationEventStream();
            var twoStream = new BattlePresentationEventStream();
            EmitHeavy(oneStream, 1, 10);
            EmitHeavy(twoStream, 1, 10);
            Drain(oneStream, one);
            Drain(twoStream, two);
            var firstOneSequence = one.ActiveImpactBeat.EventSequence;
            var firstTwoSequence = two.ActiveImpactBeat.EventSequence;

            one.Advance(.08f, true, 1);
            two.Advance(.08f, true, 2);
            Assert(Mathf.Approximately(one.ImpactClock, 0f)
                && Mathf.Approximately(two.ImpactClock, 0f)
                && Mathf.Approximately(one.ActiveImpactBeat.Ttl,
                    one.ActiveImpactBeat.Duration)
                && Mathf.Approximately(two.ActiveImpactBeat.Ttl,
                    two.ActiveImpactBeat.Duration),
                "pause freezes active beats and the unscaled cooldown at both speeds");

            one.Advance(.159f, false, 1);
            two.Advance(.159f, false, 2);
            EmitHeavy(oneStream, 2, 11);
            EmitHeavy(twoStream, 2, 11);
            Drain(oneStream, one);
            Drain(twoStream, two);
            Assert(one.ActiveImpactBeat.EventSequence == firstOneSequence
                && two.ActiveImpactBeat.EventSequence == firstTwoSequence
                && Mathf.Approximately(one.ImpactClock, two.ImpactClock),
                "equal Heavy requests at 0.159 real seconds are rejected at 1x and 2x");

            one.Advance(.0011f, false, 1);
            two.Advance(.0011f, false, 2);
            EmitHeavy(oneStream, 3, 12);
            EmitHeavy(twoStream, 3, 12);
            Drain(oneStream, one);
            Drain(twoStream, two);
            Assert(one.ActiveImpactBeat != null && two.ActiveImpactBeat != null
                && one.ActiveImpactBeat.EventSequence != firstOneSequence
                && two.ActiveImpactBeat.EventSequence != firstTwoSequence
                && one.ActiveImpactBeat.EventSequence == two.ActiveImpactBeat.EventSequence,
                "the 0.16-second boundary admits the same beat identity independent of battle speed");
        }

        private static void ValidateBoundedLowCycleEnvelopeAndGeometry()
        {
            var buffer = new BattlePresentationBuffer(CreateCatalog());
            var stream = new BattlePresentationEventStream();
            stream.EmitEntityDefeated(1, string.Empty, BossEnemyId,
                1, 99, Vector2.zero, Vector2.right, 0f);
            Drain(stream, buffer);
            Assert(buffer.ActiveImpactBeat != null
                && buffer.ActiveImpactBeat.Oscillations <= 2f,
                "terminal motion declares no more than two visible oscillations");

            var layout = new BattleUiLayout(GameConfig.DefaultBattlefield);
            var header = layout.Header;
            var pause = layout.PauseAction;
            var speed = layout.SpeedAction;
            var board = layout.Board;
            var sampleCell = GameConfig.DefaultBattlefield.PlantableCells[0];
            var potHit = layout.Battlefield.PotHitRect(sampleCell);
            var maximum = 0f;
            var zeroCrossings = 0;
            var previousSign = 0;
            for (var sample = 0; sample < 96; sample++)
            {
                var offset = buffer.BattlefieldOffset;
                Assert(Finite(offset), "analytic beat offset stays finite");
                maximum = Mathf.Max(maximum, offset.magnitude);
                var sign = Mathf.Abs(offset.x) <= .0001f ? previousSign
                    : offset.x < 0f ? -1 : 1;
                if (previousSign != 0 && sign != previousSign) zeroCrossings++;
                previousSign = sign;
                buffer.Advance(CombatImpactBeatCatalog.MaximumDuration / 96f,
                    false, 2);
            }
            Assert(maximum <= 3.0001f && zeroCrossings <= 4,
                "one damped low-cycle beat stays within 3 pixels and four x-axis crossings");
            Assert(layout.Header == header && layout.PauseAction == pause
                && layout.SpeedAction == speed && layout.Board == board
                && layout.Battlefield.PotHitRect(sampleCell) == potHit,
                "battlefield shake cannot mutate HUD or authoritative hit geometry");
        }

        private static CombatFeedbackCatalog CreateCatalog()
        {
            var keys = new[]
            {
                new CombatFeedbackKey(BattlePresentationEventKind.DamageResolved, NormalId),
                new CombatFeedbackKey(BattlePresentationEventKind.DamageResolved, PeriodicId),
                new CombatFeedbackKey(BattlePresentationEventKind.ResourceGranted, ResourceId),
                new CombatFeedbackKey(BattlePresentationEventKind.StatusApplied, StatusId),
                new CombatFeedbackKey(BattlePresentationEventKind.DamageResolved, HeavyId),
                new CombatFeedbackKey(BattlePresentationEventKind.EntityDefeated, OrdinaryEnemyId),
                new CombatFeedbackKey(BattlePresentationEventKind.EntityDefeated, BossEnemyId),
            };
            var catalog = new CombatFeedbackCatalog(keys);
            Declare(catalog, keys[0], "feedback.test.normal",
                CombatFeedbackPriority.Light, CombatFloatingTextRole.NormalDamage,
                CombatImpactBeatRole.None);
            Declare(catalog, keys[1], "feedback.test.periodic",
                CombatFeedbackPriority.Ambient, CombatFloatingTextRole.PeriodicDamage,
                CombatImpactBeatRole.None);
            Declare(catalog, keys[2], "feedback.test.resource",
                CombatFeedbackPriority.Medium, CombatFloatingTextRole.Resource,
                CombatImpactBeatRole.None);
            Declare(catalog, keys[3], "feedback.test.status",
                CombatFeedbackPriority.Medium, CombatFloatingTextRole.None,
                CombatImpactBeatRole.None);
            Declare(catalog, keys[4], "feedback.test.heavy",
                CombatFeedbackPriority.Heavy, CombatFloatingTextRole.HeavyDamage,
                CombatImpactBeatRole.Heavy);
            Declare(catalog, keys[5], "feedback.test.cluster",
                CombatFeedbackPriority.Defeat, CombatFloatingTextRole.Defeat,
                CombatImpactBeatRole.Cluster);
            Declare(catalog, keys[6], "feedback.test.terminal",
                CombatFeedbackPriority.Defeat, CombatFloatingTextRole.Defeat,
                CombatImpactBeatRole.Terminal);
            return catalog;
        }

        private static void Declare(CombatFeedbackCatalog catalog,
            CombatFeedbackKey key, string id, CombatFeedbackPriority priority,
            CombatFloatingTextRole role, CombatImpactBeatRole beatRole)
        {
            catalog.Declare(key, CombatFeedbackCatalogEntry.Concrete(
                new CombatFeedbackProfile(id, PresentationVfxKind.None,
                    priority, .62f, floatingTextRole: role,
                    mergeWindow: role == CombatFloatingTextRole.Defeat ? 0f : .12f,
                    beatRole: beatRole)));
        }

        private static void EmitHeavy(BattlePresentationEventStream stream,
            int tick, int targetId)
        {
            stream.EmitDamageResolved(tick, HeavyId, string.Empty,
                "plant.test", OrdinaryEnemyId, 1, targetId,
                Vector2.zero, Vector2.right, 20f, false);
        }

        private static void Drain(BattlePresentationEventStream stream,
            BattlePresentationBuffer destination)
        {
            var events = new List<BattlePresentationEvent>();
            stream.DrainTo(events);
            destination.Consume(events);
        }

        private static bool Finite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Combat feedback impact-rhythm smoke failed: " + message);
        }
    }
}
