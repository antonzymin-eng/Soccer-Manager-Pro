# Steam Packaging & Release Engineering #39 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## 2.1 Functional requirements

**Cloud conflict policy (KD-1)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-PK-001 | #39 MUST obtain every version judgement from **#50** (`Classify`, `CompareForConflict`) and MUST NOT compare version numbers itself. | MUST | KD-1 |
| FR-PK-002 | Conflict resolution MUST run **only after both copies are local**: the remote copy is fetched to a staging path and classified from its **bytes**. | MUST | KD-1 |
| FR-PK-003 | Resolving on **timestamp or size alone MUST be refused**, not guessed. A metadata-only decision is the *"newer wins"* heuristic every conflict rule exists to reject. | MUST | KD-1 |
| FR-PK-004 | If **either** side classifies `TooNew`, this build MUST resolve nothing: surface the conflict and **touch neither copy**. | MUST | KD-1 |
| FR-PK-005 | Two `Current` copies that differ MUST be resolved by the **player**. #39 MUST NOT merge, ever. | MUST | KD-1 |
| FR-PK-006 | A local `Migratable` against a remote `Current` MUST be resolved on the **pre-migration** versions. Migration MUST NOT act as a tiebreaker. | MUST | KD-1 |
| FR-PK-007 | A `Corrupt` or `Unsupported` copy MUST never be auto-selected and MUST never be deleted. | MUST | KD-1 |
| FR-PK-008 | A migrated save MUST be uploaded **only on the player's next explicit save**, never on load. **A read MUST NOT become a write.** | MUST | KD-1 |
| FR-PK-009 | No conflict outcome MUST delete, truncate or overwrite a save the player has not chosen. | MUST | KD-1 |
| FR-PK-010 | Conflict and refusal text MUST route through **#49** as an identity + slots. #39 MUST bake no display string. | MUST | KD-1 |

**The release gate (KD-2)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-PK-011 | The gate MUST be **fail-closed on positive evidence**: each required artifact MUST be **present and affirmative**, and **absence MUST fail**. | MUST | KD-2 |
| FR-PK-012 | A **skipped** check MUST NOT count as a pass. The gate MUST read a job's **artifact**, never its summary status. | MUST | KD-2 |
| FR-PK-013 | Every evidence record MUST name the **commit** it describes. | MUST | KD-2 |
| FR-PK-014 | Input-side evidence MUST be admissible **only** when its commit equals the commit the artifact was built from. A mismatch MUST fail. | MUST | KD-2 |
| FR-PK-015 | The gate MUST require **artifact-side** evidence: a packaged-build smoke path (launch → career → save → quit → relaunch → load → advance a day) run on the **shipped player**. | MUST | KD-2 |
| FR-PK-016 | The spec MUST state that five of the six evidence rows measure the **project at a commit**, not the packaged binary. | MUST | KD-2 |
| FR-PK-017 | The certifying evidence MUST come from the **pinned host** per `certification-platform.md`. Sourcing it from the Linux gate MUST be treated as a fabricated certification. | MUST | KD-2 |
| FR-PK-018 | The gate MUST be specified as a **runbook** producing committed, reviewable evidence records — the `cert-run-runbook.md` descendant. | MUST | KD-2 |
| FR-PK-019 | #39 MUST define **no new determinism proof**. It consumes #16's and #18's. | MUST | KD-2 |
| FR-PK-020 | The gate MUST be evaluable as a **pure function of an evidence set**, so its fail-closed property is testable without a release. | MUST | KD-2 |

**Achievements (KD-3)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-PK-021 | An achievement MUST be a **read-only predicate** over events the career already emits. #39 MUST add no event, no hook and no sim field. | MUST | KD-3 |
| FR-PK-022 | Predicate **evaluation MUST live in the client shell**, not in #39's assembly. | MUST | KD-3 |
| FR-PK-023 | **The platform is the store of record** for unlock state. On any disagreement about whether something is unlocked, **the platform wins**. | MUST | KD-3 |
| FR-PK-024 | The local store MUST be a **pending-unlock queue plus cross-session counters** — never an unlock ledger. | MUST | KD-3 |
| FR-PK-025 | A pending unlock MUST flush **exactly once** on reconnect, and an already-held platform unlock MUST NOT be re-granted. | MUST | KD-3 |
| FR-PK-026 | Achievement state MUST NOT be a save sub-blob, MUST NOT enter any determinism-gated save, and MUST NOT appear in #50's version registry. | MUST | KD-3 |
| FR-PK-027 | **No sim assembly MUST read achievement state.** A sim branching on unlock state would make replay depend on account state that is not in the save. | MUST | KD-3 |
| FR-PK-028 | `AchievementId` MUST carry **APPEND-only ordinal stability** — a shipped achievement's identity is player-visible and cannot be renumbered. | MUST | KD-3 |

