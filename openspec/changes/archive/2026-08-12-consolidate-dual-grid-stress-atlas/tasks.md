## 1. Single-Image Stress Artifact

- [x] 1.1 Add fixed stress panel metadata and deterministic single-image atlas assembly.
- [x] 1.2 Replace the four per-scenario PNG writes and `stressMaps` manifest field with one manifest-declared stress atlas and a decodable `stressAtlas` record.
- [x] 1.3 Replace multi-file validation with whole-atlas and independent quadrant reconstruction while preserving adjacency evidence.

## 2. Compatibility and Automated Coverage

- [x] 2.1 Remove obsolete per-profile stress filenames and update unit tests for dimensions, quadrant order, exact pixels, metadata, and corruption detection.
- [x] 2.2 Run the Python unit suite and strict OpenSpec validation.

## 3. Documentation and Regression Evidence

- [x] 3.1 Update the formal Dual-Grid pipeline and output contract to define one-image pressure review and its limits.
- [x] 3.2 Rebuild and validate the Design KB static snapshot.
- [x] 3.3 Regenerate A and B evidence in a new versioned directory, validate both runs, and publish the two single pressure-atlas paths without performing visual acceptance.

## 4. Compact Density Revision

- [x] 4.1 Revise the contract to use four 16×16 native-Runtime panels in one 1024×1024 atlas.
- [x] 4.2 Implement the 17×17 vertex formulas, compact atlas metadata, sixteen-mask assertion, and corruption coverage.
- [x] 4.3 Update the formal pipeline and rebuild and validate Design KB.
- [x] 4.4 Regenerate and validate A/B compact evidence without performing visual acceptance.
