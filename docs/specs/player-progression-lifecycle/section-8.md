# Player Progression & Lifecycle #28 — Section 8: References

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 Internal (authoritative)

- **Master development plan** `docs/planning/master-development-plan.md` §4.3 (aging/decline/
  retirement) / §5 (youth/regens) — the source of the Stage-2 §4.3 literal rule (>30 → −1/yr, <24 →
  +1/yr, retire at 36) this spec expresses as a deterministic projection.
- **Squad/Player Data Layer #27** `docs/tracking/squad-player-data-design.md` +
  `src/player-database/` — `PlayerRecord` / `PlayerAttributes` / `RosterGenerator` / `Squad` /
  `PlayerDatabaseConstants` (the record shape, the fixed-budget draw pattern, the position-bias table,
  `CLUB_SQUAD_SIZE`, the club-scoped `PlayerId` contract KD-3).
- **Season & Competition Loop #30** `docs/tracking/season-competition-loop-design.md` +
  `docs/specs/season-competition-loop/` — the day-advance loop (KD-2) + season-boundary roll (KD-6)
  that invoke #28, the `SeasonSaveCodec` opaque-sub-blob pattern (KD-1), the serialize-don't-regenerate
  posture (KD-5), and the reserved #28 seam.
- **Deterministic Simulation #16** `docs/specs/deterministic-sim/section-3.md` — the `_RESERVED_0x20_`
  / `SubsystemOrdinals` 82 rows this spec promotes; `DeterministicRngService` (Reserve/DrawReserved/
  CloseReservation) + `CanonicalSerializer`; the `world.arcs` / #30 `0x22` register-at-first-draw-site
  precedent (FR-LW-031).
- **Living World #22** `FR-LW-031` (no phantom interfaces) — the rule behind the KD-2 method-parameter
  seam and the register-at-first-draw-site discipline.
- **Match save file / Unified season save** `docs/tracking/match-save-file-design.md` /
  `unified-season-save-design.md` — the `MatchSaveCodec` / `WorldStateSerializer` fail-loud codec
  posture (version gate first, overflow-safe `ReadCount`, trailing-byte check) §3.5 mirrors.
- **Code Standards #20** — constant tags, off-pitch-cadence carve-out from the 60 Hz hot-path rules,
  the "Interface Design Principle" (no interface until both sides are specified).

## 8.2 External

No external academic citations. The Stage-2 aging model is the master plan's own literal rule; the
CA/PA framing is the genre-standard current-ability/potential-ability model (Football Manager /
Championship Manager lineage), used as a design vocabulary, not a cited source. The `[GT]` magnitudes
are illustrative pending the balance pass (§1.3), so no calibration citation is load-bearing at
approval.

## 8.3 Citation status

No `[CITATION-PENDING]` rows. The spec's numeric contract is *shapes*, not tuned magnitudes (the #21
§9.2 / #30 posture), so approval is not gated on an external calibration source.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial references: internal authoritative sources (#27/#30/#16/#22/#20 + master plan); no external citations required (shapes-not-magnitudes). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
