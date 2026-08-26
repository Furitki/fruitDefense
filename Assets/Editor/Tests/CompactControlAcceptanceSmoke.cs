using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FruitDefense.Core;
using FruitDefense.Presentation;
using FruitDefense.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FruitDefense.Editor
{
    public static class CompactControlAcceptanceSmoke
    {
        private const int CompactControlSize = 52;
        private const byte SignificantAlpha =
            RuntimeUiQualityProfile.NineSliceSignificantAlphaHigh;

        public static void Run()
        {
            ValidateFiniteSlotContract();
            ValidateContourMetricContract();
            ValidateSemanticActionContract();
            ValidateCompleteActionPairings();
            ValidateOrthogonalContainedGeometry();
            ValidateNamedStateCatalog();
            ValidateBattleMappingAndAuthoritativeRects();
            ValidateProductionResources();
            ValidateStrictRendererSource();
            Debug.Log("COMPACT_CONTROL_ACCEPTANCE_SMOKE_OK");
        }

        private static void ValidateSemanticActionContract()
        {
            Assert(Enum.GetValues(typeof(RuntimeUiActionKind)).Length == 4
                && Enum.GetValues(typeof(RuntimeUiActionContentForm)).Length == 4
                && Enum.GetValues(typeof(RuntimeUiActionBehavior)).Length == 2
                && Enum.GetValues(typeof(BattleUiActionSemantic)).Length == 5,
                "action role, content form, behavior, and Battle mapping stay finite");

            AssertActionSpec(BattleUiActionSemantic.StartWave,
                RuntimeUiActionKind.Primary, RuntimeUiActionContentForm.IconLabel,
                RuntimeUiActionBehavior.Instantaneous);
            AssertActionSpec(BattleUiActionSemantic.NurseryRefresh,
                RuntimeUiActionKind.Secondary, RuntimeUiActionContentForm.IconLabel,
                RuntimeUiActionBehavior.Instantaneous);
            AssertActionSpec(BattleUiActionSemantic.PauseContinue,
                RuntimeUiActionKind.Quiet, RuntimeUiActionContentForm.IconOnly,
                RuntimeUiActionBehavior.PersistentMode);
            AssertActionSpec(BattleUiActionSemantic.Speed,
                RuntimeUiActionKind.Quiet,
                RuntimeUiActionContentForm.CompactMultiplier,
                RuntimeUiActionBehavior.PersistentMode);
            AssertActionSpec(BattleUiActionSemantic.Close,
                RuntimeUiActionKind.Quiet, RuntimeUiActionContentForm.IconOnly,
                RuntimeUiActionBehavior.Instantaneous);

            AssertThrows<ArgumentException>(() => new RuntimeUiActionSpec(
                    RuntimeUiActionKind.Primary,
                    RuntimeUiActionContentForm.IconOnly,
                    RuntimeUiActionBehavior.PersistentMode),
                "persistent behavior cannot be inferred for a Primary icon");
            AssertThrows<ArgumentException>(() => new RuntimeUiActionSpec(
                    RuntimeUiActionKind.Quiet,
                    RuntimeUiActionContentForm.CompactMultiplier,
                    RuntimeUiActionBehavior.Instantaneous),
                "compact multiplier cannot be declared as an instantaneous command");

            var battle = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            foreach (BattleUiActionSemantic semantic in
                     Enum.GetValues(typeof(BattleUiActionSemantic)))
            {
                Assert(battle.Contains("BattleUiActionSemantic." + semantic),
                    semantic + " renderer call consumes the authoritative three-axis mapping");
            }
            var nursery = Slice(battle, "private void DrawNursery(",
                "private void RefreshNurseryFromUi(");
            Assert(nursery.Contains("BattleUiActionSemantic.NurseryRefresh")
                && !nursery.Contains("RuntimeUiActionKind.Primary"),
                "nursery refresh is explicitly Secondary rather than competing with start-wave Primary");
        }

        private static void AssertActionSpec(BattleUiActionSemantic semantic,
            RuntimeUiActionKind role, RuntimeUiActionContentForm form,
            RuntimeUiActionBehavior behavior)
        {
            var spec = BattleUiPresentationState.ResolveActionSpec(semantic);
            Assert(spec.Role == role && spec.ContentForm == form
                    && spec.Behavior == behavior,
                semantic + " preserves its explicit role/form/behavior mapping");
        }

        private static void ValidateCompleteActionPairings()
        {
            var theme = RuntimeUiArtSetRegistry.LoadReleaseTheme();
            Assert(theme != null, "release theme is available for action-style resolution");
            var interactions = new[]
            {
                RuntimeUiInteractionState.Normal,
                RuntimeUiInteractionState.HoveredOrFocused,
                RuntimeUiInteractionState.Pressed,
                RuntimeUiInteractionState.Disabled,
            };
            foreach (RuntimeUiActionKind role in
                     Enum.GetValues(typeof(RuntimeUiActionKind)))
            {
                var spec = new RuntimeUiActionSpec(role,
                    RuntimeUiActionContentForm.IconLabel,
                    RuntimeUiActionBehavior.Instantaneous);
                foreach (var interaction in interactions)
                {
                    ValidateResolvedPair(theme.ResolveActionStyle(spec,
                            interaction, false), spec, interaction, false,
                        role + "/icon-label/instantaneous/" + interaction);
                }
            }

            foreach (var semantic in new[]
                     {
                         BattleUiActionSemantic.PauseContinue,
                         BattleUiActionSemantic.Speed,
                     })
            {
                var spec = BattleUiPresentationState.ResolveActionSpec(semantic);
                foreach (var interaction in interactions)
                {
                    var inactive = theme.ResolveActionStyle(spec, interaction, false);
                    var active = theme.ResolveActionStyle(spec, interaction, true);
                    ValidateResolvedPair(inactive, spec, interaction, false,
                        semantic + "/inactive/" + interaction);
                    ValidateResolvedPair(active, spec, interaction, true,
                        semantic + "/active/" + interaction);
                    Assert(inactive.ContainerSlot
                            == RuntimeUiArtSlot.ActionCompactControl
                        && active.ContainerSlot
                            == RuntimeUiArtSlot.ActionCompactControlActive,
                        semantic + " resolves one mutually-exclusive compact surface per mode state");
                }
            }

            var primary = theme.ResolveActionStyle(
                BattleUiPresentationState.ResolveActionSpec(
                    BattleUiActionSemantic.StartWave),
                RuntimeUiInteractionState.Normal, false);
            var secondary = theme.ResolveActionStyle(
                BattleUiPresentationState.ResolveActionSpec(
                    BattleUiActionSemantic.NurseryRefresh),
                RuntimeUiInteractionState.Normal, false);
            Assert(primary.VisualRole == RuntimeUiActionVisualRole.Primary
                && secondary.VisualRole == RuntimeUiActionVisualRole.Secondary
                && primary.ContainerSlot == RuntimeUiArtSlot.ActionPrimary
                && secondary.ContainerSlot == RuntimeUiArtSlot.ActionSecondary
                && MaximumRgbDistance(primary.ContainerColor,
                    secondary.ContainerColor) > .1f,
                "ready-phase Primary and Secondary actions resolve distinct hierarchy pairings");

            AssertThrows<ArgumentException>(() => theme.ResolveActionStyle(
                    BattleUiPresentationState.ResolveActionSpec(
                        BattleUiActionSemantic.Close),
                    RuntimeUiInteractionState.Normal, true),
                "instant close cannot acquire a persistent mode-active style");
        }

        private static void ValidateResolvedPair(RuntimeUiResolvedActionStyle style,
            RuntimeUiActionSpec spec, RuntimeUiInteractionState interaction,
            bool modeActive, string label)
        {
            Assert(style.Spec.Role == spec.Role
                && style.Spec.ContentForm == spec.ContentForm
                && style.Spec.Behavior == spec.Behavior
                && style.InteractionState == interaction
                && style.ModeActive == modeActive
                && style.Disabled
                    == (interaction == RuntimeUiInteractionState.Disabled),
                label + " preserves every orthogonal semantic input");
            Assert(style.ContainerColor.a >= .999f
                && style.ContentColor.a >= .999f
                && style.OutlineColor.a >= .999f
                && Contrast(style.ContentColor, style.ContainerColor) + .001f
                    >= RuntimeUiQualityProfile.NormalTextContrast
                && Contrast(style.OutlineColor, style.ContainerColor) + .001f
                    >= RuntimeUiQualityProfile.NonTextContrast,
                label + " resolves a complete opaque contrast-safe container/content/cue pairing");
        }

        private static void ValidateContourMetricContract()
        {
            var single = CreateContourFixture(false, false);
            Assert(single.MinimumCentralHalfAxisContourRuns(.12f) == 1
                && single.MaxCentralHalfAxisContourRuns(.12f) == 1
                && single.MaxCentralHalfAxisStrongTransitionRuns(.10f) == 1
                && single.MaxCentralHalfAxisColorPathExcess(.04f) <= 1.6f,
                "fixed contour metric accepts one solid outer band");

            var stacked = CreateContourFixture(true, false);
            Assert(stacked.MaxCentralHalfAxisContourRuns(.12f) > 1,
                "fixed contour metric rejects a smaller button nested inside the base");

            var highlighted = CreateContourFixture(false, true);
            Assert(highlighted.MaxCentralHalfAxisStrongTransitionRuns(.10f) > 1
                && highlighted.MaxCentralHalfAxisColorPathExcess(.04f) > 1.6f,
                "fixed contour metric rejects inner/outer trim and highlight stripes");
        }

        private static ThumbnailProfile CreateContourFixture(bool stacked,
            bool highlighted)
        {
            const int size = CompactControlSize;
            var colors = Enumerable.Repeat(Color.clear, size * size).ToArray();
            var mask = new bool[colors.Length];
            var fill = new Color(.92f, .78f, .54f, 1f);
            var contour = new Color(.55f, .27f, .09f, 1f);
            var highlight = new Color(.95f, .69f, .25f, 1f);
            for (var y = 2; y < size - 2; y++)
            {
                for (var x = 2; x < size - 2; x++)
                {
                    var index = y * size + x;
                    var edgeDistance = Mathf.Min(
                        Mathf.Min(x - 2, size - 3 - x),
                        Mathf.Min(y - 2, size - 3 - y));
                    var color = fill;
                    if (highlighted && edgeDistance <= 5)
                    {
                        color = edgeDistance <= 1 || edgeDistance >= 4
                            ? contour : highlight;
                    }
                    else if (edgeDistance <= 3
                        || (stacked && edgeDistance >= 6
                            && edgeDistance <= 8))
                    {
                        color = contour;
                    }
                    colors[index] = color;
                    mask[index] = true;
                }
            }
            return new ThumbnailProfile(size, colors, mask);
        }

        private static void ValidateFiniteSlotContract()
        {
            Assert(RuntimeUiArtSlots.RequiredCount == 56
                && RuntimeUiArtSlots.Required.Count == 56,
                "finite art contract contains exactly 56 slots");
            for (var index = 0; index < RuntimeUiArtSlots.Required.Count; index++)
            {
                Assert((int)RuntimeUiArtSlots.Required[index] == index,
                    "finite slot cache remains contiguous at " + index);
            }

            Assert((int)RuntimeUiArtSlot.ActionCompactControl == 53
                && RuntimeUiArtSlots.SemanticId(
                    RuntimeUiArtSlot.ActionCompactControl) == "action.compact-control"
                && RuntimeUiArtSlots.Geometry(
                    RuntimeUiArtSlot.ActionCompactControl)
                    == RuntimeUiArtGeometry.NineSlice,
                "slot 53 is the dedicated compact-control nine-slice base");
            Assert((int)RuntimeUiArtSlot.ActionCompactControlActive == 54
                && RuntimeUiArtSlots.SemanticId(
                    RuntimeUiArtSlot.ActionCompactControlActive)
                    == "action.compact-control-active"
                && RuntimeUiArtSlots.Geometry(
                    RuntimeUiArtSlot.ActionCompactControlActive)
                    == RuntimeUiArtGeometry.NineSlice,
                "slot 54 is the complete active compact-control surface");
        }

        private static void ValidateOrthogonalContainedGeometry()
        {
            var tokens = RuntimeUiFeedbackTokens.SunnyOrchardDefault();
            var theme = RuntimeUiArtSetRegistry.LoadReleaseTheme();
            Assert(theme != null,
                "release theme is available for action focus-cue geometry");
            var drawContext = RuntimeUiDrawContext.Create(theme, 1f);
            var layout = new BattleUiLayout(GameConfig.DefaultBattlefield);
            var controls = new[]
            {
                new GeometryCase("pause", layout.PauseAction, false),
                new GeometryCase("continue", layout.PauseAction, false),
                new GeometryCase("speed", layout.SpeedAction, true),
                new GeometryCase("instant-close-52",
                    new Rect(20f, 100f, CompactControlSize, CompactControlSize), false),
                new GeometryCase("detail-close-existing-hit",
                    layout.DetailCloseAction, false),
            };
            var interactions = new[]
            {
                RuntimeUiInteractionState.Normal,
                RuntimeUiInteractionState.HoveredOrFocused,
                RuntimeUiInteractionState.Pressed,
                RuntimeUiInteractionState.Disabled,
            };
            var phases = new[]
            {
                RuntimeUiCompactControlVisualSample.Inactive,
                new RuntimeUiCompactControlVisualSample(
                    RuntimeUiCompactControlPhase.Activating, .45f),
                RuntimeUiCompactControlVisualSample.Active,
                new RuntimeUiCompactControlVisualSample(
                    RuntimeUiCompactControlPhase.Deactivating, .55f),
            };

            foreach (var control in controls)
            {
                RuntimeUiCompactControlLayout? normalActive = null;
                RuntimeUiCompactControlLayout? pressedActive = null;
                foreach (var interaction in interactions)
                {
                    foreach (var phase in phases)
                    {
                        var resolved = RuntimeUiGui.ResolveCompactControlLayout(
                            control.Rect, interaction, control.UsesMultiplierText,
                            tokens);
                        Assert(Approximately(resolved.ControlRect, control.Rect),
                            control.Name + " keeps draw/hit authority on its input Rect");
                        Assert(resolved.IsContained()
                            && Contains(control.Rect, resolved.VisualBounds)
                            && Contains(control.Rect, resolved.SurfaceRect)
                            && Contains(control.Rect, resolved.ContentRect)
                            && Approximately(resolved.VisualBounds,
                                resolved.SurfaceRect),
                            control.Name + "/" + interaction + "/" + phase.Phase
                            + " keeps every derived visual layer contained");
                        Assert(resolved.UsesMultiplierText
                                == control.UsesMultiplierText
                            && resolved.ContentRect.width > 0f
                            && resolved.ContentRect.height > 0f,
                            control.Name
                            + " owns exactly one centered icon or multiplier subject");
                        Assert(phase.ActiveAmount >= 0f
                                && phase.ActiveAmount <= 1f,
                            control.Name
                            + " lifecycle amount is independent of interaction");

                        if (phase.Phase == RuntimeUiCompactControlPhase.Active
                            && interaction == RuntimeUiInteractionState.Normal)
                            normalActive = resolved;
                        if (phase.Phase == RuntimeUiCompactControlPhase.Active
                            && interaction == RuntimeUiInteractionState.Pressed)
                            pressedActive = resolved;
                    }
                }

                Assert(normalActive.HasValue && pressedActive.HasValue
                    && pressedActive.Value.SurfaceRect.width
                        < normalActive.Value.SurfaceRect.width
                    && pressedActive.Value.SurfaceRect.height
                        < normalActive.Value.SurfaceRect.height
                    && Contains(control.Rect, pressedActive.Value.SurfaceRect),
                    control.Name
                    + " composes pressed feedback with a recognizable active surface");

                var translated = RuntimeUiGui.ResolveCompactControlLayout(
                    control.Rect, RuntimeUiInteractionState.Normal,
                    control.UsesMultiplierText, tokens,
                    new RuntimeUiMotionSample(.96f, .8f, 4f));
                Assert(translated.IsContained()
                    && Contains(control.Rect, translated.VisualBounds),
                    control.Name + " clamps feedback translation into the hit rectangle");

                foreach (var interaction in interactions)
                {
                    var cue = RuntimeUiGui.ResolveActionInteractionCueLayout(
                        drawContext, control.Rect, interaction);
                    var shouldShow = interaction
                        == RuntimeUiInteractionState.HoveredOrFocused;
                    Assert(cue.Visible == shouldShow && cue.IsContained(),
                        control.Name + "/" + interaction
                        + " resolves a contained focus cue only for hover/focus");
                    if (!shouldShow) continue;
                    Assert(cue.Top.width > 0f && cue.Top.height > 0f
                        && cue.Right.width > 0f && cue.Right.height > 0f
                        && cue.Bottom.width > 0f && cue.Bottom.height > 0f
                        && cue.Left.width > 0f && cue.Left.height > 0f,
                        control.Name
                        + " focus cue has four material structural segments");
                }
            }

            AssertThrows<ArgumentOutOfRangeException>(() =>
                    RuntimeUiGui.ResolveCompactControlLayout(layout.PauseAction,
                        RuntimeUiInteractionState.Selected, false, tokens),
                "generic Selected cannot substitute for mode lifecycle");
            AssertThrows<ArgumentOutOfRangeException>(() =>
                    RuntimeUiGui.ResolveCompactControlLayout(layout.PauseAction,
                        RuntimeUiInteractionState.Loading, false, tokens),
                "non-interaction semantic states cannot leak into compact controls");

            ValidateAdjacentGeometry(layout, interactions, phases, tokens);
            ValidateSafeAreaGeometry(layout, tokens);
        }

        private static void ValidateAdjacentGeometry(BattleUiLayout layout,
            IReadOnlyList<RuntimeUiInteractionState> interactions,
            IReadOnlyList<RuntimeUiCompactControlVisualSample> phases,
            RuntimeUiFeedbackTokens tokens)
        {
            foreach (var interaction in interactions)
            {
                foreach (var phase in phases)
                {
                    var pause = RuntimeUiGui.ResolveCompactControlLayout(
                        layout.PauseAction, interaction, false, tokens);
                    var speed = RuntimeUiGui.ResolveCompactControlLayout(
                        layout.SpeedAction, interaction, true, tokens);
                    Assert(!pause.VisualBounds.Overlaps(speed.VisualBounds)
                        && !pause.VisualBounds.Overlaps(layout.WaveMetric)
                        && !speed.VisualBounds.Overlaps(layout.WaveMetric)
                        && Contains(layout.Header, pause.VisualBounds)
                        && Contains(layout.Header, speed.VisualBounds),
                        interaction + "/" + phase.Phase
                        + " avoids adjacent controls, resource metrics and header bounds");
                }
            }
        }

        private static void ValidateSafeAreaGeometry(BattleUiLayout layout,
            RuntimeUiFeedbackTokens tokens)
        {
            var safeAreas = new[]
            {
                new Rect(0f, 0f, 402f, 874f),
                new Rect(0f, 34f, 402f, 796f),
            };
            foreach (var safeArea in safeAreas)
            {
                var viewport = BattlefieldProjection.CalculateViewportLayout(
                    402f, 874f, safeArea, 402f, 874f).DesignViewportRect;
                var pause = RuntimeUiGui.ResolveCompactControlLayout(
                    layout.PauseAction, RuntimeUiInteractionState.Pressed,
                    false, tokens);
                var speed = RuntimeUiGui.ResolveCompactControlLayout(
                    layout.SpeedAction, RuntimeUiInteractionState.Pressed,
                    true, tokens);
                Assert(Contains(safeArea, ToViewport(pause.VisualBounds, viewport))
                    && Contains(safeArea, ToViewport(speed.VisualBounds, viewport)),
                    "full/inset viewport keeps compact-control visual mass inside safe area "
                    + safeArea);
            }
        }

        private static void ValidateBattleMappingAndAuthoritativeRects()
        {
            var layout = new BattleUiLayout(GameConfig.DefaultBattlefield);
            Assert(Approximately(layout.PauseAction,
                       new Rect(274f, 12f, 52f, 52f))
                && Approximately(layout.SpeedAction,
                       new Rect(334f, 12f, 52f, 52f))
                && Approximately(layout.DetailCloseAction,
                       new Rect(346f, 606f, 44f, 44f)),
                "pause, speed and instant-close hit rectangles match the current layout authority");

            var ready = BattleUiPresentationState.Create(GamePhase.Playing, false);
            var paused = BattleUiPresentationState.Create(GamePhase.Playing, true);
            Assert(ready.PauseActionIcon == RuntimeUiArtSlot.IconControlPause
                && paused.PauseActionIcon == RuntimeUiArtSlot.IconControlContinue,
                "authoritative paused value selects pause/continue icon semantics");

            var battle = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            var header = Slice(battle,
                "private void DrawHeader(", "private void RefreshHeaderMetricMotion(");
            Assert(header.Contains("TrackBattleAction(")
                && header.Contains("PauseActionFeedbackTarget, layout.PauseAction")
                && header.Contains("SpeedActionFeedbackTarget, layout.SpeedAction")
                && header.Contains("DrawCompactControlVisual(drawContext, layout.PauseAction")
                && header.Contains("DrawCompactControlVisual(drawContext, layout.SpeedAction")
                && header.Contains("_pauseCompactControlState, viewState.IsPaused")
                && header.Contains("_speedCompactControlState, _game.State.Speed != 1")
                && header.Contains("_game.State.Speed + \"×\"")
                && !header.Contains("RuntimeUiActionKind.Quiet")
                && !header.Contains("RuntimeUiInteractionState.Selected"),
                "pause and speed share unchanged hit rects but use authoritative lifecycle mapping");

            var selectedPlant = Slice(battle,
                "private void DrawSelectedPlant(", "private void DrawDragGhost(");
            Assert(selectedPlant.Contains(
                    "DetailCloseFeedbackTarget, layout.DetailCloseAction")
                && selectedPlant.Contains("DrawCompactControlVisual(drawContext")
                && selectedPlant.Contains("layout.DetailCloseAction")
                && selectedPlant.Contains(
                    "BattleUiActionSemantic.Close), closeState")
                && selectedPlant.Contains(
                    "RuntimeUiCompactControlVisualSample.Inactive")
                && !selectedPlant.Contains("RuntimeUiCompactControlLifecycle.Evaluate"),
                "instant close uses interaction feedback and can never enter persistent mode");

            var rebind = Slice(battle,
                "private void RebindCompactControlPresentation(",
                "private RestartPresentationState CaptureRestartPresentation(");
            Assert(rebind.Contains("_pauseCompactControlState = default")
                && rebind.Contains("_speedCompactControlState = default")
                && rebind.Contains("RuntimeUiCompactControlLifecycle.Rebind(")
                && rebind.Contains("_game.State.Paused")
                && rebind.Contains("_game.State.Speed != 1"),
                "session clear/rebind initializes from current authoritative values");
        }

        private static void ValidateNamedStateCatalog()
        {
            const float start = 100f;
            var tokens = RuntimeUiFeedbackTokens.SunnyOrchardDefault();

            var oneX = RuntimeUiCompactControlLifecycle.Reset(1 != 1, start);
            Assert(oneX.Phase == RuntimeUiCompactControlPhase.Inactive
                && Approximately(RuntimeUiCompactControlLifecycle.Sample(
                    oneX, start, tokens).ActiveAmount, 0f),
                "1x catalog state is stable inactive");
            var twoXActivating = RuntimeUiCompactControlLifecycle.Evaluate(
                oneX, 2 != 1, start, tokens);
            var twoXActive = RuntimeUiCompactControlLifecycle.Evaluate(
                twoXActivating.State, 2 != 1,
                start + tokens.CompactControlActivateSeconds, tokens);
            var oneXDeactivating = RuntimeUiCompactControlLifecycle.Evaluate(
                twoXActive.State, 1 != 1,
                start + tokens.CompactControlActivateSeconds, tokens);
            Assert(twoXActivating.State.Phase
                    == RuntimeUiCompactControlPhase.Activating
                && twoXActive.State.Phase == RuntimeUiCompactControlPhase.Active
                && oneXDeactivating.State.Phase
                    == RuntimeUiCompactControlPhase.Deactivating,
                "speed catalog covers 2x activating/active and return-to-1x deactivating");

            var pauseActivating = RuntimeUiCompactControlLifecycle.Evaluate(
                RuntimeUiCompactControlLifecycle.Reset(false, start),
                true, start, tokens);
            var paused = RuntimeUiCompactControlLifecycle.Evaluate(
                pauseActivating.State, true,
                start + tokens.CompactControlActivateSeconds, tokens);
            var continueDeactivating = RuntimeUiCompactControlLifecycle.Evaluate(
                paused.State, false,
                start + tokens.CompactControlActivateSeconds, tokens);
            Assert(pauseActivating.State.Phase
                    == RuntimeUiCompactControlPhase.Activating
                && paused.State.Phase == RuntimeUiCompactControlPhase.Active
                && continueDeactivating.State.Phase
                    == RuntimeUiCompactControlPhase.Deactivating,
                "pause/continue catalog covers persistent startup, sustain and shutdown");

            var close = RuntimeUiCompactControlVisualSample.Inactive;
            Assert(close.Phase == RuntimeUiCompactControlPhase.Inactive
                && Approximately(close.ActiveAmount, 0f),
                "instant-close catalog has no persistent lifecycle state");
        }

        private static void ValidateProductionResources()
        {
            var releaseTheme = RuntimeUiArtSetRegistry.LoadReleaseTheme();
            Assert(releaseTheme != null
                && Approximately(releaseTheme.Feedback.CompactControlActivateSeconds, .16f)
                && Approximately(releaseTheme.Feedback.CompactControlDeactivateSeconds, .12f),
                "release theme serializes the approved compact-control feedback tokens");
            var sets = RuntimeUiArtSetRegistry.DiscoverProductionSets().ToArray();
            Assert(sets.Length == 2
                && sets.Select(set => set.SetId).SequenceEqual(
                    new[] { "sunny-orchard", "sunny-orchard-painted" }),
                "acceptance covers exactly both production ArtSets");

            var semanticHashes = new Dictionary<string, List<string>>
            {
                { "action.compact-control", new List<string>() },
                { "action.compact-control-active", new List<string>() },
            };
            var validationReports =
                new List<KeyValuePair<string, RuntimeUiVisualValidationReport>>();
            var artSetResults =
                new List<KeyValuePair<string, RuntimeUiValidationResult>>();
            foreach (var artSet in sets)
            {
                var report = RuntimeUiVisualSystemValidator.ValidateCandidate(
                    releaseTheme, artSet);
                validationReports.Add(new KeyValuePair<string,
                    RuntimeUiVisualValidationReport>(artSet.SetId, report));
                Assert(artSet.Bindings.Count == RuntimeUiArtSlots.RequiredCount,
                    artSet.SetId + " owns one complete finite binding set");
                artSetResults.Add(new KeyValuePair<string,
                    RuntimeUiValidationResult>(artSet.SetId, artSet.Validate()));

                var manifestPath = RuntimeUiArtSetRegistry.ManifestPath(artSet);
                var manifestJson = File.ReadAllText(ToAbsolute(manifestPath));
                var manifest = JsonUtility.FromJson<CompactArtManifest>(manifestJson);
                Assert(manifest != null && manifest.bindings != null
                    && manifest.slotCount == RuntimeUiArtSlots.RequiredCount
                    && manifest.bindings.Length == RuntimeUiArtSlots.RequiredCount,
                    artSet.SetId + " manifest records the complete 56-slot contract");

                foreach (var slot in new[]
                         {
                             RuntimeUiArtSlot.ActionCompactControl,
                             RuntimeUiArtSlot.ActionCompactControlActive,
                         })
                {
                    var semantic = RuntimeUiArtSlots.SemanticId(slot);
                    var matches = manifest.bindings.Where(row =>
                        row != null && row.slot == (int)slot
                        && row.semantic_id == semantic).ToArray();
                    Assert(matches.Length == 1,
                        artSet.SetId + " manifest owns exactly one " + semantic);
                    var row = matches[0];
                    var binding = artSet.GetRequiredBinding(slot);
                    ValidateGeneratedBinding(artSet, binding, row, manifestJson);
                    semanticHashes[semantic].Add(row.runtimeSha256);
                }

                var compact = manifest.bindings.Single(row =>
                    row.slot == (int)RuntimeUiArtSlot.ActionCompactControl);
                var active = manifest.bindings.Single(row =>
                    row.slot == (int)RuntimeUiArtSlot.ActionCompactControlActive);
                var quiet = artSet.GetRequiredBinding(RuntimeUiArtSlot.ActionQuiet);
                var selected = artSet.GetRequiredBinding(RuntimeUiArtSlot.MarkerSelected);
                Assert(!string.Equals(compact.runtimeSha256,
                        Sha256(ToAbsolute(AssetDatabase.GetAssetPath(quiet.Texture))),
                        StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(active.runtimeSha256, compact.runtimeSha256,
                        StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(active.runtimeSha256,
                        Sha256(ToAbsolute(AssetDatabase.GetAssetPath(quiet.Texture))),
                        StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(active.runtimeSha256,
                        Sha256(ToAbsolute(AssetDatabase.GetAssetPath(selected.Texture))),
                        StringComparison.OrdinalIgnoreCase),
                    artSet.SetId
                    + " compact surfaces are distinct and not quiet/selected fallbacks");
                ValidateAlphaAndGrayscaleStructure(artSet, compact, active);
                ValidateFinalPixelActionContrast(releaseTheme, artSet);
                ValidateGrayscaleContentStructure(releaseTheme, artSet);
            }

            foreach (var pair in artSetResults)
            {
                Assert(pair.Value.IsValid,
                    pair.Key + " finite binding values satisfy the art-set contract");
            }
            foreach (var pair in validationReports)
            {
                var report = pair.Value;
                Assert(report.IsValid && report.ErrorCount == 0
                    && report.WarningCount == 0,
                    pair.Key + " passes the complete runtime/manifest/import validator: "
                    + RuntimeUiVisualSystemValidator.FormatReport(report));
            }

            foreach (var pair in semanticHashes)
            {
                Assert(pair.Value.Count == 2
                    && !string.Equals(pair.Value[0], pair.Value[1],
                        StringComparison.OrdinalIgnoreCase),
                    pair.Key + " was independently generated for both ArtSets");
            }
        }

        private static void ValidateFinalPixelActionContrast(
            RuntimeUiTheme theme, RuntimeUiArtSet artSet)
        {
            var interactions = new[]
            {
                RuntimeUiInteractionState.Normal,
                RuntimeUiInteractionState.HoveredOrFocused,
                RuntimeUiInteractionState.Pressed,
                RuntimeUiInteractionState.Disabled,
            };
            var loaded = new Dictionary<RuntimeUiArtSlot, Texture2D>();
            try
            {
                foreach (RuntimeUiActionKind role in
                         Enum.GetValues(typeof(RuntimeUiActionKind)))
                {
                    var spec = new RuntimeUiActionSpec(role,
                        RuntimeUiActionContentForm.IconLabel,
                        RuntimeUiActionBehavior.Instantaneous);
                    foreach (var interaction in interactions)
                    {
                        var style = theme.ResolveActionStyle(spec,
                            interaction, false);
                        ValidateFinalPixelPair(theme, artSet, loaded, style,
                            184, 44, role + "/" + interaction);
                    }
                }

                foreach (var semantic in new[]
                         {
                             BattleUiActionSemantic.PauseContinue,
                             BattleUiActionSemantic.Speed,
                         })
                {
                    var spec = BattleUiPresentationState.ResolveActionSpec(semantic);
                    foreach (var interaction in interactions)
                    foreach (var modeActive in new[] { false, true })
                    {
                        var style = theme.ResolveActionStyle(spec,
                            interaction, modeActive);
                        ValidateFinalPixelPair(theme, artSet, loaded, style,
                            CompactControlSize, CompactControlSize,
                            semantic + "/" + interaction
                            + "/modeActive=" + modeActive);
                    }
                }
            }
            finally
            {
                foreach (var texture in loaded.Values)
                    Object.DestroyImmediate(texture);
            }
        }

        private static void ValidateFinalPixelPair(RuntimeUiTheme theme,
            RuntimeUiArtSet artSet,
            IDictionary<RuntimeUiArtSlot, Texture2D> loaded,
            RuntimeUiResolvedActionStyle style, int width, int height,
            string label)
        {
            if (!loaded.TryGetValue(style.ContainerSlot, out var texture))
            {
                var binding = artSet.GetRequiredBinding(style.ContainerSlot);
                texture = LoadPng(AssetDatabase.GetAssetPath(binding.Texture));
                loaded.Add(style.ContainerSlot, texture);
            }

            var surfaceBinding = artSet.GetRequiredBinding(style.ContainerSlot);
            var minimum = float.PositiveInfinity;
            var measured = 0;
            var xMin = Mathf.FloorToInt(width * .25f);
            var xMax = Mathf.CeilToInt(width * .75f);
            var yMin = Mathf.FloorToInt(height * .25f);
            var yMax = Mathf.CeilToInt(height * .75f);
            for (var y = yMin; y < yMax; y++)
            for (var x = xMin; x < xMax; x++)
            {
                var surface = SampleDestinationPixel(texture, surfaceBinding,
                    width, height, x, y);
                if (surface.a < SignificantAlpha / 255f)
                    continue;
                surface = CompositeOver(surface, theme.Colors.BaseSurface);
                minimum = Mathf.Min(minimum,
                    Contrast(style.ContentColor, surface));
                measured++;
            }

            Assert(measured > 0 && minimum + .001f
                    >= RuntimeUiQualityProfile.NormalTextContrast,
                artSet.SetId + "/" + label
                + " final significant interior pixels keep text/action-glyph contrast >=4.5:1; min="
                + minimum.ToString("0.00") + ":1, slot="
                + RuntimeUiArtSlots.SemanticId(style.ContainerSlot));
            Debug.Log("ACTION_FINAL_PIXEL_CONTRAST set=" + artSet.SetId
                + ", case=" + label + ", slot="
                + RuntimeUiArtSlots.SemanticId(style.ContainerSlot)
                + ", minimum=" + minimum.ToString("0.0000"));
        }

        private static Color SampleDestinationPixel(Texture2D texture,
            RuntimeUiArtBinding binding, int destinationWidth,
            int destinationHeight, int x, int y)
        {
            var source = binding.Sprite.rect;
            float sourceX;
            float sourceY;
            if (binding.Geometry == RuntimeUiArtGeometry.NineSlice)
            {
                var border = binding.SliceBorder;
                var scale = binding.PixelsPerLogicalUnit;
                sourceX = MapNineSliceCoordinate(x + .5f, destinationWidth,
                    source.xMin, source.width, border.Left, border.Right,
                    border.Left / scale, border.Right / scale);
                sourceY = MapNineSliceCoordinate(y + .5f, destinationHeight,
                    source.yMin, source.height, border.Bottom, border.Top,
                    border.Bottom / scale, border.Top / scale);
            }
            else
            {
                sourceX = source.xMin + (x + .5f) / destinationWidth * source.width;
                sourceY = source.yMin + (y + .5f) / destinationHeight * source.height;
            }
            return texture.GetPixelBilinear(sourceX / texture.width,
                sourceY / texture.height);
        }

        private static Color CompositeOver(Color foreground, Color background)
        {
            var alpha = Mathf.Clamp01(foreground.a);
            return new Color(
                foreground.r * alpha + background.r * (1f - alpha),
                foreground.g * alpha + background.g * (1f - alpha),
                foreground.b * alpha + background.b * (1f - alpha), 1f);
        }

        private static void ValidateGrayscaleContentStructure(
            RuntimeUiTheme theme, RuntimeUiArtSet artSet)
        {
            var pauseBinding = artSet.GetRequiredBinding(
                RuntimeUiArtSlot.IconControlPause);
            var continueBinding = artSet.GetRequiredBinding(
                RuntimeUiArtSlot.IconControlContinue);
            var pause = LoadPng(AssetDatabase.GetAssetPath(pauseBinding.Texture));
            var continuation = LoadPng(
                AssetDatabase.GetAssetPath(continueBinding.Texture));
            try
            {
                var different = 0;
                var union = 0;
                for (var y = 0; y < CompactControlSize; y++)
                for (var x = 0; x < CompactControlSize; x++)
                {
                    var pauseVisible = SampleDestinationPixel(pause, pauseBinding,
                            CompactControlSize, CompactControlSize, x, y).a
                        >= SignificantAlpha / 255f;
                    var continueVisible = SampleDestinationPixel(continuation,
                            continueBinding, CompactControlSize,
                            CompactControlSize, x, y).a
                        >= SignificantAlpha / 255f;
                    if (pauseVisible || continueVisible) union++;
                    if (pauseVisible != continueVisible) different++;
                }
                Assert(union > 0 && different >= union * .35f,
                    artSet.SetId
                    + " pause and continue remain different alpha/grayscale structures at 52px; changed="
                    + different + "/" + union);
            }
            finally
            {
                Object.DestroyImmediate(pause);
                Object.DestroyImmediate(continuation);
            }

            var font = theme.PackagedChineseFont;
            Assert(font != null && font.HasCharacter('1') && font.HasCharacter('2')
                && font.HasCharacter('×') && '1' != '2',
                "1× and 2× retain different non-color text structures in the packaged release font");
        }

        private static void ValidateGeneratedBinding(RuntimeUiArtSet artSet,
            RuntimeUiArtBinding binding, CompactManifestBinding row,
            string manifestJson)
        {
            var semantic = RuntimeUiArtSlots.SemanticId(binding.Slot);
            const string expectedGeometry = "nine-slice";
            var sourceRoot = RuntimeUiArtSetRegistry.SourceDirectory(artSet) + "/";
            var runtimeRoot = RuntimeUiArtSetRegistry.RuntimeDirectory(artSet) + "/";
            var source = RuntimeUiArtSetRegistry.Normalize(row.source);
            var runtime = RuntimeUiArtSetRegistry.Normalize(row.runtime);
            var promptRecord = RuntimeUiArtSetRegistry.Normalize(row.prompt_record);
            Assert(row.geometry == expectedGeometry
                && source.StartsWith(sourceRoot, StringComparison.Ordinal)
                && runtime.StartsWith(runtimeRoot, StringComparison.Ordinal)
                && promptRecord.StartsWith(sourceRoot, StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(row.shared_from_set),
                artSet.SetId + "/" + semantic
                + " is locally owned with the required geometry and no cross-set fallback");
            Assert(row.imagegen_provider == "built-in-imagegen"
                && !string.IsNullOrWhiteSpace(row.imagegen_output)
                && row.imagegen_output.StartsWith("exec-", StringComparison.Ordinal)
                && row.imagegen_output.EndsWith(".png", StringComparison.OrdinalIgnoreCase),
                artSet.SetId + "/" + semantic
                + " records a selected built-in imagegen output");
            Assert(File.Exists(ToAbsolute(source)) && File.Exists(ToAbsolute(runtime))
                && File.Exists(ToAbsolute(promptRecord)),
                artSet.SetId + "/" + semantic
                + " source, runtime and prompt-record files exist");
            Assert(string.Equals(row.sourceSha256, Sha256(ToAbsolute(source)),
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.runtimeSha256, Sha256(ToAbsolute(runtime)),
                    StringComparison.OrdinalIgnoreCase),
                artSet.SetId + "/" + semantic
                + " manifest hashes match checked-in source/runtime bytes");
            Assert(AssetDatabase.GetAssetPath(binding.Texture) == runtime
                && AssetDatabase.GetAssetPath(binding.Sprite) == runtime
                && binding.Sprite.texture == binding.Texture,
                artSet.SetId + "/" + semantic
                + " binds the exact local runtime PNG as texture and sprite");
            Assert(CountOccurrences(manifestJson,
                    "\"semantic_id\": \"" + semantic + "\"") == 1,
                artSet.SetId + "/" + semantic
                + " appears exactly once in the manifest");

            var promptJson = File.ReadAllText(ToAbsolute(promptRecord));
            Assert(promptJson.Contains("fruit-defense.imagegen-prompt-record.v1")
                && promptJson.Contains("\"semanticId\": \"" + semantic + "\"")
                && promptJson.Contains(row.imagegen_output)
                && promptJson.IndexOf(row.sourceSha256,
                    StringComparison.OrdinalIgnoreCase) >= 0
                && promptJson.Contains("\"prompt\"")
                && promptJson.Contains("\"references\"")
                && promptJson.Contains("\"alphaContract\""),
                artSet.SetId + "/" + semantic
                + " prompt record preserves prompt, references, alpha contract and selected hash");

            var importer = AssetImporter.GetAtPath(runtime) as TextureImporter;
            Assert(importer != null
                && importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single
                && !importer.mipmapEnabled
                && importer.wrapMode == TextureWrapMode.Clamp,
                artSet.SetId + "/" + semantic
                + " uses the standalone Sprite Single runtime import contract");
        }

        private static void ValidateAlphaAndGrayscaleStructure(RuntimeUiArtSet artSet,
            CompactManifestBinding compact, CompactManifestBinding active)
        {
            var baseTexture = LoadPng(compact.runtime);
            var activeTexture = LoadPng(active.runtime);
            try
            {
                var baseProfile = AnalyzeThumbnail(baseTexture, CompactControlSize,
                    artSet.GetRequiredBinding(
                        RuntimeUiArtSlot.ActionCompactControl));
                var activeProfile = AnalyzeThumbnail(
                    activeTexture, CompactControlSize,
                    artSet.GetRequiredBinding(
                        RuntimeUiArtSlot.ActionCompactControlActive));
                ValidateSimpleSurface(artSet, "inactive", baseProfile);
                ValidateSimpleSurface(artSet, "active", activeProfile);
                ValidateSurfacePair(artSet, baseProfile, activeProfile);
                var edgeDelta = MaximumEdgeDelta(baseProfile.Bounds,
                    activeProfile.Bounds);
                var visibleDelta = Mathf.Abs(baseProfile.VisiblePixels
                    - activeProfile.VisiblePixels)
                    / (float)Mathf.Max(baseProfile.VisiblePixels,
                        activeProfile.VisiblePixels);
                var centerDistance = MaximumRgbDistance(
                    baseProfile.CenterMeanColor(24),
                    activeProfile.CenterMeanColor(24));
                Debug.Log("COMPACT_CONTROL_SINGLE_SURFACE_METRICS set=" + artSet.SetId
                    + ", edgeDelta=" + edgeDelta
                    + ", visibleDelta=" + visibleDelta.ToString("F3")
                    + ", centerDistance=" + centerDistance.ToString("F3")
                    + ", centerRanges="
                    + baseProfile.CenterColorRange(24).ToString("F3")
                    + "/" + activeProfile.CenterColorRange(24).ToString("F3")
                    + ", inactive={"
                    + baseProfile.DescribeComposite(.12f, .10f)
                    + "}, active={"
                    + activeProfile.DescribeComposite(.12f, .10f) + "}");

                var grayscaleDifferences = CountGrayscaleSurfaceDifferences(
                    baseTexture, artSet.GetRequiredBinding(
                        RuntimeUiArtSlot.ActionCompactControl),
                    activeTexture, artSet.GetRequiredBinding(
                        RuntimeUiArtSlot.ActionCompactControlActive),
                    CompactControlSize, .025f);
                Assert(grayscaleDifferences
                        >= CompactControlSize * CompactControlSize * .01f,
                    artSet.SetId
                    + " mutually-exclusive inactive/active surfaces remain structurally distinct at 52px in grayscale");
            }
            finally
            {
                Object.DestroyImmediate(baseTexture);
                Object.DestroyImmediate(activeTexture);
            }
        }

        private static void ValidateSimpleSurface(RuntimeUiArtSet artSet,
            string state, ThumbnailProfile profile)
        {
            var label = artSet.SetId + " compact " + state + " surface";
            var metrics = profile.Describe();
            Assert(profile.ComponentCount == 1
                && profile.LargestComponentPixels == profile.VisiblePixels,
                label + " has exactly one significant-alpha component and no islands; "
                + metrics);
            Assert(profile.EnclosedHoleCount == 0
                && profile.MaxRowRuns == 1
                && profile.MaxColumnRuns == 1,
                label + " is one continuous hole-free rounded-square silhouette; "
                + metrics);
            Assert(profile.Bounds.width >= 46 && profile.Bounds.width <= 50
                && profile.Bounds.height >= 46 && profile.Bounds.height <= 50
                && Mathf.Abs(profile.Bounds.width - profile.Bounds.height) <= 4,
                label + " keeps a single square subject at readable 52px scale; "
                + metrics);

            var canvasArea = profile.Size * profile.Size;
            var boundsArea = profile.Bounds.width * profile.Bounds.height;
            var canvasCoverage = profile.VisiblePixels / (float)canvasArea;
            var boundsFill = profile.VisiblePixels / (float)boundsArea;
            Assert(canvasCoverage >= .78f && canvasCoverage <= .88f
                && boundsFill >= .85f,
                label + " prioritizes one filled body without excessive peripheral ornament; "
                + metrics);
            Assert(profile.CenterSignificantCoverage(24) >= .98f,
                label + " preserves a broad filled center for pause/play/speed glyphs; "
                + metrics);
            Assert(profile.CorePixelRatio >= .65f
                && profile.NormalizedBoundaryLength <= 1.25f,
                label + " has readable mass and one economical outer edge; " + metrics);
            Assert(profile.StrongEdgeDensity(profile.CenterSquare(24), .14f) <= .04f
                && profile.StrongEdgeDensity(profile.Bounds, .16f) <= .12f
                && profile.MaxCentralAxisStrongEdges(.16f) <= 3,
                label + " center stays calm and avoids nested borders or dense texture; "
                + metrics);
            Assert(profile.MinimumCentralHalfAxisContourRuns(.12f) == 1
                && profile.MaxCentralHalfAxisContourRuns(.12f) == 1
                && profile.MaxCentralHalfAxisStrongTransitionRuns(.10f) <= 1
                && profile.MaxCentralHalfAxisColorPathExcess(.04f) <= 1.6f,
                label + " has one outer contour per side with no inner highlight; "
                + profile.DescribeComposite(.12f, .10f));
        }

        private static void ValidateSurfacePair(RuntimeUiArtSet artSet,
            ThumbnailProfile baseProfile, ThumbnailProfile activeProfile)
        {
            var edgeDelta = MaximumEdgeDelta(baseProfile.Bounds,
                activeProfile.Bounds);
            var visibleDelta = Mathf.Abs(baseProfile.VisiblePixels
                - activeProfile.VisiblePixels)
                / (float)Mathf.Max(baseProfile.VisiblePixels,
                    activeProfile.VisiblePixels);
            var centerDistance = MaximumRgbDistance(
                baseProfile.CenterMeanColor(24), activeProfile.CenterMeanColor(24));
            Assert(edgeDelta <= 1 && visibleDelta <= .08f,
                artSet.SetId + " inactive and active surfaces share one coincident "
                + "button silhouette; edgeDelta=" + edgeDelta
                + ", visibleDelta=" + visibleDelta.ToString("F3"));
            Assert(baseProfile.CenterColorRange(24) <= .08f
                && activeProfile.CenterColorRange(24) <= .08f
                && centerDistance <= .22f,
                artSet.SetId + " inactive and active centers stay calm and visually "
                + "related so the glyph remains dominant; centerDistance="
                + centerDistance.ToString("F3"));
        }

        private static float MaximumRgbDistance(Color left, Color right)
        {
            return Mathf.Max(Mathf.Abs(left.r - right.r),
                Mathf.Abs(left.g - right.g), Mathf.Abs(left.b - right.b));
        }

        private static float Contrast(Color left, Color right)
        {
            var leftLuminance = RelativeLuminance(left);
            var rightLuminance = RelativeLuminance(right);
            return (Mathf.Max(leftLuminance, rightLuminance) + .05f)
                / (Mathf.Min(leftLuminance, rightLuminance) + .05f);
        }

        private static float RelativeLuminance(Color color)
        {
            return .2126f * LinearChannel(color.r)
                + .7152f * LinearChannel(color.g)
                + .0722f * LinearChannel(color.b);
        }

        private static float LinearChannel(float channel)
        {
            return channel <= .04045f
                ? channel / 12.92f
                : Mathf.Pow((channel + .055f) / 1.055f, 2.4f);
        }

        private static int MaximumEdgeDelta(RectInt left, RectInt right)
        {
            return Mathf.Max(
                Mathf.Max(Mathf.Abs(left.xMin - right.xMin),
                    Mathf.Abs(left.xMax - right.xMax)),
                Mathf.Max(Mathf.Abs(left.yMin - right.yMin),
                    Mathf.Abs(left.yMax - right.yMax)));
        }

        private static void ValidateStrictRendererSource()
        {
            var lifecycle = ReadSource(
                "Scripts/UI/RuntimeUiCompactControlLifecycle.cs");
            Assert(lifecycle.Contains("float unscaledTime")
                && !lifecycle.Contains("Time.")
                && !lifecycle.Contains("FruitDefense.Core")
                && !lifecycle.Contains("PlayerPrefs")
                && !lifecycle.Contains("GameSimulation"),
                "lifecycle is presentation-only and samples caller-supplied unscaled time");

            var gui = RuntimeUiSourceAuthority.ReadRuntimeGui();
            var standardAction = Slice(gui,
                "public static void DrawActionVisual(",
                "public static void DrawCompactControlVisual(");
            Assert(standardAction.Contains("ResolveActionStyle(spec, state, false)")
                && standardAction.Contains("style.ContainerSlot")
                && standardAction.Contains("style.ContentColor")
                && standardAction.Contains(
                    "DrawActionInteractionCue(context, visualRect, state,")
                && standardAction.Contains("style.OutlineColor")
                && CountOccurrences(standardAction,
                    "tintOverride: style.ContentColor") == 1
                && standardAction.Contains(
                    "DrawSlotArt(context, visualRect, style.ContainerSlot")
                && CountOccurrences(standardAction,
                    "RuntimeUiInteractionState.Normal") >= 2
                && !standardAction.Contains("ResolveActionTextTone")
                && !standardAction.Contains("context.Tint(")
                && !standardAction.Contains("context.Opacity("),
                "standard action renderer uses one resolved container and the same content color for label and glyph");
            var compact = Slice(gui,
                "public static void DrawCompactControlVisual(",
                "public static RuntimeUiActionContentLayout ResolveActionContentLayout(");
            Assert(compact.Contains("ResolveActionStyle(")
                && compact.Contains("style.ContainerSlot")
                && compact.Contains(
                    "DrawActionInteractionCue(context, layout.SurfaceRect,")
                && compact.Contains("style.OutlineColor")
                && compact.Contains("layout.SurfaceRect")
                && compact.Contains("lifecycleSample.ActiveAmount")
                && compact.Contains("tintOverride: style.ContentColor")
                && compact.Contains("RequireCompactInteractionState(interactionState)")
                && compact.Contains("RequireCompactActionSpec(spec)")
                && compact.Contains("ResolveCompactControlLayout(")
                && CountOccurrences(compact, "DrawSlotArt(context, layout.SurfaceRect") == 1
                && !compact.Contains("lifecycleSample.ShowsActiveSurface")
                && !compact.Contains("lifecycleSample.ActiveSurfaceOpacity")
                && !compact.Contains("ActionCompactControlActive")
                && !compact.Contains("ActionCompactControl,")
                && !compact.Contains("ScaleFromCenter")
                && !compact.Contains("IndicatorControlActive")
                && !compact.Contains("ShowsActiveOverlay")
                && !compact.Contains("OverlayOpacity")
                && !compact.Contains("RuntimeUiArtSlot.ActionQuiet")
                && !compact.Contains("RuntimeUiArtSlot.MarkerSelected")
                && !compact.Contains("TryGetBinding")
                && !compact.Contains("Texture2D.whiteTexture")
                && !compact.Contains("GUI.skin"),
                "compact renderer resolves and draws exactly one complete surface with one shared content color");
        }

        private static ThumbnailProfile AnalyzeThumbnail(Texture2D texture,
            int targetSize, RuntimeUiArtBinding binding = null)
        {
            var colors = new Color[targetSize * targetSize];
            var mask = new bool[colors.Length];
            for (var y = 0; y < targetSize; y++)
            {
                for (var x = 0; x < targetSize; x++)
                {
                    var index = y * targetSize + x;
                    colors[index] = SampleThumbnailPixel(texture,
                        binding, targetSize, x, y);
                    mask[index] = colors[index].a
                        >= SignificantAlpha / 255f;
                }
            }
            return new ThumbnailProfile(targetSize, colors, mask);
        }

        private static Color SampleThumbnailPixel(Texture2D texture,
            RuntimeUiArtBinding binding, int targetSize, int x, int y)
        {
            if (binding == null
                || binding.Geometry != RuntimeUiArtGeometry.NineSlice)
            {
                return texture.GetPixelBilinear((x + .5f) / targetSize,
                    (y + .5f) / targetSize);
            }

            var source = binding.Sprite.rect;
            var border = binding.SliceBorder;
            var scale = binding.PixelsPerLogicalUnit;
            var sourceX = MapNineSliceCoordinate(x + .5f, targetSize,
                source.xMin, source.width, border.Left, border.Right,
                border.Left / scale, border.Right / scale);
            var sourceY = MapNineSliceCoordinate(y + .5f, targetSize,
                source.yMin, source.height, border.Bottom, border.Top,
                border.Bottom / scale, border.Top / scale);
            return texture.GetPixelBilinear(sourceX / texture.width,
                sourceY / texture.height);
        }

        private static float MapNineSliceCoordinate(float destination,
            float destinationSize, float sourceOrigin, float sourceSize,
            float sourceLeading, float sourceTrailing,
            float destinationLeading, float destinationTrailing)
        {
            var fittedLeading = destinationLeading;
            var fittedTrailing = destinationTrailing;
            var destinationBorders = fittedLeading + fittedTrailing;
            if (destinationBorders > destinationSize && destinationBorders > 0f)
            {
                var fit = destinationSize / destinationBorders;
                fittedLeading *= fit;
                fittedTrailing *= fit;
            }

            if (destination < fittedLeading && fittedLeading > 0f)
                return sourceOrigin + destination / fittedLeading * sourceLeading;
            if (destination > destinationSize - fittedTrailing
                && fittedTrailing > 0f)
            {
                return sourceOrigin + sourceSize - sourceTrailing
                    + (destination - (destinationSize - fittedTrailing))
                    / fittedTrailing * sourceTrailing;
            }

            var destinationCenter = destinationSize
                - fittedLeading - fittedTrailing;
            var sourceCenter = sourceSize - sourceLeading - sourceTrailing;
            if (destinationCenter <= 0f || sourceCenter <= 0f)
                return sourceOrigin + sourceLeading;
            return sourceOrigin + sourceLeading
                + (destination - fittedLeading) / destinationCenter * sourceCenter;
        }

        private static int CountGrayscaleSurfaceDifferences(Texture2D baseTexture,
            RuntimeUiArtBinding baseBinding, Texture2D activeTexture,
            RuntimeUiArtBinding activeBinding, int targetSize, float threshold)
        {
            var changed = 0;
            for (var y = 0; y < targetSize; y++)
            {
                for (var x = 0; x < targetSize; x++)
                {
                    var inactive = SampleThumbnailPixel(baseTexture, baseBinding,
                        targetSize, x, y);
                    var active = SampleThumbnailPixel(activeTexture, activeBinding,
                        targetSize, x, y);
                    if (Mathf.Abs(Grayscale(active) - Grayscale(inactive))
                        >= threshold)
                        changed++;
                }
            }
            return changed;
        }

        private static float Grayscale(Color color)
        {
            return color.r * .299f + color.g * .587f + color.b * .114f;
        }

        private static Texture2D LoadPng(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            Assert(ImageConversion.LoadImage(texture,
                    File.ReadAllBytes(ToAbsolute(assetPath)), false),
                "PNG decodes for structural inspection: " + assetPath);
            return texture;
        }

        private static Rect ToViewport(Rect designRect, Rect designViewport)
        {
            var scaleX = designViewport.width / BattleUiLayout.DesignWidth;
            var scaleY = designViewport.height / BattleUiLayout.DesignHeight;
            return new Rect(designViewport.x + designRect.x * scaleX,
                designViewport.y + designRect.y * scaleY,
                designRect.width * scaleX, designRect.height * scaleY);
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            const float tolerance = .001f;
            return inner.xMin >= outer.xMin - tolerance
                && inner.yMin >= outer.yMin - tolerance
                && inner.xMax <= outer.xMax + tolerance
                && inner.yMax <= outer.yMax + tolerance;
        }

        private static bool Approximately(Rect left, Rect right)
        {
            return Vector2.Distance(left.position, right.position) <= .001f
                && Vector2.Distance(left.size, right.size) <= .001f;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= .0001f;
        }

        private static string ReadSource(string assetRelativePath)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath,
                assetRelativePath));
        }

        private static string Slice(string source, string startToken, string endToken)
        {
            var start = source.IndexOf(startToken, StringComparison.Ordinal);
            Assert(start >= 0, "cannot locate source boundary " + startToken);
            var end = source.IndexOf(endToken, start + startToken.Length,
                StringComparison.Ordinal);
            Assert(end > start, "cannot locate source boundary " + endToken);
            return source.Substring(start, end - start);
        }

        private static int CountOccurrences(string value, string token)
        {
            var count = 0;
            var cursor = 0;
            while ((cursor = value.IndexOf(token, cursor,
                       StringComparison.Ordinal)) >= 0)
            {
                count++;
                cursor += token.Length;
            }
            return count;
        }

        private static string Sha256(string absolutePath)
        {
            using (var hash = SHA256.Create())
            using (var stream = File.OpenRead(absolutePath))
                return BitConverter.ToString(hash.ComputeHash(stream))
                    .Replace("-", string.Empty);
        }

        private static string ToAbsolute(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                RuntimeUiArtSetRegistry.Normalize(assetPath)));
        }

        private static void AssertThrows<T>(Action action, string message)
            where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException(
                "Compact-control acceptance smoke failed: " + message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Compact-control acceptance smoke failed: " + message);
        }

        private readonly struct GeometryCase
        {
            public GeometryCase(string name, Rect rect, bool usesMultiplierText)
            {
                Name = name;
                Rect = rect;
                UsesMultiplierText = usesMultiplierText;
            }

            public string Name { get; }
            public Rect Rect { get; }
            public bool UsesMultiplierText { get; }
        }

        private readonly struct ThumbnailProfile
        {
            private readonly Color[] colors;
            private readonly bool[] mask;

            public ThumbnailProfile(int size, Color[] colors, bool[] mask)
            {
                Size = size;
                this.colors = colors;
                this.mask = mask;

                var minX = size;
                var minY = size;
                var maxX = -1;
                var maxY = -1;
                var visible = 0;
                var boundary = 0;
                var core = 0;
                var maxRowRuns = 0;
                var maxColumnRuns = 0;
                var rowsWithMoreThanTwoRuns = 0;
                var columnsWithMoreThanTwoRuns = 0;
                for (var y = 0; y < size; y++)
                {
                    var rowRuns = 0;
                    var inRun = false;
                    for (var x = 0; x < size; x++)
                    {
                        var index = y * size + x;
                        if (!mask[index])
                        {
                            inRun = false;
                            continue;
                        }

                        if (!inRun) rowRuns++;
                        inRun = true;
                        visible++;
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                        if (IsSet(mask, size, x - 1, y)
                            && IsSet(mask, size, x + 1, y)
                            && IsSet(mask, size, x, y - 1)
                            && IsSet(mask, size, x, y + 1))
                            core++;
                        if (!IsSet(mask, size, x - 1, y)) boundary++;
                        if (!IsSet(mask, size, x + 1, y)) boundary++;
                        if (!IsSet(mask, size, x, y - 1)) boundary++;
                        if (!IsSet(mask, size, x, y + 1)) boundary++;
                    }
                    maxRowRuns = Mathf.Max(maxRowRuns, rowRuns);
                    if (rowRuns > 2) rowsWithMoreThanTwoRuns++;
                }

                for (var x = 0; x < size; x++)
                {
                    var columnRuns = 0;
                    var inRun = false;
                    for (var y = 0; y < size; y++)
                    {
                        if (!mask[y * size + x])
                        {
                            inRun = false;
                            continue;
                        }
                        if (!inRun) columnRuns++;
                        inRun = true;
                    }
                    maxColumnRuns = Mathf.Max(maxColumnRuns, columnRuns);
                    if (columnRuns > 2) columnsWithMoreThanTwoRuns++;
                }

                VisiblePixels = visible;
                Bounds = visible == 0
                    ? new RectInt(0, 0, 0, 0)
                    : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
                BoundaryLength = boundary;
                CorePixels = core;
                MaxRowRuns = maxRowRuns;
                MaxColumnRuns = maxColumnRuns;
                RowsWithMoreThanTwoRuns = rowsWithMoreThanTwoRuns;
                ColumnsWithMoreThanTwoRuns = columnsWithMoreThanTwoRuns;
                AnalyzeComponents(mask, size, true,
                    out var componentCount, out var largestComponent);
                ComponentCount = componentCount;
                LargestComponentPixels = largestComponent;
                EnclosedHoleCount = CountEnclosedHoles(mask, size);
            }

            public int Size { get; }
            public int VisiblePixels { get; }
            public int ComponentCount { get; }
            public int LargestComponentPixels { get; }
            public int EnclosedHoleCount { get; }
            public RectInt Bounds { get; }
            public int BoundaryLength { get; }
            public int CorePixels { get; }
            public int MaxRowRuns { get; }
            public int MaxColumnRuns { get; }
            public int RowsWithMoreThanTwoRuns { get; }
            public int ColumnsWithMoreThanTwoRuns { get; }

            public float CorePixelRatio => VisiblePixels == 0
                ? 0f : CorePixels / (float)VisiblePixels;

            public float NormalizedBoundaryLength => Bounds.width == 0
                || Bounds.height == 0
                    ? float.PositiveInfinity
                    : BoundaryLength / (2f * (Bounds.width + Bounds.height));

            public RectInt CenterSquare(int side)
            {
                var clamped = Mathf.Clamp(side, 1, Size);
                var origin = (Size - clamped) / 2;
                return new RectInt(origin, origin, clamped, clamped);
            }

            public string Describe()
            {
                return "components=" + ComponentCount
                    + ", holes=" + EnclosedHoleCount
                    + ", bounds=" + Bounds.width + "x" + Bounds.height
                    + ", visible=" + VisiblePixels
                    + ", rowRuns=" + MaxRowRuns
                    + ", columnRuns=" + MaxColumnRuns
                    + ", excessRunLines="
                    + (RowsWithMoreThanTwoRuns + ColumnsWithMoreThanTwoRuns)
                    + ", centerAlpha=" + CenterSignificantCoverage(24).ToString("F3")
                    + ", core=" + CorePixelRatio.ToString("F3")
                    + ", boundary=" + NormalizedBoundaryLength.ToString("F3")
                    + ", centerEdges="
                    + StrongEdgeDensity(CenterSquare(24), .14f).ToString("F3")
                    + ", allEdges="
                    + StrongEdgeDensity(Bounds, .16f).ToString("F3")
                    + ", axisEdges=" + MaxCentralAxisStrongEdges(.16f);
            }

            public string DescribeComposite(float contourDistance,
                float transitionDistance)
            {
                return Describe()
                    + ", halfContourRuns="
                    + MinimumCentralHalfAxisContourRuns(contourDistance)
                    + ".." + MaxCentralHalfAxisContourRuns(contourDistance)
                    + ", halfContourPixels="
                    + MinimumCentralHalfAxisContourPixels(contourDistance)
                    + ".." + MaxCentralHalfAxisContourPixels(contourDistance)
                    + ", halfTransitionRuns="
                    + MaxCentralHalfAxisStrongTransitionRuns(transitionDistance)
                    + ", pathExcess="
                    + MaxCentralHalfAxisColorPathExcess(.04f).ToString("F3");
            }

            public float CenterSignificantCoverage(int side)
            {
                var region = CenterSquare(side);
                var significant = 0;
                for (var y = region.yMin; y < region.yMax; y++)
                    for (var x = region.xMin; x < region.xMax; x++)
                        if (mask[y * Size + x]) significant++;
                return significant / (float)(region.width * region.height);
            }

            public Color CenterMeanColor(int side)
            {
                var region = CenterSquare(side);
                var sum = Color.clear;
                var count = 0;
                for (var y = region.yMin; y < region.yMax; y++)
                {
                    for (var x = region.xMin; x < region.xMax; x++)
                    {
                        var index = y * Size + x;
                        if (!mask[index]) continue;
                        sum += colors[index];
                        count++;
                    }
                }
                return count == 0 ? Color.clear : sum / count;
            }

            public float CenterColorRange(int side)
            {
                var region = CenterSquare(side);
                var minimum = new Color(1f, 1f, 1f, 1f);
                var maximum = Color.clear;
                var count = 0;
                for (var y = region.yMin; y < region.yMax; y++)
                {
                    for (var x = region.xMin; x < region.xMax; x++)
                    {
                        var index = y * Size + x;
                        if (!mask[index]) continue;
                        var color = colors[index];
                        minimum.r = Mathf.Min(minimum.r, color.r);
                        minimum.g = Mathf.Min(minimum.g, color.g);
                        minimum.b = Mathf.Min(minimum.b, color.b);
                        maximum.r = Mathf.Max(maximum.r, color.r);
                        maximum.g = Mathf.Max(maximum.g, color.g);
                        maximum.b = Mathf.Max(maximum.b, color.b);
                        count++;
                    }
                }
                return count == 0 ? 0f : MaximumRgbDistance(minimum, maximum);
            }

            public float StrongEdgeDensity(RectInt region, float threshold)
            {
                var xMin = Mathf.Clamp(region.xMin, 0, Size);
                var xMax = Mathf.Clamp(region.xMax, 0, Size);
                var yMin = Mathf.Clamp(region.yMin, 0, Size);
                var yMax = Mathf.Clamp(region.yMax, 0, Size);
                var eligible = 0;
                var strong = 0;
                for (var y = yMin; y < yMax; y++)
                {
                    for (var x = xMin; x < xMax; x++)
                    {
                        var index = y * Size + x;
                        if (!mask[index]) continue;
                        if (x + 1 < xMax && mask[index + 1])
                        {
                            eligible++;
                            if (ColorDistance(colors[index], colors[index + 1])
                                >= threshold) strong++;
                        }
                        if (y + 1 < yMax && mask[index + Size])
                        {
                            eligible++;
                            if (ColorDistance(colors[index], colors[index + Size])
                                >= threshold) strong++;
                        }
                    }
                }
                return eligible == 0 ? 0f : strong / (float)eligible;
            }

            public int MaxCentralAxisStrongEdges(float threshold)
            {
                var lower = Size / 2 - 1;
                var upper = Size / 2;
                return Mathf.Max(
                    Mathf.Max(CountRowStrongEdges(lower, threshold),
                        CountRowStrongEdges(upper, threshold)),
                    Mathf.Max(CountColumnStrongEdges(lower, threshold),
                        CountColumnStrongEdges(upper, threshold)));
            }

            public int MinimumCentralHalfAxisContourRuns(float threshold)
            {
                var profile = this;
                return EvaluateCentralHalfAxes((startX, startY, stepX, stepY) =>
                    profile.CountContourRuns(startX, startY, stepX, stepY,
                        threshold),
                    true);
            }

            public int MaxCentralHalfAxisContourRuns(float threshold)
            {
                var profile = this;
                return EvaluateCentralHalfAxes((startX, startY, stepX, stepY) =>
                    profile.CountContourRuns(startX, startY, stepX, stepY,
                        threshold),
                    false);
            }

            public int MinimumCentralHalfAxisContourPixels(float threshold)
            {
                var profile = this;
                return EvaluateCentralHalfAxes((startX, startY, stepX, stepY) =>
                    profile.CountContourPixels(startX, startY, stepX, stepY,
                        threshold), true);
            }

            public int MaxCentralHalfAxisContourPixels(float threshold)
            {
                var profile = this;
                return EvaluateCentralHalfAxes((startX, startY, stepX, stepY) =>
                    profile.CountContourPixels(startX, startY, stepX, stepY,
                        threshold), false);
            }

            public int MaxCentralHalfAxisStrongTransitionRuns(float threshold)
            {
                var profile = this;
                return EvaluateCentralHalfAxes((startX, startY, stepX, stepY) =>
                    profile.CountStrongTransitionRuns(startX, startY, stepX,
                        stepY, threshold), false);
            }

            public float MaxCentralHalfAxisColorPathExcess(float minimumStep)
            {
                var lower = Size / 2 - 1;
                var upper = Size / 2;
                var maximum = 0f;
                for (var axis = lower; axis <= upper; axis++)
                {
                    maximum = Mathf.Max(maximum,
                        ColorPathExcess(lower, axis, -1, 0, minimumStep));
                    maximum = Mathf.Max(maximum,
                        ColorPathExcess(upper, axis, 1, 0, minimumStep));
                    maximum = Mathf.Max(maximum,
                        ColorPathExcess(axis, lower, 0, -1, minimumStep));
                    maximum = Mathf.Max(maximum,
                        ColorPathExcess(axis, upper, 0, 1, minimumStep));
                }
                return maximum;
            }

            private int EvaluateCentralHalfAxes(
                Func<int, int, int, int, int> measure, bool minimum)
            {
                var lower = Size / 2 - 1;
                var upper = Size / 2;
                var result = minimum ? int.MaxValue : 0;
                for (var axis = lower; axis <= upper; axis++)
                {
                    Accumulate(measure(lower, axis, -1, 0));
                    Accumulate(measure(upper, axis, 1, 0));
                    Accumulate(measure(axis, lower, 0, -1));
                    Accumulate(measure(axis, upper, 0, 1));
                }
                return result == int.MaxValue ? 0 : result;

                void Accumulate(int value)
                {
                    result = minimum
                        ? Mathf.Min(result, value) : Mathf.Max(result, value);
                }
            }

            private int CountContourRuns(int startX, int startY,
                int stepX, int stepY, float threshold)
            {
                var reference = CenterReferenceColor();
                var runs = 0;
                var inRun = false;
                for (int x = startX, y = startY;
                     x >= 0 && x < Size && y >= 0 && y < Size;
                     x += stepX, y += stepY)
                {
                    var index = y * Size + x;
                    if (!mask[index]) break;
                    var contour = ColorDistance(colors[index], reference)
                        >= threshold;
                    if (contour && !inRun) runs++;
                    inRun = contour;
                }
                return runs;
            }

            private int CountStrongTransitionRuns(int startX, int startY,
                int stepX, int stepY, float threshold)
            {
                var runs = 0;
                var inRun = false;
                var previous = -1;
                for (int x = startX, y = startY;
                     x >= 0 && x < Size && y >= 0 && y < Size;
                     x += stepX, y += stepY)
                {
                    var index = y * Size + x;
                    if (!mask[index]) break;
                    if (previous < 0)
                    {
                        previous = index;
                        continue;
                    }
                    var strong = ColorDistance(colors[previous], colors[index])
                        >= threshold;
                    if (strong && !inRun) runs++;
                    inRun = strong;
                    previous = index;
                }
                return runs;
            }

            private int CountContourPixels(int startX, int startY,
                int stepX, int stepY, float threshold)
            {
                var reference = CenterReferenceColor();
                var count = 0;
                for (int x = startX, y = startY;
                     x >= 0 && x < Size && y >= 0 && y < Size;
                     x += stepX, y += stepY)
                {
                    var index = y * Size + x;
                    if (!mask[index]) break;
                    if (ColorDistance(colors[index], reference) >= threshold)
                        count++;
                }
                return count;
            }

            private float ColorPathExcess(int startX, int startY,
                int stepX, int stepY, float minimumStep)
            {
                var reference = CenterReferenceColor();
                var path = 0f;
                var maximumDistance = 0f;
                var previous = -1;
                for (int x = startX, y = startY;
                     x >= 0 && x < Size && y >= 0 && y < Size;
                     x += stepX, y += stepY)
                {
                    var index = y * Size + x;
                    if (!mask[index]) break;
                    maximumDistance = Mathf.Max(maximumDistance,
                        ColorDistance(reference, colors[index]));
                    if (previous >= 0)
                    {
                        var step = ColorDistance(colors[previous], colors[index]);
                        if (step >= minimumStep) path += step;
                    }
                    previous = index;
                }
                return maximumDistance <= 0f ? 0f : path / maximumDistance;
            }

            private Color CenterReferenceColor()
            {
                var region = CenterSquare(8);
                var sum = Color.clear;
                var count = 0;
                for (var y = region.yMin; y < region.yMax; y++)
                {
                    for (var x = region.xMin; x < region.xMax; x++)
                    {
                        var index = y * Size + x;
                        if (!mask[index]) continue;
                        sum += colors[index];
                        count++;
                    }
                }
                return count == 0 ? Color.clear : sum / count;
            }

            private int CountRowStrongEdges(int y, float threshold)
            {
                var count = 0;
                for (var x = 0; x + 1 < Size; x++)
                {
                    var index = y * Size + x;
                    if (mask[index] && mask[index + 1]
                        && ColorDistance(colors[index], colors[index + 1])
                        >= threshold)
                        count++;
                }
                return count;
            }

            private int CountColumnStrongEdges(int x, float threshold)
            {
                var count = 0;
                for (var y = 0; y + 1 < Size; y++)
                {
                    var index = y * Size + x;
                    if (mask[index] && mask[index + Size]
                        && ColorDistance(colors[index], colors[index + Size])
                        >= threshold)
                        count++;
                }
                return count;
            }

            private static float ColorDistance(Color left, Color right)
            {
                return Mathf.Max(Mathf.Abs(left.r - right.r),
                    Mathf.Abs(left.g - right.g),
                    Mathf.Abs(left.b - right.b));
            }

            private static bool IsSet(bool[] values, int size, int x, int y)
            {
                return x >= 0 && x < size && y >= 0 && y < size
                    && values[y * size + x];
            }

            private static void AnalyzeComponents(bool[] values, int size,
                bool target, out int componentCount, out int largestComponent)
            {
                var visited = new bool[values.Length];
                var queue = new Queue<int>();
                componentCount = 0;
                largestComponent = 0;
                for (var start = 0; start < values.Length; start++)
                {
                    if (visited[start] || values[start] != target) continue;
                    componentCount++;
                    var count = Flood(values, visited, queue, size, start, target);
                    largestComponent = Mathf.Max(largestComponent, count);
                }
            }

            private static int CountEnclosedHoles(bool[] values, int size)
            {
                var exterior = new bool[values.Length];
                var queue = new Queue<int>();
                for (var x = 0; x < size; x++)
                {
                    FloodIfBackground(values, exterior, queue, size, x);
                    FloodIfBackground(values, exterior, queue, size,
                        (size - 1) * size + x);
                }
                for (var y = 0; y < size; y++)
                {
                    FloodIfBackground(values, exterior, queue, size, y * size);
                    FloodIfBackground(values, exterior, queue, size,
                        y * size + size - 1);
                }

                var holes = 0;
                for (var index = 0; index < values.Length; index++)
                {
                    if (values[index] || exterior[index]) continue;
                    holes++;
                    Flood(values, exterior, queue, size, index, false);
                }
                return holes;
            }

            private static void FloodIfBackground(bool[] values, bool[] visited,
                Queue<int> queue, int size, int start)
            {
                if (!values[start] && !visited[start])
                    Flood(values, visited, queue, size, start, false);
            }

            private static int Flood(bool[] values, bool[] visited,
                Queue<int> queue, int size, int start, bool target)
            {
                var count = 0;
                visited[start] = true;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var index = queue.Dequeue();
                    count++;
                    var x = index % size;
                    var y = index / size;
                    Enqueue(index - 1, x > 0);
                    Enqueue(index + 1, x + 1 < size);
                    Enqueue(index - size, y > 0);
                    Enqueue(index + size, y + 1 < size);
                }
                return count;

                void Enqueue(int index, bool inBounds)
                {
                    if (!inBounds || visited[index] || values[index] != target)
                        return;
                    visited[index] = true;
                    queue.Enqueue(index);
                }
            }

        }

        [Serializable]
        private sealed class CompactArtManifest
        {
            public int slotCount;
            public CompactManifestBinding[] bindings;
        }

        [Serializable]
        private sealed class CompactManifestBinding
        {
            public string semantic_id;
            public string geometry;
            public string source;
            public string runtime;
            public string sourceSha256;
            public string runtimeSha256;
            public int slot;
            public string shared_from_set;
            public string imagegen_provider;
            public string imagegen_output;
            public string prompt_record;
        }
    }
}
