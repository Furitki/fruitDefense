using System;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Presentation
{
    public enum BattleCombatSprite
    {
        PeaProjectile,
        WatermelonProjectile,
        BananaProjectile,
        DurianDrop,
        PeaImpact,
        WatermelonBlast,
        DurianShockwave,
        SunBurst,
        GatlingMuzzle,
        IceImpact,
        FrozenAura,
        ChiliImpact,
        Burning,
        HitSpark,
        ShockwaveRing,
        SunCollectible,
    }

    public readonly struct BattlePlantMotionSample
    {
        public BattlePlantMotionSample(Vector2 offset, Vector2 scale,
            float angle, float flash)
        {
            Offset = offset;
            Scale = scale;
            Angle = angle;
            Flash = flash;
        }

        public Vector2 Offset { get; }
        public Vector2 Scale { get; }
        public float Angle { get; }
        public float Flash { get; }
    }

    /// <summary>
    /// Allocation-free gameplay combat drawing shared by the release battle and
    /// development stress battle. The atlas order is the production content
    /// contract above; callers provide only authoritative positions and state.
    /// </summary>
    public static class BattleCombatGuiRenderer
    {
        public const string AtlasResourcePath = "TempArt/combat-vfx-atlas";
        public const int AtlasColumns = 4;
        public const int AtlasRows = 4;
        public const int AtlasSpriteCount = AtlasColumns * AtlasRows;

        public static bool ValidateAtlas(Texture2D atlas, out string reason)
        {
            if (atlas == null)
            {
                reason = "combat-vfx-atlas is missing";
                return false;
            }
            if (atlas.width < AtlasColumns || atlas.height < AtlasRows
                || atlas.width != atlas.height)
            {
                reason = "combat-vfx-atlas must be a square texture containing the 4-by-4 grid";
                return false;
            }
            if ((int)BattleCombatSprite.SunCollectible + 1 != AtlasSpriteCount)
            {
                reason = "combat-vfx-atlas sprite contract is incomplete";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        public static BattlePlantMotionSample SamplePlantMotion(float elapsed,
            int entityId, PresentationReactionSample reaction)
        {
            var idlePhase = elapsed * 2.2f + entityId * .73f;
            var idlePulse = Mathf.Sin(idlePhase);
            return new BattlePlantMotionSample(
                reaction.Offset + Vector2.up * (-idlePulse * .65f),
                new Vector2((1f + idlePulse * .012f) * reaction.Scale.x,
                    (1f - idlePulse * .008f) * reaction.Scale.y),
                Mathf.Sin(idlePhase * .67f) * 1.25f,
                reaction.Flash);
        }

        public static void DrawPlant(Texture2D plantAtlas, Rect rect,
            int spriteIndex, float elapsed, int entityId,
            PresentationReactionSample reaction)
        {
            var motion = SamplePlantMotion(elapsed, entityId, reaction);
            rect.position += motion.Offset;
            rect = ScaleAroundCenter(rect, motion.Scale.x, motion.Scale.y);
            var previousMatrix = GUI.matrix;
            if (Mathf.Abs(motion.Angle) > .01f)
                GUIUtility.RotateAroundPivot(motion.Angle, rect.center);
            DrawAtlasSprite(plantAtlas, rect, spriteIndex,
                Color.Lerp(Color.white, new Color(1f, .92f, .58f), motion.Flash));
            GUI.matrix = previousMatrix;
        }

        public static BattleCombatSprite ProjectileSprite(string presentationId)
        {
            switch (BattlePresentationVisualCatalog.Projectile(presentationId))
            {
                case ProjectileVisualArchetype.Watermelon:
                    return BattleCombatSprite.WatermelonProjectile;
                case ProjectileVisualArchetype.Banana:
                    return BattleCombatSprite.BananaProjectile;
                default:
                    return BattleCombatSprite.PeaProjectile;
            }
        }

        public static BattleCombatSprite PrimaryEffectSprite(
            PresentationVfxKind kind)
        {
            switch (kind)
            {
                case PresentationVfxKind.PeaImpact:
                    return BattleCombatSprite.PeaImpact;
                case PresentationVfxKind.WatermelonBlast:
                    return BattleCombatSprite.WatermelonBlast;
                case PresentationVfxKind.BananaHit:
                    return BattleCombatSprite.HitSpark;
                case PresentationVfxKind.DurianImpact:
                    return BattleCombatSprite.DurianShockwave;
                case PresentationVfxKind.SunBurst:
                    return BattleCombatSprite.SunBurst;
                case PresentationVfxKind.GatlingMuzzle:
                    return BattleCombatSprite.GatlingMuzzle;
                case PresentationVfxKind.IceImpact:
                    return BattleCombatSprite.IceImpact;
                case PresentationVfxKind.FreezeProc:
                    return BattleCombatSprite.FrozenAura;
                case PresentationVfxKind.ChiliImpact:
                    return BattleCombatSprite.ChiliImpact;
                case PresentationVfxKind.BurnTick:
                    return BattleCombatSprite.Burning;
                case PresentationVfxKind.Defeat:
                    return BattleCombatSprite.HitSpark;
                default:
                    throw new InvalidOperationException(
                        "Unsupported combat presentation effect: " + kind);
            }
        }

        public static BattleCombatSprite? SecondaryEffectSprite(
            PresentationVfxKind kind)
        {
            switch (kind)
            {
                case PresentationVfxKind.WatermelonBlast:
                case PresentationVfxKind.DurianImpact:
                case PresentationVfxKind.FreezeProc:
                case PresentationVfxKind.Defeat:
                    return BattleCombatSprite.ShockwaveRing;
                default:
                    return null;
            }
        }

        public static void DrawProjectile(Texture2D atlas, Vector2 point,
            float visualScale, ProjectileFlash projectile, string presentationId,
            float elapsed)
        {
            if (projectile == null) return;
            var sprite = ProjectileSprite(presentationId);
            if (sprite == BattleCombatSprite.WatermelonProjectile)
            {
                var size = Mathf.Lerp(30f, 40f,
                    Mathf.Sin(projectile.Progress * Mathf.PI));
                DrawSprite(atlas, CenteredRect(point, size * visualScale), sprite,
                    Color.white);
                return;
            }
            if (sprite == BattleCombatSprite.BananaProjectile)
            {
                var angle = elapsed * 900f * (projectile.Returning ? -1f : 1f);
                DrawRotatedSprite(atlas,
                    CenteredRect(point, 38f * visualScale), sprite, angle,
                    projectile.Returning
                        ? new Color(1f, .96f, .62f)
                        : Color.white);
                return;
            }
            DrawSprite(atlas, CenteredRect(point, 26f * visualScale), sprite,
                Color.white);
        }

        public static void DrawCombatEffect(Texture2D atlas, Vector2 point,
            float visualScale, PresentationCombatEffect effect)
        {
            if (effect == null || effect.Kind == PresentationVfxKind.None) return;
            var progress = effect.Duration <= 0f
                ? 1f
                : 1f - Mathf.Clamp01(effect.Ttl / effect.Duration);
            var fade = Mathf.Clamp01(1f - progress * .9f);
            var whiteFade = new Color(1f, 1f, 1f, fade);
            switch (effect.Kind)
            {
                case PresentationVfxKind.PeaImpact:
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(20f, 39f, progress) * visualScale),
                        PrimaryEffectSprite(effect.Kind), whiteFade);
                    break;
                case PresentationVfxKind.WatermelonBlast:
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(48f, 102f, progress) * visualScale),
                        PrimaryEffectSprite(effect.Kind), whiteFade);
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(35f, 125f, progress) * visualScale),
                        SecondaryEffectSprite(effect.Kind).Value, whiteFade);
                    break;
                case PresentationVfxKind.BananaHit:
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(18f, 43f, progress) * visualScale),
                        PrimaryEffectSprite(effect.Kind), whiteFade);
                    break;
                case PresentationVfxKind.DurianImpact:
                    DrawSprite(atlas, CenteredRect(
                            point + Vector2.up * (13f * visualScale),
                            Mathf.Lerp(52f, 128f, progress) * visualScale),
                        PrimaryEffectSprite(effect.Kind), whiteFade);
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(40f, 138f, progress) * visualScale),
                        SecondaryEffectSprite(effect.Kind).Value, whiteFade);
                    break;
                case PresentationVfxKind.SunBurst:
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(38f, 78f,
                                Mathf.Sin(progress * Mathf.PI)) * visualScale),
                        PrimaryEffectSprite(effect.Kind), whiteFade);
                    break;
                case PresentationVfxKind.GatlingMuzzle:
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(34f, 20f, progress) * visualScale),
                        PrimaryEffectSprite(effect.Kind), whiteFade);
                    break;
                case PresentationVfxKind.IceImpact:
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(30f, 58f, progress) * visualScale),
                        PrimaryEffectSprite(effect.Kind), whiteFade);
                    break;
                case PresentationVfxKind.FreezeProc:
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(42f, 78f, progress) * visualScale),
                        PrimaryEffectSprite(effect.Kind), whiteFade);
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(25f, 70f, progress) * visualScale),
                        SecondaryEffectSprite(effect.Kind).Value,
                        new Color(.7f, .9f, 1f, fade));
                    break;
                case PresentationVfxKind.ChiliImpact:
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(34f, 62f, progress) * visualScale),
                        PrimaryEffectSprite(effect.Kind), whiteFade);
                    break;
                case PresentationVfxKind.BurnTick:
                    DrawSprite(atlas, CenteredRect(
                            point + Vector2.up * (5f * visualScale),
                            Mathf.Lerp(18f, 30f, progress) * visualScale),
                        PrimaryEffectSprite(effect.Kind), whiteFade);
                    break;
                case PresentationVfxKind.Defeat:
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(36f, 82f, progress) * visualScale),
                        PrimaryEffectSprite(effect.Kind),
                        new Color(1f, .88f, .45f, fade));
                    DrawSprite(atlas, CenteredRect(point,
                            Mathf.Lerp(24f, 94f, progress) * visualScale),
                        SecondaryEffectSprite(effect.Kind).Value,
                        new Color(1f, .82f, .35f, fade));
                    break;
                default:
                    throw new InvalidOperationException(
                        "Unsupported combat presentation effect: " + effect.Kind);
            }
        }

        public static void DrawFrozenAura(Texture2D atlas, Rect entityRect,
            Color tint)
        {
            DrawSprite(atlas, Grow(entityRect, 4f),
                BattleCombatSprite.FrozenAura, tint);
        }

        public static void DrawBurningStatus(Texture2D atlas, Rect entityRect)
        {
            DrawSprite(atlas,
                new Rect(entityRect.xMax - 5f, entityRect.y - 6f, 11f, 11f),
                BattleCombatSprite.Burning, Color.white);
        }

        public static void DrawAtlasSprite(Texture2D atlas, Rect rect, int index,
            Color tint)
        {
            if (atlas == null) return;
            const float cell = .25f;
            const float inset = .004f;
            var column = index % AtlasColumns;
            var rowFromTop = index / AtlasColumns;
            var uv = new Rect(column * cell + inset,
                1f - (rowFromTop + 1) * cell + inset,
                cell - inset * 2f, cell - inset * 2f);
            var previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, atlas, uv, true);
            GUI.color = previous;
        }

        private static void DrawSprite(Texture2D atlas, Rect rect,
            BattleCombatSprite sprite, Color tint)
        {
            DrawAtlasSprite(atlas, rect, (int)sprite, tint);
        }

        private static void DrawRotatedSprite(Texture2D atlas, Rect rect,
            BattleCombatSprite sprite, float angle, Color tint)
        {
            var previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, rect.center);
            DrawSprite(atlas, rect, sprite, tint);
            GUI.matrix = previousMatrix;
        }

        private static Rect CenteredRect(Vector2 center, float size)
        {
            return new Rect(center.x - size * .5f, center.y - size * .5f,
                size, size);
        }

        private static Rect ScaleAroundCenter(Rect rect, float scaleX,
            float scaleY)
        {
            var center = rect.center;
            rect.width *= scaleX;
            rect.height *= scaleY;
            rect.center = center;
            return rect;
        }

        private static Rect Grow(Rect rect, float amount)
        {
            return Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount,
                rect.xMax + amount, rect.yMax + amount);
        }
    }
}
