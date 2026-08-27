# Round-2 Architecture Remediation — Design Supplement

> **Created:** August 27, 2026
> **Status:** DESIGN SUPPLEMENT — owner decisions approved; implementation not yet landed
> **Scope:** Architectural decisions D1–D4 from the round-2 adversarial review of Player Progression & Lifecycle #28 and Injuries & Medical #41, including the shared deterministic/configuration/identity/testing infrastructure they expose.
> **Governing decisions:** **D1** centralize SplitMix64 in `deterministic-sim`; **D2** centralize `[GT]` invariant validation at boot, coordinated through `GameplayConfigHolder.Bind`; **D3** make `player-database` the canonical owner of career-wide `PlayerId` uniqueness; **D4** keep progression mathematics and its oracle in `player-progression`, with `season-save` testing wiring only.
>
> This supplement defines ownership, dependency direction, affected assemblies/files, proposed APIs, constants, state, migration rules, verification requirements, and documentation back-propagation. Existing approved specs remain authoritative for gameplay behavior. This supplement governs the architectural remediation until its decisions are back-propagated into those specs.

---

## 0. Objective

The four deferred findings are four instances of the same architectural failure mode:

> **A rule exists in more than one place because the repository lacks a clear owner at the layer where the rule belongs.**

The remediation therefore establishes ownership boundaries:

1. deterministic bit mixing belongs to **Deterministic Simulation #16**;
2. configuration validity belongs to the **boot/configuration boundary**, not individual simulation calculations;
3. player identity validity belongs to **Squad / Player Data #27**;
4. progression mathematics belongs to **Player Progression #28**, not an integration test in #30.

No gameplay tuning is part of this design. Every migration must preserve current output unless a separate adversarial-review correctness finding explicitly requires a behavioral change.

---

# 1. Architectural principles

## 1.1 One rule, one production owner

A subsystem may adapt an error into its own exception type or adapt its own storage shape to a shared rule. It may not independently implement the rule.

Examples after remediation:

- `MedicalStep.DrawOccurrence` builds the injury-specific key, but does not implement SplitMix64.
- `ProgressionSaveCodec` decides whether malformed persisted state is an `InvalidOperationException`, but does not independently decide what constitutes duplicate global `PlayerId`s.
- `SeasonLoopProgressionTests` proves slot-1 wiring, but does not contain a second implementation of the progression ramp.

## 1.2 Foundation assemblies stay dependency-safe

The dependency direction must remain acyclic.

`ProjectConstants` remains a foundation assembly with no gameplay references.

`DeterministicSim` may be referenced by deterministic consumers but must not gain reverse dependencies on those consumers.

`PlayerDatabase` owns identity rules and is consumed by `PlayerProgression` and `SeasonSave`.

`SeasonSave` remains a composition-level consumer of progression, medical, training, deterministic simulation, and player database.

## 1.3 Validation and computation are distinct concerns

After D2:

- malformed `[GT]` configuration is rejected at the configuration/boot boundary;
- caller/state validity remains checked at simulation entry points;
- cheap runtime catalogue checks may remain temporarily as defense-in-depth, but are no longer the authoritative validation owner.

## 1.4 Tests may duplicate expected values, not algorithms

A fixed literal expected result is a useful oracle. A copied branch structure with the same algebra as production is not independent merely because it lives in another assembly.

Golden vectors and fixed expected cases are preferred over mirrored implementations.

---

# 2. Affected-assembly summary

| Decision | Owning assembly | Other affected assemblies | Assembly-reference change |
|---|---|---|---|
| D1 SplitMix64 | `TacticalDirector.DeterministicSim` | CollisionSystem, DecisionTree, HeadingMechanics, PassMechanics, SeasonSave, InjuriesMedical | `CollisionSystem` gains `DeterministicSim`; others already reference it |
| D2 Config validation | `TacticalDirector.ProjectConstants` for binding protocol; domain validators stay with their owning subsystem | InjuriesMedical; TrainingSystem for the cross-owned injury-risk ceiling | No reverse reference from ProjectConstants |
| D3 PlayerId uniqueness | `TacticalDirector.PlayerDatabase` | PlayerProgression, SeasonSave | None; both already reference PlayerDatabase |
| D4 Ramp oracle | `TacticalDirector.PlayerProgression.Tests` for formula verification | SeasonSave.Tests | None |

---

# 3. D1 — Canonical SplitMix64 ownership

## 3.1 Problem

The repository contains multiple implementations of SplitMix64/Stafford Mix13 behavior.

