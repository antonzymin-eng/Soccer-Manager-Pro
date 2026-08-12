# The Tackle — wiring backlog W2

> **Created:** August 12, 2026
> **Status:** DESIGN SUPPLEMENT — the same governance class as `match-engine-design.md`. Opens no
> numbered spec and changes no `SPEC_INDEX.md` row.
> **Owner document:** `docs/tracking/match-engine-wiring-backlog.md` **W2** (this is that item).
> **Purpose:** Nothing in this engine had ever made a tackle, and a player in control of the ball
> could not be dispossessed by any means at all. This note is the investigation, the defects it
> surfaced, the measurements that shaped the fix — including the two that corrected this note's own
> earlier conclusions — and the landing. **WIRED August 12, 2026** (`ERR-014-006`); the ownership
> question §4 put to the owner was answered: the outcome model back-propagates into **#14 §3.6.5**,
> with a fourth outcome, `BALL_LOOSE`, that the owner added.

---

## 0. This is a wiring task, not a realism pass

`match-engine-wiring-backlog.md` §0 and the gate at the top of the `match-realism-pass` skill both
say it: *is the subsystem this touches fully wired? If not, this is a wiring task, not a realism
pass.* The consequences for this note's shape are the W1 ones:

- **KD-W1 holds.** No `[GT]` governing an already-wired subsystem is retuned here. In particular the
  four discipline constants (`FoulCallProbability`, `RedCardProbability`, `YellowCardProbability`,
  `FoulCooldownTicks`) are **not** touched, even though this landing changes what they produce.
- **The deliverable is the wiring plus whatever the wiring surfaces**, and W2 surfaced a great deal.

There is one deviation from W1, and it is deliberate: **W1 shipped a live trigger with zero executed
measurement**, on the stated grounds that no .NET SDK was available. That excuse is void — `src/CLAUDE.md`
records (verified August 7, 2026) that `apt-get update && apt-get install -y dotnet-sdk-8.0` works from
the Ubuntu archive, and it works in this session. **W2 measures before it decides.**

---

## 1. What is actually dormant

The backlog names three dead links. Verified in source August 12, 2026, there are **four**, and the
fourth is the one that makes W2 more than a plumbing job.

| Link | State |
|---|---|
| `DefensiveAITick.GetTackleIntentRequests()` (`src/defensive-ai/DefensiveAITick.cs:358`) | Populated every 10 Hz stride. **No production reader** until this landing's instrument. |
| `GetAndClearTackleFlag` in `PassWorldAdapter` (`MatchEngine.cs:7248`) **and** `ShotWorldAdapter` (`:7321`) | Both hardcoded `=> false`. |
| `PassExecutor.UpdateWindup` (`src/pass-mechanics/PassExecutor.cs:408`) **and** `ShotExecutor.AdvanceWindup` (`:414`) | Unreachable. The backlog names only the pass side; the shot-windup interrupt is equally dead. |
| `PassCancelledEvent` / `ShotCancelledEvent` | **No production subscriber anywhere.** So even a working flag produces no possession consequence today. |

**And the finding that outranks all four:** `MatchEngine.RunFirstTouch` gate 1 refuses any possessed
ball, and nothing else writes `_possessingAgentId` away from a controlled carrier. The engine has
exactly two turnover mechanisms — *the carrier kicks it and a team-mate fails to receive*, and *a foul
restart*. **A player in control cannot be dispossessed, at all, under any pressure.** That is a missing
football mechanism, not a churn imbalance, and it is the real content of W2.

**The backlog's cited line numbers `:6721` / `:6789` are stale** — those offsets are now GK snapshot
deserialization. Corrected in the backlog in the same commit as this note.

---

## 2. Four things the pre-implementation council corrected, before any code

The council (`advisor-integrity` + `advisor-evidence`, convened on the plan, not the diff) refuted or
corrected four points of the first design. Recorded here because three of them would have shipped.

### 2.1 `ApproachAngle` does not mean what the plan read it to mean

