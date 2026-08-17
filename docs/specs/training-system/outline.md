# Training System #29 — Outline

**Created:** July 23, 2026
**Last Updated:** August 8, 2026 (v0.3 — balance-pass AR pass 10 L3: the §2 row's ranges were two landings stale; corrected to FR-TR-001..026 / F1..F8)
**Last Updated (prior):** July 23, 2026 (v0.2 — PASS-1 → AR-2 → AR-3; APPROVED)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/training-system-design.md` v0.4
**FR prefix:** FR-TR · **Wave:** 2 · **Master-plan home:** §4.4

---

## Purpose

Weekly-directed, **daily-accrued** training on the **world tick**: a per-player training **focus** drives a
deterministic **conditioning** cursor and a **training-fatigue** accumulator, and (deep tier) a granular
per-attribute growth contribution that is an **input to #28's CA/PA curve** — never a parallel attribute
write. Fully deterministic; no RNG stream.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope, dependencies, key decisions KD-1..KD-8 |
| 2 | Functional requirements FR-TR-001..026, data structures, failure modes F1..F8 |
| 3 | Algorithms — the daily step, the fatigue projection, the growth-input contribution |
| 4 | Architecture, assembly, file layout, reference direction |
| 5 | Test plan (T-TR-*) |
| 6 | Performance / off-pitch world-tick cadence |
| 7 | Future extensions, T-phase plan T0–T3, the #34/#41 seam contracts |
| 8 | References |
| 9 | Approval checklist |
| Appendices | Constant catalogue + worked examples |

## Key decisions (summary; full text in §1)

- **KD-1** World-tick training-fatigue ↔ match-tick fatigue reconciliation: a single accumulator + a pure
  one-directional projection into the match-boot caller-supplied starting fatigue; no shared counter, no
  write-back.
- **KD-2** Attribute growth is single-owned by #28 — #29 only populates #28's `TrainingInput`; a **pure**
  `ComputeTrainingInput` read feeds #28 at #30's slot-1 seam with no staleness / no reorder.
- **KD-3** #34 coaching modulation is an identity routing seam (`CoachingModifier.Identity`) until #34 lands.
- **KD-4** Daily accrual, weekly focus cadence — no batch boundary, no rollover step.
- **KD-5** Injury-risk **output** shaped for #41 to read; #29 owns no injury model; no #41 interface built.
- **KD-6** #29 is fully deterministic — **no RNG stream**; `_RESERVED_0x21_` / `SubsystemOrdinals` 83 stay
  reserved (not promoted).
- **KD-7** Opaque `TRAINING_SAVE_FORMAT_VERSION` sub-blob under #30's season save; fail-loud; serialize-
  don't-regenerate.
- **KD-8** Behaviour-neutral identity: dial off + `CoachingModifier.Identity` + `TrainingInput.Neutral` ⇒
  #29 never touches #28's attributes/CA/PA.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial outline from the converged design supplement. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | PASS-1 → AR-2 → AR-3 convergence; APPROVED. |
| 0.3 | 2026-08-08 | — | **Balance-pass AR pass 10 (L3)**: the §2 row was TWO rows behind — stale since F7/FR-TR-025/026 landed at §2 v0.3 (July 2026), and pass 9's F8 made it three; corrected to FR-TR-001..026 / F1..F8. |
#endregion
