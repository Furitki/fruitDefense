## 1. Presentation Event Contract

- [x] 1.1 Add immutable presentation event kinds and payloads with local sequence, logic tick, stable cue/visual IDs, entity IDs, position, duration, and feedback data.
- [x] 1.2 Add a bounded simulation-owned event stream with ordered emission, destructive drain, explicit discard, and reset semantics.
- [x] 1.3 Remove presentation event, combat-effect, feedback lifetime, and delivery state from authoritative `GameState`.

## 2. Simulation Producers and Persistence Boundary

- [x] 2.1 Route skill cues and battle feedback through the presentation event stream while preserving cue-to-visual mapping and durations.
- [x] 2.2 Clear pending presentation delivery on battle reset and successful snapshot restore without changing snapshot v1 or outcome checksum.
- [x] 2.3 Migrate composable-skill, snapshot, and project fixtures from transient `GameState` lists to the event boundary.

## 3. Battle View Consumer

- [x] 3.1 Add a presentation-only buffer that consumes events and owns effect/feedback lifetime independently of fixed simulation steps.
- [x] 3.2 Update `FruitDefenseGame` to drain events, draw from the local buffer, and clear it at restart, restore, simulation replacement, and acceptance-state boundaries.
- [x] 3.3 Preserve the existing immediate-mode layout, safe-area geometry, inputs, and combat visual mapping without adding presentation-to-simulation callbacks.

## 4. Boundary Validation

- [x] 4.1 Add a dedicated smoke for event order, stable identifiers, single consumption, explicit discard, bounded overflow, and reset behavior.
- [x] 4.2 Prove snapshots/checksums exclude presentation data and consuming or omitting events cannot alter deterministic battle results.
- [x] 4.3 Run dedicated boundary, composable-skill, snapshot, deterministic, P0 suite, and project smoke validation.
- [x] 4.4 Build WebGL and pass the existing 13-state acceptance runner with unchanged manifest and capture surfaces.
- [x] 4.5 Run strict OpenSpec validation and record all tasks complete.
