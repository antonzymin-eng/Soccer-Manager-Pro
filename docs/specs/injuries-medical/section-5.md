# Injuries & Medical #41 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** August 8, 2026, second final entry (v0.6 — AR pass 16 L2: T-MD-MOD-002 covers both clamp arms)
**Last Updated (prior):** August 8, 2026 (v0.5 — balance-pass AR pass 10 L4: T-MD-DET-010 names the existing F8 sentinel lock)
**Last Updated (prior):** August 8, 2026 (v0.4 — balance-pass AR pass 6 M4: the ERR-041-012 sweep — T-MD-DET-004 / T-MD-NEU-003 / T-MD-SEV-001 restated off the phantom registered stream and its service reservation. Prior header below.)
**Last Updated (prior):** July 23, 2026 (v0.3 — AR-2 fixed-radix append-parity; prior v0.2 AR-1 integer fix, v0.1 initial)
**Version:** 0.6
**Status:** APPROVED

---

Tests land at T-phase; this is the acceptance contract.

## 5.1 Determinism & save/restore

- **T-MD-DET-001** — Save→restore across a **mid-recovery** boundary: each player's `InjuryState`
  (`Severity`, `RecoveryRemaining`, `InjuryCount`, `LastAdvancedWorldDay`) restores **field-identical**;
  advancing N more days after restore equals an uninterrupted run (two-run digest match from one seed).
- **T-MD-DET-002** — Save→restore across a **post-fixture-draw** boundary (a save taken immediately after
  a world-tick occurrence draw resolves): resumes byte-identically with nothing to continue (the KD-1
  keyed-draw property — no cursor to restore).
- **T-MD-DET-003** — **Position-independent draw lock (the KD-1 lock):** the same `(playerId, worldDay,
  purpose)` reproduces the same occurrence outcome regardless of the order other players/days are drawn in
  a season — asserted by drawing the same triple via two different overall roster/day iteration orders and
  comparing outcomes.
- **T-MD-DET-004** — No free-running cursor: the serialized medical block carries no
  `RngCursor`/`actionOrdinal` field (schema-shape assertion — there is no registered stream to hold one;
  ERR-041-012) — FR-MD-007.
- **T-MD-DET-005** — Idempotency: `AdvanceMedicalDay` for an already-advanced `worldDay` is a no-op
  (`LastAdvancedWorldDay` unchanged, `RecoveryRemaining`/`Severity`/`InjuryCount` unchanged) — F6.
- **T-MD-DET-006** — **Day-0 boundary:** a state from `InjuryState.Create` (sentinel `LastAdvancedWorldDay =
  uint.MaxValue`) advances **once** on world-day 0, and a re-run of day 0 (after save→restore) is a no-op —
  the sentinel does not collide with a legitimate day 0 (F6).
- **T-MD-DET-007** — **Day gap fails loud:** `AdvanceMedicalDay(worldDay = last + 2)` throws
  `ArgumentException` (F7).
- **T-MD-DET-009** — **Fixed-radix append parity:** `DeriveActionOrdinal(worldDay, Occurrence)` yields the
  identical `u64` (`worldDay × DRAW_PURPOSE_RADIX`) whether `DRAW_PURPOSE_RADIX`-worth of purposes are
  defined or only one — i.e. adding a future purpose ordinal does **not** shift the occurrence key
  (FR-MD-008); a `purpose >= DRAW_PURPOSE_RADIX` fails the bound guard (§3.1.1).
- **T-MD-DET-010** — **Sentinel-as-worldDay fails loud (F8):** `AdvanceMedicalDay(worldDay =
  MEDICAL_NOT_ADVANCED_SENTINEL)` throws `ArgumentException` — locked by
  `MedicalStepTests.AdvancingTheSentinelDay_FailsLoud` (the lock predates its F8 row; id assigned at the
  balance-pass AR pass 10, L4).
- **T-MD-DET-008** — **No match-tick draw path exists structurally** — #41's assembly references nothing in
  `MatchEngine`, and the match assembly references nothing in #41 (asmdef-shape assertion) — KD-1/KD-3.

## 5.2 Roster-membership lifecycle (FR-MD-025)

- **T-MD-LIFE-001** — A #28 `RegenResult` inserts an `InjuryState.Create()` for each fresh `PlayerId`
  (advances correctly on its first world day — never the `default`/day-0 trap); a `RetirementResult` removes
  the retiree's entry, so the per-club `InjuryState` count equals the roster count across a season roll (no
  leak).

