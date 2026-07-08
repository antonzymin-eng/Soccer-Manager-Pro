# Dismarking & Marker-Awareness AI Specification #23 — Section 8: References

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
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
| Wilson, J. — *Inverting the Pyramid: The History of Football Tactics* (Orion, 2008) — marking evolution, man-marking vs zonal context | `[CITATION-PENDING]` edition/ISBN verification per the #11 OI-003 precedent |
| Low, B. et al. — "A systematic review of collective tactical behaviours in football using positional data" (*Sports Medicine*, 2020) — marking/dyad proximity metrics motivating the proximity×dwell form | `[CITATION-PENDING]` DOI verification |

Per the #10/#11 precedent, `[CITATION-PENDING]` rows must be verified (or replaced with verifiable
equivalents) before `APPROVED`; none may be cited as load-bearing for a formula constant — §3's
constants are `[GT]` (designer-tuned), not literature-derived.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial references; two external rows explicitly CITATION-PENDING, none load-bearing. |
#endregion
