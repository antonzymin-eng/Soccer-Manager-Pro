# Match Presentation Depth #48 — Section 6: Performance

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 6.1 Loop classification

#48 is the **only spec in this wave with work on a per-tick path**, and that makes its performance section
the one that actually constrains a design rather than recording a formality.

- **`CommentaryRecorder.OnTick` and `CueMapper` run on the streamer's tick thread, once per tick** —
  60 Hz during a live streamed match, and as fast as the loop runs during a fast-forward.
- **`DeriveAnimationFrame` runs per rendered frame**, on the UI thread, over the observation history.
- **View-model snapshots run per screen refresh**, on the UI thread.

**It is still not a hot path in #18's sense.** #48 executes **outside** `MatchEngine.RunTick` — it is a
consumer of a tap the engine calls, not a step inside the tick pipeline — and it feeds no digest. But
"once per tick" is close enough to the loop that the zero-allocation discipline applies to the capture
path in practice, and §6.2 treats it that way.

**The budget that matters is the tick-thread one**, because it is the only #48 cost that can slow the
**simulation** rather than the presentation.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| `OnTick` — **depth disabled** | per tick | **one bool test, then return** (§3.1). The minimal tier's whole cost |
| `OnTick` — no narratable event | per tick, **the common case** | one bool test + a walk over an empty or short event span |
| `OnTick` — a narratable event | rare (a goal, a card, a save) | + 1 intent map, 1 gate, 1 transcript append |
| `SelectionMix` | per rendered line | **4 SplitMix64 rounds** — no lookup, no allocation, no state |
| `CueMapper` | per tick | 1 map attempt; `Emit` to a **no-op** by default |
| `DeriveAnimationFrame` | per rendered frame | O(`SQUAD_SIZE`) vector arithmetic + state-machine steps |
| `GetFeedView` | per screen refresh | `CopyLast(≤ COMMENTARY_WINDOW_LINES)` under the transcript lock |
| Export | once per export | O(lines) boundary renders + embedding |

**Allocation on the tick thread is zero on the common path**, and that is a requirement rather than an
observation: the recorder walks a caller-supplied read-only span, appends a value type to a pre-sized
transcript, and allocates only when the transcript grows past its capacity. The mix allocates nothing.
**`GetFeedView` copies into a fixed-capacity struct** — the bounded-window design (FR-MP-029) exists for
correctness reasons, and its allocation-free consequence is a second benefit rather than the motivation.

**The transcript is the one collection that grows**, and it grows with **narratable events**, not with
ticks — on the order of tens to low hundreds of lines per match, not tens of thousands. It is
**session-scoped** (§4.6): released with the session, never persisted, and never carried across matches.

**The lock in `GetFeedView` is held for a bounded copy**, and it is the only synchronisation in the spec.
The tick thread appends under it; the UI thread copies under it. Neither holds it across a render or
across a tick.

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `MP_BUDGET_ONTICK_US` — one `OnTick` on the **tick thread** | 20 µs | `[GT]` |
| `MP_BUDGET_ANIM_FRAME_MS` — one `DeriveAnimationFrame` over the full squad | 2 ms | `[GT]` |
| `MP_BUDGET_FEED_VIEW_US` — one window snapshot | 50 µs | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #48 and none is invented here: a
certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #48 has no implementation to measure. The ceilings are generous so a
first measurement either passes comfortably or reveals something genuinely wrong — the
`CertifiedPerfBaseline` PENDING posture applied to a spec that has not been built.

**`MP_BUDGET_ONTICK_US` is the one to measure first, and the only one with real consequences.** It is
multiplied by every tick of every match, on the **tick thread** — so an overrun does not merely make the
presentation stutter, it **slows the simulation**, which is visible in a fast-forwarded season. The other
two are UI-thread costs whose overrun costs frames, not simulation time.

**`MP_BUDGET_ANIM_FRAME_MS` is in milliseconds deliberately**: a per-rendered-frame operation on the UI
thread is measured against a frame budget, not a loop-step budget.

**The certified per-tick engine baseline is the reference #48 must not disturb.** `FR-PO-052`'s certified
figure (p50 = 0.4768 ms, p99 = 2.5669 ms per tick) is the engine's; #48 sits outside it, and a 20 µs
ceiling is ~4% of the p50 — enough headroom to be honest about, and small enough that an overrun is
diagnostic rather than ambiguous.

## 6.4 Memory

| Quantity | Order |
|---|---|
| One `CommentaryLine` (tick + intent + slots) | ~28 bytes |
| A full match's transcript (tens to low hundreds of lines) | **a few KB** |
| `CommentaryFeedView` (fixed capacity × line) | **~600 bytes**, by value |
| `AnimationFrameView` (per-agent derived state × `SQUAD_SIZE`) | **~1 KB**, by value |
| Persistent state | **0 bytes** — nothing is saved (FR-MP-032) |

**The last row is the point.** #48 adds **no save sub-blob, no format version, and no restore path** — the
#37 property, for the same reason: everything is derived per-frame from observation plus the live tap, and
the transcript is session-scoped.

**The exported HTML artifact grows with the match, and it is not a save.** Embedded rendered commentary
adds on the order of tens of kilobytes to a file that already carries sampled positions. It has no format
version and nothing reads it back — which is precisely why FR-MP-017 can bake locale into it without a
FR-LC-006 problem.

**Nothing in #48 grows with career length**, because nothing in #48 outlives a session. Recorded so its
absence from #22's `SAVE_SIZE_BUDGET` machinery — and from #50's version registry — is read as a
classification rather than an omission.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §6 (the only wave spec with per-tick work, classified as *outside* `RunTick` but close enough that the zero-allocation discipline applies to the capture path; cost profile with the depth-disabled and no-narratable-event fast paths named as the dominant ones; `[GT]` ceilings with `MP_BUDGET_ONTICK_US` flagged as the only one whose overrun slows the **simulation** rather than the presentation, and sized against the certified FR-PO-052 per-tick baseline; memory with the zero-persistent-state row and the export's non-save status). Status IN REVIEW. |
#endregion
