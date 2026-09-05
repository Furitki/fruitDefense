## 1. Prerequisites and approved product content

- [x] 1.1 Complete the remaining validation gate for `establish-modular-game-config-manifest` and confirm its root manifest/export path is the only production content authority.
- [x] 1.2 Finalize stable IDs, Chinese copy, icons, starter reward quantities, starter growth-equipment ranks, first cultivation node, supported growth attributes, and the three bundled level growth policies; prove one starter claim funds at least one real upgrade.
- [x] 1.3 Obtain the major-design synchronization decision and, only if approved, update `docs/design/game-design-overview.md` sections 3.1, 5, 7, 8, and 9 without copying transient implementation status.

## 2. Outgame content authority

- [x] 2.1 Add typed item, activity, reward, outgame growth-equipment, cultivation, cost, growth-contribution, and growth-policy DTOs plus ScriptableObject authoring assets, keeping growth-equipment identity separate from existing in-battle equipment.
- [x] 2.2 Extend the root game-content manifest with the required outgame catalog resource, ID, version, and fingerprint, with no second manifest or fallback source.
- [x] 2.3 Implement canonical outgame JSON export/load, deep-copy compilation, ordinal stable-ID indexes, and complete local/cross-catalog structured validation.
- [x] 2.4 Add required `growthPolicyId` references to every playable level and populate the bundled starter activity/reward/growth-equipment/cultivation/policy content.
- [x] 2.5 Add Editor fixtures and tests for round-trip determinism, invalid IDs/references/operations/ranks/caps, manifest mismatch, and preservation of the last valid export.

## 3. Current profile and transactional progression

- [x] 3.1 Replace `PlayerProfileEnvelopeV1` with the single current schema for normalized item balances, receipts, owned growth-equipment/ranks, loadout slots, and cultivation ranks; remove V1 DTO/codec/test paths.
- [x] 3.2 Extend complete profile validation, clone/serialization, Editor-file persistence, WebGL PlayerPrefs persistence, fixtures, and explicit unsupported-schema/reset behavior.
- [x] 3.3 Implement immutable hub/profile view projections and one concrete `PlayerProgressionService` that serializes persistence-changing commands.
- [x] 3.4 Implement atomic idempotent activity claim with receipt validation, complete grants, one revision/save, duplicate rejection, and rollback on persistence failure.
- [x] 3.5 Implement transactional equip, equipment upgrade, and cultivation upgrade commands with ownership, slot, prerequisite, maximum-rank, and complete-cost validation.
- [x] 3.6 Add tests for valid/invalid profile collections, duplicate claims, duplicate clicks, insufficient costs, partial-failure rollback, slot replacement, prerequisites, maximum ranks, and single revision publication.

## 4. Hub program framework and shared layout

- [x] 4.1 Add the finite `HubPageRouter` and narrow read/command flow contexts while keeping `AppNavigator` limited to Lobby, Battle, and Settlement.
- [x] 4.2 Add `PortraitHubLayout` with shared top bar, page host, bottom navigation, and page-specific child layouts used identically by drawing, pointer tracking, hit testing, telemetry, and validation.
- [x] 4.3 Build `LobbyHubPresenter` with one `OnGUI`, shared chrome, serialized command state, page feedback, Home/Activity/Growth switching, and local page selection/scroll preservation.
- [x] 4.4 Move current level selection and Start behavior into Home, select Home on cold start/Settlement return, and remove the obsolete single-page `LobbyPresenter` ownership path after cutover.
- [x] 4.5 Add Shell unit/layout tests for internal navigation without scene loads, page lifecycle, draw/hit parity, duplicate-input rejection, and every supported full/inset portrait geometry.

## 5. Activity and Growth visual pages

