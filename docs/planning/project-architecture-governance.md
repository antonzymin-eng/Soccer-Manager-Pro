# Project Architecture Governance Specification
## Evidence, Review, and Agentic Development Governance

**Document Class:** Project-level governance specification  
**Status:** Draft  
**Version:** 0.5  
**Created:** August 27, 2026  
**Last Updated:** August 31, 2026  
**Primary downstream authorities:** Testing Strategy & Framework Specification #19; Code Standards & Style Guide Specification #20  
**Related authorities:** Master Development Plan; Development Best Practices; adversarial-review process  
**Normative language:** MUST / MUST NOT / SHOULD / SHOULD NOT / MAY are normative requirements.

---

# 1. Purpose, Scope, and Authority

## 1.1 Purpose

The project already defines architecture review, quality gates, testing obligations, dependency controls, reproducible evidence, programmatic verification, and adversarial review.

Those rules are individually useful but do not fully define:

- how a newly discovered architectural concern becomes a project requirement;
- how review findings are classified as blockers, tradeoffs, residual risks, or candidate properties;
- when an architectural review is complete;
- who owns integration and lifecycle responsibility;
- what constitutes repository-wide reachability and lifecycle evidence;
- when structural, lifecycle, failure-injection, and mutation proofs are mandatory;
- how agent judgment interacts with mechanical enforcement;
- how repeated architectural decisions are converted into stable project rules.

This specification supplies that missing decision layer.

It does **not** replace Testing Strategy #19 or Code Standards #20.

The governing principle is:

> **Architectural judgment resolves uncertainty. Machine enforcement protects settled conclusions.**

---

## 1.2 Scope

This specification applies to:

- project architecture;
- cross-system integration;
- runtime ownership;
- composition;
- lifecycle-sensitive behavior;
- public or cross-assembly architecture;
- architectural review;
- architectural verification;
- governance rule evolution;
- agentic coding workflows.

It applies to:

- production hosts;
- testhosts;
- tools where they participate in runtime or architectural behavior;
- alternate bootstraps;
- editor/development hosts where relevant;
- static initialization and registration paths;
- repository-wide mechanically discoverable architectural surfaces.

---

## 1.3 Out of Scope

This specification does not own:

- detailed test framework implementation;
- coverage thresholds;
- performance thresholds;
- coding style;
- dependency-layer definitions already owned elsewhere;
- concrete CI provider selection;
- detailed build commands;
- domain-specific simulation correctness;
- individual system APIs unless required to express a governance property.

Those remain with their existing authoritative owners.

---

## 1.4 Authority Matrix

| Rule Class | Authoritative Owner |
|---|---|
| Architectural property admission | **This specification** |
| Finding disposition | **This specification** |
| Review termination semantics | **This specification** |
| Judgment vs machine-enforcement boundary | **This specification** |
| Agent-heavy development assumption | **This specification** |
| Integration ownership code rules | Spec #20 |
| Dependency direction | Spec #20 |
| Composition-root enforcement | Spec #20 |
| Runtime reachability code constraints | Spec #20 |
| Static-state / initialization restrictions | Spec #20 |
| Required evidence | Spec #19 |
| Integration / simulation verification | Spec #19 |
| Failure injection | Spec #19 |
| Mutation testing | Spec #19 |
| Evidence freshness / reproducibility | Spec #19 |
| Merge-gate mechanics | Spec #19 |
| Stage-level development gates | Master Development Plan |
| Adversarial review execution process | Adversarial-review process, constrained by this specification |

No rule may have two normative owners.

---

## 1.5 Key Design Decisions

### KD-AG-1 — Concerns do not automatically become requirements

A reviewer discovering a plausible concern does not automatically create a new merge blocker.

New generalized architectural obligations must pass the property-admission process.

### KD-AG-2 — Severity and disposition are independent

Severity expresses impact.

Disposition determines whether approval is blocked.

A High-severity concern may be an accepted tradeoff. A lower-severity finding may still be a blocker if it violates a MUST-level requirement.

### KD-AG-3 — Mechanical proof is preferred for mechanical questions

Finite, objective, repeatable architectural questions SHOULD be answered by tooling rather than repeated reviewer judgment.

### KD-AG-4 — Agent judgment remains the architectural reasoning layer

Judgment is reserved for questions that cannot be reduced safely to objective checks.

### KD-AG-5 — Agent-heavy development changes reasonable proof cost

Repository-wide enumeration and cross-reference work are assumed to be cheap enough to require routinely when mechanically discoverable.

This assumption concerns primarily the elimination of human mechanical effort. It does not imply that computational cost, combinatorial state-space growth, or excessive CI resource consumption are irrelevant.

### KD-AG-6 — Review is finite

Review ends when admitted gating properties are proven and all findings have valid dispositions.

Review does not remain open merely because additional hypothetical architectural preferences can be invented.

### KD-AG-7 — Governance is a ratchet, not an accumulation trap

Repeated architectural conclusions SHOULD become stable rules.

Rules MAY later be narrowed, replaced, or retired if their premises cease to hold.

---