`TackleIntentRequest.ApproachAngle` is `acos(dot(normalize(agent→opponent), normalize(agent.Velocity)))`
(`TackleIntentEvaluator.cs:67-80`). **0 = the defender is running straight at his man; π = he is running
directly away from him.** #14 §2.2.3 says exactly that and nothing more.

The first design weighted the tackle outcome by "from behind ⇒ likelier foul", which this quantity
cannot express — it carries **no information about which side of the carrier the defender is on**. That
reading came from the field's own XML doc (`src/defensive-ai/TackleIntentRequest.cs:29-31`), which
states *"π/2 = lateral; π = from behind"*. **The doc is wrong**, it contradicts the spec its own file
header cites, and it is fixed in this landing. The genuine from-behind geometry is spec-owned by #3's
`ContactTypeClassifier`, whose sign convention has been wrong twice already (ERR-003-002, ERR-003-006).

### 2.2 `Commit` does not mean "I am going to tackle"

`TackleIntentEvaluator.SelectMode` returns `Commit` whenever `coverageDepth >= TackleCommitCoverageFloor`
(default **1**, counted in a 5 m lateral corridor). **The ball is not an input and possession is not an
input.** `Commit` means *"I have cover behind me"*, nothing more. Any gating this design does on
"the target is the carrier" is doing all of the work, and a cooldown must be sized on that assumption
rather than on `Commit` being selective.

### 2.3 The tackle flag is redundant on a *won* tackle

`PassExecutor` already re-checks possession at CONTACT and cancels with `CancelReason.PossessionLost`
(FM-08, `PassExecutor.cs:470`). So a tackle that *takes the ball* needs no flag — the executor
self-cancels. Worse, `PassExecutor.Initiate` **drains and discards** the flag (`:351`) and polls it
**only during WINDUP** (`:408`), so a flag raised at the 10 Hz stride does something only if the target
is mid-windup that very tick and is otherwise swallowed silently.

**The flag's genuinely new coverage is therefore the case where a tackle disrupts the carrier without
winning the ball** — and that is the only thing this landing may claim for it. Claiming the flag
"makes the interrupt live" in general would be the second-copy-of-a-rule class.

### 2.4 The presser is structurally excluded from tackling

`HoldShapePoolFilter.BuildPool` (`src/defensive-ai/HoldShapePoolFilter.cs:53`) excludes every agent
with `PressRole.PrimaryPress` or `CoverShadow` (FR-DA-010 / KD-4). **The primary presser is by
definition the player #13 sends at the ball — so the one man most likely to be within 3 m of the
carrier is, by construction, the one who cannot produce tackle intent.**

Compounding it: `MarkAssigner` (`src/defensive-ai/MarkAssigner.cs`) **never reads the ball** — grep
returns zero ball references in the file. `DefensiveAgentSnapshot.HasBall` **is** populated by the
engine (`MatchEngine.cs:3348`) and is read by nothing in the assignment path: a field written and never
read, the **C5 class** the backlog already tracks, in a second assembly.

Together these mean `TargetEntityId == carrier` is a coincidence, not a design. Whether that
coincidence is common enough to build on is not a thing to reason about — it is a thing to measure,
which is §3.

---

## 3. The measurement — the census, and what it decides

**Instrument:** `src/match-engine/tests/TackleIntentDiagnosticTests.cs`
(`TD_TACKLE_DIAGNOSTIC=1`), over the §5.Z.20 three-seed corpus at full match length, assertion-free
per the ERR-030-014 convention.

It is counted in **possession episodes**, not strides: at 10 Hz a match is ~54 000 strides, so a stride
count measures the sampling rate and is not comparable with football's per-90 rates. It is reported
**per defending team and never pooled** — the coverage-depth goal-side term and `LastManDetector.DefendsX0`
are team-relative, and three home/away asymmetry defects have shipped in this tree because a fixture
only ever used the home team (ERR-008-002).

The counters are chosen so that **a zero arrives with its cause attached**, because three unrelated
things produce one and they want opposite fixes:

