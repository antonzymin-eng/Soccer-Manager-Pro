# Perception — shared per-heartbeat pass and perceptual memory

> **Created:** October 15, 2026
> **Status:** DESIGN SUPPLEMENT — draft v0.3, revised after review (advisory council integrity +
> evidence on v0.2; external review of the council's synthesis). Opens no numbered spec and changes
> no `SPEC_INDEX.md` row. It proposes amendments to Perception System **#7**, Decision Tree **#8**
> and Deterministic Simulation **#16** (all APPROVED); none of them is made here.
> **Purpose:** Records the owner's design decisions of October 14–15, 2026 for restructuring
> perception into one shared pass over all players plus a perception-filtered memory of recent
> positions. It also states the constraints that keep each landing correct, the measurements that
> must precede the behaviour changes, the landing order and what is still open. Nothing here is
> implemented.
> **Evidence base:** `docs/tracking/match-engine-throughput-profile.md` v1.0 (measured at `8344024`;
> one seed, Linux .NET 8, non-certifying).

---

## 0. Governance and scope

- **Not a realism pass and not tuning.** Every numeric value below is a proposed `[GT]`
  placeholder unless marked as an owner decision. KD-W1 is unaffected until a landing wires
  something.
- **Motivation, stated narrowly.** P1 does **not** bring background fixtures through the real engine
  within reach. Roadmap C1 budgets the quick-sim at ≲ 10 ms/match, and even the ablated floor
  (~26 s/match) is ~2,600× over it. P1's value is faster live-measurement loops (W8 windows,
  diagnostics, sweeps) and cheaper calibration captures (A4a: 198 × 78 s ≈ 4.3 worker-h →
  ≈ 2.9 worker-h after the query fix), and closing a zero-allocation contract gap (§6, P1b).
  The memory work (P3–P4) is motivated by football behaviour, not speed.
- **Sequencing rules that actually exist.**
  - Roadmap §5: a behaviour-affecting merge during an A→B or B→C window invalidates that
    comparison. P1 items are digest-identical and may land with neutrality evidence. P2–P4 change
    results and must not land inside those windows.
  - Whether P2–P4 belong in PM-2 is a D-08 question (the PM-2 engine anchor and scope) or a **new
    registered decision**. D-05 governs only #434/#440 and does **not** constrain this work.
- **Interfaces only when producer and consumer are both specified** (root `CLAUDE.md`). The memory
  query is a #7 → #8 interface, so P3 cannot land before #8 specifies its consumer (§6).
- **Determinism.** No new RNG stream, domain tag or draw site is proposed. All memory state is
  deterministic and serialized (§4).

## 1. Facts this design depends on

### 1.1 Measured (throughput profile)

- Baseline 78.1 s/match. An order-preserving occupied-cell variant of the perception spatial query
  gave **53.2 s** with a **byte-identical final digest** (one seed). Additionally skipping the
  per-tick digest gave 39.9 s, and also skipping serialization gave 26.2 s.
- The per-tick snapshot payload is 35,252 B. Its serialization costs 13.7 s and its digest 13.3 s
  per match. The digest step allocates ~11.5 GB per match.
- Profile shares below that level (occlusion ≈ 3.5 s, physics ≈ 5–8%) are **low confidence**: the
  ~100 Hz sampling profile and the ablations disagree on physics.

### 1.2 Code facts at `8344024`

