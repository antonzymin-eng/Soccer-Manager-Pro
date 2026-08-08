# Injuries & Medical #41 — Section 8: References

**Created:** July 23, 2026
**Last Updated:** August 8, 2026 (v0.3 — balance-pass AR pass 9 M2: §8.2's FR-LW-031 line no longer cites the anti-phantom FR as authority FOR registering the stream)
**Last Updated (prior):** August 8, 2026 (v0.2 — balance-pass AR pass 7 M1: the #16 reference row no longer lists `DeterministicRngService` as a consumed API)
**Last Updated (prior):** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.3
**Status:** APPROVED

---

## 8.1 Internal cross-references

- **#27 Squad/Player Data** — `PlayerRecord` / `PlayerAttributes` (the robustness/injury-proneness term's
  source attributes; no dedicated injury-proneness field exists today, KD-4). `src/player-database/`.
- **#29 Training System** — `InjuryRiskContribution` / `ComputeInjuryRisk` (the already-published occurrence
  input, FR-TR-017); the `TrainingFatigue` accumulator #41 never reads or mutates (KD-2).
  `docs/specs/training-system/`.
- **#28 Player Progression & Lifecycle** — `RegenResult` / `RetirementResult` roster-boundary churn
  (FR-PG-011/015 — the FR-MD-025 lifecycle parallel); `GrowthProjection` sole attribute writer (the boundary
  #41 transitively honours). `docs/specs/player-progression-lifecycle/`.
- **#30 Season & Competition Loop** — `WorldStore.AdvanceDay` day-advance tick order (the pinned
  progression/training/human-systems seams #41's new slot is inserted after, per KD-6/ERR-030-002);
  `SeasonSaveCodec` sub-blob pattern; serialize-don't-regenerate. `docs/specs/season-competition-loop/`.
- **#16 Deterministic Sim** — `CanonicalSerializer`; the SplitMix64 determinism namespace (no
  `DeterministicRngService` consumption — ERR-041-012); the `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A`
  allocation, with `SubsystemOrdinals.InjuriesMedical = 92` deliberately unallocated
  (ERR-041-001/-012). `docs/specs/deterministic-sim/section-3.md`.
- **#34 Medical Staff (future)** — the `MedicalModifier` producer.
- **#38 UI/Client Framework (future)** — the `MedicalViewModel` consumer.
- **Match event ledger (read-only, deep tier)** — the already-emitted Tier-A collision/foul events #37
  (Match Analytics) and #44 (Discipline) also read read-only; #41's deep-tier `MatchLoad.HardContacts`
  derivation joins that same read-only posture, adding no new producer.

## 8.2 Determinism / phantom-avoidance precedents

- **FR-LW-031** — no phantom interface / no zero-draw RNG stream (the `world.arcs` precedent) — governs
  KD-1 (no stream registered at all — the occurrence draw is a keyed SplitMix64 derivation and ordinal 92
  is deliberately unallocated, ERR-041-012), KD-3 (no new match-engine interface), and KD-5 (no
  #34 interface).
- **#28 / #30 off-pitch keyed-draw precedent** — `player-progression.regen` (keyed by `entityId = clubId`)
  and `season-loop.season-events` (keyed on `(seed, seasonNumber, roundIndex, homeClubId, awayClubId)`) are
  the position-independent keyed-draw pattern KD-1 follows, distinct from the match-tick free-running
  card-severity cursor.
- **#21 / #29** — default-behaviour-neutral routing-seam discipline (identity modifiers, dial-off
  no-ops) — KD-5/KD-8.

## 8.3 External references

None — the numeric magnitudes (severity-tier recovery days, risk weights, bucketing fractions; Appendix A)
are `[GT]` gameplay-tuned and pinned by a future Stage-2/3 balance pass (the #21 G2 precedent), not sourced
from literature. This is a systems spec over the project's own data model, not a citation-bearing biomechanics
or sports-science spec.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial references. Status IN REVIEW. |
| 0.2 | 2026-08-08 | — | **Balance-pass AR pass 7 (M1)**: §8.1's #16 row listed `DeterministicRngService` as consumed, which §1.3 (pass 6) explicitly denies; ordinal 92 noted deliberately unallocated. |
| 0.3 | 2026-08-08 | — | **Balance-pass AR pass 9 (M2)**: §8.2's precedent line read "KD-1 (stream registered only at the first draw site)" — citing FR-LW-031, the FR that forbids the phantom, as the authority *for* registering it, in the file pass 7 bumped one section over. Re-anchored: no stream registered at all, ordinal 92 deliberately unallocated. |
#endregion
