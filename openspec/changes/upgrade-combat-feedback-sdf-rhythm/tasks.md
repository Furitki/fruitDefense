## 1. Editor SDF Bake and Atlas Assets

- [x] 1.1 Use Unity 6000.3's resolved TextMeshPro editor dependency only as the maintained transient SDF generation source
- [x] 1.2 Add the stable `Fruit Defense/...` deterministic combat atlas generator using the packaged Noto Sans SC source and finite glyph inventory
- [x] 1.3 Generate and commit one static 512-by-512 RGBA32 atlas plus metadata, including the finite reviewed composite-token inventory
- [x] 1.4 Add build-time validation for source font, glyph/token coverage, RGBA32 atlas dimensions, transparent packing boundary, owned asset paths, and zero runtime materials

## 2. Pooled Final-Layer Atlas Runtime

- [x] 2.1 Add a fixed 12-record final-IMGUI-layer compositor with 192 preallocated atlas commands and no Canvas, render target, camera, mesh, material, or per-label renderer objects
- [x] 2.2 Map active presentation records to pooled slots, build fixed draw ranges in place, resolve hot copy through O(1) longest composite tokens, and drive role size/fill/alpha/scale without material instances
- [x] 2.3 Transform reference anchors through the existing safe-area viewport layout and keep the overlay synchronized across portrait size changes
- [x] 2.4 Delete the legacy glyph-request, fill/outline GUIStyle arrays, four-offset outline, and IMGUI floating-text fallback path

## 3. Contact Anchors and Compact Lanes

- [x] 3.1 Add the 0.12-second live-target follow resolver with event-position fallback, same-tick defeat centroid, and presentation-only detach behavior
- [x] 3.2 Reduce deterministic lane separation to the 0/14/28 vertical and at-most-8 horizontal contract
- [x] 3.3 Reduce the defeat band to 26 reference pixels while preserving upper-edge inward unfolding and fatal-number suppression

## 4. Global Impact-Beat Scheduler

- [x] 4.1 Replace raw profile shake amplitude/duration with finite None, Heavy, Cluster, and Terminal beat roles and migrate bundled profiles
- [x] 4.2 Implement one pause-aware unscaled global scheduler with semantic replacement, cooldown, bounded amplitude, and a low-cycle damped envelope
- [x] 4.3 Admit shake only for reviewed heavy impacts, compact defeat clusters, and boss defeat; retain local feedback for ordinary/control/resource events
- [x] 4.4 Remove additive surface-motion records and prove battlefield-only offset leaves HUD and authoritative interaction geometry unchanged

## 5. Automated Validation

- [x] 5.1 Add focused atlas bake, composite-token, legacy-path removal, pool/command/material, safe-area transform, glyph, and warm-allocation Editor checks
- [x] 5.2 Add contact-distance, follow/detach, compact-lane, defeat-band, upper-edge, and missing-target Editor checks
- [x] 5.3 Add ordinary-no-shake, cluster promotion, heavy/terminal priority, cooldown, pause/1x/2x, non-additive amplitude, and low-cycle envelope Editor checks
- [x] 5.4 Run strict OpenSpec validation and the focused combat-feedback/runtime-UI smoke suites

## 6. Release Acceptance

- [x] 6.1 Run `FruitDefense.Editor.ProjectSetup.SmokeValidate` and build ordinary WebGL with `FruitDefense.Editor.WebBuild.Build`
- [x] 6.2 Capture real 402-by-874 WebGL grass/route role coverage, rebound, clustered anchors, dense 1x/2x, heavy/cluster/boss beats, and interaction evidence
- [x] 6.3 Record final-raster outline/position/rhythm acceptance plus p95 render time, steady allocation, atlas-page, material, and pool results
