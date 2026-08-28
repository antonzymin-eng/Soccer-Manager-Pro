# Round-2 Architecture Remediation — Design Supplement

> **Created:** August 27, 2026
> **Amended:** August 27, 2026 — second hostile-review hardening: fail-closed config bootstrap, immutable identity indexing, baseline-differential verification, and spec-owned oracle/mutation evidence
> **Status:** DESIGN SUPPLEMENT — owner decisions approved; second hostile-review objections resolved as explicit design obligations; implementation not yet landed
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

- a process cannot read any migrated `[GT]` catalogue before an explicit config boot decision;
- “use defaults” is itself an explicit boot decision, never an implicit unbound fallback;
- malformed reviewed configuration is rejected before publication;
- caller/state validity remains checked at simulation entry points;
- runtime calculations do not independently re-own boot invariants;
- tests needing a non-default real static catalogue run in an isolated test host because CLR static initialization is one-shot.

## 1.4 Tests may duplicate expected values, not algorithms

A fixed literal expected result is a useful oracle. A copied branch structure with the same algebra as production is not independent merely because it lives in another assembly.

Golden vectors and fixed expected cases are preferred over mirrored implementations.

---

# 2. Affected-assembly summary

| Decision | Owning assembly | Other affected assemblies | Assembly-reference change |
|---|---|---|---|
| D1 SplitMix64 | `TacticalDirector.DeterministicSim` | CollisionSystem, DecisionTree, HeadingMechanics, PassMechanics, SeasonSave, InjuriesMedical | CollisionSystem gains DeterministicSim; others already reference it |
| D2 config boot/validation | `TacticalDirector.GameplayBootstrap` is the sole production binder; ProjectConstants owns the fail-closed holder; domain schemas/validators remain with TrainingSystem/InjuriesMedical | current production composition roots and affected tests | new top-level GameplayBootstrap references ProjectConstants + reviewed validator owners; no foundation reverse dependency |
| D3 live PlayerId uniqueness | `TacticalDirector.PlayerDatabase` owns an immutable current-roster identity index | PlayerProgression, SeasonSave | none |
| D4 ramp oracle | approved Player Progression #28 test-plan text owns the oracle | PlayerProgression.Tests, SeasonSave.Tests | none |

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

## 3.7 D1 tests, migration-equivalence locks, and baseline proof

New owner test:

`src/deterministic-sim/tests/SplitMix64Tests.cs`

Required primitive vectors cover 0, 1, `ulong.MaxValue`, gamma, arbitrary non-pattern values, a fixed `Next(ref state)` sequence, `Step(x) == Mix13(unchecked(x + gamma))`, and exact one-step state mutation.

Primitive tests are insufficient. Each migrated consumer also needs a frozen external-contract lock:

| Consumer | Contract frozen before migration |
|---|---|
| `collision-system/DeterministicRNG` | first outputs from at least two seeds, including all-zero recovery |
| `decision-tree/ActionSelector` | exact selection/random scalar for representative fixed tuples |
| `heading-mechanics/HeadingRngServiceStub` | exact repeated stateful sequence |
| `pass-mechanics/PassErrorCalculator` | exact error-direction outputs, including finalizer-only path |
| `season-save/FixtureScheduler` | exact bounded draws or fixture permutation |
| `season-save/LeagueBootstrap` | exact seed-derived ordering/permutation |
| `season-save/RoundResolutionModel` | exact fixture/match derivations |
| `season-save/SeasonLoop.DeriveNextSeasonSeed` | exact next-season seeds |
| `injuries-medical/MedicalStep.DrawOccurrence` | exact occurrence draws |

### Mandatory A0/A1 split

**A0 — lock-only commit**

- production files unchanged;
- records exact production SHA frozen;
- adds the expected literals;
- executes the new vectors against old production.

**A1 — production migration**

- adds/migrates the shared helper;
- does not change A0 expected literals;
- may alter only test plumbing/imports required by the API move;
- executes the identical vectors after migration.

The commit boundary is the proof that expected values predate the new implementation.

### Whole-tree baseline differential

The repository already has inherited red tests on main, so D1 does not require an unrelated globally-green tree.

