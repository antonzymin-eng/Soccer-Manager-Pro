# Steam Packaging & Release Engineering #39 — Section 3: Algorithms

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

**Nothing below parses a save, compares a version, or touches sim state.** #39 folds evidence, applies a
policy over #50's answers, and moves whole files.

## 3.1 `EvaluateGate` — fail-closed on positive evidence (FM-PK-01)

```
EvaluateGate(EvidenceRecord[] records, string artifactCommit) -> GateVerdict:
    foreach kind in REQUIRED_EVIDENCE:                      # Appendix B — the six rows
        if (!TryFind(records, kind, out r))     return Fail   # F1: ABSENCE IS FAILURE
        if (!r.Executed)                        return Fail   # F2: A SKIP IS NOT A PASS
        if (!r.Passed)                          return Fail
        if (IsInputSide(kind) && r.CommitSha != artifactCommit)
                                                return Fail   # F3: stale evidence LOOKS like a pass
    return Pass
```

**`REQUIRED_EVIDENCE` is iterated, not the record list**, and the direction matters. Walking the records
and checking each one passes would return `Pass` for an **empty** set — the failure mode that produces a
green gate on a release where nothing was run at all, which is §1.4(b)'s defect reproduced one level up.

**`!r.Executed ⇒ Fail` is the line the whole spec exists for** (FR-PK-012). CI's `unity-tests` is gated on
a secret and reports **success** when that secret is absent; its own comment says it is *"cleanly SKIPPED
(not failed)."* That is right for CI and wrong for a ship gate, and the two must be read with **opposite
defaults** from the **same** job — the gate reading its **artifact**, never its summary status.

**The commit check applies to input-side rows only** (FR-PK-014 / KD-2). Five of the six evidence kinds
measure the **project at a commit** rather than the packaged binary, so the commit binding is the *only*
thing tying them to the build being shipped — which is why R-1 is a gate rule rather than hygiene.
`PackagedSmokePath` is the exception: it is run **on the artifact**, so it binds by construction.

**The function is pure** (FR-PK-020), and deliberately so. A gate that existed only as a runbook could not
be tested except by attempting a release; as a fold over an evidence set, its fail-closed property is
testable today — including against the **real skip-shaped output** of an unlicensed `unity-tests` run
(§5.1).

## 3.2 `ResolveConflict` — policy over #50's classification (FM-PK-02)

```
ResolveConflict(localPath, remoteHandle) -> ConflictOutcome:
    if (!TryFetchToStaging(remoteHandle, out remotePath))
        return Refuse                                  # F4: NEVER decide on metadata alone

    localClass  := Migration.Classify(ReadBytes(localPath))    # #50 owns this (FR-PK-001)
    remoteClass := Migration.Classify(ReadBytes(remotePath))

    if (localClass == TooNew || remoteClass == TooNew)   return NoAction      # F5
    if (IsRefused(localClass) && IsRefused(remoteClass)) return Refuse
    if (IsRefused(localClass))                           return UseRemote     # never SELECT the
    if (IsRefused(remoteClass))                          return UseLocal      # refused copy (F6)

    ordering := Migration.CompareForConflict(localPath, remotePath)   # PRE-migration versions
    return ordering == Equal ? UseLocal : AskPlayer                   # NEVER auto-merge
```

