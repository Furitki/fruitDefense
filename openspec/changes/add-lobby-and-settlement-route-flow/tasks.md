## 1. Shared Shell Layout

- [ ] 1.1 Add a portrait safe-area shell layout whose rectangles drive both drawing and hit testing
- [ ] 1.2 Add reusable shell styles and deterministic layout validation without changing battle styles

## 2. Lobby

- [ ] 2.1 Add a thin Lobby presenter with title, Start, and visible disabled level/growth/settings areas
- [ ] 2.2 Create a valid `orchard-01` launch request with a new session ID, nonzero seed, and bundled content version
- [ ] 2.3 Disable Start while navigation is transitioning and ignore reserved-area input

## 3. Settlement

- [ ] 3.1 Add a thin Settlement presenter for outcome, reached wave, and remaining lives
- [ ] 3.2 Implement Return with session/result cleanup and Retry with a new session ID and seed
- [ ] 3.3 Route missing or mismatched result data safely to Lobby with a structured error

## 4. Validation

- [ ] 4.1 Add layout, hit-test, launch-request, result-display, return, retry, and duplicate-input validation
- [ ] 4.2 Run OpenSpec validation, Unity compilation, and shell smoke; leave scene/build activation to the integration change
