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
        [SerializeField] private DualGridTileSet tileSet;

        public string SurfaceId { get { return surfaceId ?? string.Empty; } }
        public DualGridTileSet TileSet { get { return tileSet; } }

        public BattlefieldTerrainSurfaceBinding(string surfaceId, DualGridTileSet tileSet)
        {
            this.surfaceId = surfaceId ?? string.Empty;
            this.tileSet = tileSet;
        }
    }

    [CreateAssetMenu(menuName = "Fruit Defense/Battlefield Terrain Palette")]
    public sealed class BattlefieldTerrainPalette : ScriptableObject
    {
        [SerializeField] private string paletteId = string.Empty;
        [SerializeField] private Texture2D soilBaseTexture;
        [SerializeField] private BattlefieldTerrainSurfaceBinding[] surfaceBindings =
            Array.Empty<BattlefieldTerrainSurfaceBinding>();

        public string PaletteId { get { return paletteId ?? string.Empty; } }
        public Texture2D SoilBaseTexture { get { return soilBaseTexture; } }
        public IReadOnlyList<BattlefieldTerrainSurfaceBinding> SurfaceBindings
        {
            get { return surfaceBindings ?? Array.Empty<BattlefieldTerrainSurfaceBinding>(); }
        }

        public DualGridTileSet ReferenceTileSet
        {
            get
            {
                var binding = SurfaceBindings.FirstOrDefault(value => value != null && value.TileSet != null);
                return binding == null ? null : binding.TileSet;
            }
        }

        public bool TryGetTileSet(string surfaceId, out DualGridTileSet tileSet)
        {
            foreach (var binding in SurfaceBindings)
            {
                if (binding == null || !string.Equals(binding.SurfaceId, surfaceId, StringComparison.Ordinal))
                    continue;
                tileSet = binding.TileSet;
                return tileSet != null;
            }
            tileSet = null;
            return false;
        }

        public bool Validate(out string reason)
        {
            if (string.IsNullOrWhiteSpace(PaletteId))
            {
                reason = "Battlefield terrain palette identity is required.";
                return false;
            }
            if (SoilBaseTexture == null || SoilBaseTexture.width <= 0 || SoilBaseTexture.height <= 0)
            {
                reason = "Battlefield terrain palette soil base texture is required.";
                return false;
            }
            var known = new HashSet<string>(StringComparer.Ordinal)
            {
                BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.Surfaces.StoneRoad,
            };
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in SurfaceBindings)
            {
                if (binding == null || !known.Contains(binding.SurfaceId))
                {
                    reason = "Battlefield terrain palette contains an unknown surface binding.";
                    return false;
                }
                if (!seen.Add(binding.SurfaceId))
                {
                    reason = "Battlefield terrain palette duplicates surface '" + binding.SurfaceId + "'.";
                    return false;
                }
                if (binding.TileSet == null)
                {
                    reason = "Battlefield terrain palette binding requires a TileSet.";
                    return false;
                }
                if (!binding.TileSet.Validate(out reason)) return false;
            }
            if (!seen.SetEquals(known))
            {
                reason = "Battlefield terrain palette must bind grass and stone-road surfaces.";
                return false;
            }
            reason = "ok";
            return true;
        }

#if UNITY_EDITOR
        public void Configure(string id, Texture2D baseTexture,
            IEnumerable<BattlefieldTerrainSurfaceBinding> bindings)
        {
            paletteId = id ?? string.Empty;
            soilBaseTexture = baseTexture;
            surfaceBindings = (bindings ?? Enumerable.Empty<BattlefieldTerrainSurfaceBinding>()).ToArray();
        }
#endif
    }
}
