# Save Migration & Versioning #50 — Section 3: Algorithms

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

**Everything below runs on the load path, outside the tick loop, before any subsystem is constructed**
(FR-MG-038). Nothing here draws from a #16 stream; the one place a draw occurs is **inside a frozen
generator** a generation migration invokes, which is that generator's own seeded stream (KD-7).

## 3.1 `Classify` — version fields only (FM-MG-01)

```
Classify(ReadOnlySpan<byte> file) -> SaveClass:
    frameVer := ReadLeadingVersion(file)                    # SAFE: the frame writes it FIRST
    if (!IsReadable(frameVer))            return Corrupt    # F1
    if (frameVer > BUILD_FRAME_VERSION)   return TooNew     # F2 -- distinct class, distinct message
    if (frameVer < SUPPORTED_FLOOR)       return Unsupported

    worst := (frameVer == BUILD_FRAME_VERSION) ? Current : Migratable
    foreach (kind, offset, length) in EnumerateSubBlobHeaders(file):   # LENGTH-PREFIXED, never parsed
        v := ReadLeadingVersion(file, offset)               # each sub-blob writes ITS version first
        worst := Worst(worst, ClassifyOne(kind, v))         # FR-MG-008
    return worst
```

**`ReadLeadingVersion` is the whole reason this is safe** (§1.4(d)). `SeasonSaveCodec` writes
`SEASON_SAVE_FORMAT_VERSION` as the **first field**, then a flag byte, then length-prefixed sub-blobs it
*"never parses"*; every sub-blob codec likewise reads its own version first. So #50 reads a version
without trusting **any** byte after it — a property the format already has, rather than one #50 asks for.

**`Worst` is a most-severe fold** (FR-MG-008): `Corrupt` > `TooNew` > `Unsupported` > `Migratable` >
`Current`. **`Corrupt` dominating `TooNew` is the ordering that matters**: a damaged file reported as
merely futuristic invites the player to wait for a patch that will never help.

**Nothing here is `Migratable` by default.** An unrecognised version at any level is refused
(FR-MG-004/006). Running a transform over garbage would produce a **plausible-looking** career, and the
asymmetry between "a refusal the player can recover from" and "a silently wrong save" is the whole
argument for the conservative rule.

**Classification does not lock or write the file** (FR-MG-007). It is a bounded read, so a classifier
called on a directory of saves to populate a load screen costs one small read each.

## 3.2 `MigrationRunner.Run` — the per-blob chain (FM-MG-02)

```
Run(ReadOnlySpan<byte> file) -> byte[]:
    require Classify(file) == Migratable                     # never called on any other class

    frame := MigrateBlob(Frame, ExtractFrame(file))           # FR-MG-020: the frame FIRST --
                                                              # it determines the sub-blob inventory
    out := NewFrameWriter(frame)
    foreach (kind, blob) in EnumerateSubBlobs(frame):
        out.Append(kind, MigrateBlob(kind, blob))             # independently; neighbours untouched
    return out.Finish()

MigrateBlob(BlobKind kind, ReadOnlySpan<byte> blob) -> byte[]:
    v := ReadLeadingVersion(blob)
    while (v < BuildVersionOf(kind)):
        step := registry.Require(kind, v)                     # F3 if absent -- refuse, do not skip
        blob  = step.Apply(blob)                              # FR-MG-034: pure byte -> byte
        v     = ReadLeadingVersion(blob)
        require v == step.FromVersion + 1                     # a step that lies is a bug, not a variant
    return blob
```

**A blob at the current version runs zero steps and is copied byte-untouched** (FR-MG-019). That is the
**common** case, not an optimisation: a typical update bumps one blob, and every other blob in the file
passes through exactly as the frame's opaque discipline already passes them through today when the frame
version bumps around them.

**Each step produces exactly `FromVersion + 1`, and the loop verifies it** (FR-MG-017). A step that
claimed a longer jump would be untestable in isolation and would silently skip whatever step it stepped
over. The post-condition turns "a step lied" into an immediate failure rather than a corrupted chain.

**A missing step refuses; it never skips.** This is where FR-MG-022's build-time completeness check earns
its place: the runtime behaviour is correct but undiagnosable — the player sees `Unsupported` and no
information about which spec forgot to register.

