# Steam Packaging & Release Engineering #39 — Section 5: Test Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

Test-ID prefixes follow #19 §3.1.4: `T-PK-U-*` unit, `T-PK-I-*` integration, `T-PK-ID-*` identity,
`T-PK-FAIL-*` fail-loud, `T-PK-BOUND-*` structural, `T-PK-HOST-*` host-gated, `T-PK-PROC-*` process
(exercised only against a real release).

**Which of these can be exercised before an artifact exists is itself a finding** (R-5), and §5.7 states
it rather than leaving a reader to discover that half the spec is untested until the first build.

## 5.1 The gate's fail-closed property, tested **as a property** (KD-2)

**This is the single most important suite in the spec**, because it tests the inversion the spec exists to
introduce. It is testable at all only because `EvaluateGate` is a **pure fold** over an evidence set
(FR-PK-020) rather than only a runbook.

| ID | Test |
|---|---|
| T-PK-U-001 | **An empty evidence set ⇒ `Fail`** (§3.6(b)). Catches the loop written the wrong way round: walking the *records* rather than the *required kinds* returns `Pass` for an empty set, which is a green gate on a release where nothing ran. |
| T-PK-U-002 | **A missing row ⇒ `Fail`**, for each of the six kinds individually (F1). Per-kind, so a required row cannot be quietly dropped from the set. |
| T-PK-U-003 | **A `skipped` row ⇒ `Fail`** (F2). **Constructed from the real skip-shaped `unity-tests` output of an unlicensed run** — the exact input §1.4(b) shows CI treats as success. Not a synthetic fixture: the point is that the *real* artifact must fail the gate. |
| T-PK-U-004 | A row that executed but **failed** ⇒ `Fail`. |
| T-PK-U-005 | **A row naming a different commit ⇒ `Fail`** (F3), for every input-side kind. Stale evidence is worse than missing evidence, because it looks like a pass. |
| T-PK-U-006 | The **commit check does not apply** to `PackagedSmokePath`, which binds by construction (it runs on the artifact). Asserted so the rule is not over-applied and the smoke path silently un-runnable. |
| T-PK-U-007 | All six present, affirmative, same commit ⇒ **`Pass`**. The non-vacuity control: without it every test above passes on a gate that always fails. |

## 5.2 The conflict matrix, exhaustively (KD-1)

Over #50's five classes × {local newer, remote newer, divergent}. Appendix C is the authoritative matrix.

| ID | Test |
|---|---|
| T-PK-U-008 | `TooNew` on **either** side ⇒ `NoAction`; **neither copy is touched** (F5). |
| T-PK-U-009 | **No case auto-merges** — asserted over the whole matrix, since a merge path added later would satisfy every individual row. |
| T-PK-U-010 | **No case deletes**, truncates or overwrites an unchosen copy (FR-PK-009). |
| T-PK-U-011 | A `Corrupt` / `Unsupported` copy is **never auto-selected** and never repaired (F6). |
| T-PK-U-012 | Local `Migratable` vs. remote `Current` compares **pre-migration** versions (FR-PK-006) — migration is not a tiebreaker. |
| T-PK-FAIL-001 | **The negative lock the whole matrix depends on:** a resolution attempted with **metadata only** ⇒ `Refuse`, not a guess (F4). Constructed with **an older save carrying a newer timestamp** — the input a *"newer wins"* implementation gets wrong, and the shortcut that would quietly undo every row above. |
| T-PK-I-001 | **A migrated save is not uploaded on load** (FR-PK-008): open a migrated career, close without saving, and the remote copy is byte-identical. A read must not become a write. |
| T-PK-BOUND-001 | **#39 performs no version comparison of its own** (FR-PK-001) — a source-level assertion, since re-deriving one is the two-truths defect the `#39 → #50` reference exists to prevent. |

## 5.3 Cloud sync mechanics (KD-5)

| ID | Test |
|---|---|
| T-PK-I-002 | A save **round-trips through a simulated sync byte-identically** and loads. |
| T-PK-I-003 | **A mid-match save syncs as one unit** (FR-PK-034): world and match together, or neither. There is no state in which one arrives without the other. |
| T-PK-I-004 | A sync **never observes a partial file** — the existing `temp → fsync → rename` guarantee, re-asserted at this new consumer. |
| T-PK-I-005 | Sync with a **write in flight** is deferred to the next quiescent boundary (F7). |
| T-PK-I-006 | A **load is deferred** while sync is replacing the file (FR-PK-036) — the running game owns the file. |
| T-PK-I-007 | A session ending by **crash** falls back to the last completed save (FR-PK-037), which is already the local behaviour — so Cloud adds no new loss mode. |
| T-PK-BOUND-002 | **#39 parses no save bytes** (FR-PK-033): a source-level assertion that it reads no frame field and no sub-blob. |

## 5.4 Achievements (KD-3)

