# System XI — Localization #49 L2 Implementation Plan

**Created:** October 6, 2026\
**Last Updated:** October 7, 2026\
**Version:** 0.7\
**Status:** IMPLEMENTED — Q1–Q4 recommendations selected under the owner’s October 7 work instruction; Linux PR gate and exact-head pinned Unity compile pending before merge\
**Purpose:** plan the #49 L2 slice (immutable in-memory catalogue, template expander and the production `ILocalizer`), and the ERR-049-005 discharge that ships with it.\
**Baseline:** `main` at `ce2e2a36152590e623602ec633ce568fdfe8b04d` (PR #482 merge). L1 core landed at `f4e8bed4648e5b3e7b7c1437d3065472981fb288`.
**Implementation base:** current `main` `0381b2bbc909ac8d51b4069a0cf3e80e7eb0433d` (PR #486 merge).

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
- **Defect record:** `spec-error-log.md` ERR-049-005 and ERR-049-006 (RESOLVED in this L2 changeset).

The spec wins over this plan wherever they differ. L2 types and members below are implemented in this changeset.
Compilation/test evidence: Roslyn C# 9 against netstandard2.1; NUnitLite 3.14 executes the compiled
Localization test assembly: 67 passed / 0 failed / 0 skipped (18 L1 + 49 L2 cases). Five distinct-English
selection regressions failed before the canonical-base guard; null/same-base selection passes. Removing each
static/template/clause coverage check and adding recursive expansion all fail negative controls.
The environment’s .NET CLI/VSTest fail before execution, so this is targeted evidence, not a canonical
PR-gate pass. All localization test sources also compile against NUnit 3.5; that API compatibility
check does not substitute for the Unity Editor. Pinned Unity 6000.4.9f1 remains unavailable here and required on the exact PR head.

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
| Living-world expansion | `InteractionTextGenerator.Expand` chains `.Replace("{subject}")`, `.Replace("{opponent}")`, `.Replace("{score}")`. | Base-locale identity reference for §5.4 and §4.2 (ERR-049-006). |

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

With §8 Q1 selected, the same terminal rule also covers `Render` of a non-admitted template id and a
clause missing from both catalogues. Those are recorded in the same back-prop, under F1/F2, as
defensive behaviour.

### 4.2 ERR-049-006 (owner-approved, to be filed) — `Render` expansion cannot be identical to chained `.Replace`

**Defect.** KD-3, FR-LC-009 and §3.5 require base-locale expansion to be identical to
`InteractionTextGenerator`'s chained `.Replace` (subject, then opponent, then score), and FR-LC-016
and §3.6 rely on that identity. A generic expander cannot meet it in general, for two reasons:

- **Re-expansion.** Chained `.Replace` re-scans substituted values. A subject named `{opponent}`
  becomes the opponent's name. That is order-dependent behaviour on player-supplied data.
- **Order.** The result depends on the replacement order. `NamedSlotSet` sorts slot names ordinally
  (opponent, score, subject), and the producer-agnostic core has no producer order to follow. Copying
  living-world's order into the core would break FR-LC-012/KD-6.
- **Tokens formed across boundaries.** Chained replacement can create a token that exists in neither
  the template nor any single value. Template `{subject}{opponent}` with subject `{`, opponent `score}`
  and score `2-1` gives `2-1` when chained but `{score}` in a single pass. Template braces can do the
  same: `{{subject}}` with subject `score` gives `2-1` chained and `{score}` single-pass, even though
  every value is brace-free.

**Sufficient identity guarantee.** Single-pass and chained `.Replace` (in any order) produce the same
string when both of these hold:

1. every slot value is brace-free (contains no `{` or `}`); and
2. every brace in the template belongs to a well-formed token: `{`, then one or more characters that
   are not `{` or `}`, then `}`. L2 enforces this at catalogue construction (§5.2).

Under these two conditions the only braces left during chained replacement are those of not-yet-replaced
original tokens, so no new token can form. Neither condition alone is sufficient, as the two examples
above show.

**Current evidence.** Template rows do not constrain runtime names, so the corpus alone proves nothing
about values. The verified L3A oracle slot values are brace-free (subject `Kade Moreno`, opponent
`Halden Rovers`, and scores formatted from integers with `InvariantCulture`), and the current corpus
templates contain braces only in `{subject}`, `{opponent}` and `{score}` tokens. A real player or club
name containing a brace is exactly the case where behaviour deliberately differs.

**Resolution (owner-approved October 6, 2026; it changes approved spec text):** generic
`Expand` performs single-pass, non-recursive substitution. Each `{name}` token in the template is
replaced once, and substituted values are never re-scanned. Base-locale identity with today's output
is required only under the two conditions of the sufficient identity guarantee above.

**Back-prop in the implementing commit:** KD-3 (`section-1.md`), FR-LC-009 and FR-LC-016
(`section-2.md`), §3.5/§3.6 (`section-3.md`) and the tests in `section-5.md` (T-LC-IDENTITY-001 and
T-LC-TEMPLATE-002, which currently require unconditional `.Replace` identity) gain the single-pass rule
and the two identity conditions.
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
- A selected catalogue tagged `BaseLocale` must be the actual base instance. A distinct instance
  fails construction, including an identical copy or an empty catalogue. Null selection still means
  canonical base-only rendering; non-base translations retain sparse fallback.
- **Coverage (F5 + ERR-049-005):** every required static key, template id (at least one variant;
  a missing row is tested, not only an explicit empty one) and clause key must exist in the base
  catalogue.
- **Base variant indices:** for each id, the base catalogue's indices must be exactly `0..n-1`
  with no gap. That defines `n = variantCount(BaseLocale, Id)`.
- **Selected catalogue (FR-LC-008):** translated indices may be sparse, but each one must lie in
  `[0, n)`. A missing translated index falls back to the base variant at that index. An index at or
  above `n` is rejected, because it could never render. See §8 Q4 on orphans.
- **Templates:** placeholders are parsed once at construction. A token is `{`, one or more characters
  that are not `{` or `}`, then `}`. Any other brace (unmatched, nested such as `{{x}}`, or empty `{}`)
  is rejected; L2 has no brace escape. This is condition 2 of the §4.2 identity guarantee. A slot whose
  name contains a brace (L1 allows it) simply cannot be referenced. Static rows are opaque and are
  never parsed (§5.3).
- **Selectors:** the base English catalogue must declare no selector of any kind. `Localizer`
  construction rejects a base variant with a plural **or** a gender selector (KD-3 and FR-LC-009:
  base English declares no categories, which is what keeps base rendering identical to `.Replace`
  under FR-LC-016). Only selected-locale variants may declare a selector, and a selected catalogue
  that declares a plural selector must supply a plural rule.

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
   (§8 Q1 selected).
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
   If found, append `" " + clause`. If absent from both, append nothing (§8 Q1 selected).

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
| T13 | Single-pass expansion: (a) a subject named `{opponent}` stays literal; (b) template `{subject}{opponent}` with subject `{`, opponent `score}`, score `2-1` gives `{score}`, not `2-1`; (c) with brace-free values and well-formed templates, output equals chained `.Replace` in both living-world order and sorted order; (d) an unknown placeholder stays verbatim | §5.4 step 5, ERR-049-006 |
| T14 | Construction rejects: wrong base locale, distinct selected base-locale catalogue (empty, identical copy, sparse plain override, plural override or gender override; null/same-instance selections remain valid), duplicate rows, a gap in base indices, a selected index at or above the base count, malformed template braces (unmatched, nested `{{subject}}`, empty `{}`), a base variant declaring a plural selector, a base variant declaring a gender selector, a selected plural selector without a rule | §5.2 |
| T15 | Caller arrays mutated after construction do not change output | Immutability |
| T16 | Non-admitted template id → `""`; required clause absent from both is impossible by construction; an unrequired missing clause appends nothing | §8 Q1 defensive paths |
| — | Existing L1 locks keep passing: no references, public type shape, no mutable static, no forbidden state, L1-only tripwire | Layer and state constraints |

## 7. Implementation order and landing set

1. Branch from current `main`. Re-check that the ERR-049-005 entry text is still as audited.
2. Add `PluralCategory`, `TemplateForm`, `TemplateVariant`, `CatalogueCoverage`, then
   `TemplateCatalogue` with its construction validation, plus tests T5, T6, T14 and T15.
3. Add `TemplateExpander` and `Localizer`, plus tests T1–T4, T7–T13 and T16.
4. Make both spec back-props and error-log updates in the same commit as the code: ERR-049-005 resolved, and ERR-049-006 filed and resolved (owner-approved, §4.2).
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

## 8. Implementation choices (Q1–Q4 selected October 7, 2026)

- **Q1 — selected: terminal rule applies to `Render`.** A never-admitted template id returns
  `string.Empty` before modulo. A clause absent from both catalogues appends nothing. Construction
  coverage prevents either terminal path for required identities.
- **Q2 — selected: forms in the data model.** Each variant declares at most one plural or gender
  selector, keyed forms and one default form. No inline grammar or brace escape is added; combined
  plural/gender selection remains deferred.
- **Q3 — selected: optional caller-supplied rule per catalogue.** `Func<long, PluralCategory>` is
  required only for catalogues declaring plural selectors. It must be pure and total; concurrent-read
  safety depends on that caller contract. Undefined returned categories use the default form. Real
  CLDR tables remain Wave-8 work.
- **Q4 — selected: reject selected-locale orphans.** Static keys, template ids and clause keys absent
  from base fail `Localizer` construction; translated variant indices outside the base range also fail.
- **Q5. Single-pass expansion versus chained `.Replace`.** Decided as ERR-049-006 (§4.2), approved by
  the owner on October 6, 2026: single pass, with the approved spec text corrected in the implementing
  commit and tested by T13. (v0.1 cited the S0
  non-recursive rule as its authority. That rule governs the client formatter over `Resolve` patterns,
  not `Render`, so it does not authorize this change.)

## 9. Exit criteria (plan §6.6, plus this plan)

- [x] Tests T1–T16 are green, and every L1 structural lock still passes.
- [x] Each coverage mutant (T5/T6) fails construction.
- [x] FR-LC-009 conformance (T10) is shown through the real renderer.
- [x] ERR-049-005 is RESOLVED with executable evidence (T5 static-key mutant + T7), and the spec,
  section-7 and error log are updated in the same commit.
- [x] ERR-049-006 is filed and RESOLVED in the same commit, with T13 as evidence.
- [x] No file or Unity dependency is introduced; `localization.asmdef` references remain empty.
- [ ] `bash tools/run-tests-local.sh --pr` passes with no new failures.
- [x] Tracking surfaces are current, and the doc-consistency check passes.
- [ ] Pinned Unity compile passes on the PR head before merge.

L2 does not unblock captions on its own: audio D49 still needs the approved #49/#51 caption boundary.
After the remaining merge gates pass and L2 lands, P5b copy/scale/screens can inject the production `ILocalizer`.

## Validation evidence

The canonical GitHub PR gate passed on prior head `4b74ec7982d41b01c19fab42d0521e0c987f089d`
([run 37713997728](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/37713997728)): the standard
runner executed Localization 60/60 and the full gate ended `Gate PASSED`, quarantine empty. The
Codex base-locale fix changes production after that head; fresh final-head CI remains required.

At initial implementation, canonical `bash tools/run-tests-local.sh --pr` was attempted locally:
coverage names `TacticalDirector.Localization`; checklist/schema surveys and approval-transition
selection pass; metadata and generated-project checks pass; `dotnet restore` then aborts in
`Process.GetStat` / `Process.StartTime` before compilation/test execution. Canonical gate status is
**BLOCKED by the authoring environment**, not passed. Fresh GitHub Actions evidence for the corrected final head remains due.

Broader tooling: 343 tests, 340 passed / 1 failed / 2 skipped. The failure is
`test_snapshot_checkout_disables_lfs_process_and_smudge_even_when_required` (Git exit 128,
"the remote end hung up unexpectedly"). It reproduces in a detached baseline checkout at
`0381b2bbc909ac8d51b4069a0cf3e80e7eb0433d`; tooling/hook code is unchanged by L2. No test was
excluded or altered. Documentation consistency, assembly tiers, whitespace and metadata checks pass.

The 49 L2 cases live in `LocalizationRenderingTests.cs` and `LocalizationCatalogueTests.cs`;
parameterized cases expand T1–T16 without adding another production dependency. The unchanged
`LocalizationCoreContractTests` supplies the 18 L1 locks. The pre-merge host procedure is:
check out the final PR head, record its full SHA and a clean source tree, compile both Localization
asmdefs in Unity 6000.4.9f1 on the pinned host, and run Localization EditMode tests. Record the host,
Editor version, result counts and log/evidence paths against that exact SHA before merge.

Codex review reproduction: five distinct selected-English catalogue cases were accepted before the
fix (62 passed / 5 failed); after the guard, all 67 cases pass. Removing that guard again fails
the five rejection cases; the earlier static/template/clause/recursive controls still fail 2/3/3/1
cases respectively, and restored production passes 67/67. The supplementary host still cannot
start the normal dotnet CLI (`Process.GetStat`); direct Roslyn/NUnitLite supplies the narrow proof.
The standard prior-head CI pass is separate evidence and is not carried forward as a new-head pass.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 0.1 | October 6, 2026 | Initial draft against `ce2e2a36`. Records the owner's ERR-049-005 choice (construction coverage plus a `string.Empty` terminal for never-admitted keys), the L2 type and test plan, and five open choices with recommendations. |
| 0.2 | October 6, 2026 | Review corrections: translated variant indices may be sparse within the base range (T3 uses base `0,1,2`, selected `0,2`); Q5 becomes proposed ERR-049-006 with spec back-prop and test T13, pending owner approval; handoff obligations assign P5b to admit its exact client consumers in the L1 tripwire and #20 record while keeping the sim ban; thread-safety qualified on the plural-rule purity contract; S0's separate content proof stated as still mandatory. |
| 0.3 | October 6, 2026 | Second review: the identity guarantee becomes brace-free slot values plus well-formed template tokens (enforced at construction), with counterexamples for token formation across substitutions and from template braces; T13/T14 cover them; evidence is stated as verified oracle slot values rather than corpus rows; `section-5.md` joins the ERR-049-006 back-prop list. Owner approval of ERR-049-006 still pending. |
| 0.4 | October 6, 2026 | Records the owner's approval of ERR-049-006 (single-pass expansion with the two-condition identity guarantee). The ERR is still filed, and the spec back-prop made, only in the implementing commit. Status moves to plan for review. |
| 0.5 | October 6, 2026 | PR #483 Codex review: the base catalogue now rejects every selector kind at construction (a gender selector was previously accepted, contradicting KD-3/FR-LC-009 and weakening FR-LC-016 identity); T14 covers base plural and base gender selectors. |
| 0.6 | October 7, 2026 | Q1–Q4 recommendations selected and L2 implemented; T1–T16/structural evidence and negative controls recorded; both ERRs back-propagated together. Canonical PR and pinned Unity gates remain open. |
| 0.7 | October 7, 2026 | Codex review: selected English content must be the canonical base instance; five rejection and two identity cases bring localization to 67 local passes. Records prior-head standard CI 60/60 and full gate pass, with corrected-head CI and pinned Unity still due. |