They are not all identical in calling semantics:

- some routines perform the SplitMix state increment and finalizer together;
- some manually increment state and then execute the finalizer only;
- some use the golden-ratio constant as an input salt but require only the finalizer afterward;
- keyed derivations such as injury and season resolution use a pure full SplitMix step from a value.

Centralizing these without distinguishing those forms would silently change deterministic output.

D1 therefore centralizes **three explicit operations**, not one ambiguously named `Mix()`.

## 3.2 Owner

New production owner:

`src/deterministic-sim/SplitMix64.cs`

Namespace:

`TacticalDirector.DeterministicSim`

New type:

`SplitMix64`

The type is stateless and pure except for the explicit `ref` state operation.

It does not own:

- RNG stream registration;
- domain tags;
- draw-site ordinals;
- modulo/rejection sampling;
- key packing;
- floating-point conversion.

Those remain with their current subsystem owners.

## 3.3 Canonical constants

Move the SplitMix64 algorithm literals to `DeterministicSimConstants.cs` as `[FIXED]` constants:

| New constant | Value | Meaning |
|---|---:|---|
| `SPLITMIX64_GAMMA` | `0x9E3779B97F4A7C15UL` | SplitMix64 state increment |
| `SPLITMIX64_MIX_MULTIPLIER_1` | `0xBF58476D1CE4E5B9UL` | Stafford Mix13 first multiplier |
| `SPLITMIX64_MIX_MULTIPLIER_2` | `0x94D049BB133111EBUL` | Stafford Mix13 second multiplier |

These are algorithm constants, not `[GT]` tuning values.

No domain-tag, subsystem-ordinal, stream, save-format, or digest-version value changes.

## 3.4 Required API semantics

### `Finalize(ulong value)`

Stafford Mix13 finalization only:

1. XOR/shift 30;
2. multiply by multiplier 1;
3. XOR/shift 27;
4. multiply by multiplier 2;
5. XOR/shift 31.

It does **not** add `SPLITMIX64_GAMMA`.

Use this where the caller already owns state advancement or input salting.

### `Step(ulong value)`

Pure complete SplitMix64 step:

`Finalize(value + SPLITMIX64_GAMMA)`

It does not mutate caller state.

This replaces current pure full-step helpers such as `MedicalStep.Mix`, `RoundResolutionModel.Mix`, and `ActionSelector.SplitMix64`.

### `Next(ref ulong state)`

Stateful generator operation:

1. increment `state` by `SPLITMIX64_GAMMA`;
2. return `Finalize(state)`.

All operations use deliberate `unchecked` arithmetic under Spec #16 §3.4.4.

## 3.5 Production migration inventory

### `src/collision-system/DeterministicRNG.cs`

Affected state/members:

- `_state0`
- `_state1`
- private `SplitMix64(ulong x)`

Migration:

- retain both state words;
- delete the local SplitMix implementation;
- initialize each state word through `TacticalDirector.DeterministicSim.SplitMix64.Step`;
- preserve the all-zero recovery-vector branch unchanged.

Assembly impact:

`src/collision-system/collision-system.asmdef` gains `TacticalDirector.DeterministicSim`.

### `src/decision-tree/ActionSelector.cs`

Affected member:

private `SplitMix64(ulong x)`

Retain all key packing and float mapping:

- `AgentIdBits`
- `HeartbeatBits`
- masks
- `packed`
- `combined`
- upper-24-bit conversion.

Only the full SplitMix step delegates to `SplitMix64.Step`.

### `src/heading-mechanics/HeadingRngServiceStub.cs`

Affected state:

`private ulong _state`

Current behavior manually increments gamma and then finalizes. Migration must therefore use:

`SplitMix64.Next(ref _state)`

Delete local `Mix`.

Do not retain the manual increment and then call `Step`; that would add gamma twice.

`_state` remains raw, unmixed generator state.

### `src/pass-mechanics/PassErrorCalculator.cs`

`ComputeErrorDirection` deliberately uses the SplitMix gamma as an initial salt, followed by a Stafford finalizer with **no extra gamma step**.

Migration:

- preserve tuple construction and xxHash-family input multipliers;
- source gamma from `DeterministicSimConstants.SPLITMIX64_GAMMA`;
- replace only finalizer arithmetic with `SplitMix64.Finalize(h)`.

This call site is why `Finalize` must remain distinct from `Step`.

### `src/season-save/FixtureScheduler.cs`

Affected helper:

