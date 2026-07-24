# Youth Academy & Intake #42 — Design Supplement

> **Created:** July 24, 2026
> **Last Updated:** July 24, 2026 (v0.3 — AR-1 3M+1L → AR-2 2L → **CONVERGENCE**)
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> Candidate spec number **#42** (proposed in `management-layer-spec-roadmap.md` §6, reserved in
> `spec-plans/README.md`, not yet registered).
> **Governing plan:** `spec-plans/spec-42-youth-academy-intake.md` v0.1.
> **Wave:** 5 · **FR prefix (proposed):** FR-YA · **Determinism:** domain tag `0x2B` /
> `SubsystemOrdinals` 93 (proposed; pinned at promotion).
> **Purpose:** The club academy pipeline — an annual, deterministic **intake** of youth prospects,
> their **academy roster**, and **promotion** into the senior squad — built as a *consumer* of #28's
> generation machinery rather than a fork of it, with academy structure (facilities / youth coaching)
> modulating **prospect ceiling**, not the generator.

---

## 0. Scope

**In:** the per-club **academy state** (structure quality inputs + the current youth cohort), the
**annual intake event** that generates a cohort deterministically, the **academy roster** those
prospects live on until promoted, the **promotion** command that hands a prospect to the senior squad,
and (deep) **youth contracts**.

**Out (owned elsewhere, referenced as seams):**

