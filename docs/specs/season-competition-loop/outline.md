# Season & Competition Loop Specification #30 — Outline

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 reconciliation, §9.3)
**Version:** 0.2
**Status:** IN REVIEW
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2 (July 22, 2026), AR-1 (1M+3L) → AR-2 converged

---

## Purpose

Defines the **playable career/season spine**: deterministic round-robin fixture generation, a live
league table, a calendar cursor with match-day flow, board objectives / job-security, and
multi-season continuity — the loop that **owns** the `SeasonSaveManager` composition root and drives
it day to day, ticking the world (#22) forward between fixtures. Authored **minimal-first**: the
Stage-2 surface is a single-division round-robin league, deliberately the **identity** #43
(Competition Structure) later generalizes, not a throwaway. Unlike #27, this is a **forward design**
— nothing is built yet; the section text is a specification-before-code plan (the #21–#26 posture).

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope (the playable spine; single-league Stage-2 surface), dependencies, key decisions (KD-1..KD-9), boundary matrix (season loop vs. world vs. match; producer vs. ingest) |
| 2 | Functional requirements (FR-SN-001..034 (+013a/013b, the FR-PO-019a lettered-sub-FR precedent)), data structures (`SeasonState`/`Fixture`/`LeagueTableRow`/`SeasonCalendar`/`BoardState`/`MatchResult`), failure modes F1–F6 |
| 3 | Algorithms: deterministic round-robin (circle method); table update + tie-breaks; calendar cursor + day-advance tick order; season-boundary roll; season-state sub-blob codec; the match-outcome producer payload. Worked examples |
| 4 | Architecture: `TacticalDirector.SeasonSave` extension (the season-loop composition root), assembly placement, the `SeasonSaveCodec`/`SeasonSaveManager` signature change, RNG-stream registration, the #22 producer-not-consumer boundary |
| 5 | Test plan (fixture determinism / table / round-trip / behaviour-neutral floor / mid-sequence restore / two-run season / fail-loud) + FR traceability |
| 6 | Performance: world-tick cadence only (not per-tick, not the 60 Hz hot path); fixture/table sizing |
| 7 | Future extensions / T-phase plan; the #43/#40/#44 generalization seams; #22 ingest deferral |
| 8 | References |
| 9 | Approval checklist |
| App. | Constant catalogue, season-state byte layout, round-robin worked schedule, table tie-break worked example |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial promotion from design supplement v0.2. Forward-design spec (nothing built yet); FR prefix FR-SN; candidate #30. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 reconciliation (whole-round KD-9 command/API rename, living-world-KD disambiguation, KD/FR label fixes). See section-9 §9.3. |
#endregion
