# Manager Career, Reputation & Job Market #54 — Outline

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Purpose

Spec #54 owns the **manager entity** and their **tenure** (appointment → employment → termination), the
**career record** and the **reputation** projected from it, the **job market** (vacancies, interest,
offers), and the **unemployed** state that makes all three representable.

It exists because the project has a fully-specified path to *"you are about to be sacked"* and **no
specified behaviour for being sacked**. #45 (APPROVED) states four times — including in the MUST
`FR-BD-012` — that *"#45 supplies confidence; **#30 decides** the sacking"*, and #30's approved section
files contain no sacking, dismissal, or termination text whatsoever. Underneath that, a manager without a
club is **structurally unrepresentable**: `SeasonState`'s constructor throws when `managedClubId` is not
in the club set.

It is deliberately **not** the owner of board confidence (#45), the season objective (#30), the day loop
(#30), club finances/facilities/squads (#40/#53/#27), player morale (#33), or the in-match tactical
`ManagerProfile` (#26 — a different "manager" entirely).

**Promoted from:** `docs/tracking/manager-career-reputation-design.md` v0.4 (AR-1 0H+1M → AR-2 0H+1M →
AR-3 0H+0M+2L, CONVERGENCE).

## Section map

| § | Content |
|---|---|
| 1 | Scope, out-of-scope table, dependencies + DAG, KD-1..KD-8, determinism posture, the two verification findings |
| 2 | FR-MC-001..034, data structures, failure modes F1..F9 |
| 3 | FM-MC-01..05 — tenure evaluation, termination, appointment, the reputation projection, vacancy attractiveness; worked examples |
| 4 | Assembly, file layout, the #30 slot, the command-layer appointment join, save composition, reference contracts |
| 5 | Test plan — the unemployed-save lock, the no-stored-reputation lock, identity / determinism / save / fail-loud / structural |
| 6 | Performance — world tick and season boundary only, no hot path |
| 7 | T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-6 |
| 8 | Cross-references XC-054-001..016, back-prop table |
| 9 | Approval checklist + gates |
| A | Constant catalogue, save layout, the end-reason roster + reputation-term table |

## Key decisions (summary — full text in §1.5)

- **KD-1** — **#54 owns tenure end to end.** #45 keeps confidence, #30 keeps the objective. Splitting the
  rule from its aftermath is exactly what produced an orphaned MUST.
- **KD-2** — Reputation is a **projection over an APPEND-only career record**, never an independent
  scalar — `ERR-030-009`'s lesson applied pre-emptively.
- **KD-3** — A vacancy is a property of a **club**; a rival manager is an entity #54 does not invent.
- **KD-4** — A termination means the career **continues, unemployed** — and the mirror case,
  **appointment**, must not start a career in crisis.
- **KD-5** — The unemployed representation is a **#30 back-prop**, and an **explicit optional** rather
  than a sentinel, so the compiler enumerates every read site.
- **KD-6** — Minimal is **draw-free**; `_RESERVED_0x2E_` / 96 stays **reserved, not promoted**.
- **KD-7** — Persistence: **one APPEND-only career block that outlives the season**.
- **KD-8** — Behaviour-neutral identity, stated with its honest limit: the minimal tier means *"the save
  survives a sacking"*, not *"the player continues after one"*.

## Back-props (land atomically at APPROVED)

| ID | Target |
|---|---|
| ERR-045-002 | #45 — re-point `FR-BD-012`'s counterparty from #30 to #54; confirm the factory-pair insertion is available **mid-career** |
| ERR-030-021 | #30 — the tenure slot **and** `ManagedClubId` as an explicit optional (a `SEASON_STATE_FORMAT_VERSION` bump, recommended to be combined with `ERR-030-009`'s queued one) |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial outline from supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes: **KD-8** promoted from a paragraph inside KD-6 to a key decision of its own, since the identity claim's *limit* (the minimal tier makes a sacking survivable, not recoverable) is the thing most likely to be overstated; section map cited `XC-054-001..010`, §8 defines **001..016**. |
#endregion
