# League Bootstrap + Round-Resolution Calibration — Design Supplement

> **Created:** July 25, 2026
> **Status:** DESIGN SUPPLEMENT — **A3 LANDED July 25, 2026** (design converged AR-1..AR-3;
> implemented, then code-reviewed AR-4 → AR-5 (whole-file, 1H+4M+3L) → AR-6 (1M) — full gate green
> at each step, then AR-7 over the spec/tests/governance — 1H+4M). **A4a is designed here but NOT executed** — its
> ~9 h corpus run is its own roadmap item. Same governance class as `lineup-selection-design.md`,
> `match-save-file-design.md`, `unified-season-save-design.md`. Opens **no numbered spec** and
> changes no `SPEC_INDEX.md` row.
> **Governs:** `docs/tracking/path-to-playable-roadmap.md` items **A3** (league bootstrap) and
> **A4a** (round-resolution calibration corpus) — the two items §6 item 1 of that roadmap says
> "need governance that does not exist" and assigns to *one short design note*.
> **Purpose:** Define (a) how a playable league of clubs, squads and a startable season comes into
> existence from a single world seed, and (b) the shape of the round-resolution model A4 will build
> plus the methodology that calibrates it. This note is explicitly **not #47** — it authors no
> database editor and defines no new data format; it consumes #27's `Squad`/`PlayerRecord` verbatim.

---

## 0. Scope

**In scope (A3, buildable now):**

- A `LeagueBootstrap` that turns one `ulong worldSeed` into `N` clubs, each with a 25-player
  `Squad`, deterministically and with no authored data files.
- Club identity: ids, names, and a per-club **strength** so the league is not 20 statistically
  identical teams (which would make the table meaningless).
- The `ISquadProvider` the match engine and #30 T2 both consume.
- A `CreateSeason` helper that hands #30 a startable `SeasonState`.

**In scope (A4a, defined now, executed as its own item):**

