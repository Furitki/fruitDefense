## ADDED Requirements

### Requirement: Home preserves the approved reference hierarchy
The Lobby Home page SHALL present three illustration-led selectable level cards, an explicit non-color selected cue, one raised-paper pre-battle growth preview, and one leaf-green Start primary action in that reading order while retaining the real level identities and battle-growth projection.

#### Scenario: Home opens with a valid selected level
- **WHEN** Home is rendered from a committed profile and successful growth preview
- **THEN** all three real levels remain selectable, the current card is visually explicit, its orchard illustration is a dominant part of the card, the growth preview shows the authoritative policy/effect summary, and Start launches only the visibly selected level

#### Scenario: Growth preview blocks launch
- **WHEN** the selected gameplay's growth projection is loading or fails
- **THEN** the same Home composition remains mounted, the preview shows its explicit loading or error state, and Start is unavailable without drawing an empty-growth fallback

### Requirement: Activity preserves the approved reward-focus hierarchy
The Lobby Activity page SHALL present the starter activity title and finite description, one dedicated text-free reward hero illustration, two graphical reward tiles backed by the actual reward grants, an explicit claim state, and one leaf-green Claim primary action in that reference-controlled reading order.

#### Scenario: Starter reward is claimable
- **WHEN** the unclaimed starter activity is rendered
- **THEN** the reward hero is the first dominant visual below the title, its actual equipment and material rewards are each visible as an icon-plus-label tile below that hero, claimable state is readable without relying only on color, and exactly one Claim action is enabled

#### Scenario: Starter reward is already claimed
- **WHEN** the committed profile owns the starter activity receipt
- **THEN** the same reward illustration and reward tiles remain visible, a non-color completed cue and claimed copy are shown, and no enabled duplicate Claim action is exposed

### Requirement: Reference composition does not fabricate product data
Home and Activity SHALL use the approved references as authority for material, hierarchy, relative scale, illustration emphasis, and ornament language, while dynamic copy, balances, level state, rewards, commands, and hit targets SHALL remain owned by current runtime systems.

#### Scenario: Reference contains unsupported indicators
- **WHEN** a reference shows a second currency, star rating, schedule, or reward that has no current authoritative runtime model
- **THEN** the runtime composition omits that datum rather than baking, simulating, or inferring it

### Requirement: Hub primary actions preserve the restored original square treatment
Home Start and Activity Claim SHALL use the user-selected original light lime-green rounded-square raster master with its warm paper double rim, shallow lower shadow, restrained smooth paper texture, and protected square-origin corner anatomy, stretched only through its validated 32-pixel nine-slice center. Runtime text, interaction state, and hit geometry SHALL remain independent from the surface raster. Their live label SHALL use the reference inverse warm-white face with the shared warm soil-brown outline token; pure black and near-black action-label outlines are forbidden. These two actions SHALL NOT insert the unrelated Start/play glyph shown by the rejected implementation.

#### Scenario: A Home or Activity primary action is rendered
- **WHEN** Start or Claim is normal, focused, pressed, loading, disabled, success, warning, or error
- **THEN** exactly one restored light lime-green rounded-square-origin surface is rendered inside the authoritative action rectangle, its inverse label is optically centered as live content with a visibly warm soil-brown rather than black outline, and no semicircular capsule replacement, dark-green face, play glyph, dark-brown fill-only label, or second state overlay is visible

### Requirement: Bottom navigation preserves the reference's simple silhouettes and base image
The shared Home, Activity, and Growth navigation SHALL render with exactly two large chrome silhouettes: one dedicated full-width warm-paper base image and one dedicated sun-yellow selected tab merged visually into that base. The base image SHALL own the reference-led outer contour, upper edge, paper thickness, and shallow shadow; it SHALL NOT be approximated with a generic raised panel, three item cards, or primitive rectangles. Home SHALL use one house with a doorway negative space, Activity one calendar with a star, and Growth one two-leaf sprout with a minimal flat base. Each icon SHALL contain exactly one dominant subject silhouette and only necessary semantic negative space, remain recognizable at 24–33 logical points, and contain no detached prop, secondary scenery, fruit/leaf ornament, soil cluster, painterly micro-texture, or selected-state color multiplication.

