# Pre-B Decision Packets — D-03, D-04, D-05

> **Created:** October 10, 2026
> **Purpose:** The source-backed options, costs and evidence that roadmap §5.1 asks the implementing
> agent to prepare for the three owner decisions blocking step 6 (the pre-B engine baseline freeze).
> **Status:** DECIDED October 10, 2026 by Anton Zymin: D-03 option A, D-04 option A, D-05 as
> recommended. Decisions are recorded in the owning documents named below and linked from the
> roadmap's decision register (§5.1). Nothing here is a `[GT]` change or a KD-W1 exception.
> **Evidence anchor:** `main` at `7d9ce8b` (PR #495). Code facts below were read at that commit.

---

## Summary

| ID | Question | Recommended option | Code change if adopted | Delays step 6? |
|---|---|---|---|---|
| D-03 | May a `Recovering` keeper claim or save? | **A: no, keep it strict for now.** Clarify #11 §3.1.1; revisit a cause-split after W8 B | None (one spec clarification) | No |
| D-04 | Keep or correct the W2 tackle-foul cooldown bypass? | **A: keep the bypass as intended.** Record the rationale | None (supplement + preregistration disposition) | No |
| D-05 | Include or defer #434/#440, and where? | **Include both; land neither before B.** Land them after B→C in the step 9 lane, before the D-08 anchor | Two separate later landings | No |

With all three recommendations, step 6 freezes current `main` behavior with **no** pre-B behavior
change, which is the shortest path to W8 B. Every other combination adds at least one measured,
host-compiled landing before the freeze.

---

## D-03 — `Recovering` claim/save eligibility

**Owning records:** Goalkeeper Mechanics #11 §3.1.1; `open-issues.md` entry "A keeper in
`Recovering` cannot claim, rush or dive…" (opened September 27, 2026). The roadmap scopes this
decision to eligibility only: **no cooldown tuning**.

### Verified facts (main `7d9ce8b`)

- #11 §3.1.1 has only two exits from `Recovering`: to `Set` (when `RECOVERY_COOLDOWN_TICKS` has
  elapsed **or** the keeper is within `GK_REACTIVE_RADIUS_M` of its #12 slot), and to `Resting`.
  It has no row to `Anticipate`, `Diving`, `Rushing` or a claim.
- `MatchEngine.TryCommitClaimIntents` (`src/match-engine/MatchEngine.cs:4763`) and the rush
  producer (`:4890`) commit only from `Set`/`Anticipate`. A dive needs `Anticipate`, which is not
  reachable from `Recovering`.
- **Save commitment is not gated (found in PR #496 review):** `RunMechanicsAI` sets `SaveAvailable`
  without reading keeper state, and `HostSaveDispatch.CommitSave` → `CommitSaveIntent` stores the
  intent in any state. A visible threat during `Recovering` therefore banks a `SaveIntent` and opens
  the reaction window, and the dive can happen only after the keeper exits to `Set` and reaches
  `Anticipate`. Option A documents this as-is; withholding it is a behavior change (D-08 candidate).
- **What a `Recovering` keeper can still do:** `RunFirstTouch` (`:6066`) and `RunLooseBallPickup`
  (`:6217`) do not check keeper state. A ground-level ball reaching the keeper can still be first-touched or
  picked up like any outfield player. The block covers only hand claims, rushes and dives.
- `Recovering` is entered from several different situations: parry, spill or miss
  (`Airborne`/`Smothered`), a completed or cancelled distribution (`Distributing`), and a finished
  or aborted rush (`Rushing`/`OneOnOne`). Football treats these differently: a keeper on the
  ground after a dive is not the same as a keeper standing up after a goal kick. One state covers all of them.
- Measured effect of ERR-011-018, which made the 0.6 s cooldown real (open-issues entry, six
  frozen seeds, W8 instrument): hand claims 862 → 135; same-keeper self-reclaim 501 → 24 (the
  degenerate loop; collapsing it was intended); `Anticipate → Diving` 52 → 28;
  `Anticipate → Rushing` 233 → 204. `match-balance-scoreline` (4 matches): goals 34 → 44 on
  shots 98 → 94. Four matches are too few to size the conversion change, and the base engine
  already scores about 8.5 goals per match.

### Options

| Option | Behavior | Work and cost | Risk |
|---|---|---|---|
| **A. Strict (status quo)** | No claim, rush or dive from `Recovering`. Feet first touch and pickup unchanged | Spec-only: one sentence in #11 §3.1.1 saying the missing row is intentional, plus a version row. No code, no host slot, no re-baseline | Keeps the claim/dive loss. Its realism cost cannot be measured yet: four matches, against a base goal rate far from reality |
| **B. Uniform allowance** | `Recovering` may commit claims and saves like `Set` | #11 amendment, two producer gates and a new `Recovering → Anticipate` row. Measured six-seed landing and host slot **before** step 6 | High. Likely reopens the self-reclaim loop (501 → 24) that ERR-011-018 deliberately closed |
| **C. Split by entry cause** | Blocked after ground events (parry, spill, miss). Allowed after distribution or a finished rush, except reclaiming the ball it has just released | #11 amendment, a new serialized entry-cause field (**snapshot schema bump** to the next free version), restore and replay locks, measured landing and host slot **before** step 6 | Medium. The most realistic end state, but it takes the schema version B may need, and its design interacts with B's `Distributing → Recovering` path, which is not live yet |
| D. Saves only | Dives allowed from `Recovering`, hand claims still blocked | Like B, but only for the save path | Medium. Treats parry rebounds inconsistently: a keeper still on the ground could dive again at once |

### Recommendation: A, with C kept as a D-08 candidate after W8 B

- A is the only option that does not lengthen the critical path before step 6.
- C is the right long-term shape, but it should be designed **after** B lands. B activates the
  keeper-distribution path, which changes how often and why a keeper enters `Recovering`.
  Designing C against today's dormant distribution would mean designing it twice.
- C landing after B→C invalidates only the later calibration basis, which comes after D-08 anyway.
  It does not invalidate the A→B or B→C comparisons.
- If you want evidence before choosing, the cheapest addition is an observation-only counter:
  shots on target, and crosses reaching hand height, that arrive while the defending keeper is
  `Recovering`, broken down by entry cause, on the six frozen seeds. This is optional and not a
  prerequisite for A.

**Decision record:** Anton Zymin chose **option A** on October 10, 2026. Recorded in #11 Section 3 v0.19
(§3.1.1 note) and the open `Recovering` issue.

---

## D-04 — W2 foul-cooldown policy

**Owning records:** `foul-discipline-balance-design.md` KD-F3 (v1.3 correction);
`foul-card-w3-w9-preregistration.md` (#435) §2.1; `MatchEngineConstants.FoulCooldownTicks`;
evidence `evidence/foul-card-six-seed/`.

### Verified facts

- **The asymmetry.** Every applied foul re-arms `_foulCooldownRemaining = FoulCooldownTicks`
  (180 ticks = 3 s; `MatchEngine.cs:5831`). Only collision `FROM_BEHIND` candidates consult it.
  Decided W2 tackle fouls (`RaiseDecidedFoulCandidate`) do not check it, so they can be applied
  during the cooldown.
- **Measured exposure (pre-W3 `main` `c56e5e1a`, run `35765635130`, six full matches):**
  50 fouls = 42 collision + 8 tackle. The cooldown suppressed 25 of 773 collision candidates.
  Both tackle-during-cooldown counters were **0**. With only eight tackle fouls, that zero does not
  decide the question, as the evidence README says. Current-`main` exposure has not been re-measured.
- **New observation relevant to the decision:** `ApplyRestart` (`MatchEngine.cs:5503`) gives the
  restart taker possession in the **same tick**. There is no dead-ball hold. The v1.0 rationale for
  180 ticks ("a restart takes several seconds and the players are still tangled through it") does
  not match the engine: the whole 3 s cooldown runs during live play after the free kick.

### Options

| Option | Behavior | Work and cost | Risk |
|---|---|---|---|
| **A. Retain the bypass as intended** | Unchanged | Documents only: KD-F3 disposition, preregistration §2.1 disposition, comment-only `FoulCooldownTicks` note. No invalidator, no rerun | Low. A tackle foul is one adjudicated event, not a sustained tangle, so the debounce's purpose does not apply to it |
| B. Symmetric gate | Tackle fouls are also suppressed during the cooldown | Needs a new rule for the suppressed tackle: (i) the tackle stands and the foul goes unpunished, or (ii) the tackle is cancelled. #14/#435 amendment, a code change, a **#435 §8 invalidator**: rerun the frozen corpus, plus a host slot before step 6 | Medium. Both sub-rules are less realistic than the status quo, and the measured exposure was zero |
| C. Rework the cooldown itself | For example, gate only the same offender–victim pair, or end the gate when the restart is taken | Semantics change and a `[GT]`-adjacent calibration question. KD-W1 forbids it until the complete-engine calibration | Out of scope for a pre-B decision |

### Recommendation: A

- No behavior change, no invalidator, and the step 6 freeze is not delayed.
- Record the instant-restart observation as an input to the eventual KD-W1 discipline calibration
  (option C territory). Do not act on it now.

**Decision record:** Anton Zymin chose **option A** on October 10, 2026, reasoning that a tackle foul
the referee whistles stops the game, so a cooldown is moot for it. Recorded in
`foul-discipline-balance-design.md` v1.5 KD-F3, the #435 preregistration §2.1 and the open foul/card
issue. The engine's stoppage is zero-length (instant restart); that stays a separate restart-flow and
KD-W1 calibration question.

---

## D-05 — #434 and #440: include or defer, and placement

**Owning records:** PR #434 (draft) and issue #440; `match-engine-wiring-backlog.md`;
`w3-agent-ball-fanout-design.md` (where #440 was diagnosed). Owner direction of September 23, 2026
on #440: a separate gameplay PR, not folded into #439 or #442.

### #440: a `BALL_LOOSE` tackle returns the ball to the original carrier in the same tick

- **Fact:** 14 of 14 observed `BallLoose` outcomes were re-picked by the original carrier in the same
  tick (three production parents). The code path is `TryResolveTackles` →
  `ReleaseControlledPossession(placeAtGround: false)` (`MatchEngine.cs:3978`), which leaves a resting ball at the
  carrier's position. Later in the same tick, `RunLooseBallPickup` gives it to the nearest agent,
  which is usually the carrier.
- **Why it matters:** #14 §3.6.5.2 says `BALL_LOOSE` "is not an implementation detail". Folding
  it into `MISSED` "makes it invisible". It is the commonest non-null outcome (25.27% in the
  §3.6.5.7 worked example). At present it behaves almost exactly like `MISSED`. Only `BALL_WON`
  changes the ball holder.
- **Fix shape (to be designed in its own PR):** either give the knocked ball a velocity so
  first touch decides it, or exclude the dispossessed carrier from same-tick pickup. Either way
  #14 §3.6.5 is likely the defect source: "knocked free" has no specified velocity. That makes it
  a spec+code landing with an ERR, not a code-only fix.

### #434: route live score, formation and Duty into Positioning AI

- **State:** a draft Codex-authored PR. Base `6cd05a03` is **493 commits behind** `main`; last
  updated September 28. Its `ERR-012-012` is not in `spec-error-log.md` on `main`. No host compile
  evidence is recorded for it.
- **On `main` today:** positioning is built with the fixed `STAGE0_FORMATION` (default F442;
  `MatchEngine.cs:1012`). `scoreDiff: 0` is hardcoded (`:3454`). No production code reads
  `DutyForeOffsetM`. So the formation and Duty a manager picks have **no positional effect**, which
  matters for PM-1's "set a formation and team instructions" and for PM-2.
- **Behavior impact:** formation routing at the default F442 and Duty at the default `Support`
  claim to be neutral. That must be re-proven on current `main`. Live `ScoreDiff` changes behavior
  in essentially every match, because the engine scores about 8.5 goals per match. The sent-off
  `ActiveOutfieldCount` exclusion changes behavior after a red card.

### Placement options (for each item)

| Placement | Effect on the critical path | Attribution |
|---|---|---|
| Before step 6 | Adds a measured landing and a host slot before the freeze, per item | The pre-B anchor includes it; A→B is clean |
| **After B→C, before the D-08 anchor (step 9 lane)** | None to W8 B/C | Separately measured; invalidates only the later calibration basis, which D-08/KD-W1 already expect |
| Defer past PM-2 | None | PM-2 ships with `BALL_LOOSE` acting like `MISSED` (#440), or with formation/Duty inert (#434) |

Landing either item **during** an A→B or B→C window is excluded: the roadmap treats a
behavior-affecting merge there as invalidating the comparison.

### Recommendation

- **#440: include, after B→C.** It corrects a spec-intent violation in a core duel outcome, and
  nothing ties it to keeper distribution. Placing it after B→C keeps W8 off its path. Land it as its
  own spec+code PR with six-seed before/after evidence.
- **#434: include, after B→C, re-authored on current `main` rather than merging the stale draft.**
  Split it into (i) formation and Duty routing, which must prove digest identity at the defaults on
  the frozen seeds, (ii) live `ScoreDiff` routing, and (iii) the sent-off exclusion. Part (i) may land
  earlier, even inside a window, **only** with digest-identity evidence on the frozen seeds.
  Parts (ii) and (iii) are behavior changes and need their own measurements. Whether PM-2 ships
  with them is then a D-08 scope question. I recommend that it does, because formation choice is a
  stated PM-1 capability.

**Decision record:** Anton Zymin accepted the recommendation on October 10, 2026: #440 included, after
B→C; #434 included, after B→C, re-authored and split. The owner also placed #434's formation and Duty
part in PM-2 scope; PM-2 inclusion of the `ScoreDiff` and sent-off parts remains a D-08 question.
Recorded in `match-engine-wiring-backlog.md` v1.34 and in decision comments on #440 and #434.

---

## Caveats

- The D-03 numbers come from four (scoreline) and six (instrument) matches on the `ff4dd34`/`520ba31`
  basis. The D-04 numbers come from pre-W3 `c56e5e1a`. Neither has been re-measured on `7d9ce8b`.
- No host time or worker-hour figure is given for any option. Book actual slots under D-01a.
- The #434 neutrality claims are the PR author's. They are unverified on current `main`.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.3 | October 10, 2026 | PR #496 review: D-03 facts add the ungated save-commitment path (a `SaveIntent` can be held during `Recovering`; the dive waits for exit). |
| v0.2 | October 10, 2026 | Owner decisions recorded: D-03 A, D-04 A (whistled foul stops play), D-05 as recommended, with #434 formation/Duty in PM-2 scope. |
| v0.1 | October 10, 2026 | Created: D-03/D-04/D-05 option packets with code facts verified at `7d9ce8b`. Prepared, not decided. |