| If | Then the bound is | And W2 is |
|---|---|---|
| `<=3m` ≈ 0 | nobody is ever near the man on the ball | dead **upstream**, at positioning — wiring the chain changes nothing |
| `<=3m` large, `poolElig` ≈ 0, `presser` large | §2.4 — the only man near the carrier is the excluded presser | a **#14/#13 spec question**: either the tackle producer belongs in #13, or FR-DA-010's exclusion is wrong for tackles. Not a decision a call site may take |
| `poolElig` large, `intent` ≈ 0 | `MarkAssigner` is not marking the carrier (§2.4) | a **producer** fix — give the assignment a ball model — before any outcome model |
| `intent` large, `COMMIT` ≈ 0 | the `CoverageDepth` floor or the last-man override | a `[GT]` bound, **frozen under KD-W1** — measure, do not tune |
| `COMMIT` non-trivial | the chain can fire | a **resolution** problem: §4 |

It also reports `ballGap` — episodes in which the "carrier" was ever more than 1 m from the ball he is
recorded as holding. Possession here is a **flag, not a kinematic constraint** (backlog W6:
`BallStateType.Controlled` has no producer), so tackler-to-carrier and tackler-to-**ball** are different
distances, and a contact gate calibrated against one while the mechanism uses the other is calibrated
against the wrong quantity.

### 3.0 RESULT — measured August 12, 2026

3 seeds × 90 min, both defending teams reported separately (6 team-matches). Per defending team per
match, means over the six:

| Quantity | Mean | Range | As a share |
|---|---|---|---|
| Defending episodes | **681.7** | 622–733 | — |
| …with an outfielder within 3.0 m of the carrier | **310.2** | 291–326 | 45.5% of episodes |
| …within 2.0 m | 228.7 | 213–249 | 33.5% |
| …within 1.5 m | 178.8 | 167–192 | 26.2% |
| …where that man was HOLD_SHAPE-**pool eligible** | **310.0** | 291–326 | **99.9% of the 3 m episodes** |
| …where he carried a #13 **press role** | **0.5** | 0–3 | **0.2%** |
| …with ≥1 tackle intent naming the carrier | 97.2 | 82–108 | 14.3% of episodes |
| …with ≥1 **COMMIT** naming the carrier | **65.3** | 50–77 | 9.6% of episodes |
| …where the carrier was ever >1 m from his own ball | 81.7 | 67–101 | 12.0% |

**W2 is a RESOLUTION problem, not a producer problem, and not dead upstream.** Against §3's decision
table the answer is the last row: `COMMIT` is not merely non-zero, it is **plentiful** — ~65 candidate
challenges per team per match against football's ~15–17 tackle attempts per team per 90. The gate
produces roughly **four times** the football rate, so the contact radius, the cooldown and the outcome
model exist to **select down** from an abundant candidate pool, not to scrape for opportunities. That is
the comfortable direction to be wrong in, and it is the opposite of C1.

**Two predictions were refuted, and one of them is a finding in its own right.**

**(a) The presser exclusion is not the bound.** §2.4 reasoned — and the evidence advisor agreed — that
FR-DA-010's exclusion of `PrimaryPress`/`CoverShadow` would starve the tackle producer, since the presser
is the man sent at the ball. Measured, `poolElig` and the 3 m population are the **same number to within
one episode in six team-matches** (310.0 vs 310.2), and the presser column is **0.5**. The exclusion
costs W2 essentially nothing.

But the reason it costs nothing is itself alarming: **a #13 press-role holder is almost never within
3 m of the man on the ball** — 0.5 episodes out of ~682, on a subsystem whose entire purpose is to send
someone at the carrier. That is not a W2 defect and W2 must not fix it, but it is a **gate-level
dormancy signal for #13** of exactly the class the backlog's §1.1 books as **W12**, found here by
accident, and it is filed to the backlog rather than left in this note.

**(b) `MarkAssigner`'s ball-blindness costs less than §2.4 implied.** It does cost: of the ~310 episodes
with a pool-eligible defender inside 3 m, only ~97 (31%) produce any intent naming the carrier, because
the assignment is not made on possession. But ~97 is still ample, and ~65 of those reach `COMMIT`. So
giving the assignment a ball model is a **fidelity improvement, not a precondition** — and landing it
inside W2 would be scope creep on a producer that already supplies four times what is needed.

