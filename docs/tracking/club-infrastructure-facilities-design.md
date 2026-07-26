# Club Infrastructure & Facilities #53 — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.4 — AR-3 sweep: 0H+0M+2L, **CONVERGENCE**; prior v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.4
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#53** (**new** — gap-fill, proposed here; see §0) · **FR prefix:** `FR-IN`
> **Master-plan home:** §5 Stage 3 *"Infrastructure upgrades (training ground, stadium)"* · **Wave:** 5 (late — see below) · **Tier:** S3
>
> **Wave note:** #53 is a producer whose consumers (#42, #29, #41) have already landed. That inverts the
> roadmap's producer-before-consumer rule, and it is safe **only** because each consumer was built to the
> value-input pattern with a `Neutral` identity (§2(c)) — they function today with no producer at all. It
> is recorded rather than silently accepted, because the same inversion would be unsafe for any consumer
> that lacked a neutral default.
> **Determinism:** **none** — draw-free; no RNG stream, no domain tag, no `SubsystemOrdinal` (KD-6)

---

## 0. Why this candidate exists

**#53 is a new gap-fill candidate, opened on the same basis as #40–#50 (roadmap v0.2) and #51/#52
(Amendment 01 / roadmap v0.4): a master-plan feature that no candidate spec owns.** The trigger is
stronger than "the plan names it", though — that alone would justify a plan file, not a supplement:

> **Four APPROVED specs already consume a facility model, and all of them attribute it to #40, whose own
> approved scope excludes it.** The producer is named, designed-for, and does not exist.

This is the inverse of the phantom-interface hazard FR-LW-031 guards against. There, the danger is a
consumer built ahead of its producer. Here the consumers were built **correctly** — as value inputs with
`Neutral` identities and no assembly reference, so nothing is broken today — but the producer they name
was never anyone's to build. Left alone, the outcome is not a compile error; it is a `Neutral` dial that
stays `Neutral` forever while four specs' Stage-3 tiers each quietly wait for the other.

The roadmap and plan-file rows for #53 land alongside this supplement (§8.1), per the v0.2/v0.4 precedent.

## 1. Scope

**#53 owns:** the per-club **facility model** — a small roster of facility types, each with a **level**;
the **upgrade lifecycle** (start, build duration, completion) on the world tick; and the **projection of
levels into the value-input dials four other specs already declare**.

**#53 does not own:**

