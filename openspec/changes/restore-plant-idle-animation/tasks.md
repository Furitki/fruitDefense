## 1. Runtime and Regression Coverage

- [x] 1.1 Add subtle deterministic idle motion to planted-fruit drawing while preserving hit geometry and existing attack poses
- [x] 1.2 Add an editor regression that proves a durable in-range target causes the same plant to begin a second basic attack
- [x] 1.3 Make per-skill fixed-tick cooldown state authoritative after initialization so stale legacy mirrors cannot prevent rearming
- [x] 1.4 Upgrade repeated-attack coverage to use a resolved bundled level, real frame ticks, actual damage, and every attacking plant kind

## 2. Validation and Release

- [x] 2.1 Run the focused combat regression and the unified P0 Unity release gate
- [x] 2.2 Build a fresh ordinary WebGL artifact and pass local portrait acceptance
- [x] 2.3 Publish through the approved online pipeline and pass remote health, header, and portrait acceptance checks
- [x] 2.4 Run the upgraded focused combat regression
- [x] 2.5 Pass the unified P0 Unity release gate with the current 64x64 battlefield base-texture baseline
- [x] 2.6 Build and publish the authoritative cooldown fix through the approved online WebGL pipeline from a reviewed clean release revision
