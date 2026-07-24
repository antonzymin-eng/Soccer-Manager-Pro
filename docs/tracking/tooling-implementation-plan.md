# Tooling — Implementation Plan

> **Created:** July 23, 2026
> **Status:** PLAN (pre-implementation; no code landed by this document)
> **Purpose:** High-level implementation plan for three independent tooling / follow-up
> items carried in the root `CLAUDE.md` OPEN ISSUES and spec §9.2 carve-outs:
>   1. **#26 KD-6 on-disk preset format** (Stage 1) — the deferred `TacticPreset` disk loader.
>   2. **`[GT]` balance passes for #21 / #23–#26** — pin the illustrative gameplay-tuned magnitudes.
>   3. **`tools/dotnet-ci` shim verification** — confirm the `netstandard2.1` / `LangVersion 9.0`
>      pins against the real Unity 6000.4.9f1 target.
>
> This is a planning supplement in the same governance class as the other
> `docs/tracking/*-design.md` notes — no section files, no spec-registry change. Each
> workstream lands as its own reviewed change per the project's design-first / adversarial-review
> convention.
>
> **Detailed expansion:** `docs/tracking/tooling-implementation-plan-detailed.md` is the file-by-file,
> signature-level, test-by-test implementation plan (injectable-catalogue refactor + loader for WS-1,
> the four lock-suite specs for WS-2, the verbatim shim edit targets for WS-3). This document stays the
> high-level umbrella (scope, sequencing, risks, adversarial-review history); read it first, then the
> detailed note for implementation.

---

## 0. Scope, independence, and sequencing

The three items are **independent** — no ordering dependency between them, so they may land in
any order or in parallel branches. Recommended sequence by cost/leverage:

| Order | Workstream | Rough size | Gating on anything? |
|-------|------------|-----------|---------------------|
| 1 | **WS-3** shim verification | Small (analysis + a doc/comment refresh; possibly one probe test) | No — the authoritative on-disk artifact is already present in the repo (see WS-3) |
| 2 | **WS-1** on-disk preset format | Medium–Large (new loader + tests **plus** a static→injectable refactor of `TacticPresetLibrary` — see §1.3/§1.4) | No |
| 3 | **WS-2** `[GT]` balance passes (#23/#24/#25/#26) | Medium×4 (one lock suite per spec, no new production behaviour) | No (#21 already complete) |

All three preserve the project's hard invariants: **default-behaviour-neutral** (no digest change
unless a version bump is explicitly justified), **fail-loud** parsing/validation, and **no phantom
interfaces**. None of the three is expected to change match behaviour or `SNAPSHOT_SCHEMA_VERSION`
(WS-1's step-0(a) refactor touches `ManagerAdaptation`/`MatchEngine` but is digest-*proven* neutral,
not neutral by omission; WS-2 stays neutral only if it keeps current magnitudes — §2.6).

Each workstream follows the standard cycle: **design note → adversarial review to convergence →
implement → code adversarial review → full `dotnet` gate green → commit.**

---

## WS-1 — #26 KD-6 on-disk preset format (Stage 1)

### 1.1 What exists today

- **The in-code catalogue.** `src/tactical-instructions/TacticPresetLibrary.cs` hardcodes the five
  Appendix A.1 presets (`ParkTheBus 0 / CounterAttack 1 / Balanced 2 / Possession 3 / Gegenpress 4`,
  ordinal = the defensive→attacking `[FIXED]` ladder) as `s_presets`, exposed as
  `IReadOnlyList<TacticPreset> Presets` + `int Count`.
- **A preset value.** `src/tactical-instructions/TacticPreset.cs` — `readonly struct` of
  `{ string Name (authoring metadata only, never serialized); TeamTactic Team; PlayerTactic[] Players
  (null = all-identity, snapshot-copied) }`; ctor fail-loud on null/empty name; `ValidatePlayers`
  is the FR-TP-014 roster gate.
- **The consumption seam.** `src/match-engine/TacticPresetProjection.cs::Project(...)` turns a
  `TacticPreset` into a `TeamTacticConfig` + `PlayerTacticConfig`, which the existing
  `TeamTacticConfigApplier` / `PlayerTacticConfigApplier` stage via `SetTeamTactic` / `SetPlayerTactic`
  before kickoff.
