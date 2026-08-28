# Project Architecture Governance — Integration Map and Implementation Plan

**Document Class:** Integration design and implementation plan  
**Status:** Draft — implementation planning; no production code implemented by this document  
**Version:** 0.3  
**Created:** August 27, 2026  
**Governing authority:** docs/planning/project-architecture-governance.md v0.4  
**Primary downstream specifications:** Testing Strategy & Framework #19; Code Standards & Style Guide #20  
**Related project authorities:** Master Development Plan; adversarial-review process; root and src agent guides  
**Review/authoring base:** branch docs/round-2-architecture-remediation-design at commit 9b03dad3997ad927e2f81044ec747e596a58273b (provenance only; not an evidence-freshness key)

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

## 0.5 Amendment precedence and activation prerequisites

Version 0.3 preserves the v0.2 sequencing decisions and adds the end-to-end implementation constraints required to make those stages mechanically sound without reopening the architectural decisions in project-architecture-governance.md.

The following rules override earlier sequencing in this plan:

1. Governance v0.4 is currently Draft. It is design input until an explicit adoption gate records approval, completed self-checklist, SPEC_INDEX/status alignment, and the exact governing version plus canonical Governance content/blob digest. A Git revision MAY be recorded as provenance but is not required to be self-embedded in the landing that creates the approved artifact.
2. Dependency-direction enforcement MUST NOT become blocking until a read-only current-tree discovery pass has produced the complete asmdef graph, every assembly has an explicit production/test/tooling/out-of-band classification, arrow semantics are fixed, and ERR-020-002 / ERR-020-003 are resolved.
3. Machine-readable schemas for discovery classification, applicability, integration contracts, proof, finding ledgers, and any temporary baseline MUST be frozen before #19/#20 normative amendments are finalized.
4. #19 and #20 are amended and reapproved as one coordinated governance-integration bundle. Enforcement eligibility requires both amendments approved against the same repository base and governance version.
5. No checker may make an absence claim blocking unless the relevant search universe is closed and mechanically enumerated. Known-path lists and naming heuristics are not proof of absence.
6. No CI job is a merge gate merely because it exists. Required-status configuration and skipped/cancelled/unavailable behavior are part of activation.
7. A temporary baseline is permitted only as a finite migration artifact and MUST be mechanically empty at final strict activation.
8. Committed governance artifacts MUST separate the material subject they prove from the Git commit/tree that happens to contain the evidence record. A committed artifact MUST NOT require equality with its own containing commit/tree as a freshness condition.
9. A1 closed-world discovery uses the union of compiler/mechanical candidates and a finite bootstrap intent declaration for roots that syntax cannot infer. A4 promotes those declarations into final contracts; it does not introduce previously invisible runtime roots.
10. Merge-blocking C# symbol/public-surface/static-initialization discovery MUST consume compiler-backed semantic facts. The Python governance tool may orchestrate those facts, but MUST NOT implement a regex or hand-written C# parser and call the result closed-world proof.
11. A2 freezes not only JSON shapes but the executable identity, selector, applicability, dependency-closure, and freshness semantics needed to interpret them. Those semantics MUST pass representative fixtures before A3 reapproval.
12. Required executable proof is satisfied only by an explicit successful execution state. Skipped, excluded, unavailable, not-run, or runner-failed evidence does not satisfy a required proof unless #19 permits and records a bounded substitute.

This document remains an implementation plan. It does not itself approve Governance v0.4 or modify approved #19/#20 requirements.

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

# 3. Durable governance artifacts and frozen machine contracts

## 3.1 Schema-freeze rule

Before any merge-blocking architecture tool is implemented, A2 freezes versioned schemas for runtime-surface classification, applicability resolution, integration contracts, property records, governance exceptions, proof artifacts, adversarial-review findings, and any temporary activation baseline.

Every schema carries schema_version and rejects unknown major versions. Schema evolution that changes discovery, applicability, gating, or proof semantics invalidates affected downstream evidence and reopens the corresponding approval step.

Free-text narrative MAY supplement a record, but blocking checks MUST depend only on typed fields whose semantics are defined and tested.

A2 is not a schema-shape review. Before a machine contract is frozen, an executable reference implementation MUST demonstrate canonical selector parsing/resolution, stable-identity handling, applicability precedence/conflict behavior, proof dependency-closure calculation, subject-scope fingerprinting/freshness, review-state transitions, and N/A/bounded-result handling. A5 may productionize and optimize those semantics, but MUST NOT silently redefine them.

Any change after A2 that materially changes those semantics reopens the affected A2/A3 approval dependency; implementation bugs that do not change semantics are ordinary tooling fixes.

## 3.2 Closed-world runtime-surface classification, identity, and provenance

Create `docs/tracking/architecture-governance/runtime-surface-classifications.json` for durable classification intent. Generated discovery output remains ephemeral evidence; the committed file MUST NOT become a hand-maintained copy of the source tree.

Allowed classifications remain: `production-runtime-root`; `contracted-child`; `test-only`; `tooling-only`; `generated-or-external`; `non-runtime-bearing`.

### 3.2.1 Subject identity versus provenance

Every durable evidence-bearing schema separates:

- `subject_scope_digest` — digest of the exact applicability/dependency closure whose state is being asserted;
- scoped inventory/asmdef/config fingerprints only when those surfaces are members of that closure;
- `provenance_revision` / `provenance_tree` — optional record of the checkout on which the evidence was produced;
- `artifact_identity` / created metadata — identity of the evidence record itself.

`provenance_revision` and `provenance_tree` are provenance, not universal freshness predicates. A committed record MUST NOT require its containing commit/tree to equal a value stored inside itself. CI-transient evidence MAY record `GITHUB_SHA` directly because that evidence is not inserted back into the tree it identifies.

Freshness is decided by recomputing `subject_scope_digest` and the proof-class-specific dependency fingerprints. Unrelated repository changes therefore do not invalidate a proof unless its declared/derived closure actually includes the changed surface.

### 3.2.2 Closed discovery and bootstrap declarations

The discovery universe MUST include all asmdefs; Unity lifecycle/initialization entry surfaces; conventional `Main` entry points; supported serialized/factory activation surfaces; testhosts/tooling assemblies; explicit static constructors; and compiler-generated type initialization caused by static field initializers.

