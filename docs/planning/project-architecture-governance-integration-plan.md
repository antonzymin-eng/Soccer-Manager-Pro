# Project Architecture Governance — Integration Map and Implementation Plan

**Document Class:** Integration design and implementation plan  
**Status:** Draft — implementation planning; no production code implemented by this document  
**Version:** 0.45\
**Created:** August 27, 2026  
**Last Updated:** September 4, 2026\
**Governing authority:** docs/planning/project-architecture-governance.md v0.10 (v0.4 when this plan was created)\
**Primary downstream specifications:** Testing Strategy & Framework #19; Code Standards & Style Guide #20  
**Related project authorities:** Master Development Plan; adversarial-review process; root and src agent guides  
**Review/authoring base:** branch docs/round-2-architecture-remediation-design at commit 12abb982c45f667fb90311320997b6d7f00dc8cf (provenance only; not an evidence-freshness key)

---

# 0. Purpose, scope, and authority

## 0.1 Purpose

This document maps the Project Architecture Governance Specification into the existing System XI repository.

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
| Dependency direction | Code Standards #20 | Reuses the existing assembly-tier checker; adds machine-readable evidence and activation mechanics without a second parser |
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

- which host owns a runtime-bearing component;
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

Version 0.5 corrects the A1 rollout against the live repository while preserving the settled governance architecture and the two-track structural/behavioral model. ERR-020-002 and ERR-020-003 were already resolved on August 17, 2026; Code Standards #20 §3.5.2 already contains the complete ten-tier/out-of-band dependency model; and `tools/assembly-tier-check.py` already enforces that model inside the `Spec hygiene checks` CI job. A1 therefore consolidates machine-readable evidence into that existing checker and separates CI execution from merge-protection activation. It does not reopen the architectural decisions in project-architecture-governance.md.

The following rules override earlier sequencing in this plan:

1. Governance v0.10 is **Approved and authoritative under A0**. Before A0 closed it was design input only. The adoption gate requires human approval, the §9.1–§9.6 self-check, the current fresh-review/finding conditions, and the exact approved Governance content digest. SPEC_INDEX registration is not an A0 prerequisite; downstream registration/alignment remains owned by the later landing stage that requires it. A Git revision MAY be recorded as provenance but is not required to be self-embedded in the approved artifact.

   **"Completed self-checklist" means Governance §9.1–§9.6 only, amended in v0.7.** The earlier wording said "completed self-checklist" without qualification, which read as all 59 boxes including §9.7. That is circular and was never the intent: §9.7 is headed *"Before this specification is considered **fully adopted**"* and asks for #19/#20 amendments, the Master Development Plan pointer, adversarial-review reconciliation, the property registry, finding-schema tooling, and inventory tooling — work this plan itself assigns to A3–A9. Requiring it at A0 would make Governance's authority depend on stages that cannot start until Governance has authority. The two gates are therefore distinct and are named separately below.
2. Dependency-direction policy is already approved and mechanically enforced by `tools/assembly-tier-check.py` inside `Spec hygiene checks`. A1 MUST reuse that checker rather than create a second §3.5.2 parser. Its machine report covers the complete `src/**/*.asmdef` universe, classifies production/test/out-of-band assemblies from existing #20 rules, reports unresolved items explicitly, and does not publish a fictitious tooling count when no tooling asmdef exists in that source universe.
3. Machine-readable schemas for discovery classification, applicability, integration contracts, proof, finding ledgers, and any temporary baseline MUST be frozen before #19/#20 normative amendments are finalized.
4. #19 and #20 are amended and reapproved as one coordinated governance-integration bundle. Enforcement eligibility requires both amendments approved against the same repository base and governance version.
5. No checker may make an absence claim blocking unless the relevant search universe is closed and mechanically enumerated. Known-path lists and naming heuristics are not proof of absence.
6. No CI job is a merge gate merely because it exists. Required-status configuration and skipped/cancelled/unavailable behavior are part of activation.
7. A temporary baseline is permitted only as a finite migration artifact and MUST be mechanically empty at final strict activation.
8. Committed governance artifacts MUST separate the material subject they prove from the Git commit/tree that happens to contain the evidence record. A committed artifact MUST NOT require equality with its own containing commit/tree as a freshness condition.
9. A1 remains deliberately asmdef-only and requires no Roslyn extractor, governance schema freeze, or #19/#20 governance amendment. It extends the existing `assembly-tier-check.py` evidence surface rather than introducing another parser. Compiler-backed runtime/root reachability occurs only after selector/identity semantics exist; bootstrap declarations then close what compiler facts cannot infer.
10. Merge-blocking C# symbol/public-surface/static-initialization discovery MUST consume compiler-backed semantic facts. The Python governance tool may orchestrate those facts, but MUST NOT implement a regex or hand-written C# parser and call the result closed-world proof.
11. A2 freezes not only JSON shapes but the executable identity, selector, applicability, dependency-closure, and freshness semantics needed to interpret them. Those semantics MUST pass representative fixtures before A3 reapproval.
12. Required executable proof is satisfied only by an explicit successful execution state. Skipped, excluded, unavailable, not-run, or runner-failed evidence does not satisfy a required proof unless #19 permits and records a bounded substitute.
13. Structural classification and activation state are orthogonal. A component remains a production runtime-bearing component even when deliberately disabled or not yet integrated.
14. `intentionally-disabled` is valid only when its disabled state is independently machine-verifiable from a resolvable source/config selector plus a typed expected predicate/value; prose alone cannot create a suppression.
15. Static discovery covers Class A dormancy (exists but has no production activation/reachability). It MUST NOT claim to prove Class B gate firing. Runtime gate/trigger instrumentation remains owned by the component/domain and governance consumes its evidence when an applicable rule requires it.
16. The objective asmdef check already executes inside `Spec hygiene checks`. A1c is therefore an activation/configuration step: verify merge-protection state and enable the existing required status where enforcement is disabled; it MUST NOT create a parallel `architecture-asmdef` status unless the existing status cannot express the approved requirement.
17. Changes to declared `[GT]`/calibration tuning surfaces are prohibited while the owning component is `intentionally-disabled`, `pending-integration`, or `unresolved`, unless the approved exception path explicitly authorizes the change.

This document remains an implementation plan and does not itself modify approved #19/#20 requirements. Governance v0.10 approval is recorded by the completed A0 gate; the required human sign-off was supplied by the project owner on August 31, 2026.

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
| .asmdef files | Exact production/test assembly references | Source of graph facts; legality is already checked by the existing #20 checker |
| tools/assembly-tier-check.py | Authoritative #20 §3.5.2 parser/checker; already runs inside `Spec hygiene checks` | Human-oriented output only before A1; lacks machine-readable complete-graph evidence and checker self-tests |
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

## 1.4 Existing dependency authority and corrected A1 premise

The dependency-policy premise carried by v0.4 was stale.

- ERR-020-002 and ERR-020-003 were resolved on August 17, 2026.
- Code Standards #20 §3.5.2 already contains the complete ten-tier order, the two out-of-band Infrastructure assemblies, and both labeled arrow conventions.
- `tools/assembly-tier-check.py` already parses that authority directly and enforces placement completeness, FR-CS-046/046a/046b direction rules, duplicate production names/folders, production→test references, unknown production references, and production-graph cycles.
- `.github/workflows/ci.yml` already runs that checker in the `Spec hygiene checks` job.

Therefore A1 has no dependency-policy repair to perform. The former A1b step is retired rather than re-executed.

The remaining A1 work is narrower:

1. extend the existing checker with deterministic machine-readable complete-graph evidence, classification-aware digests, and report-only all-assembly cycle visibility while preserving its existing production-policy verdict;
2. wire focused checker self-tests into CI; and
3. verify merge-protection state, then activate the existing required status where enforcement is disabled.

As observed during the v0.5 correction, repository ruleset `CI for Main branch` already lists `Spec hygiene checks` among its required contexts but the ruleset itself is disabled. Classic branch-protection state was not readable through the current integration and MUST be verified at A1c before claiming merge blocking.

## 1.5 Empirical integration failure classes

`docs/tracking/match-engine-wiring-backlog.md` independently documents the target failure mode: subsystem code can be built, tested, and assembly-reachable while having no production caller. It also records seven successive `[GT]` realism/calibration passes against an engine containing dormant subsystems, turning integration gaps into misleading calibration evidence rather than merely missing features.

The plan keeps two failure classes separate:

- **Class A — structurally dormant:** capability exists but no production activation/caller reaches it. Static/compiler-backed reachability can detect this.
- **Class B — behaviorally starved:** the call exists and executes, but a gate/trigger almost never becomes true. Static reachability cannot prove meaningful firing; W12-style runtime instrumentation is the existing answer.

Tackling demonstrates a valid third condition: deliberately disabled behavior. `TackleContactRadiusM = 0` is behaviorally unwired by owner decision, so structural role and activation state must be separate axes.

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
        +--> no Blocker with Status Open
        +--> every finding has one valid Disposition
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

Canonical Draft 2020-12 schemas live under `docs/tracking/architecture-governance/schemas/`. `common.schema.json` is the single machine source for shared enums, transition maps, fallback maps, and dependency-relation groups; the pure-stdlib reference module consumes that file rather than restating those values in Python. The remaining files are eight §3.1 category schemas plus the A4 bootstrap auxiliary schema. Seven committed state registries are seeded separately under `docs/tracking/architecture-governance/`; proof records are per-proof artifacts rather than an empty registry, and the finite bootstrap file is created only if A4 needs non-inferable runtime intent.

Free-text narrative MAY supplement a record, but blocking checks MUST depend only on typed fields whose semantics are defined and tested.

A2 is not a schema-shape review. Before a machine contract is frozen, an executable reference implementation MUST demonstrate canonical selector parsing/resolution, stable-identity handling, applicability precedence/conflict behavior, proof dependency-closure calculation, subject-scope fingerprinting/freshness, review-state transitions, and N/A/bounded-result handling. A5 may productionize and optimize those semantics, but MUST NOT silently redefine them.

Any change after A2 that materially changes those semantics reopens the affected A2/A3 approval dependency; implementation bugs that do not change semantics are ordinary tooling fixes.

## 3.2 Closed-world runtime-surface classification, identity, and provenance

Create `docs/tracking/architecture-governance/runtime-surface-classifications.json` for durable classification intent. Generated discovery output remains ephemeral evidence; the committed file MUST NOT become a hand-maintained copy of the source tree.

Allowed structural classifications remain: `production-runtime-root`; `contracted-child`; `test-only`; `tooling-only`; `generated-or-external`; `non-runtime-bearing`.

Applicability fallback scopes are selectors over those six classifications; they are not additional classifications. Their mapping is frozen as:

- `runtime-bearing` → `production-runtime-root`, `contracted-child`;
- `non-runtime-bearing` → `test-only`, `tooling-only`, `generated-or-external`, `non-runtime-bearing`;
- `repository` → all six classifications plus repository-level subjects that do not carry a structural classification.

Activation state is **not** another classification value. Structural role answers what the surface is; activation state answers whether a production capability is currently expected to execute.

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

The compiler-backed Class-A pass permits a finite temporary `docs/tracking/architecture-governance/bootstrap-runtime-surfaces.json` containing only non-inferable runtime intent. A1 does not create this runtime universe; it produces the asmdef graph only. After A2 freezes selector/identity semantics and A3 lands the governance amendments, A4 computes the fixed-point union of compiler-discovered candidates plus bootstrap declarations, classifies every emitted surface or records it unresolved, promotes those declarations into final contracts/classifications, and retires the temporary bootstrap file.

Test classification MUST NOT rely on a `.Tests` suffix alone. Assembly metadata, path, platform/define constraints, references, compiler facts, and explicit classification are considered. `TacticalDirector.TestingStrategy` or any similar assembly must be explicitly classified.

### 3.2.3 Stable identity and selector algebra

Compiler-discovered source surfaces use deterministic mechanical `symbol_key` values derived from canonical compiler symbols/signatures. Those keys are discovery identities, not permanent architectural IDs.

Stable `component_id` values are allocated only for durable declared architectural concepts such as a supported host, composition root, runtime-bearing component, or testhost. A file/symbol rename updates that component's selector/history; it does not create a new architectural component solely because a path changed.

The selector grammar MUST be frozen in A2 and MUST distinguish namespaces/types, constructors, overloaded method signatures, static members, and assembly identity. Every selector `type_id`, `containing_type_id`, and `parameter_type_ids` value MUST use the C# XML documentation ID type-signature spelling emitted from compiler symbols, not source/display-name text. The convention includes the by-reference `@` suffix, so legal overloads such as `M(System.Int32)` and `M(System.Int32@)` remain mechanically distinct; it also supplies the canonical spelling for generic parameters/types, arrays, pointers, and nested type structure. Producers MUST NOT erase those distinctions by emitting plain type names. Contracts keep `selector_history` sufficient to migrate ordinary moves/renames while preserving logical identity. Ambiguous or multiply resolving selectors fail strict mode.

