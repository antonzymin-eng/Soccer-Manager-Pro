# Match Analytics & Statistics #37 — Section 6: Performance

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+3L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 6.1 Cadence

#37 does **no work inside the engine's per-physics-tick (60 Hz) hot path** and none inside the 10 Hz AI
stride. It observes at the world-tick / render sample cadence (the `match-viewer` rate). The per-tick
tap read is a copy-out of the current tick's drained Tier A records (typically 0–2 records/tick — the
engine publishes sparsely: a possession change, an occasional foul/goal/restart), and the world-state
sample is 1 ball + 22 agent value copies. This is presentation tooling (Code Standards #20 layer
taxonomy), **not** zero-allocation game-loop code, so the FR-CS-068 zero-alloc hot-path budget does not
bind it (the `match-viewer` precedent).

## 6.2 Sizing

- **Accumulators:** a fixed handful of per-team counters + a `HEATMAP_COLS × HEATMAP_ROWS × 2` bin
  grid + the possession tick triple. Constant memory, independent of match length.
- **Maps:** `GoalMap`/`FoulMap`/`OffsideMap` grow with actual events (bounded by real match rates —
  goals/fouls/offsides are tens per match, not thousands). No unbounded growth.
- **`Build()`** is O(bins + map points) — a per-render-frame `Build()` for a live HUD is cheap and
  allocation-light (a snapshot of the current view models).

## 6.3 No persistent-state cost

#37 stores nothing across matches and bumps no save format (FR-AN-020), so it adds **zero** to save
size or save/restore time. A match's analytics are recomputed live; they are not serialized.

## 6.4 Perf-gate posture

No FR-PO-052 per-tick engine budget applies (#37 adds no engine tick work — the tap is a read-only
copy-out, observer-neutral). If a Stage-2 live HUD wants a frame budget, that is a #38 UI concern
measured on the pinned host, not a #37 sim-tick gate.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial performance analysis: observation cadence, constant-memory accumulators, zero persistent-state cost. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L; M-1 lossless every-tick + F6, M-2 possession known-handler, L-1 `SubstitutionEvent.Team`, L-2 territorial disambiguation, L-3 phase-context) → AR-2 convergence; APPROVED. See section-9 §9.3. |
#endregion