**The step is supplied by the bumping spec and closes over its own layout** (FR-MG-018). #50 sees a
`byte[]` and a `BlobKind`; a step migrating #45's board block lives in #45's T-phase and #50 never learns
what a board is. **It also cannot reach a neighbouring blob** (F8/FR-MG-021), because it is handed only
its own bytes — the violation is not representable rather than merely forbidden.

**The result is handed to the current, unmodified codec** (FR-MG-003), whose fail-loud gates adjudicate it
like any other input. **That is what makes the whole seam safe to add in front of ten codecs:** #50 can
only ever produce bytes those codecs already accept, so a migration bug surfaces as a refusal (F4) rather
than a corrupt career.

## 3.3 `GenerationGate.Check` — the decision the plan did not contain (FM-MG-03)

```
Check(in SaveOriginStamp stamp) -> GenerationVerdict:
    if (stamp.WorldGenerationVersion == WORLD_GENERATION_VERSION)        return Ok
    if (stamp.WorldGenerationVersion >  WORLD_GENERATION_VERSION)        return Refuse   # TooNew
    if (!generationRegistry.Has(stamp.WorldGenerationVersion))           return Refuse   # F5 -- past
                                                                                         # the floor
    return Materialise
```

```
Materialise(in SaveOriginStamp stamp, int worldSeed, int clubCount) -> byte[]:
    oldGen := generationRegistry.Require(stamp.WorldGenerationVersion)   # a FROZEN delegate (§4.4)
    rng    := new DeterministicRngService(worldSeed)                     # KD-7: seeded, NOT byte-pure
    league := oldGen(rng, worldSeed, clubCount)                          # the OLD world, reproduced
    return WriteAuthoredStyleBlob(league)                                # #47's shape -- no new machinery
```

**`Materialise` is unreachable without a retained generator** (FR-MG-015), and that is the honest shape of
the constraint rather than a defensive check: **the new build cannot reproduce the old world from the seed
alone once the generator's code has changed.** Only v(N)'s code can produce v(N)'s rosters, so a
generation migration exists exactly as far back as the build still ships generators.

**It runs once, and the world stops being derived.** After materialisation the save carries its rosters,
so the career no longer depends on generator code at all — which is what makes the repair permanent rather
than a per-load re-run.

**The output is #47's authored-style blob** (§1.5 KD-2). That reuse is not a convenience: it means a
migrated career is exactly a career whose rosters came from data instead of a seed, which is a state the
codebase already supports and already tests, so migration adds **no new save shape**.

**This gate is why #50 is more than plumbing.** Without it, every format in the file migrates perfectly
and the player's squads change anyway (§1.4(c), F6) — the failure arriving through the one door a
format-only migrator does not watch.

## 3.4 The non-destructive write (FM-MG-04)

```
CommitMigration(string originalPath, byte[] migrated):
    tmp := originalPath + ".migrating"
    Write(tmp, migrated); Fsync(tmp)
    VerifyLoadable(tmp)                       # re-READ through the CURRENT codec before committing
    Rename(tmp, NewSavePath(originalPath))    # a NEW file -- the original is untouched
    # the original is deleted by NOBODY here (FR-MG-023/024)
```

**The original is never modified, never deleted, never renamed** (FR-MG-023). A player who upgrades, hits
a migration, dislikes the result and reinstalls the old build still has the career they started with.

**`VerifyLoadable` runs before the rename, not after.** A migration whose output the current codec rejects
must fail *before* anything is committed (F4), and re-reading through the real codec is the only check
that means anything — a self-check by #50 would only prove #50 agrees with itself.

**The rejected design is *migrate in place, roll back on error*** (R-3). It is simpler, it will be
proposed, and it is the only design here that can lose a career: it loses one exactly when the rollback is
the thing that fails, which is the moment it is most needed.

## 3.5 `CompareForConflict` — the fact #39 needs (FM-MG-05)

```
CompareForConflict(a, b) -> { ClassA, ClassB, Ordering }:
    ClassA := Classify(a);  ClassB := Classify(b)
    Ordering := CompareVersionVectors(a, b)      # VERSIONS, never timestamps
```

**Versions, never timestamps** (KD-5). A file's modification time says which machine's clock ran last, not
which save a build can safely open.

