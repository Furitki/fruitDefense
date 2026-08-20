## Why

The runtime UI currently passes rectangle-based alignment checks while rendered Chinese glyphs, icon alpha ink, and paired action surfaces remain visibly misaligned. The paused Battle modal exposes the systemic gap: equal hit rectangles do not produce equal visible buttons, and independently centered line boxes do not produce an optically centered composition.

## What Changes

- Replace safe-inset and line-box proxies with authoritative optical geometry for common icons, nine-slice action surfaces, and packaged Chinese typography.
- Make shared icon-and-label, indicator-and-message, title-ribbon, and paired-action anatomy center and size the rendered visual group rather than unrelated rectangles.
- Normalize the visible bounds of the active action-surface family without changing gameplay hit geometry or introducing per-screen nudges.
- Apply the corrected shared anatomy to Bootstrap, Lobby, Battle modals and controls, and Settlement.
- Extend Editor and live WebGL acceptance so visible alpha/glyph misalignment, unequal paired actions, and asset-specific padding differences fail deterministically.
- Keep gameplay, persistence, battle simulation, route navigation, and platform adapters unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `runtime-ui-quality-standard`: Require authoritative rendered-ink geometry and family-level visible-surface normalization instead of safe-inset or line-box proxies.
- `portrait-game-interface`: Require shared title, hint-row, icon-label, and paired-action anatomy to be optically aligned across all supported portrait geometries.
- `webgl-visual-acceptance`: Require canonical paused-modal and cross-route evidence to measure final rendered pixels, not only declared rectangles.

## Impact

Affected areas include `RuntimeUiArtSet`, `RuntimeUiGui`, typography/theme metrics, the Sunny Orchard Painted export/manifest pipeline, shared Shell/Battle layout helpers, Editor quality tests, and WebGL visual acceptance. Production art GUIDs and hit rectangles remain stable; no new runtime dependency is introduced.
