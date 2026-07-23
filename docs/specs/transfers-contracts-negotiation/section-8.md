# Transfers, Contracts & Negotiation #31 — Section 8: References & Cross-References

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 8.1 Cross-spec cross-references (XC-031-*)

| ID | Direction | Target | Contract |
|---|---|---|---|
| XC-031-001 | #31 → #27 | `PlayerRecord`/`PlayerAttributes`/`Squad`; `PlayerId = clubId*CLUB_SQUAD_SIZE+localIndex`; `CLUB_SQUAD_SIZE = 25` | Read the pool + valuation inputs; identity is #27-owned (KD-1/KD-7). |
| XC-031-002 | #31 → #40 | `AvailableTransferBudget(in ClubFinances) → long` (read), `ApplyTransaction(ref ClubFinances, in FinanceTransaction)` (commit), `FinanceLineItem.{TransferFee,PlayerWage}` | The budget constraint + the single mutation path (KD-2). #40 FR-FN-013 already names #31 the caller. |
| XC-031-003 | #31 → #30 (via composition root) | `RunWorldTickInFixedOrder` slot (new), `SeasonCalendar` (read), `SeasonSaveCodec`/`SEASON_SAVE_FORMAT_VERSION` (compose), `RequestRosterCommit` (new mid-season entry point) | #30 invokes #31 + owns roster/calendar/save (KD-4/KD-6/KD-7). #31 never references #30. |
| XC-031-004 | #31 → #16 | determinism namespace; `_RESERVED_0x23_` / `SubsystemOrdinals.Transfers = 85` (RESERVED); world-tick `DeterministicRngService` (deep only) | Draw-free minimal (KD-5); promotes at the deep-tier first draw. |
| XC-031-005 | #33 → #31 (deferred) | `PersonalityProfile` / `MoraleOf` (read-only) | #31 is a read-only consumer (#33 §7.3, FR-HS-024); deep-tier valuation modulation (KD-1). |
| XC-031-006 | #28 → #31 (deferred) | CA/PA career-state keyed by `PlayerId` | Deep-tier valuation refinement (KD-1); not a minimal dependency. |
| XC-031-007 | #31 → #32 / #34 (deferred, producer side) | the KD-3 offer/response seam (`Offer`/`NegotiationOutcome`/`EvaluateOffer`, counterparty-generic) | #31 publishes the reusable seam; #32 (scouting bids) / #34 (staff hiring) consume it; #31 builds no interface for them (FR-LW-031). |

## 8.2 Determinism references

- `_RESERVED_0x23_` / `0x23` / [FIXED] — the #16 §3.4 placeholder row (`deterministic-sim/section-3.md:267`),
  held for #31, `SubsystemOrdinals.Transfers = 85`. **Stays RESERVED at #31 approval** (draw-free minimal,
  KD-5) — the #40 `_RESERVED_0x29_` (ERR-040-001) / #29 `_RESERVED_0x21_` precedent. Promotes to
  `DOMAIN_TAG_TRANSFERS = 0x23` at #31 T3's first rival-bid draw.

## 8.3 Back-prop references

- **ERR-030-004 (proposed, at #31 approval)** — #30 §3.3 `RunWorldTickInFixedOrder` gains the transfers
  tick-order null-seam slot (a fill of a documented position, the ERR-030-002 #41 precedent; the FR-SN-034
  enumeration extends to #31). Doc-only; the seam is empty until #31 T2/T3.
- **ERR-030-005 (deferred, at #31 T2)** — #30 grows the mid-season `RequestRosterCommit` entry point +
  roster-move hook (KD-7); the outer `SEASON_SAVE_FORMAT_VERSION` 2 → 3 bump (T1) is coordinated in the same
  #30 T-phase window.
- **ERR-016 (deferred, at #31 T3)** — `DOMAIN_TAG_TRANSFERS = 0x23` promotion at the first rival-bid draw.

## 8.4 Master-plan & literature anchors

- Master development plan §4.3 (simplified transfers, accept/reject window), §5 (complex clauses) — the
  staging source (minimal §4.3 → deep §5). No external academic citation is load-bearing for the minimal
  deterministic valuation (it is a game-design tuning surface, not an empirical model — the #40 posture).
  Any deep-tier valuation-calibration references are recorded at the balance pass, not here.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §8 (XC-031-001..007, determinism reference, back-prop references, master-plan anchor). Status IN REVIEW. |
#endregion
