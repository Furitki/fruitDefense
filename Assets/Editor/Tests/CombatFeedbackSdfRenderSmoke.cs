using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Presentation;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class CombatFeedbackSdfRenderSmoke
    {
        private const string DamageId = "ability.test.sdf-anchor";
        private const string EnemyId = "enemy.test.sdf-anchor";

        public static void Run()
        {
            ValidateContactFollowDetachAndMergeClock();
            ValidateHighCapacityAdmission();
            ValidateMissingTargetAndDefeatCentroid();
            ValidateCompactUpwardLanes();
            ValidateSafeAreaProjection();
            ValidateMonotonicAllocationCounter();
            ValidatePooledSdfOverlay();
            Debug.Log("FRUIT_DEFENSE_COMBAT_FEEDBACK_SDF_RENDER_OK");
        }

        private static void ValidateContactFollowDetachAndMergeClock()
        {
            Assert(Mathf.Approximately(
                    CombatFloatingTextStyleCatalog.FollowSeconds, .12f),
                "live-target follow duration is exactly 0.12 presentation seconds");
            var buffer = new BattlePresentationBuffer(CreateCatalog());
            var stream = new BattlePresentationEventStream();
            var eventPoint = new Vector2(2f, 3f);
            stream.EmitDamageResolved(10, DamageId, string.Empty,
                "plant.test", EnemyId, 1, 10, eventPoint,
                Vector2.right, 4f, false);
            Drain(stream, buffer);
            Assert(buffer.Feedback.Count == 1, "one damage event admits one label");
            var feedback = buffer.Feedback[0];
            Assert(feedback.IsFollowingTarget
                && feedback.TargetEntityId == 10
                && feedback.EventPoint == eventPoint
                && feedback.Point == eventPoint
                && Mathf.Approximately(feedback.FollowElapsed, 0f),
                "damage starts at its semantic contact point with a live target resolver");

            var firstFollowPoint = new Vector2(4f, 5f);
            feedback.UpdateFollowPoint(firstFollowPoint);
            buffer.Advance(.08f, false, 1);
            Assert(feedback.IsFollowingTarget
                && feedback.Point == firstFollowPoint
                && Mathf.Abs(feedback.FollowElapsed - .08f) <= .0001f,
                "the live target can update the anchor inside the contact phase");

            stream.EmitDamageResolved(11, DamageId, string.Empty,
                "plant.test", EnemyId, 1, 10, new Vector2(7f, 8f),
                Vector2.right, 6f, false);
            Drain(stream, buffer);
            Assert(buffer.Feedback.Count == 1
                && ReferenceEquals(feedback, buffer.Feedback[0])
                && Mathf.Abs(feedback.FollowElapsed - .08f) <= .0001f,
                "merge reuses the record without restarting its follow clock");

            var secondFollowPoint = new Vector2(9f, 10f);
            feedback.UpdateFollowPoint(secondFollowPoint);
            buffer.Advance(.0401f, false, 1);
            Assert(!feedback.IsFollowingTarget,
                "the original follow clock reaches its cutoff after a merge");
            feedback.DetachFromTarget();
            var detachedPoint = feedback.Point;
            feedback.UpdateFollowPoint(new Vector2(20f, 30f));
            Assert(!feedback.IsFollowingTarget
                && feedback.TargetEntityId == 0
                && detachedPoint == secondFollowPoint
                && feedback.Point == detachedPoint
                && feedback.Point != feedback.EventPoint,
                "detach locks the last resolved point, clears target identity, and ignores later movement");
        }

        private static void ValidateHighCapacityAdmission()
        {
            Assert(CombatFloatingTextStyleCatalog.TotalCapacity == 9999
                && CombatFloatingTextStyleCatalog.OrdinaryCapacity == 9999,
                "combat floating-text admission uses the requested 9999 total and ordinary caps");
            var buffer = new BattlePresentationBuffer(CreateCatalog());
            var stream = new BattlePresentationEventStream(
                CombatFloatingTextStyleCatalog.TotalCapacity);
            for (var index = 0;
                 index < CombatFloatingTextStyleCatalog.TotalCapacity;
                 index++)
            {
                stream.EmitDamageResolved(index, DamageId, string.Empty,
                    "plant.test", EnemyId, 1, index + 1,
                    new Vector2(index, 0f), Vector2.right, 1f, false);
            }
            Drain(stream, buffer);
            Assert(stream.DroppedCount == 0
                && buffer.Feedback.Count
                    == CombatFloatingTextStyleCatalog.TotalCapacity
                && buffer.OrdinaryFeedbackCount
                    == CombatFloatingTextStyleCatalog.OrdinaryCapacity,
                "a 9999-record ordinary feedback burst is admitted without capacity eviction");
        }

        private static void ValidateMissingTargetAndDefeatCentroid()
        {
            var missing = new BattlePresentationBuffer(CreateCatalog());
            var stream = new BattlePresentationEventStream();
            var fallback = new Vector2(9f, 4f);
            stream.EmitDamageResolved(1, DamageId, string.Empty,
                "plant.test", EnemyId, 1, 0, fallback,
                Vector2.right, 3f, false);
            Drain(stream, missing);
            Assert(missing.Feedback.Count == 1
                && !missing.Feedback[0].IsFollowingTarget
                && missing.Feedback[0].TargetEntityId == 0
                && missing.Feedback[0].Point == fallback,
                "a missing target uses the finite semantic event position immediately");

            var fatal = new BattlePresentationBuffer(CreateCatalog());
            stream.EmitDamageResolved(2, DamageId, string.Empty,
                "plant.test", EnemyId, 1, 40, Vector2.zero,
                Vector2.right, 99f, true);
            Drain(stream, fatal);
            Assert(fatal.Feedback.Count == 0,
                "fatal damage remains free of a duplicate numeric label");

            var centroid = new BattlePresentationBuffer(CreateCatalog());
            stream.EmitEntityDefeated(20, string.Empty, EnemyId,
                1, 41, new Vector2(0f, 0f), Vector2.right, 0f);
            stream.EmitEntityDefeated(20, string.Empty, EnemyId,
                1, 42, new Vector2(3f, 0f), Vector2.right, 0f);
            stream.EmitEntityDefeated(20, string.Empty, EnemyId,
                1, 43, new Vector2(0f, 3f), Vector2.right, 0f);
            Drain(stream, centroid);
            Assert(centroid.Feedback.Count == 1,
                "same-tick defeat copy collapses to one record");
            var defeat = centroid.Feedback[0];
            Assert(defeat.Count == 3
                && Vector2.Distance(defeat.EventPoint, Vector2.one) <= .0001f
                && Vector2.Distance(defeat.Point, Vector2.one) <= .0001f
                && defeat.TargetEntityId == 0
                && !defeat.IsFollowingTarget,
                "same-tick defeat anchor is the arithmetic centroid and retains no target");
        }

        private static void ValidateCompactUpwardLanes()
        {
            var lane0 = CombatFloatingTextStyleCatalog.VisualLaneOffset(0);
            var lane1 = CombatFloatingTextStyleCatalog.VisualLaneOffset(1);
            var lane2 = CombatFloatingTextStyleCatalog.VisualLaneOffset(2);
            Assert(lane0 == Vector2.zero
                && lane1 == new Vector2(-8f, -14f)
                && lane2 == new Vector2(8f, -28f)
                && Mathf.Max(Mathf.Abs(lane1.x), Mathf.Abs(lane2.x)) <= 8f,
                "the three lanes are fixed at 0/-14/-28 with at most 8 horizontal pixels");
            Assert(CombatFloatingTextStyleCatalog.SemanticLaneOffset(
                    CombatFloatingTextRole.Defeat) == new Vector2(0f, -26f),
                "defeat owns the upward target-proximate 26-pixel semantic band");
        }

        private static void ValidateSafeAreaProjection()
        {
            var full = BattlefieldProjection.CalculateViewportLayout(
                402f, 874f, new Rect(0f, 0f, 402f, 874f), 402f, 874f);
            var inset = BattlefieldProjection.CalculateViewportLayout(
                430f, 932f, new Rect(0f, 36f, 430f, 850f), 402f, 874f);
            var anchor = new Vector2(201f, 437f);
            var lane = new Vector2(8f, 28f);
            var fullAnchor = CombatFloatingTextSdfOverlay.ProjectReferencePoint(
                full, anchor);
            var fullLane = CombatFloatingTextSdfOverlay.ProjectReferencePoint(
                full, anchor + lane);
            var insetAnchor = CombatFloatingTextSdfOverlay.ProjectReferencePoint(
                inset, anchor);
            var insetLane = CombatFloatingTextSdfOverlay.ProjectReferencePoint(
                inset, anchor + lane);
            Assert(Vector2.Distance(fullAnchor, anchor) <= .0001f
                && Vector2.Distance(fullLane - fullAnchor, lane * full.Scale) <= .0001f
                && Vector2.Distance(insetLane - insetAnchor, lane * inset.Scale) <= .0001f
                && Vector2.Distance(insetAnchor,
                    inset.ProjectDesignRect(new Rect(anchor, Vector2.zero)).position)
                    <= .0001f,
                "SDF anchors consume the same full and inset safe-area scale/offset transform");
        }

        private static void ValidatePooledSdfOverlay()
        {
            string reason;
            Assert(CombatFloatingTextSdfOverlay.TryValidateProductionAssets(out reason),
                "production baked-atlas assets validate before overlay creation: " + reason);
            CombatFloatingTextSdfOverlay overlay = null;
            try
            {
                Assert(CombatFloatingTextSdfOverlay.TryCreate(
                        null, out overlay, out reason) && overlay != null,
                    "one production atlas overlay is created: " + reason);
                var fields = typeof(CombatFloatingTextSdfOverlay).GetFields(
                    BindingFlags.Instance | BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.NonPublic);
                Assert(CombatFloatingTextSdfOverlay.PoolCapacity == 9999
                    && CombatFloatingTextSdfOverlay.SharedMaterialCount == 0
                    && CombatFloatingTextSdfOverlay.DrawCommandCapacity
                        == CombatFloatingTextSdfOverlay.PoolCapacity
                            * CombatFloatingTextSdfOverlay.MaximumGlyphsPerLabel
                    && CombatFloatingTextSdfOverlay.MaximumGlyphsPerLabel == 16
                    && typeof(CombatFloatingTextSdfOverlay).GetMethod(
                        "DrawOnGuiRepaint", BindingFlags.Instance | BindingFlags.Public) != null
                    && typeof(CombatFloatingTextSdfOverlay).GetMethod(
                        "DrawPreparedBatches", BindingFlags.Instance | BindingFlags.NonPublic) == null
                    && typeof(CombatFloatingTextSdfOverlay).GetMethod(
                        "RunPrePresentUpdate", BindingFlags.Static | BindingFlags.NonPublic) == null
                    && typeof(CombatFloatingTextSdfOverlay).GetMethod(
                        "SubmitAtPrePresent", BindingFlags.Instance | BindingFlags.NonPublic) == null
                    && fields.All(field => field.FieldType != typeof(Mesh)
                        && field.FieldType != typeof(Material)
                        && field.FieldType != typeof(RenderTexture)
                        && field.FieldType != typeof(WaitForEndOfFrame)
                        && field.FieldType != typeof(Camera)
                        && field.FieldType.FullName != "UnityEngine.Rendering.CommandBuffer")
                    && overlay.GetComponentsInChildren<Canvas>(true).Length == 0
                    && overlay.GetComponentsInChildren<CanvasRenderer>(true).Length == 0
                    && overlay.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Length == 0
                    && overlay.GetComponents<Camera>().Length == 0
                    && overlay.Metadata != null && overlay.Atlas != null
                    && overlay.Atlas.format == TextureFormat.RGBA32,
                    "runtime owns one RGBA page, fixed commands, and no TMP/mesh/material/PlayerLoop/camera/RT path");

                var gameSource = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
                var overlaySource = System.IO.File.ReadAllText(
                    System.IO.Path.Combine(Application.dataPath,
                        "Scripts/Presentation/CombatFloatingTextSdfOverlay.cs"));
                var overlayCall = gameSource.IndexOf(
                    "_floatingTextOverlay.DrawOnGuiRepaint();", StringComparison.Ordinal);
                var overlayLayer = gameSource.LastIndexOf(
                    "DrawOverlay(layout, _runtimeUiDrawContext);",
                    overlayCall, StringComparison.Ordinal);
                Assert(overlayLayer >= 0 && overlayCall > overlayLayer,
                    "FruitDefenseGame calls the atlas renderer after its final battle/HUD overlay layer");
                Assert(!overlaySource.Contains("previousMatrix", StringComparison.Ordinal)
                    && overlaySource.Contains(
                        "for (var rangeIndex = 0;", StringComparison.Ordinal)
                    && !overlaySource.Contains("towardInterior", StringComparison.Ordinal)
                    && !overlaySource.Contains("CollisionCandidate", StringComparison.Ordinal)
                    && !overlaySource.Contains("TotalOverlapArea", StringComparison.Ordinal),
                    "final-layer rendering has no upper-edge inward redirection or collision-avoidance branch");

                var buffer = BuildDenseOverlayRecords();
                Assert(buffer.Feedback.Count == 12
                    && buffer.OrdinaryFeedbackCount == 8,
                    "dense render fixture reaches exactly 12 visible / 8 ordinary records");
                var layout = new BattleUiLayout(GameConfig.DefaultBattlefield);
                var headerBefore = layout.Header;
                var pauseBefore = layout.PauseAction;
                var speedBefore = layout.SpeedAction;
                var boardBefore = layout.Board;
                var sampleCell = GameConfig.DefaultBattlefield.PlantableCells[0];
                var potHitBefore = layout.Battlefield.PotHitRect(sampleCell);
                var viewport = BattlefieldProjection.CalculateViewportLayout(
                    402f, 874f, new Rect(0f, 0f, 402f, 874f), 402f, 874f);
                ValidateFixtureBounds(overlay,
                    BuildAcceptanceRoleOverlayRecords(false), layout, viewport,
                    "role-grass");
                ValidateFixtureBounds(overlay,
                    BuildAcceptanceRoleOverlayRecords(true), layout, viewport,
                    "role-route");
                ValidateFixtureBounds(overlay, buffer, layout, viewport, "dense");
                var routeSideGutter = layout.BattleStage.xMax
                    - layout.Battlefield.GridRect.xMax;
                Assert(routeSideGutter > 0f,
                    "the inset BattleStage retains a finite route-side gutter");
                var routeSideX = layout.Battlefield.GridRect.xMax
                    + Mathf.Min(.05f, routeSideGutter * .25f);
                var originalRoutePoint = buffer.Feedback[0].Point;
                var routeSidePoint = originalRoutePoint;
                routeSidePoint.x += (routeSideX
                    - layout.Battlefield.MapToScreen(routeSidePoint).x)
                    / layout.Battlefield.MapScale;
                var reconstructedRouteSideAnchor = layout.Battlefield.MapToScreen(
                    routeSidePoint);
                Assert(Mathf.Abs(reconstructedRouteSideAnchor.x - routeSideX) <= .001f
                    && reconstructedRouteSideAnchor.x
                        > layout.Battlefield.GridRect.xMax
                    && reconstructedRouteSideAnchor.x < layout.BattleStage.xMax,
                    "route-side fixture reconstructs inside the audited two-point Stage gutter");
                buffer.Feedback[0].Point = routeSidePoint;
                var routeSideRecords = new[] { buffer.Feedback[0] };
                overlay.Sync(routeSideRecords, buffer.FloatingTextStyles,
                    layout.Battlefield, viewport, layout.BattleStage,
                    Vector2.zero);
                Vector2 routeCenter;
                Vector2 routeAnchor;
                float routeError;
                Rect routeBounds;
                Assert(overlay.TryGetScreenPlacement(
                        buffer.Feedback[0].EventSequence, out routeCenter,
                        out routeAnchor, out routeError, out routeBounds),
                    "route-side label exports its final atlas placement");
                var surfaceScreen = viewport.ProjectDesignRect(layout.BattleStage);
                var gridScreen = viewport.ProjectDesignRect(layout.Battlefield.GridRect);
                Assert(overlay.ActiveTextCount == 1
                    && overlay.PlacementValid
                    && overlay.PreparedAtlasDrawCount > 0
                    && overlay.PreparedAtlasDrawCount <= 192
                    && routeAnchor.x > gridScreen.xMax
                    && routeBounds.xMax > gridScreen.xMax
                    && routeBounds.xMin >= surfaceScreen.xMin - .05f
                    && routeBounds.yMin >= surfaceScreen.yMin - .05f
                    && routeBounds.xMax <= surfaceScreen.xMax + .05f
                    && routeBounds.yMax <= surfaceScreen.yMax + .05f
                    && routeError <= CombatFloatingTextSdfOverlay
                        .MaximumAnchorHorizontalError * viewport.Scale + .05f,
                    "a route-side label prepares bounded one-page glyph commands and clamps to BattleStage"
                    + " active=" + overlay.ActiveTextCount
                    + " valid=" + overlay.PlacementValid
                    + " draws=" + overlay.PreparedAtlasDrawCount
                    + " anchor=" + routeAnchor
                    + " bounds=" + routeBounds
                    + " grid=" + gridScreen
                    + " surface=" + surfaceScreen
                    + " error=" + routeError);

                buffer.Feedback[0].Point = originalRoutePoint;
                buffer.Advance(.08f, false, 1);
                overlay.Sync(buffer.Feedback, buffer.FloatingTextStyles,
                    layout.Battlefield, viewport, layout.BattleStage,
                    Vector2.zero);
                Assert(overlay.ActiveTextCount == 12 && overlay.PlacementValid
                    && overlay.PreparedAtlasDrawCount <= 20,
                    "the visible entry phase prepares all 12 atlas labels in no more than 20 composite/glyph draws");
                Debug.Log("FRUIT_DEFENSE_COMBAT_COMPOSITE_DENSE_DRAWS="
                    + overlay.PreparedAtlasDrawCount);

                var commandField = typeof(CombatFloatingTextSdfOverlay).GetField(
                    "_drawCommands", BindingFlags.Instance | BindingFlags.NonPublic);
                var rangeField = typeof(CombatFloatingTextSdfOverlay).GetField(
                    "_labelDrawRanges", BindingFlags.Instance | BindingFlags.NonPublic);
                var commands = commandField == null ? null
                    : commandField.GetValue(overlay) as Array;
                var ranges = rangeField == null ? null
                    : rangeField.GetValue(overlay) as Array;
                Assert(commands != null
                    && commands.Length == CombatFloatingTextSdfOverlay
                        .DrawCommandCapacity
                    && ranges != null
                    && ranges.Length == CombatFloatingTextSdfOverlay.PoolCapacity
                    && overlay.PreparedLabelDrawCount == 12,
                    "preallocated pool-sized glyph and label arrays own every atlas draw");
                var first = commands.GetValue(0);
                var commandType = first.GetType();
                var screenRect = (Rect)commandType.GetField("ScreenRect").GetValue(first);
                var uvRect = (Rect)commandType.GetField("UvRect").GetValue(first);
                Assert(screenRect.width > 0f && screenRect.height > 0f
                    && uvRect.xMin >= 0f && uvRect.yMin >= 0f
                    && uvRect.xMax <= 1f && uvRect.yMax <= 1f
                    && commandType.GetField("Color") == null,
                    "prepared glyph command owns only its finite screen rect and normalized one-page UV");
                var expectedRangeStart = 0;
                for (var index = 0; index < overlay.PreparedLabelDrawCount; index++)
                {
                    var range = ranges.GetValue(index);
                    var rangeType = range.GetType();
                    var start = (int)rangeType.GetField("Start").GetValue(range);
                    var count = (int)rangeType.GetField("Count").GetValue(range);
                    var color = (Color)rangeType.GetField("Color").GetValue(range);
                    Assert(start == expectedRangeStart && count > 0 && color.a > 0f,
                        "label range " + index
                        + " is contiguous and owns one semantic GUI color");
                    expectedRangeStart += count;
                }
                Assert(expectedRangeStart == overlay.PreparedAtlasDrawCount,
                    "12 label ranges cover every prepared glyph exactly once");
                overlay.Clear();
                overlay.Sync(buffer.Feedback, buffer.FloatingTextStyles,
                    layout.Battlefield, viewport, layout.BattleStage, Vector2.zero);
                Assert(ReferenceEquals(commands, commandField.GetValue(overlay))
                    && ReferenceEquals(ranges, rangeField.GetValue(overlay)),
                    "Clear and Sync reuse both fixed glyph-command and label-range arrays");

                ValidateMixedCompositeTokenLayout(overlay,
                    buffer.FloatingTextStyles, layout, viewport);

                overlay.Sync(buffer.Feedback, buffer.FloatingTextStyles,
                    layout.Battlefield, viewport, layout.BattleStage, Vector2.one);
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (var iteration = 0; iteration < 200; iteration++)
                    overlay.Sync(buffer.Feedback, buffer.FloatingTextStyles,
                        layout.Battlefield, viewport, layout.BattleStage, Vector2.one);
                var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
                Assert(allocatedAfter >= allocatedBefore
                    && allocatedAfter - allocatedBefore <= 16384,
                    "warm 12-label atlas command preparation remains allocation-bounded");

                overlay.BeginAcceptanceSyncProfile();
                var recordSample = typeof(CombatFloatingTextSdfOverlay).GetMethod(
                    "RecordAcceptanceSample",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert(recordSample != null,
                    "combined command/final-IMGUI samples own one aggregation seam");
                for (var iteration = 0; iteration < 720; iteration++)
                {
                    var firstTick = 10L + iteration * 10L;
                    recordSample.Invoke(overlay,
                        new object[] { firstTick, firstTick + 2L,
                            iteration % 20 == 0 ? 0L : 1L, 0L });
                }
                var profileSamples = overlay.AcceptanceProfileSamplesMilliseconds;
                var sortedSamples = profileSamples.OrderBy(value => value).ToArray();
                var p95Index = Mathf.CeilToInt(.95f * sortedSamples.Length) - 1;
                Assert(overlay.AcceptanceProfileSupported
                    && overlay.AcceptanceProfileCompleted
                    && !overlay.AcceptanceProfileActive
                    && overlay.AcceptanceProfileWarmupCount == 120
                    && overlay.AcceptanceProfileSampleCount == 600
                    && profileSamples.Count == 600
                    && profileSamples.All(value => value >= 0f
                        && !float.IsNaN(value) && !float.IsInfinity(value))
                    && overlay.AcceptanceProfileElapsedSeconds > 0f
                    && overlay.AcceptanceProfileAllocatedBytes >= 0
                    && overlay.AcceptanceProfileAllocatedBytesPerSecond >= 0f
                    && Mathf.Abs(overlay.AcceptanceProfileP95Milliseconds
                        - sortedSamples[p95Index]) <= .0001f,
                    "profiling retains 120 warmups and 600 command/final-IMGUI atlas CPU samples");

                Assert(layout.Header == headerBefore
                    && layout.PauseAction == pauseBefore
                    && layout.SpeedAction == speedBefore
                    && layout.Board == boardBefore
                    && layout.Battlefield.PotHitRect(sampleCell) == potHitBefore,
                    "atlas overlay leaves HUD and hit geometry unchanged");
            }
            finally
            {
                if (overlay != null) overlay.Dispose();
            }
        }

        private static void ValidateMixedCompositeTokenLayout(
            CombatFloatingTextSdfOverlay overlay,
            CombatFloatingTextStyleCatalog styles,
            BattleUiLayout layout, BattlefieldViewportLayout viewport)
        {
            const string mixedText = "-1234击败×7 阳光";
            var map = GameConfig.DefaultBattlefield;
            var point = map.CellToMap(
                map.PlantableCells[map.PlantableCells.Count / 2]);
            var feedback = new PresentationFeedback
            {
                Kind = BattlePresentationEventKind.DamageResolved,
                SemanticId = DamageId,
                ProfileId = "feedback.test.composite-mixed",
                EventPoint = point,
                Point = point,
                Role = CombatFloatingTextRole.NormalDamage,
                Magnitude = 1234f,
                Count = 1,
                Ttl = .52f,
                Duration = .62f,
                EventSequence = 999001,
                VisualLane = 0,
                Text = mixedText,
            };
            overlay.Clear();
            overlay.Sync(new[] { feedback }, styles,
                layout.Battlefield, viewport, layout.BattleStage, Vector2.zero);
            Assert(overlay.PlacementValid && overlay.ActiveTextCount == 1
                && overlay.PreparedLabelDrawCount == 1
                && overlay.PreparedAtlasDrawCount == 6,
                "mixed '-12' + glyphs + fixed tokens preserve arbitrary-length copy in six draws"
                + " (valid=" + overlay.PlacementValid
                + ", active=" + overlay.ActiveTextCount
                + ", labels=" + overlay.PreparedLabelDrawCount
                + ", draws=" + overlay.PreparedAtlasDrawCount
                + ", failure=" + overlay.PlacementFailure + ")");

            var commandField = typeof(CombatFloatingTextSdfOverlay).GetField(
                "_drawCommands", BindingFlags.Instance | BindingFlags.NonPublic);
            var commands = commandField == null ? null
                : commandField.GetValue(overlay) as Array;
            Assert(commands != null, "mixed token fixture exposes fixed draw commands");
            Vector2 center;
            Vector2 anchor;
            float anchorError;
            Rect bounds;
            Assert(overlay.TryGetScreenPlacement(feedback.EventSequence,
                    out center, out anchor, out anchorError, out bounds),
                "mixed token fixture exports unchanged character-measured bounds");
            for (var index = 0; index < overlay.PreparedAtlasDrawCount; index++)
            {
                var command = commands.GetValue(index);
                var commandType = command.GetType();
                var rect = (Rect)commandType.GetField("ScreenRect").GetValue(command);
                Assert(rect.xMin >= bounds.xMin - .05f
                    && rect.yMin >= bounds.yMin - .05f
                    && rect.xMax <= bounds.xMax + .05f
                    && rect.yMax <= bounds.yMax + .05f,
                    "mixed token command " + index
                    + " remains inside the original per-character measured bounds");
            }

            CombatFloatingTextCompositeToken negative = default;
            CombatFloatingTextCompositeToken defeat = default;
            CombatFloatingTextCompositeToken resource = default;
            Assert(overlay.Metadata.TryGetLongestCompositeToken(
                    mixedText, 0, out negative) && negative.Text == "-12"
                && overlay.Metadata.TryGetLongestCompositeToken(
                    mixedText, 5, out defeat) && defeat.Text == "击败×"
                && overlay.Metadata.TryGetLongestCompositeToken(
                    mixedText, 9, out resource) && resource.Text == " 阳光",
                "O(1) resolver selects the longest numeric and fixed prefixes in mixed copy");
            Assert(CommandUsesToken(commands.GetValue(0), negative)
                && CommandUsesToken(commands.GetValue(3), defeat)
                && CommandUsesToken(commands.GetValue(5), resource),
                "mixed layout submits the reviewed token UVs at the expected draw positions");

            var metadataSource = System.IO.File.ReadAllText(
                System.IO.Path.Combine(Application.dataPath,
                    "Scripts/Presentation/CombatFloatingTextAtlasMetadata.cs"));
            var resolverStart = metadataSource.IndexOf(
                "public bool TryGetLongestCompositeToken", StringComparison.Ordinal);
            var resolverEnd = metadataSource.IndexOf(
                "private bool TryGetCompositeTokenAt", resolverStart,
                StringComparison.Ordinal);
            var resolverSource = resolverStart < 0 || resolverEnd <= resolverStart
                ? string.Empty
                : metadataSource.Substring(resolverStart, resolverEnd - resolverStart);
            Assert(CombatFloatingTextAtlasMetadata
                    .MaximumCompositeTokenTableLookupsPerSegment == 2
                && !resolverSource.Contains("_compositeTokens.Length",
                    StringComparison.Ordinal)
                && !resolverSource.Contains("tokenIndex++", StringComparison.Ordinal),
                "hot token resolution uses direct numeric/fixed indices and never scans the 124-entry table");
        }

        private static bool CommandUsesToken(object command,
            CombatFloatingTextCompositeToken token)
        {
            if (command == null) return false;
            var commandType = command.GetType();
            var uv = (Rect)commandType.GetField("UvRect").GetValue(command);
            var expected = new Rect(
                token.AtlasRect.x / 512f, token.AtlasRect.y / 512f,
                token.AtlasRect.width / 512f, token.AtlasRect.height / 512f);
            return RectApproximately(uv, expected);
        }

        private static void ValidateMonotonicAllocationCounter()
        {
            Assert(CombatFloatingTextSdfOverlay.AcceptanceAllocationMetric
                    == "GC.GetAllocatedBytesForCurrentThread epoch-normalized into an "
                    + "acceptance-session cumulative managed-allocation counter",
                "acceptance telemetry names its managed-allocation metric exactly");

            var advance = typeof(CombatFloatingTextSdfOverlay).GetMethod(
                "TryAdvanceAcceptanceAllocationCounter",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert(advance != null,
                "the WebGL allocation counter exposes a deterministic reset seam");

            var initialized = false;
            var previousRaw = 0L;
            var cumulative = 0L;
            var rawSequence = new[] { 100L, 160L, 8L, 12L };
            var expectedSequence = new[] { 0L, 60L, 68L, 72L };
            for (var index = 0; index < rawSequence.Length; index++)
            {
                var arguments = new object[]
                {
                    rawSequence[index], initialized, previousRaw, cumulative,
                    0L, string.Empty,
                };
                var advanced = (bool)advance.Invoke(null, arguments);
                initialized = (bool)arguments[1];
                previousRaw = (long)arguments[2];
                cumulative = (long)arguments[3];
                var current = (long)arguments[4];
                var failure = (string)arguments[5];
                Assert(advanced && string.IsNullOrEmpty(failure)
                    && current == expectedSequence[index]
                    && cumulative == current,
                    "allocation epoch reset remains monotonic at sample " + index);
            }

            var invalidArguments = new object[]
            {
                -1L, initialized, previousRaw, cumulative, 0L, string.Empty,
            };
            Assert(!(bool)advance.Invoke(null, invalidArguments)
                && (string)invalidArguments[5]
                    == "managed-allocation-counter-negative",
                "a negative allocation source value fails closed");

            var overflowArguments = new object[]
            {
                1L, true, 0L, long.MaxValue, 0L, string.Empty,
            };
            Assert(!(bool)advance.Invoke(null, overflowArguments)
                && (string)overflowArguments[5]
                    == "managed-allocation-counter-overflow",
                "allocation accumulation overflow fails closed");
        }

        private static void ValidateFixtureBounds(
            CombatFloatingTextSdfOverlay overlay,
            BattlePresentationBuffer buffer, BattleUiLayout layout,
            BattlefieldViewportLayout viewport, string fixtureName)
        {
            overlay.Clear();
            overlay.Sync(buffer.Feedback, buffer.FloatingTextStyles,
                layout.Battlefield, viewport, layout.BattleStage, Vector2.zero);
            Assert(overlay.PlacementValid
                && overlay.ActiveTextCount == buffer.Feedback.Count,
                fixtureName + " prepares every admitted label"
                + " (active=" + overlay.ActiveTextCount
                + ", expected=" + buffer.Feedback.Count
                + ", failure=" + overlay.PlacementFailure + ")");
            var surface = viewport.ProjectDesignRect(layout.BattleStage);
            var bounds = new Rect[buffer.Feedback.Count];
            var centers = new Vector2[buffer.Feedback.Count];
            for (var index = 0; index < buffer.Feedback.Count; index++)
            {
                Vector2 anchor;
                float anchorError;
                Assert(overlay.TryGetScreenPlacement(
                        buffer.Feedback[index].EventSequence,
                        out centers[index], out anchor, out anchorError,
                        out bounds[index])
                    && bounds[index].width > 0f && bounds[index].height > 0f
                    && bounds[index].xMin >= surface.xMin - .05f
                    && bounds[index].yMin >= surface.yMin - .05f
                    && bounds[index].xMax <= surface.xMax + .05f
                    && bounds[index].yMax <= surface.yMax + .05f
                    && anchorError <= CombatFloatingTextSdfOverlay
                        .MaximumAnchorHorizontalError * viewport.Scale + .05f,
                    fixtureName + " label " + index
                    + " remains inside BattleStage and the contact envelope"
                    + " (anchorError=" + anchorError
                    + ", maximum=" + CombatFloatingTextSdfOverlay
                        .MaximumAnchorHorizontalError * viewport.Scale + ")");
            }
            overlay.Sync(buffer.Feedback, buffer.FloatingTextStyles,
                layout.Battlefield, viewport, layout.BattleStage, Vector2.zero);
            for (var index = 0; index < buffer.Feedback.Count; index++)
            {
                Vector2 center;
                Vector2 anchor;
                float anchorError;
                Rect repeatedBounds;
                Assert(overlay.TryGetScreenPlacement(
                        buffer.Feedback[index].EventSequence,
                        out center, out anchor, out anchorError,
                        out repeatedBounds)
                    && Vector2.Distance(center, centers[index]) <= .0001f
                    && RectApproximately(repeatedBounds, bounds[index]),
                    fixtureName + " label " + index
                    + " preserves stable event-order placement across Sync");
            }
            var reordered = buffer.Feedback.Reverse().ToArray();
            overlay.Sync(reordered, buffer.FloatingTextStyles,
                layout.Battlefield, viewport, layout.BattleStage, Vector2.zero);
            for (var index = 0; index < buffer.Feedback.Count; index++)
            {
                Vector2 center;
                Vector2 anchor;
                float anchorError;
                Rect reorderedBounds;
                Assert(overlay.TryGetScreenPlacement(
                        buffer.Feedback[index].EventSequence,
                        out center, out anchor, out anchorError, out reorderedBounds)
                    && Vector2.Distance(center, centers[index]) <= .0001f
                    && RectApproximately(reorderedBounds, bounds[index]),
                    fixtureName + " label " + index
                    + " keeps its authored-lane placement when dense record order changes");
            }
        }

        private static bool RectApproximately(Rect first, Rect second)
        {
            return Mathf.Abs(first.x - second.x) <= .0001f
                && Mathf.Abs(first.y - second.y) <= .0001f
                && Mathf.Abs(first.width - second.width) <= .0001f
                && Mathf.Abs(first.height - second.height) <= .0001f;
        }

        private static BattlePresentationBuffer BuildAcceptanceRoleOverlayRecords(
            bool route)
        {
            var map = GameConfig.DefaultBattlefield;
            var anchors = new Vector2[6];
            for (var index = 0; index < anchors.Length; index++)
                anchors[index] = route
                    ? RoutePoint(.22f + index * .1f)
                    : map.CellToMap(map.PlantableCells[index]);
            var stream = new BattlePresentationEventStream(16);
            stream.EmitDamageResolved(10, BattleContentIds.Abilities.PeaAttack,
                string.Empty, BattleContentIds.Plants.Pea,
                BattleContentIds.Enemies.Normal, 7001, route ? 7101 : 0,
                anchors[0], Vector2.left, 12f, false);
            stream.EmitDamageResolved(10,
                BattleContentIds.Abilities.WatermelonAttack, string.Empty,
                BattleContentIds.Plants.Watermelon,
                BattleContentIds.Enemies.Normal, 7002, 0,
                anchors[1], Vector2.left, 42f, false);
            stream.EmitDamageResolved(10, string.Empty, string.Empty,
                BattleContentIds.Statuses.ChiliBurn,
                BattleContentIds.Enemies.Normal, 7003, 0,
                anchors[2], Vector2.left, 6f, false);
            stream.EmitResourceGranted(10,
                BattleContentIds.Abilities.SunflowerProduce,
                BattleContentIds.Resources.Sun, 7004, 0, anchors[3], 25f);
            stream.EmitStatusProcced(10, BattleContentIds.Abilities.IceOnHit,
                BattleContentIds.Statuses.IceFreeze, 7005, 0,
                anchors[4], Vector2.left, 1f);
            stream.EmitEntityDefeated(10,
                BattleContentIds.Abilities.WatermelonAttack,
                BattleContentIds.Enemies.Normal, 7006, 7106,
                anchors[5], Vector2.left, 1f);
            return ConsumeAcceptanceFixture(stream);
        }

        private static BattlePresentationBuffer BuildDenseOverlayRecords()
        {
            var stream = new BattlePresentationEventStream(16);
            for (var index = 0; index < 3; index++)
                stream.EmitDamageResolved(30,
                    BattleContentIds.Abilities.PeaAttack, string.Empty,
                    BattleContentIds.Plants.Pea,
                    BattleContentIds.Enemies.Normal, 7001, 7200 + index,
                    RoutePoint(.22f + index * .1f), Vector2.left,
                    10f + index, false);
            for (var index = 0; index < 3; index++)
                stream.EmitDamageResolved(30,
                    BattleContentIds.Abilities.BananaAttack, string.Empty,
                    BattleContentIds.Plants.Banana,
                    BattleContentIds.Enemies.Normal, 7002, 7300 + index,
                    RoutePoint(.52f + index * .1f), Vector2.left,
                    14f + index, false);
            for (var index = 0; index < 2; index++)
                stream.EmitDamageResolved(30, string.Empty, string.Empty,
                    BattleContentIds.Statuses.ChiliBurn,
                    BattleContentIds.Enemies.Normal, 7003, 7400 + index,
                    RoutePoint(.37f + index * .12f), Vector2.left,
                    5f + index, false);
            stream.EmitDamageResolved(31,
                BattleContentIds.Abilities.WatermelonAttack, string.Empty,
                BattleContentIds.Plants.Watermelon,
                BattleContentIds.Enemies.Armored, 7004, 7501,
                RoutePoint(.58f), Vector2.left, 52f, false);
            stream.EmitResourceGranted(31,
                BattleContentIds.Abilities.SunflowerProduce,
                BattleContentIds.Resources.Sun, 7005, 0,
                GameConfig.DefaultBattlefield.CellToMap(
                    GameConfig.DefaultBattlefield.PlantableCells[3]), 25f);
            stream.EmitStatusProcced(31,
                BattleContentIds.Abilities.IceOnHit,
                BattleContentIds.Statuses.IceFreeze, 7006, 7502,
                RoutePoint(.47f), Vector2.left, 1f);
            stream.EmitEntityDefeated(31,
                BattleContentIds.Abilities.DurianAttack,
                BattleContentIds.Enemies.Normal, 7007, 7503,
                RoutePoint(.68f), Vector2.left, 1f);
            return ConsumeAcceptanceFixture(stream);
        }

        private static BattlePresentationBuffer ConsumeAcceptanceFixture(
            BattlePresentationEventStream stream)
        {
            var buffer = new BattlePresentationBuffer(
                CombatFeedbackCatalog.CreateBundled());
            Drain(stream, buffer);
            buffer.Advance(.06f, false, 1);
            return buffer;
        }

        private static Vector2 RoutePoint(float normalizedProgress)
        {
            var route = GameConfig.DefaultBattlefield.Route;
            return route.Sample(route.TotalLength * Mathf.Clamp01(normalizedProgress));
        }

        private static CombatFeedbackCatalog CreateCatalog()
        {
            var damageKey = new CombatFeedbackKey(
                BattlePresentationEventKind.DamageResolved, DamageId);
            var defeatKey = new CombatFeedbackKey(
                BattlePresentationEventKind.EntityDefeated, EnemyId);
            var catalog = new CombatFeedbackCatalog(new[] { damageKey, defeatKey });
            catalog.Declare(damageKey, CombatFeedbackCatalogEntry.Concrete(
                new CombatFeedbackProfile("feedback.test.sdf-anchor",
                    PresentationVfxKind.None, CombatFeedbackPriority.Light, .62f,
                    floatingTextRole: CombatFloatingTextRole.NormalDamage,
                    mergeWindow: .2f)));
            catalog.Declare(defeatKey, CombatFeedbackCatalogEntry.Concrete(
                new CombatFeedbackProfile("feedback.test.sdf-defeat",
                    PresentationVfxKind.None, CombatFeedbackPriority.Defeat, .62f,
                    floatingTextRole: CombatFloatingTextRole.Defeat,
                    mergeWindow: 0f)));
            return catalog;
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
                    "Combat feedback SDF render smoke failed: " + message);
        }
    }
}
