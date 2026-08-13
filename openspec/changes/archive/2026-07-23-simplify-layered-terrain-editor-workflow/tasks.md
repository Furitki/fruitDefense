## 1. Author-facing material metadata

- [x] 1.1 Add bounded editor-facing metadata for material A and B display names, thumbnails or swatches without changing their runtime terrain identities
- [x] 1.2 Configure the layered terrain demo/sample to present grass and soil through that metadata and report incomplete authoring profiles clearly
- [x] 1.3 Add validation coverage for complete, missing-name, missing-preview, and duplicate/invalid material presentation configuration

## 2. Shared Scene paint session

- [x] 2.1 Extract editor-only target, active-tool, hover, painting, dirty-marking, and teardown behavior into one paint-session controller that delegates to `LayeredTerrainTilemap`
- [x] 2.2 Group every mouse-down-to-mouse-up drag into one Undo operation, skip duplicate cells within the gesture, and verify Undo/Redo restores all canonical and generated layers
- [x] 2.3 Release Scene input on stop, Escape, invalid target, window disable, play-mode transition, and script reload while leaving authored terrain unchanged
- [x] 2.4 Show the semantic active-brush label and affected-cell outline in Scene view for paint, erase-landform, and clear-cell tools

## 3. Dedicated terrain painter workflow

- [x] 3.1 Add the map-tools menu entry and painter window with selected-target resolution, sole-candidate resolution, and explicit selection for ambiguous scenes
- [x] 3.2 Add four visual preset cards for pure A, pure B, A on B, and B on A using configured names/previews and the existing validated paint operations
- [x] 3.3 Add contextual base-edge and AI-refined-edge choices only for landform tools, including exact directed-pair disabled reasons and no silent substitution
- [x] 3.4 Add a collapsed advanced section for landform-only A/B, erase-landform, clear-cell, and concise empty-base guidance
- [x] 3.5 Replace the ordinary custom Inspector workflow with configuration status, rebuild, painter launch, and a collapsed developer-configuration section

## 4. Automated editor acceptance

- [x] 4.1 Add focused tests for target resolution, semantic metadata, all four preset-to-layer mappings, contextual edge visibility, missing direction, and empty-base rejection
- [x] 4.2 Add focused tests for explicit erase scopes, one-gesture Undo/Redo, duplicate-cell suppression, session teardown, and generated-output ownership
- [x] 4.3 Extend `FruitDefense.Editor.ProjectSetup.SmokeValidate` and the layered demo validation to require an author-ready painter profile without changing release scene order

## 5. Visual and release-parity acceptance

- [x] 5.1 Run Unity batch compilation and all focused/aggregate editor smoke validation with no new errors
- [x] 5.2 Capture and inspect the Unity editor painter showing the four semantic presets, active-brush summary, contextual edge choice, advanced tools, and Scene feedback without raw asset selection
- [x] 5.3 Paint the accepted layered sample through the new front end, build ordinary WebGL through `FruitDefense.Editor.WebBuild.Build`, and verify portrait terrain plus `Bootstrap → Lobby → Battle → Settlement` parity
- [x] 5.4 Record evidence, run strict change and aggregate OpenSpec validation, and leave the change ready for user review and archive
