# Tooling — Detailed Implementation Plan

> **Created:** July 24, 2026
> **Status:** PLAN (pre-implementation; no code landed by this document)
> **Supersedes the detail level of:** `docs/tracking/tooling-implementation-plan.md` (the high-level
> umbrella). That note frames the three workstreams and their sequencing/risks; this note is the
> file-by-file, signature-level, test-by-test expansion. Read the umbrella first for scope and the
> adversarial-review history that shaped WS-1.
> **Purpose:** Implementation-ready plans for the three carried-forward tooling items:
>   1. **WS-1 — #26 KD-6 on-disk preset format** (Stage 1) — injectable catalogue + `TacticPresetFileLoader`.
>   2. **WS-2 — `[GT]` balance passes** for #23 / #24 / #25 / #26 (#21 already complete bar one nit).
>   3. **WS-3 — `tools/dotnet-ci` shim** `netstandard2.1` / `LangVersion 9.0` verification vs Unity 6.
>
> Each workstream is independent (any order; the umbrella recommends WS-3 → WS-1 → WS-2 by cost).
> Each lands as its own branch/PR through the standard cycle: **design note (this) → adversarial
> review to convergence → implement → code adversarial review → full `dotnet` gate green → commit.**
> Line numbers below are current-as-of authoring; re-confirm at implementation time.

---

## WS-1 — #26 KD-6 on-disk preset format (Stage 1)

### 1.0 Why this is Medium–Large, not a "parser swap"

`TacticPresetLibrary` is a **`public static class`** (`src/tactical-instructions/TacticPresetLibrary.cs:25`)
read by **static reference** at live, determinism-load-bearing sites:

| Consumer | Site | Reads |
|----------|------|-------|
| `ManagerAdaptation.KickoffScore` | `ManagerAdaptation.cs:44` | `TacticPresetLibrary.Count` |
| `ManagerAdaptation.SelectKickoffPreset` | `:63` | `.Count` |
| `ManagerAdaptation.StepToward` | `:85`, `:92` | `.Count` |
| `ManagerAdaptation.RunDecisionPoint` | `:189` | `.Presets[target]` |
| `ManagerAdaptation.ApplyKickoff` | `:247` | `.Presets[selected]` |
| `MatchEngine` (ManagerState seed) | `MatchEngine.cs:1453` | `.BalancedOrdinal` |
| `MatchEngine` (ordinal bound guard) | `:1473` | `.Count` |

A disk-loaded catalogue cannot reach these by "producing a list" — the consumers read a static
singleton. And the ordinal is **serialized**: `ManagerState.CurrentPresetOrdinal` is persisted, and a
running/restored match resolves its preset via `TacticPresetLibrary.Presets[ordinal]`
(`ManagerAdaptation.cs:189`). So the **ordinal→content mapping is a save-compatibility surface** the
compiled static catalogue structurally cannot violate but a disk file can. Both facts drive the design.

### 1.1 Chosen shape (recommended): injectable catalogue behind an interface

