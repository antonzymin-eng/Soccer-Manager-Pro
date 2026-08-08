# Injuries & Medical #41 — Outline

**Created:** July 23, 2026
**Last Updated:** August 8, 2026 (v0.3 — balance-pass AR pass 10 L3: the §2 row's failure-mode range F1..F7 → F1..F8, stale since pass 9 added F8)
**Last Updated (prior):** August 8, 2026 (v0.2 — balance-pass AR pass 7 M1: the KD-1 summary mirrored from §1's corrected text — it still carried the phantom stream)
**Last Updated (prior):** July 23, 2026 (v0.1 — initial authoring from the converged design supplement)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/injuries-medical-design.md` v0.2
**FR prefix:** FR-MD · **Wave:** 2 · **Master-plan home:** §4.2

---

## Purpose

Injury **occurrence** (draw + trigger), **severity** classification, and a **recovery** timeline that
advances on the **world tick** (`WorldClock`, one day = one `worldTick` — never the 10 Hz tactical / 60 Hz
physics match loops), modulated by future physio/medical staff (#34). Split out of #29 Training System so
injury is a system in its own right rather than a training side-effect. Fully deterministic in structure —
its one stochastic surface is a **single, position-independent, keyed** world-tick draw; there is no
free-running cursor and nothing to persist beyond the injury state itself.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope, dependencies, key decisions KD-1..KD-8 |
| 2 | Functional requirements FR-MD-001..027, data structures, failure modes F1..F8 |
| 3 | Algorithms — `AdvanceMedicalDay` (recovery countdown then keyed occurrence draw), severity classification, the risk-score assembly, a worked example |
| 4 | Architecture, assembly, file layout, reference direction |
| 5 | Test plan (T-MD-*) + FR traceability |
| 6 | Performance / off-pitch world-tick cadence |
| 7 | Future extensions, T-phase plan T0–T3, the #34/#27 deferred seams |
| 8 | References |
| 9 | Approval checklist |
| Appendices | Constant catalogue + worked examples |

## Key decisions (summary; full text in §1)

- **KD-1** Single-clock, position-independent occurrence: one keyed world-tick derivation off the world
  seed (no registered stream — ERR-041-012); every draw is **keyed** on `(worldSeed, playerId, worldDay,
  purpose)` — no free-running cursor, nothing to persist. The match tick never draws for #41.
- **KD-2** Fatigue reconciliation is read-only: #41 reads #29's already-published `InjuryRiskContribution`
  as one occurrence input; #41 never reads or mutates either fatigue accumulator (#29's training-fatigue,
  the match engine's `AerobicPool`).
- **KD-3** Match-incident coupling is a read-only ledger derivation (deep tier only) — no new match-engine
  producer, no #41 interface in the match engine. Stage-2 uses `MatchLoad.AppearanceDays` only.
- **KD-4** Severity/recovery model is one code path: Stage-2 fixed severity tiers (Minor/Moderate/Serious)
  with fixed recovery-days + linear countdown; Stage-3 adds a distribution-driven severity + recurrence,
  defaulting to the Stage-2 identity via a config dial.
- **KD-5** Staff modulation is an identity routing seam (`MedicalModifier.Identity`) until #34 lands.
- **KD-6** #30 tick-order integration is a back-prop (ERR-030-002), inserting a new step **after**
  #28/#29/#33 and **before** `WorldStore.AdvanceDay()`. Recovery countdown precedes the occurrence draw
  within the step.
- **KD-7** Persistence is an opaque `MEDICAL_SAVE_FORMAT_VERSION` sub-blob under #30's season save — **not**
  `WORLD_STORE_FORMAT_VERSION`. No RNG cursor is serialized (KD-1 makes this unnecessary).
- **KD-8** Behaviour-neutral identity: stream independence, an `occurrenceEnabled` dial off ⇒ recovery-only
  no-op, `InjuryState` defaults to `Create()` = Healthy.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial outline from the converged design supplement. Status IN REVIEW. |
| 0.2 | 2026-08-08 | — | **Balance-pass AR pass 7 (M1)**: the KD-1 summary still read "one `injuries.occurrence` world-tick stream" — the exact wording §1's KD-1 was corrected from at pass 6; mirrored. |
| 0.3 | 2026-08-08 | — | **Balance-pass AR pass 10 (L3)**: the §2 row's failure-mode range went stale the moment pass 9 added F8; corrected F1..F7 → F1..F8. |
#endregion