Before A0, run the whole-tree gate at the exact baseline SHA and record build warnings/errors, suite counts, failing test identities, and diagnostics/predicate values. After A1, run the same gate.

Acceptance means:

- no new build error/warning attributable to D1;
- every previously green test remains green;
- inherited red tests on untouched behavior surfaces retain the same identity and materially identical diagnostics;
- every D1 vector is exact;
- deterministic digests/goldens do not move.

An inherited red may remain red; it may not be silently rebaselined, disappear because execution changed, or worsen. An unrelated fix must be a separate commit.

Record A0/A1 and gate evidence in `docs/tracking/round-2-architecture-remediation-verification.md`.

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

# 4. D2 — Fail-closed, complete `[GT]` boot validation

## 4.1 Failure being corrected

The prior amendment remained bypassable: a caller-selected validator list could mint trust, an unbound holder silently returned defaults, and tests could pretend holder reset also reset already-initialized catalogue statics.

D2 removes those states.

## 4.2 Single production binder

Add:

`src/gameplay-bootstrap/`

Assembly:

`TacticalDirector.GameplayBootstrap`

Required file:

`GameplayConfigBootstrap.cs`

This assembly is the only public production path that publishes gameplay configuration.

Required API:

- `GameplayConfigBootstrap.Bind(GameplayConfig candidate)`;
- `GameplayConfigBootstrap.BindDefaults()`.

There is no public validator-list parameter and no public validation token.

`Bind(candidate)` directly hard-codes the complete reviewed plan:

1. `TrainingSystemConfigValidation.Validate(candidate)`;
2. `InjuriesMedicalConfigValidation.Validate(candidate)`;
3. ProjectConstants' internal publication seam.

`BindDefaults()` is the explicit-default path: it validates `GameplayConfig.Empty` through the same plan and then publishes it. Missing on-disk config remains behavior-neutral only because the host explicitly selected defaults.

Any new boot-owned domain validator must be added to this source-level plan in the same implementation/spec commit and increments `GameplayConfigBootstrap.ValidationPlanVersion`.

Gameplay/domain assemblies never reference GameplayBootstrap.

## 4.3 ProjectConstants becomes fail-closed

Modify `GameplayConfigHolder.cs`.

Required state:

- `GameplayConfig s_config`;
- `bool s_bound`;
- `bool s_locked`.

`Config`:

1. throws `InvalidOperationException` if unbound;
2. otherwise sets `s_locked = true`;
3. returns `s_config`.

No unbound fallback exists.

Required internal publication seam:

`internal static void PublishValidated(GameplayConfig config)`

It rejects null, a second publication, or publication after lock; otherwise stores the config and marks bound.

The old public `Bind(GameplayConfig)` is deleted.

`src/project-constants/AssemblyInfo.cs` grants only `InternalsVisibleTo("TacticalDirector.GameplayBootstrap")` for this publication seam.

Add:

`public static void RequireBound()`

It throws if unbound and otherwise has no side effect.

## 4.4 Dominance over successful gameplay paths

Boot dominance no longer depends on finding one future executable: every migrated catalogue read fails while unbound.

Current composition roots are nevertheless inventoried for immediate diagnostics. At minimum the manifest identifies:

- `MatchEngine`;
- `SeasonLoop`;
- `WorldStore`;
- `MatchSession`;
- any client/application root that directly constructs them.

Each root that can transitively reach config-backed catalogues calls `GameplayConfigHolder.RequireBound()` at boot entry unless it immediately delegates through another already-gated root.

Top-level hosts/tests call GameplayBootstrap before constructing those roots.

Code Standards #20 is amended so every new production composition root must preserve this rule.

## 4.5 One key/fallback/resolver source

Add:

- `TrainingSystemConfigSchema.cs`;
- `InjuriesMedicalConfigSchema.cs`.

Each schema is the sole source for section name, key name, fallback, and pure resolver.

For example `TrainingSystemConfigSchema.ResolveInjuryRiskMax(config)` is used by both the runtime catalogue and validation. The validator may not spell another key/fallback; the runtime catalogue may not either.

Every reviewed #41 value follows the same pattern through `InjuriesMedicalConfigSchema`.

## 4.6 Reviewed invariants