Plain-C# architectural roots cannot always be inferred from syntax or present production callers. A1 therefore permits a finite temporary file `docs/tracking/architecture-governance/bootstrap-runtime-surfaces.json` containing only the architectural intent needed to close the initial universe: stable logical component ID, canonical selector, intended root/host classification, and rationale. It MUST NOT contain a copied source-tree inventory.

A1 computes the fixed-point union of compiler-discovered candidates plus bootstrap declarations and classifies every emitted surface or records it unresolved. A2 freezes final selector/identity semantics and reruns A1 through those semantics before A3. At A4 the bootstrap declarations are migrated into final integration contracts/classifications and the temporary bootstrap file is retired.

Test classification MUST NOT rely on a `.Tests` suffix alone. Assembly metadata, path, platform/define constraints, references, compiler facts, and explicit classification are considered. `TacticalDirector.TestingStrategy` or any similar assembly must be explicitly classified.

### 3.2.3 Stable identity and selector algebra

Compiler-discovered source surfaces use deterministic mechanical `symbol_key` values derived from canonical compiler symbols/signatures. Those keys are discovery identities, not permanent architectural IDs.

Stable `component_id` values are allocated only for durable declared architectural concepts such as a supported host, composition root, runtime component, or testhost. A file/symbol rename updates that component's selector/history; it does not create a new architectural component solely because a path changed.

The selector grammar MUST be frozen in A2 and MUST distinguish namespaces/types, constructors, overloaded method signatures, static members, and assembly identity. Contracts keep `selector_history` sufficient to migrate ordinary moves/renames while preserving logical identity. Ambiguous or multiply resolving selectors fail strict mode.

Each classification record therefore carries the current `symbol_key`, `kind`, source path, symbol/signature, assembly, classification, and stable `component_id`/`contract_id` only when the surface has durable architectural identity.

Strict mode fails when a newly discovered surface is unclassified after initial baseline acceptance.

## 3.3 Property registry

`property-registry.json` adds `schema_version`, `decision_id`, `decision_actor`, optional `decision_provenance_revision`, `transition_from`, `transition_to`, `decision_rationale`, and `revalidation_history`.

`decision_provenance_revision` is provenance only and MUST NOT require the registry landing to contain its own future commit SHA. Transition immutability is enforced by comparing the proposed registry/history against the trusted merge-base/parent version and permitting only schema-valid append/transition operations. If the prior authoritative registry cannot be retrieved, strict transition validation reports uncertainty rather than silently accepting rewritten history.

The validator enforces legal transitions and append-only decision history; it does not judge admission quality.

## 3.4 Typed integration ownership contracts

`integration-contracts.json` uses typed fields including: stable `component_id`; current `source_selector`; `selector_history`; assembly; `owning_host_component_id`; `composition_root_selector`; typed construction edges; typed lifecycle edges; activation phase; update/use owner; shutdown/disposal owner or justified N/A; `testhost_component_ids`; `alternate_supported_component_ids`; public activation selectors; prohibited bypass selectors; static-initialization selectors; requirement refs; evidence refs.

Edges refer to stable logical component IDs where architectural identity exists and to canonical compiler selectors for concrete symbols. A rename/move therefore updates selector binding without rewriting every dependent architectural record.

Narrative fields MAY explain intent but cannot satisfy a blocking mechanical ownership proof.

Blocking is allowed only for assertions independently verifiable through typed selectors/edges, compiler-backed closed-universe absence checks, or current #19 proof. Unsupported semantic claims remain Hybrid/Judgment and report-only.

The contract schema does NOT attempt to encode every component's full runtime state machine or concurrency semantics. Complex single-use, mutual-exclusion, lock-order, thread-affinity, and equivalent lifecycle invariants remain component-owned behavior demonstrated through applicable #19 evidence; their proof dependency closure MUST nevertheless include the members/owners on which the invariant depends.

## 3.5 Applicability manifest and deterministic resolver

Create `docs/tracking/architecture-governance/applicability-rules.json`.

Each rule contains `rule_id`, selectors, trigger ref, requirement refs, proof classes, gate classes, allowed N/A reasons, precedence, and fallback scope.

All matches are evaluated. Schema-defined specificity controls precedence; equal-precedence conflicts fail. N/A is valid only for an enumerated reason and required approval reference. `--changed` optimizes only after applicability is resolved and falls back to the full relevant universe whenever non-impact cannot be proven. Unresolved applicability fails strict mode.

Applicability answers **which obligations apply**. It does not itself define the complete freshness dependency surface of a proof. The proof-class closure resolver in §3.7 derives that surface from the matched obligations, integration contracts, compiler/asmdef facts, tests/fixtures, configuration, and tooling required by the proof class.

A2 MUST execute the resolver against representative good/bad/conflict/N/A fixtures. Identical repository facts and declarations MUST resolve to identical obligation sets and closure inputs before A3 can rely on the schema.

## 3.6 Exception routing and precedence

Governance exceptions remain property-oriented exactly as Governance §7 defines them. This integration MUST NOT route FR-CS or FR-TS waivers directly into exceptions.json unless the affected obligation is an admitted AP that explicitly allows an exception.

Existing #19/#20 exception mechanisms remain owner-specific. They cannot waive an admitted AP, missing required evidence, concrete correctness/integrity failure, or Governance Blocker.

## 3.7 Canonical proof artifact schema and dependency closure

The #19 amendment MUST land the canonical schema and closure semantics before proof workflow or CI gating.

Reusable proof records require: `schema_version`; `proof_id`; `proof_class`; requirement/property refs; applicability rule IDs; result (`pass`/`fail`/`na`/`bounded`); N/A or bounded justification/approval; `subject_scope_digest`; provenance revision/tree metadata; proof-class dependency closure and content fingerprints; scoped inventory/asmdef digests when applicable; relevant configuration fingerprints; tool/extractor identities; runner/execution records; conditional failure-injection and mutation records; created metadata; revalidation history.

### 3.7.1 Proof-class closure resolution

The audit MUST derive and validate closure using the proof class rather than trusting an author-supplied file list:

- structural reachability: matched contract + owning roots + construction/registration edges + applicable public/bypass surfaces + relevant asmdef nodes/edges;
- lifecycle/order: structural closure + lifecycle members, owners, ordering edges, relevant synchronization/thread-affinity members, and testhost equivalents;
- persistence/external-resource proof: applicable structural/lifecycle closure + serializer/schema/resource/configuration surfaces;
- executable failure/mutation proof: applicable closure + exact target symbol, test/fixture, runner configuration/environment, and tool semantics.