#### Scenario: A Hub destination is selected
- **WHEN** Home, Activity, or Growth owns the visible Hub page
- **THEN** the visible chrome contains one continuous base silhouette plus one selected-tab silhouette, the tab extends upward without moving or shrinking its authoritative hit rectangle, all three icon/label groups keep the same baseline rhythm, every domain icon remains a clean single-subject silhouette at its actual rendered size, selection is conveyed by the tab, underline, and label without muddying the icon raster, and neither a compact-control surface nor a generic raised-panel surface is reused as navigation chrome

### Requirement: Production UI art is never drawn by script
Every visible pixel in a production UI-art raster SHALL originate from a fixed owned user-approved raster master, including manual, ImageGen, or explicitly selected historical PNG masters. Scripts, canvases, shaders, vector primitives, procedural geometry, and fallback renderers SHALL NOT author, paint, recolor, shade, outline, decorate, reconstruct, or synthesize source/master/runtime artwork. Deterministic tooling MAY only validate hashes, copy fixed bytes, crop complete components, extract alpha from the selected output itself, pad transparently, alpha-safe resize, clear low-alpha fringe, measure, encode, and update metadata.

#### Scenario: A UI-art exporter runs
- **WHEN** any production UI-art set is exported or validated
- **THEN** it consumes pre-existing hash-locked masters, contains no visible-pixel drawing API or procedural asset-authoring path, fails when a required master is missing, and never substitutes generated geometry, a primitive approximation, or a fallback raster

### Requirement: Hub presentation geometry remains single-source and portrait-safe
Home and Activity outer components, child visual anatomy, pointer tracking, hit testing, telemetry, and validation SHALL derive from `PortraitHubLayout` and shared Hub drawing helpers at 360×800, 375×812, 402×874, and 430×932 full and representative inset safe areas.

#### Scenario: A Home card or Activity action is activated
- **WHEN** a pointer press and release completes inside the visible component at any supported geometry
- **THEN** the target resolved from the authoritative layout matches the drawn component exactly, retains at least a 44-point shortest interactive dimension, and triggers only its existing command

#### Scenario: The same pages render with safe-area insets
- **WHEN** Home or Activity is rendered with representative top and bottom insets
- **THEN** the top bar, page anatomy, finite Chinese copy, state indicators, actions, and bottom navigation remain contained, readable, and non-overlapping

### Requirement: Visual completion requires real-canvas user approval
Home and Activity SHALL be built and captured from the ordinary WebGL release canvas at the 402×874 reference viewport, and automated validation SHALL NOT be treated as final visual approval.

#### Scenario: A rebuilt page reaches technical green
- **WHEN** its source/resource, importer, text, geometry, state, editor smoke, and WebGL checks pass
- **THEN** a same-state `reference / before / after` comparison is presented and the visual gate remains pending until the user explicitly approves it

### Requirement: Home reference decomposition produces contained owned components
The approved Home reference SHALL be measured at the 402×874 design viewport and may be decomposed only into complete text-free component imagery with a recorded reference hash, source-space bounds, destination semantic slot, and non-creative transform. The runtime SHALL NOT bind the page screenshot, baked copy, unsupported values, or a page-sized crop.

#### Scenario: A Home level illustration is restored from the reference
- **WHEN** a level image window is integrated into the production ArtSet
- **THEN** its fixed master comes from the recorded complete image-window crop, preserves the reference scene and rounded window silhouette, is drawn inside the authoritative `Thumbnail` rectangle, has no Home illustration-frame overlay, and exposes zero pixels outside its card during normal, focused, pressed, selected, loading, or reveal motion

#### Scenario: Navigation is restored from the reference
- **WHEN** the shared Hub navigation is rendered
- **THEN** its base, selected tab, and three domain icons each resolve from separately owned reference-derived fixed rasters, remain crisp at their final 24–33 logical-point size, and contain no script-drawn border, primitive reconstruction, baked label, or page-background remnant

### Requirement: Home cards use one visible perimeter
Every Home level card SHALL use exactly one card surface perimeter. The illustration SHALL NOT add a second enclosing frame, black edge, mask border, or square-corner overflow.

#### Scenario: A selected Home card is rendered
- **WHEN** the current level is selected
- **THEN** the selected card uses one restrained pale sunlight-paper perimeter plus its independent selected marker, while its reference-derived image window remains visually inside that perimeter without an additional frame rail