**The fetch comes first, and it is not an optimisation detail.** A Cloud conflict is first observed as
**metadata**, but `Classify` reads version fields **inside** the file (#50 KD-1) — so a policy that could
run before the fetch would necessarily run on timestamp and size, which is the *"newer wins"* heuristic
every row of KD-1's table exists to reject. **Refusing is the correct answer to "I only have metadata"**;
guessing would happily overwrite a `TooNew` save with an older one and call it a sync.

**`TooNew` short-circuits before anything else.** A build cannot reason about a format it does not know,
so the outcome is *"this build resolves nothing"* rather than *"the newer copy wins"* — the second
sentence is how the newer copy gets overwritten by a build that could not read it.

**A refused copy is never selected and never deleted** (FR-PK-007/009 / F6): #50's non-destructive refusal
extends to Cloud unchanged, and the branch above picks the *other* copy rather than repairing the bad one.

**Two `Current` copies that differ go to the player** (FR-PK-005). **There is no merge, at any tier.** A
save is one causal history; two divergent careers cannot be combined, and any automatic pick silently
discards one of them.

**Migration is never a tiebreaker** (FR-PK-006). `CompareForConflict` runs on the **pre-migration**
versions, because treating a locally-migrated copy as "newer" would let *opening* a career on machine B
rewrite machine A's cloud copy — and FR-PK-008 completes that protection: **a migrated save uploads only
on the player's next explicit save.** A read must never become a write.

## 3.3 Achievement evaluation and flush (FM-PK-03)

```
# in the SHELL -- NOT in #39's assembly (FR-PK-022)
OnCareerEvent(e):
    foreach def in achievementDefinitions:
        if (def.Predicate(e, progress.Counters))          # READ-ONLY over events already emitted
            progress.PendingUnlocks.Add(def.Id)           # a QUEUE, not a ledger

OnPlatformConnected():
    held := platform.QueryUnlocked()                      # THE STORE OF RECORD (FR-PK-023)
    foreach id in progress.PendingUnlocks.Snapshot():
        if (!held.Contains(id))  platform.Unlock(id)      # F8: never re-grant
        progress.PendingUnlocks.Remove(id)                # flush EXACTLY once
```

**The predicate is read-only over events the career already emits** (FR-PK-021) — the #37 / #44 posture.
**#39 adds no event, no hook and no sim field**, which is what makes FR-PK-045's observer neutrality true
by construction rather than by argument.

**Evaluation lives in the shell, and that placement is load-bearing** (§4.1). Had #39 evaluated
achievements itself it would reference the career and season assemblies — and `season-save` reaches
`MatchEngine` and `LivingWorld`, so **the packaging spec would transitively depend on the whole
simulation**. The inversion is the one #48 uses for `ICueSink` and #50 uses for its registered generators.

**The platform wins on unlock state** (FR-PK-023). A local file that also *owns* unlock truth produces
either a double grant or a lost unlock, and the queue-plus-reconcile shape is what avoids both: the local
store holds **what is pending**, never **what is held**.

**The one prohibition that is a determinism defect if violated:** nothing in the sim may read achievement
state (FR-PK-027 / F9). A sim branching on *"has this player unlocked X"* would make replay depend on
**account state that is not in the save** — a defect that would survive every save-format test in the
tree, because the save would still be perfectly valid.

## 3.4 Sync on quiescence (FM-PK-04)

```
OnQuiescentBoundary(reason):                    # save completion, or clean exit -- FR-PK-035
    if (writeInFlight)  return                  # the running game owns the file (F7)
    CloudUpload(saveFilePath)                   # WHOLE FILE -- #39 parses nothing (FR-PK-033)

BeforeLoad(path):
    if (syncReplacing(path))  Defer()           # never load a file sync is replacing (FR-PK-036)
```

**Whole-file only** (FR-PK-033). The save is one atomic file with a version-first frame and opaque
length-prefixed sub-blobs (§1.4(d)), so there is no meaningful per-blob sync — and attempting one would
require #39 to parse a frame it has no business parsing.

**A mid-match save syncs as one unit or not at all** (FR-PK-034). The `matchPresent` blob rides *inside*
the file; there is no coherent state in which the world syncs and the match does not, and **#39 must not
create one**.

**Quiescence over continuity, deliberately.** The rejected alternative — sync continuously so the cloud is
always current — **maximises the window in which two machines hold divergent in-progress careers**, which
is exactly the state KD-1 says cannot be merged. **Reducing conflict frequency is worth more than reducing
conflict staleness.**

**A crashed session falls back to the last completed save** (FR-PK-037), which is already the local
behaviour — so Cloud adds no new loss mode here, only a new copy of an existing one.

## 3.5 Arithmetic convention

**#39 performs no domain arithmetic.** It folds booleans, compares commit strings, and copies files. There
is no rounding convention to pin, no magnitude to balance, and nothing that reaches a digest.

The rule that matters instead is a **layering** one: no #39 value may flow into the simulation
(FR-PK-027), and #39 reads no sim value to compute one. §5.5 asserts both structurally.

## 3.6 Worked examples (hand-verifiable)

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | Cloud off, no achievements | nothing runs | **today's save path, byte-for-byte** (FR-PK-044) |
| (b) | Gate with an **empty** evidence set | `REQUIRED_EVIDENCE` iterated | **Fail** — the direction of the loop is what makes this true |
| (c) | Gate with a **skipped** `unity-tests` artifact | `Executed == false` | **Fail** (F2) — the exact input CI treats as success |
| (d) | Every row present and affirmative, one naming an older commit | input-side commit check | **Fail** (F3) — stale evidence *looks* like a pass |
| (e) | Every row present, affirmative, same commit, smoke path run | — | **Pass** |
| (f) | A conflict seen only as metadata | fetch not performed | **Refuse** (F4) — never guess |
| (g) | An older save with a **newer timestamp** | classified from bytes | resolved on **class and version**, not on the clock |
| (h) | Remote `TooNew` | short-circuit | **NoAction** — neither copy touched (F5) |
| (i) | Remote `Corrupt`, local `Current` | refused-copy branch | **UseLocal**; the corrupt copy is **neither selected nor deleted** (F6) |
| (j) | Both `Current`, contents differ | no merge path exists | **AskPlayer** (FR-PK-005) |
| (k) | Local `Migratable`, remote `Current` | compare **pre-migration** | a normal conflict; migration is **not** a tiebreaker |
| (l) | (k) resolved, career opened, closed without saving | FR-PK-008 | the cloud copy is **unchanged** — a read is not a write |
| (m) | Mid-match save syncing | whole file | world **and** match sync together, or neither (FR-PK-034) |
| (n) | Sync fires while a save is being written | `writeInFlight` | **deferred** to the next boundary (F7) |
| (o) | An unlock earned offline, then reconnect | queue flush | granted **once**; an already-held unlock is **not** re-granted (F8) |

Examples (b), (c) and (f) are the three that matter most: (b) and (c) are the inversion the spec exists to
introduce, and (f) is the shortcut that would quietly undo every conflict rule.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §3 (FM-PK-01..04: the gate as a **pure fold** over an evidence set — with the loop direction called out, since walking the records rather than the required kinds returns `Pass` for an empty set; conflict resolution as policy over #50's classification, with the fetch-before-decide rule argued from the fact that a conflict is first observed as metadata; shell-side achievement evaluation and the queue-plus-reconcile flush; sync on quiescence, with the rejected continuous-sync alternative and its frequency-over-staleness argument. Fifteen worked examples). Status IN REVIEW. |
#endregion
