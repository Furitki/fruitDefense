# `sunny-orchard` editable masters

This is the owned master location for the approved A「阳光果园」production direction.

The 38 checked-in SVG files are the independent editable masters for the original 40 semantic bindings. `icon.control-continue`, `icon.control-start-wave`, and `icon.control-start` intentionally share the same play-symbol master and Sprite. The current 49-slot manifest explicitly lists the nine shared painted composition bindings; every slot remains independently serialized and validated.

- `art_manifest.json` records every slot, owned master/export path, SHA-256, stable GUID, dimensions, source scale, geometry, slice border, safe inset, and final-PNG optical inset.
- `export_sunny_orchard.py` is the dependency-light deterministic export pipeline. It renders the SVG recipes to antialiased PNG with Pillow, creates stable Unity importer metadata only when it is missing, updates the ArtSet asset, and creates the review gallery. Existing `.meta` files are never rewritten.
- The exporter measures `optical_inset` for every binding from its final runtime PNG at `alpha >= 48` and writes the same four-sided value to ArtSet `opticalInset`. Shared painted bindings are remeasured from their owned runtime PNG instead of trusting copied source dimensions. `safeInset` remains the interaction/content protection contract; `opticalInset` is the visible-ink contract used for optical alignment.
- Source and runtime filenames use the same lowercase kebab-case semantic basename.
- The 2× source scale produces 128×128 nine-slice surfaces with 32 px protected borders and 96×96 common icons with 12 px transparent safe insets.
- `action.primary` uses the dedicated accessible production token `#559A39`; the broader leaf-green `#6DBE4B` remains the success and icon accent token.
- Common 96 px icons are optically normalized in the vector recipes: alpha-mass centroid stays within 4 px per axis, the common-family major dimension stays in 60–72 px, and drag cues keep at least a 64 px alpha short edge.

Do not crop or publish the approved style board as component artwork. Re-export in place, preserve destination `.meta` GUIDs, review the gallery, and increment the ArtSet revision whenever the visible production content changes.
