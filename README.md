# Tactical Director: Football Management Simulation

**Created:** December 30, 2025, 11:50 AM PST
**Last Updated:** August 6, 2026 (**ERR-008-021 — a shot is now harder to take past a good defender
than past a poor one.** The judgment-proxy review's third fix closes the follow-up deferred when the
pass-lane template landed: the check that measures how much of the goal an opponent blocks out no
longer treats every outfield body as the same obstacle — a blocker's Anticipation and Pace now scale
his effective cover, read through the shooter's Vision, using the exact constants the pass lane
already uses, so no new tuning dial was added. The goalkeeper's cover deliberately stays as pure
geometry, because his shot-stopping quality is already priced at the save itself and counting it
twice would double-punish shooters. An average or unknown blocker reproduces today's behaviour
exactly. Spec and code landed in the same commit with six new test locks, including the away-side
mirror. The gate cannot run in this environment; CI on push compiles and executes it.)

**Last Updated (prior):** August 5, 2026, end of same day (**ERR-008-019 — one recorded claim about the
long-shot ramp was wrong and is withdrawn.** A review of yesterday's landing found that the note
saying the change "moves no match digests" rested on a false assumption about how a player takes
possession of the ball: it assumed the player must be within half a metre of it, when the engine
actually hands possession to anyone within a metre of a stationary loose ball and leaves the ball
where it is — and nothing pulls it back to him afterwards. That extra half-metre is enough for a
shooter rated 19 (not only 20) to take a midfield shot, and at 19 the new ramp gives a slightly
different number from the old step. So the change **can** alter match results on some seeds. That
is fine — the wider ramp is what the owner asked for — but the "no effect on results" claim was
not true and has been retracted everywhere it was recorded. Nothing else changed: no formula, no
tunable, no test. One documentation fix went with it (the ramp's half-width is pinned at its
maximum by a test, so the range its comment advertised was misleading). Gate not runnable in the
authoring environment; CI runs it on push. Prior entry below.)

**Last Updated (prior):** August 5, 2026, later same day (**ERR-008-019 owner revision — every Long Shots
point now matters for midfield shooting.** At owner direction, the just-landed ramp widened from
its initial 8–13 band to the full 1–20 attribute range: a rating of 1 keeps the full suppression,
20 the full long-shot modifier, and every point in between moves the willingness smoothly — no
plateaus. One tunable changed (`LONG_SHOT_RAMP_HALF_WIDTH` to its maximum 0.25); the formula, the
midpoint anchor, and the population-average balance are untouched, and the change still moves no
match digests (only a maximum-rated shooter can even generate a midfield shot, and for him the
ramp equals the old value). Gate not runnable in the authoring environment; CI runs it on push.
Prior entry below.)

**Last Updated (prior):** August 5, 2026 (**ERR-008-019 — the second fix under the football-judgment
remediation doctrine, and the closing of the review's founding finding.** Decision Tree #8
§3.2.3.1's midfield long-shot gate — the original "11× jump for a 1-point attribute difference"
cliff the whole judgment-proxy review was named after, whose earlier "FIXED" record proved false —
is now a linear ramp in the same shifted attribute form, centred on the old threshold so endpoints
and the population-integrated modifier are preserved (doctrine P1/P5; spec, code, ERR entry and
five test locks in one commit; the soft-reserved id re-verified free at landing). The branch is
production-unreachable in the only band the fix changes (the ramp differs from the old step only
for LongShots values whose own range gate keeps the shooter ~5 m short of any midfield shot), so
no digest moves; the fix lands anyway because a wrong-shaped model cannot be repaired by later
tuning. Review tally: 2 fixed, 32 open.
Gate not runnable in the authoring environment (no .NET SDK); CI runs it on push. Prior entry
below.)