| Not owned | Owner | How #53 relates |
|---|---|---|
| **Money** — budgets, balance, the ledger | **#40** | An upgrade's *cost* is a #40 transaction; #53 holds no currency and checks no budget (KD-1) |
| **Who decides** to upgrade | the command layer / #30 | #53 applies a validated command; it contains no AI that spends (KD-1) |
| Staff quality | **#34** | A separate seam into the same consumers — and it must not double-count (KD-4) |
| Intake quality, training effect, injury recovery, revenue | **#42 / #29 / #41 / #40** | #53 supplies an **input**; each consumer owns its own response curve (KD-4) |
| The stadium as a *rendered place* | presentation (#48) | #53 holds capacity as a number, not a venue |

## 2. What already exists (verified)

**(a) Four approved specs consume a facility model and name #40 as its producer.** Quoted at source:

| Spec (APPROVED) | Where | What it says |
|---|---|---|
| #34 Staff | `staff-backroom/section-1.md:90`, `section-3.md:46` | staff modulation must not *"double-count with #33 morale or **#40 facilities** (which reach those consumers by **their own separate seams**)"* |
| #42 Youth Academy | `youth-academy-intake/section-1.md:62` | *"**Value-input only (no assembly reference):** #34 staff quality, **#40 facility spend**"* |
| #42 Youth Academy | `youth-academy-intake/section-4.md:57` | *"when #34 lands its coaching-quality projection and **#40 its facility spend**, the root maps them into the two dials"* |
| #28 Progression | `player-progression-lifecycle/section-1.md:30`, `section-7.md:46` | the youth-academy structure *"(**facilities** → intake quality)"* modulates regen quality |

**(b) #40's approved spec has no facility model, and never intended one.** A `grep` for `facilit` across
`docs/specs/club-finances-economy/` returns **nothing**. Its own outline scopes it to *"**budgets**, a
**wage ledger**, **revenue** (prize money / matchday / sponsorship), and (deep tier) FFP-style balance
constraints"*. #42's supplement states the position bluntly in its KD-3: *"#40 exposes budgets, **not an
academy facility model**."*

**Consequence:** §2(a)'s attribution is a **mis-assignment**, not a pending deliverable. #40 is not late;
it was never the owner. That is what makes this a spec gap rather than a scheduling note, and it is why
§8.1 files corrections against four approved specs rather than one.

**(c) The consumers are already shaped to receive it, which makes #53 cheap.** #42 defines
`AcademyQuality` as *"a small integer value type with an explicit `Neutral` identity"* assembled by the
**composition root** from *"whatever producers exist (today: nothing, so it is `Neutral`)"* — the same
pattern as #29's `TrainingInput` and #34's projections.

**Consequence:** #53 does not design new plumbing or ask any consumer to change. It fills existing dials.
The eventual §4 is mostly *"produce these four values"*, and the risk profile is correspondingly low —
which is the strongest argument for opening it now rather than after four Stage-3 tiers have each
improvised something.

**(d) #40 has a deferred consumer too, and it needs a number only #53 can hold.** #40's
`section-7.md` §7.2 defers *"a Stage-3 daily accrual (**matchday attendance** revenue, sponsorship
instalments)"*. Attendance is bounded by **stadium capacity**, which no spec holds.

**Consequence:** stadium capacity belongs in #53's roster from the start (KD-2), even though its consumer
is deferred — this is not a phantom, because the *value* is meaningful on its own (it is club state, like
a roster) and #40's consumer is already specified as deferred rather than absent.

**(e) The master plan names the feature and the determinism block has no room to spare.** §5 Stage 3
lists *"Infrastructure upgrades (training ground, stadium)"* among V2's major features. Separately, the
roadmap §6 block is **exactly full** — `0x20`–`0x2D` / 82–95 consumed, with `0x2E`–`0x2F` / 96–97 reserved
as slack.

