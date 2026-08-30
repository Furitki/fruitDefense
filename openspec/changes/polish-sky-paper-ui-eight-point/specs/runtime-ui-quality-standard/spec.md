## ADDED Requirements

### Requirement: P0 reference-parameter authority
For the affected visual scope, every visual parameter explicitly supplied or user-approved in a reference image SHALL be treated as P0 authority. Generic visual-system guidance SHALL apply only when no reference image exists or to parameters the reference does not define, and SHALL NOT alter, average, reject, or replace a reference-authoritative parameter.

#### Scenario: Reference and generic guidance conflict
- **WHEN** an approved reference specifies a component's color, luminance, shape, proportion, rim, outline, shadow, or texture and a generic palette or style rule prefers another value
- **THEN** the candidate preserves the reference parameter exactly and the generic rule does not trigger a resource modification

#### Scenario: Reference does not define a parameter
- **WHEN** a component has no supplied reference or the supplied reference does not define a required visual parameter
- **THEN** the current visual-system contract guides that unresolved parameter without changing any parameter already controlled by the reference

#### Scenario: Reference authority is integrated
- **WHEN** a reference-controlled component is exported into the project-owned runtime ArtSet
- **THEN** its approved visual parameters remain hash- or measurement-verifiable while runtime copy, command semantics, semantic icon identity, gameplay content, and hit geometry remain project-owned authorities

### Requirement: Content-first contrast correction
Contrast SHALL be validated against the actual rendered container/content pair. When a reference controls the container but does not control content color, a failed contrast gate SHALL be corrected by changing the separate text/icon content token first and SHALL NOT be corrected by darkening, recoloring, regenerating, overlaying, or substituting the reference-authoritative raster.

#### Scenario: Reference container fails with the current content token
- **WHEN** the approved container pixels fail the required contrast threshold against the current text or icon content color
- **THEN** the content token is recalibrated and remeasured while the raster hash and reference-controlled pixels remain unchanged

#### Scenario: Both sides are reference-locked
- **WHEN** the approved reference explicitly controls both the container and content colors and that pair cannot satisfy a mandatory accessibility gate
- **THEN** the visual Gate stops with the exact conflict for explicit user direction and no generic rule silently changes either reference parameter

#### Scenario: No reference exists
- **WHEN** a new component is designed without a supplied reference image
- **THEN** the generic visual-system contract may guide both container and content selection, and the final rendered pair still satisfies the required contrast gate

### Requirement: Reviewed ImageGen page-material family
The active sky-paper-orchard ArtSet SHALL source the six action surfaces plus `surface.safe-area`, `surface.panel-standard`, `surface.panel-raised`, `surface.metric`, `surface.card-selectable`, `slot.tool`, `slot.nursery`, and `surface.gameplay-stage` from fixed reviewed project-owned ImageGen output. The deterministic exporter SHALL preserve their generated material pixels and SHALL NOT procedurally paint, recolor, reconstruct, or replace their face, rim, outline, highlight, shadow, texture, or decoration. The reviewed `surface.metric` and `slot.nursery` outputs SHALL each retain a rounded cream paper carrier and soft tonal depth while containing no solid or dashed linear rail.

#### Scenario: Selected direct masters are exported
- **WHEN** the reviewed individual ImageGen master bytes are processed without source changes
- **THEN** every selected semantic slot records one generated asset/output/hash and deterministic transform, exports to its existing path with stable GUID and valid nine-slice geometry, and contains no baked text or icon

#### Scenario: Selected generated source is missing or invalid
- **WHEN** a reviewed asset hash, complete-component containment, alpha edge, stretch-safe center, or text/icon-free check fails
- **THEN** export fails without using a procedural material recipe, script recolor, legacy source, or runtime fallback

#### Scenario: Semantic slots share a direct master
- **WHEN** two semantic slots are configured to reuse one ImageGen master
- **THEN** their final edge, outline, shadow, alpha, and material-anatomy contracts are identical; a line-free compact carrier SHALL NOT reuse a bordered panel master

#### Scenario: A transparent ImageGen material is resized
- **WHEN** any direct ImageGen nine-slice is normalized into its source master or runtime export
- **THEN** low-alpha ringing is cleared after that resize, every `alpha == 0` pixel stores zero RGB, and failure of either check stops export and Unity release validation

#### Scenario: Export is repeated
- **WHEN** the same reviewed individual masters are exported twice from an unchanged source state
- **THEN** all selected source masters, runtime PNGs, manifest bindings, and active ArtSet metadata are byte/hash stable

### Requirement: Direct replacement ImageGen workflow
When the existing runtime layout, typography, copy, interaction geometry, and semantic slots are already approved, material iteration SHALL generate directly replaceable, text-free and icon-free single-component ImageGen masters. The production path SHALL NOT require a decomposition sheet or a second generated solid-background correction.

#### Scenario: A material family needs replacement
- **WHEN** one action, paper, card, slot, or stage material family is revised
- **THEN** the supplied reference controls visual direction, the current slotted master controls geometry/alpha intent, and one direct generated component replaces every intentionally shared semantic slot without changing layout, font, copy, GUID, slice metadata, or draw/hit geometry

#### Scenario: ImageGen returns invalid transparency
- **WHEN** a selected output contains a baked checkerboard or opaque exterior
- **THEN** integration may perform exactly one deterministic connected-background cleanup derived from that output's own pixels or use one separately hash-locked same-semantic geometry-alpha mask whose silhouette has been measurement-locked, and SHALL NOT reuse a legacy/foreign mask, request a solid-background regeneration, or script-paint any material pixel

