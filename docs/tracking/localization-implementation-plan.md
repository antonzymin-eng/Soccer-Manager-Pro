# Localization #49 — End-to-End Implementation Plan

**Created:** September 6, 2026
**Version:** 2.0
**Status:** READY FOR IMPLEMENTATION
**Baseline:** `main` at `4c07393f00111f2c1b5f79625de4920df95ed98e`
**Scope:** the APPROVED #49 seam/T0+T1 implementation first; Wave-8 locale/a11y content remains a later, separately approved tier.

---

## 0. Posture

This revision supersedes the v1 planning branch. It incorporates the external review against current repository rules and removes three invalid assumptions from v1:

1. an assembly-less APPROVED spec may **not** be hardened ahead of its T0 landing (root `CLAUDE.md`; path-to-playable C6);
2. a new production assembly must be seated in Code Standards §3.5.2 in the **same commit** that adds its `.asmdef` (`assembly-tier-check.py` enforces both directions);
3. Wave-8 locale-content requirements are not prerequisites for the already-approved seam and are explicitly deferred past PM-3 by the live path-to-playable roadmap.

Starting #49 seam work now is therefore an **owner parallel-workstream choice**, justified by the repo's C5 principle that first execution exposes specification defects. It is not on the PM-2 critical path and is not described here as a roadmap requirement.

The provisional `codex/localization-infrastructure-t0` branch remains non-authoritative and must not be merged.

---

# 1. Review adjudication

| Finding | Disposition | Revision |
|---|---|---|
| H1 — v1 Phase 0 hardened an assembly-less #49 before T0 | **Accepted** | Pre-T0 work is limited to **recording** defects and capturing the existing-English oracle. Spec corrections are discharged in the T0/T1 implementation commits they govern. |
| H2 — architecture/tier admission cannot precede the `.asmdef` | **Accepted** | Delete separate L-P0B. `localization` seating lands in L1 with `localization.asmdef`; `localization-boundary` seating lands in L3B with its `.asmdef`. |
| H3 — BCP 47/NFC/JSON/schema/completeness requirements were smuggled into seam T0 | **Accepted** | Remove them from L1/L2. T0 uses only the approved in-memory seam/catalogue/template/fallback contracts. External locale serialization and release-completeness tooling stay Wave 8. |
| M1 — §2.2 still places `ForInteraction` in the core | **Stale on current `main`** | Current `section-2.md` correctly puts `ForInteraction` under `LivingWorldTextBoundary`. The live authoritative defect is `section-1.md` KD-6, which still says `Localization -> living-world` and cites `TextTemplateId.ForInteraction`. Historical review text in §9 is not rewritten merely because it describes the pre-fix state. |
| M2 — no ERR ids | **Accepted** | Record two #49 defects before implementation, proposed as ERR-049-002/003 after re-verifying the ids are free at filing. Discharge them atomically with their implementing commits. |
| M3 — ProducerTag back-prop inventory/pair uniqueness missing | **Accepted** | Inventory every symbolic `ProducerTag.*` consumer before T1 and lock uniqueness of the **pair** `(ProducerTag, LocalOrdinal)`, not only tag values. |
| M4 — external JSON had no CI-path loader | **Accepted; design removed** | No external locale file is part of seam T0/T1. Base English remains compiled host-free content so the Linux gate exercises the exact shipped T0 content. External content files/loaders are a Wave-8 decision. |
| Roadmap framing | **Accepted** | #49 content is explicitly deferred past PM-3. Seam T0 is parallel work, not critical-path work. |

---

# 2. Settled high-level plan

## H1 — Record defects; do not harden before T0

Before implementation, record known #49 defects without correcting their contract text:

- authoritative §1 KD-6 still contradicts §2/§3/§4/§5 by describing a core `Localization -> living-world` reference and `TextTemplateId.ForInteraction` factory;
- the generic `TextTemplateId` has an integer producer tag, while later approved specs assume symbolic append-only `ProducerTag.*` allocations without an owning allocation contract.

Recording is allowed by C6; correction waits for the implementation commit that proves the replacement.

## H2 — Capture the pre-migration English oracle first

Extend the existing living-world test suite to exhaustively lock current English output, variant coverage, clause output and cursor/refusal behavior **before** any localization code changes. This is an observation lock on built code, not hardening of #49.