# 2. Normative Requirement Registry

## 2.1 Architectural Property Governance

### FR-AG-001 — Candidate status

A newly proposed generalized architectural obligation MUST initially be classified as a **Candidate Property** unless it already maps directly to an existing authoritative requirement.

### FR-AG-002 — Admission requirement

A Candidate Property MUST NOT become a required architectural property until it satisfies the admission criteria in §3.

### FR-AG-003 — Existing-rule reuse

If a candidate concern is already governed by an existing authoritative rule, the reviewer MUST cite that rule rather than create a duplicate property.

### FR-AG-004 — Stable property ID

Every admitted property MUST receive a stable identifier.

Recommended form: `AP-###`.

### FR-AG-005 — Single normative owner

Every admitted property MUST have exactly one normative owner.

### FR-AG-006 — Defined scope

Every admitted property MUST define the repository or runtime surface to which it applies.

### FR-AG-007 — Evidence definition

Every admitted property MUST define how satisfaction is demonstrated.

### FR-AG-008 — Enforcement classification

Every admitted property MUST be classified as:

- machine-enforceable;
- hybrid;
- judgment-dependent.

---

## 2.2 Finding Governance

### FR-AG-009 — Required disposition

Every substantive architectural review finding MUST end in exactly one disposition:

- Blocker;
- Accepted Tradeoff;
- Residual Risk;
- Candidate Property;
- Resolved.

### FR-AG-010 — Evidence-based blocker

A finding MUST NOT be classified as a Blocker solely because a reviewer prefers another design.

### FR-AG-011 — Requirement linkage

Every Blocker MUST cite:

- an admitted architectural property; or
- an existing approved specification, invariant, or other authoritative requirement; or
- a concrete independently established correctness/integrity failure.

### FR-AG-012 — Tradeoff integrity

An Accepted Tradeoff MUST NOT be used to waive violation of an admitted MUST-level property.

### FR-AG-013 — Residual-risk integrity

Residual Risk MUST NOT be used to conceal missing required evidence.

### FR-AG-014 — Candidate isolation

A Candidate Property MUST NOT independently block the current review unless it is admitted and applies to the reviewed work.

---

## 2.3 Review Termination

### FR-AG-015 — Property-based termination

Review completion MUST be based on satisfaction of applicable admitted properties, not exhaustion of reviewer imagination.

### FR-AG-016 — No open blockers

A review MUST NOT converge while any Blocker remains unresolved.

### FR-AG-017 — Required disposition completeness

Every substantive finding MUST have an explicit disposition before review converges.

### FR-AG-018 — Fresh review requirement

Final convergence MUST include a fresh review over the current artifact.

### FR-AG-019 — Round-budget semantics

A round budget limits process execution but MUST NOT convert unresolved blockers into approval.

### FR-AG-020 — Non-converged outcome

If the review budget ends with blockers open, the result MUST be recorded as **NON-CONVERGED**.

---

## 2.4 Integration and Lifecycle Governance

### FR-AG-021 — Explicit integration owner

Every runtime-bearing component whose correctness depends on activation MUST have an explicit integration owner.

### FR-AG-022 — No deferred anonymous wiring

Statements such as “wire later,” “register at composition root,” or equivalent are insufficient unless the exact owner and integration point are identified.

### FR-AG-023 — Lifecycle ownership

Applicable runtime components MUST identify construction, activation, update/use, and teardown ownership.

### FR-AG-024 — Alternate-host coverage

Applicable testhosts, tools, alternate bootstraps, and development hosts MUST be included in integration analysis.

### FR-AG-025 — Bypass-path declaration

Known alternate or bypass activation paths MUST either be prohibited or explicitly classified.

---

## 2.5 Proof Governance

### FR-AG-026 — Repository-wide inventory

Where an architectural property applies to a finite mechanically discoverable repository surface, the proof MUST enumerate that complete surface unless an approved exclusion exists.

### FR-AG-027 — Structural reachability proof

Applicable runtime components MUST have structural reachability evidence.

### FR-AG-028 — Lifecycle/order proof

Lifecycle-sensitive behavior MUST have lifecycle/order evidence.

### FR-AG-029 — Failure-injection proof

Applicable meaningful failure paths MUST be deliberately exercised.

### FR-AG-030 — Mutation proof

Important invariants whose tests could plausibly pass despite a broken implementation MUST have targeted mutation evidence when triggered by §5.

### FR-AG-031 — Evidence freshness

Evidence MUST correspond to the current materially relevant repository state.

### FR-AG-032 — Reproducibility

Required proof MUST be independently reproducible by another reviewer or agent.

### FR-AG-032A — Evidence invalidation

Architectural evidence MUST be regenerated or revalidated when a change materially alters the declared dependency surface on which that evidence depends.

Revalidation MAY reuse unaffected evidence. A repository change does not require unrelated proofs to be rerun.

---

## 2.6 Agentic Enforcement Governance

### FR-AG-033 — Exhaustive mechanical work assumption

Human effort required for enumeration, indexing, cross-reference discovery, and other mechanical search work MUST NOT by itself justify sampling when agents can perform exhaustive analysis.

