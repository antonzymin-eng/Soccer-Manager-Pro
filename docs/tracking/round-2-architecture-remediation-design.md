# Round-2 Architecture Remediation — Design Supplement

> **Created:** August 27, 2026
> **Amended:** August 27, 2026 — hostile-review closure for D1 equivalence, D2 production boot/atomicity, D3 lifecycle enforcement, and D4 oracle provenance
> **Status:** DESIGN SUPPLEMENT — owner decisions approved; hostile-review blockers resolved in design; implementation not yet landed
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

### `Mix13(ulong value)`

Stafford Mix13 finalization only:

1. XOR/shift 30;
2. multiply by multiplier 1;
3. XOR/shift 27;
4. multiply by multiplier 2;
5. XOR/shift 31.

It does **not** add `SPLITMIX64_GAMMA`.

The name is deliberately `Mix13`, not `Finalize`: `Finalize` carries CLR finalization/destructor meaning in C# and would make a pure integer transform look lifecycle-related.

Use `Mix13` only where the caller already owns state advancement or input salting.

### `Step(ulong value)`

Pure complete SplitMix64 step:

`Mix13(unchecked(value + SPLITMIX64_GAMMA))`

It does not mutate caller state.

This replaces current pure full-step helpers such as `MedicalStep.Mix`, `RoundResolutionModel.Mix`, and `ActionSelector.SplitMix64`.

### `Next(ref ulong state)`

Stateful generator operation:

1. increment `state` by `SPLITMIX64_GAMMA`;
2. return `Mix13(state)`.

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
- replace only finalizer arithmetic with `SplitMix64.Mix13(h)`.

This call site is why `Mix13` must remain distinct from `Step`.

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

## 3.7 D1 tests and migration-equivalence locks

New owner test:

`src/deterministic-sim/tests/SplitMix64Tests.cs`

Required golden categories:

- input 0;
- input 1;
- `ulong.MaxValue`;
- `SPLITMIX64_GAMMA`;
- at least two arbitrary non-pattern values;
- repeated `Next(ref state)` sequence from a fixed seed;
- `Step(x) == Mix13(unchecked(x + gamma))`;
- `Next(ref state)` mutates state exactly once.

Those tests prove the shared primitive only. They are **not sufficient** to prove migration equivalence at a consumer whose key packing, state advancement, float mapping, bounding, or fold order can be changed accidentally.

Before replacing each local implementation, the implementation commit must add or identify fixed external-contract vectors produced by the **pre-migration code at the exact pre-migration commit SHA**. The expected literals are then frozen before that consumer is edited. The same vectors must pass after delegation to `SplitMix64`.

Required consumer locks:

| Consumer | Pre/post behavior that must be frozen |
|---|---|
| `collision-system/DeterministicRNG` | first several outputs from at least two fixed seeds, including all-zero recovery behavior |
| `decision-tree/ActionSelector` | exact selection/random scalar for representative fixed `(agentId, heartbeat, seed/input)` tuples |
| `heading-mechanics/HeadingRngServiceStub` | exact repeated stateful sequence from fixed seeds |
| `pass-mechanics/PassErrorCalculator` | exact error-direction outputs for fixed input tuples, including the finalizer-only path |
| `season-save/FixtureScheduler` | exact bounded draws or resulting fixture permutation for fixed state |
| `season-save/LeagueBootstrap` | exact seed-derived ordering/permutation for fixed bootstrap inputs |
| `season-save/RoundResolutionModel` | exact mixed fixture/match derivations for fixed fixture keys/seeds |
| `season-save/SeasonLoop.DeriveNextSeasonSeed` | exact next-season seed vectors |
| `injuries-medical/MedicalStep.DrawOccurrence` | exact `(worldSeed, playerId, actionOrdinal) → draw` vectors |

A migrated consumer with no pre/post lock is not complete even if `SplitMix64Tests` passes.

Golden provenance rules:

1. record the pre-migration source commit SHA beside the vector;
2. capture the public/internal contract result **before** replacing the local mixer;
3. store the expected output as a literal;
4. do not generate the post-migration expectation from the new helper;
5. any mismatch is a failed migration and must not be automatically rebaselined.

## 3.8 D1 documentation

Required updates:

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

## 4.2 Existing binding constraint and mandatory production wiring

`GameplayConfigHolder.Config` locks binding on first read.