**The checklist (KD-4)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-PK-029 | Every checklist item MUST carry exactly one **gate class**: `Compliance` (blocks) or `Marketing` (does not). | MUST | KD-4 |
| FR-PK-030 | The `Compliance` set MUST include the age-rating declaration, the third-party licence/attribution manifest, EULA + privacy text localized through #49, the Cloud configuration matching KD-5's file set, the crash-reporting path, and the KD-2 evidence set. | MUST | KD-4 |
| FR-PK-031 | A `Marketing` item MUST NOT be able to block a release. | MUST | KD-4 |
| FR-PK-032 | #39 MUST specify the checklist and its gate classes, and MUST NOT specify the **assets**. | MUST | KD-4 |

**Cloud sync mechanics (KD-5)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-PK-033 | Sync MUST be **whole-file only**. #39 MUST NOT parse the save frame or any sub-blob. | MUST | KD-5 |
| FR-PK-034 | A mid-match save MUST sync **as one unit or not at all**. #39 MUST NOT create a state in which the world syncs and the match does not. | MUST | KD-5 |
| FR-PK-035 | Sync MUST occur at **quiescent boundaries** — save completion and clean exit — not continuously mid-session. | MUST | KD-5 |
| FR-PK-036 | The **running game owns its save file**: the game MUST NOT load a file that sync is replacing, and sync MUST NOT run with a write in flight. | MUST | KD-5 |
| FR-PK-037 | A session ending by crash MUST fall back to the last completed save — which is already the local behaviour. | MUST | KD-5 |

**Reproducibility (KD-6)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-PK-038 | #39 MUST NOT gate on a **bit-identical** rebuild. | MUST | KD-6 |
| FR-PK-039 | #39 MUST require **behavioural** reproducibility: the pinned commit built on the pinned tuple **reproduces the certified digests**. | MUST | KD-6 |
| FR-PK-040 | The spec MUST state that FR-PK-039 is measured **project-side**, and reaches the artifact only via FR-PK-014's commit binding plus FR-PK-015's smoke path. | MUST | KD-6 |

**Determinism and identity (KD-7)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-PK-041 | #39 MUST register **no** RNG stream, allocate **no** domain tag or `SubsystemOrdinal`, and take **no `_RESERVED_` placeholder** — #16 is untouched, and #39 has **nothing to promote later**. | MUST | KD-7 |
| FR-PK-042 | #39 MUST serialize nothing into any sim save and MUST bump no format version. | MUST | KD-7 |
| FR-PK-043 | #39's two client-local stores MUST live **outside** every determinism-gated save (the #38 / #49 / #51 settings class). | MUST | KD-7 |
| FR-PK-044 | With **Cloud disabled and no achievements defined**, save behaviour MUST be byte-for-byte today's local path. | MUST | KD-7 |
| FR-PK-045 | Achievement evaluation MUST be **observer-neutral**: a career advanced with evaluation active MUST produce a digest chain byte-identical to one without. | MUST | KD-3/KD-7 |

## 2.2 Data structures

```csharp
// APPEND-only (FR-PK-028). A shipped achievement's identity is PLAYER-VISIBLE and is held by the
// platform, so a renumber re-points trophies that are already in players' accounts -- with no
// version gate anywhere, because the platform's copy has none.
public enum AchievementId : int { None = 0, /* appended per release */ }

// #39's policy verdict over #50's classification. NEVER serialized.
public enum ConflictOutcome : int
{
    NoAction = 0,      // TooNew on either side -- touch neither copy (FR-PK-004)
    UseLocal,
    UseRemote,
    AskPlayer,         // two Current copies that differ (FR-PK-005)
    Refuse,            // metadata-only input, or Corrupt/Unsupported (FR-PK-003/007)
}

// The evidence set the gate folds. Each row names its commit (FR-PK-013).
public readonly struct EvidenceRecord
{
    public readonly EvidenceKind Kind;
    public readonly string       CommitSha;      // compared to the artifact's (FR-PK-014)
    public readonly bool         Executed;       // FR-PK-012: a SKIP is `false`, not `true`
    public readonly bool         Passed;
}

public enum EvidenceKind : int
{
    DeterminismKat = 0, PerfBaselineCertified, UnityEditModePlayMode,
    DotnetCiSuite, PackagedSmokePath, ComplianceChecklist,
}

// The gate's verdict. A pure function of an evidence set (FR-PK-020), so the fail-closed
// property is testable without a release.
public enum GateVerdict : int { Pass = 0, Fail }

// Client-local, OUTSIDE every determinism-gated save (FR-PK-043). A QUEUE AND COUNTERS --
// not an unlock ledger, because the platform is the store of record (FR-PK-023/024).
public readonly struct AchievementProgress
{
    // pendingUnlocks : set<AchievementId>        -- offline queue, flushed exactly once
    // counters       : map<AchievementId, int>   -- cross-session predicate state
}

// Client-local, DIAGNOSTIC ONLY. It deliberately caches NO remote version: a cached version is
// a decision input that can be stale, and FR-PK-002 requires classification from fetched bytes
// every time.
public readonly struct CloudSyncState
{
    public readonly long LastSyncedAt;
    public readonly int  LastSyncedSaveId;
}

// #39 defines the contract; the SHELL evaluates it against the event surface (FR-PK-022).
public readonly struct AchievementDefinition
{
    public readonly AchievementId Id;
    // predicate identity + threshold; #39 holds no sim type
}
```

