# Club Infrastructure & Facilities #53 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## 1.1 Purpose

#53 models a club's **physical infrastructure** as persistent, upgradeable state: a training ground, youth
facilities, a medical centre, and a stadium, each carrying an integer **level** that a manager can raise
over time, and each projecting into a dial some other spec already reads.

It is opened for a reason stronger than "the master plan names it". **Four approved specs already consume
a facility model, and all four attribute it to #40 — whose own approved scope excludes it.** The
consumers were built correctly: each takes a value input with an explicit neutral identity assembled by
the composition root, so nothing is broken today. But the producer they name was never anyone's to build,
and the failure mode is not a compile error — it is four Stage-3 tiers each finding the dial still
neutral and improvising a local facility notion. Two specs improvising the same model is the
parallel-surface trap this project has hit three times (`TacticTranslation`, `PlayerAttributes`,
`POSITION_COUNT`).

This is the **inverse** of the phantom-interface hazard FR-LW-031 guards against. There, a consumer is
built ahead of its producer. Here the producer is named by its consumers and does not exist.

## 1.2 Scope

**In scope**

- A per-club **facility roster**: a fixed, APPEND-only set of facility types, each with an integer level.
- The **upgrade lifecycle** — startability, the latch, and completion — advanced on the world tick.
- The **projection** of levels into the value-input dials #42, #29, #41 and #40 already declare.
- **Stadium capacity** as an integer, held from the start because #40's deferred matchday accrual needs
  a number no spec currently holds.

**Out of scope** — each already has an owner; duplicating it is the failure this section prevents:

| Not owned | Owner | #53's relation |
|---|---|---|
| **Money** — budgets, balance, the wage ledger, the transaction | **#40** | An upgrade's *cost* is a #40 transaction. #53 holds no currency and checks no budget (KD-1). |
| **Who decides** to upgrade | the command layer / #30 | #53 applies a validated command. It contains no AI that spends (KD-1). |
| **Staff quality** | **#34** | A separate seam into the same dials. #53 must not pre-blend it (KD-4). |
| Intake quality, the training effect, injury recovery, matchday revenue | **#42 / #29 / #41 / #40** | #53 supplies a **term**; each consumer owns its own response curve. |
| The stadium as a **rendered place** | **#48** presentation | #53 holds capacity as a number, not a venue. |
| Cross-version save migration | **#50** | #53 declares its own version and fails loud; #50 owns upgrade paths. |
| Reputation, player attraction | **#54 / #31** | Deep-tier consumers of facility state, not #53 logic. |

## 1.3 Dependencies

**Upstream (consumed):**

- **#16 Deterministic Simulation** — `CanonicalSerializer` for the sub-blob. **Nothing else** — no RNG
  service, because #53 is draw-free (KD-6).
- **#27 Squad / Player Data** — `ClubId` as the keying identity. Read-only; #27's schema is untouched.
- **#30 Season & Competition Loop** — invokes the daily advance. **Reference direction is `#30 → #53`;
  #53 never references #30.**

**Downstream (consumers, all via root-assembled value inputs):**

- **#42 Youth Academy** — the `YouthFacilities` term reaches `AcademyQuality`.
- **#29 Training** — the `TrainingGround` term reaches `ComputeTrainingInput` (KD-9: **#29**, not #28).
- **#41 Injuries & Medical** — the `MedicalCentre` term reaches `MedicalModifier`.
- **#40 Club Finances** *(deferred)* — `Stadium` capacity bounds the §7.2 matchday-attendance accrual.
- **#38 UI** — reads a value-copy view model.

**Reference DAG**

```
root → {#30, #40, #53, #42, #29, #41}        #30 → #53        #53 → {#27, #16}
```

**#53 is a leaf.** It references no consumer — the consumers define the dial types and the root maps —
and it does not reference #40, because KD-1 puts the purchase sequence in the command layer. Acyclic at
every tier, and asserted by reference-absence (§5.8), not by convention.

## 1.4 Determinism posture

- **World tick only** (`WorldClock`, one day = one `worldTick`). No #53 type is reachable from
  `MatchEngine.RunTick`; nothing runs on the 10 Hz tactical or 60 Hz physics loops.
- **Draw-free.** No RNG stream, no `DOMAIN_TAG_*`, no `SubsystemOrdinal` — and therefore **none of the
  roadmap §6 reserved slack** (`0x2E`–`0x2F` / 96–97) consumed (KD-6).
- **Integer-only.** Levels, days and capacities are integers; the dials are integers or integer
  per-mille. No float appears anywhere in #53 at any tier.
