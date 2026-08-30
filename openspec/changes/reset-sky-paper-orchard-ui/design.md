## Context

The release currently renders one approved Sunny Orchard theme through a shared immediate-mode component layer, a single release `RuntimeUiTheme`, a complete 56-slot `RuntimeUiArtSet`, `PortraitShellLayout` for shell routes, and `BattleUiLayout` plus one `BattlefieldProjection` for Battle drawing and input. The current 402×874 Battle allocates 486 logical points to the stage and embeds phase/Wave controls in the battlefield projection, leaving the lower control stack denser and visually farther from the supplied sky-paper reference.

The supplied full-page image establishes hierarchy and visual direction only: blue edge background, warm white paper page, soil-brown gameplay stage, leaf-green primary action, sunlight-yellow phase emphasis, rounded high-weight headings, and restrained fruit/leaf decoration. It contains generated copy and illustrative gameplay content, so it cannot own production text, exact geometry, gameplay art, or runtime dependencies. The stable baseline remains `docs/ui/ui-visual-system.md` plus the current affected capability specs until this change is implemented, accepted, and synchronized.

This is a cross-route presentation reset with one deliberate topology change: phase/Wave UI moves out of the stage into an independent flow row. Stakeholders are UI/art authors, runtime presentation maintainers, acceptance tooling owners, and players using ordinary WebGL full or inset portrait canvases.

## Goals / Non-Goals

**Goals:**

- Deliver a coherent sky-paper-orchard release treatment across `Bootstrap → Lobby → Battle → Settlement`, with Battle approved first as a 402×874 vertical slice.
- Reduce Battle stage dominance to the reference's approximate 38–43% vertical band and give phase/Wave, context, nursery, and refresh controls stable independent owners without reducing required readability or interaction safety.
- Preserve one authoritative layout/projection path for rendering, clicking, dragging, target highlights, and acceptance coordinates.
- Bind every typography role to a packaged static Chinese font asset, using a rounded high-weight face for display/title/action roles and a clear reading face for body/metric/supplemental roles.
- Replace relevant UI art in place through owned text-free masters, deterministic export, stable destination GUIDs, complete semantic bindings, and exact importer/manifest validation.
- Prove ready, active, paused, and selected-detail Battle states before applying the treatment to the other routes, then pass Unity validation and real ordinary-WebGL full/inset acceptance.

**Non-Goals:**

- Changing combat rules, values, level/content identity, route legality, launch/result contracts, persistence, snapshots, session identity, or input meaning.
- Redrawing terrain, plants, flowerpots on the map, enemies, weapons, or combat effects as part of the UI ArtSet.
- Migrating from immediate-mode GUI to uGUI/UI Toolkit or introducing a runtime theme selector, remote assets, platform-specific skins, compatibility paths, or visual fallbacks.
- Claiming Douyin or WeChat conversion readiness from ordinary WebGL evidence.
- Shipping, cropping, tracing, or transcribing the supplied concept image as production UI.

## Decisions

### 1. Treat the reference as a component-anatomy and proportion contract, not a shippable raster

The implementation will translate both the image's hierarchy and its visible
component construction into project-owned semantic roles. At the 402×874 Gate A
viewport, `BattleUiLayout` will preserve the reference's relative page bands and
nested composition: floating two-row Header, three resource capsules, two
cream-rimmed yellow compact controls, one large warm-paper page shell, inset
soil stage, paired phase/Wave blocks, paper section cards, recipe tiles, dashed
nursery slots, and a thick leaf-green bottom refresh action. These proportions
are allowed to drive the authoritative layout; they are not inferred at runtime
from image pixels.

Copy, gameplay entities, values, and exact production geometry still come from
project-owned data, `BattleUiLayout`, theme assets, and ArtSet metadata. The
reference image itself is never cropped or shipped. Production surfaces are
text-free, independently owned nine-slice masters that reproduce the approved
rim, highlight, outline, and shadow anatomy.

For action surfaces, the visible material pixels originate from a project-owned
ImageGen component sheet created with the supplied image as style reference.
The selected sheet is retained as generation provenance; a deterministic tool
may only locate/crop isolated components, clean the exterior alpha, add
transparent padding, resize, measure, hash, and export. It may not draw or
reconstruct the rim, face, outline, highlight, shadow, texture, or decoration.
Determinism therefore begins at the fixed reviewed generated output rather than
at prompt regeneration. Runtime text and icons remain separate from the raster.

This keeps generated glyph errors, baked text, and gameplay-art mismatch out of
the build while preventing a second generic recolor from being mistaken for
reference fidelity. Directly slicing the image remains rejected because it would
combine text, layout, and illustration ownership in one untestable raster.

### 2. Approve one Battle vertical slice before route-wide rollout

The first implementation gate is the canonical 402×874 Battle in four states: ready, active, paused, and selected detail. The reference viewport will use this track order:

1. compact Header containing title, three metrics, pause, and speed;
2. one gameplay stage containing only the authoritative battlefield projection and stage-local gameplay feedback;
3. one persistent phase/Wave flow row;
4. ContextTray tools or the mutually exclusive selected-plant detail;
5. NurseryTray;
6. bottom RefreshAction.

