## Why

The current Lobby Home and Activity pages satisfy the functional loop but render as sparse, text-led utility panels that do not preserve the approved full-page reference hierarchy. This change restores the reference-led Sunny Orchard presentation for those two pages while keeping the already-validated reward, profile, level-selection, growth-preview, and battle-launch behavior unchanged.

## What Changes

- Recompose the shared Lobby chrome used by Home and Activity so the top bar, page host, and bottom navigation reproduce the approved paper-collage hierarchy at the 402×874 reference viewport and supported portrait/inset variants.
- Rebuild Home around three illustration-led selectable level cards, explicit selected state, a visual pre-battle growth summary, and one leaf-green Start primary action.
- Rebuild Activity around readable reward items, explicit claim state, a reward hero composition, and one leaf-green Claim primary action.
- Restore Home/Activity primary actions to the user-selected original light lime-green rounded-square fixed raster and keep the refined standalone bottom-navigation masters.
- Prohibit scripts, canvases, shaders, primitives, or procedural drawing APIs from authoring any visible production UI-art pixel; exporters may only validate, crop/extract existing alpha, pad, resize, measure, hash, and encode selected masters.
- Use only the existing production `RuntimeUiTheme`/`RuntimeUiArtSet` semantic bindings and existing authoritative `PortraitHubLayout` draw/hit rectangles; reference mockups remain review evidence rather than runtime screens.
- Preserve Growth page presentation, gameplay rules, reward quantities, profile persistence, content identities, battle-growth resolution, and route navigation.
- Validate the two rebuilt pages at all supported portrait sizes and representative inset safe areas, then capture real ordinary-WebGL `reference / before / after` evidence for user visual approval.
- For the Home rework requested after the first visual review, measure the approved reference and split only complete text-free component imagery into owned fixed raster masters. Replace the leaking Home image-frame stack and the blurred navigation family without using scripts to draw, reconstruct, recolor, or decorate visible pixels.

## Capabilities

### New Capabilities

- `outgame-home-activity-presentation`: Defines the reference-led visual anatomy, state presentation, geometry parity, and real-canvas acceptance contract for the Lobby Home and Activity pages.

### Modified Capabilities

- `runtime-ui-quality-standard`: Reconciles the finite production ArtSet contract with the three Hub navigation icons, one Activity reward illustration, and two dedicated text-free navigation chrome surfaces, for a total of 62 required slots.

## Impact

- Runtime presentation: `Assets/Scripts/Shell/LobbyHubPresenter.cs`, `Assets/Scripts/Shell/PortraitHubLayout.cs`, and shared Hub drawing helpers under `Assets/Scripts/UI/`.
- Production presentation assets and bindings under `Assets/UI/Art/` only where existing semantic slots can provide the approved reference treatment.
- Editor validation and WebGL acceptance coverage for Home and Activity visual anatomy, text containment, draw/hit parity, states, and portrait safe areas.
- No new package dependency, scene route, persistence schema, content identity, gameplay rule, or platform-support claim.