## H3 — Land #49 T0 as the generic core, with architecture admission in the same commit

Create `TacticalDirector.Localization` and simultaneously:

- seat `src/localization/` in the active assembly-tier authority;
- add any architecture-governance records that are required for a newly existing component at that moment;
- discharge the §1 reference-direction defect in the same commit;
- implement only the APPROVED generic contracts.

No sim assembly references the core.

## H4 — Complete the approved seam renderer/catalogue without Wave-8 scope

Implement the approved in-memory catalogue, base-count variant selection, named-slot expansion, bounded plural/gender selector, fallback and base-locale construction coverage.

Do **not** introduce:

- canonical BCP 47 validation beyond the approved fixed base id `en`;
- JSON/XLIFF/CSV locale files;
- Unicode normalization policy;
- translation-completeness percentages;
- pseudo-locales;
- release-language gates;
- font/script eligibility;
- real translated locales.

Those are content-tier concerns.

## H5 — Land #49 T1 as one living-world proof slice

Create `TacticalDirector.LocalizationBoundary`, seat it in the same commit, resolve the ProducerTag ownership gap, migrate #22's English corpus out of `living-world`, change `InteractionTextGenerator` to native result data, and prove byte identity/determinism against the pre-migration oracle.

## H6 — Onboard later producers only when their code exists

Each later procedural producer adds one sibling adapter + allocated tag + base content + roster coverage. Static UI text uses `LocalizationKey`. No phantom adapter is added for an unbuilt producer.

## H7 — Leave Wave-8 content/a11y expansion deferred

The AR-converged Wave-8 supplement remains useful design input, but it is **not promoted or implemented as a prerequisite** for seam T0/T1. It resumes at the project's later content/release stage unless the owner deliberately reprioritizes it.

---

# 3. Pre-T0 record-only slice — L0R

**Purpose:** make known defects durable without violating C6.

## 3.1 Proposed ERR-049-002 — stale core reference direction

At filing time, re-verify `ERR-049-002` is free.

Record, but do not yet correct:

- `docs/specs/localization-accessibility/section-1.md` KD-6 says the renderer references built producers and explicitly states `Localization -> living-world`;
- the same paragraph cites `TextTemplateId.ForInteraction(intent)`;
- authoritative §2.2/§2.2.1, §3, §4.1, §5 and §9 instead require a producer-agnostic core and a sibling `LivingWorldTextBoundary`.

The eventual fix belongs in **L1/L3B**, whichever first introduces the executable boundary needed to prove the corrected wording. Prefer L1 for the pure reference-direction sentence and L3B for concrete `LivingWorldTextBoundary` wording if splitting produces cleaner evidence.

Do not rewrite historical review records simply because they describe the pre-fix design. Search each occurrence and classify it as live contract vs historical record.

## 3.2 Proposed ERR-049-003 — ProducerTag allocation ownership

At filing time, re-verify `ERR-049-003` is free.

Record:

- #49 defines `TextTemplateId.ProducerTag` as `int` but no allocation owner/table/append-only rule;
- later specs use symbolic `ProducerTag.Media`, `.Inbox`, `.MatchCommentary`, etc.;
- catalogue identity is the **pair** `(ProducerTag, LocalOrdinal)`, so collision prevention must cover the pair and tag allocation stability.

The record must inventory all current downstream symbolic uses. Minimum current-main search includes:

- #35 media/press section 4 + appendices;
- #46 news/inbox section 4 + appendices + boundary tests;
- #48 match-presentation section 4;
- any #39/#50/#51 occurrences found by repository-wide search at filing time.

Do not choose/fix the allocation mechanism in this record-only slice. T1 execution decides it against real code.

## 3.3 L0R exit

- findings present in the normal #49 §7/deferred-finding location and `docs/tracking/spec-error-log.md`;
- spec version-history rows updated because the spec file is modified;
- no functional FR wording is hardened;
- no production assembly or API is created;
- ordinary document consistency gates green.

---

# 4. Oracle slice — L3A (runs before T0)

**Purpose:** establish an implementation-independent acceptance oracle for FR-LC-016.

Extend `src/living-world/Tests/WorldTextSnapshotTests.cs` or a focused sibling fixture. Existing tests already cover same-seed determinism, expansion, one eligible citation, refusal/no-cursor, None/out-of-roster, malformed slots, one-draw advancement, and sibling-stream independence. L3A adds exhaustive coverage missing from that suite.

