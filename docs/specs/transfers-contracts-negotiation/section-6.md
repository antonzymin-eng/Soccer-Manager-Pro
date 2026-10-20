# Transfers, Contracts & Negotiation #31 — Section 6: Performance

**Created:** July 23, 2026
**Last Updated:** October 20, 2026 (v0.3 — ERR-031-006: §6.2 `SubmitBid` roster cost restated for the stable-id re-home)
**Last Updated (prior):** July 23, 2026 (v0.2 — AR-4 fix pass; prior v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

## 6.1 Cadence

#31 has **no per-tick match-loop cost** — it is off-pitch, on the world tick only (KD-5). At minimal the
world-tick transfers slot is a **null seam** (no daily work; window open/close is a predicate evaluated at
command time), so a day with no manager action costs **zero** #31 work. `SubmitBid` runs **on demand**
(manager-initiated), a bounded amount of integer work per call.

## 6.2 Per-operation cost

- **`ValuePlayerPermille`** — one mean over the consumed #27 `[1,20]` attributes + two per-mille multiplies:
  a fixed number of integer ops, no allocation, no RNG.
- **`EvaluateOffer`** — one integer comparison.
- **`SubmitBid`** — the validate-all-first gate chain (window predicate, universe check, valuation,
  affordability) + **one** `ApplyTransaction` post (the transfer fee — wages are deep, FR-TX-005) + one
  `TryPreviewRosterCommit` (a source-membership check + a destination count against `CLUB_SQUAD_SIZE`) + one
  `RequestRosterCommit` (an id-preserving re-home: the player's record moves to the destination roster under his
  unchanged id, ERR-031-005) + one `TryGetPlayer` landing confirmation: a fixed number of integer ops + one bounded
  roster mutation. *(ERR-031-006: the commit was originally costed as "a squad-slot allocation + a record move +
  the hook dispatch" — with stable ids there is no slot/id allocation and the re-key hook is deleted.)* No allocation in the common path (the `Contract` insert is amortized).
- **`DeriveCliques`-style scans:** none — #31 has no graph derivation.

## 6.3 Player search

Minimal player search over #27's pool is a linear scan of candidate `PlayerRecord`s with the integer valuation
applied — O(pool) per search, on demand, off the match tick; acceptable for a manager-initiated query. A
deep-tier indexed/cached search is a recorded §7 extension, not a minimal concern.

## 6.4 Save cost

Encoding/decoding the transfers sub-blob is O(active contracts + clubs) integer serialization through the
`CanonicalSerializer`, once per save/load — the #40/#33 sub-blob-cost class. No per-tick serialization.

## 6.5 Budget

Off-pitch, at most once-per-command for `SubmitBid` and once-per-save for the codec — nowhere near a per-tick
budget concern. No RNG stream registered at minimal (KD-5), so no stream-advance cost.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §6 (cadence, per-operation cost, player search, save cost, budget). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-4: `SubmitBid` cost corrected to **one** `ApplyTransaction` post (fee-only at minimal; wages deep, FR-TX-005). |
| 0.3 | 2026-10-20 | — | ERR-031-006 (ERR-031-005 back-prop completion): §6.2 `SubmitBid` roster cost is a capacity-only preview + an id-preserving re-home + a `TryGetPlayer` confirmation; the former slot allocation and hook dispatch no longer exist. |
#endregion
