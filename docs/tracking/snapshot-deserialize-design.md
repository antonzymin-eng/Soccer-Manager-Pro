# Snapshot Deserialize / Replay Path — Match Engine (Design Supplement)

> **Created:** July 20, 2026
> **Last Updated:** July 20, 2026 (v0.7 — **Phase 1 READER LANDED — G3 round-trip determinism GREEN.**
> The remaining Phase-1 slices are implemented: the missing `RestoreState` counterparts
> (Pressing/Defensive/Attacking `RestoreState(in XxxTickState)`, Perception `RestoreState(in
> PerceptionTickState)`, Positioning `RestoreState(HysteresisState)`, `MovementCommand.
> ReconstructFromSnapshot`; RotationController's `Restore*` seams already existed);
> `MatchEngine.DeserializeWorldState` (the symmetric line-for-line mirror of `SerializeWorldState`,
> reconstructing subsystem state through the restore seams — KD-1/KD-2); the static
> `MatchEngine.RestoreFromSnapshot(in SnapshotHeader, SnapshotPayload, ulong matchSeed)` factory
> (KD-4/KD-6: fingerprint gate step 0 → boot + `EventBus.ResetForNewMatch` → deserialize → KD-3
> distinct-squad fail-loud → `SnapshotCodec.CommitLoadedDigest` + clock restore, KD-5); and the G3
> acceptance suite `MatchEngineSnapshotRestoreTests` (neutral kickoff / mid-match tactics changed /
> the KD-8 booking-cursor regression, plus version-gate + trailing-byte + distinct-squad fail-loud).
> **Two implementation findings folded in during landing, both verified against source:** (1) the
> excluded `_possessingAgentId`/`_prevPossessingAgentId` (folded into `MatchContext.PossessingAgentId`,
> the writer's exclusion proof) ARE read at the start of the next tick, so the reader reconstructs both
> from the restored MatchContext — leaving them at the boot sentinel diverged any match that had
> developed possession (a first-touch control); the `_prev == _poss == MatchContext.PossessingAgentId`
> invariant at snapshot time (Snapshot phase, after Resolve authors the context + the possession
> producer runs) makes the one field sufficient. (2) The KD-1 "cursor consumed exactly
> `payload.BytesWritten`" trailing guard was too strict: `RunSnapshotPhase` appends the digest-load-
> bearing **event ledger** (`EventBus.SerializeLedger` — a 1-byte domain tag + u32 count + any Tier A
> records) AFTER the world state, so the payload's `BytesWritten` includes it. The reader does NOT
> restore the ledger (the engine replays it forward, and the saved tick's ledger is baked into the
> digest the factory inherits via the header), but it validates the world-state read ended exactly at
> the ledger domain-tag boundary (the R1 drift check, stronger than a bare boundary length compare)
> and byte-exact-accounts the empty-ledger common case. **KD-3 fail-loud kept in Phase 1** (a
> non-sentinel roster reference refuses restore rather than silently diverging); the actual
> re-projection + `ISquadProvider` param stays Phase 2. **No `SNAPSHOT_SCHEMA_VERSION` change** (the
> reader is a pure addition over the v17 writer). **Full dotnet gate: PASSED, 0 failures (257
> match-engine tests, whole-tree green).** See §9 v0.7. Prior v0.6 — **PROMOTED to
> `match-engine-design.md` Phase G; Phase 1 KD-8 writer half LANDED** — `SNAPSHOT_SCHEMA_VERSION` 16 → 17 serializes the `match-flow.card-severity`
> `RngStreamState` cursor, so a save after a booking round-trips deterministically (AR-3's High, the
> writer-side gap). Remaining Phase-1 slices: the reader / `RestoreState` seams / factory / G3 test.
> See §9 v0.6. Prior v0.5 — **AR cycle CONVERGED.** Self-adversarial reviews AR-1
> (0H+3M+2L), AR-2 (0H+1M+2L), AR-3 (1H+0M+1L), and **AR-4 (0H+0M+1L — L-only ⇒ convergence)** all
> folded in and resolved; see §9 Version History. AR-4 re-walked the full surface fresh-eyes (traced
> the digest-chain math A↔C, the boot-registers-stream-before-restore ordering, and the
> reservation-closed-at-snapshot-time invariant — all sound) and found only one L: the
> `ISquadProvider` was ambiguously placed between `DeserializeWorldState` and the factory; pinned to
> the factory. An L-only round closes the cycle per the project convention. **Ready to promote** into
> `match-engine-design.md` as a phase and open the implementing work.)
> **Status:** DESIGN SUPPLEMENT — **AR-CONVERGED; PROMOTED to `match-engine-design.md` Phase G;
> Phases 1 + 2 LANDED** (Stage 0+1 integration scaffolding; NOT a numbered spec, same governance
> class as `match-engine-design.md` and `squad-roster-reference-design.md`). The **KD-8 writer half
> landed July 20, 2026** (`SNAPSHOT_SCHEMA_VERSION` 16 → 17 — the `match-flow.card-severity`
> `RngStreamState` cursor; `MatchEngine.cs` v1.40 / `MatchEngineConstants.cs` v1.24). The **reader half
> landed July 20, 2026** (`MatchEngine.cs` v1.41 — `DeserializeWorldState`, the `RestoreState` counterpart
> seams on Pressing/Defensive/Attacking/Perception/Positioning + `MovementCommand.ReconstructFromSnapshot`,
> the `RestoreFromSnapshot` factory, and the G3 round-trip determinism test suite); no schema change.
> **Phase 1 is complete.** **Phase 2 LANDED July 20, 2026** (`MatchEngine.cs` v1.42): the `ISquadProvider`
> seam (`ISquadProvider.cs`) threaded into `RestoreFromSnapshot(…, ISquadProvider squads = null)`, and
> `ReprojectDistinctSquads` replacing the Phase-1 fail-loud — for each team with a non-sentinel
> `_rosterClubId` it resolves the roster (ClubId-checked, size/record validated, both teams before any
> apply), re-runs `LineupSelector` + `PlayerAttributeProjection` for the base lineup (attribute arrays
> only; the bench GK flags re-projected too since `_benchIsGoalkeeper` is a boot-constant NOT serialized;
> the on-pitch serialized GK flags stay the restored value), then replays the substitutions the serialized
> `_activeBenchSlot` records. Fail-loud on absent/unresolvable/mismatched roster (R4). G3 round-trip
> determinism green for a distinct (varied-attribute) squad, a mid-match substitution, a post-restore
> substitution, and a post-restore keeper-for-keeper substitution; no schema change; full dotnet gate
> PASSED (263 match-engine tests; whole tree green). **Remaining:** Phase 3 (native MXCSR query + on-disk
> `SaveManager` fold — host/upstream-gated). **Discovered (out of scope, recorded as a Phase-1
> snapshot-completeness follow-up):** a post-restore substitution that FLIPS a pitch slot's goalkeeper
> status — subbing a keeper onto an OUTFIELD slot, which realistic play never does — diverges, traced to
> the Positioning AI (#12) producing a different formation slot for that agent after restore (an un-captured
> interaction of the GK-flag flip with the Positioning/rotation state; two fresh engines with the same
> substitution are deterministic, and the base distinct-squad round-trip + realistic keeper-for-keeper and
> outfielder substitutions all round-trip). The Phase 2 attribute re-projection is unaffected.
> **Author:** —
> **Purpose:** Authoritative design for adding a **snapshot-deserialize (load/restore) path** to
> `MatchEngine` — the reader that reconstructs full engine state from the payload
> `SerializeWorldState` already writes. This is the keystone the roadmap's next tier of MVP work
> depends on: distinct-squad restore fidelity (#27 T3), runtime MXCSR float-mode validation
> (#16 §4.8.2), save/load of an in-progress match, and (upstream of a career mode) the unified
> season save. Read `CLAUDE.md`, `src/CLAUDE.md`, and `docs/tracking/match-engine-design.md`
> first.

---

## 0. Scope and governance

The match engine is not covered by any of the 26 approved specs (`SPEC_INDEX.md` remains the
canonical list). It is Stage 0+1 integration scaffolding governed by
`docs/tracking/match-engine-design.md`. This note extends that governance to the one capability the
integration has always lacked: **a read path for the world-state snapshot.**

`MatchEngine.SerializeWorldState` (`MatchEngine.cs` v1.39, `SNAPSHOT_SCHEMA_VERSION` 16) writes the
full cross-tick world-state field set field-by-field through `CanonicalSerializer` into a
`SnapshotPayload`. **Nothing reads it back.** Verified: there is no `Read`/`Deserialize`/
`LoadSnapshot`/`RestoreFromSnapshot` in `MatchEngine.cs` (only the per-component `RestoreState`
seams below, none of them driven from a payload). This is recorded across the tracking docs as the
gate several deliverables sit behind:

- **#27 T3 distinct-squad restore** — the roster reference `_rosterClubId[]` is serialized (v16) but
  "Full distinct-squad restore still needs a snapshot-deserialize path to re-project the records
  from the referenced roster … none exists in the engine yet (KD-T3-3), so building the
  re-projection consumer now would be a phantom." (`squad-roster-reference-design.md`; the
  `SerializeWorldState` exclusion proof says the same.)
- **#16 §4.8.2 runtime MXCSR validation** — "with no snapshot-deserialize/replay path in
  `MatchEngine.cs` it also has no consumer yet" (root `CLAUDE.md` OPEN ISSUES, `EnvironmentFingerprint`
  entry).
- **Save/load of an in-progress match, replay/rewind, and the unified season save** — all read a
  snapshot back; none can exist without this path.

This note designs that reader. It does **not** design the on-disk save-file format, the transfer
market, or aging — those are separate Stage-1 deliverables that will *call* this reader (see §7).

### 0.1 What already exists (the writer + the component seams)

The reader is not building from nothing. Two things are already in place:

1. **The writer** — `SerializeWorldState` (`MatchEngine.cs:3106`) writes, in a pinned order gated by
   `SNAPSHOT_SCHEMA_VERSION`: the schema version + tick; ball state; per-agent `AgentState` +
   ancillary (`_teamIds`, `_isGoalkeeper`, `_isCollisionKnockdown`, `_collisionForces`, held
   `MovementCommand`, Pass/Shot executor state, `DecisionTreeState`); `MatchContext`; per-team
   Positioning/Pressing/Defensive/Attacking hysteresis; Perception internal state; per-team + per-
   agent `TeamTactic`/`PlayerTactic` (active+pending); #23 marking dwell; #24 build-up + settled-team;
   #25 rotation binding/cache/pairs; #26 manager state; score + last-holder; discipline + substitution
   + match-flow clock; and (v16) `_rosterClubId[]`.

2. **Partial restore seams.** Some subsystems already expose a restore counterpart to their
   `CaptureState`, others only a `CaptureState`. Inventory (to be completed in Phase 1):

   | Surface | Capture seam | Restore seam today | Phase-1 work |
   |---|---|---|---|
   | `MatchClock` | — | `RestoreFromSnapshot(tick)` ✓ | none |
   | `DecisionTree` (×22) | `CaptureState()` | `RestoreState(state)` ✓ | none |
   | Pass/Shot executors (×22) | `CaptureState()` | **none** | add `RestoreState` |
   | `OscillationGuard` (in `AgentState`) | `GetState()` | `RestoreState`/setter ✓ (B0 seam) | verify |
   | Positioning `HysteresisState` (×2) | `CaptureState()` | **none** | add `RestoreState` |
   | Pressing/Defensive/Attacking tick state (×2 each) | `CaptureState()` | **none** | add `RestoreState` |
   | Perception internal (×1) | `CaptureState()` | **none** | add `RestoreState` |
   | `RotationController` (×2) | `CaptureRotationState()` | **none** | add restore |
   | `DeterministicRngService` card-severity stream | `GetStreamState(idx)` | `RestoreStream(idx, in state)` ✓ | **writer** must serialize the stream (KD-8) |
   | Ball / agents / tactics / dwell / manager / score / discipline / roster | direct field writes | `TestOnly_Set*` (internal, test-only) | promote to production restore (KD-3) |

   The RNG row is the exception to "the writer already writes everything": the restore *seams*
   exist (`GetStreamState`/`RestoreStream`, the `WorldStore` world.text precedent), but the
   **writer omits the stream** — the one genuine gap AR-3 found (KD-8).

   The `TestOnly_Set*` seams (`TestOnly_SetAgent`, `TestOnly_SetBall`, `TestOnly_SetGoals`, …) prove
   the fields are individually settable but are test-only by contract and cover only a subset; the
   reader must not depend on them.

---

## 1. Goals and non-goals

**Goals (this note):**

- **G1** — A `DeserializeWorldState` reader that consumes a `SerializeWorldState` payload and
  reconstructs the engine's full cross-tick state, byte-symmetric with the writer.
- **G2** — A public entry point that produces a *ready-to-tick* `MatchEngine` from a snapshot (boot
  + deserialize + digest-chain restore + EventBus reset).
- **G3** — **Round-trip determinism** as the acceptance criterion: `save at tick N → restore →
  tick to N+K` produces a digest chain byte-identical to an uninterrupted run ticked to `N+K`
  (KD-5). This is the property that makes the reader *correct*, not merely *present*.
- **G4** — The **seam** for distinct-squad re-projection (#27 T3, KD-3) and for MXCSR validation
  (#16 §4.8.2, KD-6), even where the consumer lands in a later phase.

**Non-goals (deferred — see §7):**

- **N1** — On-disk save-file format / `SaveManager` wiring. `SaveManager` exists but writes the
  header `Fingerprint = null` and there is no season save-file root; this note produces the
  *in-memory* reader those will call.
- **N2** — The unified match/season save (folds the living-world `WorldStore` composite into the
  same file). Blocked on N1 + FR-LW-003.
- **N3** — Replay *scrubbing / rewind UI*. The reader enables it; the presentation is Stage-1 UI.
- **N4** — Transfer market, aging, career progression (`#27` Stage-1+, master plan §4.3/§4.4).

---

## 2. Key decisions

### KD-1 — One symmetric reader, version-gated, fail-loud on mismatch

`DeserializeWorldState(SnapshotPayload payload)` reads the exact field set `SerializeWorldState`
writes, in the same order, through `CanonicalSerializer` read primitives (`ReadU32`/`ReadU64`/
`ReadI32`/`ReadF32`/`ReadBool`/`ReadU8`). The reader is authored as the line-for-line mirror of the
writer and the two are kept adjacent in the file so a future field addition is edited in both places
in one diff.

The **first field read is the schema version.** If it does not equal
`MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION`, the reader throws (fail-loud) — **there is no
cross-version migration at Stage 0.** A snapshot is loadable only by an engine build whose schema
version matches. (Rationale: migration is a Stage-1+ save-compatibility concern; at Stage 0 the
schema bumps freely with every feature, and a silent partial-read against a shifted field layout is
exactly the corruption class the digest chain exists to catch. Same posture as
`WorldStateSerializer`'s version gate and `SnapshotCodec.ValidateHeader`.)

**After** reading the full payload, the reader asserts the cursor consumed exactly
`payload.BytesWritten` bytes — a trailing-byte / short-read guard (the `WorldStateSerializer`
`ReadCount` / trailing-byte precedent). A mismatch throws.

### KD-2 — Reconstruct through restore seams, not raw field pokes

Where a subsystem owns internal state behind a `CaptureState` seam (executors, the four Mechanics-AI
hysteresis surfaces, Perception, `RotationController`), the reader reconstructs the state *struct*
from the payload and hands it to a **`RestoreState` counterpart** on that subsystem — the mirror of
the seam the writer captured through. Phase 1 adds the missing `RestoreState` methods (§0.1 table).
This keeps the ownership boundary the capture seams established: `MatchEngine` never reaches inside a
subsystem's private fields, in either direction. (Mirrors living-world `WorldStateSerializer`'s
"rebuild through the validating store seams" and the existing `DecisionTree.RestoreState` /
`MatchClock.RestoreFromSnapshot` pattern.)

Engine-owned plain arrays (`_teamIds`, `_goals`, `_yellowCards`, tactics, dwell, `_rosterClubId`,
…) are written directly by the reader into the same fields the writer read from — these are
`MatchEngine`'s own state, so a direct assignment is not a boundary violation. The `TestOnly_Set*`
seams are **not** reused as the restore path (they are test contracts and cover a subset); the
reader assigns the fields itself in the deserialize method.

### KD-3 — Distinct-squad re-projection: roster-provider seam, keyed by `_activeBenchSlot`

The attribute VALUES (`_canonicalAttrs`/`_attrs`/`_dtAttrs`/`_perceptionAttrs`/bench) are **not**
serialized (the B3 boot-constant exclusion, re-affirmed through v16). On a **default/neutral** boot
they are reconstructed identically at boot (`CreateDefault()`), so a default-path restore needs no
re-projection — **Phase 1 restores default-path matches exactly with no roster provider.**

For a match booted through `ConfigureSquads` with **distinct** squads, the attributes are a
projection of the loaded `Squad` records, and the payload carries only the **identity** — each
team's `_rosterClubId` (v16) — not the values (#27 T3 / projection-design KD-P10). To restore
attribute fidelity the reader needs the actual rosters back. Design:

- The **factory** `RestoreFromSnapshot` (KD-4) owns the optional
  **`ISquadProvider`** (`Squad ResolveByClubId(int clubId)`), a caller-supplied map from `ClubId`
  to the `Squad` that was loaded, and threads it into the re-projection step. `DeserializeWorldState`
  itself stays provider-free (it reads the payload into fields per KD-1/KD-2); the re-projection is a
  distinct step the factory runs against the just-deserialized `_rosterClubId[]` + `_activeBenchSlot[]`.
- If both teams' `_rosterClubId == NO_ROSTER_CLUB_ID (-1)` (unconfigured / neutral), no provider is
  needed and none is consulted.
- If either `_rosterClubId != -1`, the reader **re-runs the projection** (`PlayerAttributeProjection`
  via the same path `ConfigureSquads` uses) from the resolved `Squad`, seeding the per-slot
  canonical/derived attribute records. Bench-swap fidelity is preserved by re-projecting **keyed by
  the serialized `_activeBenchSlot[]`** (which pitch slot currently holds which bench player) — the
  reason `_activeBenchSlot` is serialized and the mechanism KD-P10 named.
- **Fail-loud** if `_rosterClubId != -1` and no provider (or the provider returns no `Squad` for
  that ClubId): a distinct-squad match cannot be faithfully restored without its roster, and a
  silent fall-back to `CreateDefault()` would produce a match that *diverges from the saved one*
  (different attributes ⇒ different agent behaviour ⇒ digest mismatch on the very next tick). The
  reader must not paper over a fidelity gap it cannot close.

This is the T3 restore consumer KD-T3-3 deferred; it lands in **Phase 2** (Phase 1 handles the
neutral path, which is every match that never calls `ConfigureSquads`).

### KD-4 — Public entry point: a static restore factory, not an instance mutator

Restore is exposed as a **static factory** —
`MatchEngine.RestoreFromSnapshot(in SnapshotHeader header, SnapshotPayload payload, /* boot inputs */, ISquadProvider squads = null)`
— that:

0. **Validates the fingerprint first** (KD-6): checks `header.Fingerprint` against the live host
   *before any state is touched*, so a rejected restore mutates nothing.
1. Constructs a fresh `MatchEngine` through the normal boot path (wires `TickOrchestrator`,
   `SnapshotCodec`, EventBus registrars, seeds boot constants) using the boot inputs the payload
   does **not** carry (RNG seed, formation, etc. — see KD-7 open question).
2. Calls `EventBus.ResetForNewMatch()` so the process-static bus is clean for the restored match
   (the existing per-match reset seam; match-engine design Risk #4).
3. Runs `DeserializeWorldState(payload)` (+ KD-3 re-projection) to overwrite the boot-seeded
   cross-tick state with the saved state.
4. Restores the **digest chain** from `header` and the clock to the saved tick (KD-5).

The entry point takes **both** the `SnapshotHeader` and the `SnapshotPayload`: the payload carries
cross-tick *state* (what `SerializeWorldState` writes), while the header carries the framing the
restore needs — `EnvironmentFingerprint` (KD-6), the digest chain (`PrevSnapshotDigest` /
`CurrentSnapshotDigest`, KD-5), the tick, and the schema/digest versions. A save artifact is the
(header, payload) pair; the reader consumes both. A static factory (rather than a `Load()` method on an already-running engine) is chosen because boot
does load-bearing wiring that must happen exactly once and before any state is applied, and because
a "half-booted, half-restored" instance is not a valid intermediate state to expose. (Mirrors
`WorldStore.Restore` rebuilding + re-wiring the loop; and #16 `ReplayEngine.PrepareReplay`.)

### KD-5 — Digest-chain continuity is the correctness contract

A snapshot is one link in the chained digest (`SnapshotHeader.PrevSnapshotDigest` →
`CurrentSnapshotDigest`, `SnapshotCodec`). After restore, the engine must continue the chain from
the saved link so that the **next** tick's digest equals what an uninterrupted run would produce.
The factory restores the codec's `_prevDigest` chain state from the saved header via
`SnapshotCodec.CommitLoadedDigest` (the existing "loaded digest" seam) and restores
`MatchClock` to the saved tick (`RestoreFromSnapshot`).

**Acceptance test (G3):** boot match A, tick to N and snapshot → (headerₙ, payloadₙ); keep ticking A
to N+K, recording A's digest at each tick. Separately, `RestoreFromSnapshot(headerₙ, payloadₙ)` →
engine C, and tick C K more ticks recording its digest. C's digest at every tick N+1 … N+K must
equal A's at the same tick. (C is a fresh engine produced by the factory — no separately-ticked
engine is needed; A is kept running only to produce the reference chain to compare against.) This is
the single test that
proves the reader captured *everything* cross-tick — any omitted field diverges the chain within K
ticks (the exact failure mode the writer's per-field exclusion proofs guard against, now checked
from the read side).

### KD-6 — MXCSR / EnvironmentFingerprint validation belongs at the restore seam

`#16 §4.8.2` runtime float-mode (MXCSR) validation — reject a snapshot whose
`EnvironmentFingerprint` does not match the live host's float mode — is a **load-time gate**: it
runs when a snapshot crosses into a process, which is exactly this reader. The factory validates the
`header.Fingerprint` against a **live** fingerprint as **step 0** of restore (KD-4), before any
state is touched (`EnvironmentFingerprint.ValidateAgainst(live)` returns
`ERR_DS_REPLAY_ENV_MISMATCH` on any field difference, `0` on match — a return code, so the factory
checks it and refuses; it does not throw on its own). **Caveat (AR-3 L-1):** the gate is only as
strong as the *live* fingerprint it compares against, and constructing a truthful live fingerprint
requires reading the host's actual float mode — which is the same native MXCSR query below that
stays host-blocked. Until that lands, Phase 1 can wire the call against the recorded / dev
fingerprint factory (`CreateStage0Dev` / `CreateStage0MonoCertified`), where it functions as a
self-consistency check (schema/tuple sanity), and it becomes a real float-mode gate only once the
live source exists. The **native MXCSR query**
(reading live float-mode flags) is the still-unbuilt half (native interop, host-blocked); this note
**defines the seam and the call site** so that when the query lands it has a consumer, and does not
build the query itself (that stays the root-`CLAUDE.md` host-blocked item). Fingerprint validation
against a *recorded* tuple is buildable now and is included.

### KD-7 — The payload is not fully self-describing: boot inputs are a separate input

`SerializeWorldState` writes cross-tick *state*, not the boot *constants* an engine needs to exist
(the RNG match seed, the Stage-0 formation constant, squad wiring). The payload is self-describing
for replay/digest tooling that decodes it in isolation, but reconstructing a *tickable* engine needs
those boot inputs too. **Open question (O1):** either (a) the restore factory takes the boot inputs
as explicit parameters (caller's responsibility to persist them alongside the payload), or (b) a
small **boot-header** block is prepended to the save (seed + formation + schema), distinct from the
digest payload. Recommendation: **(a) for Phase 1** (explicit params — no format change, matches how
Stage-0 authors config in code), with (b) revisited when the on-disk save-file root (N1) is designed,
since that is where a boot-header naturally lives. Recorded as a decision to make at N1, not now.

### KD-8 — RNG stream cursor is cross-tick state the current writer omits; Phase 1 must add it (schema bump)

`MatchEngine` owns a `DeterministicRngService _rng` seeded from `matchSeed`, with one registered
mutable stream: **`match-flow.card-severity`** (`_cardSeverityStreamIndex`), drawn from when a foul
issues a card. Its `RngStreamState` (`RngCursor` + `ActionOrdinal`) advances on each draw and is
**cross-tick mutable state**. `SerializeWorldState` (v16) does **not** serialize it — verified: no
`RngStreamState`/`RngCursor`/`GetStreamState` in the writer. Consequence: on restore, the fresh
engine re-registers the stream at `ActionOrdinal 0`, so the **next card-severity draw after a
restore diverges from the saved run** the moment any foul draw happened before the snapshot — i.e.
the KD-5 round-trip determinism contract silently fails for any match with a booking. (This also
makes the writer's own "CROSS-TICK COVERAGE COMPLETE (D4) — no cross-tick gameplay state is
excluded" exclusion note stale: it predates the card-severity stream added with match-flow
completion, v15.)

The other match-engine randomness sources are **not** affected — they are pure functions of the
tick, reconstructible at the restored tick with no stored state: collision self-seeds from
`matchSeed ^ frameNumber`, and pass/shot error is hash-based on `(agentId, frameNumber, …)`
(match-engine design Phase C: "registers NO `DeterministicRngService` draw sites"). The
card-severity stream is the **only** `DeterministicRngService` stream with a mutable cursor, so it
is the whole of the gap.

**Decision:** Phase 1 adds the card-severity `RngStreamState` (`RngCursor` + `ActionOrdinal`, the
two fields the reservation-atomic draw leaves at rest — the exact `WorldStore.Snapshot` precedent)
to `SerializeWorldState`, and the reader restores it via `DeterministicRngService.RestoreStream`.
This is a **`SNAPSHOT_SCHEMA_VERSION` bump (16 → 17)** and updates the writer's exclusion proof — so
**Phase 1 is not a pure read-only addition**; it carries this one writer change. The bump is
harmless to existing digests (a new field appended last; two same-seed runs still serialize the
identical stream state each tick, so the default digest chain simply gains a field, and the existing
match-engine determinism tests re-baseline exactly as every prior schema bump did).

---

## 3. Phased implementation plan

**Phase 1 — neutral-path reader + round-trip determinism (the keystone).**
Add the card-severity RNG stream state to the writer (KD-8, `SNAPSHOT_SCHEMA_VERSION` 16 → 17); add
the missing `RestoreState` counterparts (§0.1 table); write `DeserializeWorldState` as the symmetric
mirror of `SerializeWorldState` (KD-1/KD-2); add `RestoreFromSnapshot` factory (KD-4) with EventBus
reset + digest-chain restore (KD-5); fingerprint validation against a recorded tuple (KD-6,
recorded-tuple half). **Acceptance:** the G3 round-trip determinism test passes for default/neutral
matches (every match that never calls `ConfigureSquads`) **including a match with a foul/booking
before the snapshot** (the direct H-1 regression), plus the trailing-byte/version-gate fail-loud
guards. One writer change + schema bump (KD-8); the reader is otherwise a pure addition. This phase
alone unblocks save/load and replay for the default path — the bulk of the MVP value.

**Phase 2 — distinct-squad re-projection (#27 T3 consumer). ✅ LANDED July 20, 2026.**
Added the `ISquadProvider` seam + `ReprojectDistinctSquads` (base lineup via `LineupSelector` +
`PlayerAttributeProjection`, then the substitutions replayed keyed by `_activeBenchSlot`) (KD-3); extended the
G3 test to a `ConfigureSquads`-booted distinct-squad match (+ mid-match / post-restore / keeper-for-keeper
substitutions and the provider fail-loud gates). Closes the last open #27 T3 item on the data side.
(Implementation note: `_benchIsGoalkeeper` is NOT serialized — only the on-pitch `_isGoalkeeper` is — so it
is re-projected in `ReprojectBaseLineup`, not left to the payload.)

**Phase 3 — native MXCSR query + on-disk fold (host / upstream-gated).**
Wire the native float-mode query into the KD-6 seam (host-blocked today); then N1/N2 (on-disk
`SaveManager` fold + unified season save) consume the reader. These are separate deliverables that
*call* Phase 1/2; listed here only to show where the reader plugs in.

**The on-disk `SaveManager` fold (N1) LANDED July 21, 2026** — see the companion design supplement
`docs/tracking/match-save-file-design.md` (v0.3, AR-CONVERGED). New `src/match-engine/`
`MatchSaveManager` (atomic `Save(engine, path)` / `Load(path, squads) → MatchEngine`) + the pure
`MatchSaveCodec` (a version-gated blob = the KD-7 boot-`matchSeed` boot-header + the `SnapshotHeader`
incl. its `EnvironmentFingerprint` + the `SnapshotPayload`, fail-loud on version/length-bound/
trailing-byte violation) + `MatchSaveContents`. `MatchEngine` gains a public `MatchSeed` property and
the durable-capture seams promoted `TestOnly_` → production internal (`CaptureDurableHeader/Payload`).
Disk round-trip determinism (the on-disk G3) is green for the neutral path, a booking-before-save
match, and a distinct-squad `ConfigureSquads` match through an `ISquadProvider`; the KD-6 fingerprint
gate now runs end-to-end through disk (O3 closed for the on-disk path — the on-disk header no longer
writes `Fingerprint = null` the way the deterministic-sim `SaveManager` does). No `SNAPSHOT_SCHEMA_
VERSION` change (a file frame around the unchanged reader/writer). Full dotnet gate PASSED (279
match-engine tests; whole tree green). **Remaining in Phase 3:** the native MXCSR live-mode query
(host-blocked) and N2 (the unified match/season save, blocked on FR-LW-003 + the season save-file
root).

---

## 4. Risks

- **R1 — Writer/reader drift.** A future field added to `SerializeWorldState` but not
  `DeserializeWorldState` silently truncates restore. Mitigations: the two methods are kept adjacent
  and edited together; the trailing-byte guard (KD-1) turns an under-read into a fail-loud; the G3
  round-trip test turns a mis-ordered read into a digest divergence. The schema-version gate does
  **not** catch drift within one version (both writer and reader are at the same version) — the
  trailing-byte guard + G3 are the real defenses. Recorded so the guard is treated as load-bearing,
  not cosmetic.
- **R2 — Excluded-field assumption rot.** The reader's correctness rests on the writer's
  boot-constant exclusion proof (`_attrs`/`_perfs` reconstructible at boot). If a future change makes
  one of those cross-tick (the PHASE-D `_perfs` note in the exclusion proof), it must be added to
  *both* writer and reader, and Phase 1's neutral-path exactness would otherwise silently break. The
  G3 test catches it; the exclusion proof must stay the single source of truth for what is / isn't in
  the payload. **AR-3 already found one instance of this rot**: the card-severity RNG stream (added
  v15) became cross-tick but was never added to the writer or the exclusion proof, which still
  claims "no cross-tick gameplay state is excluded" (KD-8). Phase 1 closes it and re-verifies the
  proof; the lesson is that a new `DeterministicRngService` draw site is cross-tick state and must
  land in the snapshot in the same change that adds it.
- **R3 — EventBus process-static state.** Restore into a process that already ran a match must
  `ResetForNewMatch` (KD-4 step 2) or stale subscriber tables corrupt the restored match. Already the
  match-engine Risk #4 pattern; called out so the factory does not skip it.
- **R4 — Distinct-squad silent divergence.** Covered by KD-3's fail-loud: never fall back to
  `CreateDefault` when the roster reference is non-sentinel.

---

## 5. Acceptance criteria (definition of done, per phase)

- **Phase 1:** G3 round-trip determinism test green for ≥3 neutral scenarios (kickoff-multi-second
  capstone + a mid-match-with-tactics-changed case + **a match with a booking before the snapshot**,
  the KD-8/H-1 regression); version-gate + trailing-byte fail-loud tests; `SNAPSHOT_SCHEMA_VERSION`
  16 → 17 (KD-8) with the existing schema-pin test re-baselined; full dotnet gate PASSED.
- **Phase 2:** ✅ MET (July 20, 2026). G3 green for a distinct-squad `ConfigureSquads` match; fail-loud
  tests for a non-sentinel roster reference with no provider, an unresolvable ClubId, and a mismatched
  returned roster; bench-swap fidelity tests (a mid-match and a post-restore substitution restore the
  swapped-in player's attributes on the correct slot, and a keeper-for-keeper substitution restores the
  un-serialized bench-GK flag).
- **Phase 3:** out of scope for the first landings (host / N1-gated).

---

## 6. Open questions

- **O1 (KD-7):** boot inputs as explicit factory params vs a prepended boot-header block. Recommend
  params for Phase 1; decide the header at N1 (on-disk root).
- **O2:** does Phase 1 promote any `TestOnly_Set*` seam to a production restore method, or does the
  reader assign fields directly? Recommend direct assignment inside `DeserializeWorldState`
  (the `TestOnly_Set*` seams stay test-only; they cover a subset and carry test-only contracts).
- **O3:** should `RestoreFromSnapshot` validate `EnvironmentFingerprint` unconditionally, or only
  when a fingerprint is present in the header? Today `SaveManager` writes `Fingerprint = null`
  (N1-gated). Recommend: validate when present, skip-with-note when null, so Phase 1 works before N1
  lands a real fingerprint — the same graceful posture the FR-PO-052 cert path took.

---

## 7. Relationship to the roadmap (why this is the keystone)

This reader is the single dependency shared by the next tier of MVP work:

- **Save/load an in-progress match** → calls `RestoreFromSnapshot` (Phase 1) + N1 on-disk format.
- **Replay / rewind** → the digest-chain-continuous restore (KD-5) is exactly a rewind primitive.
- **#27 T3 distinct-squad restore** → Phase 2 is its consumer (closes the last data-side item).
- **#16 §4.8.2 MXCSR validation** → Phase 1 defines its seam (KD-6); the native query is its own
  host-blocked half.
- **Career / season persistence** → N2 unified save reads both this payload and the living-world
  `WorldStore` composite through one file root.

Landing Phase 1 alone converts "the match engine can only run forward, once" into "a match is a
loadable, resumable, replayable artifact" — the capability the presentation layer, the career loop,
and cross-platform replay all build on.

---

## 8. Verification approach

Per project convention: this design note is adversarially reviewed to convergence **before** any
code; then the implementation is adversarially reviewed to convergence. The reader's central
correctness property (G3 round-trip determinism) is itself the strongest verification — it exercises
the composed engine, not per-field units, and catches exactly the omission/ordering class of defect
the writer's exclusion proofs were written to prevent, now checked from the read side.

---

## 9. Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.8 | 2026-07-20 | — | **Phase 2 LANDED — distinct-squad re-projection (#27 T3 / KD-3).** New `ISquadProvider` seam (`src/match-engine/ISquadProvider.cs`) threaded into `RestoreFromSnapshot(…, ISquadProvider squads = null)`. `ReprojectDistinctSquads` (`MatchEngine.cs` v1.42) replaces the Phase-1 fail-loud: neutral fast-path returns immediately; each distinct team resolves its roster (ClubId-check + size/record validation, both teams before any apply — the ConfigureSquads validate-both-before-write discipline), `ReprojectBaseLineup` re-runs `LineupSelector` + `PlayerAttributeProjection` for the base lineup (attribute arrays only; also re-projects the bench GK flags `_benchIsGoalkeeper`, a boot-constant NOT serialized — the on-pitch `_isGoalkeeper` stays the restored serialized value), and `ReprojectSubstitutions` replays the attribute half of each substitution the serialized `_activeBenchSlot` records. Fail-loud on absent provider / unresolvable ClubId / mismatched returned ClubId (R4). `MatchEngineSnapshotRestoreTests` v1.1: G3 round-trip for a distinct squad + mid-match substitution + post-restore substitution + post-restore keeper-for-keeper substitution, plus the three fail-loud provider gates. No `SNAPSHOT_SCHEMA_VERSION` change. **Full dotnet gate PASSED (263 match-engine tests; whole tree green).** Implementation-time finding folded in: `_benchIsGoalkeeper` is NOT serialized (only the on-pitch `_isGoalkeeper` is), so it must be re-projected for a post-restore substitution to bring a bench keeper on with the correct flag — added, locked by the keeper-for-keeper test. Also surfaced (out of scope, recorded above + in the root CLAUDE.md OPEN ISSUES): a post-restore substitution that FLIPS a slot's GK status (keeper → outfield slot, never done in realistic play) diverges via a Positioning-AI (#12) formation-slot interaction with the GK-flag flip — a Phase-1 snapshot-completeness edge, not a Phase-2 re-projection defect. Only Phase 3 (native MXCSR + on-disk fold) remains. |
| 0.1 | 2026-07-20 | — | Initial design supplement. Scope (snapshot-deserialize / restore path), KD-1..KD-7, phased plan, risks, acceptance criteria, open questions. |
| 0.2 | 2026-07-20 | — | **Self-adversarial review AR-1: 0H + 3M + 2L, all resolved.** M-1: v0.1 KD-4 had the factory validating the fingerprint (KD-6) but never said *when* relative to `ResetForNewMatch` / deserialize — a validation that runs after state is applied wastes the reject; ordered it as step-0-of-restore in KD-6 and O3, before any state mutation. M-2: v0.1 claimed Phase 1 "restores default matches exactly" but did not state that `_activeBenchSlot` is *always* serialized (v15) and therefore available to Phase 1 even though Phase 1 does not re-project — clarified in KD-3 that Phase 1 restores the slot value (so a neutral substituted match round-trips) and only the *attribute re-projection* waits for Phase 2. M-3: v0.1 did not address that `SerializeWorldState` omits the boot RNG seed / formation, so a payload alone cannot rebuild a tickable engine — added KD-7 + O1 (the payload is state, not boot constants). L-1: R1 originally implied the schema-version gate catches writer/reader drift; corrected — same-version drift is caught by the trailing-byte guard + G3, not the version gate. L-2: added the §0.1 restore-seam inventory table so the Phase-1 "add `RestoreState` counterparts" work is enumerated, not hand-waved. |
| 0.3 | 2026-07-20 | — | **Self-adversarial review AR-2: 0H + 1M + 2L, all resolved.** M-1 (contract gap): the KD-4 factory signature took `SnapshotPayload payload` alone, but the `EnvironmentFingerprint` (KD-6) and the digest chain (KD-5) live in `SnapshotHeader`, not the payload — the entry point cannot validate or continue the chain without it. Signature now `RestoreFromSnapshot(in SnapshotHeader header, SnapshotPayload payload, …)`; a save artifact is the (header, payload) pair. L-1: KD-4's numbered step list referenced fingerprint validation (KD-6) and digest restore (KD-5) but did not enumerate them — added as explicit step 0 and step 4. L-2: the KD-5 (G3) acceptance-test description carried a redundant "separately boot the same match, tick to N" step that contradicted "the factory produces a fresh engine C"; rewritten so A is kept running only as the reference chain and C comes solely from `RestoreFromSnapshot`. |
| 0.4 | 2026-07-20 | — | **Self-adversarial review AR-3: 1H + 0M + 1L, all resolved. Every cited engine / deterministic-sim seam verified against source** (`SnapshotCodec.CommitLoadedDigest`, `SnapshotHeader.Fingerprint`, `EnvironmentFingerprint.ValidateAgainst`, `MatchClock.RestoreFromSnapshot`, `EventBus.ResetForNewMatch`, the `CanonicalSerializer.Read*` primitives, and `DeterministicRngService.GetStreamState`/`RestoreStream`) — all present with the claimed shapes; no phantom API cited. **H-1 (load-bearing):** the engine owns a `DeterministicRngService` `match-flow.card-severity` stream whose `RngStreamState` (`RngCursor`+`ActionOrdinal`) is cross-tick mutable state the current writer does NOT serialize (grep-confirmed), so KD-5 round-trip determinism silently fails for any match with a booking before the snapshot — added KD-8 (Phase 1 serializes the stream, `SNAPSHOT_SCHEMA_VERSION` 16 → 17, restore via `RestoreStream`, the `WorldStore` world.text precedent), corrected the Phase-1 "no schema bump / read-only" claim in §3 + §5, added the booking-before-snapshot acceptance case, and updated R2 (the writer's "cross-tick coverage complete" exclusion proof is stale — it predates the v15 card-severity stream; a new `DeterministicRngService` draw site is cross-tick state that must land in the snapshot in the same change that adds it). Scope pinned: card-severity is the *only* mutable RNG stream — collision (`matchSeed^frameNumber`) and pass/shot error (hash-based) are pure functions of the tick, reconstructible with no stored state. L-1: KD-6 said the factory "validates against the live host" without noting that constructing a truthful *live* `EnvironmentFingerprint` is itself the host-blocked MXCSR half; clarified that Phase 1 wires the call against the recorded/dev factory (self-consistency check) and it becomes a real float-mode gate only once the live source exists, and that `ValidateAgainst` returns a code (not a throw). **Because a High was found, the cycle is not converged — AR-4 (fresh-eyes) is the remaining gate before promotion.** |
| 0.7 | 2026-07-20 | — | **Phase 1 READER LANDED — G3 round-trip determinism GREEN.** Implemented the remaining Phase-1 slices: the `RestoreState` counterparts (Pressing/Defensive/Attacking `RestoreState(in XxxTickState)`, Perception `RestoreState(in PerceptionTickState)`, Positioning `RestoreState(HysteresisState)`, `MovementCommand.ReconstructFromSnapshot`; RotationController's `Restore*` seams pre-existed); `MatchEngine.DeserializeWorldState` (symmetric mirror of `SerializeWorldState`, restore-seam reconstruction, KD-1/KD-2); the static `RestoreFromSnapshot` factory (KD-4/KD-6 fingerprint gate → boot + EventBus reset → deserialize → KD-3 distinct-squad fail-loud → `CommitLoadedDigest` + clock restore, KD-5); and `MatchEngineSnapshotRestoreTests` (neutral / mid-match-tactics / KD-8 booking-cursor round-trip + version-gate/trailing-byte/distinct-squad fail-loud). Two findings folded in during landing: (1) the excluded `_possessingAgentId`/`_prevPossessingAgentId` are read at the next tick's start, so the reader reconstructs both from the restored `MatchContext.PossessingAgentId` (the `_prev == _poss == MatchContext.PossessingAgentId` snapshot-time invariant makes one field sufficient) — without this a match that developed possession (a first-touch control) diverged; (2) the KD-1 exact-`BytesWritten` trailing guard was too strict because `RunSnapshotPhase` appends the digest-load-bearing **event ledger** after the world state — the reader does not restore the ledger (replayed forward; baked into the header digest) but validates the world-state read ended exactly at the ledger domain-tag boundary (the R1 drift check) and byte-exact-accounts the empty-ledger case. No `SNAPSHOT_SCHEMA_VERSION` change. Full dotnet gate PASSED (257 match-engine tests; whole tree green). `MatchEngine.cs` v1.41; Pressing/Defensive/Attacking/Perception/Positioning `RestoreState` seams; `MovementCommand.cs` v1.5. Phase 1 complete; Phase 2 (T3 re-projection) and Phase 3 (MXCSR/on-disk) remain. |
| 0.6 | 2026-07-20 | — | **Promoted to `match-engine-design.md` Phase G; Phase 1 KD-8 writer half LANDED.** No design change — the note is implementation-tracked from here. Promotion: `match-engine-design.md` §5 gains the Phase G entry + v2.1 history row; root `CLAUDE.md` OPEN ISSUES gains the tracking entry. Code: the KD-8 writer change landed — `SerializeWorldState` now serializes the `match-flow.card-severity` `RngStreamState` cursor (RngCursor + ActionOrdinal, u64 each — the two mutable fields the reservation-atomic draw leaves at rest), `SNAPSHOT_SCHEMA_VERSION` 16 → 17 (`MatchEngine.cs` v1.40 / `MatchEngineConstants.cs` v1.24); the stale v8 "no cross-tick state excluded" exclusion-proof note is corrected (it predated the v15 card-severity stream). New `TestOnly_SetCardSeverityStreamCursor` seam + `MatchEngineSnapshotSchemaTests` v1.14 (pin 17 + `CardSeverityRngCursor_FeedsSnapshotDigest` probe — advancing the stream cursor with no card moves the digest). Status header updated to reflect Phase-1-in-progress. Remaining Phase-1 slices unchanged (reader + `RestoreState` seams + factory + G3 test). (dotnet gate not runnable in this environment; verified by manual review, CI runs on push.) |
| 0.5 | 2026-07-20 | — | **Self-adversarial review AR-4: 0H + 0M + 1L — CONVERGENCE (L-only round closes the cycle).** Fresh-eyes re-walk of the whole v0.4 surface, with focus on the KD-8 addition and the interactions the earlier rounds had not composed. Verified sound with no change: (a) the digest-chain math — after A produces snapshot N, A's `_prevDigest` = digest_N; `CommitLoadedDigest(headerₙ)` sets C's `_prevDigest` = `headerₙ.CurrentSnapshotDigest` = digest_N, so A and C chain identically from N+1 (the G3 comparison window is exactly right); (b) the RNG restore ordering — boot (KD-4 step 1) registers the card-severity stream before `DeserializeWorldState` (step 3) calls `RestoreStream`, so the stream index is valid; (c) the reservation-at-rest invariant — the card-severity draw is atomic (`Reserve`…`CloseReservation` within `ApplyFoulIfCaptured`, no yield) and snapshots are taken in phase 6 after Resolve, so the reservation is always closed at snapshot time and serializing only `RngCursor`+`ActionOrdinal` is correct (the exact `WorldStore` precedent). L-1: the `ISquadProvider` was ambiguously placed — KD-1's `DeserializeWorldState(payload)` signature omitted it while KD-3 said "DeserializeWorldState / the public entry point" takes it; pinned to the factory (`DeserializeWorldState` stays provider-free; the factory runs re-projection as a distinct step against the just-deserialized `_rosterClubId`/`_activeBenchSlot`), which also reconciles KD-4 step 3. **Cycle converged — ready to promote to a `match-engine-design.md` phase.** |
