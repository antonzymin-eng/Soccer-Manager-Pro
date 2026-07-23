# Personalities, Morale & Squad Dynamics #33 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-1 fix pass; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.HumanSystems` assembly: value types (`MoraleState`, `PersonalityProfile`,
  `PairwiseRelationship`, `HumanSystemsView`, `MentoringPlan`), the deterministic `AdvanceHumanSystemsDay` /
  `AdvanceRelationshipDay` / `DeriveCliques`, `PairwiseRelationshipStore`, `HumanSystemsConstants`.
  Behaviour-neutral by construction (KD-8).
- **T1** — `HumanSystemsSaveCodec` (`HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` = 1) + composition into #30's season
  save (the `SeasonSaveCodec` sub-blob; #30's composing outer format-version bump coordinated here). Fail-loud
  gates.
- **T2** — Wire `AdvanceHumanSystemsDay` at #30's **pre-declared slot 3** (a *fill* of the FR-SN-034 null
  seam — no #30 spec-text back-prop, contrast #41's ERR-030-002); wire the roster lifecycle
  (regen-insert / retire-remove) at #28's season boundary. **Add the `MemoryStore.SetPlayerEdgeMirror` seam
  to #22** (M3 — none exists today) and the `SeasonSave`-root phase-2 route, **default-empty (no-op,
  `T-LW-U-035` green)**; record the seam + primitive-param shape in #22 (ERR-022-NNN). Expose morale read
  accessors for later consumers. **No RNG stream registered (draw-free).**
- **T3** — Deep tier (each defaulting to its Stage-2 identity via `deepHumanSystemsEnabled`): the **H-Gate
  split** (morale = f(Confidence, SelfEfficacy)); **trait-variety generation** (a keyed draw at roster/regen
  time); **stochastic morale/personality reactions** (the first draw site — promotes
  `DOMAIN_TAG_HUMAN_SYSTEMS = 0x25` / `SubsystemOrdinals.HumanSystems = 87`, spec-text-first, ERR-016, keyed
  on `(playerId, worldDay, purpose)`); **mentoring propagation** + the #34 staff-driven pairing seam;
  **cross-club relationships**; and the **real-canon #22 activation** (flowing a non-empty view — a named,
  separately-reviewed, **non-behaviour-neutral** step per KD-8).

## 7.2 Deferred (recorded, not built)

- **The real-canon #22 activation.** T2 wires the phase-2 seam empty (neutral). Flowing real #33 canon — at
  which point a pairwise edge can cross `600‰`, form a clique, and change #22 arc behaviour — is a named,
  separately-reviewed activation, **not** behaviour-neutral (KD-8). (Also gated on #22's own arc-trigger
  seam — the `world.arcs` stream — being registered, itself a KD-10 dormant item.)
- **A per-entity morale VALUE routed to #22 arc triggers** (XC-022-001) — distinct from the pairwise
  `PlayerEdge` scalar; added when #22's arc triggers wire up to read morale. Not part of the minimal phase-2
  mirror (those triggers are dormant).
- **The H-Gate confidence/self-efficacy split.** Stage-2 morale is one collapsed scalar; the split is a
  deep-tier extension on the same field via the config dial (KD-2).
- **Trait variety + stochastic reactions.** The genuine first draw site (promotes `0x25`/87, KD-6). Traits
  are neutral-seeded at minimal.
- **Cross-club relationships.** Minimal is club-scoped (O(squad²)); cross-club (national team, ex-teammates)
  is an O(global²) deep-tier extension.
- **A dedicated #27 personality-trait append.** Traits live in #33-owned state at minimal; moving them into
  #27's `PlayerRecord` is a recorded deep-tier #27 append (avoids a minimal-tier #27 schema ripple, KD-2).
- **Mentoring propagation + #34 staff pairing.** The ledger structure (`MentoringPlan`) exists today, empty;
  the propagation math and the #34 override producer are deep-tier (KD-5).

## 7.3 Seam contracts recorded for downstream authors

- **#22 (Living World):** #33 is the FR-LW-004 `PlayerEdge` producer (XC-022-002). #22 MUST consume the
  routed scalar **read-only** (mirror via `SetPlayerEdgeMirror`, refuse via `ApplyEvent`), MUST NOT write back
  into #33, and MUST NOT expect a baseline from #33 (its `Affinity`/`Trust` baseline `b` is #22-owned). The
  phase-2 wire-up is a #22 **code** change (one new seam + the route), **no schema/arc-logic change**. The new
  `SetPlayerEdgeMirror` seam validates `value ∈ [0,1]` only; the **SeasonSave root MUST supply player↔player
  ids** (PlayerEdge is valid only on player↔player pairs, `RelationshipLayer.cs`), enforced by the root's F7
  club-universe check — the seam relies on that caller contract, so #22 SHOULD add a defensive player↔player
  assertion when the id-kind predicate exists.
- **#30 (season loop):** owns the slot-3 invocation timing and the routing of #33's view to the `SeasonSave`
  root; #30 stays **producer-only** for #22 (FR-SN-017) and is **not** the #22 router. #33 MUST NOT reference
  #30.
- **The match engine / #27:** morale reaches the match only through the #27 attribute-projection seam,
  read-only; wiring it is the match's own reviewed change (KD-3). #33 MUST NOT gain a match-tick surface.
- **#31/#35/#45 (future):** read-only morale-accessor consumers; MUST NOT write #33 state. **#46** is the sole
  consumer that writes #33 morale (man-management) — the one exception, deferred.
- **#34 (staff, future):** becomes the producer of a non-identity mentoring pairing via the routing seam;
  MUST supply a valid plan (`MentoringPlan.None` is the identity) and MUST NOT add a second mentoring path.
- **#27 (squad/player data):** the `Squad.ClubId` / `PlayerId` enumeration #33 keys per-player state and the
  F7 club-universe check on MUST remain the authoritative identity source; #33 MUST NOT gain a competing
  identity notion.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial T-phase plan (T0–T3) + deferred extensions + downstream seam contracts. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (L): §7.3 records the `SetPlayerEdgeMirror` player↔player caller obligation. |
#endregion