- The **shape** of the round-resolution model (#30 §3.4.1 / FR-SN-013a) — what it computes, what
  parameters it has, what determinism contract it must satisfy.
- The **calibration methodology**: corpus size and composition, how it is generated, what artifact
  it produces, how the fit is locked by test, and what invalidates it.

**Explicitly out of scope:**

- **#47 (New-Game Setup & DB Editor).** Roadmap C3: playability needs *a league to exist*, not an
  editor. This note authors no editing surface and no on-disk roster format; the Stage-0 authoring
  path for hand-made squads already exists (`SquadFileLoader`).
- **Building the round-resolution model itself.** That is A4 (#30 T2's `AdvanceAndPlayNextRound`),
  and it cannot be *calibrated* before A4a runs. This note pins its shape so A4a knows what it is
  fitting; the code lands at A4.
- **Running the A4a corpus.** ~9 h of compute (roadmap C1a). Budgeted as its own roadmap item.
- **Promotion/relegation, multiple divisions, cups.** #43, deferred past PM-3.
- **Club finances, reputation, stadium.** #40/#45, deferred.

---

## 1. What exists

| Piece | Where | State |
|---|---|---|
| Deterministic 25-player squad generation | `RosterGenerator.Generate(rng, streamIndex, clubId, count)` | ✅ exact per-player RNG budget; caller registers the stream |
| Canonical player record | `PlayerRecord` / `PlayerAttributes` (31 × `[1,20]` + `WeakFootRating` `[1,5]`) | ✅ |
| Club-scoped roster container | `Squad` (`ClubId` + ≤ `CLUB_SQUAD_SIZE`=25 players) | ✅ |
| Roster → match-engine seam | `ISquadProvider.ResolveByClubId(int) → Squad` | ✅ (snapshot-deserialize Phase 2) |
| Lineup selection | `LineupSelector.Select(squad, family)` | ✅ per-line greedy, **fails loud** on an unfillable starter line (KD-L3) |
| Season value types + scheduler + table | `src/season-save/` (#30 T0) | ✅ |
| Season save/restore | `SeasonStateCodec` / `SeasonSaveManager` (#30 T1) | ✅ |

Nothing today produces **more than one club**. Every existing test builds squads by hand.

---

## 2. Key decisions

### KD-1 — The code lives in `src/season-save/` (`TacticalDirector.SeasonSave`); no new assembly.

`LeagueBootstrap` needs three things at once: `RosterGenerator` + `Squad` (player-database),
`ISquadProvider` (match-engine), and `SeasonState` (season-save). `season-save` already references
match-engine and is the assembly #30 T0/T1 landed in; it gains a `TacticalDirector.PlayerDatabase`
reference (player-database references only `DeterministicSim`, so this introduces **no cycle** — it
is the same downward direction `match-engine → player-database` already uses, and the season-save
*test* assembly already declares it).

Rejected: a new `league-bootstrap` assembly. It would sit at exactly the same layer position as
`season-save` with the same reference set, and #30 T0 already made this call ("no new assembly; the
root already sits above both").

### KD-2 — Club count and identity: contiguous ids `0..N-1`, default `N = 20`.

Club ids are the integers `0 .. N-1`, ascending and contiguous. This is load-bearing, not
cosmetic: `RosterGenerator` computes `PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex`, so
contiguous non-negative club ids give **globally unique, non-overlapping** player ids across the
whole league for free. A sparse or negative id set would still be unique but wastes the invariant
and complicates the #28 regen id allocator later.

- `DefaultClubCount = 20` `[GT]` — one 38-round double round-robin, 380 fixtures (roadmap C1's
  sizing case).
- Bounds `[2, MaxClubCount]`, `MaxClubCount = 32` `[GT]`, fail-loud outside. The floor is
  `SeasonState`'s own (≥ 2 clubs); the ceiling is a sanity bound — at 32 clubs a season is already
  62 rounds / 992 fixtures, far past anything playable, and it keeps `PlayerId` well inside `int`.
  It is also **half** `DeterministicSimConstants.MaxRngStreams` (64), because the bootstrap
  registers one roster stream per club (KD-4); that coupling is enforced by a fail-loud gate in
  `Generate` and locked by test, so raising one without the other cannot fail halfway through
  generation with a generic "registry full".
- `0` is a valid club id (`MatchEngineConstants.NO_ROSTER_CLUB_ID` is `-1`, so there is no
  collision with the "no roster" sentinel).

### KD-3 — Club names are assigned **by id from a fixed catalogue**, not drawn.

A new `ClubNameCatalogue` holds an APPEND-only in-code table of ≥ `MaxClubCount` club names (the
`NameCatalogue` / `InteractionTextCorpus` Stage-0 authoring pattern — a later data swap, not a
format). Club `k` always gets entry `k`.

Rationale: a new-game seed should change **who is good**, not **who exists**. Stable
name↔id binding makes a bug report ("Ashford Town's table row is wrong") reproducible across seeds
and keeps the name out of the determinism surface entirely (names are drawn from no stream and
enter no digest). Recorded alternative for a later deepening: seed-permuted names, if careers ever
need to feel like different *worlds* rather than different *seasons*.

`ClubNameCatalogue` must expose enough entries for `MaxClubCount`; a test locks
`Names.Count >= MaxClubCount` and uniqueness, so raising the cap cannot silently produce two
clubs with the same name.

### KD-4 — One world seed, three domain-separated derivations, and **no new RNG stream**.

The bootstrap's single input is `ulong worldSeed`. From it:

```
rosterSeed   = SplitMix64Mix(worldSeed ^ ROSTER_SEED_DOMAIN)
strengthSeed = SplitMix64Mix(worldSeed ^ STRENGTH_SEED_DOMAIN)
seasonSeed   = SplitMix64Mix(worldSeed ^ SEASON_SEED_DOMAIN)
```

with three distinct `[FIXED]` 64-bit domain constants, so the three uses are decorrelated and a
change to one (e.g. a different strength model) cannot shift the others.

- **Rosters** consume `rosterSeed` through a `DeterministicRngService`, one registered stream per
  club under `SubsystemOrdinals.PlayerDatabase` with `entityId = clubId` — exactly the registration
  `RosterGenerator`'s own doc recommends. Nothing new is allocated in #16.
- **Strength ranks** consume `strengthSeed` through a **local SplitMix64 Fisher–Yates**, the shape
  `FixtureScheduler.Generate` already uses for its club-label permutation (`unchecked` +
  `// Spec #16 §3.4.4` per FR-CS-044, rejection-sampled bounded draw). No stream, no ordinal.
- **Fixtures** consume `seasonSeed` through the existing `FixtureScheduler`.

**Why no new stream matters:** ERR-030-001 pins `DOMAIN_TAG_SEASON_LOOP = 0x22` /
`SubsystemOrdinals.SeasonLoop = 84` to **#30 T2's first draw site** (A4), precisely so a stream is
never registered before it has a draw. A3 must not pre-empt that allocation, and with this decision
it does not need to.

### KD-5 — Strength is a per-club **integer attribute delta**, ramped over a seeded rank.

Each club gets `StrengthDelta ∈ [-S, +S]` (`S = LeagueStrengthSpread`, `[GT]`, default **3**),
applied by shifting every `[1,20]` attribute of every player on that club's roster:
`attr' = Clamp(attr + delta, 1, 20)`.

Assignment: permute `0 .. N-1` by Fisher–Yates over `strengthSeed` to get each club's **rank**,
then ramp linearly across the league:

```
delta(rank) = RoundHalfAwayFromZero( -S + 2S * rank / (N - 1) )         # N >= 2
```

so rank 0 is the weakest (`-S`), rank `N-1` the strongest (`+S`), and the middle of the table
clusters near 0. For `N = 20, S = 3` the deltas run −3 … +3 in unit steps with the mass in the
middle, which is roughly what a real league's spread looks like.

Notes and deliberate limits:

- **`WeakFootRating` is NOT shifted.** It is a `[1,5]` scale (the #27 KD isolation); ±3 would
  saturate it to a boundary for most clubs. Documented, not an oversight.
- **Uniform across attributes.** A strong club is better at everything. This is one dial, which is
  exactly what makes A4a's calibration tractable (KD-8 fits against *rating difference*). A
  per-attribute strength profile (a "good defensively, poor in attack" club) is a recorded
  deepening, not Stage-0.
- **The spread's *sufficiency* is unverified, and A4a must check it first.** A3 proves the delta
  reaches the rosters (mean attribute differs), which is a near-tautology; whether ±3 points on a
  mean of 10 produces *distinguishable match results* is unknown and unknowable without engine
  runs. If it does not, the strength model is decorative and the league table is noise — the exact
  failure the model exists to prevent. See KD-8's first step.
- **The final attributes are a function of league size.** `StrengthDelta` depends on `clubCount`
  twice (the rank permutation's length and the ramp denominator), so growing or shrinking the league
  rewrites every club's attributes. Only the *base* roster (identity + pre-strength attributes) is
  size-invariant. This matters for **#43**, whose promotion/relegation transform changes the club
  set: it cannot assume a club's players survive a division change unchanged. Both halves are
  test-locked (`Generate_ClubBaseRoster_IsIndependentOfLeagueSize` and
  `Generate_ClubAttributes_DoChangeWithLeagueSize`).
- **Clamping can bite at the top.** `RosterGenerator`'s untuned output spans `[6, 18]`, so `+3`
  clamps only attributes already at 18. Fine, and it means the delta is *slightly* sublinear at the
  top — recorded so a calibration residual there is not later mistaken for a model defect.

### KD-6 — Squads use an explicit **position template**; the generator gains an additive overload.

This is a correctness requirement, not a nicety. `LineupSelector` **fails loud** when a starter
slot's required position has no eligible player, and `RosterGenerator` draws positions uniformly
over the four `PlayerPosition` values. For a 25-player squad the defender count is
`Binomial(25, 0.25)` (mean 6.25), so `P(fewer than the 4 defenders a back four needs) ≈ 3%` **per
club per line** — across a 20-club league, a bootstrapped season would fail to start most of the
time. A league generator that produces unusable squads a few percent of the time is worse than one
that fails always, because it fails *by seed*.

The bootstrap therefore supplies a fixed position template:

| Position | Count |
|---|---|
| Goalkeeper | 3 |
| Defender | 8 |
| Midfielder | 8 |
| Forward | 6 |
| **Total** | **25** |

Sized against the maximum any shipped formation family requires for a starting XI (F442 1/4/4/2,
F433 1/4/3/3, F4231 1/4/5/1 → max GK 1, DF 4, MF 5, FW 3), leaving margin for the seven
position-agnostic bench slots and for in-season absences.

**Mechanism.** `RosterGenerator` gains an **additive overload**
`Generate(rng, streamIndex, clubId, PlayerPosition[] positions)` that forces each player's
`Position` (and therefore its attribute bias) to the supplied value while consuming the **identical**
`FIELDS_PER_PLAYER` reservation per player — the position draw still happens and is discarded, so
the stream layout, the budget lock, and the existing `Generate(..., count)` path stay
**byte-identical**. `RosterGenerator`'s own source already names this as the intended refinement
("Uniform over the 4 positions is a documented Stage-0 simplification — a real squad's position
distribution … is a future refinement, not designed here"); this note is where it gets designed, and
the change is back-propped as **ERR-027-002** — #27 is APPROVED and its *section files* are
authoritative (its tracking supplement was superseded at promotion), so the back-prop patches
`docs/specs/squad-player-data/`: `section-2.md` gains **FR-SQ-012a** for the overload plus
`POSITION_COUNT` in the §2.2.5 catalogue, `section-3.md`'s draw table annotates draw 3 and retires
the now-stale "future work" framing, and `appendices.md` gains the constant row.

Rejected: post-processing the generated squad in the bootstrap by subtracting the drawn position's
bias and adding the template position's. It is exact *today* only because the default `[GT]`
constants make clamping unreachable during generation (`10 ± 4 + bias ≤ 4` never leaves `[1,20]`) —
a silent coupling to tuning values that a config change would break with no test to catch it.

### KD-7 — Round-resolution model shape (defined here, built at A4).

#30 FR-SN-013a requires a deterministic model whose draws are keyed, **not cursor-positioned**, so
resolving a round's fixtures in any order yields the same table (a §5 lock, T-SN-CAL-003c). The
shape A4 builds and A4a calibrates:

```
ResolveRound(fixture, seasonSeed, seasonNumber, ratings) -> MatchResult
    key      = SplitMix64Mix( Mix(seasonSeed, seasonNumber, RoundIndex, HomeClubId, AwayClubId) )
    dSquad   = Rating(home) - Rating(away)              # measured; engine-observable (see below)
    edge     = dSquad + HomeAdvantageRating             # model-side only; a FITTED parameter
    lambdaH  = clamp( BaseGoals * exp(+GoalRatingSlope * edge), LambdaMin, LambdaMax )
    lambdaA  = clamp( BaseGoals * exp(-GoalRatingSlope * edge), LambdaMin, LambdaMax )
    goalsH   = PoissonInverseCdf(lambdaH, key, subStream 0)
    goalsA   = PoissonInverseCdf(lambdaA, key, subStream 1)
```

**`dSquad` vs `edge` — keep these separate.** `dSquad` is a property of the two squads and is
observable from an engine match. `edge` adds `HomeAdvantageRating`, which is a **parameter of this
model**, not a property of anything the engine knows about. Only `dSquad` may appear on the
calibration corpus's axis (KD-8); conflating the two makes the corpus unbuildable, because the
harness would have to record a value that does not exist until after the fit.

- **`Rating(club)`** = the mean `[1,20]` attribute over the club's selected starting XI
  (`LineupSelector` output — already deterministic and already the thing the engine actually
  fields). Reusing the selector rather than the whole 25-man squad matters: a deep bench should not
  make a club stronger this Saturday.
  > **A4 prerequisite (found during A3's code review, recorded so A4 does not discover it):**
  > `LineupSelector` is `internal` to `match-engine` and visible only to `MatchEngine.Tests`, so
  > `SeasonLoop` — which lives in `season-save` — **cannot call it today**. A4 must first expose the
  > rating: either promote `LineupSelector` to `public` (it is a pure, stateless, RNG-free function,
  > so this costs nothing but surface) or add a small public rating accessor beside
  > `ConfigureSquads`. Do **not** re-implement selection in `season-save` — two selectors would
  > disagree the moment either changes, and the quick-sim's rating would stop describing the team
  > the engine actually fields.
- **Parameters to fit (A4a):** `BaseGoals`, `GoalRatingSlope`, `HomeAdvantageRating`. `LambdaMin` /
  `LambdaMax` are safety clamps, not fitted.
- **`PoissonInverseCdf`, named — not "a Poisson draw".** Inversion: accumulate
  `p = exp(-lambda)`, `cdf = p`, and while `u > cdf` step `k++`, `p *= lambda/k`, `cdf += p`,
  bounded by a `[FIXED] MaxGoalsPerSide` cap. Two engineers handed "PoissonDraw" would otherwise
  reasonably pick Knuth's product-of-uniforms or a normal approximation — which consume different
  numbers of draws and produce different scorelines **from the identical key**. That scoreline
  reaches `LeagueTable`, which `SeasonStateCodec` serializes, so the divergence would be a
  save-format divergence, not a transient one. Inversion also needs exactly **one** uniform per
  side, which suits the `Reserve`/`DrawReserved` fixed-budget discipline.
- **Float posture, stated rather than assumed.** `exp` is a libm call and the lambdas are floats.
  That is acceptable here under the project's Stage-0 doctrine — single-machine determinism on the
  pinned certification host, with cross-platform bit-exactness deferred to Stage 5 alongside
  Fixed64 — and it is the same posture the match engine already ships. It is called out because
  this model's output is **persisted** (the league table), unlike most float work, and because the
  adjacent management specs (#41/#40/#33, and `StrengthDelta` in this very note) deliberately use
  integer/per-mille arithmetic. If a later decision wants persisted season state off floats
  entirely, this is the line item to revisit.
- **Determinism:** every draw derives from `key`, which is a pure function of
  `(seasonSeed, seasonNumber, roundIndex, homeClubId, awayClubId)`. No cursor, no shared stream
  state ⇒ order-independent by construction, satisfying T-SN-CAL-003c without a separate mechanism.
- **Budget:** a handful of arithmetic ops and two Poisson draws — well inside roadmap C1's
  ≲ 10 ms/match target (it is microseconds), which is the whole reason the model exists.

A4 also keeps #30 §3.4.1's **minimal identity** available (resolve every fixture through the real
`MatchEngine`): correct, deterministic, and unusably slow (roadmap C1: ≥ 16.3 h/season), so it is a
test/verification mode and a `SeasonState`/config dial, never the default.

### KD-8 — Calibration methodology (A4a).

The risk being managed is roadmap risk row 1: *"round-resolution model diverges from engine results;
league tables feel wrong"* — a failure no unit test catches, because every unit test would pass
against a model that is internally consistent and empirically wrong.

**Step 0 — check the signal before fitting anything.** Before spending ~9 h on a 200-match grid, run a
small pilot (~20 matches) at the two extremes of achievable `dSquad` (a `−S` squad versus a `+S` squad,
both venues). If those two populations' goal distributions are not distinguishable, **the corpus carries no
signal to fit** and `LeagueStrengthSpread` must be raised before the full run — otherwise A4a burns
nine hours fitting three parameters to noise and the league table stays meaningless. This is KD-5's
unverified-sufficiency risk, discharged at the first point it can be measured.

**Corpus.** ~200 engine-simulated matches over a grid of **`dSquad`** — the measured
`Rating(home) − Rating(away)`, never `edge`. Concretely: 11 buckets in unit steps, ~18 matches per
bucket, each a real `MatchEngine` run at a distinct match seed. Both orderings (strong-at-home and
strong-away) appear in every bucket, which is what lets `HomeAdvantageRating` be **fitted from the
home/away asymmetry within each bucket** rather than assumed — and is the reason it must not be on
the axis.

Two things follow, and both were wrong in the first draft of this section:

- **Build the pairs by direct shift, not by picking league clubs.** The harness calls
  `LeagueBootstrap.ApplyStrength(baseSquad, delta)` with an **arbitrary** `int` delta against a fixed
  base-roster pair. It is not limited to `LeagueStrengthSpread`, so the grid's range is achievable by
  construction. Sourcing pairs from a generated league instead would cap the reachable spread at
  `2 · S = 6` *and* — because a 20-club ramp puts only two clubs at each extreme — leave roughly a
  dozen ordered pairs to fill the outermost buckets, so 18 "samples" there would be the same few
  pairs re-seeded, measuring seed variance rather than rating response.
- **Bucket on the measured value, not the knob.** Two squads shifted by the same delta do not have
  the same `Rating`, because their base rosters differ. The harness records the actual
  `Rating(home) − Rating(away)` per match and buckets on that; the delta is only how the spread is
  produced.

**Generation.** A committed harness (lands with A4a) that boots the engine through
`ConfigureSquads` and ticks to `MatchEnded`, recording `(dSquad, homeGoals, awayGoals, matchSeed)`
— every column observable at capture time, which `dRating`/`edge` would not be.
It is parallelisable across buckets and **run once**; roadmap C1a budgets ~9 h. It must be
*scheduled*, not discovered.

> **Harness placement (corrected during A3's code review).** It belongs in
> `src/season-save/tests/`, **not** beside `MatchEngineCapstonePerfHarness` in
> `src/match-engine/tests/` as first drafted. Building a controlled rating differential means
> generating two squads and shifting them by a chosen delta — i.e. `LeagueBootstrap`'s own
> `ApplyStrength`, which is `internal` to `season-save`. The season-save test assembly already
> references `match-engine` and `player-database`, so it can boot a real engine *and* reach the
> bootstrap internals; the match-engine test assembly can do neither for `season-save`.

**Artifact.** `docs/tracking/round-resolution-corpus.md` — the per-bucket sample count, mean and
variance of home/away goals, plus the raw rows. It **records the engine commit SHA and
`SNAPSHOT_SCHEMA_VERSION` at capture time**, because the corpus measures what the engine does
*today*: goal detection landed July 11 2026 with a deliberately minimal restart model, so a later
engine change invalidates the fit rather than merely aging it. That is a re-capture trigger, and
naming it now is cheaper than rediscovering it.

**Fit + lock.** Least-squares over the three KD-7 parameters against the per-bucket means. The lock
is **not** a statistical sample at test time (the #27 AR-2 precedent: "direct constant assertions,
not statistical sampling") — it is a pinned table: for each bucket, the quick-sim's mean goals over
a **fixed** seed sweep must equal a recorded expected value within a recorded tolerance. Fixed
seeds ⇒ the assertion is exact and reproducible; the tolerance encodes the agreement requirement,
not test flake.

**Acceptance.** Per-bucket mean home and away goals within ±0.25 of the corpus mean, and the
win/draw/loss split within ±5 percentage points at `dSquad = 0` (evenly matched squads — note this is
where home advantage should show up as an asymmetry, so it is the bucket that actually tests the
fitted `HomeAdvantageRating`). Both are recorded in the artifact
so a later re-fit is measured against the same bar.

> **⚠️ BOTH BARS WERE MISSED WHEN A4a ACTUALLY RAN (August 12, 2026), and neither miss is a fit
> failure. Read `round-resolution-corpus.md` before re-using either number.**
>
> - **The ±0.25 bar is not measurable at the depth this same section specifies (`ERR-030-033`).** At
>   ~18 matches per bucket a bucket mean carries a standard error of 0.135–0.633, and **15 of 22
>   bucket-sides have a standard error larger than the entire bar**. The tolerance and the sample
>   size were chosen independently and never checked against each other; as written, a perfectly
>   correct model scored against a re-run of the same corpus would also fail. Resolving ±0.25 needs
>   n ≈ 770/bucket — ~210 h of engine time against a budgeted ~9 h — so this is a bar to re-specify,
>   not a run to re-size. The ±5 pp W/D/L bar has the same defect in milder form: at n = 18 the
>   corpus draw share carries a ~7 pp standard error, so A4a deepened that one bucket specifically in
>   order to evaluate it.
> - **The model shape cannot express what the corpus shows (`ERR-030-034`).** KD-7 draws Poisson, whose
>   variance equals its mean by construction; the engine is over-dispersed at **z = +5.40** (mean
>   `var/mean` 1.395, 19 of 22 bucket-sides above 1), which shows up as far fewer draws than the model
>   can produce. No value of the three fitted parameters closes a second-moment gap.
>
> Both are recorded, deliberately **not** fixed by A4a: widening a bar to fit its own result stops it
> being a bar, and changing the distribution family is a KD-7 decision that moves persisted season
> state. The corpus is committed, so a re-fit against a new family costs seconds rather than hours.

### KD-9 — What A3 hands #30: a `League` that is itself the `ISquadProvider`.

```
League = LeagueBootstrap.Generate(worldSeed, clubCount)
    League.Clubs          -> Club { ClubId, Name, StrengthDelta }
    League.ResolveByClubId(clubId) -> Squad        // ISquadProvider, for the engine and A4
    League.CreateSeason(managedClubId, objective?) -> SeasonState
```

`CreateSeason` calls the existing `SeasonState.CreateNew(clubIds, managedClubId, seasonSeed,
objective, firstRoundDay, daysBetweenRounds)` — the bootstrap owns the *seed derivation and the
club set*, not a second season constructor. The board objective defaults to "finish in the top
half" (`⌈N/2⌉`), overridable by the caller; `firstRoundDay` / `daysBetweenRounds` are `[GT]`
(default 7 / 7 — one fixture a week from world-day 7).

`League` is immutable after construction and hands out value copies / the existing snapshot-copying
`Squad`, so the recurring live-array aliasing defect class (living-world slice-2 AR-1 M-1,
match-viewer AR-3 M-1, #26 T0 AR-1, #30 T0 H-1) cannot recur here.

**Resuming a saved career — who owns the world seed.** Squads are *not* persisted: a save carries club
IDs, and `League` re-derives every roster when it is handed to `SeasonSaveManager.Load` as the
`ISquadProvider`. So a save is only reopenable if the **world seed** survives it. It does — inside the
world blob (`WorldStore` serializes it) — but it was write-only until A3's review, so the resume path
is now pinned explicitly:

```
contents = SeasonSaveManager.Load(path)                              # world + season, no provider yet
league   = LeagueBootstrap.Generate(contents.World.WorldSeed,        # WorldSeed accessor added for this
                                    contents.Season.ClubCount)
contents = SeasonSaveManager.Load(path, squads: league)              # now with the provider
```

`clubCount` comes from the season blob, not from config — a career started at 20 clubs must resume at
20 even if `DefaultClubCount` later changes. The **caller must construct its `WorldStore` with the same
seed it passed to `Generate`**; nothing enforces that coupling, and the new-game flow (roadmap C4) owns
it. Locked by `SavedWorldSeed_RebuildsTheSameLeague`.

### KD-10 — The generation path is persistence-equivalent, and is pinned by a golden vector.

Direct consequence of the resume path above: because rosters are regenerated rather than saved, **any
change that moves `Generate`'s output invalidates every existing save** — a draw-order change in
`RosterGenerator`, a reorder of `NameCatalogue` or `ClubNameCatalogue`, an edit to
`SquadPositionCounts`, or a one-line tweak to a `[GT]` generation constant such as
`PlayerDatabaseConstants.AttributeBaseMean`.

Every same-seed determinism test on this path is **self-referential** ("generate twice, compare") and
stays green through all of those. The guard is therefore a **pinned golden vector**
(`LeagueBootstrapGoldenVectorTests`): absolute expected values for a fixed
`Generate(0x5EED1EA6D0DEC0DE, 4)` — the derived season seed, the four strength deltas, spot identity and
attribute values, and an FNV-1a-64 digest over every field of every club and player. This is the
mechanism #16 already uses for HKDF / SipHash / canonical serialization.

Verified non-vacuous, not assumed: perturbing `AttributeBaseMean` 10 → 11 fails the digest and
spot-value locks while the rest of the suite passes. **If it fails, do not re-pin to go green** — either
the change was unintended, or it is deliberate and must be re-pinned in the same commit and treated as
save-breaking.

---

## 3. Determinism contract

Given the same `worldSeed` and `clubCount`, `LeagueBootstrap.Generate` produces byte-identical
output: same club ids, names, strength deltas, and every player's identity and all 31 attributes.
Locked by test (two independent `Generate` calls compared field-by-field over every player of every
club). Two different seeds produce different rosters *and* a different strength ordering.

The bootstrap consumes no `MatchEngine`, mutates no global state, and registers nothing in #16's
domain-tag/ordinal registry.

---

## 4. Failure modes (fail-loud)

| # | Condition | Behaviour |
|---|---|---|
| F1 | `clubCount` outside `[2, MaxClubCount]` | `ArgumentOutOfRangeException` |
| F2 | `clubCount` exceeds the club-name catalogue | `InvalidOperationException` (a catalogue/cap coherence bug, not caller input) |
| F3 | Position template does not sum to `CLUB_SQUAD_SIZE`; or `clubCount` exceeds `MaxRngStreams`; or a negative world-day `[GT]` | `InvalidOperationException` (catalogue/config coherence, not caller input; test-locked) |
| F4 | `ResolveByClubId` given an unknown club | returns `null` — the `ISquadProvider` contract, so the engine's own gate fails loud rather than substituting a default roster |
| F5 | `CreateSeason` given a `managedClubId` outside the league | `ArgumentException` (delegated to `SeasonState`'s own gate, which already checks it) |
| F6 | A generated squad cannot field the Stage-0 formation | impossible by KD-6, and **test-locked**: every bootstrapped squad's position counts are checked against the worst case across all shipped families, plus an end-to-end `ConfigureSquads` run through the real engine |
| F7 | Generation output moves (KD-10) | the pinned golden vector fails — deliberately loud, because the change invalidates every existing save |

F6's lock is the one that matters — it is the test that would have caught KD-6's defect had the
template been sized by eye.

---

## 5. Implementation plan

| Step | Files | Notes |
|---|---|---|
| 1 | `player-database/RosterGenerator.cs` | additive `Generate(..., PlayerPosition[] positions)` overload; existing path byte-identical |
| 2 | `season-save/LeagueBootstrapConstants.cs` | `[GT]` club count / cap / strength spread / calendar; `[FIXED]` seed domains; the position template (array-valued `[GT]` carve-out, the `TacticalInstructionsConstants` precedent) |
| 3 | `season-save/ClubNameCatalogue.cs` | APPEND-only names, ≥ `MaxClubCount` |
| 4 | `season-save/Club.cs` | `ClubId` / `Name` / `StrengthDelta` value type |
| 5 | `season-save/League.cs` | immutable result; `ISquadProvider`; `CreateSeason` |
| 6 | `season-save/LeagueBootstrap.cs` | seed derivation, strength ramp, roster generation |
| 7 | `season-save/season-save.asmdef` | `+ TacticalDirector.PlayerDatabase` |
| 8 | `season-save/tests/LeagueBootstrapTests.cs` | determinism, seed divergence, ids/names, strength ramp, F1–F6, `LineupSelector` coherence across all families, `CreateSeason` round-trip through `SeasonStateCodec` |

**Not in this landing:** the round-resolution model (A4), the calibration corpus and harness (A4a),
`SeasonLoop` (A4), the boundary roll (A5).

---

## 6. Adversarial review

### AR-1 (self, before implementation) — 1 H + 2 M + 2 L, all resolved in this text

- **H-1 — Position coherence was missing entirely.** The first draft had the bootstrap call
  `RosterGenerator.Generate(rng, streamIndex, clubId, 25)` and hand the result straight to #30. With
  uniform position draws that produces a squad `LineupSelector` refuses roughly 3% of the time per
  line per club, so a 20-club league would fail to start for most seeds — and *by seed*, which is
  the worst failure shape. Resolved: KD-6 (position template + generator overload) and F6 (the
  every-squad-every-formation lock).
- **M-1 — The strength model was going to register a new RNG stream.** Drafting KD-4 surfaced that
  a `league-bootstrap.strength` stream needs a subsystem ordinal, and the only unclaimed
  season-adjacent one is `SeasonLoop = 84`, which ERR-030-001 explicitly pins to #30 T2's first draw
  site. Registering it here would either pre-empt that allocation or create a zero-draw phantom
  stream (the FR-LW-031 class). Resolved: the strength permutation uses a local SplitMix64 exactly
  as `FixtureScheduler` already does, so A3 allocates nothing in #16.
- **M-2 — `WeakFootRating` would have been saturated.** The first strength formulation said "shift
  every attribute"; `WeakFootRating` is a `[1,5]` field, so ±3 pins nearly every club to a boundary
  and destroys the one attribute #27 deliberately kept on its own scale. Resolved: explicitly
  excluded in KD-5.
- **L-1 — Club names were going to be drawn from the seed.** Harmless but strictly worse for
  debuggability, and it would put names into the RNG budget. Resolved: KD-3 assigns by id and
  records the alternative.
- **L-2 — The calibration artifact had no invalidation trigger.** A fit against a Stage-0 engine
  silently rots when the engine's scoring changes. Resolved: KD-8 requires the engine commit SHA and
  `SNAPSHOT_SCHEMA_VERSION` in the artifact and names re-capture as the trigger.

### AR-2 (self) — 0 H + 1 M + 1 L, resolved

- **M-1 — The rejected post-processing alternative was rejected for the wrong reason.** The draft
  said it was "less clean"; the real reason is that it is only *exact* while the default `[GT]`
  generation constants keep clamping unreachable, i.e. it is a silent coupling to tuning values with
  no test that would catch a config change breaking it. Recorded properly in KD-6.
- **L-1 — `Rating(club)` was unspecified in KD-7.** "Club strength" could reasonably mean the
  strength delta, the 25-man mean, or the XI mean, and each fits differently. Pinned to the
  `LineupSelector` XI mean, with the reason (bench depth must not win Saturday's match).

### AR-3 (self) — 0 H + 0 M + 0 L — **DESIGN CONVERGENCE**

*(The design was converged here; AR-4 and AR-5 below review the resulting CODE.)*

Re-read end to end against source. Verified against the actual files rather than from memory:
`LineupSelector` really does throw on an unfillable starter line; the three shipped formation
families really do require at most GK 1 / DF 4 / MF 5 / FW 3; `RosterGenerator` really does compute
`PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex` and really does draw position uniformly over 4;
`FixtureScheduler` really does carry its own local SplitMix64 with the FR-CS-044 `unchecked` +
citation; `SeasonState.CreateNew` really does take `(clubIds, managedClubId, seed, objective,
firstRoundDay, daysBetweenRounds, seasonNumber)`; `MatchEngineConstants.NO_ROSTER_CLUB_ID` really is
`-1`; and `season-save.asmdef` really does not yet reference `TacticalDirector.PlayerDatabase` while
its test asmdef already does. No further findings — the AR cycle is closed per the project's
L-only/clean-round convention.

---

### AR-4 (self, over the shipped code) — 0 H + 2 M + 4 L, all resolved

Run fresh-eyes over the landed diff rather than the plan. Both M findings are **forward gaps** — the
code is correct as written, but A4 would have walked into them:

- **M-1 — `Rating(club)` is not reachable from where A4 will need it.** KD-7 pins the quick-sim's
  rating to the `LineupSelector` XI mean, but `LineupSelector` is `internal` to `match-engine` and
  `InternalsVisibleTo` only `MatchEngine.Tests`; `SeasonLoop` lives in `season-save`. A4 would have
  reached this at implementation time and been tempted to re-implement selection in `season-save`,
  which is the parallel-surface trap. Recorded as a named A4 prerequisite in KD-7, with the
  re-implementation route explicitly refused.
- **M-2 — A4a's harness was placed in an assembly that cannot build its own fixtures.** The draft put
  it in `src/match-engine/tests/` beside the perf harness. But controlled rating differentials need
  `LeagueBootstrap.ApplyStrength`, which is `internal` to `season-save`. Corrected to
  `src/season-save/tests/`, whose test assembly already references both `match-engine` and
  `player-database`.
- **L-1 — `MaxClubCount` was coupled to `MaxRngStreams` only implicitly.** The bootstrap registers one
  roster stream per club; at the drafted cap of 64 it exactly filled a 64-stream registry, and any
  raise would have failed *mid-generation* with `DeterministicRngService`'s generic "registry full"
  after some clubs were already built. Cap lowered to 32, an explicit coherence gate added in
  `Generate`, and the coupling locked by test.
- **L-2 — Two private copies of "how many `PlayerPosition` members are there".** `RosterGenerator`
  had one; `LeagueBootstrapConstants` was about to add a second, in a different assembly. This is the
  parallel-surface drift class the project keeps finding (PM AR-7 M-1). Hoisted to
  `PlayerDatabaseConstants.POSITION_COUNT`, both consumers delegate, and a test locks it against
  `Enum.GetValues(typeof(PlayerPosition)).Length` and the bias-table row count.
- **L-3 — Two config-driven `[GT]` values had no bound.** `LeagueStrengthSpread` is read from config
  and feeds integer ramp arithmetic: a negative value silently **inverts** the ramp (the bottom of the
  table gets the best players — wrong, and wrong quietly), and a large one overflows the products in
  `StrengthDelta`. Now bounded at its catalogue seam to `[0, ATTRIBUTE_MAX - ATTRIBUTE_MIN]`, which
  also makes the "cannot overflow" claim in `StrengthDelta`'s doc *true by construction* rather than
  by assumption. Locked by test.
- **L-4 — A negative world-day `[GT]` would wrap silently.** `FirstRoundDay` / `DaysBetweenRounds`
  are `uint` (world days) read from an `int` config getter, so a config typo of `-1` became
  4,294,967,295 and surfaced much later as a confusing calendar-overflow throw. Now refused at read
  with a message naming the key. (Also folded in: `StrengthDelta`'s divide-by-zero on a one-club
  ramp now throws rather than crashing, and `Mix` delegates to the SplitMix64 stepper instead of
  carrying a second copy of the same four lines.)

### AR-5 (self, hostile re-read of the whole landing) — 1 H + 4 M + 3 L, all resolved

AR-4 reviewed the diff. AR-5 re-read every shipped file in full and found what a diff-shaped pass
structurally cannot:

- **H-1 — Roster regeneration became a save-correctness dependency with nothing pinning it.** Squads
  are not persisted; `League` re-derives them as the `ISquadProvider`, whose contract requires the
  same roster the saved match loaded. Every determinism test on that path is self-referential, so a
  draw-order change, a catalogue reorder, or a one-line `[GT]` tweak silently rewrites every club in
  every save with the whole suite green. Resolved: KD-10 + `LeagueBootstrapGoldenVectorTests`,
  verified non-vacuous by perturbation.
- **M-1 — The world seed was write-only, so a saved career could not be reopened.** `SeasonState.Seed`
  holds the *derived* season seed and `Mix` has no implemented inverse; `WorldStore._worldSeed` was
  private with no accessor. Resolved: `WorldStore.WorldSeed` (read-only over an already-serialized
  field) + the KD-9 resume recipe + `SavedWorldSeed_RebuildsTheSameLeague`.
- **M-2 — Four documents and a code comment claimed a determinism property the code does not have.**
  "A club's roster is a function of `(worldSeed, clubId)` alone, independent of league size" is true
  only of the *base* roster; the strength ramp makes the shipped attributes size-dependent. The test
  knew (it compares identity fields only); the prose did not. Resolved: claim narrowed everywhere,
  the opposite half asserted rather than left implicit, and the #43 consequence named in KD-5.
- **M-3 — `SquadPositionCounts` was a public mutable `int[]` gating squad validity.** A mutation that
  still sums to 25 (`{25,0,0,0}`) passes the coherence check and voids the KD-6 fieldable-squad
  guarantee for every league generated afterwards. Resolved: `ReadOnlyCollection` over a private
  backing array. The array-table carve-out justifies the *config* decision, not write access.
- **M-4 — The strength model's sufficiency was unverified while being the feature's stated purpose.**
  Resolved: recorded in KD-5 and discharged as KD-8's Step 0, so A4a checks for signal before
  spending nine hours fitting to it.
- **L-1** `ClubNameCatalogue.Names` same exposure, bounded consequence — also made read-only.
  **L-2** the config readers' `<exception>` docs now say what a consumer observes
  (`TypeInitializationException` from the static initializer). **L-3** the `Generate(ulong)` overload
  had no test; it has one.

One claim made *during* this review was itself wrong and is corrected rather than quietly dropped:
`AttributeBaseMean` is **not** config-overridable — `player-database` is one of the four catalogues
carved out of the FR-CS-019 migration, so its `[GT]` rows are plain literals. The vector is a one-line
code edit that reads as a balance tweak, not a config file change.

### AR-7 (self, passes 4-6 — spec, tests, and governance) — 1 H + 4 M, all resolved

Three further passes over an unchanged artifact, each attacking a surface the previous ones had not.

- **H-1 — the calibration corpus's axis was unbuildable.** KD-8 bucketed on `dRating`, which KD-7
  defined to include `HomeAdvantageRating` — a parameter A4a exists to *fit* — and asked the harness
  to *record* it. The harness cannot: that value does not exist until after the fit, and the engine
  knows nothing about it. Two further symptoms from the same root: the ±5 grid was unreachable from
  league-generated clubs (`2·S = 6` ceiling, ~12 ordered pairs at the extremes), and the acceptance
  bar at "`dRating = 0`" described an *unequal* pairing. Resolved: the axis is now the measured,
  engine-observable `dSquad = Rating(home) − Rating(away)`; `edge = dSquad + HomeAdvantageRating` is
  named separately and lives model-side only; the harness builds pairs by direct `ApplyStrength` with
  an arbitrary delta rather than by picking league clubs; and the acceptance bar reads `dSquad = 0`.
  Cost avoided: a ~9 h corpus run bucketed on a column that cannot be filled in.
- **M-1 — `exp` and an unnamed `PoissonDraw` for persisted output.** "PoissonDraw" admits Knuth,
  inversion, or a normal approximation — different draw counts, different scorelines from the same
  key, and that scoreline is serialized in the season blob. Pinned to inverse-CDF with a
  `MaxGoalsPerSide` cap (one uniform per side, which suits the `Reserve`/`DrawReserved` discipline),
  and the float posture is now stated rather than assumed.
- **M-2 — `League`-as-`ISquadProvider` was never exercised against a real save/restore**, despite
  being A3's headline deliverable; only hand-rolled providers were tested. Added
  `DiskRoundTrip_SeasonWithLeagueBootstrappedMatch_IsDeterministic`.
- **M-3 — three test names promised properties their bodies did not assert.** WeakFoot-exclusion was
  only bounds-checked (a regression that shifted and clamped it passes); "...ArePermuted" checked no
  permutation and its message claimed "exactly one club" at each extreme when there are two; and
  roster-vs-seed divergence was detected through an attribute the strength delta also moves, so it
  could not isolate what it claimed. The first mattered most: after AR-5 the golden vector was the
  only thing locking WeakFoot-exclusion, and the documented workflow for a deliberate generation
  change is *re-pin the golden vector* — precisely when an unasserted property disappears silently.
- **M-4 — a back-prop this note claimed had been filed, had not been.** KD-6 said the generator
  change was "back-propped to `squad-player-data-design.md`"; `git log --name-only` shows that file
  was never touched — and it had been superseded anyway when #27 was promoted to section files
  (APPROVED July 22). So an APPROVED spec's implementation surface changed with no ERR and no
  spec-text patch. Resolved as **ERR-027-002** against `docs/specs/squad-player-data/`.

### AR-6 (self, over the AR-5 fixes) — 0 H + 1 M + 0 L, resolved

Re-read the fixed state rather than the fix diff — which is what surfaced the one defect the fix itself
introduced:

- **M-1 — The new golden vector pinned a 4-club league; production runs 20.** Everything that varies
  with league size was therefore unguarded: the Fisher–Yates permutation runs over `N` elements, the
  ramp denominator is `N-1`, name indexing reaches `N-1`, and `delta == 0` — and so `ApplyStrength`'s
  early-return branch — does not occur at all at `N = 4`. A generation change that happened to preserve
  a 4-club league would have shipped straight through the guard added to prevent exactly that.
  Resolved: a second pinned digest plus the full delta row at `DefaultClubCount`, fronted by a guard
  that fails if `DefaultClubCount` is ever retuned, because a golden vector silently pinned to a size
  nobody generates is worse than no vector at all.

Verified unchanged this pass: static-initialisation order on both new `ReadOnlyCollection` wrappers
(backing arrays declared first — empirically confirmed, since a null capture would throw in the tests
that read them); every consumer of the two catalogues moved to the read-only surface; and the digest
folds only integers and UTF-8 bytes, so the pinned values are platform-stable and will hold on the
Windows certification host.

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-07-25 | — | Initial supplement: A3 league bootstrap (KD-1..KD-6, KD-9) + A4a round-resolution shape and calibration methodology (KD-7, KD-8); determinism contract, F1–F6, implementation plan. AR-1 (1H+2M+2L) → AR-2 (1M+1L) → AR-3 CONVERGENCE. |
| 1.1 | 2026-07-25 | — | **A3 LANDED.** AR-4, run over the shipped code: 0H+2M+4L, all resolved. Two forward gaps A4 would have hit — `LineupSelector` is `internal` to `match-engine` so KD-7's `Rating(club)` is unreachable from `SeasonLoop` (A4 prerequisite recorded; re-implementing selection in `season-save` explicitly refused), and A4a's harness was placed in an assembly that cannot reach `ApplyStrength` (corrected to `src/season-save/tests/`). Plus: `MaxClubCount` 64 → 32 with an explicit `MaxRngStreams` coherence gate (one roster stream per club); `POSITION_COUNT` hoisted to `PlayerDatabaseConstants` so two assemblies stop carrying private copies; negative world-day `[GT]` values refused at read instead of wrapping to ~4.29e9. |
| 1.2 | 2026-07-25 | — | **AR-5** (hostile re-read of the whole landing, not the diff): 1H+4M+3L, all resolved. **H-1** the generation path is persistence-equivalent — rosters are regenerated from the world seed, not saved, so any change to it silently invalidates every save while self-referential determinism tests stay green; closed by new **KD-10** + a pinned golden vector, verified non-vacuous by perturbing `AttributeBaseMean`. **M-1** the world seed was write-only, so a saved career could not rebuild its `ISquadProvider` at all; closed by a `WorldStore.WorldSeed` accessor + the KD-9 resume recipe. **M-2** the league-size-independence claim was true only of the base roster; narrowed everywhere and the #43 consequence named. **M-3** `SquadPositionCounts` was a public mutable array gating squad validity. **M-4** the strength spread's sufficiency was unverified; discharged as KD-8 Step 0. Plus 3 L. Also corrected a claim made during the review itself: `AttributeBaseMean` is not config-overridable (`player-database` is carved out of the FR-CS-019 migration). |
| 1.3 | 2026-07-25 | — | **AR-6** (over the AR-5 fixes): 0H+1M+0L. The new golden vector pinned only a 4-club league, leaving everything that varies with league size unguarded — the permutation length, the ramp denominator, name indexing, and the `delta == 0` branch that does not occur at N=4 at all. A second digest + delta row pinned at `DefaultClubCount`, guarded so a retuned default fails loudly rather than leaving the vector pinned to a size nobody generates. |
| 1.4 | 2026-07-25 | — | **AR-7** (passes 4-6: the note as a *specification*, the test bodies vs their names, and governance): 1H+4M, all resolved. **H-1** KD-8 bucketed the calibration corpus on `dRating`, which includes the to-be-fitted `HomeAdvantageRating` — the harness was asked to record a value that cannot exist at capture time; axis re-based on the measured `dSquad`, `edge` separated model-side, pairs built by direct `ApplyStrength` rather than from league clubs. **M-1** `PoissonDraw` pinned to inverse-CDF and the float posture stated (the scoreline is persisted). **M-2** `League`-as-`ISquadProvider` now tested through a real save/restore. **M-3** three test names over-claimed (WeakFoot exclusion, delta permutation, roster-vs-seed divergence). **M-4** KD-6 claimed a back-prop that was never filed, against a spec that had since been promoted — filed as **ERR-027-002** against `docs/specs/squad-player-data/`. |
