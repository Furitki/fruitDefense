# 7.3 UI delivery cleanup

## Result

Task 7.3 removes only confirmed transient or rejected UI-delivery artifacts.
The approved Sunny Orchard release set, stable authoring/validation workflows,
every required canonical capture, and every defect-to-fix regression remain
intact. After the initial cleanup, the user explicitly rejected treatment B;
its production assets were removed in a bounded follow-up. A is still the only
active and now the only retained production set. Task 3.4 remains open.

## Deleted inventory

| Deleted item | Count | Reason | Recovery |
|---|---:|---|---|
| Unreferenced `server.stdout.log` / `server.stderr.log` under change evidence | 102 files / 5,304 bytes | Host access-process noise; README, manifest, JSON, TXT, and scripts contain zero references, while manifests already own HTTP/cache/payload metadata | Re-created automatically by the stable acceptance script on a new capture |
| `Assets/Editor/Tests/RuntimeUiWarmProfile.cs` and `.meta` | 2 files | Bounded task-7.2 `executeMethod` EditorWindow profiler, with no stable menu, P0 entry, runtime dependency, or continuing product role | Its five-scenario result and method are retained in `7.2-performance/warm-profile.json` and README; exact source can be restored only from task/source history or deliberately re-authored for a future measurement |
| Empty partial-run directories under `6.3-cross-route/regressions/parallel-overload-attempt` | 4 directories | After server-log cleanup the failed Battle, Flow victory/defeat, and ShellVisual attempts contained no evidence | The acceptance script recreates output directories; failure modes remain recorded in the regression README |
| Rejected treatment-B source/runtime/ArtSet assets under `Assets/UI` | 164 files / 454,305 bytes | User explicitly rejected the non-active treatment after its technical interchange review; rejected exports are not retained as compatibility inventory | Recoverable only from source history; historical style-board and interchange evidence remain outside `Assets` and cannot be activated |

The 102 server logs were distributed as follows: 3.5 Bootstrap/Lobby `20`, 4.6
Battle `38`, 6.3 cross-route `30`, 7.2 clarity `6`, 1.1 current UI `4`, and 5.3
Settlement `4`. The WarmProfile `.cs` and `.meta` were removed as one Unity
asset pair. No other Unity asset was deleted.

## Explicitly retained

- A: approved board, all lossless SVG masters, deterministic exporter,
  manifest, runtime PNGs/metas, ArtSet, gallery, and production evidence.
- Historical B review only: the style board, candidate gallery, preview logs,
  hashes, and isolation report remain as pre-rejection evidence. No B source,
  runtime PNG, importer metadata, manifest/exporter, or ArtSet remains under
  `Assets`.
- `RuntimeUiTheme`, `RuntimeUiArtSet`, shared GUI path, registry, Visual System
  window, validator, glyph authority, fixtures, and aggregate smoke coverage.
- Stable `scripts/accept-webgl-portrait.ps1` and the existing guarded acceptance
  bridge; no route or capture helper source changed.
- Canonical evidence for 1.1, 3.5, 4.6, 5.3, 6.3, and 7.2.
- All three directly referenced machine-readable audits:
  `seam-probes.json`, `visual-probes.json`, and `matrix-audit.json`.
- The three 3.5 defect regressions and all five 4.6 defect regressions,
  including the terminal `attempt-1` PNG.
- The 6.3 parallel-overload README plus its one under-load ShellError screenshot
  and manifest; only the four empty partial directories were removed.
- Battle world-only `DrawWorldLabel`, `DrawWorldRect`, and `DrawWorldOutline`.

The initial cleanup classified B as an intentional pending candidate. The later
explicit rejection changed that classification and authorized the bounded
164-file production-asset deletion above. Historical review and signed
acceptance evidence was not rewritten or deleted. The Settlement migration
evidence and all non-server build/editor logs remain because they still own
task-local implementation or validation history.

## Static and ownership audit

- server stdout/stderr remaining: `0`;
- lingering WarmProfile source references outside retained evidence docs: `0`;
- lingering `GeneratedInvalid*` files after P0: `0`;
- missing UI `.meta`: `0`; orphan UI `.meta`: `0`; duplicate Assets GUIDs: `0`;
- release runtime references to `ShellStyleSet`, `ShellGui`, runtime
  `Resources.Load<Font>`, or `GUI.skin`: `0`;
- rejected-treatment asset files and GUID declarations under `Assets`: `0`;
- release theme active ArtSet GUID:
  `12cc0c638d174040bb0384d7bf17ea92` (`SunnyOrchardRuntimeUiArtSet`);
- production registry inventory after rejection: A only; historical evidence
  may still quote the removed candidate identity and pre-rejection hashes;
