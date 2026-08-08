# Season & Competition Loop Specification #30 — Outline

**Created:** July 22, 2026
**Last Updated:** August 8, 2026 (v0.3 — balance-pass AR pass 13 L4: the outline stops claiming nothing is built; §2 row F1–F9 + the career types)
**Last Updated (prior):** July 22, 2026 (v0.2 — section-file PASS-1 reconciliation, §9.3)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2 (July 22, 2026), AR-1 (1M+3L) → AR-2 converged

---

## Purpose

Defines the **playable career/season spine**: deterministic round-robin fixture generation, a live
league table, a calendar cursor with match-day flow, board objectives / job-security, and
multi-season continuity — the loop that **owns** the `SeasonSaveManager` composition root and drives
it day to day, ticking the world (#22) forward between fixtures. Authored **minimal-first**: the
Stage-2 surface is a single-division round-robin league, deliberately the **identity** #43
(Competition Structure) later generalizes, not a throwaway. It was authored as a **forward design**
(the #21–#26 posture); the implementation has since landed — T1/T2 and the #29/#41 balance pass all
live in `src/season-save/` (AR pass 13 L4: this paragraph read "nothing is built yet" of a spec
implemented since T0).

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope (the playable spine; single-league Stage-2 surface), dependencies, key decisions (KD-1..KD-9), boundary matrix (season loop vs. world vs. match; producer vs. ingest) |
| 2 | Functional requirements (FR-SN-001..034 (+013a/013b, the FR-PO-019a lettered-sub-FR precedent)), data structures (`SeasonState`/`Fixture`/`LeagueTableRow`/`SeasonCalendar`/`BoardState`/`MatchResult`/`SeasonLoop` + the career pair/`PlayerCareerStates`/`AppearanceState`/`ClubAppearanceStates`), failure modes F1–F9 |
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
| 0.3 | 2026-08-08 | — | **Balance-pass AR pass 13 (L4)**: the sibling-outline sweep (pass 10 L3 fixed #29/#41's) had not reached #30's — "nothing is built yet" of a spec implemented since T0, "F1–F6" after F7/F8/F9 landed, no career types in the §2 row. |
#endregion
