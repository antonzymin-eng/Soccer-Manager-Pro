# CLAUDE.md — Change Log

> **Created:** July 31, 2026
> **Purpose:** The `**Last Updated:**` entry chain formerly carried in the header of
> `CLAUDE.md`. Newest first; each entry records what landed, what was measured, and
> what was deliberately not done.
> Split out of `CLAUDE.md` on July 31, 2026. Content is **verbatim** — entries were moved, never edited, reordered, or deduplicated.

**Appending a new entry:** add it at the top of the chain below and re-label the
previous newest entry `**Last Updated (prior):**`. The chain is the record — do not
break it, and do not edit historical entries.

---

> **Last Updated:** August 5, 2026, later same day (**#29/#41 gate run — PASSED. Both assemblies
> compiled for the first time; all 67 of their tests executed and passed.** PR #299, CI run 394, head
> `ddbbe58`. Build 0 errors; `TrainingSystem.Tests` 27/27, `InjuriesMedical.Tests` 40/40, 0 skipped in
> either; whole-tree gate PASSED with the quarantine empty, `MatchEngine.Tests` 420/430 unchanged.
>
> **The PR had to be unblocked first, and the reason is worth recording:** #299 was conflicted against
> `main`, and GitHub cannot construct the merge ref for a conflicted PR, so the `pull_request` workflow
> never fired at all. The gate was not slow or flaky — it had never been *asked* to run. Merging `main`
> resolved five chain-append conflicts (both branches prepending to "Last Updated" chains; both sides
> kept everywhere) plus two genuine collisions, since the branches forked at `2.60`/`v1.56` and then
> allocated the same numbers independently: `CHANGELOG-src` 2.61–2.64 (main's kept, this branch's
> renumbered 2.65–2.68) and `spec-error-log` v1.57 (main's ERR-008-020 kept; ERR-041-002/003 became
> v1.58/v1.59).
>
> **What the run retires:** every "the suite locks X" claim across the T0 landing and five adversarial
> review passes was, until now, a claim about code that had never been compiled — the never-compiled
> surface trap this file's own history records. No fix was needed to get green. Beyond compilation it
> confirms Appendix B day by day, #41 §3.6 term by term, the keyed-draw separation, and AR pass 5's
> hand-computed occurrence-probability baseline (231/0/431 per-mille), which had been derived by
> mirroring the C# in Python against a tree that could not be built.
>
> The authoring environment still has no .NET SDK — the installer is still 403 at the agent proxy,
> re-checked here — so CI remains the only compiler for this work.)

> **Last Updated (prior):** August 5, 2026, later same day (**Adversarial review over the #29/#41 T0 landing —
> 2 High, 4 Medium, 4 Low, all fixed; converged on pass 2.** Both Highs were the same shape: a design
> that made a silent wrong answer reachable, guarded by a test that could not fail.
>
> **H-1 — one contract value, two config keys, and a lock wired to nothing.** `InjuryRiskMax` was
> declared `[GT]` in BOTH catalogues, under `[training-system]` and `[injuries-medical]`. #41 §3.4
> passes #29's `RiskScore` through with weight 1 and compares it against a draw whose denominator is
> derived from that ceiling, so setting one key without the other rescales every occurrence probability
> and #29's clamped maximum stops meaning "certain". The equality test written to catch exactly that
> passed unconditionally, because the gate leaves `GameplayConfigHolder` unbound and both sides return
> their fallback. Fixed by re-tagging #41's row `[CROSS]` and mirroring #29's — **ERR-041-003**.
>
> **H-2 — a focus command that could write another club's player.** `TrainingStep.SetFocus(int[] ids,
> TrainingState[] states, …)` took the pair as separate arguments and checked only that the lengths
> matched — and every club in a generated league has the same squad size, so passing club A's ids with
> club B's states resolved the player against A and wrote B. No exception, wrong player, wrong club,
> and it would have persisted at T1. The command moves onto `TrainingSchedule.TrySetFocus`, which binds
> the pair once at construction, so there is no argument a caller can supply to reach it. Locked by a
> test that fails against the old signature.
>
> **The Mediums:** a `MedicalModifier` gate that rejected zero but not negative (a negative recovery
> speed one-days a Serious injury; a negative occurrence multiplier clamps risk to zero and silently
> ends injuries forever — and #34 is the declared future producer of both); an F1 coherence check that
> structurally could not see a negative `RecoveryRemaining`, because "not recovering" and "healthy"
> look identical to an iff; **four tests that could not fail** (asserting the identity function is the
> identity, asserting a pure function is pure, and comparing two values that are equal by construction
> — the documented repo trap, with FR ids on them claiming coverage they did not provide); and the one
> cross-assembly contract in the whole landing — #29's `ComputeInjuryRisk` feeding #41's
> `AssembleRiskScore` — having **no test at all**.
>
> **Pass 2 caught two regressions in pass 1's own fixes**, which is the reason the loop re-reads
> everything rather than the diff: the replacement for one tautological test was *itself* tautological
> (`in` parameters cannot be mutated, so "this read does not mutate" is a compile-time guarantee), and
> the new seam test asserted something **false** — that #29's saturated maximum reaches #41's ceiling.
> It does not, and finding out why is the more useful half: **both specs mitigate on the same three
> physical attributes**, so a robust player is priced down twice and #41 always subtracts again on top
> of #29's already-mitigated value. Spec-faithful, since each spec mandates its own term, but it
> entangles the two `[GT]` tables and it means "maximum risk" never means certain occurrence. Recorded
> as an explicit assertion so the balance pass inherits the fact instead of rediscovering it.
>
> Pass 3 over the full surface of both assemblies surfaced no new High or Medium. **Still no gate run**
> — no .NET SDK, installer blocked by network policy — so every fix above is reviewed and unexecuted.)

> **Last Updated (prior):** August 5, 2026 (**#41 Injuries & Medical T0 landed — and #29 Training System T0
> with it, because #41 could not be built without it.** The task was the next spec after #29 in code
> implementation order; the roadmap's Phase D orders that as D2 #29 → D3 #41, and #41 §4.1 requires
> a reference to `TacticalDirector.TrainingSystem` for the one type it reads — `InjuryRiskContribution`,
> #29's already-published risk scalar (FR-TR-017 / FR-MD-009). That assembly did not exist. So #29 T0
> landed as the declared prerequisite rather than as a half-built stub, and the pair went in together:
> **two new host-free assemblies, `src/training-system/` and `src/injuries-medical/`, taking `src/` from
> 31 to 33** and the assembly-less-APPROVED-spec count from 22 down to 20.
>
> **#29 T0** — `TrainingFocus`, `TrainingState` (+ the `Create`-not-`default` sentinel discipline),
> `TrainingSchedule` as a genuine read-only VIEW over per-player focus rather than a stored copy
> (FR-TR-003), `CoachingModifier`, `InjuryRiskContribution`, `TrainingViewModel`, the four `TrainingStep`
> entry points (§3.1–§3.4) and the FR-TR-023 `SetFocus` command, plus the Appendix A catalogue. Appendix
> B's Fitness week is reproduced day by day as a test, including its `ProjectMatchEntryFatigue = 0.23`.
> No RNG anywhere: `_RESERVED_0x21_` / ordinal 83 stay reserved (KD-6).
>
> **#41 T0** — `InjurySeverity`, `InjuryState`, `MatchLoad`, `MedicalModifier` (explicit `Identity`, and
> `default` fails loud — its zero is ×0 risk and a divide-by-zero recovery scale), `MedicalViewModel`,
> `MedicalStep` (§3.1–§3.4: the recovery-then-draw day step, the keyed occurrence draw, the same-draw
> severity bucketing, the risk assembly), and the Appendix A catalogue. §3.6's worked example is pinned
> term by term — the risk assembly's 2900, the `draw 1500 ⇒ Minor` bucketing, the 7-day Minor tier — and
> the robustness table is calibrated so `mean 14 ⇒ 400` is exact rather than approximately reproduced.
>
> **Two findings, both filed** (`spec-error-log.md` v1.58). **ERR-041-002** is the consequential one and
> it is ERR-030-012's twin, reached independently from the same constraint: **#41 §2.2/§3.1 call
> `rng.DrawKeyed(...)` on `DeterministicRngService`, and no such method exists.** #16 exposes only the
> branch-safe reservation trio, whose draw value is keyed on an `ActionOrdinal` the service increments
> inside `Reserve` — nothing accepts a caller-supplied ordinal. The one shape that *is* implementable
> against today's API is cursor-positioned, which KD-1 of the same spec forbids: FR-MD-007 serializes no
> cursor precisely because every draw must be reproducible from `(playerId, worldDay, purpose)` alone.
> Resolved the way #30 resolved it — a local keyed SplitMix64 derivation, the
> `RoundResolutionModel.FixtureKey` precedent — so `AdvanceMedicalDay` takes `ulong worldSeed` in place
> of the service and registers no stream. **ERR-041-001 closes with it**: `DOMAIN_TAG_INJURIES_MEDICAL =
> 0x2A` lands in `DeterministicSimConstants` at that first draw site; `SubsystemOrdinals.InjuriesMedical
> = 92` is deliberately **not** allocated, because an ordinal with no registered stream behind it is the
> zero-consumer phantom FR-LW-031 forbids.
>
> **Not done, and named rather than implied:** T1 (both save codecs and the `SeasonSaveCodec`
> composition) and T2 (the #30 tick-order wiring, the availability read into squad selection, the
> FR-MD-025 / FR-TR-025 roster-membership handoff) are untouched. Both assemblies are inert — nothing
> constructs them, so the season loop is byte-identical to before this landing. #29's `ComputeTrainingInput`
> returns `TrainingInput.Neutral` on both branches, because #28's type still has no fields to populate;
> the deep branch is a marked seam, not a magnitude invented ahead of its consumer.
>
> **NO GATE RUN.** The authoring environment has no .NET SDK and the network policy blocks the
> installer (`builds.dotnet.microsoft.com` → 403 at the proxy), so 17 production files and 5 test files
> across two new assemblies are **written and never compiled** — precisely the defect class
> `tools/dotnet-ci` exists to catch. Every "the suite locks X" claim in this entry is a claim about test
> code that has not executed. First CI run on push is the real gate.)
> **Last Updated (prior):** August 5, 2026, later same day (**PR #298's first gate run: one failure — the
> snapshot-coverage guard, correctly — and two execution-verified confirmations.** The failure:
> `DecisionTree_InstanceFieldCount_MatchesCapturedSet`, the reflection lock that pins DecisionTree's
> field count so cross-tick state cannot silently skip the snapshot. ERR-008-020's
> `_allAgentAttributes` made it 11; the landing had made (and documented) the exclusion decision —
> injected dependency, host re-wires at boot/restore, the `_saveDispatch` class — but never updated
> the guard's ledger. Fixed: count 10 → 11 + the field recorded in the excluded class; no production
> change. **The confirmations, both by execution for the first time:** (1) `MatchEngine.Tests` 420
> passed / 0 failed — `RoundTrip_KeeperSubstitutedOntoOutfieldSlot_IsDeterministic`, red on `main`
> since the W1 merge, passes under the restore-resync fix; (2) all nine ERR-008-020 lane-model locks
> and the engine wiring lock pass on their first-ever compile. Once this push goes green, the PR
> carries a gate strictly better than `main`'s (which remains red until merged). Prior entry below.)

> **Last Updated (prior):** August 5, 2026 (**CI fix — main went red at the W1 merge, and the cause was the
> W1 AR-2 fix's own restore claim being false.** `RoundTrip_KeeperSubstitutedOntoOutfieldSlot_IsDeterministic`
> failed on `main` at `ba04d49` (and on both prior W1-branch runs): digest diverged at tick 151, the
> first post-restore tick. The v1.60 occupant-change fix argued `_gkAgentIds` needs no schema bump
> because it is "reconstructed rather than serialized, so restore re-derives it and sees no change" —
> half true. The boot-time derivation runs against the DEFAULT goalkeeper-flag layout;
> `DeserializeWorldState` then overwrites the flags with the SAVED layout; and whenever the two
> differ (this test substitutes a bench keeper onto an outfield slot at tick 50, saves at 150), the
> first post-restore `RefreshGkAgentIds` misreads the flag delta as a live occupant change and
> `ResetSlot`s #11 keeper state that was itself just restored — a wipe the uninterrupted run does
> not perform at that tick, because its reset fired back at the substitution and its state evolved
> since. Fixed in `MatchEngine.cs` v1.63: the keeper resolution extracted to `ResolveGkAgentId`,
> and `RestoreFromSnapshot` gains **step 3b — `ResyncGkAgentIdsAfterRestore`**, re-deriving the map
> from the restored flags **without** reset, since restored #11 per-slot state already belongs to
> the restored occupant. The live-path reset — the actual substitute-inheritance fix — is unchanged;
> this restores exactly the restore-transparency that existed before the reset was introduced. All
> restore paths route through the one factory (`MatchSession.RestoreFrom` → `MatchSaveManager` →
> `RestoreFromSnapshot`), so one fix covers all. Verification is the already-failing CI test; not
> runnable locally (no .NET SDK). `gk-rush-trigger-design.md` v1.4 supersedes the v1.3 claim.
> Prior entry below.)

> **Last Updated (prior):** August 4, 2026, latest same day (**ERR-008-020 adversarial review — 2 Medium,
> 1 Low, all fixed; pass 2 clean.** Both Mediums are lessons in what a lock is worth when it doesn't
> execute the thing it claims to lock. **M-1:** the landing's P5-pivot test asserted "an average
> defender counts exactly 1.0" through the *null-attribute-view guard* — the ability computation it
> exists to pin was never run for an average defender anywhere in the suite, so the spec's "MIN/MAX
> midpoint MUST equal 1.0" invariant was enforced by nothing and a `[GT]` retune could break the
> whole pivot-on-baseline argument silently. Now locked twice: a computed-path pivot (Anticipation
> 10 + Pace 11, whose normalised mean is 0.5 *exactly*) and a constants midpoint invariant. **M-2:**
> the engine wiring had no detector, and the model's null fallback is silent *by design* — dropping
> the one `SetAllAgentAttributes` boot call would revert every match to attribute-blind lane pricing
> with every test green, the wiring-backlog gate-level-dormancy class this repo documents as its top
> defect shape. Now `DecisionTree.HasSquadAttributeView` + an engine `TestOnly` sweep +
> `MatchEngineSquadTests` construction lock. **L:** the elite-vs-poor discrimination margins were a
> hardcoded 0.15; now derived from the constants (half the true `(MAX−MIN)/DIVISOR` gap), so a
> legitimate retune shrinks the margin instead of false-failing the suite. Production delta is two
> read-only accessors — no digest, schema, RNG, or draw-order surface. Nine locks now cover the
> model across two suites. **Gate still NOT runnable here (no .NET SDK); CI on this push is the
> first compile.** Prior entry below.)

> **Last Updated (prior):** August 4, 2026, latest same day (**ERR-008-020 — the doctrine's template fix
> landed: the pass lane learns who the defender is, and a false "FIXED" claim is corrected.** First
> fix under `football-judgment-proxy-review.md` §6, exactly as converged: #8 §3.1.3.3's binary 0.8 m
> `is_interceptor` corridor — 2 cm of defender position stepped `PassLaneScore` by 0.33, and no
> defender attribute entered the judgment, so a Pace/Anticipation 1/1 defender priced a lane
> identically to a 20/20 one — becomes a continuous per-opponent threat weight: linear falloff
> (core 0.4 m [GT], zero at 1.2 m [GT], **ramp centred on the old cliff so integrated threat is
> preserved and the neutral verification rows reproduce exactly** — doctrine P5, locked by test) ×
> defender Anticipation+Pace ability (0.6–1.4 [GT], average ⇒ exactly 1.0) read through the passer's
> **Vision as discrimination fidelity** (P2: `perceived = 1 + fidelity × (true − 1)`, floor 0.2 [GT]
> — a Vision-1 passer reads everyone as near-average, which IS the pre-fix engine; §3.2.2's Vision
> term untouched, P3 no double-count). Plumbing: `DecisionTree.SetAllAgentAttributes` boot seam (the
> `SetMatchSeed` pattern) carries the engine's live `_dtAttrs` reference into `DecisionContext` —
> substitutions visible through it; null view ⇒ ability-neutral, never an exception. Spec §3.1.3.3
> rewritten (v1.3, worked example + verification table), shot lane §3.1.4.3 deferred with a scope
> note (owner call), `spec-error-log.md` → v1.57, 6 `OptionGeneratorTests` locks incl. the away-side
> mirror. No `SNAPSHOT_SCHEMA_VERSION` change (the view is an injected dependency, excluded from
> `CaptureState`), no new RNG stream / domain tag / draw site, no draw-order change; digests move
> for any match with a PASS candidate near a defender, as intended. **Blast radius recorded:** every
> tick-window/rate-band instrument may shift on its seeds and cannot be checked here; the A4a
> round-resolution fit needs its Step-0 re-check after the first measured corpus; FR-PO-052 adds no
> allocation, only a few float ops per candidate. **Gate NOT run — no .NET SDK in this environment;
> nothing compiled or executed; CI's dotnet gate on this push is the first compile.** **Separately,
> a record correction:** the review file's §2 claim that ERR-008-019 (the long-shot cliff) was
> "FIXED … gate green" is **false against both branches** — no log entry exists, the cliff is live
> in `UtilityWeights.cs`/`UtilityScorer.cs` and the spec, and no branch carries a fix; the prior
> session recorded a landing that never happened (the fabricated-claims trap). Review §2/§5
> corrected, the finding re-opened (33 open again), the id soft-reserved. Prior entry below.)

> **Last Updated (prior):** August 4, 2026, latest same day (**Football-judgment proxy review — the remediation
> doctrine (§6) landed, doc-only.** The review file stops being identification-only: an owner session
> converged the general approach before any of the 33 open findings gets a fix, and §6 records it so
> each fix cites a principle instead of re-arguing the method up to 33 times. The frame is the owner's
> **recognition → decision → execution** pattern — which is already the #7 → #8 → Mechanics/Physics
> pipeline — with its five failure modes made into binding mitigations (stages degrade assessment
> quality, never delete options; attributes enter a judgment once; decisions commit intent, not a
> frozen coordinate — a spot where a teammate *will arrive* is a legitimate target, a lock on his
> current position is not; coordination is signalled, not mind-read; calibration targets the chain).
> Five principles: **P1** continuous-never-cliff (the ERR-008-019 shape, covering the pattern-(b)
> findings), **P2** skill as *discrimination fidelity* — `perceived = neutral + fidelity × (true −
> neutral)`, so a low-skill assessor sees everyone as average, which IS today's attribute-blind engine
> (graceful degradation, no RNG in assessment), **P3** the attribute ownership ledger (Vision owns
> on-ball recognition, Anticipation off-ball/predictive; **no new "play recognition" attribute** —
> owner call), **P4** intent as a first-class object (pass-to-space, run-intent signals on the event
> bus, set-piece routine targets — mechanism-class, design supplement first), **P5** calibrate
> end-to-end, pivot on today's baseline, defer real `[GT]` tuning per KD-W1. The **template fix is
> chosen but NOT implemented**: #8 §3.1.3.3 pass-lane interceptors become `distance_falloff ×
> perceived(Anticipation+Pace)` through the passer's Vision fidelity; §3.2.2's Vision term is
> untouched (it rewards vision generally, fidelity owns risk discrimination — no double-count); the
> §3.1.4 shot lane deliberately deferred. Also recorded: the **pairwise playing-familiarity gap** —
> #33's social graph and #2's per-player Stage-4 hooks exist, but nothing pairwise-on-pitch; the
> natural third input to the run-signal handshake; candidate supplement. The review file also finally
> enters `file-manifest.md` — it was never recorded at creation.)

> **Last Updated (prior):** August 4, 2026, latest same day (**W1 adversarial review pass 2 — 1 High, 1
> Medium, 3 Low.** The High is a seam defect, and it is the other half of pass 1's own fix rather than
> a new subject. #11 indexes every per-keeper array by `gkIndex`, which is the **team** (KD-1); this
> engine keys identity by **roster slot**. Those agree right up until the occupant of the keeper slot
> changes — a keeper is sent off, and the reserve keeper comes on in a *different* slot, which is the
> only shape the sequence can take because `SubstitutePlayer` refuses the dismissed slot itself. The
> path is live from `ManagerCommand`, not hypothetical. Nothing inside #11 can observe the handover,
> so the substitute inherited the slot whole: state, dive scratch, hold stamps, and a `RushIntent`
> whose target was **locked at commit (KD-15) for a player who has left the pitch** — which the
> `Set → Rushing` row then launched him at, making his first act on the field a sprint to a point
> nobody chose for him. Pass 1's sent-off filter is what made this reachable: it changed the dismissed
> keeper's slot from self-resolving (#11 kept ticking him to the end of his run) to frozen
> indefinitely, and frozen state is precisely what gets inherited — a ghost sprint traded for a stale
> one. Fixed by giving #11 a `ResetSlot` and having `RefreshGkAgentIds` detect a change of occupant,
> so the slot's state belongs to whoever holds it. **No new engine state and no
> `SNAPSHOT_SCHEMA_VERSION` bump**: `_gkAgentIds` *is* the previous value, and it is reconstructed
> rather than serialized, so a restore re-derives it and sees no change. The constructor's sentinel
> loop now runs through `ResetSlot` too — a fresh slot defined once instead of in a pair that must
> agree (§5.Z.12). The Medium is a gap in my own last pass: the sent-off fix shipped with **nothing
> asserting it**, so its return would have been silent; both locks are now in, mirrored home and away.
> Three Lows recorded not fixed in `gk-rush-trigger-design.md` §7 — a comment in #11 that W1 falsified,
> a redundant `_attrs` write on the rush path, and a state-machine comment that states the opposite of
> the row order it describes. **Gate still NOT run — no .NET SDK in this environment, so none of this
> has been compiled or executed.**)

> **Last Updated (prior):** August 4, 2026, same day (**W1 adversarial review pass 1 — 1 High, 4
> Medium, 4 Low, all fixed.** The High is the one worth naming: `RushArmed` bounded how LONG a run the
> keeper would commit to and never how SHORT, so a keeper standing on the ball he had just swept
> re-armed — and that is the *ordinary* end state of a sweep, not an edge case, because §5.Z.15/16
> bars the keeper from collecting the loose ball he ran to. Traced through the real call order the
> result is a zero-length rush every third tactical tick (`Set` → commit → `Anticipate` → `Rushing` →
> target reached → `Recovering` → `Set`, the cooldown bypassed because `UpdateBaselineSlot` feeds the
> keeper his own position), a keeper pinned to a dead ball, a `RushPhase.Reached` published every
> cycle, and — the part that bites — never enough `Anticipate` dwell for §3.3.6's dive gate, so the
> save path is suppressed while it runs. **`ERR-011-009` ended the stall; without this guard it became
> a churn.** The fix reuses #11's own arrival radius rather than minting a twelfth `[GT]`: the commit
> test and the arrival test must agree, and §5.Z.12's rule is that a pair has two places that must
> agree where a mirror has one. Mediums: a keeper **sent off mid-rush kept sprinting** (the engine's
> freeze is `_commands = Stop`, which governs the movement integration only, while #11's `Rushing`
> branch writes position *after* it — `RefreshGkAgentIds` now filters `_isSentOff`, which is what
> `NotifyKeeperOfShot`'s own comment already assumed); `RushCommitFatiguePenaltyM` is **structurally
> dead**, since all four `ToGoalkeeper` call sites hardcode `fatigue: 0f`, so it is recorded
> do-not-calibrate in both the spec and the design note rather than entering the calibration pass
> looking live; **no test proved the keeper physically leaves his line** through a real engine (the
> composed locks stopped at `GkState == Rushing`, which is equally true of an engine whose rush
> position write-back is dropped — the #11 v1.4 H-2 defect), now fixed by a displacement test plus the
> re-arm lock; and `GkHeadingIntentSource`'s v1.1 history row still documented the **rejected**
> last-man model as current. Lows: the epsilon renamed `GK_RUSH_DEGENERACY_EPSILON` because it guards
> three dimensionally different quantities, a `+4 [GT]` header corrected to 5, an orphaned header
> continuation folded back, and the cross-catalogue `GkRushCommitment > RushCommitThreshold` invariant
> — which keeps the whole trigger from going silently dead — now **asserted** instead of merely
> commented. **Still not measured: no .NET SDK in this environment, so no gate run and no numbers.**)

> **Last Updated (prior):** August 4, 2026, latest same day (**WIRING BACKLOG W1 LANDED — the goalkeeper
> comes off his line for the first time, and the spec defect that discovery surfaced
> (`ERR-011-009`).** `GoalkeeperMechanics.CommitRushIntent` had **zero production callers** since it
> was written, so every one-on-one this engine has ever played was a stationary keeper on his line —
> the whole rush subsystem below the trigger (dispatch, `Rushing → OneOnOne → Smothered`, abort
> reasons, telemetry, snapshot serialization) was built, tested and dead. `MatchEngine.TryCommitRushIntents`
> is that caller, over a new pure `GkHeadingIntentSource.RushArmed`. **The predicate is built from one
> sentence: a keeper comes out to REDUCE THE SHOOTING ANGLE.** So the only thing that keeps him home is
> a team-mate already **goal-side** of the ball, inside the corridor the shot would travel down — a
> defender merely *chasing* the carrier, or wrestling him for the ball, narrows nothing and does not
> stop him. And **how far** he comes out is not an engine constant but the keeper's own attributes,
> #11 §3.7.0's `ComputeRushCommitDistanceM` over `OneVsOne` / `Composure` / fatigue: ~9 m for a timid
> keeper, ~16 m for an aggressive one at 20% fatigue. (This is the corrected model — the first cut used
> a last-man test, refusing the rush whenever any team-mate was nearer the ball, which keeps the keeper
> home in exactly the situation he exists for. Caught at owner review, before any measurement.) For a
> loose ball the locked target is an **intercept-race solve** rather than the ball's current position,
> because KD-15 locks the target at commit and a rolling ball is not where it was; the solve
> self-guards, since a clearance outrunning the keeper has no positive root. Skipped whenever
> `SaveArmed` holds for the same keeper — **a ball driving at the goal is a save, not a rush** — or a
> shot would send the keeper charging out while the ERR-011-007 commit-lead gate still held the dive,
> regressing the whole §5.Z.17–§5.Z.22 save pipeline. Deliberately **not** routed through the Decision
> Tree: `ActionType.SAVE = 7` is the last ordinal that fits the 3-bit composure-noise field, so a RUSH
> action would force the same digest rebaseline that defers W9, turning the board's cheapest large
> lever into its most expensive item. **No new engine state** — #11's own already-serialized
> `_rushIntentActive` is the per-episode latch, read through new `GetState`/`HasActiveRushIntent`
> accessors rather than duplicated (two latches with different lifetimes for one episode is precisely
> ERR-011-002's dive-at-nothing) — so **no `SNAPSHOT_SCHEMA_VERSION` change**. **What the wiring
> surfaced, first: `ERR-011-010`.** §3.7's state entry delegated the rush DECISION to Decision Tree #8,
> which has no goalkeeper model and structurally cannot acquire one — so the condition belonged to
> nobody, which is the whole reason the method sat uncalled for ten weeks while everything below it was
> built, reviewed and tested. And because the "when" was delegated, the spec never said what a keeper is
> *deciding* either, a gap no call site can fill by guessing. New §3.7.0 takes the decision back (the
> §3.3.6 move) and states it normatively on both halves: only a goal-side body is cover, and the
> distance is his own attributes. `OneVsOne` is consumed for the commit DECISION only — FR-GK-024's
> closed-form constraint on the 1v1 SAVE formulas is untouched. **And second: `ERR-011-009`.**
> #11 §3.1.1 gives `Rushing` three exits and `OneOnOne` two, and for a
> LOOSE ball **none of them can fire** — the 1v1 and smother triggers are false by construction with
> no ball possessor, F-08 needs one, and §3.7.2's update converges on the locked target and stops
> without overshooting — so a keeper who swept a loose ball would have stood over it in `Rushing` for
> the remainder of the match. Everything else anticipated the completion (`RushPhase.Reached` has been
> in the enum since v0.1, never published; §3.7.3 reserves `AbortReason.AttackerBeatGK`, also
> unreachable); only the table that adjudicates state had no row. Fixed spec-and-code in the same
> commit: two §3.1.1 rows, the §3.7.2 terminating check, `[GT] RUSH_TARGET_REACHED_RADIUS_M`, and the
> `Reached` event finally emitted — a **completion, not an abort**, ranked below contact, F-08 and the
> 1v1 trigger, so FR-GK-018 / KD-15 are untouched. **The honest headline, and a deliberate break with
> every §5.Z entry above: NOTHING HERE HAS BEEN EXECUTED.** The authoring environment has no .NET SDK
> and the agent proxy denies `builds.dotnet.microsoft.com`, so `tools/dotnet-ci/run-gate.sh` did not
> run and the new `GkRushDiagnosticTests` instrument is written-and-unrun. There are **no pre/post
> numbers**, and none were invented; the gate result for this landing is whatever the GitHub
> `dotnet-compile-test` job reports, and no claim that a suite enforces anything may be cited before
> then — that is this project's own never-compiled-surfaces hazard, and it is being named rather than
> stepped in. Eleven new `[GT]`s — six in #11's catalogue (the §3.7.0 commit-distance model plus
> `RushTargetReachedRadiusM`) and five in the engine's (`GkRushMaxInterceptS`, `GkRushMaxBallHeightM`,
> `GkRushCommitment`, and the two cover-geometry dials) — are all first plausible numbers, not fitted
> ones; under KD-W1 they are **new dials for a dead surface**, not retunes, and they are the
> calibration pass's input. Note where they live: how far the keeper comes out is **#11's**, because it
> is a property of the keeper; the cover geometry and the guards are the **engine's**. Files: `MatchEngine.cs` v1.58,
> `GkHeadingIntentSource.cs` v1.1, `MatchEngineConstants.cs` v1.28, `GoalkeeperMechanics.cs` v1.11,
> `GoalkeeperStateMachine.cs` v1.7, `GoalkeeperConstants.cs` v1.5, `GoalkeeperRushDispatch.cs` v1.1,
> new `GoalkeeperRushTests.cs` /
> `GkRushTriggerTests.cs` / `GkRushDiagnosticTests.cs`, new owner doc `gk-rush-trigger-design.md`,
> spec #11 §3 v0.7, `spec-error-log.md` v1.56, `match-engine-wiring-backlog.md` v1.1. Next in the
> backlog sequence: **C1**, the `InPoss` gate — the largest starvation on the board. Prior entry
> below.)

> **Last Updated (prior):** August 4, 2026, latest same day (**MERGE — `main` into the P4a branch, and a version-number collision resolved.** PR #295 was un-mergeable. Three conflicts, **all in the "newest entry at top" chains** — this file, `CHANGELOG-src.md`, `file-manifest.md` — which is the expected class when two branches each prepend, and **no source conflict at all**: `main` had moved on in `decision-tree` and `match-engine` only, which the client assemblies do not touch. Resolved by **interleaving both sides chronologically by commit time** rather than picking a winner, so every entry from both branches survives verbatim, one `Last Updated:` per chain, everything below it `(prior)`. **The collision worth knowing about:** both branches independently allocated `CHANGELOG-src.md` **v2.53**. `main` owns it (close-chance §5.Z.24, already in trunk), so this branch's four entries renumbered up by one — P4a landing 2.53→**2.54**, AR pass 1 →**2.55**, tilted-view →**2.56**, AR pass 2/3 →**2.57** — in both the header chain and the VERSION HISTORY table. Nothing outside that file cited them (grep over `docs/`, `src/`, `README.md`). **A consequence to leave alone rather than "fix":** 2.54 is dated August 3 and sits above 2.53 dated August 4. The table is keyed on version, and version numbers record the order things land in trunk, which is not the order they were written — renumbering by date would mean renumbering an entry already merged. **One edit to content that arrived from `main`, made deliberately and recorded here rather than silently:** its chain tagged close-chance `v2.52` while its own VERSION HISTORY table gives that entry 2.53; left alone the merge would have put two `v2.52` tags in one chain — the duplicated-version hazard this project has a standing trap entry for — so the tag now agrees with the table it contradicted. **Deliberately NOT touched:** `main` carries a repeated §5.Z.23 entry in both changelog chains (both tagged v2.51), and the VERSION HISTORY table has six long-standing duplicate version ids (2.9, 2.10, 2.34, 2.36, 2.42, 2.44) — identical on both branches, so neither was introduced here. Deleting a historical entry during a conflict resolution is what these files forbid, and renumbering 150 rows of merged history is not a merge's job. **Verified mechanically, not by reading:** every line of both parents is accounted for in all three merged files — the only absences are the label relabels, the four renumbers, one deduplicated header, and five manifest rows where this branch's text is a superset of `main`'s (the `LiveAgentCue` row gained `IsGoalkeeper`; the client section headers moved P3→P4a and P4→P4b). Every P4a source file is byte-identical pre- and post-merge. No `src/` change in this merge.)
>
> **Last Updated (prior):** August 4, 2026, latest same day (**P4a ADVERSARIAL-REVIEW PASS 2 — 1 High, 4 Medium fixed; run over the tilted-view revision's own output.** **H-1, and it is the pointed one:** `PitchCameraRig` decided where the camera goes and how it is angled, but said nothing about **how much of the pitch it sees** — so P4b would have chosen a field of view inside the `MonoBehaviour`. A framing decision, in the one place the CI gate cannot compile, sitting inside the deliverable whose entire purpose is keeping decisions out of there (§12 rule 1, the P4a/P4b split). `PitchCameraPose` gains `FieldOfViewDegrees` — the binding now assigns position, look-at and field of view, and picks nothing — `MatchClientConstants` gains `CameraVerticalFovDegrees`, and because two individually-legal dials can pair into a camera whose lowest ray never meets the ground, the bound is `tilt + fov/2 < 90` rather than two range checks. `PitchCameraRig.GroundExtentAlongTilt` attaches a number to the framing: near and far reach of visible ground, **deliberately asymmetric**, since a tilted camera sees a trapezoid and asserting symmetry is the mistake the test guards. **M-1:** §5-P4b instructed *both* cameras in a single bullet — the new rig placement and, in the same sentence, the deleted orthographic one — while the very next bullet said the orthographic assumption was wrong; the roadmap's B8 row carried only the stale half. The live instruction sheet for the next phase on the critical path contradicted itself. **M-2:** `PitchMarking`'s doc still sent the render skin to `ToView`, which would stand every marking upright in the world XY plane instead of laying it on the turf — and `ToView`/`ToPitch` turned out to have no production caller left at all after the revision (`ToView` was `ToWorld` with the height dropped, and the inverse a click needs is a ray intersection), so both are deleted and their tests re-anchored. **M-3:** `CameraLateralOffsetM` was the only camera dial with no validation, and it lands directly in the camera's world position — a non-finite value put the camera nowhere while every assertion about the aim point still passed. **M-4:** the tilted-view revision never appended version-history rows to `MatchClientConstants.cs` (v1.4) or `MatchRenderProjection.cs` (v1.2), so each file's newest row described constants and a `HeightScale` it no longer had, and three tracking documents cited versions the files themselves did not claim. The `// Modified:` date check did not catch it, because the previous row carried the same date. `match-client-core` 129 → 135; the two new locks verified non-vacuous by breaking them (symmetric ground extent fails 2, a fov dropped from the pose fails 1). **Full dotnet gate: PASSED, 0 failures** (whole tree green, 30 suites; match-client-core 129 → 135, match-engine 368 unchanged). **The sweep after the fixes found one more Medium, so this pass is NOT converged** — `PitchMarkingKind.Rectangle` still documented corner ordering as *not* guaranteed and told consumers to re-normalise, which is the exact contract pass 1's H-1 reversed: `PitchMarking.cs` was fixed then and the enum sitting beside it was not, so two files stated opposite contracts for one field, and the enum is the one a renderer switching on `Kind` reads first. Fixed; the guarantee is test-locked by `EveryRectangleArrivesWithItsCornersNormalised`, so the docs cannot silently drift from the code again. **Pass 3 then re-read the whole P4a surface and surfaced no High and no Medium — the loop is converged.** Two Lows fixed: `PitchCameraPose`'s header and class summary still described it as two values, and a test comment credited the wrong assertion with guarding the static-init-order defect. That second one is worth stating plainly, because the correction is counter-intuitive: asserting `CameraTiltDegrees > 0` does **not** catch a declaration reorder. By the time any test reads the field, static init has finished and it holds its real value whichever order it ran in. What catches it is re-evaluating the invariant itself on the finished values — a pair that is genuinely invalid fails there regardless of what the boot check saw. The guard was already present and correct; only the comment beside it was wrong. **Full dotnet gate on the converged tree: PASSED, 0 failures** (30 suites; match-client-core 135, match-engine 368 unchanged).)
>
> **Last Updated (prior):** August 4, 2026, latest same day (**`match-realism-pass` SKILL RE-CUT FOR WIRE-FIRST
> — the calibration ladder moves behind a wiring gate, and the gate now defers to the wiring backlog
> and KD-W1.** Tooling-only; no `.cs`, no spec, no assembly, no gate run. The skill encoded
> measure → localize → ladder → land, which is the right shape only when the chain under the dial is
> complete. Twice in the §5.Z chain a brief arrived asking for a *quality* that turned out to be
> **undefined** because a stage was missing — **§5.Z.17** ("the quality of the save, not its
> existence"; measured zero hand contacts across six keeper-matches, one cause being
> `OnShotExecutedEvent` with zero callers anywhere) and **§5.Z.23 / ERR-011-008** (#11's catch coded to
> one of its two spec statements, so a claimed ball flew on into the net).
>
> **New `## 0. The wiring gate`, ahead of the premise check (now §0.1).** It opens by requiring the
> chain to be **enumerated from the observable backwards to the dial** out of the owning spec's §3 —
> building that list is the hard part, since nobody had "the catch parks the ball" on a stage list
> until §5.Z.23's instrument followed the ball after the contact — and falls back to §1's funnel when
> the list cannot be written from source. Then six source-read checks, **all six run, every failure
> reported**: multi-gap chains are not rare (§5.Z.15 found #11 switched off AND keepers skipped by the
> physics phase; §5.Z.17 found three independently sufficient defects). Checks 1–5 are assembly
> existence, composition-root construction + phase reach + **the flag state inside your own
> instrument** (`DisableGkHeading()` is called in five places and §1 tells you to copy an exemplar),
> live **read**-side consumer, spec §3 **body** vs Outputs summary, and Stage-0 placeholder. Checks 1
> and 5 split on whether the brief names a spec or a symptom.
>
> **Merged with `main` across the wiring audit, which changed this skill rather than merely colliding
> with it.** Three integrations: (a) **check 0 is now `match-engine-wiring-backlog.md`** — the audit
> enumerated **10 Class-A dormant capabilities** by three systematic sweeps, so the gate reads that
> board before re-deriving anything, and W1/W2 (the keeper never leaves his line, no player has ever
> made a tackle) are cited as the standing examples; (b) **new check 6, gate-level dormancy**, which
> the audit names as the explicit blind spot of exactly the static checks §0 had listed — a call site
> that runs but whose condition is almost never true is invisible to all of checks 1–5, and C1 (#12
> commits `InPoss` on **9.5%** of final-third samples) was found only by runtime instrumentation;
> (c) **§3 now opens with KD-W1's `[GT]` freeze**, since the project-wide rule — no `[GT]` change
> governing an unwired subsystem until the post-backlog calibration pass — is strictly stronger than
> the per-chain conditionality this pass had written, and a skill that told the reader to calibrate
> once *its* gate passed would have contradicted standing policy. §5.Z.24 also refutes a claim in the
> skill's own opening — it is "the first premise in this chain that survived its own check" — so
> "every one produced a partly-wrong brief" is corrected to seven of eight, and its **ERR-008-018**
> joins ERR-008-017 as §2's second cause-1 instance.
>
> **The gate is a filter, not a verdict on calibration.** §5.Z.20 is cited in both §0 and §3 as the
> standing counterexample: a `[GT]` recalibration inside #11's own §3.4.3/§3.4.5 ranges produced **the
> largest single movement this chain has measured, goals per match 14.7 → 8.0**. It fixed two timing
> defects in the same pass — so the gate would have had work to do there too — and its owner document
> states those fixes alone were not sufficient, the old values "could not reach the catch band … even
> with a perfect window", which is precisely the point: the dial was load-bearing independently of the
> wiring. The stated rationale for wiring first is therefore **not** that it moves the number more, but
> that a missing stage bounds the outcome at a level no dial can reach.
>
> **Two further edits.** §2's cause 3 (structurally unreachable / vacuous gate) is labelled **§0
> failing late**. §7 requires the recorded residual to be **classified — missing stage or mis-set
> dial** — because the next pass runs §0 against that sentence.
>
> **Adversarially reviewed before landing; the review is why this entry reads as it does.** Pass 1
> raised 4 High: a superlative ("the largest movements came from a missing stage") that the chain's own
> record **refutes** via §5.Z.20; "§3 is the step most passes should skip", contradicted by load-bearing
> `[GT]` work in at least §5.Z.18/.19/.20/.21; "stop at the first check that fails", contradicted by the
> two-gaps example the gate itself cites; and a **misattribution of the motivating evidence** — §5.Z.15
> and §5.Z.16 were cited as calibration briefs that turned out to be wiring, when §5.Z.11 item 2 had
> named that wiring in advance ("opt-in and default-off (`EnableGkHeading`) … plus GK locomotion") and
> §5.Z.16 was never a brief at all. Passes 2–3 caught 3 more Medium, two of them introduced by the
> pass-1 fixes; pass 4 was clean.
>
> **Chain repair, recorded rather than absorbed.** This merge's conflict region contained a
> pre-existing defect on `main`: an **orphaned `**Last Updated:**` header** for §5.Z.23 with no body
> and an unclosed parenthesis (the real §5.Z.23 entry survives intact below as `(prior)`), plus the B6
> entry left bare when the audit entries were inserted above it — three bare labels where the chain
> permits exactly one. The orphan is deleted and B6 relabelled `(prior)`; no entry body was edited.
> This is the fourth time this chain has needed the same correction.
>
> Modified: `.claude/skills/match-realism-pass/SKILL.md` (frontmatter description + §0/§0.1/§2/§3/§7),
> `.claude/skills/README.md` (derivation row), `file-manifest.md`, and this file. Prior entry below.)

