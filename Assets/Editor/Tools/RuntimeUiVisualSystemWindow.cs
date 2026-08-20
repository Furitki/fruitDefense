using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FruitDefense.Editor
{
    public static class RuntimeUiVisualSystemPreview
    {
        public static bool TryCreate(RuntimeUiTheme releaseTheme, RuntimeUiArtSet candidate,
            out RuntimeUiTheme previewTheme, out RuntimeUiVisualValidationReport report)
        {
            previewTheme = null;
            report = RuntimeUiVisualSystemValidator.ValidateCandidate(releaseTheme, candidate);
            if (!report.IsValid) return false;

            previewTheme = Object.Instantiate(releaseTheme);
            previewTheme.name = releaseTheme.name + " (Candidate Preview)";
            previewTheme.hideFlags = HideFlags.HideAndDontSave;
            var serialized = new SerializedObject(previewTheme);
            serialized.FindProperty("activeArtSet").objectReferenceValue = candidate;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (previewTheme.ActiveArtSet == candidate) return true;

            Object.DestroyImmediate(previewTheme);
            previewTheme = null;
            report.Error("preview.isolation.failure", AssetDatabase.GetAssetPath(releaseTheme),
                "The isolated preview clone did not receive the candidate art set.",
                "Repair the RuntimeUiTheme activeArtSet serialized contract.");
            return false;
        }
    }

    public static class RuntimeUiVisualSystemActivation
    {
        public const string UndoLabel = "Activate Runtime UI Art Set";

        public static bool TryActivate(RuntimeUiTheme releaseTheme, RuntimeUiArtSet candidate,
            out RuntimeUiVisualValidationReport report)
        {
            return TryActivate(releaseTheme, candidate, out report, out _);
        }

        public static bool TryActivate(RuntimeUiTheme releaseTheme, RuntimeUiArtSet candidate,
            out RuntimeUiVisualValidationReport report, out int undoGroup)
        {
            undoGroup = -1;
            report = RuntimeUiVisualSystemValidator.ValidateCandidate(releaseTheme, candidate);
            if (!report.IsValid) return false;
            if (releaseTheme.ActiveArtSet == candidate) return true;

            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            Undo.RecordObject(releaseTheme, UndoLabel);
            var serialized = new SerializedObject(releaseTheme);
            serialized.FindProperty("activeArtSet").objectReferenceValue = candidate;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Undo.FlushUndoRecordObjects();
            EditorUtility.SetDirty(releaseTheme);
            Undo.CollapseUndoOperations(undoGroup);

            if (EditorUtility.IsPersistent(releaseTheme))
            {
                AssetDatabase.SaveAssetIfDirty(releaseTheme);
                AssetDatabase.Refresh();
            }
            return releaseTheme.ActiveArtSet == candidate;
        }

        public static void ValidateReleaseAndWorkflowOrThrow()
        {
            RuntimeUiVisualSystemValidator.ValidateReleaseOrThrow();
            var releaseTheme = RuntimeUiArtSetRegistry.LoadReleaseTheme();
            var candidate = RuntimeUiArtSetRegistry.DiscoverProductionSets().FirstOrDefault();
            var beforeJson = EditorJsonUtility.ToJson(releaseTheme, true);
            var beforeDirty = EditorUtility.IsDirty(releaseTheme);

            RuntimeUiTheme preview = null;
            RuntimeUiTheme activationClone = null;
            try
            {
                if (!RuntimeUiVisualSystemPreview.TryCreate(releaseTheme, candidate,
                        out preview, out var previewReport))
                    throw new InvalidOperationException(RuntimeUiVisualSystemValidator.FormatReport(previewReport));
                if (preview.ActiveArtSet != candidate
                    || EditorJsonUtility.ToJson(releaseTheme, true) != beforeJson
                    || EditorUtility.IsDirty(releaseTheme) != beforeDirty)
                {
                    throw new InvalidOperationException(
                        "Candidate preview changed the serialized release theme or failed to isolate the candidate.");
                }

                activationClone = Object.Instantiate(releaseTheme);
                activationClone.hideFlags = HideFlags.HideAndDontSave;
                var serialized = new SerializedObject(activationClone);
                serialized.FindProperty("activeArtSet").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (!TryActivate(activationClone, candidate, out var activationReport,
                        out var undoGroup))
                    throw new InvalidOperationException(RuntimeUiVisualSystemValidator.FormatReport(activationReport));
                if (activationClone.ActiveArtSet != candidate || undoGroup < 0)
                    throw new InvalidOperationException("Valid candidate activation was not atomic.");

                Undo.RevertAllDownToGroup(undoGroup);
                if (activationClone.ActiveArtSet != null)
                    throw new InvalidOperationException("The single activation Undo group did not restore the prior set.");

                var invalidBefore = EditorJsonUtility.ToJson(activationClone, true);
                if (TryActivate(activationClone, null, out _)
                    || EditorJsonUtility.ToJson(activationClone, true) != invalidBefore)
                    throw new InvalidOperationException("Invalid candidate activation changed the target theme.");
            }
            finally
            {
                if (preview != null) Object.DestroyImmediate(preview);
                if (activationClone != null) Object.DestroyImmediate(activationClone);
            }

            if (EditorJsonUtility.ToJson(releaseTheme, true) != beforeJson
                || EditorUtility.IsDirty(releaseTheme) != beforeDirty)
                throw new InvalidOperationException("Workflow validation changed the release theme asset.");
            Debug.Log("Runtime UI preview isolation, valid activation, invalid rejection and single-group Undo passed.");
        }
    }

    public sealed class RuntimeUiVisualSystemWindow : EditorWindow
    {
        private static readonly RuntimeUiInteractionState[] GalleryStates =
            (RuntimeUiInteractionState[])Enum.GetValues(typeof(RuntimeUiInteractionState));

        private Vector2 scroll;
        private RuntimeUiTheme releaseTheme;
        private RuntimeUiTheme previewTheme;
        private RuntimeUiArtSet[] candidates = Array.Empty<RuntimeUiArtSet>();
        private RuntimeUiArtSet candidate;
        private RuntimeUiDrawContext drawContext;
        private RuntimeUiVisualValidationReport candidateReport;
        private RuntimeUiVisualValidationReport releaseReport;
        private string transientMessage;

        [MenuItem("Fruit Defense/UI/Visual System")]
        public static void Open()
        {
            var window = GetWindow<RuntimeUiVisualSystemWindow>();
            window.titleContent = new GUIContent("UI Visual System");
            window.minSize = new Vector2(720f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleExternalChange;
            EditorApplication.projectChanged += HandleExternalChange;
            RefreshAll();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleExternalChange;
            EditorApplication.projectChanged -= HandleExternalChange;
            DestroyPreview();
        }

        private void OnGUI()
        {
            DrawHeader();
            if (releaseTheme == null)
            {
                EditorGUILayout.HelpBox("Missing fixed release theme: "
                    + RuntimeUiArtSetRegistry.ReleaseThemePath, MessageType.Error);
                return;
            }

            DrawCandidatePicker();
            DrawValidation(candidateReport, "Candidate validation");
            if (drawContext == null) return;

            scroll = EditorGUILayout.BeginScrollView(scroll, true, true);
            DrawStateGallery();
            GUILayout.Space(12f);
            DrawSemanticArtStrip();
            GUILayout.Space(12f);
            DrawRouteChrome();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    RefreshAll();
                GUILayout.FlexibleSpace();
                GUILayout.Label("One theme · one active set · isolated candidate preview",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Release theme", Identity(releaseTheme));
            EditorGUILayout.LabelField("Active set",
                releaseTheme == null ? "—" : Identity(releaseTheme.ActiveArtSet));
            EditorGUILayout.LabelField("Candidate", Identity(candidate));
            if (!string.IsNullOrEmpty(transientMessage))
                EditorGUILayout.HelpBox(transientMessage, MessageType.Info);
            DrawValidation(releaseReport, "Release validation");
        }

        private void DrawCandidatePicker()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var selected = Mathf.Max(0, Array.IndexOf(candidates, candidate));
                var labels = candidates.Select(Identity).ToArray();
                EditorGUI.BeginChangeCheck();
                var next = labels.Length == 0
                    ? -1
                    : EditorGUILayout.Popup("Production set", selected, labels);
                if (EditorGUI.EndChangeCheck() && next >= 0)
                    SelectCandidate(candidates[next]);

                using (new EditorGUI.DisabledScope(candidate == null
                    || candidateReport == null || !candidateReport.IsValid
                    || releaseTheme.ActiveArtSet == candidate))
                {
                    if (GUILayout.Button("Activate", GUILayout.Width(88f))) ActivateCandidate();
                }
            }
            if (candidates.Length == 0)
            {
                EditorGUILayout.HelpBox("No RuntimeUiArtSet assets were discovered under "
                    + RuntimeUiArtSetRegistry.ArtSetRoot + ".", MessageType.Error);
            }
        }

        private static void DrawValidation(RuntimeUiVisualValidationReport report, string title)
        {
            if (report == null) return;
            if (report.IsValid && report.WarningCount == 0) return;
            EditorGUILayout.LabelField(title + ": " + report.Summary(), EditorStyles.boldLabel);
            foreach (var issue in report.Issues)
            {
                var type = issue.Severity == RuntimeUiVisualIssueSeverity.Error
                    ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.HelpBox(issue.ToString(), type);
            }
        }

        private void DrawStateGallery()
        {
            EditorGUILayout.LabelField("Complete component × nine-state gallery",
                EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(142f);
                foreach (var state in GalleryStates)
                    GUILayout.Label(StateLabel(state), EditorStyles.miniLabel, GUILayout.Width(116f));
            }

            foreach (RuntimeUiComponentKind kind in Enum.GetValues(typeof(RuntimeUiComponentKind)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(kind.ToString(), EditorStyles.miniLabel, GUILayout.Width(138f));
                    foreach (var state in GalleryStates)
                    {
                        var rect = GUILayoutUtility.GetRect(116f, 72f,
                            GUILayout.Width(116f), GUILayout.Height(72f));
                        DrawComponentCell(kind, state, rect);
                    }
                }
            }
        }

        private void DrawComponentCell(RuntimeUiComponentKind kind,
            RuntimeUiInteractionState state, Rect rect)
        {
            switch (kind)
            {
                case RuntimeUiComponentKind.Screen:
                    RuntimeUiGui.DrawScreenBackground(drawContext, rect);
                    break;
                case RuntimeUiComponentKind.SafeArea:
                    RuntimeUiGui.DrawSafeArea(drawContext, rect);
                    break;
                case RuntimeUiComponentKind.StandardPanel:
                    RuntimeUiGui.DrawStandardPanel(drawContext, rect, state);
                    break;
                case RuntimeUiComponentKind.RaisedPanel:
                    RuntimeUiGui.DrawRaisedPanel(drawContext, rect, state);
                    break;
                case RuntimeUiComponentKind.SelectableCard:
                    RuntimeUiGui.DrawSelectableCard(drawContext, rect, state);
                    break;
                case RuntimeUiComponentKind.PrimaryButton:
                    RuntimeUiGui.DrawAction(drawContext, rect, "开始",
                        RuntimeUiActionKind.Primary, state, RuntimeUiArtSlot.IconControlStart);
                    break;
                case RuntimeUiComponentKind.SecondaryButton:
                    RuntimeUiGui.DrawAction(drawContext, rect, "刷新",
                        RuntimeUiActionKind.Secondary, state, RuntimeUiArtSlot.IconControlRefresh);
                    break;
                case RuntimeUiComponentKind.QuietButton:
                    RuntimeUiGui.DrawAction(drawContext, rect, "返回",
                        RuntimeUiActionKind.Quiet, state, RuntimeUiArtSlot.IconControlReturn);
                    break;
                case RuntimeUiComponentKind.DangerButton:
                    RuntimeUiGui.DrawAction(drawContext, rect, "重试",
                        RuntimeUiActionKind.Danger, state, RuntimeUiArtSlot.IconControlRetry);
                    break;
                case RuntimeUiComponentKind.Metric:
                    RuntimeUiGui.DrawMetric(drawContext, rect, RuntimeUiArtSlot.IconResourceSun,
                        "阳光", "120", state);
                    break;
                case RuntimeUiComponentKind.Status:
                    RuntimeUiGui.DrawStatus(drawContext, rect, "波次 2 / 5", state);
                    break;
                case RuntimeUiComponentKind.DetailCard:
                    RuntimeUiGui.DrawDetailCard(drawContext, rect, state);
                    break;
                case RuntimeUiComponentKind.BlockingModal:
                    RuntimeUiGui.DrawBlockingModal(drawContext, rect,
                        Inset(rect, 9f), state);
                    break;
                case RuntimeUiComponentKind.ResultCard:
                    RuntimeUiGui.DrawResultCard(drawContext, rect, state);
                    break;
                case RuntimeUiComponentKind.ToolSlot:
                    RuntimeUiGui.DrawSlot(drawContext, rect, RuntimeUiSlotKind.Tool, state);
                    break;
                case RuntimeUiComponentKind.NurserySlot:
                    RuntimeUiGui.DrawSlot(drawContext, rect, RuntimeUiSlotKind.Nursery, state);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private void DrawSemanticArtStrip()
        {
            EditorGUILayout.LabelField("All " + RuntimeUiArtSlots.RequiredCount
                + " semantic slots", EditorStyles.boldLabel);
            const int columns = 10;
            var slots = RuntimeUiArtSlots.Required;
            for (var start = 0; start < slots.Count; start += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var offset = 0; offset < columns && start + offset < slots.Count; offset++)
                    {
                        var slot = slots[start + offset];
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(116f)))
                        {
                            var rect = GUILayoutUtility.GetRect(76f, 60f,
                                GUILayout.Width(76f), GUILayout.Height(60f));
                            DrawSlotSample(slot, rect);
                            GUILayout.Label(RuntimeUiArtSlots.SemanticId(slot), EditorStyles.miniLabel,
                                GUILayout.Width(112f), GUILayout.Height(30f));
                        }
                    }
                }
            }
        }

        private void DrawSlotSample(RuntimeUiArtSlot slot, Rect rect)
        {
            switch (slot)
            {
                case RuntimeUiArtSlot.SurfaceScreenBackground:
                    RuntimeUiGui.DrawScreenBackground(drawContext, rect); break;
                case RuntimeUiArtSlot.SurfaceSafeArea:
                    RuntimeUiGui.DrawSafeArea(drawContext, rect); break;
                case RuntimeUiArtSlot.SurfacePanelStandard:
                    RuntimeUiGui.DrawStandardPanel(drawContext, rect); break;
                case RuntimeUiArtSlot.SurfacePanelRaised:
                    RuntimeUiGui.DrawRaisedPanel(drawContext, rect); break;
                case RuntimeUiArtSlot.SurfaceCardSelectable:
                    RuntimeUiGui.DrawSelectableCard(drawContext, rect, RuntimeUiInteractionState.Normal); break;
                case RuntimeUiArtSlot.SurfaceMetric:
                    RuntimeUiGui.DrawMetric(drawContext, rect, RuntimeUiArtSlot.IconResourceSun,
                        "", "12", RuntimeUiInteractionState.Normal); break;
                case RuntimeUiArtSlot.SurfaceStatus:
                    RuntimeUiGui.DrawStatus(drawContext, rect, "状态", RuntimeUiInteractionState.Normal); break;
                case RuntimeUiArtSlot.SurfaceDetail:
                    RuntimeUiGui.DrawDetailCard(drawContext, rect); break;
                case RuntimeUiArtSlot.SurfaceModal:
                    RuntimeUiGui.DrawBlockingModal(drawContext, rect, Inset(rect, 7f)); break;
                case RuntimeUiArtSlot.SurfaceResult:
                    RuntimeUiGui.DrawResultCard(drawContext, rect, RuntimeUiInteractionState.Success); break;
                case RuntimeUiArtSlot.SurfaceScrim:
                    RuntimeUiGui.DrawBlockingModal(drawContext, rect, new Rect()); break;
                case RuntimeUiArtSlot.ActionPrimary:
                    RuntimeUiGui.DrawAction(drawContext, rect, string.Empty,
                        RuntimeUiActionKind.Primary, RuntimeUiInteractionState.Normal); break;
                case RuntimeUiArtSlot.ActionSecondary:
                    RuntimeUiGui.DrawAction(drawContext, rect, string.Empty,
                        RuntimeUiActionKind.Secondary, RuntimeUiInteractionState.Normal); break;
                case RuntimeUiArtSlot.ActionQuiet:
                    RuntimeUiGui.DrawAction(drawContext, rect, string.Empty,
                        RuntimeUiActionKind.Quiet, RuntimeUiInteractionState.Normal); break;
                case RuntimeUiArtSlot.ActionDanger:
                    RuntimeUiGui.DrawAction(drawContext, rect, string.Empty,
                        RuntimeUiActionKind.Danger, RuntimeUiInteractionState.Normal); break;
                case RuntimeUiArtSlot.SlotTool:
                    RuntimeUiGui.DrawSlot(drawContext, rect, RuntimeUiSlotKind.Tool,
                        RuntimeUiInteractionState.Normal); break;
                case RuntimeUiArtSlot.SlotNursery:
                    RuntimeUiGui.DrawSlot(drawContext, rect, RuntimeUiSlotKind.Nursery,
                        RuntimeUiInteractionState.Normal); break;
                case RuntimeUiArtSlot.OrnamentScreenCorner:
                    RuntimeUiGui.DrawScreenCorners(drawContext, rect); break;
                case RuntimeUiArtSlot.SurfaceSectionRibbon:
                    RuntimeUiGui.DrawSectionRibbon(drawContext, rect); break;
                case RuntimeUiArtSlot.SurfaceIllustrationFrame:
                    RuntimeUiGui.DrawIllustrationFrame(drawContext, rect); break;
                case RuntimeUiArtSlot.OrnamentMetricDivider:
                    RuntimeUiGui.DrawMetricDivider(drawContext, rect); break;
                case RuntimeUiArtSlot.OrnamentResultBanner:
                    RuntimeUiGui.DrawResultBanner(drawContext, rect); break;
                case RuntimeUiArtSlot.IllustrationOrchardVista:
                    RuntimeUiGui.DrawOrchardVista(drawContext, rect); break;
                case RuntimeUiArtSlot.IllustrationLobbyOrchard01:
                    RuntimeUiGui.DrawLobbyThumbnail(drawContext, rect,
                        RuntimeUiLobbyThumbnail.Orchard01); break;
                case RuntimeUiArtSlot.IllustrationLobbyOrchard02:
                    RuntimeUiGui.DrawLobbyThumbnail(drawContext, rect,
                        RuntimeUiLobbyThumbnail.Orchard02); break;
                case RuntimeUiArtSlot.IllustrationLobbyOrchard03:
                    RuntimeUiGui.DrawLobbyThumbnail(drawContext, rect,
                        RuntimeUiLobbyThumbnail.Orchard03); break;
                default:
                    RuntimeUiGui.DrawIcon(drawContext, rect, slot); break;
            }
        }

        private void DrawRouteChrome()
        {
            EditorGUILayout.LabelField("Representative release-route chrome",
                EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRouteCard("Lobby", DrawLobbyChrome);
                DrawRouteCard("Battle", DrawBattleChrome);
                DrawRouteCard("Settlement", DrawSettlementChrome);
            }
        }

        private void DrawRouteCard(string title, Action<Rect> draw)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(300f)))
            {
                GUILayout.Label(title, EditorStyles.miniBoldLabel);
                var rect = GUILayoutUtility.GetRect(280f, 420f,
                    GUILayout.Width(280f), GUILayout.Height(420f));
                draw(rect);
            }
        }

        private void DrawLobbyChrome(Rect rect)
        {
            RuntimeUiGui.DrawScreenBackground(drawContext, rect);
            var safe = Inset(rect, 12f);
            RuntimeUiGui.DrawSafeArea(drawContext, safe);
            RuntimeUiGui.DrawScreenCorners(drawContext, safe);
            var title = new Rect(safe.x + 16f, safe.y + 14f,
                safe.width - 32f, 42f);
            RuntimeUiGui.DrawSectionRibbon(drawContext, title);
            RuntimeUiGui.DrawText(drawContext, title, "水果塔防", RuntimeUiTypographyRole.ScreenTitle,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter);
            var card = new Rect(safe.x + 16f, safe.y + 74f, safe.width - 32f, 82f);
            RuntimeUiGui.DrawSelectableCard(drawContext, card,
                RuntimeUiInteractionState.Selected, drawStateIndicator: false);
            var frame = new Rect(card.x + 8f, card.y + 14f, 84f, 54f);
            RuntimeUiGui.DrawLobbyThumbnail(drawContext, Inset(frame, 3f),
                RuntimeUiLobbyThumbnail.Orchard01);
            RuntimeUiGui.DrawIllustrationFrame(drawContext, frame);
            RuntimeUiGui.DrawIndicator(drawContext,
                new Rect(card.xMax - 56f, card.y + 8f, 48f, 48f),
                RuntimeUiIndicatorKind.Selected);
            RuntimeUiGui.DrawAction(drawContext,
                new Rect(safe.x + 28f, safe.yMax - 76f, safe.width - 56f, 56f), "开始关卡",
                RuntimeUiActionKind.Primary, RuntimeUiInteractionState.Normal,
                RuntimeUiArtSlot.IconControlStart);
        }

        private void DrawBattleChrome(Rect rect)
        {
            RuntimeUiGui.DrawScreenBackground(drawContext, rect);
            var top = new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 76f);
            RuntimeUiGui.DrawRaisedPanel(drawContext, top);
            RuntimeUiGui.DrawMetric(drawContext, new Rect(top.x + 8f, top.y + 8f, 112f, 60f),
                RuntimeUiArtSlot.IconResourceSun, "阳光", "120", RuntimeUiInteractionState.Normal);
            RuntimeUiGui.DrawMetricDivider(drawContext,
                new Rect(top.center.x - 4f, top.y + 30f, 8f, 16f));
            RuntimeUiGui.DrawStatus(drawContext, new Rect(top.xMax - 124f, top.y + 8f, 116f, 60f),
                "波次 2 / 5", RuntimeUiInteractionState.Warning);
            var modal = new Rect(rect.x + 30f, rect.y + 112f, rect.width - 60f, 178f);
            RuntimeUiGui.DrawResultCard(drawContext, modal, RuntimeUiInteractionState.Success);
            var ribbon = new Rect(modal.x + 12f, modal.y + 10f, modal.width - 24f, 42f);
            RuntimeUiGui.DrawSectionRibbon(drawContext, ribbon);
            RuntimeUiGui.DrawText(drawContext, ribbon, "果园守住了！",
                RuntimeUiTypographyRole.SectionTitle, RuntimeUiTextTone.State,
                TextAnchor.MiddleCenter, RuntimeUiInteractionState.Success);
            RuntimeUiGui.DrawOrchardVista(drawContext,
                new Rect(modal.x + 14f, modal.y + 64f, 74f, 72f));
            var resultBanner = new Rect(modal.x + 98f, modal.y + 64f,
                modal.width - 112f, 72f);
            RuntimeUiGui.DrawResultBanner(drawContext, resultBanner);
            RuntimeUiGui.DrawIndicator(drawContext,
                new Rect(resultBanner.xMax - 28f, resultBanner.y + 8f, 20f, 20f),
                RuntimeUiIndicatorKind.Success);
            RuntimeUiGui.DrawSlot(drawContext, new Rect(rect.x + 24f, rect.yMax - 92f, 68f, 68f),
                RuntimeUiSlotKind.Tool, RuntimeUiInteractionState.Selected);
            RuntimeUiGui.DrawAction(drawContext,
                new Rect(rect.xMax - 92f, rect.yMax - 92f, 68f, 68f), string.Empty,
                RuntimeUiActionKind.Secondary, RuntimeUiInteractionState.Normal,
                RuntimeUiArtSlot.IconControlPause);
        }

        private void DrawSettlementChrome(Rect rect)
        {
            RuntimeUiGui.DrawScreenBackground(drawContext, rect);
            RuntimeUiGui.DrawScreenCorners(drawContext, rect);
            var title = new Rect(rect.x + 38f, rect.y + 18f, rect.width - 76f, 42f);
            RuntimeUiGui.DrawSectionRibbon(drawContext, title);
            RuntimeUiGui.DrawText(drawContext, title, "战斗结算",
                RuntimeUiTypographyRole.ScreenTitle, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleCenter);
            var card = new Rect(rect.x + 26f, rect.y + 72f,
                rect.width - 52f, rect.height - 98f);
            RuntimeUiGui.DrawResultCard(drawContext, card, RuntimeUiInteractionState.Success);
            var banner = new Rect(card.x + 14f, card.y + 10f, card.width - 28f, 66f);
            RuntimeUiGui.DrawResultBanner(drawContext, banner);
            RuntimeUiGui.DrawOrchardVista(drawContext,
                new Rect(card.x + 18f, card.y + 92f, 108f, 80f));
            RuntimeUiGui.DrawMetricDivider(drawContext,
                new Rect(card.center.x + 32f, card.y + 144f, 8f, 16f));
            RuntimeUiGui.DrawIndicator(drawContext,
                new Rect(banner.xMax - 36f, banner.y + 18f, 28f, 28f),
                RuntimeUiIndicatorKind.Success);
            RuntimeUiGui.DrawText(drawContext, new Rect(card.x + 20f, card.y + 36f,
                    card.width - 40f, 52f), "胜利", RuntimeUiTypographyRole.Display,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter,
                RuntimeUiInteractionState.Success);
            RuntimeUiGui.DrawAction(drawContext,
                new Rect(card.x + 24f, card.yMax - 140f, card.width - 48f, 52f), "继续",
                RuntimeUiActionKind.Primary, RuntimeUiInteractionState.Normal,
                RuntimeUiArtSlot.IconControlContinue);
            RuntimeUiGui.DrawAction(drawContext,
                new Rect(card.x + 24f, card.yMax - 76f, card.width - 48f, 48f), "返回大厅",
                RuntimeUiActionKind.Quiet, RuntimeUiInteractionState.Normal,
                RuntimeUiArtSlot.IconControlReturn);
        }

        private void ActivateCandidate()
        {
            if (!RuntimeUiVisualSystemActivation.TryActivate(releaseTheme, candidate,
                    out var report))
            {
                candidateReport = report;
                transientMessage = "Activation rejected; the release theme was not changed.";
                Repaint();
                return;
            }
            transientMessage = "Activated " + Identity(candidate)
                + ". Use Edit → Undo to restore the prior active set.";
            RefreshAll(candidate);
        }

        private void HandleExternalChange()
        {
            RefreshAll(candidate);
        }

        private void RefreshAll(RuntimeUiArtSet preferred = null)
        {
            var preferredPath = AssetDatabase.GetAssetPath(preferred ?? candidate);
            releaseTheme = RuntimeUiArtSetRegistry.LoadReleaseTheme();
            candidates = RuntimeUiArtSetRegistry.DiscoverProductionSets().ToArray();
            var next = candidates.FirstOrDefault(set =>
                AssetDatabase.GetAssetPath(set) == preferredPath);
            if (next == null && releaseTheme != null)
                next = candidates.FirstOrDefault(set => set == releaseTheme.ActiveArtSet);
            if (next == null) next = candidates.FirstOrDefault();
            releaseReport = RuntimeUiVisualSystemValidator.ValidateRelease();
            SelectCandidate(next);
            Repaint();
        }

        private void SelectCandidate(RuntimeUiArtSet next)
        {
            DestroyPreview();
            candidate = next;
            candidateReport = RuntimeUiVisualSystemValidator.ValidateCandidate(releaseTheme, candidate);
            if (!candidateReport.IsValid) return;
            if (!RuntimeUiVisualSystemPreview.TryCreate(releaseTheme, candidate,
                    out previewTheme, out candidateReport)) return;
            try
            {
                drawContext = RuntimeUiDrawContext.Create(previewTheme, 1f);
            }
            catch (Exception exception)
            {
                candidateReport.Error("preview.context", AssetDatabase.GetAssetPath(candidate),
                    exception.Message, "Correct the validated theme/art-set drawing contract.");
                DestroyPreview();
            }
        }

        private void DestroyPreview()
        {
            drawContext = null;
            if (previewTheme != null) Object.DestroyImmediate(previewTheme);
            previewTheme = null;
        }

        private static Rect Inset(Rect rect, float amount)
        {
            return new Rect(rect.x + amount, rect.y + amount,
                Mathf.Max(0f, rect.width - amount * 2f),
                Mathf.Max(0f, rect.height - amount * 2f));
        }

        private static string Identity(RuntimeUiTheme theme)
        {
            return theme == null ? "—" : theme.ThemeId + " @ " + theme.Revision;
        }

        private static string Identity(RuntimeUiArtSet set)
        {
            return set == null ? "—" : set.SetId + " @ " + set.Revision;
        }

        private static string StateLabel(RuntimeUiInteractionState state)
        {
            return state == RuntimeUiInteractionState.HoveredOrFocused
                ? "Hovered / Focused" : state.ToString();
        }
    }
}
