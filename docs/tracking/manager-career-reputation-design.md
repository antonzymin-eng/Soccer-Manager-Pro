# Manager Career, Reputation & Job Market #54 — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.4 — AR-3 sweep: 0H+0M+2L, **CONVERGENCE**; prior v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.4
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#54** (**new** — gap-fill, proposed here; see §0) · **FR prefix:** `FR-MC` (grep-verified unclaimed)
> **Master-plan home:** §5 Stage 5 *"Manager career mode (job offers, reputation)"* · **Wave:** 6 · **Tier:** S2 min → S5 deep
> **Determinism:** minimal tier is **draw-free**; `_RESERVED_0x2E_` / ordinal 96 **reserved, not promoted** (KD-6)

---

## 0. Why this candidate exists

**#54 is a new gap-fill candidate**, opened on the same basis as #40–#50, #51/#52 and #53. As with #53,
the trigger is not only that the master plan names the feature with no owner. It is this:

> **The project has a fully-specified path to *"you are about to be sacked"* and no specified behaviour for
> *being sacked*.** #45 (APPROVED) states four times — including in a **MUST**, `FR-BD-012` — that *"#45
> supplies confidence; **#30 decides** the sacking."* #30's approved section files contain **no** sacking,
> dismissal, or termination text whatsoever (§2(a)).

And underneath it, a harder constraint: a manager without a club is **structurally unrepresentable** in the
save format — `SeasonState`'s constructor throws when `managedClubId` is not in the club set (§2(b)). So
even if a sacking were decided, there is nowhere for the career to go.

Everything the master plan lists as Stage-5 manager career mode — job offers, reputation, moving clubs —
sits on top of that missing floor. The roadmap and plan-file rows for #54 land alongside this supplement
(§8.1), per the v0.2/v0.4 gap-fill precedent.

## 1. Scope

**#54 owns:** the **manager entity** and their **tenure** (appointment → employment → termination), the
**career record** and the **reputation** projected from it, the **job market** (vacancies, interest,
offers), and the **unemployed** state that makes all three representable.

**#54 does not own:**

| Not owned | Owner | How #54 relates |
|---|---|---|
| **Board confidence** | **#45** | Read-only input to the termination rule; #45 keeps its one-directional posture (KD-1) |
| The **objective** and its season evaluation | **#30** | #54 reads the outcome; it does not grade the season (KD-1) |
| The day/season loop | **#30** | #54 exposes a step; #30 invokes it (KD-1) |
| **Club** finances, facilities, squads | #40 / #53 / #27 | Vacancy attractiveness reads them as values; #54 owns none (KD-3) |
| **Player** morale/personality | **#33** | A manager is not a player record (KD-2) |
| In-match tactical AI (`ManagerProfile`, `ManagerMode`) | **#26** | A different "manager" entirely — see the naming hazard in §7 |

## 2. What already exists (verified)

**(a) The sacking decision is assigned by an approved MUST to a spec that does not contain it.** #45's
`section-2.md` `FR-BD-012`: *"#45 MUST NOT expose a sacking API, and MUST NOT fire any event that
terminates a manager. It supplies confidence; **#30 decides**."* Its `section-1.md` (KD-3) and `outline.md`
repeat it, and `appendices.md` adds *"#30 owns what a band means for the sacking decision"*.

A `grep` for `sack` / `dismiss` / `unemploy` across `docs/specs/season-competition-loop/` returns
**nothing**. #30 owns `BoardState`, the objective, and — from #45 T2 — a derived `JobSecurityBand`. It owns
no rule that ends a tenure.

**Consequence:** this is #53's finding in a second place: a responsibility named, designed around, and
owned by nobody. #45's design is *correct* and must not change — its one-directional posture is what keeps
confidence a single truth. What is wrong is only the **name of the counterparty**, and KD-1 supplies the
real one.

**(b) An unemployed manager cannot be represented — verified in code, not inferred.**
`src/season-save/SeasonState.cs` validates in its constructor:

```
$"ManagedClubId {managedClubId} is not in the season's club set."
```

and `appendices.md` row 3a lists `managedClubId i32` as *"the human manager's club"* — a mandatory field
(the omission of which was `ERR-030-011`, filed precisely because a season cannot be reconstructed
without it).

