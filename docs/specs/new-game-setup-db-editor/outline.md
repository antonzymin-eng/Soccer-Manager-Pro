# New-Game Setup & Database Editor #47 — Outline

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Purpose

Spec #47 owns the **new-game setup flow** (start point, league/club selection, world seed) and the
**authoring surface** over #27's data format — plus the **authored-database artifact** and its identity.

It is deliberately **not** the owner of the roster/attribute model (#27), generation from a seed
(`LeagueBootstrap` / `RosterGenerator`), the season loop (#30), competition instance definitions (#43),
the UI shell that hosts the editor (#38), or live-save migration (#50).

**The plan this spec came from was wrong on its central claim**, and the reason is architectural rather
than clerical: it stated the editor *"adds no new save block"*. That holds for a **generated** game and
fails for an **authored** one, because this project does not save rosters — it **regenerates them from the
world seed**. A player the user edits is, by construction, no longer a function of the seed.

**Promoted from:** `docs/tracking/new-game-setup-db-editor-design.md` v0.6 (AR-1 0H+2M → AR-2 0H+2M+1L →
AR-3 0H+2M+1L → AR-4 0H+1M+1L → AR-5 0H+0M+2L, CONVERGENCE).

## Section map

| § | Content |
|---|---|
| 1 | Scope, out-of-scope table, dependencies + DAG, KD-1..KD-7, determinism posture, the two verification findings |
| 2 | FR-ED-001..032, data structures, failure modes F1..F8 |
| 3 | FM-ED-01..04 — the writer and its round-trip contract, the setup handoff, authored-`League` construction, the pin-precedence rule; worked examples |
| 4 | Assembly, file layout, the #38 hosting split, the root's two construction paths, save composition, reference contracts |
| 5 | Test plan — the round-trip lock, the generated-identity lock, save/restore, structural, fail-loud |
| 6 | Performance — authoring is human-cadence; the save-size consequence is the real cost |
| 7 | T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-6 |
| 8 | Cross-references XC-047-001..018, back-prop table |
| 9 | Approval checklist + gates |
| A | Constant catalogue, save layout, the authored-vs-generated comparison table |

## Key decisions (summary — full text in §1.5)

- **KD-1** — An authored database is a **source for `League`**, never a patch over the generator — **and
  an authored game saves its rosters**, because they are not derivable from any seed. A **generated** game
  writes nothing and stays byte-identical.
- **KD-2** — **#27's loader is the single validation authority**; the new writer's correctness condition
  is `Parse(Write(s)) == s`.
- **KD-3** — Minimal is **generated-world setup only**; #47 adds **no gate of its own**.
- **KD-4** — The editor is a **#38-hosted mode over #47's own non-UI data layer**.
- **KD-5** — Handoff is a **value artifact**; #47 never references #30.
- **KD-6** — Tooling: no stream, no tag, **no `_RESERVED_` row**. Authored names travel through #49's seam
  as **slot values**, satisfying FR-LC-001 by routing rather than by translating.
- **KD-7** — Behaviour-neutral identity: #47's entire save-format footprint is **conditional on the user
  having authored something**.

## Back-props (land atomically at APPROVED)

| ID | Target |
|---|---|
| ERR-030-017 | #30 / the season-save composition — an **optional** `AUTHORED_DB_SAVE_FORMAT_VERSION` sub-blob, present only for an authored game |
| ERR-030-018 | `season-save` / `League` — an **authored-source factory** (`League`'s constructor is `internal` there, so this cannot be a #47 change) |

**Nothing is filed against #27, #36, #43, #16 or #50** — each absence is argued in §8.4. #47 does,
however, **answer the precedence question #36 left open** (an authored pin is overwritten by a later
re-key pin).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial outline from supplement v0.6. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes: **KD-7** promoted from a paragraph inside KD-1 to a key decision of its own — the *conditionality* of #47's save footprint is the claim a reviewer checks first, and it should not be reachable only through the authored-database decision; section map cited `XC-047-001..012`, §8 defines **001..018**. |
#endregion
