# Round-2 Architecture Remediation — Design Supplement

> **Created:** August 27, 2026
> **Amended:** August 27, 2026 — validated self-audit hardening: reviewed-domain config scope, concrete Unity bootstrap, proposed-roster transaction projection, and exact verification semantics
> **Status:** DESIGN SUPPLEMENT — owner decisions approved; hostile-review conclusions revalidated against the repository; remaining design blockers closed; implementation not yet landed
> **Scope:** Architectural decisions D1–D4 from the round-2 adversarial review of Player Progression & Lifecycle #28 and Injuries & Medical #41, including the shared deterministic/configuration/identity/testing infrastructure they expose.
> **Governing decisions:** **D1** centralize SplitMix64 in `deterministic-sim`; **D2** make config publication fail-closed and prevalidate the reviewed #29/#41 `[GT]` invariants through `GameplayConfigBootstrap`; **D3** make `player-database` the canonical owner of live career-wide `PlayerId` uniqueness without taking roster-write ownership; **D4** keep progression mathematics and its oracle in approved Player Progression #28, with `season-save` testing wiring only.
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
- the **reviewed #29/#41 structural invariants** are rejected before config publication;
- D2 does **not** claim that every other config-backed catalogue has acquired a domain validator: an unreviewed domain may still reject a malformed typed value when that catalogue first resolves it;
- caller/state validity remains checked at simulation entry points;
- runtime #29/#41 calculations do not independently re-own their boot invariants;
- tests needing a non-default real static catalogue run in an isolated test host because CLR static initialization is one-shot.

The distinction is deliberate: **fail-closed publication is repository-wide; pre-publication structural validation is reviewed-domain scoped until additional domain validators are designed and added.**

## 1.4 Tests may duplicate expected values, not algorithms

A fixed literal expected result is a useful oracle. A copied branch structure with the same algebra as production is not independent merely because it lives in another assembly.

Golden vectors and fixed expected cases are preferred over mirrored implementations.

---

# 2. Affected-assembly summary

| Decision | Owning assembly | Other affected assemblies | Assembly-reference change |
|---|---|---|---|
| D1 SplitMix64 | `TacticalDirector.DeterministicSim` | CollisionSystem, DecisionTree, HeadingMechanics, PassMechanics, SeasonSave, InjuriesMedical | CollisionSystem gains DeterministicSim; others already reference it |
| D2 config boot/reviewed-domain validation | `TacticalDirector.GameplayBootstrap` is the sole production binder; ProjectConstants owns the fail-closed holder; #29/#41 schemas/validators remain with TrainingSystem/InjuriesMedical | current production composition roots and affected tests | new top-level GameplayBootstrap references ProjectConstants + reviewed validator owners; no foundation reverse dependency |
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
- inherited red tests on untouched behavior surfaces retain the same failure identity and **exact deterministic diagnostics/predicate values** recorded at baseline; runner duration, timestamps, host load, and other explicitly non-deterministic test-runner metadata are excluded;
- every D1 vector is exact;
- deterministic digests/goldens do not move.

An inherited red may remain red; it may not be silently rebaselined, disappear because execution changed, or change any deterministic diagnostic value. If a particular diagnostic is legitimately non-deterministic, the verification record must name that field and its comparison rule before A1. An unrelated fix must be a separate commit.

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

# 4. D2 — Fail-closed config boot with reviewed-domain `[GT]` validation

## 4.1 Exact scope

D2 has two different guarantees and must not blur them:

1. **repository-wide boot guarantee** — no migrated config-backed catalogue may read an implicitly unbound default; config publication is explicit and fail-closed;
2. **reviewed-domain validation guarantee** — the #29 Training System and #41 Injuries & Medical structural invariants identified by this adversarial review are validated before publication.

D2 does **not** certify every existing `GameplayConfig` key in the repository. Other domains such as Ball Physics and Match Engine already read the same config and may still fail at their first typed catalogue read if a configured value is malformed. Their structural invariant ownership remains unchanged until their own validation design is approved.

Accordingly, this design never calls the published object “fully validated configuration.”

## 4.2 Single production binder

Add:

`src/gameplay-bootstrap/`

Assembly:

`TacticalDirector.GameplayBootstrap`

Required file:

`GameplayConfigBootstrap.cs`

This assembly is the sole **production** publisher of `GameplayConfig`.

