## Context

The validated outgame loop already owns one immediate-mode Lobby Hub, one `PortraitHubLayout` geometry authority, immutable page projections, serialized progression commands, and an active Sunny Orchard production ArtSet. Home and Activity currently use those systems correctly, but their runtime composition reduces the approved references to generic panels, small illustrations, and text rows. The full-page reference PNGs are P0 visual evidence and never ship as runtime screens. For the user-requested Home rework, their complete text-free component imagery may be split once into owned fixed raster masters with source hash and crop bounds; baked copy, unsupported values, page-sized crops, and layout geometry remain forbidden.

The implementation must stay inside the current immediate-mode presentation stack. Draw rectangles, pointer tracking, hit testing, telemetry, and validation continue to derive from `PortraitHubLayout`; the change may recompose rectangles inside the existing Hub tracks, but it must not create presenter-local coordinates or separate input geometry.

## Goals / Non-Goals

**Goals:**

- Make Home recognizable as the approved illustration-led level-selection page: three dominant orchard cards, explicit selection, a paper/ribbon pre-battle growth panel, and one large Start action.
- Make Activity recognizable as the approved starter-supply page: title/description, two graphical reward tiles, explicit state, dedicated reward hero art, and one large Claim action.
- Preserve the shared raised-paper top bar and illustrated bottom navigation while keeping Home/Activity/Growth route semantics unchanged.
- Preserve all finite state copy, packaged-font containment, state indicators, 44-point targets, full/inset portrait support, and real WebGL input parity.
- Add one owned, text-free Activity reward illustration and two owned, text-free bottom-navigation surfaces to the single active ArtSet, reconciling the finite slot contract to 62 slots.

**Non-Goals:**

- No uGUI or UI Toolkit migration.
- No Growth equipment/cultivation page redesign.
- No new currency, star-rating progression, activity schedule, reward amount, equipment identity, persistence field, gameplay rule, or scene route.
- No runtime use of the reference mockup, generated review sheet, or fallback asset.
- No platform conversion or publishing claim beyond ordinary WebGL acceptance.

## Decisions

### 1. Recompose within the existing immediate-mode Hub authority

`PortraitHubLayout` remains the sole producer of shared chrome, page owner, Home card/action, and Activity action rectangles. It will expose richer child anatomy for level illustrations, card copy/marker, growth ribbon/content, Activity hero/reward tiles/status, and navigation visuals. `LobbyHubPresenter` passes content and state only; `RuntimeUiGui.Hub` resolves and draws the shared anatomy.

This keeps draw and hit testing on the same geometry and avoids a second framework. A uGUI migration was rejected because the stable visual standard explicitly excludes it and it would expand this two-page change into a route-wide rewrite.

### 2. Preserve real content instead of copying unsupported reference data

The references control hierarchy, material, relative scale, border/shadow language, and illustration emphasis. Runtime text, one real Morning Dew balance, three real level identities, actual reward grants, claim state, and battle-growth resolution remain authoritative. Unsupported second currency and star progress are not fabricated.

This intentionally adapts the reference rather than baking the mockup verbatim. It prevents decorative data from becoming a false player promise.

### 3. Make illustration and reward groups first-class page anatomy

Home level art will fill the framed left portion of each selectable card rather than sit as a small centered thumbnail. The selected card keeps the same outer and hit rectangle but receives the existing selection surface/marker and stronger illustration-to-copy balance.

The pre-battle growth panel will use the existing section-ribbon, Hub Growth icon, state indicator, and two-line authoritative preview inside one raised-paper owner. Activity will follow the P0 reference order: title/finite description → dominant reward hero → reward preview tiles → claim state → action. The equipment reward tile uses the existing Hub Growth icon and the material tile uses the existing resource icon; their labels and quantities remain dynamic text. The former description → tiles → state → illustration order is removed because it preserved the utility-panel layout instead of the approved reference hierarchy.

The existing `action.primary` semantic slot is restored in place from the user-selected original text-free light lime-green rounded-square PNG at historical commit `d423af201917d6a66a1328f55533d2119203db28`. Runtime labels, icons, states, and hit rectangles remain separate. The master keeps its original 256×256 bytes and 32-pixel nine-slice border; a page-local button slot or compatibility surface is rejected because Start and Claim are ordinary Primary actions and the stable visual system requires one active action family.

An intermediate capsule replacement corrected the live label and removed the Home play glyph, but the user subsequently selected the original square treatment instead. The final primary treatment therefore restores the historical rounded-square corner anatomy and 32-pixel nine-slice border while retaining the already-approved live warm-white label, soil-brown outline, no-play-glyph rule, layout, and hit geometry.

The shared bottom navigation is governed first by the reference's composition-level simplicity, not by whether individual icons satisfy generic technical metrics. Its chrome has a strict two-silhouette budget: one continuous full-width warm-paper base and one selected sun-paper tab rising from it. The base contour, upper edge, paper thickness, shallow shadow, and the tab/base merge are owned by dedicated text-free `surface.hub-navigation-base` and `surface.hub-navigation-selected-tab` rasters. Reusing `surface.panel-raised`, drawing three item cards, or reconstructing the reference bottom image from generic rectangles is forbidden.

