# Match Engine — Wiring Backlog

> **Created:** August 4, 2026
> **Status:** AUDIT — a finding list, not a design. No spec is opened here and no `[GT]` is proposed.
> **Owning doc:** `match-engine-design.md` (the composition root this audit measures).
> **Purpose:** Enumerate every subsystem surface that is built, tested, and reachable from the
> match engine's assembly graph but has **no production caller** — the code that exists and never
> runs. Produced because four such surfaces were found by accident while answering questions about
> goalkeeper behaviour, and nobody had ever gone looking.

---

## 0. Why this document exists, and the rule it establishes

Seven consecutive `§5.Z` match-realism passes fitted `[GT]` constants against the composed engine.
Every one of those fits was made against a machine with dormant subsystems in it. That is not
merely a calibration inefficiency — it is a **diagnostic** hazard:

> The measured shot conversion is ~18% against football's ~11%. That reads as "the shot model is
> too generous." At least part of it is "no keeper has ever narrowed an angle, and no defender has
> ever made a tackle." A realism pass aimed at the shot model would have chased the wrong lever and
> left behind a `[GT]` that later has to be un-tuned.

**KD-W1 — the `[GT]` freeze.** Do not land a `[GT]` change governing a subsystem that is not fully
wired. Defect fixes, instruments, and measurement are unaffected and should continue freely;
constants wait for the calibration pass that follows this backlog.

**KD-W2 — scope.** This audit covers the match engine and the assemblies it composes only. The 22
approved specs with no `src/` assembly are a different problem, tracked in
`path-to-playable-roadmap.md`, and are explicitly out of scope here.

---

## 1. Method

Three passes, each of which found things the others missed:

1. **Comment sweep** — grep for self-declared deferrals (`intentionally not called`, `zero
   production call sites`, `Stage 1 deliverable`, `not plumbed`). High precision, low recall: it
   only finds gaps someone knew about and wrote down.
2. **Call-graph sweep** — for every `public` method on every type in the 18 assemblies the engine
   references, count production (non-test) callers across the whole tree. Zero callers ⇒ candidate.
   This is the pass that found the tackle gap, which no comment records.
3. **Manual triage** — every candidate read in source to separate a genuine dormant capability from
   an internal helper, a redundant setter, or a test seam.

Scripts: `scratchpad/audit*.py` (not committed — the finding list below is the deliverable).

### 1.1 What this method CANNOT see

The sweep detects **method-level** dormancy: nothing calls X. It is blind to the more common and
more expensive failure — **gate-level** dormancy, where the call site exists and executes but its
condition is almost never true. Those surfaces look perfectly wired to a call-graph scan.

