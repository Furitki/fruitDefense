## ADDED Requirements

### Requirement: Activity provides a real item reward path
The bundled Activity page SHALL contain at least one always-available one-time activity whose visible reward can be claimed into the current player profile without a network clock.

#### Scenario: New player opens Activity
- **WHEN** an unclaimed starter activity is displayed
- **THEN** its title, finite description, reward identities and quantities, claim status, and claim action are readable before activation

### Requirement: Activity claim is atomic and idempotent
Claiming an activity SHALL validate all grants and its receipt, apply the complete grant and receipt to one cloned profile revision, persist once, and publish the new balances only after persistence succeeds.

#### Scenario: Valid activity is claimed
- **WHEN** the player activates Claim for an eligible activity
- **THEN** every configured item or outgame growth-equipment grant and exactly one receipt are committed in one new profile revision

#### Scenario: Claim is activated repeatedly
- **WHEN** the same activity is activated while saving or after its receipt exists
- **THEN** no second grant is applied and the visible state resolves to claiming or claimed as appropriate

#### Scenario: Persistence fails
- **WHEN** profile storage fails during a claim
- **THEN** no new balance, growth-equipment ownership, or receipt becomes authoritative and the activity exposes a retryable error

### Requirement: Starter reward closes the first growth step
The initial bundled activity reward SHALL grant a starter outgame growth-equipment identity and enough configured growth material to complete at least one visible growth-equipment or cultivation upgrade.

#### Scenario: Fresh profile completes the starter claim
- **WHEN** the claim succeeds and the player opens Growth
- **THEN** the granted growth-equipment is owned and at least one configured next-rank action is affordable from the granted material

### Requirement: Activity state has explicit semantic feedback
Activity cards SHALL distinguish available, claimable, claiming, claimed, locked, insufficient-context, and error states through semantic copy plus at least one non-color cue, and only claimable cards SHALL expose an enabled primary claim action.

#### Scenario: Already claimed activity is revisited
- **WHEN** Activity renders a receipt-owned entry
- **THEN** it shows a completed marker and claimed copy, retains the reward summary, and exposes no enabled duplicate Claim action