`NextUInt64(ref ulong state)`

Migration:

- delete local SplitMix implementation;
- use `SplitMix64.Next(ref state)`;
- keep `NextBoundedUInt64` local because rejection/bounding belongs to fixture scheduling.

### `src/season-save/LeagueBootstrap.cs`

Affected helpers:

- `Mix(ulong value)`
- `NextUInt64(ref ulong state)`

Migration:

- pure seed derivation uses `SplitMix64.Step(value)`;
- stateful Fisher–Yates uses `SplitMix64.Next(ref state)`;
- delete both local algorithm implementations.

### `src/season-save/RoundResolutionModel.cs`

Affected helper:

`internal static ulong Mix(ulong value)`

Migration:

- replace internal calls with `SplitMix64.Step`;
- delete `RoundResolutionModel.Mix`.

The following remain local:

- `FixtureKey`;
- match-seed domain folding;
- `UnitFloat`;
- Poisson/scoring calculations;
- fixture key component order.

### `src/season-save/SeasonLoop.cs`

`DeriveNextSeasonSeed` currently calls `RoundResolutionModel.Mix`.

Migration:

- call `SplitMix64.Step` directly;
- retain current `seasonSeed`, `seasonNumber`, multiplication by gamma, and `SEASON_ROLL_SEED_DOMAIN` construction exactly.

### `src/injuries-medical/MedicalStep.cs`

Affected helper:

`internal static ulong Mix(ulong value)`

`DrawOccurrence` remains:

1. fold `DomainTagInjuriesMedical ^ worldSeed`;
2. fold `playerId`;
3. fold `actionOrdinal`;
4. reduce modulo `OCCURRENCE_DRAW_DENOM`.

Each fold changes from local `Mix` to `SplitMix64.Step`.

No key component, order, cast, or modulus changes.

## 3.6 Repository-wide migration guard

Completion is not defined by a remembered count of copies.

Implementation must repository-grep for:

- `0x9E3779B97F4A7C15`
- `0xBF58476D1CE4E5B9`
- `0x94D049BB133111EB`
- local `SplitMix64`
- local Mix13/Stafford finalizers.

Any production copy equivalent to the canonical 64-bit algorithm joins the migration.

SplitMix32 is separate and is not migrated by D1.

## 3.7 D1 tests

New:

`src/deterministic-sim/tests/SplitMix64Tests.cs`

Required golden categories:

- input 0;
- input 1;
- `ulong.MaxValue`;
- `SPLITMIX64_GAMMA`;
- at least two arbitrary non-pattern values;
- repeated `Next(ref state)` sequence from a fixed seed;
- `Step(x) == Finalize(unchecked(x + gamma))`;
- `Next(ref state)` mutates state exactly once.

For #41, `MedicalStepTests` adds fixed `DrawOccurrence` vectors for known:

`(worldSeed, playerId, actionOrdinal) → exact draw`

This locks domain-tag participation, fold order, signed-to-unsigned player conversion, action ordinal, SplitMix behavior, and modulus.

Any pre/post migration golden mismatch is a failed migration and must not be rebaselined automatically.

## 3.8 D1 documentation

Likely updates:

- `src/CLAUDE.md`
- `docs/agent-guides/coding-reference.md`
- `docs/agent-guides/project-reference.md`
- `docs/specs/deterministic-sim/section-3.md`
- `docs/specs/injuries-medical/section-4.md`
- `docs/specs/injuries-medical/section-6.md`
- #30 text describing local SplitMix implementations.

Centralizing the mixer does not convert #41 into a registered `DeterministicRngService` stream.

---

# 4. D2 — Boot-owned `[GT]` invariant validation

## 4.1 Problem

`GameplayConfigHolder` already binds one immutable configuration before catalogue initialization, but validity constraints are scattered through consuming calculations such as:

- `MedicalStep.ClassifySeverityFromDraw`;
- `MedicalStep.DrawOccurrence`;
- recovery countdown;
- recovery assignment;
- `AgeRiskFor`.

Consequences:

1. malformed config may be discovered only when a specific gameplay branch runs;
2. default-config tests cannot reach many malformed values;
3. catalogues and calculations both partially own configuration integrity.

Configuration validity belongs to boot.

## 4.2 Existing binding constraint

`GameplayConfigHolder.Config` locks binding on first read.

`GameplayConfigHolder.Bind` must happen before any `[GT]` catalogue initializes.

Current repository search shows no production call to `GameplayConfigHolder.Bind`; the current direct callers are tests.

