## Context

The release flow is currently `Bootstrap → Lobby → Battle → Settlement`. `AppNavigator` treats Lobby as one route, while `LobbyPresenter` owns one immediate-mode page containing three level cards and Start. The local `PlayerProfileEnvelopeV1` intentionally contains no economy or progression, and `BattleLaunchRequest` contains only session, level, seed, content version, and mode. Consequently there is no authoritative place to navigate outgame features, grant items, mutate growth, or prove which growth values entered a battle.

The ongoing `establish-modular-game-config-manifest` change already establishes one root manifest, deterministic bundled export, stable content identities, and immutable battle configuration. This design extends that authority with a separate outgame catalog; it does not introduce a second root manifest or return to code-authored production values.

The UI remains portrait-first immediate-mode GUI and preserves the approved Sunny Orchard visual language in `docs/ui/ui-visual-system.md`. Draw and hit geometry must continue to originate from the same safe-area-aware layout authority at 360×800, 375×812, 402×874, and 430×932. The design is a major change to the Lobby/pre-battle loop, but `docs/design/game-design-overview.md` remains untouched until the user explicitly approves synchronization.

### Target player loop

```text
┌──────────────┐   claim rewards   ┌──────────────────┐
│ Activity page│ ────────────────▶ │ Inventory/profile│
└──────┬───────┘                    └────────┬─────────┘
       │                                    │ spend/equip
       │                           ┌────────▼─────────┐
       │                           │   Growth page    │
       │                           │ equipment/cultiv.│
       │                           └────────┬─────────┘
       │                                    │ preview permitted growth
       │                           ┌────────▼─────────┐
       └──────────────────────────▶│    Home page     │
                                   │ select gameplay  │
                                   └────────┬─────────┘
                                            │ immutable launch snapshot
                                   ┌────────▼─────────┐
                                   │ Battle/Settlement│
                                   └────────┬─────────┘
                                            └──────▶ Home
```

## Goals / Non-Goals

**Goals:**

- Deliver one usable outgame hub with Home, Activity, and Growth, plus Equipment and Cultivation inside Growth.
- Prove one complete production loop: claim an activity reward, consume or equip it in Growth, preview the permitted effect on Home, enter Battle with that effect, settle, and return.
- Keep item grants and progression writes atomic, revisioned, validated, and idempotent.
- Make every gameplay definition explicitly choose which growth domains and attributes it permits.
- Make the exact applied growth immutable, deterministic, inspectable, and part of battle identity.
- Keep UI semantics, art ownership, safe-area handling, finite copy, touch targets, and visual acceptance aligned with the current runtime UI standard.
- Leave stable extension points for additional equipment, cultivation nodes, activities, and policies through content data rather than new page or simulation branches.

**Non-Goals:**

- A backend economy, trusted server clock, daily reset, remote activity schedule, cloud save, payment, ads, mailbox, battle pass, or remote gameplay-content delivery.
- Randomized equipment instances, affixes, inventory capacity, item selling, dismantling, crafting, or a general-purpose quest language.
- Replacing or reinterpreting the existing battle equipment installed on fruits; Growth-page equipment is a separate account-level outgame domain unless a later design explicitly relates the two.
- Arbitrary reflection-based stat names, scriptable runtime effects, or modifiers that bypass the deterministic combat attribute pipeline.
- Turning Home, Activity, or Growth into Unity scenes or new `AppRoute` values.
- Preserving or migrating `PlayerProfileEnvelopeV1`; the obsolete schema and code path are removed.
- Automatically editing the stable game-design overview before design synchronization is approved.

## Decisions

### Lobby becomes one hub route with an internal finite page router

`AppRoute` remains `Lobby`, `Battle`, and `Settlement`. A pure `HubPageRouter` owns the finite `Home`, `Activity`, and `Growth` page state only while Lobby is active. `LobbyHubPresenter` owns the single `OnGUI`, shared chrome, page transition feedback, and input serialization. Concrete `HomePagePresenter`, `ActivityPagePresenter`, and `GrowthPagePresenter` draw only inside the page-host rectangle; Growth uses a finite `Equipment` / `Cultivation` secondary selector.

This is preferred over adding route values because page switches do not load scenes, destroy sessions, or participate in application recovery. It is preferred over one monolithic presenter because resource chrome, navigation, activity transactions, and growth details have different state and validation needs. The implementation remains concrete and finite rather than introducing a plugin-style page framework.

Page switches are synchronous presentation changes. Cold start and return from Settlement select Home. Switching pages during Lobby preserves each page's local scroll/selection state until Lobby unloads; authoritative profile and content state are never stored only in presenters.

