---
name: snapshot-schema-bump
description: >-
  Add, remove, or reorder a field in the match-engine world-state snapshot and bump
  SNAPSHOT_SCHEMA_VERSION correctly — deciding whether the state is genuinely cross-tick, keeping the
  serializer and deserializer symmetric, adding the version pin and single-field digest probe, proving
  the exclusion set, and locking a save/restore round-trip. Use this skill whenever a change adds
  engine state that survives a tick, whenever SNAPSHOT_SCHEMA_VERSION or any *_FORMAT_VERSION is
  touched, whenever save/load/replay/restore determinism is in play, and whenever a code review asks
  "does this need serializing?". Trigger it even when the field looks trivial — a one-line latch or an
  RNG cursor is exactly the kind of omission that has silently broken restore here before.
---

# Snapshot Schema Bump

`SNAPSHOT_SCHEMA_VERSION` has gone 1 → 19 in this repo. The failure mode when it goes wrong is not a
red test — it is a save that loads cleanly and then diverges, which is the worst shape of defect
available. Two of the nineteen bumps exist purely because an earlier landing missed a field.

## First: which version are you bumping?

The repo carries several independent format versions and they mean different things. Getting this
wrong versions the wrong thing and lets a genuinely incompatible payload through:

| Constant | Owner | Versions |
|---|---|---|
| `MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION` | `src/match-engine/` | the world-state **body** inside the payload |
| `DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION` | `src/deterministic-sim/` | the `SnapshotHeader` **framing** |
| `MATCH_SAVE_FORMAT_VERSION` | `src/match-engine/` | the on-disk match-save file frame |
| `SEASON_SAVE_FORMAT_VERSION` / `SEASON_STATE_FORMAT_VERSION` | `src/season-save/` | the season file frame / the season state blob |
| `WORLD_STORE_FORMAT_VERSION` | `src/living-world/` | the living-world composite save |

A file frame wrapping unchanged opaque sub-blobs bumps only the frame — that is how the season save
added a third sub-blob without touching any inner version.

## Step 1 — Is the state actually cross-tick?

This is the whole decision, and it is easy to get wrong in both directions.

**Bump when** the field is read on a later tick than it was written. Serialize it even if nothing
restores it yet: `_rosterClubId` was serialized at v16 with no restore consumer, because a save must
record *which squad each team loaded* — boot-constant identity is still identity.

**Do not bump when** the field is written and consumed inside one tick and reset each `RunInputPhase`.
`_prevTickBallPosition` is within-tick despite its name (the `RestartAppliedThisTick` class), and the
richer `LiveMatchFrame` added no version because its new fields were read-only copies of already
serialized state.

Three categories get missed almost every time — check each explicitly:

- **RNG stream cursors.** v17 exists because the `match-flow.card-severity` cursor was unserialized,
  so restore re-registered the stream at cursor 0 and the next card draw diverged. Any match
  containing a booking was silently non-restorable. Note the streams that *don't* need it: collision
  self-seeds from `matchSeed ^ frameNumber` and pass/shot error is hash-based, so both are
  tick-reconstructible.
- **Latches and one-shot flags.** v18 carries `_saveCommittedForGk` / `_headerCommittedThisEpisode`
  because they *gate re-commits* — a restore that omitted them re-fires a suppressed trigger. Same
  class as the half-time/full-time fired flags.
- **Buffers that are recomputed rather than reseeded.** The GK-flag-flip divergence was
  `SlotComposer` skipping GK-flagged agents, leaving a slot's composed position frozen at a stale
  value that a restored engine boot-seeds differently. If a subsystem holds a buffer it only
  partially rewrites, either serialize it or make the skipped path a pure function of serialized
  state — the second is usually better and is what was done there.

## Step 2 — Write and read symmetrically

`MatchEngine.SerializeWorldState` and `DeserializeWorldState` are line-for-line mirrors. Append the
new block **last** so no existing offset moves, and reconstruct through each subsystem's
`RestoreState` seam rather than poking fields — the seams validate, raw pokes do not.

Two structural rules the reader depends on:

- The event ledger is appended after the world state by `RunSnapshotPhase` and is *replayed forward*,
  not restored. The reader validates that the world-state read ended exactly at the ledger domain-tag
  boundary; that boundary check is the drift detector, so don't replace it with a byte-exact total.
- Length prefixes go through a bounds-checked read (`ReadCount` / `Require`) computed as
  `remaining / width`, never as `count * width` — a crafted count overflows the product and slips
  past the guard.

## Step 3 — Update the exclusion proof

Every field deliberately *not* serialized needs its reconstruction argument written down next to the
serializer. `_possessingAgentId` / `_prevPossessingAgentId` are excluded because the snapshot-time
invariant `_prev == _poss == MatchContext.PossessingAgentId` makes one serialized field sufficient —
and a reader written without that reasoning diverged on any match that developed possession.

If you add a field, also re-read the existing proof: a stale "no cross-tick state excluded" note is
how v17 hid for months.

## Step 4 — Tests

Three locks, all in `src/match-engine/tests/`:

1. **The pin**, in `MatchEngineSnapshotSchemaTests` — assert the literal new number with the standing
   message: *"SNAPSHOT_SCHEMA_VERSION drifted — bump it intentionally only with a field-set/order
   change."* This is what makes an accidental bump visible in review.
2. **A single-field digest probe**, named `<Field>_FeedsSnapshotDigest` — mutate only the new field
   and assert the digest moves. This proves the field actually reaches the preimage rather than being
   written into a block nothing hashes.
3. **A restore round-trip** in `MatchEngineSnapshotRestoreTests` — `save@N → restore → tick to N+K`
   byte-identical to an uninterrupted run, exercising a state where the new field is *non-default*.
   A round-trip over a zeroed field proves nothing; the v17 lock deliberately takes its save after a
   booking so the cursor is non-zero.

## Step 5 — Declare the digest consequence

State in the commit message and the design supplement whether the bump moves existing digests, and
whether the pass introduced **a new RNG stream, domain tag, draw site, or draw-order change**. Most
bumps move digests for any match exercising the new field and change none of the four — say that
explicitly. A comparative round-trip contract (restore matches uninterrupted) is the normal proof; an
absolute golden rebaseline is a separate, heavier decision that needs stating.

There is no Stage-0 migration path: a file at the old version is rejected fail-loud. That is
deliberate, so don't quietly add tolerance for the previous version.

Finish with the `dotnet-gate` skill.
