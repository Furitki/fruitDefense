using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class RuntimeUiChineseGlyphCoverage
    {
        // Fixed shell/gameplay copy lives here. Player-visible catalog copy is
        // appended from the exported bundled outgame content below, so adding
        // or changing authored names/descriptions cannot silently escape the
        // packaged font subset.
        private const string FixedRequiredGlyphs =
            "!+-.:0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz"
            + "·×…■▰▲▶◆●◒♛♣✓✹✿❄、一丁三上下不与个中为了二五交产人伤位低住体余作信倒候停僵光入全关兼内再冰冲冷准出击切利到刷剩功加动励升单卡即却厅压原取受只可右合向启周命品器回园围圃在场块坚塔处备复大失奖始存学守安完定害容宽将尸局左已库廊建开弯形往得御心快恢息悉戏成或战所扩找把护抵拖择拽持按损换操攻放效敌教斗新无日时星是普暂有期未本机杀束来松构果枪查标栏株核格桶植椒榴槽次正此武段水法波消添游炸点熟爆物王现理瓜生用甲的盆盖目直看短砸碑种秒移程稳空穿立第筑算线经结继续绿置耗胜能至色花苗范莲获葵蕉行袭装西覆计试该误请豆豌败走足路转输辣达运近返这进远连迫选透通速部配里重铁错闯防阳阶障面页顾领频首香冻"
            + "主养前化卫号尚强当没活版玩节账载长件保地培拥料晨材条档满等级育览锁限露预！，：；｜";

        // This is the single resolved authority consumed by font generation and
        // editor validation. Order is stable: fixed copy first, then exported
        // content in catalog traversal order, with duplicates removed.
        public static readonly string RequiredGlyphs = BuildRequiredGlyphs();

        public static IReadOnlyList<string> ReadBundledOutgameVisibleCopy()
        {
            var absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "..", BattleContentCatalogEditor.OutgameJsonPath));
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException(
                    "Bundled outgame content is required for UI glyph closure.",
                    absolutePath);

            var catalog = OutgameContentJson.Deserialize(
                File.ReadAllText(absolutePath, Encoding.UTF8));
            var copy = new List<string>();
            AppendVisibleCopy(copy, catalog.items,
                value => value.displayName, value => value.description);
            AppendVisibleCopy(copy, catalog.activities,
                value => value.displayName, value => value.description);
            AppendVisibleCopy(copy, catalog.growthEquipment,
                value => value.displayName, value => value.description);
            AppendVisibleCopy(copy, catalog.cultivationNodes,
                value => value.displayName, value => value.description);
            AppendVisibleCopy(copy, catalog.growthPolicies,
                value => value.displayName);
            return copy;
        }

        private static string BuildRequiredGlyphs()
        {
            var seen = new HashSet<char>();
            var resolved = new StringBuilder();
            AppendGlyphs(resolved, seen, FixedRequiredGlyphs);
            var contentCopy = ReadBundledOutgameVisibleCopy();
            for (var index = 0; index < contentCopy.Count; index++)
                AppendGlyphs(resolved, seen, contentCopy[index]);
            return resolved.ToString();
        }

        private static void AppendGlyphs(StringBuilder resolved,
            HashSet<char> seen, string copy)
        {
            if (string.IsNullOrEmpty(copy))
                return;
            for (var index = 0; index < copy.Length; index++)
            {
                var glyph = copy[index];
                if (char.IsControl(glyph) || !seen.Add(glyph))
                    continue;
                resolved.Append(glyph);
            }
        }

        private static void AppendVisibleCopy<T>(List<string> output,
            T[] definitions, params Func<T, string>[] selectors)
        {
            if (definitions == null || selectors == null)
                return;
            for (var definitionIndex = 0;
                 definitionIndex < definitions.Length; definitionIndex++)
            {
                var definition = definitions[definitionIndex];
                for (var selectorIndex = 0;
                     selectorIndex < selectors.Length; selectorIndex++)
                {
                    var copy = selectors[selectorIndex](definition);
                    if (!string.IsNullOrEmpty(copy))
                        output.Add(copy);
                }
            }
        }

        public static bool TryFindMissingGlyph(Font font, out char missingGlyph)
        {
            missingGlyph = default;
            if (font == null)
                return false;
            for (var index = 0; index < RequiredGlyphs.Length; index++)
            {
                var glyph = RequiredGlyphs[index];
                if (font.HasCharacter(glyph))
                    continue;
                missingGlyph = glyph;
                return false;
            }
            return true;
        }
    }
}