Required API:

- `GameplayConfigBootstrap.Bind(GameplayConfig candidate)`;
- `GameplayConfigBootstrap.BindDefaults()`.

There is no caller-provided validator list and no public validation token.

`Bind(candidate)` directly executes the complete **reviewed-domain** plan:

1. `TrainingSystemConfigValidation.Validate(candidate)`;
2. `InjuriesMedicalConfigValidation.Validate(candidate)`;
3. ProjectConstants' internal publication seam.

The plan version is named:

`GameplayConfigBootstrap.ReviewedValidationPlanVersion`

Any additional domain validator joins this explicit source-level plan only through a governing spec/design change and increments that version. Adding a validator broadens the plan's documented scope; it does not retroactively make earlier plan versions globally complete.

`BindDefaults()` executes the same #29/#41 validation over the singleton `GameplayConfig.Empty` and publishes it.

**Binding is strictly one-shot.** A second `Bind` or `BindDefaults` call throws even when the same object or the empty singleton is supplied. No semantic-equality or reference-equality exception exists. Tests that need baseline boot bind once at assembly/process setup.

Gameplay/domain assemblies never reference GameplayBootstrap.

## 4.3 ProjectConstants becomes fail-closed

Modify:

`src/project-constants/GameplayConfigHolder.cs`

Required state:

- `GameplayConfig s_config`;
- `bool s_bound`;
- `bool s_locked`.

`Config`:

1. throws `InvalidOperationException` if `s_bound == false`;
2. otherwise sets `s_locked = true`;
3. returns `s_config`.

No unbound fallback exists.

Required publication seam:

`internal static void PublishBootConfig(GameplayConfig config)`

It rejects null, a second publication, or publication after lock; otherwise stores the candidate and marks bound.

The name deliberately avoids `PublishValidated`: ProjectConstants cannot truthfully assert that every downstream domain invariant was checked.

The old public `GameplayConfigHolder.Bind(GameplayConfig)` is deleted.

Add:

`public static void RequireBound()`

It throws if unbound and otherwise has no side effect.

### Friend-assembly reality

`src/project-constants/AssemblyInfo.cs` already grants `InternalsVisibleTo("TacticalDirector.ProjectConstants.Tests")`. Add:

`InternalsVisibleTo("TacticalDirector.GameplayBootstrap")`

Therefore GameplayBootstrap is the sole **production** assembly that may call `PublishBootConfig`; ProjectConstants.Tests can also access internals by design. Do not claim the friend grant is exclusive.

`ResetForTests` remains available only to ProjectConstants.Tests and is never used after any real gameplay catalogue has initialized.

## 4.4 Concrete production bootstrap path

The current Stage-0 Unity production host is:

`src/match-client-unity/MatchClientBehaviour.cs`

Its `Awake()` ultimately executes:

`MatchClientBehaviour.BuildScene()`
→ `new MatchSession(...)`
→ `new MatchEngine(...)`.

D2 therefore modifies `MatchClientBehaviour.Awake()` to make the boot decision **before `ValidateWiring()` and before any gameplay catalogue can be referenced**.

Current Stage-0 sequence:

1. `GameplayConfigBootstrap.BindDefaults()`;
2. `ValidateWiring()`;
3. if wiring accepted, `BuildScene()`;
4. construction proceeds through MatchSession/MatchEngine.

When the production on-disk gameplay-config source is wired later, step 1 becomes:

1. load/parse candidate;
2. `GameplayConfigBootstrap.Bind(candidate)`;
3. then `ValidateWiring()`.

This supplement does not invent that future file-location/IO policy.

Assembly impact:

`src/match-client-unity/match-client-unity.asmdef` gains `TacticalDirector.GameplayBootstrap`.

The inner roots remain defensive boundaries. At minimum:

- `MatchEngine`;
- `SeasonLoop`;
- `WorldStore`;
- `MatchSession`

call `GameplayConfigHolder.RequireBound()` at boot entry unless they immediately delegate to an already-gated root before touching any catalogue.

Thus:

- the Unity application root owns publication;
- inner composition roots fail loud when instantiated directly by another host/test without prior boot;
- a future production host must bind before entering the same root graph.

Code Standards #20 records this rule for any new application/composition root.

## 4.5 One key/fallback/resolver source for reviewed domains