D2 must not invent hidden auto-binding to conceal that fact.

It establishes the validation protocol and migrates the reviewed catalogues to it. A future executable composition root must explicitly load, validate, and bind configuration.

## 4.3 ProjectConstants owner

Modify:

`src/project-constants/GameplayConfigHolder.cs`

Add:

`src/project-constants/GameplayConfigValidation.cs`

The protocol is boot-only and explicitly supplied.

Required flow:

`validate candidate config → if all validators succeed, bind → catalogue reads may then lock binding`

Constraints:

- no reflection;
- no assembly scanning;
- no static mutable validator registry;
- no downstream assembly reference from `ProjectConstants`.

A composition root explicitly supplies validators from the domain assemblies it loads.

## 4.4 Atomic Bind semantics

Required ordering:

1. reject null config;
2. reject binding after lock;
3. validate validator list/plan;
4. execute every validator against candidate `GameplayConfig`;
5. only if all succeed, assign `s_config`;
6. leave holder lock behavior otherwise unchanged.

If validation throws:

- `s_config` remains `GameplayConfig.Empty`;
- `s_locked` remains false;
- corrected config may be retried;
- no gameplay catalogue may have initialized during validation.

Validators may read `GameplayConfig`, but must not read catalogue statics whose initializers read `GameplayConfigHolder.Config`.

## 4.5 Domain validators are separate from catalogue initialization

New:

`src/injuries-medical/InjuriesMedicalConfigValidation.cs`

Where validator logic needs config key/default metadata embedded in a static catalogue initializer, move that metadata into a non-initializing schema/helper owned by the same assembly.

The runtime `public static readonly` field remains the gameplay constant. Schema metadata is loader metadata, not a second gameplay value.

## 4.6 InjuriesMedical configuration surface

Current #41 keys:

- `RecoveryMax` — fallback 240
- `RecoveryDaysPerTickBase` — 1
- `SeverityMinorPermille` — 600
- `SeverityModeratePermille` — 300
- `TrainingRiskPassthroughWeight` — 1
- `AppearanceLoadWeight` — 5600
- `BaselineDailyRisk` — 4000
- `AppearanceWindowDays` — 7
- `HardContactWeight` — 0
- `AgeRiskPivotYears` — 26
- `AgeRiskPerYearFromPivot` — 150
- `AgeRiskSpan` — 1800

The validator enforces already-normative invariants only.

### Recovery

- `RecoveryMax >= 1`
- `RecoveryDaysPerTickBase > 0`

### Severity

- `SeverityMinorPermille >= 0`
- `SeverityModeratePermille >= 0`
- `(long)minor + moderate < SEVERITY_PERMILLE_DENOM`

The widened sum is mandatory so boot validation does not centralize the arithmetic overflow defect found by the arithmetic lens.

### Appearance window

`AppearanceWindowDays` must satisfy the structural `[1,31]` contract of the u32 appearance record.

### Age term

- `AgeRiskPerYearFromPivot >= 0`
- `AgeRiskSpan >= 0`

`AgeRiskPivotYears` remains unrestricted by config-integrity validation because extreme values remain defined.

### No new tuning rules

Do not silently invent new structural rules for:

- `TrainingRiskPassthroughWeight`
- `AppearanceLoadWeight`
- `BaselineDailyRisk`
- `HardContactWeight`

unless their governing spec already defines one or a separate ERR/spec correction is filed.

## 4.7 Cross-owned `InjuryRiskMax`

`InjuriesMedicalConstants.InjuryRiskMax` is `[CROSS]`.

Owner:

`TrainingSystemConstants.InjuryRiskMax`

Config key:

`[training-system] InjuryRiskMax`

fallback:

`16000`

#41 additionally requires:

`0 < InjuryRiskMax <= OCCURRENCE_DRAW_DENOM`

The architecture must not initialize `TrainingSystemConstants` during pre-bind validation.

Recommended shape:

- add a pure training-system config resolver/validator for the owned key;
- #41's validator obtains the candidate value through that non-catalogue resolver;
- #41 validates compatibility with its fixed denominator.

Ownership remains:

- #29 owns the configurable value;
- #41 owns compatibility with #41's probability denominator.

## 4.8 Runtime guard policy after D2

### Keep permanently as caller/state checks

- invalid `MedicalModifier`;
- negative `ageYears`;
- invalid `InjuryState`;
- bad severity call preconditions;
- invalid purpose ordinal;
- day-order/state-coherence checks.

