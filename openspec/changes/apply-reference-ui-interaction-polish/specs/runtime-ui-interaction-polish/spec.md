## ADDED Requirements

### Requirement: Semantic runtime motion language
The runtime UI SHALL express transient feedback through shared press, pop, fade-slide, and stagger patterns whose timing, scale, alpha, offset, and easing originate from validated theme tokens and unscaled time.

#### Scenario: High-frequency action feedback
- **WHEN** an enabled action is pressed and released
- **THEN** its visual response uses the shared short press pattern and returns exactly to its authoritative resting rectangle without leaving residual scale, offset, or opacity

#### Scenario: Impulse duration is independent from feedback lifetime
- **WHEN** a pop is driven by a status or reward pulse that remains visible for longer than the pop timing token
- **THEN** the inward impulse completes within the short pop token, never enlarges past scale `1.0`, and the remaining visibility time does not stretch the rebound

#### Scenario: Important value or result feedback
- **WHEN** a resource, status, or settlement emphasis is triggered repeatedly
- **THEN** the previous owned sample is replaced and the new pop begins from a deterministic state without stacking multiple animations

### Requirement: Authoritative press lifecycle
Shell actions SHALL use one press lifecycle that captures the primary pointer, distinguishes pressed, released, cancelled, and drag-suppressed states, and activates a command only on a valid release inside the originating enabled target.

#### Scenario: Pointer becomes a drag
- **WHEN** the pointer moves beyond the configured threshold after pressing a Shell action
- **THEN** the press is cancelled for activation and releasing the pointer does not invoke the action

#### Scenario: Route changes during a press
- **WHEN** navigation begins, the target becomes disabled, or the presenter is reset while a pointer is captured
- **THEN** ownership is cleared and no delayed activation occurs

### Requirement: Route-specific restrained feedback
Lobby, Battle, and Settlement SHALL each apply the shared motion language to their highest-value interaction and information moments without animating the entire screen continuously.

#### Scenario: Lobby becomes visible
- **WHEN** Lobby initializes with playable levels
- **THEN** the title, level choices, and primary Start action reveal in a bounded sequence while all controls remain inside their existing layout and become interactive according to the route state

#### Scenario: Battle feedback changes
- **WHEN** a high-value Battle resource, status, selection, or wave action changes
- **THEN** the affected local element receives short feedback without moving the battlefield projection or changing simulation timing

#### Scenario: Settlement becomes visible
- **WHEN** Settlement binds a valid result
- **THEN** result, metrics, and actions reveal in a short hierarchy and finish at the same authoritative rectangles used for static acceptance

### Requirement: Reduced-motion equivalence
The interaction-polish system SHALL support a reduced-motion policy that removes travel, stagger, and transient impulse while preserving content, state cues, command availability, and final layout.

#### Scenario: Reduced motion is active
- **WHEN** a route or control requests a motion sample
- **THEN** the system returns the semantic resting presentation without hiding information or relying on motion to communicate selected, loading, success, warning, error, or disabled state

### Requirement: Provisional reference-resource provenance
Any resource derived from the analyzed reference APK and bound to runtime UI SHALL have recorded source identity, extraction method, output format, dimensions, import settings, semantic slot, and provisional replacement status before entering the production ArtSet.

#### Scenario: Protected resource is not decoded
- **WHEN** an APK resource cannot be decoded and visually verified
- **THEN** it remains outside production runtime assets and the implementation uses an existing validated semantic-slot resource

#### Scenario: Provisional resource is accepted
- **WHEN** a decoded reference resource passes provenance, importer, optical, nine-slice, and WebGL validation
- **THEN** it is referenced only through an existing semantic ArtSet slot so a later replacement does not require route code or layout changes

### Requirement: Allocation-safe deterministic evaluation
Motion sampling SHALL be deterministic, allocation-free during repaint, cancellable by owner reset, and independent of gameplay time scale.

#### Scenario: Repeated repaint during active motion
- **WHEN** the WebGL render loop evaluates an active motion across successive repaint events
- **THEN** the sample progresses from its defined start to exact resting values without managed allocations from the evaluator or dependence on `Time.timeScale`
