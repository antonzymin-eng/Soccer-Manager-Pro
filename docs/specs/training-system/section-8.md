# Training System #29 — Section 8: References

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 Internal cross-references

- **#27 Squad/Player Data** — `PlayerRecord` / `PlayerAttributes` (the trainee records; `Stamina` is an
  attribute, not a condition). `src/player-database/`.
- **#28 Player Progression** — `GrowthProjection` sole attribute writer; `TrainingInput` (Neutral identity,
  the #29 append point); FR-PG-008/009. `docs/specs/player-progression-lifecycle/`.
- **#30 Season & Competition Loop** — `WorldStore.AdvanceDay` day-advance tick order (slot-1 progression /
  slot-2 training reserved null seams); `SeasonSaveCodec` sub-blob pattern; KD-5 serialize-don't-regenerate.
  `docs/specs/season-competition-loop/`.
- **#16 Deterministic Sim** — `CanonicalSerializer`; the `_RESERVED_0x21_` / `SubsystemOrdinals` 83
  reservation (unpromoted — KD-6). `docs/specs/deterministic-sim/section-3.md`.
- **#34 Coaching (future)** — the `CoachingModifier` producer.
- **#41 Injuries (future)** — the `InjuryRiskContribution` consumer.
- **Match engine** — `PlayerAttributeProjection` caller-supplied `float fatigue` (KD-P4), the KD-1
  projection target. `src/match-engine/PlayerAttributeProjection.cs`.

## 8.2 Determinism / phantom-avoidance precedents

- **FR-LW-031** — no phantom interface / no zero-draw RNG stream (the `world.arcs` precedent) — KD-6.
- **#21** — default-behaviour-neutral routing-seam discipline — KD-3/KD-8.

## 8.3 External references

None — the numeric magnitudes are `[GT]` gameplay-tuned (Appendix A) and pinned by a Stage-2/3 balance pass
(the #21 G2 precedent), not sourced from literature.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial references. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | APPROVED. |
#endregion
