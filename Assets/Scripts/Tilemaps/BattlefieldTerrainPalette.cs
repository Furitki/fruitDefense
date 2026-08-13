using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Tilemaps
{
    [Serializable]
    public sealed class BattlefieldTerrainSurfaceBinding
    {
        [SerializeField] private string surfaceId = string.Empty;
        [SerializeField] private Texture2D baseTexture;
        [SerializeField] private DualGridTileSet tileSet;

        public string SurfaceId { get { return surfaceId ?? string.Empty; } }
        public Texture2D BaseTexture { get { return baseTexture; } }

        // Compatibility view for pre-contour palette construction only. Runtime rendering and
        // validation use LandformBindings and never fall back to this field.
        public DualGridTileSet TileSet { get { return tileSet; } }
        public DualGridTileSet LandformTileSet { get { return tileSet; } }

        public BattlefieldTerrainSurfaceBinding(string surfaceId, Texture2D baseTexture)
            : this(surfaceId, baseTexture, null)
        {
        }

        public BattlefieldTerrainSurfaceBinding(string surfaceId, DualGridTileSet tileSet)
            : this(surfaceId, null, tileSet)
        {
        }

        public BattlefieldTerrainSurfaceBinding(string surfaceId, Texture2D baseTexture,
            DualGridTileSet legacyLandformTileSet)
        {
            this.surfaceId = surfaceId ?? string.Empty;
            this.baseTexture = baseTexture;
            tileSet = legacyLandformTileSet;
        }
    }

    [Serializable]
    public sealed class BattlefieldTerrainLandformBinding
    {
        [SerializeField] private string surfaceId = string.Empty;
        [SerializeField] private string contourStyleId = string.Empty;
        [SerializeField] private DualGridTileSet tileSet;

        public string SurfaceId { get { return surfaceId ?? string.Empty; } }
        public string ContourStyleId { get { return contourStyleId ?? string.Empty; } }
        public DualGridTileSet TileSet { get { return tileSet; } }

        public BattlefieldTerrainLandformBinding(string surfaceId, string contourStyleId,
            DualGridTileSet tileSet)
        {
            this.surfaceId = surfaceId ?? string.Empty;
            this.contourStyleId = contourStyleId ?? string.Empty;
            this.tileSet = tileSet;
        }
    }

    [Serializable]
    public sealed class BattlefieldTerrainEdgeBinding
    {
        [SerializeField] private string landformSurfaceId = string.Empty;
        [SerializeField] private string baseSurfaceId = string.Empty;
        [SerializeField] private string contourStyleId = string.Empty;
        [SerializeField] private string edgeStyleId = string.Empty;
        [SerializeField] private DualGridTileSet tileSet;

        public string LandformSurfaceId { get { return landformSurfaceId ?? string.Empty; } }
        public string BaseSurfaceId { get { return baseSurfaceId ?? string.Empty; } }
        public string ContourStyleId { get { return contourStyleId ?? string.Empty; } }
        public string EdgeStyleId { get { return edgeStyleId ?? string.Empty; } }
        public DualGridTileSet TileSet { get { return tileSet; } }

        public BattlefieldTerrainEdgeBinding(string landformSurfaceId, string baseSurfaceId,
            string edgeStyleId, DualGridTileSet tileSet)
            : this(landformSurfaceId, baseSurfaceId,
                BattlefieldLayerIds.ContourStyles.Organic, edgeStyleId, tileSet)
        {
        }

        public BattlefieldTerrainEdgeBinding(string landformSurfaceId, string baseSurfaceId,
            string contourStyleId, string edgeStyleId, DualGridTileSet tileSet)
        {
            this.landformSurfaceId = landformSurfaceId ?? string.Empty;
            this.baseSurfaceId = baseSurfaceId ?? string.Empty;
            this.contourStyleId = contourStyleId ?? string.Empty;
            this.edgeStyleId = edgeStyleId ?? string.Empty;
            this.tileSet = tileSet;
        }
    }

    [CreateAssetMenu(menuName = "Fruit Defense/Battlefield Terrain Palette")]
    public sealed class BattlefieldTerrainPalette : ScriptableObject
    {
        private static readonly HashSet<string> KnownSurfaces = new HashSet<string>(
            new[]
            {
                BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.Surfaces.StoneRoad,
                BattlefieldLayerIds.Surfaces.Water,
            }, StringComparer.Ordinal);
        private static readonly HashSet<string> KnownContours = new HashSet<string>(
            new[]
            {
                BattlefieldLayerIds.ContourStyles.Square,
                BattlefieldLayerIds.ContourStyles.Organic,
            }, StringComparer.Ordinal);

        [SerializeField] private string paletteId = string.Empty;
        [SerializeField] private Texture2D soilBaseTexture;
        [SerializeField] private BattlefieldTerrainSurfaceBinding[] surfaceBindings =
            Array.Empty<BattlefieldTerrainSurfaceBinding>();
        [SerializeField] private BattlefieldTerrainLandformBinding[] landformBindings =
            Array.Empty<BattlefieldTerrainLandformBinding>();
        [SerializeField] private BattlefieldTerrainEdgeBinding[] edgeBindings =
            Array.Empty<BattlefieldTerrainEdgeBinding>();

        public string PaletteId { get { return paletteId ?? string.Empty; } }
        public Texture2D SoilBaseTexture
        {
            get
            {
                Texture2D texture;
                return TryGetBaseTexture(BattlefieldLayerIds.Surfaces.Soil, out texture)
                    ? texture : soilBaseTexture;
            }
        }
        public IReadOnlyList<BattlefieldTerrainSurfaceBinding> SurfaceBindings
        {
            get
            {
                return (surfaceBindings ?? Array.Empty<BattlefieldTerrainSurfaceBinding>())
                    .OrderBy(value => value == null ? string.Empty : value.SurfaceId,
                        StringComparer.Ordinal)
                    .ToArray();
            }
        }
        public IReadOnlyList<BattlefieldTerrainSurfaceBinding> BaseBindings { get { return SurfaceBindings; } }
        public IReadOnlyList<BattlefieldTerrainSurfaceBinding> MaterialBindings { get { return SurfaceBindings; } }
        public IReadOnlyList<BattlefieldTerrainLandformBinding> LandformBindings
        {
            get
            {
                return (landformBindings ?? Array.Empty<BattlefieldTerrainLandformBinding>())
                    .OrderBy(value => value == null ? string.Empty : value.SurfaceId,
                        StringComparer.Ordinal)
                    .ThenBy(value => value == null ? string.Empty : value.ContourStyleId,
                        StringComparer.Ordinal)
                    .ToArray();
            }
        }
        public IReadOnlyList<BattlefieldTerrainEdgeBinding> EdgeBindings
        {
            get
            {
                return (edgeBindings ?? Array.Empty<BattlefieldTerrainEdgeBinding>())
                    .OrderBy(value => value == null ? string.Empty : value.LandformSurfaceId,
                        StringComparer.Ordinal)
                    .ThenBy(value => value == null ? string.Empty : value.BaseSurfaceId,
                        StringComparer.Ordinal)
                    .ThenBy(value => value == null ? string.Empty : value.ContourStyleId,
                        StringComparer.Ordinal)
                    .ThenBy(value => value == null ? string.Empty : value.EdgeStyleId,
                        StringComparer.Ordinal)
                    .ToArray();
            }
        }

        public DualGridTileSet ReferenceTileSet
        {
            get
            {
                var binding = LandformBindings.FirstOrDefault(value => value != null
                    && value.TileSet != null);
                return binding == null ? null : binding.TileSet;
            }
        }

        public Texture2D ReferenceBaseTexture
        {
            get
            {
                var binding = SurfaceBindings.FirstOrDefault(value => value != null
                    && value.BaseTexture != null);
                return binding == null ? null : binding.BaseTexture;
            }
        }

        public bool TryGetTileSet(string surfaceId, out DualGridTileSet tileSet)
        {
            return TryGetLandformTileSet(surfaceId,
                BattlefieldLayerIds.ContourStyles.Organic, out tileSet);
        }

        public bool TryGetLandformTileSet(string surfaceId, string contourStyleId,
            out DualGridTileSet tileSet)
        {
            foreach (var binding in LandformBindings)
            {
                if (binding == null
                    || !string.Equals(binding.SurfaceId, surfaceId, StringComparison.Ordinal)
                    || !string.Equals(binding.ContourStyleId, contourStyleId,
                        StringComparison.Ordinal)) continue;
                tileSet = binding.TileSet;
                return tileSet != null;
            }
            tileSet = null;
            return false;
        }

        public IEnumerable<string> ContourStylesFor(string surfaceId)
        {
            return LandformBindings.Where(binding => binding != null && binding.TileSet != null
                    && string.Equals(binding.SurfaceId, surfaceId, StringComparison.Ordinal))
                .Select(binding => binding.ContourStyleId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal);
        }

        public bool TryGetBaseTexture(string surfaceId, out Texture2D texture)
        {
            foreach (var binding in SurfaceBindings)
            {
                if (binding == null || !string.Equals(binding.SurfaceId, surfaceId,
                        StringComparison.Ordinal)) continue;
                texture = binding.BaseTexture;
                return texture != null;
            }
            texture = null;
            return false;
        }

        public bool TryGetEdgeTileSet(string landformSurfaceId, string baseSurfaceId,
            string edgeStyleId, out DualGridTileSet tileSet)
        {
            return TryGetEdgeTileSet(landformSurfaceId, baseSurfaceId,
                BattlefieldLayerIds.ContourStyles.Organic, edgeStyleId, out tileSet);
        }

        public bool TryGetEdgeTileSet(string landformSurfaceId, string baseSurfaceId,
            string contourStyleId, string edgeStyleId, out DualGridTileSet tileSet)
        {
            bool ignored;
            return TryGetEdgeTileSet(landformSurfaceId, baseSurfaceId, contourStyleId,
                edgeStyleId, out tileSet, out ignored);
        }

        public bool TryGetEdgeTileSet(string landformSurfaceId, string baseSurfaceId,
            string contourStyleId, string edgeStyleId, out DualGridTileSet tileSet,
            out bool complementMask)
        {
            var exact = FindExactEdgeBinding(landformSurfaceId, baseSurfaceId,
                contourStyleId, edgeStyleId);
            if (exact != null && exact.TileSet != null)
            {
                tileSet = exact.TileSet;
                complementMask = false;
                return true;
            }

            var reverse = FindExactEdgeBinding(baseSurfaceId, landformSurfaceId,
                contourStyleId, edgeStyleId);
            tileSet = reverse == null ? null : reverse.TileSet;
            Sprite reverseCenter;
            if (tileSet == null
                || !tileSet.TryGetSprite(DualGridMask.Empty, out reverseCenter))
            {
                tileSet = null;
                complementMask = false;
                return false;
            }
            complementMask = true;
            return true;
        }

        public bool HasExactEdgeBinding(string landformSurfaceId, string baseSurfaceId,
            string contourStyleId, string edgeStyleId)
        {
            var binding = FindExactEdgeBinding(landformSurfaceId, baseSurfaceId,
                contourStyleId, edgeStyleId);
            return binding != null && binding.TileSet != null;
        }

        public bool Validate(out string reason)
        {
            if (string.IsNullOrWhiteSpace(PaletteId))
            {
                reason = "Battlefield terrain palette identity is required.";
                return false;
            }

            var surfaces = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in SurfaceBindings)
            {
                if (binding == null || !KnownSurfaces.Contains(binding.SurfaceId))
                {
                    reason = "Battlefield terrain palette contains an unknown base surface binding.";
                    return false;
                }
                if (!surfaces.Add(binding.SurfaceId))
                {
                    reason = "Battlefield terrain palette duplicates base surface '"
                        + binding.SurfaceId + "'.";
                    return false;
                }
                if (binding.BaseTexture == null || binding.BaseTexture.width <= 0
                    || binding.BaseTexture.height <= 0)
                {
                    reason = "Battlefield terrain base surface '" + binding.SurfaceId
                        + "' requires its own renderable base texture.";
                    return false;
                }
            }
            if (!surfaces.Contains(BattlefieldLayerIds.Surfaces.Soil)
                || !surfaces.Contains(BattlefieldLayerIds.Surfaces.Grass)
                || !surfaces.Contains(BattlefieldLayerIds.Surfaces.StoneRoad))
            {
                reason = "Battlefield terrain palette must bind soil, grass and stone-road bases.";
                return false;
            }

            var landformKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in LandformBindings)
            {
                if (binding == null || !surfaces.Contains(binding.SurfaceId)
                    || !KnownContours.Contains(binding.ContourStyleId))
                {
                    reason = "Battlefield terrain palette contains an invalid surface-plus-contour landform binding.";
                    return false;
                }
                var key = LandformKey(binding.SurfaceId, binding.ContourStyleId);
                if (!landformKeys.Add(key))
                {
                    reason = "Battlefield terrain palette duplicates landform '" + key + "'.";
                    return false;
                }
                if (binding.TileSet == null)
                {
                    reason = "Battlefield terrain landform '" + key + "' has no tile set.";
                    return false;
                }
                if (!binding.TileSet.Validate(out reason)) return false;
            }
            if (landformKeys.Count == 0)
            {
                reason = "Battlefield terrain palette requires at least one contour-specific landform binding.";
                return false;
            }

            var edgeKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in EdgeBindings)
            {
                if (binding == null || !surfaces.Contains(binding.LandformSurfaceId)
                    || !surfaces.Contains(binding.BaseSurfaceId)
                    || !KnownContours.Contains(binding.ContourStyleId)
                    || string.IsNullOrWhiteSpace(binding.EdgeStyleId))
                {
                    reason = "Battlefield terrain palette contains an invalid directed contour edge binding.";
                    return false;
                }
                var landformKey = LandformKey(binding.LandformSurfaceId,
                    binding.ContourStyleId);
                if (!landformKeys.Contains(landformKey))
                {
                    reason = "Battlefield terrain edge '" + EdgeKey(binding)
                        + "' has no exact foreground landform binding.";
                    return false;
                }
                var key = EdgeKey(binding);
                if (!edgeKeys.Add(key))
                {
                    reason = "Battlefield terrain palette duplicates directed contour edge '"
                        + key + "'.";
                    return false;
                }
                if (binding.TileSet == null)
                {
                    reason = "Battlefield terrain edge '" + key + "' has no tile set.";
                    return false;
                }
                if (!binding.TileSet.Validate(out reason)) return false;
                DualGridTileSet landform;
                var socketReason = "missing exact landform binding";
                if (!TryGetLandformTileSet(binding.LandformSurfaceId,
                        binding.ContourStyleId, out landform)
                    || !landform.HasCompatibleNormalizedSockets(binding.TileSet,
                        out socketReason))
                {
                    reason = "Battlefield terrain edge '" + key
                        + "' is incompatible with its exact contour landform's normalized sockets: "
                        + (socketReason ?? "missing exact landform binding") + ".";
                    return false;
                }
            }
            reason = "ok";
            return true;
        }

