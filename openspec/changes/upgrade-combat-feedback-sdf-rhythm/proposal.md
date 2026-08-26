## Why

The current floating-text renderer simulates an outline with four offset IMGUI labels, so rebound scaling produces fragmented edges and unnecessary repeated draw work. Runtime review also found that damage/defeat labels detach too far from their source targets and that camera shake is triggered by too many ordinary events, flattening the intended impact hierarchy.

## What Changes

- Replace the five-layer IMGUI floating-text draw path with one deterministic 512-by-512 RGBA32 atlas baked offline from a transient SDF source generated from the packaged Noto Sans SC font and a finite reviewed glyph inventory.
- Bake solid fill and a continuous neutral outline into reviewed glyphs plus finite hot-copy composite tokens, then render them in the final IMGUI layer without runtime font assets, materials, meshes, cameras, canvases, or render targets.
- Anchor damage and defeat feedback to a target-relative contact region, briefly follow the target, then detach into bounded semantic lanes rather than starting from a distant fixed offset.
- Route camera shake through a global impact-beat scheduler so ordinary repeated hits do not shake the camera and high-value beats remain readable under dense combat.
- Add Editor validation plus real 402-by-874 WebGL visual and performance acceptance for outline continuity, glyph coverage, anchor distance, shake cadence, batching, and allocation behavior.
- Keep gameplay damage, targeting, rewards, snapshots, checksums, RNG, hit-test geometry, and platform authorization unchanged.

## Capabilities

### New Capabilities

- `sdf-combat-feedback-rendering`: Defines deterministic editor-time SDF-derived atlas generation, finite glyph and composite-token coverage, continuous baked outline quality, bounded final-layer atlas drawing, and WebGL performance requirements for floating text.
- `combat-feedback-impact-rhythm`: Defines target-relative floating-text anchoring and the global camera-shake beat scheduler used to preserve impact hierarchy under dense combat.

### Modified Capabilities

None.

## Impact

- Floating-text drawing, coordinate conversion, and camera feedback routing under `Assets/Scripts/` and `Assets/Scripts/Presentation/`.
- Deterministic font-atlas generation and validation under `Assets/Editor/Tools/` and `Assets/Editor/Tests/`.
- One generated, reviewable RGBA32 atlas plus deterministic metadata sourced only from the packaged Noto Sans SC font; the transient SDF font asset is editor-only generation input.
- Aggregate Editor smoke coverage, ordinary WebGL build output, and real portrait WebGL acceptance evidence.
- No gameplay, persistence, content-catalog, or mini-game platform behavior changes.