### FR-AG-034 — No unsupported agent assertion

Agent claims such as “all call sites were checked” MUST be backed by an inventory, executable check, or equivalent reproducible evidence when the set is mechanically discoverable.

### FR-AG-035 — Mechanical promotion

A stable objective rule SHOULD be converted into machine enforcement where technically reliable.

### FR-AG-036 — Judgment retention

Architectural judgment MUST remain available for questions of abstraction quality, ownership cleanliness, proportionality, maintainability, extensibility, and rule admission.

### FR-AG-036A — Governance-tool verification

A tool that supplies required architectural evidence or blocks merge based on an admitted architectural property MUST have verification appropriate to the consequence of false negatives and false positives.

### FR-AG-036B — Computational proportionality

The agent-heavy development assumption MUST NOT require mechanically exhaustive proof where the computational cost is materially disproportionate to the architectural risk.

Any bounded substitute MUST explicitly identify the omitted proof surface or remaining uncertainty.

---

## 2.7 Governance Evolution

### FR-AG-037 — Rule promotion review

Repeated Candidate Properties SHOULD be reviewed for formal admission.

### FR-AG-038 — No duplicate governance

Rule promotion MUST first check for an existing authoritative property.

### FR-AG-039 — Explicit exception

Any temporary exception to an admitted property MUST be recorded through the exception mechanism in §7.

### FR-AG-040 — Explicit retirement

Admitted properties MUST NOT silently disappear. Retirement or replacement requires a recorded governance decision.

### FR-AG-040A — Property revalidation

An admitted property MUST be reconsidered when its architectural premise, scope, failure mode, proof mechanism, or enforcement mechanism materially changes.

### FR-AG-040B — Mutation retirement

Targeted mutation obligations MUST be removable when the protected failure mode disappears or equivalent stronger verification replaces them.

### FR-AG-040C — Non-recursive tool verification

Verification of governance tooling MUST terminate at ordinary software verification unless a separately admitted property justifies additional meta-verification.

### FR-AG-040D — Precise evidence invalidation

Reusable architectural evidence MUST declare a sufficiently precise dependency surface so unrelated repository changes do not cause unnecessary proof regeneration.

---

# 3. Architectural Property Lifecycle

## 3.1 Property States

A property exists in one of the following states:

1. **Candidate**
2. **Admitted**
3. **Superseded**
4. **Retired**
5. **Rejected**

### Valid transitions

| From | To | Trigger |
|---|---|---|
| Candidate | Admitted | Admission criteria satisfied |
| Candidate | Rejected | Criteria not satisfied / disproportionate / duplicate |
| Admitted | Superseded | New property replaces it |
| Admitted | Retired | Requirement no longer justified |
| Rejected | Candidate | Material new evidence appears |
| Superseded | Retired | Historical cleanup after replacement stabilizes |

---

## 3.2 Admission Criteria

A Candidate Property MUST satisfy all mandatory criteria below.

### AC-1 — Concrete failure mode

The property addresses a credible failure mode, maintenance hazard, integration risk, architectural degradation, or project-quality risk.

Pure style preference is insufficient.

### AC-2 — Project relevance

The concern materially affects one or more:

- correctness;
- determinism;
- maintainability;
- extensibility;
- architectural isolation;
- testability;
- integration safety;
- lifecycle safety;
- diagnosability;
- performance;
- recoverability;
- data integrity.

### AC-3 — Defined invariant

The rule can be expressed as an identifiable invariant, obligation, prohibition, or proof requirement.

### AC-4 — Bounded scope

The property defines exactly where it applies.

### AC-5 — Evidence model

A credible means exists to determine compliance.

### AC-6 — Authority ownership

Exactly one normative owner is identified.

### AC-7 — Enforcement classification

The property is marked Machine, Hybrid, or Judgment.

### AC-8 — Proportionality

The proof and enforcement burden must be proportionate to the failure mode under the project's agent-heavy development assumption.

Human search, enumeration, indexing, or repetition cost is normally insufficient justification for weakening a mechanically obtainable proof.

However, proportionality MUST also consider actual computational cost, including:

- execution time;
- memory consumption;
- state-space growth;
- combinatorial explosion;
- mutation-suite cost;
- CI resource consumption.

Where exhaustive proof is computationally disproportionate, the strongest practical bounded proof MAY be used, provided the limitation and remaining uncertainty are explicit.

---

## 3.3 Property Record Schema

Every admitted property MUST record:

| Field | Requirement |
|---|---|
| Property ID | Stable `AP-###` |
| Title | Short descriptive name |
| State | Candidate / Admitted / Superseded / Retired / Rejected |
| Statement | Normative property text |
| Failure mode | What goes wrong without it |
| Scope | Exact applicable surface |
| Non-scope | Explicit exclusions where useful |
| Authority | Single normative owner |
| Evidence | Required proof |
| Enforcement class | Machine / Hybrid / Judgment |
| Activation | Immediate or staged |
| Exceptions allowed | Yes/No + mechanism |
| Supersedes | Property ID if applicable |
| Decision rationale | Why admitted/rejected |
| Last reviewed | Date/commit/version |

