## MODIFIED Requirements

### Requirement: Portrait-first composition
The game SHALL render dedicated portrait compositions for the shared Home/Activity/Growth hub, Battle, and Settlement at the iPhone 17 reference viewport of 402 by 874 logical points and SHALL preserve approved information hierarchy, typography alignment, icon alignment, component containment, and shared navigation at 360×800, 375×812, 402×874, and 430×932 full and representative inset safe areas.

#### Scenario: Initial portrait hub
- **WHEN** the game opens at the iPhone 17 portrait aspect
- **THEN** shared top chrome, Home content, growth preview, primary Start action, and bottom navigation appear in one intentional top-to-bottom flow without horizontal scrolling, overlap, clipped controls, misaligned repeated content, or stretched ornamental art

#### Scenario: Wider or taller portrait hub page
- **WHEN** Home, Activity, or Growth opens on another supported portrait viewport
- **THEN** the page preserves the same shared chrome and navigation anchors, uses available page-host space, and keeps text, icons, indicators, actions, and nine-slice boundaries inside their authoritative rectangles

### Requirement: Balanced route composition
Home, Activity, Growth, Battle, terminal, and Settlement SHALL use intentional occupied-content bounds, symmetric or explicitly justified gutters, and a stable page-level visual center; every hub page SHALL close above the persistent bottom navigation, and Battle SHALL retain the gameplay stage as its sole normal-state heavy structural anchor.

#### Scenario: Battlefield is projected into its stage
- **WHEN** the battlefield grid is rendered and hit-tested
- **THEN** the grid uses the same projection for drawing and input, remains contained by the gameplay-stage frame, and keeps symmetric visual gutters within tolerance

#### Scenario: Battle has no selected detail
- **WHEN** the ready or active Battle state is rendered without a selected plant
- **THEN** the stage, context tools, nursery, and refresh action form one intentional top-to-bottom rhythm without an unused detail band or a second enclosing lower-page frame

#### Scenario: Home fills a tall portrait canvas
- **WHEN** Home is shown at a supported full or inset height
- **THEN** level selection, growth preview, and Start form a deliberate hierarchy above bottom navigation without unfinished lower space

#### Scenario: Activity or Growth fills a portrait canvas
- **WHEN** Activity, Equipment, or Cultivation is shown at a supported full or inset height
- **THEN** its list/detail content has one clear scroll owner, its primary or completed action state is visible, and no content is hidden behind shared chrome or navigation