- **The precedent to mirror.** `src/match-engine/TeamTacticFileLoader.cs` and
  `PlayerTacticFileLoader.cs` — line-oriented, case-insensitive `key = value` under `[section]`
  headers, `#` comments, omitted key ⇒ Balanced/Default identity, every unknown key/section /
  unparsable value / duplicate key fail-loud `FormatException`. **The grammar is a human-authoring
  text format, NOT a determinism-pinned wire format** — only the resulting tactic *values* enter the
  digest, never the file text. This is exactly what KD-6 / §7.3 cite as the model.

### 1.2 Requirement anchors (verbatim)

- **FR-TP-002:** "`TacticPresetLibrary` is a static in-code catalogue (Appendix A); Stage 0+1 has no
  disk format (parser-swap deferral)." — MUST, KD-6.
- **FR-TP-017:** "No phantom interfaces: … no disk-loader interface until their prerequisites exist."
  — MUST.
- **KD-6:** "Stage 0+1 authors the catalogue in code; the disk loader is a pure parser swap producing
  the same `TacticPresetLibrary` (the `TeamTacticFileLoader`/`ScenarioIndex` D1 precedent)."
- **§4.4:** "The disk loader (KD-6) will be a parser producing `TacticPresetLibrary` contents — a
  construction-time input swap, not an interface this spec pre-declares."

**Stage gate:** FR-TP-002/017 pin "no disk format at Stage 0+1." This work is a **Stage 1**
deliverable — building it now is the promotion, and the plan should record that Stage-1 transition
explicitly (the spec's §7.3 anticipates it, so it is a deferral being fulfilled, not a spec
violation).

### 1.3 The seam — and why it is NOT a free "input swap"

The spec's §4.4 frames the loader as "a construction-time input swap, not an interface." That is true
for the *projection* half — `TacticPresetProjection.Project(in TacticPreset preset, …)` consumes a
single preset **value** — but it is **false for the selection half**, and the plan must be honest
about that.

`TacticPresetLibrary` is a **`public static class`** (`TacticPresetLibrary.cs:25`) whose catalogue is
consumed by **static reference** at live, determinism-load-bearing sites:

- `ManagerAdaptation.cs:189` / `:247` — `TacticPreset preset = TacticPresetLibrary.Presets[ordinal];`
  (the running manager AI resolves its selected/adapted preset by ordinal against the static library).
- `ManagerAdaptation.cs:44/:63/:85/:92` — `TacticPresetLibrary.Count` (ladder bounds).
- `ManagerAdaptation.cs:49–51` — `TacticalPresetsConstants.BaseFit/AggrAffinity/CautAffinity[presetOrdinal]`,
  three **fixed 5-element `float[]`** indexed by the same preset ordinal (the A.3 kickoff-scoring rows).
- `MatchEngine.cs:1453/:1473` — `TacticPresetLibrary.BalancedOrdinal` / `.Count`.

A disk-loaded catalogue therefore **cannot reach the consumers by producing a list** — the consumers
do not take a list, they read a static singleton. Delivering disk-authored presets to the running
manager requires changing how the catalogue is *sourced*, not just adding a parser. The precedent
(`TeamTacticFileLoader → TeamTacticConfig`) is a **weaker analogy than it looks**: `TeamTacticConfig`
was always an *instance consumed by value*, so its loader genuinely was a construction-time swap;
`TacticPresetLibrary` is a *static catalogue consumed by static reference*, so its loader is not.
**And the A.3 affinity rows are ordinal-parallel to the ladder but live in a fixed-length side-table**
(`TacticalPresetsConstants`, length 5) — so a disk catalogue with `Count ≠ 5` would crash or mis-score
`KickoffScore` unless those rows travel *with* the presets (the detailed note folds them onto
`TacticPreset`; see detailed §1.0/§1.1).

**This static→injectable decision — including moving the affinity rows onto the preset — is the real
WS-1 work; the file grammar is the easy half.**

### 1.4 Proposed implementation

