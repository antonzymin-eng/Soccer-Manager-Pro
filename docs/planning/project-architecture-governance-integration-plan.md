# Project Architecture Governance — Integration Map and Implementation Plan

**Document Class:** Integration design and implementation plan  
**Status:** Draft — implementation planning; no production code implemented by this document  
**Version:** 0.1  
**Created:** August 27, 2026  
**Governing authority:** docs/planning/project-architecture-governance.md v0.4  
**Primary downstream specifications:** Testing Strategy & Framework #19; Code Standards & Style Guide #20  
**Related project authorities:** Master Development Plan; adversarial-review process; root and src agent guides  
**Implementation base:** branch docs/round-2-architecture-remediation-design at or after commit 2b67b9c437fe2ec7969dab2e8efb3e28dbc46587

---

# 0. Purpose, scope, and authority

## 0.1 Purpose

This document maps the Project Architecture Governance Specification into the existing Tactical Director repository.

It answers five implementation questions:

1. where governance state and architectural intent live;
2. which existing code, tooling, CI, review, and specification surfaces must change;
3. which facts are discovered mechanically versus declared manually;
4. how architectural proof is produced, invalidated, and revalidated;
5. how the integration can land incrementally without creating a second hand-maintained architecture system.

The objective is not more documentation. The objective is to make the governance rules operational with the minimum durable machinery necessary to prevent architectural drift and repeated rediscovery.

## 0.2 Explicit exclusion — frozen D1–D4 supplement

The deferred D1–D4 architectural remediation supplement is **not an implementation authority for this plan**.

This plan does not schedule, implement, or depend on its proposed:

- SplitMix64 centralization;
- config-bootstrap redesign;
- PlayerId identity-index redesign;
- progression-ramp oracle move.

Those items remain frozen.

Where the frozen supplement exposed a general governance failure mode, that failure mode may already be represented in project-architecture-governance.md. This document implements the governance rule, not the frozen remediation proposal.

## 0.3 Authority boundary

The ownership split remains exactly the one established by project-architecture-governance.md:

| Concern | Normative owner | This plan does |
|---|---|---|
| Architectural property admission | Project Architecture Governance | Provides registry/storage mechanics |
| Finding disposition / convergence semantics | Project Architecture Governance | Reworks review ledger and process integration |
| Integration ownership code rules | Code Standards #20 | Specifies exact amendments and enforcement points |
| Dependency direction | Code Standards #20 | Adds machine checking once the current taxonomy defect is resolved |
| Composition/lifecycle rules | Code Standards #20 | Adds declaration and verification mechanics |
| Proof/evidence requirements | Testing Strategy #19 | Specifies exact amendments and evidence tooling |
| Failure injection / mutation | Testing Strategy #19 | Adds trigger-driven execution/evidence integration |
| Merge-gate mechanics | Testing Strategy #19 | Adds architecture-evidence gate to CI |
| Stage-level quality gates | Master Development Plan | Adds pointer only |
| Review execution | adversarial-review process | Replaces severity-driven convergence with disposition-driven convergence |

No project architecture rule is redefined here.

## 0.4 Integration principles

The implementation MUST preserve these design constraints.

### IP-1 — Discover code facts; declare architectural intent

Facts that can be derived reliably from the repository MUST be generated from the repository.

Examples:

- assembly names;
- asmdef references;
- test versus production assemblies;
- dependency cycles;
- source file existence;
- declared symbols;
- common framework entry points;
- static initialization constructs;
- CI test-project existence.

Human- or agent-authored declarations are reserved for facts that code cannot infer safely:

- which host owns a runtime component;
- whether a public surface is supported or accidental;
- required lifecycle ordering;
- supported alternate paths;
- prohibited bypasses;
- admitted-property state;
- exception rationale;
- evidence dependency scope.

### IP-2 — No third architecture inventory

The existing file-manifest remains the file inventory.

The governance integration MUST NOT create another manually maintained file tree or another hand-copied assembly list.

Generated architecture inventory is ephemeral evidence produced from source and asmdefs. Only architectural intent records are committed.

### IP-3 — No runtime governance dependency

No gameplay or simulation assembly references a governance/tooling assembly.

Governance implementation lives under docs/, tools/, CI, tests, and review workflows.

Production code changes occur only when a governed architectural rule itself requires a code change.

### IP-4 — No giant permanent suppression list

Existing violations or unresolved design questions are not hidden in a broad allowlist.

Temporary non-compliance uses the governance exception mechanism with scope, owner, risk, and expiry trigger.

A baseline may be used during staged activation only when it is finite, enumerated, and retired by an explicit phase gate.

### IP-5 — One source for each control datum

Each durable architecture datum has one source:

- admitted-property history → property registry;
- temporary waivers → exception registry;
- integration/lifecycle intent → integration contracts;
- review finding state → adversarial-review ledger;
- reusable proof → architecture evidence artifact;
- assembly/reference facts → asmdefs and generated inventory;
- source/file existence → repository itself.

Generated reports may repeat data for readability but are never authoritative.

---

# 1. Current-state integration map

## 1.1 Normative document layer

Current project architecture governance spans several authorities that already exist:

| Surface | Current role | Governance integration |
|---|---|---|
| docs/planning/project-architecture-governance.md | Decision layer for properties, findings, proof triggers, convergence | New project-level authority |
| docs/specs/testing-strategy/ | Testing, evidence, CI mechanics | Receives proof/evidence and merge-gate mechanics |
| docs/specs/code-standards/ | Dependency, interface, code architecture rules | Receives ownership/lifecycle/host/public-surface mechanics |
| docs/planning/master-development-plan.md | Stage quality gates | Receives pointer only |
| .claude/skills/adversarial-review/ | Review execution loop | Receives disposition/convergence semantics |
| CLAUDE.md / src/CLAUDE.md | Compact agent routing | Receives concise routing rules only |
| docs/agent-guides/* | Expanded reference | Receives commands and examples, not new authority |

The governance integration should strengthen these boundaries rather than merge them.

## 1.2 Existing mechanical enforcement layer

The repository already contains useful enforcement machinery:

| Existing component | Current strength | Limitation relevant to governance |
|---|---|---|
| .asmdef files | Exact production/test assembly references | No project-level legality check against a complete approved tier model |
| tools/dotnet-ci/generate_projects.py | Rebuilds .NET project graph from asmdefs and fails unknown references | Excludes MatchClientUnity; designed for compilation, not complete architecture inventory |
| tools/dotnet-ci/run-gate.sh | Whole-tree build/test gate | Does not validate architecture property records or evidence artifacts |
| .github/workflows/ci.yml | Merge-time static/build/test jobs | No architecture-governance job |
| tools/recurring-defect-lint.py | Detects known recurring defect classes | Intentionally specialized; should not become the architecture framework |
| .claude/skills/adversarial-review/scripts/findings.py | Stable review finding IDs and round bookkeeping | Convergence is severity-driven, not disposition/property-driven |
| docs/tracking/file-manifest.md | Authoritative file inventory | Not an architectural ownership registry |
| docs/tracking/data-contract-index.md | Entity/data ownership | Does not own runtime activation/lifecycle contracts |

The new integration reuses these systems where their existing responsibility is a match and adds a separate governance audit tool where it is not.

## 1.3 Existing runtime architecture surfaces

The first governance inventory must classify, at minimum, the currently visible application/runtime roots below.

This is an **initial candidate map, not a replacement for mechanical discovery**.

| Surface | Current file | Architectural significance |
|---|---|---|
| Unity application host | src/match-client-unity/MatchClientBehaviour.cs | MonoBehaviour Awake/Start/Update lifecycle; constructs MatchSession |
| Web/headless match host | src/match-client-web/MatchClientHost.cs | Constructs MatchSession and presentation/analytics adapters; Start/Stop |
| Match session root | src/match-client-core/MatchSession.cs | Constructs MatchEngine and match-client runtime services; Start/Stop/TickOnce |
| Match simulation composition | src/match-engine/MatchEngine.cs | Large composition boundary joining gameplay assemblies |
| Season orchestration | src/season-save/SeasonLoop.cs | Cross-system day/season ordering and persistence-facing lifecycle |
| Living-world composition | src/living-world/WorldStore.cs | Owns WorldClock/WorldLoop and long-lived world state |
| Match streaming lifecycle | src/match-viewer/LiveMatchStreamer.cs | Threaded Start/Stop/Pause/Resume lifecycle |
| Web server resource | src/match-client-web/MatchClientServer.cs | Listener/thread lifecycle and external resource ownership |
| Viewer server resource | src/match-viewer/LiveMatchServer.cs | Listener/thread lifecycle and external resource ownership |
| Client screen/application flow | src/client-app/ClientScreenFlow.cs | Application flow boundary that starts match flow |

The implementation-time scanner MUST discover the current repository again. The table above is a starting review set, not the acceptance inventory.

## 1.4 Existing specification defect that affects enforcement activation

Two open Code Standards defects are directly relevant:

- ERR-020-002 — current #20 layer taxonomy does not classify a substantial portion of the present assembly tree;
- ERR-020-003 — reference-direction diagrams use opposite arrow conventions without labeling the meaning.

These do not prevent all governance work.

They **do** prevent a new merge-blocking checker from deciding full tier-direction legality until the normative taxonomy is corrected.

Therefore:

- cycle detection may block immediately;
- unknown/missing asmdef references may block immediately;
- exact current graph inventory may block on internal inconsistency immediately;
- full tier-direction legality remains report-only until #20 receives the approved complete taxonomy and notation correction;
- the #20 governance amendment is the natural landing in which ERR-020-002/003 should be resolved.

The implementation MUST regenerate the asmdef graph at that landing. The old proposed tier table is evidence/history, not a substitute for checking current HEAD.

---

# 2. Target governance control flow

The target project-control flow is:

    repository change
        |
        v
    mechanical inventory
        |
        +--> asmdef/reference facts
        +--> entry-point/static-init facts
        +--> integration-contract references
        +--> property/exception/evidence schema validation
        |
        v
    applicable architecture requirements
        |
        +--> existing approved FR/invariant
        +--> admitted AP-###
        +--> no rule yet -> Candidate Property
        |
        v
    required proof resolution
        |
        +--> structural reachability
        +--> lifecycle/order
        +--> failure injection
        +--> targeted mutation
        |
        v
    adversarial review
        |
        +--> severity = consequence
        +--> disposition = gating state
        |
        v
    convergence decision
        |
        +--> no open Blocker
        +--> every finding dispositioned
        +--> all required proof current
        +--> fresh full review complete
        |
        v
    CI / merge decision

The important separation is that the scanner does not decide whether a new architectural idea is good.

It only proves repository facts and enforces already-settled rules.

---

# 3. Durable governance artifacts

## 3.1 Property registry

### Location

Create:

docs/tracking/architecture-governance/property-registry.json

### Purpose

This is the canonical lifecycle record for generalized architectural properties that enter the governance property process.

It is not a duplicate FR catalogue.

If a concern is already owned by Code Standards #20, Testing Strategy #19, Deterministic Simulation #16, or another approved authority, the finding cites that authority instead of creating an AP record.

### Record shape

Each property record contains:

| Field | Required | Notes |
|---|---:|---|
| id | Yes | Stable AP-### |
| title | Yes | Short name |
| state | Yes | Candidate / Admitted / Superseded / Retired / Rejected |
| normative_statement | For Admitted | Property text when governance itself owns the invariant |
| failure_mode | Yes | Concrete debt/failure prevented |
| scope | Yes | Paths/assemblies/hosts/surfaces |
| exclusions | Optional | Explicit non-scope |
| authority | Yes | Exactly one normative owner |
| existing_requirement | Optional | FR/spec pointer when property process maps to existing authority |
| evidence | Yes for Admitted | Required proof class/mechanism |
| enforcement_class | Yes | Machine / Hybrid / Judgment |
| activation | Yes for Admitted | Immediate or staged |
| exceptions_allowed | Yes | Boolean/mechanism |
| supersedes | Conditional | Prior AP |
| decision_rationale | Yes for terminal/admitted states | Why |
| last_reviewed | Yes | Date + commit/version |

### Registry rules

The validator rejects:

- duplicate AP IDs;
- unknown states;
- Admitted records without authority/evidence/scope;
- more than one authority;
- Superseded without replacement linkage;
- Retired without rationale;
- exception references to nonexistent properties;
- malformed stable IDs.

The registry validator does **not** judge whether the property deserves admission. That remains architectural judgment.

## 3.2 Integration ownership contracts

### Location

Create:

docs/tracking/architecture-governance/integration-contracts.json

### Purpose

This implements the integration/lifecycle contract from Governance Appendix C without adding annotations to gameplay code.

Only runtime-bearing components whose correctness depends on activation need a contract.

### Record shape

Each record contains:

| Field | Meaning |
|---|---|
| component_id | Stable local ID, e.g. IC-MATCH-SESSION |
| component | Type/service/component name |
| source | Repository file and symbol |
| assembly | Owning asmdef assembly |
| owning_host | Host responsible for activation |
| composition_root | Exact integration point |
| construction_path | File/symbol chain or declared factory |
| activation_phase | Awake/start/constructor/day-start/etc. |
| update_use_owner | Tick/update/service owner |
| shutdown_disposal_owner | Stop/dispose/teardown owner or justified N/A |
| testhost_paths | Supported testhost equivalents |
| alternate_supported_paths | Other supported boot paths |
| prohibited_bypass_paths | Known forbidden direct routes |
| static_initialization | None or explicit static-init dependency |
| lifecycle_ordering | Required before/after relationships |
| requirement_refs | FR/AP links |
| evidence_refs | Reusable proof IDs if applicable |

### Contract design rule

Do not copy derived facts into the contract when the audit tool can discover them.

For example, the contract names TacticalDirector.MatchClientCore as the owning assembly; the audit checks the asmdef and source file rather than storing the entire reference list in the contract.

### Initial classification target

The first integration-contract pass must mechanically enumerate and then classify all runtime-bearing roots, including at least the current candidates in §1.3.

An item may be classified as:

- contract-required;
- ordinary runtime class whose activation is entirely owned by a contracted parent;
- test-only;
- tooling-only;
- not runtime-bearing.

That classification closes the inventory without forcing a contract for every class.

## 3.3 Exception registry

### Location

Create:

docs/tracking/architecture-governance/exceptions.json

### Record shape

Each exception contains the fields required by Governance §7.1:

- exception_id;
- property;
- scope;
- reason;
- risk;
- mitigation;
- owner;
- expiry_trigger;
- approval;
- status.

### Rules

Exceptions must be narrow.

The validator rejects:

- missing expiry trigger;
- property that forbids exceptions;
- unknown property ID;
- active exception with empty scope;
- expired exception still marked active.

No generic "legacy architecture" exception is permitted.

## 3.4 Architecture proof artifacts

### Location

Reusable proof:

docs/tracking/architecture-evidence/<proof-id>.json

Transient PR-only proof may remain a CI artifact when no later change needs to rely on it.

### When proof must be committed

Commit a proof artifact when one or more are true:

- an admitted property cites it;
- an exception mitigation cites it;
- later work is expected to reuse/revalidate it;
- it is required to explain an architectural baseline or supported host path;
- the implementation changes a high-risk architectural surface and its acceptance record must remain reproducible after the PR.

Do not commit every ordinary test log.

### Dependency surface

Each reusable proof contains a file-level dependency surface at minimum.

Initial supported dependency selectors:

- exact repository path;
- exact asmdef edge;
- exact integration contract ID;
- exact test project;
- exact governance tool version/commit.

Symbol-level dependency hashing MAY be added later for high-churn files.

File-level dependencies are intentionally the first implementation: they invalidate far less than repository-wide hashing while avoiding a fragile source parser.

### Invalidation

The audit tool calculates current dependency fingerprints.

A reusable proof becomes stale only when a declared dependency changes.

Unrelated repository changes do not invalidate it.

A stale proof blocks only when the associated property/change requires current proof.

## 3.5 Generated architecture inventory

Generated output is not committed as an authority.

The audit command can emit:

artifacts/architecture/architecture-inventory.json

or CI-equivalent output.

It contains:

- all asmdefs;
- production/test classification;
- reference graph;
- graph cycles;
- known framework entry points;
- static initialization sites within supported detection;
- integration contracts and their source resolution;
- governed evidence records and freshness;
- property and exception state.

The generated inventory is evidence and diagnostic input, not a second registry.

---

# 4. Codebase integration map

## 4.1 Assembly and dependency graph

### Source of truth

All src/**/*.asmdef files remain the source of dependency edges.