#### Scenario: Direct material pixels are transformed
- **WHEN** the generated component enters the exporter
- **THEN** the only permitted operations are hash validation, complete-component crop/alpha extraction, transparent padding, alpha-safe resize, measurement, hashing, and export

### Requirement: Fresh leaf-green contrast pairing
Primary and green secondary action surfaces SHALL use the user-approved light leaf-green direct master as their P0 container authority with soil-brown text and icon content as the current contrast correction, and that actual rendered semantic pair SHALL retain contrast of at least `4.5:1` in every required state. Danger actions SHALL retain warm-white content.

#### Scenario: Fresh-green candidate is validated
- **WHEN** primary and green secondary containers are measured from their final runtime PNGs and composed Battle canvas
- **THEN** they are lighter and more yellow-fresh than the recorded baseline, preserve the approved cream rim and soil outline anatomy, and pass the `4.5:1` soil-brown content threshold without exporter-side recoloring

#### Scenario: Light-green runtime pairing is integrated
- **WHEN** the approved light-green master is exported and composed in Battle
- **THEN** its generated material pixels and hash remain unchanged, the primary/secondary content token is soil brown, and no universal warm-white or generic palette requirement causes the container to be replaced by a darker candidate

### Requirement: Marker-free contained interaction feedback
Actions and interactive tool/nursery slots SHALL NOT draw an action-specific or slot-specific `marker.selected` badge. Hover/focus and mouse press SHALL use the existing restrained theme scale tokens, and nursery selection SHALL use one short selection pulse, without changing the authoritative draw owner or hit rectangle.

#### Scenario: Action receives pointer or keyboard focus
- **WHEN** a standard or compact action resolves `HoveredOrFocused`
- **THEN** its complete visual content moves slightly inward using the theme interaction scale, no marker or primitive outline is drawn, and its hit geometry, label ownership, and icon ownership remain unchanged

#### Scenario: Nursery slot is clicked or selected
- **WHEN** an empty or occupied nursery slot receives a mouse press or selection click
- **THEN** its complete slot visual, including the reviewed base surface, performs one restrained contained press/selection motion inside the unchanged slot rectangle, no selected marker or primitive outline is drawn, and applicable semantic state indicators remain singular

#### Scenario: Other semantic state needs an indicator
- **WHEN** a slot resolves disabled, loading, success, warning, error, or a drag-derived state
- **THEN** its applicable semantic indicator is drawn exactly once without restoring the removed selected-marker path

### Requirement: Authored Battle carriers remain distinct from PC seams
The Battle header metrics and nursery section SHALL retain their reviewed light frame surfaces exactly once. Each nursery slot SHALL retain one reviewed line-free rounded-paper carrier surface. Fractional-scale PC seam correction SHALL operate in the shared nine-slice renderer and SHALL NOT hide, edit, recolor, cover, substitute, or duplicate those authored surfaces.

#### Scenario: Battle header is rendered
- **WHEN** the Sun, Core, and Wave compact metrics are composed inside the raised Header
- **THEN** each metric draws exactly one dedicated line-free `surface.metric` capsule together with its icon, label, value, and existing pulse, with no authored solid/dashed perimeter rail, dark/neutral alpha fringe, black internal seam, panel-master reuse, or duplicate border layer

#### Scenario: Battle nursery section is rendered
- **WHEN** the persistent nursery section is composed below the context tray
- **THEN** it draws exactly one `surface.panel-standard`, and each of its five cells draws exactly one reviewed `slot.nursery` rounded-paper base with no solid rim or dashed rail, while title, content, refresh action, and input geometry remain unchanged

#### Scenario: A non-normal nursery state is rendered
- **WHEN** a nursery cell resolves selection, reward, warning, or drag feedback
- **THEN** the same single line-free reviewed base surface participates in contained motion, and the applicable singular semantic indicator operates inside the original slot rectangle without a selected marker, primitive outline, solid/dashed rail, second surface, or black internal seam

#### Scenario: Nursery alpha is extracted from an RGB ImageGen plate
- **WHEN** the selected line-free nursery output contains a baked neutral checkerboard instead of genuine alpha
- **THEN** exactly one deterministic edge-connected neutral-background cleanup derives geometry from that output's own pixels, every resize clears low-alpha ringing, and no legacy slot alpha mask, neutral/dark partial-alpha fringe, or hidden checkerboard RGB remains in source or runtime PNGs

#### Scenario: Metric alpha is extracted from an RGB ImageGen plate
- **WHEN** the selected line-free metric output contains a baked neutral checkerboard instead of genuine alpha
- **THEN** exactly one deterministic edge-connected neutral-background cleanup derives geometry from that output's own pixels, every resize clears low-alpha ringing, and no panel alpha, legacy/foreign mask, neutral/dark partial-alpha fringe, hidden checkerboard RGB, or continuous perimeter rail remains in source or runtime PNGs

### Requirement: PC-scale seam-safe nine-slice rendering
Nine-slice surfaces SHALL remain free of black or transparent internal boundary lines across the required portrait matrix and representative PC device scale factors.

#### Scenario: Nine-slice surface is rendered at a fractional PC scale
- **WHEN** source borders map to snapped destination boundaries under a non-integer host or GUI scale
- **THEN** adjacent patches cover and sample the shared boundary without a visible horizontal or vertical seam, while the outer rectangle and transparent gameplay-stage center remain authoritative
