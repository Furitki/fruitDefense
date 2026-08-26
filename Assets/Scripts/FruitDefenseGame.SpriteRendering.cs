using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.App;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Platform;
using FruitDefense.Presentation;
using FruitDefense.Tilemaps;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense
{
    public sealed partial class FruitDefenseGame
    {
        private static Texture2D CreateAttackRangeTexture()
        {
            var size = AttackRangeTextureSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PlantAttackRange",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var normalized = new Vector2((x + .5f) / size * 2f - 1f, (y + .5f) / size * 2f - 1f);
                var distance = normalized.magnitude;
                if (distance > 1f) continue;
                var edge = Mathf.InverseLerp(.88f, 1f, distance);
                pixels[y * size + x] = new Color(.98f, .86f, .2f, Mathf.Lerp(.12f, .42f, edge));
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void DrawTempSprite(Rect rect, TempSprite sprite)
        {
            DrawAtlasSprite(_tempArtAtlas, rect, (int)sprite, Color.white);
        }

        private void DrawTempSprite(Rect rect, TempSprite sprite, Color tint)
        {
            DrawAtlasSprite(_tempArtAtlas, rect, (int)sprite, tint);
        }

        private void DrawStatefulTempSprite(RuntimeUiDrawContext drawContext,
            Rect rect, TempSprite sprite, RuntimeUiInteractionState state)
        {
            var opacity = state == RuntimeUiInteractionState.Disabled
                ? drawContext.Opacity(state)
                : 1f;
            DrawTempSprite(rect, sprite, new Color(1f, 1f, 1f, opacity));
        }

        private void DrawVfxSprite(Rect rect, CombatSprite sprite)
        {
            DrawAtlasSprite(_combatVfxAtlas, rect, (int)sprite, Color.white);
        }

        private void DrawVfxSprite(Rect rect, CombatSprite sprite, Color tint)
        {
            DrawAtlasSprite(_combatVfxAtlas, rect, (int)sprite, tint);
        }

        private static void DrawAtlasSprite(Texture2D atlas, Rect rect, int index, Color tint)
        {
            if (atlas == null) return;
            const float cell = .25f;
            const float inset = .004f;
            var column = index % 4;
            var rowFromTop = index / 4;
            var uv = new Rect(
                column * cell + inset,
                1f - (rowFromTop + 1) * cell + inset,
                cell - inset * 2f,
                cell - inset * 2f);
            var previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, atlas, uv, true);
            GUI.color = previous;
        }

        private void DrawAnimatedPlant(Rect rect, Plant plant)
        {
            rect = ApplyPlantVisualHeight(rect, PlantVisualHeightOffset(plant));
            var idlePhase = _game.State.Elapsed * 2.2f + plant.Id * .73f;
            var idlePulse = Mathf.Sin(idlePhase);
            rect.y -= idlePulse * .65f;
            rect = ScaleAroundCenter(rect, 1f + idlePulse * .012f, 1f - idlePulse * .008f);
            var angle = Mathf.Sin(idlePhase * .67f) * 1.25f;
            var reaction = _presentation.ReactionFor(plant.Id);
            rect.position += reaction.Offset;
            rect = ScaleAroundCenter(rect, reaction.Scale.x, reaction.Scale.y);
            var previousMatrix = GUI.matrix;
            if (Mathf.Abs(angle) > .01f) GUIUtility.RotateAroundPivot(angle, rect.center);
            DrawTempSprite(rect, PlantSprite(plant),
                Color.Lerp(Color.white, new Color(1f, .92f, .58f), reaction.Flash));
            GUI.matrix = previousMatrix;
        }

        private float PlantVisualHeightOffset(Plant plant)
        {
            if (plant == null || _game == null || _game.Content == null) return 0f;
            PlantDefinitionDto definition;
            return _game.Content.Plants.TryGetValue(
                    plant.DefinitionId ?? string.Empty, out definition)
                ? definition.potVisualHeightOffset
                : 0f;
        }

        private static Rect ApplyPlantVisualHeight(Rect rect, float height)
        {
            var center = rect.center;
            center.y -= Mathf.Max(0f, height);
            rect.center = center;
            return rect;
        }

        private void DrawRotatedVfx(Rect rect, CombatSprite sprite, float angle, Color tint)
        {
            var previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, rect.center);
            DrawVfxSprite(rect, sprite, tint);
            GUI.matrix = previousMatrix;
        }

        private Rect CenteredRect(Vector2 center, float size)
        {
            size = Projection.LegacyVisualSize(size);
            return new Rect(center.x - size * .5f, center.y - size * .5f, size, size);
        }

        private static Rect ScaleAroundCenter(Rect rect, float scaleX, float scaleY)
        {
            var center = rect.center;
            rect.width *= scaleX;
            rect.height *= scaleY;
            rect.center = center;
            return rect;
        }

        private static TempSprite PlantSprite(string definitionId)
        {
            switch (BattlePresentationVisualCatalog.Plant(definitionId))
            {
                case PlantVisualArchetype.Watermelon: return TempSprite.Watermelon;
                case PlantVisualArchetype.Banana: return TempSprite.Banana;
                case PlantVisualArchetype.Durian: return TempSprite.Durian;
                case PlantVisualArchetype.Sunflower: return TempSprite.Sunflower;
                default: return TempSprite.Pea;
            }
        }

        private static TempSprite EquipmentSprite(string definitionId)
        {
            switch (BattlePresentationVisualCatalog.Equipment(definitionId))
            {
                case EquipmentVisualArchetype.Ice: return TempSprite.Ice;
                case EquipmentVisualArchetype.Chili: return TempSprite.Chili;
                default: return TempSprite.Gatling;
            }
        }

        private static TempSprite PlantSprite(Plant plant)
        {
            if (plant == null) return PlantSprite(string.Empty);
            if (string.IsNullOrEmpty(plant.EquipmentId)
                || BattlePresentationVisualCatalog.Equipment(plant.EquipmentId)
                    == EquipmentVisualArchetype.Generic)
                return PlantSprite(plant.DefinitionId);
            return EquipmentSprite(plant.EquipmentId);
        }

        private static TempSprite ZombieSprite(string definitionId)
        {
            switch (BattlePresentationVisualCatalog.Enemy(definitionId))
            {
                case EnemyVisualArchetype.Runner: return TempSprite.Runner;
                case EnemyVisualArchetype.Armored: return TempSprite.Armored;
                case EnemyVisualArchetype.Boss: return TempSprite.Boss;
                default: return TempSprite.Zombie;
            }
        }

        private void SetStatus(bool success, string text)
        {
            _status = BattleUiPresentationState.FormatTransientStatus(success, text);
            _statusState = success
                ? RuntimeUiInteractionState.Success
                : RuntimeUiInteractionState.Error;
            InvalidatePreparedStatusText();
            _statusPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledStatusSeconds);
        }

        private void SetGuidanceStatus(string text)
        {
            _status = text ?? string.Empty;
            _statusState = RuntimeUiInteractionState.Normal;
            InvalidatePreparedStatusText();
            _statusPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledStatusSeconds);
        }

        private void PrepareTransientStatusText(RuntimeUiDrawContext drawContext,
            Rect statusRect, string text, RuntimeUiInteractionState state)
        {
            if (string.Equals(_preparedStatusSource, text, StringComparison.Ordinal)
                && Mathf.Abs(_preparedStatusWidth - statusRect.width) <= .001f)
                return;

            _preparedStatusTextMode = RuntimeUiGui.ResolveStatusTextMode(
                drawContext, statusRect, text, state,
                RuntimeUiTypographyRole.Supplemental);
            var textLayout = RuntimeUiGui.ResolveStatusTextLayout(
                drawContext, statusRect, state,
                RuntimeUiTypographyRole.Supplemental, _preparedStatusTextMode);
            _preparedStatusTextLines = RuntimeUiGui.ResolveStatusTextLines(textLayout, text);
            _preparedStatusSource = text;
            _preparedStatusWidth = statusRect.width;
        }

        private void InvalidatePreparedStatusText()
        {
            _preparedStatusSource = string.Empty;
            _preparedStatusWidth = -1f;
            _preparedStatusTextMode = RuntimeUiStatusTextMode.SingleLine;
            _preparedStatusTextLines = default;
        }

        private Vector2 ToBoard(Vector2 point)
        {
            return Projection.MapToScreen(point) + _presentation.BattlefieldOffset;
        }

        private Rect OffsetBattlefieldVisual(Rect rect)
        {
            rect.position += _presentation.BattlefieldOffset;
            return rect;
        }

        private void DrawWorldLabel(Rect rect, string text, int fontSize, Color color)
        {
            _worldLabelStyle.fontSize = fontSize;
            _worldLabelStyle.normal.textColor = color;
            GUI.Label(rect, text, _worldLabelStyle);
        }

        private static void DrawWorldRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawWorldOutline(Rect rect, float thickness, Color color)
        {
            thickness = Mathf.Max(1f, Mathf.Min(thickness, Mathf.Min(rect.width, rect.height) * .5f));
            DrawWorldRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawWorldRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawWorldRect(new Rect(rect.x, rect.y + thickness,
                thickness, rect.height - thickness * 2f), color);
            DrawWorldRect(new Rect(rect.xMax - thickness, rect.y + thickness,
                thickness, rect.height - thickness * 2f), color);
        }

        private Color ThemeColor(Func<LevelPresentationThemeDefinition, string> select,
            Color fallback)
        {
            var theme = _game == null ? null : _game.Theme;
            if (theme == null || select == null) return fallback;
            return ColorUtility.TryParseHtmlString(select(theme), out var color) ? color : fallback;
        }

    }
}
