using System;
using System.Globalization;
using UnityEngine;

namespace FruitDefense
{
    /// <summary>
    /// Resolves the runtime safe area for every portrait surface. Acceptance-only query
    /// overrides make inset behavior observable on desktop WebGL without changing release UI.
    /// </summary>
    public static class RuntimeSafeAreaResolver
    {
        private const float MinimumSafeAreaHeight = 1f;

        public static Rect ResolveCurrent()
        {
            return Resolve(Screen.safeArea, Screen.width, Screen.height, Application.absoluteURL);
        }

        public static Rect Resolve(Rect systemSafeArea, float screenWidth, float screenHeight,
            string absoluteUrl)
        {
            if (!TryGetQueryValue(absoluteUrl, "acceptance", out var acceptance)
                || !string.Equals(acceptance, "1", StringComparison.Ordinal))
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

        public static bool TryGetQueryValue(string absoluteUrl, string key, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrEmpty(absoluteUrl) || string.IsNullOrEmpty(key)) return false;

            var queryStart = absoluteUrl.IndexOf('?');
            if (queryStart < 0 || queryStart + 1 >= absoluteUrl.Length) return false;
            var fragmentStart = absoluteUrl.IndexOf('#', queryStart + 1);
            var queryLength = (fragmentStart >= 0 ? fragmentStart : absoluteUrl.Length)
                - queryStart - 1;
            var query = absoluteUrl.Substring(queryStart + 1, queryLength);
            foreach (var pair in query.Split('&'))
            {
                var separator = pair.IndexOf('=');
                var encodedName = separator >= 0 ? pair.Substring(0, separator) : pair;
                if (!string.Equals(Decode(encodedName), key, StringComparison.OrdinalIgnoreCase))
                    continue;

                var encodedValue = separator >= 0 ? pair.Substring(separator + 1) : string.Empty;
                value = Decode(encodedValue);
                return true;
            }

            return false;
        }

        private static bool TryGetInset(string absoluteUrl, string key, out float inset)
        {
            inset = 0f;
            if (!TryGetQueryValue(absoluteUrl, key, out var value)
                || !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out inset)
                || float.IsNaN(inset) || float.IsInfinity(inset))
            {
                inset = 0f;
                return false;
            }

            return true;
        }

        private static string Decode(string value)
        {
            try
            {
                return Uri.UnescapeDataString((value ?? string.Empty).Replace('+', ' '));
            }
            catch (UriFormatException)
            {
                return value ?? string.Empty;
            }
        }
    }
}