`GameplayConfigHolder.Bind` must happen before any `[GT]` catalogue initializes.

The repository currently has no production call to `GameplayConfigHolder.Bind`; direct callers are tests. That is not a harmless Stage-0 detail once D2 claims boot-owned validation.

**D2 may not be declared complete while that remains true.**

The implementation must identify the production composition entry point that first constructs the career/season simulation and wire the following sequence there before any `[GT]` catalogue access:

1. load/parse the candidate `GameplayConfig`;
2. build the explicit validator plan;
3. validate the candidate and obtain a `ValidatedGameplayConfig`;
4. bind that validated value;
5. only then construct/read gameplay catalogues and simulation services.

If the repository still has no executable/composition entry point capable of owning that sequence when D2 implementation begins, **D2 is BLOCKED, not partially complete**. Validation infrastructure may land only if tracking continues to mark D2 open; the D2 acceptance gate cannot pass until a real production call site exists and is tested. No “future composition root” deferral is permitted.

No hidden auto-binding, reflection discovery, or static-registration mechanism may substitute for the explicit production call.

## 4.3 ProjectConstants API and ownership

Modify:

`src/project-constants/GameplayConfigHolder.cs`

Add:

`src/project-constants/GameplayConfigValidation.cs`

Required types/API:

- `public delegate void GameplayConfigValidator(GameplayConfig candidate)`;
- `public sealed class ValidatedGameplayConfig` whose constructor is not publicly callable;
- `GameplayConfigValidation.Validate(GameplayConfig candidate, IReadOnlyList<GameplayConfigValidator> validators) -> ValidatedGameplayConfig`;
- `GameplayConfigHolder.Bind(ValidatedGameplayConfig validated)`.

After D2 there is **no production overload** `Bind(GameplayConfig)`. A caller cannot publish an unvalidated candidate by bypassing the validation coordinator.

Constraints:

- no reflection;
- no assembly scanning;
- no static mutable validator registry;
- no downstream assembly reference from `ProjectConstants`;
- validators are supplied explicitly and in deterministic order by the composition root;
- a `ValidatedGameplayConfig` is only a boot capability token proving the candidate completed that plan; it is not gameplay state and is never serialized.

## 4.4 Atomicity and validator-purity contract

Validation is deliberately separated from publication.

Required validation flow:

1. reject null candidate;
2. reject null validator list or null validator entries;
3. enter ProjectConstants' internal “validation in progress” scope;
4. execute each validator against the **candidate instance only**;
5. leave the scope in `finally`;
6. only if all validators succeed, return `ValidatedGameplayConfig`.

Required holder flow:

1. reject null validated token;
2. reject binding after the holder has locked;
3. assign the token's candidate to `s_config`;
4. leave existing first-read lock semantics unchanged.

`GameplayConfigHolder.Config` must check the internal validation-in-progress flag **before** setting `s_locked`. A validator that reads the holder therefore fails immediately without publishing or locking a candidate.

Validator purity is a structural boot contract:

- validators may read only their `GameplayConfig candidate`, literal/schema metadata, and pure resolver functions;
- validators must not read `GameplayConfigHolder.Config`;
- validators must not touch static catalogue fields whose initializers read the holder;
- validators must not mutate global/process state.

Atomicity guarantees are intentionally precise:

- an ordinary malformed-config exception leaves `s_config == GameplayConfig.Empty` and `s_locked == false`; a corrected candidate may be validated and retried;
- an attempted holder read during validation throws before the holder locks;
- if a broken validator triggers a CLR static catalogue initializer and poisons that type, boot aborts as a programmer error and **no in-process retry is promised**. The previous supplement's unconditional retry claim was too strong because CLR type-initializer failure is not roll-backable.

Thus D2 guarantees “no invalid candidate is published,” not the impossible claim that arbitrary validator side effects can be transactionally undone.

## 4.5 Domain validators are pure and separate from catalogue initialization

Required new files:

- `src/injuries-medical/InjuriesMedicalConfigValidation.cs`;
- `src/training-system/TrainingSystemConfigValidation.cs`.

Each owns literal key names, fallback metadata, pure candidate resolvers, and validation for its domain without reading runtime catalogue statics.

The runtime `public static readonly` catalogue fields remain the gameplay values loaded after bind. Validation metadata is loader/schema metadata, not a second mutable gameplay value.

Required composition plan order for the reviewed surface:

1. `TrainingSystemConfigValidation.Validate`;
2. `InjuriesMedicalConfigValidation.Validate`.

That order is explicit for diagnostics only; correctness must not depend on one validator mutating state for another.

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

Required shape:

- `TrainingSystemConfigValidation.ResolveInjuryRiskMax(GameplayConfig candidate)` reads the owned key/fallback without touching `TrainingSystemConstants`;
- `TrainingSystemConfigValidation.Validate` enforces #29-owned invariants on that candidate value;
- `InjuriesMedicalConfigValidation.Validate` obtains the same candidate value through `ResolveInjuryRiskMax` and enforces #41 compatibility with `OCCURRENCE_DRAW_DENOM`;
- neither validator reads a runtime catalogue static.

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

### Remove as authoritative config checks from production hot paths

Once the production boot sequence in §4.2 is wired and tested, production config-backed calculations must not independently re-implement:

- recovery config validity;
- severity config validity;
- age slope/span config validity;
- injury-risk ceiling validity.

That rule has one boot owner after D2.

Parameterised `TestOnly_*` seams may still reject invalid explicit arguments as **function preconditions** so tests can exercise arithmetic safely; those checks must not read `GameplayConfigHolder` or runtime catalogue statics and must be documented as call-contract checks, not configuration ownership.

D2 does not land in a state where hot-path guards are called “temporary” with removal deferred to an unspecified future pass.

## 4.9 D2 tests

### ProjectConstants

Modify:

`src/project-constants/tests/GameplayConfigHolderTests.cs`

Add cases proving:

- validation receives the exact candidate instance;
- validators run in supplied order;
- malformed candidate failure produces no `ValidatedGameplayConfig`;
- malformed candidate failure leaves holder empty and unlocked;
- corrected candidate can be validated and bound afterward;
- null candidate/list/validator entries are rejected;
- a validator reading `GameplayConfigHolder.Config` fails before locking the holder;
- `Bind` accepts only a `ValidatedGameplayConfig`;
- Bind-after-first-read fails before publication;
- no raw production `Bind(GameplayConfig)` escape hatch remains.

### InjuriesMedical

Malformed-config cases:

- `RecoveryMax = 0`;
- `RecoveryDaysPerTickBase = 0`;
- negative Minor;
- negative Moderate;
- severity sum exactly 1000;
- severity sum above 1000;
- extreme severity values that overflow an `int` sum;
- `AppearanceWindowDays = 0`;
- `AppearanceWindowDays = 32`;
- negative age slope;
- negative age span.

### TrainingSystem / cross invariant

Add:

- `InjuryRiskMax = 0`;
- negative;
- exactly `OCCURRENCE_DRAW_DENOM`;
- greater than denominator.

No domain-validation test may mutate process-global catalogue state to reach these conditions.

### Production boot integration — mandatory gate

Add a test at the actual production composition boundary proving:

1. a malformed reviewed config fails **before** the first gameplay catalogue/service is constructed;
2. a valid non-default reviewed config is validated, bound, and then observed by the real catalogue;
3. attempting to construct/read a catalogue before binding causes the existing fail-loud late-bind behavior;
4. the production boot path contains the validator plan explicitly.

Without this production-boundary test and call site, D2 remains open regardless of ProjectConstants/domain unit-test coverage.

---

# 5. D3

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

`src/player-database/PlayerIdRegistry.cs`

Assembly:

`TacticalDirector.PlayerDatabase`

Required public value type:

`PlayerIdCollision`

Fields:

- `PlayerId`;
- `ExistingClubId`;
- `IncomingClubId`.

Required non-static, per-career registry:

`PlayerIdRegistry`

The registry is derived identity state, not serialized gameplay state. It is rebuilt from canonical roster records on new-game/load and then updated atomically with roster membership changes.

There is no static/global registry.

## 5.3 Required registry operations

The #27 owner defines the only production implementation of career-wide ownership:

- `TryRegisterExisting(ClubId clubId, PlayerId playerId, out PlayerIdCollision collision)`;
- `TryReserveNew(ClubId clubId, PlayerId playerId, out PlayerIdCollision collision)`;
- `Move(PlayerId playerId, ClubId expectedFromClubId, ClubId toClubId)`;
- `Remove(PlayerId playerId, ClubId expectedClubId)`;
- read-only owner lookup for diagnostics/tests.

Semantics:

