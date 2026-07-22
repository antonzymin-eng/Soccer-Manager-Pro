# Season & Competition Loop Specification #30 — Section 9: Approval Checklist

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 fixes, §9.3)
**Version:** 0.2
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

- [x] FR set complete + stable: FR-SN-001..034 + 013a/013b (36 distinct; grep-verified in §2/§5).
- [ ] Layer built — **NOT STARTED** (forward design; §7 T-phase plan T0..T3).
- [ ] `MatchEngine` / `WorldStore` wiring — NOT STARTED (T2).
- [ ] Round-trip determinism proven — NOT STARTED (T1/T2 tests, §5.5/§5.6).

## 9.3 Review gates

- [x] **PASS-1 adversarial review of the section files — RUN July 22, 2026 (1H+2M+2L); all fixed.**
      **H-1:** the loop played one fixture per fixture-day and skipped the round's other `N/2−1`
      fixtures, leaving the league table **undefined for any N>2** (only the played clubs ever accrue
      points) — and the spec defined no managed-club concept or non-managed-fixture resolution. Fixed
      structurally: new KD-9 + FR-SN-012/013/013a/013b + §3.4 "play a round" — a fixture-day resolves
      **all** `N/2` fixtures (managed club through the full `MatchEngine`, the rest through a
      deterministic round-resolution model that gives the reserved `DOMAIN_TAG_SEASON_LOOP` sub-stream
      its concrete consumer); `SeasonState` gains `ManagedClubId`; the command becomes
      `AdvanceAndPlayNextRound`. **M-1/M-2 (API accuracy):** §3.4 cited `MatchEngine.RunToFullTime()`
      (does not exist — real is `RunTick()` to `MatchEnded`) and `ISquadProvider.Resolve` (real is
      `ResolveByClubId`); both corrected against source. **L:** `CurrentWorldTick` is `uint`
      (pinned in App. B / §3.3); a garbled §3.4 phrase (rewritten).
- [x] **AR-2 convergence sweep — RUN July 22, 2026 (0H+0M; L-only ⇒ CONVERGENCE).** Caught and
      fixed one regression the H-1 fix introduced (the new #30 KD-9 collided in name with the
      **living-world** KD-9/KD-10 cited in the producer-boundary context — all #22 KD references
      qualified `living-world KD-9/KD-10`) + label reconciliation (KD range 1→9; FR count 36 =
      34 + 013a/013b; the loose "KD-6 world-tick cadence" label → §1.2 convention; the odd-N bye case
      in the round-completeness test). Cycle closes per the #21–#27 L-only-round convention.
- [ ] **`DOMAIN_TAG_SEASON_LOOP = 0x22` / `SubsystemOrdinals.SeasonLoop = 84` cross-cite** — to be
      filed in Deterministic Sim #16 §3.4 + back-prop `ERR-030-001` **at approval** (only #30's row;
      `0x20`/`0x21` stay gaps for #28/#29 per KD-5 / roadmap §6). Not filed at IN REVIEW (the #27
      precedent files the cross-cite at the APPROVED transition).
- [ ] **#22 phase-1 contract cross-check** — the section-file pass MUST re-read `FR-LW-027`/
      `FR-LW-032`/living-world KD-9/KD-10 before pinning the `MatchResult` producer payload; a #22 back-prop is
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

> **Status: OPEN — pending R-01..R-05.** PASS-1 → AR-2 converged (§9.3, 0H unresolved); this is a forward design, so sign-off waits on the
> #16 §3.4 `0x22`/84 cross-cite (filed at approval) and the human review, not on further §9.3 review gates.

| # | Review gate | Evidence to confirm | Status |
|---|---|---|---|
| R-01 | **Content completeness** — §1–§9 + appendices per the template | `outline.md`, `section-1..8`, `section-9`, `appendices.md` | ☐ |
| R-02 | **Technical accuracy** — circle-method fixtures / table+tie-break / day-advance order / season codec internally consistent; 36 FRs (FR-SN-001..034 + 013a/013b); constants one tag each, no `[EST]` | §2/§3/App. A/B; verified against `src/season-save/*.cs` | ☐ |
| R-03 | **Cross-spec consistency** — the #16 §3.4 `0x22`/84 cross-cite (filed at approval); the #22 producer-not-ingest boundary reconciled with `FR-LW-032`/FR-LW-031; the `SeasonSaveCodec`/`SeasonSaveManager` signature change stated | §4 / §7 / #16 §3.4 / #22 §2 | ☐ |
| R-04 | **Stage-binding correctness** — world-tick cadence (§1.2) (not per-tick / not zero-alloc); KD-1 season blob as opaque sub-blob; KD-8 behaviour-neutral world floor; single-league Stage-2 surface as #43's identity | §1 / §4 / §6 / §7 | ☐ |
| R-05 | **Approval granted** — PASS-1 + AR-2 resolved; `SPEC_INDEX.md` row flipped `IN REVIEW → APPROVED` | ☐ |

## 9.6 Decision

**IN REVIEW — July 22, 2026.** The section files are authored from the converged design supplement
(v0.2, AR-1 1M+3L → AR-2 clean), grounded in the real `season-save`/`WorldStore`/`MatchEngine` APIs,
and the **section-file PASS-1 (1H+2M+2L) → AR-2 convergence** is resolved (§9.3 — the H-1 whole-round
resolution fix is the load-bearing one). Outstanding before APPROVED: the #16 §3.4 `0x22`/84
cross-cite (+ any #22 back-prop, filed at approval); R-01..R-05 sign-off. No code is built — the §7
T-phase plan is the forward implementation sequence.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial checklist. Content/consistency gates checked; implementation + review gates OPEN by construction (forward design). Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (1H+2M+2L) → AR-2 convergence recorded in §9.3; H-1 = whole-round resolution (KD-9). §9.5 status note updated (sign-off is the remaining gate). |
#endregion