| ID | Test |
|---|---|
| T-PK-ID-001 | **Observer neutrality** (FR-PK-045): a career advanced with achievement evaluation **active** produces a digest chain **byte-identical** to one without — the `MatchViewerTests` lock extended. Asserted with evaluation **on**, since neutrality with the feature off proves nothing. |
| T-PK-I-008 | **Progress lands in no sim save** (FR-PK-026): the season save frame is byte-identical with achievements active. |
| T-PK-BOUND-003 | **No sim assembly reads achievement state** (FR-PK-027 / F9) — a **reverse**-reference scan, because this prohibition runs *toward* #39 and the reference graph's outward direction cannot see it. **The one #39 violation that is a determinism defect rather than a packaging bug.** |
| T-PK-I-009 | **The offline path:** an unlock earned with no connection flushes **exactly once** on reconnect (F8). |
| T-PK-I-010 | **An already-held platform unlock is not re-granted** (FR-PK-023/025) — the platform-wins half, which a queue-only test would miss. |
| T-PK-U-013 | `AchievementId` **ordinal stability** (FR-PK-028). A **player-visible** identity held by the **platform**, so a renumber re-points trophies already in players' accounts — with no version gate anywhere, because the platform's copy has none. |
| T-PK-BOUND-004 | **#39 adds no event, hook or sim field** (FR-PK-021) — asserted structurally, which is what makes T-PK-ID-001 true by construction rather than by measurement. |

## 5.5 Structural and identity locks

| ID | Test |
|---|---|
| T-PK-BOUND-005 | **#39 references only `TacticalDirector.SaveMigration`** — the `.asmdef` scan. In particular it references **no** sim, season or career assembly (§4.1). |
| T-PK-BOUND-006 | **No sim or loop assembly references #39** — the FR-UI-001 reverse-reference scan extended. |
| T-PK-BOUND-007 | **#39 emits no display string** (FR-PK-010): a source-level assertion over `src/release/`. |
| T-PK-BOUND-008 | **#39 registers no RNG stream and allocates no ordinal** (FR-PK-041): a full career with Cloud and achievements active leaves every registered stream's cursor byte-identical. |
| T-PK-ID-002 | **The identity** (FR-PK-044): **Cloud disabled + no achievements ⇒ save behaviour byte-for-byte today's local path.** Every existing save test still describes the shipped behaviour. |
| T-PK-ID-003 | `CloudSyncState` **caches no remote version** — asserted over the type, because caching one is the most natural optimisation in the spec and it silently reintroduces metadata-based resolution (F4 / §4.5). |

## 5.6 Host-gated

| ID | Test |
|---|---|
| T-PK-HOST-001 | The determinism KAT reproduces the **certified digests** from the pinned commit on the pinned tuple (FR-PK-039). |
| T-PK-HOST-002 | The FR-PO-052 baseline is **CERTIFIED**, not `PENDING`. |
| T-PK-HOST-003 | Unity EditMode + PlayMode **execute** and produce artifacts with `Executed == true`. |

## 5.7 What cannot be tested before an artifact exists (R-5)

**Stated plainly, because a gate never exercised is a document.**

| Class | Status |
|---|---|
| §5.1 gate folding, §5.2 conflict policy, §5.4 achievement contracts, §5.5 structural locks | **Exercisable now** — pure functions and reference scans |
| §5.3 sync mechanics | **Exercisable now against a simulated sync**; the real Steam path is not |
| §5.6 host-gated | Exercisable **on the pinned host**, which exists (§1.4(c)) |
| `T-PK-PROC-001` — **the packaged-build smoke path** | **Not exercisable until the first player build exists** |
| `T-PK-PROC-002` — the end-to-end runbook, from commit to committed evidence record | **Not exercisable until a real release is attempted** |

**The two `T-PK-PROC-*` rows are the artifact-side half of KD-2**, and they are precisely the half that
cannot be simulated: the smoke path exists to catch a **packaging-only** failure — a stripped assembly, a
missing asset, a broken path — that every project-side test in this plan passes straight through. Their
coverage is a genuine decision rather than a formality (R-1a), and each step of the smoke path should be
justified individually rather than expanded by habit into a second test suite maintained in the worst
possible environment.

## 5.8 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `achievements-are-observer-neutral`, owning specs `{16, 19, 30, 39}`,
registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`:

advance a career through a full season **twice from one seed** — once plain, once with achievement
evaluation active and unlocks accumulating — and assert the two digest chains are **byte-identical**, the
season save frames are **byte-identical**, and every registered RNG cursor is unchanged.

This is the composition-level proof of the one #39 property whose violation would be a **determinism
defect** rather than a packaging bug (F9). The conflict policy and the gate are deliberately **not** in a
`ScenarioRunner` scenario: neither touches the simulation, and putting them there would assert a
relationship that does not exist.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §5. §5.1 leads with the gate's fail-closed property, testable only because `EvaluateGate` is a pure fold, and T-PK-U-003 is constructed from the **real** skip-shaped CI artifact rather than a synthetic fixture. T-PK-U-007 is the non-vacuity control without which every other gate test passes on an always-failing gate. T-PK-FAIL-001 is named as the negative lock the whole conflict matrix depends on. T-PK-BOUND-003 is a **reverse**-reference scan, because the achievement prohibition runs toward #39 and an outward scan cannot see it. §5.7 states which classes cannot be exercised before an artifact exists, since R-5's risk is that a gate never exercised is a document. Status IN REVIEW. |
#endregion
