# Personalities, Morale & Squad Dynamics #33 — Outline

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial authoring from the converged design supplement)
**Version:** 0.1
**Status:** APPROVED
**Source:** `docs/tracking/personalities-morale-dynamics-design.md` v0.2
**FR prefix:** FR-HS · **Wave:** 3 (GATING) · **Master-plan home:** §5 Stage 4 / Master Vol 2

---

## Purpose

The canonical **vol-2 human-systems substrate**: per-player **personality traits**, a **morale/happiness**
model (the H-Gate confidence-vs-self-efficacy shape), squad **relationships / cliques / chemistry**, and
**mentoring** — advanced on the **world tick** (`WorldClock`, one day = one `worldTick` — never the 10 Hz/60 Hz
match loops) and exposed as an authoritative, **read-only committed-state surface**. This is **the single
producer Living World #22 was built to consume read-only** (its dormant `WorldLoop` phase-2 read + the
FR-LW-004 `PlayerEdge` relationship-layer mirror). Landing #33 **wires those dormant seams phantom-free**; the
minimal tier is deterministic and draw-free, and its one #22-facing read surface is **exactly** the pairwise
`PlayerEdge` scalar — no baseline, no over-exposure.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope, dependencies, key decisions KD-1..KD-8 |
| 2 | Functional requirements FR-HS-001..028, data structures, failure modes F1..F7 |
| 3 | Algorithms — `AdvanceHumanSystemsDay` (deterministic morale/relationship projection), clique derivation, the KD-1 read-surface assembly, a worked example |
| 4 | Architecture, assembly, file layout, reference direction, the #22 route + the new `SetPlayerEdgeMirror` seam |
| 5 | Test plan (T-HS-*) + FR traceability |
| 6 | Performance / off-pitch world-tick cadence |
| 7 | Future extensions, T-phase plan T0–T3, the #22/#27/#31/#34/#46 deferred seams |
| 8 | References + cross-references (XC-033-*; XC-022-002 producer side) |
| 9 | Approval checklist |
| Appendices | Constant catalogue + worked examples |

## Key decisions (summary; full text in §1)

- **KD-1 (headline)** #33's #22 read surface is **exactly one quantity** — the pairwise `PlayerEdge` scalar
  `∈ [0,1]` per player↔player ordered pair (clique threshold `> 0.6` intact). #33 supplies **no baseline**
  (#22 never decays `PlayerEdge`; the `x' = x + r·(b − x)` relaxation is on #22's own `Affinity`/`Trust` with
  a #22-owned `b`). One-directional: #33 writes canon, #22 reads a mirror, #33 never reads #22. The mirror
  write needs **one new** `MemoryStore.SetPlayerEdgeMirror` seam (no `MemoryStore` method sets `PlayerEdge`
  on a live edge today) — a #22 **code** addition, **no schema/arc-logic change**; `T-LW-U-035` stays green.
- **KD-2** Minimal = a small **stable neutral-seeded** trait vector + a **scalar** morale (H-Gate collapsed);
  the deep tier splits morale into Confidence/SelfEfficacy on the same field via a config dial. Traits live in
  #33-owned state, **not** appended to #27's `PlayerRecord` at minimal (a recorded deep-tier option).
- **KD-3** Morale → consumers is a **read-only projection OUT** (match via the #27 projection seam; #31/#35/
  #45 read; **#46 is the only writer**) — all deferred, no two-way coupling.
- **KD-4** Cliques/chemistry are a **derived read** over the one #33-owned pairwise scalar (`> 600‰` = `0.6`),
  **not** independent persisted state — no double-truth against #22's edge store.
- **KD-5** Mentoring is the **empty identity** at minimal; #34 staff-driven pairing is a deep-tier routing
  seam (default = #33 auto-derivation), no #34 interface built.
- **KD-6** Determinism: **draw-free minimal** ⇒ `0x25`/87 stays `_RESERVED_0x25_` (no #16 change at
  approval, the #40 precedent); promotes to a live tag + stream at the deep tier's first keyed draw.
- **KD-7** Persistence is an opaque `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` sub-blob under #30's season save —
  **not** a `WORLD_STORE_FORMAT_VERSION` bump; the #22 `PlayerEdge` mirror stays #22's own serialized state.
- **KD-8** Behaviour-neutral: stream independence (no stream at minimal), the #22 view fed **empty** ⇒
  #22 byte-identical (`T-LW-U-035` green), no consumer wired. Flowing real canon is a **named, separately-
  reviewed activation**, not behaviour-neutral by design.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial outline from the converged design supplement (v0.2, AR-1 folded). Status IN REVIEW. |
#endregion
