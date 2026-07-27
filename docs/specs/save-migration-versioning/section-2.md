# Save Migration & Versioning #50 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## 2.1 Functional requirements

**Classification (KD-1)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MG-001 | `SaveVersionClassifier` MUST read **only version fields** — the frame's leading version and each sub-blob's leading version — and MUST NOT parse any blob body. | MUST | KD-1 |
| FR-MG-002 | Classification MUST yield exactly one of `Current`, `Migratable`, `TooNew`, `Unsupported`, `Corrupt` (Appendix D). | MUST | KD-1 |
| FR-MG-003 | #50 MUST NOT modify, relax, bypass or replace any codec's existing fail-loud gate. A migrated blob MUST be handed to the **current, unmodified** codec and MUST pass its gates like any other input. | MUST | KD-1 |
| FR-MG-004 | A save MUST classify `Migratable` **only** on an exact, registered version match at **every** level. Anything unrecognised MUST be refused. | MUST | KD-1 |
| FR-MG-005 | `TooNew` MUST be a **distinct class** from `Corrupt`, carrying a distinct refusal identity, even though both refuse. | MUST | KD-1 |
| FR-MG-006 | A version field that is unreadable, out of range, or not a known value for its blob MUST classify `Corrupt` — never `Migratable`. | MUST | KD-1 |
| FR-MG-007 | Classification MUST NOT open, lock or write the save file beyond the read needed for the version fields. | MUST | KD-1 |
| FR-MG-008 | Where a save carries multiple blob versions, the **aggregate** class MUST be the most severe of the per-blob classes: any `Corrupt` ⇒ `Corrupt`; else any `TooNew` ⇒ `TooNew`; else any `Unsupported` ⇒ `Unsupported`; else any `Migratable` ⇒ `Migratable`; else `Current`. | MUST | KD-1 |