---

## 3.4 Admission Decision

Admission is an architectural judgment decision.

The deciding agent or project lead MUST evaluate the complete record against §3.2.

Admission MUST NOT be based on reviewer severity alone.

If a candidate is rejected, the reason SHOULD be one of:

- duplicate;
- vague;
- no credible failure mode;
- disproportionate;
- not project-relevant;
- no enforceable scope;
- premature;
- inferior to a broader existing invariant.

---

## 3.5 Promotion Trigger

No fixed number of repeated findings automatically forces admission.

However, a Candidate Property SHOULD be reconsidered when:

- the same concern occurs in multiple independent reviews;
- the same class of defect recurs;
- multiple agents independently derive the same requirement;
- the concern causes repeated manual review effort;
- an existing workaround repeatedly appears;
- a formerly ambiguous architectural decision has stabilized.

Repeated appearance triggers **consideration**, not automatic rule creation.

---

# 4. Finding Disposition Model

## 4.1 Finding States

Each finding moves through:

**Open → Dispositioned → Resolved/Accepted/Recorded**

A finding MUST retain a stable identifier across review rounds.

---

## 4.2 Required Finding Schema

| Field | Requirement |
|---|---|
| Finding ID | Stable review ID |
| Summary | Short defect/concern statement |
| Evidence | Concrete supporting evidence |
| Severity | Critical / High / Medium / Low or project equivalent |
| Requirement/Property | Cited authority, if any |
| Disposition | Blocker / Tradeoff / Residual Risk / Candidate Property / Resolved |
| Required action | Fix / document / admit property / none |
| Owner | Responsible resolver where applicable |
| Status | Open / Resolved / Accepted |
| Round introduced | Review round |
| Resolution evidence | Proof of final disposition |

---

## 4.3 Blocker

A finding is a Blocker only when at least one of the following holds:

1. an admitted MUST-level property is violated;
2. an approved specification or invariant is violated;
3. required evidence is absent or failed;
4. required integration ownership is absent;
5. a mandatory proof trigger is unmet;
6. concrete correctness, integrity, determinism, security, or equivalent established behavior is broken.

---

## 4.4 Accepted Tradeoff

Accepted Tradeoff requires:

- competing legitimate qualities;
- no unmet MUST-level property;
- explicit consequence;
- explicit rationale;
- acceptance by the appropriate architectural decision-maker.

Examples:

- slightly larger public surface to avoid dependency inversion complexity;
- additional allocation outside a prohibited hot path to simplify lifecycle ownership;
- intentionally narrower extensibility to preserve deterministic state ownership.

---

## 4.5 Residual Risk

Residual Risk requires:

- identified credible risk;
- no violated mandatory property;
- mitigation judged disproportionate, unavailable, or intentionally deferred;
- material consequence documented.

Material residual risk SHOULD include:

- owner;
- revisit trigger;
- expiry/review date where appropriate.

---

## 4.6 Candidate Property

A finding becomes Candidate Property when:

- it proposes a generalized new architectural obligation;
- no existing authority owns it;
- current evidence does not independently establish a concrete existing-rule defect.

Candidate Property findings leave the current review unless and until the property is formally admitted.

---

# 5. Architectural Proof Obligations

## 5.1 Proof Set

The high-risk architectural proof set consists of:

1. **Structural Reachability**
2. **Lifecycle / Ordering**
3. **Failure Injection**
4. **Mutation**

The proofs are applicability-driven.

They are not ceremonial requirements for every code change.

---

## 5.2 Trigger Matrix

| Change Type | Reachability | Lifecycle | Failure Injection | Mutation |
|---|---:|---:|---:|---:|
| Pure local calculation | No | No | Case-by-case | Case-by-case |
| New public cross-assembly API | Yes | If runtime-bearing | Case-by-case | If invariant-critical |
| New runtime service | Yes | Yes | Yes if meaningful failure path | Yes for critical integration invariant |
| New composition-root registration | Yes | Yes | If registration can fail/degrade | Yes |
| Host/bootstrap change | Yes | Yes | Yes where failure/recovery exists | Yes for routing/ownership guarantees |
| Static initialization change | Yes | Yes | If fallback/error path exists | Usually Yes |
| Persistence boundary | Yes | Yes | Yes | Yes for integrity invariant |
| External resource/dependency | Yes | Yes | Yes | Case-by-case |
| Testhost/runtime divergence fix | Yes | Yes | Case-by-case | Yes if test could otherwise pass accidentally |
| Dependency-graph-only refactor | Yes | If lifecycle changes | No unless failure path affected | Case-by-case |
| Pure data schema with no runtime behavior | Structural only if cross-assembly | No | Migration failure if applicable | If compatibility invariant critical |

A reviewer MAY require an additional proof outside this matrix only by identifying the admitted property or concrete failure mode that triggers it.

Where a theoretically exhaustive proof would impose computational cost materially disproportionate to the architectural risk, §3.2 AC-8 and FR-AG-036B govern the use of a bounded substitute.

