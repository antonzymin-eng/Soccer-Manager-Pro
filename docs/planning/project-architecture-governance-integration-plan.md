# Project Architecture Governance — Integration Map and Implementation Plan

**Document Class:** Integration design and implementation plan  
**Status:** Draft — implementation planning; no production code implemented by this document  
**Version:** 0.2  
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

## 0.5 Amendment precedence and activation prerequisites

Version 0.2 hardens implementation sequencing without reopening the architectural decisions in project-architecture-governance.md.

The following rules override earlier sequencing in this plan:

1. Governance v0.4 is currently Draft. It is design input until an explicit adoption gate records approval, completed self-checklist, SPEC_INDEX/status alignment, and the exact governing commit/version.
2. Dependency-direction enforcement MUST NOT become blocking until a read-only current-tree discovery pass has produced the complete asmdef graph, every assembly has an explicit production/test/tooling/out-of-band classification, arrow semantics are fixed, and ERR-020-002 / ERR-020-003 are resolved.
3. Machine-readable schemas for discovery classification, applicability, integration contracts, proof, finding ledgers, and any temporary baseline MUST be frozen before #19/#20 normative amendments are finalized.
4. #19 and #20 are amended and reapproved as one coordinated governance-integration bundle. Enforcement eligibility requires both amendments approved against the same repository base and governance version.
5. No checker may make an absence claim blocking unless the relevant search universe is closed and mechanically enumerated. Known-path lists and naming heuristics are not proof of absence.
6. No CI job is a merge gate merely because it exists. Required-status configuration and skipped/cancelled/unavailable behavior are part of activation.
7. A temporary baseline is permitted only as a finite commit-bound migration artifact and MUST be mechanically empty at final strict activation.

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

## 3.2 Closed-world runtime-surface classification

Create docs/tracking/architecture-governance/runtime-surface-classifications.json.

This stores classification decisions for every surface emitted by the generated discovery universe and binds them to repository_commit, repository_tree, discovery_tool_identity, discovery_roots, inventory_digest, and asmdef_graph_digest.

Each record contains surface_id, kind, source_path, symbol, assembly, classification, contract_id when required, and rationale when not contract-required.

Allowed classifications: production-runtime-root; contracted-child; test-only; tooling-only; generated-or-external; non-runtime-bearing.

The discovery universe MUST include all asmdefs, Unity lifecycle/initialization entry surfaces, explicit static constructors, conventional Main entry points, supported serialized/factory activation surfaces, typed plain-C# roots, testhosts, and tooling assemblies.

Test classification MUST NOT rely on a .Tests suffix alone. Assembly metadata, path, platform/define constraints, references, and explicit classification are considered. TacticalDirector.TestingStrategy or any similar assembly must be explicitly classified.

Strict mode fails when a newly discovered surface is unclassified after initial baseline acceptance.

## 3.3 Property registry

property-registry.json adds schema_version, decision_id, decision_actor, decision_commit, transition_from, transition_to, decision_rationale, and revalidation_history. The validator enforces legal transitions and immutable decision history but does not judge admission quality.

## 3.4 Typed integration ownership contracts

integration-contracts.json uses typed fields: component_id; source_selector; assembly; owning_host_surface_id; composition_root_selector; construction_edges; lifecycle_edges; activation_phase; update_use_owner; shutdown_disposal_owner or justified_na; testhost_surface_ids; alternate_supported_surface_ids; public_activation_surfaces; prohibited_bypass_selectors; static_initialization_selectors; requirement_refs; evidence_refs.

Narrative fields MAY explain intent but cannot satisfy a blocking mechanical ownership proof.

Blocking is allowed only for assertions independently verifiable through typed selectors/edges, closed-universe absence checks, or current #19 proof. Unsupported semantic claims remain Hybrid/Judgment and report-only.

## 3.5 Applicability manifest and deterministic resolver

Create docs/tracking/architecture-governance/applicability-rules.json.