- **The generation machinery (#28 Player Progression & Lifecycle).** #28 owns `RegenGenerator`, the
  CA/PA model (`AbilityModel`), and the `PlayerLifecycle` overlay. #42 **calls** `RegenGenerator`
  unmodified and **MUST NOT** fork or edit it (roadmap §3; #28 §7 already records the reciprocal —
  "#28 provides the machinery, #42 the quality dial").
- **Player development after intake (#28 / #29).** A generated prospect grows through #28's
  `GrowthProjection` fed by #29's `TrainingInput`. #42 adds **no** second growth path.
- **The canonical roster (#27 Squad / Player Data Layer).** `PlayerRecord` / `PlayerAttributes` /
  `Squad` are #27's. #42 produces records in that shape; it **never mutates a `Squad`** (the FR-PG-012
  discipline — #28 emits a result the roster owner applies; #42 does the same).
- **The season loop, tick order, and save root (#30).** #30 owns `RunWorldTickInFixedOrder`, the
  calendar, and `SeasonSaveCodec`. It **invokes** #42 at a new pre-declared tick-order slot and composes
  #42's opaque sub-blob. #42 never references #30.
- **Staff quality (#34) and club money (#40).** #34 publishes staff-quality projections and #40 owns
  budgets. #42 reads **neither directly** — it takes a plain `AcademyQuality` value input the
  composition root assembles (KD-3), so #42 builds no #34/#40 interface (FR-LW-031).
- **Scouting a prospect (#32).** Fog-of-war over youth is #32's; #42 exposes the prospect record, not a
  knowledge model. #32 does not exist yet — #42 builds nothing for it.

---

## 1. What exists vs. what #42 adds

**Verified present in `src/` (read, not assumed):**

| Surface | Where | What it gives #42 |
|---|---|---|
| `RegenGenerator.GenerateRegen(rng, streamIndex, clubId, newPlayerId, worldDay)` | `src/player-progression/RegenGenerator.cs` | A **stateless, pure** single-prospect generator returning `(PlayerRecord, PlayerLifecycle)` over one fixed `PROGRESSION_REGEN_FIELDS` reservation. **It takes `streamIndex` as a parameter** — so a caller may drive it from *its own* registered stream. This is what makes KD-1 possible without touching #28. |
| `AbilityModel.ComputeCA(in attributes, position)` | `src/player-progression/AbilityModel.cs` | CA is a **pure function of the attributes** — hence a derived cache, never independently writable (KD-2). |
| `PlayerLifecycle` (`PotentialAbility`, `CurrentAbility`, `GrowthCursor`, `BirthWorldDay`, retirement) | `src/player-progression/PlayerLifecycle.cs` | The overlay a prospect carries. `PotentialAbility` is drawn independently of the attributes ⇒ it is the one field an academy-quality dial can shift coherently. |
| `PROGRESSION_REGEN_FIELDS`, `REGEN_AGE_MIN`=16 / `REGEN_AGE_MAX`=20, `PA_MIN`=4000, `ABILITY_MAX`=10000, `REGEN_PA_HEADROOM`=1000, `DAYS_PER_YEAR`=365 | `src/player-progression/PlayerProgressionConstants.cs` | The exact bounds every #42 transform must respect. |
| `DeterministicRngService.RegisterStream(siteId, subsystemOrdinal, entityId, streamVersion)` + `Reserve` / `DrawReserved` / `CloseReservation` / `RestoreStream` / `GetStreamState` | `src/deterministic-sim/DeterministicRngService.cs` | Registration is **append-only into a bounded table** (`MaxRngStreams`, default **64**, no unregister); `Reserve`/`DrawReserved` free-run `RngCursor`/`ActionOrdinal`. Both facts decide KD-7. |
| `DeriveActionOrdinal(worldDay, purpose)` — the **position-independent keyed-draw idiom** | #41 §3.1.1 / §1 KD-1 | A draw keyed on `(entityId, worldDay, purpose)` instead of a free-running counter, so "there is **no free-running cursor to persist**". #42 adopts the *property* (KD-7). |
| `SeasonCalendar` (`NextRoundIndex`, `RoundToDay`), `SeasonState` (`Seed`, `ManagedClubId`, …) | #30 §2.2 | What #30 actually exposes — note there is **no season-year field** (KD-4). |
| `RunWorldTickInFixedOrder()` with pre-declared null seams 1–6 then `WorldStore.AdvanceDay()` | `docs/specs/season-competition-loop/section-3.md` §3.3 | Where #42's daily/annual step slots in (KD-4 / back-prop). |
| `SeasonSaveCodec` opaque sub-blob composition; `MEDICAL_SAVE_FORMAT_VERSION` (#41) / `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` (#33) / `STAFF_SAVE_FORMAT_VERSION` (#34) | `src/season-save/`, #41/#33/#34 §4.4 | The persistence pattern #42 repeats verbatim (KD-6). |

**What #42 adds:** an academy store (per-club structure inputs + the youth cohort + the intake latch),
an intake driver, two pure post-generation transforms (KD-2), a promotion command, and one save
sub-blob. **No new generator, no new ability model, no new roster type.**

---

## 2. Staging (minimal identity → deep, one code path)

**Stage-3 minimal (the identity):** on each intake day (KD-4 — a world-day period, not a calendar year
#30 does not model), the managed club receives a
cohort of `ACADEMY_INTAKE_COHORT_SIZE` prospects produced by `RegenGenerator.GenerateRegen` driven from
#42's own stream, with the academy quality dial at its **neutral** value — which applies **exactly zero
transform**, so a minimal cohort is byte-identical to what #28's generator alone would have produced.
Prospects sit on the academy roster; a promotion command moves one to the senior squad.

**Stage-3+ deep:** the quality dial becomes non-neutral (facility + youth-coaching inputs raise the
prospect **ceiling**), the intake age band narrows/bio-bands (KD-2b), youth contracts gain negotiation
depth, and promotion gains criteria beyond an explicit command.

**One code path.** Minimal is the deep path with `AcademyQuality.Neutral` and the age band pinned to
#28's own `[REGEN_AGE_MIN, REGEN_AGE_MAX]`; deepening is *populating dials*, not a rewrite. Every
transform below is written so that its neutral input is the **exact identity** (no shift, no re-anchor,
no re-clamp), which is what makes the minimal tier provably equal to unmodified #28 output.

---

## 3. Dependencies & reference direction (one-way, no cycle)

**Upstream (needs):** **#28** (the generator + CA/PA model + lifecycle type + its constants), **#27**
(`PlayerRecord`/`PlayerAttributes` shape), **#16** (RNG service + domain-tag namespace).
**Value-input only (no reference):** #34 staff quality, #40 facility spend — both arrive inside
`AcademyQuality` (KD-3).

**Downstream (consumers, deferred — no interface built):** #30 (applies `IntakeResult` /
`PromotionResult` to the senior roster; owns the tick slot + save composition), #29 (trains a promoted
prospect), #32 (scouts a prospect), #38 (an academy screen).

```
compositionRoot (season loop, #30) ──► #42 Academy ──► { #28, #27, #16 }
        │                                   ▲
        ├─ invokes the intake tick slot      └── #29 / #32 / #38 consume the prospect + view model (deferred)
        ├─ assembles AcademyQuality from #34/#40 (KD-3)
        └─ applies IntakeResult / PromotionResult to the #27 roster
```

**Acyclic.** #42 references **neither #30 nor #34 nor #40**. No consumer references #42.

---

## 4. Persistent state & save impact (KD-6)

New per-club academy state: the structure inputs last applied, the youth cohort (each prospect =
`PlayerRecord` + `PlayerLifecycle` + academy-scoped fields), the **intake latch** (`LastIntakeWorldDay`
+ its genesis sentinel, KD-4), the `NextYouthPlayerId` allocator high-water, and (deep) youth-contract
state. **No RNG cursor is serialized** — KD-7's per-intake anchor makes the draw a pure function of
`(worldSeed, clubId, intakeWorldDay)`, all of which are already in the blob or the world.

It lands as **one opaque, independently version-gated season-save sub-blob**
(`ACADEMY_SAVE_FORMAT_VERSION` [FIXED] = 1) composed into `SeasonSaveCodec` — the #41 / #33 / #34
precedent, all of which are "no `WORLD_STORE_FORMAT_VERSION` bump". `SeasonSaveCodec` never parses it.
Fail-loud posture mirrored exactly: version gate first, an **overflow-safe** `Require(offset, need,
total)` bound against `total − offset` on every length-prefixed read, trailing-byte guard. The outer
`SEASON_SAVE_FORMAT_VERSION` bump is coordinated with #30 at the T-phase (exact version assigned by
whichever T-phase lands first — never hardcoded here).

**Not a world-store bump.** The academy is *club-scoped season/career state*, and the season save
already composes the world blob; putting it in the season frame keeps `WorldStore` untouched and keeps
#42 out of the living-world assembly entirely (which FR-LW-003 would otherwise complicate).

---

## 5. Determinism

All arithmetic is **integer** (attributes `[1,20]`; CA/PA `[0, ABILITY_MAX]`; quality dials per-mille
`int`). **No float in #42** — the #40/#41/#31/#34 off-pitch posture.

#42 advances on the **world tick** (`WorldClock`, one day = one `worldTick`), never the 10 Hz/60 Hz match
loops. The intake fires **once per intake period** on a world-day comparison against the serialized latch
(KD-4) — no #30 calendar concept is consumed beyond the tick itself.

The one stochastic surface is **cohort generation**, which draws from a #42-owned `youth.intake` stream
under domain tag `0x2B` / `SubsystemOrdinals` 93 (proposed), re-anchored per intake (KD-7). Draw budget
per prospect is exactly #28's `PROGRESSION_REGEN_FIELDS` — #42 adds **no draw of its own** (both
post-generation transforms are pure functions of already-drawn values plus the dials, KD-2). Validation
runs **before** any draw, so a refused intake consumes nothing (the living-world `world.text`
refuse-before-draw precedent).

---

## 6. Primary surfaces (proposed → pinned in §4 of the section files)

| Surface | Role |
|---|---|
| `AcademyQuality` (value type, `Neutral` identity) | the KD-3 structure-quality input the root assembles |
| `AcademyState` | per-club: structure inputs, cohort, latch, id high-water, stream cursor |
| `YouthProspect` | `PlayerRecord` + `PlayerLifecycle` + academy-scoped fields (intake year, contract) |
| `AcademyIntake.RunIntake(...) → IntakeResult` | the KD-4 one-shot driver (period-dialled, not hardcoded "annual"); re-anchors the KD-7 stream, calls #28's generator, applies the KD-2 transforms |
| `AcademyTransforms` | the two pure post-generation transforms (`ApplyCeilingShift`, `ReanchorAge`) |
| `AcademyPromotion.Promote(...) → PromotionResult` | the KD-5 command; emits, never mutates a `Squad` |
| `AcademySaveCodec` | the KD-6 sub-blob encode/decode |
| `AcademyConstants` | the Appendix A catalogue |
| `AcademyViewModel` | read-only observer for #38 (deferred consumer, no interface built) |

---

## 7. Key design decisions

### KD-1 — #42 calls #28's generator unmodified, from its own stream. (Load-bearing.)

The plan's §9 named this the central risk: "if #28's generator isn't parameterized for academy quality
when #42 lands, #42 either forks the generator (violates roadmap §3) or forces a #28 edit." **Neither is
necessary.** `RegenGenerator.GenerateRegen` is `static`, pure, and takes `streamIndex` as an explicit
parameter (verified in source), so #42 registers its **own** stream and passes that index in. #42
therefore reuses the machinery *and* keeps its randomness on its own domain tag — no fork, no #28 edit,
no shared cursor with `player-progression.regen`.

Academy quality is applied **after** generation as a pure transform (KD-2), not threaded into the
generator. Consequences: #28 stays schema-untouched at #42's approval; the per-prospect draw budget is
exactly `PROGRESSION_REGEN_FIELDS`; and the minimal tier is provably equal to unmodified #28 output.

**Rejected:** adding a `quality` parameter to `GenerateRegen`. It would change #28's draw semantics (or
add a draw), force a #28 back-prop and re-test at #42's approval, and give #42 co-ownership of a #28
formula — the "second path into a model you do not own" trap #34 KD-3 and #29/#41 §7 all forbid.

### KD-2 — the quality dial shifts **PotentialAbility**, never CurrentAbility or the attributes.

`PlayerLifecycle.CurrentAbility` is a **derived cache** of `AbilityModel.ComputeCA(attributes,
position)`. Shifting CA without shifting attributes would decohere the pair (and #28's growth step
recomputes CA from the attributes anyway, so the shift would silently vanish on day one). Shifting the
**attributes** would mean re-implementing #28's weighted spend/drain ordering — a second path into
#28's model.

`PotentialAbility` is drawn **independently** of the attributes and is exactly the "how good can this
prospect become" quantity an academy improves. So:

```
ApplyCeilingShift(life, quality):
    if quality.CeilingShiftPerMille == 0: return life          # neutral ⇒ EXACT identity
    shifted := life.PotentialAbility + (life.PotentialAbility * quality.CeilingShiftPerMille) / 1000
    # The floor is #28's OWN generation postcondition, re-applied verbatim — not the weaker "PA >= CA".
    paFloor := max(PA_MIN, min(life.CurrentAbility + REGEN_PA_HEADROOM, ABILITY_MAX))
    life.PotentialAbility := Clamp(shifted, paFloor, ABILITY_MAX)
```

Integer per-mille, truncating division (deterministic). **The clamp floor is `RegenGenerator`'s own
`paFloor` expression, reproduced exactly** — so a shifted prospect satisfies precisely the postcondition
the generator guarantees ("room to grow", §3.3), not merely the weaker `PA ≥ CA` (F1) invariant. A
strongly negative dial therefore produces a *low-ceiling* prospect, never a **zero-headroom** one that
could not grow at all — which would be a different thing from "a weak academy" and would silently
contradict #28's generation contract. Neutral dial (`0‰`) returns the record untouched — byte-identical,
not merely equal.

**KD-2b — age re-anchor is the same shape, and neutral at minimal.** Bio-banding / a distinct academy
intake age band is deep-tier. The transform re-anchors both halves coherently (`record.Age` **and**
`life.BirthWorldDay`, recomputed from the same `worldDay` by #28's own formula) so the pair never
decoheres; at minimal the band **is** `[REGEN_AGE_MIN, REGEN_AGE_MAX]`, so the re-anchor is a no-op.
This is a deliberate scope call: the plan cites "bio-banding per Master Vol 1", but that source model
must be confirmed against the master plan before a band is pinned (§10), and pinning it now would be
inventing a number.

### KD-3 — academy structure arrives as a **value input**, not a #34/#40 reference.

#34 explicitly built no #42 interface (FR-ST-021, FR-LW-031) and #40 exposes budgets, not an academy
facility model. #42 defines `AcademyQuality` — a small integer value type with an explicit `Neutral`
identity — and the **composition root** assembles it from whatever producers exist (today: nothing, so
it is `Neutral`; later: #34's coaching-quality projection + #40-funded facility level). This is exactly
#29's `TrainingInput` / #34's projections-into-consumer-identity-types pattern: the consumer declares
the shape, the root does the wiring, and no phantom interface is built in either direction.

`default(AcademyQuality)` (all-zero) **is** `Neutral` here, deliberately: zero per-mille shift is the
identity, so the zero-value trap that bit `MarkingOrientation`/`LineOfEngagement` cannot occur. This is
recorded as an explicit invariant with a test lock, not left implicit.

### KD-4 — the intake is a one-shot latched on the **world day**, not on a season year.

#30's `RunWorldTickInFixedOrder` gains a pre-declared **academy null seam** (back-prop, §8) positioned
after staff (step 6) and before the live `WorldStore.AdvanceDay()`.

**#30 exposes no season-year field** (verified: `SeasonCalendar` carries `NextRoundIndex` + `RoundToDay`;
`SeasonState` carries `Seed`/`ManagedClubId`/`Fixtures`/`Table`/`Calendar`/`Board`). Keying the latch on a
"season year" would therefore either invent #30 state or force a #30 back-prop for a field #42 alone
wants. Instead the latch is a serialized **`LastIntakeWorldDay` (`uint`)** and the trigger is a pure
comparison on the world clock #42 already ticks on:

```
if currentWorldDay >= LastIntakeWorldDay + ACADEMY_INTAKE_PERIOD_DAYS:   # [GT], default DAYS_PER_YEAR
    RunAnnualIntake(...); LastIntakeWorldDay := currentWorldDay
```

This needs **nothing from #30 but the tick**, keys the KD-7 draw anchor on a value that is already
serialized, and makes "annual" a tunable dial rather than a hidden calendar assumption. The genesis case
(a new career with no prior intake) is an explicit sentinel, not `0` arithmetic — `0` is a legal world
day, so a `HasIntakenBefore` flag or an explicit sentinel constant is required; leaving it implicit is
the class of bug this note exists to prevent.

The latch is **serialized state, not a runtime flag** — the #26 half-time one-shot-flag and the
GK/Heading v18 `_saveCommittedForGk` precedents both exist precisely because a latch omitted from the
snapshot re-fires after a restore. A save taken on the intake day, restored, and advanced must produce
**one** cohort, not two.

### KD-5 — promotion emits a result; #42 never mutates a `Squad`.

The academy roster is #42's; the senior roster is #27's, owned by #30. `Promote` validates (prospect
exists, senior roster has a vacancy against `CLUB_SQUAD_SIZE`, contract state permits) and returns a
`PromotionResult` the composition root applies to the `Squad`, removing the prospect from the academy
roster in the same atomic step. This is the FR-PG-012 discipline #28 already follows for regens, and it
keeps #42 free of any `Squad` write path.

The prospect keeps its `PlayerId` across promotion — **no re-key** (the #34 KD-7 shape). Ids come from a
#42-owned monotonic `NextYouthPlayerId` high-water that is serialized and never reused, and must not
collide with #28's regen allocator: the two allocators are reconciled at the composition root (§10 R-3).

### KD-6 — one opaque season-save sub-blob (see §4). No `WORLD_STORE_FORMAT_VERSION` bump.

### KD-7 — one `youth.intake` stream, **anchored per intake** so no cursor is persisted.

Three facts constrain this, and they pull against each other:

1. `RegisterStream` appends into a **bounded, never-shrinking** table — `MaxRngStreams`, default **64**
   (verified in `DeterministicSimConstants`). There is no unregister.
2. `RegenGenerator` draws through `Reserve` / `DrawReserved` / `CloseReservation`, which **free-run** the
   stream's `RngCursor` / `ActionOrdinal`. KD-1 (reuse the generator unmodified) therefore *cannot* use a
   purely keyed draw the way #41 does.
3. #41 established the idiom that dissolves the persistence question: derive the anchor from
   `(entityId, worldDay, purpose)` so there is "**no free-running cursor to persist**".

**Rejected — a stream per (club, intake)**: makes every intake start at cursor 0 and needs no serialized
cursor, but consumes a bounded slot per intake per club and exhausts a 64-slot table within a few career
seasons.

**Rejected — one stream per club, cursor serialized** (the `match-flow.card-severity` v17 remedy): it
works, but it re-introduces exactly the persistence coupling #41 showed is avoidable, and it makes every
future #42 draw site a schema-versioning event.

**Rejected — relying on re-registration at cursor 0 after a restore**: correct only by accident, and the
precise class of the v17 card-severity defect (a restore re-registered at cursor 0 and the next draw
diverged, silently breaking round-trip determinism for any carded match).

**Chosen — anchor-then-free-run.** One `youth.intake` stream per club, registered lazily at that club's
first intake. Immediately **before** each intake the stream is re-anchored to
`DeriveActionOrdinal(clubId, intakeWorldDay, DRAW_PURPOSE_INTAKE)` (the #41 §3.1.1 derivation, with the
fixed-radix guard #41's own AR-2 added); the generator's reservations then free-run *within* that one
cohort. The property that matters holds: **each intake is a pure function of `(worldSeed, clubId,
intakeWorldDay)`**, independent of how many draws any prior intake consumed — so **no cursor is
serialized**, a restore reproduces the next cohort exactly, and a future second draw site cannot silently
inherit a stale position. At Stage-3 minimal only the **managed club** runs an academy (other clubs'
academies are background-tier, deferred with the global sim exactly as #36 defers to Stage 5), so this is
**one** stream slot.

**Open mechanism question for the section files (not a design fork — both give the same property):**
today the only public way to set a stream's anchor is `RestoreStream(index, in RngStreamState)`, whose
name and contract are the *restore* seam. Re-purposing it as a re-key seam works but reads wrong at the
call site. The alternative is a small #16 addition — an explicit `SeekStream`/`ReKeyStream(index,
actionOrdinal)` seam — filed as a T-phase back-prop. §4 of the section files must pick one and record
it; the KD pins the **invariant** (per-intake position-independence), not the call.

**This surfaces a shared, pre-existing bound, recorded in §10 R-1:** #28's FR-PG-020 also registers a
`player-progression.regen` stream **per club**; a full-world regen population against a 64-slot table is
a #28/#16 concern that #42 must not make worse — and does not, by staying single-club at minimal.

### KD-8 — behaviour-neutral by construction, and the approval-time back-prop is exactly one.

An academy at `AcademyQuality.Neutral` with the minimal age band produces cohorts identical to #28's
unmodified generator, and a career with the academy seam null is byte-identical to pre-#42 (the seam is
a documented position, empty until the T-phase). The single approval-time back-prop is #30's
reserve-ahead academy tick-order slot (ERR-030-007). Everything else — the outer
`SEASON_SAVE_FORMAT_VERSION` bump, the `0x2B`/93 promotion in #16 §3.4, the #34 coaching-quality
consumption — is deferred to the T-phase or to the deep tier.

---

## 8. Cross-spec back-props

| ID | Target | When | Change |
|---|---|---|---|
| **ERR-030-007** | #30 §2 FR-SN-034 + §3.3 `RunWorldTickInFixedOrder` | **At approval** | Append the **academy null seam** as step 7 (after staff, before the world-day tick; `AdvanceDay` → step 8). A position reservation — empty until #42's T-phase. (`ERR-030-005` is soft-reserved by #31's deferred `RequestRosterCommit`; `-006` is #34's, so `-007` is the next free number.) |
| ERR-016-yyy | #16 `DeterministicRngService` | At T-phase, **only if** §4 picks it | An explicit `SeekStream`/`ReKeyStream(index, actionOrdinal)` seam, so KD-7's per-intake anchor does not re-purpose `RestoreStream`. Not needed if §4 chooses the `RestoreStream` call. |
| ERR-016-xxx | #16 §3.4 + `SubsystemOrdinals` | At T-phase (first draw) | Promote `DOMAIN_TAG_YOUTH_ACADEMY = 0x2B` / ordinal 93 from proposed to allocated. `[CROSS-PENDING]` → `[CROSS]` in #42 §3. |
| ERR-030-xxx | #30 `SEASON_SAVE_FORMAT_VERSION` | At T-phase | Bump, composing the academy sub-blob (version coordinated with whichever T-phase lands first). |
| ERR-034-xxx | #34 | **Deep tier only** | #42 consumes #34's published coaching-quality projection through `AcademyQuality`. **No #34 change** — #34 already publishes it and built no #42 interface by design. |
| — | #28 | **Never** | #28 is schema-untouched. KD-1/KD-2 exist to keep it that way. |

---

## 9. Test focus

- **Identity:** `AcademyQuality.Neutral` ⇒ the cohort is **byte-identical** to a direct
  `RegenGenerator.GenerateRegen` call sequence over the same stream/seed (the KD-1/KD-2 identity proof);
  `default(AcademyQuality) == Neutral` (KD-3 zero-value lock).
- **Two-run determinism:** same seed + same club ⇒ field-identical cohort (mirrors `RosterGeneratorTests`).
- **Save round-trip:** `AcademyState` → sub-blob → restore is field-identical; the **next** intake after a
  restore is byte-identical to the uninterrupted run (KD-7 — the position-independence lock, which is the
  property that would have caught the v17 card-cursor defect class *without* persisting a cursor).
- **Position-independence:** an intake preceded by a different number of prior draws on the same stream
  produces the **same** cohort (the KD-7 anchor lock — this is the test that fails if someone later
  "simplifies" the anchor away).
- **One-shot latch:** save on the intake day → restore → advance produces **one** cohort, not two (KD-4);
  and the genesis sentinel is exercised (world day `0` is a legal day, not "never").
- **Ceiling-floor invariant:** a negative dial clamps at `max(PA_MIN, min(CA + REGEN_PA_HEADROOM,
  ABILITY_MAX))` — i.e. every shifted prospect still satisfies #28's own generation postcondition, not
  merely `PA ≥ CA` (KD-2, F1); a positive dial clamps at `ABILITY_MAX`.
- **Coherence:** the age re-anchor moves `Age` and `BirthWorldDay` together (never one alone).
- **Promotion:** moves the record without corrupting either roster; refuses on a full senior squad;
  `PlayerId` is unchanged across promotion and never reused.
- **Fail-loud:** sub-blob version mismatch / out-of-bounds length prefix / trailing bytes; an intake for
  an unknown club; a promotion of an unknown prospect.
- **No draw on refusal:** a refused intake leaves the cursor untouched.

---

## 10. Risks

- **R-1 (shared, pre-existing — surfaced here, not caused here): `MaxRngStreams` = 64 vs per-club
  streams.** #28 FR-PG-020 registers a regen stream per club; #42 would too if academies ran world-wide.
  A full-world career would exhaust the table. #42 stays **single-club at minimal** so it does not make
  this worse, and the bound is flagged for #28/#16 to resolve before either spec goes world-wide (a
  larger table, or club-indexed sub-streams under one registration).
- **R-2: bio-banding source model unconfirmed.** The plan cites Master Vol 1; KD-2b therefore leaves the
  band at #28's values and defers the band to the deep tier rather than inventing numbers.
- **R-3: two `PlayerId` allocators.** #28 allocates fresh ids for regens; #42 for prospects. They must
  not collide. The reconciliation belongs at the composition root (one id authority, both callers
  request from it) and is an explicit §4 interface-contract item for the section files, not something
  #42 can settle unilaterally.
- **R-4: quality-input double-counting.** If #34's coaching projection later reaches growth through #29
  *and* the ceiling through #42, an academy coach would be counted twice. #42's dial modulates **PA at
  intake only** (a one-time ceiling), never the daily growth rate — the two are disjoint by construction,
  and that disjointness must be stated in §1 so a later reader does not "unify" them.

---

## 11. Promotion pipeline

1. This supplement → adversarial review to convergence (AR-1 …; an L-only round ends the cycle).
2. Author the 11 section files at `docs/specs/youth-academy-intake/` (`outline.md`, `section-1..8`,
   `section-9-approval-checklist.md`, `appendices.md`) at `Status: IN REVIEW`.
3. Section-file PASS-1 adversarial review → v0.2 fix pass.
4. File **ERR-030-007** (the only approval-time back-prop) atomically with the status flip.
5. Lead-developer R-01..R-05 sign-off; all files → `APPROVED`; add the `SPEC_INDEX.md` registry row and
   a Registry-Changes entry.
6. T-phase implementation per §7 of the section files (not part of this pipeline).

---

## Version History

| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 24, 2026 | Initial design supplement, promoted from `spec-plans/spec-42-youth-academy-intake.md` v0.1. Resolves the plan's five KDs against verified source: KD-1 (call #28's generator unmodified from a #42-owned stream — the plan's central risk, dissolved by `GenerateRegen`'s `streamIndex` parameter), KD-2 (the quality dial shifts PA, not CA/attributes, because CA is a derived cache), KD-3 (`AcademyQuality` value input, no #34/#40 reference), KD-4 (serialized one-shot latch), KD-5 (promotion emits a result, never mutates a `Squad`), plus KD-6 (season sub-blob), KD-7 (RNG stream strategy) and KD-8 (behaviour-neutral; one approval-time back-prop). |
| v0.2 | July 24, 2026 | **AR-1 fix pass: 0H + 3M + 1L, all resolved.** **M-1 (KD-7 rebuilt):** v0.1 chose "serialize the stream cursor" and rejected keyed draws on a false premise — it conflated "keyed draw" with "a stream per key". #41 §3.1.1 had already established `DeriveActionOrdinal(entityId, worldDay, purpose)`, whose stated consequence is "no free-running cursor to persist". v0.1's rejection reasoning (the `MaxRngStreams`=64 bound) applies only to per-key *registration*, not to per-intake *anchoring*. KD-7 is now **anchor-then-free-run**: one stream per club, re-anchored before each intake, so the cohort is a pure function of `(worldSeed, clubId, intakeWorldDay)` and **no cursor is serialized** — with the genuine remaining tension named (KD-1's generator reservations free-run, so #42 cannot be *purely* keyed like #41) and the `RestoreStream`-vs-new-`SeekStream` call-site question left as an explicit §4 decision + conditional #16 back-prop. §4/§5/§9 updated; the cursor row is gone from the save block. **M-2 (KD-4 keyed on a non-existent concept):** v0.1 latched on a "season year"; #30 exposes none (verified — `SeasonCalendar` has `NextRoundIndex`/`RoundToDay`; `SeasonState` has `Seed`/`ManagedClubId`/`Fixtures`/`Table`/`Calendar`/`Board`), so the latch would have invented #30 state or forced a #30 back-prop for a #42-only field. Now `LastIntakeWorldDay` + an `ACADEMY_INTAKE_PERIOD_DAYS` `[GT]` — needs nothing from #30 but the tick, and supplies KD-7's anchor key. Added the genesis-sentinel note (world day `0` is a legal day, not "never"). **M-3 (KD-2 clamp too weak):** the PA floor was `max(PA_MIN, CA)`, which permits a strongly negative dial to produce a **zero-headroom** prospect — legal under F1 but contradicting the "room to grow" postcondition `RegenGenerator` guarantees at §3.3. The floor is now the generator's own `paFloor` expression reproduced verbatim, so the transform preserves exactly the invariant its producer asserts. **L-1:** recorded the `ERR-030-005` soft-reservation (#31) that makes `-007` the next free number. |
| v0.3 | July 24, 2026 | **AR-2 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle, per the project convention). L-1: §2's staging paragraph still opened "on the intake day of each season year", the exact phrasing AR-1 M-2 removed from KD-4 — re-anchored to the world-day period. L-2: the §6 surface was named `RunAnnualIntake`, hardcoding into the API name the cadence KD-4 had just made an `ACADEMY_INTAKE_PERIOD_DAYS` dial — renamed `RunIntake` and its row now names the KD-7 re-anchor step. Re-verified clean with no change: KD-1's generator-reuse claim against `RegenGenerator`'s actual signature; the KD-2 floor against `RegenGenerator`'s `paFloor`; the tick-order step numbers (staff = 6, `AdvanceDay` = 7 today ⇒ academy = 7, `AdvanceDay` → 8); `CLUB_SQUAD_SIZE` = 25; `MaxRngStreams` = 64; the #34 FR-ST-021 / #28 §7 reciprocal citations; and that no §8 back-prop touches #28. |
