#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEngine;

namespace FruitDefense.Development.GmStress
{
    public static class GmStressBattleIds
    {
        public const string LevelId = "development.gm-stress";
        public const string MapId = "development.gm-stress.8x7";
        public const string PotGroupId = "gm.bottom-two-rows";
        public const int GridWidth = 8;
        public const int GridHeight = 7;
        public const int RouteRowCount = 5;
        public const int PlantRowStart = 5;
        public const int ActiveAndPendingCapacity = 500;
        public const string TerrainPaletteId =
            BundledLevelCatalogIds.TerrainPalettes.OrchardDefault;

        public static readonly IReadOnlyList<int> BatchCounts =
            Array.AsReadOnly(new[] { 1, 10, 50 });

        public static readonly IReadOnlyList<string> EnemyDefinitionIds =
            Array.AsReadOnly(new[]
            {
                BattleContentIds.Enemies.Normal,
                BattleContentIds.Enemies.Runner,
                BattleContentIds.Enemies.Armored,
                BattleContentIds.Enemies.Boss,
            });

        public static readonly IReadOnlyList<string> PlantDefinitionIds =
            Array.AsReadOnly(new[]
            {
                BattleContentIds.Plants.Pea,
                BattleContentIds.Plants.Watermelon,
                BattleContentIds.Plants.Banana,
                BattleContentIds.Plants.Durian,
                BattleContentIds.Plants.Sunflower,
            });

        public static string RouteId(int laneIndex)
        {
            if (laneIndex < 0 || laneIndex >= GridWidth)
                throw new ArgumentOutOfRangeException(nameof(laneIndex));
            return "route.gm." + laneIndex.ToString("00", CultureInfo.InvariantCulture);
        }
    }

    public static class GmStressBattleLaunchRequest
    {
        public const string QueryKey = "gmStress";
        public const string QueryValue = "1";
        private const string EditorOneShotKey = "fruit-defense.gm-stress.play-once";

#if UNITY_EDITOR
        public static void SetEditorOneShot()
        {
            PlayerPrefs.SetInt(EditorOneShotKey, 1);
            PlayerPrefs.Save();
        }

        public static bool TryConsumeEditorOneShot()
        {
            if (PlayerPrefs.GetInt(EditorOneShotKey, 0) != 1) return false;
            PlayerPrefs.DeleteKey(EditorOneShotKey);
            PlayerPrefs.Save();
            return true;
        }

        public static void ClearEditorOneShot()
        {
            PlayerPrefs.DeleteKey(EditorOneShotKey);
            PlayerPrefs.Save();
        }
#else
        public static bool TryConsumeEditorOneShot() { return false; }
#endif
    }