- registering/reserving an already-owned numeric id reports the first owner;
- `Move` requires that the id is currently owned by `expectedFromClubId` and changes that single ownership entry rather than transiently registering the id twice;
- `Remove` requires the expected current owner;
- failed operations do not mutate the registry.

Consumers may adapt a returned collision or registry-state failure into their boundary-specific exception type. They may not implement a second `PlayerId -> ClubId` predicate.

## 5.4 Lifecycle-wide enforcement boundary

A dictionary utility alone is insufficient. D3 therefore governs **all roster membership mutations**, not only save/load validation.

Today, `ProgressionEngine` explicitly documents itself as the sole writer of the evolving career roster. D3 makes a `PlayerIdRegistry` part of that authority:

- `SeedFrom` rebuilds/registers every day-0 carried player through the registry;
- `FromBlocks` rebuilds/registers every persisted carried player through the registry;
- the monotonic `_nextPlayerId` cursor remains #28's allocator, but any future regen insertion must `TryReserveNew` before committing the roster mutation;
- retiree removal must `Remove` in the same staged mutation;
- a transfer/club move must use `Move`, never remove-then-register as two externally visible operations.

`PlayerCareerStates` is companion state synchronized to the authoritative roster. Its `ForLeague`, `FromBlocks`, and `PrepareRosterSync` boundaries rebuild/validate through the #27 registry implementation so a drifted external roster fails before state arrays are committed.

Atomic mutation rule:

1. stage the proposed roster change;
2. stage/validate the corresponding registry operation;
3. only after both succeed, commit roster membership and the registry change together;
4. on validation failure, neither side changes.

Future #31 transfer, #42 youth-intake, import, clone, or other roster-producing code is not allowed to introduce a direct membership write that bypasses this boundary.

Implementation completion requires a repository-wide inventory of production writes to roster membership / `PlayerId` insertion. Every write must either route through `PlayerIdRegistry` or be documented as incapable of changing membership. A grep count alone is evidence, not the ownership mechanism.

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

`ProgressionEngine` owns a per-career `PlayerIdRegistry` alongside its roster arrays. Construction/reconstruction populates it through #27; future membership mutation stages registry and roster updates together. A small boundary adapter may remain for exception wording/parameter name, but detection delegates to #27.

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

Only the predicate implementation moves; the class still decides where companion-state validation is required. It rebuilds/validates through `PlayerIdRegistry` rather than maintaining a private duplicate dictionary.

The class still enforces it at:

- `ForLeague`;
- block reconstruction;
- roster-sync validation.

It retains local exception/parameter semantics.

## 5.9 D3 tests

New:

`src/player-database/tests/PlayerIdRegistryTests.cs`

Required:

- empty registry;
- one player;
- multiple unique players in one club;
- duplicate numeric ID in same club;
- duplicate ID across clubs;
- negative ID if accepted structurally — range validity is separate from uniqueness;
- first-owner information is correct;
- failed register/reserve leaves state unchanged;
- valid move changes ownership exactly once;
- move from the wrong expected owner fails without mutation;
- remove from the wrong owner fails without mutation.

Existing boundary tests remain in:

- `ProgressionEngineTests`;
- `ProgressionSaveCodecTests`;
- `PlayerCareerStatesTests`.

Those must prove not only reconstruction-time rejection but lifecycle invocation:

- seed/load duplicate rejection;
- roster-sync duplicate rejection before commit;
- staged membership failure leaves roster and identity registry unchanged;
- the next-player-id/new-player path cannot commit an already-owned id;
- transfer/move semantics are locked when that production path exists.

Mutation review for D3 must include bypass mutations at each current roster-membership write site, not only mutations inside the registry dictionary algorithm.

## 5.10 D3 documentation

Primary back-prop:

Squad / Player Data #27.

Required back-propagation targets:

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

## 6.3 Formula-level test ownership and oracle provenance

Mathematical proof belongs entirely in:

`TacticalDirector.PlayerProgression.Tests`

Required focused file:

`src/player-progression/tests/AbilityModelRampTests.cs`

Also add the documentation-only oracle ledger:

`docs/tracking/player-progression-ramp-golden-vectors.md`

The ledger is the provenance source for fixed expected values. Each vector has a stable id such as `PG-RAMP-G001` and records:

- exact inputs;
- exact expected integer result;
- governing #28 spec section/equation;
- explicit arithmetic substitution sufficient to audit the literal;
- rounding/floor point where applicable;
- date/reviewer note;
- the production commit against which it was later compared.

