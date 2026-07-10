# Dismarking & Marker-Awareness AI Specification #23 — Section 8: References

**Created:** July 8, 2026
**Last Updated:** July 10, 2026 (v0.2)
**Version:** 0.2
**Status:** IN REVIEW

---

## 8.1 Project-internal (authoritative)

| Ref | Content |
|---|---|
| `docs/tracking/advanced-positional-behaviors-design.md` v0.3 | source design supplement; KD-2/KD-5 origins |
| Perception System #7 §3.7 | `FilteredView`/`PerceivedAgent` contract (sole opponent-data source) |
| Positioning AI #12 §7.13 + `RestDefenseEvaluator.cs` | the pure-evaluator + phase-gate pattern this spec mirrors |
| Decision Tree #8 §7.7 | awareness-scaled risk-dampening precedent (corrected rest-defense design) |
| Tactical Instructions #21 §2.2 / Appendix B | `TeamTactic` field-append + canonical-order rules |
| Deterministic Sim #16 §6.2 | enum ordinal-stability and snapshot-schema obligations |

## 8.2 Domain literature

| Ref | Status |
|---|---|
| Wilson, J. — *Inverting the Pyramid: The History of Football Tactics* (Orion, hardcover, September 2, 2008, ISBN 978-0-7528-8995-5) — marking evolution, man-marking vs zonal context | **VERIFIED** July 10, 2026 (publisher/retail listings; first edition, Orion Books) |
| Low, B., Coutinho, D., Gonçalves, B., Rein, R., Memmert, D. & Sampaio, J. — "A Systematic Review of Collective Tactical Behaviours in Football Using Positional Data", *Sports Medicine* 50, 343–385 (2020), DOI [10.1007/s40279-019-01194-7](https://doi.org/10.1007/s40279-019-01194-7) — marking/dyad proximity metrics motivating the proximity×dwell form | **VERIFIED** July 10, 2026 (Springer record) |

Both rows verified July 10, 2026 per the #10/#11 OI-003 precedent; neither is load-bearing for a
formula constant — §3's constants are `[GT]` (designer-tuned), not literature-derived.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial references; two external rows explicitly CITATION-PENDING, none load-bearing. |
| 0.2 | 2026-07-10 | — | Both `[CITATION-PENDING]` rows VERIFIED (Wilson ISBN 978-0-7528-8995-5; Low et al. DOI 10.1007/s40279-019-01194-7 with full author list, vol/pages). §9.1 citation gate closed. |
#endregion
