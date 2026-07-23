# Personalities, Morale & Squad Dynamics #33 — Section 1: Introduction & Key Decisions

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.1
**Status:** APPROVED

---

## 1.1 Scope

#33 is the **canonical vol-2 human-systems model** — the authoritative producer of per-player **personality
traits**, **morale/happiness**, squad **relationships / cliques / chemistry**, and **mentoring**, advanced on
the **world tick** (`WorldClock`, one day = one `worldTick`) and exposed as a read-only committed-state
surface. It is the single producer **Living World #22 was built to consume read-only**: #22's `WorldLoop`
phase-2 read and its FR-LW-004 `PlayerEdge` relationship-layer mirror are dormant seams authored (phantom-free)
against exactly this model. #33 realizes the "vol-2/vol-3 impl." prerequisite FR-LW-032 gates #22's Stage-1
activation on, and FR-SN-017 gates #30's match-outcome ingest activation on.

**In scope (Stage-2 minimal):** a per-`PlayerId` `PersonalityProfile` (stable neutral-seeded traits) +
`MoraleState` (a scalar happiness + an internal projection equilibrium); a **club-scoped** pairwise
relationship store (the authoritative vol-2 §2.1 scalar #22 mirrors as `PlayerEdge`); **derived** cliques /
chemistry; an **identity** mentoring surface; the deterministic `AdvanceHumanSystemsDay` world-day step at
#30's pre-declared tick-order slot 3; the read-only committed human-systems view routed into #22 phase-2; and
the `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` season-save sub-blob.

**Out of scope (owned elsewhere, referenced as seams):**
- **#22's interaction / memory / arc layer** — already built over this model. #22 mirrors #33's `PlayerEdge`
  scalar and owns its own `Affinity`/`Trust` layers; #33 **never** reads #22's memory layer (one-way, KD-1).
  The `Affinity`/`Trust` decay baseline `b` (`x' = x + r·(b − x)`, #22 §3.1) is **#22-owned and currently
  deferred** — **not** a #33 responsibility and **not** applied to `PlayerEdge` (which #22 never mutates).
- **Match-tick behaviour** — the match engine reads morale only through the read-only #27 attribute-projection
  seam (KD-3); #33 owns no match-tick logic and makes no match-tick draw.
- **Consumers that do not exist yet** — #31 (negotiation willingness), #35 (media), #45 (board-confidence
  shape reuse), #46 (man-management morale writes), #34 (staff-driven mentoring): #33 exposes the read/route
  seams they attach to and builds none (FR-LW-031).
- **The season day-advance loop** — #30 owns the tick order and *invokes* #33 at slot 3; #33 never references
  #30. The **routing of #33's committed view into #22** is owned by the `TacticalDirector.SeasonSave` root
  (the assembly that already references both `match-engine` and `living-world`), not by #30 (KD-1 / §4).

## 1.2 Dependencies

- **Upstream (needs):** #27 Squad/Player Data (`PlayerRecord`/`PlayerAttributes`, `PlayerId = clubId *
  CLUB_SQUAD_SIZE + localIndex`), #16 Deterministic Simulation (the determinism namespace + the world-tick
  `DeterministicRngService`, consumed only when the deep tier draws), #30 Season & Competition Loop (the
  day-advance loop that invokes #33 at slot 3 and hands its committed view to the root).
- **Downstream (consumers, all deferred):** #22 Living World (the dormant phase-2 read + `PlayerEdge` mirror),
  the match engine (morale via the #27 projection seam), #31/#35/#45/#46/#29.

## 1.3 Key decisions

- **KD-1 (the FR-LW-004 read surface — the headline, matched verbatim).** #33 exposes the vol-2 §2.1 edge as
  a **pure read of exactly one quantity**: a scalar `∈ [0,1]` per **player↔player ordered pair** (clique
  threshold `> 0.6` intact). **#33 supplies no baseline**: #22 never decays `PlayerEdge` toward a baseline (it
  is a read-only mirror #22 re-reads, never evolves), and the `x' = x + r·(b − x)` relaxation that *does* use a
  baseline runs on #22's own `Affinity`/`Trust` with a **#22-owned, deferred** `b`. The `TacticalDirector.
  SeasonSave` root routes the pairwise scalar into #22's phase-2 as **primitive arrays** (no assembly
  reference either way — the #23 `MarkingPressureEvaluator` primitive-span precedent). #22 **refuses** all
  `PlayerEdge` writes via `ApplyEvent` and owns only `Affinity`/`Trust`. **One new mirror-write seam is
  required:** no `MemoryStore` method sets `PlayerEdge` on a *live* edge today (`GetOrCreateEdge` no-ops on an
  existing edge, `InsertEdge` throws on one, `ApplyEvent` refuses), so the T-phase adds a small public
  `MemoryStore.SetPlayerEdgeMirror(fromId, toId, value)` — a #22 **code** addition with **no schema change and
  no arc-logic change**. `T-LW-U-035` (PlayerEdge bit-unchanged under an *owned-layer* update) stays green;
  the phase-2 wire-up accepts an **empty view = byte-identical #22**, and flowing a real #33 view is a
  deliberate, separately-reviewed activation (KD-8; the #21 `SetTeamTactic` wire-neutral pattern).

- **KD-2 (minimal trait vector + collapsed-scalar morale).** Minimal traits = a small **stable** vector
  (Professionalism / Ambition / Loyalty / Temperament / Determination, `byte[1,20]` on the #27 posture),
  **neutral-seeded** (10) — variety is a deep-tier generation draw (the #27 T0→T1 all-neutral-first
  precedent). Morale is a **single scalar per-mille** at Stage 2 (the H-Gate collapsed); the deep tier splits
  it into Confidence/SelfEfficacy on the same field via a config dial. Traits live in **#33's own** per-player
  state, **not** appended to #27's `PlayerRecord` at minimal (they are genuinely new, non-derivable from
  football attributes); a #27 append is a **recorded deep-tier option, not built** (avoids a #27 schema
  ripple).

- **KD-3 (morale → consumers — read-only projection OUT, deferred).** Morale reaches the match engine **only**
  through the #27 attribute-projection seam, **read-only**; #33 owns no match-tick write and makes no
  match-tick draw. The direction is defined (out of #33), consumption **deferred** (wiring morale into the
  match projection is its own reviewed change — the #27 T1 "not behaviour-neutral" precedent). #31/#35/#45
  read morale accessors when they exist; **#46 is the sole consumer that writes #33 morale** — all deferred
  (FR-LW-031). **No two-way coupling.**

- **KD-4 (cliques/chemistry — derived, no double-truth).** Cliques/chemistry are a **derived read** over the
  #33-owned pairwise relationship scalar (a group where pairwise `> 600‰` = #22's `0.6`), **not** independent
  persisted state. The **one truth** is #33's pairwise scalar; #22's `PlayerEdge` is a **mirror** and cliques
  are a **derived view** — nothing independent is persisted, so there is nothing to diverge.

- **KD-5 (mentoring — identity routing seam).** Minimal mentoring is the **empty identity** (`MentoringPlan.
  None`). The deep tier adds daily trait/morale propagation between paired players; the pairing lifecycle is a
  **#34 staff-driven producer** via an identity routing seam (default = #33's auto-derivation), the #41
  `MedicalModifier` / #29 `CoachingModifier` pattern — no #34 interface built. #33 owns the lifecycle+default;
  #34 becomes the override producer.

- **KD-6 (determinism — draw-free minimal, single world clock).** Minimal makes **no stochastic draw**;
  projections are deterministic integer per-mille functions of committed inputs. `0x25`/87 **stays
  `_RESERVED_0x25_`** at approval (no `DOMAIN_TAG_HUMAN_SYSTEMS` promotion, **no #16 spec-text change** — the
  #40 KD-2 reserved-not-promoted precedent); it promotes at the deep tier's first draw (keyed on `(playerId,
  worldDay, purpose)`, the #41/#28/#30 off-pitch precedent). One clock (world), so no cross-clock
  determinism-ordering fragility. `0x25`/87 confirmed free and contiguous (the placeholder row exists;
  off-pitch band ordinals 80–92 contiguous).

- **KD-7 (persistence — season-save sub-blob; supersedes the plan's `WORLD_STORE_FORMAT_VERSION`).**
  `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob composed into `SeasonSaveCodec`, **not** a
  `WORLD_STORE_FORMAT_VERSION` bump (§4). The #22 `PlayerEdge` mirror stays **#22's own** serialized state
  (unchanged) — #33 supplies the value #22 already serializes; the root refreshes the mirror onto live edges
  (via `SetPlayerEdgeMirror`) before phase 2, so mirror ≡ authoritative at every save point. Fail-loud gates;
  serialize-don't-regenerate; roster-lifecycle in lockstep with #28.

- **KD-8 (behaviour-neutral identity + stream independence, wire/flow boundary explicit).** Neutral in three
  senses at minimal: (a) registering **no** stream leaves every existing cursor byte-identical (trivially);
  (b) the #22 phase-2 wire-up is fed an **empty view** ⇒ #22 output byte-identical (`T-LW-U-035` green); (c)
  **no consumer is wired** — morale drifts internally with no reader. **Wiring** the seam (empty view) is
  neutral and is what #33 minimal ships; **flowing real #33 canon** into #22 (a pairwise edge can legitimately
  cross `600‰`, form a clique, change #22 arc behaviour) is a **named, separately-reviewed activation, NOT
  behaviour-neutral by design** — the intended payoff, not a violation.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §1 from the converged design supplement (v0.2). Status IN REVIEW. |
#endregion
