# Fixed64 Math Library Specification #9 — Adversarial Critique (Pass 2)

> **Created:** May 6, 2026
> **Reviewer mode:** Adversarial / implementation-risk focused. Independent of the May 1, 2026 review (`adversarial-review.md`).
> **Scope:** All current spec files (`outline.md`, `section-1.md` … `section-8.md`, `section-9-approval-checklist.md`, `appendices.md`) at the v0.1–v0.2 versions checked in on 2026-05-01.
> **Purpose:** Surface remaining implementation-risk gaps after Pass 1 remediation, log each item, then validate (or correct) every conclusion against the actual file text.

---

## Executive Summary

The May 1 remediation closed the largest gaps from Pass 1 (added the API behavior matrix, mul/div pseudocode, conversion grammar, pinned benchmark host, error registry, vector schema, digest spec, lint policy). The spec is now substantially more concrete. However, several **new and previously-undetected issues** remain that would block independent implementations from converging on identical bit outputs, and several governance/tracking inconsistencies have appeared that would block Section 9 sign-off:

- A **correctness bug in the `CheckedMulNearestEven` pseudocode** (negative-operand path) that would silently produce wrong results.
- **Operator overload semantics are completely unspecified** despite C# being the implementation target — the most-used arithmetic API surface is undefined.
- The **deterministic algorithm for `sqrt`/`sin`/`cos`/`atan2` is not normatively pinned**, only suggested as "e.g., bounded Newton-Raphson". Two compliant implementations can diverge bit-for-bit.
- **Tracking documents disagree** about this spec's status: SPEC_INDEX.md says `NOT STARTED`, Section 9 says `IN REVIEW`, and v0.2 content exists across multiple sections.
- The **Section 9 approval checklist is a generic stub** that does not absorb Pass 1's "must add before approval" list.

The spec is **not yet ready for Section 9 sign-off**.

---

## Critique Items (Initial Findings)

### High Severity

#### H2-1 — `CheckedMulNearestEven` pseudocode is incorrect for negative operands

`section-2.md` §2.3.1:

```text
wide = int128(a.raw) * int128(b.raw)
q = wide >> 32                   // arithmetic shift
r = abs(wide) & ((1<<32)-1)      // remainder magnitude
half = 1<<31
if r > half: q += sign(wide)
if r == half and (q & 1) != 0: q += sign(wide)
```

The arithmetic right-shift `wide >> 32` is **floor division by 2^32**, not truncation toward zero. For negative `wide`, this gives a quotient that is more negative than the truncated value. The remainder definition `r = abs(wide) & ((1<<32)-1)` is the magnitude of `wide` mod 2^32, which is **not** the floor-division remainder of `wide` mod 2^32. The two values disagree whenever `wide` is negative and not a multiple of 2^32.

Worked example: `wide = -1` (i.e., the true product is the smallest negative quantum, `-2^-32 ≈ -2.33e-10`).

- `q = -1 >> 32 = -1` (arithmetic shift fills with sign bit).
- `r = |-1| & 0xFFFFFFFF = 1`. `half = 2^31`. `r < half`, no adjustment.
- Algorithm returns raw `-1`.
- Correct nearest-even result for `-2.33e-10` is `0` (raw `0`).

Worked example: `wide = -2.5 * 2^32`.

- `q = floor(-2.5) = -3`.
- `r = |wide| & mask = 2^31 = half`. `q & 1 = 1` (-3 is odd). Adjustment: `q += sign(wide) = -1`, so `q = -4`.
- Correct nearest-even for `-2.5` is `-2` (rounds toward the even integer); algorithm returns `-4`.