TrainingSystem validation owns #29 structural rules.

InjuriesMedical validation owns:

- `RecoveryMax >= 1`;
- `RecoveryDaysPerTickBase > 0`;
- minor/moderate each non-negative;
- `(long)minor + moderate < SEVERITY_PERMILLE_DENOM`;
- `1 <= AppearanceWindowDays <= 31`;
- non-negative age slope/span;
- unrestricted age pivot because extremes are defined;
- candidate `InjuryRiskMax`, resolved through the TrainingSystem schema, satisfies `0 < value <= OCCURRENCE_DRAW_DENOM`.

No new rule is invented for the other tuning weights without a spec/ERR change.

## 4.7 Runtime guard policy

After fail-closed boot, production hot-path calculations do not duplicate those config predicates.

They retain caller/state rules such as invalid modifier/state, negative age, invalid purpose ordinal, and day/state coherence.

Parameterized `TestOnly_*` seams keep explicit argument preconditions because they bypass catalogues by design; those are function contracts, not config ownership.

## 4.8 Static initialization and test-host policy

CLR catalogue statics are one-shot.

Therefore:

- `ResetForTests` is limited to ProjectConstants holder tests that never touch gameplay catalogues;
- no test that has read a real catalogue may reset/rebind in-process;
- ordinary test assemblies needing baseline config call `GameplayConfigBootstrap.BindDefaults()` in assembly setup before catalogue access;
- repeated `BindDefaults()` may no-op only when the published config is exactly `GameplayConfig.Empty`; it throws if a non-default config is already published;
- real non-default catalogue tests run in a dedicated test assembly/process.

Required isolated assembly:

`src/gameplay-bootstrap/tests/gameplay-bootstrap-integration-tests.asmdef`

Its ordered integration scenario:

1. malformed candidate fails and publishes nothing;
2. valid non-default candidate binds;
3. real TrainingSystem/InjuriesMedical catalogues are read for the first time;
4. exact non-default values are observed;
5. second/different bind is refused.

The gate invokes this assembly in a clean host.

## 4.9 D2 acceptance

ProjectConstants tests prove unbound read/RequireBound failure, internal publication success, second/late publication failure, and no catalogue touch in reset tests.

GameplayBootstrap tests prove:

- no public raw ProjectConstants bind exists;
- no caller validator list exists;
- defaults use the same hard-coded plan;
- malformed #29/#41 config never publishes;
- plan version/declared owners match the explicit implementation calls;
- isolated real-catalogue non-default scenario passes.

Production-root tests prove unbound construction fails and explicit default boot restores prior behavior.

D2 passes only when implicit fallback is gone, the plan cannot be shortened by a caller, schema metadata is single-source, all current config-reaching roots are inventoried/gated, and the isolated real-catalogue test passes.

---

# 5. D3 — Canonical live PlayerId uniqueness without stealing roster ownership

## 5.1 Two distinct identity invariants

Do not conflate:

1. **live current-roster uniqueness**: one numeric `PlayerId` has at most one current club owner across carried rosters — #27 owns this predicate;
2. **historical generated-id non-reuse**: a departed/retired generated id is never allocated again — #28 owns this through serialized monotonic `NextPlayerId` (FR-PG-011).

D3 centralizes rule 1. It does not move rule 2.

## 5.2 Immutable #27 identity index

New:

`src/player-database/PlayerIdentityIndex.cs`

Required collision value:

`PlayerIdentityCollision { PlayerId, ExistingClubId, IncomingClubId }`

`PlayerIdentityIndex` is built from the complete proposed current set of `(clubId, playerId)` pairs.

It:

- rejects same-club and cross-club duplicates;
- retains first-owner diagnostic information;
- optionally exposes read-only owner lookup;
- exposes no production `Add`, `Move`, `Remove`, `Reserve`, or other mutator.

Build is pure: either a complete valid index is returned or nothing changes.

Roster membership is cold-path; full rebuild is deliberately preferred to a mutable registry.

## 5.3 Preserve existing membership ownership

Roles remain:

- `ProgressionEngine` is the persisted #28 roster representation when wired;
- #30 decides when season-boundary changes are applied and orchestrates the transaction;
- future #31/#42 produce transfer/intake decisions rather than directly writing #28 storage;
- #27 validates the complete proposed roster.

