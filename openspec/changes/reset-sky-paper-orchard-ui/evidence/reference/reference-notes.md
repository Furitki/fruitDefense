# Sky-paper orchard reference notes

## Evidence identity

- Source: user-supplied image attached to the change request.
- Stored evidence: `sky-paper-orchard-reference.png`.
- Dimensions: 850×1850 pixels.
- SHA-256: `b744a656a2888e231624de1b511702769f971921a3fc04489b9fe2a348da58fd`.
- Production dependency: forbidden. The image is review evidence only.

## Authoritative Gate A interpretation

- Clear sky-blue edge outside a warm white paper page.
- Floating two-row paper Header with a dark soil-brown title, three individual
  raised resource capsules, and two cream-rimmed yellow pause/speed controls.
- One large rounded warm-paper page shell below the Header, containing one inset
  soil-brown gameplay stage and the complete lower control stack.
- Buttons use a visible outer cream rim, rounded colored face, upper highlight,
  soil outline, and short bottom shadow; a flat recolor is not equivalent.
- The tool area uses recipe-style mini cards; the nursery uses five dashed empty
  slots; the bottom refresh action is a thick full-width leaf-green button.
- A separate phase/Wave row below the stage: sunlight-yellow phase status and one
  leaf-green primary Wave action when the phase permits it.
- Light paper ContextTray and NurseryTray sections followed by one wide RefreshAction.
- Rounded, high-weight Chinese title/action typography paired with a clear reading face.
- Restrained fruit/leaf ornaments may touch protected visual corners but never own
  layout, hit geometry, copy, or safe-area resolution.

## Non-authoritative content

- All generated Chinese copy, numbers, gameplay entities, inventory contents,
  costs, and interaction states in the reference.
- Exact sampled colors or font identity. Relative component proportions and
  visible material anatomy are authoritative for Gate A, but final rectangles
  remain explicitly owned by `BattleUiLayout` rather than sampled at runtime.
- The reference's 850×1850 raster itself, any crop of it, and any traced full-page
  geometry.

Production copy remains owned by `RuntimeUiCopyCatalog`; gameplay content remains
owned by the battle catalog and renderer; exact tokens and rectangles remain owned by
the release theme, ArtSet metadata, `BattleUiLayout`, `PortraitShellLayout`, and
`BattlefieldProjection`.
