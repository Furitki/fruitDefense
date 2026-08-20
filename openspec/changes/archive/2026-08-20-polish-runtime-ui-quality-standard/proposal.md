## Why

The unified runtime UI now has a working interchangeable 49-slot art system, but the accepted build still scores only 50/100 in manual art review because typography alignment, text fit, color usage, optical icon alignment, component spacing, and route composition are not governed by one enforceable quality standard. This change turns those subjective defects into a repeatable specification, automated checks, a reviewed resource inventory, and a second layout/art polish pass against the approved Sunny Orchard reference board.

## What Changes

- Define a project-owned runtime UI quality standard for typography, baseline and optical alignment, wrapping/clipping, component bounds, spacing, color/contrast, icon canvases, nine-slice safety, illustration placement, and route hierarchy.
- Add deterministic Editor validation for text fit and overflow at 360×800, 375×812, 402×874, and 430×932 in full and representative inset safe areas.
- Add resource audits for semantic ownership, importer geometry, transparent edges, optical icon boxes, palette/contrast, unbound review assets, and duplicate or mixed-set references.
- Produce a scored current-state audit and fix every blocking or high-priority defect rather than documenting known failures as acceptable.
- Refine Bootstrap, Lobby, Battle, and Settlement composition against the approved Sunny Orchard style board and painted component proof while preserving route flow, gameplay behavior, and existing external hit geometry unless a separately justified layout contract change is required.
- Expand WebGL evidence so typography alignment, text overflow, color contrast, icon alignment, nine-slice integrity, and cross-route visual hierarchy are explicitly accepted rather than inferred from a general screenshot pass.
- Remove disposable inspection helpers and rejected resource experiments after evidence is captured.
- Make the ordinary-WebGL host scale the complete portrait canvas to fit both width and height on desktop and embedded browser windows instead of centering an oversized fixed canvas off-screen.
- Replace rectangle-only alignment claims with raster-aware visual-group checks for glyph ink, icon weight, combined icon-and-label centering, asymmetric gutters, and page-level visual balance.
- Recompose the visible Lobby, Battle, terminal, and Settlement silhouettes where the accepted geometry is still visibly top-heavy, off-center, or dominated by dead space.

Gameplay simulation, battle balance, persistence, content catalog behavior, route semantics, and platform adapters are non-goals.

## Capabilities

### New Capabilities

- `runtime-ui-quality-standard`: Defines enforceable typography, alignment, bounds, color, component, resource, and layout-quality requirements for the shared runtime UI system.

### Modified Capabilities

- `portrait-game-interface`: Strengthens portrait layout requirements with explicit typography alignment, overflow prevention, optical icon alignment, and reference-driven route hierarchy across all supported portrait sizes.
- `webgl-visual-acceptance`: Requires scored multi-size evidence for text fit, contrast, alignment, nine-slice integrity, semantic resource identity, and full cross-route UI states.

## Impact

- Runtime presentation: `Assets/Scripts/UI/`, `Assets/Scripts/App/`, `Assets/Scripts/Shell/`, and Battle presentation code in `Assets/Scripts/FruitDefenseGame.cs`.
- Layout contracts and validation: `Assets/Scripts/Presentation/`, `Assets/Editor/Tests/`, and `Assets/Editor/Tools/`.
- UI production assets and manifests: `Assets/UI/Art/`, `Assets/UI/Theme/`, and review-only reference boards.
- Acceptance tooling and evidence: `scripts/accept-webgl-portrait.ps1` and this change's `evidence/` directory.
- Stable UI guidance: `docs/ui/ui-visual-system.md`, linked rather than duplicated by the project README.
- Required gates: `FruitDefense.Editor.ProjectSetup.SmokeValidate`, `FruitDefense.Editor.P0ValidationSuite.Run`, `FruitDefense.Editor.WebBuild.Build`, and live ordinary-WebGL portrait capture. This does not authorize Douyin or WeChat support.