Introduce a catalogue abstraction, keep the in-code presets as the default implementation, and thread
it through the two consumers. The **default path stays byte-identical** (the default catalogue reads
exactly today's `s_presets`), proven by a digest-equality test.

**New file — `src/tactical-instructions/ITacticPresetCatalogue.cs`:**
```csharp
public interface ITacticPresetCatalogue
{
    IReadOnlyList<TacticPreset> Presets { get; }
    int Count { get; }
    byte BalancedOrdinal { get; }
}
```

**New file — `src/tactical-instructions/InCodeTacticPresetCatalogue.cs`:** a sealed
`ITacticPresetCatalogue` whose members return the existing `TacticPresetLibrary.Presets` / `.Count` /
`.BalancedOrdinal`. This is the default; it wraps the unchanged static data so the refactor moves *how
consumers reach the catalogue*, not the catalogue contents.

- Keep `TacticPresetLibrary` (static) as the data source `InCodeTacticPresetCatalogue` reads — do NOT
  delete it. Minimizes the diff and gives the default catalogue a provably-unchanged backing store.
- `TacticPresetLibrary.BalancedOrdinal` / `Count` / `Presets` already exist (used at the sites above),
  so the wrapper is a thin pass-through.

**Modify `ManagerAdaptation.cs` (static class):** its five static methods take an
`ITacticPresetCatalogue catalogue` parameter (last param, before the existing trailing params where
natural) instead of touching `TacticPresetLibrary` directly:
- `KickoffScore(in ManagerProfile, int presetOrdinal, ITacticPresetCatalogue catalogue)`
- `SelectKickoffPreset(in ManagerProfile, ITacticPresetCatalogue catalogue)`
- `StepToward(bool moreAttacking, byte current, ITacticPresetCatalogue catalogue)`
- `EvaluateLadder(in ManagerProfile, byte current, int goalDiff, long ticksRemaining, long matchTicksTotal, ITacticPresetCatalogue catalogue)`
- `RunDecisionPoint(MatchEngine engine, int teamId, ref ManagerState, int tick, int goalDiff, long ticksRemaining, long matchTicksTotal, ITacticPresetCatalogue catalogue)`
- `ApplyKickoff(MatchEngine engine, ITacticPresetCatalogue catalogue, TeamTacticConfig teamBaseline = null, PlayerTacticConfig playerBaseline = null)`

Each `TacticPresetLibrary.X` reference becomes `catalogue.X`. `KickoffScore`'s
`TacticalPresetsConstants.BaseFit/AggrAffinity/CautAffinity[presetOrdinal]` stays as-is (those tables
are #26's own `[GT]`s, ordinal-indexed against the same ladder — see the ordinal-stability guard in
§1.4).

**Modify `MatchEngine.cs`:** hold `private readonly ITacticPresetCatalogue _presetCatalogue`
(defaults to `new InCodeTacticPresetCatalogue()` at boot, or a new optional ctor param for injection);
replace the two static reads at `:1453` / `:1473` with `_presetCatalogue.*`; pass `_presetCatalogue`
into every `ManagerAdaptation.*` call site (there is a `RunManagerDecisionPoints` internal wrapper and
the boot `ApplyKickoff` path — both in `MatchEngine`). `ConfigureManager` / `SeedManagerKickoff` are
where the ordinal/BalancedOrdinal seeding lives.

**Behaviour-neutrality obligation (KD gate):** a match run with the default `InCodeTacticPresetCatalogue`
must be **byte-identical** to the current static path. Prove it with a digest-equality test
(`ManagerAITests` / `MatchEngineTacticTests` already have two-run digest infrastructure): boot two
engines identically (one pre-refactor conceptually — locked by the existing determinism tests staying
green — one on the refactor), run an AI-managed match, assert identical `CurrentSnapshotDigest` chains.
No `SNAPSHOT_SCHEMA_VERSION` change (the refactor changes which object supplies boot-constant records,
not the serialized surface).

**Alternative (only if runtime authoring is genuinely not needed at Stage 1): offline codegen.** A
build-time tool regenerates `TacticPresetLibrary.s_presets` from a text file; the static class and all
consumers stay untouched. Simpler, but it is not the runtime `[GT]` loader FR-CS-019 anticipates — pick
this only with an explicit decision recorded here. The rest of §1 assumes the injectable shape.

### 1.2 The loader

**New file — `src/match-engine/TacticPresetFileLoader.cs`** (placed in `match-engine/` alongside the
two precedent loaders and `TacticPresetProjection`; the *catalogue type* stays in
`tactical-instructions/`, so this is a `match-engine`→`tactical-instructions` reference, already
present):

```csharp
public static class TacticPresetFileLoader
{
    // Returns a ready-to-inject catalogue; the ladder ordinal is the array index.
    public static ITacticPresetCatalogue Parse(string text);
}
```

**Grammar (mirror `TeamTacticFileLoader` exactly — line-oriented, case-insensitive, `#` comments,
fail-loud `FormatException`):**
- One `[preset N]` section per ladder ordinal, `N` = `0..Count-1` ascending (the pinned defensive→
  attacking ladder `ParkTheBus 0 … Gegenpress 4`). A `name = <string>` key inside the section carries
  the authoring name.
- The team dials reuse the **exact key set of `TeamTacticFileLoader`** (every `TeamTactic` field:
  `mentality`, `tempo`, `width`, `defensiveWidth`, `lineOfEngagement`, `pressing`, `passing`,
  `transitionWon`, `transitionLost`, `focusPlay`, `offsideTrap`, `timeWasting`, `markingOrientation`,
  `dismarkIntensity`, `buildUpStructure`, `rotationFreedom`, `defensiveLine`, …). An omitted key
  inherits the `TeamTactic.Balanced` identity (KD-7), so a minimally-specified preset reproduces the
  Appendix A.1 rows now in `TacticPresetLibrary.Compose(...)`.
- Optional per-player block for `TacticPreset.Players` (Stage-0 presets set `Players = null`, so this
  can be **deferred**: a preset section with no player block ⇒ `Players = null`, matching the current
  catalogue; document the deferral rather than inventing an unused per-player grammar).
- **Reuse, do not re-implement, the parse helpers.** Extract `TeamTacticFileLoader`'s
  `ParseEnum`/`ParseFloat`/`ParseBool`/`ParseTimeWasting`/`StripComment`/section-dispatch into a shared
  internal helper (e.g. `TacticFileGrammar`) that both `TeamTacticFileLoader` and
  `TacticPresetFileLoader` call, so the preset file and the team file can never drift. If extraction is
  too invasive for one pass, at minimum the preset loader calls the same public/internal helpers — never
  a second hand-rolled enum parser.

**Fail-loud gates (each throws `FormatException`, matching the precedent):** unknown key / unknown or
malformed section header / missing `=` / empty key / duplicate key within a section / duplicate section
/ unparsable enum/float/bool/byte value / out-of-range `timeWasting` / leftover unconsumed keys.

### 1.3 Ordinal↔content stability (the real "versioning")

The preset ordinal is serialized (`ManagerState.CurrentPresetOrdinal`) and looked up at runtime/restore.
So the loader MUST guarantee the loaded catalogue's ordinal→content map is consistent with the ladder
contract (FR-TP-013, APPEND-only):

- **`Count` and section coverage:** sections must be exactly `[preset 0]`…`[preset Count-1]` with no
  gap and no re-ordering; fail loud otherwise. The parser fills an array by index, so a missing/extra
  ordinal is caught structurally.
- **Ladder monotonicity is a property of the values, not enforceable from the file alone** — the file
  author *is* the ladder. Decide (record the choice in this note during review): either (a) trust the
  file's order as authoritative (simplest; the ordinal is whatever the file says), or (b) add a
  save-compat guard — stamp an *ordinal-content fingerprint* (a hash over the loaded presets' dials) so
  a restore against a **changed** catalogue fails loud instead of diverging silently. **Recommendation:**
  do NOT add a `PRESET_FORMAT_VERSION` on the file (the grammar is not a digest-pinned wire format,
  matching `TeamTacticFileLoader`), but DO consider option (b)'s fingerprint if/when a disk-loaded
  catalogue can feed a savable match — that is the genuine coupling, and it is orthogonal to the file
  grammar. At the loader-only scope (§1.5 sequencing), a `Count`/coverage guard is sufficient; the
  fingerprint lands with the boot-wiring that first lets a loaded catalogue reach `ManagerState`.

### 1.4 Acceptance test — `deep-equal`, not `==`

`TacticPreset` is a `readonly struct` with a `PlayerTactic[] Players` field and **no** `Equals` /
`IEquatable` override (`TacticPreset.cs:41`). Default `ValueType.Equals` compares `Players` **by
reference** (two content-equal presets compare *unequal*) and also compares `Name`. The round-trip test
must therefore use an explicit comparator, NOT struct `==`:

- Author the five Appendix A.1 presets as a canonical text file (the loader's own fixture).
- Parse it → `ITacticPresetCatalogue actual`.
- Assert `actual` **deep-equals** the default `InCodeTacticPresetCatalogue`:
  - same `Count` and `BalancedOrdinal`;
  - for each ordinal: `Team` compared **field-by-field** (every `TeamTactic` dial); `Players`
    compared **null-or-elementwise** (both null, or same length and each `PlayerTactic` field-equal);
    `Name` handled deliberately — either require the file to carry the library's names and assert
    equality, or exclude `Name` and document that the file need not reproduce it.
- Getting this wrong makes the test tautological or always-failing — it is the load-bearing acceptance
  criterion.

### 1.5 File inventory & sequencing

**New:** `ITacticPresetCatalogue.cs`, `InCodeTacticPresetCatalogue.cs` (tactical-instructions);
`TacticPresetFileLoader.cs` (match-engine); `tests/TacticPresetFileLoaderTests.cs`,
`tests/InCodeTacticPresetCatalogueTests.cs` (deep-equal helper lives here or in a shared test util).
**Modified:** `ManagerAdaptation.cs` (signatures), `MatchEngine.cs` (field + call sites), possibly
`TacticFileGrammar` extraction from `TeamTacticFileLoader.cs`.

**Order within the workstream:**
1. Settle §1.1 shape (injectable vs codegen) in review.
2. Land the **refactor alone** (interface + default catalogue + threaded consumers) with the
   digest-equality neutrality proof — no loader yet, no behaviour change. This is a self-contained,
   reviewable, behaviour-neutral change.
3. Land the **loader + deep-equal round-trip test** on top.
4. **Boot disk-READ wiring is separately optional/deferred** — whether the engine boot reads a preset
   file at startup is a further composition decision (the `TacticPresetProjection` + appliers already
   exist). The ordinal-content fingerprint (§1.3 option b) lands with that wiring, not before.

### 1.6 Acceptance (whole workstream)
- Refactor: default catalogue ⇒ byte-identical match (digest-equality test green); no
  `SNAPSHOT_SCHEMA_VERSION` change; existing `ManagerAITests`/`MatchEngineTacticTests` still green.
- Loader: empty/comment-only ⇒ the identity/Balanced catalogue; canonical five-preset file
  round-trips **deep-equal** to the default catalogue; every fail-loud gate throws `FormatException`.
- Full `dotnet` gate green (SDK 8.0.129 via apt, per recent landings).
- Spec touch: record the Stage-1 loader landing in #26 §7.3 / FR-TP-002 version-history, leaving the
  FR text's Stage-0+1 scope intact.

---

## WS-2 — `[GT]` balance passes (#21 / #23 / #24 / #25 / #26)

### 2.0 Status recap

| Spec | State | Action |
|------|-------|--------|
| **#21** tactical-instructions | **DONE** — pinned + `BalancePassInvariantsTests`-locked (incl. #14/#15 leftovers) | One-line doc fix only (§2.1) |
| **#23** dismarking-ai | PENDING (§9.2 carve-out) | Full pass (§2.2) |
| **#24** build-up-structures | PENDING | Full pass |
| **#25** positional-rotations | PENDING | Full pass |
| **#26** tactical-presets | PENDING (own `[GT]`s only; §9.1 engine gates already CLOSED) | Full pass (own archetype/threshold/interval `[GT]`s) |

### 2.1 #21 — the one-line nit
`src/tactical-instructions/TacticalInstructionsConstants.cs:150` still reads
`/// Magnitudes illustrative; directions are the reviewable contract.` on `RoleWeightModifiers`, which
contradicts the pinned header (`:31`, `:172` record the pass DONE / illustrative→pinned). Change the
comment to match the pinned status (doc-only, no value change); `BalancePassInvariantsTests` stays
green. Land this as a trivial standalone commit or fold into whichever #23–#26 PR touches the file.

### 2.2 What a balance pass IS here (the per-spec procedure, ×4)

Per #21 §5.6 and each spec's §9.2, a balance pass is a **numerical-mirror + adversarial review** of the
`[GT]` magnitudes, after which values are **pinned** (illustrative caveat retired) and **invariant-
locked** in a dedicated test suite mirroring `src/tactical-instructions/Tests/BalancePassInvariantsTests.cs`.
The reviewed *contract* is the **shapes / directions / gates / identity rows**, not the magnitudes — so
the default posture (per #21) is **keep** spec-aligned monotonic values and add the locks; change a
value only on a specific review finding, recorded with rationale (D4). "Pinned" retires the illustrative
caveat and adds the invariant lock — the tag stays `[GT]` (designer-tunable at Stage 1); it does not
freeze the value like a `[FIXED]` (D5).

**`BalancePassInvariantsTests` is the template** — it locks: (1) exact pinned scalar-table values;
(2) identity rows exactly neutral (FR-TI-031 — the neutral enum member = ×1.0 / 0 offset); (3) strict
monotonicity of ordered scalar tables; (4) bounds + directional shapes of jagged role tables. Each new
suite reproduces that structure for its spec's tables.

**Per-spec steps (identical shape ×4):**
1. **Numerical-mirror + adversarial review** of that spec's `[GT]` magnitudes against its §3 formula
   shapes / identity rows / monotonicity / `[DERIVED]` inequalities. Adjust off-shape values, else keep.
2. **Pin** — flip the catalogue region header comment `illustrative → pinned`; align class/consumer docs
   (mirroring #21's `TacticalInstructionsConstants.cs` reframing).
3. **Add a `BalancePassInvariantsTests`-style lock suite** asserting identity-row exactness, strict
   monotonicity of scalar tables, table bounds/shape, and any `[DERIVED]` inequality. This is the bulk
   of the new code and the durable deliverable.
4. **Update the spec's §9.2 / §9.6** to record the pass DONE (version-history row), as #21 §5.6 did.

### 2.3 Per-spec targets and invariants

> Region line-locators below are indicative (from a research sweep, not re-verified line-by-line);
> confirm at each spec's design-note time. New lock suites live under `src/positioning-ai/Tests/`
> (#23/#24/#25) and `src/tactical-instructions/Tests/` (#26), matching where the constants live.

**#23 dismarking** — `src/positioning-ai/PositioningAIConstants.cs` (dismarking region ~L222–248):
`MARKING_RADIUS_M`, dwell saturation/decay heartbeats, marking-pressure floor, max dismark offset (m),
and the `DISMARK_INTENSITY_SCALAR` table (indexed by `DismarkIntensity`); plus
`src/decision-tree/TacticalWeights.cs` `TargetMarkedUtilityMult` (`MarkedPassRadiusM` is a `[CROSS]`
mirror of `MARKING_RADIUS_M`, not tuned here). **Lock:** `DISMARK_INTENSITY_SCALAR[Off]` (the zero-value
identity) == exactly 1.0; table monotonic in intensity; `TargetMarkedUtilityMult` in its shape band;
offsets/floors within documented bounds.

**#24 build-up** — `PositioningAIConstants.cs` (build-up region ~L250–258): zone-boundary hysteresis
(m), overlay-offset bound (m), post-regain suppression window (heartbeats); plus the overlay tables in
`src/positioning-ai/BuildUpOverlayCatalogue.cs`. **Lock:** the `None`/default structure ⇒ zero overlay
offset (identity); overlay offsets within the componentwise bound; suppression window semantics
(non-negative, ≤ documented cap).

**#25 rotations** — `PositioningAIConstants.cs` (rotations region ~L261–282): base trigger advantage
margin (m), per-`RotationFreedom` advantage scalars (Conservative / Free), dwell heartbeats, minimum-hold
heartbeats, per-team commit cap; plus `src/positioning-ai/RotationAdjacencyCatalogue.cs`. **Lock:** the
`[DERIVED]` non-interference inequality (`ROTATION_HOLD_TICKS ≥ line-dwell`); freedom scalars ordered
(Conservative ≤ 1 ≤ Free or per the spec's direction); commit cap ≥ 1; `Off` freedom ⇒ no rotation
(identity, if modelled as a scalar).

**#26 presets** — `src/tactical-instructions/TacticalPresetsConstants.cs`: `MANAGER_DECISION_INTERVAL`,
hold intervals, urgency ladder-step threshold, goal-diff cap, and the A.2 archetype columns
(Aggression / Caution / PatienceIntervals) + A.3 affinity/kickoff rows. **Note:** preset *contents*
(`TacticPresetLibrary`) reuse #21-pinned values (KD-7) — they need shape/reference review only, not
re-tuning. **Lock:** affinity/kickoff rows bounded per FR-TP-020 ([−1,+1]); interval/threshold/cap
positive and within documented ranges; the B.1/B.2 worked examples (already unit-locked in
`ManagerAITests`) still hold against any pinned value.

### 2.4 Sequencing & acceptance
- Four independent per-spec commits (any order) + the #21 one-liner. No cross-dependency.
- Behaviour-neutral **only if** each pass keeps current magnitudes (the expected #21-style outcome). If a
  review re-tunes a value, that spec's digest is no longer neutral — flag it loudly and rebaseline that
  spec's determinism tests. Call this out in the PR.
- Acceptance per spec: new lock suite green; catalogue + §9.2/§9.6 docs reframed illustrative→pinned;
  full `dotnet` gate green. #21: comment fixed, existing suite green.

---

## WS-3 — Verify `tools/dotnet-ci` shim `netstandard2.1` / `LangVersion 9.0` vs Unity 6

### 3.0 The gap (CLAUDE.md OPEN ISSUES item (4))
Item (4) is the sole remaining open sub-item of the Unity-6 bump: "authoritative re-verification of the
`tools/dotnet-ci` shim's `netstandard2.1` / `LangVersion 9.0` claims against a real Unity 6000.4.9f1
install." The pins themselves are almost certainly correct — this workstream **confirms and re-cites
them**, it does not change them.

### 3.1 The key de-risking fact (repo-verifiable now)
The open issue assumes the authoritative artifact is host-only. It is not — `ProjectSettings/ProjectSettings.asset`
is present and populated in the checkout:
- `:928` → `apiCompatibilityLevel: 6`. In Unity's `ApiCompatibilityLevel` enum, **6 = `NET_Standard`
  (.NET Standard 2.1)**, **3 = `NET_Framework`** (cite the enum mapping in the closure note so `6→2.1`
  is auditable).
- `:850` → `apiCompatibilityLevelPerPlatform: {}` (empty ⇒ the global applies).
- `:836–837` → `scriptingBackend:` has only `Android: 1` (IL2CPP); **no Standalone entry ⇒ default =
  Mono** for the desktop/Windows pin.
- `ProjectVersion.txt` → `6000.4.9f1`.

So **API-compatibility-target (.NET Standard 2.1)** and **Mono backend** are verifiable **directly from
the committed project**. What genuinely still needs external evidence: (a) the C# 9 default (docs), and
(b) the BCL reference-assembly *surface* equivalence (needs the install — see §3.5).

### 3.2 Verification steps
1. **Repo read (do now):** record `apiCompatibilityLevel: 6` (.NET Standard 2.1), the empty per-platform
   map, and the Mono-by-default backend from `ProjectSettings.asset`. Confirm **no `LangVersion` override
   exists** — grep for `csc.rsp` / `Directory.Build.props` anywhere in the tree (current result: none).
   This closes the "against a real install" concern for the **API-compatibility-target and backend**
   claims (the settings file is the Unity-authored artifact). It does **not** close the BCL reference-
   assembly *surface* equivalence — that is the separate §3.5 item.
2. **Docs read:** confirm Unity 6000.4.9f1's default C# language version is **C# 9** for the Mono /
   .NET Standard 2.1 profile via official Unity 6 documentation (the "C# compiler" / Roslyn page and/or
   the bundled Roslyn version). Cite the page + version in the closure note.

### 3.3 Edits — refresh the stale "Unity 2022.3" citations
The pins stay `netstandard2.1` / `9.0`; only the **citations** move to Unity 6. Exact targets (verbatim
current text → replacement intent):

**`tools/dotnet-ci/generate_projects.py`:**
- `:11` `the pinned Windows 11 / Unity 2022.3.62f1 / Mono tuple` → `Unity 6000.4.9f1`.
- `:22–24` docstring: `LangVersion is pinned to 9.0 (Unity 2022.3 C# level) …` → `(Unity 6000.4.9f1 C#
  level; Mono / .NET Standard 2.1 profile defaults to C# 9)`.
- `:95` `Production assemblies compile against netstandard2.1 — Unity 2022.3's actual BCL surface —` →
  `Unity 6000.4.9f1's .NET Standard 2.1 BCL surface (ProjectSettings.asset apiCompatibilityLevel: 6)`.
- `:134` `Unity 2022.3 bundles System.Runtime.CompilerServices.Unsafe.dll;` → `Unity 6000.4.9f1 bundles …`.
- Pin lines `:100` (`tfm = "net8.0" if is_test else "netstandard2.1"`) and `:102` (`<LangVersion>9.0</…>`)
  **unchanged**.

**`tools/dotnet-ci/UnityShim/UnityShim.csproj`:**
- `:6–8` header `the pinned Windows 11 / Unity 2022.3.62f1 / Mono tuple …` → `Unity 6000.4.9f1`.
- `:14` `<TargetFramework>netstandard2.1</…>` and `:15` `<LangVersion>9.0</…>` **unchanged**.

**`tools/dotnet-ci/UnityShim.TestTools/UnityShim.TestTools.csproj`:** `:10–11` **unchanged** (no
`2022.3` citation in this file).

**`tools/dotnet-ci/README.md`:**
- `:52` layout-table row: `LangVersion 9.0 (Unity 2022.3 C# level)` → `(Unity 6000.4.9f1 C# level)` and
  `netstandard2.1 (Unity's BCL surface)` → `(Unity 6000.4.9f1 .NET Standard 2.1 BCL surface)`.
- `:85` Version-History v1.1 caveat currently states the claims are "UNCHANGED and unverified against
  Unity 6" — add a new Version-History row recording this verification (repo-confirmed API-compat +
  backend; docs-confirmed C# 9; BCL-surface spot-check tracked per §3.5), rather than editing the
  historical v1.1 row (project convention: append, don't rewrite history).
- `:77–78` (`netstandard2.1 surface wins`) stays — it's a design rule, not a version citation.

### 3.4 Optional hardening — a repo guard for the API-compat level
Add a small check so a future accidental switch to `.NET Framework` (which would invalidate the
`netstandard2.1` pin) fails CI. `tools/dotnet-ci/` has **no** existing test harness (only
`generate_projects.py` + the two shims), and `run-gate.sh` is inline shell that calls exactly one Python
entry point (`:18`, `generate_projects.py`). Hook shape:
- New standalone script `tools/dotnet-ci/verify-project-settings.py`: read
  `$ROOT/ProjectSettings/ProjectSettings.asset`, assert `apiCompatibilityLevel: 6`; exit non-zero with a
  clear message otherwise.
- Invoke it from `run-gate.sh` **between the variable setup (~L15) and the first generate step (~L17)**
  (`ROOT` is defined at `:13`, so `$ROOT/ProjectSettings/ProjectSettings.asset` is reachable), e.g.
  `echo "── Verify Unity API compat level ──"; python3 "$ROOT/tools/dotnet-ci/verify-project-settings.py"`.
- Decide in review whether the coupling is worth it (it protects a real invariant but adds a repo-shape
  dependency to the gate). If declined, record the decision; the verification (§3.2) still stands on the
  repo read + docs.

### 3.5 Close item (4)
Rewrite the `CLAUDE.md` OPEN ISSUES item (4) to one of:
- **Closed** with the evidence trail (ProjectSettings.asset field + enum mapping + Unity-6 C#-9 doc
  citation + the refreshed shim citations + optional guard), **if** review accepts the docs-level C#-9
  confirmation as sufficient; or
- **Narrowed** to the single genuinely host-blocked sub-check — a BCL reference-assembly *surface* probe
  against Unity 6's shipped `Data/NetStandard/` assemblies (needs the pinned host) — with the
  API-compat-level and backend claims marked **verified-from-repo** and the C#-9 claim **verified-from-
  docs**. Corroborating signal already on record: the deterministic-sim assembly compiled and ran under
  Unity 6 on the pinned host during the July-19 recert.

### 3.6 File inventory & acceptance
**Modified:** `generate_projects.py` (comments only), `UnityShim.csproj` (header only), `README.md`
(row + new history row), `CLAUDE.md` (item 4). **New (optional):** `verify-project-settings.py` + one
`run-gate.sh` line. **Acceptance:** no change to the `netstandard2.1` / `9.0` pins; `dotnet` gate still
green (and the optional guard passes against the committed `ProjectSettings.asset`); item (4) closed or
narrowed with a cited evidence trail.

---

## Cross-cutting

- **Design-first.** Each workstream opens/uses its own review cycle before code (this note is the shared
  design basis; a workstream may add a focused sub-note if review surfaces open decisions — e.g. WS-1
  §1.1 shape, §1.3 fingerprint).
- **Determinism.** WS-3 is behaviour-neutral by construction. WS-1's *loader + tests* are neutral by
  construction, but the §1.1 injectable refactor touches `ManagerAdaptation`/`MatchEngine` and must
  **prove** neutrality with a digest-equality run. WS-2 stays neutral only if it keeps current
  magnitudes; any re-tune is flagged and rebaselined.
- **No phantom interfaces.** WS-1's *file loader* is a construction-time input, not a runtime interface
  (FR-TP-017); an `ITacticPresetCatalogue` from §1.1 is an internal injection point with real consumers,
  not a phantom.
- **Full gate.** Each change ends on a green whole-tree `dotnet test` run.
