# Injuries & Medical #41 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.1
**Status:** APPROVED

---

## 1.1 Introduction

Injuries & Medical advances each player's injury status on the **world tick** (`WorldClock`, one day = one
`worldTick` — distinct from the 10 Hz tactical / 60 Hz physics match loops). A per-player `InjuryState`
(active-injury severity, recovery-remaining days, cumulative injury count) is countdown-recovered and, once
healthy, evaluated once per world day against a **single, keyed, position-independent** occurrence draw
whose input risk score is assembled from Training System #29's already-published `InjuryRiskContribution`,
recent match participation, and the player's own physical attributes. Split out of #29 so injury is a system
in its own right rather than a training side-effect (the training design's own KD-5 boundary).

## 1.2 Scope

**In scope:** the per-player `InjuryState` (severity, recovery-remaining, cumulative injury count, the
idempotency cursor); the world-tick `AdvanceMedicalDay` step (recovery countdown, THEN occurrence draw);
severity classification (Stage-2 fixed tiers; Stage-3 distribution-driven, deferred); the risk-score
assembly from #29's output + recent match participation + #27 attributes; a read-only availability view for
#30's squad selection; a read-only `MedicalViewModel` observer for #38; the persistent medical sub-blob
under #30's season save; and the single `injuries.occurrence` world-tick RNG stream.

**Out of scope (owned elsewhere, referenced as seams):**
- **The fatigue accumulators themselves** — #29 owns the world-tick training-fatigue accumulator; the match
  engine owns in-match fatigue (`1 − AerobicPool`). #41 reads #29's *output* as an occurrence input (KD-2)
  and never touches either accumulator.
- **Squad-selection consequences** — #30 reads a read-only availability view; #41 owns no selection logic.
- **The medical-staff entity model** — #34 supplies staff quality through the identity `MedicalModifier`
  routing seam (KD-5); #41 builds no #34 interface.
- **Attribute decline from injury** — #28 owns `GrowthProjection` (the sole attribute-mutation path); #41
  exposes a read-only injury signal #28 *may* later read, never a parallel attribute write (KD-2 direction).
- **A dedicated injury-proneness attribute** — #27's `PlayerAttributes` has no such field today; #41 derives
  a robustness term from existing physical attributes at Stage 2. A dedicated `InjuryProneness` #27 append
  is a recorded deep-tier deferral (KD-4), not built here.

## 1.3 Dependencies

| Spec | Relationship | Direction |
|---|---|---|
| #27 Squad/Player Data | reads `PlayerRecord` / `PlayerAttributes` for a physical robustness term | #41 → #27 |
| #29 Training System | reads the already-published `InjuryRiskContribution` output, read-only | #41 → #29 |
| #16 Deterministic Sim | consumes the determinism namespace + the world-tick `DeterministicRngService` | #41 → #16 |
| #30 Season & Competition Loop | #30's day-advance loop invokes `AdvanceMedicalDay` at a new reserved slot; reads the availability view for squad selection | #30 → #41 |
| #34 Medical Staff (future) | supplies a non-identity `MedicalModifier` when it lands | #34 → #41 |
| #38 UI/Client (future) | reads the `MedicalViewModel` observer (value copies) | #38 reads #41 |
| Match engine (read-only, deep tier) | the deep-tier per-fixture physical-load summary derives read-only from the already-emitted event ledger; no new match-engine producer, no #41 interface in the match engine | #41 reads the ledger (no reference) |

Reference DAG: `#30 → {#28, #29, #41}`, `#41 → {#29, #27, #16}`. **Acyclic.** #29's assembly stays
schema-untouched — `InjuryRiskContribution` is #29's own already-published output (FR-TR-017); #41 gains no
interface into #29 beyond reading that value.

## 1.4 Key decisions

- **KD-1 (single-clock, position-independent occurrence — the headline).** All #41 stochastic draws happen
  on the **world tick**, on one dedicated `injuries.occurrence` stream registered on the world-tick
  `DeterministicRngService` (the same service #22's `world.text` and #28's `player-progression.regen`
  register on — seeded from the world seed, sub-streams independent). **The match tick NEVER draws for
  #41.** Each daily occurrence draw is **position-independent / keyed**, not a free-running counter draw: it
  is keyed on `(stream, entityId = playerId, ActionOrdinal derived deterministically from worldDay + a
  draw-purpose ordinal)` — the off-pitch keyed-draw precedent (#28 regen keyed by `entityId = clubId`; #30
  quick-sim keyed on `(seed, seasonNumber, roundIndex, homeClubId, awayClubId)`), **not** the match-tick
  free-running card-severity cursor. Consequence: there is **no free-running cursor to persist** — the same
  `(playerId, worldDay, purpose)` reproduces the same draw regardless of how many other players/days drew
  first, so save→restore is automatically byte-exact with nothing to continue. Draw-purpose ordinals are
  **APPEND-only** (never renumbered), preserving replay parity across fail-loud paths. Match-incident
  injuries do **not** draw on the match tick either — the deep-tier per-fixture physical-load summary is a
  **read-only** ledger derivation (KD-3) fed as a world-tick occurrence *input*, never a match-tick draw
  site; the fixture is played and its result/ledger read on the world-tick day that follows it, before the
  next fixture's squad selection reads availability.

- **KD-2 (fatigue reconciliation — read-only input, no double count).** #41 reads #29's already-published
  `InjuryRiskContribution` (FR-TR-017) read-only as one occurrence input, plus recent match participation
  (`MatchLoad`) and a robustness term derived from #27 attributes. #41 never reads or mutates either fatigue
  accumulator (#29's training-fatigue, the match engine's `AerobicPool`) — #29 owns the accumulator and
  exposes a scalar; #41 consumes the scalar. No counter is shared between the two specs, so no double count
  is representable.

- **KD-3 (match-incident coupling — read-only ledger derivation, no new producer).** The deep-tier
  per-fixture physical-load summary derives **read-only** from the already-emitted event ledger (collisions
  / hard fouls) — the #37 analytics / #44 discipline posture — so **no** new match-engine surface and **no**
  #41 interface in the match engine is built (phantom-free, FR-LW-031). **Stage-2 minimal uses
  `MatchLoad.AppearanceDays` only** (a participation count #30's fixture result already tracks); the
  ledger-derived `HardContacts` summary is the deep-tier extension, one code path via a config dial. This
  keeps the match-tick layer untouched and the occurrence layer single-clock.

- **KD-4 (severity/recovery model shape — one code path).** Stage-2: a **fixed severity-tier** table
  (Minor/Moderate/Serious → a fixed recovery-days constant each), derived from the SAME single occurrence
  draw (bucketed by fixed proportions — no second RNG draw at Stage 2), and a **linear** per-day recovery
  countdown. Stage-3: a distribution-driven severity draw + recurrence risk on early return, defaulting to
  the Stage-2 fixed-tier / no-recurrence behaviour via a config dial (`deepMedicalEnabled`). Injury-proneness
  is a **derived** term from #27 physical attributes at Stage 2; a dedicated `InjuryProneness` #27 attribute
  is a deep-tier #27 append **recorded, not built** (avoids a #27 schema ripple in the minimal tier).

- **KD-5 (staff modulation — identity routing seam).** `AdvanceMedicalDay` takes `in MedicalModifier`,
  default `Identity` (×1.0 on **both** occurrence-risk and recovery-speed). **No #34 interface is built**
  (FR-LW-031); #34 becomes the producer of a non-identity modifier when it lands — the #29 `CoachingModifier`
  pattern.

- **KD-6 (#30 tick-order integration — a back-prop, not a #30 rewrite).** #41 needs a per-day world-tick step
  (recovery countdown + occurrence draw), but #30's KD-2 tick order (§3.3) is a **pinned four-step sequence**
  — spec seams `1. progression (#28)` / `2. training (#29)` / `3. human-systems (#33)`, then the live
  terminal step `4. WorldStore.AdvanceDay()`; only #28/#29/#33 are enumerated as null seams (FR-SN-034,
  authored before #41). **Slot 4 is already the live world-day tick — it is not free.** #41's promotion
  files a **#30 back-prop (ERR-030-002)** inserting an **injuries null seam** as a new step **positioned
  after the #28/#29/#33 spec seams and immediately before `WorldStore.AdvanceDay()`** — i.e. the sequence
  becomes `1 #28 · 2 #29 · 3 #33 · 4 injuries (#41, NEW) · 5 WorldStore.AdvanceDay()`, shifting **only** the
  ordinal of the terminal live tick, never re-pinning a reserved seam's position (the ERR-021-005
  `TeamTactic` append precedent for extending an APPROVED spec's reserved enumeration). **Ordering rationale
  (pinned):** the injuries step runs **after** progression (1) and training (2) so the occurrence draw reads
  the **day's updated** training-fatigue / condition (avoids a one-day-stale risk input), and **before**
  `WorldStore.AdvanceDay()` so it operates on the current `worldDay` before the clock increments (the same
  pre-increment position #28/#29 hold). Recovery countdown precedes the occurrence draw **within** the step
  so a player cannot both recover-to-zero and be re-injured on the same tick from one call — the occurrence
  draw is gated on whether the player was **already healthy at call entry** (before the countdown ran), not
  on the post-countdown state, so a player whose recovery completes this same call remains ineligible for a
  new occurrence until the *next* `AdvanceMedicalDay` call (§3.1).

- **KD-7 (persistence — season-save sub-blob; supersedes an earlier `WORLD_STORE_FORMAT_VERSION` guess).**
  `MEDICAL_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob composed into `SeasonSaveCodec`, **not** a
  `WORLD_STORE_FORMAT_VERSION` bump. Rationale: injury state is a per-`PlayerId` **career-state overlay**
  exactly like #28's lifecycle block (which chose the season-save sub-blob) and #29's training block — a
  self-contained #41 unit, not scattered across the world-store block. Fail-loud gates; serialize-don't-
  regenerate. **Roster-membership lifecycle** in lockstep with #28's season-boundary churn (the FR-PG-011 /
  FR-TR-025 remove/insert parallel): a #28 regen inserts an `InjuryState.Create()` (healthy) for the fresh
  `PlayerId`; a retirement removes the retiree's entry — keyed by `PlayerId`, applied by the roster owner
  (#30).

- **KD-8 (behaviour-neutral identity + stream independence).** #41's addition is neutral in three senses:
  (a) **stream independence** — registering the `injuries.occurrence` sub-stream leaves every existing
  stream's cursor byte-identical (the #22/#26/#29 sub-stream-independence precedent), so a world without
  #41 active is unperturbed; (b) an `occurrenceEnabled` dial off reduces #41 to a recovery-only no-op (no
  draws); (c) `InjuryState` defaults to `Create()` = Healthy. The deep tier extends the fixed-tier / minutes
  / identity-staff surface, never rewrites it.

## 1.5 Determinism posture

One stochastic surface only: the single, keyed, position-independent `injuries.occurrence` world-tick draw
(KD-1). Everything else — the recovery countdown, the risk-score assembly, the severity bucketing derived
from that same draw — is a deterministic integer projection. Save/restore is exact because every
`InjuryState` field is serialized (KD-7) and there is no cursor to restore for the one stream that draws
(the keyed-draw property dissolves the free-running-cursor persistence question entirely).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial. Status IN REVIEW. |
#endregion