### New checker behavior

The governance audit reads every asmdef, including Unity-only assemblies excluded by the Linux shim.

Checks:

1. duplicate assembly name;
2. reference to unknown project assembly;
3. production assembly referencing a test assembly;
4. dependency cycle;
5. declared architecture tier when full #20 taxonomy is active;
6. forbidden upward edge under the approved taxonomy;
7. runtime reference to infrastructure-only tooling assembly;
8. integration-contract assembly name resolves to a real asmdef.

### Relationship to tools/dotnet-ci/generate_projects.py

Do not treat generated .csproj output as the complete architecture source.

generate_projects.py deliberately excludes TacticalDirector.MatchClientUnity because the Unity shim cannot compile its engine-facing types.

The architecture scanner must include that assembly.

The two tools may both parse asmdefs because their scopes differ:

- dotnet-ci builds the shim-compatible graph;
- architecture-governance audits the complete Unity project graph.

The architecture parser remains small and deterministic JSON parsing; it does not copy dotnet project-generation logic.

## 4.2 Hosts and composition roots

The governance implementation needs two classes of discovery.

### Mechanically recognizable hosts

The scanner detects repository-language/framework constructs with reliable syntax patterns:

- MonoBehaviour subclasses;
- Awake / Start / OnEnable / OnDisable / OnDestroy lifecycle methods on those subclasses;
- RuntimeInitializeOnLoadMethod if introduced;
- static constructors;
- conventional executable Main entry points;
- test asmdefs and generated test projects.

### Plain C# application roots

Types such as MatchClientHost and MatchSession are ordinary classes and cannot be distinguished safely from a domain service solely by syntax.

These are explicitly declared in integration-contracts.json.

The audit verifies that the declared source/type/member exists.

This avoids heuristic "class name contains Host" enforcement.

## 4.3 Lifecycle and ordering

Lifecycle rules are expressed in integration contracts and proven through normal tests where possible.

Examples of existing surfaces that should receive lifecycle classification:

- MatchClientBehaviour: Awake → Start → Update;
- MatchClientHost: construction → Start → Stop;
- MatchSession: construction → Start/Service/Tick → Stop;
- LiveMatchStreamer: hook configuration before Start; Start single-use; Stop; Pause/Resume;
- MatchClientServer / LiveMatchServer: construction → listener Start → accept-thread lifecycle → Stop;
- SeasonLoop: construction/restore → day/round/season operations;
- WorldStore: construction/restore → AdvanceDay.

The plan does not prescribe new lifecycle behavior for these types.

It requires the existing intended behavior to be declared and, when the governance trigger matrix applies, proven.

## 4.4 Static initialization

The scanner inventories:

- explicit static constructors;
- mutable static fields in governed runtime surfaces;
- Unity runtime initialization attributes;
- assembly-level initialization hooks that match supported patterns.

Code Standards #20 owns whether a pattern is allowed.

The governance tool owns only discovery and comparison to declared/allowed state.

A static field is not automatically a defect.

The checker should fail only for a settled prohibited pattern or a missing required lifecycle declaration.

## 4.5 Runtime public surfaces

A public type or member is not automatically a supported architecture entry point.

