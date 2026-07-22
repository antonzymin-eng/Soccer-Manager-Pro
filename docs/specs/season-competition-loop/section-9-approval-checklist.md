# Season & Competition Loop Specification #30 — Section 9: Approval Checklist

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1 — promoted from design supplement; PASS-1 + sign-off OPEN)
**Version:** 0.1
**Status:** IN REVIEW
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

Checklist entries are verified against real source; nothing is checked without a programmatically
verifiable anchor (CLAUDE.md "Never fabricate verification values"). This is a **forward-design**
spec — the implementation gates are open by construction (nothing is built yet), and the review
gates track the promotion pipeline.

## 9.1 Content gates

- [x] Every constant in Appendix A carries exactly one source tag (`[FIXED]`
      `SEASON_SAVE_FORMAT_VERSION`/`SEASON_STATE_FORMAT_VERSION`; `[GT]` points/objective;
      `[CROSS]` domain-tag/ordinal mirrors of #16).
- [x] Every §3 algorithm has ranges + a worked example (circle-method 4-club schedule §3.7 / App. C;
      table tie-break App. D; the season-codec byte layout App. B).
- [x] No `[EST]` tags present.
- [x] KD-1 season sub-blob / `SEASON_SAVE_FORMAT_VERSION` 1 → 2 layout pinned (App. B) against the
      real `SeasonSaveCodec` frame.
- [x] KD-2 fixed day-advance tick order pinned with the Wave-2+ null-seam positions explicit (§3.3).
- [x] KD-3 producer-not-ingest boundary stated and reconciled against `FR-LW-032` / FR-LW-031.

## 9.2 Implementation status (forward design — nothing built yet)

- [x] FR set complete + stable: FR-SN-001..034 (grep-verified in §2/§5).
- [ ] Layer built — **NOT STARTED** (forward design; §7 T-phase plan T0..T3).
- [ ] `MatchEngine` / `WorldStore` wiring — NOT STARTED (T2).
- [ ] Round-trip determinism proven — NOT STARTED (T1/T2 tests, §5.5/§5.6).

## 9.3 Review gates — OPEN

- [ ] **PASS-1 adversarial review of the section files** — not yet run. Open gates recorded here.
- [ ] **AR-2 convergence sweep** — pending PASS-1.
- [ ] **`DOMAIN_TAG_SEASON_LOOP = 0x22` / `SubsystemOrdinals.SeasonLoop = 84` cross-cite** — to be
      filed in Deterministic Sim #16 §3.4 + back-prop `ERR-030-001` **at approval** (only #30's row;
      `0x20`/`0x21` stay gaps for #28/#29 per KD-5 / roadmap §6). Not filed at IN REVIEW (the #27
      precedent files the cross-cite at the APPROVED transition).
- [ ] **#22 phase-1 contract cross-check** — the section-file pass MUST re-read `FR-LW-027`/
      `FR-LW-032`/KD-9/KD-10 before pinning the `MatchResult` producer payload; a #22 back-prop is
      filed **only if** a genuine gap is found (KD-3). Deferred to #33's landing regardless
      (activation is not #30's).
- [ ] **Lead-developer R-01..R-05 sign-off** — pending (§9.5).

## 9.4 Consistency gates

- [x] FR prefix `FR-SN-` verified unclaimed by grep over `docs/specs/**/*.md` (existing prefixes: AT,
      BU, CS, DA, DM, DS, EVT, GK, HE, LW, PA, PO, PR, RO, SQ, TI, TP, TS).
- [x] Candidate number #30 matches the roadmap / `spec-plans/spec-30-…` reservation.
- [ ] `SPEC_INDEX.md` row added (`IN REVIEW`) — landed with this promotion.
- [x] Cited source APIs verified against real files (`SeasonSaveCodec.cs` `Encode(worldBlob,
      matchBlobOrNull)`; `SeasonSaveManager.Save(world, matchOrNull, path)`;
      `WorldStore.AdvanceDay`/`Snapshot`/`Restore`; `WorldLoop` phase-1 null seam; current
      `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` / next-free `0x20`).

## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: OPEN — pending PASS-1 convergence.** This is a forward design; sign-off waits on the
> section-file adversarial review closing (0H, no unresolved M/H) and the §9.3 gates.

| # | Review gate | Evidence to confirm | Status |
|---|---|---|---|
| R-01 | **Content completeness** — §1–§9 + appendices per the template | `outline.md`, `section-1..8`, `section-9`, `appendices.md` | ☐ |
| R-02 | **Technical accuracy** — circle-method fixtures / table+tie-break / day-advance order / season codec internally consistent; 34 FRs; constants one tag each, no `[EST]` | §2/§3/App. A/B; verified against `src/season-save/*.cs` | ☐ |
| R-03 | **Cross-spec consistency** — the #16 §3.4 `0x22`/84 cross-cite (filed at approval); the #22 producer-not-ingest boundary reconciled with `FR-LW-032`/FR-LW-031; the `SeasonSaveCodec`/`SeasonSaveManager` signature change stated | §4 / §7 / #16 §3.4 / #22 §2 | ☐ |
| R-04 | **Stage-binding correctness** — KD-6 world-tick cadence (not per-tick / not zero-alloc); KD-1 season blob as opaque sub-blob; KD-8 behaviour-neutral world floor; single-league Stage-2 surface as #43's identity | §1 / §4 / §6 / §7 | ☐ |
| R-05 | **Approval granted** — PASS-1 + AR-2 resolved; `SPEC_INDEX.md` row flipped `IN REVIEW → APPROVED` | ☐ |

## 9.6 Decision

**IN REVIEW — July 22, 2026.** The section files are authored from the converged design supplement
(v0.2, AR-1 1M+3L → AR-2 clean), grounded in the real `season-save`/`WorldStore`/`MatchEngine` APIs.
Outstanding before APPROVED: section-file PASS-1 → AR-2 convergence; the #16 §3.4 cross-cite + any
#22 back-prop; R-01..R-05 sign-off. No code is built — the §7 T-phase plan is the forward
implementation sequence.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial checklist. Content/consistency gates checked; implementation + review gates OPEN by construction (forward design). Status IN REVIEW. |
#endregion
