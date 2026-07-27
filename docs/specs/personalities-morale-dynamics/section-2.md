# Personalities, Morale & Squad Dynamics #33 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 23, 2026
**Last Updated:** July 27, 2026 (v0.4 — back-prop landed atomically with the ten-spec approval wave; see the version-history row)
**Last Updated (prior):** July 23, 2026 (v0.3 — AR-2 fix pass; prior v0.2 AR-1, v0.1 initial)
**Version:** 0.4
**Status:** APPROVED

---

## 2.1 Functional requirements

| FR | Requirement | Priority | KD |
|---|---|---|---|
| FR-HS-001 | All #33 state advances on the **world tick** (`WorldClock`, one day = one `worldTick`) — never the 10 Hz/60 Hz match loops. | MUST | KD-6 |
| FR-HS-002 | #33 owns a per-`PlayerId` `MoraleState` + `PersonalityProfile`, serialized in the #33 sub-blob (KD-7). No other assembly writes them. | MUST | KD-2 |
| FR-HS-003 | #33 owns a **club-scoped** pairwise relationship store — the authoritative vol-2 §2.1 scalar #22 mirrors as `PlayerEdge`. Cross-club pairs are a recorded deep-tier extension. | MUST | KD-1/KD-4 |
| FR-HS-004 | All #33 fields and projections are **integer per-mille**; the **only** float is `edgePermille / 1000f ∈ [0,1]` produced at the #22 mirror boundary (FR-HS-015). No other float enters #33. | MUST | KD-6 |
| FR-HS-005 | Per-player state is created via the `Create()` factories (neutral seeds) and inserted as the **pair** `{MoraleState, PersonalityProfile}`. The **enforced** guard is at **record insertion** (the roster owner inserts only `Create()`-built records; a `default(PersonalityProfile)` — all traits `0 ∉ [1,20]` — MUST fail loud at the insertion validation and at any consuming seam, F4). `default(MoraleState)` alone is field-in-contract (morale `0`, equilibrium `0`, `LastAdvancedWorldDay 0`) so it is **not independently a trap**; note its `LastAdvancedWorldDay = 0` (not the `Create()` sentinel `uint.MaxValue`) would make the §3.1 F6 guard **no-op** a day-0 advance rather than fail loud, which is why insertion-time validation — not the F6 path — is the enforced guard. | MUST | KD-2 |
| FR-HS-006 | At the minimal tier traits are **neutral-seeded** (`TRAIT_NEUTRAL`) and morale seeds to `MORALE_NEUTRAL_PERMILLE` (content) — no variety yet (#27 T0 precedent). | MUST | KD-2/KD-8 |
| FR-HS-007 | `MoraleState.EquilibriumPermille` is an **internal** #33 projection set-point. It is **NOT** routed to #22 (#22 needs no baseline from #33). | MUST | KD-1 |
| FR-HS-008 | `MoraleState.LastAdvancedWorldDay` is an idempotency cursor; its unadvanced sentinel is `HS_NOT_ADVANCED_SENTINEL = uint.MaxValue`, **not** `0`. | MUST | KD-6/F6 |
| FR-HS-009 | `AdvanceHumanSystemsDay` is a **deterministic** function of committed inputs — it makes **no stochastic draw** at the minimal tier. | MUST | KD-6 |
| FR-HS-010 | Morale drifts toward its target (assembled from committed match results / playing time / board state) by an integer per-mille step, clamped `[0, 1000]`. | MUST | KD-2/KD-3 |
| FR-HS-011 | The pairwise relationship scalar is projected deterministically (a neutral hold, or an integer per-mille drift) at the minimal tier — no draw. | MUST | KD-4/KD-6 |
| FR-HS-012 | Advancing the same `worldDay` twice for a player is a **no-op** (`LastAdvancedWorldDay`); a day **gap** (skipping a day) fails loud — #30 advances one day at a time (F6). | MUST | KD-6 |
| FR-HS-013 | Because the minimal tier is draw-free, #33 registers **no** RNG stream and promotes **no** domain tag at approval — `_RESERVED_0x25_` / `SubsystemOrdinals 87` stay reserved. | MUST | KD-6 |
| FR-HS-014 | Any deep-tier stochastic draw MUST be a **position-independent keyed** draw on `(playerId, worldDay, purpose)` on the world-tick `DeterministicRngService` — no free-running cursor is persisted. | MUST | KD-6 |
| FR-HS-015 | #33's #22 read surface is **exactly** the pairwise `PlayerEdge` scalar `∈ [0,1]` per player↔player **ordered pair** (from the internal per-mille via `/1000f`). #33 supplies **no baseline** and no other per-entity quantity into #22 phase-2. | MUST | KD-1 |
| FR-HS-016 | The #22 read is a **pure read**: #33 writes canon, #22 reads a mirror, #33 **never** reads #22's memory / relationship / arc layer (strictly one-directional). | MUST | KD-1 |
| FR-HS-017 | The pairwise scalar is routed into #22 phase-2 as **primitive arrays** (`fromPlayerId[]/toPlayerId[]/edge[0,1][]`) by the `TacticalDirector.SeasonSave` root. **Neither** `living-world` nor `#33` references the other; **#30** is not the router (it is producer-only, FR-SN-017). | MUST | KD-1 |
| FR-HS-018 | #22 mirrors the routed value via a **new** public `MemoryStore.SetPlayerEdgeMirror(fromId, toId, value)` seam (added at #33's T-phase — none exists today); #22's `ApplyEvent` `PlayerEdge` refusal is **unchanged**, and `T-LW-U-035` stays green. This is a #22 **code** addition with **no schema/arc-logic change**. | MUST | KD-1 |
| FR-HS-019 | Wiring #22 phase-2 with an **empty** #33 view leaves #22 output **byte-identical** (`T-LW-U-035`-class). Flowing a **real** #33 view is a **named, separately-reviewed activation**, not behaviour-neutral by design. | MUST | KD-8 |
| FR-HS-020 | Cliques are a **derived read** over the #33-owned pairwise scalar — a clique edge requires the threshold in **both** ordered directions (`> CLIQUE_THRESHOLD_PERMILLE = 600` for `a→b` **and** `b→a`; the mutuality rule, matching #22's `mutual > 0.6`). **No** clique state is persisted (no independent truth). | MUST | KD-4 |
| FR-HS-021 | Squad **chemistry** (a squad-level aggregate) is likewise a **derived read**, persisted **nowhere**. | MUST | KD-4 |
| FR-HS-022 | Minimal mentoring is the **empty identity** (`MentoringPlan.None`). Deep-tier mentoring pairing/propagation defaults to #33's auto-derivation and is overridable by a **#34 staff-driven** routing seam — no #34 interface is built. | MUST | KD-5 |
| FR-HS-023 | Morale reaches the match engine **only** through the read-only #27 attribute-projection seam; consumption is **deferred** (its own reviewed change). #33 owns no match-tick write. | MUST | KD-3 |
| FR-HS-024 | #33 exposes read-only morale accessors for #31/#35/#45. **Amended by ERR-033-004 (at #46's approval): no consumer writes #33 morale, #46 included.** #46's man-management seam **is** the routed `ExternalDeltaPermille` (ERR-033-003), not a #46-callable mutator — the earlier wording invited a direct `MoralePermille` assignment, which would contradict FR-HS-002. All consumers remain **deferred** (FR-LW-031) — no interface built ahead of the producer/consumer. | MUST | KD-3 |
| FR-HS-025 | Morale is a **projection OUT** of #33 — **no two-way coupling** with any consumer (avoids determinism-ordering fragility). | MUST | KD-3 |
| FR-HS-026 | #33 state persists as an opaque, independently version-gated `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` sub-blob composed into #30's `SeasonSaveCodec` — **not** a `WORLD_STORE_FORMAT_VERSION` bump. Fail-loud on version mismatch / out-of-bounds length prefix / trailing bytes (F3/F5). Serialize-don't-regenerate. | MUST | KD-7 |
| FR-HS-027 | **Extended by ERR-033-002 (at #35's approval): a routed input's pending source-side value is dropped with the player's entries** — otherwise an undelivered external delta outlives its subject and lands on whoever next holds that `PlayerId`. Roster-membership lifecycle is in lockstep with #28's season-boundary churn: a regen inserts neutral `MoraleState.Create()`/`PersonalityProfile.Create()` for the fresh `PlayerId` and drops it from prior teammates' pairwise sets; a retirement removes the retiree's per-player + pairwise entries. Keyed by `PlayerId`, applied by the roster owner (#30). | MUST | KD-7 |
| FR-HS-028 | #33 **never** references #30 or `living-world`; the reference DAG (`root → {#30, #22, #33}`, `#33 → {#27, #16}`) is **acyclic**. | MUST | KD-1/KD-7 |

## 2.2 Data structures

```csharp
// #33-owned per-player world-tick state (serialized, KD-7). Integer per-mille — no float internally.
public struct MoraleState
{
    public int MoralePermille;          // happiness [0,1000]; MORALE_NEUTRAL_PERMILLE (500) = content
    public int EquilibriumPermille;     // INTERNAL projection set-point [0,1000]. NOT routed to #22 (FR-HS-007).
    public uint LastAdvancedWorldDay;   // idempotency cursor; HS_NOT_ADVANCED_SENTINEL = uint.MaxValue (F6)
    public static MoraleState Create() => new() {
        MoralePermille = MORALE_NEUTRAL_PERMILLE, EquilibriumPermille = MORALE_NEUTRAL_PERMILLE,
        LastAdvancedWorldDay = HS_NOT_ADVANCED_SENTINEL };   // never default()
}

// Stable personality traits (neutral-seeded at minimal; variety is a deep-tier generation draw). byte[1,20]
// on the #27 posture; NOT appended to #27's PlayerRecord at minimal (a recorded deep-tier option, KD-2).
public struct PersonalityProfile
{
    public byte Professionalism, Ambition, Loyalty, Temperament, Determination;   // each [1,20]
    public static PersonalityProfile Create() => new() {
        Professionalism = TRAIT_NEUTRAL, Ambition = TRAIT_NEUTRAL, Loyalty = TRAIT_NEUTRAL,
        Temperament = TRAIT_NEUTRAL, Determination = TRAIT_NEUTRAL };   // never default()
}

// The authoritative vol-2 §2.1 pairwise edge #22 mirrors as PlayerEdge (club-scoped at minimal). Integer
// per-mille; the float [0,1] appears only at the #22 route boundary. Cliques DERIVE from this (KD-4).
public struct PairwiseRelationship { public int FromPlayerId, ToPlayerId, StrengthPermille; }   // [0,1000]

// KD-1 committed read-only view the SeasonSave root routes into #22 phase-2 — ONLY the pairwise scalar,
// as PRIMITIVE ARRAYS (no cross-assembly type). Pure read; #22 mirrors via SetPlayerEdgeMirror, never calls back.
public readonly struct HumanSystemsView   // fromPlayerId[]/toPlayerId[]/edge[0,1][]  (edge = StrengthPermille/1000f)
{ /* value-copy accessors only */ }

// KD-5 mentoring — empty identity at minimal; #34 staff-driven pairing is the deep-tier routing seam.
public readonly struct MentoringPlan { public static MentoringPlan None => default; }

// Committed-values input to AdvanceHumanSystemsDay (§3.1) — the day's true results #30 routes in as VALUES
// (no #30 / match-engine type reference; provenance enforced at the #30 call seam, §4.4). All integer.
public readonly struct HumanSystemsDayInput
{
    public readonly MatchDayResult Result;   // None | Win | Draw | Loss for this player's club that day (enum)
    public readonly int MinutesPlayed;       // [0, 120] — appearance signal (0 = did not feature)
    public readonly int BoardObjectiveDeltaPermille;   // [-1000,1000] committed board-state nudge (0 = neutral)
    // ERR-033-003 (filed JOINTLY by #35 and #46 at their approval). PRODUCER-AGNOSTIC, deliberately:
    // the supplement proposed a per-producer `MediaDeltaPermille`, which does not survive a SECOND
    // producer of the same quantity -- #46 would have needed a third field on an approved struct, and
    // producer N a further one. The ROOT sums every producer's contribution and CLAMPS before it
    // reaches #33, so #33 sees one already-bounded term and never learns who produced it.
    public readonly int ExternalDeltaPermille;         // [-1000,1000] summed+clamped by the root (0 = neutral)
    public static HumanSystemsDayInput Neutral =>      // a non-match day: no result, no minutes, no deltas
        new(MatchDayResult.None, 0, 0, 0);
}
public enum MatchDayResult : byte { None = 0, Win, Draw, Loss }   // None = no fixture that day (default)
```

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| F1 | An out-of-range value reaching a consuming seam — morale/equilibrium outside `[0,1000]`, a trait outside `[1,20]`, a relationship strength outside `[0,1000]`. | Fail loud (`ArgumentOutOfRangeException`). |
| F2 | An out-of-contract byte on restore (e.g. a negative per-mille, a trait `0`/`>20`). | Fail loud at deserialize (the `MatchSaveCodec` posture). |
| F3 | Bad `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` on restore. | Fail loud (version-gate). |
| F4 | `default(PersonalityProfile)` (all traits `0 ∉ [1,20]` — the zero-value trap) reaching a consuming seam; a default-constructed per-player record is caught here (its paired `MoraleState` default is field-in-contract and never used unpaired — FR-HS-005). | Fail loud (the #40 `BoardModifier` F4 precedent). |
| F5 | Out-of-bounds length prefix / trailing bytes in the sub-blob. | Fail loud (overflow-safe `ReadCount`). |
| F6 | Re-advancing the same `worldDay` for a player is a **no-op**; a `worldDay` **gap** (> 1 day since `LastAdvancedWorldDay`, when not the sentinel) fails loud. | No-op / fail loud. |
| F7 | A pairwise `PlayerId` outside the club universe (#27's `Squad` enumeration) reaching a consuming seam. | Fail loud (the #27/#40 club-universe check). |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §2 (FR-HS-001..028, data structures, F1..F7). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (M): FR-HS-005/F4 fail-loud scoped to `PersonalityProfile` (MoraleState default is field-in-contract); added `HumanSystemsDayInput`/`MatchDayResult` structs. |
| 0.3 | 2026-07-23 | — | AR-2 (L): FR-HS-020 states the mutuality rule; FR-HS-005 clarifies the enforced guard is at record insertion (the F6 no-op path caveat). |
| 0.4 | 2026-07-27 | — | **ERR-033-003** (filed **jointly** by #35 and #46): `HumanSystemsDayInput` gains a **producer-agnostic** `ExternalDeltaPermille`, **summed across producers and clamped by the root**. Supersedes the supplement's per-producer `MediaDeltaPermille`, which would have needed a third field the moment a second producer arrived. **Transient struct — no `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` bump.** **ERR-033-004** (at #46's approval): FR-HS-024 corrected — **no consumer writes #33 morale, #46 included**; its seam *is* the routed delta. **ERR-033-002** (at #35's approval): FR-HS-027 extended so a pending routed delta is dropped with the player's entries. |
#endregion
