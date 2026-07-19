# Player-Attribute Projection — Design Supplement (Plan 1, #27 T1/T2)

> **Created:** July 17, 2026
> **Status:** DESIGN SUPPLEMENT (pre-code — no section files, no `SPEC_INDEX.md` row).
> Companion to `docs/tracking/squad-player-data-design.md` (candidate spec **#27**); this doc is
> the detailed design for that supplement's deferred **§4 T1/T2 wiring** — the field-by-field
> projection from the canonical `PlayerDatabase.PlayerAttributes` record into the per-spec
> attribute-seeding sites `MatchEngine` actually has, the scale semantics of each, and the
> default-neutrality guarantee that keeps the no-squad match byte-identical to today.
> **Purpose:** Turn "wire `MatchEngine` to source attributes from a `Squad`" from a mechanical
> hand-wave into a reviewed mapping — scoped to the consumers that exist, with explicit scale
> conversions, a proven neutral path, and a pinned decision for every field with no canonical source.

---

## 0. Scope and why this is its own doc

The #27 T0 landed the canonical data layer (`src/player-database/`) but deliberately left
`MatchEngine` seeding on `PlayerAttributes.CreateDefault()` / `STAGE0_NEUTRAL_ATTRIBUTE`. The #27
supplement §4 records T1 ("seed from a `Squad`") and T2 ("close `ERR-007` for real") as one-line
deferrals. The corrected Plan-1 review found those two lines hide a per-field projection with scale
semantics (P1-H1), a default-neutrality proof or planned rebaseline (P1-H2), a GK-routing contract
(P1-M1), and a runtime-vs-attribute boundary (P1-M2). This doc is that work.

**Critical scoping correction (AR-2, this doc's own review — see §11):** the projection targets are
**the attribute-seeding sites `MatchEngine` actually has**, not "every per-spec attribute struct."
Two of the seven structs — `GoalkeeperAgentAttributes` (#11) and `HeadingAgentAttributes` (#10) —
**are never built by `MatchEngine`** (those specs are not wired into the tick pipeline; there is no
`_gkAttrs`/`_headingAttrs` field or `BuildGk*/BuildHeading*` method). Projecting into them in T1
would be building against a non-existent consumer — the phantom-interface class the project forbids.
They are kept below as **forward-compat canonical mappings**, explicitly **not T1 wiring targets**.

**In scope:** the pure projection into the live seeding sites; its scale rules; the neutral-path
proof; the runtime/attribute split; the GK-routing contract (for when #11 is wired); the CS0104
hazard; the T1/T2 test plan.

**Out of scope (unchanged from #27 §0):** aging/training/transfers; the on-disk save format; the
snapshot roster-reference field (that is #27 T3 / Plan-4). This doc does **not** design the
`MatchSquads` input container or lineup selection (Plan-1c / Plan-3), nor wire #10/#11 into the
engine (a separate, unbuilt integration).

---

## 1. The two sides (grounded in source)

**Canonical source** — `src/player-database/PlayerAttributes.cs`: 31 `int [1,20]` fields (in
`AttrIdx` order) + `WeakFootRating int [1,5]`. `CreateDefault()` = every `[1,20]` field `10`,
`WeakFootRating = 3`.

**Attribute-seeding sites `MatchEngine` has today** (each seeding line read from source this
session). "T1?" = whether T1 rewires it from a `Squad`:

| Seeding site (MatchEngine) | Target type | Storage | Current seed | T1? |
|---|---|---|---|---|
| `_attrs[i]` (677) + `_benchAttrs[t][b]` (523) | `AgentMovement.PlayerAttributes` | `int [1,20]` | `CreateDefault()` | **yes** |
| `_dtAttrs[i]` (1577) | `DecisionTree.DtAgentAttributes` | `int [1,20]` | `CreateDefault()` | **yes** |
| `_perceptionAttrs[i]` (1574) | `Perception.PerceptionAgentAttributes` | `int [1,20]` | `CreateDefault()` | **yes** |
| `BuildPassAttributes(i)` (3552) | `PassMechanics.PassAgentAttributes` | `float` (`[1,20]`) | `STAGE0_NEUTRAL_*` | **yes** |
| `BuildShotAttributes(i)` (3575) | `ShotMechanics.ShotAgentAttributes` | `int [1,20]` | `STAGE0_NEUTRAL_*` | **yes** |
| `PressingAgentSnapshot.FirstTouchAttribute` (1872) | `float [1,20]` | float | `STAGE0_NEUTRAL_ATTRIBUTE` | **yes** |
| `DefensiveAgentSnapshot.PerceivedFirstTouch` (1977) | `float [1,20]` | float | `STAGE0_NEUTRAL_ATTRIBUTE` | **yes** |
| `FirstTouchContext.FirstTouchAttribute` (2775) | `int [1,20]` | int | `STAGE0_NEUTRAL_ATTRIBUTE` (rounded) | **yes** |
| `AttackingAgentSnapshot(pace,·,dribbling)` (2025/2027) | `float [0,1]` (**normalized**) | float | `STAGE0_NEUTRAL_NORMALIZED` (0.5) | **yes** |

**Not seeded by MatchEngine (forward-compat mapping only, NOT a T1 target — §0):**
`GoalkeeperMechanics.GoalkeeperAgentAttributes` (#11) and `HeadingMechanics.HeadingAgentAttributes`
(#10). Their canonical mappings are in §3.6/§3.7 for when those specs are integrated; T1 wires none
of them.

**`_perfs[i]` (678, `PerformanceContext.CreateNeutral`)** is runtime performance state, not an
attribute — out of T1 (P1-M2 / §5), same as fatigue.

**The three `FirstTouchAbility` sites** (Pressing #13 `FirstTouchAttribute`, Defensive #14
`PerceivedFirstTouch`, First Touch #4 `FirstTouchContext.FirstTouchAttribute`) all consume a
first-touch-ability attribute, today flat at the generic neutral. They map to canonical
`FirstTouchAbility` — which #27 §3 lists as **RESERVED / "not consumed by any Stage-0 spec."** That
classification is **wrong**: `FirstTouchAbility` is consumed (as a neutral placeholder) by #13, #14,
and #4 right now. Correcting #27's reserved-list is a separate one-line edit (noted, not made here);
this doc treats `FirstTouchAbility` as a live, consumed attribute.

**Derived consumers flow transitively (no separate projection).** Some snapshot fields are computed
*from* an already-projected struct rather than seeded directly — e.g. the #13 CoverShadowCurve inputs
`DefensivePositioningAttribute` / `PhysicalEffortAttribute` / `MentalSharpnessAttribute`
(MatchEngine.cs:1884–1886) are means over `_dtAttrs` fields. Once `ToDecisionTree` populates
`_dtAttrs` with real values, these become varied automatically — they are **not** seeding sites and
need no projection row. AR-3 (§11) swept for such cases; the §1 seeding inventory above is exhaustive
(every `STAGE0_NEUTRAL_*` and `CreateDefault()` attribute site, verified against source).

---

## 2. Scale reconciliation — the core P1-H1 finding

The review anticipated a broad "`[1,20]` canonical vs `[0,1]` consumers" split. The source is
narrower and sharper:

1. **Every non-normalized seeding site takes raw `[1,20]`.** The struct/context targets hold
   `[1,20]` values (Pass uses `float`, but `[1,20]`-valued; the three FirstTouch sites are `float`
   or `int`, `[1,20]`-valued). So the projection into them is a **raw value copy** — `int→int` or
   lossless `int→float` widening — with **no scale conversion at the seam**. WeakFootRating stays
   `[1,5]` (KD-2, already isolated). There is **no `[0,1]` field** on any live struct target.

2. **Exactly one live target is pre-normalized:** `AttackingAgentSnapshot.pace/dribbling` (`[0,1]`).
   Here the projection *must* convert `[1,20] → [0,1]`.

3. **The codebase carries two contradictory `[1,20]→[0,1]` conventions** — the projection must pin
   one:
   - **`÷ ATTR_MAX` (÷20):** `GoalkeeperAgentAttributes.Clamp01Norm` (`GoalkeeperConstants.ATTR_MAX
     = 20.0f`, confirmed) and `HeadingMechanicsConstants.ATTR_MAX = 20.0f` (confirmed). Neutral
     `10 → 0.5`. This is what the live `STAGE0_NEUTRAL_NORMALIZED = 0.5` seed matches.
   - **`(raw − 1) / 19`:** the **documented** convention on `AttackingAgentSnapshot.Pace`'s XML
     (`"Convention: (raw − 1) / 19"`). Neutral `10 → 0.4737`. Maps the full range to exactly
     `[0,1]` (`1→0`, `20→1`); `÷20` maps to `[0.05, 1.0]`.

   So the one live `[0,1]` seed (`0.5`) **matches `÷20` but contradicts that struct's own stated
   `(raw−1)/19` convention.** These fields are documented "declared for Stage 1+ … not consumed," so
   nothing observable depends on the discrepancy today.

**Decision (KD-P3, proof in §7):** copy **raw `[1,20]`** into every non-normalized site (any
internal normalization those consumers do — GK/Heading `÷20` — is out of scope and untouched;
changing it *would* rebaseline live behavior), and for the sole pre-normalized target
(`AttackingAgentSnapshot.pace/dribbling`) use **`÷ ATTR_MAX` (÷20)** so neutral = `0.5` and the
default path stays byte-identical (§7). The `(raw−1)/19`-vs-`÷20` struct-doc inconsistency is
**real, pre-existing, and unconsumed** — flagged as a **separate follow-up** (fix
`AttackingAgentSnapshot`'s doc-vs-neutral mismatch in its own pass), not folded into T1, since
resolving it via `(raw−1)/19` moves the neutral off `0.5` and costs the byte-identical default.

---

## 3. Field-by-field projection mapping

Recommended seam: a pure static `PlayerAttributeProjection`, one method per **live** target, taking
the canonical record + the explicit **runtime** inputs the caller owns (§5), so the whole
attribute-sourcing surface is one auditable seam (the `BuildPassAttributes(i)` style generalized):

```
static AgentMovement.PlayerAttributes     ToAgentMovement(in PlayerAttributes c);   // starters + bench
static DecisionTree.DtAgentAttributes      ToDecisionTree(in PlayerAttributes c, int teamId);
static Perception.PerceptionAgentAttributes ToPerception(in PlayerAttributes c, int teamId, bool isHalfTurned);
static PassMechanics.PassAgentAttributes   ToPass(in PlayerAttributes c, float fatigue);
static ShotMechanics.ShotAgentAttributes   ToShot(in PlayerAttributes c, float fatigue);
static int   FirstTouchAbility(in PlayerAttributes c);          // the 3 §1 first-touch sites
static float ToNormalized(int canonical1to20);                  // = canonical / ATTR_MAX (20) — KD-P3
// Forward-compat, NOT wired in T1 (§0): ToGoalkeeper / ToHeading — defined in §3.6/§3.7 for when
// #10/#11 are integrated into the engine.
```

Every row is a **raw `[1,20]` copy** unless the Scale column says otherwise. "Neutral→" is the value
produced when the canonical field is neutral (`10`, or WeakFoot `3`); it must equal the field's
current `STAGE0_*` seed for §7 to hold.

### 3.1 `ToAgentMovement` (starters `_attrs` **and** bench `_benchAttrs`)
Pace, Acceleration, Agility, Balance, Strength, Stamina — raw `int` copy. Neutral→ 10 each.

### 3.2 `ToDecisionTree` (TeamId ← caller)
Passing, Finishing, Dribbling, LongShots, Crossing, Composure, Anticipation, Decisions, Vision,
Pace, Agility, WorkRate, Stamina, Aggression, Positioning — raw `int` copy of the identically-named
canonical field. Neutral→ 10 each. (`Crossing` stays declared-but-unconsumed per `ERR-008-006`.)

### 3.3 `ToPerception` (TeamId, IsHalfTurned ← caller)
Decisions, Anticipation — raw `int`. Neutral→ 10.

### 3.4 `ToPass` (Fatigue ← caller)
| Target | ← canonical | Scale | Neutral→ |
|---|---|---|---|
| Passing | Passing | int→float | 10 |
| Technique | Technique | int→float | 10 |
| KickPower | **derived** `(Passing+Technique)×0.5f` | float | 10 |
| WeakFootRating | WeakFootRating | raw int `[1,5]` | 3 |
| Crossing | Crossing | int→float | 10 |

### 3.5 `ToShot` (Fatigue ← caller)
| Target | ← canonical | Scale | Neutral→ |
|---|---|---|---|
| Finishing | Finishing | raw int | 10 |
| LongShots | LongShots | raw int | 10 |
| Composure | Composure | raw int | 10 |
| KickPower | **derived** `RoundToInt((Finishing+LongShots)×0.5f)` (§4, L-1) | int | 10 |
| Technique | Technique | raw int | 10 |
| WeakFootRating | WeakFootRating | raw int `[1,5]` | 3 |

### 3.5a `FirstTouchAbility` (the 3 §1 first-touch sites)
`← canonical FirstTouchAbility`, raw copy (float at the #13/#14 snapshot sites, `int` — the same
`RoundToInt` the current site uses — at the #4 `FirstTouchContext` site). Neutral→ 10, matching
`STAGE0_NEUTRAL_ATTRIBUTE` at all three sites. Projected for **every** agent (no GK gate — a
first-touch attribute is meaningful for outfield and GK alike).

### 3.6 `ToHeading` — **forward-compat, NOT a T1 target (§0)**
Heading, Strength, Balance ← identically-named canonical, raw int. Neutral→ 10. Defined for the
eventual #10 integration; MatchEngine builds no `HeadingAgentAttributes` today.

### 3.7 `ToGoalkeeper` — **forward-compat, NOT a T1 target (§0); built iff GK when wired (§6)**
Reflexes, Handling, Composure, Strength, Aerial, Balance, OneVsOne, Pace, Throwing, Kicking ←
identically-named canonical, `int→float`. Neutral→ 10.0 (the `Norm` accessors then map `10.0/20 =
0.5`). Defined for the eventual #11 integration; MatchEngine builds no `GoalkeeperAgentAttributes`
today.

### 3.8 Attacking snapshot (inline at `FillAttackingSnapshot`)
| Param | ← canonical | Scale | Neutral→ |
|---|---|---|---|
| pace | Pace | `÷ ATTR_MAX` (KD-P3) | 0.5 |
| dribbling | Dribbling | `÷ ATTR_MAX` | 0.5 |
| stamina | (runtime `1 − AerobicPool`) | — | — |

---

## 4. KickPower has no canonical source (KD-P1)

The canonical 31 has `Passing`, `Technique`, `Finishing`, `LongShots`, `Kicking` — but **no generic
`KickPower`.** Both `PassAgentAttributes` and `ShotAgentAttributes` carry one, today flat at
`STAGE0_NEUTRAL_ATTRIBUTE` (Pass's tagged `[TEMPORARY-PROXY-ERR-007]` = `(Passing+Technique)×0.5`).

- **(a) Add a 32nd canonical `KickPower`** — rejected: no master-plan §4.2 trait; breaks the
  `AttrIdx.Count = 31` / `FIELDS_PER_PLAYER` locks the #27 T0 pinned.
- **(b) Derive per-consumer** — chosen. `ToPass.KickPower = (Passing+Technique)×0.5f`;
  `ToShot.KickPower = (Finishing+LongShots)×0.5f`, rounded. Any weighted mean of neutral-`10` inputs
  is `10`, so both preserve the neutral seed (§7). This is the concrete `ERR-007` closure (T2): the
  proxies become functions of real varied attributes, not "all neutral."

**L-1 (rounding determinism):** `ToShot.KickPower` rounds a possibly-half-integer average to `int`.
Pin the rounding to `Mathf.RoundToInt` — the exact call the current seed site already uses
(`Mathf.RoundToInt(STAGE0_NEUTRAL_ATTRIBUTE)`, MatchEngine.cs:3577) — so the derivation is
deterministic and consistent with the rest of the engine. Pass's `KickPower` is `float` and needs no
rounding.

---

## 5. Runtime vs. attribute boundary (P1-M2)

Non-attribute fields on the targets must **not** come from the `Squad`:

- **Fatigue `[0,1]`** (Pass/Shot; also Heading/GK when wired): live state, already `1 − AerobicPool`.
  Stays runtime. This is why `_perfs`/fatigue is **out of T1** (the `_attrs`/`_perfs` EXCLUSION
  PROOF couples them; only `_attrs`-class data moves).
- **TeamId** (DT/Perception; Heading/GK when wired): match-scoped, from `_teamIds[i]`, never the club
  roster (KD-3).
- **IsHalfTurned** (Perception): runtime body stance.

The projection functions take `(in PlayerAttributes, <runtime params>)` and are pure over the
attribute half; the caller supplies the runtime half exactly as today. No runtime-field behavior
changes.

---

## 6. GK routing contract (P1-M1) — forward-compat

When #11 is eventually wired, `ToGoalkeeper` is called **iff `_isGoalkeeper[i]`** (the gate
MatchEngine already uses to decide whether an agent is a keeper). Outfield agents never get a
`GoalkeeperAgentAttributes` built, so their low goalkeeping canonical values are never read — no
leakage, no zeroing. `_isGoalkeeper[i]` is **already serialized** (MatchEngine.cs:2890), so on
restore the GK/outfield routing re-resolves with **no new state**. Invariant to assert at that
future seam: `ToGoalkeeper` reached only for `_isGoalkeeper` agents (fail-loud in dev builds). This
is a routing contract for the #11 integration, not a T1 deliverable.

The one live routing note for T1: `FirstTouchAbility` (§3.5a) is projected for **all** agents, so it
has no GK gate.

---

## 7. Default-neutrality: proven for the default path, no rebaseline (P1-H2)

Grounding the live seeds refutes the review's pessimism (P1-H2, "per-spec CreateDefaults disagree").
Every live seed is the same neutral — `10` on `[1,20]`, `3` on WeakFoot, `0.5` on the one normalized
target — and the canonical neutral (`CreateDefault`) projects to exactly those via §3:

| Live site | Projection of canonical-neutral | Current seed | Match? |
|---|---|---|---|
| AgentMovement (starters+bench) | all 10 | `CreateDefault` all 10 | ✓ |
| DecisionTree | all 10 | `STAGE0_NEUTRAL_ATTRIBUTE` | ✓ |
| Perception | 10 | `CreateDefault` 10 | ✓ |
| Pass | 10; KickPower `(10+10)×.5=10`; WeakFoot 3 | `STAGE0_NEUTRAL_*` | ✓ |
| Shot | 10; KickPower `Round((10+10)×.5)=10`; WeakFoot 3 | `STAGE0_NEUTRAL_*` | ✓ |
| FirstTouchAbility ×3 | 10 (Round at #4) | `STAGE0_NEUTRAL_ATTRIBUTE` | ✓ |
| Attacking pace/dribbling | `10 ÷ 20 = 0.5` | `STAGE0_NEUTRAL_NORMALIZED` 0.5 | ✓ |

(GK/Heading are absent — no MatchEngine seed exists to match, consistent with §0.)

**So T1 needs no digest rebaseline.** Precise guarantee:

> A match booted with **no `Squad`** (or an all-`CreateDefault` `Squad`) produces **byte-identical**
> seeding to pre-T1 → the capstone / away-team / schema digests do not move. A match booted with
> **distinct** squads diverges *by design* (that is the point of T1 — #27 §4).

### 7.1 Restore scope (M-2) — distinct-squad restore is a T3 concern, not T1

`_attrs`, `_dtAttrs`, `_perceptionAttrs`, and the bench roster attributes are **not serialized**
(the boot-deterministic `_attrs`/`_perfs` exclusion proof, MatchEngine.cs:2849). Only
`_activeBenchSlot` **is** serialized (MatchEngine.cs:3054). Consequences for T1:

- **Default path:** on restore, everything re-seeds from `CreateDefault` (no `Squad`) → byte-
  identical to pre-restore. Safe.
- **Distinct-squad path:** on restore in a fresh process there is **no `Squad` to re-project from**,
  so `_attrs` cannot be reconstructed — the match is **not restore-deterministic**. This includes a
  mid-match **substitution**, which swaps `_attrs[outSlot] = _benchAttrs[...]` (MatchEngine.cs:825);
  with distinct attributes that swap is restore-relevant state, reconstructible only by re-projecting
  the bench roster via the serialized `_activeBenchSlot` — which needs the `Squad`. This is the exact
  hazard the root CLAUDE.md v2.20 substitution-attrs note flagged, and the ERR-021-002 class.

Therefore: **T1's byte-identical guarantee and restore-safety are scoped to the default path.**
Distinct-squad attribute restore requires the **T3 roster reference** (snapshot header roster id +
a restore-time re-projection keyed by `_activeBenchSlot`), which is **out of this doc's scope**
(#27 T3 / Plan-4). T1 must either land distinct-squad support *with* T3, or explicitly document that
distinct-squad matches are not restore-safe until T3 — it must not ship a distinct-squad path that
silently diverges on restore.

---

## 8. CS0104 — fully-qualify from line one (carried from #27 §4)

`PlayerDatabase.PlayerAttributes` collides by bare name with `AgentMovement.PlayerAttributes` the
moment `match-engine` references `player-database` (the `src/CLAUDE.md` v1.73 five-`TacticTranslation`
defect class). The projection layer **must** fully-qualify
`TacticalDirector.PlayerDatabase.PlayerAttributes` (and `TacticalDirector.AgentMovement.PlayerAttributes`
where both appear) from the first line; a compile-clean test (§9) guards it so the collision is
caught by the gate, not a red build.

---

## 9. Test plan (T1/T2)

- **Per-field scale locks:** each §3 row with a *distinct* (non-10) canonical input produces the
  exact expected target — raw copy for raw rows; `(Passing+Technique)×.5` (Pass) and
  `RoundToInt((Finishing+LongShots)×.5)` (Shot) for KickPower; `÷20` for Attacking pace/dribbling;
  `FirstTouchAbility` for the three first-touch sites. WeakFoot `[1,5]` round-trips (never `[1,20]`-clamped).
- **Neutral-equivalence locks (§7):** `projection(CreateDefault())` equals each **live** site's
  current seed, field-by-field (incl. the three FirstTouch sites and the Attacking `0.5` pair). GK/
  Heading excluded (no live seed).
- **Default-path digest lock:** the capstone / away-team scenarios booted through the T1 no-squad
  path yield the *same* `CurrentSnapshotDigest` chain as pre-T1.
- **Distinct-squad divergence:** a non-neutral squad seeds differently than `CreateDefault` (wiring
  is live).
- **Restore scope (§7.1):** a distinct-squad match is documented/asserted not-restore-safe until T3
  — e.g. a test that a fresh-process restore without a roster reference re-seeds to `CreateDefault`
  (the divergence is explicit, not silent), OR distinct-squad support is gated off until T3.
- **CS0104 compile-clean:** projection assembly + a match-engine reference compile with both
  `PlayerAttributes` types in scope.
- **Runtime-fields-untouched:** Fatigue/TeamId/IsHalfTurned equal the caller-supplied runtime values,
  independent of the canonical record.

No *new* closed-loop `ScenarioRunner` scenario here — T1 rides the existing capstone/away-team
scenarios (their digests are the neutrality oracle); a varied-squad scenario is Plan-5.

---

## 10. Key decisions

- **KD-P1 (KickPower).** No canonical `KickPower`; derive per-consumer (`Pass=(Passing+Technique)×.5`,
  `Shot=RoundToInt((Finishing+LongShots)×.5)`). Neutral-preserving. Dedicated trait deferred.
- **KD-P2 (raw copy, internal normalization untouched).** Project raw `[1,20]` into all non-normalized
  live sites; consumers' internal `÷20` normalization is unchanged (out of scope).
- **KD-P3 (one normalized target, `÷20`).** `AttackingAgentSnapshot.pace/dribbling` normalize
  `÷ ATTR_MAX` so neutral = `0.5` = today's seed. The struct-doc `(raw−1)/19` mismatch is a flagged,
  unconsumed, **pre-existing** defect fixed separately (§2).
- **KD-P4 (runtime split).** Fatigue/TeamId/IsHalfTurned are caller-supplied runtime, never from the
  `Squad`; `_perfs`/fatigue out of T1.
- **KD-P5 (GK gate, forward-compat).** `ToGoalkeeper` iff `_isGoalkeeper[i]`; `FirstTouchAbility`
  projected for all agents (no gate).
- **KD-P6 (fully-qualify).** `TacticalDirector.PlayerDatabase.PlayerAttributes` from line one;
  compile-clean test guards CS0104.
- **KD-P7 (neutral proven).** No digest rebaseline; the no-squad path is byte-identical, guarded by
  the neutral-equivalence + default-path digest locks.
- **KD-P8 (GK/Heading not wired — AR-2 H-1).** `ToGoalkeeper`/`ToHeading` are forward-compat mappings,
  **not T1 targets** — MatchEngine has no build site for either (#10/#11 unwired). Writing them into
  T1 would be a phantom consumer. They land when those specs are engine-integrated.
- **KD-P9 (`FirstTouchAbility` is consumed — AR-2 M-1).** Three live sites (#13/#14/#4) consume it as
  a neutral placeholder; it is a T1/T2 target, not RESERVED. #27's reserved-list entry for it is
  inaccurate (separate one-line correction).
- **KD-P10 (restore scope — AR-2 M-2).** T1's byte-identical + restore guarantee is the default path
  only; distinct-squad restore (incl. substitution bench-swaps via `_activeBenchSlot`) needs the T3
  roster reference and is out of scope here. **(T3 LANDED July 18, 2026 —
  `squad-roster-reference-design.md`: the per-team roster reference `Squad.ClubId` is now serialized
  at `SNAPSHOT_SCHEMA_VERSION` 16, so the identity half of the re-projection is captured; the restore
  re-projection keyed by `_activeBenchSlot` stays future work — the match engine has no
  snapshot-deserialize path yet.)**

---

## 11. Self-adversarial review

**AR-1 (v0.1) — folded in at authoring:** the `(raw−1)/19` vs `÷20` normalization inconsistency
(→ §2/KD-P3); default-neutrality proven rather than "maybe rebaseline" (→ §7); Shot KickPower
derivation pinned (→ §4); GK-routing restore story (→ §6).

**AR-2 (v0.1 → v0.2) — fresh-eyes sweep of every attribute-seeding site in `MatchEngine`, verified
against source. 2 H + 1 M + 1 L, all fixed in place:**

- **H-1 (phantom consumer):** v0.1 listed all seven per-spec structs as equal T1 targets, but a
  source sweep (`grep new GoalkeeperAgentAttributes / BuildGk / BuildHeading` → nothing) shows
  `MatchEngine` builds **no** `GoalkeeperAgentAttributes` or `HeadingAgentAttributes` — #10/#11 are
  not wired into the tick pipeline. Projecting into them in T1 is building against a non-existent
  consumer, and v0.1 §7's neutrality table claimed a "current seed" for GK that does not exist.
  Fixed: §0/§1/§3.6/§3.7/KD-P8 reclassify GK/Heading as forward-compat mappings, explicitly not T1
  targets; §7 drops their phantom "current seed" rows.
- **H-2 (incomplete inventory):** v0.1's target set missed three live attribute-seeding sites —
  `PressingAgentSnapshot.FirstTouchAttribute` (1872), `DefensiveAgentSnapshot.PerceivedFirstTouch`
  (1977), `FirstTouchContext.FirstTouchAttribute` (2775) — all mapping to canonical `FirstTouchAbility`,
  which v0.1 (and #27) called RESERVED/unconsumed. Fixed: §1/§3.5a/§7/KD-P9 add the three sites +
  the `FirstTouchAbility` projection and reclassify the field as consumed; #27's reserved-list is
  flagged for a separate correction.
- **M-1 (restore scope):** v0.1 §6/§7 implied restore fidelity broadly. Source confirms `_attrs`/
  `_dtAttrs`/bench attrs are not serialized while `_activeBenchSlot` is (3054), so a distinct-squad
  match — including a substitution's bench-attr swap — is not restore-deterministic until the T3
  roster reference. Fixed: new §7.1 + KD-P10 scope the guarantee to the default path and defer
  distinct-squad restore to T3, cross-referencing the v2.20 substitution-attrs hazard.
- **L-1 (rounding determinism):** Shot KickPower's round-to-int mode was unpinned. Fixed: §4 pins
  `Mathf.RoundToInt`, matching the existing neutral site.

**AR-3 (v0.2 → v0.3) — re-verify the corrected inventory against source. 0 H + 0 M + 1 L — CONVERGED.**
The pass swept every attribute-seeding site in `MatchEngine`: (A) every `STAGE0_NEUTRAL_*` reference
maps exactly to a §1 row (no missed site); (B) the derived consumers — the #13 CoverShadowCurve
attributes at 1884–1886 — are computed from `_dtAttrs`, so they flow transitively once DT is projected
and are correctly *not* seeding sites; (C) `grep "new .*AgentAttributes"` in match-engine returns only
`Perception`/`Dt`/`Pass`/`Shot` — **zero** `Goalkeeper`/`Heading`, confirming AR-2 H-1's reclassification.
**L-1 (fixed):** added the "derived consumers flow transitively" note to §1 so the exhaustiveness of the
inventory is explicit (the class of omission that caused AR-2 H-2). No H/M.

**Cycle status: CONVERGED at AR-3** (an L-only round ends the cycle, per the project convention —
match-viewer AR-4 / squad-player-data AR-2 precedent). The corrected v0.2 inventory verified exhaustive
against source; GK/Heading forward-compat scoping and the three FirstTouch sites re-checked clean.

---

#### Version History
| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-17 | Initial draft — projection mapping for #27 T1/T2. AR-1 self-review folded in. |
| 0.2 | 2026-07-17 | AR-2 (2H+1M+1L, all fixed): H-1 GK/Heading are not MatchEngine-seeded → reclassified forward-compat, not T1 targets (phantom-consumer avoidance); H-2 three missed `FirstTouchAbility` sites (#13/#14/#4) added, field reclassified consumed-not-reserved; M-1 distinct-squad restore scoped to T3 (§7.1); L-1 Shot KickPower rounding pinned to `Mathf.RoundToInt`. Inventory reframed as "seeding sites MatchEngine has," not "seven structs." Cycle NOT converged — AR-3 pending. |
| 0.3 | 2026-07-17 | AR-3 (0H+0M+1L): swept every seeding site against source — `STAGE0_NEUTRAL_*` set exact, derived `_dtAttrs` consumers (#13 CoverShadowCurve 1884–1886) flow transitively, `grep new *AgentAttributes` confirms zero GK/Heading in match-engine. L-1 fixed: explicit "inventory exhaustive / derived consumers" note in §1. **CONVERGED** (L-only round). |
| 0.4 | 2026-07-17 | Implementation-time inventory correction (T1 code review, not a design-stage AR round — the squad-player-data v0.4 precedent): the §1 inventory row for the #4 site listed only `FirstTouchContext.FirstTouchAttribute` (2775), but the SAME construction site also seeds `FirstTouchContext.Technique` from the identical rounded `STAGE0_NEUTRAL_ATTRIBUTE` local (2773) — an AR-2-H-2-class omission the AR-3 sweep's per-line `STAGE0_NEUTRAL_*` grep matched but the row text under-reported. T1 projects it as a raw copy of canonical `Technique` (neutral → 10, the exact pre-T1 seed — KD-P7 unaffected). Also recorded: T1 landed `ConfigureSquads` with the Stage-0 roster-order lineup mapping (player 0 → the GK slot; lineup selection proper stays Plan-3), and the KD-P10 restore scope is documentation-only today because no restore path exists in `MatchEngine`. |
