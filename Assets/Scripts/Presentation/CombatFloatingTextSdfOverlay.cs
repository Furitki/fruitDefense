using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using FruitDefense.Core;
using Unity.Profiling;
using UnityEngine;

namespace FruitDefense.Presentation
{
    public sealed class CombatFloatingTextSdfOverlay : MonoBehaviour
    {
        private static readonly ProfilerMarker RenderMarker =
            new ProfilerMarker("FruitDefense.CombatFloatingText.Render");

        public const int PoolCapacity = CombatFloatingTextStyleCatalog.TotalCapacity;
        public const int SharedMaterialCount = 0;
        public const int AtlasSize = 512;
        public const int MaximumGlyphsPerLabel = 16;
        public const int DrawCommandCapacity = PoolCapacity * MaximumGlyphsPerLabel;
        public const int AcceptanceWarmupSampleCount = 120;
        public const int AcceptanceActiveSampleCount = 600;
        public const float MaximumAnchorHorizontalError = 20f;
        private const int AcceptanceProfileActiveRecordCount = 12;
        public const string AcceptanceAllocationMetric =
            "GC.GetAllocatedBytesForCurrentThread epoch-normalized into an "
            + "acceptance-session cumulative managed-allocation counter";
        public const string AcceptancePerformanceScope =
            "CombatFloatingTextSdfOverlay command preparation plus final IMGUI-layer "
            + "RGBA atlas glyph submissions; GPU raster excluded";
        public const string AtlasResourcePath = "CombatFeedback/CombatFloatingTextAtlas";
        public const string MetadataResourcePath = "CombatFeedback/CombatFloatingTextAtlasMetadata";

        private const float ContactGap = 8f;
        private const float GeometryEpsilon = .05f;
        private const float AtlasUvScale = 1f / AtlasSize;

        private sealed class Slot
        {
            public long EventSequence;
            public bool Assigned;
            public int ActiveIndex;
            public int LastSeenSync;
            public Vector2 FinalScreenCenter;
            public Vector2 AnchorScreen;
            public Rect FinalScreenBounds;
            public float AnchorScreenError;
        }

        private struct GlyphDrawCommand
        {
            public Rect ScreenRect;
            public Rect UvRect;
        }

        private struct LabelDrawRange
        {
            public int Start;
            public int Count;
            public Color Color;
        }

        private readonly struct AcceptanceScopeSample
        {
            public AcceptanceScopeSample(long timestamp, long allocatedBytes)
            {
                Timestamp = timestamp;
                AllocatedBytes = allocatedBytes;
                Enabled = true;
            }
            public long Timestamp { get; }
            public long AllocatedBytes { get; }
            public bool Enabled { get; }
        }

        private readonly Slot[] _slots = new Slot[PoolCapacity];
        private readonly int[] _activeSlotIndices = new int[PoolCapacity];
        private readonly int[] _freeSlotIndices = new int[PoolCapacity];
        private readonly Dictionary<long, int> _slotIndexByEventSequence =
            new Dictionary<long, int>(PoolCapacity);
        private readonly GlyphDrawCommand[] _drawCommands =
            new GlyphDrawCommand[DrawCommandCapacity];
        private readonly LabelDrawRange[] _labelDrawRanges =
            new LabelDrawRange[PoolCapacity];
        private readonly CombatFloatingTextGlyph[] _glyphScratch =
            new CombatFloatingTextGlyph[MaximumGlyphsPerLabel];
        private CombatFloatingTextAtlasMetadata _metadata;
        private Texture2D _atlas;
        private int _drawCommandCount;
        private int _labelDrawRangeCount;
        private int _activeSlotCount;
        private int _freeSlotCount;
        private int _slotSyncVersion;
        private bool _initialized;
        private bool _placementValid = true;
        private string _placementFailure = string.Empty;

        private bool _acceptanceProfileActive;
        private bool _acceptanceProfileSupported;
        private bool _acceptanceProfileCompleted;
        private int _acceptanceProfileWarmupCount;
        private int _acceptanceProfileSampleCount;
        private long _acceptanceProfileFirstTimestamp;
        private long _acceptanceProfileLastTimestamp;
        private long _acceptanceProfileAllocatedBytes;
        private float _acceptanceProfileP95Milliseconds;
        private float[] _acceptanceProfileSamplesMilliseconds = Array.Empty<float>();
        private string _acceptanceProfileFailure = string.Empty;
        private bool _acceptanceAllocationCounterInitialized;
        private long _acceptanceAllocationCounterPreviousRaw;
        private long _acceptanceAllocationCounterCumulative;
        private bool _acceptanceSubmissionPending;
        private long _acceptancePendingTimestamp;
        private long _acceptancePendingDurationTicks;
        private long _acceptancePendingAllocatedBytes;