---

## 5.3 Structural Reachability Proof

Structural reachability answers:

> Is the required runtime behavior actually reachable through every applicable supported path?

Evidence MUST identify:

- applicable entry points;
- composition roots;
- construction or registration sites;
- allowed dependency edges;
- relevant public surfaces;
- alternate runtime paths;
- testhost equivalents.

The proof MUST detect, where applicable:

- unreachable implementations;
- orphan registrations;
- duplicate construction;
- alternate roots omitting the component;
- unauthorized bypasses;
- public types that imply unsupported integration paths.

---

## 5.4 Lifecycle / Ordering Proof

Lifecycle proof answers:

> Does the component exist and execute in the required phase and order?

Applicable evidence includes:

- construction-before-use;
- registration-before-resolution;
- deterministic ordering;
- exactly-once initialization;
- permitted reinitialization;
- teardown/disposal ordering;
- load/restore ordering;
- testhost equivalence;
- absence of static initialization bypasses.

---

## 5.5 Failure Injection

Failure injection answers:

> Has the relevant failure behavior actually executed?

Where meaningful, the proof scope SHOULD include deliberately causing:

- unavailable dependency;
- registration failure;
- invalid state;
- partial initialization;
- corrupted persistence input;
- missing resource;
- dependency rejection;
- timeout/failure signal;
- recovery/fallback path.

Static inspection alone is not sufficient when the failure can reasonably be executed.

---

## 5.6 Mutation Proof

Mutation answers:

> Would the evidence fail if the protected invariant were deliberately broken?

Mutation MUST be targeted.

Examples:

- remove required registration;
- invert dependency condition;
- bypass owner;
- omit disposal;
- reverse ordering;
- disable error propagation;
- change success/failure branch;
- replace correct host with alternate host;
- remove required cross-reference.

The project does not require mutation score maximization.

Mutation obligations MUST be tied to a specific important invariant, requirement, or failure mode.

A mutation requirement MAY be retired when:

- the protected invariant no longer applies;
- the architecture removes the relevant failure mode; or
- another verification mechanism provides equivalent or stronger evidence that the defect class cannot escape detection.

Mutation tests MUST NOT be retained solely because they were previously required.

The purpose of mutation is to validate that important evidence is sensitive to the defect it claims to detect, not to create a permanently expanding mutation suite.

---

## 5.7 Evidence Dependency, Invalidation, and Revalidation

Architectural proof is valid only while the material surface on which it depends remains unchanged or has been explicitly revalidated.

Every reusable proof artifact MUST identify its **specific evidence dependency surface** with sufficient precision to determine whether a later repository change can affect the proof.

The dependency surface SHOULD be expressed in terms such as:

- specific hosts or entry points;
- composition roots;
- assemblies or dependency edges;
- construction or registration paths;
- lifecycle owners or ordering relationships;
- public runtime surfaces;
- static initialization paths;
- alternate or bypass paths;
- relevant tests;
- governance tools used to generate or validate the proof.

Changes to an identified dependency invalidate the affected proof unless compatibility is established.

Changes outside the declared dependency surface MUST NOT automatically invalidate the proof.

Broad repository-wide invalidation SHOULD be used only where the proof itself genuinely depends on a repository-wide invariant.

When prior evidence is retained through revalidation rather than full regeneration, the revalidation record MUST identify:

- the intervening change;
- the declared evidence dependency affected;
- the analysis establishing continued validity.

The objective is **precise invalidation**, not routine regeneration of all architectural evidence after unrelated changes.

---

# 6. Enforcement Architecture and Agentic Development Model

## 6.1 Machine-Enforced Domain

Objective repeatable rules SHOULD move into automation.

Examples:

- assembly dependency direction;
- entry-point inventories;
- public-surface inventories;
- composition roots;
- lifecycle declaration presence;
- forbidden APIs/patterns;
- static-state rules;
- mutation execution;
- stale cross-references;
- required test coverage;
- evidence artifact presence;
- missing ownership declarations;
- runtime reachability graph consistency.

---

## 6.2 Judgment Domain

Judgment remains required for:

- property admission;
- abstraction selection;
- ownership quality;
- conceptual coupling;
- maintainability;
- extensibility;
- tradeoff acceptance;
- residual-risk acceptance;
- proportionality;
- rule retirement;
- whether a proposed fix addresses the correct architectural layer.

---

## 6.3 Governance Ratchet

The intended progression is:

**Novel concern → architectural judgment → admitted property → mechanical enforcement where reliable**

Mechanical enforcement MUST NOT be introduced merely because a rule can technically be checked.

The underlying rule must first be architecturally justified.

---

## 6.4 Agent-Heavy Development Assumption

This project assumes implementation and repository analysis are predominantly agent-assisted.

Therefore exhaustive repository work is normally reasonable when the set is mechanically finite.

Examples:

- enumerate every `Main`;
- enumerate all host bootstraps;
- index all public cross-assembly types;
- enumerate all constructors for a service;
- trace every composition root;
- enumerate all static initialization;
- find every bypass call;
- validate every cross-reference.

