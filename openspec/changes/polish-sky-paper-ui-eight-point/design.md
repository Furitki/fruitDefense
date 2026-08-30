## Context

The accepted Battle baseline uses one finite 56-slot ArtSet, role-level packaged Chinese fonts, and the approved floating Header/page-shell composition. The user rated the first ImageGen visual `8.5/10`, the production solid-background extraction `7.5/10`, and the composed page about `7/10`; the specific accepted observation is that the current green is too dark and less fresh than the reference. The framework, layout, typography, copy, and hit geometry are accepted authorities, so the remaining work is direct replacement of selected surface pixels rather than another page decomposition or layout pass.

This increment is presentation-only. Explicitly supplied or user-approved reference-image visual parameters are P0 authority for the affected visual scope. Runtime copy, semantic icon identity, commands, gameplay content, hit targets, and any visual parameter not defined by the reference remain project-owned authorities.

## Goals / Non-Goals

**Goals:**

- Restore the accepted light leaf/lime action master and pair it with soil-brown content at contrast of at least `4.5:1` in final composed pixels.
- Prevent generic visual guidance or accessibility checks from changing a reference-authoritative raster; resolve contrast through the separate text/icon content token first.
- Remove the four-line focus treatment and PC-scale nine-slice seams without changing authoritative draw or hit geometry.
- Replace the most visible script-authored page, container, card, slot, and stage-frame materials with reviewed ImageGen pixels so their rims, highlights, shadows, and painted texture integrate with the accepted buttons.
- Preserve exactly one active ArtSet, stable semantic slots, destination paths/GUIDs, importer geometry, text-free rasters, and deterministic export from fixed reviewed bytes.
- Produce a same-payload 402×874 full/inset Battle comparison and require explicit user scoring before claiming the 8-point target.

**Non-Goals:**

- Changing `BattleUiLayout`, `BattlefieldProjection`, safe-area topology, typography, copy, icons, action semantics, commands, gameplay simulation, balance, content art, persistence, or route flow.
- Programmatic hue shifts, gradient painting, border reconstruction, shadow synthesis, compatibility resources, alternate ArtSets, or fallback material recipes for the selected generated surfaces.
- Claiming ordinary WebGL evidence as Douyin or WeChat support.

## Decisions

### 0. Reference parameters are P0 visual authority

For the affected visual scope, every visual parameter that is explicitly supplied or approved in a reference image—such as color, luminance, shape, proportion, rim, outline, shadow, or texture—is immutable P0 input. The generic visual system is a design guide only when there is no reference image, or for parameters the reference does not define. It cannot overrule, average, or “correct” a reference parameter.

This does not make the reference file a release dependency and does not transfer ownership of runtime copy, commands, semantic icon identity, gameplay content, or hit geometry. Production still uses project-owned masters and runtime exports; their reference-controlled pixels are verified rather than rediscovered by a generic palette rule.

### 1. Use reviewed directly replaceable ImageGen masters

Each distinct material family is generated as one text-free, icon-free, directly replaceable component master. The current slotted PNG supplies geometry and alpha intent only when its semantic and silhouette remain authoritative; the supplied page reference supplies color, rim, shadow, texture, and integration direction. Semantic slots may share a master only when their final edge, outline, shadow, alpha, anatomy, and material contracts are identical. A compact line-free metric therefore cannot reuse a bordered large-panel master merely because both are warm paper.

Decomposition sheets, multi-component generation grids, and a second “change the background to a solid color” generation were rejected because they add latency, merged-component risk, and repeated loss between generation and production. Prompt regeneration during export remains rejected because it is nondeterministic and would weaken provenance.

### 2. Preserve generated colors and material pixels through direct integration

The exporter validates each individual master hash, crops the complete component, uses native alpha when present, adds transparent padding, resizes with alpha-safe sampling, measures optical bounds, hashes, and exports. When ImageGen returns a baked checkerboard or opaque background, the only permitted correction is one deterministic connected-background cleanup or one separately hash-locked geometry-alpha mask from the current slotted master. It does not recolor, draw, repair, or synthesize material pixels. Any required color or anatomy correction returns to ImageGen and replaces that one reviewed master.

This boundary directly follows the user's approved production method and avoids both the quality loss of script-painted substitutes and the latency of decomposition/background-correction passes.

### 3. Replace the selected production paths in place

The selected fourteen semantic slots are integrated into their existing source paths and exported to their existing runtime paths. Their `.meta` GUIDs, slice borders, safe insets, and ArtSet slot identities remain stable. Previous sheet bindings and procedural material recipes for the selected surfaces are removed from the active exporter and manifest rather than retained as alternates. Unselected slots keep their existing owned source.

### 4. Treat contrast as a content-first semantic pair

The user-approved light-green master is P0 container authority. Primary and green secondary actions use soil-brown text/icons as their current semantic content pair, and release validation measures that actual pairing in the central content region. Danger retains warm-white content. The exporter cannot darken, recolor, overlay, regenerate, or substitute the generated green to force contrast.