Add:

- `src/training-system/TrainingSystemConfigSchema.cs`;
- `src/injuries-medical/InjuriesMedicalConfigSchema.cs`.

For each #29/#41 value brought under D2, its domain schema is the sole source for:

- section name;
- key name;
- fallback value;
- pure candidate resolver.

For example:

`TrainingSystemConfigSchema.ResolveInjuryRiskMax(GameplayConfig candidate)`

is used by:

- `TrainingSystemConstants.InjuryRiskMax` after boot;
- `TrainingSystemConfigValidation.Validate`;
- #41's cross-domain compatibility check.

Neither validator nor runtime catalogue may spell a second copy of the reviewed key/fallback.

This single-source requirement is scoped to the reviewed #29/#41 surface. D2 does not silently redesign unrelated catalogues.

## 4.6 Reviewed invariants

`TrainingSystemConfigValidation.Validate` owns #29's reviewed structural rules.

`InjuriesMedicalConfigValidation.Validate` owns:

### Recovery

- `RecoveryMax >= 1`;
- `RecoveryDaysPerTickBase > 0`.

### Severity

- `SeverityMinorPermille >= 0`;
- `SeverityModeratePermille >= 0`;
- `(long)minor + moderate < SEVERITY_PERMILLE_DENOM`.

### Appearance

- `1 <= AppearanceWindowDays <= 31`.

### Age term

- `AgeRiskPerYearFromPivot >= 0`;
- `AgeRiskSpan >= 0`;
- `AgeRiskPivotYears` remains unrestricted because extreme values are defined.

### Cross-owned ceiling

The candidate `InjuryRiskMax`, resolved through `TrainingSystemConfigSchema`, must satisfy:

`0 < InjuryRiskMax <= OCCURRENCE_DRAW_DENOM`.

No new rule is invented for the remaining tuning weights without a governing spec/ERR change.

## 4.7 Runtime guard policy

After D2, #29/#41 production hot-path calculations do not duplicate the reviewed config predicates.

They retain caller/state rules such as invalid modifier/state, negative age, invalid purpose ordinal, and day/state coherence.

Parameterized `TestOnly_*` seams retain explicit argument preconditions because they bypass catalogues by design; those checks are function contracts.

No rule here removes existing guards from an **unreviewed** domain.

## 4.8 Static initialization and test-host policy

CLR catalogue statics are one-shot.

Therefore:

- `ResetForTests` is limited to ProjectConstants holder tests that never initialize gameplay catalogues;
- no test that has read a real gameplay catalogue may reset/rebind config in-process;
- a test project needing config-backed catalogues performs exactly one boot call in its assembly/process setup;
- low-level test asmdefs may reference GameplayBootstrap **for test composition only**; this does not add a production dependency to their owning gameplay asmdef;
- a second bind in the same process is always an error.

Required isolated assembly:

`src/gameplay-bootstrap/tests/gameplay-bootstrap-integration-tests.asmdef`

Required assembly name:

`TacticalDirector.GameplayBootstrap.Integration.Tests`

The name ends in `.Tests` so `tools/dotnet-ci/generate_projects.py` emits a test project.

Its ordered integration scenario:

1. malformed reviewed candidate fails and publishes nothing;
2. valid non-default #29/#41 candidate binds;
3. real TrainingSystem/InjuriesMedical catalogues are read for the first time;
4. exact non-default values are observed;
5. second bind is refused.

The D2 verification runs this test project in its **own `dotnet test` invocation**, not merely as an assumption about solution-wide scheduling:

`dotnet test src/gameplay-bootstrap/tests/gameplay-bootstrap-integration-tests.gen.csproj --no-build`

If the same proof is executed through Unity EditMode, it must likewise use a separate Unity batch-mode test invocation so no prior catalogue initialization shares the process.

## 4.9 D2 acceptance

ProjectConstants tests prove:

- unbound `Config` and `RequireBound` throw;
- internal `PublishBootConfig` then read succeeds;
- second publication fails;
- late publication after first read fails;
- reset tests never initialize gameplay catalogues.

GameplayBootstrap tests prove:

- no public raw ProjectConstants bind exists;
- no caller validator list exists;
- the reviewed plan has exactly the declared #29/#41 owners for its current version;
- defaults execute that same reviewed plan;
- malformed reviewed #29/#41 config never publishes;
- isolated real-catalogue non-default scenario passes;
- binding is strictly one-shot.

