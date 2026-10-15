# Perception — shared per-heartbeat pass and perceptual memory

> **Created:** October 15, 2026
> **Status:** DESIGN SUPPLEMENT — draft v0.2, not reviewed. Opens no numbered spec and changes no
> `SPEC_INDEX.md` row. It proposes amendments to Perception System **#7** (APPROVED), Decision Tree
> **#8** (APPROVED) and Deterministic Simulation **#16** (APPROVED); none of them is made here.
> **Purpose:** Records the owner's design decisions of October 14–15, 2026 for restructuring
> perception into one shared pass over all players plus a perception-filtered memory of recent
> positions. It also states what they imply for determinism, snapshot size, consumers and landing
> order, and lists what is still open. Nothing here is implemented.
> **Evidence base:** `docs/tracking/match-engine-throughput-profile.md` v1.0 (measured at `8344024`).

---

## 0. Governance and scope

- **Not a realism pass and not tuning.** Every numeric value below is a proposed `[GT]`
  placeholder, uncalibrated. KD-W1 is unaffected because nothing here touches a wired subsystem until
  a landing does.
- **Behaviour changes are sequenced, not bundled.** Per `path-to-playable-roadmap.md` §5, a
  behaviour-affecting merge during an A→B or B→C window invalidates that comparison. Phase P1 (§6) is
  digest-identical by construction and may land with neutrality evidence. Phases P2–P4 change match
  results and must be placed against the W8 windows and the D-08 PM-2 engine anchor.
- **Interfaces are created only when producer and consumer are both specified** (root `CLAUDE.md`).
  The shared pass therefore stays inside the `perception-system` assembly until a #8 amendment
  specifies its consumer (§3.5).
- **Determinism:** no new RNG stream, domain tag or draw site is proposed. All memory state is
  deterministic and serialized (§4).

## 1. Why — measured facts and code facts

From the throughput profile (one match, Linux .NET 8, non-certifying):

- Perception is ~32% of match time spent in a wasted spatial query, plus about half of the remaining
  ~26 s of simulation. Physics is ≈ 5–8% of baseline.
- `PerceptionSystem.RunAgentPipeline` queries `SpatialHashGrid` with `MaxPerceptionRange` = 120 m on
  a 1 m grid (~16,600 cell steps per call). An order-preserving occupied-cell query gave a
  byte-identical digest.

From the code at `8344024`:

| Fact | Where |
|---|---|
| Each target's occlusion test recomputes every occluder's shadow wedge (`sqrt`, `asin`, `atan2` per target–occluder pair); the target bearing is computed twice (FoV and occlusion) | `OcclusionFilter.IsOccluded`, `PerceptionSystem.cs:375–430` |
| Pressure sums **all** opponents in the 120 m candidate list; #7 §3.6.2 / §4.4.1 specify opponents within `PRESSURE_RADIUS` = 3.0 m via a dedicated query | `PressureEvaluator.cs:47–64` — **ERR candidate, not yet filed** |
| A perceived agent's position and velocity are ground truth, `ConfidenceScore` = 1.0 | `PerceptionSystem.cs` Step 4+5; #7 §7.1.2 plans Stage 1 error |
| An agent that leaves view stays in the snapshot for **one** heartbeat (100 ms) | #7 §3.3.6 `CONFIRMATION_EXPIRY_TICKS = 1` |
| The ball keeps a last-known position indefinitely with `BallStalenessFrames`; Anticipation-driven projection is planned | #7 §3.5.2, §7.1.3 |
| Pass options are generated **only** from `VisibleTeammates`, so a teammate out of view cannot be passed to | `OptionGenerator.cs:136–177` |
| Perception receives only `Decisions`, `Anticipation`, `TeamId`, `IsHalfTurned`; `Concentration` and `Vision` exist in `PlayerAttributes` but are not plumbed in | `PerceptionAgentAttributes`, `PlayerAttributes.cs` |
| Team-level AI (pressing, defensive, attacking) reads ground-truth positions, not `FilteredView` | `MatchEngine.cs` `Fill*Snapshot` (4222–4470) |
| Engine snapshot schema is v24; a perception tick-state block is already serialized | `MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION`, `WritePerceptionTickState` |

## 2. Owner decisions (October 14–15, 2026)