The rejected runtime candidate exposed the failure mode in the former contract: requiring warm-white content at `4.5:1` made a visually approved light green impossible and drove the selected master darker. When a reference locks the container but not the content color, the correction order is `text/icon content token → permitted content treatment → user decision`; the raster is never a contrast-control variable. If the reference locks both sides and they cannot satisfy a mandatory gate together, the candidate stops for explicit user direction rather than silently changing either reference parameter.

### 5. Use marker-free contained interaction motion and seam-safe nine-slice sampling

Actions and interactive tool/nursery slots do not draw an action-specific or slot-specific `marker.selected` badge. Hover/focus and mouse press resolve to the existing restrained theme scale tokens, and a nursery click uses the existing short selection pulse; only the visual rectangle moves inward while the authoritative draw owner and hit rectangle remain unchanged. `marker.selected` remains available for components whose approved anatomy explicitly owns a marker, such as selectable cards.

The former `slot-nursery.png` baked a solid orange rim and orange dashed inner rail into every slot. Fractional scaling only changed how strongly those thin pixels were sampled; it did not create them. Suppressing the whole slot surface removed the rails but also removed the required paper carrier, while restoring the old master restored both. The final boundary is therefore asset-specific: the Header and Nursery section panel frames remain; `surface.metric` and `slot.nursery` each remain one complete rounded-paper carrier with soft tonal depth, but each owns a dedicated line-free master containing no solid or dashed linear rail. `surface.metric` no longer inherits the large panel's inner outline through shared-source convenience. The shared renderer still draws each surface exactly once and removes only black/transparent fractional-scale seams. No runtime mask, overlay, state branch, duplicate surface, or shader-specific color removal is permitted.

The first rail-free integration was still invalid: ImageGen returned an RGB checkerboard plate, and applying the previous slot's alpha geometry left neutral/dark and orange RGB underneath semi-transparent edge pixels because the generated silhouette and legacy mask did not coincide. WebGL filtering could hide that fringe while particular PC scales reconstructed it as four perimeter lines. The corrected production path removes the nursery geometry mask entirely and gives the metric an independent output; both derive alpha from their own center-connected warm material against their own edge-connected neutral background. Every direct ImageGen nine-slice clears low-alpha ringing after both source and runtime resize and stores zero RGB under zero alpha. Line-free carriers additionally reject neutral/dark partial-alpha pixels, while the metric rejects a continuous dark rail on any of its four sides. A master-sharing validator rejects semantic pairs whose edge/anatomy contracts differ before the package is built.

Nine-slice rendering keeps the same source border and destination geometry, but adjacent patches receive a device-pixel-safe overlap/sample guard so PC scale factors cannot expose black internal boundaries.

### 6. Re-run one focused Battle Gate A with user-owned visual judgment

After deterministic repeatability, art/theme validation, aggregate Unity smoke, and ordinary WebGL build pass, the acceptance profile may retain the required state matrix as technical evidence. The default user handoff is exactly one same-viewport, same-state three-column artifact: supplied reference, immutable user-scored before capture, and new real WebGL capture. The assistant does not self-score or visually approve the candidate. Route-wide topology and gameplay gates are not reopened because their authorities are unchanged.

## Risks / Trade-offs

- **[Transparent ImageGen output may contain clipped shadows or a baked background]** → Require protected gutters and center stretch safety; use only native alpha, deterministic exterior cleanup, or the approved geometry-alpha mask, and reject rather than repaint invalid output.
- **[A reference-authoritative container may fail contrast with current content]** → Change the separate content token first and measure final central pixels; never darken, recolor, regenerate, or substitute the approved master.
- **[Reference-locked container and content conflict with a mandatory gate]** → Report the exact conflicting pair and stop for explicit user direction; generic guidance cannot override the reference.
- **[Generated paper materials may look inconsistent when nine-sliced]** → Validate protected corners/edges and test the narrowest/widest live components; reject seams or stretched ornaments.
- **[Replacing fourteen surfaces can create mixed provenance]** → Record direct asset/output/hash and deterministic transform on every selected manifest binding, bump one revision, run the exporter twice, and fail on any remaining selected procedural recipe or sheet binding.
- **[The page may improve locally but remain below the subjective target]** → Preserve the approved baseline and stop at a new explicit visual gate; no automated score substitutes for user review.

## Migration Plan

1. Preserve the approved screenshots, source hashes, and release identity as the before evidence.
2. Generate and select directly replaceable masters for each distinct action and structural material family.
3. Update the existing exporter to integrate the selected individual assets only, export in place, bump revision, and remove superseded sheet bindings and selected material recipes.
4. Run deterministic export twice, focused art/theme checks, aggregate smoke, WebGL build, and canonical captures.
5. Publish one reference/before/after real-runtime comparison and stop for user review; on rejection, replace the affected individual master and rerun rather than adding a fallback.

There is no runtime data migration or compatibility period. Source-control revert is the only rollback before acceptance.

## Open Questions

No implementation question blocks this increment. The final 8-point judgment remains an explicit user visual decision.
