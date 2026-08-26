## ADDED Requirements

### Requirement: Contact-first floating-text anchor
Damage and defeat floating text SHALL begin in a compact target-relative contact region. A live target SHALL be followed only for the first 0.12 seconds of pause-aware local presentation time, after which the label SHALL detach from its last resolved anchor and continue its bounded lane/rise animation. Merging additional damage SHALL NOT restart the follow phase or reattach a detached label. Missing or defeated targets SHALL use the event position without retaining gameplay entities.

#### Scenario: Hit target moves after impact
- **WHEN** an admitted damage label begins and the live target moves during the contact phase
- **THEN** the label remains visually associated with that target for no more than 0.12 seconds
- **AND** it then detaches without following the target for the remainder of its lifetime

#### Scenario: Repeated damage merges after detach
- **WHEN** more damage merges into an existing label after its contact phase has ended
- **THEN** magnitude, count, copy, and bounded lifetime may update
- **AND** the label does not restart its follow phase or attach to the target again

#### Scenario: Target is defeated by the event
- **WHEN** defeat feedback is admitted after the gameplay target has been removed
- **THEN** its anchor is derived from the recorded semantic event position
- **AND** presentation does not recreate or mutate the defeated entity

#### Scenario: Same-tick defeats aggregate
- **WHEN** defeat feedback from several targets is collapsed into one same-tick record
- **THEN** its anchor is the arithmetic centroid of every contributing event position
- **AND** the aggregate does not retain one contributing target as a live follow target

### Requirement: Compact deterministic separation
At the 402-by-874 reference composition, deterministic damage lanes SHALL add no more than 28 vertical pixels and 8 horizontal pixels beyond the contact anchor, and the dedicated defeat separation SHALL add no more than 26 vertical pixels. Upper-edge feedback SHALL unfold toward the battlefield interior after anchoring.

#### Scenario: Nearby targets emit together
- **WHEN** up to three admitted labels originate from clustered target positions on the same tick
- **THEN** deterministic compact lanes reduce exact overlap
- **AND** every label remains visually attributable to the target cluster rather than a distant screen band

#### Scenario: Defeat follows damage
- **WHEN** compact defeat copy appears while preceding damage still fades nearby
- **THEN** the defeat copy remains distinguishable without exceeding the 26-pixel terminal separation
- **AND** fatal damage remains free of a duplicate numeric label

### Requirement: Finite global camera-impact beats
Camera shake SHALL be requested only through finite Heavy, Cluster, and Terminal impact-beat roles. Ordinary damage, periodic damage, resource feedback, ordinary status application, and an isolated ordinary-enemy defeat SHALL NOT shake the camera. Bundled feedback profiles SHALL NOT provide arbitrary shake waveforms.

#### Scenario: Ordinary sustained fire lands
- **WHEN** repeated normal, gatling, periodic, resource, or control-adjacent events are presented
- **THEN** local recoil, squash, flash, VFX, and floating text may play
- **AND** no camera-impact beat is admitted from those ordinary events

#### Scenario: Reviewed high-value event lands
- **WHEN** a reviewed heavy impact, compact defeat cluster, or boss defeat occurs
- **THEN** exactly one corresponding semantic impact beat may be requested
- **AND** its amplitude and duration come from the presentation-owned beat catalog

### Requirement: Non-additive real-time beat scheduling
The impact-beat scheduler SHALL keep at most one active camera motion, enforce a pause-aware unscaled real-time cooldown, and SHALL NOT sum simultaneous shake records. A request inside the cooldown SHALL be discarded unless it has strictly higher semantic priority, in which case it SHALL replace the active beat. The analytic motion SHALL use a bounded damped envelope with no more than approximately two visible oscillations.

#### Scenario: Dense events request several beats
- **WHEN** multiple eligible events arrive in one frame or inside the global cooldown
- **THEN** camera offset remains bounded by one catalog amplitude
- **AND** equal or lower-priority requests do not extend or restart the beat

#### Scenario: Terminal beat interrupts a heavy beat
- **WHEN** a boss defeat occurs while a Heavy beat is active or cooling down
- **THEN** the Terminal beat replaces the Heavy beat rather than adding to it
- **AND** the resulting offset remains within the terminal amplitude bound

#### Scenario: Battle speed changes
- **WHEN** the same eligible event stream is viewed at 1x and 2x for equal real time
- **THEN** 2x event production does not double the maximum admitted shake frequency
- **AND** pause freezes the active beat and its cooldown

### Requirement: Camera motion remains presentation-only
Accepted impact beats SHALL move battlefield visuals only. HUD, buttons, pointer hit regions, drag/drop geometry, modal layout, authoritative positions, snapshots, checksums, and battle outcomes SHALL remain unchanged.

#### Scenario: Player interacts during a shake beat
- **WHEN** a heavy or terminal beat is active while the player points, drags, pauses, or changes speed
- **THEN** the same authoritative UI and battlefield targets remain selectable
- **AND** no shake state appears in saved or deterministic battle state
