# Staff & Backroom #34 — Section 8: References & Cross-References

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 8.1 Cross-spec cross-references (XC-034-*)

| ID | Direction | Target | Contract |
|---|---|---|---|
| XC-034-001 | #34 → #41 | `MedicalModifier { int OccurrenceRiskMillMult, RecoverySpeedMillMult; static Identity => new(1000,1000); }`; `AdvanceMedicalDay`/`ComputeInjuryRisk` take `in MedicalModifier` (FR-MD-016) | #34 produces the modifier #41 reads; neutral ⇒ `Identity`; no second injury path (KD-3). |
| XC-034-002 | #34 → #29 | `CoachingModifier { static Identity => default; }`; `AdvanceTrainingDay`/`ComputeTrainingInput` take `in CoachingModifier` (FR-TR-016) | #34 produces the modifier #29 reads; scaffold ⇒ `Identity`; the field shape + consumption is a deep back-prop (ERR-029-002). |
| XC-034-003 | #34 → #40 | `FinanceLineItem.StaffWage`; `ApplyTransaction(ref ClubFinances, in FinanceTransaction)`; `WageBudget` / `WageBillAggregate` (read); FR-FN-015 / FR-FN-016 | staff-wage commit + affordability read (deep, KD-6). #40 §7 already names #34 the `StaffWage` caller. Scaffold posts nothing (FR-FN-015 preserved). |
| XC-034-004 | #34 → #33 (deep) | `MentoringPlan` (identity `None`, FR-HS-022 — #34 override producer); `MoraleOf` / `PersonalityProfile` (read-only) | deep mentoring/judgement modulation (KD-3); #33 §7.3 names #34 the producer; #34 never writes #33 state. |
| XC-034-005 | #34 → #31 (deep) | `NegotiationOutcome { Rejected=0, Accepted=1, CounterOffered=2 }` (reused); `TRANSFERS_STAFF_MULT_IDENTITY = 1000` (#34 is the `staffMult` producer, FR-TX-011) | hiring reuses the enum + pattern (not the `Offer` struct — KD-1); #34 produces #31's deferred `staffMult`. |
| XC-034-006 | #34 → #30 (via composition root) | `RunWorldTickInFixedOrder` slot (new, ERR-030-006); `SeasonSaveCodec`/`SEASON_SAVE_FORMAT_VERSION` (compose) | #30 invokes #34 + owns the save root (KD-4/KD-8). **No roster-commit** (KD-7). #34 never references #30. |
| XC-034-007 | #34 → #16 | determinism namespace; `_RESERVED_0x26_` / `SubsystemOrdinals.Staff = 88` (RESERVED); world-tick `DeterministicRngService` (deep only) | draw-free scaffold (KD-4); promotes at the deep-tier first draw (candidate-pool generation). |
| XC-034-008 | #34 → #32 / #42 (deferred, producer side) | the scout-quality projection `ToScoutQuality` / the coaching projection | #34 publishes the reusable staff-quality seam; #32 (scouting) / #42 (academy) consume it; #34 builds no interface for them (FR-LW-031). |
| XC-034-009 | #34 ↔ #28 (indirect) | #28 `TrainingInput` (#29-owned); #28 is schema-untouched | coaching reaches development only via #29 (`CoachingModifier → TrainingInput → #28`), never a direct #34→#28 seam (#28 §9 R-03). |

## 8.2 Determinism references

- `_RESERVED_0x26_` / `0x26` / [FIXED] — the #16 §3.4 placeholder row (`deterministic-sim/section-3.md:270`),
  held for #34, `SubsystemOrdinals.Staff = 88`. **Stays RESERVED at #34 approval** (draw-free scaffold, KD-4)
  — the #40 `_RESERVED_0x29_` (ERR-040-001) / #31 `_RESERVED_0x23_` / #29 `_RESERVED_0x21_` precedent.
  Promotes to `DOMAIN_TAG_STAFF = 0x26` at #34 T3's first candidate-pool draw.

## 8.3 Back-prop references

- **ERR-030-006 (proposed, at #34 approval)** — #30 §3.3 `RunWorldTickInFixedOrder` gains the staff
  tick-order null-seam slot (the ERR-030-002 #41 / ERR-030-004 #31 precedent; a new **insertion**, since
  FR-SN-034 enumerates #28/#29/#33/#41/#31 only, not #34). Doc-only; the seam is empty until #34 T2/T3.
  **ERR-030-005 is soft-reserved by #31** (its deferred `RequestRosterCommit` build), so #34 takes **006**.
- **ERR-029-002 (deferred, at #34 T3)** — #29 `CoachingModifier` gains its per-mille field shape +
  `AdvanceTrainingDay`/`ComputeTrainingInput` consumption of it, when #34 produces a non-identity coaching
  modifier.
- **ERR-040 (deferred, at #34 T3, shared with #31)** — relax #40 FR-FN-015 (`WageBillAggregate ≡ 0` at Stage
  2) for the wage producers + wire the `WageBudget` affordability gate; the two wage producers (`PlayerWage`
  #31, `StaffWage` #34) arrive together. **Not needed at approval** — the scaffold is fee-… wage-free
  (FR-ST-016).
- **ERR-016 (deferred, at #34 T3)** — `DOMAIN_TAG_STAFF = 0x26` promotion at the first candidate-pool draw.

## 8.4 Master-plan & literature anchors

- Master development plan §5 Stage 3 (staff/backroom) — the staging source. No external academic citation is
  load-bearing for the deterministic staff-quality projections (a game-design tuning surface, not an empirical
  model — the #40/#41 posture). Any deep-tier projection-calibration references are recorded at the balance
  pass, not here.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §8 (XC-034-001..009, determinism reference, back-prop references, master-plan anchor), promoted from design supplement v0.4. Status IN REVIEW. |
#endregion
