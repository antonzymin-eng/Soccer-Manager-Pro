# Match Analytics & Statistics #37 — Section 3: Algorithms

**Created:** July 22, 2026
**Last Updated:** August 16, 2026, later (v0.4 — L-6, reviewed findings pass: the §3.2 `CardIssuedEvent`
row's three-way mapping is exhaustive over `CardKind`'s domain but stated no posture for a value
outside it, and the implementation's own `else` branch was silently absorbing any such value as a
plain yellow. Added a one-sentence catch-note: the mapping is exhaustive and a fourth value fails loud,
naming it, rather than falling through F5's unrecognized-ordinal ignore posture — F5 governs a Tier A
ordinal #37 does not recognize at all, not a malformed value inside a record it does. No FR text
change, no format-version change; see the log entry for the implementation-side fix)
**Version:** 0.4
**Status:** APPROVED

---

All algorithms are pure functions of the deterministic ledger tap + world-state sample (KD-5). No RNG,
no wall-clock, no observation-order dependence.

## 3.1 Possession share (tick-weighting)

Ledger records carry no tick field, so the aggregator timestamps by observing per tick (KD-3). It holds
`currentHolderTeam ∈ {0, 1, none}`, updated whenever the per-tick tap surfaces a `PossessionChangedEvent`
(team resolved from `NewHolder` via the KD-6 map; `NewHolder = −1 ⇒ none`, F3). Each observed tick adds
1 to `possessionTicks[currentHolderTeam]` (the holder **in effect that tick**; the `none` bucket for
dead-ball spans).

```
share%[team] = 100 · possessionTicks[team] / totalObservedTicks
```

so `share%[0] + share%[1] + share%[none] = 100` (dead-ball time is the remainder). A UI wanting the
conventional two-number split renormalizes over `{0,1}`; the spec reports the un-normalized triple so no
information is lost. Deterministic: `totalObservedTicks` and every transition are fixed by the match.

## 3.2 Event-count derivation + agent→team routing

The per-tick tap yields the current tick's Tier A records. `PossessionChangedEvent` is a **known
handler** (it drives the §3.1 holder state, not a tally); the other six counted records route to a
per-team tally; any record #37 does not recognize is ignored (F5). Known records:

| Record | Team resolution | Handler / tally / map |
|---|---|---|
| `PossessionChangedEvent` | `NewHolder` → KD-6 map (−1 ⇒ none) | **holder update** (§3.1), not a tally |
| `GoalAwardedEvent` | `ScoringTeam` (direct) | `Goals[team]++`; append `(team, BallPosition.xy)` to `GoalMap` |
| `FoulCommittedEvent` | `AgentTeamId(Offender)` (KD-6) | `Fouls[team]++`; append `(team, Location.xy)` to `FoulMap` |
| `CardIssuedEvent` | `AgentTeamId(Recipient)` | `CardKind` is #17's three-value domain ordinal (0=Yellow, 1=Red, 2=SecondYellow — Event System #17 Appendix A row 0x06): `Yellow ⇒ YellowCards[team]++`; `Red ⇒ RedCards[team]++`; `SecondYellow ⇒ YellowCards[team]++ AND RedCards[team]++` (ERR-037-003 — the producer emits the second caution and the resulting dismissal as ONE event, never a yellow-then-red pair, so the single event must cover both tallies for the box score to read as two yellows + one red, matching #44 Discipline's identical "one yellow AND one dismissal ban" treatment of the same value). This mapping is exhaustive over the three-value domain and states no fourth-value posture — a `CardKind` outside `{0, 1, 2}` fails loud (naming the value) rather than being silently absorbed into a tally, distinct from F5's unrecognized-*ordinal* ignore posture, which does not extend to a malformed value inside a record #37 already recognizes. |
| `OffsideCalledEvent` | `Team` (direct) | `Offsides[team]++`; append `(team, Location.xy)` to `OffsideMap` |
| `RestartAwardedEvent` | `AwardedTeam` (direct) | by `RestartKind`: `Corners`/`ThrowIns`/`GoalKicks` `[team]++` |
| `SubstitutionEvent` | `Team` (direct) | `Substitutions[team]++` |
| `MatchPhaseChangedEvent` | — | phase-boundary context only; Stage-1 aggregates whole-match, so it drives no tally (available for a future per-half split, §7.5) |

