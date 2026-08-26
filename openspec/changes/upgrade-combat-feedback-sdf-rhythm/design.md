## Context

Combat floating text currently reuses the packaged Noto Sans SC font through legacy IMGUI. Every label is drawn four times at diagonal offsets for the outline and once for the fill, then the whole bitmap result is continuously scaled for the rebound envelope. This creates discontinuous corners, sub-pixel breakup, and repeated draw work. The finite visible budget is already 12 total / 8 ordinary and the display strings are cached, so event volume is bounded before rendering.

The project does not currently enable the Unity UI package. Unity 6000.3 provides the production `com.unity.ugui` 2.0.0 core package, which includes TextMeshPro and its maintained SDF layout/shader path. The accepted composition remains the same 402-by-874 safe-area projection used by IMGUI, and ordinary WebGL remains the build baseline.

Current floating coordinates use an event point plus 24/48-pixel damage lanes and an additional 84-pixel defeat band. Camera motion is accumulated from as many as four active records, and even light/medium impacts request shake; each record uses a high-frequency 11-to-15-cycle sample. Those independent choices explain the observed distance and fragmented shake density.

## Goals / Non-Goals

**Goals:**

- Produce continuous, scalable fill and outline from one deterministic RGBA32 atlas baked offline from a transient SDF source.
- Retain finite semantic roles and bounded pools while removing the five-layer IMGUI floating-text path.
- Keep initial feedback close to the target/contact point, follow the live target for a short contact phase, then detach into compact lanes.
- Reserve camera shake for reviewed impact beats, collapse simultaneous requests into one non-additive motion, and use a low-cycle damped envelope.
- Preserve exact HUD, input, safe-area, gameplay, persistence, and platform boundaries.

**Non-Goals:**

- Migrating the rest of the immediate-mode runtime UI to UGUI or TextMeshPro.
- Adding dynamic glyph population, operating-system font fallback, arbitrary rich text, localization scope, or content-authored typography.
- Changing damage, targeting, skills, statuses, rewards, snapshots, checksums, RNG, or hit-test geometry.
- Claiming Douyin or WeChat support from ordinary WebGL acceptance.

## Decisions

### 1. Use one final-IMGUI-layer atlas compositor

Keep the floating-text compositor inside the battle's existing final `OnGUI` layer and draw the committed atlas with `GUI.DrawTextureWithTexCoords`. The renderer prepares fixed atlas rectangles and UVs before repaint, stores one contiguous command range and semantic color per visible label, then changes `GUI.color` once per label and submits its atlas primitives. This gives the overlay an explicit final order without a Canvas, camera, mesh, material, render texture, coroutine, player-loop hook, or per-label GameObject.

Each reference-space anchor is transformed through the same `BattlefieldProjection.CalculateViewportLayout(...)` result used by the rest of IMGUI, including safe-area translation and scale. Final visible bounds are clamped against the complete `BattleSurface`, not the grass-only `GridRect`. A fixed post-clamp collision search checks nine deterministic candidate offsets in stable event order, preserving at most eight reference pixels of horizontal anchor error while separating accepted bounds.

A screen-space TMP Canvas was rejected by real WebGL evidence because the later full-screen IMGUI pass covered it. Direct `Graphics.DrawMeshNow`, end-of-frame mesh submission, a transparent render-target composite, and a helper camera each introduced an unreliable layer boundary or unnecessary runtime subsystem. A static bitmap `Font`/`GUI.Label` prototype was also rejected: Unity 6000.3 converts it through a missing runtime `FontAsset` path and does not honor scalable `GUIStyle.fontSize`. The final atlas compositor keeps one rendering path and avoids all of those boundaries.

### 2. Bake one static RGBA atlas from the packaged font and reviewed inventory

An idempotent editor tool temporarily generates SDF glyph data from `Assets/Resources/Fonts/NotoSansSC-UI.ttf` with fixed sampling and padding, then resolves the approved solid face and thick continuous neutral outline into one committed 512-by-512 RGBA32 texture plus deterministic metadata. The temporary TMP font asset, source SDF texture, and materials are generation inputs only and are deleted before the production assets are saved. Runtime code cannot request characters, add pages, or create a font or material.

The left atlas region owns the finite reviewed character inventory. The remaining transparent region owns 124 finite composite tokens for the dense hot copy: signed one- and two-digit damage, `冻结`, `击败`, `击败×`, ` 阳光`, and signed resource digits. Runtime longest-prefix resolution is direct-indexed and bounded, so common mowing-style copy usually becomes one atlas primitive while longer reviewed numeric copy continues through the same character path. Fill pixels are neutral white and are tinted once per semantic label; the outline remains neutral, so roles do not create texture or material variants.

Dynamic TMP population was rejected because it recreates WebGL first-use and missing-glyph uncertainty. Separate semantic atlases and per-role materials were rejected because color is already expressed by the label draw range. The generator validates the source font, glyph and token inventory, RGBA format, packing boundary, owned paths, and exact zero-material runtime topology.

### 3. Keep 12 fixed record slots and 192 fixed atlas commands

The overlay owns exactly the existing total visible capacity. It maps active presentation records to 12 reusable slots by event sequence, resolves composite tokens and glyph metrics into 192 preallocated draw commands, and records at most 12 preallocated label ranges. Unused slots are released and reused; no combat-time GameObject, string construction, mesh container, font page, material, render texture, list growth, or dictionary lookup is permitted after warm-up.

The old fill/outline `GUIStyle` arrays, glyph-request loop, and five `GUI.Label` calls are deleted. There is one rendering path and no compatibility fallback to legacy labels or bitmap fonts. A missing or invalid production atlas fails project validation and runtime initialization explicitly.