Production integration proves:

- `MatchClientBehaviour.Awake()` binds before `ValidateWiring()`;
- direct unbound construction of guarded inner roots fails;
- explicit default boot restores prior Stage-0 behavior.

D2 passes only when the fail-closed holder is global, the pre-publication validation claims are accurately scoped to reviewed domains, the concrete Unity application root owns publication, current inner roots are guarded, reviewed schema metadata is single-source, and the isolated real-catalogue test passes.

---

# 5. D3 — Canonical live PlayerId uniqueness without stealing roster ownership

## 5.1 Two distinct identity invariants

Do not conflate:

1. **live current-roster uniqueness** — one numeric `PlayerId` has at most one current club owner across carried rosters; #27 owns this predicate;
2. **historical generated-id non-reuse** — a departed/retired generated id is never allocated again; #28 owns this through serialized monotonic `NextPlayerId` (FR-PG-011).

D3 centralizes rule 1. It does not move rule 2.

## 5.2 Immutable #27 identity index

New:

`src/player-database/PlayerIdentityIndex.cs`

Required collision value:

`PlayerIdentityCollision { PlayerId, ExistingClubId, IncomingClubId }`

`PlayerIdentityIndex` is built from the complete proposed current set of `(clubId, playerId)` pairs.

It:

- rejects same-club and cross-club duplicates;
- retains first-owner diagnostics;
- may expose read-only owner lookup;
- exposes no production `Add`, `Move`, `Remove`, `Reserve`, or other mutator.

Build is pure: either a complete valid index is returned or no live state changes.

Roster membership is cold-path; complete rebuild is preferred to a mutable identity registry.

## 5.3 Preserve existing membership ownership

Roles remain:

- `ProgressionEngine` is the persisted #28 roster representation when #28 is wired;
- #30 decides when season-boundary changes are applied and orchestrates the transaction;
- future #31/#42 produce transfer/intake decisions rather than directly writing #28 storage;
- #27 validates the complete proposed roster.

PlayerDatabase is not made a roster writer.

For an external `ISquadProvider`, that provider's owner remains its membership owner; `PlayerCareerStates.PrepareRosterSync` validates the complete provider result through #27 before companion-state commit.

## 5.4 Staged whole-roster mutation

Add to `ProgressionEngine`:

`PrepareRosterMutation(...) -> ProgressionRosterMutationPlan`

Prepare:

1. validates proposed removals/additions/moves and all applicable #28 lifecycle rules;
2. builds complete proposed per-club record/lifecycle arrays without touching live state;
3. computes proposed `nextPlayerId`;
4. applies #28's cursor-ahead-of-all-carried rule;
5. builds a new immutable `PlayerIdentityIndex` from the complete proposed roster;
6. returns replacement arrays/index/cursor plus the current roster generation.

The plan is immutable derived staging state and is never serialized.

Required read surface on the plan:

- `Squad SquadFor(int clubId)`;
- read-only club enumeration/count needed by the SeasonSave adapter.

`SquadFor` projects from the **planned replacement records**, not from the live `ProgressionEngine`. It follows the same snapshot-copy posture as `ProgressionEngine.SquadFor`.

This is possible without a MatchEngine dependency because `Squad` belongs to PlayerDatabase, which PlayerProgression already references.

## 5.5 Proposed-roster provider at the composition layer

New:

`src/season-save/PlannedProgressionSquads.cs`

Type:

`internal sealed class PlannedProgressionSquads : ISquadProvider`

It wraps one `ProgressionRosterMutationPlan` and implements:

`ResolveByClubId(clubId) => plan.SquadFor(clubId)`

It contains no mutation logic and no duplicate predicate.

This adapter is the missing composition seam: #28 can expose its proposed roster without referencing MatchEngine's `ISquadProvider`, while SeasonSave can present that proposed roster to #29/#41 companion-state staging.

## 5.6 Existing boundary migration

### ProgressionEngine

Remove the local duplicate dictionary walk. `SeedFrom` and `FromBlocks` build #27's index and adapt boundary exception wording only.

The live engine holds its current derived `PlayerIdentityIndex`. On a successful roster commit, replacement arrays, cursor, and identity index are installed together.

### ProgressionSaveCodec

