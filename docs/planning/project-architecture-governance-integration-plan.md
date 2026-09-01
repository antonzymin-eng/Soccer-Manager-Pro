# Project Architecture Governance — Integration Map and Implementation Plan

**Document Class:** Integration design and implementation plan  
**Status:** Draft — implementation planning; no production code implemented by this document  
**Version:** 0.12\
**Created:** August 27, 2026  
**Last Updated:** August 31, 2026  
**Governing authority:** docs/planning/project-architecture-governance.md v0.10 (v0.4 when this plan was created)\
**Primary downstream specifications:** Testing Strategy & Framework #19; Code Standards & Style Guide #20  
**Related project authorities:** Master Development Plan; adversarial-review process; root and src agent guides  
**Review/authoring base:** branch docs/round-2-architecture-remediation-design at commit 12abb982c45f667fb90311320997b6d7f00dc8cf (provenance only; not an evidence-freshness key)

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

The selector grammar MUST be frozen in A2 and MUST distinguish namespaces/types, constructors, overloaded method signatures, static members, and assembly identity. Contracts keep `selector_history` sufficient to migrate ordinary moves/renames while preserving logical identity. Ambiguous or multiply resolving selectors fail strict mode.

Each classification record therefore carries the current `symbol_key`, `kind`, source path, symbol/signature, assembly, classification, and stable `component_id`/`contract_id` only when the surface has durable architectural identity.

Strict mode fails when a newly discovered surface is unclassified after initial baseline acceptance.

## 3.3 Property registry

`property-registry.json` adds `schema_version`, `decision_id`, `decision_actor`, optional `decision_provenance_revision`, `transition_from`, `transition_to`, `decision_rationale`, and `revalidation_history`.

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

## 3.5 Applicability manifest and deterministic resolver

Create `docs/tracking/architecture-governance/applicability-rules.json`.

Each rule contains `rule_id`, selectors, trigger ref, requirement refs, proof classes, gate classes, allowed N/A reasons, precedence, and fallback scope.

All matches are evaluated. Schema-defined specificity controls precedence; equal-precedence conflicts fail. Precedence is not author-chosen: every explicit selector/identity/classification/activation rule outranks every fallback rule; among fallback rules, `runtime-bearing` and `non-runtime-bearing` outrank `repository`; the two category fallbacks are disjoint under §3.2's mapping; conflicting rules at the same derived precedence fail. N/A is valid only for an enumerated reason and required approval reference.

`--changed` optimizes only after applicability and the full proof-class closure are resolved. It MUST fall back to the full relevant proof universe when any changed surface is unmapped, when the current proof is stale, or when a changed surface is a member of the derived closure. It MAY skip only when every changed surface is mapped, none belongs to the closure, and the current applicability/closure fingerprints remain fresh. Unresolved applicability fails strict mode.

Applicability answers **which obligations apply**. It does not itself define the complete freshness dependency surface of a proof. The proof-class closure resolver in §3.7 derives that surface from the matched obligations, integration contracts, compiler/asmdef facts, tests/fixtures, configuration, and tooling required by the proof class.

A2 MUST execute the resolver against representative good/bad/conflict/N/A fixtures. Identical repository facts and declarations MUST resolve to identical obligation sets and closure inputs before A3 can rely on the schema.

## 3.6 Exception routing and precedence

Governance exceptions remain property-oriented exactly as Governance §7 defines them. This integration MUST NOT route FR-CS or FR-TS waivers directly into exceptions.json unless the affected obligation is an admitted AP that explicitly allows an exception.

Existing #19/#20 exception mechanisms remain owner-specific. They cannot waive an admitted AP, missing required evidence, concrete correctness/integrity failure, or Governance Blocker.

## 3.7 Canonical proof artifact schema and dependency closure

The #19 amendment MUST land the canonical schema and closure semantics before proof workflow or CI gating.

Reusable proof records require: `schema_version`; `proof_id`; `proof_class`; requirement/property refs; applicability rule IDs; result (`pass`/`fail`/`na`/`bounded`); N/A or bounded justification/approval; `subject_scope_digest`; provenance revision/tree metadata; proof-class dependency closure and content fingerprints; scoped inventory/asmdef digests when applicable; relevant configuration fingerprints; tool/extractor identities; runner/execution records; conditional failure-injection and mutation records; created metadata; revalidation history.

### 3.7.1 Proof-class closure resolution

The audit MUST derive and validate closure using the proof class rather than trusting an author-supplied file list. The proof-class enum is exactly the four classes owned by Governance §5 and FR-AG-027–030: `structural-reachability`, `lifecycle-order`, `failure-injection`, and `mutation`. Persistence boundaries and external resources are applicability/change triggers from Governance §5.2, not additional proof classes.

- structural reachability: matched contract + owning roots + construction/registration edges + applicable public/bypass surfaces + relevant asmdef nodes/edges; serializer/schema/resource/configuration edges are included when they are reachable members of a persistence/external-resource boundary;
- lifecycle/order: structural closure + lifecycle members, owners, ordering edges, relevant synchronization/thread-affinity members, and testhost equivalents;
- failure-injection: applicable structural/lifecycle closure + exact failure target, test/fixture, runner configuration/environment, and tool semantics;
- mutation: applicable structural/lifecycle closure + exact mutation target, test/fixture, runner configuration/environment, and tool semantics.

Proof records MAY include additional declared dependencies, but the resolver verifies they are not narrower than the mechanically required closure. If the resolver cannot prove closure completeness, strict mode fails or the proof must use a #19-approved bounded substitute.