Proof records MAY include additional declared dependencies, but the resolver verifies they are not narrower than the mechanically required closure. If the resolver cannot prove closure completeness, strict mode fails or the proof must use a #19-approved bounded substitute.

Freshness must detect material additions, deletions, renames, generated/config changes, new applicable roots, asmdef changes, and checker/extractor-semantic changes inside that resolved closure. A rename that preserves stable component identity updates selector binding and fingerprints without pretending the architectural component was deleted/recreated.

### 3.7.2 Execution truth

Every required executable record carries `execution_state` from: `passed`, `failed`, `skipped`, `excluded`, `unavailable`, `not-run`, `runner-failed`. Only `passed` satisfies an unqualified required execution obligation. Any other state is unsatisfied unless #19 explicitly permits a bounded substitute and that substitute is recorded/approved.

Execution records bind the exact test/command/runner, environment/configuration, subject digest, start/end result, and machine-readable result artifact when the runner provides one.

### 3.7.3 Failure-injection and mutation identity

Failure-injection evidence records the exact injected condition/input, target selector, expected failure/recovery path, executed command/test, observed result, and tool/environment identity.

Mutation evidence records the base subject digest, exact target selector, mutation operator or canonical patch/mutant digest, baseline execution, mutant execution, expected detector, observed detector failure, tool identity, and restoration/clean-state verification. A no-op/equivalent/wrong-target mutant cannot satisfy the named invariant merely because a test command ran.

Proof-class validation is conditional. A triggered mutation/failure proof without these fields is invalid. Structural proof without its required closed-world/scoped inventory binding is invalid.

## 3.8 Versioned adversarial-review ledger

Create a canonical durable `docs/tracking/architecture-governance/review-ledger.json`. The existing ignored `.adversarial-review/` directory remains scratch/session cache only and MUST NOT be the durable governance record.

The durable ledger has two entity types:

1. **Review run/series records** — `schema_version`, `review_run_id`, optional series ID, review scope, `subject_scope_digest`, provenance revision/tree, review round, reviewer identity, coverage/unverified surfaces, convergence state, and the final-review marker.
2. **Finding records** — `finding_id`, namespaced `stable_key`, parent review/series ID, evidence, severity, requirement/property, disposition, status, required action, owner, resolution evidence, and disposition approval where required.

The final-review marker belongs to the review run, not to each finding. A clean final review with zero findings is therefore representable without inventing a synthetic finding, and a finding-heavy review does not duplicate run metadata across every record.

Finding IDs remain stable across rounds within their review namespace. `stable_key` uniqueness is defined as `(review_series_id, stable_key)` unless the schema explicitly declares a broader namespace. Silent global-key assumptions are forbidden.

Disposition and status are distinct. New governance-aware reviews use the versioned durable ledger prospectively. Historical prose/ERR review records remain read-only unless a deterministic source record actually contains the required fields; the migration MUST NOT infer tradeoff/risk/disposition approvals from narrative history.

A fresh final-review marker is valid when the current material review subject recomputes to the recorded `subject_scope_digest`. Recording the review run itself or unrelated landing/tracking metadata does not recursively invalidate that subject.

## 3.9 Temporary activation baseline

If required, the baseline is finite and versioned. Each item records violation ID, exact stable component/selector binding, baseline `subject_scope_digest`, creation provenance revision, owner, disposition, required action, and expiry trigger.

Creation provenance is not the freshness key. New violations fail; a baseline item's governed selector/content changing outside the recorded subject scope requires explicit review. Final strict activation requires zero active items and retirement of activation-only baseline machinery.

---

# 4. Codebase integration and closed-world enforcement boundaries

## 4.1 Assembly and dependency graph

All src/**/*.asmdef files remain the source of edges. A1 performs read-only discovery before #20 amendment and emits every asmdef/reference, cycles, explicit production/test/tooling/out-of-band classification, graph digest, proposed normative category for each production assembly, and unresolved items.

ERR-020-002/003 are resolved from that graph, not the old 31-assembly model. The approved model must define one arrow convention in text and machine data.

Full tier-direction legality remains report-only until taxonomy and semantics are approved.

## 4.2 Runtime roots and host discovery

Discovery is closed over the supported mechanisms in §3.2 by combining compiler-backed candidates with the finite A1 bootstrap declarations. Present caller reachability alone MUST NOT classify a public/plain-C# surface as production, dormant-supported, or test-only because several supported architecture surfaces may have no current production construction site.

A1 is two-pass: produce provisional mechanical candidates + bootstrap classifications, then after A2 selector/identity semantics are executable, rerun the same universe through the frozen semantics before A3. A4 may enrich final contracts but MUST NOT introduce a runtime root that was absent from the A1/A2 closed universe without reopening discovery.

Additions and removals change the applicable subject/inventory digest. Renames preserve stable component identity when selector history resolves the move. New roots fail classification completeness after A4.

## 4.3 Lifecycle and ordering

Lifecycle requirements use typed lifecycle_edges plus owners. Blocking proof requires mechanically verifiable order evidence, an execution record, or a #19-approved bounded substitute. Narrative statements alone do not satisfy the proof.

## 4.4 Static initialization

Supported static-init constructs are inventoried from compiler-backed syntax/semantic facts. Coverage includes both source-declared static constructors and compiler-generated type initialization caused by static field initializers; checking only explicit `static TypeName()` constructors is insufficient.

This is material in the current repository: boot/config and registry behavior already depends on static initialization order, including `GameplayConfigHolder`, `EventRegistry`, and `[GT]` catalogue initialization. The discovery layer therefore reports the type initializer, contributing field initializers, referenced initialization dependencies where mechanically available, and whether the type is part of an applicable ownership/order contract.

Static-init findings block only when #20 prohibits the construct or an applicable ownership/lifecycle declaration/proof is missing or inconsistent. Unsupported semantic patterns remain report-only until coverage is demonstrated.

## 4.5 Runtime public surfaces and bypasses

Regex/public-member inventory is not an absence proof.

No-bypass or no-unclassified-public-entry claims may block only for categories with a demonstrated closed world and blind-spot fixtures covering alternate factories/callers. Before that, inventory is diagnostic and semantic absence claims remain Hybrid/Judgment.

