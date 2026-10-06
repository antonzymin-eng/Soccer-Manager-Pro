# System XI — Localization #49 L2 Implementation Plan

**Created:** October 6, 2026\
**Last Updated:** October 6, 2026\
**Version:** 0.2\
**Status:** DRAFT PLAN — source audited; owner decision on ERR-049-005 recorded; proposed ERR-049-006 (§4.2) needs owner approval\
**Purpose:** plan the #49 L2 slice (immutable in-memory catalogue, template expander and the production `ILocalizer`), and the ERR-049-005 discharge that ships with it.\
**Baseline:** `main` at `ce2e2a36152590e623602ec633ce568fdfe8b04d` (PR #482 merge). L1 core landed at `f4e8bed4648e5b3e7b7c1437d3065472981fb288`.

## 1. Authority

- **Spec:** #49 Localization & Accessibility (`docs/specs/localization-accessibility/`, APPROVED):
  FR-LC-005–011 and FR-LC-008a, KD-3 (template model), KD-5 (fallback), §2.3 F1–F6, §3.2 (renderer),
  §3.5 (expansion), §4.2 (file layout).
- **Execution authority:** [`localization-implementation-plan.md`](localization-implementation-plan.md)
  v2.7 §6 (L2), §3.4 (ERR-049-005 discharge stage), §6.5 (no Wave-8 leakage), §6.6 (L2 exit).
  This plan chooses implementation boundaries inside §6. It does not widen §6's scope.
- **Consumer contract:** [S0 binding contracts](../design/ux-s0-binding-contracts.md) §4 and the
  [S0 journey](../design/ux-s0-pm1-journey.md) §14.7. The P5b copy/scale/screens slice will inject
  one L2 `ILocalizer` and call only `Resolve`. It never calls `Render`.
- **Defect record:** `spec-error-log.md` ERR-049-005 (OPEN, assigned to L2).

The spec wins over this plan wherever they differ. Proposed types and members are TO BUILD.

## 2. Scope

**In scope (plan §6.1–6.4):**

1. An immutable, host-free, per-locale in-memory catalogue. It holds static strings, ordered template
   variants per `TextTemplateId` and producer-scoped citation clauses.
2. The template expander: pure named-slot substitution plus one bounded plural-or-gender selector per
   template variant (KD-3).
3. `Localizer : ILocalizer`, which implements §3.2 `Resolve`/`Render` with the KD-5 fallback order.
4. Construction-time coverage of caller-supplied required identities (FR-LC-008a / F5), now extended to
   static keys (§4).
5. The ERR-049-005 discharge: spec back-propagation, the error-log update and executable proof, in the
   same commit as the code.

**Out of scope (plan §6.5, unchanged):** locale file formats, loaders and packaging; BCP-47
validation; Unicode normalization; pseudo-localization; coverage percentages and offered-locale
logic; fonts and glyphs; translation interchange; built-in CLDR plural tables for real locales;
a11y content (KD-4, Wave 8); live locale switching or change signals (S0 binding contracts §4.4);
any producer boundary adapter (L3B); any client consumer (P5b copy/scale/screens slice).

L2 adds no assembly reference in either direction. `localization.asmdef` keeps `"references": []`,
and the L1 tripwire `NoOtherProductionAsmdef_ReferencesLocalizationAtL1` stays unchanged, because L2
adds no consumer.

**Handoff obligations for later slices.** That tripwire rejects every production assembly that
references `TacticalDirector.Localization`, so it blocks both later consumers until they change it:

- **P5b copy/scale/screens slice:** must narrow the tripwire to admit exactly its client consumer
  assemblies (today that is `TacticalDirector.ClientApp`; any other must be named in that slice),
  keep the FR-LC-012 ban on every sim/loop assembly, and record the new references in #20
  (`code-standards/section-3.md`) and the #49 references in the same commit.
- **L3B:** admits `TacticalDirector.LocalizationBoundary` under the same rule (plan §5.5).

Each slice admits only its own named consumer. Neither replaces the tripwire with a general allowance.

## 3. Audited source

| Item | State at baseline | Consequence for L2 |
|---|---|---|
| `ILocalizer` | `string Resolve(LocalizationKey)`, `string Render(in LocalizedTextRequest)`. L1 froze both signatures. | Implement as is. No `Try` member and no provenance output. |
| `LocalizedTextRequest` | `Id`, `SelectionDraw` (`ulong`), `Slots`, `Selectors`, `HasCitedEpisode`, `CitationKind` (`int`). | Clause key = `(Id.ProducerTag, CitationKind)`. |
| `NamedSlotSet` / `NamedSelectorSet` | Immutable, sorted, ordinal `TryGetValue`. Duplicate names are rejected. | The expander reads them by name and never enumerates them. |
| `SelectorOperand` | Optional cardinal (`long`) and optional `GrammaticalGender`; `IsValid` when either is present. | The plural selector reads `CardinalValue`; the gender selector reads `Gender`. |
| `LocaleId` | Trimmed and lower-cased; `BaseLocale` = `en`. | Catalogue identity. No further validation (§6.5). |
| L1 structural tests | Public type shape may use only `System*` or Localization types. No mutable static fields. No field or type name containing `Random`, `Rng`, `Save` or `Snapshot`. No `[Serializable]`. | New types must comply. Use instance `readonly` fields and no static caches. |
| Consumers | No production assembly or test outside `src/localization/` references `ILocalizer`. | No call-site migration is needed. |
| Living-world expansion | `InteractionTextGenerator.Expand` chains `.Replace("{subject}")`, `.Replace("{opponent}")`, `.Replace("{score}")`. | Base-locale identity reference for §5.4 and §4.2 (proposed ERR-049-006). |

## 4. Spec defects discharged with L2

### 4.1 ERR-049-005 — owner decision

**Decision (owner, October 6, 2026):** construction coverage for admitted static keys, plus one
defined terminal result for a key that was never admitted.

1. **Construction coverage.** The caller passes the set of required static keys, as it already does
   for template ids and clauses (plan §6.4). `Localizer` construction fails, listing every missing
   identity, if the base catalogue lacks any required key. Every admitted key therefore always
   resolves, through the selected locale or the base locale.
2. **Terminal result.** `Resolve(key)` for a key absent from both the selected and the base catalogue
   returns `string.Empty`. It does not throw, does not mutate anything and does not show a `‹key›`
   marker. FR-LC-011 permits that marker only in dev builds, and plan §6.2 omits it at T0. The key
   text is rejected as the terminal value for the same reason: it would be a production key dump.
3. **Reachability.** A key can reach the terminal path only if it is outside the admitted set, which is
   a code defect. The P5b copy slice is expected to pass every `ui.s0.*` key as required coverage
   (TO BUILD; binding contracts §4.1 already requires base coverage of every admitted role), so no
   shipped S0 label could be blank. That satisfies the journey §14.7 rule that no exception, key dump
   or blank label is an acceptable shipping result.
4. **S0's own proof stays mandatory.** L2's construction coverage does not replace the client's
   separate content validation and shipped-content proof (binding contracts §4.1 and §4.4). The S0
   slice still validates every authored pattern and proves which content shipped.

**Spec back-propagation in the implementing commit** (per `err-file-and-backprop`):

- `section-2.md`: FR-LC-011 gains the terminal-result sentence. FR-LC-008a and F5 extend coverage to
  caller-admitted static keys. New failure-mode row F7 covers a non-admitted identity absent from
  both catalogues. Version-history row added.
- `section-1.md`: KD-5's precedence line gains the production terminal step. Version-history row added.
- `section-3.md`: the §3.2 `Resolve`/`Render` pseudocode shows coverage and the terminal step.
  Version-history row added.
- `section-7.md`: ERR-049-005 marked RESOLVED in L2. Version-history row added.
- `spec-error-log.md`: ERR-049-005 entry and index row marked RESOLVED, with the test names as evidence.
- `SPEC_INDEX.md`: #49 status stays APPROVED. This is a back-prop, not a re-approval.

If §8 Q1 is accepted, the same terminal rule also covers `Render` of a non-admitted template id and a
clause missing from both catalogues. Those are recorded in the same back-prop, under F1/F2, as
defensive behaviour.

### 4.2 Proposed ERR-049-006 — `Render` expansion cannot be identical to chained `.Replace`

**Defect.** KD-3, FR-LC-009 and §3.5 require base-locale expansion to be identical to
`InteractionTextGenerator`'s chained `.Replace` (subject, then opponent, then score), and FR-LC-016
and §3.6 rely on that identity. A generic expander cannot meet it in general, for two reasons:

- **Re-expansion.** Chained `.Replace` re-scans substituted values. A subject named `{opponent}`
  becomes the opponent's name. That is order-dependent behaviour on player-supplied data.
- **Order.** The result depends on the replacement order. `NamedSlotSet` sorts slot names ordinally
  (opponent, score, subject), and the producer-agnostic core has no producer order to follow. Copying
  living-world's order into the core would break FR-LC-012/KD-6.

The two agree whenever no slot value contains another slot's `{name}` token, which holds for every
row in the current corpus and oracle.

**Proposed resolution (requires owner approval, since it changes approved spec text):** generic
`Expand` performs single-pass, non-recursive substitution. Each `{name}` token in the template is
replaced once, and substituted values are never re-scanned. Base-locale identity with today's output
is required only when no slot value contains a `{name}` token. In that case single-pass and chained
`.Replace` give the same result in any order.

**Back-prop in the implementing commit:** KD-3 (`section-1.md`), FR-LC-009 and FR-LC-016
(`section-2.md`), and §3.5/§3.6 (`section-3.md`) gain the single-pass rule and the identity condition.
Appendix C's slot-expansion row, which currently says "yes" unconditionally, gains the same condition. A new `spec-error-log.md` entry is filed
(re-check that the id is still free at filing time). Test T13 proves it now (§6). L3B's oracle
comparison then confirms identity on the real corpus. This changes no S0 behaviour: S0 uses only
`Resolve`, and the client formatter's own non-recursive rule is separate.

## 5. Design

### 5.1 Types (all in `src/localization/`; each new file has a `.meta`)

| File | Visibility | Role |
|---|---|---|
| `PluralCategory.cs` | public enum | `Other = 0`, `One`, `Few`, `Many` (KD-3's four CLDR categories; `Other` is the default). |
| `TemplateForm.cs` | public readonly struct | One sub-form: optional `PluralCategory` or `GrammaticalGender` key, plus its text. |
| `TemplateVariant.cs` | public sealed class | One variant: either plain text, or a declared selector (name and kind) with its forms. Construction requires a default form. |
| `TemplateCatalogue.cs` | public sealed class | One locale: `LocaleId`, static rows, variants per `TextTemplateId` keyed by index, clause rows per `(producerTag, citationKind)`, and an optional plural rule. Copies caller arrays. Rejects duplicates and negative indices. `Localizer` checks contiguity against the base catalogue (§5.2). |
| `CatalogueCoverage.cs` | public sealed class | Required static keys, template ids and clause keys, supplied by the caller or boundary. The core never enumerates a producer roster. |
| `TemplateExpander.cs` | internal static | Pre-parsed segment expansion and selector choice. Has no state. |
| `Localizer.cs` | public sealed class | `ILocalizer`. Constructor: `(TemplateCatalogue baseCatalogue, TemplateCatalogue selected, CatalogueCoverage coverage)`, where `selected` may be null or the base catalogue. |

That is seven source files, under the §4.2 names where the spec names them. Small row-value types may
fold into `TemplateCatalogue.cs` during implementation if the type-shape test allows it.

### 5.2 Construction (all failures throw `ArgumentException` naming every offending identity, in ordinal order)

- The base catalogue's locale must be `LocaleId.BaseLocale`.
- **Coverage (F5 + ERR-049-005):** every required static key, template id (at least one variant;
  a missing row is tested, not only an explicit empty one) and clause key must exist in the base
  catalogue.
- **Base variant indices:** for each id, the base catalogue's indices must be exactly `0..n-1`
  with no gap. That defines `n = variantCount(BaseLocale, Id)`.
- **Selected catalogue (FR-LC-008):** translated indices may be sparse, but each one must lie in
  `[0, n)`. A missing translated index falls back to the base variant at that index. An index at or
  above `n` is rejected, because it could never render. See §8 Q4 on orphans.
- **Templates:** placeholders are parsed once at construction. Unbalanced braces in a template are
  rejected. Static rows are opaque and are never parsed (§5.3).
- **Selectors:** a variant that declares a plural selector requires its catalogue to supply a plural
  rule. The base English catalogue declares none (KD-3 identity).

`Localizer` and catalogues are immutable after construction and hold only `readonly` instance fields.
Concurrent reads are safe without locks **provided the supplied plural rule is pure and total**
(§8 Q3). `readonly` fields cannot make a captured delegate pure, so purity is a documented caller
contract, not something L2 can enforce.

### 5.3 `Resolve(key)` (§3.2, KD-5)

Selected row → base row → `string.Empty` (§4). The returned string is the authored row exactly. L2
never parses or formats static rows, because S0 static rows are .NET composite patterns (`{0}`, `{1:D}`)
that the client formatter consumes (binding contracts §4.1).

### 5.4 `Render(request)` (§3.2, FR-LC-007–010)

1. `n` = base variant count for `request.Id`. If `n == 0` (not admitted), return `string.Empty`
   (§8 Q1).
2. `variant = (int)(request.SelectionDraw % (ulong)n)`. The modulo is computed in `ulong` before
   narrowing (FR-LC-007/020).
3. Take the selected locale's variant at that index if present, otherwise the base variant at the
   same index (FR-LC-008).
4. If the variant declares a selector, read its operand from `request.Selectors`:
   - plural: category = the catalogue's plural rule applied to `CardinalValue`;
   - gender: category = `Gender`.

   Use the matching form. Fall back to the default form if the operand is missing, lacks the needed
   field, or has no matching form (FR-LC-011, no throw).
5. Expand placeholders in a single pass: each `{name}` with a slot is replaced by the slot value, and
   substituted values are never re-scanned. A placeholder with no slot is left verbatim, as `.Replace`
   does today.
6. If `HasCitedEpisode`, look up the clause by `(Id.ProducerTag, CitationKind)`: selected, then base.
   If found, append `" " + clause`. If absent from both, append nothing (§8 Q1).

The renderer draws no random numbers, advances no tick, writes no state and does no producer-specific
formatting (FR-LC-005).

## 6. Tests (`src/localization/tests/`, host-free NUnit)

| # | Test | Proves |
|---|---|---|
| T1 | Worked render §3.6: base-only catalogue, draw 5, two variants → variant 1, exact string | FR-LC-007, base identity |
| T2 | Draw near and at `ulong.MaxValue`, and above `int.MaxValue`, select the expected variant | `ulong` modulo before narrowing |
| T3 | Sparse translation: base indices `0,1,2`, selected indices `0,2`. Draws selecting 0 and 2 give selected text; a draw selecting 1 gives base text at index 1 | FR-LC-008 per-index fallback |
| T4 | Variant count comes from base even when the selected locale has a full set | Locale-independent count (KD-2) |
| T5 | Missing required static key / template id / clause → construction throws (separate mutant for each) | F5, ERR-049-005 construction half |
| T6 | Required template id with no rows at all (not an empty row) → throws | Plan §6.4 "missing row" requirement |
| T7 | `Resolve` of a non-admitted key absent from both → `""`; repeated calls equal; other keys unaffected | ERR-049-005 terminal half: no throw, no mutation |
| T8 | Selected static row present → selected; absent → base | KD-5 for static keys |
| T9 | Static row containing `{0}` and `{1:D}` is returned byte-identical | Static rows are opaque (S0 contract) |
| T10 | Synthetic non-English catalogue with a test plural rule chooses `Few` for 3 through the real `Render`; English base takes the identity path | FR-LC-009 conformance (plan §6.6) |
| T11 | Gender form chosen by operand; unknown or missing operand → default form; plural selector with a gender-only operand → default form | Bounded selector, FR-LC-011 |
| T12 | Clause appended with one space; selected → base fallback; same `CitationKind` under two producer tags does not collide; `HasCitedEpisode == false` appends nothing | FR-LC-010, producer scoping |
| T13 | Single-pass expansion: a subject named `{opponent}` stays literal (chained `.Replace` would expand it); output for brace-free values equals the chained `.Replace` result; an unknown placeholder stays verbatim | §5.4 step 5, ERR-049-006 |
| T14 | Construction rejects: wrong base locale, duplicate rows, a gap in base indices, a selected index at or above the base count, unbalanced braces, plural selector without a rule | §5.2 |
| T15 | Caller arrays mutated after construction do not change output | Immutability |
| T16 | Non-admitted template id → `""`; required clause absent from both is impossible by construction; an unrequired missing clause appends nothing | §8 Q1 defensive paths |
| — | Existing L1 locks keep passing: no references, public type shape, no mutable static, no forbidden state, L1-only tripwire | Layer and state constraints |

## 7. Implementation order and landing set

1. Branch from current `main`. Re-check that the ERR-049-005 entry text is still as audited.
2. Add `PluralCategory`, `TemplateForm`, `TemplateVariant`, `CatalogueCoverage`, then
   `TemplateCatalogue` with its construction validation, plus tests T5, T6, T14 and T15.
3. Add `TemplateExpander` and `Localizer`, plus tests T1–T4, T7–T13 and T16.
4. Make both spec back-props and error-log updates in the same commit as the code: ERR-049-005 resolved, and ERR-049-006 filed and resolved if the owner approves §4.2.
5. Close out with `landing-close-out`: `CHANGELOG.md`/`CHANGELOG-src.md` headers; `file-manifest.md`
   (new `.cs`/`.meta` paths); `open-issues.md` #49 entry; `localization-implementation-plan.md` version
   row (L2 executed); roadmap and audio-plan D49 wording (L2 delivered; the caption boundary is still
   open); this plan's status.
6. Gate: `bash tools/run-tests-local.sh --pr` (the policy runner, not the lower-level executor) with no
   new failures. Run `tools/doc-consistency-check.py` and `tools/assembly-tier-check.py`.
7. Before merge: the owner compiles the Localization assemblies in pinned Unity 6000.4.9f1 on the PR head
   (precedent: plan §5.5 for L1).

Expected size: about seven source files and two test files in one PR. No match-engine, save, schema or
RNG change, so the full Linux gate is runnable on a worker.

## 8. Open choices for review (each has a recommendation)

- **Q1. Apply the terminal rule to `Render` as well?** A non-admitted template id would otherwise divide
  by zero, and FR-LC-011 forbids a crash. **Recommend:** yes. Return `""` for a non-admitted template id,
  and append nothing for a clause absent from both catalogues. Both cases are unreachable for admitted
  identities.
- **Q2. Selector representation.** Option (a): forms in the data model (a variant declares one selector
  and keyed forms). Option (b): an inline syntax such as `{n, plural, one{…} other{…}}`.
  **Recommend (a):** it needs no new grammar, escaping or parser, malformed forms fail at construction,
  and KD-3's "bounded" rule is enforced by type. Combined plural+gender selection in one variant is
  deferred.
- **Q3. Plural rules.** **Recommend:** each catalogue receives an optional `Func<long, PluralCategory>`.
  It is required only if that catalogue declares a plural selector. L2 ships no built-in locale rule;
  tests use a synthetic rule. Real CLDR tables arrive with real locales (Wave 8). The alternative,
  shipping CLDR rules in L2, is Wave-8 scope under §6.5. Risk: a delegate can be impure. The
  contract requires a pure, total function; `Render` treats an undefined returned category as the
  default form; and the thread-safety claim in §5.2 depends on that contract.
- **Q4. Selected-locale orphans.** These are selected rows whose key, id or clause is absent from base.
  **Recommend:** reject at construction. An orphan can never render, because the base catalogue is the
  admission authority, so it most likely indicates a typo.
- **Q5. Single-pass expansion versus chained `.Replace`.** Now proposed ERR-049-006 (§4.2):
  single pass, with the approved spec text corrected in the implementing commit and tested by T13.
  This deliberately changes approved spec behaviour, so it needs owner approval. (v0.1 cited the S0
  non-recursive rule as its authority. That rule governs the client formatter over `Resolve` patterns,
  not `Render`, so it does not authorize this change.)

## 9. Exit criteria (plan §6.6, plus this plan)

- [ ] Tests T1–T16 are green, and every L1 structural lock still passes.
- [ ] Each coverage mutant (T5/T6) fails construction.
- [ ] FR-LC-009 conformance (T10) is shown through the real renderer.
- [ ] ERR-049-005 is RESOLVED with executable evidence (T5 static-key mutant + T7), and the spec,
  section-7 and error log are updated in the same commit.
- [ ] If approved, ERR-049-006 is filed and RESOLVED in the same commit, with T13 as evidence.
- [ ] No file or Unity dependency is introduced; `localization.asmdef` references remain empty.
- [ ] `bash tools/run-tests-local.sh --pr` passes with no new failures.
- [ ] Tracking surfaces are current, and the doc-consistency check passes.
- [ ] Pinned Unity compile passes on the PR head before merge.

L2 does not unblock captions on its own: audio D49 still needs the approved #49/#51 caption boundary.
It does unblock the P5b copy/scale/screens slice, which can then inject a real `ILocalizer`.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 0.1 | October 6, 2026 | Initial draft against `ce2e2a36`. Records the owner's ERR-049-005 choice (construction coverage plus a `string.Empty` terminal for never-admitted keys), the L2 type and test plan, and five open choices with recommendations. |
| 0.2 | October 6, 2026 | Review corrections: translated variant indices may be sparse within the base range (T3 uses base `0,1,2`, selected `0,2`); Q5 becomes proposed ERR-049-006 with spec back-prop and test T13, pending owner approval; handoff obligations assign P5b to admit its exact client consumers in the L1 tripwire and #20 record while keeping the sim ban; thread-safety qualified on the plural-rule purity contract; S0's separate content proof stated as still mandatory. |
