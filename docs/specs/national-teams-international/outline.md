# National Teams & International Management #36 — Outline

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Purpose

Spec #36 owns **eligibility and call-up selection** for national teams, the **international-window
schedule**, and the national-team **entrant identities** — plus, at the deep tier, the manager's own
national-team job.

It is deliberately **not** the owner of canonical player records (#27), fixtures/brackets/draws (#43), the
calendar (#30), squad availability filtering (the #30 seam #44 already consumes), fatigue or condition
(#29/#41), or the Stage-5 global sim that populates other nations.

**Promoted from:** `docs/tracking/national-teams-international-design.md` v0.6 (AR-1 1H+2M+1L → AR-2
1H+1L → AR-3 0H+2M → AR-4 0H+2M → AR-5 0H+0M+2L, CONVERGENCE).

## Section map

| § | Content |
|---|---|
| 1 | Scope, out-of-scope table, dependencies + DAG, KD-1..KD-8, determinism posture, the nationality finding |
| 2 | FR-NT-001..034, data structures, failure modes F1..F9 |
| 3 | FM-NT-01..06 — the pin-then-derive nationality, the window derivation, call-up selection, the availability filter, squad resolution, the re-key hook; worked examples |
| 4 | Assembly, file layout, the #30 seams, the composite `ISquadProvider` at the root, save composition, reference contracts |
| 5 | Test plan — the transfer lock, the golden-vector lock, identity / determinism / save / filter composition / fail-loud / structural |
| 6 | Performance — world tick + the resolve seam, no hot path |
| 7 | T0–T3 plan, the Stage-5 gate, the not-planned list, risks R-1..R-6 |
| 8 | Cross-references XC-036-001..018, back-prop table |
| 9 | Approval checklist + gates |
| A | Constant catalogue, save layout, the nation catalogue + id-range table |

## Key decisions (summary — full text in §1.5)

- **KD-1** — Nationality is a **pin-then-derive read**, not a stored field and not a drawn one. #27 is
  untouched: no `PlayerRecord` field, no `RosterGenerator` draw, no `FIELDS_PER_PLAYER` bump, no
  golden-vector rebaseline, **no existing save broken**. The `NationPin` table exists because
  **`PlayerId` re-keys on transfer**.
- **KD-2** — The window is a **read-only calendar derivation**; withdrawal reuses **#44's existing seam**.
  Two filters share it and compose order-independently **because both are removals**.
- **KD-3** — An international tournament **is a #43 instance**. #36 defines no draw — and **does not
  implement `ISquadProvider`**, because that type lives in `match-engine`.
- **KD-4** — Fatigue and minutes travel as **committed values on existing inputs**, deferred until
  minutes exist.
- **KD-5** — The Stage-5 gate cut at **"withdrawal without a match"** — the authorable minimum.
- **KD-6** — Persistence: an opaque, independently version-gated sub-blob carrying the **selection**, not
  the squad.
- **KD-7** — Behaviour-neutral identity, stated honestly (the #44 FR-DC-018 formulation).
- **KD-8** — **Draw-free at every tier #36 owns**; `_RESERVED_0x28_` may stay reserved permanently.

## Back-props

| ID | Target | When |
|---|---|---|
| ERR-030-016 | #30 FR-SN-013 — the multi-consumer filter-seam contract note + the shared empty-squad floor | at APPROVED |

**Nothing is filed against #27, #43, #44 or #16** — and each absence is argued in §8.4 rather than left to
be read as an omission. The #27 one is KD-1's entire point.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial outline from supplement v0.6. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes: **KD-8** promoted from a clause inside KD-3 to a key decision of its own (the draw-free property spans KD-3 *and* KD-5 and is what keeps `_RESERVED_0x28_` unpromoted, so it should not be reachable only through the tournament decision); section map cited `XC-036-001..012`, §8 defines **001..018**. |
#endregion
