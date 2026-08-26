using System;
using UnityEngine;

namespace FruitDefense.Presentation
{
    [Serializable]
    public struct CombatFloatingTextGlyph
    {
        public int CodePoint;
        public RectInt AtlasRect;
        public float Width;
        public float Height;
        public float HorizontalBearingX;
        public float HorizontalBearingY;
        public float HorizontalAdvance;
        public float Scale;
        public float Padding;
    }

    [Serializable]
    public struct CombatFloatingTextCompositeToken
    {
        public string Text;
        public RectInt AtlasRect;
        public float BaseScale;
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
        public float HorizontalAdvance;
    }

    public sealed class CombatFloatingTextAtlasMetadata : ScriptableObject
    {
        public const int MaximumCompositeTokenTableLookupsPerSegment = 2;

        [SerializeField] private float _facePointSize;
        [SerializeField] private float _ascentLine;
        [SerializeField] private float _descentLine;
        [SerializeField] private string _glyphInventory = string.Empty;
        [SerializeField] private CombatFloatingTextGlyph[] _glyphs =
            Array.Empty<CombatFloatingTextGlyph>();
        [SerializeField] private RectInt _compositeRegion;
        [SerializeField] private float _compositeBasePointSize;
        [SerializeField] private CombatFloatingTextCompositeToken[] _compositeTokens =
            Array.Empty<CombatFloatingTextCompositeToken>();

        public float FacePointSize { get { return _facePointSize; } }
        public float AscentLine { get { return _ascentLine; } }
        public float DescentLine { get { return _descentLine; } }
        public string GlyphInventory { get { return _glyphInventory; } }
        public int GlyphCount { get { return _glyphs == null ? 0 : _glyphs.Length; } }
        public RectInt CompositeRegion { get { return _compositeRegion; } }
        public float CompositeBasePointSize { get { return _compositeBasePointSize; } }
        public int CompositeTokenCount
        {
            get { return _compositeTokens == null ? 0 : _compositeTokens.Length; }
        }

        public bool TryGetGlyph(char character, out CombatFloatingTextGlyph glyph)
        {
            if (_glyphs != null)
            {
                for (var index = 0; index < _glyphs.Length; index++)
                {
                    if (_glyphs[index].CodePoint != character) continue;
                    glyph = _glyphs[index];
                    return true;
                }
            }
            glyph = default;
            return false;
        }

        public bool TryGetCompositeToken(string text,
            out CombatFloatingTextCompositeToken token)
        {
            if (_compositeTokens != null)
            {
                for (var index = 0; index < _compositeTokens.Length; index++)
                {
                    if (!string.Equals(_compositeTokens[index].Text, text,
                            StringComparison.Ordinal)) continue;
                    token = _compositeTokens[index];
                    return true;
                }
            }
            token = default;
            return false;
        }

        public bool TryGetLongestCompositeToken(string text, int startIndex,
            out CombatFloatingTextCompositeToken token)
        {
            token = default;
            if (string.IsNullOrEmpty(text) || startIndex < 0
                || startIndex >= text.Length || _compositeTokens == null)
                return false;

            var first = text[startIndex];
            if (first == '-' && startIndex + 1 < text.Length)
            {
                var firstDigit = text[startIndex + 1] - '0';
                if (firstDigit >= 0 && firstDigit <= 9)
                {
                    if (startIndex + 2 < text.Length)
                    {
                        var secondDigit = text[startIndex + 2] - '0';
                        if (secondDigit >= 0 && secondDigit <= 9
                            && TryGetCompositeTokenAt(
                                10 + firstDigit * 10 + secondDigit,
                                text, startIndex, 3, out token))
                            return true;
                    }
                    return TryGetCompositeTokenAt(
                        firstDigit, text, startIndex, 2, out token);
                }
            }
            if (first == '+' && startIndex + 1 < text.Length)
            {
                var digit = text[startIndex + 1] - '0';
                return digit >= 0 && digit <= 9
                    && TryGetCompositeTokenAt(
                        114 + digit, text, startIndex, 2, out token);
            }
            if (first == '冻')
                return TryGetCompositeTokenAt(
                    110, text, startIndex, 2, out token);
            if (first == '击')
            {
                if (TryGetCompositeTokenAt(
                        112, text, startIndex, 3, out token))
                    return true;
                return TryGetCompositeTokenAt(
                    111, text, startIndex, 2, out token);
            }
            return first == ' '
                && TryGetCompositeTokenAt(
                    113, text, startIndex, 3, out token);
        }

        private bool TryGetCompositeTokenAt(int tokenIndex,
            string text, int startIndex, int length,
            out CombatFloatingTextCompositeToken token)
        {
            token = default;
            if (tokenIndex < 0 || tokenIndex >= CompositeTokenCount) return false;
            var candidate = _compositeTokens[tokenIndex];
            var candidateText = candidate.Text;
            if (string.IsNullOrEmpty(candidateText)
                || candidateText.Length != length
                || startIndex + length > text.Length)
                return false;
            for (var index = 0; index < length; index++)
                if (candidateText[index] != text[startIndex + index]) return false;
            token = candidate;
            return true;
        }

#if UNITY_EDITOR
        public void Configure(float facePointSize, float ascentLine,
            float descentLine, string glyphInventory,
            CombatFloatingTextGlyph[] glyphs, RectInt compositeRegion,
            float compositeBasePointSize,
            CombatFloatingTextCompositeToken[] compositeTokens)
        {
            _facePointSize = facePointSize;
            _ascentLine = ascentLine;
            _descentLine = descentLine;
            _glyphInventory = glyphInventory ?? string.Empty;
            _glyphs = glyphs ?? Array.Empty<CombatFloatingTextGlyph>();
            _compositeRegion = compositeRegion;
            _compositeBasePointSize = compositeBasePointSize;
            _compositeTokens = compositeTokens
                ?? Array.Empty<CombatFloatingTextCompositeToken>();
        }
#endif
    }
}