0. **Catalogue-sourcing decision (the load-bearing step — settle FIRST, in the design note).** Choose
   how a disk-loaded catalogue reaches the static consumers in §1.3. Two viable shapes:
   - **(a) Injectable catalogue (recommended).** Refactor `TacticPresetLibrary` into a constructible
     catalogue — an instance (or interface `ITacticPresetCatalogue`) with the in-code presets as the
     default — and thread it through `ManagerAdaptation` and `MatchEngine` (the `Presets[]`/`Count`/
     `BalancedOrdinal` consumers). **`TacticPreset` also gains the three A.3 affinity scalars**
     (`BaseFit`/`AggrAffinity`/`CautAffinity`, seeded from `TacticalPresetsConstants` in the default
     catalogue) so `KickoffScore` reads them off the resolved preset instead of a fixed side-table —
     without this the abstraction is incomplete and a `Count ≠ 5` catalogue crashes (detailed §1.0/§1.1).
     The loader then constructs one from parsed text. This is the honest "construction-time swap," but it
     is a real refactor of a type read at the sites listed in §1.3, **not** a behaviour-neutral no-op —
     it must carry a **faithful-pass-through neutrality proof** (a host-independent catalogue-contents-
     equality test — the default catalogue delivers today's exact preset dials + affinity scalars — plus
     the existing formula/determinism locks; a same-build two-engine comparison is tautological and an
     absolute float-physics digest golden is host-fragile — detailed §1.1).
   - **(b) Offline codegen.** The Stage-1 loader runs at build time and regenerates the `s_presets`
     initializer; the class stays static and runtime consumers are untouched. Simpler, but it is not
     the runtime `[GT]` config-loader FR-CS-019 anticipates, so pick this only if runtime authoring is
     genuinely not required at Stage 1.

   Do not start the parser until this is chosen; the parser's output type falls out of it.
1. **`src/match-engine/TacticPresetFileLoader.cs`** (new) — `static class`,
   `TacticPresetLibraryData Parse(string text)` (or `IReadOnlyList<TacticPreset> Parse(...)`),
   mirroring `TeamTacticFileLoader` exactly:
   - Per-preset `[section]` headers keyed by the preset's stable name/ordinal; a `team.*` key block
     reusing the **same key grammar as `TeamTacticFileLoader`** for the `TeamTactic` fields, and an
     optional `[preset N player M]` (or nested) block reusing `PlayerTacticFileLoader`'s per-agent
     grammar for `Players`. **Reuse the existing key parsers** (extract shared enum/float/bool
     helpers if practical) so the preset file and the team/player files never drift.
   - Omitted dial ⇒ `TeamTactic.Balanced` identity (KD-7), so a minimally-specified preset file
     reproduces the Appendix A.1 rows now hardcoded.
   - Fail-loud `FormatException` on every malformed/unknown/duplicate/out-of-range token, matching
     the precedent's posture.
   - **APPEND-only ordinal contract (FR-TP-013):** the ladder order is `[FIXED]`; the loader must
     preserve/verify ordinal↔name mapping (fail-loud on a re-ordering or a gap), because the ordinal
     is the serialized preset identity (`TacticPresetLibrary.Count` is the F2 restore-seam bound).