Freshness must detect material additions, deletions, renames, generated/config changes, new applicable roots, asmdef changes, and checker/extractor-semantic changes inside that resolved closure. A rename that preserves stable component identity updates selector binding and fingerprints without pretending the architectural component was deleted/recreated.

### 3.7.2 Execution truth

Every required executable record carries `execution_state` from: `passed`, `failed`, `skipped`, `excluded`, `unavailable`, `not-run`, `runner-failed`. Only `passed` satisfies an unqualified required execution obligation. Any other state is unsatisfied unless #19 explicitly permits a bounded substitute and that substitute is recorded/approved.

A2 freezes the executable state machine before A3 consumes it. A bounded substitute cannot satisfy execution merely because a justification string exists: the caller must establish that the owning #19 rule permits substitution, and the record must carry the exact authority reference, approval reference, justification, and omitted proof surface or remaining uncertainty. `passed` and bounded-substitute claims are mutually exclusive.

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

A fresh final-review marker is valid when the current material review subject recomputes to the recorded `subject_scope_digest`. Recording the review run itself or unrelated landing/tracking metadata does not recursively invalidate that subject.

## 3.9 Temporary activation baseline

If required, the baseline is finite and versioned. Each item records violation ID, exact stable component/selector binding, baseline `subject_scope_digest`, creation provenance revision, owner, disposition, required action, and expiry trigger.

Creation provenance is not the freshness key. New violations fail; a baseline item's governed selector/content changing outside the recorded subject scope requires explicit review. Final strict activation requires zero active items and retirement of activation-only baseline machinery.

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
| FR-CS-076 | Applicable runtime-bearing components MUST declare construction, activation, update/use, and teardown ownership through typed lifecycle records, with schema-valid N/A only where a phase does not exist. | MUST | Governance FR-AG-023 | §3.5.6 |
| FR-CS-077 | Applicable alternate hosts/testhosts MUST preserve the invariant or declare an approved divergence linked to current evidence. | MUST | Governance FR-AG-024 | §3.5.7 |
| FR-CS-078 | Activation bypasses inside a mechanically closed governed surface MUST be prohibited or explicitly supported. | MUST | Governance FR-AG-025/026 | §3.5.7 |
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
| FR-TS-094 | Missing, failed, stale, schema-invalid, applicability-incomplete, skipped, excluded, unavailable, not-run, or runner-failed required architectural proof MUST block merge once the gate is active unless an approved bounded substitute explicitly satisfies the obligation. | MUST | Stage 0+1 |
| FR-TS-095 | Merge-critical governance tooling MUST have known-good, known-bad, and blind-spot verification proportionate to false-positive/negative consequence. | MUST | Stage 0+1 |
| FR-TS-096 | Bounded substitutes for computationally disproportionate exhaustive proof MUST record scope, rationale, omitted uncertainty, and approval. | MUST | Stage 0+1 |
| FR-TS-097 | A `[GT]` or owner-declared calibration/tuning change MUST NOT land for a component whose activation state is intentionally-disabled, pending-integration, or unresolved unless an approved exception explicitly authorizes that tuning scope. | MUST | Stage 0+1 |

§2.2 gains FR-TS-086–097 as Architecture proof/evidence integration, mechanics in new §3.11, verification through §5.6/architecture gate. Total becomes 97.

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
| section-2.md | FR-TS-086–097; 85→97 partition/count; FR-TS-084/076/077; KD-W1 activation/tuning precondition; exception boundary; failure modes/history. |
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

Owning placement does not imply execution. Every required executable proof resolves to a runner capable of compiling/executing that assembly and a machine-readable execution record. A required test excluded by `known-failures.txt`, flake quarantine, `[Ignore]`, `Assert.Ignore`, unsupported-assembly filtering, conditional Unity-job skipping, or equivalent exclusion is unsatisfied unless #19 explicitly approves a bounded substitute.

The architecture gate MUST mechanically reject intersection between its required-test set and active quarantine/exclusion sets. Where possible it also executes the resolved required test set directly; otherwise it consumes mandatory upstream runner results with exact test identity/result binding.

Targeted governance mutation at Stage 0+1 does not depend on project-wide Stryker activation. FR-TS-091 is satisfied by the exact reproducible mutant protocol in §3.7.3; the broader Stryker.NET program may remain deferred under #19's existing Stage-1 tooling decision.

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

Freeze identity/selectors, activation-state/disable-anchor semantics, applicability, contracts, the exact four Governance proof classes and their closure/freshness behavior, execution-truth/bounded-substitute semantics, property/exception, review, and baseline schemas. The reference semantics include activation-anchor evaluation, KD-W1 tuning-surface matching, schema-derived fallback precedence/classification mapping, and conservative changed-surface rerun decisions. Any compiler reference implementation is source-built with the pinned .NET SDK/toolchain.

## A3 — Amend and reapprove #19/#20 governance integration

The coordinated bundle consumes the already-repaired #20 dependency model. Add activation-state mechanics to #20 and FR-TS-097/KD-W1 to #19 alongside the existing governance amendments.

## A4 — Compiler-backed Class-A discovery and state seeding

Build/run the extractor from the governed checkout, combine semantic candidates with finite bootstrap intent, compute the closed runtime universe, classify every surface, assign activation state, and seed final contracts/registries. Intentional-disable anchors must resolve/evaluate; pending-integration records need owner/exit condition.

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
