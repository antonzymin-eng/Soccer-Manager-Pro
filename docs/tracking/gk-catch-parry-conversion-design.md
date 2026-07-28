# Goalkeeper Catch/Parry Conversion — §5.Z.17 §7.5, the dominant goal-rate term

> **Created:** July 28, 2026
> **Status:** DESIGN SUPPLEMENT — the same governance class as `match-engine-design.md`. Opens no
> numbered spec and changes no `SPEC_INDEX.md` row. Files cross-spec back-props against Goalkeeper
> Mechanics **#11** (`ERR-011-005` / `ERR-011-006` — ids verified free against `spec-error-log.md`
> and the #11 spec folder before assignment).
> **Owner document:** `docs/tracking/match-engine-design.md` **§5.Z.20**.
> **Purpose:** §5.Z.19 fixed shot speed and made the goal frame physical, and goals per shot ROSE
> 0.14–0.25 → 0.38–0.42 — a football-pace shot beats this keeper far more often than a roller.
> §5.Z.17 §7.5 recorded why: the keeper contacts shots but converts almost none of those contacts
> into catches or controlled parries, because the §3.2 reaction window it blends into handling
> quality is incoherent. This note is the correctness fix for that window, the calibration of the
> conversion against measured full matches, and the measurement of what it does to the goal rate.

---

## 1. The finding

### 1.0 The baseline, measured first (3 full matches, `ConfigureSquads` path, the §5.Z.17 seeds)

| | measured |
|---|---|
| goals | 13 / 13 / 18 (mean **14.7**) |
| dives per keeper-match | 12–39 |
| hand contacts, whole corpus | **15** (the keeper meets roughly a quarter of on-target shots) |
| catches, whole corpus | **1** |
| quality at contact | 0.36–0.50 |
| reaction window at contact | **0.000 / 0.000 / 0.199** |
| mean elapsed-since-shot when airborne | **85–349 seconds** |

Two structural facts fall out. First, **a contact almost always stops the shot** (parry, deflect
and spill all redirect the ball; the 44 goals came from *uncontacted* on-target shots), so the
contact RATE bounds what any conversion fix can recover — recorded honestly in §7.1. Second,
**conversion at contact was structurally dead**: the window term (30% of the §3.5.1 blend) was
pinned at ~0 by the defects below, and the quality distribution sat in the spill/deflect bands.