## 5.3 Behaviour-neutral identity (KD-8)

- **T-MD-NEU-001** — With `occurrenceEnabled` off, `AdvanceMedicalDay` never fires an occurrence draw for
  any input (risk score, `MatchLoad`, attributes) — it reduces to the recovery countdown only — FR-MD-027.
- **T-MD-NEU-002** — `InjuryState.Create()` yields `Severity = None`, `RecoveryRemaining = 0`, `InjuryCount
  = 0` — the Healthy identity.
- **T-MD-NEU-003** — Stream independence, vacuous by construction since ERR-041-012 (#41 registers
  nothing): every pre-existing stream's cursor (`world.text`, `player-progression.regen`,
  `season-loop.season-events`, match-tick streams) is byte-identical across a full world-tick season run
  with and without #41 active.

## 5.4 Ordering & recovery/occurrence interaction (KD-6)

- **T-MD-ORD-001** — Recovery-to-zero and re-injury cannot both fire in one `AdvanceMedicalDay` call: a
  player entering the call with `RecoveryRemaining = 1` who recovers this call is **not** eligible for a new
  occurrence draw this same call, even under a risk score/draw combination that would otherwise trigger one
  (constructed via a stubbed `rng` returning a value that WOULD satisfy `draw < risk`) — FR-MD-004.