Each rule contains rule_id, selectors, trigger_ref, requirement_refs, proof_classes, gate_classes, allowed_na_reasons, precedence, and fallback_scope.

All matches are evaluated. Schema-defined specificity controls precedence; equal-precedence conflicts fail. N/A is valid only for an enumerated reason and required approval reference. --changed optimizes after applicability is resolved and falls back to the full relevant universe whenever non-impact cannot be proven. Unresolved applicability fails strict mode.

Representative fixtures must prove identical changes resolve to identical obligation sets.

## 3.6 Exception routing and precedence

Governance exceptions remain property-oriented exactly as Governance §7 defines them. This integration MUST NOT route FR-CS or FR-TS waivers directly into exceptions.json unless the affected obligation is an admitted AP that explicitly allows an exception.

Existing #19/#20 exception mechanisms remain owner-specific. They cannot waive an admitted AP, missing required evidence, concrete correctness/integrity failure, or Governance Blocker.

## 3.7 Canonical proof artifact schema

The #19 amendment MUST land the canonical schema before proof workflow or CI gating.

Reusable proof records require: schema_version; proof_id; proof_class; requirement/property refs; applicability_rule_ids; result pass/fail/na/bounded; N/A or bounded justification/approval; repository_commit; repository_tree; inventory_digest; asmdef_graph_digest; relevant configuration fingerprints; tool identities; dependency selectors/fingerprints; execution records; conditional failure-injection and mutation results; created metadata; revalidation history.

Proof-class validation is conditional. A triggered mutation/failure proof without its result is invalid. Structural proof without required closed-world inventory binding is invalid.

Freshness must detect additions, deletions, renames, generated/config changes, new roots, asmdef changes, and checker-semantic changes inside the proof universe. The audit validates dependency coverage against applicability; it does not trust a self-declared dependency list merely because one exists.

## 3.8 Versioned adversarial-review ledger

New finding records carry schema_version, finding_id, stable_key, review_scope, reviewed_commit/tree, review_round, reviewer_identity, evidence, severity, requirement_or_property, disposition, status, required_action, owner, resolution_evidence, disposition_approval where required, and final_review_marker.

Disposition and status are distinct. Legacy ledgers are deterministically migrated or read-only; silent permissive defaults are forbidden. A fresh final-review marker is valid only for the current tree and scope.

## 3.9 Temporary activation baseline

If required, the baseline is finite and versioned. Each item records violation_id, exact selector, baseline_commit, inventory_digest, owner, disposition, required_action, and expiry_trigger.

New violations fail. Changed baseline violations require explicit review. Final strict activation requires zero active items and retirement of activation-only baseline machinery.

---

# 4. Codebase integration and closed-world enforcement boundaries

## 4.1 Assembly and dependency graph

All src/**/*.asmdef files remain the source of edges. A1 performs read-only discovery before #20 amendment and emits every asmdef/reference, cycles, explicit production/test/tooling/out-of-band classification, graph digest, proposed normative category for each production assembly, and unresolved items.

ERR-020-002/003 are resolved from that graph, not the old 31-assembly model. The approved model must define one arrow convention in text and machine data.

Full tier-direction legality remains report-only until taxonomy and semantics are approved.

## 4.2 Runtime roots and host discovery

Discovery is closed over the supported mechanisms in §3.2. Plain C# roots syntax cannot infer safely are included through typed contract selectors and become part of the classified universe.

Additions, deletions, and renames alter the inventory digest. New roots fail classification completeness after A4.

## 4.3 Lifecycle and ordering

Lifecycle requirements use typed lifecycle_edges plus owners. Blocking proof requires mechanically verifiable order evidence, an execution record, or a #19-approved bounded substitute. Narrative statements alone do not satisfy the proof.

## 4.4 Static initialization

Supported static-init constructs are inventoried. They block only when #20 prohibits the construct or an applicable ownership/lifecycle declaration is missing/inconsistent. Unsupported patterns remain report-only until coverage is demonstrated.

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

## 5.1 Responsibility

