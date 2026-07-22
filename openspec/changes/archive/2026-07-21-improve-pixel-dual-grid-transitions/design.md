## Context

The pixel baker currently classifies single-corner masks with a fixed quarter-circle, classifies adjacent pairs with exact half-planes, and then paints every occupied pixel nearest transparency with one configured edge color. This is deterministic and seam-safe, but the silhouette is independent of the source material and the mandatory dark band visually separates an overlay from its base ground. The existing runtime only consumes sixteen `Tile` slots, so the change can remain editor-only and retain the stable corner-mask and side-socket boundary.

The source textures are opaque and are Point-sampled onto the final native grid. They contain useful material structure but no semantic alpha or authored transition path. Guidance therefore must be deterministic, bounded, palette-preserving, and safe when the source has little luminance variation.

## Goals / Non-Goals

**Goals:**

- Produce non-circular native-pixel silhouettes whose small-scale variation is derived from the effective foreground source texture.
- Permit a zero-pixel outline so transparent overlay edges can meet arbitrary base terrain without a baked solid halo.
- Keep all compatible side borders pixel-identical and keep masks `5` and `10` disconnected.
- Preserve binary alpha, Point sampling, source palette membership, deterministic rebakes, existing TileSet paths, and aggregate editor smoke coverage.
- Give the wizard one bounded contour-guidance control without requiring artists to author sixteen masks.

**Non-Goals:**

- Inferring a bespoke semantic transition for every possible pair of terrain materials.
- Adding graph-cut libraries, runtime shaders, antialiasing, blended palette colors, animated variants, or scene/runtime changes.
- Replacing the optional future path for hand-authored sixteen-mask templates or pair-specific transition profiles.
- Changing gameplay, persistence, release scenes, collision, navigation, or platform adapters.

## Decisions

### Build a normalized corner field instead of quarter-circle templates

For non-empty/non-full masks, the baker evaluates the bilinear interpolation of the four corner bits at each final pixel center and converts the threshold difference to approximate pixel distance using the local field gradient. Opposite-corner masks receive a deterministic negative center saddle before thresholding and retain the explicit transparent center block.

This produces the standard corner-state topology without encoding a circular primitive. Reusing the continuous high-resolution baker is rejected because that pipeline supersamples, antialiases, interpolates colors, and has different importer guarantees.

### Derive bounded guidance from the sampled source texture

The foreground `PixelSource` records its luminance range and supplies a small box-filtered, normalized tone value using the same Point-sampling phase as color composition. The configured integer `textureGuidancePixels` scales that value and offsets the normalized corner-field distance before the binary occupancy decision.

Guidance is clamped to a small profile range, cannot move the contour farther than that range, and becomes zero for a flat source. A bounded scalar field is preferred over a full shortest-path or graph-cut implementation because the native targets are small, the source is fully opaque, and the project must remain dependency-free. The result still follows coherent light/dark material clusters rather than adding unrelated random noise.

### Keep sockets canonical and make their cut source-guided

Each of the four two-bit side states remains canonical. For a mixed side, the transition index is selected within the same bounded range around the midpoint using the foreground source guidance; the identical canonical result is reused by every mask with those side bits. Corner pixels remain explicitly rewritten.

This preserves exact RGBA equality between compatible borders while avoiding a visibly fixed midpoint on every transition. Border compatibility remains a stronger invariant than visual variation.

### Represent no outline as a zero-width edge band

`outlinePixels` accepts zero. When it is zero, neither interior composition nor sockets emit `edgeColor`; the occupied boundary begins with the soil rim when configured and otherwise with foreground texels. Existing explicit-outline behavior remains available for profiles with a positive width. The allowed-palette validator only admits the configured edge color when that band is active.

An enum is rejected because width zero already represents the state, keeps serialized data simple, and avoids duplicating two controls that can contradict one another.

### Extend evidence with behavioral measurements

Profile evidence records guidance width, whether a solid outline is active, and the number of occupied pixels changed by source guidance relative to the unguided corner field. Validation requires guidance to affect a non-flat representative source, preserves the existing deterministic hash, and continues to check every compatible border and both opposite-corner component counts.

The existing native atlas remains the visual evidence. The PixelGrass and StoneFloor profiles are migrated to zero outline with bounded guidance so aggregate project smoke exercises both styles of source texture.

## Risks / Trade-offs

- [High-frequency source noise creates isolated edge pixels] -> box-filter guidance, bound displacement, and reject invalid component/topology output in validation.
- [Very flat textures cannot guide a contour] -> fall back deterministically to the non-circular bilinear contour and report zero texture-guided changes as valid only for a flat source or zero guidance.
- [A generic foreground-only guide cannot express material-pair semantics] -> keep pair-specific authored transition profiles as a future extension; the no-outline mode remains connection-safe over arbitrary bases.
- [Changing existing profile output alters generated PNG hashes] -> regenerate only owned derived assets and profile-specific evidence; source images and runtime contracts remain unchanged.
- [Socket rewriting can create a one-pixel direction change] -> use the same bounded guidance and add native-scale border/topology assertions.

## Migration Plan

1. Add backward-compatible profile guidance data and permit zero outline width.
2. Replace land classification and canonical mixed-side cuts while preserving output paths and mask indexes.
3. Migrate PixelGrass and StoneFloor profiles to zero outline and guidance enabled, then rebake owned outputs.
4. Extend smoke/evidence validation and run the aggregate editor validation surface.

Rollback restores the prior profile values and generator classification, then rebakes the same owned output folders. No runtime or saved-data migration is required.

## Open Questions

- Pair-specific transition exemplars and fully hand-authored sixteen-mask templates remain future quality options if foreground-only guidance is insufficient for production art.