Code Standards #20 will add the rule that a runtime-bearing public surface that creates an alternate activation path must be one of:

- supported and declared;
- test-only with enforced visibility/scope;
- intentionally public data/query surface with no activation authority;
- reduced to internal if cross-assembly use does not exist.

The first implementation should not attempt to semantically classify every public member with a regex.

Instead:

1. mechanically inventory public types for affected assemblies during property-specific proof;
2. require integration contracts to identify activation-bearing public surfaces;
3. use repository search/compilation to prove callers where needed;
4. promote a generalized public-surface analyzer only after its detection semantics are reliable.

This obeys the governance ratchet rather than prematurely creating a fragile checker.

## 4.6 Bypass paths

A bypass is any supported/direct path that can construct or activate a component outside its declared owner.

The integration contract records known supported alternatives and prohibited bypasses.

Proof mechanisms may include:

- call-site inventory;
- constructor visibility;
- testhost construction;
- dependency graph;
- targeted mutation removing the intended root;
- failure test for direct unbound construction where an invariant requires it.

The checker must not invent a bypass rule from naming convention alone.

## 4.7 Testhosts

Test assemblies are first-class architectural surfaces when production correctness depends on equivalent boot or lifecycle behavior.

The scanner already has an exact source for test assemblies: asmdef names ending in .Tests and their platform declarations/references.

For each applicable integration contract, testhost_paths identifies:

- owning test asmdef;
- fixture/bootstrap entry;
- intentional divergence from production;
- evidence demonstrating the divergence is safe.

A low-level unit test that constructs a pure calculation directly does not need to emulate an application host.

The requirement applies only when the test claims to verify integration/lifecycle behavior.

## 4.8 Persistence and external resources

Persistence boundaries and external resources already trigger stronger proof under Governance §5.2.

The integration plan does not centralize persistence ownership.

It adds evidence expectations:

- corrupted/invalid input failure path;
- load/restore ordering;
- resource acquisition failure where meaningful;
- teardown/close ordering;
- mutation only for specific integrity invariants.

Existing owner specs continue to define format and domain behavior.

---

# 5. Governance tooling design

## 5.1 Tool location

Create a focused package:

tools/architecture-governance/

Recommended initial files:

- audit.py — command entry point;
- model.py — record parsing and validation;
- asmdef_graph.py — complete asmdef discovery/graph checks;
- evidence.py — dependency fingerprint/freshness logic;
- source_inventory.py — narrow, testable source-surface discovery;
- tests/ — stdlib unittest suite and fixtures.

No third-party Python dependency is required for the first version.

## 5.2 Command surface

Primary command:

python3 tools/architecture-governance/audit.py --repo .

Useful modes:

- --check registry
- --check contracts
- --check asmdefs
- --check evidence
- --inventory <output>
- --changed <base-ref> for local/CI optimization
- --strict for merge-blocking mode

Default local behavior should run all active checks.

## 5.3 Check classes

### AG-CHECK-REGISTRY

Validates:

- property schema;
- property IDs/state;
- single authority;
- exception integrity;
- cross-reference existence.

### AG-CHECK-CONTRACTS

Validates:

- required integration-contract fields;
- source path exists;
- owning asmdef exists;
- declared symbol/member can be found by supported source inventory;
- referenced testhost asmdef exists;
- no duplicate component ID;
- requirement/evidence references resolve.

It does not judge whether ownership is architecturally good.

### AG-CHECK-ASMDEF

Validates:

- complete asmdef parse;
- unknown references;
- production→test dependency;
- cycles;
- infrastructure rules;
- approved tier direction when activated.

### AG-CHECK-EVIDENCE

Validates:

- proof record schema;
- evidence commit/version;
- dependency surface present;
- dependency fingerprints;
- stale/current state;
- required proof references resolve;
- governance tool version used by the proof.

### AG-CHECK-SOURCE-SURFACE

Initial reliable discovery only:

- MonoBehaviour declarations and lifecycle methods;
- runtime-init attributes;
- explicit static constructors;
- conventional Main methods;
- public top-level type inventory where syntax is unambiguous.

Ambiguous semantic questions remain property-specific judgment/proof until a reliable analyzer is admitted.

## 5.4 Tool verification

Because this checker will become merge-critical, its own tests are mandatory before CI activation.

Required known-bad fixtures:

1. duplicate AP ID;
2. Admitted property missing authority;
3. retired property without rationale;
4. exception with no expiry;
5. integration contract pointing to missing file;
6. integration contract pointing to missing asmdef;
7. duplicate integration component ID;
8. asmdef unknown reference;
9. production assembly referencing test assembly;
10. asmdef dependency cycle;
11. forbidden upward edge after taxonomy activation;
12. stale proof dependency;
13. proof with unrelated repository change remaining current;
14. unsupported/malformed evidence selector;
15. missing required testhost reference.

Required known-good fixtures mirror the same classes.

At least one negative fixture per merge-blocking check must prove the checker exits non-zero.

## 5.5 Tool change invalidation

A change to architecture-governance tooling that changes discovery/classification semantics must:

1. run its own tests;
2. identify which reusable proofs cite the previous tool behavior;
3. revalidate or regenerate affected proof;
4. avoid invalidating proof that does not depend on that tool.

This is implemented by storing tool identity in proof artifacts.

---

# 6. Code Standards #20 amendment map

## 6.1 New functional requirements

Append after existing FR-CS-073. The exact numbering below is reserved by this plan for the implementation amendment.

| ID | Planned normative rule |
|---|---|
| FR-CS-074 | Every runtime-bearing component whose correctness depends on activation MUST have an explicit integration owner and integration point. |
| FR-CS-075 | Every production host and composition root MUST be represented in the mechanically checked host/integration inventory. |
| FR-CS-076 | Applicable runtime components MUST declare construction/registration, activation, update/use, and shutdown/disposal ownership, with justified N/A where a phase does not exist. |
| FR-CS-077 | Supported alternate hosts and testhosts MUST preserve applicable architectural invariants or explicitly declare their intentional divergence and evidence. |
| FR-CS-078 | Known bypass activation paths MUST be either prohibited or explicitly classified as supported. |
| FR-CS-079 | A runtime-bearing public surface that creates or implies an activation path MUST be classified as supported, test-only, non-activating public data/query surface, or made non-public. |
| FR-CS-080 | Static initialization that materially participates in runtime ownership or ordering MUST be declared in the component lifecycle contract and MUST NOT bypass required composition ownership. |
| FR-CS-081 | Integration ownership declarations MUST be mechanically validated against repository files, symbols, asmdefs, and supported host/testhost records. |

These rules implement FR-AG-021–025.