Each classification record therefore carries the current `symbol_key`, `kind`, source path, symbol/signature, assembly, classification, and stable `component_id`/`contract_id` only when the surface has durable architectural identity.

Strict mode fails when a newly discovered surface is unclassified after initial baseline acceptance.

## 3.3 Property registry

`property-registry.json` adds `schema_version`, `decision_id`, `decision_actor`, optional `decision_provenance_revision`, `transition_from`, `transition_to`, `decision_rationale`, and `revalidation_history`.

Decision fields live in each record's append-only `decision_history`. A new property is established only as `null → Candidate`; every later edge must be one of Governance §3.1's six transitions, the current `state` must equal the final `transition_to`, and existing property order/history is immutable against the trusted merge-base. A material property amendment that does not change state appends `revalidation_history` rather than rewriting the admission record.

`decision_provenance_revision` is provenance only and MUST NOT require the registry landing to contain its own future commit SHA. Transition immutability is enforced by comparing the proposed registry/history against the trusted merge-base/parent version and permitting only schema-valid append/transition operations. If the prior authoritative registry cannot be retrieved, strict transition validation reports uncertainty rather than silently accepting rewritten history.

The validator enforces legal transitions and append-only decision history; it does not judge admission quality.

## 3.4 Typed integration ownership and activation contracts

`integration-contracts.json` retains the typed ownership/lifecycle fields and adds `activation_state`, activation-state metadata, and `tuning_surface_selectors`.

Allowed activation states are `active | intentionally-disabled | pending-integration | unresolved`, independent of structural classification.

- `active`: production execution is intended now; applicable Class-A reachability and required runtime evidence must not contradict it.
- `intentionally-disabled`: deliberate owner decision. Requires `activation_owner`, `decision_ref`, `disable_anchor`, and `reactivation_condition`. The anchor is a canonical selector plus typed predicate/value that the checker must resolve and verify. Missing/drifted anchors fail.
- `pending-integration`: known incomplete integration. Requires owner, exact gap, and exit/activation condition; it does not satisfy an active requirement.
- `unresolved`: activation intent is not established and cannot be treated as compliant in strict mode.

An `active` component with missing production reachability is a Class-A defect. Class-B firing/starvation can contradict `active` only when an owning FR/AP/contract defines the runtime condition; governance does not invent firing thresholds.

Components calibrated through `[GT]` or equivalent owner-declared tuning constants provide canonical `tuning_surface_selectors`. A tuning change is permitted normally only when every applicable owning component is `active`. Otherwise KD-W1 fails unless an approved exception explicitly authorizes the tuning scope.

Narrative fields may explain intent but cannot satisfy blocking ownership, activation, or disable-state assertions. Complex internal state-machine/concurrency semantics remain component-owned behavior proved through #19 evidence.

The frozen integration-contract schema `1.0.0` and reference semantics `2.1.0` validate envelope, selector, and activation-state shape; they do not resolve the schema-v1 ownership/path strings across the contract registry, runtime-surface registry, assembly/compiler inventory, dependency graph, and repository tree. A3.1a defines those strings' exact binding vocabulary in Code Standards §3.5.6. They remain declaration-only and ineligible for Machine blocking claims until A4 implements that resolver and its closed-inventory/blind-spot fixtures. The same boundary applies to the `not-applicable`/`na_fields` pairing: schema v1 can represent it, but A4 must enforce the pairing before it can satisfy a blocking lifecycle assertion. This limitation does not silently redefine the approved A2 semantics.

## 3.5 Applicability manifest and deterministic resolver

Create `docs/tracking/architecture-governance/applicability-rules.json`.

Each applicability evaluation has a **current change context**. In strict mode the subject MUST carry exactly one typed `change_type` from the canonical Governance §5.2 rows: `pure-local-calculation`, `new-public-cross-assembly-api`, `new-runtime-service`, `new-composition-root-registration`, `host-bootstrap-change`, `static-initialization-change`, `persistence-boundary`, `external-resource-dependency`, `testhost-runtime-divergence-fix`, `dependency-graph-only-refactor`, or `pure-data-schema-no-runtime-behavior`. An omitted change context cannot certify proof closure completeness.

Each rule contains `rule_id`, selectors, trigger ref, optional `change_types` match set, requirement refs, proof classes, gate classes, allowed N/A reasons, precedence, and fallback scope. `trigger_ref` identifies the owning FR/AP/failure-mode authority; `change_types` is only an applicability filter over the current subject change context. A rule with no `change_types` applies across all change contexts allowed by its other selectors.

All matches are evaluated. Schema-defined specificity controls precedence; equal-precedence conflicts fail. Precedence is not author-chosen: every explicit selector/identity/classification/activation rule outranks every fallback rule; among fallback rules, `runtime-bearing` and `non-runtime-bearing` outrank `repository`; the two category fallbacks are disjoint under §3.2's mapping. Change context is orthogonal to that surface ordering. Among otherwise-identical rules, a smaller matching non-empty `change_types` set is more specific than a broader matching set, every restricted set outranks a generic rule, and equal-sized overlapping context sets with different obligation payloads still fail as an ambiguity. Context specificity MUST NOT outrank one step of surface specificity. N/A is valid only for an enumerated reason and required approval reference.

All enum-valued JSON inputs are fail-closed typed validation surfaces. Wrong JSON shapes such as arrays/objects where a scalar enum is required MUST raise the owning semantics error type rather than leaking host-language `TypeError`. Non-strict applicability may be used for discovery, but if current `change_type` is absent its result MUST explicitly report incomplete change context (currently `context_complete: false` with `missing-change-type`) and MUST remain ineligible for proof certification under §3.7.1.

`--changed` optimizes only after applicability and the full proof-class closure are resolved. It MUST fall back to the full relevant proof universe when any changed surface is unmapped, when the current proof is stale, or when a changed surface is a member of the derived closure. It MAY skip only when every changed surface is mapped, none belongs to the closure, and the current applicability/closure fingerprints remain fresh. Unresolved applicability fails strict mode.

Applicability answers **which obligations apply**. It does not itself define the complete freshness dependency surface of a proof. The proof-class closure resolver in §3.7 derives that surface from the matched obligations, integration contracts, compiler/asmdef facts, tests/fixtures, configuration, and tooling required by the proof class.

A2 MUST execute the resolver against representative good/bad/conflict/N/A fixtures. Identical repository facts and declarations MUST resolve to identical obligation sets and closure inputs before A3 can rely on the schema.

## 3.6 Exception routing and precedence

Governance exceptions remain property-oriented exactly as Governance §7 defines them. This integration MUST NOT route FR-CS or FR-TS waivers directly into exceptions.json unless the affected obligation is an admitted AP that explicitly allows an exception.

The canonical `exceptions.json` record binds each exception to an admitted property, exact typed scope, risk/mitigation/owner, finite expiry trigger, architectural approval, and current status. Routing is exclusive: AP waivers use Governance `exceptions.json`; FR-CS and FR-TS requests remain with their owning mechanisms, which cannot be used as a back door to waive an AP.

Existing #19/#20 exception mechanisms remain owner-specific. They cannot waive an admitted AP, missing required evidence, concrete correctness/integrity failure, or Governance Blocker.

## 3.7 Canonical proof artifact schema and dependency closure

The #19 amendment MUST land the canonical schema and closure semantics before proof workflow or CI gating.

Reusable proof records require: `schema_version`; `proof_id`; `proof_class`; requirement/property refs; applicability rule IDs; result (`pass`/`fail`/`na`/`bounded`); N/A or bounded justification/approval; `subject_scope_digest`; provenance revision/tree metadata; proof-class dependency closure and content fingerprints; scoped inventory/asmdef digests when applicable; relevant configuration fingerprints; tool/extractor identities; runner/execution records; conditional failure-injection and mutation records; created metadata; revalidation history.

### 3.7.1 Proof-class closure resolution

The audit MUST derive and validate closure using the proof class rather than trusting an author-supplied file list. The proof-class enum is exactly the four classes owned by Governance §5 and FR-AG-027–030: `structural-reachability`, `lifecycle-order`, `failure-injection`, and `mutation`. Persistence boundaries and external resources are applicability/change triggers from Governance §5.2, not additional proof classes.

- structural reachability: matched contract + owning roots + construction/registration edges + applicable public/bypass surfaces + relevant asmdef nodes/edges;
- lifecycle/order: structural closure + lifecycle members, owners, ordering edges, relevant synchronization/thread-affinity members, and testhost equivalents;
- failure-injection: applicable structural/lifecycle closure + exact failure target, test/fixture, runner configuration/environment, and tool semantics;
- mutation: applicable structural/lifecycle closure + exact mutation target, test/fixture, runner configuration/environment, and tool semantics.

For any of those four proof classes, serializer/schema/resource edges are added to the closure only when the **current applicability subject** has `change_type: persistence-boundary` or `change_type: external-resource-dependency`. The closure engine does not infer that context from `trigger_ref` or from rule payloads. Generic tool/runtime configuration dependencies remain governed by their ordinary relation semantics; a persistence change context does not make every repository configuration file relevant. The current `change_type` is part of the applicability subject digest and is stamped into proof closure output.

Proof records MAY include additional declared dependencies, but the resolver verifies they are not narrower than the mechanically required closure. A proof closure cannot be certified from an applicability result that omits the current `change_type`; strict applicability already rejects that omission, and closure validation also fails closed if fed a non-strict/incomplete applicability result. If the resolver otherwise cannot prove closure completeness, strict mode fails or the proof must use a #19-approved bounded substitute.

Freshness must detect material additions, deletions, renames, generated/config changes, new applicable roots, asmdef changes, and checker/extractor-semantic changes inside that resolved closure. A rename that preserves stable component identity updates selector binding and fingerprints without pretending the architectural component was deleted/recreated.

### 3.7.2 Execution truth

Every required executable record carries `execution_state` from: `passed`, `failed`, `skipped`, `excluded`, `unavailable`, `not-run`, `runner-failed`. Only `passed` satisfies an unqualified required execution obligation.

A bounded substitute is a proportionality mechanism for omitted or unavailable proof, not a waiver of contrary execution evidence. Therefore `failed`, `skipped`, and `runner-failed` are unsatisfied and cannot be converted to satisfied by a bounded substitute. `excluded`, `unavailable`, and `not-run` may satisfy only when #19 explicitly permits a bounded substitute for that obligation and the approved substitute record carries the exact authority reference, approval reference, justification, and omitted proof surface or remaining uncertainty. `passed` and bounded-substitute claims are mutually exclusive.

A2 freezes this executable state machine before A3 consumes it.

Execution records bind the exact test/command/runner, environment/configuration, subject digest, start/end result, and machine-readable result artifact when the runner provides one.

### 3.7.3 Failure-injection and mutation identity

Failure-injection evidence records the exact injected condition/input, target selector, expected failure/recovery path, executed command/test, observed result, and tool/environment identity.

Mutation evidence records the base subject digest, exact target selector, mutation operator or canonical patch/mutant digest, baseline execution, mutant execution, expected detector, observed detector failure, tool identity, and restoration/clean-state verification. A no-op/equivalent/wrong-target mutant cannot satisfy the named invariant merely because a test command ran.

Proof-class validation is conditional. A triggered mutation/failure proof without these fields is invalid. Structural proof without its required closed-world/scoped inventory binding is invalid.

## 3.8 Versioned adversarial-review ledger

Create a canonical durable `docs/tracking/architecture-governance/review-ledger.json`. The existing ignored `.adversarial-review/` directory remains scratch/session cache only and MUST NOT be the durable governance record.

The durable ledger has two entity types:

1. **Review run/series records** — `schema_version`, `review_run_id`, optional series ID, review scope, `subject_scope_digest`, provenance revision/tree, review round, reviewer identity, coverage/unverified surfaces, convergence state, and the final-review marker.
2. **Finding records** — `finding_id`, `summary`, `evidence`, `severity`, `requirement_property`, `disposition`, `required_action`, `owner`, `status`, `round_introduced`, and `resolution_evidence`, plus namespaced `stable_key`, parent review/series ID, and disposition approval where required. The first eleven fields are the machine-field equivalents of Governance §4.2's required finding schema; extensions do not replace them.

The final-review marker belongs to the review run, not to each finding. A clean final review with zero findings is therefore representable without inventing a synthetic finding, and a finding-heavy review does not duplicate run metadata across every record.

Finding IDs remain stable across rounds within their review namespace. `stable_key` uniqueness is defined as `(review_series_id, stable_key)` unless the schema explicitly declares a broader namespace. Silent global-key assumptions are forbidden.

Disposition and status are distinct. New governance-aware reviews use the versioned durable ledger prospectively. Historical prose/ERR review records remain read-only unless a deterministic source record actually contains the required fields; the migration MUST NOT infer tradeoff/risk/disposition approvals from narrative history.

