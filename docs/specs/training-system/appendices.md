# Training System #29 — Appendices

**Created:** July 23, 2026
**Last Updated:** August 7, 2026 (v0.4 — ERR-029-007 at the #29/#41 balance pass: the `INJURY_RISK_MAX` row states its post-ERR-041-011 meaning — the shared clamp ceiling and probability cap, no longer #41's draw denominator)
**Last Updated (prior):** July 23, 2026 (v0.3 — PASS-2 re-review; prior APPROVED)
**Version:** 0.4
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Every constant carries exactly one source tag. Magnitudes marked `[GT]` are illustrative pending the
Stage-2/3 balance pass (the #21 G2 precedent); the shapes/directions are the reviewed contract.

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `TRAINING_SAVE_FORMAT_VERSION` | 1 | [FIXED] | The #29 sub-blob version (KD-7). A THIRD-party version under #30's season save; independently gated. |
| `DAYS_PER_WEEK` | 7 | [FIXED] | The focus-selection cadence (informational — the step is daily, KD-4). |
| `CONDITION_MIN` / `CONDITION_MAX` | 0 / 10000 | [GT] | Conditioning cursor bounds (integer). |
| `CONDITION_START` | 7000 | [GT] | New-game / new-player conditioning seed. |
| `TRAINING_FATIGUE_MAX` | 10000 | [GT] | World-tick accumulator ceiling (distinct from match fatigue). |
| `TRAINING_NOT_ADVANCED_SENTINEL` | `uint.MaxValue` | [FIXED] | "Never advanced" seed for `LastAdvancedWorldDay` — chosen so a legitimate world-day 0 cannot collide with the fresh-state value (the day-0 double-accrual trap, F6). |
| `FocusConditionDelta[Focus]` | table | [GT] | Per-focus daily conditioning delta (Rest small +, Fitness large +, Balanced mid). |
| `FocusFatigueDelta[Focus]` | table | [GT] | Per-focus daily training **load** (before recovery). Rest is 0 or small; Fitness/Physical are large. |
| `FATIGUE_DAILY_RECOVERY` | 200 | [GT] | Passive daily fatigue recovery, applied **every** day regardless of focus (§3.1). Net daily fatigue = `FocusFatigueDelta − FATIGUE_DAILY_RECOVERY`, so a non-Rest regime reaches a sub-max equilibrium instead of saturating; Rest (load ≈ 0) nets strongly negative. |
| `MATCH_ENTRY_FATIGUE_SCALE` | 1.0 | [GT] | KD-1 projection scale: training-fatigue fraction → starting-fatigue offset. |
| `AttributeConditioningBonus` weights | table | [GT] | Deterministic own-attribute (e.g. `WorkRate`/`Stamina`) bonus — never RNG (FR-TR-009). |
| `FatigueRiskWeight` / `LowConditionRiskWeight` | table | [GT] | KD-5 injury-risk weights. |
| `INJURY_RISK_MAX` | 10000 | [GT] | Risk-scalar clamp ceiling — **this catalogue is the sole owner; #41 `[CROSS]`-mirrors it** (ERR-041-003) and clamps its assembled occurrence risk to the same ceiling. Since ERR-041-011 it is **not** #41's draw denominator (that is the `[FIXED]` per-million `OCCURRENCE_DRAW_DENOM`); it sets the daily occurrence-probability ceiling (`INJURY_RISK_MAX / OCCURRENCE_DRAW_DENOM`, 1% today) and MUST stay ≤ the denominator (fail-loud at #41's draw site). ERR-029-007. |
| `RobustnessMitigation` weights | table | [GT] | Deterministic own-attribute injury mitigation. |

**No `DOMAIN_TAG_TRAINING` / `SubsystemOrdinals.Training` constant is defined** — #29 registers no stream
(KD-6). `_RESERVED_0x21_` / 83 remain reserved in #16 §3.4.

## Appendix B — Worked example: a Fitness week, mid-week save

Seed: `Condition = 7000`, `TrainingFatigue = 2000`, `Focus = Fitness`, `LastAdvancedWorldDay = 100`.
`FocusConditionDelta[Fitness] = +120`, `FocusFatigueDelta[Fitness] = +300` (load), `FATIGUE_DAILY_RECOVERY =
200`, so net fatigue = `+300 − 200 = +100`/day; `AttributeConditioningBonus = +20`, `CoachingModifier.Identity`.

- Day 101: `Condition = 7000 + 120 + 20 = 7140`; `TrainingFatigue = 2000 + 100 = 2100`; `LastAdvancedWorldDay = 101`.
- Day 102: `Condition = 7280`; `TrainingFatigue = 2200`; day = 102.
- Day 103: `Condition = 7420`; `TrainingFatigue = 2300`; day = 103.
- **Save after day 103**, restore → all four fields restore field-identical; re-running day 104 gives
  `Condition = 7560`, `TrainingFatigue = 2400` — identical to an uninterrupted run (T-TR-DET-001).
- `ProjectMatchEntryFatigue = Clamp01(2300/10000 × 1.0) = 0.23` after day 103 — the same value before and
  after the save (pure over the accumulator; not stored, KD-1).

Re-running day 103 (already advanced) is a no-op (F6); calling day 106 (a gap over the last-advanced 103)
**fails loud** (F7), not a silent skip.

## Appendix C — Worked example: behaviour-neutral identity (KD-8)

`deepTrainingEnabled` off (Stage-2 minimal). For every `TrainingFocus`, `ComputeTrainingInput(state, attrs,
Identity) == TrainingInput.Neutral`. Feeding a `Neutral` batch into #28's `AdvanceDay(worldDay,
trainingInputs)` yields the exact no-training growth step — attributes/CA/PA byte-identical (T-TR-NEU-001).
`AdvanceTrainingDay` changes only `Condition` / `TrainingFatigue`, never a `PlayerAttributes` field
(T-TR-NEU-002) — #29 is not a second attribute writer.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial constant catalogue + worked examples. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | APPROVED. |
| 0.3 | 2026-07-23 | — | PASS-2: +`FATIGUE_DAILY_RECOVERY` constant; App. B recomputed (net +100/day, projection 0.23) + F7 gap note; App. C `deepTrainingEnabled` + batch `AdvanceDay`; schedule-not-persisted note. |
| 0.4 | 2026-08-07 | — | **ERR-029-007 (the balance pass)**: `INJURY_RISK_MAX` row updated — sole owner of the shared clamp ceiling, `[CROSS]`-mirrored by #41 (ERR-041-003), no longer the draw denominator (ERR-041-011 pinned that `[FIXED]` at 1,000,000); now the 1%/day probability cap with the ≤-denominator invariant. Value unchanged. |
#endregion