```text
AppNavigator:       Lobby ─────────▶ Battle ─────────▶ Settlement
                      ▲                                  │
                      └──────────────────────────────────┘

HubPageRouter:      Home ◀────────▶ Activity ◀────────▶ Growth
                                                        ├─ Equipment
                                                        └─ Cultivation
```

### One shell layout authority owns draw and hit rectangles

Add `PortraitHubLayout` alongside the existing Shell layout authority. It resolves one safe-area frame and named rectangles for `TopBar`, `PageHost`, and `BottomNavigation`; page-specific layout helpers derive their rectangles only from `PageHost`. The same layout value is passed to drawing, pointer tracking, hit testing, layout validation, and acceptance telemetry.

Navigation items use icon-and-label anatomy, a minimum 44 logical-point target, non-color selected state, and the same persistent position on every page. No page draws over the top bar or bottom navigation. Long lists use clipping/scrolling only inside the page host and retain fixed primary actions where required.

### The visual framework extends Sunny Orchard instead of introducing a second theme

The hub uses the existing `edge-background`, `surface-base`, `surface-raised`, action roles, typography roles, four-point rhythm, shallow depth, and orchard ornaments. New semantic component anatomies are added only where the current set has no owner:

- `hub-top-bar`: page identity plus compact item balances; resource chips remain secondary to the current page title.
- `hub-bottom-nav` / `hub-nav-item`: fixed Home, Activity, Growth destinations with inactive, selected, pressed, disabled, and attention-marker states.
- `activity-card`: illustration/title/body/reward preview/status/finite action with available, claimable, claiming, claimed, locked, and error states.
- `growth-domain-tabs`: Equipment and Cultivation secondary navigation, visually weaker than bottom navigation.
- `equipment-slot` / `growth-node`: identity, rank, effect, cost, selected/owned/max/locked/insufficient/error states.
- `growth-preview`: selected gameplay, applied sources, suppressed sources, finite aggregate attributes, and blocking error state.

The first implementation should reuse current standard/raised panels, selectable cards, action surfaces, status indicators, and slots where their semantics fit. The production ArtSet is revised only for missing navigation/domain icons and genuinely new slot anatomy; no parallel ArtSet, local page colors, baked Chinese copy, or full-page raster is added.

### Portrait visual composition

```text
┌──────────────────────────────┐
│ safe top / orchard edge      │
├──────────────────────────────┤
│ page title       item balance│  shared hub-top-bar
├──────────────────────────────┤
│                              │
│          PAGE HOST           │
│                              │
│ Home                         │ Activity
│ ┌ selected level ─────────┐  │ ┌ activity banner ───────┐
│ │ illustration + identity │  │ │ status / finite copy   │
│ └─────────────────────────┘  │ └────────────────────────┘
│ ┌ effective growth ───────┐  │ ┌ reward card ───────────┐
│ │ applied / suppressed    │  │ │ preview + Claim/state  │
│ └─────────────────────────┘  │ └────────────────────────┘
│ [Start battle]               │
│                              │ Growth
│                              │ [Equipment | Cultivation]
│                              │ [list/grid] [detail/action]
├──────────────────────────────┤
│  Home       Activity   Growth│  shared hub-bottom-nav
└──────────────────────────────┘
```

Home keeps current level selection and Start but adds a compact growth preview between selection and Start. Activity uses one vertically ordered card stack; the initial production activity is bundled, always available, and claimable once, so the loop does not depend on an untrusted local clock. Growth uses a secondary selector and a list/detail composition rather than placing all equipment and cultivation controls on one dense page.

### A separate outgame catalog is pinned by the root game-content manifest

Add an immutable `OutgameContentCatalog` selected by `GameContentManifest`. It owns typed arrays and compiled ordinal-ID indexes for:

- `ItemDefinition`: stackable material identity, presentation identity, and bounded quantity rules.
- `ActivityDefinition`: presentation, bundled availability, one-time claim identity, and finite reward grants.
- `GrowthEquipmentDefinition`: account-level slot identity, unlock grant, ordered ranks, costs, and finite growth contributions; it is distinct from the existing battle-equipment definition installed on fruits during a session.
- `CultivationNodeDefinition`: prerequisites, ordered ranks, costs, and finite growth contributions.
- `GrowthPolicyDefinition`: permitted domains, permitted attribute IDs, optional source filters, and finite caps.

Existing level definitions gain one required `growthPolicyId`. Activity, growth-equipment, cultivation, item, policy, and level references are validated together before Lobby becomes interactive. Authoring remains modular ScriptableObjects, while export produces deterministic canonical JSON and compiled read-only runtime indexes. Production C# factories and fallback definitions are not added.