PlayerDatabase is not made a roster writer.

For an external `ISquadProvider`, that provider's owner remains its mutation owner; `PlayerCareerStates.PrepareRosterSync` validates the resulting complete roster through #27 before companion-state commit.

## 5.4 Staged whole-roster mutation

Add to `ProgressionEngine`:

`PrepareRosterMutation(...) -> ProgressionRosterMutationPlan`

Prepare:

1. validates proposed removals/additions/moves and existing lifecycle rules;
2. builds complete proposed per-club record/lifecycle arrays without touching live state;
3. computes proposed `nextPlayerId`;
4. applies #28's cursor-ahead-of-all-carried rule;
5. builds a new immutable `PlayerIdentityIndex` from the proposed whole roster;
6. returns replacement arrays/index/cursor plus current roster generation.

`CommitRosterMutation(in plan)`:

1. checks generation before any mutation;
2. then performs only pre-sized field/reference assignments with no remaining validation branch;
3. increments generation.

No identity object is mutated before roster commit, so there is no two-object rollback problem.

The current identity index is derived state: rebuild at `SeedFrom`/`FromBlocks`, replace with roster arrays on commit, never serialize it.

## 5.5 Existing boundary migration

### ProgressionEngine

Remove local duplicate dictionary walk. `SeedFrom` and `FromBlocks` build the #27 index and adapt exception wording only.

### ProgressionSaveCodec

Encode/decode duplicate checks delegate to the #27 pure index builder and retain asymmetric argument-vs-corrupt-state exception semantics.

### PlayerCareerStates

`ForLeague`, `FromBlocks`, and `PrepareRosterSync` validate the complete proposed current roster through #27 before state-array commit.

This is companion-state validation, not roster/identity ownership.

## 5.6 Future membership writers

For a #28-backed career:

1. producer supplies semantic change;
2. `ProgressionEngine.PrepareRosterMutation` stages complete next roster/index;
3. #30 performs remaining boundary validation;
4. #30 installs the prepared roster at the transaction's defined commit point;
5. `PlayerCareerStates` commits its already-prepared companion-state sync in the established validate-all-before-write order.

No throwing validation may be introduced after the first irreversible assignment.

Future regen/transfer/intake/import/clone paths may not directly mutate career membership around this staged owner.

## 5.7 D3 tests

New:

`src/player-database/tests/PlayerIdentityIndexTests.cs`

Cover empty/one/unique/same-club duplicate/cross-club duplicate/first-owner/source-unchanged-on-failure/read-only lookup.

Progression tests prove:

- seed/load duplicate rejection through #27;
- failed prepare leaves roster, lifecycle, index, cursor, generation unchanged;
- stale plan fails before writes;
- successful commit installs roster/index together;
- `NextPlayerId` still independently prevents historical reuse.

SeasonSave/PlayerCareerStates tests prove:

- duplicate external roster fails during prepare before companion writes;
- a transfer-like move represented in the complete proposed roster has one owner and validates;
- duplicate ids fail even when each club's local arrays are valid.

Completion includes a repository inventory of every production career-membership write, classified as staged #28 mutation, external-provider owner validated before sync, or match-local lineup/substitution state that does not alter career membership.

## 5.8 Documentation

Back-propagate #27: current-roster global uniqueness and `PlayerIdentityIndex`.

Back-propagate #28: `NextPlayerId` remains historical non-reuse owner; persisted roster exposes staged mutation.

Back-propagate #30: boundary orchestration and exact ordering between prepared #28 roster mutation and prepared `PlayerCareerStates` sync.

Historical ERR entries remain historical.

---

# 6. D4 — Spec-owned progression oracle and executable mutation evidence

## 6.1 Problem

Deleting `ExpectedRampAccrual` and replacing it with unexplained literals would convert duplicated code into magic numbers.

D4 requires an authoritative source for every literal and executed evidence that tests kill formula/wiring mutations.

## 6.2 Production owner

Production mathematics remains in `AbilityModel.cs`. No formula moves to SeasonSave and no public formula API is added for tests.

## 6.3 Oracle authority is approved #28