- **Idempotent by construction**, so no idempotency cursor and no day-gap guard exist (KD-7).
- **Outside `WORLD_GENERATION_VERSION`** (#50 KD-2), because state is stored, not regenerated, and
  genesis is a uniform baseline — a property that holds **only while KD-2's uniform baseline does**.
- Round-trip determinism: the sub-blob serializes fully; `save@N → restore → advance to N+K` is
  byte-identical to the uninterrupted run.

## 1.5 Key decisions

### KD-1 — #53 owns levels; #40 owns money; the command layer joins them

An upgrade is a purchase, and a purchase spans two specs. The sequence is owned by the **command layer**
— the same layer that already routes manager intents — not by either spec:

1. the command names a facility and a target level;
2. **#53 is asked whether the build is startable** — `CanStartUpgrade`, a **pure check with no
   mutation** (known facility, not already building, target above current and within range);
3. only if it is, **#40** checks affordability and records the debit through its existing
   `ApplyTransaction`;
4. **#53 latches the build** — `StartUpgrade`.

**Neither spec references the other.** This is #40's established relationship with #31 — *#31 owns
negotiation and reads #40's budget as a constraint; #40 owns no negotiation logic* — applied to
construction. #53 holding a price, or #40 holding a level, would each create a second truth for a
quantity the other owns.

**The check-before-debit ordering is load-bearing, because reversing it loses a player's money.** A debit
followed by a refused build is unrecoverable; a refused build followed by no debit is a no-op. This is
why the surface is **split in two**: a single `TryStartUpgrade` that both validates and latches cannot be
sequenced correctly around #40's transaction — the caller would have to debit first and roll back on
failure, which is the pattern #50 KD-4 identifies as the one that loses data precisely when the roll-back
is what fails.

**Why the two-step is safe.** Between step 2 and step 4 nothing else runs: one command, inside one world
tick, with no interleaved #53 mutation, so the check's result is still valid when the latch executes.
This is a stated premise, not an assumption — §2 FR-IN-013 requires the latch to **re-validate**, so that
if the premise is ever broken the failure is loud rather than a build started from a stale check.

### KD-2 — A fixed, APPEND-only roster; genesis is a uniform baseline

The Stage-3 roster is exactly four members — `TrainingGround`, `YouthFacilities`, `MedicalCentre`,
`Stadium` — **one per existing consumer dial** (KD-4). A `ScoutingInfrastructure` member is deliberately
**not** declared: #32 has no such dial, and an enum member with no consumer is the phantom FR-LW-031
forbids. Being APPEND-only, adding one later costs nothing.

**Fixed over data-driven**, for the reason #51 gives for its bus set: a closed roster is
completeness-checkable — every consumer dial maps from a known member — whereas a data-driven roster
makes *"a consumer reads a facility type that no longer exists"* a runtime state. **APPEND-only** because
levels are persisted and a reordered enum silently re-points every club's facilities to the wrong
building.

**Genesis: every club starts at the uniform baseline — and this is a determinism decision, not a balance
one.** Facility state is *persisted*, so it is not regenerated from the seed on load; but its initial
value at career creation has to come from somewhere, and the two candidates differ sharply:

- **Uniform baseline (adopted).** Every club starts identical. §1.7's identity claim holds exactly, and
  the genesis value depends on no generator, so #53 stays **outside** `WORLD_GENERATION_VERSION` (#50
  KD-2) entirely.
- **Seed-varied baseline (deferred, deep tier).** Deriving a club's starting facilities from the world
  seed — big clubs begin with better grounds — is attractive, and it makes genesis a **generation**
  concern: a later change to that derivation would alter existing careers' starting state, which is
  exactly the class `WORLD_GENERATION_VERSION` exists to version. Adopting it therefore means enrolling
  #53 in the generation version, and that must be a stated decision at its own promotion rather than a
  quiet default. §5 carries the lock that fails first if it is introduced silently.

### KD-3 — An upgrade is a dated latch on the world tick, not a season-boundary event

Per club: `{ facility → level }` plus **at most one** `{ InProgressFacility, TargetLevel,
CompletionWorldDay }`. The day advance compares the clock against the stored completion day and applies
the level; there is no per-day progress accumulator to drift.

**Why a completion *day* rather than a remaining-days counter.** A counter must be decremented exactly
once per day, which makes it order-sensitive within #30's pinned tick order and wrong after any restore
that replays a day boundary. A stored completion day is a **pure comparison against the world clock** —
it cannot double-decrement, it survives save/restore trivially, and a restart mid-build resumes correctly
by construction. This is the reasoning that makes #42's intake *a one-shot latched on the world day*.

**One build at a time per club** at Stage 3 — a deliberate simplification that removes every question of
concurrent-completion ordering. Multiple concurrent builds are recorded as a deep-tier extension (§7.2).

