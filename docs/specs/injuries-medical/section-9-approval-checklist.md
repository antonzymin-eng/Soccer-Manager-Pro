# Injuries & Medical #41 — Section 9: Approval Checklist

**Created:** July 23, 2026
**Last Updated:** August 8, 2026, later same day (v0.5 — balance-pass AR pass 10 L5, relabelled v0.4 → v0.5 at pass 11 L1: the renumber commit was itself a versioned change and had claimed the same v0.4 the renumbered pass-9 row now holds — the pass-7 row had duplicated version 0.2; rows renumbered 0.3/0.4 and this chain relabelled to match)
**Last Updated (prior):** August 8, 2026 (v0.4, as-published v0.3 — balance-pass AR pass 9 M2: the §9.1 KD-1 gate and the §9.6 Decision's T-phase list stop ordering the registration the spec forbids)
**Last Updated (prior):** August 8, 2026 (v0.3, as-published v0.2 — balance-pass AR pass 7 M1: R-02's evidence phrase no longer signs off the phantom stream as verified)
**Last Updated (prior):** July 23, 2026 (v0.2 — AR-1/AR-2/AR-3 recorded; R-01..R-05 signed; APPROVED)
**Version:** 0.5
**Status:** APPROVED
**Source:** `docs/tracking/injuries-medical-design.md` v0.2

---

Checklist entries are verified against real source; nothing is checked without a programmatically
verifiable anchor (CLAUDE.md "Never fabricate verification values"). This is a **forward-design** spec —
implementation gates are open by construction (nothing is built yet); review gates track the pipeline.

## 9.1 Content gates

- [x] Every Appendix A constant carries exactly one source tag (`[FIXED]`/`[GT]`/`[DERIVED]`); no `[EST]`.
- [x] Every §3 algorithm has rules + a worked example (§3.6; Appendix B mid-recovery + post-fixture-draw
      save/restore; Appendix C behaviour-neutral identity).
- [x] KD scope stated: injury occurrence/severity/recovery on the world tick; the fatigue **accumulators**
      (#29/match engine), squad-selection consequences (#30), the medical-staff entity model (#34), and
      attribute decline from injury (#28) deferred to their owners (§1.2 / §7).
- [x] KD-1 single-clock, position-independent occurrence (one keyed derivation — no registered stream,
      ERR-041-012 — no free-running cursor, nothing to persist) stated with the save-round-trip consequence.
- [x] KD-2 read-only fatigue-input reconciliation (no shared counter, no write-back) + KD-8 behaviour-neutral
      identity stated.
- [x] KD-6 #30 tick-order back-prop (ERR-030-002) + the recovery-before-occurrence ordering guarantee
      stated precisely (gated on entry-state, not post-countdown state).
- [x] KD-7 persistence as a season-save sub-blob (not `WORLD_STORE_FORMAT_VERSION`) with the rationale
      recorded.

## 9.2 Implementation status (forward design — nothing built yet)

- [x] FR set complete + stable: FR-MD-001..027 (grep-verified: 27 unique, contiguous, in §2).
- [ ] `TacticalDirector.InjuriesMedical` assembly (value types + deterministic Stage-2 step) — **NOT
      STARTED** (T0).
- [ ] `MedicalSaveCodec` + season-save composition (a #30 change) — NOT STARTED (T1).
- [ ] `AdvanceMedicalDay` wired at #30's new reserved slot — NOT STARTED (T2, gated on the ERR-030-002
      back-prop landing in #30 first).
- [ ] Deep-tier severity distribution / recurrence / ledger-load input / staff modulation — NOT STARTED
      (T3).

## 9.3 Review gates

- [x] **PASS-1 (AR-1) adversarial review of the section files — RUN July 23, 2026 (§9.3.1); 1M fixed.**
- [x] **AR-2 → AR-3 convergence sweep — RUN July 23, 2026 (§9.3.1); AR-2 1M fixed, AR-3 clean → CONVERGENCE.**
- [x] **Lead-developer R-01..R-05 sign-off — GRANTED July 23, 2026 (§9.5).**

### 9.3.1 PASS-1 / AR-2 record

**AR-1 (1M):** §3.2/§3.3/§3.4 + appendices used float arithmetic (`(float)draw/(float)risk`, `1.0` weights, `RecoverySpeedMult`) against the #28/#29 integer-projection posture, and the per-tick recovery-speed multiply truncated to a no-op at base 1. Fixed: integer per-mille `MedicalModifier` multipliers with an explicit `Identity` (default() = ×0/div-0 → F4 fail-loud); integer cross-multiply severity bucketing (`SEVERITY_*_PERMILLE`); recovery-speed applied to assigned tier-days at injury time (floored at 1 for F1 coherence). **AR-2 (1M):** §3.1.1 `DeriveActionOrdinal` used the growing purpose *count* as its radix — appending a Stage-3 purpose would shift every prior occurrence ordinal, breaking replay/save parity (defeating the FR-MD-008 append-only guarantee). Fixed: a **fixed** `DRAW_PURPOSE_RADIX` (= 16) + a purpose bound guard; +T-MD-DET-009. **AR-3:** full-set sweep — no new High/Medium (the lone `DRAW_PURPOSE_COUNT` hit is a version-history entry); 27 FRs, all 8 KDs present, cross-refs consistent → **CONVERGENCE**.

## 9.4 Consistency gates

- [x] FR prefix `FR-MD-` verified unclaimed by grep over `docs/specs/**` (0 hits before this spec, per the
      design supplement's own grep at authoring time — to be re-verified at sign-off).
- [x] Candidate number #41 matches the roadmap; `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` /
      `SubsystemOrdinals.InjuriesMedical = 92` promotion (ERR-041-001) filed against
      `deterministic-sim/section-3.md` at approval.
- [x] Cited source APIs verified against real files: `InjuryRiskContribution`/`ComputeInjuryRisk` (#29
      FR-TR-017), `PlayerAttributes` (#27, 31 `int[1,20]` fields, no dedicated injury-proneness field),
      `WorldStore.AdvanceDay` / the pinned tick-order seams (#30 §3.3, FR-SN-009..012/034), `SeasonSaveCodec`
      sub-blob pattern (#30 §4), `CanonicalSerializer` (#16).
- [x] The #30 back-prop **ERR-030-002** (new injuries null seam, positioned after #28/#29/#33 and before
      `WorldStore.AdvanceDay()`) is filed and lands atomically with this spec's `APPROVED` flip.
- [x] `SPEC_INDEX.md` row added at promotion (`IN REVIEW`), to be flipped `IN REVIEW → APPROVED` at
      sign-off.

## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: SIGNED — July 23, 2026.** Design-supplement AR (2M+2L) + section-file AR-1 (1M) → AR-2 (1M) → AR-3 CONVERGENCE; the #16 §3.4 `0x2A`/92 promotion (ERR-041-001) and the #30 tick-order back-prop (ERR-030-002) filed and landed atomically with this flip. Forward-design approval per the #21–#30 precedent (approved before T0 code; the §7 T-phase plan is the post-APPROVED sequence).

| # | Review gate | Evidence | Status |
|---|---|---|---|
| R-01 | **Content completeness** — §1–§9 + appendices per the template | all files | ☑ |
| R-02 | **Technical accuracy** — ONE keyed occurrence derivation (no registered stream — ERR-041-012), position-independent draws, no
        persisted cursor; the KD-6 entry-state-gated ordering guarantee; the risk-score assembly citing
        #29's real `InjuryRiskContribution` output; the day-0 sentinel + day-gap fail-loud; 27 FRs;
        constants one tag each, no `[EST]`; cited #27/#29/#30/#16 APIs verified; **integer-only arithmetic (AR-1)** | §2/§3/§4/App. A/B/C | ☑ |
| R-03 | **Cross-spec consistency** — the #30 ERR-030-002 back-prop (new slot after #28/#29/#33, before
        `AdvanceDay()`); the KD-2 read-only fatigue-input boundary (no double count); the KD-5 identity
        seam (no phantom #34); the FR-MD-025 regen/retire roster-membership handoff mirroring #28
        FR-PG-011/015 and #29 FR-TR-025; no reverse reference (#41 references #29/#27/#16 only; #29/#27
        stay schema-untouched); the `0x2A`/92 + ERR-030-002 back-props filed | §1 / §4 / §7 | ☑ |
| R-04 | **Stage-binding correctness** — world-tick off-pitch cadence (not the match loops); byte-exact
        save/restore with no RNG cursor to restore; the `[GT]` magnitudes honestly illustrative | §1 / §3 /
        §6 | ☑ |
| R-05 | **Approval granted** — all AR resolved; `SPEC_INDEX.md` flipped `IN REVIEW → APPROVED` | | ☑ |

## 9.6 Decision

**APPROVED — July 23, 2026.** Section files authored from the converged design supplement
(`docs/tracking/injuries-medical-design.md` v0.2, design-AR 2M+2L → CONVERGENCE); section-file AR-1 (1M float→integer) → AR-2 (1M fixed-radix) → AR-3 CONVERGENCE; R-01..R-05 signed. `SPEC_INDEX.md` row 41 flipped `IN REVIEW → APPROVED`; the #16 `0x2A`/92 (ERR-041-001) and #30 tick-order (ERR-030-002) back-props landed atomically. This approves the **forward design** — the §7 T-phase plan (T0 value types → T1 save sub-blob → T2 wiring at #30's slot → T3 deep tier) is the post-APPROVED implementation sequence. *(The Decision as ratified in July named "stream registration" in the T2 item; ERR-041-012 retired the registered stream — the occurrence draw is a keyed SplitMix64 derivation, ordinal 92 deliberately unallocated — and the T2 that actually landed registered nothing.)*

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial checklist. Content/consistency/implementation gates open by construction; review gates NOT YET RUN. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M float→integer) / AR-2 (1M fixed-radix) / AR-3 CONVERGENCE recorded (§9.3.1); 9.1/9.4 gates checked; R-01..R-05 signed (§9.5); §9.6 APPROVED. |
| 0.3 | 2026-08-08 | — | **Balance-pass AR pass 7 (M1)** *(row renumbered 0.2 → 0.3 at pass 10 L5 — it had shipped colliding with the July 23 row, leaving the pass-7 edit unaddressable by version)*: R-02 signed off "ONE `injuries.occurrence` stream" as verified evidence; restated to the keyed derivation (ERR-041-012). |
| 0.4 | 2026-08-08 | — | **Balance-pass AR pass 9 (M2)** *(renumbered 0.3 → 0.4 with the row above)*: two residuals in the file pass 7 bumped for this class — the §9.1 content gate still ticked KD-1 as "one stream", and the §9.6 **Decision** still ratified a T-phase plan whose T2 item ordered "stream registration" (the pass-7 M1 wording, surviving in the approval that signs the plan off). Both re-anchored; the Decision keeps the ratified-as wording as an annotated frozen original. |
| 0.5 | 2026-08-08 | — | **Balance-pass AR pass 10 (L5)**: rows renumbered 0.2 → 0.3 / 0.3 → 0.4 (the pass-7 edit had collided with the July 23 row, leaving it unaddressable by version) and the header chain relabelled. Recorded as its own version at pass 11 (L1) — the renumber had reused v0.4 in the header while the table's 0.4 described pass 9, relocating the duplicate-label defect from the table into the chain. |
#endregion
