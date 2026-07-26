# Round-Resolution Calibration Corpus — A4a

> **Created:** July 26, 2026
> **Status:** **BLOCKED at KD-8 Step 0.** This document was meant to record the fitted parameters of the
> #30 round-resolution model against ~200 engine-simulated matches. It instead records why that corpus
> cannot be generated today, because Step 0 — the cheap signal check that runs *before* the multi-hour
> corpus — refused to proceed.
> **Governance:** `docs/tracking/league-bootstrap-design.md` KD-7 (model shape) + KD-8 (methodology and
> Step 0); `docs/tracking/path-to-playable-roadmap.md` item **A4a** and risk row 2.
> **Filed as:** ERR-030-014 (`docs/tracking/spec-error-log.md`).

---

## 1. What Step 0 is, and what it found

KD-8 Step 0 exists because of a specific failure mode: *"if those two populations' goal distributions
are not distinguishable, the corpus carries no signal to fit … otherwise A4a burns nine hours fitting
three parameters to noise and the league table stays meaningless."* It was added at the league-bootstrap
AR-5 review (M-4) precisely so this check would happen before the expensive run.

**It fired.** Run 2026-07-26 on the Linux gate host (Release, one process):

| | strong-at-home | strong-away |
|---|---|---|
| Matches | 10 | 10 |
| Mean measured `dSquad` | **+6.013** | **−5.984** |
| Mean goal margin | **0.000** | **0.000** |
| Distinct scorelines observed | 0–0 ×10 | 0–0 ×10 |
| Wall clock | 32 m 37 s for the 20 matches (~98 s/match) | |

Every one of the twenty full 90-minute matches finished **0–0**, at a rating differential of ±6 points on
a `[1,20]` scale — an enormous gap, correctly measured and correctly applied to the rosters. The corpus
carries no signal at all, and the reason is not the one Step 0 was written to catch.

## 2. Why: the engine never puts the ball in motion

A follow-up characterisation (`EngineScoringDiagnosticTests`, env-gated, committed alongside this note)
instrumented a match directly. Over 60,000 ticks (~16 minutes of match time), in **both** a
distinct-squad configuration and a plain neutral one:

| Observable | distinct squads | neutral, unconfigured |
|---|---|---|
| Score | 0–0 | 0–0 |
| **Max ball speed** | **0.00 m/s** | **0.00 m/s** |
| Max ball height | 0.11 m (= resting centre height) | 0.11 m |
| Ticks with a possessing agent | **0** | **0** |
| Ball x range | 23.53 … 84.76 | 17.97 … 83.11 |

The ball's velocity is **identically zero for the entire match**, it never leaves the ground, and no agent
ever holds it. Its x-position does wander, which is agents jostling a stationary ball around by physical
contact — not play.

The cause is a closed loop, and the engine's own source states half of it outright:

1. `MatchEngine.InitializeKickoffState` places the ball at the centre spot at rest —
   *"Stationary ball at the centre spot (**a kick would set it in motion; none at Stage 0**)."*
2. `MatchEngine.RunFirstTouch` **gate 3** requires the ball to already be moving before any agent can
   receive it (`|ballVelXY| ≥ FIRST_TOUCH_MIN_BALL_SPEED_M_S`, 0.5 m/s).
3. Possession is granted **only** by that first-touch path in production
   (`TestOnly_SetPossessor` is documented "Not called by production").
4. The ball is set in motion **only** by a pass or shot executor, whose adapters gate on
   `IsBallPossessedBy(agentId)`.

So: no motion ⇒ no reception ⇒ no possession ⇒ no kick ⇒ no motion. `ApplyRestart` does not break the
loop either — it repositions the ball and clears possession, and a restart requires a boundary crossing,
which requires motion.

**A production match is therefore a 90-minute 0–0 deadlock, and always has been.** This is not a defect
A4 or A3 introduced: the neutral column above is the exact configuration every existing match-engine test
and the `match-engine-kickoff-multi-second` capstone use.

### Why no existing test caught it

The 321 match-engine tests are per-subsystem or per-mechanic, each driving its own inputs. The one
composed test — the kickoff capstone — ticks 600 ticks (10 s) and asserts tick count, AI-stride cadence,
finiteness, on-pitch bounds, and that the digest chain advances. Every one of those holds for a match in
which nothing happens. **No test has ever asserted that the ball gets kicked**, which is exactly the class
of gap the path-to-playable roadmap opened with: *"the question a playable build answers — is this game
any good — has never once been asked."*