    public static class GmStressBattleFactory
    {
        public static BattlefieldLayeredMapSource CreateMapSource()
        {
            var cellCount = GmStressBattleIds.GridWidth * GmStressBattleIds.GridHeight;
            var visuals = new BattlefieldVisualCellSource[cellCount];
            var gameplay = new BattlefieldGameplayCellSource[cellCount];
            for (var y = 0; y < GmStressBattleIds.GridHeight; y++)
            for (var x = 0; x < GmStressBattleIds.GridWidth; x++)
            {
                var index = y * GmStressBattleIds.GridWidth + x;
                var routeCell = y < GmStressBattleIds.RouteRowCount;
                visuals[index] = routeCell
                    ? new BattlefieldVisualCellSource(BattlefieldLayerIds.Surfaces.Soil)
                    : new BattlefieldVisualCellSource(
                        BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.ContourStyles.Square,
                        BattlefieldLayerIds.EdgeStyles.Refined);
                gameplay[index] = new BattlefieldGameplayCellSource(new[]
                {
                    routeCell
                        ? BattlefieldLayerIds.Capabilities.EnemyTraversable
                        : BattlefieldLayerIds.Capabilities.Plantable,
                });
            }

            var routes = new BattlefieldRouteDefinition[GmStressBattleIds.GridWidth];
            var markers = new List<BattlefieldMarkerDefinition>(
                GmStressBattleIds.GridWidth * 2
                + GmStressBattleIds.GridWidth * 2);
            for (var lane = 0; lane < GmStressBattleIds.GridWidth; lane++)
            {
                var routeId = GmStressBattleIds.RouteId(lane);
                var cells = Enumerable.Range(0, GmStressBattleIds.RouteRowCount)
                    .Select(row => new Vector2Int(lane, row)).ToArray();
                routes[lane] = new BattlefieldRouteDefinition(routeId, cells);
                markers.Add(new BattlefieldMarkerDefinition(
                    "spawn.gm." + lane.ToString("00", CultureInfo.InvariantCulture),
                    BattlefieldMarkerKind.EnemySpawn, cells[0], routeId,
                    facing: BattlefieldDirection.South));
                markers.Add(new BattlefieldMarkerDefinition(
                    "goal.gm." + lane.ToString("00", CultureInfo.InvariantCulture),
                    BattlefieldMarkerKind.RouteGoal, cells[cells.Length - 1], routeId,
                    facing: BattlefieldDirection.South));
            }

            for (var y = GmStressBattleIds.PlantRowStart;
                 y < GmStressBattleIds.GridHeight; y++)
            for (var x = 0; x < GmStressBattleIds.GridWidth; x++)
            {
                markers.Add(new BattlefieldMarkerDefinition(
                    "pot.gm." + x.ToString("00", CultureInfo.InvariantCulture)
                    + "." + y.ToString("00", CultureInfo.InvariantCulture),
                    BattlefieldMarkerKind.InitialPotCandidate,
                    new Vector2Int(x, y), groupId: GmStressBattleIds.PotGroupId));
            }

            var groups = new[]
            {
                new BattlefieldMarkerGroupDefinition(GmStressBattleIds.PotGroupId,
                    BattlefieldMarkerKind.InitialPotCandidate,
                    GmStressBattleIds.GridWidth * 2),
            };
            return new BattlefieldLayeredMapSource(
                BattlefieldLayerIds.SchemaVersion,
                GmStressBattleIds.MapId,
                GmStressBattleIds.GridWidth,
                GmStressBattleIds.GridHeight,
                BattlefieldMapDefinition.LegacyReferenceMapUnitsPerCell,
                string.Empty,
                visuals,
                gameplay,
                routes,
                groups,
                markers,
                BattlefieldExecutionProfile.GmMultiRoute);
        }

        public static BattlefieldMapDefinition CreateMap()
        {
            return new BattlefieldMapDefinition(CreateMapSource());
        }

        public static bool ValidateTerrainPalette(BattlefieldMapDefinition map,
            BattlefieldTerrainPalette palette, out string reason)
        {
            if (palette == null)
            {
                reason = "GM terrain palette is required.";
                return false;
            }
            if (!string.Equals(palette.PaletteId, GmStressBattleIds.TerrainPaletteId,
                    StringComparison.Ordinal))
            {
                reason = "GM terrain palette must be '"
                    + GmStressBattleIds.TerrainPaletteId + "'.";
                return false;
            }
            if (!BattlefieldDualGridTerrain.Validate(map, palette, out reason)) return false;
            if (!palette.TryGetEdgeTileSet(
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined,
                    out var edgeTileSet, out var complementMask)
                || edgeTileSet == null || complementMask)
            {
                reason = "GM terrain requires the exact registered square refined "
                    + "grass-on-soil brush binding.";
                return false;
            }
            reason = "ok";
            return true;
        }

        public static CompiledBattleContentCatalog CreateContent()
        {
            if (BundledGameContentLoader.TryLoad(out var manifest,
                    out var content, out var validation))
                return content;
            var issues = validation == null
                ? "validation-unavailable"
                : string.Join("\n", validation.Issues.Select(issue => issue.ToString()));
            throw new InvalidOperationException("GM battle content is invalid:\n" + issues);
        }

        public static GmStressBattleController Create(int seed)
        {
            var simulation = new GameSimulation(CreateContent(), seed, CreateMap(),
                BattleSimulationMode.GmStress);
            return new GmStressBattleController(simulation);
        }
    }

    public sealed class GmStressBattleController : IDisposable
    {
        private readonly Queue<string>[] _laneQueues;
        private bool _disposed;

