## Why

The pixel Dual-Grid baker can already derive a validated sixteen-mask TileSet, but creating another terrain still requires manual profile setup and several PixelGrass-specific paths. Artists need one reusable editor workflow that accepts one or two existing images, or prepares a strict imagegen handoff when source art must be created by AI.

## What Changes

- Add a generic Unity editor wizard for creating named pixel terrain profiles, owned source/output folders, and generated TileSets.
- Support a true single-source mode and a separate grass-plus-soil mode for manually supplied opaque pixel textures.
- Add an AI-assisted mode that writes a machine-readable imagegen request and copyable Codex instruction, waits for imagegen-produced files at explicit paths, and never draws, synthesizes, or substitutes source art in editor code.
- Make profile baking configure arbitrary source importers, emit terrain-specific atlas/JSON evidence, and validate arbitrary generated profiles without overwriting PixelGrass evidence.
- Add one-click actions to bake, validate, and apply the generated TileSet to the currently selected `DualGridTilemap`.
- Extend aggregate editor smoke validation to every valid generated pixel terrain profile while keeping the existing PixelGrass sample compatible.
- Keep release scenes, runtime terrain rules, gameplay, persistence, colliders, navigation, and the high-resolution baker unchanged.

## Capabilities

### New Capabilities

- `generic-pixel-terrain-authoring`: Defines manual one/two-source setup, imagegen handoff, generic profile ownership, per-terrain evidence, multi-profile validation, and selected-map application from one editor wizard.

### Modified Capabilities

None.

## Impact

- Extends `DualGridPixelTerrainProfile` and `DualGridPixelTileSetGenerator` while preserving existing PixelGrass paths and generated assets.
- Adds an editor-only wizard and imagegen request artifact format under the Dual-Grid authoring area.
- Reuses `DualGridTileSet` and `DualGridTilemap`; no runtime public API, scene order, package, network, credential, platform adapter, or build dependency changes.
- Required acceptance remains reachable from `FruitDefense.Editor.ProjectSetup.SmokeValidate`; ordinary WebGL behavior is unaffected.