Encode/decode duplicate checks delegate to #27's pure index builder and preserve argument-vs-corrupt-state exception semantics.

### PlayerCareerStates

`ForLeague`, `FromBlocks`, and `PrepareRosterSync` validate the complete roster presented through their provider using #27.

No special “progression-plan” duplicate logic is added here.

## 5.7 SeasonLoop transaction ordering

The current `SeasonLoop.RollToNextSeason()` already follows a validate-all-before-write discipline. D3 preserves it.

For a #28-backed career whose season boundary proposes roster churn:

### Prepare phase — every step may fail, no live state changes

1. derive next season seed/fixtures/calendar;
2. `ProgressionRosterMutationPlan progressionPlan = _progression.PrepareRosterMutation(...)`;
3. construct `PlannedProgressionSquads plannedSquads = new PlannedProgressionSquads(progressionPlan)`;
4. if companion state exists, `RosterSyncPlan rosterSync = _career.PrepareRosterSync(plannedSquads)`;
5. complete all remaining season-boundary validation.

The critical rule is step 4: companion state is prepared against the **same proposed post-churn roster** stored in `progressionPlan`, not against the old live `ProgressionSquads` projection.

If any prepare step throws:

- SeasonState is unchanged;
- ProgressionEngine is unchanged;
- PlayerCareerStates is unchanged.

### Commit phase — established single-threaded no-intervening-mutation contract

After every throwing preparation succeeds:

1. `_state.BeginNextSeason(...)`;
2. `_state.SetBoard(...)`;
3. `_progression.CommitRosterMutation(in progressionPlan)`;
4. if companion state exists, `_career.CommitRosterSync(in rosterSync)`.

Both roster commits retain defensive stale-generation checks, matching the existing `PlayerCareerStates.CommitRosterSync` posture.

Within `RollToNextSeason`, those guards are **logically non-failing** because:

- SeasonLoop is single-threaded/not thread-safe;
- both plans were prepared synchronously in the same call;
- nothing between prepare and commit mutates ProgressionEngine's roster generation or PlayerCareerStates' roster generation;
- `BeginNextSeason` and `SetBoard` do not touch either generation.

A stale-plan exception therefore indicates programmer misuse or forbidden concurrent mutation, not an expected domain failure. The code comments and tests must state this exact precondition; the design does not falsely claim the methods are incapable of throwing under arbitrary external misuse.

After step 3, the live `ProgressionSquads` projection now matches the proposed roster against which `rosterSync` was prepared; step 4 installs the companion arrays for that same membership set.

For a boundary with no #28 roster change, existing provider/sync behavior remains unchanged.

## 5.8 Future membership writers

For any #28-backed regen, transfer, youth intake, import, or clone:

1. producer supplies semantic change;
2. #28 stages the complete next roster/index;
3. composition exposes that plan through a planned-roster provider;
4. every dependent companion subsystem prepares against that provider;
5. only after all prepares succeed does the composition owner enter the commit phase.

No dependent subsystem may discover the new roster only after #28 has already committed it.

## 5.9 D3 tests

New:

`src/player-database/tests/PlayerIdentityIndexTests.cs`

Cover:

- empty;
- one player;
- multiple unique players;
- same-club duplicate;
- cross-club duplicate;
- first-owner diagnostic;
- source unchanged on failure;
- read-only lookup.

Progression tests prove:

- seed/load duplicate rejection through #27;
- failed `PrepareRosterMutation` leaves roster/lifecycle/index/cursor/generation unchanged;
- plan `SquadFor` exposes proposed, not live, membership;
- live engine remains unchanged while the plan is inspected;
- stale plan is refused before its own writes;
- successful commit installs roster/index/cursor together;
- `NextPlayerId` independently prevents historical reuse.

SeasonSave/PlayerCareerStates tests prove:

- `PlannedProgressionSquads` resolves the exact proposed post-churn players;
- `PrepareRosterSync(plannedSquads)` creates companion state for a new regen and removes state for a retiree before any live roster commit;
- a deliberately invalid proposed roster fails during prepare and leaves all three live owners unchanged;
- after successful four-step commit, ProgressionEngine membership and PlayerCareerStates membership are identical;
- transfer-like move has one live owner;
- duplicate ids fail even when each club's local arrays are individually valid.