2. **A round-trip fixture:** author the five Appendix A.1 presets as a canonical text file, parse it,
   and assert the result **deep-equals the default catalogue** (the "parser swap produces the same
   catalogue" contract). **"Deep-equal" must be specified, not left to `==`:** `TacticPreset` is a
   `readonly struct` with a `PlayerTactic[] Players` reference field and **no** `Equals`/`IEquatable`
   override (`TacticPreset.cs:41`), so default struct equality compares the `Players` array **by
   reference** (two content-equal presets compare *unequal*) and also compares `Name`. The test must
   use an explicit comparator: `Team` field-by-field; `Players` null-or-elementwise-equal; and `Name`
   handled deliberately — since Name is authoring-only (§1.1), either require the file to carry the
   library's names and assert they match, or exclude Name from the comparison and document that the
   file need not reproduce it. The comparator also asserts the three per-preset affinity scalars
   (`BaseFit`/`AggrAffinity`/`CautAffinity`, from step 0(a)) **exactly**. This is the load-bearing
   acceptance test; getting the equality wrong makes it tautological or always-failing. Add a negative
   companion: empty/comment-only input and a section missing a required affinity key each throw
   `FormatException`.
3. **Boot disk-READ wiring is separately optional** — distinct from the sourcing refactor in step 0.
   The WS-1 deliverable is *the sourcing choice (step 0a refactor or 0b codegen) + the loader + its
   tests*; **whether/where the engine boot actually reads a preset file from disk at startup** is a
   further composition decision that can be deferred (the appliers already exist), exactly as
   `TeamTacticFileLoader` landed before any disk-read wiring. Note the "minimal, loader + tests only"
   sizing applies **only** under step 0(b); under the recommended 0(a) the change also includes the
   injectable-catalogue refactor and its digest-neutrality proof (§1.6).

### 1.5 Decisions to settle in the design note

- **D1 — Ordinal↔content stability (the real versioning question), not "format version by
  analogy."** The team-tactic loader needs no version constant because it serializes full tactic
  **values**. Presets are different: `ManagerState.CurrentPresetOrdinal` is serialized, and a running
  or restored match resolves its preset by **ordinal** against the catalogue —
  `TacticPresetLibrary.Presets[ordinal]` at `ManagerAdaptation.cs:189`/`:247`. So the **ordinal→content
  mapping is digest-load-bearing across save/restore**: a disk-authored catalogue that changes what
  preset *N contains* under a fixed ordinal makes a restored match resolve a different tactic and
  diverge — a failure mode the compiled static catalogue structurally cannot have. The grammar text
  itself is still not a digest-pinned wire format (so a literal `PRESET_FORMAT_VERSION` on the *file*
  may be unnecessary, matching `TeamTacticFileLoader`), but the **catalogue's ordinal↔content map is a
  save-compatibility surface** the precedent does not carry. Decide the guard: at minimum a fail-loud
  check that the loaded catalogue's `Count` and ladder ordering match the ordinal contract
  (FR-TP-013 APPEND-only); consider an ordinal-content fingerprint stamped into the save so a restore
  against a changed catalogue fails loud instead of diverging silently. Justify any "no version
  constant" conclusion against *this* coupling, not against the team-tactic loader.
- **D2 — File section grammar.** Exact header shape for presets and their optional per-player blocks.
  Prefer maximal reuse of the two existing loaders' grammars over inventing a third dialect.
- **D3 — Assembly placement.** `src/match-engine/` (alongside the two precedent loaders and
  `TacticPresetProjection`) vs `src/tactical-instructions/`. Recommendation: **`src/match-engine/`**,
  matching the precedent and avoiding a new dependency edge from the tactics assembly. Note this
  interacts with step 0(a): if `TacticPresetLibrary` becomes an injectable catalogue, the catalogue type
  stays in `src/tactical-instructions/` (its consumers `ManagerAdaptation`/`MatchEngine` already
  reference that assembly), while only the file *loader* lives in `src/match-engine/`.

### 1.6 Acceptance

- New `TacticPresetFileLoaderTests`: **empty/comment-only ⇒ fail loud (`FormatException`)** — a preset
  catalogue has no "identity" default (a catalogue is a variable-length ladder, not a single tactic with
  a neutral value; detailed §1.3); the canonical five-preset file round-trips **deep-equal to the default
  catalogue** including the three per-preset affinity scalars (via the explicit comparator specified in
  §1.4 step 2, not struct `==`); every fail-loud gate (empty catalogue, unknown key/section, unparsable
  value, duplicate, ordinal re-order/gap, missing/out-of-range affinity, roster-size mismatch via
  `ValidatePlayers`) throws `FormatException`.
- If step 0(a) is chosen: the injectable-catalogue refactor carries a **faithful-pass-through neutrality
  proof** — a host-independent catalogue-contents-equality test (default catalogue == today's exact
  preset data incl. affinity scalars) plus the existing formula/determinism locks prove byte-identity to
  the static path by construction (a same-build two-engine comparison is tautological and an absolute
  float-physics digest golden is host-fragile; detailed §1.1). The refactor is behaviour-neutral even
  though it touches `ManagerAdaptation`/`MatchEngine`.
- No `SNAPSHOT_SCHEMA_VERSION` change; no behaviour change (loader is not yet wired into boot, and the
  step 0(a) refactor is digest-neutral on the default catalogue).
- Full `dotnet` gate green.
- Spec touch: flip FR-TP-002/§7.3's "Stage 0+1 has no disk format" to record the Stage-1 loader as
  landed (version-history row), leaving the FR text's Stage-0+1 scope intact.

---

## WS-2 — `[GT]` balance passes (#21 / #23 / #24 / #25 / #26)

### 2.1 Status per spec

| Spec | Balance pass | Notes |
|------|--------------|-------|
| **#21** tactical-instructions | **DONE (2026-06-30)** | Pinned + invariant-locked by `BalancePassInvariantsTests`; the #14 `OffsideTrapRequestedDwellTicks` / #15 `OverloadFocusCountBias` leftovers are pinned too. **One nit only:** a stale `Magnitudes illustrative…` XML comment on `RoleWeightModifiers` (`TacticalInstructionsConstants.cs:150`) contradicts the pinned header. |
| **#23** dismarking-ai | **PENDING** | §9.2 carve-out; magnitudes illustrative. |
| **#24** build-up-structures | **PENDING** | §9.2 carve-out; magnitudes illustrative. |
| **#25** positional-rotations | **PENDING** | §9.2 carve-out; magnitudes illustrative. |
| **#26** tactical-presets | **PENDING (own `[GT]`s only)** | Engine-substrate §9.1 gates already CLOSED; preset *contents* reuse #21-pinned values (KD-7), so only #26's own archetype/threshold/interval `[GT]`s need the pass. |

**So #21 needs only the one-line stale-comment fix; #23/#24/#25/#26 need real balance passes.**

### 2.2 What a "balance pass" is here (from #21 §5.6 / the §9.2 texts)

A **numerical-mirror + adversarial review** of the `[GT]` magnitudes, after which the values are
**pinned** (illustrative → committed) and **invariant-locked** in a dedicated test suite. The
reviewed *contract* is the **shapes / directions / gates / identity rows**, not the magnitudes —
so the pass typically **keeps** spec-aligned monotonic values and **adds the lock tests**, changing
a magnitude only where the review finds it off-shape. `#21`'s `BalancePassInvariantsTests`
(`src/tactical-instructions/Tests/`) is the template: it locks (1) exact pinned values, (2) identity
rows exactly neutral (FR-TI-031), (3) strict monotonicity of the scalar tables, (4) bounds +
directional shapes of the jagged role table.

### 2.3 Constant catalogues in scope

> Line numbers below are indicative (region locators from a research sweep, not re-verified
> line-by-line in this plan); confirm them at each spec's design-note time. `TacticalInstructionsConstants.cs:150`
> (WS-2's #21 nit) and `generate_projects.py:100/:102` (WS-3) are the only line refs verified exact here.

- **#23:** `src/positioning-ai/PositioningAIConstants.cs` (dismarking region ~L222–248:
  `MARKING_RADIUS_M`, dwell saturation/decay, pressure floor, max dismark offset, the
  `DISMARK_INTENSITY_SCALAR` table); plus `src/decision-tree/TacticalWeights.cs`
  (`TargetMarkedUtilityMult` `[GT]`; `MarkedPassRadiusM` is a `[CROSS]` mirror, not tuned here).
- **#24:** `PositioningAIConstants.cs` (build-up region ~L250–258: zone hysteresis, overlay bound,
  suppression window); plus the overlay tables in `src/positioning-ai/BuildUpOverlayCatalogue.cs`.
