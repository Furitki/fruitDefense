using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FruitDefense.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FruitDefense.Editor
{
    public static class RuntimeUiVisualSystemSmoke
    {
        private const string FixtureRoot =
            "Assets/Editor/Tests/Fixtures/RuntimeUi";
        private const string InvalidPathFixture =
            FixtureRoot + "/GeneratedInvalidPathRuntimeUiArtSet.asset";

        public static void Run()
        {
            ValidateFinitePaintedSlotContract();
            ValidateNineSliceDevicePixelCoverage();
            var releaseTheme = RuntimeUiArtSetRegistry.LoadReleaseTheme();
            Assert(releaseTheme != null, "fixed release theme exists");
            ValidateNineSliceSourceUvContract();
            ValidateScreenBackgroundOpacityContract();
            ValidateLoadingPrimaryContrast(releaseTheme);
            ValidateSemanticStateContrast(releaseTheme);
            var production = releaseTheme.ActiveArtSet;
            Assert(production != null && RuntimeUiArtSetRegistry.IsProductionSet(production),
                "release theme starts with one production art set");

            var originalActive = production;
            var originalThemeBytes = ReadAssetBytes(RuntimeUiArtSetRegistry.ReleaseThemePath);
            var originalScenes = CaptureReleaseSceneBytes();
            var originalSources = CaptureAssetTreeBytes("Assets/Scripts", "*.cs");
            var originalLayouts = CaptureProtectedLayoutBytes();
            var originalThemeContract = CaptureThemeContractWithoutActive(releaseTheme);
            var originalThemeGuid = AssetDatabase.AssetPathToGUID(
                RuntimeUiArtSetRegistry.ReleaseThemePath);
            var originalThemeId = releaseTheme.ThemeId;
            var originalThemeRevision = releaseTheme.Revision;
            var productionSets = ValidateProductionRegistry();
            var transients = new List<RuntimeUiArtSet>();
            var invalidContractFixtures = new List<RuntimeUiArtSet>();
            RuntimeUiTheme previewTheme = null;
            RuntimeUiArtSet invalidPath = null;
            try
            {
                DeleteGeneratedFixture();
                ValidateFixtureMatrix(production, transients, invalidContractFixtures);
                ValidatePreviewIsolation(releaseTheme, production, originalThemeBytes,
                    originalScenes, out previewTheme);
                ValidateInvalidActivation(releaseTheme, production, originalThemeBytes,
                    originalScenes, transients, invalidContractFixtures, out invalidPath);
                AssertAssetBytesEqual(originalSources,
                    "invalid candidate activation changes no runtime or presenter code");
                AssertAssetBytesEqual(originalLayouts,
                    "invalid candidate activation changes no authoritative layout");
                ValidateProductionCandidateWorkflows(releaseTheme, productionSets,
                    originalActive, originalThemeBytes, originalThemeContract,
                    originalThemeGuid, originalThemeId, originalThemeRevision,
                    originalScenes, originalSources, originalLayouts);
                ValidateInPlaceReimport(releaseTheme, production, originalScenes);
                ValidateFixtureExclusion(releaseTheme);
            }
            finally
            {
                if (previewTheme != null) Object.DestroyImmediate(previewTheme);
                RestoreActiveWithoutUndo(releaseTheme, originalActive);
                Undo.ClearAll();
                DeleteGeneratedFixture();
                foreach (var fixture in transients.Where(value => value != null))
                {
                    if (!EditorUtility.IsPersistent(fixture)) Object.DestroyImmediate(fixture);
                }
                AssetDatabase.SaveAssetIfDirty(releaseTheme);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            Assert(BytesEqual(originalThemeBytes,
                    ReadAssetBytes(RuntimeUiArtSetRegistry.ReleaseThemePath)),
                "smoke restores release-theme serialization byte for byte");
            AssertSceneBytesEqual(originalScenes,
                "the complete smoke leaves all release scene bytes unchanged");
            AssertAssetBytesEqual(originalSources,
                "the complete smoke leaves runtime and presenter code unchanged");
            AssertAssetBytesEqual(originalLayouts,
                "the complete smoke leaves authoritative layouts unchanged");
            Debug.Log("RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK");
        }

        private static void ValidateFinitePaintedSlotContract()
        {
            Assert(RuntimeUiArtSlots.RequiredCount == 53
                && RuntimeUiArtSlots.Required.Count == 53,
                "runtime visual contract owns exactly 53 required semantic slots");
            for (var index = 0; index < RuntimeUiArtSlots.Required.Count; index++)
            {
                Assert((int)RuntimeUiArtSlots.Required[index] == index,
                    "required slots remain contiguous and cache-index stable at " + index);
            }

            var added = new[]
            {
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.OrnamentScreenCorner, "ornament.screen-corner"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.SurfaceSectionRibbon, "surface.section-ribbon"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.SurfaceIllustrationFrame, "surface.illustration-frame"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.OrnamentMetricDivider, "ornament.metric-divider"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.OrnamentResultBanner, "ornament.result-banner"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.IllustrationOrchardVista, "illustration.orchard-vista"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.IllustrationLobbyOrchard01,
                    "illustration.lobby-orchard-01"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.IllustrationLobbyOrchard02,
                    "illustration.lobby-orchard-02"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.IllustrationLobbyOrchard03,
                    "illustration.lobby-orchard-03"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.IconResourceSunMicro,
                    "icon.resource-sun-micro"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.IconResourceCoreMicro,
                    "icon.resource-core-micro"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.IconResourceWaveMicro,
                    "icon.resource-wave-micro"),
                new KeyValuePair<RuntimeUiArtSlot, string>(
                    RuntimeUiArtSlot.IllustrationShellOrchardDepth,
                    "illustration.shell-orchard-depth"),
            };
            for (var index = 0; index < added.Length; index++)
            {
                var expectedGeometry = index == 0 || index >= 9 && index <= 11
                    ? RuntimeUiArtGeometry.Icon
                    : index == 1 || index == 2
                        ? RuntimeUiArtGeometry.NineSlice
                        : RuntimeUiArtGeometry.Stretch;
                Assert((int)added[index].Key == 40 + index
                    && RuntimeUiArtSlots.SemanticId(added[index].Key) == added[index].Value
                    && RuntimeUiArtSlots.Geometry(added[index].Key) == expectedGeometry,
                    "painted hierarchy slot contract changed at " + (40 + index));
            }

            var guiPath = Path.Combine(Application.dataPath, "Scripts/UI/RuntimeUiGui.cs");
            var gui = File.ReadAllText(guiPath);
            var requiredApis = new[]
            {
                "public static void DrawScreenCorners(",
                "public static void DrawShellOrchardDepth(",
                "public static void DrawSectionRibbon(",
                "public static void DrawIllustrationFrame(",
                "public static void DrawMetricDivider(",
                "public static void DrawResultBanner(",
                "public static Rect ResolveOpticalEnvelopeDrawRect(",
                "public static void DrawOrchardVista(",
                "public static void DrawLobbyThumbnail(",
                "public static RuntimeUiMetricContentLayout ResolveCompactInlineMetricContentLayout(",
                "private static RuntimeUiArtSlot LobbyThumbnailSlot(",
            };
            for (var index = 0; index < requiredApis.Length; index++)
                Assert(gui.Contains(requiredApis[index]),
                    "shared renderer is missing explicit hierarchy API: " + requiredApis[index]);
            Assert(!gui.Contains("Resources.Load")
                && !gui.Contains("AssetDatabase")
                && !gui.Contains("Texture2D.whiteTexture")
                && !gui.Contains("GUI.skin")
                && !gui.Contains(
                    "DrawSlotArt(context, rect, RuntimeUiArtSlot.SurfaceMetric"),
                "painted hierarchy renderer has no path, resource, default-skin or white fallback");

            var lobby = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/Shell/LobbyPresenter.cs"));
            Assert(lobby.Contains("RuntimeUiGui.DrawScreenCorners")
                && lobby.Contains("RuntimeUiGui.DrawSectionRibbon")
                && lobby.Contains("RuntimeUiGui.DrawLobbyThumbnail")
                && lobby.Contains("RuntimeUiGui.DrawIllustrationFrame")
                && lobby.Contains("RuntimeUiIndicatorKind.Selected")
                && lobby.Contains("_pressTracker.Update(LevelControlId(levelId), rect")
                && lobby.Contains("RuntimeUiGui.DrawActionVisual")
                && !lobby.Contains("GUI.Button(rect, GUIContent.none"),
                "Lobby hierarchy keeps art/copy inside the original finite card and action hits");
            var settlement = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/Shell/SettlementPresenter.cs"));
            Assert(settlement.Contains("RuntimeUiGui.DrawScreenCorners")
                && settlement.Contains("RuntimeUiGui.DrawSectionRibbon")
                && settlement.Contains("RuntimeUiGui.DrawResultBanner")
                && settlement.Contains("RuntimeUiGui.DrawOrchardVista")
                && !settlement.Contains("RuntimeUiGui.DrawMetricDivider")
                && settlement.Contains("compactInline: true"),
                "Settlement hierarchy consumes its explicit result semantics");
            var battle = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/FruitDefenseGame.cs"));
            Assert(battle.Contains("RuntimeUiGui.DrawMetricDivider")
                && battle.Contains("RuntimeUiGui.DrawSectionRibbon")
                && battle.Contains("RuntimeUiGui.DrawResultBanner")
                && battle.Contains("RuntimeUiGui.DrawOrchardVista"),
                "Battle hierarchy consumes its explicit header and terminal semantics");
            var bootstrap = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/App/AppFlowCoordinator.cs"));
            Assert(bootstrap.Contains("RuntimeUiGui.DrawScreenCorners"),
                "Bootstrap consumes the explicit screen-corner semantic");
        }

        private static void ValidateNineSliceDevicePixelCoverage()
        {
            var snap = typeof(RuntimeUiGui).GetMethod("SnapNineSliceBoundaries",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert(snap != null,
                "nine-slice renderer owns one complete device-pixel partition snapper");

            // Exact pre-fix Settlement case: x0=22.517 and x1=36.137 made IMGUI round
            // the first patch's origin and width separately, leaving device column 35 bare.
            ValidateNineSliceDevicePixelCoverage(snap, Matrix4x4.identity,
                new Rect(22.517f, 134.995f, 314.966f, 221.327f),
                36.137f, 323.863f, 148.615f, 342.702f,
                "360x800 inset shell x35 regression", 35);

            ValidateViewportNineSlice(snap, 360, 800, 0, 0, "360x800 full");
            ValidateViewportNineSlice(snap, 360, 800, 32, 24, "360x800 inset");
            ValidateViewportNineSlice(snap, 375, 812, 0, 0, "375x812 full");
            ValidateViewportNineSlice(snap, 375, 812, 40, 21, "375x812 inset");
            ValidateViewportNineSlice(snap, 402, 874, 0, 0, "402x874 full");
            ValidateViewportNineSlice(snap, 402, 874, 44, 34, "402x874 inset");
            ValidateViewportNineSlice(snap, 430, 932, 0, 0, "430x932 full");
            ValidateViewportNineSlice(snap, 430, 932, 50, 36, "430x932 inset");
            Debug.Log("RUNTIME_UI_NINE_SLICE_PARTITION_OK matrices=9 x35=covered");
        }

        private static void ValidateViewportNineSlice(MethodInfo snap,
            float width, float height, float safeTop, float safeBottom, string caseName)
        {
            const float designWidth = 402f;
            const float designHeight = 874f;
            var safeHeight = height - safeTop - safeBottom;
            var scale = Mathf.Min(width / designWidth, safeHeight / designHeight);
            var offsetX = (width - designWidth * scale) * .5f;
            var offsetY = safeTop + (safeHeight - designHeight * scale) * .5f;
            var matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
            var destination = new Rect(18.25f, 121.5f, 365.5f, 260.25f);
            ValidateNineSliceDevicePixelCoverage(snap, matrix, destination,
                destination.xMin + 16f, destination.xMax - 16f,
                destination.yMin + 16f, destination.yMax - 16f, caseName, null);
        }

        private static void ValidateNineSliceDevicePixelCoverage(MethodInfo snap,
            Matrix4x4 matrix, Rect destination, float x1, float x2,
            float y1, float y2, string caseName, int? requiredDeviceColumn)
        {
            var arguments = new object[]
            {
                matrix,
                destination.xMin,
                x1,
                x2,
                destination.xMax,
                destination.yMin,
                y1,
                y2,
                destination.yMax,
            };
            snap.Invoke(null, arguments);

            var x0 = (float)arguments[1];
            var snappedX1 = (float)arguments[2];
            var snappedX2 = (float)arguments[3];
            var x3 = (float)arguments[4];
            var y0 = (float)arguments[5];
            var snappedY1 = (float)arguments[6];
            var snappedY2 = (float)arguments[7];
            var y3 = (float)arguments[8];
            Assert(x0 <= snappedX1 && snappedX1 <= snappedX2 && snappedX2 <= x3
                && y0 <= snappedY1 && snappedY1 <= snappedY2 && snappedY2 <= y3,
                caseName + " keeps all nine destination patches monotonic");

            Assert(IsDevicePixelBoundary(matrix.m00 * x0 + matrix.m03)
                && IsDevicePixelBoundary(matrix.m00 * snappedX1 + matrix.m03)
                && IsDevicePixelBoundary(matrix.m00 * snappedX2 + matrix.m03)
                && IsDevicePixelBoundary(matrix.m00 * x3 + matrix.m03)
                && IsDevicePixelBoundary(matrix.m11 * y0 + matrix.m13)
                && IsDevicePixelBoundary(matrix.m11 * snappedY1 + matrix.m13)
                && IsDevicePixelBoundary(matrix.m11 * snappedY2 + matrix.m13)
                && IsDevicePixelBoundary(matrix.m11 * y3 + matrix.m13),
                caseName + " snaps outer and internal edges into one device-pixel partition");

            var deviceX = new[]
            {
                matrix.m00 * x0 + matrix.m03,
                matrix.m00 * snappedX1 + matrix.m03,
                matrix.m00 * snappedX2 + matrix.m03,
                matrix.m00 * x3 + matrix.m03,
            };
            var deviceY = new[]
            {
                matrix.m11 * y0 + matrix.m13,
                matrix.m11 * snappedY1 + matrix.m13,
                matrix.m11 * snappedY2 + matrix.m13,
                matrix.m11 * y3 + matrix.m13,
            };
            AssertAxisCoveredExactlyOnce(deviceX, caseName + " horizontal");
            AssertAxisCoveredExactlyOnce(deviceY, caseName + " vertical");

            Assert(Mathf.Abs(deviceX[0]
                    - (matrix.m00 * destination.xMin + matrix.m03)) <= .5001f
                && Mathf.Abs(deviceX[3]
                    - (matrix.m00 * destination.xMax + matrix.m03)) <= .5001f
                && Mathf.Abs(deviceY[0]
                    - (matrix.m11 * destination.yMin + matrix.m13)) <= .5001f
                && Mathf.Abs(deviceY[3]
                    - (matrix.m11 * destination.yMax + matrix.m13)) <= .5001f,
                caseName + " preserves the requested outer draw rect to device-pixel precision");

            if (requiredDeviceColumn.HasValue)
            {
                Assert(Mathf.Abs(PatchCoverageCount(deviceX, requiredDeviceColumn.Value)
                        - RuntimeUiQualityProfile.NineSlicePartitionCoverageCount)
                        <= RuntimeUiQualityProfile.NineSliceSeamToleranceDevicePixels,
                    caseName + " covers device column " + requiredDeviceColumn.Value
                    + " exactly once");
            }
        }

        private static bool IsDevicePixelBoundary(float value)
        {
            return Mathf.Abs(value - Mathf.Round(value)) <= .0001f;
        }

        private static void AssertAxisCoveredExactlyOnce(float[] boundaries, string caseName)
        {
            var first = Mathf.RoundToInt(boundaries[0]);
            var last = Mathf.RoundToInt(boundaries[boundaries.Length - 1]);
            for (var devicePixel = first; devicePixel < last; devicePixel++)
            {
                Assert(Mathf.Abs(PatchCoverageCount(boundaries, devicePixel)
                        - RuntimeUiQualityProfile.NineSlicePartitionCoverageCount)
                        <= RuntimeUiQualityProfile.NineSliceSeamToleranceDevicePixels,
                    caseName + " covers device pixel " + devicePixel + " exactly once");
            }
        }

        private static int PatchCoverageCount(float[] boundaries, int devicePixel)
        {
            var count = 0;
            for (var index = 0; index < boundaries.Length - 1; index++)
            {
                if (devicePixel >= Mathf.RoundToInt(boundaries[index])
                    && devicePixel < Mathf.RoundToInt(boundaries[index + 1]))
                {
                    count++;
                }
            }

            return count;
        }

        private static void ValidateNineSliceSourceUvContract()
        {
            var checkedBindings = 0;
            foreach (var artSet in RuntimeUiArtSetRegistry.DiscoverProductionSets())
            {
                foreach (var binding in artSet.Bindings.Where(value =>
                             value != null
                             && value.Geometry == RuntimeUiArtGeometry.NineSlice))
                {
                    var texture = LoadBindingTexture(binding,
                        artSet.SetId + "/" + RuntimeUiArtSlots.SemanticId(binding.Slot));
                    try
                    {
                        AssertNineSliceSourceEdgesSeamSafe(texture, binding,
                            artSet.SetId + "/" + RuntimeUiArtSlots.SemanticId(binding.Slot));
                        checkedBindings++;
                    }
                    finally
                    {
                        Object.DestroyImmediate(texture);
                    }
                }
            }

            Assert(checkedBindings > 0,
                "production art sets expose nine-slice source partitions for UV validation");
            Debug.Log("RUNTIME_UI_NINE_SLICE_SOURCE_UV_OK bindings=" + checkedBindings);
        }

        private static void ValidateScreenBackgroundOpacityContract()
        {
            var preFixOverwrite = new[]
            {
                new Color32(245, 221, 174, 255),
                new Color32(255, 231, 159, 32),
                new Color32(113, 184, 70, 18),
            };
            Assert(!RuntimeUiVisualSystemValidator.IsFullyOpaque(preFixOverwrite,
                    out var preFixNonOpaque, out var preFixMinimumAlpha)
                && preFixNonOpaque == 2 && preFixMinimumAlpha == 18,
                "screen-background contract rejects pre-fix alpha-replacing overlays");

            var checkedSets = 0;
            foreach (var artSet in RuntimeUiArtSetRegistry.DiscoverProductionSets())
            {
                var binding = artSet.GetRequiredBinding(
                    RuntimeUiArtSlot.SurfaceScreenBackground);
                var texture = LoadBindingTexture(binding,
                    artSet.SetId + "/surface.screen-background");
                try
                {
                    Assert(RuntimeUiVisualSystemValidator.IsFullyOpaque(texture.GetPixels32(),
                            out var nonOpaque, out var minimumAlpha)
                        && nonOpaque == 0 && minimumAlpha == 255,
                        artSet.SetId
                        + " screen background is fully opaque in the runtime PNG");
                    checkedSets++;
                }
                finally
                {
                    Object.DestroyImmediate(texture);
                }
            }

            Assert(checkedSets > 0,
                "production registry exposes screen backgrounds for opacity validation");
            Debug.Log("RUNTIME_UI_SCREEN_BACKGROUND_OPAQUE_OK sets=" + checkedSets
                + " pre-fix-min-alpha=" + preFixMinimumAlpha);
        }

        private static void AssertNineSliceSourceEdgesSeamSafe(Texture2D texture,
            RuntimeUiArtBinding binding, string caseName)
        {
            var rect = binding.Sprite.rect;
            var x0 = Mathf.RoundToInt(rect.xMin);
            var x1 = x0 + binding.SliceBorder.Left;
            var x2 = Mathf.RoundToInt(rect.xMax) - binding.SliceBorder.Right;
            var x3 = Mathf.RoundToInt(rect.xMax);
            var y0 = Mathf.RoundToInt(rect.yMin);
            var y1 = y0 + binding.SliceBorder.Bottom;
            var y2 = Mathf.RoundToInt(rect.yMax) - binding.SliceBorder.Top;
            var y3 = Mathf.RoundToInt(rect.yMax);
            Assert(x0 < x1 && x1 < x2 && x2 < x3
                && y0 < y1 && y1 < y2 && y2 < y3,
                caseName + " owns a non-empty nine-slice source partition");
            var transparentCenterFrame =
                binding.Slot == RuntimeUiArtSlot.SurfaceIllustrationFrame;

            for (var y = y1; y < y2; y++)
            {
                Assert(IsNineSliceBoundaryPairSafe(
                           texture.GetPixel(x1 - 1, y).a,
                           texture.GetPixel(x1, y).a,
                           transparentCenterFrame)
                    && IsNineSliceBoundaryPairSafe(
                           texture.GetPixel(x2 - 1, y).a,
                           texture.GetPixel(x2, y).a,
                           transparentCenterFrame),
                    caseName
                    + " keeps both texels around each vertical UV boundary seam-safe");
            }

            for (var x = x1; x < x2; x++)
            {
                Assert(IsNineSliceBoundaryPairSafe(
                           texture.GetPixel(x, y1 - 1).a,
                           texture.GetPixel(x, y1).a,
                           transparentCenterFrame)
                    && IsNineSliceBoundaryPairSafe(
                           texture.GetPixel(x, y2 - 1).a,
                           texture.GetPixel(x, y2).a,
                           transparentCenterFrame),
                    caseName
                    + " keeps both texels around each horizontal UV boundary seam-safe");
            }
        }

        private static bool IsNineSliceBoundaryPairSafe(float left, float right,
            bool allowMatchedTransparency)
        {
            var first = (byte)Mathf.Clamp(Mathf.RoundToInt(left * 255f), 0, 255);
            var second = (byte)Mathf.Clamp(Mathf.RoundToInt(right * 255f), 0, 255);
            return RuntimeUiVisualSystemValidator.IsNineSliceBoundaryPairSafe(
                first, second, allowMatchedTransparency);
        }

        private static void ValidateLoadingPrimaryContrast(RuntimeUiTheme theme)
        {
            var resolve = typeof(RuntimeUiGui).GetMethod("ResolveActionVisualState",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert(resolve != null,
                "action renderer owns one scoped contrast-critical Loading resolver");

            var primary = ResolveActionVisualState(resolve, RuntimeUiActionKind.Primary,
                RuntimeUiInteractionState.Loading);
            var danger = ResolveActionVisualState(resolve, RuntimeUiActionKind.Danger,
                RuntimeUiInteractionState.Loading);
            var secondary = ResolveActionVisualState(resolve, RuntimeUiActionKind.Secondary,
                RuntimeUiInteractionState.Loading);
            var quiet = ResolveActionVisualState(resolve, RuntimeUiActionKind.Quiet,
                RuntimeUiInteractionState.Loading);
            Assert(primary == RuntimeUiInteractionState.Normal
                && danger == RuntimeUiInteractionState.Normal
                && secondary == RuntimeUiInteractionState.Loading
                && quiet == RuntimeUiInteractionState.Loading,
                "only inverse-text Primary/Danger actions keep their semantic surface and content opaque while Loading");

            var resolveDraw = typeof(RuntimeUiGui).GetMethod("ResolveActionDrawState",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert(resolveDraw != null,
                "action renderer owns one visual-state priority resolver");
            Assert(ResolveActionDrawState(resolveDraw, RuntimeUiActionKind.Primary,
                    RuntimeUiInteractionState.Loading, true)
                    == RuntimeUiInteractionState.Normal
                && ResolveActionDrawState(resolveDraw, RuntimeUiActionKind.Danger,
                    RuntimeUiInteractionState.Loading, true)
                    == RuntimeUiInteractionState.Normal,
                "an emphasized Loading pulse cannot fade contrast-critical action content");
            Assert(ResolveActionDrawState(resolveDraw, RuntimeUiActionKind.Secondary,
                    RuntimeUiInteractionState.Loading, true)
                    == RuntimeUiInteractionState.Loading,
                "an emphasized Loading pulse preserves non-critical action Loading semantics");
            Assert(ResolveActionDrawState(resolveDraw, RuntimeUiActionKind.Primary,
                    RuntimeUiInteractionState.Normal, true)
                    == RuntimeUiInteractionState.Pressed,
                "an emphasized normal action still renders its approved Pressed feedback");
            Assert(ResolveActionDrawState(resolveDraw, RuntimeUiActionKind.Primary,
                    RuntimeUiInteractionState.Disabled, true)
                    == RuntimeUiInteractionState.Disabled,
                "Disabled remains higher priority than emphasized Pressed feedback");

            var fadedSurface = Composite(theme.Colors.PrimaryAction,
                theme.Colors.BaseSurface, theme.Feedback.LoadingOpacity);
            var fadedText = Composite(theme.Colors.InverseText,
                fadedSurface, theme.Feedback.LoadingOpacity);
            var fadedContrast = Contrast(fadedText, fadedSurface);
            Assert(fadedContrast < RuntimeUiQualityProfile.LargeOrBoldTextContrast,
                "regression fixture reproduces the double-attenuated Loading contrast failure");

            var resolvedOpacity = primary == RuntimeUiInteractionState.Normal
                ? theme.Feedback.NormalOpacity
                : theme.Feedback.LoadingOpacity;
            var resolvedSurface = Composite(theme.Colors.PrimaryAction,
                theme.Colors.BaseSurface, resolvedOpacity);
            var resolvedText = Composite(theme.Colors.InverseText,
                resolvedSurface, resolvedOpacity);
            var resolvedContrast = Contrast(resolvedText, resolvedSurface);
            Assert(resolvedContrast + .001f
                    >= RuntimeUiQualityProfile.LargeOrBoldTextContrast,
                "composited Loading Primary inverse-text contrast meets the 3.0:1 gate");
            Debug.Log("RUNTIME_UI_LOADING_PRIMARY_CONTRAST_OK ratio="
                + resolvedContrast.ToString("0.0000",
                    System.Globalization.CultureInfo.InvariantCulture));
        }

        private static RuntimeUiInteractionState ResolveActionVisualState(MethodInfo method,
            RuntimeUiActionKind kind, RuntimeUiInteractionState state)
        {
            return (RuntimeUiInteractionState)method.Invoke(null, new object[] { kind, state });
        }

        private static RuntimeUiInteractionState ResolveActionDrawState(MethodInfo method,
            RuntimeUiActionKind kind, RuntimeUiInteractionState state, bool emphasized)
        {
            return (RuntimeUiInteractionState)method.Invoke(null,
                new object[] { kind, state, emphasized });
        }

        private static void ValidateSemanticStateContrast(RuntimeUiTheme theme)
        {
            var resolveSurface = typeof(RuntimeUiGui).GetMethod("ResolveSurfaceVisualState",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert(resolveSurface != null,
                "stateful surface renderer owns one scoped semantic-state resolver");

            foreach (var state in new[]
                     {
                         RuntimeUiInteractionState.Success,
                         RuntimeUiInteractionState.Warning,
                         RuntimeUiInteractionState.Error,
                     })
            {
                Assert(ResolveSurfaceVisualState(resolveSurface, state)
                    == RuntimeUiInteractionState.Normal,
                    state + " keeps the component surface neutral");
            }

            foreach (var state in new[]
                     {
                         RuntimeUiInteractionState.Normal,
                         RuntimeUiInteractionState.HoveredOrFocused,
                         RuntimeUiInteractionState.Pressed,
                         RuntimeUiInteractionState.Disabled,
                         RuntimeUiInteractionState.Selected,
                         RuntimeUiInteractionState.Loading,
                     })
            {
                Assert(ResolveSurfaceVisualState(resolveSurface, state) == state,
                    state + " preserves its existing component-surface treatment");
            }

            var context = RuntimeUiDrawContext.Create(theme, 1f);
            var textColor = typeof(RuntimeUiDrawContext).GetMethod("TextColor",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var stateIndicator = typeof(RuntimeUiGui).GetMethod("StateIndicatorSlot",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert(textColor != null && stateIndicator != null,
                "semantic state text and non-color indicator resolvers exist");

            foreach (RuntimeUiInteractionState state in Enum.GetValues(
                         typeof(RuntimeUiInteractionState)))
            {
                var resolved = (Color)textColor.Invoke(context,
                    new object[] { RuntimeUiTextTone.State, state });
                Assert(ColorApproximately(resolved, theme.Colors.PrimaryText),
                    state + " state text uses the existing contrast-safe primary text token");
            }

            var statusSurface = LoadBindingCenter(theme.ActiveArtSet,
                RuntimeUiArtSlot.SurfaceStatus);
            var semanticStates = new[]
            {
                RuntimeUiInteractionState.Success,
                RuntimeUiInteractionState.Warning,
                RuntimeUiInteractionState.Error,
            };
            var semanticColors = new[]
            {
                theme.Colors.Success,
                theme.Colors.Warning,
                theme.Colors.Danger,
            };
            var resolvedRatios = new float[semanticStates.Length];
            for (var index = 0; index < semanticStates.Length; index++)
            {
                var oldTintedSurface = Multiply(statusSurface, semanticColors[index]);
                var oldContrast = Contrast(semanticColors[index], oldTintedSurface);
                Assert(oldContrast < RuntimeUiQualityProfile.NonTextContrast,
                    semanticStates[index]
                    + " regression fixture reproduces semantic-on-semantic surface failure");

                var resolvedText = (Color)textColor.Invoke(context,
                    new object[] { RuntimeUiTextTone.State, semanticStates[index] });
                Assert(ColorApproximately(resolvedText, theme.Colors.PrimaryText),
                    semanticStates[index]
                    + " essential state text uses the existing contrast-safe primary text token");
                resolvedRatios[index] = Contrast(resolvedText, statusSurface);
                Assert(resolvedRatios[index] + .001f
                        >= RuntimeUiQualityProfile.NonTextContrast,
                    semanticStates[index]
                    + " state text meets the 3.0:1 gate on the actual neutral status surface");
                Assert(stateIndicator.Invoke(null, new object[] { semanticStates[index] }) != null,
                    semanticStates[index] + " retains a distinct non-color indicator sprite");
            }

            var selectedSurface = Multiply(statusSurface, theme.Colors.SelectionAccent);
            var oldSelectedContrast = Contrast(theme.Colors.SelectionAccent, selectedSurface);
            Assert(oldSelectedContrast < RuntimeUiQualityProfile.NonTextContrast,
                "selected regression fixture reproduces same-color status text failure");
            var selectedText = (Color)textColor.Invoke(context,
                new object[]
                {
                    RuntimeUiTextTone.State,
                    RuntimeUiInteractionState.Selected,
                });
            var selectedContrast = Contrast(selectedText, selectedSurface);
            Assert(selectedContrast + .001f >= RuntimeUiQualityProfile.NonTextContrast,
                "selected status text meets the 3.0:1 gate on the actual selected surface");
            Assert(stateIndicator.Invoke(null,
                    new object[] { RuntimeUiInteractionState.Selected }) != null,
                "selected status retains its non-color marker sprite");

            var modalSurface = LoadBindingCenter(theme.ActiveArtSet,
                RuntimeUiArtSlot.SurfaceModal);
            var modalTitleContrast = Contrast(theme.Colors.PrimaryText, modalSurface);
            Assert(modalTitleContrast + .001f
                    >= RuntimeUiQualityProfile.LargeOrBoldTextContrast,
                "primary modal title meets the 3.0:1 gate on the actual neutral modal surface");

            foreach (var slot in new[]
                     {
                         RuntimeUiArtSlot.SurfacePanelStandard,
                         RuntimeUiArtSlot.SurfacePanelRaised,
                         RuntimeUiArtSlot.SurfaceDetail,
                         RuntimeUiArtSlot.SurfaceResult,
                     })
            {
                var neutralSurface = LoadBindingCenter(theme.ActiveArtSet, slot);
                Assert(Contrast(theme.Colors.PrimaryText, neutralSurface) + .001f
                        >= RuntimeUiQualityProfile.LargeOrBoldTextContrast,
                    RuntimeUiArtSlots.SemanticId(slot)
                    + " remains readable with primary text when a semantic outcome is shown");
            }

            Debug.Log("RUNTIME_UI_SEMANTIC_STATE_CONTRAST_OK status-success="
                + resolvedRatios[0].ToString("0.0000",
                    System.Globalization.CultureInfo.InvariantCulture)
                + " status-warning=" + resolvedRatios[1].ToString("0.0000",
                    System.Globalization.CultureInfo.InvariantCulture)
                + " status-error=" + resolvedRatios[2].ToString("0.0000",
                    System.Globalization.CultureInfo.InvariantCulture)
                + " status-selected=" + selectedContrast.ToString("0.0000",
                    System.Globalization.CultureInfo.InvariantCulture)
                + " modal-title=" + modalTitleContrast.ToString("0.0000",
                    System.Globalization.CultureInfo.InvariantCulture));
        }

        private static RuntimeUiInteractionState ResolveSurfaceVisualState(MethodInfo method,
            RuntimeUiInteractionState state)
        {
            return (RuntimeUiInteractionState)method.Invoke(null, new object[] { state });
        }

        private static Color LoadBindingCenter(RuntimeUiArtSet artSet, RuntimeUiArtSlot slot)
        {
            var binding = artSet.GetRequiredBinding(slot);
            var caseName = artSet.SetId + "/" + RuntimeUiArtSlots.SemanticId(slot);
            var texture = LoadBindingTexture(binding, caseName);
            try
            {
                var rect = binding.Sprite.rect;
                var x = Mathf.Clamp(Mathf.FloorToInt(rect.center.x), 0, texture.width - 1);
                var y = Mathf.Clamp(Mathf.FloorToInt(rect.center.y), 0, texture.height - 1);
                return texture.GetPixel(x, y);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static Texture2D LoadBindingTexture(RuntimeUiArtBinding binding,
            string caseName)
        {
            var assetPath = AssetDatabase.GetAssetPath(binding.Texture);
            var fullPath = string.IsNullOrEmpty(assetPath)
                ? string.Empty
                : Path.GetFullPath(assetPath);
            Assert(!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath),
                caseName + " runtime PNG exists");

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert(texture.LoadImage(File.ReadAllBytes(fullPath), false),
                caseName + " runtime PNG decodes");
            return texture;
        }

        private static Color Multiply(Color first, Color second)
        {
            return new Color(first.r * second.r, first.g * second.g,
                first.b * second.b, first.a * second.a);
        }

        private static bool ColorApproximately(Color first, Color second)
        {
            return Mathf.Abs(first.r - second.r) <= .001f
                && Mathf.Abs(first.g - second.g) <= .001f
                && Mathf.Abs(first.b - second.b) <= .001f
                && Mathf.Abs(first.a - second.a) <= .001f;
        }

        private static Color Composite(Color foreground, Color background, float opacity)
        {
            opacity = Mathf.Clamp01(opacity * foreground.a);
            return new Color(
                foreground.r * opacity + background.r * (1f - opacity),
                foreground.g * opacity + background.g * (1f - opacity),
                foreground.b * opacity + background.b * (1f - opacity),
                1f);
        }

        private static float Contrast(Color first, Color second)
        {
            var bright = Mathf.Max(Luminance(first), Luminance(second));
            var dark = Mathf.Min(Luminance(first), Luminance(second));
            return (bright + .05f) / (dark + .05f);
        }

        private static float Luminance(Color color)
        {
            return .2126f * LinearChannel(color.r)
                + .7152f * LinearChannel(color.g)
                + .0722f * LinearChannel(color.b);
        }

        private static float LinearChannel(float value)
        {
            value = Mathf.Clamp01(value);
            return value <= .04045f
                ? value / 12.92f
                : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
        }

        private static void ValidateFixtureMatrix(RuntimeUiArtSet production,
            ICollection<RuntimeUiArtSet> transients,
            ICollection<RuntimeUiArtSet> invalidContractFixtures)
        {
            var complete = Clone(production, "complete", transients);
            Assert(complete.Validate().IsValid
                && complete.Bindings.Count == RuntimeUiArtSlots.RequiredCount,
                "complete injected fixture owns exactly all required slots");

            var incomplete = Clone(production, "incomplete", transients);
            SetBindingArraySize(incomplete, 0);
            var incompleteReport = incomplete.Validate();
            Assert(!incompleteReport.IsValid
                && incompleteReport.Issues.Count(issue =>
                    issue.Code == "art-set.slot.missing")
                == RuntimeUiArtSlots.RequiredCount,
                "incomplete fixture reports every omitted required slot");
            invalidContractFixtures.Add(incomplete);

            var missing = Clone(production, "missing", transients);
            SetBindingArraySize(missing, RuntimeUiArtSlots.RequiredCount - 1);
            var missingReport = missing.Validate();
            Assert(!missingReport.IsValid
                && missingReport.Issues.Count(issue =>
                    issue.Code == "art-set.slot.missing") == 1
                && missingReport.Issues.All(issue =>
                    issue.Code != "art-set.slot.duplicate"),
                "missing fixture reports one missing slot without fallback");
            invalidContractFixtures.Add(missing);

            var duplicate = Clone(production, "duplicate", transients);
            var serialized = new SerializedObject(duplicate);
            var bindings = Require(serialized, "bindings");
            var firstSlot = bindings.GetArrayElementAtIndex(0)
                .FindPropertyRelative("slot").intValue;
            bindings.GetArrayElementAtIndex(bindings.arraySize - 1)
                .FindPropertyRelative("slot").intValue = firstSlot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var duplicateReport = duplicate.Validate();
            Assert(!duplicateReport.IsValid
                && duplicateReport.Issues.Any(issue =>
                    issue.Code == "art-set.slot.duplicate")
                && duplicateReport.Issues.Any(issue =>
                    issue.Code == "art-set.slot.missing"),
                "duplicate fixture reports both duplicate ownership and displaced slot");
            invalidContractFixtures.Add(duplicate);
        }

        private static void ValidatePreviewIsolation(RuntimeUiTheme releaseTheme,
            RuntimeUiArtSet candidate, byte[] themeBytes,
            IReadOnlyDictionary<string, byte[]> sceneBytes,
            out RuntimeUiTheme previewTheme)
        {
            var activeBefore = releaseTheme.ActiveArtSet;
            Assert(RuntimeUiVisualSystemPreview.TryCreate(releaseTheme, candidate,
                    out previewTheme, out var report),
                "complete production candidate creates isolated preview: "
                + RuntimeUiVisualSystemValidator.FormatReport(report));
            Assert(previewTheme != null && previewTheme != releaseTheme
                && previewTheme.ActiveArtSet == candidate
                && !EditorUtility.IsPersistent(previewTheme)
                && (previewTheme.hideFlags & HideFlags.DontSave) != 0,
                "preview uses a non-persistent cloned theme");
            ValidateAllRequiredSlots(previewTheme, candidate,
                "isolated preview resolves every semantic slot");
            ValidateWindowPreviewReadiness(candidate);
            Assert(releaseTheme.ActiveArtSet == activeBefore,
                "preview does not mutate active release binding");
            Assert(BytesEqual(themeBytes,
                    ReadAssetBytes(RuntimeUiArtSetRegistry.ReleaseThemePath)),
                "preview leaves release-theme serialization unchanged");
            AssertSceneBytesEqual(sceneBytes,
                "preview leaves every release scene byte unchanged");
            Debug.Log("RUNTIME_UI_PRODUCTION_CANDIDATE_PREVIEW_OK candidate="
                + candidate.SetId + "@" + candidate.Revision
                + " candidateGuid=" + AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(candidate))
                + " slots=" + RuntimeUiArtSlots.RequiredCount
                + " components=" + Enum.GetValues(typeof(RuntimeUiComponentKind)).Length
                + " states=" + Enum.GetValues(typeof(RuntimeUiInteractionState)).Length
                + " routes=3 themeBytes=unchanged scenes=" + sceneBytes.Count);
        }

        private static void ValidateWindowPreviewReadiness(RuntimeUiArtSet candidate)
        {
            var windowType = typeof(RuntimeUiVisualSystemWindow);
            var refresh = windowType.GetMethod("RefreshAll",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var drawContextField = windowType.GetField("drawContext",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var previewThemeField = windowType.GetField("previewTheme",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var candidateField = windowType.GetField("candidate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var galleryStatesField = windowType.GetField("GalleryStates",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert(refresh != null && drawContextField != null && previewThemeField != null
                && candidateField != null && galleryStatesField != null,
                "visual-system window exposes its one candidate preview path");
            Assert(windowType.GetMethod("DrawStateGallery",
                       BindingFlags.NonPublic | BindingFlags.Instance) != null
                && windowType.GetMethod("DrawRouteChrome",
                       BindingFlags.NonPublic | BindingFlags.Instance) != null
                && windowType.GetMethod("DrawLobbyChrome",
                       BindingFlags.NonPublic | BindingFlags.Instance) != null
                && windowType.GetMethod("DrawBattleChrome",
                       BindingFlags.NonPublic | BindingFlags.Instance) != null
                && windowType.GetMethod("DrawSettlementChrome",
                       BindingFlags.NonPublic | BindingFlags.Instance) != null,
                "visual-system window owns component gallery and all representative route chrome");

            var states = (RuntimeUiInteractionState[])galleryStatesField.GetValue(null);
            Assert(states != null
                && states.SequenceEqual((RuntimeUiInteractionState[])Enum.GetValues(
                    typeof(RuntimeUiInteractionState)))
                && Enum.GetValues(typeof(RuntimeUiComponentKind)).Length > 0,
                "component gallery covers every finite component and interaction state");

            var window = ScriptableObject.CreateInstance<RuntimeUiVisualSystemWindow>();
            window.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                refresh.Invoke(window, new object[] { candidate });
                var preview = previewThemeField.GetValue(window) as RuntimeUiTheme;
                var context = drawContextField.GetValue(window) as RuntimeUiDrawContext;
                Assert(candidateField.GetValue(window) == candidate
                    && preview != null && preview.ActiveArtSet == candidate
                    && context != null && context.Theme == preview
                    && context.ArtSet == candidate,
                    "actual editor window is render-ready from the isolated candidate context");
                ValidateAllRequiredSlots(preview, candidate,
                    "editor window gallery and route chrome resolve all semantic slots");
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        private static void ValidateInvalidActivation(RuntimeUiTheme releaseTheme,
            RuntimeUiArtSet production, byte[] themeBytes,
            IReadOnlyDictionary<string, byte[]> sceneBytes,
            ICollection<RuntimeUiArtSet> transients,
            IEnumerable<RuntimeUiArtSet> invalidContractFixtures,
            out RuntimeUiArtSet invalidPath)
        {
            Undo.ClearAll();
            foreach (var fixture in invalidContractFixtures)
            {
                AssertRejectedWithoutMutation(releaseTheme, fixture,
                    "runtime.art-set.slot.", themeBytes, sceneBytes,
                    fixture.name + " activation");
            }

            invalidPath = Clone(production, "invalid-path", transients);
            invalidPath.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(invalidPath, InvalidPathFixture);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(InvalidPathFixture,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            transients.Remove(invalidPath);
            invalidPath = AssetDatabase.LoadAssetAtPath<RuntimeUiArtSet>(InvalidPathFixture);
            Assert(invalidPath != null && invalidPath.Validate().IsValid,
                "invalid-path fixture is otherwise a complete art set");

            AssertRejectedWithoutMutation(releaseTheme, invalidPath,
                "candidate.not-production", themeBytes, sceneBytes,
                "invalid-path activation");

            AssetDatabase.DeleteAsset(InvalidPathFixture);
            invalidPath = null;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void AssertRejectedWithoutMutation(RuntimeUiTheme releaseTheme,
            RuntimeUiArtSet candidate, string expectedCodePrefix, byte[] themeBytes,
            IReadOnlyDictionary<string, byte[]> sceneBytes, string caseName)
        {
            var undoGroupBefore = Undo.GetCurrentGroup();
            var undoNameBefore = Undo.GetCurrentGroupName();
            var activeBefore = releaseTheme.ActiveArtSet;
            Assert(!RuntimeUiVisualSystemActivation.TryActivate(releaseTheme, candidate,
                    out var report, out var rejectedGroup)
                && rejectedGroup == -1
                && report.Issues.Any(issue =>
                    issue.Code.StartsWith(expectedCodePrefix, StringComparison.Ordinal)),
                caseName + " is rejected with its actionable validation error");
            Assert(Undo.GetCurrentGroup() == undoGroupBefore
                && Undo.GetCurrentGroupName() == undoNameBefore,
                caseName + " creates no Undo group");
            Assert(releaseTheme.ActiveArtSet == activeBefore,
                caseName + " is zero-mutation");
            Assert(BytesEqual(themeBytes,
                    ReadAssetBytes(RuntimeUiArtSetRegistry.ReleaseThemePath)),
                caseName + " leaves release-theme serialization unchanged");
            AssertSceneBytesEqual(sceneBytes,
                caseName + " leaves every release scene byte unchanged");
        }

        private static IReadOnlyList<RuntimeUiArtSet> ValidateProductionRegistry()
        {
            var discovered = RuntimeUiArtSetRegistry.DiscoverProductionSets().ToArray();
            var expected = discovered
                .OrderBy(set => set.SetId, StringComparer.Ordinal)
                .ThenBy(set => set.Revision, StringComparer.Ordinal)
                .ThenBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal)
                .ToArray();
            Assert(discovered.Length > 0 && discovered.SequenceEqual(expected),
                "production registry discovery is stable by set identity and revision");
            Assert(discovered.Select(set => set.SetId + "\n" + set.Revision)
                    .Distinct(StringComparer.Ordinal).Count() == discovered.Length,
                "production registry identities are unique");
            Debug.Log("RUNTIME_UI_PRODUCTION_REGISTRY_OK order="
                + string.Join(",", discovered.Select(set => set.SetId + "@" + set.Revision)));
            return discovered;
        }

        private static void ValidateProductionCandidateWorkflows(RuntimeUiTheme releaseTheme,
            IReadOnlyList<RuntimeUiArtSet> productionSets, RuntimeUiArtSet originalActive,
            byte[] originalThemeBytes, string originalThemeContract,
            string originalThemeGuid, string originalThemeId, string originalThemeRevision,
            IReadOnlyDictionary<string, byte[]> sceneBytes,
            IReadOnlyDictionary<string, byte[]> sourceBytes,
            IReadOnlyDictionary<string, byte[]> layoutBytes)
        {
            Assert(releaseTheme.ActiveArtSet == originalActive,
                "production replacement workflow starts from the release art set");
            var candidates = productionSets.Where(set => set != originalActive).ToArray();
            var validatedCount = 0;
            foreach (var candidate in candidates)
            {
                var validation = RuntimeUiVisualSystemValidator.ValidateCandidate(
                    releaseTheme, candidate);
                Assert(validation.IsValid && validation.ErrorCount == 0
                    && validation.WarningCount == 0,
                    "production candidate validates without errors or warnings: "
                    + candidate.SetId + "@" + candidate.Revision + "\n"
                    + RuntimeUiVisualSystemValidator.FormatReport(validation));

                RuntimeUiTheme preview = null;
                try
                {
                    ValidatePreviewIsolation(releaseTheme, candidate, originalThemeBytes,
                        sceneBytes, out preview);
                    AssertAssetBytesEqual(sourceBytes,
                        "candidate preview changes no runtime or presenter code");
                    AssertAssetBytesEqual(layoutBytes,
                        "candidate preview changes no authoritative layout");
                }
                finally
                {
                    if (preview != null) Object.DestroyImmediate(preview);
                }

                ValidateAtomicReplacementAndUndo(releaseTheme, originalActive, candidate,
                    originalThemeBytes, originalThemeContract, originalThemeGuid,
                    originalThemeId, originalThemeRevision, sceneBytes, sourceBytes,
                    layoutBytes);
                validatedCount++;
            }

            Debug.Log("RUNTIME_UI_PRODUCTION_CANDIDATE_WORKFLOWS_OK candidates="
                + validatedCount);
        }

        private static void ValidateAtomicReplacementAndUndo(RuntimeUiTheme releaseTheme,
            RuntimeUiArtSet originalActive, RuntimeUiArtSet candidate,
            byte[] originalThemeBytes, string originalThemeContract,
            string originalThemeGuid, string originalThemeId, string originalThemeRevision,
            IReadOnlyDictionary<string, byte[]> sceneBytes,
            IReadOnlyDictionary<string, byte[]> sourceBytes,
            IReadOnlyDictionary<string, byte[]> layoutBytes)
        {
            Undo.ClearAll();
            Assert(RuntimeUiVisualSystemActivation.TryActivate(releaseTheme, candidate,
                    out var report, out var undoGroup)
                && report.IsValid && report.WarningCount == 0 && undoGroup >= 0
                && releaseTheme.ActiveArtSet == candidate,
                "valid production replacement activates through one named Undo group: "
                + RuntimeUiVisualSystemValidator.FormatReport(report));
            var undoName = Undo.GetCurrentGroupName();
            Assert(undoName == RuntimeUiVisualSystemActivation.UndoLabel,
                "activation exposes the stable user-facing Undo label");
            Assert(CaptureThemeContractWithoutActive(releaseTheme) == originalThemeContract,
                "activation changes only the theme active-art-set reference");
            ValidateAllRequiredSlots(releaseTheme, candidate,
                "active replacement resolves every semantic slot");
            AssertSceneBytesEqual(sceneBytes,
                "replacement activation changes no release scene bytes");
            AssertAssetBytesEqual(sourceBytes,
                "replacement activation changes no runtime or presenter code");
            AssertAssetBytesEqual(layoutBytes,
                "replacement activation changes no authoritative layout");

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert(releaseTheme.ActiveArtSet == originalActive,
                "one Undo restores the prior production art set");
            Undo.PerformUndo();
            Assert(releaseTheme.ActiveArtSet == originalActive,
                "activation contributes no second Undo step");
            Undo.PerformRedo();
            Assert(releaseTheme.ActiveArtSet == candidate,
                "one Redo reapplies the complete production replacement");
            Undo.PerformRedo();
            Assert(releaseTheme.ActiveArtSet == candidate,
                "activation contributes no second Redo step");
            Undo.PerformUndo();
            Assert(releaseTheme.ActiveArtSet == originalActive,
                "final Undo restores the approved production set");

            EditorUtility.SetDirty(releaseTheme);
            AssetDatabase.SaveAssetIfDirty(releaseTheme);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Assert(BytesEqual(originalThemeBytes,
                    ReadAssetBytes(RuntimeUiArtSetRegistry.ReleaseThemePath)),
                "final Undo restores release-theme bytes exactly");
            Assert(AssetDatabase.AssetPathToGUID(RuntimeUiArtSetRegistry.ReleaseThemePath)
                    == originalThemeGuid
                && releaseTheme.ThemeId == originalThemeId
                && releaseTheme.Revision == originalThemeRevision,
                "final Undo preserves release-theme GUID, identity and revision");
            Assert(CaptureThemeContractWithoutActive(releaseTheme) == originalThemeContract,
                "Undo/Redo changes no theme contract outside the active set");
            AssertSceneBytesEqual(sceneBytes,
                "replacement Undo/Redo changes no release scene bytes");
            AssertAssetBytesEqual(sourceBytes,
                "replacement Undo/Redo changes no runtime or presenter code");
            AssertAssetBytesEqual(layoutBytes,
                "replacement Undo/Redo changes no authoritative layout");
            AssertNoReleaseDependency(candidate);

            Debug.Log("RUNTIME_UI_PRODUCTION_CANDIDATE_WORKFLOW_OK active="
                + originalActive.SetId + "@" + originalActive.Revision
                + " activeGuid=" + AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(originalActive))
                + " candidate=" + candidate.SetId + "@" + candidate.Revision
                + " candidateGuid=" + AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(candidate))
                + " undoGroup=" + undoGroup + " undoName=\"" + undoName + "\""
                + " theme=" + originalThemeId + "@" + originalThemeRevision
                + " themeGuid=" + originalThemeGuid
                + " validationErrors=0 validationWarnings=0"
                + " slots=" + RuntimeUiArtSlots.RequiredCount
                + " scenes=" + sceneBytes.Count
                + " codeHash=" + ComputeSnapshotHash(sourceBytes)
                + " layoutHash=" + ComputeSnapshotHash(layoutBytes)
                + " mutation=theme.activeArtSet-only restoredThemeBytes=true");
            Undo.ClearAll();
        }

        private static void ValidateInPlaceReimport(RuntimeUiTheme releaseTheme,
            RuntimeUiArtSet production, IReadOnlyDictionary<string, byte[]> sceneBytes)
        {
            var setPath = AssetDatabase.GetAssetPath(production);
            var slot = RuntimeUiArtSlot.SurfaceScreenBackground;
            var binding = production.GetRequiredBinding(slot);
            var texturePath = AssetDatabase.GetAssetPath(binding.Texture);
            var spritePath = AssetDatabase.GetAssetPath(binding.Sprite);
            var guid = AssetDatabase.AssetPathToGUID(texturePath);
            Assert(!string.IsNullOrEmpty(guid) && texturePath == spritePath,
                "reimport fixture starts from one standalone texture/sprite asset");

            AssetDatabase.ImportAsset(texturePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var reloadedSet = AssetDatabase.LoadAssetAtPath<RuntimeUiArtSet>(setPath);
            var reloadedBinding = reloadedSet.GetRequiredBinding(slot);
            Assert(AssetDatabase.AssetPathToGUID(texturePath) == guid,
                "in-place PNG reimport preserves its GUID");
            Assert(AssetDatabase.GetAssetPath(reloadedBinding.Texture) == texturePath
                && AssetDatabase.GetAssetPath(reloadedBinding.Sprite) == spritePath
                && reloadedBinding.Sprite.texture == reloadedBinding.Texture,
                "semantic texture/sprite binding survives in-place reimport");
            Assert(releaseTheme.ActiveArtSet == reloadedSet,
                "release theme retains the same art-set binding after reimport");
            AssertSceneBytesEqual(sceneBytes,
                "PNG reimport leaves every release scene byte unchanged");
        }

        private static void ValidateFixtureExclusion(RuntimeUiTheme releaseTheme)
        {
            Assert(FixtureRoot.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) < 0,
                "runtime UI fixtures are outside every Resources hierarchy");
            Assert(AssetDatabase.FindAssets("t:RuntimeUiArtSet", new[] { FixtureRoot }).Length == 0,
                "generated art-set fixtures are cleaned after validation");
            Assert(!HasFixtureDependency(AssetDatabase.GetAssetPath(releaseTheme)),
                "release theme has no fixture dependency");
            foreach (var scenePath in RuntimeUiArtSetRegistry.ReleaseScenes)
            {
                Assert(!HasFixtureDependency(scenePath),
                    scenePath + " has no runtime UI fixture dependency");
            }
        }

        private static bool HasFixtureDependency(string ownerPath)
        {
            return AssetDatabase.GetDependencies(ownerPath, true).Any(path =>
                Normalize(path).StartsWith(FixtureRoot + "/", StringComparison.Ordinal));
        }

        private static RuntimeUiArtSet Clone(RuntimeUiArtSet source, string name,
            ICollection<RuntimeUiArtSet> owner)
        {
            var clone = Object.Instantiate(source);
            clone.name = "RuntimeUiSmoke-" + name;
            clone.hideFlags = HideFlags.HideAndDontSave;
            owner.Add(clone);
            return clone;
        }

        private static void SetBindingArraySize(RuntimeUiArtSet artSet, int size)
        {
            var serialized = new SerializedObject(artSet);
            Require(serialized, "bindings").arraySize = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Require(SerializedObject owner, string propertyName)
        {
            var property = owner.FindProperty(propertyName);
            Assert(property != null, "serialized property exists: " + propertyName);
            return property;
        }

        private static void RestoreActiveWithoutUndo(RuntimeUiTheme releaseTheme,
            RuntimeUiArtSet activeSet)
        {
            if (releaseTheme == null) return;
            var serialized = new SerializedObject(releaseTheme);
            Require(serialized, "activeArtSet").objectReferenceValue = activeSet;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(releaseTheme);
        }

        private static void ValidateAllRequiredSlots(RuntimeUiTheme theme,
            RuntimeUiArtSet expectedSet, string message)
        {
            Assert(theme != null && theme.ActiveArtSet == expectedSet
                && expectedSet != null && expectedSet.Bindings.Count
                    == RuntimeUiArtSlots.RequiredCount,
                message + ": complete active set");
            var context = RuntimeUiDrawContext.Create(theme, 1f);
            Assert(context.ArtSet == expectedSet,
                message + ": drawing context owns the expected set");
            foreach (var slot in RuntimeUiArtSlots.Required)
            {
                var binding = expectedSet.GetRequiredBinding(slot);
                Assert(binding != null && binding.Slot == slot
                    && binding.Texture != null && binding.Sprite != null,
                    message + ": " + RuntimeUiArtSlots.SemanticId(slot));
            }
        }

        private static string CaptureThemeContractWithoutActive(RuntimeUiTheme theme)
        {
            Assert(theme != null, "theme exists for contract snapshot");
            var clone = Object.Instantiate(theme);
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var serialized = new SerializedObject(clone);
                Require(serialized, "activeArtSet").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return EditorJsonUtility.ToJson(clone, true);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        private static void AssertNoReleaseDependency(RuntimeUiArtSet candidate)
        {
            var candidatePath = Normalize(AssetDatabase.GetAssetPath(candidate));
            var runtimePrefix = Normalize(RuntimeUiArtSetRegistry.RuntimeDirectory(candidate)) + "/";
            var owners = new[] { RuntimeUiArtSetRegistry.ReleaseThemePath }
                .Concat(RuntimeUiArtSetRegistry.ReleaseScenes);
            foreach (var owner in owners)
            {
                var dependencies = AssetDatabase.GetDependencies(owner, true)
                    .Select(Normalize).ToArray();
                Assert(!dependencies.Contains(candidatePath, StringComparer.Ordinal)
                    && dependencies.All(path =>
                        !path.StartsWith(runtimePrefix, StringComparison.Ordinal)),
                    "inactive production candidate has zero release references: " + owner);
            }
        }

        private static IReadOnlyDictionary<string, byte[]> CaptureReleaseSceneBytes()
        {
            return RuntimeUiArtSetRegistry.ReleaseScenes.ToDictionary(path => path,
                ReadAssetBytes, StringComparer.Ordinal);
        }

        private static IReadOnlyDictionary<string, byte[]> CaptureAssetTreeBytes(
            string assetRoot, string searchPattern)
        {
            var projectRoot = Path.GetFullPath(Directory.GetCurrentDirectory())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var absoluteRoot = Path.GetFullPath(Path.Combine(projectRoot,
                Normalize(assetRoot)));
            Assert(Directory.Exists(absoluteRoot), "asset tree exists: " + assetRoot);
            return Directory.GetFiles(absoluteRoot, searchPattern,
                    SearchOption.AllDirectories)
                .Select(Path.GetFullPath)
                .Select(path => Normalize(path.Substring(projectRoot.Length)))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToDictionary(path => path, ReadAssetBytes, StringComparer.Ordinal);
        }

        private static IReadOnlyDictionary<string, byte[]> CaptureProtectedLayoutBytes()
        {
            var layoutPaths = new[]
            {
                "Assets/Scripts/Shell/PortraitShellLayout.cs",
                "Assets/Scripts/Presentation/BattleUiLayout.cs",
            };
            return layoutPaths.ToDictionary(path => path, ReadAssetBytes,
                StringComparer.Ordinal);
        }

        private static void AssertSceneBytesEqual(
            IReadOnlyDictionary<string, byte[]> expected, string message)
        {
            foreach (var pair in expected)
            {
                Assert(BytesEqual(pair.Value, ReadAssetBytes(pair.Key)),
                    message + ": " + pair.Key);
            }
        }

        private static void AssertAssetBytesEqual(
            IReadOnlyDictionary<string, byte[]> expected, string message)
        {
            foreach (var pair in expected)
            {
                Assert(BytesEqual(pair.Value, ReadAssetBytes(pair.Key)),
                    message + ": " + pair.Key);
            }
        }

        private static string ComputeSnapshotHash(
            IReadOnlyDictionary<string, byte[]> snapshot)
        {
            var rows = snapshot.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Key + "=" + ComputeHash(pair.Value));
            return ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", rows)));
        }

        private static string ComputeHash(byte[] bytes)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty);
        }

        private static byte[] ReadAssetBytes(string assetPath)
        {
            var absolute = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                Normalize(assetPath)));
            Assert(File.Exists(absolute), "asset exists: " + assetPath);
            return File.ReadAllBytes(absolute);
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            using (var hash = SHA256.Create())
                return hash.ComputeHash(left).SequenceEqual(hash.ComputeHash(right));
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace('\\', '/');
        }

        private static void DeleteGeneratedFixture()
        {
            if (AssetDatabase.LoadMainAssetAtPath(InvalidPathFixture) != null
                || File.Exists(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                    InvalidPathFixture))))
                AssetDatabase.DeleteAsset(InvalidPathFixture);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Runtime UI visual-system smoke failed: " + message);
        }
    }
}