A transaction-order mutation test must move `PrepareRosterSync` back onto the old live `_careerSquads`; the new regen/retiree coherence case must fail, proving the planned-roster adapter is load-bearing.

Completion includes a repository inventory of every production career-membership write, classified as staged #28 mutation, external-provider owner validated before sync, or match-local lineup/substitution state that does not alter career membership.

## 5.10 Documentation

Back-propagate #27:

- current-roster global uniqueness;
- `PlayerIdentityIndex` as sole predicate implementation.

Back-propagate #28:

- `NextPlayerId` remains historical non-reuse owner;
- staged roster plan exposes a read-only proposed-roster projection.

Back-propagate #30:

- planned-roster provider is the composition seam;
- companion state prepares against post-churn membership before any write;
- exact prepare/commit order above is normative.

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
4. record mutation id, the **exact unified diff/hunk** (or temporary mutation commit SHA), command, and failing test ids;
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
- `src/player-database/PlayerIdentityIndex.cs`;
- `src/season-save/PlannedProgressionSquads.cs`.

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

`GameplayConfigHolder.cs`, `project-constants/AssemblyInfo.cs`, TrainingSystem/InjuriesMedical runtime catalogues and MedicalStep, `match-client-unity/MatchClientBehaviour.cs` + `match-client-unity.asmdef`, guarded inner roots (`MatchEngine`, `SeasonLoop`, `WorldStore`, `MatchSession`), and every affected test-assembly setup that currently relies on implicit fallback.

### D3

`PlayerRecord.cs`, `ProgressionEngine.cs`, `ProgressionSaveCodec.cs`, `PlayerCareerStates.cs`, `ProgressionSquads.cs` documentation as needed, and `SeasonLoop.cs` for planned-roster staging/commit ordering.

### D4

`SeasonLoopProgressionTests.cs`, approved #28 `section-5.md`, and source docs/version history as needed.

Exact D2 test-setup inventory is discovered and recorded during implementation rather than asserted from memory.

# 9. Assembly-definition impact

## 9.1 D1

CollisionSystem adds DeterministicSim; existing consumers retain direction.

## 9.2 D2

New `TacticalDirector.GameplayBootstrap` is an **application/bootstrap composition assembly**, above the gameplay/domain assemblies it coordinates.

It references:

- `TacticalDirector.ProjectConstants`;
- `TacticalDirector.TrainingSystem`;
- `TacticalDirector.InjuriesMedical`.

Those domain assemblies never reference GameplayBootstrap.

`TacticalDirector.MatchClientUnity`, the current application host, gains a reference to GameplayBootstrap so `MatchClientBehaviour.Awake()` can publish defaults/config before constructing the simulation graph.

ProjectConstants remains a foundation. Its internal publication seam is visible to:

- GameplayBootstrap — production publisher;
- ProjectConstants.Tests — pre-existing test friend.

Low-level **test** asmdefs may reference GameplayBootstrap to perform their one-time process boot; that test-only edge does not alter the production asmdef dependency graph.

The dedicated integration asmdef is named `TacticalDirector.GameplayBootstrap.Integration.Tests`.

GameplayBootstrap must be added to the root assembly map as an application/bootstrap tier, not left to the unresolved gameplay-layer taxonomy. It is above, not inside, Physics/Mechanics/AI and may not be referenced downward by those production layers.

## 9.3 D3

PlayerProgression already references PlayerDatabase, so `ProgressionRosterMutationPlan.SquadFor` can return `PlayerDatabase.Squad` without a new dependency.

SeasonSave already references PlayerProgression, PlayerDatabase, and MatchEngine, so `PlannedProgressionSquads : ISquadProvider` adds no asmdef reference.

No PlayerProgression → MatchEngine reference is introduced.

## 9.4 D4

No production assembly reference changes.

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
- defaults require explicit, one-shot GameplayBootstrap;
- `MatchClientBehaviour.Awake()` is the current Unity production publisher before `ValidateWiring()`;
- GameplayBootstrap is sole **production** binder, while ProjectConstants.Tests retains test-only internal access;
- #29/#41 schema owns reviewed key/fallback/resolver once;
- the plan is explicitly reviewed-domain scoped; each new domain validator joins the hard-coded plan and bumps `ReviewedValidationPlanVersion`;
- new composition roots preserve `RequireBound` dominance;
- tests cannot reset/rebind after real static catalogue initialization.

