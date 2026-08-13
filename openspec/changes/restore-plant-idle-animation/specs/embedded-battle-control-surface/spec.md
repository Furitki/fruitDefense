## MODIFIED Requirements

### Requirement: Frameless near-cell-size flowerpot and fruit icons
Map flowerpots, map fruits, occupied nursery fruits, nursery flowerpot rewards, and the flowerpot tool SHALL render without persistent opaque square backplates, their atlas art SHALL nearly fill the associated logical cell without changing its click or drag bounds, and planted map fruits SHALL retain subtle time-varying idle motion between combat actions.

#### Scenario: Idle map entities
- **WHEN** active flowerpots and fruits render without selection, drag feedback, or an active combat action
- **THEN** their transparent atlas art is visible at no less than 0.85 of the map tile width, no opaque rectangular underlay or persistent border is drawn beneath it, and each planted fruit's draw pose changes subtly over simulated time without changing its interaction bounds

#### Scenario: Nursery and flowerpot tool icons
- **WHEN** a nursery fruit, nursery flowerpot reward, or flowerpot tool icon renders
- **THEN** the icon uses only a small inset from its logical cell and the cell remains the unchanged interaction target

#### Scenario: Entity interaction feedback
- **WHEN** a flowerpot or fruit is selected, targeted, dragged, or returned
- **THEN** the relevant state is indicated with a transient outline while the persistent backplate remains absent

#### Scenario: Repeated plant attack motion
- **WHEN** a planted attacker completes one basic attack while a living enemy remains in range through the next cooldown
- **THEN** the plant returns to idle motion and begins another plant-specific attack motion when the next basic attack executes