        public CombatFloatingTextAtlasMetadata Metadata { get { return _metadata; } }
        public Texture2D Atlas { get { return _atlas; } }
        public int ActiveTextCount { get; private set; }
        public int PreparedAtlasDrawCount { get { return _drawCommandCount; } }
        public int PreparedLabelDrawCount { get { return _labelDrawRangeCount; } }
        public bool PlacementValid { get { return _placementValid; } }
        public string PlacementFailure { get { return _placementFailure; } }
        public bool AcceptanceProfileActive { get { return _acceptanceProfileActive; } }
        public bool AcceptanceProfileSupported { get { return _acceptanceProfileSupported; } }
        public bool AcceptanceProfileCompleted { get { return _acceptanceProfileCompleted; } }
        public int AcceptanceProfileWarmupCount { get { return _acceptanceProfileWarmupCount; } }
        public int AcceptanceProfileSampleCount { get { return _acceptanceProfileSampleCount; } }
        public long AcceptanceProfileAllocatedBytes { get { return _acceptanceProfileAllocatedBytes; } }
        public float AcceptanceProfileP95Milliseconds { get { return _acceptanceProfileP95Milliseconds; } }
        public IReadOnlyList<float> AcceptanceProfileSamplesMilliseconds
        {
            get { return _acceptanceProfileSamplesMilliseconds; }
        }
        public string AcceptanceProfileFailure { get { return _acceptanceProfileFailure; } }
        public float AcceptanceProfileElapsedSeconds
        {
            get
            {
                if (_acceptanceProfileFirstTimestamp <= 0
                    || _acceptanceProfileLastTimestamp <= _acceptanceProfileFirstTimestamp)
                    return 0f;
                return (float)((_acceptanceProfileLastTimestamp
                    - _acceptanceProfileFirstTimestamp) / (double)Stopwatch.Frequency);
            }
        }
        public float AcceptanceProfileAllocatedBytesPerSecond
        {
            get
            {
                var elapsed = AcceptanceProfileElapsedSeconds;
                return elapsed <= 0f ? 0f : _acceptanceProfileAllocatedBytes / elapsed;
            }
        }

        public void BeginAcceptanceSyncProfile()
        {
            _acceptanceProfileActive = true;
            _acceptanceProfileSupported = true;
            _acceptanceProfileCompleted = false;
            _acceptanceProfileWarmupCount = 0;
            _acceptanceProfileSampleCount = 0;
            _acceptanceProfileFirstTimestamp = 0;
            _acceptanceProfileLastTimestamp = 0;
            _acceptanceProfileAllocatedBytes = 0;
            _acceptanceProfileP95Milliseconds = 0f;
            _acceptanceProfileSamplesMilliseconds = new float[AcceptanceActiveSampleCount];
            _acceptanceProfileFailure = string.Empty;
            _acceptanceAllocationCounterInitialized = false;
            _acceptanceAllocationCounterPreviousRaw = 0;
            _acceptanceAllocationCounterCumulative = 0;
            _acceptanceSubmissionPending = false;
            long ignored;
            string failure;
            if (!TryReadAcceptanceAllocatedBytes(out ignored, out failure))
                FailAcceptanceSyncProfile("measurement-api-unavailable:" + failure);
        }

        public static bool TryCreate(Transform parent,
            out CombatFloatingTextSdfOverlay overlay, out string reason)
        {
            overlay = null;
            CombatFloatingTextAtlasMetadata metadata;
            Texture2D atlas;
            if (!TryLoadAndValidateAssets(out metadata, out atlas, out reason))
                return false;
            var root = new GameObject("Combat Floating Text SDF Overlay",
                typeof(CombatFloatingTextSdfOverlay));
            if (parent != null) root.transform.SetParent(parent, false);
            overlay = root.GetComponent<CombatFloatingTextSdfOverlay>();
            try
            {
                overlay.Initialize(metadata, atlas);
                reason = "ok";
                return true;
            }
            catch (Exception exception)
            {
                reason = "combat-sdf-overlay-initialization-failed:" + exception.Message;
                overlay.Dispose();
                overlay = null;
                return false;
            }
        }

        public static bool TryValidateProductionAssets(out string reason)
        {
            CombatFloatingTextAtlasMetadata metadata;
            Texture2D atlas;
            return TryLoadAndValidateAssets(out metadata, out atlas, out reason);
        }

        public void Sync(IReadOnlyList<PresentationFeedback> records,
            CombatFloatingTextStyleCatalog styles,
            BattlefieldProjection projection,
            BattlefieldViewportLayout viewport, Rect battlefieldSurface,
            Vector2 battlefieldOffset)
        {
            var acceptanceSample = BeginAcceptanceCommandSample(records);
            try
            {
                using (RenderMarker.Auto())
                {
                    ClearPreparedGeometry();
                    if (!_initialized || records == null || styles == null
                        || projection == null) return;
                    BeginSlotSync();
                    for (var index = 0; index < records.Count; index++)
                    {
                        var feedback = records[index];
                        if (feedback != null) MarkSlotSeen(feedback.EventSequence);
                        if (feedback == null
                            || feedback.Role == CombatFloatingTextRole.None
                            || string.IsNullOrEmpty(feedback.Text)) continue;
                        var slot = ResolveSlot(feedback.EventSequence);
                        if (slot == null)
                        {
                            InvalidatePlacement("floating-text-pool-capacity-exceeded");
                            break;
                        }
                        if (!AppendLabel(slot, feedback, styles, projection,
                                viewport, battlefieldSurface, battlefieldOffset)) break;
                        ActiveTextCount++;
                    }
                    ReleaseUnseenSlots();
                }
            }
            finally
            {
                EndAcceptanceCommandSample(acceptanceSample);
            }
        }

