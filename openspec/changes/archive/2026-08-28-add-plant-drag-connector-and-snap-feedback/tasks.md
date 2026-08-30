## 1. Drag Feedback Geometry

- [x] 1.1 Capture a stable authoritative plant source rectangle separately from the pointer activation origin.
- [x] 1.2 Add allocation-free connector endpoint, edge trimming, dash, and target-frame geometry derived from existing source, preview, and target rectangles.

## 2. Shared Runtime Presentation

- [x] 2.1 Add shared runtime UI drawing for the semantic dashed connector and target-sized legal/illegal/merge/swap frame without adding an ArtSet slot.
- [x] 2.2 Integrate the overlay into active plant dragging, remove partial ghost interpolation, and preserve existing merge-hint and non-plant drag behavior.
- [x] 2.3 Replace the primitive four-edge target frame with the approved transparent-center production nine-slice UI resource.
- [x] 2.4 Project connector geometry into device space before rotation so PC letterboxing and fractional scaling do not offset or skew the dashes.

## 3. Validation and Trial Evidence

- [x] 3.1 Add deterministic editor coverage for source stability, free-drag endpoint, legal snap, illegal rejection, finite dash geometry, lifecycle cleanup, and semantic cue mapping.
- [ ] 3.2 Run focused checks, OpenSpec validation, and `FruitDefense.Editor.ProjectSetup.SmokeValidate`, fixing any regressions.
- [x] 3.3 Build ordinary WebGL and capture live 402×874 full/inset free-drag, legal-target, and illegal-target evidence for user trial review.
- [x] 3.4 Add deterministic 1280×720 letterbox projection coverage and refresh focused live evidence for the resource-backed target frame.