**One number changes a design decision.** `ballGap` is 81.7 episodes (12%): in one defending episode in
eight the "carrier" is at some point more than a metre from the ball he is recorded as holding. So
tackler-to-carrier and tackler-to-ball genuinely diverge, and the contact gate must name which one it
means. Given that a tackle is a challenge **for the ball**, the gate should measure to the ball, and
this instrument's bands — which measure to the carrier — must be re-read against that before any
constant is set from them.

> Raw output: `scratchpad/tackle-census2.log`. Command:
> `TD_TACKLE_DIAGNOSTIC=1 dotnet test --filter TackleIntentDiagnostic --logger "console;verbosity=detailed"`.
> Note the `--logger` — at default verbosity the instrument runs, passes, and prints nothing.

### 3.1 The pre-change baseline, measured — and one tracking correction it forces

Whole-tree gate at `4b9271c`, before anything in this landing: **build 0 errors / 0 warnings, quarantine
empty, every suite green except `MatchEngine.Tests` at 451 passed / 1 failed / 10 skipped (49 m 59 s).**

The single failure is `sim_match_engine_close_chance`, on exactly the two predicates the August 11 owner
call covers (`meanCosine = −0.165` against a −0.16 bound; `goalwardShare = 0.407` against 0.42) — held
red deliberately, not to be rebaselined a third time.

**The correction: `sim_match_engine_shot_outcomes` PASSES.** Root `CLAUDE.md`'s OPEN ISSUES entry and
`match-engine-wiring-backlog.md` v1.6 both record its `fast-balls-deflect-off-bodies` reachability
predicate as still open, with the branch "red by design" on **two** predicates. On this tree it is red on
**one**. The test exists (`MatchEngineShotOutcomeTests.cs:20`), the quarantine is empty, and it appears in
neither the failed nor the skipped list. Several main-line commits have landed since C1 drove it to zero —
`ERR-010-002` changed header aim, and therefore ball trajectories, which is the plausible route — but this
run establishes only the *outcome*, not the cause, and the entry is corrected to that and no further.

This matters for W2 beyond bookkeeping: a landing that adds a possession-loss mechanism has to be able to
tell its own regressions from inherited red, and "the branch is red on two predicates" would have hidden
one.

