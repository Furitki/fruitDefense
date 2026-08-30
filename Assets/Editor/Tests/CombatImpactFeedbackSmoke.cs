using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Presentation;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class CombatImpactFeedbackSmoke
    {
        public static void Run()
        {
            ValidateSemanticContractAndBundledCatalog();
            ValidateFloatingTextLanguageAndDensity();
            ValidateMergeRateLimitClockAndCaps();
            ValidateInterpolationAndSafeSnap();
            Debug.Log("FRUIT_DEFENSE_COMBAT_IMPACT_FEEDBACK_OK");
        }

        private static void ValidateFloatingTextLanguageAndDensity()
        {
            var styles = CombatFloatingTextStyleCatalog.CreateBundled();
            var styleIssues = styles.Validate();
            Assert(styleIssues.Count == 0 && styles.Count == 6,
                "floating-text style catalog declares six valid semantic roles: "
                + string.Join(", ", styleIssues));
            var normal = styles.Resolve(CombatFloatingTextRole.NormalDamage);
            var heavy = styles.Resolve(CombatFloatingTextRole.HeavyDamage);
            var periodic = styles.Resolve(CombatFloatingTextRole.PeriodicDamage);
            Assert(normal.FontSize >= 16 && periodic.FontSize >= 14
                && heavy.FontSize > normal.FontSize
                && CombatFloatingTextStyleCatalog.ContrastRatio(
                    normal.FillColor,
                    CombatFloatingTextStyleCatalog.SharedOutlineColor) >= 3f,
                "role typography and fill/outline contrast satisfy the reference floor");
            var normalStart = CombatFloatingTextStyleCatalog.Sample(normal, 0f);
            var normalQuarter = CombatFloatingTextStyleCatalog.Sample(normal, .25f);
            var normalHalf = CombatFloatingTextStyleCatalog.Sample(normal, .5f);
            var heavyStart = CombatFloatingTextStyleCatalog.Sample(heavy, 0f);
            var normalEnd = CombatFloatingTextStyleCatalog.Sample(normal, 1f);
            Assert(heavyStart.Scale > normalStart.Scale
                && normalStart.Opacity > .99f
                && normalQuarter.OffsetY < -normal.RiseDistance * .43f
                && normalQuarter.Opacity > normalHalf.Opacity
                && normalHalf.Opacity > .49f
                && normalEnd.OffsetY < -normal.RiseDistance * .99f
                && normalEnd.Opacity <= .001f,
                "analytic motion rises immediately and fades gradually across its lifetime");
            Assert(CombatFloatingTextStyleCatalog.AtlasFrameTimeGateMilliseconds == .5f
                && CombatFloatingTextStyleCatalog.AtlasAllocationGateBytesPerSecond == 1024
                && CombatFloatingTextStyleCatalog.RuntimeGlyphInventory.Contains("击败×")
                && CombatFloatingTextStyleCatalog.RuntimeGlyphInventory.Contains("0123456789"),
                "the reviewed finite glyph inventory retains the WebGL performance gates");
            Assert(CombatFloatingTextStyleCatalog.SemanticLaneOffset(
                    CombatFloatingTextRole.NormalDamage) == Vector2.zero
                && CombatFloatingTextStyleCatalog.SemanticLaneOffset(
                    CombatFloatingTextRole.Defeat).y
                    <= -CombatFloatingTextStyleCatalog.TerminalLaneDistance,
                "terminal copy owns a dedicated upward semantic lane");
            var gameSource = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            Assert(!gameSource.Contains("RequestCharactersInTexture")
                && !gameSource.Contains("_floatingTextFillStyles")
                && !gameSource.Contains("_floatingTextOutlineStyles")
                && !gameSource.Contains("DrawCombatFloatingText")
                && !gameSource.Contains("GUIUtility.ScaleAroundPivot"),
                "legacy glyph requests, duplicate GUIStyles, and five-layer IMGUI text are absent");

            var pea = PeaBuffer();
            Assert(pea.Feedback.Count == 1
                && pea.Feedback[0].Role == CombatFloatingTextRole.NormalDamage
                && pea.Feedback[0].Text == "-5",
                "normal damage uses cached signed numeric copy");

            var semanticStream = new BattlePresentationEventStream();
            semanticStream.EmitStatusProcced(1, string.Empty,
                BattleContentIds.Statuses.IceFreeze, 1, 10,
                Vector2.zero, Vector2.right, 0f);
            semanticStream.EmitResourceGranted(1,
                BattleContentIds.Abilities.SunflowerProduce,
                CombatFeedbackCatalog.SunResource, 2, 2,
                Vector2.one, 15f);
            semanticStream.EmitEntityDefeated(1, string.Empty,
                BattleContentIds.Enemies.Normal, 1, 20,
                Vector2.one, Vector2.right, 0f);
            var semantic = new BattlePresentationBuffer();
            Drain(semanticStream, semantic);
            Assert(semantic.Feedback.Any(value => value.Role
                       == CombatFloatingTextRole.Control && value.Text == "冻结")
                && semantic.Feedback.Any(value => value.Role
                       == CombatFloatingTextRole.Resource
                    && value.Text == "+15 阳光")
                && semantic.Feedback.Any(value => value.Role
                       == CombatFloatingTextRole.Defeat && value.Text == "击败"),
                "control, resource, and defeat use non-color semantic copy");

            var denseKey = new CombatFeedbackKey(
                BattlePresentationEventKind.DamageResolved, "ability.test.dense");
            var defeatKey = new CombatFeedbackKey(
                BattlePresentationEventKind.EntityDefeated, "enemy.normal");
            var denseCatalog = new CombatFeedbackCatalog(new[] { denseKey, defeatKey });
            denseCatalog.Declare(denseKey, CombatFeedbackCatalogEntry.Concrete(
                FloatingOnly("feedback.test.dense", 0f)));
            denseCatalog.Declare(defeatKey, CombatFeedbackCatalogEntry.Concrete(
                new CombatFeedbackProfile("feedback.test.dense-defeat",
                    PresentationVfxKind.None, CombatFeedbackPriority.Defeat, 0f,
                    floatingTextRole: CombatFloatingTextRole.Defeat,
                    mergeWindow: 0f)));
            var denseBuffer = new BattlePresentationBuffer(denseCatalog, styles);
            var denseStream = new BattlePresentationEventStream(512);
            for (var index = 0; index < 300; index++)
                denseStream.EmitDamageResolved(index, "ability.test.dense",
                    string.Empty, "plant.test", "enemy.normal", 1, 1000 + index,
                    new Vector2(index % 6, index % 4), Vector2.right, 1f, false);
            denseStream.EmitEntityDefeated(301, string.Empty, "enemy.normal",
                1, 9999, Vector2.zero, Vector2.right, 0f);
            Drain(denseStream, denseBuffer);
            Assert(denseBuffer.Feedback.Count <= CombatFloatingTextStyleCatalog.TotalCapacity
                && denseBuffer.OrdinaryFeedbackCount
                    <= CombatFloatingTextStyleCatalog.OrdinaryCapacity
                && denseBuffer.Feedback.Any(value => value.Role
                    == CombatFloatingTextRole.Defeat),
                "mowing-style bursts enforce ordinary and total admission budgets");
            Assert(denseBuffer.Feedback.All(value => value.VisualLane >= 0
                    && value.VisualLane < CombatFloatingTextStyleCatalog.VisualLaneCount),
                "admitted records use finite deterministic visual lanes");

            var burstStream = new BattlePresentationEventStream(64);
            for (var index = 0; index < 30; index++)
                burstStream.EmitDamageResolved(400, "ability.test.dense",
                    string.Empty, "plant.test", "enemy.normal", 1, 7000 + index,
                    new Vector2(index, 0f), Vector2.right, 1f, false);
            var burstBuffer = new BattlePresentationBuffer(denseCatalog, styles);
            Drain(burstStream, burstBuffer);
            Assert(burstBuffer.Feedback.Count
                    == CombatFloatingTextStyleCatalog.SameProfileTickCapacity
                && burstBuffer.Feedback.Select(value => value.VisualLane)
                    .OrderBy(value => value).SequenceEqual(new[] { 0, 1, 2 }),
                "one area-impact profile keeps at most three readable labels per tick");

            var defeatBurstStream = new BattlePresentationEventStream(16);
            for (var index = 0; index < 5; index++)
                defeatBurstStream.EmitEntityDefeated(500, string.Empty, "enemy.normal",
                    1, 8000 + index, new Vector2(index, 0f), Vector2.right, 0f);
            var defeatBurstBuffer = new BattlePresentationBuffer(denseCatalog, styles);
            Drain(defeatBurstStream, defeatBurstBuffer);
            Assert(defeatBurstBuffer.Feedback.Count == 1
                && defeatBurstBuffer.Feedback[0].Role == CombatFloatingTextRole.Defeat
                && defeatBurstBuffer.Feedback[0].Count == 5
                && defeatBurstBuffer.Feedback[0].Text == "击败×5",
                "same-tick defeats collapse into one semantic tally");

            var warmEvents = BuildDenseEvents(12);
            var warmBuffer = new BattlePresentationBuffer(denseCatalog, styles);
            warmBuffer.Consume(warmEvents);
            warmBuffer.Advance(2f, false, 1);
            warmBuffer.Consume(warmEvents);
            warmBuffer.Advance(2f, false, 1);
            var allocatedRecords = warmBuffer.AllocatedFeedbackCount;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var iteration = 0; iteration < 100; iteration++)
            {
                warmBuffer.Consume(warmEvents);
                warmBuffer.Advance(2f, false, 1);
            }
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert(warmBuffer.AllocatedFeedbackCount == allocatedRecords
                && warmBuffer.PooledFeedbackCount == allocatedRecords
                && allocatedBytes <= 4096,
                "warm dense floating channel reuses records within allocation budget: bytes="
                + allocatedBytes);
        }

        private static void ValidateSemanticContractAndBundledCatalog()
        {
            var forbidden = new[]
            {
                "cue", "visual", "color", "duration", "audio", "shake",
                "combateffect", "text",
            };
            var names = typeof(BattlePresentationEvent).GetProperties()
                .Select(property => property.Name.ToLowerInvariant()).ToArray();
            Assert(names.All(name => forbidden.All(value => !name.Contains(value))),
                "semantic events contain no rendering policy");

            var catalog = CombatFeedbackCatalog.CreateBundled();
            var compiled = Compile();
            var coverage = catalog.ValidateCoverage(compiled);
            Assert(coverage.Count == 0,
                "compiled bundled emission closure has an explicit profile or None policy: "
                + string.Join(", ", coverage));
            string productionReason;
            Assert(FruitDefenseGame.ValidateCombatFeedbackCatalog(
                    catalog, compiled, out productionReason),
                "production initialization accepts the bundled feedback closure: "
                + productionReason);
            Assert(!FruitDefenseGame.ValidateCombatFeedbackCatalog(
                    new CombatFeedbackCatalog(Array.Empty<CombatFeedbackKey>()),
                    compiled, out productionReason)
                && productionReason.StartsWith("missing-emittable-policy:",
                    StringComparison.Ordinal),
                "production initialization rejects a missing profile/None policy");
            foreach (var id in new[]
            {
                "ability.plant.pea.attack", "ability.plant.watermelon.attack",
                "ability.plant.banana.attack", "ability.plant.durian.attack",
                "ability.plant.sunflower.produce",
                "ability.equipment.ice.on-hit", "ability.equipment.chili.on-hit",
            })
            {
                Assert(catalog.CanResolve(new CombatFeedbackKey(
                        BattlePresentationEventKind.AbilityReleased, id)),
                    "bundled catalog declares " + id);
            }

            var gatlingStream = new BattlePresentationEventStream();
            gatlingStream.EmitAbilityReleased(1, BattleContentIds.Abilities.PeaAttack,
                1, 2, Vector2.zero, Vector2.right,
                BattleContentIds.Equipment.Gatling);
            var gatling = new BattlePresentationBuffer(catalog);
            Drain(gatlingStream, gatling);
            Assert(gatling.CombatEffects.Count == 1
                && gatling.CombatEffects[0].Kind == PresentationVfxKind.GatlingMuzzle,
                "gatling feedback is selected by the real equipment discriminator");

            var stream = new BattlePresentationEventStream(2);
            var first = stream.EmitAbilityStarted(1, "ability.plant.pea.attack",
                1, 2, Vector2.zero, Vector2.right);
            var second = stream.EmitDamageResolved(2, "ability.plant.pea.attack",
                "projectile.pea", "plant.pea", "enemy.normal", 1, 2,
                Vector2.one, Vector2.right, 12f, false);
            var third = stream.EmitEntityDefeated(2, "ability.plant.pea.attack",
                "enemy.normal", 1, 2, Vector2.one, Vector2.right, 4f);
            Assert(stream.PendingCount == 2 && stream.DroppedCount == 1,
                "semantic event stream remains bounded");
            var drained = new List<BattlePresentationEvent>();
            stream.DrainTo(drained);
            Assert(drained.Count == 2 && drained[0] == second && drained[1] == third
                && first.Sequence < second.Sequence && second.Sequence < third.Sequence,
                "semantic event delivery remains ordered and single-consumption");
            Assert(stream.DrainTo(drained) == 0,
                "semantic event delivery is destructive");
        }

        private static void ValidateMergeRateLimitClockAndCaps()
        {
            var burnStream = new BattlePresentationEventStream();
            var burn = new BattlePresentationBuffer();
            burnStream.EmitDamageResolved(1, string.Empty, string.Empty,
                BattleContentIds.Statuses.ChiliBurn,
                BattleContentIds.Enemies.Normal, 1, 10, Vector2.zero,
                Vector2.right, 3f, false);
            burnStream.EmitDamageResolved(2, string.Empty, string.Empty,
                BattleContentIds.Statuses.ChiliBurn,
                BattleContentIds.Enemies.Normal, 1, 10, Vector2.zero,
                Vector2.right, 4f, false);
            Drain(burnStream, burn);
            Assert(burn.Feedback.Count == 1 && Mathf.Approximately(
                    burn.Feedback[0].Magnitude, 7f) && burn.Feedback[0].Count == 2,
                "burn floating text merges inside its battle-time window");
            Assert(burn.CombatEffects.Count == 1 && burn.AudioRequests.Count == 1,
                "light VFX and audio respect their minimum interval");
            burnStream.EmitDamageResolved(10, string.Empty, string.Empty,
                BattleContentIds.Statuses.ChiliBurn, BattleContentIds.Enemies.Normal,
                1, 10, Vector2.zero, Vector2.right, 2f, false);
            Drain(burnStream, burn);
            Assert(burn.Feedback.Count == 2,
                "floating-text merge window is measured by authoritative logic ticks");

            var ttl = burn.CombatEffects[0].Ttl;
            var clock = burn.BattleClock;
            burn.Advance(.1f, true, 1);
            Assert(Mathf.Approximately(burn.CombatEffects[0].Ttl, ttl)
                && Mathf.Approximately(burn.BattleClock, clock),
                "pause freezes every combat-feedback channel");
            burn.Advance(.1f, false, 1);
            Assert(Mathf.Approximately(burn.BattleClock, .1f)
                && burn.CombatEffects.Count == 2,
                "feedback lifetime advances by local unscaled presentation time");

            var oneX = PeaBuffer();
            var twoX = PeaBuffer();
            oneX.Advance(.1f, false, 1);
            twoX.Advance(.1f, false, 2);
            Assert(oneX.CombatEffects.Count == 1 && twoX.CombatEffects.Count == 1
                && twoX.CombatEffects[0].Ttl < oneX.CombatEffects[0].Ttl
                && Mathf.Approximately(BattlePresentationBuffer.DisplayClockScale(2), 1.25f),
                "2x presentation stays responsive without halving real reading time");

            var baseVariantKey = new CombatFeedbackKey(
                BattlePresentationEventKind.DamageResolved, "ability.test.variant");
            var equipmentVariantKey = new CombatFeedbackKey(
                BattlePresentationEventKind.DamageResolved, "ability.test.variant",
                BattleContentIds.Equipment.Gatling);
            var variants = new CombatFeedbackCatalog(
                new[] { baseVariantKey, equipmentVariantKey });
            variants.Declare(baseVariantKey, CombatFeedbackCatalogEntry.Concrete(
                FloatingOnly("feedback.test.variant.base", .2f)));
            variants.Declare(equipmentVariantKey, CombatFeedbackCatalogEntry.Concrete(
                FloatingOnly("feedback.test.variant.gatling", .2f)));
            var variantStream = new BattlePresentationEventStream();
            variantStream.EmitDamageResolved(1, "ability.test.variant", string.Empty,
                "plant.test", "enemy.normal", 1, 7, Vector2.zero,
                Vector2.right, 2f, false);
            variantStream.EmitDamageResolved(2, "ability.test.variant", string.Empty,
                "plant.test", "enemy.normal", 1, 7, Vector2.zero,
                Vector2.right, 3f, false, BattleContentIds.Equipment.Gatling);
            var variantBuffer = new BattlePresentationBuffer(variants);
            Drain(variantStream, variantBuffer);
            Assert(variantBuffer.Feedback.Count == 2,
                "floating-text merge key includes the resolved feedback profile");

            var lightKey = new CombatFeedbackKey(
                BattlePresentationEventKind.DamageResolved, "ability.test.light");
            var defeatKey = new CombatFeedbackKey(
                BattlePresentationEventKind.EntityDefeated, "enemy.boss");
            var catalog = new CombatFeedbackCatalog(new[] { lightKey, defeatKey });
            catalog.Declare(lightKey, CombatFeedbackCatalogEntry.Concrete(
                new CombatFeedbackProfile("feedback.test.light",
                    PresentationVfxKind.PeaImpact, CombatFeedbackPriority.Light, 10f,
                    targetFlash: .5f,
                    floatingTextRole: CombatFloatingTextRole.NormalDamage,
                    audioRoute: CombatAudioRoute.LightImpact,
                    beatRole: CombatImpactBeatRole.None)));
            catalog.Declare(defeatKey, CombatFeedbackCatalogEntry.Concrete(
                new CombatFeedbackProfile("feedback.test.defeat",
                    PresentationVfxKind.Defeat, CombatFeedbackPriority.Defeat, 10f,
                    targetFlash: 1f,
                    floatingTextRole: CombatFloatingTextRole.Defeat,
                    audioRoute: CombatAudioRoute.Defeat,
                    beatRole: CombatImpactBeatRole.Terminal)));
            var bounded = new BattlePresentationBuffer(catalog);
            var dense = new BattlePresentationEventStream(256);
            BattlePresentationEvent latestLight = null;
            for (var index = 0; index < 80; index++)
            {
                latestLight = dense.EmitDamageResolved(index, "ability.test.light", string.Empty,
                    "plant.test", "enemy.normal", 1, 100 + index,
                    new Vector2(index, 0f), Vector2.right, 1f, false);
            }
            var defeat = dense.EmitEntityDefeated(81, string.Empty, "enemy.boss",
                1, 999, Vector2.zero, Vector2.right, 20f);
            Drain(dense, bounded);
            Assert(bounded.CombatEffects.Count <= BattlePresentationBuffer.CombatEffectCapacity
                && bounded.Reactions.Count <= BattlePresentationBuffer.ReactionCapacity
                && bounded.Feedback.Count <= BattlePresentationBuffer.FloatingTextCapacity
                && bounded.AudioRequests.Count <= BattlePresentationBuffer.AudioCapacity,
                "every dense-combat feedback channel remains capped");
            Assert(bounded.CombatEffects.Any(effect => effect.EventSequence == defeat.Sequence)
                && bounded.Feedback.Any(value => value.Kind
                    == BattlePresentationEventKind.EntityDefeated)
                && bounded.AudioRequests.Any(value => value.EventSequence == defeat.Sequence),
                "higher-priority defeat feedback displaces saturated light hits");
            Assert(bounded.CombatEffects.Any(effect => effect.EventSequence
                    == latestLight.Sequence),
                "equal-priority saturation evicts the oldest record for recent feedback");

            var reactionBuffer = new BattlePresentationBuffer(catalog);
            var reactionStream = new BattlePresentationEventStream(256);
            for (var index = 0;
                 index < BattlePresentationBuffer.ReactionCapacity; index++)
            {
                reactionStream.EmitDamageResolved(index,
                    "ability.test.light", string.Empty, "plant.test",
                    "enemy.normal", 1, 2000 + index, Vector2.zero,
                    Vector2.right, 1f, false);
            }
            Drain(reactionStream, reactionBuffer);
            Assert(reactionBuffer.Reactions.Count
                    == BattlePresentationBuffer.ReactionCapacity,
                "nonfatal target reactions fill but do not exceed capacity");
            reactionStream.EmitDamageResolved(100,
                "ability.test.light", string.Empty, "plant.test",
                "enemy.normal", 1, 9999, Vector2.zero,
                Vector2.right, 100f, true);
            Drain(reactionStream, reactionBuffer);
            Assert(reactionBuffer.Reactions.Count
                    == BattlePresentationBuffer.ReactionCapacity
                && reactionBuffer.Reactions.All(value => value.EntityId != 9999),
                "fatal damage skips missing-target reaction without consuming capacity");

            var routed = bounded.RoutePendingAudio(SilentCombatAudioRouter.Instance);
            Assert(routed > 0 && bounded.AudioRequests.Count == 0,
                "missing bundled audio assets use an explicit consumed silent route");
        }

        private static void ValidateInterpolationAndSafeSnap()
        {
            var state = new GameState { Phase = GamePhase.Playing, LogicTick = 1 };
            var zombie = new Zombie
            {
                Id = 1,
                RouteId = BattlefieldLayerIds.PrimaryRoute,
                PathProgress = 0f,
            };
            var projectile = new ProjectileFlash { Id = 2, Position = Vector2.zero };
            state.Zombies.Add(zombie);
            state.Projectiles.Add(projectile);
            var samples = new BattleRenderInterpolationSamples();
            samples.SnapTo(state);

            state.LogicTick++;
            zombie.PathProgress = 10f;
            projectile.Position = new Vector2(10f, 4f);
            samples.Capture(state, 1);
            Assert(Mathf.Approximately(samples.EnemyPathProgress(1, 10f, .5f), 5f)
                && Vector2.Distance(samples.ProjectilePosition(2,
                    projectile.Position, .5f), new Vector2(5f, 2f)) < .001f,
                "single fixed-step movement renders between previous/current samples");

            state.Paused = true;
            state.LogicTick++;
            zombie.PathProgress = 20f;
            projectile.Position = new Vector2(20f, 8f);
            samples.Capture(state, 1);
            Assert(Mathf.Approximately(samples.EnemyPathProgress(1, 20f, 0f), 20f)
                && samples.ProjectilePosition(2, projectile.Position, 0f)
                    == projectile.Position,
                "pause alpha zero cannot move a rendered entity backward");

            state.Paused = false;
            state.LogicTick += 2;
            zombie.PathProgress = 30f;
            projectile.Position = new Vector2(30f, 12f);
            samples.Capture(state, 2);
            Assert(Mathf.Approximately(samples.EnemyPathProgress(1, 30f, .5f), 30f)
                && samples.ProjectilePosition(2, projectile.Position, .5f)
                    == projectile.Position,
                "multi-step catch-up safely snaps instead of spanning unknown samples");
        }

        private static BattlePresentationBuffer PeaBuffer()
        {
            var stream = new BattlePresentationEventStream();
            stream.EmitDamageResolved(1, "ability.plant.pea.attack", "projectile.pea",
                "plant.pea", "enemy.normal", 1, 2, Vector2.zero,
                Vector2.right, 5f, false);
            var result = new BattlePresentationBuffer();
            Drain(stream, result);
            return result;
        }

        private static CombatFeedbackProfile FloatingOnly(string id, float mergeWindow)
        {
            return new CombatFeedbackProfile(id, PresentationVfxKind.None,
                CombatFeedbackPriority.Light, .5f,
                floatingTextRole: CombatFloatingTextRole.NormalDamage,
                mergeWindow: mergeWindow);
        }

        private static IReadOnlyList<BattlePresentationEvent> BuildDenseEvents(int count)
        {
            var stream = new BattlePresentationEventStream(Mathf.Max(1, count));
            for (var index = 0; index < count; index++)
                stream.EmitDamageResolved(index + 1, "ability.test.dense",
                    string.Empty, "plant.test", "enemy.normal", 1, 5000 + index,
                    new Vector2(index % 4, index % 3), Vector2.right, 1f, false);
            var result = new List<BattlePresentationEvent>(count);
            stream.DrainTo(result);
            return result;
        }

        private static CompiledBattleContentCatalog Compile()
        {
            CompiledBattleContentCatalog catalog;
            ContentValidationResult validation;
            if (!BattleContentCompiler.TryCompile(BundledBattleContentFactory.Create(),
                    out catalog, out validation))
                throw new InvalidOperationException(string.Join("\n",
                    validation.Issues.Select(issue => issue.ToString()).ToArray()));
            return catalog;
        }

        private static void Drain(BattlePresentationEventStream stream,
            BattlePresentationBuffer destination)
        {
            var events = new List<BattlePresentationEvent>();
            stream.DrainTo(events);
            destination.Consume(events);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Combat impact feedback smoke failed: " + message);
        }
    }
}