tools/architecture-governance discovers facts, resolves settled applicability, validates records, and evaluates evidence freshness. It does not admit properties, invent tiers, choose owners, or convert novel reviewer preferences into rules.

## 5.2 Versioned CLI contract

The amendment pins minimum Python version, UTF-8, repository-relative normalized paths, deterministic ordering, schema handling, malformed-input behavior, generated-input handling, full/--changed semantics, and exact exit codes.

Required exits: 0 pass; 1 activated check failure; 2 CLI/schema error; 3 applicability/discovery uncertainty prevents a sound strict result.

--strict fails closed on unresolved applicability, unclassified closed-world surfaces, stale required evidence, or unsupported schemas.

## 5.3 Check classes

AG-CHECK-DISCOVERY: asmdefs, runtime surfaces, classifications, digests.
AG-CHECK-REGISTRY: property transitions and governance exceptions.
AG-CHECK-APPLICABILITY: trigger resolution, precedence conflicts, N/A, fallback.
AG-CHECK-CONTRACTS: typed selectors/edges/references.
AG-CHECK-EVIDENCE: proof-class schema, scope coverage, freshness.
AG-CHECK-ASMDEF: unknown refs, approved production/test/tooling rules, cycles, later tier direction.
AG-CHECK-REVIEW: finding state machine and current-tree final marker.
AG-CHECK-BASELINE: finite baseline, no new violations, expiry, zero-item final gate.

## 5.4 Verification boundary

Before a check blocks merge, tests cover obvious failures and false-negative boundaries, including omitted plain-C# root, new public/runtime factory, constructor bypass, lifecycle reorder, missing alternate host, non-.Tests test/tooling classification, incomplete proof dependencies, add/delete/rename, generated/config change, --changed uncertainty, legacy finding defaults, stale final marker, and nonempty final baseline.

One negative fixture per check is a floor, not sufficient evidence for absence claims.

## 5.5 Tool semantic changes

Discovery/classification/applicability/blocking semantic changes alter tool identity and stale affected proofs unless compatibility is established.

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
| section-3.md | §3.5.2 taxonomy/arrow repair; new typed integration/lifecycle/runtime-surface mechanics; history. |
| section-4.md | Contract/discovery relationships and diagrams; no runtime dependency. |
| section-5.md | Checklist; FR-to-verification rows 074–081; report-only vs blocking; history. |
| section-6.md | Repair only references/counts made stale; no duplicate authority. |
| section-7.md | Activation/deferral text tied to real prerequisites. |
| section-8.md | Governance/#19 references and traceability. |
| section-9-approval-checklist.md | FR count/range, traceability, reapproval evidence, status/history. |
| appendices.md | Typed contract schema/examples; examples illustrative only. |
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
| FR-TS-087 | Required architectural proof MUST use the canonical versioned artifact and bind repository/tree/inventory/config/tool identity. | MUST | Stage 0+1 |
| FR-TS-088 | Structural proof MUST cover the complete applicability-resolved host/root/alternate/test/public universe or record an approved bounded substitute and omitted uncertainty. | MUST | Stage 0+1 |
| FR-TS-089 | Lifecycle/order proof MUST independently demonstrate required construction/activation/use/teardown/restore ordering rather than rely on declaration text. | MUST | Stage 0+1 |
| FR-TS-090 | Meaningful triggered failure paths MUST be deliberately executed where reasonably inducible. | MUST | Stage 0+1 |
| FR-TS-091 | Triggered mutation MUST demonstrate evidence sensitivity for the named critical invariant; no project-wide mutation-score target is created. | MUST | Stage 0+1 |
| FR-TS-092 | Reusable proof MUST declare and validate a complete relevant dependency universe and stale on material add/delete/rename/config/tool-semantic changes inside it. | MUST | Stage 0+1 |
| FR-TS-093 | #19 merge/review mechanics MUST consume Governance disposition/convergence state and MUST NOT rederive convergence from severity. | MUST | Stage 0 |
| FR-TS-094 | Missing, failed, stale, schema-invalid, or applicability-incomplete required architectural proof MUST block merge once the gate is active. | MUST | Stage 0+1 |
| FR-TS-095 | Merge-critical governance tooling MUST have known-good, known-bad, and blind-spot verification proportionate to false-positive/negative consequence. | MUST | Stage 0+1 |
| FR-TS-096 | Bounded substitutes for computationally disproportionate exhaustive proof MUST record scope, rationale, omitted uncertainty, and approval. | MUST | Stage 0+1 |