**Consequence:** #53's draw-free posture (KD-6) is not merely convenient; it means a new candidate enters
the plan **without touching the reserved slack**, which stays available for a currently-read-only spec
that later discovers it needs a draw.

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | Every facility at its **baseline level**, no upgrades possible. Every projected dial is exactly the `Neutral` its consumer already defines — so a minimal #53 is **behaviourally indistinguishable from #53 not existing**, which is the current build (§2(c)). |
| **S3 (the feature)** | The upgrade lifecycle (KD-3), costs paid through #40, non-neutral projections into #42/#29/#41, and stadium capacity feeding #40's deferred matchday accrual (§2(d)). |
| **Deep (S3+/S5)** | Maintenance/decay, capacity expansion economics, facility effects on reputation and player attraction (#31/#54), multiple grounds. |

The minimal tier is worth landing precisely because it is a no-op: it puts the **producer** in place so the
four waiting consumers have something to bind to, before any of them needs a non-neutral value.

## 4. Key decisions

### KD-1 — #53 owns **levels**; #40 owns **money**; the command layer joins them

An upgrade is a purchase, and a purchase spans two specs. The sequence is owned by the **command layer**
(the same layer that already routes manager intents), not by either spec:

1. the command names a facility and a target level;
2. **#53 is asked whether the build is startable** — `CanStartUpgrade`, a **pure check** with no mutation
   (valid facility, not already building, level in range);
3. only if it is, **#40** checks affordability and records the transaction through its existing
   `ApplyTransaction`;
4. **#53 latches the build** (`StartUpgrade`, KD-3).

**Neither spec references the other.** This is #40's own established relationship with #31 — *"#31 owns
negotiation and reads #40's budget as a constraint; #40 owns no negotiation logic"* — applied to
construction. #53 holding a price, or #40 holding a level, would each create a second truth for a quantity
the other owns.

**The check-before-debit ordering is load-bearing, because reversing it loses a player's money.** A debit
followed by a refused build is unrecoverable; a refused build followed by no debit is a no-op. This is why
the surface is **split in two** (§7): a single `TryStartUpgrade(...)` that both validates and latches
cannot be sequenced correctly around #40's transaction — the caller would have to either debit first or
roll back, and roll-back-on-failure is the pattern #50 KD-4 identifies as the one that loses data when the
roll-back is what fails. Between step 2 and step 4 nothing else runs (one command, one world tick), so the
check's result is still valid when the latch executes.

### KD-2 — A **fixed, APPEND-only roster of facility types**, each an integer level

The Stage-3 roster is exactly four members — `TrainingGround`, `YouthFacilities`, `MedicalCentre`,
`Stadium` (capacity) — one per **existing** consumer dial (KD-4). Each carries a small integer level in a
pinned range, with a **baseline** that is the `Neutral` identity (§3). A `ScoutingInfrastructure` member is
**not** declared: #32 has no such dial, and an enum member with no consumer is the phantom FR-LW-031
forbids. Being APPEND-only, adding one later costs nothing (§8.2).

**Fixed over data-driven**, for the reason #51 gives for its bus set: a closed roster is
completeness-checkable — every consumer dial maps from a known member — whereas a data-driven roster makes
"a consumer reads a facility type that no longer exists" a runtime state. **APPEND-only** because levels
are persisted (§5) and a reordered enum silently re-points every club's facilities.

**Stadium is capacity, not architecture**: one integer, consumed by #40's deferred attendance model
(§2(d)). Anything visual is #48's.

**Genesis: every club starts at the uniform baseline** — and this is a determinism decision, not a balance
one. Facility state is *persisted* (§5), so it is not regenerated from the seed on load; but its **initial
value at career creation** has to come from somewhere, and the two candidates differ sharply:

- **Uniform baseline (adopted).** Every club starts identical. The §3 identity claim holds exactly, and
  the genesis value depends on no generator, so #53 stays outside `WORLD_GENERATION_VERSION` (#50 KD-2)
  entirely.
- **Seed-varied baseline (deferred).** Deriving a club's starting facilities from the world seed — big
  clubs begin with better grounds — is attractive and is **explicitly a deep-tier option**, because it
  makes genesis a *generation* concern: a later change to that derivation would alter existing careers'
  starting state, which is precisely the class #50's `WORLD_GENERATION_VERSION` exists to version. Adopting
  it therefore means enrolling #53 in the generation version, and that must be a stated decision at
  promotion rather than a quiet default.

### KD-3 — An upgrade is a **dated latch on the world tick**, not a season-boundary event

`{ facility → level }` plus at most one `{ inProgressType, targetLevel, completionWorldDay }` per club.
The day-advance checks completion and applies it; there is no per-day progress accumulator to drift.

**Why a completion *day* rather than a remaining-days counter:** a counter must be decremented exactly
once per day, which makes it order-sensitive within #30's tick order and wrong after any restore that
replays a day boundary. A stored completion day is a **pure comparison against the world clock** — it
cannot double-decrement, it survives save/restore trivially, and a restart mid-build resumes correctly by
construction. This is the same reasoning that makes #42's intake *"a one-shot latched on the world day"*.

**One build at a time per club** at Stage 3 — a deliberate simplification that removes any question of
concurrent-completion ordering. Multiple concurrent builds are recorded as a deep-tier extension.

### KD-4 — #53 projects into the **existing dials**, through the root, exactly once

| Consumer | Existing dial | #53's contribution |
|---|---|---|
| #42 Youth Academy | `AcademyQuality` | `YouthFacilities` level |
| #29 Training | `TrainingInput` | `TrainingGround` level |
| #41 Injuries/Medical | its recovery-modifier input | `MedicalCentre` level |
| #40 Finances (deferred) | matchday attendance (§2(d)) | `Stadium` capacity |

Two rules, both inherited rather than invented:

- **Value inputs only, assembled by the root** (§2(c)) — #53 references no consumer and no consumer
  references #53.
- **The "no second source" rule holds** (#34 `section-3.md`): each facility effect reaches a consumer by
  **one** seam. Where #34 staff quality and #53 facility level both feed one dial, the **consumer** owns
  how they combine — #53 supplies its term and nothing more. #53 must not "helpfully" pre-blend staff
  quality into its projection, which is exactly how double-counting gets built by a well-meaning producer.

### KD-5 — Persistence is #53's own opaque sub-blob, and the cost is acknowledged

Per-club facility state is durable, so it lands as an independently version-gated
`FACILITY_SAVE_FORMAT_VERSION` sub-blob in the season save — the convention every management spec follows
and which `SeasonSaveCodec` composes without parsing.

**This makes #53 the twenty-sixth format version** (#50 §2(a) counts 25 across code and specs) and adds a
row to #50's registry bookkeeping — its own R-2 risk. The alternative — folding facility levels into #40's
block because they are "financial" — is rejected: it would make #40's codec parse state #40 does not own,
recreating in the save layer the ownership confusion §2(a)/(b) documents in the spec layer. A version per
owner is the price of the ownership model, and it is the right price.

### KD-6 — Determinism: **draw-free**, therefore no tag, no ordinal, no slack consumed

An upgrade completes on a stored day; a level is an integer; a projection is a table lookup. **Nothing
here is stochastic**, so #53 registers no RNG stream, no `DOMAIN_TAG_*`, and no `SubsystemOrdinal` — and
per §2(e) it does not touch the reserved `0x2E`–`0x2F` / 96–97 slack.

**This is a design commitment, not an observation.** Random build overruns or variable outcomes are
plausible deep-tier features, and adopting one would consume the block's slack. The recorded position: if
that is ever wanted, it takes `0x2E` / 96 **as a promotion of this spec**, decided explicitly — not
absorbed as an implementation detail. (The #40/#29 precedent: `_RESERVED_` rows stay reserved until a real
draw exists.)

**Identity:** at baseline levels every projection equals its consumer's `Neutral`, so a minimal #53 is a
byte-for-byte no-op on every existing behaviour (§3).

## 5. Persistent state (shape)

```
ClubFacilities : { levels        : map<FacilityType, int>,          # baseline = Neutral identity
                   inProgress    : { type, targetLevel,
                                     completionWorldDay } | none }  # at most one (KD-3)
```

One record per club, in a `FACILITY_SAVE_FORMAT_VERSION`-gated sub-blob (KD-5). No floats — levels and
days are integers, which keeps the block free of the representation issues #45's `JobSecurity` bump had to
resolve in #30's season block.

## 6. Determinism posture

- Draw-free; no stream, tag, or ordinal (KD-6).
- World-tick only (`WorldClock`, one day = one `worldTick`) — never the 10 Hz/60 Hz match loops.
- Completion is a comparison against the clock, so it is restore-safe by construction (KD-3).
- Round-trip determinism: the sub-blob serializes fully; save@N → restore → advance is byte-identical.
- **Outside `WORLD_GENERATION_VERSION`** (#50 KD-2), because state is stored and genesis is uniform — a
  property that holds only while KD-2's uniform baseline does.

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `FacilityType` (fixed, APPEND-only enum) | #53 | KD-2 |
| `ClubFacilities` record + `LevelOf(club, type)` | #53 | read-only query |
| `CanStartUpgrade(club, type, targetLevel) → bool` | command layer → #53 | **pure check, no mutation** — must be separable from the latch so it can run before #40's debit (KD-1) |
| `StartUpgrade(club, type, targetLevel)` | command layer → #53 | the latch, run after the debit; **no money** (KD-1) |
| `AdvanceFacilityDay(worldDay)` | #30 tick order → #53 | completion check (KD-3) |
| `ProjectAcademyQuality` / `ProjectTrainingInput` / `ProjectMedicalInput` / `StadiumCapacity` | #53 → root → consumers | value types the consumers already define (KD-4) |
| `FacilitySaveCodec` | #53 | opaque sub-blob (KD-5) |

## 8. Cross-spec back-props

### 8.1 At approval

| ID | Target | Change |
|---|---|---|
| **ERR-034-001** | #34 (`staff-backroom`, APPROVED) | `section-1.md` / `section-3.md` name *"#40 facilities"* as reaching consumers by their own seam. Re-attribute to **#53**; #40 owns the *funding*, #53 the *level*. The double-count rule is unchanged and still correct — only the producer's identity is wrong (§2(a)/(b)). (`ERR-034-*` unfiled and unproposed — verified.) |
| **ERR-042-001** | #42 (`youth-academy-intake`, APPROVED) | `section-1.md:62` / `section-4.md:57` list *"#40 facility spend"* as an `AcademyQuality` input. Re-attribute to **#53**'s `YouthFacilities` projection. `AcademyQuality`'s shape, its `Neutral` identity, and the root-assembly pattern are all unchanged — this is a pointer fix, not a design change (KD-4). (`ERR-042-*` unfiled — verified.) |
| **ERR-028-002** | #28 (`player-progression-lifecycle`, APPROVED) | `section-1.md:30` / `section-7.md:46` describe the academy structure *"(facilities → intake quality)"* without an owner. Name **#53** as the facility producer feeding #42's dial, keeping #28's own out-of-scope position intact. (`ERR-028-001` is filed; `-002` is next free — verified.) |
| **ERR-040-002** | #40 (`club-finances-economy`, APPROVED) | Record that **#53 owns facility state** and that #40's role is funding via the existing transaction path (KD-1) — closing the gap where four specs point at #40 for a model its own scope excludes. Also name #53's `Stadium` capacity as the input for §7.2's deferred matchday-attendance accrual (§2(d)). No #40 code, constraint, or ledger change. (`ERR-040-001` is filed; `-002` is next free — verified.) |

| **ERR-030-020** | #30 (`season-competition-loop`, APPROVED) | Insert `AdvanceFacilityDay` into the day-advance tick order (KD-3). Filed **at approval, not deferred**, because #30's tick order is a *pinned sequence*: #41's `AdvanceMedicalDay` and #45's board seam (`ERR-030-008`, which renumbered `AdvanceDay` 8 → 9) both landed as filed insertions, and a step whose position is decided later is a step whose ordering was never reviewed. #53's slot must sit **before** the consumers that read facility-derived inputs on the same day. (Proposed `ERR-030-*` ids reach `-019` — #50's; `-020` is next free — verified.) |

**Governance, landing with the same commit:** the `management-layer-spec-roadmap.md` row + §3 sketch +
§7 wave placement for #53, and `spec-plans/spec-53-club-infrastructure-facilities.md` — the v0.2 gap-fill
and v0.4 Amendment-01 precedent for adding a candidate.

### 8.2 Deferred (land at the named tier)

- **#41's medical-recovery dial** binding, when #41's Stage-3 tier lands.
- **#40's matchday accrual** consuming `Stadium` capacity, at #40's T3 (§2(d)).
- **A `ScoutingInfrastructure` member**, if and when #32 declares a dial for it (KD-2) — an APPEND-only
  addition, deliberately not declared in advance.
- **Reputation / player-attraction effects** (#31, #54) — deep tier.

### 8.3 Explicitly **not** back-props

- **#29 Training** — its `TrainingInput` already accepts root-assembled value inputs; #53 fills one
  (KD-4). No requirement changes.
- **#16** — draw-free; no tag, no ordinal, no `_RESERVED_` row to file (KD-6).
- **#30's loop logic** — only the tick-order *slot* is added (`ERR-030-020`, §8.1); #53 changes no
  existing step, no boundary roll, and no season state.

## 9. Test focus

**The identity, which is what makes minimal #53 safe** (§3/KD-6): with every facility at baseline, each
projection equals its consumer's `Neutral` **exactly**, and a career advanced with #53 present is
byte-identical to one without it.

**Purchase ordering** (KD-1): a refused build leaves the balance untouched — constructed by attempting an
invalid upgrade with sufficient funds, which is the case a debit-first implementation gets wrong and the
player cannot recover from. The separability itself is locked too: `CanStartUpgrade` must leave state
byte-identical, since the whole ordering rests on it being pure.

**Completion is restore-safe** (KD-3): a build in progress across a save/restore completes on the same
world day as an uninterrupted run; a restore that replays a day boundary does not advance it twice — the
exact failure a remaining-days counter would exhibit and a stored completion day cannot.

**No double-counting** (KD-4): with both a #34 staff projection and a #53 facility projection feeding one
dial, the consumer's combination is the only place they meet; #53's projected value is independent of
staff state (asserted directly, since "the producer pre-blended it" is the realistic way this breaks).

**Round-trip + append-only** (KD-2/KD-5): the sub-blob round-trips byte-identically; an ordinal-stability
lock on `FacilityType` mirrors the `CueId` / text-intent precedent. **Genesis uniformity** (KD-2): two
careers created from different world seeds start with identical facility levels — the lock that keeps #53
outside `WORLD_GENERATION_VERSION`, and the one that would fail first if a seed-varied baseline were
introduced without the accompanying decision.

## 10. Reference DAG

```
root → {#53, #40, #42, #29, #41, #30}      #53 → { }      #40 → { }
```

**#53 is a leaf.** It references no consumer (they define the dial types; the root maps), and not #40
(KD-1 puts the purchase sequence in the command layer). The pattern is the wave's established one — #48's
cue sink, #50's generator registry, #51's cue mapping — and it is what lets a *new* candidate join a
mature graph without any existing spec gaining a reference.

## 11. Risks and standing options

- **R-1 — the mis-attribution is the whole reason to act now** (§2(a)/(b)). Four approved specs point at a
  producer that does not exist. Each will otherwise reach its Stage-3 tier, find the dial still `Neutral`,
  and either improvise a local facility notion or defer again — and two specs improvising the same model
  is the parallel-surface trap this project has hit repeatedly (`TacticTranslation`, `PlayerAttributes`,
  `POSITION_COUNT`).
- **R-2 — #53 is a twenty-sixth format version** (KD-5), feeding #50's registry-bookkeeping risk. Accepted
  as the cost of the ownership model, and cheap here because the block is small and integer-only.
- **R-3 — scope creep toward "club operations"** — infrastructure attracts stadium expansion economics,
  ticket pricing, and naming rights, all of which are #40's or #45's. The eventual §1 should hold the line
  at *levels + upgrade lifecycle + projections*.
- **R-4 — the draw-free commitment is load-bearing for the determinism block** (KD-6/§2(e)). A deep-tier
  "build overrun" feature would consume the reserved slack; it must be an explicit promotion decision, not
  an implementation choice.
- **R-5 — Stage-3 placement means the minimal tier may sit unused for a while.** That is acceptable
  because it is a no-op (§3), but the spec should not grow speculative depth in the meantime.

## 12. Promotion pipeline

1. **This supplement, AR-converged** — **DONE at v0.4.** AR-1 (0H+2M) → v0.2, AR-2 (0H+2M) → v0.3,
   AR-3 (0H+0M+2L) → v0.4 = **CONVERGENCE** (an L-only round closes the cycle, per the project
   convention).
2. **Roadmap + plan-file rows** for #53 (§8.1 governance note) — the gap-fill precedent.
3. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/club-infrastructure-facilities/`,
   FR prefix `FR-IN`.
4. **Section-file PASS-1 adversarial review** + a fix pass.
5. **`SPEC_INDEX.md` registry row** at promotion.
6. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
7. **Flip to `APPROVED`**, landing the four §8.1 back-props atomically.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement opening **#53** as a gap-fill candidate. The trigger is not merely that the master plan names *"Infrastructure upgrades (training ground, stadium)"* (§5 Stage 3) with no owning spec, but that **four APPROVED specs already consume a facility model and all four attribute it to #40, whose own approved scope excludes it** — `grep facilit` over `docs/specs/club-finances-economy/` returns nothing, and #42's KD-3 says so outright (*"#40 exposes budgets, not an academy facility model"*). The consumers are correctly built — value inputs with `Neutral` identities, assembled by the root — so nothing is broken today; the failure mode is four Stage-3 tiers each finding the dial still `Neutral` and improvising, which is the parallel-surface trap (R-1). #53 therefore fills existing dials rather than designing plumbing (KD-4), and its minimal tier is a **provable no-op** (§3). **KD-1** splits levels (#53) from money (#40) with the purchase sequence in the command layer, and pins the failure ordering — accept-then-debit, because debit-then-refuse loses a player's money irrecoverably. **KD-3** stores a **completion world-day** rather than a remaining-days counter, so completion is a pure clock comparison that cannot double-decrement across a restore. **KD-6** commits to draw-free, so #53 joins the plan **without consuming** the roadmap §6 reserved slack (`0x2E`–`0x2F` / 96–97) — and records that a stochastic deep-tier feature would have to take it as an explicit promotion decision. Four back-props re-attribute the producer (#34, #42, #28, #40), all pointer fixes with no design change. |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 0H + 2M, both resolved.** **M-1** — KD-1 contradicted itself in adjacent paragraphs: the numbered sequence debited through #40 at step 2 and started the build at step 3, while the prose one paragraph later required acceptance *before* the transaction. The list's ordering is exactly the money-losing one the prose warns against, and a reader implements the list. Resequenced to check → debit → latch, and the surface **split in two** (`CanStartUpgrade` pure / `StartUpgrade` latch) — because a single `TryStartUpgrade` that validates *and* mutates cannot be sequenced correctly around #40's transaction without roll-back-on-failure, the pattern #50 KD-4 identifies as the one that loses data when the roll-back is what fails. **M-2** — **genesis was unspecified**, and the answer decides two other claims: §3's identity and whether #53 falls under `WORLD_GENERATION_VERSION`. Facility state is *stored*, but its initial value must come from somewhere, and a seed-varied baseline would make genesis a **generation** concern (#50 KD-2). Adopted the uniform baseline explicitly, recorded the seed-varied option as deep-tier with its generation-version consequence attached, and added the §9 lock that fails first if it is introduced silently. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 0H + 2M, both resolved.** **M-1** — §8.3 deferred the #30 tick-order slot to "filed at promotion", but #30's day-advance order is a **pinned sequence** and its two precedents both filed the insertion at approval (#41's `AdvanceMedicalDay`; #45's `ERR-030-008`, which renumbered `AdvanceDay` 8 → 9). A step whose position is decided later is a step whose ordering was never reviewed — and #53's slot has a real constraint (before same-day consumers of facility-derived inputs). Filed as **ERR-030-020** (verified next free). **M-2** — KD-2's roster included a `ScoutingInfrastructure` member *"if #32 wants one"*, i.e. an enum member with no consumer dial: the phantom FR-LW-031 forbids, in a roster that is APPEND-only and therefore permanent. Roster cut to exactly the four members with existing consumer dials, with the addition recorded as a zero-cost later append. |
| v0.4 | July 26, 2026 | **AR-3 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle). **L-1** — the header gave `Wave: 5` without noting that #53 is a **producer landing after its consumers**, inverting the roadmap's producer-before-consumer rule; safe here only because #42/#29/#41 were built to the neutral value-input pattern, which is worth recording rather than silently relying on (the same inversion would be unsafe for a consumer without a neutral default). **L-2** — §6's determinism list did not carry AR-1's `WORLD_GENERATION_VERSION` conclusion, leaving the posture stated in KD-2 only; a reader checking determinism reads §6. |