Known prohibited bypass selectors are enforceable only inside their defined closed search universe. A known-path list never proves no other bypass exists.

## 4.6 Testhosts/tooling

Testhosts are first-class when integration/lifecycle equivalence is claimed. Classification is explicit and not suffix-only. Pure low-level tests may construct calculations directly without becoming production hosts.

## 4.7 Persistence/external resources

Existing domain owners remain authoritative. Triggered proof binds failure/restore/teardown evidence to the relevant closed universe and configuration.

---

# 5. Governance tooling design

## 5.1 Responsibility and implementation split

`tools/architecture-governance` is the governance orchestrator: it resolves settled applicability, validates records, computes scoped fingerprints/closure, and evaluates evidence/review/baseline state. It does not admit properties, invent tiers, choose owners, or convert novel reviewer preferences into rules.

C# language facts required for blocking discovery are produced by a small compiler-backed extractor under `tools/architecture-governance/csharp-discovery/` (Roslyn/compiler APIs) and emitted as deterministic canonical JSON for the Python orchestrator. The extractor owns C# symbol/signature/accessibility/attribute/preprocessor/type-initialization facts; it does not own architectural classification or policy.

The Python layer MUST NOT implement a regex/hand-written C# parser and treat that output as closed-world semantic proof. Regex/grep inventories remain diagnostics only. If the compiler-backed extractor cannot run or cannot resolve the configured compilation universe, any check requiring those facts returns discovery uncertainty rather than a false pass.

Asmdef parsing remains source-JSON driven. C# semantic extraction and asmdef discovery are combined through stable assembly/symbol identities in the A2 reference semantics.

## 5.2 Versioned CLI contract

The amendment pins minimum Python version, UTF-8, repository-relative normalized paths, deterministic ordering, schema handling, malformed-input behavior, generated-input handling, full/`--changed` semantics, exact exit codes, and the C# extractor's .NET/compiler version, compilation roots/references, preprocessor symbol set, canonical output format, and failure behavior.

Required exits: 0 pass; 1 activated check failure; 2 CLI/schema error; 3 applicability/discovery/extractor uncertainty prevents a sound strict result.

--strict fails closed on unresolved applicability, unclassified closed-world surfaces, stale required evidence, or unsupported schemas.

## 5.3 Check classes

AG-CHECK-DISCOVERY: asmdefs, runtime surfaces, classifications, digests.
AG-CHECK-REGISTRY: property transitions and governance exceptions.
AG-CHECK-APPLICABILITY: trigger resolution, precedence conflicts, N/A, fallback.
AG-CHECK-CONTRACTS: typed selectors/edges/references.
AG-CHECK-EVIDENCE: proof-class schema, mechanically derived dependency closure, execution truth, scoped freshness.
AG-CHECK-ASMDEF: unknown refs, approved production/test/tooling rules, cycles, later tier direction.
AG-CHECK-REVIEW: review-run/finding state machines and fresh subject-scope final marker.
AG-CHECK-BASELINE: finite baseline, no new violations, expiry, zero-item final gate.

## 5.4 Verification boundary

Before a check blocks merge, tests cover obvious failures and false-negative boundaries, including:

- omitted plain-C# root and bootstrap-declared dormant root;
- new public/runtime factory and constructor bypass;
- implicit static initialization from a field initializer, not only explicit static constructors;
- preprocessor-dependent public/activation symbol;
- overloaded selector resolution and ambiguous selector failure;
- source move/rename that preserves stable component identity;
- lifecycle reorder and missing alternate host/testhost;
- non-`.Tests` test/tooling classification;
- incomplete mechanically required proof dependency closure;
- add/delete/rename/generated/config/asmdef/extractor-semantic change;
- `--changed` uncertainty;
- clean adversarial review with zero findings and a valid run-level final marker;
- legacy review input with no permissive default;
- required executable proof that is skipped, excluded, unavailable, not-run, or runner-failed;
- required test intersecting any quarantine/exclusion source;
- wrong-target/no-op mutation and failure injection that does not hit the claimed path;
- stale subject-scope final marker;
- nonempty final baseline.

One negative fixture per check is a floor, not sufficient evidence for absence claims. Compiler-extractor fixtures MUST include known-good and known-bad C# snippets exercising the exact syntax/semantic classes on which a blocking absence claim depends.

## 5.5 Tool semantic changes

Discovery/classification/applicability/closure/blocking semantic changes alter tool identity and stale only affected proofs unless compatibility is explicitly established.

The C# extractor carries its own semantic identity/version. A material change in compiler version, preprocessor symbol set, extraction algorithm, or canonical symbol-key behavior invalidates proof whose closure depends on those facts unless compatibility is demonstrated.

---

# 6. Code Standards #20 proposed amendment package

A3 may edit #20 only after A0–A2 pass.

## 6.1 Proposed FR rows

Append after FR-CS-073 using #20's existing columns ID | Statement | Level | Source | Mechanics §.

| ID | Statement | Level | Source | Mechanics § |
|---|---|---|---|---|
| FR-CS-074 | Every runtime-bearing component whose correctness depends on activation MUST have an explicit integration owner and exact integration point. | MUST | Governance FR-AG-021/022 | §3.5.6 |
| FR-CS-075 | Every production host/composition root in the approved runtime discovery universe MUST be classified and mechanically accounted for. | MUST | Governance FR-AG-024/026 | §3.5.6–3.5.7 |
| FR-CS-076 | Applicable runtime components MUST declare construction, activation, update/use, and shutdown/disposal ownership through typed lifecycle records, with schema-valid N/A only where a phase does not exist. | MUST | Governance FR-AG-023 | §3.5.6 |
| FR-CS-077 | Applicable alternate hosts/testhosts MUST preserve the invariant or declare an approved divergence linked to current evidence. | MUST | Governance FR-AG-024 | §3.5.7 |
| FR-CS-078 | Activation bypasses inside a mechanically closed governed surface MUST be prohibited or explicitly supported. | MUST | Governance FR-AG-025/026 | §3.5.7 |
| FR-CS-079 | Activation-capable public runtime surfaces inside an activated closed-world category MUST be classified supported, test-only, non-activating, or made non-public. | MUST | Governance FR-AG-026/027; §5.3 | §3.5.7 |
| FR-CS-080 | Static initialization participating in runtime ownership/order MUST be declared and MUST NOT bypass applicable composition/lifecycle requirements. | MUST | Governance FR-AG-023/025; §5.4 | §3.5.6–3.5.7 |
| FR-CS-081 | Blocking integration declarations MUST be mechanically resolvable to repository selectors and independently verifiable facts; unsupported semantic assertions remain non-blocking evidence. | MUST | Governance FR-AG-034/035/036A | §3.5.6–3.5.7; §5 |

