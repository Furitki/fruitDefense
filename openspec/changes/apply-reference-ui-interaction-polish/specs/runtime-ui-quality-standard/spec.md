## ADDED Requirements

### Requirement: Temporal interaction quality
The normative runtime UI quality standard SHALL validate motion timing, easing, cancellation, final-state stability, reduced-motion equivalence, and the separation of visual animation from authoritative hit geometry.

#### Scenario: Animated component is reviewed
- **WHEN** a shared action, card, status, metric, or route reveal is added or changed
- **THEN** validation proves its token ownership, bounded duration, cancellation behavior, exact resting geometry, semantic non-motion cues, and absence of input drift

#### Scenario: Motion is interrupted
- **WHEN** a route closes, an owner replays feedback, or an interaction is cancelled before its motion completes
- **THEN** no orphaned motion, delayed command, stale opacity, or transformed resting geometry remains