This separation is preferred over placing inventory/economy definitions inside the battle catalog: outgame data has different ownership and lifetime, while the root manifest still pins both catalogs to one release identity. It is preferred over a generic key/value table because typed definitions provide complete validation and controlled evolution.

### Replace V1 with one progression-capable profile aggregate

Replace `PlayerProfileEnvelopeV1` with the single current profile schema. In addition to identity, revision, settings, and last level, it contains normalized finite collections for:

- item balances (`itemId`, non-negative quantity);
- owned outgame growth-equipment (`growthEquipmentId`, rank) and slot loadout;
- cultivation node ranks;
- claimed activity receipt IDs;
- existing shell preferences.

Collections reject duplicate identities, unknown references, negative balances, out-of-range ranks, illegal growth-equipment slots, unmet cultivation prerequisites, and claim receipts that do not resolve. The store continues to clone and validate a complete aggregate before promotion. An obsolete V1 payload is not interpreted, copied, or migrated; it returns the existing structured unsupported-schema result and can only be replaced through the explicit profile-recovery/reset workflow.

UI presenters receive immutable view projections and command results, never a mutable profile DTO. A concrete `PlayerProgressionService` clones the current aggregate, validates a command against compiled content, applies all debits/credits/state changes, increments revision once, saves once, and publishes the new snapshot only after persistence succeeds.

### Activity grants use idempotent atomic receipts

The initial activity capability supports one finite operation: claim a bundled one-time reward. `TryClaimActivity(activityId)` validates that the activity is active, its receipt is absent, and every grant is valid. It applies all item/growth-equipment grants and the receipt to one cloned profile, then persists atomically.

Repeated input while saving is rejected as in progress; repeated input after success returns already claimed and grants nothing. A failed save exposes a recoverable error and retains the old visible balance and claim state. The initial reward grants at least one starter growth-equipment identity plus enough configured growth material to perform one visible upgrade, proving the requested loop without a server clock or battle-result reward redesign.

### Equipment and cultivation use finite transactional commands

Growth exposes only explicit commands:

- `TryEquip(growthEquipmentId, slotId)` changes the loadout after ownership and slot validation.
- `TryUpgradeGrowthEquipment(growthEquipmentId)` consumes its next-rank cost and increments exactly one rank.
- `TryUpgradeCultivation(nodeId)` validates prerequisites, consumes its next-rank cost, and increments exactly one rank.

Costs are item-quantity lists in content, and all quantities are checked before any debit. There are no partial payments. Maximum-rank actions render as completed, insufficient costs render disabled with a reason, and locked nodes expose their prerequisite instead of a clickable fake action.

Equipment and cultivation contributions share one finite `GrowthContribution` schema but remain separate domains for policy filtering and UI. Attribute IDs are an explicitly supported set consumed by the battle attribute pipeline; arbitrary strings, formulas, reflection, and runtime script hooks fail content validation.

### Each gameplay selects a growth policy and Home previews the exact result

`BattleGrowthResolver` is a pure deterministic service. Its inputs are the compiled outgame catalog, selected resolved level, current validated profile snapshot, and content identities. It performs these steps in stable ordinal-ID order:

1. Read the level's required growth policy.
2. Enumerate equipped outgame growth-equipment and purchased cultivation ranks.
3. Reject invalid source definitions; classify valid contributions as applied or suppressed by domain, source, attribute, and cap rules.
4. Aggregate finite flat, additive, and multiplicative contributions using one documented order.
5. Produce a `BattleGrowthSnapshot` containing profile revision, policy ID, ordered source records, aggregate modifiers, and a canonical fingerprint.

Home uses this same snapshot for the visible growth preview and Start. It does not reimplement effect math. Start is blocked if content/profile validation or projection fails; suppressed but valid sources are explained and do not block launch. Selecting a different level immediately re-resolves the preview against that level's policy.

### Battle receives immutable baseline growth

`BattleLaunchRequest` gains the required `BattleGrowthSnapshot`; initialization validates its profile revision metadata, policy identity, content identity, canonical fingerprint, finite values, and match with the resolved level. The simulation deep-copies it before the first simulation step.

The deterministic attribute order is:

```text
authored base
  → launch baseline growth (equipment/cultivation)
  → battle-owned permanent/tier rules
  → transient runtime statuses/buffs/debuffs
  → finite clamp and final effective value
```

