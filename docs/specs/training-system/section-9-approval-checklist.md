# Training System #29 — Section 9: Approval Checklist

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.3 — PASS-2 hostile re-review 1H+6M+2L recorded in §9.3.1, all fixed →
re-converged; FR set 24 → 26; re-affirmed APPROVED)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/training-system-design.md` v0.4

---

Checklist entries are verified against real source; nothing is checked without a programmatically verifiable
anchor (CLAUDE.md "Never fabricate verification values"). This is a **forward-design** spec — implementation
gates are open by construction (nothing is built yet); review gates track the pipeline.

## 9.1 Content gates

- [x] Every Appendix A constant carries exactly one source tag (`[FIXED]`/`[GT]`); no `[EST]`.
- [x] Every §3 algorithm has rules + a worked example (Appendix B mid-week save; Appendix C behaviour-
      neutral identity; §3.1–§3.4 pseudocode).
- [x] KD scope stated: training on the world tick; the growth **curve** (#28), injury **model** (#41), and
      coaching **attributes** (#34) deferred to their specs (§1.2 / §7).
- [x] KD-1 fatigue reconciliation (single accumulator + pure one-directional projection, no shared counter,
      no write-back) + KD-8 behaviour-neutral identity stated with the save-round-trip consequence.
- [x] KD-2 single-owner attribute mutation (pure `ComputeTrainingInput` feeds #28; no second writer) +
      KD-6 no-stream determinism stated.

## 9.2 Implementation status (forward design — nothing built yet)

- [x] FR set complete + stable: FR-TR-001..026 (grep-verified: 26 unique, contiguous, in §2; +FR-TR-025/026 from PASS-2).
- [ ] `TacticalDirector.TrainingSystem` assembly (value types + deterministic Stage-2 step) — **NOT
      STARTED** (T0).
- [ ] `TrainingSaveCodec` + season-save composition (a #30 change) — NOT STARTED (T1).
- [ ] `AdvanceTrainingDay` / `ComputeTrainingInput` wired at #30's reserved slots — NOT STARTED (T2, gated
      on #30 implemented first).
- [ ] Deep per-attribute growth input + #34 coaching consumption — NOT STARTED (T3).

## 9.3 Review gates

- [x] **PASS-1 adversarial review of the section files — RUN July 23, 2026 (results in §9.3.1); all fixed.**
- [x] **AR-2 → AR-3 convergence sweep — RUN July 23, 2026 (results in §9.3.1); CONVERGENCE.**
- [x] **Lead-developer R-01..R-05 sign-off — GRANTED July 23, 2026 (§9.5).**

### 9.3.1 PASS-1 / AR-2 record

**Design-supplement AR (pre-promotion) — July 23, 2026 (1H+1M+2L); all fixed.**
- **H — phantom RNG stream:** the draft KD-6 invented a `training.session` stream to justify promoting
  `0x21` while citing FR-LW-031 (which forbids exactly that). #29 has no honest #29-owned stochastic draw
  site (growth flows through #28's deterministic curve; injury variation is #41's). Resolved to **fully
  deterministic — no stream**; `_RESERVED_0x21_` / 83 stay reserved (KD-6).
- **M-1 — Form/Fitness two-cursor muddle:** two undefined overlapping cursors, and "form" is really
  match-participation/morale-driven (a boundary leak / shared-writer risk in a *training* spec). Collapsed to
  one #29-owned `Condition` cursor; match-driven sharpness deferred to its owner (§1.2 / §7.2).

**Section-file PASS-1 — July 23, 2026 (0H+1M); fixed.**
- **M-1 (day-0 idempotency sentinel collision → double-accrual):** the §3.1 guard used
  `LastAdvancedWorldDay != 0` as the "never advanced" escape, so a fresh state (struct-default `0`)
  advancing on world-**day 0** stored `0`, and a save→restore→re-run of day 0 (`0 <= 0 && 0 != 0` → false)
  advanced **again** — the sentinel `0` collided with a legitimate day 0 (the sentinel-collision class #28
  avoided by deriving age). Fixed: an explicit `TRAINING_NOT_ADVANCED_SENTINEL = uint.MaxValue` seeded by a
  `TrainingState.Create` factory; the guard is `!= SENTINEL && worldDay <= last`. `default(TrainingState)`
  is documented as not a valid runtime state. Locked by new T-TR-DET-004 (§5).

**AR-2 → AR-3 (closing the original PASS-1 cycle) — July 23, 2026 (0H+0M; CONVERGENCE).** Regression sweep
of the day-0 fix: the sentinel is referenced consistently across §2.2 (struct + factory), §3.1 (guard),
Appendix A (const), §5 (T-TR-DET-004); Appendix B's mid-game seed (`LastAdvancedWorldDay = 100`) is
consistent (the sentinel is only the fresh-state seed). §6.2 "gap-independent" wording (which implied skips
occur) softened. Full grep sweep: no phantom-stream / two-cursor residue; FR-TR-001..024 contiguous (this was
the pre-PASS-2 count). No new High/Medium at this round — the original cycle closed here; **PASS-2 below then
re-opened it.**

**PASS-2 (post-approval hostile re-review, /adversarial-review) — July 23, 2026 (1H+6M+2L); all fixed →
re-converged.** The original PASS-1 was too lenient on freshly-authored work; a full re-read against the
actual #28/#30 APIs surfaced real defects (FR count 24 → 26):
- **H — regen/retire `TrainingState` membership handoff missing.** #28 churns the roster every season
  (FR-PG-011/015: fresh `PlayerId` per regen, retiree block entry removed), but #29 had no parallel rule —
  retired entries would leak unboundedly and regens would have no state (a `default(TrainingState)` reviving
  the day-0 trap). Fixed: **FR-TR-025** (regen inserts `TrainingState.Create(Balanced)`, retiree removes) +
  KD-7 lifecycle clause + §3.5 note + T-TR-LIFE-001; F7 covers the missing-state case.
- **M — §3.5 cited a non-existent #28 API** (`GrowthProjection.Step`, per-player) vs #28's real batch
  `AdvanceDay(worldDay, in trainingInputs)` (FR-PG-021). §3.5/§4.3/§7/App. C rewritten to the batch shape.
- **M — `ComputeTrainingInput` gated on #28's `curveEnabled`.** Now a #29-owned `deepTrainingEnabled`
  parameter (FR-TR-007), with the stated coupling that #28 realizes the input only under its own curve.
- **M — focus double-stored** (`TrainingState.Focus` + a serialized `TrainingSchedule`). Collapsed:
  `TrainingState.Focus` is the single source of truth; `TrainingSchedule` is a derived read-only view, not
  serialized (FR-TR-003/019, §2.2/§4/§5 realigned) — this also removes the readonly-struct-over-mutable-map
  footgun.
- **M — silent day-gap under-accrual.** `AdvanceTrainingDay` now **fails loud** on `worldDay > LastAdvanced
  + 1` (FR-TR-026 / F7 / T-TR-DET-005) instead of skipping days.
- **M — no passive fatigue recovery** (match-entry fatigue saturated to 1.0 under any non-Rest regime).
  Added `FATIGUE_DAILY_RECOVERY` (net = load − recovery); §3.1 + Appendix A/B recomputed.
- **M — "no staleness" mis-justified** ("pure" ≠ order-safe). Restated as the load-bearing FR-TR-006
  invariant: `ComputeTrainingInput` reads only fields slot-2 does not mutate.
- **L — `InjuryRiskContribution` named both the type and its function** → method renamed `ComputeInjuryRisk`.
- **L — #31/#38 absent from the §1.3 dependency table** → observer-consumer row added.

**PASS-2 convergence — July 23, 2026 (0H+0M; CONVERGENCE).** Post-fix sweep: FR-TR-001..026 contiguous (26
unique); `deepTrainingEnabled` / `FATIGUE_DAILY_RECOVERY` / F7 / `ComputeInjuryRisk` / the batch
`AdvanceDay(worldDay, …)` and the FR-TR-025 lifecycle referenced consistently across §1–§7 + appendices; no
stale `GrowthProjection.Step` / per-day `+300` arithmetic / separately-serialized-`TrainingSchedule` residue;
Appendix B recomputed (net +100/day, projection 0.23). No new High/Medium — the cycle re-closes.

## 9.4 Consistency gates

- [x] FR prefix `FR-TR-` verified unclaimed by grep over `docs/specs/**` (0 hits before this spec).
- [x] Candidate number #29 matches the roadmap; `_RESERVED_0x21_` / `SubsystemOrdinals` 83 are held for #29
      in `deterministic-sim/section-3.md` and — per KD-6 — **remain reserved** (no promotion; #29 registers
      no stream).
- [x] Cited source APIs verified against real files: `PlayerAttributes` (31 `int[1,20]` + `WeakFootRating`;
      **no Form/Fitness/Condition field**), `PlayerAttributeProjection` (caller-supplied `float fatigue`,
      KD-P4 — the KD-1 target), #28 `GrowthProjection` sole writer + `TrainingInput.Neutral` append point
      (FR-PG-008/009) + the **batch** public entry `AdvanceDay(worldDay, in trainingInputs)` (FR-PG-021 —
      the §3.5 composition cites this real API, not a per-player `GrowthProjection.Step`) + the regen/retire
      roster churn (FR-PG-011/015, the FR-TR-025 handoff mirror), #30 `WorldStore.AdvanceDay` slot-1/slot-2
      reserved null seams + `SeasonSaveCodec` sub-blob, `CanonicalSerializer`.
- [x] `SPEC_INDEX.md` row added at promotion (`IN REVIEW`), flipped `IN REVIEW → APPROVED` at sign-off.
- [x] #16 §3.4: a **note** (ERR-029-001) records #29 confirmed deterministic — `_RESERVED_0x21_` / 83 stay
      reserved (no promotion, no code const, no `DETERMINISM_DIGEST_VERSION` bump).

## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: SIGNED — July 23, 2026 (re-affirmed at v0.3 after PASS-2).** Design-supplement AR (1H+1M+2L) +
> section-file PASS-1 (1M) → AR-2 → AR-3, then **PASS-2 hostile re-review (1H+6M+2L) → PASS-2 convergence**,
> all resolved (§9.3.1, 0H unresolved). Forward design (nothing built) — sign-off approves the DESIGN, as
> #21–#30 were approved before their T0 code; the §7 roadmap is the post-APPROVED sequence.

| # | Review gate | Evidence | Status |
|---|---|---|---|
| R-01 | **Content completeness** — §1–§9 + appendices per the template | all files | ☑ |
| R-02 | **Technical accuracy** — ONE `Condition` cursor + ONE fatigue accumulator; the KD-1 pure/no-write-back projection; KD-2 pure-read single-owner seam citing #28's real batch `AdvanceDay` API; the day-0 sentinel + day-gap fail-loud; passive fatigue recovery; the #29-owned `deepTrainingEnabled` gate; **26 FRs**; constants one tag each, no `[EST]`; cited #27/#28/#30/#16 APIs verified (PASS-2) | §2/§3/§4/App. A/B/C | ☑ |
| R-03 | **Cross-spec consistency** — no-stream determinism (0x21/83 stay reserved); the no-reverse-reference invariant (#29 references #27/#28/#16 only; #28 schema-untouched); **the FR-TR-025 regen/retire roster-membership handoff mirroring #28 FR-PG-011/015** (PASS-2 High fix); the KD-3 identity seam (no phantom #34) + KD-5 injury output (no phantom #41); #30's tick order honored (no reorder, no staleness — the FR-TR-006 field-independence invariant) | §1 / §4 / §7 | ☑ |
| R-04 | **Stage-binding correctness** — world-tick off-pitch cadence (not the match loops); byte-exact save/restore; training-fatigue ≠ match-fatigue (no shared counter); the `[GT]` magnitudes honestly illustrative | §1 / §3 / §6 | ☑ |
| R-05 | **Approval granted** — all AR resolved; `SPEC_INDEX.md` flipped `IN REVIEW → APPROVED` | | ☑ |

## 9.6 Decision

**APPROVED — July 23, 2026 (re-affirmed at v0.3 after PASS-2).** The section files are authored from the
converged design supplement (v0.4, design-AR 1H+1M+2L); the section-file PASS-1 (0H+1M day-0 sentinel) →
AR-2 → AR-3, then a post-approval hostile **PASS-2 (1H+6M+2L — the regen/retire lifecycle High, the #28
batch-API citation, the `curveEnabled` conflation, the focus duplication, the day-gap fail-loud, the fatigue
recovery, the staleness invariant) → PASS-2 convergence**, are all resolved (§9.3.1); #29 is confirmed
**fully deterministic** (no RNG stream — `_RESERVED_0x21_` / 83 stay reserved,
ERR-029-001, no `DETERMINISM_DIGEST_VERSION` bump); and lead-developer R-01..R-05 sign-off is granted
(§9.5). `SPEC_INDEX.md` row 29 flips `IN REVIEW → APPROVED`. This approves the **forward design** (the
#21–#30 pre-T0 precedent); the §7 plan (T0 value types + deterministic Stage-2 step → T1
`TRAINING_SAVE_FORMAT_VERSION` sub-blob + season-save composition → T2 `AdvanceTrainingDay`/
`ComputeTrainingInput` at #30's reserved seams → T3 deep per-attribute growth + coaching consumption) is the
post-APPROVED sequence.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial checklist. Content/consistency gates checked; review gates OPEN by construction. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Design-AR (1H+1M+2L) + PASS-1 (1M) → AR-2 → AR-3 recorded (§9.3.1); R-01..R-05 signed; §9.6 APPROVED. |
| 0.3 | 2026-07-23 | — | PASS-2 hostile re-review (1H regen/retire lifecycle + 6M + 2L) → PASS-2 convergence; FR set 24 → 26 (FR-TR-025/026); R-02/R-03 + §9.6 updated; re-affirmed APPROVED. |
#endregion
