using UnityEngine;

namespace FruitDefense.Shell
{
    public sealed class ShellStyleSet
    {
        public GUIStyle Title { get; private set; }
        public GUIStyle PrimaryButton { get; private set; }
        public GUIStyle SecondaryButton { get; private set; }
        public GUIStyle CardTitle { get; private set; }
        public GUIStyle CardBody { get; private set; }
        public GUIStyle ResultOutcome { get; private set; }
        public GUIStyle ResultMetric { get; private set; }
        public GUIStyle Status { get; private set; }
        public GUIStyle Panel { get; private set; }

        public static ShellStyleSet Create(GUISkin skin, float scale)
        {
            scale = Mathf.Max(.5f, scale);
            return new ShellStyleSet
            {
                Title = Label(skin, Mathf.RoundToInt(31f * scale), FontStyle.Bold, TextAnchor.MiddleCenter),
                PrimaryButton = Button(skin, Mathf.RoundToInt(22f * scale), FontStyle.Bold),
                SecondaryButton = Button(skin, Mathf.RoundToInt(18f * scale), FontStyle.Normal),
                CardTitle = Label(skin, Mathf.RoundToInt(19f * scale), FontStyle.Bold, TextAnchor.MiddleLeft),
                CardBody = Label(skin, Mathf.RoundToInt(14f * scale), FontStyle.Normal, TextAnchor.MiddleLeft),
                ResultOutcome = Label(skin, Mathf.RoundToInt(28f * scale), FontStyle.Bold, TextAnchor.MiddleCenter),
                ResultMetric = Label(skin, Mathf.RoundToInt(18f * scale), FontStyle.Normal, TextAnchor.MiddleCenter),
                Status = Label(skin, Mathf.RoundToInt(13f * scale), FontStyle.Normal, TextAnchor.UpperCenter),
                Panel = new GUIStyle(skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    padding = new RectOffset(
                        Mathf.RoundToInt(14f * scale),
                        Mathf.RoundToInt(14f * scale),
                        Mathf.RoundToInt(10f * scale),
                        Mathf.RoundToInt(10f * scale)),
                },
            };
        }

        private static GUIStyle Label(GUISkin skin, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            return new GUIStyle(skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = true,
            };
        }

        private static GUIStyle Button(GUISkin skin, int fontSize, FontStyle fontStyle)
        {
            return new GUIStyle(skin.button)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
        }
    }

    public static class ShellGui
    {
        public static void DrawPanel(Rect rect, GUIStyle style)
        {
            var previous = GUI.color;
            GUI.color = new Color(.18f, .25f, .20f, .96f);
            GUI.Box(rect, GUIContent.none, style);
            GUI.color = previous;
        }

        public static void DrawReservedCard(Rect rect, string title, string description, ShellStyleSet styles)
        {
            DrawPanel(rect, styles.Panel);
            var inset = Mathf.Max(8f, rect.height * .14f);
            var textX = rect.x + inset;
            var textWidth = rect.width - inset * 2f;
            GUI.Label(new Rect(textX, rect.y + inset * .45f, textWidth, rect.height * .38f), title, styles.CardTitle);
            GUI.Label(new Rect(textX, rect.y + rect.height * .48f, textWidth, rect.height * .36f), description, styles.CardBody);
        }
    }
}