## 4.1 Exact output coverage

Lock exact UTF-8 strings for:

- every defined non-None `InteractionIntent`;
- every current template variant at least once (choose deterministic seeds/cursors/draws that hit each index);
- every defined citable `EventKind` clause;
- cited and uncited rendering;
- representative score formatting including 0-0 and multi-digit values if valid;
- punctuation and whitespace, including the single-space clause concatenation.

## 4.2 Selection/count coverage

For every intent:

- record/derive the current template count;
- prove the chosen calls visit all variants;
- lock the count separately from the strings so a duplicated/deleted row cannot hide behind an unchanged sampled output.

## 4.3 Refusal/cursor oracle

For each relevant pre-draw refusal class:

- None/undefined intent;
- default/malformed slots;
- below-threshold citation;
- NaN citation salience;
- invalid citation kind;

assert unchanged `RngCursor` and `ActionOrdinal`.

For successful generation assert exactly one advancement.

## 4.4 Non-vacuity

At least one mutation/probe must demonstrate the oracle fails when:

- a template row changes;
- a row is reordered/deleted; or
- clause punctuation/spacing changes.

## 4.5 L3A exit

- no #49 production code/spec hardening;
- existing `living-world` assembly/tests only;
- golden oracle green on current code and proven non-vacuous.

---

# 5. T0 core contracts — L1

**Purpose:** create the APPROVED `TacticalDirector.Localization` seam with no producer coupling.

## 5.1 Same-commit architecture rule

The L1 commit contains all mechanically inseparable changes:

1. `src/localization/localization.asmdef`;
2. Code Standards §3.5.2 seating for `localization` in the currently authoritative tier;
3. any active architecture-governance inventory/property/integration records that become mandatory only because the component now exists;
4. the relevant ERR-049-002 contract correction that is now executable/provable;
5. localization core code/tests.

Never land the tier row without the asmdef or vice versa.

Re-read the active A3/A4 architecture state immediately before implementation; this plan does not freeze registry mechanics that may change before L1 starts.

## 5.2 Core assembly contract

`TacticalDirector.Localization` references **no sim/producer assembly**.

Implement the minimum approved public surface:

- `ILocalizer` — exactly `Resolve(LocalizationKey)` and `Render(in LocalizedTextRequest)` as rendered-string entry points;
- `LocalizationKey` — stable static-string identity;
- `TextTemplateId` — generic `(int ProducerTag, int LocalOrdinal)`;
- `NamedSlotSet` — immutable name -> already-formatted string values;
- `LocalizedTextRequest` — id, `ulong SelectionDraw`, slots, citation flag/key;
- minimal `LocaleId`/base-locale representation needed to distinguish base vs selected locale, with **no** new BCP-47 conformance requirement beyond `BaseLocale = "en"`.

No `InteractionIntent`, `EventKind`, `ProducerTag.Media`, Unity types, file I/O, client-settings store, RNG or save state.

## 5.3 Value-safety tests

Lock:

- default/invalid identities do not alias valid ids;
- value equality/hash behavior is deterministic;
- `SelectionDraw` remains `ulong` end-to-end;
- duplicate named slot names are refused;
- slot storage cannot be mutated through retained caller references;
- citation namespace includes `ProducerTag`;
- no public baked-string pass-through API exists.

## 5.4 Layer tests

Mechanically assert:

- core asmdef has no producer/sim references;
- no sim/loop assembly references `TacticalDirector.Localization`;
- type-shape contains no sim-owned type.

## 5.5 L1 exit

- assembly-tier check green;
- active architecture-governance checks green;
- localization unit tests green;
- full Linux generated-project compile/test gate green;
- ERR-049-002 portion discharged with executable evidence.

---

# 6. Approved renderer and in-memory catalogue — L2

**Purpose:** implement only FR-LC-007/008/008a/009/010/011 and the renderer behavior required for T1.

## 6.1 In-memory model only

Implement host-free immutable structures for:

- selected locale + base locale;
- static string rows;
- ordered template variants keyed by `TextTemplateId`;
- producer-scoped clause rows;
- required base-surface descriptors sufficient to enforce FR-LC-008a without embedding producer enums in the core.