They must not duplicate the proof mechanics owned by #19.

## 6.2 Existing requirements to amend

### FR-CS-046 / §3.5.2

Resolve ERR-020-002 and ERR-020-003 in the same #20 amendment that activates the dependency-direction checker.

Required outcome:

- every current production assembly is classified in the approved dependency model or explicitly out-of-band;
- arrow notation states whether an arrow means "depends on" or "is available to";
- intra-tier acyclicity is explicit;
- machine checking reads the approved model rather than a stale copy in an agent guide.

The implementation-time asmdef graph must be regenerated before the taxonomy text is finalized.

### FR-CS-055 / §4.3

Keep asmdefs as the source of cross-assembly dependency facts.

Add a pointer that architecture-governance tooling validates the complete reference graph, including assemblies excluded by the Linux shim.

## 6.3 Section-by-section file map

### docs/specs/code-standards/section-2.md

- append FR-CS-074–081;
- update failure-to-comply text so active machine-enforced architecture violations are Mode 1;
- route temporary waivers to the project architecture-governance exception record rather than inventing a second exception shape for these FRs.

### docs/specs/code-standards/section-3.md

Add a new subsection under architecture mechanics covering:

- integration owner;
- host/composition-root inventory;
- lifecycle declaration;
- alternate/testhost rule;
- bypass classification;
- runtime public-surface classification;
- static initialization participation.

Resolve the complete dependency taxonomy/arrow semantics in §3.5.2.

### docs/specs/code-standards/section-4.md

Add:

- integration-contract location;
- rule that integration contracts are architectural intent, not a copied source tree;
- generated asmdef/host inventory relationship;
- composition-root and host boundary diagram.

Do not add a new runtime assembly.

### docs/specs/code-standards/section-5.md

Add review/checklist items:

- changed host/composition root;
- changed lifecycle owner/order;
- changed public activation surface;
- changed static-init path;
- alternate/testhost impact;
- integration-contract update;
- architecture audit result.

### docs/specs/code-standards/appendices.md

Add paste-ready integration ownership record schema and examples.

Examples should use existing repository components but remain clearly illustrative; the JSON registry is the live record.

## 6.4 #20 enforcement ownership

#20 owns the rule.

tools/architecture-governance proves objective structural facts.

#19 decides how a failed required proof/gate affects merge.

The audit tool must not invent a new dependency tier or architecture property.

---

# 7. Testing Strategy #19 amendment map

## 7.1 New functional requirements

Append after FR-TS-085.

| ID | Planned normative rule |
|---|---|
| FR-TS-086 | An architectural change MUST resolve the Governance §5 trigger matrix and identify which proof classes apply. |
| FR-TS-087 | Required architectural proof MUST use a reproducible proof artifact containing applicable requirement/property, repository surface, result, evidence commit/version, and evidence dependencies. |
| FR-TS-088 | Structural reachability proof MUST enumerate applicable hosts, composition roots, alternate paths, testhosts, and relevant public/runtime surfaces when triggered. |
| FR-TS-089 | Lifecycle/order proof MUST execute or otherwise independently prove required construction, activation, use, teardown, restore, and ordering relationships when triggered. |
| FR-TS-090 | Meaningful failure paths covered by the Governance trigger matrix MUST be deliberately executed; static inspection alone is insufficient where the failure can reasonably be induced. |
| FR-TS-091 | Targeted mutation MUST demonstrate evidence sensitivity for critical integration/integrity invariants when triggered; no project-wide mutation-score target is created. |
| FR-TS-092 | Reusable architectural proof MUST declare a precise dependency surface and is invalidated only by material change to that surface. |
| FR-TS-093 | A review may converge only when all applicable required proof is current, every substantive finding has a valid disposition, no Blocker remains, and a fresh final review has completed. |
| FR-TS-094 | Missing, failed, or stale required architectural proof is a merge blocker once the corresponding governance gate is active. |
| FR-TS-095 | Merge-critical governance tooling MUST have known-good/known-bad verification appropriate to its false-positive/false-negative consequence. |
| FR-TS-096 | When exhaustive machine execution is computationally disproportionate, the approved bounded substitute and omitted uncertainty MUST be recorded in the proof artifact. |

These mechanics implement FR-AG-026–032A and FR-AG-036A/B.

## 7.2 Existing requirements to amend

### FR-TS-084 — defect-to-FR traceability

The current wording requires every defect to cite an FR.

That is too narrow under the governance model.

Amend it so a blocker/finding may link to:

- an FR;
- an admitted AP;
- an approved invariant or equivalent authoritative requirement;
- a concrete independently established correctness/integrity failure where no prior FR exists.

A generalized new preference with no authority becomes a Candidate Property rather than an uncited blocker.

### FR-TS-076 / gate composition

Extend the merge-gate model from three classes to include an **architecture/evidence gate**.

Authority split:

- Governance spec decides applicability/property/disposition rules;
- #20 owns structural code architecture rules;
- #19 owns proof execution/freshness and merge-gate mechanics.

No separate soft architecture gate is created.

## 7.3 Section-by-section file map

### docs/specs/testing-strategy/section-2.md

- append FR-TS-086–096;
- amend FR-TS-084;
- add architecture proof/evidence failure mode.

### docs/specs/testing-strategy/section-3.md

Add mechanics for:

- trigger resolution;
- proof artifact;
- structural reachability;
- lifecycle/order;
- failure injection;
- targeted mutation;
- evidence dependencies/invalidation;
- bounded proof recording.

Do not restate Code Standards ownership rules.

### docs/specs/testing-strategy/section-4.md

Extend CI topology with the architecture/evidence gate.

Map:

- pre-commit/local: fast schema/registry/asmdef checks;
- PR: full active architecture audit + applicable architectural integration tests/proof;
- nightly: only architecture proof whose owner explicitly requires a nightly/soak surface.

### docs/specs/testing-strategy/section-5.md

Add conformance tests for #19's own new governance obligations:

- proof schema checker;
- stale-evidence fixture;
- required-proof missing fixture;
- governance-tool negative fixture;
- review convergence fixture.

### docs/specs/testing-strategy/section-6.md

Update gate composition and triage:

- architecture/evidence failure is blocking when active;
- severity is not equivalent to governance disposition;
- finding requirement linkage follows amended FR-TS-084;
- flake quarantine does not waive missing architectural proof.

### docs/specs/testing-strategy/section-7.md

Remove any newly-obsolete deferral if the governance checker is now present.

Do not create future extensions merely for completeness.

### docs/specs/testing-strategy/appendices.md

Add canonical proof artifact schema and examples.

## 7.4 Test placement

Runtime architecture tests stay with the assembly that owns the behavior unless cross-assembly composition requires a higher-level integration test.

