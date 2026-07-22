# Squad / Player Data Layer Specification #27 — Section 8: References

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

This is a data-layer spec: attribute ranges, roster generation, and text import. It carries **no
empirical formulas**, so §8 has no academic-literature rows — all references are project-internal
and authoritative. Nothing below is a load-bearing constant source (all constants are `[FIXED]`/
`[DERIVED]`/`[GT]`/`[CROSS]` per Appendix A).

## 8.1 Project-internal (authoritative)

| Ref | Content |
|---|---|
| `docs/planning/master-development-plan.md` §4.2 (Squad Management) | places a player database at Stage 2; the V1 attribute list this record reconciles; §4.3 (Transfer System) / §4.4 (aging/training) = the out-of-scope Stage-1+ deferrals (§0 / §7.2) |
| `docs/tracking/spec-error-log.md` `ERR-007` | `KickPower`/`WeakFootRating`/`Crossing` absent from `PlayerAttributes` — the gap the canonical record closes for real (§7.1 T2, FR-SQ-023) |
| `docs/tracking/spec-error-log.md` `ERR-008-006` | DT #8 documented-open reserved-field / unconsumed-`Crossing` precedent — the forward-declaration pattern FR-SQ-002's reserved rows follow |
| Deterministic Sim #16 §3.4 | `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` + `SubsystemOrdinals.PlayerDatabase = 81` allocation (off-pitch 80–99 band); ERR-022-001 (Living World `0x1E`/`80`) is the back-prop precedent for this allocation (KD-5) |
| Deterministic Sim #16 §6.2 | enum ordinal-stability + snapshot-schema obligations governing `PlayerPosition`/`AttrIdx`/`NameCatalogue` (FR-SQ-006/007/017/020) |

## 8.2 Sibling design supplements (design history)

| Ref | Content |
|---|---|
| `docs/tracking/squad-player-data-design.md` (v0.7) | source design supplement; KD-1..KD-8, T-phase plan; promoted to this spec set |
| `docs/tracking/player-attribute-projection-design.md` (v0.4) | T1/T2 field-by-field projection + `ConfigureSquads` mapping (§7.1) |
| `docs/tracking/squad-roster-reference-design.md` (v0.2) | T3 snapshot roster-reference field (§7.1) |
| `docs/tracking/lineup-selection-design.md` (v1.0) | proper per-line lineup selection (§7.1) |

## 8.3 T0 precedent specs (Stage-1-forward-pull pattern)

Tactical Instructions #21, Living World #22, and the advanced-behaviour set #23–#26 — each a
Stage-1-forward pull promoted design-supplement-first, the precedent this spec (and its
design-supplement-first promotion) follows.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial references; internal-only (data-layer spec, no empirical citations). |
#endregion
