# Before baseline — 402×874 ordinary WebGL Battle

These files are copied byte-for-byte from the last canonical full-safe-area Battle
evidence produced by `implement-orchard-paper-battle-ui`. They are the review baseline
for this reset; they are not a claim that an unchanged build was recaptured after the
current dirty working tree began evolving.

## Runtime identity recorded by `acceptance.json`

- Theme: `ui.sunny-orchard@2`.
- ArtSet: `sunny-orchard-painted@1`.
- Theme asset: `Assets/UI/Theme/ReleaseRuntimeUiTheme.asset`.
- ArtSet asset: `Assets/UI/Art/Sets/SunnyOrchardPaintedRuntimeUiArtSet.asset`.
- Font path at that baseline: `Assets/Resources/Fonts/NotoSansSC-UI.ttf`.
- Font finding from the reset audit: the file still contains an `fvar` weight axis,
  defaults to weight 100, and identifies its default face as Noto Sans SC Thin;
  existing bold roles depend on synthesized `FontStyle.Bold`.

## Screenshots

| State | File | SHA-256 |
| --- | --- | --- |
| Ready | `01-ready.png` | `188d0e91a4a22937f94a0a5455e524dcafc3095f58901cc3c1bfed0213cbcf0d` |
| Active wave | `02-active-wave.png` | `7cdbffaa6c9f0ad878f4cc146423190c3dc43811449642067148364a4bc5a911` |
| Paused | `05-paused.png` | `2352cafa5247e6d1c4a87bc4815096de69c03b967b77ab5503a8bab5a2bc0a52` |
| Selected detail | `14-plant-detail.png` | `2bac50ac389477208cabdbb19ceada20e467b1350ad983eb6b1951d3357643ef` |

## Blocking visual defects carried into the reset

- The 486-point gameplay stage consumes about 56% of the design height and retains a
  62-point projection control strip, leaving visible empty mass above the grid.
- Phase/Wave presentation is embedded in the battlefield projection instead of owning
  the independent flow row required by the new reference.
- The edge background remains warm beige instead of creating the clear sky/paper split.
- Title and action roles use a thin variable-font default plus synthesized bold, so the
  final WebGL weight does not reliably express the rounded high-weight hierarchy.
- Header, stage, ContextTray, NurseryTray, and RefreshAction are structurally valid but
  visually compressed into similarly weighted pale rectangles.

The reset must close these defects without changing gameplay rules, command identity,
drag/drop semantics, or platform-support claims.
