## Verification scope

This record owns the verification matrix and final evidence for tasks 5.x and 6.x. The terrain-material laboratory is explicitly excluded from canonical map-editor acceptance.

## Automated matrix

| ID | Contract | Required observation | Status |
|---|---|---|---|
| A01 | Blank creation and bounds | An `8 x 7` asset has 56 visual and 56 gameplay records; every out-of-bounds mutation is rejected without changing serialized content | Pass |
| A02 | Resize and round trip | Resize retains in-bounds data, default-fills new cells, reports removed route/marker coordinates, and save/import/reload preserves source order, diagnostics, and gameplay fingerprint | Pass |
| A03 | Layer independence | Gameplay edits do not change presentation; presentation, area tools, and recommendation do not change gameplay, route, or marker bytes | Pass |
| A04 | Route and markers | Cardinal append succeeds, disconnected append is atomic, endpoints synchronize typed spawn/goal markers, and core/initial-pot identities remain stable | Pass |
| A05 | Editor operations and Undo | Rectangle, flood fill, eyedropper, recommendation, resize, and grouped gestures affect only bounded cells and one Undo/Redo restores/reapplies the aggregate | Pass |
| P01 | Negative publication | Incomplete coverage, out-of-bounds authored data, disconnected route, missing core, marker conflicts, duplicate IDs, and unknown templates all block publication with structured diagnostics | Pass |
| P02 | Manifest full rebuild | A/B rebuild, deterministic order, idempotence, unrelated-entry preservation, cancellation/removal, deleted-output recovery, and equivalent regeneration all pass | Pass |
| P03 | Atomicity and draft isolation | Failed rebuild leaves the last valid generated resource byte/content equivalent; draft edits remain absent until a later successful rebuild | Pass |
| P04 | Real palette gate | Missing base/landform binding, reverse-only refined edge, and release registry omission fail with map, coordinate, palette, pair/style diagnostics | Pass |
| R01 | Generated catalog reload | A saved/imported generated resource is reloaded and appended through normal catalog compilation; AppFlow resolves the expected authored `levelId` and `mapId` | Pass |
| R02 | Bundled regression | `orchard-01`, `orchard-02`, and `orchard-03` remain in stable order and compile unchanged with generated content present or absent | Pass |
| R03 | Terrain parity | Editor preview and Battle resolve identical base texture, landform tile set/mask, and exact directed-edge tile set/mask from the template's real palette | Pass |
| R04 | Aggregate marker | `FruitDefense.Editor.ProjectSetup.SmokeValidate` invokes focused canonical-authoring and publication smoke and emits stable success markers | Pass |

## Human and release matrix

| ID | Contract | Required evidence | Status |
|---|---|---|---|
| H01 | Fresh official authoring | A newly created bounded asset is completed, saved, reopened, published, and shown publish-ready in the official editor | Pass |
| H02 | Complete editor canvas | Evidence shows bounds plus gameplay, route/markers, presentation, diagnostics, dirty state, and publish state; no laboratory board is substituted | Pass |
| H03 | Normal Battle | The same published `levelId`/`mapId` runs in the release Battle route and demonstrates spawn, traversal, planting, core damage, and settlement | Pass (runtime view plus aggregate behavior evidence) |
| H04 | Release gates | Unity compilation, focused smoke, aggregate smoke, ordinary WebGL build/portrait run, and release-flow parity complete successfully | Pass |
| H05 | Spec validation | Strict change validation and aggregate OpenSpec validation complete with the stable game-design overview unchanged | Pass |

## Evidence

### Authored identity and publication

- Acceptance asset: `Assets/Battlefield/Maps/CanonicalEditorAcceptanceMap.asset`
- Publication manifest: `Assets/Battlefield/Maps/BattlefieldMapPublicationManifest.asset`
- Generated runtime resource: `Assets/Resources/Generated/PublishedBattlefieldMapCatalog.asset`
- Stable identity: `level.canonical-editor-acceptance` / `map.canonical-editor-acceptance`
- Final diagnostics: `grid=8x7`, `visualCells=56`, `gameplayCells=56`, `routeCells=7`, `markers=11`, `authoringBlocking=0`, `publicationBlocking=0`
- The manifest owns `levelId`, `templateLevelId`, and order; the generated resource contains the published deep copy and the source map asset contains none of those manifest fields.

