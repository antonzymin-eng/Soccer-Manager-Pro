# Player-Attribute Projection — Design Supplement (Plan 1, #27 T1/T2)

> **Created:** July 17, 2026
> **Status:** DESIGN SUPPLEMENT (pre-code — no section files, no `SPEC_INDEX.md` row).
> Companion to `docs/tracking/squad-player-data-design.md` (candidate spec **#27**); this doc is
> the detailed design for that supplement's deferred **§4 T1/T2 wiring** — specifically the
> field-by-field projection from the canonical `PlayerDatabase.PlayerAttributes` record into the
> seven per-spec attribute structs, the scale semantics of each projection, and the
> default-neutrality guarantee that keeps the no-squad match byte-identical to today.
> **Purpose:** Turn "wire `MatchEngine` to source attributes from a `Squad`" from a mechanical
> hand-wave into a reviewed mapping with explicit scale conversions, a proven neutral path, and a
> pinned decision for every field that has no canonical source.

---

## 0. Scope and why this is its own doc

The #27 T0 landed the canonical data layer (`src/player-database/`) but deliberately left `MatchEngine`
seeding on `PlayerAttributes.CreateDefault()` / `STAGE0_NEUTRAL_ATTRIBUTE`. The #27 supplement §4
records T1 ("seed from a `Squad`") and T2 ("close `ERR-007` for real") as one-line deferrals. The
corrected Plan-1 review (this session) found that those two lines hide the real work: a **per-field
projection with scale semantics** (P1-H1), a **default-neutrality proof or a planned digest
rebaseline** (P1-H2), a **GK-routing contract** (P1-M1), and a **runtime-vs-attribute boundary**
(P1-M2). This doc is that work, at design-supplement rigor, before any code.

**In scope:** the pure projection `canonical PlayerAttributes → each of the 7 per-spec structs`;
its scale rules; the neutral-path proof; the runtime/attribute split; the GK routing contract; the
CS0104 hazard; the T1/T2 test plan.

**Out of scope (unchanged from #27 §0):** aging/training/transfers; the on-disk save format; the
snapshot roster-reference field (that is #27 T3 / Plan-4's concern, designed separately). This doc
does **not** design the `MatchSquads` input container or lineup selection — those are Plan-1c /
Plan-3, downstream of this projection.

---

## 1. The two sides (grounded in source)

**Canonical source** — `src/player-database/PlayerAttributes.cs`: 31 `int [1,20]` fields (in
`AttrIdx` order) + `WeakFootRating int [1,5]`. `CreateDefault()` = every `[1,20]` field `10`,
`WeakFootRating = 3`.

**Seven target structs** (each read from source this session):

| # | Struct | File | Storage | Attribute fields | Non-attribute fields (NOT projected — §5) |
|---|---|---|---|---|---|
| 1 | `AgentMovement.PlayerAttributes` | `agent-movement/PlayerAttributes.cs` | `int [1,20]` | Pace, Acceleration, Agility, Balance, Strength, Stamina | — (fatigue lives in `PerformanceContext`, separate) |
| 2 | `DecisionTree.DtAgentAttributes` | `decision-tree/DtAgentAttributes.cs` | `int [1,20]` | Decisions, Vision, Passing, Finishing, Dribbling, LongShots, Crossing, Composure, Anticipation, Pace, Agility, WorkRate, Stamina, Aggression, Positioning | TeamId |
| 3 | `PerceptionSystem.PerceptionAgentAttributes` | `perception-system/…` | `int [1,20]` | Decisions, Anticipation | TeamId, **IsHalfTurned** (runtime body stance) |
| 4 | `PassMechanics.PassAgentAttributes` | `pass-mechanics/…` | `float` (holds `[1,20]`) | Passing, Technique, **KickPower** (proxy §4), WeakFootRating `[1,5]`, Crossing | Fatigue `[0,1]` |
| 5 | `ShotMechanics.ShotAgentAttributes` | `shot-mechanics/…` | `int [1,20]` | Finishing, LongShots, Composure, **KickPower** (proxy §4), Technique, WeakFootRating `[1,5]` | Fatigue `[0,1]` |
| 6 | `HeadingMechanics.HeadingAgentAttributes` | `heading-mechanics/…` | `int [1,20]` | Heading, Strength, Balance | Fatigue `[0,1]`, TeamId |
| 7 | `GoalkeeperMechanics.GoalkeeperAgentAttributes` | `goalkeeper-mechanics/…` | `float` (holds `[1,20]`; `Norm` accessors ÷20) | Reflexes, Handling, Composure, Strength, Aerial, Balance, OneVsOne, Pace, Throwing, Kicking | Fatigue `[0,1]`, TeamId |

Plus one **pre-normalized** consumer that is not a struct but a snapshot constructor:
`AttackingAI.AttackingAgentSnapshot(pace, stamina, dribbling)` takes `pace`/`dribbling` as
**`[0,1]` normalized** (currently seeded `STAGE0_NEUTRAL_NORMALIZED = 0.5`; `stamina` is already
runtime `1 − AerobicPool`). This is the single projection target that is not a raw `[1,20]` copy.

---

## 2. Scale reconciliation — the core P1-H1 finding

The review anticipated a broad "`[1,20]` canonical vs `[0,1]` consumers" split. The source says
something narrower and more precise:

1. **Every attribute struct stores raw `[1,20]`.** Structs 1–7 hold `[1,20]` values (structs 4 & 7
   use `float` but the values are `[1,20]`; GK/Heading normalize *internally* at their consumption
   sites, not at the struct boundary). So the projection into all seven structs is a **raw
   value copy** — `int→int` or lossless `int→float` widening — with **no scale conversion at the
   seam**. WeakFootRating stays `[1,5]` (KD-2, already isolated). This refutes the broad reading
   of P1-H1: there is no `[0,1]` field on any of the seven structs.

2. **Exactly one projection target is pre-normalized:** `AttackingAgentSnapshot.pace/dribbling`
   (`[0,1]`). Here the projection *must* convert `[1,20] → [0,1]`.

3. **The codebase carries two contradictory `[1,20]→[0,1]` conventions** — and the projection must
   pin one:
   - **`÷ ATTR_MAX` (÷20):** `GoalkeeperAgentAttributes.Clamp01Norm` and `HeadingMechanicsConstants.ATTR_MAX`.
     Neutral `10 → 0.5`. This is what the live `STAGE0_NEUTRAL_NORMALIZED = 0.5` seed matches.
   - **`(raw − 1) / 19`:** the **documented** convention on `AttackingAgentSnapshot.Pace`'s own XML
     (`"Convention: (raw − 1) / 19"`). Neutral `10 → 0.4737`. This maps the full range to exactly
     `[0,1]` (`1→0`, `20→1`); `÷20` maps to `[0.05, 1.0]` and never reaches 0.

   So the one live `[0,1]` seed (`0.5`) **matches `÷20` but contradicts the very struct's stated
   `(raw−1)/19` convention.** These fields are documented "declared for Stage 1+ … not consumed,"
   so nothing observable depends on the discrepancy today — but a projection that normalizes here
   has to choose, and the choice interacts with the neutrality proof (§7).

**Decision (KD-P3, see §7 for the proof it preserves):** the projection copies **raw `[1,20]`**
into all seven structs (their internal normalization is out of scope and untouched — changing
GK/Heading `÷20` *would* rebaseline live behavior), and for the sole pre-normalized target
(`AttackingAgentSnapshot.pace/dribbling`) it uses **`÷ ATTR_MAX` (÷20)** so the neutral case yields
exactly `0.5` and the default path stays byte-identical (§7). The `(raw−1)/19`-vs-`÷20` struct-doc
inconsistency is **real but pre-existing and unconsumed**; it is flagged here as a **separate
follow-up** (fix `AttackingAgentSnapshot`'s doc-vs-neutral mismatch in its own pass) rather than
entangled with the projection landing — resolving it via `(raw−1)/19` would move the neutral off
`0.5` and cost the byte-identical default this design otherwise guarantees.

---

## 3. Field-by-field projection mapping

Signature (recommended): a pure static `PlayerAttributeProjection` with one method per target,
taking the canonical record and the explicit **runtime** inputs the caller owns (§5), so the whole
attribute-sourcing surface is one auditable seam (the `BuildPassAttributes(i)` style, generalized):

```
static AgentMovement.PlayerAttributes        ToAgentMovement(in PlayerAttributes c);
static DecisionTree.DtAgentAttributes         ToDecisionTree(in PlayerAttributes c, int teamId);
static PerceptionSystem.PerceptionAgentAttributes ToPerception(in PlayerAttributes c, int teamId, bool isHalfTurned);
static PassMechanics.PassAgentAttributes      ToPass(in PlayerAttributes c, float fatigue);
static ShotMechanics.ShotAgentAttributes      ToShot(in PlayerAttributes c, float fatigue);
static HeadingMechanics.HeadingAgentAttributes ToHeading(in PlayerAttributes c, float fatigue, int teamId);
static GoalkeeperMechanics.GoalkeeperAgentAttributes ToGoalkeeper(in PlayerAttributes c, float fatigue, int teamId);
// Attacking snapshot pace/dribbling are produced inline at the FillAttackingSnapshot seam:
static float ToNormalized(int canonical1to20);   // = canonical / ATTR_MAX (20)  — KD-P3
```

Every row below is a **raw `[1,20]` copy** unless the Scale column says otherwise. "Neutral→" is the
value the row produces when the canonical field is its neutral (`10`, or WeakFoot `3`); it must equal
the field's current `STAGE0_*` seed for §7 to hold.

### 3.1 `ToAgentMovement`
| Target field | ← canonical | Scale | Neutral→ |
|---|---|---|---|
| Pace | Pace | raw int | 10 |
| Acceleration | Acceleration | raw int | 10 |
| Agility | Agility | raw int | 10 |
| Balance | Balance | raw int | 10 |
| Strength | Strength | raw int | 10 |
| Stamina | Stamina | raw int | 10 |

### 3.2 `ToDecisionTree` (TeamId ← caller)
Passing, Finishing, Dribbling, LongShots, Crossing, Composure, Anticipation, Decisions, Vision,
Pace, Agility, WorkRate, Stamina, Aggression, Positioning — each a raw `int` copy of the
identically-named canonical field. Neutral→ 10 each. (`Crossing` stays declared-but-unconsumed per
`ERR-008-006`; projecting it is forward-compat, not activation.)

### 3.3 `ToPerception` (TeamId, IsHalfTurned ← caller)
| Target | ← canonical | Scale | Neutral→ |
|---|---|---|---|
| Decisions | Decisions | raw int | 10 |
| Anticipation | Anticipation | raw int | 10 |

### 3.4 `ToPass` (Fatigue ← caller)
| Target | ← canonical | Scale | Neutral→ |
|---|---|---|---|
| Passing | Passing | int→float | 10 |
| Technique | Technique | int→float | 10 |
| KickPower | **derived** (§4) `(Passing+Technique)×0.5` | int→float | 10 |
| WeakFootRating | WeakFootRating | raw int `[1,5]` | 3 |
| Crossing | Crossing | int→float | 10 |

### 3.5 `ToShot` (Fatigue ← caller)
| Target | ← canonical | Scale | Neutral→ |
|---|---|---|---|
| Finishing | Finishing | raw int | 10 |
| LongShots | LongShots | raw int | 10 |
| Composure | Composure | raw int | 10 |
| KickPower | **derived** (§4) `(Finishing+LongShots)×0.5` | raw int (round) | 10 |
| Technique | Technique | raw int | 10 |
| WeakFootRating | WeakFootRating | raw int `[1,5]` | 3 |

### 3.6 `ToHeading` (Fatigue, TeamId ← caller)
| Target | ← canonical | Scale | Neutral→ |
|---|---|---|---|
| Heading | Heading | raw int | 10 |
| Strength | Strength | raw int | 10 |
| Balance | Balance | raw int | 10 |

### 3.7 `ToGoalkeeper` (Fatigue, TeamId ← caller; built **iff GK** — §6)
Reflexes, Handling, Composure, Strength, Aerial, Balance, OneVsOne, Pace, Throwing, Kicking — each
`int→float` copy of the identically-named canonical field. Neutral→ 10.0 each (GK `Norm` accessors
then map `10.0/20 = 0.5`, unchanged from today).

### 3.8 Attacking snapshot (inline at `FillAttackingSnapshot`)
| Param | ← canonical | Scale | Neutral→ |
|---|---|---|---|
| pace | Pace | `÷ ATTR_MAX` (KD-P3) | 0.5 |
| dribbling | Dribbling | `÷ ATTR_MAX` | 0.5 |
| stamina | (runtime `1 − AerobicPool`) | — | — |

---

## 4. KickPower has no canonical source (KD-P1)

The canonical 31 has `Passing`, `Technique`, `Finishing`, `LongShots`, `Kicking` — but **no generic
`KickPower`.** Both `PassAgentAttributes` and `ShotAgentAttributes` carry a `KickPower` field, today
seeded flat to `STAGE0_NEUTRAL_ATTRIBUTE` (Pass's is tagged `[TEMPORARY-PROXY-ERR-007]`, documented
as `(Passing+Technique)×0.5`). Options:

- **(a) Add a 32nd canonical `KickPower` attribute.** Rejected: the master-plan §4.2 attribute list
  has no such trait; expanding the canonical record for a proxy is a data-model decision that
  belongs to a real spec pass, and it would break the `AttrIdx.Count = 31` / `FIELDS_PER_PLAYER`
  locks the #27 T0 pinned.
- **(b) Derive per-consumer from canonical fields.** Chosen. `ToPass.KickPower = (Passing +
  Technique)×0.5` (matches the existing proxy formula, now over *real varied* Passing/Technique
  instead of all-`10`). `ToShot.KickPower = (Finishing + LongShots)×0.5` (rounded to int) — a
  shooting-power proxy from the two shooting attributes, symmetric in construction with Pass. Any
  weighted mean of neutral-`10` inputs is `10`, so both preserve the neutral seed (§7).

**This is the concrete `ERR-007` closure (T2):** the proxies stop being "all neutral" and become
functions of real distinct attributes. A dedicated `KickPower`/leg-strength trait remains a
master-plan question, explicitly deferred — the derivation is the Stage-0-honest answer, not a
placeholder.

---

## 5. Runtime vs. attribute boundary (P1-M2)

Three field classes on the target structs are **not** attributes and must **not** come from the
`Squad`; the projection either omits them (caller fills) or takes them as explicit params:

- **Fatigue `[0,1]`** (Pass/Shot/Heading/GK): live match state, already `1 − AerobicPool` at every
  `Build*` site. Stays runtime. This is why `_perfs`/fatigue is **out of T1** (the
  `MatchEngine.cs` `_attrs`/`_perfs` EXCLUSION PROOF couples them; only `_attrs`-class data moves in
  this projection).
- **TeamId** (DT/Perception/Heading/GK): match-scoped (which side this agent is on), owned by
  `MatchEngine` from `_teamIds[i]`, never the club roster (KD-3).
- **IsHalfTurned** (Perception): runtime body stance, set per-heartbeat, not an attribute.

Consequence: the projection functions take `(in PlayerAttributes, <runtime params>)` and are pure
over the attribute half; the caller supplies the runtime half exactly as today. No behavior change
to any runtime field.

---

## 6. GK routing contract (P1-M1)

The six goalkeeping canonical fields (Reflexes/Handling/Aerial/OneVsOne/Throwing/Kicking) are
meaningful only for a goalkeeper. `ToGoalkeeper` is called **iff `_isGoalkeeper[i]`** — the same
gate `MatchEngine` already uses to decide whether to build `_gkAttrs` for an agent. Outfield agents
never get a `GoalkeeperAgentAttributes` built, so their (position-biased, low) goalkeeping canonical
values are simply never read — no leakage, no need to zero them. `_isGoalkeeper[i]` is **already
serialized** (`MatchEngine.cs:2890`), so on snapshot restore the GK/outfield routing re-resolves
correctly without any new state. Invariant to assert at the seam: `ToGoalkeeper` is reached only for
an agent whose `_isGoalkeeper` is true (a typed guard, fail-loud in dev builds).

---

## 7. Default-neutrality: proven, no rebaseline (P1-H2)

The review flagged P1-H2 pessimistically ("per-spec `CreateDefault`s disagree, byte-identical
default path may be impossible"). Grounding the seven structs **refutes that**: every struct's
neutral is the *same* — `10` on `[1,20]` fields, `3` on WeakFoot, `0.5` on the one normalized
consumer — and the canonical neutral (`CreateDefault` = all `10` / WeakFoot `3`) projects to exactly
those values via the §3 rules:

| Target | Projection of canonical-neutral | Current seed | Match? |
|---|---|---|---|
| AgentMovement | all 10 | `CreateDefault` all 10 | ✓ |
| DecisionTree | all 10 | `STAGE0_NEUTRAL_ATTRIBUTE` (10) | ✓ |
| Perception | Decisions/Anticipation 10 | `CreateDefault` 10 | ✓ |
| Pass | Passing/Technique/Crossing 10; KickPower `(10+10)×.5=10`; WeakFoot 3 | `STAGE0_NEUTRAL_*` (10 / 3) | ✓ |
| Shot | Finishing/LongShots/Composure/Technique 10; KickPower `(10+10)×.5=10`; WeakFoot 3 | `STAGE0_NEUTRAL_*` | ✓ |
| Heading | Heading/Strength/Balance 10 | `CreateDefault` 10 | ✓ |
| Goalkeeper | all 10.0 | `CreateDefault` 10.0 | ✓ |
| Attacking pace/dribbling | `10 ÷ 20 = 0.5` (KD-P3) | `STAGE0_NEUTRAL_NORMALIZED` 0.5 | ✓ |

**Therefore option (a) "proven-neutral" holds and no digest rebaseline is required.** The precise
guarantee T1 ships:

> A match booted with **no `Squad`** (or an all-`CreateDefault` `Squad`) produces **byte-identical**
> agent seeding to pre-T1, so the capstone / away-team / schema digests do not move. A match booted
> with **distinct** squads diverges *by design* (that is the point of T1 — #27 §4).

This is why T1 is "not behaviour-neutral" (varied squads change play) *and* safe (the default path
is provably unchanged) at once. **Verification obligation** (§9): a test that asserts
`projection(CreateDefault) ==` each struct's existing seed, field-by-field, and a capstone digest
that is unchanged when T1 lands with the default (no-squad) path. The one residual risk — whether
`AttackingAgentSnapshot.pace/dribbling` reach any serialized digest — is covered by keeping the
neutral at `0.5` (KD-P3); if a test shows they are neither serialized nor consumed (expected, since
`AttackingSnapshot` is a per-tick input and the fields are documented unconsumed), the `÷20`-vs-
`(raw−1)/19` convention choice is observationally free and the §2 follow-up can pick either later.

---

## 8. CS0104 — fully-qualify from line one (carried from #27 §4)

`PlayerDatabase.PlayerAttributes` collides by bare name with `AgentMovement.PlayerAttributes` the
moment `match-engine` references `player-database` (the exact class of defect from `src/CLAUDE.md`
v1.73, five `TacticTranslation` types in scope). The projection layer **must** fully-qualify
`TacticalDirector.PlayerDatabase.PlayerAttributes` (and `TacticalDirector.AgentMovement.PlayerAttributes`
where both appear) from the first line — a compile-clean test (§9) guards it so the collision is
caught by the gate, not discovered by a red build.

---

## 9. Test plan (T1/T2)

- **Per-field scale locks:** for each §3 row, a canonical input with a *distinct* value (not 10)
  produces the exact expected target value — proves raw-copy for the raw rows, `(Passing+Technique)×.5`
  for Pass KickPower, `(Finishing+LongShots)×.5` (rounded) for Shot KickPower, and `÷20` for the
  Attacking pace/dribbling row. WeakFoot stays `[1,5]` (a `[1,5]` input round-trips, never clamped by
  a `[1,20]` gate).
- **Neutral-equivalence locks (§7):** `projection(PlayerAttributes.CreateDefault())` equals each
  struct's current seed, field-by-field (7 structs + the Attacking `0.5` pair).
- **Default-path digest lock:** the capstone / away-team scenarios booted through the T1 no-squad
  path produce the *same* `CurrentSnapshotDigest` chain as pre-T1 (the byte-identical guarantee).
- **Distinct-squad divergence:** a squad with non-neutral attributes produces a *different* seeding
  than `CreateDefault` (proves the wiring is live, not dead).
- **GK routing:** `ToGoalkeeper` reached only for `_isGoalkeeper` agents; the fail-loud guard trips
  on an outfield call in dev builds.
- **CS0104 compile-clean:** the projection assembly + a match-engine reference compile with both
  `PlayerAttributes` types in scope.
- **Runtime-fields-untouched:** Fatigue/TeamId/IsHalfTurned on the projected structs equal the
  caller-supplied runtime values, independent of the canonical record.

No closed-loop `ScenarioRunner` scenario is *new* here — T1 rides the existing capstone/away-team
scenarios (their digests are the neutrality oracle); a varied-squad scenario is a Plan-5 concern.

---

## 10. Key decisions

- **KD-P1 (KickPower).** No canonical `KickPower`; derive per-consumer (`Pass = (Passing+Technique)×.5`,
  `Shot = (Finishing+LongShots)×.5`). Neutral-preserving. Dedicated trait deferred to a real spec.
- **KD-P2 (raw copy, internal normalization untouched).** Project raw `[1,20]` into all seven
  structs; GK/Heading keep their internal `÷20` normalization unchanged (changing it would rebaseline
  live behavior — out of scope).
- **KD-P3 (one normalized consumer, `÷20`).** `AttackingAgentSnapshot.pace/dribbling` normalize
  `÷ ATTR_MAX` so neutral = `0.5` = today's seed. The struct-doc's `(raw−1)/19` mismatch is a
  flagged, unconsumed, **pre-existing** defect to fix separately (§2), not folded into T1.
- **KD-P4 (runtime split).** Fatigue/TeamId/IsHalfTurned are caller-supplied runtime, never sourced
  from the `Squad`; `_perfs`/fatigue stays out of T1 (the exclusion-proof coupling).
- **KD-P5 (GK gate).** `ToGoalkeeper` iff `_isGoalkeeper[i]`; the flag is already serialized, so
  restore re-resolves routing with no new state.
- **KD-P6 (fully-qualify).** `TacticalDirector.PlayerDatabase.PlayerAttributes` fully-qualified from
  line one; compile-clean test guards CS0104.
- **KD-P7 (neutral proven).** No digest rebaseline; the no-squad path is byte-identical, guarded by
  the neutral-equivalence + default-path digest locks.

---

## 11. Self-adversarial review

**AR-1 (v0.1, this pass) — findings folded in above, per the fix-in-place convention:**

1. **The `(raw−1)/19` vs `÷20` inconsistency was not in v0.1's first draft table** — the first pass
   assumed all normalization was `÷20`. Reading `AttackingAgentSnapshot.Pace`'s own XML surfaced the
   documented `(raw−1)/19`, contradicting the live `0.5` seed. Folded into §2 as the core P1-H1
   substance and resolved by KD-P3 (keep `÷20` for byte-identity; flag the doc mismatch separately).
2. **v0.1 claimed default-neutrality "may need a rebaseline" (echoing the review's P1-H2).** Grounding
   the seven `CreateDefault`s showed they all agree at `10`/`3`/`0.5`, so the neutral path is provably
   byte-identical — §7 rewritten from "or rebaseline" to "proven, no rebaseline," with the guarantee
   stated precisely and a digest lock as its oracle.
3. **v0.1's Shot `KickPower` derivation was left unspecified** ("derive from something"). Pinned to
   `(Finishing+LongShots)×.5` — symmetric with Pass's proxy, neutral-preserving, and sourced from the
   two shooting attributes rather than an arbitrary field, so the choice is defensible not incidental.
4. **v0.1 did not state the GK-routing restore story.** Added to §6/KD-P5: `_isGoalkeeper` is already
   serialized, so no new snapshot state is needed for routing to survive restore — closing the loop
   with the Plan-1 T3 concern without pulling it into this doc.

**No open findings.** The one deliberately-deferred item (the `AttackingAgentSnapshot` doc-vs-neutral
convention mismatch) is recorded as a separate follow-up, not an open finding against this projection.
**CONVERGED** at v0.1 after one self-review round.

---

#### Version History
| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-17 | Initial draft — Plan-1 (#27 T1/T2) projection mapping. Grounded in all 7 target structs + the canonical record + the live seeding sites. AR-1 self-review folded in (normalization-convention inconsistency, proven neutrality, Shot KickPower derivation, GK restore). Converged. |
