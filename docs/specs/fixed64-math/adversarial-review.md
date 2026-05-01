# Fixed64 Math Library Specification #9 — Adversarial Review & Critique

Date: 2026-05-01  
Reviewer mode: adversarial / implementation-risk focused
Validation pass: 2026-05-01 against `section-1.md` … `section-8.md` and `appendices.md`

## Executive Summary
The current draft is a useful skeleton but **not implementation-ready**. It has good directional requirements, yet it omits critical normative details required to avoid cross-platform drift and inconsistent behavior. The largest risks are: (1) underspecified rounding/tie rules at operation boundaries, (2) missing complete error-code taxonomy and precedence, (3) ambiguous checked/saturating semantics in sign-edge cases, and (4) no concrete machine-readable schemas despite requiring them.

## Validation Outcome (Conclusions + Solutions)
All **High-Severity** conclusions are validated as accurate against the current spec text. Most Medium/Low findings are also validated; one item is downgraded from contradiction to “clarification gap.” Proposed remediation phases (A/B/C) are appropriate and sufficient if made normative.

### Validation Matrix

| Finding | Validation | Evidence snapshot | Solution quality |
|---|---|---|---|
| H-1 Rounding policy not bound per API | **Validated** | Section 2.4 lists modes but no operation-by-operation default/tie table; Sections 2.2/2.3 say “configured/deterministic rounding” without tie examples. | **Strong** — adding a normative matrix is the correct fix. |
| H-2 Failure precedence incomplete | **Validated** | Error behaviors are listed piecemeal (e.g., divide-by-zero, abs/negate min edge), but no global precedence order exists. | **Strong** — precedence + operation×mode matrix is necessary. |
| H-3 Mul/div formulas underspecified | **Validated** | `wide >> 32` exists, but tie handling and signed rounding branch rules are absent. | **Strong** — deterministic pseudocode is required. |
| H-4 Conversion ranges/grammar incomplete | **Validated** | Section 4 references boundaries and explicit rounding but omits exact numeric ranges per type and lexical grammar. | **Strong** — grammar + ranges + reject cases needed. |
| H-5 Perf budgets unanchored | **Validated** | ns budgets exist, but no pinned host spec (CPU/governor/toolchain/OS controls/stat test). | **Strong** — benchmark protocol should be normative. |
| M-1 Error catalog/versioning | **Validated** | Error codes named but no centralized numeric registry/stability policy. | **Strong** |
| M-2 Unchecked lint policy underspecified | **Validated** | Lint-ban is required, but no rule IDs/scope/suppression/CI binding text. | **Strong** |
| M-3 Utility error envelopes unquantified | **Validated** | Envelope requirement appears, but no per-function numeric limits in spec body. | **Strong** |
| M-4 Serialization schema missing | **Validated** | Schema requirement exists, but no concrete field definitions/test vectors are provided yet. | **Strong** |
| M-5 Signed-zero compatibility statement absent | **Validated (minor)** | Equality/order semantics are defined, but explicit “single zero encoding” statement is missing. | **Acceptable** |
| L-1 Terminology drift | **Validated** | Mixed phrasing (`checked mode`, `Checked*`) appears across sections. | **Good cleanup** |
| L-2 Placeholder version history | **Validated** | Sections are all v0.1 initial-draft entries. | **Good governance fix** |
| L-3 Appendix artifacts missing | **Validated** | Appendices list intended artifacts without concrete content in-file. | **Good governance fix** |

## Cross-Section Contradictions / Gaps (Validated with one adjustment)
1. Runtime error transport contract is not yet explicit (result enum vs struct). **Validated.**
2. No guidance connecting strict compare semantics to approximate-utility assertions in downstream tests. **Validated.**
3. “Zero allocations” vs harness artifact generation is better framed as a **scope clarification gap** (runtime library vs testing harness), not a hard contradiction. **Adjusted severity: medium clarification issue.**

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
