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

---

## 0. Scope, independence, and sequencing

The three items are **independent** — no ordering dependency between them, so they may land in
any order or in parallel branches. Recommended sequence by cost/leverage:

| Order | Workstream | Rough size | Gating on anything? |
|-------|------------|-----------|---------------------|
| 1 | **WS-3** shim verification | Small (analysis + a doc/comment refresh; possibly one probe test) | No — the authoritative on-disk artifact is already present in the repo (see WS-3) |
| 2 | **WS-1** on-disk preset format | Medium (one new loader + tests, mirrors an existing precedent) | No |
| 3 | **WS-2** `[GT]` balance passes (#23/#24/#25/#26) | Medium×4 (one lock suite per spec, no new production behaviour) | No (#21 already complete) |

All three preserve the project's hard invariants: **default-behaviour-neutral** (no digest change
unless a version bump is explicitly justified), **fail-loud** parsing/validation, and **no phantom
interfaces**. None of the three changes match behaviour or the `SNAPSHOT_SCHEMA_VERSION`.

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

### 1.3 The seam

Nothing downstream of `TacticPreset` values changes. The loader produces the same
`IReadOnlyList<TacticPreset>` / `TacticPreset[]` the static catalogue exposes;
`TacticPresetProjection.Project` and the two appliers consume it unchanged.

### 1.4 Proposed implementation

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
   and assert the result is **value-equal to `TacticPresetLibrary.Presets`** (the "parser swap
   produces the same catalogue" contract). This is the load-bearing acceptance test.
3. **Wiring is optional and out of scope for the loader itself** — the loader is a construction-time
   input; whether/where the engine boot reads a preset file from disk is a separate composition
   decision (the appliers already exist). Keep this change to *the loader + its tests* to stay
   minimal, exactly as `TeamTacticFileLoader` landed before any disk-read wiring.

### 1.5 Decisions to settle in the design note

- **D1 — Format-version constant?** The precedent (`TeamTacticFileLoader`) has **none**, precisely
  because the grammar is not a determinism-pinned wire format and §7.3 frames this as the same
  contract. **Recommendation: no `PRESET_FORMAT_VERSION` constant** — matching the precedent. (A
  `[FIXED] *_FORMAT_VERSION` is only introduced for binary/digest-pinned formats, e.g.
  `MATCH_SAVE_FORMAT_VERSION`, `SEASON_SAVE_FORMAT_VERSION`.) Record the rationale so a future
  reviewer does not "add the missing version."
- **D2 — File section grammar.** Exact header shape for presets and their optional per-player blocks.
  Prefer maximal reuse of the two existing loaders' grammars over inventing a third dialect.
- **D3 — Assembly placement.** `src/match-engine/` (alongside the two precedent loaders and
  `TacticPresetProjection`) vs `src/tactical-instructions/`. Recommendation: **`src/match-engine/`**,
  matching the precedent and avoiding a new dependency edge from the tactics assembly.

### 1.6 Acceptance

- New `TacticPresetFileLoaderTests`: empty/comment-only ⇒ the identity/Balanced catalogue; the
  canonical five-preset file round-trips **value-equal to `TacticPresetLibrary.Presets`**; every
  fail-loud gate (unknown key/section, unparsable value, duplicate, ordinal re-order/gap,
  roster-size mismatch via `ValidatePlayers`) throws `FormatException`.
- No `SNAPSHOT_SCHEMA_VERSION` change; no behaviour change (loader is not yet wired into boot).
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

- `ProjectSettings/ProjectSettings.asset` → `apiCompatibilityLevel: 6`. In Unity's
  `ApiCompatibilityLevel` enum, **6 = `.NET_Standard` (2.1)** — the direct on-disk analogue of the
  `netstandard2.1` TFM pin. **Confirmed from the repo.**
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
   install" concern for the **TFM/BCL-target and backend** claims, since the settings file is the
   Unity-authored artifact.
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
- **Determinism neutrality.** WS-1 and WS-3 are behaviour-neutral by construction. WS-2 is
  behaviour-neutral **only if** the balance passes keep the current magnitudes (the expected #21-style
  outcome); any re-tune must be flagged and its determinism tests rebaselined.
- **No phantom interfaces.** WS-1's loader is a construction-time input swap, not a new interface
  (FR-TP-017 / §4.4) — do not pre-declare a disk-read interface the boot path does not yet call.
- **Full gate.** Each change ends on a green whole-tree `dotnet test` run (SDK installable via apt in
  this environment, per recent landings).