**W1's owed measurement is discharged in the same session** — `TD_GK_DIAGNOSTIC=1 dotnet test -c Release
--filter GkRushDiagnostic` (`gk-rush-trigger-design.md` §6). W1 changes the near-goal contact geometry
any tackle model would later be tuned against, and its instrument has never executed.

---

## 3.2 WHAT THE LANDING MEASURED, and the two things it corrected in this note

§3.0's conclusion — *"W2 is a RESOLUTION problem, the gate supplies ~4x what is needed"* — was measured
**to the carrier**. The mechanism reaches for the **ball**. Measured to the ball, the population
collapses by about an order of magnitude, and §3.0's own `ballGap` row is why:

| Measured at the gate (40 000 ticks, both seeds) | |
|---|---|
| Strides where a COMMIT intent named the carrier and the tackler was eligible | **10** and **1** (per seed) |
| …of those, strides where anyone was within the first 1.5 m contact radius | **1** and **1** |
| Mean nearest-eligible-challenger distance **to the ball** | **2.20 m** and 0.60 m |

So §3.0's headline stands as a statement about *intent supply* and is **wrong as a statement about
reachable challenges**. Corrected here rather than quietly restated: the intent is plentiful, the
geometry is not.

**The obvious widening was tried and rejected on measurement.** Dropping the "his mark target is the
carrier" gate — acting on any COMMIT intent from a player near the ball — raises the eligible
population about 14×, and the mean nearest-challenger-to-ball distance goes from 2.2 m to **21–31 m**,
because those extra intents belong to defenders marking someone else at the far end of the pitch. It
admits noise, not tackles. The rejection is recorded at the gate in `TryResolveTackle` so it is not
re-tried by the next reader.

**The contact radius went 1.5 m → 2.5 m, and not to make the count come out.** Fitting a constant to
make a measurement look right is calibration, which KD-W1 forbids at a wiring pass. It is re-derived
from what the mode *means*: #14 §3.6.1 defines COMMIT as a **lunge**, and a lunge is the extended-leg
case #3 §7.2.1 itself describes for the Stage-2 slide tackle (*"compound hitbox (body + leg)"*,
`ExtendedLegCapsule`). A standing challenge reaches about a metre; a lunging one reaches a body-length
further. The measurement prompted re-deriving the number; it did not supply it.

**Resulting rate, recorded and NOT tuned:** on the livelier seed, ~10 resolved challenges per 40 000
ticks of which ~2 are decisive. Extrapolated that is order-of-magnitude ~40 challenges and ~8
dispossessions per team per 90, against football's ~15–17 tackle attempts and ~9–10 won. The
dispossession figure is close; the attempt figure is high because most challenges miss. **Both are
un-calibrated by design** — they are the calibration pass's input.

---

## 3.3 What the wiring surfaced

1. **FM-08 was logging an ordinary event as an error.** `PassExecutor`'s CONTACT-time possession
   re-check logged `LogError` with the text *"Race condition."* — accurate while an ordering accident
   between systems was the only way to lose the ball mid-windup, which is what FM-08 was written to
   catch. A tackle makes it an ordinary football event. Left alone it would put a red line in the log
   for every successful tackle on a passer, burying real errors — and it is how the defect was found,
   because the Unity shim's `LogAssert` fails a test on an unexpected `LogError`. Now a `LogWarning`
   with corrected wording.
2. **`ContactType.SLIDE_TACKLE` gains its first producer.** Defined in #3 since the collision system
   was written and produced by nothing; every foul this engine has ever given was published as
   `FROM_BEHIND` regardless of cause. `FoulCommittedEvent.FoulKind` is meaningful for the first time.
3. **`Tackling` gains its first consumer anywhere in the tree.** It and `Marking` are canonical #27
   attributes — loaded, defaulted, serialized, and read by no formula. `Marking` still has none.
4. **`DOMAIN_TAG_DEFENSIVE_AI` (0x1A) gains its first draw site**, which un-blocks #14's own
   T-DA-DET-005 — `Assert.Ignore`d since May with the message *"activate when DOMAIN_TAG_DEFENSIVE_AI
   RNG draws are live"*. Wiring it was not in this pass's scope; the test is now unblocked, not
   un-ignored.
5. **The FR-CS-057 recurrence happened in the landing that cites it.** Five new files shipped without
   a `// Modified:` header field and were caught by `tools/recurring-defect-lint.py` before the commit.
   Sixth consecutive occurrence of this class; the lint is what stopped it this time.

---

## 4. The decision that was not this note's to take — ANSWERED

> **RESOLVED by owner decision, August 12, 2026: back-propagate into #14**, and add a fourth outcome.
> Landed as **#14 §3.6.5 Tackle Outcome Resolution** under `ERR-014-006`, with KD-6 revised. The
> `BALL_LOOSE` outcome is the owner's addition and it is the one that makes the model football rather
> than bookkeeping — see §3.6.5.2. The reasoning below is preserved as the state of the question when
> it was asked.

**Who owns the tackle outcome model?** Not a rhetorical question — today nobody does:

- **#14** produces intent and stops there by design. KD-6: *"#14 produces `TackleIntentRequest` intent;
  #8 mediates dispatch; #3 owns contact physics."*
- **#8** structurally cannot mediate. `ActionType.SAVE = 7` is the last ordinal that fits the 3-bit
  composure-noise field in `ActionSelector.ComputeOptionNoise`; an eighth forces a digest rebaseline,
  which is exactly why the DT-emitted HEADER (W9) is deferred. #8 also has no tackle model.
- **#3** defers slide-tackle collision to **Stage 2** (§7.2.1) and its KR-3 states that ball-first-vs-
  player-first is undecidable with 60 Hz discrete detection. It also never landed the
  `TackleContactFlag` / `GetAndClearTackleFlag` amendment that **#5 §4.4.2 flagged as XC-4.4-02,
  "✅ Blocking — requires Collision System amendment"** — and #5 was APPROVED anyway, with no ERR ever
  filed against the open obligation.

