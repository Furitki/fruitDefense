## Context

The previous UI change established one runtime theme, a finite 49-slot art contract, deterministic resource validation, shared layout helpers, and full WebGL route evidence. The remaining problem is visual quality: text and icons can technically fit while still looking misaligned; a screenshot can pass broad black/clip checks while color hierarchy, optical balance, or reference-board composition remains weak. The project uses Unity immediate-mode GUI, so quality rules must be expressed through cached GUI styles, explicit layout rectangles, deterministic font measurement, semantic art metadata, and live WebGL evidence rather than relying on prefab authoring tools.

The approved visual authorities are the Sunny Orchard style board, the painted component proof v2, the active `sunny-orchard-painted@1` set, and actual supported portrait canvases. Existing gameplay, navigation, persistence, and content behavior are protected boundaries.

## Goals / Non-Goals

**Goals:**

- Make typography alignment, text bounds, spacing, color use, contrast, icon optical boxes, surface hierarchy, nine-slice safety, and route composition explicit and testable.
- Produce one current-state audit with severity, ownership, screenshot/resource evidence, and a closed-loop resolution for every blocking/high issue.
- Improve all four release routes against the approved references without changing gameplay or input semantics.
- Keep draw and hit geometry derived from the same layout helpers and preserve supported portrait/safe-area behavior.
- Keep production resources deterministic, replaceable, and isolated from review/evidence assets.

**Non-Goals:**

- Changing battle rules, level balance, persistence, route order, session behavior, or content catalog semantics.
- Adding runtime skin selection, remote art loading, filename lookup, compatibility fallback, or partially merged release themes.
- Claiming Douyin or WeChat support from ordinary WebGL results.
- Rebuilding the UI in UGUI/UI Toolkit during this polish pass.

## Decisions

### 1. One normative quality standard plus one machine-readable profile

`docs/ui/ui-visual-system.md` remains the stable human-readable authority. The implementation will add a compact editor-owned quality profile for numeric rules that tests must consume: supported viewports/insets, minimum font roles, line counts, touch sizes, spacing grid, contrast floors, icon optical-box tolerance, and nine-slice constraints. Tests and presenters will not duplicate those numbers.

Alternative considered: keep all values in prose and hand-review screenshots. Rejected because the previous pass already demonstrated that broad visual checks miss alignment and clipping defects.

### 2. Deterministic layout inspection before WebGL capture

Each player-visible copy/state will be represented by a finite inspection case with its authoritative draw rect, text role, alignment, line policy, icon/indicator rect, and supported viewport/safe-area matrix. Editor tests will use the packaged Noto Sans SC font and the same GUIStyle configuration to verify `CalcSize`/`CalcHeight`, baselines, containment, and non-overlap. Long copy must use an explicit finite line split or a justified layout change; runtime truncation and implicit shrink-to-fit are forbidden.

Alternative considered: OCR every screenshot. Rejected as the primary gate because browser rasterization and Chinese OCR are not deterministic enough; live captures remain the final visual proof.

### 3. Optical alignment is metadata and geometry, not transparent-canvas centering

Common icons and indicators retain stable canvas/importer contracts, while editor audits record alpha bounds, centroid, intended optical box, and baseline offset. Runtime drawing continues to use explicit content rectangles; any compensation is semantic and bounded, not inferred every frame from pixels.

Alternative considered: crop every icon tightly. Rejected because tight cropping destabilizes state transitions and breaks the existing replaceable-canvas contract.

### 4. Color quality combines semantic tokens with raster evidence

Theme tokens remain the only runtime color authority. Static validation checks semantic role usage and minimum contrast; WebGL acceptance measures actual rendered foreground/background clusters for representative normal, selected, loading, disabled, warning, error, and modal states. Color cannot be the only state cue.

Alternative considered: accept source-palette contrast as sufficient. Rejected because prior alpha/tint composition reduced real rendered contrast below source values.

### 5. Reference-driven layout refinement stays inside authoritative external geometry by default

Lobby, Battle, Settlement, and Bootstrap may change internal composition, spacing, text grouping, icon placement, and ornament usage against the approved reference. Existing external component and hit rectangles remain unchanged unless an audit demonstrates that the current rectangle cannot meet the normative quality standard; any such change must update the single layout authority and its interaction tests together.