Examples:

- a MatchSession Start/Stop invariant belongs with MatchClientCore tests;
- a MatchEngine composition invariant belongs with MatchEngine tests;
- a SeasonLoop ordering invariant belongs with SeasonSave tests;
- a cross-host equivalence proof may require a dedicated integration test assembly only if no existing test assembly can own the consumer-side behavior cleanly.

No central mega test assembly should become the owner of every architectural invariant.

---

# 8. Adversarial-review integration

## 8.1 Current incompatibility

The existing adversarial-review process terminates when only Low findings or none remain.

findings.py currently computes:

- High/Medium = gating;
- Low = non-gating;
- round cap with High open = stop.

That conflicts with Governance §§2.2–2.3 and §4:

- severity is impact, not disposition;
- a Low finding can block if it violates a MUST;
- a High finding can be an accepted tradeoff if no MUST is violated and the correct authority accepts it;
- Candidate Properties do not independently block;
- round-budget exhaustion with any unresolved Blocker yields NON-CONVERGED.

## 8.2 Review finding schema change

Extend each finding record with:

- evidence;
- requirement_or_property;
- disposition;
- required_action;
- owner;
- status;
- resolution_evidence.

Preserve:

- stable key;
- stable finding ID;
- severity;
- title;
- location;
- problem/fix text.

Do not renumber historical H/M/L IDs merely because the semantics improve. Stability is more valuable than cosmetic ID normalization.

## 8.3 Triage responsibilities

Reviewer:

- reports defect/concern;
- supplies concrete evidence;
- states severity;
- identifies known governing requirement if one is clear;
- may flag that a concern appears novel.

Orchestrator/architectural decision layer:

- deduplicates;
- resolves requirement linkage;
- assigns disposition;
- decides whether a novel generalized concern enters Candidate Property process;
- accepts tradeoff/residual risk only with appropriate authority.

Ledger:

- validates fields;
- persists stable IDs;
- computes open blockers;
- computes convergence state;
- never decides architectural quality.

## 8.4 New exit semantics

Recommended:

| Exit | Meaning |
|---|---|
| 0 | Review can converge: no open Blocker, every finding dispositioned, required proof state supplied, fresh round complete |
| 1 | Open Blocker or incomplete mandatory disposition/proof |
| 2 | Usage/schema error |
| 3 | Round budget exhausted while gating obligations remain → NON-CONVERGED |

The exact numeric codes may remain compatible where practical, but their meaning changes from severity to governance state.

## 8.5 Fix routing

Severity may still route model capability and work order.

For example:

- High Blocker → strongest fixer first;
- Medium Blocker → normal fixer;
- Low Blocker → still gates despite Low impact;
- Accepted Tradeoff → no fixer unless mitigation is required;
- Residual Risk → record/revisit trigger, not forced repair;
- Candidate Property → property process, not current-review fix;
- Resolved → verify next fresh pass.

Thus severity remains useful without controlling convergence.

## 8.6 findings.py verification

Add tests covering:

1. Low + Blocker → gates;
2. High + Accepted Tradeoff → does not gate after valid approval fields;
3. High + Residual Risk → does not gate after valid risk fields;
4. Candidate Property → does not gate current review;
5. open Blocker at round cap → NON-CONVERGED;
6. unresolved required evidence → gates;
7. all findings dispositioned but no fresh review marker → does not converge;
8. stable key retains ID across severity/disposition changes;
9. malformed Blocker with no authority/correctness basis is rejected;
10. duplicate key still rejected.

## 8.7 Skill text changes

Update .claude/skills/adversarial-review/SKILL.md:

- replace "only Low or none" termination;
- separate severity and disposition definitions;
- add property/requirement linkage;
- add Candidate Property handling;
- explain that review does not invent a generalized merge rule mid-round;
- retain fresh full re-review;
- retain stable finding identity;
- retain round budget, but record NON-CONVERGED rather than approval;
- keep existing ERR/back-prop obligation for approved-spec defects;
- keep reviewers independent and evidence-driven.

---

# 9. Agent workflow integration

## 9.1 Root CLAUDE.md

Add only a compact routing rule.

Suggested substance:

- architecture/cross-system/runtime-ownership changes must read project-architecture-governance.md;
- consult the architecture property registry and integration contracts;
- settled objective architecture rules must be proven by the governance audit, not asserted from memory.

Do not copy property schemas or proof mechanics into root CLAUDE.md.

## 9.2 src/CLAUDE.md

Under "Before writing code" / Architecture add:

- if changing an application host, composition root, runtime service activation, lifecycle order, cross-assembly public activation surface, static initialization, or alternate testhost path, inspect/update the relevant integration contract;
- run the architecture audit for affected work;
- no new hand-maintained assembly list.

## 9.3 docs/agent-guides/coding-reference.md

Add:

- governance audit commands;
- integration contract examples;
- explanation of generated inventory versus declared intent;
- how to identify an affected proof dependency;
- how to record a bounded proof.

The reference guide remains non-authoritative.

## 9.4 docs/agent-guides/project-reference.md

Add locations for:

- governance spec;
- property registry;
- integration contracts;
- exception registry;
- evidence folder;
- audit tool.

## 9.5 landing-close-out skill

Add an architecture-governance applicability check.

When a landing changes any governed architecture surface, close-out verifies:

- applicable integration contract updated;
- required proof exists/current;
- property/exception references resolve;
- governance audit executed;
- changelog/manifest statements do not overclaim gate status.

Do not duplicate the governance schema in the skill.

## 9.6 orchestrator skill

The orchestrator should route architectural work through the governance applicability check before final close-out.

It must not automatically create an AP for every reviewer suggestion.

---

# 10. CI integration

## 10.1 New job

Add a dedicated job to .github/workflows/ci.yml:

architecture-governance

Initial steps:

1. checkout;
2. setup Python already available on runner;
3. run governance-tool unit tests;
4. run architecture audit in strict mode for activated checks;
5. emit generated inventory/evidence diagnostics.

This job is separate from recurring-defect-lint because the responsibilities differ.

## 10.2 Why separate from dotnet-ci

Static governance validation is fast and should fail before the 30–50 minute match-engine suite.

Runtime architectural tests remain ordinary NUnit tests and continue through run-gate.sh.

This provides:

- fast structural failure;
- no second test runner;
- no special architecture mega-suite;
- clear owner for failures.

## 10.3 Activation tiers

### Immediately blocking once tooling lands

- governance tool self-tests;
- malformed property/exception/contract records;
- duplicate IDs;
- dangling file/asmdef references in contracts;
- asmdef unknown reference;
- production→test assembly reference;
- dependency cycles;
- stale required proof that explicitly declares its dependency surface.

