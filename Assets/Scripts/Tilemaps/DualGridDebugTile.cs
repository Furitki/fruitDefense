using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Tilemaps
{
    public sealed class DualGridDebugTile : TileBase
    {
        private const int TextureSize = 64;
        private static readonly Dictionary<int, Sprite> SpriteCache = new Dictionary<int, Sprite>();

        [SerializeField, Range(0, 15)] private int mask;
        [SerializeField] private Color fillColor = new Color(.27f, .78f, .48f, 1f);
        [SerializeField] private Color edgeColor = new Color(.09f, .28f, .16f, 1f);

        public DualGridMask Mask { get { return (DualGridMask)mask; } }

        public void Configure(DualGridMask configuredMask, Color fill, Color edge)
        {
            mask = Mathf.Clamp((int)configuredMask, 0, 15);
            fillColor = fill;
            edgeColor = edge;
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = GetOrCreateSprite((DualGridMask)mask, fillColor, edgeColor);
            tileData.color = Color.white;
            tileData.transform = Matrix4x4.identity;
            tileData.flags = TileFlags.LockAll;
            tileData.colliderType = Tile.ColliderType.None;
        }

        private static Sprite GetOrCreateSprite(DualGridMask mask, Color fill, Color edge)
        {
            var fill32 = (Color32)fill;
            var edge32 = (Color32)edge;
            unchecked
            {
                var key = (int)mask;
                key = key * 397 ^ fill32.GetHashCode();
                key = key * 397 ^ edge32.GetHashCode();
                Sprite cached;
                if (SpriteCache.TryGetValue(key, out cached) && cached != null) return cached;

                var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
                {
                    name = "DualGridMask-" + ((int)mask).ToString("00"),
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                var pixels = new Color32[TextureSize * TextureSize];
                for (var y = 0; y < TextureSize; y++)
                {
                    var v = (y + .5f) / TextureSize;
                    for (var x = 0; x < TextureSize; x++)
                    {
                        var u = (x + .5f) / TextureSize;
                        var value = BilinearMaskValue(mask, u, v);
                        var alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.42f, .58f, value));
                        var edgeFactor = 1f - Mathf.Clamp01(Mathf.Abs(value - .5f) / .12f);
                        var color = Color.Lerp(fill, edge, edgeFactor * .72f);
                        color.a *= alpha;
                        pixels[y * TextureSize + x] = color;
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                var sprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize),
                    new Vector2(.5f, .5f), TextureSize, 0, SpriteMeshType.FullRect);
                sprite.name = texture.name;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                SpriteCache[key] = sprite;
                return sprite;
            }
        }

        private static float BilinearMaskValue(DualGridMask mask, float u, float v)
        {
            var northWest = (mask & DualGridMask.NorthWest) != 0 ? 1f : 0f;
            var northEast = (mask & DualGridMask.NorthEast) != 0 ? 1f : 0f;
            var southEast = (mask & DualGridMask.SouthEast) != 0 ? 1f : 0f;
            var southWest = (mask & DualGridMask.SouthWest) != 0 ? 1f : 0f;
            var south = Mathf.Lerp(southWest, southEast, u);
            var north = Mathf.Lerp(northWest, northEast, u);
            return Mathf.Lerp(south, north, v);
        }
    }
}
