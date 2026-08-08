# Club Finances & Economy #40 — Section 8: References

**Created:** July 23, 2026
**Last Updated:** August 8, 2026 (v0.2 — balance-pass AR pass 8 M2: the §8 precedent list no longer names #41's draw as the `injuries.occurrence` stream)
**Last Updated (prior):** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 Internal cross-references

- **#27 Squad/Player Data** — `Squad.ClubId` (the stable club-identity enumeration #40's F6 club-universe
  check reads; no dedicated wage field exists on `PlayerAttributes` today, per KD-5). `src/player-database/`,
  `docs/specs/squad-player-data/`.
- **#30 Season & Competition Loop** — `RollToNextSeason()` (§3.5 / FR-SN-029: `finalize (a) → board (b) →
  [#43 insertion (a')] → regenerate (c) → advance-ages (d) → reset (e)`); FR-SN-031's well-defined
  insertion-point precedent (the model KD-6's own back-prop follows); `SeasonSaveCodec` sub-blob pattern;
  serialize-don't-regenerate. `docs/specs/season-competition-loop/`.
- **#16 Deterministic Sim** — `CanonicalSerializer`; `DeterministicRngService`; the reserved
  `_RESERVED_0x29_` / `SubsystemOrdinals.ClubFinances = 91` off-pitch namespace slot (ERR-040-001).
  `docs/specs/deterministic-sim/section-3.md`.
- **#31 Transfer Market (future)** — the `AvailableTransferBudget`/`ApplyTransaction` caller.
- **#34 Staff (future)** — the second `ApplyTransaction` caller (staff wage line items).
- **#45 Board & Ownership (future)** — the `BoardModifier` producer.
- **#43 Promotion/Relegation (future)** — the (a') transform whose result #40's step (b') reads
  (`finalTablePosition` post-promotion/relegation).
- **#38 UI/Client Framework (future)** — the `FinancesViewModel` consumer.
- **#28 Player Progression & Lifecycle / #29 Training System / #41 Injuries & Medical** — the season-save
  sub-blob precedent (`PROGRESSION_SAVE_FORMAT_VERSION` / `TRAINING_SAVE_FORMAT_VERSION` /
  `MEDICAL_SAVE_FORMAT_VERSION`) #40's `FINANCE_SAVE_FORMAT_VERSION` follows (KD-7); #41's `MedicalModifier`
  Identity-vs-`default()` zero-value-trap lesson, applied here to `BoardModifier` (KD-4/§1.6).
- **Master development plan §4.3/§4.5/§5** — "Budget based on league finish" (Transfer System, §4.3) and
  "Board sets expectation based on budget… Exceed: Bonus budget" (Board Objectives & Progression, §4.5) are
  the master-plan framing #40 implements as `SettleFinances`; Stage 3 "Financial management (FFP, budgets,
  wages)" (§5) is #40's home wave. Cited for feature framing only — no numeric magnitude in this spec is
  sourced from the master plan. `docs/planning/master-development-plan.md`.

## 8.2 Determinism / phantom-avoidance precedents

- **FR-LW-031** — no phantom interface / no zero-draw RNG stream (the `world.arcs` precedent) — governs
  KD-2 (the reserved-not-promoted namespace slot), KD-3 (no #31/#34 interface built ahead of those specs),
  and KD-4 (no #45 interface built ahead of #45).
- **#29 `0x21`-stays-reserved precedent** — the model KD-2's `_RESERVED_0x29_` placeholder-row treatment
  follows exactly.
- **#28 / #30 / #41 off-pitch keyed-draw precedent** — `player-progression.regen` (keyed by `entityId =
  clubId`), `season-loop.season-events` (keyed on `(seed, seasonNumber, roundIndex, homeClubId,
  awayClubId)`), and #41's keyed occurrence derivation (keyed on `(worldSeed, playerId, worldDay, purpose)` — no registered stream, ERR-041-012) are the
  position-independent keyed-draw pattern the future T3 `club-finances.sponsorship-variance` stream will
  follow (KD-2/FR-FN-010) — no free-running cursor to persist even once it exists.
- **#21 / #29 / #41** — default-behaviour-neutral routing-seam discipline (identity modifiers, dial-off
  no-ops) — KD-4/KD-5/KD-8.
- **#41 `MedicalModifier` Identity-vs-`default()` zero-value-trap lesson** — the direct model for
  `BoardModifier`'s explicit `Identity` factory and F4 fail-loud gate (§1.6/KD-4).

## 8.3 External references

None — the numeric magnitudes (prize-money-by-position endpoints, base transfer/wage budget allocations,
prize-money-to-budget share weights; Appendix A) are `[GT]` gameplay-tuned and pinned by a future Stage-2/3
balance pass (the #21 G2 precedent), not sourced from literature. This is a systems spec over the project's
own data model and its own master-plan feature framing (§8.1), not a citation-bearing biomechanics,
sports-science, or football-finance-industry spec.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial references. Status IN REVIEW. |
| 0.2 | 2026-08-08 | — | **ERR-041-012 back-prop**: the keyed-draw precedent list restated #41's entry off the phantom stream name. |
#endregion