**`EvidenceRecord.Executed` is a separate field from `Passed`, and that separation is the whole spec in
one line.** A skipped `unity-tests` job has nothing red about it; without an explicit `Executed` flag the
natural encoding is a single "did it pass" boolean, which a skip satisfies. §1.4(b) is exactly this defect
in the CI configuration, and a data structure that could not express the difference would reproduce it.

**Types #39 consumes but does not declare:**

| Type | Owner | #39's use |
|---|---|---|
| `SaveClass`, `CompareForConflict` | **#50** | The **only** source of version judgement (FR-PK-001) |
| The save file, as bytes | #30 + sub-blob owners | **Transported, never parsed** (FR-PK-033) |
| The career event surface | the sim | Read **by the shell**, never by #39 (FR-PK-022) |
| The Steam API | platform | Bound **by the shell** — #39 declares policy, not calls |

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| **F1** | An evidence record is **missing**. | **Gate FAILS** (FR-PK-011). An unanswered question is a no. |
| **F2** | An evidence record is present but `Executed == false` (a **skip**). | **Gate FAILS** (FR-PK-012). The single most important failure mode in the spec: it is the exact input CI treats as success. |
| **F3** | An evidence record names a **different commit** than the artifact. | **Gate FAILS** (FR-PK-014). Worse than a missing record, because it *looks* like a pass (R-1). |
| **F4** | A conflict resolution attempted with **metadata only**. | **Refuse** (FR-PK-003) — never guess. Constructed test: an older save with a newer timestamp, which is the input a "newer wins" implementation gets wrong. |
| **F5** | Either side classifies `TooNew`. | **No action** (FR-PK-004) — surface it, touch neither copy. |
| **F6** | A `Corrupt` or `Unsupported` copy in a conflict. | **Never auto-selected, never deleted** (FR-PK-007). |
| **F7** | Sync attempted with a write in flight, or a load attempted on a file sync is replacing. | **Deferred to the next quiescent boundary** (FR-PK-035/036) — the running game owns the file. |
| **F8** | A pending unlock flushed **twice**, or an already-held unlock re-granted. | **Barred** (FR-PK-025). The classic double-write the platform-is-truth rule exists to prevent. |
| **F9** | Any sim read of achievement state. | **Barred** (FR-PK-027) and asserted structurally. This is the one #39 failure that is a **determinism defect** rather than a packaging bug. |

**Deliberately not a failure mode: a clean refusal in the wild.** Two machines on different builds is the
**normal** Cloud case, not an edge case (R-4), so `Unsupported` will occur in the field and a
non-destructive refusal is a **success path** — the outcome the policy is designed to produce, not an
error to be engineered away.

**Deliberately not a failure mode: Cloud disabled.** That is the minimal tier's normal state (FR-PK-044)
and is exactly today's behaviour.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §2 (FR-PK-001..045, data structures, F1..F6) from supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **FR-PK-020** — the gate was described only as a runbook, which would have made its fail-closed property untestable except by attempting a release; requiring it to be **a pure function of an evidence set** is what lets §5's most important test exist at all. **M:** added **FR-PK-009 / F7** — nothing barred a conflict outcome from deleting or overwriting a copy the player had not chosen, and nothing said what happens when sync and a save collide; both are the concrete ways a second writer loses a career, which is the hazard KD-5 exists for. **M:** added **FR-PK-025 / F8** — KD-3 makes the platform the store of record but nothing stated the flush-exactly-once obligation, and the double-grant/lost-unlock pair is precisely the defect a queue-plus-platform design must be specified against. **L:** wrote out `AchievementId`, `ConflictOutcome`, `EvidenceRecord`, `EvidenceKind`, `GateVerdict`, `AchievementProgress`, `CloudSyncState` and `AchievementDefinition`, each annotated with the constraint that shapes it — in particular that **`Executed` is a separate field from `Passed`**, since a single "did it pass" boolean is satisfied by a skip and would reproduce, in the data model, the exact CI defect §1.4(b) identifies; and recorded the two *"not a failure mode"* notes, the first because a clean refusal in the wild is a **success path** rather than something to engineer away. |
#endregion
