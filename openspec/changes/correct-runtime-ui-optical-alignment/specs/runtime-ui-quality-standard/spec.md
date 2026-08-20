## MODIFIED Requirements

### Requirement: Optical icon and resource consistency
Each common icon, marker, indicator, and action surface SHALL keep a stable import canvas and safe inset while also publishing authoritative optical alpha bounds measured from its final runtime raster. Comparable members of one visual family SHALL satisfy the documented optical-box, centroid, baseline, visible-envelope, and family-weight tolerances at their smallest runtime size.

#### Scenario: Resource set is validated
- **WHEN** a production ArtSet is imported or previewed
- **THEN** validation reports missing/duplicate slots, importer violations, alpha-edge contamination, stale optical metadata, family-envelope mismatch, optical outliers, unbound production files, review dependencies, and mixed-set ownership as failures

#### Scenario: Equal paired actions use different surface roles
- **WHEN** primary, secondary, quiet, or danger action surfaces are rendered into equal destination rectangles
- **THEN** their final visible surface bounds have equal width and height within the documented tolerance and remain contained by the original action rectangles

### Requirement: Raster-aware optical inspection
The quality system SHALL distinguish component rectangles, safe insets, text line boxes, and final rendered visual mass. It SHALL inspect actual alpha bounds, role-corrected glyph placement, combined group bounds, repeated baselines, asymmetric gutters, and occupied-content balance in addition to containment.

#### Scenario: Rectangles pass but the rendered group is off-center
- **WHEN** the icon rectangle and label rectangle are each valid but their final rendered alpha-and-glyph group exceeds the optical-centering tolerance
- **THEN** quality validation fails and the shared anatomy or owned asset metadata is corrected rather than declaring the component aligned

#### Scenario: Safe insets match but icon silhouettes differ
- **WHEN** two icon bindings share the required canvas and safe inset but their actual alpha bounds differ
- **THEN** runtime layout uses each binding's authoritative optical bounds and validation rejects missing or stale optical metadata

#### Scenario: Route content is technically contained but visibly unbalanced
- **WHEN** all components fit yet the occupied bounds or opposite gutters exceed the documented balance tolerance
- **THEN** the route remains a visual failure until its authoritative layout is rebalanced
