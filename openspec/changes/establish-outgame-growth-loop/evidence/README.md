# Outgame growth loop acceptance evidence

This directory owns review evidence for `establish-outgame-growth-loop`. Evidence is valid only when every referenced artifact is produced from the same source revision and the final manifest records the ordinary release build separately from the dedicated acceptance build.

## Required automated evidence

- Focused content, profile, progression, hub, growth projection, launch, retry, restore, and acceptance-isolation smoke results.
- Aggregate `FruitDefense.Editor.ProjectSetup.SmokeValidate` result.
- Strict `openspec validate establish-outgame-growth-loop --strict` result.
- One ordinary release WebGL build and one dedicated acceptance WebGL build from the same revision. The ordinary build must reject or ignore acceptance-shaped queries and contain no acceptance route, bridge, synthetic reward, synthetic safe area, or injected growth surface.
- Dedicated live-canvas hub captures for Home, Activity, Equipment, and Cultivation at every case in `Get-OutgameHubPortraitMatrix`: 360×800, 375×812, 402×874, and 430×932, each full and canonical inset.
- A real fresh-profile `Activity reward → Growth upgrade/equip → Home preview/start → Battle → Settlement → Home` sequence plus Settlement Retry. It must use real canvas clicks and committed profile commands, not screenshot substitution.
- A real desktop-host pass using the acceptance payload and browser bridge, with canvas, viewport, input, route, profile, content, and growth telemetry retained.

## Identity and outcome contract

The final evidence manifest must fill every field in `acceptance-evidence-template.json`. In particular it records:

- source revision and both build identities;
- root manifest plus outgame/battle content IDs, versions, and fingerprints;
- profile ID and distinct reward/growth/level-selection revisions, activity receipt count, item grant/debit quantities, owned equipment/loadout/cultivation ranks;
- each static named-state `stateId`, `fixtureId`, evidence kind, fixture flag, screenshot hash, and telemetry snapshot; fixture-backed states are review evidence only and never satisfy the real loop;
- selected level, growth policy, applied/suppressed source counts, and exact growth fingerprint across preview, Battle, Settlement, return, and Retry;
- each viewport/safe-area case, screenshot hashes, shared chrome/page-host geometry, 44-point target outcomes, text/contrast/ArtSet checks, and manual review disposition;
- cleanup proof for one-shot runners, marker files, disposable debug menus, and non-suite acceptance helpers.

Files under `imagegen/` are source evidence only. They do not satisfy runtime or live-canvas acceptance.

## Completion rule

Do not mark tasks 8.1, 8.4, or 8.5 complete from this template or from a partial matrix. Mark them only after the dedicated catalog/bridge is exercised, all required live evidence exists, identities agree, manual review is recorded, and cleanup passes.