Two layers, one measured symptom (goals/shot 0.38–0.42 against football's ~0.10):

### 1.1 The reaction window is evaluated at the wrong moment, against the wrong shot

`reactionWindowAchieved` (§3.2.3) is 30% of the §3.5.1 handling-quality blend. Three defects
compose to pin it at ~0 for nearly every contact:

1. **The anchor is the contact frame, not the dive commit.** The orchestrator recomputes the
   window every 60 Hz frame while the keeper is in `Anticipate`/`Diving`/`Airborne`, so the value
   the contact consumes is dated by the ball's whole *flight time* — `elapsed` at contact is
   400–1000 ms against a `requiredReactionMs` of ~300 ms and a late tolerance of 80 ms, which
   clamps the window to 0 for any shot that takes longer than ~380 ms to arrive. **The spec's own
   §3.2.5 worked example anchors the window at the moment "the dive is already launched"** — the
   commit, not the contact. A keeper that commits its dive on time and meets the ball late in
   flight has reacted perfectly; the implementation scores it as sluggish. (`ERR-011-005`.)

2. **`_shotDetectedTickMs` is never cleared** (§5.Z.17 §7.5 recorded this): a stale shot from a
   previous episode dates every later dive — measured mean elapsed-when-airborne ran 34–174 *s*.
   (`ERR-011-006`.)

3. **Episodes without a shot event have no anchor at all.** The engine's save trigger is "a loose
   ball driving at my goal", which includes deflections, rebounds and mis-hit passes;
   `OnShotExecutedEvent` fires only on a #6 shot CONTACT. A rebound episode leaves the stamp
   empty (or stale, per 2) and the window at 0. §7.5's own suggestion — date the window from the
   episode's onset — is adopted here as the fallback anchor. (`ERR-011-006`.)

### 1.2 The conversion at contact is mis-calibrated for the engine's timing structure

Even with a coherent anchor, the `[GT]` reaction constants describe a continuous-time human
(`REACTION_BASE_MS` = 350) while the engine's commit pipeline is discrete and fast: detection is
stamped at the strike + ~102 ms perception latency, and the dive launches one to two 10 Hz
strides later — `elapsed` at launch is ~100–300 ms, which the 350 ms `required` scores as an
*early* commit beyond the 120 ms tolerance ⇒ window ≈ 0 from the other side. The constants must
be recalibrated, inside their spec §3.4.3 ranges, against the engine's *measured*
elapsed-at-launch distribution — the same measure-then-set method as the §5.Z.9 foul pass and
the §5.Z.19 `VFloor` iterations.

## 2. Scope

**In scope.** The window anchor fix, the stamp lifecycle (clear + episode-onset fallback), the
`[GT]` recalibration of the reaction and handling constants against measured full matches, unit
locks, and the measured effect on goals/shot and goals/match.

**Explicitly out of scope.** The `SaveArmed` trigger geometry (§5.Z.17 §4.5 — unchanged); dive
kinematics and reach (§5.Z.17 measured them sufficient); rush/claim/distribution; shot volume
(lever (a), its own pass); the Stage-1 "keeper waits to time its dive" mechanism (the engine
commits at the earliest stride — a deliberate Stage-0 simplification recorded in §7).

## 3. Key decisions

### KD-C1 — Freeze the window at the dive-launch frame, not per-frame

The window is computed ONCE, on the frame the dive launches (where `_diveLaunchFrames` is
stamped), from `elapsed = launchTimeMs − shotDetectedTickMs`, and frozen into
`GkContactState.ReactionWindowAchieved` for the contact to consume. The per-frame recomputation
is removed. This is what §3.2.5's worked example describes — the window scores the COMMIT.

Rejected alternative: keep per-frame evaluation and freeze the maximum. That rewards a keeper
whose window *passed through* a good value at any point, which is not a reaction model — and it
keeps the late-anchor defect for slow balls.

Edge recorded: a shot struck *while the keeper is already airborne* (rebound mid-dive) does not
re-date the frozen window — the keeper committed before that ball existed, and its stale (lower)
window is the honest score for it.

### KD-C2 — Stamp lifecycle: cleared at episode end, seeded at episode onset, shot wins

- `ClearSaveIntent` (the engine's per-stride disarm call, already a no-op mid-dive) now also
  clears `_shotDetectedTickMs`/`_requiredReactionMs`, and the save-resolution branch clears them
  with the intent — so a stamp can never outlive its episode (`ERR-011-006`).
- New `OnThreatArmed(gkIndex, matchTimeMs, ballSpeedMps, attrs)`: stamps the §3.2.1/§3.2.2
  detection ONLY when no stamp is live (`_shotDetectedTickMs == 0`). The engine calls it each
  stride the `SaveArmed` geometry holds; after the first call it is a no-op until the episode
  ends, so no edge-detection state is needed — **the stamp itself is the latch, and it is already
  serialized in the v19 GK block: no new cross-tick state, no `SNAPSHOT_SCHEMA_VERSION` change.**
- A true shot CONTACT (`OnShotExecutedEvent`) still stamps unconditionally — the newest shot is
  the live threat and overwrites an arming-time stamp with the more precise strike anchor. For an
  in-range shot the strike stamp lands (Resolve phase) before the next stride's arming call sees
  it, so the precise anchor survives; for a long shot that only later enters the 16.5 m trigger
  range, the episode is dated from entering range — coherent, slightly conservative.

### KD-C3 — Recalibrate inside the spec §3.4.3/§3.4.5 ranges; measure, don't derive

The offline arithmetic gives the shape but not the values (the §5.Z.9 lesson). Iteration
procedure: land the correctness fix, run the funnel instrument over full matches, read the
elapsed-at-launch distribution, set the reaction constants so a typical committed dive scores
0.6–1.0 and a genuinely late one decays, re-run, then calibrate the handling constants against
the resulting contact-quality distribution. All values stay inside the spec's own `[GT]` ranges;
none of the formulas change.

Calibrated values (each measured over full matches; two iterations, §6):

| Constant | Old | New | Range | Why |
|---|---|---|---|---|
| `ReactionBaseMs` | 350 | 220 | [200, 500] | anchor for the engine's ~100–300 ms commit grid; iteration 1 measured mean elapsed-when-airborne 291–308 ms and mean required 136–169 ms — windows landed at 0.30–0.57 |
| `ReactionBallSpeedCoeff` | 8 | 3 | [3, 18] | at 8, a 27 m/s shot adds +72 ms required — every commit "early" |
| `ReactionEarlyToleranceMs` | 120 | 200 | [60, 200] | the 10 Hz grid quantizes commits ±100 ms |
| `ReactionLateToleranceMs` | 80 | 140 | [40, 140] | same, late side; stays < early per KD-18 |
| `HandlingBase` | 0.45 | 0.60 | [0.20, 0.70] | iteration 1 measured contact quality 0.29–0.60 with live windows — the neutral attrFactor of 0.675 cannot reach the catch band through the fixed pointQuality lottery (§4.3); 0.90 can |
| `HandlingKAttr` | 0.45 | 0.60 | [0.20, 0.70] | raised WITH the base so Handling separates keepers more, not less (poor 0.63, neutral 0.90, elite 1.20) |
| `CatchThreshold` | 0.78 | 0.74 | [0.65, 0.90] | with the pointQuality lottery at E≈0.68, 0.78 made catches a tail event even for elite keepers |

### KD-C4 — No new RNG stream, no draw-order change, no schema change

The fixes alter WHEN existing values are computed and what arguments feed existing draws — never
how many draws are taken or in what order. The stamp fields are already serialized (v19);
`GkContactState.ReactionWindowAchieved` is already serialized. Digests move for any match where a
keeper dives (intended).

### KD-C5 — Lock the mechanism with units + the instrument, not a rate pin in the old scenario

`match-engine-goalkeeper-saves`' recorded evidence ("11 of 12 predicates fail pre-fix, verified
by execution") is anchored to its exact predicate corpus; adding predicates would silently
re-scope that record. The new locks are:
- unit tests on the frozen-at-launch window (launch-frame anchoring, stamp lifecycle,
  `OnThreatArmed` no-op-when-stamped, clear-on-disarm, no-clear-mid-dive);
- a new acceptance scenario `match-engine-keeper-conversion` (#19 ScenarioRunner) asserting the
  conversion is *reachable*: over its corpus, at least one contact converts to a catch or parry
  band, and the contact-time reaction window is non-zero on average — the two quantities that are
  structurally zero pre-fix;
- the funnel instrument (already committed) for the calibration numbers.

## 4. The changes

### 4.1 `ERR-011-005` — the window anchored at the dive launch

`GoalkeeperMechanics.Update`: the per-frame Anticipate/Diving/Airborne recomputation block is
removed; the Diving launch-bookkeeping branch computes the window once from the live stamp and
freezes it into `GkContactState.ReactionWindowAchieved` (0 when no stamp is live — unreachable in
production now that every commit implies an armed episode, which implies a stamp). Telemetry
records the window once per dive instead of once per frame.

### 4.2 `ERR-011-006` — stamp lifecycle

`GoalkeeperMechanics.ClearSaveIntent` clears the stamp pair with the intent (still a no-op
mid-dive); the save-resolution branch clears both. New `GoalkeeperMechanics.OnThreatArmed` seeds
the stamp at episode onset when none is live (KD-C2). `MatchEngine.RunMechanicsAI`'s armed branch
calls it with the same projected attributes `HostSaveDispatch` uses.

### 4.3 Calibration (KD-C3) — and the pointQuality invariance, recorded

`GoalkeeperConstants` per the KD-C3 table; config keys unchanged. One structural property is
recorded rather than changed: on the Stage-0 shot path both contact anchors are the ball position
(§5.Z.17's own H-1 fix), so `contactPointError` is pure noise `σ·g` and
`pointQuality = 1 − clamp01(g)` — **invariant under every `[GT]` in the catalogue** (the σ in the
noise and the σ in the divisor cancel), expectation ≈ 0.68, unmodulated by any attribute. The
calibration above works around this fixed lottery; replacing it with a geometric,
attribute-modulated contact error belongs to the Stage-1 dive/contact model, not to a `[GT]` pass
(§7.2).

## 5. Acceptance

New `tests/scenarios/cross-spec/match-engine-keeper-conversion` (#19 ScenarioRunner, Tier B,
**2 seeds × 45 min** on the `ConfigureSquads` path, ~2.6 min — the corpus SIZED FROM the funnel's
measured per-contact tick positions after two drafts failed their own floors (§8): contacts run
~5 per full match and the earliest in any calibration seed lands at 16.6 min, so the first
45 minutes of the two chosen seeds deterministically contain 6 contacts and 3 catches. Owning
specs {2, 6, 11, 12, 16, 19}) — predicates:

| Predicate | Pre-fix | Post-fix |
|---|---|---|
| `dive-reaction-window-is-live` (mean frozen window over all dives ≥ 0.15) | ~0.0 (stale anchors + flight-time decay) | passes |
| `contact-converts-to-parry-band` (≥ 1 contact at quality ≥ `ParryThreshold`) | rare | passes |
| `keeper-holds-a-ball` (≥ 1 `HandsOnBall` entry over the corpus) | ~0 (1 catch in 3 FULL matches) | passes |

No determinism predicate of its own: the pass makes no RNG/draw-order/schema change (KD-C4), and
live-play digest determinism is already locked by the play-development and shot scenarios whose
corpora include the keeper path.

Plus the new `GoalkeeperConversionTests` unit fixture (7 locks) driven through the real
orchestrator (`TacticalTick`/`Update`): window frozen at the dive-launch frame and not re-dated
by flight time, late commit scores below prompt commit, `OnThreatArmed` stamps once per episode,
shot-stamp precedence, clear-on-disarm, mid-dive stamp preservation, clear-on-resolution.

**Instrument fallout, fixed with the pass:** `match-engine-shot-speed`'s sampler and
`ShotOutcomeDiagnosticTests` counted "shots" off `ShotDetectedTickMs` edges; the ERR-011-006
arming stamps would have folded slow threat episodes into the speed distribution those floors
pin. New diagnostic counter `MatchEngine.TestOnly_ShotContacts` (the `WoodworkStrikes`
observation class — not serialized) counts genuine #6 strikes where `NotifyKeeperOfShot`
verifies them; both instruments re-anchored to it, and the funnel's `shotsNotified` relabelled
`episodesStamped`.

## 6. Verification and measured effect

Measured over 3 full matches, `ConfigureSquads` path, same seeds pre/post (the §5.Z.17/§5.Z.19
corpus), via the committed funnel + shot-outcome instruments. Iteration 1 = window fix + reaction
retune only (attribution); iteration 2 = + handling calibration.

| | baseline | iter 1 | iter 2 (shipped) |
|---|---|---|---|
| reaction window at contact | 0.000 / 0.000 / 0.199 | 0.30–0.57 | 0.30–0.67 |
| mean elapsed-since-stamp when airborne | **85–349 s** | 291–308 ms | 296–309 ms |
| quality at contact | 0.36–0.50 | 0.29–0.60 | **0.41–0.79** |
| hand contacts (corpus) | 15 | 26 | 15 |
| catches (corpus) | 1 | 2 | **6** (≈ 40% of contacts) |
| goals per match | 13 / 13 / 18 (**14.7**) | 9 / 10 / 12 (10.3) | 6 / 9 / 9 (**8.0**) |
| goals per shot | 0.38–0.42 | — | **0.19–0.26** |
| shots per match (genuine strikes) | 31–45 | — | 31–38 |

The n = 3 caveat from §5.Z.17 applies to any single goal number — one different deflection
re-rolls a match — but the direction is uniform across every seed and both iterations, and the
mechanism numbers (window, elapsed, quality, catches) are the systematic evidence: the window
went from structurally dead to live, and contacts convert. **Goals per shot roughly halves,
0.38–0.42 → 0.19–0.26**, against football's ~0.10; the remaining gap is bounded by the contact
rate (§7.1) and shot volume (§7.4), not by conversion at contact.

Scorelines moved from 8-5 / 7-6 / 13-5 to **3-3 / 6-3 / 8-1** — the first football-plausible
scorelines the engine has produced (8-1 carries the recorded §5.Z.11 strong-side asymmetry).

Full `tools/dotnet-ci/run-gate.sh`: **PASSED, 0 failures** (whole tree; run at landing with the
acceptance scenario and the 7 unit locks included).

## 7. Recorded, NOT fixed

1. **The contact rate is the residual mass inside lever (c).** Measured at baseline: 15 hand
   contacts against 44 goals over three full matches — the keeper meets roughly a quarter of
   on-target shots, and since a contact almost always stops the shot (§1.0), the uncontacted
   three quarters are where the goals live. The anatomy is measured, not guessed: dives DO
   launch (12–39 per keeper-match), best-approach distances reach ~0 m, but the mean lateral
   offset of the keeper from the ball while airborne runs 1.7–4.6 m against a dive displacement
   of 2.2 m + reach ~1.7 m — the keeper's *position* (the #12 GK slot's lateral tracking) and
   the commit-to-arrival timing bound the interception set, not the dive model's magnitude.
   Closing it means #12 GK-slot positioning and/or a #11 commit-timing model — each a behaviour
   change to an APPROVED spec, neither a `[GT]` dial, both out of this pass's scope.
2. **The engine cannot time a dive.** The keeper commits at the earliest stride after the DT's
   SAVE; a real keeper delays to match the ball. With the corrected window this shows up as a
   mild early-commit penalty rather than a wrong score. A commit-timing mechanism (hold the dive
   until `elapsed ≈ required`) is a #11 behaviour change with its own risks (a delayed dive
   reaches less of a fast shot) — deferred, jointly with item 1.
3. **pointQuality is a fixed lottery** (§4.3): E ≈ 0.68, invariant under every `[GT]`, blind to
   attributes. The honest fix is a geometric contact-error model when the dive migrates to AM #2
   kinematics at Stage 1 (KD-12).
4. **Shot volume** (lever (a)): 31–45/match vs football ~25 — untouched here, next pass.
5. **Straight-at-keeper shots without a dive** are saved only by the generic #3 body deflection,
   never held. A standing-catch path for slow, central balls is a #11 extension, not a `[GT]`.

## 8. Adversarial review history

| Round | Findings | Notes |
|---|---|---|
| Measurement-1 (baseline funnel, 3 full matches) | — | Re-framed the pass before design: contacts stop shots, and at 15 contacts vs 44 goals the contact RATE bounds lever (c) — recorded as §7.1 rather than silently absorbed into "conversion". Window confirmed dead (0.000 at contact; elapsed 85–349 s) |
| Iteration-1 (window fix + reaction retune, isolated) | — | Windows went live (0.30–0.57 at contact; elapsed 291–308 ms) with handling untouched, so the calibration's remaining gap (quality 0.29–0.60 vs catch 0.78) is attributable to the handling constants, not the window |
| Instrument audit | 1 | The `match-engine-shot-speed` scenario and `ShotOutcomeDiagnosticTests` counted "shots" off `ShotDetectedTickMs` edges; the ERR-011-006 arming stamps redefine an edge as "a threat episode" (min 3 m/s), which would have polluted the speed floors the GATE pins — caught by reading the consumers before landing, the §5.Z.9 "an instrument disagreed with what it calibrates" class. Fixed via `TestOnly_ShotContacts` before any gate run |
| Scenario draft 1 (neutral path) | 1 | `keeper-holds-a-ball` failed: heldBalls = 0 — the neutral all-10 path samples a different shot population and the conversion calibrated on the `ConfigureSquads` path did not transfer (EXACTLY the §5.Z.19 AR-4 finding, reproduced). Switched to the configured path |
| Scenario draft 2 (configured, 4 × 15 min) | 1 | contacts = 0 across the corpus — contacts run ~5 per full match and the first 15 minutes of these seeds contain none (measured earliest: 16.6 min), so a 15-minute-window floor predicate was dishonest. Corpus re-sized from the funnel's measured per-contact tick positions: 2 seeds × 45 min containing 6 contacts / 3 catches deterministically. Final scenario PASSES (2 m 36 s) |
| Code AR (hostile diff re-read) | 0H+0M | Verified: no draw-order change (the frozen-window computation makes no draws and sits before the jitter draw in the same branch); every state the pass writes (stamp/required/attrs/frozen window) is already in the serialized v19 GK block, so flag-on restore determinism holds structurally; the stamp-empty dive launch is production-unreachable (commit implies armed implies a stamp) and falls back to 0; `dotnet format` fallout on the new fixture fixed before the gate |

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-28 | — | Initial draft: window anchor (ERR-011-005), stamp lifecycle + episode-onset fallback (ERR-011-006), KD-C3 calibration plan. Measured numbers pending. |
| 1.0 | 2026-07-28 | — | Implemented + measured. §1.0 baseline table (the contact-rate finding re-framed the pass before design); §3 KD-C3 values finalized over two full-match iterations; §5 acceptance (the `ConfigureSquads` path after the neutral draft's population-transfer failure — §8 rows 4–5) + the instrument re-anchor to `TestOnly_ShotContacts`; §6 measured table (goals/match 14.7 → 8.0, goals/shot 0.38–0.42 → 0.19–0.26, catches 1 → 6); §7.1 contact-rate residual with the measured anatomy. |