Each finding carries append-only `status_history`: creation is exactly `null → Open`, and the only terminal edge is the Governance §4.1 mapping for its selected Disposition. Review runs are immutable snapshots; later rounds append runs instead of rewriting coverage or final-review claims. Strict convergence recomputes the final run's material `subject_scope_digest` and fails on any open finding, invalid pairing, unsatisfied applicable property, or unverified surface.

A fresh final-review marker is valid when the current material review subject recomputes to the recorded `subject_scope_digest`. Recording the review run itself or unrelated landing/tracking metadata does not recursively invalidate that subject.

## 3.9 Temporary activation baseline

If required, `temporary-activation-baseline.json` is finite and versioned. Each item records violation ID, exact stable component/selector binding, baseline `subject_scope_digest`, creation provenance revision, owner, disposition, required action, and expiry trigger.

Creation provenance is not the freshness key. New violations fail; a baseline item's governed selector/content changing outside the recorded subject scope requires explicit review. Final strict activation requires zero active items and retirement of activation-only baseline machinery.

The baseline modes are `inactive → migration → strict` (with direct `inactive → strict` allowed). Sealing is irreversible; a sealed baseline may shrink but cannot add or rewrite items. Strict activation requires `mode: strict`, `sealed: true`, and a mechanically empty `items` array.

---

# 4. Codebase integration and closed-world enforcement boundaries

## 4.1 Assembly and dependency graph

All `src/**/*.asmdef` files remain the source of edges. Code Standards #20 §3.5.2 and FR-CS-046/046a/046b already own the approved dependency classifications and direction semantics; `tools/assembly-tier-check.py` is the single parser/checker for those rules.

A1 extends that checker with deterministic JSON evidence covering every asmdef/reference, production/test/out-of-band classification, external and ambiguous references, production and all-assembly cycle components, and separate graph/classification/subject digests. Tooling is not emitted as a zero-valued pseudo-class when no tooling asmdef belongs to the `src/**/*.asmdef` universe.

The existing production-policy verdict remains blocking within its CI job; all-assembly cycle visibility is report evidence only unless a later approved rule gives test-graph cycles gating semantics.

## 4.2 Structural versus behavioral activation evidence

### 4.2.1 Track A — Class-A structural discovery

A1 is asmdef-only: it reuses the existing #20 checker to emit complete machine-readable assembly/reference/classification evidence and all-assembly cycle visibility. It requires no Roslyn and makes no source-level reachability claim.

After A2/A3, A4 performs compiler-backed runtime/root discovery over §3.2, combining semantic candidates with finite bootstrap declarations. A4 computes the closed runtime universe, detects Class-A dormancy, and seeds final structural classifications/contracts.

Static discovery can establish that production activation is absent; it cannot establish that an existing gate fires often enough to be behaviorally meaningful.

### 4.2.2 Track B — Class-B runtime firing evidence

Gate/trigger firing instrumentation remains in the owning runtime/domain, not `tools/architecture-governance`. For the match engine, W12-style instrumentation counts whether relevant phases, gates, and trigger conditions fire over representative match execution.

Governance consumes the resulting evidence only when an approved FR, admitted AP, or component contract requires it. It validates identity/freshness/runner configuration and the owning rule's condition; it does not own the counters or choose domain thresholds.

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

`tools/architecture-governance` remains the later governance policy orchestrator. A1 does not create a second asmdef parser: its asmdef-only evidence is produced by the existing `tools/assembly-tier-check.py`, independently of the later C# extractor.

Later blocking Class-A discovery uses a small compiled .NET extractor under `tools/architecture-governance/csharp-discovery/` with Roslyn/compiler APIs. For certifying execution it MUST be built **from source at the governed checkout**. Checked-in/downloaded/prebuilt binaries cannot satisfy governance proof.

CI provisions the pinned .NET SDK/compiler toolchain, builds the extractor from the checkout, and fingerprints source/project/config/compiler identity before use. If the source build or semantic extraction cannot run, checks depending on those facts return discovery uncertainty rather than a false pass.

The Python layer does not implement a hand-written C# parser. Asmdef parsing remains source-JSON driven.

## 5.2 Versioned CLI contract

The amendment pins minimum Python version, UTF-8, repository-relative normalized paths, deterministic ordering, schema handling, malformed-input behavior, generated-input handling, full/`--changed` semantics, exact exit codes, and the C# extractor's .NET/compiler version, compilation roots/references, preprocessor symbol set, canonical output format, and failure behavior.

Required exits: 0 pass; 1 activated check failure; 2 CLI/schema error; 3 applicability/discovery/extractor uncertainty prevents a sound strict result.

--strict fails closed on unresolved applicability, unclassified closed-world surfaces, stale required evidence, or unsupported schemas.

## 5.3 Check classes

AG-CHECK-DISCOVERY: asmdefs, runtime surfaces, classifications, digests.
AG-CHECK-REGISTRY: property transitions and governance exceptions.
AG-CHECK-APPLICABILITY: trigger resolution, precedence conflicts, N/A, fallback.
AG-CHECK-CONTRACTS: typed selectors/edges/references.
AG-CHECK-ACTIVATION: activation state, machine-verifiable disable anchors, pending-integration ownership, and KD-W1 tuning preconditions.
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
- structural classification with independently varying activation states;
- valid and drifted/missing `intentionally-disabled` anchors;
- `[GT]`/tuning-surface change while `pending-integration` or `intentionally-disabled`;
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
| FR-CS-074 | Every runtime-bearing component whose correctness depends on activation MUST have an explicit integration owner, exact integration point, and orthogonal activation state. | MUST | Governance FR-AG-021/022 | §3.5.6 |
| FR-CS-075 | Every production host/composition root in the approved runtime discovery universe MUST be classified and mechanically accounted for. | MUST | Governance FR-AG-024/026 | §3.5.6–3.5.7 |
| FR-CS-076 | Applicable runtime-bearing components MUST declare construction, activation, update/use, and teardown ownership through typed lifecycle records, with the §3.5.6 `not-applicable`/`na_fields` representation only where a phase does not exist. | MUST | Governance FR-AG-023 | §3.5.6 |
| FR-CS-077 | Applicable alternate hosts/testhosts MUST preserve the invariant or declare an approved divergence linked to current evidence. | MUST | Governance FR-AG-024 | §3.5.7 |
| FR-CS-078 | Activation bypasses inside a mechanically closed governed surface MUST be prohibited or explicitly classified. | MUST | Governance FR-AG-025/026 | §3.5.7 |
| FR-CS-079 | Activation-capable public runtime surfaces inside an activated closed-world category MUST be classified supported, test-only, non-activating, or made non-public. | MUST | Governance FR-AG-026/027; §5.3 | §3.5.7 |
| FR-CS-080 | Static initialization participating in runtime ownership/order MUST be declared and MUST NOT bypass applicable composition/lifecycle requirements. | MUST | Governance FR-AG-023/025; §5.4 | §3.5.6–3.5.7 |
| FR-CS-081 | Blocking integration/activation declarations MUST be mechanically resolvable to repository selectors and independently verifiable facts; `intentionally-disabled` requires a verifiable disable anchor, and unsupported semantic assertions remain non-blocking evidence. | MUST | Governance FR-AG-034/035/036A | §3.5.6–3.5.7; §5 |

§2.2 updates the 73 total to 81 and adds the architecture range without renumbering existing IDs.

## 6.2 FR-CS-046 / dependency authority

ERR-020-002/003 and the #20 dependency repair were already completed on August 17, 2026. A3 consumes that approved dependency model as existing authority; it does not reopen or re-land the taxonomy or arrow semantics.

The A1 implementation delta is tooling/evidence only: machine-readable output and self-tests are added to the checker that already enforces FR-CS-046/046a/046b. Any future dependency-policy change remains a normal #20 amendment and is not smuggled through governance tooling.

## 6.3 Exception boundary

#20 Mode 3 remains #20-owned. FR-level exceptions affect only #20 conformance and cannot waive an admitted AP, required proof, concrete correctness/integrity failure, or Governance Blocker.

## 6.4 Exhaustive #20 amendment matrix

| File | Required work |
|---|---|
| section-1.md | Authority/scope references if affected; synchronized status/version history. |
| section-2.md | FR-CS-074–081; 73→81 counts/partition/TOC; Mode 1/3 boundary; history. |
| section-3.md | Existing §3.5.2 dependency authority remains intact; add only governance-specific stable component/canonical selector semantics, typed integration/lifecycle/runtime-surface mechanics, explicit + implicit static-initialization treatment, and history. |
| section-4.md | Contract/discovery relationships and diagrams; no runtime dependency. |
| section-5.md | Checklist; FR-to-verification rows 074–081; compiler-backed semantic fact source; report-only vs blocking boundaries; history. |
| section-6.md | Repair only references/counts made stale; no duplicate authority. |
| section-7.md | Activation/deferral text tied to real prerequisites. |
| section-8.md | Governance/#19 references and traceability. |
| section-9-approval-checklist.md | FR count/range, traceability, reapproval evidence, status/history. |
| appendices.md | Typed contract schema/examples; stable component/symbol identities, overload-safe selectors, rename migration; examples illustrative only. |
| outline.md / outline-mid.md / outline-detailed.md | Repair stale 73-count/section/dependency claims where current. |
| docs/specs/SPEC_INDEX.md | #20 status/version updated atomically with §9 decision. |
| docs/tracking/spec-error-log.md | No new ERR-020-002/003 action; preserve their existing resolved record and correct only stale duplicate/index state if independently encountered. |
| docs/tracking/file-manifest.md / CHANGELOG.md | Record the governance amendment and distinguish CI execution from actual required-status activation. |

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
| FR-TS-094 | Missing, failed, stale, schema-invalid, applicability-incomplete, skipped, excluded, unavailable, not-run, or runner-failed required architectural proof MUST block merge once the gate is active. A bounded substitute MAY replace only an `excluded`, `unavailable`, or `not-run` execution when FR-TS-096 permits it; it MUST NOT convert `failed`, `skipped`, or `runner-failed` execution into satisfaction. | MUST | Stage 0+1 |
| FR-TS-095 | Merge-critical governance tooling MUST have known-good, known-bad, and blind-spot verification proportionate to false-positive/negative consequence. | MUST | Stage 0+1 |
| FR-TS-096 | Bounded substitutes are permitted only for computationally disproportionate, intentionally omitted, or unavailable proof and MUST record authority, scope/rationale, omitted surface or remaining uncertainty, and approval. They MUST NOT waive an executed proof failure, runner failure, or ordinary skipped execution. | MUST | Stage 0+1 |
| FR-TS-097 | A `[GT]` or owner-declared calibration/tuning change MUST NOT land for a component whose activation state is intentionally-disabled, pending-integration, or unresolved unless an approved exception explicitly authorizes that tuning scope. | MUST | Stage 0+1 |

§2.2 gains FR-TS-086–097 as Architecture proof/evidence integration, mechanics in new §3.11, verification through §5.6/architecture gate. Total becomes 97.

## 7.2 Existing FR amendments

FR-TS-084: authority linkage may be FR, admitted AP, approved invariant/equivalent authority, or concrete independently established correctness/integrity failure. Novel generalized preferences become Candidate Property.

FR-TS-076: add architecture/evidence gate while preserving #16/#18 ownership.

FR-TS-077: flake quarantine cannot waive missing architecture proof or structural governance gates.

FR-TS-063: qualify the existing quarantine rule so quarantine suppresses only an eligible functional-gate blocking effect; it does not satisfy or waive a separately required architecture-proof obligation. This is a consistency amendment required by FR-TS-077/094, not a new quarantine mechanism.

FR-TS-088: §3.11.6 states the structural failure-class obligation as MUST and defers to Governance §5.3 (FR-AG-027) as the authority for the class list, reproducing it verbatim rather than restating it in spec-local vocabulary. FR-TS-088 itself continues to govern universe completeness, not detector coverage.

FR-TS-093 remains pointer-style; Governance owns convergence, #19 consumes it.

## 7.3 Canonical proof appendix

`appendices.md` publishes §3.11's schema-shaped proof contract and proof-class closure semantics before proof implementation. It defines pass/fail/N/A/bounded results; subject-versus-provenance identity; stable selector binding; execution states; N/A approval; exact failure-injection/mutation identity; scoped inventory/asmdef/config/tool binding; mechanically derived dependency closure; revalidation; and bounded uncertainty.

Examples MUST include a committed reusable artifact whose freshness does not depend on its own containing Git tree, an unrelated-change case that remains current, a relevant transitive dependency change that stales proof, and a required execution whose runner is skipped/excluded and therefore does not satisfy the obligation. Example-only requirement/property identifiers SHOULD use an explicit reserved/example namespace rather than a live project FR identifier to reduce copy-paste ambiguity.

## 7.4 Exhaustive #19 amendment matrix