Sampling is insufficient merely because exhaustive inspection would be tedious for a human.

This principle does not require exhaustive execution when computational cost becomes materially disproportionate under §3.2 AC-8.

---

## 6.5 Independent Evidence Principle

Agent capability does not reduce evidence requirements.

It increases the amount of evidence reasonably obtainable.

An agent statement is not proof merely because the agent is capable of exhaustive analysis.

The result MUST be reproducible.

---

## 6.6 Governance Tool Verification

Machine enforcement is evidence, not an infallible authority.

Any tool whose output becomes required evidence or a merge-blocking architectural gate MUST itself be subject to appropriate verification.

Verification SHOULD be proportionate to the consequence of tool failure and MAY include:

- unit tests for discovery and classification logic;
- fixtures containing known violations and known compliant cases;
- failure tests proving the tool rejects malformed or incomplete input;
- regression tests for previously discovered blind spots;
- targeted mutation or equivalent negative testing for critical enforcement logic.

A merge-critical checker MUST demonstrate that representative violations cause the checker to fail.

Changes to a governance tool that materially alter what it discovers, classifies, or blocks invalidate affected proof produced under the previous behavior unless compatibility is established.

The fact that an architectural rule is machine-enforced does not remove architectural responsibility for validating that the enforcement mechanism actually represents the rule.

Governance-tool verification terminates at ordinary software verification.

A governance checker does **not** require a second governance checker merely because its output is merge-critical.

Its correctness may be established through appropriate combinations of ordinary tests, known-good and known-bad fixtures, regression tests, targeted negative testing, targeted mutation, and independent inspection where warranted.

Additional meta-verification MUST NOT be introduced recursively unless an independently admitted architectural property identifies a concrete failure mode requiring it.

---

# 7. Exceptions, Amendments, Supersession, and Retirement

## 7.1 Exception Rule

An admitted MUST-level property MAY be temporarily waived only through an explicit exception record if the property allows exceptions.

An exception MUST contain:

| Field | Requirement |
|---|---|
| Exception ID | Stable ID |
| Property | Affected `AP-###` |
| Scope | Exact files/components/hosts |
| Reason | Why compliance is currently inappropriate |
| Risk | Consequence |
| Mitigation | Compensating control |
| Owner | Responsible party |
| Expiry trigger | Date, milestone, or condition |
| Approval | Architectural decision record |

No silent exceptions are permitted.

---

## 7.2 Exception Restrictions

Exceptions MUST NOT be used to bypass:

- concrete correctness defects;
- data corruption;
- determinism violations where determinism is mandatory;
- unresolved required evidence;
- security-critical requirements unless separately governed by applicable security policy.

---

## 7.3 Property Amendment

A property MAY be amended when:

- scope is too broad/narrow;
- evidence method is defective;
- architectural assumptions changed;
- better enforcement became available;
- repeated exceptions demonstrate rule mismatch.

Amendment MUST preserve the property ID unless the semantic invariant itself changes materially.

---

## 7.4 Supersession

A property is Superseded when a new property replaces its architectural role.

The superseding property MUST identify the old property.

Historical references MUST remain resolvable.

---

## 7.5 Retirement

Retirement requires evidence that:

- the failure mode is no longer relevant; or
- another authority fully subsumes the property; or
- the architecture changed such that the property no longer applies.

Retired properties remain in history.

They are not deleted as though they never existed.

---

## 7.6 Property Revalidation Trigger

An admitted architectural property MUST be reconsidered when a material change affects any of the assumptions that justified it.

Reconsideration is required when one or more of the following materially changes:

- the property's scope;
- the failure mode it addresses;
- the architectural premise on which it depends;
- the ownership model it governs;
- its enforcement mechanism;
- its proof mechanism;
- the cost of complying with or verifying it;
- another admitted property that overlaps or supersedes its purpose.

Reconsideration does not imply automatic amendment or retirement.

The architectural decision is whether the property should:

- remain unchanged;
- be narrowed;
- be broadened;
- be superseded;
- be retired.

Associated enforcement tooling and proof obligations MUST be updated consistently with that decision.

An admitted property MUST NOT remain active solely because it has historically existed.

---

# 8. Downstream Integration and Traceability

## 8.1 Required Spec #19 Amendments

Testing Strategy #19 SHOULD be amended to add:

- property-linked evidence requirements;
- proof artifact schemas;
- evidence freshness rules;
- structural reachability verification;
- lifecycle/order verification;
- targeted mutation;
- failure injection;
- review convergence evidence;
- finding-disposition validation;
- unresolved proof as merge blocker.

---

## 8.2 Required Spec #20 Amendments

Code Standards #20 SHOULD be amended to add:

- integration ownership;
- composition-root ownership;
- lifecycle declaration requirements;
- runtime-bearing public-surface classification;
- entry-point/host inventories;
- static initialization analysis;
- bypass-path restrictions;
- reachability constraints;
- alternate-host/testhost architecture obligations.

---

## 8.3 Master Development Plan Amendment