> **Last Updated (prior):** August 4, 2026, later same day (**MATCH-ENGINE WIRING AUDIT — the code that
> exists and never runs, and the `[GT]` freeze that follows from it.** Seven consecutive §5.Z passes
> fitted constants against the composed engine. This audit asks what was *in* that engine, and the
> answer is: less than the assembly graph suggests. Three passes over the 18 assemblies the match
> engine references — a comment sweep for self-declared deferrals, a whole-tree production-caller
> count over every `public` method, and manual triage of every candidate in source — found **10
> Class-A dormant capabilities**. The two largest were invisible to the project's own tracking. **The
> keeper never comes off his line**: `GoalkeeperMechanics.CommitRushIntent` has no production caller,
> though everything downstream of it works (`GoalkeeperRushDispatch.UpdateRushFrame` moves the keeper
> and writes back to the movement array; `Rushing → OneOnOne → Smothered` exists with abort reasons
> and telemetry; the `RushIntent` is even serialized) — only the trigger is missing, so every 1v1 in
> the game is a stationary keeper waiting to dive. **No player has ever made a tackle**: three
> independent dormant links in one chain — `DefensiveAITick.GetTackleIntentRequests` is populated
> every tick and read by nobody (its own class doc says integration is Stage 1, KD-16),
> `GetAndClearTackleFlag` is hardcoded `=> false` in **both** engine collision adapters
> (`MatchEngine.cs:6721`, `:6789`), and consequently `PassExecutor`'s §3.8.5 tackle-interrupt branch
> and its `CancelReason.TackleInterrupt` outcome are unreachable code. No comment anywhere records
> this one; only the call-graph pass found it. Also dormant: cross claims (`ResolveHandContactDuel`
> intentionally not called, blocked on the same multi-agent contact feed as the already-filed
> AGENT_BALL fan-out), the keeper's vision (`SaveArmed` is four lines of pure geometry while a
> tested `OcclusionFilter` runs for every outfielder), the #13 BackwardPass press trigger
> (`PassEventRing.Push` has no producer, so the ring the engine builds per team is permanently
> empty), `BallStateType.Controlled` (no producer — possession is a flag, never a kinematic
> constraint), and #26's kickoff preset selection (`ManagerAdaptation.ApplyKickoff` uncalled, so an
> AI manager starts every match on the human baseline; only the mid-match ladder is wired).
> **The method's blind spot is stated rather than hidden:** it detects *method-level* dormancy and is
> structurally unable to see *gate-level* dormancy — a call site that runs but whose condition is
> almost never true. One such is already measured (#12 commits `InPoss` on **9.5%** of final-third
> samples, starving every phase-gated mechanism in #13/#14/#15), found by runtime instrumentation in
> §5.Z.24 and by no static analysis, so the backlog carries four Class-B entries from that pass and
> books a gate-firing instrument as its own item. **This backlog is a floor, not a ceiling.**
> **KD-W1, the `[GT]` freeze:** do not land a constant governing an unwired subsystem. The hazard is
> diagnostic, not just arithmetic — measured conversion of ~18% against football's ~11% reads as "the
> shot model is too generous" when part of it is "no keeper has ever narrowed an angle and no
> defender has ever tackled", and a pass aimed at the shot model would have left behind a `[GT]` that
> later has to be un-tuned. Defect fixes, instruments and measurement continue freely; constants wait
> for one calibration pass against the complete engine. **KD-W2** scopes this to the match engine —
> the 22 approved specs with no assembly remain `path-to-playable-roadmap.md`'s problem. The §5.Z.23
> `pointQuality` owner decision is **parked, not resolved**: the rush trigger changes the contact
> geometry that decision turns on. New `docs/tracking/match-engine-wiring-backlog.md` v1.0. Read-only
> audit — no `src/` change, no spec change, no gate run. **Prior entry below.**)

> **Last Updated (prior):** August 4, 2026 (**Tilted-view revision — KD-P4a-2 (owner call).** P4a first shipped a flat top-down view with ball height faked by a sprite lift and a capped size ramp. The owner reversed it to an FM-style view — from above, **tilted back from vertical, slightly off centre** — since the ball only needs to be visible on and above the pitch, not scaled. The revision **deletes more than it adds**: with a tilted camera height is a real world axis, so `BallHeightViewOffsetPerMetre`, `BallHeightScalePerMetre` and `BallMaxHeightScale` are gone, along with `BallRenderModel.SpritePosition`/`SpriteRadius` and `MatchRenderProjection.HeightScale` — and with them the AR pass's M-5 finding and its 10 m saturation limitation, which stop existing rather than needing a retune. New: `PitchCameraRig`/`PitchCameraPose` (height, tilt-from-vertical, lateral offset — a placement is a decision, so it is gate-compiled, and the pose is two world points because `Quaternion` is not in the shim) and `PitchViewProjection.ToWorld`/`ToWorldGround`/`TryGroundHit`. **The one real cost is the click inverse:** screen position is no longer affine in pitch position, so `TryGroundHit` does a ray/ground-plane intersection; `Camera` is not in the shim, so the Unity side supplies the ray and the math stays gate-tested. Survivors, each for a reason — the **shadow** (under any tilt a lofted ball separates from the pitch point it is over, the one cue perspective cannot supply), the corner→centre re-origin (it is the ground plane), and `FollowBallCamera` (it decides *where* the camera looks). Two things recorded rather than left implicit: the engine's Y becomes the world's **Z** and its Z the world's **Y** — an axis swap, the same trap class as the corner origin, locked by its own test (seven tests fail if it is inverted) — and `FollowBallCamera`'s pitch clamp is now **approximate**, since it describes a rectangle of visible ground where a tilted view sees a trapezoid; kept deliberately, as its job is keeping the target near the pitch rather than exact framing. **Full dotnet gate: PASSED, 0 failures** (whole tree green; match-client-core 112 → 129, match-engine 368 unchanged — no sim source was touched). The entry was first written while the run was still in flight and recorded as *not yet reported*; this line replaces that provisional wording with the run's actual result.)
>
> **Last Updated (prior):** August 4, 2026 (**§5.Z.24 — CLOSE-CHANCE CREATION: the first premise in this
> chain that survived its own check, one formula defect fixed, and the creation gap deliberately NOT
> claimed.** §5.Z.23 §7 item 4 re-localized the creation residual to the final-third → penalty-area
> stage (6.5% against football's ~40%) and named it a #8/#15 surface. Two premises were checked.
> **The first SURVIVED — a first for this seven-pass chain**: the "306.7 final-third entries" figure
> is a raw boundary-crossing count that a ball rattling across x = 35 would have inflated, but
> re-counted with a 1 s exit dwell over six full matches it reads **311 episodes against 312 raw
> crossings**, each averaging 5.1 s. The denominator was sound. The second premise located the stage
> without naming a mechanism, and the instrument (`CloseChanceDiagnosticTests`, env-gated
> `TD_CREATION_DIAGNOSTIC=1`) found two, both real: **nobody is in the box** — mean attacking
> outfielders inside the penalty area while the ball is in the final third is **0.11**, with 92% of
> samples at zero, and the deepest *composed target slot* is **22.8 m** from goal against a deepest
> *attacker* at 22.2 m, so the players sit within 0.6 m of where they are told to be and are simply
> never asked into the area — and **the carrier walks the ball back out**: DRIBBLE is the modal
> attacking-third action at **40%** of heartbeat decisions with a mean cosine to the opponent goal of
> **−0.302** and only 31% pointing goalward. **ERR-008-018** is the second half: #8 §3.1.5.2 picks the
> dribble direction by free space alone and closes by delegating the correction to *"the scoring stage
> (§3.2.2)"* — but §3.2.4.1, DRIBBLE's actual formula, has no directional factor and **§3.2.2 is the
> PASS formula**, so the promised term was delegated to a section that does not own DRIBBLE and never
> had a home. Same class as ERR-008-017. Fixed with `DirectionQuality_DRIBBLE`; measured cosine
> **−0.302 → +0.006** and goalward share **31% → 49%**, moving on **all six seeds with no overlap**
> between the pre- and post-fix distributions. The `[GT]` floor lands at **0.80** rather than the 0.50
> that maximises the effect, because suppressing the dribble pushes the carrier onto HOLD — which has
> no timeout — and at floors 0.65 and 0.50 one seed in six stalled outright (mean final-third episode
> 5.1 s → 17.5 s / 28.6 s). **The creation funnel itself did not move and is not claimed**: box
> occupancy 0.11 → 0.10, ball into the box 6% → 5% of episodes, passes into the box 1% → 0%, shots
> 19.3 → 19.5, goals 3.67 → 3.50 — **the residual shot-count gap is NOT closed**. #15 §4.5.2's
> run-target overlay was implemented, measured and **REFUSED**: it moves the committed RUNNER's target
> from 80.9 m to 14.7 m from the attacked goal and moves box occupancy **down**, 0.11 → 0.08, because
> a RUNNER's target is `carrier + 12 m` and the carrier is usually still in midfield. A pooled number
> nearly carried a false creation claim — at floor 0.50 the corpus reads box occupancy 0.11 → 0.59,
> but five of six seeds are flat and the whole movement is **one stalled match** contributing 32% of
> samples; the acceptance scenario's box predicate failed post-fix, forced the per-seed breakdown, and
> the claim was withdrawn and the predicate deleted rather than re-tuned. The residual is re-localized
> and sharper than what it replaces: **#8 cannot pass to a place, only to a player** — §3.1.3 generates
> one PASS candidate per visible teammate *at that teammate's current position*, so passes into the box
> measured 1% at every rung of the ladder, including rungs where players did reach the box. Owner doc:
> `docs/tracking/close-chance-creation-design.md`; match-engine §5.Z.24; `spec-error-log.md` v1.56.
> Acceptance `match-engine-close-chance` — **2 of 3 predicates fail at `7fcd897` by execution**. No
> schema / RNG / domain-tag / draw-site / draw-order change. Prior entry below.)

> **Last Updated (prior):** August 4, 2026 (**P4a ADVERSARIAL-REVIEW PASS — 1 High, 5 Medium, 3 Low fixed;
> the pass then re-run clean.** **H-1, and the one that would have shipped:** `PitchMarking.Rectangle`
> took its two corners in whatever order it was given, and `PitchMarkings` builds the end boxes from
> their goal line *inwards* — so the away penalty area and away goal area arrived with **descending
> X** while the home pair ascended. A P4b binding doing the obvious `B − A` would have drawn those two
> inverted or invisible: the #8 ERR-008-002 home/away asymmetry class, landing in a `MonoBehaviour`
> where the gate can never see it, in the very type whose purpose is to leave the skin nothing to
> decide. Worse, the fixture *laundered* it — `AssertAreaBox` normalised with `Mathf.Min`/`Max` before
> asserting, so any corner order passed. `Rectangle` now normalises (A = min, B = max), the helper
> reads `A`/`B` directly, the mirror test states the rectangle pairing explicitly, and two new tests
> pin it; verified non-vacuous by un-normalising the factory, which fails four tests.
> **M-2:** the render path had **no non-finite gate** while its sibling `MatchFrameView` refuses one
> fail-loud — and `ProjectBall`'s doc excused the omission with "the producers upstream refuse to
> publish a non-finite coordinate at all", which is false: `LiveMatchStreamer` does not check, and
> `FrameInterpolator` *deliberately propagates* a non-finite position (it reads as a discontinuity and
> snaps to it). A NaN would have reached `transform.position`. Agent and ball **ground** positions are
> now refused; ball **height** keeps its graceful degradation, because a bad height still leaves a
> true ground position to draw at. **M-3:** `HasBall` was derived from `PossessionRingRadius > 0`, so
> a `[GT]` config setting the ring radius to zero would have answered "nobody has the ball" for a
> whole match — a fact about the simulation riding a presentation size. Inverted: `HasBall` is stored,
> the radius derives. **M-4:** a `BallMaxHeightScale` below 1 was silently repaired into "no cap",
> contradicting the `[GT]` loader's fail-loud contract in an untestable branch; it is now refused at
> boot, along with the previously documented-only "the ring must exceed the marker" invariant, and the
> repair branch is deleted. **M-5:** two `[GT]` rationales carried **fabricated figures** — an uncapped
> 20 m ball is 2.8 m across, not "wider than the penalty area"/"the six-yard box", and a 0.25 m marker
> is ~9 px at the default camera, not "a pixel". Replaced with checked numbers, plus the cap's real
> 10 m saturation point, now pinned by a test. **M-6:** the shirt-numbering rule was **duplicated, not
> moved** — the browser viewer's inline `computeJersey` was still there while the class doc, the
> version history and the commit message all said it had moved into `MatchRoster`. New
> `match-viewer/RosterShirtNumbers.cs` is now the one implementation; `LiveMatchStreamer` caches its
> output, `LiveMatchServer` serves a `"shirt"` key, `computeJersey` is deleted, and the rule's tests
> move down with the rule. **Lows:** a tautological marker-radius test replaced with one that can
> fail, `MatchRoster.FromStreamer`'s happy path covered (it had only its null guard, so the only
> production path into the type never ran), and the ring/marker invariant now enforced rather than
> merely asserted against the fallbacks.
> Two further defects surfaced while re-reviewing the fixes and were closed in the same pass: the M-2 gate initially ran *inside* the write loop, which would have left the destination half this frame and half the last behind a thrown exception (it now validates in a pass of its own, so the method stays all-or-nothing), and M-4's new validators were themselves unreachable from any test — replacing an untestable repair branch with an untestable guard would have moved the problem, so `MatchClientConstantsTests.cs` v1.0 drives them directly.
> **Full dotnet gate: PASSED, 0 failures** (whole tree green; all 30 suites reported, quarantine empty) — `match-client-core` 103 → 112, `match-viewer` 41 → 48, `ui-framework` 50 (unchanged), `match-engine` 368 passed / 8 skipped (unchanged; no `match-engine` source is touched by this pass), every other suite unchanged. No new compiler warnings — the five the tree reports are pre-existing CS0649s in `decision-tree`. No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream /
> domain tag / draw site, no draw-order change, no engine-behaviour change.)
>
> **Last Updated (prior):** August 3, 2026, latest same day (**INTERACTIVE UNITY CLIENT P4a LANDED — the
> host-free render model.** P4 is split into **P4a, every render *decision*, and P4b, the binding.**
> That split is the August-3 owner-decision rule ("keep logic out of `MonoBehaviour`s") turned from a
> discipline into a phase boundary, and the ordering argument is the one that already put P6's
> head-less scenario ahead of P4: land the decisions where `tools/dotnet-ci` can compile and test them,
> and what is left for the pinned host is binding — which a cert run genuinely verifies — rather than
> behaviour, which it verifies only along the paths someone thought to click.
>
> **What landed** (all in gate-compiled `src/match-client-core/`): `PitchViewProjection`, the single
> documented coordinate adapter §7 requires — engine **corner-origin** metres ⇄ a **centre-origin**
> view plane at 1 unit per metre, plus the inverse a pointer click needs. Centring is not cosmetic:
> it makes a home-end position and its away-end mirror differ only in sign, which is what turns the
> mirrored assertions this repo's #8 ERR-008-002 history demands into one line each.
> `PitchMarkings`/`PitchMarking`/`PitchMarkingKind`, the IFAB catalogue as shapes, read from the
> **existing** `MatchViewerConstants` `[FIXED]` values rather than restated (§7's one-source-of-truth
> rule across both Views), with both ends emitted from one loop over a sign so a marking cannot be
> right at one end and wrong at the other. `MatchRoster`, the match-constant per-slot data — and the
> shirt-numbering rule finally out of the browser viewer's inline JavaScript and into gate-tested C#.
> `MatchRenderProjection` → `AgentRenderModel`/`BallRenderModel`: positions from the P3 interpolator's
> buffer because that is what is actually being drawn, every discrete cue from the newest captured
> frame because cues do not interpolate, the possession ring, and the ball's shadow / height-lift /
> capped-scale cues. Colour-free by design — a palette has no correct answer a test could assert.
>
> **Deliberately not built:** the D-arc and the corner arcs. Neither has a `[FIXED]` constant and the
> browser viewer draws neither, so adding them would mean inventing geometry here and diverging the two
> Views. Recorded rather than silently dropped.
>
> **The finding, KD-P4a-1 — a stale cache older than this pass.** `LiveMatchStreamer` cached team ids
> *and* goalkeeper flags at construction under "roster metadata never changes across a match". True of
> team ids; **false of goalkeeper flags**, which `MatchEngine.SubstitutePlayer` rewrites — so a keeper
> substituted for an outfield player moves which slot is the goalkeeper and the cache has silently
> disagreed with the engine ever since, drawing the keeper ring on the wrong player in the browser
> viewer since P1. A Unity roster type built on the same accessor would have inherited it wholesale,
> which is the argument for doing the render model before the skin rather than after. `LiveAgentCue`
> gains `IsGoalkeeper` — the first cue added through the extension mechanism KD-P1-6 created the struct
> for — sampled every tick; `MatchRoster` holds no goalkeeper flag at all so the stale copy cannot come
> back; `LiveMatchServer` reads the frame cue, fixing the harness with no JSON key and no viewer-script
> change. Re-reading the engine from the accessor was rejected: that is the off-sim-thread tear-read the
> streamer's single-writer invariant exists to prevent, and the reason it was cached to begin with.
>
> **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order
> change, no engine-behaviour change** — the new cue is sampled from an existing read-only accessor.
> **Full dotnet gate: PASSED, 0 failures** (whole tree green; all 30 suites reported) — `match-client-core` 65 → 103, `match-viewer` 39 → 41, `ui-framework` 50 (unchanged), `match-engine` 368 passed / 8 skipped (unchanged; no `match-engine` source is touched by this landing), every other suite unchanged. **Next: P4b on the pinned host** (roadmap row B8), which now binds a render model that is
> already decided and already tested.)