Correct existing docs that promise implicit `GameplayConfig.Empty` before bind.

## D3

Back-prop #27/#28/#30 together: #27 live uniqueness, #28 historical non-reuse and staged roster storage, and #30's `PlannedProgressionSquads` seam that prepares dependent companion state against the proposed post-churn roster before any write.

## D4

Put normative oracle vectors in approved #28 §5. Correct #30 language about the old “independent” copied oracle. Keep mutation evidence in tracking only.

Historical text is corrected through append-only entries where policy requires.

# 12. Implementation sequence

## A0 — D1 locks only

Record exact SHA and whole-tree baseline; add/freeze consumer vectors without production edits; execute targeted suites; commit.

## A1 — D1 migration

Add helper/constants; migrate one consumer at a time; never alter A0 expected literals; run consumer locks and post whole-tree differential; final literal/helper sweep.

## B — D2 fail-closed bootstrap

Add GameplayBootstrap; add reviewed #29/#41 schemas/validators; make those catalogues use schema resolvers; remove public raw bind; fail unbound reads; add internal `PublishBootConfig`; hard-code reviewed-domain plan; wire `MatchClientBehaviour.Awake()` before `ValidateWiring()`; gate inner roots; update test boot setup; add isolated non-default integration assembly and direct `dotnet test` invocation; back-prop governance.

## C — D3 immutable identity index

Add #27 index/tests; migrate duplicate predicates; add #28 staged whole-roster prepare/commit plus proposed `SquadFor`; add SeasonSave `PlannedProgressionSquads`; prepare `PlayerCareerStates` against that proposed provider before any write; retain/test `NextPlayerId`; inventory membership writers; back-prop #27/#28/#30.

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
- inherited deterministic red diagnostics/predicate values are exact pre/post unless a field was explicitly declared non-deterministic before A1;
- deterministic digests/goldens unchanged.

## D2

- unbound `Config` cannot return defaults;
- no public raw ProjectConstants bind;
- internal publication seam is named/scoped as boot publication, not global validation certification;
- GameplayBootstrap is sole **production** binder; ProjectConstants.Tests' pre-existing friend access is documented;
- caller cannot choose/omit/reorder the reviewed #29/#41 validators;
- D2 never claims unreviewed domains were structurally prevalidated;
- #29/#41 validator/runtime catalogue share schema resolvers;
- `BindDefaults()` and `Bind()` are strictly one-shot;
- `MatchClientBehaviour.Awake()` publishes before `ValidateWiring()`;
- guarded inner roots fail when constructed unbound;
- isolated `TacticalDirector.GameplayBootstrap.Integration.Tests` direct test invocation observes non-default reviewed values and rejects malformed reviewed config before publication;
- no reset/rebind after real catalogue initialization;
- GameplayBootstrap is classified as application/bootstrap composition.

## D3

- #27 is sole live current-roster duplicate predicate/index;
- immutable index is built from the complete proposed roster;
- `ProgressionRosterMutationPlan.SquadFor` exposes proposed membership while live ProgressionEngine remains unchanged;
- SeasonSave's `PlannedProgressionSquads` adapts that plan to `ISquadProvider` without duplicating rules;
- `PlayerCareerStates.PrepareRosterSync` is executed against the proposed post-churn provider before any live roster write;
- failed preparation leaves SeasonState, ProgressionEngine, and PlayerCareerStates unchanged;
- after successful commit, ProgressionEngine and PlayerCareerStates carry identical membership;
- stale-plan guards remain defensive and are logically non-failing inside the documented single-threaded `RollToNextSeason` sequence;
- #28 `NextPlayerId` separately proves historical non-reuse;
- #30 remains boundary orchestrator;
- transaction-order mutation reverting companion preparation to the old live provider is killed by tests;
- every current membership writer is inventoried/classified.

## D4

- approved #28 §5 owns all oracle values;
- no tracking/production output is oracle authority;
- no copied ramp implementation remains in SeasonSave.Tests;
- all required mutation probes record an exact unified diff/hunk or temporary mutation commit SHA, command, and failing test ids;
- mutations are reverted and final suites return to baseline.

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

**Deterministic Simulation #16** owns SplitMix64/Mix13 mathematics. Migration proof is a lock-only predecessor commit plus exact deterministic baseline differential.