Launch growth is not represented as a timed status and cannot expire, stack through combat events, or be mutated by the profile after launch. Snapshot/source identity includes growth policy and fingerprint so replay, restore, and mismatch validation cannot silently substitute current profile state. Settlement Retry reuses the original session's growth snapshot with a new session ID and seed because Settlement exposes no growth edits; returning to Home resolves a new preview from the current profile.

### Errors and loading are stateful, visible, and non-destructive

Only one activity/progression save may be active at once. While saving, affected actions render loading and hub navigation remains available unless switching would discard a required confirmation; duplicate commands cannot create duplicate debits or grants. Structured errors appear inside the owning activity/growth card and remain retryable. Shared top balances update only after successful persistence.

Fatal manifest/profile/projection errors block Start and use the existing recoverable/blocking shell language. Pages never silently replace missing content, reset costs, apply unpermitted growth, or launch with an empty fallback snapshot.

### Acceptance proves behavior and final pixels

Editor validation covers catalog round trips, cross-references, profile validation, atomic rollback, duplicate claim, cost debit, prerequisites, loadout legality, per-policy filtering, aggregate order, fingerprint stability, launch mismatch, retry identity, and no post-launch profile influence.

Shell validation covers draw/hit parity, navigation, page state, minimum targets, finite text, and full/inset safe areas. Dedicated acceptance WebGL captures Home default/alternate/loading/error and growth previews, Activity claimable/claiming/claimed/error, Growth Equipment and Cultivation owned/locked/insufficient/max states, and the end-to-end reward-to-Battle flow. Ordinary release WebGL remains free of acceptance routing.

## Risks / Trade-offs

- [The growth pipeline can accidentally create a second modifier executor] → Integrate launch baseline modifiers into the existing deterministic effective-attribute resolver and keep transient statuses as the only runtime status executor.
- [Profile replacement invalidates local development saves] → Reject V1 explicitly, document the one-time local reset needed for development, and do not ship a compatibility reader or migration branch.
- [An activity page without server time cannot support trustworthy daily events] → Limit this change to bundled always-available one-time claims and make remote schedules a later capability with explicit platform/backend authority.
- [A generic growth schema can become an unbounded scripting language] → Use finite domains, finite supported attribute IDs, typed flat/additive/multiplicative operations, bounded ranks, and complete export-time validation.
- [Bottom navigation and growth controls can overcrowd narrow portrait screens] → Keep three primary destinations, move Equipment/Cultivation to secondary navigation, use one page-host scroll owner, and validate every supported full/inset geometry.
- [Resource balances and claim actions can visually overpower the battle entry] → Keep top balances compact, preserve Home Start as the only page-level primary action, and use semantic state/action hierarchy from the existing theme.
- [Profile save failure could show rewards that were not persisted] → Publish view state only after successful atomic save; on failure retain the previous profile revision and expose retry.
- [Catalog work overlaps the modular manifest change] → Make that change an implementation prerequisite and extend its root manifest/validation rather than adding a competing loader.

## Migration Plan

1. Complete and validate `establish-modular-game-config-manifest` so the root manifest and deterministic authoring/export path are authoritative.
2. Add the outgame catalog schema, authoring assets, canonical export, compiled indexes, current starter activity/reward/growth content, and cross-catalog validation.
3. Replace the profile schema and validation/store fixtures; add the progression service and atomic activity/equipment/cultivation commands.
4. Add `HubPageRouter`, `PortraitHubLayout`, shared shell chrome, and Home/Activity/Growth presenters; move existing level selection and Start into Home, then remove the obsolete single-page Lobby presenter path.
5. Add growth policy references, resolver, Home preview, launch snapshot/fingerprint, baseline attribute integration, retry behavior, and snapshot/source-identity validation.
6. Extend theme/ArtSet/copy/icon bindings only for approved missing hub semantics and add layout, interaction, deterministic, aggregate Editor smoke, and WebGL acceptance coverage.
7. Replace the local development profile through the explicit reset workflow, run the complete player loop, and build ordinary plus acceptance WebGL from the same revision.

Rollback is source-level only: revert the entire change and rebuild the prior release. No runtime dual-schema profile support, alternate Lobby presenter, empty growth snapshot fallback, or old launch constructor remains after cutover.

## Open Questions

- The production-facing names, icons, and exact numeric grants/costs for the starter activity, starter equipment, and first cultivation node require content review; their structural identities and the requirement that one claim funds at least one real upgrade are fixed here.
- The first supported growth attribute set and per-level caps must be chosen from attributes the current simulation can resolve deterministically; adding a brand-new combat attribute requires its own gameplay specification rather than a free-form catalog entry.
