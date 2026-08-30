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

        private static void DrawAtlasSprite(Texture2D atlas, Rect rect, int index, Color tint)
        {
            BattleCombatGuiRenderer.DrawAtlasSprite(atlas, rect, index, tint);
        }

        private void DrawAnimatedPlant(Rect rect, Plant plant)
        {
            rect = ApplyPlantVisualHeight(rect, PlantVisualHeightOffset(plant));
            var reaction = _presentation.ReactionFor(plant.Id);
            BattleCombatGuiRenderer.DrawPlant(_tempArtAtlas, rect,
                (int)PlantSprite(plant), _game.State.Elapsed, plant.Id, reaction);
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

        private static Rect ScaleAroundCenter(Rect rect, float scaleX, float scaleY)
        {
            var center = rect.center;
            rect.width *= scaleX;
            rect.height *= scaleY;
            rect.center = center;
            return rect;
        }

        private TempSprite PlantSprite(string definitionId)
        {
            return ResolvePlantSprite(_game == null ? null : _game.Content, definitionId);
        }

        private static TempSprite ResolvePlantSprite(CompiledBattleContentCatalog content,
            string definitionId)
        {
            PlantDefinitionDto definition;
            var presentationId = content != null
                && content.Plants.TryGetValue(definitionId ?? string.Empty, out definition)
                    ? definition.presentationId : string.Empty;
            switch (BattlePresentationVisualCatalog.Plant(presentationId))
            {
                case PlantVisualArchetype.Watermelon: return TempSprite.Watermelon;
                case PlantVisualArchetype.Banana: return TempSprite.Banana;
                case PlantVisualArchetype.Durian: return TempSprite.Durian;
                case PlantVisualArchetype.Sunflower: return TempSprite.Sunflower;
                default: return TempSprite.Pea;
            }
        }

        private TempSprite EquipmentSprite(string definitionId)
        {
            return ResolveEquipmentSprite(_game == null ? null : _game.Content, definitionId);
        }

        private static TempSprite ResolveEquipmentSprite(CompiledBattleContentCatalog content,
            string definitionId)
        {
            EquipmentDefinitionDto definition;
            var presentationId = content != null
                && content.Equipment.TryGetValue(definitionId ?? string.Empty, out definition)
                    ? definition.presentationId : string.Empty;
            switch (BattlePresentationVisualCatalog.Equipment(presentationId))
            {
                case EquipmentVisualArchetype.Ice: return TempSprite.Ice;
                case EquipmentVisualArchetype.Chili: return TempSprite.Chili;
                default: return TempSprite.Gatling;
            }
        }

        private TempSprite PlantSprite(Plant plant)
        {
            return ResolvePlantSprite(_game == null ? null : _game.Content, plant);
        }

        private static TempSprite ResolvePlantSprite(CompiledBattleContentCatalog content,
            Plant plant)
        {
            if (plant == null) return ResolvePlantSprite(content, string.Empty);
            EquipmentDefinitionDto equipment;
            if (string.IsNullOrEmpty(plant.EquipmentId)
                || content == null
                || !content.Equipment.TryGetValue(plant.EquipmentId, out equipment)
                || BattlePresentationVisualCatalog.Equipment(
                    equipment.presentationId)
                    == EquipmentVisualArchetype.Generic)
                return ResolvePlantSprite(content, plant.DefinitionId);
            return ResolveEquipmentSprite(content, plant.EquipmentId);
        }

        private TempSprite ZombieSprite(string definitionId)
        {
            return ResolveEnemySprite(_game == null ? null : _game.Content, definitionId);
        }

        private static TempSprite ResolveEnemySprite(CompiledBattleContentCatalog content,
            string definitionId)
        {
            EnemyDefinitionDto definition;
            var presentationId = content != null
                && content.Enemies.TryGetValue(definitionId ?? string.Empty, out definition)
                    ? definition.presentationId : string.Empty;
            switch (BattlePresentationVisualCatalog.Enemy(presentationId))
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