### KD-4 — #53 projects into the existing dials; the **root** combines

| Consumer | Existing dial | #53's term | Combined at |
|---|---|---|---|
| #42 Youth Academy | `AcademyQuality` | `YouthFacilities` level | composition root |
| #29 Training | `ComputeTrainingInput`'s inputs | `TrainingGround` level | composition root |
| #41 Injuries & Medical | `MedicalModifier` | `MedicalCentre` level | composition root |
| #40 Finances *(deferred)* | matchday attendance | `Stadium` capacity | #40, at its T3 |

Two rules, both inherited rather than invented:

- **Value inputs only, assembled by the root.** #53 references no consumer and no consumer references
  #53. This is #42's own stated pattern for `AcademyQuality` and #29's for `TrainingInput`.
- **No second source.** Each facility effect reaches a consumer by **one** seam. Where #34's staff
  quality and #53's facility level both feed one dial, the **composition root** owns how they combine —
  #53 supplies its term and nothing more.

**The combination point is the root, not the consumer.** #41 takes a single already-assembled
`MedicalModifier`; #42 takes a single already-assembled `AcademyQuality`. Neither consumer sees two
sources, so neither *can* own the combination. Stating this precisely matters: #53 must **not** "helpfully"
pre-blend staff quality into its projection, which is exactly how double-counting gets built by a
well-meaning producer, and §5.7 asserts the independence directly.

### KD-5 — Persistence is #53's own opaque sub-blob, and the cost is acknowledged

Per-club facility state is durable, so it lands as an independently version-gated
`FACILITY_SAVE_FORMAT_VERSION` sub-blob in the season save — the convention every management spec follows
and which `SeasonSaveCodec` composes without parsing.

**This makes #53 a twenty-sixth format version** and adds a row to #50's registry bookkeeping — #50's own
R-2 risk, inherited knowingly. The alternative — folding facility levels into #40's block because they
are "financial" — is **rejected**: it would make #40's codec parse state #40 does not own, recreating in
the save layer exactly the ownership confusion §1.1 documents in the spec layer. A version per owner is
the price of the ownership model, and it is the right price.

### KD-6 — Determinism: draw-free, therefore no tag, no ordinal, no slack consumed

An upgrade completes on a stored day; a level is an integer; a projection is a table lookup. **Nothing
here is stochastic**, so #53 registers no RNG stream, promotes no `DOMAIN_TAG_*`, and takes no
`SubsystemOrdinal`.

**This is a design commitment, not an observation.** Random build overruns and variable outcomes are
plausible deep-tier features, and adopting one would consume the roadmap §6 reserved slack — which is
exactly full at `0x20`–`0x2D` / 82–95 with only `0x2E`–`0x2F` / 96–97 held back for a currently-read-only
spec that later discovers it needs a draw. The recorded position: if a stochastic facility feature is
ever wanted, it takes `0x2E` / 96 **as an explicit promotion of this spec**, decided on the record — not
absorbed as an implementation detail. (The `_RESERVED_0x29_` #40 / `_RESERVED_0x21_` #29 precedent:
reserved rows stay reserved until a real draw exists.)

### KD-7 — No idempotency cursor: the advance is idempotent by construction