Do not use a tracking document as oracle authority.

Add a normative **Ramp Oracle Vectors** table to:

`docs/specs/player-progression-lifecycle/section-5.md`

Each stable `PG-RAMP-*` vector records exact inputs/result, governing §3.1.3 equation/branch, explicit arithmetic substitution, integer floor/division point, and isolated property.

Required vector classes:

- zero-width identity;
- growth start/interior/mid/end;
- stable interior;
- decline start/interior/mid/end;
- one-day continuity around both edges;
- full-integral/P5;
- disjoint-ramp rejection;
- overflow-guard boundary;
- construction-day credit;
- age ceiling.

Expected values are derived from approved spec arithmetic before consulting production output. Production is comparison only. Disagreement opens an explicit spec/code defect; it never regenerates the oracle.

Because vectors live in approved #28, normal spec version/approval rules govern expected-value changes.

## 6.4 Formula and integration tests

Required:

`src/player-progression/tests/AbilityModelRampTests.cs`

Every expected literal cites its `PG-RAMP-*` id.

Expected values may not be computed through production methods or a copied general G(n)/D(n) implementation.

`SeasonLoopProgressionTests` deletes `ExpectedRampAccrual` and formula-local algebra. SeasonSave uses a small subset of the same spec-owned vectors across Growth/ramp/Stable/Decline to test slot-1 invocation/persistence.

#28 says what is correct; PlayerProgression.Tests verify math; SeasonSave.Tests verify composition.

## 6.5 Required mutation protocol

Prose claims are insufficient. Execute:

| ID | Temporary production mutation | Required kill |
|---|---|---|
| PG-MUT-01 | force ramp half-width zero | ramp-vector test fails |
| PG-MUT-02 | shift growth edge one day | edge/continuity test fails |
| PG-MUT-03 | invert growth sign | growth test fails |
| PG-MUT-04 | invert/remove decline phase | decline test fails |
| PG-MUT-05 | alter midpoint comparison | midpoint/boundary test fails |
| PG-MUT-06 | remove required widening | overflow-boundary test fails |
| PG-MUT-07 | bypass shared construction-day credit | construction-credit test fails |
| PG-WIRE-01 | remove slot 1 | wiring test fails |
| PG-WIRE-02 | call slot 1 twice | wiring test fails |
| PG-WIRE-03 | supply wrong world day | wiring test fails |
| PG-WIRE-04 | supply wrong player/store | wiring test fails |
| PG-WIRE-05 | do not carry/persist result | save/resume test fails |

For each mutation:

1. start from final unmutated baseline;
2. apply exactly one temporary source edit;
3. run the narrow owning suite;
4. record mutation id, exact edit, command, and failing test ids;
5. revert completely;
6. re-run to baseline.

Evidence lives in `docs/tracking/round-2-architecture-remediation-verification.md`; it is evidence, not mathematical authority.

## 6.6 D4 acceptance

- no ramp copy remains in SeasonSave.Tests;
- every literal maps to approved/versioned `PG-RAMP-*`;
- production output was not used to author oracle values;
- every PG-MUT/PG-WIRE probe has recorded kill evidence;
- final unmutated suites return to baseline.

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

## 8.1 New production

- `src/deterministic-sim/SplitMix64.cs`;
- `src/gameplay-bootstrap/gameplay-bootstrap.asmdef`;
- `src/gameplay-bootstrap/GameplayConfigBootstrap.cs`;
- `src/training-system/TrainingSystemConfigSchema.cs`;
- `src/training-system/TrainingSystemConfigValidation.cs`;
- `src/injuries-medical/InjuriesMedicalConfigSchema.cs`;
- `src/injuries-medical/InjuriesMedicalConfigValidation.cs`;
- `src/player-database/PlayerIdentityIndex.cs`.

## 8.2 New tests/evidence

- `src/deterministic-sim/tests/SplitMix64Tests.cs`;
- consumer-specific D1 locks from §3.7;
- `src/gameplay-bootstrap/tests/gameplay-bootstrap-integration-tests.asmdef`;
- `src/gameplay-bootstrap/tests/GameplayConfigBootstrapIntegrationTests.cs`;
- `src/player-database/tests/PlayerIdentityIndexTests.cs`;
- `src/player-progression/tests/AbilityModelRampTests.cs`;
- `docs/tracking/round-2-architecture-remediation-verification.md`.