### 4. Use contact-first anchoring, short follow, then compact detachment

The event position remains the semantic source of truth. For a live target, the view resolves its current interpolated visual position during the first 0.12 seconds of local presentation time; the label then uses the last resolved anchor and its analytic rise. A missing or defeated target always uses the event position, so presentation cannot retain or recreate gameplay entities. Same-tick defeat aggregation uses the arithmetic centroid of all contributing event positions and clears the single-target identity instead of pairing the first target id with the last position.

At the 402-by-874 reference composition, ordinary lanes become 0, 14, and 28 vertical pixels with at most 8 horizontal pixels of deterministic separation. The defeat band becomes 26 pixels rather than 84. The initial lane-zero label center stays approximately one half enemy-height above the contact point, and all coordinates are clamped only after target anchoring and compact lane selection. Upper-edge inversion still unfolds toward the battlefield interior.

Alternative considered: follow the target for the complete label lifetime. Rejected because text would look attached to a moving health plate instead of recording an impact. Alternative considered: keep the dedicated 84-pixel defeat band. Rejected because it visually disconnects terminal copy from the defeated enemy.

### 5. Replace additive shake records with one global impact-beat scheduler

Profiles select a finite impact-beat role instead of arbitrary shake amplitude/duration. Ordinary damage, periodic damage, resource gain, status application, and ordinary single-enemy defeat do not request camera shake. Reviewed heavy impacts request a Heavy beat; a compact same-window defeat cluster may request one Cluster beat; boss defeat requests a Terminal beat.

The scheduler keeps at most one active beat. Requests inside the global real-time cooldown are discarded unless their semantic priority is strictly higher, in which case the active beat is replaced rather than added. The offset uses a deterministic direction and a damped analytic envelope with no more than roughly two visible oscillations; amplitude is clamped by the role catalog. Battlefield flash may accompany an accepted beat but cannot create a second shake path.

The scheduler uses pause-aware unscaled presentation time rather than logic time, so 2x event production cannot double real-time shake density. It remains presentation-only and is excluded from snapshots and checksums.

Alternative considered: keep per-profile rate limits and merely reduce amplitudes. Rejected because independent profile keys still interleave and additive records still create unstable peaks. Alternative considered: remove camera shake. Rejected because sparse, semantically aligned beats are a valuable impact cue.

### 6. Accept quality and performance from the final WebGL raster

Editor checks cover atlas ownership/settings/glyph and token coverage, absence of legacy outline calls, pool/command/material limits, deterministic collision placement, coordinate envelopes, follow cutoff, scheduler admission/replacement, pause/2x density, and non-additive amplitude. Aggregate smoke and the ordinary WebGL build remain required.

Real 402-by-874 WebGL evidence must include normal/heavy/control/resource/defeat text over grass and route, a rebound sequence, clustered targets, dense 1x/2x combat, and boss/cluster shake beats. The capture must show continuous outline, correct glyphs, target proximity, restrained shake density, unchanged HUD/input geometry, one unchanged atlas page, zero runtime materials, no steady-state allocation regression, and floating-text p95 at or below the existing 0.5-millisecond budget on the reference environment. The CPU measurement includes command preparation, collision placement, and all final-layer `GUI.DrawTextureWithTexCoords` submissions; GPU raster time remains excluded.

## Risks / Trade-offs

- [Risk] Atlas coordinates drift from the IMGUI safe-area transform → Mitigation: both paths consume the same viewport-layout result, final bounds are exported in telemetry, and Editor tests compare transformed reference anchors at multiple portrait sizes.
- [Risk] Baked outline becomes fragmented or tinted into the scene → Mitigation: the generator owns fixed face/outline thresholds, one neutral outline, transparent-region validation, and final-raster grass/route review through the full rebound envelope.
- [Risk] Per-primitive IMGUI submission dominates the small texture raster cost → Mitigation: 12 fixed slots, 192 fixed commands, 124 direct-indexed hot-copy tokens, at most one color change per label, and WebGL profiling that includes preparation, collision placement, and final atlas draws.
- [Risk] Static inventory misses future copy → Mitigation: one reviewed inventory constant, deterministic generator, build-time glyph coverage failure, and no runtime fallback.
- [Risk] Very dense target motion creates a small snap at detach → Mitigation: short follow window, compact lanes, and final-raster review; detachment remains presentation-only.
- [Risk] Removing common-hit shake makes early combat feel quieter → Mitigation: retain local recoil, squash, flash, VFX, and floating rebound while reserving camera motion for heavy/cluster/terminal beats.

## Migration Plan

1. Use the resolved official TMP editor dependency to add the deterministic transient-SDF-to-RGBA atlas generator plus committed atlas and metadata output.
2. Add the fixed-slot final-IMGUI-layer atlas compositor, transform reference anchors through the existing safe-area viewport, prepare fixed label ranges and draw commands, then delete the legacy five-layer label and all rejected Canvas, mesh, camera, render-target, player-loop, and bitmap-font paths.
3. Reduce lane/defeat distances and add the short target-follow resolver.
4. Replace raw/additive shake routing with the finite global impact-beat scheduler and migrate bundled profiles.
5. Add focused tests, run aggregate smoke, build ordinary WebGL, and capture real portrait quality/performance evidence.

Rollback is a source-and-asset revert of this presentation-only change. No save, snapshot, or player-data migration is needed.

## Open Questions

None. The finite glyph inventory and the user-approved SDF/impact-rhythm principles are sufficient for the first implementation.