Every other world-tick management spec (#33, #41, #42, #45) carries a `LastAdvancedWorldDay` cursor with
an `uint.MaxValue` sentinel, a same-day no-op, and a fail-loud day-gap guard. **#53 carries none, and
that is a decision rather than an omission.**

Completion is `worldDay >= CompletionWorldDay`, and applying it **clears the in-progress record**. So:

- **Re-advancing the same day is already a no-op** — the second call finds no build in progress, or finds
  one whose completion day has not arrived. No cursor is needed to make it idempotent.
- **A day *gap* is already correct** — a build whose completion day fell inside the skipped range
  completes on the first day observed after it. There is nothing to fail loud about, because nothing was
  missed.

Those two properties are precisely what a cursor exists to provide, so adding one would be ceremony that
buys nothing while adding a field to the save block and a failure mode to the surface. It is stated as
KD-7 because the alternative outcome is predictable: a later reviewer notices the missing cursor, reads
it as an inconsistency with four sibling specs, and "fixes" it — introducing a gap-guard that would then
**fail loud on a legitimate multi-day advance**, which is a regression dressed as a consistency
improvement. §5.3 locks both properties directly.

### KD-8 — Two identity conventions, and #53 returns each consumer's own

The consumer dials do **not** share an identity form, and #53 spans both:

| Dial | Identity | `default()` |
|---|---|---|
| `AcademyQuality` (#42) | **all-zero** — `Neutral => default` | legal, *is* the identity |
| `TrainingInput` (#28, produced by #29) | **all-zero** — `Neutral => default` | legal, *is* the identity |
| `MedicalModifier` (#41) | **1000 per-mille** — `Identity => new(1000, 1000)` | **fails loud** (all-zero is ×0) |

#53's projections therefore return **additive** terms for the first two and a **multiplicative
per-mille** term for the third (§3.4). Collapsing them into one convention for tidiness would either
give #41 a ×0 modifier or give #42 a permanent +1000 ceiling shift. Named as a key decision because it is
the kind of asymmetry a refactor "simplifies" without noticing.

### KD-9 — The training-ground term feeds #29, not #28's `TrainingInput`

#29 FR-TR-005 makes **#29 the sole writer** of #28's `TrainingInput`: *"#29 MUST write attributes only by
populating #28's `TrainingInput`; it MUST NOT add a second path."* A #53 projection that returned a
`TrainingInput` directly would be a second writer of that type — the very thing FR-TR-005 forbids, and
functionally the same double-source defect KD-4 rules out.

So the training-ground term is a **root-assembled input to `ComputeTrainingInput`**, sitting alongside
#34's `CoachingModifier` exactly as that modifier already does. #29 folds it into the single
`TrainingInput` it emits. This costs #29 one additional input parameter at its Stage-3 tier and is filed
as **ERR-029-003** (`-001` is filed and RESOLVED; `-002` is soft-reserved by #34 — both verified, so
`-003` is next free); it changes no #29 logic and no #28 type.

## 1.6 Lessons folded in up front

Three known traps are handled by construction rather than left to be found in review:

1. **The zero-value trap** (#40 `BoardModifier`, #41 `MedicalModifier`, #45 `BoardConfidence`).
   `default(ClubFacilities)` has every level at `0` — below `FACILITY_LEVEL_MIN` — **and** an
   `InProgressFacility` of `0`, which is the *first enum member*, i.e. a silent "the training ground is
   being built". The level half is caught by a range check; the in-progress half is **not**, so the
   sentinel is `FACILITY_NONE_SENTINEL = -1` and the enforced guard is at **record insertion**
   (FR-IN-006a / F4a), following #45's F4a and #33's FR-HS-005.
2. **The APPEND-only ordinal contract** (`CueId`, text intents, `PassType`). `FacilityType` ordinals are
   persisted, so reordering re-points every saved club's facilities. Locked by an ordinal-stability test.
3. **The sentinel-collision trap.** `CompletionWorldDay` is a `uint` and could in principle be computed
   as `uint.MaxValue`; that value is therefore **not** used as any sentinel in #53 — the "no build"
   state is carried by `InProgressFacility == -1` alone, so no legal computed day can ever be mistaken
   for "idle" (§3.2).

## 1.7 The identity claim, stated precisely

At baseline levels, **every** projection equals its consumer's own identity **exactly**:
`AcademyQuality.Neutral`, the zero training term, and `MedicalModifier.Identity`. A career advanced with
#53 present and every facility at baseline is therefore **behaviourally indistinguishable** from the same
career with #53 absent — which is the current build.

Two scope limits on that claim, stated rather than implied:

- It is a claim about **behaviour**, not about the **save frame**. From T2 the season save gains #53's
  sub-blob, so the file is not byte-identical. §5.1 scopes its test accordingly.
- It holds **at baseline levels**. A club that has upgraded has deliberately left the identity tier; that
  is the feature, not a violation.

`Stadium` capacity is the one facility whose projection is not an identity-at-baseline dial, because its
value is meaningful absolutely rather than as a deviation. At the minimal tier nothing reads it, so the
identity claim holds vacuously for it; from #40's T3 it holds by #40 calibrating its attendance model
against the baseline capacity, which is #40's to do (§8.2).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §1 (scope, out-of-scope table, dependencies + leaf DAG, determinism posture, KD-1..KD-6 from supplement v0.4, §1.6 folded-in lessons, §1.7 the scoped identity claim). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes: **KD-7** added (no idempotency cursor — with the reason it must be a *decision*, since the predictable "consistency fix" would fail loud on a legitimate multi-day advance); **KD-8** added (the two identity conventions — `MedicalModifier` is 1000-identity with a fail-loud `default()` while `AcademyQuality`/`TrainingInput` are zero-identity, so a single convention is impossible); **KD-9** added (FR-TR-005 makes #29 the sole writer of `TrainingInput`, so the training term feeds #29, not #28 — the supplement's §4 table row was imprecise); KD-4's combination point corrected from *"the consumer"* to *"the composition root"*, since #41 and #42 each take a single already-assembled dial and therefore cannot own a combination; KD-1 gained the explicit re-validation premise; §1.6 gained the sentinel-collision item. |
#endregion