### Boot owner + optional defense-in-depth

- recovery config guards;
- severity config guards;
- age slope/span guards;
- injury-risk ceiling guard.

Initially these may remain as cheap defense-in-depth. Their comments must state that boot validation is authoritative.

After boot validation is proven and a production composition root exists, redundant hot-path checks may be removed deliberately.

## 4.9 D2 tests

### ProjectConstants

Modify:

`src/project-constants/tests/GameplayConfigHolderTests.cs`

Add cases proving:

- validation runs before binding;
- validator receives the candidate config;
- multiple validators run deterministically in supplied order;
- validation failure leaves holder unbound and unlocked;
- retry after correction succeeds;
- null validators are rejected;
- Bind-after-first-read fails before validator execution.

### InjuriesMedical

Malformed-config cases:

- `RecoveryMax = 0`
- `RecoveryDaysPerTickBase = 0`
- negative Minor
- negative Moderate
- severity sum exactly 1000
- severity sum above 1000
- extreme severity values that overflow an `int` sum
- `AppearanceWindowDays = 0`
- `AppearanceWindowDays = 32`
- negative age slope
- negative age span.

### TrainingSystem / cross invariant

Add:

- `InjuryRiskMax = 0`
- negative
- exactly `OCCURRENCE_DRAW_DENOM`
- greater than denominator.

No test should mutate process-global catalogue state to reach these conditions.

---

# 5. D3 — Canonical career-wide PlayerId uniqueness

## 5.1 Problem

The rule that one career must not carry the same `PlayerId` in two clubs is repeated in multiple places.

Current implementations include separate dictionary walks in:

- `ProgressionEngine`
- `ProgressionSaveCodec`
- `PlayerCareerStates`.

The identity rule belongs to #27.

Consumers need different exception semantics, but not different predicate implementations.

## 5.2 Owner

New:

`src/player-database/PlayerIdUniqueness.cs`

Assembly:

`TacticalDirector.PlayerDatabase`

No new assembly dependency is needed.

## 5.3 Data model

Recommended result value:

`PlayerIdCollision`

Fields:

- `PlayerId`
- `ExistingClubId`
- `IncomingClubId`

It is a value type.

No gameplay state is stored globally.

## 5.4 Validation helper

Recommended primitive:

`PlayerIdUniquenessTracker` or equivalent.

Internal state:

`PlayerId → first owning ClubId`

Operation:

attempt to add `(clubId, playerId)`.

Result:

- unique: accepted;
- duplicate: returns `PlayerIdCollision`.

The owner defines:

- what constitutes a collision;
- first-owner tracking;
- duplicate detection.

Each consumer traverses its own local storage shape:

- `ProgressionEngine` carries `PlayerRecord[]`;
- `ProgressionSaveCodec` carries `ClubCareerStates[]`;
- `PlayerCareerStates` carries `_clubIds` + `_playerIds`.

No common DTO is required merely to centralize the rule.

## 5.5 `PlayerRecord.cs`

Update documentation in:

`src/player-database/PlayerRecord.cs`

The file already states the career-global requirement through ERR-027-004 / ERR-041-019.

Restate enforcement:

- #27 defines and implements the canonical uniqueness predicate;
- career/composition boundaries invoke it and choose boundary-specific failure semantics.

No field, type, or save-shape change.

## 5.6 ProgressionEngine migration

Current local `RequireGloballyUniquePlayerIds` dictionary implementation is removed.

A small boundary adapter may remain for exception wording/parameter name, but detection delegates to #27.

Preserve `SeedFrom` / `FromBlocks` exception semantics.

No change to:

- `_clubIds`
- `_records`
- `_lifecycles`
- `_nextPlayerId`
- id-cursor logic.

## 5.7 ProgressionSaveCodec migration

Affected members:

- `DescribeGlobalDuplicatePlayerId`
- `RequireGloballyUniquePlayerIds`

Preserve asymmetric failure contract:

- encode/input path → argument failure;
- decoded persisted corruption → invalid-operation/state failure.

A local adapter may convert `PlayerIdCollision` to existing text, but duplicate detection delegates to #27.

No local `Dictionary<PlayerId, ClubId>` remains after D3.

## 5.8 PlayerCareerStates migration

Current:

`PlayerCareerStates.RequireGloballyUniquePlayerIds`

Only duplicate detection moves.

The class still decides where to enforce it:

- `ForLeague`;
- block reconstruction;
- roster-sync validation.

It retains local exception/parameter semantics.

