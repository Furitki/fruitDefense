# Final production-resource audit for tasks 4.4 and 4.5

This is a read-only resource audit of the merged post-polish workspace. It did not start Unity, a browser, an exporter, or a build, and it does not mark either task complete. The machine-readable results are in `audit.json`.

## Result

The production resource graph passes its release contract:

- both `sunny-orchard@1` and `sunny-orchard-painted@1` serialize exactly 49 unique semantic slots, slots 0–48 once each;
- painted owns 47 unique exports; old Sunny Orchard owns 38 unique exports for its original 40 rows and explicitly shares nine composition rows from painted, resolving to 47 unique runtime Sprites;
- the two sets have no semantic, geometry, slice, safe-inset, or pixels-per-logical-unit differences;
- all 85 physical production runtime PNGs and all 85 source PNG/SVG masters are manifest-owned, with no unbound production art;
- every ArtSet binding resolves to `Assets/UI/Art/Runtime/**`, every manifest hash and runtime meta GUID matches, importer violations are zero, and all 954 Assets GUIDs are unique;
- `ReleaseRuntimeUiTheme` remains `ui.sunny-orchard@1` and activates only `sunny-orchard-painted@1` through ArtSet GUID `91aa538ae02449cba8c971ffe4d427eb`;
- release Theme, ArtSets, scenes, and prefabs reference no Approved/Review board GUID and no source/evidence path;
- visible exact magenta, required-opacity, transparent-edge, safe-inset, stale production file, `GeneratedInvalid`, UI-scope `__pycache__`/`.pyc`, and capture-server log scans are clean;
- both production sets satisfy the same common-icon thresholds. Old Sunny Orchard has a 60–71 px family range and 3.945 px maximum centroid offset; painted has a 70–71 px range and 3.909 px maximum. All four drag-cue set/semantic combinations meet alpha-box, smallest-draw, and 7 px source-stroke witness gates;
- painted determinism evidence still matches every current final hash; old-set double-export evidence is present; the final release validator is `Valid (0 warning(s))` and aggregate P0 records `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`.

The replaceability conclusion is positive: either complete ArtSet can satisfy the same runtime slot contract without filename loading, fallback, scene edits, route branches, or layout changes. Release remains on painted; this audit does not activate the old candidate.

## 4.4 retention decision

Task 4.4 is complete. Its authoritative cleanup and retention record is
[`../4.4-cleanup.md`](../4.4-cleanup.md). The following files are deliberately
retained as part of the final evidence and authoring workflow:

1. `prompt-record.json`, `icons/prompt-record.md`, and
   `icons/alignment-audit.md`, with their paired metas, are stable authoring
   provenance for the reproducible painted masters. The owning set README
   documents this contract, the manifest ancillary/resource inventory
   classifies the files, and release-reference inspection reports zero
   dependencies on them. They are not raw or rejected production art.
2. `evidence/resource-inventory/build_inventory.py` is retained because the
   resource-inventory README links it as the reproducible collector for the
   machine-readable inventory. It is evidence tooling, not a runtime or
   disposable Unity Editor helper.

The selected image-generation raw used for the legal-drag correction is correctly retained only under `evidence/resource-polish/`; it is useful provenance and must not be moved into production or deleted as an unreferenced product asset.
