# Club Finances & Economy #40 — Section 9: Approval Checklist

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-1/AR-2/AR-3 recorded; R-01..R-05 signed; APPROVED)
**Version:** 0.2
**Status:** APPROVED
**Source:** `docs/tracking/club-finances-economy-design.md` v0.2

---

Checklist entries are verified against real source; nothing is checked without a programmatically
verifiable anchor (CLAUDE.md "Never fabricate verification values"). This is a **forward-design** spec —
implementation gates are open by construction (nothing is built yet); review gates track the pipeline.

## 9.1 Content gates

- [x] Every Appendix A constant carries exactly one source tag (`[FIXED]`/`[GT]`/`[DERIVED]`); no `[EST]`.
- [x] Every §3 algorithm has rules + a worked example (§3.5; Appendix B mid-season + mid-boundary-roll
      save/restore; Appendix C behaviour-neutral identity).
- [x] KD scope stated: budget/ledger/revenue on the season boundary; negotiation (#31), board/ownership
      (#45), staff wages as a mechanic (#34), and the season loop that drives #40 (#30) deferred to their
      owners (§1.2 / §7).
- [x] KD-2 minimal-is-pure-no-draw (reserved, not promoted, namespace slot; the #29 `0x21` precedent) stated
      with its promotion condition (the T3 deep-tier draw).
- [x] KD-3 read-only #31 constraint query + one-way `ApplyTransaction` command (no two-way coupling) + KD-8
      behaviour-neutral identity stated.
- [x] KD-6 #30 season-boundary back-prop (ERR-030-003) + the ordering rationale relative to the #43 (a')
      insertion point stated precisely.
- [x] KD-7 persistence as a season-save sub-blob (not `WORLD_STORE_FORMAT_VERSION`) with the rationale
      recorded, including the club-vs-player lifecycle contrast (KD-7/FR-FN-025).

## 9.2 Implementation status (forward design — nothing built yet)

- [x] FR set complete + stable: FR-FN-001..028 (grep-verified: 28 unique, contiguous, in §2).
- [ ] `TacticalDirector.ClubFinances` assembly (value types + deterministic Stage-2 step) — **NOT STARTED**
      (T0).
- [ ] `ClubFinancesSaveCodec` + season-save composition (a #30 change) — NOT STARTED (T1).
- [ ] `SettleFinances` wired at #30's new reserved slot — NOT STARTED (T2, gated on the ERR-030-003 back-prop
      landing in #30 first).
- [ ] Deep-tier per-day accrual / stochastic sponsorship variance / FFP soft-penalty / board modulation / #31
      / #34 wage producers — NOT STARTED (T3).

## 9.3 Review gates

- [x] **PASS-1 (AR-1) adversarial review — RUN July 23, 2026 (§9.3.1); 1M fixed.**
- [x] **AR-2 → AR-3 convergence sweep — RUN July 23, 2026 (§9.3.1); AR-2/AR-3 clean → CONVERGENCE.**
- [x] **Lead-developer R-01..R-05 sign-off — GRANTED July 23, 2026 (§9.5).**

### 9.3.1 PASS-1 / AR record

**AR-1 (1M):** §3.2 `ApplyTransaction` moved BOTH `Balance` and `WageBillAggregate` by a wage's `Amount`, conflating a one-time cash payment with an ongoing liability (repeated wage debits would inflate the aggregate unboundedly). Fixed: a wage line item (`PlayerWage`/`StaffWage`) changes `WageBillAggregate` **only** (the current wage bill); a cash line item (`TransferFee`/`General`) changes `Balance` **only**; the periodic wage cash-out that drains `Balance` from the aggregate is a deferred deep-tier accrual (§7). FR-FN-016 / §3.2 / §3.5 / Appendix B / T-FN-LEDGER-002 / T-FN-BOUND-003 aligned. **AR-2:** full-set sweep — codec correct (6 I64 fields, ClubId-ascending, F1/F3/F5 gates, no RNG cursor), the wage fix consistent, no float, no regressions → no new High/Medium. **AR-3:** 28 FRs, all 8 KDs present, `_RESERVED_0x29_` reserved-not-promoted consistent → **CONVERGENCE**. (The subagent proactively applied the #41 `MedicalModifier` zero-value-trap lesson to `BoardModifier` — explicit per-mille `Identity`, `default()` fails loud — so that class did not recur as a finding.)

## 9.4 Consistency gates

- [x] FR prefix `FR-FN-` verified unclaimed by grep over `docs/specs/**` (0 hits before this spec, per the
      design supplement's own grep at authoring time — to be re-verified at sign-off).
- [x] Candidate number #40 matches the roadmap; `_RESERVED_0x29_` / `SubsystemOrdinals.ClubFinances = 91`
      placeholder-row promotion (ERR-040-001) filed against `deterministic-sim/section-3.md` at approval
      (reserved, **not** a named tag — KD-2).
- [x] Cited source APIs verified against real files: `Squad.ClubId` (#27), `RollToNextSeason()` / the pinned
      `(a) finalize → (b) board → (a') #43 → (c) regenerate → (d) advance-ages → (e) reset` sequence (#30
      §3.5, FR-SN-029/031), `SeasonSaveCodec` sub-blob pattern (#30 §4), `CanonicalSerializer` (#16).
- [x] The #30 back-prop **ERR-030-003** (new finance-settlement step (b'), positioned after the (a') #43
      insertion point and before (c) regenerate) is filed and lands atomically with this spec's `APPROVED`
      flip.
- [x] `SPEC_INDEX.md` row added at promotion (`IN REVIEW`), to be flipped `IN REVIEW → APPROVED` at
      sign-off.

## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: SIGNED — July 23, 2026.** Design-supplement AR (1M+1L) + section-file AR-1 (1M wage semantics) → AR-2 → AR-3 CONVERGENCE; the #16 `_RESERVED_0x29_`/91 reservation (ERR-040-001) and #30 boundary-roll back-prop (ERR-030-003) filed and landed atomically with this flip. Forward-design approval per the #21–#30 precedent.

| # | Review gate | Evidence | Status |
|---|---|---|---|
| R-01 | **Content completeness** — §1–§9 + appendices per the template | all files | ☑ |
| R-02 | **Technical accuracy** — the pure `budget = f(finalTablePosition, prizeMoney)` projection; the
        reserved-not-promoted `_RESERVED_0x29_`/91 namespace slot; the KD-6 ordering rationale relative to
        the #43 (a') insertion point; the risk-free integer-only arithmetic throughout; 28 FRs; constants
        one tag each, no `[EST]`; cited #27/#30/#16 APIs verified; **wage vs cash ledger split (AR-1)** | §2/§3/§4/App. A/B/C | ☑ |
| R-03 | **Cross-spec consistency** — the #30 ERR-030-003 back-prop (new step (b') after (a'), before (c));
        the KD-3 read-only #31 boundary + one-way `ApplyTransaction` command (no two-way coupling); the
        KD-5 identity seam (no phantom #31/#34); the KD-4 identity seam (no phantom #45); no reverse
        reference (#40 references #27/#16 only); the `_RESERVED_0x29_`/91 + ERR-030-003 back-props filed | §1 / §4 / §7 | ☑ |
| R-04 | **Stage-binding correctness** — season-boundary cadence (not the world tick, not the match loops);
        byte-exact save/restore with no RNG cursor to restore at minimal; the `[GT]` magnitudes honestly
        illustrative | §1 / §3 / §6 | ☑ |
| R-05 | **Approval granted** — all AR resolved; `SPEC_INDEX.md` flipped `IN REVIEW → APPROVED` | | ☑ |

## 9.6 Decision

**APPROVED — July 23, 2026.** Section files authored from the converged design supplement
(`docs/tracking/club-finances-economy-design.md` v0.2, design-AR 1M+1L → CONVERGENCE); section-file AR-1 (1M wage semantics) → AR-2 → AR-3 CONVERGENCE; R-01..R-05 signed; `SPEC_INDEX.md` row 40 flipped `IN REVIEW → APPROVED`; the `_RESERVED_0x29_`/91 (ERR-040-001) and #30 boundary-roll (ERR-030-003) back-props landed atomically. The §7 T-phase plan
(T0 value types → T1 save sub-blob → T2 wiring at #30's slot + `ApplyTransaction` command → T3 deep tier) is
the post-`APPROVED` implementation sequence and is not gated on anything in this checklist beyond `APPROVED`
itself.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial checklist. Content/consistency/implementation gates open by construction; review gates NOT YET RUN. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M wage semantics) / AR-2 / AR-3 CONVERGENCE recorded (§9.3.1); 9.1/9.4 gates checked; R-01..R-05 signed (§9.5); §9.6 APPROVED. |
#endregion
