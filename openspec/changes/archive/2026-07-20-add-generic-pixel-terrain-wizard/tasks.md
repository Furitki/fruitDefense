## 1. Generic profile contract

- [x] 1.1 Add backward-compatible manual/imagegen provenance and single/two-source layout fields plus a generic profile configuration API.
- [x] 1.2 Add stable terrain-id normalization and per-profile atlas/report path helpers that preserve PixelGrass filenames.

## 2. Generic bake and validation

- [x] 2.1 Configure and validate arbitrary manual or imagegen source assets during bake without copying, drawing, or overwriting source PNGs.
- [x] 2.2 Emit and validate profile-specific derived assets, TileSets, atlases, and JSON evidence.
- [x] 2.3 Make aggregate pixel validation discover all profiles and report the exact offending profile while retaining the public project-smoke entry.

## 3. Authoring wizard

- [x] 3.1 Add a Unity editor wizard for manual one-source and grass-plus-soil creation with unique profile/output ownership.
- [x] 3.2 Add imagegen request JSON generation, copyable Codex handoff, missing-source status, and refresh without any scripted bitmap fallback.
- [x] 3.3 Add Bake, Validate, Reveal Output, and Undo-safe Apply to Selected `DualGridTilemap` actions.

## 4. Acceptance

- [x] 4.1 Add editor smoke coverage for generic profile configuration, one/two-source behavior, imagegen request constraints, independent evidence, and selected-map application.
- [x] 4.2 Regenerate PixelGrass and prove its source/profile/pixel hashes and legacy paths remain stable.
- [x] 4.3 Exercise a second terrain id, inspect the wizard UI and independent native evidence, then run final Unity project smoke and strict OpenSpec validation.
