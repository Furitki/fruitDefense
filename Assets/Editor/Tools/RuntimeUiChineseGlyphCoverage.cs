using UnityEngine;

namespace FruitDefense.Editor
{
    public static class RuntimeUiChineseGlyphCoverage
    {
        // This is the one explicit glyph authority for player-visible Bootstrap,
        // Lobby, Battle, and Settlement copy, including dynamic content names,
        // world labels, level IDs/error codes, punctuation, and runtime symbols.
        // It is intentionally maintained from the owning copy/content contracts;
        // validators must consume it directly and must not scan source comments.
        public const string RequiredGlyphs =
            "!+-.:0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz"
            + "·×…■▰▲▶◆●◒♛♣✓✹✿❄、一丁三上下不与个中为了二五交产人伤位低住体余作信倒候停僵光入全关兼内再冰冲冷准出击切利到刷剩功加动励升单卡即却厅压原取受只可右合向启周命品器回园围圃在场块坚塔处备复大失奖始存学守安完定害容宽将尸局左已库廊建开弯形往得御心快恢息悉戏成或战所扩找把护抵拖择拽持按损换操攻放效敌教斗新无日时星是普暂有期未本机杀束来松构果枪查标栏株核格桶植椒榴槽次正此武段水法波消添游炸点熟爆物王现理瓜生用甲的盆盖目直看短砸碑种秒移程稳空穿立第筑算线经结继续绿置耗胜能至色花苗范莲获葵蕉行袭装西覆计试该误请豆豌败走足路转输辣达运近返这进远连迫选透通速部配里重铁错闯防阳阶障面页顾领频首香！，：；｜";

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