No external serialized locale format is defined here.

## 6.2 Renderer

`Resolve`:

1. selected-locale row if present;
2. base-locale row otherwise;
3. dev marker only if the APPROVED fallback contract permits it and base is absent in a dev-only malformed fixture.

`Render`:

1. obtain `variantCount(BaseLocale, Id)`;
2. require count >= 1 by construction;
3. compute `SelectionDraw % (ulong)count` before narrowing;
4. lookup the selected locale at the **same** variant index;
5. fall back to the base row for that exact index;
6. pure named-slot expansion;
7. optional clause lookup by `(Id.ProducerTag, CitationKind)` with selected -> base fallback;
8. append one space + clause exactly as the oracle expects.

Renderer performs no RNG draw, tick advance, persistence write or producer-specific formatting.

## 6.3 Template model

Implement the already-approved bound only:

- named string substitution;
- bounded plural categories (`one/few/many/other`) + small gender selector where authored;
- base English requires no grammatical selector;
- no case-declension/agreement engine;
- no locale-specific number/date/currency formatter in T0.

Use synthetic test catalogues for selector behavior. Real translated content is not required.

## 6.4 Construction coverage

Because the core cannot enumerate producer enums, catalogue construction accepts generic required identities/clauses from its caller/boundary and fails if the base catalogue omits any required row or has zero variants.

Tests must prove a **missing row**, not only an explicit zero row, fails construction.

## 6.5 No Wave-8 leakage

Explicitly absent from L2:

- JSON or other canonical locale file schema;
- external asset loader;
- BCP-47 validation/normalization beyond the fixed base id;
- Unicode NFC/NFD policy;
- ASCII key grammar not already required by the approved seam;
- pseudo-localization;
- coverage percentages/offered-locale logic;
- font/glyph checks;
- translation vendor interchange.

## 6.6 L2 exit

- base and synthetic partial-locale fallback tests green;
- missing-base coverage mutant killed;
- modulo/variant-order tests green;
- no file/Unity dependency introduced;
- full Linux gate green.

---

# 7. T1 living-world retrofit + boundary — L3B

**Purpose:** fulfill FR-LC-016 using the oracle, while resolving the remaining ProducerTag gap against real code.

## 7.1 Same-commit boundary architecture

The L3B commit contains together:

1. `src/localization-boundary/localization-boundary.asmdef`;
2. Code Standards §3.5.2 seating for `localization-boundary`;
3. required architecture-governance records for the now-existing component;
4. ProducerTag allocation implementation + ERR-049-003 spec/back-props;
5. `LivingWorldTextBoundary` + base content/requirements;
6. living-world producer API retrofit;
7. corpus migration/removal;
8. identity/determinism tests.

## 7.2 ProducerTag resolution principles

Choose the exact code shape only now, with real callers, but enforce these invariants:

- generic core remains `int ProducerTag`; adding a producer never changes `ILocalizer`, `TextTemplateId`, `LocalizedTextRequest` or renderer logic;
- symbolic allocations live outside the generic seam surface (boundary/composition ownership is the default candidate);
- `0` is reserved/invalid if zero-value safety requires it;
- allocations are append-only, never reused/renumbered;
- the first built procedural producer receives the first real allocation;
- no allocation is created for an unbuilt producer;
- uniqueness is proven for tags **and for every `(ProducerTag, LocalOrdinal)` catalogue identity**.

Back-propagate every approved downstream example whose assumed `ProducerTag` location/API is made false by the implementation. Do not update only the examples named in the original review; use a fresh repository-wide search at L3B.

## 7.3 Compiled base content, not external locale files

For seam T1, migrate the exact English templates/clauses into host-free compiled #49-owned content on the localization/boundary side.

Requirements:

- no editorial rewrites;
- exact row order and variant counts preserved;
- content exercised directly by Linux tests — no duplicate embedded test fixture standing in for an untested shipped JSON asset;
- `living-world` no longer owns human-readable template/citation strings after migration.

The precise class/file placement may be chosen at T1 so long as the generic core gains no producer-type dependency and the content remains #49-owned, not sim-owned.

External data files are a later Wave-8 migration if still desirable.

## 7.4 `LivingWorldTextBoundary`

Map native #22 values to the generic request:

- `InteractionIntent` -> `(ProducerTag, LocalOrdinal)`;
- raw `ulong world.text` selection value copied verbatim;
- `SubjectName` -> `subject`;
- `OpponentName` -> `opponent`;
- invariant score string derived in the boundary, not the generic expander;
- citation presence + `EventKind` ordinal -> generic producer-scoped clause key.

The adapter references both assemblies; neither side gains the reverse dependency.

## 7.5 Producer retrofit

Refactor `InteractionTextGenerator` so it emits living-world-owned native result data rather than a final string.

Preserve sim-side before the draw:

- defined/non-None intent gate;
- slot validation;
- salience/citation validation.

Preserve selection behavior:

- exactly one successful `world.text` reservation/draw;
- no draw on any refusal;
- same cursor/action-ordinal advancement.

The producer must not construct #49 types and must not reference localization.

## 7.6 Oracle acceptance

Against L3A, prove:

- every exact English string is byte-identical;
- every intent's variant count unchanged;
- every clause unchanged;
- row ordering unchanged;
- score formatting unchanged;
- refused calls consume zero cursor/action;
- successful calls consume exactly one;
- sibling streams unchanged;
- localization rendering itself is RNG/tick/state neutral;
- relevant world/season serialization remains locale-independent.

## 7.7 L3B exit

- oracle fully green;
- layer/tier/governance checks green;
- no `InteractionTextCorpus` human-readable ownership remains sim-side unless a documented non-user-facing residual is proven necessary;
- ERR-049-003 discharged and downstream spec back-props consistent;
- full Linux gate green.

---

# 8. Later producer onboarding — L4+

This work is continuous, not a prebuilt batch.

For each **built** procedural producer:

1. verify its native identity/fact/selection contract exists;
2. allocate the next tag without changing prior allocations;
3. add a sibling boundary adapter;
4. add required-surface enumeration from that producer's defined roster;
5. add exact base-English content;
6. prove `(tag, ordinal)` uniqueness;
7. add base coverage/fallback/selection tests;
8. prove the producer has no reverse localization reference.

For static UI strings:

- use stable `LocalizationKey`s;
- do not invent keys for pure data values such as proper names/numbers;
- avoid UI-side sentence concatenation;
- add base rows and route through `Resolve`.

A future producer is never pre-allocated merely because its spec exists.

---

# 9. Wave-8 content/a11y tier — deferred lane

The AR-converged `localization-content-a11y-design.md` is **not** promoted as part of L0R-L4 unless the owner explicitly reprioritizes it.

When the project reaches the content/release stage, run its documented promotion pipeline first. Only after approval may that lane add requirements such as:

- canonical non-base locale identifiers/policy;
- external locale content format and import/export tooling;
- Unicode normalization policy;
- pseudo-locale;
- completeness measurement;
- offered-locale release gate;
- font/script coverage;
- actual a11y option catalogue/application back-props;
- real translated locales and linguistic QA.

At that stage, revisit whether compiled T0 base content should migrate to repository data files. Do not pre-commit to JSON/XLIFF/vendor formats during seam T0.

---

# 10. Parallelism and project priority

Localization T0/T1 may run in parallel with the main backend/UI/architecture work because its first slices are isolated and host-free, but it must not distort the live PM-2 critical path.

Safe parallel work:

- L0R record-only findings;
- L3A oracle;
- L1/L2 once the active architecture authority is re-read;
- L3B after L1/L2.

Coordination rules:

- if A3/A4 changes §3.5.2 or architecture registry mechanics before L1/L3B, rebase/re-read and use the new authority rather than preserving this plan's assumptions;
- do not create a separate architecture-admission PR that names an assembly before its asmdef exists;
- UI work may continue without waiting for #49, but once L1/L2 are available, new user-facing copy should route through the seam rather than accumulate new baked-string debt;
- no Wave-8 translation/content work is pulled forward solely to keep the localization workstream busy.

---

# 11. Mergeable slice order

