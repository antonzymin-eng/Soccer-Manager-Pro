# Scouting & Player Knowledge #32 — Section 1: Introduction

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.2 — section-file AR PASS-2; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 1.1 Scope

**Player knowledge as a per-manager VIEW**: scout assignments, **attribute masking / fog-of-war**
(the manager sees attribute *ranges*, not truths, until a player is scouted), scout reports, and
recommendations. All #32 state advances on the **world tick** (`WorldClock`, one day = one
`worldTick` — never the 10 Hz/60 Hz match loops) and persists alongside **#30's season/career save**.

**The governing invariant (roadmap §5):** knowledge is a **per-manager VIEW over #27's true
attributes and is NEVER a mutation of them.** #32 stores a knowledge overlay; the canonical
`PlayerAttributes` are untouched. Every decision below is downstream of that invariant.

**Minimal identity (always present, behaviour-neutral)** = fog **off** (`fogEnabled = false`): every
knowledge read resolves at the maximal band (`KNOWLEDGE_BAND_MAX`), whose error half-width is `0`, so
the view returns `[truth, truth]` for all 31 attributes — the **omniscient view**. The overlay stores
nothing, no assignment exists, #30's #32 tick slot is empty, and **no RNG call is made**, so a season
with #32 landed is **byte-identical to pre-#32**; the only new artifact is #32's own (nearly empty)
save sub-blob (the #34 scaffold posture).

**Stage-3 deep (`fogEnabled = true`)** = an **external** unscouted player resolves at band 0 (widest
ranges); the **managed club's own players always resolve at `KNOWLEDGE_BAND_MAX`** (own-squad
omniscience, KD-2). `AssignScout` (a manager command) starts an assignment; progress accrues on the
world tick at #30's slot (days-per-band scaled by #34 scout quality, KD-4); each band-up stores the
advanced band and stamps a report — the next view read derives the narrower estimate. At
`KNOWLEDGE_BAND_MAX` the estimate **collapses back to the exact-value identity** — the same code path
the minimal tier short-circuits through, not a second one (KD-8).

## 1.2 Out of scope (owned elsewhere, referenced as seams)

