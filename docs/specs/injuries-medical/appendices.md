# Injuries & Medical #41 — Appendices

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.3 — AR-2 fixed-radix append-parity; prior v0.2 AR-1 integer fix, v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Every constant carries exactly one source tag. Magnitudes marked `[GT]` are illustrative pending a future
Stage-2/3 balance pass (the #21 G2 precedent); the shapes/directions are the reviewed contract.

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `MEDICAL_SAVE_FORMAT_VERSION` | 1 | [FIXED] | The #41 sub-blob version (KD-7). A season-save sub-blob, independently gated from `WORLD_STORE_FORMAT_VERSION` / `SEASON_STATE_FORMAT_VERSION` / `TRAINING_SAVE_FORMAT_VERSION` / `PROGRESSION_SAVE_FORMAT_VERSION`. |
| `MEDICAL_NOT_ADVANCED_SENTINEL` | `uint.MaxValue` | [FIXED] | "Never advanced" seed for `LastAdvancedWorldDay` — chosen so a legitimate world-day 0 cannot collide with the fresh-state value (the day-0 double-accrual trap, the #28/#29 lifecycle precedent, F6). |
| `RECOVERY_MAX` | 240 | [GT] | Ceiling on `RecoveryRemaining` (world-days) — generously bounds even a Stage-3 deep-tier recurrence-extended recovery; a Stage-2 Serious-tier injury never approaches this ceiling. |
| `RECOVERY_DAYS_PER_TICK_BASE` | 1 | [GT] | Stage-2 linear recovery-countdown rate: a **fixed integer** number of `RecoveryRemaining` days consumed per world day. Staff recovery-speed does NOT scale this per-tick (it scales assigned tier-days at injury time — §3.1/FR-MD-014). |
| `MEDICAL_MODIFIER_IDENTITY_PERMILLE` | 1000 | [FIXED] | Per-mille identity for `MedicalModifier.OccurrenceRiskMillMult` / `RecoverySpeedMillMult` (= ×1.0). `MedicalModifier.Identity` sets both to this; `default(MedicalModifier)` (all-zero) is NOT valid (FR-MD-016 / F4). |
| `RecoveryDaysForTier[Minor]` | 7 | [GT] | Fixed recovery-days constant for `InjurySeverity.Minor` (§3.2). |
| `RecoveryDaysForTier[Moderate]` | 21 | [GT] | Fixed recovery-days constant for `InjurySeverity.Moderate`. |
| `RecoveryDaysForTier[Serious]` | 60 | [GT] | Fixed recovery-days constant for `InjurySeverity.Serious`. |
| `SEVERITY_PERMILLE_DENOM` | 1000 | [FIXED] | Denominator for the integer per-mille severity bucketing (§3.2 uses `draw×DENOM < risk×numerator` — no float division). |
| `SEVERITY_MINOR_PERMILLE` | 600 | [GT] | Per-mille numerator of the occurrence-draw range (below the risk threshold) classified `Minor` (§3.2). Equivalent to the 0.60 fraction, expressed as an integer to keep bucketing float-free. |
| `SEVERITY_MODERATE_PERMILLE` | 300 | [GT] | Per-mille numerator classified `Moderate` (cumulative with Minor: 900); the remaining 100‰ is `Serious`. `SEVERITY_MINOR_PERMILLE + SEVERITY_MODERATE_PERMILLE` MUST be ≤ `SEVERITY_PERMILLE_DENOM` (a catalogue invariant). |
| `INJURY_RISK_MAX` | 10000 | [GT] | Occurrence-risk-score clamp ceiling — the same scale #29's `InjuryRiskContribution.RiskScore` uses (§3.4). |
| `OCCURRENCE_DRAW_DENOM` | = `INJURY_RISK_MAX` (10000) | [DERIVED] | The keyed draw's output range `[0, OCCURRENCE_DRAW_DENOM)` — derived to match `INJURY_RISK_MAX` so the assembled risk score compares directly against the draw with no extra scale factor (§3.1/§3.4). |
| `TRAINING_RISK_PASSTHROUGH_WEIGHT` | 1 | [GT] | Integer weight applied to #29's `InjuryRiskContribution.RiskScore` in the risk-score assembly (§3.4). Integer, not float (FR-MD-014). |
| `APPEARANCE_LOAD_WEIGHT` | 150 | [GT] | Risk-score contribution per `MatchLoad.AppearanceDays` (Stage-2 match-load term). |
| `HARD_CONTACT_WEIGHT` | 0 (Stage 2) | [GT] | Risk-score contribution per `MatchLoad.HardContacts`; zero at Stage 2 (the field is deep-tier only, KD-3); a future non-zero Stage-3 value is a config-dial change, not a formula rewrite. |
| `RobustnessMitigation` weights | table | [GT] | Deterministic own-attribute (e.g. `Strength`/`Stamina`/`Balance`) mitigation subtracted from the assembled risk score — never RNG (FR-MD-015). |
| `DRAW_PURPOSE_OCCURRENCE` | 0 | [FIXED] | The sole Stage-2 draw-purpose ordinal on `injuries.occurrence`. APPEND-only (FR-MD-008) — a future deep-tier purpose (e.g. recurrence) appends the next ordinal, never renumbering this one. |
| `DRAW_PURPOSE_RADIX` | 16 | [FIXED] | **Fixed** radix for `DeriveActionOrdinal`'s `worldDay × RADIX + purpose` bijection (§3.1.1). MUST be constant across all versions and MUST exceed the largest purpose ordinal ever defined — using the growing purpose *count* instead would shift every prior `(worldDay, Occurrence)` ordinal on an append, breaking replay/save parity (the hazard FR-MD-008 prevents). 16 leaves ample headroom for Stage-3 purposes; every `purpose` MUST be `< DRAW_PURPOSE_RADIX`. |

**`DOMAIN_TAG_INJURIES_MEDICAL` / `SubsystemOrdinals.InjuriesMedical`** — `0x2A` / `92` respectively, per
`docs/tracking/injuries-medical-design.md` §5 and the roadmap §6 reservation; promoted at section-file
approval (ERR-041-001, spec-text-first like `0x22`/`0x20`); the code const + the `injuries.occurrence`
stream registration itself land at #41 T2 with the first draw site (FR-LW-031 — no phantom stream). These
are **not** `[GT]`/`[FIXED]` project constants declared in this catalogue — they are `#16`'s tag-namespace
allocation, cross-cited `[CROSS: #16 §3.4]` once promoted.

## Appendix B — Worked example: save/restore across a mid-recovery AND a post-fixture-draw boundary

**Mid-recovery boundary.** Seed (from §3.6): player 501, world day 208, `InjuryState { Severity = Minor,
RecoveryRemaining = 3, InjuryCount = 1, LastAdvancedWorldDay = 208 }`. Save now; restore. The four fields
restore field-identical. Advancing day 209: `wasAvailableAtEntry = false` (still `Minor`); countdown
`RecoveryRemaining = 2`; no occurrence draw. This is identical to an uninterrupted run that never saved
(T-MD-DET-001) — there is no RNG cursor to diverge, because none is serialized (KD-1/FR-MD-007).

**Post-fixture-draw boundary.** Player 501, world day 213 (healthy entering the call, per §3.6). The
occurrence draw resolves — suppose this time `draw = 6200` against `risk = 2850` (no occurrence, since
`6200` is not `< 2850`); `InjuryState` is unchanged except `LastAdvancedWorldDay = 213`. Save immediately
after this draw resolves; restore. Advancing day 214 draws again, keyed on `(501, 214, Occurrence)` — a
*different* key from `(501, 213, Occurrence)`, so it is unaffected by whether day 213's draw happened before
or after a save/restore boundary (T-MD-DET-002/003) — the position-independent property means "immediately
after a draw" carries no special state to lose.

## Appendix C — Worked example: behaviour-neutral identity (KD-8)

With `occurrenceEnabled` off (Stage-2-minus-occurrence configuration used only for the identity proof, not a
normal operating mode), `AdvanceMedicalDay` for every input still runs the recovery countdown (if currently
injured) but never evaluates §3.1 step 2 — no draw is issued, so `InjuryState.Severity` can only ever
decrease toward `None` and never increase (T-MD-NEU-001). `InjuryState.Create()` yields `{ Severity = None,
RecoveryRemaining = 0, InjuryCount = 0, LastAdvancedWorldDay = MEDICAL_NOT_ADVANCED_SENTINEL }` — the Healthy
identity (T-MD-NEU-002). Registering the `injuries.occurrence` stream at #41 T2 leaves every other
registered stream's cursor byte-identical across a full season run, with or without #41 active (T-MD-NEU-003)
— the keyed-draw property means the new stream's presence changes nothing about how any other stream is
addressed or advanced.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial constant catalogue + worked examples (mid-recovery + post-fixture-draw save/restore; behaviour-neutral identity). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): integer-arithmetic fix — `SEVERITY_*_PERMILLE` + `SEVERITY_PERMILLE_DENOM` replace the float severity fractions; `MEDICAL_MODIFIER_IDENTITY_PERMILLE` added; `RECOVERY_DAYS_PER_TICK_BASE` / `TRAINING_RISK_PASSTHROUGH_WEIGHT` clarified integer. |
| 0.3 | 2026-07-23 | — | AR-2 (1M): `DRAW_PURPOSE_COUNT` [DERIVED] replaced by `DRAW_PURPOSE_RADIX` = 16 [FIXED] (append-parity radix). |
#endregion