**GameplayBootstrap** is the sole production config publisher. **ProjectConstants** is fail-closed until an explicit one-shot bind/default-bind. The bootstrap prevalidates the reviewed #29/#41 invariant surface; it does not pretend unreviewed domains are globally certified. The current Unity publisher is `MatchClientBehaviour.Awake()` before `ValidateWiring()`.

**Training System #29 / Injuries & Medical #41** own their reviewed config schemas and validators; runtime catalogues consume the same resolvers.

**Squad / Player Data #27** owns immutable uniqueness among currently carried players.

**Player Progression #28** owns historical generated-id non-reuse through serialized `NextPlayerId`, the persisted roster representation when wired, and a pure staged proposed-roster plan.

**Season & Competition Loop #30** owns boundary orchestration. `PlannedProgressionSquads` projects the #28 plan as an `ISquadProvider` so companion state is prepared against post-churn membership before any live write.

**Approved Player Progression #28 test-plan text** owns ramp oracle values. PlayerProgression tests verify math; SeasonSave tests verify wiring; mutation evidence records exact reproducible source changes proving those tests are load-bearing.

No consumer keeps a second upstream rule implementation, and no dependent state is prepared from a stale pre-churn roster.

# 16. Validated hostile-review resolution map

This section reflects the hostile critique **after re-auditing that critique against the repository**. It records design obligations, not implementation completion.

1. **Forgeable D2 validator token/list** — removed; no token/list exists and GameplayBootstrap owns the fixed reviewed-domain plan.
2. **Implicit unbound defaults** — removed; unbound Config throws and defaults require an explicit one-shot bind.
3. **Overstated “complete config validation” claim** — corrected. Fail-closed binding is global; pre-publication structural validation is explicitly #29/#41 reviewed-domain scoped.
4. **Unspecified production binder** — corrected. `MatchClientBehaviour.Awake()` is the current Unity publisher and binds before `ValidateWiring()`/session construction.
5. **Ambiguous BindDefaults idempotency/equality** — removed. Binding is strictly one-shot; no equality rule is needed.
6. **Friend-assembly overclaim** — corrected. GameplayBootstrap is sole production publisher; ProjectConstants.Tests retains pre-existing internal access.
7. **Static test contamination** — reset/rebind after real catalogue initialization is forbidden; the non-default proof runs through a dedicated `.Tests` project in its own `dotnet test` invocation.
8. **D3 mutable-registry atomicity problem** — removed through immutable `PlayerIdentityIndex`.
9. **D3 critique correction: stale-generation guard itself is not a blocker** — the repository already uses the same defensive generation-check posture for `PlayerCareerStates.CommitRosterSync`; inside synchronous SeasonLoop preparation it is logically non-failing.
10. **Actual remaining D3 blocker: companion state saw the old live roster** — closed by `ProgressionRosterMutationPlan.SquadFor` + SeasonSave `PlannedProgressionSquads`, so `PrepareRosterSync` stages against the proposed post-churn roster before any write.
11. **Two identity authorities** — separated into different invariants: #27 current-live uniqueness and #28 historical generated-id non-reuse.
12. **D3 ownership conflict** — corrected: #27 validates identity, #28 stages/stores its wired roster, #30 orchestrates both progression and companion-state commit.
13. **Impossible globally-green D1 gate** — replaced by pre/post whole-tree baseline differential.
14. **Fuzzy D1 “materially identical” wording** — removed; deterministic diagnostics/predicate values compare exactly, with any non-deterministic field declared beforehand.
15. **Weak D1 provenance** — A0 lock-only commit precedes A1 production migration.
16. **Duplicated reviewed D2 key/default metadata** — removed through shared #29/#41 schema resolvers.
17. **D4 oracle in non-authoritative tracking** — removed; oracle vectors live in approved #28 §5.
18. **Mutation reproducibility** — evidence now records exact unified diffs/hunks or temporary mutation commit SHAs, plus commands and failing test ids.
19. **Stale header reference to deleted `GameplayConfigHolder.Bind`** — corrected in this amendment.
20. **Bootstrap taxonomy ambiguity** — GameplayBootstrap is explicitly an application/bootstrap composition tier above gameplay/domain assemblies.

Implementation remains incomplete until the acceptance gates are executed and recorded.
