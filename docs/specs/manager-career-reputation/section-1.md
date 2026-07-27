# Manager Career, Reputation & Job Market #54 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 1.1 Purpose

#54 is the spec that makes the manager a **career** rather than a fixed point: appointed to a club, judged
over seasons, terminated, unemployed, appointed again. It owns the tenure lifecycle, the record that
accumulates from it, the reputation projected over that record, and the job market that gives an
unemployed manager somewhere to go.

**It exists because of a hole, not because of a feature request.** The project has a fully-specified path
to *"you are about to be sacked"* — #45's board confidence, drifting daily, banded into `Critical` — and
**no specified behaviour for being sacked**. #45 says four times, including in a MUST, that #30 decides
it; #30 contains no such rule. And underneath that, a manager without a club **cannot be represented at
all**: `SeasonState`'s constructor throws.

So #45's confidence model is currently a countdown to nothing. §1.4 records both findings; KD-1 and KD-5
answer them.

## 1.2 Scope

**In scope**

- The **manager entity** and **tenure** — appointment, employment, termination, unemployment.
- The **career record**: an APPEND-only history of tenures, seasons served, finishes, trophies, endings.
- **Reputation**, as a projection over that record.
- The **job market** — vacancies, interest, offers (S3).

**Out of scope** — each already has an owner; duplicating it is the failure this section prevents:

| Not owned | Owner | #54's relation |
|---|---|---|
| **Board confidence** | **#45** | a read-only **value** input to the termination rule; #45 keeps its one-directional posture exactly (KD-1) |
| The **objective** and its season evaluation | **#30** | #54 reads the committed outcome; it does not grade the season |
| The day / season loop | **#30** | #54 exposes a step; **#30 invokes it** |
| **Club** finances, facilities, squads | #40 / #53 / #27 | vacancy attractiveness reads them as **root-supplied values**; #54 owns none (KD-3) |
| **Player** morale and personality | **#33** | a manager is not a player record |
| **Rival managers** as entities | #22's phase-5 `BackgroundTierSim` | #54 generates **vacancies**, never rival tenures (KD-3) |
| In-match tactical AI (`ManagerProfile`, `ManagerMode`) | **#26** | **a different "manager" entirely** — see the CS0104 note in §4.1 |
| Cross-version save migration | **#50** | #54 declares its own version and fails loud |

## 1.3 Dependencies

**Upstream (consumed): none as references.** #54 is a **leaf** (§4.1). Board confidence, objective
outcomes, and club values all arrive as **integers the root supplies**.

**Downstream (consumers):**

- **#30**, which invokes `EvaluateTenure` at a boundary/day slot and holds the (now optional) managed
  club id.
