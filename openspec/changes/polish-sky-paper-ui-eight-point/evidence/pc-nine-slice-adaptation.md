# PC fractional-scale nine-slice adaptation evidence

## Scope

This correction changes only the shared runtime nine-slice renderer and its
validation. It does not change the approved raster bytes, semantic colors,
layout rectangles, hit rectangles, copy, or gameplay behavior.

## Implementation

- `RuntimeUiGui.Art.cs` aligns each complete nine-slice destination through
  `GUIUtility.AlignRectToDevice`, converts it once to screen space, and submits
  one `Graphics.DrawTexture` call under an identity GUI matrix.
- `RuntimeUiNineSlice.shader` remaps that single quad across independent source
  and target border coordinates. Each source region terminates at texel centers,
  so filtering does not cross a slice partition.
- The former nine independent `GUI.DrawTextureWithTexCoords` patches, manual
  `GUI.matrix` snapper, source-texel expansion, and destination overlap guard
  are deleted rather than retained as a fallback.
- The shader is an always-included production shader for both Windows and
  WebGL builds.

Implementation identities:

| Artifact | SHA-256 |
| --- | --- |
| `Assets/Scripts/UI/RuntimeUiGui.Art.cs` | `50FA313ABA710E5AA2465C0A21423D6F117116A015989A270383DA8B74394551` |
| `Assets/UI/RuntimeUiNineSlice.shader` | `21E9B3E2AC0DDE56AB6886EA22EAFC7FF4820D1EAF3A25E7F00E0B1D77BBC41F` |

## Automated validation

- Focused `RuntimeUiVisualSystemSmoke.Run`: pass.
  - one nine-slice GPU draw is structurally required;
  - the obsolete independent-patch path is absent;
  - source sampling is partitioned at texel centers;
  - device borders are integral and contained at `1280×720`
    (`720/874 = 0.823798627`), `720×1280`, logical `1×`, `2×`, `3×`, a scaled
    Shell context, and a target smaller than the protected border sum;
  - all 20 production nine-slice source bindings pass the UV/source-boundary
    contract.
- Deterministic UI export: pass twice from unchanged fixed masters; all 234
  tracked source/runtime/manifest/ArtSet files have identical hashes between
  the two runs.
- Windows x64 build: pass. The Windows build log compiles and serializes
  `Hidden/FruitDefense/RuntimeUiNineSlice` with no shader error.
  `Assembly-CSharp.dll` SHA-256:
  `7B03DCE26D81A6925E27463315A03B270BCE4B6431A1BAF169A1269DCF2AA1E8`.
- Ordinary WebGL release build: pass as a compile-compatibility check only.
  The GLES3 shader variant compiles successfully. This is not used as evidence
  for the PC-only visual defect. `index.html` SHA-256:
  `6FD6F70C72E71A5582AD0AB38DBC3BA5E95C825564D700B22E849C0C1AB5DCF8`.
- Aggregate `ProjectSetup.SmokeValidate`: blocked twice by the unrelated dirty
  combat-feedback worktree at
  `CombatFeedbackSdfRenderSmoke.ValidateFixtureBounds` with
  `Combat feedback SDF render smoke failed: role-route prepares every admitted label`.
  The focused runtime-UI suite completes before this external failure.

## Real Windows D3D12 evidence

The newly built Windows player was launched at a `402×874` client size, entered
Battle through the ordinary Lobby flow, and then the same live window was
resized to an exact `1280×720` client area. The captures therefore exercise the
Standalone resize/D3D path rather than WebGL CSS scaling.

| State | Capture | SHA-256 |
| --- | --- | --- |
| Battle ready, `402×874` | `pc-402x874-battle-single-draw.png` | `79CD8E683E8A5C19F41E7BF9995C818F534158C3181EFF2EBC2938FB55E2D668` |
| Battle ready after live resize, `1280×720` | `pc-1280x720-battle-single-draw.png` | `4EA2A840710C8DFE42AA5850B60B9C452BC4FAD100E00B3AB9D9E161C6FE1233` |
| Start-wave hover, `1280×720` | `pc-1280x720-action-hover-single-draw.png` | `3E582305409CA7593E502BD1BF3F4FAF22508C1C53525010A81177F9161722E9` |
| Empty-nursery click pulse, `1280×720` | `pc-1280x720-nursery-pulse-single-draw.png` | `64628DD0F1912887BEA942336C14F9EF4C6FA26DB27BB293ED87A82C9F96293A` |

The candidate keeps the reference-authored nursery solid outer rim and orange
dashed inner rail. No runtime selection marker or four-segment outline is
reintroduced. Final subjective visual approval remains user-owned, so tasks
5.5 and 5.6 remain open until the unrelated aggregate blocker is cleared and
the user reviews this PC evidence.
