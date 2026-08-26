using System;
using System.IO;
using System.Linq;
using FruitDefense.Development.GmStress;
using FruitDefense.Core;
using FruitDefense.Presentation;
using FruitDefense.Tilemaps;
using FruitDefense.UI;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class GmStressBattleLayoutSmoke
    {
        private const string PresenterSourcePath =
            "Assets/Scripts/Development/GmStress/GmStressBattlePresenter.cs";
        private const string ReleaseTerrainPalettePath =
            "Assets/Battlefield/Terrain/OrchardDefaultTerrainPalette.asset";

        public static void Validate()
        {
            var layout = new GmStressBattleLayout(GmStressBattleFactory.CreateMap());
            ValidateHeader(layout);
            ValidateEnemySelectors(layout);
            ValidatePlantSelectors(layout);
            ValidatePresenterOwnerRectSource();
            ValidateCompactControlContract();
            ValidateRegisteredTerrainBrushContract();
            Debug.Log("FRUIT_DEFENSE_GM_STRESS_LAYOUT_OK");
        }

        private static void ValidateHeader(GmStressBattleLayout layout)
        {
            AssertRect(layout.HeaderTitle, 24f, 12f, 150f, 34f,
                "GM header title owner rect");
            Assert(layout.HeaderTitle.yMax <= layout.ActiveMetric.yMin,
                "GM header title stays above the active metric without overlap");
            Assert(Contains(layout.Header, layout.HeaderTitle)
                && Contains(layout.Header, layout.ActiveMetric),
                "GM title and active metric stay inside the header panel");
        }

        private static void ValidateEnemySelectors(GmStressBattleLayout layout)
        {
            var owner = new Rect(20f, 528f, 350f, 22f);
            Assert(owner.height == 22f,
                "enemy supplemental title uses the required 22px line height");
            for (var index = 0; index < GmStressBattleIds.EnemyDefinitionIds.Count;
                 index++)
            {
                var choice = layout.EnemyChoice(index);
                AssertRect(choice, 20f + index * 92f, 550f, 86f, 48f,
                    "enemy selector " + index);
                Assert(owner.yMax <= choice.yMin
                    && Contains(layout.EnemyPanel, choice),
                    "enemy title ends at the selector boundary and choice stays in its panel "
                    + index);
            }
        }

        private static void ValidatePlantSelectors(GmStressBattleLayout layout)
        {
            var owner = new Rect(20f, 696f, 350f, 22f);
            Assert(owner.height == 22f,
                "plant supplemental title uses the required 22px line height");
            var choices = Enumerable.Range(0,
                    GmStressBattleIds.PlantDefinitionIds.Count)
                .Select(layout.PlantChoice).ToArray();
            for (var index = 0; index < choices.Length; index++)
            {
                var choice = choices[index];
                AssertRect(choice, 18f + index * 74f, 718f, 70f, 62f,
                    "plant selector " + index);
                Assert(owner.yMax <= choice.yMin
                    && Contains(layout.PlantPanel, choice),
                    "plant title ends at the selector boundary and choice stays in its panel "
                    + index);

                var icon = new Rect(choice.x + (choice.width - 34f) * .5f,
                    choice.y + 3f, 34f, 34f);
                var label = new Rect(choice.x + 3f, choice.yMax - 24f,
                    choice.width - 6f, 22f);
                Assert(icon.width == 34f && icon.height == 34f
                    && Mathf.Approximately(icon.y, choice.y + 3f),
                    "stacked plant selector icon uses exact 34px size at y+3: "
                    + index);
                AssertRect(label, choice.x + 3f, choice.yMax - 24f,
                    choice.width - 6f, 22f,
                    "stacked plant selector label " + index);
                Assert(icon.yMax <= label.yMin
                    && Contains(choice, icon) && Contains(choice, label),
                    "stacked plant icon and 22px label are disjoint and contained: "
                    + index);
            }
        }

        private static void ValidatePresenterOwnerRectSource()
        {
            Assert(File.Exists(PresenterSourcePath),
                "GM presenter source exists for exact private owner-rect validation");
            var compact = new string(File.ReadAllText(PresenterSourcePath)
                .Where(character => !char.IsWhiteSpace(character)).ToArray());
            Assert(compact.Contains("newRect(20f,528f,350f,22f)"),
                "presenter uses the exact enemy supplemental owner rect");
            Assert(compact.Contains("newRect(20f,696f,350f,22f)"),
                "presenter uses the exact plant supplemental owner rect");
            Assert(compact.Contains(
                    "newRect(rect.x+(rect.width-34f)*.5f,rect.y+3f,34f,34f)"),
                "presenter uses the exact stacked 34px selector icon rect");
            Assert(compact.Contains(
                    "newRect(rect.x+3f,rect.yMax-24f,rect.width-6f,22f)"),
                "presenter uses the exact stacked selector label rect");
            Assert(compact.Contains(
                    "GUI.Button(rect,GUIContent.none,_drawContext.Styles.HitTarget)"),
                "selector hit testing uses the same owner rect passed to drawing");
        }

        private static void ValidateCompactControlContract()
        {
            var pauseSpec = BattleUiPresentationState.ResolveActionSpec(
                BattleUiActionSemantic.PauseContinue);
            var speedSpec = BattleUiPresentationState.ResolveActionSpec(
                BattleUiActionSemantic.Speed);
            Assert(pauseSpec.Role == RuntimeUiActionKind.Quiet
                && pauseSpec.ContentForm == RuntimeUiActionContentForm.IconOnly
                && pauseSpec.Behavior == RuntimeUiActionBehavior.PersistentMode,
                "GM pause resolves the approved Quiet/IconOnly/PersistentMode spec");
            Assert(speedSpec.Role == RuntimeUiActionKind.Quiet
                && speedSpec.ContentForm
                    == RuntimeUiActionContentForm.CompactMultiplier
                && speedSpec.Behavior == RuntimeUiActionBehavior.PersistentMode,
                "GM speed resolves the approved Quiet/CompactMultiplier/PersistentMode spec");

            var compact = CompactPresenterSource();
            Assert(CountOccurrences(compact,
                    "RuntimeUiGui.DrawCompactControlVisual(") == 2,
                "GM presenter draws exactly pause and speed through compact-control visuals");
            Assert(compact.Contains(
                    "DrawCompactControlVisual(_drawContext,_layout.PauseAction,BattleUiPresentationState.ResolveActionSpec(BattleUiActionSemantic.PauseContinue)"),
                "GM pause visual uses ResolveActionSpec(PauseContinue) on PauseAction");
            Assert(compact.Contains(
                    "DrawCompactControlVisual(_drawContext,_layout.SpeedAction,BattleUiPresentationState.ResolveActionSpec(BattleUiActionSemantic.Speed)"),
                "GM speed visual uses ResolveActionSpec(Speed) on SpeedAction");
            Assert(compact.Contains(
                    "multiplierText:Simulation.State.Speed+\"×\""),
                "GM speed compact control renders the authoritative speed multiplier text");
            Assert(compact.Contains(
                    "GUI.Button(_layout.PauseAction,GUIContent.none,_drawContext.Styles.HitTarget)"),
                "GM pause HitTarget uses the same PauseAction rect as its visual");
            Assert(compact.Contains(
                    "GUI.Button(_layout.SpeedAction,GUIContent.none,_drawContext.Styles.HitTarget)"),
                "GM speed HitTarget uses the same SpeedAction rect as its visual");
            Assert(!compact.Contains(
                    "DrawAction(_drawContext,_layout.PauseAction")
                && !compact.Contains(
                    "DrawAction(_drawContext,_layout.SpeedAction"),
                "PersistentMode pause/speed specs are never passed into DrawAction");
        }

        private static void ValidateRegisteredTerrainBrushContract()
        {
            var map = GmStressBattleFactory.CreateMap();
            for (var y = 0; y < GmStressBattleIds.GridHeight; y++)
            for (var x = 0; x < GmStressBattleIds.GridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                Assert(map.BaseSurfaceAt(cell) == BattlefieldLayerIds.Surfaces.Soil,
                    "every GM visual cell uses the brush soil endpoint as its base");
                if (y < GmStressBattleIds.PlantRowStart)
                {
                    Assert(string.IsNullOrEmpty(map.LandformSurfaceAt(cell))
                        && string.IsNullOrEmpty(map.ContourStyleAt(cell))
                        && string.IsNullOrEmpty(map.EdgeStyleAt(cell)),
                        "route rows remain soil-only brush base cells");
                    continue;
                }
                Assert(map.LandformSurfaceAt(cell) == BattlefieldLayerIds.Surfaces.Grass
                    && map.ContourStyleAt(cell)
                        == BattlefieldLayerIds.ContourStyles.Square
                    && map.EdgeStyleAt(cell) == BattlefieldLayerIds.EdgeStyles.Refined,
                    "bottom two GM rows use square refined grass-on-soil composition");
            }

            var palette = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(
                ReleaseTerrainPalettePath);
            Assert(palette != null, "registered orchard terrain palette asset exists");
            Assert(GmStressBattleFactory.ValidateTerrainPalette(
                    map, palette, out var reason),
                "GM map resolves the exact registered production brush: " + reason);
            Assert(palette.TryGetEdgeTileSet(
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined,
                    out var tileSet, out var complementMask)
                && tileSet != null && tileSet.name == "GrassSoilCompositeTileSet"
                && !complementMask,
                "GM terrain resolves the production GrassSoilCompositeTileSet exactly");

            var presenter = CompactPresenterSource();
            Assert(presenter.Contains(
                    "BattlefieldTerrainGuiRenderer.DrawValidated(Simulation.Map,_layout.Battlefield,_terrainPalette)")
                && !presenter.Contains("DrawRect(Inset(visual,1f),color)"),
                "GM presenter uses the shared terrain renderer and removes flat cell colors");
            var releaseGame = new string(RuntimeUiSourceAuthority.ReadFruitDefenseGame()
                .Where(character => !char.IsWhiteSpace(character)).ToArray());
            Assert(releaseGame.Contains(
                    "BattlefieldTerrainGuiRenderer.DrawValidated(map,Projection,palette)"),
                "release and GM battle use the same layered-terrain GUI renderer");
        }

        private static string CompactPresenterSource()
        {
            Assert(File.Exists(PresenterSourcePath),
                "GM presenter source exists for compact-control validation");
            return new string(File.ReadAllText(PresenterSourcePath)
                .Where(character => !char.IsWhiteSpace(character)).ToArray());
        }

        private static int CountOccurrences(string source, string token)
        {
            var count = 0;
            for (var index = 0;
                 (index = source.IndexOf(token, index, StringComparison.Ordinal)) >= 0;
                 index += token.Length)
                count++;
            return count;
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            const float epsilon = .0001f;
            return inner.xMin >= outer.xMin - epsilon
                && inner.yMin >= outer.yMin - epsilon
                && inner.xMax <= outer.xMax + epsilon
                && inner.yMax <= outer.yMax + epsilon;
        }

        private static void AssertRect(Rect actual, float x, float y,
            float width, float height, string label)
        {
            Assert(Mathf.Approximately(actual.x, x)
                && Mathf.Approximately(actual.y, y)
                && Mathf.Approximately(actual.width, width)
                && Mathf.Approximately(actual.height, height),
                label + " expected=(" + x + "," + y + "," + width + ","
                + height + ") actual=" + actual);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "GM stress layout validation failed: " + message);
        }
    }
}
