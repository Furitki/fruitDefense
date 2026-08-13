using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Tilemaps
{
    [CreateAssetMenu(menuName = "Fruit Defense/Terrain Brush Definition",
        fileName = "TerrainBrushDefinition")]
    public sealed class TerrainBrushDefinition : ScriptableObject
    {
        [SerializeField] private string brushId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string sourceProfileId = string.Empty;
        [SerializeField] private string landformDisplayName = string.Empty;
        [SerializeField] private string baseDisplayName = string.Empty;
        [SerializeField] private string landformSurfaceId = string.Empty;
        [SerializeField] private string baseSurfaceId = string.Empty;
        [SerializeField] private string contourStyleId = string.Empty;
        [SerializeField] private string edgeStyleId = string.Empty;
        [SerializeField] private int foregroundMask = 15;
        [SerializeField] private int backgroundMask;
        [SerializeField] private int runtimeTileSize = 32;
        [SerializeField] private DualGridTileSet compositeTileSet;
        [SerializeField] private DualGridTileSet reverseLandformTileSet;
        [SerializeField] private TileBase foregroundBaseTile;
        [SerializeField] private TileBase backgroundBaseTile;
        [SerializeField] private Texture2D foregroundTexture;
        [SerializeField] private Texture2D backgroundTexture;
        [SerializeField] private TextAsset sourceManifest;
        [SerializeField] private bool publishEndpointsToPalette = true;

        public string BrushId { get { return brushId ?? string.Empty; } }
        public string DisplayName { get { return displayName ?? string.Empty; } }
        public string SourceProfileId { get { return sourceProfileId ?? string.Empty; } }
        public string LandformDisplayName { get { return landformDisplayName ?? string.Empty; } }
        public string BaseDisplayName { get { return baseDisplayName ?? string.Empty; } }
        public string LandformSurfaceId { get { return landformSurfaceId ?? string.Empty; } }
        public string BaseSurfaceId { get { return baseSurfaceId ?? string.Empty; } }
        public string ContourStyleId { get { return contourStyleId ?? string.Empty; } }
        public string EdgeStyleId { get { return edgeStyleId ?? string.Empty; } }
        public int ForegroundMask { get { return foregroundMask; } }
        public int BackgroundMask { get { return backgroundMask; } }
        public int RuntimeTileSize { get { return runtimeTileSize; } }
        public DualGridTileSet CompositeTileSet { get { return compositeTileSet; } }
        public DualGridTileSet ReverseLandformTileSet { get { return reverseLandformTileSet; } }
        public TileBase ForegroundBaseTile { get { return foregroundBaseTile; } }
        public TileBase BackgroundBaseTile { get { return backgroundBaseTile; } }
        public Texture2D ForegroundTexture { get { return foregroundTexture; } }
        public Texture2D BackgroundTexture { get { return backgroundTexture; } }
        public TextAsset SourceManifest { get { return sourceManifest; } }
        public bool PublishEndpointsToPalette { get { return publishEndpointsToPalette; } }

        public void Configure(string id, string label, string profileId,
            string foregroundLabel, string backgroundLabel,
            string foregroundSurface, string backgroundSurface,
            string contour, string edge, int foregroundEndpointMask,
            int backgroundEndpointMask, int runtimeSize, DualGridTileSet tileSet,
            DualGridTileSet reverseLandform, TileBase foregroundBase,
            TileBase backgroundBase,
            Texture2D foregroundEndpoint, Texture2D backgroundEndpoint,
            TextAsset manifest, bool publishEndpoints = true)
        {
            brushId = Trim(id);
            displayName = Trim(label);
            sourceProfileId = Trim(profileId);
            landformDisplayName = Trim(foregroundLabel);
            baseDisplayName = Trim(backgroundLabel);
            landformSurfaceId = Trim(foregroundSurface);
            baseSurfaceId = Trim(backgroundSurface);
            contourStyleId = Trim(contour);
            edgeStyleId = Trim(edge);
            foregroundMask = foregroundEndpointMask;
            backgroundMask = backgroundEndpointMask;
            runtimeTileSize = runtimeSize;
            compositeTileSet = tileSet;
            reverseLandformTileSet = reverseLandform;
            foregroundBaseTile = foregroundBase;
            backgroundBaseTile = backgroundBase;
            foregroundTexture = foregroundEndpoint;
            backgroundTexture = backgroundEndpoint;
            sourceManifest = manifest;
            publishEndpointsToPalette = publishEndpoints;
        }

        public bool Validate(out string reason)
        {
            if (string.IsNullOrEmpty(BrushId) || string.IsNullOrEmpty(DisplayName)
                || string.IsNullOrEmpty(SourceProfileId)
                || string.IsNullOrEmpty(LandformDisplayName)
                || string.IsNullOrEmpty(BaseDisplayName)
                || string.IsNullOrEmpty(LandformSurfaceId)
                || string.IsNullOrEmpty(BaseSurfaceId)
                || string.IsNullOrEmpty(ContourStyleId)
                || string.IsNullOrEmpty(EdgeStyleId))
            {
                reason = "Terrain brush identity, labels and semantic registration are required.";
                return false;
            }
            if (string.Equals(LandformSurfaceId, BaseSurfaceId, StringComparison.Ordinal))
            {
                reason = "Terrain brush foreground and background surfaces must differ.";
                return false;
            }
            if (ForegroundMask < 0 || ForegroundMask > 15
                || BackgroundMask < 0 || BackgroundMask > 15
                || ForegroundMask == BackgroundMask)
            {
                reason = "Terrain brush endpoint masks must be distinct values in 0..15.";
                return false;
            }
            if (RuntimeTileSize < 8 || RuntimeTileSize > 256
                || (RuntimeTileSize & (RuntimeTileSize - 1)) != 0)
            {
                reason = "Terrain brush runtime tile size must be a power of two from 8 to 256.";
                return false;
            }
            if (compositeTileSet == null)
            {
                reason = "Terrain brush composite TileSet is required.";
                return false;
            }
            if (!compositeTileSet.Validate(out reason)) return false;
            if (reverseLandformTileSet == null
                || !reverseLandformTileSet.Validate(out reason))
            {
                reason = "Terrain brush complemented TileSet is required and must be valid: "
                    + reason;
                return false;
            }
            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var sourceMask = DualGridMaskUtility.Complement((DualGridMask)mask);
                if (reverseLandformTileSet.GetTile((DualGridMask)mask)
                    == compositeTileSet.GetTile(sourceMask)) continue;
                reason = "Terrain brush complemented TileSet mask " + mask
                    + " must reference primary mask " + (int)sourceMask + ".";
                return false;
            }
            if (foregroundBaseTile == null || backgroundBaseTile == null)
            {
                reason = "Terrain brush endpoint base tiles are required.";
                return false;
            }
            if (foregroundTexture == null || backgroundTexture == null)
            {
                reason = "Terrain brush endpoint textures are required.";
                return false;
            }
            if (publishEndpointsToPalette
                && (foregroundTexture.width != RuntimeTileSize
                    || foregroundTexture.height != RuntimeTileSize
                    || backgroundTexture.width != RuntimeTileSize
                    || backgroundTexture.height != RuntimeTileSize))
            {
                reason = "Terrain brush endpoint textures do not match runtime tile size.";
                return false;
            }
            if (!BaseTileUsesTexture(foregroundBaseTile, foregroundTexture)
                || !BaseTileUsesTexture(backgroundBaseTile, backgroundTexture))
            {
                reason = "Terrain brush endpoint base tiles must render their registered textures.";
                return false;
            }
            if (sourceManifest == null)
            {
                reason = "Terrain brush source manifest is required.";
                return false;
            }
            reason = "ok";
            return true;
        }

        private static string Trim(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }

        private static bool BaseTileUsesTexture(TileBase value, Texture2D texture)
        {
            var tile = value as Tile;
            return tile != null && tile.sprite != null && tile.sprite.texture == texture;
        }
    }
}