- **#45**, indirectly: the **command layer** inserts a factory-built `{BoardConfidence,
  OwnershipProfile}` pair on appointment (KD-4). **#54 never writes into #45's store.**
- **#38 UI** — value copies of tenure, record, reputation and vacancies.
- **#31** *(deferred)* — reputation as a negotiation input, if ever wanted: a **value**, never a
  reference.

**Reference DAG**

```
root → {#54, #30, #45, #40, #53, #27}      #54 → { }      #45 → { }
```

**#54 is a leaf.** It reads confidence and objective outcomes as **values supplied by the root** and
exposes tenure read-only. That it does **not** reference #45 matters more than usual here: the natural
implementation of *"read board confidence to decide a sacking"* is a direct reference, and that would put
a Wave-6 spec inside #45's one-directional guarantee.

## 1.4 What verification changed

**(a) The sacking decision is assigned by an approved MUST to a spec that does not contain it.** #45's
`FR-BD-012`: *"#45 MUST NOT expose a sacking API, and MUST NOT fire any event that terminates a manager.
It supplies confidence; **#30 decides**."* Its §1.5 KD-3, `outline.md` and `appendices.md` repeat it.

A search for *sack* / *dismiss* / *unemploy* across `docs/specs/season-competition-loop/` returns
**nothing**. #30 owns `BoardState`, the objective, and — from #45 T2 — a derived `JobSecurityBand`. It
owns **no rule that ends a tenure**.

**Consequence:** this is #53's finding in a second place — a responsibility named, designed around, and
owned by nobody. **#45's design is correct and must not change**: its one-directional posture is what
keeps confidence a single truth. What is wrong is only the **name of the counterparty**.

**A MUST that names the wrong spec is worse than one that names none**, because it reads as settled. The
first person to implement #45's confidence will look for the sacking rule in #30, not find it, and put one
somewhere convenient.

**(b) An unemployed manager cannot be represented — verified in code, not inferred.**
`src/season-save/SeasonState.cs` validates in its constructor:

```
$"ManagedClubId {managedClubId} is not in the season's club set."
```

and #30's Appendix B row 3a lists `managedClubId i32` as *"the human manager's club"* — a **mandatory**
field, whose omission from the §3.6 pseudocode was filed as `ERR-030-011` precisely because a season
cannot be reconstructed without it.

**Consequence:** *"sacked"* has **no representable successor state**. This bounds #54's minimal tier more
than any design preference could: either #54 introduces an unemployed representation (a #30 back-prop), or
a sacking must end the career outright. KD-4 chooses, and the choice is forced by what a career *is*.

**(c) Three supporting facts, each of which removes a decision rather than adding one.** Rival clubs have
no managers and #22's phase-5 `BackgroundTierSim` — the seam that would simulate them — is a **documented
null** precisely because it *"summarises club-AI / transfer / **sacking** outcomes that do not exist
yet"*; `ERR-030-009` already teaches this spec its hardest lesson by turning an independent `JobSecurity`
scalar into a derived band; and #26 already ships a `ManagerProfile` / `ManagerMode` that means something
else entirely.

## 1.5 Key decisions

### KD-1 — #54 owns tenure end to end; #45 keeps confidence, #30 keeps the objective

The termination **decision** and its **consequences** live in one spec, because splitting them is exactly
what created §1.4(a): #45 correctly refused the decision, #30 was named and never took it, and the
consequence — unemployment — was nobody's.

- **#45 → #54:** read-only confidence, as a **routed integer**. #45's `FR-BD-012` posture is preserved
  *exactly*: it exposes no sacking API and fires no terminating event. **Only the sentence naming its
  counterparty changes** (ERR-045-002).
- **#30 → #54:** the season objective outcome, and a **tick-order/boundary slot** that invokes #54's
  evaluation. #30 gains a **seam, not a mechanic** — the same relationship it has to #40's
  `SettleFinances` and #41's `AdvanceMedicalDay`.
- **#54 → nobody:** it exposes tenure state read-only and references no consumer.

**Why not give it to #30, as `FR-BD-012` says?** #30 is the career *spine* and could host the rule. But
the decision's consequences — unemployment, a job market, reputation, a change of club — are a system in
their own right, and #30 is already the project's most cross-referenced spec. Putting the **rule** in #30
and the **aftermath** in #54 would split one causal chain across two owners — which is precisely the shape
that produced a MUST pointing at a spec that never implemented it.

### KD-2 — Reputation is a projection over a stored career record, never an independent scalar

The **career record is the truth**: appointments, seasons served, final positions against objective,
trophies, terminations. `Reputation` is computed from it **on read**.

**This is the load-bearing choice rather than an implementation detail.** A stored scalar and a stored
history are two representations of one thing, and `ERR-030-009` documents exactly what happens: they
*"diverge at the first restore, with nothing to detect it"*, because both values are individually
plausible. A projection **cannot** diverge from its source. The cost — recomputation on read — is trivial
at world-tick cadence.

Reputation is also the shape that most invites a stored scalar: it is a single number everyone wants to
read, cheap to cache and expensive to reconcile. FR-MC-011 therefore forbids the field, and §5.2 asserts
its **absence structurally** — because a prose rule does not survive a contributor who notices the
recomputation.

The career record is **APPEND-only**. A completed tenure is history and is never rewritten, which also
gives #22 the durable substrate its §7 *"reputation persistence beyond a single career"* extension
anticipates.

### KD-3 — A vacancy is a property of a club; a rival manager is an entity #54 does not invent

The minimal and S3 tiers generate vacancies from **club state that already exists** — league position
against objective, finances (#40), facilities (#53), squad strength (#27) — with **no rival-manager entity
behind them**. A club is *"looking for a manager"*; nobody was sacked from it, because there was nobody
there.

**This is honest rather than expedient, and the distinction matters at the boundary:** #54 must not emit
events implying a rival manager was dismissed, and must not let the player observe a rival's tenure.
Inventing rival managers to make vacancies feel alive would build the consumer #22's phase-5 is meant to
**produce** — the phantom rule, in the exact place the project already documented it.

When phase-5 lands, rival managers become real and #54's vacancy source is **replaced by** their outcomes
— a **producer swap behind an unchanged surface**, not a redesign.

**Attractiveness is a read-only projection** over root-supplied club values, in the value-input pattern
#42/#29/#53 already use. #54 references none of those assemblies.

### KD-4 — What a termination does: the career continues, unemployed — and appointment is its mirror

Given §1.4(b) there are two candidates, and they are not equally honest:

- *End the career on sacking.* Simple, needs no #30 change — and it makes the game's response to its own
  most dramatic event *"load your last save"*. It also makes #45's entire confidence model a countdown to
  a game-over screen, which is not what a management game is.
- **Continue, unemployed** (adopted). The world keeps simulating; the manager holds no club until
  appointed. This requires the unemployed representation (KD-5), and it is the only version in which
  reputation, vacancies and offers mean anything.

**Mid-season termination is the case that forces the design**, and it must be stated: the league continues
without the player, so #30's loop must be able to advance a season **in which the human manages nobody** —
every fixture resolving through the round-resolution model rather than the engine. **That capability
already exists** (#30's `RoundResolutionMode`); what does not exist is a season state that can *express*
it.

**The mirror case — appointment — carries a hazard #45 has already documented.** Taking a new job means
the new club needs a `BoardConfidence`, and #45's `FR-BD-005a` (a MUST) requires `{BoardConfidence,
OwnershipProfile}` to be inserted as a **factory-built pair, guarded at insertion**, precisely because
`default(BoardConfidence)` is *field-in-range yet semantically severe*: confidence `0` is the `Critical`
band — *"dismissal imminent"* — with a `LastAdvancedWorldDay = 0` that no-ops the day-0 guard.

A naive appointment that default-constructs the record would therefore hand a manager a **new job in
crisis on day one**, and #45's insertion guard would throw — the *good* failure, but still a crash on an
ordinary career action. So #54 states two things the seam does not imply:

- **An appointment initialises the club's board confidence to the factory honeymoon value** — not
  `default`, and **not the predecessor's standing**. Inheriting a crisis is defensible as a *design*, but
  it must be **chosen**: confidence is the board's view of the **current** manager, and the new manager
  has no record at that club yet.
- **The insertion is performed by the command layer, not by #54.** #54 records the tenure; the **root**
  calls #45's factory-built insertion — the same two-spec join KD-1 uses for the evaluation and #53 uses
  for a purchase. This is what keeps #54 from acquiring a **write** into #45's store, which would break
  both #54's leaf position and #45's one-directional guarantee.

### KD-5 — The unemployed representation is a #30 back-prop, and a sentinel is the wrong shape

`SeasonState.ManagedClubId` is validated against the club set (§1.4(b)). Two ways to represent
unemployment:

- *A sentinel* (`-1`, as `NO_ROSTER_CLUB_ID` does in the match engine). **Rejected here:**
  `ManagedClubId` is read at many sites that legitimately assume a club — fixture routing, the
  engine-vs-model decision, the table view — and a sentinel makes every one of them a **latent crash that
  only fires for an unemployed save**, a state the whole test corpus currently cannot even construct.
- **An explicit optional** — `ManagedClubId` becomes nullable / presence-flagged, so every read site is
  **forced by the type** to state what it does when there is no club (adopted). The compiler enumerates
  the work rather than leaving it to be discovered in the field.

Either way it is a `SEASON_STATE_FORMAT_VERSION` bump. **#45's `ERR-030-009` already queues one for the
same block** (the `JobSecurity` float → band change), so the honest recommendation is to **land both in
one bump** if the tiers align — one version step, one #50 registry row, one refusal boundary for existing
saves, instead of two.

### KD-6 — Determinism: minimal is draw-free; `0x2E` / 96 stays reserved, not promoted

The minimal tier has **no stochastic element**: tenure evaluation is a rule over confidence and objective
outcome; reputation is arithmetic over a record.

From S3, job-market interest is naturally stochastic, and #54 is the plausible **first claimant** of the
roadmap's reserved slack (`0x2E`–`0x2F` / 96–97, held back *"so that if a candidate currently classified
read-only/presentation/infra later discovers it needs a draw, it extends from `0x2E`/96 onward"*).

Following #40 (`_RESERVED_0x29_`) and #29 (`0x21`), #54's promotion **adds the `_RESERVED_0x2E_`
placeholder row** — reserved, **not** a named tag — and promotes it only when a real draw site exists.
Claiming a tag for a tier that draws nothing is the phantom in its determinism form.

**When it does promote: one subsystem-wide stream with keyed action ordinals** — never one per club or per
vacancy (#45's KD-2 rule, which exists because the shared `MaxRngStreams = 64` bound is a real ceiling
#42's R-1 records).

### KD-7 — Persistence: one APPEND-only career block that outlives the season

A `CAREER_SAVE_FORMAT_VERSION`-gated sub-blob, composed **opaquely** by `SeasonSaveCodec` like every other
management block. Version gate first; overflow-safe length prefixes against `total − offset`;
trailing-byte guard; fail loud on all three. Integer-only, so the block carries no `float` and avoids the
representation class `ERR-030-009` had to resolve in #30's season block.

**The career outlives the season, which is the one structural difference from its neighbours.** #30's
season state is **replaced** at each boundary roll, while a tenure **spans** them. That is why it is its
own block rather than a season-state field, and it is what makes the block meaningful across the
multi-season careers Stage-5 assumes.

**Reputation is absent from the shape by design** (KD-2). If it ever appears in this structure, the
projection has become a second truth.

### KD-8 — Behaviour-neutral identity, and its honest limit

With one appointment, no vacancies and no draw, a minimal #54 **changes nothing observable** — it records
what already happens. No stream is registered ⇒ every existing cursor is byte-identical; a career with a
single club produces behaviour identical to today's.

**The limit must be stated, because overselling a minimal tier is how a tier gets called finished.** With
no vacancy source until S3, the honest minimal-tier claim is **"the save survives a sacking"** — the
career records the ended tenure, the manager is unemployed, and the season advances and round-trips — and
**not** *"the player continues after one"*. There is nothing to be appointed to until S3.

That is still worth shipping, for the same reason #53's minimal tier is: it makes the missing floor exist.
#45's confidence and #30's objective already produce the *inputs* to a termination decision every season;
today they flow into nothing.

## 1.6 Determinism posture

- **World tick and season boundary only** (`WorldClock`); never the 10 Hz tactical or 60 Hz physics loops.
  #54 feeds no digest.
- **Minimal is draw-free** (KD-6): no `RegisterStream`, no domain tag. `_RESERVED_0x2E_` / ordinal 96 is
  **added as a placeholder and left unpromoted**.
- **All-integer** — days, counts, ordinals, per-mille. No float at any tier.
- **Reputation is derived**, so it **cannot desynchronise across a restore** (KD-2) — the failure
  `ERR-030-009` documents.
- The career block serializes fully; `save@N → restore → advance` is byte-identical.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §1 (scope, out-of-scope table, leaf DAG, §1.4's verification findings — the orphaned MUST and the unrepresentable unemployed state — KD-1..KD-7 from supplement v0.4 plus **KD-8** promoted to its own decision, determinism posture). KD-8 is separated because the identity claim's *limit* — the minimal tier makes a sacking survivable, not recoverable — is the part most likely to be overstated, and the supplement's own AR-3 caught exactly that. Status IN REVIEW. |
#endregion
