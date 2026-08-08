# Personalities, Morale & Squad Dynamics #33 — Design Supplement

> **Created:** July 23, 2026
> **Last Updated:** July 23, 2026 (v0.3 — **PROMOTED**; prior v0.2 AR-1 fix pass 1H+3M+2L)
> **Status:** DESIGN SUPPLEMENT → **PROMOTED** (July 23, 2026) — 11-file section set authored at
> `docs/specs/personalities-morale-dynamics/` (FR-HS-001..028) → section-file AR-1 (5M+4L) → AR-2 (1M+2L) →
> CONVERGENCE → R-01..R-05 signed → **APPROVED**; `SPEC_INDEX.md` row 33 added (36 APPROVED). **Zero
> approval-time cross-spec back-props** (0x25/87 stays reserved; #30 slot 3 + FR-SN-017 + #22
> FR-LW-004/FR-LW-032 pre-declared). Section files are authoritative; this supplement is the design-history
> record. (Original status line follows for history.)
> DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> **Candidate spec:** #33 · **FR prefix:** FR-HS (grep-verified unclaimed across `docs/specs/**`).
> **Master-plan home:** §5 Stage 4 / Master Vol 2 · **Wave:** 3 (GATING — the critical-path spine, §4 of the roadmap).
> **Determinism (proposed):** `DOMAIN_TAG_HUMAN_SYSTEMS` / `SubsystemOrdinals.HumanSystems` = `0x25` / `87`
> — the roadmap §6 off-pitch reservation, **already present as the `_RESERVED_0x25_` placeholder row** in
> #16 §3.4. **Stays RESERVED at approval** (minimal tier is draw-free — the #40 KD-2 precedent); promotes at
> the deep tier's first stochastic draw.
> **Source plan:** `docs/tracking/spec-plans/spec-33-personalities-morale-dynamics.md` v0.2.

---

## 0. Scope

