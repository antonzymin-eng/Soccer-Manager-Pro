# Positional Rotations Specification #25 — Section 8: References

**Created:** July 8, 2026
**Last Updated:** July 10, 2026 (v0.2)
**Version:** 0.2
**Status:** IN REVIEW

---

## 8.1 Project-internal (authoritative)

| Ref | Content |
|---|---|
| `docs/tracking/advanced-positional-behaviors-design.md` v0.3 | source supplement; KD-4 risk analysis (a)–(c) this spec's KD-3..KD-6 answer |
| Positioning AI #12 §3/§6.1 | `FormationSlotRecord`, `AgentPositioningData.SlotIndex`, `SeedFromFormation`, `ShapeAnalyzer` dwell logic |
| `src/positioning-ai/AgentHysteresisState.cs` | the line/lane dwell pattern §3.2 parallels |
| Tactical Instructions #21 §2.2 / Appendix B | `TeamTactic` field-append rules |
| Deterministic Sim #16 §6.2 / living-world validating-seam precedent | ordinal stability; fail-loud restore gates (the F2 permutation check) |
| Decision Tree #8 audit (ERR-008-002) | away-mirror test motivation (T-RO-I-002) |

## 8.2 Domain literature

| Ref | Status |
|---|---|
| Wilson, J. — *Inverting the Pyramid: The History of Football Tactics* (Orion, hardcover, September 2, 2008, ISBN 978-0-7528-8995-5) — positional interchange history (Total Football to juego de posición) | **VERIFIED** July 10, 2026 (publisher/retail listings; first edition, Orion Books) |
| Low, B., Coutinho, D., Gonçalves, B., Rein, R., Memmert, D. & Sampaio, J. — "A Systematic Review of Collective Tactical Behaviours in Football Using Positional Data", *Sports Medicine* 50, 343–385 (2020), DOI [10.1007/s40279-019-01194-7](https://doi.org/10.1007/s40279-019-01194-7) — the positional-data collective-behaviour measurement literature (inter-player/dyadic coordination metrics) behind detecting positional exchange geometrically, informing the organic-ratification trigger | **VERIFIED** July 10, 2026 (Springer record). **REPLACES** the v0.1 Memmert & Raabe (Routledge, 2018) book row per the #10 OI-003 replace-with-verifiable precedent — that row's ISBN/DOI could not be verified in the authoring environment (verification attempted July 10, 2026; search quota + publisher/Crossref access unavailable), and this peer-reviewed review (Memmert is a co-author) is a verified equivalent for the same motivating claim |

No formula constant is literature-derived; all magnitudes `[GT]`.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial references. |
| 0.2 | 2026-07-10 | — | Wilson row VERIFIED (ISBN 978-0-7528-8995-5); Memmert & Raabe book row REPLACED with the verified Low et al. 2020 *Sports Medicine* review (DOI 10.1007/s40279-019-01194-7) per the OI-003 precedent. §9.1 citation gate closed. |
#endregion