## 8.3 Existing production modifications

### D1

`DeterministicSimConstants.cs`, the listed SplitMix consumers, CollisionSystem asmdef, and any additional local Mix13 copy found by final sweep.

### D2

`GameplayConfigHolder.cs`, `project-constants/AssemblyInfo.cs`, TrainingSystem/InjuriesMedical runtime catalogues and MedicalStep, current config-reaching composition roots (`MatchEngine`, `SeasonLoop`, `WorldStore`, `MatchSession`, direct client roots), and every affected test-assembly setup that currently relies on implicit fallback.

### D3

`PlayerRecord.cs`, `ProgressionEngine.cs`, `ProgressionSaveCodec.cs`, `PlayerCareerStates.cs`, and `SeasonLoop.cs` when staged boundary mutation is wired.

### D4

`SeasonLoopProgressionTests.cs`, approved #28 `section-5.md`, and source docs/version history as needed.

Exact D2 test-setup inventory is discovered and recorded during implementation rather than asserted from memory.

# 9. Assembly-definition impact

## 9.1 D1

CollisionSystem adds DeterministicSim; existing consumers retain direction.

## 9.2 D2

New `TacticalDirector.GameplayBootstrap` is top-level composition-only and references:

- ProjectConstants;
- TrainingSystem;
- InjuriesMedical.

Domain assemblies never reference GameplayBootstrap.

ProjectConstants remains a foundation; it grants only `InternalsVisibleTo("TacticalDirector.GameplayBootstrap")` for the publish seam.

Production roots may depend on ProjectConstants for `RequireBound()`; top-level hosts/tests may reference GameplayBootstrap to perform publication.

The new assembly must be added to the authoritative assembly map/taxonomy tracking rather than left unclassified.

## 9.3 D3/D4

PlayerProgression and SeasonSave already reference PlayerDatabase. D4 adds no production reference.

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

Back-prop #16 with canonical `Mix13`/`Step`/`Next`, local-copy prohibition, and lock-before-production migration evidence.

## D2

Back-prop Code Standards #20/coding reference:

- unbound Config is illegal;
- defaults require explicit GameplayBootstrap;
- GameplayBootstrap is sole production binder;
- schema owns key/fallback/resolver once;
- each new domain validator joins the hard-coded plan and bumps plan version;
- new composition roots preserve `RequireBound` dominance;
- tests cannot reset/rebind after real static catalogue initialization.

Correct existing docs that promise implicit `GameplayConfig.Empty` before bind.

## D3

Back-prop #27/#28/#30 together: #27 live uniqueness, #28 historical non-reuse and staged roster storage, #30 transaction orchestration.

## D4

Put normative oracle vectors in approved #28 §5. Correct #30 language about the old “independent” copied oracle. Keep mutation evidence in tracking only.

Historical text is corrected through append-only entries where policy requires.

# 12. Implementation sequence

## A0 — D1 locks only

Record exact SHA and whole-tree baseline; add/freeze consumer vectors without production edits; execute targeted suites; commit.

## A1 — D1 migration

Add helper/constants; migrate one consumer at a time; never alter A0 expected literals; run consumer locks and post whole-tree differential; final literal/helper sweep.

## B — D2 fail-closed bootstrap

Add GameplayBootstrap; add shared schemas/validators; make catalogues use schema resolvers; remove public raw bind; fail unbound reads; add friend-only publication; hard-code plan; inventory/gate current roots; update test boot setup; add isolated non-default integration assembly; back-prop governance.

## C — D3 immutable identity index

Add #27 index/tests; migrate duplicate predicates; add #28 staged whole-roster prepare/commit; retain/test `NextPlayerId`; inventory membership writers; back-prop #27/#28/#30.

## D — D4 spec oracle/mutation proof

Author/approve `PG-RAMP-*` in #28 §5 without production as calculator; add formula tests; resolve disagreements explicitly; delete copied SeasonSave oracle; add wiring cases; execute every mutation probe; restore baseline and rerun.

Only after A0/A1/B/C/D pass may remaining arithmetic/mutation/governance findings be applied.