#if UNITY_EDITOR
        public void Configure(string id, Texture2D baseTexture,
            IEnumerable<BattlefieldTerrainSurfaceBinding> bindings)
        {
            var legacy = (bindings ?? Enumerable.Empty<BattlefieldTerrainSurfaceBinding>()).ToArray();
            var bases = legacy.Select(binding => binding == null ? null
                : new BattlefieldTerrainSurfaceBinding(binding.SurfaceId,
                    binding.BaseTexture)).ToList();
            if (bases.All(binding => binding == null || !string.Equals(binding.SurfaceId,
                    BattlefieldLayerIds.Surfaces.Soil, StringComparison.Ordinal)))
                bases.Insert(0, new BattlefieldTerrainSurfaceBinding(
                    BattlefieldLayerIds.Surfaces.Soil, baseTexture));
            ConfigureLayered(id, bases,
                legacy.Where(binding => binding != null && binding.LandformTileSet != null)
                    .Select(binding => new BattlefieldTerrainLandformBinding(binding.SurfaceId,
                        BattlefieldLayerIds.ContourStyles.Organic, binding.LandformTileSet)),
                Array.Empty<BattlefieldTerrainEdgeBinding>());
        }

        public void ConfigureLayered(string id,
            IEnumerable<BattlefieldTerrainSurfaceBinding> materials,
            IEnumerable<BattlefieldTerrainEdgeBinding> edges)
        {
            var legacy = (materials ?? Enumerable.Empty<BattlefieldTerrainSurfaceBinding>()).ToArray();
            ConfigureLayered(id, legacy,
                legacy.Where(binding => binding != null && binding.LandformTileSet != null)
                    .Select(binding => new BattlefieldTerrainLandformBinding(binding.SurfaceId,
                        BattlefieldLayerIds.ContourStyles.Organic, binding.LandformTileSet)),
                edges);
        }

        public void ConfigureLayered(string id,
            IEnumerable<BattlefieldTerrainSurfaceBinding> bases,
            IEnumerable<BattlefieldTerrainLandformBinding> landforms,
            IEnumerable<BattlefieldTerrainEdgeBinding> edges)
        {
            paletteId = id ?? string.Empty;
            surfaceBindings = (bases ?? Enumerable.Empty<BattlefieldTerrainSurfaceBinding>()).ToArray();
            landformBindings = (landforms ?? Enumerable.Empty<BattlefieldTerrainLandformBinding>()).ToArray();
            edgeBindings = (edges ?? Enumerable.Empty<BattlefieldTerrainEdgeBinding>()).ToArray();
            Texture2D soil;
            soilBaseTexture = TryGetBaseTexture(BattlefieldLayerIds.Surfaces.Soil, out soil) ? soil : null;
        }
#endif

        private static string LandformKey(string surfaceId, string contourStyleId)
        {
            return (surfaceId ?? string.Empty) + "|" + (contourStyleId ?? string.Empty);
        }

        private static string EdgeKey(BattlefieldTerrainEdgeBinding binding)
        {
            return (binding.LandformSurfaceId ?? string.Empty) + "|"
                + (binding.BaseSurfaceId ?? string.Empty) + "|"
                + (binding.ContourStyleId ?? string.Empty) + "|"
                + (binding.EdgeStyleId ?? string.Empty);
        }

        private BattlefieldTerrainEdgeBinding FindExactEdgeBinding(string landformSurfaceId,
            string baseSurfaceId, string contourStyleId, string edgeStyleId)
        {
            return EdgeBindings.FirstOrDefault(binding => binding != null
                && string.Equals(binding.LandformSurfaceId, landformSurfaceId,
                    StringComparison.Ordinal)
                && string.Equals(binding.BaseSurfaceId, baseSurfaceId, StringComparison.Ordinal)
                && string.Equals(binding.ContourStyleId, contourStyleId,
                    StringComparison.Ordinal)
                && string.Equals(binding.EdgeStyleId, edgeStyleId, StringComparison.Ordinal));
        }
    }
}
