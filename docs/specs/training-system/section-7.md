# Training System #29 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.TrainingSystem` assembly: value types (`TrainingFocus`, `TrainingState`,
  `TrainingSchedule`, `CoachingModifier`, `InjuryRiskContribution`, `TrainingViewModel`), the deterministic
  Stage-2 `AdvanceTrainingDay` + `ComputeTrainingInput` (dial off → `TrainingInput.Neutral`) +
  `ProjectMatchEntryFatigue`, `TrainingSystemConstants`. Behaviour-neutral vs #28 by construction (KD-8).
- **T1** — `TrainingSaveCodec` (`TRAINING_SAVE_FORMAT_VERSION` = 1) + composition into #30's season save
  (the `SeasonSaveCodec` sub-blob; #30's composing format-version bump coordinated here). Fail-loud gates.
- **T2** — Wire `ComputeTrainingInput` at #30's **slot-1** progression seam (feeding #28) and
  `AdvanceTrainingDay` at the **slot-2** training seam; wire `ProjectMatchEntryFatigue` into the match-boot
  fatigue seam. No #30 tick-order change (KD-2).
- **T3** — Deep tier: the per-attribute growth contribution (deterministic `BuildTrainingInput`) populating
  #28's `TrainingInput` fields; consume a non-identity `CoachingModifier` when #34 lands.

## 7.2 Deferred (recorded, not built)

- **Match-participation sharpness / morale ("form").** A match-minutes/morale-driven concept owned by its
  future spec — #29 stays single-owner of training-driven conditioning only (§1.2).
- **Conditioning affecting match entry beyond the fatigue offset.** A future match-side conditioning input;
  KD-1 currently projects only training-fatigue → starting fatigue.
- **A stochastic training extension.** If a later spec adds a genuine #29-owned random training outcome, it
  promotes `_RESERVED_0x21_` → `DOMAIN_TAG_TRAINING` / `SubsystemOrdinals` 83 at that first draw site (KD-6).
  Nothing in the current design draws, so the reservation stands unpromoted.

## 7.3 Seam contracts recorded for downstream authors

- **#34 (coaching):** becomes the producer of a non-identity `CoachingModifier`. The routing seam is a value
  parameter; #34 MUST NOT add a second training-effectiveness path — it supplies the modifier #29 already
  reads (the KD-3 identity contract).
- **#41 (injuries):** consumes `InjuryRiskContribution` (read-only). #41 owns occurrence/severity/recovery;
  #29 MUST NOT gain an injury model (the KD-5 boundary, cross-checked at #41's section-file stage).
- **#28 (progression):** `ComputeTrainingInput` is the sole path by which training influences attributes;
  #29 MUST NOT write `PlayerAttributes` (the KD-2 / FR-PG-008 sole-writer contract).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial T-phase plan + downstream seam contracts. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | APPROVED. |
#endregion