At least one is already known and measured (§3 below, C1: #12 commits `InPoss` on **9.5%** of
final-third samples, so every phase-gated mechanism in #13/#14/#15 is starved). That was found by
runtime instrumentation during §5.Z.24, not by any static analysis, and there is no reason to
believe it is the only one.

**Therefore this backlog is a floor, not a ceiling.** A second detection pass — an env-gated
instrument counting how often each phase gate and trigger condition actually fires over a match —
should run before the calibration pass, and belongs on this board as item **W12**.

---

## 2. Class A — dormant capability (no production caller)

Ordered by measured or expected impact on match realism. "Evidence" cites the declaration site; in
every case the whole-tree production caller count is zero.

### W1 — The goalkeeper never comes off his line — ✅ **WIRED August 4, 2026**
**Evidence (as filed):** `goalkeeper-mechanics/GoalkeeperMechanics.cs:281` `CommitRushIntent`.
The engine called `CommitSaveIntent` and only `CommitSaveIntent`.

**Landed:** `MatchEngine.TryCommitRushIntents` (fired from `DriveGkHeadingTactical`, *before* the
tactical tick — the rush is a 10 Hz state-machine input, unlike the 60 Hz-consumed header) over a
new pure `GkHeadingIntentSource.RushArmed`. **The keeper comes out to reduce the shooting angle**,
and the predicate is built from that sentence: the only thing that keeps him home is a team-mate
already **goal-side** of the ball inside the shot corridor — a defender merely *chasing* the carrier
narrows nothing and does not stop him — while **how far** he comes out is #11 §3.7.0's
attribute-driven `ComputeRushCommitDistanceM` (`OneVsOne` / `Composure` / fatigue), not an engine
range. For a loose ball the locked target is an **intercept race solve**, not the ball's current
position, because KD-15 locks the target at commit. Skipped whenever `SaveArmed` holds for the same
keeper: a ball driving at the goal is a save, not a rush, and without that exclusion a shot would
send the keeper charging out while the ERR-011-007 lead gate still held the dive. Deliberately
**not** a Decision Tree action — `ActionType.SAVE = 7` is the last ordinal that fits the 3-bit
composure-noise field, so an eighth would force the same digest rebaseline that defers W9. **No new
engine state** (#11's own serialized `_rushIntentActive` is the latch, read through new `GetState` /
`HasActiveRushIntent` accessors), so no schema bump.

**What it surfaced: two spec defects.** **`ERR-011-010`** — §3.7 delegated the rush DECISION to
Decision Tree #8, which has no goalkeeper model and structurally cannot acquire one, so the condition
belonged to nobody and the method sat uncalled for ten weeks; and because the "when" was delegated,
the spec never said what a keeper is *deciding* either — **the first cut of this trigger guessed
wrong**, using a last-man test that keeps the keeper home in exactly the situation he exists for.
New §3.7.0 takes the decision back and states the model. **`ERR-011-009`** — a rush that REACHED its
target had no exit. Both fixed spec-and-code in the same commit; see §3 of the owner doc.

**Measurement NOT run.** No .NET SDK in the authoring environment (the agent proxy denies the
installer), so neither the gate nor the new `GkRushDiagnosticTests` instrument executed. Owner doc
`docs/tracking/gk-rush-trigger-design.md` §6 carries the command and the honest status.

**Owner doc:** `docs/tracking/gk-rush-trigger-design.md`.

Everything downstream of the trigger is built and works: `GoalkeeperRushDispatch.UpdateRushFrame`
genuinely advances the keeper toward a locked target and writes the position back to the movement
array; the `Rushing → OneOnOne → Smothered` transitions exist with abort reasons, a 1v1 trigger
radius, a smother radius, and telemetry. The `RushIntent` is even serialized into the snapshot.
Only the trigger condition is missing.

**Consequence (as filed):** every one-on-one in the game was a stationary keeper on his line waiting
to dive. This was the single most likely contributor to the conversion gap, and the cheapest to
close. Whether closing it moved the conversion gap is **unmeasured** — see above.

### W2 — No player has ever made a tackle — ⚙️ **BUILT August 12, 2026; SHIPS DISABLED pending W6**
**Evidence:** three independent dormant links in one chain — **four**, on re-verification.
- `defensive-ai/DefensiveAITick.cs:358` — `GetTackleIntentRequests` is populated every tick and read
  by nobody. The class doc says so outright: *"all output surfaces are populated at Stage 0 but
  integration with the match orchestrator and Decision Tree #8 occurs at Stage 1 (KD-16)."*
- `match-engine/MatchEngine.cs:7248` and `:7321` — **both** collision-query adapters hardcode
  `public bool GetAndClearTackleFlag(int agentId) => false;`. **(The `:6721` / `:6789` cited in v1.0
  were already stale when written and are now GK snapshot deserialization.)** The two adapters are the
  PASS and the SHOT adapter, so `shot-mechanics/ShotExecutor.cs:414`'s interrupt is equally dead — this
  entry named only the pass side.
- Consequently `pass-mechanics/PassExecutor.cs:408`'s §3.8.5 tackle-interrupt branch, and the
  `CancelReason.TackleInterrupt` outcome it raises, are **unreachable code**.
- **Fourth link, not previously recorded:** `PassCancelledEvent` / `ShotCancelledEvent` have **no
  production subscriber**, so even a working flag produces no possession consequence.

**Consequence, restated and sharpened:** `RunFirstTouch` gate 1 refuses any possessed ball and nothing
else writes `_possessingAgentId` away from a controlled carrier, so the engine has exactly **two**
turnover mechanisms — the carrier kicks it and a team-mate fails to receive, and a foul restart.
**A player in control cannot be dispossessed, at all, under any pressure.** That is a missing football
mechanism rather than a churn imbalance, and it is the real content of W2.

**⚙️ BUILT AND SHIPPING DISABLED — owner decision, August 12, 2026.** The mechanism is complete and locked; `TackleContactRadiusM` ships at **0**, so no challenge resolves. **Why:** with the challenge live, `sim_match_engine_inposs_gate` collapsed to 0.501 against its 0.70 bound — it passed at baseline. Measured: tackles OFF gives 0.975/0.966 on the two scenario seeds; tackles ON collapses ONE of them, and which one moves with the contact radius. The collapsing run had **three** decisive tackles, so it is a **stall**, not a rate effect. Root cause NOT isolated; the leading candidate is **W6** — possession is a flag and the ball is unattached, so the two reclaim paths are mutually exclusive and a ball between them is reclaimed by nobody, which a tackle is the first mechanic to deliberately create. **Disabled rather than held red** because this predicate is the only detector of the 0.24-class collapse and W4/W12 land on this branch: held red on an un-isolated cause it could no longer catch a NEW regression of the same class (ERR-030-014 one layer up). Recorded as overridden: KD-7a's tripwire waits on the tackle being WIRED and radius-0 is behaviourally unwired, so the next dispersion capture stays pre-tackle. **Arming is one constant** once the wedge is closed, at `LooseBallPickupRadiusM` so a knocked-loose ball is always reachable by the challenge that produced it. Everything downstream is live and locked BOTH ways (#41 FR-MD-027 posture). Full attribution in `tackle-wiring-design.md` §3.4/§3.4.1.

**W2 NOW GATES THE FOUL/CARD CALIBRATION — owner sequencing decision, August 15, 2026.** The open foul/card entry (`open-issues.md`) measured **fouls 35.0 / yellows 5.0 / reds 1.00 per 90** on August 13 against football's ~22 / ~3.5 / ~0.25 — fouls and yellows both ~67% above their own July-26 post-balance-pass figures, drifted by C1's phase reclassification. The decision is **arm W2 first, then calibrate once**, rather than fitting `FoulCallProbability` to today's numbers. The reason is this item: today's foul population is **pre-tackle**, and arming the challenge routes ~47 challenges per team per 90 into the same single foul-candidate slot, so a fit landed now would be re-fitted immediately while its intermediate value sat in the tree looking calibrated. That is KD-W1 read literally — do not land a `[GT]` governing an unwired subsystem — and the July-26 pass is the counter-example that makes it concrete: it fitted correctly against the contact stream of its day, C1 moved that stream, and nobody re-measured for four months. **What this makes W2, in sequencing terms:** it is no longer only the next wiring item, it is the precondition for the most load-bearing open realism item in the project, since #44 now turns cards into suspensions and a 4× red rate is a 4× suspension rate. The `sim_match_engine_inposs_gate` wedge above (leading candidate: W6) is therefore in the way of a calibration pass as well as of the tackle itself.

**Gate history for this item.** The whole-tree gate reads `MatchEngine.Tests` 459/3/11. Beyond the inherited `sim_match_engine_close_chance`, **`sim_match_engine_inposs_gate` regressed to 0.501 against its 0.70 bound** — it passed at the pre-change baseline. Measured attribution: with tackles OFF both scenario seeds read 0.975/0.966; with tackles ON one seed collapses, and WHICH seed collapses moves with the contact radius. The collapsing run had **three** decisive tackles, so it is a **stall**, not a rate effect. The on-ball share and the pass-in-flight latch share fall together and the ball is traced loose-live-and-never-possessed for 400 ticks after the challenge — the leading candidate is **W6** (possession is a flag, the ball is not attached) surfacing under the first mechanic that deliberately creates a contested loose ball. Two precedents fit and the choice is the owner's: hold red (the `close_chance` precedent) or ship the challenge disabled at `TackleContactRadiusM = 0` (the FR-MD-027 precedent, measured to restore 0.975/0.966). **Widening the 0.70 bound is explicitly not among them.** Full attribution table in `tackle-wiring-design.md` §3.4.

**MEASURED (August 12, 2026)** — `TackleIntentDiagnosticTests` (`TD_TACKLE_DIAGNOSTIC=1`), 3 seeds ×
90 min, both defending teams separately, counted in possession episodes. Per defending team per match:
**681.7 defending episodes, 310.2 with an outfielder within 3 m of the carrier, 178.8 within 1.5 m,
97.2 with an intent naming the carrier, and 65.3 with a COMMIT naming the carrier** — against football's
~15–17 tackle attempts per team per 90. **The gate supplies ~4× what is needed, so W2 is a RESOLUTION
problem: not a producer problem, and not dead upstream.** This is the C1 question asked *before* the
wiring instead of after it, which is what §1.1 books as W12; the instrument and the raw output are
committed (`docs/tracking/corpus-data/w2-tackle-intent-census-2026-08-12.txt`).

**Two predictions the investigation made were refuted by the measurement** — recorded because both were
plausible enough to have shaped the design: the FR-DA-010 presser exclusion is **not** the bound
(`poolElig` 310.0 against the 3 m population's 310.2, presser 0.5), and `MarkAssigner`'s ball-blindness
is a fidelity gap rather than a precondition (31% of eligible episodes still yield an intent).

**What had been open was the wiring itself**, and it was gated on one governance decision rather than on
any engineering: **the tackle outcome model had no owner.** #14 stops at intent by design (KD-6:
*"#8 mediates dispatch; #3 owns contact physics"*); #8 structurally cannot mediate (`ActionType.SAVE = 7`
is the last ordinal that fits the 3-bit composure-noise field — the W9 ceiling); #3 defers slide-tackle
collision to **Stage 2** (§7.2.1) and never landed the `TackleContactFlag` amendment that **#5 §4.4.2
flagged as XC-4.4-02, "Blocking — requires Collision System amendment"**, which #5 was APPROVED around
with no ERR filed. Full analysis and the ten settled constraints on any resolution are in
**`docs/tracking/tackle-wiring-design.md`**.