**Production output is not allowed to generate the oracle.** The expected literal is derived from the approved specification before the production formula is edited or used as a calculator. Running current production is only a comparison step; disagreement opens a defect instead of rewriting the ledger.

Prefer vectors whose answers follow directly from structural points of the normative curve — zero width, ramp endpoints, midpoint/symmetry points, stable region, full growth/decline — plus a small number of interior rational cases whose integer floor is shown explicitly.

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
- representative fixed ledger vectors.

Test expected values are literals tagged with their ledger vector id. Tests must not call production code to compute their expected side and must not contain a copied general-purpose G(n)/D(n) implementation.

This makes a golden independently auditable rather than merely “a number observed from the current code.”

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
4. representative values reaching #30 match a small approved subset of the #28 oracle ledger.

Use representative players in:

- full Growth;
- ramp;
- Stable;
- full Decline.

Every hardcoded SeasonSave expectation must cite the corresponding `PG-RAMP-*` vector id from `docs/tracking/player-progression-ramp-golden-vectors.md`. SeasonSave does not invent or regenerate its own expected value.

The test must not calculate expectations through:

- `AbilityModel.AccruedBandPoints`;
- `DailyBandPoints`;
- copied G(n)/D(n) algebra.

Thus:

**#28 tests answer “is the mathematics correct against a spec-derived oracle?”**

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

Required:

- `src/deterministic-sim/SplitMix64.cs`
- `src/project-constants/GameplayConfigValidation.cs`
- `src/injuries-medical/InjuriesMedicalConfigValidation.cs`
- `src/training-system/TrainingSystemConfigValidation.cs`
- `src/player-database/PlayerIdRegistry.cs`

## 8.2 New test/document files

Required:

- `src/deterministic-sim/tests/SplitMix64Tests.cs`
- `src/player-database/tests/PlayerIdRegistryTests.cs`
- `src/player-progression/tests/AbilityModelRampTests.cs`
- `docs/tracking/player-progression-ramp-golden-vectors.md`

D1 also modifies or adds consumer-specific tests wherever the required pre/post vectors in §3.7 do not already exist.

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
- `src/training-system/TrainingSystemConstants.cs` to delegate key/default loading to the pure schema/resolver metadata where needed;
- the actual production composition-root file that performs gameplay boot; D2 cannot pass without this concrete call site.

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

D2 therefore accepts an explicit validator plan supplied from the production composition root rather than importing TrainingSystem or InjuriesMedical. The production root necessarily references the participating domain assemblies; that is composition-layer coupling, not a foundation reverse dependency.

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

1. record the exact pre-migration commit SHA;
2. add/freeze every consumer contract vector required by §3.7 **before** editing that consumer;
3. add canonical constants;
4. add `SplitMix64` with `Mix13` / `Step` / `Next`;
5. migrate one consumer at a time;
6. add CollisionSystem assembly reference;
7. run that consumer's pre/post vectors immediately after migration;
8. run per-assembly tests;
9. run whole-tree gate and deterministic/golden suites;
10. run final literal/helper grep.

Do not combine D1 with unrelated arithmetic fixes.

## Commit B — D2 boot validation

1. add `ValidatedGameplayConfig` + explicit validator-plan protocol;
2. remove the raw production `Bind(GameplayConfig)` escape hatch;
3. add validation-in-progress holder-read guard and atomicity tests;
4. add #29 and #41 pure candidate validators/resolvers;
5. wire the **actual production composition root** to load → validate → bind before catalogue use;
6. add malformed-config and production boot integration cases;
7. remove duplicated production config-authority guards while retaining caller/state preconditions;
8. back-prop Code Standards/#41/#29 docs.

If step 5 cannot be performed because no production root exists, Commit B does not satisfy D2 and D2 remains BLOCKED.

## Commit C — D3 identity owner

1. add #27 `PlayerIdRegistry` / `PlayerIdCollision`;
2. add registry owner tests including move/remove/no-partial-mutation cases;
3. make ProgressionEngine's authoritative roster construction/reconstruction use the registry;
4. migrate ProgressionSaveCodec validation;
5. migrate PlayerCareerStates construction and staged roster-sync validation;
6. inventory every current production roster-membership write and close bypasses;
7. remove duplicated predicates/dictionaries;
8. preserve each boundary's exception semantics;
9. back-prop #27 and consumer docs.

