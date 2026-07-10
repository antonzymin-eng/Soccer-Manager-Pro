# Scripted Build-Up Structures Specification #24 — Section 8: References

**Created:** July 8, 2026
**Last Updated:** July 10, 2026 (v0.2)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 Project-internal (authoritative)

| Ref | Content |
|---|---|
| `docs/tracking/advanced-positional-behaviors-design.md` v0.3 | source supplement; KD-3 seam analysis (`TransitionWon` free-seam verification) |
| Positioning AI #12 §3/§6.1 | `SlotComposer` pipeline, formation tables, `LineId`/`LaneId`, team-relative frame |
| Tactical Instructions #21 §2.2/§3.4 (FR-TI-020) | `TransitionPlan` semantics (`TransitionWon` = plan on winning the ball) |
| Match Engine design note + #17 §3.10 | possession-changed producer/consumer (the only wired event producer, verified July 7, 2026) |
| Decision Tree #8 audit (ERR-008-002) | the home/away mirror bug class motivating T-BU-I-002 |
| Deterministic Sim #16 §6.2 | ordinal stability + snapshot obligations |

## 8.2 Domain literature

| Ref | Status |
|---|---|
| Wilson, J. — *Inverting the Pyramid: The History of Football Tactics* (Orion, hardcover, September 2, 2008, ISBN 978-0-7528-8995-5) — back-three build-up and positional-structure history | **VERIFIED** July 10, 2026 (publisher/retail listings; first edition, Orion Books) |
| Spielverlagerung web analyses of build-up structures (juego de posición / salida lavolpiana) | **RECLASSIFIED — informal background, non-citable** (July 10, 2026, per this row's own v0.1 resolution path and the #10 OI-003 precedent: web-published analysis with no stable citation form; carries no normative weight and is not consulted for any constant or FR) |

No formula constant is literature-derived; all magnitudes are `[GT]`.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial references. |
| 0.2 | 2026-07-10 | — | Wilson row VERIFIED (ISBN 978-0-7528-8995-5); Spielverlagerung row reclassified informal background per its own resolution path. §9.1 citation gate closed. |
#endregion