The Master Development Plan SHOULD contain only a pointer:

> Project architecture and integration gates are governed by the Project Architecture Governance Specification, Testing Strategy #19, and Code Standards #20. Architectural requirements are admitted explicitly, objective settled rules are mechanically enforced where practical, and reviews terminate against proven admitted properties.

Detailed mechanics MUST NOT be copied into the master plan.

---

## 8.4 Adversarial-Review Amendment

The adversarial-review process SHOULD be updated so every finding records:

- evidence;
- severity;
- implicated requirement/property;
- disposition.

The current severity-driven termination model MUST be reconciled with this specification.

In particular:

- severity does not itself define blocker status;
- novel generalized concerns become Candidate Properties;
- admitted property violations remain fully adversarial;
- round budgets stop execution but do not grant approval.

---

## 8.5 FR-to-Enforcement Traceability

| FR Range | Enforcement Owner | Verification |
|---|---|---|
| FR-AG-001–008 | This spec / governance review | Property record validation |
| FR-AG-009–014 | Adversarial-review process | Finding ledger validation |
| FR-AG-015–020 | Adversarial-review + #19 | Review state / proof completeness |
| FR-AG-021–025 | #20 | Static analysis / design verification |
| FR-AG-026–032A | #19 | Proof artifacts / CI / evidence revalidation |
| FR-AG-033–036B | Agent process + tooling | Inventory/evidence audit / checker verification / proportionality review |
| FR-AG-037–040D | Governance review | Property registry / history / revalidation |

---

# 9. Approval Checklist

This specification MUST satisfy its own governance model before becoming authoritative.

## 9.1 Authority Checklist

- [ ] Every rule class has exactly one normative owner.
- [ ] No detailed #19 responsibility is duplicated here.
- [ ] No detailed #20 responsibility is duplicated here.
- [ ] Master-plan responsibilities remain pointer-level.
- [ ] Adversarial-review ownership remains procedural rather than normative-rule ownership.

---

## 9.2 Property Governance Checklist

- [ ] Candidate state defined.
- [ ] Admission criteria defined.
- [ ] Stable property ID defined.
- [ ] Admission record schema defined.
- [ ] Rejection defined.
- [ ] Amendment defined.
- [ ] Supersession defined.
- [ ] Retirement defined.
- [ ] Property revalidation trigger defined.
- [ ] Exception mechanism defined.

---

## 9.3 Finding Governance Checklist

- [ ] Blocker defined.
- [ ] Tradeoff defined.
- [ ] Residual Risk defined.
- [ ] Candidate Property defined.
- [ ] Severity separated from disposition.
- [ ] Finding record schema defined.
- [ ] Requirement linkage mandatory for blockers.
- [ ] Review termination requires complete dispositions.

---

## 9.4 Proof Checklist

- [ ] Structural reachability defined.
- [ ] Lifecycle/order defined.
- [ ] Failure injection defined.
- [ ] Mutation defined.
- [ ] Mutation obligations can retire when their failure mode disappears or stronger proof replaces them.
- [ ] Trigger matrix present.
- [ ] Repository-wide inventory requirement present.
- [ ] Evidence freshness present.
- [ ] Reproducibility present.
- [ ] Reusable proof declares a precise dependency surface.
- [ ] Affected proof is revalidated after material changes to that dependency surface.
- [ ] Unrelated changes do not automatically invalidate proof.
- [ ] Merge-critical governance tooling is itself verified.
- [ ] Governance-tool verification terminates without recursive checker chains.
- [ ] Computational proportionality permits bounded proof where exhaustive machine execution would be materially disproportionate.

---

## 9.5 Agentic Development Checklist

- [ ] Exhaustive mechanical-work assumption explicit.
- [ ] Unsupported agent assertions prohibited.
- [ ] Judgment domain explicit.
- [ ] Machine-enforcement domain explicit.
- [ ] Governance ratchet explicit.
- [ ] Rule-retirement and property-revalidation mechanisms prevent permanent checker accumulation.
- [ ] Agent-heavy development does not override computational proportionality.

---

## 9.6 Review Termination Checklist

- [ ] Applicable admitted properties identified.
- [ ] Every MUST-level property satisfied.
- [ ] Required proof complete.
- [ ] No Blockers open.
- [ ] Every finding dispositioned.
- [ ] Fresh final review completed.
- [ ] Round-budget exhaustion produces NON-CONVERGED, not APPROVED.

---

## 9.7 Downstream Landing Checklist

Before this specification is considered fully adopted:

- [ ] Spec #19 amendments landed.
- [ ] Spec #20 amendments landed.
- [ ] Master Development Plan pointer landed.
- [ ] Adversarial-review process reconciled.
- [ ] Property registry location created.
- [ ] Finding schema supported by review tooling or documented workflow.
- [ ] Applicable repository inventory tooling identified or implemented.

---

# Appendix A — Architectural Property Record Template

