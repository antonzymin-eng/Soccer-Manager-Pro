# Training System #29 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

Tests land at T-phase; this is the acceptance contract.

## 5.1 Determinism & save/restore

- **T-TR-DET-001** — Save→restore across a **mid-week** boundary: `TrainingState` (focus, `Condition`,
  `TrainingFatigue`, `LastAdvancedWorldDay`) + `TrainingSchedule` restore **field-identical**; advancing N
  more days after restore equals an uninterrupted run (two-run digest match from one seed).
- **T-TR-DET-002** — No RNG: two runs from the same seed produce identical state with **no** stream
  registered for #29 (`_RESERVED_0x21_` absent from the registered-stream set) — KD-6.
- **T-TR-DET-003** — Idempotency: `AdvanceTrainingDay` for an already-advanced `worldDay` is a no-op
  (`LastAdvancedWorldDay` unchanged, cursors unchanged) — F6.
- **T-TR-DET-004** — **Day-0 boundary:** a state from `TrainingState.Create` (sentinel `LastAdvancedWorldDay
  = uint.MaxValue`) advances **once** on world-day 0, and a re-run of day 0 (after save→restore) is a no-op
  — the sentinel does not collide with a legitimate day 0 (F6, the PASS-1 M-1 fix).

## 5.2 Behaviour-neutral identity (KD-8)

- **T-TR-NEU-001** — With the attribute-growth dial off, `ComputeTrainingInput` returns exactly
  `TrainingInput.Neutral` for every focus, so #28's `GrowthProjection` output is **byte-identical** to the
  no-training path (attributes/CA/PA unchanged) — FR-TR-007.
- **T-TR-NEU-002** — Under the dial-off configuration, `AdvanceTrainingDay` changes **only** `Condition` /
  `TrainingFatigue` and never any `PlayerAttributes` field (no second attribute writer) — FR-TR-005.

## 5.3 Fatigue reconciliation (KD-1)

- **T-TR-FAT-001** — `ProjectMatchEntryFatigue` recomputed after restore equals its value before restore
  (pure over the serialized accumulator, not stored).
- **T-TR-FAT-002** — Match-tick fatigue never mutates `TrainingFatigue`: there is no code path from
  `AerobicPool`/`MatchEngine` into #29 (asmdef has no `MatchEngine` reference) — FR-TR-013.
- **T-TR-FAT-003** — Monotonic shape: higher `TrainingFatigue` → higher (or equal) projected starting
  fatigue, clamped to `[0,1]`.

## 5.4 Cursors & clamps

- **T-TR-CON-001** — `Condition` clamps at `[CONDITION_MIN, CONDITION_MAX]` (F1); a `Rest` focus lowers
  `TrainingFatigue` (recovery) without underflow.
- **T-TR-CON-002** — `AttributeConditioningBonus` is deterministic over own attributes (identical inputs →
  identical delta) — FR-TR-009.

## 5.5 Seams & fail-loud

- **T-TR-COA-001** — `CoachingModifier.Identity` yields the exact Stage-2 deltas (×1.0) — KD-3.
- **T-TR-INJ-001** — `InjuryRiskContribution` is monotone in `TrainingFatigue` and inverse in `Condition`,
  clamped — KD-5.
- **T-TR-FAIL-001** — Bad `TRAINING_SAVE_FORMAT_VERSION` → fail loud (F3).
- **T-TR-FAIL-002** — Out-of-bounds length prefix / trailing bytes → fail loud (F5).
- **T-TR-FAIL-003** — `SetFocus` with an out-of-range enum or unknown player → refused (F2/F4).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial test plan. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Aligned to the single-cursor / no-RNG model; APPROVED. |
#endregion