**Last Updated (prior):** August 4, 2026 (**ERR-008-020 — the first fix under the football-judgment
remediation doctrine.** The new `docs/tracking/football-judgment-proxy-review.md` swept all 53
APPROVED specs for continuous football judgments collapsed into thresholds or bare geometry — 34
findings across 24 specs — and its owner-converged §6 doctrine (P1 continuous-never-cliff, P2 skill
as discrimination fidelity, P3 the attribute ownership ledger, P4 intent as a first-class object,
P5 chain calibration pivoted on today's baseline) now governs every fix. The template landed same
day: Decision Tree #8 §3.1.3.3's binary pass-lane interceptor corridor became a continuous,
attribute-weighted threat model — a defender's Anticipation/Pace now prices the lane, read through
the passer's Vision — with spec, code, ERR entry, and tests in one commit. Also corrected at that
landing: the review's inherited claim that the ERR-008-019 long-shot cliff was already fixed was
false — the finding is re-opened. Gate not runnable in the authoring environment (no .NET SDK);
CI runs it on push. *Drift note: this file had also trailed the two August-4 W1 keeper-rush
landings — see `docs/tracking/CHANGELOG.md` for those; they are not reconstructed here.* Prior
entry below.)

**Last Updated (prior):** August 3, 2026, latest same day (**Interactive Unity client P4a LANDED — the
host-free render model, and P4 is split.** P4a is every render *decision*; P4b is the binding. That
split turns the standing "keep logic out of `MonoBehaviour`s" rule from a discipline into a phase
boundary, so what the pinned host is left to verify is binding — which a cert run genuinely checks —
rather than behaviour, which it checks only along the paths someone thought to click. New in
gate-compiled `src/match-client-core/`: `PitchViewProjection` (the one documented corner-origin ⇄
centre-origin adapter, both directions; centring is what makes a home position and its away mirror
differ only in sign), `PitchMarkings`/`PitchMarking`/`PitchMarkingKind` (the IFAB catalogue as shapes,
read from the *existing* `MatchViewerConstants` `[FIXED]` values, both ends emitted from one loop over
a sign; rectangles arrive corner-normalised so a binding cannot draw the away boxes inverted),
`MatchRoster` (match-constant per-slot data, with the shirt-numbering rule shared with the browser
viewer through `RosterShirtNumbers`), and `MatchRenderProjection` → `AgentRenderModel`/`BallRenderModel`
(positions from the P3 interpolator's buffer, discrete cues from the newest frame, possession ring,
ball shadow/lift/capped scale). Colour-free by design — a palette has no correct answer a test could
assert. **The finding (KD-P4a-1):** `LiveMatchStreamer` cached goalkeeper flags as immutable roster
metadata, but `MatchEngine.SubstitutePlayer` rewrites them — so a keeper substitution had been drawing
the keeper ring on the wrong player in the browser viewer since P1. **The view was then revised to a tilted, slightly off-centre perspective camera (KD-P4a-2)** — which deletes the faked ball-height cues and their three `[GT]` dials, since a tilted camera conveys altitude by itself, and adds a ray/ground-plane click inverse in their place. The keeper flag now rides `LiveAgentCue`
per tick and `MatchRoster` deliberately holds none, which fixes both surfaces. No
`SNAPSHOT_SCHEMA_VERSION` change, no engine-behaviour change. **Adversarially reviewed August 4,
2026 — 1 High, 5 Medium, 3 Low fixed, then re-run clean:** the High was `PitchMarking.Rectangle`
taking its corners in either order while `PitchMarkings` builds the end boxes goal-line-inwards, so
the two away boxes arrived with descending X and a binding using `B − A` as an extent would have
drawn exactly those two inverted — #8 ERR-008-002's home/away asymmetry class, in a `MonoBehaviour`
the gate can never see. **Next: P4b on the pinned host.**)

**Last Updated (prior):** August 3, 2026, latest same day (**Owner decision — roadmap B6 reversed: the product
ships the full Unity UI, not the web-hosted viewer.** Doc-only. `src/match-client-web/` is retained and
reclassified as the host-free reference harness — the only surface exercising read/playback/intent in
CI on every push — while `src/match-client-unity/` (asmdef + README; P4 never started) becomes the
critical path. Nothing blocks P4: the whole substrate a UGUI skin binds is already gate-compiled and
unchanged. Standing rule recorded in `interactive-unity-client-design.md` §12 and
`path-to-playable-roadmap.md` §7/C2 — **keep logic out of `MonoBehaviour`s**, since the CI gate cannot
compile that assembly and faking `MonoBehaviour` in the Unity shim is explicitly refused. `PM-1`'s three
screen-facing exit criteria reopen against the Unity client; its determinism criterion is met head-lessly
and stays met.)

**Last Updated (prior):** August 3, 2026, later same day (**Interactive Unity client P6 — the head-less
closed-loop scenario LANDED, ahead of P4.** The client's input-determinism claim is now checked on
every push rather than asserted. Two closed-loop scenarios on the #19 `ScenarioRunner`, booted through
the real `MatchSession`: same `MatchSetup` + the same tick-stamped command log ⇒ digest-identical runs,
and save@90 → restore → replay the post-90 log to tick 180 == the uninterrupted run. Landed before the
Unity render skin for the reason the plan's §12 gives — `match-client-unity` is excluded from the
`tools/dotnet-ci` shim, so **every P4/P5 line is invisible to CI** while this is not; the skin now
arrives against an existing lock rather than ahead of one.

**The phase needed three production additions before any scenario could be written**, because
`MatchSession` could not be advanced head-lessly, saved, or restored: `TickOnce()` (driving the real
streamer seam, and refusing fail-loud once paced playback has started — two threads through one engine
is a data race), `CaptureSave()` (riding the `ServiceOnce` seam so it works running, paused and at full
time, with the drained-empty-before-capture invariant now held by *ordering* rather than asserted), and
`RestoreFrom()` (a session over a restored engine, re-applying no boot mutator). Plus
`TickStampedCommandReplay` — the log-replay mechanism the reproducibility invariant is *defined*
against, under which the log is a fixed point of its own replay.

**The predicate that carries it is the control run.** Both scenarios would pass on a command channel
that did nothing — a run reproducing itself says nothing about whether the commands are in the loop —
so a third session with the same setup and **no commands** must DIVERGE, in a bounded window around
the first command. That is the direct lesson of the capstone that asserted a match ticked while every
match was a 90-minute 0–0 deadlock. The script drives all three live mutators across **both** teams.

No engine behaviour changed; no schema, RNG-stream, domain-tag, draw-site or draw-order change. Gate
not runnable in this environment (the network policy blocks the .NET SDK download); verified by
exhaustive manual review and a project-generation run, and it runs in CI on the PR. **Prior entry
below.**)

**Last Updated (prior):** August 3, 2026 (**§5.Z.23 conversion at contact — ERR-011-008: a keeper's CATCH
never stopped the ball.** #11 §3.5.2's catch branch is two statements — the possession record AND
`ball.velocity = gkHandVelocity` ("parked at hand position") — and only the first was implemented.
Possession here is a flag, not a kinematic constraint (the ball integrates unconditionally; the goal
check adjudicates on ball POSITION), so a claimed shot flew on into the net. Measured per contact over
three full matches: ball speed in → out is **parried 10.8 → 0.0, deflected 10.3 → 4.2, spilled
13.9 → 9.0, missed 9.5 → 9.5 — and caught 11.1 → 10.8**, with **7 of 10 catches followed by a goal
within 5 s** (parries and spills: zero). This **refutes** §5.Z.22's recorded premise that the residual
lay in what marginal parries and spills do. Fixed with a new `IGoalkeeperBallSystem.ParkBall()` at both
claim sites. **Goals 5.0 → 3.7 per match — the closest this engine has measured to football's ~2.7 —
scorelines 2-2/2-0/6-3 → 1-0/2-2/4-2.** No schema / RNG / draw-order change. Both levers §5.Z.22
named are recorded NOT fixed with evidence: the `pointQuality` lottery is confirmed blind *and*
inverted, and the geometry-aware form was implemented, measured (catches 11 → 0 at every in-range
`[GT]`) and reverted — it is blocked on a design decision, not effort; parry placement produced zero
goals in either corpus. The creation residual is re-localized from "possession churn" to a measured
stage: **final third → penalty area converts at 6.5% against football's ~40%**, while shots per BOX
entry already run above football's rate.

> **Note on this file's currency:** the entry chain below trails the engine by several landings — the
> previous entry is the July 27 shot-outcome pass, so §5.Z.18–§5.Z.22 (shot speed and woodwork, the
> keeper's catch/parry conversion, shot volume, the keeper's contact rate) and the August 3 project-
> skills landing are recorded in `docs/tracking/CHANGELOG.md` and `docs/tracking/PROGRESS.md` but not
> here. Reconciling them is its own pass; this entry was appended rather than layered over a silently
> stale summary.)

**Last Updated (prior):** July 27, 2026, latest same day (**Shot-outcome distribution pass — §5.Z.17's residual,
the named A4a blocker, fixed and measured.** Shots can now miss (a genuine `tan(err) × distance` error
cone, ERR-006-003, plus the vertical placement/error half made live per #6's own §3.5.6/§3.5.7 —
ERR-006-002), **the goal has a crossbar** (the `z < 0.22 m` boundary gate removed per Law 9/10:
airborne crossings adjudicate at the crossing — ERR-001-004), and **shots are blocked** (the empty-TODO
agent-ball deflection is live via the new `BallCollision.ApplyAgentDeflection`, `BodyPartCoefficients`'
first consumer — ERR-003-007), with the shot pressure query wired and the vacuous goal-visibility gate
raised off its floor. Measured over three full matches, same seeds pre/post: **goals 15.3 → 12.3 per
match, goals/shot 0.24–0.29 → 0.14–0.25, fast-ball body deflections 0 → 560–612 per match.** The
remaining goal-rate mass is recorded, not fixed: shot volume (~2.5× football), shot speed (means
7–10 m/s vs ~25), keeper conversion. New `match-engine-shot-outcomes` acceptance scenario — 3 of 8
predicates fail on the pre-fix engine, verified by execution. Full dotnet gate PASSED; no
`SNAPSHOT_SCHEMA_VERSION` change. See `docs/tracking/shot-outcome-distribution-design.md`. Prior entry
below.)
**Last Updated (prior):** July 27, 2026, later same day (**Documentation sync pass — no code, no spec, no gate
run.** Cross-referenced this file, `CLAUDE.md`, `src/CLAUDE.md`, `SPEC_INDEX.md`, and `docs/tracking/`
against the actual repo state and found four discrepancies, all now corrected here and in the other root
docs: (1) **Match Analytics #37 T0** (`src/match-analytics/` — value types + `XgLocationModel`) landed
July 27 without updating this file or `CLAUDE.md`, so the assembly count (29 → **30**) and the
"APPROVED with no assembly" count (23 → **22**) were both stale, and #37's status row here still read
"none". (2) **Track C B1** (the interactive-Unity-client richer observation frame) landed the same way —
already recorded in `path-to-playable-roadmap.md` but not folded into either root doc. (3)
**`SNAPSHOT_SCHEMA_VERSION` was stale at 18**; the match-realism landing below (home/away asymmetry +
contact-rate fixes) bumped it to **19** the same day. (4) The home/away-asymmetry / goal-rate OPEN ISSUES
entry in `CLAUDE.md` was stale in the other direction — it described the asymmetry as an open blocker,
but the root cause (`GoalGeometryProvider` always returning the same `GoalLineX`, so both teams shot at
one goal) was found and fixed the same day; corrected there. `docs/tracking/file-manifest.md`'s
"Current Specification Folders" table was separately found stuck at 26 rows since July 8, 2026 (missing
the entire #27–#54 wave) and is fixed in the same pass. Prior entry below.)
**Last Updated (prior):** July 27, 2026, later same day (**The specification phase is CLOSED — all ten
approved.** `SPEC_INDEX.md` reads **53 APPROVED / 0 IN REVIEW / 0 NOT STARTED**. Lead-developer sign-off
granted on #53, #35, #46, #36, #54, #47, #48, #50, #51 and #39, with **23 back-props filed atomically**.
Docs only — no code, no `src/` change, no gate run. **The finding that justified landing them together:** #30's pinned tick order was not implementable, because `ERR-030-007` had been filed twice at two separate approvals — a defect neither approval could have seen alone. See VERSION HISTORY v1.37.)
**Last Updated (prior):** July 26, 2026 (**Root-doc reconciliation — this file re-based on the actual repo
state.** It had been pinned at July 14, 2026 and **26 specs**: twelve days and seventeen approved
specs stale, still describing `SNAPSHOT_SCHEMA_VERSION` 15 (actual **18**) and still listing the
Unity 6 recertification and the FR-PO-052 perf baseline as outstanding — both of which completed
July 19. Updated: CURRENT STATUS (**43 APPROVED / 0 IN REVIEW / 0 NOT STARTED**; 29 production
assemblies; the Phase-H possession bootstrap that makes a match actually play); the specification
schedule (a new table for the 17 management-layer specs #27–#49 approved July 22–25, each with its
real implementation status, plus the seven design supplements awaiting promotion); PROJECT STRUCTURE
(the real tree — `tools/`, `docs/design/`, the Unity shell, and a layer-grouped `src/` listing);
and NEXT IMMEDIATE STEPS (re-based on `path-to-playable-roadmap.md`, which is now the critical path).
**The load-bearing addition is the honest gap:** **13 APPROVED specs have no assembly at all**
(#29, #31–#34, #37, #40–#45, #49) — "approved" had become a misleading proxy for "a consumer
exists", and both root docs now say so plainly. Also recorded: assembly names do not reliably match
spec folder names (#27 → `player-database`, #28 → `player-progression`, #30 → `season-save`, and #38
→ `ui-framework`, with #23–#25 inside `positioning-ai` and #26 inside `tactical-instructions`), and the
42-file design-supplement governance class, which had appeared in no root document. The dotnet gate
was **not** re-run in the authoring environment (no SDK), so the gate claims restated here were quoted
from the last landing's record — but CI subsequently ran the full Linux shim gate green on this
branch, re-verifying them independently. See `CLAUDE.md` for the matching pass on the
AI-behavioural-rules side.)
**Last Updated (prior):** July 14, 2026 (**Match-flow model completion LANDED** — throw-ins, corners,
goal kicks, fouls/cards, offside, substitutions, half-time break, and full-time end (previously
only kickoff + goal-restart existed). Design doc adversarially reviewed to convergence, then
implemented, then the code itself adversarially reviewed to convergence (catching an
`OffsideEvaluator` bug where too few defenders left an accumulator at an `Infinity` sentinel
instead of `NaN`, inverting the offside rule for every finite attacker position). New
`src/match-engine/RestartResolver.cs`, `OffsideEvaluator.cs`, `SubstitutionReason.cs`; three new
Tier A events (`OffsideCalledEvent` 0x18, `RestartAwardedEvent` 0x19, `MatchPhaseChangedEvent`
0x1A). `MatchEngine.cs` v1.31 gains restart routing, per-tick foul/card detection with sent-off
tracking, offside evaluation on pass reception, a public `SubstitutePlayer` (bench-roster swap,
pending-event queue flushed at the next Resolve phase per an AR-5 fix), and half-time/full-time
transition handling. **`SNAPSHOT_SCHEMA_VERSION` 14 → 15.** New test suites for restarts, offside,
fouls/cards, substitutions, and match-flow transitions. Full dotnet gate not runnable in this
environment — verified by exhaustive manual code review in place of `dotnet test`. See
`docs/tracking/match-flow-completion-design.md`, `docs/tracking/match-engine-design.md` v2.0, and
`src/CLAUDE.md` v2.17.)
**Last Updated (prior):** July 13, 2026, later same day (**P1 real perf harness landed** —
`StopwatchPerfHarness` + `MatchEngineCapstonePerfHarness` boot the real capstone scenario and
Stopwatch-time each `RunTick`, superseding the synthetic `run.sh` stub that ran no `src/` code.
The Linux run is stamped non-certifying; a certified number still needs pinned-host access
(Windows 11 / Unity 6000.4.9f1) plus a real cert run. `CertifiedPerfBaseline.Stage0CertPlatformPin`
updated to the Unity-6 tuple string.)
**Last Updated (prior):** July 13, 2026 (**Unity engine version bumped: 2022.3.62f1 → Unity
6000.4.9f1, graphics API pinned DX11 — documentation-only, no recertification performed.**
`certification-platform.md` → v1.3, Status reverted `✅ PINNED` → `⏳ RECERT REQUIRED` per its own
Maintenance Rule; every downstream unblocker it previously closed (`FR-DS-009-GATE`, `FR-PO-052`,
the §7.5 D1 test-runner pin, `EnvironmentFingerprint`) is blocked again until a real cert run
executes against the new tuple. Historical `Unity 2022.3` citations inside already-`APPROVED`
spec section files deliberately left untouched — frozen approval-time records, per this project's
"historical rows preserved verbatim" convention.)
**Last Updated (prior):** July 11, 2026, latest same day (**Engine substrate landed** — goal
detection (`MatchEngine.cs` v1.30: Resolve-phase goal check + scoring + centre-spot restart +
`GoalAwardedEvent`) and the match-length/halves model (`MATCH_TICKS_TOTAL` = 324,000 ticks,
`HALF_TIME_BOUNDARY_TICK` = 162,000). **`SNAPSHOT_SCHEMA_VERSION` 13 → 14.** This activates
Tactical Presets **#26**'s half-time trigger and live goalDiff/clock ladder inputs — its
engine-substrate gates (§9.1) are now closed; only the §9.2 `[GT]` balance-pass review remains.
Full dotnet gate: PASSED, 0 failures.)
**Last Updated (prior):** July 11, 2026 (**Specs #23/#24/#25/#26 wiring landed**, all
default-behaviour-neutral — Balanced ⇒ Off/None/Off/Human are exact identities, byte-identical
default match: the #24 build-up overlay + #23 dismark offset `SlotComposer` stages;
`positioning-ai/RotationController.cs` (#25) wired into `PositioningAITick`; the #23 marked-pass-
target penalty in the Decision Tree's `UtilityScorer`; and **#26**'s full T1–T4 stack (preset→
config projection, kickoff/interval decision gate, kickoff scoring, mid-match adaptation ladder)
via `ManagerDecisionGate`/`ManagerProfile`/`ManagerAdaptation`. `SNAPSHOT_SCHEMA_VERSION` 11 → 12
→ 13 across the two commits. Full dotnet gate: PASSED, 0 failures both times.)
**Last Updated (prior):** July 10, 2026, later same day (**Specs #23–#26 — Dismarking &
Marker-Awareness AI, Scripted Build-Up Structures, Positional Rotations, Tactical Presets &
AI-Manager Selection — all advanced `IN REVIEW` → `APPROVED`.** PASS-1 section-file adversarial
reviews resolved same day; lead-developer R-01..R-05 sign-off granted on all four; the seven
cross-spec back-prop ERRs filed and landed atomically; the #26 Bradley citation VERIFIED. `docs/
specs/dismarking-ai/`, `build-up-structures/`, `positional-rotations/`, `tactical-presets/`
folders now exist. **`SPEC_INDEX.md`: 26 APPROVED / 0 IN REVIEW / 0 NOT STARTED.** T0 scaffolding
(behaviour-neutral data types + pure math) landed the same day.)
**Last Updated (prior):** July 10, 2026 (**#23–#26 post-PASS-1 gates closed where closable** — §8
citations verified/replaced/reclassified across all four specs (one #26 row remains pending with
a recorded environment-blocked attempt); #25 Appendix A completed for all three formation
families; #26 A.1 preset compositions pinned against the #21 enums. Remaining before `APPROVED`:
the #26 Bradley citation, back-prop ERRs, #26 engine-substrate gates, R-01..R-05 sign-off.)
**Last Updated (prior):** July 8, 2026 (Candidates **#23–#26 promoted to section files at `IN REVIEW`** —
`docs/specs/dismarking-ai/` (#23), `docs/specs/build-up-structures/` (#24),
`docs/specs/positional-rotations/` (#25), `docs/specs/tactical-presets/` (#26), each a full
11-file spec set (v0.1) authored from its July 7 design supplement per that supplement's §6
promotion pipeline. `SPEC_INDEX.md` now reads **22 APPROVED / 4 IN REVIEW**; the RESERVED entries
are retired. Section-file PASS-1 adversarial reviews are pending per each spec's §9.3 — no `src/`
code lands until each is `APPROVED`.)
**Last Updated (prior):** July 7, 2026, later same day (Two design supplements opened, scoping the four
items the same day's tactical-theory cross-reference flagged as too large for a cheap seam
reuse: `docs/tracking/advanced-positional-behaviors-design.md` (dismarking, scripted build-up
structures, positional rotations — candidate specs #23–#25) and
`docs/tracking/game-model-ai-manager-design.md` (tactical preset library + AI-manager
selection/adaptation — candidate #26). DESIGN SUPPLEMENT stage only, pre-promotion, no code —
see root `CLAUDE.md` OPEN ISSUES and `SPEC_INDEX.md` "RESERVED" section.)
**Last Updated (prior):** July 7, 2026 (Tactical-theory research cross-reference: four small tactical additions landed on top of #21/#12/#13/#14/#8 — a `MarkingOrientation` dial (ball- vs man-oriented marking, scales the #14 MAN_MARK radius), a Positioning AI #12 rest-defense coverage check (dampens risky PASS/SHOOT/DRIBBLE when insufficient cover is left behind while attacking), a half-spaces PASS bonus (routes each agent's existing #12 lane into the Decision Tree #8 utility scorer), and a curving-press blind-side bias (#13 nudges the primary presser's approach toward the ball carrier's blind side). All four default to today's exact behaviour (byte-identical) until a manager sets a non-default tactic. `SNAPSHOT_SCHEMA_VERSION` 10 → 11. Prior June 29, 2026 (Tactical Instructions **#21** T0 data layer + T2 consumer seams (DecisionTree/Pressing/Positioning/Defensive/Attacking, behaviour-neutral) + runtime activation landed — `MatchEngine.SetTeamTactic`, the per-team Phase-D writers for #8/#13, and an in-code `TeamTacticConfig` source + boot applier; the on-disk tactic-file format is deferred to Stage 0+1 per the #19 D1 precedent. Prior June 20, 2026: Tactical Instructions **#21** APPROVED — first Stage-1 forward spec beyond the Stage-0 set of 20: the manager-facing tactical instruction layer (formation/mentality/team instructions/player roles+duties/individual instructions) that drives the existing AI subsystems #8/#11–#15. 11 section files at v0.3 after PASS-1 + PASS-2 adversarial reviews; lead-developer sign-off granted. Its `[GT]` balance-pass values are illustrative pending a non-blocking Stage-1 follow-up; no `src/` code yet (T0 scaffolding pending). `SPEC_INDEX.md` count now **21 APPROVED**. Prior June 7, 2026: AR-hardening sweep complete across all 18 coded sections; 350+ source files across 17 spec assemblies + Testing Strategy CI tooling. See `docs/tracking/PROGRESS.md` for per-spec AR-round tallies.)
**Last Updated (prior):** July 7, 2026, later same day (Tactical-theory research cross-reference follow-up: after user review, three of the four July 7 tactical additions were corrected. A `MarkingOrientation` dial (ball- vs man-oriented marking, scales the #14 MAN_MARK radius) stands unchanged. The rest-defense coverage check (Positioning AI #12) is redesigned so its PASS/SHOOT/DRIBBLE dampener only applies in proportion to the ball carrier's own tactical awareness — an unaware carrier is never silently corrected for what is really a manager-facing setup flaw. The half-spaces PASS bonus is reverted entirely — half-spaces are an exploitable spatial gap that requires tactical/player instructions to exploit, not a flat statistical bonus. The curving-press mechanic is redesigned from a flat "blind-side approach" bias into a cover-shadow curve (#13): a presser now bends their pursuit path toward denying a nearby passing option, scaled by their own defensive/physical/mental attributes, so a poor or low-effort defender barely curves at all. `SNAPSHOT_SCHEMA_VERSION` 10 → 11 (unaffected by the corrections — none of the reverted/redesigned items were ever serialized). Prior July 7, 2026 (initial landing, since corrected): Tactical-theory research cross-reference: four small tactical additions landed on top of #21/#12/#13/#14/#8. Prior June 29, 2026 (Tactical Instructions **#21** T0 data layer + T2 consumer seams (DecisionTree/Pressing/Positioning/Defensive/Attacking, behaviour-neutral) + runtime activation landed — `MatchEngine.SetTeamTactic`, the per-team Phase-D writers for #8/#13, and an in-code `TeamTacticConfig` source + boot applier; the on-disk tactic-file format is deferred to Stage 0+1 per the #19 D1 precedent. Prior June 20, 2026: Tactical Instructions **#21** APPROVED — first Stage-1 forward spec beyond the Stage-0 set of 20: the manager-facing tactical instruction layer (formation/mentality/team instructions/player roles+duties/individual instructions) that drives the existing AI subsystems #8/#11–#15. 11 section files at v0.3 after PASS-1 + PASS-2 adversarial reviews; lead-developer sign-off granted. Its `[GT]` balance-pass values are illustrative pending a non-blocking Stage-1 follow-up; no `src/` code yet (T0 scaffolding pending). `SPEC_INDEX.md` count now **21 APPROVED**. Prior June 7, 2026: AR-hardening sweep complete across all 18 coded sections; 350+ source files across 17 spec assemblies + Testing Strategy CI tooling. See `docs/tracking/PROGRESS.md` for per-spec AR-round tallies.)
**Project Type:** Full-scale football management simulation
**Development Timeline:** Open-ended passion project, staged releases
**Target:** The Football Manager Killer
**Current Stage:** Stage 0+1 — Implementation (coding begun May 19, 2026). All 26 approved specs
(20 Stage-0 + 6 Stage-1 forward specs #21–#26) have active `src/` implementations; the match
engine composition root now includes goal detection, halves/full-time, and the complete
match-flow model (throw-ins, corners, goal kicks, fouls/cards, offside, substitutions).

---

## PROJECT VISION

Build the definitive football management simulation with:
- **Observable Systems:** Every tactic has visible, measurable impact
- **Deep Physics:** Sim-quality ball physics and player movement
- **Psychological Depth:** Complex social and mental systems
- **Transparent Mechanics:** Users understand WHY things happen

---

## DOCUMENTATION HIERARCHY

### PRIMARY AUTHORITY

**[Master Development Plan v1.0](docs/planning/master-development-plan.md)**
The definitive 10-year roadmap. All development decisions reference this document.

**What it contains:**
- 6 development stages (Stage 0 through Stage 5)
- Quality gates and success criteria
- Technical architecture foundation
- Resource requirements and timelines
- Risk mitigation strategies

**Current stage:** Stage 0 / Stage 0+1 — Physics Foundation (Year 1), with the management-layer
specification set (#27–#49) authored ahead of its implementation
**Current phase:** Implementation. Stage-0 specs all APPROVED May 18, 2026; coding began May 19, 2026;
AR-hardening sweep complete June 7, 2026; 43 specs APPROVED as of July 25, 2026. The active frontier
is `docs/tracking/path-to-playable-roadmap.md` — closing the gap between 43 approved specs and 28
implemented ones.

---

### DESIGN REFERENCE LIBRARY

These four volumes describe the FULL vision. Features are implemented incrementally across stages.

**[Master Volume I: Physics & Simulation Core](docs/planning/master-vol-1-physics-core.md)**
The 10Hz heartbeat, match logic, physical environment, and tactical micro-physics.

**Implementation timeline:**
- Stage 0-1: Core physics (ball, movement, basic AI)
- Stage 2: Match simulation with statistics
- Stage 5: Full environmental physics and 85,000 entity simulation

---

**[Master Volume II: Human Systems & Behavioral Sociology](docs/planning/master-vol-2-human-systems.md)**
Psychology, social graphs, communication, atmosphere, and personality evolution.

**Implementation timeline:**
- Stage 2: Basic morale and form
- Stage 4: Full H-Gate system, social graphs, media interactions
- Ongoing: Personality evolution and social dynamics

---

**[Master Volume III: Club Operations & Strategic Management](docs/planning/master-vol-3-club-operations.md)**
Tactical systems, recruitment, finance, governance, youth development, and training.

**Implementation timeline:**
- Stage 1: Tactical system (formations, instructions)
- Stage 2: Basic squad management and transfers
- Stage 3: Full financial systems, staff, infrastructure
- Stage 4+: Deep recruitment AI, board dynamics

---

**[Master Volume IV: Technical Implementation & Systems Engineering](docs/planning/master-vol-4-tech-implementation.md)**
Architecture, optimization, data ontology, tooling, modding support, and UI/UX.

**Implementation timeline:**
- Stage 0: Core architecture (Fixed64, DoD, event system)
- Stage 1: 2D rendering, UI framework
- Stage 2: Save system, database architecture
- Stage 3+: Advanced tooling, modding API, multiplayer infrastructure

---

### SUPPORTING DOCUMENTATION

**[Development Best Practices](docs/planning/development-best-practices.md)**
Technical wisdom extracted from project analysis. Read before starting each stage.

**What it contains:**
- Performance profiling methodology
- Testing strategies
- Technical debt prevention
- Anti-patterns to avoid
- Quality gate framework
- Optimization guidelines
- Specification writing standards
- **Specification approval checklist template**

**When to reference:**
- Before starting each development stage
- When making major technical decisions
- During code reviews
- Monthly as process reminder

---

## CURRENT STATUS

### Stage 0+1: Physics Foundation — Implementation

**Goal:** Build the physics and simulation core that all future systems depend on.

**Progress:** Implementation Phase (coding begun May 19, 2026)
**Spec Phase Started:** February 2, 2026
**Stage-0 Spec Phase Completed:** May 18, 2026 — all 20 Stage-0 specs APPROVED
**Deliverables:** 53 APPROVED specifications + 33 production assemblies in `src/`

**Summary (July 27, 2026):** `SPEC_INDEX.md` records **53 APPROVED / 0 IN REVIEW / 0 NOT STARTED — every spec in the registry is approved, and the specification phase is closed** —
the Stage-0 set of 20, plus 23 Stage-1-forward and management-layer specs (#21–#34, #37, #38, and
specs #40–#45, #49), plus the **ten promoted on July 27, 2026** (#53, #35, #46, #36, #54, #47, #48, #50, #51, #39), which emptied the pre-promotion backlog and were **approved the same day**, with their
**23 back-props filed atomically**. A non-certifying Linux dotnet compile/test CI gate (`tools/dotnet-ci/`) compiles the entire `src/` tree
and runs the full NUnit suite on every push; the quarantine list is empty.

**Note the direction of travel — it is now the project's dominant fact.** The ten approvals add
specification, not code, so **22 of the 53 APPROVED specs have no `src/` assembly at all**. *"The spec is
APPROVED"* now says nothing whatsoever about whether code exists, and that is true of **~42% of the
registry**. What remains is implementation: see `path-to-playable-roadmap.md`.

**A production match now plays.** Until July 26, 2026 every match finished 0–0 with the ball
*identically motionless for the full 90 minutes* — a closed deadlock (no motion ⇒ no reception ⇒ no
possession ⇒ no kick ⇒ no motion) that survived for months because the composed test asserted tick
count, cadence, finiteness, bounds and digest advance, all of which hold for a match in which nothing
happens. The possession bootstrap (§5.Z Phase H) closed it across five seams. Measured over six
seeds × 9 minutes: peak ball speed **16.2–17.2 m/s** (was 0.00), possession held **10.5–20.9%** of
ticks (was 0%) and changing hands **262–298 times** (was 0), with the ball reaching both penalty
areas and goals scored. Locked by the `match-engine-play-develops` acceptance scenario — every
predicate of which fails on the pre-fix engine.

**The live gap: 12 APPROVED specs have no assembly at all** — #29 Training, #31 Transfers,
and #32 Scouting, #33 Personalities/Morale, #34 Staff, #40 Finances, #41 Injuries,
plus #42 Youth, #43 Competition Structure, #44 Discipline, #45 Board, #49 Localization. (#37 Match
Analytics gained a `src/match-analytics/` T0 assembly on July 27, 2026 — value types + the
`XgLocationModel`; no engine wiring yet.) The specification frontier now runs well ahead of the code;
`docs/tracking/path-to-playable-roadmap.md` sequences the shortest path to closing it.

**Current versions:** `SNAPSHOT_SCHEMA_VERSION` **19** · `SEASON_SAVE_FORMAT_VERSION` 2 ·
`SEASON_STATE_FORMAT_VERSION` 1 · `MATCH_SAVE_FORMAT_VERSION` 1 · `WORLD_STORE_FORMAT_VERSION` 3.
Unity target **6000.4.9f1 / DX11**, recertified July 19, 2026 (`certification-platform.md` v1.4
`✅ PINNED` — both the determinism-KAT run and the FR-PO-052 perf baseline executed on the pinned
host). `src/CLAUDE.md` (v2.44) governs all C# authoring.

**Total specification output:** ~5 MB+ across 400+ files (see `file-manifest.md`)

---

### Specification Writing Schedule

**Priority 1 — Physics Foundation:**
1. ✅ Ball Physics — **APPROVED** Feb 8, 2026 (~50 pages, 44 tests, audited)
2. ✅ Agent Movement — **APPROVED** Apr 27, 2026 (~130 pages, 83 tests, audited)
3. ✅ Collision System — **APPROVED** Feb 19, 2026 (~50 pages, ~70 tests, audited; §9 back-filled Apr 26)
4. ✅ First Touch Mechanics — **APPROVED** Feb 22, 2026 (~70 pages, 72 tests, audited)
5. ✅ Pass Mechanics — **APPROVED** May 6, 2026 (audit March 25 — all 19 findings + F-A01/F-A02 resolved; re-approved May 6)

**Priority 2 — Core Gameplay:**
6. ✅ Shot Mechanics — **APPROVED** Apr 27, 2026 (~70 pages, 104 tests, audited)
7. ✅ Perception System — **APPROVED** Apr 22, 2026 (~80 pages, 92 tests; §9 v1.7 signed off)
8. ✅ Decision Tree — **APPROVED** Apr 27, 2026 (~55+ pages, draft-level quality gate; comprehensive audit candidate before implementation)
9. ✅ Fixed64 Math Library — **APPROVED** May 15, 2026 (§9 v1.0 lead-developer sign-off; §9.2 engine + gameplay owner sign-offs granted; §9.7 reciprocal `XC-016-NNN` resolved via #16 §8.3.2 documented deferral; §9.8 implementation-time deliverables remain post-APPROVED follow-ups. Implementation deferred to Stage 5 per §8.1 v0.2 stage-gating.)

**Priority 3 — Advanced Physics:**
10. ✅ Heading Mechanics — **APPROVED** May 16, 2026 (PASS-1 + OI closure + R-01..R-05 sign-off same day; ERR-010-001 resolved: `DOMAIN_TAG_HEADING = 0x16`)
11. ✅ Goalkeeper Mechanics — **APPROVED** May 18, 2026 (ERR-011-001 resolved: `DOMAIN_TAG_GOALKEEPER = 0x1D`; 44 FRs; ~79 constants)
12. ✅ Positioning AI — **APPROVED** May 18, 2026 (ERR-012-001 resolved: `DOMAIN_TAG_POSITIONING_AI = 0x17`; 47 active FRs; no `[EST]` remain)

**Priority 4 — Tactical AI:**
13. ✅ Pressing AI — **APPROVED** May 17, 2026 (all gate items resolved; R-01..R-05 signed; 26 constants)
14. ✅ Defensive AI — **APPROVED** May 18, 2026 (ERR-014-001 + ERR-014-004 resolved; 37 FRs; 26 constants)
15. ✅ Attacking AI — **APPROVED** May 18, 2026 (ERR-015-001/002/005 back-props closed; 36 FRs; 85 tests)
16. ✅ Deterministic Simulation — **APPROVED** May 14, 2026 (Tier 2 Final Approval; §9 v1.7; all §9.4.2 gates cleared; golden-vector files hkdf-sha256-kat v1.1, siphash-2-4-kat v1.1, serialize-canonical-corpus v1.0; §8.3.1 cross-spec re-audit complete; ERR-017-001 closed atomically via `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocation in §3.4 v1.0.1)

**Priority 5 — Systems Architecture (Weeks 17-20):**
17. ✅ Event System — **APPROVED** May 13, 2026 (section-files PASS 1 + PASS 2 adversarial review applied; lead-developer sign-off complete)
18. ✅ Performance Optimization Strategy — **APPROVED** May 15, 2026 (§9 v1.0 lead-developer sign-off; v1.0 `[TBD-NORMATIVE]` sweep landed across §1 / §2 / §3 / §4 / §5 / §6 / §8 / appendices against #16 APPROVED text + #19 v1.0.1 patch-revised text; KD-2 sequencing satisfied; FR count 82.)
19. ✅ Testing Strategy & Framework — **APPROVED** May 15, 2026 (§9 v1.0 lead-developer sign-off; v1.0.1 `[TBD-NORMATIVE]` sweep landed across §1 / §3 / §4 / §5 / §6 / §8 / appendices against #16 APPROVED text + #18 v0.3; KD-2 sequencing satisfied. Clears #18's last KD-2 gate.)
20. ✅ Code Standards & Style Guide — **APPROVED** May 11, 2026 (adversarial review pass-1 applied; lead-developer R-01..R-05 sign-off complete)

**Stage-1 Forward Specs (beyond the Stage-0 set of 20):**
21. ✅ Tactical Instructions — **APPROVED** Jun 20, 2026 (manager-facing tactic input layer driving #8/#11–#15; 32 FRs; G2 `[GT]` balance pass landed Jun 30, 2026; T0 data layer + T2 consumer seams + runtime activation via `MatchEngine.SetTeamTactic` + per-agent `PlayerTactic` + on-disk tactic-file loaders all landed)
22. ✅ Living World System — **APPROVED** Jun 22, 2026 (off-pitch interaction-generation layer over vol-2/vol-3 human systems; 34 FRs; season/world loop (`WorldStore`) + `InteractionTextGenerator` + deep-memory auto-cite all landed at `src/living-world/`; BackgroundTierSim + arc triggers remain documented seams pending upstream canon)
23. ✅ Dismarking & Marker-Awareness AI — **APPROVED** Jul 10, 2026 (perception-derived marking-pressure signal; `SlotComposer` dismark offset stage + Decision Tree marked-pass-target penalty wired Jul 11)
24. ✅ Scripted Build-Up Structures — **APPROVED** Jul 10, 2026 (3-zone team-relative classifier + hysteresis; `SlotComposer` build-up overlay stage wired Jul 11)
25. ✅ Positional Rotations — **APPROVED** Jul 10, 2026 (adjacency-table two-sided rotation with hysteresis; `RotationController` wired into `PositioningAITick` Jul 11)
26. ✅ Tactical Presets & AI-Manager Selection — **APPROVED** Jul 10, 2026 (preset library over #21's parameter space + AI-manager kickoff/interval decision gate and adaptation ladder; full T1–T4 stack + the engine-substrate gates it depended on — goal detection, halves model — all landed Jul 11; only the §9.2 `[GT]` balance-pass review remains)

**Management-Layer Specs (#27–#49) — authored Jul 22–25, 2026 in dependency waves per
[`management-layer-spec-roadmap.md`](docs/tracking/management-layer-spec-roadmap.md):**

| # | Specification | Approved | Implementation |
|---|---------------|----------|----------------|
| 27 | Squad / Player Data Layer | Jul 22 | 🔒 `src/player-database/` |
| 28 | Player Progression & Lifecycle | Jul 23 | 🔒 `src/player-progression/` — **T0 only** (draw-free core, not engine-wired) |
| 29 | Training System | Jul 23 | ⏳ none |
| 30 | Season & Competition Loop | Jul 22 | 🔒 `src/season-save/` — T0–T2 landed; T3 season roll open |
| 31 | Transfers, Contracts & Negotiation | Jul 23 | ⏳ none |
| 32 | Scouting & Player Knowledge | Jul 24 | ⏳ none |
| 33 | Personalities, Morale & Squad Dynamics | Jul 23 | ⏳ none |
| 34 | Staff & Backroom | Jul 23 | ⏳ none |
| 37 | Match Analytics & Statistics | Jul 22 | 🔒 `src/match-analytics/` — **T0 only** (value types + `XgLocationModel`; no engine wiring, no aggregator) |
| 38 | UI / Client Framework | Jul 22 | 🔒 `src/ui-framework/` — **T0 substrate only** (no screens, no UGUI binding) |
| 40 | Club Finances & Economy | Jul 23 | ⏳ none |
| 41 | Injuries & Medical | Jul 23 | ⏳ none |
| 42 | Youth Academy & Intake | Jul 24 | ⏳ none |
| 43 | Competition Structure | Jul 24 | ⏳ none |
| 44 | Discipline & Suspensions | Jul 24 | ⏳ none |
| 45 | Board & Ownership Dynamics | Jul 25 | ⏳ none |
| 49 | Localization & Accessibility (seam + template contract) | Jul 23 | ⏳ none |

**Approved July 27, 2026** (11-file section sets, each with a recorded PASS-1 adversarial review and its
back-props filed atomically at the flip; **none has an assembly**):

| # | Specification | FR prefix | Implementation |
|---|---------------|-----------|----------------|
| 53 | Club Infrastructure & Facilities | `FR-IN` | ⏳ none |
| 35 | Media & Press Interactions | `FR-ME` | ⏳ none |
| 46 | News, Inbox & Man-Management | `FR-NW` | ⏳ none |
| 36 | National Teams & International | `FR-NT` | ⏳ none |
| 54 | Manager Career, Reputation & Job Market | `FR-MC` | ⏳ none |
| 47 | New-Game Setup & Database Editor | `FR-ED` | ⏳ none |
| 48 | Match Presentation Depth | `FR-MP` | ⏳ none |
| 50 | Save Migration & Versioning | `FR-MG` | ⏳ none |
| 51 | Audio & Sound Design | `FR-AU` | ⏳ none |
| 39 | Steam Packaging & Release Engineering | `FR-PK` | ⏳ none |

**No design supplement now awaits promotion.** The only candidate spec without one is **#52**
(Multiplayer Transport), deliberately deferred behind the Stage-5 Fixed64 migration — see
`management-layer-spec-roadmap.md`.

**Status Key:**
- ⏳ Not started
- 📝 In progress
- 🔍 In review
- ⏸ Suspended
- ✅ Approved
- 🔒 Locked (implementation begun)

**Locked (implementation begun) — 29 of 53 approved specs:** #1–#8, #10–#19, #21–#28, #30, #37, #38.
That is the full Stage-0 physics/AI/systems stack, the tactical layer (#21, #23–#26), the living world
(#22), the squad data layer (#27), progression (#28, T0), the season loop (#30, T0–T2), match analytics
(#37, T0), and the UI framework substrate (#38, T0).

**Not implemented by design:** Fixed64 Math Library #9 (deferred to Stage 5+ per §8.1) and Code
Standards #20 (a style guide, not a coded subsystem).

**Approved but not implemented — 12 specs:** #29, #31, #32, #33, #34, #40, #41, #42, #43, #44, #45, #49.
No assembly exists for any of them.

**Plus five unnumbered assemblies** (governed by design supplements, not specs): `match-engine`
(the composition root), `match-viewer`, `match-client-core`, `match-client-unity`, `project-constants`.

**For detailed progress tracking, see:** [docs/tracking/PROGRESS.md](docs/tracking/PROGRESS.md)
**For file inventory, see:** [docs/tracking/file-manifest.md](docs/tracking/file-manifest.md)
**For cross-spec error tracking, see:** [docs/tracking/spec-error-log.md](docs/tracking/spec-error-log.md)

---

## QUALITY GATES

### Stage 0 Quality Gate (Must Pass Before Stage 1)

**Physics Quality:**
- ⬜ Pass trajectory with curve looks realistic
- ⬜ Player acceleration from standstill feels natural
- ⬜ Collisions produce sensible outcomes
- ⬜ AI makes reasonable decisions 90%+ of time

**Performance:**
- ⬜ <5ms per simulation tick (22 agents)
- ⬜ 60 FPS rendering (no stutters)

**Technical:**
- ⬜ Deterministic: Same seed = identical match
- ⬜ Cross-platform: Windows, Mac, Linux produce identical results

**Community:**
- ⬜ Positive feedback: "This looks/feels real"

**If ANY criteria fail → Do not proceed to Stage 1**

---

## DEVELOPMENT PHILOSOPHY

### Core Principles

**1. Quality Over Speed**
- No deadlines pressure
- Each stage must pass ALL quality gates
- Better to delay than ship broken foundation

**2. Specification Before Code**
- Full specification written first
- Community feedback on specs
- Code becomes implementation of spec
- **20 weeks of specs before ANY Stage 0 coding**

**3. Incremental Complexity**
- Start simple, prove it works
- Add complexity in stages
- Each stage is commercially releasable

**4. Community Involvement**
- Share progress openly
- Gather feedback early
- Build audience before launch

**5. Sustainable Pace**
- 40 hours/week maximum
- No crunch (10-year journey)
- Breaks between stages

### Key Architectural Decisions (Established During Specs 1-8)

- **Parameter-based physics:** No type enums (KickType, ShotType, PassType eliminated). Physics systems receive velocity/spin vectors; the Decision Tree supplies intent parameters.
- **Possession is agent state, not ball state:** BallState has no PossessingAgentId field (ERR-008).
- **Write interfaces only when both sides are specified:** Avoids phantom interface proliferation.
- **Struct-based zero-allocation architecture:** All hot-path data uses value types.
- **10Hz heartbeat for tactical decisions; 60Hz for physics:** Decision Tree runs at heartbeat rate; physics systems run per-frame.
- **Fatigue convention:** 0 = fully rested, 1 = fully fatigued (no exceptions).

---

## PROJECT STRUCTURE

```
Soccer-Manager-Pro/
├── CLAUDE.md                           [AI behavioral rules — read first]
├── README.md                           [This file]
├── Assets/ Packages/ ProjectSettings/  [Unity project shell — editor 6000.4.9f1, DX11]
├── docs/
│   ├── planning/                       [Master volumes I–IV, master development plan, best practices]
│   ├── design/ui-mockups/              [Non-normative UI visual reference — not on any build path]
│   ├── specs/
│   │   ├── SPEC_INDEX.md               [Canonical registry — 43 spec folders, all APPROVED]
│   │   └── <43 spec folders>/          [See SPEC_INDEX.md for the number ↔ folder map]
│   └── tracking/
│       ├── PROGRESS.md                 [Schedule and milestone tracking]
│       ├── SPEC_INDEX-adjacent logs:
│       │   ├── spec-error-log.md       [Cross-spec architectural errors]
│       │   ├── file-manifest.md        [Authoritative file inventory]
│       │   └── fix-manifest-pass-mechanics.md
│       ├── Roadmaps (meta-planning — design no system, open no spec):
│       │   ├── management-layer-spec-roadmap.md  [Which specs to author, in what order]
│       │   └── path-to-playable-roadmap.md       [Which code to land, in what order]
│       ├── certification-platform.md   [Pinned host/engine tuple] + cert-run-runbook.md
│       └── *-design.md                 [42 design supplements — see note below]
├── src/                                [33 production assemblies — coding begun May 19, 2026]
│   ├── CLAUDE.md                       [Coding guide — read before writing code]
│   ├── Physics:    ball-physics, agent-movement, collision-system, first-touch,
│   │               pass-mechanics, shot-mechanics, heading-mechanics, goalkeeper-mechanics
│   ├── Mechanics:  positioning-ai (+#23/#24/#25), pressing-ai, defensive-ai, attacking-ai
│   ├── AI:         decision-tree, perception-system
│   ├── Foundations: deterministic-sim, event-system, project-constants
│   ├── Data/loop:  player-database (#27), player-progression (#28), season-save (#30),
│   │               tactical-instructions (#21 + #26), living-world (#22), match-analytics (#37, T0)
│   ├── Client:     ui-framework (#38), match-viewer, match-client-core, match-client-unity
│   ├── Infra:      performance-optimization (#18), testing-strategy (#19)
│   └── match-engine/                   [Composition root — not a numbered spec]
└── tools/
    ├── dotnet-ci/                      [Non-certifying Linux compile/test gate]
    ├── unity-ci/  perf-harness/  spec-stress/
    └── budget-auditor.py, select-seed.py, round-resolution-fit.py, run-perf-local.sh
```

**Assembly names do not reliably match spec folder names.** #27 lives in `player-database`, #28 in
`player-progression`, #30 in `season-save`, #37 in `match-analytics` (folder `match-analytics-statistics/`), #38 in
`ui-framework`; #23/#24/#25 live inside `positioning-ai` and #26 inside `tactical-instructions`. Consult the
assembly map in [`CLAUDE.md`](CLAUDE.md#repo-structure) rather than inferring from the folder name.

**On design supplements.** `docs/tracking/*-design.md` is a governance class of its own: a converged,
adversarially-reviewed design note that either precedes promotion to a numbered spec, or permanently
governs a surface that is deliberately *not* a numbered spec (`match-engine-design.md` for the
composition root is the canonical example). A supplement is not a spec — it has no `SPEC_INDEX.md` row
and confers no approval status.

Note: Fixed64 Math Library (#9) has no `src/` tree — implementation deferred to Stage 5+. Code Standards (#20) is a style guide, not a coded subsystem.

---

## NEXT IMMEDIATE STEPS

### Outstanding (as of July 26, 2026)

**Specification phase:** ✅ **CLOSED** — `SPEC_INDEX.md`: **53 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**
The July 27, 2026 wave (#53, #35, #46, #36, #54, #47, #48, #50, #51, #39) was promoted **and approved**
the same day, with sign-off granted and its 23 back-props filed atomically. The only candidate without a
spec is **#52** (Multiplayer Transport), deliberately deferred behind the Stage-5 Fixed64 migration.

**Nothing on the specification side is outstanding. Everything outstanding is implementation** — and the
gap is large: 22 of the 53 approved specs have no assembly.

**The critical path is now `path-to-playable-roadmap.md`** — the shortest route to a build a person can
sit down and play, against the PM-1 (playable match) / **PM-2 (playable season — the objective)** /
PM-3 (playable career) ladder. Phase A (season spine) is in progress:

1. ✅ **A1–A4 landed** — #30 T0 season value types, T1 save/restore path, the league bootstrap
   (one seed → an N-club league, no authored data), and T2 the day-advance loop + round-resolution
   model (a 380-fixture season resolves in milliseconds against the ≥ 16 h the real engine would need).
2. ✅ **A4b landed Jul 26** — the possession bootstrap. **This is the item that unblocked PM-1:** it
   closed ERR-030-014, under which a production match had never once put the ball in motion.
3. ⏳ **A4a — round-resolution calibration corpus.** Unblocked by A4b but not yet run: re-run the
   ~33-minute Step-0 pilot, then (only if the squad-strength extremes separate) the ~1.4 h corpus and
   the parameter fit. The three `[GT]` round-resolution parameters currently ship **provisional and
   explicitly not fitted** — football-plausible rather than engine-matched.
4. ⏳ **A5 — #30 T3 season boundary roll.** Phase A's exit is PM-2-sim: a full 38-round season
   simulating headlessly, saving and resuming byte-identically, and rolling into a second season.
5. ⏳ **The 13 unimplemented approved specs** (#29, #31–#34, #37, #40–#45, #49) — ~30–60k lines of
   unwritten code at this project's realised spec→code ratios.

**Known behavioural defect (not blocking play):**
6. ⏳ **The foul heuristic issues ~7 red cards per 9 minutes** — every player would be dismissed well
   inside a full match. Invisible until Phase H made matches actually play. Deliberately not fixed
   inside a correctness pass: the levers are `[GT]` and setting them needs a stated foul-rate target
   and a measurement pass. **The most visible remaining unrealism in a played match.**

**Client / presentation (Track C — can run in parallel, must not block Phase A):**
7. ⏳ **UI screens.** #38's T0 substrate exists; no screens and no UGUI binding. Of the screens the
   July-25 mockups cover, only Tactics and Squad have built data behind them — the rest are gated on
   #29/#31/#32/#34/#40, none of which have any `src/`.
8. ⏳ **Interactive Unity client — now the shipping UI, and the next thing to build.** Roadmap B6 was
   reversed by owner decision on Aug 3, 2026: the product ships the full Unity client, not the
   web-hosted viewer. The browser client (`src/match-client-web/`) is retained as the **host-free
   reference harness** — the only surface exercising the read/playback/intent loop in CI — and is the
   current floor, but it is not the target. P0–P3, the head-less half of P6, **and P4a (the render
   model — coordinate adapter, IFAB marking catalogue, roster, per-agent and ball draw states)** are
   done; **P4b (the Unity binding) is next, on the pinned host**, then P5 and P6's on-host half. P4 was
   split on the standing rule for this surface: **keep logic out of `MonoBehaviour`s** — the CI gate
   cannot compile `match-client-unity` and never will, so every decision lives in gate-compiled
   `match-client-core` / `ui-framework`, and P4a is that rule made into a phase. See
   `docs/tracking/interactive-unity-client-design.md` §12.

**Operations / housekeeping:**
9. ⏳ Push the approval tags (HTTP 403 branch-protection); run the squash-merge precondition check in
   `CLAUDE.md` OPEN ISSUES before `git push --tags`.
10. ⏳ Re-verify the `tools/dotnet-ci` shim's `netstandard2.1` / `LangVersion 9.0` claims against a real
    Unity 6000.4.9f1 install (documented behaviour says they hold; confirming needs the install).

**Resolved since the last README update (July 14 → July 26):**
- ✅ **Unity 6 recertification COMPLETE** (Jul 19) — both the determinism-KAT run and the FR-PO-052 perf
  baseline executed on the pinned host; `certification-platform.md` v1.4 back to `✅ PINNED`. This
  closed items 6 and 7 of the previous list, plus the native MXCSR live-mode gate (Jul 22).
- ✅ **Snapshot deserialize / restore** — Phases 1–3 complete: the reader, distinct-squad re-projection
  (#27 T3), the on-disk match save file, and the unified season save.
- ✅ **17 management-layer specs authored and approved** (#27–#34, #37, #38, #40–#45, #49) in
  dependency waves, Jul 22–25.
- ✅ **A production match plays** (Jul 26) — see CURRENT STATUS above.

---

## ARCHIVED DOCUMENTS

The following documents have been superseded and moved to `/Archive/`:

**1. Focused_Tactical_Sim_Plan.md**
- **Reason:** Proposed 18-month simplified scope, contradicts Master Plan's 10-year full simulation vision
- **Superseded by:** Master Development Plan v1.0
- **Date archived:** December 30, 2025

**2. CRITICAL_ANALYSIS_Development_Risks.md**
- **Reason:** Valid warnings extracted into docs/planning/development-best-practices.md
- **Superseded by:** Development Best Practices
- **Date archived:** February 3, 2026

**3. Architecture_Plan_Critique.md**
- **Reason:** Conflicts resolved in Master Development Plan
- **Superseded by:** Master Development Plan v1.0
- **Date archived:** December 30, 2025

These remain available in `/Archive/` for historical reference but should NOT be followed.

---

## GETTING STARTED

### For New Team Members (Future)

1. Read **CLAUDE.md** (understand AI rules and conventions)
2. Read **Master Development Plan v1.0** (understand the roadmap)
3. Read **Development Best Practices** (understand the process)
4. Read Master Volumes I-IV (understand the vision)
5. Review current stage specifications (start with Ball Physics #1)
6. Review **docs/tracking/spec-error-log.md** (understand cross-spec issues)
7. Set up development environment (guide TBD)

### For Current Development (Solo)

1. ✅ Master Plan understood
2. ✅ Best Practices documented
3. ✅ Specification progress tracker created
4. ✅ All 20 specifications APPROVED (May 18, 2026) — spec phase COMPLETE
5. ✅ `src/CLAUDE.md` coding guide created (May 19, 2026; v1.8)
6. ✅ Ball Physics #1 — initial implementation at `src/Core/Physics/Ball/`
7. ✅ Agent Movement #2 — implementation + tests at `src/agent-movement/`
8. ✅ Collision System (#3), First Touch (#4), Pass Mechanics (#5), Shot Mechanics (#6) — all coded, AR-clean
9. ✅ Heading Mechanics (#10) — 25 source files, coded and AR-clean (May 28)
10. ✅ Goalkeeper Mechanics (#11) — 36 source files, coded and AR-clean (May 28)
11. ✅ Perception System (#7) — 14 source files, coded and AR-clean (May 29)
12. ✅ Decision Tree (#8) — 36 files, coded and AR-clean (May 29)
13. ✅ Positioning AI (#12) — 20 files, coded and AR-clean (May 29)
14. ✅ Pressing AI (#13) — 21 files, coded and AR-clean (May 29)
15. ✅ Defensive AI (#14) — 19 files, coded and AR-clean (May 29)
16. ✅ Attacking AI (#15) — 24 files, coded and AR-clean (May 29)
17. ✅ Deterministic Simulation (#16) — 23 files (21 .cs + 2 asmdef), coded and AR-clean (May 29)
18. ✅ Structural deviation resolved: Ball Physics relocated to `src/ball-physics/` (May 30, 2026)
19. ✅ Event System (#17), Performance Optimization (#18), Testing Strategy (#19) — coded, AR-clean
20. ✅ Non-certifying Linux `dotnet-ci` gate — full `src/` tree compile + 1,165+ NUnit tests on every push (Jun 12–13, 2026)
21. ✅ Match Engine composition root — Phases A–F complete, wiring all subsystems into the deterministic-sim 7-phase tick pipeline (Jun 28, 2026)
22. ✅ Tactical Instructions (#21) APPROVED + fully landed incl. per-agent tactics, balance pass, disk loaders (Jun 20–30, 2026)
23. ✅ Living World System (#22) APPROVED + season/world loop, ArcEngine, text generation landed (Jun 21 – Jul 3, 2026)
24. ✅ Specs #23–#26 (Dismarking, Build-Up Structures, Positional Rotations, Tactical Presets) APPROVED + wired (Jul 8–11, 2026)
25. ✅ Match Engine — goal detection, halves/full-time model, and the complete match-flow model (throw-ins, corners, goal kicks, fouls/cards, offside, substitutions) landed (Jul 11–14, 2026)
26. ⏳ Unity 6 recertification — engine target bumped to 6000.4.9f1/DX11 (Jul 13, 2026, docs-only); real cert run on the new tuple outstanding (needs pinned host access)
27. ⏳ FR-PO-052 certified perf baseline — real Stopwatch harness landed (Jul 13); first cert run on the pinned tuple still outstanding, blocked on item 26

---

## CONTACT & COMMUNITY

**Developer:** Solo developer with AI assistance
**Project Start:** December 29, 2025
**Current Phase:** Implementation (Stage 0+1)

**Community Channels (To be established):**
- Discord: TBD (Stage 1)
- Dev Blog: TBD (Stage 0 Month 6)
- Reddit: r/TacticalDirector TBD (Stage 1)
- Twitter: TBD (Stage 1)

---

## VERSION CONTROL DISCIPLINE

**Critical Habit:** Commit after each specification completion.

```bash
# After completing each specification:
git add docs/specs/[spec-folder]/
git add docs/tracking/PROGRESS.md
git commit -m "Complete [SpecName] Specification v1.0"
git push

# After approval:
git commit -m "Approve [SpecName] Specification - Ready for implementation"
git tag "spec-[specname]-v1.0-approved"
git push --tags
```

**Tag Status:**
- `spec-ball-physics-v1.0-approved` — created locally Apr 26, 2026; awaits push
- `spec-agent-movement-v1.0-approved` — created locally Apr 27, 2026; awaits push
- `spec-collision-system-v1.0-approved` — created locally Apr 26, 2026 (back-fill of Feb 19 sign-off); awaits push
- `spec-first-touch-v1.0-approved` — created locally Apr 26, 2026; awaits push
- `spec-pass-mechanics-v1.0-approved` — created locally May 6, 2026 (re-approved); awaits push
- `spec-event-system-v1.0-approved` — created locally May 13, 2026; awaits push
- `spec-shot-mechanics-v1.0-approved` — created locally Apr 27, 2026; awaits push
- `spec-perception-system-v1.0-approved` — created locally Apr 26, 2026; awaits push
- `spec-decision-tree-v1.0-approved` — created locally Apr 27, 2026 (draft-level approval); awaits push
- `spec-fixed64-math-v1.0-approved` — created locally May 15, 2026; awaits push
- `spec-testing-strategy-v1.0-approved` — created locally May 15, 2026; awaits push
- `spec-performance-optimization-v1.0-approved` — created locally May 15, 2026; awaits push

> **Tag push blocked**: `git push origin --tags` returns HTTP 403 from the current remote endpoint. After this branch merges to main with privileged credentials, run `git push origin spec-ball-physics-v1.0-approved spec-collision-system-v1.0-approved spec-first-touch-v1.0-approved spec-perception-system-v1.0-approved` from a context that allows tag creation. The tags are annotated and point at the merge commit (or its branch-tip predecessor if fast-forward).

**Why this matters:**
- Preserves specification history
- Enables rollback if implementation reveals design flaws
- Documents approval timeline
- Supports future audit trail

**Frequency:**
- After each specification section completed
- After each review cycle
- After final approval
- Weekly (minimum) during specification phase

---

## VERSION HISTORY

**v1.37 — July 27, 2026 (later same day)**
- **All ten specs advanced `IN REVIEW → APPROVED`.** Lead-developer R-01..R-05 sign-off granted; the
  **23 back-props filed and RESOLVED atomically with the flips** (`spec-error-log.md` v1.47), per each
  spec's own promotion-pipeline step 6.
- CURRENT STATUS: **53 APPROVED / 0 IN REVIEW / 0 NOT STARTED.** The specification phase is closed; the
  only candidate without a spec is **#52**, deliberately deferred behind the Stage-5 Fixed64 migration.
- **Landing the back-props together exposed a defect no single approval could have seen:** #30's pinned
  day-advance tick order was **not implementable as written** — `ERR-030-007` had been filed **twice**
  (#42's academy step, #32's scouting step, at two separate approvals), leaving two step 7s, two step 8s
  and an orphaned `AdvanceDay` line in a sequence six approved specs cite by number. Reconciled in a new
  #30 §3.3.1, which also resolved a conflict between two of this wave's own back-props.
- Three entries correct **approved contracts** rather than pointers: ERR-048-001 (a contradiction between
  two MUSTs inside #48 that would have surfaced as an assembly cycle), ERR-045-002 (a MUST delegating the
  sacking decision to a spec containing no such rule), ERR-033-003 (a per-producer field replaced with a
  producer-agnostic one, filed jointly by two specs).
- **No code, no gate run, and no format version bumped today** — three entries are spec-text-first with a
  named future bump, and #54's is decided to combine with #45's queued one.
- The gap restated as the project's dominant fact: **23 of 53 APPROVED specs have no `src/` assembly**.

**v1.36 — July 27, 2026**
- **Ten design supplements promoted to full 11-file section sets at `IN REVIEW`** — #53 Club
  Infrastructure, #35 Media & Press, #46 News/Inbox & Man-Management, #36 National Teams, #54 Manager
  Career & Reputation, #47 New-Game Setup & DB Editor, #48 Match Presentation Depth, #50 Save Migration
  & Versioning, #51 Audio & Sound Design, #39 Steam Packaging & Release. **The pre-promotion backlog is
  now empty.** Docs only: no code, no `src/` change, no gate run.
- CURRENT STATUS: **43 APPROVED / 10 IN REVIEW / 0 NOT STARTED** (was 43 / 0 / 0).
- Each promotion carries a recorded section-file **PASS-1 adversarial review + fix pass** and an AR-2
  sweep to convergence, and stops at `IN REVIEW` deliberately — **sign-off is a human authority and is
  not self-grantable**, per each supplement's own promotion pipeline.
- **The finding that generalises:** three supplements proposed `ERR-` ids that had already been filed,
  because nothing cross-checks a *proposed* id against the error log — reassigned at promotion and
  recorded. A supplement's id is a suggestion, not a reservation.
- Restated the gap in its new form: **23 of the 53 specs with a folder have no `src/` assembly at all**
  (the 13 APPROVED ones plus all ten new), so a spec existing still says nothing about a consumer
  existing.
- `management-layer-spec-roadmap.md` → v0.7: header status note only; the wave blocks were **left
  intact** as the record of why each spec sits where it does.

**v1.35 — July 26, 2026**
- Root-doc reconciliation against the actual repo state; no code, spec, or tracking-doc change.
- CURRENT STATUS re-based: 43 APPROVED / 0 IN REVIEW / 0 NOT STARTED (was 26); 29 production
  assemblies; `SNAPSHOT_SCHEMA_VERSION` 15 → 18 corrected; Unity 6 recertification and the
  FR-PO-052 perf baseline moved from outstanding to resolved (both completed July 19, 2026).
- Recorded the Phase-H possession bootstrap: a production match now plays (ERR-030-014 closed).
- Added the 17 management-layer specs (#27–#49, approved July 22–25) with per-spec implementation
  status, and the seven design supplements awaiting promotion (#35, #36, #46, #47, #48, #50, #51).
- **Stated the implementation gap plainly: 13 APPROVED specs have no assembly at all.**
- PROJECT STRUCTURE rebuilt from the real tree; added the assembly-name ≠ spec-folder-name warning
  and the design-supplement governance note.
- NEXT IMMEDIATE STEPS re-based on `path-to-playable-roadmap.md`; added the known ~7-red-cards-per-
  9-minutes foul-heuristic defect.

**v1.0 — December 30, 2025**
- Initial README creation
- Documentation hierarchy established
- Stage 0 specification schedule defined
- Archived obsolete planning documents

**v1.1 — February 3, 2026**
- Updated specification progress tracking
- Added Ball Physics Specification status (in progress)
- Added specification approval checklist reference
- Added version control discipline section
- Updated archive status (CRITICAL_ANALYSIS moved)
- Added progress tracker reference

**v1.2 — February 15, 2026**
- Updated to Week 2 status
- Ball Physics marked as APPROVED (Feb 8, 2026)
- Agent Movement marked as IN REVIEW (~130 pages, 83 tests)
- Added file-manifest.md reference
- Updated project structure to reflect current files
- Changed quality gate checkboxes to unchecked (pending implementation)
- Added pending git tags section
- Updated next steps for Week 2-3

**v1.3 — March 24, 2026**
- Updated to Week 8 status (was 6 weeks stale)
- Corrected spec numbering to canonical PROGRESS.md order
- 4 approved, 3 in review, 1 in progress (was: 1 approved, 1 in review)
- Added Decision Tree #8 as IN PROGRESS
- Added comprehensive audit process to status notes
- Added Key Architectural Decisions section
- Added spec-error-log.md reference to documentation hierarchy and project structure
- Updated pending git tags
- Updated Getting Started checklist
- Updated next steps for Week 8

**v1.4 — April 21, 2026**
- Updated to Week 12 status
- Pass Mechanics (#5): APPROVED → SUSPENDED (audit March 25, 2026)
- Decision Tree (#8): IN PROGRESS → IN REVIEW (Sections 1–9 + appendices all drafted)
- Progress summary corrected: 3 approved, 1 suspended, 4 in review
- Project structure updated to reflect current repo layout (docs/ paths)
- Tracking document links corrected to docs/tracking/ paths
- Added ⏸ SUSPENDED to status legend
- Added Decision Tree to pending git tags
- Updated Getting Started checklist
- Updated next immediate steps to Week 12 actions

**v1.5 — April 26, 2026**
- Removed scheduling milestones (passion project, no deadline)
- Perception System (#7) status: IN REVIEW → APPROVED (Apr 22, 2026; §9 v1.7 signed off)
- Approval count corrected to 4 per §9-file evidence (with Collision System #3 status disagreement flagged)
- Next Immediate Steps rewritten around outstanding sign-offs and documentation debt rather than week-12 actions
- Added explicit reference to ~74 stale spec-number references in body text (measured)
- Added pointer to spec-error-log.md rebuild and CLAUDE.md dangling references

**v1.6 — April 27, 2026**
- Lead developer sign-off pass: Agent Movement (#2), Shot Mechanics (#6), Decision Tree (#8) all APPROVED.
- Stage 0 spec count: 7 APPROVED, 1 SUSPENDED, 0 IN REVIEW, 12 NOT STARTED.
- Decision Tree (#8) approved at draft-level quality gate; comprehensive audit candidate before implementation.
- Renumber sweep marked complete (Apr 26).
- Fixed64 migration deferred to Stage 5+; Stage 0 uses float + state snapshots.
- Pass Mechanics (#5) is the only Priority 1–2 spec still outstanding (re-sign-off after March 25 audit).

**v1.8 — May 12, 2026**
- Code Standards & Style Guide (#20) reclassified NOT STARTED → APPROVED (May 11, 2026).
- Testing Strategy & Framework (#19) reclassified NOT STARTED → IN REVIEW (May 12, 2026). Approval gated by KD-2 preconditions (#16 Tier 2 APPROVED, #18 outline-level draft, all `TBD-NORMATIVE` tags resolved).
- Stage 0 spec count: **9 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 8 NOT STARTED.**
- Project Structure block updated to break out testing-strategy/ and code-standards/ folders.

**v1.7 — May 6, 2026**
- Deterministic Simulation (#16) reclassified to IN PROGRESS (May 2, 2026).
- Fixed64 Math Library (#9) reclassified to IN REVIEW (May 6, 2026) after Pass 2 adversarial critique fixes landed.
- Stage 0 spec count: 7 APPROVED, 1 SUSPENDED, 1 IN REVIEW, 1 IN PROGRESS, 10 NOT STARTED.
- Project Structure block updated to reflect approved sign-offs and current statuses; #9 and #16 broken out from the "..." catch-all.
- Next Immediate Steps refreshed to May 6 reality; resolved documentation-debt items checked off (spec-error-log rebuild, file-manifest reconciliation).
- ERR-016-002 (EntityId no-reuse cross-spec back-propagation) added to outstanding documentation debt.

**v2.7 — May 29, 2026**
- Positioning AI (#12): 20 files (19 .cs + 1 asmdef) coded at `src/positioning-ai/`; AR-1+AR-2+AR-3 clean.
- `src/CLAUDE.md` coding guide updated to v1.17 (positioning-ai/ tree expanded).
- `docs/tracking/PROGRESS.md`, `README.md`, `CLAUDE.md` updated with #12 implementation status.

**v2.6 — May 29, 2026**
- Perception System (#7): 14 source files coded and AR-clean (AR-1: 3M+3L; AR-2: 3L) at `src/perception-system/`.
- Decision Tree (#8): 36 files (35 .cs + 1 asmdef) coded at `src/decision-tree/`; AR-1 (2H+3M+4L) + AR-2 (full sweep clean).
- `src/CLAUDE.md` coding guide updated to v1.16 (decision-tree/ tree expanded).
- `docs/tracking/PROGRESS.md` and `README.md` updated with #7 and #8 implementation status.

**v2.5 — May 28, 2026**
- Heading Mechanics (#10): 25 source files coded and AR-clean at `src/heading-mechanics/`.
- Goalkeeper Mechanics (#11): 36 source files coded at `src/goalkeeper-mechanics/`; AR-1 (5H+1M) + AR-2 (2M) adversarial review cycles complete.
- `src/CLAUDE.md` coding guide updated to v1.14 (goalkeeper-mechanics/ tree).
- `docs/tracking/file-manifest.md` updated with Heading Mechanics and Goalkeeper Mechanics source file inventories.
- `docs/tracking/PROGRESS.md` updated with implementation status for #10 and #11.

**v2.4 — May 25, 2026**
- All 20 specifications APPROVED. Stage 0 specification phase COMPLETE (May 18, 2026).
- Specs #10–#15 APPROVED May 16–18, 2026 (Heading Mechanics, Goalkeeper Mechanics, Positioning AI, Pressing AI, Defensive AI, Attacking AI).
- Coding phase begun May 19, 2026. `src/CLAUDE.md` coding guide created; at v1.8 as of May 25.
- Ball Physics (#1): initial implementation at `src/Core/Physics/Ball/` — 8 source files + 3 test files.
- Agent Movement (#2): initial implementation at `src/agent-movement/` — 16 source files.
- Known structural deviation: Ball Physics at `src/Core/Physics/Ball/` vs spec-canonical `src/ball-physics/` — fix pass planned.
- README updated to reflect implementation phase, full spec schedule, and actual `src/` structure.

**v2.3 — May 15, 2026 (later same day still)**
- Performance Optimization Strategy (#18) — APPROVED. §9 v1.0 lead-developer sign-off granted; v1.0 `[TBD-NORMATIVE]` sweep landed across §1 / §2 / §3 / §4 / §5 / §6 / §8 / appendices (60 instances) against #16 APPROVED text + #19 v1.0.1 patch-revised text. KD-2 sequencing satisfied.
- Stage 0 spec count: **14 APPROVED, 0 SUSPENDED, 0 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.** Priority 5 spec set fully complete; only Priority 3+4 AI specs (#10–#15) remain in the spec phase.

**v2.2 — May 15, 2026 (later same day)**
- Testing Strategy & Framework (#19) — APPROVED. §9 v1.0 lead-developer sign-off granted; v1.0.1 `[TBD-NORMATIVE]` sweep landed across §1 / §3 / §4 / §5 / §6 / §8 / appendices (51 citation-qualifier instances resolved against #16's APPROVED text and #18's IN REVIEW v0.3 surface). KD-2 sequencing satisfied.
- Stage 0 spec count: **13 APPROVED, 0 SUSPENDED, 1 IN REVIEW (#18), 0 IN PROGRESS, 6 NOT STARTED.** Priority 5 spec set now 75% complete.
- Downstream effect: clears Spec #18's last KD-2 gate for its own `IN REVIEW → APPROVED` advancement.

**v2.1 — May 15, 2026**
- Fixed64 Math Library (#9) — APPROVED. §9 v1.0 lead-developer sign-off granted; §9.2 engine + gameplay owner sign-offs granted; §9.7 reciprocal `XC-016-NNN` resolved via #16 §8.3.2 documented comparator-glossary deferral (Interface Design Principle). Stage 0 effect: none — Fixed64 binds to Stage 5+ per §8.1 v0.2.
- Event System (#17) §1.0.1 patch revision — `[CROSS-PENDING]` → `[CROSS]` promotion of `DOMAIN_TAG_EVENT_LEDGER` across all consuming sections + Appendix B literal-value inlining (`0x15`). ERR-017-001 FULLY RESOLVED.
- Stage 0 spec count: **12 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.** Priority 1–2 spec set fully complete.

**v2.0 — May 14, 2026 (later same day)**
- Deterministic Simulation (#16) — Tier 2 APPROVED. §9 v1.7 sign-off; all §9.4.2 gates cleared (golden-vector files complete; §8.3.1 cross-spec re-audit complete; §9.3 sign-offs granted with Stage-0 host-platform-pin caveat); ERR-017-001 closed atomically via `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocation in #16 §3.4 v1.0.1.
- Performance Optimization Strategy (#18) — IN PROGRESS → IN REVIEW. Section files v0.1 → v0.2 → v0.3 (PASS-1 + PASS-2 adversarial reviews; ERR-018-002..018 closed; FR count 82). Advancement to APPROVED gated on #19 reaching APPROVED per KD-2.
- Stage 0 spec count: **11 APPROVED, 0 SUSPENDED, 3 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.**
- Project Structure block: #16 broken out as APPROVED; #18 broken out as IN REVIEW; catch-all reduced to #10–#15.
- Next Immediate Steps fully rewritten around May 14 reality.

**v1.9 — May 13, 2026**
- Event System (#17) — APPROVED May 13, 2026. Section files authored from `outline-detailed.md` v1.1; section-files PASS 1 (3 H / 5 M / 12 L) and PASS 2 (2 H / 6 M / 7 L) adversarial critiques both resolved; all section files at v0.3; lead-developer sign-off granted.
- Pass Mechanics (#5) status corrected from stale SUSPENDED to APPROVED (re-approved May 6, 2026).
- Stage 0 spec count: **10 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 7 NOT STARTED.**
- Priority 5 schedule row 17 updated from ⏳ NOT STARTED to ✅ APPROVED.
- Project Structure block: #17 broken out as APPROVED; catch-all updated to Specs #10–#15, #18.
- Tag list: `spec-pass-mechanics-v1.0-approved` corrected; `spec-event-system-v1.0-approved` added.

**v3.0 — June 29, 2026 (consolidates a month of unrecorded changes, May 30 – June 29)**
- Structural fix landed: Ball Physics relocated `src/Core/Physics/Ball/` → `src/ball-physics/` (May 30, 2026).
- AR-hardening sweep completed across all 18 then-coded sections (June 7, 2026); Stage-0 host-platform pin landed in `certification-platform.md` (June 7, 2026).
- Non-certifying Linux `dotnet-ci` gate landed (June 12–13, 2026): full `src/` tree compile + 1,165+ NUnit tests on every push; first-ever full compile surfaced 8 previously-dead build surfaces (notably ERR-017-002, the illegal-overload EventBus production assembly); 30-test quarantine fully adjudicated and cleared.
- Comprehensive Decision Tree (#8) audit (AR-2, June 11, 2026): 3H+11M+9L resolved — production assembly had never compiled; away-team zone/press/line-depth asymmetry bugs fixed.
- Pass Mechanics (#5) AR-9/AR-10 fix pass (June 11, 2026): test suite had never compiled since v1.1; 3 functional defects fixed.
- **Match Engine** composition root (`src/match-engine/`, unnumbered) built end-to-end: Phases A–F complete (June 19–28, 2026) — boot, full snapshot serialization (`SNAPSHOT_SCHEMA_VERSION` 8), Physics/Resolve/AI-phase wiring through all five tactical AI specs, Events-phase possession-changed pipeline, and the Phase F capstone closed-loop scenario.
- **Tactical Instructions (#21)** — second Stage-1 forward spec — APPROVED June 20, 2026; T0 data layer, T2 consumer seams (DecisionTree/Pressing/Positioning/Defensive/Attacking), and runtime activation (`MatchEngine.SetTeamTactic` + in-code `TeamTacticConfig`) landed through June 29, 2026.
- **Living World System (#22)** — third Stage-1 forward spec — APPROVED June 22, 2026; T0 scaffolding landed at `src/living-world/`.
- Spec count: **22 APPROVED** (20 Stage-0 + 2 Stage-1 forward specs), 0 outstanding in any other status.
- Project Structure, Locked list, Next Immediate Steps, and Getting Started checklist all corrected to match current `src/` and `docs/specs/` contents (this README had not been substantively updated since v2.7 / May 29, 2026 despite header dates being hand-advanced).

**v4.0 — July 14, 2026 (consolidates two weeks of unrecorded changes, June 30 – July 14)**
- **Specs #23–#26 promoted from design supplements to full 11-file spec sets, adversarially reviewed, and APPROVED** (July 8–10, 2026): Dismarking & Marker-Awareness AI (#23), Scripted Build-Up Structures (#24), Positional Rotations (#25), Tactical Presets & AI-Manager Selection (#26). Spec count: **26 APPROVED**, 0 outstanding in any other status.
- **#23/#24/#25/#26 implementation wiring landed** (July 10–11, 2026): T0 data-layer scaffolding, then the #24 build-up + #23 dismark `SlotComposer` stages, the #25 `RotationController`, the #23 Decision Tree marked-pass-target penalty, and #26's full T1–T4 stack (preset→config projection, kickoff/interval decision gate, kickoff scoring, mid-match adaptation ladder). All default-behaviour-neutral at each dial's identity value.
- **Match Engine — engine substrate landed** (July 11, 2026): goal detection (scoring, `GoalAwardedEvent`, centre-spot restart) and the match-length/halves model (`MATCH_TICKS_TOTAL`, `HALF_TIME_BOUNDARY_TICK`). `SNAPSHOT_SCHEMA_VERSION` 13 → 14. This closed #26's engine-substrate gates, activating its half-time trigger and live goalDiff/clock ladder inputs.
- **Unity engine version bumped 2022.3.62f1 → Unity 6000.4.9f1, DX11** (July 13, 2026) — documentation-only; no recertification performed. `certification-platform.md` Status reverted `✅ PINNED` → `⏳ RECERT REQUIRED`, re-blocking `FR-DS-009-GATE`, `FR-PO-052`, the §7.5 D1 test-runner pin, and `EnvironmentFingerprint` until a real cert run executes against the new tuple. A real Stopwatch-based perf harness (`StopwatchPerfHarness` / `MatchEngineCapstonePerfHarness`) landed the same day, superseding the old synthetic stub — still non-certifying pending pinned-host access.
- **Tactical Instructions (#21) Stage-1 leftovers landed** (June 30, 2026): per-agent `PlayerTactic` config surface, the §5.6/G2 `[GT]` balance pass, the §3.4 defensive-line depth recompute, and on-disk team/player tactic-file loaders. #21 has no remaining Stage-1 follow-ups.
- **Living World System (#22) season/world loop landed** (July 2–3, 2026): `WorldClock`/`WorldLoop`/`MemoryStore`/`ColdStore`, `ArcEngine` + `ActiveSetMembership`, `InteractionTextGenerator` with deep-memory auto-cite, and the `WorldStore` composition root (the KD-10 season-store prerequisite). BackgroundTierSim summaries and arc trigger evaluators remain documented seams, deliberately unbuilt pending upstream canon.
- **Presentation layer: minimal match viewer landed** (July 2, 2026): `src/match-viewer/` records a real `MatchEngine` run and exports a self-contained HTML canvas replay. Observer-neutral and digest-locked; not a determinism-pinned wire format.
- **Match-flow model completion landed** (July 14, 2026): throw-ins, corners, goal kicks, fouls/cards (with sent-off tracking), offside, substitutions, half-time break, and full-time end — the last major gap in the match engine (previously only kickoff + goal-restart existed). New `RestartResolver.cs`, `OffsideEvaluator.cs`, `SubstitutionReason.cs`; three new Tier A events. `SNAPSHOT_SCHEMA_VERSION` 14 → 15. Design doc and code both adversarially reviewed to convergence.
- Header "Last Updated" chain, CURRENT STATUS, Specification Writing Schedule, Locked list, Project Structure, and Next Immediate Steps all reconciled to match current `src/` and `docs/specs/` contents (this README had drifted since v3.0 / June 29, 2026 despite the header being updated piecemeal through July 10).

---

## LICENSE

**TBD** — To be determined before Stage 2 commercial release

---

**Remember:** This is a 10-year project. Quality over speed. Build it right.

**Current Focus:** All 26 approved specs have active `src/` implementations. Active frontier: Unity 6 recertification (pinned-host access required) and the #26 `[GT]` balance-pass review — both non-blocking follow-ups, not new feature work.
