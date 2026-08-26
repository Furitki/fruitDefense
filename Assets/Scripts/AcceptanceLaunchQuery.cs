#if FRUIT_DEFENSE_ACCEPTANCE || UNITY_EDITOR
using System;

namespace FruitDefense
{
    public static class AcceptanceLaunchQuery
    {
        public static bool IsEnabled(string absoluteUrl)
        {
            return TryGetFirstValue(
                    absoluteUrl, "acceptance", out var value)
                && string.Equals(value, "1", StringComparison.Ordinal);
        }

        public static bool TryGetFirstValue(
            string absoluteUrl,
            string key,
            out string value)
        {
            value = string.Empty;
            if (string.IsNullOrEmpty(absoluteUrl) || string.IsNullOrEmpty(key))
                return false;

            var queryStart = absoluteUrl.IndexOf('?');
            if (queryStart < 0 || queryStart + 1 >= absoluteUrl.Length)
                return false;
            var fragmentStart = absoluteUrl.IndexOf('#', queryStart + 1);
            var queryLength = (fragmentStart >= 0
                    ? fragmentStart
                    : absoluteUrl.Length)
                - queryStart - 1;
            var query = absoluteUrl.Substring(queryStart + 1, queryLength);
            foreach (var pair in query.Split('&'))
            {
                var separator = pair.IndexOf('=');
                var encodedName = separator >= 0
                    ? pair.Substring(0, separator)
                    : pair;
                if (!string.Equals(
                        Decode(encodedName), key, StringComparison.Ordinal))
                    continue;

                var encodedValue = separator >= 0
                    ? pair.Substring(separator + 1)
                    : string.Empty;
                value = Decode(encodedValue);
                return true;
            }

            return false;
        }

        private static string Decode(string value)
        {
            try
            {
                return Uri.UnescapeDataString(
                    (value ?? string.Empty).Replace('+', ' '));
            }
            catch (UriFormatException)
            {
                return value ?? string.Empty;
            }
        }
    }
}
#endif