That is `ERR-011-010`'s shape — a condition delegated to a delegate that cannot accept it — except that
here the obligation was *recorded as blocking* and signed off around.

**W1's precedent cuts against the easy answer.** W1 put the *trigger geometry* in the engine but took
the *decision magnitude* back into **#11 §3.7.0**, for the stated reason that *"how far he comes out is
a property of the keeper"*. Whether a defender wins a challenge is, by the same sentence, a property of
the defender and the carrier — so the outcome formula wants a spec owner, and the composition root is
not obviously it.

The counter-argument is the live foul model: `match-engine-design.md` §5.Z.9 owns fouls and cards with
no numbered spec, and #44 explicitly disclaims in-match card mechanics. So there is precedent for the
composition root owning a match-flow judgment.

**This is recorded as an owner decision, not defaulted into.** Defaulting is precisely how
`ERR-011-010`'s condition sat ownerless for ten weeks. Whatever is chosen must also be routed through
`football-judgment-proxy-review.md` §6 **P3, the attribute ownership ledger**, which binds every fix in
this class.

---

## 5. Constraints any resolution must respect (settled, whatever §4 decides)

1. **Not a DT action.** `ActionType` ordinal 8 overflows the 3-bit composure-noise field. The route is
   W1's: a pure predicate plus a composition-root commit.
