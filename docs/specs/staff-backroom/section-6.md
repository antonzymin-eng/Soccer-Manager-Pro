# Staff & Backroom #34 — Section 6: Performance

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 6.1 Cadence

#34 has **no per-tick match-loop cost** — it is off-pitch, on the world tick only (KD-4). At the scaffold the
world-tick staff slot is a **null seam** (candidate-pool/hiring are deep), so a day with no manager action
costs **zero** #34 tick work. The projections are **pull-based**: the composition root evaluates
`ToMedicalModifier`/`ToCoachingModifier` for the managed club's role slots once per consuming day-step when
building #29's and #41's inputs — a fixed, tiny amount of integer work.

## 6.2 Per-operation cost

- **`ToMedicalModifier` / `ToCoachingModifier` / `ToStaffMult` / `ToScoutQuality`** — a fixed number of
  per-mille integer multiply/adds over a role-slot-holder's `StaffAttributes`: no allocation, no RNG.
- **`EvaluateStaffOffer`** (deep) — one integer comparison.
- **`HireStaff`** (deep) — the validate-all-first gate chain (candidate check, wage-well-formed, accept,
  affordability read from #40, free role slot) + **one** `ApplyTransaction` post + one role-slot assignment: a
  fixed number of integer ops + one bounded store mutation. No allocation in the common path.
- **Candidate-pool generation** (deep) — bounded by the pool size, on demand, off the match tick; keyed
  draws (no cursor).

## 6.3 Save cost

Encoding/decoding the staff sub-blob is O(role slots + candidates) integer serialization through the
`CanonicalSerializer`, once per save/load — the #41/#33 sub-blob-cost class. No per-tick serialization.

## 6.4 Budget

Off-pitch, at most once-per-consuming-day for the projections and once-per-command for `HireStaff` — nowhere
near a per-tick budget concern. **No RNG stream registered at the scaffold** (KD-4), so no stream-advance
cost; the deep candidate-pool draws are the only stochastic cost and are bounded per generation.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §6 (cadence, per-operation cost, save cost, budget), promoted from design supplement v0.4. Status IN REVIEW. |
#endregion
