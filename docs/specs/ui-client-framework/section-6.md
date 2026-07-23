# UI / Client Framework #38 — Section 6: Performance

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 6.1 Cadence

The UI does **no work inside the sim's 60 Hz physics / 10 Hz AI loops**. It reads at **render cadence**:
the match view pulls the latest published `LiveMatchFrame` once per rendered frame (a copy of the
already-published snapshot — no sim work), and non-match screens re-project on-change (after a
`AdvanceAndPlayNextRound` returns, a tactic is set, etc.). This is presentation infrastructure (Code
Standards #20 layer taxonomy), **not** zero-allocation game-loop code, so the FR-CS-068 hot-path budget
does not bind it (the `match-viewer` precedent).

## 6.2 Sizing

- **Projection:** each `Project()` builds one immutable value-type VM (a match frame is 1 ball + 22
  agents + score — the `LiveMatchFrame` size; a season VM is a table + calendar cursor). Bounded,
  independent of match length.
- **Navigation:** a shallow screen stack + a small registry — constant memory.
- **Dispatch:** O(1) per intent — a switch to one public-seam call.

## 6.3 No persistent-state cost

The UI holds no sim state and bumps no save format (FR-UI-022). Client-local preferences (layout,
last-screen) live outside the determinism save, so #38 adds **zero** to save size / save-restore time.

## 6.4 Refresh decoupling (KD-3)

Reading the latest published frame at render cadence is decoupled from the sim tick by the streamer's
lock-protected handoff — the UI never blocks the sim thread and the sim never blocks the render thread.
No determinism obligation applies (the UI advances nothing); observer-neutrality (§5 T-UI-NEU-001) is the
only load-bearing property, and it is structural (reads only).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial performance analysis: render-cadence observation, constant-memory substrate, zero persistent-state cost, refresh decoupling. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L; M-1 match-view reads streamer frame, M-2 cross-thread command marshaling FR-UI-023/F6, L-1 dispatcher split, L-2 Pop-below-root) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