At 402×874, the stage targets roughly 38–43% of resolved safe-content height; Header, phase row, context, nursery, refresh, outer insets, and four-point gaps consume the remainder. Exact logical rectangles are finalized in `BattleUiLayout` only after packaged-font measurement and battlefield projection checks. If a percentage and minimum readable/interactive geometry conflict, minimum geometry wins and the reference rhythm is recovered by reducing decorative/padding mass, not by shrinking text, targets, or map entities below their accepted bounds.

After the four-state slice passes editor raster review and real WebGL interaction capture, shared theme, fonts, and components propagate to Bootstrap, Lobby, and Settlement through `PortraitShellLayout`. Building all routes simultaneously was rejected because it would multiply art/layout rework before the densest route is proven.

### 3. Move phase/Wave into one independent, persistent flow-row owner

`BattleUiLayout` will own a `PhaseWaveRow` and its child rectangles. The left side presents the current phase/status with sunlight emphasis; the right side hosts the phase-specific Wave command when one is available. Ready shows `开始波次`; between-wave countdown shows `立即开始下一波`; active shows non-action wave/enemy progress in the same row; terminal state exposes no Wave command. Pause and speed remain quiet Header controls.

The old `BattlefieldProjection.WaveActionRect`/control-strip presentation path and any paired compatibility draw/hit path will be removed. Commands, labels, visibility rules, pause/restart effects, and session state remain unchanged. A persistent owner avoids lower-stack movement between phases, while removing the in-stage path prevents two action authorities.

Keeping the action inside the stage was rejected because it conflicts with the approved reference hierarchy and leaves phase progression visually entangled with map projection. Adding a second mirrored action was rejected because it would duplicate command and hit semantics.

### 4. Keep all Battle geometry under one immutable layout and projection graph

`BattleUiLayout` remains the sole logical-space owner for Header, stage, phase/Wave row, context/detail, nursery, refresh, modals, and their children. It constructs the one `BattlefieldProjection` used by renderer, hit tests, drag sources/destinations, range overlays, and acceptance coordinates. Style and motion may alter only visual rectangles; transparent padding never changes interaction rectangles.

The stage becomes shorter by changing its authoritative owner and regenerating its projection, not by post-scaling a rendered board. Tests will assert track order, non-overlap, four-point rhythm, safe-area containment, minimum 44-point action targets, projection round trips, and draw/hit identity across 360×800, 375×812, 402×874, and 430×932 full and representative inset cases.

A separate reference-layout layer or screen-specific pointer correction was rejected because it would reintroduce geometry drift.

### 5. Put the font reference on each semantic typography role

`RuntimeUiTypographyStyle` will own its packaged `Font` reference together with font size, line height, style, and optical offset. The release theme will serialize every role explicitly: display, screen title, section title, and control label resolve to the approved rounded high-weight Chinese face; body, metric, and supplemental resolve to the approved reading face, with intentional role exceptions allowed only in the theme asset. The legacy single `packagedChineseFont` field and synthesized-style assumption will be removed in the same change without migration or fallback.

Both font files must be project assets with recorded licenses, required Chinese and punctuation coverage, deterministic import settings, and successful WebGL glyph proof. Shared text measurement and `GUIStyle` caching resolve the same role font used for drawing; no Presenter chooses fonts or applies private offsets.

Keeping one face plus `FontStyle.Bold` was rejected because it cannot establish the reference's distinct rounded heading/action voice. System font lookup and chained fallback were rejected because WebGL output must be self-contained and deterministic.

### 6. Replace the single active ArtSet in place and retain the finite semantic schema

The existing 56 semantic slots already cover screen/safe surfaces, panels, stage, actions, slots, state indicators, resource/control icons, the corner ornament, ribbons, frames, and route illustrations. This change therefore keeps the finite slot schema and one active ArtSet; it does not introduce route-specific texture references or a second skin.

Relevant reviewed source masters will be replaced in their owned source tree and re-exported to the existing runtime destinations. Exports remain text-free standalone PNG/Sprite assets; nine-slice borders, safe insets, optical insets, source/runtime hashes, prompt/license records, and importer metadata remain manifest-owned. Existing destination `.meta` files and GUIDs are preserved, the ArtSet/theme revision is incremented, and obsolete superseded masters/exports are deleted. There is no old/new set switch, inherited slot, placeholder, or runtime fallback.

The six action surfaces (`primary`, `secondary`, `quiet`, `danger`, compact
normal, and compact active) use one reviewed ImageGen material sheet as their
only pixel source. The former procedural `ReferenceMaterialStyle` production
recipes for those semantic slots are removed, not retained as a fallback. Other
finite slots keep their current owned source until separately approved for
replacement.

Creating a parallel compatibility ArtSet was rejected because only one release treatment is required and the project explicitly removes obsolete paths. Adding new decoration slots was rejected because `ornament.screen-corner` and existing illustration roles can express the restrained motifs without expanding the runtime contract.

### 7. Preserve action semantics while changing visual composition