        public GmStressBattleController(GameSimulation simulation)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            if (simulation.Mode != BattleSimulationMode.GmStress)
                throw new ArgumentException("GM controller requires a GM stress simulation.",
                    nameof(simulation));
            if (simulation.Map.RouteIds.Count != GmStressBattleIds.GridWidth)
                throw new ArgumentException("GM controller requires exactly eight routes.",
                    nameof(simulation));
            _laneQueues = Enumerable.Range(0, GmStressBattleIds.GridWidth)
                .Select(_ => new Queue<string>()).ToArray();
            simulation.FixedStepStarting += HandleFixedStepStarting;
        }

        public GameSimulation Simulation { get; }
        public IReadOnlyList<string> LaneIds { get { return Simulation.Map.RouteIds; } }
        public int ActiveCount { get { return Simulation.State.Zombies.Count; } }
        public int PendingCount { get { return _laneQueues.Sum(queue => queue.Count); } }
        public int EscapedCount { get { return Simulation.EscapedEnemyCount; } }
        public int Capacity { get { return GmStressBattleIds.ActiveAndPendingCapacity; } }
        public int RemainingCapacity { get { return Mathf.Max(0, Capacity - ActiveCount - PendingCount); } }
        public bool IsPaused { get { return Simulation.State.Paused; } }
        public int Speed { get { return Simulation.State.Speed; } }

        public int PendingInLane(int laneIndex)
        {
            RequireLaneIndex(laneIndex);
            return _laneQueues[laneIndex].Count;
        }

        public string PeekPendingEnemy(int laneIndex)
        {
            RequireLaneIndex(laneIndex);
            return _laneQueues[laneIndex].Count == 0
                ? string.Empty : _laneQueues[laneIndex].Peek();
        }

        public bool EnqueueLane(int laneIndex, string enemyDefinitionId, int batchCount,
            out int acceptedCount, out string reason)
        {
            RequireLaneIndex(laneIndex);
            acceptedCount = 0;
            if (!ValidateEnemyAndBatch(enemyDefinitionId, batchCount, out reason)) return false;
            acceptedCount = Mathf.Min(batchCount, RemainingCapacity);
            for (var index = 0; index < acceptedCount; index++)
                _laneQueues[laneIndex].Enqueue(enemyDefinitionId);
            if (acceptedCount == 0)
            {
                reason = "容量已满：活动与待生成合计上限为 " + Capacity;
                return false;
            }
            reason = "第 " + (laneIndex + 1) + " 路已加入 " + acceptedCount
                + "/" + batchCount + " 只";
            return true;
        }

        public bool EnqueueAll(string enemyDefinitionId, int batchPerLane,
            out int acceptedCount, out string reason)
        {
            acceptedCount = 0;
            if (!ValidateEnemyAndBatch(enemyDefinitionId, batchPerLane, out reason)) return false;
            var total = checked(batchPerLane * GmStressBattleIds.GridWidth);
            var remaining = RemainingCapacity;
            for (var lane = 0; lane < GmStressBattleIds.GridWidth && remaining > 0; lane++)
            {
                var laneAccepted = Mathf.Min(batchPerLane, remaining);
                for (var index = 0; index < laneAccepted; index++)
                    _laneQueues[lane].Enqueue(enemyDefinitionId);
                acceptedCount += laneAccepted;
                remaining -= laneAccepted;
            }
            reason = acceptedCount == 0
                ? "容量已满：活动与待生成合计上限为 " + Capacity
                : "全路按路线顺序加入 " + acceptedCount + "/" + total + " 只";
            return acceptedCount > 0;
        }

        public int AdvanceFrame(float unscaledDelta)
        {
            ThrowIfDisposed();
            return Simulation.AdvanceFrame(unscaledDelta);
        }

        public int RunFixedSteps(int count)
        {
            ThrowIfDisposed();
            var completed = 0;
            for (var index = 0; index < Mathf.Max(0, count); index++)
            {
                if (!Simulation.Step()) break;
                completed++;
            }
            return completed;
        }

        public void TogglePause()
        {
            ThrowIfDisposed();
            Simulation.TogglePause();
        }

        public void SetSpeed(int speed)
        {
            ThrowIfDisposed();
            Simulation.SetSpeed(speed);
        }