§2.2 updates the 73 total to 81 and adds the architecture range without renumbering existing IDs.

## 6.2 FR-CS-046 / dependency repair

Resolve ERR-020-002/003 from A1 evidence: every production assembly classified or explicitly out-of-band; test/tooling/generated categories distinct; arrow meaning stated; machine data and diagrams identical; cycle rules explicit; unknown/new production assemblies fail classification before direction legality.

## 6.3 Exception boundary

#20 Mode 3 remains #20-owned. FR-level exceptions affect only #20 conformance and cannot waive an admitted AP, required proof, concrete correctness/integrity failure, or Governance Blocker.

## 6.4 Exhaustive #20 amendment matrix

| File | Required work |
|---|---|
| section-1.md | Authority/scope references if affected; synchronized status/version history. |
| section-2.md | FR-CS-074–081; 73→81 counts/partition/TOC; Mode 1/3 boundary; history. |
| section-3.md | §3.5.2 taxonomy/arrow repair; stable component/canonical selector semantics; typed integration/lifecycle/runtime-surface mechanics; explicit + implicit static-initialization treatment; history. |
| section-4.md | Contract/discovery relationships and diagrams; no runtime dependency. |
| section-5.md | Checklist; FR-to-verification rows 074–081; compiler-backed semantic fact source; report-only vs blocking boundaries; history. |
| section-6.md | Repair only references/counts made stale; no duplicate authority. |
| section-7.md | Activation/deferral text tied to real prerequisites. |
| section-8.md | Governance/#19 references and traceability. |
| section-9-approval-checklist.md | FR count/range, traceability, reapproval evidence, status/history. |
| appendices.md | Typed contract schema/examples; stable component/symbol identities, overload-safe selectors, rename migration; examples illustrative only. |
| outline.md / outline-mid.md / outline-detailed.md | Repair stale 73-count/section/dependency claims where current. |
| docs/specs/SPEC_INDEX.md | #20 status/version updated atomically with §9 decision. |
| docs/tracking/spec-error-log.md | Resolve ERR-020-002/003 with exact evidence. |
| docs/tracking/file-manifest.md / CHANGELOG.md | Record amendment without claiming enforcement before A8. |

Acceptance requires repo-wide sweeps for 73-count claims, FR-CS-073/074 boundaries, arrow wording, and approval/status assertions.

## 6.5 Activation boundary

#20 owns code architecture rules; governance tooling supplies objective facts; #19 owns proof/gate mechanics. A rule may be normative before its machine check is blocking, so enforcement state must be explicit.

---

# 7. Testing Strategy #19 proposed amendment package

A3 may edit #19 only after A0–A2 pass.

## 7.1 Proposed FR rows

Append after FR-TS-085 using ID | Statement | Level | Activation.

| ID | Statement | Level | Activation |
|---|---|---|---|
| FR-TS-086 | Architectural changes MUST resolve the versioned applicability manifest and record every matched trigger/requirement/proof class. | MUST | Stage 0+1 |
| FR-TS-087 | Required architectural proof MUST use the canonical versioned artifact, separate material subject identity from provenance, and bind the applicability-resolved dependency/config/tool surface by reproducible digest. | MUST | Stage 0+1 |
| FR-TS-088 | Structural proof MUST cover the complete applicability-resolved host/root/alternate/test/public universe or record an approved bounded substitute and omitted uncertainty. | MUST | Stage 0+1 |
| FR-TS-089 | Lifecycle/order proof MUST independently demonstrate required construction/activation/use/teardown/restore ordering rather than rely on declaration text. | MUST | Stage 0+1 |
| FR-TS-090 | Meaningful triggered failure paths MUST be deliberately executed where reasonably inducible and record the exact injected condition, target, expected path, executed test/command, and observed result. | MUST | Stage 0+1 |
| FR-TS-091 | Triggered mutation MUST demonstrate evidence sensitivity for the named critical invariant using an exact target and reproducible mutant/patch identity, baseline result, mutant result, and expected detector; no project-wide mutation-score target is created. | MUST | Stage 0+1 |
| FR-TS-092 | Reusable proof MUST have its complete relevant dependency universe mechanically derived/validated by proof class and stale only on material changes inside that resolved closure or its tool/config semantics. | MUST | Stage 0+1 |
| FR-TS-093 | #19 merge/review mechanics MUST consume Governance disposition/convergence state and MUST NOT rederive convergence from severity. | MUST | Stage 0 |
| FR-TS-094 | Missing, failed, stale, schema-invalid, applicability-incomplete, skipped, excluded, unavailable, not-run, or runner-failed required architectural proof MUST block merge once the gate is active unless an approved bounded substitute explicitly satisfies the obligation. | MUST | Stage 0+1 |
| FR-TS-095 | Merge-critical governance tooling MUST have known-good, known-bad, and blind-spot verification proportionate to false-positive/negative consequence. | MUST | Stage 0+1 |
| FR-TS-096 | Bounded substitutes for computationally disproportionate exhaustive proof MUST record scope, rationale, omitted uncertainty, and approval. | MUST | Stage 0+1 |

§2.2 gains FR-TS-086–096 as Architecture proof/evidence integration, mechanics in new §3.11, verification through §5.6/architecture gate. Total becomes 96.

## 7.2 Existing FR amendments

FR-TS-084: authority linkage may be FR, admitted AP, approved invariant/equivalent authority, or concrete independently established correctness/integrity failure. Novel generalized preferences become Candidate Property.

FR-TS-076: add architecture/evidence gate while preserving #16/#18 ownership.

FR-TS-077: flake quarantine cannot waive missing architecture proof or structural governance gates.

FR-TS-093 remains pointer-style; Governance owns convergence, #19 consumes it.

## 7.3 Canonical proof appendix

`appendices.md` publishes §3.7's schema and proof-class closure semantics before proof implementation. It defines pass/fail/N/A/bounded results; subject-versus-provenance identity; stable selector binding; execution states; N/A approval; exact failure-injection/mutation identity; scoped inventory/asmdef/config/tool binding; mechanically derived dependency closure; revalidation; and bounded uncertainty.

