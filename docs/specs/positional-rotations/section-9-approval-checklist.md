# Positional Rotations Specification #25 — Section 9: Approval Checklist

**Created:** July 8, 2026
**Last Updated:** July 10, 2026, later same day (v0.4 — APPROVED)
**Version:** 0.4
**Status:** APPROVED

---

Entries verified against actual files; nothing checked without a verifiable anchor.

## 9.1 Content gates

- [x] Every constant tagged (§3.5: 5 `[GT]`, 1 `[FIXED]`; FR-RO-007 bound `[DERIVED]`); no `[EST]`
- [x] Every formula has units, ranges, worked example (FM-RO-01..02 + timeline)
- [x] KD-7 perception-boundary invariant cited verbatim (§1.4)
- [x] Zero-value-identity: `RotationFreedom.Off = 0` is the identity
- [x] The supplement's KD-4 risk items (a) trigger, (b) hysteresis, (c) ShapeAnalyzer contract each map to a KD + FR here (KD-3/FR-RO-004, KD-4/FR-RO-005..006, KD-5/FR-RO-007..008)
- [x] Appendix A adjacency tables authored for every shipped `FormationFamily` — **completed July 10, 2026**: A.2 (4-3-3, 5 rows) + A.3 (4-2-3-1, 6 rows) authored against the verified `Family433`/`Family4231` slot rosters (F442/F433/F4231 = the complete `FormationFamily.cs` enum); F1 hand-audit (GK-free / valid / distinct / ≤ 8) recorded per table; see Appendix A v0.3
- [x] `[CITATION-PENDING]` rows verified or replaced — **closed July 10, 2026** (Wilson VERIFIED ISBN 978-0-7528-8995-5; Memmert & Raabe book row REPLACED with the verified Low et al. 2020 review, DOI 10.1007/s40279-019-01194-7, per the OI-003 precedent); see §8.2 v0.2

## 9.2 Balance-pass carve-out

`[GT]` magnitudes illustrative pending the balance pass (#21 G2 precedent). Reviewed contract:
predicate shape, two-sided hysteresis, atomicity, caps, ordering contract.

## 9.3 Review gates

- [x] PASS-1 adversarial review — **run July 8, 2026: 1H+1M+3L, all resolved in the v0.2 fix pass same day** (`adversarial-review-section-files-v1.md`). The predicted §4.2 risk was real: H-1 = the previous-tick targets did not exist on `AgentPositioningData` and the restore re-seed broke byte-identity; now a controller-owned serialized cache
- [x] PASS-2 — run after the fixes per the pipeline rule (High found ⇒ repeat); re-read of the fixed surfaces clean at H/M
- [x] §2.4 back-props filed (incl. the #12 `SlotIndex` single-writer contract amendment) atomically with `APPROVED` — **DONE July 10, 2026**: ERR-021-007 / ERR-012-009 (spec-error-log.md v1.30; `positioning-ai/section-3.md` v0.6 §3.7.1 carries the contract amendment)
- [x] Lead-developer R-01..R-05 sign-off — **SIGNED July 10, 2026** (§9.5)

## 9.4 Consistency gates

- [x] FR prefix `FR-RO-` verified unclaimed by grep (July 8, 2026; the supplement's own AR caught and pre-empted the `FR-PR-` collision)
- [x] Candidate number #25 matches the `SPEC_INDEX.md` reservation
- [x] Away-team mirror test present (T-RO-I-002)
- [x] `SPEC_INDEX.md` status flip — **DONE July 10, 2026** (row 25, Approved Jul 10, 2026)


## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: SIGNED — July 10, 2026.** All five gates ticked by the lead developer.

| # | Review gate | Evidence to confirm | Status |
|---|---|---|---|
| R-01 | **Content completeness** — all sections (§1–§9 + appendices) present per the template | 11 files, all present | ☑ |
| R-02 | **Technical accuracy** — FM-RO-01..02 + timeline worked examples consistent; 18 FRs (FR-RO-001..018, grep-verified); FR-RO-007 `[DERIVED]` bound verified (`ROTATION_HOLD_TICKS` 30 ≥ `LINE_DWELL_TICKS` 5, 6× margin); Appendix A complete for all three shipped `FormationFamily` members with F1 hand-audits | §3; Appendix A v0.3; PASS-1 (1H+1M+3L → v0.2) + PASS-2 re-read clean at H/M | ☑ |
| R-03 | **Cross-spec consistency** — PASS-1 H-1 resolved with the controller-owned SERIALIZED `LastComposedTarget` cache (`AgentPositioningData` verified to carry no composed-target field; restore re-seed would break FR-RO-013/T-RO-DET-003 byte-identity); §2.4 back-props filed and landed (ERR-021-007/012-009); §8.2 closed (Wilson verified; Memmert & Raabe replaced with Low et al. 2020 per OI-003 precedent) | §2.4 v0.3 / §4.2 / §8.2 v0.2 | ☑ |
| R-04 | **Stage-binding correctness** — no phantom interfaces (FR-RO-018; cyclic/OOP rotations are §7 deferrals); `RotationFreedom.Off` zero-value identity (FR-RO-011); atomic pairwise swap + partner lock + permutation-validated restore (F2/F6); away-mirror test present (T-RO-I-002); phase-exit freeze (not reset) contradiction fixed at spec stage (PASS-1 M-1) | §2 / §3.4 / §5 / §7 | ☑ |
| R-05 | **Approval granted** — `SPEC_INDEX.md` row 25 flipped; `[GT]` balance pass carried forward (§9.2) | `SPEC_INDEX.md` row 25; §9.2 | ☑ |

## 9.6 Decision

**APPROVED — July 10, 2026.** Lead-developer R-01..R-05 sign-off granted. PASS-1 (1H+1M+3L)
resolved in v0.2 with PASS-2 re-read clean at H/M per the High-found rule; Appendix A complete
across the full `FormationFamily` enum; §8.2 closed; §2.4 back-props — including the #12
`SlotIndex` single-writer contract amendment the design supplement ranked riskiest — filed and
landed atomically (ERR-021-007 / ERR-012-009, spec-error-log.md v1.30). The §9.2 balance pass is
the carried-forward post-APPROVED item; the reviewed contract is the predicate shape, two-sided
hysteresis, atomicity, caps, and ordering contract.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial checklist; Appendix-A family completeness flagged as an open PASS-1 item. |
| 0.2 | 2026-07-08 | — | PASS-1 run and resolved (1H+1M+3L); PASS-2 re-read clean at H/M. Appendix-A family completeness remains open (§9.1). |
| 0.3 | 2026-07-10 | — | §9.1 Appendix-A completeness + citation gates both closed. Remaining open: §2.4 back-props at `APPROVED`; R-01..R-05 sign-off; status flip. |
| 0.4 | 2026-07-10 | — | **APPROVED.** §9.3 back-prop + sign-off gates closed; §9.4 status flip done; §9.5 R-01..R-05 table + §9.6 decision added. |
#endregion
