# Round-Resolution Calibration Corpus — A4a

> **Created:** July 26, 2026
> **Status (current):** **STEP 0 PASSED July 28, 2026 — the corpus is worth fitting for the first time.**
> Re-run after the §5.Z.17–§5.Z.21 match-realism chain (shot outcomes → shot speed + woodwork → keeper
> catch/parry conversion → shot volume; engine goal rate on the diagnostic seeds now 4.7/match vs
> football's ~2.7). Result over the same 20 keyed matches (spread = 3, dSquad ±6.0):
> **strong-at-home mean margin +7.100, strong-away mean margin −4.700** — the ramp extremes separate
> **in both directions**, upsets exist (the strong side LOSES 3–4 at away-weak's home in one row; a 5–8
> thriller in another), and the §5.Z.11 home/away asymmetry that made the July-26 numbers unfittable
> (25.3 vs 1.7 — the strong away side effectively never scored) has collapsed to a margin ratio of
> ~1.5× (7.1 vs 4.7; football's home advantage is smaller still, recorded below as a fit caveat, not a
> blocker). Full CSV in §1.b. Goals run hot for a ±6 mismatch (strong side 5.8–8.0/match) — expected:
> ±6 is a third of the whole [1,20] scale; the near-balanced buckets the fit actually leans on sit at
> the ~4.7/match diagnostic rate. **Instrument note:** the first post-play pilot run FAILED at teardown
> with every assertion green — a PLAYING match emits FM-08/FM-03 possession-race errors as ordinary
> match events (§5.Z Phase H), and this driver predates play developing; both env-gated drivers now
> carry the same `LogAssert.ignoreFailingMessages` wrapper as every other engine-driving diagnostic
> (harness tests v1.1), and the re-run with the fixed instrument reproduced the identical rows
> (deterministic keyed seeds) and PASSED. **Next action: the corpus run + fit** (`TD_CALIBRATION_SAMPLES`
> slices + `tools/round-resolution-fit.py`, ~1.4 h across four processes) — its own roadmap item.
> The prior records below are kept verbatim as the evidence trail.
>
> **Status (superseded, July 26):** **STILL BLOCKED July 26, 2026 — Step 0 was re-run twice and now PASSES, but on scorelines that must not be fitted.** After the §5.Z.9 foul balance pass and the §5.Z.10 keeper-placement fix, the pilot's assertion (`strongHomeMargin > strongAwayMargin`) is satisfied — 25.3 vs 1.7 — so it no longer refuses. The raw results are nonetheless unfittable: **strong-at-home matches finish 19–40 to nil, and the away side scores 0–2 in every one of the twenty matches** regardless of which side carries the +3 strength. Full detail and the two findings behind it: `match-engine-design.md` **§5.Z.11** (a structural home/away asymmetry worth ~50× football's home advantage, and a goal rate ~10× football's). **A4a stays blocked, now for a different and more dangerous reason than before:** previously the corpus was all zeros and obviously useless, so nothing could be fitted by accident; now it is full of plausible-looking non-zero numbers, and fitting three parameters against 25–0 results would calibrate the quick-sim to reproduce the defect faithfully across a whole 380-fixture league. That is worse than not fitting at all. **This is also a gap in Step 0 itself** — it asks "is there signal?", not "is the signal football?" — recorded rather than patched, because the right fix is upstream. Everything below is the July-26 pre-Phase-H record, kept verbatim as the evidence trail.
>
> **Status (superseded, post-Phase-H):** **UNBLOCKED July 26, 2026 — the upstream defect is fixed; Step 0 is re-runnable.** The engine gap this document diagnosed (ERR-030-014) was closed the same day by match-engine §5.Z Phase H (roadmap A4b): a production match now kicks the ball (peak 16.2–17.2 m/s, was 0.00), holds possession 10.5–20.9% of ticks (was 0%), works into both penalty areas and scores. **Next action: re-run the Step 0 pilot below (~33 min).** Note it may still refuse — Phase H makes matches *play*, not necessarily *discriminate by squad strength*, and the latter is exactly what Step 0 asks; if the ramp extremes remain indistinguishable the answer is to raise `LeagueStrengthSpread`, not to fit three parameters to noise. Everything below is the July-26 pre-fix record, kept verbatim as the evidence trail.
>
> **Status (original, pre-fix):** **BLOCKED at KD-8 Step 0.** This document was meant to record the fitted parameters of the
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

## 1.b Step 0 re-run, July 28, 2026 — PASSED

Same 20 keyed matches (deterministic seeds, so the rows are reproducible in isolation), on the
post-§5.Z.21 tree, ~33 min Release:

| | strong-at-home | strong-away |
|---|---|---|
| Mean measured `dSquad` | +6.013 | −5.984 |
| **Mean goal margin (home − away)** | **+7.100** | **−4.700** |
| Strong-side goals/match | 8.0 | 5.8 |
| Weak-side goals/match | 0.9 | 1.1 |
| Upsets (strong side beaten) | 1 of 10 (3–4) | 0 of 10 (one 5–8 near-miss) |

```
strong-at-home (homeDelta +3, awayDelta −3):
15-0  13-0  3-2  10-1  8-0  7-0  8-1  3-4  5-0  8-1
strong-away (homeDelta −3, awayDelta +3):
0-3  0-10  3-7  1-3  1-5  1-6  0-4  0-7  0-5  5-8
```

What changed since the July-26 record: the §5.Z.17–§5.Z.21 chain (every outcome class reachable;
football-pace shots + a physical goal frame; the keeper's conversion live; the U_SHOOT distance
term). The venue asymmetry is now a modifier on a strength signal instead of the signal itself.
**Fit caveat, recorded:** the residual home-advantage factor (~1.5× on margin) is still above
football's; the fit will absorb it into the model's home term, and if a later engine pass shrinks
it the corpus must be re-captured (the KD-8 re-capture rule already says exactly this).

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
| 0.2 | 2026-07-26 | — | Post-Phase-H / §5.Z.11 status updates (kept in the header chain): Step 0 re-runnable, then re-ran onto 25–0 scorelines — passing while unfittable (the home/away asymmetry). |
| 0.3 | 2026-07-28 | — | **Step 0 PASSED** on the post-§5.Z.21 tree: margins +7.100 / −4.700, both directions separate, upsets present, the venue asymmetry down to ~1.5× on margin (§1.b). Instrument fix recorded (LogAssert wrapper — a playing match emits FM-08/FM-03 as ordinary events; harness tests v1.1). Next: the corpus slices + fit, its own roadmap item. |
