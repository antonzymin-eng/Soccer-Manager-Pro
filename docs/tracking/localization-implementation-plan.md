# Localization #49 — End-to-End Implementation Plan

**Created:** September 6, 2026
**Version:** 2.5
**Status:** READY FOR IMPLEMENTATION
**Baseline:** `main` at `67f2343c34e767ba02a4dc13816c91090b3bf3d9` — the L3A merge commit (PR #370, September 9, 2026). v2.0–v2.2 were authored against `9fbd7533`; the historical review records below keep that value and are not rewritten.
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
| M4 — external JSON had no CI-path loader | **Accepted; design removed** | No external locale file is part of seam T0/T1. Base English remains compiled host-free content so the Linux policy gate exercises the exact shipped T0/T1 content. External content files/loaders are a Wave-8 decision. |
| Roadmap framing | **Accepted** | #49 content is explicitly deferred past PM-3. Seam T0 is parallel work, not critical-path work. |
| Second review M-a — “full Linux gate green” conflicts with owner-held RED | **Accepted as an ambiguity; factual premise updated** | Current `main` makes `tools/run-tests-local.sh --pr` the canonical policy composition. It excludes the exact owner-held RED from the ordinary blocking pass and verifies that RED separately. Every runtime exit now requires that canonical PR composition to pass, with no new failures and the owner-held RED verifier accepting the recorded baseline. A bare `run-gate.sh` result is not the acceptance proof. |
| Second review M-b — same-commit lists omit checker/close-out surfaces | **Accepted** | L0R/L1/L3B now explicitly include landing-close-out and every checker-tracked cardinality they change: assembly counts, assembly-less spec counts, ERR counts, active/archive issue counts where applicable, changelog chains, `src/CLAUDE.md`/`CHANGELOG-src.md`, file manifest, and the dated README snapshot when its facts move. |
| Second review M-c — ERR-049-002 discharge split is ambiguous | **Accepted** | ERR-049-002 is discharged **wholly in L1**. L1's executable layer proof is sufficient to correct the already-approved KD-6 wording, including its reference to the boundary topology. |
| Second review lows — deferred ERR skill timing, base-content placement, dev marker, soft corpus exit | **Accepted** | L0R explicitly uses the ERR skill's named-stage deferral because C6 forbids the normal immediate back-prop; #49-owned base English is pinned to `TacticalDirector.Localization`; T0 omits the optional dev marker; L3B requires complete removal of human-readable corpus ownership from `living-world`. |
| Third review — L3B could satisfy the generic PR gate after coverage ceiling fallback | **Accepted, scoped** | Preserve PR #375's deliberate global runtime protection, but make L3B acceptance stricter: its gate evidence must name one or more real non-sentinel production coverage assemblies. Either zero-instrumentation line is an L3B failure. |
| Third review — L3A frozen-file invariant was only a comment / orphan supplement | **Accepted** | Consolidate the rule directly into §7.7 and §12. L3B must use a two-dot diff from the actual merged L3A commit and fail on any modification/deletion/rename of the frozen expectations file. |
| Third review — planning artifact drifted behind the implementation baseline | **Accepted** | Rebuild this planning branch directly on current `main` at `9fbd7533` and update the header baseline. The standalone acceptance amendment is superseded by this consolidated v2.2 text. |

---

# 2. Settled high-level plan

## H1 — Record defects; do not harden before T0

Before implementation, record known #49 defects without correcting their contract text:

- authoritative §1 KD-6 still contradicts §2/§3/§4/§5 by describing a core `Localization -> living-world` reference and `TextTemplateId.ForInteraction` factory;
- the generic `TextTemplateId` has an integer producer tag, while later approved specs assume symbolic append-only `ProducerTag.*` allocations without an owning allocation contract;
- FR-LC-009 mandates a bounded plural/gender category selector, while §2.2 supplies no typed operand it could consume — `NamedSlotSet` values are already-formatted strings and `LocalizedTextRequest` has no operand field;
- FR-LC-011's "render the base-locale identity" is circular for a **static** `LocalizationKey` absent from both catalogues, because FR-LC-008a/§2.3 F5's loud construction coverage is enumerated over the procedural rosters only.

Recording is allowed by C6; correction waits for the implementation commit that proves the replacement. **All four are recorded, not two** — see §3.1–§3.4.

## H2 — Capture the pre-migration English oracle first

Extend the existing living-world test suite to exhaustively lock current English output, variant coverage, clause output and cursor/refusal behavior **before** any localization code changes. This is an observation lock on built code, not hardening of #49.

## H3 — Land #49 T0 as the generic core, with architecture admission in the same commit

Create `TacticalDirector.Localization` and simultaneously:

- seat `src/localization/` in the active assembly-tier authority;
- add any architecture-governance records that are required for a newly existing component at that moment;
- discharge ERR-049-002 and correct the §1 KD-6 reference-direction defect in the same commit;
- discharge ERR-049-004 in the same commit — the selector operand is a core seam type, so it cannot wait for L2 (§3.3);
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

Create `TacticalDirector.LocalizationBoundary`, seat it in the same commit, resolve the ProducerTag ownership gap, migrate #22's English corpus out of `living-world` and into #49-owned compiled content in `TacticalDirector.Localization`, change `InteractionTextGenerator` to native result data, and prove byte identity/determinism against the pre-migration oracle.

## H6 — Onboard later producers only when their code exists

Each later procedural producer adds one sibling adapter + allocated tag + base content + roster coverage. Static UI text uses `LocalizationKey`. No phantom adapter is added for an unbuilt producer.

## H7 — Leave Wave-8 content/a11y expansion deferred

The AR-converged `localization-content-a11y-design.md` is useful design input, but it is **not promoted or implemented as a prerequisite** for seam T0/T1. It resumes at the project's later content/release stage unless the owner deliberately reprioritizes it.

---

# 3. Pre-T0 record-only slice — L0R

**Purpose:** make known defects durable without violating C6.

The repository's `err-file-and-backprop` skill normally patches defective approved spec text in the same commit. **C6 is the controlling exception here because #49 has no production assembly yet.** L0R therefore uses the skill's explicit **deferred to a named stage** timing: file **all four** entries (§3.1–§3.4) as OPEN/deferred, name their discharge slices, and do not “helpfully” patch the contract early.

## 3.1 Proposed ERR-049-002 — stale core reference direction

At filing time, re-verify `ERR-049-002` is free.

Record, but do not yet correct:

- `docs/specs/localization-accessibility/section-1.md` KD-6 says the renderer references built producers and explicitly states `Localization -> living-world`;
- the same paragraph cites `TextTemplateId.ForInteraction(intent)`;
- authoritative §2.2/§2.2.1, §3, §4.1, §5 and §9 instead require a producer-agnostic core and a sibling `LivingWorldTextBoundary`.

Status at L0R: **OPEN — deferred to L1 T0 by C6.**

ERR-049-002 is discharged **wholly in L1**. L1 can replace KD-6 with the already-approved producer-agnostic topology because the new `localization` assembly and its layer tests provide executable evidence for the corrected reference direction; the wording may name the already-specified `LivingWorldTextBoundary`/`src/localization-boundary/` topology without constructing that boundary early.

Do not rewrite historical review records simply because they describe the pre-fix design. Search each occurrence and classify it as live contract vs historical record.

## 3.2 Proposed ERR-049-003 — ProducerTag allocation ownership

At filing time, re-verify `ERR-049-003` is free.

Record:

- #49 defines `TextTemplateId.ProducerTag` as `int` but no allocation owner/table/append-only rule;
- later specs use symbolic `ProducerTag.Media`, `.Inbox`, `.MatchCommentary`, etc.;
- catalogue identity is the **pair** `(ProducerTag, LocalOrdinal)`, so collision prevention must cover the pair and tag allocation stability.

Status at L0R: **OPEN — deferred to L3B T1 by C6/real-consumer timing.**

The record must inventory all current downstream symbolic uses. Minimum current-main search includes:

- #35 media/press section 4 + appendices;
- #46 news/inbox section 4 + appendices + boundary tests;
- #48 match-presentation section 4;
- any #39/#50/#51 occurrences found by repository-wide search at filing time.

Do not choose/fix the allocation mechanism in this record-only slice. T1 execution decides it against real code.

## 3.3 Proposed ERR-049-004 — plural/gender selector has no typed operand in the core seam

At filing time, re-verify `ERR-049-004` is free.

Record, but do not yet correct:

- **FR-LC-009** requires the template model to be named-placeholder substitution **plus a bounded plural/gender category selector** (CLDR-style categories + a small gender set);
- **§2.2** defines `NamedSlotSet` as an immutable **name -> string** map whose values are "**ALREADY formatted to a string** by the boundary adapter", and defines generic `Expand` as pure string substitution (the v0.3 AR-3 amendment);
- `LocalizedTextRequest` carries `Id`, `SelectionDraw`, `Slots`, `HasCitedEpisode`, `CitationKind` — and **no** selector operand;
- a CLDR plural category is a function of a numeric operand **and** the target locale's rules, neither of which a pre-formatted display string reliably carries.

So FR-LC-009 mandates a selector for which §2.2 supplies no input. The two cannot both hold as written.

Status at L0R: **OPEN — deferred to L1 T0 by C6.**

**Discharge stage is L1, not L2.** Any locale-neutral typed selector operand must live on a **core seam type** — `LocalizedTextRequest` or `NamedSlotSet`, both declared in the §2.2 CORE block — and §5 creates exactly those types. Deferring the decision to L2 would land the T0 public seam already known to be incomplete and then change it in the next slice, which is what §5.1's same-commit rule and C6's "discharged at T0, in the same commit as the code they govern" timing exist to prevent.

Two already-approved constraints narrow the design and must be carried in the record, because they rule out the obvious shortcuts:

- **FR-LC-006** — a producer's serialized state and its draw are locale-independent and a save round-trips byte-identically across display locales, so the operand must not change serialized bytes per locale;
- **FR-LC-014** — `LocalizedTextRequest` and `TextTemplateId` are assembled by the **boundary adapter**, never by the producer, so the operand is boundary-assembled from the producer's native values.

Base-locale English "declares no categories (identity with `.Replace`)" per FR-LC-009, so nothing before L2's synthetic selector catalogues exercises this path at all. That is why it survived approval; it is **not** a reason to defer the contract past L1.

Do not choose the operand type in this record-only slice. L1 decides it against the real seam, together with ERR-049-002.

## 3.4 Proposed ERR-049-005 — no terminal result for a static key absent from the base catalogue

At filing time, re-verify `ERR-049-005` is free.

Record, but do not yet correct:

- **FR-LC-011** requires a missing key to "render the base-locale identity", to not crash and to not mutate state, and permits the visible `‹key›` marker **only** in dev builds;
- **FR-LC-008a / §2.3 F5** make a missing base-locale row a loud construction failure — but their coverage is enumerated over the **procedural rosters only** (a defined `InteractionIntent` with no base template row, a defined citable `EventKind` with no base clause);
- a **static** `LocalizationKey` row therefore sits outside that coverage net;
- for such a key absent from both the selected and the base catalogue, "render the base-locale identity" is circular — the base identity is precisely what is missing — and the only terminal marker is dev-build-only.

The production terminal result is undefined. Two implementers could reasonably choose null, empty string, the key text, or a throw; the last would violate FR-LC-011's own "MUST NOT crash". This is a contradiction between FR-LC-011 and the scope of FR-LC-008a/F5, in the approved spec.

Status at L0R: **OPEN — deferred to L2 by C6, conditional on the L1 seam decision below.**

**The discharge stage depends on the resolution chosen, and the choice cannot be left until L2.** Extending FR-LC-008a's construction coverage to every admitted static key, or defining an explicit production-safe terminal string, are both L2-discharged: they change catalogue construction (§6.4) and renderer behavior (§6.2) without altering `ILocalizer`'s signature. But a Try-pattern resolution — `bool TryResolve(LocalizationKey, out string)` or any variant returning a found/not-found signal — **changes an L1 public contract** and would move this entry's discharge to L1.

Therefore: **decide which family the resolution belongs to before L1 freezes `ILocalizer`.** If it needs a signature, it lands in L1 with ERR-049-004; otherwise it discharges in L2 and L1 records that `Resolve`'s signature is deliberately final. Do not choose the resolution in this record-only slice.

## 3.5 L0R same-commit record/close-out

Because L0R adds ERR rows and records a live deferred defect, its commit must also run the current `landing-close-out` contract. Recompute immediately before commit rather than trusting the planning-baseline counts.

At minimum update every surface actually changed by the new records:

- #49 §7/deferred-finding location and its version history, without changing functional FR/KD wording;
- `docs/tracking/spec-error-log.md` header/version/index rows and any checker-tracked **ERR cardinality** claim (current baseline has only ERR-049-001; proposed additions are -002/-003/-004/-005, but IDs and global counts are rechecked at landing);
- `docs/tracking/CHANGELOG.md` header chain;
- `docs/tracking/open-issues.md` plus `docs/agent-guides/project-reference.md` title index/counts if the deferred localization defects create/update a live issue entry; update archive counts too if any entry is moved;
- any manifest/current-state pointer that the active landing-close-out skill says the modified spec/tracking files require.

## 3.6 L0R exit

- **all four** ERR entries are durably recorded as OPEN with named discharge slices (`ERR-049-002 -> L1`, `ERR-049-003 -> L3B`, `ERR-049-004 -> L1`, `ERR-049-005 -> L2`, the last conditional per §3.4 and re-decided before L1 freezes `ILocalizer`);
- spec version-history rows updated because the spec record is modified;
- no functional FR/KD wording is hardened;
- no production assembly or API is created;
- `python3 tools/doc-consistency-check.py` and other ordinary document gates pass with all changed cardinalities current.

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

The oracle must also freeze the complete current producer rosters by comparing frozen counts to the real `InteractionIntent` and `EventKind` enum member counts. An append to either roster before L3B is therefore a deliberate L3A failure, not silently uncovered content.

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

Preserve the mutation evidence outside the disposable branch before closing it.

## 4.5 L3A exit

- no #49 production code/spec hardening;
- existing `living-world` assembly/tests only;
- golden oracle green on current code and proven non-vacuous;
- migration-independent frozen expectations live in `src/living-world/Tests/InteractionTextOracleExpectations.cs`;
- normal landing-close-out surfaces updated for the added test code, including `CHANGELOG-src.md`/`src/CLAUDE.md` if required by the current skill.

---

# 5. T0 core contracts — L1

**Purpose:** create the APPROVED `TacticalDirector.Localization` seam with no producer coupling.

## 5.1 Same-commit architecture and close-out rule

The L1 commit contains all mechanically inseparable changes:

1. `src/localization/localization.asmdef`;
2. Code Standards §3.5.2 seating for `localization` in the currently authoritative tier;
3. any active architecture-governance inventory/property/integration records that become mandatory only because the component now exists;
4. **all** ERR-049-002 contract correction/back-prop needed to mark the entry `✅ RESOLVED`;
4a. **all** ERR-049-004 contract correction/back-prop needed to mark that entry `✅ RESOLVED` — the locale-neutral typed selector operand lands on the core seam type in the **same commit** that creates it, because L1 is the commit that freezes `LocalizedTextRequest`/`NamedSlotSet`. Also settle §3.4's question here: either take ERR-049-005's signature-changing resolution now, or record that `ILocalizer.Resolve`'s signature is final and the entry discharges in L2;
5. localization core code/tests;
6. landing-close-out surfaces required by a new production assembly/code landing: `docs/tracking/CHANGELOG.md`, `src/CLAUDE.md` + `docs/tracking/CHANGELOG-src.md`, `docs/tracking/file-manifest.md`, the relevant `open-issues`/project-reference records, and any owning design/spec history required by the active skill;
7. every checker-tracked current-state cardinality changed by the assembly landing. Recount immediately before commit. The old 35 -> 36 example is historical only; use live counts from the implementation base. Update the dated README status snapshot if and only if those stated facts are thereby falsified.

Never land the tier row without the asmdef or vice versa. Never leave a stale `N production assemblies`, `N of M ... no src assembly`, ERR count, or active/archive issue count behind merely because the code itself is correct.

Re-read the active A3/A4 architecture state and the current landing-close-out skill immediately before implementation; this plan does not freeze registry mechanics or cardinalities that may change before L1 starts.

## 5.2 Core assembly contract

`TacticalDirector.Localization` references **no sim/producer assembly**.

Implement the minimum approved public surface:

**Three of the shapes below are the pre-fix §2.2 text and are explicitly subject to the two recorded L1 decisions (§3.3, §3.4).** They are listed as the starting point, not as a contract to freeze as written: ERR-049-004 requires a locale-neutral typed selector operand on `LocalizedTextRequest` or `NamedSlotSet`, and ERR-049-005's resolution may add a found/not-found signal to `ILocalizer`. Landing this list unchanged would freeze the defect the same commit is supposed to discharge.

- `ILocalizer` — `Resolve(LocalizationKey)` and `Render(in LocalizedTextRequest)` as rendered-string entry points. **Subject to §3.4:** if that decision takes a Try-pattern resolution, `Resolve`'s signature changes here; otherwise L1 records the signature as final;
- `LocalizationKey` — stable static-string identity;
- `TextTemplateId` — generic `(int ProducerTag, int LocalOrdinal)`;
- `NamedSlotSet` — immutable name -> already-formatted string values. **Subject to §3.3**, which may extend it, or `LocalizedTextRequest`, with the typed selector operand;
- `LocalizedTextRequest` — id, `ulong SelectionDraw`, slots, citation flag/key. **Subject to §3.3** per the previous bullet;
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
- no public baked-string pass-through API exists;
- **the ERR-049-004 selector operand, structurally** (§3.3): it exists on the core seam, is a typed value rather than a formatted string, is locale-neutral — carrying no locale identity and no locale-dependent formatting — has deterministic value equality/hash like the other seam types, cannot be mutated through retained caller references, and adds no persisted or RNG state. These are L1-runnable type-level locks; they do **not** attempt to prove selection behavior, which needs the L2 renderer and catalogue.

## 5.4 Layer tests

Mechanically assert:

- core asmdef has no producer/sim references;
- no sim/loop assembly references `TacticalDirector.Localization`;
- type-shape contains no sim-owned type.

This executable layer proof is the evidence used to discharge ERR-049-002 in full.

## 5.5 L1 exit

- assembly-tier check green;
- active architecture-governance and document-consistency checks green;
- localization unit tests green;
- canonical PR-equivalent Linux policy composition `bash tools/run-tests-local.sh --pr` passes;
- no new test failure exists, and the separate owner-held RED verifier accepts the exact recorded `sim_match_engine_close_chance` baseline (an unexpected pass or diagnostic drift is a failure);
- if the lower-level raw generated-project executor is also run for investigation, its owner-held RED result is recorded as baseline evidence rather than incorrectly required to be whole-tree green;
- all assembly/spec/ERR/open-issue cardinalities changed by L1 are current;
- ERR-049-002 is `✅ RESOLVED` with executable evidence;
- ERR-049-004 is `✅ RESOLVED` with executable evidence that is **runnable in L1**: the §5.3 structural locks prove the core seam now carries a locale-neutral typed selector operand, and FR-LC-006 is re-proven — no locale-dependent state is persisted and the operand does not enter serialized bytes. That is the whole of this ERR: the recorded defect is that the seam supplies *no* operand, and it is discharged when the operand exists with the right properties. **The behavioral half is L2's and is not required here** — proving the operand actually drives `one/few/many/other` selection needs the renderer and catalogue, which §6 builds; requiring it in L1 would pull L2 forward. §6.6 carries that proof as FR-LC-009 conformance, not as this ERR's discharge;
- §3.4 is settled on the record: either ERR-049-005's resolution changed `ILocalizer` here, or the L1 entry states that `Resolve`'s signature is final and the discharge stays in L2. Do not leave it undecided — L1 freezes the interface.

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
3. if both are absent, follow the construction/fallback invariant; **omit the optional dev-only `‹key›` marker in T0**. FR-LC-011 permits that marker but does not require it, while production must fall through to base.

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
- canonical `bash tools/run-tests-local.sh --pr` passes with no new failures and the owner-held RED verifier accepting its recorded baseline;
- normal landing-close-out/document-consistency surfaces are current;
- **FR-LC-009 selector behavior is proven here, on the operand L1 delivered** (§3.3, §5.3): a synthetic non-English catalogue selects a non-`other` plural category through the real renderer, and base-locale English still takes the identity path. This is FR-LC-009 conformance, not ERR-049-004's discharge — that entry closed in L1 on the structural locks;
- ERR-049-005 is `✅ RESOLVED` with executable evidence, unless §3.4's L1 decision already discharged it there: a static `LocalizationKey` absent from **both** the selected and the base catalogue has one defined, production-safe terminal result, proven by a test asserting that exact result and asserting no throw and no state mutation (FR-LC-011). If the resolution was to extend construction coverage to static keys instead, the proof is a killed mutant: a catalogue omitting one admitted static key must fail construction.

---

# 7. T1 living-world retrofit + boundary — L3B

**Purpose:** fulfill FR-LC-016 using the oracle, while resolving the remaining ProducerTag gap against real code.

## 7.1 Same-commit boundary architecture and close-out

The L3B commit contains together:

1. `src/localization-boundary/localization-boundary.asmdef`;
2. Code Standards §3.5.2 seating for `localization-boundary`;
3. required architecture-governance records for the now-existing component;
4. ProducerTag allocation implementation + complete ERR-049-003 spec/back-props, allowing the entry to become `✅ RESOLVED`;
5. `LivingWorldTextBoundary` + base content/requirements;
6. living-world producer API retrofit;
7. corpus migration/removal;
8. identity/determinism tests;
9. landing-close-out surfaces required by the new assembly/API/spec landing: `docs/tracking/CHANGELOG.md`, `src/CLAUDE.md` + `docs/tracking/CHANGELOG-src.md`, `docs/tracking/file-manifest.md`, affected open-issue/project-reference records, spec-error-log history, and any owning design/spec histories;
10. every checker-tracked cardinality changed by the landing. Recount immediately before commit; do not reuse historical 36 -> 37 examples after `main` moves.

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

#49 §2.2 already places catalogue content in `TacticalDirector.Localization`; T1 follows that authority rather than leaving placement open.

Migrate the exact English **string/template/clause content into `src/localization/` / `TacticalDirector.Localization`**. The core still must not reference `living-world` or boundary types. If the ProducerTag allocation itself is boundary-owned, the localization-side base-content factory may accept the generic integer producer tag as an input; do **not** duplicate/hardcode a second symbolic tag source inside the core merely to place the strings there.

Requirements:

- no editorial rewrites;
- exact row order and variant counts preserved;
- content exercised directly by Linux tests — no duplicate embedded test fixture standing in for an untested shipped JSON asset;
- `living-world` owns **no human-readable template/citation strings** after migration;
- content is #49-owned and compiled host-free in `TacticalDirector.Localization`, while producer roster/type mapping remains in the boundary.

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
- layer/tier/governance/document-consistency checks green;
- **no human-readable `InteractionTextCorpus` template/clause ownership remains sim-side**;
- ERR-049-003 is `✅ RESOLVED` and all downstream spec back-props are consistent;
- every changed assembly/ERR/open-issue/current-state cardinality is current;
- canonical `bash tools/run-tests-local.sh --pr` passes with no new failures and the owner-held RED verifier accepting the exact recorded baseline;
- the same canonical gate log must contain `PR coverage assemblies:` followed by one or more real, non-sentinel production assembly names. Either exact output below is an L3B acceptance failure even if the generic PR job itself is green:
  - `PR coverage assemblies: none (scope ceiling fallback)`
  - `PR coverage assemblies: none (no shim-coverable changed production/test-owned src assembly)`
- if L3B resolves more than `MAX_PR_COVERAGE_ASSEMBLIES` and triggers the global ceiling, split L3B only where semantics/atomicity allow or land a separately reviewed Testing Strategy coverage-policy solution. Never waive the zero-instrumentation result merely to satisfy this slice;
- L3A merged as PR #370 on September 9, 2026, so `<L3A_MERGE_COMMIT>` is no longer a placeholder. It is **`67f2343c34e767ba02a4dc13816c91090b3bf3d9`** — the merge commit on `main`, *not* the merged branch tip `79543a5` and not a later `main` commit. Mechanically prove the frozen expectations file is unchanged with two-dot semantics:
  `git diff --quiet 67f2343c34e767ba02a4dc13816c91090b3bf3d9 HEAD -- src/living-world/Tests/InteractionTextOracleExpectations.cs`
  A non-zero result blocks L3B. This catches modification, deletion and rename without depending on branch topology. For independent verification the frozen blob is `0907c88b312e43e07363abb847fc3fae6a475b05`; that hash is informational corroboration, not a second gate — the `git diff --quiet` result above is the acceptance criterion.

---

# 8. Later producer onboarding — L4+

This work is continuous, not a prebuilt batch.

For each **built** procedural producer:

1. verify its native identity/fact/selection contract exists;
2. allocate the next tag without changing prior allocations;
3. add a sibling boundary adapter;
4. add required-surface enumeration from that producer's defined roster;
5. add exact base-English content under the #49-owned localization content surface;
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

At that stage, revisit whether compiled T0/T1 base content should migrate to repository data files. Do not pre-commit to JSON/XLIFF/vendor formats during seam T0/T1.

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
| 0 | **L0R** | record OPEN ERR-049-002/003/004/005 with named discharge stages only | none | C6-compliant durable findings |
| 1 | **L3A** | exhaustive current-English/cursor oracle | built #22 | non-vacuous golden lock |
| 2 | **L1** | T0 core contracts + same-commit tier/governance admission + complete ERR-049-002 **and ERR-049-004** discharge | L0R | compile/layer/policy gates |
| 3 | **L2** | approved in-memory catalogue/renderer/template/fallback only + ERR-049-005 discharge (unless §3.4 moved it to L1) | L1 | seam behavior/policy gates |
| 4 | **L3B** | T1 boundary + same-commit seating + ERR-049-003 discharge + #22 retrofit + compiled base content in Localization | L2 + merged L3A | byte identity + determinism + non-sentinel coverage + frozen-file zero diff |
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
- **canonical PR-equivalent Linux policy:** `bash tools/run-tests-local.sh --pr`;
- verify the separate owner-held RED step accepts exactly the recorded `sim_match_engine_close_chance` failure identity/diagnostics; unexpected green, drift, ambiguity or extra failures block;
- if `tools/dotnet-ci/run-gate.sh` is run directly for debugging, do not substitute its raw whole-tree status for the policy-runner verdict;
- landing-close-out drift check plus explicit inspection of its output;
- `git diff --check`/equivalent hygiene.

L3B adds two mandatory evidence checks beyond generic PR-gate success:

1. **Non-vacuous PR coverage.** Capture the canonical gate output and require a real named production assembly set after `PR coverage assemblies:`. Reject both zero-instrumentation lines exactly:
   - `PR coverage assemblies: none (scope ceiling fallback)`
   - `PR coverage assemblies: none (no shim-coverable changed production/test-owned src assembly)`
   Do not infer coverage merely from a green job.
2. **Frozen-oracle immutability.** Use the actual merged L3A commit — **`67f2343c34e767ba02a4dc13816c91090b3bf3d9`** (PR #370, September 9, 2026) — not a branch tip, and run:
   `git diff --quiet 67f2343c34e767ba02a4dc13816c91090b3bf3d9 HEAD -- src/living-world/Tests/InteractionTextOracleExpectations.cs`
   The command must return zero. Use two-dot semantics; do not use `...` here.

Do not claim Unity-host rendering/font/layout certification from the Linux shim. No Unity-host behavior is introduced in T0/T1 anyway.

For every slice that changes a production assembly, ERR inventory, or live issue inventory, recompute and update the corresponding machine-checked cardinalities in the same commit. Do not copy historical plan examples after `main` has moved.

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

Any such finding is recorded as an ERR and discharged with the implementation that proves the resolution, following C6 and the ERR skill's named-stage deferral mechanism where the consumer does not yet exist.

---

# 14. Implementation-ready exit criteria

The plan is ready for execution when:

1. implementation starts from current `main`, not either stale planning/scaffold branch;
2. L0R verifies the next ERR-049 ids are free before filing and records each OPEN with its named discharge stage;
3. L3A locks the current producer before migration, including complete enum-roster counts;
4. L1 and L3B each perform assembly seating in the same commit as their asmdef;
5. no Wave-8-only requirement is treated as a seam-T0 prerequisite;
6. ERR-049-002 and ERR-049-004 are discharged wholly in L1, ERR-049-003 wholly in L3B, and ERR-049-005 in L2 unless §3.4's contract decision moves it to L1 — each with executable evidence;
7. every runtime slice uses the canonical PR policy runner rather than demanding an impossible/raw low-level whole-tree green;
8. L3B additionally proves a real non-sentinel PR coverage assembly set and a zero two-dot diff from the actual L3A merge commit for `InteractionTextOracleExpectations.cs`;
9. all landing-close-out surfaces and machine-checked cardinalities affected by each slice are updated atomically;
10. every slice remains independently reviewable.

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

## v2.1 second external review corrections

Accepted:

- make ERR-049-002's discharge unambiguously all-L1;
- name the complete landing-close-out/cardinality blast radius for L0R/L1/L3B;
- pin base-English string/template/clause ownership to `TacticalDirector.Localization` per #49 §2.2;
- omit the optional dev marker in T0;
- remove the soft residual exception from the living-world corpus exit;
- document explicitly why L0R uses deferred ERR timing under C6.

Corrected against current `main` rather than blindly adopting the review:

- `sim_match_engine_close_chance` remains owner-held RED, but current PR/nightly policy **does not require the raw full tree to turn green**. `tools/run-tests-local.sh --pr` excludes that exact test from the ordinary blocking pass, executes it separately, and blocks on any unexpected pass or diagnostic drift. The plan therefore requires the canonical policy composition to pass and the owner-held verifier to accept its baseline; a bare low-level `run-gate.sh` is not the acceptance criterion.

## v2.2 third external review corrections

Accepted and consolidated directly into the authoritative plan:

- PR #375's >8-assembly sentinel fallback remains valid as global runtime protection, but L3B may not count a zero-instrumentation green run as acceptance evidence;
- §7.7 and §12 now require a real named PR coverage assembly set and reject both exact sentinel/no-coverable lines;
- the strongest L3A migration invariant is now mechanical in the plan itself: a two-dot `git diff --quiet` from the actual **merged L3A commit** must prove `InteractionTextOracleExpectations.cs` unchanged through L3B;
- the standalone `localization-l3b-acceptance.md` amendment is superseded and removed;
- the planning branch is rebuilt directly on current `main` at `9fbd7533`, which already contains PR #375.

**v2.2 conclusion:** no remaining known structural High/Medium planning defect. L1 remains held until L3A positive CI and negative-control evidence are both complete; after that, execution follows L1 -> L2 -> L3B with the stricter L3B acceptance above.

## v2.3 L3A landing reconciliation

L3A merged. This revision records what landed and resolves the one placeholder v2.2 could not fill; it adds no new requirement and relaxes none.

- **`<L3A_MERGE_COMMIT>` is resolved.** PR #370 merged into `main` on September 9, 2026 as `67f2343c34e767ba02a4dc13816c91090b3bf3d9`. §7.7 and §12 now carry that literal instead of the placeholder, together with the explicit warning that the merged branch tip `79543a5` is *not* the value to use. The frozen blob at that commit is `0907c88b312e43e07363abb847fc3fae6a475b05`.
- **The header baseline advances** from `9fbd7533` to the L3A merge commit, so §14 criterion 1 ("implementation starts from current `main`") is measured against a `main` that already contains the oracle. The v2.0–v2.2 review records keep `9fbd7533` and are deliberately not rewritten.
- **The v2.2 hold on L1 is discharged.** v2.2 held L1 until L3A had both positive CI and negative-control evidence. Both are complete: the PR #370 head `79543a5` passed the canonical gate (run `34308477489`, `Gate PASSED`, quarantine empty, the one failure the recorded owner-held RED `sim_match_engine_close_chance`, not rebaselined), and disposable negative-control PR #371 — closed unmerged — produced 9 `LivingWorld.Tests` failures in run `34263162049` covering all three §4.4 mutation classes.
- **One evidence limitation is carried forward rather than smoothed over.** `Generator_EveryCitationClause_IsFrozenEndToEnd` diverged at index 12 on the *row-text* mutation, so that single end-to-end assertion evidences the row-text class only. The clause-punctuation class rests on `Corpus_EveryCitationClause_IsFrozen` (`EventKind` ordinal 1, index 50). The classes are all covered; no one assertion covers all of them.
- **The execution order in §11 is unchanged; only its remaining span shortens** to **L0R -> L1 -> L2 -> L3B**, L3A having landed. §11 is a dependency/order table and carries no status column, so its L3A row is left as written rather than annotated here. §11 row 4's `L2 + merged L3A` dependency for L3B is now half-satisfied: the merged-L3A half is met.
- **`codex/localization-infrastructure-t0` remains non-authoritative and must not be merged.** Landing L3A does not change that disposition; L1 is built fresh per §5, not by promoting that scaffold.

---

## v2.4 two further pre-T0 defects recorded

A review of the v2.3 landing raised two Codex findings against §6 and correctly rejected the disposition first proposed for them — that this plan could land unchanged and PR #372 absorb both. It could not: **C6 requires known defects in an assembly-less spec to be recorded, not merely deferred**, and recording a finding with a named discharge stage is not the pre-T0 hardening C6 forbids. §0 item 1 restricts *hardening the contract*, not *writing down that a contract is broken*. Landing the authoritative plan with L0R still scoped to two known defects, when four are known, would have made it stale at birth.

- **Both findings are real defects in APPROVED #49**, verified against the spec rather than accepted on assertion: FR-LC-009 mandates a plural/gender selector for which §2.2 supplies no typed operand (**§3.3**, proposed ERR-049-004), and FR-LC-011's "render the base-locale identity" is circular for a static key that FR-LC-008a/F5's procedural-roster-only coverage never guaranteed (**§3.4**, proposed ERR-049-005).
- **L0R's scope grows from two entries to four.** §3.6, §11 row 0 and §14 criterion 6 previously named only ERR-049-002/003; all three now name four. §3.5's spec-error-log bullet lists the proposed additions. No id is allocated here — L0R allocates them and re-verifies each is free at filing, per the `ERR-030-025` precedent.
- **ERR-049-004 discharges in L1, not L2.** This corrects the stage first proposed for it. A locale-neutral typed selector operand must live on `LocalizedTextRequest` or `NamedSlotSet`, both declared in §2.2's **CORE** block, and §5 is the commit that creates and freezes those types. Deferring to L2 would knowingly land an incomplete T0 public seam and then change it, which §5.1's same-commit rule exists to prevent. §5.1 item 4a, §5.5, §11 rows 0 and 2, §14 criterion 6 and H3 are updated together.
- **ERR-049-005 discharges in L2, conditionally, and the condition is resolved in L1.** Extending construction coverage to static keys, or defining an explicit terminal string, changes only §6.2/§6.4 and stays in L2. A Try-pattern resolution changes `ILocalizer` and would move it to L1. Because L1 freezes the interface, §3.4, §5.1 item 4a and §5.5 require the *family* to be chosen before L1 lands, even though the resolution itself is L2 work.
- **Executable exit evidence is named for both**, so neither can be closed by assertion: §5.5 requires a synthetic non-English catalogue selecting a non-`other` plural category plus a re-proven FR-LC-006 byte-identical save round-trip; §6.6 requires either a defined terminal result proven with no throw and no mutation, or a killed construction mutant.
- **No normative fix is chosen for either defect here**, and no FR or KD wording is altered. That remains forbidden pre-T0 and is the discharging slice's work.

---

## v2.5 propagate the four-defect scope into the L1 contract and split the ERR-049-004 evidence

Review of v2.4 found that the two new records had been added without following them through into every place the old two-defect scope was written down, and that one piece of exit evidence had been placed in a slice that cannot run it. Both are corrected here. No new defect is recorded and no normative fix is chosen.

- **§5.2 no longer freezes the pre-fix API it is meant to repair.** It still listed `ILocalizer` as "exactly `Resolve`/`Render`", `NamedSlotSet` as name→already-formatted-string, and `LocalizedTextRequest` with only the old fields — directly contradicting §3.3, §3.4, §5.1 item 4a and §5.5, which require L1 to change those very shapes. Landing that list unchanged would have frozen the defect the same commit is supposed to discharge. The three affected bullets are now marked **subject to** the two recorded L1 decisions, and the list is framed as the starting point rather than the contract.
- **ERR-049-004's exit evidence is split, because v2.4 placed a behavioral proof in a slice that cannot execute it.** §5.5 required "a synthetic non-English catalogue test that selects a non-`other` plural category" — but the renderer, catalogue and selector behavior are all §6 (L2) work, so L1 could not have run it without pulling L2 forward. The evidence now divides along the slice boundary: **L1** proves the operand *exists* with the right properties (§5.3 — typed not string, locale-neutral, deterministic equality, immutable, no persisted or RNG state, FR-LC-006 re-proven), which is the whole of the recorded defect, since the defect is that the seam supplies no operand; **L2** proves the operand *drives* `one/few/many/other` selection (§6.6), recorded there as **FR-LC-009 conformance, not as this ERR's discharge**. ERR-049-004 still closes in L1.
- **The stale two-defect scope is swept.** §2 H1 listed only the original two known defects and the §3 preamble still said "file both entries as OPEN/deferred"; both now name all four. This completes the propagation begun in v2.4, which had correctly updated §3.5, §3.6, §11 and §14 but missed these two narrative sites.

---

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 2.0 | 2026-09-06 | — | Rebuilt from current main after external C6/T0 review: removed pre-T0 hardening, atomicized asmdef seating, deferred Wave-8 scope, added ERR recording/back-prop inventory and pair-identity locks, removed external locale-file requirement, and re-sequenced to oracle -> T0 -> renderer -> T1 retrofit. |
| 2.1 | 2026-09-06 | — | Closed second external review: canonicalized PR-policy gate wording around owner-held RED, made close-out/cardinality updates explicit, fixed ERR-049-002 to an all-L1 discharge, documented C6 deferred-ERR timing, pinned base content to Localization, omitted the optional T0 dev marker, and made living-world corpus removal a hard T1 exit. |
| 2.2 | 2026-09-08 | — | Rebased plan onto PR-#375 current main; consolidated L3B non-sentinel coverage acceptance and the two-dot frozen-oracle diff into §7.7/§12; froze producer enum rosters in L3A; removed the orphan acceptance supplement. |
| 2.3 | 2026-09-09 | — | L3A landing reconciliation: resolved `<L3A_MERGE_COMMIT>` to the PR #370 merge commit `67f2343` in §7.7 and §12 (naming the merged branch tip as the wrong value and recording the frozen blob hash as informational corroboration), advanced the header baseline to that commit, discharged the v2.2 hold on L1 with the CI and negative-control run ids, and carried forward the narrowed end-to-end citation-clause evidence limitation. No requirement added or relaxed. |
| 2.4 | 2026-09-09 | — | Recorded two further pre-T0 defects found by review of the v2.3 landing: §3.3 proposed ERR-049-004 (FR-LC-009 mandates a plural/gender selector for which §2.2 supplies no typed operand) and §3.4 proposed ERR-049-005 (FR-LC-011's base-locale-identity fallback is circular for a static key outside FR-LC-008a/F5's procedural-roster-only coverage). L0R grows from two recorded entries to four across §3.5, §3.6, §11 row 0 and §14 criterion 6; ERR-049-004 assigned to **L1** because the operand lands on a core seam type frozen there, with §3.4's signature question settled before L1 rather than at L2; executable exit evidence named in §5.5 and §6.6. No normative fix chosen and no FR/KD wording altered. |
| 2.5 | 2026-09-09 | — | Followed the v2.4 four-defect scope through the places it had not reached, and fixed one mis-sliced proof. §5.2 no longer hard-codes the pre-fix `ILocalizer`/`NamedSlotSet`/`LocalizedTextRequest` shapes that §3.3/§3.4 require L1 to change — the three bullets are marked subject to those decisions. ERR-049-004's exit evidence split at the slice boundary: L1 proves the operand exists with the required type/locale-neutrality/immutability/no-persisted-state properties (§5.3, §5.5) and closes the ERR there; L2 proves it drives plural selection (§6.6) as FR-LC-009 conformance, since the renderer and catalogue are L2. §2 H1 and the §3 preamble updated from two known defects to four. No new defect recorded, no normative fix chosen, no FR/KD wording altered. |
#endregion
