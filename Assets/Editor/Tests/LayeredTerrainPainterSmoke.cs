using System;
using System.Linq;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public static class LayeredTerrainPainterSmoke
    {
        public static void Validate()
        {
            LayeredTerrainArtSetup.EnsurePaletteAssets();
            var first = CreateRig("TerrainPainterSmoke-SharedEdge", true, false);
            var second = CreateRig("TerrainPainterSmoke-B", true, true);
            var missingDirection = CreateRig("TerrainPainterSmoke-MissingEdge", false, false);
            try
            {
                ValidatePresentation(first);
                ValidateTargetResolution(first, second);
                ValidateRegisteredBrushApplication();
                ValidateRefinedPresetsAndPreviews(first, second, missingDirection);
                ValidateAdvancedOperations(first);
                ValidateUniformEdgeRegions(first);
                ValidateGestureUndo(first);
                ValidateResponsiveHoverState(first);
                ValidateContourChoicesAndUndo(first);
                ValidateEmbeddedWorkspace(first);
                ValidateAcceptanceSceneProfile();
                Debug.Log("FRUIT_DEFENSE_LAYERED_TERRAIN_PAINTER_OK");
            }
            finally
            {
                Undo.ClearAll();
                first.Dispose();
                second.Dispose();
                missingDirection.Dispose();
            }
        }

        private static void ValidatePresentation(TestRig rig)
        {
            Assert(rig.Component.ValidateAuthoringPresentation(out var reason), reason);
            Assert(rig.Component.MaterialDisplayName(LayeredTerrainMaterial.A) == "草地"
                && rig.Component.MaterialDisplayName(LayeredTerrainMaterial.B) == "泥土"
                && rig.Component.MaterialPreview(LayeredTerrainMaterial.A) != null
                && rig.Component.MaterialPreview(LayeredTerrainMaterial.B) != null,
                "configured names and previews are author-ready");

            rig.Component.ConfigureAuthoringPresentation(string.Empty, rig.GrassBase.sprite,
                Color.green, "泥土", rig.SoilBase.sprite, Color.gray);
            Assert(!rig.Component.ValidateAuthoringPresentation(out reason)
                && reason.Contains("display names"), "missing display name is rejected");
            rig.Component.ConfigureAuthoringPresentation("地貌", rig.GrassBase.sprite,
                Color.green, "地貌", rig.SoilBase.sprite, Color.gray);
            Assert(!rig.Component.ValidateAuthoringPresentation(out reason)
                && reason.Contains("distinct"), "duplicate display names are rejected");
            rig.Component.ConfigureAuthoringPresentation("草地", null, Color.clear,
                "泥土", rig.SoilBase.sprite, Color.gray);
            Assert(!rig.Component.ValidateAuthoringPresentation(out reason)
                && reason.Contains("thumbnail"), "missing preview and swatch is rejected");
            rig.ConfigurePresentation();
            Assert(rig.Component.ValidateAuthoringPresentation(out reason), reason);
            Assert(rig.Component.TryGetBasePreviewSprite(LayeredTerrainMaterial.A,
                    out var grassBasePreview)
                && grassBasePreview == rig.GrassBase.sprite,
                "pure-only brush mode resolves the real configured base Sprite");
            rig.Component.ConfigureBaseVisuals(rig.SoilBase, rig.GrassBase);
            Assert(rig.Component.TryGetBasePreviewSprite(LayeredTerrainMaterial.A,
                    out var configuredBrushPureA)
                && configuredBrushPureA == rig.SoilBase.sprite
                && rig.Component.TryGetBasePreviewSprite(LayeredTerrainMaterial.B,
                    out var configuredBrushPureB)
                && configuredBrushPureB == rig.GrassBase.sprite,
                "pure-only mode follows the current brush's configured pure endpoints");
            rig.Component.ConfigureBaseVisuals(rig.GrassBase, rig.SoilBase);
        }

        private static void ValidateTargetResolution(TestRig first, TestRig second)
        {
            Assert(LayeredTerrainPainterWindow.ResolveInitialTarget(null,
                    new[] { first.Component }) == first.Component,
                "sole valid scene target is selected automatically");
            Assert(LayeredTerrainPainterWindow.ResolveInitialTarget(first.Component,
                    new[] { first.Component, second.Component }) == first.Component,
                "explicitly selected valid target wins");
            Assert(LayeredTerrainPainterWindow.ResolveInitialTarget(null,
                    new[] { first.Component, second.Component }) == null,
                "ambiguous valid targets require explicit selection");
        }

        private static void ValidateRegisteredBrushApplication()
        {
            var palette = LayeredTerrainArtSetup.EnsurePaletteAssets();
            var definitions = TerrainBrushRegistry.FindAll();
            var choices = TerrainBrushRegistry.FindPaintChoices();
            var grassSoil = definitions.Single(value => value.BrushId
                == "terrain-brush.grass-on-soil");
            var stoneWater = definitions.Single(value => value.BrushId
                == "terrain-brush.stone-on-water");
            var original = definitions.Single(value => value.BrushId
                == LayeredTerrainArtSetup.OriginalBrushId);
            Assert(definitions.Count == 3 && choices.Count == 6
                && choices.GroupBy(value => value.Definition).All(group => group.Count() == 2)
                && choices.All(value => TerrainBrushRegistry.IsPaintChoiceAvailable(
                    value, palette, out _)),
                "every registered resource contributes two directly paintable choices");
            using (var grassRig = CreateRig("TerrainPainterSmoke-RegisteredGrass", true, true))
            {
                Assert(!grassRig.Component.HasAuthoredCells(),
                    "a new laboratory target starts empty");
                var forward = choices.Single(value => value.Definition == grassSoil
                    && !value.Reverse);
                var reverse = choices.Single(value => value.Definition == grassSoil
                    && value.Reverse);
                Assert(LayeredTerrainSceneLaboratory.TrySelectPaintChoice(forward,
                    grassRig.Component, palette, out var reason), reason);
                Assert(grassRig.Component.MaterialDisplayName(LayeredTerrainMaterial.A)
                        == grassSoil.LandformDisplayName
                    && grassRig.Component.MaterialDisplayName(LayeredTerrainMaterial.B)
                        == grassSoil.BaseDisplayName
                    && LayeredTerrainSceneLaboratory.IsPainting
                    && LayeredTerrainSceneLaboratory.ActivePaintChoiceId == forward.ChoiceId,
                    "one grass-soil tile configures its resource and starts painting");
                Assert(grassRig.Component.CanPaintPair(LayeredTerrainMaterial.A,
                        LayeredTerrainMaterial.B, true, out reason)
                    && grassRig.Component.CanPaintPair(LayeredTerrainMaterial.B,
                        LayeredTerrainMaterial.A, true, out reason),
                    "grass-soil exposes both valid landform directions");
                Assert(grassRig.Component.PaintPair(new Vector3Int(1, 1, 0),
                    LayeredTerrainMaterial.A, LayeredTerrainMaterial.B, true, out reason), reason);
                var authoredCell = new Vector3Int(1, 1, 0);
                var baseMarkerBeforeSwitch = grassRig.Component.BaseLogicalTilemap
                    .GetTile(authoredCell);
                var landformMarkerBeforeSwitch = grassRig.Component.LandformLogicalTilemap
                    .GetTile(authoredCell);
                Assert(LayeredTerrainSceneLaboratory.TrySelectPaintChoice(reverse,
                        grassRig.Component, palette, out reason)
                    && LayeredTerrainSceneLaboratory.ActivePaintChoiceId == reverse.ChoiceId,
                    "the reciprocal tile switches direction without clearing matching cells");
                var stoneForward = choices.Single(value => value.Definition == stoneWater
                    && !value.Reverse);
                Assert(LayeredTerrainSceneLaboratory.TrySelectPaintChoice(stoneForward,
                        grassRig.Component, palette, out reason)
                    && LayeredTerrainSceneLaboratory.ActivePaintChoiceId
                        == stoneForward.ChoiceId
                    && TerrainBrushLaboratoryRegistration.Matches(stoneWater,
                        grassRig.Component, palette)
                    && grassRig.Component.BaseLogicalTilemap.GetTile(authoredCell)
                        == baseMarkerBeforeSwitch
                    && grassRig.Component.LandformLogicalTilemap.GetTile(authoredCell)
                        == landformMarkerBeforeSwitch
                    && grassRig.Component.HasAuthoredCells(),
                    "a non-empty laboratory switches resources immediately while preserving logical cells: "
                        + reason);
                LayeredTerrainSceneLaboratory.Close();
            }
            using (var stoneRig = CreateRig("TerrainPainterSmoke-RegisteredStone", true, true))
            {
                var stoneReverse = choices.Single(value => value.Definition == stoneWater
                    && value.Reverse);
                Assert(LayeredTerrainSceneLaboratory.TrySelectPaintChoice(stoneReverse,
                    stoneRig.Component, palette, out var reason), reason);
                Assert(stoneRig.Component.CanPaintPair(LayeredTerrainMaterial.A,
                        LayeredTerrainMaterial.B, true, out reason)
                    && stoneRig.Component.CanPaintPair(LayeredTerrainMaterial.B,
                        LayeredTerrainMaterial.A, true, out reason),
                    "stone-water uses its registered complemented view in both directions");
                Assert(stoneRig.Component.PaintPair(new Vector3Int(2, 2, 0),
                    LayeredTerrainMaterial.B, LayeredTerrainMaterial.A, true, out reason), reason);
                Assert(TerrainBrushLaboratoryRegistration.TryClear(stoneRig.Component,
                        out reason) && !stoneRig.Component.HasAuthoredCells(), reason);
                LayeredTerrainSceneLaboratory.Close();
            }
            using (var originalRig = CreateRig("TerrainPainterSmoke-OriginalSquare", true, true))
            {
                var originalForward = choices.Single(value => value.Definition == original
                    && !value.Reverse);
                Assert(LayeredTerrainSceneLaboratory.TrySelectPaintChoice(originalForward,
                        originalRig.Component, palette, out var reason)
                    && originalRig.Component.ActiveContourStyleId
                        == BattlefieldLayerIds.ContourStyles.Square
                    && originalRig.Component.MaterialPreview(LayeredTerrainMaterial.A)
                        == originalRig.GrassBase.sprite
                    && originalRig.Component.MaterialPreview(LayeredTerrainMaterial.B)
                        == originalRig.SoilBase.sprite,
                    "the initial square terrain image remains a direct registered choice: "
                        + reason);
                LayeredTerrainSceneLaboratory.Close();
            }
        }

        private static void ValidateEmbeddedWorkspace(TestRig rig)
        {
            Assert(!typeof(EditorWindow).IsAssignableFrom(typeof(LayeredTerrainPainterWindow)),
                "terrain laboratory launch API no longer creates a standalone EditorWindow");
            Assert(typeof(IMGUIOverlay).IsAssignableFrom(
                    typeof(LayeredTerrainResourceAcceptanceOverlay))
                && LayeredTerrainResourceAcceptanceOverlay.Title.Contains("资源验收"),
                "terrain resource acceptance uses a native Scene IMGUIOverlay");
            var compactOverlay = new LayeredTerrainResourceAcceptanceOverlay();
            Assert(compactOverlay.defaultSize.x <= 340f
                && compactOverlay.defaultSize.y <= 320f
                && compactOverlay.maxSize.x <= 420f
                && compactOverlay.layout == Layout.Panel
                && !compactOverlay.collapsed,
                "terrain resource acceptance opens expanded as a compact, bounded panel");
            Assert(typeof(LayeredTerrainSceneLaboratory).GetMethod("DrawContourChoice",
                    System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Static) == null,
                "ordinary terrain laboratory exposes no square or organic contour switch");
            Assert(typeof(LayeredTerrainSceneLaboratory).GetMethod("CalculatePanelRect",
                    System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Static) == null
                && typeof(LayeredTerrainPaintSession).GetMethod("SetReservedGuiRect") == null,
                "native Overlay owns layout and input instead of hand-positioned panel geometry");
            Assert(LayeredTerrainSceneLaboratory.ResourceBoundaryMessage.Contains("不生成可玩地图")
                && LayeredTerrainSceneLaboratory.ResourceBoundaryMessage.Contains("关卡地图编辑器")
                && LayeredTerrainSceneLaboratory.ContourDisplayName(
                    BattlefieldLayerIds.ContourStyles.Square) == "方形"
                && LayeredTerrainSceneLaboratory.ContourDisplayName(
                    BattlefieldLayerIds.ContourStyles.Organic) == "自然",
                "resource acceptance states its boundary and exposes configured contour read-only");
            Assert(typeof(LayeredTerrainSceneLaboratory).GetMethod("DrawRegisteredBrushes",
                        System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Static) == null
                    && typeof(LayeredTerrainSceneLaboratory).GetMethod("DrawPrimaryPresets",
                        System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Static) == null
                    && typeof(LayeredTerrainSceneLaboratory).GetMethod("DrawPureBaseOnlyOption",
                        System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Static) == null,
                "the resource selector, second direction selector, and primary pure toggle are removed");
            var registered = LayeredTerrainSceneLaboratory.RegisteredBrushes();
            var paintChoices = LayeredTerrainSceneLaboratory.RegisteredPaintChoices();
            var registeredHeight = LayeredTerrainSceneLaboratory.CalculatePreviewGridHeight(
                paintChoices.Count, LayeredTerrainSceneLaboratory.RegisteredBrushColumnCount,
                LayeredTerrainSceneLaboratory.RegisteredBrushCardHeight,
                LayeredTerrainSceneLaboratory.RegisteredBrushGap);
            var registeredRects = LayeredTerrainSceneLaboratory.CalculateBrushPreviewRects(
                new Rect(0f, 0f, 300f,
                    LayeredTerrainSceneLaboratory.RegisteredBrushCardHeight),
                paintChoices.Count,
                LayeredTerrainSceneLaboratory.RegisteredBrushColumnCount,
                LayeredTerrainSceneLaboratory.RegisteredBrushGap);
            Assert(registered.Count == 3 && paintChoices.Count == 6
                && LayeredTerrainSceneLaboratory.RegisteredBrushColumnCount == 4
                && registeredHeight == 176f
                && registeredRects.Length == paintChoices.Count
                && paintChoices.Take(2).Select(value => value.Definition).Distinct().Count() == 1
                && registeredRects[0].y == registeredRects[1].y
                && registeredRects[2].y == registeredRects[0].y
                && registeredRects[3].y == registeredRects[0].y
                && registeredRects[4].y == registeredRects[0].yMax + 4f,
                "one compact four-column gallery lays out both directions of every registered resource");
            var futureHeight = LayeredTerrainSceneLaboratory.CalculatePreviewGridHeight(
                10, LayeredTerrainSceneLaboratory.RegisteredBrushColumnCount,
                LayeredTerrainSceneLaboratory.RegisteredBrushCardHeight,
                LayeredTerrainSceneLaboratory.RegisteredBrushGap);
            var futureRects = LayeredTerrainSceneLaboratory.CalculateBrushPreviewRects(
                new Rect(0f, 0f, 300f,
                    LayeredTerrainSceneLaboratory.RegisteredBrushCardHeight), 10,
                LayeredTerrainSceneLaboratory.RegisteredBrushColumnCount,
                LayeredTerrainSceneLaboratory.RegisteredBrushGap);
            Assert(futureHeight == 266f
                && futureRects[0].y == futureRects[1].y
                && futureRects[3].y == futureRects[0].y
                && futureRects[4].y == futureRects[0].yMax + 4f
                && futureRects[9].yMax == futureHeight,
                "registered-brush gallery grows into scrollable rows for future imports");
            var compactArtwork = LayeredTerrainSceneLaboratory.CalculateCenteredSquareRect(
                new Rect(registeredRects[0].x + 3f, registeredRects[0].y + 3f,
                    registeredRects[0].width - 6f, registeredRects[0].height - 43f));
            Assert(Mathf.Approximately(registeredRects[0].width, 72f)
                && Mathf.Approximately(compactArtwork.width, 43f)
                && compactArtwork.width < 80f,
                "four-column cards keep the centered artwork substantially smaller than the former two-column preview");
            var squareArtwork = LayeredTerrainSceneLaboratory.CalculateCenteredSquareRect(
                new Rect(3f, 3f, 112f, 80f));
            Assert(Mathf.Approximately(squareArtwork.width, squareArtwork.height)
                && Mathf.Approximately(squareArtwork.width, 80f)
                && Mathf.Approximately(squareArtwork.center.x, 59f)
                && Mathf.Approximately(squareArtwork.center.y, 43f),
                "registered-brush artwork is centered and never stretched by the footer");

            try
            {
                LayeredTerrainPainterWindow.Open(rig.Component);
                Assert(LayeredTerrainSceneLaboratory.IsOpen
                    && LayeredTerrainSceneLaboratory.Target == rig.Component
                    && LayeredTerrainSceneLaboratory.HasNativeOverlay
                    && LayeredTerrainSceneLaboratory.NativeOverlayInstanceCount == 1,
                    "compatibility launch API activates one native Scene Overlay");
                var firstOverlay = LayeredTerrainSceneLaboratory.ActiveOverlay;
                LayeredTerrainPainterWindow.Open(rig.Component);
                Assert(ReferenceEquals(firstOverlay, LayeredTerrainSceneLaboratory.ActiveOverlay)
                    && LayeredTerrainSceneLaboratory.NativeOverlayInstanceCount == 1,
                    "repeated launch reuses the same Overlay and paint session");
                LayeredTerrainPainterWindow.PrepareAcceptanceView();
                Assert(LayeredTerrainSceneLaboratory.IsPainting,
                    "resource-acceptance Overlay owns one active paint session");
                LayeredTerrainSceneLaboratory.SetCollapsed(false);
                Assert(LayeredTerrainSceneLaboratory.IsPainting
                    && LayeredTerrainSceneLaboratory.Target == rig.Component
                    && LayeredTerrainSceneLaboratory.ActiveOverlay.displayed
                    && LayeredTerrainSceneLaboratory.ActiveOverlay.layout == Layout.Panel
                    && typeof(LayeredTerrainSceneLaboratory).GetMethod(
                        "OnOverlayCollapsed", System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Static) != null,
                    "active terrain Overlay keeps its expanded-panel recovery contract and paint session");
            }
            finally
            {
                LayeredTerrainSceneLaboratory.Close();
            }
            Assert(!LayeredTerrainSceneLaboratory.IsOpen
                && !LayeredTerrainSceneLaboratory.IsPainting
                && LayeredTerrainSceneLaboratory.Target == null
                && !LayeredTerrainSceneLaboratory.HasNativeOverlay,
                "closing resource acceptance releases its Overlay, session, and target");
        }

        private static void ValidateRefinedPresetsAndPreviews(TestRig rig,
            TestRig exactReverseCompatibility, TestRig missingDirection)
        {
            using (var session = new LayeredTerrainPaintSession())
            {
                session.SetTarget(rig.Component);
                Assert(session.Start(out var reason), reason);

                session.SetPureBaseOnly(true);
                var pureA = new Vector3Int(0, 0, 0);
                Paint(session, LayeredTerrainPainterTool.LandformA, pureA);
                Assert(rig.BaseLogical.GetTile(pureA) == rig.MarkerA
                    && rig.BaseOutput.GetTile(pureA) == rig.GrassBase
                    && !rig.LandformLogical.HasTile(pureA)
                    && !rig.EdgeLogical.HasTile(pureA),
                    "ordinary grass brush in pure-only mode writes only material A base");

                var pureB = new Vector3Int(1, 0, 0);
                Paint(session, LayeredTerrainPainterTool.LandformB, pureB);
                Assert(rig.BaseLogical.GetTile(pureB) == rig.MarkerB
                    && !rig.LandformLogical.HasTile(pureB)
                    && session.ActiveToolLabel.Contains("只绘制纯图"),
                    "ordinary soil brush exposes and applies pure-only mode");
                session.SetPureBaseOnly(false);

                var aOnB = new Vector3Int(2, 0, 0);
                Paint(session, LayeredTerrainPainterTool.AOnB, aOnB);
                Assert(rig.BaseLogical.GetTile(aOnB) == rig.MarkerB
                    && rig.LandformLogical.GetTile(aOnB) == rig.MarkerA
                    && rig.EdgeLogical.GetTile(aOnB) == rig.EdgeMarker,
                    "grass-on-soil preset writes exact A-on-B composition");

                var bOnA = new Vector3Int(3, 0, 0);
                Paint(session, LayeredTerrainPainterTool.BOnA, bOnA);
                Assert(rig.BaseLogical.GetTile(bOnA) == rig.MarkerA
                    && rig.LandformLogical.GetTile(bOnA) == rig.MarkerB
                    && rig.EdgeLogical.GetTile(bOnA) == rig.EdgeMarker,
                    "soil-on-grass preset writes exact reverse composition");

                Assert(LayeredTerrainPainterToolUtility.ContainsLandform(
                        LayeredTerrainPainterTool.LandformA)
                    && LayeredTerrainPainterToolUtility.ContainsLandform(
                        LayeredTerrainPainterTool.AOnB)
                    && !session.ActiveToolLabel.Contains("边缘"),
                    "primary brush labels contain no separate pure preset or refinement mode");

                Assert(rig.Component.TryGetRefinedPreviewSources(
                        LayeredTerrainMaterial.A, LayeredTerrainMaterial.B,
                        out var background, out var landformSet, out var edgeSet, out reason)
                    && background == rig.SoilBase.sprite
                    && landformSet != null && edgeSet != null,
                    "grass-on-soil preview resolves real base, active contour, and edge sources");
                var representativeMasks = new[]
                {
                    DualGridMask.SouthEast, DualGridMask.SouthWest,
                    DualGridMask.NorthEast, DualGridMask.NorthWest,
                };
                Assert(representativeMasks.All(mask =>
                        landformSet.TryGetSprite(mask, out _)
                        && edgeSet.TryGetSprite(mask, out _)),
                    "pair-card island preview has renderable real sprites for every quadrant");
                Assert(rig.Component.CanPaintPair(LayeredTerrainMaterial.A,
                    LayeredTerrainMaterial.B, true, out reason), reason);
                Assert(rig.Component.TryGetRefinedPreviewSources(
                        LayeredTerrainMaterial.B, LayeredTerrainMaterial.A,
                        out var reverseBackground, out var reverseLandformSet,
                        out var sharedReverseEdgeSet, out var complementReverseMask,
                        out reason)
                    && reverseBackground == rig.GrassBase.sprite
                    && reverseLandformSet != null
                    && ReferenceEquals(sharedReverseEdgeSet, edgeSet)
                    && complementReverseMask,
                    "one edge TileSet enables the reverse brush through complemented masks");
                Assert(DualGridMaskUtility.Complement(DualGridMask.NorthWest)
                        == (DualGridMask.NorthEast | DualGridMask.SouthEast
                            | DualGridMask.SouthWest)
                    && DualGridMaskUtility.Complement(DualGridMask.Full)
                        == DualGridMask.Empty,
                    "reverse edge masks use the four-corner complement contract");

                Assert(exactReverseCompatibility.Component.TryGetRefinedPreviewSources(
                        LayeredTerrainMaterial.B, LayeredTerrainMaterial.A,
                        out _, out _, out var exactReverseEdgeSet,
                        out var exactReverseUsesComplement, out reason)
                    && ReferenceEquals(exactReverseEdgeSet,
                        Require<DualGridTileSet>(
                            LayeredTerrainArtSetup.SoilOnGrassEdgeTileSetPath))
                    && !exactReverseUsesComplement,
                    "legacy exact reverse resources remain compatibility overrides");
                session.Stop();
                Assert(!session.IsActive, "stopped session releases Scene painting state");
            }

            using (var session = new LayeredTerrainPaintSession())
            {
                session.SetTarget(missingDirection.Component);
                session.SetTool(LayeredTerrainPainterTool.AOnB);
                Assert(!session.CanUseTool(LayeredTerrainPainterTool.AOnB, out var reason)
                    && reason.Contains("A on B"),
                    "a pair brush is disabled only when neither edge direction exists");
                Assert(!missingDirection.Component.CanPaintPair(
                        LayeredTerrainMaterial.A, LayeredTerrainMaterial.B, true, out reason)
                    && reason.Contains("A on B"),
                    "missing pair resources report the exact unavailable direction");
                session.SetPureBaseOnly(true);
                Assert(session.CanUseTool(LayeredTerrainPainterTool.AOnB, out reason),
                    "pure-only mode remains available without a directed pair edge");
                Assert(missingDirection.Component.TryGetBasePreviewSprite(
                        LayeredTerrainMaterial.A, out _),
                    "the advanced pure-only operation still resolves its base preview");
                session.SetPureBaseOnly(false);
                session.SetTool(LayeredTerrainPainterTool.BOnA);
                Assert(!session.CanUseTool(LayeredTerrainPainterTool.BOnA, out reason)
                    && reason.Contains("B on A"),
                    "the opposite brush is also unavailable when the material pair has no edge");
            }
        }

        private static void ValidateAdvancedOperations(TestRig rig)
        {
            using (var session = new LayeredTerrainPaintSession())
            {
                session.SetTarget(rig.Component);
                Assert(session.Start(out var reason), reason);

                var empty = new Vector3Int(9, 9, 0);
                session.SetTool(LayeredTerrainPainterTool.LandformA);
                Assert(session.BeginGesture(out reason), reason);
                Assert(!session.ApplyCell(empty, out reason) && reason.Contains("base"),
                    "landform-only paint rejects an empty base with guidance");
                session.EndGesture();
                Assert(!rig.LandformLogical.HasTile(empty),
                    "rejected landform-only operation leaves cell unchanged");

                var cell = new Vector3Int(4, 2, 0);
                Paint(session, LayeredTerrainPainterTool.AOnB, cell);
                Paint(session, LayeredTerrainPainterTool.EraseLandform, cell);
                Assert(rig.BaseLogical.GetTile(cell) == rig.MarkerB
                    && !rig.LandformLogical.HasTile(cell)
                    && !rig.EdgeLogical.HasTile(cell),
                    "erase-landform preserves the base");
                Paint(session, LayeredTerrainPainterTool.ClearCell, cell);
                Assert(!rig.BaseLogical.HasTile(cell) && !rig.BaseOutput.HasTile(cell),
                    "clear-cell removes canonical and generated base state");
                session.Stop();
            }
        }

        private static void ValidateGestureUndo(TestRig rig)
        {
            var first = new Vector3Int(5, 3, 0);
            var second = new Vector3Int(6, 3, 0);
            Assert(rig.Component.PaintBase(first, LayeredTerrainMaterial.B, out var reason), reason);
            Assert(rig.Component.PaintBase(second, LayeredTerrainMaterial.B, out reason), reason);
            Undo.ClearAll();

            using (var session = new LayeredTerrainPaintSession())
            {
                session.SetTarget(rig.Component);
                session.SetTool(LayeredTerrainPainterTool.AOnB);
                Assert(session.Start(out reason), reason);
                Assert(session.BeginGesture(out reason), reason);
                Assert(session.ApplyCell(first, out reason), reason);
                Assert(session.ApplyCell(second, out reason), reason);
                Assert(session.ApplyCell(first, out reason) && reason == "duplicate",
                    "one drag skips a cell already visited in the same gesture");
                session.EndGesture();
                Assert(session.LastCompletedGestureMutationCount == 2,
                    "one gesture records exactly two unique cell mutations");
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert(rig.BaseLogical.GetTile(first) == rig.MarkerB
                    && rig.BaseLogical.GetTile(second) == rig.MarkerB
                    && !rig.LandformLogical.HasTile(first)
                    && !rig.LandformLogical.HasTile(second),
                    "one Undo restores every cell from the drag");
                Undo.PerformRedo();
                Assert(rig.LandformLogical.GetTile(first) == rig.MarkerA
                    && rig.LandformLogical.GetTile(second) == rig.MarkerA
                    && rig.EdgeLogical.GetTile(first) == rig.EdgeMarker
                    && rig.EdgeLogical.GetTile(second) == rig.EdgeMarker
                    && rig.LandformAOutput.GetUsedTilesCount() > 0,
                    "Redo restores refined canonical and generated landform outputs");
                session.Stop();
            }
        }

        private static void ValidateResponsiveHoverState(TestRig rig)
        {
            using (var session = new LayeredTerrainPaintSession())
            {
                session.SetTarget(rig.Component);
                Assert(session.Start(out var reason), reason);
                var first = new Vector3Int(2, 3, 0);
                var second = new Vector3Int(3, 3, 0);
                Assert(session.SetHoveredCell(first, new Vector3(2.5f, 3.5f, 0f))
                    && session.HasHoveredCell && session.HoveredCell == first,
                    "first pointer cell creates transient hover state");
                Assert(!session.SetHoveredCell(first, new Vector3(2.5f, 3.5f, 0f)),
                    "repeated pointer events in one cell do not request redundant repaint state");
                Assert(session.SetHoveredCell(second, new Vector3(3.5f, 3.5f, 0f))
                    && session.HoveredCell == second,
                    "crossing a cell boundary updates the cached outline immediately");
                Assert(session.ClearHoveredCell() && !session.HasHoveredCell,
                    "panel or window boundaries clear the stale outline");

                var sceneView = ScriptableObject.CreateInstance<SceneView>();
                try
                {
                    sceneView.wantsMouseMove = false;
                    sceneView.wantsMouseEnterLeaveWindow = false;
                    session.EnsurePointerEvents(sceneView);
                    Assert(sceneView.wantsMouseMove && sceneView.wantsMouseEnterLeaveWindow
                        && session.TrackedMouseMoveSceneCount == 1,
                        "active painting opts the Scene view into pointer movement boundaries");
                    session.SetHoveredCell(first, Vector3.zero);
                    session.Stop();
                    Assert(!sceneView.wantsMouseMove && !sceneView.wantsMouseEnterLeaveWindow
                        && session.TrackedMouseMoveSceneCount == 0
                        && !session.HasHoveredCell,
                        "stop restores Scene pointer settings and releases hover state");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sceneView);
                }
            }
        }

        private static void ValidateUniformEdgeRegions(TestRig rig)
        {
            var first = new Vector3Int(10, 10, 0);
            var diagonal = new Vector3Int(11, 11, 0);
            Assert(rig.Component.PaintPair(first, LayeredTerrainMaterial.A,
                LayeredTerrainMaterial.B, true, out var reason), reason);
            Assert(rig.Component.PaintPair(diagonal, LayeredTerrainMaterial.A,
                LayeredTerrainMaterial.B, false, out reason), reason);
            Assert(!rig.EdgeLogical.HasTile(first) && !rig.EdgeLogical.HasTile(diagonal),
                "legacy bare-edge mutation remains readable and uniform for migration");

            Assert(rig.Component.PaintLandform(first, LayeredTerrainMaterial.A,
                true, out reason), reason);
            Assert(rig.EdgeLogical.HasTile(first) && rig.EdgeLogical.HasTile(diagonal),
                "one refined-edge gesture updates the complete exact pair region");

            rig.EdgeLogical.SetTile(diagonal, null);
            Assert(!rig.Component.ValidateConfiguration(out reason)
                && reason.Contains("partial edge refinement"),
                "terrain laboratory rejects a serialized per-cell partial edge region");
            Assert(rig.Component.ValidateAuthoringConfiguration(out reason), reason);
            Assert(rig.Component.Rebuild(out reason),
                "an inconsistent authored edge region remains editable and rebuildable: " + reason);
            rig.EdgeLogical.SetTile(diagonal, rig.EdgeMarker);
            Assert(rig.Component.Rebuild(out reason), reason);
        }

        private static void ValidateContourChoicesAndUndo(TestRig rig)
        {
            rig.LandformLogical.ClearAllTiles();
            rig.EdgeLogical.ClearAllTiles();
            Assert(rig.Component.Rebuild(out var resetReason), resetReason);
            Assert(rig.Component.AvailableContourStyleIds.Contains(
                    BattlefieldLayerIds.ContourStyles.Square)
                && rig.Component.AvailableContourStyleIds.Contains(
                    BattlefieldLayerIds.ContourStyles.Organic),
                "terrain laboratory exposes only its registered square and organic contours");

            Assert(rig.Component.TrySetContourStyle(
                BattlefieldLayerIds.ContourStyles.Organic, out var reason), reason);
            Assert(rig.Component.CanPaintPair(LayeredTerrainMaterial.B,
                    LayeredTerrainMaterial.A, true, out reason), reason);
            var reverseCell = new Vector3Int(8, 6, 0);
            Assert(rig.Component.PaintPair(reverseCell, LayeredTerrainMaterial.B,
                    LayeredTerrainMaterial.A, true, out reason), reason);
            Assert(rig.Component.CanSelectContourStyle(
                    BattlefieldLayerIds.ContourStyles.Square, out reason), reason);

            var objects = new UnityEngine.Object[]
            {
                rig.Component, rig.BaseLogical, rig.LandformLogical, rig.EdgeLogical,
                rig.BaseOutput, rig.LandformAOutput, rig.LandformBOutput,
                rig.EdgeAOnBOutput, rig.EdgeBOnAOutput,
            };
            Undo.ClearAll();
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(objects, "Smoke contour switch");
            Assert(rig.Component.TrySetContourStyle(
                BattlefieldLayerIds.ContourStyles.Square, out reason), reason);
            Undo.CollapseUndoOperations(group);
            Assert(rig.Component.ActiveContourStyleId
                    == BattlefieldLayerIds.ContourStyles.Square,
                "terrain laboratory switches the entire canvas to square");
            Assert(rig.Component.CanPaintPair(LayeredTerrainMaterial.A,
                    LayeredTerrainMaterial.B, true, out reason), reason);
            Assert(rig.Component.CanPaintPair(LayeredTerrainMaterial.B,
                    LayeredTerrainMaterial.A, true, out reason), reason);
            Assert(rig.Component.TryGetRefinedPreviewSources(
                    LayeredTerrainMaterial.B, LayeredTerrainMaterial.A,
                    out _, out _, out _, out var complemented, out reason)
                && complemented,
                "square reverse refinement reuses the current edge resource");
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert(rig.Component.ActiveContourStyleId
                    == BattlefieldLayerIds.ContourStyles.Organic,
                "one Undo restores the previous whole-canvas contour");
        }

        private static void ValidateAcceptanceSceneProfile()
        {
            var scene = SceneManager.GetSceneByPath(LayeredTerrainArtSetup.AcceptanceScenePath);
            var openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
                scene = EditorSceneManager.OpenScene(LayeredTerrainArtSetup.AcceptanceScenePath,
                    OpenSceneMode.Additive);
            try
            {
                LayeredTerrainTilemap found = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var candidate = root.GetComponentInChildren<LayeredTerrainTilemap>(true);
                    if (candidate == null) continue;
                    Assert(found == null, "acceptance scene contains one layered terrain painter target");
                    found = candidate;
                }
                Assert(found != null, "acceptance scene contains a layered terrain painter target");
                Assert(found.ValidateAuthoringPresentation(out var reason),
                    "acceptance scene has an author-ready painter profile: " + reason);
                Assert(found.AvailableContourStyleIds.SequenceEqual(new[]
                        { BattlefieldLayerIds.ContourStyles.Square })
                    && found.TryGetRefinedPreviewSources(LayeredTerrainMaterial.B,
                        LayeredTerrainMaterial.A, out _, out _, out _,
                        out var complemented, out reason)
                    && complemented,
                    "acceptance scene keeps only the current square edge family and serves both brushes");
            }
            finally
            {
                if (openedForValidation) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Paint(LayeredTerrainPaintSession session,
            LayeredTerrainPainterTool tool, Vector3Int cell)
        {
            session.SetTool(tool);
            Assert(session.BeginGesture(out var reason), reason);
            Assert(session.ApplyCell(cell, out reason), reason);
            session.EndGesture();
        }

        private static TestRig CreateRig(string name, bool aOnBEdge, bool bOnAEdge)
        {
            var rig = new TestRig(name);
            var grassSet = Require<DualGridTileSet>(LayeredTerrainArtSetup.GrassLandformTileSetPath);
            var soilSet = Require<DualGridTileSet>(LayeredTerrainArtSetup.SoilLandformTileSetPath);
            var grassEdge = aOnBEdge
                ? Require<DualGridTileSet>(LayeredTerrainArtSetup.GrassOnSoilEdgeTileSetPath) : null;
            var soilEdge = bOnAEdge
                ? Require<DualGridTileSet>(LayeredTerrainArtSetup.SoilOnGrassEdgeTileSetPath) : null;
            rig.Component.Configure(rig.BaseLogical, rig.LandformLogical, rig.EdgeLogical,
                rig.BaseOutput, rig.LandformAOutput, rig.LandformBOutput,
                rig.EdgeAOnBOutput, rig.EdgeBOnAOutput,
                rig.MarkerA, rig.MarkerB, rig.EdgeMarker, rig.GrassBase, rig.SoilBase,
                grassSet, soilSet, grassEdge, soilEdge);
            var squareGrass = Require<DualGridTileSet>(
                SquareTerrainArtProfile.GrassLandformTileSetPath);
            var squareSoil = Require<DualGridTileSet>(
                SquareTerrainArtProfile.SoilLandformTileSetPath);
            var squareEdge = aOnBEdge ? Require<DualGridTileSet>(
                SquareTerrainArtProfile.GrassOnSoilEdgeTileSetPath) : null;
            rig.Component.ConfigureContourBindings(new[]
            {
                new LayeredTerrainContourBinding(BattlefieldLayerIds.ContourStyles.Organic,
                    grassSet, soilSet, grassEdge, soilEdge),
                new LayeredTerrainContourBinding(BattlefieldLayerIds.ContourStyles.Square,
                    squareGrass, squareSoil, squareEdge, null),
            }, BattlefieldLayerIds.ContourStyles.Organic);
            rig.ConfigurePresentation();
            Assert(rig.Component.ValidateConfiguration(out var reason), reason);
            return rig;
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert(asset != null, typeof(T).Name + " asset is missing: " + path);
            return asset;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Layered terrain painter smoke failed: " + message);
        }

        private sealed class TestRig : IDisposable
        {
            public readonly GameObject Root;
            public readonly Tile MarkerA;
            public readonly Tile MarkerB;
            public readonly Tile EdgeMarker;
            public readonly Tile GrassBase;
            public readonly Tile SoilBase;
            public readonly Tilemap BaseLogical;
            public readonly Tilemap LandformLogical;
            public readonly Tilemap EdgeLogical;
            public readonly Tilemap BaseOutput;
            public readonly Tilemap LandformAOutput;
            public readonly Tilemap LandformBOutput;
            public readonly Tilemap EdgeAOnBOutput;
            public readonly Tilemap EdgeBOnAOutput;
            public readonly LayeredTerrainTilemap Component;

            public TestRig(string name)
            {
                Root = new GameObject(name);
                MarkerA = ScriptableObject.CreateInstance<Tile>();
                MarkerB = ScriptableObject.CreateInstance<Tile>();
                EdgeMarker = ScriptableObject.CreateInstance<Tile>();
                GrassBase = Require<Tile>(LayeredTerrainArtSetup.GrassBaseTilePath);
                SoilBase = Require<Tile>(LayeredTerrainArtSetup.SoilBaseTilePath);
                var gridObject = new GameObject("Grid");
                gridObject.transform.SetParent(Root.transform, false);
                gridObject.AddComponent<Grid>();
                BaseLogical = AddTilemap(gridObject.transform, "base source");
                LandformLogical = AddTilemap(gridObject.transform, "landform source");
                EdgeLogical = AddTilemap(gridObject.transform, "edge source");
                BaseOutput = AddTilemap(gridObject.transform, "base output");
                LandformAOutput = AddTilemap(gridObject.transform, "landform A output");
                LandformBOutput = AddTilemap(gridObject.transform, "landform B output");
                EdgeAOnBOutput = AddTilemap(gridObject.transform, "edge A on B output");
                EdgeBOnAOutput = AddTilemap(gridObject.transform, "edge B on A output");
                Component = Root.AddComponent<LayeredTerrainTilemap>();
            }

            public void ConfigurePresentation()
            {
                Component.ConfigureAuthoringPresentation("草地", GrassBase.sprite,
                    new Color(.31f, .76f, .24f, 1f), "泥土", SoilBase.sprite,
                    new Color(.61f, .38f, .2f, 1f));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
                UnityEngine.Object.DestroyImmediate(MarkerA);
                UnityEngine.Object.DestroyImmediate(MarkerB);
                UnityEngine.Object.DestroyImmediate(EdgeMarker);
            }

            private static Tilemap AddTilemap(Transform parent, string name)
            {
                var value = new GameObject(name);
                value.transform.SetParent(parent, false);
                return value.AddComponent<Tilemap>();
            }
        }
    }
}
