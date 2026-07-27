# Audio & Sound Design #51 — Section 6: Performance

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 6.1 Loop classification

**#51 has no path in any simulation loop** (FR-AU-006/034/035). It is not in the 10 Hz tactical loop, the
60 Hz physics loop, the world-day advance, or any per-tick tap. It cannot be, because it holds no sim type
at all — which makes this the one spec in the wave whose loop-isolation claim is provable from the
reference graph rather than asserted behaviourally.

Its work sits on two presentation cadences:

- **`Play` runs on whatever thread the shell's `ICueSink` adapter is called on**, which during a live
  streamed match is #48's tick thread. **That is the one cadence with real consequences**, and §6.3
  budgets it accordingly.
- **Ducking envelopes and gain composition run per audio frame**, on the host's audio thread — owned by
  Unity, not by #51's contract layer.

**The `Play` cadence deserves precision rather than the reassurance that #51 "is not in the loop".** #48
maps cues on the tick thread, and #51's `Play` is what its adapter calls — so an expensive `Play` slows
the **simulation**, exactly as #48's own `OnTick` does, even though neither is inside `RunTick`.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| `Play` — **#51 absent** | — | **nothing**; #48's sink is a no-op (FR-AU-038) |
| `Play` — mapped cue | per emitted cue, **on the tick thread** | 1 dictionary lookup (shell) + 1 catalogue lookup + gain composition + a host handoff |
| `Play` — **unmapped** cue | per emitted cue | **one failed lookup, then return** (F1) — the silent-no-op path |
| Cue **variation** | per cue with variants | 1 display-side random + an index — **no cursor, no serialized state** (F7) |
| `DuckGain` | per audio frame per bus | a fold over the rows targeting that bus — a table of **single-digit** length |
| Settings apply | per settings change | O(bus count) clamps — a human-cadence operation |
| Catalogue / mapping completeness | **build time** | O(entries) — never at run time |

**Emitted cues are rare relative to ticks.** #48 maps a cue on a narratable or physically notable event —
a goal, a whistle, a ball strike — not on every tick, so `Play`'s tick-thread cost is paid on a small
fraction of ticks rather than on all of them. **Ambient loops are started once and sustained by the host**,
not re-triggered per tick.

**Allocation on the `Play` path is zero**, and that is a requirement rather than an observation: two
lookups into pre-built tables, integer gain arithmetic, and a handoff. The catalogue and the mapping are
built once and read-only thereafter.

**The completeness checks cost nothing at run time** because they are **build-time** (FR-AU-005). That is
the second benefit of putting them at build time; the first is that a missing sound cannot ship unnoticed.

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `AU_BUDGET_PLAY_US` — one `Play` on the **tick thread** | 30 µs | `[GT]` |
| `AU_BUDGET_DUCK_FRAME_US` — one `DuckGain` fold over the table | 10 µs | `[GT]` |
| `AU_BUDGET_SETTINGS_MS` — one settings apply | 5 ms | `[GT]` |
| `AU_BUDGET_CATALOGUE_VALIDATE_MS` — the build-time completeness sweep | 500 ms | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #51 and none is invented here: a
certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #51 has no implementation to measure. They are generous so a first
measurement either passes comfortably or reveals something genuinely wrong — the `CertifiedPerfBaseline`
PENDING posture applied to a spec that has not been built.

**`AU_BUDGET_PLAY_US` is the only one whose overrun costs simulation time**, and it is the one to measure
first. The other three cost frames, a user-visible pause on a settings screen, and build time
respectively.

**`AU_BUDGET_CATALOGUE_VALIDATE_MS` is a build budget deliberately**, and it belongs in this table rather
than being unmeasured: a completeness sweep that grows superlinearly with catalogue size would eventually
be "temporarily" disabled, which is how a build-time guarantee becomes a shipped silence.

**Nothing here touches the certified per-tick engine baseline directly.** `FR-PO-052`'s p50 = 0.4768 ms /
p99 = 2.5669 ms is the engine's; a 30 µs `Play` ceiling on a small fraction of ticks is well inside its
noise — but the *"well inside"* claim is exactly the one that stops being true if cue emission ever became
per-tick, which is #48's contract to keep.

## 6.4 Memory

| Quantity | Order |
|---|---|
| The cue catalogue | one entry per cue — **hundreds**, static after build |
| The `CueId → CueKey` map (shell) | one row per emittable cue — **hundreds**, static |
| The ducking table | **single-digit** rows |
| The settings fragment | **tens of bytes** |
| Loaded audio assets | **the dominant cost by orders of magnitude — and not #51's** |
| Persistent **sim** state | **0 bytes** — nothing enters any save (FR-AU-037) |

**The fifth row is the honest one.** #51's own data structures are negligible; the memory an audio system
actually consumes is **assets**, and those are production's (R-1). A spec that budgeted its tables while
ignoring the sample bank would be measuring the wrong thing — the tables are noise, the bank is the
budget, and the bank is not specified here.

**Nothing in #51 grows with career length, league size or match count.** The catalogue is static, the map
is static, the settings fragment is fixed-size, and nothing is persisted into a save — so #51 is absent
from #22's `SAVE_SIZE_BUDGET` machinery and from #50's version registry as a **classification**, not as an
omission.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §6 (loop isolation provable from the reference graph rather than asserted, since #51 holds no sim type — but with the **`Play` cadence stated precisely**: it runs on #48's tick thread, so an expensive `Play` slows the simulation even though nothing is inside `RunTick`; cost profile with the rarity of emitted cues relative to ticks made explicit; `[GT]` ceilings incl. a **build-time** budget for the completeness sweep, on the ground that a sweep which grows superlinearly gets "temporarily" disabled and turns a build-time guarantee into a shipped silence; memory, whose honest row is that assets dominate #51's own tables by orders of magnitude and are not #51's). Status IN REVIEW. |
#endregion
