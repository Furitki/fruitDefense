## ADDED Requirements

### Requirement: Lobby owns a finite outgame hub
Lobby SHALL present one shared outgame hub with `Home`, `Activity`, and `Growth` destinations, and switching among those destinations SHALL NOT begin an `AppRoute` transition or load a Unity scene.

#### Scenario: Player switches from Home to Activity
- **WHEN** Activity is activated while Lobby is interactive
- **THEN** the page host shows Activity, shared chrome remains mounted, and `AppNavigator.CurrentRoute` remains Lobby

#### Scenario: Player returns from Settlement
- **WHEN** Settlement completes a valid return transition
- **THEN** Lobby becomes interactive on Home with the completed session cleared

### Requirement: Shared chrome and page ownership
The hub SHALL own one persistent top bar, one page host, and one bottom navigation, while Home SHALL own level selection and battle start, Activity SHALL own activity rewards, and Growth SHALL own Equipment and Cultivation subpages.

#### Scenario: Any primary page is visible
- **WHEN** Home, Activity, or Growth is drawn
- **THEN** the same top bar and bottom navigation remain visible and the active presenter draws only inside the page host

#### Scenario: Growth is selected
- **WHEN** the player opens Growth
- **THEN** Equipment and Cultivation are exposed through secondary navigation visually subordinate to the primary bottom navigation

### Requirement: Finite hub page lifecycle
Cold start and Settlement return SHALL select Home, and switching pages during one Lobby lifetime SHALL preserve page-local selection and scroll state without treating presenter state as authoritative profile data.

#### Scenario: Page is revisited during Lobby
- **WHEN** the player selects an equipment entry, visits Activity, and returns to Growth without leaving Lobby
- **THEN** the same Growth subpage and local selection are restored while balances and ownership are refreshed from the latest persisted profile projection

### Requirement: Safe-area draw and input parity
Hub chrome, page content, and navigation SHALL derive drawing and hit testing from the same safe-area-aware portrait layout and SHALL keep every required control and label usable at all supported full and inset viewports.

#### Scenario: Bottom navigation is used on the narrowest inset viewport
- **WHEN** the 360×800 inset hub receives a pointer activation inside a visible navigation item
- **THEN** exactly that destination is selected, every target is at least 44 logical points on its shortest interactive dimension, and no page content overlaps the navigation

### Requirement: Serialized hub commands and explicit unavailable states
The hub SHALL allow at most one persistence-changing activity or growth command at a time, SHALL reject duplicate activation while that command is pending, and SHALL render locked or unavailable content with an explicit reason rather than a functioning placeholder.

#### Scenario: Player switches pages during an activity save
- **WHEN** an activity claim is persisting and the player opens Growth
- **THEN** the claim remains single-submit, Growth reads the last committed profile revision until save completion, and no duplicate grant or debit occurs

#### Scenario: A future growth section is not implemented
- **WHEN** content exposes the section as unavailable
- **THEN** the hub shows a non-interactive locked state and does not present an action that appears usable

