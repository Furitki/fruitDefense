## Context

The runtime UI uses one shared IMGUI layer, semantic ArtSet slots, and finite copy/layout catalogs. Current layout tests validate component rectangles, uniform icon safe insets, and `GUIStyle.CalcSize` line boxes. Those proxies miss the actual alpha bounds of individual images and the packaged font's rendered glyph position. The active action family demonstrates the gap: every source uses the same 128-pixel canvas, yet primary and danger have different alpha envelopes, so equal destination rectangles render unequal visible buttons.

This presentation-only change must preserve the existing 402×874 logical layout authority, hit rectangles, stable asset GUIDs, unscaled interaction feedback, and ordinary WebGL baseline.

## Goals / Non-Goals

**Goals:**

- Make common layout consume authoritative, asset-specific optical bounds generated from final runtime PNG alpha.
- Give packaged-font typography one role-level optical vertical correction instead of presenter-specific nudges.
- Compose icon/indicator and text as a single measured visual group in actions, metrics, statuses, and the paused modal.
- Make paired action surfaces visibly equal while their authoritative hit rectangles remain unchanged.
- Add deterministic Editor gates plus canonical live WebGL evidence for the final rendered result.

**Non-Goals:**

- Change gameplay, battle simulation, route navigation, persistence, or platform adapters.
- Introduce runtime-readable textures, runtime alpha scans, a second layout system, or per-screen pixel exceptions.
- Replace the approved Sunny Orchard Painted art direction or import protected APK payloads.

## Decisions

### Serialize generated optical alpha insets on every art binding

`RuntimeUiArtBinding` will own an `opticalInset` measured from the final runtime PNG's non-transparent alpha bounds. The existing source/export pipeline writes it beside slice and safe insets, and validation rejects stale or invalid values. Runtime composition converts this metadata into a destination-space visual rectangle without reading texture pixels.

This is preferred over `SafeInset` because safe padding is a family/import contract, not the actual visible silhouette. Runtime texture scanning is rejected because release textures need not be readable and scanning would add startup work and allocations.

### Normalize action surfaces at the owned export boundary

Primary, secondary, quiet, and danger nine-slice sources will share one reviewed visible alpha envelope on the common 128-pixel canvas. The deterministic exporter/validator will enforce the envelope while preserving each destination `.meta` GUID and the protected 32-pixel slice border.

Per-kind destination-rectangle compensation is rejected: it would hide inconsistent assets in runtime layout, complicate nine-slice math, and risk drawing outside the hit rectangle.

### Apply one optical text offset per semantic typography role

`RuntimeUiTypographyStyle` will add a finite `opticalOffsetY` token applied through cached GUI styles. It corrects the packaged Noto Sans SC font's glyph placement consistently for titles, body copy, labels, metrics, and supplemental text. Shared layout continues to use semantic roles; presenters cannot add local text offsets.

Per-string glyph scans are rejected because the finite role correction is deterministic, allocation-free, and sufficient for the packaged CJK font. Final WebGL raster evidence remains the acceptance authority.

### Use one reusable icon-label visual-group resolver

Actions, compact metrics, statuses, and modal hints will resolve the actual icon optical rectangle plus the measured label line box as one group, center that group inside the owning content rectangle, and draw through the same geometry. The paused hint will no longer draw an independently anchored indicator and independently centered message.

### Preserve authoritative interaction geometry

All visual corrections operate inside the original component/hit rectangle. `GUI.Button`, pointer ownership, drag behavior, and motion transforms continue to use the existing layout authority. Visual insets and typography offsets never expand the hit target or allow feedback outside its owner.

### Validate assets, geometry, and live raster separately

Editor validation will verify final PNG alpha bounds, serialized optical metadata, normalized action-family envelopes, role offsets, and shared group geometry at every supported scale. Ordinary WebGL acceptance will include the paused modal and cross-route action/title samples so the final raster is reviewed, not inferred from Rect containment alone.

## Risks / Trade-offs

- [A role-level offset may not perfectly model every rare glyph combination] → Keep offsets small, validate the finite copy catalog, and use live canonical screenshots as the final gate.
- [Re-normalizing painted surfaces may soften or shift brush detail] → Preserve original dimensions/GUIDs, modify only transparent framing/positioning, and compare before/after alpha and screenshots.
- [Existing tests may depend on safe-inset proxies] → Replace obsolete assertions with optical-metadata assertions; do not retain dual geometry paths.
- [Binary PNG changes are harder to review in diffs] → Keep deterministic source/export records and publish alpha-bound measurements plus live evidence.
