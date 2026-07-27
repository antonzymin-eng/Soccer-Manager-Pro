# Steam Packaging & Release Engineering #39 — Section 6: Performance

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 6.1 Loop classification

**#39 has no path in any loop, and it is the only spec in this wave for which that is trivially true.**
It is not in the 10 Hz tactical loop, the 60 Hz physics loop, the world-day advance, any per-tick tap, or
any presentation cadence. Its work happens at three moments:

- **A quiescent sync boundary** — save completion or clean exit. Off the tick loop by construction
  (FR-PK-035).
- **A conflict** — at most once per session, on a Cloud handle.
- **A release** — a human-cadence process measured in minutes, not milliseconds.

**Achievement predicate evaluation is the one thing that runs during play — and it is not #39's code**
(FR-PK-022). The shell evaluates predicates against the career's existing event surface, so its cost is
the shell's, and #39's contribution is a definition table. This is worth stating rather than eliding: a
reader looking for "the per-event cost of achievements" should find it named and correctly attributed
rather than absent.

**The performance section that matters for #39 is not about milliseconds at all** — it is §6.3's build
and gate durations, because those are what a release cadence is actually made of.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| Achievement predicate evaluation | per career event, **in the shell** | O(definitions) predicate checks over already-emitted events; no allocation on the common path |
| Pending-unlock flush | on reconnect | O(pending) platform calls — **single digits**, and idempotent (F8) |
| `EvaluateGate` | **once per release** | a fold over six records — microseconds, and irrelevant |
| `ResolveConflict` | at most once per session | **one remote fetch** + two `Classify` calls (#50's, bounded reads) |
| Whole-file sync | per quiescent boundary | one file upload — dominated by **save size and network**, neither of which is #39's |
| The packaged build | per release | minutes |
| The full gate run | per release | the KAT + perf + Unity + `dotnet-ci` + smoke path, on the pinned host |

**The remote fetch is the only #39 operation with a latency a player can feel**, and it is unavoidable:
`Classify` reads version fields **inside** the file (#50 KD-1), so a conflict cannot be resolved without
the bytes. **The alternative — deciding from metadata — is faster and is forbidden** (F4), and it is worth
recording here as well as in §3.2 that the performance argument is the one most likely to be used to
reintroduce it.

**Sync cost is save size, and save size is not #39's** (FR-PK-033). #39 moves whole files; the size is
#30's frame plus its sub-blobs, and the one #39-adjacent thing that could inflate it — #50's generation
**materialisation** — belongs to #50 and is once-only.

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `PK_BUDGET_GATE_EVAL_MS` — one `EvaluateGate` fold | 10 ms | `[GT]` |
| `PK_BUDGET_CONFLICT_RESOLVE_S` — fetch + classify + decide | 30 s | `[GT]` |
| `PK_BUDGET_SYNC_S` — one whole-file upload at a quiescent boundary | 60 s | `[GT]` |
| `PK_BUDGET_SMOKE_PATH_MIN` — the packaged-build smoke path | 10 min | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #39 and none is invented here: a
certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #39 has no implementation — **or artifact** — to measure. They are
generous so a first measurement either passes comfortably or reveals something genuinely wrong.

**The units are seconds and minutes deliberately, and the spec should not apologise for it.** #39's
operations are a network fetch, a file upload, and a human running a build. Expressing them in
microseconds to match the tree's other budget tables would be precision theatre about operations whose
real variance is dominated by a network and an operator.

**`PK_BUDGET_SMOKE_PATH_MIN` is a budget on a *process*, and it is the one with a design consequence**
(R-1a). The smoke path is the only artifact-side evidence in the gate, so there is standing pressure to
grow it — and a ten-minute ceiling is the mechanism that keeps it from becoming a second test suite
maintained in the worst possible environment. **A budget overrun here is a signal to justify the added
steps, not to raise the number.**

**Nothing here touches the certified per-tick engine baseline.** `FR-PO-052`'s p50 = 0.4768 ms /
p99 = 2.5669 ms is the engine's; #39 executes at save boundaries, at conflicts, and at releases — never
inside a tick.

## 6.4 Memory

| Quantity | Order |
|---|---|
| `AchievementProgress` (queue + counters) | **tens to hundreds of bytes** |
| `CloudSyncState` | **tens of bytes** |
| The achievement definition table | one row per achievement — **tens**, static |
| The evidence set at gate time | six records — **negligible**, and not resident at run time |
| A conflict in flight | **the save file, twice** — the local copy and the staged remote copy |
| Persistent **sim** state | **0 bytes** — nothing enters any save (FR-PK-042) |

**"The save file, twice" is the peak, and it is stated because it is the one place #39 is not free.** A
conflict stages the remote copy alongside the local one so both can be classified from their bytes
(FR-PK-002). For a save measured in hundreds of kilobytes to low megabytes this is unremarkable — and it
is **unavoidable given that classification cannot run on metadata**, which is the same constraint §6.2
records the latency for.

**Nothing in #39 grows with career length.** The progress store grows with the *achievement set*, which
grows per release and is bounded by the number of achievements shipped; `CloudSyncState` is fixed-size;
and nothing is persisted into a save. #39 is therefore absent from #22's `SAVE_SIZE_BUDGET` machinery and
from #50's version registry as a **classification**, not an omission.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §6 (no loop path at all, with **achievement predicate evaluation named and correctly attributed to the shell** rather than elided, since a reader will look for it; cost profile with the remote fetch identified as the only player-felt latency **and** as the argument most likely to be used to reintroduce forbidden metadata-based resolution; `[GT]` ceilings deliberately in seconds and minutes, with `PK_BUDGET_SMOKE_PATH_MIN` flagged as a budget on a **process** whose overrun should prompt justification rather than a raised number; memory with the stage-the-remote-copy peak stated as unavoidable for the same reason the latency is). Status IN REVIEW. |
#endregion
