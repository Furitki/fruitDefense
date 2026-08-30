## ADDED Requirements

### Requirement: Fixed upward combat floating-text motion
Damage and defeat floating text SHALL use the upward screen-space placement direction for every BattleStage anchor. The overlay MUST NOT reverse its motion, semantic defeat offset, or collision-lane direction to push an upper-edge label downward into the battlefield. It SHALL continue to use the existing shared battlefield projection, safe-area transform, battlefield offset, and BattleStage containment rules.

#### Scenario: Target is near the BattleStage upper edge
- **WHEN** normal damage or defeat feedback is anchored in the upper-edge region of BattleStage
- **THEN** its requested placement direction remains upward rather than being redirected downward
- **AND** its rendered bounds remain governed by the existing BattleStage containment and viewport projection rules

#### Scenario: Target is away from the upper edge
- **WHEN** normal damage or defeat feedback is anchored elsewhere on BattleStage
- **THEN** it uses the same upward direction and shared projection as an upper-edge label

### Requirement: Expanded bounded dense-feedback separation
The floating-text collision resolver SHALL search 33 stable, deterministic candidate lanes, extending its vertical separation envelope through 224 logical points in the upward direction. It MUST test nearer candidates before farther candidates, preserve the existing 12 logical-point horizontal anchor limit, and remain allocation-free within the fixed 12-label pool.

#### Scenario: Dense simultaneous feedback exceeds the prior envelope
- **WHEN** active floating labels overlap after all candidates through 140 logical points have been considered
- **THEN** the resolver considers the 168, 196, and 224 logical-point tiers in deterministic order before accepting overlap

#### Scenario: A near candidate is available
- **WHEN** an earlier candidate has zero overlap and satisfies containment and horizontal anchoring
- **THEN** the resolver selects that candidate without considering a farther tier