Examples MUST include a committed reusable artifact whose freshness does not depend on its own containing Git tree, an unrelated-change case that remains current, a relevant transitive dependency change that stales proof, and a required execution whose runner is skipped/excluded and therefore does not satisfy the obligation.

## 7.4 Exhaustive #19 amendment matrix

| File | Required work |
|---|---|
| section-1.md | Governance boundary references; revision status/history. |
| section-2.md | FR-TS-086–096; 85→96 partition/count; FR-TS-084/076/077; exception boundary; failure modes/history. |
| section-3.md | New §3.11 applicability/proof mechanics: subject/provenance split, proof-class closure, execution-state and revalidation semantics; no #20 ownership duplication. |
| section-4.md | Proof/test structures/interfaces only where §4 owns them. |
| section-5.md | FR-to-verification through 096; stale/missing/applicability/closure/skip-exclusion/wrong-mutant blind-spot fixtures; history. |
| section-6.md | Architecture/evidence gate topology, owning-runner/result bridge, triage, exits, no-soft-gate. |
| section-7.md | Remove deferrals only when prerequisites exist. |
| section-8.md | Governance/#20 references and traceability. |
| section-9-approval-checklist.md | FR range/count, self-check rows, reapproval status/history. |
| appendices.md | Canonical proof + closure/execution schemas/examples; TOC/history. |
| outline.md / outline-detailed.md | Repair stale 85-count/section claims where current. |
| docs/specs/SPEC_INDEX.md | #19 status/version updated atomically with §9 reapproval. |
| tests/exceptions.md / coverage-exemptions.md references | State they cannot waive Governance-required evidence/property obligations. |
| docs/tracking/file-manifest.md / CHANGELOG.md | Record amendment without enforcement claim before A8. |

Acceptance requires repo-wide sweeps for FR-TS-001…085/85-count claims, gate lists, severity-driven convergence, exception routes, and §5.6 coverage.

## 7.5 Test placement and execution ownership

Runtime architecture tests remain with owning behavior unless genuinely cross-host composition has no clean existing owner. The governance tool validates metadata/results; it does not become a mega test assembly.

Owning placement does not imply execution. Every required executable proof resolves to a runner capable of compiling/executing that assembly and a machine-readable execution record. A required test excluded by `known-failures.txt`, flake quarantine, `[Ignore]`, `Assert.Ignore`, unsupported-assembly filtering, conditional Unity-job skipping, or equivalent exclusion is unsatisfied unless #19 explicitly approves a bounded substitute.

The architecture gate MUST mechanically reject intersection between its required-test set and active quarantine/exclusion sets. Where possible it also executes the resolved required test set directly; otherwise it consumes mandatory upstream runner results with exact test identity/result binding.

Targeted governance mutation at Stage 0+1 does not depend on project-wide Stryker activation. FR-TS-091 is satisfied by the exact reproducible mutant protocol in §3.7.3; the broader Stryker.NET program may remain deferred under #19's existing Stage-1 tooling decision.

---

# 8. Adversarial-review integration

New governance-aware reviews use the durable two-entity model in §3.8: review runs/series plus findings. The state machine remains `Open → Dispositioned → Resolved/Accepted/Recorded` for findings, while the review run separately records coverage and convergence.

Before convergence behavior changes, version both schemas; define required fields per disposition; legal transitions; approval authorities; review-series/stable-key namespaces; subject-scope digest calculation; run-level final marker; prospective legacy cutover/read-only policy; and rejection of silent defaults. Every producer and consumer migrates in A6.

The current `.adversarial-review/round-*.json` and `ids.json` remain scratch inputs only. They are not treated as durable governance evidence because the directory is intentionally ignored. Historical prose/ERR records are not reverse-engineered into approvals they never encoded.

Convergence requires no open Blocker, every substantive finding validly dispositioned, current required proof, and a fresh full review **run** whose material subject digest matches the current reviewed scope. A clean final review may contain zero findings and still record convergence. Round-budget exhaustion with any gating obligation is NON-CONVERGED. Severity never independently decides convergence.

Required fixtures include Low Blocker, accepted High, residual-risk High, Candidate Property, round-cap blocker, missing evidence, clean zero-finding convergence, stale run marker, stable ID across rename/rounds, duplicate key within one series, same key in independent series, and legacy-no-default.

---

# 9. Agent workflow integration

Dependency guidance is synchronized at A1/A3 when taxonomy and arrow semantics are approved for drafting; otherwise implementation on that surface remains frozen until guidance is consistent.

Root `CLAUDE.md` and `src/CLAUDE.md` receive routing only: consult Governance plus approved #19/#20 amendments, inspect applicable contracts/rules, and run settled objective checks instead of asserting from memory.

Expanded guides document commands/examples only after commands exist.

`landing-close-out` and the orchestrator are updated in A9 so final review and recording do not recreate the provenance recursion fixed in §3.2. The landing first resolves/stages deterministic material tracking changes, computes the material subject closure, performs the final review over that subject, and records the review run against `subject_scope_digest`. Writing the review record itself is provenance output and does not alter the reviewed material digest unless that record is explicitly the artifact under review.

`landing-close-out` verifies applicable classification/contract state, applicability result, current proof, current run-level review marker, architecture audit, and that tracking does not claim report-only checks are blocking.

The orchestrator MUST NOT create APs automatically from reviewer suggestions.

---

# 10. CI and merge-gate integration

## 10.1 Architecture aggregator job

After A5/A6, the required `architecture-governance` job runs with `if: always()` (or provider-equivalent semantics) so failed/skipped dependencies cannot silently skip the architecture status itself. It runs tool/extractor self-tests; discovery/classification/applicability; registry/contract/proof/ledger validation; activated asmdef checks; scoped strict audit; and diagnostics.

The job consumes the conclusion/result artifacts of every runner needed by the applicability-resolved proof set. For each required execution it maps runner/test state to the §3.7.2 execution enum. It fails when a required execution is not `passed` unless an approved bounded substitute applies.

## 10.2 Required runner bridge

The Linux shim gate, Unity Test Runner, and any future specialized runner remain owners of executing tests in their supported universes. The governance layer does not infer execution from test existence.

