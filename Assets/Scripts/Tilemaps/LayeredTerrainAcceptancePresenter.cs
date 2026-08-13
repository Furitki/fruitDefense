using UnityEngine;

namespace FruitDefense.Tilemaps
{
    [DisallowMultipleComponent]
    public sealed class LayeredTerrainAcceptancePresenter : MonoBehaviour
    {
        private GUIStyle titleStyle;
        private GUIStyle captionStyle;
        private GUIStyle chipStyle;

        private void OnGUI()
        {
            EnsureStyles();
            var scale = Mathf.Min(Screen.width / 402f, Screen.height / 874f);
            var width = Mathf.Min(Screen.width - 24f * scale, 378f * scale);
            var left = (Screen.width - width) * .5f;
            GUI.Label(new Rect(left, 24f * scale, width, 36f * scale),
                "TERRAIN MATERIAL LAB", titleStyle);
            GUI.Label(new Rect(left, 58f * scale, width, 28f * scale),
                "NON-PLAYABLE ART DIAGNOSTIC · BASE + LANDFORM + EDGE", captionStyle);

            DrawChip(left, 110f * scale, width * .47f, "GRASS ON SOIL · AI ON", scale);
            DrawChip(left + width * .53f, 110f * scale, width * .47f,
                "SOIL ON GRASS · AI ON", scale);
            DrawChip(left, 314f * scale, width * .47f, "AI EDGE OFF BELOW", scale);
            DrawChip(left + width * .53f, 314f * scale, width * .47f,
                "AI EDGE OFF BELOW", scale);
            DrawChip(left, Screen.height - 92f * scale, width * .47f,
                "BASE ONLY: SOIL", scale);
            DrawChip(left + width * .53f, Screen.height - 92f * scale, width * .47f,
                "BASE ONLY: GRASS", scale);
        }

        private void DrawChip(float x, float y, float width, string text, float scale)
        {
            GUI.Label(new Rect(x, y, width, 28f * scale), text, chipStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(20f * Mathf.Min(Screen.width / 402f, Screen.height / 874f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.96f, 1f, .9f) },
            };
            captionStyle = new GUIStyle(titleStyle)
            {
                fontSize = Mathf.Max(10, titleStyle.fontSize / 2),
                normal = { textColor = new Color(.72f, .88f, .68f) },
            };
            chipStyle = new GUIStyle(captionStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            var background = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            background.SetPixel(0, 0, new Color(.05f, .12f, .13f, .8f));
            background.Apply(false, true);
            chipStyle.normal.background = background;
        }
    }
}
