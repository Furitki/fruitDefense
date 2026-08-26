using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Content
{
    [Serializable]
    public sealed class BattlefieldMapPublicationManifestEntry
    {
        [SerializeField] private int order;
        [SerializeField] private string levelId = string.Empty;
        [SerializeField] private string templateLevelId = string.Empty;
        [SerializeField] private BattlefieldMapAuthoringAsset map;

        public int Order { get { return order; } }
        public string LevelId { get { return levelId ?? string.Empty; } }
        public string TemplateLevelId { get { return templateLevelId ?? string.Empty; } }
        public BattlefieldMapAuthoringAsset Map { get { return map; } }

        public BattlefieldMapPublicationManifestEntry()
        {
        }

        public BattlefieldMapPublicationManifestEntry(int order, string levelId,
            string templateLevelId, BattlefieldMapAuthoringAsset map)
        {
            this.order = order;
            this.levelId = levelId ?? string.Empty;
            this.templateLevelId = templateLevelId ?? string.Empty;
            this.map = map;
        }
    }

    [CreateAssetMenu(menuName = "Fruit Defense/关卡地图/发布清单",
        fileName = "BattlefieldMapPublicationManifest")]
    public sealed class BattlefieldMapPublicationManifest : ScriptableObject
    {
        [SerializeField] private List<BattlefieldMapPublicationManifestEntry> entries =
            new List<BattlefieldMapPublicationManifestEntry>();

        public IReadOnlyList<BattlefieldMapPublicationManifestEntry> Entries
        {
            get { return entries == null
                ? Array.Empty<BattlefieldMapPublicationManifestEntry>() : entries; }
        }

        public void Configure(IEnumerable<BattlefieldMapPublicationManifestEntry> values)
        {
            entries = (values ?? Enumerable.Empty<BattlefieldMapPublicationManifestEntry>())
                .ToList();
        }

        public BattlefieldMapPublicationManifestEntry FindByMap(
            BattlefieldMapAuthoringAsset map)
        {
            return Entries.FirstOrDefault(entry => entry != null && entry.Map == map);
        }
    }

    [Serializable]
    public sealed class PublishedBattlefieldMapRecord
    {
        [SerializeField] private int schemaVersion;
        [SerializeField] private string mapId = string.Empty;
        [SerializeField] private int gridWidth;
        [SerializeField] private int gridHeight;
        [SerializeField] private float mapUnitsPerCell;
        [SerializeField] private List<BattlefieldVisualCellAuthoringRecord> visualCells =
            new List<BattlefieldVisualCellAuthoringRecord>();
        [SerializeField] private List<BattlefieldGameplayCellAuthoringRecord> gameplayCells =
            new List<BattlefieldGameplayCellAuthoringRecord>();
        [SerializeField] private BattlefieldRouteAuthoringRecord primaryRoute;
        [SerializeField] private List<BattlefieldMarkerGroupAuthoringRecord> markerGroups =
            new List<BattlefieldMarkerGroupAuthoringRecord>();
        [SerializeField] private List<BattlefieldMarkerAuthoringRecord> markers =
            new List<BattlefieldMarkerAuthoringRecord>();

        public string MapId { get { return mapId ?? string.Empty; } }
        public int SchemaVersion { get { return schemaVersion; } }
        public int GridWidth { get { return gridWidth; } }
        public int GridHeight { get { return gridHeight; } }
        public float MapUnitsPerCell { get { return mapUnitsPerCell; } }

        public PublishedBattlefieldMapRecord()
        {
        }

        public PublishedBattlefieldMapRecord(BattlefieldMapAuthoringAsset asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            schemaVersion = asset.SchemaVersion;
            mapId = asset.MapId;
            gridWidth = asset.GridWidth;
            gridHeight = asset.GridHeight;
            mapUnitsPerCell = asset.MapUnitsPerCell;
            visualCells = asset.VisualCells.Select(cell => cell == null ? null : cell.Copy()).ToList();
            gameplayCells = asset.GameplayCells.Select(cell => cell == null ? null : cell.Copy()).ToList();
            primaryRoute = asset.PrimaryRoute == null ? null : asset.PrimaryRoute.Copy();
            markerGroups = asset.MarkerGroups.Select(group => group == null ? null : group.Copy()).ToList();
            markers = asset.Markers.Select(marker => marker == null ? null : marker.Copy()).ToList();
        }

        public BattlefieldLayeredMapSource ToSource()
        {
            return new BattlefieldLayeredMapSource(schemaVersion, MapId, gridWidth,
                gridHeight, mapUnitsPerCell, BattlefieldLayerIds.PrimaryRoute,
                (visualCells ?? new List<BattlefieldVisualCellAuthoringRecord>())
                    .Select(cell => cell == null ? null : cell.ToSource()),
                (gameplayCells ?? new List<BattlefieldGameplayCellAuthoringRecord>())
                    .Select(cell => cell == null ? null : cell.ToSource()),
                primaryRoute == null
                    ? Array.Empty<BattlefieldRouteDefinition>()
                    : new[] { primaryRoute.ToSource() },
                (markerGroups ?? new List<BattlefieldMarkerGroupAuthoringRecord>())
                    .Select(group => group == null ? null : group.ToSource()),
                (markers ?? new List<BattlefieldMarkerAuthoringRecord>())
                    .Select(marker => marker == null ? null : marker.ToSource()),
                BattlefieldExecutionProfile.StandardRelease);
        }
    }

    [Serializable]
    public sealed class PublishedBattlefieldMapEntry
    {
        [SerializeField] private int order;
        [SerializeField] private string levelId = string.Empty;
        [SerializeField] private string templateLevelId = string.Empty;
        [SerializeField] private PublishedBattlefieldMapRecord map;

        public int Order { get { return order; } }
        public string LevelId { get { return levelId ?? string.Empty; } }
        public string TemplateLevelId { get { return templateLevelId ?? string.Empty; } }
        public PublishedBattlefieldMapRecord Map { get { return map; } }

        public PublishedBattlefieldMapEntry()
        {
        }

        public PublishedBattlefieldMapEntry(int order, string levelId,
            string templateLevelId, PublishedBattlefieldMapRecord map)
        {
            this.order = order;
            this.levelId = levelId ?? string.Empty;
            this.templateLevelId = templateLevelId ?? string.Empty;
            this.map = map;
        }
    }

    public sealed class PublishedBattlefieldMapCatalog : ScriptableObject
    {
        public const int CurrentSchemaVersion = 2;
        public const string ResourcePath = "Generated/PublishedBattlefieldMapCatalog";

        [SerializeField, HideInInspector] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField, HideInInspector] private string sourceCatalogId = string.Empty;
        [SerializeField, HideInInspector] private string contentVersion = string.Empty;
        [SerializeField, HideInInspector] private List<PublishedBattlefieldMapEntry> entries =
            new List<PublishedBattlefieldMapEntry>();

        public int SchemaVersion { get { return schemaVersion; } }
        public string SourceCatalogId { get { return sourceCatalogId ?? string.Empty; } }
        public string ContentVersion { get { return contentVersion ?? string.Empty; } }
        public IReadOnlyList<PublishedBattlefieldMapEntry> Entries
        {
            get { return entries == null
                ? Array.Empty<PublishedBattlefieldMapEntry>() : entries; }
        }

        public void Configure(string catalogId, string version,
            IEnumerable<PublishedBattlefieldMapEntry> values)
        {
            schemaVersion = CurrentSchemaVersion;
            sourceCatalogId = catalogId ?? string.Empty;
            contentVersion = version ?? string.Empty;
            entries = (values ?? Enumerable.Empty<PublishedBattlefieldMapEntry>())
                .OrderBy(entry => entry == null ? int.MaxValue : entry.Order)
                .ThenBy(entry => entry == null ? string.Empty : entry.LevelId,
                    StringComparer.Ordinal).ToList();
        }

        public static PublishedBattlefieldMapCatalog LoadGenerated()
        {
            return Resources.Load<PublishedBattlefieldMapCatalog>(ResourcePath);
        }
    }

    public static class PublishedBattlefieldPlaytestRequest
    {
        private const string LevelKey = "fruit-defense.map-editor.playtest-level";

        public static void Set(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
                throw new ArgumentException("Published level identity is required.", nameof(levelId));
            PlayerPrefs.SetString(LevelKey, levelId);
            PlayerPrefs.Save();
        }

        public static bool TryConsume(out string levelId)
        {
#if UNITY_EDITOR
            levelId = PlayerPrefs.GetString(LevelKey, string.Empty);
            if (string.IsNullOrWhiteSpace(levelId)) return false;
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.Save();
            return true;
#else
            levelId = string.Empty;
            return false;
#endif
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.Save();
        }
    }
}