        public bool DrawOnGuiRepaint()
        {
            if (!_initialized || _atlas == null || Event.current == null
                || Event.current.type != EventType.Repaint) return false;
            var acceptanceSample = BeginAcceptanceSubmissionSample();
            var submitted = false;
            var previousColor = GUI.color;
            try
            {
                using (RenderMarker.Auto())
                {
                    // FruitDefenseGame calls this as its final OnGUI layer and owns
                    // the outer matrix restoration in its OnGUI try/finally.
                    GUI.matrix = Matrix4x4.identity;
                    for (var rangeIndex = 0;
                         rangeIndex < _labelDrawRangeCount; rangeIndex++)
                    {
                        var range = _labelDrawRanges[rangeIndex];
                        GUI.color = range.Color;
                        var end = range.Start + range.Count;
                        for (var commandIndex = range.Start;
                             commandIndex < end; commandIndex++)
                        {
                            var command = _drawCommands[commandIndex];
                            GUI.DrawTextureWithTexCoords(command.ScreenRect,
                                _atlas, command.UvRect, true);
                        }
                    }
                    submitted = true;
                }
                return true;
            }
            finally
            {
                GUI.color = previousColor;
                EndAcceptanceSubmissionSample(acceptanceSample, submitted);
            }
        }

        public bool TryGetScreenPlacement(long eventSequence,
            out Vector2 finalScreenCenter, out Vector2 anchorScreen,
            out float anchorScreenError, out Rect finalScreenBounds)
        {
            int slotIndex;
            if (_slotIndexByEventSequence.TryGetValue(eventSequence, out slotIndex))
            {
                var slot = _slots[slotIndex];
                if (slot.Assigned && slot.EventSequence == eventSequence)
                {
                    finalScreenCenter = slot.FinalScreenCenter;
                    anchorScreen = slot.AnchorScreen;
                    anchorScreenError = slot.AnchorScreenError;
                    finalScreenBounds = slot.FinalScreenBounds;
                    return true;
                }
            }
            finalScreenCenter = Vector2.zero;
            anchorScreen = Vector2.zero;
            anchorScreenError = float.PositiveInfinity;
            finalScreenBounds = Rect.zero;
            return false;
        }

        public void Clear()
        {
            ClearPreparedGeometry();
            while (_activeSlotCount > 0)
                ReleaseSlot(_activeSlotIndices[_activeSlotCount - 1]);
        }

