# Steam Packaging & Release Engineering #39 — Section 4: Architecture

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.Release`** at `src/release/`, referencing **`TacticalDirector.SaveMigration`
(#50) and nothing else**.

```
shell → {#39, #50, #49, #38, sim}        #39 → {#50}        #50 → { }        sim → { }
```

**Acyclic, and #39 is a leaf but for #50.** It holds policy over **classifications** and **evidence
artifacts**, never over domain types: achievement predicates are evaluated **by the shell** against the
event surface (KD-3), and the Steam API binding is the shell's.

**Had #39 evaluated achievements itself it would reference the career and season assemblies — and
`season-save` reaches `MatchEngine` and `LivingWorld`, so the packaging spec would transitively depend on
the whole simulation.** The inversion is the same one #48 uses for `ICueSink` and #50 uses for its
registered generators, and it is load-bearing in exactly the same way. **This is the third such inversion
in this wave**, which is a sign it is the project's convention for cross-layer joins rather than a
one-off.

**The `#39 → #50` reference is the one #39 must take**, and taking it is what prevents the worse
architecture: a #39 that compared version numbers itself would be a **second version authority**, and the
project has a name for that failure (the two-truths class). #50 already names #39 as its consumer
(§1.4(e)), so this edge is a specified contract rather than a new coupling.

## 4.2 File layout

```
src/release/
├── ReleaseConstants.cs         # the Appendix A catalogue — no magic numbers in formula code
├── AchievementId.cs            # APPEND-only — player-visible and held by the PLATFORM
├── AchievementDefinition.cs    # the predicate contract; the SHELL evaluates it
├── AchievementProgress.cs      # a QUEUE + counters — never an unlock ledger
├── CloudSyncState.cs           # diagnostic only; caches NO remote version
├── ConflictOutcome.cs          # #39's policy verdict over #50's classification
├── ConflictPolicy.cs           # FM-PK-02 — pure; every version answer comes from #50
├── EvidenceRecord.cs           # `Executed` is SEPARATE from `Passed` (§2.2)
├── EvidenceKind.cs
├── ReleaseGate.cs              # FM-PK-01 — a PURE FOLD, so fail-closed is testable
├── ICloudSyncPolicy.cs         # #39 defines the policy; the SHELL binds the Steam API
└── tests/
```

**Plus two process artifacts that are not code**, and are as much a part of this spec as the assembly:

```
docs/specs/steam-packaging-release/
├── release-runbook.md          # the cert-run-runbook.md descendant (FR-PK-018)
└── release-runs/               # committed, reviewable evidence records
```

**No Steam API binding lives in this tree.** `ICloudSyncPolicy` is a policy declaration; the shell holds
the SDK. That keeps #39 buildable and testable **without the platform**, which matters because the gate's
own tests must run long before a store page exists.

**No achievement evaluator lives here either** (FR-PK-022). #39 declares the identity set and the
predicate contract; the shell wires them to the event surface — the reason §4.1's leaf claim holds.

**No save parsing code lives here** (FR-PK-033). #39 moves whole files and asks #50 about them.

**CS0104 pre-check.** #39 introduces `AchievementId`, `AchievementDefinition`, `AchievementProgress`,
`CloudSyncState`, `ConflictOutcome`, `ConflictPolicy`, `EvidenceRecord`, `EvidenceKind`, `ReleaseGate`,
`ICloudSyncPolicy`. Each was checked against every name that could be in scope with it before authoring,
because this project has hit CS0104 twice (`TacticTranslation`, `PlayerAttributes`). **`ConflictOutcome`
is the one worth naming**: it is deliberately not `SaveClass`, `SaveStatus` or `ConflictClass`, all of
which read as #50 types — and #39 holding a type that *looks* like a classification is precisely the
confusion FR-PK-001 exists to prevent.

## 4.3 The release gate as a process

```
1. Pick the release commit.
2. On the PINNED HOST (certification-platform.md v1.4):
     - run the determinism KAT                    -> cert-runs/ record
     - confirm the FR-PO-052 baseline is CERTIFIED (not PENDING)
     - run Unity EditMode + PlayMode              -> artifacts, with Executed = true
     - run tools/dotnet-ci/run-gate.sh            -> suite green, quarantine empty
3. Build the player from that commit.
4. Run the PACKAGED-BUILD SMOKE PATH on the artifact.
5. Complete the Compliance subset of the checklist.
6. EvaluateGate(records, artifactCommit)  ->  Pass | Fail        # FM-PK-01
7. Commit the evidence records to release-runs/.
```

**Steps 2–5 produce evidence; step 6 is a pure function of it** (FR-PK-020). That split is what makes the
gate's central property testable without attempting a release, and it is why `ReleaseGate.cs` is code
while the runbook is a document.

**The gate does not run in CI, and this is permanent rather than transitional** (R-2). Its certifying
inputs are pinned-host-only, and `cert-run-runbook.md` states in its own words that a number sourced from
the Linux gate *"would be a fabricated certification."* **Any future proposal to "just run the cert in CI"
is a proposal to stop certifying.**

**Step 4 is the only step that touches the artifact**, and steps 2's four rows all measure the **project
at a commit** (KD-2). Step 3's position between them is what makes step 6's commit check meaningful: the
evidence is gathered before the build, from the same commit, and the check is what binds the two.

## 4.4 The Cloud path

```
# CLIENT SHELL — holds the Steam SDK and calls #39's policy
class SteamCloudBinding
{
    void OnConflictDetected(remoteHandle)
        => Apply(ConflictPolicy.Resolve(localPath, remoteHandle));   // FM-PK-02
    void OnQuiescent(reason)
        => syncPolicy.OnQuiescentBoundary(reason);                   // FM-PK-04
}
```

**#39 defines the policy; the shell binds the platform** (FR-PK-022 pattern applied to sync). The whole
Cloud surface reduces, on #39's side, to two pure decisions: *which copy* and *when to upload*.

**Cloud is the project's first second writer**, and that is the architectural fact this section exists to
record (R-3). Every save path in the tree is local, single-writer and atomic-by-rename; Cloud can replace
that file underneath a running process, and can deliver a save written by a **different build**. The
policy's non-destructive rules (FR-PK-007/009) are what keep the resulting failures **recoverable** —
most save bugs that reach players will arrive through this path, and they will look like corruption while
being conflict mishandling.

## 4.5 State and persistence

**#39 adds nothing to any sim save** (FR-PK-042): no sub-blob, no format version, no restore path, and no
#50 registry row.

Its two **client-local** stores live outside every determinism-gated save (FR-PK-043), in the #38 / #49 /
#51 settings class:

| Store | Contents | Why it is not a ledger / not a cache |
|---|---|---|
| `AchievementProgress` | pending unlocks + cross-session counters | **The platform owns unlock truth** (FR-PK-023). Holding "what is held" locally is the double-grant/lost-unlock defect |
| `CloudSyncState` | last synced id + timestamp | **Diagnostic only.** It deliberately caches **no remote version**, because a cached version is a decision input that can be stale — and FR-PK-002 requires classification from the fetched bytes every time |

**The second row's parenthetical is the load-bearing part.** A cached remote version is the most natural
optimisation in the whole spec and it silently reintroduces metadata-based resolution (F4), which every
rule in KD-1 exists to reject.

## 4.6 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **#50** | **The only version authority** (FR-PK-001). #39 consumes `Classify` and `CompareForConflict` and inherits the `TooNew` and write-back-on-explicit-save rules verbatim. |
| **#30 / the codecs** | **Untouched.** #39 transports the file and parses nothing (FR-PK-033). |
| **#16 / #18 / `certification-platform.md`** | **Consumed as evidence** (FR-PK-019). #39 defines no determinism proof and re-pins nothing. |
| **#49** | #39 is an ordinary producer: conflict and refusal notices as identity + slots. |
| **#38** | Hosts the client-local settings store the two #39 stores live in. |
| **the sim** | **Unreachable, in both directions.** #39 references no sim assembly, and no sim assembly may read achievement state (FR-PK-027). |
| **the client shell** | Evaluates achievement predicates, binds the Steam SDK, and applies #39's policy verdicts. |
| **#52** (Stage 5+) | The tier at which bit-identical packaging becomes materially valuable (KD-6). |

**Standing review item:** #39's isolation rests on one property a reference graph cannot prove — that **no
sim assembly reads achievement state** (F9). The graph shows #39 references almost nothing, but the
prohibition runs the *other* way, and the natural violation ("show a cosmetic in-game reward for an
unlock") looks harmless and is a replay-determinism defect. §5.5 asserts it as a reverse-reference scan.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §4 (a leaf-but-for-#50 assembly, with the argument that the single reference **prevents** the worse architecture of a second version authority; file layout including the two **process artifacts** — runbook and committed evidence records — that are as much this spec as the code; the release gate written out as a seven-step process whose step 6 is a pure function of steps 2–5, which is what makes the fail-closed property testable; the Cloud path, recording that Cloud is the project's **first second writer**; state and persistence, with the deliberately-uncached remote version called out as the most natural optimisation in the spec and the one that silently reintroduces metadata-based resolution; a standing review item on the one prohibition that runs *toward* #39 rather than away). Status IN REVIEW. |
#endregion
