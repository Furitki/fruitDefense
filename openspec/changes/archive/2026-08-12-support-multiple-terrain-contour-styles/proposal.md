## Why

The current terrain pipeline hard-wires one bilinear Dual-Grid silhouette per surface, so an isolated authored cell renders as a diamond-like organic patch even when the canonical battlefield is an explicit square gameplay grid. Its optional AI edge pass is then reduced to a 32-pixel contact ribbon, preventing the wide hand-painted grass lip, exposed earth, shading, and square rounded corners required by the accepted visual reference.

## What Changes

- Separate semantic surface identity, contour style, and edge treatment so square and organic terrain can coexist without changing gameplay meaning.
- Add an explicitly authored contour style for landform cells, with square as the bundled battlefield default and organic retained for reviewed decorative or laboratory regions.
- Reuse the existing NW=`1`, NE=`2`, SE=`4`, SW=`8` mask resolver and vertex projection while allowing each contour style to bind its own sixteen-mask landform and directed edge TileSets.
- Reject unsupported or incompatible style mixing instead of silently substituting another contour or edge asset.
- Replace the legacy edge sample with a high-resolution square-contour source, but package it as a narrow top-down material blend: retain irregular grass encroachment while excluding a directional soil wall, dark contact stroke, or second outer shadow contour.
- Extend authoring, validation, evidence, and portrait WebGL acceptance for one-cell squares, strips, turns, holes, disconnected diagonals, and square/organic coexistence.
- Migrate the three bundled battlefield presentations explicitly to square contours while preserving gameplay cells, routes, markers, fingerprints, snapshots, and deterministic outcomes.
- Promote the explicitly authorized A grass-on-soil and B stone-on-water full-composite families into versioned production TileSets, palette bindings, and one-click semantic brush presets, then package their accepted 256-pixel sources as 64-pixel runtime masks so the portrait/editor scale does not upscale 32-pixel art.
- Replace pair-specific production installation and editor shortcuts with one reusable brush-package descriptor and registry consumed by the importer, canonical map editor, and terrain laboratory.

## Capabilities

### New Capabilities
- `terrain-contour-style-authoring`: Defines contour-style identity, square/organic authoring choices, connected-region compatibility, high-resolution art ownership, and visual acceptance.

### Modified Capabilities
- `battlefield-layered-map-model`: Adds presentation-only contour style to visual cells while excluding it from gameplay identity and deterministic simulation.
- `layered-terrain-brush-authoring`: Allows a material to bind multiple contour-specific landform and directed edge TileSets and replaces the narrow AI contact-ribbon contract with a hand-painted transition contract.
- `battlefield-dual-grid-terrain-presentation`: Resolves and renders terrain by surface plus contour style using the existing mask/projection authority and validates all bundled square-contour maps.
- `layered-terrain-painter-workflow`: Exposes only available contour choices, prevents invalid connected-region mixing, and keeps image generation out of the painting interaction.

## Impact

- Presentation-only map fields and compiler validation under `Assets/Scripts/Core`.
- Terrain palette bindings, Dual-Grid mask grouping, and immediate-mode Battle rendering under `Assets/Scripts/Tilemaps` and `Assets/Scripts/FruitDefenseGame.cs`.
- Canonical map and terrain-laboratory editor controls, migration, focused smoke tests, and generated evidence under `Assets/Editor`.
- New square-contour landform and directed hand-painted edge assets under `Assets/LayeredTerrain`, with source prompts and provenance retained.
- Two production composite brush families under `Assets/LayeredTerrain/CompositeBrushes`, sourced byte-for-byte from the approved pipeline run and retaining the source manifests.
- A reusable pipeline-to-Unity brush import contract whose generated descriptor owns semantic registration and whose imported definition is the only authoring-menu source.
- Required validation surfaces remain `FruitDefense.Editor.ProjectSetup.SmokeValidate`, focused contour-style smoke checks, ordinary WebGL build, and real portrait canvas inspection.
- No combat, route, collision, placement, economy, progression, snapshot, backend, or mini-game adapter behavior changes.