        public void Dispose()
        {
            _initialized = false;
            Clear();
            _metadata = null;
            _atlas = null;
            if (gameObject == null) return;
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        public static Vector2 ProjectReferencePoint(
            BattlefieldViewportLayout viewport, Vector2 referencePoint)
        {
            return viewport.Offset + referencePoint * viewport.Scale;
        }

        private void Initialize(CombatFloatingTextAtlasMetadata metadata,
            Texture2D atlas)
        {
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
            _slotIndexByEventSequence.Clear();
            _activeSlotCount = 0;
            _freeSlotCount = PoolCapacity;
            _slotSyncVersion = 0;
            for (var index = 0; index < _slots.Length; index++)
            {
                _slots[index] = new Slot { ActiveIndex = -1 };
                _freeSlotIndices[index] = PoolCapacity - index - 1;
            }
            _initialized = true;
        }

        private bool AppendLabel(Slot slot, PresentationFeedback feedback,
            CombatFloatingTextStyleCatalog styles, BattlefieldProjection projection,
            BattlefieldViewportLayout viewport, Rect battlefieldSurface,
            Vector2 battlefieldOffset)
        {
            if (feedback.Text.Length > MaximumGlyphsPerLabel)
            {
                InvalidatePlacement("floating-text-glyph-capacity-exceeded");
                return false;
            }
            var token = styles.Resolve(feedback.Role);
            var motion = CombatFloatingTextStyleCatalog.Sample(
                token, feedback.LifetimeProgress);
            var mergePulse = Mathf.Min(.08f, Mathf.Max(0, feedback.Count - 1) * .012f);
            var motionScale = Mathf.Max(.01f, motion.Scale + mergePulse);
            var elementScale = token.FontSize * viewport.Scale
                / _metadata.FacePointSize * motionScale;
            float minX;
            float maxX;
            float minY;
            float maxY;
            if (!TryMeasure(feedback.Text, elementScale,
                    out minX, out maxX, out minY, out maxY))
            {
                InvalidatePlacement("floating-text-runtime-glyph-closure-violation");
                return false;
            }
            var width = Mathf.Max(1f, maxX - minX);
            var height = Mathf.Max(1f, maxY - minY);
            var anchorReference = projection.MapToScreen(feedback.Point) + battlefieldOffset;
            var anchorScreen = ProjectReferencePoint(viewport, anchorReference);
            var surfaceScreen = viewport.ProjectDesignRect(battlefieldSurface);
            Vector2 finalCenter;
            Rect finalBounds;
            float horizontalError;
            if (!TryPlaceAtAuthoredLane(feedback, anchorReference,
                    anchorScreen, motion.OffsetY, width, height,
                    viewport, surfaceScreen, out finalCenter, out finalBounds,
                    out horizontalError))
            {
                InvalidatePlacement("floating-text-final-placement-invalid");
                return false;
            }
            slot.FinalScreenCenter = finalCenter;
            slot.AnchorScreen = anchorScreen;
            slot.FinalScreenBounds = finalBounds;
            slot.AnchorScreenError = horizontalError;
            var fill = token.FillColor;
            fill.a *= motion.Opacity;
            if (_labelDrawRangeCount >= _labelDrawRanges.Length)
            {
                InvalidatePlacement("floating-text-label-range-capacity-exceeded");
                return false;
            }
            var rangeStart = _drawCommandCount;
            if (!AppendGlyphs(feedback.Text, elementScale,
                    minX, maxX, minY, maxY, finalCenter))
            {
                _drawCommandCount = rangeStart;
                InvalidatePlacement("floating-text-draw-command-capacity-exceeded");
                return false;
            }
            var rangeCount = _drawCommandCount - rangeStart;
            if (rangeCount <= 0)
            {
                InvalidatePlacement("floating-text-label-has-no-visible-glyphs");
                return false;
            }
            _labelDrawRanges[_labelDrawRangeCount++] = new LabelDrawRange
            {
                Start = rangeStart,
                Count = rangeCount,
                Color = fill,
            };
            return true;
        }

        private static bool TryPlaceAtAuthoredLane(PresentationFeedback feedback,
            Vector2 anchorReference, Vector2 anchorScreen, float travelY,
            float width, float height,
            BattlefieldViewportLayout viewport, Rect surfaceScreen,
            out Vector2 selectedCenter, out Rect selectedBounds,
            out float selectedHorizontalError)
        {
            selectedCenter = Vector2.zero;
            selectedBounds = Rect.zero;
            selectedHorizontalError = float.PositiveInfinity;
            var semanticOffset = CombatFloatingTextStyleCatalog.SemanticLaneOffset(
                feedback.Role);
            var laneOffset = CombatFloatingTextStyleCatalog.VisualLaneOffset(
                feedback.VisualLane) + semanticOffset;
            var contactReference = anchorReference + laneOffset
                + Vector2.up * travelY;
            var contactScreen = ProjectReferencePoint(viewport, contactReference);
            var desiredCenter = new Vector2(contactScreen.x,
                contactScreen.y - ContactGap * viewport.Scale - height * .5f);
            var finalCenter = ClampCenter(desiredCenter, width, height, surfaceScreen);
            var finalBounds = new Rect(finalCenter.x - width * .5f,
                finalCenter.y - height * .5f, width, height);
            var horizontalError = Mathf.Abs(finalCenter.x - anchorScreen.x);
            if (!Finite(finalCenter) || !Finite(anchorScreen)
                || !Finite(finalBounds)
                || !Contains(surfaceScreen, finalBounds, GeometryEpsilon))
                return false;
            selectedCenter = finalCenter;
            selectedBounds = finalBounds;
            selectedHorizontalError = horizontalError;
            return true;
        }

        private bool TryMeasure(string text, float elementScale,
            out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            minY = float.PositiveInfinity;
            maxY = float.NegativeInfinity;
            var cursor = 0f;
            for (var index = 0; index < text.Length; index++)
            {
                CombatFloatingTextGlyph glyph;
                if (!_metadata.TryGetGlyph(text[index], out glyph)) return false;
                _glyphScratch[index] = glyph;
                var scale = elementScale * glyph.Scale;
                if (glyph.Width > 0f && glyph.Height > 0f)
                {
                    minX = Mathf.Min(minX,
                        cursor + (glyph.HorizontalBearingX - glyph.Padding) * scale);
                    maxX = Mathf.Max(maxX,
                        cursor + (glyph.HorizontalBearingX + glyph.Width + glyph.Padding) * scale);
                    minY = Mathf.Min(minY,
                        (glyph.HorizontalBearingY - glyph.Height - glyph.Padding) * scale);
                    maxY = Mathf.Max(maxY,
                        (glyph.HorizontalBearingY + glyph.Padding) * scale);
                }
                cursor += glyph.HorizontalAdvance * scale;
            }
            if (float.IsInfinity(minX))
            {
                minX = 0f;
                maxX = Mathf.Max(1f, cursor);
                minY = _metadata.DescentLine * elementScale;
                maxY = _metadata.AscentLine * elementScale;
            }
            else
            {
                minX = Mathf.Min(minX, 0f);
                maxX = Mathf.Max(maxX, cursor);
            }
            return Finite(new Vector2(minX, minY))
                && Finite(new Vector2(maxX, maxY)) && maxX > minX && maxY > minY;
        }

        private bool AppendGlyphs(string text, float elementScale,
            float minX, float maxX, float minY, float maxY,
            Vector2 screenCenter)
        {
            var cursor = 0f;
            var localCenter = new Vector2((minX + maxX) * .5f, (minY + maxY) * .5f);
            var index = 0;
            while (index < text.Length)
            {
                CombatFloatingTextCompositeToken token;
                if (_metadata.TryGetLongestCompositeToken(text, index, out token))
                {
                    if (_drawCommandCount >= _drawCommands.Length) return false;
                    var left = cursor + token.MinX * elementScale;
                    var right = cursor + token.MaxX * elementScale;
                    var bottom = token.MinY * elementScale;
                    var top = token.MaxY * elementScale;
                    AppendDrawCommand(left, right, bottom, top,
                        token.AtlasRect, localCenter, screenCenter);
                    cursor += token.HorizontalAdvance * elementScale;
                    index += token.Text.Length;
                    continue;
                }
                var glyph = _glyphScratch[index];
                var scale = elementScale * glyph.Scale;
                if (glyph.Width > 0f && glyph.Height > 0f)
                {
                    if (_drawCommandCount >= _drawCommands.Length) return false;
                    var left = cursor + (glyph.HorizontalBearingX - glyph.Padding) * scale;
                    var right = cursor
                        + (glyph.HorizontalBearingX + glyph.Width + glyph.Padding) * scale;
                    var bottom = (glyph.HorizontalBearingY - glyph.Height - glyph.Padding) * scale;
                    var top = (glyph.HorizontalBearingY + glyph.Padding) * scale;
                    AppendDrawCommand(left, right, bottom, top,
                        glyph.AtlasRect, localCenter, screenCenter);
                }
                cursor += glyph.HorizontalAdvance * scale;
                index++;
            }
            return true;
        }

        private void AppendDrawCommand(float left, float right,
            float bottom, float top, RectInt atlasRect,
            Vector2 localCenter, Vector2 screenCenter)
        {
            _drawCommands[_drawCommandCount++] = new GlyphDrawCommand
            {
                ScreenRect = Rect.MinMaxRect(
                    screenCenter.x + left - localCenter.x,
                    screenCenter.y - (top - localCenter.y),
                    screenCenter.x + right - localCenter.x,
                    screenCenter.y - (bottom - localCenter.y)),
                UvRect = new Rect(
                    atlasRect.x * AtlasUvScale,
                    atlasRect.y * AtlasUvScale,
                    atlasRect.width * AtlasUvScale,
                    atlasRect.height * AtlasUvScale),
            };
        }

        private Slot ResolveSlot(long eventSequence)
        {
            int slotIndex;
            if (_slotIndexByEventSequence.TryGetValue(eventSequence, out slotIndex))
            {
                var existing = _slots[slotIndex];
                existing.LastSeenSync = _slotSyncVersion;
                return existing;
            }
            if (_freeSlotCount <= 0) return null;
            slotIndex = _freeSlotIndices[--_freeSlotCount];
            var slot = _slots[slotIndex];
            slot.Assigned = true;
            slot.EventSequence = eventSequence;
            slot.ActiveIndex = _activeSlotCount;
            slot.LastSeenSync = _slotSyncVersion;
            _activeSlotIndices[_activeSlotCount++] = slotIndex;
            _slotIndexByEventSequence.Add(eventSequence, slotIndex);
            return slot;
        }

        private void BeginSlotSync()
        {
            if (_slotSyncVersion == int.MaxValue)
            {
                for (var index = 0; index < _activeSlotCount; index++)
                    _slots[_activeSlotIndices[index]].LastSeenSync = 0;
                _slotSyncVersion = 1;
            }
            else _slotSyncVersion++;
        }

        private void MarkSlotSeen(long eventSequence)
        {
            int slotIndex;
            if (_slotIndexByEventSequence.TryGetValue(eventSequence, out slotIndex))
                _slots[slotIndex].LastSeenSync = _slotSyncVersion;
        }

        private void ReleaseUnseenSlots()
        {
            for (var index = 0; index < _activeSlotCount;)
            {
                var slot = _slots[_activeSlotIndices[index]];
                if (slot.LastSeenSync == _slotSyncVersion)
                {
                    index++;
                    continue;
                }
                ReleaseSlot(_activeSlotIndices[index]);
            }
        }

        private void ReleaseSlot(int slotIndex)
        {
            var slot = _slots[slotIndex];
            if (slot == null || !slot.Assigned) return;
            _slotIndexByEventSequence.Remove(slot.EventSequence);
            var activeIndex = slot.ActiveIndex;
            var lastActiveIndex = --_activeSlotCount;
            if (activeIndex != lastActiveIndex)
            {
                var movedSlotIndex = _activeSlotIndices[lastActiveIndex];
                _activeSlotIndices[activeIndex] = movedSlotIndex;
                _slots[movedSlotIndex].ActiveIndex = activeIndex;
            }
            _freeSlotIndices[_freeSlotCount++] = slotIndex;
            slot.Assigned = false;
            slot.EventSequence = 0;
            slot.ActiveIndex = -1;
            slot.LastSeenSync = 0;
            slot.FinalScreenCenter = Vector2.zero;
            slot.AnchorScreen = Vector2.zero;
            slot.FinalScreenBounds = Rect.zero;
            slot.AnchorScreenError = 0f;
        }

        private void ClearPreparedGeometry()
        {
            ActiveTextCount = 0;
            _drawCommandCount = 0;
            _labelDrawRangeCount = 0;
            _placementValid = true;
            _placementFailure = string.Empty;
        }

        private void InvalidatePlacement(string reason)
        {
            _placementValid = false;
            if (string.IsNullOrEmpty(_placementFailure)) _placementFailure = reason;
        }

        private AcceptanceScopeSample BeginAcceptanceCommandSample(
            IReadOnlyList<PresentationFeedback> records)
        {
            if (!_acceptanceProfileActive) return default;
            if (_acceptanceSubmissionPending)
            {
                FailAcceptanceSyncProfile("final-imgui-submission-missing-before-next-sync");
                return default;
            }
            if (records == null || records.Count != AcceptanceProfileActiveRecordCount)
            {
                FailAcceptanceSyncProfile("active-count-before-sync-not-12");
                return default;
            }
            return BeginAcceptanceScope("command-begin");
        }

        private void EndAcceptanceCommandSample(AcceptanceScopeSample sample)
        {
            if (!sample.Enabled || !_acceptanceProfileActive) return;
            if (ActiveTextCount != AcceptanceProfileActiveRecordCount)
            {
                FailAcceptanceSyncProfile("active-count-after-sync-not-12");
                return;
            }
            if (!_placementValid)
            {
                FailAcceptanceSyncProfile("placement-invalid:" + _placementFailure);
                return;
            }
            long timestamp;
            long allocatedBytes;
            if (!EndAcceptanceScope(sample, "command-end", out timestamp, out allocatedBytes))
                return;
            _acceptanceSubmissionPending = true;
            _acceptancePendingTimestamp = sample.Timestamp;
            _acceptancePendingDurationTicks = timestamp - sample.Timestamp;
            _acceptancePendingAllocatedBytes = allocatedBytes - sample.AllocatedBytes;
        }

        private AcceptanceScopeSample BeginAcceptanceSubmissionSample()
        {
            if (!_acceptanceProfileActive || !_acceptanceSubmissionPending) return default;
            return BeginAcceptanceScope("submission-begin");
        }

        private void EndAcceptanceSubmissionSample(AcceptanceScopeSample sample, bool submitted)
        {
            if (!sample.Enabled || !_acceptanceProfileActive) return;
            if (!submitted)
            {
                FailAcceptanceSyncProfile("final-imgui-atlas-submission-rejected");
                return;
            }
            long timestamp;
            long allocatedBytes;
            if (!EndAcceptanceScope(sample, "submission-end", out timestamp, out allocatedBytes))
                return;
            _acceptanceSubmissionPending = false;
            RecordAcceptanceSample(_acceptancePendingTimestamp, timestamp,
                _acceptancePendingDurationTicks + timestamp - sample.Timestamp,
                _acceptancePendingAllocatedBytes + allocatedBytes - sample.AllocatedBytes);
        }

        private AcceptanceScopeSample BeginAcceptanceScope(string phase)
        {
            long allocatedBytes;
            string failure;
            if (!TryReadAcceptanceAllocatedBytes(out allocatedBytes, out failure))
            {
                FailAcceptanceSyncProfile("measurement-api-failed-at-" + phase + ":" + failure);
                return default;
            }
            return new AcceptanceScopeSample(Stopwatch.GetTimestamp(), allocatedBytes);
        }

        private bool EndAcceptanceScope(AcceptanceScopeSample sample, string phase,
            out long timestamp, out long allocatedBytes)
        {
            timestamp = Stopwatch.GetTimestamp();
            allocatedBytes = 0;
            string failure;
            if (!TryReadAcceptanceAllocatedBytes(out allocatedBytes, out failure))
            {
                FailAcceptanceSyncProfile("measurement-api-failed-at-" + phase + ":" + failure);
                return false;
            }
            if (timestamp < sample.Timestamp)
            {
                FailAcceptanceSyncProfile("stopwatch-counter-not-monotonic");
                return false;
            }
            if (allocatedBytes < sample.AllocatedBytes)
            {
                FailAcceptanceSyncProfile("cumulative-allocation-counter-decreased");
                return false;
            }
            return true;
        }

        private void RecordAcceptanceSample(long firstTimestamp, long lastTimestamp,
            long durationTicks, long allocatedBytes)
        {
            if (!_acceptanceProfileActive) return;
            if (firstTimestamp <= 0 || lastTimestamp < firstTimestamp
                || durationTicks < 0 || allocatedBytes < 0)
            {
                FailAcceptanceSyncProfile("combined-profile-sample-invalid");
                return;
            }
            if (_acceptanceProfileWarmupCount < AcceptanceWarmupSampleCount)
            {
                _acceptanceProfileWarmupCount++;
                return;
            }
            if (_acceptanceProfileSampleCount == 0)
                _acceptanceProfileFirstTimestamp = firstTimestamp;
            var milliseconds = (float)(durationTicks * 1000d / Stopwatch.Frequency);
            if (float.IsNaN(milliseconds) || float.IsInfinity(milliseconds)
                || milliseconds < 0f)
            {
                FailAcceptanceSyncProfile("profile-duration-not-finite");
                return;
            }
            _acceptanceProfileSamplesMilliseconds[_acceptanceProfileSampleCount] = milliseconds;
            if (_acceptanceProfileAllocatedBytes > long.MaxValue - allocatedBytes)
            {
                FailAcceptanceSyncProfile("profile-allocation-total-overflow");
                return;
            }
            _acceptanceProfileAllocatedBytes += allocatedBytes;
            _acceptanceProfileLastTimestamp = lastTimestamp;
            _acceptanceProfileSampleCount++;
            if (_acceptanceProfileSampleCount < AcceptanceActiveSampleCount) return;
            var sorted = (float[])_acceptanceProfileSamplesMilliseconds.Clone();
            Array.Sort(sorted);
            var index = Mathf.Clamp(Mathf.CeilToInt(.95f * sorted.Length) - 1,
                0, sorted.Length - 1);
            _acceptanceProfileP95Milliseconds = sorted[index];
            var elapsed = AcceptanceProfileElapsedSeconds;
            var rate = AcceptanceProfileAllocatedBytesPerSecond;
            if (elapsed <= 0f || float.IsNaN(elapsed) || float.IsInfinity(elapsed)
                || _acceptanceProfileP95Milliseconds < 0f
                || float.IsNaN(_acceptanceProfileP95Milliseconds)
                || float.IsInfinity(_acceptanceProfileP95Milliseconds)
                || rate < 0f || float.IsNaN(rate) || float.IsInfinity(rate))
            {
                FailAcceptanceSyncProfile("reported-profile-value-invalid");
                return;
            }
            _acceptanceProfileActive = false;
            _acceptanceProfileCompleted = true;
        }

        private void FailAcceptanceSyncProfile(string reason)
        {
            _acceptanceProfileActive = false;
            _acceptanceProfileSupported = false;
            _acceptanceProfileCompleted = false;
            _acceptanceSubmissionPending = false;
            _acceptanceProfileFailure = reason ?? "unknown";
        }

        private bool TryReadAcceptanceAllocatedBytes(
            out long cumulativeAllocatedBytes, out string failure)
        {
            cumulativeAllocatedBytes = 0;
            failure = string.Empty;
            long rawAllocatedBytes;
            try { rawAllocatedBytes = GC.GetAllocatedBytesForCurrentThread(); }
            catch (Exception exception)
            {
                failure = exception.GetType().Name;
                return false;
            }
            return TryAdvanceAcceptanceAllocationCounter(rawAllocatedBytes,
                ref _acceptanceAllocationCounterInitialized,
                ref _acceptanceAllocationCounterPreviousRaw,
                ref _acceptanceAllocationCounterCumulative,
                out cumulativeAllocatedBytes, out failure);
        }

        private static bool TryAdvanceAcceptanceAllocationCounter(
            long rawAllocatedBytes, ref bool initialized,
            ref long previousRawAllocatedBytes, ref long cumulativeAllocatedBytes,
            out long currentAllocatedBytes, out string failure)
        {
            currentAllocatedBytes = cumulativeAllocatedBytes;
            failure = string.Empty;
            if (rawAllocatedBytes < 0)
            {
                failure = "managed-allocation-counter-negative";
                return false;
            }
            if (!initialized)
            {
                initialized = true;
                previousRawAllocatedBytes = rawAllocatedBytes;
                return true;
            }
            var delta = rawAllocatedBytes >= previousRawAllocatedBytes
                ? rawAllocatedBytes - previousRawAllocatedBytes : rawAllocatedBytes;
            if (cumulativeAllocatedBytes > long.MaxValue - delta)
            {
                failure = "managed-allocation-counter-overflow";
                return false;
            }
            cumulativeAllocatedBytes += delta;
            previousRawAllocatedBytes = rawAllocatedBytes;
            currentAllocatedBytes = cumulativeAllocatedBytes;
            return true;
        }

        private static bool TryLoadAndValidateAssets(
            out CombatFloatingTextAtlasMetadata metadata,
            out Texture2D atlas, out string reason)
        {
            metadata = Resources.Load<CombatFloatingTextAtlasMetadata>(
                MetadataResourcePath);
            atlas = Resources.Load<Texture2D>(AtlasResourcePath);
            if (metadata == null || atlas == null)
            {
                reason = "combat-floating-atlas-production-assets-missing";
                return false;
            }
            if (atlas.width != AtlasSize || atlas.height != AtlasSize
                || atlas.format != TextureFormat.RGBA32 || atlas.mipmapCount != 1
                || atlas.filterMode != FilterMode.Bilinear
                || atlas.wrapMode != TextureWrapMode.Clamp)
            {
                reason = "combat-floating-atlas-format-or-size-invalid";
                return false;
            }
            var expected = CombatFloatingTextStyleCatalog.RuntimeGlyphInventory;
            if (metadata.FacePointSize <= 0f
                || metadata.GlyphInventory != expected
                || metadata.GlyphCount != expected.Length)
            {
                reason = "combat-floating-atlas-glyph-closure-size-invalid";
                return false;
            }
            for (var index = 0; index < expected.Length; index++)
            {
                CombatFloatingTextGlyph glyph;
                if (!metadata.TryGetGlyph(expected[index], out glyph))
                {
                    reason = "combat-floating-atlas-missing-glyph:U+"
                        + ((int)expected[index]).ToString("X4");
                    return false;
                }
                if (glyph.AtlasRect.xMin < 0 || glyph.AtlasRect.yMin < 0
                    || glyph.AtlasRect.xMax > AtlasSize
                    || glyph.AtlasRect.yMax > AtlasSize
                    || glyph.Scale <= 0f || glyph.HorizontalAdvance < 0f)
                {
                    reason = "combat-floating-atlas-glyph-metrics-invalid:U+"
                        + ((int)expected[index]).ToString("X4");
                    return false;
                }
            }
            var compositeRegion = new RectInt(192, 0, AtlasSize - 192, AtlasSize);
            if (metadata.CompositeRegion != compositeRegion
                || !Mathf.Approximately(metadata.CompositeBasePointSize, 24f)
                || metadata.CompositeTokenCount != 124)
            {
                reason = "combat-floating-composite-token-contract-invalid";
                return false;
            }
            for (var digit = 0; digit <= 9; digit++)
            {
                var single = "-" + digit.ToString(CultureInfo.InvariantCulture);
                if (!ValidCompositeToken(metadata, single, compositeRegion)
                    || !ValidCompositeToken(metadata, "+"
                        + digit.ToString(CultureInfo.InvariantCulture), compositeRegion))
                {
                    reason = "combat-floating-composite-single-token-invalid:"
                        + digit;
                    return false;
                }
            }
            for (var value = 0; value <= 99; value++)
            {
                var tokenText = "-" + value.ToString(
                    "00", CultureInfo.InvariantCulture);
                if (ValidCompositeToken(metadata, tokenText, compositeRegion)) continue;
                reason = "combat-floating-composite-double-token-invalid:"
                    + tokenText;
                return false;
            }
            var fixedTokens = new[] { "冻结", "击败", "击败×", " 阳光" };
            for (var index = 0; index < fixedTokens.Length; index++)
            {
                if (ValidCompositeToken(
                        metadata, fixedTokens[index], compositeRegion)) continue;
                reason = "combat-floating-composite-fixed-token-invalid:"
                    + fixedTokens[index];
                return false;
            }
            reason = "ok";
            return true;
        }

        private static bool ValidCompositeToken(
            CombatFloatingTextAtlasMetadata metadata, string text,
            RectInt compositeRegion)
        {
            CombatFloatingTextCompositeToken token;
            CombatFloatingTextCompositeToken resolved;
            return metadata.TryGetCompositeToken(text, out token)
                && metadata.TryGetLongestCompositeToken(text, 0, out resolved)
                && resolved.Text == text
                && token.AtlasRect == resolved.AtlasRect
                && token.BaseScale > 0f
                && token.MaxX > token.MinX && token.MaxY > token.MinY
                && token.HorizontalAdvance > 0f
                && token.AtlasRect.xMin >= compositeRegion.xMin
                && token.AtlasRect.yMin >= compositeRegion.yMin
                && token.AtlasRect.xMax <= compositeRegion.xMax
                && token.AtlasRect.yMax <= compositeRegion.yMax;
        }

        private static Vector2 ClampCenter(Vector2 desired, float width,
            float height, Rect surface)
        {
            var minX = surface.xMin + width * .5f;
            var maxX = surface.xMax - width * .5f;
            var minY = surface.yMin + height * .5f;
            var maxY = surface.yMax - height * .5f;
            return new Vector2(
                minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : surface.center.x,
                minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : surface.center.y);
        }

        private static bool Contains(Rect outer, Rect inner, float epsilon)
        {
            return inner.xMin >= outer.xMin - epsilon
                && inner.yMin >= outer.yMin - epsilon
                && inner.xMax <= outer.xMax + epsilon
                && inner.yMax <= outer.yMax + epsilon;
        }

        private static bool Finite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }

        private static bool Finite(Rect value)
        {
            return Finite(value.position) && Finite(value.size);
        }
    }
}