Both cases give wrong answers. **This is a determinism-and-correctness bug, not a stylistic issue.** A faithful reimplementation will produce wrong outputs for any operation whose intermediate widened product is negative and has a non-zero low-32-bit remainder. Recommended fix: split into magnitude + sign before shifting (round magnitude with banker's rule, then re-apply sign), or replace `wide >> 32` with explicit truncated division of `wide` by `2^32` and recompute the remainder via `wide - q * 2^32` so that the remainder is always non-negative for the floor case (and adjust the rounding rule to match).

#### H2-2 — Operator overload semantics are not specified

C# is the chosen implementation language (CLAUDE.md "When Writing Code"), and `Fixed64` is overwhelmingly likely to expose `operator+`, `operator-`, `operator*`, `operator/`, `operator==`, `operator<`, etc. The spec **never mentions operators**. Consumers will reach for `a + b` first; without normative semantics, callers will get one of `Checked`, `Saturating`, or `Unchecked` behavior at the implementer's whim — exactly the surface area the spec was supposed to lock down.

`section-1.md` §1.5 says "Default APIs used by gameplay/simulation code MUST map to checked behavior unless a subsystem spec grants an approved saturating/unchecked exception", but never names the operators as an API family or ties them to the `Checked*` semantics. The lint policy in Appendix F also targets `Unchecked*` by name; operators are invisible to that lint rule as written.

#### H2-3 — Deterministic algorithm for `sqrt`/`sin`/`cos`/`atan2` is not pinned

`section-3.md` §3.1: "Implementation MUST use deterministic iteration (e.g., bounded Newton-Raphson with fixed iteration cap)." The "e.g." makes the algorithm informative, not normative. §3.2 lists trig API but specifies no algorithm at all.

For cross-platform bit-exact determinism (the spec's stated goal, see outline Purpose; §7.3 "Core arithmetic MUST match exactly by raw bit value"), the algorithm — including iteration count, initial seed, lookup-table contents/quantization, and operation order — must be fully nailed down. Two implementations both following "deterministic iteration with a fixed iteration cap" can produce different raw outputs. The error envelope policy in §3.3 governs accuracy versus real-valued reference, not bit-exactness across implementations.

#### H2-4 — Tracking-document divergence on this spec's status

- `docs/specs/SPEC_INDEX.md` line 29: "Fixed64 Math Library | `fixed64-math/` | 2 | NOT STARTED | —".
- `section-9-approval-checklist.md` line 16: "Status: `IN REVIEW`".
- `section-2.md`, `section-4.md`, `section-5.md`, `appendices.md` carry `v0.2` version-history entries dated 2026-05-01.
- A full Pass 1 adversarial review exists (`adversarial-review.md`).

SPEC_INDEX.md is the canonical status source per CLAUDE.md, so its `NOT STARTED` is authoritative — but it is plainly inaccurate. Either the spec content is unauthorized (and the folder should be reverted), or the index needs to be moved to `IN PROGRESS` or `IN REVIEW`. This mirrors the existing CLAUDE.md "Tracking-doc divergence" open issue and needs lead-developer adjudication before §9 sign-off.

#### H2-5 — `section-8.md` §8.1 mandate ignores Stage 0/Stage 5+ staging

§8.1: "Fixed64 MUST be mandatory in simulation-critical gameplay, physics, collision, and replay paths."

CLAUDE.md "When Writing Code" and the April 26 OPEN ISSUE explicitly state Fixed64 migration is a **Stage 5+** concern; Stage 0 uses `float`. The seven approved physics specs (#1–#4, #6–#8) were drafted under that assumption and remain unchanged. As written, §8.1 is in direct conflict with the approved staging decision and with the contents of every approved upstream spec. Either §8.1 needs the Stage 5+ qualifier, or every approved physics spec is suddenly out of compliance — only the first option is consistent with the rest of the project.

#### H2-6 — Section 9 Approval Checklist is a generic stub

`section-9-approval-checklist.md` contains six unchecked generic boxes ("All required sections present", "FR coverage complete", etc.) and a status line. It does not:

- absorb Pass 1's "Must Add Before Approval" list (full op/mode/failure matrix, normative tie examples, exact conversion ranges, parsing grammar, benchmark protocol, machine-readable schemas, harness digest format),
- record verification evidence (file paths, line ranges, golden-vector IDs) the way other approved specs do,
- cite the open Pass 1 / Pass 2 critiques as gates,
- address the status conflict with SPEC_INDEX.md.

Per CLAUDE.md "Things That Have Gone Wrong Before — Fabricated checklist values", the rule is "verify every checklist entry against actual files". The current §9 has no entries to verify and no evidence anchors. It is far below the bar set by Pass Mechanics #5, Shot Mechanics #6, and Perception #7.

### Medium Severity

#### M2-1 — Failure behavior matrix in Appendix D is incomplete

Appendix D has four rows: `Negate`, `Abs`, `Div`, `Sqrt`. The outline (line 119) promised "Failure behavior matrix (operation × mode × outcome)". Missing operations include `Add`/`Sub` overflow, `Mul` overflow, `Fixed→Int` range, `Float→Fixed` range, `Float→Fixed` non-finite, parser failures, and `Clamp`/`Min`/`Max` corner cases (e.g., `Clamp(x, lo, hi)` when `lo > hi`). Section 2.1 has a partial behavior matrix in the body; the appendix should be the canonical full version, and currently is not.

#### M2-2 — `Float → Fixed64` conversion lacks a non-determinism warning

§4.2 specifies nearest-even rounding for `float → Fixed64`, but never warns that the **input** `float` may already be non-deterministic across platforms (FMA fusion, x87 80-bit intermediates, SIMD vs scalar codegen, denormals-as-zero modes). Even with deterministic rounding on the conversion step, the upstream float can differ. The spec should state explicitly that `float → Fixed64` MUST NOT be used to materialize simulation state from float arithmetic that wasn't itself generated under a Stage-5 deterministic-float regime; otherwise the conversion's "deterministic rounding" is a false guarantee.

#### M2-3 — Outline's appendix lettering does not match `appendices.md`

`outline.md` Appendices section (lines 114–120) lists: A=Constants, B=Error-bound derivations, C=Performance benchmark templates, D=Vector schema, E=Failure matrix, F=Incident report.

`appendices.md` actually contains: A=Constants, B=Error code registry, C=Vector schema, D=Failure matrix, E=Digest spec, F=Lint policy.

Five of six letters disagree on content. Cross-references such as "see Appendix D" become ambiguous depending on which file the reader is in. Either the outline must be updated to match the realized appendices, or the appendices renamed.

#### M2-4 — `section-4.md` §4.6 sign-prefix rule is ambiguous

`section-4.md` §4.6: "Canonical format: `[+|-]d+.d{1,10}`" combined with "Zero MUST format as `+0.0`."

The `[+|-]` notation is read as "optional sign" in some BNF dialects and as "required sign, choice of + or -" in others. The "+0.0" example implies positives carry a leading `+`, but that is not stated in plain prose, and §4.5's parsing grammar does the opposite — it accepts `sign?` as optional. So format and parse may not be inverses. The canonical format must be stated unambiguously (probably "sign required, `+` for non-negative, `-` for negative") and matched by the parser if round-tripping is to be guaranteed.

#### M2-5 — `Fixed64 → int32` boundary check interacts with rounding mode

`section-4.md` §4.1: "`Fixed64 -> int32` checked conversion MUST fail unless `raw` is within `[-2^31<<32, (2^31-1)<<32]` and fractional bits are handled per rounding mode."

The interaction is undefined: with toward-zero rounding, `raw = (2^31 - 1)*2^32 + 0xFFFFFFFF` (just below int32 max + 1) should succeed (truncates to `INT32_MAX`); with nearest-even, the same input rounds **up** to `2^31` and overflows int32. The spec should either define the range *after* applying the rounding mode, or specify per-mode pre-checks. As written, two implementations will disagree on the high-edge cases.

#### M2-6 — Platform matrix in §7.1 is not enumerated

`section-7.md` §7.1 says the harness "MUST execute across approved OS/CPU/CPU-feature/compiler/runtime combinations with pinned optimization flags" but lists none. "Approved" without an enumerated matrix is unfalsifiable; CI cannot gate on it; reviewers cannot tell whether ARM64-on-macOS or WASM are in scope.

#### M2-7 — `op_id` mapping/stability is not specified

Appendix E digest layout: `op_id:u16`. There is no table mapping each public API to a stable `op_id`. Without that, two implementations (or two versions) cannot produce comparable digests, defeating the cross-platform purpose of the harness. The mapping should live in the same appendix, with explicit "stable in major version" language matching Appendix B's error code policy.

#### M2-8 — No typed cross-references (`XC-`, `FM-`, `EC-`, `ERR-`) anywhere

CLAUDE.md mandates `XC-`/`FM-`/`EC-`/`ERR-` IDs for cross-spec references. `grep -rn "XC-\|FM-\|EC-\|ERR-" docs/specs/fixed64-math/*.md` returns zero matches. Section 8.4 names downstream subsystems generically. This will create rot quickly: when Deterministic Simulation #16 lands a normative requirement against Fixed64 (it already does — see CLAUDE.md OPEN ISSUE on `ERR-016-002` and the reciprocal `XC-` refs pending against #2/#8), there is no place in this spec to attach it.

#### M2-9 — "Equivalent host" is undefined in §5.2

§5.2: "Equivalent hosts MAY be used for local profiling, but CI gate decisions MUST use this pinned profile". "Equivalent" is unscoped — same vendor? same generation? same SMT topology? Without criteria, "equivalent" devolves into "whatever the developer has on their desk" and local results will not be comparable to CI.

### Low Severity

#### L2-1 — `sign(num*den)` in §2.3.2 invites overflow when read literally

The `Div` pseudocode uses `q += sign(num*den)`. `num` is already shifted left by 32 (so up to ~2^95 magnitude) and `den` is int128; their product overflows even int128. The intent is the sign of the conceptual quotient, i.e., `sign(num) * sign(den)` (or XOR-of-sign-bits). Faithful pseudocode-to-code translation would compute the actual product and either trap or wrap. Replace with `sign(num) * sign(den)` (or equivalent).

#### L2-2 — Formatter precision (10 fractional digits) versus quantum

`section-4.md` §4.6 caps the canonical format at 10 fractional digits. Quantum is 1/2^32 ≈ 2.328e-10. Worth confirming round-trip uniqueness in a worked example or an explicit assertion in §4.6. *(Validated and resolved below — 10 digits is sufficient.)*

#### L2-3 — `expected_flags` allowed values not enumerated

Vector schema (Appendix C) declares `"expected_flags": {"type": "array", "items": {"type": "string"}}`. The allowed strings are not constrained. Different harness implementations may emit different flag spellings (`OVERFLOW` vs `overflow` vs `ERR_FIXED64_OVERFLOW`), breaking digest comparison. Constrain to an enum referencing Appendix B error symbols.

#### L2-4 — Schema lacks an "operation produced error" representation

Appendix C lists `expected_raw` as **required**. For checked operations that should fail (e.g., `CheckedDiv(x, 0)`), there is no defined raw output. Either make `expected_raw` optional when `expected_flags` contains an error code, or define a sentinel encoding (e.g., `"0x0000000000000000"` plus required flag) — the current schema will not accept legitimate failure vectors.

#### L2-5 — No version history in `appendices.md` or `section-9-approval-checklist.md`

CLAUDE.md "When Writing or Editing Specs" requires "Append a version history entry to every modified file." Both files lack version-history sections entirely.

#### L2-6 — Saturating add/sub side not made explicit

§2.1 matrix says "clamp to min/max" for `SaturatingAdd`/`SaturatingSub` overflow without saying which side. The intent is obviously "min for negative-direction overflow, max for positive-direction overflow", but every other ambiguity in the matrix (rounding ties, conversion ranges, etc.) is spelled out — this one should be too, for symmetry and readability.

---

## Validation Pass

Each finding above is re-checked against the actual file text to confirm or correct.

| ID | Verdict | Notes |
|----|---------|-------|
| H2-1 | **Validated** | Re-derived two worked examples from the literal pseudocode; both produce wrong outputs. The `r = abs(wide) & mask` line is not the floor-division remainder for negative `wide`. Treat as a normative defect, not a style issue. |
| H2-2 | **Validated** | `grep -in 'operator' docs/specs/fixed64-math/*.md` returns zero matches. Operator surface is fully unspecified. |
| H2-3 | **Validated** | §3.1 uses "e.g." for the sqrt algorithm; §3.2 names trig functions but no algorithm. Cross-platform bit-exactness requirement (§7.3, outline Purpose) cannot be met with informative-only algorithm guidance. |
| H2-4 | **Validated** | Confirmed mismatch: SPEC_INDEX.md line 29 says `NOT STARTED`; `section-9-approval-checklist.md` line 16 says `IN REVIEW`; v0.2 entries dated 2026-05-01 exist in §2/§4/§5/appendices. |
| H2-5 | **Validated** | §8.1 quoted text is unconditional. CLAUDE.md "When Writing Code" pins Stage-5+ migration. The Stage-0 approved specs (#1–#4, #6–#8) currently use `float`; an unconditional MUST in §8.1 makes them retroactively non-compliant, which contradicts the April 26 OPEN ISSUE resolution. |
| H2-6 | **Validated** | §9 file is six generic checkboxes plus status line. Contrast with Perception §9 v1.7 and Shot Mechanics §9 (both detailed, evidence-anchored). |
| M2-1 | **Validated** | Appendix D has four rows; missing rows enumerated above are all visibly absent. |
| M2-2 | **Validated** | §4.2 contains no warning about float-input non-determinism. |
| M2-3 | **Validated** | Side-by-side comparison shows five of six appendix letters disagree on content between `outline.md` and `appendices.md`. |
| M2-4 | **Validated** | The `[+|-]` notation versus the explicit "+0.0" example is a real ambiguity, especially against §4.5's `sign := "+"\|"-"` with `sign?` (optional) parser grammar. |
| M2-5 | **Validated** | Two-mode worked example confirms range check and rounding interaction is undefined at the high edge. |
| M2-6 | **Validated** | §7.1 contains no enumerated matrix. |
| M2-7 | **Validated** | Appendix E declares `op_id:u16` with no mapping table or stability clause. |
| M2-8 | **Validated** | `grep -rn 'XC-\|FM-\|EC-\|ERR-' docs/specs/fixed64-math/*.md` returns no results. |
| M2-9 | **Validated** | "Equivalent" appears once in §5.2 with no definition. |
| L2-1 | **Validated** | `sign(num*den)` is conceptually correct but a literal-code hazard; `sign(num) * sign(den)` (or sign-bit XOR) is preferred in pseudocode meant for re-implementation. |
| L2-2 | **Rejected on validation** | 10 fractional decimal digits gives precision ~5e-11 per nearest-half rounding, finer than the quantum 2.328e-10. Adjacent Fixed64 values therefore map to distinct 10-digit strings, and round-trip is safe. The original concern is unfounded; the only remaining nit is that §4.6 doesn't *state* the round-trip property explicitly, which is a documentation taste issue, not a correctness gap. **Downgrade to informational; do not block.** |
| L2-3 | **Validated** | Schema permits any string for `expected_flags` items. |
| L2-4 | **Validated** | `expected_raw` is in the `required` list; no sentinel for error-only outcomes. |
| L2-5 | **Validated** | Both files lack version-history sections. |
| L2-6 | **Validated** | §2.1 matrix wording does not specify which side of clamp applies to over- vs under-flow. |

### Amendments after Validation

- **L2-2 is withdrawn** as a correctness concern. Restated as an informational nit: §4.6 could optionally state the round-trip-uniqueness property explicitly. Not a blocker.
- **All other findings stand as filed.**

---

## Severity Summary

| Severity | Filed | Validated | Withdrawn |
|----------|------:|----------:|----------:|
| High     | 6     | 6         | 0         |
| Medium   | 9     | 9         | 0         |
| Low      | 6     | 5         | 1 (L2-2)  |
| **Total**| **21**| **20**    | **1**     |

---

## Recommended Next Actions

1. **Block §9 sign-off** until H2-1 (mul pseudocode bug), H2-2 (operator semantics), H2-3 (algorithm pinning), H2-5 (Stage-5+ qualifier), and H2-6 (real approval checklist) are resolved.
2. **Reconcile tracking** (H2-4): pick one of `IN PROGRESS` or `IN REVIEW` in SPEC_INDEX.md and update §9 to match. File a fix manifest entry analogous to `fix-manifest-pass-mechanics.md`.
3. **Fix the mul pseudocode** (H2-1) by either splitting magnitude/sign before the shift or replacing the floor-shift with truncated division. Add golden vectors covering negative-product corner cases (small negative wide, half-tie negative wide, `INT64_MIN` operand).
4. **Add an Operator Semantics subsection** under §2 (or a new §2.x) pinning `+ - * / == < <= > >= == !=` to `Checked*` semantics and adding `Saturating`/`Unchecked` only via named methods.
5. **Pin algorithms** for `sqrt`, `sin`, `cos`, `atan2` in §3 (algorithm name, iteration count, starting seed, lookup tables if any, evaluation order). Add bit-exact golden vectors per §6.4.
6. **Reconcile outline appendix lettering** with `appendices.md` (M2-3).
7. **Fold Pass 1 + Pass 2 "must add" items** into a real `section-9-approval-checklist.md` with file-path and line-range evidence anchors, the way Perception §9 and Shot Mechanics §9 do.
8. **Add typed cross-references** (XC-/FM-/EC-/ERR-) to the integration section so that `ERR-016-002` and reciprocal references from #2/#8 have a documented home.

---

## Resolution Log (2026-05-06)

All 20 validated findings have been resolved in the same-day Pass 2 remediation commit. Mapping:

| ID | Resolution | Touched files |
|----|------------|---------------|
| H2-1 | Mul/div pseudocode rewritten in magnitude+sign formulation; negative-operand worked examples now pass. | `section-2.md` §2.3.1, §2.3.2 |
| H2-2 | Operator overload binding table added (operators bind to `Checked*`). | `section-2.md` §2.8 |
| H2-3 | `sqrt` pinned to paired-bit algorithm; `sin`/`cos`/`atan2` pinned to CORDIC N=32 with normative angle table and `CORDIC_K`. | `section-3.md` §§3.1–3.2; `appendices.md` Appendix A and Appendix G |
| H2-4 | SPEC_INDEX.md updated to `IN REVIEW`; §9 status aligned. | `SPEC_INDEX.md`; `section-9-approval-checklist.md` |
| H2-5 | §8.1 made stage-gated (Stage 0–4 float / Stage 5+ Fixed64). | `section-8.md` §8.1 |
| H2-6 | §9 rewritten with evidence-anchored checklist citing file paths and section numbers. | `section-9-approval-checklist.md` |
| M2-1 | Failure behavior matrix expanded to cover Add/Sub/Mul/Div/Negate/Abs/Sqrt/Clamp/Min/Max/Fixed→Int/Float→Fixed/Parse. | `appendices.md` Appendix D |
| M2-2 | Cross-platform float-input non-determinism warning added with permitted/forbidden call-site list. | `section-4.md` §4.2 |
| M2-3 | Outline appendix lettering reconciled with `appendices.md`; deferred items noted. | `outline.md` |
| M2-4 | Sign prefix rule made unambiguous (optional on parse, required on format) with round-trip-uniqueness statement. | `section-4.md` §4.5, §4.6 |
| M2-5 | `Fixed64 -> int32` made two-step (round, then range-check) with worked-example matrix across rounding modes. | `section-4.md` §4.1 |
| M2-6 | Six-row enumerated platform matrix (OS / arch / CPU floor / compiler / flags) with release-blocking flags. | `section-7.md` §7.1 |
| M2-7 | Op_id mapping table and `flags:u32` bit encoding pinned. | `appendices.md` Appendix E.1 |
| M2-8 | Typed cross-reference index (XC-/FM-/EC-/ERR-) added; reciprocals against #1, #2, #3, #8, #16 named. | `section-8.md` §8.7 |
| M2-9 | "Equivalent host" criteria defined (microarch, clock, OS, compiler, optimization, SMT, isolation). | `section-5.md` §5.2 |
| L2-1 | `sign(num*den)` removed; magnitude+sign formulation eliminates the literal-overflow hazard. | `section-2.md` §2.3.2 |
| L2-3 | `expected_flags` constrained to enum of Appendix B error symbols. | `appendices.md` Appendix C |
| L2-4 | `expected_raw` made conditional on `expected_flags` emptiness; failure sample added. | `appendices.md` Appendix C |
| L2-5 | Version-history sections added to `appendices.md` and `section-9-approval-checklist.md`. | both files |
| L2-6 | Saturating Add/Sub clamp side specified as "sign of true result". | `section-2.md` §2.4 |

L2-2 was withdrawn at validation time (see table above) and required no remediation.

Status post-fix: every High and Medium finding is closed in the spec text itself; remaining open items in `section-9-approval-checklist.md` §9.8 are downstream artifacts (golden-vector corpus, CI benchmark first-pass run, full-matrix harness digest, ownership ledger entries, reciprocal XC from #16) that require harness implementation or other specs to advance, and are not blockers on the spec text.

## Version History

- v0.2 (2026-05-06): Added Resolution Log; all 20 validated findings closed in same-day Pass 2 fix commit.
- v0.1 (2026-05-06): Initial Pass 2 critique. 21 findings filed; 20 validated; L2-2 withdrawn after re-derivation.
