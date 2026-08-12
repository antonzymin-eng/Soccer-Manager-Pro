# Specification Error Log

**Purpose:** Records architectural errors, unnecessary complexity, and incorrect patterns
identified during specification review. Each entry documents the problem, the correct
approach, and every file requiring revision. Fixes are deferred — this log is the
authoritative remediation backlog.

**Created:** February 19, 2026, 5:00 PM PST
**Version:** 2.16
**Updated:** August 12, 2026, later still (v2.16 — **ERR-030-034's successor is PRE-DECIDED and gated as `league-bootstrap-design.md` KD-7a; the ERR stays OPEN.** NB2 marginal, `NegativeBinomialInverseCdf` pinned by name and recurrence, one uniform per side with sub-streams unchanged, and a `[GT] QuickSimDispersion` whose zero case routes to `PoissonInverseCdf` verbatim so α = 0 is bit-identical rather than identical-in-the-limit. **Writing it surfaced a correction that changed the deliverable: α is NOT determined by this corpus** — 0.0773 weighted vs 0.1552 unweighted, one 18-sample cell carrying 36% of the weighted fit — so no initial value is recorded and the fitter now emits the instability every run. Also pinned: NB2 closes only ~0.3 pp of the 7.6 pp draw gap (26.5% vs 26.8%, engine 19.2%), the draw deficit gets NO successor because the family that would explain it needs negative home/away correlation the corpus refutes (+0.044 ± 0.073), and adoption requires a post-defensive-wiring capture. The ERR's two findings — marginal over-dispersion and the draw deficit — are now stated separately, which the original single causal sentence had conflated.**)
**Updated (prior):** August 12, 2026, later same day (v2.15 — **ERR-030-033 RESOLVED: KD-8's per-bucket acceptance bar re-specified, and the A4a fit now reads mean-agreement PASS.** The flat ±0.25 could not be met by any model at the depth KD-8 itself sizes; it is replaced by a bar stated against the corpus's measured precision, a priori and for any corpus — per-cell `max(0.25, 2·se)` with **±0.25 retained as a floor**, bounded exceedances, a pooled `χ² ≤ χ²₀.₉₅(cells − 3)` where the statistical power actually lives, an 18/bucket scoreability floor so the se-relative form cannot be gamed by shrinking n, and an n ≥ 250 depth pin on the W/D/L bucket with an INCONCLUSIVE verdict when a miss is not distinguishable from noise. Measured: worst |z| = 2.06, one exceedance of an allowed two, pooled **χ² = 16.0 on 19 dof** vs 30.1. **The verdict is now two-part — mean agreement PASS, distribution shape FAIL** — because the halves fail for unrelated reasons and the flat verdict had been making ERR-030-034 read as a fit failure. Everything is computed by `tools/round-resolution-fit.py` (χ² criticals by Wilson–Hilferty, verified against exact values at dof 10 and 19), so no figure is hand-copied.**)
**Updated (prior):** August 12, 2026 (v2.14 — **ERR-030-033 and ERR-030-034 filed from the roadmap-A4a calibration run, the first execution of KD-8's corpus methodology end to end.** Both are recorded, NOT fixed, because both are owner decisions rather than repairs. **ERR-030-033:** KD-8's ±0.25 per-bucket acceptance bar is below the sampling error of the corpus KD-8 itself sizes — at ~18 matches/bucket the mean carries a standard error of 0.135–0.633 and **15 of 22 bucket-sides exceed the entire bar**, so a perfectly correct model re-scored against a re-run of the same corpus would fail it too; the tolerance and the sample size were set independently and never checked against each other, which is why AR-5 through AR-7 on that note all read the bar as a statement about the model. Resolving ±0.25 needs n ≈ 770/bucket ≈ 210 h against a budgeted ~9 h — a bar to re-specify, not a run to re-size. **ERR-030-034:** KD-7 resolves a fixture as two Poisson draws, whose variance equals their mean by definition, while the engine's scorelines are over-dispersed — mean var/mean **1.395** across 22 bucket-sides, 19 above 1, pooled chi2 521.7 on 374 dof, **z = +5.40** — so the engine makes more blowouts and shut-outs and **far fewer draws** (19.2% vs the fitted model's 26.8% at dSquad ≈ 0, the whole of the 7.6 pp W/D/L miss **⚠️ CORRECTED August 12, 2026 (Fable advisory review, independently reproduced): the causal sentence above is WRONG and would misdirect the fix.** Marginal over-dispersion fattens BOTH tails — more blowouts *and more 0–0s, which are draws* — so it barely moves the draw share. Computed at the fitted bucket-0 lambdas: independent negative-binomial at the measured dispersion gives **26.3%** draws against Poisson's 26.8%, i.e. it closes ~0.5 pp of the 7.6 pp gap. **Dispersion and the draw deficit are substantially INDEPENDENT findings.** The only mixed-Poisson mechanism that cuts draws materially is a shared antithetic swing, and it necessarily implies negative home/away correlation, which this corpus refutes: pooled within-bucket correlation is **+0.004 ± 0.052** (n=378), ~4σ from the ≈ −0.20 such a family predicts. So the draw deficit's mechanism is NOT established and is not expressible by any mixed-Poisson consistent with the measured correlation. Over-dispersion is separately confirmed real and NOT a pooling artifact (within-bucket `dSquad` spread contributes ≤ 0.005 of the ~0.4 excess), and is better specified as `var = μ(1+αμ)`, α ≈ 0.15–0.25, than as a constant 1.395 ratio.). A second-moment gap that no value of the three mean-shaping parameters closes: a statement about the model's FAMILY, filed against KD-7's shape rather than against the fit, and the surviving half of roadmap risk row 1. The three fitted `[GT]`s shipped with the FAIL verdict recorded at their own declaration, and `RoundResolutionFitLockTests` locks the ACHIEVED agreement rather than the unmet bar, so a later improvement tightens a real number instead of re-flying a claim.**)
**Updated (prior):** August 11, 2026 (v2.13 — **ERR-028-019 filed: docs-only close-out for #28's AR passes 5-8 (`39c385a`, `cf5abf0`, `8556ddd`, `b798ce2`), four consecutive production landings with zero `docs/specs/` edits, the ERR-028-017 class recurring twice more. Full entry above ERR-001. Same pass also reconciles this file's own duplicate `## ERR-008-021` heading (two independent write-ups from two concurrent branches, one superseded, annotated in place rather than deleted) and annotates a false renumbering-scope claim in `CHANGELOG-src.md` v2.113. No `src/` file touched; `recurring-defect-lint.py --repo .` reports 0 ERROR after these edits.**)
**Updated (prior):** August 11, 2026 (v2.12 — **Merge `origin/main` into this branch (docs-only, 4 conflicting files, no `src/` change): a VERSION HISTORY numbering collision spanning v2.00, v2.02, v2.03 and v2.04.** `origin/main` had independently used those four numbers for its own `ERR-010-002`/`ERR-010-003` review chain while this branch had already used the full `v2.00`–`v2.06` run for the #28 T1/T2a landing and its four-pass adversarial review loop. `origin/main` is the trunk and keeps `v2.00`/`v2.02`/`v2.03`/`v2.04` verbatim; this branch's seven rows renumbered `v2.00→2.05, v2.01→2.06, v2.02→2.07, v2.03→2.08, v2.04→2.09, v2.05→2.10, v2.06→2.11` — text otherwise verbatim, only the version cell and the one stale `(now v2.00)` self-citation (in what is now the v2.09 row) changed. No `ERR-` id collision: `origin/main`'s new commits did not touch the `ERR-028-*`/`ERR-030-*` series this branch filed into. See `CHANGELOG.md`, `CHANGELOG-src.md` and `file-manifest.md` for this same merge's other three conflicts.)
**Updated (prior):** August 10, 2026 (v2.11 — **ERR-028-018: the tracking close-out for commit `789ea74`, filed retroactively (FR-CS-057) — AR pass 5 (time/arithmetic axis), High: `SeedLifecycle` credited `LastAdvancedWorldDay` to the seed day (ERR-028-014) but left `GrowthCursor` at 0, so every full Growth/Decline band traversal accrued one attribute point short of Appendix A / KD-8's `+1/yr` promise, with a permanent residue eating the first year of the next band. Fixed by crediting the seed day's own band step; five locks rebaselined by +1 day of accrual, one new traversal lock added, mutation-verified (6/109). Also CORRECTS ERR-028-017 finding (k), filed earlier this session, which had read this exact seam and concluded the discrepancy was a harmless day-label difference — falsified by execution; that row now carries a pointer here. `section-3.md` v0.7 and `appendices.md` v0.5 patched spec+code. Five items carried forward recorded-not-fixed (M1–M4, L1 from `789ea74`; four more from `e68e2ad`'s invariant axis).**)
**Updated (prior):** August 10, 2026 (v2.10 — **ERR-028-017 + ERR-030-032: AR pass 5 over the #28 T1/T2a landing, a docs-only spec-vs-code sweep — 12 findings against `player-progression-lifecycle/` (F3/F5 exception-type self-contradiction, F8 understated by four refusing sites, the FR-PG-021 batch's undeclared type and validation contract, the byte layout's unstated string encoding and missing value gates, the retirement-evaluation placement, stale file layout / frame-version / public-surface text, the still-forward-design §9 preamble one revision after §9.2 was corrected, two Appendix A rows claiming code presence they lack, and the worked example's ambiguous entry point) and 3 against `season-competition-loop/` (FR-SN-021's stale seven-argument signature, §2.2's missing #28 type rows, §4's stale frame-version/file-layout/holdings text) — all found by re-reading the spec against `src/` after two consecutive AR passes (ERR-028-015, ERR-028-016) landed production refusals with no `docs/specs/` edit at all, which is why F8 itself was one of the findings. No code changed; `git status --short` after the pass touches only `docs/`.**)
**Updated (prior):** August 10, 2026 (v2.09 — **merge-audit tracking-hygiene corrections over the f85958f merge resolution.** **Merge-audit tracking-hygiene correction, same day (2026-08-10):** the merge had renumbered the branch's entries to v2.00–v2.02 but left them physically below `main`'s v1.97–v1.99 block, so the chain read v2.03, v1.99, v1.98, v1.97, v2.02, v2.01, v2.00, v1.96 top-to-bottom — monotonic by neither version nor date (v1.97 is dated Aug 8; v2.02 immediately below it is dated Aug 9). Reordered to strictly decreasing version order (v2.03, v2.02, v2.01, v2.00, v1.99, v1.98, v1.97, v1.96, …) with entries moved whole and only their `**Updated:**`/`**Updated (prior):**` labels adjusted so exactly the top entry carries `**Updated:**`; no entry's text altered. The header `**Version:**` field, stale at 1.99, is bumped to **2.03** to match this (still-top) entry. `file-manifest.md`'s #28 T1/T2a entry separately corrected two dangling citations of the pre-renumbering `v1.97` (then renumbered to `v2.00`; renumbered again to `v2.05` at the 2026-08-11 merge below, `origin/main` having independently used `v2.00` for unrelated content) and a `SeasonLoop.cs` version claim (`v1.7` → `v1.16`); see that file's own chain. *(Recorded as its own entry rather than appended to v2.03: this project has already had one record written by editing a PUBLISHED entry in place, which was restored, split out and rowed at v2.88 of `CHANGELOG.md`; the same correction is applied here before it shipped.)*)
**Updated (prior):** August 10, 2026 (v2.08 — **adversarial review passes 2 and 3 over the #28 T1/T2a landing.** Pass 2: **ERR-028-013** (H) — an EMPTY `ProgressionEngine` was treated as a wired roster authority, which made the pre-#28 save the save root deliberately WRITES impossible to resume, and made slot 1's `Neutral` branch provably unreachable; **ERR-028-014** (M) — the never-advanced sentinel retired from #28's legal store states, a **sibling-copy error**: the exemption is sound for #29/#41, whose fresh states carry no clock-anchored quantity, and false for #28, whose age derives from `BirthWorldDay`. Pass 3: **ERR-030-031** (H) — the ERR-028-014 sweep stopped at its own spec folder for the FIFTH recurrence of that class, leaving #30's F8 row and Appendix B asserting a sentinel exemption this branch had just deleted; **ERR-028-015** (H×2) — **both Highs were introduced by pass 2's own fixes**: ERR-028-014 silently DISARMED three locks (deleting the idempotency guard left all 469 tests green; deleting the retirement age comparison left 85/85 green), and ERR-028-013's relaxation reopened the ERR-028-010 provider gate. A **mutation sweep of 33 guards found 15 with no failing-if-reverted test**; all are now locked. Numbering note: this branch and `main` independently minted v1.97/98/99 — `main`'s keep those numbers, this branch's renumber to v2.00–v2.02 below.)
**Updated (prior):** August 9, 2026, **AR pass 1 remediation COMPLETE — the remaining High, all four Mediums and all eight Lows fixed** (v2.07). **ERR-028-010 (H):** a progression-wired `SeasonLoop` could not play a round through ANY public API — the constructor keeps its projected provider private and the `ISquadProvider` overload demands reference-equality with it, so the configuration the landing exists to enable could advance days and save and nothing else; the reviewer reached the working path only by reflection. A parameterless `AdvanceAndPlayNextRound()` resolves through the loop's own provider, which also removes the two-provider hazard by construction. **ERR-028-011 (M×4):** Encode wrote cross-club duplicate ids its own Restore refuses; `FromBlocks` accepted an id cursor behind its live ids; the PROG block was never checked against the three career blocks; and the missing-club constructor test was a tautology. **ERR-028-012 (L×8):** decoded attributes/age had no range gate though the block IS the roster now; `default(ClubCareerStates)` was misdiagnosed; frame docs, the stale (d) comment, a doubled roster copy, and the landing's own overstated records. **Two Lows were resolved as DECISIONS, not repairs** — the proposed per-day squad cache is a silent behaviour change (slot 1 mutates the store, so slots 2 and 4 would price off a one-day-stale roster), and two tests had their CLAIMS corrected rather than their code, because neither can carry the weight it implied. Prior entry below.)
**Updated (prior):** August 8, 2026, **adversarial review pass 1 over the #28 T1/T2a landing — 4 High, 7 Medium, 8 Low, every High and most Mediums demonstrated by executing probes against the built assemblies** (v2.06). **The landing was BROKEN and had already been pushed.** **ERR-028-006 (H):** a new world starts on day 0, so the `uint` birth anchor underflowed for every player with a non-zero age and was clamped to 0 — the ENTIRE LEAGUE became age 0 on the first daily step (`26,22,30,…` → `0,0,0,…`; bands `growth=100 stable=0 decline=0`), retirement could never fire, and `Age = 0` fed `LineupSelector` and the match engine through the KD-4 projection. **Both #28 fixtures used `BaseDay = 100000` with a comment explaining that it avoids the underflow — written around the defect, avoiding the only day the product starts on.** Fixed by making the anchor a signed `long` (codec `u32 → i64`, free at format v1); the landing's own headline lock passed *because* of this defect and was rewritten against bootstrap ages. **ERR-028-007 (H):** the FOURTH persisted per-player cursor was checked at none of the three boundaries the #29/#41 loop spent passes 5/6/9 establishing — a cursor 9,999 days ahead was accepted at composition, Save and Load, silently freezing growth. **ERR-028-008 (H):** `Save`'s `?? ProgressionEngine.Empty` let a resume that dropped the store write a zero-club roster over a populated file (`clubs=4 → clubs=0`, every gate green); the reviewer's proposed fix was NARROWED after testing showed it broke four legitimate pre-#28 suites — the guard now reads the destination instead. **ERR-028-009 (M):** the sentinel was a legal `worldDay`, breaking idempotency and hanging the gap-replay loop forever. Remaining findings tracked in the landing entry. Prior entry below.)
**Updated (prior):** August 8, 2026, #28 T1/T2a landing (v2.05 — **roadmap D1 part one: ERR-029-006 FULLY RESOLVED.** #28 exposes the FR-PG-021 batch `AdvanceDay` and #30's KD-2 **slot 1 is LIVE**, gathered through the new `PlayerCareerStates.GatherTrainingInputs`; mutation-verified (reverting slot 1 fails two locks). Four ERRs filed AND resolved in the same commit: **ERR-028-003** (new-game `PotentialAbility` had no derivation anywhere — owner's call is that it is #47 authored data, seeded here by a deterministic `[GT]` placeholder so the landing has NO draw site; recorded-not-fixed that a whole career moves CA by only ~421 of 10,000, so the PA ceiling is decorative whatever its source), **ERR-028-004** (§3.5 specified the save block version-first with the RNG domain tag as its identifier — the ERR-029-005/ERR-041-009 MUST, third spec; now magic-led plus a typed `ProgressionBlock` at the frame), **ERR-028-005** (§5.2's keystone T-PG-DET-002 was unsatisfiable as worded, and §3.1 had no per-day cursor while #30 runs a fixture day's slots twice — growth would have double-accrued on every fixture day, silently), and **ERR-030-030** (five stale #28-null-seam sites + the v4 frame description). `SEASON_SAVE_FORMAT_VERSION` **4 -> 5**; no draw site, no RNG stream, no `DETERMINISM_DIGEST_VERSION` or `SNAPSHOT_SCHEMA_VERSION` bump. **This retires roadmap A3's seed-rebuildable-roster property** — #28 KD-4 makes the block the serialized roster. Prior entry below.)
**Updated (prior):** August 9, 2026, later still (v2.04 — **Whole-tree gate result for the AR-over-ERR-010-002
landing (commits `48977fa` doc half + `d93e0c8` code half).** `d93e0c8` landed the two behavioural
fixes from the same adversarial review that v2.03 documented the doc-and-comment half of. **(1)** The
out-of-range branch in `HeadingAim`'s ballistic solve returned a flat 45° as "the maximum-range
launch." That is the max-range angle only when the target sits at contact height — a header contacts
near 2.3 m and its targets sit on the ground, so `dz` is negative on essentially every real header, and
§3.5.1 itself calls this branch "the ordinary case for a defensive clearance," making the wrong angle
the production path rather than an edge case. Measured: 9.98° of error across 4 cm of target distance
at the boundary, 4.38° at the production nominal speed (7.0 + 4.0 + 5.0 = 16.0 m/s × `PowerIntent` 0.7
= 11.2 m/s). Fixed to the true max-range angle, `tan(θ) = v / √(v² − 2·g·dz)`, with a guard for a
target above what the speed can reach at any angle. `MaxRangeLaunchComponent` — a `[DERIVED]` constant
whose name asserted what it was not — is retired with it. **(2)** `ComputeAimNormal` did not propagate
a degenerate desired direction: `half = incident + 0` has magnitude 1, so the zero guard missed and the
method returned the incident itself, and the blend then steered toward it — at full authority
reflecting the ball straight back the way it came at full power, the **maximum** possible deflection,
arrived at through the branch documented as producing the natural rebound. This made
`ComputeAchievedNormal`'s zero-aim fallback unreachable through the composition, so the lock on it had
been passing against a branch production could never enter — this project's guard-on-an-unreachable-
branch class, one method away from the omission the original landing was proud of. Also landed: four
ERR-008-002 home/away locks for `GkHeadingIntentSource.HeaderAimTarget`, the landing's only
team-branching geometry, which had none — a `HeadingMechanics.Tests` case labelled as the ERR-008-002
lock ran team-**agnostic** code at `TeamId = 0` on both sides and could not have failed for an
asymmetry; the mirror itself is correct, so this was a coverage/false-claim defect, not a live bug.
**One bug was introduced by the fix and caught by its own new lock before landing**: the
unreachable-height guard first returned `Vector3.up`, which is Unity's +Y, while this project's up axis
is +Z (Ball Physics #1 §1.2) — the coordinate-axis trap in `CLAUDE.md`'s own hazard table, and it still
nearly shipped.

**Whole-tree gate, local run, head `d93e0c8`:** build 0 errors, 3 warnings. `GATE_EXIT=1` — the gate
did **NOT** print "Gate PASSED"; this is **not** GATE-VERIFIED. Sole failure:
`sim_match_engine_close_chance`, 2 of 3 predicates — `final-third-dribbles-are-not-goal-averse`
meanCosine **−0.165** (bound −0.16) and `goalward-dribbles-are-not-a-minority-of-one-in-three`
goalwardShare **0.407** (bound 0.42). This is the **inherited C1 failure** that predates this branch
and awaits an owner call; it is identical to three decimals against the pre-fix baseline recorded at
`589a011`, so this landing moved nothing. `MatchEngine.Tests` **451 passed / 1 failed / 10 skipped /
462 total**, up from the 447/1/10/458 baseline — the +4 are exactly the four new `HeaderAimTarget`
locks. `HeadingMechanics.Tests` **63 passed / 15 skipped / 0 failed** (60 → 63). All 31 other suites
green, quarantine empty. `python3 tools/recurring-defect-lint.py --repo .`: **0 ERRORs**. Prior entry
below.)
**Updated (prior):** August 9, 2026, still later same day (v2.03 — **Adversarial review of the ERR-010-002
landing: two of its five findings confirmed and fixed here (the other three touch files under
concurrent edit and are out of scope for this pass).** **Finding 1 (High, fixed in
`docs/specs/heading-mechanics/section-3.md` v0.5, not this file):** §3.5.1 Step 2's "bounded to the
hemisphere the ball can physically reach" was stale spec text — `HeadingAim.ComputeAimNormal` never
implemented that bound and its own XML doc proves it provably cannot fire (`dot(incident + aimDir,
incident) = 1 + dot(aimDir, incident) ≥ 0`, always ≥ 0). **Finding 2 (Medium, this entry):** the same
stale claim survives in THIS file at two sites — the `ERR-010-002` "Updated (prior)" summary below and
the Error Index table row — both annotated in place below rather than rewritten, per this file's own
convention (see the v1.99/v1.98 pair for the precedent). **Verified NOT present** in `CHANGELOG.md`,
`CHANGELOG-src.md`, or `file-manifest.md` — all three already carry the correct "no bound is applied,
provably unreachable" phrasing; Finding 2 was wrong to name them and no change was made there. **Finding
3 (Medium, `docs/tracking/gk-heading-engine-integration-design.md` §4.2a, not this file):** `§4.2a`,
cited by `GkHeadingIntentSource.cs:325` and `MatchEngine.cs:3848` and by this file's own `ERR-010-002`
entry below, was a phantom citation — no document defined it. **Correcting the finding's own file
target**: the citing code's header comment names `gk-heading-engine-integration-design.md` §4 as
`GkHeadingIntentSource`'s governing document, not `match-engine-design.md` (verified — that file's own
§4 is an unrelated "Boot sequence" section, and it never mentions `GkHeadingIntentSource` or GK/Heading
at all); §4.2a is now documented there, beside the §4.2 it extends. Findings 4 and 5 are addressed in
`src/heading-mechanics/HeadingMechanics.cs`, `src/match-engine/GkHeadingIntentSource.cs`, and
`src/match-engine/MatchEngine.cs` directly (version rows / comments; no logic change). Prior entry
below.)
**Updated (prior):** August 9, 2026, later same day (v2.02 — **ERR-010-003 filed: `#10`'s KD-18 aerial-phase
gate borrows AM #2's `GROUNDED` state, which #2 §3.1.2 defines as "knocked down", not "on the
ground."** Surfaced as a "recorded, not fixed" bullet at the tail of the `ERR-010-002` entry below
and filed here as its own candidate, per that entry's own note. Verified against source: #10 §3.2/§3.3
(KD-18) and the mirrored `HeadingEligibility.cs`/`HeadingMechanics.cs` comments describe the
`{GROUNDED, STUMBLING}` exclusion as establishing that the agent "has left the ground" / is in an
"aerial phase," but AM #2 §3.1.2 defines `GROUNDED` as one specific incapacitated substate entered only
via collision knockdown or an extreme stumble-fail — never entered by a merely standing, walking,
jogging, sprinting, or decelerating player — and AM #2 publishes no Z-axis/airborne state at Stage 0 at
all (KD-18's own premise), so no check reading `AgentMovementState` can establish "has left the
ground" in the first place. **Verified NOT a no-op and NOT inverted**: the exclusion is real and
reachable — it correctly blocks a header attempt while the agent is prone or stumbling from a
collision, which is exercised by ordinary gameplay (`HeadingMechanics.cs:192-206`,
`HeadingEligibility.cs:54-65`). The defect is entirely in the label: the tree's actual notion of
"aerial phase" is synthesized independently by `HeadingJumpKinematics`'s `jumpStartFrame` →
`landingFrame` elapsed-frame timer, never validated against any position, velocity, or state signal.
**Documentation only, Low severity — same class as `ERR-020-003`** (two files using one word for two
different things, each internally self-consistent); no code change proposed. Prior entry below.)
**Updated (prior):** August 9, 2026 (v2.00 — **ERR-010-002 filed + RESOLVED: the header aim had no owner, and every header was a passive mirror.** #10 §3.5 delegated the aim to Decision Tree #8, which cannot emit a header at all (`ActionType` ordinal 8 overflows the 3-bit composure-noise field — wiring backlog W9), so `TargetIntent` reached no formula and the outgoing direction was pure specular reflection about `normalize(ballPosition − headCentre)`. Correcting `close-chance-creation-design.md` §10.6 item 3, which recorded the symptom ("a fixed aim point") and mis-stated its consequence: a defender clearing in his own box did not aim 90 m at the far goal — he headed the ball back the way it came. Two further defects in the same chain: the contact point had **two independent derivations** across `HeadingMechanics.Update`'s two passes, and Pass 2 rebuilt the world point from its **2-D** head-local projection, pinning `contactPointActual.z` to the head centre, so the reflection normal was permanently horizontal and `reflected.z = v̂_in.z` — **a descending ball was headed further down** and no header could lift the ball. Resolved by new #10 §3.5.1 + `HeadingAim.cs` (ballistic launch solve, half-vector normal bounded to the reachable hemisphere `[CORRECTED at v2.03 above: no such bound exists or is needed — the half-vector is always in the forward hemisphere by construction; see the v2.03 entry and #10 §3.5.1 v0.5]`, attribute-blended achieved normal — authority 0 ≡ pre-fix, FULL-RANGE ramp) plus the producer half `GkHeadingIntentSource.HeaderAimTarget` (clear wide when deep, aim at goal when advanced, continuous). The `ERR-011-010` shape. No new `[GT]` (inside KD-W1), no schema bump, no RNG/draw-order change. **GATE-VERIFIED** — `HeadingMechanics.Tests` 60/15/0 (+13 new locks), `MatchEngine.Tests` 447/1/10 byte-identical to the pre-fix baseline, its one failure the inherited C1 close-chance band. The landing's "digests DO move" claim is **withdrawn as stated**: nothing moved, because at a 0.2% contact ratio no acceptance scenario contains an executed header. Prior entry below.)
**Updated (prior):** August 9, 2026 (v1.99 — **CORRECTION to v1.98: `ERR-008-024` was recorded RESOLVED. It is not.** The v1.98 entry below overstated the outcome, filed earlier this same session. The tie-break fix — ranking §3.1.5.2's 8 sectors on `spaceInSector × DirectionQuality_DRIBBLE(sectorDir, toGoal)` instead of `spaceInSector` alone — was **implemented, measured, and REFUSED**, the KD-CC7 pattern (`close-chance-creation-design.md` §4, where the #15 run overlay met the same fate). It DOES fix the symptom: `sim_match_engine_close_chance` goes meanCosine −0.165 / goalwardShare 0.407 (both failing) to **PASS** (bounds −0.16 / 0.42, neither moved). But the same build **stalls play outright**: `sim_match_engine_play_develops` fails with "play stalled: last possession change at tick 18424, ball last moving at tick 18465 of 32400", and `sim_match_engine_shot_outcomes` fails `goals-still-scored` at **0**. A wider form ranking on `space × DirectionQuality` outright (not as a tie-break) produced the **identical** stall at the **identical tick**, plus mean-shot-distance 25.41 m against a 24.00 m ceiling — that identity is what localises the cause to the tie-break itself, not to how much space either form trades away. **Refused, not landed.** `OptionGenerator.cs` is now byte-identical in logic to the pre-fix baseline (verified: `git diff 23f8dd9 -- src/decision-tree/OptionGenerator.cs` has zero non-comment lines). What was KEPT is behaviour-neutral only: `DirectionQuality_DRIBBLE` hoisted to public static `UtilityWeights.DribbleDirectionQuality(Vector2, Vector2)` with `UtilityScorer` delegating to it (so generation and scoring cannot drift apart if this is retried), plus a long explanatory note at the defect site recording the refusal for the next attempt. The two §3.1.5.2 unit locks the v1.98 landing added are **REMOVED** — they locked behaviour that no longer exists. `DecisionTree.Tests` is back to **129 passed / 4 skipped / 0 failed**. ERR-008-024's status below changes from Resolved to **recorded, NOT fixed — implemented, measured, refused**; `section-3-1.md` reverted to v1.8 to describe the code that actually ships; `close-chance-creation-design.md` §7 item 6 **REOPENED** at v1.4. Prior (overstated) entry below, left unedited per this file's convention.)
**Updated (prior):** August 9, 2026 (v1.98 — **ERR-008-024 filed + RESOLVED: §3.1.5.2's 8-sector dribble scan always picked `AgentFacingDirection`, whatever the goal.** `spaceInSector` saturates at exactly 1.0 for any sector clear of `DRIBBLE_THREAT_RADIUS`, and the old scan ranked on `spaceInSector` alone with a strict `>` improvement test — so whenever two or more sectors were clear (the common case in the final third) the winner was always sector 0, `AgentFacingDirection` by construction, and goal direction never entered the choice at all. This is why ERR-008-018's `DirectionQuality_DRIBBLE` scoring term could suppress a retreating dribble but never redirect it (`close-chance-creation-design.md` KD-CC3 / §7 item 6, now closed). Fixed by ranking sectors on `spaceInSector × DirectionQuality_DRIBBLE(sectorDir, toGoal)` — the SAME term §3.2.4.1 already applies when scoring the resulting option, hoisted to a new public static `UtilityWeights.DribbleDirectionQuality(Vector2, Vector2)` so both stages share one formula instead of a hand-copied second walk; `UtilityScorer.ComputeDribbleDirectionQuality` now delegates to it, behaviour there unchanged. **No new constant** — the floor is ERR-008-018's `DRIBBLE_GOAL_DIR_MIN_MODIFIER` = 0.80, untouched, so `DirectionQuality_DRIBBLE ∈ [0.80, 1.0]` and direction can outrank at most a 20% space deficit (KD-CC6 preserved; a genuinely blocked sector still loses on space). Measured: `sim_match_engine_close_chance` acceptance scenario — meanCosine −0.165 → **PASS** (bound −0.16, unmoved), goalwardShare 0.407 → **PASS** (bound 0.42, unmoved). `DecisionTree.Tests` **131 passed / 4 skipped / 0 failed**, incl. 2 new §3.1.5.2 locks. `OptionGenerator.cs` v1.11, `UtilityScorer.cs` v1.16, `UtilityWeights.cs` v1.14, `OptionGeneratorTests.cs` v1.11, `section-3-1.md` v1.7, `close-chance-creation-design.md` v1.3. **⚠️ CORRECTED at v1.99 above — this entry overstated the outcome. The fix described here was implemented, measured, and REFUSED, not landed; see v1.99 for the refusal evidence.** Prior entry below.)
**Updated (prior):** August 8, 2026, later same day (v1.97 — **ERR-012-011 filed + RESOLVED at wiring-backlog C1: the #12 `InPoss` gate.** #12 §3.0 classified phase from the on-ball carrier, absent for the whole flight of every pass, so a passing team read as being in transition — measured `InPoss` on **7.5%** of final-third samples against `TransToAtk` 58.9%. Phase now classifies from TEAM possession, composed by the orchestrator from the carrier's team ∪ the intended receiver of a pass in flight; the latch expires with no new `[GT]` by reusing `RunFirstTouch`'s receding predicate. Snapshot fields ADDED not redefined (#23's FR-DM-007 exclusion untouched); **`SNAPSHOT_SCHEMA_VERSION` 19 → 20**; no RNG/draw-order change. Two clears recorded as having no isolating lock. Prior entry below.)
**Updated (prior):** August 8, 2026, convergence entry (v1.96 — **BALANCE-PASS ADVERSARIAL REVIEW LOOP: CONVERGED. Pass 16 returned "no new High or Medium findings" — the termination bar — over the pass-15 delta and an open-scope final look.** Pass 16's verification: the draw branch is atomic and the WHOLE method is now all-or-nothing (every call traced for writes and throw sites — all guards pre-write); §3.1 matches the code line for line; the M2 binding condition exact (`RecoverySpeedMillMult < 250`); every pass-15 row claim accurate; XML balance clean; the lint and the `InjuriesMedical` suite re-executed rather than trusted. Its three Lows, all mechanical, FIXED at the convergence commit: **L1** — pass 15's seven file sites + four tracking headers dated Aug 9 (tomorrow), breaking the lint baseline 275 → 282 one commit after the tool was handed to the owner as a ratchet input (corrected; baseline re-confirmed 275, surface clean); **L2** — pass 15 M2 made the assignment CEILING normative with nothing locking it — a mutant replacing `RecoveryMax` with `int.MaxValue` left all 67 tests green; the slow-physio assert (60×1000/200 = 300 → 240) lands beside the floor lock and T-MD-MOD-002 states both arms (`MedicalStepTests` v1.8, #41 §5 v0.6); **L3** — §3.3's own summary still said "floored at 1" after M2 swept the rule's other two statements (#41 §3 v0.16). **The loop's final ledger: 16 passes** (counts by pass: 3M/1M/1H+4M/6M/7M/4M/2M/2M/2M/1M/2M/4M/4M/1M/2M/0M), **13 consecutive whole-tree gate PASSES** (plus one run invalidated by this session's own process error, recorded in v1.90), **8 ERR ids filed and resolved** (ERR-030-027/-028/-029, ERR-041-011/-012/-019, ERR-029-007/-008, ERR-027-004 — nine including the owning-spec back-prop), the FR-MD-027 occurrence dial ARMED and measured in the football band (league 717–816 injuries/season, starters 2.08, reserves 1.12–1.13, unavailability ~9.5%), and `tools/recurring-defect-lint.py` (owner-directed) mechanizing four of the loop's seven recurring defect classes with the tree-wide 275-error backlog filed in `open-issues.md` for the owner. Convergence verified after the Low fixes: build 0 errors, `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67 (the ceiling assert inside T-MD-MOD-002), `TrainingSystem.Tests` 52/52; **the pass-15 gate PASSED — the THIRTEENTH consecutive verdict**; Final gate over the convergence commit: **PASSED** (the fourteenth consecutive whole-tree verdict, closing the chain) — quarantine empty; `MatchEngine.Tests` 436/0/10 (27 m 46 s), `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67 (the ceiling assert live in T-MD-MOD-002), `TrainingSystem.Tests` 52/52 over the convergence commit. Prior entry below.)
**Updated (prior):** August 8, 2026, second final entry of the day (v1.95 — **Balance-pass adversarial review pass 15: 0 High, 2 Medium, 2 Low, all fixed — both Mediums INSIDE the pass-14 fix, the audit chain working on itself.** **M1:** the pass-14 guard fired AFTER `state.Severity` was written — the draw branch was `AdvanceMedicalDay`'s ONE partial-write throw site (every other refusal precedes all writes, the F7 refused-advance-mutates-nothing standard), so under `RecoveryMax = 0` the refusal ITSELF left `RecoveryRemaining == 0` beside a fresh severity in the live career, the exact breach being refused, surfacing a day later as a state-blaming fault — and fixing the config did not recover the session (demonstrated by model, both arms). The branch is now ATOMIC — `AssignRecoveryDays` runs before the three writes — and the three prevention claims are corrected: prevention is the ORDERING's property; the guard alone only made the breach loud (`MedicalStep` v1.12, #41 §3 v0.15, appendices v0.13). No lock is possible (unreachable under the fallback — the accepted class); the reorder is justified by the atomicity contract. **M2:** §3.1's normative assignment had NO `RECOVERY_MAX` ceiling while the code has always clamped to it — FR-MD-014 put the `[0, RECOVERY_MAX]` clamp on the COUNTDOWN and gave the assignment as a bare floored division, so an implementer following the spec wrote `241+` for a below-average physio on the Serious tier (binding whenever `RecoverySpeedMillMult < 250`), refused by `ValidateState` the next day and persisted happily by the codec. The ceiling existed only in the code and the two paragraphs pass 14 wrote. §3.1's step now carries `Clamp(…, 1, RECOVERY_MAX)` + the guard line; FR-MD-014's assignment clause gains the ceiling (#41 §2 v0.10). Spec-only; the code was right. **Lows:** the v1.3 asymmetry `<para>` was nested instead of a sibling (`MedicalSaveCodec` v1.4); `src-tree.md`'s two ERR-029-008-stale annotations (the sweep's next boundary out — the file's own header disclaims authority, but the pattern is the grep-boundary class's fourth tracking-doc recurrence). **Pass 15's verification half:** pass 14's dead-branch claim CONFIRMED by exhaustive enumeration (0 reachable states over the full `RecoveryMax × Severity × RecoveryRemaining` lattice); `AppearanceWindow` modelled against a naive reference over all windows × 3,000 random sets × 70 read days — 0 mismatches; the tautology, unexecuted-branch, parallel-surface and partial-write sweeps over the never-fully-read files all CLEAN (the draw branch was the sole partial-write exception, now closed); lint surface clean, 18 suppressions read, none hiding a defect. Verified after the fixes: build 0 errors; `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52. **The pass-14 gate PASSED — the TWELFTH consecutive whole-tree verdict.** Pass-15 gate + AR pass 16 next; the reviewable delta is now two files. Prior entry below.)
**Updated (prior):** August 8, 2026, past midnight into the 9th (v1.94 — **Balance-pass adversarial review pass 14: 0 High, 1 Medium, 4 Low, all fixed — the lint-armed pass; the Medium is the sharpest catch of the loop.** **M1:** pass 13's `RecoveryMax < 1` guard sat on a branch it can PROVABLY NEVER REACH — `ValidateState` runs first and refuses any injured state with `RecoveryRemaining > RecoveryMax`, and its F1 iff-rule forces `RecoveryRemaining ≥ 1` while injured, so the predicate is unsatisfiable on the countdown branch under ANY config; the breach it names happens on the MUTUALLY EXCLUSIVE draw branch (`AssignRecoveryDays`' clamp, gated on healthy-at-entry). Demonstrated by model: a healthy player drawn injured under `RecoveryMax = 0` gets `RecoveryRemaining == 0` written beside a severity and is refused a day later as a state fault. A guard on a mutually-exclusive branch ships green precisely BECAUSE it is unreachable — the pass-13 verification gap, and the reason the loop's rule is now: a guard's placement claim needs the same scrutiny as a lock's kill claim. Moved to `AssignRecoveryDays` (the one site whose clamp can write the breach); §3.1 reverts to rate-only with the move recorded; §3.3 and the Appendix A row carry the corrected site; the falsified v0.13/v0.11 claims annotated in place (`MedicalStep` v1.11, #41 §3 v0.14, appendices v0.12). The pass-13 record's mechanism was also off: `ClampLong` hits the `value > max` arm, not a min-over-max rule. **Lows:** **L1** — the medical codec's `[GT]`-non-gate rationale inherited #29's clamp claim, but #41's day step REFUSES an out-of-band counter (F1), so a lowered `RECOVERY_MAX` loads cleanly and halts the career loudly at slot 4 until restored; the asymmetry stated at the codec (v1.3) and §4.4 (v0.6). **L2** — ERR-029-008's "all three restated" was three of SEVEN: four bare `SetFocus` sites survived, two INSIDE the section the ERR rewrote (the grep-boundary class with no file boundary to blame); completed (#29 §2 v0.9, §3 v0.6, §5 v0.5, the v0.8 row annotated). **L3** — the last unsanctioned draw-key spelling lives in #27 §2's v0.3 ROW — the one line class the lint deliberately skips, and rows are where this project's spelling record lives; annotated in place, the tool-scope decision recorded in `.suppressions`. **L4** — pass 13 M3's own rewrite left `SEASON_SAVE_FORMAT_VERSION 1 → 2` five lines above the D2 files it added — the contradiction the fix was closing, re-introduced one block apart (#30 §4 v0.5). **The reviewer's negative results carry weight now:** the tautology/mutant sweep over the newest locks found them sound; the unexecuted-branch sweep found every branch driven; the parallel-surface sweep found every prior collapse holding; the lint confirms the surface CLEAN and its 27 suppressions hide nothing. Verified after the fixes: build 0 errors; `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52; lint surface clean. Pass-14 gate + AR pass 15 next. Prior entry below.)
**Updated (prior):** August 8, 2026, final entry of the day (v1.93 — **Balance-pass adversarial review pass 13: 0 High, 4 Medium, 6 Low, all fixed — the convergence test FAILED the useful way: three Mediums are pass-12 fixes incomplete at their own class boundary, the fourth an axis untouched in twelve passes.** **M1:** pass 12's "the one `[GT]` whose lock had no runtime mirror" was FALSE twice over — `RecoveryMax` had no mirror (below 1, `AssignRecoveryDays`' clamp has min > max and `ClampLong` returns max: `RecoveryRemaining == 0` while injured, the F1 breach the floor's own doc names, written into the live career and blamed a day later as data corruption), and `InjuryRiskMax`'s draw-site guard was ONE-SIDED (non-positive: every score clamps to 0 and the ARMED dial injures nobody, forever, silently — pass 12's own described failure shape). Countdown guard widened to `rate ≤ 0 || RecoveryMax < 1`; `DrawOccurrence` refuses both sides; the falsified completeness claims ANNOTATED in #41 §3 v0.11's and appendices v0.10's rows rather than silently rewritten (`MedicalStep` v1.10, #41 §3 v0.13, appendices v0.11). **M2 (ERR-029-008, row below):** #29 still specified the pre-T0-AR `TrainingSchedule` — FR-TR-003's "read-only view" and FR-TR-023's free `SetFocus(club, playerId, focus)`, the exact two-array shape the T0 High DELETED — twelve passes and three months after the code fix whose whole point was structural safety (§2 v0.8, §4 v0.6, spec-only). **M3:** #30 §4 — the architecture file `src/CLAUDE.md` orders implementers to read before coding — was untouched through T1/T2/D2 and all twelve passes, holding the THIRD copy of the Save/Encode signature (pass 11 corrected the second): four arguments, the 1→2 bump and a five-field frame against today's seven-argument Save, version 4, eight-field frame. The copy is DELETED in favour of a pointer to Appendix B — a third copy is not re-synchronised, the parallel-surface rule applied to spec text; §4.3 gains the career pair + `AdvanceDays`, §4.2 the eight T1/T2/D2 files (v0.4). **M4:** #30's own Appendix A said `SEASON_SAVE_FORMAT_VERSION = 2` — the identical wrong value pass 5 M6 fixed in the manifest, left in the OWNING catalogue, contradicting Appendix B in the same file — and had no rows for the three appearance constants, `APPEARANCE_BITMASK_MAX_WINDOW_DAYS` load-bearing (the `AppearanceWindow` runtime guard reads it; #41's lock hard-codes its value) and in NO spec anywhere: ERR-030-028's class on a constant, one appendix over from where that ERR landed (v0.8). **Lows:** the 4-tuple key spelling sanctioned as a third expansion (three live sites used it and the pass-12 rule as written made them defects); the stale two-block counts in `PlayerCareerStates` v1.14 / `SeasonSaveManager` v1.17 (+ "every save written before T2" — frame v4 REFUSES a pre-T2 file; the live case is a careerless save); `TrainingBlock`/`MedicalBlock`'s pass-12 header addition had itself shipped ROWLESS (v1.1 each — the FR-CS-057 class inside its own fix, sixth recurrence); #30's outline swept like its siblings were at pass 10 (v0.3 — "nothing is built yet" of a spec implemented since T0, F1–F6 after F9); **T-SN-DET-004** names the two existing depleted-squad locks (#30 §5 v0.4 — ERR-030-029's back-prop had reached #36's test plan and not #30's own); `AdvanceMedicalDay`'s exception doc named the retired denominator guard and omitted two live ones (folded into `MedicalStep` v1.10). Verified after the fixes: build 0 errors; `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52. **The pass-12 gate PASSED — the TENTH consecutive whole-tree verdict** (quarantine empty, 436/0/10). In parallel, `tools/recurring-defect-lint.py` is being built (owner-directed) to mechanize the checkable classes — the header/row hygiene, version coherence, phantom-stream, key-spelling, stale-claim and pending-marker sweeps — so pass 14 consumes a machine's residue rather than rediscovering by hand. Pass-13 gate + AR pass 14 next. Prior entry below.)
**Updated (prior):** August 8, 2026, last entry of the day (v1.92 — **Balance-pass adversarial review pass 12: 0 High, 4 Medium, 5 Low, all fixed — the pass ran with the GENERATOR-CLASS question as an explicit brief (is each prior fix the complete class or one instance?), and every Medium is a class-mate of a pass-11 fix.** **M1:** FR-SN-034 still MANDATED #29/#41 as null seams one row below the FR-SN-013 pass 11 corrected, and #30 §3.3's prose still read "(steps 1–7)" (pre-ERR-030-022 numbering) and "with only the world-day tick live" (false since T2) — the pass-3 slot-list correction had stopped above the prose; amended to landed-live slots 2/4, byte-identity qualified to the WORLD blob (§2 v1.1, §3 v1.7; §3's v0.8 row's "extended to steps 1–8" claim recorded as inaccurate rather than silently rewritten). **M2:** #30 §2.2 declared a `SeasonLoop` with no career pair and knew nothing of `PlayerCareerStates`/`AppearanceState`/`ClubAppearanceStates` three landings after all became load-bearing — the gap that let the APPR layout ship unspecified (ERR-030-028) and forced pass 11's F7/F8 to cite undeclared members; bullets added, FR-SN-032/F5 gain `AdvanceDays` and the roll's `RequireEveryFixturePlayed` half (the L5 finding, folded in). **M3:** `RecoveryDaysPerTickBase` was the one `[GT]` in the landing whose design-time lock had NO runtime mirror — non-positive, the countdown never falls and EVERY injury is permanent, silently, the armed dial progressively injuring the whole league with the only symptom the back-fill quietly fielding whole squads; strictly worse than the deleted tier the pass-10 guard stops. Guard at the countdown site — the DrawOccurrence posture's FOURTH instance (`MedicalStep` v1.9, §3.1 v0.11, Appendix A v0.10, lock v1.10). **M4 (ERR-030-029, row below):** the depleted-squad back-fill rule existed in NO spec while `SelectAvailable` had implemented it since T2 and `SquadRating.CanFieldStartingEleven` existed solely to serve it — and **#36 §2 F7 explicitly refused to invent a policy while §5 T-NT-I-005 asserted "whatever ERR-030-016 settles on"**: an APPROVED spec waiting on a decision the code had made unilaterally, the reasoning living only in a supplement belonging to the WRONG spec. #30 §3.4 owns the rule (v1.8), its terminal refusal is F9, #36 §2/§5 point at it as settled (v0.3/v0.2) — ERR-030-028's class on a behavioural rule. **Lows:** `SaveBlobFramingHelpers` still said "the two current callers" (three since D2 — v1.2 + the manifest row); the ERR-041-012 sweep's FIFTH widening reached `src/` (`TrainingStep`'s risk doc still put the draw "on #41's stream" — v1.4); the draw key's canonical spelling + two sanctioned abbreviations pinned at #41 §3.1.1 (v0.12) with `PlayerRecord` v1.3 / `PlayerCareerStatesTests` v1.5 aligned — three drifted spellings had survived pass 11's local fix; `TrainingBlock`/`MedicalBlock` were the only two files in scope missing their `Modified:` header (added); `AdvanceDays`+roll refusal folded into M2. Verified after the fixes: build 0 errors; `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52. **The pass-11 gate PASSED — the NINTH consecutive whole-tree verdict** (quarantine empty, 436/0/10). Pass-12 gate + AR pass 13 launch next; the class-completion sweep is the convergence test. Prior entry below.)
**Updated (prior):** August 8, 2026, even later same day (v1.91 — **Balance-pass adversarial review pass 11: 0 High, 2 Medium, 4 Low, all fixed — both Mediums stale-spec debt on #30 §2, the section an implementer reads first.** **M1:** FR-SN-013 still declared the availability seam "MAY be filtered … **empty until #44 T2**" — three false statements in one MUST while the seam has been LIVE, unconditional, both-clubs-both-paths since #29/#41 T2; §3.4 was corrected at AR pass 5 (its v1.4 row records retiring exactly this sentence) and the sweep stopped one section short — the pass-8-M2 grep-boundary shape in the requirements table. Rewritten to match §3.4 (MUST; occupied by #41's FR-MD-023 view; #44/#36 join at their T-phases; ERR-030-016's removal-composition property). FR-SN-021's signature refreshed with it — three landings and three parameters stale while Appendix B moved (§2 v0.9 → **v1.0**). **M2:** the composition-pairing refusals and the cross-blob cursor-vs-clock rule — enforced at THREE boundaries in TWO directions over THREE cursor kinds since the T2/pass-5/pass-6 landings — had one appendix sentence (one kind, one direction, one boundary) as their entire normative source, and #30 §2.3's F-table had not moved since v0.1 through five landings that changed the composition root: the pass-9-L4 class ("a production fail-loud with no spec row"), six refusals wide. New **F7** (career mispaired at composition) and **F8** (cursor outside the coherent band — Save, Load AND composition, one shared predicate set) rows; Appendix B states the rule in full, superseding the single anchor sentence (v0.7). **Lows:** **L1** — pass 10's own renumber had reused v0.4 in #41 §9's header while the table's 0.4 described pass 9 (the duplicate-label defect relocated from the table into the chain — now v0.5 with its own row), and #29 §2's reorder had shipped rowless (v0.7). **L2** — `SaveBlobFramingHelpers.CanonicalOrder`'s doc still said "the FR- id" while pass 10's own new call site passes a section citation; widened to FR-or-section (v1.1). **L3** — the pass-10 runtime guard implemented ONE of the design-time lock's three predicates while both new comments called them two halves of one invariant: a NEGATIVE `[GT]` numerator passed the sum guard and silently deleted its own tier (Minor at −100 = a 0/20/80 split — the pass-6 rule-at-one-boundary shape, inside the fix being verified). Non-negativity added at the same site; zero stays legal (an expressible empty-tier intent); spec §3.2 v0.10 + Appendix A v0.9 + both comments corrected. **L4** — one draw key had three spellings across the ERR-041-019 guard's doc, its throw message and #41 §3.1.1; both local sites now spell `(worldSeed, playerId, actionOrdinal = worldDay × RADIX + purpose)` (v1.13, doc only). Verified after the fixes: build 0 errors; `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52. **The pass-10 gate PASSED — the EIGHTH consecutive whole-tree verdict** (32 suites, quarantine empty, `MatchEngine.Tests` 436/0/10), superseding the invalidated pass-9 run as recorded in v1.90. Pass-11 gate + AR pass 12 launch next. Prior entry below.)
**Updated (prior):** August 8, 2026, still later same day (v1.90 — **Balance-pass adversarial review pass 10: 0 High, 1 Medium, 6 Low, all fixed — the Medium is ERR-041-003's High RECURRING, measured.** **M1:** the severity-split invariant was the only one of #41's three catalogue invariants with NO production guard — `SeverityMinorPermille`/`SeverityModeratePermille` are `[GT]` config keys, the only lock reads the same fields in a config-unbound gate (so it can only ever see the 600/300 fallbacks — exactly the vacuous-lock shape ERR-041-003 recorded), and a shipped config at 600+400 or 700+300 **silently deletes the `Serious` tier**: the reviewer measured Serious = 0 over the whole `[0, 16000)` range at both, ~10% of all injuries becoming 21-day Moderates with no symptom the season instrument can see (its bands do not read severity). The file already knew the answer twice — `DrawOccurrence` guards `InjuryRiskMax ≤ DENOM` at the one drawing site and `AppearanceWindow.RequireValidWindow` cites that posture — so the fix is the third instance of the same posture: `ClassifySeverityFromDraw` fail-louds when the numerators sum to the denominator or past it, §3.2's pseudocode and the Appendix A row carry the enforcement site, and the catalogue lock's comment records the two-layer split (design-time fallback lock + runtime classifying-site guard, no reachability case by design — the denominator-guard precedent). **Lows:** **L1** — pass 9's `SeasonSaveManagerTests` header edit left the prior two-line `Modified:` field's continuation dangling with an unmatched paren — the exact stale-parenthetical class pass 8 L3 fixed, one file over. **L2** — the v2.93 row was inserted BELOW v2.92 against `CHANGELOG-src`'s own add-at-top rule; moved. **L3** — both outlines' §2 ranges were stale (#41's went stale the moment pass 9 added F8; #29's had been TWO landings behind since July); corrected with rows. **L4** — F8 had no §5 test-plan id at either spec while both suites already lock it (`AdvancingTheSentinelDay_FailsLoud` ×2): **T-MD-DET-010** / **T-TR-DET-006** assigned, naming the existing locks. **L5** — #41 §9's version table carried TWO rows numbered 0.2 (the pass-7 edit had collided with the July 23 row, leaving it unaddressable by version) and #29 §2's table read 0.3/0.5/0.6/0.4; renumbered 0.3/0.4 with as-published annotations, reordered. **L6** — `AppearanceSaveCodec`'s duplicate-key refusals cited **FR-MD-025** — a sibling spec's id on the #30-owned APPR block that #41 is forbidden to describe (KD-2/KD-7); both call sites now cite #30 Appendix B.1, the block's owning pin. **Process note, recorded against this session:** the pass-9 whole-tree gate was **INVALIDATED** — the pass-10 fix build ran while that gate's test phase was still executing, swapping binaries under the unfinished suites (the hold-builds-during-a-gate rule this loop itself established), so the run would have mixed two trees; stopped at 31/32 suites all green and superseded by the pass-10 gate at HEAD, whose tree strictly contains pass 9's. Suites after the pass-10 fixes: `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52, build 0 errors. AR pass 11 launches with the pass-10 gate. Prior entry below.)
**Updated (prior):** August 8, 2026, later same day (v1.89 — **Balance-pass adversarial review pass 9: 0 High, 2 Medium, 5 Low, all fixed — both Mediums the loop's own recurring classes, each verified by execution or enumeration.** **M1:** the cursor-vs-clock invariant was TWO hand-copied five-predicate walks — one over the live `PlayerCareerStates` arrays (pass 6 M3), one over the decoded blocks in `SeasonSaveManager` (pass 6 M2) — with nothing enforcing agreement: the parallel-surface class the T2 AR's H3 collapsed and pass 4 M1 resolved for the sibling gate ONE PASS before the duplicate was written. Exhaustive call-site enumeration showed the file-boundary copy's **medical-lag predicate had no isolating case at Save or Load** — deleting it left the whole suite green, the exact defect pass 8 M1 fixed at the boundary pass 8 did not check. Collapsed to ONE owner: `RequireTraining/Medical/AppearanceAnchorCursorWithinClock` internal statics on `PlayerCareerStates` (the `RequireGloballyUniquePlayerIds` shape), both boundaries delegating; + the medical-only-lag case at Save (training in-band, so only the medical predicate can refuse). **Mutant re-run post-fix: deleting the medical-lag clause now fails BOTH boundaries' locks** (`Save_CursorLagging…` + `Constructor_PairingGate…`) — one deletion, two failures, which is what the shared owner buys. **M2:** the ERR-041-012 sweep's FOURTH residue, and the first INSIDE files the previous widening bumped for this class: #41 §9.1's content gate still ticked KD-1 as "one stream"; §9.6's **Decision** still ratified a T-phase plan ordering "stream registration" at T2 (pass 7 M1's headline wording, surviving in the approval that signs the plan off); §8.2 cited **FR-LW-031 — the anti-phantom FR — as authority FOR registering the stream**; and the LIVE research-alignment supplement (awaiting owner sign-off, the doc that drives #41's next landing) named "#41's existing `injuries.occurrence` stream" twice. All re-anchored. The mandated repo-wide grep then caught a fifth site the reviewer had not flagged: `DeterministicSimConstants`' own 0x2A XML doc still designated the draws by the retired stream name. **Frozen-by-design exclusions now recorded here so the next sweep does not re-derive them:** `injuries-medical-design.md`, `personalities-morale-dynamics-design.md`, `transfers-contracts-negotiation-design.md` (pre-promotion supplements, frozen at convergence per the root governance rule), and #16 §3.5's v1.0.11 note (deliberately annotated superseded, original kept). **Lows:** **L1** — pass 8's #16/SPEC_INDEX back-props shipped ROWLESS against the commit's and v1.88's explicit "with version rows" claim (both false for the two files outside #41 — the FIFTH consecutive FR-CS-057 recurrence, inside the commit whose own Low list fixed the fourth; v1.88's claim is corrected by this entry): §3.5 gains v1.0.15, SPEC_INDEX gains its header bump. **L2** — four tracking headers dated pass 8 August 9 (tomorrow); corrected. **L3** — `SEASON_SAVE_FORMAT_VERSION`'s inner-decoder cref list omitted `AppearanceSaveCodec.Decode`, the omission its own v1.3 row records fixing for the sibling codecs. **L4** — `MedicalStep`'s sentinel-as-worldDay refusal had NO normative source (a production fail-loud with no spec row — the ERR-041-012 class inverted): new **F8** in #41 §2.3 + the §3.1 pseudocode guard, and the SAME unspecced guard found and fixed at the #29 sibling (`TrainingStep`, #29 §2.3/§3.1) in the same commit — the folder-boundary lesson applied forward for once. **L5** — the severity-split invariant said ≤ where its own sentence requires strict < (at a sum of exactly 1000 `Serious` is unreachable with the invariant "satisfied"): appendix, catalogue doc and lock (`Assert.Less`) all corrected. Suites after the fixes: `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52; M1 mutant killed at both boundaries. The pass-8 gate PASSED meanwhile — the SEVENTH consecutive whole-tree pass, quarantine empty. Pass-9 gate next; AR pass 10 launches with it. Prior entry below.)
**Updated (prior):** August 8, 2026 (v1.88 — **Balance-pass adversarial review pass 8: 0 High, 2 Medium, 6 Low, all fixed — and both Mediums are again completions-at-a-boundary of earlier fixes, each demonstrated.** **M1:** pass 7's "the lock now isolates every predicate" was FALSE for the two first-evaluated predicates — the original ctor lock drove both cursors ahead together, so the training branch was shadowed in BOTH directions, and deleting it entirely left the whole suite green (mutation-demonstrated); (a2) training-only-ahead and (c3) training-only-lag close it at five predicates, five isolating cases, one PASS case. The pass-6 shape recurring inside the pass-7 fix that cited it, for the second time. **M2:** the ERR-041-012 de-phantoming was FOLDER-scoped — the registered `injuries.occurrence` stream survived outside #41: in **#16 §3.4's own `DOMAIN_TAG_INJURIES_MEDICAL` row** (the allocation's owner, still promising "the code const + RNG-stream registration land at #41 T2" — the const landed at T0 and the registration is forbidden), its v1.0.11 revision note (annotated superseded, original frozen), **three #40 lines including a comparator factually claiming #41 "does register a stream"**, and SPEC_INDEX's #41 summary. All re-anchored with version rows — the sweep's third widening (§4.5 → §§1-6 → the other specs), each widening found because the previous one's grep stopped at a folder boundary. **Lows:** #41 §3 was at v0.6 with no v0.6 row (the rowless class again — pass 5's probe note claimed under it); two version tables reordered ascending (#41 §2's 0.6/0.7 — the third recurrence in a table whose own rows record the previous two — and #30 §3's 1.4–1.6, which read DESCENDING); two stale `Modified:` parentheticals (one five versions behind, in a file pass 7 itself edited); FR-MD-007 and the `DRAW_PURPOSE_OCCURRENCE` row stop naming the stream that must never exist (true statements, phantom name); the §3.1.1 transfer note separated from the radix rule it had fused into; the instrument's per-club log line reads `DefaultClubCount`. Suites after the fixes: `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 356/359 (the pass-8 cases live inside the pairing lock). The pass-7 gate PASSED meanwhile — the SIXTH consecutive whole-tree pass, quarantine empty. Pass-8 gate re-running; AR pass 9 next. Prior entry below.)
**Updated (prior):** August 8, 2026, last entry of the day (v1.87 — **Balance-pass adversarial review pass 7: 0 High, 2 Medium, 4 Low, all fixed — both Mediums are COMPLETIONS of pass-6 fixes, and every pass-6 verification held (the bidirectional gate's boundary arithmetic exact at the sentinel, the ctor gate breaking no legitimate composition, the internal-ized writers breaking no caller).** **M1:** the ERR-041-012 sweep had stopped at §§1/2/5/6 — SIX more #41 files still mandated the registered `injuries.occurrence` stream, including §7.1's T2 instruction ORDERING the registration §4.5 forbids (the file a deep-tier author reads for the T-phase plan, pointing the wrong way), §9 R-02 signing the stream off as verified evidence, and Appendix C defining T-MD-NEU-003 as the registration's independence while §5.5's pass-6 restatement defines the same id as vacuous-by-construction — one test id, two contradictory definitions. Swept: outline v0.2, §4 v0.5, §7 v0.2, §8 v0.2, §9 v0.2, appendices v0.5. **M2:** pass 6's composition gate had ONE of its five predicates locked — the pass-6 test drove both cursors ahead together, so the training branch threw first and shadowed the rest; two mutants deleted the medical, appearance and both lagging checks with the whole suite green (demonstrated). The lock now isolates every predicate — medical-only ahead, appearance-anchor ahead (the acute wedge case), lag ≥ 2, medical-only lag — plus the lag-of-exactly-one PASS case mirroring the file boundary's. **Lows:** two `Modified:` parentheticals three passes stale (the class pass 5 recorded fixing); #30 §3.4's v1.5 pseudocode had put `RunCareerDaySteps` ABOVE the F5 guards, contradicting §3.3.2's after-every-guard property two sections up and the code (reordered, v1.6, with the season-complete refusal that defines `Calendar.DayOf(round)`); `SeasonSaveContents`' Purpose finally names the appearance records; the duplicate-ClubId refusal gains its message+paramName lock (without it the unstable sorts pair duplicates arbitrarily and the gate names a phantom mismatch). Suites after the fixes: `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 356/359 (3 known skips; +2). The pass-6 gate PASSED meanwhile (fifth consecutive: `4a5ab5e`→`92af584`→`b76cbd3`→`d891aad` all whole-tree, quarantine empty). Pass-7 gate re-running; AR pass 8 next. Prior entry below.)
**Updated (prior):** August 8, 2026, latest same day (v1.86 — **Balance-pass adversarial review pass 6: 0 High, 4 Medium, 5 Low, all fixed; two demonstrated by probe, and every pass-5 fix verified sound (Appendix B.1 field-for-field against the codec, §3.4 v1.4 against the loop, the restored changelog entries verbatim against the landing commit, the rebuilt default-block lock killing its mutant).** **The Mediums — all four are the same lesson from different angles: a rule enforced at one boundary and stated as if it held at all of them.** **M1:** the KD-4 calendar invariant was Load-only, three lines above the pass-5 gate whose own doc states the never-write-what-Load-refuses rule — and the suite carried a PASSING test that saved the unloadable file and recorded the asymmetry as intended; Save now refuses it, the test rebuilt through `SeasonSaveCodec.Encode`. **M2:** the pass-5 cursor gate was AHEAD-only; a #29/#41 cursor LAGGING the clock by ≥ 2 is WORSE — F7 refuses the gap on every later advance and the day steps run before the clock increment, so the career wedges permanently while saving cleanly (demonstrated through public API); the gate now refuses both directions, with the lag-of-exactly-one PASS case locked (the pre-increment convention's normal saved state). **M3:** the rule lived at the file boundary but not the COMPOSITION boundary — the reviewer drove a career eleven days ahead through public API, composed a loop that ACCEPTED it, and watched seven world days of conditioning and seven armed draws silently skip while the save root refused the identical state; `PlayerCareerStates.RequireCursorsWithinClock` now runs in `SeasonLoop`'s constructor beside its KD-4 and coverage gates, and the three career writers went `internal` (public bought nothing and handed any `Career` holder the ability to drive the career off the clock — the `SeasonLoop.World` lesson, again). **M4:** ERR-041-012's de-phantoming stopped at §4.5/§3.1 — the registered `injuries.occurrence` stream survived in FIVE places across #41 §§1/2/5/6, including the headline KD-1 (flatly contradicting FR-MD-005 in the same spec) and §2.2's normative signature, which named the phantom `rng` and could not express the dial FR-MD-027 declares required; one sweep, four section bumps (§1 v0.2, §2 v0.7, §5 v0.4, §6 v0.2), KD-1's closing clause also re-anchored off the retired ERR-030-026 ordering. **Lows:** five step-9 prose sites → step 12 (step 9 is the #32 null seam under pass 1's own renumbering); `WindowMask`'s unreachable width≥32 branch deleted; `SeasonSaveManager`'s own header/summaries mention the appearance block; #30 §3.4's pseudocode gains the pre-round `RunCareerDaySteps` line and the clock guard defining its own `worldDay` (v1.5); duplicate ClubIds refused by name in the coherence gate. Suites after the fixes: `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 354/357 (3 known skips; +2). Full gate re-running; verdict in `CHANGELOG.md`. AR pass 7 next. Prior entry below.)
**Updated (prior):** August 8, 2026, still later same day (v1.85 — **Balance-pass adversarial review pass 5: 0 High, 7 Medium, 10 Low, all fixed; three findings demonstrated by probe execution, and the M4 gate's first suite run caught the round-trip fixture itself writing incoherent cursors.** **The Mediums:** **M1 (ERR-030-028, row below)** — the `APPR` sub-blob's byte layout was specified in NO spec, existing only in `AppearanceSaveCodec.cs`'s own comment, while F3 makes the first written layout the format permanently — ERR-029-004's exact class, on the block created one landing after that ERR was filed; #30 Appendix B gains **B.1** (the field-by-field layout, the four sibling MUSTs, the deliberate no-`[GT]`-gating-on-decode decision). **M2** — #30 §3.4, the owning spec's algorithm for the round loop, contradicted the code this chain built on three counts: the seam marked "empty until #44 T2" has been LIVE via #41 FR-MD-023 since T2 (both clubs, both paths); `PlayThroughEngine`'s pseudocode showed raw rosters and the two-argument `ConfigureSquads`; and the loop had NO appearance-record step at all — §3.4 untouched since v0.8 while three landings changed the code it specifies (§3.4 → v1.4). **M3** — the pass-4 default-block lock killed no mutant (demonstrated: the reverted code threw the same TYPE from the count branch for its 1-player siblings); rebuilt with empty siblings + message/paramName pins. **M4** — a per-player world-day cursor AHEAD of the restored world clock wedged the career PERMANENTLY (demonstrated: the day steps run before `AdvanceDay`, so the slot-4 window read throws forever once the dial is armed — and the trap went live at D4; the #29/#41 cursors fail the other way, a silent per-player freeze-out); `SeasonSaveManager` gains the cursor-vs-clock rule as its SECOND cross-blob check, at Save AND Load — and its first run caught the round-trip fixture's own day-19 cursors against a clock of 2. **M5** — five doc sites still described the pre-arming world, one of them deferring a consequence "to the balance pass that arms the dial" which armed it and did not: **fielding the injured is strictly FREE** (unmodified attributes, cannot be re-injured — the KD-6 entry gate — recovery not extended); now a RECORDED-NOT-FIXED block at `SelectAvailable` (#27/#28 attributes or #41 deep-tier own the consequence). **M6** — the manifest's per-file INVENTORY (as opposed to its chronology) omitted eleven balance-pass files and carried six wrong rows (`SEASON_SAVE_FORMAT_VERSION = 2`, five-blob frame, the retired `[DERIVED] OccurrenceDrawDenom`, …); all corrected. **M7** — AR pass 1's record had been written by EDITING the published balance-pass changelog entries in place, which both files forbid — no chain entry, no VERSION HISTORY row, for the chain's only production `[GT]` change; both originals restored verbatim from the landing commit and pass 1 split into its own entries (v2.88, numbered past the documented collisions). **Lows:** the log's own footer pinned v1.5 (retired — the header is the authority); four three-column rows in a four-column table; KD-3 gains the ERR-027-004 pointer (#27 §1 v0.3); `[FIXED] APPEARANCE_BITMASK_MAX_WINDOW_DAYS = 31` catalogued and read by the guard (was a bare literal in two places); two stale `Modified:` parentheticals; `PlayerRecord`'s header cites #27, not the pre-promotion supplement; the scenario gains the injury-changed-the-eleven precondition and states the round-mate model's unfiltered-roster assumption; the instrument's closed form reads the round count and window from their constants; the `MatchLoad(2,0)` congestion rows named as FORMULA PROBES (the Stage-0 cadence cannot produce them — #43's cup calendars will); the coherence gates' `ParamName` names the OFFENDING set (demonstrated misdirection). Suites after the fixes: `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 352/355 (3 known skips; +3). Full gate re-running at this entry; verdict in `CHANGELOG.md`. AR pass 6 next. Prior entry below.)
**Updated (prior):** August 8, 2026, even later same day (v1.84 — **Balance-pass adversarial review pass 4: 0 High, 6 Medium, 10 Low, all fixed; the reviewer demonstrated two findings by executing a scratch probe against the built assemblies rather than by reading.** **The Mediums:** **M1** — `Save`'s pass-3 coherence gate was one predicate short of its own stated contract: it still wrote the one file `FromBlocks` refuses, a cross-club duplicate `PlayerId` — the ERR-041-019 check added to `FromBlocks` in the SAME commit as the gate (demonstrated: saved cleanly, loaded, restore refused); the gate now runs the same walk (`RequireGloballyUniquePlayerIds` went `internal`; one owner, one message). **M2** — the appearance record's ENGINE branch had never executed anywhere: every career suite runs `QuickSimAll`, every engine-mode career test stops at `BootFixtureEngine`, and the one test driving a real engine round built the loop careerless — the T2 AR pass-5 High's shape recurring one layer out, in a landing that cites it; the season-loop scenario now wires a career with an injured managed-club starter and asserts the engine-resolved fixture records the FILTERED eleven, on the one real match it already pays for. **M3 (ERR-027-004, row below)** — ERR-041-019's contract lived only in the CONSUMING spec; FR-SQ-010, the row #42/#31's allocators will be written against, still said club-scoped full stop — amended, with §2.2.3 and `PlayerRecord`'s header. **M4** — the per-club roster reconciliation DESTROYS a transferred player's career state (departure + `Create()` — he arrives fit), directly contradicting the "player carries his medical identity" rationale pass 3 wrote one file away; RECORDED, NOT FIXED at the code site and #41 §3.1.1 (cross-club carry is #31's arrival obligation; ERR-041-019's global ids are what make it implementable). **M5** — two APPROVED spec headers misstated their own currency on the sections this chain amended (#41 §3 at v0.4 with a v0.5 table; #30 §3 at v1.1 with v1.2/v1.3 rows — two consecutive landings missed it, the drift class both files' own histories record); bumped with demotions. **M6** — two pass-3 test-file edits shipped ROWLESS (the FOURTH consecutive recurrence of the FR-CS-057 class, including the discriminating lock the pass was proudest of) and `file-manifest.md` asserted version numbers not present in the tree; rows added, five `Modified:` dates aligned. **Lows:** the gate refuses a default block by name instead of NRE-ing at its clone (demonstrated), and every refusal branch + the permuted-club-order PASS case is locked; `AppearanceWindowDays` gains the `[1,31]` catalogue invariant its sibling had; the shifting-provider lock asserts its pass-through budget was CONSUMED (a call-shape change that removes a resolve otherwise leaves it passing vacuously — including against the pre-fix loop); `SeasonSaveCodec`'s header/doc predated the appearance block ("all four"); #41 §3.2's overflow bound corrected to 1.6×10⁷; this log's OWN ERR-041-011/ERR-029-007 resolution rows still said 1% (annotated in place); #41 §2's rows 0.5/0.6 un-swapped (the third out-of-order table in this chain); root `CLAUDE.md`'s T2 "Recorded, not fixed" MatchLoad residual marked CLOSED at D2 (it read as live); the F6-mirror guard's comment states it is inert by construction and cannot be locked — the pass-6 question has no answer there by design. Suites after the fixes: `InjuriesMedical.Tests` 67/67 (+1: the window invariant), `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 349/352 (3 known skips; +3). Full gate re-running at this entry; verdict in `CHANGELOG.md`. AR pass 5 next. Prior entry below.)
**Updated (prior):** August 8, 2026, later same day (v1.83 — **Balance-pass adversarial review pass 3: 1 High, 4 Medium, 8 Low, all fixed — and the High's guard caught a REAL collision on its first execution.** **The High, ERR-041-019 (filed + resolved, row below):** the now-ARMED occurrence draw is keyed `(worldSeed, playerId, worldDay)` with NO club term, while #27 promises `PlayerId` uniqueness only within a club and `PlayerCareerStates` is keyed `(ClubId, PlayerId)` on exactly that premise — globally unique today only by accident of `RosterGenerator`'s formula, stated nowhere, checked nowhere. Enforced now at all three id entry points (`ForLeague` / `FromBlocks` / `PrepareRosterSync`), key deliberately unchanged (a club term re-rolls every career and moves a transferred player's luck with his club). **The first suite run under the guard failed**: the roll test's own regen fixture (suffix `N+5` on club 1 ⇒ id `2N+5` = club 2's local-5 player) had been creating a cross-club duplicate — two players sharing one draw — since T2, invisibly. **The Mediums:** M2 — `FromBlocks` copied the three state arrays but shared the training block's `PlayerIds`, the public back door reopened through the binary-search KEYS (an aliased write breaks the ascending precondition enforced twenty lines earlier); now copied, locked through the block. M3 — the appearance state's carry through the roster sync had NO test (delete `appearance[i] = heldAppearance[held]` and every suite stayed green — the pass-6 question asked of the third state set's third carry point); the roll lock now records pre-roll and asserts anchor+bits post-roll. M4 — pass 2's "locked by" claim was FALSE: both recorded locks compute their expectation through the same `SelectAvailable` walk the deleted code used, so both passed against the PRE-fix loop; the new `TheRecordedXi_ComesFromTheResolutionsOwnSquadInstance` shifts the provider's roster mid-round through a one-shot decorator and fails pre-fix by construction. M5 — #41 §3.1's normative signature still took the `rng` its body never used, never named the dial FR-MD-027 makes a required parameter, and §3.5 repeated the stale call — the ERR-041-012 class one section from the section D4 rewrote; now `worldSeed, occurrenceEnabled` with the gate in step 2. **Lows:** the `RecordFixtureAppearances` pair form (both clubs validated before either written — the per-club discipline one level up, with the unplayed-fixture lock now asserting the home side is unwritten too); `Save` gates the career-block triple's coherence, order-insensitively (it could write a file its own restore path refuses — and the round-trip suite WAS writing one: 1 training + 1 medical + 0 appearance clubs); the #30 §3.3 slot list un-marked slots 2/4 "NULL SEAM today" (LIVE since T2, cited by the §3.3.2 that shipped in the same amendment); two out-of-order version-history tables swapped (`SeasonLoop.cs`, #41 §4); the null-appearance Save refusal and the empty-`AppearanceClubs` assertions join their siblings; `SeasonLoop`'s header parenthetical and the stale 23%-disarmed comment corrected; #41 §3's v0.4 row 1% → 1.6% like its appendices counterpart. Suites after the fixes: `InjuriesMedical.Tests` 66/66, `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 346/349 (3 known skips; +3 = this pass's locks). Full gate re-running at this entry; verdict in `CHANGELOG.md`. AR pass 4 next — the loop ends only when a pass returns no new High/Medium. Prior entry below.)
**Updated (prior):** August 8, 2026 (v1.82 — **Balance-pass adversarial review pass 2: 0 High, 1 Medium, 9 Low, all fixed; suites re-run green after the fixes (`InjuriesMedical.Tests` 66/66, `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 343/346 — the +5 are this pass's new locks — full-gate verdict recorded in `CHANGELOG.md` when it completes).** **The Medium:** the loop's appearance recording re-derived the fielded XIs by a SECOND `SelectAvailable` + selector walk in `AdvanceAndPlayNextRound`, one comment away from the configuration it claimed to mirror — an unenforced agreement of exactly the documented-not-structural class the T2 AR's H3 collapsed (`CanSelect`'s hand-copied walk), and the moment a manager-chosen XI lands (#38 Wave-7) the two sites diverge silently, recording eleven men who did not play. Fixed structurally: the fielded XIs now come OUT of `ResolveFixture` — the engine branch derives them inside `BootFixtureEngine`'s new id-producing overload, one statement from the `ConfigureSquads` that consumes the same filtered squad instances; the quick-sim branch from the very squads its rating reads — and the loop's second walk is deleted (`RecordFieldedAppearances`/`RecordClubAppearances` gone). Locked by `EnginePath_HandsBackTheFilteredElevensIds` (both sides asserted; injured starters absent from the ids the record consumes) and by the injured-starter round case, which forces the availability filter to PARTICIPATE in the XI-identity assertion — with every player fit, both sides could read the unfiltered squad and still agree. **Lows worth the ink:** `AdvanceMedicalDay` computed the occurrence inputs before `MedicalStep`'s F6 cursor could no-op a re-entered day, so the ERR-030-027 pre-round/next-advance pair read the appearance window with today's match already in the bits (correct only by the shift-0 exclusion — now guarded so that correctness is not a draw-path dependency); the direct `RecordAppearances` refusals had no lock proving NOTHING is written on a failed validate (the fresh-listed-first ordering is the interleaved-write mutant-killer), nor one proving a recording throw leaves the fixture unplayed — the property the pass-1 ordering fix exists for; the perturbation lock's starter band-edge margin was ~0.03, so the effect-size claim moved onto delta-form asserts no band refit can erode; `MedicalStep`'s tuning note still claimed the worst case "lands strictly below the clamp" at "1%" — false since M3's headroom raise (it saturates; 1.6%) — plus three "1%" test-comment residues and `training-system-design.md`'s duplicated/malformed "v0.5" row (renumbered v0.8, 1.6%); the slots-9–11 seam comments joined `RunCareerDaySteps`; three pass-1 one-line doc edits had shipped ROWLESS (`InjuriesMedicalConstants`, `MedicalStep`, `InjuriesMedicalConstantsTests` — rows added, the FR-CS-057 class recurring one pass after its hygiene sweep); `SeasonSaveManager`'s orphaned header fragment and `SeasonSaveManagerTests`' stale T1 annotation; the three-parallel-state-sets ceiling note on `PlayerCareerStates` (a fourth per-player set collapses the shape into a per-player career struct — #44 suspensions is the candidate); and the substitution-dependency notes on `AppearanceState`/`SquadRating.StartingElevenPlayerIds` ("who started" equals "who played" only while Stage 0 fields a fixed eleven; the widening belongs to the recording call sites). No new ERR ids — every finding is implementation/test/doc-side of already-filed entries. Prior entry below.)
**Updated (prior):** August 7, 2026, latest same day (v1.81 — **Adversarial review over the balance-pass landing: 0 High, 3 Medium, 8 Low, all fixed; the reviewer executed the suites and re-measured the season chain, and two of the three Mediums are this file's own recorded test-tautology classes recurring in the landing that cited them.** **M-1:** the D4 headline lock (`RestoredCareer_WithTheDialArmed_StillInjures`) passed with the restored career DISARMED — `InjuryCount` is cumulative and rides through the save, so `Greater(injuries, 0)` was satisfied by the six pre-save injuries; reproduced by mutation (armed 26 vs disarmed 6, both > 0). Now asserts growth PAST the carried count. The pass-6 which-test-fails-if-reverted question, failed by the very lock advertised as that question's answer. **M-2:** `HardContacts_AreWeightedZeroAtStage2` became a clamp tautology at the retuned weights — both operands saturated `InjuryRiskMax`, so mutating `HardContactWeight` 0 → 100 still passed; operands moved below the clamp with an explicit stay-below precondition (the `RiskAssembly` magnitude operand had the same disease, same fix). **M-3, the substantive one:** the two new `[GT]`s consume 9,600 of what was a 10,000 ceiling, so for every player with an appearance in the window the #29 passthrough and BOTH robustness mitigations were compressed into ≤4% of the range (measured over a real season: starter risks spanned [9143, 10000], 7% at the cap) — P2's skill-as-discrimination doctrine inverted one landing after it was cited. Fixed as headroom, not a refit: `InjuryRiskMax` 10000 → 16000 (still below #29's ~19,960 unclamped producer max, so the clamp still binds and `TrainingRiskFlows`' saturation lock still holds), which also un-flattens congestion (two matches in a window now price at 1.49%/day; the 1.6% cap binds beyond). Re-measured over the same 8 seeds: league 719–822 (pooled 783), starters 2.08, reserves 1.13, unavailability 9.5% — the aggregates barely moved, which is the point: sub-cap probabilities were untouched. Lows fixed: the stale ERR-030-026 convention text in `PlayThroughEngine`'s summary; `RunCareerDaySteps`' seam list renumbered to the spec's 0–12 (slot 0 was missing, the tick was "9"); `RecordFieldedAppearances` moved ABOVE the apply/emit/mark sequence (a throw after `MarkFixturePlayed` strands the round unrecoverably); `RecordAppearances` pre-checks the day-regression refusal so the write loop cannot half-record a club; `ImpossibleOccurrenceRisk` renamed `MinimumOccurrenceRisk` (the baseline term made "impossible" a lie — it assembles to 3600); the appearance round lock asserts XI IDENTITY against the selector's ids (count could not see a wrong eleven; doubles as the mode-independence lock); the save round trip proves 44 bits were recorded before asserting they survive; the perturbation comment's false "no clamp binds" reasoning replaced with the reviewer's measured halved-chain numbers (AppearanceLoadWeight/2 → starter 1.511, BaselineDailyRisk/2 → reserve 0.606, both out of band); and the FR-CS-056/057 hygiene sweep (`AppearanceBlock`'s missing `Modified:` — the exact Low the T2 pass 6 closed, recurred on a new file; three stale `Modified:` dates; six test files' missing version rows; `SeasonSaveManager.Save`'s missing `appearanceClubs` param doc; `SeasonSaveConstants`' five-of-six nested-version Purpose list). Suites after the fixes: `InjuriesMedical.Tests` 66/66, `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 338/341 (3 known skips). Prior entry below.)
**Updated (prior):** August 7, 2026, latest same day (v1.80 — **ERR-041-011 + ERR-041-012 + ERR-029-007 filed + RESOLVED at the balance pass (D3/D4): the retune that arms FR-MD-027, measured in the football band.** **ERR-041-011** (#41 §3.4 / Appendix A / #29 Appendix A): three defects in one formula's scale. (a) The `OCCURRENCE_DRAW_DENOM == INJURY_RISK_MAX` identity made the draw's denominator a `[GT]`-derived value — and the draw is `hash % denominator`, so one config edit re-rolled every career's injury luck with the save recording nothing about which config produced it; the denominator is now `[FIXED]` at 1,000,000 (per-million probability resolution) with the `INJURY_RISK_MAX ≤ DENOM` invariant enforced fail-loud at the draw site, so config edits move only thresholds. (b) §3.4 had no exposure-independent term, so the default Balanced focus converged on an injury-proof-forever player (the T0 fifth AR pass's third measured absurdity); `BASELINE_DAILY_RISK` (4000, ≈0.4%/day gross) lands INSIDE the sum before the mitigation — position normative, so robustness discriminates the floor — with the recorded R-2 note that the supplement's under-exposure arm must re-fit against it, not add beside it. (c) `APPEARANCE_LOAD_WEIGHT` refits 150 → 5600 on the new scale (≈3.9% cumulative per match over the 7-day window). Appendix A's `INJURY_RISK_MAX` re-tags `[CROSS: #29 Appendix A]`, discharging ERR-041-003's standing back-prop; ERR-029-007 is the sibling #29 Appendix A row correction (no longer "the draw denominator"; now the 1%/day cap). **Measured through the wired chain by the new season-scale instrument** (`SeasonInjuryRealismTests`, 8 seeds × full 20-club quick-sim seasons): league 717–816 injuries/season (~39/club vs the E-1-derived 30–55 band), starters 2.08 pooled, reserves 1.12, squad unavailability 9.4% — all locked league-wide, with the per-population bands proven by closed-form perturbation to fail if either new `[GT]` halves. The characterization test carries the AFTER numbers at per-100k resolution (631/371/831 per-100k daily + the appearance rows and the 1% congestion clamp), replacing the BEFORE absurdities (23.1%/0/43.1% daily). Certainty is structurally unreachable at the per-million denominator (max 1%/day), so the suite's forced-occurrence pattern becomes a deterministic hot-day scan over the keyed derivation. **ERR-041-012** (#41 §4.5 / FR-MD-005): arming the dial was the moment normative text describing a NONEXISTENT registered stream (`injuries.occurrence` at ordinal 92, "registered at the first draw site") would have governed a live subsystem — the requirement was self-contradictory (a registered stream is cursor-positioned, forbidden by FR-MD-006/007) and was resolved in code at T0 (ERR-041-002); §4.5 now describes the keyed derivation and pins ordinal 92 as deliberately unallocated (FR-LW-031), discharging ERR-041-002's deferred re-anchor. **D4 arms the dial**: `InjuryOccurrenceEnabled` becomes a REQUIRED construction argument (a default in either position flips every omitting call site with no diff — the evidence advisor's rule), production posture ON, the OFF identity supported and locked both ways at season scale, plus the restored-career-still-injures lock (the dial is not serialized, so the reconstruction site's choice is the whole game). FR-MD-027 re-stated ARMED (`section-2.md` v0.6). Suites: `InjuriesMedical.Tests` 66/66, `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 338/341 (3 known skips). **GATE PASSED — executed locally, whole tree, quarantine empty; `MatchEngine.Tests` 436/0/10 (the arming moved no acceptance band).** Prior entry below.)
**Updated (prior):** August 7, 2026, latest same day (v1.79 — **ERR-041-010(b) CLOSED at the balance pass (D2): the per-player appearance record exists, persisted, and supplies FR-MD-010's `MatchLoad`.** The shape is the council's: a lazily-shifted day-bitmask per player (`AppearanceState`), shifted at read time so no daily mutation step, no new KD-2 slot and no third idempotency cursor is created; written by `AdvanceAndPlayNextRound` for both clubs' fielded XIs on both resolution paths through the new `SquadRating.StartingElevenPlayerIds` (one selector, three read shapes — no second selection walk); persisted as the season frame's new mandatory `APPR` v1 sub-blob (typed `AppearanceBlock`, magic-led, `SEASON_SAVE_FORMAT_VERSION` 3→4, no migration); read at slot 4 through a window that covers the `AppearanceWindowDays` `[GT]` days strictly BEFORE the draw day — never the current day, so ERR-030-027's pre-round draw can never see a match not yet played. FR-MD-010's unit pinned and its false "a count #30's fixture result already tracks" premise corrected (`section-2.md` v0.5); #30 Appendix B v0.5 additionally repairs a T1 omission (the v3 training/medical blocks had never been recorded in its frame sketch); `unified-season-save-design.md` gains §3.1 (the v2–v4 amendment chain). The multi-bit window path is locked by hand-driven tests, because the 7-day fixture calendar makes a season-driven popcount identically 0 or 1 — a season test structurally cannot exercise it. Gate run pending at this filing. Prior entry below.)
**Updated (prior):** August 7, 2026, latest same day (v1.78 — **ERR-030-027 filed + RESOLVED at the #29/#41 balance pass (D1): the round now sits AFTER the fixture day's own day-slots, closing the half of ERR-030-026 deferred to this pass.** The council convened over the balance-pass plan (integrity + evidence advisors, both independently) rejected the deferred question's own framing: splitting `AdvanceMedicalDay` into separately callable recovery/occurrence halves would cost a second persisted cursor — `MEDICAL_SAVE_FORMAT_VERSION` 1→2 with no migration (KD-7/F3) — plus a KD-6 revision, to buy ordering the cheaper shape gets free. The adopted shape: `AdvanceAndPlayNextRound` runs the fixture day's own KD-2 slots at its top, pre-round, after every guard, over the whole career, through the new `SeasonLoop.RunCareerDaySteps` helper both callers share; the post-round re-run inside the next advance is a cursor no-op (F6 idempotency, which both live steps already carry). Recovery lands before selection — a player whose tier expires on matchday plays it, so tiers mean what they say and the balance pass fits them un-biased. The occurrence draw moves to matchday MORNING: a matchday-drawn injury is a pre-kickoff training-ground loss, and match participation reaches the draw through the FR-MD-010 appearance window, which by construction never contains the current day (a match on day *d* first feeds the draw on *d+1* — one day of latency in a rolling multi-day window, in exchange for FR-MD-022's one-atomic-step contract surviving verbatim). #41 text untouched; #30 §3.3 gains **§3.3.2** pinning the convention, §3.3/§3.4 amended (`section-3.md` v1.2). `DayAdvance_StopsBeforeTheFixtureDaysOwnSteps` rewritten to assert the new convention BOTH ways: the served-his-time player is available for THIS round, and the fixture day is lived exactly once (the conditioning cursor is the discriminating assertion — Balanced's net fatigue is 0 by construction, so fatigue alone would be a tautology); `AWholeSeason_PlaysWithTheCareerWired`'s cursor expectation moves to `CurrentWorldTick` (the pre-round step lives the fixture day itself). Gate: **PASSED** — executed locally after the filing, whole tree, quarantine empty (`MatchEngine.Tests` 436/0/10; the full verdict line is in `CHANGELOG.md`). Prior entry below.)
**Updated (prior):** August 7, 2026, latest same day (v1.77 — **ERR-008-023's downstream measured on main: two acceptance bands rebaselined by owner call; no new ERR.** CI run 419 (main merge `9b8a7b4`, the first run of the full -021/-022/-023 chain on main) tripped two scenario bands, one of them invisible to every prior session (the 5,000-line CI log-tail cap hides `sim_match_engine_close_chance`, which prints early — the PR #303 run actually had 3 failures, not the 2 its session could see). Measured by local reproduction (the Ubuntu-archive `dotnet-sdk-8.0` runs the full gate in Claude remote sessions — `tools/dotnet-ci/README.md` v1.2; verdicts matched run 419 exactly): `sim_match_engine_keeper_contact` `no-deep-dive-early-miss` — one crossed episode 616.7 ms early, INSIDE the pre-fix 456–2000 ms class, band `== 0` → `<= 1` rather than widening the ms bound past the episode; `sim_match_engine_close_chance` cosine — pooled −0.119, one seed's entire ERR-008-018 gain returned (+0.078 / −0.232 per seed), bound −0.10 → −0.16, still refusing the pre-fix ≈ −0.29. Both recorded on the -023 row and queued for the KD-W1 calibration pass — the P5 residuals the -021/-022 record already carries are the suspects. Test-only; no spec text was the defect, so no ERR id is allocated. Prior update below.)
**Updated (prior):** August 7, 2026 (v1.76 — **ERR-008-023 filed + RESOLVED: the ERR-008-022 landing scored ZERO GOALS, and the acceptance scenario caught it.** The first gate run to reach `MatchEngine.Tests` (CI run `31188688249`, PR #303, head `a2987be`) failed `sim_match_engine_shot_outcomes` on `goals-still-scored = 0` — four seeds x 18 minutes, 72 minutes of football, no goal. Cause: -022's headline fix. The old goal-centre-plane bound discarded a keeper standing on his line for **every** shooter position, so the keeper-only `GK_BLOCKER_RADIUS_M` = 1.5 m disc — in the catalogue since the model was written — had never been exercised. It went live at -022 and removed **~42% of the goal arc on every shot** (1.000 → 0.584 at 16 m from the keeper alone, before any outfield defender), which `MIN_GOAL_VISIBILITY` then converts into SHOOT options that are never generated and `RiskPenalty_SHOOT` roughly doubles for those that survive. Fixed by retiring the disc: every blocker occludes with `BLOCKER_RADIUS_M`, the keeper included, because a keeper's reach beyond his body is **shot-stopping** — which P3 assigns to Goalkeeper Mechanics #11, and which #11 already prices at contact. `gkness` survives, lerping the P3 ability exemption alone. **This is the P5 residual recorded as 'not fixed' at the -022 landing under KD-W1, arriving with interest:** -022 strictly ADDS blockers to the count and was landed with no recalibration, one landing after the claim that -021 was population-preserving had itself been withdrawn. Also corrects a claim made in this session: run 402's sweep did **not** 'run to completion' — suites run in parallel and `MatchEngine.Tests` takes 22 m 55 s, so a job cancelled 3 minutes into testing never came near it. The match engine had never been exercised on this branch at all. Prior entry below.)

**Updated (prior):** August 6, 2026, latest same day (v1.75 — **the "GATE-VERIFIED" status v1.74 attached to ERR-008-021 / ERR-008-022 is WITHDRAWN: CI run 402's gate job never returned a verdict.** Read back from the Actions API after PR #302 was closed, the run is far more degraded than its console output suggested. The `Compile + test` job was externally **CANCELLED at 16:59:45** — 2 m 07 s after its last suite reported, and before `tools/dotnet-ci/run-gate.sh` could reach its closing `── Gate PASSED ──` line — so the script never evaluated its own exit condition. Four sibling jobs (**Markdown link check, Spec hygiene checks, File manifest sanity, Unity .meta integrity**) were cancelled at 17:05:28 carrying `runner_id: 0` and `started_at == created_at`: never assigned a runner, never executed. `Unity asset hygiene` failed inside `Set up job` on an action-download outage. Only **YAML lint**, **C# format check**, **Markdown lint** and `Unity license configured?` completed at all. What survives is narrower and still worth having: the build really did succeed with **0 errors**, and the test sweep really did run to completion — it reached `ui-framework`, last alphabetically, at 16:57:38 — so `DecisionTree.Tests` **127 / 1 / 4** and the other suites' green results are genuine measurements. What is not supported is the word *verified*: no gate verdict was emitted, four of this repo's hygiene checks have still never run against this work, and the far-post correction in `0612bcc` has never been compiled at all. Same shape as the three claims the two rows below already retract — a verification asserted ahead of the thing that would establish it, this time by me, one commit after writing that sentence. Prior entry below.)

**Updated (prior):** August 6, 2026, latest same day (v1.74 — **ERR-008-022's far-post lock was testing the NEAR post; the first real gate run caught it.** CI run 402 (PR #302, head `301c634`) compiled and executed this work for the first time. Build **0 errors**; `DecisionTree.Tests` **127 passed / 1 failed / 4 skipped**, every other suite green. The one failure — `ShotLane_FarPostBlocker_OccludesTheGoal`, expected 0.782157, got **0.728880** — was the test, not the model: it read `ctx.OpponentGoalPostL`, which in the home fixture is y = **30.34**, the post *nearer* a shooter at (90, 24). The pre-fix goal-centre-plane bound **kept** the near post and discarded only the far one, so the lock named for this entry's headline finding **would have passed against the broken model** — it was never a lock on the fix. Now selected by geometry (`FarPostFrom`) rather than by the `PostL`/`PostR` label, which carries opposite sides in this file's two fixtures. Expected value unchanged — and *not* compiler-confirmed, as this entry claimed: the run executed the old test and returned the near post's 0.728880, so nothing has ever evaluated 0.782157 (corrected at v1.75). The recorded **12 of 12** mutant kill accordingly overstates the far-bound mutant: the Python harness killed it, the committed test did not. Third hand-derived verification claim in the -021/-022 chain that execution falsified. Prior entry below.)

**Updated (prior):** August 6, 2026, latest same day (v1.73 — **ERR-008-022 filed + RESOLVED: the adversarial review over the ERR-008-021 landing.** #8 §3.1.4.3's shooting lane was bounded by a plane through the goal **CENTRE**, which for any off-centre shooter cuts diagonally across the goal mouth: it discarded the **far-post** blocker on **20,213 of 20,213** sampled in-range off-centre shooters, dropped a keeper standing on his line at goal centre for *every* shooter position (`proj == distToGoal` exactly — shooter (95,20) read a **completely open goal**), and admitted an opponent standing *behind* the goal line at the keeper's radius. ERR-008-021's overlap model was being denied much of the geometry it exists to price, so that landing achieved substantially less than it claimed. Two further hard predicates in the same derivation were **larger cliffs than the one -021 removed**: `GOAL_MIN_SHOT_DIST` stepped `GoalOpeningScore` 1.000 → 0.050 across 1 cm (and, 0.050 being below `MIN_GOAL_VISIBILITY`, deleted the SHOOT option with it), and the goalkeeper predicate stepped it 0.768 → 0.311 across 2 cm — which -021 had *widened* to 0.551, three lines from the code it rewrote, unrecorded. All three fixed: goal-line-plane bound + two new `[GT]` ramp widths (`SHOT_BLOCKER_NEAR_FADE_M` 1.0 m, `GK_PROXIMITY_FADE_M` 2.0 m), with `gkness` lerping the radius and the P3 exemption together. **Three -021 verification claims corrected as false:** the P5 exactness argument (holds only for `h ≤ halfArc`; up to **2×** above it — the stated reason no recalibration was needed, withdrawn), the test count ("9 locks / 5 of 8" → **10** / 9 evaluable / 5 fail / 4 pass), and the §3.2.3.2 worked example (its opponent sat 4.5 m from the goal line ⇒ classified a **goalkeeper** ⇒ exempt from the very ability term it demonstrated; all three numbers unreachable). **The suite was inadequate too** — the over-blocking half had no lock, a mutant restoring the pre-fix full width passed all ten, 8 of 12 mutants survived, and `NullAttributeView` was a **tautology** in both the pass and shot suites. Suite 10 → **15**. **Gate NOT run — no .NET SDK in the authoring environment.** Prior entry below.)

**Updated (prior):** August 5, 2026, latest same day (v1.72 — **ERR-008-021 filed + RESOLVED: the third fix under the football-judgment proxy review's remediation doctrine, and the discharge of the deferral ERR-008-020 opened.** #8 §3.1.4.3/§3.2.3.2's shot lane carried the SAME two defects the pass lane did, which is why §6.4 named it as the follow-up. **(a) A containment cliff:** an opponent contributed his *whole* angular blocking width when his angular centre fell inside the goal arc and **exactly nothing** when it fell outside — so a defender standing squarely across the near post scored a **fully open goal**, one a centimetre the other side scored a width half of which lay behind the post, and 4 cm of lateral position stepped `GoalOpeningScore` by **0.41** on the fixture the suite now uses (0.595 → 1.000). **(b) Attribute blindness:** the width was body radius alone, so a defender who neither reads the shot nor gets his body into its line shut the goal off exactly as hard as one who does. Found by review, not measurement — a structural property of the formula, read from spec + code. Fixed per doctrine: **P1** — the contribution is now the true angular OVERLAP of the blocking disc with the goal arc, which is continuous *by construction* (no ramp constant, no tolerance epsilon) and is also the geometrically honest answer, so the over- and under-blocking go with the cliff; **P2** — the overlap is scaled by the blocker's Anticipation/Positioning ability (`SHOT_BLOCKER_ABILITY_MIN/MAX` 0.6–1.4 `[GT]`, league-average exactly 1.0) read through the SHOOTER's Vision fidelity, reusing §3.1.3.3's floor as ONE dial because fidelity belongs to the assessor, not to what he assesses; **P3** — the **goalkeeper is exempt from the ability term** and occludes on geometry alone, because #11 §3.5/§3.7.0 owns keeper shot-stopping and pricing it here too would charge the shooter twice for one keeper. **P5 holds exactly, not approximately:** over a uniformly-placed blocker the old rectangle and the new trapezoid both integrate to `4h·halfArc`, for every disc width and every arc — so the fix redistributes occlusion from a step to a slope without opening or closing the goal on average, and the ability midpoint leaves the attribute axis neutral too. **Digest invariance is NOT claimed** — the model is live on every shot the engine generates and moves on any blocker who is not exactly average or not wholly inside the arc; the behaviour change is the point. No schema / RNG / domain-tag / draw-site / draw-order change. **10** new `OptionGeneratorTests` locks incl. the GK-exemption proof and the away-side mirror; a reference implementation of both models confirms **5 of the 9 evaluable against the old model fail on it** (the four that pass pre-fix are the two P5 pivot rows, null-view neutrality and the GK exemption, by construction). *(Counts corrected at ERR-008-022, which also found the null-view lock tautological and the over-blocking half unlocked.)* **Gate NOT run — no .NET SDK in the authoring environment; nothing in this landing has been compiled or executed.** Prior entry below.)
**Updated (prior):** August 6, 2026, later same day (v1.71 — **ERR-030-026 filed at the #29/#41 T2 adversarial review (pass 5).** #30 §3.3's KD-2 tick order pins nine day-slots and has **no slot for playing the round**, because a round is a separate command — so where a fixture sits relative to slot 2 (#29) and slot 4 (#41) is specified nowhere, and in the code it falls out of `AdvanceToNextFixtureDay`'s loop condition, which stops on *reaching* the fixture day. The emergent order is **play the round, then process matchday**: right for #41's occurrence draw, wrong for the recovery countdown sharing the same atomic step, so **every injury runs one matchday longer than its tier**. Inert today (the dial is off) and invisible to the suites, which is exactly why it needed filing — the balance pass would otherwise fit the recovery tiers straight through an unstated convention. **Resolved as: convention adopted, documented at all three determining sites, and locked by `DayAdvance_StopsBeforeTheFixtureDaysOwnSteps`;** whether #41 should split recovery from occurrence so each lands on the right side of the round is deferred to that pass with owner sign-off. No FR text change, no format bump, no behaviour change. **NO GATE RUN.** Prior entry below.)

**Updated (prior):** August 6, 2026, later same day (v1.70 — **ERR-029-006 and ERR-041-010 filed at #29/#41 T2**, and both are the same finding in the two sibling specs: the T2 seam text names #28 APIs and types that `TacticalDirector.PlayerProgression` does not expose. #29 §3.5/§4.3 route the growth input through a **batch** `#28.AdvanceDay(worldDay, in trainingInputs)`; #28's only daily entry point is the per-player `GrowthProjection.AdvanceDayForPlayer`, and #28's own slot-1 wiring (roadmap D1) has not landed, so there is nothing to hand a batch to. Both FR-TR-025 and FR-MD-025 specify the roster handoff against `RegenResult` / `RetirementResult`, two more types #28 does not define. The same class as ERR-041-002 and ERR-030-012, and found the same way — by trying to call it. **Resolution splits.** The handoff half is resolved in substance: `PlayerCareerStates.SyncToRoster` reconciles both state sets against the roster #30 already holds, inserting via `Create` and dropping departures, keyed by `PlayerId` at the season boundary — the same contract over state that exists, which starts inserting exactly the regens the moment #28 T2 produces them. The slot-1 half is **deliberately deferred to D1**: gathering a batch for a consumer with neither the API nor a call site is the phantom this project refuses, and `ComputeTrainingInput` returns `Neutral` on both branches anyway. ERR-041-010 additionally records **(b)**: §3.5 sources `MatchLoad` from "#30's fixture result", and #30 has no per-player appearance record — `MatchLoad.None` is passed, which is inert while the occurrence dial is off, and a real record needs a persisted home this landing does not invent. No FR text change, no format-version change, no `DETERMINISM_DIGEST_VERSION` bump. **NO GATE RUN.** Prior entry below.)
**Updated (prior):** August 7, 2026 (v1.69 — **ERR-008-021 + its AR-1 are now GATE-VERIFIED. No new ERR.** PR #305, CI run 404, head `3f207ee`: build succeeded **0 errors** (5 warnings — the known count), `DecisionTree.Tests` **120 passed / 0 failed / 4 skipped / 124 total** — the 7 `ShotLane_*` locks (incl. the H-1 in-band-defender-is-weighted regression lock and the exact GK-arc pin) compiled and executed for the first time; whole-tree gate PASSED with the **quarantine empty**; `MatchEngine.Tests` **420/430 (10 skipped) unchanged**, so the digest-moving behaviour change tripped **no goal-rate band or tick-window scenario** — the blast-radius note in v1.67/v1.68 resolves as "checked by execution, nothing moved". This retires the **Gate NOT run** caveats on v1.67 and v1.68: every resolution claim in both had been written against never-compiled code, including the `gk_candidate` pre-pass and the `VisionFidelity` hoist. The authoring environment still has no .NET SDK; CI on push remains the only compiler for this work. Prior entry below.)

**Updated (prior):** August 6, 2026, latest (v1.68 — **ERR-008-021 AR-1: the same-day adversarial review over the shot-lane landing found 1 High, 7 Medium, 5 Low — all fixed.** The High (H-1): the landed P3 exemption keyed on the whole 6 m `GK_PROXIMITY_TO_GOAL` band rather than the goalkeeper, so EVERY near-goal defender escaped the new ability weighting — inert precisely where shots are blocked (for a 10 m shot, most of the usable path), and the landing's fixtures all sat 8 m off the goal line so none of its six locks registered it. Now a single **GK candidate** (goal-line-nearest in band, snapshot-order tie-break, independent of `IsInShotPath`) is exempt and every other blocker is weighted; radius stays per-band (the recorded Stage-0 limitation), so neutral arcs are unchanged. Notable Mediums: the P5 "today's arcs bit-for-bit" claim corrected to midpoint/null-view-only — the all-default 10/10 profile reads ≈ 0.979, the same overclaim shape retracted for ERR-008-019 one day earlier (M-2); margin-less discrimination locks that survive a collapsed `[GT]` band (M-3); vacuously-passable equality locks (M-4); the Vision-fidelity expression duplicated in both lanes on day one, hoisted (M-5); both away-mirror fixtures running a goal-post assignment production never builds (M-6); §3.2.3.2's Known-limitation paragraph stating the radius-misclassification direction backwards (M-7). Full list in the entry. Surfaces: `OptionGenerator.cs` v1.8, `OptionGeneratorTests.cs` v1.8 (7 locks now), `UtilityWeights.cs` v1.11 (doc), `section-3-1.md` v1.5, `section-3-2.md` v1.13. **Gate NOT run — no .NET SDK; CI on push is the gate.** Prior entry below.)

**Updated (prior):** August 6, 2026, later again (v1.67 — **ERR-008-021 filed + RESOLVED: the third fix under the football-judgment proxy review's remediation doctrine, closing the shot-lane follow-up deliberately deferred at the ERR-008-020 landing.** #8 §3.1.4.3/§3.2.3.2's goal-occlusion sum was attribute-blind — every outfield blocker occluded the same geometric arc whoever he was, and no shooter attribute entered the read — so a Pace/Anticipation 1/1 defender walled off the goal exactly as hard as a 20/20 one (pattern (a); already continuous in position, so P1 not in play). Fixed as §3.2.3.2 **step 3a**: each OUTFIELD blocker's arc × §3.1.3.3's `perceived_ability` (Anticipation/Pace → 0.6..1.4, read through the SHOOTER's Vision fidelity, doctrine P2) — **no new constants**, the ERR-008-020 `[GT]`s reused verbatim (one calibration lever, KD-W1); the GOALKEEPER's arc stays purely geometric (doctrine P3 — keeper quality is priced once, at the #11 save; `GK_BLOCKER_RADIUS` is an abstraction of coverage, not a body); league-average / null-view ability = 1.0 reproduces today's arcs exactly (P5 pivot). `OptionGenerator.cs` v1.7 + 6 `OptionGeneratorTests` locks incl. the away mirror and the GK-exclusion lock. Adjacent defect recorded-not-fixed: §3.2.3.2's numerical example is in a legacy centre-origin frame and its blocker classifies as GK under the section's own heuristic yet uses the outfield radius (annotated; the §3.2.3.3 chain consumes its 0.757). No schema / RNG / draw-order change; digests move where a generated SHOOT has a non-neutral outfield blocker in the path, as intended. **Gate NOT run — no .NET SDK in the authoring environment; CI on push is the gate.** Prior entry below.)

**Updated (prior):** August 6, 2026, later same day (v1.66 — **the four #29/#41 T1 entries below are now GATE-VERIFIED. No new ERR.** PR #300, CI run 397, head `9a7f703`: build succeeded 0 errors, `TrainingSystem.Tests` **52/52**, `InjuriesMedical.Tests` **66/66**, 0 skipped in either, `SeasonSave.Tests` **267 passed / 3 skipped / 270**, whole-tree gate PASSED with the quarantine empty. Nothing needed a fix to get green. This retires the **NO GATE RUN** caveat on v1.65 (ERR-029-005 / ERR-041-009) and v1.64 (ERR-029-004 / ERR-041-008) — every resolution claim in those two entries had been written against code that had never been compiled. **The one that most needed executing is ERR-029-005 / ERR-041-009's load-time half:** the `*_SAVE_MAGIC` gate was proven only by a byte-exact Python model of both formats, so until this run no compiler had ever seen a codec refuse its sibling's block, and the compile-time half — the typed `TrainingBlock` / `MedicalBlock` that makes the triggering transposition a build error — is by construction a claim *about* the compiler that no amount of authoring-side reasoning can establish. Both now hold by execution. ERR-041-008's `ClubId` write and ERR-029-004's pinned §4.4.1 layout are likewise exercised by the round-trip tests rather than only specified. The authoring environment still has no .NET SDK (installer still 403 at the agent proxy); CI on push remains the only compiler for this work. Prior entry below.)

**Updated (prior):** August 6, 2026 (v1.65 — **ERR-029-005 and ERR-041-009 filed + RESOLVED at the adversarial-review pass over the #29/#41 T1 landing.** One defect, two spec homes, and it exists *because* ERR-029-004 succeeded: pinning #29's layout to match #41's made the two blocks byte-for-byte the same shape, and every sub-blob format in the save stack — `TRAINING_SAVE_FORMAT_VERSION`, `MEDICAL_SAVE_FORMAT_VERSION`, `SEASON_STATE_FORMAT_VERSION`, `MATCH_SAVE_FORMAT_VERSION`, `PROGRESSION_SAVE_FORMAT_VERSION` — sits at version 1. A version gate therefore separates one *generation* of a format from the next and **never one format from another**, so each codec decoded the other's bytes cleanly, completely and silently: severity tiers arrived as training focuses, recovery counters as conditioning cursors, injury counts as training fatigue, every gate green and no trailing byte. Verified by executing a byte-exact model of both formats in **both** directions before the fix. `SeasonSaveCodec.Encode` took five consecutive `byte[]`, so the transposition that triggers it had no compile-time signal either. **Resolution is two-layered:** each block now writes a self-identifying `*_SAVE_MAGIC` first and refuses a foreign one on decode (the load-time gate, deliberately NOT an RNG domain tag — those name draw domains and must stay free to change independently), and the frame's two confusable parameters become the typed `TrainingBlock` / `MedicalBlock`, making the transposition a build error (the compile-time gate). The wider lesson is recorded in both §4.4 sections: **a format version is not a format identifier.** The same review pass also hardened `SeasonSaveManager.Save`, whose `trainingClubs`/`medicalClubs` defaulted to null-meaning-empty — at T2 a call site omitting them would have compiled, saved and loaded back empty arrays indistinguishable from an unwired game, silently dropping a season of conditioning and injury history; both are now required and reject null. No FR text change, no format-version bump (neither sub-blob format has ever been written to a real save), no `SEASON_SAVE_FORMAT_VERSION` bump — the *frame* layout is unchanged, only the contents of two blocks it treats as opaque. **NO GATE RUN** — still no .NET SDK in the authoring environment; CI on push is the gate. Prior entry below.)

**Updated (prior):** August 6, 2026 (v1.64 — **ERR-029-004 and ERR-041-008 filed + RESOLVED at #29/#41 T1**, the save-codec landing. Both are the same finding in the two sibling specs' persistence sections, and both were found by trying to *write* the format rather than by reading it. **ERR-029-004:** #29 §4.4 pinned the sub-blob's framing posture but never its byte layout — it said "opaque, independently version-gated" and stopped — while #41 §4.4 pinned its own. A format whose F3 refuses every cross-version migration and whose fields are nowhere written down is one two implementers can only agree on by accident. §4.4.1 now pins it. **ERR-041-008:** #41 §4.4's layout groups the blocks by club without ever *naming* one, so club identity would be carried across a save boundary by list order alone — an implicit agreement with a sibling sub-blob the same section's KD-7 forbids this codec to read. `WriteI32(club.ClubId)` added to spec and code together, and #29's new §4.4.1 carries it from the start. Both entries also correct their spec's §2.3 **F3** row, which named `ArgumentException` while citing the `MatchSaveCodec` posture — and `MatchSaveCodec` throws `InvalidOperationException`, so the row contradicted itself. **Id note:** ERR-041-**008** rather than 004, because `injury-aging-research-alignment-design.md` soft-reserves 004–007 for its own pending back-props; the gap is deliberate. No FR text change; `SEASON_SAVE_FORMAT_VERSION` 2 → 3 (a frame change the design already required, not a consequence of either entry); no `DETERMINISM_DIGEST_VERSION` bump. **NO GATE RUN** — the authoring environment still has no .NET SDK, so both codecs and their 40 tests are written and unexecuted; CI on push is the gate. Prior entry below.)

**Updated (prior):** August 5, 2026, even later same day (v1.63 — **ERR-008-019 invariance claim corrected (adversarial review over the landing): "no digest moves on any seed" is RETRACTED for the full-range form.** Documentation only — no formula, constant, test or behaviour changes; the code and the four test locks stand. The recorded argument was false-premised: it placed the shooter inside Ball Physics #1 §3.1.11.1 `CheckPossession`'s 0.5 m `ControlRadius`, but that is not how this engine grants possession. The production paths are `MatchEngine.RunLooseBallPickup` (§5.Z Phase H, KD-H3), which grants possession to the nearest eligible agent within `MatchEngineConstants.LooseBallPickupRadiusM` = **1.0 m** of a loose ball at rest and **leaves the ball where it lies**, and the first-touch path (`FIRST_TOUCH_ACCEPTANCE_RADIUS_M` = 1.0 m); after the grant **nothing re-anchors the ball to the holder or releases possession on separation** — the holder moves freely under dispatched `MoveTo` commands and the executors' only entry check is the possession id (`PassExecutor` FM-01 `IsBallPossessedBy`). So holder–ball separation at a decision tick reaches 1.0 m, and a MIDFIELD ball at x → 70⁻ with the holder goal-side puts the shooter just above **34.0 m** — **inside** raw 19's §3.1.4.2 range gate (20 + (18/19) × 15 = 34.21 m), where the full-range ramp gives 0.05 + (18/19) × 0.5 ≈ **0.524** against the old step's 0.55. A generated option can score differently, so invariance is **not established** and is likely false on seeds realizing that state; the behaviour change is owner-intended, not an accident. The **superseded** v1.61 narrow-ramp argument survives the corrected premise (it differs from the step only at A_LongShots ≤ 0.6, range gate capping at 29.0 m — still disjoint from > 34.0 m) and is left as historical chain. Also recorded (Low): `LONG_SHOT_RAMP_HALF_WIDTH`'s documented (0, 0.25] range is the FORMULA's validity domain, not a free dial — `ShootMidfield_FullRangeRamp_EndpointsExact_AndStrictlyMonotone` fails at any half-width below 0.25, so a retune downward must revisit that lock. Surfaces corrected: #8 §3.2.3.1 + `section-3-2.md` v1.11, this log's index row and ERR-008-019 entry, `football-judgment-proxy-review.md`, `open-issues.md`, `CLAUDE.md`; `UtilityWeights.cs` v1.10 XML doc. **Gate NOT run — no .NET SDK in the authoring environment.** Prior entry below.)

**Updated (prior):** August 5, 2026, later same day (v1.62 — **ERR-008-019 owner revision: the long-shot ramp widened to the FULL attribute range.** The owner directed the scaling to span the whole LongShots range rather than the initial 8–13 band; since the metres-based §3.1.4.2 range gate already scales raw 1–20, the instruction lands on the §3.2.3.1 zone-modifier ramp. One-value change — `LONG_SHOT_RAMP_HALF_WIDTH` 0.05 → 0.25, its maximum valid value: the ramp spans the whole shifted domain, `t` reduces to `A_LongShots`, raw 1 is exactly 0.05 and raw 20 exactly 0.55, and every raw point between moves the modifier ≈ 0.026 — no plateau anywhere. P5 still holds (midpoint at the old cliff; uniform-population mean 0.30 under step, narrow ramp, and full ramp alike). Digest invariance survives in tighter form: only raw 20 can generate a MIDFIELD SHOOT (35.0 m range vs ≥ ~34.5 m needed; raw 19 caps at 34.2 m) and there the ramp equals the old step — no digest moves. Spec §3.2.3.1/§3.2.3.4 re-derived, Case B recomputed 0.200 → 0.162, `section-3-2.md` v1.10; tests refitted (shifted-form lock at raw 10; endpoints-exact + strictly-monotone replaces the plateau assertions, which were the exact opposite of the instruction). **Gate NOT run — no .NET SDK in the authoring environment.** Prior entry below.)

**Updated (prior):** August 5, 2026 (v1.61 — **ERR-008-019 filed + RESOLVED: the second fix under the football-judgment proxy review's remediation doctrine, and the closing of the review's founding finding.** #8 §3.2.3.1's midfield `ZoneModifier_SHOOT` was a hard step on shifted LongShots — 0.55 strictly above `LONG_SHOT_THRESHOLD`, 0.05 at or below it, an **11× jump across one raw attribute point** — the original pattern-(b) instance the whole review was named after, whose prior "FIXED … gate green" record was verified false at the ERR-008-020 landing (no log entry, cliff live, no branch carrying a fix); the id was soft-reserved there and re-verified free at this landing as required. Fixed per doctrine P1/P5: a linear ramp in the unchanged shifted form, centred on the old threshold with new `[GT] LONG_SHOT_RAMP_HALF_WIDTH` = 0.05 — full suppression at raw ≤ 8, full long-shot modifier at raw ≥ 13, the exact SHORT/LONG midpoint at the old cliff, so endpoints and the population-integrated modifier reproduce the old behaviour (the ERR-008-020 centred-ramp precedent, locked by test). P2/P3 deliberately out of scope: long-shot inclination is the shooter's own execution capability, not a recognition judgment — no fidelity term, no new attribute, no double-count. **The branch is production-unreachable in the only band the fix changes** (the ramp differs from the old step only at A_LongShots ≤ 0.6, whose §3.1.4.2 range gate caps at 29.0 m, while a generator-reachable MIDFIELD SHOOT needs ≥ ~34.5 m of range — disjoint bands, so no generated option ever scores differently; ERR-008-017's stale "≥ 40 m" reachability figure — pre-ERR-008-016 zone geometry — corrected in passing), so the cliff was latent and **no digest moves on any seed** — landed anyway per the standing wrong-shaped-model posture; the ramp goes live if the range gate or zone geometry ever changes. §3.2.3.4 item 2 re-derived as the ramp bands; Case B unchanged (past the ramp end). 4 new `UtilityScorerTests` locks + the AR-2 M-4 lock refitted raw 12 → raw 14 (mid-ramp now; raw 14 still discriminates shifted vs raw form). **Gate NOT run — no .NET SDK in the authoring environment.** Prior entry below.)

**Updated (prior):** August 5, 2026 (v1.60 — **ERR-041-001, ERR-041-002 and ERR-041-003 are now EXECUTION-VERIFIED.** All three were filed and resolved against code that had never been compiled; PR #299's gate run (CI 394, head `ddbbe58`) compiled both assemblies for the first time and ran their suites: `TrainingSystem.Tests` 27/27, `InjuriesMedical.Tests` 40/40, 0 skipped in either, whole-tree gate PASSED with an empty quarantine, and **no fix was needed to reach green**. That matters most for **ERR-041-002**, whose resolution replaced #41 §3.1's non-existent `rng.DrawKeyed` call with a local keyed SplitMix64 derivation: the draw-separation locks (adjacent player ids, adjacent world days, adjacent seeds) had never been executed, and the whole KD-1 position-independence argument rested on them. They pass. **ERR-041-001**'s `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` is verified distinct from all 15 other allocations, and **ERR-041-003**'s `[CROSS]` mirror compiles as a mirror rather than a second config key. Also now executed: #41 §3.6's worked example term by term, #29 Appendix B day by day, and the AR-5 occurrence-probability baseline (231/0/431 per-mille) whose literals had been hand-derived in Python against an unbuildable tree — the compiler agrees, so the recorded balance-pass numbers are real. No FR text change, no format-version change, no new entry. Prior entry below.)

**Updated (prior):** August 5, 2026 (v1.59 — **ERR-041-003 filed + RESOLVED at #41 T0's adversarial review**, and it is the third entry in this log found by *reviewing* code rather than by writing it. #41 Appendix A tags `INJURY_RISK_MAX` `[GT]` — an independently tunable value with its own config key — while §3.4 requires it to be the *same scale* as #29's `RiskScore` and derives the draw denominator from it. Both cannot hold, and the T0 landing implemented both rows literally: two config keys (`[training-system]` and `[injuries-medical]`) for one contract value, guarded by an equality test that was **vacuous under the only conditions it runs in** (the gate leaves `GameplayConfigHolder` unbound, so both sides return their fallback and the assertion passes whatever a config says). Resolved by re-tagging #41's row `[CROSS]` and mirroring #29's — the ERR-037-001 posture. **Recorded, not fixed:** the two specs mitigate on the same three physical attributes, so robustness is priced in twice across the layers and #29's maximum risk never means certain occurrence at #41; pinned as an explicit test assertion for the balance pass. The same review pass fixed two code defects that needed no ERR because no spec text was wrong — a `MedicalModifier` gate that caught only zero and not negative multipliers (a negative one silently disables injuries or one-days a Serious injury), and an F1 coherence check that structurally could not see a negative `RecoveryRemaining` — plus four tests that could not fail. Prior entry below.)

**Updated (prior):** August 5, 2026 (v1.58 — **ERR-041-002 filed + RESOLVED, and ERR-041-001 closed, at Injuries & Medical #41 T0** (roadmap D3, landed alongside its declared prerequisite #29 T0). ERR-041-002 is the same class as ERR-030-012 and reached the same answer from the same constraint, independently: **#41 §2.2/§3.1 call `rng.DrawKeyed(stream, entityId, actionOrdinal, drawIndex)` on `DeterministicRngService`, and that method does not exist.** #16 exposes only the branch-safe reservation trio, whose draw value is keyed on an `ActionOrdinal` the service increments inside `Reserve` — no overload takes a caller-supplied ordinal. The only implementable shape against today's API is cursor-positioned, which KD-1 of the same spec forbids: FR-MD-007 serializes *no* cursor precisely because every draw must be reproducible from `(playerId, worldDay, purpose)` alone. Resolved by realizing the draw as a local keyed SplitMix64 derivation (`MedicalStep.DrawOccurrence`) — the `RoundResolutionModel.FixtureKey` / `LeagueBootstrap` precedent — so `AdvanceMedicalDay` takes `ulong worldSeed` instead of the service, and no stream is registered. ERR-041-001 closes with it: `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` lands in code at that draw site, while `SubsystemOrdinals.InjuriesMedical = 92` is deliberately **not** allocated — an ordinal with no registered stream behind it is the zero-consumer phantom FR-LW-031 forbids. **No gate run: the authoring environment has no .NET SDK and the network policy blocks the installer**, so both entries' locks are written and unexecuted. No FR text change, no format-version change, no `DETERMINISM_DIGEST_VERSION` bump. Prior entry below.)

**Updated (prior):** August 4, 2026 (v1.57 — **ERR-008-020 filed + RESOLVED: the first fix landed under the football-judgment proxy review's remediation doctrine** (`football-judgment-proxy-review.md` §6.4 — the owner-selected template for the review's 33 open findings). #8 §3.1.3.3's `is_interceptor` was a binary 0.8 m corridor — 2 cm of defender position stepped `PassLaneScore` by 0.33, and no defender attribute entered the judgment anywhere, verified against `OptionGenerator.CountInterceptors`: a Pace/Anticipation 1/1 defender priced a pass lane identically to a 20/20 one in the identical spot, and the lane score both prices PASS candidates and gates their existence. Found by review, not measurement — a structural property of the formula. Fixed as the doctrine prescribes: per-opponent `weight = falloff × perceived_ability` — linear falloff, core 0.4 m to zero at 1.2 m, **ramp centred on the old cliff so integrated threat is preserved and every neutral row of the old verification table reproduces exactly** (P5 pivot, locked by test); defender Anticipation+Pace → 0.6..1.4 with the league-average defender at exactly 1.0; and the passer's **Vision as discrimination fidelity** (P2) — `perceived = 1 + fidelity × (true − 1)`, floor 0.2, so a Vision-1 passer reads everyone as near-average, which IS the pre-fix engine; §3.2.2's Vision term untouched (no double-count, P3). Plumbing: `DecisionTree.SetAllAgentAttributes` boot seam (the `SetMatchSeed` pattern) carries the orchestrator's live `_dtAttrs` reference into `DecisionContext`; null view ⇒ ability-neutral. Shot lane §3.1.4.3 deliberately deferred (owner call). No schema / RNG / domain-tag / draw-site / draw-order change. 6 `OptionGeneratorTests` locks incl. the away-side mirror. **Gate NOT run — no .NET SDK in this environment; nothing compiled or executed.** **Housekeeping:** the review file's claim that ERR-008-019 (long-shot cliff) was "FIXED, gate green" is **false against both branches** — no log entry, cliff still live in code and spec; corrected in the review file this commit, id soft-reserved. The two v1.56 header entries below are relabelled `(prior)` per the chain convention. Prior update below.)


**Updated (prior):** August 4, 2026 (v1.56 — **ERR-008-018 filed + RESOLVED at the close-chance-creation pass** (§5.Z.24). #8 §3.1.5.2 picks a dribble direction by FREE SPACE alone and closes by delegating the correction — *"the scoring stage (§3.2.2) applies directional-to-goal modifiers to the DRIBBLE utility"* — but §3.2.4.1, DRIBBLE's actual formula, has no such factor, and §3.2.2 is the **PASS** formula, so the promised term was delegated to a section that does not own DRIBBLE and never had a home. Measured over six full matches: in the attacking third DRIBBLE is the modal carrier action at **40%** of decisions with a mean cosine to the opponent goal of **−0.302** and only 31% pointing goalward — the average final-third dribble points AWAY from the goal, and the utility was identical either way. Same class as ERR-008-017. Fixed with `DirectionQuality_DRIBBLE` in §3.2.4.1 (the §3.1.3.5 PASS shape), the cross-reference corrected, worked examples recomputed and a Case A′ added; the zero-`BestDirection` degenerate input resolves to the exact ×1.0 identity (KD-V3 restated), so all 22 pre-existing `UtilityScorerTests` are bitwise unchanged. Measured: cosine **−0.302 → +0.006**, goalward **31% → 49%**, moving on **all six seeds with no overlap** between pre- and post-fix distributions. The `[GT]` floor lands at **0.80** rather than the PASS floor of 0.50 because suppressing the dribble pushes the carrier onto HOLD, which has no timeout: at floors 0.65 and 0.50 one seed in six stalled (mean final-third episode 5.1 s → 17.5 s / 28.6 s). **The creation funnel itself did NOT move and is not claimed** (box occupancy 0.11 → 0.10, ball into box 6% → 5%, passes into box 1% → 0%) — the owner doc re-localizes it to #8 §3.1.3 generating PASS candidates only at a teammate's CURRENT POSITION, so the tree cannot pass to a place, only to a player. No schema/RNG/domain-tag/draw-site/draw-order change. Acceptance `match-engine-close-chance` — **2 of 3 predicates fail pre-fix, verified by execution at `7fcd897`**. Prior entry below.)

**Updated (prior):** August 4, 2026 (v1.56 — **ERR-011-009 + ERR-011-010 filed + RESOLVED at wiring backlog W1**, and they are the first entry in this log found by *wiring* rather than by measurement. `GoalkeeperMechanics.CommitRushIntent` had **zero production callers** since it was written, so every one-on-one this engine has ever played was a stationary keeper on his line; W1 gives it one. Reading the `Rushing` exits before switching the trigger on surfaced the defect: #11 §3.1.1 gives `Rushing` three exits and `OneOnOne` two, and for a LOOSE ball **none of them can fire** — the 1v1 and smother triggers are false by construction with no ball possessor, F-08 needs one, and §3.7.2's update converges on the locked target and stops. A keeper who swept a loose ball would have stood over it in `Rushing` for the rest of the match. Everything else anticipated the completion (`RushPhase.Reached` has been in the enum since v0.1 and was never emitted; §3.7.3 reserves `AbortReason.AttackerBeatGK`) — only the table that adjudicates state had no row. Fixed with two §3.1.1 rows + the §3.7.2 terminating check + `[GT] RUSH_TARGET_REACHED_RADIUS_M`; a **completion, not an abort**, so FR-GK-018 / KD-15 are untouched and it ranks below contact, F-08 and the 1v1 trigger. No schema / RNG / domain-tag / draw-site / draw-order change. **Deliberately unlike every entry above it, this one carries NO measured numbers**: the authoring environment has no .NET SDK and the agent proxy denies the installer, so neither the gate nor the new `GkRushDiagnosticTests` instrument was run — see `gk-rush-trigger-design.md` §6. Under KD-W1 the eleven new `[GT]`s are new dials for a dead surface, not retunes, and every one is un-calibrated. **ERR-011-010** is the deeper of the two and explains why the surface was dead for ten weeks: §3.7's state entry delegated the rush DECISION to Decision Tree #8, which has no goalkeeper model and structurally cannot acquire one, so the condition belonged to nobody. Because the "when" was delegated, the spec also never said what a keeper is *deciding* — and the first implementation of this trigger guessed wrong, refusing to send him whenever any team-mate was nearer the ball. That is not the model: **a keeper comes out to reduce the shooting angle**, and a defender chasing the carrier reduces nothing, so a last-man rule keeps him home in exactly the situation he exists for. New §3.7.0 takes the decision back (the §3.3.6 move) and is normative on both halves — only a GOAL-SIDE body in the shot corridor is cover, and how far out he comes is his own `OneVsOne` / `Composure` / fatigue. Prior update below.)

**Updated (prior):** August 3, 2026 (v1.55 — **ERR-011-008 filed + RESOLVED at the conversion-at-contact pass** (§5.Z.23), and it is the seventh consecutive realism pass whose brief's premise did not survive measurement. `gk-contact-rate-design.md` §7 item 1 recorded the residual as the Stage-0 `pointQuality` lottery and parry placement; the new per-contact instrument found that **the parries and spills work and the CATCH does not**. Ball speed the tick before a contact vs at the end of it: parried **10.8 → 0.0**, deflected 10.3 → 4.2, spilled 13.9 → 9.0 — and **caught 11.1 → 10.8**, one tick of drag, because §3.5.2's catch branch is TWO statements (the possession record AND `ball.velocity = gkHandVelocity`) and only the first was implemented. §3.5's **Outputs** summary is the contributing spec defect: it named `Ball.SetPossessor` alone for the catch, and `IGoalkeeperBallSystem` exposed no park seam, so the omission was invisible from the interface. Possession in this engine is a flag, not a kinematic constraint — the ball integrates unconditionally and the goal check adjudicates on POSITION — so **7 of 10 catches were followed by a goal within 5 s**, with 14 of 15 goals following a keeper contact within 10 s. Fixed with `ParkBall()` at both claim sites (catch + Stage-0 smother); §3.5.2's pseudocode body unchanged, because it was right. No schema/RNG/domain-tag/draw-site/draw-order change. Measured (3 full matches, same seeds): goals **15 → 11** over the corpus (5.0 → **3.7**/match — the closest this engine has measured to football's ~2.7), scorelines 2-2/2-0/6-3 → 1-0/2-2/4-2. Acceptance `match-engine-keeper-claim`: 2 of 3 predicates fail pre-fix, verified by execution (6 of 6 claims left the ball travelling; 5 of 6 held balls entered the keeper's own net). The `pointQuality` lottery is measured in detail and recorded NOT fixed — a probe of the geometry-aware form is reported in the owner doc §7. Prior update below.)
**Updated (prior):** August 2, 2026 (v1.54 — **ERR-020-002 + ERR-020-003 filed, both OPEN, Code Standards #20 §3.5.2-owned.** Found while splitting `src/CLAUDE.md`, which reproduces the layer taxonomy: §3.5.2 places **19 of the 31 assemblies now in `src/`**, leaving the composition root, the management layer, the data layer, `tactical-instructions` and all four client assemblies outside the layer order — so FR-CS-046 decides nothing about ~39% of the tree, including every reference into or out of `match-engine`. ERR-020-002 proposes a **ten-tier order covering all 31 folders**, derived from the `.asmdef` reference graph rather than folder names and verified against it: **zero upward references**, 29 intra-tier references all pre-existing and acyclic — so adopting it changes nothing that exists and constrains only future code. It also retires the stale empty `UI (Stage 1+ — not specified yet)` row and strikes the `code-standards` phantom from `src/CLAUDE.md`'s infrastructure table. **Spec #20 is deliberately untouched:** layer membership is its authority and wants owner sign-off, and a wrong answer in the authority file is worse than a documented gap — the ⚠️ note in `src/CLAUDE.md` names the gap meanwhile. ERR-020-003 is the notation defect found by the same verification: §3.5.2 draws `Physics ──► Mechanics ──► AI` while the root `CLAUDE.md` states `AI → Mechanics → Physics, never the reverse` — same rule, opposite arrows, neither labelled. Code follows the `CLAUDE.md` reading; no violation exists. Prior update below.)
**Updated (prior):** July 28, 2026 (v1.53 — **ERR-011-007 + ERR-012-010 filed + RESOLVED at the gk-contact-rate pass** (§5.Z.20 §7.1's residual — the keeper met ~a quarter of on-target shots and the uncontacted remainder held nearly all the surplus goals). Measured per episode at the ball's goal-plane crossing (new `GkContactRateDiagnosticTests`): of 15 crossed un-contacted threat episodes, **9 were dive-early** (the unconditional `Anticipate → Diving` row launched the dive at the first 10 Hz tick after SAVE committed, so the 600 ms envelope closed 456–2000 ms before the ball arrived; dive-late exactly 0), 3 no-dive, 3 lateral-miss — with the lateral need at the crossing (1.91–3.83 m) at or beyond the dive's ~3.55 m total coverage because #12 §3.3.3's pitch-anchored `GK_LATERAL_FACTOR × basisY` lateral term moved the keeper at most ±2 m over the whole 68 m width. **ERR-011-007**: new #11 §3.3.6 commit-to-arrival timing — the transition gates on predicted time-to-plane against a lateral-need-scaled commit lead (`[GT] DIVE_COMMIT_MIN_LEAD_FRAC`), sharing ONE crossing predictor with the §3.3.4 dive direction; §3.2.3's `elapsed` anchor refined to the keeper's first decision opportunity at/after the live stamp — `max(AttemptCommittedTick × 100 ms, the first tactical tick after the stamp)` — so neither the held launch (scored as sluggish) nor a shot struck after the commit (scored as seconds-early) re-clamps the window; the second ordering is COMMON under the hold and was found by the first full-corpus run. **ERR-012-010**: #12 §3.3.3's lateral term becomes the ball-line point clamped inside the goal mouth (`GK_LATERAL_CLAMP_M` replaces `GK_LATERAL_FACTOR`, retired not retuned — no value of a pitch-anchored gain expresses goal-anchored tracking). No schema/RNG/draw-order change (both mechanisms are pure functions of current tick state). Measured effect in the owner doc `gk-contact-rate-design.md` §6. Prior update below.)
**Updated (prior):** July 28, 2026 (v1.52 — **ERR-008-017 filed + RESOLVED at the shot-volume pass.** #8 §3.2.3.1's U_SHOOT had NO distance term while `GoalOpeningScore` is scale-free and the §3.1.4.2 range gate is a cliff — within range a 34 m shot scored identically to a 10 m one, and measured shots clustered AT the range-gate boundary (means 30–34 m vs football's ~17; ~60% beyond 22 m). The formula gains a `DistanceQuality_SHOOT` hyperbolic-decay term (1.0 inside `[GT] SHOOT_SWEET_RANGE_M`, so every close-range calibration is bitwise untouched); the midfield long-shot machinery is recorded as production-unreachable dead surface (zone minimum 40 m vs range-gate maximum 35 m). No schema/RNG/draw-order change. Locked by the `match-engine-shot-speed` scenario's mean-shot-distance predicate — fails pre-fix at 30.0 vs 24.0, verified by execution. Prior update below.)
**Updated (prior):** July 28, 2026 (v1.51 — **ERR-011-005 + ERR-011-006 filed + RESOLVED at the gk-catch-parry-conversion pass** (§5.Z.19's residual lever (c)). The §3.2.3 reaction window — 30% of the §3.5.1 handling-quality blend — was re-evaluated per frame, so the value consumed at contact was dated by the ball's whole FLIGHT time (the spec's own §3.2.5 worked example anchors it at the dive COMMIT); and the detection stamp was never cleared, so most dives were dated against shots struck 34–349 seconds earlier, with rebound/deflection episodes having no anchor at all. Fixed: the window computed once at the dive-launch frame and frozen (ERR-011-005), the stamp dying with its episode + an `OnThreatArmed` episode-onset fallback (ERR-011-006), and a KD-C3 `[GT]` recalibration inside the §3.4.3/§3.4.5 spec ranges. Measured (3 full matches, same seeds): contact windows 0.000 → 0.30–0.57, and the goal effect recorded in the owner doc's §6 table. No schema/RNG/draw-order change. Instruments that counted "shots" off stamp edges re-anchored to the new `TestOnly_ShotContacts` genuine-strike counter. Prior update below.)
**Updated (prior):** July 28, 2026 (v1.50 — **ERR-008-016 + ERR-006-004 + ERR-001-005 filed + RESOLVED at the shot-speed & woodwork pass** (residual lever (b) of the shot-outcome distribution pass). #8 §3.5.3's PowerIntent — a product of two [0,1] fractions — pinned nearly every shot at its own 0.1 clamp floor, and #6's `V_FLOOR = 10` anchored a neutral full-power shot at ~16 m/s before reducers: composed, measured shot-tick means ran 6.9–10.3 m/s against football's ~20–25. PowerIntent becomes floor-plus-modulation (`[GT] POWER_INTENT_FLOOR` 0.65), `V_FLOOR` retunes 10 → 24 over two measured iterations, and — because football-pace shots move ~0.42 m/tick — the goal frame becomes PHYSICAL and precisely adjudicated: a swept six-cylinder segment test (`ApplySweptGoalFrameCollision`, `ApplyGoalPostCollision`'s first production caller — a discrete test tunnels a 0.12 m post) and crossing-point goal-line adjudication (the detected position is up to 0.42 m past the plane; a rising ball crossing UNDER the bar read as over it). Measured: means 14.7–16.1, maxima to 27.6, shots/match 59–70 → 31–45 (football ~25), goals/shot ROSE 0.14–0.25 → 0.38–0.42 — pace now exposes the keeper's conversion, residual lever (c), measured against real shot speeds for the first time. No schema/RNG/draw-order change. Acceptance `match-engine-shot-speed`: 5 of 7 predicates fail pre-fix, verified by execution. Prior update below.)
**Updated (prior):** July 27, 2026, latest same day (v1.48 — **ERR-037-002 filed + RESOLVED at Match Analytics & Statistics #37 T1 implementation** (path-to-playable roadmap B3) — the second #37 error found by *code*, and the same class as ERR-030-011/-012/-013: a §3 rule whose two clauses cannot both hold. §3.4 states the territorial split as **two strict inequalities** (`x > L/2` for team 0, `x < L/2` for team 1) and then, one sentence later, requires the split to be **total** — *"no double-count, no gap"*. At exactly `x == L/2` two strict inequalities credit **neither** team, so the invariant `territorial%[0] + territorial%[1] == 100` breaks. Reachable on ordinary play rather than only in the limit: a kickoff parks the ball on the centre spot for many consecutive ticks with `x` exactly `52.5`. Resolved in favour of **totality** — the strict `>` decides and the halfway line falls to team 1 — because at a single sample point on a continuous axis the side of the line is arbitrary while losing samples is not. Locked by a boundary test that asserts the two shares sum to 100 for a ball sitting exactly on the line. No FR text change, no format-version change, no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 27, 2026, later same day (v1.47 — **TWENTY-THREE back-props filed and RESOLVED atomically with the ten-spec approval wave** (#53, #35, #46, #36, #54, #47, #48, #50, #51, #39 all `IN REVIEW → APPROVED`), per each spec's own promotion-pipeline step 6. Two of the ten — **#48 and #39 — file nothing at all**, stated in their §8.2 as a positive property rather than an empty table. **The load-bearing find is that #30's pinned day-advance tick order was not implementable as written**: `ERR-030-007` had been filed **twice** (once for #42's academy step at #42's approval, once for #32's scouting step at #32's approval), so §3.3 carried **two step 7s and two step 8s** plus an orphaned `AdvanceDay` comment line — and six approved specs cite those numbers. Reconciled under **ERR-030-022** in a new **§3.3.1**, which also records the **conflict between two of this wave's own back-props**: ERR-030-020 (#53) requires its step to precede its same-day consumers and says to renumber below it, while ERR-030-022 requires the cited slots not to move — jointly unsatisfiable by inserting a new step 1. Resolved by numbering the facility step **0**; a step numbered zero is unusual, but a renumber that silently invalidates six approved specs' citations is worse, and patching all six would edit approved text for a numbering preference rather than a design need. **`ERR-030-009` is likewise a duplicate** (#45's `JobSecurity` band and #44's §3.4 availability filter) — both filings are preserved verbatim as frozen records and are now documented as errata rather than left to be rediscovered. **The other structurally significant entries:** **ERR-048-001** corrects a **contradiction between two MUSTs inside APPROVED #48** (FR-MP-025 forbids `#51 → #48`; FR-MP-027 required #51's catalogue to be keyed on #48's `CueId`) which would have surfaced as an assembly cycle after both specs were approved; **ERR-045-002** re-points `FR-BD-012` from **#30 to #54**, closing a MUST that delegated the sacking decision to a spec containing no such rule; **ERR-033-003** replaces a per-producer morale field with a **producer-agnostic** one, filed **jointly by #35 and #46** because the second producer arrived before the first was approved; **ERR-049-001** generalizes `FR-LC-020` off one producer's RNG reservation, and is load-bearing for three specs; **ERR-027-003** records that the **generation contract is save-visible without being saved**; and **ERR-030-019/-017** amend the outer save frame. Five entries are pure doc-only producer re-attributions (#34/#42/#28/#40 all pointed at **#40** for a facility model #40's own scope excludes — the gap that caused #53 to exist). **Also fixed in passing:** `season-competition-loop/section-2.md` and `section-3.md` each carried **two bare `**Last Updated:**` labels** with different content, the header-drift class this log has recorded before. **No code changed and no gate was run — every entry is spec text.** Prior update below.)
**Updated (prior):** July 27, 2026 (v1.46 — **ERR-030-015 filed + RESOLVED at Season & Competition Loop #30 T3 implementation (roadmap A5)** — the third #30 error found by *code* rather than by a downstream spec's approval, and the same shape as ERR-030-011: a §3 pseudocode block that omits a step the surrounding spec requires. §3.5's `RollToNextSeason` regenerates `Fixtures`, resets `Table`, and advances `SeasonNumber`/`Seed` — but **never touches `Calendar`**, whose cursor sits at `RoundCount` (season complete) precisely because the season just ended. A roll implemented from §3.5 verbatim therefore produces a season that is **permanently unplayable**: `IsSeasonComplete` stays true forever, so `AdvanceToNextFixtureDay` throws F5 and `AdvanceAndPlayNextRound` throws, on every subsequent call, for the rest of the career. The transform is not merely incomplete — as written it cannot deliver FR-SN-029's "multi-season continuity" at all. §3.5 gains step (c′), the calendar rebuild, between (c) regenerate and (d) age advance, and the surrounding steps are untouched so FR-SN-031's (a')/(b') insertion points keep their meaning. T3 implements it by **shifting the old calendar's shape forward** by one season length plus a `[GT] SeasonBreakDays` close season, which keeps the roll a pure function of the prior `SeasonState` (KD-6) and preserves a non-uniform schedule instead of flattening it to linear. Caught by the acceptance test playing a **second** season to completion — asserting only on the rolled state's fields would have passed. No FR text change, no `SEASON_STATE_FORMAT_VERSION` change (the calendar was already serialized), no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 26, 2026, later same day (v1.45 — **ERR-030-014 RESOLVED, and ERR-008-014 + ERR-008-015 filed + resolved with it** at match-engine §5.Z Phase H (roadmap A4b). The possession bootstrap turned out to be five seams, not the single kickoff grant the finding anticipated, and four of the five were found by RUNNING the composed engine one after another — each defect invisible until the previous fix let play run further. Match-engine-owned seams: the restart taker award (`ApplyRestart` now takes an `awardedTeam`, every call site declares one), the loose-ball pickup for a ball that comes to REST (the exact speed-gate complement of `RunFirstTouch`, which correctly refuses a still ball), and the orchestrator-side DecisionTree completion sweep. **ERR-008-014** (Decision Tree #8): the tree had NO action that fetches a stationary loose ball — PRESS targets an opponent, MOVE targets the formation slot, INTERCEPT bailed at its minimum-ball-speed gate — so play died the first time a ball came to rest more than ~10 m from anyone; fixed by emitting a loose-ball collect as the SOLE off-ball option for one host-designated collector per team (the ERR-008-013 SAVE precedent, and for AR-4's reason: a must-happen action cannot depend on out-scoring a competitor under composure noise — measured, the collect lost to MOVE inside the noise band and the collector dithered). **ERR-008-015** (Decision Tree #8): §3.7.2 parks a tree in EXECUTING after PASS/SHOOT and says completion "arrives via `NotifyActionComplete`", but assigns that obligation to nobody and **no production caller existed** — so every agent that passed or shot was frozen for the rest of the match, and if it still held the ball it could never release it; the composition root now closes the lifecycle, since it is the only layer that sees both the tree and its executors. Acceptance is the new `match-engine-play-develops` scenario (6 seeds × 9 min; every predicate fails on the pre-fix engine). Full dotnet gate PASSED, 0 failures. Prior update below.)
**Updated (prior):** July 26, 2026 (v1.44 — **ERR-030-014 filed, OPEN, match-engine-owned — the most consequential finding on the path-to-playable track so far.** Discovered by running roadmap item A4a's KD-8 **Step 0** pilot (the cheap signal check that precedes the multi-hour calibration corpus): all 20 full 90-minute engine matches finished **0–0** at a measured squad-rating differential of **±6** on a `[1,20]` scale. Characterisation over 60 000 ticks, in both a distinct-squad and a plain neutral configuration, found the ball's velocity **identically zero for the entire match**, never airborne, and **never possessed by any agent**. Root cause is a closed loop, half of it stated in the engine's own source: `InitializeKickoffState` places the ball at rest (*"a kick would set it in motion; none at Stage 0"*), `RunFirstTouch` gate 3 requires the ball to ALREADY be moving before any agent can receive it, production possession is granted only by that path (`TestOnly_SetPossessor` is documented "Not called by production"), and the ball is set in motion only by a pass/shot executor gated on `IsBallPossessedBy`. No motion ⇒ no reception ⇒ no possession ⇒ no kick ⇒ no motion. **A production match has always been a 90-minute 0–0 deadlock**; this is not a #30 or A3 regression — the neutral configuration above is the one every existing match-engine test and the kickoff capstone use, and none of them asserts that the ball is ever kicked. Consequences: **A4a is blocked upstream of itself** (not by its ~5 h of compute, measured here at ~98 s/match); the #30 T2 quick-sim's three `[GT]` shape parameters ship **provisional, explicitly not fitted**; **PM-1 ("watch a match") is blocked by the same gap**; PM-2-sim is not. Owner is `match-engine-design.md`, not #30 — the fix is a kickoff/restart possession grant, deliberately NOT attempted inside A4 (it is a behaviour change to the most safety-critical assembly, it activates a large amount of never-composed code, and it moves every engine digest). Evidence, blast radius and reproduction: `docs/tracking/round-resolution-corpus.md`. Prior update below.)
**Updated (prior):** July 26, 2026 (v1.43 — **ERR-030-012 + ERR-030-013 filed + RESOLVED at Season & Competition Loop #30 T2 implementation** — the third and fourth #30 errors found by *code* rather than by a downstream spec's approval, and both are the same shape: a §4 architecture sketch that cannot be implemented as written because another section of the same spec forbids it. **ERR-030-012** — §4.5 specifies a REGISTERED, cursor-positioned `DeterministicRngService` season stream (`season-loop.season-events`, `SubsystemOrdinals.SeasonLoop = 84`), but §3.4.1 requires the round-resolution model's draws to be **keyed on the fixture** so a round resolves order-independently (T-SN-CAL-003c) — a cursor makes each scoreline depend on how many fixtures were drawn before it, and that scoreline is serialized. T2 realizes the sub-stream as a keyed derivation folding `DOMAIN_TAG_SEASON_LOOP` into the fixture key (that tag's first consumer, discharging ERR-030-001's code-const-at-T2 obligation), and deliberately does **not** allocate `SubsystemOrdinals.SeasonLoop = 84` in code — an ordinal with no registered stream is the zero-consumer phantom FR-LW-031 forbids and ERR-030-001 exists to prevent; ordinal 84 stays spec-reserved for the first cursor-positioned season event. **ERR-030-013** — §4.6 says `EmitMatchOutcome` "records the `MatchResult` in `SeasonState`", but §2.2 and Appendix B give `SeasonState` no outcome collection; adding one would bump `SEASON_STATE_FORMAT_VERSION` for a payload FR-SN-017 forbids #30 from building a consumer for. The producer record is loop-scoped and transient (`SeasonLoop.MatchOutcomes`); the durable record is the serialized league table. FR-SN-016 unchanged and satisfied. `section-4.md` → v0.3; no FR text change, no format-version change, no `DETERMINISM_DIGEST_VERSION` bump. **Housekeeping, same commit:** this file carried TWO `**Version:**` fields (1.39 at the head of the stack and 1.40 four lines below), both stale against the v1.42 entry at the top — the same drift class the v1.38 correction note records. Consolidated to one field, and no `Updated` row was removed. Prior update below.)
**Updated (prior):** July 25, 2026 (v1.39 — **ERR-030-008 + ERR-030-009 + ERR-045-001 filed at Board & Ownership Dynamics #45 section-file approval.** ERR-030-008: `season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder` gains the **board null seam as step 8** (after the #42 academy seam; `AdvanceDay` → step 9), FR-SN-034 enumeration + "documented positions" prose extended to steps 1–8 / #45 (section-2 v0.8 / section-3 v0.8). Like #42's and unlike the #31/#34 deep-tier position reservations, this seam **goes live at #45's own T2** — one bounded integer drift per **modelled** club (the minimal tier models the managed club only). ERR-030-009: **`BoardState.JobSecurity` becomes a DERIVED BAND** over #45's per-club board confidence from #45 T2, rather than independent state — an independent scalar alongside #45's confidence is two truths for one quantity that diverge at the first restore with nothing to detect it. #30 keeps sole ownership of `BoardObjective` and the boundary evaluation; only the job-security half becomes a projection. Two deliberate consequences: the season block loses its **last `float`** (#28/#33/#40/#41/#42/#45 are integer-only by requirement), and the representation change is a **`SEASON_STATE_FORMAT_VERSION` bump** — pre-T2 saves rejected fail-loud, **no migration** (#50's subject). ◑ Spec-text-first: text at approval, effect + bump at #45 T2. ERR-045-001: `deterministic-sim/section-3.md` §3.4 (v1.0.14) gains **three** `_RESERVED_` rows — `0x2B` (#42, ordinal 93), `0x2C` (#43, 94), `0x2D` (#45, 95) — all **RESERVED, not promoted** (#45's minimal tier is draw-free; #42's `youth.intake` site awaits its T2; #43 unauthored). No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 24, 2026 (v1.38 — ERR-030-007 filed at Youth Academy & Intake #42 section-file approval: `season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder` gains the **academy null seam as step 7** (after the #34 staff seam, before the live world-day tick; `AdvanceDay` → step 8) and `section-2.md` FR-SN-034's enumeration extends to #42 (both → v0.7). Doc-only re-pin of a documented position — no interface, no code. **Two corrections applied July 25, 2026:** (a) this entry was originally filed as **v1.36**, duplicating the existing v1.36 (ERR-030-004) while v1.37 already existed, and was inserted mid-stack rather than at the top — renumbered **v1.38** and moved into order, and the file's `Version` field (left at 1.37) is now correct; (b) it claimed "**No #16 change**", which was right about not *promoting* `0x2B` (FR-LW-031 — no stream with zero draw sites) but wrong to conclude nothing was owed: #16's **A-04 every-gap-has-a-placeholder rule** still required a `_RESERVED_0x2B_` row, as #29 and #40 both have while unpromoted. That placeholder is filed under **ERR-045-001** above. Prior update below.)
**Updated (prior):** July 25, 2026 (v1.42 — **ERR-030-011 filed + RESOLVED at Season & Competition Loop #30 T1 implementation** — the second #30 error found by *code* rather than by a downstream spec's approval. Two spec surfaces disagreed about the season sub-blob's byte layout: `section-3.md` §3.6's `EncodeSeason` pseudocode omitted `ManagedClubId` (which `appendices.md` Appendix B lists as row 3a and §2.2's `SeasonState` requires — a codec written to §3.6 verbatim emits a blob no season can be reconstructed from), and Appendix B row 11 left job security as `jobSecurity f32/u8`, neither matching the integer per-mille `BoardState` carries. **Appendix B is the byte-layout authority.** §3.6 gains the missing row-3a line plus a correction note; Appendix B row 11 is pinned `jobSecurityPerMille i32`, ratifying the integer convention #30 T0 adopted and flagged as a back-prop candidate (the #41 AR-1 float→integer-per-mille precedent). Code `src/season-save/SeasonStateCodec.cs` implements the corrected layout with a pinned-offset test guarding field order. No FR change, no format-version change (T1 is `SEASON_STATE_FORMAT_VERSION`'s first use, so the correction lands before any file exists), no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 25, 2026 (v1.41 — **ERR-030-010 filed + RESOLVED at Season & Competition Loop #30 T0 implementation** — the FIRST #30 error found by *code* rather than by a downstream spec's approval, and the first ERR on this project's path-to-playable implementation track. §3.1's fixture-generation pseudocode venues the first leg by round parity (`(round even) ? (a,b) : (b,a)`, commented "for a balanced first leg"), but the two concrete worked schedules derived from it — `section-3.md` §3.7 and `appendices.md` Appendix C — were hand-computed WITHOUT that step, inverting rounds 1 and 4; `section-5.md`'s T-SN-FIX-001 then pinned the wrong table. **The pseudocode is authoritative; the worked tables are the defect.** Measured at the Stage-2 target size of 20 clubs: without parity the pinned club plays **all 19** first-leg fixtures at home (first-leg home counts range 9..19); with parity every club lands in 8..10 against an ideal of 9..10, longest consecutive home run 2. Both forms satisfy FR-SN-002/003 (verified N = 2,3,4,5,6,19,20) and no FR constrains venues, so §3.1's own stated intent decides it. Patched: §3.7 + Appendix C rounds 1/4 venue-corrected (pairings unchanged, so Appendix C's 12-ordered-pair completeness bullet is untouched), T-SN-FIX-001 re-anchored, new **T-SN-FIX-008** venue-balance lock added (fails under the pre-correction rule). Code: `src/season-save/FixtureScheduler.cs` implements §3.1 verbatim. Doc + code, same commit; no `DETERMINISM_DIGEST_VERSION` bump, no FR change. Prior update below.)
**Updated (prior):** July 24, 2026 (v1.40 — ERR-030-009 filed + RESOLVED at Discipline & Suspensions #44 section-file approval: `season-competition-loop/section-2.md` FR-SN-013 (v0.8) + `section-3.md` §3.4 (v0.8) gain the **#44 suspension-availability-filter null seam** on the managed squad's resolve→configure path (`ISquadProvider.ResolveByClubId` → *filter* → `ConfigureSquads`; a value-copy reduction, empty until #44 T2 — the flow-side sibling of the ERR-030-002/004/006/007 tick-order pre-declarations). **ERR-030-008 remains soft-reserved by #43** (its T-phase (a') hook + deep fixture-day driver), so #44 takes 009. **No #16 change** — #44 is the read-only class (no RNG stream / domain tag / `SubsystemOrdinals` entry; the #37/#49 positive property): its accumulation is a pure fold over already-deterministic Tier A card/substitution events via the #37-class per-tick ledger tap, and any future quick-sim card synthesis is #30-owned on #30's `0x22` stream. Doc-only; no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 24, 2026 (v1.39 — ERR-043-001 filed + RESOLVED at Competition Structure #43 section-file approval: `deterministic-sim/section-3.md` §3.4 (v1.0.14) gains the three A-04 placeholder rows `_RESERVED_0x2B_` (Youth Academy #42, ordinal 93) / `_RESERVED_0x2C_` (Competition Structure #43, `SubsystemOrdinals.Competition = 94`) / `_RESERVED_0x2D_` (Board & Ownership #45, ordinal 95) — the gap-rule sweep completing the roadmap §6 contiguous block `0x20`–`0x2D` (the v1.0.13 precedent; the catalogue previously ended at `0x2A`, and #43 is the first of the three to reach it). `_RESERVED_0x2C_` **stays reserved at #43's approval** — #43's minimal tier (a singleton-league collection) is draw-free; it promotes to `DOMAIN_TAG_COMPETITION = 0x2C` at #43 T3's first knockout draw (keyed draws on `competition.draws`, `entityId = competitionId`, fixed-radix ordinals — no cursor, nothing serialized). **No #30/#40 change** — FR-SN-031's (a') promotion/relegation insertion point and #40's (b')-after-(a') ordering were pre-declared at those specs' approvals (#43 is the first management spec whose #30 spec-text seams were all reserved ahead; the code-side (a') hook + deep fixture-day driver are soft-reserved as ERR-030-008, T-phase). Pure namespace reservation; no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 24, 2026 (v1.38 — ERR-030-007 filed + RESOLVED at Scouting & Player Knowledge #32 section-file approval: `season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder` gains the **scouting null seam as tick-order step 7** (after staff so a scouting day reads the day's staff state — the ChiefScout doing the scouting; before the world-day tick; `AdvanceDay` → step 8), FR-SN-034 enumeration extended (section-2 v0.7 / section-3 v0.7). A **deep-tier position reservation** — #32's minimal tier is the fog-off omniscient identity (no assignment can exist; `AdvanceScoutingDay` no-ops with fog off), so the seam is empty until the deep tier's daily assignment progress (the ERR-030-002 #41 / ERR-030-004 #31 / ERR-030-006 #34 precedent). **No #16 change** — #32's minimal tier is draw-free (every read short-circuits at zero width before any draw), so `_RESERVED_0x24_` / `SubsystemOrdinals.Scouting = 86` stay RESERVED (the #40 ERR-040-001 / #31 / #34 precedent); promotion to `DOMAIN_TAG_SCOUTING = 0x24` lands at #32 T3's first accuracy draw. Doc-only; the world-floor byte-identity (FR-SN-026) is unaffected (null seam). No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.37 — ERR-030-006 filed + RESOLVED at Staff & Backroom #34 section-file approval: `season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder` gains the **staff null seam as tick-order step 6** (after transfers, before the world-day tick; `AdvanceDay` → step 7), FR-SN-034 enumeration extended (section-2 v0.6 / section-3 v0.6). A **deep-tier position reservation** — #34's scaffold projections are pull-based (threaded into #29/#41 when their inputs are built), so the seam is empty until the deep tier's daily candidate-pool / in-flight-hiring processing (the ERR-030-002 #41 / ERR-030-004 #31 precedent). **No #16 change** — #34's scaffold is draw-free, so `_RESERVED_0x26_` / `SubsystemOrdinals.Staff = 88` stay RESERVED (the #40 ERR-040-001 / #31 ERR-030-004 precedent). Doc-only; the world-floor byte-identity (FR-SN-026) is unaffected (null seam). No `DETERMINISM_DIGEST_VERSION` bump. **ERR-030-005 is soft-reserved by #31** (its deferred `RequestRosterCommit` build), so #34 takes 006. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.36 — ERR-030-004 filed + RESOLVED at Transfers, Contracts & Negotiation #31 section-file approval: `season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder` gains the **transfers null seam as tick-order step 5** (after injuries, before the world-day tick; `AdvanceDay` → step 6), FR-SN-034 enumeration extended (section-2 v0.5 / section-3 v0.5). A **deep-tier position reservation** — minimal #31 transfers are command-driven (`SubmitBid`), so the seam is empty until the deep tier's daily negotiation/rival-bid processing (the ERR-030-002 #41 documented-position precedent). **No #16 change** — #31's minimal tier is draw-free, so `_RESERVED_0x23_` / `SubsystemOrdinals.Transfers = 85` stay RESERVED (the #40 ERR-040-001 / #29 precedent). Doc-only; the world-floor byte-identity (FR-SN-026) is unaffected (null seam). No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.34 — ERR-029-001 filed + RESOLVED at #29 (Training System) section-file approval: **no determinism promotion** — #29 is fully deterministic (pure integer projections; deterministic own-attribute variation), registers no RNG stream, so `_RESERVED_0x21_` / `SubsystemOrdinals.Training = 83` **stay reserved** (not promoted, unlike ERR-028-001's `0x20`). `deterministic-sim/section-3.md` §3.4 (v1.0.10) updates the `_RESERVED_0x21_` rationale; no code const, no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.35 — ERR-040-001 + ERR-030-003 filed at Club Finances & Economy #40 section-file approval: `deterministic-sim/section-3.md` §3.4 (v1.0.12) adds the `_RESERVED_0x29_` placeholder / `SubsystemOrdinals.ClubFinances = 91` **RESERVED not promoted** (minimal tier is a pure integer budget projection, no draw — the #29 `0x21` precedent); and `season-competition-loop/section-3.md` §3.5 gains the finance-settlement null seam at boundary-roll step (b') after the (a') #43 point (ERR-030-003, doc-only). No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.34 — ERR-041-001 + ERR-030-002 filed at Injuries & Medical #41 section-file approval: `deterministic-sim/section-3.md` §3.4 (v1.0.11) allocates `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` / `SubsystemOrdinals.InjuriesMedical = 92` for the `injuries.occurrence` world-tick keyed-draw sub-stream (spec-text-first — code + registration at #41 T2); and `season-competition-loop/section-3.md` §3.3 gains the injuries null seam as tick-order step 4 (FR-SN-034 enumeration extended, ERR-030-002, doc-only). No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.33 — ERR-028-001 filed at #28 section-file approval: `deterministic-sim/section-3.md` §3.4 (v1.0.9) promotes the `_RESERVED_0x20_` placeholder → `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` / `SubsystemOrdinals.PlayerProgression = 82` for the per-club `player-progression.regen` regen stream; spec-text-first like ERR-030-001 — code const + registration at #28 T2 with the first regen; `_RESERVED_0x21_` (#29) stays a placeholder; no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 22, 2026, later same day (ERR-030-001 filed at #30 section-file approval: `DOMAIN_TAG_SEASON_LOOP = 0x22` / `SubsystemOrdinals.SeasonLoop = 84` reserved in `deterministic-sim/section-3.md` §3.4 (v1.0.8) for the Season & Competition Loop #30 season RNG sub-stream + the two `_RESERVED_0x20_`/`_RESERVED_0x21_` placeholders for #28/#29 (roadmap §6 block). This back-prop is **spec-text-first** (◑ partial): the code const + stream registration land at #30 T2 with the first draw site (FR-LW-031 — no phantom stream), unlike the code-first ERR-022/027-001. No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 22, 2026 (ERR-027-001 + ERR-022-001 filed and RESOLVED at #27 promotion: the off-pitch determinism allocations `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` / `SubsystemOrdinals.PlayerDatabase = 81` (#27) and `DOMAIN_TAG_LIVING_WORLD = 0x1E` / `SubsystemOrdinals.LivingWorld = 80` (#22) — both landed in code but never recorded in the #16 §3.4 spec text — are now filed there and in this log. `deterministic-sim/section-3.md` §3.4 gains both rows (v1.0.7); pure namespace allocations, no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 10, 2026, later same day (ERR-024-001 filed and RESOLVED at #23–#26 T0 implementation: Build-Up Structures #24 Appendix A v0.2's PASS-1 M-3 "lane-key correction" keyed every overlay row to lane values NO slot occupies — FR-BU-007 keys rows by the RECORDED `FormationSlotRecord.DefaultLine`/`DefaultLane`, and all three `PositioningAIConstants.Family*` tables record fullbacks at `LH`/`RH` (half-space) and central mids/forwards at `C`, so the catalogue as spec'd was a structural no-op. M-3 verified lane GEOMETRY (LB at y = 10.2 m is in the wide bin) but not the recorded seed values the key actually uses. Appendix A v0.3 + §3.2 v0.3 re-keyed with magnitudes/intents unchanged; `BuildUpOverlayCatalogue.cs` implements the corrected keys; `BuildUpStructureTests.Catalogue_RowKeys_HitEveryFamily_Err024001Regression` locks that every family receives a non-zero own-third offset per structure.)
**Updated (prior):** July 10, 2026 (ERR-021-005 through ERR-021-007, ERR-012-007 through ERR-012-009, and ERR-008-012 filed and RESOLVED same commit — the seven cross-spec back-props landed atomically with specs #23 Dismarking / #24 Build-Up Structures / #25 Positional Rotations reaching `APPROVED` (each spec's §2.3/§2.4 pending-ERR table, per its own pipeline step 6; #26 Tactical Presets declares no back-props at T0–T3). #21-side: `TeamTactic` gains `DismarkIntensity`/`BuildUpStructure`/`RotationFreedom` field rows + Appendix B canonical-order appends in pinned approval order #23 → #24 → #25 after `MarkingOrientation` (`tactical-instructions/section-2.md` v0.5 + `appendices.md` v0.5); serialization enters `WriteTeamTactic` with a `SNAPSHOT_SCHEMA_VERSION` bump only when each owning spec's wiring lands. #12-side: new `positioning-ai/section-3.md` §3.7.1 (v0.6) records the build-up overlay stage (ContextModifier → spacing), the dismark offset stage (spacing → pitch clamp, FR-DM-008), the `RotationController` pre-composition tick position, and the `AgentPositioningData.SlotIndex` single-writer contract amendment (no longer immutable after `SeedFromFormation`; `RotationController` sole post-seed writer). #8-side: `decision-tree/section-3-2.md` v1.5 §3.2.2.1 anchors the FM-DM-03 marked-pass-target multiplier in the external tactical-multiplier product before the final clamp. All amendments identity-preserving at zero-value dials; ERR-012-004..006 remain soft-reserved for the June-13 quarantine adjudication cluster and were deliberately skipped.)
**Updated (prior):** June 16, 2026 (ERR-016-006 through ERR-016-008 + ERR-017-003 filed from the `src/deterministic-sim/` + `src/event-system/` foundation adversarial review. ERR-016-006 (H) RESOLVED same commit — `SaveManager.Load` discarded the on-disk header so the digest chain was unverifiable on reload + `ReplayEngine` step-3 null-fingerprint NRE; `SaveManager.cs` v1.5 (`ReadHeaderBytes` + header-reconstructing `Load` overload) and `ReplayEngine.cs` v1.3 (fail-closed env guard). ERR-016-007 (M, open) fingerprint not on the on-disk header — cross-process digest/env verification blocked, needs a `SNAPSHOT_SCHEMA_VERSION` bump. ERR-016-008 (M, open) RNG zero-count `Reserve` ambiguity + `Skip`/`Reserve` by-convention parity. ERR-017-003 (M, open) `EventBus` producer-phase enforcement is debug-only → debug/release digest divergence on a mis-phased publish. The three open items are deferred for gate-verified follow-up — they are digest/wire-format-sensitive and the remote review environment has no .NET SDK.)
**Updated (prior):** June 13, 2026 (ERR-007-001 through ERR-007-003 filed from the Perception System #7 implementation AR-3 adversarial review (1H+1M+1L-cluster); all patched and CLOSED same commit — forced-refresh double-advance of cross-heartbeat state, pre-dedup candidate-buffer truncation, DeterministicHash Mathf.Abs overflow)
**Updated (prior):** June 11, 2026 (ERR-008-002 through ERR-008-011 filed from the Decision Tree #8 comprehensive audit (spec + May 29 implementation); all ten spec-side defects patched and CLOSED same commit — see the consolidated entry below and `decision-tree/audit-report.md`)
**Updated (prior):** May 22, 2026 (ERR-020-001 filed and resolved: Code Standards #20 §4.2 `[CROSS]` mirror ALL_CAPS → PascalCase; `section-4.md` v1.0.1 patched; `src/CLAUDE.md` v1.4 discrepancy note updated)
**Status:** ERR-001 through ERR-012, ERR-010-001 (closed May 16, 2026), ERR-011-001 (closed May 18, 2026), ERR-012-001 (closed May 18, 2026), ERR-012-002 (closed), ERR-016-001, ERR-016-002 (FULLY CLOSED May 18, 2026), ERR-017-001, ERR-018-001 through ERR-018-018 logged. ERR-010 closed (March 6, 2026). ERR-012 appended from addendum (April 22, 2026). ERR-016-001 added May 2, 2026 (phantom interface mitigation in Deterministic Simulation §4.2). ERR-016-002 added May 3, 2026; spec-text resolved May 6, 2026 (`XC-002-001` in #2 §2.5; `XC-008-001` in #8 §1.7.3); #16 §3.2.5 back-prop prose confirmed landed (OBS-1, stress-test run 2, May 18, 2026) — FULLY CLOSED. ERR-017-001 added May 12, 2026 (Event System #17 PASS 2 review — `DOMAIN_TAG_EVENT_LEDGER` allocation back-prop into #16 §3.4); fully resolved May 15, 2026 — #16-side allocation landed May 14, 2026 (`0x15` in #16 §3.4 v1.0.1) and #17-side `[CROSS-PENDING]` → `[CROSS]` promotion landed in #17 §1.0.1 patch revision May 15, 2026 (literal value inlined across §3.4.2 / §3.10 / §1.4 / §2.4.4 / §7.5 D9 / §8.1.4 / §8.3.4 / §8.4 / §9.2 Q10 / §9.3 R3 / Appendix B / Appendix D). ERR-018-001 added May 13, 2026 and resolved same day at outline level (Performance Optimization #18 `outline-detailed.md` v1.1 inverts KD-3 — #18 owns trace pipeline, #16 retains record format / regression scenarios / emission constraints; section-number citations corrected). ERR-018-002 through ERR-018-011 added May 14, 2026 from PASS-1 adversarial review of #18 section files v0.1 (4 H + 6 M findings); all resolved in v0.2 fix pass (May 14, 2026). ERR-018-012 through ERR-018-018 added May 14, 2026 from PASS-2 adversarial review of #18 section files v0.2 (2 H + 5 M findings tracing primarily to PR #59 + PR #60 parallel-branch merge collisions); all resolved in v0.3 fix pass (May 14, 2026) — #18 section files at v0.3. ERR-002 and ERR-003 remain open. ERR-003-001 through ERR-003-004 added June 10, 2026 (Collision System #3 implementation AR-7 adversarial review — force-conversion calibration, FROM_BEHIND normal convention, same-team stumble gap, candidate-counted pair valve); ERR-003-005 and ERR-003-006 added same day from the AR-8 follow-up sweep (inverted approach gate in §3.3 impulse response; FROM_BEHIND shadowed by the shoulder predicate); all six spec-and-code patched and CLOSED June 10, 2026. ERR-004-003 through ERR-004-005 added June 10, 2026 (First Touch #4 implementation AR-7 adversarial review — §3.3.2 IncomingDir sign inversion, agent-anchored interception proximity, vacuous DEFLECTION alignment gate); ERR-004-003 and ERR-004-004 spec-and-code patched and CLOSED same day; ERR-004-005 documented-open (model observation, gate retained per spec). ERR-004-006 added June 10, 2026 (AR-8 follow-up sweep — §5.10 VS-001 hand-calc used an additive below-reference velocity modifier contradicting normative §3.2.3) — spec and test patched and CLOSED same day. ERR-017-002 added June 12, 2026 (constraint-only Publish/Subscribe overload triple — CS0111, event-system production assembly never compiled; found by the first-ever full-tree compile on the dotnet CI gate) — spec §3.2.1/§3.2.2 and code patched and CLOSED same day. ERR-016-004 and ERR-016-005 added June 15, 2026 from the `src/deterministic-sim/` implementation adversarial review (ERR-016-004 H: `Skip()` advanced `RngCursor` but not the determinism-relevant `ActionOrdinal`, breaking RNG branch-safety; ERR-016-005 M: `SnapshotCodec.Encode` hashed payload-only instead of the §3.2.3 chained header‖payload digest, with the golden-corpus suite reconstructing the preimage by hand so the divergence was untested) — both code-patched and CLOSED same day; regression tests added.
**Raised During:** Pass Mechanics Spec #5 pre-Section 3 cross-spec audit; Decision Tree Spec #8 BLK-001

---

## Error Index

| ID | Title | Severity | Files Affected | Status |
|----|-------|----------|---------------|--------|
| ERR-001 | `IBallPhysicsCallback` fragments a single operation into four methods | Major | 2 | Closed — fixed in First_Touch_Spec_Section_4_v1_1.md |
| ERR-002 | `StringIDs` papers over an undesigned event bus with the wrong solution | Moderate | 1 | Open — low priority, fix at convenience |
| ERR-003 | `PerformanceContext` violation mandate imposes governance with no Stage 0 benefit | Moderate | 10 | Open — low priority, fix at convenience |
| ERR-004 | `IPossessionManager` and `IFirstTouchEventQueue` interface against unspecified systems | Major | 4 | Closed — fixed in First_Touch_Spec_Section_4_v1_1.md |
| ERR-005 | `KickType` enum encodes caller intent into Ball Physics (eliminated by design decision) | Major | 2 | Closed — resolved during audit |
| ERR-006 | `Ball.ApplyKick()` / `KickType` referenced in Ball Physics §8 but never defined in §3.1.11 | Critical | 2 | Closed — resolved in Ball_Physics_Spec_Section_3_1_v2_5.md |
| ERR-007 | `KickPower`, `WeakFootRating`, `Crossing` absent from `PlayerAttributes` | Critical | 1 | Closed — resolved in Agent_Movement_Spec_Section_3_5_v1_3.md |
| ERR-008 | `BallState` has no `PossessingAgentId` field; `ApplyKick()` amendment references it incorrectly | Critical | 2 | Closed — Option B adopted; possession external to BallState; resolved in Ball_Physics_Spec_Section_3_1_v2_5.md |
| ERR-009 | `PassThroughGround` / `PassThroughAerial` are redundant `KickType` values | Minor | 1 | Closed — resolved during audit; through passes use `PassGround`/`PassLofted` |
| ERR-010 | Shot Mechanics §1.1 refers to Decision Tree as Spec #7 — canonical number is #8 | Minor | 1 | ✅ Closed — Fixed in shot-mechanics/section-1.md v1.2 (March 6, 2026); part of comprehensive audit renumbering cascade |
| ERR-011 | `SpatialHashGrid.Query()` ignores radius parameter — always returns fixed 3×3 neighbourhood | Major | 1 | ✅ Closed — Fixed in Collision_System_Spec_Section_3_v1_1.md (March 5, 2026) |
| ERR-012 | First Touch §7 refers to Decision Tree as Spec #7 (5 occurrences) | Minor | 1 | ✅ Closed — Fixed in first-touch/section-7.md v1.1 (March 5, 2026) |
| ERR-012-001 | `DOMAIN_TAG_POSITIONING_AI` allocation + Phase B/C block (originally proposed `0x16…0x1B`; shifted to `0x17…0x1C` May 16, 2026 after #10 took `0x16`) needed in #16 §3.4 | Medium | 1 | ✅ Resolved May 18, 2026 — `DOMAIN_TAG_POSITIONING_AI = 0x17` allocated in #16 §3.4 v1.0.5; §6.1 `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted atomically with #12 `APPROVED`; body-text instances in §1/§2/§3/§4/§8 promoted in v0.3/v0.4 fix passes |
| ERR-012-002 | Decision Tree #8 `section-3-1.md` L716 cites Formation System as "Spec #14" — current #14 is Defensive AI; Formation System is #12 | Minor | 1 | ✅ Closed — Fixed in decision-tree/section-3-1.md v1.1.1 (May 15, 2026); single-token "Spec #14" → "Positioning AI, Spec #12"; approval status preserved |
| ERR-010-002 | Heading Mechanics #10 §3.5 delegated the header aim to Decision Tree #8 — which **cannot emit a header at all** (`ActionType` ordinal 8 overflows the 3-bit composure-noise field; wiring backlog W9) — so the aim decision had no owner and `TargetIntent` reached no formula. Every header was a **passive specular mirror**: the ball left the head along the reflection of its own incoming path and the player had no influence on direction. Two further defects in the same chain: the contact point had **two independent derivations** (Pass 1 and Pass 2 of `HeadingMechanics.Update`, agreeing only by coincidence), and Pass 2 rebuilt the world point from its **2-D** head-local projection, pinning `contactPointActual.z` to the head centre — so the reflection normal was permanently horizontal, `reflected.z = v̂_in.z`, and **a descending ball was headed further down**. No header could lift the ball. The `ERR-011-010` shape. | **High** | 10 | ✅ **Resolved August 9, 2026** — new #10 §3.5.1 + `HeadingAim.cs`: ballistic launch solve to the target (low root; maximum-range fallback out of range, P1) `[CORRECTED — this row said "45°"; 45° is the max-range angle only at dz = 0, and `d93e0c8` replaced it with tanθ = v / sqrt(v² − 2·g·dz). See the AR pass 2 entry above]`, the reflecting half-vector bounded to the physically reachable hemisphere `[CORRECTED — no bound exists; see the v2.03 entry above]`, and an achieved normal blended from the geometric normal by normalised Heading (FULL-RANGE ramp, `ERR-008-019` shape; authority 0 ≡ pre-fix). One `ResolveContactGeometry` owner read by both passes; the 3-D contact point carried directly. Producer half: new `GkHeadingIntentSource.HeaderAimTarget` — clear wide when deep, aim at goal when advanced, continuous between; documented at `gk-heading-engine-integration-design.md` §4.2a as of v2.03. **No new `[GT]`** (the attribute is the dial, so inside KD-W1), **no schema bump**, no new RNG stream / domain tag / draw site / draw-order change. **GATE-VERIFIED** (local whole-tree run, head `c89c838`): `HeadingMechanics.Tests` 60/15/0 (47 → 60, the +13 new locks all executed), `MatchEngine.Tests` 447/1/10 **byte-identical to the pre-fix baseline**, the single failure being the inherited C1 `sim_match_engine_close_chance`. The landing's own "digests DO move" claim is **WITHDRAWN as stated** — no measured movement anywhere; a match containing an executed header would digest differently and no scenario in this tree contains one, at a measured 0.2% contact ratio. |
| ERR-012-011 | Positioning AI #12 §3.0 classified phase from the **on-ball carrier**, which the engine clears at every `ApplyKick` and restores only on physical receipt — so for the entire flight of every pass the snapshot read "loose ball" and §3.0.2's velocity branch classified a team knocking the ball around as being in **transition**. Measured: `InPoss` committed on **7.5%** of final-third samples (`TransToAtk` 58.9%), starving every phase-gated mechanism in #13/#14/#15. Spec and code were each self-consistent; "who is on the ball" and "which team has the ball" are different questions and only the first was ever asked. | **High** | 9 | ✅ **Resolved August 8, 2026** (wiring backlog C1) — §3.0/FR-PA-022 now classify from TEAM possession, composed by the orchestrator as carrier's team ∪ intended receiver of a pass in flight; new §3.0.5 worked example. Engine gains a `_passInFlightReceiverId` latch expiring on possession, any ball strike, restart, receiver inactivity, or the ball ceasing to approach him (`RunFirstTouch`'s own receding predicate, hoisted — **no new `[GT]`, no timeout**, so inside the KD-W1 freeze). Snapshot fields ADDED, not redefined, so #23's FR-DM-007 carrier exclusion is untouched. **`SNAPSHOT_SCHEMA_VERSION` 19 → 20**; no new RNG stream / domain tag / draw site / draw-order change. Two clears (GK-heading adapter, `ApplyRestart`) recorded as having no isolating lock. |
| ERR-010-003 | Heading Mechanics #10 §3.2/§3.3 (KD-18) and the mirrored `HeadingEligibility.cs`/`HeadingMechanics.cs` comments describe the `{GROUNDED, STUMBLING}` exclusion as an **aerial-phase check** — "agent must have left the ground" — but Agent Movement #2 §3.1.2 defines `GROUNDED` as one specific incapacitated substate ("knocked down" after a collision or extreme stumble), not the complement of airborne. A standing, walking, jogging, sprinting, or decelerating player is never `GROUNDED` and clears the check trivially; AM #2 publishes no Z-axis/airborne state at all (KD-18's own premise), so "has left the ground" is not a question this check is capable of answering. Surfaced as a recorded-not-folded-in bullet at the tail of `ERR-010-002`. | Low | 9 (citation sites; none changed) | 🟡 **Open — RECORDED, NOT FIXED, August 9, 2026.** Verified NOT a no-op and NOT inverted: the exclusion is real and reachable — it correctly blocks a header attempt while the agent is prone or off-balance from a collision/stumble, exercised by ordinary gameplay. The defect is entirely in the label, not the behaviour: the actual aerial phase is synthesized independently by `jumpStartFrame`→`landingFrame` timing, never validated against any position/state signal. A real fix is a relabeling exercise (documentation-only; same class/severity as `ERR-020-003`), deferred — no code change proposed by this entry. |
| ERR-008-001 | Decision Tree #8 §3.2 `PitchGeometry` pseudocode class uses centered origin `(0,0) = centre of pitch` with X:−52.5–+52.5m/Y:−34–+34m — contradicts CLAUDE.md + Ball Physics #1 §1.2 corner-origin; all goal constants wrong | High | 1 | ✅ Resolved May 18, 2026 — `section-3-2.md` v1.3: class rewritten to corner-origin (0,0,0); all `Vector2` goal constants replaced with `Vector3` using correct values; citation corrected to §1.2 and Appendix C; XC-GEOM-01 verification note added |
| ERR-008-002 | DT #8 §2.2.5 `MatchContext.BallZone` is a single shared field documented "from own goal line" — unsatisfiable for both teams; implementation consumed home-perspective zone for away agents (all zone modifiers inverted; away in-range shots ×0.10) | High | 3 | ✅ Resolved June 11, 2026 — §2.2.5 field note (home-perspective; normative consumption is per-team derivation from `BallPosition.x`), §3.2.1.3 consumption note; `DecisionContextAssembler.cs` v1.2 + `PitchGeometry.cs` v1.1 + `UtilityScorer.cs` v1.2 |
| ERR-008-003 | DT #8 §3.4.5 line-depth pseudocode adjusts `adjustedSlotY` — Y is the touchline axis in the corner-origin system; formula also lacks the team sign. Implementation copied the Y form verbatim (latent: Stage 0 depth pinned 0.5) | Medium | 2 | ✅ Resolved June 11, 2026 — §3.4.5 pseudocode rewritten to team-signed X; `TacticalContext.cs` v1.1 |
| ERR-008-004 | DT #8 §3.4.2 PassingStyle table cell `DRIBBLE 0.9 [GT]` under DIRECT contradicts §3.4.4 prose ("neutral under all three styles") and the §3.4.7 catalogue (no such constant) | Low | 1 | ✅ Resolved June 11, 2026 — table cell corrected to 1.0 (prose + catalogue + implementation agree) |
| ERR-008-005 | DT #8 §3.4.6 gates press urgency on `PossessionState.OPPONENT` — no such enum member (§2.2.5 enum is absolute HOME/AWAY/CONTESTED); implementation literalised it as `== AWAY_TEAM`, inverting urgency for away agents | Medium | 2 | ✅ Resolved June 11, 2026 — §3.4.6 reworded to the derived perspective flag; `DecisionContext.OpponentHasBall` added (assembler-derived); `TacticalModifierResolver.cs` v1.1 |
| ERR-008-006 | DT #8 §3.1.3.4 CROSS gate tests "AgentPosition.x in WIDE_ZONE" — wide channels are touchline-relative (Y axis), and WIDE_ZONE is declared in no constant table; gate unimplementable at Stage 0 | Low | 1 | ⚠ Documented-open June 11, 2026 — SPEC-DEVIATION NOTE at `OptionGenerator.DerivePassType` (CROSS classified from range + facing angle; `Crossing` attribute doc-noted unconsumed); WIDE_ZONE declaration is a Stage 1 spec task |
| ERR-008-007 | DT #8 allocates FM-DT-09 twice: §3.1.1.3 possession-uncertainty warning AND §3.5.9 unknown-ActionType dispatch failure | Low | 1 | ✅ Resolved June 11, 2026 — §3.5.9 row renumbered FM-DT-14 (next free ID); FM-DT-09 stays with §3.1.1.3; `DecisionTreeConstants.cs` v1.2 |
| ERR-008-008 | DT #8 §3.7.2 row 5 lists only HOLD/MOVE as continuous, leaving DRIBBLE/PRESS/INTERCEPT in EXECUTING pending a completion signal that no Stage 0 system emits (agents would freeze after first movement dispatch); no DT→executor cancel entry point exists for the §3.6.3 action-change path | Medium | 2 | ✅ Resolved June 11, 2026 — §3.7.2 Stage 0 deviation note (all movement-routed actions continuous; PASS/SHOOT hold EXECUTING; executor self-cancel via Pass #5 FM-08/§3.8.5); `DecisionTreeStateMachine.cs` v1.1 |
| ERR-008-009 | DT #8 §3.1.9.2 tags `DRAG_APPROX = 0.3 s⁻¹` as `[CROSS — Ball Physics #1 §3.x]` — #1 models quadratic drag and declares no such constant; value is a DT-side calibration, so [CROSS] violates the verbatim-copy rule (citation also names no real section) | Low | 2 | ✅ Resolved June 11, 2026 — retagged [EST] with derivation note in §3.1.9.2 and `UtilityWeights.cs` v1.2 |
| ERR-008-010 | DT #8 §3.4.7 / §3 completion summary claim 23 constants but list 22 rows (`PRESS_URGENCY_FACTOR` double-counted across the tactical and dispatch groups); §3.2.7.2 also claims pressing modifiers live in `UtilityWeights.cs`, contradicting §3.4.7 "exclusively in TacticalWeights.cs" | Low | 2 | ✅ Resolved June 11, 2026 — tallies corrected to 22 (16+6); file-rule contradiction resolved in favour of §3.4.7; `TacticalWeights.cs` v1.1 header |
| ERR-008-011 | DT #8 §3.1.4.3 pseudocode offsets goal posts along X (`GoalCentre + Vector2(±3.66, 0)`) — the goal line runs along Y at fixed X; §3.2.1.4 PitchGeometry (post-ERR-008-001) has the correct form | Low | 1 | ✅ Resolved June 11, 2026 — §3.1.4.3 corrected to Y ± 3.66 |
| ERR-007-001 | Perception #7 §4.6 forced refresh re-ran the full pipeline, double-advancing cross-heartbeat recognition-expiry/scheduler/latency state out of the 10 Hz cadence (§4.6.2 mandates resetting only the triggering entity) — premature eviction + off-cadence shoulder checks; `FilteredView` depended on whether a refresh fired (determinism hazard) | High | 3 | ✅ Resolved June 13, 2026 (impl AR-3 H-1) — all three mutations gated behind `!forcedRefresh`; new side-effect-free `IsConfirmed`/`IsBlindSideConfirmed` reads; dead `ResetObserver` removed. `PerceptionSystem.cs` v1.4, `RecognitionLatencyTracker.cs` v1.3, `ShoulderCheckScheduler.cs` v1.2 |
| ERR-007-002 | Perception #7 §3.0 Step 1 truncated the spatial-hash query to the first `MaxAgents+1` entries BEFORE de-duplication (ball never deduped) — multi-cell straddle could drop a unique agent from perception | Medium | 1 | ✅ Resolved June 13, 2026 (impl AR-3 M-1) — dedup (agents + ball) across the full raw query before any cap; `id ≥ MaxAgents` dropped at source. `PerceptionSystem.cs` v1.4 |
| ERR-007-003 | Perception #7 §3.3.4 `DeterministicHash` returned `Mathf.Abs(h)` — `Math.Abs(int.MinValue)` throws (latent ~1-in-2³² crash) and a negative hash made caller `% N` (L_rec noise / shoulder jitter) out-of-range | Low | 4 | ✅ Resolved June 13, 2026 (impl AR-3 L-cluster) — `h & 0x7FFFFFFF`; bundled: possession multiplier constant (FR-CS-016), FoV doc. (Window-close `>`→`>=` proposal WITHDRAWN — broke SC-002; expiry is the last active tick by design.) `RecognitionLatencyTracker.cs` v1.3, `ShoulderCheckScheduler.cs` v1.2, `PerceptionConstants.cs` v1.3, `FovCalculator.cs` v1.1 |
| ERR-015-006 | Attacking AI #15 §1/§2/§3/§4 retain 7 stale `[CROSS-PENDING]` tags on `DOMAIN_TAG_ATTACKING_AI` after ERR-015-001 declared resolved; §9 checklist falsely claims "0 `[CROSS-PENDING]` remain" | Medium | 4 | ✅ Resolved May 18, 2026 — promoted all 7 hits to `[CROSS: #16 §3.4]` in §1 (4 instances), §2 FR-AT-005, §3 constant table, §4 §4.6 prose; v0.3 version-history rows added to all four section files |
| ERR-015-007 | Attacking AI #15 §3.13 Step 4 pseudocode `if isStable: continue` neither sets `agent.assignedRole` for stable agents nor counts stable RUNNERs/WEAK_SIDEs toward `runnerCount`/`weakSideCount` — non-stable agents are then assigned as if no stable holders existed, so MAX_RUNNERS and the single-WEAK_SIDE gate are enforced only retroactively by the §3.11 invariant pass | Medium | 1 | ✅ Resolved June 15, 2026 (impl AR-4) — folded into the ERR-015-009 single-pass rewrite: §3.13 / §3.3 Step 4 now evaluate every agent in one EntityId-ascending pass and count on the **committed** (post-hysteresis) role, so stable/retained RUNNERs and WEAK_SIDEs always seed the cap. Supersedes the initial two-pass patch (the single-pass form subsumes it). `RoleAssigner.cs` v1.2 |
| ERR-015-009 | Attacking AI #15 §3.12/§3.13/§3.3 use `isStable()` (dwellCounter ≥ ATTACK_DWELL_TICKS) as an evaluation gate (`if isStable: retain; continue`) — once an agent's role has been held `ATTACK_DWELL_TICKS` ticks it is never re-evaluated, so the `candidateDwell` transition machinery can never observe a newly-preferred role and the role is **permanently locked** for the rest of the possession (a SUPPORT_BALL agent stays SUPPORT_BALL after the ball moves 60 m away). Shared spec + implementation defect | High | 1 | ✅ Resolved June 15, 2026 (impl AR-4 H-1) — removed the is-stable short-circuit. Role-assignment is now a single always-evaluate pass; the §3.12 anti-thrash hysteresis lives entirely in `update()`'s `candidateDwell` (retains `currentRole` until a *different* candidate persists the dwell window). `isStable()` retagged diagnostic-only in spec + code. `RoleAssigner.cs` v1.2, `AttackHysteresis.cs` v1.2, `AttackHysteresisState.cs` v1.1, `section-3.md` §3.3/§3.12/§3.13 |
| ERR-015-010 | Attacking AI #15 §2.2.6 `AttackIntentSnapshot.intents` typed `ReadOnlySpan<AttackIntent>` — illegal as a field of a non-ref `readonly struct` (won't compile). The implementation worked around it with a raw `AttackIntent[] Intents` (Length = SQUAD_SIZE = 22) + a separate `IntentCount`, but the XML doc claimed "length IntentCount", so a consumer iterating `Intents.Length` reads stale/default entries past the valid count, and the raw array leaks the orchestrator's mutable buffer | Medium | 1 | ✅ Resolved June 15, 2026 (impl AR-5 M-1) — replaced with a bounded `ArraySegment<AttackIntent>` view (zero-alloc; `.Count` == valid count; consumers iterate that). Spec §2.2.6 struct + prose patched (`ReadOnlySpan` → `ArraySegment`). `AttackIntentSnapshot.cs` v1.1 |
| ERR-015-011 | Attacking AI #15 §2.3 / FR-AT-008 state a loose ball (carrier `null`/`-1`) MUST yield an empty directive, and `AttackingSnapshot`'s own doc says `-1` is treated as OUT_OF_POSSESSION — but `AttackingAITick.Tick` gated only on `PositioningAI.GetPhase()` and never checked `BallCarrierEntityId`. An IN_POSSESSION tick with carrier `-1` ran the pipeline against an undefined `BallCarrierPosition` (run-target origin §3.4, support radius §3.5) | Medium | 1 | ✅ Resolved June 15, 2026 (impl AR-5 M-2) — added the FR-AT-008 loose-ball guard in `Tick` (after the phase gate, before pool build): `BallCarrierEntityId < 0` → `SetEmpty` + return. `AttackingAITick.cs` v1.2 |
| ERR-015-008 | Attacking AI #15 §3.13 Step 10 pseudocode emits `validThroughTick = currentTick + 1`, contradicting the §2.2.2 `AttackIntent` data-structure contract ("equals currentTick") and the staleness rule (consumer treats `vt < currentTick` as stale) | Medium | 1 | ✅ Resolved June 15, 2026 (impl AR-4 M-1) — `section-3.md` Step 10 corrected to `validThroughTick = currentTick` with an inline §2.2.2 cite. Implementation `AttackingAITick.PublishIntents` already stamps `currentTick`; intra-spec contradiction removed |
| ERR-016-003 | Domain tag registry (#16 §3.4) silent gaps at `0x18` and `0x1C` — no `_RESERVED_0xNN_` placeholder rows; `0x18` orphaned when GK shifted to `0x1D`; `0x1C` block-end margin never documented | Medium | 1 | ✅ Resolved May 18, 2026 — `deterministic-sim/section-3.md` v1.0.6: `_RESERVED_0x18_` and `_RESERVED_0x1C_` placeholder rows added to §3.4 domain-tag table; v1.0.6 version-history row added |
| ERR-016-004 | Deterministic Sim #16 §3.2.5 `DeterministicRngService.Skip()` advanced only `RngCursor`, but draw values key on `ActionOrdinal` (bumped only by `Reserve`); `RngCursor` is not a hash input. A branch that took `Skip` instead of `Reserve` ended the draw-site evaluation with a different `ActionOrdinal` and desynced **every subsequent draw** on the stream — branch-safety silently broken. Implementation defect | High | 1 | ✅ Resolved June 15, 2026 (impl AR) — `Skip()` now advances `ActionOrdinal` (one consumed action) **and** `RngCursor`, and rejects an open reservation (`ERR_DS_RNG_BUDGET_MISMATCH`; signature `void`→`ushort`). `DeterministicRngService.cs` v1.3; new `DeterministicSimAdversarialRegressionTests` lock parity + open-reservation rejection |
| ERR-016-005 | Deterministic Sim #16 §3.2.3 `SnapshotCodec.Encode` computed `SHA-256(payload)` only — not the spec chained digest `SHA-256(0x12‖schema‖tick‖prevSnapshotDigest‖envFpDigest ‖ 0x11‖payload)`. The "digest chain" was not chained (altering an earlier snapshot left every later digest valid) and ignored the domain tags + header the golden corpus D-07 pins. `SerializeCanonicalCorpusTests` reconstructs the D-04..D-07 preimages by hand and never calls `Encode`, so the production divergence was untested (encode-not-catch pattern). Implementation + test-coverage gap | Medium | 2 | ✅ Resolved June 15, 2026 (impl AR) — `Encode` now builds the §3.2.3 header‖payload preimage (TransformBlock, no combined-buffer alloc); new `EnvironmentFingerprint.ComputeDigest()` supplies the 32-byte envFp slot. Bundled doc/semantic fixes (mirroring the perception L-cluster precedent): env mutation-guard over-claim corrected (readonly enforces immutability), `RngStreamState` `DrawIndex`/`BudgetRemaining` docs, `SaveManager.Load` storage-vs-schema error split, `TickOrchestrator` codec-owns-chain + AI-no-op doc. `SnapshotCodec.cs` v1.2, `EnvironmentFingerprint.cs` v1.1, `RngStreamState.cs`, `SaveManager.cs` v1.4, `TickOrchestrator.cs` v1.2; regression tests added |
| ERR-016-006 | Deterministic Sim #16 §4.2.2/§4.6.1 `SaveManager.Load` read only the payload and discarded the on-disk header, so replay's `ValidateHeader` / `ValidatePrevDigest` / cursor step-7 ran against a caller-supplied placeholder — the digest chain could not be verified across a process restart (a save→quit→reload could not detect a tampered/foreign snapshot). Compounded: `ReplayEngine` step 3 dereferenced `header.Fingerprint` (null on a disk-loaded header) → NRE. Implementation defect (foundation AR H-1/H-2/M-3) | High | 2 | ✅ Resolved June 16, 2026 — new `SaveManager.ReadHeaderBytes` + `Load(tick, headerOut, payloadOut)` overload reconstruct the header from disk (purely additive; on-disk format and the old payload-only `Load` delegate unchanged), so the chain is now verifiable on load; `ReplayEngine` step 3 fails closed (`ERR_DS_REPLAY_ENV_MISMATCH`) on a null fingerprint/live instead of NRE. `SaveManager.cs` v1.5, `ReplayEngine.cs` v1.3. Round-trip + chain test deferred to the Stage-0 file-I/O test-enablement follow-up (existing `DeterministicSimSaveLoadTests` file-I/O cases are `Assert.Ignore` at Stage 0) |
| ERR-016-007 | Deterministic Sim #16 §4.8 the on-disk snapshot header does NOT serialize the `EnvironmentFingerprint`, yet the fingerprint digest is part of the §3.2.3 digest preimage AND the §4.2.2 step-3 env-validation input. A snapshot reloaded in a fresh process therefore cannot recompute/verify its own digest or run step 3 (the disk-loaded header carries a null fingerprint; ERR-016-006 makes that fail closed). Wire-format gap (foundation AR M-4) | Medium | 2 | ⚠ Documented-open June 16, 2026 — fixing requires serializing the fingerprint (or its digest) into the on-disk header, which is a `SNAPSHOT_SCHEMA_VERSION` bump and would disturb the pinned `serialize-canonical-corpus.md` D-04/D-07 vectors + the #17 boot-wiring smoke digest; deferred to a gate-verified change (no local SDK in the remote review environment). Until then cross-process replay env-validation is blocked by design |
| ERR-016-008 | Deterministic Sim #16 §3.2.5 `DeterministicRngService.Reserve(stream, 0)` sets `BudgetRemaining = 0`, indistinguishable from "no reservation open", so a subsequent `Reserve`/`Skip` is not rejected (open-state is overloaded onto the count field); and `Skip(count)` ↔ sibling `Reserve(count)` branch parity is enforced only by caller convention — a mismatched `count` silently desyncs `RngCursor` (Tier-A snapshot state) and surfaces as a spurious HardDesync rather than a caught budget error. Implementation defect (foundation AR M-1/M-2) | Medium | 1 | ⚠ Documented-open June 16, 2026 — proposed fix: a dedicated `IsReserved` flag independent of the count (or reject `count <= 0` in `Reserve`), and derive `Skip`/`Reserve` budgets from the draw-site registration rather than the caller. Deferred with ERR-016-007 for a gate-verified RNG change |
| ERR-016-001 | Phantom interface risk in Deterministic Simulation §4.2 | Medium | 1 | ✅ Mitigated — §4.2 reclassified as non-normative sketches in v0.7 fix pass |
| ERR-016-002 | EntityId no-reuse cross-spec constraint not back-propagated to specs #2 and #8 | Medium | 3 | ✅ FULLY RESOLVED May 18, 2026 — (1) `XC-002-001` added to Agent Movement #2 §2.5 (v1.1.1, May 6, 2026); (2) `XC-008-001` added to Decision Tree #8 §1.7.3 (v1.1.1, May 6, 2026); (3) #16 §3.2.5 prose updated from "filed for back-propagation" to "back-propagated to #2 §2.5 and #8 §1.7.3" (confirmed landed per OBS-1 stress-test run 2, May 18, 2026). CLAUDE.md OPEN ISSUES entry removed. |
| ERR-017-001 | `DOMAIN_TAG_EVENT_LEDGER` allocation needed in Deterministic Simulation #16 §3.4 domain-tag table | Medium | 2 | ✅ FULLY RESOLVED. (1) #16-side May 14, 2026: `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in #16 §3.4 (v1.0.1 patch revision); §8.3.1 #17 row promoted to `complete`. (2) #17-side May 15, 2026 (§1.0.1 patch revision): `[CROSS-PENDING]` → `[CROSS]` promotion completed across §3.4.2 / §3.10 / §1.4 / §2.4.4 / §7.5 D9 / §8.1.4 / §8.3.4 / §8.4 / §9.2 Q10 / §9.3 R3; Appendix B byte streams and Appendix D glossary now carry the literal value `0x15`. |
| ERR-017-002 | Event System #17 §3.2.1/§3.2.2 specified three `Publish<T>`/`Subscribe<T>` overloads distinguished ONLY by generic constraint (`IEventA`/`IEventB`/`IEventC`) — illegal C# (CS0111: constraints are not part of a method signature); `EventBus.cs` and five spec `EventBusStub.cs` files implemented it verbatim, so the event-system production assembly never compiled | High | 8 | ✅ RESOLVED June 12, 2026 (same day; found by the first-ever compile on the dotnet CI gate, `tools/dotnet-ci/`). Spec §3.2.1/§3.2.2 patched to a single `where T : struct` method with cached tier-marker dispatch (section-3.md v1.0.2); code: `EventBus.cs` v1.9, new `EventTierCache.cs` v1.0, `CosmeticChannel.cs` v1.9 (`SubscribeFromBus` seam), 5× `EventBusStub.cs` merged to a single forwarder. Call sites unchanged; FR-EVT-009a exactly-one-marker contract enforced at the entry point. Adjacent boot-order fix: `EventRegistry.EnsureInitialized()` (v1.5) — `EventOrdinalCache<T>` reads never triggered the seeded-row cctor. |
| ERR-017-003 | Event System #17 §3.2.1 `EventBus.Publish<T>` enforces the registered producer phase only under `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` (a `Debug.Assert`); in a release/certification build a Tier A/B event published from the wrong phase is accepted, and `PublishAuthoritative` stamps the FM-017-002 sort key with the *actual* current phase rather than the registered producer phase. Determinism holds within one build config, but a debug run and a release run of the same scenario can produce different canonical orderings/digests if any producer is mis-phased — defeating the cross-environment digest contract. Implementation defect (foundation AR, event-system) | Medium | 1 | ⚠ Documented-open June 16, 2026 — proposed fix: promote the producer-phase comparison to an unconditional guard (the data — `GetProducerPhaseIndex(ordinal)` — is already available). Deferred (not applied blind): the change is digest-sensitive (it gates which publishes reach the ledger and could alter the pinned #17 boot-wiring smoke digest), and the remote review environment has no .NET SDK to run the gate. Apply with CI verification |
| ERR-018-001 | Performance Optimization #18 `outline-detailed.md` cites Deterministic Simulation #16 sections by stale numbers / non-existent name (`#16 §7 regression scenarios`, `#16 §5 canonical save format`, `#16 §8 trace channels`) | Medium | 1 | ✅ Resolved at outline level — May 13, 2026 (same day as filing). `outline-detailed.md` v1.1 (a) inverts KD-3 (Spec #18 owns the trace pipeline; Spec #16 retains authority over canonical record format §3.2.4.1, regression scenarios §5, and determinism-of-emission constraints / veto authority over tick-pipeline trace points §3.1), and (b) corrects every `TBD-NORMATIVE`-marked #16 section-number citation against current `deterministic-sim/section-*.md`. Rationale for inversion: trace channels are an observability concern, not a determinism concern; mirrors KD-4 (#19 owns testing infrastructure, consumes #16 scenarios). New FR-PO-058a in §3.8.3 enforces determinism-of-emission for every #18-emitted trace point. Section files drafted from v1.1 will not inherit the drift. Architectural concern (re-anchor vs invert) is closed; section-file authoring still required to faithfully implement inverted KD-3 (FR-PO-058a in §3.8.3, #16-owner sign-off audit in §5.7, record-format binding in §3.8.4). |
| ERR-018-002 | `[HotPathAllocExempt]` attribute cited in #18 as "declared in Spec #20 §3" but does not exist in `code-standards/` | High | 5 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.7.5 declares governance identifier in #18; Spec #20 §3 cited as policy authority only; C# attribute deferred to Stage 0+1 |
| ERR-018-003 | MUST/MAY conflict between FR-PO-067 (§2.2.9) and §3.4.4 on baseline-reproducibility re-run | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.4.4 "MAY" → "MUST" |
| ERR-018-004 | Three-way stage-of-resolution contradiction on +5% threshold: FR-PO-031 "Stage 0+1" vs §7.5 D9 "Stage 1" vs §7.1 Stage 0+1 deliverable | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §7.5 D9 "Stage 1" → "Stage 0+1" |
| ERR-018-005 | Channel registry schema absent from Appendix F; §3.8.2 "Stage 0 declares schema" obligation unmet; F.1/F.2/F.4 reference `perf.budget`/`perf.alloc` channels without registry backing | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): Appendix F.0 channel registry schema added |
| ERR-018-006 | Hot-path allocation budget = 0 bytes/tick tagged `[GT]` in §3.10 instead of `[FIXED]` — not a designer-tunable value | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.10 and §8.4 tags updated `[GT]` → `[FIXED]` |
| ERR-018-007 | Three Spec #19 body-text citations missing `TBD-NORMATIVE` tag and absent from §9.4.1 blocker list: §3.4.3 ("per Spec #19 §3.4.3"), §3.3.5 ("parallel Spec #19 §6.1"), §3.9.5 ("Spec #19 §3.1") | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): TBD-NORMATIVE added to all three citations; §9.4.1 blocker list extended |
| ERR-018-008 | §3.9.1 ±20% `[EST]`→`[GT]` promotion tolerance untagged; not in §3.10 constants catalogue (CLAUDE.md requires source tag on every constant) | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): `[GT]` tag added inline; §3.10 and §8.4 rows added |
| ERR-018-009 | FR-PO-070 (Stage 0 MUST) requires `tools/run-perf-local.sh` to invoke `tools/budget-auditor.py`, which is a Stage 0+1 deliverable per §7.1 — bootstrapping contradiction | Medium | 2 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): FR-PO-070 stage column updated to "Stage 0 (manual) / Stage 0+1 (automated)" with qualifier note |
| ERR-018-010 | Appendix F.1 `N=100` captures `[GT]` and Appendix F.5 1% flake-rate threshold are governance constants absent from §3.10 catalogue; F.5 threshold also untagged | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.10 and §8.4 rows added; F.5 threshold tagged `[GT]` |
| ERR-018-011 | `SPEC_INDEX.md` row 18 still shows `IN PROGRESS`; #18 §9.4 prematurely declares `IN REVIEW` (canonical registry contradicted per CLAUDE.md "SPEC_INDEX.md is the canonical source of truth") | Medium | 3 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): SPEC_INDEX.md row 18 updated to `IN REVIEW`; CLAUDE.md and file-manifest.md updated atomically |
| ERR-018-012 | Appendix F has two `### F.0 Channel Registry Schema` sections (lines 231 and 258) with conflicting field sets (13 fields vs 7 fields, different names — `owning_subsystem` vs `subsystem_owner`, `inside_tick_pipeline`+`sign_off_log_ref` vs `emission_veto_required`) | High | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): kept canonical 13-field F.0; merged in `perf.budget`/`perf.alloc`/`perf.trace` anchor rows from the duplicate as Stage 0 illustrative entries. Root cause: PR #59 + PR #60 parallel-branch merge of independent ERR-018-005 fixes |
| ERR-018-013 | `section-3.md` §3.10 Constants Catalogue has three pairs of duplicate-constant rows: ±20% promotion tolerance (565↔572), N=100 dashboard window (566↔573), 1% flake threshold (567↔574) | High | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): deleted the three v0.1 rows; kept the v0.2 rows with richer rationale. Root cause: same PR #59 + PR #60 merge collision as ERR-018-012 |
| ERR-018-014 | Seven section files (section-2 / 3 / 5 / 7 / 8 / 9 + appendices) carry duplicate v0.2 version-history rows sandwiching the v0.1 row | Medium | 7 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): consolidated each pair into a single v0.2 row carrying the union of fix-list notes; v0.3 row appended below |
| ERR-018-015 | `section-1.md` header `Last Updated: May 13, 2026` is stale vs its own v0.2 row dated May 14, 2026 (every other section file's header is May 14) | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): header updated to `May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)` |
| ERR-018-016 | `section-3.md` §3.5.2 Shot Mechanics example conflates the +5% per-PR gate (vs measured pre-PR baseline) with the ±20% `[EST]`→`[GT]` promotion tolerance from §3.9.1 — invokes the +5% gate against an un-promoted spec-time anchor | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): example rewritten to apply ±20% promotion tolerance at first capture, then +5% (or per-spec tighter override) for subsequent per-PR captures |
| ERR-018-017 | FR-PO-019 levels `MAY` but its statement embeds an unconditional MUST ("manifest ID and seed MUST be recorded the same way") — same structural shape as ERR-018-003 | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): split into FR-PO-019 (MAY: cross-scenario profiling is permitted) and FR-PO-019a (MUST: manifest ID and seed MUST be recorded per FR-PO-016) |
| ERR-018-018 | §3.7.5 pre-specifies a C# attribute signature (`Method | Constructor` targets, `string rationale` constructor argument) at spec-time without a specified consumer — phantom-interface trap per CLAUDE.md "Interface Design Principle" (ERR-001 / ERR-004 hazard) | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): §3.7.5 deferred concrete C# signature to Stage 0+1 alongside §7.5 D2 alloc-tracker pin; retained governance contract (rationale, sign-off, source-level marker) which is signature-independent |
| ERR-013-001 | Pressing AI #13 requires a back-prop into Decision Tree #8 §2.2.6 to add `PressDirective?` field to `TacticalContext`. Option B selected. | Medium | 2 | ✅ **Resolved May 17, 2026** — `decision-tree/section-2-1-to-2-2.md` v1.1.1: nullable `PressDirective?` field added to `TacticalContext` struct (null at Stage 0; #13 writes at Stage 1+; DT reads for PRESS utility §3.2.7). |
| ERR-013-002 | Pressing AI #13 requires `PRESS_TRIGGERED` channel registration in Event System #17 §3.10 channel registry. Channel emitted when a `PressDirective` becomes non-empty (non-trivial press fires). | Low | 1 | Open (Stage 1) — filed May 17, 2026 from #13 section-files v0.1. Non-blocking for #13 Stage 0 spec text per KD-11 ("no #17 channels at Stage 0"). Lands at Stage 1 first commit per #18 Appendix F.0 / §7.2. |
| ERR-013-003 | Pressing AI #13 requires `PRESS_DISENGAGED` channel registration in Event System #17 §3.10 channel registry. Channel emitted when a `PressDirective` returns to all-`HOLD_SHAPE` after a non-trivial press. | Low | 1 | Open (Stage 1) — filed May 17, 2026 from #13 section-files v0.1. Non-blocking for #13 Stage 0 spec text per KD-11. Lands at Stage 1 first commit per #18 Appendix F.0 / §7.2. |
| ERR-013-004 | Stale "Fatigue System #13" reference at `decision-tree/section-3-1.md` L753 — but #13 is Pressing AI. | Minor | 1 | ✅ **Resolved May 17, 2026** — one-token patch: "Fatigue System #13" → "Pressing AI #13" at `decision-tree/section-3-1.md` L753. |
| ERR-013-005 | `DOMAIN_TAG_PRESSING_AI = 0x19` allocation needed in Deterministic Simulation #16 §3.4. | Medium | 1 | ✅ **Resolved May 17, 2026** — allocated in `deterministic-sim/section-3.md` v1.0.3 (`0x17` reserved for #12, `0x18` for #11, `0x19` for #13); #13 §6.1 `[CROSS-PENDING]` → `[CROSS]` atomically. |
| ERR-013-007 | Pressing AI #13 requires `GetPhase(TeamId)` as a Stage 1 accessor on Positioning AI #12. | Medium | 2 | ✅ **Resolved May 17, 2026** — declared in `positioning-ai/section-4.md` §4.5.1 v0.3 patch as Stage 1 publication commitment. |
| ERR-013-008 | Pressing AI #13 requires `GetLine(EntityId)` elevated from Stage 1+ to Stage 1 on Positioning AI #12. | Medium | 2 | ✅ **Resolved May 17, 2026** — declared in `positioning-ai/section-4.md` §4.5.1 v0.3 patch; `GetLine` elevated Stage 1+ → Stage 1. |
| ERR-013-009 | Pressing AI #13 §3.1.2 `BACKWARD_PASS` dotted the pass direction against `attackingDirection` (the **pressing** team's), but "backward" is backward for the team **in possession**, which attacks the opposite goal. The trigger therefore fired on the possessing team's *forward* pass (home/away inversion class — AR-3 implementation review). It also did not exclude a pressing-team passer. | High | 2 | ✅ **Resolved June 15, 2026** — `pressing-ai/section-3.md` v0.4 §3.1.2: pseudocode + worked example use `-attackingDirection` (possessing team's forward) and add an own-team-passer guard; implementation `TriggerEvaluator.cs` v1.3 matches; tests T-U-002 re-derived + new own-team-passer guard test. |
| ERR-013-010 | Pressing AI #13 §3.4 `receiverProgressionGain` dotted against `attackingDirection` (the **pressing** team's), rewarding receivers retreating toward their own goal as most threatening — same inversion as ERR-013-009. | High | 2 | ✅ **Resolved June 15, 2026** — `pressing-ai/section-3.md` v0.4 §3.4: formula + worked example use `-attackingDirection`; implementation `CoverShadowSelector.cs` v1.3 matches; T-U-031 fixture re-derived in the corrected frame. Zone/third frames (§3.8/§3.9) unchanged — those correctly use the pressing team's direction. |
| ERR-020-001 | Code Standards #20 §4.2 `[CROSS]` mirror example uses ALL_CAPS field name (`PHYSICS_TICK_HZ`) — contradicts §3.2.3 PascalCase rule for `[CROSS]` constants. | Minor | 2 | ✅ **Resolved May 22, 2026** — `code-standards/section-4.md` v1.0.1: mirror field renamed `PHYSICS_TICK_HZ` → `PhysicsTickHz`; XML doc updated with spec+section citation. `src/CLAUDE.md` v1.4: discrepancy note updated with ERR-020-001 reference. |
| ERR-021-005 | Dismarking AI #23 back-prop: `TeamTactic` gains `DismarkIntensity` (`Off = 0` identity) + Appendix B canonical-order row (after `MarkingOrientation`); `WriteTeamTactic` coverage + `SNAPSHOT_SCHEMA_VERSION` bump land with #23's wiring stage | Medium | 2 | ✅ Resolved July 10, 2026 — filed and landed atomically with #23 `APPROVED`: `tactical-instructions/section-2.md` v0.5 field row + `appendices.md` v0.5 Appendix B append |
| ERR-021-006 | Build-Up Structures #24 back-prop: `TeamTactic` gains `BuildUpStructure` (`None = 0` identity) + Appendix B row (after `DismarkIntensity`) | Medium | 2 | ✅ Resolved July 10, 2026 — filed and landed atomically with #24 `APPROVED`: same v0.5 pair as ERR-021-005 |
| ERR-021-007 | Positional Rotations #25 back-prop: `TeamTactic` gains `RotationFreedom` (`Off = 0` identity) + Appendix B row (after `BuildUpStructure`) | Medium | 2 | ✅ Resolved July 10, 2026 — filed and landed atomically with #25 `APPROVED`: same v0.5 pair as ERR-021-005 |
| ERR-012-007 | Dismarking AI #23 back-prop: #12 `SlotComposer` pipeline gains the dismark offset stage between spacing and pitch clamp (order pinned by FR-DM-008; identity no-op at `Off`) | Medium | 1 | ✅ Resolved July 10, 2026 — `positioning-ai/section-3.md` v0.6 new §3.7.1 (combined #23/#24 stage order cited from #23 §4.2 / #24 §4.2) |
| ERR-012-008 | Build-Up Structures #24 back-prop: #12 `SlotComposer` pipeline gains the build-up overlay stage between `ContextModifier` and spacing + per-team `BuildUpZoneState` classifier state (identity no-op at `None`) | Medium | 1 | ✅ Resolved July 10, 2026 — `positioning-ai/section-3.md` v0.6 §3.7.1 |
| ERR-012-009 | Positional Rotations #25 back-prop: #12 contract amendment — `RotationController` runs before slot composition, and `AgentPositioningData.SlotIndex` is no longer immutable after `SeedFromFormation` (the `RotationController` is its sole post-seed writer; single-writer rule per #25 §4.4) | Medium | 1 | ✅ Resolved July 10, 2026 — `positioning-ai/section-3.md` v0.6 §3.7.1 (numbers ERR-012-004..006 deliberately skipped — soft-reserved by the June-13 dotnet-CI quarantine adjudication cluster, whose ERR-012-003 citation is already live in section-3.md v0.5) |
| ERR-008-012 | Dismarking AI #23 back-prop: #8 §3.2 `UtilityScorer` gains the FM-DM-03 marked-pass-target multiplier row in the external tactical-multiplier product, applied before the single final clamp (identity ×1.0 at `Off`) | Medium | 1 | ✅ Resolved July 10, 2026 — `decision-tree/section-3-2.md` v1.5 §3.2.2.1 back-prop anchor note; #23 owns formula/constants/tests |
| ERR-008-013 | GK/Heading #11/#10 integration: #8 gains a DT-emitted `SAVE` action (ordinal 7) — the goalkeeper save the #11 `SaveIntent` doc always anticipated the DT committing. Supersedes the `MatchEngine` heuristic save trigger. Off-ball-branch-only, gated on a new `TacticalContext.SaveAvailable` fact (set only under the opt-in `EnableGkHeading` flag); emitted as the SOLE off-ball option so selection is robust; `PlayerTacticActionMultiplier` exempts SAVE (its #21 tables are 7-wide) | Medium | 1 | ✅ Resolved July 23, 2026 — see the ERR-008-013 detailed section; `decision-tree/section-2.md`/`section-3-1.md`/`section-3-2.md`/`section-3-5.md` notes; code landed (`ActionType.cs` v1.1, `OptionGenerator`/`UtilityScorer`/`ActionDispatcher`/`DecisionTree`/`IDtSaveDispatch`, `MatchEngine.cs` `HostSaveDispatch`) |
| ERR-024-001 | Build-Up Structures #24 Appendix A v0.2 keyed every overlay row to lane values no slot occupies (fullbacks recorded `LH`/`RH` in every family table, not wide L/R; central mids `C`, not LH/RH) — the whole FR-BU-007 catalogue was a structural no-op; the PASS-1 M-3 "correction" checked lane geometry, not the recorded `DefaultLane` key values | High | 3 | ✅ Resolved July 10, 2026 (T0 implementation) — `build-up-structures/appendices.md` v0.3 + `section-3.md` v0.3 re-keyed to the recorded values (magnitudes/intents unchanged); `BuildUpOverlayCatalogue.cs` v1.0 implements the corrected keys; regression test locks non-zero own-third coverage in every family |
| ERR-022-001 | Living World #22 back-prop: `DOMAIN_TAG_LIVING_WORLD = 0x1E` + `SubsystemOrdinals.LivingWorld = 80` (first entry of the off-pitch 80–99 band) allocation needed in Deterministic Simulation #16 §3.4 for the `world.text` / `world.arcs` sub-streams. | Medium | 1 | ✅ Resolved July 22, 2026 — the `0x1E` / `80` allocation landed in code with #22's slice-3 wiring (`DeterministicSimConstants` / `SubsystemOrdinals`); the #16 §3.4 spec-text row + this ERR were filed retroactively (the code back-prop had preceded the doc back-prop). Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-027-002 | Squad/Player Data Layer #27 back-prop: `RosterGenerator` gained an **additive** supplied-position `Generate` overload and `PlayerDatabaseConstants` gained `POSITION_COUNT`, both landed in code with the league bootstrap (path-to-playable A3) but neither recorded in the #27 spec text — which still described the uniform position draw as the only path and omitted the new constant from its catalogue. | Medium | 1 | ✅ Resolved July 25, 2026 — `section-2.md` gains **FR-SQ-012a** (the overload; identical 36-draw budget, position draw made and discarded, so FR-SQ-012's path stays byte-identical) + `POSITION_COUNT` in the §2.2.5 catalogue; `section-3.md`'s draw table annotates draw 3 and retires the stale "a realistic few-GK distribution is future work" framing (the template overload IS that refinement, shipped); `appendices.md` gains the `POSITION_COUNT` row. No behaviour change to the drawn-position path and no RNG-budget change, so no `[CROSS]` or determinism impact. |
| ERR-027-001 | Squad/Player Data Layer #27 back-prop: `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` + `SubsystemOrdinals.PlayerDatabase = 81` allocation needed in Deterministic Simulation #16 §3.4 (the `RosterGenerator` RNG stream, KD-5). | Medium | 1 | ✅ Resolved July 22, 2026 — allocated in `deterministic-sim/section-3.md` §3.4 (`0x1F`, next after `DOMAIN_TAG_LIVING_WORLD = 0x1E`); the code allocation (`DeterministicSimConstants.DOMAIN_TAG_PLAYER_DATABASE` / `SubsystemOrdinals.PlayerDatabase`) landed with #27 T0; #27 Appendix A `[CROSS]` cross-cite confirmed. Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-028-001 | Player Progression & Lifecycle #28 back-prop: promote `_RESERVED_0x20_` → `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` + `SubsystemOrdinals.PlayerProgression = 82` in Deterministic Simulation #16 §3.4 (the per-club `player-progression.regen` regen/newgen RNG stream, siteId `player-progression.regen`, `entityId = clubId`; FR-PG-020 / KD-3). | Medium | 1 | ◑ Spec-text promoted July 23, 2026 at #28 section-file approval — `deterministic-sim/section-3.md` §3.4 promotes the former `_RESERVED_0x20_` placeholder to the `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` row (aging/decline/growth is a pure integer projection and registers no stream — `0x20` covers regen generation only). **Like ERR-030-001 (spec-text-first), this row PRECEDES the code:** the code const (`DeterministicSimConstants.DOMAIN_TAG_PLAYER_PROGRESSION` / `SubsystemOrdinals.PlayerProgression`) + the per-club RNG-stream registration land at **#28 T2** with the first regen (registering a stream with zero draw sites now would be the phantom-surface class FR-LW-031 avoids, the `world.arcs` precedent). Pure namespace promotion; no `DETERMINISM_DIGEST_VERSION` bump. Fully resolves when the T2 code const lands. |
| ERR-029-001 | Training System #29 determinism note: confirm at #29 section-file approval whether `_RESERVED_0x21_` / `SubsystemOrdinals.Training = 83` is promoted (FR-TR-008 / KD-6). | Low | 0 | ✅ RESOLVED July 23, 2026 at #29 section-file approval — **NO promotion.** #29 was authored + APPROVED and confirmed **fully deterministic**: conditioning / training-fatigue / growth-input are pure integer projections, and per-player variation is a deterministic function of the player's own attributes, so #29 registers **no** RNG stream. Unlike ERR-028-001 (`0x20`/#28, whose regen is a genuine draw site), #29 has no #29-owned stochastic outcome — growth flows through #28's deterministic curve; injury variation is #41's. Promoting `0x21` to a named tag with a zero-draw stream would be the phantom-surface class FR-LW-031 forbids (`world.arcs` precedent). `deterministic-sim/section-3.md` §3.4 (v1.0.10) updates the `_RESERVED_0x21_` row rationale to record this; the reservation **stands** (no code const, no new row, no `DETERMINISM_DIGEST_VERSION` bump). A future stochastic training extension would promote it at that first draw site. |
| ERR-030-001 | Season & Competition Loop #30 back-prop: `DOMAIN_TAG_SEASON_LOOP = 0x22` + `SubsystemOrdinals.SeasonLoop = 84` allocation needed in Deterministic Simulation #16 §3.4 (the season RNG sub-stream, siteId `season-loop.season-events`; FR-SN-027 / KD-5). | Medium | 1 | ◑ Spec-text reserved July 22, 2026 at #30 section-file approval — `deterministic-sim/section-3.md` §3.4 gains the `DOMAIN_TAG_SEASON_LOOP = 0x22` row (v1.0.8) + the two reserved-pending-promotion placeholders `_RESERVED_0x20_`/`_RESERVED_0x21_` (#28/#29, roadmap §6 contiguous block; #30 reached the catalogue first as Wave 1). **Unlike ERR-022/027-001 (code-first), this row PRECEDES the code:** the code const (`DeterministicSimConstants.DOMAIN_TAG_SEASON_LOOP` / `SubsystemOrdinals.SeasonLoop`) + the RNG-stream registration land at **#30 T2** with the first draw site (the FR-SN-013a quick-sim round-resolution model) — registering a stream with zero draw sites now would be the phantom-surface class FR-LW-031 avoids (the `world.arcs` precedent). Pure namespace reservation; no `DETERMINISM_DIGEST_VERSION` bump. Fully resolves when the T2 code const lands. |
| ERR-030-002 | Season & Competition Loop #30 back-prop (at Injuries & Medical #41 approval): the KD-2 day-advance tick order (`season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder`) enumerated null seams for #28/#29/#33 only (FR-SN-034, authored before #41). #41 needs a per-day world-tick step (recovery countdown + occurrence draw). | Low | 1 | ✅ Resolved July 23, 2026 at #41 approval — the tick order gains an **injuries null seam as step 4** (after #28/#29 so the occurrence-risk assembly reads the day's updated training-fatigue/condition; before the live `WorldStore.AdvanceDay()` tick), and FR-SN-034's enumeration + the "documented positions" prose extend to #41. Doc-only re-pin of a documented position (no interface, no code — the seam is empty until #41 T2 wires `AdvanceMedicalDay`); the world-floor byte-identity (FR-SN-026) is unaffected since the seam is null. |
| ERR-030-003 | Season & Competition Loop #30 back-prop (at Club Finances & Economy #40 approval): the season-boundary roll (`season-competition-loop/section-3.md` §3.5 `RollToNextSeason`, FR-SN-029/031) needs a finance-settlement step; FR-SN-031 reserved an insertion point for #43 promotion/relegation only. | Low | 1 | ✅ Resolved July 23, 2026 at #40 approval — the boundary roll gains a **finance-settlement null seam at step (b')**, positioned **after** the (a') #43 promotion/relegation insertion point (budget depends on post-promotion division) and **before** (c) regenerate; FR-SN-031 now enumerates both insertion points. Doc-only re-pin of a documented position (no interface, no code — the seam is empty until #40 T2 wires `SettleFinances`); the transform stays a pure function of `SeasonState + nextSeed` (FR-SN-029 restartable contract preserved). |
| ERR-030-004 | Season & Competition Loop #30 back-prop (at Transfers, Contracts & Negotiation #31 approval): the KD-2 day-advance tick order (`season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder`) enumerated null seams for #28/#29/#33/#41 only (FR-SN-034, authored before #31). #31's deep-tier daily negotiation/rival-bid processing needs a world-tick step. | Low | 1 | ✅ Resolved July 23, 2026 at #31 approval — the tick order gains a **transfers null seam as step 5** (after injuries, before the live `WorldStore.AdvanceDay()` tick, which becomes step 6), and FR-SN-034's enumeration + the "documented positions" prose extend to #31. A **deep-tier position reservation** (the ERR-030-002 #41 precedent): minimal #31 transfers are command-driven (`SubmitBid`), so the seam is **empty even after #31 T-phase minimal**; it fills at the deep tier. Doc-only re-pin (no interface, no code); the world-floor byte-identity (FR-SN-026) is unaffected (null seam). No #16 change — #31 is draw-free, so `_RESERVED_0x23_`/85 stays reserved. |
| ERR-030-006 | Season & Competition Loop #30 back-prop (at Staff & Backroom #34 approval): the KD-2 day-advance tick order (`season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder`) enumerated null seams for #28/#29/#33/#41/#31 only (FR-SN-034, authored before #34). #34's deep-tier daily candidate-pool / in-flight-hiring processing needs a world-tick step. | Low | 1 | ✅ Resolved July 23, 2026 at #34 approval — the tick order gains a **staff null seam as step 6** (after transfers, before the live `WorldStore.AdvanceDay()` tick, which becomes step 7), and FR-SN-034's enumeration + the "documented positions" prose extend to #34. A **deep-tier position reservation** (the ERR-030-002 #41 / ERR-030-004 #31 precedent): #34's scaffold projections are pull-based (threaded into #29/#41 when their inputs are built), so the seam is **empty even after #34 T-phase scaffold**; it fills at the deep tier. Doc-only re-pin (no interface, no code); the world-floor byte-identity (FR-SN-026) is unaffected (null seam). No #16 change — #34's scaffold is draw-free, so `_RESERVED_0x26_`/88 stays reserved. **ERR-030-005 is soft-reserved by #31** (its deferred `RequestRosterCommit` build), so #34 takes 006. |
| ERR-038-001 | UI / Client Framework #38 §3.3 / §4.1 / §5.1 (T-UI-DISPATCH-004) specify live-match command marshaling as a new `LiveMatchStreamer.EnqueueIntent`. That would give the SHARED streamer a mutation surface — regressing the browser viewer's playback-only invariant that `interactive-unity-client-design.md` AR-1 H-2 (July 23, one day after #38's approval) had already rejected, and that `LiveMatchServer` relies on by construction (it holds a streamer, never a `MatchEngine`). | Medium | 1 | ✅ Resolved July 25, 2026 at #38 T0 — the framework marshals through the already-shipped `ManagerCommandQueue` + `MatchClientDriver` pre-tick drain instead (`MatchTacticsDispatcher`, live mode). FR-UI-023's requirement is met identically (intent applied between ticks by the thread that owns the engine); only the mechanism differs, and `LiveMatchStreamer` gains no mutation surface. Spec §3.3/§4.1/§5.1 to be re-anchored to the shipped mechanism at next #38 revision. |
| ERR-038-002 | UI / Client Framework #38 §4.1 states the generic substrate "references nothing sim-side", but §2.2 gives `ManagerIntent` a `TeamTactic` / `PlayerTactic` payload (`TacticalDirector.TacticalInstructions`) and the substitution payload needs `SubstitutionReason` (`TacticalDirector.MatchEngine`). The two statements cannot both hold literally. | Low | 1 | ✅ Resolved July 25, 2026 at #38 T0 — the assembly references both (config/enum value types only). The invariants that actually carry the layer contract are FR-UI-001 (no sim/loop assembly references the UI — preserved, and mechanically locked by T-UI-LAYER-001) and FR-UI-003 (the framework provides no mutation path of its own — preserved). §4.1's wording to be re-anchored to those two at next #38 revision. |
| ERR-038-003 | UI / Client Framework #38 §3.2 pins `Register(reg): registry[reg.Id] = reg` — an assignment, i.e. a silent overwrite of an already-registered `ScreenId`. Overwriting swaps a live screen's view-model source / dispatcher underneath a navigation stack that still references that id. | Low | 1 | ✅ Resolved July 25, 2026 at #38 T0 — `NavigationShell.Register` refuses a duplicate id (`ArgumentException`), consistent with the shell's other fail-loud transitions (F2 unregistered navigation, root `Pop`). Locked by `NavigationShellTests.DuplicateRegistration_Throws`. §3.2 pseudocode to be re-anchored at next #38 revision. |
| ERR-040-001 | Club Finances & Economy #40: `_RESERVED_0x29_` / `SubsystemOrdinals.ClubFinances = 91` reservation in Deterministic Simulation #16 §3.4 (roadmap §6 off-pitch block). | Low | 1 | ✅ Resolved July 23, 2026 at #40 section-file approval — `deterministic-sim/section-3.md` §3.4 (v1.0.12) gains the `_RESERVED_0x29_` placeholder row, **RESERVED not promoted** (the #29 `_RESERVED_0x21_` precedent): #40's minimal tier is a pure integer `budget = f(finalTablePosition, prizeMoney)` projection with no draw, so it registers no stream, and a named tag with a zero-draw stream would be the phantom-surface class FR-LW-031 forbids. Promotes to `DOMAIN_TAG_CLUB_FINANCES = 0x29` at #40 T3's first stochastic sponsorship/revenue draw (keyed on `(clubId, seasonNumber, purpose)`). No code const; no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-030-007 | Season & Competition Loop #30 back-prop (at Youth Academy & Intake #42 approval): the KD-2 day-advance tick order (`season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder`) enumerated null seams for #28/#29/#33/#41/#31/#34 only (FR-SN-034, authored before #42). #42's periodic youth-intake step needs a world-tick slot. | Low | 1 | ✅ Resolved July 24, 2026 at #42 approval — the tick order gains an **academy null seam as step 7** (after staff, before the live `WorldStore.AdvanceDay()` tick, which becomes step 8), and FR-SN-034's enumeration + the "documented positions" prose extend to #42. **Unlike the #31/#34 deep-tier position reservations, this seam goes live at #42's own T2** (the intake is #42's minimal tier, not a deep-tier addition) — but it is a **one-shot latched on `LastIntakeWorldDay`** (#42 KD-4 / FR-YA-014), so on every day but one per intake period it costs two integer comparisons and a return, and the FR-SN-026 world-floor byte-identity is unaffected while the seam is null. Doc-only re-pin (no interface, no code). No #16 change — #42 registers no stream until its first intake at T2, so the roadmap-§6 `0x2B`/93 reservation stays unpromoted (FR-LW-031). |
| ERR-030-008 | Season & Competition Loop #30 back-prop (at Board & Ownership Dynamics #45 approval): the KD-2 day-advance tick order (`season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder`) enumerated null seams for #28/#29/#33/#41/#31/#34/#42 only (FR-SN-034, authored before #45). #45's daily board-confidence step needs a world-tick slot. | Low | 1 | ✅ Resolved July 25, 2026 at #45 approval — the tick order gains a **board null seam as step 8** (after the #42 academy seam, before the live `WorldStore.AdvanceDay()` tick, which becomes step 9), and FR-SN-034's enumeration + the "documented positions" prose extend to steps 1–8 / #45. **Like #42's and unlike the #31/#34 deep-tier position reservations, this seam goes live at #45's own T2** (the daily confidence drift is #45's minimal tier) — but it costs one bounded integer drift per **modelled** club, and the minimal tier models the managed club only, so the FR-SN-026 world-floor byte-identity is unaffected while the seam is null. Doc-only re-pin (no interface, no code). |
| ERR-030-009 | Season & Competition Loop #30 back-prop (at #45 approval): FR-SN-014 / §2.2 `BoardState` held a **job-security scalar** as independent state. Once #45 owns a persistent per-club board-confidence scalar, these are **two truths for one quantity** — they diverge at the first restore with nothing to detect it. `JobSecurity` was also typed *"float/enum"*, the last `float` in an otherwise integer-per-mille management layer, sitting inside a round-trip-deterministic save block. | Medium | 2 | ◑ Spec-text amended July 25, 2026 at #45 approval — FR-SN-014, the §2.2 `BoardState` entry, and §3.6's `WriteBoard` now record that from **#45 T2** `JobSecurity` is a **derived `JobSecurityBand`** (a `u8` enum) projected on read from #45's confidence, not independent state (section-2 v0.8 / section-3 v0.8). #30 keeps **sole ownership of `BoardObjective`** and of the season-boundary pass/fail evaluation; only the job-security half becomes a projection. **Spec-text-first (the ERR-028-001 pattern):** the text lands at approval, the *effect* — and its **`SEASON_STATE_FORMAT_VERSION` bump** — land at #45 T2. Pre-T2 saves are then rejected fail-loud with **no migration**, matching the living-world slice-2 posture; cross-version migration is #50's subject. This is the one non-additive consequence of #45's approval and is recorded as such in #45 §1.5 KD-5 / §7.4 R-1. Fully resolves when the T2 representation change lands. |
| ERR-045-001 | Board & Ownership Dynamics #45: `_RESERVED_0x2D_` / `SubsystemOrdinals.BoardOwnership = 95` reservation needed in Deterministic Simulation #16 §3.4 (roadmap §6 off-pitch block) — **widened during #45's pre-approval verification** to also cover `0x2B` (#42) and `0x2C` (#43), which had no placeholder. | Low | 1 | ✅ Resolved July 25, 2026 at #45 section-file approval — `deterministic-sim/section-3.md` §3.4 (v1.0.14) gains **three** placeholder rows, all **RESERVED not promoted**: `_RESERVED_0x2B_` (#42, ordinal 93 — its `youth.intake` draw site does not exist until #42 T2), `_RESERVED_0x2C_` (#43, 94 — unauthored; cup draws are a documented future draw site), and `_RESERVED_0x2D_` (#45, 95 — #45's minimal tier is a draw-free integer projection, the #29 `_RESERVED_0x21_` / #40 `_RESERVED_0x29_` precedent). **Why three:** #16's **A-04 every-gap-has-a-placeholder rule** was violated when #42's approval deferred promoting `0x2B` without filing its placeholder, leaving the catalogue ending at `0x2A`; filing only #45's `0x2D` would have left two unmarked gaps and re-committed the exact defect **v1.0.13** was written to fix (when the #40/#41 approvals allocated past the `0x22` block without reserving `0x23`–`0x28`). Closed retroactively and atomically, the v1.0.13 way. `0x2D` promotes to `DOMAIN_TAG_BOARD_OWNERSHIP` at #45 T3's first takeover draw — **one** subsystem-wide stream with keyed action ordinals, never one per club, so #45 never contributes to the `MaxRngStreams` bound. No code const; no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-030-010 | Season & Competition Loop #30, found at **T0 implementation** (not a downstream back-prop): `section-3.md` §3.1's generation pseudocode venues the first leg by round parity, but the worked schedules in §3.7 and `appendices.md` Appendix C were hand-derived without applying it (rounds 1 and 4 inverted), and `section-5.md` T-SN-FIX-001 pinned those tables. | Medium | 3 | ✅ Resolved July 25, 2026 at #30 T0 — **the pseudocode wins, the worked tables are corrected.** Justification measured at the Stage-2 target size (20 clubs): the unparried form gives the pinned club all 19 first-leg fixtures at home (range 9..19), the parity form gives every club 8..10 of an ideal 9..10 with a longest home run of 2. Both satisfy FR-SN-002 (ordered-pair completeness) and FR-SN-003 (one fixture per club per round) at N = 2,3,4,5,6,19,20, and no FR constrains the venue pattern, so §3.1's own "for a balanced first leg" comment is the only stated intent and it decides. Patched same commit: §3.7 (v0.9) + Appendix C (v0.3) rounds 1/4 corrected — **pairings unchanged**, so Appendix C's 12-ordered-pair bullet needed no edit; §5.2 (v0.3) T-SN-FIX-001 re-anchored and **T-SN-FIX-008** venue-balance regression lock added. Code `src/season-save/FixtureScheduler.cs` implements §3.1 verbatim with the deviation reasoning in its header. No FR text change, no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-037-001 | Match Analytics & Statistics #37 §4.1 lists the new assembly's references as `TacticalDirector.EventSystem` + `TacticalDirector.MatchEngine` only, but Appendix A tags `GOAL_WIDTH_M` **`[CROSS]`** and names Ball Physics #1 §3.1.2 as its authority. `[CROSS]` means consumed read-only from the owning spec and never set independently — and Unity asmdef references are not transitive, so honouring the tag requires a direct `TacticalDirector.BallPhysics` reference the §4.1 list omits. The two sections cannot both be satisfied: the same § -architecture-sketch-contradicts-another-section-of-the-same-spec class as ERR-030-012 / ERR-038-002. | Low | 1 | ✅ Resolved July 27, 2026 at #37 T0 — **Appendix A wins**: `match-analytics.asmdef` references `TacticalDirector.BallPhysics` and `MatchAnalyticsConstants.GOAL_WIDTH_M` / `PITCH_LENGTH_M` / `PITCH_WIDTH_M` mirror `BallPhysicsConstants.Pitch.GOAL_WIDTH` / `.LENGTH` / `.WIDTH`. Re-declaring 7.32 locally would have been the parallel-surface trap the `[CROSS]` tag exists to prevent (and would have made a **third** copy — `MatchViewerConstants.GoalWidthM` already holds an independent IFAB literal, recorded here as a pre-existing duplicate, untouched). Ball Physics is a Physics-layer assembly, so the reference direction is unchanged (presentation → sim, never the reverse) and KD-4 still holds — mechanically locked by `MatchAnalyticsValueTypeTests.NoOtherAssemblyReferencesMatchAnalytics`. §4.1's reference list to be re-anchored at next #37 revision. |
| ERR-037-002 | Match Analytics & Statistics #37 §3.4, found at **T1 implementation**: the territorial rule is stated as two strict inequalities — team 0 credited when `BallView.Position.x > PITCH_LENGTH/2`, team 1 when `x < PITCH_LENGTH/2` — and the very next sentence requires the split to be **total**: *"assigned by the strict `>` so the split is total (no double-count, no gap)."* Both cannot hold at exactly `x == PITCH_LENGTH/2`, where two strict inequalities leave the sample credited to **neither** team. The gap is small but not harmless: it silently breaks the invariant `territorial%[0] + territorial%[1] == 100` that the statline's own definition rests on, and a kickoff (ball on the centre spot for many consecutive ticks, `x` exactly `52.5`) hits it every restart, so it is reachable on ordinary play rather than only in the limit. | Low | 1 | ✅ Resolved July 27, 2026 at #37 T1 — **totality wins; the second inequality is the defect.** The sentence naming the strict `>` as what makes the split total is the operative one, so `x > L/2` credits team 0 and **everything else** (including the halfway line itself) credits team 1. `MatchAnalyticsAggregator.AccruePositional` implements exactly that with the reasoning recorded inline, and two tests lock it: `Territorial_CreditsTheTeamWhoseOpponentHalfHoldsTheBall_AndTheSplitIsTotal` (the ordinary case) and `Territorial_HalfwayLineSampleIsStillAttributed_SoNoSampleIsLost` (the boundary, asserting the two shares sum to 100 for a ball sitting exactly on the line). The asymmetry is deliberate and stated: at a single sample point on a continuous axis, which side of the line it falls on is arbitrary, whereas losing samples is not. §3.4's second inequality to be re-anchored at next #37 revision. No FR text change, no format-version change. |
| ERR-030-011 | Season & Competition Loop #30, found at **T1 implementation**: two spec surfaces disagree about the season sub-blob's byte layout. (a) `section-3.md` §3.6's `EncodeSeason` pseudocode omits `ManagedClubId`, which `appendices.md` Appendix B lists as row 3a and §2.2's `SeasonState` requires — a codec written to §3.6 verbatim emits a blob no season can be reconstructed from. (b) Appendix B row 11 leaves job security as `jobSecurity f32/u8`, neither of which matches the integer per-mille `BoardState` carries (resolved at #30 T0 and recorded there as a back-prop candidate). | Low | 2 | ✅ Resolved July 25, 2026 at #30 T1 — **Appendix B is the byte-layout authority; §3.6's sketch is the defect.** (a) §3.6's pseudocode gains the `WriteI32(state.ManagedClubId)` line in Appendix B row-3a position, with a correction note pinning Appendix B as authoritative for the layout. (b) Appendix B row 11 pinned `(targetPosition i32, jobSecurityPerMille i32)`, ratifying the integer convention #30 T0 adopted (the #41 AR-1 float→integer-per-mille precedent; #40 integer currency; #33 per-mille scalars) — integers also make the sub-blob round-trip exact with no NaN gate. Code `src/season-save/SeasonStateCodec.cs` implements the corrected Appendix B layout, with the pinned-offset lock `SeasonStateCodecTests.Decode_SeedAndSeasonNumberSitAtTheirPinnedOffsets` (row 3a included) guarding against future field-order drift. No FR text change, no `SEASON_STATE_FORMAT_VERSION` change (T1 is the version's first use, so the correction lands before any file exists), no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-030-012 | Season & Competition Loop #30, found at **T2 implementation**: `section-4.md` §4.5 specifies that `SeasonLoop` registers a `DeterministicRngService` stream (siteId `season-loop.season-events`, `SubsystemOrdinals.SeasonLoop = 84`, `entityId: SeasonNumber`) for the FR-SN-027 season sub-stream. That is **cursor-positioned**, which §3.4.1 of the same spec forbids for the round-resolution model: its draws must be keyed on the fixture so a round's fixtures resolve order-independently (the §5 lock T-SN-CAL-003c). A cursor makes each scoreline depend on how many fixtures were drawn before it — and that scoreline is serialized in the season blob, so the divergence would be a save-format divergence, not a transient one. | Low | 2 | ✅ Resolved July 26, 2026 at #30 T2 — **§3.4.1's keyed requirement wins; §4.5's registration sketch is the defect.** The season sub-stream is realized as a keyed derivation: `RoundResolutionModel.FixtureKey` folds `DOMAIN_TAG_SEASON_LOOP` (mirrored as `SeasonLoopConstants.DomainTagSeasonLoop`) together with `(seasonSeed, seasonNumber, roundIndex, homeClubId, awayClubId)` through SplitMix64 finalizers, giving the tag its **first consumer** and discharging ERR-030-001's "code const at T2's first draw site" obligation. `SubsystemOrdinals.SeasonLoop = 84` is deliberately **NOT** allocated in code: an ordinal exists only to key a registered stream, so a const with no stream behind it is the zero-consumer phantom FR-LW-031 forbids (the #28 KD-B and living-world `world.arcs` precedents) — ordinal 84 stays reserved in #16 §3.4 spec text for the first genuinely cursor-positioned season event (a #43 knockout draw is the likely first). §4.5 gains a correction note and retains the superseded description as the reservation record; FR-SN-027 is satisfied in substance (domain-separated season draws) rather than by stream registration. Locked by `RoundResolutionModelTests.DomainTag_MirrorsTheSixteenAllocation` plus the order-independence tests at model and loop level. No FR text change, no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-030-013 | Season & Competition Loop #30, found at **T2 implementation**: `section-4.md` §4.6 states that `SeasonLoop.EmitMatchOutcome(result)` "records the `MatchResult` in `SeasonState`". `SeasonState`'s own definition (§2.2) and byte layout (Appendix B) contain no outcome collection, so the sentence is not implementable as written; adding one would be a `SEASON_STATE_FORMAT_VERSION` bump for a payload FR-SN-017 forbids #30 from building any consumer for (#22 phase-1 ingest is gated on #33 / `FR-LW-032`). | Low | 1 | ✅ Resolved July 26, 2026 at #30 T2 — the producer record is **loop-scoped and transient**: `SeasonLoop.MatchOutcomes`, a read-only value-copy collection of every emitted `MatchResult`, also returned per round by `AdvanceAndPlayNextRound`. The *durable* record of what happened is the league table, which IS serialized. FR-SN-016 is unchanged and satisfied — exactly one structured, deterministic `MatchResult` per played fixture. §4.6 gains a correction note; whether the payload also needs persisting becomes a #33-side decision at its landing, co-defined against `FR-LW-027`/`FR-LW-032`. No FR text change, no format-version change, no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-008-014 | Decision Tree #8 has **no action that collects a loose ball lying at rest**: PRESS (§3.1.8) targets an opponent, MOVE_TO_POSITION (§3.1.7) targets the formation slot, and INTERCEPT (§3.1.9) rejects every slow ball at its `INTERCEPT_MIN_BALL_SPEED` gate. Composed, that meant play stopped for good the first time a pass ran out of momentum further than INTERCEPT's ~10 m `MAX_INTERCEPT_TIME` reach from the nearest player — all 22 agents circling their formation slots around a ball none of them was able to decide to fetch. The gate's real purpose (stop teammates converging on a ball their own carrier is standing over — a carried ball is also slow) is preserved. | **High** | 1 | ✅ Resolved July 26, 2026 (match-engine §5.Z Phase H) — the off-ball branch short-circuits to a loose-ball **collect** (an INTERCEPT targeting the ball where it lies, feasibility 1.0, no look-ahead geometry) as the **SOLE** option when the new `TacticalContext.LooseBallCollector` fact is set; the §3.1.9.1 minimum-speed gate is deliberately left UNCHANGED (loosening it to "intercept-eligible while LOOSE" would make every off-ball agent chase a resting ball — the converge-and-dither failure the single designated collector prevents; the resulting sub-`INTERCEPT_MIN_BALL_SPEED` band is transient and accepted). Sole-option per the ERR-008-013 SAVE precedent + its AR-4 rationale (measured: the collect scores ~0.35 vs MOVE's ~0.21, a 0.14 gap inside the ±0.15 composure-noise band, so the collector flip-flopped and never arrived). The fact is set by `MatchEngine.RunMechanicsAI`, not derived in the tree: it is a team-level role assignment from team state (the #13 primary-presser precedent) and — load-bearing — only the host knows who is **sent off**; a perception-derived "nearest teammate" rule deferred to a frozen red-carded agent and deadlocked anyway. `OptionGenerator`/`TacticalContext`/`DecisionTreeConstants` (+`NoPossessorAgentId`); `decision-tree/section-3-1.md` anchor note. |
| ERR-008-015 | Decision Tree #8 §3.7.2 parks a tree in EXECUTING after a PASS/SHOOT dispatch and re-evaluates only on `NotifyActionComplete` / `NotifyInterrupt` / a forced refresh — but it assigns the completion obligation to **nobody**, and **no production caller of `NotifyActionComplete` existed** (zero outside tests). Every agent that completed a pass or a shot was therefore frozen in EXECUTING for the remainder of the match: no further decisions, no further movement commands, and — if it still held the ball — no way to release it. A **rejected** `Execute` was worse: the dispatcher deliberately does not inspect the result (§3.5.2), so the tree entered EXECUTING with no in-flight action at all and nothing could ever complete. | **High** | 1 | ✅ Resolved July 26, 2026 (match-engine §5.Z Phase H) — the composition root closes the lifecycle, since it is the only layer that sees both the trees and the executors: after the Resolve-phase executor advance, a tree that `IsAwaitingExecutorCompletion` (new #8 predicate expressing §3.7.2's continuous-vs-blocking rule in ONE place, over `DecisionTreeStateMachine.IsContinuousAction`) whose pass AND shot executors are both idle is released via `NotifyActionComplete`. One rule covers completion and rejection alike. Paired: `OnPossessionChanged` no longer interrupts a holder whose own executor is still in flight — that was re-planning agents into their own busy executor once rebounds began ("Execute() called while shot in progress"). `DecisionTree.cs`; `MatchEngine.RunResolvePhase`/`OnPossessionChanged`; `decision-tree/section-3-6-to-3-8.md` anchor note. |
| ERR-008-016 | Decision Tree #8 §3.2.1.3 defines the utility zone bands as thirds relative to a team's own goal line, but pins `ATTACKING` at `65m – 105m`, making the attacking third 40 m and the middle third 30 m. `65` is neither a third of the 105 m pitch nor derivable from any stated formula, and the implementation carried it as `public const float AttackingZoneMinX = 65.0f` under a `[DERIVED] — split pitch into thirds` region comment its value contradicted (FR-CS-021 requires a `[DERIVED]` constant's formula to be documented AND to hold). Its sibling `DefensiveZoneMaxX = 35.0f` WAS a true third, so the pair was internally inconsistent. | Low | 1 | ✅ Resolved July 26, 2026 — both bounds derived from the pitch length (`PitchLengthM / 3`, `PitchLengthM * 2 / 3`), so the thirds are equal and track the pitch dimension. `decision-tree/section-3-2.md` v1.7 + `PitchGeometry.cs` v1.2. Recorded side effect: equal thirds make the boundary pair SELF-MIRRORING (`{L/3, 2L/3}` maps to itself under `x → L − x`), so a team's own-goal-relative bands no longer depend on which direction it attacks — which also retires the v1.1 claim that "enum mirroring is not exact (35/65 mirror to 40/70)". The ERR-008-002 per-team recomputation stays the contract: it is what measures from the correct goal line. `DecisionContextAssemblerTests` v1.1 replaces the test that discriminated via the now-nonexistent 35–40 m band with the same AR-2 H-2 contract at x = 20, plus locks for mirror-symmetry and for the bounds actually being equal thirds. Measured behaviour-neutral over two 9-minute composed runs (identical scorelines, possession and ball ranges) — a correctness and clarity fix, not a balance lever. |
| ERR-030-014 | **Match-engine-owned, discovered at #30 T2 / roadmap A4a Step 0.** A production `MatchEngine` match cannot develop play at all: the ball's velocity is identically zero for all 324 000 ticks, it is never airborne, and no agent ever possesses it, so every match ends 0–0 regardless of squad strength (20/20 pilot matches 0–0 at a measured `dSquad` of ±6). Closed loop: `InitializeKickoffState` places the ball at rest and comments that no Stage-0 kick sets it in motion; `RunFirstTouch` gate 3 refuses to grant a touch unless the ball is already moving (`FIRST_TOUCH_MIN_BALL_SPEED_M_S`); production possession comes only from that path (`TestOnly_SetPossessor` is not a production caller); and only a pass/shot executor — gated on `IsBallPossessedBy` — can impart velocity. `ApplyRestart` cannot break the loop either, since a restart needs a boundary crossing and therefore motion. Invisible to the suite because the 321 match-engine tests drive their own inputs per subsystem, and the one composed test (the 600-tick kickoff capstone) asserts tick count, stride cadence, finiteness, bounds and digest advance — all of which hold for a match in which nothing happens. | **High** | 1 (+ every path-to-playable item that needs a played match) | ✅ **Resolved July 26, 2026 (match-engine §5.Z Phase H, roadmap A4b)** — a production match now plays: the ball is kicked and airborne, possession is held 10–21% of ticks and changes hands 262–298 times per 9 minutes, the ball reaches both penalty areas and goals are scored. The fix is five seams, not one (the single kickoff grant below was necessary but not sufficient): the KD-H1 restart taker award, the KD-H3 loose-ball pickup, the KD-H5 / ERR-008-014 DecisionTree loose-ball collect, the KD-H4 / ERR-008-015 PASS/SHOOT completion sweep, and the interrupt deferral that stops a re-plan dispatching into a busy executor. Locked by the new `match-engine-play-develops` acceptance scenario, whose every predicate fails on the pre-fix engine — including `play-still-alive-at-final-tick`, which caught two stalls that let play run for eight or nine minutes before dying. No `SNAPSHOT_SCHEMA_VERSION` change. Full detail: `match-engine-design.md` §5.Z. ORIGINAL ASSESSMENT (kept — its diagnosis was right, its scope estimate was not): Not fixed inside #30 T2 on purpose: the minimal fix is a kickoff/restart **possession grant** (award possession to a designated agent so the Decision Tree has a carrier), which is a behaviour change to the most safety-critical assembly in the tree, activates a large amount of code that has never run in composition (roadmap C5 at its strongest), and moves every engine digest — so it wants its own design note, adversarial-review cycle and landing. **What A4/A4a did instead:** left the loop and the model correct and green; shipped the three round-resolution `[GT]` parameters labelled provisional-not-fitted at their declaration; committed the reproducible Step 0 pilot and the `EngineScoringDiagnosticTests` characterisation (both env-gated, neither asserting current behaviour, since pinning it would turn a defect into a contract); and recorded the re-run recipe so A4a resumes with `tools/round-resolution-fit.py` once a match can be played. |
| ERR-006-001 | Shot Mechanics #6 §3.5 / §4.1.1 resolves every shot against ONE goal. `GoalGeometryProvider.Get()` returns `GoalLineX = PitchLength` unconditionally and states the assumption in its own doc — *"Assumes the attacking team is shooting toward X = PitchLength (right goal). Stage 1+ will supply attack direction from match context"* — and `ShotPlacementResolver` is written to match (`Mathf.Max(goal.GoalLineX - shooterPosition.x, floor)`, `Mathf.Max(baseAimDirection.x, ε)`). No caller ever supplied that direction, so **both teams shot at x = 105**: the away side shot at the goal it defends, and any that went in were credited by the exit-half-space rule to the home side. Measured over four full 90-minute matches: **home 21 goals, away 0**, on symmetric possession (1.8–2.4% each), passes (~700 each) and time in the third each team attacks (10–15% each) — with the ball reaching x = 105+ and never once reaching x = 0. Decision Tree #8 is correctly team-relative (`PitchGeometry.GetOpponentGoalCentre(teamId)`), so the away side *decided* to shoot in the right places and then kicked the wrong way. Invisible to the suite because #6's own fixtures are all home-perspective — the ERR-008-002 / ERR-013-009 defect class the project has now hit four times. | **High** | 1 (+ every consumer of a played scoreline: A4a calibration, #30 quick-sim fitting, PM-1) | ✅ **Resolved July 27, 2026 (match-engine §5.Z.14).** Fixed at the composition root, not in #6: `MatchEngine.ShotWorldAdapter` maps the away team's shooter state INTO #6's canonical attack-+X frame (`MirrorPitchIfAway` for the position, `MirrorVelocityIfAway` for velocity and facing) and maps the resulting kick back OUT on `ApplyKick`. Per §5.Z.12 — "a pair has two places that must agree; a mirror has one" — this reuses the mirror the rest of the engine already uses rather than introducing a second hardcoded goal line, and leaves every APPROVED #6 formula, constant and test untouched. The mirror is a 180° rotation about Z, so the same negate-x-y rule is correct for velocity and for spin (a proper rotation transforms a pseudovector exactly as it transforms a vector). Measured after: scorelines 6–0/10–0/2–0/3–0 → **6–6/12–5/2–6/11–10**, the away side scoring in every match and winning one, ball min x 2.1 → −2.4. **#6's spec text is left as-is deliberately**: it is not wrong about its own scope, it is explicit that attack direction is the caller's to supply, and supplying it is exactly what this fix does. |
| ERR-030-015 | Season & Competition Loop #30, found at **T3 implementation**: `section-3.md` §3.5's `RollToNextSeason` pseudocode regenerates `Fixtures`, resets `Table`, and advances `SeasonNumber`/`Seed`, but **never rebuilds `Calendar`** — whose cursor is at `RoundCount` precisely because the season just ended. Implemented verbatim, the roll yields a season that is permanently unplayable: `SeasonCalendar.IsSeasonComplete` stays true, so `AdvanceToNextFixtureDay` throws F5 and `AdvanceAndPlayNextRound` throws, on every call thereafter. The transform cannot deliver FR-SN-029's multi-season continuity as written, and no unit assertion over the rolled state's *fields* would notice — the schedule, table, seed and season number are all exactly right. | **High** | 1 | ✅ Resolved July 27, 2026 at #30 T3 (roadmap A5) — §3.5 gains step **(c′) rebuild the calendar**, between (c) regenerate and (d) age advance, leaving the surrounding steps and therefore FR-SN-031's (a')/(b') insertion points untouched. `SeasonLoop.ShiftCalendarToNextSeason` implements it by shifting the OLD calendar's day mapping forward by one season length plus a new `[GT] SeasonBreakDays` close season: the roll stays a pure function of the prior `SeasonState` (KD-6 — no clock read, no draw), the new season opens exactly one break after the old one's finale, and a non-uniform schedule keeps its shape instead of being silently flattened to linear. Caught by an acceptance test that plays a **second** season to completion; 9 of the suite's 18 predicates fail against the pre-fix form. No FR text change, no `SEASON_STATE_FORMAT_VERSION` change (the calendar was already serialized), no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-041-001 | Injuries & Medical #41 back-prop: `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` + `SubsystemOrdinals.InjuriesMedical = 92` allocation needed in Deterministic Simulation #16 §3.4 (the `injuries.occurrence` world-tick sub-stream, siteId `injuries.occurrence`, `entityId = playerId`, position-independent keyed draws; #41 KD-1 / §5). | Medium | 1 | ◑ Spec-text allocated July 23, 2026 at #41 section-file approval — `deterministic-sim/section-3.md` §3.4 gains the `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` row (v1.0.11; value `0x2A` per roadmap §6, block skips `0x23`–`0x29` reserved for #31–#40). **Spec-text-first like ERR-030-001** (not code-first like ERR-022/027-001): the code const (`DeterministicSimConstants.DOMAIN_TAG_INJURIES_MEDICAL` / `SubsystemOrdinals.InjuriesMedical`) + the `injuries.occurrence` stream registration land at **#41 T2** with the first draw site (FR-LW-031 — no phantom stream). Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. **✅ Resolved August 5, 2026 at #41 T0** — the code const `DeterministicSimConstants.DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` lands at #41's first draw site (`MedicalStep.DrawOccurrence`), mirrored `[CROSS]` as `InjuriesMedicalConstants.DomainTagInjuriesMedical` and locked by `InjuriesMedicalConstantsTests.DomainTag_MirrorsTheDeterministicSimAllocation`. The draw site arrived at T0 rather than T2 because the T0 step IS the draw site — §7.1 puts `AdvanceMedicalDay` in T0 and §4.5 puts the const at "the first draw site", and those are the same landing. **`SubsystemOrdinals.InjuriesMedical = 92` is deliberately NOT allocated in code** and stays reserved in #16 §3.4 spec text: #41 registers no cursor stream (KD-1 / FR-MD-007), and an ordinal exists only to key a registered stream, so a const with no stream behind it is the zero-consumer phantom FR-LW-031 forbids — the ERR-030-012 precedent, reached independently from the same constraint. See ERR-041-002 for the API-shape half of the same finding. |
| ERR-020-002 | Code Standards #20 §3.5.2's layer taxonomy places **19 of the 31 assemblies now in `src/`**. The 12 unplaced are `living-world`, `match-analytics`, `match-client-core`, `match-client-unity`, `match-client-web`, `match-engine`, `match-viewer`, `player-database`, `player-progression`, `season-save`, `tactical-instructions`, `ui-framework` — so FR-CS-046 ("references flow one direction only") is unenforceable for ~39% of the tree, including the composition root and every client assembly. The taxonomy also still carries `UI` as an empty "Stage 1+ — not specified yet" placeholder, though four UI/client assemblies exist. Separately, `src/CLAUDE.md`'s infrastructure table lists `code-standards` as an assembly; no such folder exists (#20 is a style guide). | Medium | 2 | 🟡 **Open — PROPOSAL filed August 2, 2026, awaiting owner sign-off.** A ten-tier order covering all 31 folders is proposed in the entry body and was verified against every `.asmdef` reference list: **zero upward references** — adopting it constrains future code only, and changes nothing that exists. Layer membership is #20's to decide, so no spec text was edited. |
| ERR-011-008 | Goalkeeper Mechanics #11 §3.5.2 — **a keeper's CATCH never stopped the ball.** §3.5.2's body has always carried `ball.velocity = gkHandVelocity` ("parked at hand position"), but §3.5's **Outputs** summary named only `Ball.SetPossessor` for the catch branch, and the implementation followed the summary. Possession is a FLAG in this engine, not a kinematic constraint — `RunPhysicsPhase` integrates the ball unconditionally and `CheckRestartAndApply` adjudicates a goal on ball POSITION — so a claimed shot travelled on into the net with the keeper recorded as holding it. Measured over three full matches: ball speed **11.1 m/s in, 10.8 m/s out** of a catch (one tick of drag), against parry 10.8 → 0.0 and deflect 10.3 → 4.2; **7 of 10 catches followed by a goal within 5 s**, and 14 of 15 goals following a keeper contact within 10 s. The same omission is at the Stage-0 smother/1v1 claim. `IGoalkeeperBallSystem` offered no park seam at all, which is why the gap was invisible from the interface. | **High** | 4 | ✅ **Resolved August 3, 2026** (conversion-at-contact pass, §5.Z.23) — `ParkBall()` added to the seam and called at both claim sites; §3.5's Outputs restated to name the catch's two effects (§3.5.2's pseudocode body unchanged — it was correct). No schema / RNG / domain-tag / draw-site / draw-order change. Locked by `match-engine-keeper-claim`: **2 of 3 predicates fail pre-fix, verified by execution — 6 of 6 claims left the ball travelling, 5 of 6 held balls entered the keeper's own net.** Measured: goals 15 → 11 over the corpus (5.0 → 3.7/match). |
| ERR-011-009 | Goalkeeper Mechanics #11 §3.1.1 / §3.7.2 — **a rush that REACHED its target had no exit.** §3.1.1 gives `Rushing` exactly three exits (hand contact, the 1v1 radius, F-08 interception) and `OneOnOne` two (`SaveIntent`, the smother radius). For a LOOSE ball **none of them can fire**: `existsAttackerWithBallWithinRadius` is false by construction with no possessor, F-08 needs one, and §3.7.2's per-frame update converges on the locked `rushTarget` and stops without overshooting. A keeper who swept a loose ball therefore stood over it in `Rushing` for the remainder of the match. The completion was anticipated everywhere except the table that adjudicates state — `RushPhase.Reached` has been in §2's enum since v0.1 and was never emitted, and §3.7.3 reserves `AbortReason.AttackerBeatGK` for the related case. **Latent, not live**, until wiring backlog W1: `CommitRushIntent` had no production caller, so no rush had ever run in a match. | **High** | 11 | ✅ **Resolved August 4, 2026** (wiring backlog W1) — two new §3.1.1 rows (`Rushing → Recovering`, `OneOnOne → Recovering`) on arrival within the new `[GT] RUSH_TARGET_REACHED_RADIUS_M` (§3.4.6), plus the terminating check in §3.7.2, emitting `GoalkeeperRushEvent { rushPhase: Reached }`. A **completion, not an abort** — FR-GK-018 / KD-15 untouched, since nothing about the ball's trajectory ends the rush, and it is ranked below contact, F-08 and the 1v1 trigger. No schema / RNG / domain-tag / draw-site / draw-order change. Locked by `GoalkeeperRushTests` (both keepers) and the four state-machine priority cases. **Measurement NOT run — no .NET SDK in the authoring environment; see the owner doc §6.** |
| ERR-011-010 | Goalkeeper Mechanics #11 §3.7 — **the rush decision had no owner, so the whole subsystem never ran.** §3.7's state entry delegated the "when" of a rush entirely to Decision Tree #8. #8 has no goalkeeper model and **structurally cannot acquire one at Stage 0**: `ActionType.SAVE = 7` is the last ordinal that fits the 3-bit composure-noise field in §3.3.3's noise function, so a `RUSH` action would force a composure-noise digest rebaseline (the same cost that defers the DT-emitted HEADER). The condition therefore belonged to nobody, and `CommitRushIntent` had **no caller of any kind — production or test — from May 28 to August 4, 2026**. Every one-on-one in the engine's history was a stationary keeper on his line. Compounding it, the delegation left the *football* undefined too: nothing in the spec said what a keeper is deciding when he comes out. | **High** | 11 | ✅ **Resolved August 4, 2026** (wiring backlog W1) — new **§3.7.0** takes the decision back, the same move §3.3.6 made for dive-commit timing. Normative on two points: (1) a team-mate merely CHASING the carrier is **not** a reason to stay — a recovering defender narrows no shooting angle, and only a goal-side body inside the shot corridor does; (2) how far out the keeper comes is **his own attributes** — `clamp(RUSH_COMMIT_BASE_M + RUSH_COMMIT_K_ONE_VS_ONE·OneVsOne_norm + RUSH_COMMIT_K_COMPOSURE·Composure_norm − RUSH_COMMIT_FATIGUE_PENALTY_M·fatigue, min, max)`, six new `[GT]`s in §3.4.6, with a worked example. `OneVsOne` is consumed for the commit DECISION only — FR-GK-024's closed-form constraint on the §3.2 / §3.5 1v1 SAVE formulas is untouched. No schema / RNG / domain-tag / draw-site / draw-order change. **Measurement NOT run — no .NET SDK in the authoring environment; see the owner doc §6.** |
| ERR-008-020 | Decision Tree #8 §3.1.3.3 — the pass-lane `is_interceptor` test was a binary 0.8 m corridor: a 2 cm positional difference stepped `PassLaneScore` by 0.33, and **no defender attribute entered the judgment** — a Pace/Anticipation 1/1 defender priced a lane identically to a 20/20 one. First fix landed under the football-judgment proxy review's remediation doctrine (§6.4 template fix): continuous falloff (P1) × Anticipation/Pace ability read through the passer's Vision fidelity (P2), ramp centred on the old cliff so the neutral rows reproduce exactly (P5). | Moderate | 8 | ✅ **Resolved August 4, 2026** (spec §3.1.3.3 + `OptionGenerator`/`UtilityWeights`/`DecisionContext(±Assembler)`/`DecisionTree`/`MatchEngine`, same commit). Shot lane (§3.1.4.3) deliberately deferred. **Gate NOT run — no .NET SDK in the authoring environment.** Same commit: the review file's false "ERR-008-019 FIXED" claim corrected; -019 stays soft-reserved for the still-live long-shot cliff. |
| ERR-020-003 | Code Standards #20 §3.5.2 draws the layer rule as `Physics ──► Mechanics ──► AI ──► UI`, while the root `CLAUDE.md` states the same rule as **AI → Mechanics → Physics, never the reverse**. The arrows point opposite ways. Both are defensible readings of their own notation (#20's arrow = "is available to"; CLAUDE.md's = "may reference"), and neither states which it means, so the two authoritative statements of the project's single most load-bearing architectural rule read as contradictory. | Low | 2 | 🟡 **Open — filed August 2, 2026.** Proposed fix: label the arrow in #20 §3.5.2 explicitly (`──► reads "may be referenced by"`) and add the reference-direction sentence verbatim beneath the diagram, so the two files state the rule in the same words. No behaviour change; the rule itself is not in dispute. |
| ERR-041-002 | Injuries & Medical #41, found at **T0 implementation**: `section-2.md` §2.2 and `section-3.md` §3.1 specify the occurrence draw as `rng.DrawKeyed(STREAM_INJURIES_OCCURRENCE, entityId: playerId, actionOrdinal: …, drawIndex: 0)` against a `DeterministicRngService` parameter. **No such API exists.** #16's service exposes only the branch-safe reservation trio (`Reserve` / `DrawReserved` / `CloseReservation` / `Skip`), whose draw value is keyed on an `ActionOrdinal` the service itself increments inside `Reserve` — there is no overload that accepts a caller-supplied action ordinal, and the field is private. So the signature cannot be implemented, and the one shape that could be (register a stream, then `Reserve`/`DrawReserved` per player-day) is **cursor-positioned**, which §3.1/KD-1 of the same spec forbids: the whole design rests on the draw being reproducible from `(playerId, worldDay, purpose)` alone so that FR-MD-007 can serialize no cursor at all. The same §4-architecture-sketch-contradicts-another-section class as ERR-030-012 / ERR-037-001 / ERR-038-002. | Low | 2 | ✅ Resolved August 5, 2026 at #41 T0 — **KD-1's keyed requirement wins; the `DrawKeyed` call is the defect.** The draw is realized as a local keyed derivation (`MedicalStep.DrawOccurrence`): `DomainTagInjuriesMedical` folded in first, then `playerId`, then the `(worldDay, purpose)` action ordinal, each through a SplitMix64 finalizer, reduced into `[0, OccurrenceDrawDenom)`. This is the #30 `RoundResolutionModel.FixtureKey` / `LeagueBootstrap` precedent — the project's established way to take a position-independent draw — and it satisfies FR-MD-005/006/007 in substance: domain-separated, keyed, and with nothing to persist. Consequences: `AdvanceMedicalDay` takes `ulong worldSeed` in place of the `DeterministicRngService rng` parameter (the seed is the only service input a keyed draw needs, and it is already readable from `WorldStore.WorldSeed` per roadmap A3), and no stream is registered — so FR-MD-027's stream-independence property holds vacuously rather than by test. Locked by `MedicalStepTests.Draw_IsPositionIndependent_TTMDDET003` and `TwoPlayersOnTheSameDay_DoNotInfluenceEachOther`. §2.2/§3.1 signatures to be re-anchored at the next #41 revision — **discharged August 7, 2026 at the balance pass (ERR-041-012: §3.1's pseudocode now shows the keyed derivation; FR-MD-005 and §4.5 re-anchored with it)**; no FR text change at this entry, no format-version change, no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-041-003 | Injuries & Medical #41, found at **T0 adversarial review**: Appendix A tags `INJURY_RISK_MAX` **`[GT]`** in #41's own catalogue, while §3.4 requires the assembled risk to be on **the same scale** as #29's `InjuryRiskContribution.RiskScore` (it passes through with weight 1) and derives `OCCURRENCE_DRAW_DENOM` from it, so the draw is taken on that scale too. Both cannot hold. A `[GT]` row means an independently tunable value with its own config key — `[injuries-medical] InjuryRiskMax` alongside #29 Appendix A's `[training-system] InjuryRiskMax` — and setting one without the other silently rescales every occurrence probability while #29's clamped maximum quietly stops meaning "certain occurrence", with nothing in either system able to notice. The T0 landing initially implemented both rows literally and added an equality test between them, which made it worse rather than better: the gate runs with `GameplayConfigHolder` unbound, so both sides returned their design-time fallback and the assertion passed no matter what a config file said — a lock wired to nothing. Same class as ERR-037-001 (a tag and an architecture list that cannot both be honoured). | Low | 2 | ✅ Resolved August 5, 2026 at #41 T0 (AR pass 1) — **§3.4's shared-scale requirement wins; the `[GT]` tag is the defect.** `InjuriesMedicalConstants.InjuryRiskMax` moves to the `Cross` region as `[CROSS: #29 Appendix A]`, mirroring `TrainingSystemConstants.InjuryRiskMax` directly (the single-consumer routing rule — mirror the source spec's catalogue, not `ProjectConstants`). One owner, one config key; the mirror-fidelity test now holds by construction, which is the point of the `[CROSS]` tag rather than a weakness of the test. `OccurrenceDrawDenom` stays `[DERIVED]` off the mirror and remains a property rather than a field, since a `Derived`-region field would initialise before the `Cross`-region field it reads and silently capture 0. #41's Appendix A row to be re-tagged `[CROSS]` at the next #41 revision — **discharged August 7, 2026 at the balance pass (ERR-041-011: Appendix A v0.4 carries the `[CROSS: #29 Appendix A]` tag)**. **Recorded, not fixed (a balance decision, not a defect):** both specs mandate their own robustness mitigation over the same three #27 physical attributes (#29 §3.4 and #41 §3.4/FR-MD-015), so a player's robustness is priced in twice and #29's saturated maximum can never reach #41's ceiling — pinned as an explicit assertion in `MedicalStepTests` so the balance pass inherits the fact rather than rediscovering it. No FR text change, no format-version change. |
| ERR-008-019 | Decision Tree #8 §3.2.3.1 — the midfield `ZoneModifier_SHOOT` was a hard step on one attribute: 0.55 strictly above `LONG_SHOT_THRESHOLD` (shifted LongShots), 0.05 at/below — an **11× jump across one raw attribute point** (10 → 11). The football-judgment proxy review's *founding* pattern-(b) finding, and the id whose original "FIXED" record was verified false at the ERR-008-020 landing (soft-reserved since; re-verified free here). | Moderate | 4 | ✅ **Resolved August 5, 2026** (spec §3.2.3.1/§3.2.3.4 + `UtilityScorer`/`UtilityWeights`, same commit; doctrine P1/P5) — linear ramp in the unchanged shifted form, centred on the old threshold with new `[GT] LONG_SHOT_RAMP_HALF_WIDTH`, **owner-revised same day from 0.05 to the full-range 0.25**: raw 1 exactly 0.05, raw 20 exactly 0.55, every raw point between moves the modifier ≈ 0.026 — no plateau anywhere (`t` reduces to `A_LongShots`); exact midpoint at the old cliff and uniform-population mean 0.30 preserved (P5 pivot, locked by test). **Digest invariance NOT established** (claim retracted August 5, 2026 at the adversarial review over the landing — the original argument assumed a 0.5 m possession radius; the engine's production path is `RunLooseBallPickup`'s **1.0 m** KD-H3 radius, which leaves the ball where it lies, and nothing re-anchors it afterwards): a MIDFIELD ball at x → 70⁻ with the holder 1.0 m goal-side reaches just above 34.0 m, inside raw 19's range gate (34.21 m), where the ramp gives ≈ 0.524 against the step's 0.55 — so a generated option **can** score differently. Behaviour change owner-intended; the narrow-ramp (0.05) predecessor's disjoint-bands argument survives (29.0 m vs > 34.0 m). 4 `UtilityScorerTests` locks (shifted-form, no-cliff, exact midpoint, endpoints-exact + strictly-monotone). **Gate NOT run — no .NET SDK in the authoring environment.** |
| ERR-029-004 | Training System #29, found at **T1 implementation**: §4.4 describes the `TRAINING_SAVE_FORMAT_VERSION` sub-blob's *posture* — opaque, independently version-gated, fail-loud per F3/F5, `serialize, don't regenerate` — and never states a single field of its byte layout, while the sibling #41 §4.4 pins its own in full pseudocode. F3 refuses every cross-version migration at Stage 0, so the first written layout becomes the format permanently; leaving it unwritten means two implementers can only agree by accident, and the one who guesses differently produces files the other rejects with a version error that is not the actual problem. The same section's §2.3 **F3** row compounds it by naming `ArgumentException` while citing "the `MatchSaveCodec` posture" — that codec throws `InvalidOperationException`, which is not an `ArgumentException`, so the row contradicts itself and an implementer honouring the type diverges from every sibling codec in the tree. | Medium | 1 | ✅ Resolved August 6, 2026 at #29 T1 — spec + code, same commit. New **§4.4.1** pins the layout as normative: version, club count, then per club `ClubId` + player count + per player `(playerId, focus byte, condition, trainingFatigue, lastAdvancedWorldDay)`, no RNG cursor block (KD-6 / ERR-029-001). Three properties are stated as MUSTs rather than left implicit: **`ClubId` is written** (see ERR-041-008 — club identity must not be positional); **order is not state**, so encode canonicalizes to ascending `(ClubId, PlayerId)` and decode requires it, which is what makes two equal state sets produce equal bytes whatever roster order the caller holds and what stops a duplicate key reaching the file with no defined winner; and **`[GT]` bands are NOT gated on decode** — `CONDITION_MIN`/`CONDITION_MAX`/`TRAINING_FATIGUE_MAX` are tunable, and enforcing them at load would turn a designer's ceiling change into data loss across every existing save, so only structurally impossible values (negative fatigue, an undefined focus ordinal) are refused. §2.3 F3 corrected to `InvalidOperationException`. Implemented as `TrainingSaveCodec` + `ClubTrainingStates`; locked by `TrainingSaveCodecTests` (round-trip field identity, all six focus ordinals surviving the byte, order-independence, the encode-side duplicate/unbound guards, and each decode gate). |
| ERR-041-008 | Injuries & Medical #41, found at **T1 implementation**: §4.4's `MEDICAL_SAVE_FORMAT_VERSION` pseudocode iterates `for club in perClubStates (deterministic club order)` and writes the player records — but **never writes the club id**. Club identity is therefore carried across a save boundary by list position alone, which means the block can only be interpreted by agreeing with something outside it about club ordering; the only candidate is #30's season sub-blob, and KD-7 of this same section forbids this codec to read it. A club-set reorder between save and load (promotion/relegation at a season boundary is the obvious one) silently re-attaches every club's medical states to the wrong club, with no id in the bytes for anything to notice. Same class as ERR-029-004 — the persistence section of each sibling spec was under-specified in a different way, and both surfaced the moment someone wrote the format instead of reading it. | Medium | 1 | ✅ Resolved August 6, 2026 at #41 T1 — spec + code, same commit. §4.4's layout gains `WriteI32(club.ClubId)` and the decode side gains the strictly-ascending club-id gate; #29's new §4.4.1 carries the field from the start, so the two blocks stay byte-shaped alike. The same edit pins the canonical ascending-key rule, the negative-counter refusals, and — the one asymmetry worth naming — that **the F1 coherence gate runs on encode as well as decode**: a codec validating only on the way in writes files no load of it can accept, and the contradiction then surfaces a session later, far from the bug that produced it. `[GT]` bands stay ungated on both sides for ERR-029-004's reason. §2.3 F3 corrected to `InvalidOperationException`. Implemented as `MedicalSaveCodec` + `ClubInjuryStates`; locked by `MedicalSaveCodecTests`, including a block-size assertion that fails if an RNG cursor is ever added to the block — the question "did we just make the draw position-dependent?" (KD-1 / FR-MD-007) then gets asked before it ships. **Id note:** 008, not 004 — `injury-aging-research-alignment-design.md` soft-reserves ERR-041-004..007 for its own pending back-props, and ERR-041-002 was already reassigned away from that supplement once. |
| ERR-029-005 | Training System #29, found by **adversarial review over the T1 landing**: §4.4.1's block is gated by `TRAINING_SAVE_FORMAT_VERSION` alone, and that is not an identifier. Every sub-blob format in the save stack is at version 1 (`MEDICAL_SAVE_FORMAT_VERSION`, `SEASON_STATE_FORMAT_VERSION`, `MATCH_SAVE_FORMAT_VERSION`, `PROGRESSION_SAVE_FORMAT_VERSION` included), so the gate separates one *generation* of a format from the next and never one format from another. ERR-029-004 had just made this block byte-for-byte the same shape as #41's, so the #41 medical block decoded here **completely and silently**: severity ordinals 0–3 are all defined `TrainingFocus` values, `RecoveryRemaining` landed in `Condition`, the always-non-negative `InjuryCount` landed in `TrainingFatigue` and passed its only gate, and there were no trailing bytes. Confirmed by executing a byte-exact model of both formats in both directions. The trigger — transposing two arguments in `SeasonSaveCodec.Encode`'s list of five consecutive `byte[]` — had no compile-time signal either, and the only thing that caught it in one direction was #41's F1 coherence rule firing on the *other* block, an accident of #41 having an invariant #29 lacks. | **High** | 1 | ✅ Resolved August 6, 2026 at the AR pass — spec + code, same commit. Two layers, because the defect has two halves. **Load-time:** `TRAINING_SAVE_MAGIC` (ASCII `"TRNG"`) is written before the version and checked before it, so a foreign block is refused by name rather than mis-read; the message names the observed magic, turning a mystery corruption into "you handed me the medical block". Deliberately not an RNG domain tag — those name draw domains and must stay free to change independently of a save format. **Compile-time:** `SeasonSaveCodec.Encode`'s two confusable parameters become the typed `TrainingBlock` / `MedicalBlock`, so the transposition is a build error; `Encode` still null-guards `default(TrainingBlock)`, which skips the wrapper's constructor exactly as `default(ClubTrainingStates)` does one layer down. §4.4.1 records the general rule as a MUST: **a format version is not a format identifier.** Locked by `Decode_ForeignMagic_FailsLoud_NotSilentlyReinterpreted` (#29's suite, which cannot reference #41 and so stands in a same-shape foreign block), `Decode_ATrainingBlock_FailsLoud_BothDirections_ERR041009` (#41's suite, which can, and proves both directions on real blocks), and `SaveLoad_TransposedTrainingAndMedicalBlocks_FailLoud` through a whole file. |
| ERR-041-009 | Injuries & Medical #41 — the same defect in the sibling spec, filed separately because §4.4 is its own normative layout and would otherwise stay wrong. The #29 training block decoded as a medical block just as silently in the reverse direction on realistic data: a squad on `Fitness`/`Technical` focus with a healthy `Condition` read back as a squad carrying `Moderate`/`Serious` injuries with thousands of recovery days, F1 coherence satisfied throughout, because a positive `Condition` is indistinguishable from a positive `RecoveryRemaining`. §4.4's ERR-041-008 bullet additionally cited "KD-7 blob independence"; in `unified-season-save-design.md` **KD-2** is the no-cross-parse decision and KD-7 is the codec/disk-I/O split, so the citation pointed at the wrong decision. | **High** | 1 | ✅ Resolved August 6, 2026 at the AR pass — spec + code, same commit. `MEDICAL_SAVE_MAGIC` (ASCII `"MEDL"`) written and checked first, mirroring ERR-029-005; §4.4's property list grows from three MUSTs to four, leading with **the block names its own format**; the `KD-7` citation corrected to `KD-2` in both §4.4 and #29's §4.4.1. Neither sub-blob's format version is bumped and `SEASON_SAVE_FORMAT_VERSION` stays 3: no such block has ever been written to a real save (nothing constructs either state set until T2), and the *frame* layout is untouched — only the contents of two blocks the frame treats as opaque. **Id note:** 009, not 004 — `injury-aging-research-alignment-design.md` still soft-reserves ERR-041-004..007. |
| ERR-008-021 | Decision Tree #8 §3.1.4.3 / §3.2.3.2 — the shot-lane occlusion test was the pass lane's twin defect, deferred at the ERR-008-020 landing by owner call and closed here. An opponent contributed his **whole** blocking width if his angular centre fell inside the goal arc and **nothing at all** if it fell outside: a defender across the near post scored a fully open goal, and 4 cm of lateral position stepped `GoalOpeningScore` by 0.41 (0.595 → 1.000). The width was body radius alone, so blocker identity never entered the shooter's read of the goal. | Moderate | 3 | ✅ **Resolved August 5, 2026** (spec §3.1.4.3 + §3.2.3.2 + `OptionGenerator`/`UtilityWeights`, same commit; doctrine P1/P2/P3/P5) — the contribution is the true angular OVERLAP of the disc with the goal arc (continuous by construction) × the blocker's Anticipation/Positioning ability (`SHOT_BLOCKER_ABILITY_MIN/MAX` 0.6–1.4 `[GT]`, average exactly 1.0) read through the shooter's Vision fidelity (§3.1.3.3's floor, shared as one dial). **Goalkeeper exempt from the ability term** — #11 owns keeper shot-stopping (P3). P5 exact: old rectangle and new trapezoid both integrate to `4h·halfArc` over a uniformly-placed blocker. Digest invariance **not claimed** — the model is live on every generated shot. 10 `OptionGeneratorTests` locks incl. the GK exemption and the away mirror (counts and adequacy corrected at ERR-008-022). **COMPILED AND EXERCISED, NOT GATE-VERIFIED** — CI run 402 (PR #302, head `301c634`): build 0 errors, `DecisionTree.Tests` 127 passed / 1 failed / 4 skipped, all other suites green. The one failure was -022's far-post lock, not this landing. The gate job was **cancelled before returning a verdict** and four hygiene checks never ran; see the v1.75 header entry. **RECONCILED August 7, 2026 at the main merge — this finding was implemented TWICE, concurrently, by two sessions.** `claude/football-judgment-proxy-review-pq12dz` (PR #305) landed a form that keeps §3.2.3.2's wedge-containment test and §3.1.4.3's `IsInShotPath` goal-centre-plane bound and adds the ability weighting plus a single-goalkeeper-candidate selection (its AR-1 H-1); it merged to main and passed the gate. This branch landed the form recorded above — the containment test replaced by true angular overlap, then ERR-008-022's lane bounds and ERR-008-023's keeper body radius on top. **The merge keeps this branch's form**, because the other retains precisely the 0.595 ⇒ 1.000 cliff this finding was filed against, and the goal-centre-plane bound that ERR-008-022 then measured discarding the far-post blocker on 20,213 of 20,213 sampled off-centre shooters. **Not carried, and open:** PR #305's single-goalkeeper-candidate selection — exactly one keeper (goal-line-nearest within the band) takes the P3 ability exemption — is strictly better than this branch's `gkness`, which exempts the whole 6 m band and so hands the exemption to any defender who has tracked back. That is the same Stage-0 positional-proxy limitation recorded as *not fixed* at -022, and PR #305 solved it. It is deliberately NOT grafted in this merge: it is a behaviour change, the merge is already large, and grafting an unverified behaviour change into a reconciliation commit is how the -022 landing produced `goals-still-scored = 0`. Follow-up work, on its own gate run. |
| ERR-008-022 | Decision Tree #8 §3.1.4.3 / §3.2.3.2 — the shooting lane's far bound was a plane through the goal **centre**, which cuts diagonally across the goal mouth for any off-centre shooter: the **far-post** blocker was discarded on 100% of 20,213 sampled in-range off-centre shooters, a keeper on his line at goal centre was dropped for every shooter position (reading as a fully open goal), and an opponent standing *behind* the goal line was admitted at the keeper's radius — so ERR-008-021's overlap model was denied much of the geometry it exists to price. Two further hard predicates in the same derivation were larger cliffs than the one -021 removed: `GOAL_MIN_SHOT_DIST` stepped `GoalOpeningScore` 1.000 → 0.050 across 1 cm (and with 0.050 below `MIN_GOAL_VISIBILITY`, decided whether a SHOOT option existed), and the goalkeeper predicate stepped it 0.768 → 0.311 across 2 cm. | Moderate | 3 | ✅ **Resolved August 6, 2026** (spec §3.1.4.3 + §3.2.3.2 + `OptionGenerator`/`UtilityWeights`/`DecisionTreeConstants`, same commit; doctrine P1/P3) — lane bounded by the **goal-line plane**, near bound ramped over new `[GT] SHOT_BLOCKER_NEAR_FADE_M` = 1.0 m, GK predicate replaced by a scalar `gkness` lerping radius **and** the P3 ability exemption over new `[GT] GK_PROXIMITY_FADE_M` = 2.0 m. Also corrects three false -021 verification claims: the **P5 exactness** argument (holds only for `h ≤ halfArc`; up to **2×** above it — the stated reason no recalibration was needed, withdrawn), the **test count** (10 locks / 9 evaluable / 5 fail / 4 pass, not "9 / 5 of 8"), and the **worked example** (its opponent was classified a goalkeeper, so all three of its numbers were unreachable). Suite 10 → 15 locks; the over-blocking mutant that passed all ten now fails, and both `NullAttributeView` tautologies are fixed. **COMPILED AND EXERCISED, NOT GATE-VERIFIED** — CI run 402 (PR #302, head `301c634`): build 0 errors, `DecisionTree.Tests` 127 passed / 1 failed / 4 skipped, every other suite green. The failure was this entry's own `ShotLane_FarPostBlocker_OccludesTheGoal`, which read the NEAR post; fixed in `0612bcc`, **which has never been compiled** — run 403 was evicted from the queue without starting and PR #302 was then closed. The gate job in 402 was itself cancelled before returning a verdict and four hygiene checks never ran; see the header chain v1.75. |
| ERR-008-023 | Decision Tree #8 §3.2.3.2 — the goalkeeper was assigned a `GK_BLOCKER_RADIUS_M` = **1.5 m** blocking disc rather than the 0.5 m body every other player occludes with, to "approximate arm reach + lateral movement". That is a **shot-stopping** argument, and doctrine P3's ownership ledger assigns keeper shot-stopping to Goalkeeper Mechanics #11 (§3.5 save model, §3.7.0 rush) — which prices the dive at contact — so the shooter's read of the goal charged him a second time for the same keeper. ERR-008-021 had already exempted the keeper from the *ability* term for exactly this reason and left the radius alone. The constant had never been exercised: the pre-ERR-008-022 lane bound discarded a goal-line keeper for **every** shooter position, so the disc went live for the first time at that landing and immediately removed **~42% of the goal arc on every shot** — `GoalOpeningScore` 1.000 → 0.584 at 16 m from the keeper alone, before any outfield defender. `MIN_GOAL_VISIBILITY` (§3.1.4.1 gate 4) then withholds the SHOOT option entirely below 0.12, and `blockedArc` sums blockers with no mutual-overlap correction, so a keeper plus two defenders in the lane compounds to the floor. | **High** | 1 | ✅ **Resolved August 7, 2026** (spec §3.2.3.2 + §3.2.10 + `OptionGenerator`/`UtilityWeights`, same commit; doctrine P3) — `GK_BLOCKER_RADIUS_M` **RETIRED** from the catalogue with a do-not-reintroduce note; `radius` is now `BLOCKER_RADIUS_M` for every blocker. `gkness` survives and still lerps the P3 ability exemption, so -022's continuity fix is untouched. **Found by execution, not review:** `sim_match_engine_shot_outcomes` failed `goals-still-scored = 0` across four seeds × 18 minutes on CI run `31188688249` — the first run ever to reach `MatchEngine.Tests` on this branch, that suite taking 22 m 55 s against the 3 minutes run 402 survived. This is the P5 residual the -022 entry recorded as *recorded, not fixed* under KD-W1: -022 strictly ADDS blockers to the count and landed with no recalibration, one landing after -021's population-preserving claim was withdrawn. Locked by `ShotLane_Goalkeeper_OccludesWithABodyNotAReach` (closed form 0.860770; the retired disc scored the same shot 0.583540, and the lock fails anywhere near it). `ShotLane_FarPostBlocker_OccludesTheGoal` recomputed 0.782157 → 0.927268 — its blocker stands on the goal line, so the GK read saturates. `ShotLane_GoalkeeperRead_IsContinuousAcrossItsBoundary` was about to become the **third tautology of its class in this file**: with the radius half of the read gone it moved only the ability term, which an ability-neutral blocker zeroes, so the sweep would have computed one geometric curve and passed whatever the read did. It now carries live attributes and a swing assertion (0.145 across the ramp, max step 0.004). Suite 15 → 16. **Downstream measured August 7, 2026 (v1.77):** the chain's first full run on main (CI 419) tripped two acceptance bands, both rebaselined by owner call to the post--023 baseline — keeper-contact deep dive-early `== 0` → `<= 1` (one episode 616.7 ms early, inside the pre-fix class) and close-chance cosine −0.10 → −0.16 (pooled −0.119; seed 0xD1A6D05E's entire ERR-008-018 gain returned, −0.232, while its partner held +0.078). The regressions themselves are KD-W1 calibration-pass work; the -021 P5 residual (withdrawn exactness above `h = halfArc`) and this row's uncalibrated blocker additions are the suspects. |
| ERR-029-006 | Training System #29, found at **T2 implementation**: §3.5 and §4.3 route the growth input through *"#28's public `AdvanceDay(worldDay, in trainingInputs)` (FR-PG-021)"* — a **batch** entry point taking one `TrainingInput` per player. `TacticalDirector.PlayerProgression` exposes no such method. Its only daily entry point is the per-player `GrowthProjection.AdvanceDayForPlayer(ref rec, ref life, worldDay, in training, curveEnabled)`, landed at #28 T0, and #28's own slot-1 wiring (roadmap D1) has not landed either — so #30 has nowhere to hand a batch to. The same class as ERR-041-002 and ERR-030-012: a §3/§4 sketch naming an API the cited assembly does not expose, found by trying to call it. Compounded by **FR-TR-025**, which specifies the roster handoff as reacting to #28 `RegenResult` / `RetirementResult` values — two more types #28 does not define. | Medium | 1 | ◑ Partially resolved August 6, 2026 at #29 T2 — **the handoff half is resolved; the slot-1 half is deferred to D1, deliberately.** FR-TR-025's contract is realized as roster *reconciliation* (`PlayerCareerStates.SyncToRoster`): it diffs the per-club state set against the roster #30 already holds, inserting `TrainingState.Create(Balanced)` for every unseen `PlayerId` and dropping every state whose player has left. That is the same contract keyed the same way — by `PlayerId`, at the season boundary, by the roster owner — stated over state that exists, and it starts inserting exactly the regens and dropping exactly the retirees the moment #28 T2 produces them, with no further change here. Subscribing to `RegenResult` today would be a phantom seam against a type that does not exist. **✅ FULLY RESOLVED August 8, 2026 at #28 T1/T2a (roadmap D1).** #28 now exposes the batch `ProgressionEngine.AdvanceDay(uint worldDay, in TrainingInputBatch)` FR-PG-021 specifies and #29 §3.5 composes against, and #30's slot 1 is **LIVE**: `SeasonLoop.RunCareerDaySteps` gathers the batch through the new `PlayerCareerStates.GatherTrainingInputs` (each player's `ComputeTrainingInput`, keyed by player id) and hands it to `ProgressionEngine.AdvanceDay` at slot 1, before slot 2 runs — so the FR-TR-006 order-independence is a property of the code and not only of the argument for it. The batch is **not** the phantom the deferral feared, because both sides are specified: the ids travel WITH the inputs and #28 refuses a batch that does not describe the players it is about to advance (wrong club, wrong count, wrong id, or a club omitted), which turns a drift between #29's roster view and #28's from silently mis-attributed growth into a fail-loud at the seam. Values are `TrainingInput.Neutral` today because #29's own `deepTrainingEnabled` dial is off at Stage 2 (FR-TR-007) — behaviour-neutral **by construction**, not by accident. The §3.5/§4.3 citation needed no re-anchoring: #28 grew the batch overload, which was the first of the two options this row left open. **The handoff half is unchanged** — `SyncToRoster` still reconciles against the roster #30 holds, and `RetirementResult`/`RegenResult` are deliberately still not defined, because #28's season boundary is NOT in this landing (it needs the `player-progression.regen` stream, whose survival across a save §3.5 does not pin). Retirement FLAGGING is live, being part of the draw-free daily step. **Locked by** `SeasonLoopProgressionTests.AdvanceDays_DrivesSlot1_AndTheCursorTracksTheClock` — the cursor must move by exactly (days x players), so a seam that ran once, twice or not at all is distinguishable — **and mutation-verified**: reverting slot 1 to a bare comment fails that test and the save/resume lock, both. No new RNG stream, no draw site, no `DETERMINISM_DIGEST_VERSION` or `SNAPSHOT_SCHEMA_VERSION` bump; `SEASON_SAVE_FORMAT_VERSION` **4 -> 5** for the new mandatory `PROG` sub-blob. **GATE: run locally, whole tree** (see the landing entry). |
| ERR-028-003 | Player Progression & Lifecycle #28, found at **T1/T2a implementation**: §3.2 says `PotentialAbility` is *"generated once at regen/**new-game**"*, and §3.3 gives the formula only for the REGEN path. There is no new-game derivation anywhere — `RosterGenerator` produces no PA and no `PlayerLifecycle`, and `PlayerRecord` gains no CA/PA field by FR-PG-016's own instruction. So the ~500 bootstrapped players of a new game had no PA at all, and the value is not cosmetic: PA is the F1 growth ceiling, so a default of 0 makes `TrySpendOnePoint` refuse at the ceiling on day one and the whole daily step a silent no-op that every existing unit test still passes. The owner's decision is that new-game PA is **authored data owned by #47** (New-Game Setup & Database Editor) and only regens compute it — which is right, and which #28 states nowhere; #47 is APPROVED with **no `src/` assembly**, so the seam had no producer either. | Medium | 1 | ✅ **Resolved August 8, 2026 at #28 T1/T2a, spec + code same commit.** `ProgressionEngine.SeedFrom` seeds every bootstrapped player from the new `[GT]` `NEW_GAME_PA_HEADROOM` (`PA = clamp(CA + headroom, PA_MIN, ABILITY_MAX)`), explicitly documented as a **placeholder for #47's authored value** rather than a model. Deliberately **deterministic, not drawn**: a draw here would be #28's first draw site and would force the `player-progression.regen` stream to register (FR-PG-020) for a number #47 is going to overwrite — so this landing has no draw site at all. **Recorded, NOT fixed, and it survives the owner's decision:** at the §4.3 band step a whole youth career raises CA by only ~421 of `ABILITY_MAX` = 10,000 (8 growth years x ~52.6 per point, ONE attribute per year), so the PA ceiling binds only if the authored gap is under ~420 — which no realistic authored wonderkid gap is. **PA-as-ceiling is therefore decorative whatever PA's source**, because that is a property of the growth RATE, not of PA; closing it is the Stage-3 `curveEnabled` tier's, and KD-W1 forbids retuning it in the pass that wires the subsystem. #28 §3.2/§7 to carry the #47 seam and the ~421 ceiling note. |
| ERR-028-004 | Player Progression & Lifecycle #28 §3.5: the career-state sub-blob is specified as `PROGRESSION_SAVE_FORMAT_VERSION -> DOMAIN_TAG_PLAYER_PROGRESSION -> NextPlayerId -> …` — version-first, with an **RNG hash-domain tag standing in as the block's identifier**. That is the exact defect ERR-029-005 / ERR-041-009 filed as a MUST against, arriving in a third spec: every sub-blob format in this save stack sits at version 1, so a transposed `byte[]` at the frame decodes a sibling's bytes against this layout cleanly and silently, and a version gate cannot catch it (it separates generations of ONE format, never one format from another). The domain tag is doubly wrong for the job — it is a hash-domain separator with an unrelated purpose, and ERR-029-005 recorded that the magic is *deliberately not* an RNG tag. | Medium | 1 | ✅ **Resolved August 8, 2026 at #28 T1, spec + code same commit.** New `[FIXED]` `PROGRESSION_SAVE_MAGIC` = `0x50524F47` ("PROG") written **before** the version and checked before it on decode, so sibling bytes are refused as the wrong FORMAT rather than mis-diagnosed as the wrong generation; the domain tag is not written at all. The compile-time half lands too, per the same precedent: a typed `ProgressionBlock` at the `SeasonSaveCodec.Encode` seam, joining `TrainingBlock`/`MedicalBlock`/`AppearanceBlock` — the frame now carries FOUR same-shaped opaque payloads, so a positional mistake needs a build error rather than a load-time one. #28 §3.5's layout rewritten to the shipped byte order, which F3 makes permanent (the ERR-029-004 rule). |
| ERR-028-005 | Player Progression & Lifecycle #28 §5.2 (T-PG-DET-002) and §3.1, found at **T2a implementation**: §5.2 asserts that a *"long single `AdvanceDay` gap"* matches a day-by-day advance for derived age **and** the accumulated cursor. Age is gap-independent, being derived — the cursor is not: `GrowthProjection.AdvanceDayForPlayer` adds `DailyPoints` exactly **once per call**, so a single call across a 400-day gap would bank 1 day and lose 399. The spec's own keystone lock was unsatisfiable as worded, and the existing suite hid it by asserting only the age half. Compounded by a second, sharper gap: §3.1's step carries **no per-day cursor at all**, while #30's `RunCareerDaySteps` runs a fixture day's slots TWICE (pre-round and from the advance loop, ERR-030-027) and relies on each subsystem's own cursor for idempotency — so a wired #28 would have double-accrued growth on every fixture day: a silent ~11% rate error, not a crash. | Medium | 1 | ✅ **Resolved August 8, 2026 at #28 T1/T2a, spec + code same commit.** `PlayerLifecycle` gains `LastAdvancedWorldDay` (sentinel `uint.MaxValue`, never 0 — day 0 is a legitimate world day and a zero default would read as "already advanced" and skip the first real step; the #29 `TRAINING_NOT_ADVANCED_SENTINEL` precedent, same value for the same reason), serialized in the block. `ProgressionEngine.AdvanceDay` is therefore **idempotent per day** (a day at or behind the cursor is a no-op) and **gap-complete** (a day beyond it replays every intervening day), which makes §5.2's claim true as written rather than weakening it. The first call on a never-advanced store advances exactly one day and anchors — it cannot know which day the career began accruing on — and that semantic is itself locked (`AdvanceDay_FirstCall_AdvancesExactlyOneDay`) rather than left as an assumption the gap lock leans on. #28 §3.1/§5.2 to carry the cursor and the anchoring rule. |
| ERR-030-030 | Season & Competition Loop #30, found at **#28 T2a implementation**: five sites still describe #28 as an unwired null seam and one describes a frame that no longer exists. §3.3's KD-2 block comments slot 1 *"NULL SEAM today (FR-SN-034)"*; **FR-SN-034** names #28 among the mandatory null seams; §3.5 step (d) reads `AdvanceAges() # (d) #28 — NULL SEAM today`; Appendix A's `SEASON_SAVE_FORMAT_VERSION` row and Appendix B's frame describe the **v4** six-blob frame. All are now false: slot 1 is live, and the frame is v5 with a seventh mandatory sub-blob. This is the identical stale-seam-text class corrected for #29/#41 at balance-pass AR passes 11 and 12, recurring on the next subsystem to wire — and the grep-boundary lesson from passes 8/9/11 applies, so the sweep is repo-wide rather than folder-scoped. | Medium | 1 | ✅ **Resolved August 8, 2026 at #28 T2a, spec + code same commit.** §3.3 slot 1, FR-SN-034, §3.5 step (d), Appendix A's version row and Appendix B's frame all updated to the live v5 shape; the `PROG` sub-blob's byte layout is pinned in **#28 §3.5** (its own domain) with #30 Appendix B carrying only the frame position, matching how the training/medical blocks are split between the two specs. **Recorded with it:** the frame bump **retires roadmap A3's property that a career could be reopened from the world seed alone** — #28 KD-4 makes the block the serialized roster, because the `[1,20]` attributes now evolve and a seed-rebuilt squad carries day-0 values forever. `LeagueBootstrapTests.SavedWorldSeed_RebuildsTheSameLeague` asserted exactly the retired rule and was **narrowed, not deleted**, to the half that survives: bootstrap GENERATION is still a pure function of the world seed, which is what makes a new game reproducible and what `ProgressionEngine.SeedFrom` consumes — it is simply no longer how an existing career gets its rosters back. |
| ERR-028-006 | Player Progression & Lifecycle #28 §2.2/§3.1.1, found by **adversarial review over the T1/T2a landing** — and it broke the model in production while every test stayed green. §3.1.1 pins the age anchor as `BirthWorldDay = newGameDay − Age0·DAYS_PER_YEAR` and §2.2 types it `uint`. **A new world starts on day 0**, so for every player with a non-zero generated age the anchor is NEGATIVE and unrepresentable; the implementation clamped it to 0, which makes the derived age `worldDay / 365` — so on the first `AdvanceDay` the ENTIRE LEAGUE became age 0 and stayed under 1 for a simulated year. Measured: bootstrap ages `26,22,30,26,28,30` → `0,0,0,0,0,0` after one day; bands over 100 players `growth=100 stable=0 decline=0`. Consequences: the Decline band unreachable, `RETIREMENT_AGE` never fires, `LifecycleView.Age` 0 for #31/#38, and — since KD-4 made `SquadFor` the roster authority — `Age = 0` projected into `LineupSelector`, `SquadRating` and `MatchEngine.ConfigureSquads`. **The suite could not catch it**: both #28 fixtures use `BaseDay = 100000` with the comment *"large enough that BirthWorldDay stays non-negative"* — a fixture written around the defect, avoiding the one day the product actually starts on. The ERR-030-014 class exactly. | High | 1 | ✅ **Resolved August 8, 2026, spec + code same commit.** The anchor is made REPRESENTABLE rather than clamped: `PlayerLifecycle.BirthWorldDay` becomes a signed `long` and the codec field `u32 → i64` (free: the format is v1 and unreleased, so no migration). Both clamps deleted — `ProgressionEngine.SeedLifecycle` and `RegenGenerator`. Re-measured at day 0: ages unchanged through the first advance, bands `growth=42 stable=26 decline=32`. §2.2 and §3.1.1 carry the signedness and the reason. **The landing's own headline lock was complicit and was rewritten**: `AdvanceDays_DrivesSlot1_AndTheCursorTracksTheClock` asserted `days × players == |Σ cursors|`, which is only satisfiable when every player is in ONE band — i.e. it passed *because* of this defect. It now asserts each player's cursor against his **bootstrap** age (an independent source, so it cannot re-derive the answer with the code under test) and requires all three bands to be represented. |
| ERR-028-007 | Player Progression & Lifecycle #28 / Season & Competition Loop #30, found by **adversarial review over the T1/T2a landing**: `PlayerLifecycle.LastAdvancedWorldDay` is the **FOURTH** persisted per-player cursor, and it was checked at **none** of the three boundaries the #29/#41 balance-pass AR loop spent passes 5, 6 and 9 establishing for the other three. `RequireCareerCursorsWithinClock`'s own doc still read *"Three persisted cursors, two failure modes, one owner"*. Measured: a store whose cursor was 9,999 days ahead of a clock at 0 was accepted at composition, froze growth silently through six advanced days, and was accepted by Save and by Load. Lagging is WORSE here than for the siblings — `AdvanceDay` REPLAYS a gap, so a mispaired file banks N days of growth in one call from a single day's inputs. The rule was established, mechanized and documented one landing earlier; the next subsystem to land skipped it. | High | 2 | ✅ **Resolved August 8, 2026, spec + code same commit.** `PlayerCareerStates.RequireProgressionCursorWithinClock` joins its three siblings as a shared static — ONE owner, every boundary delegating (the AR pass-9 M1 shape, which exists precisely so two hand-copied walks cannot drift). Called from `SeasonSaveManager.Save`, `SeasonSaveManager.Load` (both through the extended `RequireCareerCursorsWithinClock`) and the `SeasonLoop` constructor. Both directions refused, sentinel exempt, lag-of-one accepted (the normal state between the day steps and the clock increment). |
| ERR-028-008 | Player Progression & Lifecycle #28 / #30 save root, found by **adversarial review over the T1/T2a landing**: `SeasonSaveManager.Save(SeasonLoop, …)` defaulted a null progression store to `ProgressionEngine.Empty` via `??`, so a caller who loaded a save and resumed WITHOUT threading `SeasonSaveContents.Progression` back into the loop wrote a **zero-club roster over a file that had one**. Measured end to end: `load → clubs=4 cursorSum=600`; `resume without threading, save → clubs=0 cursorSum=0`. Every gate green — world, season, training, medical and appearance blocks intact, frame still v5, `Load` still succeeding, and the roster falling back to the caller's bootstrap so the game looks fine with a season of banked growth deleted. The same null-means-empty class this file's v1.5 row fixed for `trainingClubs`/`medicalClubs`, reopened one parameter over. | High | 1 | ✅ **Resolved August 8, 2026.** **The reviewer's proposed fix was deliberately narrowed after testing it.** Refusing `career != null && progression == null` outright breaks four existing suites that build career-wired loops over a mutable bootstrap provider — a legitimate pre-#28 composition which round-trips correctly, so that refusal would have been over-broad. The real defect is a property of the DESTINATION, not of the loop: `Save` now reads the destination file when the store is empty and refuses to overwrite a populated progression block (`RequireDestinationCarriesNoRoster`). An empty store may create a file, and may overwrite an empty one; it may never erase a roster. An unreadable or foreign destination is not this guard's business and is overwritten as before. |
| ERR-028-009 | Player Progression & Lifecycle #28 §2.3/§3.1, found by **adversarial review over the T1/T2a landing**: #28 adopted the siblings' `uint.MaxValue` never-advanced sentinel but shipped **no F8 guard**, while #29 (`TrainingStep`) and #41 (`MedicalStep`) both refuse `worldDay == sentinel` under an explicit F8 row landed one day earlier. Two proven consequences: `AdvanceDay(uint.MaxValue)` STORES the sentinel, so the step is no longer idempotent (a second identical call accrued again, breaking the ERR-030-027 contract); and the gap-replay loop `for (d = cursor + 1; d <= worldDay; d++)` never terminates at `uint.MaxValue` — demonstrated by a probe that hung until killed. The folder-boundary lesson the siblings recorded (a rule established in one spec's folder does not reach the next) recurring immediately. | Medium | 1 | ✅ **Resolved August 8, 2026, spec + code same commit.** `ProgressionEngine.AdvanceDay` refuses the sentinel as a world day up front (`ArgumentOutOfRangeException`), before any validation or mutation; #28 §2.3 gains the F8 row and §3.1's pseudocode the guard, matching both siblings. |
| ERR-028-010 | Season & Competition Loop #30 / Player Progression #28, found by **adversarial review over the T1/T2a landing (High)**: a progression-wired `SeasonLoop` could not play a round **through any public API**. The constructor projects the provider from the store into a private field, and `AdvanceAndPlayNextRound(ISquadProvider)` demands reference-equality with that instance — which nothing exposed. Every caller-constructible provider (the `League`, a fresh `ProgressionSquads`, a wrapper) was refused; the reviewer reached the working code path only by reflection. So the configuration the whole landing exists to enable could advance days and save, and nothing else — and no test covered it, because the landing's own suite only ever called `AdvanceDays`. A second defect sat behind the first: the overload resolves the round from the CALLER's provider, not the loop's, so merely relaxing the reference gate would have played the round against the day-0 bootstrap. | High | 2 | ✅ **Resolved August 9, 2026.** New parameterless `SeasonLoop.AdvanceAndPlayNextRound()` resolves through the loop's own `_careerSquads`, which removes the two-provider hazard by construction rather than guarding against it by hand; the `ISquadProvider` overload remains as the careerless / caller-supplied path and keeps its reference gate. Both delegate to one extracted `PlayNextRound` body — two copies of a twelve-guard round resolution would be the parallel-surface defect this repo keeps filing. Locked by three cases: a wired loop plays a full round; a round played after growth has banked still resolves against the store; and the no-argument overload fails loud on a loop that owns no provider. |
| ERR-028-011 | Player Progression #28 / #30 save root, the remaining **Medium** findings of the same review, fixed together. **(a)** `ProgressionSaveCodec.Encode` wrote blobs its own `ProgressionEngine.Restore` refuses — cross-club duplicate player ids passed Encode and Decode, because `CanonicalOrder` only rejects a duplicate WITHIN a club, and were then rejected at Restore; verbatim the balance-pass AR pass-4 Medium, recurring in the landing whose engine guard cites the same ERR. **(b)** `ProgressionEngine.FromBlocks` accepted a `nextPlayerId` at or behind the ids it carries, defeating the stated purpose of serializing the cursor (FR-PG-011) — the next regen would collide with a LIVE player. **(c)** Nothing checked that the PROG block describes the same squads as the three career blocks, so Save wrote and Load accepted a file whose progression set covered {0,1} while training/medical/appearance covered {0,1,2,3}; the failure surfaced later and elsewhere, after the file had been declared good. **(d)** `Constructor_WithAProgressionStoreMissingASeasonClub_IsRefused` was a **tautology** — its career covered the same three clubs as its store, so the pre-existing career-covers-the-season check fired first and deleting the new predicate left the suite green. | Medium | 4 | ✅ **Resolved August 9, 2026.** (a) a shared `RequireGloballyUniquePlayerIds` runs at Encode AND Decode, so neither a written nor a read file can carry the collision; (b) `FromBlocks` refuses a cursor at or behind its highest carried id; (c) `RequireCoherentCareerBlocks` becomes a FOUR-way gate and now runs at Load as well as Save (an empty store is exempt — that is the honest pre-#28 composition); (d) the test's career now covers the whole season, so only the progression predicate can refuse it, and it asserts on `ParamName`. |
| ERR-028-012 | Player Progression #28 — the **Low** findings of the same review, recorded together because two of them are decisions rather than repairs. Fixed: the decoded block had **no range gate** on attributes, weak foot or age, so a corrupt `9999` flowed straight into `PlayerAttributeProjection` and the match engine — higher stakes than for the sibling career codecs, since nothing downstream re-derives these from a trusted source (L3); `default(ClubCareerStates)` was diagnosed as *"Duplicate club id 0"* because keying ran before the bind check (L4); `SeasonSaveConstants` still described a six-blob frame at "Value: 4" and omitted `ProgressionSaveCodec.Decode` from its decoder list (L5); `RollToNextSeason` step (d) still read *"empty until #28 T2"* when the daily step is live and age is derived there, so no "age advance" remains at that position (L6); `SquadFor` copied the roster TWICE per projection, `Squad`'s own constructor already snapshot-copying (L7a); and the landing's records understated `SeasonSave.Tests` and asserted a `SeasonLoop.cs` version chain that never existed (L8 / M7). **Recorded, deliberately NOT fixed — L7b:** the reviewer's proposal to resolve each club's squad once per world day and share it across the KD-2 slots would be a **silent behaviour change**, not an optimisation: slot 1 MUTATES the store, so slots 2 and 4 would then read attributes from before that mutation and price a day's conditioning and injury risk off a one-day-stale roster on exactly the days growth lands. The per-slot resolve is correct as it stands; the cost is one array copy per club per slot per world day, off the 60 Hz path. Revisit only with a cache invalidated by the slot-1 write. **L1/L2 corrected as claims rather than code:** the Neutral-batch test is a SHAPE lock (an empty `TrainingInput` makes any batch equal Neutral, so it cannot enforce FR-PG-009 until T3), and the world-digest test is a boundary guard that no change to slot 1 can turn red — both now say so, so neither is read as evidence it does not carry. | Low | 8 | ✅ **Resolved August 9, 2026** (six repaired, L7b rejected with reasoning, L1/L2 re-scoped). |
| ERR-028-013 | Player Progression #28 / Season & Competition Loop #30, found by **adversarial review pass 2 over the T1/T2a landing (High + Medium)**: the constructor conflated "a progression store was supplied" with "#28 is the roster authority", and the two are not the same thing. **(a) The High.** `SeasonSaveContents.Progression` is **never null** — a loop composed without #28 saves a well-formed ZERO-club PROG block, which `SeasonSaveManager.Save` documents as "the honest pre-#28 composition" and its own ERR-028-008 refusal message instructs the caller to resume from. But an empty store was treated as wired: threaded back beside the caller's provider it tripped the two-authorities refusal, and passed alone it failed the season-coverage check because it carries no clubs. The only way through was to pass `null` **instead of** the loaded store — an undocumented special case, and the exact opposite of what the save root advises. So a save the system deliberately writes could not be resumed through its own documented path. It survived because **nothing anywhere reconstructed a `SeasonLoop` from `SeasonSaveManager.Load` output**: all 28 `Load` call sites assert on the contents and stop, so the one operation a real game performs on every load had no coverage in either shape. **(b) The Medium.** The pairing rule `(career == null) != (squads == null)` predates #28 and was right when a bare `ISquadProvider` drove nothing — but a progression store drives slot 1 and **is** its own provider, so requiring #29/#41 career state beside it made #28 unusable without two subsystems it does not depend on, and made slot 1's own `_career == null` branch — the FR-PG-009 "no training anywhere" path, and the only production consumer of `TrainingInputBatch.Neutral` — **provably unreachable**. A guard on a branch nothing can execute ships green forever; verbatim the class balance-pass AR pass 14 caught on `RecoveryMax`, recurring one landing later. | High | 2 | ✅ **Resolved August 9, 2026.** A single `progressionIsRoster = store != null && store.ClubCount > 0` drives the two-authorities refusal, the provider projection, the coverage gate, the cursor-vs-clock gate and what `_progression` retains — an empty store is the ABSENCE of #28, stated rather than arrived at, and a loop built from one still round-trips to the same zero-club block via `loop.Progression ?? Empty`. The pairing rule splits in two: a career still requires a provider, but a provider with a populated store behind it no longer requires a career. Locked by four cases — an empty store composes beside a provider; `contents.Progression` threads back verbatim through a real save/load/resume; a store with no career drives slot 1 on the Neutral batch; and a bare provider with nothing behind it is STILL refused, so the relaxation did not delete a real guard. |
| ERR-028-014 | Player Progression #28 / Season & Competition Loop #30, found by **adversarial review pass 2 (Medium)** and resolved on advice from a second-opinion design review: the never-advanced cursor sentinel was **exempt from the cursor-vs-clock rule at all three boundaries**, so a store seeded on world day 0 composed against a clock at day 3650 banked **one day** of growth for a decade while every player's DERIVED age jumped the whole span — silently, with every gate green. **The diagnosis is a sibling-copy error, and it is the part worth remembering.** "A never-advanced state is coherent at any clock" is TRUE for #29 and #41: their fresh states (fatigue 0, no injuries) carry no clock-anchored quantity, so they mean the same thing on every world day. #28's fresh state is the **only one of the four** that carries one — age is derived from `BirthWorldDay` — so it means something DIFFERENT at every clock value. The fourth cursor inherited the siblings' exemption without checking the premise the exemption rests on, and the predicate's own doc already said lag is *worse* here because `AdvanceDay` replays a gap — then exempted the one state where the gap is unbounded and never replayed. §3.1's pseudocode confessed it in a comment (the store "cannot know an earlier start"), which was false: `SeedFrom` is handed `newGameWorldDay` and was discarding it as a cursor anchor. **Two tests were locking the defect as intended behaviour** — `Constructor_WithSentinelProgressionCursor_IsAcceptedAtAnyClock` asserted the exemption was correct, and `AdvanceDay_FirstCall_AdvancesExactlyOneDay` asserted the collapse was correct on the reasoning the spec comment gave. Both are INVERTED, not adjusted. | Medium | 1 | ✅ **Resolved August 9, 2026.** The seed day IS the cursor: `SeedFrom` anchors `LastAdvancedWorldDay = newGameWorldDay`, `FromBlocks` REFUSES the sentinel, `AdvancePlayerTo`'s never-advanced branch is deleted, and the exemption is removed. This closes the hole by **deleting a special case rather than adding a gate** — with the cursor anchored, the existing bidirectional predicate refuses the day-0-store-at-day-3650 pairing with no new code. Two rejected alternatives, recorded because each looked cheaper: recovering the seed day arithmetically as `BirthWorldDay + Age·DAYS_PER_YEAR` rests on an invariant NO entry point enforces (`FromBlocks` is public and accepts any age/anchor pair) and is the implicit-agreement-between-two-surfaces shape this log keeps re-filing; and persisting a store-level seed day cannot express the deferred regen path at all, since one store will hold players seeded on many different days. A third, `cursor = newGameWorldDay − 1`, would have preserved every existing test's meaning and **underflows to the sentinel at day 0** — the day-0 trap, re-armed at the commonest input. **Verified by mutation:** reverting the anchor to the sentinel fails 9 of 85. **Honest scope note — the exemption removal in `RequireProgressionCursorWithinClock` is inert, not load-bearing:** with `SeedFrom` anchoring and `FromBlocks` refusing, no `ProgressionEngine` can hold a sentinel cursor, so the predicate can never see one. It is removed as dead-special-case cleanup; the two load-bearing changes are the anchor and the `FromBlocks` refusal. **One hazard this created, recorded:** `AdvanceDay` replays a gap DAY BY DAY, so its cost is O(gap) with no bound — the anchoring turned a first call at an arbitrary day from O(1) into O(span), and the suite's own `AdvanceDay_OneDayBelowTheSentinel` case (seeded at day 100,000, advancing to `sentinel−1`) became a ~4.29-billion-iteration loop that HUNG the run. The fixture now seeds beside its target. The public API remains unbounded; every boundary that matters bounds the lag to 1, so this is recorded rather than capped — inventing a maximum-gap constant would be an arbitrary number, and the fail-loud boundaries already refuse every legitimate route to a large gap. |
| ERR-030-031 | Season & Competition Loop #30 / Player Progression #28, found by **adversarial review pass 3** — the ERR-028-014 sweep stopped at its own spec folder, which is this project's **fifth** recurrence of the grep-boundary widening class. ERR-028-014 retired the never-advanced sentinel from #28's legal store states and corrected #28 §2 and §3 the same day. Three documents outside that folder still described the retired world as current, and two of them are the ones an implementer of the SAVE ROOT reads: **(a)** #30 §2.3's **F8 row** enumerated only THREE persisted per-player cursors (#29, #41, the appearance anchor) and said the sentinel is exempt — while `SeasonSaveManager.cs` labels #28's in its own source as *"The FOURTH persisted per-player cursor"* and enforces it at Save, Load AND composition. **(b)** #30 **Appendix B.1** made the count explicit — *"all **three** persisted per-player cursors"* — and added the blanket claim *"The sentinel (never-advanced) is **exempt in every case**"*, which ERR-028-014 had made false that same day: #28 alone has no exemption, because it is the only one of the four whose fresh state carries a clock-anchored quantity (age derives from `BirthWorldDay`), so "never advanced" means something different at every clock value for #28 and the same thing at every clock value for its siblings. **(c)** #28 §5.1 still documented the RETIRED behaviour as current, quoting the reasoning ERR-028-014 identified as false — *"since it cannot know how far in the past the career actually began accruing"* — for a test that had been renamed and whose assertion had been **inverted**. A reader following §5 would have rebuilt the defect. | High | 3 | ✅ **Resolved August 9, 2026.** F8 covers all four cursors and states #28's exception; Appendix B.1 carries the corrected count and the REASON for the asymmetry rather than just the rule; §5.1 describes the shipped behaviour and records the inversion. **Also closed with it:** §5 gained ids for the ~17 mutation-audit locks that had landed with none (`T-PG-BLOCK-001..007`, `T-PG-BATCH-001`, `T-PG-CODEC-001..007`, `T-PG-SAVE-007/008` — collision-checked against the 24 pre-existing ids), each recorded as a lock whose guard a mutation sweep had proven untested; and #28 Appendix A, which had not been touched since **before the T0 landing**, gained the seven shipped constants it was missing. |
| ERR-028-015 | Player Progression #28 / Season & Competition Loop #30, found by **adversarial review pass 3 (High ×2)** — both defects were INTRODUCED by pass 2's own fixes, which is the finding that matters more than either of them. **(a) ERR-028-014 silently disarmed three locks.** Anchoring the cursor at the seed day made `AdvanceDay(seedDay, …)` a total no-op — `AdvancePlayerTo` returns before the growth step and the retirement check — and three tests called exactly that and then asserted on state the code under test never touched. **Verified by mutation, and the mutants overturn the static audit that found them:** deleting the idempotency guard OUTRIGHT left **all 469 tests across both suites green** (ERR-030-027's property — that #30 runs slot 1 twice on every fixture day — was genuinely unguarded, not merely weakly tested); deleting the retirement age comparison so that EVERY player retires on every advance left 85/85 green (FR-PG-013's discrimination unguarded); and reinstating the ERR-028-006 birth clamp no longer failed its own designated regression lock — though, contrary to the audit's claim, two sibling tests still caught it, so that defect was never unguarded. **The ERR-028-014 commit claimed it had swept for this.** It had swept for tests that FAILED, not for tests that started PASSING for the wrong reason — and only the second class is silent. **(b) The ERR-028-013 relaxation reopened the ERR-028-010 gate.** `AdvanceAndPlayNextRound(ISquadProvider)`'s two-provider refusal was keyed on `_career != null`, which was equivalent to `_careerSquads != null` only because the old pairing rule made them a biconditional. ERR-028-013 broke that biconditional deliberately — a populated progression store now composes without #29/#41 career state — and left the gate keyed on the half that had stopped covering the case. A progression-only loop skipped the gate entirely, so a caller could hand in the day-0 bootstrap and have the round resolved against attributes the store had already grown away from, silently. It survived because the composition ERR-028-013 created was only ever exercised through `AdvanceDays`: **a configuration that could advance days and save and nothing else — verbatim the ERR-028-010 shape, in the fix that cites it.** | High | 2 | ✅ **Resolved August 10, 2026.** The three locks now advance to a genuinely LIVED day, and the idempotency case carries a precondition that the first call actually accrued — so the fixture cannot silently stop reaching the code again. The gate is rekeyed to `_careerSquads`, the authority the loop OWNS whichever subsystem put it there. A round-play case for the progression-only composition closes the coverage gap that hid it. Also closed: `SeedFrom` now carries the F8 sentinel refusal (anchoring the cursor made the seed site a second way to write the one value `FromBlocks` refuses, producing a store that can be neither saved, restored nor advanced). |
| ERR-028-016 | Player Progression #28, **adversarial review pass 4** — the pass's headline finding is a correction to **pass 3's own comment**, and the rest is the half-guard class recurring. **(a) Medium — an over-attributed comment, and a statement with no isolating test.** Pass 3 rewrote `AdvancePlayerTo`'s else-branch comment to say the branch is load-bearing "only against a BACKWARD call" because without it the cursor regresses. That is wrong in a precise way: what prevents cursor regression is the `if` **condition** (the assignment sits inside the `if`, so a backward call never reaches it either way) — the bare `return;` prevents something else entirely, namely the §3.4 **retirement evaluation running on a call that advanced nothing**. Not inert: a player not yet flagged whose `rec.Age` already satisfies `RETIREMENT_AGE` would be flagged on a backward call, stamping `RetirementDay` with a day EARLIER than his own cursor. Worse, pass 3's rewrite **discarded** the original comment's accurate claim ("the retirement flag below is not re-stamped") while fixing a different overclaim — an over-correction. Both halves are now stated separately, and `AdvanceDay_BackwardCall_DoesNotEvaluateRetirement` isolates the `return;` from the condition: deleting ONLY `return;` fails it and nothing else, while the sibling condition lock stays green. **(b) Medium — three decode range checks were half-tested.** `attributes`, `weakFoot` and `potentialAbility` are each a two-sided OR, and each had a test for only ONE side (MAX, MAX, MIN respectively). Deleting the untested half of any one left the whole suite green — the same shape as the 15 survivors pass 2's mutation sweep found. Three new tests, one per missing side. **(c) Low — five guards with no isolating test**: `SeedFrom` duplicate CLUB id (distinct from the duplicate-PLAYER-id case), `SeedFrom` null array and null element, `FromBlocks` null array and unbound element, and `ValidateBatch`'s unbound-entry branch. All now locked. **(d) Low** — `SeedFrom_AtTheSentinelWorldDay_IsRefused` proved the guard FIRES but not that it is NARROW; a seed at `SENTINEL - 1` must succeed, mirroring `AdvanceDay`'s existing narrowness proof. And `SeedFrom`'s XML doc did not declare the `ArgumentOutOfRangeException` its own new guard throws. | Medium | 4 | ✅ **Resolved August 10, 2026.** Suite 89 → **100**, every new lock proven by executing its mutation, not by reasoning. **Two findings recorded, not fixed.** (1) `SeedFrom`'s explicit `byClub.ContainsKey` duplicate-club check is **redundant** — `SortedDictionary.Add` already throws `ArgumentException` on a duplicate key, so deleting the check changes nothing; the discriminating mutation is replacing `Add` with a silent-overwrite indexer assignment, which is what the new test actually locks. The check is kept for the explicit message, and the test comment records why the naive mutation does not kill it. (2) `ValidateBatch` evaluates its positional club-id check BEFORE its bind check, so a `default(ClubTrainingInputs)` entry (whose `ClubId` is 0) is only reachable by the bind check when the store carries a club at id 0 — the new test seeds there deliberately. Ordering is not wrong, but it makes the bind branch unreachable for any other club id. |
| ERR-041-010 | Injuries & Medical #41, found at **T2 implementation**: two gaps in the same section pair. (a) **FR-MD-025** specifies the roster handoff against #28 `RegenResult` / `RetirementResult` values, which `TacticalDirector.PlayerProgression` does not define — ERR-029-006's finding, mirrored, and filed separately because §2/§5.2 are #41's own normative text. (b) §3.5's composition sketch sources `recentMatchLoad` from *"#30's fixture result"*, and #30 has no per-player appearance record to source it from: `MatchResult` carries a scoreline, `SeasonState` carries fixtures and a table, and neither #29's nor #41's save block may describe the other's domain, so an appearance counter has no persisted home anywhere in the current stack. `AppearanceLoadWeight` is a non-zero `[GT]` (150), so the term is not vacuous — it is simply unsupplied. | Medium | 1 | ◑ Partially resolved August 6, 2026 at #41 T2. (a) resolved exactly as ERR-029-006: `PlayerCareerStates.SyncToRoster` reconciles both state sets against the roster in one pass, inserting `InjuryState.Create()` — never `default`, the F1/F6 day-0 sentinel trap — and removing departures, keyed by `PlayerId` at the season boundary. (b) **✅ closed August 7, 2026 at the balance pass (D2)**: the per-player appearance record lands as #30-side state with its own persisted home — `AppearanceState` (a lazily-shifted day-bitmask: `(RecentBits, BitsAsOfWorldDay)`, shifted at READ time so no daily mutation step, no new KD-2 slot and no third idempotency cursor exists) held by `PlayerCareerStates`, written by `SeasonLoop.AdvanceAndPlayNextRound` for both clubs' fielded XIs after each fixture resolves (the ids come from the new `SquadRating.StartingElevenPlayerIds` — the same single `TrySelect` walk, not a second selection surface), serialized as the frame's new mandatory `APPR` v1 sub-blob (`AppearanceBlock` typed at the Encode seam, magic-led per ERR-029-005; `SEASON_SAVE_FORMAT_VERSION` 3→4), and read into the FR-MD-010 `MatchLoad` at slot 4 through `AppearanceWindow.AppearanceDaysOn` — whose window covers the `AppearanceWindowDays` `[GT]` days strictly BEFORE the draw day, never the current day, which is what makes the term coherent with ERR-030-027's pre-round draw (a match on day *d* first feeds the draw on *d+1*). FR-MD-010's false premise ("a count #30's fixture result already tracks") corrected in the same commit (`section-2.md` v0.5); #30 Appendix B v0.5 + `unified-season-save-design.md` §3.1 carry the frame change. The original entry's recompute-from-fixtures rejection stands — the record is written from who was actually fielded, filter included. `IsAvailable` (FR-MD-023) and the slot-4 step itself are live. Gate: **PASSED** — executed locally, whole tree, quarantine empty (verdict line in `CHANGELOG.md`). |
| ERR-030-026 | Season & Competition Loop #30, found by **adversarial review over the T2 landing (pass 5)**: §3.3's KD-2 tick order pins nine day-slots but has **no slot for playing the round** — a round is resolved by a separate command (`AdvanceAndPlayNextRound`), not by the day advance — so where a fixture sits relative to slot 2 (#29 training) and slot 4 (#41 injuries) is not specified anywhere in #30, and in the implementation it falls out of `AdvanceToNextFixtureDay`'s loop condition alone (`while (CurrentWorldTick < targetDay)`, which stops on *reaching* the fixture day). The emergent answer is **play the round, then process matchday**. That is correct for #41's occurrence draw — an injury sustained in a match must be drawn after it, and it is what makes the FR-MD-010 `MatchLoad` term coherent once ERR-041-010(b) supplies a per-player appearance record — and **wrong for #41's recovery countdown, which shares the same atomic `AdvanceMedicalDay` step**: a player whose `RecoveryRemaining` reaches 0 on matchday has his decrement applied after the round, so he misses a fixture he had served his time for and **every injury runs one matchday longer than its assigned tier**. Invisible today (the occurrence dial ships off, FR-MD-027, so nobody is ever injured) and invisible to the suites either way, since `AWholeSeason_PlaysWithTheCareerWired` asserts `lastLivedDay = CurrentWorldTick - 1` which holds under both orders. The cost is not today's behaviour: it is that the balance pass would fit `RecoveryDaysPerTickBase` and the per-tier day assignments straight through an unstated convention and absorb the bias into the constants. | Medium | 1 | ◑ **Convention stated and pinned August 6, 2026 at the T2 AR pass; the split remains open for the balance pass.** Option (a) taken — the emergent order is adopted rather than changed, because splitting recovery from occurrence would alter #41's step contract (one `AdvanceMedicalDay` per player-day, FR-MD-022) and that is a #41 revision, not a #30 wiring fix. The order is now documented at all three sites that determine it (`AdvanceToNextFixtureDay`, `RunWorldTickInFixedOrder`, `AdvanceAndPlayNextRound`), including the one-matchday recovery bias by name, and locked by `SeasonLoopCareerTests.DayAdvance_StopsBeforeTheFixtureDaysOwnSteps` — which asserts both halves: that `LastAdvancedWorldDay` is `fixtureDay − 1` at kickoff, and that a player with one recovery day outstanding is unavailable for that round and fit immediately after it. **What was deferred:** whether #41 should expose recovery and occurrence as separately callable halves so recovery can run before the round and occurrence after it. **✅ Closed August 7, 2026 by ERR-030-027 (the balance pass, owner-authorized), which achieves both orderings WITHOUT the split**: the fixture day's whole atomic day-step runs pre-round inside `AdvanceAndPlayNextRound`, so recovery lands before selection while match participation reaches the occurrence draw through the FR-MD-010 appearance window (never containing the current day). FR-MD-022's one-step contract, KD-6, and the medical save format all survive verbatim — the split's costs (a second persisted cursor, `MEDICAL_SAVE_FORMAT_VERSION` 1→2, a KD-6 revision) bought nothing the wiring pin did not. See ERR-030-027. |
| ERR-030-027 | Season & Competition Loop #30, the #29/#41 **balance pass D1** (closing ERR-030-026's deferred half, owner-authorized): §3.3 needed a pinned answer to "where does the round sit relative to the fixture day's own day-slots", and the interim ERR-030-026 convention (play the round, then process matchday) ran every injury one matchday longer than its tier — a bias the balance pass would otherwise have fitted `RecoveryDaysPerTickBase` and every tier-day constant through. The deferred proposal (split `AdvanceMedicalDay` into separately callable halves) prices at a second persisted cursor + `MEDICAL_SAVE_FORMAT_VERSION` 1→2 with no migration (KD-7/F3) + a KD-6 revision, because `AdvanceAndPlayNextRound` returning between the halves is an ordinary save point. | Medium | 1 | ✅ **Resolved August 7, 2026, spec + code same commit** — the no-split shape both council advisors converged on: **the fixture day's own KD-2 slots run at the top of `AdvanceAndPlayNextRound`, pre-round** (new §3.3.2; `SeasonLoop.RunCareerDaySteps` extracted, shared by both callers, placed after every guard so a refused call advances no cursor, run over the WHOLE career so no-fixture clubs stay synchronised). Step 12 (world-day tick) still runs on the next advance, whose re-entry of the same day is a cursor no-op (F6). Recovery therefore lands before selection (tiers mean what they say); the occurrence draw sits on matchday morning, fed by the FR-MD-010 appearance window, which never contains the current day — a match on day *d* first feeds the draw on *d+1*. FR-MD-022, KD-6 and the medical format survive verbatim; no format-version change, no RNG/domain-tag/draw-site change, no `DETERMINISM_DIGEST_VERSION` bump. Behaviour changes only where a career is wired: cursor positions around a round (locked), and the served-his-time player playing his round (locked). `section-3.md` v1.2 (§3.3 comment, §3.3.2, §3.4 opening); `SeasonLoop.cs` v1.9; `DayAdvance_StopsBeforeTheFixtureDaysOwnSteps` rewritten to assert the new convention both ways, `AWholeSeason_PlaysWithTheCareerWired` cursor expectation → `CurrentWorldTick`. |
| ERR-041-011 | Injuries & Medical #41, the **balance pass (D3)**: §3.4/Appendix A carried three scale defects. (a) `OCCURRENCE_DRAW_DENOM` was `[DERIVED] == INJURY_RISK_MAX`, a `[GT]` — and the draw is `hash % denominator`, so the denominator determines the VALUE of every draw: one config edit re-rolled every career's injury luck, unrecorded (#50's `SaveOriginStamp` is unbuilt). (b) The assembly had no exposure-independent term, so the default focus converged on injury-proof-forever (the fifth AR pass's measured absurdity) — and the other two measured absurdities (23%/43% per day) were two to three orders out because career-scale inputs sat on the same 10,000 scale as the denominator. (c) `APPEARANCE_LOAD_WEIGHT` had never been fitted against a real appearance record (none existed until ERR-041-010(b)). | Medium | 3 | ✅ **Resolved August 7, 2026, spec + code same commit.** `OCCURRENCE_DRAW_DENOM` → `[FIXED]` 1,000,000, decoupled; invariant `INJURY_RISK_MAX ≤ DENOM` fail-loud at the draw site (the old negative-denominator guard is unrepresentable against a const); §3.4 gains `BASELINE_DAILY_RISK` (4000) inside the sum BEFORE the mitigation — position normative; `APPEARANCE_LOAD_WEIGHT` 150 → 5600; `INJURY_RISK_MAX` re-tagged `[CROSS: #29 Appendix A]` (ERR-041-003's back-prop discharged; ERR-029-007 corrects the #29 side). §3.6 re-derived (6600; congestion clamps at the ceiling — 1% when this row was written, 1.6% since the pass-1 headroom raise (InjuryRiskMax 16000); the recorded Stage-2 compression R-2's refit inherits). Measured by the season-scale instrument over 8 seeds: league ~780/season (~39/club), starters 2.08, reserves 1.12, unavailability 9.4% — in the E-1-derived band, locked league-wide with perturbation-proof bands. Characterization test moved to AFTER numbers at per-100k resolution; forced-occurrence tests moved to a deterministic hot-day scan (certainty is structurally unreachable at 1%/day max). `section-3.md` v0.4, `appendices.md` v0.4, `#29 appendices.md` v0.4; `InjuriesMedicalConstants` v1.3, `MedicalStep` v1.4, `TrainingSystemConstants` v1.3. |
| ERR-041-012 | Injuries & Medical #41, the **balance pass (D4)**: §4.5 and FR-MD-005 still required registering an `injuries.occurrence` stream on `DeterministicRngService` at `SubsystemOrdinals.InjuriesMedical = 92` "at the first draw site" — a requirement that was self-contradictory from approval (a registered stream is cursor-positioned; FR-MD-006/007 of the same spec forbid a cursor) and was resolved in CODE at T0 as the keyed derivation (ERR-041-002), leaving the normative text describing a stream that does not exist and must not. Arming the dial (FR-MD-027) is the moment stale text would govern a live subsystem — the integrity advisor's pre-landing obligation. | Low | 2 | ✅ **Resolved August 7, 2026** — §4.5 rewritten from "stream registration" to the keyed derivation that exists (`DrawOccurrence`: domain tag → playerId → action ordinal, each through a SplitMix64 finalizer, reduced into the `[FIXED]` denominator; the #30 `FixtureKey`/`LeagueBootstrap` precedent); ordinal 92 pinned as **deliberately unallocated** (an ordinal exists only to key a registered stream — the zero-consumer phantom FR-LW-031 forbids; ERR-030-012 posture); FR-MD-005 re-anchored the same way, discharging the re-anchor ERR-041-002 deferred to "the next #41 revision". FR-MD-027's stream-independence clause noted as vacuous by construction. `section-2.md` v0.6, `section-4.md` v0.4. |
| ERR-029-007 | Training System #29, the **balance pass (D3)**: Appendix A's `INJURY_RISK_MAX` row (and the `TrainingSystemConstants.InjuryRiskMax` doc) described the value as the scale #41's draw is taken on — "changing the value here changes both sides at once". True until ERR-041-011 decoupled the draw denominator; afterwards the doc would have told a tuner that raising the ceiling rescales probabilities uniformly, when it now raises the probability CAP (and, past the `[FIXED]` denominator, trips the draw-site invariant). | Low | 1 | ✅ **Resolved August 7, 2026** — #29 Appendix A row and the catalogue doc state the post-decoupling meaning: sole owner of the shared clamp ceiling (`[CROSS]`-mirrored by #41, ERR-041-003), the daily probability cap (1%/day when this row was written; 1.6% since the pass-1 headroom raise), MUST stay ≤ `OCCURRENCE_DRAW_DENOM` (enforced fail-loud at #41's draw site). Value unchanged; no behaviour change. `training-system/appendices.md` v0.4, `TrainingSystemConstants.cs` v1.3. |
| ERR-041-019 | Injuries & Medical #41, **balance-pass AR pass 3 (the High)**: §3.1.1's occurrence-draw key is `(worldSeed, playerId, worldDay×16+purpose)` — **no club term** — but #27 (squad/player data KD-3) promises `PlayerId` uniqueness only WITHIN a club, and #30's career state is keyed `(ClubId, PlayerId)` on that premise. Nothing checked the difference: two clubs carrying one id would draw bit-identical injury luck on every world day forever, with matching severities whenever risks are close — silent, total, indistinguishable from chance. Safe only by accident of `RosterGenerator`'s `clubId × N + local` formula, unstated as a precondition anywhere; armed at D4, so live. **The very next suite run proved the hazard real**: the guard's first execution caught `RollToNextSeason_ReconcilesRosterMembership`'s own regen fixture handing club 1 the suffix `N+5` — id `2N+5`, which IS club 2's local-5 player. The project's first "new allocator" after the generator was its own test, and it collided. | High | 4 | ✅ **Resolved August 8, 2026, spec + code same commit** — the precondition made explicit and enforced at the one layer that spans clubs: `PlayerCareerStates.RequireGloballyUniquePlayerIds`, called at ALL THREE id entry points (`ForLeague`, `FromBlocks`, `PrepareRosterSync` — the validating half, since `CommitRosterSync`'s contract is cannot-fail; the sync is the path #42 intake / #31 transfers actually arrive through). §3.1.1 states the global-uniqueness contract and REFUSES the alternative fix (a club term in the key re-rolls every career — the ERR-041-011 argument — and would change a transferred player's luck with his club); `PlayerRecord.PlayerId`'s doc carries the career-level requirement over KD-3's club scope. Locked by `ACrossClubDuplicatePlayerId_IsRefusedAtEveryIdEntryPoint` (all three points) + the corrected roll fixture. `section-3.md` v0.5, `PlayerCareerStates.cs` v1.8, `PlayerRecord.cs` v1.1. **AR pass 4 addenda:** the same guard now also runs inside `SeasonSaveManager.Save`'s coherence gate (M1 — Save could still write the one file `FromBlocks` refuses on this predicate), the owning-spec back-prop landed as **ERR-027-004** (M3 — FR-SQ-010 carried club-scoped-full-stop while the consuming spec carried the contract), and the transfer residual is RECORDED, NOT FIXED (M4 — the only cross-club handoff path, per-club roster reconciliation, resets a moved player's career state entirely; #31's arrival obligation, recorded at #41 §3.1.1 and the code site). |
| ERR-027-004 | Squad/Player Data #27, **balance-pass AR pass 4 (M3)**: ERR-041-019 established that #41's club-less occurrence-draw key requires GLOBALLY unique `PlayerId`s, but the contract landed only in the CONSUMING spec (#41 §3.1.1) and a code comment — FR-SQ-010, the row the future allocators (#42 intake, #31 transfers) will actually be written against, still promised club-scoped uniqueness full stop. | Low | 2 | ✅ **Resolved August 8, 2026** — FR-SQ-010 and §2.2.3 amended: KD-3's formula stands, with the career-level global-uniqueness requirement stated on the owning row, citing #41 §3.1.1 / ERR-041-019 and the `PlayerCareerStates` enforcement points. `squad-player-data/section-2.md` v0.3, `PlayerRecord.cs` v1.2. **AR pass 5 addendum:** KD-3 itself (#27 §1.4) gains the pointer — the key decision an allocator author reads first said nothing while the FR carried the contract (`section-1.md` v0.3). |
| ERR-030-028 | Season & Competition Loop #30, **balance-pass AR pass 5 (M1)**: the `APPR` appearance sub-blob's byte layout was specified in NO spec — Appendix B pinned only the block's frame position and magic, and the layout existed solely in `AppearanceSaveCodec.cs`'s own XML comment — while **F3 refuses every cross-version migration, so the first written layout IS the format permanently**. This is ERR-029-004 / ERR-041-008's exact reasoning, recorded when the sibling #29/#41 blocks got their layouts pinned — and missed on the very next block, created by the landing that cited both. Every `SEASON_SAVE_FORMAT_VERSION = 4` save already carries the unpinned layout. | Medium | 1 | ✅ **Resolved August 8, 2026** — new **Appendix B.1**: the layout field by field (`magic → version → clubCount → {clubId, playerCount} → {playerId, recentBits, bitsAsOfWorldDay}`), the four MUSTs the siblings carry (magic-before-version; `ClubId` written, never order-implied; canonical ascending keys; trailing-byte guard + gates on encode AND decode), and the deliberate **no-`[GT]`-gating-on-decode** decision (`recentBits` is structurally valid at any value; gating on `AppearanceWindowDays` would turn a window retune into data loss). The cross-blob anchor-vs-clock rule is recorded as the save root's (see the v1.85 M4 entry). `season-competition-loop/appendices.md` v0.6. |
| ERR-030-029 | Season & Competition Loop #30, **balance-pass AR pass 12 (M4)**: the depleted-squad back-fill rule existed in NO spec while `PlayerCareerStates.SelectAvailable` had implemented it since #29/#41 T2 and `SquadRating.CanFieldStartingEleven` existed solely to serve it — press the least-injured back in one at a time (ascending remaining recovery, ties on earliest roster position), probing the engine's own selector each round, fail-loud when even the whole squad cannot field the formation. Meanwhile #30 §3.4 recorded the empty-squad floor as an OPEN shared #44/#36/#30 obligation, **#36 §2 F7 explicitly refused to invent a policy for it, and #36 §5 T-NT-I-005 asserted "whatever ERR-030-016 settles on"** — an APPROVED spec waiting on a decision the code had made unilaterally, its reasoning living only in a non-normative supplement belonging to the WRONG spec. ERR-030-028's class (a shipped behaviour specified nowhere, permanent by usage), on a behavioural rule rather than a byte layout — and its terminal refusal is the one fail-loud in the landing a player can actually reach. | Medium | 12 | ✅ **Resolved August 8, 2026, spec-only (the code needed no change)** — #30 §3.4 owns the rule (the back-fill, the tie-break, the selector-probe rationale — fieldability is asked of the selection rule that will run, never answered by a second parallel rule — and the never-worse-than-unfiltered limit); its terminal refusal is **F9** in §2.3; #36 §2 F7 and §5 T-NT-I-005 point at the settled rule (v0.3/v0.2, pointers only, contracts unchanged). `section-3.md` v1.8, `section-2.md` v1.1. |
| ERR-029-008 | Training System #29, **balance-pass AR pass 13 (M2)**: FR-TR-003 still specified `TrainingSchedule` as a **read-only view** and FR-TR-023 still specified the free **`SetFocus(club, playerId, focus)`** command — the exact two-array shape the **T0 AR's High deleted** (one club's ids silently paired with another club's states, same length, no guard, the wrong club's player written; the command moved onto the club-scoped `TrainingSchedule.TrySetFocus`, whose construction binds ids and states as a pair) — and the §2.2 sketch and §4.2 layout comment matched the retired design. Twelve passes, three months: a code fix whose whole point was structural safety, with the spec still publishing the unsafe shape an implementer would rebuild. The ERR-041-012 class (a normative surface the implementation deliberately refused), found by the pass-13 generator question. | Medium | 13 | ✅ **Resolved August 8, 2026, spec-only (the code needed no change)** — FR-TR-003 restates `TrainingSchedule` as the club-scoped handle that OWNS the FR-TR-023 write (keeping the true halves: no stored focus, not serialized); FR-TR-023 restates the command as `TrySetFocus(playerId, focus)` on the handle with the bind-the-pair-once rationale and records the deleted shape as the hazard; the §2.2 sketch and §4.2 layout comment match. `training-system/section-2.md` v0.8, `section-4.md` v0.6. |
| ERR-008-024 | Decision Tree #8 §3.1.5.2 — the 8-sector dribble scan ranked candidates on `spaceInSector` alone with a strict `>` improvement test. `spaceInSector` saturates at exactly 1.0 for any sector holding no opponent inside `DRIBBLE_THREAT_RADIUS`, and sector 0 is `AgentFacingDirection` by construction, so whenever two or more sectors were clear — the common case in the final third — the winner was always sector 0: the carrier dribbled wherever he already faced and goal direction had no influence on the scan at all. This is why ERR-008-018's `DirectionQuality_DRIBBLE` scoring term could suppress a retreating dribble but never redirect it toward a sector that was both clear AND goalward (`close-chance-creation-design.md` KD-CC3 / §7 item 6, the recorded residual it left open). | Moderate | 6 | ◑ **Recorded, NOT fixed — implemented, measured, refused, August 9, 2026 (the KD-CC7 pattern; `close-chance-creation-design.md` §4).** Ranking sectors on `spaceInSector × DirectionQuality_DRIBBLE(sectorDir, toGoal)` as a tie-break — the SAME term §3.2.4.1 already applies when scoring the resulting option — DOES fix the symptom: `sim_match_engine_close_chance` goes meanCosine −0.165 / goalwardShare 0.407 (both failing) to **PASS** (bounds −0.16 / 0.42, neither moved). But the same build **stalls play outright**: `sim_match_engine_play_develops` fails with "play stalled: last possession change at tick 18424, ball last moving at tick 18465 of 32400", and `sim_match_engine_shot_outcomes` fails `goals-still-scored` at **0**. A WIDER form ranking on `space × DirectionQuality` outright (not merely as a tie-break) produced the **identical** stall at the **identical tick**, plus mean-shot-distance 25.41 m against a 24.00 m ceiling — that identity is what localises the cause to the tie-break itself, not to how much space either form trades away. **Refused; not landed.** `OptionGenerator.cs` is back to the pre-fix baseline logic byte-for-byte (verified: `git diff 23f8dd9 -- src/decision-tree/OptionGenerator.cs` has zero non-comment lines). Kept, behaviour-neutral only: `DirectionQuality_DRIBBLE` hoisted to a public static `UtilityWeights.DribbleDirectionQuality(Vector2, Vector2)` with `UtilityScorer.ComputeDribbleDirectionQuality` delegating to it (so generation and scoring cannot drift apart if this is retried), plus a long explanatory note at the defect site recording the refusal for the next attempt. The two §3.1.5.2 unit locks the attempted fix added (goalward wins an all-clear tie; a blocked goalward sector still loses on space) are **REMOVED** — they locked behaviour that no longer exists. `DecisionTree.Tests` back to **129 passed / 4 skipped / 0 failed**. **Do not re-attempt this in isolation** — the measured blockers are recorded in `close-chance-creation-design.md` §10.2/§10.3: nobody can receive a ball above 0.5 m, and no composed slot reaches the penalty area; sending the ball goalward is only safe once those are addressed. `close-chance-creation-design.md` §7 item 6 **REOPENED**; `section-3-1.md` reverted to v1.8 to describe the shipped (unfixed) code. `OptionGenerator.cs`, `UtilityWeights.cs` v1.14, `UtilityScorer.cs` v1.16, `OptionGeneratorTests.cs`. |
| ERR-028-017 | Player Progression & Lifecycle #28, **AR pass 5 over the T1/T2a landing — a spec-vs-code sweep, not a code-behaviour finding: every item below is a spec correction against unchanged code.** Twelve findings, one owning spec, filed together because they are the same failure of the spec+code-same-commit doctrine compounding: **AR passes 3 and 4 (ERR-028-015, ERR-028-016) each landed production refusals — the F8 sentinel guard at `SeedFrom`, the `AdvanceDay_BackwardCall_DoesNotEvaluateRetirement` isolation, three decode range-check halves, five previously-unguarded `SeedFrom`/`FromBlocks`/`ValidateBatch` branches — with NO `docs/specs/` edit at all** (verifiable: `git show --stat 89ae54d` and `git show --stat 6a68f52` touch only `spec-error-log.md` and `src/`). That is the doctrine failing twice consecutively, and it is why F8 itself understated the contract by four sites before this pass: the guard at `SeedFrom` (ERR-028-015) had already existed in code for one AR pass with no spec row naming it. **(a) F3/F5 exception-type self-contradiction** — §2.3's F3 row said `ArgumentException` while citing the `MatchSaveCodec` posture, which throws `InvalidOperationException`; the third instance of this exact class, after ERR-029-004 (#29 §2.3 F3) and ERR-041-008 (#41 §2.3 F3) on the sibling rows. F5 gains the same type, undocumented until now. **(b) F8 understated by four sites** — named only `AdvanceDay`; the code refuses the sentinel at `AdvanceDay`, `SeedFrom`, `FromBlocks`, and `ProgressionSaveCodec.Encode`/`Decode` (two exception types across the five). **(c) The FR-PG-021 batch parameter had no declared type or validation contract anywhere** — §2.2 named only `TrainingInput`, §4.5 called the seam "a `TrainingInput` method parameter", §3.1's pseudocode wrote `AdvanceDay(worldDay, in trainingInputs)` without saying what that was; the code takes `TrainingInputBatch` (`ClubTrainingInputs[]`) and `ValidateBatch` fail-louds on four normative rules stated nowhere (club-count coverage, positional club agreement, per-club player-count exactness, per-player id agreement). **(d) §3.5's byte layout left `str` unencoded** (now pinned: u32 length + ASCII, #16 §3.2.4.1) **and its fail-loud enumeration named only framing gates**, omitting the four VALUE gates `Decode` applies (attribute range, weak-foot range, non-negative age, `PotentialAbility` within `[PA_MIN, ABILITY_MAX]`) — the last of which makes a save-acceptance predicate keyed on `[GT]` config, the exact posture #30 Appendix B.1 argues against for its own block; recorded as an OPEN decision, not resolved. **(e) §3.4's retirement-evaluation placement was undocumented** — it runs once per `AdvanceDay` CALL (in `ProgressionEngine.AdvancePlayerTo`, wrapping the whole gap-replay loop) against the post-replay derived age, never once per lived day, so `RetirementDay` is stamped with the call's target day rather than the day within a multi-day gap the threshold was actually crossed; recorded as a known limitation, cross-referenced to §5's T-PG-DET-002. **(f) §4.2's file layout was stale against `src/`** — missing `ClubCareerStates.cs`/`TrainingInputBatch.cs` (both public, both load-bearing), listing `RetirementResult.cs`/`RegenResult.cs`, which do not exist (only sketched inline in §2.2; `RunSeasonBoundary` is deferred). **(g) §4.2 also carried a stale "2 → 3" `SEASON_SAVE_FORMAT_VERSION` copy** — actual is 4 → 5 as of #28 T1 — restated as a citation to #30 Appendix A rather than a fourth restated copy (the AR pass 13 "a third copy is not re-synchronised" lesson, on the second copy this time). **(h) §4.5's "sole seam" claim understated `ProgressionEngine`'s real public surface** — it named `AdvanceDay`/`RunSeasonBoundary` (not yet built)/`Snapshot`/`Restore`/`LifecycleViewModel`, omitting `SeedFrom`, `SquadFor`, `ToBlocks`/`FromBlocks`, `Empty`, `ClubCount`, `NextPlayerId`, `CarriesClub` — verified against `ProgressionEngine.cs` and re-enumerated split by role (#30 contract / codec-internal / observation / construction convenience). **(i) §9.1's preamble and §9.5's blockquote both still read "forward design (nothing built)"** one revision after §9.2 was itself corrected (August 9) to record T0/T1/T2a as landed — the `tools/recurring-defect-lint.py` hygiene class, missed by its August 8 sweep. **(j) Appendix A's "copied verbatim from code" claim was false for two rows** — `DOMAIN_TAG_PLAYER_PROGRESSION`/`SUBSYSTEM_ORDINAL_PLAYER_PROGRESSION` are named only in doc-comment prose in `PlayerProgressionConstants.cs`, never declared as constants (they land with the regen stream). **(k) Appendix B's worked example was scoped ambiguously** — it describes `GrowthProjection.AdvanceDayForPlayer` called directly; the public `SeedFrom`+`AdvanceDay` entry point spends its first point one day later (world-day 365, not 364) since `SeedFrom` anchors the cursor at the seed day (ERR-028-014). **(l) §3.1.1's age formula was stated unconditionally** — `GrowthProjection.AdvanceDayForPlayer` guards `age = 0` when `ageDays ≤ 0` rather than dividing; unstated. | High | 12 | ✅ **Resolved August 10, 2026, docs-only — no code, no `src/` file touched, verified by `git status --short` after the pass.** (a)/(b) §2.3's F3/F5/F8 rows corrected and extended. (c) §2.2 gains `ClubTrainingInputs`/`TrainingInputBatch`; §2.3 gains new **F9** for the four `ValidateBatch` rules; §4.5's seam sentence restated in batch terms. (d) §3.5 gains the `str` encoding pin and the four value gates, both in the layout note and in the `Restore` ordering paragraph; the `[GT]`-keyed-acceptance-predicate tension is recorded as OPEN, no tag changed. (e) §3.4 gains the once-per-call placement statement, the `RetirementDay`-is-the-call-day limitation, and the T-PG-DET-002 cross-reference. (f)/(g)/(h) §4.2's file layout, frame-version citation and §4.5's public-surface enumeration all corrected against `src/player-progression/*.cs`. (i) §9.1 and §9.5 qualified/date-stamped to the July 23, 2026 approval they describe, pointing at §9.2 for current status. (j)/(k)/(l) `appendices.md` and §3.1.1 corrected as described. `section-2.md` v0.6, `section-3.md` v0.6, `section-4.md` v0.3, `appendices.md` v0.4, `section-9-approval-checklist.md` v0.4.  **⚠️ CORRECTED August 10, 2026 (ERR-028-018):** finding (k) above — describing the public entry-point discrepancy as merely "day-column labels differ by one" and asserting "the per-player projection itself is unchanged and this table's arithmetic is correct" — was FALSIFIED BY EXECUTION. Shifting the accrual window one day right of a fixed band edge is not a label difference: it cost one whole attribute point per full band traversal plus a permanent residue that ate the first year of the next accruing band, contradicting Appendix A / KD-8's own `+1/yr` promise. See ERR-028-018 for the measured numbers, the fix, and the corrected Appendix B. |
| ERR-030-032 | Season & Competition Loop #30, **the same AR pass 5 sweep, split to this spec because the corrections land in `season-competition-loop/`'s own files** — three staleness findings, all found against `src/season-save/` with no code change. **(a) FR-SN-021 (High)** — still showed the seven-argument `Save(world, season, matchOrNull, path, trainingClubs, medicalClubs, appearanceClubs)` after #28 T1 added a required, null-rejecting eighth (`progression`), even though §4.4 was rewritten at AR pass 13 to point at this row as the SOLE owner of the signature — so the sole owner was itself wrong; `Load`'s return description also omitted `Progression`, which `SeasonSaveContents` never returns as null. **(b) §2.2 declared no #28-owned type at all** — the identical gap AR pass 12 filed as ERR-030-028 for the appearance types one landing earlier, recurring verbatim on the very next subsystem to wire. **(c) §4 was stale in three more places** — §4.2's `SEASON_SAVE_FORMAT_VERSION` delta line still read "1 → 4" after #28 T1's 4 → 5 bump (AR pass 14 had only just corrected the prior "1 → 2"/"1 → 2" duplication, and #28 T1 landed the same week), §4.2's file layout omitted `ProgressionBlock.cs`/`ProgressionSquads.cs`, and §4.3's `SeasonLoop` holdings list never mentioned `_progression`, the `Progression` property, or its three constructor refusals. | High | 3 | ✅ **Resolved August 10, 2026, docs-only — no code, no `src/` file touched.** (a) FR-SN-021 refreshed to the eight-argument form with `progression` marked REQUIRED/null-rejecting, `Load`'s description gains `Progression`. (b) §2.2 gains `ProgressionEngine` (the roster authority, KD-4, not an overlay), `ProgressionSquads` (the `ISquadProvider` projection — lives in `season-save` because #28 §4.1 forbids #28 itself from referencing that `match-engine` type) and `ProgressionBlock` (the typed frame block, ERR-028-004's compile-time half), mirroring the appearance-type entries. (c) §4.2's version line corrected to 1 → 5, the two missing files added; §4.3 gains the `_progression` holding, the `Progression` property, and its three constructor refusals (provider mutual exclusion, season-club coverage, no-bare-provider). `section-2.md` v1.4, `section-4.md` v0.6. |
| ERR-028-018 | Player Progression & Lifecycle #28 §3.1 (KD-1) / §3.1.1, Appendix A / Appendix B (KD-8), found by **adversarial review pass 5 (time/arithmetic axis, High)** and filed here retroactively — the fix landed at commit `789ea74` without this close-out, itself an instance of the FR-CS-057 rowless-landing class this project keeps re-hitting. **The defect.** A band exit is decided by the player's DERIVED age (§3.1.1), not by `GrowthCursor`. `SeedLifecycle` anchored `LastAdvancedWorldDay` at the seed day (ERR-028-014 — "already accounted for, not a day still to be lived") but left `GrowthCursor = 0`, crediting that day's own band step to NOTHING. That shifted the accrual window one day right of every fixed band edge: a full traversal of an *N*-year band accrued `N · DAYS_PER_YEAR − 1` days instead of `N · DAYS_PER_YEAR`, and because `POINT_COST == DAYS_PER_YEAR` exactly (KD-8) that is one whole `[1,20]` attribute point short, every single traversal — contradicting Appendix A's and KD-8's own `+1/yr` promise. Measured through the public `SeedFrom` + `AdvanceDay` entry point, before the fix: seedAge 16 (8 years of Growth) gained 7 points with a 364-day residue cursor; seedAge 18 (6 years) gained 5; seedAge 20 (4 years) gained 3; seedAge 23 (1 year) gained ZERO. The 364-day residue survives the (accrual-free) Stable band, which can never spend it, and then eats the first year of Decline — a player's first decline point landed on day 728, not day 365 — and it made two otherwise identical players diverge purely by the age they were seeded at (seeded 20 vs 28, both run to 36, different peaks AND different losses). **A claim landed this same session, corrected.** ERR-028-017 finding (k) examined this exact seam and concluded "only the day-column labels differ by one from the public-API sequence" and "the per-player projection itself is unchanged and this table's arithmetic is correct." Execution falsifies both: the one-day shift against a fixed band edge is not a label difference, it is one point per traversal plus a permanent residue — ERR-028-017's own row above now carries a pointer to this correction, per this project's convention of annotating a falsified claim rather than quietly restating it. **Why five prior AR passes missed it.** Every existing growth lock measured a hand-placed 365-day window in MID-band, never a band TRAVERSAL against the fixed edge — `GrowthBand_SpendsExactlyOnePointPerYear` steps a 365-day window that INCLUDES the anchor day the engine never replays, and `DeclineBand_DrainsExactlyOnePointPerYear` starts its 32-year-old at `GrowthCursor = 0`, a state no career that passed through Growth can actually be in. | High | 1 | ✅ **Resolved (code) August 10, 2026, commit `789ea74`; this tracking close-out filed the same day.** `SeedLifecycle` now credits the seed day's own band step to `GrowthCursor` at construction — `GROWTH_DAILY_POINTS` for a Growth-band seed, `DECLINE_DAILY_POINTS` for Decline, `0` for Stable — the single `AdvanceDayForPlayer` accrual step that day would have produced, without also running its spend/drain step or re-writing `LastAdvancedWorldDay` (both already handled by the ERR-028-014 anchor). Deliberately NOT fixed by anchoring the cursor at `newGameWorldDay - 1`: at day 0 that underflows to `uint.MaxValue`, the sentinel `FromBlocks` refuses. Five existing locks rebaselined by exactly +1 day of accrual (`AdvanceDay_FirstCall_ReplaysFromTheSeedDay` 300→301×SquadSize; `AdvanceDay_BackwardCall_DoesNotRegressTheCursor` 10→11×SquadSize; `AdvanceDay_OneDayBelowTheSentinel_StillAdvances` 1→2×SquadSize; `AdvanceDay_AtWorldDayZero_EachAgeBand...` +1/0/−1→+2/0/−2; the SeasonLoop `AdvanceDays_DrivesSlot1_...` lock ±5→±6). One new lock, `AdvanceDay_AWholeGrowthBandTraversal_GainsExactlyOnePointPerYear_AndLeavesNoResidue`, asserts the property none of the existing ones did — points gained equals years in band AND the residue at the band edge is zero, so a future regression that reintroduced a residue while rounding the count back up would still be caught. **Mutation-verified:** reverting the seed credit fails 6 of 109 `PlayerProgression.Tests` (the five rebaselined locks plus the new traversal lock) and nothing else. `ProgressionEngine.cs` v1.4, `ProgressionEngineTests.cs` v1.6, `SeasonLoopProgressionTests.cs` v1.6, `section-3.md` v0.7 (the accrual-window rule stated as a MUST), `appendices.md` v0.5 (Appendix B's worked example corrected to describe what the public entry point now actually produces — it now matches the direct-call table exactly, world-day for world-day, with no residual one-day offset). **VERIFIED AT HEAD (commit `789ea74`):** build 0 errors; `PlayerProgression.Tests` 109/0/0; `InjuriesMedical.Tests` 70/0/0; `TrainingSystem.Tests` 52/0/0; `SeasonSave.Tests` 386/0/3; `recurring-defect-lint` 0 ERROR / 125 WARN / 27 INFO. **NOT a whole-tree gate** — `MatchEngine.Tests` was not run on this tree, and CI is red on `sim_match_engine_close_chance`, which fails identically on `main` and this branch cannot have caused. **Recorded, not fixed** (carried forward from `789ea74`'s time/arithmetic axis and `e68e2ad`'s invariant-duplication axis, so the next pass over this seam finds them in one place): **M1** `BirthWorldDay` is the ONLY lifecycle field with no range gate, and the daily step narrows it into `int` unchecked — a career can load, advance and project fine and become permanently unsavable (probe-verified: derived age `int.MinValue`, and `ClassifyAgeBand(int.MinValue)` returns Growth, so the player grows forever and retirement can never fire — the same class as ERR-028-006 and ERR-028-018 itself, through the one field with no gate). **M2** at the PA ceiling the cursor banks without bound (2,189 unspendable points measured) and silently cancels ~6 years of Decline; unreachable via procedural seeding today, reachable via `FromBlocks` / #47 authored PA. **M3** `RegenGenerator` still seeds `PROGRESSION_NOT_ADVANCED_SENTINEL`, which every store and codec boundary now refuses by name — landed code guaranteed to fail on first use; inert, no production caller. **M4** `DAYS_PER_YEAR = 365` has no relationship to the 315-day season period, so a season advances every player's age by 0.863 years and `RETIREMENT_AGE` arrives ~2.7 seasons late; owner decision — the defect is that the two constants are neither derived from nor checked against each other. **L1** `ageDays > 0` where `>= 0` is meant. **From the invariant axis (`e68e2ad`):** **M5** FR-PG-011's id-cursor rule is enforced only in `FromBlocks`; `Encode`/`Decode` admit a cursor at or below a carried id, so `Encode` can write a blob `Restore` refuses forever (probe-verified). **L2** the cursor-vs-clock walk is hand-copied at the composition and file boundaries — the predicate is shared and both walks are locked, so drift is caught by deletion but not by a divergent edit. **L3** global-id-uniqueness and the never-advanced sentinel each have two independent implementations inside `player-progression` with no equivalence test. **L4** progression-vs-career player-set coherence is enforced at `Save` and `Load` but not at composition. |
| ERR-028-019 | Player Progression & Lifecycle #28 §2.2/§2.3/§3.1/§3.1.1/§3.3/§3.5, Appendix A, and Season & Competition Loop #30 §2.3/Appendix B.1 — **docs-only close-out for AR passes 5-8, four consecutive production landings (`39c385a`, `cf5abf0`, `8556ddd`, `b798ce2`) that shipped with ZERO `docs/specs/` edits between them — the exact ERR-028-017 class recurring twice more, this time across four commits rather than two.** Verified by reading each commit's diff directly (not the orientation summary that named them): `git show --stat` on all four touches only `src/player-progression/`, `src/season-save/` and their `tests/` folders, plus `docs/tracking/file-manifest.md` on the last one — never `docs/specs/`. Contract changes found: **(1)** `MAX_DERIVABLE_AGE_YEARS` — a new `[FIXED]` representability-bound constant, undocumented in Appendix A, whose own value history (first set to a football-plausibility 1000, corrected same-session to 100,000,000 after it broke the `i64` `BirthWorldDay` field-width lock) was nowhere recorded. **(2)** §3.1's age-derivation guard changed from "`ageDays ≤ 0 → age 0`" (ERR-028-017's own correct-at-the-time statement) to "`ageDays == 0` ordinary, `ageDays < 0` FAILS LOUD" (M2(a), AR pass 6) — the old guard's else-branch had been silently deriving age 0 from a future-dated, corrupt `BirthWorldDay`, permanently and undetectably. **(3)** Both spend/drain loop refusal branches changed TWICE — AR pass 5's `GrowthCursor = POINT_COST - 1` clamp (itself undocumented) was superseded by AR pass 6's `GrowthCursor = 0` clamp after execution falsified the "pending fraction" rationale; `AbilityModel.DrainOnePoint` changed `void` → `bool` so the drain loop gained a failure exit it never had (AR pass 6 High — an out-of-band cursor previously ground the loop for ~70 days of CPU with no diagnostic). **(4)** The FR-PG-011 id-cursor rule and the M3 club-size rule — each enforced at three-to-four boundaries in code — had **no normative text in this spec at all**, at any point, before this pass. **(5)** The Encode/FromBlocks-vs-Decode exception-type split (`ArgumentException` vs `InvalidOperationException`, AR pass 8 M-1) was undocumented; before that fix, `Decode` threw `ArgumentException` naming an argument its own signature does not have, for four of the section's shared boundary rules — F8's own claim that "`Encode` and `Decode`, both via the shared `RequireNoNeverAdvancedSentinel` → `ArgumentException` at each" was consequently stale the moment AR pass 8 landed. **(6)** Two new gates on `CurrentAbility`/`ComputeCA` equality and `RetirementDay`/`RetirementFlag` pairing (AR pass 8 L-4), the first of which carries an OPEN hazard (below). **(7)** #30's `PlayerCareerStates.RequireBirthWorldDayWithinClock` (AR pass 6 M2(b)) — a #28 `BirthWorldDay` anchor ahead of the world clock, refused at `SeasonLoop` composition and `SeasonSaveManager` Save/Load — had no normative text in `docs/specs/season-competition-loop/` at all, despite being live in `src/season-save/` since `cf5abf0`. | High | 4 commits, ~10 items | ✅ **Resolved (docs) August 11, 2026, docs-only — no `src/` file touched, verified by `git status --short` after the pass.** #28 `section-2.md` → **v0.7** (F8's stale exception-type claim for `Decode`'s sentinel gate corrected in place, superseding rather than restating per this project's convention; new **F10** FR-PG-011 id-cursor row, new **F11** M3 club-size row; `PlayerLifecycle` field comments extended for `GrowthCursor`/`BirthWorldDay`/`CurrentAbility`/`RetirementDay`). `section-3.md` → **v0.8** (§3.1's pseudocode rewritten with the fail-loud/saturating/clamp-to-zero history stated explicitly rather than silently overwritten; §3.1.1's guard corrected; §3.3 gains the AR-pass-7 regen construction-day credit; §3.5's four-gate enumeration superseded by eight, the exception-type split stated as a general rule, the id-cursor/club-size rules stated in full for the first time, a cross-reference to #30's anchor-vs-clock check). `appendices.md` → **v0.6** (`MAX_DERIVABLE_AGE_YEARS` row, `[FIXED]`, with its own correction history recorded verbatim per this pass's no-fabrication constraint). #30 `section-2.md` → **v1.5** (new **F10**, the anchor-vs-clock rule — a DIFFERENT invariant from F8's cursor-vs-clock rule, ahead-only), `appendices.md` → **v1.1** (Appendix B.1 gains the sibling paragraph). **OPEN decision recorded, NOT resolved (MEDIUM-1, AR pass 9's finding, folded in rather than re-derived):** `DescribeOutOfRangeValues`'s new `CurrentAbility == ComputeCA(attributes, position)` gate is keyed on `PlayerDatabaseConstants.PositionAttributeBias`, tagged `[GT]` with a standing `TODO: replace with config loader (Stage 1)` — tuning one cell of that table would make every previously-written save refuse to load, permanently, under #30 Appendix B.1's F3 no-migration rule. Not triggerable today (the table is presently a compile-time constant, so stored always equals recomputed at write time); bites at the first tune. No tag changed, no code changed; the alternative (recompute at `Decode` instead of refusing) is recorded as an owner call. **Two unrelated hygiene items folded in from this same pass's citation sweep:** `CHANGELOG-src.md` v2.113's "only … one stale internal pointer" claim about the second merge-renumbering was itself false for two more rows (2.111, 2.112 also had an embedded `spec-error-log.md` version citation edited) — annotated in place; this file's own duplicate `## ERR-008-021` heading (two independent write-ups of the same id from two concurrent branches, each individually correct in its own branch and jointly false once merged) reconciled — the surviving-at-merge entry marked authoritative, the superseded one annotated rather than deleted, cross-referencing the `ERR-008-021` summary-table row's own reconciliation note. **Cross-reference:** this is the fourth-through-seventh instance of the class ERR-028-017 named first (spec+code-same-commit doctrine failing); see that entry and ERR-028-018 above. |
| ERR-030-033 | Season & Competition Loop #30 §3.4.1 / league-bootstrap **KD-8 (acceptance)**, found by **executing A4a's corpus run** (August 12, 2026): KD-8's per-bucket acceptance bar — *"mean home and away goals within ±0.25 of the corpus mean"* — is **below the sampling error of the corpus KD-8 itself specifies**, so it cannot be satisfied by any model, including a correct one. KD-8 sizes the grid at ~18 matches per bucket; a bucket's mean therefore carries a standard error of `sqrt(var/n)`, and the measured per-bucket variances (0.33–7.21, rising with the mean because scorelines are counts) put that error at **0.135–0.633**. **15 of the 22 bucket-sides have a standard error larger than the whole ±0.25 bar.** The bar is stated as an agreement requirement between model and engine, but at this depth it is dominated by how precisely the engine's own mean is known — a perfectly specified model scored against a re-run of the same corpus would fail it too. The two halves were set independently (n from a compute budget, the tolerance from a plausibility judgement) and never checked against each other, which is why the defect survived AR-5 through AR-7 on this note: every review read the bar as a statement about the model. Reaching ±0.25 as a *resolvable* bar needs n ≈ 770/bucket (~8,500 matches, ~210 h serial at the measured 90 s/match) — three orders of magnitude past the budgeted run, so this is a bar to re-specify, not a run to re-size. **Distinct from ERR-030-034**, which is why the fit misses *at all*; this entry is why the miss cannot be measured. | High | 2 (`league-bootstrap-design.md` KD-8, `round-resolution-corpus.md`) | ✅ **RESOLVED August 12, 2026 — the bar is re-specified (owner-approved), and the same fit now reads mean-agreement PASS.** Deliberately NOT closed by widening ±0.25 to whatever this run achieved: a bar moved to fit its own result stops being a bar, and there is a standing owner ruling against exactly that move (`close-chance-creation-design.md` §10.9 item 6). Instead the bar is stated against the precision the corpus actually has, **a priori and for any corpus** — the standard construction of a test with a controlled false-alarm rate. KD-8 now carries **A1** per-cell `|Δ| ≤ max(0.25, 2·se)` (**±0.25 retained as a FLOOR**, so a deeper corpus automatically restores the original requirement rather than abandoning it), **A2** at most `1 + round(0.0455·cells)` exceedances with none over `max(0.40, 3·se)` (a 2σ screen over N cells expects ~4.55% to exceed by chance, so a zero-exceedance rule would fail a correct model on a large grid), **A3** a pooled `χ² ≤ χ²₀.₉₅(cells − 3)` — **where the statistical power actually lives**, since A1/A2 are per-cell screens blind to systematic misfit every individual cell passes — **A4** a scoreability floor of 18/bucket, without which the se-relative form is gameable by shrinking n, and **A5** the unchanged ±5 pp W/D/L bar plus a pinned n ≥ 250 for a resolvable *pass* and a requirement that a *failure* also exceed 2·se or be reported INCONCLUSIVE. **Measured against it, the August 2026 fit passes the mean half**: worst |z| = 2.06, one exceedance of an allowed two, no hard exceedance, pooled χ² = **16.0 on 19 dof** against a 30.1 threshold. The verdict is now reported in two parts — **mean agreement PASS, distribution shape FAIL** — because the two halves fail for unrelated reasons and a single flat verdict hid which, which had the practical effect of making ERR-030-034 look like a fit failure. All of it is computed and emitted by `tools/round-resolution-fit.py` (χ² critical values by Wilson–Hilferty, no third-party dependency, verified against exact values at dof 10 and 19), so no figure here is hand-copied. `league-bootstrap-design.md` KD-8; `RoundResolutionFitLockTests` v1.1 corrects its now-stale FAIL comments — its own tolerance is a regression guard, not this bar, since the suite has no standard errors. |
| ERR-030-034 | Season & Competition Loop #30 §3.4.1 (FR-SN-013a) / league-bootstrap **KD-7 (model shape)**, found by **executing A4a's corpus run** (August 12, 2026): KD-7 resolves a fixture as two independent **Poisson** draws, and a Poisson variable's variance equals its mean *by definition* — the shape has no spare parameter for spread. The engine's scorelines are **over-dispersed**: across the 198-match corpus the dispersion index `var/mean` averages **1.395** over 22 bucket-sides, **19 of 22 are above 1**, and the pooled test is `chi2 = 521.7` on 374 dof — **z = +5.40**. So the engine produces more blowouts and more shut-outs than any Poisson with the same means can, and correspondingly **fewer draws**: at the `dSquad ≈ 0` acceptance bucket the corpus draws at **19.2%** against the fitted model's **26.8%** — a **7.6 pp** gap, which is the whole of the W/D/L bar's miss. That figure is measured at **n = 198**: the acceptance bucket was deliberately deepened past the grid's 18 (four parallel sample windows, 180 extra matches) precisely because ERR-030-033 makes the bar unresolvable at 18, and the deepening moved the draw share from 11.1% to 19.2% — so the grid-depth reading would have overstated this defect by more than a factor of two while still, correctly, detecting it. The gap is 2.7σ against the deepened corpus's own 2.8 pp standard error. **No choice of the three fitted parameters closes this** — `BaseGoals`, `GoalRatingSlope` and `HomeAdvantageRating` all move the two *means*, and the discrepancy is in the second moment. It is a statement about the model's FAMILY, not its coefficients, which is why it is filed against KD-7's shape rather than against the fit. Consequence if left: a quick-sim league table will show systematically **more draws and fewer decisive results** than the same fixtures played through the engine — the precise "league tables feel wrong" failure roadmap risk row 1 names, surviving a fit that did its job. | High | 2 (`league-bootstrap-design.md` KD-7, `round-resolution-corpus.md`) | ◑ **Recorded, NOT fixed — but the successor is now PRE-DECIDED and gated, August 12, 2026: `league-bootstrap-design.md` **KD-7a**.** The section pins what the successor would be with KD-7's own specificity (S1 NB2 marginal as `var = μ(1+αμ)`, **not** a constant ratio, because dispersion rises with the mean; S2 `NegativeBinomialInverseCdf` by inversion, pinned by name, **one uniform per side with the existing sub-streams unchanged**, so the keyed order-independent fixed-budget contract survives exactly; S3 a new `[GT] QuickSimDispersion` whose **zero case routes to `PoissonInverseCdf` verbatim** — an explicit branch, not a limit, so `α = 0` is bit-identical to today rather than identical-in-the-limit), and states four things that stop it being adopted prematurely. **S4 — α is NOT determined by this corpus**: 0.0773 weighted vs 0.1552 unweighted, a factor of 2.01, with **one 18-sample cell carrying 36% of the weighted fit** (a variance estimate at n=18 has ~33% relative error and the weights go as `1/var²`), so adopting on it would be fitting noise; the fitter now emits both estimators, the max single-cell leverage and a determined/not-determined verdict every run. **S5 — NB2 does not fix the draw deficit and must not be adopted expecting it to**: measured 26.5% draws vs Poisson's 26.8% and the engine's 19.2%, i.e. ~0.3 pp of a 7.6 pp gap. **S6 — the draw deficit gets no pinned successor**, because its mechanism is unestablished: the shared-swing family that would cut draws implies negative home/away correlation and the corpus refutes that (**+0.044 ± 0.073**, n=198, ~3σ from the ≈ −0.20 it predicts); a Dixon–Coles `ρ` remains the candidate but needs the joint scoreline histogram at depth, which is why the raw rows are now committed under `docs/tracking/corpus-data/`. **S7 — the adoption tripwire**: dispersion still z > 3, α determined, the draw gap still beyond 2·se under KD-8's A5, and the capture taken **post-defensive-wiring** — the corpus is produced by an engine in which no player has ever made a tackle (wiring backlog W2), and the second moment of scorelines is exactly what that wiring moves. **S8 corrects this row's own cost claim** (see the ⚠️ above): there is no save-format bump. **Both remaining findings inside this ERR are now separately stated** — marginal over-dispersion (real, family-inexpressible, successor pinned) and the draw deficit (real, mechanism unestablished, no successor) — which the original single causal sentence had conflated. |

---

## ERR-001: `IBallPhysicsCallback` fragments a single operation into four methods

**Severity:** Major
**Detected:** February 19, 2026
**Root Cause:** Interface written by producer (First Touch) to describe what it provides
to Ball Physics, rather than by the consumer (Ball Physics) to describe what it needs.
The four methods encode First Touch's internal `TouchResult` taxonomy into Ball Physics,
creating coupling between two systems that should be independent.

**Problem in detail:**
`IBallPhysicsCallback` defines four methods:
- `OnControlled(agentID, position, velocity)`
- `OnLooseBall(position, velocity)`
- `OnDeflected(position, deflectionVelocity)`
- `OnIntercepted(interceptingAgentID, position, velocity)`

All four do the same physical thing: set ball position and velocity. The method name
encodes why First Touch is calling — which is First Touch's concern, not Ball Physics'.
Ball Physics does not and should not change its behaviour based on which `TouchResult`
produced the call. Teaching Ball Physics about `TouchResult` states via method names
is inverted responsibility.

**Correct approach:**
Single method: `SetBallState(Vector3 position, Vector3 velocity)`
First Touch calls it once with the computed position and velocity regardless of outcome.
Ball Physics applies the state. The `TouchResult` outcome is First Touch's internal
classification and stays there.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.2 | Remove `IBallPhysicsCallback` interface definition; replace 4-method calls with single `SetBallState(position, velocity)` call in `ApplyTouchResult()`; update §4.5 interface table entry; update flow diagram ASCII art at §4.4 |
| `First_Touch_Spec_Outline_v1_0.md` | Interface contracts table | Remove `IBallControlCallback` row; replace with `SetBallState()` direct call note |

**Version impact:** `First_Touch_Spec_Section_4_v1_0.md` → v1.1

---

## ERR-002: `StringIDs` papers over an undesigned event bus with the wrong solution

**Severity:** Moderate
**Detected:** February 19, 2026
**Root Cause:** Premature optimisation for a system (Event Bus) that has not yet been
designed. The `StringIDs` pattern assumes the Event Bus will dispatch on string keys and
pre-hashes them to avoid runtime allocation. This assumption may be wrong.

**Problem in detail:**
`Master_Vol_4_Tech_Implementation.md` specifies a `StringIDs` static class that
pre-hashes string constants (player names, tactic names) to `int32` at startup:

```csharp
public static class StringIDs {
    public static readonly int TACTIC_GEGENPRESS = Hash("Gegenpressing");
}
```

This pattern only makes sense if the Event Bus dispatches on string keys. If the Event
Bus uses typed event structs (the standard C# pattern: `EventBus.Publish<TEvent>(evt)`),
dispatch is on the type identity — zero strings, zero hashing, zero `StringIDs` class
needed. The `StringIDs` solution solves the wrong problem.

**Correct approach:**
Remove `StringIDs`. Document that the Event Bus will use typed event structs. String
hashing is a last resort for systems that cannot use typed dispatch (e.g., scripting
bridges, serialised network events). Those cases, if they arise, are addressed when
the Event System (Spec #17) is designed.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Master_Vol_4_Tech_Implementation.md` | `StringIDs` section | Remove class definition and example; replace with note: "Event Bus dispatches on typed structs. String-keyed dispatch is not used. String hashing deferred pending Event System Spec #17 design." |

**Version impact:** `Master_Vol_4_Tech_Implementation.md` → minor revision

---

## ERR-003: `PerformanceContext` violation mandate imposes governance with no Stage 0 benefit

**Severity:** Moderate
**Detected:** February 19, 2026
**Root Cause:** Legitimate Stage 4 architecture (`PerformanceContext` modifier chain)
given an enforcement rule that designates direct attribute access as a "specification
violation" — in a stage where the gateway is a passthrough multiplying by 1.0.

**Problem in detail:**
`Agent_Movement_Spec_Section_3_2_v1_0.md` §3.2.1 contains:

> "Any specification that evaluates a player attribute for gameplay purposes MUST call
> `EvaluateAttribute()` or `EvaluateAttributePair()`. Direct access to raw attribute
> values for gameplay calculations is a **specification violation**."

`PerformanceContext` and `EvaluateAttribute()` are correct long-term architecture — in
Stage 4, a rated-18 player performing like a 13 during a bad season is a genuinely
valuable simulation feature. The gateway earns its existence.

The problem is the **violation designation**. Calling `EvaluateAttribute(18)` in Stage 0
returns exactly `18.0f`. The mandate forces every spec (all 20) to import, instantiate,
and route through `PerformanceContext` for a multiply-by-one operation, on pain of
being in violation. This governance overhead is disproportionate to Stage 0 benefit.

**Correct approach:**
Keep `PerformanceContext` and `EvaluateAttribute()` — they are good architecture.
Reword the enforcement rule as a recommendation:

> "Specifications evaluating player attributes for gameplay calculations should route
> through `EvaluateAttribute()`. This enables Stage 4 form, psychology, and career
> modifiers to activate without refactoring downstream formulas."

No violation designation. Compliance by convention, not mandate.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Agent_Movement_Spec_Section_3_2_v1_0.md` | §3.2.1 | Remove bolded violation rule; reword as recommendation |
| `Agent_Movement_Spec_Section_3_5_v1_2.md` | PerformanceContext usage note (`CRITICAL` block) | Remove `CRITICAL` designation; reword as convention note |
| `Agent_Movement_Spec_Section_3_6_v1_1.md` | Any violation reference | Remove violation language |
| `Agent_Movement_Spec_Section_3_7_v1_2.md` | Test descriptions referencing violation | Remove violation language from test pass criteria |
| `Agent_Movement_Spec_Section_4_v1_1.md` | Any violation reference | Remove violation language |
| `Agent_Movement_Spec_Section_6_v1_1.md` | Future extensions referencing enforcement | Remove violation language |
| `Agent_Movement_Spec_Section_9_Approval_Checklist.md` | Any checklist item verifying enforcement compliance | Reword as convention check, not violation check |
| `Agent_Movement_Spec_Appendices_v1_1.md` | Any enforcement reference | Remove violation language |
| `Agent_Movement_Spec_Remaining_Sections_Outline.md` | Any enforcement reference | Remove violation language |
| `First_Touch_Spec_Outline_v1_0.md` | Any PerformanceContext violation reference | Remove violation language |

**Note:** `PerformanceContext` struct definition, `EvaluateAttribute()` method, factory
methods, and all formula usage remain unchanged. Only the enforcement designation is
removed.

**Version impact:** 10 files → minor revision each (single sentence change per file)

---

## ERR-004: `IPossessionManager` and `IFirstTouchEventQueue` interface against unspecified systems

**Severity:** Major
**Detected:** February 19, 2026
**Root Cause:** Interfaces written before the systems they interface with have been
specified. Interfaces written speculatively against undesigned consumers will be
redesigned when the real consumer is specified, making the Stage 0 interface vestigial
or a constraint on the future design.

**Problem in detail:**

**`IPossessionManager`** (First Touch §4.5.4):
The spec notes: *"Implementer: PossessionManager (Spec TBD, Stage 0 stub sufficient)"*
The Stage 0 stub is one line of work. An interface written against "Spec TBD" will
either be replaced when the Possession Manager is specified, or will constrain that
spec's design to fit an interface written without knowing what the system needs to do.

**`IFirstTouchEventQueue`** (First Touch §4.5.5):
A ring buffer interface with capacity 64, connected to Event System (Spec #17, Stage 1).
The Event System has not been designed. The ring buffer capacity (64) and the
`Enqueue(FirstTouchEvent)` method shape are speculative. When Stage 1 Event System is
designed, it will define its own buffering and dispatch requirements — at which point
this interface is either replaced or becomes a constraint.

**Correct approach:**
Remove both interfaces. Replace with direct, minimal Stage 0 implementations:

- Possession: `ball.PossessingAgentId = agentId` (pending BallState amendment ERR-008)
- Event queue: comment stub — *"Event publishing deferred to Stage 1. When Event System
  (Spec #17) is designed, First Touch will implement its consumer interface here."*

Write the interfaces when both sides (First Touch and their consumers) are fully
specified. Do not write an interface when one side is "Spec TBD."

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.4 | Remove `IPossessionManager` interface; replace possession assignment logic with direct `BallState` field write; update §4.5 interface table; update flow diagram |
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.5 | Remove `IFirstTouchEventQueue` interface and ring buffer specification; replace with deferred comment stub; update §4.5 interface table |
| `Agent_Movement_Spec_Section_5_v1_1.md` | Any test mocking `IFirstTouchEventQueue` | Remove or replace with stub |
| `Collision_System_Spec_Section_6_v1_1.md` | Any performance reference to event queue | Remove or note as deferred |
| `First_Touch_Spec_Section_6_v1_0.md` | Event queue in performance budget | Remove ring buffer from budget; note as deferred |

**Version impact:** `First_Touch_Spec_Section_4_v1_0.md` → v1.1 (combined with ERR-001 fix)

---

## ERR-005: `KickType` enum encodes caller intent into Ball Physics

**Severity:** Major
**Detected:** February 19, 2026
**Status:** CLOSED — resolved during audit session

**Resolution:**
`KickType` enum eliminated entirely. `Ball.ApplyKick()` signature reduced to physical
parameters only: `ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin,
int agentId, float matchTime)`. The pass type is fully encoded in the velocity and
spin vectors — Ball Physics does not need to know the caller's intent label to simulate
correct aerodynamics. Pass Mechanics maps its `PassType` to physical parameters; that
is its entire job.

**Files affected by resolution:**
- `Ball_Physics_Spec_Section_3_1_Amendment_1_v1_0.md` — drafted without `KickType`
- `Pass_Mechanics_Spec_Outline_v1_0.md` — `KickType` references are outline-only;
  will not appear in Section 3 implementation

---

## ERR-006: `Ball.ApplyKick()` referenced in Ball Physics §8 but never defined in §3.1.11

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Resolved in Ball_Physics_Spec_Section_3_1_v2_5.md (February 21, 2026)

**Resolution:**
`ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime)`
defined at §3.1.11.2. No `KickType` parameter (ERR-005 resolution). Option B possession
model applied (ERR-008 resolution). State transitions to `AIRBORNE` or `ROLLING` on kick;
agent system observes and clears possession on its side.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Ball_Physics_Spec_Section_3_1_v2_4.md` | §3.1.11 | Add §3.1.11.1 label to `CheckPossession()`; add §3.1.11.2 `ApplyKick()` method (no `KickType` per ERR-005 resolution); update table of contents |
| `Ball_Physics_Spec_Section_8_v1_2.md` | §8.3 reference | Update `§3.1.11.2` cross-reference to `§3.1.11.2` (or §3.1.11.3 per final subsection numbering) |

**Version impact:** `Ball_Physics_Spec_Section_3_1_v2_4.md` → v2.5

---

## ERR-007: `KickPower`, `WeakFootRating`, `Crossing` absent from `PlayerAttributes`

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Resolved in Agent_Movement_Spec_Section_3_5_v1_3.md (February 22, 2026)

**Resolution:**
`KickPower` (1–20), `WeakFootRating` (1–5), and `Crossing` (1–20) added to
`PlayerAttributes` struct. All 9 blocked Pass Mechanics tests (PV-006, WF-001–WF-006,
IT-004) are now unblocked.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Agent_Movement_Spec_Section_3_5_v1_2.md` | §3.5.6 `PlayerAttributes` | Add `KickPower` (1–20), `WeakFootRating` (1–5), `Crossing` (1–20); update struct comment `Consumed by` list; update struct size estimate |

**Version impact:** `Agent_Movement_Spec_Section_3_5_v1_2.md` → v1.3

---

## ERR-008: `BallState` has no `PossessingAgentId` field; `ApplyKick()` amendment references it

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Option B adopted February 22, 2026. Resolved in Ball_Physics_Spec_Section_3_1_v2_5.md.

**Design Decision: Option B — Possession external to BallState**

Possession is agent state, not ball state. `BallState` is a pure physics struct; adding
`PossessingAgentId` would introduce the only agent reference in Ball Physics, violating
single responsibility. It would also create a synchronisation hazard between two systems
both tracking possession.

**Resolution:**
`ApplyKick()` transitions `ball.State` from `CONTROLLED` to `AIRBORNE` (or `ROLLING`).
The agent system observes this state transition and clears its own possession record.
Agent system is the single source of truth for possession. No `PossessingAgentId` field
added to `BallState`.

Ball_Physics_Spec_Section_3_1_v2_5.md §3.1.11.2 documents this design with full rationale.

---

## ERR-009: `PassThroughGround` / `PassThroughAerial` are redundant `KickType` values

**Severity:** Minor
**Detected:** February 19, 2026
**Status:** CLOSED — resolved during audit session

**Resolution:**
Through passes use the same aerodynamic profile as their non-through equivalents
(`PassGround` and `PassLofted` respectively). The distinction between a through ball
and a regular pass is entirely a Pass Mechanics targeting concern — the receiver
prediction model, lane detection, and lead distance calculation. Ball Physics sees
identical physics profiles. Separate `KickType` values were unnecessary.

The `KickType` enum was subsequently eliminated entirely (ERR-005), making this
resolution moot. Recorded for completeness.

---

## ERR-011: `SpatialHashGrid.Query()` ignores radius parameter — always returns fixed 3×3 neighbourhood

**Severity:** Major
**Detected:** February 23, 2026 (Shot Mechanics Spec #6 §4 cross-spec audit)
**Status:** CLOSED — Fixed in Collision_System_Spec_Section_3_v1_1.md; Query() now uses
dynamic neighbourhood sizing: `cellRadius = Ceil(radius / CELL_SIZE)`. Interim workaround in Shot Mechanics §4.4.1; root cause unfixed

**Root Cause:**

`SpatialHashGrid.Query(Vector3 position, float radius)` accepts a `radius` argument
but never reads it. The implementation unconditionally queries the 3×3 cell neighbourhood
around the query position (covering approximately ±1.5m regardless of the radius
argument passed). This was documented in the Collision System spec as a comment
("not currently used; 3×3 query is always sufficient") but the architectural consequence
for callers using larger pressure radii was not evaluated.

**Problem in detail:**

All three systems that query the spatial hash for pressure detection — Pass Mechanics,
Shot Mechanics, and First Touch — pass `PRESSURE_RADIUS_MAX = 3.0m` to `Query()`. The
call returns only entities within the fixed ±1.5m neighbourhood. Opponents at 1.6–3.0m
are invisible to the pressure model in all three specifications.

**Impact by system:**
- **Pass Mechanics (Spec #5):** `PassErrorCalculator` under-estimates pressure for shots
  taken with opponents at 1.6–3.0m. Passes executed under moderate pressure behave as if
  under no pressure.
- **Shot Mechanics (Spec #6):** Same effect on `ShotErrorCalculator`. Shots under
  moderate defensive pressure are not penalised correctly.
- **First Touch (Spec #4):** Same effect on `FirstTouchPressureEvaluator`. Ball control
  under moderate pressure is over-estimated.

**Interim workaround (applied in Shot Mechanics §4.4.1 v1.3):**

Callers must distance-filter the `Query()` result set after receiving it:

```csharp
List<AgentId> queriedEntities = SpatialHash.QueryRadius(center, PRESSURE_RADIUS_MAX, filter);
List<AgentId> nearbyOpponents = queriedEntities
    .Where(id => Vector3.Distance(center, AgentSystem.GetAgent(id).Position)
                 <= PRESSURE_RADIUS_MAX)
    .ToList();
```

This workaround is correct — the 3×3 neighbourhood is a superset of all entities within
3.0m (a 3.0m radius on 1.0m cells requires at most ±3 cells to capture; the 3×3 returns
±1 cells). **The workaround does NOT fully fix the defect** — opponents at 1.6–3.0m that
fall in cells beyond the ±1 neighbourhood are still missed. However, at normal match
density (22 agents on a 105×68m pitch), the probability of an opponent being at 1.6–3.0m
but outside the 3×3 neighbourhood is low. The workaround reduces the error but does not
eliminate it.

**Correct fix:**

`SpatialHashGrid.Query()` must compute a dynamic neighbourhood based on the radius
parameter:

```csharp
public List<int> Query(Vector3 position, float radius)
{
    int cellRadius = Mathf.CeilToInt(radius / SpatialHashConstants.CELL_SIZE);
    // Query (2*cellRadius+1)² cells instead of fixed 3×3
    for (int dy = -cellRadius; dy <= cellRadius; dy++)
    for (int dx = -cellRadius; dx <= cellRadius; dx++)
    { /* add cells */ }
}
```

For `PRESSURE_RADIUS_MAX = 3.0m` on 1.0m cells: `cellRadius = 3`, query covers 7×7 = 49
cells (vs current 9). Performance impact is negligible at N=22 agents.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Collision_System_Spec_Section_3_v1_0.md` | §3.1.4 `Query()` implementation | Dynamic neighbourhood: `cellRadius = Ceil(radius / CELL_SIZE)`; iterate `(2*cellRadius+1)²` cells |
| `Pass_Mechanics_Spec_Section_4_v1_0.md` | §4.4.1 pressure query | Add interim workaround comment (or remove workaround once Collision System fixed) |
| `First_Touch_Spec_Section_4_v1_1.md` | §4.4 pressure query | Add interim workaround comment |

**Version impact:** `Collision_System_Spec_Section_3_v1_0.md` → v1.1 (when fixed)

---

## ERR-008-002 … ERR-008-011: Decision Tree #8 comprehensive audit (June 11, 2026)

Filed during the comprehensive audit of the Decision Tree #8 spec + its May 29, 2026
implementation (the audit the April 27 approval carved out as a pre-implementation
follow-up; implementation landed first, so the audit ran as a combined document-and-code
review). Full findings, severities, and fix traceability:
`docs/specs/decision-tree/audit-report.md`. Code-side companions: H-1 (assembly never
compiled — static calls to instance executors; the SIXTH consecutive spec with a
structurally dead build surface, and the first where the PRODUCTION assembly was dead),
H-2 (= ERR-008-002), H-3 (= ERR-008-008 vicinity), M-1..M-11, L batch. All spec-side
entries patched in the same commit; ERR-008-006 documented-open (Stage 1 WIDE_ZONE
declaration).

---

## Revision Summary

| Priority | ERR ID | Blocking | Status |
|----------|--------|----------|--------|
| ~~1 — Fix before Section 3~~ | ERR-006, ERR-007, ERR-008 | ~~Yes~~ | ✅ All three closed |
| ~~2 — Fix before approval~~ | ERR-001, ERR-004 | ~~Yes~~ | ✅ Both closed in First_Touch_Spec_Section_4_v1_1.md |
| 3 — Fix at convenience | ERR-002, ERR-003 | No | Open — minor edits to Master_Vol_4 and Agent Movement §3.2 |
| **2 — Fix before Collision System approval** | **ERR-011** | **Yes (blocks Collision System §4 approval)** | **Closed — fixed in Collision_System_Spec_Section_3_v1_1.md (Mar 5, 2026)** |
| 3 — Fix at convenience before Shot Mechanics final sign-off | ERR-010 | No | ✅ Closed — fixed in shot-mechanics/section-1.md v1.2 (March 6, 2026) |
| 3 — Fix at convenience | ERR-012 | No | ✅ Closed — fixed in first-touch/section-7.md v1.1 (March 5, 2026) |

**All critical Shot Mechanics cross-spec audit defects resolved (A1–A7). ERR-011 is a
Collision System defect with an interim workaround applied — it blocks Collision System
Section 3 revision, not Shot Mechanics approval. ERR-010 is a minor documentation
error (Decision Tree spec number) in Shot Mechanics §1.1 — non-blocking on approval.**

---

**v1.4 Changes (Mar 5, 2026):
- ERR-009 (SpatialHash Query) renumbered to ERR-011 to resolve duplicate ID
  conflict with ERR-009 (KickType, closed). ERR-011 now CLOSED.

End of Error Log v1.4**

---

## ERR-012: First Touch §7 refers to Decision Tree as Spec #7 (5 occurrences)

**Severity:** Minor (documentation error; no architectural impact)
**Detected:** March 5, 2026
**Detected During:** First Touch Specification #4 comprehensive audit
**Root Cause:** Same as ERR-010 — First Touch Section 7 was written before the specification
numbering was finalised. Decision Tree was tentatively #7; Perception System was subsequently
inserted at #7, bumping Decision Tree to #8.

**Problem in detail:**
`First_Touch_Spec_Section_7_v1_0.md` references "Decision Tree Spec #7" in 5 locations:
- §7.1.4 body text: "Decision Tree (Spec #7, Stage 1)"
- §7.2.4 body text: "Decision Tree (Spec #7, Stage 1/2 scope)"
- §7.2.4 dependency line: "Decision Tree Spec #7"
- §7.6 dependency map row: "Decision Tree Spec #7 | Intent flag | Stage 1"
- §7.6 dependency map row: "Decision Tree Spec #7 | Intent flag | Stage 2"

**Correct approach:**
Replace all 5 instances of "Spec #7" (referring to Decision Tree) with "Spec #8".

**Status:** ✅ CLOSED — Fixed in `first-touch/section-7.md` (March 5, 2026, First Touch
comprehensive audit remediation).

**Files revised:**

| File | Section | Change |
|------|---------|--------|
| `first-touch/section-7.md` (was v1.0 → v1.1) | §7.1.4, §7.2.4, §7.6 | All "Decision Tree Spec #7" → "Decision Tree Spec #8" |

**Version impact:** `first-touch/section-7.md` → v1.1

---

*End of Spec Error Log — the version is the HEADER's (`**Version:**` at the top); this footer previously pinned v1.54 and had gone thirty revisions stale (balance-pass AR pass 5, L1).

---

## ERR-016-001: Phantom interface risk in Deterministic Simulation Spec #16 §4.2

**Severity:** Medium (architectural discipline; no immediate code impact — Stage 0 spec phase)
**Detected:** May 2, 2026
**Detected During:** Deterministic Simulation Spec #16 drafting (adversarial review + v0.7 fix pass)
**Root Cause:** Same root cause as ERR-001 and ERR-004. §4.2 originally contained normative C#-shaped interface sketches (`IDeterministicRngService`, `IReplayRunner`, etc.) against consumer specs (#17 Event System, #18 Performance Optimization, #19 Testing Strategy) that are all currently `NOT STARTED`. Writing normative interface shapes before the consumer is specified creates phantom interfaces that constrain future design.

**Mitigation applied (v0.7 fix pass):**
§4.2 was reframed as explicitly **non-normative sketches** — the C# shapes are illustrative only. The §4.2.1 *behavior contract* remains normative (determinism in inputs→outputs, byte-idempotent serialization, canonical ordering in Compare output). The note at the top of §4.2 explicitly cites CLAUDE.md's "write interfaces only when both sides are specified" rule and the ERR-001/004 hazard, and prohibits promotion to normative `.cs` interfaces until consumer specs #17/#18/#19 reach at least `IN REVIEW`.

**Status:** ✅ MITIGATED — phantom interface risk contained by non-normative classification. Full resolution requires co-authoring final interface shapes with specs #17/#18/#19.

**Files revised:**

| File | Section | Change |
|------|---------|--------|
| `docs/specs/deterministic-sim/section-4.md` | §4.2 preamble | Added non-normative disclaimer and phantom-interface hazard citation |

---

*End of Spec Error Log v1.6 — May 2, 2026.*

---

## ERR-016-002: EntityId no-reuse cross-spec constraint not back-propagated

**Severity:** Medium (consistency/discipline; latent integrity hazard if specs #2/#8 silently reuse EntityIds during a match)
**Detected:** May 3, 2026
**Detected During:** Deterministic Simulation Spec #16 third-pass adversarial critique (finding M-F)
**Root Cause:** Deterministic Simulation §3.2.5 declares a normative constraint binding two already-APPROVED specs:

> "entity allocators in Agent Movement (#2) and the AI subsystem (Decision Tree #8) MUST guarantee EntityId uniqueness for the lifetime of a match; once an EntityId is despawned it MUST NOT be reassigned."

This is the renumbering-cascade hazard CLAUDE.md flags: a downstream spec adding a normative constraint to upstream specs after they have been approved, without filing reciprocal `XC-` cross-references in those specs. As of May 3, 2026, neither Agent Movement (#2) nor Decision Tree (#8) carries a corresponding `XC-` reference to Deterministic Simulation §3.2.5; the constraint is "floating".

**Problem in detail:**
- Agent Movement #2 was approved Apr 27, 2026.
- Decision Tree #8 was approved Apr 27, 2026 (at draft-level rigor).
- The EntityId no-reuse constraint is necessary for #16's RNG stream isolation and replay parity, but is unenforceable until specs #2 and #8 explicitly carry it.
- Without back-propagation, an implementer of Agent Movement could legitimately recycle a despawned EntityId to a new agent on the same tick. This would silently break per-stream RNG cursor isolation in Deterministic Simulation, manifesting only as a hard desync at replay time.

**Required fix:**
1. Add an `XC-002-NNN` cross-reference in Agent Movement #2 §3 (entity allocator) citing Deterministic Simulation §3.2.5; declare the no-reuse constraint normatively in #2's own constants/contracts.
2. Add an `XC-008-NNN` cross-reference in Decision Tree #8 (subsystem entity allocation, if any) likewise.
3. File the back-propagation as a minor revision of both specs, version-bumped (no behavioral changes; constraint is consistent with how a sane allocator would behave anyway).
4. Once both reciprocal references exist, mark this entry CLOSED.

**Status:** ✅ FULLY RESOLVED — May 18, 2026. All three required steps confirmed complete:
1. Agent Movement #2 §2.5 as `XC-002-001` (v1.1.1, non-behavioral patch) — landed May 6, 2026.
2. Decision Tree #8 §1.7.3 as `XC-008-001` (v1.1.1, non-behavioral patch) — landed May 6, 2026.
3. `docs/specs/deterministic-sim/section-3.md` §3.2.5 prose confirmed updated from "filed for back-propagation" to "back-propagated to #2 §2.5 and #8 §1.7.3" (verified by OBS-1 probe in stress-test Tier A Run 2, May 18, 2026). CLAUDE.md OPEN ISSUES entry removed.

**Files revised:**

| File | Section | Change |
|---|---|---|
| `docs/specs/agent-movement/section-1-2.md` | New §2.5 | `XC-002-001` (EntityId no-reuse). v1.1.1 patch. |
| `docs/specs/decision-tree/section-1.md` | New §1.7.3 | `XC-008-001` (EntityId no-reuse). v1.1.1 patch. |
| `docs/specs/deterministic-sim/section-3.md` §3.2.5 | post-fix prose | Pending: update "filed for back-propagation" line. |

**Version impact:** Patch revision (v1.1 → v1.1.1) of Agent Movement #2 and Decision Tree #8 — no behavioral change; constraint formalizes existing sensible allocator behavior.

---

## ERR-017-001: `DOMAIN_TAG_EVENT_LEDGER` allocation required in Deterministic Simulation #16 §3.4

**Severity:** Medium (cross-spec back-prop; latent if not landed before #17 IN REVIEW)
**Detected:** May 12, 2026
**Detected During:** PASS 2 adversarial review of `event-system/outline-detailed.md` v1.0 (finding 3)
**Root Cause:** Event System #17 §3.4.2 declares the `Events`-phase digest preimage as `SerializeCanonical(DOMAIN_TAG_EVENT_LEDGER ‖ EventLedgerRecord[T])`. This domain-tag entry is normatively owned by Deterministic Simulation #16 §3.4's domain-tag table, but no allocation exists there. There is no documented mechanism by which a downstream spec registers a domain-tag need with #16; the dependency direction (#17 cites #16) makes this a chicken-and-egg.

**Problem in detail:**
- Spec #17 needs a stable numeric `DOMAIN_TAG_EVENT_LEDGER` to commit its FM-017-001 formula to.
- Spec #16 §3.4 currently does not enumerate `EVENT_LEDGER` among its allocated domain tags.
- Without back-prop, #17 cannot reach `APPROVED` (its `[CROSS-PENDING]` constant cannot promote to `[CROSS]`).
- The same hazard class as ERR-016-002 (downstream spec adds normative constraint on upstream after the upstream's review pass).

**Required fix:**
1. At `event-system/outline-detailed.md` reaching IN REVIEW, file a patch to `docs/specs/deterministic-sim/section-3.md` §3.4 domain-tag table allocating `DOMAIN_TAG_EVENT_LEDGER` (next available numeric value in #16's tag-namespace).
2. Update §3.10 constants catalogue in `event-system/outline-detailed.md` (and any drafted §3 section file) to pin the literal value and promote `[CROSS-PENDING]` → `[CROSS]` at the same beat that resolves the citation's `TBD-NORMATIVE` tag (gated on #16 reaching `APPROVED` per KD-2).
3. Once the allocation lands in #16, mark this entry CLOSED.

**Status:** ✅ FULLY RESOLVED.

- **#16-side — May 14, 2026.** `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in `docs/specs/deterministic-sim/section-3.md` §3.4 (next value after `DOMAIN_TAG_ENV_FP = 0x14`); §3.5 v1.0.1 patch-revision history entry recorded; §8.3.1 #17 row promoted `pending re-audit → complete` atomically with this resolution; §8 v1.2 version-history entry recorded.
- **#17-side — May 15, 2026 (#17 §1.0.1 patch revision).** `[CROSS-PENDING]` → `[CROSS]` promotion completed and literal value `0x15` inlined across `docs/specs/event-system/`: §3.4.2 prose; §3.10 constants catalogue row + trailing-notes paragraph; §1.4 cross-spec-constants-imported summary; §2.4.4 `EventLedgerRecord` preimage description; §7.5 D9 deferred-decisions row (RESOLVED); §8.1.4 ERR-017-001 row; §8.3.4 imported-constants table (heading renamed `[CROSS]` constants imported); §8.4 constant-provenance summary row; §9.2 Q10 quality-checklist row; §9.3 R3 review-checklist row; Appendix B preamble + B.1 / B.2 / B.3 byte streams (symbolic `DT` replaced with literal `15`); Appendix D glossary row. Section-version histories on §1 / §2 / §3 / §7 / §8 / §9 / appendices each carry a v1.0.1 row recording the patch.

**Files revised at #16 side:**

| File | Section | Change |
|---|---|---|
| `docs/specs/deterministic-sim/section-3.md` | §3.4 constants catalogue | Added `DOMAIN_TAG_EVENT_LEDGER = 0x15` `[FIXED]` row citing ERR-017-001 |
| `docs/specs/deterministic-sim/section-3.md` | §3.5 version history | v1.0.1 patch-revision entry recording the allocation and rationale (no `DETERMINISM_DIGEST_VERSION` bump) |
| `docs/specs/deterministic-sim/section-8.md` | §8.3.1 audit table + §8.5 v1.2 | #17 row promoted to `complete`; ERR-017-001 closure recorded |

**Files revised at #17 side (May 15, 2026; §1.0.1 patch revision):**

| File | Section | Change |
|---|---|---|
| `docs/specs/event-system/section-1.md` | §1.4 | `[CROSS-PENDING]` → `[CROSS]`; literal value `0x15` inlined; ERR-017-001 marked RESOLVED |
| `docs/specs/event-system/section-2.md` | §2.4.4 | `EventLedgerRecord` preimage prose updated to `0x15` / `[CROSS]` |
| `docs/specs/event-system/section-3.md` | §3.4.2, §3.10 + trailing notes | `[CROSS-PENDING]` → `[CROSS]`; literal value `0x15` inlined in formula prose and constants catalogue |
| `docs/specs/event-system/section-7.md` | §7.5 D9 | Deferred-decision row marked RESOLVED with `0x15` |
| `docs/specs/event-system/section-8.md` | §8.1.4 ERR-017-001, §8.3.4 heading + row, §8.4 row | ERR-017-001 RESOLVED; `[CROSS]` table and provenance summary updated to `0x15` |
| `docs/specs/event-system/section-9-approval-checklist.md` | §9.2 Q10, §9.3 R3 | Evidence rows updated to reflect `[CROSS]` promotion and ERR-017-001 RESOLVED |
| `docs/specs/event-system/appendices.md` | Appendix B preamble + B.1 / B.2 / B.3, Appendix D | Byte streams inline literal `15`; glossary row updated to `0x15` / `[CROSS]` |

**Version impact:** Patch revision (`v1.0` → `v1.0.1`) on the #16 side (§3.5) and on the #17 side (sections 1, 2, 3, 7, 8, 9-approval-checklist, appendices). No behavioral change on either side; pure namespace allocation in #16 (catalogue grew; no preimage layout, field width, or hash-input rule changed; no `DETERMINISM_DIGEST_VERSION` bump) and pure tag/value substitution in #17 (no FR text changed, no formula re-derived).

---

## ERR-017-002: §3.2.1/§3.2.2 Publish/Subscribe API specified as constraint-only overloads — illegal C# (CS0111)

**Severity:** High (production assembly never compiled; every claim resting on event-system test execution was unverifiable)
**Detected:** June 12, 2026
**Detected During:** First-ever full-tree compile on the non-certifying dotnet CI gate (`tools/dotnet-ci/`)
**Root Cause:** #17 §3.2.1 declared `Publish<T>(in T evt)` three times, distinguished only by `where T : struct, IEventA/IEventB/IEventC`, asserting "the compiler picks the path at the call site; there is no runtime tier dispatch." C# generic constraints are NOT part of a method signature — overloading on constraints alone is CS0111 in every compiler, including Unity's. The spec passed two adversarial review passes because no reviewer compiled the surface; the implementation (`EventBus.cs`, plus `EventBusStub.cs` in pass-mechanics / shot-mechanics / perception-system / heading-mechanics / goalkeeper-mechanics) reproduced the illegal triple verbatim, so `TacticalDirector.EventSystem` and the five forwarding surfaces never compiled. Eighth instance of the structurally-dead-build-surface class; second (after Decision Tree AR-2 H-1) in a PRODUCTION assembly.

**Resolution (June 12, 2026, same commit):**

1. **Spec:** §3.2.1/§3.2.2 rewritten to ONE `Publish<T>`/`Subscribe<T>` (`where T : struct`) with tier routing via per-closed-type cached marker flags; exactly-one-marker contract (FR-EVT-009a) enforced at the entry point at runtime; §3.2.2 compile-time-mismatch note re-anchored (section-3.md v1.0.2).
2. **Code:** new `EventTierCache<T>` (type-init reflection only; JIT folds the flags to constants — FR-EVT-048 zero-alloc preserved); `EventBus.cs` v1.9 single-method dispatch + tier-contract throw; `CosmeticChannel.cs` v1.9 internal `SubscribeFromBus` seam (public `Subscribe` keeps its `IEventC` constraint) + internal `Publish` constraint relaxed; five `EventBusStub.cs` files merged to a single `where T : struct` forwarder. All call sites compile unchanged.
3. **Adjacent fix surfaced by first execution:** `EventOrdinalCache<T>` is a separate static-generic type, so reading it never triggered `EventRegistry`'s seeded-row static constructor — a Subscribe/Publish of a #17-owned event before anything else touched `EventRegistry` threw `ERR_EVT_UNREGISTERED_ORDINAL`. New no-op `EventRegistry.EnsureInitialized()` called at the EventBus entry points (EventRegistry.cs v1.5).

**Status:** ✅ RESOLVED June 12, 2026.

---

## ERR-010-001: `DOMAIN_TAG_HEADING` allocation required in Deterministic Simulation #16 §3.4

**Severity:** Medium (cross-spec back-prop; latent if not landed before #10 APPROVED)
**Detected:** May 16, 2026
**Detected During:** Section-files v0.1 → v0.2 PASS-1 adversarial-review fix pass (`heading-mechanics/adversarial-review-section-files-v1.md` finding M-1). v0.1 KD-10 / Appendix G / §9.4 OI-001 each claimed the entry was "created during section authoring", but `grep ERR-010 docs/tracking/spec-error-log.md` returned only the long-closed ERR-010 (Shot Mechanics renumbering; March 6, 2026). v0.2 files this row.
**Root Cause:** Heading Mechanics #10 §3.4 + §3.7 route Gaussian and float draws through `DeterministicRngService` (Deterministic Simulation #16 §4.1) keyed on `DOMAIN_TAG_HEADING`. This domain-tag entry is normatively owned by #16 §3.4's domain-tag table, but no allocation exists there yet. Same hazard class and same resolution shape as `ERR-017-001` (Event System #17 / `DOMAIN_TAG_EVENT_LEDGER = 0x15`, closed May 15, 2026).

**Problem in detail:**
- Spec #10 needs a stable numeric `DOMAIN_TAG_HEADING` to commit its three draw-site IDs (`DRAW_SITE_DUEL_TIEBREAK`, `DRAW_SITE_CONTACT_POINT_ERROR`, `DRAW_SITE_TIMING_JITTER`) to.
- Spec #16 §3.4 currently does not enumerate `HEADING` among its allocated domain tags.
- Without back-prop, #10 cannot reach `APPROVED` (its `[CROSS-PENDING]` constant in §3.1 cannot promote to `[CROSS]`).
- Next available numeric slot in #16 §3.4's tag-namespace is `0x16` (verified May 16, 2026: current allocations run `0x10`..`0x15`).

**Required fix:**
1. At `heading-mechanics/SPEC_INDEX.md` row 10 reaching `IN REVIEW`, file a patch to `docs/specs/deterministic-sim/section-3.md` §3.4 domain-tag table allocating `DOMAIN_TAG_HEADING = 0x16` (next available numeric value in #16's tag-namespace). Pure namespace allocation — no `DETERMINISM_DIGEST_VERSION` bump required, per the `ERR-017-001` precedent (#16 §3.5 v1.0.1 patch revision, May 14, 2026).
2. Update §3.1 Master Physical Profile Table in `heading-mechanics/section-3.md` to pin the literal value `0x16` and promote `[CROSS-PENDING]` → `[CROSS]` at the same beat that #16's allocation lands.
3. Once the allocation lands in #16, mark this entry CLOSED.

**Status:** ✅ FULLY RESOLVED.

- **#16-side — May 16, 2026.** `DOMAIN_TAG_HEADING = 0x16` allocated in `docs/specs/deterministic-sim/section-3.md` §3.4 (next value after `DOMAIN_TAG_EVENT_LEDGER = 0x15`); §3.5 v1.0.2 patch-revision history entry recorded. Pure namespace allocation in #16's tag-namespace; no `DETERMINISM_DIGEST_VERSION` bump (catalogue grew; no preimage layout, field width, or hash-input rule changed). Follows the v1.0.1 / ERR-017-001 precedent exactly.
- **#10-side — May 16, 2026 (#10 v0.3 patch revision).** `[CROSS-PENDING]` → `[CROSS]` promotion completed in `heading-mechanics/section-3.md` §3.1 Master Physical Profile Table; literal value `0x16` retained; ERR-010-001 reference updated `pending → RESOLVED`. §1.3 KD-10 wording updated; §1.4 dependency table updated; §8.2 / §8.4 / §9.1 / §9.2 / §9.4 OI-001 status rows all updated. Section-version histories on §1 / §3 / §9 / appendices each carry a v0.3 row recording the patch.

**Files revised at #16 side:**

| File | Section | Change |
|---|---|---|
| `docs/specs/deterministic-sim/section-3.md` | §3.4 constants catalogue | Added `DOMAIN_TAG_HEADING = 0x16` `[FIXED]` row citing ERR-010-001 |
| `docs/specs/deterministic-sim/section-3.md` | §3.5 version history | v1.0.2 patch-revision entry recording the allocation and rationale (no `DETERMINISM_DIGEST_VERSION` bump) |

**Files revised at #10 side (May 16, 2026; v0.3 patch revision):**

| File | Section | Change |
|---|---|---|
| `docs/specs/heading-mechanics/section-1.md` | §1.3 KD-10, §1.4 | Wording updated to reflect RESOLVED filing; #16 anchor pinned |
| `docs/specs/heading-mechanics/section-3.md` | §3.1 | `[CROSS-PENDING]` → `[CROSS]`; literal value `0x16` retained |
| `docs/specs/heading-mechanics/section-8.md` | §8.2, §8.4 | XC-010-004 row marked RESOLVED; #16 row updated |
| `docs/specs/heading-mechanics/section-9-approval-checklist.md` | §9.1, §9.2, §9.4 OI-001, §9.5 | All checklist rows referencing OI-001 / `DOMAIN_TAG_HEADING` checked/RESOLVED |
| `docs/specs/heading-mechanics/appendices.md` | Appendix G | OI-001 status updated to RESOLVED |

**Version impact:** Patch revision (#16 §3.5: `v1.0.1 → v1.0.2`; #10 sections: `v0.2 → v0.3`). No behavioral change on either side; pure namespace allocation in #16 and pure tag-promotion in #10.

---

## ERR-011-001: `DOMAIN_TAG_GOALKEEPER` allocation required in Deterministic Simulation #16 §3.4

**Severity:** Medium (cross-spec back-prop; latent if not landed before #11 APPROVED)
**Detected:** May 16, 2026
**Detected During:** Section-files v0.1 → v0.2 PASS-1 adversarial-review fix pass (`goalkeeper-mechanics/adversarial-review-section-files-v1.md`). Filed at the moment Goalkeeper Mechanics #11 section files v0.2 land and `SPEC_INDEX.md` row 11 flips `NOT STARTED → IN REVIEW`.

**Root Cause:** Goalkeeper Mechanics #11 §3.3 / §3.5 / §3.6 route Gaussian draws through `DeterministicRngService` (Deterministic Simulation #16 §4.1) keyed on `DOMAIN_TAG_GOALKEEPER`. Same hazard class and same resolution shape as `ERR-010-001` (Heading #10 / `DOMAIN_TAG_HEADING = 0x16`, closed May 16, 2026) and `ERR-017-001` (Event System #17 / `DOMAIN_TAG_EVENT_LEDGER = 0x15`, closed May 15, 2026).

**Problem in detail:**
- Spec #11 needs a stable numeric `DOMAIN_TAG_GOALKEEPER` to commit its four draw-site IDs (`DRAW_SITE_HANDLING_NOISE`, `DRAW_SITE_HANDLING_POINT_NOISE`, `DRAW_SITE_DIVE_TIMING_JITTER`, `DRAW_SITE_CROSS_CLAIM_TIEBREAK`) to.
- Spec #16 §3.4 currently does not enumerate `GOALKEEPER` among its allocated domain tags.
- Without back-prop, #11 cannot reach `APPROVED` (its `[CROSS-PENDING]` constant in §3.4 cannot promote to `[CROSS]`).
- **Collision-management policy (KD-7).** Open ERR-012-001 proposes block `0x17…0x1C` for Positioning AI #12 Phase B/C; whichever spec reaches `APPROVED` first takes `0x17`. If ERR-011-001 lands first, the #12 block re-shifts to `0x18…0x1D` (mirroring the May 16, 2026 #10 / #12 shift via ERR-010-001 vs. ERR-012-001). If ERR-012-001 lands first, `DOMAIN_TAG_GOALKEEPER` shifts to `0x1D`. The `[CROSS-PENDING]` tag accommodates either outcome.

**Required fix:**
1. At `goalkeeper-mechanics/SPEC_INDEX.md` row 11 reaching `APPROVED`, file a patch to `docs/specs/deterministic-sim/section-3.md` §3.4 domain-tag table allocating `DOMAIN_TAG_GOALKEEPER`. Numeric value depends on collision-management outcome (`0x17` or `0x1D`). Pure namespace allocation — no `DETERMINISM_DIGEST_VERSION` bump, per ERR-010-001 / ERR-017-001 precedent.
2. Update §3.4.9 in `goalkeeper-mechanics/section-3.md` to pin the literal value and promote `[CROSS-PENDING]` → `[CROSS]` at the same beat that #16's allocation lands.
3. Once the allocation lands in #16, mark this entry CLOSED.

**Status:** ✅ Resolved May 18, 2026 — `DOMAIN_TAG_GOALKEEPER = 0x1D` allocated in #16 §3.4 v1.0.5 (Positioning AI #12 reached APPROVED first and claimed `0x17`; per KD-7 first-to-APPROVED precedent GK shifted to `0x1D`); #11 §3.4.9 `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted atomically with #16 back-prop landing.

---

*End of Spec Error Log v1.11 — May 16, 2026.*

---

## ERR-010: Shot Mechanics §1.1 refers to Decision Tree as Spec #7

**Severity:** Minor (documentation error; no architectural impact)  
**Detected:** February 27, 2026  
**Detected During:** Decision Tree Specification #8 Outline v1.1 pre-approval review (BLK-001)  
**Root Cause:** Shot Mechanics Specification #6 was written before the specification
numbering was finalised. At time of authoring, the Decision Tree was tentatively
assigned #7. Perception System was subsequently inserted at #7, bumping Decision Tree
to #8. The Shot Mechanics text was not updated.

**Problem in detail:**  
`Shot_Mechanics_Spec_Section_1_v1_1.md` §1.1 Dependencies section references:
> "Decision Tree Specification #7"

The canonical specification number for the Decision Tree, as recorded in
`PROGRESS.md` (authoritative), `FILE_MANIFEST.md`, and Perception System
Specification #7 §1.1, is **#8**.

This creates an inconsistency that could mislead implementers cross-referencing
Shot Mechanics with Decision Tree documentation.

**Correct approach:**  
Replace all instances of "Decision Tree Specification #7" with "Decision Tree
Specification #8" in `Shot_Mechanics_Spec_Section_1_v1_1.md`.

**Blocking condition:**  
This error is non-blocking on Shot Mechanics approval (the architectural content is
correct; only the number is wrong). However, it **must be closed before**:
1. Shot Mechanics receives final lead developer sign-off, and
2. Decision Tree Specification #8 Section 4 (interface contracts) is written and
   references Shot Mechanics as a dependency by number.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Shot_Mechanics_Spec_Section_1_v1_1.md` | §1.1 Dependencies table, any other references | Replace "Spec #7" with "Spec #8" for Decision Tree |

**Version impact:** No version increment required for minor text correction. Document
in Shot Mechanics changelog when the edit is made.

---

## ERR-018-002: `[HotPathAllocExempt]` cited as declared in Spec #20 §3 but does not exist there

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option-2 path; Spec #20 not touched).
**Severity:** High (citation of APPROVED spec for content it does not contain — matches CLAUDE.md "fabricated checklist values" hazard class)
**Detected:** May 14, 2026
**Detected During:** PASS-1 adversarial review of Performance Optimization #18 section files v0.1
**Root Cause:** The `[HotPathAllocExempt]` C# attribute is referenced as a key allocation-exemption mechanism in five locations in #18, every one of which treats the attribute as already declared in Spec #20 §3 (APPROVED May 11, 2026). Grep against the entire `code-standards/` folder returns zero hits for `HotPathAllocExempt` or any allocation-exemption attribute. The attribute is not declared in Spec #20.

**Problem in detail:**

Cited locations:
- `section-2.md` FR-PO-053: "exempt via `[HotPathAllocExempt]` (declared in Spec #20 §3, cite-not-redefine per KD-1)"
- `section-3.md` §3.1.2: "exempted via `[HotPathAllocExempt]` (cite Spec #20 §3)"
- `section-3.md` §3.7.5: "exempted via the `[HotPathAllocExempt]` attribute declared in Spec #20 §3"
- `section-8.md` §8.1.4: "§3 `[HotPathAllocExempt]` attribute (cited by §3.7.5, FR-PO-053)"
- `appendices.md` Appendix B: "Exemptions require `[HotPathAllocExempt]` per Spec #20 §3"

§3.7.5 itself hedges with "Coordinate with the #20 author if the attribute is not yet declared … attribute presence to be verified at first `src/` commit," which directly contradicts the surrounding "declared in Spec #20 §3" claim. The spec is simultaneously asserting the attribute exists in #20 and acknowledging it may not.

**Required fix (choose one):**

1. **Update Spec #20 §3** to formally declare the `[HotPathAllocExempt]` attribute with version-history entry and lead-developer re-sign-off (Spec #20 is APPROVED; any spec change requires sign-off per CLAUDE.md). Spec #18 citations then resolve.
2. **Move ownership to Spec #18** — declare the attribute in #18 §3.7 directly; drop the KD-1 cite-not-redefine framing for this case. Update Spec #20's `[HotPathAllocExempt]` row only if/when #20 adopts it.
3. **Tag as `[CROSS-PENDING]`** — treat the attribute name as a cross-spec constant gated on a future Spec #20 patch; file the back-prop expectation here and in #18's body text.

Option (2) has the smallest cross-spec blast radius because #20 is APPROVED and (1) would require re-review.

**Files requiring revision (per resolution path chosen):**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-2.md` | FR-PO-053 | Reword to remove "declared in Spec #20 §3" claim |
| `docs/specs/performance-optimization/section-3.md` | §3.1.2, §3.7.5 | Same |
| `docs/specs/performance-optimization/section-8.md` | §8.1.4 | Same |
| `docs/specs/performance-optimization/appendices.md` | Appendix B | Same |
| `docs/specs/code-standards/section-3.md` (option 1 only) | §3 | Add attribute declaration |

**Version impact:** #18 section-file revision (v0.1 → v0.2). Option (1) additionally bumps Spec #20 (re-review required).

**Resolution (May 14, 2026):** Option (2) applied. `section-3.md` §3.7.5, `section-2.md` FR-PO-053, and `appendices.md` Appendix B all updated. `[HotPathAllocExempt]` declared as Spec #18 §3.7.5 governance identifier. Spec #20 unchanged.

---

## ERR-018-003: MUST/MAY conflict between FR-PO-067 and §3.4.4 on baseline-reproducibility re-run

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §3.4.4 upgraded MAY → MUST with Stage 0 carve-out).
**Severity:** High (binding-requirement contradiction within the same spec)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review of #18 section files v0.1
**Root Cause:** FR-PO-067 in `section-2.md §2.2.9` states the baseline-reproducibility auditor **MUST** re-run the recorded session manifest. §3.4.4 in `section-3.md` (the implementing mechanics section for that FR) states the validator **MAY** re-run the session. §2 is the binding-requirement section; §3 is the implementing mechanics. The verbs disagree directly on the same action.

**Problem in detail:**

FR-PO-067 (normative MUST): *"The §5.4 baseline-reproducibility auditor MUST re-run the recorded session manifest and confirm the recaptured metric matches within §3.4.3 confidence interval."*

§3.4.4 (mechanics MAY): *"Reproducibility check (Stage 0+1): the validator MAY re-run the session under the recorded seed + fingerprint + platform pin and confirm the captured metric matches within the §3.4.3 confidence interval."*

FR-PO-068 makes failure to re-run a merge-blocking event. The §3.4.4 "MAY" would allow the validator to silently skip the check without triggering FR-PO-068's block.

**Required fix:**

Either upgrade §3.4.4 to "MUST re-run" (aligning §3 with §2's binding requirement), or downgrade FR-PO-067 to SHOULD (aligning §2 with §3's permissive mechanic). FR-PO-068's merge-blocking semantics push toward the MUST resolution.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.4.4 | "MAY" → "MUST" (recommended) |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.4.4 "MAY" → "MUST". FR-PO-067 (MUST) and §3.4.4 (now MUST) are consistent.

---

## ERR-018-004: Three-way stage-of-resolution contradiction on +5% threshold (FR-PO-031 / §7.5 D9 / §7.1)

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §7.5 D9 re-anchored Stage 0+1 to match FR-PO-031 and §7.1).
**Severity:** High (three locations in the same spec state three different resolution stages for the same governance number)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** The +5% per-PR regression threshold (`[GT]` governance number) has its resolution stage stated three times with three different answers.

**Problem in detail:**

- **FR-PO-031** (`section-2.md §2.2.5`): "`[GT]` pinned at Stage 0+1 §7.5 D9" — implies pin at Stage 0+1.
- **§7.5 D9** (`section-7.md`): "Resolution stage: Stage 1 | Notes: Tie to first-month variance measurement" — explicit Stage 1.
- **§7.1** (`section-7.md`) Stage 0+1 Transition Deliverables: "§3.5.2 +5% threshold re-evaluated against actual baseline variance" — listed as Stage 0+1 deliverable.

The three statements cannot all be true. Either the threshold is pinned/re-evaluated at Stage 0+1 (FR-PO-031 + §7.1) and D9 is wrong, or D9 is correct and FR-PO-031 + §7.1 are wrong.

**Required fix:**

Choose one canonical stage and update all three locations. Recommended: Stage 0+1 (matches FR-PO-031 + §7.1 which jointly outvote D9; matches the operational reality that you can't gate Stage 0+1 CI on a Stage-1 threshold).

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-7.md` | §7.5 D9 | "Stage 1" → "Stage 0+1" (under recommended resolution) |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-7.md` §7.5 D9 resolution stage changed from "Stage 1" to "Stage 0+1". All three locations (FR-PO-031, §7.1, §7.5 D9) now consistently state Stage 0+1.

---

## ERR-018-005: Channel registry schema absent from Appendix F; §3.8.2 "Stage 0 declares schema" obligation unmet

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; new **Appendix F.0 Channel Registry Schema** authored with 12 schema fields; §3.8.2 channel-registry bullet rewritten to cite F.0 as the Stage 0 schema deliverable).
**Severity:** High (declared Stage 0 deliverable is missing; channel names used without registry backing)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** §3.8.2 in `section-3.md` explicitly states the channel registry is a Stage 1 deliverable but the **schema** for the registry is a Stage 0 deliverable to be published in Appendix F. Appendix F as written contains only F.1–F.5 dashboard schemas; there is no channel registry schema. Compounding this, F.1, F.2, and F.4 reference channel names (`perf.budget`, `perf.alloc`) as data sources without those channels having registry entries.

**Problem in detail:**

§3.8.2: *"Channel registry. Named channels per subsystem, declared in Appendix F catalogue (Stage 1 deliverable; **Stage 0 declares schema**)."*

Appendix F section headings: F.1 Per-Spec Per-Tick Budget Dashboard, F.2 Per-PR Delta Dashboard, F.3 Milestone-Baseline Trend Dashboard, F.4 Allocation-Tracker Dashboard, F.5 Flake/Determinism Cross-Reference Dashboard. All five are dashboard schemas; none is a channel registry schema. No section in Appendix F defines what fields a channel registry entry carries (channel name, owning subsystem, default verbosity level, sampling rule, sink routing, determinism class, etc.).

**Required fix:**

Author an "Appendix F.0 — Channel Registry Schema" (or "Appendix H — Channel Registry Schema") before F.1, declaring the schema fields per channel entry. Stage 0 deliverable; populated entries are Stage 1.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/appendices.md` | New Appendix F.0 / H | Add channel registry schema headers (channel name, subsystem, verbosity, sampling rule, sink, determinism class) |

**Version impact:** #18 appendices revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** Appendix F.0 "Channel Registry Schema" added to `appendices.md` with full field schema (channel_name, subsystem_owner, verbosity_tier_min, sink_targets, emission_veto_required, record_format, declared_stage) and Stage 0 channel registry table with three seed entries (perf.budget, perf.alloc, perf.trace).

---

## ERR-018-006: Hot-path allocation budget = 0 bytes/tick tagged `[GT]` instead of `[FIXED]` in §3.10

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §3.10 row re-tagged `[GT]` → `[FIXED]`; §8.4 mirror row updated).
**Severity:** Medium (constant-tag misclassification; implies designer-tunability of an architectural mandate)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-3.md` §3.10 tags "Hot-path allocation budget = 0 bytes/tick" as `[GT]`. Per CLAUDE.md "Constant Tags" table, `[GT]` = "Gameplay-Tuned; Designer sets value; must live in tunable config." The zero-allocation budget is a non-negotiable architectural mandate from CLAUDE.md "When Writing Code: zero-allocation architecture in the game loop" — not a designer-settable value. Tagging it `[GT]` creates a false implication that a game designer could change it.

**Required fix:**

Re-tag as `[FIXED]` ("invariant by project mandate") or remove from the constants catalogue entirely and treat as a pure CLAUDE.md cite. FR-PO-050's "MUST declare allocation budget = 0 bytes per tick" reinforces the non-tunable nature.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.10 Constants Catalogue | "Hot-path allocation budget = 0 bytes/tick" tag `[GT]` → `[FIXED]` |
| `docs/specs/performance-optimization/section-8.md` | §8.4 Constant Provenance Summary | Mirror the tag change |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.10 tag updated `[GT]` → `[FIXED]`; rationale updated to "non-tunable invariant". `section-8.md` §8.4 mirrored.

---

## ERR-018-007: Three Spec #19 body-text citations missing `TBD-NORMATIVE` tag

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; `TBD-NORMATIVE` added to §3.3.5, §3.4.3, §3.9.5; §9.4.1 #19 blocker list extended).
**Severity:** Medium (KD-4 status caveat violated; §9.4.1 blocker list incomplete)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** KD-4 mandates that every Spec #19 citation in #18 carry a `TBD-NORMATIVE` tag because #19 is `IN REVIEW`. §9.4.1 enumerates blocked sections — but three #19 body-text citations are absent from that list and carry no tag.

**Problem in detail:**

1. **`section-3.md` §3.4.3:** *"provisional value 30 samples / 95% CI per Spec #19 §3.4.3 parallel convention"* — no `TBD-NORMATIVE`; not in §9.4.1.
2. **`section-3.md` §3.3.5:** *"selection criteria parallel Spec #19 §6.1 — must support deterministic re-play …"* — no `TBD-NORMATIVE`; not in §9.4.1.
3. **`section-3.md` §3.9.5:** *"owned by Spec #19 §3.1 end-to-end / soak layer for test execution"* — no `TBD-NORMATIVE`; not in §9.4.1.

All three would silently rot if #19's section numbering shifts before #18 is approved.

**Required fix:**

Add `(TBD-NORMATIVE)` parenthetical to each citation and add §3.4.3, §3.3.5, §3.9.5 to §9.4.1's #19 blocker list.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.4.3, §3.3.5, §3.9.5 | Add `TBD-NORMATIVE` tag to each #19 citation |
| `docs/specs/performance-optimization/section-9-approval-checklist.md` | §9.4.1 | Add §3.4.3, §3.3.5, §3.9.5 to #19 blocker list |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `(TBD-NORMATIVE)` added to all three citations in `section-3.md`. `section-9-approval-checklist.md` §9.4.1 #19 blocker list extended with §3.3.5, §3.4.3, §3.9.5.

---

## ERR-018-008: §3.9.1 ±20% promotion tolerance untagged and absent from constants catalogue

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; inline `[GT]` tag at §3.9.1; new ±20% row in §3.10 + §8.4 with rationale).
**Severity:** Medium (untagged constant; CLAUDE.md requires source tag on every constant in every spec)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-3.md` §3.9.1 declares: *"the first Stage 0+1 baseline capture promotes the estimate to a measured value tagged `[GT]` if within ±20% of estimate, or files an `ERR-018-NNN` review finding if not."* The ±20% threshold governs whether a spec's implementation matches its design estimate — a consequential governance number. It carries no `[GT]`/`[EST]`/`[FIXED]` tag and is absent from §3.10's constants catalogue.

**Required fix:**

Add the ±20% threshold to §3.10's table with `[GT]` tag and rationale (e.g., "twice the +5% per-PR threshold for first-measurement variance"). Also add to §8.4 constant-provenance summary.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.9.1 | Append `[GT]` tag to ±20% |
| `docs/specs/performance-optimization/section-3.md` | §3.10 | Add ±20% row with `[GT]` and rationale |
| `docs/specs/performance-optimization/section-8.md` | §8.4 | Mirror row |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `[GT]` tag added inline in `section-3.md` §3.9.1. §3.10 row added: "±20% acceptance tolerance `[GT]`". `section-8.md` §8.4 mirrored.

---

## ERR-018-009: FR-PO-070 (Stage 0 MUST) requires invoking Stage 0+1 tooling

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option (b) — FR-PO-070 split Stage 0 manual / Stage 0+1 automated; §5.2 activation row and §5.6 traceability row updated).
**Severity:** Medium (FR activation-stage / tooling-availability mismatch)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** FR-PO-070 (`section-2.md §2.2.10`) has activation stage Stage 0 and MUST-level binding: *"`tools/run-perf-local.sh` (Appendix E) MUST invoke the §5.3 schema-conformance auditor and §5.5 loop-tag auditor against `docs/specs/` only."* Appendix E's shell script invokes `python3 tools/budget-auditor.py`, which §7.1 lists as a Stage 0+1 deliverable. At Stage 0 the tool does not exist; the script as written cannot run.

**Problem in detail:**

Appendix E partially acknowledges this: *"`tools/budget-auditor.py` and `tools/perf-harness/run.sh` are Stage 0+1 deliverables (§7.1). At Stage 0 the auditor's behaviour is a manual review against §3.1.2 schema and §3.2.2 loop-tag mandate; the script above is the structure into which the automated implementation will land."* But FR-PO-070's MUST language and "Stage 0" activation do not reflect this caveat.

**Required fix:**

Either (a) move FR-PO-070 to "Stage 0+1" activation stage in §2.2.10 — matching when its tool dependencies exist — or (b) keep at Stage 0 but qualify the MUST to "MUST execute the manual review equivalents of the schema-conformance and loop-tag auditors per §5.3 and §5.5."

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-2.md` | §2.2.10 FR-PO-070 | Move to Stage 0+1, or qualify Stage 0 manual interpretation |
| `docs/specs/performance-optimization/section-5.md` | §5.2 Stage-Gated Activation Table | Update FR-PO-069 … 074 row if FR-PO-070 stage shifts |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** FR-PO-070 stage column updated to "Stage 0 (manual) / Stage 0+1 (automated)" with qualifier note clarifying Stage 0 uses manual audit execution per Appendix E template.

---

## ERR-018-010: Appendix F.1 N=100 and F.5 1% flake-rate thresholds absent from §3.10

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; both values added to §3.10 + §8.4 with rationale; Appendix F.5 inline `[GT]` tag appended).
**Severity:** Medium (governance constants outside the declared constants catalogue; F.5 also untagged)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** §3.10 declares itself the constants catalogue for #18's governance numerics. Appendix F (`appendices.md`) introduces two governance numbers not present in §3.10:

- **F.1:** "per-spec p50/p99 over last **N=100** captures (`[GT]`, pinned at Stage 0+1)."
- **F.5:** "flake rate **> 1%** triggers boundary-defect routing (§5.7.3)." — untagged.

§3.10's evidence-artifact convention says each `[GT]` value's evidence is the section-file path that introduces it; these two values introduce themselves in Appendix F but are not catalogued.

**Required fix:**

Add both values to §3.10 (and §8.4 mirror) with tags and rationale. F.5's threshold needs a tag (`[GT]` likely).

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.10 | Add `N=100 captures` row (`[GT]`, Appendix F.1) and `1% flake-rate threshold` row (`[GT]`, Appendix F.5) |
| `docs/specs/performance-optimization/section-8.md` | §8.4 | Mirror both rows |
| `docs/specs/performance-optimization/appendices.md` | Appendix F.5 | Append `[GT]` tag to "> 1%" |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.10 rows added for N=100 and 1% flake-rate. `section-8.md` §8.4 mirrored. `appendices.md` F.5 "> 1%" tagged `[GT]`.

---

## ERR-018-011: `SPEC_INDEX.md` row 18 not updated; §9.4 prematurely claims `IN REVIEW`

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option (a) — `SPEC_INDEX.md` row 18 + CLAUDE.md OPEN ISSUES + `file-manifest.md` row 18 all flipped to `IN REVIEW` atomically; §9.3 atomic-update checkbox flipped `[x]` for the `IN PROGRESS → IN REVIEW` transition; `IN REVIEW → APPROVED` flip remains the future atomic update with lead-developer sign-off).
**Severity:** Medium (canonical-registry contradiction; CLAUDE.md says SPEC_INDEX.md is the source of truth on status)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-9-approval-checklist.md` §9.4 declares *"Status: `IN REVIEW` (author-driven flip; lead-developer review pending)."* `SPEC_INDEX.md` row 18 still shows `IN PROGRESS`. CLAUDE.md states: *"SPEC_INDEX.md is the canonical source of truth for spec numbers, folder names, and approval status."* By that rule, the spec is `IN PROGRESS`, regardless of what §9.4 claims. CLAUDE.md OPEN ISSUES entry for #18 also still says "Section files remain stubs," which is no longer accurate.

**Problem in detail:**

§9.3 checklist row *"`SPEC_INDEX.md` status updated atomically with sign-off"* is correctly marked `[ ]` (unchecked) — acknowledging the update hasn't happened. But §9.4's Decision block then asserts `IN REVIEW` as the current status. The §9.4 status claim contradicts both the canonical registry and the unchecked §9.3 checklist row in the same file.

**Required fix:**

Either (a) update `SPEC_INDEX.md` row 18 and CLAUDE.md OPEN ISSUES entry to `IN REVIEW` atomically (the section files are authored — this state would be consistent), or (b) revert §9.4's status claim to `IN PROGRESS` until lead-developer sign-off. The status flip and the registry/CLAUDE.md updates must move together.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/SPEC_INDEX.md` | Row 18 | `IN PROGRESS` → `IN REVIEW` (option a) |
| `CLAUDE.md` | OPEN ISSUES entry for #18 | Update "Section files remain stubs" → "Section files drafted at v0.1; PASS-1 adversarial review filed (ERR-018-002…011); v0.2 fix pass pending"; flip status text to `IN REVIEW` |
| `docs/tracking/file-manifest.md` | #18 rows | Move section files from "stub" to "drafted" |
| `docs/specs/performance-optimization/section-9-approval-checklist.md` | §9.4 (option b alternative) | Revert "IN REVIEW" → "IN PROGRESS" |

**Version impact:** No section-file content revision required; metadata-only across three tracking files (option a). Option b is a one-line §9.4 edit.

**Resolution (May 14, 2026):** Option (a) applied. `SPEC_INDEX.md` row 18 updated `IN PROGRESS` → `IN REVIEW` with changelog entry. `CLAUDE.md` OPEN ISSUES entry for #18 updated to reflect `IN REVIEW` status and v0.2 section files. `file-manifest.md` row 18 updated from "stubs" to "section-1 through section-9-approval-checklist + appendices.md at v0.2".

---

## ERR-018-012: Appendix F has two conflicting `### F.0 Channel Registry Schema` sections

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** High
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` H-1)
**Root Cause:** PR #59 (`claude/fix-performance-specs-J1t5Z`, commit `14c6ba6`) and PR #60 (`claude/review-performance-specs-YHGga`, commit `dd6a87c`) both authored an Appendix F.0 channel-registry schema as fixes for `ERR-018-005`. Both PRs merged into `main` without de-duplication, leaving two `### F.0 Channel Registry Schema` sections in `appendices.md` (lines 231–256 and 258–281) with materially different field sets — 13 fields vs 7 fields, different names (`owning_subsystem` vs `subsystem_owner`, `inside_tick_pipeline` + `sign_off_log_ref` pair vs single `emission_veto_required` boolean, `record_format_version` semver vs `record_format` reference). The §5.7.1 audit hook walks `sign_off_log_ref` — present only in the first schema. The F.1–F.5 dashboards cite `perf.budget` / `perf.alloc` / `perf.trace` channel names — populated only as anchor rows in the second schema.

**Resolution:** Kept the canonical 13-field F.0 (richer, supports §5.7.1 audit hook against `sign_off_log_ref`, declares `record_format_version` semver per KD-11). Merged the duplicate's `perf.budget` / `perf.alloc` / `perf.trace` example rows into the canonical schema as illustrative Stage 0 anchor entries so F.1–F.5 dashboard data-source citations resolve at draft time. Per-subsystem channels (`ai.*`, `physics.*`) remain Stage 0+1 deliverables.

---

## ERR-018-013: `section-3.md` §3.10 has three duplicate-constant rows

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** High
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` H-2)
**Root Cause:** Same PR #59 + PR #60 parallel-branch merge as ERR-018-012. Both branches resolved ERR-018-008 (±20% promotion tolerance) and ERR-018-010 (N=100 dashboard window, 1% flake threshold) by appending rows to §3.10. Merge retained both row sets:

| First (v0.1) row | Duplicate (v0.2) row | Constant |
|------------------|----------------------|----------|
| `[EST]-baseline acceptance tolerance = ±20%` `[GT]` → §3.9.1 | `[EST]→[GT]` promotion tolerance = ±20% `[GT]` → §3.9.1 | ±20% promotion tolerance |
| Dashboard sample window = 100 captures `[GT]` → Appendix F.1 | Per-spec p50/p99 rolling window N = 100 captures `[GT]` → Appendix F.1 | N=100 dashboard window |
| Flake-rate alert threshold = 1% `[GT]` → Appendix F.5 | Flake-rate boundary-defect routing threshold = 1% `[GT]` → Appendix F.5 | 1% flake threshold |

**Resolution:** Deleted the three v0.1 rows; kept the v0.2 rows whose rationale columns are richer. §8.4 mirror table was already correct (v0.1 §3.10 was not mirrored there) — no §8.4 change required.

---

## ERR-018-014: Seven section files carry duplicate v0.2 version-history rows

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-1)
**Root Cause:** Same PR #59 + PR #60 merge as ERR-018-012 / 013. Each branch independently authored its own v0.2 version-history row. Merge retained both, producing the pattern `v0.2 (summary) | v0.1 | v0.2 (detailed fix list)` in seven files: `section-2.md`, `section-3.md`, `section-5.md`, `section-7.md`, `section-8.md`, `section-9-approval-checklist.md`, `appendices.md`. (`section-1.md`, `section-4.md`, `section-6.md` were not affected — only one branch touched each.)

**Resolution:** Consolidated each pair into a single v0.2 row carrying the union of fix-list notes — the more detailed (PR #59) text plus any uniquely-stated items from the PR #60 summary. v0.3 row appended below for this fix-pass landing.

---

## ERR-018-015: `section-1.md` header `Last Updated` is stale

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-2)
**Root Cause:** `section-1.md` line 4 still reads `**Last Updated:** May 13, 2026` despite the v0.2 row at §1.5 being dated May 14, 2026. Every other section file's header is `May 14, 2026 (v0.2 PASS-1 adversarial-review fix pass)`. The v0.2 PR for section-1 updated §1.5 but missed the header.

**Resolution:** Updated header to `**Last Updated:** May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)`.

---

## ERR-018-016: §3.5.2 conflates +5% per-PR gate with ±20% `[EST]`→`[GT]` promotion tolerance

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-3)
**Root Cause:** §3.5.2 *"Per-spec overrides"* bullet says: *"For example, Shot Mechanics #6 §4.5 already declares a 0.05 ms total budget; deviations larger than 5% from the 0.017 ms estimated cite #6 §4.5 authority, not §3.5.2 default."* The +5% per-PR threshold (§3.5.2 / FR-PO-031) is defined against a **measured pre-PR baseline**. The 0.017 ms is a spec-time `[EST]` anchor, not a captured baseline. Per §3.9.1, the first Stage 0+1 capture promotes `[EST]` → `[GT]` if within ±20%; the +5% gate only activates against promoted `[GT]` baselines. The example invokes the +5% gate against an un-promoted anchor.

**Resolution:** Rewrote the example to clarify the staging:
- First Stage 0+1 capture: apply §3.9.1 ±20% promotion tolerance (gate's MAY-override surface not exercised yet — value still an `[EST]` anchor).
- Once promoted: subsequent per-PR captures apply §3.5.2 default +5% gate against the measured baseline, or tighter per-spec override.

---

## ERR-018-017: FR-PO-019 levels `MAY` but embeds an unconditional MUST

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-4)
**Root Cause:** FR-PO-019 stated: *"Cross-scenario profiling (Spec #19 KD-8 cross-spec scenarios) is permitted; the manifest ID and seed MUST be recorded the same way."* Level column: `MAY`. RFC 2119 grammar treats the row's declared level as binding for the whole statement — a MAY-row that embeds a MUST is structurally identical to the MUST/MAY conflict PASS-1 caught as `ERR-018-003` (FR-PO-067 vs §3.4.4). Conformance auditor reading the level column would not enforce the recording requirement.

**Resolution:** Split into two FRs:
- FR-PO-019 (MAY): *"Cross-scenario profiling (Spec #19 KD-8 cross-spec scenarios) is permitted."*
- FR-PO-019a (MUST): *"For any cross-scenario profiling session entered into the baseline corpus, the manifest ID and seed MUST be recorded per FR-PO-016."*

---

## ERR-018-018: §3.7.5 pre-specifies C# attribute signature without specified consumer

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-5)
**Root Cause:** §3.7.5 stated: *"the C# `Attribute` definition lands at first `src/` commit (targets: `Method | Constructor`; required constructor argument: `string rationale`; companion lead-developer-sign-off comment cites the `spec-error-log.md` row that authorizes the exemption)."* The attribute's C# signature is fully pinned at spec time — target enum, constructor argument, companion-comment grammar — but its consumer (the CI allocation-tracker build step that reads the attribute) is unspecified anywhere in #18 / #19 / #20. The allocation-tracker pin is §7.5 D2 / Stage 0+1. CLAUDE.md "Interface Design Principle" (ERR-001 / ERR-004 hazard): *"Write interfaces only when both sides are specified."*

**Resolution:** §3.7.5 deferred the concrete C# signature to Stage 0+1 alongside §7.5 D2. Retained the signature-independent governance contract:
- Every exemption MUST carry a rationale.
- Every exemption MUST be authorized by lead-developer sign-off recorded in `spec-error-log.md`.
- Every exempted call site MUST be marked at the source level so the alloc-tracker CI step can exclude it from the §3.7.4 diff.

---

## ERR-012-001: `DOMAIN_TAG_POSITIONING_AI` allocation needed in #16 §3.4 — proposed Phase B/C block-allocation policy

**Status:** ✅ Resolved May 18, 2026 — `DOMAIN_TAG_POSITIONING_AI = 0x17` allocated in #16 §3.4 v1.0.5; #12 §6.1 `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted atomically; all body-text instances in §1/§2/§3/§4/§8 promoted in v0.3/v0.4 fix passes.
**Severity:** Medium
**Detected:** May 15, 2026
**Detected During:** Positioning AI #12 `outline-detailed.md` v1.1 self-adversarial review (AR-V1-01); resolution proposed in v1.2.
**Files Affected:** 1 (`deterministic-sim/section-3.md` §3.4 domain-tag table)

**Root Cause:** Spec #12 Positioning AI requires a `DOMAIN_TAG_POSITIONING_AI` value to bind `DeterministicRngService` calls per #16 §3.4 / KD-9. The current §3.4 table ends at `DOMAIN_TAG_EVENT_LEDGER = 0x15` (#17, allocated May 14, 2026 per ERR-017-001). Five further Phase B/C specs (#10 Heading, #11 Goalkeeper, #13 Pressing, #14 Defensive, #15 Attacking) will each need their own tag during their own outline → section-file phases.

If each spec unilaterally claims the next-available value at outline time (first-come, first-served), there is a real risk of (a) value collisions when two specs draft concurrently and (b) fragmented patch revisions to #16's APPROVED tag namespace. The cleanest pattern is a single block allocation now, gated on lead-developer sign-off, that all six specs cite as `[CROSS-PENDING]` until the patch lands.

**Proposed Resolution (Phase B/C block `0x17 … 0x1C`) — REVISED May 16, 2026:**

The original proposal (`0x16…0x1B`) assigned `0x16` to Positioning AI #12. However, Heading Mechanics #10 reached `APPROVED` first (May 16, 2026, via ERR-010-001 resolution per the same project precedent — first-to-APPROVED claims the next-available slot) and took `0x16`. The block therefore shifts one slot:

| Spec | Domain Tag | Proposed Value | Notes |
|---|---|---|---|
| #10 Heading Mechanics | `DOMAIN_TAG_HEADING` | `0x16` | ✅ ALLOCATED May 16, 2026 via ERR-010-001 (#16 §3.5 v1.0.2 patch) |
| #12 Positioning AI | `DOMAIN_TAG_POSITIONING_AI` | `0x17` | Drafting NOW (#12 IN REVIEW); shifted from `0x16` after #10's allocation landed |
| #11 Goalkeeper Mechanics | `DOMAIN_TAG_GOALKEEPER` | `0x18` | NOT STARTED |
| #13 Pressing AI | `DOMAIN_TAG_PRESSING_AI` | `0x19` | NOT STARTED |
| #14 Defensive AI | `DOMAIN_TAG_DEFENSIVE_AI` | `0x1A` | NOT STARTED |
| #15 Attacking AI | `DOMAIN_TAG_ATTACKING_AI` | `0x1B` | NOT STARTED |
| #16 reserve | — | `0x1C` | Reserved (one slot of margin from the original `0x1B` ceiling). |

The collision avoidance ERR-012-001 was authored to prevent — multiple specs unilaterally claiming the same slot at outline time — did NOT trigger here because #10's allocation was formal (#16 §3.4 patch landed) before #12's `0x16` `[CROSS-PENDING]` was promoted. #12 must update its `outline-detailed.md` and section files to cite `0x17` when its own back-prop lands.

Block is contiguous with `DOMAIN_TAG_HEADING = 0x16` and consumes one nibble of u8 namespace. No `DETERMINISM_DIGEST_VERSION` bump required (pure namespace allocation, no preimage layout / field width / hash-input rule changes — mirrors the ERR-017-001 resolution pattern).

**Patch landing site:** `deterministic-sim/section-3.md` §3.4 constants catalogue (add 6 rows in canonical numerical order). One revision, six rows; #16 §3.5 version-history row notes Phase B/C namespace allocation.

**Atomic promotion mechanic:** all six specs carry the tag as `[CROSS-PENDING]` until the #16 patch revision lands. On patch merge, each spec promotes its row from `[CROSS-PENDING]` → `[CROSS]` in its own §3.10 / §3.4 / KD-9 citation site in a follow-up patch (parallel to ERR-017-001 #17-side promotion).

**Sign-off required:** Lead developer (#16 owner). Once ratified, #12 outline KD-9 and FR-PA-005 promote from `[CROSS-PENDING]` to `[CROSS]` and section-file authoring proceeds with the value fixed.

---

## ERR-012-002: `decision-tree/section-3-1.md` L716 cites Formation System as "Spec #14" — stale spec number

**Status:** ✅ Closed — Fixed May 15, 2026 in `decision-tree/section-3-1.md` v1.1.1 (single-token patch; approval status preserved)
**Severity:** Minor
**Detected:** May 15, 2026
**Detected During:** Positioning AI #12 `outline-detailed.md` v1.2 Outstanding-Questions resolution pass (Q3 grep against #8).
**Files Affected:** 1 (`decision-tree/section-3-1.md` L716)

**Root Cause:** Decision Tree #8 §3.1.7.2 reads: *"Stage 1 wires the Formation System (Spec #14) to provide live formation slot positions that adjust with tactical instructions and ball position."* Current `SPEC_INDEX.md` row 14 is **Defensive AI**. The Formation System functionality is #12 Positioning AI (verified — #8 §1.4.21 and §1.7.3 already use the canonical #12 number elsewhere in #8). Stale spec number left over from an earlier numbering scheme — same regression class as ERR-010 (Shot Mechanics #6 §1.1 calling Decision Tree #7) and ERR-012 (First Touch §7 calling Decision Tree #7), both closed in the March 2026 renumbering cascade. #8 §3.1.7.2 was missed by that cascade.

**Resolution:** Patch `decision-tree/section-3-1.md` L716 to read "Positioning AI (Spec #12)". One-token change in an APPROVED spec; no behavioural impact; patch-revision row in #8 §3.x version history.

**Detection grep:** `grep -n "Spec #14" decision-tree/` returns only this one line in `section-3-1.md`. (`grep -n "Formation System" decision-tree/section-*.md` returns multiple "Formation System (Stage 1+)" references without spec numbers — those are correct as-is and should not be touched.)

**Recommended patch landing:** alongside #16 §3.4 ERR-012-001 patch (same lead-developer revision pass), or as a standalone one-token revision.

---

## ERR-012-003: Documentary anchor for `XC-012-001`..`XC-012-009` allocation

**Status:** ✅ Closed (informational — no remediation required)
**Severity:** Minor
**Detected:** May 16, 2026
**Detected During:** Positioning AI #12 section-files PASS-1 adversarial review (AR-S1-18).
**Files Affected:** 1 (`positioning-ai/section-8.md` §8.3)

**Root Cause:** AR-S1-18 noted that #9 / #16 / #17 / #19 precedent files at least a short error-log row when allocating `XC-NNN-NNN` typed cross-reference IDs, so cross-spec readers can discover them by grep. Spec #12 §8.3 allocates `XC-012-001`..`XC-012-009` at section-file v0.1 without a corresponding error-log entry.

**Resolution:** This entry serves as the documentary anchor. `XC-012-NNN` are not erratum-class entries — they are typed cross-reference IDs published in `positioning-ai/section-8.md` §8.3 against approved upstreams #2, #8, #16, #18, #20. No remediation; entry exists for grep discoverability.

---

---

## ERR-008-001: Decision Tree #8 §3.2 `PitchGeometry` class uses centered coordinate origin

**Status:** ✅ Resolved May 18, 2026 — `decision-tree/section-3-2.md` v1.3: class rewritten to corner-origin (0,0,0); all `Vector2` goal constants replaced with `Vector3` using correct values; citation corrected to §1.2 and Appendix C; XC-GEOM-01 verification note added.
**Severity:** High
**Detected:** May 18, 2026
**Detected During:** Stress-test Tier A run 1, probe A-06 (coordinate-convention-guard FAIL) + T-03 (inverted domain conventions).
**Files Affected:** 1 (`decision-tree/section-3-2.md`, lines 305–350+)

**Root Cause:** The `PitchGeometry` static class in Decision Tree #8 §3.2 is authored with a center-origin coordinate system — the same defect class logged in CLAUDE.md "Things That Have Gone Wrong Before" ("Wrong coordinate origin — 'Pitch center' comment in Agent Movement §3.5"). The class comment states:

```
/// Coordinate system (consistent with Ball Physics #1 §2.2 and Agent Movement #2 §2.1):
///   Origin (0, 0) = centre of pitch
///   X-axis: pitch length (−52.5m to +52.5m; total 105m)
///   Y-axis: pitch width (−34m to +34m; total 68m)
```

The authoritative coordinate system (CLAUDE.md §"Coordinate System", Ball Physics #1 §1.2 and Appendix C, verified in `ball-physics/section-3-1.md` and `agent-movement/section-3-5-part-1.md`) is:
- Origin: corner of pitch (0, 0, 0)
- X: 0–105m (goal-to-goal)
- Y: 0–68m (touchline-to-touchline)

**Consequence — all goal position constants are wrong:**

| Constant | DT §3.2 value (centered) | Correct corner-origin value |
|----------|--------------------------|----------------------------|
| `HOME_OPPONENT_GOAL_CENTRE` | `(52.5, 0)` | `(105.0, 34.0, 0)` |
| `HOME_OPPONENT_GOAL_POST_L` | `(52.5, +3.66)` | `(105.0, 37.66, 0)` |
| `HOME_OPPONENT_GOAL_POST_R` | `(52.5, −3.66)` | `(105.0, 30.34, 0)` |
| `HOME_OWN_GOAL_CENTRE` | `(−52.5, 0)` | `(0.0, 34.0, 0)` |
| `HOME_OWN_GOAL_POST_L` | `(−52.5, +3.66)` | `(0.0, 37.66, 0)` |
| `HOME_OWN_GOAL_POST_R` | `(−52.5, −3.66)` | `(0.0, 30.34, 0)` |

The citation "consistent with Ball Physics #1 §2.2" is also incorrect — the authoritative section per CLAUDE.md is §1.2 (not §2.2).

**Resolution:**
1. Rewrite `PitchGeometry` class in `decision-tree/section-3-2.md` to use corner-origin (0,0,0) throughout.
2. Update `Origin` comment to `Origin (0, 0, 0) = corner of pitch (home team's left defensive corner)`.
3. Update `X-axis` range to `0m to 105m`. Update `Y-axis` range to `0m to 68m`.
4. Recalculate and update all `Vector2`/`Vector3` goal position constants using the correct system.
5. Switch goal positions to `Vector3` (not `Vector2`) to match the 3D coordinate system; or add a note that Y-component = 0 (ground-level Z in the spec's convention) and Y in `Vector2` here maps to X in the global system — this requires careful thought; simpler to use `Vector3` directly to avoid axis-label confusion.
6. Correct the citation from "§2.2" to "§1.2 and Appendix C".
7. Append a version-history row to `section-3-2.md`.

**Probe trigger:** A-06 FAIL — phrase "Origin (0, 0) = centre of pitch" is a direct origin claim. T-03 defect class (inverted coordinate convention).

---

## ERR-015-006: Attacking AI #15 §1/§2/§3/§4 retain stale `[CROSS-PENDING]` tags after ERR-015-001 resolution

**Status:** ✅ Resolved May 18, 2026 — all 7 stale `[CROSS-PENDING]` hits promoted to `[CROSS: #16 §3.4]` in §1 (4 instances), §2 FR-AT-005, §3 constant table, §4 §4.6 prose; v0.3 version-history rows added to all four section files.
**Severity:** Medium
**Detected:** May 18, 2026
**Detected During:** Stress-test Tier A run 1, probe A-03 (cross-pending-tracker FAIL) + T-05 + T-02.
**Files Affected:** 4 (`attacking-ai/section-1.md`, `section-2.md`, `section-3.md`, `section-4.md`)

**Root Cause:** ERR-015-001 was resolved on May 18, 2026 — `DOMAIN_TAG_ATTACKING_AI = 0x1B` was allocated in `deterministic-sim/section-3.md` §3.4 (v1.0.4), and the `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promotion was applied in `section-6.md` §6.1.9 and `section-9-approval-checklist.md`. However, the same tag appears as `[CROSS-PENDING]` in four additional section files that were not part of the promotion pass. The approval checklist therefore falsely claims "0 `[CROSS-PENDING]` remain" (T-02: fabricated checklist entry).

**Stale hits (all in `attacking-ai/`):**

| File | Line | Stale text |
|------|------|------------|
| `section-1.md` | 114 | `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING]` in §1.4 dependency table |
| `section-1.md` | 164 | "`[CROSS-PENDING]` throughout this spec until ERR-015-001 is ratified" in KD-11 note |
| `section-1.md` | 245 | `0x1B [CROSS-PENDING]` in KD table column |
| `section-1.md` | 266 | `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING] until ERR-015-001 ratified` in cross-spec compliance table |
| `section-2.md` | 25 | FR-AT-005: `([CROSS-PENDING] until ERR-015-001 is ratified in #16 §3.4)` |
| `section-3.md` | 948 | Constant reference table: `\| DOMAIN_TAG_ATTACKING_AI \| [CROSS-PENDING] \| 0x1B (ERR-015-001) \|` |
| `section-4.md` | 206 | `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING] (ERR-015-001; see …)` |

**Resolution:** In each location above, replace `[CROSS-PENDING]` with `[CROSS: #16 §3.4]` and update "until ERR-015-001 is ratified" clauses to "resolved May 18, 2026". Update `section-9-approval-checklist.md` §9.1 evidence row to accurately state which files were updated. Append version-history rows to each of the four section files.

**Probe trigger:** A-03 FAIL — `[CROSS-PENDING]` present in approved spec body text with no matching `Status: OPEN` ERR entry (ERR-015-001 is CLOSED). T-05 (dangling tag after upstream APPROVED). T-02 (fabricated checklist claim).

---

## ERR-016-003: Domain tag registry (#16 §3.4) silent gaps at `0x18` and `0x1C`

**Status:** ✅ Resolved May 18, 2026 — `deterministic-sim/section-3.md` v1.0.6: `_RESERVED_0x18_` and `_RESERVED_0x1C_` placeholder rows added to §3.4 domain-tag table; v1.0.6 version-history row added.
**Severity:** Medium
**Detected:** May 18, 2026
**Detected During:** Stress-test Tier A run 1, probe A-04 (domain-tag-allocator-audit FAIL) + T-08.
**Files Affected:** 1 (`deterministic-sim/section-3.md` §3.4 domain-tag table)

**Root Cause:** The ERR-012-001 Phase B/C block originally proposed the range `0x17…0x1C` (with `0x1C` as one slot of margin). As allocations landed, `0x18` was informally noted in the v1.0.3 changelog as "reserved for #11 Goalkeeper" before Goalkeeper Mechanics was reallocated to `0x1D` (because Positioning AI reached APPROVED first and claimed `0x17`, triggering the first-to-APPROVED cascade that shifted GK from `0x17` to `0x1D`). Neither `0x18` nor `0x1C` was ever assigned or documented in the live §3.4 table as a placeholder.

**A-04 requirement:** "every gap in the allocation sequence has an explicit `_RESERVED_0xNN_` placeholder row in the §3.4 table."

**Actual allocation sequence:**
```
0x10 DOMAIN_TAG_PHASE
0x11 DOMAIN_TAG_SNAPSHOT_PAYLOAD
0x12 DOMAIN_TAG_SNAPSHOT_HEADER
0x13 DOMAIN_TAG_RNGDRAW
0x14 DOMAIN_TAG_ENV_FP
0x15 DOMAIN_TAG_EVENT_LEDGER
0x16 DOMAIN_TAG_HEADING
0x17 DOMAIN_TAG_POSITIONING_AI
[0x18 — MISSING; no row]
0x19 DOMAIN_TAG_PRESSING_AI
0x1A DOMAIN_TAG_DEFENSIVE_AI
0x1B DOMAIN_TAG_ATTACKING_AI
[0x1C — MISSING; no row]
0x1D DOMAIN_TAG_GOALKEEPER
```

**Risk:** A developer assigning the next subsystem domain tag would search for the last-allocated value and find `0x1D`, concluding `0x1E` is next-available. The orphaned `0x18` and `0x1C` remain permanently unavailable for reuse but are not documented as such, creating a silent encoding hole.

**Resolution:** Add two rows to the §3.4 domain-tag table in `deterministic-sim/section-3.md` (in numerical order between the existing rows):

```
| _RESERVED_0x18_ | 0x18 | — | Skipped. Originally informally noted in #16 §3.4 v1.0.3 changelog as a reservation for Goalkeeper Mechanics #11 (ERR-011-001). GK was subsequently reallocated to 0x1D when Positioning AI #12 reached APPROVED first and claimed 0x17 per first-to-APPROVED precedent (ERR-011-001 KD-7 policy). Value 0x18 is permanently orphaned — must not be reassigned to any subsystem without explicit ERR tracking. |
| _RESERVED_0x1C_ | 0x1C | — | Skipped. Block-end margin value of the ERR-012-001 Phase B/C block (0x17…0x1C). Block was closed when 0x1B was allocated to Attacking AI #15 (ERR-015-001). Value 0x1C was never assigned; permanently orphaned — must not be reassigned without explicit ERR tracking. |
```

Append a v1.0.6 version-history row to `deterministic-sim/section-3.md`. No `DETERMINISM_DIGEST_VERSION` bump required (placeholder rows are namespace documentation, not preimage-layout changes).

**Probe trigger:** A-04 FAIL (silent gap without placeholder row). T-08 (DOMAIN_TAG gap).

---

## ERR-020-001: Code Standards #20 §4.2 `[CROSS]` mirror example uses ALL_CAPS field name, contradicting §3.2.3 PascalCase rule

**Spec:** Code Standards #20  
**Section:** §4.2 Constant Catalogue File Convention — `ProjectConstants.cs` Cross-Spec Source of Truth  
**Severity:** Minor  
**Detected During:** `src/CLAUDE.md` v1.3 adversarial review (May 22, 2026), finding M-3.  
**Status:** ✅ Resolved May 22, 2026

**Problem:** The §4.2 worked example for a `[CROSS]` mirror constant in `BallPhysicsConstants.cs` used `PHYSICS_TICK_HZ` (ALL_CAPS) as the mirror field name:

```csharp
public static readonly float PHYSICS_TICK_HZ = ProjectConstants.PHYSICS_TICK_HZ;
```

Spec #20 §3.2.3 (Tag → C# Storage Class Mapping) is the authoritative naming rule and explicitly states that `[CROSS]` constants use PascalCase. The ALL_CAPS convention is reserved exclusively for `[FIXED]` (`public const`) constants. A developer reading only §4.2 would use ALL_CAPS for every `[CROSS]` mirror, producing a codebase-wide naming inconsistency.

**Root Cause:** The §4.2 example was authored with the `PHYSICS_TICK_HZ` name matching the source constant in `ProjectConstants.cs` (which is correctly `[FIXED]` ALL_CAPS) rather than following the mirror field naming convention from §3.2.3.

**Files Affected:**
| File | Location | Change |
|---|---|---|
| `docs/specs/code-standards/section-4.md` | §4.2 mirror example (line ~160) | `PHYSICS_TICK_HZ` → `PhysicsTickHz`; XML doc updated with spec+section citation |
| `src/CLAUDE.md` | `[CROSS]` mirrors naming discrepancy note | Reference to ERR-020-001 added; "has been patched" noted |

**Resolution:** `code-standards/section-4.md` v1.0.1 patch: mirror field renamed to `PhysicsTickHz` (PascalCase); XML doc updated to include authoritative spec and section citation (`Ball Physics #1 §1.2`) and value (`60 Hz`) per FR-CS-022. `src/CLAUDE.md` v1.4 discrepancy note updated with ERR-020-001 reference.

**Rule confirmed:** The source constant in `ProjectConstants.cs` is `[FIXED]` and correctly uses ALL_CAPS (`PHYSICS_TICK_HZ`). The mirror field in any spec's constants catalogue is `[CROSS]` and uses PascalCase (`PhysicsTickHz`). The right-hand side of the mirror assignment must reference the source by its ALL_CAPS name (`= ProjectConstants.PHYSICS_TICK_HZ`).

---

## ERR-004-002: `FirstTouchContext` does not expose the nearest opponent's agent ID — `PossessionStateMachine` cannot resolve `InterceptingAgentID` on INTERCEPTION outcome

**Spec:** First Touch Mechanics #4
**Section:** §3.4.2 (priority-ordered outcome state machine), §4.3.1 (FirstTouchContext fields), §4.3.2 (FirstTouchResult fields)
**Severity:** Minor (Stage 0 carve-out; documented placeholder behaviour)
**Detected During:** `src/first-touch/` AR-5 adversarial review (June 6, 2026), finding L-4.
**Status:** 🟡 Open — placeholder behaviour in place; spec revision deferred

**Problem:** `PossessionStateMachine.Determine` (Priority 1 — INTERCEPTION branch) returns `(TouchResult.Interception, AGENT_ID_NONE, AGENT_ID_NONE)` because `FirstTouchContext` exposes only `HasNearbyOpponent` (bool) + `NearestOpponentDistance` (float) — there is no field carrying the nearest opponent's entity ID. The third tuple element of the return value is supposed to be `InterceptingAgentID`, but the data needed to populate it is not in the context. Result: the `FirstTouchResult.InterceptingAgentID` field surfaced to callers is `AGENT_ID_NONE = -1` on every INTERCEPTION outcome, which is indistinguishable from "no interception" downstream — Stage 1+ consumers that route possession to the intercepting opponent have no way to identify the receiving agent.

**Root Cause:** First Touch #4 §3.4.2 specifies the outcome classification logic but §4.3.1 omits a `NearestOpponentEntityId` field from `FirstTouchContext`. The omission was discovered post-implementation when `PossessionStateMachine` was wired up. The implementation placed an inline `// TODO: spec gap …` comment at the INTERCEPTION return; the AR-5 review found the gap was untracked in the error log.

**Files Affected:**
| File | Location | Change |
|---|---|---|
| `src/first-touch/PossessionStateMachine.cs` | Priority 1 INTERCEPTION return (~line 40) | Inline `TODO:` comment replaced with `ERR-004-002` anchor |
| `docs/specs/first-touch/section-4.md` (pending) | §4.3.1 FirstTouchContext field list | Add `NearestOpponentEntityId : int` field (or equivalent) |
| `src/first-touch/FirstTouchContext.cs` (pending) | Field declarations after `NearestOpponentDistance` | Add the field once §4.3.1 is patched |
| `src/first-touch/FirstTouchSystem.cs` (pending) | EvaluateFirstTouch wiring | Forward the ID into `PossessionStateMachine.Determine` |

**Resolution (proposed):** Add `int NearestOpponentEntityId` (sentinel `AGENT_ID_NONE` when `!HasNearbyOpponent`) to `FirstTouchContext` in a coordinated §4.3.1 patch. Caller (currently the integration boundary in `FirstTouchSystem`) populates it from the same scan that produces `NearestOpponentDistance` — typically the `PressureEvaluator` result. `PossessionStateMachine.Determine` then uses it for the INTERCEPTION return tuple. No formula change; pure data-flow gap closure.

**Stage 0 carve-out:** Until §4.3.1 is patched, INTERCEPTION outcomes carry `InterceptingAgentID = AGENT_ID_NONE`. Stage 0 has no downstream consumer that routes on this field (FirstTouchSystem.ApplyTouchResult only consumes `PossessingAgentID`); the gap blocks Stage 1+ AI-routed interception handoffs but not the Stage 0 test surface.

**Probe trigger:** AR-5 L-4 (June 6, 2026).

---

## ERR-003-001: Collision System #3 §3.3 impulse-to-force conversion F = j × 60 Hz inflates contact force ~10× against literature-calibrated thresholds

**Spec:** Collision System #3
**Section:** §3.3 Step 6 (impact force); contradicts the §3.3.1 threshold derivations (FALL_FORCE_BASE 500 N, FALL_FORCE_PER_STRENGTH 50 N, FALL_PROBABILITY_RANGE 500 N — sustained-force literature values)
**Severity:** Critical
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** `F = Mathf.Abs(j) * 60f` assumes the whole collision impulse acts within one 16.7 ms frame. For an 85 kg equal pair, F ≈ 3315 × vRel (N), so the entire stochastic fall/stumble band (500–1500 N) spanned closing speeds of 0.15–0.6 m/s — below walking pace. Every real contact (jog ≈ 4 m/s closing → 13 kN) was a guaranteed knockdown roll, the failed roll guaranteed a stumble, and `knockdownForceOut` saturated at 1.0 (MaxCollisionForceRef = 2000 N at vRel ≈ 0.6 m/s). The test suite encoded the same scale (FL-002 asserted likely-stumble at vRel = 0.23 m/s), so the calibration defect was invisible to it.

**Resolution:** New `[GT]` `CONTACT_DURATION_S = 0.15 s` (biomechanics contact time ~0.1–0.3 s) added to the §3.3 catalogue; conversion patched to `F = j / CONTACT_DURATION_S` in spec pseudocode and `CollisionResponse.cs` v1.5 (`CollisionPhysicsConstants.ContactDurationS`; `PHYSICS_TICK_HZ` removed — that conversion was its sole consumer). Stochastic band now spans vRel ≈ 1.4–5.4 m/s. FL-001..005 / DT-001..002 closing speeds re-derived (tests v1.2).

---

## ERR-003-002: Collision System #3 §3.3/§3.4 FROM_BEHIND classification — normal convention sign-inverted on two surfaces

**Spec:** Collision System #3
**Section:** §3.3 `ClassifyContactType` (behindDot formula) and §3.4 `ProcessAgentAgentCollision` (Classify call site + `ForceDirection`)
**Severity:** Major
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding M-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** Both `Classify` and `ContactForceData.ForceDirection` are documented against an instigator→victim normal, but the §3.4 call site always passed `manifold.Normal` (entity1→entity2) unflipped — sign-inverted whenever the instigator is the second agent. Compounding it, the §3.3 formula `Dot(-collisionNormal, victimDir) > 0.5` detects a victim moving TOWARD the instigator (head-on), not a fleeing victim; with a doc-correct normal FROM_BEHIND could never fire. Net behaviour: FROM_BEHIND fired only when the second agent instigated, via two cancelling sign errors; identical geometry with the first agent instigating yielded SIDE_IMPACT.

**Resolution:** §3.3 formula corrected to `Dot(collisionNormal, victimDir)` (victim fleeing along instigator→victim normal); §3.4 call site computes `instigatorToVictim = instigatorIdx == 0 ? manifold.Normal : -manifold.Normal` and feeds it to both `Classify` and `ForceDirection`. Implementation: `ContactTypeClassifier.cs` v1.2 + `CollisionSystem.cs` v1.6. Stage 0 consumers do not act on FoulData (Referee is Stage 1+), but the event stream is replay/analytics surface.

---

## ERR-003-003: Collision System #3 §3.3 same-team contacts above fallThreshold escape both fall and stumble branches

**Spec:** Collision System #3
**Section:** §3.3 `DetermineFallOrStumble` (stumble condition)
**Severity:** Major
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding M-2.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** The fall branch requires `!isSameTeam`; the stumble branch required `impactForce <= fallThreshold`. A same-team impact above fallThreshold matched neither — the hardest same-team collisions were consequence-free while moderate ones could stumble (non-monotonic).

**Resolution:** Upper gate dropped; stumble probability clamped to 1 (`Clamp01`). Opposing-team forces above fallThreshold still return from the fall branch first, so its behaviour is unchanged. Spec pseudocode + `CollisionResponse.cs` v1.5.

---

## ERR-003-004: Collision System #3 §3.4 MAX_COLLISION_PAIRS_PER_FRAME valve counts broad-phase candidates and aborts the whole frame

**Spec:** Collision System #3
**Section:** §3.4 `UpdateCollisions` pair loop; §8 sizing rationale ("~10–20 pairs in practice") counted colliding pairs, not candidates
**Severity:** Major
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding M-3.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** The valve charged the 50-pair budget per broad-phase candidate (3×3-cell neighbour after dedupe) and on exceedance aborted all remaining processing including agent-ball. A goalmouth scramble (~15 clustered agents) generates 100+ unique candidates, so the valve fired in exactly the scenarios where collisions matter, deterministically but silently dropping response for the higher-indexed roster half. Candidate iteration needs no valve — it is already bounded at 253 pairs by the dedupe bitfield.

**Resolution:** `ProcessAgentAgent` / `ProcessAgentBall` return narrow-phase confirmation; the valve counts confirmed collisions only (cap = event-buffer capacity, so the buffer cannot overflow). Spec pseudocode + `CollisionSystem.cs` v1.6.

---

## ERR-003-005: Collision System #3 §3.3 impulse response — approach/separation gate inverted for the a1→a2 normal convention

**Spec:** Collision System #3
**Section:** §3.3 Step 2 (relative velocity gate) and Step 4 (impulse application signs); §3.2 defines the manifold normal as pointing from Entity1 toward Entity2
**Severity:** Critical
**Detected During:** `src/collision-system/` AR-8 adversarial review (June 10, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** With n pointing a1→a2, `vRel = (v1 − v2)·n > 0` means a1 closes on a2 — approaching. The pseudocode gated `if (vRel > 0) → separation only` (labelled "separating") and computed `j = −(1+e)·vRel/Σ(1/m)` with `Δv1 = +j·n/m1`. Net behaviour: genuine closing collisions produced penetration separation only — no momentum exchange, no ImpactForce, and `DetermineFallOrStumble` was unreachable for real contacts — while overlapped pairs already moving apart received a velocity-reversing impulse back toward re-collision (energy injection). The unit suite encoded the inversion: CR-001 set both agents moving outward and rationalised it as a "passed-through state".

**Resolution:** Gate corrected to `vRel <= 0 → separation only`; `j = +(1+e)·vRel/Σ(1/m)` (preserving the j > 0 invariant the AR-3/AR-5 simplifications rely on); application signs corrected to `Δv1 = −j·n/m1`, `Δv2 = +j·n/m2`. Restitution verified: equal-mass head-on at ±5 m/s, e = 0.3 → ∓1.5 m/s with separation speed = e·closing speed. Spec §3.3 pseudocode + `CollisionResponse.cs` v1.6; CR-001..003 / FL-001..005 / DT-001..002 / EC-004 setups flipped to approaching geometry (tests v1.3).

---

## ERR-003-006: Collision System #3 §3.3 contact classification — FROM_BEHIND shadowed by the velocity-only shoulder predicate

**Spec:** Collision System #3
**Section:** §3.3 `ClassifyContactType` branch order
**Severity:** Major
**Detected During:** `src/collision-system/` AR-8 adversarial review (June 10, 2026), finding M-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** A chase-down (instigator catching a fleeing victim) has parallel velocities, so the shoulder predicate `Dot(approachDir, victimDir) > 0.7` — which tests velocity alignment only, with no contact geometry — classified every from-behind contact as SHOULDER_TO_SHOULDER before the from-behind test ran. Latent until ERR-003-002 made the from-behind geometry test correct; the two defects together meant FROM_BEHIND was effectively unreachable for its canonical geometry.

**Resolution:** FROM_BEHIND evaluated before SHOULDER_TO_SHOULDER; the contact normal is the discriminator (back-on contact: victimDir ∥ instigator→victim normal; side-by-side: perpendicular, falls through to the shoulder test). Spec §3.3 pseudocode + `ContactTypeClassifier.cs` v1.3.

---


## ERR-001-001: Ball Physics #1 §3.1.8.1 bounce pseudocode uses Unity Y-up `Vector3.up` as the ground normal in a Z-up coordinate system

**Spec:** Ball Physics #1
**Section:** §3.1.8.1 (Impulse-Based Bounce); contradicts §1.2 / Appendix C (Z = height) and Appendix B ("v_n ... vertical for a flat pitch")
**Severity:** Critical
**Detected During:** `src/ball-physics/` AR-7 adversarial review (June 9, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 9, 2026

**Problem:** The §3.1.8.1 pseudocode sets `Vector3 normal = Vector3.up;`. Unity's `Vector3.up` is `(0, 1, 0)` — the touchline (Y) axis in this project's corner-origin Z-up coordinate system. `BallGroundInteraction.ApplyBounce` implemented the line faithfully, so restitution and friction were computed against the lateral velocity component: a vertically falling ball had `v_n = v_y = 0`, zero restitution impulse, zero friction budget (`J_n = 0`), and never rebounded. Every other surface in the assembly (gravity `-Z`, height gates `.z`, the bounce's own `Position.z = RADIUS` write) is Z-up. Undetectable by the test suite because the Unity project is not yet initialized (tests have never executed).

**Resolution:** Spec §3.1.8.1 pseudocode patched to `new Vector3(0f, 0f, 1f)` with an inline ERR-001-001 warning (changelog row 2.8); `BallGroundInteraction.cs` v1.3 fixed identically (AR-7 H-1). Unit/integration expectations re-verified by a numerical mirror of the corrected model.

---

## ERR-001-002: Ball Physics #1 §3.1.8.1 friction stick impulse omits the rotational-coupling divisor

**Spec:** Ball Physics #1
**Section:** §3.1.8.1 STEP 4 (tangential friction impulse)
**Severity:** Major
**Detected During:** `src/ball-physics/` AR-7 adversarial review (June 9, 2026), finding M-1.
**Status:** ✅ Closed — spec and implementation patched June 9, 2026

**Problem:** `J_t_required = m * contactSpeed` is the impulse that zeroes contact-point slip for a non-rotating body. For a sphere the friction impulse also changes ω, so the contact-point velocity changes by `(1 + m·r²/I)` per unit of tangential Δv — for the hollow-sphere model (I = ⅔·m·r²) the factor is 2.5. When the μ·J_n cap is not binding, the applied impulse therefore reversed the slip by ~150% instead of zeroing it, injecting spurious tangential velocity and spin at every gripping bounce.

**Resolution:** Stick impulse divided by the catalogued `[DERIVED]` constant `BallPhysicsConstants.Bounce.StickImpulseCouplingDivisor = 1 + (MASS × RADIUS²) / MomentOfInertia` in both the spec pseudocode (changelog row 2.8) and `BallGroundInteraction.cs` v1.3 (AR-7 M-1).

---

## ERR-001-003: Ball Physics #1 — seven `[EST]` constants lack the FR-CS-020 validation log entries

**Spec:** Ball Physics #1 / Code Standards #20 (FR-CS-020)
**Section:** `src/ball-physics/BallPhysicsConstants.cs` — `Drag.CrisisSpeedLow` (20.0 m/s), `Drag.CrisisSpeedHigh` (25.0 m/s), `Spin.RollingSpinDecayPerSecond` (5.0 rad/s²), `Bounce.SpinToLinearRatio` (0.1), `Limits.MaxVelocity` (50 m/s), `Limits.MaxSpin` (80 rad/s), `Limits.MaxHeight` (50 m)
**Severity:** Minor (documentation-governance gap; values plausible, none validated)
**Detected During:** `src/ball-physics/` AR-8 adversarial review (June 9, 2026), finding L-2.
**Status:** 🟡 Open — this entry IS the required FR-CS-020 record; per-constant validation (promotion to `[GT]`/`[DERIVED]`/`[FIXED]`) is a Stage 1 tuning task

**Problem:** FR-CS-020 requires every `[EST]` constant to carry a `spec-error-log.md` entry tracking its validation path; the seven constants above had none. (An eighth, `Ball.MomentOfInertia`, was retagged `[EST]` → `[DERIVED]` in AR-7 L-2 — it is a documented formula over `[FIXED]` inputs, not an estimate.)

**Validation paths:** `CrisisSpeedLow/High` — literature check against Asai et al. (2007) drag-crisis Reynolds range; `RollingSpinDecayPerSecond` and `SpinToLinearRatio` — empirical tuning against rolling/bounce footage at Stage 1; `Limits.*` — sanity ceilings (fastest recorded shot ≈ 45 m/s) that promote to `[GT]` once gameplay tuning begins.

---

## ERR-004-003: First Touch #4 §3.3.2 direction blend negates ball velocity — heavy touches displaced against their own retained momentum

**Spec:** First Touch Mechanics #4
**Section:** §3.3.2 (Angular Error Model pseudocode); contradicts §3.3.2's own intent prose and §3.3.5 (BallRetained)
**Severity:** Critical
**Detected During:** `src/first-touch/` AR-7 adversarial review (June 10, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** The §3.3.2 pseudocode set `IncomingDir = Normalise(Vector2(-ball.Velocity.x, -ball.Velocity.y))` ("the direction the ball came FROM"). The same subsection states the intended q=0 behaviour four times — "ball goes entirely along incoming direction (no control)", "fallback to IncomingDir — ball follows original path, which is the correct heavy-touch behaviour", and the design rationale "a poorly executed touch deflects the ball further along its original path" — and §3.3.5 retains momentum along `+ball.Velocity`. `BallDisplacementProcessor.cs` implemented the negation faithfully (its v1.1 "H-4 fix" cited the pseudocode line), so a heavy touch teleported the ball up to 2.0 m back toward the passer while its velocity pointed forward — the ball then travelled back through the receiving agent. The test suite ENCODED both conventions simultaneously (BD-002 asserts travel-direction; BD-003/BD-004 assert the negation) — mutually unsatisfiable, and undetected because the suite has never compiled (see FirstTouchTests.cs v1.2 structural note).

**Resolution:** Spec §3.3.2 pseudocode patched to `Normalise(Vector2(ball.Velocity.x, ball.Velocity.y))` with an inline ERR-004-003 warning (changelog v1.4); `BallDisplacementProcessor.cs` v1.5 fixed identically and the degenerate-blend fallback aligned to the spec's IncomingDir mandate (AR-7 M-2). Test expectations re-derived from a numerical mirror of the corrected model. NOTE: `OrientationDetector` negates velocity CORRECTLY (facing-vs-approach comparison) and is untouched.

---

## ERR-004-004: First Touch #4 §3.4.2 interception proximity implemented agent-anchored instead of ball-anchored

**Spec:** First Touch Mechanics #4 / implementation drift
**Section:** §3.4.2 (Determination Logic — `SpatialQuery(newBallPosition, INTERCEPTION_RADIUS)`)
**Severity:** Major
**Detected During:** `src/first-touch/` AR-7 adversarial review (June 10, 2026), finding M-1.
**Status:** ✅ Closed — implementation patched June 10, 2026 (Stage 0 single-candidate approximation documented; full SpatialQuery + interceptor ID land with ERR-004-002)

**Problem:** §3.4.2 anchors the INTERCEPTION opponent query at `newBallPosition`. `PossessionStateMachine` tested `ctx.NearestOpponentDistance` — computed by `PressureEvaluator` around the AGENT and truncated at PressureRadius (3.0 m). With displacement up to RadiusHeavy (2.0 m) against the 2.5 m interception radius, the anchor error reached 80 % of the radius, and an opponent 2.5–3.0 m from the displaced ball but > 3.0 m from the agent read +∞ (invisible). Interceptions both spuriously fired and spuriously missed. The §3.4.5 interception velocity redirect ("Ball velocity set toward intercepting opponent (not zero)") was additionally unimplemented — INTERCEPTION outcomes kept the generic displacement velocity, breaking the Frame N+1 contact chain.

**Resolution:** `PressureEvaluator` v1.3 tracks the global nearest opponent (no radius truncation) and emits `NearestOpponentPositionXY`; `FirstTouchContext` v1.2 / `PressureResult` v1.1 carry it; `PossessionStateMachine` v1.3 measures `|opponent − newBallPosition| ≤ INTERCEPTION_RADIUS`; `FirstTouchSystem` v1.5 Step 7.5 implements the §3.4.5 velocity redirect (speed preserved). Residual Stage 0 approximation: only the single nearest-to-agent opponent is a candidate — the full multi-candidate `SpatialQuery` arrives with the ERR-004-002 context surface (same query returns the interceptor ID).

---

## ERR-004-005: First Touch #4 §3.4.2 DEFLECTION alignment gate is effectively vacuous through the public pipeline

**Spec:** First Touch Mechanics #4
**Section:** §3.4.2 (DEFLECTION momentum-alignment condition) interacting with §3.1 (q model) and §3.3.5 (velocity model)
**Severity:** Minor (model observation; no incorrect code — gate retained per spec)
**Detected During:** `src/first-touch/` AR-7 adversarial review (June 10, 2026), filed with the fix pass.
**Status:** 🟡 Open — documented; revisit when §3.3.5 gains Stage 1 loft/contact modelling

**Problem:** DEFLECTION requires `r ≥ 1.50 m` AND `alignment ≥ 0.70`. Reaching r ≥ 1.50 m requires small q (heavy band), and at small q the §3.3.5 velocity is dominated by `BallRetained = +v·(1−q)·0.5` (agent contribution ≤ DRIBBLE_MAX_SPEED·q ≈ 1.1 m/s vs retention ≥ ~8 m/s for the ball speeds that produce heavy touches), so alignment ≈ 1.0 always. Consequently every non-intercepted touch at r ≥ 1.50 m classifies DEFLECTION, and the low-alignment LOOSE_BALL escape is unreachable for physically producible inputs. Original test PO-005 encoded the unreachable expectation (90° intent ⇒ LOOSE_BALL) and could never pass.

**Resolution path:** Gate retained verbatim per §3.4.2 (it is cheap and becomes meaningful if Stage 1 contact modelling lets the agent contribution scale). PO-005 re-derived to lock the actual behaviour with an ERR-004-005 anchor; branch comment added in `PossessionStateMachine.cs` v1.3. Designer-facing implication: LOOSE_BALL occupies exactly r ∈ [0.60, 1.50) ∪ non-aligned degenerates.

---

## ERR-004-006: First Touch #4 §5.10 VS-001 hand-calc applies the velocity modifier additively and below reference speed, contradicting normative §3.2.3

**Spec:** First Touch Mechanics #4
**Section:** §5.10 VS-001 (validation scenario hand-calc + expected outputs); contradicts §3.2.3 (Velocity Modifier)
**Severity:** Major (test-encoded wrong expectation; no code defect — implementation matches §3.2.3)
**Detected During:** `src/first-touch/` AR-8 follow-up sweep (June 10, 2026), via full-pipeline numerical mirror.
**Status:** ✅ Closed — spec §5.10 and test VS-001 patched June 10, 2026

**Problem:** §3.2.3 defines the velocity modifier as multiplicative on the EXCESS above VELOCITY_REFERENCE (`r = r_base × (1 + Max(0, speed − 15)/15 × 0.25)`), so a 14 m/s ball gets no modifier. The §5 v1.2 changelog (Feb 22, 2026) "corrected" VS-001 from r = 0.195 m to r = 0.428 m by ADDING `(14/15) × 0.25 = 0.233 m` — an additive modifier applied below reference speed, a formula that exists nowhere in §3.2.3 (and whose Appendix B verification inherited the same arithmetic). `FirstTouchTests.cs` VS-001 encoded the 0.428 m expectation; against the §3.2.3-conformant `TouchRadiusCalculator` the actual radius is 0.195 m, so the test could never pass. Undetected because the suite has never compiled (FirstTouchTests.cs v1.2 structural note).

**Resolution:** §5.10 hand-calc and expected outputs re-derived per §3.2.3 (r ≈ 0.195 m; outcome CONTROLLED unaffected) in `section-5-7-to-5-13.md` v1.1 with a §5 changelog row in `section-5-1-to-5-6.md` v1.4; test VS-001 expectation updated to 0.195 ± 0.02 m. The original v1.1→v1.2 flip-flop (0.195 → 0.428 → 0.195) is preserved in both changelogs.

---

## ERR-007-001: Perception System #7 §4.6 forced refresh re-runs the full pipeline and double-advances cross-heartbeat recognition/scheduler state

**Spec:** Perception System #7
**Section:** §4.6 (Forced mid-heartbeat refresh) interacting with §3.3.6 (expiry) and §3.4.2 (shoulder-check scheduling)
**Severity:** High
**Detected During:** `src/perception-system/` AR-3 adversarial review (June 13, 2026), finding H-1.
**Status:** ✅ Closed — implementation patched June 13, 2026

**Problem:** `PerceptionSystem.HandleForcedRefresh` (§4.6) ran the complete `RunAgentPipeline`, including the §3.3.6 second-pass `ProcessInvisible` expiry loop, the `ShoulderCheckScheduler.UpdateAgent` autonomous-schedule advance, and the per-(observer,target) `ProcessVisible` / `ProcessBlindSideEntity` latency increments. Because a forced refresh fires out of the normal 10 Hz cadence (an extra pipeline run between heartbeats), every one of those stateful counters was ticked twice per logical heartbeat whenever a refresh occurred: confirmed-but-invisible entities had their expiry drained early and were evicted prematurely; scheduled shoulder checks could fire or be retimed off-cadence; and visible non-triggering entities accumulated extra latency toward confirmation. §4.6.2 mandates that a forced refresh reset `L_rec` for the **triggering entity only** — all other cross-heartbeat state must be left untouched. The defect made the produced `FilteredView` depend on whether a refresh happened to fire, a determinism hazard.

**Resolution:** `PerceptionSystem.cs` v1.4 gates all three cross-heartbeat mutations behind `!forcedRefresh`. Non-triggering entities now resolve through two new side-effect-free reads — `RecognitionLatencyTracker.IsConfirmed` and `ShoulderCheckScheduler.IsBlindSideConfirmed` — while the triggering entity still force-confirms (`L_rec = 0`) via `ProcessVisible(forcedRefresh: true)` per §4.6.2. The expiry loop and `UpdateAgent` are skipped entirely on a forced refresh. Dead `RecognitionLatencyTracker.ResetObserver` (never called; its doc-comment misrepresented §4.6.2 as a full-observer reset) removed in the same pass. Files: `PerceptionSystem.cs` v1.4, `RecognitionLatencyTracker.cs` v1.3, `ShoulderCheckScheduler.cs` v1.2.

---

## ERR-007-002: Perception System #7 §3.0 Step 1 candidate enumeration truncates the spatial-hash query before de-duplication, dropping unique agents

**Spec:** Perception System #7
**Section:** §3.0 Step 1 (spatial query / candidate enumeration), §4.1 (zero-alloc buffer sizing)
**Severity:** Medium
**Detected During:** `src/perception-system/` AR-3 adversarial review (June 13, 2026), finding M-1.
**Status:** ✅ Closed — implementation patched June 13, 2026

**Problem:** `RunAgentPipeline` copied candidates into the pre-allocated `_candidateIds` buffer (capacity `MaxAgents + 1 = 23`) under `limit = Min(candidates.Count, 23)` — i.e. it truncated the raw query to the first 23 entries **before** the `_candidateVisited` de-duplication ran. `SpatialHashGrid.Query` can return the same entity from multiple cells (body-radius straddle — the very reason the AR-1 M-1 dedup exists), and the ball sentinel (`-1`) was never deduped at all, so the raw list routinely exceeded 23 with duplicates. When it did, a unique agent appearing only past the cap behind duplicate entries was silently dropped from perception. Out-of-range positive ids (`id ≥ MaxAgents`) were also written into the buffer (skipped only downstream), wasting cap slots.

**Resolution:** `PerceptionSystem.cs` v1.4 de-dups across the **full** raw query before any capacity check: agents via `_candidateVisited`, the ball via a single `ballAdded` flag, and `id ≥ MaxAgents` dropped at source. Unique entities are bounded by `MaxAgents + 1`, so the 23-slot buffer cannot overflow and no unique agent can be truncated out (a defensive `break` guards the invariant). File: `PerceptionSystem.cs` v1.4.

---

## ERR-007-003: Perception System #7 §3.3.4 DeterministicHash uses Mathf.Abs — int.MinValue overflow and negative-modulo jitter

**Spec:** Perception System #7
**Section:** §3.3.4 (deterministic L_rec noise) / §3.4.2 (shoulder-check jitter); KD-4
**Severity:** Low
**Detected During:** `src/perception-system/` AR-3 adversarial review (June 13, 2026), finding L-1 (filed with the L-cluster).
**Status:** ✅ Closed — implementation patched June 13, 2026

**Problem:** `RecognitionLatencyTracker.DeterministicHash` returned `Mathf.Abs(h)`. `Math.Abs(int.MinValue)` throws `OverflowException` regardless of the surrounding `unchecked` context (a ~1-in-2³² latent crash on the avalanche output), and any negative hash that escaped would make the callers' `% N` (L_rec noise `% 2`, jitter `% (2·range+1)`) produce an out-of-range negative result — e.g. a jitter of −5 against the intended `[−2, +2]` band.

**Resolution:** `RecognitionLatencyTracker.cs` v1.3 returns `h & 0x7FFFFFFF` (mask off the sign bit) — always non-negative, no overflow, distribution preserved for the downstream modulos. Bundled L items in the same pass (no separate ERR rows): `ShoulderCheckScheduler` possession-interval magic literal `2.0f` → `PerceptionConstants.PossessionCheckIntervalMultiplier` [GT] (FR-CS-016); `FovCalculator.ComputeEffectiveFoV` doc clarified (decisions/ATTR_MAX normalisation per §3.9; only the `MIN_FOV_ANGLE` floor is explicit, the 170° ceiling holds by construction). **NOTE — withdrawn finding:** the AR-3 review additionally flagged the shoulder-check window-close comparison `>` as an off-by-one (window spans `DURATION + 1` ticks) and proposed `>=`; this was REVERTED after the CI gate showed it broke `SC002_Window_ClosesAfterDurationTicks` — that test locks `GetWindowExpiryFrame` as the **last active tick (inclusive)**, so the window is active through expiry by design, not by defect. An anti-re-tightening comment was added instead. Files: `RecognitionLatencyTracker.cs` v1.3, `ShoulderCheckScheduler.cs` v1.2, `PerceptionConstants.cs` v1.3, `FovCalculator.cs` v1.1.

---

## ERR-016-004: Deterministic Sim #16 §3.2.5 DeterministicRngService.Skip() breaks RNG branch-safety (advances RngCursor, not ActionOrdinal)

**Spec:** Deterministic Simulation #16
**Section:** §3.2.5 (branch-safe reservation API: Reserve / DrawReserved / CloseReservation / Skip)
**Severity:** High
**Detected During:** `src/deterministic-sim/` implementation adversarial review (June 15, 2026), finding H-1.
**Status:** ✅ Closed — implementation patched June 15, 2026

**Problem:** A per-stream draw value is `SipHash(streamKey, ActionOrdinal, drawIndex)` — `RngCursor` is **not** an input to the hash. `ActionOrdinal` advances only in `Reserve()`. The pre-fix `Skip(count)` advanced only `RngCursor` and left `ActionOrdinal` untouched, so a code path that took the skip branch instead of the Reserve branch ended the draw-site evaluation with a *different* `ActionOrdinal` than the drawing branch. Every subsequent `Reserve`+`DrawReserved` on that stream then drew from a shifted `ActionOrdinal`, desyncing all later draws. The whole point of the API is branch-safe RNG parity, yet `Skip` — advertised in both its XML doc and `src/CLAUDE.md` as the parity-preserving alternative — silently failed to preserve the only counter that matters. No test exercised `Skip`-vs-`Reserve` divergence, so it was invisible (the existing T-DS-RNG-002 only compares two Reserve branches).

**Resolution:** `Skip(streamIndex, count)` now treats a skip as one consumed draw-site evaluation: it advances `ActionOrdinal` exactly as `Reserve()` does **and** advances `RngCursor` by `count` for cursor parity, and it rejects an open reservation with `ERR_DS_RNG_BUDGET_MISMATCH` (signature `void`→`ushort`, parallel to `Reserve`). `DeterministicRngService.cs` v1.3. New `DeterministicSimAdversarialRegressionTests` fixture locks (a) a Skip branch and a Reserve+draw branch produce identical subsequent draw values, ActionOrdinal, and RngCursor; (b) Skip during an open reservation returns the budget-mismatch code.

---

## ERR-016-005: Deterministic Sim #16 §3.2.3 SnapshotCodec.Encode hashes payload-only — digest chain not chained, and untested

**Spec:** Deterministic Simulation #16
**Section:** §3.2.3 (SnapshotDigest chain) / §3.9.2 / §4.6.1; golden corpus `serialize-canonical-corpus.md` D-04..D-07
**Severity:** Medium
**Detected During:** `src/deterministic-sim/` implementation adversarial review (June 15, 2026), finding M-1.
**Status:** ✅ Closed — implementation patched June 15, 2026

**Problem:** §3.2.3 defines `SnapshotDigest[T] = SHA-256( SerializeCanonical(0x12 ‖ SnapshotHeader[T]) ‖ SerializeCanonical(0x11 ‖ SnapshotPayload[T]) )`, where `SnapshotHeader[T]` carries `prevSnapshotDigest` — that is how the chain links. Production `SnapshotCodec.Encode` instead computed `SHA-256(payloadBytes)` only: it ignored both domain tags, the header (schema/tick/prevDigest), and the environment-fingerprint slot. Consequence: each `CurrentSnapshotDigest` was independent of its predecessor, so altering or diverging an earlier snapshot did **not** invalidate any later digest — the "chain" provided ordering metadata, not tamper-evidence, and `ReplayEngine.ValidatePrevDigest` only verified a stored field that no digest depended on. The defect was untested because the golden-corpus suite (`SerializeCanonicalCorpusTests.Corpus_SnapshotDigestChain_D04toD07`) rebuilds the D-04..D-07 preimages by hand and never calls `SnapshotCodec.Encode` — the recurring "test encodes the spec but does not catch the production divergence" pattern.

**Resolution:** `Encode` now builds the §3.2.3 header preimage (`0x12 ‖ schemaVersion(u32) ‖ tick(u64) ‖ prevSnapshotDigest(32) ‖ envFpDigest(32)`) into a reused buffer and hashes `headerPreimage ‖ 0x11 ‖ payloadBytes` via `TransformBlock`/`TransformFinalBlock` (no combined-buffer allocation); `PrevSnapshotDigest` is threaded in *before* hashing so `CurrentSnapshotDigest` genuinely chains off its predecessor. New `EnvironmentFingerprint.ComputeDigest()` produces the 32-byte envFp slot (canonical `DOMAIN_TAG_ENV_FP ‖ workerCount ‖ length-prefixed §4.8 strings`, cached). The unused `ComputeSha256` helper was removed. `TickOrchestrator` now passes `prevDigest:null` to `Initialize` (the codec is the chain authority). Bundled lower-severity items resolved in the same pass (no separate ERR rows, per the perception L-cluster precedent): (i) `EnvironmentFingerprint` `Lock()`/file doc no longer claim a runtime `ERR_DS_ENV_MUTATION` guard that never existed — immutability is enforced structurally by the `readonly` fields; (ii) `RngStreamState.DrawIndex`/`BudgetRemaining` docs corrected to the actual random-access window semantics; (iii) `SaveManager.Load` returns `ERR_DS_STORAGE_ATOMICITY` for read/IO failure vs `ERR_DS_SCHEMA_INCOMPATIBLE` for a present-but-malformed file; (iv) `TickOrchestrator` AI-no-op comment no longer claims a per-phase digest emission that does not exist; (v) `SaveManager` class doc no longer asserts directory-fsync as a satisfied contract (Stage-0 Windows carve-out). Files: `SnapshotCodec.cs` v1.2, `EnvironmentFingerprint.cs` v1.1, `RngStreamState.cs`, `SaveManager.cs` v1.4, `TickOrchestrator.cs` v1.2; new regression tests assert `Encode` matches the §3.2.3 preimage digest and that the digest depends on `prevDigest`. **Follow-up (informational, non-blocking):** the real (non-empty) envFp preimage encoding is project-chosen — corpus D-05 is explicitly *illustrative-empty* — and should be pinned to a golden vector when the §4.8 EnvironmentFingerprint corpus row lands.

---

## ERR-016-006: Deterministic Sim #16 §4.8.3 floatModelHash tuple is IL2CPP-shaped and contradicts the Stage-0 Mono pin; no live-host hasher exists

**Spec:** Deterministic Simulation #16 (tuple defect); Platform Certification #16 §5.5 / `docs/tracking/certification-platform.md` (contradiction)
**Section:** §4.8 (EnvironmentFingerprint) / §4.8.3 (floatModelHash composition) / §5.5 (certification matrix) / §5.5.1 (deterministic flag strings)
**Severity:** Medium (latent — not currently blocking; the fingerprint is unwired at Stage 0, see below)
**Detected During:** Review of `EnvironmentFingerprint` against §4.8.3 (July 19, 2026) while assessing `SessionManifest`'s fingerprint requirement.
**Status:** ✅ Spec resolved (Option A, owner sign-off July 19, 2026) + live-host hasher landed. The §4.8.2 runtime MXCSR validation and the certified capture were both recorded here as a host-blocked remainder; **both host blocks cleared (July 19 and July 22, 2026 respectively — see the August 3, 2026 update at the end of this entry)**. Open remainder is now unimplemented code, not host access: `SaveManager` still writes `Fingerprint = null`. Tracked below and in the root `CLAUDE.md` OPEN ISSUES.

**Problem:** Three linked issues.

1. **No live-host hasher.** §4.8.3 defines `floatModelHash = SHA-256(SerializeCanonical(0x14 ‖ floatFlagTuple))` over an 11-field tuple of compiler/runtime float-mode flags. No code computes it: `EnvironmentFingerprint.FloatModelHash` is a plain `string` constructor argument, and the class's own `ComputeDigest()` hashes the *outer* 6-field fingerprint for the §3.2.3 snapshot-header preimage — a different digest. The 11-field float-flag tuple has no implementation anywhere.

2. **Spec-vs-pin contradiction on the tuple's own fields.** Tuple fields 1–4 (`compilerToolchain` ∈ {MSVC,Clang,AppleClang,GCC}, `compilerVersion`, `targetTriple` LLVM-style, `il2cppVersion`) are native-compilation / IL2CPP concepts. §5.5 row 0 pins the **Stage-0 developer host to "IL2CPP (MSVC backend)"** and §4.8.3 field 4 states "Stage-0 certification REQUIRES IL2CPP … MUST reject any snapshot whose fingerprint contains `"MONO"`". But `docs/tracking/certification-platform.md` v1.3 pins the Stage-0 backend to **Mono** ("IL2CPP migration is a Stage 5+ concern"), with an explicit `IL2CPP version | N/A (Mono backend)` row. §4.8.3/§5.5 (May 3–4, 2026) predate the platform pin (June 7, 2026). Consequently fields 1–4 have no defined meaning for the runtime actually pinned — a live hasher cannot be written respectably until the spec decides what the tuple means under Mono JIT (or the pin flips to IL2CPP).

3. **Placeholder factory was wrong on `simdFeatureLevel`.** `CreateStage0Dev()` — the sole factory, used by MatchEngine boot and every perf-harness/scenario test — stamped `simdFeatureLevel: "SSE2"`, matching neither the pinned SSE4.2 baseline (certification-platform.md §4.8) nor any other pin. (§4.8.3 field 11 `simdLevel` must equal `simdFeatureLevel`; the dev factory was at least self-consistent at SSE2, but both were wrong.)

**Current blast radius (why Medium, not High):** latent. `SaveManager` writes `headerOut.Fingerprint = null` (not yet wired into the save path), and the fingerprint is load-bearing only at a real certification run, which is independently blocked (no Unity host; `certification-platform.md` is `⏳ RECERT REQUIRED`). Nothing is silently drifting; the risk is that an honest-but-wrong placeholder papers over the undecided spec. Related: ERR-016-005's follow-up already flagged that the *outer* envFp preimage needs a golden vector when the §4.8 corpus row lands; that is distinct from this inner §4.8.3 tuple.

**Resolution (code side, this pass — no fabrication):** `EnvironmentFingerprint.cs` v1.2 — `CreateStage0Dev()` `simdFeatureLevel` `"SSE2"` → `"SSE4.2"` (matches the pin); the placeholder `floatModelHash` lifted to a named `FloatModelHashDevPlaceholder` sentinel; a new `IsDevPlaceholder` property lets a future cert-run gate reject a placeholder fingerprint (the analogue of §4.8.3's "reject MONO" rule for the unimplemented hasher); `FloatModelHash`/`CreateStage0Dev` docs now flag the missing hasher and the IL2CPP/Mono gap. **Deliberately NOT done:** synthesising fields 1–4 or writing a live-host hasher — that is blocked on the spec decision (fabricating those values is precisely what this ERR exists to prevent).

**Resolution (Option A, July 19, 2026 — owner sign-off in `env-fingerprint-float-model-hash-mono-mapping.md` v0.2):** the §4.8.3 tuple is mapped onto the pinned Stage-0 Mono backend, keeping the 11-field shape. **Spec:** `section-4.md` v1.1 — field 1 gains `"Mono"`; field 4 flips so Stage-0 certification ACCEPTS `"MONO"` (reject-MONO / IL2CPP-required move to Stage 5+); a "Stage-0 Mono backend mapping" paragraph pins fields 1–4 (compilerToolchain `"Mono"`, compilerVersion = host-supplied Mono version, targetTriple = RID `"win-x64"`, il2cppVersion `"MONO"`). `section-5.md` v1.1 — §5.5 row 0 backend → Mono; §5.5.1 Mono flag-strings note. **Code:** new `FloatFlagTuple.cs` (`ComputeHash()` = `SHA-256(SerializeCanonical(0x14 ‖ tuple))`) + `EnvironmentFingerprint.CreateStage0MonoCertified(monoRuntimeVersion)` (v1.3) — a genuine, non-placeholder fingerprint from the Option-A fields + the §4.8.3 Required Stage-0 flag values; golden vector + determinism/sensitivity tests in `DeterministicSimTests`.

**Still host-blocked (Stage-1+ / cert-run, NOT done here):** (a) the §4.8.2 runtime MXCSR validation (query live float-mode flags at match start, reject on mismatch) — needs native interop on the pinned host; (b) the certified capture — supplying the real Mono runtime version and running on the pinned Windows/Unity/Mono host (`cert-run-runbook.md` P2), unrunnable in the current Linux/no-Unity environment. The recorded tuple already uses the pinned Stage-0 flag values, which is exactly what (a) validates against.

**Update (August 3, 2026) — both host blocks above have since cleared; the paragraph is retained as the July-19 record.** (b) cleared **July 19, 2026**: the certified capture ran on the pinned host (commit `819f9d1`) — the FR-PO-052 100-run perf baseline was promoted PENDING → CERTIFIED and the platform-determinism KAT passed byte-exact (44 passed / 0 failed / 4 documented Stage-0+1 deferral skips), flipping `certification-platform.md` to v1.4 **✅ PINNED** and clearing `cert-run-runbook.md` P1/P2. (a) cleared **July 22, 2026**: the §4.8.2 runtime MXCSR gate code landed July 21 and the compiled plugin + certified live read landed July 22. The "Current blast radius" paragraph above is likewise a July-19 assessment — its "independently blocked (no Unity host; `certification-platform.md` is `⏳ RECERT REQUIRED`)" clause no longer holds. **What remains is unimplemented code, not host access:** `SaveManager` still writes `headerOut.Fingerprint = null` (`src/deterministic-sim/SaveManager.cs:209`), so the fingerprint is still not wired into the save path. Tracked in the root `CLAUDE.md` OPEN ISSUES. Severity is unchanged at Medium — this update records that the blocker's *nature* changed, and closes no part of the ERR.

---

## ERR-021-005 … ERR-021-007, ERR-012-007 … ERR-012-009, ERR-008-012: #23/#24/#25 approval back-props (July 10, 2026)

**Specs:** Tactical Instructions #21, Positioning AI #12, Decision Tree #8 (targets); Dismarking AI #23, Build-Up Structures #24, Positional Rotations #25 (owners)
**Severity:** Medium (all seven)
**Detected During:** Not defects — these are the planned cross-spec amendments each owning spec's §2.3/§2.4 pending-ERR table declared for filing "atomically with `APPROVED`" (pipeline step 6), following the ERR-014-001 / ERR-015-002 precedent.
**Status:** ✅ All seven filed and RESOLVED July 10, 2026, in the same commit as the #23/#24/#25 `SPEC_INDEX.md` status flips.

**Amendments landed:**

1. **#21 `TeamTactic` field appends (ERR-021-005/006/007):** `DismarkIntensity` (#23), `BuildUpStructure` (#24), `RotationFreedom` (#25) added to the §2.2.1 field table and the Appendix B canonical snapshot order, appended after `MarkingOrientation` in pinned approval order #23 → #24 → #25 (the #24 §2.2.1 append-order coordination rule; all three approved in one pass, so the order is pinned in #21 Appendix B and mirrored in each owning spec's Appendix B). All three are zero-value identities (`Off`/`None`/`Off`), so `TeamTactic.Balanced` needs no non-zero seeding and FR-TI-031 default-behaviour identity is preserved. Serialization is deferred: each field enters `WriteTeamTactic` with its own `SNAPSHOT_SCHEMA_VERSION` bump only when its owning spec's wiring stage lands (the `MarkingOrientation` 10 → 11 pattern). Files: `tactical-instructions/section-2.md` v0.5, `tactical-instructions/appendices.md` v0.5.
2. **#12 pipeline amendments (ERR-012-007/008/009):** new `positioning-ai/section-3.md` §3.7.1 records the Stage-1 stage insertions against the §3.7 step list — the #24 build-up overlay between `ContextModifier` and spacing, the #23 dismark offset between spacing and the pitch clamp (FR-DM-008), the combined order `anchor → offset → ContextModifier → build-up overlay → spacing → dismark offset → pitch clamp → lines → lanes` (pinned jointly in #23 §4.2 / #24 §4.2; second implementer adds the shared stage-order test), the #25 `RotationController` pre-composition tick position with its serialized `LastComposedTarget` cache, and the **`SlotIndex` single-writer contract amendment**: `AgentPositioningData.SlotIndex` is no longer immutable after `SeedFromFormation`; the `RotationController` is its sole post-seed writer (#25 §4.4 — the amendment the design supplement ranked riskiest, now an explicit documented invariant). Numbering note: ERR-012-004..006 were deliberately skipped — the June-13 dotnet-CI quarantine adjudication proposed (and section-3.md v0.5 already cites) the ERR-012-003..006 cluster, of which only -003's citation is live; reusing -004..006 here would collide if that cluster is ever formally filed. File: `positioning-ai/section-3.md` v0.6.
3. **#8 scorer row (ERR-008-012):** `decision-tree/section-3-2.md` §3.2.2.1 gains the back-prop anchor note placing the FM-DM-03 marked-pass-target multiplier (`Lerp(1.0, TARGET_MARKED_UTILITY_MULT, targetProx01 × awareness01)`, #23 §3.4) in the external tactical-multiplier product — next to the #21 §3.2 Mentality risk, #21 §3.3 `PlayerTactic` product, and §7.7 rest-defense multipliers — applied after the four §3.2.1.1 components and before the single final clamp (§3.2.1.5 timing unchanged). `DismarkIntensity.Off` ⇒ ×1.0 exactly. #23 owns the formula, constants (`TacticalWeights` per FR-DM-016), and tests. File: `decision-tree/section-3-2.md` v1.5.

All seven amendments are documentation/contract changes only — no code changed in this commit; every inserted stage/multiplier is an identity no-op until its owning spec's implementation lands, preserving byte-identical default behaviour.

---

## ERR-024-001: #24 overlay catalogue keyed to lane values no slot occupies (structural no-op)

**Spec:** Scripted Build-Up Structures #24
**Section:** Appendix A (overlay catalogue) / §3.2 worked example
**Severity:** High
**Detected During:** #23–#26 T0 implementation (July 10, 2026) — authoring `BuildUpOverlayCatalogue.cs` against the real `PositioningAIConstants.Family*` tables.
**Status:** ✅ Resolved July 10, 2026, same commit (freeze-then-amend pattern).

**Problem:** FR-BU-007 addresses catalogue rows by the slot's EXISTING
`FormationSlotRecord.DefaultLine` / `DefaultLane`. Appendix A v0.2 (the PASS-1 M-3 "lane-key
correction") keyed the fullback rows to the wide L/R lanes and the midfield rows to LH/RH — but
every family table records fullbacks at `DefaultLane` LH/RH (half-space), wide
midfielders/wingers/AMs at LW/RW, and central mids/DMs/forwards at C. No v0.2 row key matched any
slot in any family: with a non-`None` dial the overlay stage would have run and displaced nothing
— a silent structural no-op of the spec's entire behavioural payload. Root cause: M-3 verified
lane *geometry* (LB's LateralPct 0.15 → y = 10.2 m sits in the LW bin) but not the recorded seed
values the FR-BU-007 key actually uses (the #12 tables deliberately seed fullbacks as half-space —
a data-vs-geometry divergence inside #12 itself that the key inherits).

**Resolution:** Appendix A v0.3 + §3.2 v0.3 re-keyed every row to the recorded values —
BackThree: (DEF, LH/RH) fullback tuck + (MID, C) central drop; DoublePivot: (MID, C) pivot +
(ATT, C) link drop; InvertedFullBacks: (DEF, LH/RH) inversion. Magnitudes and row intents
unchanged (the `[GT]` shapes stay as reviewed). `BuildUpOverlayCatalogue.cs` v1.0 implements the
corrected keys; `BuildUpStructureTests.Catalogue_RowKeys_HitEveryFamily_Err024001Regression`
mechanically locks that every `FormationFamily` receives at least one non-zero own-third offset
per structure, so a future table/key drift of this class fails the suite immediately.

---

---

## ERR-022-001, ERR-027-001: off-pitch domain-tag / subsystem-ordinal back-props (July 22, 2026)

Two off-pitch determinism allocations that had landed in **code** but were never recorded in the
#16 §3.4 spec text or this log. Both are pure namespace allocations — no `DETERMINISM_DIGEST_VERSION`
bump, matching every other §3.4 tag row.

1. **Living World #22 (ERR-022-001):** `DOMAIN_TAG_LIVING_WORLD = 0x1E` + `SubsystemOrdinals.LivingWorld
   = 80` opened the off-pitch subsystem-ordinal band (80–99, disjoint from the match
   Physics/Mechanics/AI bands) with #22's slice-3 `world.text` wiring. The code (`DeterministicSimConstants`
   / `SubsystemOrdinals`) had it since July 2, 2026; the §3.4 spec-text row and this ERR were filed
   retroactively so the table is honest about `0x1E` being taken (a future "next-free-after-the-table"
   reader would otherwise have re-grabbed it).

2. **Squad/Player Data Layer #27 (ERR-027-001):** `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` +
   `SubsystemOrdinals.PlayerDatabase = 81` (next after `LivingWorld`), the deterministic
   `RosterGenerator` roster-generation stream (siteId `player-database.roster-generation`,
   `entityId = clubId`; a boot / off-match-tick draw site). Filed as part of #27's promotion
   review to confirm the Appendix A `[CROSS]` cross-cite (the R-03 gate).

**Resolution:** `deterministic-sim/section-3.md` §3.4 gains a `DOMAIN_TAG_LIVING_WORLD` (`0x1E`) row
and a `DOMAIN_TAG_PLAYER_DATABASE` (`0x1F`) row, each citing its off-pitch subsystem ordinal and its
resolving ERR. The #27 Appendix A `DOMAIN_TAG_PLAYER_DATABASE` / `SubsystemOrdinals.PlayerDatabase`
`[CROSS]` rows are now a confirmed cross-cite against §3.4.

---

## ERR-028-001: Player Progression & Lifecycle #28 back-prop — promote `_RESERVED_0x20_` → `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` (July 23, 2026)

Filed at #28's section-file approval. The July-22 v1.0.8 pass had left `_RESERVED_0x20_` as a
reserved-pending-promotion placeholder for #28 (the roadmap §6 contiguous-block reservation, because
Season Loop #30 (Wave 1) reached the catalogue before #28/#29 (Wave 2)). #28's approval promotes it:

- **`DOMAIN_TAG_PLAYER_PROGRESSION = 0x20`** + **`SubsystemOrdinals.PlayerProgression = 82`** — the
  per-club regen/newgen RNG stream (siteId `player-progression.regen`, `entityId = clubId`, the #27
  `RosterGenerator` per-club-stream pattern; FR-PG-020 / KD-3). Aging/decline/growth of existing
  players is a pure deterministic integer projection and registers **no** stream — `0x20` covers
  regen generation only (#28 §4.3/§5).

**Resolution:** `deterministic-sim/section-3.md` §3.4 replaces the `_RESERVED_0x20_` row with the
`DOMAIN_TAG_PLAYER_PROGRESSION` (`0x20`) row (v1.0.9), citing `SubsystemOrdinals.PlayerProgression =
82` and this ERR. **Like ERR-030-001 (spec-text-first, unlike the code-first ERR-022/027-001):** the
code const + per-club RNG-stream registration land at **#28 T2** with the first regen — registering a
stream with zero draw sites now would be the phantom-surface class FR-LW-031 avoids (the `world.arcs`
precedent). `_RESERVED_0x21_` (Training #29) stays a placeholder until #29 promotes. Pure namespace
promotion; no `DETERMINISM_DIGEST_VERSION` bump. Fully resolves when the T2 code const lands.

---

## ERR-008-013: Decision Tree #8 gains a DT-emitted goalkeeper SAVE action (July 23, 2026)

**Context.** The GK (#11) / Heading (#10) engine integration (`gk-heading-engine-integration-design.md`)
landed the save/header intents fired from **engine-side world-state heuristics**
(`MatchEngine.TryCommitSaveIntents` → `GkHeadingIntentSource.SaveArmed`), listing "a DT-driven
GK/heading decision layer" as future work. The #11 `SaveIntent` doc, however, already states the intent
is "committed by the Decision Tree at the 10 Hz tactical tick" — i.e. #8 was always meant to own the
save decision. This ERR files the #8 change that realizes it, for the **SAVE** case (a DT-emitted
HEADER is deferred — ordinal 8 would overflow the 3-bit composure-noise field and force a rebaseline).
Governed by `docs/tracking/gk-heading-dt-producer-design.md` (outline + detailed plan, each
AR-converged; implementation AR-6 clean).

**The change (additive, off-ball-branch-only, opt-in-gated).**
1. `ActionType.SAVE = 7` — the last ordinal that fits the 3-bit `ActionSelector.ComputeOptionNoise`
   field. Ordinals 0–6 unchanged (no composure-noise rebaseline).
2. `TacticalContext.SaveAvailable` (bool; zero value `false` = identity) — set only for the threatened
   keeper, only under `MatchEngine.EnableGkHeading()`, from `GkHeadingIntentSource.SaveArmed`.
3. `OptionGenerator.GenerateOffBallBranch` short-circuits to **SAVE alone** when `SaveAvailable`, so
   the keeper's save is selected robustly (independent of composure noise / mentality / role tiebreak
   — a must-happen, geometry-gated action must not depend on out-scoring INTERCEPT, which can reach the
   utility ceiling under an aggressive per-agent tactic).
4. `UtilityScorer.ComputeUtility` scores SAVE = `U_BASE_SAVE` and **exempts SAVE from
   `PlayerTacticActionMultiplier`** — that multiplier indexes the #21 `RoleWeightModifiers` /
   `TempoActionBias` tables (7-wide, ordinals 0–6) by the action ordinal, so scoring `a = SAVE(7)`
   without the exemption reads out of bounds. **No #21 table is widened.**
5. `IDtSaveDispatch` seam (primitives only) + `ActionDispatcher` SAVE case + `DecisionTree` ctor param;
   `MatchEngine.HostSaveDispatch` maps agent→GK slot, applies the v18 per-episode latch, projects
   `PlayerAttributeProjection.ToGoalkeeper`, and commits the same Stage-0 `SaveIntent` the removed
   heuristic built. `MatchEngine.DriveGkHeadingTactical` drops `TryCommitSaveIntents`.

**Determinism / scope.** No `SNAPSHOT_SCHEMA_VERSION` change (SAVE reuses `AgentAction.Type`/
`TargetPosition`; no new serialized field). Flag-off is byte-identical (SaveAvailable false ⇒ off-ball
branch untouched, SAVE=7 never enters the noise field, the `!= SAVE` guard is always-true so
`PlayerTacticActionMultiplier` runs identically). Flag-on differs from the pre-change heuristic only in
the keeper's serialized DecisionTree `LastAction` (now SAVE) — expected, KD-11 non-neutral. **Full
dotnet gate PASSED, 0 failures.**

**Resolution.** `decision-tree/section-3-1.md` (§3.1 SAVE generation — off-ball, `SaveAvailable`-gated,
sole-option) and `section-3-2.md` (§3.2 `ScoreSave` + the `PlayerTacticActionMultiplier` SAVE
exemption) gain concise ERR-008-013 back-prop anchor notes (the ERR-008-012 anchor-note precedent — the
formula/behaviour is owned by this ERR + the code, the section note points to it). The `ActionType`
enum member (§2.2.1) and the dispatch seam (§3.5) are described here; their section files carry the
note by reference. Additive to an APPROVED spec via the established ERR-008 back-prop pattern.


---

## ERR-008-014 / ERR-008-015: the Decision Tree could neither fetch a resting loose ball nor finish an action (July 26, 2026)

Both were found by running the composed match engine while landing §5.Z Phase H (roadmap A4b), and both had
been latent since #8 was implemented. Neither was reachable before ERR-030-014's possession bootstrap,
because a match in which the ball never moved never produced a resting loose ball and never dispatched a
pass.

**ERR-008-014 — no loose-ball collect.** Ask "which #8 action sends an agent to a ball lying still in
space?" and the answer is none. §3.1.7 MOVE_TO_POSITION goes to the formation slot; §3.1.8 PRESS needs an
opponent target; §3.1.9 INTERCEPT rejects the ball outright at `ballSpeed < INTERCEPT_MIN_BALL_SPEED`, and
even without that gate its `MAX_INTERCEPT_TIME` feasibility cap bounds it to roughly ten metres. Composed,
play stopped permanently the first time a pass died in space: measured, the ball rested with the nearest
agent 13.75 m away and all 22 agents settled onto their formation slots around it for the remaining 27 000
ticks.

The gate itself is left **exactly as it was** — it has a real job (no slow ball should reach the look-ahead
geometry, which is meaningless at v ≈ 0: every projected point is the ball's own position, and the
`MAX_INTERCEPT_TIME` cap then makes a ball beyond ~10 m un-chaseable by anyone). Loosening it to *a slow
ball is intercept-eligible only while LOOSE* was considered and **rejected**, because it would make every
off-ball agent eligible to chase a resting ball — the converge-and-dither failure that design point 1 below
exists to prevent. Instead the loose case is routed to a dedicated collect that skips the look-ahead
geometry and carries feasibility 1.0, because for a stationary ball being the designated player IS the
feasibility. Accepted consequence: a loose ball between the host's pickup gate
(`FIRST_TOUCH_MIN_BALL_SPEED_M_S`) and `INTERCEPT_MIN_BALL_SPEED` is claimable by nobody for the fraction of
a second it takes to decelerate below the lower gate — transient and self-healing, since drag only ever
carries the ball DOWN through that band.

Two design points are load-bearing:

1. **Sole option, not a competitor.** ERR-008-013's AR-4 already established the principle for SAVE: an
   action that must happen cannot be left to out-score alternatives under composure noise. It applies here
   with measurements — the collect scores ~0.35 against MOVE_TO_POSITION's ~0.21 on neutral attributes, a
   gap of 0.14 that sits inside the ±0.15 noise band. Emitted as a competitor, the designated collector
   visibly flip-flopped between chasing the ball and returning to its slot and never covered the last few
   metres.
2. **The HOST designates, not the tree.** The first implementation used a perception-derived rule ("commit
   only if no teammate I can see is closer"). It deadlocked anyway, with the ball 4 m from a **sent-off**
   agent that eleven teammates were all deferring to — a red-carded agent is never dispatched an action and
   so never moves, and perception has no participation flag. Only the host knows who is sent off.
   Architecturally this is also the right home: it is a team-level role assignment from team state, the same
   class as Pressing AI #13 selecting one primary presser from the whole team snapshot.

**ERR-008-015 — nothing ever completed a PASS or SHOOT.** §3.7.2 is explicit that PASS/SHOOT hold EXECUTING
and that "completion arrives via `NotifyActionComplete`", but the spec never says *who* calls it, and
nothing did — the method had zero production callers. The possession-changed consumer interrupts only the
NEW holder, never the passer. So an agent that passed was frozen in EXECUTING for the rest of the match,
issuing no decisions and no movement commands; if it still held the ball it could never release it, which
on its own re-created the ERR-030-014 deadlock a few minutes after kickoff. A **rejected** `Execute` was
strictly worse: §3.5.2 has the dispatcher deliberately not inspect the result, so the tree entered
EXECUTING with nothing in flight and no completion could ever arrive.

The obligation belongs to the composition root, which is the only layer that holds both the trees and the
executors. One rule covers completion and rejection: *a tree waiting on an executor that is not running has
nothing left to wait for.* #8 exposes `IsAwaitingExecutorCompletion` so the continuous-vs-blocking rule
stays in one place (`DecisionTreeStateMachine.IsContinuousAction`) rather than being re-implemented
host-side — the parallel-surface class this project keeps having to fix.

**Resolution.** `decision-tree/section-3-1.md` (§3.1.14 loose-ball collect) and `section-3-6-to-3-8.md` (§3.7.2
completion obligation) gain concise back-prop anchor notes, per the ERR-008-012 / ERR-008-013 precedent —
the behaviour is owned by this entry plus the code, and the section notes point here. Additive to an
APPROVED spec via the established ERR-008 back-prop pattern. No `SNAPSHOT_SCHEMA_VERSION` change; the new
`TacticalContext.LooseBallCollector` fact is rebuilt each AI tick and never serialized.

---

## ERR-030-016 .. ERR-051-xxx — the ten-spec approval wave (July 27, 2026)

**Filed and RESOLVED atomically with the `IN REVIEW → APPROVED` flip of #53, #35, #46, #36, #54, #47,
#48, #50, #51 and #39.** Every entry below is **spec text only** — no code changed, no format version
bumped *today* (three are ◑ spec-text-first, with their bump named at a future T-phase), and no gate run.

**Two of the ten file nothing:** **#48** and **#39**. Both record the absence in their own §8.2 as a
positive property — a spec that consumes contracts rather than amending them sits correctly in the layer —
and #39 is the stronger case, since the spec that gates the project's ability to ship amends no approved
text anywhere.

### `ERR-030-025` is a REASSIGNMENT — the collision class recurring live

**#46's match-item projector seam was authored as `ERR-030-015`**, verified free against this log at the
time. While that work was open, **#30's own T3 landing (roadmap A5) claimed `-015` on main** for the §3.5
calendar-rebuild fix — a High-severity entry with code behind it, already cited in
`path-to-playable-roadmap.md` and `file-manifest.md`. **Main's claim has precedence**; #46's seam is
**`ERR-030-025`**.

This is the **fourth** instance of the id-collision class in one day, and the first between a branch and
main rather than between a supplement and the log. It sharpens the finding: verifying an id free **at
authoring** is not sufficient, because the log moves underneath an open branch. **The check must be
re-run at merge**, not only at promotion — a proposed id is not a reservation, and neither is a verified
one.

### The #30 tick-order reconciliation — ERR-030-022 (filed by #35)

**#30's pinned day-advance order was not implementable as written.** `ERR-030-007` was filed **twice**:
once at #42's approval for the academy step, once at #32's approval for the scouting step. Both took
"step 7", both pushed `AdvanceDay` to "step 8", and the merged text carried **two step 7s, two step 8s and
an orphaned `AdvanceDay` comment line**. Six approved specs cite these numbers.

Reconciled in a new **§3.3.1**: #32 scouting → **step 9** (its own rationale asks only for *after staff*),
#35 media expiry → **10**, #54 tenure → **11**, `AdvanceDay` → **12**, duplicate line deleted, FR-SN-034's
enumeration extended.

**The conflict inside this wave, and the judgement made:** `ERR-030-020` (#53) requires the facility step
to precede every same-day consumer of a facility-derived input — steps 2, 4 and 7 — and says to renumber
below it. `ERR-030-022` requires that slots cited **by number** not move. **Inserting a new step 1 cannot
satisfy both.** Resolved by numbering the facility step **0**. A step numbered zero is unusual; a renumber
that silently invalidates six approved specs' citations is worse, and patching all six would edit approved
text to accommodate a numbering preference rather than a design need.

**Errata against this log's own history**, recorded rather than rewritten (historical entries are frozen):
`ERR-030-007` names two different changes, and so does `ERR-030-009` (#45's `JobSecurity` band; #44's §3.4
availability filter). A reader resolving either id will find the ambiguity documented.

**The generalisable process finding:** nothing cross-checks a **proposed** back-prop id against this log.
Three of this wave's supplements proposed ids that had already been filed — #30's own T2 implementation
filed rows on the same day those supplements were written — and were reassigned at promotion
(ERR-030-022/-023, ERR-030-024, ERR-029-003). **A supplement's id is a suggestion to re-verify at
promotion, not a reservation.**

### Filed at #53's approval — Club Infrastructure & Facilities

| ID | Target | Change |
|---|---|---|
| **ERR-034-001** | #34 §1, §3 | Re-attribute *"#40 facilities"* → **#53**. Doc-only; the double-count rule is unchanged and was always correct. |
| **ERR-042-001** | #42 §1, §4 | Re-attribute *"#40 facility spend"* → **#53**'s `YouthFacilities` projection. `AcademyQuality`'s shape, `Neutral` identity and root-assembly pattern unchanged. |
| **ERR-028-002** | #28 §1, §7 | Name **#53** as the facility producer behind #42's academy structure; #28's own out-of-scope position intact. |
| **ERR-040-002** | #40 §1 | Record that **#53 owns facility state** and #40's role is **funding** via the existing transaction path. **No #40 code, constraint, ledger or requirement change.** |
| **ERR-029-003** | #29 §2, §3 | New **FR-TR-005a**: the #53 facility term is a **second root-assembled input** to `ComputeTrainingInput`. Not a #53-returned `TrainingInput` — FR-TR-005 makes #29 that type's sole writer, and that is exactly the second path it forbids. ◑ parameter at #29's Stage-3 tier. |
| **ERR-030-020** | #30 §3.3 | The facilities seam at **step 0** — see the reconciliation above. |

**Four of the six are doc-only producer re-attributions, and together they are why #53 exists**: four
approved specs each consumed a facility model and all four attributed it to **#40**, whose own scope
excludes it. Every consumer was built correctly — value input, explicit neutral identity, assembled by the
root — so #53 fits seams that already existed and invents no design change to prove it landed.

### Filed at #35's / #46's approval — Media & Press, News & Inbox

| ID | Target | Change |
|---|---|---|
| **ERR-049-001** | #49 FR-LC-020 | Generalize `SelectionDraw` from *"the `world.text` reservation"* to **the producer's own deterministic, locale-independent selection value, carried verbatim**. The original named one producer's RNG reservation on a **producer-agnostic** seam, contradicting §7.3, FR-LC-013/014 and FR-LC-005. **Contract-widening only** — #22's binding still satisfies it verbatim. **Load-bearing for #35, #46 and #48.** |
| **ERR-033-003** | #33 §2.2, §3.1 | `HumanSystemsDayInput` gains a **producer-agnostic** `ExternalDeltaPermille`, **summed across producers and clamped by the root**. **Filed jointly by #35 and #46**, superseding #35's per-producer `MediaDeltaPermille`: a second producer arrived before the first was approved, and producer three would have needed a third field on an approved struct. Transient struct ⇒ **no format bump**; `0` ⇒ behaviour-neutral. |
| **ERR-033-004** | #33 FR-HS-024, §3.3 | State that *"#46's man-management seam"* **is** the routed delta, **not** a #46-callable mutator — closing the reading under which #46 assigns `MoralePermille` directly and contradicts FR-HS-002. No behaviour change; it makes the only coherent reading the only available one. |
| **ERR-033-002** | #33 FR-HS-027 | Roster-lifecycle lockstep extended: a **pending routed delta** is dropped with the player's entries, so an undelivered delta cannot outlive its subject and land on whoever next holds that `PlayerId`. |
| **ERR-030-023** | #30 §3.3, §3.4 | The #35 media seams — the conference **queue** at `EmitMatchOutcome` and the **drain** at tick step 3. Filing only the first would produce recorded-but-never-delivered deltas with every #35-local test still green. |
| **ERR-030-024** | #30 §3.3 step 3 | Generalize the drain to iterate **every** external-delta producer, summing and clamping. |
| **ERR-030-025** | #30 §3.4 | The #46 **match-item projector** null seam. Filed in #46's own right rather than shared with #35's conference queue: sharing would make #46's most basic item type depend on **#35 being approved**. Same site, so the two coalesce into one hook if both land — **and if #35 never lands, #46 still works.** |

### Filed at #36's / #54's / #47's / #50's / #51's approval

| ID | Target | Change |
|---|---|---|
| **ERR-030-016** | #30 §3.4 | The resolve→**filter**→configure seam admits **more than one consumer** (#44 suspensions, #36 call-ups). They compose order-independently **because both are removals** — recorded as a property to preserve, since a future **non-removal** filter would need an explicit order. Also names the shared empty-squad floor as a seam-level concern. **#36's only back-prop**, which is the measure of how much of it was already waiting upstream. |
| **ERR-045-002** | #45 FR-BD-012, FR-BD-005a | **Re-point the sacking decision from #30 to #54.** The MUST named #30, which contains no sacking rule and never did. #45's posture — no sacking API, no terminating event — is **unchanged and still correct**; only the counterparty was wrong. Also **confirms mid-career pair insertion**, which #54's appointment path needs. |
| **ERR-030-021** | #30 FR-SN-013b, §3.3, §3.5 | (i) The **tenure seam at step 11** and the `(b'')` boundary insertion point; #30 supplies seam and ordering, **#54 decides**. (ii) `ManagedClubId` becomes an **explicit optional** — an unemployed manager is otherwise structurally unrepresentable, since the constructor throws when the id is not in the club set. ◑ the representation change and its `SEASON_STATE_FORMAT_VERSION` bump land at #54 T2, **to be combined with `ERR-030-009`'s queued bump on the same block** so existing saves face **one** refusal boundary rather than two. |
| **ERR-030-017** | #30 Appendix B | The outer frame composes an **optional** authored-database sub-blob, present **only** for an authored game — **no block, not an empty one** — with the flag and the block required to agree in both directions, failing loud. |
| **ERR-030-018** | `season-save` / `League` | An **authored-source factory** for `League` (`Club[]` + `Squad[]` in, **no strength ramp**), with ascending-unique-id and one-squad-per-club guards. `League`'s constructor is `internal` to `season-save`, so it must live there; #47 supplies values and the root calls it. A `League` built this way is **`ISquadProvider`-identical** to a generated one. **Code-side obligation at #47 T1** — recorded here, no code today. |
| **ERR-030-019** | #30 Appendix B | The **`SaveOriginStamp`** (`WorldGenerationVersion` + `BuildId`) in the **outer frame**, before any length-prefixed blob, carrying a `SEASON_SAVE_FORMAT_VERSION` bump at #50 T1. **Frame placement is load-bearing:** #50's classifier reads version fields without parsing a sub-blob, and a stamp inside the season block would defeat that. `BuildId` is **diagnostic only** and must never be a migration input. |
| **ERR-027-003** | #27 §1.2.1 | Record that the **generation contract is save-visible without being saved**: rosters are regenerated from the world seed, so `RosterGenerator`'s draw order and field budget, `LeagueBootstrap`'s catalogue and its strength ramp are under `WORLD_GENERATION_VERSION`, and changing any post-ship needs a bump **plus a generation migration**. The golden vector remains the **CI** guard against an accidental change; this is the **runtime** guard it never was. |
| **ERR-048-001** | #48 KD-4, FR-MP-027 | **Correct a contradiction between two MUSTs inside an APPROVED spec.** FR-MP-025 forbids `#51 → #48`; FR-MP-027 required #51's catalogue to be keyed on #48's `CueId` — **jointly impossible**, and it would have surfaced as an **assembly cycle** after both specs were approved. `CueId` is #48's semantic event identity; **#51's catalogue is keyed on its own `CueKey`**; the **shell's `ICueSink` adapter holds the mapping**. Ordinal stability **retained with a stronger rationale**. **Text-only.** |
| **ERR-038-004** | #38 new §4.4.1 | **#38 owns the one client-local settings store** — location, fragment registration, failure policy — with #49/#38/#48/#51/#39 contributing fragments. Filed because **five specs named this store and none owned it**, and two approved specs both described the audio-levels state. Policy is **reset-to-defaults-and-continue**, deliberately the inverse of #50's refusal (a corrupt preference byte must not block launch), which also places the store outside #50's migration scope. FR-UI-022 unchanged. |

### What was deliberately **not** done

- **No `DETERMINISM_DIGEST_VERSION` bump**, and **no #16 §3.4 change of any kind** — none of the ten
  registers an RNG stream, a domain tag or a `SubsystemOrdinal`, so there is not even a `_RESERVED_` row
  to file. Four of them (#46, #48, #50, #51) additionally have **nothing to promote later**: a future
  stochastic surface in any of them would need a **fresh** allocation.
- **No format version bumped today.** Three entries are ◑ spec-text-first with a named future bump
  (ERR-030-019 at #50 T1, ERR-030-017 at #47 T1, ERR-030-021(ii) at #54 T2).
- **No code, and no gate run** — nothing compiled changed.
- **The duplicate historical rows were not rewritten.** `ERR-030-007` and `ERR-030-009` each name two
  changes, and #30's section files carry duplicate v0.7/v0.8 history rows. These are frozen records;
  they are documented as errata in §3.3.1 instead.

---

---

## ERR-011-002 / ERR-011-003 / ERR-011-004: Goalkeeper Mechanics #11 — the save pipeline was unreachable in production

**Filed:** July 27, 2026. **Status:** ✅ code-resolved same day; spec-text back-prop pending #11 owner
sign-off. **Owner document:** `docs/tracking/goalkeeper-save-pipeline-design.md`; match-engine
`§5.Z.17`.

**How found.** Measurement, not review. `match-engine-design.md` §5.Z.15 recorded the next lever on the
engine's goal rate as *"the quality of the goalkeeper's save"*. A new env-gated instrument
(`GkSaveDiagnosticTests`, `TD_GK_DIAGNOSTIC=1`) walked the save pipeline as a funnel over three full
90-minute matches and found the premise false: across all six keeper-matches the goalkeepers made
**zero** hand contacts with the ball. Not poor saves — none. Three independent defects, each on its own
sufficient to prevent a save.

**Note on numbering:** `ERR-011-001` was taken in May 2026 by the `DOMAIN_TAG_GOALKEEPER` allocation.
These ids were verified free against this log before assignment — the id-collision class the root
`CLAUDE.md` records from the July-27 promotion wave.

| ID | Target | Change |
|---|---|---|
| **ERR-011-002** | #11 §3.1 (state machine inputs + `Anticipate` transitions) | **The keeper woke for the wrong end of the pitch, and never stood down.** The orchestrator computed the third the keeper's own team **attacks** and passed it to a state-machine parameter documented as *"the attacking third from the perspective of the **opposing** team (i.e. threatening GK's goal)"* — opposite ends. The name `ballInAttackingThird` reads one way at the call site and the other inside the machine. Compounding it, `Anticipate` had **no exit** but a dive or a rush. Measured: keepers held Anticipate for **76–92% of every match**, entered for the wrong reason. Fixed per §5.Z.12 (*"a pair has two places that must agree; a mirror has one"*) as ONE signed distance to the keeper's own goal, with both predicates derived from it and renamed from the **keeper's** perspective (`ballThreateningOwnGoal` / `ballSafelyUpfield`); `Recovering → Resting` re-anchored to "play is at the far end"; new `Anticipate → Set` exit. Post-fix: **11–18%**. |
| **ERR-011-003** | #11 §3.3.4 (dive direction) | **Every dive ever launched had lateral direction exactly 0.** `ComputeDiveDirectionLateral`'s only non-zero branch is gated on `SaveIntent.DeflectionTarget.HasValue`, and the engine's sole producer sets it `null` — so the reach envelope never displaced sideways and the keeper dived straight up on the spot. Measured: mean `\|diveDirectionLateral\|` = **0.000** across all six keepers; closest approach of the envelope to the ball over an entire match **2.75 m short**. The root cause is a conflation: `DeflectionTarget` is where the keeper wants to *put* the ball (§3.5.3), not where it should *dive*. Now derived from the ball — specifically the linear XY interception of where the ball **will cross the keeper's plane**, bounded by a new `[GT] DivePredictionHorizonS`; an explicit `DeflectionTarget` still wins. Post-fix: **1.000**, best miss **−0.07 m**, contacts **0 → 15**. |
| **ERR-011-004** | #11 §3.2 (reaction pipeline entry) | **`OnShotExecutedEvent` had zero callers — in production or in tests.** `_shotDetectedTickMs` therefore stayed 0, the per-frame block that writes `ReactionWindowAchieved` is gated on it being > 0, and the window was permanently 0. Since §3.5.1 blends `quality = α·rawHandling + (1−α)·reactionWindowAchieved` with α = 0.70, that capped quality at `0.70 × rawHandling` — **measured ceiling 0.630** for a *perfect* keeper (Handling 20, zero noise, exact contact point) against `CatchThreshold` 0.78, so **a catch was arithmetically impossible** regardless of positioning, reach or dive accuracy. `MatchEngine.NotifyKeeperOfShot` now fires on the shot's CONTACT frame (§3.2.1 dates perception from the strike), routed to the keeper defending the goal the shooter attacks. The method also gains an attributes parameter: it is frequently the FIRST call of an episode, earlier than `CommitSaveIntent`, which is the only other writer of the per-GK attribute snapshot, so reading that snapshot would have dated the window off a keeper with zeroed Reflexes (the KD-P4 convention — the composition root owns the projection). |

**Determinism impact: none.** No `SNAPSHOT_SCHEMA_VERSION` change (every field involved is already in
the v19 GK block, and the state machine's inputs are recomputed each tactical tick), no new RNG stream,
domain tag, subsystem ordinal or draw site, and — load-bearing — **no change to the draw order**: the
fixes alter the *arguments* to existing draws, never how many are taken or in what sequence.

**Test fallout, recorded because it is a recurring class.** `sim_goalkeeper_save_launch_executes_dive`
had encoded the inverted predicate — it parked the ball at x = 75 precisely because that was what woke
a team-0 keeper pre-fix. Re-anchored to x = 30 with its intent (reach Anticipate, launch one dive,
miss, mutate no ball state) exactly preserved. This is the Phase-H *"21 existing tests updated — most
encoded the old contract"* class.

**What this does NOT close.** The §5.Z.15 lever is discharged and the goal rate did not move at all
(15.3 → 15.3 per match). The residual is the shot side, recorded in the owner document §7 and not fixed here: shots
that essentially cannot miss the goal (aim is 0.732 m inside the post, `finalDirection.z` is never
read), **no crossbar** (`BallCollision.CheckBoundaries` gates every boundary test behind z < 0.22 m),
and **no blocked shots** (`BallCollisionHandler.OnAgentCollision` is an empty `TODO` that production
calls). Each belongs to a different APPROVED spec and needs its own pass.

---

## ERR-011-007 / ERR-012-010: the keeper contact rate — the dive launched the moment SAVE committed, and the GK slot could not track the shot line

**Filed:** July 28, 2026. **Status:** ✅ code- and spec-resolved same day (#11 `section-3.md` v0.5; #12 `section-3.md` v0.7 + `section-6.md` v0.4).
**Owner document:** `docs/tracking/gk-contact-rate-design.md`; match-engine `§5.Z.22`.

**How found.** §5.Z.20 fixed the catch/parry conversion and measured its own residual: a contact
almost always stops the shot, and the keeper contacted only ~a quarter of on-target shots — so the
contact RATE bounded everything conversion could recover (goals/shot 0.19–0.26 vs football's
~0.10). Its §7.1 anatomy was frame-aggregated; the new per-episode instrument
(`GkContactRateDiagnosticTests`) classified every goalward threat episode at the ball's actual
goal-plane crossing over 3 full matches: of 15 crossed un-contacted episodes, **9 dive-early**
(dive over 456–2000 ms before the crossing), 3 no-dive, 3 lateral-miss, **0 dive-late** — the
commit was never slow, always too eager; and the lateral need at the crossing ran 1.91–3.83 m
against ~3.55 m of total dive coverage from a slot that moved the keeper at most ±2 m.

**ERR-011-007 (the spec is the defect).** #11 §3.1.1's `Anticipate → Diving` row was
unconditional on `SaveIntent`, so the dive — a fixed `DIVE_PHASE_DURATION_MS` (600 ms) envelope —
launched at the first 10 Hz tick after the DT's SAVE and closed during the ball's 925–2006 ms
flight. New §3.3.6: the transition gates on the ball's predicted time-to-plane against a commit
lead scaled to the predicted lateral need (`clamp(need / DIVE_LAUNCH_DISPLACEMENT_M,
DIVE_COMMIT_MIN_LEAD_FRAC, 1) × duration`), so the envelope reaches the predicted crossing offset
as the ball arrives. The crossing predictor is extracted to ONE shared derivation
(`GoalkeeperDiveKinematics.TryPredictPlaneCrossing`) consumed by both the §3.3.4 dive direction
and the gate — direction and timing cannot drift apart. §3.2.3's frozen-window `elapsed` anchor is
refined from the dive-launch frame to the keeper's FIRST DECISION OPPORTUNITY at/after the live
stamp — `max(SaveIntent.AttemptCommittedTick × tacticalTickMs, ceil(stamp / tacticalTickMs) ×
tacticalTickMs)`: under a held dive the launch is deliberate timing, not reaction (the launch
anchor would have re-clamped the window ERR-011-005 fixed), and under the hold the shot is
usually struck AFTER the intent commit and re-stamps the episode (ERR-011-006's overwrite), so
the bare commit anchor read seconds-negative — the first full-corpus run measured the window
collapsing to 0.000–0.084 before the max() form landed it back at 0.34–0.44. Pre-hold all three
anchors coincide within a stride, so §5.Z.20's measured windows stay valid calibration. A ball that stops closing holds rather than diving at nothing; the engine's
`ClearSaveIntent` disarm ends the episode, so the hold cannot deadlock.

**ERR-012-010 (formula shape, not a retune).** #12 §3.3.3's `gkSlot.y = PITCH_WIDTH_M/2 +
GK_LATERAL_FACTOR × basisY(ball.y)` anchored the lateral gain to PITCH width: ±2 m of travel over
68 m, and no `[GT]` value fixes the shape (a factor tracking a close ball drags the keeper out of
the mouth for a far one). The lateral term becomes the ball-line point — the segment from the
ball to the keeper's own goal centre evaluated at the keeper's depth — clamped inside the goal
mouth by `[GT] GK_LATERAL_CLAMP_M` (3.0 m < the 3.66 m half-mouth). `GK_LATERAL_FACTOR` is
retired, not retuned (KD-CR4 — leaving it in place would be the parallel-surface trap). A central
ball reproduces the pre-fix slot exactly, so the existing worked examples and unit locks hold.

**Determinism surface.** Both mechanisms are pure functions of the current tick's ball state and
keeper position: no new cross-tick state, **no `SNAPSHOT_SCHEMA_VERSION` change, no new RNG
stream / domain tag / draw site, no draw-order change** — digests move for any match containing a
save episode, as intended.

---

## ERR-010-002: Heading Mechanics #10 §3.5 delegated the header aim to a system that cannot emit headers, so every header was a passive mirror

**Filed:** August 9, 2026. **Status: RESOLVED** (same commit, spec + code).
Owner document: `docs/tracking/close-chance-creation-design.md` §10.6 item 3 (which recorded the
symptom and mis-stated its consequence — corrected in the same commit).

**The defect.** #10 §3.5 stated, verbatim: *"The intended launch direction (toward `targetIntent`)
is realized by the upstream choice of `contactPointIntent`: Decision Tree #8 selects a contact point
on the head surface such that the reflected vector points at the target."*

Decision Tree #8 **cannot emit a header at all.** `ActionType` ordinal 8 overflows the 3-bit
composure-noise field, which is why DT-emitted HEADER is deferred as wiring-backlog **W9**. The
producer of every `HeaderIntent` in the game is, and for the whole of Stage 0 has been, the
match-engine proximity trigger `MatchEngine.TryCommitHeaderIntents`, which supplied
`ContactPointIntent = Vector2.zero` and a fixed `TargetIntent`.

So the aim was delegated to a system that structurally could not make the decision, and therefore
nobody made it. **This is the `ERR-011-010` shape exactly** — the same finding, one spec over: §11
§3.7 delegated the keeper's rush decision to #8, which has no keeper model and cannot acquire one,
and the condition sat unowned for ten weeks.

**Three defects, one chain.** The consequence is worse than "the aim is a fixed point", which is how
`close-chance-creation-design.md` §10.6 item 3 recorded it:

1. **`TargetIntent` reached no formula.** Verified by exhaustive grep: its only production uses are
   `HeadingMechanics.ClampToPitch` and the snapshot serializer. `ContactPointIntent` reached exactly
   one read — §3.4's `pointError` — and never the geometry, because `contactPointActual` was
   recomputed from ball-vs-head geometry. The outgoing direction was therefore pure specular
   reflection about `normalize(ballPosition − headCentre)`: **a header was a passive bounce and the
   player had no influence on where the ball went.** Correcting §10.6's recorded consequence: a
   defender clearing in his own box did NOT aim 90 m at the far goal — he headed the ball back the
   way it came, which is worse football and a different defect. Neither intent field had ever been
   exercised anywhere in the tree, including in #10's own `HeadingScenarios` fixture, which also sets
   `ContactPointIntent = Vector2.zero`.
2. **The contact point had two independent derivations.** `HeadingMechanics.Update` Pass 1 (quality)
   and Pass 2 (execution) each computed it from ball-vs-head geometry in separate code. They agreed
   only by coincidence — the parallel-surface trap this log has filed repeatedly, most recently as
   the T2-H3 `LineupSelector.CanSelect` finding. Now one `ResolveContactGeometry` owner, read by both.
3. **A header could not lift the ball.** Pass 2 rebuilt the world-space contact point from its **2-D**
   head-local projection (`+x` facing-forward, `+y` agent-left — both horizontal), which pins
   `contactPointActual.z` to the head centre's z. The §3.5 reflection normal was therefore
   permanently horizontal, and for a horizontal normal `reflected.z = v̂_in.z`: **a descending ball
   was headed further down.** Every cross dropping onto a defender's head was deflected into the
   turf. This was introduced by the AR-3 M-1 fix, which correctly stopped the lateral offset being
   injected as height and, in doing so, removed the vertical component altogether; that fix is
   preserved — the lateral term still maps to the agent-left axis — while the 3-D point is now
   carried directly instead of round-tripped.

**The resolution.** New #10 **§3.5.1**, and `src/heading-mechanics/HeadingAim.cs`. Three pure steps:

- **Ballistic launch direction** to `targetIntent` at the speed a perfect contact would carry. A
  destination is reached by an arc, not a straight line — aiming along the straight line to a distant
  ground point heads the ball into the turf. The **low** root is taken (a header is a driven contact,
  not a lob). `disc < 0` — the target beyond ballistic range — degrades continuously to the 45°
  maximum-range launch rather than failing (**P1**, continuous never a cliff), which is the ordinary
  case for a defensive clearance and is what makes one long and high. Solved at the perfect-contact
  speed deliberately: solving at the achieved speed would be circular, since achieved speed follows
  from quality and quality follows from the aim error.
- **The half-vector normal** that realizes it, `normalize(incident + aimDir)` — the exact inverse of
  §3.5's reflection. `[CORRECTED August 9, 2026, same-day adversarial review of this landing: this bullet
  originally continued "— bounded to the hemisphere the ball can physically reach... outside that
  hemisphere the normal is projected onto the grazing boundary." No such bound exists in the shipped
  `HeadingAim.ComputeAimNormal`, and none is needed: for unit vectors `dot(incident + aimDir, incident)
  = 1 + dot(aimDir, incident) ≥ 0`, so the half-vector is always in the forward hemisphere already, for
  every `aimDir`. A "guard on an unreachable branch" (this file's own recorded defect class) — recorded
  here rather than written into code that would never execute it. The spec text was corrected to match
  at #10 §3.5.1 v0.5, not the code, which was already right. See also the v2.03 chain entry above.]`
- **The achieved normal**, blended from the geometric normal toward the aim normal by normalised
  Heading. **Steer authority 0 is exactly the pre-fix model**, and the ramp spans the whole attribute
  range with no plateau at either end (raw 1 → 0.05, raw 20 → 1.00) — the FULL-RANGE shape settled at
  `ERR-008-019`. The aim is skill (**P2**), not a switch.

`pointError` becomes a genuine **execution** error for the first time: it was previously the distance
between a hardcoded zero and a geometric fact. A header steered hard away from its natural rebound is
now weaker as well as less accurate, which is the football.

**The producer half.** #10 realizes an aim; it does not choose one. The engine does, via new
`GkHeadingIntentSource.HeaderAimTarget` (§4.2a) — the same producer/realizer split `ERR-011-010`
settled for the keeper's rush. The football is one sentence: **the deeper you are, the wider you
clear; the further forward you are, the more you aim at the goal**, as a continuous lerp in the
taker's advancement up his own attacking direction, never a zone switch (**P1**). Constant-free: the
only inputs are position, team, and `[FIXED]` pitch geometry.

`[CORRECTED August 9, 2026, same-day adversarial review of this landing: "§4.2a" was a phantom citation
— no document defined it (this entry cited it, the code cited it, nothing declared it). Now documented
at `docs/tracking/gk-heading-engine-integration-design.md` §4.2a, the file `GkHeadingIntentSource.cs`'s
own header names as its governing document (not `match-engine-design.md`, whose own §4 is an unrelated
"Boot sequence" section). §4.2a also records a measured limitation not stated here at landing: the
target's X is pinned to the opponent goal line, so beyond roughly 15 m of range the §3.5.1 ballistic
solve is out of range on every attempt and the continuous 45°-fallback is the production path rather
than the edge case; and the wide-clearance bias is weak and inverted in lateral position (a team-0
header at (10, 10) aims 4.1° off straight upfield; one at (10, 34) aims 17.9°, the wrong direction for
"clear wider from a central position"). Recorded as a known limitation, not fixed. See the v2.03 chain
entry above.]`

**No new `[GT]`** — the Heading attribute is itself the dial — so this stays inside **KD-W1** while
heading remains unwired. **No `SNAPSHOT_SCHEMA_VERSION` bump**: both intent fields were already
serialized and nothing new survives a tick. **No new RNG stream, no new domain tag, no new draw site,
no draw-order change.** A match containing an executed header would digest differently, because the
number of contacts changes and `HeadingContactQuality` draws twice per contact from the registered
`heading.mechanics` stream, advancing its cursor — but see the measured result below: **no scenario in
this tree contains one.**

**GATE-VERIFIED** (local whole-tree run, August 9, 2026, head `c89c838`): build succeeded, 0 errors. `HeadingMechanics.Tests` **60 passed / 15 skipped / 0 failed** (47 → 60 — the +13 are this landing's `HeadingAimTests` locks, all executed). `MatchEngine.Tests` **447 passed / 1 failed / 10 skipped** — **byte-identical to the pre-fix baseline captured at HEAD before any change**, including the failing predicate's values to three decimals (`sim_match_engine_close_chance`: meanCosine −0.165 vs bound −0.16, goalwardShare 0.407 vs bound 0.42). That failure is the pre-existing C1 fallout awaiting an owner call; it predates this branch and this landing did not move it. Every other suite unchanged; 33 suites, quarantine empty. The gate's overall verdict is FAILED **solely** on that inherited failure.

**CORRECTION, forced by that run — the "digests DO move" claim written at landing is WITHDRAWN as stated.** No measured digest movement occurred anywhere: every acceptance scenario returned values identical to the pre-fix baseline. The mechanism is live, but the population it acts on in these scenarios is at or near zero — consistent with the measured **0.2% header contact ratio** (2 executed, 963 failed over 6 seeds × 90 min), against acceptance scenarios of 4 seeds × 18 minutes which plausibly contain no executed header at all. The defensible statement is narrower: a match containing an executed header WOULD digest differently, and **no scenario in this tree demonstrates one**. This is exactly the pre-implementation hazard the evidence advisor named — at a 0.2% contact rate the aim is not observable as football, so it is locked by unit geometry and by nothing else. Recorded rather than repaired: the header contact rate, not the aim, is what any future measurement of this fix depends on.

**Recorded, NOT fixed** (aim refinements on top of an aim that now exists):
- The attacking target is the goal **centre** — i.e. at the goalkeeper. Aiming away from him needs the
  keeper's position at the producer.
- The target never names a **team-mate**: a knock-down or a flick to a runner is a #8 decision that
  arrives with W9.
- `HeaderIntent.ContactPointIntent` remains on the struct as the W9 DT-supplied override and is not
  read by Stage-0 geometry. The half-vector that realizes an aim depends on the incoming velocity at
  contact, which no producer can know at commit, and KD-4 locks the intent at commit.
- **`HeadingEligibility` freezes the head centre — position AND jump-arc z — at the agent's current
  frame** while sweeping only the ball, so a player running onto a ball is predicted to miss. Not
  touched here; it is the contact model, not the aim.
- **#10 KD-18's aerial-phase gate reads `AgentMovementState.GROUNDED`, which #2 §3.1.2 defines as
  "knocked down"**, not "on the ground". A standing, upright player satisfies "must have left the
  ground". Cross-spec semantic collision, separate ERR candidate, deliberately not folded in here.

---

## ERR-010-003: Heading Mechanics #10's KD-18 aerial-phase gate borrows Agent Movement #2's `GROUNDED` state, which #2 §3.1.2 defines as "knocked down" — not "on the ground"

**Filed:** August 9, 2026. **Status: 🟡 Open — RECORDED, NOT FIXED** (documentation only; no code change
proposed by this entry). Surfaced as a "Recorded, NOT fixed" bullet at the tail of `ERR-010-002`
(same day), which named it "a separate ERR candidate, deliberately not folded in here." This entry is
that candidate.

**The defect.** #10 §3.2's eligibility predicate and §3.3's `jumpStartFrame` derivation both gate on
excluding `{GROUNDED, STUMBLING}` from `AgentMovementState`, and both the spec prose and the mirrored
code comment describe that exclusion as verifying the agent is airborne:

- `section-3.md` §3.2, step 1: *"// (1) Aerial-phase check (KD-18). Stage 0 aerial phase is owned by
  #10. // AM #2 ground state must be exitable (not GROUNDED / STUMBLING)."*
- `section-3.md` §3.3, the `jumpStartFrame` source: *"agent.movementState ∉ { GROUNDED, STUMBLING }
  (i.e. the agent has cleared any preceding AM #2 ground-recovery state)"* — and two lines later,
  *"the agent's ground exit is observed via existing `agent.movementState`."*
- `src/heading-mechanics/HeadingEligibility.cs:54` mirrors it verbatim: *"// (1) Aerial-phase check
  (KD-18). Agent must have left the ground."*

Agent Movement #2 §3.1.2 defines `GROUNDED` as one specific incapacitated substate, not the complement
of "airborne": *"Agent on the ground after fall or heavy collision. Physics: No locomotion. Position
fixed. Recovery timer active. Entry: Collision force exceeds knockdown threshold, OR stumble at extreme
speed."* (`agent-movement/section-3-1-part-2.md:78-86`). The other six states in the seven-state
machine — `IDLE, WALKING, JOGGING, SPRINTING, DECELERATING, STUMBLING(-approaching), GROUNDED`
(`section-1-2.md:154`) — are every *ordinary* thing a footballer's feet do on the ground, and none of
them is named `GROUNDED`. There is no `Jumping` state at all: `section-4.md:49` of #10 itself says so
— *"No `Jumping` state exists; Stage 0 aerial phase is owned by #10 per KD-18 and is invisible to the
AM #2 state machine."* AM #2's own vocabulary confirms the "knocked down" reading independently:
`GroundedReason.DIVING_HEADER` exists specifically because a *header* can put a player into
`GROUNDED` on landing (`section-3-1-part-2.md:97-98`), and `HeadingMechanics.cs:473`'s landing comment
— *"set GROUNDED with DIVING_HEADER if appropriate"* — treats entering `GROUNDED` as a **consequence**
of a header, not a **precondition** for one.

So the check `if agentState.CurrentState == GROUNDED || STUMBLING: not eligible` does not and cannot
establish "the agent has left the ground." A player who is simply standing (`IDLE`), walking, jogging,
sprinting, decelerating, or approaching-a-stumble clears it trivially, because none of those states is
`GROUNDED`, and AM #2 has no Z-axis / airborne state to fail the check against in the first place — by
KD-18's own design, AM #2 publishes no Z>0 kinematics at Stage 0, so "has left the ground" is not a
question this state machine is *capable* of answering. The entire notion of an aerial phase in this
tree is synthesized independently inside `HeadingJumpKinematics` (`jumpStartFrame` → `apexFrame` →
`landingFrame` over the fixed `JUMP_PHASE_DURATION_MS` window), driven purely by elapsed 60 Hz frames
once the gate first passes — never by any position, velocity, or state read that could confirm the
agent's feet actually left the turf.

**What the check actually does (verified, not a no-op, not inverted).** The gate is not vacuous: it
excludes an agent who is currently prone (`GROUNDED`, entered via `HeadingMechanics.cs:195` /
`HeadingEligibility.cs:55` reads of a real collision-knockdown or extreme-stumble state written by
Collision System #3 / Agent Movement #2) or off-balance (`STUMBLING`) from starting or continuing a
header attempt, both at the `HeadingMechanics.Update` `jumpStartFrame`-latch site
(`src/heading-mechanics/HeadingMechanics.cs:192-206`) and on every re-evaluation inside
`HeadingEligibility.Evaluate` (`HeadingEligibility.cs:54-65`). That is a real and sensible exclusion —
a felled or stumbling player should not be able to head the ball — and it fires whenever a collision
or hard stumble coincides with a header attempt. **The defect is entirely in what the check is said to
verify, not in what it verifies.** No inversion (a `GROUNDED` player is correctly excluded, not
included) and no dead code (the exclusion is reachable and is exercised by ordinary collision
gameplay) — contrary to the no-op/inversion hypothesis this entry was filed to check.

**Consequence.** A documentation/spec-clarity defect, not a behavioural one — the same class and the
same severity rationale as `ERR-020-003` (two files using one word, "grounded," for two different
things, each internally self-consistent, with nothing in the tree currently misled by it). The risk is
prospective: a future reader — implementing the KD-18 §7.8 retirement to AM #2 native Z kinematics, or
just auditing the eligibility predicate — who trusts the label ("aerial-phase check", "must have left
the ground") over #2's actual definition could believe a verticality/airborne safeguard exists here
when none does, or could "simplify" the check on the assumption that it duplicates the
`jumpStartFrame`/`landingFrame` timer, when in fact it is the *only* thing standing between a prone or
stumbling player and a header attempt.

**Recorded, NOT fixed.** No code change proposed by this entry (documentation-only per its filing
scope). A real fix is a relabeling exercise, not a behaviour change: rename/re-comment the check at
both cited spec sites and both cited code sites to state what it verifies — "agent is not incapacitated
(not knocked down, not stumbling)" — rather than "agent has left the ground" / "aerial-phase check",
and correct §3.3's "the agent's ground exit is observed via existing `agent.movementState`" claim, which
is false as written (no ground *exit* is observed; only ground *incapacity* is excluded). Whether #10
should gain a genuine airborne signal is a separate design question, out of scope here and moot until
AM #2 grows Z kinematics per KD-18 §7.8.

**Files Affected (citation sites only — no change made):**

| File | Location | What it says |
|---|---|---|
| `docs/specs/heading-mechanics/section-2.md` | FR-HE-001, FR-HE-019 | "the agent is in the Stage-0 #10-owned aerial phase (KD-18)" / KD-18 citation |
| `docs/specs/heading-mechanics/section-3.md` | §3.2 step 1 (line 118); §3.3 `jumpStartFrame` source (lines 202-213) | "Aerial-phase check (KD-18)... AM #2 ground state must be exitable"; "the agent's ground exit is observed via existing `agent.movementState`" |
| `docs/specs/heading-mechanics/section-4.md` | §4.6 60 Hz loop pseudocode (lines 206-211) | `jumpStartFrame` initialization comment referencing GROUNDED/STUMBLING clearance as ground exit |
| `docs/specs/heading-mechanics/section-5.md` | §5.1.1 eligibility truth table (line 27) | correctly distinguishes lowercase "Grounded (`STANDING`)" from backticked `` `GROUNDED` / `STUMBLING` (AM #2) `` — the two meanings are already kept apart here, unlike in §3.2/§3.3/§4.6's prose |
| `docs/specs/heading-mechanics/outline-detailed.md` | lines 314, 666 | same KD-18 framing, same terminology |
| `src/heading-mechanics/HeadingEligibility.cs` | line 54 | `// (1) Aerial-phase check (KD-18). Agent must have left the ground.` |
| `src/heading-mechanics/HeadingMechanics.cs` | lines 191-196 | `jumpStartFrame` latch condition, no comment claim beyond the state names themselves |
| `src/heading-mechanics/HeaderContactState.cs` | line 22 (XML doc) | mirrors the GROUNDED/STUMBLING exclusion condition, no "left the ground" claim |
| `src/heading-mechanics/HeaderIntent.cs` | line 41 (XML doc) | mirrors the exclusion condition in `AttemptCommittedTick`'s doc comment |

---

## ERR-012-011: Positioning AI #12 §3.0 classified phase from the on-ball carrier, so every pass read as a transition

**Filed:** August 8, 2026 — at wiring backlog **C1**. **Status: RESOLVED** (same commit, spec + code).
Owner document: `docs/tracking/match-engine-wiring-backlog.md` §3 C1 and §5 row 2.

**The defect.** #12 §3.0.1 sourced its possession input from "#7 Perception (`EntityId?`, `null` for
loose ball)", and §3.0.2 branched on it directly. The engine's answer to that question is
`_possessingAgentId`, which is cleared at every `ApplyKick` (`ReleasePossessionOnKick`,
`MatchEngine.cs`) and re-acquired only on physical receipt (`RunFirstTouch`'s Controlled branch, or
`RunLooseBallPickup`). So for the **entire flight of every pass** the engine holds no possessor, the
snapshot reported a loose ball, and §3.0.2's `V₀` velocity branch classified a team knocking the ball
around as being in **transition** — `TransToAtk` for the passing team and `TransToDef` for the other.

The spec and the code were both self-consistent; the error is that "who is on the ball" and "which
team has the ball" are different questions and only the first one had ever been asked. Football's own
possession-sequence convention answers the second: a team keeps the ball while a pass it played
travels to a team-mate.

**Measured, pre-fix**, over 6 seeds × 90 min through `CloseChanceDiagnosticTests`: with the ball in
the final third, #12 committed `InPoss` on **7.5%** of samples — `OutOfPoss` 16.7%, `TransToAtk`
58.9%, `TransToDef` 16.9%. Every phase-gated mechanism in #13/#14/#15 is gated behind a state the
engine almost never occupied. (An earlier §5.Z.24 measurement recorded 9.5%; the corpus has moved
since under the ERR-008-021/-022/-023 shot-lane chain, and 7.5% is this landing's own baseline.)

**The fix.** Phase now classifies from **team possession**, composed by the orchestrator as the union
of two engine surfaces: the on-ball carrier's team, else the team of the intended receiver of a pass
in flight, else none. #7 cannot supply this and is not asked to — a pass's intended receiver is an
*intent* held by the executing #5 pass, not a perceived fact.

- **#12 §3.0.1 / §3.0.2 / FR-PA-022** restate the input as the team in possession and make the
  football definition normative. **New §3.0.5** walks a pass between team-mates tick by tick — the
  settled-possession-with-a-moving-ball case the section had no example of, which is how this
  survived. §2.3 splits the old single possession row into the on-ball carrier (still #7's, still
  consumed by §3.3 dismarking's FR-DM-007 exclusion) and the team in possession (the orchestrator's).
- **`PositioningPerceptionSnapshot` gains two fields**, `HasTeamPossession` /
  `TeamPossessionIsOwnTeam`. `PossessionOwnerEntityId` / `...IsOwnTeam` are **unchanged**: redefining
  them would have excluded the intended RECEIVER from the #23 dismark nudge for the whole flight of
  every pass — the one player who most needs to move to receive — and FR-DM-007's stated
  justification ("it is playing the ball") is false of him.
- **The engine gains `_passInFlightReceiverId`**, latched at the CONTACT kick from the
  `PassRequest.TargetAgentId` the executor already holds, and cleared when anybody establishes
  possession, when the intended receiver goes inactive, when any agent strikes the ball, at every
  restart, and when the ball stops travelling toward the receiver. That last rule reuses
  `RunFirstTouch`'s own receding predicate, hoisted to one shared `BallApproaching` — so a pass that
  is overhit, deflected away, or simply runs out of momentum expires with **no new `[GT]` and no
  timeout**, which is what keeps this inside the KD-W1 freeze.

**The V₀ velocity branch, `PHASE_HYSTERESIS_TICKS` and every constant are unchanged.** A shot is
deliberately not possession: on release the shooting team falls to the velocity branch, which is the
right shape for a rebound.

**Determinism surface.** `_passInFlightReceiverId` is cross-tick and **not** reconstructible —
`PassExecutor` never clears its `_request` on the return to Idle, so all 22 serialized executor
states carry a stale last-pass target that nothing dates. It is serialized as the trailing field and
**`SNAPSHOT_SCHEMA_VERSION` goes 19 → 20**. The pre-existing exclusion proof beside
`SerializeWorldState` was extended rather than left asserting completeness. **No new RNG stream,
domain tag, draw site, or draw-order change**; the digest moves for any match containing a pass, as
intended.

**Verification.** `PassInFlightPossessionTests` (12 cases) covers the latch lifecycle with a positive
control on every refusal, the composition mirrored **home and away**, the untouched velocity branch,
and a v20 round trip taken **while a pass is in flight** with an explicit non-vacuity precondition —
a round trip at the latch's default would serialize −1 either way and stay green with the field
deleted. Mutation-tested: reverting the classifier to the carrier kills both mirrored cases; deleting
the v20 field kills the round trip; deleting the shot adapter's clear kills its isolating case.

**Gate: FAILED, and the failure is this fix's own doing.** Whole tree, executed locally: build 0
errors, quarantine empty, every suite green except `MatchEngine.Tests` **446 / 2 / 10**. The two are
`sim_match_engine_close_chance` (`meanCosine` −0.165 against −0.16; `goalwardShare` 0.407 against
0.42) and `sim_match_engine_shot_outcomes` (`fast-balls-deflect-off-bodies`, i.e.
`totalDeflections > 0`, read **0**). **Both PASS at the pre-fix commit `ba4e194`**, verified by
executing them in a worktree, so causation is measured rather than inferred. **Neither bound was
moved.** The close-chance band has already been rebaselined twice this fortnight and a third move
would end its life as a lock; the shot-outcomes predicate is not a band but ERR-003-007's
reachability lock, which C1 drove to zero — no fast ball found a body anywhere in the corpus,
consistent with the measured shape compression. Both are owner decisions and are filed as such.

**Recorded, not fixed — two clears have no isolating lock.** Deleting `ClearPassInFlight()` from the
GK/heading adapter, or from `ApplyRestart`, leaves the whole suite green. For the restart that is
benign and expected: the placed ball is at rest, so the receding rule clears the latch anyway, and
the explicit call exists only so the rule does not depend on another rule's side effect. For the
**GK/heading adapter it is a real gap** — a header played on along the pass's own line does not flip
the receding test, so that clear is load-bearing and unproven.

Investigated rather than assumed, and the reason is worth its own line: **no test in the tree drives
`GkHeadingWorldAdapter.ApplyKick` at all.** `GkHeadingIntentSourceTests` is pure-function; the
`MatchEngineGkHeading*` tests and the scenario pair stop at *intent committed*
(`TestOnly_LastCommittedSaveAttrs`/`...HeaderAttrs`); `GkRushTriggerTests` moves the keeper but a
rush is not a strike; and the only paths that plausibly reach a strike — `GkSaveDiagnosticTests`,
`GkContactRateDiagnosticTests` — are env-gated behind `TD_GK_DIAGNOSTIC=1` and end in
`Assert.Pass(…)`, so they cannot fail even if `ApplyKick` were never called. There is no counter on
that adapter (`TestOnly_ShotContacts` is scoped to the *shot* adapter) and no seam that forces the
contact frame; `TestOnly_DriveGkHeadingTactical` produces intent and `TestOnly_DriveGkHeadingPhysics`
advances one 60 Hz step. A genuine lock needs either an observer on the adapter or an EventBus
subscription, plus a fixture proven to produce contact rather than the `dive-early` / `lateral-miss`
/ `no-dive` outcomes those diagnostics exist to characterize. That is real work and it is **not**
this landing's; filed here rather than left implied, and it belongs in
`match-engine-wiring-backlog.md` as a test-reachability gap in its own right.

---

## ERR-011-010: Goalkeeper Mechanics #11 §3.7 — the rush decision was delegated to a spec that cannot make it

**Filed:** August 4, 2026 — at wiring backlog **W1**. **Status: RESOLVED** (same commit). Owner
design supplement: `docs/tracking/gk-rush-trigger-design.md`.

**The defect.** §3.7's state entry read, in full:

> **State entry.** Decision Tree #8 `RushIntent` with `commitmentLevel > RUSH_COMMIT_THRESHOLD` at
> the 10 Hz tactical tick.

That is a delegation, not a decision, and the delegate cannot accept it. Decision Tree #8 has no
goalkeeper model, and at Stage 0 it structurally cannot acquire one: `ActionType.SAVE = 7` is the
**last ordinal that fits the 3-bit composure-noise field** in `ActionSelector.ComputeOptionNoise`
(§3.3.3), so adding a `RUSH` action would overflow it and force a composure-noise digest rebaseline
— exactly the cost that defers the DT-emitted HEADER (wiring backlog W9).

So the condition belonged to nobody. `GoalkeeperMechanics.CommitRushIntent` had **no caller of any
kind, production or test, from 28 May to 4 August 2026**, while everything downstream of it — the
dispatch, the `Rushing → OneOnOne → Smothered` chain, the abort reasons, the telemetry, the snapshot
serialization — was built, reviewed and tested. Every one-on-one in this engine's history was a
stationary keeper waiting on his line.

**The second half of the defect is the football.** Because the "when" was delegated, the spec never
said what a keeper is *deciding*. That is not a gap a call site can fill by guessing, and the first
implementation of this trigger guessed wrong: it refused to send the keeper whenever any team-mate
was nearer the ball — a "last man" rule. That is not the model. **A keeper comes out to reduce the
shooting angle**, and a defender chasing the carrier down, or wrestling him for the ball, reduces
nothing: the carrier still has a clear sight of goal. Under a last-man rule the keeper stays home in
precisely the situation he exists for.

**Fix.** New **§3.7.0**, which takes the decision back into #11 — the same move §3.3.6 made for
dive-commit timing, for the same reason. It is normative on two points:

1. **Only a goal-side body is cover.** A team-mate between the ball and the goal, inside the shot
   corridor, makes the trip unnecessary — partly because he is already narrowing the angle, partly
   because two bodies converging on one line is how a keeper gets rounded. A chasing or level
   defender is not cover. (The test itself lives in the composition root, which has the agent set;
   §3.7.0 owns the distance.)
2. **How far out he comes is his own attributes.** No fixed range:

```
rushCommitDistanceM = clamp(RUSH_COMMIT_BASE_M
                            + RUSH_COMMIT_K_ONE_VS_ONE      · OneVsOne_norm
                            + RUSH_COMMIT_K_COMPOSURE       · Composure_norm
                            − RUSH_COMMIT_FATIGUE_PENALTY_M · fatigue,
                            RUSH_COMMIT_MIN_DISTANCE_M, RUSH_COMMIT_MAX_DISTANCE_M)
```

An aggressive, composed sweeper-keeper commits from the edge of the area; a timid or spent one
barely leaves his line. Fatigue is subtractive on the project convention (0 = rested, 1 = spent),
the same sign as `RUSH_COMMIT_FATIGUE_COEFF` on the launch speed.

**FR-GK-024 check.** `OneVsOne` is consumed here for the commit DECISION. FR-GK-024 constrains the
1v1 **save** — §3.2 `requiredReactionMs`, §3.5 `attrFactor` — to closed-form coefficients with no
alternative formula path. Deciding whether to come out is not a save, and neither formula is
touched.

**Scope of change.** No schema, RNG, domain-tag, draw-site or draw-order change. Spec: #11 §3.7
state entry, new §3.7.0, six `[GT]`s in §3.4.6, §3.11 v0.7. Code:
`GoalkeeperRushDispatch.ComputeRushCommitDistanceM`, `GoalkeeperConstants.cs` v1.5,
`GkHeadingIntentSource.RushArmed` / `HasGoalSideCover`, `MatchEngine.TryCommitRushIntents`.

**Verification status.** Locked by `GoalkeeperRushTests` (the distance rises with `OneVsOne` and
`Composure`, falls with fatigue, and clamps both ways) and `GkRushTriggerTests` (a chasing defender
is not cover and the keeper still arms; a goal-side one is and he does not; identical geometry arms
for a bold keeper and refuses for a timid one). **None of it has been executed** — the authoring
environment has no .NET SDK and the agent proxy denies the installer.

---

## ERR-011-009: Goalkeeper Mechanics #11 §3.1.1 — a rush that arrived had nowhere to go

**Filed:** August 4, 2026 — at wiring backlog **W1**, the pass that gave
`GoalkeeperMechanics.CommitRushIntent` its first production caller. **Status: RESOLVED** (same
commit). Owner design supplement: `docs/tracking/gk-rush-trigger-design.md`; board item
`docs/tracking/match-engine-wiring-backlog.md` W1.

**How found.** Not by measurement — by wiring. The backlog's §5 sequence says each item is *"wire +
fix whatever the wiring surfaces"*, and reading the `Rushing` exits before turning the trigger on
surfaced this one on paper, which is the only reason it was not shipped as a live stall.

**The defect.** §3.1.1 gives `Rushing` three exits and `OneOnOne` two:

| From | To | Trigger | Fires for a loose ball? |
|---|---|---|---|
| `Rushing` | `Smothered` | hand-ball contact | only if the sweep makes contact |
| `Rushing` | `OneOnOne` | attacker with ball inside `ONE_VS_ONE_TRIGGER_RADIUS_M` | **no** — the predicate requires a ball possessor |
| `Rushing` | `Recovering` | F-08, possession passes to a third party | **no** — requires a possessor |
| `OneOnOne` | `Diving` | `SaveIntent` committed | no producer sends one in this state |
| `OneOnOne` | `Smothered` | GK inside `SMOTHER_TRIGGER_RADIUS_M` | **no** — same possessor requirement |

Meanwhile §3.7.2's update walks `gkPos` toward the locked `rushTarget` and **stops there** — the
implementation clamps the step so the keeper does not overshoot. So the terminal state of a sweep
to an unpossessed ball is: keeper standing on the ball, in `Rushing`, forever. Everything else in
the design anticipated a completion — `RushPhase` has carried a `Reached` member since v0.1 (never
published), and §3.7.3 reserves `AbortReason.AttackerBeatGK` for the attacker-passes-the-keeper case
(also never reachable) — but the one table that decides state had no row for it.

**Why it is the spec, not the implementation.** The implementation follows §3.1.1 exactly. There is
no transition to omit. The table is the artefact that is wrong, which is why the fix lands in the
spec and the code in the same commit.

**Fix.** Two §3.1.1 rows — `Rushing → Recovering` and `OneOnOne → Recovering` — triggered by the
keeper arriving within a new `[GT] RUSH_TARGET_REACHED_RADIUS_M` (§3.4.6, default 0.5 m) of the
locked target without contact, emitting `GoalkeeperRushEvent { rushPhase: Reached }`; plus the
matching terminating check in §3.7.2's pseudocode.

**This is a completion, not an abort.** FR-GK-018 and KD-15 forbid ending a committed rush on the
basis of ball-trajectory changes, and nothing in the new rows reads the ball: they read the keeper's
own arrival at a target he locked himself. The row is evaluated at the **lowest** priority of the
`Rushing` exits, so a run that arrives *and* meets the ball still resolves as the `Smothered`
contact it is, and F-08 still outranks it.

**Recorded, NOT fixed.** `AbortReason.AttackerBeatGK` stays unreachable. Under the new rows a keeper
whom the attacker has beaten terminates as `Reached` — the right state with the wrong label.
Labelling it correctly needs the attacker's position inside `Update`, which is cheap (the initial
attacker id is already stored), but it is a telemetry refinement and folding it in would make the
state-machine change harder to review than the defect it fixes.

**Scope of change.** No schema, RNG, domain-tag, draw-site or draw-order change. Code:
`GoalkeeperStateMachine.cs` v1.7, `GoalkeeperMechanics.cs` v1.11, `GoalkeeperConstants.cs` v1.5.
Spec: #11 §3.1.1 (two rows), §3.7.2 (the terminating check), §3.4.6 (the constant), §3.11 v0.7.

**Verification status.** Locked by `GoalkeeperRushTests` (a swept loose ball leaves `Rushing`, both
keepers) and four `GoalkeeperMechanicsTests` priority cases (arrival loses to contact, to F-08 and
to the 1v1 trigger; `OneOnOne` ends on arrival). **None of it has been executed**: the authoring
environment has no .NET SDK and the agent proxy denies the installer, so
`tools/dotnet-ci/run-gate.sh` did not run locally. The gate result for this entry is whatever the
GitHub `dotnet-compile-test` job reports.

---

## ERR-011-008: Goalkeeper Mechanics #11 §3.5.2 — a caught ball was never stopped, so the keeper's own claim went in

**Filed:** August 3, 2026 — at the conversion-at-contact pass, which set out to fix the two levers
`gk-contact-rate-design.md` §7 item 1 recorded (the Stage-0 `pointQuality` lottery and parry
placement) and found that neither was the goal-rate residual. **Status: RESOLVED** (same commit).
Owner design supplement: `docs/tracking/gk-conversion-at-contact-design.md`; match-engine §5.Z.23.

**How found.** §5.Z.22 tripled the keeper's contact rate (~35% → ~72%) and the goal count did not
move (14 → 15 over the corpus). Its recorded explanation — *"the added contacts are marginal,
end-of-envelope touches whose parries and spills keep the ball alive in the box"* — was a premise
no instrument reported, so the pass opened with one (`GoalConversionDiagnosticTests`, per-contact
fate over three full matches on the §5.Z.20–§5.Z.22 seeds). The premise is **refuted**, and the
band table says so in one column — ball speed the tick before the contact vs at the end of it:

| band | n | vIn (m/s) | vOut (m/s) | goal within 5 s |
|---|---|---|---|---|
| Caught | 10 | 11.1 | **10.8** | **7** |
| Parried | 2 | 10.8 | 0.0 | 0 |
| Deflected | 5 | 10.3 | 4.2 | 1 |
| Spilled | 4 | 13.9 | 9.0 | 0 |
| Missed | 2 | 9.5 | 9.5 | 0 |

The parries and spills work. **The catch — the one band that is supposed to end the threat
outright — removes 2.7% of the ball's speed**, i.e. one tick of drag and nothing else. Goal
provenance over the same corpus: **14 of 15 goals follow a keeper contact within 10 s.**

Verified against source. `GoalkeeperMechanics.Update`'s catch branch calls
`_ballSystem.SetPossessor(agentId)` and nothing further; the smother/1v1 claim does the same.
`SetPossessor` is `_engine._possessingAgentId = agentId` — a flag. Possession is **not** a
kinematic constraint anywhere in this engine: `RunPhysicsPhase` integrates the ball
unconditionally via `BallPhysicsCore.UpdateBallPhysics`, and `CheckRestartAndApply` adjudicates a
goal on the ball's POSITION without consulting the holder (its own comment anticipates
"a possessed-into-the-goal ball"). So a claimed 11 m/s shot travelled on and crossed the line with
the keeper recorded as holding it.

**The spec is not the defect; its summary is.** §3.5.2's body has carried
`ball.velocity = gkHandVelocity  // parked at hand position` since v0.1 and is correct. §3.5's
**Outputs** paragraph, however, read *"one of `Ball.SetPossessor` (catch) or `Ball.ApplyKick`
(parry / deflect / spill)"* — naming a single ball-side effect for the catch. The implementation
followed the summary. Note also that `IGoalkeeperBallSystem` offered no seam for the park at all
(`ApplyKick` is a kick, and on the Pass/Shot adapters it releases possession), so the omission was
mechanically easy to make and impossible to notice from the interface.

**Fix (spec + code, same commit).** §3.5's Outputs now states the catch's two effects, with an
ERR-011-008 note recording why the wording mattered; §3.5.2 gains the sentence that every contact
resolves to exactly one ball-side action, the catch's being a pair. **The §3.5.2 pseudocode body is
unchanged.** Code: `IGoalkeeperBallSystem` gains `ParkBall()`; both claim sites call it beside
`SetPossessor`; the engine adapter zeroes `_ball.Velocity` and `_ball.AngularVelocity`.
`gkHandVelocity` is read as zero at Stage 0 (the ball is at rest in the keeper's frame) — carrying
the hand's world velocity, and holding the ball at hand height rather than letting it settle, are
recorded §7 refinements. The park deliberately does **not** enter `BallStateType.Controlled`: no
production path leaves that state except a kick, and this engine has never produced a Controlled
ball, so introducing the first one inside a realism fix would be a far larger change than the
defect warrants.

**Determinism impact:** none of the five. No `SNAPSHOT_SCHEMA_VERSION` change (`_ball` is already
serialized), no new RNG stream, domain tag or draw site, no draw-order change — the park is a pure
write to current-tick ball state. Digests move for any match containing a claim, as intended.

**Locked by** `match-engine-keeper-claim` (#19 ScenarioRunner, Tier B, 2 seeds × 90 min):
`claimed-ball-is-arrested` and `held-ball-does-not-enter-own-net`. **2 of 3 predicates fail at the
pre-fix commit — verified by executing the scenario in a worktree at `4b12954`: 6 of 6 claims left
the ball travelling, and 5 of 6 held balls ended in the claiming keeper's own net.** Plus
`GoalkeeperClaimTests` (3 — a claim parks and does not kick; a non-claim kicks and does not park;
every contact resolves to exactly one ball-side action). Measured effect, three full matches on the
same seeds: goals **15 → 11** over the corpus (5.0 → **3.7**/match, the closest this engine has
measured to football's ~2.7), scorelines 2-2 / 2-0 / 6-3 → **1-0 / 2-2 / 4-2**, goals-after-contact
share 93% → 36%.

---

## ERR-008-022: Decision Tree #8 §3.1.4.3 / §3.2.3.2 — the shot lane threw away the far post before the occlusion model ever ran

**Filed:** August 6, 2026 — from the **adversarial review over the ERR-008-021 landing**, one day
old. **Status: RESOLVED** (same commit). Owner doc:
`docs/tracking/football-judgment-proxy-review.md` (§6.4.2). Doctrine authority: the review's §6
remediation doctrine, P1 (continuous, never a cliff) and P3 (the attribute ownership ledger).

**Id provenance.** `ERR-008-022` verified free at this landing: zero `## ERR-008-022` entries in
this log and zero citations anywhere in `docs/specs/` or `src/` before this commit.

**How found.** By hostile review of the -021 landing, in three independent passes
(correctness/geometry, architecture/doctrine, test adequacy). Every headline claim below was
re-derived independently against a Python reference implementation of both models before being
accepted. The review's own summary is that ERR-008-021 "achieves substantially less than claimed"
— finding (a) is why.

**(a) The lane's far bound was a plane through the goal CENTRE.** §3.1.4.3's `IsInShotPath`
admitted an opponent when `proj < distToGoalCentre`, where `proj` is his projection onto the
shooting axis. That describes a plane through the centre spot, perpendicular to the shot — which,
for any shooter *not* on the goal's centre line, cuts diagonally across the goal mouth. Algebraically
a blocker on the goal line at `(105, 34+k)` satisfies `proj < d ⟺ vk < 0`, and the far post always
has `sign(k) = sign(v)`, so:

- the **far-post** blocker was discarded and the near-post one kept, on **20,213 of 20,213**
  sampled in-range off-centre shooters (100.0%);
- a keeper standing on his line at goal centre gives `proj == distToGoal` *exactly* and was
  therefore dropped for **every** shooter position tested — shooter (95,20) with a keeper on his
  line read **1.000: a completely open goal**;
- the mirror case **admitted an opponent standing behind the goal line**, in the net, and (being
  within `GK_PROXIMITY_TO_GOAL`) handed him the goalkeeper's 1.5 m blocking radius.

The far post is half of what a shooter is aiming at. ERR-008-021 exists to price partial occlusion
at the posts, and this bound was discarding that geometry before the overlap model saw it. The
bound is now the **goal-line plane** — the surface the shot actually has to reach.

**(b) `GOAL_MIN_SHOT_DIST` was a whole-decision cliff, larger than the one -021 removed.** A blocker
on the shooting axis at 0.995 m of lane depth left the goal fully open (**1.000**, SHOOT generated at
maximum power); at 1.005 m the opening fell to the `GOAL_OPENING_MIN` floor (**0.050**) — and since
0.050 sits *below* `MIN_GOAL_VISIBILITY` (0.12), one centimetre of his position also decided whether
a SHOOT option **existed at all**. That is a 0.95 step against the 0.41 step -021 was filed to
remove, in the same function, under a spec that asserts continuity "by construction". Now ramps over
new `[GT] SHOT_BLOCKER_NEAR_FADE_M` = 1.0 m.

**(c) The goalkeeper read was a predicate, so its boundary was a cliff — and -021 widened it.**
Crossing `GK_PROXIMITY_TO_GOAL` flipped the blocking radius 0.5 ⇒ 1.5 m **and** the P3 ability
exemption together: a measured `GoalOpeningScore` step of **0.768 ⇒ 0.311** across 2 cm. The
pre-existing step was 0.457; ERR-008-021's ability term made it attribute-dependent and widened its
worst case to **0.551** — three lines from the code that landing rewrote, and unrecorded. Now a
scalar `gkness` lerping both the radius and the exemption over new `[GT] GK_PROXIMITY_FADE_M` = 2.0 m.
The positional proxy itself remains (Stage 0 has no GK role flag, and `PerceivedAgent` carries none);
what is removed is its boundary being a decision cliff.

**Corrections to the ERR-008-021 record.** Three verification claims in that landing were false and
are corrected here rather than left standing:

1. **The P5 exactness claim.** -021 recorded that the old rectangle and the new trapezoid integrate
   to the identical `4h·halfArc` "for every `h` and every `halfArc`, **including `h > halfArc`**".
   They do not: the old model applied a per-opponent clamp to `totalGoalAngle`, so above `h = halfArc`
   its rectangle saturates at `4·halfArc²` while the trapezoid does not. Measured ratios: 1.000× at
   `h`=8°/`halfArc`=8.35°, **1.198×** at `h`=10°, **2.000×** at `h`=16.7°. `h > halfArc` means a
   blocker inside roughly `d_goal × r / 3.66` of the shooter — ~2.7 m at 20 m out, ~3.4 m at 25 m,
   ~6.2 m for a keeper's radius at 15 m — which is routine play, not a corner case. **That claim was
   the stated reason no recalibration was required; the reason is withdrawn.** Not retuned here
   (KD-W1); recorded for the balance pass.
2. **The test count.** Published as "9 locks / 5 of the 8 evaluable fail pre-fix" in six documents.
   There were **10**: 10 locks / **9 evaluable** / **5 fail** / **4 pass**. The omitted pre-fix passer
   is `ShotLane_GoalkeeperOcclusion_IsAttributeIndependent` — which the same sentence advertised as a
   headline new lock.
3. **The §3.2.3.2 worked example.** Its opponent at `(48, 3)` sits `|48 − 52.5| = 4.5 m` from the goal
   line, so step 3 classifies him a **goalkeeper**: radius 1.5, and exempt from the ability term. His
   disc is then *not* wholly inside the arc and the score is **0.363, not the stated 0.757** — and the
   two new derived examples (elite 0.660, Vision-1 0.738) applied an ability term the algorithm
   exempts. All three numbers were unreachable. The example is re-derived with a genuine outfielder,
   re-expressed in **corner-origin** coordinates (it was written in the abolished centre-origin frame),
   and given a second variant that exercises the clipping the fix exists for.

**Test-adequacy defects found in the -021 suite** (the review's third pass):

- **The over-blocking half of ERR-008-021 had no lock at all.** The only test reaching a partial
  overlap asserted `< 1.0` rather than the value, so a mutant that keeps the continuous entry test
  but restores the pre-fix full-width contribution **passed all ten locks**. 8 of 12 plausible
  mutants survived, including `halfArc := totalArc` and `bisector := shot direction`.
- **`bisector` and the post clipping were untestable**, because every fixture put the shooter on the
  goal's centre line — where the bisector and the shooting axis coincide. That also made the away
  "mirror" bit-identical to the home case rather than an asymmetry test.
- **`ShotLane_NullAttributeView_IsAbilityNeutral` was a tautology.** The helper's own
  `if (attrs != null)` guard discards the differing attribute arguments, so it asserted `f(x) == f(x)`
  and would have passed against any implementation. This is precisely the shape the ERR-008-020 review
  caught one landing earlier — and the -021 commit message claimed to have avoided it "at authoring
  time rather than at review". The same defect exists in `PassLane_NullAttributeView_IsAbilityNeutral`;
  both are fixed.

**Fix.** Spec §3.1.4.3 + §3.2.3.2 and `OptionGenerator.cs` / `UtilityWeights.cs` /
`DecisionTreeConstants.cs`, same commit. `IsInShotPath` → `ShotPathWeight` (goal-line-plane bound +
depth ramp); `isGk` bool → `gkness` scalar lerping radius and P3 exemption; two new `[GT]` ramp
widths; the `BisectorDegenerateSqrLen` local promoted to a tagged catalogue constant (FR-CS-016).
Six new `OptionGeneratorTests` locks (closed-form overlap value, off-centre bisector/clipping,
far-post, behind-the-goal-line, and the two ramp-continuity sweeps) plus the de-tautologised
null-view pair, taking the shot-lane suite to **15**.

**Suite adequacy, measured — and then corrected by the first real gate run.** The H-finding above
("8 of 12 plausible mutants survived") is the kind of claim that needs re-measuring after the fix,
not asserting. Re-run against the hardened suite, the Python port reported **12 of 12 killed**:
pre-fix containment, no arc clipping, `bisector := shot direction`, either bound reverted, `gkness`
back to a bool, the ability term or the P3 exemption removed, the Vision fidelity floor neutralised,
an off-centre ability range, and the GK radius collapsed to the outfield one. That figure was
recorded with the caveat that it was a port of both code and assertions, not a run of the real NUnit
suite.

**The caveat was load-bearing, and the port was wrong.** CI run 402 (PR #302, head `301c634`,
August 6, 2026) compiled and executed this suite for the first time and failed
`ShotLane_FarPostBlocker_OccludesTheGoal`: expected 0.782157, **got 0.728880**. The production model
was correct; the *test* was not. It took the blocker position from `ctx.OpponentGoalPostL`, and this
file's home fixture defines `OpponentGoalPostL` as y = **30.34** — the post *nearer* a shooter at
(90, 24). The port had been pointed at y = 37.66. So:

- The committed test placed its blocker at the **near** post while asserting the **far** post's
  value, which is exactly why it failed.
- Worse than a wrong constant: the near post was never the defect. The pre-fix goal-centre-plane
  bound **kept** it (proj 15.998 < distToGoal 18.028) and discarded only the far one, so the test
  named for this entry's headline finding **would have passed against the broken model**. It was not
  a lock on the fix at all.
- The "12 of 12" therefore overstates the far-bound mutant specifically: the harness killed it, the
  committed test did not. The other eleven stand — CI passed the remaining 14 shot-lane locks and all
  127 other `DecisionTree.Tests` cases on the same run.

Fixed by selecting the far post from the **geometry** (`FarPostFrom`, the post further from the
shooter) rather than from the `PostL`/`PostR` label, which does not carry a consistent side across
this file's two fixtures — the home fixture's `PostL` is y = 30.34, the away fixture's is y = 37.66.
The expected value 0.782157 is unchanged and is now confirmed against the compiler: the same model
that reproduces CI's 0.728880 for the near post gives 0.782157 for the far one. `Assert.Less(score,
1.0f)` is now a real anti-regression lock, since the old bound scored this shot a completely open
goal.

This is the third verification claim in the ERR-008-021/-022 chain that a compiler falsified, after
the P5 exactness argument and the §3.2.3.2 worked example. The pattern is consistent and worth
naming: every one of them was a hand-derived number that no execution had ever checked.

**First gate run.** CI run 402, PR #302, head `301c634`, August 6, 2026. **Build succeeded, 0 errors**
(5 warnings, not shown to be new). `DecisionTree.Tests` **127 passed / 1 failed / 4 skipped / 132
total** — the single failure being the far-post fixture above. Every other suite green, including
`BallPhysics` 104/104, `PositioningAI` 131/144 (13 skipped), `MatchClientCore` 135/135,
`GoalkeeperMechanics` 94/121 (27 skipped), `InjuriesMedical` 66/66 and `TrainingSystem` 52/52. The
job was then **cancelled** by the runner at 16:59:45, roughly two minutes after the last suite
reported, so the cancellation cost no coverage; a separate "Unity asset hygiene" job had failed at
16:54:17 on a transient Actions outage (`Failed to resolve action download info: Service
Unavailable`) without reaching any repo check.

**Second review pass (AR-2, same day).** A hostile re-read of this fix found the two new ramps were
**not centred on the predicates they replace** — `laneWeight` ran 1.0 → 2.0 m and `gkness` 6 → 8 m,
i.e. entirely on one side. That is a systematic one-sided change in occlusion dressed as a continuity
fix, and it violates the same P5 pivot this entry criticises -021 for getting wrong: both ERR-008-019
and ERR-008-020 explicitly centred their ramps on the old cliff so the population integral is
preserved. Corrected to half-width either side (0.5 → 1.5 m and 5 → 7 m), so a blocker at exactly
`GOAL_MIN_SHOT_DIST` now contributes half his occlusion and one at exactly `GK_PROXIMITY_TO_GOAL`
reads half keeper. Every value lock is unchanged (all sit outside the ramp bands); the two continuity
sweeps were re-ranged to span the centred bands.

**Digest invariance NOT claimed** — every change is live on generated shots. No schema / RNG /
domain-tag / draw-site / draw-order change.

**Recorded, not fixed.** The `MIN_GOAL_VISIBILITY` gate is still a hard predicate on option
*existence*; what -022 changes is that the opening now decays to it instead of jumping past it. The
GK positional proxy still reads a deep defender as part-keeper. And the P5 residual in item 1 above
is left for the balance pass per **KD-W1** — the shot chain is not calibrated against a complete
engine yet.

**Gate NOT run — no .NET SDK in the authoring environment; nothing in this landing has been compiled
or executed.** Every number above is closed-form derivation cross-checked against a Python reference
implementation of both models.

---

## ERR-008-021: Decision Tree #8 §3.1.4.3 / §3.2.3.2 — a defender standing across the near post scored a fully open goal

**[RECONCILIATION NOTE, ERR-028-019 docs close-out pass, 2026-08-11 — read before trusting either
`## ERR-008-021` entry in this file.** This id has TWO full write-ups (this one, and a second
below, "the shot-lane occlusion could not tell an elite blocker from a poor one"). Both were filed
independently — this one August 5 in this branch, the other August 6 in the concurrent
`claude/football-judgment-proxy-review-pq12dz` branch (PR #305) — and each entry's own "Id
provenance" line below claims "zero `ERR-008-021` entries in this log", which was true in its
OWN branch at the moment it was written and is FALSE of the merged file both now live in. Neither
provenance claim is being corrected in place (both are accurate period pieces of their own
branch's history at time of writing); this note exists so a reader does not take either at face
value against the CURRENT file. **THIS entry is the one whose form survived reconciliation** — the
summary-table row for `ERR-008-021` (§ the ERR table above, search `RECONCILED August 7, 2026 at
the main merge`) states explicitly that at the merge, "this branch's form" (true angular OVERLAP,
not containment) was kept over PR #305's (which retained the containment cliff and added only
ability weighting) "because the other retains precisely the 0.595 ⇒ 1.000 cliff this finding was
filed against." The second entry below is therefore SUPERSEDED as a description of what shipped —
kept verbatim rather than deleted, annotated at its own header, per this project's convention of
annotating a falsified/superseded claim in place.]**

**Filed:** August 5, 2026 — the third fix landed under the football-judgment proxy review's
remediation doctrine (`football-judgment-proxy-review.md` §6; doctrine P1/P2/P3/P5 are the fix's
design authority). **Status: RESOLVED** (same commit). Owner doc:
`docs/tracking/football-judgment-proxy-review.md` (§2 finding for #8; §6.4 named this as the
follow-up when the ERR-008-020 template fix was deliberately kept small).

**Id provenance.** `ERR-008-021` verified free at this landing: zero `## ERR-008-021` entries in
this log and zero citations anywhere in `docs/specs/` or `src/` before this commit.

**How found.** By review, not measurement — the same sweep that produced ERR-008-019 and -020.
§6.4 recorded the shot lane as sharing the pass lane's geometry and deferred it by owner call; this
landing discharges that deferral. Reading it out, the shot lane turned out to carry the pass lane's
*two* defects rather than one, and the containment defect is the worse of them:

- **(a) A containment cliff — and it was wrong in both directions at once.** §3.2.3.2 step 4 counted
  an opponent's occlusion only when his angular *centre* lay inside the goal arc, and then counted
  his **entire** angular width. So a defender whose centre sat a hair outside the post direction
  contributed **exactly zero** — the shooter read a *fully open goal* while a man stood squarely
  across his near post — and one a centimetre the other side contributed a full width, half of which
  lay behind the post and blocked nothing. On the fixture the test suite now uses (shooter 15 m out
  on the centre line, one blocker 5 m in front), 4 cm of lateral defender position stepped
  `GoalOpeningScore` from **0.595 to 1.000**. That score both prices the SHOOT candidate (§3.2.3.1,
  a direct multiplicand of `U_SHOOT`) and gates its existence (§3.1.4.1 condition 4), and it drives
  `PowerIntent` (§3.5.3), so the discontinuity reached shot selection, shot value and shot speed.
- **(b) Attribute blindness.** The width was `2 × atan(radius / distance)` — body radius alone.
  A defender who neither reads the shot nor gets his body into its line shut the goal off exactly as
  hard as one who does. This is §2's recorded pattern-(a) finding, verbatim, transposed to the goal.

Both are structural properties of the formula, read directly from spec + code
(`OptionGenerator.ComputeGoalOpeningScore`). **No measurement instrument was run for this landing**
(no .NET SDK in the authoring environment — the ERR-008-020 / ERR-011-009 constraint), and none was
required to establish either defect. The 0.595 → 1.000 step above is computed from the formula, not
observed in a match.

**Fix (spec + code, same commit).** §3.2.3.2 steps 3–4 rewritten; §3.1.4.3 states the shape and
delegates the derivation, so the two sections cannot drift.

- **Overlap, not containment (doctrine P1).** Step 3 now yields an angular *interval*
  `[centre ± halfWidth]` rather than a scalar width, and step 4 intersects it with the goal arc
  `[−halfArc, +halfArc]` measured about the arc's own bisector. This is continuous **by
  construction** — unlike ERR-008-019 and -020 it needs no ramp constant, no half-width `[GT]` and
  no tolerance epsilon (the 0.01° epsilon the containment test required is deleted with it) — and it
  is simultaneously the geometrically honest answer, so the over-blocking and under-blocking go with
  the cliff rather than needing separate fixes.
- **Ability (doctrine P2).** The overlap is scaled by the blocker's `Anticipation`/`Positioning`
  mean mapped to `[GT] SHOT_BLOCKER_ABILITY_MIN/MAX` = 0.6/1.4. Anticipation is reading the shot
  early enough to move (the ledger's off-ball/predictive recognition row); Positioning is getting the
  body into its *line* rather than merely near it. The league-average blocker lands at exactly 1.0,
  so he occludes precisely the bare geometric arc.
- **Vision as discrimination fidelity (doctrine P2), on ONE dial.** `perceived = 1 + fidelity ×
  (true − 1)` with `fidelity = LANE_VISION_FIDELITY_FLOOR (0.2) + 0.8 × A_Vision(shooter)` — the
  §3.1.3.3 constant, deliberately **not** duplicated. Fidelity is a property of the assessor's
  Vision, not of the thing assessed, so a second copy would be a parallel surface (the v1.2 lesson
  that removed the duplicated `UtilityWeights`/`TacticalWeights` constants), not a second parameter.
  A Vision-1 shooter reads every blocker as near-average — which IS the pre-fix engine.
- **The goalkeeper is exempt from the ability term (doctrine P3).** He occludes on geometry alone,
  at his own larger radius. Keeper shot-stopping quality belongs to Goalkeeper Mechanics #11 — its
  §3.5 save model, and its §3.7.0 rush (wiring backlog W1, August 4), which *sets* the very geometry
  this function measures. Pricing it here as well would charge the shooter twice for one keeper.
  Locked by a test that moves the keeper's attributes between the extremes and asserts the score
  does not move.
- **Not changed:** steps 1, 2 and 5; `IsInShotPath`'s corridor; `BLOCKER_RADIUS`,
  `GK_BLOCKER_RADIUS`, `GK_PROXIMITY_TO_GOAL`, `GOAL_MIN_SHOT_DISTANCE`, `GOAL_OPENING_MIN`,
  `MIN_GOAL_VISIBILITY`; and §3.2.3.1's `U_SHOOT` formula, which consumes the score unchanged.

**Calibration (doctrine P5) — exact, not approximate.** Over a blocker whose angular centre `c` is
uniformly distributed, the pre-fix rule contributed a rectangle (full width `2h` for `|c| ≤ halfArc`,
zero outside) of area `4h·halfArc`. The overlap contributes a symmetric trapezoid: `2h` for
`|c| ≤ |halfArc − h|`, falling linearly to zero at `|c| = halfArc + h`. Its area is `4h·halfArc` for
**every** `h` and `halfArc`, including `h > halfArc`. Combined with the ability midpoint of exactly
1.0 over a uniform attribute population, the fix leaves the population-mean occlusion untouched on
both axes — it redistributes from a step to a slope and from anonymous bodies to identified ones.
The two new `[GT]`s are first-guess values; real calibration waits for a complete-engine pass per
**KD-W1** (`match-engine-wiring-backlog.md`).

**Determinism impact: none to the machinery.** No `SNAPSHOT_SCHEMA_VERSION` change (the attribute
view is the injected dependency ERR-008-020 already added, not cross-tick state), no new RNG stream /
domain tag / draw site, no draw-order change — the model is a pure function of the tick's snapshot
plus static attributes. **Digest invariance is NOT claimed and is false:** unlike ERR-008-019's
latent branch, this model is live on every SHOOT candidate the generator produces, and it moves for
any blocker who is not both exactly league-average and wholly inside the goal arc. The behaviour
change is the point of the fix. Locked by 10 `OptionGeneratorTests` (v1.7): the P5 pivot on the
null-view path AND on the computed path (Anticipation 10 / Positioning 11 ⇒ `mean01` = 0.5 exactly,
so the ability formula is actually executed — the ERR-008-020 AR-1 M-1 lesson, applied at authoring
time rather than at review), the MIN/MAX-midpoint invariant, no cliff across the post direction, the
straddling blocker now occluding what his body covers, Vision-20 separating elite from poor blockers
while Vision-1 barely does, null-view neutrality, the GK exemption, and the discrimination case
mirrored to the away side (the home-team-only-example trap). A reference implementation of both the
old and new models, run over all ten, confirms **5 of the 9 evaluable against the old model FAIL on
it** — continuity (step 0.405 against the asserted < 0.05), the straddling blocker (1.000, not
< 1.0), home discrimination, the low-Vision separation (the pre-fix gap is exactly zero) and the
away mirror. The other three — both P5 pivot rows and null-view neutrality — pass pre-fix by
construction, which is what a pivot row is for; the ninth, the MIN/MAX-midpoint invariant, cannot be
evaluated pre-fix because the constants are new. **Gate NOT run — no .NET
SDK in this environment; nothing in this landing has been compiled or executed.**

**Recorded, not fixed (two items, both pre-existing and both out of this fix's scope):**

1. **`IsInShotPath`'s corridor ends are still hard bounds.** The near bound excludes a blocker inside
   `GOAL_MIN_SHOT_DIST` = 1.0 m of the shooter, where the occlusion angle is enormous, so it steps
   from zero to a total block across 2 cm; the far bound is strictly `proj < distToGoal`, which
   drops a keeper standing exactly on his line on the shot axis. Left alone deliberately: front-of-
   versus behind-the-goal-line is a physical fact rather than a football judgment, so doctrine P1
   does not obviously reach it, and the near bound is a self-exclusion guard whose right shape is a
   separate question. Neither is reachable-and-wrong often enough to justify widening this commit.
   Noted while reading it: **§3.1.4.3 and §3.2.3.2 describe this same test differently** — §3.1.4.3
   defines `IsInShotPath` as a projection along the shot axis (which is what the code implements),
   while §3.2.3.2 step 3 writes it as a plain `opponentDist < GOAL_MIN_SHOT_DISTANCE` distance
   check. They disagree for any opponent off the shot axis. Pre-existing, untouched here, and it
   belongs with whichever pass settles the corridor's shape rather than with this one.
2. **§3.2.10's constant catalogue does not carry these constants** — nor `POWER_INTENT_FLOOR`
   (ERR-008-016), `SHOOT_SWEET_RANGE_M`/`SHOOT_DIST_FALLOFF_M` (-017), `DRIBBLE_GOAL_DIR_MIN_MODIFIER`
   (-018), `LONG_SHOT_RAMP_HALF_WIDTH` (-019) or the pass-lane set (-020). Five consecutive landings
   have defined constants in their own §3 subsection tables and left that catalogue behind, so its
   "Total constants: 58" summary is now wrong by at least nine. Following the established practice
   here rather than half-correcting it; the catalogue needs one reconciliation pass of its own.
## ERR-008-021: Decision Tree #8 §3.1.4.3 / §3.2.3.2 — the shot-lane occlusion could not tell an elite blocker from a poor one

**[SUPERSEDED, ERR-028-019 docs close-out pass, 2026-08-11 — reconciled against the entry above and
the `ERR-008-021` summary-table row, annotated in place per this project's convention rather than
deleted.** This entry was filed August 6, 2026 in the concurrent
`claude/football-judgment-proxy-review-pq12dz` branch (PR #305) — a DIFFERENT fix from the entry
immediately above (filed August 5 in this branch): this one adds ONLY the ability-weighting term
(doctrine P2/P3) and explicitly leaves the containment cliff untouched ("Unlike the pass lane there
was no positional cliff to kill… P1 is not in play" below — which the entry above's own finding
directly contradicts, since it found and fixed exactly that cliff). At the August 7, 2026 merge, the
summary-table row for `ERR-008-021` records that **this branch's** form was kept, not PR #305's:
"the other retains precisely the 0.595 ⇒ 1.000 cliff this finding was filed against" — i.e. THIS
entry's form did not ship. Its own "Id provenance" line below ("zero `ERR-008-021` occurrences in
this log") was true in PR #305's branch when written and is false of this merged file, which is why
it is annotated rather than silently trusted. Kept verbatim below for its own content (the AR-1
H-1 single-goalkeeper-candidate selection this note's sibling entry records as "strictly better…
deliberately NOT grafted in this merge" — i.e. real, unlanded, follow-up-worthy work), not as a
description of what is live in `src/decision-tree/OptionGenerator.cs` today.]**

**Filed:** August 6, 2026 — the third fix landed under the football-judgment proxy review's
remediation doctrine (`football-judgment-proxy-review.md` §6; doctrine P2/P3/P5 are the fix's
design authority). This is the follow-up **deliberately deferred at the ERR-008-020 landing**
(its scope note: "§3.1.4.3's shot-lane occlusion check shares the concept and deliberately does
NOT adopt the model — owner call, keep the template small"). **Status: RESOLVED** (same commit).
Owner doc: `docs/tracking/football-judgment-proxy-review.md`.

**Id provenance.** Verified free at this landing: zero `ERR-008-021` occurrences in this log and
zero citations anywhere in `docs/` or `src/` before this commit.

**How found.** By review, not measurement — the ERR-008-020 landing localized it and deferred it;
structural property read directly from spec + code. `OptionGenerator.ComputeGoalOpeningScore`
summed `2 × atan(radius / dist)` per blocker in the shot path with `radius` chosen only by the
GK positional heuristic — no blocker attribute, no shooter attribute, anywhere in the read. So a
slow, poor-anticipation defender standing between shooter and goal reduced `GoalOpeningScore`
(which both prices SHOOT in §3.2.3.1 and gates its *generation* via §3.1.4.1's
`MIN_GOAL_VISIBILITY`) exactly as much as an elite reader of the game in the identical spot.
Unlike the pass lane there was no positional cliff to kill — the occlusion is already continuous
in position and P1 is not in play; the defect is pure pattern (a) attribute-blindness.

**Fix (spec + code, same commit).** §3.2.3.2 gains **step 3a**; §3.1.4.3's pseudocode and the
ERR-008-020 deferral scope note are replaced with the landed model:

- Each **outfield** blocker's `blockedAngle` is multiplied by `perceived_ability(O)` — the
  ERR-008-020 scalar verbatim: blocker Anticipation/Pace mean mapped to
  `INTERCEPTOR_ABILITY_MIN..MAX` (0.6–1.4, league-average = exactly 1.0), read through the
  **shooter's** Vision as discrimination fidelity (`LANE_VISION_FIDELITY_FLOOR` 0.2 — a Vision-1
  shooter reads every blocker as near-average, which IS the pre-fix engine; doctrine P2).
- **No new constants.** The three `[GT]`s are reused verbatim from §3.1.3.3 — deliberately, per
  KD-W1: one calibration lever moves the pass-lane and shot-lane reads together at the eventual
  complete-engine balance pass, and no new dial lands against the still-unwired subsystems.
- The **goalkeeper's** arc is deliberately NOT weighted (doctrine P3): keeper shot-stopping
  quality is priced once, at the #11 save resolution — weighting his occlusion here would count
  it twice across the layers (the exact double-count shape recorded-not-fixed for #29/#41 risk
  mitigation), and `GK_BLOCKER_RADIUS` is already an abstraction of coverage rather than a body.
  **[AR-1 H-1 correction, same day:** as landed, "the goalkeeper" was implemented as *every
  opponent within the 6 m `GK_PROXIMITY_TO_GOAL` band* — the step-3 radius heuristic reused as
  the exemption gate — so every near-goal defender escaped the weighting precisely where shots
  are blocked (for a 10 m shot, most of the usable path), and the landing's fixtures all sat 8 m
  off the goal line so no test registered it. The exemption is now a **single GK candidate**:
  the goal-line-nearest visible opponent within the band (snapshot-order tie-break, identified
  independently of `IsInShotPath`); every other blocker is weighted. The *radius* stays per-band
  — the pre-existing recorded Stage-0 limitation — so the neutral-case arcs are unchanged by the
  correction.**]**
- P5 pivot: a blocker under a null attribute view (unwired host / legacy test) — or one at the
  ability midpoint (`mean01` = 0.5, e.g. Anticipation 10 / Pace 11, where the float arithmetic
  is exactly 1.0) — multiplies by exactly 1.0, so those arcs reproduce today's bit-for-bit.
  **[AR-1 M-2 correction:** the entry originally claimed this for "a league-average blocker"
  generally; the all-default raw-10/10 profile has `mean01` = 9/19 ⇒ ability ≈ 0.979, a ~2.1%
  smaller occlusion per such blocker, which can flip the §3.1.4.1 `MIN_GOAL_VISIBILITY` gate for
  shots whose pre-fix blocked fraction sat in (0.880, 0.899]. Today's arcs are the pivot
  *approximately*; exact only at the midpoint and under a null view — which is what doctrine P5
  ("≈ today's behavior") actually requires.**]**
- Worked step-3a example added to §3.2.3.2 (elite ×1.4 ⇒ 0.660, poor ×0.6 ⇒ 0.854, Vision-1
  reads 0.738/0.776, neutral reproduces 0.757 exactly).

**Adjacent defect recorded, not fixed:** §3.2.3.2's numerical example is written in a legacy
centre-origin frame (goal line x = 52.5, posts y = ±3.66 — the ERR-008-001 class, surviving in
an example), and its blocker sits 4.5 m from the goal line — inside `GK_PROXIMITY_TO_GOAL` = 6.0
m, so under the section's own step-3 heuristic it would classify as a goalkeeper — yet the
example prices it with the outfield 0.5 m radius. Annotated in-place (the §3.2.3.3 verification
chain consumes its 0.757); left standing because re-deriving the example moves approved
§3.2.3.3 arithmetic, which is its own pass.

**Determinism impact: none to the machinery.** No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG
stream / domain tag / draw site, no draw-order change — the model is a pure function of the
tick's snapshot + static attributes, and the attribute view is the injected dependency ERR-008-020
already wired (`DecisionTree.SetAllAgentAttributes`; null ⇒ neutral, never an exception). Digests
move for any match containing a generated SHOOT with a non-neutral outfield blocker in the shot
path — a behaviour change, as intended (the live engine wires real attributes, so real matches
will move; same posture as ERR-008-020). Locked by 7 `OptionGeneratorTests` (v1.8): the
computed-midpoint pivot equals the null-view arc exactly (the ERR-008-020 AR-1 M-1 lesson —
the pivot lock must drive the COMPUTED path, not the null-view guard), Vision-20 separates
elite/poor blockers while Vision-1 barely does (both discrimination locks carrying the
constants-derived `ExpectedShotSightedGap × 0.5` margin), null-view neutrality, the goal-line
keeper's arc invariant under his attributes AND pinned to its exact geometric value
(anti-vacuity), the in-band-defender-is-weighted H-1 regression lock, and the discrimination
case mirrored to the away side (the home-team-only-example trap; both away fixtures corrected
to production's post assignment, L = lower Y — AR-1 M-6). **Gate NOT run — no .NET SDK in the
authoring environment; CI on push is the only compiler for this work.**

**AR-1 (same day, adversarial review over the landing): 1 High, 7 Medium, 5 Low — all fixed.**
The High is the exemption-scope defect folded into the Fix section above (H-1). Mediums: the
three shared `[GT]`s documented only their pass-lane consumer and `UtilityWeights.cs` was not
versioned (M-1 — docs + v1.11); the P5 "bit-for-bit" overclaim (M-2, above — the same overclaim
shape retracted for ERR-008-019 one day earlier); the two discrimination locks had no margin, so
collapsing `INTERCEPTOR_ABILITY_MIN/MAX` to ±0.001 kept them green (M-3); three locks could pass
vacuously if the fixture blocker stopped reaching the occlusion accumulator (M-4); the Vision-
fidelity expression was duplicated verbatim in both lanes on day one — the exact duplication
shape the #29/#41 T1 review ruled a defect — hoisted to `VisionFidelity` (M-5); both away
fixtures used a goal-post L/R assignment production never constructs, so the mirror could not
catch an L/R-dependent asymmetry (M-6); §3.2.3.2's Known-limitation paragraph stated the radius-
misclassification consequence backwards — it *under*-estimates the opening, not over (M-7).
Lows: `PerceivedInterceptAbility`'s doc named only its first caller (L-1); null-view fixtures
passed live-looking attribute arguments (L-2); §3.1.4.3's pseudocode called an undefined
`IsLikelyGoalkeeper` with no cross-reference (L-3 — replaced by the `gk_candidate` pre-pass);
this index row's file count said 6 for 5 files (L-4); §3.1.4.1 gate (4) requires score STRICTLY
above `MIN_GOAL_VISIBILITY` but the code generated at exact equality — code aligned to the spec
(L-5, a measure-zero behaviour change). Surfaces: `OptionGenerator.cs` v1.8,
`OptionGeneratorTests.cs` v1.8, `UtilityWeights.cs` v1.11 (doc only), `section-3-1.md` v1.5,
`section-3-2.md` v1.13. Gate still not runnable; CI on push covers the AR fixes and the landing
together.

---

## ERR-008-019: Decision Tree #8 §3.2.3.1 — the midfield long-shot gate jumped 11× across one raw attribute point

**Filed:** August 5, 2026 — the second fix landed under the football-judgment proxy review's
remediation doctrine (`football-judgment-proxy-review.md` §6; doctrine P1/P5 are the fix's design
authority). **Status: RESOLVED** (same commit). Owner doc: `docs/tracking/football-judgment-proxy-review.md`
(§2 finding; this is the review's *founding* instance — the pattern the whole sweep was named after).

**Id provenance.** `ERR-008-019` was soft-reserved at the ERR-008-020 landing after that landing
verified the review's original "FIXED … gate green" claim for this finding was **false against both
branches** (no log entry, cliff live in `UtilityWeights.cs` / `UtilityScorer.cs` /
`section-3-2-3-to-3-2-9.md`, no branch carrying a fix — the root `CLAUDE.md` fabricated-claims
trap). Re-verified free at this landing as required: zero `## ERR-008-019` entries in this log and
zero citations in `docs/specs/` before this commit.

**How found.** By review, not measurement: the original judgment-proxy review pass over #8.
Structural property, read directly from spec + code: §3.2.3.1's midfield branch of
`ZoneModifier_SHOOT` was a hard step — `SHOOT_ZONE_MID_LONG` (0.55) strictly above
`(0.5 + A_LongShots × 0.5) > LONG_SHOT_THRESHOLD` (0.75), `SHOOT_ZONE_MID_SHORT` (0.05) at or
below it — verified against `UtilityScorer.ScoreShoot` (the ternary at the `FieldZone.MIDFIELD`
branch). One raw LongShots point (10 → 11, shifted 0.737 → 0.763) stepped the zone modifier
**11×**: the pattern-(b) shape (a continuous football judgment — "is this a viable long-shot
position for *me*?" — collapsed into a single-attribute cliff). No measurement instrument was
required (or possible for this branch — see the reachability note below).

**Fix (spec + code, same commit).** §3.2.3.1's midfield branch rewritten to a linear ramp in the
unchanged shifted form: `zoneM = lerp(SHORT, LONG, t)`,
`t = clamp01((shifted − (THRESHOLD − HW)) / (2 × HW))` with new
`[GT] LONG_SHOT_RAMP_HALF_WIDTH` = 0.05 (shifted units; valid range (0, 0.25]):

- **Ramp (doctrine P1):** full `SHORT` at raw ≤ 8, full `LONG` at raw ≥ 13, largest per-raw-point
  step ≈ 0.13 zone units — a 1-point attribute difference can never flip the outcome discretely.
- **Pivot (doctrine P5):** the ramp is centred on the old threshold — at exactly 0.75 shifted the
  modifier is the exact SHORT/LONG midpoint, the endpoints reproduce the old constants, and the
  population-integrated modifier over a uniformly distributed attribute equals the old step's
  (the ERR-008-020 centred-ramp precedent). Locked by test.
- **Scope (doctrine P2/P3 deliberately not applied):** long-shot inclination is the shooter's own
  execution capability, not a recognition of an external situation, so no fidelity term and no new
  attribute enters; `A_LongShots` continues to enter SHOOT in exactly its existing places (this
  zone modifier + the §3.1.4.2 range gate — no double-count introduced).
- §3.2.3.4 item 2 re-derived as the ramp bands (superseding the "effective raw ≥ 11" hard-threshold
  derivation, retained in history); §3.2.3.3 Case B (LongShots=16, shifted 0.895 ≥ the 0.80 ramp
  end) is past the ramp and its arithmetic is unchanged; `section-3-2.md` §3.2.1.3 footnote ¹
  updated; the AR-2 M-4 rule (compare the SHIFTED form, never the raw form) applies verbatim to
  the ramp input.

**Reachability (recorded, load-bearing for the impact claim — and the precise argument, which
is tighter than ERR-008-017's).** ERR-008-017 recorded this branch as generator-unreachable via
"a MIDFIELD-zone ball sits ≥ 40 m from the goal" — **that figure was stale when written**: the
ERR-008-016 equal-thirds correction (two days earlier) moved the ATTACKING boundary to 70 m, so a
MIDFIELD ball is > 35 m from the goal line, and because the zone classifies the *ball* while the
range gate measures the *agent*, a maximum-LongShots carrier goal-side of a ball just inside the
boundary can in principle generate a MIDFIELD SHOOT at ~34.5–35 m (agent within the 0.5 m
possession radius; range cap 20 + A × 15 = 35 m at A = 1). The claim that survives is the one
that matters for THIS fix: **the ramp differs from the old step only for shifted LongShots ≤ 0.80
(A ≤ 0.6), whose range gate caps at 29.0 m — disjoint by 5.5 m from the ≥ ~34.5 m any reachable
MIDFIELD SHOOT requires.** No state the generator can produce scores differently under the ramp.
The Case B reachability note in `section-3-2-3-to-3-2-9.md` is corrected in this commit
(housekeeping, the ERR-008-020 precedent). The cliff was therefore latent, not live — and the fix
is landed anyway, per the project's standing posture (a wrong-shaped model cannot be repaired by
later fitting, and the branch goes live the moment the range gate or zone geometry changes).
KD-W1 is satisfied trivially: the new `[GT]` is a first-guess value on a currently unwired
surface; calibration waits for the complete-engine pass like every other doctrine fix.

**Determinism impact: none.** No `SNAPSHOT_SCHEMA_VERSION` change, no RNG stream / domain tag /
draw site / draw order change — and, because the ramp-differs band and the generator-reachable
band are disjoint (above), **no digest moves on any seed** (unlike ERR-008-020): the behaviour
change is visible only to direct-injection paths.

**Owner revision (August 5, 2026, later same day): full-range ramp.** The owner directed the
scaling to run over the **full** LongShots range, not the initial 8–13 band ("scale shooting
range with the full 'long shots' attribute range, not just 8 to 13" — the metres-based §3.1.4.2
range gate already scales raw 1–20, so the instruction lands on this zone-modifier ramp).
Implemented as the one-value `[GT]` change the formula was built for: `LONG_SHOT_RAMP_HALF_WIDTH`
0.05 → **0.25**, its maximum valid value, spanning the whole shifted domain [0.5, 1.0] — `t`
reduces to `A_LongShots` exactly. Raw 1 is exactly `SHOOT_ZONE_MID_SHORT`, raw 20 exactly
`SHOOT_ZONE_MID_LONG`, and every raw point in between moves the modifier ≈ 0.026: **no plateau
anywhere** (the initial landing left raw 1–8 and 13–20 flat). P1 unchanged; **P5 still holds** —
the ramp remains centred on the attribute midpoint (= the old cliff), so the uniform-population
mean modifier is 0.30 under the step, the 0.05 ramp, and the full-range ramp alike. **Digest
invariance survives in a tighter form:** the full-range ramp differs from the old step at every
rating except raw 20 — and raw 20 (range 35.0 m) is the *only* rating whose §3.1.4.2 gate reaches
the ≥ ~34.5 m a MIDFIELD SHOOT requires (raw 19 caps at 34.2 m), and there the ramp evaluates to
exactly `SHOOT_ZONE_MID_LONG` = the step's value. Still no digest moves. Spec: §3.2.3.1 constants
block + correction note, §3.2.3.4 item 2 re-derived, **Case B recomputed 0.200 → 0.162** (a
LongShots-16 agent no longer fully earns 0.55), `section-3-2.md` v1.10 footnote. Tests: the M-4
lock is now `ShootMidfield_RampRunsInShiftedForm` (raw 10 computed ratio — the raw-form defect
suppresses raw 10 to SHORT, preserving the discrimination intent) and the endpoint/monotone lock
is now `ShootMidfield_FullRangeRamp_EndpointsExact_AndStrictlyMonotone` (raw 1/20 reproduce the
old SHORT/LONG pair; every intermediate point **strictly** increases — the v1.8 plateau-equality
assertions were the exact opposite of the owner's instruction and are gone); no-cliff and
midpoint locks unchanged. **Gate NOT run — no .NET SDK in the authoring environment.** Locked by four new `UtilityScorerTests` (no-cliff across the old threshold — pre-fix ratio
exactly 11×; exact SHORT/LONG midpoint at the ramp centre, the P5 pivot; endpoint clamps
reproducing the old constants; monotonicity over raw 1–20) plus the refitted AR-2 M-4 lock
(raw 12 → raw 14: past the ramp end AND still discriminating shifted vs raw form, preserving the
original regression intent). No away-mirror case: the formula is attribute-only — `BallZone` is
already team-relative upstream (ERR-008-002) and no geometry enters. **Gate NOT run — no .NET SDK
in this environment; nothing in this landing has been compiled or executed.**

**Invariance claim corrected (August 5, 2026, adversarial review over the landing): the
full-range form's "no digest moves on any seed" is RETRACTED.** Documentation only — the fix,
the constants and the four test locks are untouched; what was wrong is the *claim*. The argument
recorded above (and in four other places) required the shooter to be within **0.5 m** of the ball,
citing Ball Physics #1 §3.1.11.1 `CheckPossession`'s `ControlRadius`. **That is not a production
possession-granting path in this engine.** The two that are:

- `MatchEngine.RunLooseBallPickup` (§5.Z Phase H, KD-H3) grants possession to the nearest eligible
  agent within `MatchEngineConstants.LooseBallPickupRadiusM` = **1.0 m** (a config-overridable
  `[GT]`) of a loose ball **at rest**, and **leaves the ball where it lies** — up to 1.0 m from the
  new holder.
- the first-touch path (`FIRST_TOUCH_ACCEPTANCE_RADIUS_M` = 1.0 m), though a *controlled* touch
  then places the ball via the first-touch system.

And after possession is granted, **no engine rule re-anchors the ball to the holder or releases
possession on separation**: the ball moves only via kicks, collisions and first touch, the holder
moves freely under dispatched `MoveTo` commands, and the executors' only entry check is the
possession id (`PassExecutor` FM-01 `IsBallPossessedBy`). Holder–ball separation at a decision tick
is therefore **not bounded by 0.5 m**, and reaches 1.0 m through the pickup path alone.

**Corrected geometry.** A ball at rest at x → 70⁻ is MIDFIELD (ERR-008-016 equal thirds); the
holder up to 1.0 m goal-side of it puts `distToGoal` just above **34.0 m** (105 − 71.0). That is
**inside** raw 19's §3.1.4.2 range gate, `20 + (18/19) × 15 = 34.21 m` — not only raw 20's 35.0 m.
At raw 19 the full-range ramp evaluates to `0.05 + (18/19) × 0.5 ≈ 0.524`, against the old step's
0.55. A generator-produced option can therefore score differently, so **digest invariance is not
established for the full-range form and is likely false on any seed that realizes that state.**
The 0.5 m premise error (0.5 m) exceeds the margin the v1.62 argument relied on (0.3 m), which is
exactly why that claim fails and the **v1.61 narrow-ramp argument survives**: the 0.05 ramp differs
from the step only at `A_LongShots` ≤ 0.6, whose range gate caps at **29.0 m** — still disjoint
from the corrected > 34.0 m bound, by a margin the premise error cannot close.

**What is unaffected.** The behaviour change is **owner-directed and intended** (the full-range
instruction); this correction retracts a claim, not a decision. P5 holds (uniform-population mean
0.30, exact for the discrete 1–20 uniform), all four `UtilityScorerTests` locks stand, and the
worked examples (0.287 / 0.313; Case B 0.4447 → 0.162) are unchanged. **Determinism impact
restated:** still no `SNAPSHOT_SCHEMA_VERSION` change and no RNG stream / domain tag / draw site /
draw order change — but the per-tick digest **may** move on seeds that put a raw-19 shooter in that
band, so a scenario instrument that fails at the first gate run should be checked against this
before being treated as a regression. **Gate NOT run — no .NET SDK in the authoring environment.**

**Recorded with it (Low, same review).** `UtilityWeights.LONG_SHOT_RAMP_HALF_WIDTH`'s XML doc gave
a valid range of "> 0 and ≤ 0.25" while
`UtilityScorerTests.ShootMidfield_FullRangeRamp_EndpointsExact_AndStrictlyMonotone` fails at *any*
half-width below 0.25 (the plateaus return — the lock deliberately pins the owner's no-plateau
instruction). (0, 0.25] is the **formula's** validity domain, not a free dial; the doc and the
§3.2.3.1 constant block now say so, and a retune downward is an owner decision that must revisit
that lock in the same change.

---

## ERR-008-020: Decision Tree #8 §3.1.3.3 — the pass-lane "interceptor" was a 2 cm cliff that could not tell an elite defender from a poor one

**Filed:** August 4, 2026 — the first fix landed under the football-judgment proxy review's
remediation doctrine (`football-judgment-proxy-review.md` §6.4 — the owner-selected template fix).
**Status: RESOLVED** (same commit). Owner doc: `docs/tracking/football-judgment-proxy-review.md`
(§3.1.3.3 finding recorded in its §2; doctrine P1/P2/P5 are the fix's design authority).

**How found.** By review, not measurement: the football-judgment proxy review (August 4, 2026) swept
all 53 APPROVED specs for the ERR-008-019 defect *shape* — a continuous football judgment collapsed
into a hard threshold or bare geometry. §3.1.3.3's `is_interceptor` is the review's §2 finding for
#8: an opponent inside a single 0.8 m corridor counted as exactly **1** interceptor and one outside
it as exactly **0**, so (a) 2 cm of defender position stepped `PassLaneScore` by a full 0.33 — the
same discontinuity class as the long-shot cliff — and (b) **no defender attribute entered the
judgment anywhere**: verified against `OptionGenerator.CountInterceptors` (perp-distance test against
`PASS_LANE_WIDTH_HALF` only), so a Pace/Anticipation 1/1 defender in the lane priced a pass
identically to a 20/20 one in the identical spot. The lane score both prices PASS candidates and
gates their existence (`MIN_PASS_LANE_SCORE` floor), so passing judgment was structurally unable to
discriminate opposition quality. **No measurement instrument was run for this landing** (no .NET SDK
in the authoring environment — the ERR-011-009/-010 constraint), and none was required to establish
the defect: it is a structural property of the formula, read directly from spec + code.

**Fix (spec + code, same commit).** §3.1.3.3 rewritten to a continuous per-opponent threat weight,
`weight(O) = falloff(O) × perceived_ability(O)`, summed into the unchanged
`PassLaneScore = clamp(1 − Σweight / PASS_LANE_DIVISOR)`:

- **Falloff (doctrine P1):** full threat inside `[GT] PASS_LANE_CORE_HALF_WIDTH` = 0.4 m, linear to
  zero at `[GT] PASS_LANE_FALLOFF_END` = 1.2 m. The ramp is centred on the old 0.8 m cliff, so the
  integrated threat over a uniformly-positioned defender is exactly the old corridor's — the fix
  redistributes threat from a step to a slope rather than re-balancing passing (doctrine P5).
- **Ability:** defender `Anticipation`+`Pace` mean (normalised) mapped to
  `[GT] INTERCEPTOR_ABILITY_MIN/MAX` = 0.6/1.4; the league-average defender lands at exactly 1.0, so
  the neutral rows of the old verification table (1 interceptor → 0.67, etc.) are reproduced
  unchanged — the P5 pivot, locked by test.
- **Vision as discrimination fidelity (doctrine P2):** `perceived = 1 + fidelity × (true − 1)` with
  `fidelity = [GT] LANE_VISION_FIDELITY_FLOOR (0.2) + 0.8 × A_Vision`. A Vision-1 passer reads every
  defender as near-average — the pre-fix behaviour, so low skill degrades to today's engine rather
  than to something new. Vision's §3.2.2 PASS-utility term is untouched: it rewards vision
  *generally*; fidelity owns risk *discrimination* only (doctrine P3 — no double-count).
- **Plumbing:** the DT had no view of opponents' attributes (the perception snapshot carries
  position/velocity only — deliberately untouched). `DecisionTree.SetAllAgentAttributes` (boot seam,
  the `SetMatchSeed` pattern) stores the orchestrator's live `_dtAttrs` array reference →
  `Assemble` → `DecisionContext.AllAgentAttributes`; `MatchEngine` wires it at DT construction.
  Substitutions rewrite `_dtAttrs[slot]` in place and are visible through the reference. A null view
  (legacy test / unwired host) reads every opponent as ability 1.0 — the attribute-blind weighting,
  never an exception. `PASS_LANE_WIDTH_HALF` removed (zero remaining consumers).
- **Scope:** §3.1.4.3's shot-lane occlusion check shares the concept and deliberately does NOT adopt
  the model — deferred per the owner's §6.4 call (keep the template small); scope note added there.

**Determinism impact: none to the machinery.** No `SNAPSHOT_SCHEMA_VERSION` change (the attribute
view is an injected dependency, not cross-tick state — excluded from `CaptureState` like the
executors), no new RNG stream / domain tag / draw site, no draw-order change (the model is a pure
function of the tick's snapshot + static attributes). Digests move for any match containing a PASS
candidate with a defender within 1.2 m of a lane — a behaviour change, as intended. Locked by 6
`OptionGeneratorTests` (v1.5): the P5 pivot row exact, no 2 cm cliff at the old edge, Vision-20
separates elite/poor while Vision-1 barely does, null-view neutrality, and the discrimination case
mirrored to the away side (the home-team-only-example trap). **Gate NOT run — no .NET SDK in this
environment; nothing in this landing has been compiled or executed.**

**Housekeeping, same commit:** the review file's §2 claimed ERR-008-019 (the long-shot cliff) was
"FIXED … same commit; full dotnet gate green". **Verified false against both this branch and
`origin/main`**: no ERR-008-019 exists in this log, `LONG_SHOT_THRESHOLD`'s hard cliff is still live
in `UtilityWeights.cs` / `UtilityScorer.cs` / `decision-tree/section-3-2-3-to-3-2-9.md`, and no
branch carries the fix — the fabricated-claim trap (root `CLAUDE.md`). The review file is corrected
in this commit; **ERR-008-019 stays soft-reserved** for the long-shot fix (the id is cited as the
named precedent throughout the review and doctrine) and MUST be re-verified free at its own landing.

---

## ERR-008-018: Decision Tree #8 §3.2.4.1 — U_DRIBBLE had no directional term, and §3.1.5.2 promised it to the wrong section

**Filed:** August 4, 2026 — at the close-chance-creation pass (§5.Z.24), against the residual
§5.Z.23 §7 item 4 recorded. **Status: RESOLVED** (same commit). Owner design supplement:
`docs/tracking/close-chance-creation-design.md`.

**How found.** The creation instrument (`CloseChanceDiagnosticTests`, 6 full matches) measured what
a ball carrier in the ATTACKING THIRD actually decides. DRIBBLE is the modal action at **40% of
heartbeat decisions**, and the mean cosine between the chosen dribble direction and the direction to
the opponent goal is **−0.302**, with only **31%** of dribbles pointing goalward at all — *the
average dribble in the attacking third points away from the goal.* Negative on all six seeds
(−0.211 to −0.448).

**The defect is two-part and both parts are in the spec.** §3.1.5.2 selects
`best_direction = argmax(space_in_dir)` — deliberately direction-blind, since `SpaceScore` measures
only how clear a sector is — and closes by delegating the correction: *"No backward-sector penalty is
applied to `SpaceScore` at generation time; the scoring stage (§3.2.2) applies directional-to-goal
modifiers to the DRIBBLE utility."* But (a) **§3.2.4.1, DRIBBLE's actual scoring formula, has no such
factor**, and (b) the cross-reference names **§3.2.2, which is the PASS formula** — so the promised
term was delegated to a section that does not own DRIBBLE, and never had a home. The consequence is
structural: a dribble toward halfway scored *identically* to the same dribble at goal, and in the
final third — where the free space is behind the carrier — that is exactly what the argmax selects.
Same class as ERR-008-017 (`U_SHOOT` had no distance term): a formula omitting the term it should be
dominated by, in a system whose spec text says the term exists.

**Fix (spec + code, same commit).** §3.2.4.1 gains a multiplicative `DirectionQuality_DRIBBLE` =
`FLOOR + ((cosine + 1) / 2) × (1 − FLOOR)` over the cosine between the option's `BestDirection` and
the direction to the opponent goal — the same linear-in-cosine shape §3.1.3.5 already uses for PASS.
§3.1.5.2's cross-reference is corrected to §3.2.4.1. Worked examples A and B are recomputed and a new
Case A′ is added, since the whole point of the term is that A and A′ (identical except direction)
previously scored the same 0.384. **Degenerate-input contract (KD-V3 restated):** a zero
`BestDirection` — what every direct-injection test option carries — resolves to the exact ×1.0
identity, not the perpendicular midpoint, so all 22 pre-existing `UtilityScorerTests` are bitwise
unchanged.

**The `[GT]` is bounded by a defect in a different action, and that is deliberate.**
`DRIBBLE_GOAL_DIR_MIN_MODIFIER` lands at **0.80**, weaker than the 0.50 PASS floor, because
suppressing the dribble pushes the carrier onto HOLD — which has no timeout. HOLD share rises
20% → 23% at 0.80 and → 31% at 0.50, and at floors 0.65 and 0.50 one seed in six stalled outright
(mean final-third episode length 5.1 s → 17.5 s and 28.6 s). A unit lock asserts the DRIBBLE floor
stays above the PASS floor with that evidence cited, so the asymmetry cannot drift back silently.
The HOLD stall is recorded as the owner doc's §7 item 2.

**Determinism impact: none to the machinery.** No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG
stream / domain tag / draw site, no draw-order change. Digests move for any match containing a
dribble decision — a behaviour change, as intended. Locked by the `match-engine-close-chance`
scenario (**2 of 3 predicates fail on the pre-fix engine, verified by executing the scenario in a
worktree at `7fcd897`**: mean cosine −0.291 against a −0.10 bound, goalward share 0.306 against
0.42) + 4 `UtilityScorerTests` locks.

**Measured effect (6 full matches, identical seeds pre/post).** Mean dribble cosine
**−0.302 → +0.006** and goalward share **31% → 49%**, moving on **all six seeds with no overlap
between the pre- and post-fix distributions**. Carrier mix DRIBBLE 40% → 33%, HOLD 20% → 23%. **The
close-chance funnel itself did not move** (box occupancy 0.11 → 0.10, ball into the box 6% → 5% of
episodes, passes into the box 1% → 0%, shots 19.3 → 19.5, goals 3.67 → 3.50) and is explicitly not
claimed — the owner doc §7 item 1 re-localizes it to #8 §3.1.3 generating PASS candidates only at a
teammate's current position, so the tree cannot pass to a place, only to a player.

---

## ERR-008-017: Decision Tree #8 §3.2.3.1 — U_SHOOT had no distance term, so shots clustered at the range-gate boundary

**Filed:** July 28, 2026 — at the shot-volume pass (the residual §5.Z.19 named after pace
discharged half the excess). **Status: RESOLVED** (same commit). Owner design supplement:
`docs/tracking/shot-volume-design.md`.

**How found.** The shot-volume baseline measurement (ShotOutcomeDiagnosticTests v1.3, 3 full
matches) put the mean shot distance at **30–34 m** against football's ~17, with ~60% of shots
beyond 22 m — shots clustered AT the §3.1.4.2 range-gate boundary (20 + A_LongShots × 15 m).
The cause is structural, verified against source: `U_SHOOT = baseU × AM × GoalOpeningScore ×
(1 − risk)` contains **no distance factor**, and `GoalOpeningScore` is scale-free by
construction (the goal arc and a near-goal blocker's occlusion arc both shrink ~1/d), so within
range a 34 m shot scored identically to a 10 m one. Football's P(goal | shot) falls roughly
tenfold from 11 m to 30 m; the formula omitted the strongest single predictor of shot value in
the game it models — the ERR-008-016 class.

**Fix (spec + code, same commit).** §3.2.3.1 gains a multiplicative `DistanceQuality_SHOOT`
term: 1.0 for `d ≤ [GT] SHOOT_SWEET_RANGE_M`, else `[GT] SHOOT_DIST_FALLOFF_M / (FALLOFF +
(d − SWEET))` — continuous at the knee, bounded (0, 1], monotone; inside the sweet range every
pre-correction utility is bitwise unchanged, so the §5.Z.17/§5.Z.19 close-range calibrations are
untouched. The range gate stays as the hard eligibility cap (a cliff must not replace a
preference — the composure-noise band still lets an adventurous agent occasionally take the
long shot). Worked example Case A is pinned inside the sweet range (arithmetic unchanged);
Case B is annotated: its MIDFIELD geometry is production-unreachable through the generator
(zone minimum 40 m vs range-gate maximum 35 m — the midfield long-shot machinery is dead
surface, recorded in the owner doc §7.3, not separately filed). Boundary analysis gains case 4
(the range-boundary shot pre/post). Calibration and measured effect: owner doc §6.

**Determinism impact: none to the machinery.** No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG
stream / domain tag / draw site, no draw-order change. Digests move for any match containing a
shot decision — a behaviour change, as intended. Locked by the `match-engine-shot-speed`
scenario's new mean-shot-distance predicate (**fails on the pre-fix engine at distMean = 30.0
vs ceiling 24.0, verified by execution**) + 5 `UtilityScorerTests` locks.

---

## ERR-011-005 / ERR-011-006: Goalkeeper Mechanics #11 — the reaction window was evaluated at the wrong moment, against the wrong shot

**Filed:** July 28, 2026. **Status:** ✅ code- and spec-resolved same day (`section-3.md` v0.4).
**Owner document:** `docs/tracking/gk-catch-parry-conversion-design.md`; match-engine `§5.Z.20`.

**How found.** §5.Z.19 gave shots football pace and goals per shot ROSE 0.14–0.25 → 0.38–0.42; its
record named the keeper's catch/parry conversion as the dominant goal-rate term, and §5.Z.17 §7.5 had
already recorded the window as incoherent. Measured at baseline (3 full matches): contact-time
reaction windows **0.000 / 0.000 / 0.199**, mean elapsed-since-shot when airborne **85–349 seconds**,
one catch in the whole corpus.

| ID | Spec surface | Defect and resolution |
|---|---|---|
| **ERR-011-005** | #11 §3.2.3 (window evaluation anchor) | **The window was re-evaluated every frame, so the value the §3.5.1 contact blend consumed was dated by the ball's whole flight time** — `elapsed` at contact runs 400–1000 ms against a `required` of ~300 ms and a late tolerance under 200 ms, clamping the window to ~0 for any shot slower than about a third of a second of flight. The spec's own §3.2.5 worked example scores the moment *"the dive is already launched"* — the COMMIT. §3.2.3 now pins the anchor explicitly: computed ONCE at the dive-launch frame and frozen into `GkContactState` for the contact to consume; a rebound struck mid-dive does not re-date it. |
| **ERR-011-006** | #11 §3.2.1 (detection-stamp lifecycle) | **`_shotDetectedTickMs` was never cleared, and save episodes without a #6 shot event had no anchor at all.** A stale stamp from a previous episode dated every later dive (the 85–349 s measurements); deflection/rebound episodes — which the engine's save trigger legitimately arms on — left the window at 0 even when fresh. §3.2.1 now defines the lifecycle: the stamp dies with its episode (cleared on disarm-without-dive via `ClearSaveIntent` and on save resolution), and a **threat-onset fallback** (`OnThreatArmed`, called by the engine each armed stride) seeds it through the same §3.2.1/§3.2.2 formulas when none is live — a live stamp always wins, so the stamp itself is the latch and no new cross-tick state exists (it is already serialized in the v19 GK block). |

**`[GT]` recalibration filed with the same pass (KD-C3), all inside the §3.4.3/§3.4.5 spec ranges:**
`REACTION_BASE_MS` 350 → 220, `REACTION_BALL_SPEED_COEFF` 8 → 3, tolerances 120/80 → 200/140 — the
engine's discrete commit pipeline (strike → perception stamp → AI stride → tactical dive launch)
lands at ~100–300 ms elapsed, which the human-continuous-time values scored as a deep-early commit
(window ≈ 0 from the other side); and `HANDLING_BASE`/`HANDLING_K_ATTR` 0.45 → 0.60 with
`CATCH_THRESHOLD` 0.78 → 0.74, because with the Stage-0 contact anchors equal, pointQuality is a
fixed noise lottery (E ≈ 0.68) that no attribute or `[GT]` can move, and the old values could not
reach the catch band through it even with a perfect window.

**Determinism impact: none.** No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag /
draw site, and no draw-order change (the window computation makes no draws; the fixes change when
existing values are computed and what feeds them). Digests move for any match where a keeper dives,
as intended.

**Instrument fallout, fixed with the pass:** `match-engine-shot-speed` and
`ShotOutcomeDiagnosticTests` counted "shots" off `ShotDetectedTickMs` edges, which the arming stamps
would have redefined as "threat episodes" (min speed 3 m/s — slow rollers included); both re-anchored
to the new genuine-strike diagnostic counter `MatchEngine.TestOnly_ShotContacts`.

---

## ERR-006-002 / ERR-006-003 / ERR-001-004 / ERR-003-007: the shot-outcome distribution — shots could not miss, the goal had no crossbar, and no shot was ever blocked

**Filed:** July 27, 2026. **Status:** ✅ code-resolved same day; spec text patched same commit where the
spec was the defect (#1 §3.1.10.3, #6 §3.6.10, #3 §3.4.3), no patch where the spec was already right
and the implementation deviated (#6 §3.5.6/§3.5.7). **Owner document:**
`docs/tracking/shot-outcome-distribution-design.md`; match-engine `§5.Z.18`.

**How found.** The §5.Z.17 goalkeeper pass measured the keeper lever, discharged it, and found the
goal rate unmoved (15.3 goals/match vs football's ~2.7); its §7 recorded the residual, verified
against source. A new env-gated instrument (`ShotOutcomeDiagnosticTests`, `TD_SHOT_DIAGNOSTIC=1`)
measured the pre-fix distribution over three full matches on the `ConfigureSquads` path:
**15.3 goals/match, 0.24–0.29 goals per shot** (football ~0.10), and **zero** fast-ball body contacts
— blocked shots and meaningful misses both structurally absent.

| ID | Target | Change |
|---|---|---|
| **ERR-006-002** | #6 §3.5.6/§3.5.7 (implementation deviation — spec text already correct) | `ShotExecutor.ExecuteContact` rebuilt the vertical from `cos/sin(launchAngle)` and **never read `finalDirection.z`**, so `PlacementTarget.v` and the entire vertical half of the §3.6 error model were inert — while the spec's own §3.5.7/§3.9 step 9 pin `finalVelocity = finalDirection × kickSpeed`. Conformed: the intended aim is now the §3.5.6 composition (`ComputeAimDirectionWithLaunchAngle` — horizontal-to-u-target tilted by the §3.3 launch angle; v deliberately does not drive elevation, per §3.5.6), and CONTACT assembles `finalVelocity = finalDirection × _kickSpeed`. Vertical error is live; shots can sail over. |
| **ERR-006-003** | #6 §3.6.10 (spec + implementation) | **The error cone was not a cone.** The spec routed angular error through a `GOAL_RELATIVE_ERROR_SCALE` calibrated at a fixed 20 m reference (0.35 m/°, correct only at exactly 20 m); the implementation dropped even that, using `Deg2Rad` as a UV scale — **0.128 m/° at every range**, so missing from `u = 0.1` (0.732 m inside the post) required > 5.73° against a neutral shooter's ~2.25°. Now `displacement = tan(errorDeg·Deg2Rad) × distance` at the goal plane (reproduces the spec's 0.35 m/° at 20 m exactly, correct everywhere else); the vertical clamp becomes `[0, max(baseTargetZ, 1.5 × GoalHeight)]` so it bounds what error can add without flattening a lofted launch. Spec §3.6.10 patched; `GOAL_RELATIVE_ERROR_SCALE` retired. |
| **ERR-001-004** | #1 §3.1.10.3 (spec + implementation) | **The spec's own pseudocode gated every boundary test behind `z < DIAMETER`** ("only detects ground-level exits"), so a ball crossing the line airborne was neither a goal nor out of play — the goal was 7.32 m wide and of **unbounded height**, and airborne touchline crossings played on. The Laws win (Law 9: out on the ground or in the air; Law 10: goal under the bar): gate removed in `CheckBoundaries` AND `BallStateMachine.IsOutOfBounds` (the two predicates are pinned to agree), goal-line crossings adjudicated via the existing posts/crossbar box. Spec pseudocode patched. |
| **ERR-003-007** | #3 §3.4.3 / #1 §3.1.10.1 (the deferred entry point, chosen and wired) | **`BallCollisionHandler.OnAgentCollision` was an empty TODO that production calls** — no agent ever deflected the ball, so there were no blocked shots (football: ~30% of shots are blocked). New `BallCollision.ApplyAgentDeflection` (Ball Physics owns the response: planar cylindrical-body normal, reflect the approaching component, `BodyPartCoefficients` speed/spin retention — its first consumer); the handler owns the detection gates: Controlled ball = possession (no call), ball below `[GT] AgentDeflection.MinBallSpeedMps` (10.0) = first-touch territory (no call). The approaching-only response gate is the **stateless self-block guard** (on the kick-release frame the ball moves away from the kicker), chosen over a cooldown precisely so no cross-tick state and no schema bump exist. Reception is protected geometrically, not by the speed gate: the first-touch trigger reach (1.0 m) is well outside the ~0.4 m combined hitbox, and a ball cannot jump the gap in one 60 Hz tick below ~35 m/s. `AgentBallCollisionData` gains `AgentPosition` (the normal's input). |

Also retuned under the same pass (a `[GT]` change, not an ERR): `MIN_GOAL_VISIBILITY` 0.05 → 0.12
(#8 §3.1.4.1 — at 0.05 it equalled the `GOAL_OPENING_MIN` floor, so the SHOOT gate could only fire on
the degenerate zero-arc return and a fully walled-off shot was generated, scored and taken); and the
`ShotWorldAdapter` pressure query went live (the §4.4.1 call already existed and already re-sampled at
CONTACT — only the adapter body was a hardcoded `0f`), reusing the first-touch `PressureEvaluator`
with the §5.Z.14 canonical-frame un-mirror for the away shooter.

**Determinism impact: none to the machinery.** No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG
stream / domain tag / ordinal / draw site, and no change to the draw order (pressure alters the error
*magnitude*; the error *direction* stays the §3.6.9 hash). Digests move for any match containing a
shot or an airborne boundary crossing — a behaviour change, as intended.

**Test fallout, the recurring class.** Two tests had encoded the old contracts and were inverted with
intent preserved: `Goal_AirborneCrossing_NotDetectedUntilGround` (now `…UnderTheBar_IsAGoal`, plus a
new over-the-bar sibling) and `OutOfBounds_HighAboveTouchline_ReturnsFalse` (now `…ReturnsTrue`, Law 9).

**Measured effect:** see the owner document §4/§8 and `match-engine-design.md` §5.Z.18 for the
post-fix distribution; the acceptance scenario is `match-engine-shot-outcomes` (#19 ScenarioRunner,
Tier B), whose pre-fix failure is verified by execution.

---

## ERR-008-016 / ERR-006-004 / ERR-001-005: shot speed — nearly every shot left the boot at 10–30% power, and the goal frame was neither physical nor precisely adjudicated

**Filed:** July 28, 2026 — at the shot-speed & woodwork pass (residual lever (b) of the
shot-outcome distribution pass). **Status: RESOLVED** (same commit). Owner design supplement:
`docs/tracking/shot-speed-woodwork-design.md`.

**How found.** The §5.Z.18 pass fixed the shot-outcome mechanisms and its §4.1 measurement
localised the remaining mass: shot-tick speed means of **6.9–10.3 m/s** (maxima 15.3–18.9) against
football's ~20–25 — slow enough that the newly-real crossbar almost never bites and keepers field
rollers. Two structural causes composed, each verified against source, and the fix then made a
third defect (the non-physical goal frame) load-bearing.

| ID | Target | Change |
|---|---|---|
| **ERR-008-016** | #8 §3.5.3 (spec + implementation) | `PowerIntent = clamp(goalOpening × A_Finishing, 0.1, 1.0)` is a product of two [0,1] factors — `A_Finishing` ≈ 0.47 normalised for a neutral 10, `goalOpening` typically 0.2–0.6 — so nearly every generated shot **pinned at the formula's own 0.1 clamp floor**. The spec's rationale ("low opening → reduce power for placement precision") inverts the game it models: a deliberate competitive shot is essentially always struck hard; occlusion argues for placement, not a pass-weight tap. Patched to floor-plus-modulation: `clamp(POWER_INTENT_FLOOR + (1 − POWER_INTENT_FLOOR) × goalOpening × A_Finishing, FLOOR, 1.0)` with new `[GT] POWER_INTENT_FLOOR` = 0.65 — the old direction survives in the top band (an elite finisher with an open goal reaches exactly 1.0). |
| **ERR-006-004** | #6 Appendix A.1 (calibration — [GT] retune, logged because APPROVED text pins the value and its worked figures) | `V_FLOOR` 10 → **24**, over two measured iterations (20 → means 12.5–14.8, still short; 24 → means 14.7–16.1, maxima 23–28). At 10, a neutral player's FULL-power `vBase` capped at ~16 m/s before the §3.2.5–§3.2.8 reducers — the formula multiplies the (V_CEILING − V_FLOOR) span by attrFraction AND PowerIntent, so the anchor, not the span, must carry the base pace. `V_CEILING`/`V_ABSOLUTE_MIN`/`V_ABSOLUTE_MAX` unchanged; A.1.4's stacked-penalty visibility is preserved (worst stack ≈ 24 × 0.42 ≈ 10 m/s, still above the 8.0 clamp). Appendix A.1's value rows and A.1.4's worked figures patched. |
| **ERR-001-005** | #1 §3.1.10.2/§3.1.10.3 (spec + implementation) | With football-pace shots the ball moves **~0.42 m per 60 Hz tick**, which breaks the frame two ways. (a) **Adjudication**: `CheckBoundaries` tested the DETECTED position, up to ~0.42 m past the plane — a rising ball that crossed UNDER the bar could read as over it; the new `prevPosition` overload adjudicates at the segment's interpolated crossing of the out-plane (t clamped [0,1]; per-position callers keep the old form — out-NESS is identical, only goal-vs-over/wide refines, so `BallStateMachine.IsOutOfBounds` needs no change). (b) **Physicality**: `ApplyGoalPostCollision` still had zero production callers, and a discrete per-tick test could never fix that — a segment can enter AND exit a post's 0.17 m combined radius within one tick (tunneling). New `BallCollision.ApplySweptGoalFrameCollision`: the tick's movement segment against six capped cylinders (post axes half a post diameter OUTWARD of the 7.32 m inner-edge box; bar axis half a diameter ABOVE the 2.44 m lower edge — the same IFAB datums the box test uses), earliest hit wins, response is the existing restitution/spin-retention model. Gates: Controlled ball never deflects; degenerate/starting-inside segments are no-ops; an X-band prefilter (`[GT] GoalFrame.SweptPrefilterBandM`) skips the test away from the goals. New `[DERIVED] GoalFrame` geometry block in the #1 catalogue. Spec §3.1.10.2/.3 patched. |

**Determinism impact: none to the machinery.** No `SNAPSHOT_SCHEMA_VERSION` change (the engine's
`_prevTickBallPosition` is WITHIN-TICK — written at the top of every Physics phase, consumed the
same tick; the woodwork counter is diagnostic observation, the `AiPhaseRunCount` class), no new RNG
stream / domain tag / ordinal / draw site, no draw-order change. Digests move for any match
containing a shot — a behaviour change, as intended.

**Measured effect** (3 full matches, same seeds pre/post — owner doc §4.1): shot-tick means
6.9–10.3 → **14.7–16.1 m/s**, maxima 15.3–18.9 → **23.3–27.6**; shots per match 59–70 → **31–45**
(football ~25); off-target exits roughly doubled; the goal frame is live. Goals per shot ROSE
(0.14–0.25 → 0.38–0.42): football-pace shots expose the keeper's catch/parry conversion — exactly
residual lever (c), already recorded by §5.Z.17 §7.5, now measured against real shot speeds for the
first time. Acceptance: `match-engine-shot-speed` (#19 ScenarioRunner, Tier B) — **5 of 7
predicates fail on the pre-fix engine, verified by executing the scenario against the unmodified
tree before the fix landed** (speed floors unreachable; both frame probes adjudicated as exits; the
rising crossing misread as a goal kick).

---

*End of Spec Error Log v1.50 — July 28, 2026.*

## ERR-020-002: Code Standards #20 §3.5.2 layer taxonomy places 19 of 31 assemblies — FR-CS-046 is unenforceable for the composition root, the management layer and every client assembly

**Spec:** Code Standards #20
**Section:** §3.5.2 Layer Order and Dependency Arrows (FR-CS-046, FR-CS-047)
**Severity:** Medium
**Detected During:** the `src/CLAUDE.md` split (August 2, 2026) — the taxonomy is reproduced there, and reproducing it required checking it against `src/`.
**Status:** 🟡 **Open — PROPOSAL, awaiting owner sign-off.** No spec text has been changed.

**Problem:** §3.5.2's box names 14 assemblies across three gameplay layers (Physics 8, Mechanics 4,
AI 2) plus an empty `UI (Stage 1+ — not specified yet)` row. `src/CLAUDE.md` reproduces it and adds
two assemblies as cross-cutting foundations (`deterministic-sim`, `event-system`) and four as
infrastructure. `src/` now holds **31 assembly folders**. Twelve are placed nowhere:

`living-world`, `match-analytics`, `match-client-core`, `match-client-unity`, `match-client-web`,
`match-engine`, `match-viewer`, `player-database`, `player-progression`, `season-save`,
`tactical-instructions`, `ui-framework`.

FR-CS-046 says assembly references must flow in one direction only. A reference is legal or illegal
only relative to two layer memberships, so for any reference touching one of those twelve — which
includes **every reference into or out of the composition root** — FR-CS-046 currently decides
nothing. That is ~39% of the tree, and it is the part still being actively built (path-to-playable
Tracks S and C both land there), which is exactly when a direction rule earns its keep.

Two smaller defects sit in the same place:

- The `UI` row still reads *"Stage 1+ — not specified yet"*. Four UI/client assemblies exist
  (`ui-framework`, `match-client-core`, `match-client-unity`, `match-client-web`), and #38 is
  APPROVED. The placeholder is stale.
- `src/CLAUDE.md`'s **infrastructure** table (a `src/CLAUDE.md` extension, not #20 text) lists
  `code-standards` as an assembly. There is no `src/code-standards/` folder and there should not be
  — #20 is a style guide. The row should be struck.

**Root Cause:** §3.5.2 was authored against the Stage-0 physics/mechanics/AI tree, before the
composition root, the management layer, the presentation layer and the clients existed. Nothing in
the landing ritual requires a new assembly to claim a layer, so twelve assemblies were added over
fourteen months without the taxonomy moving. This is the ordinary drift failure of a hand-maintained
index — the same class as the `src/CLAUDE.md` file tree, which was retired to
`docs/tracking/src-tree.md` and explicitly marked non-authoritative in the same pass.

---

### Proposed resolution — a ten-tier order covering all 31 folders

**This is a proposal, not a decision.** Layer membership is #20's authority and per this project's
convention wants owner sign-off. It is offered as something to approve or redraw, not to apply.

The tiers below were **derived from the reference graph, not from folder names.** Every
`src/*/*.asmdef` `references` list was read and the whole graph checked against the proposed order.

| Tier | Assemblies | Why this tier |
|---|---|---|
| 0 **Foundation** | `project-constants`, `deterministic-sim`, `event-system` | Referenceable by everything; reference nothing but each other. Ratifies the cross-cutting-foundations paragraph `src/CLAUDE.md` already carries. |
| 1 **Physics** | `ball-physics`, `agent-movement`, `collision-system`, `first-touch`, `pass-mechanics`, `shot-mechanics`, `heading-mechanics`, `goalkeeper-mechanics` | Unchanged from §3.5.2. |
| 2 **Configuration** | `tactical-instructions` (#21) | Consumed by Mechanics (all four), AI (`decision-tree`) and everything above; references only `project-constants`. It cannot be a Mechanics member — `decision-tree` would then reference upward. **No Physics assembly references it**, so seating it above Physics is free and keeps the physics layer parameter-only. |
| 3 **Mechanics** | `positioning-ai`, `pressing-ai`, `defensive-ai`, `attacking-ai` | Unchanged from §3.5.2. |
| 4 **AI** | `decision-tree`, `perception-system` | Unchanged from §3.5.2. |
| 5 **Data** | `player-database` (#27) | Referenced by `match-engine`, `player-progression`, `season-save`, `match-client-core` — and by **no gameplay-layer assembly**. Seating it above AI preserves that: physics and AI keep operating on struct parameters, not squad rows. This is the tier whose placement matters most, and the one most worth arguing with. |
| 6 **Composition** | `match-engine` | References all four gameplay layers plus Data; the only assembly that does. Not a numbered spec — governed by `match-engine-design.md`. |
| 7 **Management** | `living-world` (#22), `player-progression` (#28), `season-save` (#30) | Long-horizon state above a match. `season-save` → `match-engine` is downward; `season-save` → `living-world` is intra-tier. |
| 8 **Presentation** | `match-viewer`, `match-analytics` (#37) | Derived-from-a-played-match. Ratifies the root `CLAUDE.md` rule that **no sim assembly may reference `match-analytics`** — currently true, and this tier is what would keep it true. |
| 9 **Client** | `match-client-core`, `ui-framework` (#38), `match-client-unity`, `match-client-web` | Replaces the stale empty `UI` placeholder. Internal order (`match-client-core` → `ui-framework` → `match-client-web`) is intra-tier; see the caveat below. |
| — **Infrastructure** | `performance-optimization` (#18), `testing-strategy` (#19) | Out-of-band: not in the order, and no tier may reference them at runtime. Unchanged from `src/CLAUDE.md`, minus the `code-standards` phantom row. |

**Verification (before proposing, not after):** all 31 folders are covered, none proposed that does
not exist, and across every `.asmdef` reference in the tree there are **zero upward references**
under this order. Twenty-nine references are intra-tier, all of them already present today and all
acyclic — the `pressing-ai` → `positioning-ai` precedent inside Mechanics establishes that intra-tier
is permitted. Adopting the order therefore **changes nothing that exists**; it only constrains what
can be written next. That is the whole value, and it is also why the cost of adopting it is zero.

**Intra-tier acyclicity — decided, sentence included.** A flat tier permits intra-tier cycles, and
two tiers now carry a real internal order (`match-client-core` → `ui-framework` →
`match-client-web`; `season-save` → `living-world`). The alternative was sub-ranking Client and
Management, which is more precise and more brittle — it would have to be re-cut every time a client
assembly is added. The sentence is taken instead, and §3.5.2 gains it verbatim:

> **Intra-layer references are permitted; intra-layer cycles are not.** An assembly MAY reference
> another assembly in the same layer (`pressing-ai` → `positioning-ai` is the standing example), but
> the assembly reference graph as a whole MUST remain acyclic (FR-CS-046a).

This documents an invariant that is **already enforced mechanically**, verified rather than assumed:
Unity rejects circular `.asmdef` references, and `tools/dotnet-ci/generate_projects.py` emits one
`<ProjectReference>` per `.asmdef` reference (line 157), so a cycle also fails the Linux compile
gate. Writing it down costs nothing and closes the gap between what the build enforces and what
§3.5.2 says — a build error reports what broke, not why the constraint exists. The current graph is
acyclic, so this too changes nothing that exists.

`FR-CS-046a` is proposed as a sub-numbered clause of FR-CS-046 rather than a new FR, since it
constrains the same rule's residue (what FR-CS-046 leaves undecided *within* a layer) and does not
renumber anything. The same sentence has already landed in `src/CLAUDE.md` `### Reference Direction`
as a layer rule, where it binds coding practice today under the existing three-layer taxonomy and
does not wait on this proposal's sign-off.

**Files Affected:**
| File | Location | Change |
|---|---|---|
| `docs/specs/code-standards/section-3.md` | §3.5.2 box + arrow diagram | Replace the 3-layer box with the 10-tier order; retire the empty `UI` placeholder row; add the intra-tier-acyclicity sentence |
| `src/CLAUDE.md` | `### Assembly Layer Taxonomy` | Re-reproduce the corrected taxonomy verbatim; delete the ⚠️ staleness note this entry supersedes; strike the `code-standards` infrastructure row |

**Not done here, deliberately:** #20 §3.5.2 is untouched. Assigning twelve assemblies to layers is a
design decision with a sign-off owner, and the wrong answer baked into the authority file is worse
than a documented gap. The ⚠️ note in `src/CLAUDE.md` names the gap and points readers at the
assembly map in the root `CLAUDE.md` meanwhile, so nobody is currently reading a wrong taxonomy as
right.

---

## ERR-020-003: Code Standards #20 §3.5.2 and the root `CLAUDE.md` draw the reference-direction rule with arrows pointing opposite ways

**Spec:** Code Standards #20
**Section:** §3.5.2 Layer Order and Dependency Arrows (FR-CS-046)
**Severity:** Low
**Detected During:** ERR-020-002's graph verification (August 2, 2026) — checking references against the diagram required deciding which way its arrows point.
**Status:** 🟡 Open — filed August 2, 2026

**Problem:** §3.5.2 renders the rule as

```
        Physics ──► Mechanics ──► AI ──► UI
        NO upward references permitted (FR-CS-046)
```

The root `CLAUDE.md` states the same rule as *"the reference-direction rule (**AI → Mechanics →
Physics, never the reverse**)"*. The arrows run opposite ways, and neither file says what its arrow
means. Both are self-consistent — #20's reads "is available to", `CLAUDE.md`'s reads "may reference"
— but a reader who has seen one and then meets the other has to reconstruct which convention is in
force, on the project's most load-bearing architectural rule. `src/CLAUDE.md` carries a third
rendering (`### Reference Direction`) — and it is the only one of the three that **labels its
notation** (*"`←` means 'is referenced by'"*), which is why it is the model for the fix below rather
than a fourth problem.

The actual code follows `CLAUDE.md`'s reading: `decision-tree` (AI) references `positioning-ai`
(Mechanics) references `pass-mechanics` (Physics). No violation exists; this is a notation defect,
not a behaviour one — which is why it is Low, and why it is worth fixing cheaply before someone
resolves the ambiguity in the wrong direction in a review.

**Root Cause:** the diagram was drawn as a layer *stack* (bottom-up), and the prose was written as a
*dependency* chain (top-down). Neither labelled its axis.

**Proposed resolution:** label the arrow in §3.5.2 — `──► reads "is available to"` — and add the root
`CLAUDE.md` sentence verbatim beneath the diagram, so both files state the rule in the same words in
addition to their own notation. Do not renumber or reverse the diagram: the layer stack reads
correctly bottom-up and several specs cite it in that orientation.

**Files Affected:**
| File | Location | Change |
|---|---|---|
| `docs/specs/code-standards/section-3.md` | §3.5.2 arrow diagram | Label the arrow; add the reference-direction sentence verbatim |
| `src/CLAUDE.md` | `### Reference Direction` | Cite #20 §3.5.2's labelled arrow so all three renderings agree |

---

*End of Spec Error Log v1.54 — August 2, 2026.*