2. **No per-tackle counter in the draw key.** Key from `(matchSeed, tick, tacklerId, targetId,
   domainTag)` only — a counter is new cross-tick state and reintroduces the resolution-order dependence
   keying exists to remove (ERR-030-012 / #30 §3.4.1).
3. **Draw order is NOT preserved if a tackle can foul.** Routing a tackle foul through
   `ApplyFoulIfCaptured` consumes a `match-flow.card-severity` reservation (`MatchEngine.cs:4592`), so
   the draw order on that stream changes and the digest moves. That must be stated as intended, not
   discovered. **Do not claim digest invariance anywhere in this landing** — the ERR-008-019 retraction
   is what that claim costs when it is wrong.
4. **One foul authority.** A tackle foul enters as a *candidate* into the existing single slot under
   KD-F4 strongest-wins; it does not get a second application site, a second cooldown, or a second
   sent-off gate (`MatchEngine.cs:4489` already has one). Two authorities on one concept is the
   parallel-surface trap filed four times in this tree.
5. **A tackle must be able to foul.** An outcome model of {won, missed} with no {foul} is a continuous
   football judgment collapsed out of existence — a **P1 cliff** under `football-judgment-proxy-review.md`
   §6, the class ERR-008-019/-021/-022 exist to fix — and a free tackle has no downside, so defenders
   would lunge unconditionally.
6. **Foul rates cannot be summed.** More fouls ⇒ more restarts ⇒ less played time ⇒ fewer collision
   contacts ⇒ fewer collision fouls. The `foul-discipline-balance-design.md` §6 offline sweep measured
   0.025 → 37.5 live for exactly this reason. Measure the total; never add the two sources.
7. **The discipline scenario bands will not catch a regression.** `MatchEngineDisciplineScenarios.cs:74-83`
   is `MinFouls = 3`, `MaxFouls = 90`, `MaxYellows = 20`, `MaxReds = 5` — abandonment guards, not a lock
   on the fitted 21.0. A tackle source that doubles the foul rate passes the gate green. Any foul claim
   must come from `TD_FOUL_DIAGNOSTIC` with the tackle source split out.
8. **`HeadingMechanics.CancelIntent` becomes load-bearing** — the backlog's own Class-C table predicts
   it (*"a tackle should cancel a header"*) and there is no interrupt path. Cover it or record explicitly
   that it is not covered.
9. **`SNAPSHOT_SCHEMA_VERSION` 20 → 21** for any new cross-tick latch, with the v21 doc row and the
   exclusion proof in `SerializeWorldState`.
10. **A local `Mix` is the accepted norm, not a parallel-surface defect — but it would be the fifth
    copy.** The council flagged a private SplitMix64 in `match-engine` as the parallel-surface class,
    citing `src/injuries-medical/MedicalStep.cs:530`. **Checked, and that is not what the precedent
    says.** `MedicalStep`'s own remark records the opposite: *"A local copy, matching this project's
    accepted norm for keyed derivations across assemblies (`RoundResolutionModel`, `LeagueBootstrap`,
    `PlayerGenerationRng`): there is no shared helper on `deterministic-sim` to call. The constants are
    SplitMix64's, so a future shared helper is a drop-in replacement rather than a behaviour change."*
    So four copies already exist by design and a fifth is sanctioned. The real observation is that
    **four sanctioned copies is the point at which the shared helper stops being hypothetical**; that is
    a `deterministic-sim` item, recorded here and deliberately not bundled into a wiring landing.

---

### 5.1 Two council unknowns, resolved by grep

- **`DOMAIN_TAG_DEFENSIVE_AI = 0x1A` has no draw site anywhere in `src/`.** The tag is allocated
  (`DeterministicSimConstants.cs:105`) and already `[CROSS]`-mirrored into #14's own catalogue
  (`DefensiveAIConstants.cs:54` `DomainTagDefensiveAI`), and #14's own determinism test T-DA-DET-005 is
  `Assert.Ignore`d pending exactly this (*"activate when `DOMAIN_TAG_DEFENSIVE_AI` (0x1A) RNG draws are
  live"*). So a tackle draw would be its **first** draw site — legitimate, requiring no new allocation,
  and it un-ignores a test that has been waiting for it. Per the ERR-041-002 posture, do **not** also
  allocate a subsystem ordinal.
- **`TackleIntentRequest` is not in the per-tick digest, and #14 says it must be.** Before this
  landing the type had **zero** references anywhere in `src/match-engine/` or `src/deterministic-sim/`,
  while #14 §4.6 / XC-014-020 / XC-014-024 declare it digest-load-bearing. That is a pre-existing
  spec-vs-code divergence which W2 makes material rather than creates, and it belongs in the
  `ERR-014-006` filing alongside the KD-6 delegation.

---

## 6. ERR ids — verified free August 12, 2026, re-verify at filing

| Id | Target |
|---|---|
| `ERR-014-006` | #14 KD-6's unownable delegation to #8, and §3.6's tackle intent having no ball model while `HasBall` is supplied and unread |
| `ERR-003-008` | #3's never-landed `TackleContactFlag` / `GetAndClearTackleFlag` (XC-4.4-02) |
| `ERR-005-002` | #5 approved with an open ⚠ REQUIRED blocking cross-spec dependency |

Note that only `ERR-014-001` appears in `spec-error-log.md`; `-002`…`-005` live in #14's spec text
only. A proposed id is never a reservation — the July 27 wave consumed three.

---

## 7. Recorded, NOT fixed

1. **`TackleIntentRequest.ApproachAngle`'s XML doc is wrong** (§2.1) — fixed in this landing, listed
   here because it is a defect the spec did not have and the code invented.
2. **`DefensiveAgentSnapshot.HasBall` is written and never read** — a new C5-class item for the backlog.
3. **`PerceptionEvents.TackleCompletion` has no producer and no consumer** anywhere in the tree.
4. **`ContactType.SLIDE_TACKLE` is never produced** — `ApplyFoulIfCaptured` hardcodes every foul as
   `FROM_BEHIND`.
5. **`GroundedReason.SLIDING_TACKLE` and `SlidingTackleDwellMult`** are a state and a `[GT]` for a state
   nothing ever enters.
6. **`.claude/advisors/invariants.md` §5 has no row for tackles, interceptions, or turnovers.** There is
   no repo-cited football reference for any of them. Missing is not permission; the row is owed.
7. **Turnovers cannot currently be split by cause.** `PossessionChangedEvent` carries a `Reason` byte
   and `MatchEngine` publishes every change with the Stage-0 `UNSPECIFIED` sentinel, so "what fraction of
   turnovers are tackles" — the number this landing most wants to be judged on — is not answerable by any
   existing instrument. Populating `Reason` is small, additive, and squarely in W2's path; whether it
   lands here or is deferred is a scope call, but it must not be left implicit, because without it the
   landing's central claim is unmeasurable.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.2 | 2026-08-12 | — | **WIRED.** The owner answered §4: the outcome model back-propagates into **#14 §3.6.5** (`ERR-014-006`, KD-6 revised), with a **fourth outcome the owner added — `BALL_LOOSE`**, because won-or-missed cannot express the commonest result of getting a foot in and folding it into a clean win makes every successful challenge a turnover. **§3.2 records the two things the landing corrected in this note:** §3.0's "the gate supplies ~4x what is needed" was measured to the CARRIER, and measured to the BALL — which is what a tackle reaches for — the reachable population collapses ~10x (mean nearest challenger 2.20 m); and the obvious widening (act on any COMMIT intent from a player near the ball) was tried and REJECTED on measurement, since it raises the population 14x while moving the mean challenger distance to 21–31 m, admitting noise rather than tackles. The contact radius went 1.5 → 2.5 m re-derived from COMMIT meaning a *lunge* (#3 §7.2.1's own extended-leg case), explicitly NOT fitted to make the count come out. **§3.3 records five things the wiring surfaced**, including FM-08 logging an now-ordinary event as an error with the text "Race condition" (found because an unexpected LogError fails a suite), `ContactType.SLIDE_TACKLE` and the canonical `Tackling` attribute both gaining their first producer/consumer anywhere in the tree, and the FR-CS-057 recurrence happening in the landing that cites it. `SNAPSHOT_SCHEMA_VERSION` 20 → 21; card-severity draw order moves by design; **no digest invariance claimed**. |
| 1.1 | 2026-08-12 | — | **The census RAN and answered §3's decision table: W2 is a RESOLUTION problem** — 65.3 COMMIT-on-carrier episodes per defending team per match against football's ~15–17 tackle attempts, so the gate supplies ~4× what is needed and the contact radius / cooldown / outcome model exist to select DOWN. **Two of this note's own predictions refuted:** the FR-DA-010 presser exclusion is not the bound (`poolElig` 310.0 vs the 3 m population's 310.2; presser 0.5) — but a #13 press-role holder being almost never within 3 m of the carrier is a gate-level dormancy signal for #13, filed to the backlog; and `MarkAssigner`'s ball-blindness is a fidelity gap, not a precondition (31% of eligible episodes still yield an intent). `ballGap` 12% forces the contact gate to name whether it measures to the carrier or the BALL. Adds §3.1 (the pre-change baseline, and the measured correction that `sim_match_engine_shot_outcomes` now PASSES — the branch is red on one predicate, not two, and "red on two" would have masked a W2 regression), §5.1 (two council unknowns closed by grep: `DOMAIN_TAG_DEFENSIVE_AI` has no draw site anywhere so a tackle draw is its first and un-ignores #14's T-DA-DET-005; `TackleIntentRequest` is absent from the digest while #14 §4.6/XC-014-020/-024 declare it load-bearing), and §7 item 7 (turnovers cannot be split by cause — `PossessionChangedEvent.Reason` is always the UNSPECIFIED sentinel, so the landing's central claim is currently unmeasurable). One council claim corrected: a local SplitMix64 `Mix` is this project's documented norm, not a parallel-surface defect. |
| 1.0 | 2026-08-12 | — | Initial. Wiring backlog W2. Records the fourth dead link and the finding that outranks it (no path anywhere dispossesses a controlled carrier); the four pre-implementation council corrections (`ApproachAngle` does not encode from-behind and its XML doc is wrong; `Commit` means "I have cover", not "I will tackle"; the flag is redundant on a won tackle, its real coverage being disrupt-without-winning; the primary presser is structurally excluded from producing tackle intent, and `MarkAssigner` never reads the ball); the census that decides the shape, with a decision table mapping each possible zero to its cause; the ten settled constraints on any resolution; and the ownership question, recorded as an owner decision rather than defaulted into the composition root. Measurement NOT yet run. |
