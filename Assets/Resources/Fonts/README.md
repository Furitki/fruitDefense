# Fruit Defense UI role fonts

The release UI packages two static, project-specific role fonts:

- `Assets/Resources/Fonts/NotoSansSC-Reading-400.ttf`: Regular 400 for body,
  metric, and supplemental roles; 153,644 bytes; SHA-256
  `80f96e594ca0803386487d2d27ca45184e7807baeb6b02731b9a2f03ead12cdd`.
- `Assets/Resources/Fonts/FruitDefense-OrchardDisplay-400.ttf`: a renamed,
  static subset derived from Smiley Sans Oblique 2.0.1 for display,
  screen-title, section-title, and control-label roles; 99,196 bytes; SHA-256
  `6b5f7097630a9236b33b38c365cecbd8bc64062acadf9eac907c09d10f0d2ee9`.

Both assets are truly static TTFs: the reading face's variable `wght` axis is
pinned before subsetting, both omit `fvar`, and `OS/2.usWeightClass` is 400.
Runtime `GUIStyle.fontStyle` remains `Normal`; no synthetic bold or host-font
fallback is used.

## Source and license

- Reading source: Noto Sans SC at Google Fonts commit
  `2894aab31764f10f29c421bdfd2340d3b382d384`; 17,772,300-byte variable TTF;
  SHA-256 `a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da`.
- Display source: Smiley Sans Oblique v2.0.1, official release commit
  `67e3821`; 5,781,344-byte release archive; archive SHA-256
  `299c0be6c960ae37361762eca76f7d0cd516615435bb96c0d4b98a1e70178a07`;
  2,629,764-byte source TTF; source SHA-256
  `b447d7e781f08bc95c4c9f23ba71ed2b8ebb639aa7184485c71c4ca5afcd25c4`.
- Both sources are licensed under SIL Open Font License 1.1. Repository copies:
  `Assets/Resources/Fonts/OFL-NotoSansSC.txt` and
  `Assets/Resources/Fonts/OFL-SmileySans.txt`.
- Because Smiley/得意黑 are Reserved Font Names, the modified display subset is
  deterministically renamed to `Fruit Defense Orchard Display` in its name table.

## Deterministic rebuild

Run `scripts/rebuild-ui-font.ps1`. It uses fontTools 4.63.0, verifies both pinned
sources, reads the one finite glyph authority from
`RuntimeUiChineseGlyphCoverage.RequiredGlyphs`, adds printable ASCII, creates a
static reading instance, subsets both faces, and renames the display family with
timestamp recalculation disabled. Two consecutive rebuilds on 2026-08-26
produced the identical hashes recorded above.