**LANDED August 12, 2026** (`fc8f81f2`) — new **#14 §3.6.5 "Tackle Outcome Resolution"** takes the
decision back into the spec that owns the players, on the W1 precedent (#11 §3.7.0 took the keeper's
rush-commit distance back for the stated reason *"how far he comes out is a property of the keeper"*;
whether a defender wins a challenge is, by the same sentence, a property of the defender and the
carrier). `ERR-014-006` filed and resolved the same commit. A Stage-0 tackle is **not** a physics contact
(#3 defers slide-tackle collision to Stage 2) and **not** a Decision Tree action (`ActionType` ordinal 8
overflows the 3-bit composure-noise field, the W9 ceiling) — it is an abstract attribute duel between the
two players, producing one of **four** outcomes: `MISSED` / `BALL_WON` / **`BALL_LOOSE`** / `FOUL`.
`BALL_LOOSE` is an owner decision: a won/missed-only model has no way to express the commonest result of
a challenge — the ball going somewhere neither player controls — and folding that into `BALL_WON` would
make every successful tackle a clean turnover.

New `src/defensive-ai/TackleOutcome.cs`, `TackleDuelInputs.cs`, `TackleOutcomeResolver.cs` (ten new `[GT]`
+ one `[FIXED]` numerical ceiling, all **un-calibrated per KD-W1**), plus `src/defensive-ai/Tests/TackleOutcomeResolverTests.cs`
(12 pure resolver locks), `src/match-engine/tests/MatchEngineTackleTests.cs` (7 composed engine locks) and
`src/match-engine/tests/TackleIntentDiagnosticTests.cs` (the census instrument, now a modified file rather
than a new one). `MatchEngine.cs` wires the resolver at the COMMIT contact gate — the contact radius went
**1.5 → 2.5 m**, re-derived from what COMMIT means (#14 §3.6.1: a *lunge*, the extended-leg case #3 §7.2.1
describes for its own Stage-2 slide tackle), **not fitted to a target** — publishes `ContactType.SLIDE_TACKLE`
for the first time, and routes a tackle foul through the **existing single foul-candidate slot** under
KD-F4 strongest-wins (`ApplyFoulIfCaptured` does not re-judge it; #14 §3.6.5 already priced the challenge).
`SNAPSHOT_SCHEMA_VERSION` **20 → 21** (per-agent tackle flag + challenge cooldown; the four outcome
counters are excluded, proof at the write site). `DOMAIN_TAG_DEFENSIVE_AI` (0x1A) gets its **first draw
site anywhere in `src/`** — the tackle draw is keyed, not reserved, un-blocking #14's own T-DA-DET-005 —
and the `match-flow.card-severity` draw order **MOVES by design** (the foul branch now draws on ticks that
previously had none). **No digest invariance is claimed anywhere in this landing.** `Tackling` gains its
first consumer anywhere in the tree; `Marking` still has none. Surfaced and fixed in the same commit:
FM-08's CONTACT-time possession-loss log was `LogError`("Race condition") — accurate only while an
ordering accident was the sole way to lose the ball mid-windup; a tackle makes it an ordinary football
event, so it is now `LogWarning` with corrected wording.

Tests: 12 pure resolver locks (partition, monotonicity, the P1 continuity requirement, the `[FIXED]`
ceiling's numerical guarantee, the §3.6.5.7 worked example) + 7 composed engine locks (asserting a
**ceiling** as well as a floor, since the failure mode of a new turnover source is too many rather than
too few) — all green at the landing commit. **GATE PASSED for W2 (August 12, 2026):** whole-tree build 0 errors / 0 warnings, quarantine empty, 32 suites; `MatchEngine.Tests` **461 passed / 1 failed / 11 skipped** (38 m 2 s). The single failure is `sim_match_engine_close_chance`, the inherited owner-held-red predicate that also fails at the pre-change baseline `4b9271c` — so the branch is at its baseline red state and W2 adds no new failure. Baseline was 451/1/10; the +10 passed are W2 locks and the +1 skipped is the env-gated census instrument. — a whole-tree gate was running at
the time this entry was written. ⟨PLACEHOLDER — operator to fill in: build errors/warnings, per-suite
pass/fail/skip counts (especially `DefensiveAI.Tests`, `MatchEngine.Tests`), quarantine state, and
PASS/FAIL verdict once the run completes.⟩

**Two Class-B items filed from the investigation stay open**, not fixed here per scope: **C9** (a #13
press-role holder is within 3 m of the ball carrier in 0.5 episodes of ~310 — the W12 class) and **C10**
(`DefensiveAgentSnapshot.HasBall` written every stride, read by nothing — the C5 class in a second
assembly). Full account: `docs/tracking/tackle-wiring-design.md`, #14 `docs/specs/defensive-ai/section-3.md`
§3.6.5, `docs/tracking/spec-error-log.md` `ERR-014-006`. **Next in sequence: W4** (keeper perception),
**then W12** (the gate-firing instrument).

### W3 — Keepers never claim crosses
**Evidence:** `goalkeeper-mechanics/GoalkeeperMechanics.cs:496–501` — the duel buffer is cleared
every frame, no participants are ever registered, and the source states
*"ResolveHandContactDuel is intentionally not called."*
`GoalkeeperCrossClaimDuel` and `CrossClaimDuelContext` have no reference outside their own assembly.

Blocked on the same missing multi-agent contact feed as the GK/Heading `CollisionConsumer`
AGENT_BALL duel fan-out already recorded in OPEN ISSUES — these are one dependency, not two.

### W4 — The keeper is never unsighted
**Evidence:** `match-engine/GkHeadingIntentSource.cs:27` `SaveArmed` is four lines of pure geometry
(ball loose, within range of the goal line, closing, above a minimum speed). Reaction latency is a
flat constant scaled by Reflexes.

A real, tested `OcclusionFilter` (shadow-cone test against other agents) is live in the perception
system for outfield players — `perception-system/PerceptionSystem.cs:396`,
`BallPerceptionEvaluator.cs:72`. The keeper is simply not on that path.

**Consequence:** traffic in front of goal costs the keeper nothing, and a deflection off a defender
does not restart his reaction window. Shots *do* deflect off bodies
(`CollisionSystem.ProcessAgentBall`) — the keeper just doesn't notice.

### W5 — The pressing AI's pass-event trigger never fires
**Evidence:** `pressing-ai/PassEventRing.cs` `Push` has no production caller anywhere.
`MatchEngine.cs:809` constructs one ring per team and hands it to `PressingAITick`
(`PressingAITick.cs:76`), which reads it via `TryGetLatest`. Nothing ever writes to it.

**Consequence:** the ring is permanently empty, so #13's BackwardPass press trigger is dead. A
press that should be sprung by a backward pass never is.

### W6 — `BallStateType.Controlled` has no producer
**Evidence:** `ball-physics/BallCollision.cs` — `CheckPossession` and `SetBallControlled` both have
zero production callers. The doc comment describes the intended protocol
(*"Caller must: record possession in agent system, call SetBallControlled(), drive position"*) and
no caller implements it.

Already recorded from the other direction in OPEN ISSUES §5.Z.23 item (c): a claimed ball is not
held at hand height and the keeper cannot carry it, because the parked ball settles under gravity.
Same root cause. Possession in the engine is a flag, never a kinematic constraint.

### W7 — The AI manager never picks a kickoff preset
**Evidence:** `match-engine/ManagerAdaptation.cs:250` `ApplyKickoff` has no caller. Its own doc says
*"Call BEFORE the first RunTick."* The mid-match half **is** wired
(`MatchEngine.cs:2510–2514` — `ManagerDecisionGate.DecisionDue` → `RunDecisionPoint`).

**Consequence:** #26's FR-TP-004 boot path is dead. An AI-managed team starts every match on the
human baseline tactic and can only ladder away from it mid-match.

### W8 — Goalkeeper distribution
**Evidence:** `goalkeeper-mechanics/GoalkeeperMechanics.cs:301` `CommitDistributeIntent`, no caller.
The engine substitutes its own six-second-rule release (`_gkHoldTicks` /
`_gkReleaseCooldownRemaining`), so #11's `GoalkeeperDistribution` model — delivery kind, target
selection — is unused.

Lower priority than W1–W7: unlike the others there **is** a working substitute, so this is a
fidelity gap rather than a missing behaviour.

### W9 — DT-emitted HEADER
Already recorded in OPEN ISSUES. Headers are triggered by an engine-side proximity heuristic, not
decided by the tree. Blocked on `ActionType` ordinal 8 overflowing the 3-bit composure-noise field,
which forces a rebaseline — a real cost, and the reason it has not been done.

### W10 — Attribute-modulated save commit
Already recorded in OPEN ISSUES. The save's *existence* is attribute-driven; its *quality at commit*
is not.

### W11 — `TargetIntent` was serialized state with no reader — ✅ **RESOLVED August 9, 2026 (`ERR-010-002`)**
**Evidence (as filed):** `HeaderIntent.TargetIntent` (`heading-mechanics/HeaderIntent.cs:36`) was
written by the only producer (`MatchEngine.TryCommitHeaderIntents`), clamped by
`HeadingMechanics.ClampToPitch` (`HeadingMechanics.cs:100`), serialized
(`MatchEngine.cs:6846–6848`), restored (`MatchEngine.cs:6898`) — and read by **no formula anywhere
in the tree**.

**The detection gap this exposes is NOT resolved.** This is the inverse of the phantom-interface
rule this project already tracks (root `CLAUDE.md`, "Interface Design Principle" — don't write
interfaces against unspecified systems): here the interface existed and was fully plumbed on the
write side, and nothing consumed it. §1's v1.0 audit method counts *methods with no caller*; C5
(§3 below) extended that to *fields with no reader*. Neither catches a field that is written,
clamped, serialized, restored **and never read** — every one of those five steps looks exactly like
production wiring to both passes, and `TargetIntent` passed every check this document runs, right up
until today. Recommend this class — "serialized field with no formula reader" — be added as an
explicit check when **W12** (the gate-firing instrument, §1.1/§5) is built; this one needs a static
sweep, not a runtime one.

**Resolved:** `ERR-010-002` (`spec-error-log.md`) gave `TargetIntent` its first reader — new #10
§3.5.1 + `HeadingAim.cs` (a ballistic launch solve) and the producer half
`GkHeadingIntentSource.HeaderAimTarget`. See the corrected **C7** entry below for what the live
symptom actually was — not the fixed aim point itself, which never reached a formula, but the pure
specular reflection that filled its place.

**Consequence (as filed):** for the entire life of the engine's heading integration, every header
was a passive mirror of the incoming ball — `TargetIntent` computed a real aim point and it changed
nothing downstream. Resolved as of `ERR-010-002`; the detection gap that let it ship unnoticed is
not.

---

## 3. Class B — wired but starved (gate-level dormancy)

Not found by this audit's method — carried here from measured evidence in `§5.Z.24` and
`close-chance-creation-design.md` because they belong on the same board and are, in effect, larger
dormancy than anything in Class A.

- **C1 — ✅ FIXED August 8, 2026 (`ERR-012-011`).** #12 committed `InPoss` on 9.5% of final-third
  samples (7.5% when re-measured at the fix, the corpus having moved under the -021/-022/-023
  shot-lane chain), because `PossessionOwnerEntityId >= 0` is false for the entire flight of every
  pass. Phase now classifies from TEAM possession — the on-ball carrier's team, else the intended
  receiver of a pass in flight — composed by the orchestrator over a new `_passInFlightReceiverId`
  latch (`SNAPSHOT_SCHEMA_VERSION` 19 → 20). No new `[GT]`: the latch expires by reusing
  `RunFirstTouch`'s own receding predicate.
  **This entry called C1 "probably the highest-value item in this document" and that claim was
  wrong — the pre-implementation council refuted it, and the correction outlives the fix.** The
  starvation is real, but the three consumers it was supposed to unblock cannot use what it
  delivers: `TacticalContext.HasAttackIntent` is written by the engine and **read by no production
  code anywhere**, so #15 is inert regardless of its gate; #13's `PressDirective.PrimaryTargetPosition`
  and `PressAssignment.TargetPosition` have **no consumer outside `pressing-ai`** (only the `Role`
  label reaches #14's hold-shape pool), so more-correct pressing still steers nobody; and the one
  large behavioural lever C1 does pull is #12's own `PullFactor` table, whose `InPoss` column is
  LESS advanced than `TransToAtk` for every attacking role (ST 0.60 vs 0.75, AM 0.50 vs 0.60) —
  so the fix was predicted to push the deepest composed slot FURTHER from goal, not nearer.
  C1's real value is that the phase label is now correct and the `InPoss` column becomes
  exercisable for the first time — a precondition for the calibration pass, not a creation fix.
  **The named creation lever remains C4.** Two new Class-A items fell out of this and are listed
  below.
- **C2 — #15's TRANSITION branch never republishes per-agent intents**, so `GetIntent` serves stale
  ones for the whole transition window.
- **C5 — `TacticalContext.HasAttackIntent` has no production consumer** (found at C1, Aug 8, 2026).
  The engine writes it every AI stride from `_attacking[t].GetIntent(i)`; nothing reads it. This is
  the SECOND lock on #15's door and the larger of the two — the phase gate was only the first.
  Class A by the audit's own definition; it was missed because the method counts *public methods
  with no caller*, and this is a *field with no reader*.
- **C6 — `GkHeadingWorldAdapter.ApplyKick` is not reachable from any test** (found at C1, Aug 8,
  2026). Headers and keeper parry/deflect/spill all strike the ball through it. Pure-function tests
  do not construct an engine; the `MatchEngineGkHeading*` tests and scenarios stop at *intent
  committed*; `GkRushTriggerTests` moves the keeper without a strike; and the only paths that
  plausibly reach it (`GkSaveDiagnosticTests`, `GkContactRateDiagnosticTests`) are env-gated and end
  in `Assert.Pass(…)`, so they cannot fail. There is no counter on that adapter and no seam that
  forces a contact frame. Not dormancy — reachability — but the same blind spot: nobody would notice
  if it stopped working.
- **C3 — `RunParameters.RunTriggerTick` is inert**, because run params are regenerated every
  heartbeat.
- **C7 — NO AGENT CAN RECEIVE A BALL OUT OF THE AIR, and 44% of final-third passes are aerial**
  (measured August 9, 2026; `close-chance-creation-design.md` §10.3). `RunFirstTouch` gate 2 and
  `RunLooseBallPickup` both refuse any ball whose centre height exceeds
  `FirstTouchConstants.GroundControlHeight` = `BallPhysicsConstants.Possession.ControlHeight` =
  **0.5 m**, on the stated grounds that "a higher ball is a Heading Mechanics (#10) event, not
  Stage 0". **CORRECTION (Aug 9, 2026, same day): the "heading is opt-in" half of this entry as
  first written was WRONG** — `EnableGkHeading` has been default-ON since July 27, 2026
  (`MatchEngine.cs:821` sets it unconditionally) and heading state is serialized in the v18
  snapshot block, so Phase 2 is largely done and the root `CLAUDE.md`'s "opt-in" wording is stale
  too. The measured consequence below is unaffected, because the real gap is sharper than
  "heading is off": a header **redirects** the ball and never grants possession, `HeadingMechanics`
  exposes no control/trap/chest entry point at all, and `TryCommitHeaderIntents` fires only for the
  single nearest outfield agent within **1.5 m**, once per airborne episode, always aimed at a fixed
  point (opponent goal X, pitch-width/2) and never at a team-mate. DT-emitted HEADER is deferred
  behind the 3-bit
  `ActionType` ordinal ceiling. **CORRECTION (Aug 10, 2026): the fixed-aim-point half of this entry
  named the symptom and mis-stated the mechanism.** The **value** was right; the fixed target was
  not the operative defect, because `TargetIntent` reached NO formula anywhere in the tree (see the
  new **W11** above) — the outgoing direction was pure specular reflection about
  `normalize(ballPosition − headCentre)`, so a header was a passive mirror and the player had no
  influence on direction at all, fixed point or otherwise. A defender clearing in his own box did
  not aim 90 m at the far goal; he headed the ball back the way it came. Fixed August 9, 2026 as
  `ERR-010-002`. The **"never at a team-mate" half stands and is still open** — it needs W9.
  **Measured consequence over 6 seeds × 90 min, 891 final-third
  passes: Lofted completes 1% (n=221), Cross completes 1% (n=171), against Ground 41% (n=441) and
  ThroughBall 28% (n=58).** Overall final-third completion 23%, interceptions 53%. An aerial
  delivery only becomes receivable after it lands and rolls, by which point the intended receiver
  is a mean of 19.0 m away. A cross is football's primary route into the penalty area and here it
  is a 1% pass. **This is the largest measured item in this backlog** and it is the head of
  `close-chance-creation-design.md` §10.4's corrected order. Its scope is the same
  `CollisionConsumer` AGENT_BALL duel fan-out + DT-emitted HEADER already carried as the open
  remainder of the #10/#11 engine integration — this entry supplies the measurement that ranks it.

- **C8 — The header commit has no head-height gate.** `MatchEngine.TryCommitHeaderIntents`
  (`MatchEngine.cs:3814+`) commits a header whenever the ball is loose and `bp.z >=
  HeaderTriggerMinBallHeightM` (**0.5 m**). But the header taker's head only occupies roughly
  **2.0–2.6 m** during the §3.2 eligibility window: apex = commit + `round(JUMP_PHASE_DURATION_MS *
  JUMP_APEX_FRACTION / FRAME_MS)` = +20 frames, window `[apex − 9, apex + 6]` from
  `MaxEarlyToleranceMs` 140 / `MaxLateToleranceMs` 90, and `JumpReachM` = `JUMP_REACH_BASE_M` 2.20 +
  attribute terms. A knee-high loose ball at 0.6 m therefore commits a header that cannot possibly
  connect.

  **Consequence:** `FailureCause.PositionedPoorly` is emitted whenever
  `HeadingEligibility.FindContactFrame` returns −1 **for any reason**, so it conflates "he was 4 m
  away horizontally" with "the ball was 1.8 m below his head" — and `positionedPoorly` is 97–99% of
  the 963 measured failed headers (`close-chance-creation-design.md` §10.6). Which of the two
  dominates has never been measured; **UNMEASURED**. It is cheaper than, and upstream of, the
  "attack the ball" mechanism the council refuted (note under §5 below) — and it cannot stall play,
  because it only ever REDUCES commits.

- **C9 — a #13 press-role holder is almost never within 3 m of the ball carrier** (found August 12,
  2026, by the W2 census, which was not looking for it). Measured over 3 seeds × 90 min, both defending
  teams: of ~310 episodes per team per match in which an outfielder came within 3 m of the carrier, the
  nearby man carried `PrimaryPress` or `CoverShadow` in **0.5** of them. #13's whole purpose is to send
  someone at the man on the ball. Either press roles are rarely assigned, or the designated presser
  never arrives — the census does not discriminate, and it should not be assumed to be either. **This is
  gate-level dormancy of exactly the class §1.1 says a static sweep cannot see**, and it was found by
  accident while measuring something else, which is the argument for building **W12** rather than
  trusting this list to be complete. Not a W2 defect and deliberately not fixed there.

- **C10 — `DefensiveAgentSnapshot.HasBall` is written and read by nothing.** The engine populates it
  every stride (`MatchEngine.cs:3348`) and `MarkAssigner` — the one consumer that would want it —
  contains **zero** ball references, so #14's mark assignment selects its target with no model of who
  has the ball. Same class as **C5** (`TacticalContext.HasAttackIntent`), in a second assembly, and
  missed by the v1.0 method for the same reason: the sweep counts methods with no caller, and this is a
  field with no reader.

- **C4 — #8 §3.1.3 cannot pass to a place, only to a player** — one PASS candidate per visible
  teammate at that teammate's *current* position. No pass into space, no through-ball to a run, no
  cross to an arriving header. A generator change, not a `[GT]`. **DEPRIORITIZED August 9, 2026:**
  `close-chance-creation-design.md` §7 item 1 called this "the real bound"; §10 retracts that
  ranking. C7 above and the box-geometry bound (§10.2) both sit ahead of it, and C7 makes landing
  C4 first actively harmful — the engine already plays 171 crosses per corpus into space at a 1%
  completion rate, so a candidate type that plays *more* balls into space adds to that bucket.
  Note also that its executor-side half is **not** missing: `PassExecutor` Path A
  (`ResolveSpaceTargetedAimPoint`) is fully implemented and merely has no producer.

---

## 4. Class C — small, lifecycle, or non-defects

Recorded so a later sweep does not re-litigate them.

| Surface | Assessment |
|---|---|
| `HeadingMechanics.CancelIntent` | No interrupt path exists. Becomes load-bearing when W2 lands (a tackle should cancel a header). |
| `RecognitionLatencyTracker.RemoveEntity` | Per-pair state is never reclaimed on expiry or substitution. Bounded arrays, so a leak in accuracy, not memory. Low. |
| `ShoulderCheckScheduler.ClearBlindSideState` | Window-close cleanup never runs. Same class as above. Low. |
| `AttackingAITick.GetSnapshot` | Observation accessor. Not a gap. |
| `CoverShadowCurve.ComputeCurveEffectiveness` | Telemetry-only. Not a gap. |
| `DecisionTree.SetMatchSeed` | **Not a defect** — the seed is supplied at construction (`MatchEngine.cs:829`). Redundant setter; delete or leave. |
| `BallCollision.ApplyGoalPostCollision`, most of `BallPhysicsCore` / `AgentLocomotion` / `PassTargetResolver` | Internal helpers driven by their own assembly's orchestrator. Correctly wired. **CORRECTION (Aug 9, 2026): this row is wrong for `PassTargetResolver.ResolveSpaceTargetedAimPoint`**, which `ResolveAimPoint` reaches only when `TargetAgentId == -1`, and no producer in `src/` ever sets that. It is dormant Class-A, not a correctly-wired helper; it acquires its first producer if C4 lands. The v1.0 sweep counted methods with no caller and missed it because the method *has* a caller — on a branch nothing can take. |
| `HeadingEligibility.cs:54-56`'s "must have left the ground" gate | Reads `CurrentState != AgentMovementState.GROUNDED && != STUMBLING` as "the agent is airborne", but `agent-movement/AgentMovementState.cs:14-36` defines `GROUNDED` as **"Agent knocked down. Full recovery required"** — not "on the ground". A standing, upright, running player already satisfies the gate, so it is effectively a no-op: there is no aerial/jump agent state in `#2` at all, and `AgentState.Position` is 2-D — the entire jump is synthetic head-Z inside `HeadingMechanics`. Assessment: a cross-spec semantic collision (#2 defines the term, #10 misreads it), not a wiring gap. A separate ERR candidate against #10; deliberately **not** folded into `ERR-010-002`. Changes what any future header-contact fix is a fix *of*. |

---

## 5. Proposed sequence

Each item is *wire + fix whatever the wiring surfaces*. Measurement and instruments are encouraged
throughout; `[GT]` landings are frozen per KD-W1 until the final pass.

| Order | Item | Rationale |
|---|---|---|
| 1 | ~~**W1** keeper rush trigger~~ ✅ **WIRED Aug 4, 2026; MEASURED Aug 12** | Whole subsystem existed; a trigger-condition problem. Surfaced `ERR-011-010` + `ERR-011-009`. **Its owed measurement is discharged** — 23–46 rush intents per match against a pre-W1 baseline of exactly 0 by construction, keepers 9.1–14.1 m off their line, no `ERR-011-009` re-stall (`gk-rush-trigger-design.md` §6). The CONVERSION effect §6 was ultimately for is still unmeasured. |
| 2 | ~~**C1** the `InPoss` gate~~ ✅ **FIXED Aug 8, 2026** (`ERR-012-011`) | Cheap, and the phase label was simply wrong. But the "unblocks #13/#14/#15" rationale was refuted before implementation — see the C1 entry: two of the three consumers are inert for reasons the gate does not touch. Re-measurement is the deliverable, not a creation gain. |
| 3 | ~~**W2** tackles~~ ✅ **WIRED Aug 12, 2026** | **Four**-link chain, not three. Measured before building: the gate supplied ~4× football's tackle rate, so this was a RESOLUTION problem, not a producer one. Governance question resolved by `ERR-014-006`: new #14 §3.6.5 takes the outcome model back on the W1 precedent, a four-outcome (`MISSED`/`BALL_WON`/`BALL_LOOSE`/`FOUL`) abstract attribute duel, ten new `[GT]` un-calibrated per KD-W1. Surfaced **C9**, **C10**. **GATE PASSED for W2 (August 12, 2026):** whole-tree build 0 errors / 0 warnings, quarantine empty, 32 suites; `MatchEngine.Tests` **461 passed / 1 failed / 11 skipped** (38 m 2 s). The single failure is `sim_match_engine_close_chance`, the inherited owner-held-red predicate that also fails at the pre-change baseline `4b9271c` — so the branch is at its baseline red state and W2 adds no new failure. Baseline was 451/1/10; the +10 passed are W2 locks and the +1 skipped is the env-gated census instrument. — see the W2 entry above. |
| 4 | **W4** keeper perception | Reuses tested occlusion. Upstream of all keeper behaviour, so it should precede any keeper calibration. **Next in sequence.** |
| 5 | **W12** the gate-firing instrument | Before calibration, and before assuming Class B is only four items. |
| 6 | **W5**, **W7**, **W6** | Small, independent, each self-contained. |
| 7 | **W3** + AGENT_BALL fan-out | One dependency, two consumers. The largest single build in this document. |
| 8 | **W8**, **W9**, **W10** | Fidelity items with working substitutes or a known rebaseline cost. |
| — | **then** one calibration pass | Against the complete engine, using the §5.Z instruments and seeded-corpus method. |

C2/C3/C4 are folded into whichever item touches their assembly; C4 in particular is the recorded
next lever on close-chance creation and is large enough to want its own pass.

**Note (Aug 10, 2026; updated 2026-08-09).** `close-chance-creation-design.md`'s "attack the ball"
ranking has now been withdrawn, re-ranked, and withdrawn again, and this note tracks the full
sequence so the two documents cannot drift out of sync a third time:

1. **§10.7 — withdrawn.** The pre-implementation council convened for `ERR-010-002`
   (`advisor-integrity` + `advisor-evidence`) refuted §10.6's candidate ordering, which had ranked
   "attack the ball — move a player to a ball's predicted arrival point" as the first lever: the
   ranking rested on a proximity census that is an instrument artifact (ball-to-agent distance
   measured ground-inclusive against an episode gate that guarantees > 0.5 m, so the near buckets were
   structurally unreachable and the published 0% was the instrument reporting its own gate — full
   derivation in §10.7). **C8** above and the vertical half of `HeadingEligibility.FindContactFrame`'s
   frozen head-height sweep were recorded as the two cheaper, upstream candidates ahead of it.
2. **§10.8 — re-ranked to first.** A corrected instrument (Report C5b, v1.4), asked the same 3-D
   question without the height-floor artifact, reached the same 0%-within-contact-distance conclusion
   by a sound route, and §10.8 re-ranked "attack the ball" first on that corrected evidence. This
   landing was **not** back-propagated into this backlog at the time, which is what let the two
   documents disagree.
3. **§10.10 — withdrawn again, and this time on a report that was already in hand.** Report C5d — the
   cross landing-point census, measured in §10.8's own run and not read until §10.10 — found an
   attacker within 5 m of a cross's landing point in ~96–99% of episodes, the opposite of §10.8's
   headline. §10.8's Series 1 was built on 1,081 pooled episodes against only 392 aerial final-third
   passes, a bimodal distribution masked by its mean. §10.10 also finds the mechanism §10.8 proposed
   to build already exists and is live at `OptionGenerator.cs:822`
   `GenerateInterceptCandidate` (#8 §3.1.9 INTERCEPT) — so the §10.6/§10.8 claim that nothing moves a
   player toward the ball's arrival point is **false**; what is true is narrower (Z-blind perception,
   a 1.5 s horizon frozen under KD-W1, and no instrument measuring whether INTERCEPT is ever
   generated or selected). **C5d's 31 m mean landing distance (30.8 m home / 32.2 m away) points at
   C4 — the delivery, not pursuit:** the penalty area is 16.5 m deep, and crosses are landing at
   roughly twice that from goal with an attacker already there, which is exactly the shape of "#8
   cannot pass to a place" rather than "nobody chases the ball". No new lever is ranked in §10.10;
   the ranking stands **withdrawn**, matching this backlog's own C7/C8 ordering below, which never
   adopted §10.8's re-ranking in the first place.

**Standing rule, so this cannot drift again:** this backlog's own item ordering (§5 table below) is
authoritative for wire-order, and any change to the "attack the ball" ranking in
`close-chance-creation-design.md` must be reflected in this note **in the same commit** that changes
it there. See `close-chance-creation-design.md` §10.10 for the C5d measurement and its VERSION
HISTORY v2.1 entry for the record of this update.

---

## 6. What this changes elsewhere

- **`CLAUDE.md`** — KD-W1 (the `[GT]` freeze) needs to sit beside the match-realism-pass entry, or
  it will be forgotten.
- **`.claude/skills/match-realism-pass`** — currently encodes measure → localize → ladder → land.
  The ladder step is premature for any target whose subsystem is unwired. Needs a gate at the top:
  *is the subsystem this touches fully wired? If not, this is a wiring task, not a realism pass.*
- **OPEN ISSUES** — W3/W9/W10 restate items already filed under the GK/Heading entry; W6 restates
  §5.Z.23 item (c); C1–C4 restate §5.Z.24's remainder. They are consolidated here rather than
  re-filed. The §5.Z.23 `pointQuality` owner decision is **parked**, not resolved: W1 changes the
  contact geometry that decision turns on, so deciding it now risks paying for a fix to a problem
  about to change shape.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.9 | 2026-08-15 | — | **Owner sequencing decision recorded on W2: it now gates the foul/card calibration.** The August-13 re-measurement (fouls 35.0 / yellows 5.0 / reds 1.00 per 90 vs football's ~22 / ~3.5 / ~0.25, fouls and yellows ~67% above their own July-26 post-balance figures) will NOT be fitted against today's engine. The call is arm W2 first, then calibrate once, because today's foul population is pre-tackle and arming the challenge routes ~47 challenges per team per 90 into the same single foul-candidate slot — KD-W1 read literally, with the July-26 pass as the counter-example (fitted correctly against the contact stream of its day; C1 moved that stream and the fit drifted unnoticed). Consequence for this backlog's own ordering: W2 is now the precondition for the most load-bearing open realism item, not merely the next wiring item, since #44 turns cards into suspensions. No `[GT]` moved, no code changed, no gate run — documentation only. |
| 1.8 | 2026-08-12 | — | **W2 WIRED.** New #14 §3.6.5 "Tackle Outcome Resolution" (`ERR-014-006`, filed and resolved same commit `fc8f81f2`) takes the tackle outcome decision back into the spec that owns the players, on the W1 precedent. A Stage-0 tackle is an abstract attribute duel — not a physics contact (#3 defers to Stage 2), not a DT action (the ordinal-8 ceiling) — with **four** outcomes: `MISSED` / `BALL_WON` / **`BALL_LOOSE`** / `FOUL`, `BALL_LOOSE` at owner direction so a successful challenge does not have to mean a clean turnover. New `TackleOutcome.cs` / `TackleDuelInputs.cs` / `TackleOutcomeResolver.cs` (ten `[GT]` + one `[FIXED]`, un-calibrated per KD-W1); `MatchEngine.cs` wires it at the COMMIT contact gate, re-derived (not fitted) from 1.5 → 2.5 m on what COMMIT means (a lunge); a tackle foul enters the existing single foul-candidate slot rather than a second authority; `ContactType.SLIDE_TACKLE` gets its first producer. `SNAPSHOT_SCHEMA_VERSION` **20 → 21**. `DOMAIN_TAG_DEFENSIVE_AI` (0x1A) gets its first draw site anywhere in `src/`, un-blocking #14's T-DA-DET-005; the `match-flow.card-severity` draw order **moves by design**; **no digest invariance claimed**. FM-08's CONTACT-time possession-loss log corrected from `LogError`("Race condition") to `LogWarning`. Tests: 12 pure resolver locks + 7 composed engine locks, all green at the landing commit. **GATE PASSED for W2 (August 12, 2026):** whole-tree build 0 errors / 0 warnings, quarantine empty, 32 suites; `MatchEngine.Tests` **461 passed / 1 failed / 11 skipped** (38 m 2 s). The single failure is `sim_match_engine_close_chance`, the inherited owner-held-red predicate that also fails at the pre-change baseline `4b9271c` — so the branch is at its baseline red state and W2 adds no new failure. Baseline was 451/1/10; the +10 passed are W2 locks and the +1 skipped is the env-gated census instrument. — a whole-tree gate was in flight when this row was written. §5's W2 row marked WIRED; next in sequence is **W4** (keeper perception), then **W12**. |
| 1.7 | 2026-08-12 | — | **W2 measured before being built, and the measurement refuted two of the investigation's own predictions.** Per defending team per match (3 seeds × 90 min, episodes not strides, teams never pooled): 681.7 defending episodes, 310.2 with an outfielder inside 3 m of the carrier, 97.2 with an intent naming him, **65.3 with a COMMIT** — ~4× football's ~15–17 tackle attempts, so **W2 is a RESOLUTION problem, not a producer problem and not dead upstream**, and the C1 shape does not repeat. Refuted: the FR-DA-010 presser exclusion is not the bound (presser 0.5 of 310), and `MarkAssigner`'s ball-blindness is a fidelity gap rather than a precondition. **W2's evidence list corrected** — it is a FOUR-link chain (`PassCancelledEvent`/`ShotCancelledEvent` have no production subscriber), the two hardcoded adapters are the PASS and SHOT adapters so #6's windup interrupt is equally dead, the `:6721`/`:6789` line numbers were stale, and the consequence is sharper than recorded: **no path anywhere dispossesses a controlled carrier**, so the engine has exactly two turnover mechanisms. **Two new Class-B items filed:** **C9** — a #13 press-role holder is within 3 m of the carrier in 0.5 episodes of ~310, on a subsystem whose purpose is to send someone at the ball; found by accident while measuring something else, which is the argument for **W12**. **C10** — `DefensiveAgentSnapshot.HasBall` is written every stride and read by nothing, the C5 class in a second assembly. **W1's measurement debt is discharged** (§5 row 1). **Wiring NOT landed:** blocked on one governance decision — the tackle outcome model has no owner, #14 stopping at intent by design, #8 barred by the 3-bit ordinal ceiling, and #3 deferring to Stage 2 while never landing the XC-4.4-02 amendment #5 was APPROVED around. `tackle-wiring-design.md` v1.1 carries the analysis, ten settled constraints, and three verified-free ERR ids. |
| 1.6 | 2026-08-11 | — | **Owner call landed on half of C1's gate-failure note (§5 table row 2 / the open-issues.md entry this backlog does not restate).** `sim_match_engine_close_chance` — one of the two predicates C1 drove red — is CONFIRMED: hold red, do not rebaseline a third time; queued for the KD-W1 calibration pass in §5's own sequencing rather than fitted around piecemeal. Formal record: `close-chance-creation-design.md` §10.9 item 6 (v2.2); mirrored in `open-issues.md` and root `CLAUDE.md`'s OPEN ISSUES index per the standing same-commit cross-reference rule below. `sim_match_engine_shot_outcomes`'s `fast-balls-deflect-off-bodies` reachability predicate — the other half of that gate failure — is untouched by this call and stays open; the branch stays red by design and the §5 wire-order (W2 next) is unchanged. No `[GT]` moved, no code changed, no gate run — documentation only. |
| 1.5 | 2026-08-09 | — | **§5's "attack the ball" note rewritten to record the full sequence, not just the withdrawal.** `close-chance-creation-design.md` §10.10 (Report C5d, the cross landing-point census — an attacker within 5 m of a cross's landing point in ~96–99% of episodes) withdraws §10.8's re-ranking of "attack the ball" to first, which this backlog's §5 note had never adopted in the first place — the two documents had been in conflict since §10.8 landed without a corresponding update here. The note now states all three steps (§10.7 withdrawn → §10.8 re-ranked → §10.10 withdrawn again), records that C5d's ~31 m mean landing distance points at **C4** (the delivery) rather than pursuit, and that the mechanism §10.8 proposed already exists and is live (`OptionGenerator.cs:822` `GenerateInterceptCandidate`). Adds a standing same-commit cross-reference rule so the two documents cannot drift again. No code changed in this revision; §5's table ordering (C7/C8 ahead of the withdrawn ranking) is unchanged — it never needed correcting. |
| 1.4 | 2026-08-09 | — | **Four findings from today's `ERR-010-002` landing and the pre-implementation council that preceded it.** **W11 filed and resolved same-day**: `HeaderIntent.TargetIntent` was written, clamped, serialized, and restored, and read by NO formula — the inverse of the phantom-interface rule this project tracks, and a class the v1.0 method (no-caller methods) and C5 (no-reader fields) both miss, because every one of TargetIntent's five steps looked like production wiring. Resolved by `ERR-010-002`; **the detection gap is not** — recommended as an explicit check for **W12**. **C8 filed** (new Class B): the header commit has no head-height gate — `TryCommitHeaderIntents` fires above 0.5 m while the head occupies ~2.0–2.6 m during the eligibility window, so `FailureCause.PositionedPoorly` (97–99% of 963 measured failures) conflates two unrelated causes; **UNMEASURED**, cheap, upstream, and cannot stall play. **C7 corrected in place**: the "always aimed at a fixed point … never at a team-mate" defect was mis-stated — the fixed target never reached a formula at all, so the header was pure specular reflection with no player influence on direction whatsoever; fixed as `ERR-010-002`. The "never at a team-mate" half stands, still open, needs W9. **One new Class-C row**: `HeadingEligibility.cs:54-56`'s "must have left the ground" gate reads `AgentMovementState.GROUNDED`, which `#2` defines as "knocked down," not "on the ground" — a cross-spec semantic collision, not a wiring gap; filed as a separate ERR candidate against #10, deliberately not folded into `ERR-010-002`. **Also**: a note under §5 records that the same council refuted `close-chance-creation-design.md` §10.6's "attack the ball" ranking as resting on an instrument artifact (§10.7) — withdrawn, not replaced; C8 and `FindContactFrame`'s frozen-head-height vertical half are the two cheaper candidates recorded ahead of it. No code changed in this revision. |
| 1.3 | 2026-08-08 | — | **C7 filed, and it is the largest measured item in this document** — no agent can receive a ball above 0.5 m (`RunFirstTouch` gate 2 and `RunLooseBallPickup` both refuse it, heading being deferred out of Stage 0), and 44% of final-third passes are aerial: measured over 6 seeds × 90 min, **Lofted completes 1% (n=221) and Cross 1% (n=171)** against Ground 41% and ThroughBall 28%, with overall final-third completion 23%. **C4 deprioritized** on the same measurement — `close-chance-creation-design.md` §7 item 1's "the real bound" claim is retracted in §10, and landing C4 first would add to a bucket that already completes 1%. **One Class-C row corrected**: `PassTargetResolver.ResolveSpaceTargetedAimPoint` is dormant Class-A, not a correctly-wired helper — the v1.0 sweep missed it because the method has a caller on a branch nothing can take. No code changed in this revision. |
| 1.2 | 2026-08-08 | — | **C1 fixed** (`ERR-012-011`) — #12 §3.0 classifies phase from TEAM possession; the engine gains a pass-in-flight receiver latch (`SNAPSHOT_SCHEMA_VERSION` 19 → 20), no new `[GT]`. **This document's own claim that C1 was "probably the highest-value item" is retracted in place**, refuted by the pre-implementation council: the gate is real but two of the three consumers it was meant to unblock are inert for unrelated reasons, and the third lever (#12's `PullFactor` `InPoss` column) is LESS advanced than the `TransToAtk` column it replaces, so the fix was predicted to move the shape slightly AWAY from goal. C1's value is a correct label plus a first-time-exercisable `InPoss` column for the calibration pass. **Two new Class-A items filed from the same investigation**: **C5** `TacticalContext.HasAttackIntent` is written by the engine and read by no production code (the second, larger lock on #15's door — missed by the v1.0 method because it counts methods with no caller, not fields with no reader), and **C6** `GkHeadingWorldAdapter.ApplyKick` is not reachable from any test. Next in sequence is **W2**, tackles. |
| 1.1 | 2026-08-04 | — | **W1 wired** (`docs/tracking/gk-rush-trigger-design.md`) — `CommitRushIntent` has a production caller for the first time. Surfaced and fixed **two** spec defects: `ERR-011-010` (§3.7 delegated the rush decision to Decision Tree #8, which cannot make it — so the condition had no owner for ten weeks, and the spec never said what the keeper was deciding; new §3.7.0 states it, and the keeper comes out to REDUCE THE SHOOTING ANGLE, so a chasing defender does not keep him home and the distance is his own attributes) and `ERR-011-009` (a rush that reached its target had no §3.1.1 exit, so a swept loose ball stranded the keeper in `Rushing` for the rest of the match). Measurement not run — no .NET SDK in the authoring environment. Nine Class-A items remain; the next in sequence is **C1**, the `InPoss` gate. |
| 1.0 | 2026-08-04 | — | Initial audit. Three-pass sweep over the 18 assemblies the match engine references; 10 Class-A dormant capabilities, 4 Class-B starved gates carried from §5.Z.24, 7 Class-C non-defects. Establishes KD-W1 (`[GT]` freeze) and KD-W2 (scope). |
