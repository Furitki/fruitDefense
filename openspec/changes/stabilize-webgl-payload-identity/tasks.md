## 1. Deterministic Web Payloads

- [x] 1.1 Enable Unity's no-unique-identifier production WebGL build option and keep per-payload content hashing authoritative.
- [x] 1.2 Move the grass and soil HD bake source textures outside runtime `Resources` with their `.meta` GUIDs preserved, then verify editor-profile references and runtime atlas loads.

## 2. Local Build Evidence

- [x] 2.1 Add reusable payload evidence helpers that report role, file, full SHA-256, content version, and byte length.
- [x] 2.2 Make the local Web pipeline build twice, fail on any payload mismatch, retain the second candidate, and emit schema-3 determinism evidence.
- [x] 2.3 Add focused pipeline self-checks for matching and mismatching payload maps.

## 3. Cross-Release Acceptance and Publication

- [x] 3.1 Add persistent-profile cache-seed mode and cache-seed evidence to WebGL portrait acceptance.
- [x] 3.2 Add candidate-versus-seed classification, unchanged-payload transfer assertions, and release-transition evidence to deployed acceptance.
- [x] 3.3 Seed the current public release before replacement, reuse that profile after replacement, and clean its temporary profile safely.
- [x] 3.4 Replace the fragile remote cache-header grep and emit schema-3 online publication evidence with release-delta measurements.

## 4. Validation and Delivery

- [x] 4.1 Run OpenSpec validation, PowerShell parser/self-checks, Unity aggregate editor smoke, and reference validation after the asset move.
- [x] 4.2 Build the same clean revision twice and confirm all four payload digests match; record the data-size reduction.
- [x] 4.3 Run local cold/warm portrait acceptance and confirm schema-3 local evidence.
- [x] 4.4 Publish ordinary WebGL, verify same-profile release-transition reuse plus deployed visual acceptance, and confirm schema-3 publication evidence.
- [x] 4.5 Update the P0 release baseline with the verified revision, payload versions, size, transition evidence, and ordinary-WebGL boundary.