# 13. Acceptance gates

## D1

- one production SplitMix64/Mix13 implementation;
- A0 lock-only commit precedes A1;
- A1 leaves A0 literals unchanged;
- exact consumer vectors;
- no new/worsened failure against recorded whole-tree baseline;
- inherited unrelated red may remain only under differential rule;
- deterministic digests/goldens unchanged.

## D2

- unbound Config cannot return defaults;
- no public raw ProjectConstants bind;
- GameplayBootstrap is sole public binder;
- caller cannot choose/omit/reorder validators;
- validator/runtime catalogue share schema resolvers;
- all current config-reaching roots inventoried/gated;
- isolated real-catalogue non-default test passes;
- malformed config never publishes;
- no reset/rebind after real catalogue initialization;
- bootstrap assembly classified/documented.

## D3

- #27 is sole live current-roster duplicate predicate/index;
- immutable index built from complete proposed roster;
- no identity mutation before roster commit;
- failed prepare leaves all live state unchanged;
- #28 `NextPlayerId` separately proves historical non-reuse;
- #30 remains boundary orchestrator;
- companion state validates before commit;
- every current membership writer inventoried/classified.

## D4

- approved #28 §5 owns all oracle values;
- no tracking/production output is oracle authority;
- no copied ramp implementation remains in SeasonSave.Tests;
- all required mutation probes record failing test ids;
- mutations reverted and final suites return to baseline.

## Cross-cutting

No circular reference, no behavior-neutral save/determinism version bump, no unexecuted verification claims, required spec back-props land with implementation, and the verification record distinguishes formal gate status from baseline differential.

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

**Deterministic Simulation #16** owns SplitMix64/Mix13 mathematics. Migration proof is a lock-only predecessor commit plus baseline differential.

**GameplayBootstrap** is the sole production config binder. **ProjectConstants** is fail-closed until an explicit bind/default-bind. Domain schemas own key/fallback/resolution once; validator and runtime catalogue consume the same resolver.

**Squad / Player Data #27** owns immutable uniqueness among currently carried players.

**Player Progression #28** owns historical generated-id non-reuse through serialized `NextPlayerId` and the persisted roster representation when wired, including staged replacement.

**Season & Competition Loop #30** owns boundary orchestration and decides when prepared roster/companion changes install.

**Approved Player Progression #28 test-plan text** owns ramp oracle values. PlayerProgression tests verify math; SeasonSave tests verify wiring; mutation evidence proves the tests are load-bearing.

No consumer keeps a second upstream rule implementation, and no atomicity claim depends on mutating two independent objects before validation is complete.

# 16. Second hostile-review resolution map

This records design obligations, not a claim that implementation has already passed them.

1. **Forgeable D2 validator token/list** — removed; no token/list exists and GameplayBootstrap calls the complete plan directly.
2. **Implicit unbound defaults** — removed; unbound Config throws and defaults require explicit `BindDefaults()`.
3. **No dominating boot path** — successful migrated catalogue access now structurally requires prior publication; current roots are explicitly inventoried/gated.
4. **Static test contamination** — reset/rebind after real catalogue initialization is forbidden; non-default real-catalogue proof runs in a clean process.
5. **D3 impossible atomic API** — removed; no mutable registry exists and the complete next identity index is built during roster staging.
6. **Two identity authorities** — separated into distinct rules: #27 current-live uniqueness and #28 historical generated-id non-reuse.
7. **D3 ownership conflict** — corrected: #27 validates, #28 stores/stages its wired roster, #30 orchestrates commit.
8. **Impossible globally-green D1 gate** — replaced by executed pre/post whole-tree baseline differential.
9. **Weak D1 provenance** — replaced by mandatory A0 lock-only commit before A1 production migration.
10. **Duplicated D2 key/default metadata** — removed by shared domain schema resolvers.
11. **D4 oracle in non-authoritative tracking** — removed; oracle vectors live in approved #28 §5.
12. **Unverifiable mutation claim** — replaced by an explicit mutation matrix with commands/failing-test evidence.
13. **Duplicate D3 heading** — removed.

Implementation remains incomplete until the acceptance gates are executed and recorded.