- [x] 5.1 Implement the Activity page/card with reward preview and available, claimable, claiming, claimed, locked, and recoverable-error states backed by the real claim command.
- [x] 5.2 Implement Growth secondary navigation and Equipment list/detail, ownership/loadout/rank/cost presentation, equip/upgrade commands, and finite owned/locked/insufficient/maximum/loading/error states.
- [x] 5.3 Implement Cultivation list/detail or node presentation with prerequisites, ranks, costs, upgrade command, and finite locked/insufficient/maximum/loading/error states.
- [x] 5.4 Keep shared item balances synchronized only to committed profile revisions and verify Activity reward → Equipment/Cultivation spend visually and behaviorally.
- [x] 5.5 Add packaged-font copy/bounds tests and presenter interaction tests for every required Activity, Equipment, and Cultivation state.

## 6. Deterministic battle growth boundary

- [x] 6.1 Implement the pure `BattleGrowthResolver` with stable source order, domain/source/attribute filtering, caps, applied/suppressed reasons, documented aggregation order, and canonical fingerprint.
- [x] 6.2 Add Home's selected-level growth preview from the resolver, re-resolve on level/profile change, and block Start on invalid projection without inventing an empty fallback.
- [x] 6.3 Extend `BattleLaunchRequest`, flow coordination, retry, acceptance fixtures, and validation with the required deep-copied growth snapshot; remove the obsolete constructor and no-growth launch path.
- [x] 6.4 Integrate launch baseline growth into the existing deterministic effective-attribute pipeline before transient statuses without adding a second modifier executor.
- [x] 6.5 Extend resolved source identity and battle snapshot export/restore validation with growth policy/content/fingerprint and reject missing, changed, or post-launch profile substitutions atomically.
- [x] 6.6 Add deterministic tests for policy differences, applied/suppressed previews, aggregation order, cap behavior, fingerprint stability, launch mismatch, status interaction, no post-launch mutation, retry reuse, and restore rejection.

## 7. Sunny Orchard hub visual framework

- [x] 7.1 Extend `RuntimeUiTheme`, copy catalog, semantic component types, and shared drawing helpers for hub top bar/navigation, resource balances, Activity cards, growth tabs, equipment slots, cultivation nodes, and growth preview.
- [x] 7.2 Audit the current ArtSet for reusable surfaces/statuses, add only required hub/domain icons or new anatomy through owned text-free source/runtime exports, and update the single complete ArtSet schema and validation.
- [x] 7.3 Implement selected/pressed/loading/disabled/locked/success/completed/warning/error non-color cues, minimum 44-point targets, reduced-motion behavior, and page-level primary-action hierarchy.
- [x] 7.4 Update `docs/ui/ui-visual-system.md` with the accepted stable hub anatomy, state matrix, page hierarchy, and art-production rules after runtime evidence confirms the final implementation.
- [x] 7.5 Run typography, contrast, optical alignment, nine-slice, importer, source/runtime ownership, mixed-set, occupied-bounds, and safe-area validation for all new hub states.

## 8. Integrated acceptance and cleanup

- [x] 8.1 Extend the dedicated acceptance catalog/bridge with fresh-profile Home, Activity, Equipment, Cultivation, policy-preview, save-failure, and end-to-end reward-to-Battle states without exposing them in ordinary release.
- [x] 8.2 Run focused content/profile/progression/growth tests and `FruitDefense.Editor.ProjectSetup.SmokeValidate`, fixing every in-scope failure.
- [x] 8.3 Build ordinary release and dedicated acceptance WebGL from the same revision and verify the ordinary build exposes no acceptance route, synthetic reward, or injected growth path.
- [x] 8.4 Capture canonical live WebGL evidence across 360×800, 375×812, 402×874, and 430×932 full/inset matrices plus the real desktop host, including navigation, claim, upgrade/equip, preview, launch, Settlement return, and Retry interactions.
- [x] 8.5 Record manifest/content/profile/growth/build identities and geometry/quality outcomes, complete manual visual review, and remove all one-shot runners, marker files, disposable debug commands, and acceptance helpers not owned by the permanent suite.
- [x] 8.6 Run strict OpenSpec validation and update the relevant release baseline/gate evidence without claiming Douyin or WeChat support from ordinary WebGL success.