§2.2 gains FR-TS-086–096 as Architecture proof/evidence integration, mechanics in new §3.11, verification through §5.6/architecture gate. Total becomes 96.

## 7.2 Existing FR amendments

FR-TS-084: authority linkage may be FR, admitted AP, approved invariant/equivalent authority, or concrete independently established correctness/integrity failure. Novel generalized preferences become Candidate Property.

FR-TS-076: add architecture/evidence gate while preserving #16/#18 ownership.

FR-TS-077: flake quarantine cannot waive missing architecture proof or structural governance gates.

FR-TS-093 remains pointer-style; Governance owns convergence, #19 consumes it.

## 7.3 Canonical proof appendix

appendices.md publishes §3.7's schema before proof implementation. It defines pass/fail/na/bounded, N/A approval, execution identity, conditional mutation/failure fields, inventory/tree/config/tool binding, dependency coverage, revalidation, and bounded uncertainty.

## 7.4 Exhaustive #19 amendment matrix

| File | Required work |
|---|---|
| section-1.md | Governance boundary references; revision status/history. |
| section-2.md | FR-TS-086–096; 85→96 partition/count; FR-TS-084/076/077; exception boundary; failure modes/history. |
| section-3.md | New §3.11 applicability/proof mechanics; no #20 ownership duplication. |
| section-4.md | Proof/test structures/interfaces only where §4 owns them. |
| section-5.md | FR-to-verification through 096; stale/missing/applicability/blind-spot fixtures; history. |
| section-6.md | Architecture/evidence gate topology, triage, exits, no-soft-gate. |
| section-7.md | Remove deferrals only when prerequisites exist. |
| section-8.md | Governance/#20 references and traceability. |
| section-9-approval-checklist.md | FR range/count, self-check rows, reapproval status/history. |
| appendices.md | Canonical proof schema/examples; TOC/history. |
| outline.md / outline-detailed.md | Repair stale 85-count/section claims where current. |
| docs/specs/SPEC_INDEX.md | #19 status/version updated atomically with §9 reapproval. |
| tests/exceptions.md / coverage-exemptions.md references | State they cannot waive Governance-required evidence/property obligations. |
| docs/tracking/file-manifest.md / CHANGELOG.md | Record amendment without enforcement claim before A8. |

Acceptance requires repo-wide sweeps for FR-TS-001…085/85-count claims, gate lists, severity-driven convergence, exception routes, and §5.6 coverage.

## 7.5 Test placement

Runtime architecture tests remain with owning behavior unless genuinely cross-host composition has no clean existing owner. The governance tool validates metadata; it does not become a mega test assembly.

---

# 8. Adversarial-review integration

New findings use a versioned state machine: Open → Dispositioned → Resolved/Accepted/Recorded. Disposition and status are separate.

Before convergence behavior changes, version the schema, define required fields per disposition, legal transitions, approval authorities, review scope/commit/tree binding, final-review marker, legacy conversion/read-only policy, and rejection of silent defaults. Every producer and consumer migrates in one stage.

Convergence requires no open Blocker, every substantive finding validly dispositioned, current required proof, and a fresh full review marker for the current tree/scope. Round-budget exhaustion with any gating obligation is NON-CONVERGED. Severity never independently decides convergence.

Required fixtures include Low Blocker, accepted High, residual-risk High, Candidate Property, round-cap blocker, missing evidence, stale final marker, stable ID, legacy-no-default, and duplicate key.

---

# 9. Agent workflow integration

