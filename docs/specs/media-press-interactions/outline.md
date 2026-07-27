# Media & Press Interactions #35 — Outline

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## Purpose

Spec #35 owns the **press-conference lifecycle**: which conference is queued by which season event, the
**question archetype** selected for it, the bounded **answer set** offered, and the **committed
consequence value** an answer produces.

It is deliberately **not** the owner of rendered text (#49), the template corpus (#49), the morale model
(#33), man-management writes (#46), board confidence (#45), the inbox that surfaces media items (#46), or
any reputation scalar (nobody — and #35 declines to invent one).

**Promoted from:** `docs/tracking/media-press-interactions-design.md` v0.7 (AR-1 2H+4M+5L → AR-2 0H+3M+1L
→ AR-3 0H+2M+3L → AR-4 0H+2M → AR-5 0H+0M+3L, CONVERGENCE; v0.7 folds in the #46 coordination).

## Section map

| § | Content |
|---|---|
| 1 | Scope, out-of-scope table, dependencies + DAG, KD-1..KD-10, determinism posture, the #49/#33 re-basing |
| 2 | FR-ME-001..038, data structures, failure modes F1..F9 |
| 3 | FM-ME-01..06 — queue, answer, expiry, delta drain, the keyed selection value, the deferred deep draw; worked examples |
| 4 | Assembly, file layout, the `MediaTextBoundary` sibling adapter, the two #30 seams, save composition, reference contracts |
| 5 | Test plan — identity / units / determinism / save / seams / localization compliance / fail-loud / structural |
| 6 | Performance — world tick + post-round only, no hot path |
| 7 | T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-7 |
| 8 | Cross-references XC-035-001..022, the prerequisite, the back-prop table |
| 9 | Approval checklist + gates |
| A | Constant catalogue, save layout, the `MediaIntent` roster + ordinal-band table |

## Key decisions (summary — full text in §1.5)

- **KD-1** — #35 is a **#49 producer**, not a #22 consumer. It emits a `MediaIntent`, disjoint native
  slots, and a `ulong`; a sibling `MediaTextBoundary` adapter renders. **Two rosters, one enum**:
  question archetypes *and* answer-option phrasings, split by a `[FIXED]` ordinal band.
- **KD-2** — **Draw-free minimal** ⇒ `_RESERVED_0x27_` stays reserved; the FR-LC-004 `ulong` is a local
  keyed SplitMix64 mix, conditional on ERR-049-001.
- **KD-3** — The consequence is a **committed value routed through #30** into #33's own day step —
  never a morale write, which FR-HS-002/024 forbid.
- **KD-4** — #35 introduces **no** reputation scalar. There is nothing upstream to reuse, and inventing
  one inside a press spec would make it five specs' truth by accident.
- **KD-5** — **#30 queues; the manager's answer is a command.** The tick seam does one thing: expiry.
- **KD-6** — **#46 discovers by reading #35.** Strictly one-directional; #35 never references an inbox.
- **KD-7** — Persistence is an opaque, independently version-gated sub-blob carrying the
  **undelivered-delta invariant**.
- **KD-8** — Consequence scope is one code path at both tiers; minimal ships **zero or one** entry.
- **KD-9** — Behaviour-neutral identity, stated precisely: nothing queued, **or** every consequence `0`.
- **KD-10** — **The `MediaIntent` ordinal is doubly load-bearing** — serialized *and* the catalogue key —
  so APPEND-only is a save-correctness contract, not a style rule.

## Prerequisite and back-props

| ID | Target | When |
|---|---|---|
| **ERR-030-022** | #30 §3.3 + FR-SN-034 — the tick-order reconciliation (§8.0) | **before or with** promotion |
| ERR-049-001 | #49 FR-LC-020 — generalize `SelectionDraw` provenance | at APPROVED |
| ERR-033-003 | #33 — the producer-agnostic `ExternalDeltaPermille` (supersedes the v0.6 `ERR-033-001`) | at APPROVED, jointly with #46 |
| ERR-033-002 | #33 FR-HS-027 — routed-input drop-on-departure | at APPROVED |
| **ERR-030-023** | #30 — the **two** media seams (queue at §3.4, drain at step 3) | at APPROVED |

**The supplement's proposed `ERR-030-012` / `-013` were both already filed** by #30's own T2
implementation on the same day the supplement was written; PASS-1 verified this and reassigned them to
`-022` / `-023` (§9.4.1 M-1).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial outline from supplement v0.7. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes: `ERR-030-012`/`-013` reassigned to `-022`/`-023` after verification against `spec-error-log.md` found both **already filed** by #30's T2 landing; **KD-10** promoted from a paragraph inside KD-1 to a key decision in its own right (the ordinal contract is a save-correctness property, and burying it under the text-seam decision is how it gets missed); section map cited `XC-035-001..016`, §8 defines **001..022**. |
#endregion