| File | Required work |
|---|---|
| section-1.md | Governance boundary references; revision status/history. |
| section-2.md | FR-TS-086–097; 85→97 partition/count; FR-TS-063/076/077/084; KD-W1 activation/tuning precondition; exception boundary; failure modes/history. |
| section-3.md | New §3.11 applicability/proof mechanics: subject/provenance split, proof-class closure, execution-state and revalidation semantics; no #20 ownership duplication. |
| section-4.md | Proof/test structures/interfaces only where §4 owns them. |
| section-5.md | FR-to-verification through 097; stale/missing/applicability/closure/activation-anchor/KD-W1/skip-exclusion/wrong-mutant blind-spot fixtures; history. |
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

Owning placement does not imply execution. Every required executable proof resolves to a runner capable of compiling/executing that assembly and a machine-readable execution record. A required test excluded by `known-failures.txt`, flake quarantine, `[Ignore]`, `Assert.Ignore`, unsupported-assembly filtering, conditional Unity-job skipping, or equivalent exclusion is unsatisfied. Only a deliberate `excluded`, `unavailable`, or `not-run` state may be replaced by a #19-approved bounded substitute under FR-TS-096; an actual `skipped`, `failed`, or `runner-failed` result remains unsatisfied.

The architecture gate MUST mechanically reject intersection between its required-test set and active quarantine/exclusion sets. Where possible it also executes the resolved required test set directly; otherwise it consumes mandatory upstream runner results with exact test identity/result binding.

Targeted governance mutation at Stage 0+1 does not depend on project-wide Stryker activation. FR-TS-091 is satisfied by the exact reproducible mutant protocol in §3.11.9; the broader Stryker.NET program may remain deferred under #19's existing Stage-1 tooling decision.

---

# 8. Adversarial-review integration

New governance-aware reviews use the durable two-entity model in §3.8: review runs/series plus findings. A finding begins with Status `Open`, carries exactly one Disposition, and completes as `Blocker → Resolved`, `Accepted Tradeoff → Accepted`, `Residual Risk → Recorded`, or `Candidate Property → In property process`. For a selected Disposition, only `Open` or that mapped terminal Status is legal; all other pairings are rejected. `Dispositioned` is not a Status. The review run separately records coverage and convergence.

Before convergence behavior changes, version both schemas; define required fields per disposition; legal transitions; approval authorities; review-series/stable-key namespaces; subject-scope digest calculation; run-level final marker; prospective legacy cutover/read-only policy; and rejection of silent defaults. Every producer and consumer migrates in A6.

The current `.adversarial-review/round-*.json` and `ids.json` remain scratch inputs only. They are not treated as durable governance evidence because the directory is intentionally ignored. Historical prose/ERR records are not reverse-engineered into approvals they never encoded.

Convergence requires every substantive finding to carry exactly one valid Disposition and its Governance §4.1-mapped terminal Status; therefore no `Open` finding of any Disposition and no invalid Disposition/Status pairing may remain. It also requires current required proof and a fresh full review **run** whose material subject digest matches the current reviewed scope. A clean final review may contain zero findings and still record convergence. Round-budget exhaustion with any gating obligation is NON-CONVERGED. Severity never independently decides convergence.

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

## 10.1 Existing asmdef checker and A1 activation

A1 reuses `tools/assembly-tier-check.py`, which already reads `src/**/*.asmdef`, parses #20 §3.5.2/FR-CS-046/046b, and runs inside `Spec hygiene checks`.

A1a extends that checker in place with:

- deterministic `--json` output for the complete asmdef graph;
- explicit production/test/out-of-band/unresolved classification evidence without a fictitious tooling bucket;
- separate graph, classification, and combined subject digests;
- all-assembly cycle components as report evidence while preserving production-cycle gating semantics; and
- focused unit tests wired into CI.

No standalone governance asmdef parser is permitted.

A1c then verifies the repository's actual merge-protection configuration and enables the existing required `Spec hygiene checks` context where enforcement is disabled. The repository ruleset observed during v0.5 already names that context but is disabled. A1c MUST verify the live state immediately before mutation and MUST NOT claim merge blocking merely because the CI job runs.

*(v0.6: done — ruleset set Active August 29, 2026 and enforcement measured August 30, 2026. See §11 A1c and `docs/tracking/a1c-enforcement-evidence.md`.)*

The early asmdef path MUST NOT evaluate lifecycle ownership, proof freshness, review convergence, activation state, Class-B firing, or any rule that depends on later governance machinery.

## 10.2 Full architecture aggregator

After A5/A6, the full `architecture-governance` job runs with `if: always()` (or equivalent), source-builds the C# extractor with the pinned .NET SDK, runs extractor/tool self-tests, then performs discovery/classification/applicability, registry/contract/activation/proof/ledger validation, activated asmdef checks, and strict audit.

## 10.3 Required runner bridge

Owning runners remain responsible for executable proof. Class-B gate/trigger instrumentation is another owning-runner input, not a governance-tool responsibility. Missing/stale required runtime evidence is unsatisfied under the same execution-truth rules.

## 10.4 Required-status activation

A8 is incomplete until the exact full `architecture-governance` status is required on protected merge paths. This does not prevent §10.1's narrow asmdef status from becoming required earlier.

CI configuration records the pinned .NET SDK, source-build command/fingerprint for the extractor, job ordering, skipped/cancelled/unavailable behavior, and required-status settings.

## 10.5 Activation tiers

Early asmdef status: objective dependency checks only.

Report-only until prerequisites: source-level Class-A absence before compiler-backed closed coverage; host/public/bypass completeness before A4; semantic lifecycle rules without proof; Class-B firing without an owner-defined evidence contract.

Block after A4–A8 as applicable: new unclassified root; `active` component with prohibited Class-A dormancy; invalid/drifted intentional-disable anchor; KD-W1 tuning violation; changed governed lifecycle without proof; prohibited bypass; missing required proof; open Blocker; stale final review; invalid active baseline.

`--changed` never weakens applicability. After full closure resolution it falls back when a changed surface is unmapped, the proof is stale, or the changed surface belongs to the derived closure; it skips only on proven non-impact.

---

---

# 11. Staged implementation sequence

The A0–A9 model remains the lifecycle for full governance activation. v0.4 adds an intentionally narrow structural slice that can deliver evidence—and later objective enforcement—without waiting for the full stack.

## A0 — Adopt Governance authority

Governance passes its own adoption gate and pins its exact version/content digest. Draft-stage governance documents do not require SPEC_INDEX registration or file-manifest rows merely to exist; those remain landing obligations when their owning process requires them.

**A0 gate boundary, amended in v0.7.** Governance §9 carries two different bars, and A0 is the first one only:

| Gate | Governance text | Scope | Owning stage |
|---|---|---|---|
| **Authority approval — this is A0** | §9 preamble: *"MUST satisfy its own governance model before becoming **authoritative**"* | §9.1–§9.6 | A0 |
| **Full operational adoption** | §9.7 heading: *"Before this specification is considered **fully adopted**"* | §9.7 | A3–A9 |

A0 closes when all of the following hold:

1. **(Verification)** Every box in §9.1–§9.6 is either verified against the document's own text with a cited line range, or is one of the six §9.6 process-state assertions discharged by the adoption review record itself.
2. **(Review)** A fresh review over the *current* artifact is completed and recorded, per Governance FR-AG-018. The record MUST carry review-level evidence — subject identity and digest, scope, method, reviewer, date, round, and outcome — and MUST record every finding in the Appendix B field set with an explicit disposition. Appendix B is a finding-record template only; it does not by itself constitute a review record, and a bare list of findings does not evidence that a review occurred.
3. **(Findings)** Every substantive finding has exactly one valid Disposition and is in its Governance §4.1-mapped terminal Status. No `Open` finding of any Disposition and no invalid Disposition/Status pairing remains. Per FR-AG-020, a round budget that ends with Blockers open is recorded NON-CONVERGED, not approved.
4. **(Sign-off)** A human records approval. This is not delegable to an agent.
5. **(Landing, in this order)** `Status: Draft` → `Approved` is written **first**; the SHA-256 of that exact resulting file is computed **after** that edit and recorded **here, outside the Governance file**. Computing the digest before the status edit pins a superseded artifact; writing the digest into the file it covers invalidates itself.

A0 explicitly does **not** require the property registry, the durable finding ledger, review tooling, or any #19/#20 amendment. Those are §9.7 items owned by A3–A9. Building them to approve the document would invert this plan's own sequencing.

**A0 review record:** `docs/tracking/a0-governance-adoption-review.md`. **A0 CLOSED August 31, 2026.** The project owner explicitly approved Governance v0.10; the Governance file was then changed from `Status: Draft` to `Status: Approved`; only after that edit was the approved file hashed. All §9.1–§9.6 boxes verify and every finding is in its mapped terminal Status.

| A0 closure field | Recorded value |
|---|---|
| Human sign-off | Project owner — Approved, August 31, 2026 |
| Governance version / status | v0.10 / `Approved` |
| Approved Governance Git blob | `76502282f205f5c4fd77c79c3309766c4dbd4498` |
| **Adoption SHA-256** | **`aa1792bf143fb3bc1066176dedb33abc4097045e7d089844edf05ccf9961d8f6`** |
| Review record | `docs/tracking/a0-governance-adoption-review.md` v1.8 |
| A0 status | **CLOSED** |

The SHA-256 above is the canonical A0 adoption pin. It covers the exact approved Governance file and is stored outside that file, so recording the pin does not invalidate its subject.

## A1 — Consolidate existing asmdef evidence and activate enforcement

**A1a — single-checker machine evidence.** Extend `tools/assembly-tier-check.py` rather than creating a second parser. Add deterministic JSON complete-graph output, production/test/out-of-band/unresolved classification evidence, graph/classification/subject digests, all-assembly cycle reporting, and focused CI-wired unit tests. Preserve the existing #20 production-policy verdict and its human output.

The former dependency-repair step is removed: ERR-020-002/003 and the ten-tier/arrow repair already landed August 17, 2026.

**A1c — activate the existing status.** Re-read live protection/ruleset state, then enable the existing required `Spec hygiene checks` context if it remains disabled. Do not create a parallel `architecture-asmdef` status unless the existing context is technically incapable of expressing the requirement.

**A1c completion criteria, amended in v0.6; A1c COMPLETE August 30, 2026.** A1c was previously written to close on *an observed blocked merge*. That condition is not measurable as stated: `mergeable_state: blocked` is returned for an unmet approving review, an unresolved conversation, a *pending* required check, or a *failing* one, and does not name which. A1c closes when all three of the following hold, and the record MUST state which are configuration and which are execution:

1. **(Configuration)** The ruleset governing `main` is Active and its *Require status checks to pass* list is read directly in repository settings and recorded **in full** — reader, date, and every entry — not merely the presence of the context of interest. It MUST contain `Spec hygiene checks`. It MUST NOT contain a context that reports `failure` as its steady state, which on this repository means `Compile + test (Linux shim gate, non-certifying)` — it carries the owner-held `sim_match_engine_close_chance` red and would freeze every merge. *(v0.6 correction: an earlier draft of this criterion also named `Unity tests`, on the reasoning that a required-but-`skipped` context would freeze merges. That is wrong — GitHub documents `skipped` as satisfying a required check — and the claim is withdrawn. `skipped` is still worth recording in the evidence, because a context that never truly executes provides no assurance whatever it does to mergeability, which is the substance of non-negotiable 12; but it is not a freeze risk.)*
2. **(Execution)** `Spec hygiene checks` is observed reporting a real conclusion — not `skipped`, `cancelled`, or absent — in both a passing and a failing arm.
3. **(Execution)** Blocking is demonstrated by a **paired two-arm comparison varying exactly one required check**: every other required check green in both arms, every non-required check in the same state in both, and no approval or unresolved conversation outstanding in either. The record MUST name both head commits, the varied check's conclusion in each arm, and the resulting `mergeable_state`. **A single-arm reading is NOT acceptable** — see the reason above.

Non-negotiable 12 is **not** relaxed by this amendment and continues to govern every runner-supplied proof unchanged.

**Satisfied August 30, 2026.** Evidence, captured durably because `mergeable_state` is point-in-time and not retrievable later: `docs/tracking/a1c-enforcement-evidence.md`. Green arm `d689f2b` — all six required checks green — `mergeable_state: unstable`, mergeable. Red arm `d497a4d` — identical but for one stale `Decision Tree #7` line turning `Spec hygiene checks` to `failure`, the other five still green — `mergeable_state: blocked`. **A red required check stops the merge; A1 has objective enforcement.** The evidence record also carries the required-checks list in full and the required-approving-reviews 1 → 0 owner decision taken during this work, with its cost stated. Classic branch protection on `main` remains unread (403 through the current integration); the ruleset layer only is claimed.

## A2 — Freeze schemas and executable semantics