The former navigation masters were themselves authored by `ImageDraw` polygons and rounded rectangles, so their flat, coarse material contradicted that raster ownership rule even after they received dedicated slots. They are replaced by two separately hash-locked ImageGen single-component outputs. The primary action is instead restored from the exact user-selected historical PNG as a hash-locked fixed raster master; the exporter only copies its bytes and performs the already-approved alpha-safe runtime resize. All UI-art authoring functions remain removed from the exporter. It may consume fixed masters and perform non-creative normalization only; it cannot draw, recolor, shade, reconstruct, or synthesize visible asset pixels.

User review then rejected the retained Hub icon masters: the detailed farmhouse scene, event pennant/fruit cluster, and textured sprout/soil illustration collapse into noisy blobs at the 24–33 logical-point navigation size. The corrected icon family therefore follows the reference's low-detail silhouette language: Home is one house with a doorway negative space, Activity is one calendar with a star, and Growth is one two-leaf sprout with a minimal flat base. Each icon has exactly one dominant subject silhouette and only the semantic negative space needed to identify it; detached props, fruit/leaf decoration, secondary scenery, soil clusters, painterly texture, and ornamental contour breaks are forbidden. Selection remains owned by the dedicated selected tab, underline, and label; the icon raster is not multiplied by the selected-state tint. Perimeter and actual-size checks remain supporting diagnostics, but they cannot substitute for this composition and silhouette review.

### 4. Add the dedicated Activity and navigation art; keep a single complete ArtSet

Add `illustration.hub-activity-reward` as a fixed-aspect, text-free production illustration slot, plus `surface.hub-navigation-base` and `surface.hub-navigation-selected-tab` as fixed-aspect navigation chrome. Together with the three Hub navigation icon slots, this reconciles the finite ArtSet contract from the older documented 56-slot baseline to the actual 62-slot production contract.

The asset is generated as one transparent component in the approved painted paper-craft style, saved under the owned source hierarchy, recorded with prompt/output hash, exported through the deterministic source exporter, imported with stable metadata, and bound once in the active ArtSet. There is no legacy vista fallback and no page-local texture loading.

### 5. Validate one page at a time and keep visual approval external

Implementation order is shared chrome anatomy, Home, then Activity. After editor geometry/copy/resource gates pass, ordinary WebGL produces a 402×874 same-state comparison for each page. Automated validation establishes technical legality; the user remains the visual approval authority.

### 6. Remove the leaking Home frame stack and use reference-derived components

The rejected Home canvas draws each square-cornered orchard illustration across the complete frame rectangle and then overlays `surface.illustration-frame`. Because the frame has transparent exterior corners, the illustration remains visible outside the intended opening. Home will instead draw one reference-derived, rounded image-window master inside a contained `Thumbnail` rectangle and will not draw the shared illustration-frame overlay at all. The selected card keeps one surface outline; it does not receive a second image border.

The navigation base, selected tab, selected-card surface, and three domain icons become independent reference-derived fixed rasters. ImageGen may isolate a component from the approved reference and remove its surrounding page pixels, but the exporter may only hash, crop, extract same-output alpha, pad, resize, clean fringe, measure, encode, and update metadata. It may not draw or reconstruct any visible contour.

## Risks / Trade-offs

- **[Reference density exceeds compact text bounds]** → Keep actual copy finite, use existing typography roles, and validate child rectangles at 360/375/402/430 full and inset before WebGL capture.
- **[A new illustration slot breaks ArtSet completeness]** → Update the enum, semantic registry, source/runtime manifest, active ArtSet binding, exporter, and validator in one task; no fallback path remains.
- **[Home/Activity changes regress Growth through shared chrome]** → Keep Growth page owner geometry and drawing methods unchanged and rerun its existing smoke/state matrix after shared chrome changes.
- **[Generated hero art contains text or opaque background]** → Request one text-free transparent component, inspect it, preserve the selected output hash, and fail integration if alpha or text constraints are not satisfied.
- **[Reference resemblance is still subjective]** → Deliver real-canvas reference/before/after evidence and leave the visual gate pending until explicit user approval.

## Migration Plan

1. Add richer Home/Activity child layouts and tests while retaining the current page commands and hit targets.
2. Generate, normalize, register, and validate the Activity illustration and two navigation chrome slots so the active ArtSet is complete at 62 slots.
3. Replace the old Home/Activity drawing composition directly; do not retain compatibility draw paths.
4. Run focused Hub and runtime UI smoke, aggregate project smoke, WebGL build, and Home/Activity capture.
5. If acceptance fails, revert the scoped change as one unit; profile/content schemas require no migration.

## Open Questions

None for implementation. Unsupported reference data remains deliberately absent unless separately designed and authorized.
