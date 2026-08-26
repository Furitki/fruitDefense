## ADDED Requirements

### Requirement: Deterministic static SDF-derived atlas
The release SHALL render combat floating text from one committed 512-by-512 RGBA32 atlas plus metadata generated deterministically from a transient editor-time SDF source, the packaged Noto Sans SC font, and the reviewed finite glyph and composite-token inventories. No runtime font asset SHALL exist, and runtime SHALL NOT add atlas pages, request characters, create materials, or use an operating-system fallback.

#### Scenario: Production atlas is regenerated
- **WHEN** the stable editor generator runs twice without changing the source font, inventory, or fixed generation settings
- **THEN** the resulting production assets expose the same glyph and token coverage, atlas rectangles, RGBA32 dimensions, zero-material topology, and owned asset paths
- **AND** every damage, resource, control, multiplier, and defeat character and every reviewed hot-copy token is present before a WebGL build begins

#### Scenario: Runtime requests an unknown glyph
- **WHEN** floating copy contains a character outside the reviewed inventory
- **THEN** project validation fails with the missing code point or production token
- **AND** the runtime does not grow the atlas, create a second renderer, or fall back to another font

### Requirement: Single-path continuous outline rendering
Each atlas primitive SHALL contain its solid face and continuous neutral outline from the deterministic offline SDF bake, and each floating label SHALL be drawn through the one final-IMGUI-layer atlas path. Runtime SHALL NOT use offset duplicate labels, a second outline pass, or a compatibility font renderer to simulate a stroke. Rebound scaling from the smallest approved entry scale through the largest approved peak scale SHALL preserve a visually continuous contour at the 402-by-874 reference composition.

#### Scenario: Heavy and ordinary labels rebound
- **WHEN** ordinary and heavy labels are captured through entry, peak, rebound, and hold over grass and route surfaces
- **THEN** their outline remains continuous around cardinal and diagonal glyph edges
- **AND** no four-corner ghost copies or missing outline segments are visible

### Requirement: Bounded pooled text overlay
The floating-text renderer SHALL own no more than 12 reusable record slots, one RGBA32 atlas page, zero runtime materials, 192 preallocated atlas draw commands, and 12 preallocated label ranges. It SHALL update these arrays without combat-time GameObject, font-page, mesh-container, material, render-target, collection-growth, or yield-instruction creation after warm-up. It SHALL submit the prepared commands through `GUI.DrawTextureWithTexCoords` in the battle's final IMGUI layer and SHALL NOT use a Canvas, camera, mesh, per-label renderer object, automatic layout component, intermediate render-target composite, player-loop hook, bitmap-font fallback, or separate outline path.

#### Scenario: Mowing-style burst is rendered
- **WHEN** hundreds of eligible events are consumed while the existing density channel admits its maximum visible records
- **THEN** the overlay prepares no more than 12 pooled records and 192 atlas primitives, while reviewed hot copy resolves through bounded direct-indexed longest-prefix composite tokens
- **AND** atlas page count remains one and shared-material count remains zero
- **AND** steady-state rendering stays within the accepted allocation budget

### Requirement: Shared safe-area projection
Atlas floating text SHALL transform the same 402-by-874 reference-space anchors through the same safe-area viewport layout used by the IMGUI battlefield. Introducing the overlay SHALL NOT move the battlefield, HUD, pointer hit regions, drag geometry, or modal layout.

#### Scenario: Portrait viewport changes
- **WHEN** the battle is shown at the reference portrait canvas and at another supported portrait size with a safe-area inset
- **THEN** the atlas label and its source target preserve the same reference-space relationship
- **AND** final visible bounds clamp against the complete battle surface rather than the grass-only grid while preserving the bounded horizontal contact distance
- **AND** existing input coordinates continue to select the same authoritative objects

### Requirement: Real WebGL atlas acceptance
The final atlas path SHALL be accepted from the ordinary WebGL build at 402 by 874. At the existing visible-density budget, command preparation, deterministic collision placement, and all final-IMGUI-layer atlas draw CPU submissions together SHALL stay at or below 0.5 milliseconds p95 on the reference acceptance environment and SHALL allocate no more than 1 kilobyte per second after warm-up. GPU raster time is outside this CPU measurement and final pixels remain subject to visual acceptance.

#### Scenario: Final raster and profiler evidence are captured
- **WHEN** normal, heavy, periodic, resource, control, and defeat feedback are exercised over grass and route at 1x, 2x, and dense combat
- **THEN** all reviewed glyphs, continuous outlines, semantic colors, rebound scales, and density fallbacks are visible
- **AND** after at least 120 warm-up frames, at least 600 sampled frames meet the frame-time and allocation thresholds with one atlas page, zero materials, no dynamic atlas growth, and unchanged HUD/input geometry