- **T-MD-ORD-002** — The injuries slot reads the **day's updated** training-fatigue / condition — i.e. the
  risk-score assembly sees the value #29's slot-2 step produced for the same world day, not the prior day's
  value (an integration-level ordering lock at #30's composed loop) — KD-6.
- **T-MD-ORD-003** — The injuries step is invoked strictly after #28/#29/#33's documented seams and strictly
  before `WorldStore.AdvanceDay()` — a structural/ordering assertion against #30's tick-order sequence
  (post-ERR-030-002) — FR-MD-022.

## 5.5 Fatigue-input read-only lock (KD-2)

- **T-MD-FAT-001** — #41 reads #29's `InjuryRiskContribution` and never writes any fatigue accumulator — no
  code path from #41 into `TrainingFatigue` or `AerobicPool` exists (asmdef-shape assertion: no `MatchEngine`
  reference; #29's `TrainingState` internals are not exposed to #41) — FR-MD-009.

## 5.6 Severity & recovery

- **T-MD-SEV-001** — Severity bucketing consumes the **same** draw as the occurrence check — no second
  keyed evaluation is issued for a Stage-2 occurrence (`ClassifySeverityFromDraw` takes the draw VALUE;
  there is no service reservation to count against — ERR-041-012) — FR-MD-012.
- **T-MD-SEV-002** — Bucketing boundaries via the integer cross-multiply (§3.2): a draw exactly at the
  Minor boundary (`draw × SEVERITY_PERMILLE_DENOM == risk × SEVERITY_MINOR_PERMILLE`) classifies **Moderate**
  (the `<` convention, not `<=`), mirroring the project's boundary-classification precedent; no float
  division is used (FR-MD-014).
- **T-MD-REC-001** — `RecoveryRemaining` clamps at `[0, RECOVERY_MAX]` (F1); recovery under
  `MedicalModifier.Identity` consumes exactly `RECOVERY_DAYS_PER_TICK_BASE` per day.
- **T-MD-REC-002** — `RobustnessMitigation` is deterministic over own attributes (identical inputs →
  identical mitigation) — FR-MD-015.

## 5.7 Seams & fail-loud

- **T-MD-MOD-002** — Recovery-speed is applied to **assigned tier-days at injury time**, not per-tick: a
  `RecoverySpeedMillMult > 1000` shortens total recovery (fewer assigned days) while the per-day decrement
  stays the fixed integer `RECOVERY_DAYS_PER_TICK_BASE`; and an aggressive multiplier that would divide the
  assigned days below 1 is **floored at 1** so a confirmed injury never has `RecoveryRemaining == 0` while
  `Severity != None` (the F1 coherence floor, §3.1), **and a slow multiplier that would push the assigned
  days past `RECOVERY_MAX` is ceilinged there** (the field's declared range; AR pass 15 M2 / pass 16 L2 —
  both arms locked) — FR-MD-014.
- **T-MD-FAIL-006** — A `MedicalModifier` with `RecoverySpeedMillMult == 0` (e.g. `default(MedicalModifier)`)
  reaching the consuming seam → **fail loud** (divide-by-zero / ×0 risk; the zero-value-trap gate) — FR-MD-016 / F4.
- **T-MD-MOD-001** — `MedicalModifier.Identity` yields the exact Stage-2 risk score and recovery pace
  (×1.0 on both) — KD-5.
- **T-MD-AVAIL-001** — `IsAvailable` returns `false` for every non-`None` severity and `true` for `None`;
  never mutates the passed `InjuryState` — FR-MD-023.
- **T-MD-FAIL-001** — Bad `MEDICAL_SAVE_FORMAT_VERSION` → fail loud (F3).
- **T-MD-FAIL-002** — Out-of-bounds length prefix / trailing bytes → fail loud (F5).
- **T-MD-FAIL-003** — An out-of-contract `InjurySeverity` byte on restore → fail loud (F4).
- **T-MD-FAIL-004** — An `InjuryState` with `RecoveryRemaining > 0` and `Severity == None` (or the reverse
  coherence violation) reaching a consuming seam → fail loud (F1).
- **T-MD-FAIL-005** — `AdvanceMedicalDay` called for a `playerId` with no `InjuryState` → fail loud (F2).

## 5.8 FR traceability

| FR | Covering test(s) |
|---|---|
| FR-MD-001 | T-MD-DET-008 |
| FR-MD-002 | T-MD-DET-001, T-MD-LIFE-001 |
| FR-MD-003 | T-MD-AVAIL-001 |
| FR-MD-004 | T-MD-ORD-001 |
| FR-MD-005 | T-MD-DET-008 |
| FR-MD-006 | T-MD-DET-003 |
| FR-MD-007 | T-MD-DET-004 |
| FR-MD-008 | T-MD-DET-009, T-MD-SEV-001 |
| FR-MD-009 | T-MD-FAT-001 |
| FR-MD-010 | T-MD-ORD-002 |
| FR-MD-011 | T-MD-DET-008 |
| FR-MD-012 | T-MD-SEV-001, T-MD-SEV-002 |
| FR-MD-013 | T-MD-NEU-001 |
| FR-MD-014 | T-MD-REC-001, T-MD-MOD-002 |
| FR-MD-015 | T-MD-REC-002 |
| FR-MD-016 | T-MD-MOD-001, T-MD-FAIL-006 |
| FR-MD-017 | T-MD-FAIL-001 |
| FR-MD-018 | T-MD-DET-001 |
| FR-MD-019 | T-MD-FAIL-001, T-MD-FAIL-002 |
| FR-MD-020 | T-MD-DET-005, T-MD-DET-006 |
| FR-MD-021 | T-MD-DET-007 |
| FR-MD-022 | T-MD-ORD-003 |
| FR-MD-023 | T-MD-AVAIL-001 |
| FR-MD-024 | T-MD-NEU-002 (`MedicalViewModel` shape locked alongside the identity state) |
| FR-MD-025 | T-MD-LIFE-001 |
| FR-MD-026 | T-MD-FAT-001, T-MD-DET-008 |
| FR-MD-027 | T-MD-NEU-001, T-MD-NEU-002, T-MD-NEU-003 |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial test plan (T-MD-*) + full FR-MD-001..027 traceability table. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): +T-MD-MOD-002 (recovery-speed at assignment + floor-at-1) / +T-MD-FAIL-006 (zero `MedicalModifier` fails loud); T-MD-SEV-002 restated as the integer cross-multiply; traceability FR-MD-014/016 updated. |
| 0.3 | 2026-07-23 | — | AR-2 (1M): +T-MD-DET-009 (fixed-radix append parity + bound guard); FR-MD-008 traceability; fixed a `FR-MD-007` typo. |
| 0.4 | 2026-08-08 | — | **Balance-pass AR pass 6 (M4)**: three test descriptions still asserted against the registered `injuries.occurrence` stream / `DeterministicRngService` reservation that ERR-041-012 established never existed; restated against the keyed derivation the suites actually exercise. |
| 0.5 | 2026-08-08 | — | **Balance-pass AR pass 10 (L4)**: **T-MD-DET-010** — the F8 sentinel-as-worldDay refusal (pass 9) gets its §5 id, naming the `AdvancingTheSentinelDay_FailsLoud` lock that already executes it. |
| 0.6 | 2026-08-08 | — | **Balance-pass AR pass 16 (L2)**: T-MD-MOD-002 covered only the floor arm while pass 15 M2 made the ceiling normative — and a mutant erasing the ceiling left the whole suite green; both arms now stated and locked. |
#endregion