**Generation version (KD-2)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MG-009 | A `WORLD_GENERATION_VERSION` `[FIXED]` MUST be stamped into the save at genesis, inside a `SaveOriginStamp` in the **outer frame** (ERR-030-019). | MUST | KD-2 |
| FR-MG-010 | The stamp MUST live in the **frame**, not inside any sub-blob, so FR-MG-001's classifier can read it **without parsing a blob body**. | MUST | KD-2 |
| FR-MG-011 | `WORLD_GENERATION_VERSION` MUST cover **everything regenerated rather than persisted**: `RosterGenerator`'s draw order and per-player field budget, `LeagueBootstrap`'s club-name catalogue and strength ramp, and every derived-on-read table (#32 knowledge bands, #36 nationality derivation and weighting). | MUST | KD-2 |
| FR-MG-012 | On load, a **stamp equal** to the build's MUST proceed with regeneration unchanged. | MUST | KD-2 |
| FR-MG-013 | A **strictly older** stamp with a registered generation migration MUST **materialise** the affected data into the save and stamp the current version. It MUST NOT regenerate silently. | MUST | KD-2 |
| FR-MG-014 | A **strictly older** stamp with **no** registered generation migration MUST **refuse** (`Unsupported`). | MUST | KD-2 |
| FR-MG-015 | A generation migration MUST run the **frozen generator of the save's own version**, retained in `GenerationRegistry` back to the supported floor. `Materialise` MUST be unreachable when that version's generator is absent. | MUST | KD-2 |
| FR-MG-016 | Retained old generators MUST be **frozen** and covered by their own golden vector: they are save-format code, and editing one is as breaking as editing a codec. | MUST | KD-2 |

**Per-blob chains (KD-3)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MG-017 | A migration step MUST be registered for exactly one `(blobKind, fromVersion)` and MUST produce **`fromVersion + 1` of that blob only**. | MUST | KD-3 |
| FR-MG-018 | A step MUST be **written and owned by the spec that made the bump**, not by #50. #50 owns the registry and the runner. | MUST | KD-3 |
| FR-MG-019 | Blobs whose version is current MUST run **zero** steps and MUST be copied **byte-untouched**. | MUST | KD-3 |
| FR-MG-020 | The **frame** MUST be classified and migrated **before** its sub-blobs, since it determines the sub-blob inventory. | MUST | KD-3 |
| FR-MG-021 | A step MUST NOT read or write any blob other than its own. | MUST | KD-3 |
| FR-MG-022 | A **build-time check** MUST assert that every version between the supported floor and current has a registered step for every blob kind. A bump that forgets a step MUST fail the build, not the player's load. | MUST | KD-3 |

**Refusal and the write discipline (KD-4)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MG-023 | A refused save file MUST be left **byte-identical** — never modified, never deleted, never renamed. | MUST | KD-4 |
| FR-MG-024 | #50 MUST NOT migrate in place. A successful migration MUST write a **new file** via `temp → fsync → rename`, retaining the original until the new file has been written **and re-read successfully**. | MUST | KD-4 |
| FR-MG-025 | A migration that fails at any step MUST leave the original intact and MUST NOT leave a partially-written file in the save location. | MUST | KD-4 |
| FR-MG-026 | Refusal messages MUST route through **#49** as identity + slots (FR-LC-002/004). #50 MUST bake no display string. | MUST | KD-4 |
| FR-MG-027 | Each refusal class MUST carry its **own** intent. Collapsing them MUST NOT happen. | MUST | KD-4 |
| FR-MG-028 | The version numbers in a refusal MUST be **slot values**, never formatted into a message by #50. | MUST | KD-4 |

**Cloud conflict (KD-5)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MG-029 | #50 MUST expose a comparison over two saves giving their classifications and relative version ordering. **#39 owns the resulting UX**; #50 owns no interaction. | MUST | KD-5 |
| FR-MG-030 | A save classifying `TooNew` MUST be reported as **"this build must not touch it"**, never as "the newer copy wins". | MUST | KD-5 |
| FR-MG-031 | A migrated save MUST be written back **only on the player's next explicit save**, never silently on load. | MUST | KD-5 |

**Promise, determinism and identity (KD-6 / KD-7)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MG-032 | Migration MUST guarantee that the save **loads, is internally coherent, and is deterministic from that point forward**. | MUST | KD-6 |
| FR-MG-033 | Migration MUST NOT be specified or tested as guaranteeing **counterfactual identity** with a career played natively through the same fixtures — a synthesizing bump makes that unachievable. | MUST | KD-6 |
| FR-MG-034 | A **format** transform MUST be a pure function of bytes: no draw, no clock, no simulation, no filesystem read beyond its input. | MUST | KD-7 |
| FR-MG-035 | A **generation** migration MUST run against a `DeterministicRngService` exactly as the live generator does, and MUST be specified as deterministic-by-seed rather than byte-pure. | MUST | KD-7 |
| FR-MG-036 | #50 MUST register **no** RNG stream, MUST allocate **no** domain tag or `SubsystemOrdinal`, and MUST take **no `_RESERVED_` placeholder** — #16 is untouched, and #50 has **nothing to promote later**. | MUST | KD-7 |
| FR-MG-037 | With an **empty chain**, a current-version save MUST classify `Current`, run **zero** transforms and load byte-identically to pre-#50; every non-current save MUST be refused exactly as today. | MUST | KD-7 |
| FR-MG-038 | Migration MUST run **outside the tick loop**, on load, **before any subsystem is constructed**. | MUST | KD-7 |

## 2.2 Data structures

```csharp
// Appendix D is the authoritative matrix. Ordinals are NOT serialized -- this enum is a runtime
// classification, and is deliberately the one enum in this spec with no APPEND-only contract.
public enum SaveClass : int { Current = 0, Migratable, TooNew, Unsupported, Corrupt }

// APPEND-only (FR-MG-017): the ordinal is the registry key a step registers against, and a
// reorder would silently re-point every registered step at the wrong blob.
public enum BlobKind : int
{
    Frame = 0, SeasonState, WorldStore, Match, Progression, /* one per sub-blob, appended */
}

// KD-2. Lives in the OUTER FRAME (FR-MG-010), beside SEASON_SAVE_FORMAT_VERSION -- NOT inside a
// sub-blob, because the classifier must read it without parsing a blob body.
public readonly struct SaveOriginStamp
{
    public readonly int WorldGenerationVersion;   // the migration input
    public readonly int BuildId;                  // DIAGNOSTIC ONLY -- never a migration input
}

// One registered step. Supplied by the BUMPING SPEC (FR-MG-018), held by #50 as a delegate.
public interface IMigrationStep
{
    BlobKind BlobKind { get; }
    int      FromVersion { get; }                 // produces FromVersion + 1, of this blob only
    byte[]   Apply(ReadOnlySpan<byte> blob);      // FR-MG-034: pure byte -> byte
}

// The KD-2 gate's verdict. `Materialise` is UNREACHABLE without a retained generator (FR-MG-015).
public enum GenerationVerdict : int { Ok = 0, Materialise, Refuse }

// #50 -> #49. Identity + slots; #50 bakes no string (FR-MG-026/028).
public readonly struct MigrationRefusal
{
    public readonly SaveClass Class;              // one intent per class (FR-MG-027)
    public readonly int SaveVersion, BuildVersion, SupportedFloor;   // SLOT VALUES
    public readonly BlobKind OffendingBlob;
}
```

**`BuildId` is diagnostic only, and the requirement is what makes it safe.** Migrating off a build number
rather than a format version would make two builds that share a format **falsely incompatible** — an
entire class of spurious refusals, generated by a field that exists to help diagnose them.

**`SaveClass` deliberately has no ordinal-stability contract**, and the absence is stated so it is not
read as an oversight: it is never serialized, and pinning it would imply a durability it does not have.
`BlobKind` **is** pinned, because it is the registry key.

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| **F1** | A version field is unreadable, out of range, or not a known value. | **`Corrupt`** — refuse (FR-MG-006). Never `Migratable`: running a transform over garbage would write a **plausible-looking** career, which is worse than any refusal. |
| **F2** | Any version exceeds the build's. | **`TooNew`** — refuse with its **own** identity (FR-MG-005/030). A build cannot know a future format, and must not guess. |
| **F3** | A version is older than the supported floor, or no registered chain reaches current. | **`Unsupported`** — refuse. |
| **F4** | A registered step throws, or produces bytes the **current codec** rejects. | **Refuse**, original intact (FR-MG-025). This is the designed shape of a migration bug: the seam can only produce bytes the codecs already adjudicate, so the bug surfaces as a refusal rather than a corrupt career. |
| **F5** | A generation stamp is older and **no retained generator exists** for it. | **`Refuse`** (FR-MG-014/015) — not a placeholder branch but the expected outcome past the floor. |
| **F6** | A generation stamp is older and the code **silently regenerates anyway**. | **Barred** (FR-MG-013). The failure #50 exists to prevent: the career loads, nothing errors, and the squads have quietly changed. Asserted by T-MG-GEN-001. |
| **F7** | A migration write fails partway. | Original intact, no partial file at the save location (FR-MG-025). The rejected *migrate-in-place-and-roll-back* design fails here by losing the career when the rollback is the thing that fails. |
| **F8** | A step written by spec X reads or writes spec Y's blob. | **Barred** (FR-MG-021) — a step receives only its own blob's bytes, so the violation is not expressible rather than merely forbidden. |

**Deliberately not a failure mode: a `Current` save with an empty registry.** That is the minimal tier's
**normal** state (FR-MG-037) and the whole reason the seam can land early.

**Deliberately not a failure mode: a refusal.** A refused save is a correct, non-destructive outcome
(FR-MG-023), not an error condition to be worked around. The pressure to make refusals rarer by loosening
FR-MG-004 is exactly the pressure KD-1's conservative rule exists to resist.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §2 (FR-MG-001..038, data structures, F1..F6) from supplement v0.6. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **FR-MG-008** — the supplement's classification table is per-save, but a save carries **many** versions, and nothing said how per-blob classes combine; the most-severe rule is written out, with `Corrupt` dominating `TooNew` so a damaged file is never reported as merely futuristic. **M:** added **FR-MG-022**, the build-time completeness check R-2 names: without it a spec that bumps a version and forgets a step turns every old save into `Unsupported` **with no diagnosis**, and the failure lands on the player rather than on the build. **M:** added **F8** and **FR-MG-021** — nothing barred a step from reaching into a neighbouring blob, which would silently defeat KD-3's isolation; expressed so the violation is **not representable** (a step receives only its own bytes) rather than merely forbidden. **L:** wrote out `SaveClass`, `BlobKind`, `SaveOriginStamp`, `IMigrationStep`, `GenerationVerdict` and `MigrationRefusal`, each annotated with the constraint that shapes it; recorded that `BlobKind` **is** ordinal-pinned (it is the registry key) while `SaveClass` deliberately is **not** (it is never serialized); added F7 and the two *"not a failure mode"* notes, the second because the pressure to make refusals rarer is the specific pressure KD-1 resists. |
#endregion