## Commit D — D4 oracle ownership

1. create the spec-derived `PG-RAMP-*` oracle ledger **without using production output as the source**;
2. add/strengthen #28 formula tests using ledger-tagged literals;
3. compare current production against those vectors and file/fix any disagreement rather than rebaselining;
4. delete `ExpectedRampAccrual` from SeasonSave.Tests;
5. replace SeasonSave expectations with a cited subset of the same ledger vectors;
6. simplify SeasonSave wiring proof;
7. mutation-check formula owner and wiring owner separately;
8. correct false “independent implementation” description.

Only after A–D are verified should the remaining arithmetic/mutation/governance adversarial findings be applied.

---

# 13. Acceptance gates

## D1

- one SplitMix64 implementation exists in production;
- no local Stafford Mix13 copy remains outside `deterministic-sim`;
- `Mix13` vs full-step vs stateful-next semantics are explicit;
- every migrated consumer has a pre-migration-SHA-anchored external-contract vector;
- all consumer vectors are byte/value identical after migration;
- deterministic golden vectors remain unchanged;
- whole-tree gate including MatchEngine passes.

## D2

- malformed reviewed `[GT]` configurations fail before gameplay catalogue/service construction;
- no raw production `Bind(GameplayConfig)` bypass exists;
- validation failure cannot publish or lock a candidate;
- validator access to the holder fails before lock;
- validators do not initialize catalogues before binding;
- the real production composition path explicitly load → validates → binds;
- a production-boundary integration test proves a non-default reviewed value is observed after bind;
- #41/#29 config invariants have their declared boot owners;
- runtime state/caller guards remain distinct;
- if no production call site exists, D2 is **not accepted**.

## D3

- #27 contains the sole production `PlayerId -> ClubId` ownership implementation;
- ProgressionEngine's authoritative roster construction/reconstruction uses `PlayerIdRegistry`;
- every current roster-membership write is inventoried and either routed through the registry or proven incapable of changing membership;
- PlayerCareerStates staged sync validates through #27 before commit;
- no consumer maintains an independent duplicate-detection dictionary;
- failed identity/membership mutations leave both registry and roster state unchanged;
- each boundary preserves existing failure type and diagnostic context.

## D4

- no copy of the ramp integral remains in SeasonSave.Tests;
- every formula golden has a `PG-RAMP-*` provenance entry derived from the approved spec, not observed production;
- PlayerProgression.Tests kill formula mutations;
- SeasonSave.Tests cite the shared oracle ledger and kill wiring mutations;
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

**Project Constants / boot boundary** owns validation sequencing and binding atomicity, and D2 is incomplete until a real production composition call site executes that sequence.

**Training System #29 / Injuries & Medical #41** own their configuration schemas and domain invariants.

**Squad / Player Data #27** owns the per-career identity registry and every membership operation that can create, move, or remove a career-global PlayerId ownership.

**Player Progression #28** owns progression mathematics and formula-level proofs against a spec-derived, provenance-recorded golden ledger.

**Season & Competition Loop #30** owns composition and verifies that those systems are invoked correctly.

No consumer retains a second implementation of an upstream rule.

That is the architectural target against which the subsequent adversarial-review fixes should be written.


---

# 16. Hostile-review closure record

This amendment closes the seven issues identified against the first supplement:

1. **D2 production wiring** — no future-root deferral; D2 cannot pass without a real production call site and integration test.
2. **D2 atomicity** — validation and publication are separated by `ValidatedGameplayConfig`; holder reads during validation fail before locking; CLR type-initializer poisoning is explicitly treated as fatal rather than falsely promised to roll back.
3. **D3 lifecycle ownership** — #27 now owns a per-career registry with register/reserve/move/remove semantics, and ProgressionEngine's current sole-writer roster boundary must use it.
4. **D4 oracle provenance** — fixed values come from an auditable spec-derived `PG-RAMP-*` ledger, never from current production output.
5. **D1 downstream equivalence** — every migrated consumer requires a pre-migration-SHA-anchored external-contract vector, not merely shared-helper tests.
6. **C# API naming** — Stafford Mix13 finalization is named `Mix13`, not `Finalize`.
7. **Implementation discretion** — critical seams now use required types/files/operations and explicit BLOCKED/acceptance rules rather than “recommended”, “optional”, or “future” language.