### Report-only until prerequisite closes

- full dependency-tier direction legality until ERR-020-002/003 and #20 taxonomy amendment land;
- completeness of integration contracts until the initial current-root inventory has been reviewed and accepted;
- any source-surface heuristic that has not yet demonstrated acceptable false-positive/false-negative behavior.

### Blocking after baseline acceptance

- new/changed runtime root missing a required integration contract;
- changed lifecycle dependency without updated evidence;
- prohibited bypass path;
- missing required structural/lifecycle/failure/mutation proof;
- open adversarial-review Blocker at merge convergence.

## 10.4 Changed-surface optimization

The audit may accept a base ref to identify which committed contracts/proofs need revalidation.

Optimization must not change semantics.

If changed-file optimization cannot prove a dependency is unaffected, it falls back to the full relevant check.

---

# 11. Staged implementation sequence

## G0 — Adopt the integration plan

Deliverables:

- this document reviewed;
- frozen D1–D4 scope explicitly separated;
- no code/tool/spec claim of implementation yet.

Gate:

- no accidental normative rule duplicated into this plan.

## G1 — Land downstream specification amendments

Files:

- Testing Strategy #19 sections 2–7 + appendices as applicable;
- Code Standards #20 sections 2–5 + appendices as applicable;
- Master Development Plan pointer;
- SPEC_INDEX/version histories;
- resolve ERR-020-002/003 as part of #20 dependency-model amendment.

Rules:

- governance spec remains decision owner;
- #19/#20 become the mechanical normative owners named by governance;
- no CI code yet claims enforcement.

Gate:

- fresh spec review;
- cross-reference sweep;
- complete current asmdef taxonomy approved before direction gate can later block.

## G2 — Create governance state records

Create:

- property-registry.json;
- integration-contracts.json;
- exceptions.json;
- architecture-evidence directory convention/README only if needed.

Seed policy:

- do not invent APs merely to populate the registry;
- carry active Candidate/Admitted properties only when formally identified;
- seed integration contracts from a complete current runtime-root inventory;
- exceptions start empty unless a real active deviation exists.

Gate:

- every seeded record has a concrete repository referent;
- no copied assembly list beyond contract fields needed for intent.

## G3 — Implement architecture-governance audit tool

Implement:

- registry validation;
- exception validation;
- contract resolution;
- complete asmdef graph;
- cycle and test-reference checks;
- source-surface inventory for reliable patterns;
- evidence freshness/fingerprint logic;
- deterministic output ordering.

Add unit fixtures.

Gate:

- all known-bad fixtures fail;
- all known-good fixtures pass;
- no third-party dependency required;
- MatchClientUnity included in asmdef inventory;
- tool does not read generated .csproj as authority.

## G4 — Migrate adversarial review semantics

Modify:

- SKILL.md;
- findings.py;
- findings.py tests/fixtures.

Migration behavior:

- historical ledgers remain readable where practical;
- new fields may default only when legacy input is explicitly recognized;
- new reviews require disposition fields;
- convergence no longer depends on severity alone.

Gate:

- Low Blocker mutation proves gate sensitivity;
- High accepted non-blocker fixture proves severity is independent;
- round-cap NON-CONVERGED fixture passes;
- fresh-review requirement enforced.

## G5 — Integrate architecture proof mechanics

Implement #19-owned proof workflow:

- proof artifact validation;
- dependency fingerprint generation;
- evidence current/stale result;
- affected tests remain in owning assemblies;
- failure/mutation evidence recorded only when triggered.

Gate:

- stale dependency fixture blocks;
- unrelated change fixture does not stale proof;
- proof artifact without applicable property/requirement is rejected when required.

## G6 — Activate CI

Modify:

- .github/workflows/ci.yml;
- optionally local pre-commit/runbook references;
- do not merge architecture checks into recurring-defect-lint.

Activation order:

1. tool self-tests;
2. record/schema integrity;
3. graph integrity;
4. accepted current integration-contract completeness;
5. #20 tier-direction legality;
6. required proof freshness.

Gate:

- representative violation PR/fixture demonstrably fails CI;
- checker false-positive path has documented ordinary debugging route;
- no soft pass on a required architecture failure.

## G7 — Agent-guide and close-out integration

Modify compact guides and load-on-demand references after the commands and files actually exist.

Update landing-close-out/orchestrator routing.

Gate:

- compact guides remain compact;
- no stale command advertised before tooling exists;
- no duplicate authority text.

## G8 — Baseline retirement

If staged activation required temporary baseline records:

- every baseline item must be either fixed, formally excepted, or classified as non-applicable;
- delete activation-only baseline machinery after convergence;
- do not carry a permanent "legacy" suppression file.

Gate:

- architecture job can run strict against current tree without hidden baseline debt.

---

# 12. Detailed change-impact matrix