Freeze identity/selectors, activation-state/disable-anchor semantics, applicability with required strict-mode Governance §5.2 subject change context plus optional rule `change_types` filters, contracts, the exact four Governance proof classes and their conditional closure/freshness behavior, execution-truth/bounded-substitute semantics, property/exception, review, and baseline schemas. The selector contract pins C# XML documentation ID type-signature spelling for every selector type ID, including byref `@`, so the future compiler fact producer cannot collapse legal overloads by using display/plain type names. The reference semantics include activation-anchor evaluation, KD-W1 tuning-surface matching, typed enum validation for untrusted JSON, schema-derived fallback/context precedence with narrower matching context sets outranking broader ones without crossing surface precedence, explicit non-strict incomplete-context diagnostics, persistence/resource closure activation from the evaluated subject change type rather than rule payload, fail-closed proof closure when change context is absent, and conservative changed-surface rerun decisions. Any compiler reference implementation is source-built with the pinned .NET SDK/toolchain.

**A2 closure gate, added in v0.18.** Implementation, merge, and approval are separate states. A2 closes only when all of the following hold:

1. **(Scope)** The closure record maps all eight §3.1 categories to their canonical schemas and, where durable registry state exists, their committed state files. The proof category is per-proof and MUST NOT gain a meaningless empty registry merely to make the file counts equal. Shared control data and the A4 bootstrap auxiliary are identified separately.
2. **(Single source / schema verification)** Every schema declares its canonical `$id` and parses; every `$ref` resolves by RFC 3986 URI resolution against that `$id`, not by filename lookup; every seed carries a supported `schema_version`; and enum/transition/fallback/relation control data has one machine source in `common.schema.json` that the executable reference consumes. No manually duplicated Python enum is accepted.
3. **(Executable verification)** Every frozen machine contract has an executable validator and representative fixtures — the per-proof artifact contract included, notwithstanding that it carries no seed registry. The pure-stdlib reference suite passes representative good, bad, conflict, transition, stale-history, uncertainty, authority-routing, convergence, and strict-baseline fixtures, and every fixture the semantics accept is also checked against its frozen schema so the two descriptions cannot drift. The exact test split and command output are recorded; a bare aggregate count is insufficient.

   **Fail-closed validator contract, added in v0.19.** A validator that takes a cross-document input — a trusted prior registry, a prior ledger or baseline, the live violation set, a current subject digest — treats an omitted input as uncertainty in strict mode, never as approval. `None` is reserved for a positive claim the trusted merge base can support ("no prior existed"); absent discovery evidence is not the empty set. A flag that *adds* a requirement, such as `strict_activation`, is not part of this contract and correctly defaults off.
4. **(Fresh review)** A fresh review is performed over the pushed current candidate, with subject identity/digests, reviewer, scope, method, findings, and outcome recorded. A local-only commit cannot satisfy this condition.
5. **(Findings)** Every substantive finding has one valid Governance Disposition and its §4.1-mapped terminal Status; no open or invalid finding remains.
6. **(Sign-off)** The project owner records explicit approval of the frozen A2 contract. This is not delegable to an agent.
7. **(Landing)** The approved candidate lands on the base used by A3, and the closure record is marked `CLOSED` with its approved subject-digest bundle. Merge alone does not close A2, and approval of a different digest does not transfer.

**Current state: CLOSED, September 2, 2026 — all seven conditions satisfied, landed on `main` at `693db56` with the digest verified. A3 is unblocked.** The candidate provides ten schema documents, seven versioned state registries, reference semantics v2.1.0, and the bounded schema validator `tools/architecture-governance/schema_validator.py`. The verification split is 149 governance + 9 phantom-stream context + 8 assembly-tier fixtures.

**All seven conditions are satisfied.** Eleven review rounds are recorded in the durable ledger under series `A2-SCHEMA-FREEZE`, each bound to the material subject digest of the tree it actually reviewed and each digest recomputed from the commit its scope names. Twenty-three findings, all `Blocker` / `Resolved`. Every independent round through 10 found a defect the preceding non-independent work did not; round 8 found three in the frozen contract itself, round 9 found that one of round 8's own fixes had closed the §3.9 `inactive → migration` edge, and round 10 found that none of those semantic changes had advanced `REFERENCE_SEMANTICS_VERSION`. **Round 11 found nothing, which is what finally satisfied condition 4** — every earlier round moved the subject it had just reviewed.

**Condition 4 was claimed at plan v0.20, retracted at v0.21, and is satisfied at v0.30 — by a different round, against a subject that did not move.** Independent review found that round 3 reviewed `678f0f2`, the material subject then moved by 150 lines — including the `A2-R3-001` fix — and the commit asserting completion had itself never been reviewed. The gate's pushed-candidate wording is deliberately stronger than FR-AG-018's, and the party satisfying a condition does not get to relax it. Row 4 becomes claimable only after a fresh review of the artifact as pushed.

## A3 — Amend and reapprove #19/#20 governance integration

The coordinated bundle consumes the already-repaired #20 dependency model. Add activation-state mechanics to #20 and FR-TS-097/KD-W1 to #19 alongside the existing governance amendments.

Drafting is divided into bounded reviewable commits, but approval and landing remain one coordinated
bundle:

1. **A3.1a — Code Standards normative core:** amend §2, §3, and the typed examples in the appendices.
2. **A3.1b — Code Standards supporting surfaces:** amend §1, §§4–9, outlines, and tracking to complete
   the §6 matrix without changing enforcement.
3. **A3.2a — Testing Strategy normative core:** amend §2, §3, and the proof examples in the appendices
   against the same Governance and A2 semantic baseline.
4. **A3.2b — Testing Strategy supporting surfaces:** amend §1, §§4–9, outlines, exception references,
   and tracking to complete the §7 matrix without changing enforcement.
5. **A3.3 — Reconciliation:** run the required count, range, traceability, exception-route, stale-reference,
   documentation, and repository gates over the combined candidate; correct only findings within A3 scope.
6. **A3.4 — Reapproval and landing:** perform a fresh review of the combined current candidate, resolve
   every substantive finding, obtain non-delegable project-owner approval, then update both approval
   records, versions, status/index entries, manifest, and changelog atomically before landing.

No intermediate drafting commit is an approved amendment or may claim that the A8 enforcement is active.
A3 closes only when both specifications are reapproved and landed together against the same Governance
v0.10 and A2 reference-semantics v2.1.0 baseline.

## A4 — Compiler-backed Class-A discovery and state seeding

Build/run the extractor from the governed checkout, combine semantic candidates with finite bootstrap intent, compute the closed runtime universe, classify every surface, assign activation state, and seed final contracts/registries. Implement the Code Standards §3.5.6 binding resolver against the contract/runtime-surface registries, assembly/compiler inventory, dependency graph, and repository tree; enforce the `not-applicable`/`na_fields` pairing; and prove missing, ambiguous, misclassified, nonexistent-path, broken-path-step, sentinel-mismatch, and duplicate-N/A cases fail. Intentional-disable anchors must resolve/evaluate; pending-integration records need owner/exit condition.

## A5 — Productionize audit/extractor and blind-spot fixtures

Productionize §5 around A2 semantics. Certifying runs source-build the Roslyn extractor with the pinned SDK; prebuilt binaries cannot certify.

## A6 — Migrate durable review ledger and proof mechanics

Implement durable review state, proof closure/freshness, activation/KD-W1 validation, and execution-result validation.

## A7 — Finite baseline only if required

A baseline remains finite. `intentionally-disabled` is never represented as a baseline waiver; it must satisfy its own machine-anchored contract.

## A8 — Activate full runner bridge, aggregator, and required status

Provision the pinned .NET SDK, source-build the extractor, require the full status, and verify Class-B required evidence cannot be substituted by static reachability.

## A9 — Synchronize guides and final strict review

Synchronize guidance to actual commands/authority and perform the final strict review using the non-self-invalidating subject-digest model.

Production architecture remediation begins only after its applicable prerequisites. Domain-owned Class-B instrumentation may proceed independently whenever its owning backlog/spec requires measurement.

---

---

# 12. Detailed change-impact matrix

| Area | New files | Modified files | Runtime behavior |
|---|---|---|---|
| Governance state | property-registry.json, integration-contracts.json, exceptions.json, runtime-surface-classifications.json, review-ledger.json; temporary bootstrap-runtime-surfaces.json during A1 only | project governance pointer/history only if needed | None |
| Architecture tooling | `tools/tests/test_assembly_tier_check.py`; later `tools/architecture-governance/*` including the source-built compiler-backed csharp-discovery extractor | `tools/assembly-tier-check.py` gains machine-report evidence; no second asmdef parser | None |
| Review tooling | durable review-ledger + tests/fixtures for run/finding state | adversarial-review SKILL.md, findings.py | None |
| Code Standards | none | #20 sections 2–5, appendices, SPEC_INDEX | Normative code rules only |
| Testing Strategy | none | #19 sections 2–7, appendices, SPEC_INDEX | Normative test/gate rules only |
| Master plan | none | master-development-plan.md pointer | None |
| CI | none | .github/workflows/ci.yml plus repository protection/ruleset configuration | CI-wire checker self-tests; activate the existing `Spec hygiene checks` required context, later full governance merge behavior |
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

## FM-GI-15 — Deliberately disabled code is misreported as accidental dormancy

Prevention: orthogonal activation state plus machine-verifiable disable anchor.

## FM-GI-16 — Static reachability passes while runtime gates never fire

Prevention: Class A/Class B evidence separation; W12-style instrumentation stays with the runtime owner.

## FM-GI-17 — Calibration tunes a subsystem that is not active

Prevention: component-owned tuning selectors plus the KD-W1 activation precondition.
---

# 15. Acceptance gates for completed governance integration

## Authority
- [ ] Governance approved/pinned to exact version and canonical content/blob digest; revision is provenance only.
- [ ] #19/#20 dual-approved against the same Governance/A2 semantic baseline.
- [ ] D1–D4 remain excluded.

## Discovery, identity, and applicability
- [ ] A1 machine-readable asmdef graph is emitted by the existing `assembly-tier-check.py` without Roslyn/schema dependencies or a second §3.5.2 parser.
- [ ] Existing ERR-020-002/003 resolution is preserved; checker self-tests run in CI; live protection state is verified and the existing `Spec hygiene checks` required context is activated where disabled.
- [ ] Compiler-backed C# discovery covers configured public/lifecycle/factory and explicit+implicit static-init mechanisms.
- [ ] A4 bootstrap contains only non-inferable runtime intent and is retired into final contracts within A4.
- [ ] Every assembly/runtime surface explicitly classified without suffix-only inference.
- [ ] Stable component identity survives ordinary file/type moves through selector history.
- [ ] Selector grammar distinguishes overloads and fails ambiguous/missing bindings.
- [ ] Applicability resolver and proof-closure resolver are deterministic/conflict-tested/fail-closed.
- [ ] `--changed` cannot weaken obligations.