### Automated validation

- `Builds/Evidence/canonical-map-editor/focused-smoke.log`
  - `CANONICAL_BATTLEFIELD_MAP_AUTHORING_SMOKE_OK`
  - `CANONICAL_BATTLEFIELD_MAP_EDITOR_SMOKE_OK`
  - `CANONICAL_BATTLEFIELD_MAP_PUBLICATION_SMOKE_OK`
  - `CANONICAL_FOCUSED_SMOKE_PASS`
- `Builds/Evidence/canonical-map-editor/aggregate-smoke.log`
  - all three canonical markers above;
  - terrain, layered-map, catalog, multi-level simulation, battle snapshot, host, shell, and full project markers;
  - terminal marker `FRUIT_DEFENSE_SMOKE_OK` followed by `CANONICAL_AGGREGATE_SMOKE_PASS`.
- `Builds/Evidence/canonical-map-editor/deleted-output-recovery.log`
  - `CANONICAL_BATTLEFIELD_MAP_DELETED_OUTPUT_RECOVERY_OK`
- Unity `6000.3.19f1` imported and compiled the project, then emitted `Build Finished, Result: Success.` for the ordinary WebGL artifact.
- OpenSpec commands: `openspec validate build-canonical-battlefield-map-editor --strict` and `openspec validate --all --strict`.

### Human/editor evidence

- `Builds/Evidence/canonical-map-editor/editor-gameplay.png`
- `Builds/Evidence/canonical-map-editor/editor-route-markers.png`
- `Builds/Evidence/canonical-map-editor/editor-presentation.png`
- `Builds/Evidence/canonical-map-editor/editor-validation-publish-ready.png`
- `Builds/Evidence/canonical-map-editor/canonical-editor.png`
- `Builds/Evidence/canonical-map-editor/final-diagnostics.log`
- The four official-editor workspaces were inspected at `1240 x 790`; each shows the same bounded map, real palette rendering, persistent selected asset/manifest, and zero blocking diagnostics. The terrain laboratory was not used as substitute evidence.

### Runtime evidence

- `Builds/Evidence/canonical-map-editor/playtest-prepared.log` proves the official Playtest gate rebuilt, saved/imported/reloaded, recompiled, and resolved the stable published identity before launch.
- `Builds/Evidence/canonical-map-editor/acceptance-runner.log` records the generated resource and successful ordinary WebGL build.
- `Builds/Evidence/canonical-map-editor/webgl-authored-battle.png` is a live portrait capture from `?acceptance=1&route=battle&level=level.canonical-editor-acceptance`; it shows the authored seven-cell route, eight initial pots, plantable grid, and core in normal Battle UI.
- The actual normal-Battle runtime view is combined with aggregate simulation/host/shell evidence for spawn, route traversal, planting interactions, core damage, terminal result, and settlement behavior. Browser console inspection found no gameplay/runtime exception; the only platform message was the expected unsupported desktop orientation-lock warning.
- Reproducible artifact: `Builds/Evidence/canonical-map-editor/webgl/index.html` and its `Build/` payload.

### SHA-256

| Artifact | SHA-256 |
|---|---|
| Acceptance map asset | `A05D1BD88FEB6DC2E61E9E10780AABEC1572FD3EB98E965FE7F1CB4C912FFF71` |
| Publication manifest | `7C0566EBD5C4DDD140D3C0DD396E3485B18821D349E3A37DB06EC44201802E77` |
| Generated catalog | `E2F1499E2CDEBD3403D445EA22F27A488752D9C49CB7D6ED340503499F2874C6` |
| Official-editor composite | `4975E7EF6BE7D7562EDDEBC1BB7F8B31DF65C70D46D7522808EC271CA31B33C0` |
| Authored Battle capture | `75D2AE46A5CA7C35766A8D6F495852578A206845E2FD3F1A7A21C5F8EF544131` |
| WebGL index | `20FB658D3F84C6429B5B948234EDC9B9E2095405B14108A9748983DBB293B820` |

### Documentation gate

- `docs/design/pending-design-review.md` contains the new proposal as `待审核` and records the terrain-only proposal as `已撤回`.
- `docs/design/game-design-overview.md` was not changed by this change. Stable design synchronization therefore remains pending explicit user approval.
