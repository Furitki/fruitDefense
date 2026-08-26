using UnityEngine;
#if FRUIT_DEFENSE_ACCEPTANCE
using System.Globalization;
#endif

namespace FruitDefense
{
    public static class RuntimeSafeAreaResolver
    {
        public static Rect ResolveCurrent()
        {
#if FRUIT_DEFENSE_ACCEPTANCE
            return AcceptanceSafeAreaDecorator.Resolve(
                Screen.safeArea, Screen.width, Screen.height, Application.absoluteURL);
#else
            return Screen.safeArea;
#endif
        }
    }

#if FRUIT_DEFENSE_ACCEPTANCE
    public static class AcceptanceSafeAreaDecorator
    {
        private const float MinimumSafeAreaHeight = 1f;

        public static Rect Resolve(Rect systemSafeArea, float screenWidth, float screenHeight,
            string absoluteUrl)
        {
            if (!AcceptanceLaunchQuery.IsEnabled(absoluteUrl))
            {
                return systemSafeArea;
            }

            var hasTop = TryGetInset(absoluteUrl, "safeTop", out var topInset);
            var hasBottom = TryGetInset(absoluteUrl, "safeBottom", out var bottomInset);
            if (!hasTop && !hasBottom) return systemSafeArea;

            var height = Mathf.Max(MinimumSafeAreaHeight, screenHeight);
            if (!hasTop) topInset = Mathf.Max(0f, height - systemSafeArea.yMax);
            if (!hasBottom) bottomInset = Mathf.Max(0f, systemSafeArea.yMin);

            topInset = Mathf.Clamp(topInset, 0f, height);
            bottomInset = Mathf.Clamp(bottomInset, 0f, height);
            var totalInset = topInset + bottomInset;
            var maximumTotalInset = Mathf.Max(0f, height - MinimumSafeAreaHeight);
            if (totalInset > maximumTotalInset && totalInset > 0f)
            {
                var scale = maximumTotalInset / totalInset;
                topInset *= scale;
                bottomInset *= scale;
            }

            return new Rect(0f, bottomInset, Mathf.Max(0f, screenWidth),
                height - topInset - bottomInset);
        }

        private static bool TryGetInset(string absoluteUrl, string key, out float inset)
        {
            inset = 0f;
            if (!AcceptanceLaunchQuery.TryGetFirstValue(absoluteUrl, key, out var value)
                || !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out inset)
                || float.IsNaN(inset) || float.IsInfinity(inset))
            {
                inset = 0f;
                return false;
            }

            return true;
        }
    }
#endif
}