Dependency guidance is synchronized at A1/A3 when taxonomy and arrow semantics are approved for drafting; otherwise implementation on that surface remains frozen until guidance is consistent.

Root CLAUDE.md and src/CLAUDE.md receive routing only: consult Governance plus approved #19/#20 amendments, inspect applicable contracts/rules, and run settled objective checks instead of asserting from memory.

Expanded guides document commands/examples only after commands exist.

landing-close-out verifies applicable classification/contract state, applicability result, current proof, review marker, architecture audit, and that tracking does not claim report-only checks are blocking.

The orchestrator MUST NOT create APs automatically from reviewer suggestions.

---

# 10. CI and merge-gate integration

## 10.1 Architecture job

After A5/A6, architecture-governance runs tool self-tests; discovery/classification/applicability; registry/contract/proof/ledger validation; activated asmdef checks; strict current-tree audit; diagnostics.

## 10.2 Required-status activation

A8 is incomplete until repository configuration requires the exact architecture-governance status on protected merge paths.

The implementation record defines exact workflow/job/check name, needs/order, ruleset/branch protection, skipped/cancelled/unavailable/tool-crash behavior, fork/permission behavior, and diagnostic artifact retention.

Skipped/cancelled/unavailable is not success for a required check. If settings cannot be modified by the implementation agent, that operator action is a blocking prerequisite and enforcement MUST NOT be claimed active.

## 10.3 Activation tiers

May block before full taxonomy: malformed records, unknown asmdef refs, production→explicit-test/tooling edges where classification is approved, cycles, schema/applicability inconsistency.

Report-only until prerequisites: full direction before #20 repair/reapproval; host completeness before A4; public/bypass absence without closed coverage; semantic lifecycle ownership without independent proof.

Block after A4–A8: new unclassified root; changed governed lifecycle without proof; prohibited bypass in closed universe; missing triggered proof; open Blocker; stale final review; invalid active baseline.

## 10.4 --changed

Applicability resolves first. --changed never weakens obligation semantics and falls back to full relevant checks whenever non-impact cannot be proven.

---

# 11. Staged implementation sequence

The previous G0–G8 order is replaced by A0–A9.

## A0 — Adopt Governance authority

Governance v0.4 must pass its own checklist, receive explicit approval/sign-off, align status/SPEC_INDEX/version/history, and pin exact governing commit/version. Material Governance changes re-open affected downstream prerequisites.

## A1 — Read-only current-tree discovery

Produce complete asmdef graph; supported runtime-surface universe; explicit production/test/tooling/generated/out-of-band classifications; inventory/graph digests; proposed taxonomy/arrow convention; ERR-020-002/003 resolution evidence. No #19/#20 normative change yet.

Gate: every discovered assembly/surface classified or explicitly unresolved; no suffix-only test inference; artifact bound to commit/tree/tool.

## A2 — Freeze machine schemas

Freeze classifications, applicability, contracts, proof, property/exception, finding ledger, and temporary-baseline schemas.

Gate: representative good/bad records and conflicts/N/A/transitions behave deterministically.

## A3 — Amend and reapprove #19/#20 as one bundle

Bundle states: PLANNED → DISCOVERED → SCHEMAS_FROZEN → AMENDMENTS_IN_REVIEW → DUAL_APPROVED → ENFORCEMENT_ELIGIBLE.

Each spec uses its own defined status transitions, but section headers, §9, SPEC_INDEX, version history, and changelog must agree. Enforcement eligibility requires both specs approved against the same Governance version/base.

Gate: exhaustive matrices complete; counts/traceability/checklists/outlines/status synchronized; ERR-020-002/003 resolved; fresh spec review complete.

## A4 — Seed classifications/contracts/registries

Create state from A1. Gate: no unclassified current runtime surface, every selector/reference resolves, no invented exceptions.

## A5 — Implement audit and blind-spot fixtures

Implement §5. Gate: known-good/bad and blind-spot fixtures pass; report-only heuristics cannot accidentally become strict.

