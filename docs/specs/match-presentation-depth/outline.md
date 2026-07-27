# Match Presentation Depth #48 — Outline

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Purpose

Spec #48 owns the **mapping** from observed match state and emitted events to presentation output —
commentary line selection, animation/render state, and audio **cue selection**.

It is deliberately **not** the owner of the simulation or its ledger (match-engine / #17), rendered text
(#49), audio playback and the cue catalogue (#51), the UI framework (#38), match statistics (#37), any
gameplay outcome, or **the content itself** — the animation clips, the audio assets, the commentary
corpus. #48 specifies **triggers, identities and contracts**; specifying *when* a line fires is not
specifying *the line*, and the asset surface dwarfs the logic here.

**Promoted from:** `docs/tracking/match-presentation-depth-design.md` v0.6 (AR-1 0H+3M → AR-2 0H+1M+1L →
AR-3 0H+1M+2L → AR-4 0H+1M+1L → AR-5 0H+0M+2L, CONVERGENCE).

## Section map

| § | Content |
|---|---|
| 1 | Scope, out-of-scope table, dependencies + DAG, KD-1..KD-7, determinism posture, the three verification findings |
| 2 | FR-MP-001..034, data structures, failure modes F1..F7 |
| 3 | FM-MP-01..04 — the live capture, the keyed selection mix, the animation derivation, cue mapping; worked examples |
| 4 | Assembly, file layout, the shared tap, the #38 hosting and thread boundary, the two inverted seams |
| 5 | Test plan — unconditional observer neutrality, layer-taxonomy locks, transcript determinism, both replay paths, thread boundary |
| 6 | Performance — per-tick capture on the tick thread; the one real budget |
| 7 | T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-6 |
| 8 | Cross-references XC-048-001..018; **no back-props at approval** |
| 9 | Approval checklist + gates |
| A | Constant catalogue, the intent/cue rosters, the observation-surface inventory |

## Key decisions (summary — full text in §1.5)

- **KD-1** — **Observation-only, enforced structurally** — including the specific rule that #48 must not
  reference `match-client-core`, whose `ILiveMatchMutations` sits in the same layer.
- **KD-2** — Commentary is **live-captured** (there is no post-match ledger reader), **display-only and
  draw-free**, and a **#49 producer**. The exported HTML replay **embeds rendered text**.
- **KD-3** — Animation needs **no new engine field**, and the burden of proof sits on anyone who says
  otherwise.
- **KD-4** — Audio is **cue selection**; #48 declares `ICueSink` and the **shell** implements it, so #51
  never references #48.
- **KD-5** — #48 composes as a **sibling of `match-viewer`**, hosted by #38 — with a **bounded by-value
  window** and a **snapshot-copy at the thread boundary**.
- **KD-6** — Presentation/infra: no stream, no tag, **no `_RESERVED_` row**, nothing to promote later.
- **KD-7** — Identity that is unusual in this project: not *"neutral when off"* but **"neutral when
  on"**.

## Back-props

**None at approval.** #48 is a pure consumer of surfaces that already exist or are already specified —
the observation surface, the live tick tap, #38's view-model contract, and #49's adapter extension point.
The same positive property #37, #44 and #46 have, and worth stating because a presentation spec is exactly
where *"just add a field to the engine for rendering"* pressure lands.

It **inherits** #35's `ERR-049-001` as the **third** spec blocked on that one #49 wording fix, and files
no duplicate.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial outline from supplement v0.6. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes: **KD-7** promoted from a paragraph inside KD-2 to a key decision of its own — *"neutral when on"* is the claim that distinguishes #48 from every sibling and should not be reachable only through the commentary decision; section map cited `XC-048-001..010`, §8 defines **001..018**. |
#endregion
