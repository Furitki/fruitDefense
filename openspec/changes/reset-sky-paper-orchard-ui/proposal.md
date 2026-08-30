## Why

The current release UI is structurally sound but does not yet match the clearer sky-blue, warm paper, soil-brown, leaf-green hierarchy, compact Battle rhythm, and rounded Chinese typography shown by the newly approved reference direction. Resetting the presentation now creates one coherent visual baseline before additional content is layered onto the four-route release flow.

The first implementation was rejected during user review at an estimated 3/10
reference similarity: its colors were related to the reference, but it retained
generic flat component anatomy, a different page composition, and unrelated
button construction. Automated geometry and flow acceptance from that version
is an engineering baseline only and is not visual approval.

## What Changes

- Intentionally replace the current release UI treatment across `Bootstrap → Lobby → Battle → Settlement` with a sky-edge, warm-paper, soil-stage, leaf-green action system while preserving route, command, gameplay, numeric, save, and input semantics.
- Recompose Battle first as a 402×874 vertical slice: compact status Header, single gameplay stage, independent phase/Wave action row, mutually exclusive ContextTray/detail, persistent NurseryTray, and bottom RefreshAction; then propagate the approved visual language to the other routes.
- **BREAKING** Move phase/Wave presentation out of the gameplay-stage control strip into an independent flow row without retaining the old in-stage action path, fallback geometry, or compatibility rendering.
- Replace the single-font assumption with packaged static Chinese font assets referenced by semantic typography role, including a rounded high-weight title/action face and a clear body/metric face; runtime text remains separate from raster art and never depends on host fonts.
- Rebuild text-free UI masters and deterministic runtime exports for paper surfaces, stage framing, actions, slots, resource icons, state cues, and restrained orchard decoration. Production action surfaces SHALL originate from project-owned ImageGen output, selected and deterministically cropped/normalized into independent nine-slice masters; the exporter may not procedurally paint their rim, face, outline, highlight, or shadow. The supplied full-page image remains design/acceptance evidence only and is never cropped, traced as layout geometry, or shipped as a runtime screen.
- Treat the supplied Battle image as the binding component-anatomy and relative-composition contract for the 402×874 vertical slice: a two-row floating Header, three raised resource capsules, cream-rimmed yellow compact controls, one large warm-paper page shell, an inset soil stage, paired phase/Wave blocks, recipe-style build cards, dashed nursery slots, and a thick leaf-green bottom refresh action. Runtime copy and gameplay content remain project-owned and text stays separate from raster art.
- Validate the Battle vertical slice before route-wide rollout, then prove the complete flow in ordinary WebGL at the supported portrait matrix and representative full/inset safe areas, including recorded draw/hit alignment and packaged-font containment.
- Require explicit user approval of a new 402×874 Battle capture before treating Gate A as passed or propagating the revised anatomy to Bootstrap, Lobby, or Settlement.
- Explicit non-goals: gameplay simulation, balance numbers, level/content identity, navigation legality, persistence/snapshot behavior, plant/enemy/terrain content art, platform adapters, and mini-game conversion readiness.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `runtime-ui-quality-standard`: Change the approved release treatment, role-level packaged Chinese typography contract, text-free production-art contract, and reference-driven quality gates.
- `portrait-game-interface`: Change the Battle top-to-bottom layout topology and define how the reset composition scales across supported full and inset portrait viewports and the other release routes.
- `embedded-battle-control-surface`: Recompose the Battle vertical stack around a shorter single stage, independent phase/Wave flow row, persistent context/nursery/refresh controls, and unchanged authoritative draw/hit semantics.
- `battle-session-controls`: Move the phase-specific Wave action from the battlefield control strip to the independent flow row while preserving labels, visibility, commands, pause, restart, and touch behavior.
- `webgl-visual-acceptance`: Require canonical ordinary-WebGL evidence for the reset Battle states, packaged typography, deterministic assets, full/inset viewport matrix, and route-wide rollout.

## Impact

- Affects the release `RuntimeUiTheme`, semantic typography references, shared immediate-mode components, Battle and shell layout authorities, route presenters, `RuntimeUiArtSet` source/runtime assets, deterministic UI exporters/import validation, editor inspection catalogs, Unity smoke tests, and ordinary-WebGL capture/acceptance manifests.
- Existing gameplay/core APIs, launch/result contracts, content catalogs, save/snapshot formats, route transitions, and input meanings remain unchanged; drawing and hit testing continue to consume the same authoritative rectangles and battlefield projection.
- The stable visual rules in `docs/ui/ui-visual-system.md` must be synchronized after implementation and acceptance because this change intentionally replaces parts of the current visual standard; release/platform status documents remain evidence owners for their respective gates.