Every location `.xy` is NaN-gated before use (F2); every routed agentId is bounds-gated (F1).
`SubstitutionEvent`, like the goal/restart/offside records, carries its own `Team` byte (verified in
`src/event-system/SubstitutionEvent.cs`), so it needs no KD-6 lookup.

## 3.3 The xG location model (shape — KD-2)

A pure deterministic two-term geometric model. For a shot origin `s = (x, y)` by `attackingTeam`, with
that team's goal centre `g` (team 0 attacks `+x`: `g = (PITCH_LENGTH, PITCH_WIDTH/2)`; team 1:
`g = (0, PITCH_WIDTH/2)`):

- **Distance** `d = ‖s − g‖` (m).
- **Subtended goal angle** `θ` (deg): the angle the 7.32 m goal mouth subtends from `s` —
  `θ = angle(postLeft − s, postRight − s)`, where the posts are `g ± (0, GOAL_WIDTH/2)`. Larger `θ` =
  more open goal.
- **xG** `= logistic( XG_INTERCEPT − XG_DIST_COEF · d + XG_ANGLE_COEF · θ )`, `logistic(z)=1/(1+e^−z)`.

All three coefficients are `[GT]` (Appendix A), illustrative pending a Stage-2 balance pass; the
contract is the shape (monotone: closer + wider ⇒ higher xG), not the numbers. The function is
NaN-gated on `s` (F2) and total-order clamped to `[0,1]`.

**Worked example (penalty spot, illustrative coefficients `INTERCEPT=0.86, DIST=0.11, ANGLE=0.017`):**
central, 11 m out ⇒ `d = 11`; each post subtends `atan(3.66/11) = 18.4°` ⇒ `θ ≈ 36.9°`.
`z = 0.86 − 0.11·11 + 0.017·36.9 = 0.86 − 1.21 + 0.627 = 0.277` ⇒ `xG = logistic(0.277) ≈ 0.569`.
A 6-yard central tap (`d≈5.5, θ≈65°`) gives `≈ 0.80`; a 25 m central shot (`d≈25, θ≈16°`) gives `≈ 0.17`
— the correct closer-and-wider-is-higher shape.

**Stage-1 status (KD-2):** the model is authored + unit-locked here, but has **no valid live input** —
the ledger carries no shot origin (`GoalAwardedEvent.BallPosition` is the crossing point, not `s`). So
`AdvancedStatline.LiveXgAvailable=false, XgSum=0` at Stage 1 (F4). When the deferred `ShotAttemptedEvent`
producer (Appendix D) supplies real shot origins, `Evaluate` is consumed unchanged.

## 3.4 Territorial % and positional binning (world-state sample)