Start Wave remains `Primary + icon/text + momentary command`; Refresh remains `Secondary + icon/text + momentary command`; Pause/Continue and Speed remain quiet persistent modes; Retry and Return preserve their existing danger/secondary hierarchy. The reset changes container art, typography, spacing, and placement only. Normal, focused, pressed, disabled, loading, selected, success, warning, error, and drag states continue to resolve through shared components with non-color cues.

Presenters continue to supply copy, state, and commands but not texture paths, fonts, colors, or page-local style rules. This prevents the route rollout from becoming four independent themes.

### 8. Use two acceptance gates and one release payload

Gate A validates the Battle slice with editor layout/typography/art checks and real ordinary-WebGL 402×874 full and representative inset captures for ready, active, paused, and selected detail. It records visible text bounds, structural-weight bands, phase/Wave row state, lower closeout, pointer targets, projection identity, theme/art/font identity, and absence of the old in-stage Wave path.

Gate B validates Bootstrap, Lobby, Battle, and Settlement at 360×800, 375×812, 402×874, and 430×932 full plus representative inset cases, along with the actual wide desktop host. All evidence must come from the same release build generated by `FruitDefense.Editor.WebBuild.Build`, after `FruitDefense.Editor.ProjectSetup.SmokeValidate` and UI-focused tests pass. Ordinary WebGL is the shared acceptance baseline only.

Static concept comparison alone was rejected because it cannot prove packaged glyphs, safe-area behavior, draw/hit alignment, or live route state.

Automated Gate A checks are necessary but no longer sufficient. The user must
explicitly accept the new 402×874 ready-state composition after side-by-side
review. Until that decision, previous manifests remain engineering evidence and
route-wide visual rollout is blocked.

## Risks / Trade-offs

- **[A shorter stage can reduce battlefield legibility or interaction precision]** → Constrain the 38–43% target by existing map/entity and target minimums, recalculate through the single projection, and fail the Battle gate on projected content, round-trip, or pointer drift before route rollout.
- **[A rounded display font may have incomplete glyphs or poor WebGL rasterization]** → Require license and glyph-coverage records, finite-copy measurement, representative rare-character proof, and live WebGL raster review before activating the theme; no system fallback masks a failure.
- **[Replacing art in place can leave stale source/runtime pairs]** → Make the deterministic exporter update manifest hashes and revision, preserve destination GUIDs, reject unowned/mixed-revision files, and verify the actual WebGL payload identity.
- **[Prompt regeneration is nondeterministic and a generated sheet can contain accidental labels or merged components]** → Retain one reviewed generated output by hash, reject detected text/overlap/edge contact during extraction, and make repeatability assertions from that fixed source; never regenerate during export or fall back to programmatic button painting.
- **[Decorative fruit/leaf motifs can compete with controls or violate safe areas]** → Restrict them to existing ornament/illustration owners, cap their alpha bounds to protected corners, and reject overlap with text, actions, or stage content.
- **[The new phase row consumes vertical space in active states where no command exists]** → Keep the row as phase/progress feedback so vertical geometry is stable across ready, active, countdown, paused, and terminal transitions.
- **[Route rollout can drift into page-specific styling]** → Require every route to consume the same theme roles, typography styles, ArtSet bindings, shared action anatomy, and `PortraitShellLayout`; reject Presenter-local assets, fonts, or color constants.
- **[Finite semantic slots can still produce a visually unrelated screen]** →
  Validate the actual reference component anatomy and relative composition, not
  only slot completeness, colors, containment, and command semantics; keep Gate
  A pending until explicit user approval.

## Migration Plan

1. Record the current release theme/ArtSet/font identity and canonical Battle evidence as the before baseline; do not treat the concept image as a production dependency.
2. Update the Battle layout authority and tests atomically, remove the in-stage Wave draw/hit path, and establish the 402×874 track blueprint with unchanged commands and projection semantics.
3. Add explicit role font references, import the approved licensed static faces, update shared measurement/drawing and validators, then remove the legacy single-font field and synthesized-style path.
4. Generate and review one project-owned, text-free ImageGen action-material sheet; deterministically crop/normalize its six isolated components into owned masters, run the exporter into existing runtime destinations, preserve `.meta` GUIDs, bump revisions/hashes, and delete the procedural action-material generator path.
5. Complete Gate A and fix all blocking Battle typography, geometry, state, art, or input defects before changing shell layouts.
6. Propagate the accepted tokens, components, and visual rhythm through Bootstrap, Lobby, and Settlement; complete Gate B, the aggregate editor smoke, and the ordinary-WebGL flow.
7. Synchronize the accepted stable rules into `docs/ui/ui-visual-system.md` and the affected main specs when this change is archived; update evidence-owning gate documents only when their requested evidence is produced.

There is no runtime data migration and no compatibility period. Rollback, if implementation fails before release acceptance, is an atomic source-control revert of the reset change; the shipped player never contains selectable old/new themes or fallback rendering.

## Open Questions

No design question blocks implementation. Route-wide rollout is intentionally
blocked until the user accepts the rebuilt 402×874 Battle reference slice.
