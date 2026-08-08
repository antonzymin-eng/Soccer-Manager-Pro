# Injuries & Medical #41 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** August 8, 2026 (v0.2 — balance-pass AR pass 7 M1: the ERR-041-012 sweep reaches §7 — the T2 instruction no longer orders the stream registration §4.5 forbids, and §7.2's stochastic-recovery extension appends into the keyed derivation, not onto a stream)
**Last Updated (prior):** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.InjuriesMedical` assembly: value types (`InjurySeverity`, `InjuryState`,
  `MatchLoad`, `MedicalModifier`, `MedicalViewModel`), the deterministic Stage-2 `AdvanceMedicalDay` +
  `ClassifySeverityFromDraw` + `AssembleRiskScore` (dial off → recovery-only no-op),
  `InjuriesMedicalConstants`. Behaviour-neutral by construction (KD-8).
- **T1** — `MedicalSaveCodec` (`MEDICAL_SAVE_FORMAT_VERSION` = 1) + composition into #30's season save (the
  `SeasonSaveCodec` sub-blob; #30's composing format-version bump coordinated here). Fail-loud gates.
- **T2** — Wire `AdvanceMedicalDay` at #30's **new** reserved slot (after #28/#29/#33, before
  `WorldStore.AdvanceDay()`, the ERR-030-002 back-prop) — the draw stays the keyed local derivation
  (`DOMAIN_TAG_INJURIES_MEDICAL = 0x2A`, code const landed at T0; **no stream is registered and none may
  be** — ERR-041-012; ordinal 92 stays deliberately unallocated); wire the `IsAvailable` read into #30's squad selection; wire the
  FR-MD-025 roster-membership handoff (regen inserts `InjuryState.Create()`, retiree removes) at #30's
  season boundary. No #30 tick-order change beyond the KD-6 back-prop already filed (KD-6).
- **T3** — Deep tier: the distribution-driven severity draw + recurrence risk on early return, the
  ledger-derived `MatchLoad.HardContacts` per-fixture summary (read-only over the event ledger), and
  non-identity `MedicalModifier` consumption when #34 lands — all defaulting to their Stage-2 identities via
  `deepMedicalEnabled` (one code path, KD-4/KD-8).

## 7.2 Deferred (recorded, not built)

- **A dedicated #27 `InjuryProneness` attribute.** Stage 2 derives a robustness term from existing physical
  attributes (`Strength`/`Stamina`/`Balance`); a dedicated attribute is a future #27 append that would let
  the derived term become a direct read (KD-4). Not built here — avoids a #27 schema ripple in the minimal
  tier.
- **An injury→#28-decline input.** #41 could eventually expose a read-only signal #28's `GrowthProjection`
  reads (e.g. reduced growth during/after a serious injury) — #41 would remain a value producer, never a
  second attribute writer (the #29 KD-2 / FR-PG-008 sole-writer contract, transitively honoured). Not built
  here.
- **Recurrence risk on early return.** Stage-3 deep-tier extension; defaults to "no recurrence" at Stage 2
  (KD-4).
- **Ledger-derived per-fixture physical load (`MatchLoad.HardContacts`).** Deep-tier read-only derivation
  over the event ledger (KD-3); Stage 2 uses `AppearanceDays` only.
- **A genuinely stochastic recovery model** (e.g. variable-length recovery with its own draw). If a later
  extension adds this, it appends a **new** draw-purpose ordinal (never renumbering `Occurrence = 0`,
  FR-MD-008) into the keyed derivation's `DeriveActionOrdinal` radix — there is no stream, and no second
  anything is needed, since the derivation is already keyed per-purpose (ERR-041-012).

## 7.3 Seam contracts recorded for downstream authors

- **#34 (medical staff):** becomes the producer of a non-identity `MedicalModifier`. The routing seam is a
  value parameter; #34 MUST NOT add a second occurrence-risk or recovery-speed path — it supplies the
  modifier #41 already reads (the KD-5 identity contract).
- **#28 (progression):** MAY later read a read-only injury signal from #41 as an input to
  `GrowthProjection`; #41 MUST NOT gain a write path into `PlayerAttributes` (the KD-2 boundary #29 already
  established, cross-checked here).
- **#29 (training):** #41 MUST continue to read `InjuryRiskContribution` read-only; #29 MUST NOT gain an
  injury model of its own (the KD-2/KD-5 boundary, mirrored from #29's own §7.3 contract naming #41 as the
  consumer).
- **#30 (season loop):** owns the roster-membership handoff (FR-MD-025) and the tick-order invocation
  (KD-6); #41 MUST NOT reference #30 or drive its own roster-membership updates independently (the one-way
  composition, FR-MD-026).
- **#37/#44 (analytics/discipline, future):** the deep-tier `MatchLoad.HardContacts` ledger derivation MUST
  stay read-only over the same Tier-A event ledger those specs already consume — #41 MUST NOT become a
  second, competing ledger consumer with divergent semantics (KD-3).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial T-phase plan (T0–T3) + deferred extensions + downstream seam contracts. Status IN REVIEW. |
| 0.2 | 2026-08-08 | — | **Balance-pass AR pass 7 (M1)**: §7.1's T2 line ordered registering `injuries.occurrence` — the registration §4.5 (as rewritten at D4) forbids and ERR-041-012 records as never-existed; §7.2's extension clause assumed the same stream. Both re-anchored to the keyed derivation. The T-phase file a deep-tier author reads pointed the wrong way. |
#endregion
