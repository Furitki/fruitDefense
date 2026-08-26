## ADDED Requirements

### Requirement: Finite semantic floating-text roles
The battle presentation SHALL render floating combat information through a finite reviewed set of normal-damage, heavy-damage, periodic-damage, resource, control, and defeat roles, and bundled feedback profiles SHALL NOT supply arbitrary font, size, raw color, or motion values for floating text.

#### Scenario: Bundled feedback catalog is validated
- **WHEN** production feedback profiles are checked before release
- **THEN** every visible floating-text profile selects one declared semantic role
- **AND** every declared role has a packaged font, minimum reference size, fill, outline, lifetime, rise, rebound, fade, and density policy

#### Scenario: New ability content is added
- **WHEN** a new ability wants to display resolved damage
- **THEN** it selects an existing semantic floating-text role or introduces a separately reviewed role contract
- **AND** it cannot bypass the contract with a skill-specific font or raw color

### Requirement: Readable typography and non-color semantics
At the 402-by-874 reference composition, normal floating damage SHALL render at no less than 16 reference pixels, periodic damage at no less than 14 reference pixels, and every battlefield label SHALL use a reviewed high-contrast outline or equivalent halo. Color SHALL NOT be the only distinction between resource, control, heavy, periodic, and defeat feedback.

#### Scenario: Damage crosses grass and route surfaces
- **WHEN** the same normal-damage label is captured over green grass and brown route terrain
- **THEN** its fill and outline remain legible on both surfaces without changing font or color dynamically from sampled terrain

#### Scenario: Color information is unavailable
- **WHEN** the final battle capture is reviewed without relying on hue differences
- **THEN** sign, copy, size, and motion still distinguish damage, resource, control, and defeat feedback

### Requirement: Contact-led rebound rhythm
Floating text SHALL use a bounded entry, hold/rebound, and rise/fade envelope centered on the semantic event position, and rebound strength SHALL increase by semantic importance without a long oscillating tail.

#### Scenario: Ordinary and heavy damage land together
- **WHEN** normal and heavy damage feedback begin on the same frame
- **THEN** both establish within the entry phase
- **AND** heavy damage reaches a visibly stronger peak and longer hold than normal damage before both rise and fade

#### Scenario: Periodic damage repeats
- **WHEN** repeated periodic damage is not merged into an existing record
- **THEN** its entry scale and lifetime remain lower than ordinary impact feedback so it does not imitate a primary hit

### Requirement: Speed-aware readable presentation clock
Merge eligibility SHALL remain based on authoritative logic ticks, while floating-text lifetime SHALL advance on a local unscaled presentation clock only when feedback is not paused. At 2x the local lifetime SHALL retain at least 70 percent of its 1x real duration.

#### Scenario: Pause freezes feedback
- **WHEN** a floating record is active and battle pause remains enabled while real frames pass
- **THEN** its local progress, position, scale, opacity, and lifetime do not advance

#### Scenario: 1x and 2x are compared
- **WHEN** the same role is displayed at 1x and 2x
- **THEN** event production follows battle speed
- **AND** the 2x record retains at least 70 percent of the 1x real reading time

### Requirement: Dense feedback is bounded before drawing
The floating channel SHALL merge, prioritize, admit, replace, or discard records before drawing so event volume cannot increase visible records beyond 12 total or ordinary/periodic damage beyond 8. Higher-priority heavy, resource, control, and defeat information SHALL be able to displace ordinary damage.

#### Scenario: Mowing-style event burst
- **WHEN** hundreds of eligible damage events arrive within a short presentation interval
- **THEN** same-target semantic events merge where eligible
- **AND** active records remain within both density budgets without an unbounded queue

#### Scenario: One area impact hits many targets
- **WHEN** one feedback profile resolves against more than three distinct targets on the same logic tick
- **THEN** no more than three target labels from that profile and tick are admitted
- **AND** the gameplay still resolves every target outcome

#### Scenario: Defeat arrives under saturation
- **WHEN** ordinary damage occupies its visible budget and a defeat record arrives
- **THEN** defeat remains visible by replacing lower-priority ordinary feedback while total capacity remains bounded

#### Scenario: Nearby targets produce feedback
- **WHEN** admitted records originate from clustered target positions
- **THEN** deterministic visual lanes reduce exact overlap without changing authoritative positions or hit-test geometry

#### Scenario: Feedback lands at the upper route edge
- **WHEN** an upward label would be clamped against the battlefield top boundary
- **THEN** its visual lane and travel unfold toward the battlefield interior while its semantic event position remains unchanged

#### Scenario: Defeat follows damage that is still fading
- **WHEN** a defeat result appears while ordinary or heavy damage from the preceding beat remains visible nearby
- **THEN** fatal damage does not emit a duplicate numeric label and same-tick defeats collapse to compact `击败×N` copy using glyphs already present in the release UI inventory
- **AND** the defeat copy uses a dedicated terminal display band outside the three damage lanes, unfolding toward the battlefield interior without changing the authoritative event position

### Requirement: Warm steady-state path is allocation-bounded
After bounded pools, role styles, and repeated display text are warmed, consuming and advancing dense floating feedback SHALL reuse records and SHALL NOT construct display text on every repaint.

#### Scenario: Repeated dense profile is warmed
- **WHEN** a prebuilt event set repeatedly drives the floating channel after its records and common numeric text are warm
- **THEN** expired and evicted records are reused
- **AND** steady-state allocation remains within the accepted Editor smoke budget

### Requirement: Atlas adoption is evidence-gated
The release SHALL continue using the packaged Noto Sans SC source font without a separate project-authored floating-text atlas unless either real WebGL profiling at the accepted visible density exceeds the documented frame-time or allocation threshold, or final-raster acceptance demonstrates a repeatable quality failure that the current renderer cannot satisfy. A quality-triggered migration SHALL identify the observed failure and preserve the finite role, glyph, spacing, density, and WebGL acceptance contract.

#### Scenario: Baseline stays under budget
- **WHEN** 402-by-874 WebGL profiling stays at or below 0.5 milliseconds p95 for floating-text rendering and 1 kilobyte per second of steady-state allocation after warm-up and final-raster review finds no repeatable outline, scaling, or glyph-coverage failure
- **THEN** no bitmap glyph atlas, generated digit image, or additional font asset is added
- **AND** the finite floating-text glyph inventory is requested at initialization for every role size before combat display begins

#### Scenario: Future baseline crosses the gate
- **WHEN** verified WebGL profiling crosses either threshold or final-raster acceptance records a repeatable outline, scaling, or release glyph-coverage failure and a later change adopts an atlas renderer
- **THEN** its glyph atlas is deterministically generated from the packaged font and a finite reviewed glyph inventory
- **AND** the atlas preserves the same role, outline, spacing, and final-raster acceptance contract

### Requirement: Real portrait WebGL acceptance
The final floating-text system SHALL be accepted from the ordinary WebGL build at the 402-by-874 portrait reference on grass, route, 1x, 2x, and dense combat surfaces without moving HUD or authoritative hit targets.

#### Scenario: Final WebGL evidence is captured
- **WHEN** the release build is exercised through real pointer and speed controls
- **THEN** normal, heavy, periodic, resource, control, and defeat roles are readable according to their hierarchy
- **AND** density fallback, safe area, HUD, and interaction geometry remain acceptable