- no UI runtime art, fixture, raw board, or candidate moved under `Resources`;
  the packaged Chinese font remains the intentional release font dependency.

Release dependency evidence remains owned by
[task 7.2](../7.2-performance/build-dependency-report.md): A contributes 38
runtime texture rows and one ArtSet, while the now-rejected candidate contributed
zero release texture or ArtSet dependencies even before deletion. Its footprint
row is explicitly historical. Task 7.3 does not rebuild or recapture the player;
the signed task-7.4 build remains an A-only release-dependency fact.

## Protected hashes

The first-cleanup values below were recorded before its deletion pass and
matched again after Unity P0. B-specific fingerprints are retained only as
pre-rejection history; the follow-up protects A, theme, scenes, fixtures, and
acceptance tooling while proving the B asset paths are absent.

| Protected file | SHA-256 |
|---|---|
| `Bootstrap.unity` | `27AD84F0D624DA6C1BE7152AD801990E6AE832E0A92019E6D585D35421E8ABD1` |
| `Lobby.unity` | `B4FA8E3B1656D1440A47D38FFA6B2E0CAD512E40DEE673D35EC39E505FDA2A6C` |
| `Battle.unity` | `C6CF5D7246B4FE21EB205FF0D7D740B3B5FE2C1D482D5721F13D75C311621C4E` |
| `Settlement.unity` | `FB6A7204EE71A9C38551920A88287F394EF45899974F599D8C89C6B6BC6569BC` |
| `ReleaseRuntimeUiTheme.asset` | `375990DC5E2C670AAE5B34212C27D9C83982C53C8CEEC88DEAE27E62AB18C911` |
| `SunnyOrchardRuntimeUiArtSet.asset` | `89BED0E1FC02B5FEA733996D87E680B06542FBD52A89F9E7A3E8A93CFCAA8E36` |
| rejected B ArtSet (pre-rejection fingerprint; now deleted) | `C0671BEA7D1938C2BE78525E35A356A6FEC55B8806BD31F1DA878A3016C901EA` |
| Runtime UI fixture README + meta tree | `12EB4F1DA8C2AA1ACBF2AE8AECE2140BCBF1D7FF645040123C0DB4F982F5B170` |
| `accept-webgl-portrait.ps1` | `9ACEDBA039C5B50E6672C5B9BBD2026823A4F26F88D4155D6AAD485E1F5509E5` |

Whole-tree digests also match before/after:

| Protected tree | Files | Aggregate SHA-256 |
|---|---:|---|
| A sources | 82 | `7CCBAC3826B0211D25FA655DF65369D8C6788F5168CFA0A1C7FEC23DCD327E9B` |
| A runtime | 78 | `5B3506A889EF24DE8D3689E12AA270032DEEE3F7B1ABC537637B7D546E36D2D4` |
| rejected B sources (pre-rejection fingerprint; now deleted) | 82 | `E59A7199F3C7182AEB35A3D3DF8D339D6BEDF2A6C013F52948FFD82343FAAEF9` |
| rejected B runtime (pre-rejection fingerprint; now deleted) | 78 | `B260032BA350C024EBFE3EB4CF075EFA4C05778DF68BC802D5D9FF54CC98EB6B` |

## Validation

- `openspec validate unify-runtime-ui-visual-system --strict`: pass;
- release visual validator: `Valid (0 warning(s))`;
- aggregate P0: `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK`,
  `RUNTIME_UI_GLYPH_COVERAGE_OK`, `RUNTIME_UI_PERFORMANCE_SMOKE_OK`,
  `FRUIT_DEFENSE_SHELL_OK`, and `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`;
- validator log SHA-256:
  `672B525651F9458027D733CC0429D751413D34B715763EDE2C70BF90CD94CBC9`;
- P0 log SHA-256:
  `EEEF51BE9D40804407E2D94E5FF83E535C08E2DF0C1B45B39429D55550A3BCEE`.

Both Unity invocations exited successfully and no Unity/browser/server process
remained afterward.

## Post-rejection follow-up verification

After the user rejected treatment B, the bounded production cleanup was
verified without rebuilding or recapturing the already A-only WebGL player:

- rejected production source/runtime/ArtSet paths under `Assets`: `0`;
- rejected asset GUID/path references in production `Assets` and stable UI
  documentation: `0`;
- production ArtSet definitions discovered on disk: `1` (`sunny-orchard@1`);
- release validator: `Valid (0 warning(s))`;
- aggregate P0: `RUNTIME_UI_SCREEN_BACKGROUND_OPAQUE_OK sets=1
  pre-fix-min-alpha=18`, `RUNTIME_UI_NINE_SLICE_SOURCE_UV_OK bindings=15`,
  plus visual-system, glyph, performance, Shell, and final release-gate markers;