| Area | New files | Modified files | Runtime behavior |
|---|---|---|---|
| Governance state | property-registry.json, integration-contracts.json, exceptions.json | project governance pointer/history only if needed | None |
| Architecture tooling | tools/architecture-governance/* | none initially | None |
| Review tooling | tests for findings ledger | adversarial-review SKILL.md, findings.py | None |
| Code Standards | none | #20 sections 2–5, appendices, SPEC_INDEX | Normative code rules only |
| Testing Strategy | none | #19 sections 2–7, appendices, SPEC_INDEX | Normative test/gate rules only |
| Master plan | none | master-development-plan.md pointer | None |
| CI | none | .github/workflows/ci.yml | Build/merge behavior only |
| Agent guidance | none | CLAUDE.md, src/CLAUDE.md, expanded references | None |
| Landing workflow | none | landing-close-out/orchestrator skills | Process only |
| Runtime tests | property-specific as triggered | existing owning test assemblies | Test behavior only |
| Production src | none required merely to adopt governance | only future fixes required by admitted/existing rules | Change-specific |

The governance integration itself should land with **zero gameplay-format, RNG, tuning, simulation, or save-state change**.

---

# 13. Implementation ownership map

## 13.1 Governance decision owner

Owns:

- AP admission/rejection;
- disposition validity;
- exception approval;
- tradeoff/residual-risk acceptance;
- proportionality;
- property retirement.

Does not own CI code or gameplay architecture implementation.

## 13.2 Code Standards #20 owner

Owns:

- dependency model;
- host/composition rules;
- integration ownership rules;
- lifecycle declarations;
- public activation surface rules;
- static initialization architecture constraints;
- bypass rules.

## 13.3 Testing Strategy #19 owner

Owns:

- proof artifact mechanics;
- evidence freshness;
- failure injection protocol;
- mutation protocol;
- merge-gate behavior;
- architecture-evidence CI topology.

## 13.4 Component/domain owners

Own:

- actual runtime lifecycle behavior;
- domain-specific correctness;
- tests proving their own behavior;
- any production code change required to comply.

Governance does not centralize those responsibilities.

## 13.5 Tooling owner

tools/architecture-governance owns mechanical discovery/validation implementation.

It does not own the rules it checks.

---

# 14. Failure modes this integration is designed to prevent

## FM-GI-1 — Architecture rule exists only in reviewer memory

Prevention:

- admitted rule gets stable property/FR authority;
- objective settled rule becomes checker when reliable.

## FM-GI-2 — New service exists but nobody owns activation

Prevention:

- integration contract;
- contract completeness for affected roots;
- structural reachability proof when triggered.

## FM-GI-3 — Test proves a path production never executes

Prevention:

- production/testhost inventory;
- alternate-host classification;
- reachability/lifecycle proof.

## FM-GI-4 — Static initializer bypasses composition root

Prevention:

- static-init inventory;
- lifecycle contract;
- #20 prohibition/exception rule;
- targeted mutation where critical.

## FM-GI-5 — Review never converges because new preferences become blockers

Prevention:

- Candidate Property disposition;
- existing-rule linkage;
- property admission separate from active review.

## FM-GI-6 — Real MUST violation is waved through because severity is Low

Prevention:

- blocker disposition independent of severity;
- Low Blocker gates.

## FM-GI-7 — Evidence remains trusted after architecture changes

Prevention:

- dependency surface;
- fingerprint/freshness check;
- targeted invalidation.

## FM-GI-8 — Every unrelated commit reruns every architecture proof

Prevention:

- exact/file-level dependency scopes;
- changed-surface selection;
- no repository-wide invalidation unless the invariant is repository-wide.

## FM-GI-9 — Governance checker silently misses the violation class

Prevention:

- merge-critical checker fixtures;
- known-bad negative tests;
- regression tests for blind spots.

## FM-GI-10 — Governance machinery becomes larger than the architecture

Prevention:

- stdlib-only focused tool;
- no runtime framework;
- no duplicate source inventory;
- property/exception retirement;
- no mandatory mutation-score program;
- no recursive meta-checker.

---

# 15. Acceptance gates for the completed governance integration

## 15.1 Authority

- [ ] Governance specification remains the only owner of property/disposition/convergence policy.
- [ ] #19 owns proof/evidence/merge mechanics.
- [ ] #20 owns integration/lifecycle/dependency code rules.
- [ ] Master plan contains pointer-level text only.
- [ ] Agent guides contain routing/examples, not duplicate authority.

## 15.2 Repository state

- [ ] Complete current asmdef graph is mechanically generated.
- [ ] Full #20 dependency taxonomy covers current production assemblies.
- [ ] ERR-020-002/003 are resolved before direction legality blocks merge.
- [ ] Runtime-root inventory is complete and reviewed.
- [ ] Required integration contracts resolve to real files/types/assemblies.
- [ ] No permanent broad baseline/suppression hides unresolved architecture debt.

## 15.3 Property and exception state

- [ ] Property registry exists and validates.
- [ ] No duplicate AP ID.
- [ ] Admitted properties have one authority, scope, evidence, enforcement class.
- [ ] Exception registry exists and validates.
- [ ] Every active exception has owner, risk, mitigation, expiry.

## 15.4 Proof

- [ ] Proof artifact schema implemented by #19.
- [ ] Structural/lifecycle/failure/mutation triggers implemented.
- [ ] Reusable proof declares dependencies.
- [ ] Stale required evidence blocks.
- [ ] Unrelated changes do not invalidate proof.
- [ ] Bounded proof records omitted uncertainty.

## 15.5 Review

- [ ] findings.py supports requirement/property and disposition.
- [ ] Low Blocker gates.
- [ ] Accepted High tradeoff does not gate solely because it is High.
- [ ] Candidate Property does not independently block.
- [ ] Round budget produces NON-CONVERGED when blockers remain.
- [ ] Fresh final review is required.
- [ ] Stable finding identity survives severity/disposition changes.

## 15.6 Tool verification

- [ ] Architecture audit tests contain known-good and known-bad fixtures.
- [ ] Every merge-blocking check has at least one negative fixture.
- [ ] Tool changes identify affected reusable proof.
- [ ] Tool validation stops at ordinary software testing; no recursive governance chain.

## 15.7 CI

- [ ] Architecture-governance static job runs before expensive suites where possible.
- [ ] Runtime architecture tests continue through normal owning test assemblies.
- [ ] Required architecture failure is not soft.
- [ ] Flake quarantine cannot waive missing architectural proof.
- [ ] The CI result differentiates structural audit failure from runtime-test failure.

---

# 16. Explicit non-goals

This integration does not:

- implement the frozen D1–D4 design;
- change gameplay behavior;
- change save formats;
- change RNG streams, domain tags, or deterministic draw order;
- retune gameplay constants;
- create a runtime DI framework;
- create a central service locator;
- create a universal architecture test assembly;
- require a contract for every class;
- require mutation testing for every change;
- require repository-wide proof after every commit;
- convert architectural judgment into a linter;
- make file-manifest.md an architecture authority;
- duplicate the entire source tree in JSON;
- block full dependency direction before #20 owns a complete current taxonomy.

---

# 17. Final target state

After this plan is implemented:

1. An agent changing a runtime architecture surface can discover the applicable ownership and proof requirements without reconstructing them from historical reviews.
2. The repository mechanically knows its complete assembly graph and whether declared architecture records still point to real code.
3. High-risk integration paths have explicit owners and lifecycle contracts.
4. Architectural proof is targeted, reproducible, and invalidated only by relevant changes.
5. Review findings separate impact from gating disposition.
6. New generalized architectural concerns enter a finite property-admission path instead of becoming improvised merge blockers.
7. Settled objective rules are enforced automatically once the enforcement itself is trustworthy.
8. Architectural judgment remains responsible for ownership quality, abstraction quality, proportionality, tradeoffs, and rule evolution.
9. The governance layer remains small: registries for intent/state, a focused audit tool, existing owning tests, and CI/review integration.

That is the intended remediation: **architectural decisions remain judgment-driven; architecture facts and settled obligations stop depending on memory.**

---

# Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | August 27, 2026 | — | Initial detailed integration map for Project Architecture Governance v0.4. Maps #19/#20 amendments, runtime/code surfaces, governance state records, audit tooling, adversarial-review migration, CI activation, evidence invalidation, and staged implementation. Explicitly excludes the frozen D1–D4 remediation supplement. |