Alternative considered: reproduce the reference mockup literally. Rejected because the mockup is a visual authority, not a substitute for supported runtime geometry or input contracts.

### 6. Evidence is separated into baseline, defects, fixes, and canonical acceptance

The change evidence tree will contain a scored baseline, per-defect before/after evidence, resource inventory, editor gate logs, and one canonical final WebGL matrix. Rejected attempts and infrastructure regressions are retained outside canonical directories. Disposable profilers, capture servers, temporary importers, and generated invalid fixtures are removed after validation.

### 7. The WebGL host owns complete-canvas containment

The project will use a project-owned WebGL template. On desktop and embedded browser windows, the 402 by 874 reference canvas is scaled uniformly by `min(availableWidth / 402, availableHeight / 874)` and centered inside the viewport. The host must never vertically center an unscaled canvas whose top or bottom lies outside the visible viewport. Mobile may fill the viewport only when the same complete-canvas and safe-area contract remains true.

Alternative considered: keep Unity's fixed desktop template and rely on browser scrolling or fullscreen. Rejected because the actual 1280 by 720 review window clips roughly 98 logical pixels from the top before any runtime layout is evaluated.

### 8. Optical alignment is verified on rendered groups

Component layout will expose a finite anatomy for the complete visual group: icon ink box, glyph measurement box, inter-item gap, and group bounds. Actions, metrics, result rows, and comparable repeated controls center the group, not the label alone with an independently anchored icon. Editor geometry remains deterministic; live WebGL evidence supplies the final raster proof.

Alternative considered: treat individual icon and label rectangles as sufficient. Rejected because independently valid rectangles still produce visibly left-heavy buttons and border-straddling result metrics.

### 9. Page balance and battlefield gutters are first-class layout requirements

Route layouts will define occupied-content bounds and intentional flexible regions. Lobby and Settlement may not use a threshold as a target that leaves the composition barely under the maximum blank-area allowance. The Battle projection must use symmetric visual gutters within its viewport while keeping cell hit testing derived from the same projection. Any battlefield repositioning must preserve grid coordinates, command order, and deterministic simulation.

Alternative considered: preserve the previous battlefield rectangle absolutely. Rejected because the accepted build leaves visibly unequal top and right gutters, so protecting the old geometry preserves the most prominent alignment defect.

## Risks / Trade-offs

- [Risk] Immediate-mode GUI style measurement can diverge from WebGL rasterization. → Mitigation: use the packaged font and shared styles in Editor gates, then require live WebGL pixel and manual review.
- [Risk] Adding route-specific polish can fragment the component system. → Mitigation: shared anatomy and semantic slots remain mandatory; route code may only compose shared primitives and finite content illustrations.
- [Risk] Internal layout refinement may accidentally change input behavior. → Mitigation: draw/hit rectangles stay sourced from the same layout objects and interaction smokes compare pre/post authoritative rectangles.
- [Risk] Visual scoring becomes subjective or self-congratulatory. → Mitigation: publish explicit pass/fail thresholds, defect severities, and before/after evidence; do not convert known failures into relaxed thresholds without a structural justification.
- [Risk] Ideal portrait captures hide defects in the actual desktop host. → Mitigation: add wide/short desktop-host cases and assert complete-canvas visibility before route screenshots can be canonical.
- [Risk] New resource experiments leak into release dependencies. → Mitigation: validator rejects unbound production files and any release dependency on review/source/evidence roots.

## Migration Plan

1. Capture and score the current accepted painted build without changing runtime code.
2. Land the normative standard, quality profile, finite inspection catalog, and failing regression tests.
3. Fix shared components and route composition in severity order; update production art only through reviewed masters and deterministic export.
4. Run direct quality gates, ProjectSetup aggregate, P0, and a new WebGL build.
5. Execute the full supported-size Shell matrix and 402 full/inset cross-route matrix; keep the current 4173 build available until the replacement passes.
6. Switch the local acceptance build only after canonical evidence passes. Reverting means restoring the prior active ArtSet/runtime/layout commit, not adding compatibility logic.

## Open Questions

- No product-direction decision is required. If implementation proves an external layout rectangle must change, the change will document the exact rectangle and interaction impact before applying it.
