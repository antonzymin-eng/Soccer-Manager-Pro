# Youth Academy & Intake #42 — Section 1: Introduction

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** IN REVIEW

---

## 1.1 Scope

The **club academy pipeline**: a periodic **intake** that generates a cohort of youth prospects, the
**academy roster** those prospects occupy until they leave it, and the **promotion** command that hands
one to the senior squad. All #42 state advances on the **world tick** (`WorldClock`, one day = one
`worldTick` — never the 10 Hz / 60 Hz match loops, living-world KD-4), and is persisted alongside #30's
season/career save as an opaque sub-blob.

**Minimal identity (Stage-3 floor).** A cohort is produced by calling **#28's `RegenGenerator`
unmodified** with the academy quality dial at `AcademyQuality.Neutral`, which applies **exactly zero
transform** — so a minimal cohort is byte-identical to what #28's generator alone would have produced
from the same stream position. The only new state is #42's own sub-blob, and with the #30 academy tick
seam null, a career is byte-identical to pre-#42.

**Stage-3 deep.** Non-neutral quality (facility + youth-coaching inputs raising a prospect's
**ceiling**), a narrowed / bio-banded intake age band, youth-contract depth, and promotion criteria
beyond an explicit command — all on **one code path**, each defaulting to its identity.

**#42 supplies the academy structure around #28's machinery; it never owns a model it feeds.** The
generator is #28's, the CA/PA model is #28's, the roster shape is #27's, the calendar and save root are
#30's. #42 adds an intake driver, two pure post-generation transforms, a promotion command, and one save
block — and **no second path** into any of them.

## 1.2 Out of scope (owned elsewhere, referenced as seams)