## 5.9 D3 tests

New:

`src/player-database/tests/PlayerIdUniquenessTests.cs`

Required:

- empty tracker/input;
- one player;
- multiple unique players in one club;
- duplicate numeric ID in same club;
- duplicate ID across clubs;
- negative ID if accepted structurally — range validity is separate from uniqueness;
- first-owner information is correct.

Existing boundary tests remain in:

- `ProgressionEngineTests`
- `ProgressionSaveCodecTests`
- `PlayerCareerStatesTests`.

Those prove boundary invocation and failure semantics, not the dictionary algorithm.

## 5.10 D3 documentation

Primary back-prop:

Squad / Player Data #27.

Likely touched:

- `docs/specs/squad-player-data/section-1.md`
- `section-2.md`
- `section-7.md`
- relevant version histories
- `docs/tracking/data-contract-index.md`.

#28 and #41 should cross-reference #27 as owner rather than describing themselves as independent owners.

Historical ERR-027-004 / ERR-041-019 records remain history and are not rewritten as though ownership was always centralized.

---

# 6. D4 — Progression formula oracle ownership

## 6.1 Problem

`src/season-save/tests/SeasonLoopProgressionTests.cs` contains:

`ExpectedRampAccrual(...)`

It reproduces #28's ramp integral with nearly the same branches and algebra as production.

It was historically described as an “independent” implementation. It is not independent in the architectural sense.

#30 needs to verify that slot 1 calls #28 correctly. It does not need to own a second implementation of #28's mathematics.

## 6.2 Formula owner remains AbilityModel

Current production owner:

`src/player-progression/AbilityModel.cs`

Relevant members remain:

- `DailyBandPoints`
- `TestOnly_DailyBandPoints`
- `AccruedBandPoints`
- `TestOnly_AccruedBandPoints`
- `RampHalfWidthDays`
- `GrowthPhaseDays`
- `DeclinePhaseDays`
- `ConstructionDayCredit`
- `ClassifyAgeBand`.

Do not move formula logic into SeasonSave.

Do not create another public formula API solely for integration tests.

## 6.3 Formula-level test ownership

Move mathematical proof entirely into:

`TacticalDirector.PlayerProgression.Tests`

Prefer either extending `AbilityModelTests.cs` or adding focused:

`src/player-progression/tests/AbilityModelRampTests.cs`

Formula tests must cover:

- zero-width identity;
- growth ramp beginning/midpoint/end;
- stable region;
- decline ramp beginning/midpoint/end;
- one-day continuity;
- full integral/P5 property;
- disjoint-ramp guard;
- integer-overflow boundary;
- construction-day credit;
- age-ceiling behavior;
- representative fixed golden vectors.

Expected values are literals, not calls back into the implementation.

## 6.4 SeasonSave integration test after D4

Modify:

`src/season-save/tests/SeasonLoopProgressionTests.cs`

Delete:

`ExpectedRampAccrual(...)`

Remove formula-local constants/calculations used only to reproduce the algorithm, including as applicable:

- `GrowthRampStartsAt`
- `GrowthRampEndsAt`
- `DeclineRampStartsAt`
- `DeclineRampEndsAt`
- `edge`
- `h`
- `u`
- `v`
- `n1`
- `n2`.

## 6.5 Replacement integration strategy

#30 tests only:

1. slot 1 runs;
2. it advances the correct players;
3. each player advances exactly once on expected days;
4. representative values reaching #30 match fixed approved #28 outputs.

Use representative players in:

- full Growth;
- ramp;
- Stable;
- full Decline.

Expected cursor values are hardcoded golden values independently reviewed before landing.

The test must not calculate expectations through:

- `AbilityModel.AccruedBandPoints`;
- `DailyBandPoints`;
- copied G(n)/D(n) algebra.

Thus:

**#28 tests answer “is the mathematics correct?”**

**#30 tests answer “did composition invoke #28 correctly?”**

## 6.6 Mutation responsibility

PlayerProgression formula tests must kill mutations such as:

- ramp half-width forced to zero;
- edge shifted;
- sign inverted;
- phase removed;
- midpoint branch changed;
- integer widening removed.

SeasonSave tests need only kill integration mutations such as:

- slot 1 removed;
- slot 1 called twice;
- wrong world day supplied;
- wrong player/store supplied;
- result not persisted to carried progression state.

---

# 7. Cross-decision interactions

## 7.1 D1 + #41 mutation H1

D1 centralizes SplitMix64, but #41 still requires fixed `DrawOccurrence` vectors.