## Contracts/public/bypass
- [ ] Blocking contract assertions use typed independently verifiable selectors/edges.
- [ ] Every schema-v1 ownership/path string resolves through the §3.5.6 binding grammar; schema shape acceptance alone never blocks.
- [ ] Lifecycle/testhost N/A uses the exact sentinel/pairing contract and rejects missing, duplicate, mismatched, or forbidden-field entries.
- [ ] Narrative semantic claims are not treated as machine proof.
- [ ] Public/bypass absence blocks only from compiler-backed demonstrated closed universes.
- [ ] Alternate hosts/testhosts classified.
- [ ] Structural classification and `activation_state` are independently represented.
- [ ] `intentionally-disabled` has a resolvable anchor, owner/decision reference, and reactivation condition.
- [ ] Drift/missing disable anchor fails; `pending-integration` has owner/exit condition.
- [ ] `[GT]`/declared tuning changes fail KD-W1 while an owning component is not active unless explicitly excepted.

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
- [ ] Certifying extractor is built from source at the governed checkout with pinned .NET SDK/compiler/config identity; no prebuilt binary certifies.
- [ ] Class-B runtime firing instrumentation remains domain-owned and is consumed as evidence.
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
| 0.45 | September 4, 2026 | — | **`ERR-019-003` — the `ERR-019-002` fix concealed an unmet MUST, caught by the Codex review bot on PR #358 (P1).** `ERR-019-002` split FR-TS-043 out of the KD-6 band correctly but described its automated `tools/checklist-auditor.py` mechanics as due to "arrive at Stage 0+1", while §7.1 defines Stage 0+1 as the first `src/` commit (KD-5) — reached long ago — and that script does not exist. The row published a reached, unmet Stage 0+1 MUST as half-satisfied: `ERR-019-001`'s defect class, re-created by the repair of `ERR-019-002`. Both §5.2 and §5.6 rows and the §5.2 preamble now read **ACTIVE (Stage 0+1) — non-conformant** with the stage marked REACHED, deliberately not deferred behind a new prerequisite (circular and fail-open). The gap is recorded, not fixed, and wider than the named row: `tools/spec5-schema-auditor.py` is absent too, so all three §7.1 tooling deliverables are missing — new `open-issues.md` entry, active count 19 → 20. No #19/#20 FR statement, schema, executable semantics, code, workflow, required status, or `SPEC_INDEX.md` state changed; A3.4 reapproval and A8 enforcement remain pending. |
| 0.44 | September 4, 2026 | — | **Residual from the same external review pass.** v0.43 made a *missing* count claim `BROKEN` and status-affecting but left the multiple-match branch printing `UNPARSED` without setting `check_broken`, so a **duplicated** claim line still exited 0 — the same structural class, since the check cannot tell which claim is authoritative. Now `BROKEN` and exit 1, with the EXIT-CODE CONTRACT naming all three structural sub-cases. The review also correctly identified that v0.43's mutation set never constructed a duplicate; five paths are now proven by execution (healthy 0/OK, absent 1/BROKEN, wrong 0/FAIL, duplicated 1/BROKEN, restored 0/OK) and no live branch emits `UNPARSED`. The script's class-3 header comment is swept from "active/resolved" to "active/archived". No spec file, schema, executable semantics, workflow, required status, or `SPEC_INDEX.md` state changed; A3.4 reapproval and A8 enforcement remain pending. |
| 0.43 | September 4, 2026 | — | **Three external-review findings on the v0.42 commit, all valid, all fixed.** (1) The open-issues second count is renamed `resolved` → `archived` in `check_drift.sh`, the published claim and `doc-consistency-check.py`: the archive holds superseded parallel records annotated "not a resolved issue", so counting archive bullets as `resolved` was semantically false. No composition split is published — a marker grep returns 11 of 53, mostly false positives. That checker's group stays dormant (proven, not assumed) because `project-reference.md` is outside its `CURRENT_STATE` list; `check_drift.sh` guards the claim, and bringing the file into scope remains the separate decision recorded September 3. (2) `landing-close-out/SKILL.md` still told agents to update root `CLAUDE.md`'s OPEN ISSUES — the section removed by the compact restructure — which is the contract that would have recreated the drift v0.42 diagnosed; repointed to `docs/tracking/open-issues.md` with the two surfaces that move with an entry. (3) `BROKEN` (check cannot find its surface, so it verified nothing) is now status-affecting and exits 1; a count disagreement stays advisory at exit 0, and the skill now requires scanning the report rather than trusting the exit status. All four paths proven by execution. No spec file, schema, executable semantics, workflow, required status, or `SPEC_INDEX.md` state changed; A3.4 reapproval and A8 enforcement remain pending. |
| 0.42 | September 4, 2026 | — | **The last two A3.3 leftovers closed.** `check_drift.sh`'s open-issues count check was found reading root `CLAUDE.md`, which has carried no OPEN ISSUES section since the compact restructure — it printed `UNPARSED` on every run and compared nothing, while the real claim in `docs/agent-guides/project-reference.md` drifted to 15 active / 46 resolved against a true 21/51. Repointed, with a missing surface now reported as `BROKEN` rather than shrugged at, and both paths proven by execution; the EXIT-CODE CONTRACT is unchanged. `open-issues.md` carried three entries for the football-judgment proxy review rather than two, which is why its index could not be reconciled; the two superseded parallel records are archived **verbatim** under the PR #305 precedent, with the one fact they alone carried merged into the survivor and verbatim-ness proven by execution. Counts re-derived: 19 active / 53 resolved, now agreeing across `open-issues.md`, `doc-consistency-check.py`, `check_drift.sh` and the index. No spec file, schema, executable semantics, code, workflow, required status, or `SPEC_INDEX.md` state changed; A3.4 reapproval and A8 enforcement remain pending. |
| 0.41 | September 4, 2026 | — | **Self-review of the v0.40 follow-up; two defects in it corrected.** The `ERR-019-002` fix had corrected §5.2/§5.6 and left §5.2's own preamble and column-semantics note asserting the pre-split state, both falsified by its own FR-TS-043 split — prose contradicting the table beneath it, which is that ERR's own defect class reproduced by its fix. Corrected, and the ERR's closing pointer now records the fix as spanning `section-5.md` v0.6 **and** v0.7 instead of citing v0.6 as the resolved state. The #19 §2.2 footer added at v0.40 claimed the partition table carries per-partition counts, which it does not; reworded to the contiguous/non-overlapping/spans-001…097 property and re-derived mechanically. Re-verified: no other #19 prose falsified, both tables covering 97 FRs exactly once with zero §2.2 mismatches, and the #20 outline corrections exact against §3.5.2 itself. `doc-consistency-check.py` did not catch the stale ERR pointer — it excuses log-body regions — so it was found by reading, not tooling. No FR statement, schema, executable semantics, code, workflow, required status, or `SPEC_INDEX.md` state changed. |
| 0.40 | September 4, 2026 | — | **A3.3 follow-up — every recorded-not-fixed item closed, by owner direction.** `ERR-019-002` filed and RESOLVED: #19 §5.2 **and §5.6** published an Activation Stage contradicting §2.2 for eight rows (the v0.39 report caught only §5.2), against §5.2's own note binding that column to §2.2 — six Stage 0 MUSTs buried in Stage 0+1 bands, FR-TS-039 at the wrong stage behind the wrong deferral, FR-TS-043 published ACTIVE against Stage 0+1. All eight split in both tables; both now reproduce §2.2 for all 97 rows, verified mechanically, with no FR statement, level, or §2.2 value changed. #19 §3.6.5 gains the §2.1 exception boundary it alone omitted; #19 §2.2 gains the FR-total footer and renumbering prohibition mirroring #20 §2.2.10, closing a pre-A3 asymmetry because the two specs reapprove as one bundle. The three retired arrow-chain sites in #20's outlines are corrected, **reversing `ERR-020-002`'s recorded "deliberately not changed" disposition** — annotated at the disposition rather than deleted. `project-reference.md`'s OPEN ISSUES index resynced 14 → 19, adding both live #19/#20 governance issues and rewriting two bullets that published claims their owning entries retract. Recorded, not fixed: `open-issues.md`'s three overlapping football-judgment entries. No #19/#20 FR statement, schema, executable semantics, code, workflow, required status, or `SPEC_INDEX.md` state changed; A3.4 reapproval and A8 enforcement remain pending. |
| 0.39 | September 4, 2026 | — | **A3.3 reconciliation gate run over the combined #19 + #20 candidate.** The seven required gates were run: count, range, traceability, exception-route and repository passed as found — 81 FR-CS and 97 FR-TS rows, contiguous with no duplicate or gap; #20 §5.5 at 83 traceability rows and #19 §5.2/§5.6 covering 001 … 097; every cited `FR-AG-` id, §3.5.6/§3.5.7, §3.11.1–§3.11.11 and Appendix A–G reference resolving; #19 Appendix G and #20 Appendix F examples **validated against the frozen A2 schemas** rather than read; the §2.1/§2.3/§7.2 and Mode 3 exception boundaries in place; and `doc-consistency-check.py`, `assembly-tier-check.py` and 166/166 tooling tests green with `SPEC_INDEX.md` untouched and no active-gate claim anywhere. Two findings corrected inside A3 scope: ten out-of-order version-history rows across six #19 files (row order only, no row edited and no version bumped — the `76389c6e` precedent from the #20 side), taking `recurring-defect-lint.py` from 10 ERROR back to **0 ERROR tree-wide**; and a false #20 status assertion in `docs/agent-guides/project-reference.md`'s OPEN ISSUES index, which still had `ERR-020-002` awaiting sign-off and §3.5.2 placing 19 of 35 assemblies, both closed August 17, 2026 against a tree that now seats 35 of 35. The retired arrow chain in #20's two outlines was found and left as found under `ERR-020-002`'s recorded "deliberately not changed" disposition. Three observations are recorded for A3.4 rather than drafted here: #19 §3.6.5's silence on the §2.1 exception boundary, the absent #19 FR-total footer and renumbering prohibition that #20 §2.2.10 carries, and eight pre-A3 baseline rows where §2.2's per-FR activation differs from §5.2's band summary. No #19/#20 normative text, schema, executable semantics, code, workflow, required status, or `SPEC_INDEX.md` state changed; A3.4 reapproval and A8 enforcement remain pending. |
| 0.38 | September 3, 2026 | — | **A3.2b Testing Strategy supporting-surface synchronization.** Corrects §7.3/§7.5 proof pointers and reserves example-only IDs; synchronizes §1/§4–§9, both outlines, FR-to-verification through 097, architecture-proof negative fixtures, four-gate topology, owning-runner/result binding, Governance convergence, exception boundaries, and tracking. Live-repository audit closes only D1 (NUnit 3.14.0 / NUnit3TestAdapter 4.6.0 / `dotnet test`) and D4 (GitHub Actions); D2/D3/D5–D8 remain deferred. Review closure additionally splits FR-TS-093 and FR-TS-079 to their correct Stage 0 rows, moves the stranded scenario-manifest encoding/extension decision to overdue D9, records the FR-TS-078 GitHub Actions pin in `src/CLAUDE.md`, and files/resolves `ERR-019-001` for the false FR-TS-075…080 activation state while leaving the missing pre-commit/nightly/local-runner conformance gap explicitly open. The May 15 #19 baseline remains operative; A3.4 reapproval and A8 enforcement remain pending. No schema, executable semantics, runtime code, workflow, required-status, or `SPEC_INDEX.md` state changed. |
| 0.37 | September 3, 2026 | — | **A3.2a review correction.** Corrects §3.11.6 of Spec #19: the structural failure-class obligation was stated as SHOULD, which under §2.1 permits omitting a detector with rationale, while Governance §5.3 states it as MUST detect. The subsection now carries the MUST, defers to Governance §5.3 (FR-AG-027) as the authority for the class list, and reproduces that list verbatim. The prior spec-local paraphrase had dropped `unreachable implementations` and `duplicate construction`, conflated `orphan registrations` into "orphan implementations", and substituted "unsupported activation-capable public surfaces" for `public types that imply unsupported integration paths`. Mirrors the Governance v0.6 / AG-A0-002 resolution of the same defect class in §5.5. No new failure class, proof semantics, schema, workflow, or enforcement is introduced. |
| 0.36 | September 2, 2026 | — | **A3.2a review correction.** Records FR-TS-063 as an authorized consistency amendment in §7.2/§7.4: quarantine suppresses only an eligible functional-gate blocking effect and cannot satisfy or waive a separately required architecture-proof obligation. No new quarantine mechanism, proof semantics, schema, workflow, or enforcement is introduced. |
| 0.35 | September 2, 2026 | — | **A3.1a automated-review correction.** Records that frozen schema `1.0.0` / reference semantics `2.1.0` validate integration-contract shape but do not resolve ownership/path strings or enforce `na_fields` pairing. Code Standards §3.5.6 now defines the exact binding vocabulary and `not-applicable` representation; A4 owns executable cross-registry/path resolution, sentinel pairing, and discriminating failure fixtures before either surface may support a Machine blocker. No schema, executable semantics, workflow, or enforcement changed. |
| 0.34 | September 2, 2026 | — | **A3.1a review correction.** Aligns proposed FR-CS-078 with Governance FR-AG-025: known activation bypasses are prohibited or explicitly **classified**, not narrowed without rationale to only "supported." The downstream draft carries the same correction. No enforcement, schema, executable semantics, or runtime behavior changed. |
| 0.33 | September 2, 2026 | — | **A3 preflight correction and bounded execution sequence.** Removes the live contradiction immediately below the A2 `CLOSED` declaration that still said conditions 6/7 were open and A3 blocked. Each specification is divided into a normative-core slice and a supporting-surface slice, followed by combined reconciliation and atomic reapproval/landing. Intermediate commits remain unapproved drafts and make no A8 enforcement claim. A3 must close against one Governance v0.10 / A2 semantics v2.1.0 baseline. No #19/#20 normative file, schema, executable semantics, code, workflow, or enforcement changed. |
| 0.32 | September 2, 2026 | — | **A2 IS CLOSED. A3 is unblocked.** Row 7 satisfied: the approved candidate merged to `main` at `693db56`, and the landed material subject was **recomputed** — not assumed — to `4160b164…`, identical at `1f0e68a` (reviewed by `A2-RUN-011`), `9954e90` (approved), `0221491` (branch head), `693db56` (merge commit) and `origin/main`. Nothing changed on the way in, which is the check a digest-bound approval exists to make possible: "the PR merged" is not evidence that what landed is what was approved. All seven conditions now hold; closure record → v1.0 and `CLOSED`. Deliberately **not** done: no review run is marked `CONVERGED` or carries `final_review` — FR-AG-019/020 convergence is a separate question from FR-AG-018's fresh review, the seven-condition gate never required it, and runs are immutable snapshots that must not be retro-labelled; the test enforcing that was left in place rather than relaxed to fit the new state. Closure binds one artifact by digest and does not put the contract beyond revision: any later change inside the material subject is a change to an **approved** contract and takes the A5/A6 schema-evolution route. Unblocked is not started — beginning A3 remains a separate decision. Records only; discovery holds at 149/9/8 = 166. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.31 | September 1, 2026 | — | **A2 closure condition 6 recorded: project-owner approval of the candidate at `9954e90`**, material subject digest `4160b164…` — the same subject `A2-RUN-011` reviewed at `1f0e68a`, unchanged since. The approval is bound to that digest and does not transfer: any change inside the material subject returns row 6 to PENDING and requires a fresh approval, while excluded files (tracking prose, review ledger, CI configuration) may change without disturbing it. **Row 7 — landing the approved candidate on the base A3 builds on, with the landed digest verified — is now the only outstanding condition.** No run is marked `CONVERGED` and none carries `final_review`; that lock is tied to row 7 and is not what approval releases. A2 stays OPEN until the candidate lands; A3 stays BLOCKED. Closure record → v0.14. Records only: no schema, executable semantics, fixture, or finding changed; discovery holds at 149/9/8 = 166. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.30 | September 1, 2026 | — | **A2 closure condition 4 is satisfied.** `A2-RUN-011`, an independent review of `1f0e68a` as pushed, returned **no findings** — the first round in this series after which nothing followed into the contract, which is precisely what row 4 has required since its v0.21 retraction. The material subject digest `4160b164…` recomputes identically from `1f0e68a` and from the current tree, and `test_closure_condition_4_is_only_claimed_with_a_review_of_this_tree` refuses the cell otherwise; the claim is machine-checked rather than argued. Round 11 additionally verified Governance §3.3 property fields and §7.1 exception fields, carried as explicitly unverified since v0.20, and independently confirmed `Spec hygiene checks` at 166/166 with 0 skipped. Post-review corrections are confined to files the material subject excludes by construction — the ledger entry recording the run, tracking prose, and stale fixture names in a CI comment and in the closure record's §1, one of which (`test_the_current_artifact_has_not_yet_been_reviewed`) the round did not catch. A fixture pinning cited test names is deliberately not landed: it belongs to the material subject and would re-open row 4 for a twelfth round, so that trade is the owner's to make alongside the next material change. Closure record → v0.13. **Rows 6 (owner approval) and 7 (landing) remain PENDING and are not agent-satisfiable; A2 stays OPEN and A3 stays BLOCKED.** Discovery unchanged at 149/9/8 = 166. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.29 | September 1, 2026 | — | Hardens v0.28 against its own removal, at the round-10 reviewer's recommendation and before round 11. `fetch-depth: 0` is a single line whose deletion would silently return both history-dependent fixtures to skipping with the job still green. A missing-history condition is now a failure whenever `GITHUB_ACTIONS=true` — the trigger is the CI marker, not an opt-in flag, because a guard that must be remembered is the class of guard this replaces — with `GOVERNANCE_REQUIRE_HISTORY=1` additionally arming it elsewhere; local skips are preserved so a shallow clone gives an honest skip rather than a red suite. All three skip paths route through one helper: missing named revisions in either fixture, and incomplete ledger publication history. `test_the_ci_history_guard_is_not_inert` pins both directions and both triggers, which is `A2-R10-001`'s finding applied to the guard itself. Recorded consequence, measured at `1635aa3`: the publish→bind two-step must now be pushed as a pair, because at a publishing commit the equality regression fails rather than skips — the rule working, previously masked by the shallow checkout. **No frozen executable semantics changed and no `REFERENCE_SEMANTICS_VERSION` bump is owed**; the candidate does change, so it lands before round 11. Governance fixtures 148 → 149; discovery runs 166. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.28 | September 1, 2026 | — | Acts on round 10's evidence note rather than only recording it, and corrects that note. Both history-dependent fixtures — `test_every_recorded_digest_matches_the_revision_it_names` and `test_status_timestamps_equal_first_publication_commit_time` — had skipped in **every** CI run of this candidate, because `Spec hygiene checks` used the `actions/checkout` default depth of 1. The digest chain and the timestamp equality rule the A2 record rests on were therefore verified only on contributors' local clones, never by the gate; a green badge was not evidence of a check that never ran. `spec-hygiene` now sets `fetch-depth: 0` — that job only, every other job stays shallow — and all ten ledger-named revisions were confirmed ancestors of the candidate head, so the fetch reaches each. v0.27 framed this as a recording correction that would stand; it is now fixed at the source, and its parenthetical "164 tests" should read 165. Closure record → v0.11. Workflow and tracking only: no fixture, schema, semantics, or finding changed, and discovery holds at 148/9/8 = 165. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.27 | September 1, 2026 | — | Round 10, an independent review of the round-9 remediation, found one defect. `A2-R10-001` (Medium): rounds 8 and 9 changed three admission rules — activation-baseline admission, proof execution/subject binding, and disable-anchor validation — while `REFERENCE_SEMANTICS_VERSION` stayed at `2.0.0`. That value is a field of the proof-closure subject, so it is an **input to** `subject_scope_digest`, and `assess_proof_freshness` compares it by equality to raise `proof-semantics-changed`; two materially different policies under one value defeat that identity contract. The module's own history sets the rule it departed from — `1.0.0 → 1.9.0` bumped once per semantic-change commit, and v0.19 reserved MAJOR for the import-contract break — so the value advances to **2.1.0**, MINOR, covering both rounds together and restored rather than back-dated. The versioning policy is now stated at the constant, since leaving it implicit is what let it lapse. No proof artifact exists in the repository, so nothing recorded is invalidated: the bump is inert today and made for honest signalling, the same reasoning v0.19 applied. `test_reference_semantics_version_is_pinned` existed throughout and did not help — it asserts the value is what it is, never that it moved when the semantics did — and that limit is now recorded in the pin itself; a new fixture locks the constant to every document citing it. The closure record also now separates **local full-history** discovery (164 tests, 0 skipped) from **shallow-CI** discovery (2 history-dependent fixtures skip by design under `fetch-depth: 1`), which is a recording correction, not a defect. Governance fixtures 147 → 148; discovery runs 165 across three suites. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.26 | September 1, 2026 | — | Round 9, a verification pass over the round-8 corrections, found that one of them was a regression. `A2-R9-001` (Medium): the `A2-R8-001` anti-ratchet fix rejected every baseline addition measured against a trusted prior, but §3.9 declares `inactive → migration` legal and an `inactive` baseline is required to be mechanically empty, so entering migration necessarily adds items and was left unreachable — including for this repository's own committed `temporary-activation-baseline.json`, which is `inactive` and empty. Reproduced against that document: the transition raised at `c927a95` and was accepted at `a034fc3`, so the round-8 fix introduced it. Additions are now permitted only on the `inactive → migration` edge, the single act §3.9 requires to exist; the exemption cannot be re-entered because no transition returns to `inactive`, so the catalogue is populated exactly once and growth from a migration prior stays rejected. No fixture caught the regression because every `prior_baseline` test passed a *migration* prior — round 8's own lesson about fixture-bounded differentials, recurring inside the commit that recorded it. `A2-R9-002` (Low): this file's header stated **Version:** 0.18 while its version history had advanced to 0.25, so seven revisions of citations resolved against a document self-describing as an earlier one; the header is corrected here and the drift is recorded rather than silently repaired. Also reconciles the two apparently opposed rationales inside `validate_proof_artifact` — an absent execution is an obligation the contract does not state, a required field's meaning is not — and resolves the normalized rather than the raw disable-anchor selector. Governance fixtures 143 → 147; discovery runs 164 across three suites. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.25 | September 1, 2026 | — | Round 8, the automated review on pull request #347, found three defects in the frozen contract itself — the first round since round 3 to do so rather than in the record-keeping, which is a caution against reading the intervening clean-looking rounds as coverage of the contract. `A2-R8-001` (Medium): activation-baseline additions were rejected only after sealing, so an unsealed migration baseline could take a new violation into `items` and into `current_violation_ids` in one revision and the live-set comparison would see nothing new — an indefinite ratchet. §3.9 states "New violations fail" without qualification; additions are now measured against the trusted prior baseline whatever its seal state, with shrink and steady-state still legal. `A2-R8-002` (Medium): `validate_proof_artifact` never compared an execution's `subject_scope_digest` with the artifact's, leaving the field decorative and letting a passing record from an older or unrelated subject certify the proof; equality is now required, recorded as a deliberate narrowing because the plan defines no subsumption relation between scopes and widening it is an A5/A6 decision. `A2-R8-003` (Medium): an `intentionally-disabled` contract whose `disable_anchor` was `{}` passed the document validator while the canonical schema required `selector`, `operator` and `expected` — a live schema/semantics divergence, and one the differential could not detect because no fixture carried a malformed anchor. The anchor's typed shape now has a single owner called from both the contract validator and the evaluator, and malformed-anchor fixtures close the coverage gap. Round 7 (`A2-R7-001`, the owner's PR #346 provenance landing) is recorded for completeness. Governance fixtures 139 → 143; discovery runs 160 across three suites. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.24 | September 1, 2026 | — | Follow-up correction to review-record provenance semantics: `at` is first-publication commit time, not an unknown point within the review→publication interval. The prior v0.23 wording claimed publication provenance while its test enforced only interval membership. The regression now requires exact equality to first publication and separately requires publication after the reviewed artifact; `A2-R6-001` is corrected to `c349fb6`'s commit time. A2 closure row 4 remains open. No frozen schema/semantics mechanism, Governance v0.10/A0, or #19/#20 normative file changed. |
| 0.23 | September 1, 2026 | — | Third independent review; condition 4 stays open. `A2-R6-001` (Medium): round 5's timestamp remediation still recorded false provenance. A finding's Open event was stamped at the commit time of the artifact reviewed — `11547d4` at `19:24:32Z` for round 4 — but an independent review necessarily happens *after* the artifact it reviews is pushed, so the record placed each discovery at or before the thing discovered; and the resolution stamps were build times (`20:11:08Z`) described as the commit time that carried the fix (`7d4e949` at `20:13:03Z`). The regression checked only `<=` wall clock and monotonicity and could see neither error. The exact review times are **not recoverable** and the ledger no longer pretends otherwise: `at` is now the time a transition was RECORDED, derived from the commit that first published the finding, with the reviewed and resolving revisions carried in `evidence`. The regression brackets every timestamp between the reviewed artifact and the publishing commit, with a **strict** lower bound — a first cut used `>=` and a probe showed it did not catch the very defect it replaced, because the bad value was exactly the reviewed commit's timestamp. Dated-at, dated-before and dated-after-publication are all proven to fail. Governance fixtures hold at 139; discovery runs 156 across three suites. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.22 | September 1, 2026 | — | Second independent review of this candidate; condition 4 stays open. `A2-R5-001` (Medium): the historical-digest verification skipped unavailable revisions one at a time and skipped the test only when none resolved, so a shallow checkout — the CI default — could verify one digest of five, ignore the rest, and report PASS under a name asserting all of them. Verification is now all-or-nothing and names the missing revisions; proven to skip wholesale on partial history and still fail loudly on a wrong digest. `A2-R5-002` (Medium): every `A2-R4-*` status event was stamped `21:00:00Z` while `5ebc3f7`, the commit asserting those events complete, was created at `19:51:29Z` — a durable record of events that had not happened. Timestamps now derive from real commits, a finding being raised at the commit time of the artifact reviewed and resolved at the commit time carrying the fix, with a regression rejecting future-dated or out-of-order history. `A2-R5-003` (Low): `A2-R4-002` attributed FR-AG-032's reproducibility text to FR-AG-034; both are now cited with their own text. Also drops the round-digest distinctness assertion — governance does not require it, since two rounds may legitimately review an unchanged subject — and removes a self-referential test parameter that fed the ledger digests taken from the ledger. Row 4's closure cell is now mechanically tied to the ledger. Governance fixtures 138 → 139; discovery runs 156 across three suites. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.21 | September 1, 2026 | — | **Retracts v0.20's claim that A2 closure condition 4 was satisfied**, on the first independent review of this candidate. `A2-R4-001` (High): round 3 reviewed `678f0f2`, the material subject then moved 150 lines — the `A2-R3-001` fix, its schema change and its tests — and `11547d4`, the commit carrying the completion claim, was itself never reviewed. The gate's pushed-candidate wording is stronger than FR-AG-018's and cannot be weakened by the party satisfying it; row 4 returns to PENDING and a regression now fails if any round claims the current tree without a review of it. Round 3's digest is corrected to the tree it actually reviewed. `A2-R4-002` (Medium): every recorded digest is now recomputed from the commit its scope names — v0.20 verified only the latest while claiming the historical values were reproducible, and distinctness is not identity; the shallow-clone bound is stated and skips explicitly rather than passing silently. `A2-R4-003` (Low): `tools/tests/test_recurring_defect_lint.py` adds nine mixed positive/negative context fixtures and pins the adjacent-negation suppression bound, which pre-dates this work and cannot be narrowed without re-raising three genuinely wrapped negations; the reviewer's bullet-in-a-negative-list concern was checked and is correct suppression, a non-goals bullet being elliptical. Conditions 1, 2, 3 and 5 stand. Governance fixtures 137 → 138; discovery runs 155 across three suites. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.20 | September 1, 2026 | — | Records satisfaction of A2 closure conditions 1–5 and the review evidence behind them; conditions 6 and 7 stay open. Three rounds land in the durable `review-ledger.json` under series `A2-SCHEMA-FREEZE`, each carrying the material subject digest of the tree it actually reviewed rather than one digest stamped across all three; the latest recomputes mechanically in `DurableReviewLedgerTests`, so the digest bundle is verifiable rather than asserted. Nine findings, all `Blocker` / `Resolved`, each citing the specific pre-existing condition it made false — per Governance §1.6 none cites a gate this review authored. Round 3's own finding, `A2-R3-001`: a property registered under an `FR-CS-`/`FR-TS-` id captured that requirement's waiver routing through `exception_route`, because the property branch is evaluated first and `property_id` carried no namespace constraint — the crossing §3.6 forbids. Both the schema and the semantics now reject it from one control-data source, preserving §3.6's carve-out because an admitted property cites an FR requirement instead of taking its id. Also teaches `recurring-defect-lint.py` to see three legitimate ERR-041-012 negations it was re-raising — markdown-emphasised negations, "does not … registered" clauses, and a bullet inheriting its non-goals lead-in — verified not to blind it against four constructed positives. Governance fixtures grow 128 → 137; with 8 assembly-tier fixtures, `tools/tests/test_*.py` discovers 145. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.19 | September 1, 2026 | — | Second-review remediation; A2 stays OPEN and A3 stays BLOCKED. Adds the missing executable validator for the per-proof artifact contract, so every frozen §3.1 machine contract now has one. Makes the review-ledger and activation-baseline validators fail closed on an omitted trusted prior, live violation set, or current final-review digest, matching the property registry's existing sentinel posture; `strict_activation` is deliberately excluded because it adds a requirement rather than relaxing one. Pins every schema's canonical `$id` so relative `$ref` resolves by URI rather than by incidental filename lookup, and adds `tools/architecture-governance/schema_validator.py`, a bounded Draft 2020-12 validator over exactly the keyword subset these schemas use — it raises on any keyword it does not implement, so the new one-directional differential (every semantically accepted fixture must also satisfy its frozen schema) cannot pass vacuously. **Restores `REFERENCE_SEMANTICS_VERSION` to v2.0.0.** v0.17 set 2.0.0, v0.18 reverted it to v1.10.0 as an "unpublished label", and that reversion was wrong on two counts: the module now raises at import without `common.schema.json`, which breaks any standalone import contract and is a major change under semver; and the reverted sequence published 1.9.0 → 2.0.0 → 1.10.0, a regression in a value `assess_proof_freshness` stamps into every proof snapshot. The comparison there is equality, not ordering, so the restore is mechanically inert and the choice is one of honest signalling. Governance fixtures grow 104 → 128; with 8 assembly-tier fixtures, `tools/tests/test_*.py` discovers 136. Governance v0.10/A0 and #19/#20 normative files are unchanged. |
| 0.18 | September 1, 2026 | — | Corrects v0.17's invalid equation of implementation/merge with A2 closure. Adds an explicit seven-condition A2 gate: eight-category scope map, schema/control-data single source, exact executable verification split, fresh review over a pushed candidate, terminal finding state, non-delegable project-owner approval, and landing of the approved digest on A3's base. `common.schema.json` becomes the single machine source consumed by pure-stdlib reference semantics v1.10.0, now v2.0.0 — see v0.19, which restores the major bump this row had reverted. A2 is IMPLEMENTED but OPEN; A3 remains BLOCKED. |
| 0.17 | September 1, 2026 | — | **A2 schema freeze completed.** Adds canonical Draft 2020-12 schemas for classification, bootstrap intent, integration contracts, applicability, properties, exceptions, reusable proof, review state, and temporary activation baseline; seeds the seven durable state artifacts at schema v1.0.0. Reference semantics v2.0.0 now enforces trusted-merge-base property-history immutability and Governance §3.1 transitions, property-only exception routing with #19/#20 owner separation, Governance §4.1 Disposition×Status and §4.7 convergence/freshness, and finite baseline transitions with a mechanically empty strict state. A3 remains blocked only until this slice lands. |
| 0.16 | September 1, 2026 | — | A2 selector type-ID canonicalization after Codex review: pins every selector type ID to the C# XML documentation ID type-signature convention emitted from compiler symbols, including byref `@`; adds a value-vs-ref overload regression proving `M(System.Int32)` and `M(System.Int32@)` resolve distinctly without introducing a redundant `parameter_ref_kinds` field. Selector-v1 shape, execution truth, applicability, proof closure, Governance v0.10/A0, and #19/#20 normative files remain unchanged. |
| 0.15 | September 1, 2026 | — | A2 residual hardening after verification of v0.14: normalizes every enum-valued untrusted JSON boundary through typed semantics errors instead of host-language `TypeError`; makes narrower matching `change_types` sets outrank broader matching sets while preserving the surface-specificity ordering; and makes non-strict missing change context explicitly diagnostic rather than silently indistinguishable from no applicable context-gated rule. Execution truth and the v0.14 subject-side change-context model are otherwise unchanged. Governance v0.10/A0 and #19/#20 normative files remain unchanged. |
| 0.14 | August 31, 2026 | — | A2 change-context model correction after verification of v0.13: moves Governance §5.2 `change_type` from obligation/rule payload into the evaluated applicability subject; strict resolution and proof closure now require explicit current change context; rules may optionally filter with `change_types`; matching context-specific rules mechanically outrank otherwise-identical generic rules; persistence/resource closure reads only the current subject context. This removes both v1.5 over-inclusion and v1.6 omission-driven false freshness. Execution truth is unchanged. Governance v0.10/A0 and #19/#20 normative files remain unchanged. |
| 0.13 | August 31, 2026 | — | A2 closure/execution hardening after verification of v0.12: adds typed Governance §5.2 `change_type` to applicability so serializer/schema/resource closure edges are activated only for persistence-boundary or external-resource triggers; applies that condition across all four proof classes; restricts bounded substitutes to `excluded`/`unavailable`/`not-run` and forbids them from converting `failed`/`skipped`/`runner-failed`; and aligns proposed FR-TS-094/096 to that frozen behavior. Governance v0.10/A0 and #19/#20 normative files remain unchanged. |
| 0.12 | August 31, 2026 | — | A2 cross-surface authority correction after hostile sweep: removes the invented persistence/external-resource fifth proof class and freezes Governance's exact four proof classes; maps all six structural classifications into the three applicability fallback scopes and defines fallback precedence; makes `--changed` rerun whenever changed material is inside the derived closure; and requires A2 executable execution-truth/bounded-substitute semantics before A3. Governance v0.10 and A0 approval are unchanged. |
| 0.11 | August 31, 2026 | — | Post-A0 evidence correction only: review-record pointer advances v1.7 → v1.8 after Codex correctly identified that §11 A0 condition 2 required a stable reviewer identity rather than the relative phrase `same assistant`. Governance v0.10, its Approved status, canonical adoption digest, and A0 CLOSED state are unchanged. |
| 0.10 | August 31, 2026 | — | **A0 CLOSED.** Records project-owner human sign-off, Governance v0.10 `Draft → Approved`, and the post-status-edit canonical adoption SHA-256 `aa1792bf143fb3bc1066176dedb33abc4097045e7d089844edf05ccf9961d8f6` (Git blob `76502282f205f5c4fd77c79c3309766c4dbd4498`). Removes the stale pre-A0 claim that SPEC_INDEX alignment is an A0 prerequisite; §9.7/downstream registration remains owned by later stages. A2 is now the next stage. No #19/#20 normative file, code, workflow, or runtime behavior changed. |
| 0.9 | August 31, 2026 | — | Synchronizes with Governance v0.10 after hostile review: rejects invalid Disposition/Status pairings, requires every finding to reach its mapped terminal Status before convergence, and updates A0's findings gate accordingly. Governing authority reference advances to v0.10. No #19/#20 normative file, code, workflow, or runtime behavior changed. |
| 0.8 | August 31, 2026 | — | Synchronizes the plan with Governance v0.9's settled four-Disposition/five-Status model. Replaces all live `runtime component` uses with `runtime-bearing component`; aligns proposed FR-CS-076 on canonical `teardown`; makes the A0 finding condition test `Disposition: Blocker` plus `Status: Open`; completes the durable finding field set with `round_introduced`; and replaces the stale `Open → Dispositioned` lifecycle summary. A0 remains unapproved pending human sign-off; no #19/#20 normative file, code, workflow, or runtime behavior changed. |
| 0.7 | August 31, 2026 | — | A0 gate boundary made explicit, removing a circular dependency. Non-negotiable 1 required a "completed self-checklist" at A0 while this plan assigns Governance §9.7's downstream landings to A3–A9 — so A0 could not close until stages that depend on A0 had run. Governance §9 in fact carries two bars: the §9 preamble gates becoming *authoritative* (§9.1–§9.6), and §9.7 gates being *fully adopted*. A0 is now scoped to the first only, with five closure conditions including a fresh recorded review per FR-AG-018 and an explicit digest-after-status-edit ordering. A0 explicitly does not require the property registry, finding ledger, review tooling, or #19/#20 amendments. Governing authority reference updated v0.4 → v0.5. New `docs/tracking/a0-governance-adoption-review.md` carries the review. No #19/#20 normative files, code, or CI changed. |
| 0.6 | August 30, 2026 | — | A1c amended and COMPLETE. The original "observed blocked merge" condition is not measurable: `mergeable_state: blocked` is returned for an unmet approving review, an unresolved conversation, a pending required check, or a failing one, and does not name which. A1c now closes on the full required-checks list read in settings (configuration), the check observed reporting a real conclusion in both arms, and a paired two-arm comparison varying exactly one required check — a single-arm reading is explicitly not acceptable. Satisfied August 30, 2026: `d689f2b` all six required green -> `unstable`; `d497a4d` differing only by one stale `Decision Tree #7` line -> `Spec hygiene checks` failure -> `blocked`. New `docs/tracking/a1c-enforcement-evidence.md` v1.1 captures the run/job ids, the full required list, and the required-approving-reviews 1 -> 0 owner decision. The arm commits are preserved as remote branches `evidence/a1c-green-arm` / `evidence/a1c-red-arm` (the squash orphaned them). Two claims from the first draft are withdrawn as false: that a required-but-`skipped` context would freeze merges (GitHub treats `skipped` as satisfying a required check), and that the 1-approval rule was self-approval ceremony (GitHub forbids authors approving their own PRs, so it was unsatisfiable with one maintainer). Non-negotiable 12 unchanged. |
| 0.5 | August 29, 2026 | — | Repository-reality correction for A1: records ERR-020-002/003 as already resolved; recognizes `tools/assembly-tier-check.py` and its existing `Spec hygiene checks` wiring; removes the obsolete A1b dependency repair; prohibits a second §3.5.2 parser; scopes A1a to JSON complete-graph evidence, classification-aware digests, all-assembly cycle visibility and CI-wired checker tests; scopes A1c to activating the existing required status after re-reading live protection state. A2+ semantic/governance sequence otherwise unchanged. |
| 0.4 | August 28, 2026 | — | Two-track rollout and activation-state hardening: asmdef-only A1 first slice; ERR-020-002/003 repair and early objective enforcement before A8; compiler-backed Class-A reachability at A4; domain-owned Class-B runtime evidence; orthogonal activation state with machine disable anchors; KD-W1 tuning precondition / proposed FR-TS-097; source-built Roslyn extractor with pinned .NET SDK/compiler identity. No #19/#20 normative files, SPEC_INDEX, file-manifest, code, CI workflow, or runtime implementation changed. |
| 0.3 | August 28, 2026 | — | End-to-end implementation hardening: separates evidence subject identity from Git/artifact provenance; removes self-referential governance/property pins; closes A1/A4 root bootstrap; freezes executable selector/identity/applicability/closure semantics at A2; requires compiler-backed C# discovery including implicit type initialization; defines stable component/symbol identities and selector history; derives proof-class dependency closure; records exact execution/failure/mutation truth; splits durable review runs from findings; bridges owning tests to mandatory runner results/CI aggregation; preserves A0–A9, Governance authority split, and ERR-020-002/003 staging. No #19/#20 normative files or implementation changed. |
| 0.2 | August 28, 2026 | — | Hostile-review hardening: A0 Governance adoption; A1 discovery; A2 schema freeze; A3 dual #19/#20 reapproval; closed-world classification; deterministic applicability; typed contracts; complete proof binding; versioned ledger; exception-boundary correction; exhaustive amendment matrices; required-status CI; finite baseline; A0–A9 sequencing. No #19/#20 normative files or implementation changed. |
| 0.1 | August 27, 2026 | — | Initial detailed integration map for Project Architecture Governance v0.4. Maps #19/#20 amendments, runtime/code surfaces, governance state records, audit tooling, adversarial-review migration, CI activation, evidence invalidation, and staged implementation. Explicitly excludes the frozen D1–D4 remediation supplement. |
