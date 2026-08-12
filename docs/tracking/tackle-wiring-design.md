# The Tackle — wiring backlog W2

> **Created:** August 12, 2026
> **Status:** DESIGN SUPPLEMENT — the same governance class as `match-engine-design.md`. Opens no
> numbered spec and changes no `SPEC_INDEX.md` row.
> **Owner document:** `docs/tracking/match-engine-wiring-backlog.md` **W2** (this is that item).
> **Purpose:** Nothing in this engine has ever made a tackle, and a player in control of the ball
> cannot be dispossessed by any means at all. This note is the investigation, the defects the
> investigation surfaced, the measurement that decides the shape of the fix, and the one decision
> that is the owner's rather than this note's.

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

> **RESULT: NOT YET RUN.** No numbers are recorded here and none are invented. The command is
> `TD_TACKLE_DIAGNOSTIC=1 dotnet test -c Release --filter TackleIntentDiagnostic`.

**W1's owed measurement is discharged in the same session** — `TD_GK_DIAGNOSTIC=1 dotnet test -c Release
--filter GkRushDiagnostic` (`gk-rush-trigger-design.md` §6). W1 changes the near-goal contact geometry
any tackle model would later be tuned against, and its instrument has never executed.

---

## 4. The decision that is not this note's to take

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

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-08-12 | — | Initial. Wiring backlog W2. Records the fourth dead link and the finding that outranks it (no path anywhere dispossesses a controlled carrier); the four pre-implementation council corrections (`ApproachAngle` does not encode from-behind and its XML doc is wrong; `Commit` means "I have cover", not "I will tackle"; the flag is redundant on a won tackle, its real coverage being disrupt-without-winning; the primary presser is structurally excluded from producing tackle intent, and `MarkAssigner` never reads the ball); the census that decides the shape, with a decision table mapping each possible zero to its cause; the ten settled constraints on any resolution; and the ownership question, recorded as an owner decision rather than defaulted into the composition root. Measurement NOT yet run. |