- missing/orphan UI meta and duplicate Assets GUIDs: `0`; UI art under
  `Resources`: `0`; `GeneratedInvalid*`: `0`;
- protected theme, four scenes, A ArtSet, and their metas: `8/8` unchanged;
- protected A source tree: `82` files,
  `7CCBAC3826B0211D25FA655DF65369D8C6788F5168CFA0A1C7FEC23DCD327E9B`;
- protected A runtime tree: `78` files,
  `5B3506A889EF24DE8D3689E12AA270032DEEE3F7B1ABC537637B7D546E36D2D4`;
- signed task-7.4 build log contains zero rejected-candidate references and the
  preserved data/wasm hashes remain
  `F6EF5E73AE2C98AFEE676D099F7B3A0EDA2DCD946EF76362E456AAF759F24044`
  / `1E2AE62AB967ABA634BEF0D8AA3E5726EAB40CBF20ACBC7893FC4A43D55C7015`;
- post-rejection validator log SHA-256:
  `038AA41CD4D874A3E1D037BD181CE92396680887F8DFE2C18194721D23E5C35C`;
- post-rejection P0 log SHA-256:
  `C1F64A4786070C2B1F25750FA8FFD8708044DEB08F7985BEDD5D971EF5E62C36`.

Historical review assets and logs were deliberately preserved byte-for-byte:
the B style board remains
`1C96CA79DD89EBBE50BC6E61F12E2237D0B38150912BB35FC645735EA65E07EB`,
the candidate gallery remains
`18C79F881FB28C266EA4B4B77CBAC4DB46D3FB623B688BD014FD52383F838C32`,
the two interchange Unity logs remain
`74FAD1186B7E5950E3FE7768A782EB5EDAF9C4281817B834D6A66268226EC5E7`
and `15B96025C7B45CE21192544917143F9FAB2743DE960EA245E8EE29BA911D8A72`,
and the historical 3.5 WebBuild log remains
`7F1DD4B5BB487BFE864A5DB7B9E14084D7A7EAFC69E33346D39728F83C9FFEA9`.
They document pre-rejection review facts and are not current production assets.

## Post-rejection independent WebBuild confirmation

After the cleanup and the post-rejection validator/P0 run above, Unity
`6000.3.19f1` independently executed `FruitDefense.Editor.WebBuild.Build`.
The [new build log](../7.4-final/webgl-build/post-rejection-webgl-build.log)
records `DisplayProgressNotification: Build Successful`, `Build Finished,
Result: Success.`, `FRUIT_DEFENSE_WEB_BUILD_OK`, and batch return code `0`.
Its SHA-256 is
`08F83655004D85810159BBD34FF1F5167CFC5C7386CB5915E98D478EB97FEA89`.

The complete payload comparison is byte-identical to the signed task-7.4
A-only build:

| Payload | Bytes | Post-rejection SHA-256 | Signed SHA match |
|---|---:|---|---|
| `WebGL.loader.js` | 117,893 | `851FCDDE61EEF1484651F2967DACAA331AC377B0B8466D78C7557AAF1E0A507E` | yes |
| `WebGL.data.unityweb` | 4,838,224 | `F6EF5E73AE2C98AFEE676D099F7B3A0EDA2DCD946EF76362E456AAF759F24044` | yes |
| `WebGL.framework.js.unityweb` | 69,023 | `628DED04A7AF9570E4032627EDCF6F973928EA5E8AA51C10F5ED97150760FC77` | yes |
| `WebGL.wasm.unityweb` | 3,860,177 | `1E2AE62AB967ABA634BEF0D8AA3E5726EAB40CBF20ACBC7893FC4A43D55C7015` | yes |

`index.html` also remains
`B9DA71958FB5C463F1E146DC9904FCAF034ED47841F1D926DD6B652E22776091`.
The four payloads total `8,885,317` bytes, and the build marker again reports
`8,905,120` bytes for the complete output.

The new build log contains zero `orchard-woodcraft`, `OrchardWoodcraft`, or
rejected GUID `6f4a78a849e74f4798e42b71a1239056` occurrences. The release theme remains
bound only to A GUID `12cc0c638d174040bb0384d7bf17ea92`; the protected theme,
A ArtSet, Bootstrap, Lobby, Battle, and Settlement hashes remain exactly the
values recorded above. `GeneratedInvalid*` remains absent and Unity exited.

Because every delivered payload and `index.html` is byte-identical to the
signed A-only player, the existing `85` canonical PNG captures still correspond
to the exact rebuilt player; no browser recapture is necessary. This build
confirmation does not change the rejected treatment into an approved candidate,
and task 3.4 remains unchecked.