> **Last Updated (prior):** August 3, 2026, later same day (**§5.Z.23 — CONVERSION AT CONTACT: the recorded

> **Last Updated (prior):** August 3, 2026, latest same day (**OWNER DECISION — ROADMAP B6 REVERSED: the
> product ships the FULL UNITY UI, not the web-hosted viewer.** Doc-only; no `.cs` changed. Recorded in
> `path-to-playable-roadmap.md` v0.11 (§7 supersede note, C2 amended, risk register re-cut),
> `interactive-unity-client-design.md` v0.11 (§12 status-change block), `browser-match-client-design.md`
> v1.3 (standing status block), and this file's assembly map + OPEN ISSUES.
>
> **The July 25 B6 entry is preserved verbatim and is not wrong** — it decided *time to a playable
> loop*, and it delivered that: PM-1 was reached July 27 on the browser surface. This decision is about
> *what the game ships as*, which the B6 table never weighed. That distinction matters for reading the
> record: the reversal is not a correction of a bad call.
>
> **Nothing is discarded, and nothing blocks P4 starting.** `src/match-client-unity/` is an asmdef and a
> README — P4 was never begun, so there is no unwind. The entire substrate a UGUI skin binds is already
> gate-compiled and needs no change: #38's view models and dispatchers, `MatchFrameView`,
> `MatchViewModelSource`, `MatchTacticsDispatcher`, `NavigationShell`, `MatchSession`, the command
> channel, `FrameInterpolator`, `FollowBallCamera`, and the P6 determinism locks. This is the
> "renderer is a leaf" property #38's contract was written for, finally used in the direction it was
> designed for. No art prerequisite either — §5-P4 is 2D-first, the pitch renders from the IFAB
> `[FIXED]` geometry already in `MatchViewerConstants`, agents are primitives, sprites are polish.
>
> **`src/match-client-web/` (34 tests) is retained and reclassified: shipping surface → host-free
> reference harness.** It is the only surface in the repo that exercises the whole read / playback /
> intent loop in CI on every push, which `match-client-unity` structurally never can. That makes it the
> regression net under the substrate the skin binds. Rule: **keep it green, do not extend it.** If it
> ever becomes expensive to keep green, delete it deliberately — do not quarantine it into
> `known-failures.txt`, which would leave a harness reporting green while proving nothing.
>
> **The one real cost is coverage, and the rule that bounds it is the entry worth carrying.** The CI
> gate cannot compile a line of `match-client-unity` and never will — the Unity shim covers `Vector2`,
> `Vector3`, `Mathf`, `Debug` and `Profiling`, value types and statics that can be reimplemented
> honestly, and there is no honest head-less `MonoBehaviour`, `GameObject` or `Camera`. **Extending the
> shim to fake them is explicitly REFUSED:** a lifecycle-free stand-in would let a render loop that never
> runs report green, which is ERR-030-014's failure mode transplanted one layer up, and this project has
> already paid for that lesson once at the cost of months of 0–0 matches. The mitigation is
> architectural instead: **keep logic out of `MonoBehaviour`s** — every decision (what to draw, where the
> camera goes, what a click means, which intent an input maps to) lives in gate-compiled
> `match-client-core` / `ui-framework`, and the Unity types assign transforms and forward input with no
> branch a test would want to reach. P3 already demonstrates the pattern. Then the uncovered surface is
> *binding*, which a cert run genuinely verifies, rather than *behaviour*, which a cert run verifies only
> along the paths someone thought to click. Second rule: budget a cert-host run **per P4/P5 landing**,
> not one at the end — the host block cleared July 19, 2026, so that is scheduling, not access, and a
> skin first exercised at the end is the never-compiled-surface trap this repo has hit seven times.
>
> **`PM-1` is now a split claim, and the roadmap says so rather than leaving the flag to be misread.**
> Its determinism exit criterion is met head-lessly and stays met. Its other three criteria are
> statements about a *screen*, and were demonstrated on a surface that is no longer the product — so
> they are open again against the Unity client. PM-1-the-capability holds; "the Unity client plays a
> match" is not yet true.
>
> **Also fixed, pre-existing:** `path-to-playable-roadmap.md`'s Version History had its header and
> delimiter rows separated by a data row, so it did not render as a table at all, and its rows were out
> of version order. Both corrected. The duplicated `v0.9` version number — two separate July 27 landings
> — is left as found, since historical entries are not rewritten.)
>
> **Last Updated (prior):** August 3, 2026, latest same day (**INTERACTIVE UNITY CLIENT P6 — the head-less
> closed-loop scenario LANDED, ahead of P4, and the ordering is the point.** `interactive-unity-client-design.md`
> §12 recommended P6 before the render skin for one reason: `match-client-unity` is in
> `generate_projects.py`'s `SHIM_EXCLUDED_ASMDEFS`, so **every P4/P5 line is invisible to
> `tools/dotnet-ci`**, while §5-P6's scenario is head-less and checked on every push. Landing it first
> means the render skin arrives against an existing determinism lock rather than ahead of one.
>
> **What §5-P6 asks for, and what it needed first.** The scenario is specified as "boot via
> `MatchSession`, inject a scripted tick-stamped command sequence through the queue, assert (a) two runs
> with the same `MatchSetup` + same sequence are digest-identical and (b) save@N → restore →
> tick-to-N+K replaying the same post-N commands == uninterrupted run." Three of those verbs had no
> composition-level surface: **`MatchSession` could not be advanced head-lessly** (`LiveMatchStreamer.TickOnce()`
> is `internal` to `match-viewer` and the only public advance is the background pacing thread), **could
> not be saved** (the P0 pass deferred "the durable save-capture body that rides the `ServiceOnce`
> seam"), and **could not be restored** (the constructor always boot-configures a fresh engine). P6 is
> therefore three small production additions plus the scenario, not the scenario alone.
>
> **`MatchSession` v1.2.** `TickOnce()` — the head-less deterministic advance — drives the **real**
> streamer seam (`match-viewer/AssemblyInfo.cs` v1.1 grants `InternalsVisibleTo` `MatchClientCore`;
> the seam stays internal to `match-viewer`, so nothing widens for the browser viewer). Routing through
> the real seam rather than a parallel client-side tick path is what makes the scenario a proof about
> the shipping composition: the pre-tick hook fires, the frame is captured, and the full-time auto-pause
> applies exactly as under paced playback. It refuses fail-loud once `Start()` has been called — two
> threads ticking one engine is a data race, and the streamer's own "never concurrently with the pacing
> loop" contract was a comment until now. `CaptureSave()` rides the `ServiceOnce` seam, so it works
> while running, paused and at full time; **§6.3's drained-empty-before-capture invariant is now held by
> ORDERING** — one sim-thread pass under the tick gate drains and applies the queue and only then
> encodes — rather than being asserted after the fact. An `Encode` fault is latched and rethrown to the
> `CaptureSave` caller instead of escaping the pre-tick hook and killing the pacing thread (the
> isolation posture `MatchClientDriver` already takes for a refused command); the handshake is
> `Interlocked`/`Volatile` rather than a lock held across `ServiceOnce`, which would have set up the
> opposite lock order against the tick gate. `RestoreFrom(blob, squads)` splits the constructor into a
> static `BootEngine` plus an engine-agnostic wiring ctor, so a restored session re-applies **no** boot
> mutator — `ConfigureSquads` throws on a ticked engine and re-staging tactics would overwrite restored
> state.
>
> **`TickStampedCommandReplay` v1.0** is the mechanism §6.1's reproducibility invariant is defined
> against. It enqueues each log entry immediately before the tick whose pre-tick `CurrentTick` equals
> its `AppliedTick` — exactly where the original drain read the clock — so a replayed run re-stamps
> identically and **the log is a fixed point of its own replay** (asserted). An out-of-order log and an
> entry whose application point has already passed are both refused fail-loud, because silently skipping
> either yields a run that is not the log's run while still reporting success.
>
> **The load-bearing predicate is the control run.** Both scenarios would pass on a command channel
> that did nothing at all: a run reproducing itself is not evidence that the commands are in the loop.
> So `match-client-command-log-replay` runs a **third** session with the same `MatchSetup` and **no
> commands**, and requires it to DIVERGE, in a bounded window around the first command (min = the tick
> after it is drained, max = two AI strides later) rather than merely "eventually". This is the direct
> lesson of the 600-tick capstone that asserted tick count, cadence, finiteness and digest advance while
> every match was a 90-minute 0–0 deadlock (ERR-030-014). The script is ten commands across **all three**
> live mutators and **both teams**, straddling the save tick — a home-only script would have repeated
> the #8 ERR-008-002 asymmetry mistake one layer up. `match-client-save-restore-replay` saves at tick 90
> (deliberately command-free, and the scenario checks that emptiness rather than assuming it, because a
> command at the save tick is in or out of the snapshot depending on drain order while carrying the same
> stamp either way), restores, and replays the post-90 log to tick 180 against the uninterrupted run.
>
> **One predicate was deliberately not written.** A "queue is drained at capture" check inside the
> scenario would be true there no matter which order the capture pass ran in — a vacuous pass dressed as
> a guarantee. The §6.3 invariant is locked instead by a unit test that enqueues a command immediately
> before `CaptureSave` and requires it back applied and logged.
>
> **Blast radius: nothing moved.** No engine behaviour changed — the client observes and drives
> pre-existing public mutators only — so no tick-window instrument, per-90 rate band, or round-resolution
> fit is perturbed, and the FR-PO-052 baseline is untouched (no per-tick work added on any existing
> path). No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream, domain tag or draw site, no draw-order
> change. No ERR filed: nothing here contradicts an APPROVED spec.
>
> **Gate: NOT RUNNABLE in this environment.** The network policy blocks the .NET SDK download —
> `curl https://dot.net/v1/dotnet-install.sh` returns a proxy 403 — the same constraint
> `interactive-unity-client-design.md`'s own header records for the P0 landing. Verified instead by
> exhaustive manual review against source, plus a `generate_projects.py` run confirming the new
> `TacticalDirector.TestingStrategy` reference resolves and the test project is generated with it. The
> gate runs in CI on the PR; the per-suite counts are not restated here because they were not measured.
> **Prior entry below.**)
>
> **Last Updated (prior):** August 3, 2026, later same day (**§5.Z.23 — CONVERSION AT CONTACT: the recorded
> premise was refuted, and the real defect is that a keeper's CATCH never stopped the ball.**
> `gk-contact-rate-design.md` §7 item 1 recorded the goal-rate residual as *"marginal, end-of-envelope
> touches whose parries and spills keep the ball alive in the box"*, naming the Stage-0 `pointQuality`
> lottery and parry placement as the levers. That premise had never been measured. The new per-contact
> instrument (`GoalConversionDiagnosticTests`, env-gated `TD_CONVERSION_DIAGNOSTIC=1`, 3 full matches on
> the §5.Z.20–§5.Z.22 seeds) measures ball speed the tick before each contact and at the end of it:
> **parried 10.8 → 0.0, deflected 10.3 → 4.2, spilled 13.9 → 9.0, missed 9.5 → 9.5 — and caught
> 11.1 → 10.8**, one tick of drag. **The parries and spills work; the catch does nothing to the ball**,
> and **7 of 10 catches were followed by a goal within 5 s** (parries and spills: zero), with 14 of 15
> goals following a keeper contact within 10 s. **ERR-011-008**: #11 §3.5.2's catch branch is TWO
> statements — `Ball.SetPossessor(gkId)` **and** `ball.velocity = gkHandVelocity` ("parked at hand
> position") — and only the first was implemented, at both the catch and the Stage-0 smother claim.
> Possession is a FLAG in this engine, not a kinematic constraint (`RunPhysicsPhase` integrates the ball
> unconditionally; `CheckRestartAndApply` adjudicates a goal on ball POSITION), so a claimed shot flew
> on into the net with the keeper recorded as holding it. **§3.5.2's pseudocode body was correct** — the
> contributing spec defect is §3.5's **Outputs** summary, which named `SetPossessor` alone for the catch,
> and `IGoalkeeperBallSystem`, which exposed no seam for a park at all, so the omission was invisible
> from both the summary and the interface. Fixed with `ParkBall()` at both claim sites; summary restated,
> pseudocode untouched. **Measured (3 full matches, same seeds pre/post): caught-band exit speed
> 10.8 → 0.0 m/s, goals from caught contacts 7 of 10 → 0 of 11, goals over the corpus 15 → 11
> (5.0 → 3.7/match — the closest this engine has measured to football's ~2.7), scorelines
> 2-2/2-0/6-3 → 1-0/2-2/4-2.** At n=3 a 1.3 goals/match delta sits only just above this chain's noise
> bar; what carries it is that the mechanism's own signature (exit speed 10.8 → 0.0, band goals 7 → 0)
> does not depend on the goal count at all. **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream /
> domain tag / draw site, no draw-order change** — the park is a pure write to current-tick ball state.
> Locked by `match-engine-keeper-claim` (#19 ScenarioRunner, Tier B, 2 seeds × 90 min — full-match
> windows because a claim is rarer than a contact): **2 of 3 predicates fail on the pre-fix engine,
> verified by executing the scenario in a worktree at `4b12954` — `travellingAfterClaim = 6 of 6` and
> `concededWhileHolding = 5 of 6`** — plus `GoalkeeperClaimTests` (3: park XOR kick, per band).
> **Both levers §5.Z.22 named are recorded NOT fixed, with evidence rather than intent.** The
> `pointQuality` lottery is confirmed and quantified: quality 0.559 / 0.564 / 0.590 across *rising*
> contact marginality with catch rate 43% / 38% / **50%** — blind AND inverted — and
> `HandlingPointErrorSigmaM` provably cancels out of its own formula, a `[GT]` dial whose value cannot
> matter at any setting. The geometry-aware form was **implemented and measured rather than argued
> about**: it fixes the direction (0.261 / 0.255 / 0.150) and collapses the level to **zero catches and
> zero parries** (goals 3.7 → 4.3/match), because mean contact marginality is 0.68 and no `[GT]` inside
> #11's own ranges lifts the blend back over `CatchThreshold`'s 0.65 floor. **The ladder refuses; the
> next action is a design decision, not a calibration run.** Parry placement stays out on evidence — it
> produced zero goals in either corpus. **The creation residual is re-localized and is now a measured
> stage rather than "possession churn":** 306.7 final-third entries per match (football ~110) but only
> **20.0 penalty-box entries** (football ~45), and **0.68 shots per BOX entry, ABOVE football's ~0.55** —
> so neither shot selection nor the box is the bound. The bottleneck is the single transition
> **final third → penalty area, converting at 6.5% against football's ~40%**. Owner:
> `docs/tracking/gk-conversion-at-contact-design.md`; match-engine §5.Z.23. **AR-1 (full-gate
> fallout — one instrument, not the mechanism; the fourth instance of the §5.Z.22 AR-4 class):**
> `match-engine-shot-speed`'s `mean-shot-distance` predicate failed at 29.77 vs its 24.0 m ceiling.
> Three measurements settled it — the scenario **passes at the pre-fix commit**; the full-match
> diagnostic reads 29.5 / 12.9 / 19.5 m across the standing seeds (**21.7 m pooled over 41 strikes**,
> inside §5.Z.21's landed 16.5–27.1 m band, so no regression); and with everything else fixed the
> same corpus reads **27.11 m at 18 min, 24.71 m at 45 min, inside the ceiling over full matches**.
> **The shot-distance distribution is not stationary within a match** — early play is long-shot
> dominated and close-range strikes accumulate as box penetration develops — and this pass amplified
> that bias by removing a population of very-close-range REBOUND shots. Fixed in the ESTIMATOR
> (corpus 2 → 4 seeds, windows 18 min → full matches); **predicates and bounds UNCHANGED**, since a
> ceiling raised past the current reading would discriminate nothing. Full dotnet gate: **PASSED, 0 failures** (whole tree green, 30 assemblies; match-engine 376 → 376 with the failing shot-speed scenario now green (368 passed / 8 env-gated diagnostics skipped, up from 367 passed / 1 failed), goalkeeper-mechanics 78 passed; quarantine empty, so the full suite is enforced. Match-engine duration 26 m 6 s — up ~9 min, the cost of the new keeper-claim scenario (2 seeds × 90 min) and the shot-speed resize (4 seeds × 90 min); SDK 8.0.129 via apt))

> **Last Updated (prior):** August 3, 2026 (**PROJECT SKILLS LANDED — six workflow skills under
> `.claude/skills/`; tooling only: no code, no spec, no `src/` change, no gate run.** The recurring
> workflows this repo runs by hand are now Claude Code skills, checked into the repo rather than a
> personal skills directory, because each encodes conventions that live here and version with them:
> **`match-realism-pass`** (the §5.Z measure → localize → fix → calibrate → re-measure → lock loop, run
> 6 times in the §5.Z.17–§5.Z.22 chain), **`snapshot-schema-bump`** (the cross-tick decision plus the
> serializer/reader/pin/probe/round-trip checklist, over 19 bumps — two of which exist only to fix an
> earlier omission), **`err-file-and-backprop`** (id allocation against the live log, the entry
> template, spec-patch-in-the-same-commit), **`landing-close-out`** (the tracking-document sync),
> **`spec-promotion`** (supplement → 11-file set → the G1/G2/G3 gates, with G3 flagged
> non-self-grantable), and **`dotnet-gate`**. Each is derived from measured repetition in the last 200
> commits, and each carries the traps this project has actually hit — the id collisions
> (`ERR-030-015`, and the branch-vs-main class a check at authoring time cannot catch), the v17
> RNG-cursor hole, the instruments that broke because a pass moved the tick windows they hardcoded
> (three in the keeper-contact pass alone, one of which escaped to CI), the `[GT]` §6.3 → Appendix A
> gap that recurred in **all ten** promotions of the last wave, and the "offline sweep gives the shape,
> never the value" calibration lesson. **Deliberately NOT duplicated:** `adversarial-review` and
> `orientation` are invoked by the two skills that need a review step, never restated.
> **Merged with `main` twice while this branch was open**, and the second merge crossed main's
> documentation restructure — the `**Last Updated:**` chain moved out of `CLAUDE.md` into this file and
> OPEN ISSUES into `open-issues.md`, so this branch's `CLAUDE.md` edits were **redistributed into the
> new structure rather than merged textually**. In parallel main landed its own `.claude/` work (PR
> #283 `adversarial-review`, #284 the advisor council + orchestrator, #285/#287 `chat-review` and the
> SessionStart hook), so the directory now holds two kinds of thing — **agent patterns** that change
> who does the work, and the six **workflow encodings** above that change how a recurring job is done
> correctly. Only `orientation` remains account-level. `.gitignore` resolved to main's negation set (a
> strict superset), and `.claude/README.md` is the single index of the directory.
> **THREE DEFECTS FIXED IN THE SAME PASS, all found by auditing the docs against the tree rather than
> reading them:** (1) this chain carried **five** bare `**Last Updated:**` labels — the July-27 Track C
> Phase B, July-27 doc-sync, July-27 season-roll and July-26 root-doc entries each kept the current
> label instead of `(prior)`, leaving the file self-contradictory about its own currency (the defect
> `CLAUDE.md` had corrected three times before, which the split then carried across verbatim, and which
> `file-manifest.md` reproduced independently); all four relabelled, entry text untouched. (2) The
> `src/` assembly map in `CLAUDE.md` listed **`match-analytics` twice** with different Notes — one row
> from the July-27 doc-sync pass and a second from the Track C B6 landing; merged into one row carrying
> both facts (T0-only **and** the no-sim-assembly-may-reference-it layer guard). (3) The production
> assembly count read **30** in both PROJECT IDENTITY and the REPO STRUCTURE tree; disk has **31** —
> never updated when `match-client-web` landed in B6, and that assembly *is* in the map table, so the
> table and the prose disagreed. **Verified unchanged:** the `53 APPROVED / 0 IN REVIEW / 0 NOT
> STARTED` and `22 with no assembly` claims both re-derived from `SPEC_INDEX.md` registry rows and the
> assemblies on disk — correct as written. `landing-close-out` now encodes the one-bare-label
> convention so (1) stops recurring. See `.claude/skills/README.md`. Prior entry below.)
>
> **Last Updated (prior):** August 2, 2026, later same day (**Intra-layer acyclicity landed in `src/CLAUDE.md`;
> `ERR-020-002`'s one open question closed; open-issues re-filing pass — 18 → 10 active.**) Two follow-ups
> to the taxonomy filing. **(1) Acyclicity.** The proposal left one question for the owner: a flat tier
> permits intra-tier cycles, and two tiers now carry a real internal order (`match-client-core` →
> `ui-framework` → `match-client-web`; `season-save` → `living-world`). Sub-ranking Client and Management
> was the alternative and was rejected as brittle — it would need re-cutting every time a client assembly
> is added. The sentence is taken: *intra-layer references are permitted; intra-layer cycles are not*
> (proposed as `FR-CS-046a`, a sub-clause of FR-CS-046 rather than a new FR, so nothing renumbers). It
> documents an invariant **already enforced mechanically** — verified, not assumed: Unity rejects circular
> `.asmdef` references, and `tools/dotnet-ci/generate_projects.py` emits one `<ProjectReference>` per
> `.asmdef` reference (line 157), so a cycle fails the Linux compile gate too. It landed **now** in
> `src/CLAUDE.md` `### Reference Direction`, where it binds under today's three-layer taxonomy and does not
> wait on sign-off; §3.5.2 gains the same sentence when the tier order is signed off. `ERR-020-003` also
> sharpened: `src/CLAUDE.md` is the only one of the three renderings that labels its arrow, so it is the
> model for the fix rather than a fourth problem. **(2) Re-filing pass over `open-issues.md`** — the
> second flagged item from the `CLAUDE.md` split, where a deliberately conservative classifier left
> everything ambiguous in the active file. Eight entries archived: **six closed but never moved** (#18 and
> #19 both APPROVED May 15, 2026 and stale by fourteen months, their own text already reading "superseded
> above; entry retained for history"; ERR-030-014, resolved at §5.Z Phase H; the A4a blocker, superseded
> by its own July-28 UPDATE four days after opening; the Fixed64 scope decision, a decision record rather
> than an issue; the naming-convention reconciliation, complete May 6, 2026) **plus a duplicated pair** —
> the tactical-theory entry appears twice, and diffing them showed the copies are not equivalent: one
> predates the same-day CORRECTED/REVERTED review pass and still lists a test seam the item-(3) revert
> removed. Both are archived, canonical first, superseded second, so the correction history survives.
> **Three titles amended** to lead with what remains open rather than what has landed (`floatModelHash`,
> GK/Heading Phase 1, and the #23–#26 supplements — all four of which are now approved specs, leaving only
> #26's §9.2 `[GT]` balance review). **Bodies are preserved byte-for-byte** and asserted so before write;
> the only additions are a dated status clause inside each bold title and one italic *Re-filed* note.
> Where a title contradicted its own body — two did — the body wins and the note says which. Root
> `CLAUDE.md`'s index regenerated from the active set: **10 active / 41 resolved**. Prior entry below.)

> **Last Updated (prior):** August 2, 2026 (**ERR-020-002 + ERR-020-003 filed, both OPEN — the assembly layer
> taxonomy back-prop the `src/CLAUDE.md` split surfaced.**) Spec #20 §3.5.2 places **19 of the 31 assembly
> folders now in `src/`**; the twelve unplaced are `living-world`, `match-analytics`, `match-client-core`,
> `match-client-unity`, `match-client-web`, `match-engine`, `match-viewer`, `player-database`,
> `player-progression`, `season-save`, `tactical-instructions`, `ui-framework`. FR-CS-046 is decided
> relative to two layer memberships, so it currently decides nothing about ~39% of the tree — including
> every reference into or out of the composition root, which is precisely the part still being built.
> **ERR-020-002** proposes a ten-tier order (Foundation / Physics / Configuration / Mechanics / AI / Data /
> Composition / Management / Presentation / Client, with Infrastructure out-of-band) covering all 31
> folders, **derived from the `.asmdef` reference graph rather than folder names** and verified against the
> whole graph before proposing: zero upward references, 29 intra-tier references all pre-existing and
> acyclic. Adopting it therefore changes nothing that exists and constrains only future code, which is
> both its value and why its cost is zero. It also retires §3.5.2's stale empty `UI (Stage 1+ — not
> specified yet)` row (four UI/client assemblies exist; #38 is APPROVED) and strikes `code-standards` from
> `src/CLAUDE.md`'s infrastructure table (no such folder; #20 is a style guide). **Spec #20 is deliberately
> untouched** — layer membership is its authority and wants owner sign-off, and a wrong answer written into
> the authority file is worse than a documented gap; the ⚠️ note in `src/CLAUDE.md` names the gap and now
> cites the filing. The one call worth arguing with is `player-database` at tier 5 (above AI, below
> Composition): no gameplay-layer assembly references it today, and seating it there is what keeps physics
> and AI operating on struct parameters rather than squad rows. **ERR-020-003** (Low) came out of the same
> verification: §3.5.2 draws `Physics ──► Mechanics ──► AI ──► UI` while the root `CLAUDE.md` states `AI →
> Mechanics → Physics, never the reverse` — the same rule with opposite arrows and neither notation
> labelled. The code follows the `CLAUDE.md` reading; no violation exists, so this is a notation fix, not a
> behaviour one. `spec-error-log.md` → v1.54. Prior entry below.)

> **Last Updated (prior):** July 28, 2026, latest entry of the day (**KEEPER CONTACT RATE — §5.Z.20 §7.1's
> residual, BOTH NAMED LEVERS LANDED, MEASURED; the goal-rate residual moves to conversion AT
> contact.** Measured per episode at the ball's goal-plane crossing (new env-gated
> `GkContactRateDiagnosticTests` — a frame aggregate cannot attribute position vs timing): of 15
> crossed un-contacted threat episodes at baseline, **9 were dive-early with the dive over 456–2000 ms
> before the ball arrived and dive-late exactly 0** — the commit was never slow, always too eager —
> plus 3 no-dive, 3 lateral-miss, with the lateral need at the crossing (1.91–3.83 m) at or beyond the
> dive's ~3.55 m total coverage. **ERR-011-007** — #11's `Anticipate → Diving` row was unconditional on
> `SaveIntent`, so the fixed 600 ms dive envelope opened and closed during the ball's 925–2006 ms
> flight; new #11 §3.3.6 commit-to-arrival gate (hold the coiled keeper until predicted time-to-plane ≤
> a lateral-need-scaled commit lead, `[GT] DIVE_COMMIT_MIN_LEAD_FRAC`; ONE crossing predictor shared
> with the ERR-011-003 dive direction so timing and direction cannot drift). The §3.2.3 window anchor
> refined to the keeper's first decision opportunity at/after the live stamp — the first full-corpus
> run measured the window §5.Z.20 fixed collapsing back to ~0 under the hold (the shot is usually
> struck AFTER the intent commit and re-stamps the episode), the pass's one calibration iteration.
> **ERR-012-010** — #12 §3.3.3's GK-slot lateral term (`GK_LATERAL_FACTOR × basisY` over the pitch
> width: ±2 m of travel over 68 m) becomes the BALL-LINE point clamped inside the goal mouth
> (`[GT] GK_LATERAL_CLAMP_M` = 3.0 replaces the factor, retired not retuned — no value of a
> pitch-anchored gain expresses goal-anchored tracking; central ball is the exact pre-fix identity).
> **Measured over 3 full matches, same seeds pre/post: contacted episodes 8 → 23, crossed
> un-contacted 15 → 9 (contact rate ~35% → ~72%), deep dive-early GONE (residue 83–183 ms = the 10 Hz
> grid), catches 6 → 10, window at contact 0.34–0.44 — and goals 14 → 15 over the corpus, UNCHANGED at
> n=3.** The §5.Z.17 shape again: "contact rate → goals/shot" assumed a contact stops the shot, and
> that premise does not survive tripling the contact count — the added contacts are marginal
> end-of-envelope touches whose parries and spills keep the ball alive in the box (one match ran 6-3
> on such chains). **The honest next lever is conversion AT contact: the Stage-0 pointQuality lottery
> (E ≈ 0.68, attribute-blind) and parry placement (nothing steers a parry away from the goal mouth).**
> **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order
> change** — both mechanisms are pure functions of the current tick's ball state and keeper position.
> Locked by `match-engine-keeper-contact` (#19 ScenarioRunner, Tier B, 2 seeds × 45 min) — **3 of 4
> predicates fail on the pre-fix engine, verified by executing the scenario in a worktree at the
> pre-fix commit** (`heldCommits = 0` — the hold is structurally impossible pre-fix; contacts 3 vs 4
> crossings, inverted; one deep dive-early) — plus `GoalkeeperCommitGateTests` (11), four ball-line
> GK-slot locks, and the `GoalkeeperConversionTests`/save-launch-scenario re-anchor (a parked ball now
> correctly HOLDS the dive — the Phase-H "tests encoded the old contract" class, intent preserved).
> **The full gate then failed two INSTRUMENTS — neither a defect in the landed mechanisms (AR-4):**
> the shot instruments sampled the strike from `BallView` at END of the strike tick with the attacked
> goal named by the sampled velocity's x-sign, and this pass made same-tick post-strike touches common
> enough to break that — a measured 13 m strike read as **92.3 m** (velocity reversed by a touch ⇒
> wrong goal), driving `match-engine-shot-speed`'s distance mean to 51.80 vs its 24.0 ceiling, with
> the same dilution having left the speed-mean floor a 0.08 margin; fixed at the root with the
> strike-TIME `TestOnly_LastShotStrikePosition/Velocity` seam (captured beside the `_shotContacts`
> increment — post-ApplyKick, before anything else can move the ball) consumed by the scenario AND
> `ShotOutcomeDiagnosticTests`, plus 9 → 18 min/seed windows (this pass thinned 9-min windows to 3
> strikes — a per-sample lottery; predicates/bounds UNCHANGED, measured clean distMean 22.7). And the
> P1 observer-neutrality test's non-vacuity guard tripped because this pass moved its seed's first
> restart ~3 900 → 7 270 ticks; window re-measured 6 000 → 8 000, guard intact. **A THIRD instrument
> of the same class then surfaced on the PR's Linux CI gate (AR-5):** the #37 MatchAnalytics liveness
> test measured away possession at exactly 0 because this pass moved its seed's away-possession onset
> past the 30 s window (first accrual measured between ticks 1 800 and 2 400); window re-measured
> 1 800 → 3 600 ticks, assertions unchanged.
> **Full dotnet gate: PASSED, 0 failures.** See `docs/tracking/gk-contact-rate-design.md` +
> `match-engine-design.md` §5.Z.22 + `spec-error-log.md` v1.53 + src/CLAUDE.md v2.50. Prior entry below.)
> **Last Updated (prior):** July 28, 2026, last entry of the day (**A4a STEP 0 PASSED — the round-resolution
> calibration corpus is worth fitting for the first time.** Re-run after the §5.Z.17–§5.Z.21
> match-realism chain: over the same 20 keyed matches (dSquad ±6.0), **strong-at-home mean margin
> +7.100, strong-away mean margin −4.700** — the ramp extremes separate IN BOTH DIRECTIONS (the strong
> away side wins 9 of 10 at 5.8 goals/match where the July-26 runs had it scoring 0–2 across every
> match), upsets exist (the strong side loses 3–4 in one row), and the §5.Z.11 venue asymmetry is down
> from ~15× to **~1.5× on margin** — a modifier on the strength signal rather than the signal itself
> (recorded as a fit caveat: the model's home term absorbs it, KD-8's re-capture rule applies if a later
> pass shrinks it). **One instrument fix (found by execution):** the first post-play pilot FAILED at
> teardown with every assertion green — a PLAYING match emits FM-08/FM-03 possession-race errors as
> ordinary match events (§5.Z.7), and the calibration drivers predate play developing; both env-gated
> drivers now carry the standard `LogAssert.ignoreFailingMessages` wrapper
> (`RoundResolutionCalibrationHarnessTests` v1.1), and the re-run with the fixed instrument reproduced
> the identical 20 rows (deterministic keyed seeds — verified byte-identical) and PASSED. **Next A4a
> action: the corpus slices + `tools/round-resolution-fit.py` (~1.4 h across four processes), its own
> roadmap item.** Docs: `round-resolution-corpus.md` v0.3 (§1.b full CSV) + the §5.Z.11 and
> path-to-playable OPEN ISSUES updates. **Full dotnet gate: PASSED, 0 failures.** Prior entry below.)
> **Last Updated (prior):** July 28, 2026, latest same day (**SHOT VOLUME — §5.Z.19's remaining lever (a), FIXED,
> CALIBRATED AND MEASURED — and the calibration ladder REFUSED half the design target, which is the
> finding worth keeping.** Measured first (`ShotOutcomeDiagnosticTests` v1.3 gains per-shot distance +
> possession-churn context): the finding is the DISTRIBUTION, not the count — mean shot distance ran
> **30–34 m** against football's ~17, ~60% of shots beyond 22 m, clustered AT the §3.1.4.2 range-gate
> boundary. Cause (**ERR-008-017**, verified against source): #8 §3.2.3.1's `U_SHOOT` has **no distance
> term**, and `GoalOpeningScore` is scale-free by construction (goal arc and near-goal-blocker occlusion
> both shrink ~1/d) — within range a 34 m shot scored identically to a 10 m one, while football's
> P(goal|shot) falls ~tenfold over that span; the ERR-008-016 class, the formula omitted the strongest
> single predictor of shot value in the game it models. Patched (spec + code same commit): §3.2.3.1 gains
> `DistanceQuality_SHOOT` — 1.0 inside `[GT] SHOOT_SWEET_RANGE_M` = 12 m (every close-range utility
> BITWISE untouched, so the §5.Z.17–§5.Z.20 calibrations stand), hyperbolic decay
> `FALLOFF/(FALLOFF + (d−SWEET))` beyond; the range gate stays the hard cap (a preference, not a cliff —
> the ±0.15 composure-noise band still lets an adventurous agent take the occasional 30 m shot, which is
> football). **The four-rung falloff ladder (3 full matches per rung, same seeds) showed count ≈ 25 AND
> mean ≤ 22 m are NOT jointly reachable by this lever:** FALLOFF 9 hits 24.0 shots but keeps 39% long
> shots and goals at 7.7; once long shots correctly lose to passes, volume is bounded by close-chance
> CREATION, and at ~3× football's final-third churn almost no possession penetrates the box (0.05
> shots/entry vs football's ~0.2). **`[GT] SHOOT_DIST_FALLOFF_M` = 8 chosen — the distribution + goal-rate
> landing: shots 31/35/38 → 17/19/17, long-shot share 60% → 30%, goals 8.0 → 4.7/match (the closest this
> engine has ever measured to football's ~2.7), scorelines 2-2 / 3-2 / 5-0** — the roadmap chain wants a
> goal rate that makes the A4a corpus worth fitting, and a football-shaped distribution at 18 shots serves
> that strictly better than a football-count 24 still dominated by range-boundary strikes. Speed floors
> unaffected (the decay changes which shots are TAKEN, not how they are struck). **No
> `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order change.**
> Locked by the `match-engine-shot-speed` scenario's new mean-shot-distance ceiling (**fails on the
> pre-fix engine at exactly 30.0 vs 24.0, verified by execution before the scorer change landed**) + 5
> `UtilityScorerTests` locks; one existing lock re-anchored with intent preserved (a zone-ratio test at
> 28 m where the decay pushes the suppressed branch into the UTILITY_FLOOR clamp — found by execution,
> AR-3), and the `match-engine-shot-outcomes` corpus resized 9 → 18 min/seed after the full gate caught
> its goals-still-scored reachability predicate at zero goals on the calibrated neutral path (the
> keeper-conversion corpus-sizing lesson, AR-4). Recorded, not fixed: the churn/creation residual (now also owning the count gap) and the midfield
> long-shot machinery being production-unreachable dead surface (zone minimum 40 m vs range-gate maximum
> 35 m). **Full dotnet gate: PASSED, 0 failures.** See `docs/tracking/shot-volume-design.md` +
> `match-engine-design.md` §5.Z.21 + `spec-error-log.md` v1.52 + src/CLAUDE.md v2.48. Prior entry below.)
> **Last Updated (prior):** July 28, 2026, later same day (**KEEPER CATCH/PARRY CONVERSION — §5.Z.19's residual
> lever (c), the dominant goal-rate term, FIXED, CALIBRATED AND MEASURED.** The §3.2.3 reaction window —
> 30% of #11 §3.5.1's handling-quality blend — was structurally dead: re-evaluated every frame, so the
> value the contact consumed was dated by the ball's whole FLIGHT time (**ERR-011-005** — the spec's own
> §3.2.5 worked example scores the dive COMMIT; now computed once at the dive-launch frame and FROZEN),
> and the detection stamp was never cleared, so dives were dated against shots struck **85–349 seconds**
> earlier, with rebound/deflection episodes having no anchor at all (**ERR-011-006** — the stamp now dies
> with its episode via `ClearSaveIntent`/save resolution, and the new `OnThreatArmed` seeds it at episode
> onset through the same §3.2.1/§3.2.2 formulas; a live stamp always wins, so the stamp itself is the
> latch — already serialized in the v19 GK block, **no new engine state**). Baseline windows at contact:
> 0.000/0.000/0.199, one catch in three full matches. Plus the KD-C3 `[GT]` recalibration, all inside the
> #11 §3.4.3/§3.4.5 spec ranges, over two measured full-match iterations: `ReactionBaseMs` 350 → 220,
> `ReactionBallSpeedCoeff` 8 → 3, tolerances 120/80 → 200/140 (the engine's discrete ~100–300 ms commit
> grid scored as deep-early against human-continuous-time values ⇒ window ≈ 0 for every producible dive),
> and `HandlingBase`/`HandlingKAttr` 0.45 → 0.60 + `CatchThreshold` 0.78 → 0.74 (the Stage-0 pointQuality
> term is a fixed noise lottery — E ≈ 0.68, invariant under every `[GT]`, blind to attributes, recorded
> not fixed). **Measured over 3 full matches, same seeds pre/post: window at contact 0.000 → 0.30–0.67,
> elapsed-when-airborne 85–349 s → ~0.3 s, quality at contact 0.36–0.50 → 0.41–0.79, catches 1 → 6 of 15
> contacts, goals per match 14.7 → 8.0 (13/13/18 → 6/9/9), goals per shot 0.38–0.42 → 0.19–0.26 at 31–38
> genuine strikes/match; scorelines 8-5/7-6/13-5 → 3-3/6-3/8-1 — the engine's first football-plausible
> scorelines.** The measurement also BOUNDS what remains of lever (c), and it is not conversion: a contact
> almost always stops the shot, and the keeper meets only ~¼ of on-target shots — the CONTACT RATE (#12
> GK-slot lateral positioning + commit-to-arrival timing, mean lateral offset 1.7–4.6 m while airborne) is
> the residual, a behaviour change to APPROVED specs rather than a `[GT]` dial, recorded with shot volume
> (lever (a)) as what bounds the remaining ~3× gap to football's ~2.7. **No `SNAPSHOT_SCHEMA_VERSION`
> change, no new RNG stream / domain tag / draw site, no draw-order change.** Locked by the new
> `match-engine-keeper-conversion` acceptance scenario (`ConfigureSquads` path — the neutral-path draft
> failed its own hold predicate because the conversion did not transfer across shot populations, the
> §5.Z.19 AR-4 class reproduced) + the 7-lock `GoalkeeperConversionTests` fixture driven through the real
> orchestrator. **Instrument fallout caught before any gate run:** `match-engine-shot-speed` and
> `ShotOutcomeDiagnosticTests` counted "shots" off `ShotDetectedTickMs` edges, which the arming stamps
> redefine as threat episodes (≥ 3 m/s rollers included) — both re-anchored to the new
> `MatchEngine.TestOnly_ShotContacts` genuine-strike counter. **Full dotnet gate: PASSED, 0 failures (whole tree green — 30 suites; match-engine 360 → 366, goalkeeper-mechanics 55 → 62).**
> See `docs/tracking/gk-catch-parry-conversion-design.md` + `match-engine-design.md` §5.Z.20 +
> `spec-error-log.md` v1.51 + #11 `section-3.md` v0.4 + src/CLAUDE.md v2.47. Prior entry below.)
> **Last Updated (prior):** July 28, 2026 (**SHOT SPEED + THE PHYSICAL GOAL FRAME — §5.Z.18's residual lever (b),
> FIXED, CALIBRATED AND MEASURED.** The engine's strikers were tapping the ball at 10–30% power: #8
> §3.5.3's `PowerIntent = clamp(goalOpening × A_Finishing, 0.1, 1.0)` is a product of two [0,1]
> fractions that pinned nearly every shot at its own 0.1 clamp floor (**ERR-008-016** — patched to
> floor-plus-modulation with `[GT] POWER_INTENT_FLOOR` = 0.65; the spec's "low opening ⇒ reduce power"
> rationale inverted the game it models), and #6's `VFloor = 10` anchored a neutral FULL-power vBase at
> ~16 m/s before reducers (**ERR-006-004** — retuned 10 → 24 over two measured calibration iterations;
> the formula multiplies the ceiling span by attrFraction AND powerIntent, so the anchor must carry the
> base pace). Composed, measured shot-tick means ran 6.9–10.3 m/s against football's ~20–25. And because
> a football-pace ball moves **~0.42 m per 60 Hz tick**, fixing the speed made the goal frame's absence
> load-bearing (**ERR-001-005**): a discrete per-tick test TUNNELS through a 0.12 m post, and boundary
> adjudication at the detected position (up to 0.42 m past the plane) misread a rising ball crossing
> UNDER the bar as over it. New `BallCollision.ApplySweptGoalFrameCollision` — the tick's movement
> segment against six capped cylinders (post axes half a diameter OUTWARD of the 7.32 m inner-edge box,
> bar axis half a diameter ABOVE the 2.44 m lower edge — the same IFAB datums the box test uses),
> earliest hit wins, response is the existing restitution model, **`ApplyGoalPostCollision`'s first
> production caller** — plus a `CheckBoundaries` prevPosition overload adjudicating at the interpolated
> plane crossing. Engine wiring is capture-before-integrate / collide-after-integrate;
> `_prevTickBallPosition` is WITHIN-tick (the `RestartAppliedThisTick` class) — **no
> `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order change.**
> **Measured over 3 full matches, same seeds pre/post: shot-tick means 6.9–10.3 → 14.7–16.1 m/s, maxima
> 15.3–18.9 → 23.3–27.6; shots per match 59–70 → 31–45 (football ~25 — pace ends possession episodes
> decisively, so lever (a) shot volume is ~half discharged as a side effect); woodwork 0 → 1/0/5
> strikes/match; and goals per shot ROSE 0.14–0.25 → 0.38–0.42 (goals 12.3 → 14.7/match) — a
> football-pace shot beats this keeper far more often than a roller, so the catch/parry conversion
> (§5.Z.17 §7.5, lever (c)) is now unambiguously the dominant term in the goal rate, measured against
> real pace for the first time.** Locked by `match-engine-shot-speed` (#19 ScenarioRunner, Tier B, 2
> seeds × 9 min + scripted front-face frame probes, ~46 s) — **5 of 7 predicates fail on the pre-fix
> engine, verified by executing the scenario in a worktree at the pre-fix commit** (speed floors
> unreachable at mean 8.90 / max 17.59 on the calibrated `ConfigureSquads` path; both frame probes
> adjudicated as exits; the rising crossing misread as a goal kick — and the scenario's first draft
> sampled the NEUTRAL path, whose floors did not transfer: the full gate caught it, AR-4) — plus
> `SweptGoalFrameTests` (11, headlined by the tunneling
> discriminator) and 3 PowerIntent locks. Design AR-3 recorded a probe-geometry finding worth keeping:
> an UNDERSIDE bar strike reflects down-and-in and legitimately scores (football's in-off-the-bar), so
> a no-goal rebound probe must strike the frame front face. **Full dotnet gate: PASSED, 0 failures.**
> **A4a's realism gate advances again; the named next levers are the keeper's catch/parry conversion
> and the remaining half of shot volume.** See `docs/tracking/shot-speed-woodwork-design.md` +
> `match-engine-design.md` §5.Z.19 + `spec-error-log.md` v1.50 + src/CLAUDE.md v2.46. Prior entry below.)
> **Last Updated (prior):** July 27, 2026, latest same day (**SHOT-OUTCOME DISTRIBUTION — §5.Z.17's residual, the
> named A4a blocker, FIXED AND MEASURED.** The four defects that made every outcome class except "goal"
> structurally unreachable are closed, each with its ERR filed and the spec patched where the spec was the
> defect: **ERR-006-002** — `ShotExecutor` discarded `finalDirection.z` and rebuilt the vertical from
> `sin(launchAngle)`, leaving the whole vertical half of the placement/error model inert *against the
> spec's own* `finalVelocity = finalDirection × kickSpeed` (§3.5.7); conformed, with the §3.5.6
> launch-tilt aim composition. **ERR-006-003** — the error cone was not a cone: angular error mapped to a
> **fixed 0.128 m/° at every range** (the spec's own reference-anchored form was 0.35 m/°, correct only at
> exactly 20 m); now `tan(err) × distance` at the goal plane, which reproduces the spec's 20 m value
> exactly and misses wide from range. **ERR-001-004** — the spec's own §3.1.10.3 pseudocode gated EVERY
> boundary test behind `z < 0.22 m`; gate removed from `CheckBoundaries` AND `IsOutOfBounds` (Law 9/10) —
> **the goal has a crossbar**, an airborne crossing adjudicates at the crossing (goal under the bar,
> out above/wide, throw-in in the air). **ERR-003-007** — the empty-TODO `OnAgentCollision` is live:
> `BallCollision.ApplyAgentDeflection` (#1 §3.1.10.1 `BodyPartCoefficients`, first consumer), gated
> Controlled-out / sub-`[GT] AgentDeflection.MinBallSpeedMps`-out, with the approaching-only response as a
> **stateless** self-block guard — no cooldown, no schema bump. Plus the `ShotWorldAdapter` pressure query
> live (was hardcoded `0f`; reuses the first-touch `PressureEvaluator` with the §5.Z.14 un-mirror) and
> `MIN_GOAL_VISIBILITY` 0.05 → 0.12 (it equalled the `GOAL_OPENING_MIN` floor, so the SHOOT gate could
> never fire). **Measurement drove two design reversals (AR-3):** the deflection gate was designed at 18 m/s
> against an assumed 20–35 m/s shot band — measured shots run **12–21 m/s**, so 18 would have made almost
> every shot unblockable (re-anchored to 10, with reception protected by GEOMETRY: the 1.0 m first-touch
> trigger reach sits well outside the ~0.4 m hitbox and a ball cannot jump the gap in one 60 Hz tick below
> ~35 m/s — pass speeds reach 28, so no speed gate can separate pass from shot); and the acceptance
> scenario's first draft failed its own determinism predicate by interleaving its two engines — the
> documented §5.Z.7 process-static-EventBus property, reproduced before the scenario ever tested the fix.
> **Measured over 3 full matches, same seeds pre/post: goals 15.3 → 12.3 per match, goals/shot 0.24–0.29 →
> 0.14–0.25, fast-ball body deflections 0 → 560–612 per match.** Every mechanism is now real; the remaining
> mass is NOT these mechanisms and is recorded, not fixed: **shot volume** (59–70/match, ~2.5× football — a
> DT-selection/possession-churn property `MIN_GOAL_VISIBILITY` barely dents) and **shot speed** (means
> 7–10 m/s vs football's ~25 — #6 `VFloor`/`VCeiling` × #8 `PowerIntent` shaping), which keeps shots on the
> ground (the new crossbar rarely bites) and hands keepers easy contacts they still rarely hold (§7.5).
> Locked by `match-engine-shot-outcomes` (#19 ScenarioRunner, Tier B, 4 seeds × 9 min, ~59 s) — **3 of 8
> predicates fail on the pre-fix engine, verified by executing the scenario in a worktree at the pre-fix
> commit** (the over-bar crossing adjudicated as *nothing* — `cue=None` — the under-bar crossing scoring
> nothing, deflections exactly zero); two airborne-adjudication predicates are scripted-stimulus probes
> because natural airborne line-crossings above 1 m are rare in 36 min of play (a natural floor would be
> flaky for the wrong reason — recorded un-asserted instead). Plus 17 unit locks, two tests inverted with
> intent preserved (they encoded the old z-gate contract — the Phase-H class), and the env-gated
> `ShotOutcomeDiagnosticTests` instrument (`TD_SHOT_DIAGNOSTIC=1`). **Full dotnet gate: PASSED, 0
> failures; no `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no
> draw-order change** — digests move for any match containing a shot or an airborne crossing, as intended.
> **A4a remains gated on match realism, but its named blocker is discharged; the next levers are shot
> volume, shot speed, and the keeper's catch/parry conversion.** See
> `docs/tracking/shot-outcome-distribution-design.md` + `match-engine-design.md` §5.Z.18 +
> `spec-error-log.md` v1.49 + src/CLAUDE.md v2.45. Prior entry below.)
> **Last Updated (prior):** July 27, 2026, latest same day (**GOALKEEPER SAVE PIPELINE — §5.Z.15's named lever,
> measured and discharged. Three correctness defects fixed; the goal rate barely moved, and that is the
> result.** §5.Z.15 recorded the next lever on the engine's ~4.7×-football goal rate as *"the quality of
> the goalkeeper's save, not further shot or finishing tuning"*. That framing carries a premise — that
> saves happen and are merely poor. **They did not happen.** Measured over three full 90-minute matches,
> the keepers made **zero** hand contacts with the ball across all six keeper-matches. "Save quality" was
> not a low number; it was undefined. **Nothing in the tree could have said so, because no instrument had
> ever reported a goalkeeper statistic of any kind** — the ERR-030-014 class again, one level further in.
> New env-gated `GkSaveDiagnosticTests` reports the pipeline as a **funnel** (`armed → SAVE committed →
> Anticipate → Diving → Airborne → contact → caught`) because a funnel localises WHERE a chain breaks
> instead of only reporting its end empty; every stage up to and including the dive fired healthily
> (14–41 commits, 13–31 dives a match) and the chain ended at **contact, at exactly zero**. Three defects,
> each independently sufficient: **ERR-011-003** — the dive had **no direction**
> (`ComputeDiveDirectionLateral`'s only non-zero branch is gated on `SaveIntent.DeflectionTarget`, which
> the engine's sole producer sets `null`; measured mean `|diveDirectionLateral|` = **0.000** across every
> dive ever launched, with the envelope's closest approach to the ball **2.75 m short** over a whole
> match — not a near miss, the keeper dived straight up on the spot. The cause is a conflation:
> `DeflectionTarget` is where the keeper wants to PUT the ball, not where it should DIVE); **ERR-011-004**
> — a catch was **arithmetically impossible**, since `OnShotExecutedEvent` had zero callers in production
> *or tests*, pinning `reactionWindowAchieved` at 0 and capping §3.5.1's blend at a **measured 0.630** for
> a PERFECT keeper against `CatchThreshold` 0.78; **ERR-011-002** — the keeper **woke for the wrong end of
> the pitch** and never stood down (the orchestrator computed the third the keeper's own team ATTACKS and
> passed it to a state-machine parameter documented as the OPPOSING team's — the §5.Z.12 per-side-pair
> class — while `Anticipate` had no exit but a dive, so keepers held it **76–92% of every match**).
> **Measured effect: dive direction 0.000 → 1.000, best miss 2.75 m → −0.07 m, contacts 0 → 15, Anticipate
> share 76–92% → 11–18% — and goals per match 15.3 → **15.3**, i.e. UNCHANGED, against football's ~2.7.** Three genuine
> defects, each of which had to be fixed before a save was possible at all, are worth about **one goal a
> match**. The named lever was real and is now spent; **it was not where the mass is** — the same shape as
> §5.Z.9 and §5.Z.11, where the measurement refuted its own brief. Locked by the new
> `match-engine-goalkeeper-saves` acceptance scenario (#19 ScenarioRunner, Tier B, 4 seeds × 15 min, 56 s),
> which asserts **reachability** stage by stage and deliberately pins **no** save percentage and **no**
> goal rate — a band here would pin a number this pass did not earn. **11 of its 12 predicates fail on the
> pre-fix engine, verified by executing it against reverted production files rather than inferred**, three
> at exactly zero. **Full dotnet gate: PASSED, 0 failures** (match-engine 358 → 360 passed); **no
> `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, and no change to the draw
> order.** **RECORDED, NOT FIXED — and this is now the honest next lever, each verified against source:**
> a shot **essentially cannot miss the goal** (aim is hardcoded to `u ∈ {0.1, 0.9}`, i.e. **0.732 m inside
> the post**, against ~2.25° of typical angular error where >5.73° is needed — and `ShotExecutor` never
> reads `finalDirection.z`, so the entire vertical half of the placement and error model is inert); there
> is **no crossbar** (`BallCollision.CheckBoundaries` gates EVERY boundary test, goals included, behind
> `z < Ball.Diameter` = 0.22 m, so a ball crossing the line airborne is neither a goal nor out of play —
> the goal is 7.32 m wide and of unbounded height); and there are **no blocked shots**
> (`BallCollisionHandler.OnAgentCollision` is called in production and its body is an empty `TODO`; posts
> are non-physical). In football roughly **30% of shots are blocked and 30% miss the target**; here both
> are approximately zero, which is a larger multiplier on the goal rate than anything a goalkeeper does.
> **A4a remains blocked — but the reason is now specific: the shot-outcome distribution, not the keeper.**
> See `docs/tracking/goalkeeper-save-pipeline-design.md` + `match-engine-design.md` §5.Z.17 +
> `spec-error-log.md` v1.48 + src/CLAUDE.md v2.44. Prior entry below.)
> **Last Updated (prior):** July 27, 2026, latest same day (**TRACK C PHASE B IS COMPLETE — B3, B4 and B6
> landed and `PM-1` (a playable match) is REACHED.** A person can now open a browser on the running
> client and watch a real match — live pitch, clock, score, period, restart captions — change a team's
> mentality / pressing / passing and see it queued and applied on a tick boundary, substitute, pause /
> resume / run at 1–10×, and read live statistics that keep serving after full time. **B3 (#37 T1)** is
> the read-only per-tick ledger tap: a `TickLedgerSnapshot` the engine fills in the Snapshot phase,
> **after `SerializeLedger` and before the bus resets the tick — the only moment the records both exist
> and are identified with a tick** — copying rather than indexing the process-static ring, so
> "current-tick scoped" is structural rather than documentary, and sized from `EventQueueCapacity` so
> overflow is impossible by construction. It reuses `SerializeLedger`'s own canonical-order walk
> (extracted to `EventLedger.BuildCanonicalOrder`), so the digest bytes and the observer cannot drift
> apart; and the §3.2 routing table branches on `EventRegistry.GetOrdinal<T>()` rather than a local
> ordinal table, so it cannot fall out of step with Appendix A. **`GetOrdinal` now calls
> `EnsureInitialized()` first** — `EventOrdinalCache<T>` is a separate static-generic type, so a first
> caller would otherwise read 0 for every type and silently match nothing: the static-init-order trap
> this project has now hit three times. Surfaced **ERR-037-002** — §3.4 states the territorial split as
> two strict inequalities and then requires it to be **total**; both cannot hold at exactly `x == L/2`,
> which is not a limit case but where a kickoff parks the ball for many consecutive ticks. **B4 built
> two of its three items and refused the third:** `FrameInterpolator` (speed-aware alpha, because at 3×
> the same wall-clock covers three times the simulated time; and **snap-not-smooth across a
> discontinuity** — a restart teleports the ball, a substitution swaps who occupies a roster slot, and
> blending either draws a glide where the truth is a jump) and `FollowBallCamera` (dead-zone trailing,
> `1 − e^(−rate·dt)` **proven** frame-rate-independent by step subdivision rather than asserted, and a
> clamp that CENTRES when the view is wider than the pitch instead of returning whichever crossed bound
> compared last). The third, a live-stats accumulator, **is #37's aggregator** — a second one would be
> the parallel-surface trap. **B6's finding is that the obvious implementation was the wrong one:**
> extending `LiveMatchServer` would have given the spectator surface a mutation channel, which is
> exactly what ERR-038-001 and the interactive-client AR-1 H-2 rejected — the streamer holds the engine
> and that server holds no engine reference *by construction*. So the mutating surface is a new
> host-free assembly `src/match-client-web/` **above** `match-client-core`, with three routes carrying
> three privileges (reads change nothing; `/playback` changes *when* ticks happen, never what is in
> them, so it never enters the replay log; `/intent` alone mutates, and only through the tick-stamped
> `ManagerCommandQueue`) — each asserted against the command queue rather than by inspection. Router
> and transport are separate types, so every routing decision is a pure function under test and the
> socket code decides nothing. It also needed a genuinely **new seam**: #37's every-tick contract cannot
> ride the pre-tick hook, which is set-once, already taken by the command drain, and also fires from
> `ServiceOnce()` where no tick advances — so `LiveMatchStreamer` gains a read-only
> `SetPostTickObserver` that **disarms and latches** its first exception rather than killing the sim
> thread (the pacing loop does not guard `TickOnce`, and a derived statistic must not be able to end a
> match — nor be swallowed, since a frozen report reads as merely stale). Governed by the new
> `docs/tracking/browser-match-client-design.md`. **Then the two mechanical layer guards failed, and
> correctly:** both `NoOtherAssemblyReferencesMatchAnalytics` and `NoOtherAssemblyReferencesTheUiFramework`
> were written as *"nothing references me"* while the invariant each names is *"no **sim** assembly
> references me"* — the first legitimate consumer exposed the gap. Narrowed to a sanctioned-consumer
> allow-list **plus** an explicit never-reference list naming every sim assembly, so growing the
> allow-list to quiet a red test still fails: **stricter than before, not looser.** **Full dotnet gate:
> PASSED, 0 failures** (match-analytics 24 → 54, match-client-core 22 → 45, new match-client-web 34).
> **What PM-1 does NOT claim:** it is a statement about the client, not about the match it shows — the
> engine's goal rate still runs ~4.7× football's and its home/away asymmetry ~50× football's home
> advantage (§5.Z.11/§5.Z.15), both unchanged and neither blocking. Three PM-1 surfaces are
> deliberately thin and recorded rather than dropped: team selection is `MatchSetup` in code (a
> new-game screen is roadmap C4), `SetPlayerTactic` returns **501** rather than assembling a per-agent
> tactic from ten defaults the manager never chose, and the post-match report is the live statistics
> panel continuing after full time rather than a dedicated screen. **Next: Phase C — #44 discipline,
> then the season and new-game screens; the objective is PM-2.** Prior entry below.)
> **Last Updated (prior):** July 27, 2026, later same day (**Documentation sync pass — no code, no spec, no gate
> run.** Reconciled the root docs against two code landings that shipped earlier the same day (both on
> `path-to-playable-roadmap.md` Track C/S and already recorded there and in `spec-error-log.md`, but never
> folded into this file or `README.md`): **Match Analytics #37 T0** (roadmap item B2) gave that spec a
> `src/match-analytics/` assembly for the first time — value types (`MatchStatline`/`AdvancedStatline`/
> `StatPoint`/`MatchAnalyticsResult`, all copy-not-wrap and gated at construction) plus the pure, stateless
> `XgLocationModel`; it surfaced and resolved **ERR-037-001** (§4.1's reference list omitted the
> Ball-Physics `[CROSS]` reference Appendix A's `GOAL_WIDTH_M` tag requires — Appendix A won, so the
> asmdef references `TacticalDirector.BallPhysics` directly rather than re-declaring a third copy of
> 7.32 m). This moves the "APPROVED specs with no assembly" count from 23 to **22** and the assembly
> count from 29 to **30** — both were stale in the PROJECT IDENTITY section and the assembly map table
> below (which was missing a `match-analytics` row entirely). Also landed: **Track C B1**, a richer
> `LiveMatchFrame`/`MatchFrameView` observation frame (per-agent booking/sent-off/substitute state,
> per-team substitutions used, derived match period, last restart) for the interactive Unity client —
> no `SNAPSHOT_SCHEMA_VERSION` change (the new engine fields are either read-only copies of existing
> serialized state or within-tick fields reset every `RunInputPhase`, so no new cross-tick surface was
> added). Neither landing touched this file, `README.md`, or `docs/tracking/file-manifest.md` at the
> time, which is what this pass corrects; `file-manifest.md`'s "Current Specification Folders" table was
> found separately stale (stuck at 26 rows / "All 26 spec folders now exist", predating the #27–#54 wave)
> and is fixed in the same pass. Prior entry below.)
> **Last Updated (prior):** July 27, 2026, later same day (**ALL TEN APPROVED — the specification phase is
> CLOSED. `SPEC_INDEX.md`: 53 APPROVED / 0 IN REVIEW / 0 NOT STARTED.** Lead-developer R-01..R-05 sign-off
> granted on #53, #35, #46, #36, #54, #47, #48, #50, #51 and #39, with the **23 back-props filed and
> RESOLVED atomically with the flips** (`spec-error-log.md` v1.47) per each spec's own pipeline step 6.
> **Docs only: no code, no `src/` change, no gate run, and no format version bumped today.**
> **Landing the back-props together is what exposed the wave's most consequential defect, and filing them
> one spec at a time never could have:** **#30's pinned day-advance tick order was not implementable as
> written.** `ERR-030-007` had been filed **twice** — for #42's academy step and #32's scouting step, at
> two separate approvals — leaving **two step 7s, two step 8s and an orphaned `AdvanceDay` line** in a
> sequence **six approved specs cite by number**. Neither approval could have seen it alone. Reconciled
> under **ERR-030-022** in a new §3.3.1 (#32 → 9, #35 media expiry → 10, #54 tenure → 11, `AdvanceDay` →
> 12), which also had to resolve a **conflict between two of this wave's own back-props**: ERR-030-020
> (#53) requires its step to precede its same-day consumers and says to renumber below it, while
> ERR-030-022 requires the cited slots not to move — jointly unsatisfiable by inserting a new step 1.
> **Resolved by numbering the facility step 0**; a step numbered zero is unusual, but a renumber that
> silently invalidates six approved specs' citations is worse, and patching all six would edit approved
> text for a numbering preference rather than a design need. **`ERR-030-009` is a duplicate too** (#45's
> `JobSecurity` band; #44's availability filter) — both duplications preserved verbatim as frozen records
> and documented as errata. **Three entries change approved contracts rather than pointers:**
> **ERR-048-001** corrects a **contradiction between two MUSTs inside APPROVED #48** (FR-MP-025 forbids
> `#51 → #48`; FR-MP-027 required #51's catalogue to be keyed on #48's `CueId` — jointly impossible, and
> an assembly cycle waiting to happen); **ERR-045-002** re-points `FR-BD-012` from #30 to #54, closing a
> MUST that delegated the sacking decision to a spec containing no such rule; **ERR-033-003** replaces a
> per-producer morale field with a producer-agnostic one, **filed jointly by #35 and #46**. Three entries
> are ◑ spec-text-first with a named future bump, and **#54's `SEASON_STATE_FORMAT_VERSION` bump is
> decided to combine with #45's queued one** so saves face one refusal boundary rather than two. **Also
> fixed in passing:** #30's `section-2.md` and `section-3.md` each carried **two bare `**Last Updated:**`
> labels** with different content. **The consequence to carry forward:** with the spec phase closed,
> **23 of 53 APPROVED specs have no `src/` assembly** — *"the spec is APPROVED"* now says nothing about
> whether code exists, and that is true of 43% of the registry. Prior entry below.)
> **Last Updated (prior):** July 27, 2026 (**TEN DESIGN SUPPLEMENTS PROMOTED TO FULL SECTION FILES — the
> pre-promotion backlog is empty. Docs only: no code, no `src/` change, no gate run.** Every converged
> `docs/tracking/*-design.md` supplement that lacked a spec folder now has an 11-file set at
> `Status: IN REVIEW`: **#53** Club Infrastructure (`FR-IN`), **#35** Media & Press (`FR-ME`), **#46**
> News/Inbox & Man-Management (`FR-NW`), **#36** National Teams (`FR-NT`), **#54** Manager Career &
> Reputation (`FR-MC`), **#47** New-Game Setup & DB Editor (`FR-ED`), **#48** Match Presentation Depth
> (`FR-MP`), **#50** Save Migration & Versioning (`FR-MG`), **#51** Audio & Sound Design (`FR-AU`),
> **#39** Steam Packaging & Release (`FR-PK`). `SPEC_INDEX.md` gains ten registry rows: **43 APPROVED /
> 10 IN REVIEW / 0 NOT STARTED**. **Each carries a recorded section-file PASS-1 adversarial review + fix
> pass and an AR-2 sweep to CONVERGENCE (§9.4.1), and each stops at `IN REVIEW` deliberately** — G1 is
> closed, G2 (back-props) lands atomically at approval, and **G3, lead-developer R-01..R-05 sign-off, is
> a human authority and is not self-grantable**, exactly as every supplement's own §12 pipeline states.
> **The finding that generalises is an id-collision class, not a per-spec defect:** three supplements
> (#35, #46, #53) proposed `ERR-` ids that had **already been filed** — #30's T2 landing filed rows the
> same day those supplements were written, and nothing cross-checks a *proposed* id against
> `spec-error-log.md` — so a supplement's id is a suggestion to re-verify at promotion, not a
> reservation; reassigned to ERR-030-022/023, ERR-030-024 and ERR-029-003, each recorded as an M finding.
> The other seven verified their ids free against the log **and** every spec folder, and say so.
> **A second cross-wave pattern, recorded because ten repetitions is a process signal rather than ten
> slips:** in all ten, the `[GT]` budget ceilings declared in §6.3 were missing from the Appendix A
> catalogue — the #45 PASS-1 M-2 defect, reproduced independently each time, an artifact of §6 being
> authored before the appendices with nothing walking back. **Findings worth carrying forward:** #51's
> KD-1 resolves a genuine contradiction in **APPROVED** text (#48 forbids `#51 → #48` while FR-MP-027
> requires #51's catalogue to be keyed on #48's `CueId` — jointly impossible, and it would have surfaced
> as an assembly cycle after both were approved; ERR-048-001 corrects it, changes no code, and is
> therefore the back-prop most likely to be deferred at the price of the next implementer building the
> forbidden reference in good faith); #39's KD-2 inverts the release gate because **this repo's CI is
> skip-open** — `unity-tests` is gated on a secret and reports success when it is absent, so a green
> pipeline is compatible with nothing having been built or tested; and #50's KD-2 records that **rosters
> are regenerated rather than saved**, so a format-only migrator would migrate 25 versions perfectly and
> still hand the player a different squad. **Three specs file no back-props at all** (#48, #39, and the
> #37/#44/#46 class), stated as evidence of correct layering rather than left as an empty table.
> **Two numbers outside the roadmap's original #27–#51 range are promoted here for the first time:**
> #53, because four APPROVED specs consume a facility model they all attribute to #40 whose scope
> excludes it; and #54, because #45's `FR-BD-012` MUST names #30 as deciding a sacking and #30 contains
> no such rule. **Deliberately NOT done:** no sign-off claimed, no back-prop filed, no `src/` touched,
> no dotnet gate run (nothing compiled changed), and `management-layer-spec-roadmap.md`'s wave blocks
> left intact — they are the reasoning that produced the order, and rewriting them in the past tense
> would destroy the record of why each spec sits where it does. See `SPEC_INDEX.md` NOTES and the
> roadmap v0.7 header note.)
> **Last Updated (prior):** July 27, 2026 (**SEASON-BOUNDARY ROLL LANDED — #30 T3 / path-to-playable A5.
> Phase A is complete and PM-2-sim — a playable season, the objective — is REACHED.** A career no longer
> ends after one season: `SeasonLoop.RollToNextSeason()` finalizes the table, evaluates the board,
> derives the next seed, regenerates the schedule and calendar, and resets — and two careers from one
> seed now agree on **both** seasons' final tables, with a save taken at the boundary restoring to the
> same continuation. **The transform is pure in the prior `SeasonState`** — no clock read, no draw —
> which is what makes FR-SN-029's restartability claim non-trivial rather than incidental: deriving the
> next calendar from the world clock instead of from the old calendar would have made the roll depend on
> *when the client happened to call it*. `SeasonRollOutcome` is the producer record a career screen needs
> ("you finished 14th, the board wanted 10th, your job security fell"); job security is gained flat when
> the objective is met and lost **per league position short** when it is not, because a flat penalty would
> make missing by one place identical to finishing bottom. The (a') #43 promotion/relegation and (b') #40
> finance insertion points, and (d) #28's age advance, are **declared positions, not interfaces**
> (FR-SN-034 / FR-LW-031). New `[FIXED] SEASON_ROLL_SEED_DOMAIN` + three `[GT]` rows; no
> `SEASON_STATE_FORMAT_VERSION` change (the calendar was already serialized). **Full dotnet gate: PASSED,
> 0 failures (whole tree green; season-save 240 → 261, SDK 8.0.129 via apt).**
> **An adversarial review over the landing then found 1H+3M+2L, all fixed.** **H:** `AdvanceDays` bounded
> the world clock only while a season was IN PROGRESS. Once complete it was unbounded — so a client
> walking the close season past the day the next season opens reached a career with **no way forward**:
> the season cannot be played (it is complete) and cannot be rolled (the derived calendar now opens in
> the past), the world clock only moves forward, and the stuck state **saves and reloads cleanly**.
> Reproduced: complete on day 42, `AdvanceDays(57)`, both routes refuse, save/load round-trips the
> wreck. Fixed by generalising the existing KD-4 guard — post-season the bound is the day the next
> season opens, derived through the same `ShiftCalendarToNextSeason` the roll uses, so there is one
> derivation and two readers. **M-1:** the step (b) job-security arithmetic re-derived the pass/fail
> rule as `finalPosition <= targetPosition` instead of calling `BoardObjective.IsMetBy` — a second copy
> of board policy, sitting on the composition root rather than on `BoardState` (whose own doc already
> anticipated "the season-boundary pass/fail evaluation"). When #45 extends the objective model, the
> reported verdict and `IsOnTrack` would have moved while the job-security consequence silently stayed
> on the old rule. Moved to `BoardState.EvaluateAtSeasonEnd`, so one predicate drives verdict, running
> read and penalty. **M-2:** `SecondSeason_DiffersFromTheFirst` asserted a **disjunction** (table
> differs OR schedule differs) whose table half is always true — season 2 quick-sims against a
> different seed — so the schedule half, the thing the test is named for, was unreachable. Proven by
> perturbation: making the roll reuse the OLD seed for `FixtureScheduler` (every season replaying the
> identical fixture list) left the whole suite green. Now asserted separately, and the perturbation
> fails it. **M-3:** a season saved AFTER the roll had zero coverage — the shipped restartability test
> saves BEFORE it — while the roll installs a schedule and calendar the codec has never been shown,
> and "a roll installs a state Encode writes but Decode refuses" is a defect this exact path produced
> once already at T1. **L:** `EnginePlayedFixtures` / `MatchOutcomes` silently span the boundary, and
> the former's doc still claimed the per-season semantics T3 took away. Three new locks, each proven
> non-vacuous by perturbing its fix; season-save 258 → 261, full gate re-run green. **The second L —
> `ShiftCalendarToNextSeason` sitting on the composition root — was then fixed too, as
> `SeasonCalendar.ShiftedToNextSeason`:** pure calendar arithmetic now lives on the type that owns
> calendars, which also drops two array copies and a re-validation of an ordering that adding one
> constant to a strictly-ascending sequence provably preserves. What stays on the loop is
> `NextSeasonCalendar()` — the choice of the `[GT]` close season, bound in one place and read by both
> `AdvanceDays` and `RollToNextSeason`. Two new gates with it (a single-round calendar still moves
> forward; a `breakDays` of zero, and a shift that would carry the final round past `uint.MaxValue`,
> both refused). Season-save 261 → 263, full gate re-run green.**
> **The landing's finding is the sixth consecutive C5 hit, and the sharpest illustration yet of the
> project's own "tests that verify the composition runs, not that it works" trap — ERR-030-015.** §3.5's
> `RollToNextSeason` pseudocode regenerates `Fixtures`, resets `Table`, and advances `SeasonNumber`/`Seed`
> — but **never rebuilds `Calendar`**, whose cursor sits at `RoundCount` precisely *because* the season
> just ended. A roll implemented from the spec verbatim therefore produces a season that is
> **permanently unplayable**: `AdvanceToNextFixtureDay` throws F5 and `AdvanceAndPlayNextRound` throws, on
> every call, for the rest of the career — so the transform could not deliver FR-SN-029's multi-season
> continuity at all. **And no assertion over the rolled state's *fields* would have caught it**: the
> schedule, the table, the seed and the season number are all exactly right. It took an acceptance test
> that plays a **second** season to completion. Measured: **9 of the suite's 18 predicates fail** against
> the spec-as-written. Fixed as step **(c′)**, which shifts the OLD calendar's day mapping forward by one
> season length plus a `[GT] SeasonBreakDays` close season — chosen over rebuilding a linear calendar
> because it keeps the transform pure AND preserves a non-uniform schedule (a mid-season gap survives the
> roll instead of being silently flattened). `section-3.md` → v1.0, which also consolidates the **two
> stale `Version` header fields** that file carried. **What A5 does NOT claim:** PM-2-sim is a statement
> about the loop, not about the quality of what it simulates. **A4a remains gated — and not on compute.**
> Its Step 0 pilot and full corpus are ~33 min and ~1.4 h, both affordable; the blocker is that the
> engine's goal rate still runs **~4.7× football's** (§5.Z.15), so a corpus fitted today would calibrate
> the quick-sim to reproduce that faithfully across a whole 380-fixture league. Step 0 will not catch it
> on its own — it asks *"do the strength extremes separate?"*, and it passed at 25–0. The honest next
> lever is the quality of the goalkeeper's save, not further shot tuning. See
> `docs/tracking/path-to-playable-roadmap.md` v0.8 + src/CLAUDE.md v2.42.)
> **Last Updated (prior):** July 26, 2026, later same day (**FOUL & DISCIPLINE BALANCE PASS LANDED — §5.Z.9,
> closing the §5.Z.7 item 1 finding that Phase H recorded as the most visible remaining unrealism in a
> played match.** A match no longer empties itself of players: measured over one match-equivalent of
> composed play, **480 → 21.0 fouls, 147 → 3.0 yellows and 75 → 1.0 red cards per 90 minutes** against a
> football reference of ~22 / ~3.5 / ~0.25, with no team dropping below eleven where the pre-fix engine
> reduced teams to five to seven inside nine minutes. **The headline is that the measurement refuted the
> finding's own diagnosis.** §5.Z.7 framed this as a `[GT]` threshold question; the peak qualifying-force
> distribution turns out to be bounded and narrow (p99 = 1175 N, **max 2362 N** — a collision impulse over
> `ContactDurationS` cannot exceed it), so replaying the production gate across a threshold ladder gives
> 480 fouls at 1200 N, 90 at 2000 N and **0 at 3000 N**. The threshold is a cliff, not a dial, and the only
> values in between sit on the last thirty samples of a 130 000-tick run — a setting that would read as
> calibrated while being pure noise. No cooldown rescues it either. **The real gap was the referee:** the
> model called *every* hard cross-team from-behind contact a foul, and the engine produces **seventeen of
> those per second**, so what was missing is judgement — a probability. Fixed with a force-scaled call
> probability `p(F) = min(1, FoulCallProbability × F / FoulImpactForceThresholdN)` (a harder challenge is
> likelier to be given; a hard contact is never automatically a foul), whose **single draw** also selects
> the card severity from the rescaled remainder `v = u / p` — ordinary inverse-transform partitioning, so
> there is **no new RNG stream and no `SNAPSHOT_SCHEMA_VERSION` change**. A wave-on arms no cooldown
> (arming it would silently swallow the genuine foul two ticks later), and the consumer now keeps the
> **strongest** contact of a tick rather than the first, since force now decides the call and first-wins
> would systematically under-call the hardest fouls. New `[GT] FoulCallProbability` = 0.015;
> `YellowCardProbability` 0.35 → 0.16, `RedCardProbability` 0.05 → 0.011, `FoulCooldownTicks` 60 → 180.
> **Calibration required a live run, not the offline sweep, and that generalises:** the sweep pointed at
> 0.025, where a real match measured 37.5 fouls per 90 min — giving 20× fewer fouls means 20× fewer
> restarts, so play runs on and the qualifying-contact count *rose* from 36 000 to 129 000 over a
> comparable corpus. An offline gate replay finds the right shape cheaply; it never gives the value.
> **Acceptance is the test the tree did not have:** `match-engine-discipline-plausible` (#19 ScenarioRunner,
> Tier B, 6 seeds × 9 min, ~52 s) asserts foul/yellow/red rates in plausibility bands, that **no team is
> reduced below nine players** (per seed, never aggregated — one abandoned match must not average away),
> and that cards stay a minority of fouls; **9 of its 10 predicates fail on the pre-fix engine**, each by
> more than an order of magnitude. Plus 8 unit locks in `MatchEngineFoulCardTests` (probability shape,
> wave-on leaving no trace, strongest-wins capture driven through the real consumer), the env-gated
> `FoulRateDiagnosticTests` instrument (replays the gate offline across a ladder so one composed run
> yields the whole curve), and `MatchEngine.TestOnly_SetCollisionObserver` — the seam that made the force
> distribution observable at all, since the collision system takes exactly one consumer and it is private.
> **Full dotnet gate: PASSED, 0 failures (whole tree green; match-engine 333 → 342, SDK 8.0.129 via apt).**
> **One finding recorded and deliberately NOT fixed** (new OPEN ISSUES entry): the **contact rate itself**
> — 17 hard cross-team from-behind contacts per second, on 20% of all ticks, is not football. The
> refereeing model now sits plausibly on top of it, but the stream underneath is wrong, and it is the next
> thing to look at for match realism (most likely #12 agent spacing or #3's 60° `BehindDotThreshold`
> cone). See `docs/tracking/foul-discipline-balance-design.md` + `match-engine-design.md` §5.Z.9 +
> src/CLAUDE.md v2.41.)
> **Last Updated (prior):** July 26, 2026, latest same day (**ROOT-DOC RECONCILIATION — `CLAUDE.md` + `README.md`
> re-based on the actual repo state; no code, no spec, no tracking-doc change.** The two root documents had
> drifted badly behind the tree they describe: this file's body still said *"All 20 Stage-0 specifications
> are APPROVED, plus the first Stage-1 forward spec #21"* and *"Ball Physics (#1) and Agent Movement (#2)
> have initial implementations"* — against a real state of **43 APPROVED specs (0 IN REVIEW / 0 NOT
> STARTED)** and **29 production assemblies**; its REPO STRUCTURE tree listed 8 spec folders and 2 `src/`
> assemblies out of 43 and 29, and named none of `tools/`, `docs/design/`, or the Unity project shell.
> `README.md` was pinned at **July 14, 2026 / 26 specs**, twelve days and seventeen approved specs stale,
> and its status text still described `SNAPSHOT_SCHEMA_VERSION` 15 (actual: **18**). **Corrections that
> change what an agent would do:** (1) a new **`src/` assembly map** — the folder-name→spec mapping is
> *not* inferable, since #27 lives in `player-database`, #28 in `player-progression`, #30 in `season-save`,
> #38 in `ui-framework`, #23/#24/#25 inside `positioning-ai`, and #26 inside `tactical-instructions`, while
> `match-engine` / `match-viewer` / `match-client-*` / `project-constants` are not numbered specs at all;
> (2) the **13 APPROVED-but-unimplemented specs** (#29, #31–#34, #37, #40–#45, #49) are now stated in both
> files, because "approved" had become a misleading proxy for "a consumer exists" — the single most
> load-bearing fact about the current state, and the premise of `path-to-playable-roadmap.md`;
> (3) the **design-supplement governance class** (42 `docs/tracking/*-design.md`) is documented for the
> first time — it appears in no root doc, yet it is where `match-engine-design.md` and every pre-promotion
> spec note live; (4) the two **roadmaps** (`management-layer-spec-roadmap.md` — which specs to author;
> `path-to-playable-roadmap.md` — which code to land) added to TRACKING DOCUMENTS; (5) three new rows in
> *Things That Have Gone Wrong Before*, each earned: never-compiled surfaces, **tests that verify the
> composition runs rather than that it works** (the ERR-030-014 class — the capstone asserted tick count,
> cadence, finiteness, bounds and digest advance, every one of which holds for a match in which nothing
> happens), and home-team-only worked examples; (6) the *"When Writing Code (Future — after all 20 specs
> approved)"* heading de-tensed — that future arrived on May 19, 2026. Also fixed: a **second bare
> `**Last Updated:**` label** at the June-10 entry deep in this header chain, which made the block
> self-contradictory about its own currency (now `(prior)`). **Deliberately NOT touched:** the historical
> header entries and OPEN ISSUES bodies (frozen records per this project's own "historical rows preserved
> verbatim" convention — they are re-dated by nothing here), and `src/CLAUDE.md`, whose **Assembly Layer
> Taxonomy is itself now stale** (it lists `UI | (Stage 1+ — not yet specified)` while #38 is APPROVED and
> `src/ui-framework/` exists, and omits `match-engine`, `season-save`, `player-database`,
> `player-progression`, `match-viewer`, and `match-client-core`) — recorded here as a follow-up rather than
> edited, since it is the authoritative coding guide and its taxonomy is a Spec #20 §3.5.2 reproduction
> that should be corrected against that spec, not against a folder listing. **The dotnet gate was not
> re-run in the authoring environment** (no SDK), so the gate claims restated here were quoted from the
> last landing's record — but CI subsequently ran the full Linux shim gate green on this branch
> (10 checks pass, Unity tests skipped for want of a license), which re-verifies them independently.)
> **Last Updated (prior):** July 26, 2026, later same day (**MATCH-ENGINE POSSESSION BOOTSTRAP LANDED — §5.Z Phase H,
> roadmap item A4b. ERR-030-014 is CLOSED: a production match now plays.** The engine that had never in its
> history put the ball in motion now kicks it, contests it, works it into both penalty areas and scores.
> Measured over six seeds × 9 minutes: peak ball speed **16.2–17.2 m/s** (was 0.00), peak height **2.45–2.91 m**
> (was 0.11 = the resting centre height), possession held **10.5–20.9%** of ticks (was 0%) and changing hands
> **262–298 times** (was 0). **The fix is five seams, not the one the finding anticipated — and four of the
> five were found by RUNNING the composed engine, each invisible until the previous fix let play run
> further.** (1) **KD-H1 restart taker award:** `ApplyRestart` now takes an `awardedTeam` and every call site
> declares one, so no restart can silently grant the ball to nobody — kickoff to the home side, the second
> half to the other (Law 8, `[DERIVED]` from the first so they cannot drift together), a goal to the
> conceding team, throw-in/corner/goal-kick to `RestartResolver`'s already-computed award, offside to the
> defenders, a foul to the victim's team; the taker is that team's nearest **non-sent-off** agent.
> (2) **KD-H2:** possession assignment, NOT imparted velocity — `ApplyKick` stays the sole producer of ball
> motion. (3) **KD-H3 loose-ball pickup:** `RunFirstTouch` correctly refuses a ball that is not moving (a
> still ball is not an incoming receive, and #4's control-quality model is a function of incoming velocity),
> so a separate `RunLooseBallPickup` claims a ball that has come to REST — gated on the exact complement of
> first touch's speed gate, so the two can never both fire, and #4's contract is untouched.
> (4) **KD-H5 / ERR-008-014:** the Decision Tree had **no action at all that fetches a stationary loose
> ball** — PRESS targets an opponent, MOVE targets the formation slot, INTERCEPT bailed at its
> minimum-ball-speed gate — so play died the first time a pass ran out of momentum beyond INTERCEPT's ~10 m
> reach, with all 22 agents circling their slots around it; fixed by emitting a loose-ball **collect** as the
> SOLE off-ball option for one **host-designated** collector per team (host-designated because only the host
> knows who is sent off — a perception-derived "nearest teammate" rule deadlocked on a frozen red-carded
> agent eleven teammates were deferring to; and sole-option per ERR-008-013's AR-4, since the collect scores
> ~0.35 against MOVE's ~0.21, a gap **inside** the ±0.15 composure-noise band, so as a competitor the
> collector visibly dithered and never arrived). (5) **KD-H4 / ERR-008-015:** `NotifyActionComplete` had
> **zero production callers**, so every agent that passed or shot was frozen in EXECUTING for the rest of
> the match — no decisions, no movement commands, and no way to release the ball it was still holding; the
> composition root now closes the lifecycle (it is the only layer that sees both the trees and their
> executors), and `OnPossessionChanged` no longer interrupts a holder whose executor is still in flight.
> **Acceptance is the test the tree did not have:** `match-engine-play-develops` (#19 ScenarioRunner, Tier B,
> 6 seeds × 32 400 ticks, ~90 s) asserts the ball is kicked and airborne, possession is held and contested,
> **play is still alive at the final tick**, and across the spread the ball reaches both penalty areas and
> goals are scored — **every predicate fails on the pre-Phase-H engine**, and `play-still-alive-at-final-tick`
> caught two of the four stalls, both of which let play run for eight or nine minutes before dying. Plus a
> two-run byte-identical digest chain over 6 000 ticks of LIVE play (the Phase F capstone matched two
> 600-tick chains, but 600 ticks of the old engine were 600 ticks of nothing). New
> `MatchEnginePossessionBootstrapTests` (11) + `OptionGeneratorTests` (+3). **21 existing tests were updated
> — most of them encoded the old "a restart clears possession" contract, which is precisely the contract
> that made the deadlock possible.** **Full dotnet gate: PASSED, 0 failures (whole tree green; match-engine
> 322 → 333, SDK 8.0.129 via apt).** No `SNAPSHOT_SCHEMA_VERSION` change (nothing new is serialized).
> **Two findings recorded and deliberately NOT fixed** (design note §5.Z.7): the foul heuristic issues
> **~7 red cards per 9 minutes** — consistently, across seeds, i.e. every player would be dismissed inside a
> full match — which is a `[GT]` threshold question (`FOUL_MIN_FORCE_N` / `FoulCooldownTicks` /
> `RedCardProbability`) needing a foul-rate target rather than a guess folded into a correctness fix, and is
> now **the most visible remaining unrealism in a played match**; and the process-static EventBus makes
> **interleaved** engines diverge at tick 1 (sequential runs are byte-identical — verified both ways), a
> latent property of #17 §3.2.1 that was invisible only because no production event had ever been published.
> **This unblocks PM-1 and roadmap A4a** — re-run #30's KD-8 Step 0 pilot (~33 min); note it may still
> refuse, since Phase H makes matches *play*, not necessarily *discriminate by squad strength*, which is
> exactly what Step 0 exists to ask. See `docs/tracking/match-engine-design.md` §5.Z + src/CLAUDE.md v2.40.)
> **Last Updated (prior):** July 26, 2026 (**Season & Competition Loop #30 T2 LANDED — the day-advance loop + the
> round-resolution model; path-to-playable roadmap item A4 — and the same landing surfaced the most
> consequential finding on the playability track: ERR-030-014, a production match cannot develop play at
> all.** Four new files in the existing `TacticalDirector.SeasonSave` assembly (`RoundResolutionMode`,
> `RoundResolutionModel`, `SeasonLoop`) plus `src/match-engine/SquadRating.cs` — the narrow PUBLIC rating
> seam over the internal `LineupSelector` that league-bootstrap AR-4 M-1 recorded as A4's named prerequisite
> (re-implementing selection inside `season-save` was explicitly refused as the parallel-surface trap).
> `SeasonLoop` is the KD-7 **sole writer** of `SeasonState`: `AdvanceToNextFixtureDay` / `AdvanceDays` walk
> the world one calendar day at a time in the **KD-2 fixed order** (only step 9, `WorldStore.AdvanceDay`, is
> live — steps 1–8 remain documented null seams per FR-SN-034, so a no-fixture day is byte-identical to a
> bare `AdvanceDay`, FR-SN-026), and `AdvanceAndPlayNextRound(ISquadProvider)` resolves the **whole** round
> (FR-SN-012), routing the managed club's fixture through a real `MatchEngine` and every other through the
> model, applying each result in FR-SN-013's pinned table → event → mark order, and advancing the cursor.
> `RoundResolutionModel` is **keyed, not cursor-positioned** (§3.4.1): `FixtureKey(seasonSeed, seasonNumber,
> roundIndex, home, away)` folds in `DOMAIN_TAG_SEASON_LOOP` — **the tag's first draw site, discharging
> ERR-030-001** — and feeds an exp-shaped lambda pair through a *named* **inverse-CDF** Poisson quantile (one
> uniform per side, `MAX_GOALS_PER_SIDE` cap), so permuting a round's resolution order yields the
> byte-identical table (T-SN-CAL-003c). That is roadmap C1's whole point realised: a 20-club / 38-round /
> **380-fixture season resolves in milliseconds** against the ≥ 16 hours the real engine would need. New
> `SeasonLoopTests` + `RoundResolutionModelTests` + the **`season-multi-fixture` capstone** on the #19
> ScenarioRunner (season-save 179 → 240 tests (237 passed + 3 env-gated drivers skipped), incl. the capstone scenario; the capstone runs one real ~3.6-minute engine
> match — the deliberate Simulation-layer home for that cost). **Full dotnet gate: PASSED, 0 failures (whole
> tree green; SDK 8.0.129 via apt).** **Three ERRs filed. Two are the familiar shape** — a §4 architecture
> sketch another section of the same spec forbids: **ERR-030-012** (§4.5 specifies a REGISTERED
> cursor-positioned season stream, but §3.4.1 requires keyed draws for order-independence; realized as the
> keyed derivation above, and `SubsystemOrdinals.SeasonLoop = 84` deliberately **not** allocated in code,
> because an ordinal with no stream behind it is the zero-consumer phantom FR-LW-031 forbids) and
> **ERR-030-013** (§4.6's "records the `MatchResult` in `SeasonState`" is unimplementable — §2.2/Appendix B
> give `SeasonState` no outcome collection, and adding one would bump `SEASON_STATE_FORMAT_VERSION` for a
> payload FR-SN-017 forbids a consumer for; the producer record is loop-scoped, the durable record is the
> serialized table). **The third changes the plan. ERR-030-014, found by actually RUNNING A4a's KD-8 Step 0
> pilot:** all 20 full 90-minute engine matches finished **0–0** at a measured squad-rating differential of
> **±6** on a `[1,20]` scale. Characterisation over 60 000 ticks — in both a distinct-squad and a plain
> neutral configuration — found the ball's velocity **identically zero for the entire match**, never
> airborne, and **never possessed by any agent**. The cause is a closed loop, half of it already stated in
> the engine's own comment: `InitializeKickoffState` places the ball at rest (*"a kick would set it in
> motion; none at Stage 0"*), `RunFirstTouch` gate 3 refuses a touch unless the ball is ALREADY moving,
> production possession is granted only by that path (`TestOnly_SetPossessor` is documented "Not called by
> production"), and only a possessing agent can kick. No motion ⇒ no reception ⇒ no possession ⇒ no kick ⇒
> no motion. **A production match has always been a 90-minute 0–0 deadlock** — and it was invisible because
> the 321 match-engine tests each drive their own inputs per subsystem, while the one composed test (the
> 600-tick kickoff capstone) asserts tick count, AI-stride cadence, finiteness, on-pitch bounds and
> digest-chain advance: every one of which holds for a match in which nothing happens. **It verified that
> the composition runs, never that it plays** — precisely the gap the path-to-playable roadmap opened with.
> Consequences: **A4a is blocked upstream of itself** (not by its compute — measured at ~98 s/match, so the
> full corpus is ~1.4 h across four processes, well inside C1a's 9 h budget); the three round-resolution
> `[GT]` parameters ship **provisional and explicitly not fitted**, football-plausible rather than
> engine-matched; **PM-1 ("watch a match") is blocked by the same gap**, PM-2-sim is not. Owner is
> `match-engine-design.md` (new **§5.Z Phase H**), not #30, and roadmap item **A4b** (a kickoff/restart
> possession grant) now precedes A4a on the critical path — deliberately not attempted inside A4, since it is
> a behaviour change to the most safety-critical assembly, activates a large amount of never-composed code
> (C5 at its strongest), and moves every engine digest. Committed alongside: the A4a harness, the fitter
> `tools/round-resolution-fit.py`, the env-gated Step 0 and characterisation drivers (neither asserting
> current behaviour — pinning a defect would make it a contract), and the evidence record
> `docs/tracking/round-resolution-corpus.md`. Self-review over the landing found **2 M + 3 L**: no gate
> enforced "the world is ON the round's fixture day", so a client could skip the day-advance for a whole
> career and get a plausible-looking table stamped with wrong world days; and the `FullEngine` routing branch
> was reachable only by running two real matches, so a typo there would have shipped as "FullEngine quietly
> behaves like ManagedThroughEngine" (extracted to the pure `ShouldPlayThroughEngine`, all six combinations
> locked). See src/CLAUDE.md v2.39 + the path-to-playable and new match-engine-playability OPEN ISSUES entries.)
> **Last Updated (prior):** July 25, 2026, latest same day (**League bootstrap LANDED — path-to-playable
> roadmap item A3, the #47-minimal substitute (C3): a playable league now EXISTS, generated, with no
> authored data and no database editor.** `LeagueBootstrap.Generate(worldSeed, clubCount)` turns one
> seed into an N-club league — five new files in the existing `TacticalDirector.SeasonSave` assembly
> (KD-1, no new assembly; it gains a `TacticalDirector.PlayerDatabase` asmdef reference):
> `LeagueBootstrapConstants`, `ClubNameCatalogue`, `Club`, `League`, `LeagueBootstrap`. Three
> domain-separated derivations from the one world seed (KD-4 — roster / strength / season), one
> registered roster stream per club under `SubsystemOrdinals.PlayerDatabase` with `entityId = clubId`
> (so a club's BASE roster — identity + pre-strength attributes — is a function of `(worldSeed, clubId)`
> alone, independent of league size; the SHIPPED attributes are not, because the strength ramp is over
> league size, and both halves are test-locked), a seeded Fisher–Yates **strength rank** ramped into a per-club `[1,20]`
> attribute delta so the table is not 20 statistically identical teams (KD-5; `WeakFootRating`
> deliberately excluded — a `[1,5]` scale would saturate), `League` **is** the `ISquadProvider` (no
> adapter for the engine or for #30 T2), and `League.CreateSeason(managedClubId)` hands #30 a startable
> `SeasonState` through the existing `SeasonState.CreateNew`. **No new #16 domain tag or subsystem
> ordinal is allocated** — the strength permutation uses a LOCAL SplitMix64 exactly as
> `FixtureScheduler` does, so `DOMAIN_TAG_SEASON_LOOP` / ordinal 84 stay pinned to #30 T2's first draw
> site per ERR-030-001. **The load-bearing finding, caught at design time (KD-6):** `RosterGenerator`
> draws positions uniformly over four, so a 25-player squad lacks the four defenders a back four needs
> ~3% of the time per line — and `LineupSelector` refuses such a squad fail-loud, so a 20-club league
> would have failed to start **by seed**, the worst failure shape available. Fixed at the root: a `[GT]`
> position template (3 GK / 8 DF / 8 MF / 6 FW, sized against the worst case across all three shipped
> formation families) fed to a new **additive** `RosterGenerator.Generate(rng, streamIndex, clubId,
> PlayerPosition[])` overload — the position draw still runs and is discarded, so the per-player RNG
> budget, the stream layout, and the drawn-position path stay **byte-identical** (`RosterGenerator.cs`
> v1.4). Governed by the new converged supplement `docs/tracking/league-bootstrap-design.md` (v1.1 —
> AR-1 1H+2M+2L → AR-2 1M+1L → AR-3 CONVERGENCE → **AR-4 over the shipped code, 0H+2M+4L**). AR-4's two
> M findings are **forward gaps A4 would have walked into**: `LineupSelector` is `internal` to
> match-engine, so KD-7's quick-sim `Rating(club)` is unreachable from `SeasonLoop` (recorded as a named
> A4 prerequisite, with re-implementing selection inside season-save explicitly refused as the
> parallel-surface trap), and A4a's calibration harness had been placed in an assembly that cannot reach
> the `internal ApplyStrength` (corrected to `src/season-save/tests/`). AR-4 L: `MaxClubCount` 64 → 32
> plus an explicit `MaxRngStreams` coherence gate (one stream per club — at 64 it exactly filled the
> registry, and any raise would have failed *mid-generation* with a generic "registry full");
> `POSITION_COUNT` hoisted to `PlayerDatabaseConstants` so two assemblies stop carrying private copies of
> the enum's member count (the PM AR-7 M-1 parallel-surface class), locked against `Enum.GetValues`; and
> negative world-day `[GT]` values refused at read rather than wrapping to ~4.29e9. New
> `tests/LeagueBootstrapTests.cs` (27 — determinism, seed divergence, league-size independence,
> contiguous ids + globally unique `PlayerId`s, catalogue coverage/uniqueness, strength-ramp
> endpoints/symmetry/permutation, position coherence for every shipped formation **plus** an end-to-end
> `ConfigureSquads` acceptance run through the real engine, every F1–F6 gate, and the `CreateSeason`
> handoff round-tripping through `SeasonStateCodec`) + `RosterGeneratorTests` +3 + `PlayerAttributesTests`
> +1. **Full dotnet gate: PASSED, 0 failures (whole tree green; season-save 141 → 177, player-database
> 42 → 46; SDK 8.0.129 via apt).** **A4a is designed but NOT executed** — its ~9 h corpus run is its own
> roadmap item, and A4 (#30 T2) is the next item on the critical path. **AR-5 (a hostile whole-file
> re-read, not a diff pass) then found 1H+4M+3L, all fixed:** **H-1** — because rosters are REGENERATED
> from the world seed rather than saved, the generation path is persistence-equivalent, and every
> determinism test on it was self-referential ("generate twice, compare"), so a draw-order change, a
> catalogue reorder, or a one-line `[GT]` tweak would silently rewrite every club in every existing save
> with the whole suite green; closed by new **KD-10** + a pinned golden vector
> (`LeagueBootstrapGoldenVectorTests` — the #16 HKDF/SipHash precedent), proven non-vacuous by
> perturbing `AttributeBaseMean` 10 → 11 and watching it fire. **M-1** — the world seed was WRITE-ONLY
> (`SeasonState.Seed` holds the derived season seed and `Mix` has no inverse; `WorldStore._worldSeed`
> had no accessor), so a saved career could not rebuild its `ISquadProvider` at all; closed by a
> read-only `WorldStore.WorldSeed` + the KD-9 resume recipe + a round-trip lock. **M-2** — the
> league-size-independence claim above was true only of the base roster (narrowed everywhere; the #43
> promotion/relegation consequence named). **M-3** — `SquadPositionCounts` was a public mutable `int[]`
> whose mutation still passes the sum check while voiding the KD-6 fieldable-squad guarantee (now
> `ReadOnlyCollection` over a private backing array). **M-4** — the strength spread's *sufficiency* was
> unverified while being the feature's stated purpose (discharged as KD-8 **Step 0**: a ~20-match pilot
> at the ramp extremes runs BEFORE the 9 h corpus, so A4a cannot fit three parameters to noise). Plus 3
> L. **AR-6 over those fixes then found 1M** — the new golden vector pinned only a 4-club league, leaving everything that varies with league size (the permutation length, the ramp denominator, name indexing, and the `delta == 0` branch that never occurs at N=4) unguarded; a second digest + delta row is now pinned at `DefaultClubCount` behind a guard that fails if the default is retuned. **Gate re-run: PASSED, 0 failures (season-save 141 → 177, living-world 119).** See src/CLAUDE.md
> v2.38 + the path-to-playable OPEN ISSUES entry.)
> **Last Updated (prior):** July 25, 2026 (**Season & Competition Loop #30 T1 LANDED — the season save/restore path;
> path-to-playable roadmap item A2.** A season is now part of the save file, not just the world and an optional
> match. New `src/season-save/SeasonStateCodec.cs` is a pure byte codec for the season-state sub-blob over the
> #30 Appendix B layout (version gate first; seed / seasonNumber / **managedClubId**; the club set; the
> CONCRETE schedule — serialized, never regenerated, per KD-5; the calendar cursor per KD-4; the league table
> in ClubId order; the board), carrying the `MatchSaveCodec`/`WorldStateSerializer` fail-loud posture:
> overflow-safe length bounds, a trailing-byte guard, and **decode-through-the-validating-constructors**, so a
> corrupt blob throws rather than materializing a structurally impossible season. The outer frame gains a
> **third** opaque sub-blob between the world and match blocks and **`SEASON_SAVE_FORMAT_VERSION` bumps 1 → 2**
> (FR-SN-020 — the world and match blobs are byte-untouched; only the frame around them moved, and a v1 file is
> rejected fail-loud, no Stage-0 migration). `SeasonSaveManager.Save(world, season, matchOrNull, path)` /
> `Load(...) → { World, Season, Match }` per FR-SN-021, with all three blobs captured before the file is opened;
> unlike the match, the season is **never optional**. **Implementation surfaced ERR-030-011** (filed + patched
> same commit): §3.6's `EncodeSeason` pseudocode omitted `ManagedClubId` — which Appendix B row 3a lists and
> `SeasonState` requires, so a codec written to §3.6 verbatim emits a blob no season can be reconstructed from —
> and Appendix B row 11 left job security as `f32/u8`, neither matching the integer per-mille `BoardState`
> carries. **Appendix B is the byte-layout authority**; §3.6 gains the missing line, row 11 is pinned
> `jobSecurityPerMille i32` (ratifying what #30 T0 adopted and flagged as a back-prop candidate). No
> `SEASON_STATE_FORMAT_VERSION` change — T1 is that version's first use, so the correction lands before any
> file exists. **Two code self-AR findings fixed:** the per-array length bound moved from a `count * width`
> byte product (overflowable for a large blob and a crafted count) to a provably overflow-free element-wise
> `remaining / width`; and `SeasonState`'s constructor now requires a calendar mapping at least one round,
> closing an encode/decode asymmetry where an EMPTY schedule with a `default(SeasonCalendar)` was constructible
> but not decodable. New `SeasonStateCodecTests` (round-trip field identity for fresh / mid-season / completed
> seasons, per-column and scalar locks, encode determinism + a non-vacuity control, a pinned-offset layout lock,
> and every FR-SN-023 fail-loud gate) + `SeasonSaveManagerTests` v1.3. **Full dotnet gate: PASSED, 0 failures
> (whole tree green; season-save 112 → 135 tests; SDK 8.0.129 via apt).** **Adversarial review over the landing (3 passes, converged): 1M+4L / 1M+2L / 0H+0M+3L.** Pass 1: the T1 self-AR's zero-round calendar guard was MOVED, not resolved — `BeginNextSeason` carries the identical vacuous coverage check unguarded (its `maxRound >= RoundCount` is false at `maxRound = -1`) and the ctor still took an empty fixture array, so a roll could install a state `Encode` writes and `Decode` refuses (reproduced by an executed probe); fixed at the root (the ctor now refuses an EMPTY schedule) and mirrored onto the roll. Pass 2: **FR-SN-011 (MUST) / F4 were unimplemented** — `SatisfiesCursorInvariant` had ZERO production callers while its own doc claimed `SeasonLoop.Restore` invoked it, so a save whose world clock had passed the pending round loaded silently and would surface at T2 as a stuck or skipped round; `SeasonSaveManager.Load` now enforces it (the one cross-blob coherence rule, checkable only at this root, which is the layering argument for the root existing), with the completed-season vacuous case locked so the gate cannot become a spurious refusal. Pass 3: three `Modified` headers stale against their history rows (FR-CS-056). L also: `<exception>` docs on the `Decode` seam, an outer-frame pinned-order lock, a mis-naming test rename, offset-helper widths named + a coherence guard, and two docs naming a T2 type / an already-closed back-prop. **Gate re-run: PASSED, 0 failures (season-save 135 → 141).** Remaining #30: T2 the day-advance
> loop + round resolution, T3 the boundary roll. See src/CLAUDE.md v2.37 + the path-to-playable OPEN ISSUES
> entry.)
> **Last Updated (prior):** July 22, 2026 (**Goalkeeper Mechanics #11 + Heading Mechanics #10 WIRED into the match
> engine, and the GK/Heading attribute projections LANDED — Phase 1 (opt-in).** The `ToGoalkeeper` /
> `ToHeading` projections that `player-attribute-projection-design.md` deferred under KD-P8 (phantom
> consumers — `MatchEngine` built neither struct) are now non-phantom: `MatchEngine.cs` v1.44 constructs
> both sealed orchestrators + four stateless ball/RNG adapters at boot and registers `heading.mechanics` +
> `goalkeeper.mechanics` RNG streams (the card-severity precedent). A new public `EnableGkHeading()` opts
> in (default OFF): while off the engine is **byte-identical to pre-wiring** (no `SNAPSHOT_SCHEMA_VERSION`
> change — the 279-test existing snapshot/determinism/restore suite is unchanged); while on, a 10 Hz
> tactical + 60 Hz physics drive runs both orchestrators and conservative Stage-0 world-state triggers
> (the `MatchFlowCollisionConsumer` heuristic-foul precedent) commit a `SaveIntent` seeded from
> `PlayerAttributeProjection.ToGoalkeeper` (loose on-target ball near the defended goal) and a
> `HeaderIntent` seeded from `ToHeading` (nearest agent to a loose airborne ball) — the projections' live
> consumer. A flag-on engine is deterministic FORWARD but not yet snapshot-safe, so the durable-capture
> seams fail loud (`NotSupportedException`); the per-tick digest is untouched. New
> `PlayerAttributeProjection.cs` v1.2 (`ToGoalkeeper` int→float widen of the ten GK fields; `ToHeading`
> raw copy of Heading/Strength/Balance) + `MatchEngineConstants.cs` v1.25 (+6 `[GT]` trigger constants).
> Governed by the new converged supplement `docs/tracking/gk-heading-engine-integration-design.md` (AR-1
> 1M+2L → AR-2 CONVERGENCE → AR-3 opt-in scope revision; code self-AR folded CS0118 fully-qualification,
> `_gkAgentIds` refresh across `ConfigureSquads`/subs, and the guard placement on the durable-capture
> seams — not the per-tick `SerializeWorldState`). New `MatchEngineGkHeadingTests` (8) +
> `PlayerAttributeProjectionTests` +2. **Full dotnet gate: PASSED, 0 failures (whole tree green; 290
> match-engine tests; SDK 8.0.129 via apt).** **Phase 2 (deferred):** serialize the RNG cursors + both
> orchestrators' in-flight state (`SNAPSHOT_SCHEMA_VERSION` 17 → 18), flip the default to on, take the
> digest rebaseline; plus a DT-driven producer, the `CollisionConsumer` duel fan-out, and the closed-loop
> scenario. See the new GK/Heading engine-integration OPEN ISSUES entry + src/CLAUDE.md v2.32.)
> **Last Updated (prior):** July 22, 2026 (**Unified season save LANDED — snapshot-deserialize N2 / match-engine
> Phase G-Phase 3 season save-file root; Phase 3 is now COMPLETE.** A whole season is now one **file**:
> `SeasonSaveManager.Save(world, matchOrNull, path)` bundles the living-world `WorldStore.Snapshot()`
> composite together with an **optional** in-progress `MatchEngine` (a `matchPresent` flag byte — a season
> between fixtures has a world but no match), and `SeasonSaveManager.Load(path, ISquadProvider squads =
> null)` reconstructs both (`SeasonSaveContents { WorldStore World; MatchEngine Match /* null if none */
> }`). The file is a **thin frame over two self-contained, independently version-gated byte blobs**
> (`SeasonSaveCodec`) — the codec never parses either sub-blob's internals, so all four inner versions
> stay untouched and the season file only adds a **fourth** format version
> (`SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION` [FIXED] = 1). **This is the N2 blocker the notes
> deferred**, and its resolution is the whole point: `FR-LW-003` bars the match engine from referencing
> the living-world assembly (and vice-versa), so **neither save could host the other**; the new
> `TacticalDirector.SeasonSave` assembly (`src/season-save/`) sits **above both** and is the only assembly
> that may see both — the same layer class as `match-viewer` over `match-engine` — so it composes them
> without either referencing the other (verified: `match-engine.asmdef` and `living-world.asmdef`
> reference neither each other). **Decisions made at this season root:** the file carries the world blob
> (always) then the match blob (only when `matchPresent`); the match blob is reused through a new public
> `MatchSaveManager.Encode(engine) → byte[]` / `Restore(blob, squads) → MatchEngine` "match save as a
> value" API (the internal capture seams stay internal — `Save`/`Load` refactored to delegate,
> behaviour-identical, all 279 match-engine tests still green); the `ISquadProvider` is a Load-time
> parameter, never persisted (threaded into the match restore only when a match blob is present); the
> match restore's fingerprint + MXCSR float-mode gates run on season `Load` unchanged. Governed by the new
> converged supplement `docs/tracking/unified-season-save-design.md` (v0.5, AR-1 2M+3L → AR-2
> CONVERGENCE; a follow-up code AR over the shipped diff found 0H+0M+2L, both fixed — L-1 restored
> `MatchSaveManager.Save`'s engine-before-path guard order (kept behaviour-identical); L-2 added the R4
> no-match-with-provider test lock). New `SeasonSaveManagerTests` (19 — disk round-trip determinism for a
> no-match season (world field-identical + `world.text` resumes) and a season with a neutral /
> distinct-squad match via `ISquadProvider` (match digest chain byte-identical AND world field-identical,
> both through one file), `SeasonSaveCodec` round-trip + all fail-loud gates, manager
> missing/corrupt/no-provider/null-world/overwrite). No `SNAPSHOT_SCHEMA_VERSION` /
> `WORLD_STORE_FORMAT_VERSION` / `MATCH_SAVE_FORMAT_VERSION` change (a file frame around unchanged blobs);
> `MatchSaveManager.cs` v1.2. **Full dotnet gate: PASSED, 0 failures (whole tree green; 19 new season-save
> tests; SDK installed via apt).** See src/CLAUDE.md v2.31 + the snapshot-deserialize OPEN ISSUES entry.
> **With this, snapshot-deserialize Phase 3 is complete** (the native MXCSR live-mode query was certified
> July 22; N2 lands here) — nothing further open on that track.)
> **Last Updated (prior):** July 21, 2026 (**On-disk match save format LANDED — snapshot-deserialize Phase 3
> `SaveManager` fold (N1).** A running match is now a **file**: `MatchSaveManager.Save(engine, path)`
> captures a durable snapshot and writes it atomically (the §4.6.1.1 temp→fsync→rename contract), and
> `MatchSaveManager.Load(path, ISquadProvider squads = null)` reads it back into a ready-to-tick
> `MatchEngine` via the Phase-1/2 `RestoreFromSnapshot` reader. The on-disk blob (`MatchSaveCodec`, a
> pure version-gated codec) packs the three things restore needs: the KD-7 boot `matchSeed` (the payload
> does not carry it — the file is the boot-header root the deserialize note deferred to N1), the
> `SnapshotHeader` incl. its `EnvironmentFingerprint` + digest chain, and the `SnapshotPayload`;
> fail-loud on a `MATCH_SAVE_FORMAT_VERSION` mismatch, an out-of-bounds length prefix, or trailing bytes
> (overflow-safe bound guard). `MatchEngine` gains a public `MatchSeed` property + the durable-capture
> seams promoted `TestOnly_` → production internal (`CaptureDurableHeader`/`CaptureDurablePayload`).
> **Decisions made at this N1 root:** the file carries the boot seed; the `EnvironmentFingerprint` is
> serialized so the KD-6 float-mode gate runs **end-to-end through disk** (a save under `CreateStage0Dev`
> validates; a tampered/foreign fingerprint is rejected — closing O3 for the on-disk path, so the
> on-disk header no longer writes `Fingerprint = null`); the `ISquadProvider` is a Load-time parameter,
> never persisted (the file references rosters by ClubId, the caller owns the roster store). Governed by
> the new converged supplement `docs/tracking/match-save-file-design.md` (v0.3, AR-1 3M+2L → AR-2
> CONVERGENCE); code self-AR folded one overflow-safe-bound hardening. New `MatchSaveManagerTests` (16 —
> disk round-trip determinism for neutral / booking-before-save / distinct-squad-via-provider, codec
> round-trip + all fail-loud gates, manager missing-file/corrupt-file/no-provider/overwrite paths). No
> `SNAPSHOT_SCHEMA_VERSION` change (a file frame around the unchanged reader/writer). `MatchEngine.cs`
> v1.43 + `MatchEngineConstants.cs` (MATCH_SAVE_FORMAT_VERSION [FIXED] = 1). **Full dotnet gate: PASSED,
> 0 failures (279 match-engine tests; whole tree green — SDK installed via apt).** See src/CLAUDE.md
> v2.30 + the snapshot-deserialize OPEN ISSUES entry. **Still open in Phase 3:** the native MXCSR
> live-mode query (host-blocked) + the N2 unified season save (FR-LW-003 + season save-file root).)
> **Last Updated (prior):** July 20, 2026 (**Snapshot-deserialize Phase 2 LANDED — distinct-squad restore
> re-projection (#27 T3), the last T3 data-side item, CLOSED.** A match booted through `ConfigureSquads`
> with real club squads can now be saved and restored byte-deterministically, not just refused. New
> `ISquadProvider` seam (`src/match-engine/ISquadProvider.cs`) threaded into
> `MatchEngine.RestoreFromSnapshot(…, ISquadProvider squads = null)`; the new
> `MatchEngine.ReprojectDistinctSquads` replaces the Phase-1 fail-loud — the neutral path returns
> immediately, and each team with a non-sentinel `_rosterClubId` (v16 identity) has its roster resolved
> (ClubId-checked + size/record validated, both teams before any apply — the `ConfigureSquads`
> validate-both-before-write discipline), its base lineup re-projected via `LineupSelector` +
> `PlayerAttributeProjection` (`ReprojectBaseLineup` — attribute arrays + the bench GK flags
> `_benchIsGoalkeeper`, a boot-constant NOT serialized; the on-pitch `_isGoalkeeper` stays the restored
> serialized value), and the substitutions the serialized `_activeBenchSlot` records replayed
> (`ReprojectSubstitutions`, the attribute half of `SubstitutePlayer`). Fail-loud on absent provider /
> unresolvable ClubId / mismatched returned ClubId (R4). Determinism rests on the provider returning the
> SAME roster the saved match loaded (`LineupSelector` + `PlayerAttributeProjection` are pure). Acceptance:
> `MatchEngineSnapshotRestoreTests` v1.1 proves G3 round-trip determinism for a distinct (varied-attribute)
> squad, a mid-match substitution, a post-restore substitution, and a post-restore keeper-for-keeper
> substitution, plus fail-loud on no provider / unknown ClubId / mismatched roster. No
> `SNAPSHOT_SCHEMA_VERSION` change. `MatchEngine.cs` v1.42. **Full dotnet gate: PASSED, 0 failures (263
> match-engine tests; whole tree green — SDK installed via apt).** **Discovered during Phase 2 (out of
> scope; a Phase-1 snapshot-completeness follow-up, NOT a Phase-2 defect):** a post-restore substitution
> that FLIPS a pitch slot's goalkeeper status — subbing a keeper onto an OUTFIELD slot, which realistic play
> never does — diverges via a Positioning-AI (#12) formation-slot interaction with the GK-flag flip (two
> fresh engines with the same substitution are deterministic; the base distinct-squad round-trip + realistic
> keeper-for-keeper and outfielder substitutions all round-trip). See
> `docs/tracking/snapshot-deserialize-design.md` v0.8 + src/CLAUDE.md v2.29 + the OPEN ISSUES entry. **Still
> open:** Phase 3 (native MXCSR query + on-disk `SaveManager` fold, host/upstream-gated) + the Phase-1
> Positioning GK-flag-flip edge above.)
> **Last Updated (prior):** July 20, 2026 (**Snapshot-deserialize Phase 1 COMPLETE — save/load/replay reader LANDED,
> G3 round-trip determinism GREEN.** The keystone the next tier of MVP work sits behind: the match engine
> can now be reconstructed from a snapshot, not just run forward once. New `MatchEngine.DeserializeWorldState`
> (the symmetric line-for-line mirror of `SerializeWorldState`, reconstructing every subsystem's cross-tick
> state through its `RestoreState` seam) + the static `MatchEngine.RestoreFromSnapshot(in SnapshotHeader,
> SnapshotPayload, ulong matchSeed)` factory (fingerprint gate → boot + `EventBus.ResetForNewMatch` →
> deserialize → KD-3 distinct-squad fail-loud → digest-chain `CommitLoadedDigest` + clock restore). New
> `RestoreState` counterparts on Pressing/Defensive/Attacking/Perception/Positioning + `MovementCommand.
> ReconstructFromSnapshot` (RotationController / executors / DecisionTree / OscillationGuard / MatchClock /
> RNG restore seams pre-existed). Acceptance: `MatchEngineSnapshotRestoreTests` proves **save@N → restore →
> tick to N+K == an uninterrupted run** byte-for-byte (KD-5) across neutral kickoff, a mid-match tactics
> change, and the KD-8 booking-cursor regression, plus version-gate / trailing-byte / distinct-squad
> fail-loud. Two findings folded in during landing: the excluded `_possessingAgentId`/`_prevPossessingAgentId`
> are reconstructed from the restored `MatchContext.PossessingAgentId` (the `_prev == _poss ==
> MatchContext.PossessingAgentId` snapshot-time invariant), and the trailing-byte guard is now event-ledger-
> aware (`RunSnapshotPhase` appends the digest-load-bearing ledger after the world state; the reader validates
> the world-state read ended at the ledger domain-tag boundary rather than restoring the ledger, which is
> replayed forward). No `SNAPSHOT_SCHEMA_VERSION` change (a pure reader over the v17 writer). `MatchEngine.cs`
> v1.41. **Full dotnet gate: PASSED, 0 failures (257 match-engine tests; whole tree green).** This unblocks
> save/load of an in-progress match, replay/rewind, and — via Phase 2 — distinct-squad restore (#27 T3). See
> `docs/tracking/snapshot-deserialize-design.md` v0.7 + src/CLAUDE.md v2.27 + the OPEN ISSUES entry. **Still
> open:** Phase 2 (#27 T3 distinct-squad re-projection via the `ISquadProvider` seam — Phase 1 refuses a
> non-sentinel roster reference) and Phase 3 (native MXCSR query + on-disk `SaveManager` fold, host/upstream-
> gated).)
> **Last Updated (prior):** July 18, 2026 (**Squad/Player Data Layer #27 T3 LANDED** — the snapshot roster-reference
> field for distinct-squad save/restore fidelity, per the new converged
> `docs/tracking/squad-roster-reference-design.md` (v0.2, AR-1..AR-2 CONVERGED). New per-team
> `MatchEngine._rosterClubId[TEAM_COUNT]` (the loaded `Squad.ClubId`, or `[FIXED] NO_ROSTER_CLUB_ID = -1`
> when no squad is configured), set by `ConfigureSquads` **after** both squads validate-and-apply (so a
> refused call leaves the sentinel), serialized at **`SNAPSHOT_SCHEMA_VERSION` 15 → 16** (`MatchEngine.cs`
> v1.39 / `MatchEngineConstants.cs` v1.23). Boot-constant identity — the same lifecycle class as the
> already-serialized `_teamIds`/`_isGoalkeeper`, which is what makes it non-phantom despite no restore
> consumer: a save now records **which squad each team loaded** — the identity half of restore fidelity;
> the per-slot attribute VALUES stay excluded (re-projectable from the roster, keyed by the serialized
> `_activeBenchSlot` for substitution bench-swaps). **KD-T3-2 design decision:** a configured squad —
> even all-`CreateDefault` — is now digest-distinguishable from an unconfigured one **by design** (the
> reference is identity, not attributes: club 7 all-neutral ≠ frozen neutral, because club 7 is a
> persistent roster to reload on restore). This **supersedes** the T1 KD-P7 all-default byte-identity
> lock (a T1-only property — T1 added no serialized field); behavioural neutrality still holds and is
> re-locked as "a config-default run diverges from unconfigured **at tick 1**, before any behavioural
> divergence could exist, so the roster field is the sole difference." A non-digest "header" alternative
> was rejected (KD-T3-4 — the match engine has no save/restore surface distinct from the digest payload,
> so a header field would be a zero-consumer phantom that also would not do the job; the payload is the
> project's established boot-constant-identity surface). **KD-T3-3:** the restore re-projection itself is
> future work — the match engine has **no snapshot-deserialize path** (verified: no `Read`/`Deserialize`
> in `MatchEngine.cs`), so building the consumer now would be a phantom; T3 lands the reference and
> unblocks that work on the data side. New `TestOnly_RosterClubId` seam; exclusion-proof +
> `ConfigureSquads`/substitution restore-scope docs updated. Tests: `MatchEngineSnapshotSchemaTests` v1.13
> (pin 15 → 16 + `RosterReference_FeedsSnapshotDigest` single-field probe), `MatchEngineSquadTests`
> v1.2 (T1 neutrality lock replaced with the KD-T3-2 identity-capture / same-config-determinism /
> distinct-ClubId / sentinel-seam locks). **Post-landing code AR (fresh-eyes over the shipped diff):
> 0H+0M+1L — L: replacing the T1 byte-identity lock dropped the direct match-level proof that a
> config-default match is *behaviourally* identical to unconfigured (the new tests prove the roster
> field feeds the digest, not that the divergence is non-behavioural); fixed by adding
> `ConfiguredDefaultSquad_IsBehaviourNeutral_ObservableStateMatchesUnconfigured` (ball + every agent
> position match tick-for-tick — the observable level a digest can no longer isolate). Re-verified
> clean: field appended last (no offset move), no snapshot decoder reads the payload by offset (only
> the opaque digest), CROSS-TICK-COVERAGE excluded-set claim survives.** **Full dotnet gate re-run:
> PASSED, 0 failures (237 match-engine tests).** See src/CLAUDE.md v2.26. **Remaining #27:** lineup selection proper (Plan-3 —
> the Stage-0 mapping is roster-order), the per-spec GK (#11)/Heading (#10) projections (deferred until
> those specs are engine-wired, KD-P8), the distinct-squad restore re-projection (gated on a
> snapshot-deserialize path existing), and on-disk persistence / transfers / aging (Stage 1+).)
> **Last Updated (prior):** July 17, 2026, latest same day (**Repeat adversarial review of the T1/T2 landing
> (AR-4 of its cycle, run at the user's request) — 1 M + 3 L, all doc-only, all fixed; then AR-5
> sweep 0H+0M+1L (doc) — CONVERGENCE, cycle CLOSED** per the L-only-round convention. The pass
> re-walked the full touched surface against source: writer-completeness sweep of every projected
> array (`_canonicalAttrs`/`_attrs`/`_dtAttrs`/`_perceptionAttrs`/bench — exactly boot seed +
> `ConfigureSquads` + `SubstitutePlayer`, no stray writer), the FirstTouchAbility site inventory
> (exactly 3), Perception-side mutation of `_perceptionAttrs` (none — the IsHalfTurned preserve is
> defensive-only), and the downstream #13 WeakReceiver/threat-score consumers (T1 activates the
> previously-dormant WeakReceiver press trigger for genuinely below-average receivers under a
> distinct squad — designed behaviour, default path unchanged at 10 ≥ threshold). **M-1 (doc,
> cross-assembly contract):** `AttackingAgentSnapshot.Pace/Dribbling` XML still documented the
> `(raw−1)/19` normalization while the T1 writer populates them live ÷`ATTRIBUTE_MAX` (KD-P3) —
> pre-T1 the mismatch was against an unconsumed 0.5 placeholder (flagged in the projection design
> §2); post-T1 it misdescribed real data a consumer could mis-derive raw values from. Docs aligned
> to the live ÷20 convention (`AttackingAgentSnapshot.cs` v1.1); switching the MATH stays a
> recorded deferred design question (it moves the neutral off 0.5). **L:** three `MatchEngine.cs`
> comments the T1 code edits outdated ("Stage-0 neutral placeholder" claims at the
> CoverShadowCurve fill / FillAttackingSnapshot summary / BuildFirstTouchContext summary —
> v1.38, doc-only); the three `STAGE0_NEUTRAL_*` constants' stale "TODO: replace when ERR-007
> attribute split lands" markers retired (`MatchEngineConstants.cs` v1.22 — the split landed;
> production-unconsumed since T1, retained as the KD-P7 neutral-equivalence references); AR-5's
> L — `ConfigureSquads` doc now states players beyond the consumed 18 are ignored. The
> decision-tree `(raw−1)/19` hits are #8's own spec-pinned INTERNAL normalization of the raw
> values T1 feeds it — KD-P2-consistent, not a finding. Full dotnet gate re-run: PASSED, 0
> failures. See src/CLAUDE.md v2.25.)
> **Last Updated (prior):** July 17, 2026, latest same day (**Squad/Player Data Layer T1/T2 LANDED** — `MatchEngine`
> attribute seeding now sources from canonical player records per the converged
> `docs/tracking/player-attribute-projection-design.md` (v0.3, AR-1..AR-3 CONVERGED; PR #225). New
> `src/match-engine/PlayerAttributeProjection.cs` (pure per-target projections: #2/#8/#7 raw copies;
> #5/#6 with the KD-P1 derived KickPower — `(Passing+Technique)×.5` / `RoundToInt((Finishing+LongShots)×.5)`,
> the ERR-007 proxies now computed from real attributes; the three `FirstTouchAbility` sites #13/#14/#4
> per KD-P9; the sole normalized target — Attacking pace/dribbling — `÷ATTRIBUTE_MAX` per KD-P3 so
> neutral = 0.5). `MatchEngine.cs` v1.37: canonical `_canonicalAttrs`/`_benchCanonicalAttrs` records
> (default `CreateDefault()`, NOT serialized — same B3 exclusion class, proof updated), every seeding
> site converted (zero production `STAGE0_NEUTRAL_*` consumers remain), new public `ConfigureSquads`
> (pre-kickoff, Stage-0 roster-order lineup — player 0 → the GK slot; lineup selection proper stays
> deferred; fail-loud [1,20]/[1,5] bounds gate at the consuming seam, both squads validated before ANY
> write), and `SubstitutePlayer` now copies the canonical bench record + re-projects `_dtAttrs`/
> `_perceptionAttrs` (the v2.20 substitution-attrs hazard's on-pitch half). **Default path proven
> byte-identical (KD-P7, digest-locked — no schema change, no rebaseline); a distinct squad diverges
> by design and deterministically. Distinct-squad restore stays a T3 deliverable (KD-P10, documented —
> no restore path exists today).** Implementation-time corrections recorded in the design docs'
> version histories: the §1 inventory under-reported `FirstTouchContext.Technique` (same site, now
> projected, neutral-preserving; projection doc v0.4), and #27's reserved-list mis-classified
> `FirstTouchAbility` (KD-P9 correction, squad-player doc v0.5 + `PlayerAttributes.cs` v1.1).
> Self-adversarial review of the landing: **AR-1 1 M** (per-team validate-then-apply let an invalid
> AWAY squad refuse only after the HOME squad had landed — validation hoisted for both squads before
> any write, + regression lock) **+ AR-2/AR-3 sweeps clean** (residual-seed grep empty; KD-P8 honoured —
> no GK/Heading phantom projections). New suites: `PlayerAttributeProjectionTests` (scale/derivation/
> neutral-equivalence locks) + `MatchEngineSquadTests` (digest neutrality/divergence/determinism +
> substitution + fail-loud gates). Full dotnet gate: PASSED, 0 failures (232 match-engine tests).
> See src/CLAUDE.md v2.24. **Remaining #27 work:** T3 snapshot roster reference (distinct-squad
> restore fidelity), lineup selection, Stage-1+ persistence/transfers/aging.)
> **Last Updated (prior):** July 17, 2026, later same day (**Fourth repeat adversarial review (AR-4 of the cycle) —
> 0 H + 0 M + 1 L (doc-only), fixed. CONVERGENCE — the review cycle over the July 14–15 landings is
> CLOSED** per the project convention (an L-only round ends the cycle; match-viewer AR-4 precedent).
> Instead of another piecemeal sweep, the pass walked the COMPLETE sent-off participation matrix —
> AI dispatch skip / all four Mechanics-AI `IsActive` snapshot fills / physics forced-stop / offside
> line / first-touch receiver scan (AR-2's fix) / foul-card-restart interpretation (AR-3's fix) /
> substitution refusal / half+full-time one-shots — plus the in-flight-state interactions the
> earlier rounds never composed: a card's `ApplyRestart` clears possession BEFORE the Resolve-phase
> executor advance, and the executor adapters' `IsBallPossessedBy` reads the live
> `_possessingAgentId`, so a just-sent-off agent's mid-windup pass/shot self-cancels at CONTACT via
> the FM-08/FM-05 possession recheck (no participation leak through in-flight executors). **L
> (doc):** the `_lastHolderAgentId` writer comment claimed the `GoalAwardedEvent` credit "names the
> agent whose kick scored" — deflections never update the tracker (the approximation already
> documented at the `RestartResolver` seam by AR-1), so a deflection-chain goal credits the last
> SETTLED holder, possibly not the kicker and possibly sent off since; comment aligned
> (`MatchEngine.cs` v1.36, doc-only — scoring-TEAM classification is pure geometry and unaffected).
> Full dotnet gate re-run: PASSED, 0 failures. See src/CLAUDE.md v2.23.)
> **Last Updated (prior):** July 17, 2026 (**Third repeat adversarial review (AR-3 of the cycle) — 1 M found,
> fixed.** The pass re-verified all six AR-1/AR-2 fixes and swept the card/restart/possession
> interaction paths the earlier rounds had cleared piecemeal. **M-1:** foul candidates involving a
> sent-off participant were still applied — `ApplyFoulIfCaptured` checked contact type, force, and
> opposite teams but not `_isSentOff`, and sent-off agents deliberately remain collision bodies, so
> a frozen red-carded agent standing in the path of play repeatedly WON free kicks (`ApplyRestart`
> teleported the ball to their feet) and drew cards against opponents who ran into their back, for
> the rest of the match — the foul/card/restart interpretation was the remaining participation
> surface without the exclusion. Fixed (`MatchEngine.cs` v1.35 — candidate discarded at the
> application site: no event, no cooldown, no restart; physical collision response unchanged) + 2
> regression locks (`MatchEngineFoulCardTests` v1.1 — sent-off victim in the exact positive
> free-kick geometry, and sent-off offender). Verified clean: every card path clears possession via
> `ApplyRestart` (no sent-off-possessor deadlock vector); the Interception case maps the Stage-0
> unresolved interceptor to NO_POSSESSION. Full dotnet gate re-run: PASSED, 0 failures. See
> src/CLAUDE.md v2.22.)
> **Last Updated (prior):** July 16, 2026, later same day (**Repeat adversarial review (AR-2 of the cycle) — 1 M + 1 L
> found, both fixed; the pass otherwise re-verified the first round's fixes and swept the
> surfaces the first round had only skimmed** (LiveMatchFrame, AttrIdx/NameCatalogue, the four
> live-viewer/player-database test suites, RunPhysicsPhase freeze, RunFirstTouch gates). **M-1:**
> sent-off agents could still RECEIVE the ball — `RunFirstTouch`'s gate-4 receiver scan was the one
> participation surface without the `_isSentOff` exclusion (AI dispatch, all four Mechanics-AI
> `IsActive` snapshot fills, the physics forced-stop, and the offside line all have it), so a ball
> rolling past a frozen red-carded agent handed them possession they could never release (no AI
> dispatch ⇒ no kick), deadlocking play until the next half/full-time ball reset. Fixed
> (`MatchEngine.cs` v1.34) + regression lock (`MatchEngineFirstTouchTests` v1.1 — the exact
> CONTROLLED-receive geometry with the agent sent off stays loose). Physical presence
> (collision/perception/pressure) deliberately unchanged. **L (doc):** `AttrIdx`'s "Technical (8)"
> group comment lists 7 members (totals were correct). Full dotnet gate re-run: PASSED, 0 failures.
> See src/CLAUDE.md v2.21.)
> **Last Updated (prior):** July 16, 2026 (**Adversarial-review fix pass over the last three landings** —
> match-flow completion (July 14) / interactive match view (July 15) / squad-player data layer
> (July 15) were re-reviewed fresh-eyes at the user's request; findings 2 M + 4 L, all fixed same
> day. M-1: `MatchEngine.SubstitutePlayer` never reset the outgoing slot's yellow-card count —
> discipline was slot-keyed, so a substitute replacing a booked player was sent off on their own
> first yellow via the second-yellow promotion (`MatchEngine.cs` v1.33 resets it; no schema bump —
> v15 already serializes the count; +regression locks in `MatchEngineSubstitutionTests` v1.1).
> M-2: `SquadFileLoader` bounded every numeric key except `age` (silently accepted any int against
> its own "out-of-range int all throw" contract) — now [AgeMin, AgeMax] (+2 locks). L: post-full-time
> `SubstitutePlayer` refused (state mutated a frozen match while the queued SubstitutionEvent could
> never flush past the `_matchEnded` Resolve guard); `RestartResolver`'s "touched last" param doc
> aligned to the actual caller input (the last settled HOLDER — deflections never update the
> tracker, −1 ⇒ team 0); the live viewer's HUD clock reintroduced the `m:60` rounding bug the HTML
> replay's AR-1 had fixed (now rounds before the minute split; node-verified); `LiveMatchServer`
> connection threads that outlive `Stop()` now answer 503 instead of still driving /control;
> `RosterGenerator` modulo-bias doc note. Also flagged forward: the substitution attrs-swap ×
> player-database T1 interaction (see the updated squad/player OPEN ISSUES entry). See src/CLAUDE.md
> v2.20.)
> **Last Updated (prior):** July 15, 2026, later same day (**Squad/Player Data Layer T0 LANDED** — the match
> engine currently seeds all 22 agents with identical mid-range (10) attributes
> (`PlayerAttributes.CreateDefault()`, `STAGE0_NEUTRAL_ATTRIBUTE`); this is a Stage-1-forward pull
> (master plan §4.2 places a player database at Stage 2) providing the canonical data layer that
> gap needs, mirroring the #21/#22 design-supplement-first precedent. Design doc
> `docs/tracking/squad-player-data-design.md` (candidate spec #27, not yet reserved in
> `SPEC_INDEX.md` — registry rows land at promotion per the #23–26 precedent) went through 2
> self-adversarial-review rounds to convergence (AR-1: club-identity vs match-`teamId` conflation in
> the original `PlayerId`/RNG-stream keying draft — corrected via KD-3; trimmed the canonical
> attribute table to "consumed by an existing spec" ∪ "reserved, master-plan-only"; WeakFootRating
> scale isolation. AR-2: position-bias-table test strategy switched to direct constant assertions,
> not statistical sampling over generated squads). New `src/player-database/` assembly
> (`TacticalDirector.PlayerDatabase`, references only `DeterministicSim`): canonical
> `PlayerAttributes` (31 `[1,20]` fields reconciling all 7 existing per-spec attribute structs +
> `WeakFootRating` on its own `[1,5]` scale — closes the long-open `ERR-007` gap where the spec text
> was patched in 2026 but `AgentMovement.PlayerAttributes` never actually gained the fields;
> `PassAgentAttributes` still carries `[TEMPORARY-PROXY-ERR-007]` tags today), `PlayerRecord` /
> `Squad` (club-scoped roster container, `CLUB_SQUAD_SIZE`=25 per master plan §4.2 — deliberately
> not `MatchEngineConstants.SQUAD_SIZE`, which is the unrelated match-scoped 22-on-pitch-agent
> concept), `RosterGenerator` (deterministic — new `DOMAIN_TAG_PLAYER_DATABASE`=0x1F +
> `SubsystemOrdinals.PlayerDatabase`=81 back-propped into `deterministic-sim`, off-pitch band
> alongside Living World), `SquadFileLoader` (Stage-0 human-authoring text import, mirrors
> `TeamTacticFileLoader`'s grammar exactly). Code adversarial review (2 passes) caught three real
> defects before landing: `PlayerRecord.Position` had no RNG draw at all in the first pass
> (`FIELDS_PER_PLAYER` undercounted 35→36); `WeakFootRating`'s jitter reused the much-wider
> attribute spread against its own `[1,5]` range, clamping most draws to the boundary (now its own
> `WeakFootSpread`); `SquadFileLoader`'s identity default computed `PlayerId` from the raw
> section-local index instead of the club-scoped formula `RosterGenerator` uses, caught by a
> round-trip test that would have failed against the bug. Also flagged and documented (not yet
> hit): `PlayerDatabase.PlayerAttributes` shares its bare name with the pre-existing, unrelated
> `AgentMovement.PlayerAttributes` — no collision today since nothing references this new assembly
> yet, but the CS0104 class the project hit at `src/CLAUDE.md` v1.73 (`TacticTranslation`) will
> recur the moment a future T-phase wires both into `MatchEngine`. **Deliberately NOT built in this
> pass** (see the design doc §4/§5 T-phase plan): wiring into `MatchEngine` (replacing
> `CreateDefault()` seeding — intentionally NOT behaviour-neutral, unlike a typical T0, since the
> entire point is giving agents distinct attributes, so it needs its own reviewed change); per-spec
> projection updates that would close `ERR-007` for real; a snapshot roster-reference field; the
> on-disk save-format squad persistence / transfer market / aging (master plan §4.3/§4.4, explicitly
> out of scope). Full dotnet gate not runnable in this environment (mirror 404s on
> `dotnet-sdk-8.0`, consistent with prior entries) — verified by exhaustive manual review in place
> of `dotnet test`. See src/CLAUDE.md v2.19.)
> **Last Updated (prior):** July 15, 2026 (**Interactive match view LANDED** — upgrades the passive post-hoc
> HTML replay (`src/match-viewer/`) into a live-updating viewer watched *during* a real match: a
> background thread paces a real `MatchEngine` at wall-clock speed (`LiveMatchStreamer.cs`, new)
> and a minimal loopback-only HTTP server (`LiveMatchServer.cs`, new — hand-rolled over
> `TcpListener`, no package dependency) serves a browser page that polls `/frame` and redraws, plus
> a playback-only `/control` endpoint (pause/resume/speed — deliberately never a gameplay-mutation
> channel). `MatchEngine.cs` v1.32 gains 3 trivial read-only properties (`HomeScore`/`AwayScore`/
> `MatchEnded`), same section as the existing `BallView`/`AgentView` observation surface. Full
> in-Unity rendering remains blocked on Unity host access (existing OPEN ISSUE) — this is the
> "at minimum a live-updating viewer" floor. Per the user's process instructions: a design doc
> (`docs/tracking/interactive-match-view-design.md`) went through 2 self-adversarial-review rounds
> to convergence before implementation, then the code itself went through 2 adversarial-review
> passes, catching and fixing (among other things) an identical `Start()`/`Stop()` race condition
> in both new classes — the running-state flag flipped true inside the lifecycle lock before the
> background thread was actually assigned, so a `Stop()` racing into that narrow window could join
> a null thread while a fresh thread got spawned against an already-stopped listener. Full dotnet
> gate not runnable in this environment (no SDK reachable) — verified by exhaustive manual review
> in place of `dotnet test`. See src/CLAUDE.md v2.18 for the full file-by-file description.)
> **Last Updated (prior):** July 14, 2026 (**Match-flow completion LANDED** — throw-ins, corners, goal kicks,
> fouls/cards, offside, substitutions, half-time break, and full-time end (previously only kickoff +
> goal-restart existed; see `docs/tracking/match-flow-completion-design.md` for the full plan +
> adversarial-review history). Per the user's process instructions: a design doc was written first,
> adversarially reviewed to convergence (AR-1 through AR-6, each documented in the design note's own
> version history — including AR-4's rejection of a full ends-swap at half-time, since `team 0
> attacks +X` is hardcoded across goal detection/offside/Mechanics-AI and a real ends-swap is a
> Stage-1+ deferral, and AR-5's fix for `SubstitutePlayer` being callable between ticks when
> `EventBus.CurrentPhase` is not a valid producer phase — now a pending-event queue flushed at the
> top of the next Resolve phase), then implemented, then the CODE was itself adversarially reviewed
> to convergence (catching, among other things, an `OffsideEvaluator` bug where fewer than two active
> defenders left the accumulator at an `Infinity` sentinel instead of `NaN`, which made `IsOffside`
> return true for every finite attacker position — the exact opposite of the intended "too few
> defenders to be offside" rule). **New:** `src/match-engine/RestartResolver.cs` (pure
> position/awarded-team resolution for `RestartType.ThrowIn`/`Corner`/`GoalKick`, unified
> `awardedTeam = 1 − lastTouchTeam`), `OffsideEvaluator.cs` (pure second-nearest-to-goal-line
> geometry + reception-time offside check — a documented Stage-0 approximation, not the full
> freeze-at-the-pass Law), `SubstitutionReason.cs`; three new Tier A events
> (`OffsideCalledEvent` 0x18, `RestartAwardedEvent` 0x19, `MatchPhaseChangedEvent` 0x1A, all
> registered in `EventRegistry` v1.8). `MatchEngine.cs` v1.31: `CheckRestartAndApply` (renamed/
> extended from `CheckGoalAndRestart`) routes non-goal exits through `RestartResolver` +
> a shared `ApplyRestart` primitive; a per-tick foul-detection consumer (`MatchFlowCollisionConsumer`,
> replacing the former no-op `NullCollisionEventConsumer`) captures at most one FROM_BEHIND
> high-force cross-team collision per tick, drawn against a new `match-flow.card-severity` RNG
> stream for card severity (yellow/red bands), with second-yellow promotion and sent-off tracking
> (`_yellowCards`/`_isSentOff`) feeding a forced-stop in the Physics phase and an `IsActive = false`
> exclusion in all four Mechanics-AI snapshot fill sites (#12/#13/#14/#15); `EvaluateAndApplyOffside`
> hooked into `RunFirstTouch`'s Controlled case for genuine same-team pass receptions; a public
> `SubstitutePlayer` (bench-roster swap, cap-enforced at `MAX_SUBSTITUTIONS_PER_TEAM`, queued
> `SubstitutionEvent` publish); `CheckMatchFlowTransitions` (called every Input phase, not
> stride-gated) fires the half-time ball-reset-only transition once at `HALF_TIME_BOUNDARY_TICK` and
> the full-time gameplay-freeze once at `MATCH_TICKS_TOTAL` (both guarded by one-shot flags;
> `_matchEnded` freezes AI/Physics/Resolve while the tick/snapshot loop keeps advancing).
> **`SNAPSHOT_SCHEMA_VERSION` 14 → 15** (per-agent yellow-card count + sent-off flag, the global foul
> cooldown, per-agent active bench slot, per-team substitutions-used count, half-time/full-time
> fired flags — all cross-tick and now digest-load-bearing). New tests:
> `MatchEngineRestartTests`/`MatchEngineOffsideTests`/`MatchEngineFoulCardTests`/
> `MatchEngineSubstitutionTests`/`MatchEngineMatchFlowTests` (pure-function locks + MatchEngine
> integration + two-run determinism each); `MatchEngineSnapshotSchemaTests` v1.12 (pin 15 + two new
> preimage probes). Full dotnet gate not runnable in this environment (no SDK access) — verified by
> exhaustive manual code review (multiple adversarial-review rounds reading the entire touched
> surface, not just the diff) in place of `dotnet test`. See src/CLAUDE.md v2.17 and
> `docs/tracking/match-engine-design.md` v2.0.)
> **Last Updated (prior):** July 13, 2026 (**Unity engine version bumped: 2022.3.62f1 → Unity 6000.4.9f1,
> graphics API pinned DX11 — documentation-only pass, no recertification performed.**
> `ProjectSettings/ProjectVersion.txt` updated to `6000.4.9f1`. `docs/tracking/certification-platform.md`
> → v1.3: Unity-version and new Graphics-API rows updated to the target tuple; per that file's own
> Maintenance Rule this is a MAJOR version bump, so Status flips from `✅ PINNED` back to
> `⏳ RECERT REQUIRED` and every downstream unblocker it previously closed (`FR-DS-009-GATE` Stage 0
> activation, `FR-PO-052` perf-gate, §7.5 D1 test-runner pin, `EnvironmentFingerprint`) is blocked
> again until a real certification run executes against the new tuple — the June 7, 2026 run only
> certified the superseded 2022.3.62f1 tuple. `docs/tracking/cert-run-runbook.md` → v1.1 (Step 0
> pre-flight table updated; flags that `CertifiedPerfBaseline.Stage0CertPlatformPin`
> (`src/performance-optimization/CertifiedPerfBaseline.cs`) still hardcodes the old
> `win11-unity2022.3.62f1-...` pin string as a follow-up CODE change, deliberately out of scope for
> this docs-only pass). This root `CLAUDE.md`'s own "Unity 2022 LTS conventions" coding-convention
> line updated to Unity 6. **Deliberately NOT touched** (per this project's own "historical rows
> preserved verbatim" convention): dated version-history rows inside already-`APPROVED` spec section
> files that cite Unity 2022.3/2022 LTS as reference hardware or citation text (e.g.
> `positioning-ai/section-6.md`, `defensive-ai/section-6.md`, `attacking-ai/section-6.md`,
> `pressing-ai/section-6.md`, and citation blocks in `agent-movement`/`ball-physics`/`first-touch`/
> `collision-system`/`pass-mechanics` §8) — these are frozen approval-time records, not living
> config, and per Spec #16 §1.7 a version bump of this kind requires Platform Certification owner
> sign-off before it can be certified, which has not been sought here. Also not touched: the
> `tools/dotnet-ci` build-shim's technical claims about Unity's actual BCL/TFM/LangVersion surface
> (`netstandard2.1`, `LangVersion 9.0`) — verifying those against real Unity 6000.4.9f1 behavior is
> an engineering task, not a documentation edit, and is called out as a new OPEN ISSUE below. See the
> new OPEN ISSUES entry.)
> **Last Updated (prior):** July 11, 2026, latest same day (**Engine substrate LANDED — goal detection + score
> state + match-length/halves model (the #26 §9.3 upstream deliverables) — and the #26 half-time
> trigger + live ladder inputs ACTIVATED** (the §3.4/§1.6 PASS-1 M-1 gates CLOSED). **(a)
> Match-length model:** `MatchEngineConstants` v1.20 — `[FIXED] MATCH_LENGTH_MINUTES` (90) +
> `[DERIVED] MATCH_TICKS_TOTAL` (= 324 000; the #26 §3.5 `[CROSS-PENDING]` row promoted `[CROSS]`,
> §3.5 v0.3) + `[DERIVED] HALF_TIME_BOUNDARY_TICK` (162 000 — the FR-TP-019 Stage-0 halves model:
> boundary only, no break/end-swap/match-end). **(b) Goal detection:** `MatchEngine.cs` v1.30 —
> Resolve-phase `CheckGoalAndRestart` (executor advance → goal check → first touch):
> `BallCollision.CheckBoundaries` ⇒ KickOff = goal; scoring TEAM by exit half-space geometry (own
> goals credit the right side); per-team score + the FIRST-EVER Tier A `GoalAwardedEvent` (0x07;
> Scorer = the new last-holder tracker) + centre-spot restart (agents keep positions; possession
> cleared); non-goal exits untouched (no throw-in/corner model). **`SNAPSHOT_SCHEMA_VERSION` 13 →
> 14** (goals + last-holder serialized). **(c) #26 activation:** `RunManagerDecisionPoints` passes
> LIVE goalDiff + `ticksRemaining`/`MATCH_TICKS_TOTAL`; `ManagerDecisionGate` v1.1 fires the
> half-time decision (once, first stride at/after the boundary, regardless of interval position —
> the §3.2 worked example). Tests: new `MatchEngineGoalTests` (6) + `ManagerAITests` v1.1 (+4) +
> schema pin 14 + ScoreState probe. Spec docs: #26 section-1 v0.3 / section-2 v0.4 / section-3
> v0.3 / section-9 v0.5 (§9.1 engine-substrate gates CLOSED); `match-engine-design.md` v1.4.
> **Full dotnet gate: PASSED, 0 failures.** See src/CLAUDE.md v2.15. Remaining #26 follow-up:
> only the §9.2 own-`[GT]` balance review — the KD-6 on-disk preset format stays deferred BY SPEC
> (FR-TP-002/017: no disk format at Stage 0+1). Not built (Stage-1+ restart model): throw-ins /
> corners / goal kicks, the half-time break / end swap, match-end.)
> **Last Updated (prior):** July 11, 2026, later same day (**#26 T1–T4 manager-AI wiring LANDED** — the last
> item on the July-10 T-phase plans; default-behaviour-neutral (`ManagerMode.Human = 0` zero-init =
> the inert identity per KD-4 — no gate fire, no adaptation, no engine calls; a default match is
> byte-identical to pre-#26). **T1:** `tactical-instructions/TacticalPresetsConstants.cs` (§3.5
> scalars + the A.2 archetype / A.3 affinity `[GT]` tables; `MATCH_TICKS_TOTAL` deliberately
> absent — `[CROSS-PENDING]`) + `match-engine/TacticPresetProjection.cs` (FM-TP-01; the FR-TP-014
> roster gate at the consuming seam). **T2:** `ManagerDecisionGate` (FM-TP-02, KD-3 — kickoff +
> fixed interval; the half-time trigger stays gated on the engine halves model per §1.6/PASS-1
> M-1), evaluated only in RunAiPhase's stride branch BEFORE the FR-TI-027 commit (FR-TP-018;
> off-stride firing impossible, F5). **T3:** `ManagerProfile` (F4 NaN-gated, A.2 factory) +
> `ManagerAdaptation` kickoff scoring (Appendix B.1 exact: Aggressive → Gegenpress 0.66,
> Pragmatic → Balanced 0.50; tie → lowest ordinal, KD-8) + `ApplyKickoff` (the FR-TP-004 boot
> path via the EXISTING appliers; seeds `LastDecisionTick = 0` so the first stride gate never
> double-fires). **T4:** `StepToward`/`EvaluateLadder` (FM-TP-04, B.2 exact — 0.622 steps / 0.233
> holds; `URGENCY_DIFF_CAP`) + `RunDecisionPoint` (the FR-TP-005 mid-match path via
> `SetTeamTactic`/`SetPlayerTactic`, never the appliers — F3; decrement-then-check hold per the
> B.2 70′→80′ cadence). The live engine call passes goalDiff = 0 — engine-TRUE (no goal producer
> exists) — so both ladder terms are identically zero for any clock inputs and the T4 prerequisite
> gate is honoured with a single code path; the ladder body is unit-locked through explicit
> parameters. `MatchEngine.cs` v1.29 (public `ConfigureManager`, internal boot seams, `TestOnly_
> ManagerState`), **`SNAPSHOT_SCHEMA_VERSION` 13** (per-team `ManagerState` in pinned Appendix C
> order — mid-match manager decisions restore-deterministic, FR-TP-012). Tests: new
> `ManagerAITests` (21) + `MatchEngineSnapshotSchemaTests` v1.10 (pin 13 + ManagerState probe).
> **Full dotnet gate: PASSED, 0 failures.** See src/CLAUDE.md v2.14. Remaining #26 follow-ups are
> the spec's own engine-substrate gates (half-time trigger; live goalDiff/`MATCH_TICKS_TOTAL` —
> upstream match-engine deliverables per §9.3) + the KD-6 on-disk preset format (parser swap).)
> **Last Updated (prior):** July 11, 2026 (**Specs #23/#24/#25 wiring LANDED** — the T-phase step after the
> July-10 T0 scaffolding; all default-behaviour-neutral (Balanced ⇒ Off/None/Off = the exact
> identities, byte-identical default match). **(a)** `SlotComposer` v1.2 gains the #24 build-up
> overlay stage (Step 3b, FM-BU-02 — after ContextModifier, before spacing) and the #23 dismark
> offset stage (Step 4b, FM-DM-02 — after spacing, before the pitch clamp), per ERR-012-007/008
> and the #24 §4.2 combined order; `PositioningPerceptionSnapshot` v1.1 carries the routing dials +
> per-agent pressure/marker carriers (zero defaults = identities). **(b)** New
> `positioning-ai/RotationController.cs` (#25 §3.1–§3.4: FM-RO-01 predicate on the
> controller-owned SERIALIZED `LastComposedTarget` cache per PASS-1 H-1, FM-RO-02 dwell/commit +
> hold/revert, atomic pairwise `SlotIndex` swap + partner lock, phase-exit freeze, FR-RO-009
> per-tick cap, F2/F5/F6 validating restore seams) wired into `PositioningAITick` v1.3 per
> §4.2/ERR-012-009 (sole post-seed `SlotIndex` writer; identity binding never rewrites a row).
> **(c)** #23 §3.4 marked-pass-target penalty in #8 `UtilityScorer` v1.10 (passer-view proximity ×
> passer awareness per FR-DM-010/011; Off ⇒ exact ×1.0); `TacticalContext` v1.7 +
> `DismarkIntensity`; `TacticalWeights` v1.5 + `TargetMarkedUtilityMult` [GT] /
> `MarkedPassRadiusM` [CROSS]. **(d)** `MatchEngine.cs` v1.28, **`SNAPSHOT_SCHEMA_VERSION` 11 →
> 12**: Phase-D dial writers + one-stride-stale dismark carriers (§3.2 M-1 contract), per-agent
> dwell update in the perception pass (FR-DM-003, runs regardless of dial), #24
> classify/check-then-decrement pre-pass + FM-BU-03 TEAM-LEVEL regain arming in
> `OnPossessionChanged` (settledTeam diff; Balanced carries HoldShape so a default match never
> opens a window), v12 serializes dwell / zone+settledTeam / rotation binding+cache+pairs + the
> three dials appended to `WriteTeamTactic` in pinned #21 Appendix B order; 9 TestOnly seams.
> Tests: +`SlotComposerStageTests` (7) + `RotationControllerTests` (12); `UtilityScorerTests`
> v1.5 (+4 incl. the exact 0.832 worked example), `MatchEngineTacticTests` v1.5 (+5),
> `MatchEngineSnapshotSchemaTests` v1.9 (pin 12 + 2 probes). **Full dotnet gate: PASSED, 0
> failures.** See src/CLAUDE.md v2.13. Next per the T-phase plans: #26 T1 preset→config
> projection, T2 decision gate, T3 kickoff scoring, T4 adaptation.)
> **Last Updated (prior):** July 10, 2026, later same day (**Specs #23–#26 all `IN REVIEW → APPROVED`; steps
> completed: sign-off + back-props + the last citation** — (1) lead-developer R-01..R-05 sign-off
> granted on all four (each `section-9-approval-checklist.md` → v0.4 with the §9.5 gate table +
> §9.6 decision, per the #22 template; all 44 spec-folder files flip `Status: APPROVED`;
> `SPEC_INDEX.md` **26 APPROVED / 0 IN REVIEW**). (2) The seven cross-spec back-props FILED and
> landed atomically with the flips (`spec-error-log.md` v1.30): ERR-021-005/006/007 — #21
> `TeamTactic` gains `DismarkIntensity`/`BuildUpStructure`/`RotationFreedom` + Appendix B appends
> in pinned approval order #23 → #24 → #25 after `MarkingOrientation` (`tactical-instructions/
> section-2.md` + `appendices.md` → v0.5; serialization enters `WriteTeamTactic` + schema bump
> only at each spec's wiring); ERR-012-007/008/009 — new `positioning-ai/section-3.md` §3.7.1
> (v0.6) pins the build-up overlay stage (ContextModifier → spacing), the dismark offset stage
> (spacing → pitch clamp, FR-DM-008), the `RotationController` pre-composition position, and the
> `AgentPositioningData.SlotIndex` single-writer contract amendment (numbers 004–006 deliberately
> skipped — soft-reserved by the June-13 quarantine cluster whose ERR-012-003 citation is already
> live); ERR-008-012 — `decision-tree/section-3-2.md` §3.2.2.1 (v1.5) anchors the FM-DM-03
> marked-pass-target multiplier in the pre-clamp tactical product. (3) The #26 Bradley row
> VERIFIED: Bradley, P. S. & Noakes, T. D. (2013), *Match running performance fluctuations in
> elite soccer: indicative of fatigue, pacing or situational influences?*, **J Sports Sci
> 31(15):1627–1638, DOI 10.1080/02640414.2013.796062, PMID 23808376** (index-level corroboration
> across PubMed + independent indexes; publisher/Crossref direct resolution still blocked by the
> environment's network policy — same evidence class as the accepted Wilson rows). §8 citation
> rows now closed across ALL specs. Carried forward post-APPROVED, non-blocking: the `[GT]`
> balance passes (#21 G2 precedent) and the #26 engine-substrate gates (T2 halves/
> `MATCH_TICKS_TOTAL`, T4 goal-detection — upstream match-engine deliverables). Implementation of
> #23–#26 per each spec's §6 T-phase plan is the next body of work.)
> **Last Updated (prior):** July 10, 2026 (**#23–#26 post-PASS-1 open gates closed where closable** — §8
> `[CITATION-PENDING]` rows: #23 both VERIFIED (Wilson Orion 2008 ISBN 978-0-7528-8995-5; Low et
> al. 2020 *Sports Medicine* 50:343–385 DOI 10.1007/s40279-019-01194-7); #24 Wilson VERIFIED +
> Spielverlagerung reclassified informal background per its own resolution path; #25 Wilson
> VERIFIED + the Memmert & Raabe book row REPLACED with the verified Low et al. 2020 review per
> the #10/#11 OI-003 replace-with-verifiable precedent; #26 Wilson VERIFIED, the Bradley
> score-line row stays `[CITATION-PENDING]` with a recorded July-10 environment-blocked
> verification attempt (search quota + Crossref/publisher access unavailable — not fabricated,
> per the "never fabricate" rule). **#25 Appendix A completed**: A.2 (4-3-3, 5 rows — single
> pivot deliberately excluded, rest-defence anchor) + A.3 (4-2-3-1, 6 rows — double pivot rotates
> as a pair) authored against the verified `Family433`/`Family4231` slot rosters (F442/F433/F4231
> = the complete `FormationFamily` enum), F1 hand-audits recorded. **#26 A.1 preset compositions
> pinned** against the actual #21 enum member names (PASS-1 L-2 close-out; all names verified
> present, full rosters recorded). Checklists at v0.3. Remaining open gates: the one #26 Bradley
> citation row; back-prop ERRs at `APPROVED`; #26 engine-substrate gates (upstream-owned);
> R-01..R-05 sign-off.)
> **Last Updated (prior):** July 8, 2026, later same day (**Section-file PASS-1 adversarial reviews run on
> all four IN-REVIEW specs #23–#26, all findings resolved in same-day v0.2 fix passes** — #23
> Dismarking 0H+1M+3L (M-1: the dwell-update-inside-#12-tick claim was impossible — `FilteredView`
> is built in the per-agent pass AFTER Positioning in the stride order; now a pinned one-stride-
> stale consumption contract); #24 Build-Up 0H+3M+2L (M-1: the post-regain suppression window
> armed on EVERY teammate reception — `PossessionChangedEvent` carries per-agent holder ids and
> fires on intra-team transfers, verified against the payload; now team-level-regain arming. M-2:
> zone hysteresis reformulated as committed-zone expansion, well-defined for long-ball jumps. M-3:
> catalogue lane keys corrected — fullbacks occupy wide L/R lanes, not LH/RH); #25 Rotations
> **1H**+1M+3L (H-1: §4.2's "previous-tick composed targets on `AgentPositioningData`" did not
> exist — the struct has no such field — and the restore re-seed broke FR-RO-013/T-RO-DET-003
> byte-identity; now a controller-owned SERIALIZED `LastComposedTarget` cache. M-1: phase exit
> reset dwell in the pseudocode while FR-RO-010 mandated freeze — the test plan contradicted the
> pseudocode, caught at spec stage. PASS-2 re-read clean at H/M per the High-found rule. L-3:
> `LINE_DWELL_TICKS = 5` verified, 30 ≥ 5 with 6× margin); #26 Presets 0H+1M+2L (M-1: §3.2/§3.4
> consumed engine score/halves state that does not exist — no goal producer, no halves model, and
> `MATCH_TICKS_TOTAL` was an untagged phantom; now explicit T2/T4 prerequisite gates +
> `[CROSS-PENDING]` row. L-1: Appendix E sensitivity values re-derived — ~39.4′/~52.5′, not
> ~35′/~85′). Four `adversarial-review-section-files-v1.md` files filed; §9.3 gates updated.
> Remaining open gates: `[CITATION-PENDING]` §8 rows, back-prop ERRs at `APPROVED`, #25 Appendix-A
> family completeness, R-01..R-05 sign-off.)
> **Last Updated (prior):** July 8, 2026 (**Candidates #23–#26 promoted to section files at `IN REVIEW`** —
> all four authored as full 11-file spec sets (v0.1) from the two July 7 design supplements, per
> each supplement's own §6 promotion pipeline (steps 1–3): `docs/specs/dismarking-ai/` (#23,
> FR-DM), `docs/specs/build-up-structures/` (#24, FR-BU), `docs/specs/positional-rotations/`
> (#25, FR-RO), `docs/specs/tactical-presets/` (#26, FR-TP). `SPEC_INDEX.md` registry rows added
> (**22 APPROVED / 4 IN REVIEW**); RESERVED entries retired; supplements bumped to v0.4/v0.5 with
> promotion notes + the "Specification Before Code" citation fix (a README.md heading, not
> CLAUDE.md — both §6 sections cited it wrongly). Section-file PASS-1 adversarial reviews NOT yet
> run — each spec's §9.3 records its open gates (`[CITATION-PENDING]` §8 rows; back-prop ERR
> filing at `APPROVED`; R-01..R-05 sign-off). See the updated OPEN ISSUES entry below.)
> **Last Updated (prior):** July 7, 2026, later same day (**Two design supplements opened, AR cycle
> converged same day, §6 Implementation Plans added** — for the items the same day's
> tactical-theory cross-reference flagged as too large for a cheap routing-seam reuse — see the
> new OPEN ISSUES entry below. `docs/tracking/advanced-positional-behaviors-design.md` v0.1 → v0.3
> (dismarking, scripted build-up structures, positional rotations — candidate specs #23–#25) and
> `docs/tracking/game-model-ai-manager-design.md` v0.1 → v0.4 (tactical preset library +
> AI-manager selection/adaptation — candidate spec #26). AR-1 (0H+0M+2L) + AR-2 (0H+0M+1L) +
> AR-3 (clean, CONVERGENCE). Both DESIGN SUPPLEMENT stage only (pre-promotion, no code, no
> section files) — parallel to the #21/#22 pre-approval precedent.)
> **Last Updated (prior):** July 7, 2026 (**Four cheap-item tactical additions landed** — a `MarkingOrientation` dial (#14 MAN_MARK radius scalar), a Positioning AI #12 rest-defense coverage check (dampens risky PASS/SHOOT/DRIBBLE), a half-spaces PASS bonus (routes each agent's existing #12 lane into #8's utility scorer), and a curving-press blind-side bias (#13). All default-behaviour-neutral; `SNAPSHOT_SCHEMA_VERSION` 10 → 11. See the new OPEN ISSUES entry + src/CLAUDE.md v2.9. AR-1 (0H+0M+1L, resolved: §7.x citation-collision renumbering) + AR-2 (clean, CONVERGENCE).)
> **Last Updated (prior):** July 2, 2026, latest same day (**Living World #22 slice 2 landed + AR-1 resolved** — ArcEngine (§3.4 spawn/atomic-pin/resolve/§6.2-expiry; `world.arcs` trigger draws stay the KD-10 seam per FR-LW-020/031) + ActiveSetMembership (§3.5 entry/LRU-at-cap/own-club Depart, FR-LW-023/025) wired into WorldLoop phases 4/6; AR-1: 0H+2M+4L resolved (pin-array snapshot; promotion mask check via new `ColdStore.TryPeek` verify-before-take; overflow gate; 2 doc); AR-2 full-surface: 0H+1M+2L resolved (Add mask gate + upfront entity validation close the residual FR-LW-025 strand vectors; scope docs); AR-3: 0H+0M+2L doc-only — **CONVERGENCE, slice-2 AR cycle closed** — 24-test suite. See the updated OPEN ISSUES entry + src/CLAUDE.md v1.94–v1.97.)
> **Last Updated (prior):** July 2, 2026, later same day (**Living World #22 season/world loop slice 1 landed** — the first KD-10 prerequisite (#22 §7.1 "persistent world store + season-calendar loop"). New `src/living-world/` services on the T0 data types: `WorldClock` (KD-4 — worldTick = calendar day, never the match loops), `WorldLoop` (§4.2 phase order; phase-3 decay live, phases 1/2/4/5/6 documented seams — no phantom interfaces per FR-LW-031), `MemoryStore` (canonical-order edges; §3.2 evict-before-append + FR-LW-018 pins; §3.1 owned-layer ApplyEvent, PlayerEdge refused), `ColdStore` (§3.5 Compress/Rehydrate; Residue-A v1 schema recorded; FR-LW-009 episodeId resume). 20-test suite. See the new OPEN ISSUES entry + src/CLAUDE.md v1.90.)
> **Last Updated (prior):** July 2, 2026 (**Minimal match viewer landed** — first presentation-layer surface. New `src/match-viewer/` assembly (`TacticalDirector.MatchViewer`; presentation tooling, not a numbered spec): `MatchReplayRecorder` ticks a real `MatchEngine` and samples world state between ticks through a new public read-only observation surface (`MatchEngine.cs` v1.24: `BallView`/`AgentView(i)`/`AgentTeamId(i)`/`AgentIsGoalkeeper(i)`/`PossessingAgentId` — value-type copies, no behaviour change); `HtmlReplayExporter` emits a single self-contained HTML canvas replay (pitch markings, home/away/GK/possession/ball-height cues, play/pause/scrub/speed; NOT a determinism-pinned wire format). Observer-neutrality digest-locked by `MatchViewerTests` (recorded run == unobserved same-seed run). See the new OPEN ISSUES entry.)
> **Last Updated (prior):** June 28, 2026 (Status check confirms `tools/dotnet-ci/known-failures.txt` quarantine is empty — the June 12 burn-down (see OPEN ISSUES "Dotnet CI gate quarantine burn-down — RESOLVED") holds. Also surfaced: this file's history had not mentioned the **Match Engine** integration layer (`src/match-engine/`, governed by `docs/tracking/match-engine-design.md`, NOT a numbered spec) — the composition root wiring all 20 approved subsystems into the `deterministic-sim` 7-phase tick pipeline. Phases A–E are complete as of June 27, 2026 (full canonical world-state snapshot serialization through `SNAPSHOT_SCHEMA_VERSION` 8; Physics/Resolve/AI-phase wiring through Positioning→Pressing→Defensive→Attacking→DecisionTree; Events-phase possession-changed producer/consumer). **Phase F (capstone closed-loop scenario on the #19 `ScenarioRunner`) is the only remaining phase** — see new OPEN ISSUES entry below. README.md and file-manifest.md status sections updated to match.)
> **Last Updated (prior):** June 12, 2026 (Non-certifying Linux compile/test CI gate landed (`tools/dotnet-ci/` + `dotnet-compile-test` job in ci.yml): asmdef→csproj generator + ~6-type UnityEngine shim compiles the ENTIRE src/ tree (production netstandard2.1 = Unity 2022.3 BCL surface) and runs every NUnit suite under `dotnet test` on ubuntu — closing the verification gap behind the seven consecutive structurally-dead build surfaces. First-ever full-tree compile found EIGHT more never-compiled surfaces, headlined by ERR-017-002 (H): #17 §3.2.1/§3.2.2 specified Publish/Subscribe overloads distinguished ONLY by generic constraint — illegal C# (CS0111) — implemented verbatim in EventBus + five spec EventBusStub files, so the event-system PRODUCTION assembly never compiled; spec patched same commit (section-3.md v1.0.2), code now single `where T : struct` methods with cached EventTierCache<T> marker dispatch, call sites unchanged. Also: ProfilerMarker imported from the wrong namespace ×18 files; File.Move(overwrite:) absent from netstandard2.1 (SaveManager); ShotExecutor PascalCase vs ALL_CAPS enum members; missing usings (CoverShadowSelector Span<T>, UtilityScorer FilteredView — decision-tree was STILL dead post-June-11); GoalkeeperMechanics int?→int; the SIXTH stray-brace dead test suite (ShotMechanicsTests §5.12 fixture); DefensiveAITests' 51 internal [Test] methods (NUnit requires public — suite could never run); NUnit API misuse in two suites; EventRegistry static-init order fragility (EnsureInitialized() fix); SipHash old-fixture vectors 4–7 FABRICATED (production correct per independent mirror). Then 1,165 tests executed for the first time in project history; 30 genuine model/expectation failures quarantined shrinking-only (tools/dotnet-ci/known-failures.txt + docs/tracking/dotnet-ci-quarantine.md, per-test hypotheses filed) — any NEW failure or compile error fails CI. Gate is explicitly NON-CERTIFYING (certification-platform.md v1.2): determinism certification stays on the pinned Windows/Unity tuple. See src/CLAUDE.md v1.66 and the new OPEN ISSUES entry.)
> **Last Updated (prior):** June 11, 2026, later same day (Decision Tree #8 comprehensive audit (AR-2) completed — 3H+11M+9L over spec + implementation; assembly had never compiled (static calls to instance executors, missing asmdef ref); away-team zone modifiers/press urgency/line-depth all home/away-asymmetric (every prior example and fixture was home-team); §3.7.2 state machine implemented (PASS/SHOOT hold EXECUTING; forced-refresh same-type suppression); ERR-008-002..011 filed, spec patched same commit; see the resolved OPEN ISSUES entry below and `docs/specs/decision-tree/audit-report.md`.)
> **Last Updated (prior):** June 11, 2026 (Pass Mechanics #5 AR-9 fix pass: 1H+3M+5L, then AR-10 sweep: 2L (resolved same commit; no functional findings) — the FIFTH consecutive spec whose test suite was structurally incapable of catching its defects. H-1: `src/pass-mechanics/Tests/PassMechanicsTests.cs` has NEVER compiled since v1.1 (2026-06-01) — namespace closed before the appended IT- integration fixture, stray `}` at EOF (CS1022), fixture stranded in the global namespace; identical defect class to First Touch ERR-004 (170/171 braces there, 161/162 here). All AR-2..AR-8 "the test suite enforces X" claims were unverifiable while the suite was dead. M-1: PassExecutor Idle-guard rejection stomped `_lastResult` — an Execute() during FollowThrough/Complete destroyed the committed Completed record (ContactFrame replay-sync data) and surfaced Invalid at the next IsIdle; rejection now reported via return value only. M-2: FM-07 distance gate `d <= 0f` passed NaN (compares false) and Mathf.Max argument ordering silently sanitised it to a 0.001 m pass; gate now `!(d > 0f) || IsInfinity(d)` per the project NaN-gate pattern (FT AR-8 M-1 / AM AR-10 / CS AR-7). M-3: stale tackle flag — cleared only by WINDUP polling, so a tackle registered during FollowThrough/Idle (even while not in possession) spuriously cancelled the agent's NEXT pass on its first WINDUP frame; drained (discarded) at INITIATING per §3.8.5 freshness. L: CONTACT pressure re-sample now queries passer position fresh (INITIATING cache was up to ~15 frames stale on a pass on the run); ComputeErrorAngle NaN fallback flipped MinErrorAngle → MaxErrorAngle (failed OPEN — corrupted input produced a 0.1° laser pass); declared-but-unconsumed doc-notes (PhysicalProfile.DistMin/DominantSpin/IsAerial incl. the IsAerialFormula parallel-surface hazard with 9-profile agreement verified, PassAgentAttributes.Crossing); PassOutcome.Cancelled / PassAgentState.Position doc corrections; through-ball SPEC-DEVIATION NOTE (kickSpeed derived from IntendedDistance BEFORE the lead projection extends the aim point ⇒ led passes systematically underhit; joins KD-4 / §7.1 Stage 1 upgrade). New PassExecutorGuardTests fixture PX-001..004 locks M-1/M-2/M-3 via pure stub seams (no EventBus boot — all paths terminate pre-publish). Files: PassExecutor.cs v1.12, PassErrorCalculator.cs v1.8, PassTargetResolver.cs v1.8, PhysicalProfile.cs v1.2, PassAgentAttributes.cs v1.1, PassAgentState.cs v1.1, PassOutcome.cs v1.3, Tests/PassMechanicsTests.cs v1.2, src/CLAUDE.md v1.63, file-manifest.md header.)
> **Last Updated (prior):** June 10, 2026 (Collision System #3 AR-7 fix pass: 1H+3M+3L, then AR-8 sweep: 1H+1M+1L, then AR-9 sweep: 2L doc-only — Agility unconsumed-field pointer, AGENT_BALL ContactPoint Z claim, then AR-10 sweep: 2L, no functional findings — dead MaxIterations doc-noted, tracking-row tally corrected; mechanical verification of bitfield uniqueness (253 pairs), 3×3 broad-phase coverage, and emit gating (all resolved; ERR-003-001..006 filed and closed same day). Both H findings were closed-loop model defects the test suite ENCODED rather than caught. AR-7 H-1/ERR-003-001: F = j × 60 Hz assumed the whole impulse acts in one 16.7 ms frame — the entire stochastic fall/stumble band (500–1500 N literature values) sat below walking pace (P(fall)=1 at ~0.5 m/s closing; knockdownForceOut pinned at 1.0); new [GT] ContactDurationS (0.15 s), F = j / ContactDurationS, PHYSICS_TICK_HZ removed (sole consumer). AR-8 H-1/ERR-003-005: impulse approach gate INVERTED — with the a1→a2 manifold normal, vRel=(v1−v2)·n>0 is approaching, but the gate returned separation-only for vRel>0, so genuine closing collisions exchanged no momentum and EvaluateFallOrStumble was unreachable for real contacts, while overlapped pairs already moving apart were velocity-reversed back inward (CR-001 rationalised this as a 'passed-through state'); gate + impulse signs corrected (j>0 invariant preserved), restitution verified e·v. M: FROM_BEHIND broken on three surfaces — formula sign (ERR-003-002), unflipped instigator→victim normal at the call site (same ERR), and shadowing by the velocity-only shoulder predicate (ERR-003-006, AR-8); same-team hits above fallThreshold escaped both fall and stumble branches (ERR-003-003); MaxCollisionPairs valve counted broad-phase candidates and aborted the whole frame in goalmouth densities (ERR-003-004 — now counts narrow-phase CONFIRMED collisions, cap = event-buffer capacity). L: non-finite velocity sanitised to zero at the snapshot gate (NaN previously published into CollisionEvent.ImpactForce); both-grounded overlaps no longer emit 60 zero-force events/s; RecordEvent drop warning; CellX/CellY FloorToInt (cell 0 was double-width for negative coords). Spec §3.3/§3.4 pseudocode patched in the same commit (6 ERR anchors); CONTACT_DURATION_S added to the §3.3 catalogue. All test expectations re-derived for the corrected model and verified by a numerical mirror including the xorshift128+ RNG (FL-002 5210/10000 stumbles vs 0.5175 predicted; FL-003 5073 falls; FL-004 90/0; CR-001 ∓1.5 m/s). Files: CollisionResponse.cs v1.6, CollisionSystem.cs v1.6, ContactTypeClassifier.cs v1.3, CollisionSystemConstants.cs v1.5, SpatialHashGrid.cs v1.4, CollisionEvent.cs v1.2, ContactForceData.cs v1.1, AgentAgentCollisionResult.cs v1.2, tests/CollisionSystemTests.cs v1.3, spec-error-log.md v1.24, docs/specs/collision-system/section-3-3.md + section-3-4.md.) Prior June 9, 2026 (Agent Movement #2 AR-12 fix pass: 3H+1M+3L, then AR-13 sweep 2M (both resolved). The three H findings were closed-loop speed-control defects invisible to pure-function/mid-flight tests: H-1 agents at rest could never start moving (IDLE branch only decayed speed while EvaluateFromIdle required speed > IdleExit — IDLE now accelerates toward the command-capped topSpeed on moving intent); H-2 commandSpeed never capped the speed-integration target (jog commands auto-promoted to SPRINTING and drained the reservoir; walk commands flapped WALKING→JOGGING→DECELERATING — Step 4–5 now applies topSpeed = min(topSpeed, commandSpeed) and ApplyAcceleration gains an asymptote ceiling); H-3 Zeno deceleration (per-frame a = v²/(2d) against the fixed total d → ~78 s / ~32 m to stop from 6 m/s; new MinDecelerationFloor [GT] bounds the tail; §3.2.5 constant-rate spec-deviation note filed). M-1 LeanAngle now reflects velocity-direction path curvature, not facing rotation. AR-13: exhausted agents (AerobicPool < AerobicJogFloor) with jog commands clamp commandSpeed to JogEnter (kills a ~3 Hz aerobic-gate flap); IDLE launch additionally gated on a non-degenerate target offset so the Decision Tree HOLD shape (StrafeWhileWatching at own position) keeps resting agents at rest. New MovementCommand.WalkTo factory; closed-loop regression fixture T-AM-110..115 + decel-floor units T-AM-108..109. Files: AgentMovementSystem.cs v1.15, AgentLocomotion.cs v1.5, AgentDirectionalMovement.cs v1.7, AgentMovementConstants.cs v1.9, AgentState.cs v1.5, GroundedReason.cs v1.1, MovementCommand.cs v1.3, tests v2.2/v1.1, test-plan.md v0.3. Prior June 9, 2026 (Ball Physics #1 AR-7 fix pass: 2H+4M+3L, then AR-8 sweep 2L (resolved) — clean. H-1/ERR-001-001: bounce ground normal was Unity Y-up `Vector3.up` in the Z-up coordinate system — a falling ball never rebounded; fixed in `BallGroundInteraction.cs` AND in the §3.1.8.1 spec pseudocode that sourced it. H-2: ValidatePhysicsState ground clamp zeroed Velocity.z before the state machine could see vz<0, trapping fast descents in a permanent Airborne ground-hover; Airborne now keeps vz through the clamp. M-1/ERR-001-002: friction stick impulse gains the 1+m·r²/I=2.5 coupling divisor. M-2..M-4 + L: test spin-sign convention fixed, gravity added to the Bouncing branch, LongBall test windows re-derived from the model (verified numerically), magic literals catalogued, MomentOfInertia retagged [DERIVED], ERR-001-003 [EST] inventory filed. Prior June 8, 2026 (Pass Mechanics #5 AR-8 fix pass: 0M + 3L. L-1: AR-7's CrossSubType-ignore warning brace-add left an empty `if (cond) { }` in production builds; gate hoisted to wrap the entire if-statement since the diagnostic has no functional follow-up (the other 7 AR-7-gated emits MUST keep the body-gate form because their if-bodies contain `_lastResult` + `return`). L-2: `ExecuteContact` state transition (`_state = FollowThrough`) hoisted above Step 8 `EventBusStub.Publish` — if Publish throws, the ball was already kicked at Step 6 and the executor must not stay in `Contact` (re-entry would re-run `ApplyKick`); the FM-08 possession recheck currently guards against the double-kick, but defensive ordering removes the dependence on the recovery seam. L-3: forward-reference notes inserted next to the AR-2 M-2 v1.6 / v1.3 history rows in `PassExecutor.cs` and `PassTargetResolver.cs` — the "[-1, +1]" characterisation there is the AR-2-era contract, superseded by AR-6 L-1 to "[-1, +1)"; historical rows preserved verbatim. Files: `PassExecutor.cs` v1.11, `PassTargetResolver.cs` v1.7. AR-8 sweep clean — no further high or medium issues. Prior June 8, 2026 (Pass Mechanics #5 AR-7 fix pass: 1M+3L all resolved on a fresh-eyes full-surface sweep over all 24 files in `src/pass-mechanics/`. M-1: FR-CS-031 gating drift fixed across sibling files — `PassMechanicsConstants` v1.2 (AR-2 L-13) gated its FM-01 `Debug.LogError` emits but the parallel cold-path emits in `PassExecutor.cs` (8 emits), `PassTypeProfiles.cs` (2 emits), and `PassVelocityCalculator.cs` (2 emits) never got the same `#if UNITY_EDITOR || DEVELOPMENT_BUILD` gating. All 12 emits now gated. L-1: `[-1, +1]` → `[-1, +1)` propagation from AR-6 producer-side correction to the two consumer-side surfaces — `PassTargetResolver.ApplyErrorToDirection` `<summary>` and `<param>` for `errorDirectionFraction`, plus the `PassExecutor.ExecuteContact` Step 3 callsite comment. L-2: `EventBusRegistrar.cs` v1.3 history row's "no `InternalsVisibleTo` on this assembly" rationale was already stale at AR-3 time — `AssemblyInfo.cs` created 2026-06-01 with `[InternalsVisibleTo("TacticalDirector.PassMechanics.Tests")]`; corrected to the boundary-mocking rationale alone. L-3: `CrossSubType` and `PassType` enums gained the ORDINAL STABILITY paragraph parallel to `CancelReason` v1.4. `PassType` carries a stronger contract — beyond being embedded in both `PassAttemptEvent` (0x0C) and `PassCancelledEvent` (0x0D) payloads, `(int)_request.PassType` is the third hash input to `ComputeErrorDirection`, so reordering would break deterministic error-direction parity even before the event digest catches the drift. Files: `PassExecutor.cs` v1.10, `PassTypeProfiles.cs` v1.4, `PassVelocityCalculator.cs` v1.4, `PassTargetResolver.cs` v1.6, `EventBusRegistrar.cs` v1.4, `CrossSubType.cs` v1.1, `PassType.cs` v1.2. AR-7 sweep clean. Prior June 8, 2026 (Pass Mechanics #5 AR-6 fix pass: 1M+3L all resolved — converts the AR-5 cycle-stop. M-1 finished what AR-5 started: input-mix primes for `frameNumber` / `passTypeIndex` in `PassErrorCalculator.ComputeErrorDirection` replaced with xxHash64 PRIME64_3 (`0x165667B19E3779F9`) and PRIME64_5 (`0x27D4EB2F165667C5`) so the Stafford Mix13 finalizer no longer multiplies through the same primes the input-mix already used (`0xBF58476D1CE4E5B9` and `0x94D049BB133111EB` remain as finalizer multipliers only). Input-mix primes are now disjoint from finalizer primes on all three axes, completing the AR-5 M-1 invariant. L-1: `<returns>` upper bound corrected `[-1, +1]` → `[-1, +1)` to match the 24-bit mantissa quantisation (EC-010 already enforces `Assert.Less(dir, 1.0f)`). L-2: comment block rewritten to call out the additive-vs-XOR asymmetry on `agentId` (AR-5 intent) and record the AR-6 input-mix/finalizer prime disjointness invariant. L-3: bit-extraction literals `0x00FFFFFFu` / `0x01000000u` promoted to named local consts `Mantissa24Mask` / `Mantissa24Scale` with a comment noting the 24-bit window matches float mantissa precision. Files: `src/pass-mechanics/PassErrorCalculator.cs` v1.7. Closes the long-standing cycle-stop carve-out. Prior June 8, 2026 (cross-spec routing close-out: `Possession.ControlHeight` ↔ `GroundControlHeight` resolved — Ball Physics #1 §3.1.11 is the authority, First Touch #4 `GroundControlHeight` is now a `[CROSS]` mirror; sibling-hazard sweep (`ControlRadius` / `ControlVelocity` / `ChallengeRadius`) returns no other parallel declarations. Prior June 7, 2026 (AR-hardening sweep complete: every coded section's last adversarial round now yields no findings or L-only — except Pass Mechanics #5 AR-5 (1M+3L) which carried an explicit "cycle stop" (converted to AR-6 above on June 8, 2026). Final AR by spec: #1 AR-8 (2L resolved; AR-7 2H+4M+3L fixed June 9, 2026) ✓; #2 AR-11 (2L) ✓; #3 AR-6 (3L) ✓; #4 AR-6 (3L) ✓; #5 AR-10 (2L, no functional findings; AR-9 1H+3M+5L fixed June 11, 2026) ✓; #6 AR-4 (3L) ✓; #7 AR-2 (3L) ✓; #8 AR-2 (clean) ✓; #10 AR-2 (clean) ✓; #11 AR-3 (clean) ✓; #12 AR-3 (clean) ✓; #13 AR-2 (clean) ✓; #14 AR-2 (clean) ✓; #15 AR-3 (clean) ✓; #16 AR-3 (1L) ✓; #17 AR-11 (no findings) ✓; #18 AR-4 (2L) ✓; #19 AR-5 (2L) + PR #132 Codex P2 follow-up ✓. Significant test scaffolding landed: Ball Physics enum-ordinal-stability + body-part-coefficients + surface-properties tests; Agent Movement T-AM-001..107 regression + unit roster (18 + 59 NUnit tests across 11 fixtures); `docs/specs/agent-movement/test-plan.md` v0.2. The PR #132 Codex P2 follow-up to `PerfGateRunner.Run` rejects mismatched perf-baseline pairs via `ArgumentException` before delegating to `RegressionGate.Evaluate` — FR-PO-031 requires same scenario, seed, platform pin, and loop; runner validates `baseline.Loop == current.Loop` unconditionally and `ScenarioManifestId` / `Seed` / `PlatformPin` when both records carry a non-null `SessionManifest`. Stage 0 host platform pin landed same day in `docs/tracking/certification-platform.md` v1.1: Windows 11 / Unity 2022.3.62f1 / Mono / x64 / SSE4.2 / 1 worker / DAZ+FTZ+fp-contract+FMA all off. Closes the long-standing OPEN ISSUE; unblocks FR-DS-009-GATE Stage 0 activation, FR-PO-052 perf-gate, #19 §7.5 D1 test-runner pin, #18 §3.9.4 warmup-measurement path, and #16 §4.8 EnvironmentFingerprint digest semantics.)
> **Last Updated (prior):** June 10, 2026, latest same day (First Touch #4 scenario corpus: `heavy-touch-runs-on` (ERR-004-003 displacement-velocity coherence lock) + `interception-chain-anchors-at-displaced-ball` (ERR-004-004 ball-anchored gate via real PressureEvaluator, §3.4.5 redirect + Frame N+1 CONTROLLED chain) on the #19 ScenarioRunner; new AssemblyInfo.cs InternalsVisibleTo; envelope windows mirror-derived. Prior same day: First Touch #4 AR-7 fix pass: 1H+3M+3L, then AR-8 sweep: 2M — ERR-004-003..006 filed; ERR-004-003/004/006 closed same day, ERR-004-005 documented-open. H-1/ERR-004-003: §3.3.2 direction-blend sign inverted — heavy touches displaced the ball back toward the passer against their own §3.3.5 retained momentum; spec pseudocode patched same commit; the test suite encoded BOTH sign conventions at once and had never compiled (unbalanced brace since v1.1). M/ERR-004-004: interception proximity re-anchored from the agent to the displaced ball per §3.4.2 (PressureEvaluator now supplies the global-nearest opponent position). §3.4.5 interception velocity redirect implemented (was specified, never coded). AR-8: EvaluateFirstTouch non-finite input sanitise gate (Clamp01 passes NaN); §5.10 VS-001 hand-calc used a non-§3.2.3 additive velocity modifier (ERR-004-006, spec + test re-derived to r≈0.195 m). All expectations verified by a full-pipeline numerical mirror. Files: 8 src files + tests v1.2 + 3 spec section files + spec-error-log v1.25.) Prior same day (Scenario-corpus expansion on the #19 ScenarioRunner. Spec #1 per-spec corpus: `drop-and-rebound` (AR-7 H-1 / ERR-001-001 lock — load-bearing predicates are the 1.0–1.45 m first-rebound-peak window and exact X/Y purity of a spinless vertical drop) + `fast-descent-grounds-out` (AR-7 H-2 hover-deadlock lock, extended to the full composed settle) in `src/ball-physics/tests/BallPhysicsScenarios.cs` + `BallPhysicsScenarioTests.cs`; envelope windows derived from a numerical mirror of the fixed model. First cross-spec corpus per KD-8: `lofted-pass-kick-bounce-roll` under `tests/scenarios/cross-spec/` (owning specs {1, 5}) chains the real `PassExecutor` (#5) WINDUP→CONTACT lifecycle into the real `BallPhysicsCore` loop (#1) through the `IPassBallSystem` seam, with #17 EventBus boot wiring + one-tick Resolve-phase lifecycle around the CONTACT publish — the composition surface where per-spec suites passed while the chain died at first touch-down pre-AR-7. New files: CrossSpecScenarios.cs / CrossSpecScenarioTests.cs in `src/testing-strategy/Tests/`; asmdef reference updates (ball-physics-tests + testing-strategy-tests); file-manifest reconciled (incl. three June-7 ball-physics test rows missing from its per-file table). src/CLAUDE.md v1.60.) Prior June 10, 2026, still later same day (Testing Strategy #19 ScenarioRunner AR-2 sweep: 0H+0M+2L, both resolved — NaN in_range bounds now throw as authoring errors instead of masquerading as failing predicates; min>max exception message InvariantCulture. ScenarioEnvelope v1.2, ScenarioRunnerTests v1.2 (19 tests), src/CLAUDE.md v1.59. Otherwise clean.) Prior June 10, 2026, later same day (Testing Strategy #19 ScenarioRunner AR-1 fix pass: 0H+4M+6L, all resolved. M-1 entry/scenario manifest-coherence guard (a ClosedLoopScenario registered under a different manifest instance than it executes would pass load-time validation against a manifest the run never uses); M-2 non-empty `fixture_refs` refused at Stage 0 (no fixture loader exists until the Stage 0+1 KD-10 deliverable; §3.3.4 forbids silent acceptance); M-3 diagnostics hardening (CR/LF sanitized out of predicate IDs / details / exception messages so the line-oriented key=value encoding cannot be corrupted; `exception_stack=` line added — a thrown body previously dropped its stack); M-4 A.1 name-uniqueness now actually enforced (v1.0 doc claimed it via path-uniqueness), plus §3.3.5 path↔name coherence and cross-spec ≥2 owning-spec arity (`SCENARIO_PATH_CROSS_SPEC_PREFIX` [FIXED] added). L: FR-TS-070 format-version check hoisted before field interpretation; ReadOnlyCollection wrappers on manifest lists (castable-array seam, parallels #18 AR-1 L-3); InvariantCulture detail strings; T-AM-115 position-unchanged restored to exact equality (migration had silently weakened it to Vector2's approximate ==); ScenarioIndexEntry split to its own file per FILE NAMING precedent; IScenario KD-7 wording clarified as implementation obligation. ScenarioRunnerTests 12→18. src/CLAUDE.md v1.58; file-manifest reconciled.) Prior June 10, 2026 (Stage 0 closed-loop scenario harness: Spec #19 §3.3.3 `ScenarioRunner` implemented now rather than at Stage 0+1, motivated by the third consecutive spec — Ball Physics AR-7, Agent Movement AR-12/AR-13 — where H/M-class closed-loop defects were *encoded by* pure-function unit suites rather than caught by them; per-function tests verify the spec as written, only a closed-loop run verifies the spec as composed. Contract honored: single entry point `Run(manifestPath, seed)`; manifest as sole input (in-memory Appendix A.1 manifests — the on-disk `index.<ext>` encoding remains D1-pinned at Stage 0+1, so the index is injected as an immutable in-code value and the Stage 0+1 file loader is a parser swap); KD-7 verbatim seeding of `DeterministicRngService` before any subsystem init; refusal of unindexed scenarios (FR-TS-028) and unknown `format_version` (FR-TS-070) as load-time `ArgumentException`s per §3.3.4; implicit pass forbidden — zero recorded envelope predicates ⇒ Failed (FR-TS-030). New: 9 harness files + Tests in `src/testing-strategy/` (ScenarioStatus/Result/Manifest/Envelope/Context/IScenario/ClosedLoopScenario/ScenarioIndex/ScenarioRunner; ScenarioRunnerTests.cs 12 contract tests); `TestingStrategyConstants.cs` v1.3 (`SCENARIO_MANIFEST_FORMAT_VERSION`). First fixture corpus: T-AM-110..115 migrated from `AgentMovementTests.cs` (v2.3) to `AgentMovementScenarios.cs` (bodies + A.1 manifests) + `AgentMovementScenarioTests.cs` — the project's first Simulation-layer tests (`sim_<scenario>` per #19 §3.1.4); requirement IDs and assertion substance unchanged; `agent-movement-tests.asmdef` gains the testing-strategy reference; `test-plan.md` v0.4; `file-manifest.md` gains a per-file `src/testing-strategy/` section. src/CLAUDE.md v1.57. Prior June 9, 2026 (Agent Movement #2 AR-12 fix pass: 3H+1M+3L, then AR-13 sweep 2M (both resolved). The three H findings were closed-loop speed-control defects invisible to pure-function/mid-flight tests: H-1 agents at rest could never start moving (IDLE branch only decayed speed while EvaluateFromIdle required speed > IdleExit — IDLE now accelerates toward the command-capped topSpeed on moving intent); H-2 commandSpeed never capped the speed-integration target (jog commands auto-promoted to SPRINTING and drained the reservoir; walk commands flapped WALKING→JOGGING→DECELERATING — Step 4–5 now applies topSpeed = min(topSpeed, commandSpeed) and ApplyAcceleration gains an asymptote ceiling); H-3 Zeno deceleration (per-frame a = v²/(2d) against the fixed total d → ~78 s / ~32 m to stop from 6 m/s; new MinDecelerationFloor [GT] bounds the tail; §3.2.5 constant-rate spec-deviation note filed). M-1 LeanAngle now reflects velocity-direction path curvature, not facing rotation. AR-13: exhausted agents (AerobicPool < AerobicJogFloor) with jog commands clamp commandSpeed to JogEnter (kills a ~3 Hz aerobic-gate flap); IDLE launch additionally gated on a non-degenerate target offset so the Decision Tree HOLD shape (StrafeWhileWatching at own position) keeps resting agents at rest. New MovementCommand.WalkTo factory; closed-loop regression fixture T-AM-110..115 + decel-floor units T-AM-108..109. Files: AgentMovementSystem.cs v1.15, AgentLocomotion.cs v1.5, AgentDirectionalMovement.cs v1.7, AgentMovementConstants.cs v1.9, AgentState.cs v1.5, GroundedReason.cs v1.1, MovementCommand.cs v1.3, tests v2.2/v1.1, test-plan.md v0.3. Prior June 9, 2026 (Ball Physics #1 AR-7 fix pass: 2H+4M+3L, then AR-8 sweep 2L (resolved) — clean. H-1/ERR-001-001: bounce ground normal was Unity Y-up `Vector3.up` in the Z-up coordinate system — a falling ball never rebounded; fixed in `BallGroundInteraction.cs` AND in the §3.1.8.1 spec pseudocode that sourced it. H-2: ValidatePhysicsState ground clamp zeroed Velocity.z before the state machine could see vz<0, trapping fast descents in a permanent Airborne ground-hover; Airborne now keeps vz through the clamp. M-1/ERR-001-002: friction stick impulse gains the 1+m·r²/I=2.5 coupling divisor. M-2..M-4 + L: test spin-sign convention fixed, gravity added to the Bouncing branch, LongBall test windows re-derived from the model (verified numerically), magic literals catalogued, MomentOfInertia retagged [DERIVED], ERR-001-003 [EST] inventory filed. Prior June 8, 2026 (Pass Mechanics #5 AR-8 fix pass: 0M + 3L. L-1: AR-7's CrossSubType-ignore warning brace-add left an empty `if (cond) { }` in production builds; gate hoisted to wrap the entire if-statement since the diagnostic has no functional follow-up (the other 7 AR-7-gated emits MUST keep the body-gate form because their if-bodies contain `_lastResult` + `return`). L-2: `ExecuteContact` state transition (`_state = FollowThrough`) hoisted above Step 8 `EventBusStub.Publish` — if Publish throws, the ball was already kicked at Step 6 and the executor must not stay in `Contact` (re-entry would re-run `ApplyKick`); the FM-08 possession recheck currently guards against the double-kick, but defensive ordering removes the dependence on the recovery seam. L-3: forward-reference notes inserted next to the AR-2 M-2 v1.6 / v1.3 history rows in `PassExecutor.cs` and `PassTargetResolver.cs` — the "[-1, +1]" characterisation there is the AR-2-era contract, superseded by AR-6 L-1 to "[-1, +1)"; historical rows preserved verbatim. Files: `PassExecutor.cs` v1.11, `PassTargetResolver.cs` v1.7. AR-8 sweep clean — no further high or medium issues. Prior June 8, 2026 (Pass Mechanics #5 AR-7 fix pass: 1M+3L all resolved on a fresh-eyes full-surface sweep over all 24 files in `src/pass-mechanics/`. M-1: FR-CS-031 gating drift fixed across sibling files — `PassMechanicsConstants` v1.2 (AR-2 L-13) gated its FM-01 `Debug.LogError` emits but the parallel cold-path emits in `PassExecutor.cs` (8 emits), `PassTypeProfiles.cs` (2 emits), and `PassVelocityCalculator.cs` (2 emits) never got the same `#if UNITY_EDITOR || DEVELOPMENT_BUILD` gating. All 12 emits now gated. L-1: `[-1, +1]` → `[-1, +1)` propagation from AR-6 producer-side correction to the two consumer-side surfaces — `PassTargetResolver.ApplyErrorToDirection` `<summary>` and `<param>` for `errorDirectionFraction`, plus the `PassExecutor.ExecuteContact` Step 3 callsite comment. L-2: `EventBusRegistrar.cs` v1.3 history row's "no `InternalsVisibleTo` on this assembly" rationale was already stale at AR-3 time — `AssemblyInfo.cs` created 2026-06-01 with `[InternalsVisibleTo("TacticalDirector.PassMechanics.Tests")]`; corrected to the boundary-mocking rationale alone. L-3: `CrossSubType` and `PassType` enums gained the ORDINAL STABILITY paragraph parallel to `CancelReason` v1.4. `PassType` carries a stronger contract — beyond being embedded in both `PassAttemptEvent` (0x0C) and `PassCancelledEvent` (0x0D) payloads, `(int)_request.PassType` is the third hash input to `ComputeErrorDirection`, so reordering would break deterministic error-direction parity even before the event digest catches the drift. Files: `PassExecutor.cs` v1.10, `PassTypeProfiles.cs` v1.4, `PassVelocityCalculator.cs` v1.4, `PassTargetResolver.cs` v1.6, `EventBusRegistrar.cs` v1.4, `CrossSubType.cs` v1.1, `PassType.cs` v1.2. AR-7 sweep clean. Prior June 8, 2026 (Pass Mechanics #5 AR-6 fix pass: 1M+3L all resolved — converts the AR-5 cycle-stop. M-1 finished what AR-5 started: input-mix primes for `frameNumber` / `passTypeIndex` in `PassErrorCalculator.ComputeErrorDirection` replaced with xxHash64 PRIME64_3 (`0x165667B19E3779F9`) and PRIME64_5 (`0x27D4EB2F165667C5`) so the Stafford Mix13 finalizer no longer multiplies through the same primes the input-mix already used (`0xBF58476D1CE4E5B9` and `0x94D049BB133111EB` remain as finalizer multipliers only). Input-mix primes are now disjoint from finalizer primes on all three axes, completing the AR-5 M-1 invariant. L-1: `<returns>` upper bound corrected `[-1, +1]` → `[-1, +1)` to match the 24-bit mantissa quantisation (EC-010 already enforces `Assert.Less(dir, 1.0f)`). L-2: comment block rewritten to call out the additive-vs-XOR asymmetry on `agentId` (AR-5 intent) and record the AR-6 input-mix/finalizer prime disjointness invariant. L-3: bit-extraction literals `0x00FFFFFFu` / `0x01000000u` promoted to named local consts `Mantissa24Mask` / `Mantissa24Scale` with a comment noting the 24-bit window matches float mantissa precision. Files: `src/pass-mechanics/PassErrorCalculator.cs` v1.7. Closes the long-standing cycle-stop carve-out. Prior June 8, 2026 (cross-spec routing close-out: `Possession.ControlHeight` ↔ `GroundControlHeight` resolved — Ball Physics #1 §3.1.11 is the authority, First Touch #4 `GroundControlHeight` is now a `[CROSS]` mirror; sibling-hazard sweep (`ControlRadius` / `ControlVelocity` / `ChallengeRadius`) returns no other parallel declarations. Prior June 7, 2026 (AR-hardening sweep complete: every coded section's last adversarial round now yields no findings or L-only — except Pass Mechanics #5 AR-5 (1M+3L) which carried an explicit "cycle stop" (converted to AR-6 above on June 8, 2026). Final AR by spec: #1 AR-8 (2L resolved; AR-7 2H+4M+3L fixed June 9, 2026) ✓; #2 AR-11 (2L) ✓; #3 AR-6 (3L) ✓; #4 AR-6 (3L) ✓; #5 AR-10 (2L, no functional findings; AR-9 1H+3M+5L fixed June 11, 2026) ✓; #6 AR-4 (3L) ✓; #7 AR-2 (3L) ✓; #8 AR-2 (clean) ✓; #10 AR-2 (clean) ✓; #11 AR-3 (clean) ✓; #12 AR-3 (clean) ✓; #13 AR-2 (clean) ✓; #14 AR-2 (clean) ✓; #15 AR-3 (clean) ✓; #16 AR-3 (1L) ✓; #17 AR-11 (no findings) ✓; #18 AR-4 (2L) ✓; #19 AR-5 (2L) + PR #132 Codex P2 follow-up ✓. Significant test scaffolding landed: Ball Physics enum-ordinal-stability + body-part-coefficients + surface-properties tests; Agent Movement T-AM-001..107 regression + unit roster (18 + 59 NUnit tests across 11 fixtures); `docs/specs/agent-movement/test-plan.md` v0.2. The PR #132 Codex P2 follow-up to `PerfGateRunner.Run` rejects mismatched perf-baseline pairs via `ArgumentException` before delegating to `RegressionGate.Evaluate` — FR-PO-031 requires same scenario, seed, platform pin, and loop; runner validates `baseline.Loop == current.Loop` unconditionally and `ScenarioManifestId` / `Seed` / `PlatformPin` when both records carry a non-null `SessionManifest`. Stage 0 host platform pin landed same day in `docs/tracking/certification-platform.md` v1.1: Windows 11 / Unity 2022.3.62f1 / Mono / x64 / SSE4.2 / 1 worker / DAZ+FTZ+fp-contract+FMA all off. Closes the long-standing OPEN ISSUE; unblocks FR-DS-009-GATE Stage 0 activation, FR-PO-052 perf-gate, #19 §7.5 D1 test-runner pin, #18 §3.9.4 warmup-measurement path, and #16 §4.8 EnvironmentFingerprint digest semantics.)