Current repo-specific hazards that A8 MUST close include: `TacticalDirector.MatchClientUnity` is excluded from the Linux generated-project gate; Unity tests are conditionally skipped when licensing is unavailable; and `run-gate.sh` can filter names listed in `known-failures.txt`. The architecture job therefore validates runner capability, required-job conclusion, exact test result identity, and zero intersection between required tests and active exclusion/quarantine sources.

If a required host has no executable runner/test path, the proof remains unsatisfied until that path exists or #19 approves a bounded substitute. A report-only inventory cannot masquerade as executed lifecycle proof.

## 10.3 Required-status activation

A8 is incomplete until repository configuration requires the exact `architecture-governance` status on protected merge paths.

The implementation record defines exact workflow/job/check name, `needs`/ordering, unconditional aggregator behavior, ruleset/branch protection, failed/skipped/cancelled/unavailable/tool-crash behavior, fork/permission behavior, and diagnostic/result artifact retention.

Skipped/cancelled/unavailable is not success for the required architecture status. If settings cannot be modified by the implementation agent, that operator action is a blocking prerequisite and enforcement MUST NOT be claimed active.

## 10.4 Activation tiers and `--changed`

May block before full taxonomy: malformed records, unknown asmdef refs, production→explicit-test/tooling edges where classification is approved, cycles, schema/applicability inconsistency.

Report-only until prerequisites: full direction before #20 repair/reapproval; host completeness before A4; public/bypass absence without compiler-backed closed coverage; semantic lifecycle ownership without independent proof.

Block after A4–A8: new unclassified root; changed governed lifecycle without current proof; prohibited bypass in closed universe; missing/unsatisfied triggered proof; open Blocker; stale final review run; invalid active baseline.

Applicability resolves first. `--changed` never weakens obligation semantics and falls back to the full relevant checks whenever non-impact cannot be proven.

---

# 11. Staged implementation sequence

The A0–A9 stage model remains. Version 0.3 tightens the dependencies inside those stages so schema approval cannot precede the semantics needed to make the schemas sound.

## A0 — Adopt Governance authority

Governance v0.4 must pass its own checklist, receive explicit approval/sign-off, align status/SPEC_INDEX/version/history, and pin exact governing version plus canonical Governance content/blob digest. A provenance revision may also be recorded, but the landing MUST NOT require the document to contain its own future commit SHA. Material Governance changes re-open affected downstream prerequisites.

## A1 — Bootstrap intent plus read-only current-tree discovery

Pass 1: mechanically produce the complete asmdef graph and compiler-backed candidate universe; seed only non-inferable root/host intent through the finite `bootstrap-runtime-surfaces.json`; explicitly classify production/test/tooling/generated/out-of-band surfaces; emit provisional scoped inventory/graph digests; produce proposed taxonomy/arrow convention and ERR-020-002/003 resolution evidence. No #19/#20 normative change yet.

Gate: every mechanically discovered or bootstrap-declared assembly/surface is classified or explicitly unresolved; implicit static initialization is included; no suffix-only test inference; bootstrap contains intent only, not a copied tree.

## A2 — Freeze schemas **and executable semantics**, then rerun A1

Freeze classification, stable identity/selector grammar, applicability, integration-contract, proof/closure, property/exception, review-run/finding, and temporary-baseline schemas.

Implement the minimal deterministic reference semantics for compiler-fact ingestion, selector resolution, applicability precedence, proof closure/fingerprinting, execution-state validation, property-history comparison, and review transitions. Run representative good/bad/conflict/N/A/rename/closure fixtures.

Then rerun A1 through the frozen semantics. Gate: the closed universe is reproducible or every delta from Pass 1 is explicitly reconciled; no unresolved selector/closure semantics remain that A3 would normatively depend on.

## A3 — Amend and reapprove #19/#20 as one bundle

Bundle states remain: `PLANNED → DISCOVERED → SCHEMAS_FROZEN → AMENDMENTS_IN_REVIEW → DUAL_APPROVED → ENFORCEMENT_ELIGIBLE`.

Each spec uses its own defined status transitions. Enforcement eligibility requires both specs approved against the same Governance version/content digest and the same A2 semantic/schema baseline.

Gate: exhaustive matrices complete; ERR-020-002/003 resolved; #19 proof text uses subject/closure/execution semantics rather than containing-tree equality; fresh spec review complete.

## A4 — Seed final classifications/contracts/registries

Promote A1 bootstrap intent into final integration contracts/classifications, retire the temporary bootstrap file, and seed registries. Gate: no unclassified current runtime surface; every stable component/selector/reference resolves; ordinary rename fixtures preserve component identity; no invented exceptions.

## A5 — Productionize audit/extractor and blind-spot fixtures

Implement §5 around the frozen A2 semantics. Gate: Roslyn/compiler extractor and Python orchestrator known-good/bad/blind-spot fixtures pass; regex diagnostics cannot become strict semantic checks; implementation output matches the A2 reference semantics.

## A6 — Migrate durable review ledger and proof mechanics

Create the durable review-run/finding ledger; migrate all producers/consumers prospectively; implement proof closure/freshness and execution-record validation. Keep `.adversarial-review/` as scratch only.

Gate: clean zero-finding review can converge; legacy policy deterministic/no inferred approvals; relevant transitive add/delete/rename/config/asmdef/tool changes stale affected proof; unrelated changes do not; committed evidence does not self-invalidate from its containing commit/tree.

## A7 — Finite baseline only if required

Create §3.9 baseline only when immediate strict activation is impossible. Every item is subject-scope/selector-bound with owner/action/expiry; new violations fail. Final strict activation requires zero active items.

## A8 — Activate runner bridge, CI aggregator, and required merge status

Add the `architecture-governance` aggregator plus actual required-status/ruleset configuration. Gate: representative violation blocks merge; aggregator still reports failure when a needed job is failed/skipped/cancelled/unavailable; required tests cannot be quarantined/excluded; Unity-only proof cannot pass via the Linux gate; exact required status is configured.

## A9 — Synchronize guides and final strict review

Update guidance/workflow ordering to actual approved authority/commands. Stage deterministic material tracking changes, compute the final material subject digest, run strict audit and fresh full adversarial review over that subject, require zero active baseline items, then record the run-level final marker without recursively changing the subject digest.

Production architecture remediation begins only after the applicable A-stage prerequisites for that rule are satisfied.

---

# 12. Detailed change-impact matrix