**`TooNew` is not "the other one wins".** It is *"this build must not touch it"* (FR-MG-030) — a stronger
and safer statement, and the one #39's UX must present, because a build that resolves a conflict in favour
of a save it cannot read has resolved nothing.

**#50 returns facts and stops.** #39 owns every question of what to show, what to ask, and what to do —
and FR-MG-031 pins the one behaviour #50 does constrain: **a migrated save is written back only on the
player's next explicit save**, never silently on load. Otherwise opening a career on a second machine
rewrites the cloud copy into a format the first machine's older build then refuses, turning a **read** into
a data-loss event.

## 3.6 Arithmetic convention

**#50 performs no domain arithmetic at all.** It compares integers, folds a classification, and copies
bytes. There is no rounding convention to pin, no `[GT]` magnitude to balance, and nothing that reaches a
digest.

The one numeric discipline that matters is **length handling**, and it is inherited rather than invented:
every length prefix #50 reads is bounded **overflow-safely against `total − offset`**, never
`offset + need`, which wraps for a crafted near-`int.MaxValue` value. That is the exact hardening
`MatchSaveCodec` took in its own self-review, and #50 reads more untrusted length prefixes than any other
component in the tree.

## 3.7 Worked examples (hand-verifiable)

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | Current save, **empty registry** (minimal tier) | frame current, every blob current | `Current`; **zero** transforms; **byte-identical to pre-#50** (FR-MG-037) |
| (b) | Frame v2, build v3, one registered frame step | chain length 1 | `Migratable` ⇒ migrate the frame, **copy every sub-blob untouched** |
| (c) | Season-state v4 → v6, two registered steps | `4→5`, `5→6`, each verified `+1` | migrated; world-store, match and every other blob **byte-untouched** (FR-MG-019/021) |
| (d) | Frame v4, build v3 | `frameVer > build` | **`TooNew`** — refuse, own identity, own message (F2) |
| (e) | One blob `TooNew`, another `Corrupt` | most-severe fold | **`Corrupt`** — the damaged diagnosis wins, so the player is not told to wait for a patch |
| (f) | Season-state v2, floor v3 | below the floor | **`Unsupported`** — refuse (F3) |
| (g) | A registered step throws | runner aborts pre-commit | **refuse**; original **byte-identical** (F4/FR-MG-025) |
| (h) | A step's output fails the current codec | `VerifyLoadable` before rename | **refuse**; nothing committed — the designed shape of a migration bug |
| (i) | A step claims `4 → 6` | post-condition `v == From + 1` | **fails loud** — a lying step is a bug, not a variant |
| (j) | Generation stamp equals build's | `Check ⇒ Ok` | regenerate as today; **no materialisation, no cost** |
| (k) | Generation stamp older, generator retained | `Check ⇒ Materialise` | old generator runs **once**, rosters written as an authored-style blob, stamp updated |
| (l) | (k) but the generator was dropped past the floor | `Has() == false` | **`Refuse`** (F5) — correct, not a placeholder |
| (m) | Generation stamp older, code regenerates anyway | — | **the failure #50 exists to prevent** (F6): loads fine, squads silently different |
| (n) | Two cloud saves, one `TooNew` | `CompareForConflict` | *"this build must not touch it"* — **not** "the newer wins" (FR-MG-030) |
| (o) | A migrated career opened, then closed without saving | FR-MG-031 | the cloud copy is **unchanged** — a read never rewrites a save |

Examples (a), (m) and (o) are the three that matter most: (a) is why the seam can land before there is
anything to migrate, (m) is the failure the whole spec exists for, and (o) is the one that turns an
innocuous action into data loss if it is got wrong.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §3 (FM-MG-01..05: version-field-only classification with the most-severe fold and the argument for `Corrupt` dominating `TooNew`; the per-blob chain runner with the `+1` post-condition that turns a lying step into an immediate failure; the generation gate and its once-only materialisation into #47's existing blob shape; the non-destructive commit with `VerifyLoadable` **before** the rename; the #39 comparison. §3.6 records that #50 does no domain arithmetic at all and inherits `MatchSaveCodec`'s overflow-safe length bound, which matters because #50 reads more untrusted length prefixes than anything else in the tree. Fifteen worked examples). Status IN REVIEW. |
#endregion