| Order | Slice | Scope | Dependency | Key exit |
|---:|---|---|---|---|
| 0 | **L0R** | record ERR-049-002/003 only | none | C6-compliant durable findings |
| 1 | **L3A** | exhaustive current-English/cursor oracle | built #22 | non-vacuous golden lock |
| 2 | **L1** | T0 core contracts + same-commit tier/governance admission + reference-direction discharge | L0R | compile/layer gates |
| 3 | **L2** | approved in-memory catalogue/renderer/template/fallback only | L1 | seam behavior gates |
| 4 | **L3B** | T1 boundary + same-commit seating + ProducerTag discharge + #22 retrofit + compiled base content | L2 + L3A | byte identity + determinism |
| 5+ | **L4+** | one later built producer/static UI surface at a time | producer exists | coverage/layer gates |
| later | **Wave 8** | promote content/a11y extension, then implement locale production/release tooling | owner priority + later stage | separate approval |

The critical seam implementation sequence is therefore:

**L3A oracle -> L1 T0 core -> L2 approved renderer/catalogue -> L3B T1 retrofit**.

L0R can precede or run alongside L3A because it records findings only.

---

# 12. Verification on every runtime slice

Run the narrow suite first, then active repository gates.

Minimum:

- localization tests for L1/L2/L3B;
- living-world oracle/producer tests for L3A/L3B;
- `python3 tools/assembly-tier-check.py --repo .` when any production asmdef/tier surface changes;
- current document consistency / recurring-defect / governance checks required by repo instructions;
- full Linux generated-project compile + tests;
- `git diff --check`/equivalent hygiene.

Do not claim Unity-host rendering/font/layout certification from the Linux shim. No Unity-host behavior is introduced in T0/T1 anyway.

Mutation/non-vacuity requirements:

- L3A exact-output/count oracle must fail on a template/row mutation;
- L1 layer test must fail on a reverse sim reference;
- L2 construction-coverage test must fail when an entire required base row is omitted;
- L3B pair-uniqueness test must fail on a duplicate `(ProducerTag, LocalOrdinal)`;
- L3B oracle must fail on row reorder/string/spacing changes.

---

# 13. Stop conditions

Stop and revise rather than silently broadening the seam if implementation discovers any of the following:

- the core needs a sim-owned enum/type;
- base-English identity cannot be reproduced without changing deterministic draw behavior;
- the proposed ProducerTag ownership requires core API edits for each future producer;
- FR-LC-008a cannot be enforced generically without a new cross-assembly contract not justified by current §2/§4;
- active architecture governance makes the proposed two-assembly topology inadmissible;
- a proposed T0 feature exists only in the unpromoted Wave-8 supplement.

Any such finding is recorded as an ERR and discharged with the implementation that proves the resolution, following C6.

---

# 14. Implementation-ready exit criteria

The plan is ready for execution when:

1. implementation starts from current `main`, not either stale planning/scaffold branch;
2. L0R verifies the next ERR-049 ids are free before filing;
3. L3A locks the current producer before migration;
4. L1 and L3B each perform assembly seating in the same commit as their asmdef;
5. no Wave-8-only requirement is treated as a seam-T0 prerequisite;
6. each ERR correction lands with executable code/tests proving the corrected contract;
7. every slice remains independently reviewable.

---

# 15. Review history

## v1 internal cycles

The original planning pass established the useful lifecycle split (seam -> producer adoption -> pseudo/translation/release) but incorrectly front-loaded architecture admission, spec hardening and Wave-8 content mechanics.

## v2 external review corrections

Accepted High findings:

- C6 prohibits pre-T0 hardening;
- assembly seating must be atomic with the asmdef;
- serialized locale/BCP47/NFC/completeness work belongs outside approved seam T0.

Accepted Medium findings:

- ERR recording/back-props are mandatory;
- ProducerTag inventory and pair uniqueness need explicit treatment;
- an external content file cannot be introduced without a host-free path proving the actual file.

Current-main correction to the review:

- `section-2.md` already correctly places `ForInteraction` on `LivingWorldTextBoundary`; the live authoritative contradiction is `section-1.md` KD-6. `section-9` occurrences describing the historical pre-fix design are evidence records, not necessarily stale live contracts.

**v2 conclusion:** no remaining known structural High/Medium planning defect. Implementation should begin with L0R/L3A, then L1 -> L2 -> L3B.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 2.0 | 2026-09-06 | — | Rebuilt from current main after external C6/T0 review: removed pre-T0 hardening, atomicized asmdef seating, deferred Wave-8 scope, added ERR recording/back-prop inventory and pair-identity locks, removed external locale-file requirement, and re-sequenced to oracle -> T0 -> renderer -> T1 retrofit. |
#endregion
