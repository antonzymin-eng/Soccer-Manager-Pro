# Localization #49 — End-to-End Implementation Plan

**Created:** September 4, 2026
**Last Updated:** September 4, 2026
**Version:** 1.1
**Status:** READY FOR IMPLEMENTATION PLANNING GATE
**Scope:** Localization infrastructure, producer adoption, localization QA, translation production, and release gating. Accessibility is included only where #49 owns the option/value contract; #38 owns rendering/theme application.
**Primary authorities:** `docs/specs/localization-accessibility/` (#49, APPROVED), `docs/tracking/localization-content-a11y-design.md` (Wave-8 extension, AR-converged but not yet promoted), `docs/tracking/path-to-playable-roadmap.md`, Code Standards #20, and Project Architecture Governance.

---

## 0. Plan posture

This plan replaces the earlier ad-hoc T0 start. No localization implementation should be merged until the Phase 0 architecture/specification gates below are satisfied. A small provisional scaffold exists on `codex/localization-infrastructure-t0`; it is intentionally **not** the implementation baseline and should be discarded/rebuilt from the approved plan rather than merged forward by inertia.

Localization is split deliberately across the project lifecycle:

1. **early infrastructure** — seam, catalogue contracts, fallback, validation, first producer migration;
2. **continuous producer adoption** — every built user-facing producer joins the seam as it lands;
3. **developer QA infrastructure** — pseudo-localization, completeness and layout/font checks;
4. **late content production** — real translations after enough player-facing copy/UI has stabilized;
5. **release gating** — only complete/readable locales are offered and advertised.

The project roadmap already requires this split: #49 has an early seam tier and a later release/content tier. Real translations are therefore **not** a prerequisite for starting localization infrastructure.

---

# 1. Settled high-level plan

## H1 — Governance and contract convergence first

Before runtime code lands:

- reconcile the stale #49 §1 wording that still implies a core `Localization -> living-world` reference with the later approved §2/§4/§9 producer-agnostic core + boundary-adapter split;
- resolve the currently undefined `ProducerTag` allocation mechanism assumed by downstream specs;
- process architecture admission for the new production assemblies through the current Code Standards / Project Architecture Governance rules;
- advance the already AR-converged Wave-8 localization-content/a11y extension through its remaining promotion/sign-off steps before implementing its new requirements.

This lane can run in parallel with the ongoing A3 architecture work, but a runtime assembly merge must not race or casually edit the same architecture registry/tier surfaces.

## H2 — Build the approved generic seam and content model

Implement a host-free, producer-agnostic `TacticalDirector.Localization` assembly with:

- static-key resolution;
- generic procedural text requests;
- immutable locale/catalogue representation;
- named-slot expansion;
- bounded selector support;
- base-locale variant selection;
- current-locale -> base-locale fallback;
- construction-time base coverage validation;
- no sim reference, RNG, persistent sim state, or save-format impact.

The core accepts in-memory catalogue data. It owns **no Unity asset loading and no platform file I/O**.

## H3 — Prove the seam with the existing `living-world` producer

Before changing `InteractionTextGenerator`, capture an exhaustive base-English oracle. Then:

- add a separate `localization-boundary` assembly;
- migrate the producer to native deterministic values rather than baked text;
- migrate the English corpus/clauses into the base locale;
- map `InteractionIntent`/slots/draw into the generic request at the boundary;
- prove byte-identical English output and unchanged determinism/save behavior.

## H4 — Land developer localization QA before real translation

Once the Wave-8 extension is formally approved:

- pseudo-locale;
- placeholder/selector validation;
- per-producer completeness reporting;
- seam-bypass/layout stress checks;
- font/script eligibility input to the offered-locale gate.

These mechanisms land **before** the first paid/real translation.

## H5 — Onboard producers incrementally, never speculatively

Each producer joins only when its production surface exists. Adding one producer adds:

- one append-only producer tag where procedural text needs one;
- one sibling boundary adapter if procedural;
- one roster/requirements descriptor;
- base-locale content;
- coverage and behavior tests.

Static UI copy uses `LocalizationKey` directly and needs no sim-side adapter.

## H6 — Keep accessibility ownership split cleanly

- #49 owns locale/a11y **values/options**;
- #38/client settings own persistence;
- #38 renderer/theme owns text scale, reflow, contrast/colorblind palettes, fonts and glyph fallback;
- #51/audio owns playback while localized caption text still routes through #49.

Localization must not become a theme/rendering authority.

## H7 — Treat translations as a production pipeline, not runtime code

After copy/UI is sufficiently stable:

- select launch/EA locale targets as a product decision;
- create glossary/style guide and translator context;
- export/import translation packages from the canonical source catalogue;
- run linguistic review and in-game LQA;
- keep incomplete locales internal until the ship gate passes.

## H8 — Release claims derive from mechanical gates

A non-base locale is offered only when:

1. translation completeness meets the release threshold; and
2. the shipped font/fallback chain covers its script.

The base locale is always offered. The pseudo-locale is dev-only and never offered. #39 release/storefront claims must be derived from the same offered-locale report.

---

# 2. Phase 0 — Planning, specification and architecture gates

**Goal:** make the first implementation PR mechanically admissible and remove known contract ambiguity.

## 0.1 Freeze/discard provisional implementation scaffold

- Do not merge `codex/localization-infrastructure-t0`.
- Recreate implementation from the eventual Phase-1 branch after Phase 0, rather than preserving early type/API choices solely because they already exist.

## 0.2 Correct #49 seam wording

Reconcile the stale references in `section-1.md` with the approved PASS-1/AR-3 architecture:

- core `TacticalDirector.Localization` references no producer/sim assembly;
- producer-specific mapping lives in `localization-boundary`;
- a producer emits native values only;
- the adapter constructs `TextTemplateId`/`LocalizedTextRequest`.

Run the normal spec consistency review; do not change the load-bearing behavior of FR-LC-001..020/008a except to remove contradictory wording.

## 0.3 Resolve `ProducerTag` as a named architectural gap

Downstream specs already assume symbolic tags such as `ProducerTag.Media`, `ProducerTag.Inbox`, and `ProducerTag.MatchCommentary`, while #49 currently defines only an integer field.

Decision to land:

- the **generic core keeps only `int ProducerTag`** inside `TextTemplateId`;
- the append-only symbolic allocation registry lives in the **boundary/composition layer**, not the core;
- tags are allocated only when a producer is actually built/bound;
- values are never reused or renumbered;
- uniqueness and append-only behavior are mechanically tested;
- adding a producer may extend the boundary registry, but does not change `ILocalizer`, `TextTemplateId`, `LocalizedTextRequest`, or renderer logic.

Back-propagate downstream examples if their assumed type location differs from this settled ownership.

## 0.4 Promote the Wave-8 extension independently

`localization-content-a11y-design.md` is AR-converged but still a design supplement. Complete its own documented pipeline:

1. extend the existing #49 section set with FR-LC-021+;
2. PASS-1/fix/review to convergence;
3. apply the #38 a11y/rendering ownership back-prop (`ERR-038-005`) at approval;
4. obtain required lead-developer sign-off;
5. update the existing #49 registry note, not a new spec number.

**Important:** Phase 1/2 implementation of the already-approved seam does not wait for this approval. Phase 5+ features that rely on FR-LC-021+ do.

## 0.5 Architecture admission

Before creating production asmdefs on the mergeable implementation branch:

- seat `localization` and, when introduced, `localization-boundary` in the active Code Standards §3.5.2 assembly taxonomy (expected Client/top-of-graph placement, subject to the active authority at that time);
- run Project Architecture Governance applicability/property admission against each new durable component;
- add any required integration-contract/property records if the active governance rules classify them as applicable runtime-bearing components;
- prove no sim/loop assembly references localization;
- coordinate the edits with the A3 architecture branch to avoid conflicting parallel rewrites of the same tables/registries.

**Phase-0 exit:** no unresolved High/Medium contract contradiction; architecture gate recognizes the planned assemblies; the first runtime PR will not fail merely because its folder is unclassified.

---

# 3. Phase 1 — Generic localization core contracts

**Goal:** establish the minimum producer-agnostic API surface, with no catalogue content dependency on any sim type.

## 1.1 Assembly

Create `src/localization/` / `TacticalDirector.Localization` with zero sim references.

Expected initial public contracts:

- `ILocalizer`
- `LocalizationKey`
- `LocaleId`
- `TextTemplateId`
- `NamedSlot` / `NamedSlotSet`
- `LocalizedTextRequest`
- generic catalogue/requirement value types needed by Phase 2
- fixed base-locale identity (`en`)

Do **not** put `InteractionIntent`, `EventKind`, media/news enums, `ProducerTag.Media`, Unity types, file-system code, or client settings persistence in this assembly.

## 1.2 Value and locale-identity safety

Lock:

- invalid/default identifiers cannot accidentally alias valid catalogue entries;
- ordinal equality for stable string keys/locale IDs;
- locale IDs use canonical **BCP 47 language tags** (base remains `en`); canonical casing/well-formedness is enforced by content validation rather than by inventing a project-private locale naming scheme;
- no duplicate named slots;
- named-slot storage is defensively copied / externally immutable;
- `SelectionDraw` stays `ulong` end-to-end;
- clauses remain producer-scoped through `TextTemplateId.ProducerTag`.

## 1.3 Seam-shape test

Reflection/type-shape tests prove:

- the only public operations that produce rendered user-facing strings are `Resolve` and `Render`;
- there is no baked-string pass-through API;
- the core assembly references no producer assembly.

**Phase-1 exit:** contracts compile under Linux shim; assembly-tier/governance checks pass; zero producer code changed.

---

# 4. Phase 2 — Catalogue, renderer and canonical content schema

**Goal:** make the seam usable and translation-production-safe before a producer is migrated.

## 2.1 Runtime catalogue model

Implement immutable in-memory structures for:

- static strings keyed by `LocalizationKey`;
- procedural template rows keyed by `TextTemplateId`;
- ordered variants per template ID;
- producer-scoped clause rows;
- optional bounded grammatical forms/selectors;
- locale identity/metadata needed by the renderer.

The base-locale variant count is authoritative. A non-base locale cannot alter selection space.

## 2.2 Generic construction requirements

The core must enforce FR-LC-008a without knowing producer enums.

Use a generic required-surface input assembled above the core:

- required static keys;
- required `TextTemplateId`s;
- required producer-scoped clause keys.

Base catalogue construction fails when a required row is absent, empty, malformed, or has zero variants. Later boundary adapters derive these generic requirements from their own producer rosters.

## 2.3 Renderer behavior

`Resolve`:

- current-locale static entry;
- otherwise exact base entry;
- no crash/state mutation on missing non-base translation.

`Render`:

1. read base-locale variant count;
2. compute `SelectionDraw % (ulong)count` before narrowing;
3. read current-locale variant at the resulting index;
4. fall back to the same base-locale `(Id, variant)`;
5. expand named slots;
6. append the producer-scoped localized clause when present;
7. never draw RNG, tick simulation, or write persistent state.

## 2.4 Template expansion

Implement only the approved bounded model:

- pure named string substitution;
- optional authored plural/gender category selection;
- no arbitrary declension/agreement/morphology engine;
- base English with no selector behaves as pure replacement.

Malformed placeholders/categories fail at authoring/build validation, not as silent runtime corruption.

## 2.5 Canonical serialized content format

Use repository-owned locale data under `Assets/Localization/` as **content**, while keeping parsing/loading outside the core's platform-neutral contract.

Canonical authoring rules:

- files are UTF-8 and strings are canonical **Unicode NFC**; validator rejects non-canonical text rather than silently rewriting committed content;
- `LocalizationKey`/placeholder identifiers use a restricted ASCII semantic-key grammar so identity never depends on Unicode normalization or translator editing;
- `Assets/Localization/en.json` is the authoritative base strings/templates/clauses plus translator context metadata;
- `Assets/Localization/<bcp47>.json` contains non-base translated rows; omissions are legal and measured as fallback gaps;
- every file carries schema/version and locale ID;
- symbolic producer name + stable local ordinal/name are retained for authoring readability; build validation resolves the symbolic producer through the boundary registry.

The runtime `Localization` assembly accepts only the validated in-memory representation. **Host loading remains host-owned:** the Unity shipping host (`match-client-unity` or its final composition surface) loads Unity/TextAsset content and injects catalogues; any maintained web host owns its equivalent loader. `client-app` and the generic localization core remain free of Unity asset APIs.

## 2.6 Content validator

Add localization validation tooling/tests covering:

- schema/version;
- canonical BCP 47 locale ID;
- UTF-8/NFC content policy;
- restricted key/placeholder grammar;
- duplicate static/template/clause keys;
- unknown producer tags;
- extra variants beyond the base count;
- present translation variants with missing/extra placeholders relative to the base row;
- malformed braces/selectors/categories;
- orphan non-base rows not present in base;
- empty provided translations;
- exact producer scoping for clauses;
- base required-surface completeness.

A missing non-base row is **not** a validation failure; it is a completeness deficit and runtime fallback.

**Phase-2 exit:** core can render test catalogues for base and partial locales; content validator fails real malformed fixtures; still no producer API change.

---

# 5. Phase 3 — Independent English oracle and `living-world` migration

**Goal:** move the existing text producer behind the seam without changing what an English player sees or what simulation persists.

## 3.1 Capture the oracle before modifying code

Before touching `InteractionTextGenerator` or `InteractionTextCorpus`, generate/check in deterministic golden vectors covering:

- every defined `InteractionIntent`;
- every existing template variant at least once;
- representative slot values including scores;
- every defined citable `EventKind` clause;
- cited and uncited paths;
- malformed/default/None/out-of-roster refusal paths;
- cursor state before/after success/refusal.

The oracle records exact UTF-8 output and relevant cursor behavior so the post-migration test is independent of the new implementation.

## 3.2 Add `localization-boundary`

Create the separate Client/composition assembly only after its architecture admission is valid.

Initial contents:

- append-only producer-tag registry with the first built procedural producer;
- `LivingWorldTextBoundary`;
- living-world required-surface descriptor derived from the complete defined intent/clause rosters;
- invariant score formatting (`HomeGoals` + `-` + `AwayGoals`) before data enters `NamedSlotSet`.

## 3.3 Change the producer, not its deterministic selection

Refactor `InteractionTextGenerator` so:

- intent-value, slot and salience validation remain sim-side and pre-draw;
- `world.text` reservation/draw behavior remains exactly one successful selection draw;
- it returns living-world-owned native result data rather than final human-readable English;
- it gains no localization reference.

## 3.4 Migrate corpus content

Move templates and clauses from `InteractionTextCorpus` into `Assets/Localization/en.json` without editorial rewriting. Preserve row order and variant counts exactly.

Do not mix copy improvements into the migration PR; identity first, copy editing later.

## 3.5 Identity/determinism acceptance

Prove:

- every golden English output is byte-identical;
- every variant count equals the pre-migration corpus count;
- cited clauses match exactly;
- refused calls consume no cursor;
- successful calls advance the same cursor amount;
- rendering itself changes no RNG/tick state;
- world/season saves remain locale-neutral;
- no reverse asmdef reference exists.

**Phase-3 exit:** the only currently built procedural user-facing text producer routes through #49 with exact English identity.

---

# 6. Phase 4 — Static UI integration and continuous producer onboarding

**Goal:** prevent new UI/content systems from recreating baked-string debt while development continues in parallel.

## 4.1 Static UI policy

As UI screens are implemented:

- every authored label/button/tooltip/message copy uses a stable `LocalizationKey`;
- raw data-only values (player names, club names, numeric values, etc.) are explicitly treated as data, not fake localization keys;
- full phrases use templates rather than UI-side sentence concatenation;
- key naming follows one namespaced convention, e.g. `<surface>.<feature>.<semantic-name>`;
- renaming/deleting a key requires a translation-delta review so locale files cannot silently orphan entries.

## 4.2 Procedural producer onboarding template

For each built producer (#35/#46/#48/#50/#39/#51 as they actually land):

1. confirm the producer exists and its native identity roster is frozen enough to bind;
2. allocate the next producer tag without renumbering existing tags;
3. add one sibling boundary adapter;
4. derive its generic required-surface descriptor from the producer's own roster;
5. add exact base-locale content;
6. add producer-local identity/selection/fallback tests;
7. add it to completeness reporting automatically through the same requirements aggregation;
8. prove no producer assembly references localization.

No adapter or tag is added merely because a spec exists.

## 4.3 Merge discipline for parallel development

Prefer small producer-owned PRs:

- producer implementation can land first if it emits native identity/facts and no baked user-facing text;
- localization binding follows immediately in a narrow sibling PR if simultaneous landing would create excessive cross-branch conflicts;
- a producer is not considered player-facing complete until its base-locale coverage lock is green.

---

# 7. Phase 5 — Pseudo-localization and localization QA infrastructure

**Prerequisite:** Wave-8 FR-LC-021+ extension approved.

**Goal:** find UI/localization defects before paying for translations.

## 5.1 Pseudo-locale

Generate a dev-only locale from the base catalogue with:

- visible bracketing;
- accented/wide substitutions;
- deterministic length expansion;
- placeholder preservation;
- selector preservation.

Never expose it in production locale selection.

## 5.2 Seam-bypass check

Any UI string produced through the catalogue path should show pseudo bracketing. Pure data-only controls are explicitly exempt.

Use this as a practical complement to static routing checks, not as proof that every unbracketed datum is a bug.

## 5.3 Layout stress

Coordinate with UI workstream:

- pseudo-locale at maximum supported text scale;
- narrow/common target resolutions;
- long buttons/tabs/table headers/tooltips;
- wrapping/reflow rather than truncation where required;
- no hard-coded widths that fail under realistic expansion.

Add screen-level regression coverage as screens become real; do not invent phantom screen tests before their UI exists.

## 5.4 Completeness report

For every non-base locale, compute per-producer and total:

- required static keys;
- required base variant slots;
- required clauses;
- translated/present count;
- fallback/missing count;
- percentage.

The denominator comes from the same aggregated requirements used for base construction coverage so a newly bound producer cannot be forgotten.

---

# 8. Phase 6 — Accessibility value integration

**Prerequisite:** Wave-8 extension approved and #38 ownership back-prop accepted.

## 6.1 #49-owned values

Implement the approved a11y/locale value fragments only:

- locale selection;
- text-scale option value;
- contrast/colorblind mode selection;
- input-assist/subtitle selections where the final approved extension assigns them to #49.

They remain client-local and outside the determinism save.

## 6.2 #38/client responsibilities

UI/client owns:

- persistence in the shared client settings store;
- applying text scale/reflow;
- theme palettes for contrast/colorblind modes;
- fonts and glyph-fallback chain;
- exposing eligible locales/options to the user.

Do not put palette/font/rendering assets or behaviors into `TacticalDirector.Localization`.

## 6.3 Accessibility tests

- settings fragments round-trip;
- corrupt client settings reset to base locale + neutral accessibility values without affecting sim saves;
- pseudo-locale + maximum scale layout cases pass;
- font fallback coverage is measurable for any locale proposed for release;
- audio caption/subtitle copy routes through #49 while audio playback remains #51-owned.

---

# 9. Phase 7 — Translation production pipeline

**Goal:** turn stable base copy into shippable locale content without changing runtime architecture.

## 7.1 Locale selection and capability gate

Do not start large-scale translation solely because the infrastructure exists. Select Early Access/launch target locales using product criteria such as expected audience, store strategy, translator availability, QA capacity, font/script support, and support burden.

The first real locale is a product decision; this plan deliberately does not assume which language it is.

Before committing to a target locale, run a capability check against the approved model:

- if it requires **RTL/bidi layout**, the Stage-3+ RTL extension must be designed/implemented before the locale can be offered;
- if correct player-facing output requires locale-specific date/number/currency formatting beyond the approved base behavior, add the dedicated display-format extension rather than smuggling formatting logic into translation templates;
- if its grammar cannot be represented by the bounded selector model, open the named morphology-depth extension rather than adding one-off locale code.

Translation work may begin in parallel with such an extension if useful, but the locale cannot pass the release gate until the required capability exists.

## 7.2 Source-language production discipline

Before each translation batch:

- freeze the included base-copy revision;
- maintain a football/gameplay terminology glossary;
- maintain style/tone rules, abbreviations, capitalization and UI-length guidance;
- provide placeholder explanations and immutable token rules;
- provide screenshots/screen context once those screens exist.

## 7.3 Translator interchange

Canonical repository JSON remains authoritative. Build export/import tooling only once the actual translator/vendor workflow is known.

Initial interchange requirements:

- stable key/id;
- source English;
- context/description;
- placeholder list;
- max-length/layout notes where relevant;
- translation value;
- review status.

CSV/XLIFF/vendor-specific packages are generated artifacts, not the source of truth. Translators never edit producer tags, local ordinals, placeholder names, schema fields, or catalogue identities.

## 7.4 Translation QA

Each candidate locale passes:

1. automated schema/BCP47/Unicode/placeholder/coverage validation;
2. native-speaker linguistic review;
3. in-game LQA using real screens and procedural text;
4. pseudo/max-scale layout regression already established by the UI pipeline;
5. font/glyph coverage check;
6. smoke test of fallback behavior for intentionally missing internal-development rows;
7. any locale-specific capability gate from §7.1 (RTL/formatting/grammar) required by that locale.

Incomplete locales may exist in development but are not offered to players.

---

# 10. Phase 8 — Release gate and storefront consistency

## 8.1 Offered locale rule

`OfferedLocales(build)` is computed, not handwritten.

For each non-base locale:

- translation coverage >= owner-approved release threshold; **and**
- shipped font/fallback chain covers required script/glyphs; **and**
- all required locale-specific capability gates from §7.1 are satisfied.

Base locale bypasses this gate. Pseudo-locale is always excluded from shipping choices.

## 8.2 Release integration

#39 release engineering consumes the same report to verify:

- in-game locale picker;
- Steam/storefront supported-language claims (including the explicit mapping from canonical BCP 47 IDs to platform/store language identifiers);
- packaged locale assets;
- font assets;
- release checklist.

A build fails release compliance if those disagree.

## 8.3 Threshold ownership

The completeness percentage is a product/release policy value, not a renderer behavior. Do not invent the final number during infrastructure work. Record the owner decision when #39 release compliance activates.

---

# 11. Phase 9 — Ongoing maintenance rules

- `LocalizationKey` values are stable semantic identities; avoid cosmetic renames.
- Producer tags are append-only and never reused/renumbered.
- Producer local ordinals obey the producer's own append-only/stability contract where applicable.
- Base template variant reordering is a visible content change because it remaps deterministic selection values; require explicit review.
- New base placeholders require every existing translated variant to be revalidated.
- Translation PRs receive a delta report: added/changed/deleted source entries and impacted locales.
- Locale content remains UTF-8/NFC; identity fields remain ASCII/canonical and are never translator-owned.
- No deeper morphology/RTL/bidi/locale-formatting support is added opportunistically; each is activated by a real target-locale requirement and lands as an explicit extension.
- Runtime fallback is a safety net, not a substitute for release completeness.

---

# 12. PR / implementation slicing

Recommended mergeable slices:

| Slice | Scope | Depends on | Primary exit gate |
|---|---|---|---|
| **L-P0A** | #49 wording + ProducerTag contract gap | none | spec review converged |
| **L-P0B** | architecture admission for localization folders/components | active A3 baseline | architecture/tier checks green |
| **L-P0C** | Wave-8 extension promotion | supplement v0.4 | approval/sign-off |
| **L1** | localization core value/seam contracts | L-P0A/B | compile + seam/layer tests |
| **L2** | catalogue, renderer, schema, validator | L1 | fallback/template/content validation tests |
| **L3A** | pre-migration living-world golden oracle | none of producer code changes | exhaustive oracle locked |
| **L3B** | boundary assembly + living-world retrofit + English corpus migration | L2 + L3A | byte identity + determinism locks |
| **L4+** | each subsequent producer binding | producer exists + L3 | producer coverage/layer tests |
| **L5** | pseudo-locale + completeness report | L-P0C + L2 | non-vacuity/layout tooling tests |
| **L6** | a11y values/settings contracts | L-P0C + #38 back-props | ownership + settings tests |
| **L7** | first real locale content | UI/copy maturity + L5 + locale capability gate | coverage/LQA/font/capability gates |
| **L8** | release locale gate | #39 release path | picker/store/package agreement |

Parallelism:

- L-P0A, L-P0C, content schema design and L3A golden-vector preparation may run while A3 finishes.
- L-P0B must use the current post-A3 architectural authority before runtime asmdefs merge.
- UI can continue independently, but every new user-facing copy surface should avoid expanding baked-string debt once L1 is available.
- Translation glossary/style-guide work may begin before full copy freeze; bulk translation should not.

---

# 13. Verification required on every runtime slice

Narrow tests first, then repository gates:

- localization unit tests for the slice;
- producer-specific tests when an adapter is involved;
- `python3 tools/assembly-tier-check.py --repo .`;
- document consistency / recurring-defect checks required by the active repo instructions;
- Linux shim full generated-assembly compile/test gate;
- Unity host/cert run only for host-specific rendering/font/layout behavior when that surface is introduced.

Do not claim Unity rendering/font certification from the Linux shim.

---

# 14. Final implementation-ready exit criteria

The plan is considered ready to execute when:

1. the user accepts this sequence;
2. Phase 0 identifies no unresolved architecture/spec blocker requiring a redesign of H1-H8;
3. the implementation branch is rebuilt from current `main`, not from the provisional scaffold;
4. each slice remains independently reviewable and does not bundle translations, copy editing, producer API changes, UI rendering, and architecture changes into one oversized PR.

At that point implementation should begin with **L-P0A / L-P0B coordination and L3A oracle capture**, not by writing more localization runtime types blindly.

---

# 15. Review history for this plan

## High-level review cycle

- **v1 critique:** omitted architecture admission, Wave-8 promotion state, ProducerTag allocation, content-production workflow and parallel-branch coordination; pseudo-localization was too late and a11y ownership was blurred.
- **v2 critique:** improved separation but still treated runtime correctness as the whole problem; added canonical content/validation lifecycle and automatic producer completeness.
- **v3 critique:** resolved sequencing — approved seam may proceed independently of Wave-8 promotion; schema/validation belongs with the core, while vendor-specific interchange waits for a real translation workflow. High-level converged.

## Detailed review cycle

- **D1 critique:** identity proof lacked an independent pre-migration oracle; ProducerTag registry placement would have forced core edits; architecture admission was treated as a total blocker rather than a merge gate; host/platform content loading was insufficiently separated from the core.
- **D2 fixes:** exhaustive pre-retrofit golden vectors; ProducerTag moved to append-only boundary ownership; planning/schema/oracle tasks can proceed in parallel with A3 while runtime merge waits; canonical locale assets are content while the core consumes only in-memory data; Unity/platform loading remains host/composition-owned.
- **D3 critique:** locale identifiers/Unicode canonicalization were underspecified, and a first target language could otherwise force RTL, culture formatting, or deeper morphology into T0 by accident.
- **D3 fixes:** canonical BCP 47 IDs; UTF-8/NFC authoring policy with ASCII identity tokens; explicit host-loader ownership; pre-translation capability gates for RTL/bidi, locale-specific display formatting, and morphology depth; release gate now requires those capabilities when applicable.

**D3 conclusion:** no remaining structural High/Medium planning defect identified. Ready for implementation after Phase-0 gates and user acceptance.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-09-04 | — | Converged high-level and detailed localization implementation plan after iterative critique. |
| 1.1 | 2026-09-04 | — | Final hardening: BCP 47/Unicode canonicalization, explicit host loading, and target-locale capability gates for RTL/formatting/morphology. |
#endregion