- **The generation machinery (#28 Player Progression & Lifecycle).** #28 owns `RegenGenerator`, the
  CA/PA model (`AbilityModel`), and the `PlayerLifecycle` overlay. #42 **calls** the generator and
  **MUST NOT** fork, wrap, or edit it (KD-1). #28 §7 already records the reciprocal: "#28 provides the
  machinery, #42 the quality dial."
- **Player development after intake (#28 growth / #29 training).** A prospect grows through #28's
  `GrowthProjection` fed by #29's `TrainingInput`. #42 **MUST NOT** add a second growth path; its dial is
  a **one-time ceiling at intake**, disjoint from the daily growth rate (see F7 / §1.4 KD-2).
- **The canonical roster (#27 Squad / Player Data Layer).** `PlayerRecord` / `PlayerAttributes` / `Squad`
  are #27's. #42 produces records in that shape and **never mutates a `Squad`** — it emits a result the
  roster owner applies (the FR-PG-012 discipline #28 already follows for regens).
- **The season loop, tick order, and save root (#30).** #30 owns `RunWorldTickInFixedOrder`, the
  calendar, and `SeasonSaveCodec`; it **invokes** #42 at a new pre-declared tick-order slot and composes
  #42's opaque sub-blob. #42 never references #30.
- **Staff quality (#34) and club money (#40).** #34 publishes staff-quality projections (and explicitly
  built **no** #42 interface — FR-ST-021 / FR-LW-031); #40 owns budgets. #42 reads neither directly — both
  reach it inside the `AcademyQuality` value input the composition root assembles (KD-3).
- **Scouting / fog-of-war over prospects (#32).** #32 does not exist yet; #42 exposes the prospect record,
  not a knowledge model, and builds nothing for #32.
- **The academy screen (#38).** #42 publishes a read-only view model; #38 renders it.

## 1.3 Dependencies

**Upstream (needs):** **#28** (`RegenGenerator`, `AbilityModel`, `PlayerLifecycle`, and its constants —
`PROGRESSION_REGEN_FIELDS`, `PA_MIN`, `ABILITY_MAX`, `REGEN_PA_HEADROOM`, `REGEN_AGE_MIN/MAX`,
`DAYS_PER_YEAR`), **#27** (`PlayerRecord` / `PlayerAttributes` / `Squad` shape + `CLUB_SQUAD_SIZE`),
**#16** (the RNG service, the domain-tag namespace, and #41's `DeriveActionOrdinal` keyed-anchor idiom).

**Value-input only (no assembly reference):** #34 staff quality, #40 facility spend — both arrive inside
`AcademyQuality`.

**Downstream (consumers, deferred — no interface built, FR-LW-031):** #30 (applies `IntakeResult` /
`PromotionResult`; owns the tick slot + save composition), #29 (trains a promoted prospect), #32
(scouting), #38 (an academy screen).

Reference DAG: `compositionRoot → {#30, #42}`, `#42 → {#28, #27, #16}`. **Acyclic.** #42 does **not**
reference #30, #34, or #40.

## 1.4 Key decisions

- **KD-1 (call #28's generator unmodified, from a #42-owned stream).** `RegenGenerator.GenerateRegen` is
  `static`, pure, and takes **`streamIndex` as an explicit parameter**, so #42 registers its own
  `youth.intake` stream and passes that index in. #42 therefore reuses the machinery *and* keeps its
  randomness on its own domain tag — no fork, no #28 edit, no shared cursor with
  `player-progression.regen`. **Rejected:** adding a `quality` parameter to `GenerateRegen` — it would
  change #28's draw semantics, force a #28 back-prop and re-test at #42's approval, and give #42
  co-ownership of a #28 formula (the "second path into a model you do not own" trap #34 KD-3 and #29/#41
  §7 forbid).
- **KD-2 (the quality dial shifts `PotentialAbility` only).** `CurrentAbility` is a **derived cache** of
  `AbilityModel.ComputeCA(attributes, position)` — shifting it would decohere the pair, and #28's growth
  step recomputes CA from the attributes anyway, so the shift would silently vanish. Shifting the
  **attributes** would mean re-implementing #28's weighted spend/drain ordering. `PotentialAbility` is
  drawn independently of the attributes and *is* the "how good can this prospect become" quantity an
  academy improves. The clamp floor reproduces `RegenGenerator`'s own `paFloor` expression verbatim
  (§3.3), so a shifted prospect satisfies exactly the generator's "room to grow" postcondition — a weak
  academy yields a **low-ceiling** prospect, never a **zero-headroom** one.
  **KD-2b:** the age re-anchor (bio-banding) is deep-tier, moves `Age` and `BirthWorldDay` **together**,
  and is pinned to `[REGEN_AGE_MIN, REGEN_AGE_MAX]` at minimal so it is a no-op.
- **KD-3 (`AcademyQuality` is a value input, not a #34/#40 reference).** #42 declares the shape; the
  composition root assembles it from whatever producers exist (today: none ⇒ `Neutral`). This is the
  #29 `TrainingInput` / #34 projections-into-consumer-identity-types pattern — no phantom interface in
  either direction. **`default(AcademyQuality)` is `Neutral`** by construction (zero per-mille = the
  identity), so the zero-value trap that bit `MarkingOrientation` / `LineOfEngagement` cannot occur here;
  this is an explicit invariant with a test lock (§5), not an accident.
- **KD-4 (one-shot latched on the world day, not a season year).** **#30 exposes no season-year field**
  (`SeasonCalendar` carries `NextRoundIndex` / `RoundToDay`; `SeasonState` carries `Seed` /
  `ManagedClubId` / `Fixtures` / `Table` / `Calendar` / `Board`), so latching on one would invent #30
  state or force a #30 back-prop for a #42-only field. The latch is a serialized `LastIntakeWorldDay`
  with an `ACADEMY_INTAKE_PERIOD_DAYS` `[GT]` dial (default `DAYS_PER_YEAR`). Genesis uses an **explicit
  sentinel** — world day `0` is a legal day, not "never". The latch is **serialized**, because a latch
  omitted from a snapshot re-fires after restore (the #26 half-time one-shot and GK/Heading v18
  `_saveCommittedForGk` precedents).
- **KD-5 (promotion emits a result; #42 never writes a `Squad`).** `Promote` validates and returns a
  `PromotionResult` the composition root applies, removing the prospect from the academy roster in the
  same atomic step. `PlayerId` is **stable across promotion** — no re-key (the #34 KD-7 shape), so no
  cross-system migration hook is needed.
- **KD-6 (one opaque season-save sub-blob).** `ACADEMY_SAVE_FORMAT_VERSION` [FIXED] = 1, composed into
  `SeasonSaveCodec` — the #41 / #33 / #34 precedent, all "no `WORLD_STORE_FORMAT_VERSION` bump". The
  academy is club-scoped career state; keeping it in the season frame leaves `WorldStore` untouched and
  keeps #42 out of the living-world assembly (which FR-LW-003 would otherwise complicate).
- **KD-7 (anchor-then-free-run; no serialized RNG cursor).** One `youth.intake` stream per club, and the
  stream is **re-anchored before each intake** from `(clubId, intakeWorldDay, DRAW_PURPOSE_INTAKE)` (the
  #41 §3.1.1 derivation). The generator's reservations then free-run *within* that one cohort. The
  property that matters: **each intake is a pure function of `(worldSeed, clubId, intakeWorldDay)`**,
  independent of prior draw counts — so nothing needs persisting and a future second draw site cannot
  silently inherit a stale position (the `match-flow.card-severity` v17 defect class). **Rejected:** a
  stream per (club, intake) — `RegisterStream` appends into a bounded, never-shrinking table
  (`MaxRngStreams`, default 64, no unregister) and a career would exhaust it; a serialized cursor —
  works, but re-introduces the persistence coupling #41 showed is avoidable and makes every future draw
  site a schema-versioning event; re-registering at cursor 0 after restore — correct only by accident.
- **KD-8 (behaviour-neutral; exactly one approval-time back-prop).** A neutral academy reproduces #28's
  generator exactly; a career with the seam null is byte-identical to pre-#42. The one approval-time
  back-prop is #30's reserve-ahead academy tick-order slot (ERR-030-007). The `0x2B`/93 promotion, the
  outer `SEASON_SAVE_FORMAT_VERSION` bump, and any #34 consumption are deferred.

## 1.5 Determinism & coordinate posture

All arithmetic is **integer** (attributes `[1,20]`; CA/PA `[0, ABILITY_MAX]`; quality dials per-mille
`int`; world days `uint`). There is **no float in #42** — the #40 / #41 / #31 / #34 off-pitch posture.
All state advances on the world clock at #30's pre-declared slot. The single stochastic surface is cohort
generation, whose per-prospect draw budget is exactly #28's `PROGRESSION_REGEN_FIELDS`: **#42 adds no
draw of its own**, because both post-generation transforms are pure functions of already-drawn values
plus the dials. Validation runs **before** any draw, so a refused intake consumes nothing (the
living-world `world.text` refuse-before-draw precedent).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §1 (scope, out-of-scope seams, dependencies, KD-1..KD-8, determinism posture), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