- **The truth (#27 Squad/Player Data).** #27 owns `PlayerRecord`/`PlayerAttributes`/`Squad`. #32
  reads them **read-only**; the view is a pure function over caller-supplied truth records. #32
  MUST NOT write any #27 state (FR-SC-001).
- **The scouts themselves (#34 Staff & Backroom).** Scouts are staff; #34 owns `StaffRecord` and
  publishes `ToScoutQuality(in StaffRecord) → int` from the ChiefScout role slot (#34 §3.1,
  XC-034-008). #32 consumes it read-only (deep) and **defines the baseline**
  (`SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000`), closing #34's open constant. No staff modelling here.
- **The transfer decision (#31 Transfers, Contracts & Negotiation).** #31 owns bids, valuation, and
  the negotiation seam. #32 produces the **knowledge the manager's decision is based on**; the action
  stays #31's `SubmitBid`, unchanged. #32 itself issues no offers at Stage 3 (KD-5) — the FR-TX-010
  seam-reuse expectation transfers to the far-deep autonomous-AI-scouting tier.
- **Development potential (#28 Player Progression).** CA/PA live in #28. Stage-3 fog covers the 31
  canonical **attributes** only; a potential-estimate surface is a §7 extension reading #28's career
  state read-only.
- **The report-rendering UI (#38 UI/Client Framework).** #38 renders ranges/reports as view models
  (FR-UI-002 immutable read-only projections — exactly the shape #32's view exposes). Deferred
  consumer; no interface built (FR-LW-031).
- **Report prose / localization (#49, #22).** #32 emits **structured** reports only (ids, bands,
  integer estimates) — **no display text**. Prose is a presentation concern behind #49's
  localize-after-generate boundary; the plan's floated `InteractionTextGenerator` (#22) consumption
  is **rejected** (it would couple #32 to the living-world assembly and spend a `world.text` draw on
  a presentation artifact). #32 never references #22.
- **The season loop + save codec + tick order (#30).** #30 owns `RunWorldTickInFixedOrder`,
  `SeasonSaveCodec`, and the outer `SEASON_SAVE_FORMAT_VERSION`; it invokes #32 at a new
  pre-declared tick-order slot (ERR-030-007) and composes #32's opaque save sub-blob. #32 never
  references #30.

## 1.3 Dependencies

**Upstream (needs):** #27 (truth records, read-only — the minimal subset), #34 (`ToScoutQuality`,
deep), #30 (day-advance loop + season-save root, via the composition root), #16 (determinism
namespace; the world-tick `DeterministicRngService` for deep keyed draws).

**Downstream (consumers, deferred — no interface built, FR-LW-031):** #38 (renders ranges/reports),
#46 (news/inbox aggregation of report events), #42 (youth scouting reuses the knowledge-overlay
pattern), and the far-deep autonomous-AI recruitment tier (AI managers scouting under fog — where
the FR-TX-010 negotiation-seam reuse becomes live).

Reference DAG: `compositionRoot → {#30, #32}`, `#32 → {#27, #34, #16}` (minimal subset `{#27}`).
**Acyclic.** #32 does **not** reference #30, #31, #33, #28, #38, #22, or #49; #33 judgement reaches
#32 only through #34's projection.

## 1.4 Key decisions

- **KD-1 (a knowledge BAND per player; estimates derived, never stored; the live-form freshness
  semantic).** The overlay persists only `KnowledgeBand ∈ [0, KNOWLEDGE_BAND_MAX]` per scouted
  player (+ a report day-stamp); per-attribute ranges are **derived on read** by the pure
  `EstimateFor` (band → `[GT]` half-width table; stateless keyed noise re-centre, §3.2). Maximal
  knowledge collapses to `[truth, truth]` **arithmetically** (`HALFWIDTH[BAND_MAX] = 0`), not via a
  special case. This dissolves the plan's two mechanical risks by construction: save bloat (one
  small entry per scouted player) and re-roll determinism (nothing stored can drift; nothing derived
  can re-roll). **Freshness (pinned):** the estimate is a **live-form window** — the *width* is the
  scouted quantity; the centre derives from **current** truth and legitimately tracks #28
  development (a deliberate simplification with a named consequence: range endpoints shift as a
  player grows, so deltas are inferable at band-width granularity without scouting effort).
  Frozen-at-report staleness (needs stored per-attribute snapshots) and quantized-truth centering
  (breaks containment unless widths absorb the quantization error) are §7 extensions.
- **KD-2 (the view boundary — read-only by construction; own-squad omniscience).** Enforced
  structurally: `EstimateFor` takes `in PlayerRecord` (a value copy); #32 holds no reference into
  #27's stores (the composition root resolves squads via the existing `ISquadProvider` and passes
  records in); the overlay keys by `PlayerId` alone and stores no attribute data; the view types are
  readonly value types; no #32 API takes `ref Squad`/`ref PlayerRecord`. **Own-squad omniscience:**
  managed-club players always resolve at `KNOWLEDGE_BAND_MAX` (an explicit short-circuit ahead of
  the overlay read — the manager selects the lineup from these attributes via `LineupSelector`
  today); `AssignScout` on an own-squad id fails loud. Fog covers the 31 `[1,20]` attributes of
  **external** players only; identity facts (name/age/position) and `WeakFootRating [1,5]` are exact
  at any band. #31's counterparty valuation legitimately reads truth (FR-TX-001 — fog is the
  *manager's* condition); manager-side surfaces read only `KnownPlayer`.
- **KD-3 (keyed accuracy draws; views mutate no RNG state; draw-free minimal).** Deep draws are
  position-independent on the `scouting.accuracy` stream (`entityId = playerId`) with a
  **fixed-radix action ordinal** over `(band, attrIdx, purpose)` — **deliberately not `worldDay`**
  (an estimate must be stable until the band advances; #41's per-day key is right for injuries,
  wrong here). Same key ⇒ same noise forever, no serialized seed/cursor/per-pair state. A zero-width
  estimate short-circuits **before** the draw, so the minimal tier makes zero RNG calls and
  `_RESERVED_0x24_`/86 **stays reserved at approval**, promoting to `DOMAIN_TAG_SCOUTING = 0x24` /
  `SubsystemOrdinals.Scouting = 86` at the deep T-phase's first draw.
- **KD-4 (scout quality → assignment SPEED, never width; #32 closes #34's baseline).**
  `DaysPerBand = max(1, DAYS_PER_BAND_BASE · SCOUT_QUALITY_NEUTRAL_PERMILLE / quality)` (integer;
  `quality ≤ 0` fails loud) — a better scout learns **faster**; widths stay a pure function of the
  band. This kills a retroactivity trap: quality-in-the-width-formula would let a staff change
  silently rewrite already-reported knowledge. #32 defines `SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000`
  `[FIXED]` (value-compatible with #34's neutral `FacetPermille` row — no #34 edit needed); #33
  judgement reaches #32 **only** through #34's projection.
- **KD-5 (recommendation is #32's own read-only query; #32 issues no offers).** #31 owns
  *negotiation*, not *search* — there is no #31 search surface to reuse. `RankByEstimate` is a pure
  deterministic ranking over caller-supplied `KnownPlayer`s (estimate midpoint, `PlayerId`
  tiebreak — no draw, no mutation, no stored shortlist). The manager acts via #31's `SubmitBid`,
  informed by — not routed through — #32's view.
- **KD-6 (persistence — a `SCOUTING_SAVE_FORMAT_VERSION` season-save sub-blob; the hygiene rule).**
  An opaque, independently version-gated sub-blob composed into #30's `SeasonSaveCodec` (the
  #41/#33/#31/#34 precedent) — **not** the plan's `WORLD_STORE_FORMAT_VERSION` bump (an argued
  revision: the `WorldStore` composite is #22-owned living-world state; the season save *is* the
  per-manager career save). Knowledge is durable career state (survives `RollToNextSeason`; no decay
  at Stage 3). **Hygiene:** on a roster re-key (#31 transfer) or retirement (#28), the overlay entry
  for the affected `PlayerId` is **dropped** (buy → own-squad rule covers it; sell → knowledge
  reset, a named Stage-3 simplification); a view query for an unresolvable `PlayerId` **fails loud**
  (silent staleness is the trap).
- **KD-7 (assignments — manager commands + the reserved #30 slot; managed-manager scope).**
  `AssignScout`/`CancelAssignment` are explicit manager commands (the `SetTeamTactic`/`SubmitBid`/
  `HireStaff` discipline), fail-loud on fog-off / unknown id / own-squad / busy slot /
  fully-scouted target (FR-SC-020); progress accrues in `AdvanceScoutingDay` at #30's **new
  tick-order slot 7** (after staff #34, before the `AdvanceDay` live tick → step 8) — **empty at
  minimal** (and a no-op with fog off, FR-SC-022), declared reserve-ahead as the one approval-time
  back-prop (**ERR-030-007**). Scope is the **managed manager
  only**: AI clubs do not scout at Stage 3 (omniscient AI valuation, FR-TX-001, is the unchanged
  posture).
- **KD-8 (one code path; behaviour-neutral identity).** `fogEnabled` off ⇒ every read is the
  `BAND_MAX` row of the same tables the deep tier uses (`[truth, truth]`), zero draws, empty
  overlay, empty tick slot — byte-identical to pre-#32. The deep tier **narrows the same view
  seam**; no consumer switches code paths on the dial, only the resolved band changes.

## 1.5 Determinism & coordinate posture

All arithmetic is **integer** (attributes/bands/half-widths/estimates `int`; quality per-mille
`int`). There is **no float in #32**. One clock (world); the minimal tier is draw-free (KD-3); deep
draws are keyed, cursor-free. This is the #40/#41/#31/#34 off-pitch integer + world-tick posture.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §1 (scope, out-of-scope seams, dependencies, KD-1..KD-8, determinism posture), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-2 (L): KD-7's command-gate summary aligned to FR-SC-020/022 (fog-off + fully-scouted gates were missing from the summary). |
#endregion