        public bool PlaceOrReplacePlant(Vector2Int cell, string plantDefinitionId,
            out string reason)
        {
            ThrowIfDisposed();
            if (!GmStressBattleIds.PlantDefinitionIds.Contains(plantDefinitionId,
                    StringComparer.Ordinal)
                || !Simulation.Content.Plants.ContainsKey(plantDefinitionId))
            {
                reason = "未知 GM 植物";
                return false;
            }
            var pot = Simulation.State.Pots.FirstOrDefault(value => value.Active
                && value.Cell == cell);
            if (pot == null)
            {
                reason = "这里只允许底部两排花盆";
                return false;
            }
            var previous = Simulation.PlantAtPot(pot.Id);
            Simulation.PlaceOrReplaceGmPlant(plantDefinitionId, pot.Id);
            reason = previous == null ? "植物已免费放置" : "植物已免费替换";
            return true;
        }

        public string Checksum
        {
            get
            {
                var canonical = new StringBuilder(Simulation.OutcomeStateChecksum());
                canonical.Append('|').Append(PendingCount);
                for (var lane = 0; lane < _laneQueues.Length; lane++)
                {
                    canonical.Append('|').Append(LaneIds[lane]);
                    foreach (var enemyId in _laneQueues[lane])
                        canonical.Append('>').Append(enemyId);
                }
                unchecked
                {
                    const ulong offset = 14695981039346656037ul;
                    const ulong prime = 1099511628211ul;
                    var hash = offset;
                    var text = canonical.ToString();
                    for (var index = 0; index < text.Length; index++)
                    {
                        hash ^= text[index];
                        hash *= prime;
                    }
                    return hash.ToString("x16", CultureInfo.InvariantCulture);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Simulation.FixedStepStarting -= HandleFixedStepStarting;
            foreach (var queue in _laneQueues) queue.Clear();
        }

        private void HandleFixedStepStarting(GameSimulation simulation)
        {
            for (var lane = 0; lane < _laneQueues.Length; lane++)
            {
                if (ActiveCount >= Capacity) return;
                var queue = _laneQueues[lane];
                if (queue.Count == 0) continue;
                simulation.SpawnEnemy(queue.Dequeue(), LaneIds[lane]);
            }
        }

        private static bool ValidateEnemyAndBatch(string enemyDefinitionId, int batchCount,
            out string reason)
        {
            if (!GmStressBattleIds.EnemyDefinitionIds.Contains(enemyDefinitionId,
                    StringComparer.Ordinal))
            {
                reason = "未知 GM 怪物";
                return false;
            }
            if (!GmStressBattleIds.BatchCounts.Contains(batchCount))
            {
                reason = "批量只能选择 1、10 或 50";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        private static void RequireLaneIndex(int laneIndex)
        {
            if (laneIndex < 0 || laneIndex >= GmStressBattleIds.GridWidth)
                throw new ArgumentOutOfRangeException(nameof(laneIndex));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GmStressBattleController));
        }
    }

    public enum GmStressPlantDragReleaseKind
    {
        Selected,
        Deploy,
        Cancelled,
    }

    public readonly struct GmStressPlantDragRelease
    {
        public GmStressPlantDragReleaseKind Kind { get; }
        public int PlantIndex { get; }
        public int PotIndex { get; }

        public GmStressPlantDragRelease(GmStressPlantDragReleaseKind kind,
            int plantIndex, int potIndex = -1)
        {
            Kind = kind;
            PlantIndex = plantIndex;
            PotIndex = potIndex;
        }
    }

    public sealed class GmStressPlantDragInteractor
    {
        private int _plantIndex = -1;
        private Vector2 _start;
        private Vector2 _current;

        public bool HasSource { get { return _plantIndex >= 0; } }
        public bool IsActive { get; private set; }
        public int PlantIndex { get { return _plantIndex; } }
        public Vector2 Current { get { return _current; } }

        public void Begin(int plantIndex, Vector2 point)
        {
            if (plantIndex < 0
                || plantIndex >= GmStressBattleIds.PlantDefinitionIds.Count)
                throw new ArgumentOutOfRangeException(nameof(plantIndex));
            _plantIndex = plantIndex;
            _start = point;
            _current = point;
            IsActive = false;
        }

        public void Move(Vector2 point)
        {
            if (!HasSource) return;
            _current = point;
            if (!IsActive && DragGeometry.CrossedActivationThreshold(_start, _current))
                IsActive = true;
        }

        public int CurrentPotIndex(IReadOnlyList<Rect> potRects)
        {
            if (!IsActive || potRects == null) return -1;
            return DragGeometry.BestOverlapIndex(
                DragGeometry.PreviewRect(_current), potRects);
        }

        public GmStressPlantDragRelease Release(Vector2 point,
            IReadOnlyList<Rect> potRects)
        {
            if (!HasSource) throw new InvalidOperationException(
                "A GM plant drag source is required before release.");
            Move(point);
            var plantIndex = _plantIndex;
            var active = IsActive;
            var potIndex = CurrentPotIndex(potRects);
            Cancel();
            return !active
                ? new GmStressPlantDragRelease(
                    GmStressPlantDragReleaseKind.Selected, plantIndex)
                : potIndex >= 0
                    ? new GmStressPlantDragRelease(
                        GmStressPlantDragReleaseKind.Deploy, plantIndex, potIndex)
                    : new GmStressPlantDragRelease(
                        GmStressPlantDragReleaseKind.Cancelled, plantIndex);
        }

        public void Cancel()
        {
            _plantIndex = -1;
            _start = default;
            _current = default;
            IsActive = false;
        }
    }

    public sealed class GmStressBattleLayout
    {
        public const float DesignWidth = 402f;
        public const float DesignHeight = 874f;

        public GmStressBattleLayout(BattlefieldMapDefinition map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            Screen = new Rect(0f, 0f, DesignWidth, DesignHeight);
            Header = new Rect(12f, 10f, 378f, 90f);
            HeaderTitle = new Rect(24f, 12f, 150f, 34f);
            ActiveMetric = new Rect(20f, 48f, 94f, 44f);
            PendingMetric = new Rect(116f, 48f, 94f, 44f);
            EscapedMetric = new Rect(212f, 48f, 106f, 44f);
            PauseAction = new Rect(326f, 10f, 44f, 44f);
            SpeedAction = new Rect(326f, 56f, 44f, 44f);

            BoardPanel = new Rect(12f, 108f, 378f, 410f);
            BattlefieldSurface = new Rect(18f, 114f, 366f, 402f);
            Battlefield = new BattlefieldProjection(map, BattlefieldSurface);

            EnemyPanel = new Rect(12f, 526f, 378f, 80f);
            BatchPanel = new Rect(12f, 614f, 378f, 70f);
            PlantPanel = new Rect(12f, 692f, 378f, 96f);
            Status = new Rect(12f, 796f, 378f, 66f);
        }

        public Rect Screen { get; }
        public Rect Header { get; }
        public Rect HeaderTitle { get; }
        public Rect ActiveMetric { get; }
        public Rect PendingMetric { get; }
        public Rect EscapedMetric { get; }
        public Rect PauseAction { get; }
        public Rect SpeedAction { get; }
        public Rect BoardPanel { get; }
        public Rect BattlefieldSurface { get; }
        public BattlefieldProjection Battlefield { get; }
        public Rect EnemyPanel { get; }
        public Rect BatchPanel { get; }
        public Rect PlantPanel { get; }
        public Rect Status { get; }

        public Rect SpawnPad(int lane)
        {
            return Battlefield.TileRect(new Vector2Int(lane, 0));
        }

        public Rect EnemyChoice(int index)
        {
            return new Rect(20f + index * 92f, 550f, 86f, 48f);
        }

        public Rect BatchChoice(int index)
        {
            return new Rect(20f + index * 74f, 628f, 68f, 48f);
        }

        public Rect AllLanesAction { get { return new Rect(246f, 628f, 136f, 48f); } }

        public Rect PlantChoice(int index)
        {
            return new Rect(18f + index * 74f, 718f, 70f, 62f);
        }

        public Rect ClampDragPreview(Rect preview)
        {
            preview.center = new Vector2(
                Mathf.Clamp(preview.center.x, 24f, DesignWidth - 24f),
                Mathf.Clamp(preview.center.y, 24f, DesignHeight - 24f));
            return preview;
        }
    }
}
#endif