**Consequence:** "sacked" has no representable successor state. This bounds #54's minimal tier more than
any design preference could: **either** #54 introduces an unemployed representation (a #30 back-prop,
KD-5), **or** a sacking must end the career outright. KD-4 chooses, and the choice is forced by what a
career *is*.

**(c) Rival clubs have no managers, and the seam that would simulate them is a documented null.** #22's
`WorldLoop` phase-5 (`BackgroundTierSim`) is deliberately unbuilt because it *"summarises club-AI /
transfer / **sacking** outcomes that do not exist yet"* — FR-LW-031 forbids building the consumer first.

**Consequence:** a minimal #54 must generate **vacancies** without modelling **rival managers**. That is
not a shortcut, it is the phantom rule: a vacancy is a property of a *club* (which exists), while a rival
manager is an entity nothing else can yet observe. KD-3 draws the line there, and #22's phase-5 becomes
the natural deep-tier home rather than something #54 duplicates.

**(d) The `#45 → JobSecurity` precedent already teaches this spec its hardest lesson.** `ERR-030-009` took
#30's independent `JobSecurity` scalar and made it a **derived band** over #45's confidence, because
*"holding an independent scalar alongside #45's confidence would be two truths for one quantity, diverging
at the first restore with nothing to detect it."*

**Consequence:** **reputation must not be an independently stored number** (KD-2). It is exactly the shape
that invites one — a scalar everyone wants to read — and the identical failure is available: a stored
reputation that drifts from the career record it claims to summarise.

**(e) A `ManagerProfile` / `ManagerMode` already exists and means something else.** #26 (Tactical Presets)
ships `ManagerProfile` and `ManagerMode.Human` in `src/match-engine` for **in-match tactical adaptation**.

**Consequence:** #54's types must not reuse those names. This is the `TacticTranslation` / `PlayerAttributes`
CS0104 class the project has hit twice; §7 records the check.

**(f) The determinism block is full, with reserved slack for exactly this situation.** Roadmap §6:
`0x20`–`0x2D` / 82–95 consumed, `0x2E`–`0x2F` / 96–97 reserved *"so that if a candidate currently
classified read-only/presentation/infra later discovers it needs a draw, it extends from `0x2E`/96 onward"*.

