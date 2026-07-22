# Match Analytics & Statistics #37 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+3L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements (FR-AN-001..021)

### Derivation posture
- **FR-AN-001** — #37 MUST derive all statistics **read-only**: it MUST NOT mutate engine state, and
  MUST NOT add any engine event or producer.
- **FR-AN-002** — #37 MUST consume exactly two deterministic taps: the read-only per-tick ledger tap
  (KD-7) and the observational world-state sample (`BallView`/`AgentView`). It MUST NOT read any other
  engine state.
- **FR-AN-020** — #37 MUST hold **no persistent state** and MUST bump **no format version**; every stat
  is recomputed from the (already-deterministic) match. Persisting a report is out of scope (KD-4).
- **FR-AN-021** — #37 MUST consume **live during the match** (there is no post-match ledger reader);
  it MUST NOT assume the serialized ledger bytes can be re-parsed.

### Basic stats (derivable today)
- **FR-AN-003** — Possession share per team MUST be tick-weighted from `PossessionChangedEvent` holder
  spans × the agent→team map (KD-6), timestamped via `MatchEngine.CurrentTick` (§3.1). Ticks with no
  settled holder (holder = −1) MUST be attributed to neither team (F3).
- **FR-AN-004** — Goals per team + a goal-location point map MUST be derived from `GoalAwardedEvent`
  (`ScoringTeam`, `BallPosition` = crossing point; `Scorer`/`Assister` recorded).
- **FR-AN-005** — Fouls per team + a foul-location point map MUST be derived from `FoulCommittedEvent`
  (team via the `Offender` agentId → KD-6 map; `Location`).
- **FR-AN-006** — Cards per team (by `CardKind`) MUST be derived from `CardIssuedEvent` (team via
  `Recipient` → KD-6 map).
- **FR-AN-007** — Offsides per team + location MUST be derived from `OffsideCalledEvent` (`Team`,
  `Location`).
- **FR-AN-008** — Corners / throw-ins / goal-kicks per team MUST be derived from `RestartAwardedEvent`
  (`RestartKind`, `AwardedTeam`).
- **FR-AN-009** — Substitutions per team MUST be tallied from `SubstitutionEvent`.

