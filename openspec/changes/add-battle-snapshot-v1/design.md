## Context

P0 provides deterministic fixed-step simulation, serializable random state, a compiled versioned catalog, and a finite skill runtime. Snapshot support must be added only after those runtime shapes stabilize so the protocol contains every value that can affect later outcomes and none of the presentation-only noise.

## Goals / Non-Goals

**Goals:**

- Export a JsonUtility-compatible V1 DTO and restore it atomically against pinned content.
- Preserve logical step, random state, entity identity, pending attacks, projectiles, and statuses.
- Return structured validation/restore results for corrupt, incompatible, or unavailable content.
- Prove deterministic continuation from JSON round trips.

**Non-Goals:**

- Disk, cloud, lifecycle autosave, or player-facing resume UI.
- Cross-version best-effort mapping or a speculative V1-to-V2 migrator.
- Presentation, selection, drag, modal, or effect history persistence.

## Decisions

1. The snapshot envelope records `schemaVersion=1`, catalog ID, content version, map ID, logical step, random state, phase, resources, lives, wave/spawn state, speed/pause state, and next entity ID.
2. JsonUtility-friendly arrays hold pots, plants, enemies, statuses, projectiles, skill cooldown/burst state, and equipment inventory. Runtime dictionaries/indexes are rebuilt after validation.
3. Restore is atomic: validate and build a candidate state first, then replace the active simulation only on success.
4. Exact catalog ID and content version are required. Missing pinned content returns `ContentUnavailable`; mismatch returns `IncompatibleContent`; invalid data returns structured validation codes.
5. Every entity ID must be positive and unique, all references must resolve, next entity ID must exceed existing IDs, and numeric values must be finite and legal.
6. Restore preserves logical step/random state but resets the frame accumulator to zero.
7. Snapshot checksums exclude JSON field ordering and presentation state and include all future-outcome fields.

## Risks / Trade-offs

- **[A runtime field is omitted]** -> Branch continuation tests save during projectiles, burns, slows, ice count, and machine-gun bursts.
- **[Malformed data partially mutates a live run]** -> Candidate-state construction and validation precede the one commit point.
- **[Content updates reinterpret old values]** -> Require exact content version and retain the snapshot unchanged when pinned content is unavailable.
- **[DTOs leak mutable runtime state]** -> Deep-copy on export/import and rebuild runtime-only indexes.

## Migration Plan

1. Freeze V1 DTOs after the skill-runtime merge.
2. Add export, validation, candidate construction, and atomic restore.
3. Add Ready, Playing, BetweenWaves and mid-effect continuation fixtures.
4. Expose the protocol to P1 stores without adding P0 automatic resume.