## A6 — Migrate review ledger and proof mechanics

Migrate all finding producers/consumers and implement proof validation/freshness. Gate: legacy policy deterministic; disposition fixtures correct; add/delete/rename/config/tool changes stale affected proof; unrelated changes do not.

## A7 — Finite baseline only if required

Create §3.9 baseline only when immediate strict activation is impossible. Every item is commit/inventory-bound with owner/action/expiry; new violations fail.

## A8 — Activate CI and required merge status

Add workflow plus actual required-status/ruleset configuration. Gate: representative violation blocks merge; skipped/cancelled/unavailable cannot pass; required check is actually configured.

## A9 — Synchronize guides and final strict review

Update guidance to actual approved authority/commands. Run strict current-tree audit, require zero active baseline items, perform fresh full adversarial review, and record final review marker.

Production architecture remediation begins only after the applicable A-stage prerequisites for that rule are satisfied.

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

# 15. Acceptance gates for completed governance integration

## Authority
- [ ] Governance approved/pinned to exact version/commit.
- [ ] #19/#20 dual-approved against same Governance/base state.
- [ ] Headers, §9, SPEC_INDEX, histories, traceability, counts, changelog agree.
- [ ] D1–D4 remain excluded.

## Discovery/applicability
- [ ] Complete asmdef graph generated.
- [ ] Every assembly explicitly classified without suffix-only inference.
- [ ] Activated runtime-surface universe is closed over supported mechanisms.
- [ ] New roots fail until classified.
- [ ] Applicability resolver deterministic/conflict-tested/fail-closed.
- [ ] --changed cannot weaken obligations.

## Contracts/public/bypass
- [ ] Blocking contract assertions use typed independently verifiable selectors/edges.
- [ ] Narrative semantic claims are not treated as machine proof.
- [ ] Public/bypass absence blocks only in demonstrated closed universes.
- [ ] Alternate hosts/testhosts classified.

## Proof
- [ ] Canonical schema approved before proof gating.
- [ ] Triggered classes require class-specific fields.
- [ ] Proof binds tree, inventory/graph, relevant config, tool identity.
- [ ] Dependency coverage validated against applicability.
- [ ] Add/delete/rename/new-root/config/tool changes invalidate affected proof.
- [ ] Unrelated changes leave unaffected proof valid.
- [ ] N/A/bounded substitutes follow explicit rules/approval.

## Review/baseline
- [ ] Finding schema/state machine versioned; disposition ≠ status.
- [ ] Legacy records explicitly migrated or read-only.
- [ ] Low Blocker gates; accepted High does not gate by severity.
- [ ] Round cap + blocker = NON-CONVERGED.
- [ ] Final-review marker bound to current tree/scope.
- [ ] Temporary baseline finite; final strict gate requires zero active items.

## Tool/CI/guidance
- [ ] CLI/environment/path/schema/exit semantics pinned.
- [ ] Blind-spot fixtures cover false-negative boundaries.
- [ ] Report-only checks cannot block.
- [ ] Exact architecture check required by merge protection/ruleset.
- [ ] Skipped/cancelled/unavailable required check is not success.
- [ ] Representative violation demonstrably blocks merge.
- [ ] Guidance synchronized and contains routing, not duplicate authority.

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
| 0.2 | August 28, 2026 | — | Hostile-review hardening: A0 Governance adoption; A1 discovery; A2 schema freeze; A3 dual #19/#20 reapproval; closed-world classification; deterministic applicability; typed contracts; complete proof binding; versioned ledger; exception-boundary correction; exhaustive amendment matrices; required-status CI; finite baseline; A0–A9 sequencing. No #19/#20 normative files or implementation changed. |
| 0.1 | August 27, 2026 | — | Initial detailed integration map for Project Architecture Governance v0.4. Maps #19/#20 amendments, runtime/code surfaces, governance state records, audit tooling, adversarial-review migration, CI activation, evidence invalidation, and staged implementation. Explicitly excludes the frozen D1–D4 remediation supplement. |