The canonical **vol-2 human-systems substrate**: per-player **personality traits**, a **morale/happiness**
model (the H-Gate confidence-vs-self-efficacy shape), squad **relationships / cliques / chemistry**, and
**mentoring** — advanced on the **world tick** (`WorldClock`, one day = one `worldTick` — never the 10 Hz/60 Hz
match loops) and exposed as an authoritative, **read-only committed-state surface**. This is **the single
producer Living World #22 was built to consume read-only** (its dormant `WorldLoop` phase-2 read + the
FR-LW-004 `PlayerEdge` relationship-layer mirror). Landing #33 **wires those dormant seams phantom-free**; the
*behavioural* activation (real canon flowing into #22's — themselves still-dormant — arc reads) is a
**subsequent, separately-reviewed step** (KD-8 / §11-T2), not part of #33's behaviour-neutral minimal tier.
The payoff of #22 having been built phantom-free (roadmap §4) is that this wire-up needs **no #22 schema or
arc-logic change** — one new mirror-write seam aside (M3 / KD-1).

**Out of scope (owned elsewhere, referenced as seams):**
- **#22's interaction / memory / arc layer** — already built over this model. #22 *mirrors* #33's `PlayerEdge`
  scalar and owns its own `Affinity`/`Trust` layers on top; #33 **never** reads #22's memory layer (one-way,
  KD-1). The `Affinity`/`Trust` **decay baseline `b`** (`x' = x + r·(b − x)`, §3.1) is a **#22-owned,
  currently-deferred** input on those owned layers — **not** a #33 responsibility and **not** applied to
  `PlayerEdge` (which #22 never mutates); #33 supplies no baseline (AR-1 H1).
- **Match-tick behaviour** — the match engine consumes morale only through the read-only #27
  attribute-projection seam (KD-3); #33 owns no match-tick logic and makes no match-tick draw.
- **Media/press dialogue (#35), inbox man-management writes (#46), negotiation willingness (#31), staff-driven
  mentoring assignment (#34)** — those are **consumers/producers that do not exist yet**; #33 exposes the
  read/route seams they will attach to and builds none of them (FR-LW-031).
- **The season day-advance loop itself** — #30 owns the tick order and *invokes* #33 at its pre-declared
  slot 3 (KD-6); #33 never references #30.

## 1. What exists vs. what #33 adds

**Exists (verified against source / approved specs):**
- **The #22 read contract is fully specified and dormant** (verified verbatim in
  `src/living-world/{RelationshipLayer,RelationshipEdge,MemoryStore,WorldLoop,WorldStore}.cs` +
  `docs/specs/living-world/`):
  - `RelationshipLayer { PlayerEdge=0, Affinity=1, Trust=2 }` — `PlayerEdge` is valid **only** on
    player↔player pairs and is a **read-only mirror of the vol-2 §2.1 authoritative edge** (FR-LW-004).
  - `MemoryStore.ApplyEvent` **throws** on `RelationshipLayer.PlayerEdge` ("read-only mirror … FR-LW-004 /
    KD-9") and positively allow-lists only `Affinity`/`Trust`. The mirror is populated **only** through the
    edge-entry seams (`GetOrCreateEdge` / `InsertEdge`) from "the canon read at wiring", gated finite in
    `[0,1]` (NaN fails closed).
  - `WorldLoop` phase 2 ("read committed canonical human-systems state") is an explicit **null seam** — no
    interface authored (FR-LW-031). Its input is "the already-committed canonical human-systems state for
    this tick", produced by "a **prior season-loop phase owned outside this layer**; callers run it before
    this method; the world loop only reads its committed output" (FR-LW-027 / KD-9). **The one thing phase-2
    mirrors is the `PlayerEdge` scalar** — that is #33's entire read surface (AR-1 H1). (#22's live phase-3 is
    episode-*salience* decay `s' = s·(1 − decayRate)`, which needs no baseline; the owned-layer relaxation
    `x' = x + r·(b − x)` is §3.1, deferred **with** phase 2, on `Affinity`/`Trust` — its baseline `b` is
    #22-owned and **not** supplied by #33.)
  - **There is no `MemoryStore` seam to set/refresh `PlayerEdge` on a *live* edge today** (AR-1 M3):
    `GetOrCreateEdge` seeds it to `0.0` and no-ops on an existing edge; `InsertEdge` throws on an existing
    edge; `ApplyEvent` refuses it. So the T-phase wire-up adds **one new public mirror-write method** to
    `MemoryStore` (a `SetPlayerEdgeMirror`-shape seam) — a #22 **code** addition, but **no schema change and
    no arc-logic change** (KD-1).
  - `XC-022-002` (`vol-2 §2.1 social graph | edge model + clique threshold`) is the formal cross-reference
    slot #33 fills; `T-LW-U-035` is the shipped regression that locks `PlayerEdge` bit-unchanged under a
    §3.1 owned-layer update — the invariant #33's producer must not break.
- **#30 already pre-declares #33's landing spot** (`docs/specs/season-competition-loop/`, APPROVED): the
  KD-2 tick order (§3.3 `RunWorldTickInFixedOrder`) enumerates **`3. human-systems (#33) — NULL SEAM today`**
  as a documented position (FR-SN-034); **FR-SN-017** makes #30 the **producer-only** of match-outcome
  events and states "**ingest activation is gated on #33 (FR-LW-032)**"; §3.4 line pins "#22 ingest activates
  with #33". **FR-LW-032** gates #22 Stage-1 activation on the KD-10 prerequisites incl. "vol-2/vol-3 impl."
  — the prerequisite #33 realizes.
- **#27 Squad/Player Data** — `PlayerRecord`/`PlayerAttributes` (31 `int[1,20]` football attributes; **no
  personality-trait or morale field today** — those are genuinely new, not derivable from football
  attributes, unlike #41's injury-proneness). `PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex` is globally
  unique (the keyed-draw / per-player-state key).
- **#28/#29/#40/#41** — established the **season-save opaque sub-blob** convention
  (`PROGRESSION_/TRAINING_/FINANCE_/MEDICAL_SAVE_FORMAT_VERSION`) composed into `SeasonSaveCodec`, and the
  **integer-only** (per-mille) off-pitch arithmetic posture, and the **roster-lifecycle** parallel (regen
  inserts / retirement removes per-`PlayerId` state at the season boundary).
- **#16 §3.4** — `_RESERVED_0x25_` / ordinal `87` placeholder row **already exists** (added in the Wave-2
  catalogue-contiguity fix, held for #33).

**#33 adds:** a per-`PlayerId` `PersonalityProfile` (stable traits) + `MoraleState` (scalar happiness); a
**club-scoped pairwise relationship** store (the authoritative vol-2 §2.1 scalar #22 mirrors as `PlayerEdge`);
an **internal** per-player morale equilibrium (the projection set-point — **not** routed to #22, AR-1 H1);
**derived** cliques (over the pairwise scalar, no independent truth);
an **identity** mentoring surface; a world-tick `AdvanceHumanSystemsDay` step invoked at #30's **pre-declared
slot 3** (a *fill*, not a new step); a **read-only committed human-systems view** (the phase-2 route + morale
accessors); and the `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` sub-blob. **No RNG stream at minimal** (draw-free).

## 2. Staging (minimal-first → deep, one code path)

- **Stage-2 minimal** — a **small stable trait vector** per player (neutral-seeded, no variety yet — the #27
  T0 all-neutral precedent), a **scalar morale** per player (the H-Gate collapsed to one value) updated by a
  **deterministic daily projection** toward a target assembled from **committed** inputs (recent match
  results, playing time, board objective state — all already produced by #30, routed in as values), a
  **club-scoped pairwise relationship** scalar seeded to a neutral baseline with an optional deterministic
  drift, cliques **derived** from it (threshold `> 600‰` = #22's `0.6`), **empty** mentoring, and an internal
  per-player morale equilibrium (the projection set-point, not routed to #22). Every projection is **integer
  per-mille and deterministic — no draw** (KD-6).
- **Stage-3/4 deep** — the **H-Gate split** (morale = f(Confidence, SelfEfficacy) instead of one scalar),
  **trait variety generation** (a keyed draw at roster/regen time), **stochastic morale reactions /
  personality-driven event reactions** (the first draw site — promotes `0x25`/87), **mentoring propagation**
  with #34 staff-driven pairing, and cross-club relationships — all on the **same one code path**, each
  defaulting to its Stage-2 identity via a config dial (`deepHumanSystemsEnabled` off ⇒ collapsed scalar
  morale, neutral traits, no draws, no mentoring, club-scoped edges).

**One code path (KD-8):** the collapsed-scalar morale, neutral traits, identity mentoring, and draw-free
projection are the exact identities the deep tier modulates — the #21/#27/#28/#29/#40/#41
default-behaviour-neutral discipline, not a rewrite.

## 3. Dependencies & reference direction (one-way, no cycle)

- **#30 → #33** — the day-advance loop *invokes* `AdvanceHumanSystemsDay` at its **pre-declared slot 3**
  (FR-SN-034), passing committed match/season results as **values**, and hands #33's committed view **back to
  the composition root**. #30 is **producer-only** (FR-SN-017) and is **not** the router into #22 — its sole
  #22 surface stays the outcome event (#30 §4). #33 **never** references #30 (the #41/#28/#29 direction).
- **#33 → #22 (via the season-save composition root, one-way, no assembly reference)** — #33 produces the
  authoritative `PlayerEdge` scalar; the **season-save root** (`TacticalDirector.SeasonSave`, the assembly
  that already references *both* `match-engine` and `living-world`) routes it into #22's phase-2 as
  **primitive arrays** (parallel `fromPlayerId[]/toPlayerId[]/edge[0,1][]` — **no baseline array**, AR-1 H1),
  so **living-world never references #33** and #33 never references living-world (the #23
  `MarkingPressureEvaluator` primitive-span precedent — "Mechanics cannot import the AI-layer `FilteredView`").
  #22 mirrors via the new `SetPlayerEdgeMirror` seam (M3), **refuses** every `PlayerEdge` write via
  `ApplyEvent`, and owns only `Affinity`/`Trust`. **#33 never reads #22's memory layer** (KD-1).
- **#33 → #27, #16** — reads `PlayerRecord`/`PlayerAttributes` (player identity/attributes); consumes the
  determinism namespace + the world-tick `DeterministicRngService` (only when the deep tier draws).
- **Consumers (deferred, no interface built):** the match engine reads morale **read-only** through the #27
  attribute-projection seam (KD-3); #31 (negotiation willingness), #35 (media), #45 (board-confidence shape
  reuse), #29 (cohesion) read morale accessors; **#46 is the only consumer that *writes* #33 morale**
  (man-management) — all deferred until those specs exist (FR-LW-031).

Reference DAG: `compositionRoot → {#30, #22, #33}`, `#33 → {#27, #16}`. **Acyclic.** No sim assembly references
#33's consumers; #22 stays schema-untouched (its `PlayerEdge` mirror is a value it already serializes — #33
becomes the hand that supplies it).

## 4. Persistent state & save impact (KD-7)

Adds an opaque, independently version-gated **human-systems sub-blob** (`HUMAN_SYSTEMS_SAVE_FORMAT_VERSION`
[FIXED] = 1) composed into #30's season save via the `SeasonSaveCodec` pattern — **not** a
`WORLD_STORE_FORMAT_VERSION` bump (this **supersedes the plan §4 guess**; §7 KD-7 gives the rationale). Per
club: each player's `PersonalityProfile` (trait vector) + `MoraleState` (morale scalar + the internal
equilibrium set-point), and the club-scoped pairwise relationship scalars + mentoring pairings. The composing outer
`SEASON_SAVE_FORMAT_VERSION` bump is coordinated with #30 at the T-phase exactly as #28/#29/#40/#41 defer it —
appended after the existing sub-blobs, codec never parses it.

**The #22 `PlayerEdge` mirror is NOT double-persisted by #33** — it stays #22's own serialized state
(`WorldStateSerializer` / `WORLD_STORE_FORMAT_VERSION`, **unchanged**). #33 persists the **authoritative**
scalar; #22 persists its **mirror**, which the root refreshes onto live edges each tick (via the new
`SetPlayerEdgeMirror` seam, M3) **before** phase 2, so mirror ≡ authoritative at every save point — no
divergence, serialize-don't-regenerate on both sides. **No `WORLD_STORE_FORMAT_VERSION` bump at all.** (At the
#33 **minimal** tier the mirror route runs empty/neutral — KD-8 — so this refresh is exercised only once real
canon flows; the coherence argument holds identically in both states.)

**No RNG cursor is serialized** — minimal is draw-free; the deep-tier draws will be **position-independent
keyed draws** on `(playerId, worldDay, purpose)` (the #41/#28/#30 off-pitch keyed-draw precedent), so even
the deep tier persists no free-running cursor. Fail-loud on version mismatch / out-of-bounds length prefix
(overflow-safe `ReadCount`) / trailing bytes (F3/F5, the `MatchSaveCodec` posture). **Roster-membership
lifecycle** in lockstep with #28's season-boundary churn: a regen inserts a neutral
`PersonalityProfile.Create()` + `MoraleState.Create()` for the fresh `PlayerId` (and drops it from every
prior teammate's pairwise set); a retirement removes the retiree's per-player state + pairwise entries — keyed
by `PlayerId`, applied by the roster owner (#30), the FR-PG-011 / FR-MD (KD-7) parallel.

## 5. Determinism (KD-6 — single world clock, draw-free minimal)

**All #33 state advances on the WORLD tick**, at #30's pre-declared slot 3, from **committed** inputs #30
routes in as values. The **minimal tier makes no stochastic draw** — morale/relationship projections are
**deterministic integer per-mille** functions of committed inputs (the #40 minimal-draw-free posture).
Consequently:
- **`0x25`/87 stays `_RESERVED_0x25_`** at #33's approval (no `DOMAIN_TAG_HUMAN_SYSTEMS` promotion, **no #16
  spec-text change**) — the #40 KD-2 "reserved-not-promoted" precedent. It promotes to a live domain tag +
  `SubsystemOrdinals` entry only at the **deep tier's first draw** (trait-variety generation / stochastic
  morale reactions), with that stream registered on the world-tick `DeterministicRngService` at #33 T3.
- **Save→restore is byte-exact with nothing to continue** — no cursor at minimal, and keyed draws (no
  cursor) at deep.
- **Stream independence (trivially):** registering **no** stream leaves every existing stream's cursor
  byte-identical — a stronger property than #41's `injuries.occurrence` (which registers one), the #40
  `_RESERVED_0x29_` precedent (T-FN-NEU-003 class).

Integer-per-mille internally; the **only float** is at the **#22 mirror boundary** — the phase-2 route emits
`edgePermille / 1000f ∈ [0,1]` to satisfy `InsertEdge`'s finite-`[0,1]` gate (a single deterministic division
at the seam; the same integer permille always yields the same float bits, so both sides round-trip byte-exact).
The clique threshold `> 0.6` maps to `> 600‰` in #33's derived-clique computation (KD-4).

## 6. Primary surfaces (proposed → pinned in §4 of the section files)

```csharp
// #33-owned per-player world-tick state (serialized, KD-7). Integer per-mille (no float internally).
public struct MoraleState
{
    public int MoralePermille;          // happiness [0,1000]; 500 = content (neutral seed)
    public int EquilibriumPermille;     // #33-INTERNAL per-player morale set-point [0,1000] the daily
                                        //   projection drifts toward. NOT routed to #22 (AR-1 H1 — #22 needs
                                        //   no baseline from #33; its Affinity/Trust baseline `b` is #22-owned).
    public uint LastAdvancedWorldDay;   // idempotency cursor (F6); HS_NOT_ADVANCED_SENTINEL = uint.MaxValue, NOT 0
    public static MoraleState Create() => new() { MoralePermille = MORALE_NEUTRAL_PERMILLE,
        EquilibriumPermille = MORALE_NEUTRAL_PERMILLE, LastAdvancedWorldDay = HS_NOT_ADVANCED_SENTINEL };
}

// Stable personality traits (neutral-seeded at minimal; variety is a deep-tier generation draw). int[1,20]
// on the #27 posture; NOT appended to #27's PlayerRecord at minimal (a recorded deep-tier #27 append, KD-2).
public struct PersonalityProfile   // Professionalism, Ambition, Loyalty, Temperament, Determination
{
    public byte Professionalism, Ambition, Loyalty, Temperament, Determination;   // [1,20]
    public static PersonalityProfile Create() => /* all TRAIT_NEUTRAL (10) */;    // never default()
}

// The authoritative vol-2 §2.1 pairwise edge #22 mirrors as PlayerEdge (club-scoped at minimal). Integer
// per-mille internally; exposed to the #22 route as float [0,1]. Cliques DERIVE from this (KD-4) — no
// independent clique truth.
public struct PairwiseRelationship { public int FromPlayerId, ToPlayerId, StrengthPermille; }  // [0,1000]

// The world-day step (KD-6), invoked at #30's pre-declared slot 3: deterministic projection, NO draw at
// minimal. `results`/`context` are committed values #30 routes in (no #30 reference).
public static void AdvanceHumanSystemsDay(ref MoraleState m, int playerId, in PersonalityProfile p,
    in HumanSystemsDayInput committedInputs, uint worldDay);   // rng added only at the deep-tier draw

// KD-1 — the read-only committed human-systems view the season-save root routes into #22 phase-2 as
// PRIMITIVE ARRAYS (no cross-assembly type). ONLY the pairwise PlayerEdge scalar (AR-1 H1 — no baseline
// array: #22 needs none). Pure read; #22 mirrors via SetPlayerEdgeMirror, never calls back.
public readonly struct HumanSystemsView   // fromPlayerId[]/toPlayerId[]/edge[0,1][]  (edge = StrengthPermille/1000f)
{ /* value-copy accessors */ }

// KD-3 morale read accessor (match projection / #31 / #35 / #45 / #46 read; consumption deferred). A per-entity
// morale VALUE for future #22 arc-trigger reads (XC-022-001) is a DEEP-tier route added when arc triggers
// wire up — NOT part of the minimal phase-2 mirror (those triggers are themselves still dormant).
public static int MoraleOf(in MoraleState m) => m.MoralePermille;      // read-only projection OUT

// KD-4 derived cliques over the pairwise scalar (> CLIQUE_THRESHOLD_PERMILLE = 600). No persisted state.
public static IReadOnlyList<Clique> DeriveCliques(/* club pairwise set */);

// KD-5 mentoring — empty identity at minimal; #34 staff-driven pairing is the deep-tier producer (routing seam).
public readonly struct MentoringPlan { public static MentoringPlan None => default; }
```

## 7. Key design decisions

- **KD-1 (the FR-LW-004 read surface — the headline risk, matched exactly).** #33 exposes the vol-2 §2.1
  edge as a **pure read**: **exactly one quantity** — a scalar `∈ [0,1]` per **player↔player ordered pair**
  (clique threshold `> 0.6` intact). **#33 supplies no baseline** (AR-1 H1): #22 never decays `PlayerEdge`
  toward a baseline (it is a read-only mirror #22 re-reads, never evolves), and the `x' = x + r·(b − x)`
  owned-layer relaxation that *does* use a baseline `b` runs on #22's **own** `Affinity`/`Trust`, with a
  **#22-owned** (and currently-deferred) `b` — not a #33 responsibility. The season-save root routes the
  pairwise scalar into #22's phase-2 as **primitive arrays** (no assembly reference either way); #22
  **refuses** all `PlayerEdge` writes via `ApplyEvent` and owns only `Affinity`/`Trust`. The coupling is
  **strictly one-directional** — #33 writes canon, #22 reads a mirror, #33 never reads #22. **The mirror
  write needs one new seam (AR-1 M3):** no `MemoryStore` method sets `PlayerEdge` on a *live* edge today
  (`GetOrCreateEdge` no-ops on an existing edge, `InsertEdge` throws on one, `ApplyEvent` refuses), so the
  T-phase adds a small public `SetPlayerEdgeMirror(fromId, toId, value)` — a #22 **code** addition with **no
  schema change and no arc-logic change** (so "no #22 redesign" means precisely that, not "no code"). Because
  #22 was otherwise authored phantom-free for this shape, `T-LW-U-035` (PlayerEdge bit-unchanged under an
  *owned-layer* update) **stays green**, and the phase-2 wire-up accepts an **empty view = byte-identical
  #22**; flowing a real #33 view is a deliberate, separately-reviewed activation (KD-8; the #21
  `SetTeamTactic` wire-neutral / non-default-lights-it-up pattern). **Getting this surface wrong is the
  highest risk in the batch (§9); it is de-risked by supplying exactly the one pairwise scalar and nothing
  else.**

- **KD-2 (minimal trait vector + collapsed-scalar morale).** Minimal traits = a small **stable** vector
  (Professionalism/Ambition/Loyalty/Temperament/Determination, `byte[1,20]` on the #27 posture),
  **neutral-seeded** (10) — variety is a deep-tier generation draw (the #27 T0→T1 all-neutral-first
  precedent). Morale is a **single scalar per-mille** at Stage 2 (the H-Gate **collapsed**); the deep tier
  splits it into Confidence/SelfEfficacy on the same field via a config dial. Traits live in **#33's own**
  per-player state, **not** appended to #27's `PlayerRecord` at minimal (they are genuinely new, non-derivable
  from football attributes — unlike #41's derived injury-proneness); a #27 append is a **recorded deep-tier
  option, not built** (avoids a #27 schema ripple in the minimal tier).

- **KD-3 (morale → match / consumers — read-only projection, one direction, deferred).** Morale reaches the
  match engine **only** through the #27 attribute-projection seam, **read-only**; #33 owns no match-tick
  write and makes no match-tick draw. The direction is defined (out of #33), the **consumption deferred** —
  wiring morale into the match projection changes match behaviour, so it is its own reviewed change (the #27
  T1 "not behaviour-neutral, needs its own change" precedent), not built at #33 minimal. #31/#35/#45 read
  morale accessors when they exist; **#46 is the sole consumer that writes #33 morale** (man-management) —
  all deferred (FR-LW-031). **No two-way coupling** (the §9 fragility risk).

- **KD-4 (cliques/chemistry — derived, no double-truth).** Cliques/chemistry are a **derived read** over the
  #33-owned pairwise relationship scalar (a component/group where pairwise `> 600‰` = #22's `0.6`), **not**
  independent persisted state. The **one truth** is #33's pairwise scalar; #22's `PlayerEdge` is a **mirror**
  of it and cliques are a **derived view** of it — reconciling the §9 "clique double-truth against #22's edge
  store" risk structurally (there is nothing to diverge). Chemistry (a squad-level aggregate) is likewise a
  derived read, persisted nowhere.

- **KD-5 (mentoring — identity routing seam).** Minimal mentoring is the **empty identity** (`MentoringPlan.None`
  — no pairs, no propagation). The deep tier adds daily trait/morale propagation between paired players; the
  **pairing lifecycle** is a **#34 staff-driven producer** via an identity routing seam (default = #33's
  auto-derivation from committed seniority/traits), the #41 `MedicalModifier` / #29 `CoachingModifier`
  pattern — **no #34 interface built** (FR-LW-031). Answers the plan's KD-5 "#33 vs #34 owns pairing": #33
  owns the *lifecycle and default*, #34 becomes the *override producer*.

- **KD-6 (determinism — draw-free minimal, single world clock).** Minimal makes **no stochastic draw**;
  projections are deterministic integer per-mille functions of committed inputs. `0x25`/87 **stays
  `_RESERVED_0x25_`** (no #16 change at approval — the #40 KD-2 reserved-not-promoted precedent), promoting to
  a live tag + stream only at the deep tier's first draw (keyed on `(playerId, worldDay, purpose)`, the
  off-pitch precedent). One clock (world), so the plan's determinism-ordering fragility cannot arise. `0x25`/
  87 confirmed free and contiguous (the placeholder row exists; ordinals 80–92 contiguous post-Wave-2).

- **KD-7 (persistence — season-save sub-blob; supersedes the plan's `WORLD_STORE_FORMAT_VERSION`).**
  `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob composed into `SeasonSaveCodec`, **not** a
  `WORLD_STORE_FORMAT_VERSION` bump. Rationale: #33's authoritative state is a per-`PlayerId` career-state
  overlay exactly like #28/#41 (which chose the season-save sub-blob), advanced by #30's day-advance loop
  alongside them, and composed at the season/composition layer (which sits **above** living-world, so
  living-world cannot host it anyway). The **#22 `PlayerEdge` mirror stays #22's own serialized state**
  (unchanged, **no `WORLD_STORE_FORMAT_VERSION` bump**) — #33 supplies the value #22 already serializes; the
  mirror is routed each tick before phase 2, so it equals the authoritative scalar at save time (no
  divergence). The plan §4 wrote `WORLD_STORE_FORMAT_VERSION` before the #28/#29/#40/#41 season-save-sub-blob
  convention existed; this supplement reconciles to it. Fail-loud gates; serialize-don't-regenerate;
  roster-lifecycle in lockstep with #28.

- **KD-8 (behaviour-neutral identity + stream independence — with the wire/flow boundary made explicit).**
  #33's minimal addition is neutral in three senses: (a) **stream independence** — registering **no** stream
  leaves every existing stream's cursor byte-identical (trivially, the #40 property); (b) **the #22 phase-2
  wire-up is fed an empty view at #33 minimal** ⇒ #22 output byte-identical (`T-LW-U-035` green) — this is
  what #33 minimal ships; (c) **no consumer is wired at minimal** — morale drifts internally with no reader
  (match projection / #31 / #46 deferred), so no observable match/world behaviour changes (the #27 T0
  canonical-records-exist-but-unconsumed precedent). **The wire/flow boundary is explicit and honest:**
  *wiring* the seam (empty view) is behaviour-neutral and is #33 minimal; *flowing real #33 canon* into #22 —
  at which point a pairwise edge can legitimately cross `600‰`, form a clique, and change #22 arc behaviour —
  is a **named, separately-reviewed activation step (§11-T2's "real-canon" sub-step), NOT behaviour-neutral
  by design.** That is the intended payoff (#22 was dormant *because* canon did not exist), not a neutrality
  violation. The deep tier extends the collapsed-scalar / neutral-trait / identity-mentoring / draw-free
  surface, never rewrites it.

## 8. Cross-spec back-props (remarkably few — the sequencing payoff)

**At approval: ZERO cross-spec spec-text back-props.** This is the point of #33 being on the pre-planned
critical-path spine — every landing spot was reserved ahead of it:
- **#16 §3.4** — **no change.** `_RESERVED_0x25_`/87 already exists and stays reserved (draw-free minimal,
  KD-6). Contrast #41's ERR-041-001 (which promoted `0x2A` because #41 draws).
- **#30** — **no change.** Slot 3 is a pre-declared null seam (FR-SN-034) and FR-SN-017/§3.4 already gate #22
  ingest activation on #33. #33 **fills** the seam; it does not append a step. Contrast #41's ERR-030-002
  (which *inserted* step 4).
- **#22** — **no spec-text change at approval.** FR-LW-004 / XC-022-002 / FR-LW-032 / `WorldLoop` phase-2 were
  authored for exactly this producer. #33 §8 cites XC-022-002 as the **producer** side of the existing
  cross-reference.

**At the #33 T-phase (deferred, lands with code — the #28/#29/#41 deferred-coordination precedent):**
- **#30** — the outer `SEASON_SAVE_FORMAT_VERSION` bump composing the new sub-blob (coordinated at T1, as
  #28/#29/#40/#41 all defer their outer bump).
- **#22** — the phase-2 wiring: a **new public `MemoryStore.SetPlayerEdgeMirror(fromId, toId, value)` seam**
  (M3 — none exists today) plus `WorldLoop`/`WorldStore` consuming the routed **primitive-array** committed
  view (default empty = no-op, `T-LW-U-035` green). A #22 **code** addition, **no schema change, no arc-logic
  change**; the concrete primitive-param shape + the new seam are recorded in #22 at that wiring (a T-phase
  ERR-022-NNN, not an approval-time change).
- **#16** — `DOMAIN_TAG_HUMAN_SYSTEMS = 0x25` / `SubsystemOrdinals.HumanSystems = 87` promotes at #33 **T3**
  (the first deep-tier draw), spec-text-first, with the stream registered at the draw site.

## 9. Test focus

- **KD-1 read-contract lock (the headline).** The routed `PlayerEdge` values are finite `∈ [0,1]`; #22's
  `ApplyEvent` still throws on `PlayerEdge`; wiring phase-2 with an **empty** view changes **no #22 output
  byte** (`T-LW-U-035`-class); the clique threshold rides the same scalar at `> 600‰`.
- **Save→restore round-trip** across a **mid-season** boundary and a **mid-`RollToNextSeason`** boundary —
  `MoraleState` / `PersonalityProfile` / pairwise / mentoring restore **field-identical**; two-run
  determinism of a full season's human-systems projection from one world seed.
- **Draw-free / stream-independence lock (KD-6/KD-8).** The serialized human-systems block contains **no**
  `RngCursor`/`actionOrdinal` field (grep/schema-shape assertion); registering #33 leaves every existing
  stream's cursor byte-identical (the `T-FN-NEU-003` class).
- **Behaviour-neutral identity (KD-8).** A default squad advances identically to pre-#33 once wired (no
  consumer reads morale; the #22 view defaults empty); `PersonalityProfile.Create()` / `MoraleState.Create()`
  are the neutral seeds; `MentoringPlan.None` is the empty identity.
- **Cliques derive, don't double-persist (KD-4).** `DeriveCliques` is a pure read over the pairwise scalar
  (no persisted clique field in the sub-blob — schema-shape assertion); a pairwise edit is reflected in both
  the derived clique and the #22 mirror with no third stored copy.
- **Clique int/float boundary lock (KD-4/KD-6, AR-1 L2).** #33 derives cliques on integer per-mille
  (`> 600`) while #22's `PlayerEdge` clique math runs on `edgePermille / 1000f` against `0.6f`. A dedicated
  test asserts the two agree at the boundary — permille **600** is *not* a clique on either side, permille
  **601** *is* on both (`600/1000f == 0.6f`) — so the cross-representation is **proven**, not assumed.
- **Integer posture (KD-6).** Every `MoraleState`/`PersonalityProfile`/`PairwiseRelationship` field is an
  integer; no projection introduces a float except the single `edgePermille / 1000f` at the #22 boundary
  (static/reflection assertion, the #40/#41 integer-posture lock).
- **Ordering / idempotency (KD-6).** #33 runs at slot 3 (before #41's slot 4, before `AdvanceDay`); advancing
  the same world day twice is a no-op (`LastAdvancedWorldDay`); a day gap fails loud (the #30 one-day-at-a-time
  posture).
- **Roster lifecycle.** A regen inserts neutral per-player state + drops the fresh id from prior pairwise
  sets; a retiree's per-player + pairwise entries are removed (no unbounded leak across seasons).
- **Fail-loud.** Bad `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION`, out-of-bounds length prefix, trailing bytes; an
  out-of-range morale/trait value, or a pairwise `PlayerId` outside the club universe, fails loud at the
  consuming seam (the #27 `SquadFileLoader` / #28 F4 precedent).

## 10. Risks

- **KD-1 surface shape wrong (headline).** De-risked by matching the **verbatim** FR-LW-004 / `ApplyEvent` /
  phase-2 contract (§1, §7 KD-1); `T-LW-U-035` and the empty-view no-op are the locks. Getting it right is
  what avoids the #22 rewrite the whole §4 sequencing constraint exists to prevent.
- **Two-way morale coupling temptation (#31/#35).** Mitigated by KD-3: morale is a **read-only projection
  OUT**; only #46 writes it (deferred). No feedback loop → determinism ordering stays simple.
- **Clique double-truth against #22's edge store.** Dissolved by KD-4: cliques **derive** from the one
  #33-owned scalar #22 mirrors; nothing independent is persisted.
- **Save-home inconsistency.** Resolved by KD-7 reconciling the plan's `WORLD_STORE_FORMAT_VERSION` guess to
  the #28/#29/#40/#41 season-save-sub-blob convention (and #33 adds **no** `WORLD_STORE_FORMAT_VERSION` bump —
  the #22 mirror stays #22's).
- **Deferred producers/consumers land later.** Mitigated by identity seams: `MentoringPlan.None` (#34),
  deferred morale consumption (#31/#35/#45/#46), collapsed-scalar morale + neutral traits (deep tier), a
  recorded #27 trait append — all default to their Stage-2 identities.

## 11. Promotion pipeline

1. Author the 11-file section set at `IN REVIEW` (FR-HS-001..NNN).
2. Section-file PASS-1 adversarial review → AR-2/AR-3 to convergence.
3. R-01..R-05 lead-developer sign-off → APPROVED; flip `SPEC_INDEX.md` row.
4. **Back-props at approval: none** (§8 — `0x25`/87 stays reserved; #30 slot 3 + FR-SN-017 pre-declared; #22
   FR-LW-004/phase-2 pre-authored). #33 §8 cites XC-022-002 as the producer side.
5. T-phase (post-APPROVED): T0 value types + deterministic Stage-2 morale/relationship projection
   (behaviour-neutral) → T1 `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` sub-blob + season-save composition (#30 outer
   bump coordination) → T2 `AdvanceHumanSystemsDay` wired at #30 slot 3 + the #22 phase-2 primitive-array
   wire-up: adds `MemoryStore.SetPlayerEdgeMirror` (M3) + consumes the routed view **default-empty (no-op,
   `T-LW-U-035` green)**; ERR-022-NNN records the seam + param shape. **Flowing real #33 canon into #22 is a
   named sub-step (KD-8), not behaviour-neutral** → T3 deep H-Gate split / trait-variety generation /
   stochastic reactions (promotes `0x25`/87, ERR-016) / mentoring propagation / #34 staff seam.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 23, 2026 | Initial design supplement from spec-plan v0.2, grounded on the verbatim #22 FR-LW-004 / `ApplyEvent` / `WorldLoop` phase-2 read contract. |
| v0.2 | July 23, 2026 | AR-1 (1H+3M+2L). **H1** — dropped the per-entity `baseline b` from the KD-1 read surface: #22 never decays `PlayerEdge` toward a baseline, and the `x'=x+r(b−x)` relaxation is on #22's own `Affinity`/`Trust` with a #22-owned, deferred `b`; #33 supplies **only** the pairwise `PlayerEdge` scalar. `MoraleState.BaselinePermille` → internal `EquilibriumPermille` (not routed); `HumanSystemsView` drops the baseline array. **M2** — the router is the `TacticalDirector.SeasonSave` root (references both assemblies), **not** #30 (producer-only, FR-SN-017); §3 corrected. **M3** — no `MemoryStore` seam sets `PlayerEdge` on a live edge today; T-phase adds a `SetPlayerEdgeMirror` public method — a #22 code addition (no schema/arc-logic change); "no #22 redesign" re-scoped; KD-7 coherence argument now rests on that seam. **M4** — made the wire-neutral (empty view, #33 minimal) vs flow-behavioural (real canon, separate named activation) boundary explicit in §0/KD-8/§11; softened "lights up the seams". **L1** — corrected the phase-3 (salience decay) mislabel. **L2** — added the clique int/float boundary lock (permille 600/601) to §9. |
| v0.3 | July 23, 2026 | PROMOTED — 11-file section set authored + APPROVED (section-file AR-1 5M+4L → AR-2 1M+2L → CONVERGENCE). Notable section-file fixes beyond the supplement: `SetPlayerEdgeMirror` written in `MemoryStore`'s real `FindEdgeIndex(.., out found)` + ordered `_edges.Insert` idiom; the clique rule made **mutual** (matching #22); `default(MoraleState)` fail-loud scoped to the paired `PersonalityProfile` + insertion-time validation; `CLIQUE_THRESHOLD_PERMILLE` tagged `[DERIVED]`. |