```text
Property ID:
Title:
State:

Normative statement:

Failure mode:

Scope:

Explicit exclusions:

Authoritative owner:

Evidence required:

Enforcement class:
[ ] Machine
[ ] Hybrid
[ ] Judgment

Activation:

Exceptions permitted:

Supersedes:

Decision rationale:

Last reviewed:
```

---

# Appendix B — Review Finding Record Template

```text
Finding ID:
Round:
Severity:

Summary:

Evidence:

Requirement / Property:

Disposition:
[ ] Blocker
[ ] Accepted Tradeoff
[ ] Residual Risk
[ ] Candidate Property
[ ] Resolved

Required action:

Owner:

Status:

Resolution evidence:
```

---

# Appendix C — Integration Ownership Contract

Applicable runtime-bearing components MUST provide:

```text
Component:

Owning host:

Owning assembly/project:

Composition root / integration point:

Construction or registration path:

Activation phase:

Update/use owner:

Shutdown/disposal owner:

Relevant testhost path:

Alternate supported paths:

Prohibited bypass paths:

Static initialization involved:
Yes / No

Lifecycle ordering requirements:

N/A fields with justification:
```

---

# Appendix D — Architectural Proof Artifact

```text
Change / Component:

Applicable admitted properties:

Repository surface examined:

ENTRY-POINT INVENTORY
- Production hosts:
- Testhosts:
- Tools:
- Alternate bootstrap paths:
- Static initialization paths:

STRUCTURAL REACHABILITY
- Required path:
- Evidence:
- Violations detected:
- Result:

LIFECYCLE / ORDER
- Required order:
- Evidence:
- Result:

FAILURE INJECTION
- Injected failure:
- Expected behavior:
- Observed behavior:
- Result:

MUTATION
- Mutation applied:
- Protected invariant / failure mode:
- Expected test/proof failure:
- Observed failure:
- Result:

Evidence commit/version:

Evidence dependencies:
- Specific hosts / entry points:
- Composition roots:
- Assemblies / dependency edges:
- Construction / registration paths:
- Lifecycle / ordering relationships:
- Public runtime surfaces:
- Static / alternate / bypass paths:
- Relevant tests:
- Governance tool/version:

Evidence invalidated by later changes:
Yes / No

If revalidated rather than regenerated:
- Change examined:
- Declared dependency affected:
- Reason prior evidence remains valid:

Final result:
PASS / BLOCK
```

---

# Appendix E — Enforcement Classification

### Machine

Use when compliance can reliably be determined from repository state or executable behavior.

Examples:

- dependency graph;
- forbidden API;
- entry-point inventory;
- registration presence;
- cross-reference resolution.

Machine enforcement used as required architectural evidence MUST itself be verified according to §6.6.

Automation converts a settled architectural property into repeatable enforcement; it does not make the enforcement implementation self-validating.

### Hybrid

Use when a machine can prove structural facts but judgment determines whether those facts constitute good architecture.

Example:

- tooling proves two systems are coupled;
- architectural judgment determines whether the coupling is justified.

### Judgment

Use only where the core question cannot safely be reduced to a checker.

Examples:

- whether an abstraction is appropriate;
- whether a property deserves admission;
- whether a tradeoff is acceptable.

---

# Appendix F — Governance State Summary

### Property

`Candidate → Admitted → Superseded/Retired`

or

`Candidate → Rejected`

### Finding

`Open → Blocker → Resolved`

`Open → Tradeoff → Accepted`

`Open → Residual Risk → Recorded`

`Open → Candidate Property → Property process`

### Review

`OPEN → CONVERGED`

or

`OPEN → NON-CONVERGED`

`CONVERGED` requires all applicable mandatory properties proven.

`NON-CONVERGED` means the process ended with unresolved gating obligations.

---

# Governing Summary

The project adopts the following architectural-governance loop:

> **Discover with judgment. Admit deliberately. Define the property precisely. Prove mechanically where the question is mechanical. Use judgment where the question is architectural. Encode settled conclusions into stable enforcement. Terminate review when the admitted obligations are proven.**

The desired long-term state is not zero architectural judgment.

It is a project in which agents do not repeatedly rediscover settled architectural rules, reviewers cannot create arbitrary new blockers during an active review, mechanical compliance cannot substitute for architectural quality, stale evidence cannot silently survive changes to the surface it proves, enforcement tooling is itself verified without recursive checker hierarchies, targeted mutation does not become a permanently expanding suite, obsolete properties do not remain active by inertia, and exhaustive proof does not become an uncontrolled computational burden.

---

# Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.5 | August 31, 2026 | — | A0 adoption review, round 1. §5.5 reworded from "tests SHOULD intentionally cause" to "the proof scope SHOULD include deliberately causing" — finding AG-A0-001, the one place the document addressed test authoring directly rather than proof scope, which §1.3 reserves to Spec #19. No normative obligation changed: the modality stays SHOULD and the nine failure conditions are unchanged. Status remains Draft pending human sign-off; see `docs/tracking/a0-governance-adoption-review.md`. |
| 0.4 | August 27, 2026 | — | Draft as created. Version history introduced retroactively at 0.5; rows before this one are reconstructed from the document header, not from a contemporaneous log. |
