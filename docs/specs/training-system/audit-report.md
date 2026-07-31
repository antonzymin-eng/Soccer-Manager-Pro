# Training System #29 — Adversarial Review of the July 30 Code Landing (AR-1)

**Created:** July 31, 2026
**Version:** 1.0
**Target:** commit `1ee1dd0` ("Training Sytem #29 Code by GPT-5.6 Terra") — `src/training-system/` (9 files)
**Reviewer posture:** hostile external review per the project's adversarial-review convention; every file
read in full; spec §2/§3/§4/§5 + Appendices cross-checked; full dotnet tree compiled (0 errors); the 7
shipped tests executed (all pass); two findings proven by execution, not inference.

**Verdict: 2 High · 5 Medium · 4 Low — does NOT pass. The Highs gate.**

> **FIX-PASS STATUS (July 31, 2026):** all 11 findings addressed in one commit on
> `claude/adversarial-review-spec-29-q3qwh4`; tests 7 → 41; full dotnet gate PASSED, 0 failures; the
> meta-integrity check is clean. H-2 took option **(a)** — the Stage-2 surface was completed
> (`ClubTrainingBlock` / `TrainingSchedule` / `TrainingViewModel`), with the tracking-doc updates the
> finding also required. Two deliberate deviations from the suggested fixes are recorded inline in the
> code: `ComputeTrainingInput` with the dial ON returns `Neutral` rather than throwing (FR-TR-007's
> contract concerns the OFF state, and a #30 integration that sets the dial early must not crash — the
> Stage-3 deferral is doc-noted and a test pins the identity so it fails deliberately when Stage-3
> lands), and L-2's `= default` is impossible without reordering the spec's pinned parameter order
> (C# CS1737), so the deviation is doc-noted instead. One spec back-prop filed and resolved: **ERR-029-004**
> (§4.1's reference list omitted `ProjectConstants`; `section-4.md` v0.4, `spec-error-log.md` v1.54).
> **Per the review convention this cycle stays OPEN until a full re-review of the entire assembly —
> not the diff — surfaces no new High or Medium finding.**

---

## High

### H-1 — `ComputeTrainingInput` does not exist; the assembly cannot serve the purpose #29 exists for
`src/training-system/TrainingStep.cs` (absent member).

FR-TR-004 (MUST): #29 exposes **two distinct entry points** — the pure `ComputeTrainingInput` (slot-1,
feeds #28's growth) and the mutating `AdvanceTrainingDay` (slot-2). Only the second was built. With it,
everything hanging off it is also absent: the #29-owned `deepTrainingEnabled` gate returning
`TrainingInput.Neutral` (FR-TR-006/007, §3.2, Appendix C), the FR-TR-005a root-assembled facility
parameter, and both KD-8 neutrality locks (T-TR-NEU-001/002). §4.2's file layout places
`ComputeTrainingInput` **in `TrainingStep.cs` at this T-phase** — the only §4.2 file the spec itself marks
deferred is `TrainingSaveCodec.cs` "(T1)".

The omission is self-incriminating in the asmdef: `training-system.asmdef` references
`TacticalDirector.PlayerProgression` — required solely so `ComputeTrainingInput` can return #28's
`TrainingInput` — and **no line of code in the assembly uses that reference**. The growth seam is the reason
#29 exists (feeding #28 is its charter; conditioning/fatigue are the supporting cast); what shipped is the
supporting cast only, with no recorded deferral.

*Fix:* implement `ComputeTrainingInput(in TrainingState, in PlayerAttributes, in CoachingModifier,
bool deepTrainingEnabled) → TrainingInput` per §3.2 — Stage-2 body is `deepTrainingEnabled ? throw-or-
BuildTrainingInput-stub : TrainingInput.Neutral` (the Stage-2 minimal contract is the gate + `Neutral`,
Appendix C; `BuildTrainingInput`'s deep weighting is legitimately Stage-3). Add T-TR-NEU-001 (every focus ⇒
`TrainingInput.Neutral` ⇒ a `Neutral` batch through #28's `AdvanceDay` is byte-identical to no-training)
and T-TR-NEU-002 (`AdvanceTrainingDay` never touches a `PlayerAttributes` field). If the FR-TR-005a
facility parameter is deferred to Stage-3 per the ◑ marker, say so in the XML doc.

### H-2 — Roughly half the Stage-2 MUST surface is missing with no declared slice, no design supplement, and no tracking-doc integration
`src/training-system/` (absent members); `docs/tracking/*` (untouched).

Beyond H-1, these MUST-level requirements have no implementation and no recorded deferral:

- **`SetFocus(club, playerId, focus)`** — FR-TR-023, F2/F4. Without it, focus can never change after
  `TrainingState.Create` in production: the "training focus" feature is an enum and nothing more.
- **The per-club `TrainingState` container + insert/remove lifecycle entry point** — FR-TR-025, §4.3
  ("#29 exposes an insert/remove entry point over the per-club `TrainingState` set that #30 calls").
  Without a container there is nothing for `SetFocus`, the schedule view, the view model, or the T1 codec
  to attach to — every next slice is blocked on a structure this slice should have shaped.
- **`TrainingSchedule`** (FR-TR-003, the derived read-only view) and **`TrainingViewModel`** (FR-TR-022).

The project's own process for partial landings (every T0 in the tree) is: a converged design supplement
naming the slice, tracking-doc updates (root `CLAUDE.md`, `src/CLAUDE.md`, `file-manifest.md` — "update
after every file change" is a standing rule), and doc-notes on each deferred surface. This landing touched
none of those files and carries no plan; the file headers claim "Stage-2 core" while delivering a subset of
the Stage-2 contract. Building #30's integration on this shape means discovering the missing container and
command API mid-integration — the compounding-cost class that makes this High rather than Medium.

*Fix:* either (a) complete the Stage-2 surface (container keyed by `PlayerId`, `SetFocus` with F2/F4
refusals, the two view types, the FR-TR-025 insert/remove seam), or (b) explicitly declare the slice: a
design note naming what is in/out and why, doc-notes on the deferred FRs, and the mandatory tracking-doc
updates. (a) is strongly preferred — the container shapes everything downstream.

---

## Medium

### M-1 — Sentinel collision: `worldDay == uint.MaxValue` re-arms "never advanced" and defeats F6 idempotency (PROVEN BY EXECUTION)
`src/training-system/TrainingStep.cs:25-36,46`.

`AdvanceTrainingDay` never excludes the sentinel value from the `worldDay` input domain. Executed probe on
the built assembly:

```
fresh state → AdvanceTrainingDay(day = uint.MaxValue)
  after: cond=7140 fat=100 last=4294967295 (== TRAINING_NOT_ADVANCED_SENTINEL)
re-run SAME day:            cond=7280 fat=200   ← double-accrual; F6 violated
then AdvanceTrainingDay(5): cond=7420 fat=300 last=5   ← cursor REWINDS; accrues again
```

Writing `LastAdvancedWorldDay = worldDay` with `worldDay == uint.MaxValue` makes the state
indistinguishable from fresh — the exact day-0 double-accrual trap class the sentinel exists to prevent
(§2.2, T-TR-DET-004's rationale), reintroduced at the opposite boundary. The same re-arm is reachable via
`last = uint.MaxValue − 1` advancing by one. #30 advances small days one at a time, so this is a boundary
input today — but it fails **open** (silent state corruption) where the project posture and F7 demand
fail-loud on out-of-contract input.

*Fix:* first line of `AdvanceTrainingDay`:
`if (worldDay == TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL) throw new ArgumentOutOfRangeException(nameof(worldDay), …);`
plus a regression lock.

### M-2 — Every `[GT]` constant is `public const` ALL_CAPS — the exact defect class #30 T0 pass 5 already burned down tree-wide
`src/training-system/TrainingSystemConstants.cs:27-52`.

`CONDITION_MIN/MAX/START`, `TRAINING_FATIGUE_MAX`, `FATIGUE_DAILY_RECOVERY`, `MATCH_ENTRY_FATIGUE_SCALE`,
`INJURY_RISK_MAX`, `FATIGUE_RISK_WEIGHT`, `LOW_CONDITION_RISK_WEIGHT` are declared `public const` in
ALL_CAPS under `#region GT`. Project convention (#20 / FR-CS-019, enforced by the June-30 migration across
17 catalogues): ALL_CAPS `const` is **[FIXED]-only**; `[GT]` is PascalCase `static readonly` reading
`Config.GetInt/GetFloat("training-system", key, fallback)`. A `const` inlines into consumers, structurally
locking these out of the config migration — precisely the finding recorded as an M against
`SeasonLoopConstants` ("a rules variant is a config change, not a code change", `src/CLAUDE.md` v2.36
pass 5), reproduced here in a catalogue authored a month after that fix. Note: converting requires a
`TacticalDirector.ProjectConstants` asmdef reference, which spec §4.1's three-reference list omits — file
the spec-text back-prop rather than silently deviating in either direction.

*Fix:* rename PascalCase, convert to `static readonly` off `GameplayConfigHolder.Config` with the current
literals as fallbacks (behaviour-neutral), region stays `GT`, add the `ProjectConstants` asmdef ref, file
the §4.1 back-prop.

### M-3 — The Appendix A `[GT]` focus tables live as magic literals inside formula code
`src/training-system/TrainingStep.cs:75-115`.

`GetFocusConditionDelta`/`GetFocusFatigueDelta` hardcode 12 `[GT]` magnitudes (70/30/120/80/100/70;
220/0/300/200/280/180) in switch statements inside `TrainingStep`. Appendix A defines these as catalogue
tables (`FocusConditionDelta[Focus]`, `FocusFatigueDelta[Focus]`); FR-CS-016 bans magic numbers in formula
files. The `AttributeConditioningBonus` / `RobustnessMitigation` weights (Appendix A `[GT]` "weights
table" rows) are likewise implicit ×1 raw sums with no catalogue presence at all — a designer cannot see,
let alone tune, either surface, and the two implicit-weight sums will drift silently when one is later
reweighted. (Array-valued `[GT]` tables are carved out of the *config loader* per the documented
tactical-instructions precedent — but not out of the *catalogue*.)

*Fix:* move both tables into `TrainingSystemConstants` as focus-ordinal-indexed `static readonly int[]`
with a coverage lock (`Enum.GetValues` length vs table length — the `POSITION_COUNT` precedent), and give
the two attribute-weight sets named catalogue entries even at weight 1.

### M-4 — The branch fails the Unity meta-integrity CI check: 11 tracked paths lack `.meta` (PROVEN BY EXECUTION)
`src/training-system/**`.

`bash tools/unity-ci/check-meta-integrity.sh` on this branch: `::error::11 tracked path(s) under src/ lack
a .meta` — both folders and all 9 files. The identical failure already happened once (`src/CLAUDE.md`
v2.35, player-progression) and the fix tool exists.

*Fix:* `bash tools/unity-ci/generate-missing-metas.sh`, commit the sidecars.

### M-5 — The shipped tests verify the happy path of the shipped subset; the spec's own §5 locks for that subset are missing
`src/training-system/tests/TrainingStepTests.cs`.

Against §5's acceptance contract, for the surface that DID ship:

- **T-TR-DET-001 absent** — no multi-day run, no save(value-copy)→restore→advance == uninterrupted-run
  equality. #28's `T-PG-DET-002` proves this is implementable today without the T1 codec. Determinism is
  the project's hardest requirement; the shipped suite never tests more than one consecutive day.
- **T-TR-CON-002 / T-TR-COA-001 absent** (determinism of the bonus; Identity-coach exactness as a named
  lock rather than a side effect).
- `CreateAndAdvance_RejectOutOfRangeFocusValues` (line 132) **asserts only `Create`** — the name claims
  both entry points; `AdvanceTrainingDay`'s own `ValidateFocus` branch (a hand-mutated
  `state.Focus = (TrainingFocus)99`, reachable because the fields are public-mutable) is unlocked.
- `ProjectMatchEntryFatigue_IsMonotonicAndClamped` (line 100) tests two endpoints; it locks neither
  monotonicity (its name) nor the Appendix B worked value (`2300/10000 → 0.23`).

*Fix:* add the four locks above; rename or extend the two misleadingly-named tests. Assert the Appendix B
three-day sequence (7140/7280/7420 · 2100/2200/2300), not just day one.

---

## Low

- **L-1 — `coach` is accepted-and-ignored with no doc note** — `TrainingStep.cs:20,38-45`. §3.1 routes both
  deltas through `ApplyCoach(…)`; the parameter is threaded (good, per FR-TR-016) but no `ApplyCoach` exists
  and nothing records the identity-elision. Project convention: a declared-but-unconsumed surface carries a
  doc-note naming its future producer (#34). *Fix:* doc-note on the parameter, or a literal
  `ApplyCoach(x, coach) => x` with the KD-3 citation.
- **L-2 — FR-TR-016's default parameter is missing** — the spec says the step takes `in CoachingModifier`
  *defaulting to Identity*; the signature has no default (`in` params accept `= default`). *Fix:* add it or
  doc-note the deviation.
- **L-3 — `ComputeInjuryRisk` fails open on out-of-contract state via silent int overflow** —
  `TrainingStep.cs:60-63`. Fields are public-mutable; `TrainingFatigue = int.MaxValue` wraps the risk sum
  negative → clamps to `0`: the most-fatigued representable player reports zero risk. Unreachable from
  states evolved by `AdvanceTrainingDay` (both cursors clamp ≤ 10000), but the posture elsewhere is
  fail-loud, not fail-open. *Fix:* compute in `long`, or bounds-gate the cursors at the consuming seams.
- **L-4 — All 9 file headers omit the `Author:` field** — FR-CS-056 lists it as required (use `—` for an
  agent). *Fix:* add the line.

---

## Out-of-scope observation (not a finding against this commit)

Main's HEAD `e3ad25d` ("1") converted `src/deterministic-sim/native/td_mxcsr.dll` — the **certified**
MXCSR plugin — and a mockup screenshot into Git-LFS pointer files. In any checkout without `git-lfs` (this
review environment included), the certified binary is now a 131-byte ASCII pointer. If the LFS objects are
not reliably fetchable on the pinned cert host and CI, the July-22 certified artifact is effectively gone
from the tree. Worth verifying independently of #29.

## Scope reviewed (full re-review basis, pass 1)

All 9 files of `src/training-system/` (production + tests + both asmdefs) read in full; Spec #29
`section-2.md`, `section-3.md`, `section-4.md`, `section-5.md`, `appendices.md` read in full;
`PlayerAttributes` field surface and sibling-assembly `.meta`/catalogue conventions spot-verified against
source. Executed: full-tree dotnet build via the CI generator (0 errors), the 7 shipped tests (all pass),
the sentinel probe (M-1 reproduced), and `check-meta-integrity.sh` (M-4 reproduced). Not run: the full
30-suite test sweep — commit `1ee1dd0` is purely additive (no existing file touched, verified via
`git show --stat`), so cross-assembly regression risk is nil; the full gate should still run in CI on push.

Per the review convention, the cycle is open: after fixes land, a full re-review of the entire assembly
(not the diff) runs again, until a complete pass yields no new High or Medium findings.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-07-31 | — | AR-1 over commit `1ee1dd0`: 2H+5M+4L. M-1/M-4 proven by execution. Cycle open. |
| 1.1 | 2026-07-31 | — | Fix-pass status recorded (header note): all 11 findings addressed in one commit; H-2 took option (a); two deviations doc-noted in code (dial-on returns `Neutral` rather than throwing; L-2 blocked by CS1737); ERR-029-004 filed + resolved. Tests 7 → 41, full gate PASSED. Findings text unchanged — this row records the response, not a re-review. Cycle remains OPEN pending the full re-review. |
#endregion