`SplitMix64Tests` protect algorithm semantics.

`MedicalStepTests` protect #41 key composition.

A correct shared mixer cannot protect a wrong fold order.

## 7.2 D2 + severity arithmetic overflow

Boot validation must use `long` before summing severity numerators.

The production severity-classification arithmetic must later use the same widening.

Architecture must not centralize an overflow bug.

## 7.3 D3 + D1

Global `PlayerId` uniqueness remains necessary because #41 folds `playerId`, not `clubId`, into its draw key.

D1 does not alter that key.

D3 makes the prerequisite one owned identity rule.

## 7.4 D4 + construction-day credit

`AbilityModel.ConstructionDayCredit` remains the single production owner.

D4 concerns the ramp **test oracle**, not this already-centralized production rule.

The later mutation fix for regen at a ramp age must test the call site without recreating the ramp formula.

---

# 8. File-change inventory

## 8.1 New production files

Expected:

- `src/deterministic-sim/SplitMix64.cs`
- `src/project-constants/GameplayConfigValidation.cs`
- `src/injuries-medical/InjuriesMedicalConfigValidation.cs`
- training-system config-validation/schema helper as needed for `InjuryRiskMax`
- `src/player-database/PlayerIdUniqueness.cs`

## 8.2 New test files

Expected/preferred:

- `src/deterministic-sim/tests/SplitMix64Tests.cs`
- `src/player-database/tests/PlayerIdUniquenessTests.cs`
- optionally `src/player-progression/tests/AbilityModelRampTests.cs`

## 8.3 Existing production files modified

### D1

- `src/deterministic-sim/DeterministicSimConstants.cs`
- `src/collision-system/DeterministicRNG.cs`
- `src/collision-system/collision-system.asmdef`
- `src/decision-tree/ActionSelector.cs`
- `src/heading-mechanics/HeadingRngServiceStub.cs`
- `src/pass-mechanics/PassErrorCalculator.cs`
- `src/season-save/FixtureScheduler.cs`
- `src/season-save/LeagueBootstrap.cs`
- `src/season-save/RoundResolutionModel.cs`
- `src/season-save/SeasonLoop.cs`
- `src/injuries-medical/MedicalStep.cs`

Any additional local Mix13 production implementation found by the final sweep joins this list.

### D2

- `src/project-constants/GameplayConfigHolder.cs`
- `src/injuries-medical/InjuriesMedicalConstants.cs`
- `src/injuries-medical/MedicalStep.cs`
- `src/training-system/TrainingSystemConstants.cs` if key/default extraction is needed.

### D3

- `src/player-database/PlayerRecord.cs`
- `src/player-progression/ProgressionEngine.cs`
- `src/player-progression/ProgressionSaveCodec.cs`
- `src/season-save/PlayerCareerStates.cs`

### D4

No production behavior change is expected.

`AbilityModel.cs` needs only documentation/version-history changes if test ownership is recorded there.

---

# 9. Assembly-definition impact

## 9.1 Required

`src/collision-system/collision-system.asmdef`

adds:

`TacticalDirector.DeterministicSim`

## 9.2 No expected change

Already reference DeterministicSim:

- DecisionTree
- HeadingMechanics
- PassMechanics
- InjuriesMedical
- SeasonSave.

PlayerProgression and SeasonSave already reference PlayerDatabase.

ProjectConstants must remain reference-free.

D2 therefore uses callbacks/data supplied from above rather than ProjectConstants importing TrainingSystem or InjuriesMedical.

---

# 10. Versioning and persistence impact

D1–D4 must not change:

- save blob format versions;
- deterministic digest format versions;
- domain-tag allocations;
- subsystem ordinals;
- player-record binary shape;
- medical-state binary shape;
- progression-state binary shape;
- RNG stream registration;
- RNG draw order.

D1 is deterministic-output-sensitive despite being intended as behavior-neutral.

A golden mismatch is a failed migration, not a reason to rebaseline.

No determinism version is bumped for a byte-identical refactor.

---

# 11. Documentation/governance back-propagation

## D1

Back-prop to Deterministic Simulation #16:

- canonical SplitMix64 helper ownership;
- finalizer/step/stateful-next distinction;
- local copies prohibited.

Update downstream docs that currently call implementations “local”.

## D2

Back-prop to Code Standards #20:

- `[GT]` configuration with structural/range invariants is validated before catalogue use;
- `GameplayConfigHolder.Bind` is the boot coordination boundary;
- validators are explicit, not discovered by reflection/static registration.