### Advanced stats
- **FR-AN-010** — Territorial % per team MUST be derived from the observational ball-position sample
  (share of sampled ticks the ball lies in each team's attacking third/half — §3.4), **not** from the
  ledger. It MUST be deterministic (fixed sample cadence, no wall-clock).
- **FR-AN-011** — Positional heatmaps / average-position maps MUST be derived from the observational
  `AgentView` sample, binned deterministically (§3.4).
- **FR-AN-013** — The xG location model MUST be a **pure deterministic function** of shot geometry
  (§3.3). At Stage 1 it MUST NOT be fed the goal crossing-point as a shot origin; with no shot producer
  it produces **no live xG** (F4). It activates over real shots only when the deferred producer lands.
- **FR-AN-014** — The xG model's coefficients MUST be `[GT]` in `MatchAnalyticsConstants`, illustrative
  pending a Stage-2 balance pass; the spec's contract is the model **shape**, not the numbers.

### Cross-cutting
- **FR-AN-012** — Team resolution for agentId-only records MUST use `AgentTeamId(i)` snapshotted at
  boot (KD-6); team-carrying records MUST use their own team byte.
- **FR-AN-015** — The #38 view models (`MatchStatline`, `AdvancedStatline`) MUST be immutable value
  types exposing no engine reference. No sim assembly may reference `TacticalDirector.MatchAnalytics`
  (KD-4).
- **FR-AN-016** — Derivation MUST be deterministic: same match ⇒ byte-identical view models across two
  runs. #37 MUST register no RNG stream, domain tag, or `SubsystemOrdinal` (KD-5).
- **FR-AN-017** — Computing analytics MUST NOT perturb the match digest (observer-neutrality; the
  `MatchViewerTests` digest-lock precedent).
- **FR-AN-018** — #37 MUST fail loud (throw) on a malformed observational read: an agent index outside
  `[0, SQUAD_SIZE)` (F1) or a non-finite position (F2), per the `match-viewer` guard precedent.
- **FR-AN-019** — The spec MUST name every deferred producer-gated stat and the exact new Tier A
  producer it waits on (Appendix D). #37 MUST NOT build a consumer for any of them at Stage 1.

## 2.2 Data structures

```
// Immutable per-team basic statline (KD-4 view model).
readonly struct MatchStatline {
    byte  TeamId;
    int   Goals;
    float PossessionSharePercent;   // [0,100]; both teams + no-possession sum to 100
    int   Fouls, YellowCards, RedCards, Offsides;
    int   Corners, ThrowIns, GoalKicks, Substitutions;
}

// Immutable advanced statline (positional + model layer).
readonly struct AdvancedStatline {
    byte  TeamId;
    float TerritorialPercent;       // [0,100]; deterministic ball-position share (§3.4)
    // xG is producer-gated (KD-2): LiveXgAvailable=false at Stage 1, XgSum=0.
    bool  LiveXgAvailable;
    float XgSum;
    // Positional bins (heatmap) — fixed grid, deterministic (§3.4).
    // (grid dimensions in MatchAnalyticsConstants)
}

// Location point maps (goals/fouls/offsides) — read-only arrays of (team, x, y).
readonly struct StatPoint { byte TeamId; float X, Y; }

// The full per-match result the aggregator emits.
readonly struct MatchAnalyticsResult {
    MatchStatline    Home, Away;
    AdvancedStatline HomeAdvanced, AwayAdvanced;
    StatPoint[]      GoalMap, FoulMap, OffsideMap;   // read-only snapshots
}

// The xG model (KD-2 shape) — pure function, no state.
static class XgLocationModel {
    // xG(shotOrigin) = logistic( a - b*distanceToGoalCentre - c*(angleTermDeg) ), §3.3.
    static float Evaluate(Vector2 shotOrigin, byte attackingTeam);
}

// The aggregation core (KD-3): fed the per-tick tap + world sample.
class MatchAnalyticsAggregator {
    void ObserveTick(<per-tick ledger tap>, <world-state sample>, ulong currentTick);
    MatchAnalyticsResult Build();  // finalize the view models
}
```

## 2.3 Failure modes

| # | Condition | Handling | FR |
|---|---|---|---|
| **F1** | Observational read returns an agent index outside `[0, SQUAD_SIZE)` | **Throw** (fail loud) | FR-AN-018 |
| **F2** | A sampled ball/agent position or a location-carrying record is non-finite (NaN/Inf) | **Throw** (NaN-gate, `match-viewer` precedent) | FR-AN-018 |
| **F3** | Possession holder agentId = −1 (no settled possession this span) | Attribute to **neither** team (not an error); excluded from possession weighting | FR-AN-003 |
| **F4** | xG requested at Stage 1 with no shot corpus | Return `LiveXgAvailable=false, XgSum=0` (producer-gated, documented — **not** an error) | FR-AN-013 |
| **F5** | The per-tick tap surfaces a Tier A ordinal #37 does not aggregate (e.g. a future added producer) | **Ignore** that record (forward-compatible; #37 aggregates only the ordinals it knows), so adding a producer never crashes old analytics | FR-AN-019 |
| **F6** | `ObserveTick` is called with a `currentTick` that is not consecutive with the last (a **dropped tick** — the caller failed the every-tick pump, §3.5) | **Throw** (the lossless-consumption contract is enforced, not merely documented — a silent gap would drop that tick's records and corrupt counts/possession) | FR-AN-003/021 |

F5 is deliberate: it is what lets the KD-1 deferred producers be added later without a lockstep #37
change — old #37 ignores the new record until its aggregation is extended. F6 makes the §3.5 every-tick
requirement enforceable: `ObserveTick` asserts `currentTick == lastObservedTick + 1` (or it is the first
observation) and throws on a gap, so a mis-pumped caller fails loud instead of silently under-counting.
The stride in §3.4 is *internal* to `ObserveTick` (still called every tick), so it does not trip F6.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial FR set (FR-AN-001..021), data structures, failure modes F1–F5. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L; M-1 lossless every-tick + F6, M-2 possession known-handler, L-1 `SubstitutionEvent.Team`, L-2 territorial disambiguation, L-3 phase-context) → AR-2 convergence; APPROVED. See section-9 §9.3. |
#endregion