| Area | New files | Modified files | Runtime behavior |
|---|---|---|---|
| Governance state | property-registry.json, integration-contracts.json, exceptions.json, runtime-surface-classifications.json, review-ledger.json; temporary bootstrap-runtime-surfaces.json during A1 only | project governance pointer/history only if needed | None |
| Architecture tooling | tools/architecture-governance/* including compiler-backed csharp-discovery extractor | none initially | None |
| Review tooling | durable review-ledger + tests/fixtures for run/finding state | adversarial-review SKILL.md, findings.py | None |
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

- focused governance orchestrator plus a small compiler-backed C# fact extractor;
- no hand-written C# parser and no runtime governance framework;
- no duplicate source inventory;
- property/exception retirement;
- no mandatory mutation-score program;
- no recursive meta-checker.

## FM-GI-11 — Evidence invalidates itself when committed

Prevention:

- material subject digest separated from provenance/artifact identity;
- committed evidence never freshness-keys on its own containing commit/tree.

## FM-GI-12 — A required architecture test exists but never executes

Prevention:

- explicit execution-state model;
- runner-capability/result binding;
- required-test/quarantine-exclusion intersection fails;
- `architecture-governance` aggregator runs even when dependencies fail/skip.

## FM-GI-13 — Closed-world C# proof rests on a regex parser

Prevention:

- compiler-backed extractor for blocking semantic facts;
- regex/grep remains diagnostic;
- extractor unavailable/uncertain fails closed.

## FM-GI-14 — A clean final review cannot record convergence

Prevention:

- review-run records separate from findings;
- run-level final marker supports zero-finding reviews;
- durable ledger separate from ignored scratch state.
---

# 15. Acceptance gates for completed governance integration

## Authority
- [ ] Governance approved/pinned to exact version and canonical content/blob digest; revision is provenance only.
- [ ] #19/#20 dual-approved against the same Governance/A2 semantic baseline.
- [ ] D1–D4 remain excluded.

## Discovery, identity, and applicability
- [ ] Complete asmdef graph generated.
- [ ] Compiler-backed C# discovery covers configured public/lifecycle/factory and explicit+implicit static-init mechanisms.
- [ ] A1 bootstrap contains only non-inferable intent and is retired into final contracts at A4.
- [ ] Every assembly/runtime surface explicitly classified without suffix-only inference.
- [ ] Stable component identity survives ordinary file/type moves through selector history.
- [ ] Selector grammar distinguishes overloads and fails ambiguous/missing bindings.
- [ ] Applicability resolver and proof-closure resolver are deterministic/conflict-tested/fail-closed.
- [ ] `--changed` cannot weaken obligations.

## Contracts/public/bypass
- [ ] Blocking contract assertions use typed independently verifiable selectors/edges.
- [ ] Narrative semantic claims are not treated as machine proof.
- [ ] Public/bypass absence blocks only from compiler-backed demonstrated closed universes.
- [ ] Alternate hosts/testhosts classified.

## Proof/freshness
- [ ] Canonical schema approved after executable A2 semantics exist.
- [ ] Committed artifacts separate subject digest from provenance and do not require equality with their containing commit/tree.
- [ ] Each proof class has a mechanically defined dependency-closure algorithm.
- [ ] Relevant transitive source/config/asmdef/tool changes stale affected proof; unrelated changes leave it valid.
- [ ] Execution states distinguish passed/failed/skipped/excluded/unavailable/not-run/runner-failed; only passed satisfies ordinary required execution.
- [ ] Failure-injection/mutation records identify exact perturbation, target, command/test, observed result, and restoration.
- [ ] N/A/bounded substitutes follow explicit rules/approval.

## Review/baseline
- [ ] Property-history immutability compares against a trusted prior registry rather than a self-containing future commit.
- [ ] Durable review ledger has separate run and finding entities.
- [ ] `.adversarial-review/` remains scratch only.
- [ ] Clean zero-finding final review can record a valid run-level marker.
- [ ] Legacy records are read-only unless deterministically convertible; no inferred approvals.
- [ ] Low Blocker gates; accepted High does not gate by severity.
- [ ] Round cap + blocker = NON-CONVERGED.
- [ ] Final marker binds material subject scope, not its own containing tree.
- [ ] Temporary baseline finite and subject-scoped; final strict gate requires zero active items.

## Tool/CI/guidance
- [ ] Compiler-backed extractor and Python orchestrator identities/semantics pinned and verified with blind-spot fixtures.
- [ ] Regex diagnostics cannot satisfy blocking semantic absence proof.
- [ ] Exact architecture aggregator status required by merge protection/ruleset.
- [ ] Aggregator runs even when dependencies fail/skip and maps required runner conclusions to execution truth.
- [ ] Required tests cannot be hidden by `known-failures.txt`, flake quarantine, Ignore/Assert.Ignore, unsupported-assembly filtering, or skipped Unity jobs.
- [ ] Representative violation demonstrably blocks merge.
- [ ] Guidance/workflow ordering synchronized so final review record does not recursively stale itself.

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
| 0.3 | August 28, 2026 | — | End-to-end implementation hardening: separates evidence subject identity from Git/artifact provenance; removes self-referential governance/property pins; closes A1/A4 root bootstrap; freezes executable selector/identity/applicability/closure semantics at A2; requires compiler-backed C# discovery including implicit type initialization; defines stable component/symbol identities and selector history; derives proof-class dependency closure; records exact execution/failure/mutation truth; splits durable review runs from findings; bridges owning tests to mandatory runner results/CI aggregation; preserves A0–A9, Governance authority split, and ERR-020-002/003 staging. No #19/#20 normative files or implementation changed. |
| 0.2 | August 28, 2026 | — | Hostile-review hardening: A0 Governance adoption; A1 discovery; A2 schema freeze; A3 dual #19/#20 reapproval; closed-world classification; deterministic applicability; typed contracts; complete proof binding; versioned ledger; exception-boundary correction; exhaustive amendment matrices; required-status CI; finite baseline; A0–A9 sequencing. No #19/#20 normative files or implementation changed. |
| 0.1 | August 27, 2026 | — | Initial detailed integration map for Project Architecture Governance v0.4. Maps #19/#20 amendments, runtime/code surfaces, governance state records, audit tooling, adversarial-review migration, CI activation, evidence invalidation, and staged implementation. Explicitly excludes the frozen D1–D4 remediation supplement. |