Update #41 Appendix A/sections that currently present consuming sites as authoritative config guards.

## D3

Back-prop to Squad / Player Data #27:

- career-global uniqueness is #27's canonical identity invariant;
- shared implementation lives in PlayerDatabase;
- consumers adapt failure semantics.

## D4

Update #28 test-plan language:

- formula verification belongs to PlayerProgression.Tests.

Correct #30/SeasonSave tracking language describing the old copied ramp oracle as “independent”.

Historical changelog statements are corrected through new entries where history policy prohibits rewriting old entries.

---

# 12. Implementation sequence

## Commit A — D1 deterministic helper

1. add canonical constants;
2. add `SplitMix64`;
3. add golden tests;
4. migrate one consumer at a time;
5. add CollisionSystem assembly reference;
6. run per-assembly tests after each migration;
7. run whole-tree gate and deterministic/golden suites;
8. run final literal/helper grep.

Do not combine D1 with unrelated arithmetic fixes.

## Commit B — D2 boot validation

1. add ProjectConstants validation protocol;
2. strengthen `GameplayConfigHolder.Bind` atomicity tests;
3. add #41 validator;
4. add #29 raw resolver/validator needed for `InjuryRiskMax`;
5. add malformed-config cases;
6. restate runtime guards as defense-in-depth;
7. back-prop Code Standards/#41/#29 docs.

## Commit C — D3 identity owner

1. add #27 utility/result;
2. add #27 owner tests;
3. migrate ProgressionEngine;
4. migrate ProgressionSaveCodec;
5. migrate PlayerCareerStates;
6. remove duplicated predicates/dictionaries;
7. preserve each boundary's exception semantics;
8. back-prop #27 and consumer docs.

## Commit D — D4 oracle ownership

1. add/strengthen #28 golden formula tests;
2. record expected integration literals;
3. delete `ExpectedRampAccrual` from SeasonSave.Tests;
4. simplify SeasonSave wiring proof;
5. mutation-check formula owner and wiring owner separately;
6. correct false “independent implementation” description.

Only after A–D are verified should the remaining arithmetic/mutation/governance adversarial findings be applied.

---

# 13. Acceptance gates

## D1

- one SplitMix64 implementation exists in production;
- no local Stafford Mix13 copy remains outside `deterministic-sim`;
- full-step vs finalizer-only semantics are explicit;
- deterministic golden vectors remain unchanged;
- whole-tree gate including MatchEngine passes.

## D2

- malformed reviewed `[GT]` configurations are rejectable without executing a gameplay day;
- validation failure does not partially bind config;
- validators do not initialize catalogues before binding;
- #41 config invariants have one boot owner;
- runtime state/caller guards remain distinct.

## D3

- #27 contains the sole duplicate-ID predicate;
- ProgressionEngine, ProgressionSaveCodec, and PlayerCareerStates do not maintain independent duplicate-detection dictionaries;
- each boundary preserves existing failure type and diagnostic context.

## D4

- no copy of the ramp integral remains in SeasonSave.Tests;
- PlayerProgression.Tests kill formula mutations;
- SeasonSave.Tests kill wiring mutations;
- integration expectations do not calculate themselves through #28.

## Cross-cutting

- no circular assembly reference;
- no save/determinism version bump;
- source headers/version histories updated;
- owning specs/back-props land with implementation;
- tracking documents describe actual verification, not inferred verification.

---

# 14. Explicit non-goals

This architectural pass does **not** itself:

- retune injury probabilities;
- reshape the young-player age-risk curve;
- change retirement behavior;
- change progression ramp constants;
- change player IDs;
- change regen allocation;
- add a registered #41 RNG stream;
- change domain tags;
- change save formats;
- fix every arithmetic/mutation/governance finding.

Those correctness fixes follow after D1–D4 establish their final owners.

---

# 15. Final architecture after remediation

**Deterministic Simulation #16** owns SplitMix64 mathematics.

**Project Constants / boot boundary** owns validation sequencing and binding atomicity.

**Training System #29 / Injuries & Medical #41** own their configuration schemas and domain invariants.

**Squad / Player Data #27** owns player identity and career-global ID uniqueness.

**Player Progression #28** owns progression mathematics and formula-level proofs.

**Season & Competition Loop #30** owns composition and verifies that those systems are invoked correctly.

No consumer retains a second implementation of an upstream rule.

That is the architectural target against which the subsequent adversarial-review fixes should be written.