| ID | Decision |
|---|---|
| **OD-1** | One shared perception pass per AI heartbeat computes, for every player, the inputs every observer needs: pressure, effective FoV angle, the viewing cone, occlusion and the visible set. Per-player attribute filtering then shapes what reaches each decision tree. |
| **OD-2** | **All players keep the same update cadence** (10 Hz AI heartbeat). Relevance-based or per-player tick-rate variation is rejected as a future maintenance risk. |
| **OD-3** | Players get **memory** of positions seen in the recent past, implemented as a **shared position history plus a per-observer visibility record** (the "option (b)" design). A single last-seen record per pair was rejected: at 10 Hz it adds almost nothing over the current view and cannot hold a path. |
| **OD-4** | Memory must hold **paths**, not only last positions. That is what lets a player attempt a **blind pass** to a teammate they last saw making a run. |
| **OD-5** | Memory capability **scales continuously with mental attributes**. There are no on/off thresholds, which is consistent with #7 §7.4.1's rejection of awareness enums. |
| **OD-6** | Pipeline order becomes **pressure → FoV → visibility (cone, occlusion) → range**. Range is the last filter, not candidate enumeration. |

## 3. Proposed design

### 3.1 Shared pass (per AI heartbeat, all 22 players)

1. **Pair table.** For each ordered pair (i, j), compute: distance, bearing i→j, shadow half-angle
   of j as seen from i. These are computed once and are not serialized, being derived per heartbeat.
   - P1 form: **directed** (462 entries), computed with the same expressions as today so results stay
     byte-identical.
   - P2 form (optional): **symmetric** (231 pairs, bearing j→i = bearing i→j ± 180°). This halves the
     work again but is not bit-identical, so it carries a digest change.
2. **Pressure** per player from the pair table: opponents within `PRESSURE_RADIUS` = 3.0 m, with the
   #7 §3.6.2 formula unchanged. Conforming to the spec changes the pressure value, so this is P2.
3. **Effective FoV** per player: `FovCalculator.ComputeEffectiveFoV(Decisions, pressure)`, unchanged.
4. **Visibility** per observer, for each other player:
   - inside the cone (bearing vs facing);
   - not inside a nearer opponent's shadow wedge (from the table);
   - within `MaxPerceptionRange`.
   The range check **must precede the recognition-latency update**. Otherwise an out-of-range player
   in the cone would accrue confirmation progress.
5. **Outputs:**
   - the per-observer visible set, as a 22-bit mask;
   - the current `FilteredView`, built as today, with recognition latency, shoulder checks and ball
     perception unchanged in P1–P2.

Occluders: all players, not the old 120 m list. These are equivalent, since any body between an
observer and an in-range target is itself in range. The W4 goalkeeper query keeps its all-bodies
variant against the same table.

### 3.2 Perception history (OD-3, OD-4)

Two ring buffers, written once per AI heartbeat **after** the shared pass:

| Buffer | Content | Size at H = 30 frames (3.0 s) |
|---|---|---|
| `PositionHistory[frame][agent]` | 2D position + 2D velocity, ground truth, `float` | 22 × 16 B × 30 = **10,560 B** |
| `VisibilityHistory[frame][observer]` | 22-bit mask (in a `uint`) of agents that observer **saw** this frame, i.e. passed visibility *and* is confirmed by recognition latency | 22 × 4 B × 30 = **2,640 B** |
| write cursor + frame stamp | `int` | negligible |

- `H` = 30 frames (3.0 s at 10 Hz), **owner decision October 15, 2026** (reduced from the v0.1 proposal
  of 5 s to cut per-tick snapshot hashing). Constraint: `H` ≥ `W_MAX` in heartbeats, so the buffer
  never limits retention; at the proposed `W_MAX` = 2.0 s that leaves 1.0 s of headroom.
- Including the ball is **OQ-6**. The ball already has its own last-known/staleness mechanism.

### 3.3 Memory query

`QueryMemory(observer, target, now)` returns the samples `(age, position, velocity)` for frames
`f` with `now − f ≤ W(observer)` **and** `VisibilityHistory[f][observer]` has `target`'s bit set,
newest first.

**Invariants (each needs its own test):**

- **M-1 — no leakage.** A query never returns a frame in which the observer did not see the target.
  The history stores ground truth; the mask is what makes memory perceptual. This is the
  load-bearing correctness rule.