## 3. Consequences

- **A4a is blocked, upstream of itself.** The blocker is not the ~5 hours of compute the run needs
  (measured here at ~98 s/match ⇒ ~5.4 h for 198 matches serial, ~1.4 h across four processes). It is
  that the engine cannot currently produce a corpus with any variance in it.
- **The three `[GT]` shape parameters shipped with #30 T2 are provisional, not fitted**, and say so at
  their declaration (`SeasonLoopConstants`). They are chosen to be football-plausible so a human reading
  the league table sees sensible results; they make no claim to agree with the engine.
- **PM-1 ("watch a match") is blocked by the same gap**, and more severely: a browser viewer pointed at
  today's engine renders 22 players standing around a motionless ball. PM-2-sim is *not* blocked — the
  quick-sim season runs, saves, restores and rolls without touching the engine.
- The #30 T2 loop itself is unaffected and correct: it routes, resolves, applies, emits and persists
  exactly as specified. Its FR-SN-013b managed-fixture path demonstrably runs a real engine match — that
  match is simply always 0–0.

## 4. What has to happen before A4a can run

A match-engine change, owned by `docs/tracking/match-engine-design.md`, not by #30. The minimal shape is
a **kickoff possession grant**: at kickoff and at every restart, award possession to a designated agent so
the Decision Tree has a carrier to act for. That single change is what breaks the loop — from there
PASS/SHOOT dispatch, first touch, offside, fouls and goal detection all already exist.

It is deliberately **not** attempted as part of A4, for reasons worth recording:

- It is a behaviour change to the project's most safety-critical assembly, and by construction it
  activates a large amount of code that has never run in composition. Expect it to surface further
  defects (roadmap C5's prediction, at its strongest).
- It moves every digest in the engine. Most locks are comparative two-run checks and survive, but the
  schema preimage probes and the certified perf baseline need review.
- It wants its own design note, its own adversarial-review cycle, and its own landing.

Once it lands: re-run Step 0 (about 33 minutes), and only if the extremes separate, run the corpus and
`python3 tools/round-resolution-fit.py <csv...> --out docs/tracking/round-resolution-corpus.md`, which
replaces this document with the fitted artifact KD-8 specifies (per-bucket means and variances, raw rows,
engine SHA and `SNAPSHOT_SCHEMA_VERSION` at capture, and the ±0.25 / ±5 pp acceptance verdict).

## 5. Reproducing the evidence

```bash
# Step 0 pilot — 20 real matches, ~33 min Release. Fails while ERR-030-014 is open; that is the point.
TD_CALIBRATION_PILOT=1 dotnet test src/season-save/tests/season-save-tests.gen.csproj -c Release \
    --filter "FullyQualifiedName~Pilot_Extreme" -l "console;verbosity=detailed"

# Characterisation — two variants × 60 000 ticks, ~1 min Release.
TD_ENGINE_DIAGNOSTIC=1 dotnet test src/season-save/tests/season-save-tests.gen.csproj -c Release \
    --filter "FullyQualifiedName~EngineScoringDiagnostic" -l "console;verbosity=detailed"

# Corpus slice (once the engine can play) — each sample is one full match.
TD_CALIBRATION_SAMPLES=18 TD_CALIBRATION_DELTA_FROM=-5 TD_CALIBRATION_DELTA_TO=5 \
TD_CALIBRATION_OUT=/tmp/corpus.csv dotnet test ... --filter "FullyQualifiedName~Corpus_Generates"
```

Capture provenance for the runs recorded above: engine commit `23a5c98`,
`SNAPSHOT_SCHEMA_VERSION` 18, Linux gate host (non-certifying — these are engine *results*, not timings
or determinism proofs, so the pinned Windows/Unity tuple is not required).

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-26 | — | Initial record. A4a's KD-8 Step 0 pilot executed and REFUSED to proceed: 20/20 engine matches 0–0 at ±6 measured `dSquad`. Characterised to root cause — the ball starts at rest, only a moving ball can be received, and only a possessing agent can kick, so a production match is a closed deadlock (ERR-030-014). Records the evidence, the blast radius, the minimal fix, and why that fix is not attempted inside A4. |
