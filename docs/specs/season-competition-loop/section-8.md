# Season & Competition Loop Specification #30 — Section 8: References

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## 8.1 Internal (project) references

| Ref | Anchor | Used for |
|---|---|---|
| Unified Season Save | `docs/tracking/unified-season-save-design.md`; `src/season-save/SeasonSaveCodec.cs`, `SeasonSaveManager.cs`, `SeasonSaveConstants.cs` | the composition root #30 owns/extends; the opaque-sub-blob frame pattern; `SEASON_SAVE_FORMAT_VERSION` |
| Living World #22 | `docs/specs/living-world/section-2.md` (FR-LW-003 / FR-LW-027 / FR-LW-031 / FR-LW-032 / KD-9 / KD-10); `src/living-world/WorldStore.cs`, `WorldLoop.cs` | the day-advance substrate; the phase-1 producer boundary; the ingest-deferral gate |
| Squad/Player Data #27 | `docs/specs/squad-player-data/`; `src/match-engine/ISquadProvider.cs` | the roster world; `ConfigureSquads` / `ISquadProvider` per fixture |
| Match Engine (design note) | `docs/tracking/match-engine-design.md`; `src/match-engine/MatchSaveManager.cs`, `MatchEngine.cs` | plays each fixture; the match blob; the round-trip determinism contract |
| Deterministic Sim #16 | `docs/specs/deterministic-sim/section-3.md` §3.4; `src/deterministic-sim/DeterministicSimConstants.cs`, `SubsystemOrdinals.cs` | the season RNG sub-stream; `DOMAIN_TAG_SEASON_LOOP = 0x22` / `SubsystemOrdinals.SeasonLoop = 84` (back-prop at approval) |
| Event System #17 | `docs/specs/event-system/` | the match goal/card ledger `MatchResult` derives from |
| Code Standards #20 | `docs/specs/code-standards/` | layering, naming, constant tags |
| Management-Layer Roadmap | `docs/tracking/management-layer-spec-roadmap.md` §4/§6 | wave/sequencing; the off-pitch determinism-block reservation (`0x22`/84) |
| Master Development Plan | `docs/planning/master-development-plan.md` §4.1/§4.5 | the Stage-2 single-league scope; career continuity |

## 8.2 External references

The Stage-2 minimal loop is standard league-football scheduling and table logic; no academic citation
is load-bearing.

- **Round-robin tournament / the circle (polygon) method** — the standard rotation algorithm for a
  single round-robin schedule; textbook combinatorics (e.g. de Werra, D. (1981), *Scheduling in
  Sports*, in *Studies on Graphs and Discrete Programming*, North-Holland, pp. 381–395). Cited as the
  algorithm class, not a tuned parameter; verification is the completeness/no-repeat unit tests
  (§5.2), not the citation.
- **Three-points-for-a-win / GD tie-break** — the IFAB/association-football league convention
  (3/1/0; Points → Goal Difference → Goals For), a `[GT]` catalogue value (App. A), designer-tunable
  and not a physical constant.

No `[CITATION-PENDING]` rows: the loop's correctness rests on internal round-trip / completeness
tests, not on an external empirical value.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial references (internal anchors + the circle-method / points-system convention). |
#endregion
