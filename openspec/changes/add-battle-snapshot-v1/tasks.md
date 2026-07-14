## 1. Snapshot Contract

- [ ] 1.1 Add JsonUtility-compatible V1 envelope and DTO arrays for every outcome-affecting battle value
- [ ] 1.2 Define structured restore result codes for schema, content, definition, reference, numeric, and identity failures
- [ ] 1.3 Add a stable outcome-state checksum used by continuation tests

## 2. Export and Restore

- [ ] 2.1 Export deep-copied simulation data without presentation, selection, drag, modal, or transient-effect state
- [ ] 2.2 Validate exact catalog/content/map identity, unique IDs, references, ranges, finite values, and next entity ID
- [ ] 2.3 Build a candidate runtime state and atomically commit it only after all validation succeeds
- [ ] 2.4 Restore logical step/random state and reset the frame accumulator to zero

## 3. Deterministic Validation

- [ ] 3.1 Add Ready, Playing, and BetweenWaves JSON round-trip tests
- [ ] 3.2 Add continuation tests during projectiles, burn, slow, ice count, and machine-gun burst
- [ ] 3.3 Add safe-failure tests for unavailable/mismatched content, duplicate entity IDs, and invalid references
- [ ] 3.4 Run OpenSpec validation, Unity compile, snapshot smoke, deterministic smoke, and project smoke