- **M-2 — window bound.** `W(observer) ≤ H` frames, always.
- **M-3 — deterministic and RNG-free.** It is a pure function of the buffers and attributes.
- **M-4 — current view unchanged.** Memory is a *separate* channel. `FilteredView.Visible*` keep
  meaning "seen now" (plus the existing §3.3.6 one-tick expiry until it is formally subsumed, OQ-7).

### 3.4 Attribute scaling (OD-5) — proposed, all `[GT]`, uncalibrated

The mapping of attributes to roles is **OQ-1**. Proposal:

- **Retention window** from `Concentration` (C, 1–20):
  `W(C) = W_MIN + (C − 1) / 19 × (W_MAX − W_MIN)`, with `W_MIN` = 0.5 s and `W_MAX` = 2.0 s, floored
  to whole heartbeats. Range 0.5–2.0 s.
- **Path projection** from `Anticipation` (A, 1–20), reusing #7 §7.1.3's indicative scalar:
  `P_proj = P_last + V_last × Δt × S(A)`, with `S(A) = 0.3 + (A − 1) / 19 × 0.7` (dimensionless,
  0.3–1.0). `Δt` is the sample age in seconds; positions are in metres and velocities in m/s.
- **Memory confidence**: `c = 1 − Δt / W(C)`, range 0–1, exposed to #8 as a continuous value.
  Thresholds belong to the consumer (#7 §7.4.1).
- **Blind-pass willingness / precision** from `Vision`. This is a #8 concern; see §3.5.

**Worked example.** A midfielder with C = 15 and A = 12 last saw a teammate 1.0 s ago at
(60.0, 30.0) m, moving at (4.0, 0.0) m/s.
- `W` = 0.5 + 14/19 × 1.5 = 1.605 s → 16 heartbeats, so the sample at age 1.0 s is retained.
- `S` = 0.3 + 11/19 × 0.7 = 0.705.
- `P_proj` = (60.0 + 4.0 × 1.0 × 0.705, 30.0) = **(62.82, 30.0) m**.
- `c` = 1 − 1.0 / 1.605 = **0.377**.

The true position, if the run continued, is (64.0, 30.0) m. The 1.18 m shortfall is the intended
cost of imperfect anticipation.

Plumbing: `PerceptionAgentAttributes` gains `Concentration` (and `Vision` if #8 needs it
there), populated from `PlayerAttributes` at boot like the existing fields.

### 3.5 Consumers

- **Decision Tree (#8), the blind pass.** `OptionGenerator` today builds pass candidates only from
  `VisibleTeammates`. A blind pass needs:
  - candidates from **remembered** teammates, with projected position and memory confidence `c`;
  - a utility penalty growing as `c` falls, scaled by `Vision`;
  - execution against the *projected*, not true, receiver position.

  Opponents' remembered paths should feed interception risk. This is a #8 amendment and is the
  consumer that justifies the memory API (§0).
- **Team-level AI** keeps reading ground truth in this design. Whether shape, pressing and marking
  should consume perception or memory is **OQ-5**.
- **Subsumable existing mechanisms:** the §3.3.6 one-tick expiry, the ball's staleness model and
  §7.1.3 ball prediction are special cases of memory. Folding them in is OQ-6/OQ-7 and is not
  required for P3.

## 4. Determinism and snapshot

- **Cross-tick state.** Both history buffers and the cursor survive heartbeats, so they **must** be
  serialized: next free `SNAPSHOT_SCHEMA_VERSION` after v24, allocated from the landed tree per
  roadmap §5. Requirements:
  - exact `float` bits; quantizing would break replay;
  - a symmetric writer and reader;
  - a version pin, a single-field digest probe and a save/restore round-trip (the
    `snapshot-schema-bump` procedure).
- **Derived state is not serialized.** The pair table, pressure, FoV and current masks are recomputed
  every heartbeat.
- **Size and cost.** The history adds ≈ **13.2 KB** to a payload that is 35,252 B today (+~37%). The
  per-tick snapshot already costs ~35% of match time (serialize 13.7 s + digest 13.3 s per match), so
  P3 would add roughly 10 s per match on the measured host unless the per-tick snapshot becomes
  on-demand. That estimate is a linear extrapolation, unmeasured.
  - **Dependency D-16:** the #16 decision on whether headless/unwatched matches need the per-tick
    digest chain (throughput profile §3.2) should be taken **before P3**. Otherwise memory consumes
    most of the P1 speed gain.

## 5. Expected cost and benefit (none of it measured yet)

| Phase | Compute effect | Evidence |
|---|---|---|
| P1 query fix | −24.9 s/match | measured, digest-identical |
| P1 pair table (directed) | removes most of occlusion's ~3.5 s and the duplicate bearings | estimate; needs a function-level profile |
| P2 symmetric table, ID-order iteration | a further small reduction | estimate |
| P3 history write + queries | small compute; **+~13.2 KB snapshot per tick** | arithmetic; see §4 |
| P4 blind pass | DT option count rises slightly | none |

## 6. Landing plan

| Phase | Content | Results change? | Spec work |
|---|---|---|---|
| **P1** | Order-preserving query fix; directed pair table / shadow-wedge precompute; shared bearing | **No**: digest identity across the seed set is the acceptance test | None (implementation of existing #7 behaviour) |
| **P2** | Pressure ERR (3 m conformance); OD-6 reorder; ID-order iteration; optional symmetric table | **Yes**: one deliberate digest change | ERR against #7 + #7 §3.0 / §3.6 edit, same commit |
| **P3** | History buffers, memory query, attribute scaling, `Concentration` plumbing, snapshot bump | **Only if consumed**: write-only history changes the payload and digest but not play | #7 amendment (new memory section, §3.3.6 relation); after D-16 |
| **P4** | Blind pass and remembered-opponent risk in #8 | **Yes** | #8 amendment; calibration under KD-W1 once wired |

Blast radius for P2–P4:
- A4a quick-sim corpus and fit (any goal-rate change reopens it, KD-8);
- W8 baselines and windows;
- scenarios with hard-coded tick windows or per-90 bands;
- the FR-PO-052 perf baseline.

## 7. Open questions

| ID | Question |
|---|---|
| OQ-1 | Attribute roles: Concentration → retention, Anticipation → projection, Vision → blind-pass use? Or another mapping (e.g. Decisions also affecting retention)? |
| OQ-2 | `W_MIN`/`W_MAX` (0.5/2.0 s) are owner values or calibration targets? (`H` = 3.0 s is decided; it must stay ≥ `W_MAX`.) |
| OQ-3 | Should a remembered target reappearing near its projected position get a shortened recognition latency? |
| OQ-4 | Should pressure narrow the FoV for players **off** the ball (#7 §3.1.4)? The formula is borrowed from First Touch ball control. |
| OQ-5 | Should team-level AI consume perception/memory instead of ground truth? |
| OQ-6 | Should the ball join the shared history (replacing §3.5.2 staleness and absorbing §7.1.3)? |
| OQ-7 | Should §3.3.6's one-tick expiry be formally replaced by memory, keeping `Visible*` strictly "seen now"? |
| OQ-8 | Interaction with #7 §7.1.2 (Stage 1 position error): does remembered position also carry error, and from what seed? |
| OQ-9 | Teammate occlusion (#7 §7.1.1) changes visibility masks and therefore memory; land before or after P3? |
| D-16 | #16: on-demand per-tick digest/serialization for headless matches (prerequisite for P3's snapshot cost). |

## 8. Risks

- **Ground-truth leakage through memory** (M-1). One missed mask check silently makes players
  omniscient. Mitigation: a dedicated test that fails if any returned frame has the observer's bit
  clear.
- **Snapshot growth** (§4) eats the speed gained in P1 if D-16 is not taken first.
- **Stacked behaviour changes** invalidate calibration repeatedly. Mitigation: P2 lands as one
  deliberate change, P3 is write-only until P4, and P4 is calibrated once.
- **#8 complexity.** The blind pass adds a candidate source with uncertainty. The DT must not assume
  `PerceivedPosition` == truth (#7 §7.1.2 already requires this).

## Version History

| Version | Date | Change |
|---|---|---|
| v0.2 | October 15, 2026 | Owner decision: history buffer `H` reduced from 50 to 30 frames (5.0 → 3.0 s) to cut per-tick snapshot hashing. History size ≈ 22 KB → 13.2 KB per tick (+~63% → +~37% payload); estimated P3 snapshot cost ≈ 15–17 → ≈ 10 s/match (linear extrapolation, unmeasured). Constraint `H` ≥ `W_MAX` stated. |
| v0.1 | October 15, 2026 | Created: owner decisions OD-1–OD-6 recorded; shared pass, history/memory, attribute scaling, snapshot impact, landing phases P1–P4 and open questions drafted. Not yet reviewed (`/advisor` pending). |
