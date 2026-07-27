# News, Inbox & Man-Management #46 — Outline

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Purpose

Spec #46 owns the **inbox** — a persisted, ordered, bounded log of manager-facing items projected from
other specs' events, plus its **read state** — and **man-management**: talk-to-player interactions
producing a bounded morale consequence.

It is deliberately **not** the owner of the events themselves (#30/#31/#35/#44/#45), press-conference
logic (#35), the morale model (#33), rendered text (#49), match statistics (#37), or the rendering of the
inbox (#38).

**Promoted from:** `docs/tracking/news-inbox-man-management-design.md` v0.6 (AR-1 1H+3M+2L → AR-2
0H+3M+1L → AR-3 0H+2M+2L → AR-4 0H+2M → AR-5 0H+0M+3L, CONVERGENCE).

## Section map

| § | Content |
|---|---|
| 1 | Scope, out-of-scope table, dependencies + DAG, KD-1..KD-9, determinism posture, the two verification findings |
| 2 | FR-NW-001..036, data structures, failure modes F1..F9 |
| 3 | FM-NW-01..05 — append, query + lazy retention, read marks, man-management, the delta drain; worked examples |
| 4 | Assembly, file layout, the root projectors and their per-producer sites, the `InboxTextBoundary` adapter, save composition, reference contracts |
| 5 | Test plan — identity / units / determinism / save / read-state / localization / fail-loud / structural |
| 6 | Performance — command-driven and root-projected, no tick slot, no hot path |
| 7 | T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-6 |
| 8 | Cross-references XC-046-001..020, back-prop table |
| 9 | Approval checklist + gates |
| A | Constant catalogue, save layout, the `SourceTag`/`ItemKind`/`InboxIntent` roster + payload schemas |

## Key decisions (summary — full text in §1.5)

- **KD-1** — The inbox is a **persisted item log**, not a derived view. #30 destroys the scoreline the
  moment the table absorbs it, so the most basic item type is **not recomputable** from a save.
- **KD-2** — **#46 references nothing.** Producers are projected in by root-side projectors, each sited
  at **its own producer's already-pinned step**.
- **KD-3** — Man-management routes through **one producer-agnostic `ExternalDeltaPermille`**, summed and
  clamped by the root. **#46 never reads morale** — FR-HS-025 bars the two-way coupling.
- **KD-4** — #49 binding via `InboxTextBoundary`, with **two** identity types (`ItemKind` serialized,
  `InboxIntent` catalogue), both APPEND-only for two distinct reasons.
- **KD-5** — Boundary with #35: **#46 shows, #35 owns.**
- **KD-6** — Persistence: an opaque sub-blob; read state is a **watermark plus a bounded exception set**;
  a query **never mutates**.
- **KD-7** — **No tick slot** — and the argument for lazy retention here is exactly the one #35 could
  *not* make.
- **KD-8** — **Draw-free at every tier**; #16 untouched, and **no reserved value to promote later**.
- **KD-9** — Behaviour-neutral identity, at two distinct scopes.

## Back-props (land atomically at APPROVED)

| ID | Target |
|---|---|
| ERR-033-003 | #33 — the producer-agnostic `ExternalDeltaPermille` (supersedes #35's `ERR-033-001`); filed jointly with #35 |
| ERR-033-004 | #33 §3.3 / FR-HS-024 — *"#46's man-management seam"* **is** the routed field, not a mutator |
| **ERR-030-024** | #30 step 3 — generalize the drain to iterate **every** external-delta producer |
| ERR-030-015 | #30 §3.4 — the **match-item projector** null seam, filed in #46's own right |

**The supplement's proposed `ERR-030-014` was already filed** (it is ERR-030-014, the match-engine
playability defect found at #30's T2); PASS-1 verified this and reassigned it to `-024` (§9.4.1 M-1).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial outline from supplement v0.6. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes: `ERR-030-014` reassigned to `-024` after verification against `spec-error-log.md` found it **already filed**; section map cited `XC-046-001..014`, §8 defines **001..020**. |
#endregion
