# Sunny Orchard Painted WebGL preflight

Status: **FAILED / not accepted**. These are regression captures, not canonical release evidence.

The exact painted WebGL payload was checked at the required 402-by-874 full and 44/34 inset cases before the larger 8-plus-10 matrix. Both real captures show the new paper texture, amber selected surface, painted corner leaves, and painted Start icon, but the compact release UI still reads as the same scripted rectangle stack with a material swap. Hiding the text leaves one outer rectangle, three near-identical level rectangles, and one button rectangle; the orchard hierarchy from the approved board and v2 proof is not present.

The preflight also fails the existing primary-action contrast floor. Actual rendered pixels in the central label region measure 2.22:1 full and 2.47:1 inset, below the required 3.0:1. The detailed coordinates, colors, hashes, active identity, and payload hashes are recorded in `preflight-audit.json`.

## Evidence

- `preflight-fail/full-402x874/00-bootstrap-initializing.png`: real application-owned Bootstrap initializing frame. The large safe surface and modal remain thin rectangular frames with only tiny corner decoration.
- `preflight-fail/full-402x874/01-lobby-default.png`: real 402-by-874 full Lobby default state.
- `preflight-fail/inset-402x874-44-34/01-lobby-default.png`: real 402-by-874 inset Lobby default state.
- No acceptance manifest exists because each ShellVisual run stopped at the primary-action actual-pixel contrast gate. The larger matrix was intentionally not executed after the visual hard failure.

The capture script used `-ServeLocal` and its own random ports (49495 and 49600). It did not use, stop, or overwrite the user-visible server on port 4173. Generated server stdout/stderr were removed after capture; the screenshots and audit are self-contained.

## Hard-fail observations

1. **Scripted silhouette remains.** `surface.safe-area` and `surface.card-selectable` are optically one-pixel rectangular outlines. The only strong orchard cues are tiny corner leaves and one painted play icon, so the page does not remain identifiable as the v2 orchard direction when copy is hidden.
2. **Approved information hierarchy is missing.** The approved/v2 reference depends on level thumbnails, a leaf/title ribbon, grouped orchard metrics, and a result banner. The current 40-slot contract has no illustration/frame/ornament vocabulary for those roles, so replacing the same generic surfaces cannot reproduce that hierarchy.
3. **Primary CTA contrast fails.** Full: background `(89,108,24)`, rendered label `(164,159,146)`, 2.22:1. Inset: `(101,124,22)` against `(194,187,171)`, 2.47:1. This is an actual-pixel failure, not a fixed-palette mismatch.
4. **Selection cue is underweighted.** The selected-card check/leaf badge is a small corner mark relative to the 364-by-74 / scaled inset card; it does not carry the 36-to-44px optical weight of the approved v2 check medallion.
5. **Texture alone is doing the work.** Paper grain and hand-drawn edge variation are visible, but the component anatomy, depth, and decorative hierarchy remain unchanged. This is a skin replacement, not the approved painted composition.

## Minimum viable semantic additions

The following are the smallest new finite roles that can express the approved composition without baking Chinese copy into sprites or putting route-specific illustrations into generic nine-slice centers:

| Proposed role | Ownership / geometry | Required draw point inside current Rects |
|---|---|---|
| `ornament.screen-corner` | ArtSet, transparent fixed-aspect ornament; anchor/mirror without stretching | After `DrawSafeArea` in Bootstrap, `LobbyPresenter.OnGUI`, and `SettlementPresenter.OnGUI`; four safe-area corner anchors, no hit rect |
| `ornament.section-ribbon` | ArtSet, fixed-aspect leaf ribbon with empty text-safe center | Under the existing title Rect in Lobby/Settlement and under modal title in `FruitDefenseGame.DrawModal`; text remains runtime CJK |
| `surface.illustration-frame` | ArtSet, nine-slice frame whose leaves stay entirely in protected corners | Inside each existing Lobby card before copy; frame surrounds a left thumbnail sub-Rect without changing the card hit Rect |
| `ornament.metric-divider` | ArtSet, transparent fixed-aspect divider | Between the three existing metric Rects in `FruitDefenseGame.DrawHeader` and Settlement result metrics; no new interaction geometry |
| `ornament.result-banner` | ArtSet, fixed-aspect banner with empty text-safe center | Top band of the existing Settlement `ResultCard` and Battle terminal result card before outcome/title text |
| `illustration.orchard-vista` | ArtSet, fixed-aspect shared chrome illustration, never nine-sliced | Clipped behind the upper/middle portion of the existing result/modal Rect; metrics/actions remain above it |

Level thumbnails themselves must remain content-owned (`LevelPresentationThemeDefinition` or the release level catalog), not part of the swappable application ArtSet. Add one `lobbyThumbnail` content reference per playable level and draw it in the `surface.illustration-frame` sub-Rect from `LobbyPresenter.DrawLevelCard`. This preserves the application/content-art boundary while making the approved level identity visible.

Recommended compact Lobby card allocation inside the existing hit Rect: 8px inset, 84-by-54px thumbnail/frame at left, 10px gap, remaining width for the two existing single-line text rows, and a 32-to-36px selected medallion anchored at the top-right. If the two rows cannot fit without clipping at 360 inset, the card anatomy—not only the texture—must be revised and revalidated across all supported viewports.

## Acceptance condition for the next preflight

- Hidden-copy silhouette must still show orchard title/thumbnail/selected/result hierarchy, not four generic rectangles.
- Primary CTA rendered contrast must be at least 3.0:1 in full and inset actual pixels.
- Leaf/fruit ornaments must stay out of nine-slice stretch centers and remain continuous on all four inner/outer boundaries.
- Level thumbnail, text, and selected medallion must not collide at 360/375/402/430 full or inset.
- Icon optical boxes and CJK baselines must remain within 2px of their intended row center; no sprite-baked Chinese.
- The same theme and one complete ArtSet must remain active across Bootstrap, Lobby, Battle, and Settlement with no fallback or mixed set.

## Validation performed

- `scripts/accept-webgl-portrait.ps1 -SelfCheck`: passed and reported `ui.sunny-orchard@1 / sunny-orchard-painted@1`, GUID `91aa538ae02449cba8c971ffe4d427eb`.
- Both 402 ShellVisual preflight runs reached the real Lobby canvas and captured screenshots, then failed the CTA contrast gate.
- WebGL loader/data/framework/wasm SHA-256 values match the handed-off painted payload exactly.