**Consequence:** #54 is a plausible first claimant — job-market interest is naturally stochastic. KD-6
declines to claim it yet, following #40/#29: the row stays `_RESERVED_`, unpromoted, until a real draw
exists.

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | The manager entity + career record + tenure state, with **one** appointment (the career's starting club) and **no** vacancies, offers, or rival managers. Termination is **representable and terminal-for-now**: the career records the ended tenure and the manager is unemployed, but with no vacancy source there is nothing to be appointed to until S3 — so the honest minimal-tier statement is *"the save survives a sacking"*, not *"the player continues after one"*. Reputation is a projection over the record (KD-2). No draw. |
| **S3** | Vacancies generated from club state; interest and offers; moving clubs mid-career (KD-5). This is where the `0x2E` draw likely becomes real (KD-6). |
| **S5 (deep)** | Rival managers as entities, via #22's phase-5 `BackgroundTierSim` (§2(c)); manager personality via #33; international appointments alongside #36. |

**The minimal tier's value is the same as #53's:** it makes the missing floor exist. #45's confidence and
#30's objective already produce the *inputs* to a termination decision every season; today they flow into
nothing.

## 4. Key decisions

### KD-1 — **#54 owns tenure end to end**; #45 keeps confidence, #30 keeps the objective

The termination *decision* and its *consequences* live in one spec, because splitting them is what created
§2(a): #45 correctly refused the decision, #30 was named and never took it, and the consequence
(unemployment) was nobody's.

- **#45 → #54:** read-only confidence. #45's `FR-BD-012` posture is preserved *exactly* — it exposes no
  sacking API and fires no terminating event. Only the sentence naming its counterparty changes (§8.1).
- **#30 → #54:** the season objective outcome, and a **tick-order/boundary slot** that invokes #54's
  evaluation. #30 gains a seam, not a mechanic — the same relationship it has to #40's `SettleFinances`
  and #41's `AdvanceMedicalDay`.
- **#54 → nobody:** it exposes tenure state read-only and references no consumer.

**Why not give it to #30, as `FR-BD-012` says?** #30 is the career *spine* and could host the rule. But the
decision's consequences — unemployment, a job market, reputation, a change of club — are a system in their
own right, and #30 is already the project's most cross-referenced spec. Putting the *rule* in #30 and the
*aftermath* in #54 would split one causal chain across two owners, which is precisely the shape that
produced a MUST pointing at a spec that never implemented it.

### KD-2 — Reputation is a **projection over a stored career record**, never an independent scalar

The career record is the truth: appointments, seasons served, final positions against objective, trophies,
terminations. `Reputation` is computed from it on read (§2(d)).

**Why this is the load-bearing choice rather than an implementation detail:** a stored scalar and a stored
history are two representations of one thing, and #45's `ERR-030-009` documents exactly what happens —
they diverge at the first restore, *"with nothing to detect it"*, because both values are individually
plausible. A projection cannot diverge from its source. The cost — recomputation on read — is trivial at
world-tick cadence.

The career record is **APPEND-only**. A completed tenure is history and is never rewritten, which also
gives #22 the durable substrate its §7 *"reputation persistence … beyond a single career"* extension
anticipates.

### KD-3 — A vacancy is a property of a **club**; a rival manager is an entity #54 does not invent

Per §2(c), the minimal and S3 tiers generate vacancies from **club state that already exists** — league
position against objective, finances (#40), facilities (#53), squad strength (#27) — with no rival-manager
entity behind them. A club is "looking for a manager"; nobody was sacked from it, because there was nobody
there.

**This is honest rather than expedient**, and the distinction matters at the boundary: #54 must not emit
events implying a rival manager was dismissed, and must not let the player observe a rival's tenure. When
#22's phase-5 lands, rival managers become real and #54's vacancy source is *replaced by* their outcomes —
a producer swap behind an unchanged surface, not a redesign.

**Attractiveness is a read-only projection** over those club values, in the value-input pattern #42/#29/#53
already use: #54 references none of those assemblies; the root supplies the values.

### KD-4 — What a termination *does*, at minimal: the career **continues, unemployed**

Given §2(b) there are two candidates, and they are not equally honest:

- *End the career on sacking.* Simple, needs no #30 change — and it makes the game's response to its own
  most dramatic event *"load your last save"*. It also makes #45's entire confidence model a countdown to
  a game-over screen, which is not what a management game is.
- *Continue, unemployed* (**adopted**). The world keeps simulating; the manager holds no club until
  appointed. This requires the unemployed representation (KD-5) and is the only version in which
  reputation, vacancies, and offers mean anything.

**Mid-season termination is the case that forces the design**, and it must be stated: the league continues
without the player, so #30's loop must be able to advance a season **in which the human manages nobody** —
every fixture resolving through the round-resolution model rather than the engine. That capability already
exists (#30's `RoundResolutionMode`); what does not exist is a season state that can *express* it.

**And the mirror case — appointment — has a hazard #45 has already documented.** Taking a new job means the
new club needs a `BoardConfidence`, and #45's `FR-BD-005a` (a MUST) requires `{BoardConfidence,
OwnershipProfile}` to be inserted **as a factory-built pair, guarded at insertion**, precisely because
`default(BoardConfidence)` is *field-in-range yet semantically severe*: confidence `0` is the `Critical`
band — *"dismissal imminent"* — with a `LastAdvancedWorldDay = 0` that no-ops the day-0 guard. A naive
appointment that default-constructs the record would therefore hand a manager a **new job in crisis on day
one**, and #45's insertion guard would throw rather than allow it (the good failure, but still a crash on a
normal career action).

So #54 must state two things the seam does not imply:

- **An appointment initialises the club's board confidence to the factory honeymoon value**, not to
  `default`, and not to the predecessor's standing. Inheriting a crisis is defensible as a *design*, but it
  must be chosen: confidence is the board's view of **the current manager**, and the new manager has no
  record at that club yet.
- **The insertion is performed by the command layer, not by #54.** #54 records the tenure; the root calls
  #45's factory-built insertion — the same two-spec join KD-1 uses for the evaluation and #53 uses for a
  purchase. This is what keeps #54 from acquiring a *write* into #45's store, which would break both #54's
  leaf position (§10) and #45's one-directional guarantee.

### KD-5 — The unemployed representation is a **#30 back-prop**, and a sentinel is the wrong shape

`SeasonState.ManagedClubId` is validated against the club set (§2(b)). Two ways to represent unemployment:

- *A sentinel* (`-1`, as `NO_ROSTER_CLUB_ID` does in the match engine). Rejected here: `ManagedClubId` is
  read at many sites that legitimately assume a club (fixture routing, the engine-vs-model decision, the
  table view), and a sentinel makes every one of them a latent crash that only fires for an unemployed
  save — a state the whole test corpus currently cannot construct.
- **An explicit optional** — `ManagedClubId` becomes *nullable / presence-flagged*, so every read site is
  forced by the type to state what it does when there is no club (adopted). The compiler enumerates the
  work rather than leaving it to be discovered in the field.

Either way it is a `SEASON_STATE_FORMAT_VERSION` bump. **#45's `ERR-030-009` already queues one** for the
same block (the `JobSecurity` float → band change), so the honest recommendation is to **land both in one
bump** if the tiers align — one version step, one #50 registry row, one refusal boundary for existing
saves, instead of two.

### KD-6 — Determinism: minimal is draw-free; `0x2E` / 96 stays **reserved, not promoted**

The minimal tier has no stochastic element: tenure evaluation is a rule over confidence and objective
outcome; reputation is arithmetic over a record.

From S3, job-market interest is naturally stochastic, and #54 is the plausible first claimant of the
roadmap's reserved slack (§2(f)). Following #40 (`_RESERVED_0x29_`) and #29 (`0x21`), #54's promotion
**adds the `_RESERVED_0x2E_` placeholder row** — reserved, **not** a named tag — and promotes it only when
a real draw site exists. Claiming a tag for a tier that draws nothing is the phantom in its determinism
form.

**When it does promote, one subsystem-wide stream with keyed action ordinals** — never one per club or per
vacancy (the #45 KD-2 rule, which exists because the shared `MaxRngStreams = 64` bound is a real ceiling
#42's R-1 records).

**Identity:** with one appointment, no vacancies, and no draw, a minimal #54 changes nothing observable —
it records what already happens.

### KD-7 — Persistence: one APPEND-only career block, outliving the season

```
ManagerCareer : { managerId,
                  tenures : [ { clubId, startWorldDay, endWorldDay|open,
                                endReason, seasonsServed, finishes[], trophies[] } ],
                  currentTenure : index | none }        # 'none' == unemployed (KD-4/KD-5)
```

A `CAREER_SAVE_FORMAT_VERSION`-gated sub-blob, composed opaquely by `SeasonSaveCodec` like every other
management block. **Reputation is absent from the shape by design** (KD-2) — if it appears in this
structure, the projection has become a second truth.

**The career outlives the season**, which is the one structural difference from its neighbours: #30's
season state is replaced at each boundary roll, while a tenure spans them. That is why it is its own block
rather than a season-state field, and it is what makes the block meaningful across the multi-season careers
the master plan's Stage-5 mode assumes.

## 5. Persistent state (shape)

The `ManagerCareer` block of KD-7, in a `CAREER_SAVE_FORMAT_VERSION`-gated sub-blob composed opaquely by
`SeasonSaveCodec`. Two properties distinguish it from its neighbours and are worth restating here, since
this is the section an implementer reads for the save layout:

- **It outlives the season.** #30's season state is replaced at each boundary roll; a tenure spans them.
- **Reputation is absent by design** (KD-2) — its presence in this structure would *be* the second truth.

Integer-only (days, counts, ordinals), so the block carries no `float` and avoids the representation class
`ERR-030-009` had to resolve in #30's season block.

## 6. Determinism posture

- Minimal: **draw-free**; `_RESERVED_0x2E_` / 96 reserved, unpromoted (KD-6).
- World-tick only (`WorldClock`) — tenure evaluation runs at #30's boundary/day slot, never a match loop.
- The career block serializes fully; save@N → restore → advance is byte-identical.
- Reputation is derived, so it cannot desynchronise across a restore (KD-2) — the failure `ERR-030-009`
  documents.

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `ManagerCareer` record + `CurrentTenure` | #54 | APPEND-only history (KD-7) |
| `EvaluateTenure(confidence, objectiveOutcome, worldDay) → Continue \| Terminate` | #30 slot → #54 | the rule §2(a) leaves unowned (KD-1) |
| `ReputationOf(manager) → int` | #54 → consumers | **projection**, never stored (KD-2) |
| `VacancyView` (attractiveness projection) | #54 → UI | over root-supplied club values (KD-3) |
| `Appoint(manager, club, worldDay)` / `Terminate(manager, reason, worldDay)` | command layer → #54 | the only tenure mutations. `Appoint` records the tenure **only** — the paired factory-built `BoardConfidence` insertion is the command layer's call into #45 (KD-4) |
| `CareerSaveCodec` | #54 | opaque sub-blob (KD-7) |

**CS0104 note (§2(e)):** #26 already ships `ManagerProfile` / `ManagerMode` for in-match tactical
adaptation. #54's types must take different names (`ManagerCareer`, `TenureState`, …), verified by a
`docs/specs/**` + `src/**` grep at T0 — the `TacticTranslation` / `PlayerAttributes` precedent.

## 8. Cross-spec back-props

### 8.1 At approval

| ID | Target | Change |
|---|---|---|
| **ERR-045-002** | #45 (`board-ownership-dynamics`, APPROVED) | `FR-BD-012`, KD-3, `section-1.md`, `outline.md` and `appendices.md` all name **#30** as the spec that decides the sacking. Re-point to **#54**. #45's own posture is **unchanged and still correct** — it exposes no sacking API and fires no terminating event; only the counterparty's identity is wrong (§2(a)). A MUST that names the wrong spec is worse than one that names none, because it reads as settled. **Also confirm** that `FR-BD-005a`'s factory-built pair insertion remains available **mid-career**, not only at world genesis — #54's appointment path depends on inserting a `{BoardConfidence, OwnershipProfile}` pair for a club the manager has just joined (KD-4). If #45's store is genesis-populated for every club, this reduces to a no-op and the back-prop records that instead. (`ERR-045-001` is filed; `-002` is next free — verified.) |
| **ERR-030-021** | #30 (`season-competition-loop`, APPROVED) | (i) Record that **#54 owns tenure and termination**, and add the tick-order/boundary slot invoking `EvaluateTenure` (KD-1) — filed at approval, since #30's order is a pinned sequence (the `ERR-030-008` / `ERR-030-020` precedent). (ii) Make `ManagedClubId` an **explicit optional** so an unemployed career is representable (KD-5), carrying a `SEASON_STATE_FORMAT_VERSION` bump — **to be combined with `ERR-030-009`'s queued bump on the same block if the tiers align**, so existing saves face one refusal boundary rather than two. (Proposed `ERR-030-*` reach `-020` (#53); `-021` is next free — verified.) |

**Governance, landing with the same commit:** the roadmap row + §3 sketch + §7 wave placement for #54, and
`spec-plans/spec-54-manager-career-reputation.md` — the v0.2 / v0.4 gap-fill precedent.

### 8.2 Deferred (land at the named tier)

- **`_RESERVED_0x2E_` / 96 → a named tag** in #16 §3.4, only when the S3 job-market draw exists (KD-6).
- **Rival managers** via #22's phase-5 `BackgroundTierSim` (§2(c)) — a producer swap behind #54's vacancy
  surface, at S5.
- **Manager personality** via #33, and **international appointments** alongside #36 — S5.
- **#31's** reputation-influenced negotiation, if wanted: a value input, never a reference.

### 8.3 Explicitly **not** back-props

- **#45's confidence model** — untouched; #54 is a reader (KD-1).
- **#40 / #53 / #27** — vacancy attractiveness reads root-supplied values; no spec changes (KD-3).
- **#16** — nothing named at minimal; only a `_RESERVED_` row at promotion (KD-6).
- **#26** — a different "manager"; #54 avoids the names rather than amending #26 (§2(e)).

## 9. Test focus

**The unemployed state, which is the whole floor** (KD-4/KD-5): a career survives a mid-season termination
— the season advances to its boundary with **no** managed club, every fixture resolving through the
round-resolution model, and the save round-trips byte-identically in that state. This is the case the
current codec cannot even construct (§2(b)), so it is also the test that proves the back-prop landed.

**Reputation cannot diverge** (KD-2): reputation after save → restore → recompute equals the uninterrupted
value, asserted by construction (there is no stored field to compare) — plus a structural check that the
career block contains no reputation field, which is what stops a future contributor from "caching" it.

**Tenure evaluation is a pure rule** (KD-1): the same confidence + objective outcome always yields the same
verdict, and #45's state is unchanged by the call (it is a reader — the direction `FR-BD-012` protects).

**Appointment does not start a career in crisis** (KD-4): appointing to a new club yields a **factory**
`BoardConfidence`, never `default` — asserted at the appointment path, since `default` is field-in-range and
reads as `Critical`/*"dismissal imminent"* (#45 `FR-BD-005a`/`F4a`). The companion lock: `Appoint` alone
performs **no** write into #45's store, which is what keeps #54 a leaf.

**Append-only history** (KD-7): a completed tenure is never rewritten by a later appointment; ordinal
stability on `endReason`.

**Identity** (KD-6): a career with one appointment and no vacancies produces behaviour identical to today's
single-club career.

## 10. Reference DAG

```
root → {#54, #30, #45, #40, #53, #27}      #54 → { }      #45 → { }      #30 → { }
```

**#54 is a leaf.** It reads confidence and objective outcomes as **values supplied by the root**, and
exposes tenure read-only. In particular it does **not** reference #45 — which matters more than usual here,
because the natural implementation of "read board confidence to decide a sacking" is a direct reference,
and that would put a Wave-6 spec inside #45's one-directional guarantee. The wave's established inversion
(#48's cue sink, #50's registry, #51's mapping, #53's projections) applies unchanged.

## 11. Risks and standing options

- **R-1 — the unowned MUST is the reason to act** (§2(a)). `FR-BD-012` will otherwise keep pointing at #30,
  and the first person to implement #45's confidence will look for the sacking rule, not find it, and put
  one somewhere convenient.
- **R-2 — KD-5's format bump is the expensive part**, and it lands on a block that already has a queued
  bump (`ERR-030-009`). Combining them is the recommendation; if the tiers cannot align, the cost is two
  refusal boundaries for existing saves, which #50 handles but players experience.
- **R-3 — scope creep toward "career mode as a whole".** Manager attributes, coaching badges, media
  relationships and international jobs all attach naturally. The eventual §1 should hold at **tenure +
  record/reputation + job market**, with the rest as named deferrals.
- **R-4 — rival managers are the tempting shortcut** (KD-3). Inventing them at S3 to make vacancies feel
  alive would build the consumer #22's phase-5 is meant to produce — the phantom rule, in the exact place
  the project already documented it.
- **R-5 — the reserved slack is finite** (§2(f)/KD-6). #54 claiming `0x2E` at S3 leaves `0x2F` / 97 as the
  last slot. That is a fact for the roadmap to carry, not a reason to avoid the tag when a real draw
  exists.

## 12. Promotion pipeline

1. **This supplement, AR-converged** — **DONE at v0.4.** AR-1 (0H+1M) → v0.2, AR-2 (0H+1M) → v0.3,
   AR-3 (0H+0M+2L) → v0.4 = **CONVERGENCE** (an L-only round closes the cycle, per the project
   convention).
2. **Roadmap + plan-file rows** for #54 (§7.1 governance note).
3. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/manager-career-reputation/`, FR
   prefix `FR-MC`.
4. **Section-file PASS-1 adversarial review** + a fix pass.
5. **`SPEC_INDEX.md` registry row** at promotion.
6. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
7. **Flip to `APPROVED`**, landing the §8.1 back-props atomically.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement opening **#54** as a gap-fill candidate. The trigger is a verified ownership hole with two layers. **(1)** #45 (APPROVED) says four times — including the **MUST** `FR-BD-012` — that *"#45 supplies confidence; **#30** decides the sacking"*, while `grep sack\|dismiss\|unemploy` over `docs/specs/season-competition-loop/` returns **nothing**: the termination rule is assigned to a spec that does not contain it (the #53 finding, in a second place). **(2)** Underneath it, an unemployed manager is **structurally unrepresentable** — `SeasonState`'s constructor throws when `managedClubId` is not in the club set (verified in source, and `appendices.md` row 3a makes the field mandatory) — so even a decided sacking has nowhere to go. **KD-1** puts tenure end-to-end in #54, preserving #45's one-directional posture exactly and giving #30 a seam rather than a mechanic; splitting rule from aftermath is what produced the orphaned MUST. **KD-2** makes reputation a **projection over an APPEND-only career record**, applying `ERR-030-009`'s lesson pre-emptively — a stored scalar beside a stored history is two truths that *"diverge at the first restore with nothing to detect it"*. **KD-4** chooses *continue-unemployed* over *end the career*, because the alternative makes the game's answer to its own most dramatic event "load your last save", and notes that mid-season termination requires #30 to advance a season the human manages no club in (a capability `RoundResolutionMode` already has, and a state the save format cannot express). **KD-5** prefers an **explicit optional** over a `-1` sentinel so the compiler enumerates every read site rather than leaving latent crashes reachable only from a save the corpus cannot currently construct, and recommends combining its format bump with `ERR-030-009`'s queued one. **KD-3** generates vacancies from **club** state without inventing rival managers, leaving #22's phase-5 as the deep-tier producer (FR-LW-031). **KD-6** keeps `_RESERVED_0x2E_` / 96 reserved-not-promoted at minimal (#40/#29 precedent). Two back-props: ERR-045-002 (re-point the MUST) and ERR-030-021 (tenure slot + optional `ManagedClubId`). |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 0H + 1M, resolved.** **M-1** — the section numbering had drifted from the house layout (no §5 persistent-state section; back-props at §7 rather than §8), and §0 already cross-referenced *"(§8.1)"* for the governance note — so the document's own internal reference pointed at the wrong section before anyone read it. Renumbered to the sibling layout (§5 persistent state · §6 determinism · §7 surfaces · §8 back-props · §9 tests · §10 DAG · §11 risks · §12 promotion) and the new §5 written, which also gave the save-layout properties (the block outlives the season; reputation is absent by design) a home where an implementer looks for them. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 0H + 1M, resolved.** **M-1** — the design covered termination in detail and left **appointment** — its mirror — unspecified, where #45 has already documented a trap: `FR-BD-005a` (a MUST) requires `{BoardConfidence, OwnershipProfile}` to be inserted as a **factory-built pair**, because `default(BoardConfidence)` is field-*in-range* yet means the `Critical` band, *"dismissal imminent"*, with a day-0 guard that no-ops. A naive appointment would hand a manager a new job already in crisis (and trip #45's insertion guard — a crash on an ordinary career action). Added: an appointment initialises confidence to the factory honeymoon value, **not** `default` and **not** the predecessor's standing (inheriting a crisis is defensible but must be *chosen*, since confidence is the board's view of the *current* manager); and the insertion is executed by the **command layer**, not #54 — otherwise #54 acquires a *write* into #45's store, breaking both its own leaf position (§10) and #45's one-directional guarantee. §7 and §9 aligned. |
| v0.4 | July 26, 2026 | **AR-3 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle). **L-1** — §8.1's `ERR-045-002` asked only for the counterparty re-point, but AR-2's appointment path assumes #45's factory-built pair insertion is available **mid-career** rather than only at world genesis; an assumption a back-prop depends on belongs in the back-prop, so the entry now asks for confirmation either way. **L-2** — §3's minimal row said termination *"has exactly one deterministic consequence"*, which reads as a designed outcome when the truth is plainer: with no vacancy source until S3, the honest minimal claim is *"the save survives a sacking"*, not *"the player continues after one"*. Stated as such, since overselling a minimal tier is how a tier gets called finished. |