| Fact | Where |
|---|---|
| Each target's occlusion test recomputes every occluder's shadow wedge (`sqrt`, `asin`, `atan2` per target–occluder pair). The target bearing is computed with the same `Mathf.Atan2(delta.y, delta.x) * RAD_TO_DEG` expression in FoV and occlusion. | `OcclusionFilter.cs:131,167`; `FovCalculator.cs:67` |
| Occlusion depth ordering compares **squared** distances (`occluderDistSq >= targetDistSq`). Shadow half-angle and pressure use `.magnitude`. | `OcclusionFilter.cs:38,158`; `PressureEvaluator.cs:60` |
| Candidates arrive in grid-scan order (cell row, cell column, insertion order). That order drives the pressure float sum, the entry order of `VisibleTeammates` / `VisibleOpponents` / `BlindSidePerceivedAgents`, and ball occlusion. | `SpatialHashGrid.cs:124–139`; `PerceptionSystem.cs:375–517` |
| Players are inserted into the perception grid as points (radius 0), so perception queries never return duplicates. Collision queries can. | `MatchEngineConstants.PERCEPTION_GRID_POINT_INSERT_RADIUS = 0f`; `PopulatePerceptionGrid` |
| **No range test runs for agents today.** The 120 m square covers the whole pitch. Only the ball has a distance check. The pitch diagonal is 125.1 m. | `PerceptionSystem.cs:328`; `BallPerceptionEvaluator.cs:66` |
| Pressure sums **all** opponents in the candidate list. #7 §3.6.2 / §4.4.1 specify opponents within `PRESSURE_RADIUS` = 3.0 m. | `PressureEvaluator.cs:47–64` — **ERR candidate (next free id `ERR-007-006`), not yet filed** |
| Pressure is **not FoV-only**. It also reaches the Decision Tree as pass urgency. #7 §3.6.3's "exactly one use: FoV narrowing" is wrong. | `MatchEngine.cs:3366–3369`; `OptionGenerator.cs:145` |
| Perception's `PressureEvaluator` is a second copy of First Touch #4's rule (`internal`, inclusive `distSq ≤ r²`). | `first-touch/PressureEvaluator.cs:26,53` |
| **#7 §3.3.6 is not implemented as written.** `RecognitionLatencyTracker.ProcessInvisible` keeps recognition confirmed through one invisible heartbeat and returns `bool`. `RunAgentPipeline` ignores the return value and outputs only entities seen this frame, so a player who leaves view disappears from the view immediately. | `RecognitionLatencyTracker.cs:124`; `PerceptionSystem.cs` second pass — **ERR candidate** |
| The W4 goalkeeper all-body line-of-sight query (#7 §3.2.7, `ERR-007-005`) runs in `RunMechanicsAI` **before** the perception heartbeat, and again in the 60 Hz Resolve phase. It targets the ball and skips sent-off bodies. | `MatchEngine.cs:3301` vs `3329`; `3574`; `5121`; `OcclusionFilter.cs:89–115` |
| Sent-off players are still inserted into the perception grid each heartbeat, so they remain perception targets, occluders and pressure sources. | `MatchEngine.cs:3384–3393` |
| `HandleForcedRefresh` runs the pipeline between heartbeats. It has no production caller today. | `PerceptionSystem.cs:251–280`; #7 §4.6.2 |
| A perceived agent's position and velocity are ground truth (`ConfidenceScore` = 1.0). #7 §7.1.2 plans Stage 1 position error. | `PerceptionSystem.cs` Step 4+5 |
| Pass options come **only** from `VisibleTeammates`; zero visible teammates means no pass option. | `OptionGenerator.cs:136–177` |
| Perception receives only `Decisions`, `Anticipation`, `TeamId`, `IsHalfTurned`. `SubstitutePlayer` re-projects these but resets no per-slot perception state. | `PlayerAttributeProjection.ToPerception`; `MatchEngine.cs:1622–1650` |
| Team-level AI (pressing, defensive, attacking) reads ground truth, not `FilteredView`. | `MatchEngine.cs` `Fill*Snapshot` (4222–4470) |
| The engine snapshot schema is v24. `MaxSnapshotBytes` = 65,536. #16 mandates a per-tick digest **chain** (`PrevSnapshotDigest`, `ERR_DS_DIGEST_CHAIN_BREAK`). | `MatchEngineConstants.cs:317`; `DeterministicSimConstants.cs:232,371`; #16 §3 |

## 2. Owner decisions (October 14–15, 2026)

| ID | Decision |
|---|---|
| **OD-1** | One shared perception pass per AI heartbeat computes, for every player, the inputs every observer needs: pressure, effective FoV angle, the viewing cone, occlusion and the visible set. Per-player attribute filtering then shapes what reaches each decision tree. |
| **OD-2** | **All players keep the same update cadence** (10 Hz AI heartbeat). Relevance-based or per-player tick-rate variation is rejected as a future maintenance risk. |
| **OD-3** | Memory is a **shared position history plus a per-observer visibility record**. A single last-seen record per pair was rejected: at 10 Hz it adds almost nothing over the current view and cannot hold a path. |
| **OD-4** | Memory must hold **paths**, so that a player can attempt a **blind pass** to a teammate they last saw making a run. |
| **OD-5** | Memory capability **scales continuously with mental attributes**, with no on/off thresholds (consistent with #7 §7.4.1). |
| **OD-6** | Pipeline order becomes **pressure → FoV → visibility (cone, occlusion) → range**. |
| **OD-7** | History length `H` = **30 heartbeats (3.0 s)**, reduced from the v0.1 proposal of 5 s to cut per-tick snapshot hashing. |

## 3. Design

### 3.1 Shared pass — P1 form (digest-identical)

P1 restructures the computation without changing any result. Its constraints are binding:

1. **Query fix lives in `SpatialHashGrid`** (Collision #3 code). For queries whose square is large
   relative to the occupied-cell count, iterate occupied cells inside the square **sorted by cell
   index**. Small-radius queries (collision's hitbox query) keep the original path; the profile's
   over-broad variant made collision slower. Kept in the grid, this is spec-neutral.
   - If perception instead stops calling the grid, #7 §3.0 Step 1 and §4.4.1 ("Perception does
     not implement its own proximity search") must be edited, with an ERR, in the same commit.
2. **Pair table** (directed, per heartbeat, derived, not serialized). For each ordered pair (i, j)
   it stores:
   - squared distance;
   - distance, computed with the same `.magnitude` call as today;
   - bearing i→j;
   - shadow half-angle of j from i.

   Distance, squared distance and shadow half-angle are bit-exact under symmetry (`a−b = −(b−a)` in
   IEEE arithmetic), so they **may** be computed once per unordered pair in P1. Bearings may not.
3. **Candidate order is preserved** everywhere it is observable:
   - the pressure sum;
   - the order of `Visible*` and `BlindSide*` entries;
   - ball occlusion.

   ID-order iteration is a P2 change.
4. **No Euclidean range test is added in P1.** It would change behaviour for corner-to-corner pairs.
5. **Occluders** remain exactly the set used today. The equivalence "all players ≡ within 120 m"
   holds because the square already covers the pitch, not because of geometry. Sent-off players
   stay in the set in P1, as they are today.
6. **Out of scope for the table:**
   - The **W4 goalkeeper query** keeps its own computation. It runs before the heartbeat and at
     60 Hz, so a heartbeat table would be missing or stale.
   - **`HandleForcedRefresh`** must compute its own row from its own captured positions. The API
     must not let it read the heartbeat table.

### 3.2 Shared pass — P2 form (deliberate behaviour changes)

Each item below is a separate, separately measured change (§6):

- **Pressure conformance** (`ERR-007-006` if still free at filing): Euclidean `distSq ≤ PRESSURE_RADIUS²`.
  - Do **not** implement it as "a spatial-hash query at 3 m". On this grid that returns a 7 × 7-cell
    box, admitting opponents up to ~5.7 m away.
  - Reuse First Touch #4's evaluator (expose it, or `InternalsVisibleTo`), or keep the copy and pin
    it to #4's output with a test.
  - Patch #7 §3.6.2, §4.4.1, §4.4.2 and §3.6.3 (pressure's Decision Tree use) in the same commit.
- **OD-6 order with a range test.** The range check must precede the recognition-latency update.
- **ID-order iteration** and **symmetric bearings** (bearing j→i = bearing i→j ± 180°).
- **Sent-off players** stop being targets, occluders and pressure sources. This needs an explicit
  rule, matching how W4 already excludes them.

### 3.3 Perception history (OD-3, OD-4, OD-7) — P3

Ring buffers, written once per AI heartbeat **after** the shared pass:

| Buffer | Content | Size (H = 30) |
|---|---|---|
| `PositionHistory[slot][agent]` | 2D position + 2D velocity, ground truth, `float` | 22 × 16 B × 30 = 10,560 B |
| `VisibilityHistory[slot][observer]` | 22-bit mask: bit *j* set **iff** agent *j* appears in that heartbeat's built `FilteredView` for this observer (`VisibleTeammates`, `VisibleOpponents` or `BlindSidePerceivedAgents`, so shoulder-check sightings count) | 22 × 4 B × 30 = 2,640 B |
| `SlotStamp[slot]` | heartbeat number written into the slot; a sentinel marks never-filled slots | 30 × 4 B = 120 B |
| cursor | `int` | 4 B |

- **`H` is a compile-time `[FIXED]` constant**, not a `GameplayConfig` `[GT]`, because it sets the
  payload layout. Boot asserts `H ≥ W_MAX` in heartbeats.
- **Per-slot stamps, not one stamp**, so sample ages stay exact after a mid-buffer restore and
  whenever a heartbeat is skipped (whether the AI phase ever skips during stoppages is unverified;
  see the §5 measurement plan).
- **Reset rules** must be specified (OQ-10). Today an incoming substitute would inherit the outgoing
  player's memory row, and everyone's memory of that slot. Sent-off players and kickoff resets also
  need rules.
- Including the ball is OQ-6.

### 3.4 Memory query and invariants — P3

`QueryMemory(observer, target, now)` returns the samples `(age, position, velocity)` from slots
with a valid stamp, `age ≤ W(observer)`, and the observer's bit for `target` set, newest first.

- **M-1 — no leakage.** The mask bit is set **iff** the target was in that heartbeat's built
  `FilteredView` for that observer. The test's oracle must be the built `FilteredView`, not internal
  flags: a target in the cone but still within recognition latency, or occluded, must never be
  returned.
- **M-2 — precision.** Returned values are never more precise than what the observer perceived at
  the sample frame. Today perceived equals true, so storing ground truth is equivalent. Once #7
  §7.1.2 adds position error, the history must store the perceived value (or an equivalent), or
  memory would be more accurate than perception.
- **M-3 — window bound.** `W(observer) ≤ H` heartbeats, asserted against the constants.
- **M-4 — deterministic and RNG-free.**
- **M-5 — current view unchanged.** Memory is a separate channel. It is handed to #8 **by value**
  (a remembered-agents block alongside `FilteredView`), per #7 §3.0 Step 7, not as a live handle.
- **M-6 — mirrored.** Every geometry fixture has an away-team twin (ERR-008-002 pattern).

### 3.5 Attribute scaling (OD-5) — proposed, uncalibrated

Which attribute plays which role is **OQ-1**. Proposal:

- **Retention window** from `Concentration` (C, 1–20), as an integer number of heartbeats so float
  rounding cannot move a boundary:
  `W_hb(C) = W_MIN_hb + ((C − 1) × (W_MAX_hb − W_MIN_hb)) / 19` (integer division), with
  `W_MIN_hb` = 5 and `W_MAX_hb` = 20, i.e. 0.5–2.0 s. These are `[GT]` and **not established**.
  Football scanning studies measure head-turn frequency, not how long a player retains a specific
  teammate's position, so they do not set this range. The range must come from the
  observation-age measurement in §5.
- **Path projection, unbiased.** `P_proj = P_last + V_last × Δt`, with `Δt` the sample age in
  seconds, positions in metres and velocities in m/s.
  - Anticipation sets the **uncertainty**, not a systematic shortfall:
    `σ(Δt, A) = σ_RATE × Δt × (1 − 0.7 × (A − 1) / 19)`, in metres. `σ_RATE` is `[GT]`, ~1.0 m/s
    proposed.
  - v0.2 reused #7 §7.1.3's ball scalar `S(A)`, which under-projects by up to 70%: a runner at
    8 m/s seen 1.5 s ago lands 3.6 m along instead of 12 m. That is withdrawn.
  - Projection is capped at #7 §7.1.3's `T_PREDICT_MAX` (~1.5 s) unless the #7 amendment
    deliberately reconciles the two.
- **Memory confidence**: `c = 1 − age_hb / W_hb(C)`, range 0–1, from the same integer quantities.
  Thresholds belong to the consumer (#7 §7.4.1).
- **Vision** governs blind-pass willingness and precision in #8 (§3.6).

**Worked example.** A midfielder with C = 15 and A = 12 last saw a teammate 10 heartbeats (1.0 s)
ago at (60.0, 30.0) m, moving at (4.0, 0.0) m/s.

- `W_hb` = 5 + (14 × 15) / 19 = 5 + 11 = **16** heartbeats (1.6 s), so the sample is retained.
- `P_proj` = (60.0 + 4.0 × 1.0, 30.0) = **(64.0, 30.0) m**.
- `σ` = 1.0 × 1.0 × (1 − 0.7 × 11/19) = **0.595 m**.
- `c` = 1 − 10/16 = **0.375**.

If the run bent or stopped, the projection is wrong by more than σ. That is the intended cost of
single-sample extrapolation. OQ-11 asks whether #8 should use the multi-sample path.

Plumbing: `Concentration` (and `Vision` if #8 consumes it through perception) is added to
`PlayerAttributeProjection.ToPerception`, so substitution re-projects it. The `_perceptionAttrs`
exclusion proof in `SerializeWorldState` is extended.

### 3.6 Consumers — P4

- **Decision Tree (#8), the blind pass.**
  - Pass candidates from **remembered** teammates, carrying projected position, σ and `c`.
  - A utility penalty that grows as `c` falls and σ grows, modulated by `Vision`.
  - Execution against the **projected** receiver point.
  - Whether Pass Mechanics #5 can aim at a non-agent point for a teammate pass is **unverified**
    (OQ-12).
  - Opponents' remembered paths feed interception risk.
  - A blind pass in flight carries cross-tick state (projected receiver point, blind flag). That
    belongs in serialized Decision Tree / pass-executor state, the same hazard class as
    `_passInFlightReceiverId` (ERR-012-011).
- **Team-level AI** keeps reading ground truth (OQ-5).
- **Subsumable mechanisms:** the ball staleness model and §7.1.3 ball prediction (OQ-6). The
  §3.3.6 expiry is subsumable only after its own ERR resolves the spec/code mismatch (OQ-7).

## 4. Determinism and snapshot

- **One schema revision for P3 + P4** if they land together (§6). Two are needed only if separately
  landed versions expose different layouts.
- The history is written as a trailing block on the next free `SNAPSHOT_SCHEMA_VERSION` after v24,
  allocated from the landed tree per roadmap §5. Never reserve a version by calendar. Requirements:
  - exact `float` bits;
  - symmetric writer and reader;
  - a version pin and a single-field digest probe;
  - a save → restore → tick-on byte-equality test, including a restore mid-buffer;
  - the `snapshot-schema-bump` procedure.
- **Derived state is not serialized:** the pair table, pressure, FoV and current masks. A
  mid-match save/restore must reproduce the uninterrupted run's digests, which proves this.
- **Size.** About +13.3 KB per tick on a 35,252 B payload. That totals ~48.6 KB plus the event
  ledger's peak against `MaxSnapshotBytes` = 65,536. The peak must be measured (§5).
- **Cost.** About 10 s/match more by linear extrapolation. The digest part scales linearly with
  bytes; serialization may not. It must be measured (§5).
- **#16 digest-chain decision** (to be registered under the next free roadmap decision id; v0.1–v0.2
  called it "D-16", which collides with nothing yet but is unregistered).
  - Skipping per-tick digests is not permitted under current #16. Making them on-demand means a
    #16 amendment plus codec and save-format changes, because each record carries
    `PrevSnapshotDigest` and the codec enforces the chain.
  - A forced full-chain audit mode must remain, since P1's own acceptance proof uses the per-tick
    chain.
  - This decision should precede P3. Otherwise memory consumes much of the P1 gain.

## 5. Measurement plan (before the behaviour changes)

| # | Measurement | Gates | Notes |
|---|---|---|---|
| E1 | **Query sequence differential.** The old full-square scan kept as a test-only reference. Assert the **complete returned sequence, including duplicates**, is identical across a few thousand SplitMix64-seeded configurations: co-located agents, squares clipped at every edge, off-pitch and clamped positions, multi-cell (radius > 0) inserts, 21 and 20 agents. | P1a | Permanent lock. Verify it can fail: remove the sort and confirm red. |
| E2 | **Full-match digest identity, per tick.** Old and new **per-tick digest chains** compared within each runtime, over the **six frozen W8 seeds** (`w8-stage-a-preregistration.md:7`; keeper option on, default squads). Plus at least one configured-squad, keeper-option-off seed and one seed with a sending-off. Both baseline and candidate runs on the pinned Unity host as well as Linux. | P1a, P1c | Landing evidence, not a permanent lock (pinned digests break on the next behaviour change). |
| E3 | **Allocation and digest identity of the hashing fix.** Identical digest bytes and identical preimage and validation behaviour. Steady-state allocation measured after warm-up on .NET **and** Unity. | P1b | `IncrementalHash.GetHashAndReset()` still allocates the result. Zero allocation needs a reused hasher writing into a pre-allocated buffer; Unity Mono support must be checked. |
| E4 | **Function-level perception cost** via accumulating Stopwatch/ProfilerMarker scopes around FoV, occlusion, latency, shoulder checks and ball, on the post-P1a build. Not 100 Hz sampling. | P1c | P1c proceeds only if a **relative** threshold set after this measurement is met. It weighs percentage of match time saved, allocation and maintenance cost; there is no fixed seconds cutoff. |
| E5 | **Unity-host speed** pre/post P1a with `MatchEngineCapstonePerfHarnessTests` (`TD_PERF_RUN_COUNT`). | P1a close-out | Shifts the FR-PO-052 baseline. |
| E6 | **Observation ages at decision time.** At each on-ball decision, for each teammate and opponent: whether they are in view, and the age of the observer's most recent sighting. Plus the distribution of not-seen gaps per pair. | OD-5 ranges, P3 | Premise check: if teammates are nearly always in view, memory is inert; if ages usually exceed `W`, memory is empty when needed. Take it after the pressure fix, or both before and after. |
| E7 | **Pressure fix effect.** Distribution of pressure scalar, effective FoV and DT pass urgency, current rule vs 3 m rule. | P2a | Offline replay gives shape; a live run gives the value. |
| E8 | **Pass outcome diagnostic** with fixed definitions **before** collection: attempt (CONTACT of a PASS action), completion (intended receiver gains control), interception (opponent gains control first), deflection (non-controlling touch), unresolved (out of play, timeout, or match end before any of the above); plus receiver-in-view-at-decision and pass bearing vs facing. | P4 | Pass events have no producer today (#37 §1.4). |
| E9 | **P3 snapshot cost and headroom.** A dummy 13.3 KB block in a scratch build (three runs), and the peak `BytesWritten` over a full match. | P3 | Replaces the linear estimate. |

## 6. Landing plan

| Phase | Content | Results change? | Spec work | Gate |
|---|---|---|---|---|
| **P1a** | Order-preserving query fix inside `SpatialHashGrid`, radius-gated | No | None | E1, E2, E5; Unity compile |
| **P1b** | Allocation-free snapshot digest | No | None (restores `RunTick`'s stated zero-allocation contract) | E3 |
| **P1c** | Directed pair table; shared bearing; optional symmetric distance and half-angle | No | None | E4 threshold met; E2 |
| **ERR** | #7 §3.3.6 spec/code mismatch: decide which side is right | Depends on resolution | ERR + back-prop together | Owner call |
| **P2a** | Pressure conformance (3 m Euclidean), reuse of #4's evaluator, §3.6.3 fix | **Yes** | `ERR-007-006` + #7 §3.6.2/§4.4.1/§4.4.2/§3.6.3 | E7; outside W8 windows |
| **P2b** | OD-6 order with range test; sent-off exclusion | **Yes** | #7 §3.0 edit | measured separately from P2a |
| **P2c** | ID-order iteration; symmetric bearings | **Yes** | #7 §3.0 note | measured separately |
| **P3 + P4** | History, memory query, attribute plumbing, blind pass, remembered-opponent risk; **one** schema revision | **Yes** | #7 memory amendment **and** #8 blind-pass amendment, both approved before code; #16 digest-chain decision first | E6, E8, E9; goal rate and pass completion must not move in the wrong direction beyond noise |

Blast radius for P2–P4:
- the A4a corpus and fit (KD-8);
- W8 baselines;
- scenarios with hard-coded tick windows or per-90 bands;
- the FR-PO-052 baseline;
- the Decision Tree via pass urgency (P2a).

P4 acceptance asserts outcomes, not that a candidate exists (ERR-030-014 trap):
- blind passes occur;
- their completion is monotone in Vision and Concentration in a paired-attribute scenario;
- aggregate pass completion and goals per match stay within noise.

## 7. Open questions

| ID | Question |
|---|---|
| OQ-1 | Attribute roles: Concentration → retention, Anticipation → projection uncertainty, Vision → blind-pass use? |
| OQ-2 | `W_MIN`/`W_MAX`: owner values, or derived from E6? (`H` = 3.0 s is decided; it must stay ≥ `W_MAX`.) |
| OQ-3 | Should a remembered target reappearing near its projection get shortened recognition latency? |
| OQ-4 | Should pressure narrow FoV, and raise pass urgency, for players **off** the ball? |
| OQ-5 | Should team-level AI consume perception and memory instead of ground truth? |
| OQ-6 | Should the ball join the shared history (absorbing §3.5.2 staleness and §7.1.3)? |
| OQ-7 | §3.3.6: is the spec (keep a just-lost entity for one tick) or the code (drop immediately) right? |
| OQ-8 | With #7 §7.1.2 position error, what exactly does the history store to satisfy M-2? |
| OQ-9 | Teammate occlusion (#7 §7.1.1) changes masks and therefore memory: before or after P3? |
| OQ-10 | Memory reset rules for substitution, sending-off and kickoff. |
| OQ-11 | Does #8 use the multi-sample path (curved runs), or only the newest sample? |
| OQ-12 | Can Pass Mechanics #5 execute a teammate pass to a projected point? |
| OQ-13 | P2–P4 placement: inside PM-2 (D-08) or after PM-2 (new registered decision)? |

## 8. Risks

- **Ground-truth leakage** through memory (M-1) or precision (M-2). One missed check silently makes
  players omniscient.
- **Snapshot growth** consumes the P1 gain if the #16 decision is not taken first.
- **Attribution loss** if behaviour changes are bundled. P2 is split into P2a–P2c for this reason.
- **Unmeasured premise.** If E6 shows teammates are nearly always in view at decision time, P3–P4
  change little and should be re-scoped.
- **Direction of effect on goal rate is unpredicted** (currently ~12 per match on the profiled seed
  against ~2.7 in football). Blind passes may raise it; remembered-opponent risk may lower it.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.3 | October 15, 2026 | Revised after review: advisory council (integrity and evidence) on v0.2, and an external review of the council synthesis. **Corrections:** W4 keeper query excluded from the pair table; squared distances stored; candidate order preserved in P1; no range test in P1; forced refresh computes its own row; query fix located in `SpatialHashGrid` (spec-neutral); pressure also feeds DT urgency; §3.3.6 recorded as an unimplemented spec rule (ERR candidate); D-05 does not govern this work (D-08 or a new decision does); "D-16" marked unregistered and its scope corrected (chain redefinition, audit mode kept); one schema revision if P3 + P4 land together; scanning research no longer cited as setting memory range; fixed seconds cutoff replaced by a relative threshold; hashing fix requires measured zero allocation on both runtimes. **Design changes:** P3 and P4 merged, behind approved #7 and #8 amendments; P2 split into P2a–P2c; per-slot stamps; `H` `[FIXED]`; integer heartbeat window; unbiased projection with Anticipation-scaled uncertainty (replacing the `S(A)` shortfall), capped at `T_PREDICT_MAX`; mask defined by the built `FilteredView` including shoulder checks; M-2 precision invariant; measurement plan E1–E9; motivation narrowed (not C1 parity). |
| v0.2 | October 15, 2026 | Owner decision: history buffer `H` reduced from 50 to 30 frames (5.0 → 3.0 s) to cut per-tick snapshot hashing. History size ≈ 22 KB → 13.2 KB per tick (+~63% → +~37% payload); estimated P3 snapshot cost ≈ 15–17 → ≈ 10 s/match (linear extrapolation, unmeasured). Constraint `H` ≥ `W_MAX` stated. |
| v0.1 | October 15, 2026 | Created: owner decisions OD-1–OD-6 recorded; shared pass, history/memory, attribute scaling, snapshot impact, landing phases P1–P4 and open questions drafted. Not yet reviewed (`/advisor` pending). |