From the observational sample (fixed **stride** cadence `TERRITORIAL_SAMPLE_STRIDE`, deterministic —
this positional sample MAY be strided; it is distinct from the lossless every-tick ledger pump, §3.5):
- **Territorial %:** each sampled tick, credit the team into whose **attacking half** the ball has
  advanced — team 0 (attacks `+x`) is credited when `BallView.Position.x > PITCH_LENGTH/2`, team 1 when
  `x < PITCH_LENGTH/2` (i.e. territorial dominance = play in the opponent's half).
  `territorial%[team] = 100 · samples[team] / totalSamples`. A midfield-line sample (`x == half`) is
  assigned by the strict `>` so the split is total (no double-count, no gap).
- **Heatmap bins:** each sampled tick, for each agent `i`, increment the fixed grid cell containing
  `AgentView(i).Position` (grid `HEATMAP_COLS × HEATMAP_ROWS`, `[GT]`, over the `[0,PITCH_LENGTH]×
  [0,PITCH_WIDTH]` pitch). Bounds/NaN-gated (F1/F2). Bins are per team via the KD-6 map.

Determinism rests on the **fixed sample cadence** (not wall-clock) and the total-order cell assignment.

## 3.5 The aggregation-core loop (KD-3)

The ledger tap MUST be consumed on **every** engine tick (lossless — a skipped tick would drop that
tick's foul/goal/card/restart record). This is distinct from the positional sample, which MAY be
strided (`TERRITORIAL_SAMPLE_STRIDE`, §3.4), because positional stats are a frequency estimate, not a
count. So the ledger `for`-loop + possession accrual run every tick; only the positional accrual strides.
The every-tick contract is **enforced**: `ObserveTick` throws if `currentTick` is not consecutive with
the last observation (F6), so a mis-pumped caller fails loud instead of silently under-counting.

```
// called once per ENGINE TICK (lossless event consumption); no engine mutation
ObserveTick(tap, sample, currentTick):
    for record in tap.CurrentTickRecords:                   # KD-7 read-only tap, this tick
        if record is PossessionChanged: updateHolder(record)    # §3.1 known handler
        elif counted(record):           routeTally(record)      # §3.2 six counted types
        # else: unrecognized ordinal — ignore (F5 forward-compat)
    accruePossessionTick(currentHolderTeam)                 # §3.1 — every tick
    if currentTick % TERRITORIAL_SAMPLE_STRIDE == 0:        # §3.4 — strided positional sample
        accrueTerritorialSample(sample.ball)
        accrueHeatmap(sample.agents)

Build():   # finalize view models (pure over the accumulators)
    return MatchAnalyticsResult{ Home, Away, HomeAdvanced, AwayAdvanced, GoalMap, FoulMap, OffsideMap }
```

`Build()` is idempotent and side-effect-free; calling it does not consume the accumulators (so a live
HUD can `Build()` every render frame). Observer-neutrality (FR-AN-017): the loop only *reads* engine
observation surfaces — it never calls a mutating engine method — so the match digest is identical
whether or not analytics run (the `match-viewer` digest-lock).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial algorithms: possession tick-weighting, event routing, xG shape + worked example, territorial/heatmap binning, aggregation loop. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L; M-1 lossless every-tick + F6, M-2 possession known-handler, L-1 `SubstitutionEvent.Team`, L-2 territorial disambiguation, L-3 phase-context) → AR-2 convergence; APPROVED. See section-9 §9.3. |
| 0.3 | 2026-08-16 | — | ERR-037-003 back-prop (reviewed-findings pass, M4, found at implementation): the §3.2 `CardIssuedEvent` row's `CardKind==red ? RedCards[team]++ : YellowCards[team]++` formula is a two-way test over a three-value domain ordinal (0=Yellow, 1=Red, 2=SecondYellow — #17 Appendix A row 0x06) and had no branch for the third value at all, so `MatchAnalyticsAggregator` implemented it literally and every second-yellow dismissal counted as a plain yellow and never as a red — contradicting `MatchStatline.RedCards`'s own documented contract ("including second-yellow dismissals"). Rewritten as an explicit three-way mapping: `SecondYellow` now increments both `YellowCards` and `RedCards`, matching #44 Discipline's `ApplyCard` kind-2 treatment ("one yellow AND one dismissal ban") for the same producer contract (the engine emits the second caution and the dismissal as ONE event, never a yellow-then-red pair). No FR text change, no format-version change. |
| 0.4 | 2026-08-16, later | — | Reviewed findings pass (L-6): the v0.3 mapping is exhaustive over `CardKind`'s three-value domain but the implementation's `else` branch still silently absorbed a value outside it as a plain yellow — the same silent-default shape ERR-037-003 fixed for the SecondYellow case, recurring at the domain's boundary rather than inside it. Added a one-sentence catch-note to the §3.2 row: the mapping is exhaustive and a fourth value fails loud, naming it, rather than falling through F5's unrecognized-*ordinal* ignore posture (F5 governs a Tier A ordinal #37 does not recognize at all, not a malformed value inside a record it already does). `MatchAnalyticsAggregator.cs` widened to an explicit `CardKindYellow` branch (Yellow is no longer the implicit catch-all) plus an `ArgumentOutOfRangeException` for anything else, mirroring #44 `CardLedgerFold.RequireKnownCardKind` (F4) over the identical domain. No FR text change, no format-version change. |
#endregion
