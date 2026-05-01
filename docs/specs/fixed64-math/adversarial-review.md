# Fixed64 Math Library Specification #9 — Adversarial Review & Critique

Date: 2026-05-01  
Reviewer mode: adversarial / implementation-risk focused

## Executive Summary
The current draft is a useful skeleton but **not implementation-ready**. It has good directional requirements, yet it omits critical normative details required to avoid cross-platform drift and inconsistent behavior. The largest risks are: (1) underspecified rounding/tie rules at operation boundaries, (2) missing complete error-code taxonomy and precedence, (3) ambiguous checked/saturating semantics in sign-edge cases, and (4) no concrete machine-readable schemas despite requiring them.

## High-Severity Findings (Release-Blocking)

### H-1: Rounding policy is declared but not bound to each API
- **Problem:** Section 2 says APIs must declare rounding mode, but it does not provide a complete authoritative table mapping operation -> default rounding -> ties -> error behavior.
- **Why this breaks determinism:** Two teams can implement `mul` or `div` with different tie behavior and both claim compliance.
- **Required fix:** Add a normative matrix for all public APIs (including conversions and utility functions) with explicit tie-break semantics and signed-value examples.

### H-2: Overflow/underflow precedence rules are incomplete
- **Problem:** Multiple sections reference checked/saturating/unchecked modes, but there is no single precedence rule for interactions (e.g., divide-by-zero + sign + min-negate edge).
- **Why this breaks determinism:** Implementers may choose different first-failure precedence and return different error codes/results.
- **Required fix:** Add a single failure precedence order and a canonical operation × mode × outcome matrix with explicit expected raw outputs.

### H-3: Multiplication and division normative formulas are underspecified
- **Problem:** `wide >> 32` is stated, but no full rule for rounding when low bits are non-zero, no behavior for negative values in tie cases, and no explicit intermediate limits.
- **Why this breaks determinism:** Signed shifts and tie handling may differ by language/runtime abstractions.
- **Required fix:** Specify exact integer algorithm pseudocode with branch rules for all rounding modes.

### H-4: Conversion section lacks precise safe ranges and decimal parsing grammar
- **Problem:** Section 4 references boundaries but does not provide full exact ranges or accepted text formats.
- **Why this breaks determinism:** Parsing and conversion can diverge with locale/format differences.
- **Required fix:** Add exact integer bounds, decimal lexical grammar, required rejection cases, and examples.

### H-5: Performance budgets are not anchored to an identified benchmark host
- **Problem:** Nanosecond targets are listed without pinned CPU, frequency policy, compiler flags, OS scheduling controls, or statistical acceptance method.
- **Why this breaks governance:** CI pass/fail will be non-actionable and noisy.
- **Required fix:** Define benchmark host profile and confidence/variance thresholds.

## Medium-Severity Findings

### M-1: Error codes are referenced but not versioned or centrally cataloged
Need a canonical registry: symbol, numeric value, text, and stability policy.

### M-2: `Unchecked*` policy says lint-ban in simulation but no lint spec exists
Need rule identifiers, scope, suppression process, and CI enforcement points.

### M-3: Utility math error envelopes are required but not quantified
Need per-function max absolute error domains and exception buckets.

### M-4: Serialization schema is described but not actually specified
Need field-level schema, byte order test vectors, and forward/backward compatibility rules.

### M-5: Comparison semantics omit signed-zero style compatibility statement
Even though no NaN/Inf exists, explicitly state there is no alternative zero encoding and equality is raw-equality only.

## Low-Severity Findings

### L-1: Terminology drift across sections
Some sections use “checked mode” vs “Checked* APIs”; standardize naming.

### L-2: Version history entries are all v0.1 placeholders
Need changelog discipline with issue/decision references.

### L-3: Appendix references are promises without artifacts
Appendices list deliverables but contain no machine-readable examples yet.

## Cross-Section Contradictions / Gaps
1. Section 1 mandates deterministic traps/errors but no runtime contract defines transport type (result enum? status+value struct?).
2. Section 2 prohibits epsilon compare while Section 3 approximation APIs require error envelopes; no guidance on how downstream systems should assert approximate equality.
3. Section 5 requires zero allocations but Section 7 harness artifact generation may allocate heavily; scope boundary between runtime library and harness tooling is not explicit.

## Suggested Remediation Plan

### Phase A (Spec Hardening)
1. Add a normative “API Behavior Matrix” annex with all operations and modes.
2. Add deterministic pseudocode for add/sub/mul/div/sqrt rounding and failure precedence.
3. Add conversion grammar + exact ranges + examples.

### Phase B (Artifact Binding)
4. Add JSON schema files for constants and vectors.
5. Add benchmark protocol doc with pinned environment metadata.
6. Add determinism harness digest algorithm spec (hash choice, byte layout).

### Phase C (Governance)
7. Add lint policy document for `Unchecked*` usage.
8. Add compatibility/deprecation workflow checklist tied to release process.

## Concrete “Must Add Before Approval” Checklist
- [ ] Full operation/mode/failure matrix with expected raw outputs.
- [ ] Normative rounding table with tie examples for positive and negative values.
- [ ] Exact conversion safe ranges for every integer/float type in scope.
- [ ] Canonical parsing grammar and locale rejection behavior.
- [ ] Benchmark host and CI statistics acceptance definition.
- [ ] Machine-readable vector and constants schemas committed.
- [ ] Determinism harness digest and artifact format frozen.

## Overall Verdict
**Status: NOT READY FOR SECTION 9 APPROVAL.**  
The draft is directionally correct but currently too abstract to guarantee independent, identical implementations.