- **#25:** `PositioningAIConstants.cs` (rotations region ~L261–282: trigger margin, freedom scalars,
  dwell, min-hold, per-team commit cap); plus `src/positioning-ai/RotationAdjacencyCatalogue.cs`.
- **#26:** `src/tactical-instructions/TacticalPresetsConstants.cs` (decision interval, hold intervals,
  urgency threshold, goal-diff cap; A.2 archetype columns; A.3 affinity/kickoff rows).

### 2.4 Proposed implementation (per spec, ×4)

For each of #23, #24, #25, #26, one self-contained change:
1. **Numerical-mirror + adversarial review** of that spec's `[GT]` magnitudes against its §3 formula
   shapes / identity rows / monotonicity and `[DERIVED]` bounds (e.g. #25's
   `ROTATION_HOLD_TICKS ≥ line-dwell`, #24's suppression semantics, #23's `DISMARK_INTENSITY_SCALAR`
   identity row = ×1.0). Adjust any value the review finds off-shape; otherwise keep.
2. **Pin the values** — update the catalogue region header comment illustrative → pinned, and align
   the class/consumer docs (mirroring #21's `TacticalInstructionsConstants.cs` reframing).
3. **Add a `BalancePassInvariantsTests`-style lock suite** for that spec, asserting identity-row
   exactness, strict monotonicity of its scalar tables, table bounds/shape, and any `[DERIVED]`
   inequality. This is the bulk of the new code and is the durable deliverable.
4. **Update the spec's §9.2 / §9.6** to record the balance pass DONE (version-history row), the same
   way #21 §5.6 recorded it.

**#21 is a separate one-line change:** fix the stale `RoleWeightModifiers` XML comment
(`TacticalInstructionsConstants.cs:150`) to match the pinned header — doc-only, no value change.

### 2.5 Decisions to settle

- **D4 — Values, kept or re-tuned?** Default posture (per #21 precedent) is **keep** spec-aligned
  monotonic values and lock them; only change on a specific review finding. The design note per spec
  should record the mirror result and any change with its rationale.
- **D5 — Scope of "pinned."** These remain `[GT]` (designer-tunable at Stage 1 config) — "pinned"
  means the *illustrative* caveat is retired and the shape is invariant-locked, not that the value is
  frozen like a `[FIXED]`. Keep the tag `[GT]`.

### 2.6 Acceptance

- Per spec: new lock suite green; catalogue + spec §9.2/§9.6 docs reframed illustrative → pinned;
  no behaviour change unless a re-tune is explicitly recorded (in which case digest neutrality no
  longer holds for that spec and its determinism tests are rebaselined — call this out loudly).
- #21: stale comment fixed; existing `BalancePassInvariantsTests` still green.
- Full `dotnet` gate green.

---

## WS-3 — Verify `tools/dotnet-ci` shim's `netstandard2.1` / `LangVersion 9.0` claims vs. Unity 6

### 3.1 The gap (verbatim, `CLAUDE.md` OPEN ISSUES item (4))

> "authoritative re-verification of the `tools/dotnet-ci` shim's `netstandard2.1` / `LangVersion 9.0`
> claims against a real Unity 6000.4.9f1 install … confirming against an actual install remains an
> engineering task requiring the install, so this item is NOT closed and the shim was not edited."

The Unity-6 bump is otherwise recertified; **item (4) is the sole remaining open sub-item.**

### 3.2 Where the pins live

- `tools/dotnet-ci/generate_projects.py` — production TFM `netstandard2.1` (L100), `LangVersion 9.0`
  emitted into every csproj (L102); rationale comments still literally cite **"Unity 2022.3"**
  (L22–24, L95–99, L134–136).
- `tools/dotnet-ci/UnityShim/UnityShim.csproj` (L14–15) + header still cites the old 2022.3.62f1
  tuple (L6–7).
- `tools/dotnet-ci/UnityShim.TestTools/UnityShim.TestTools.csproj` (L10–11).
- `tools/dotnet-ci/README.md` (L52, L77–78; L85 = the "UNCHANGED and unverified against Unity 6"
  caveat).

### 3.3 Key finding that de-risks this item

The open issue assumes the authoritative artifact is host-generated and absent from this checkout.
**That is now outdated — the artifact is present and populated:**

- `ProjectSettings/ProjectSettings.asset` → `apiCompatibilityLevel: 6`, with
  `apiCompatibilityLevelPerPlatform: {}` empty (no per-platform override, so the global applies). In
  Unity's `ApiCompatibilityLevel` enum, **6 = `NET_Standard` (.NET Standard 2.1)** and **3 =
  `NET_Framework`** — the design note should cite the enum mapping (Unity Scripting API
  `UnityEditor.ApiCompatibilityLevel`) so the `6 → 2.1` inference is auditable. Value 6 is the direct
  on-disk analogue of the `netstandard2.1` TFM pin. **Confirmed from the repo.**
- `scriptingBackend:` has only an `Android: 1` (IL2CPP) override and **no Standalone entry**, so the
  desktop/Standalone build (the Stage-0 Windows pin) uses the **default = Mono**. Matches the
  certification pin.
- `ProjectVersion.txt` → `6000.4.9f1`. Confirmed.

So two of the three claims — **API Compatibility Level = .NET Standard 2.1** and **Mono backend** —
are now verifiable **directly from the committed project**, no live Unity install required.

### 3.4 What genuinely still needs external evidence

- **`LangVersion 9.0` (C# 9).** Unity does not store the C# language version in a project asset — it
  is the default of the Roslyn compiler bundled with the editor (unless a `csc.rsp` /
  `Directory.Build.props` overrides it; **grep confirms this repo has neither**). Authoritative
  evidence = Unity 6000.4.9f1's official "C# compiler" documentation and/or the bundled Roslyn
  version for the Mono/.NET Standard 2.1 profile. This is a **documentation-verification** task
  (WebSearch/WebFetch of Unity 6 docs), not a repo read.
- **BCL reference-assembly surface spot-checks.** The gate's whole point is that .NET 8's BCL is a
  superset of Unity's `netstandard2.1` surface (e.g. the `File.Move(overwrite:)` absence the first
  run found). Authoritative re-verification = compile a probe against Unity 6's shipped
  `Data/NetStandard/` reference assemblies. Requires the install; **corroborating signal already on
  record:** the deterministic-sim assembly compiled and ran under Unity 6 on the pinned host during
  recertification.

### 3.5 Proposed implementation

1. **Repo-verifiable half (do now):** read and record `apiCompatibilityLevel: 6` (.NET Standard 2.1)
   and the Mono-by-default backend from `ProjectSettings.asset`; confirm no `csc.rsp` /
   `Directory.Build.props` `LangVersion` override exists. This alone closes the "against a real
   install" concern for the **API-compatibility-target and backend** claims, since the settings file
   is the Unity-authored artifact. (It does **not** close the BCL reference-assembly *surface*
   equivalence — that is the separate §3.4 spot-check that still needs the install.)
2. **Docs-verifiable half:** confirm Unity 6000.4.9f1's default C# language version is **C# 9** for
   the Mono/.NET Standard 2.1 profile via official Unity 6 documentation (cite the page + bundled
   Roslyn version). Record the citation.
3. **Refresh the stale citations** in `generate_projects.py` (L22–24, L95–99), `UnityShim.csproj`
   (L6–7), and `README.md` so the comments cite **Unity 6000.4.9f1** instead of "Unity 2022.3",
   with a one-line pointer to the evidence (the `ProjectSettings.asset` field + the Unity-6 C#-9
   doc). The **pins themselves stay `netstandard2.1` / `9.0`** — the verification confirms them; it
   does not change them.
4. **Optional hardening:** a tiny guard test/script that reads `apiCompatibilityLevel` from
   `ProjectSettings.asset` and asserts it is `6` (.NET Standard 2.1), so a future accidental switch
   to `.NET Framework` (which would invalidate the `netstandard2.1` pin) fails CI. Decide in the
   design note whether this is worth the coupling.
5. **Close item (4)** in `CLAUDE.md` OPEN ISSUES with the evidence trail, or — if the design note
   concludes the C#-9 doc confirmation is insufficient without a bundled-Roslyn check on the host —
   narrow the open item to *only* the BCL reference-assembly spot-check and mark the rest verified.

### 3.6 Acceptance

- `CLAUDE.md` item (4) either closed with a cited evidence trail, or narrowed to the single genuinely
  host-blocked sub-check (BCL reference-assembly probe), with the API-compat-level and backend claims
  marked verified-from-repo and the C#-9 claim marked verified-from-docs.
- Stale "Unity 2022.3" citations in the four `tools/dotnet-ci` files refreshed to Unity 6000.4.9f1.
- No change to the `netstandard2.1` / `9.0` pins themselves. `dotnet` gate still green.

---

## 4. Cross-cutting notes

- **Design-first.** Each workstream opens its own `docs/tracking/*-design.md` (or folds into an
  existing one) and converges through adversarial review before code, per project convention.
- **Determinism neutrality.** WS-3 is behaviour-neutral by construction. WS-1's *loader + tests* are
  neutral by construction, but its step-0(a) injectable-catalogue refactor touches live consumers and
  must **prove** neutrality by **faithful-pass-through** (§1.6 / detailed §1.1) — a host-independent
  catalogue-contents-equality test plus the existing formula/determinism locks — not by a same-build
  two-engine run (tautological) or a host-fragile absolute digest golden. WS-2 is behaviour-neutral
  **only if** the balance passes keep the current magnitudes (the expected #21-style outcome); any
  re-tune must be flagged and its determinism tests rebaselined.
- **No phantom interfaces.** WS-1's *file loader* is a construction-time input, not a new runtime
  interface (FR-TP-017 / §4.4) — do not pre-declare a disk-read interface the boot path does not yet
  call. (An `ITacticPresetCatalogue` seam from step 0(a) is an internal injection point with a real
  consumer, not a phantom.)
- **Full gate.** Each change ends on a green whole-tree `dotnet test` run (SDK installable via apt in
  this environment, per recent landings).
