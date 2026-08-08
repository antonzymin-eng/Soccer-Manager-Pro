# Injuries & Medical #41 — Appendices

**Created:** July 23, 2026
**Last Updated:** August 8, 2026, even later same day (v0.10 — balance-pass AR pass 12 M3: the recovery rate's positivity enforced at the countdown site)
**Last Updated (prior):** August 8, 2026, still later same day (v0.9 — balance-pass AR pass 11 L3: the split invariant adds non-negativity)
**Last Updated (prior):** August 8, 2026, later same day (v0.8 — balance-pass AR pass 10 M1: the split invariant's Appendix A row records its runtime enforcement site)
**Last Updated (prior):** August 8, 2026 (v0.7 — balance-pass AR pass 9 L5: the severity-split catalogue invariant is STRICT — ≤ at exactly 1000 makes Serious unreachable with the invariant satisfied)
**Last Updated (prior):** August 8, 2026 (v0.6 — balance-pass AR pass 8 (L4): the DRAW_PURPOSE_OCCURRENCE row's "on `injuries.occurrence`" → "of the keyed occurrence derivation")
**Last Updated (prior):** August 8, 2026 (v0.5 — balance-pass AR pass 7 M1: the ERR-041-012 sweep reaches the appendices — Appendix A no longer asserts the T2 stream registration §4.5 forbids, and Appendix C's T-MD-NEU-003 matches §5.5's restatement instead of contradicting it under the same test id)
**Last Updated (prior):** August 7, 2026 (v0.4 — ERR-041-011 at the balance pass: Appendix A's `INJURY_RISK_MAX` re-tagged `[CROSS: #29 Appendix A]` (discharging ERR-041-003's standing back-prop), `OCCURRENCE_DRAW_DENOM` re-tagged `[FIXED]` 1,000,000 (decoupled), `APPEARANCE_LOAD_WEIGHT` refitted 150 → 5600 on the new scale, + `BASELINE_DAILY_RISK` 4000 and `APPEARANCE_WINDOW_DAYS` 7)
**Last Updated (prior):** July 23, 2026 (v0.3 — AR-2 fixed-radix append-parity; prior v0.2 AR-1 integer fix, v0.1 initial)
**Version:** 0.10
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
| `RECOVERY_DAYS_PER_TICK_BASE` | 1 | [GT] | Stage-2 linear recovery-countdown rate: a **fixed integer** number of `RecoveryRemaining` days consumed per world day. Staff recovery-speed does NOT scale this per-tick (it scales assigned tier-days at injury time — §3.1/FR-MD-014). MUST be **positive** — **enforced fail-loud at the countdown site** (§3.1; non-positive makes every injury permanent, silently; AR pass 12 M3). |
| `MEDICAL_MODIFIER_IDENTITY_PERMILLE` | 1000 | [FIXED] | Per-mille identity for `MedicalModifier.OccurrenceRiskMillMult` / `RecoverySpeedMillMult` (= ×1.0). `MedicalModifier.Identity` sets both to this; `default(MedicalModifier)` (all-zero) is NOT valid (FR-MD-016 / F4). |
| `RecoveryDaysForTier[Minor]` | 7 | [GT] | Fixed recovery-days constant for `InjurySeverity.Minor` (§3.2). |
| `RecoveryDaysForTier[Moderate]` | 21 | [GT] | Fixed recovery-days constant for `InjurySeverity.Moderate`. |
| `RecoveryDaysForTier[Serious]` | 60 | [GT] | Fixed recovery-days constant for `InjurySeverity.Serious`. |
| `SEVERITY_PERMILLE_DENOM` | 1000 | [FIXED] | Denominator for the integer per-mille severity bucketing (§3.2 uses `draw×DENOM < risk×numerator` — no float division). |
| `SEVERITY_MINOR_PERMILLE` | 600 | [GT] | Per-mille numerator of the occurrence-draw range (below the risk threshold) classified `Minor` (§3.2). Equivalent to the 0.60 fraction, expressed as an integer to keep bucketing float-free. |
| `SEVERITY_MODERATE_PERMILLE` | 300 | [GT] | Per-mille numerator classified `Moderate` (cumulative with Minor: 900); the remaining 100‰ is `Serious`. `SEVERITY_MINOR_PERMILLE + SEVERITY_MODERATE_PERMILLE` MUST be **<** `SEVERITY_PERMILLE_DENOM` (a catalogue invariant — strict: at a sum of exactly 1000 the §3.2 second bucket's bound `draw × DENOM < risk × 1000` is the method's own precondition and `Serious` becomes unreachable with the invariant "satisfied"). **Enforced fail-loud at the classifying site** (§3.2 — the `InjuryRiskMax ≤ OCCURRENCE_DRAW_DENOM` draw-site posture; both numerators are `[GT]` config keys and the catalogue suite only sees the fallbacks). Both numerators MUST also be **non-negative** — zero is a deliberate empty tier, negative silently deletes one (same guard, AR pass 11 L3). |
| `INJURY_RISK_MAX` | 16000 | [CROSS: #29 Appendix A] | Occurrence-risk-score clamp ceiling — mirrored read-only from `TrainingSystemConstants.InjuryRiskMax`, never a second config key (ERR-041-003: one owner, one key). Sets the daily probability CEILING `INJURY_RISK_MAX / OCCURRENCE_DRAW_DENOM` (1.6% today; raised 10000 → 16000 at the balance-pass AR — at 10000, baseline + one appearance = 9,600 compressed the #29 and robustness terms into ≤4% of the range for every player who played); MUST be ≤ `OCCURRENCE_DRAW_DENOM` (fail-loud at the draw site). |
| `OCCURRENCE_DRAW_DENOM` | 1,000,000 | [FIXED] | The keyed draw's output range `[0, OCCURRENCE_DRAW_DENOM)` — per-million probability resolution. **DECOUPLED from `INJURY_RISK_MAX` at ERR-041-011** (the old `== INJURY_RISK_MAX` derivation is retired): the draw is `hash % denominator`, so a config-tunable denominator re-rolls every career's draws; pinned, config edits move only thresholds. |
| `TRAINING_RISK_PASSTHROUGH_WEIGHT` | 1 | [GT] | Integer weight applied to #29's `InjuryRiskContribution.RiskScore` in the risk-score assembly (§3.4). Integer, not float (FR-MD-014). |
| `APPEARANCE_LOAD_WEIGHT` | 5600 | [GT] | Risk-score contribution per `MatchLoad.AppearanceDays` (Stage-2 match-load term), on the per-million scale — one appearance contributes for the whole window, ≈ 3.9% cumulative per match at 7 × 5600 (fitted at the balance pass: an ever-present starter carries ~1.5 match-driven injuries per 38-round season, the E-1 match:training split; was 150 on the pre-ERR-041-011 scale). |
| `BASELINE_DAILY_RISK` | 4000 | [GT] | The exposure-independent daily base risk (ERR-041-011), added BEFORE the mitigation (§3.4 — position normative, so robustness discriminates it). Fitted so a non-playing squad member carries ~1 injury/season; what keeps the default focus from converging on injury-proof-forever. The R-2 under-exposure arm must re-fit against this, not add beside it. |
| `APPEARANCE_WINDOW_DAYS` | 7 | [GT] | The FR-MD-010 window: an appearance counts toward the risk for this many days after the match, never including the current day (ERR-030-027 — the draw runs pre-round). Structurally bounded to `[1, 31]` by #30's u32 bitmask record (fail-loud outside it). |
| `HARD_CONTACT_WEIGHT` | 0 (Stage 2) | [GT] | Risk-score contribution per `MatchLoad.HardContacts`; zero at Stage 2 (the field is deep-tier only, KD-3); a future non-zero Stage-3 value is a config-dial change, not a formula rewrite. |
| `RobustnessMitigation` weights | table | [GT] | Deterministic own-attribute (e.g. `Strength`/`Stamina`/`Balance`) mitigation subtracted from the assembled risk score — never RNG (FR-MD-015). |
| `DRAW_PURPOSE_OCCURRENCE` | 0 | [FIXED] | The sole Stage-2 draw-purpose ordinal of the keyed occurrence derivation. APPEND-only (FR-MD-008) — a future deep-tier purpose (e.g. recurrence) appends the next ordinal, never renumbering this one. |
| `DRAW_PURPOSE_RADIX` | 16 | [FIXED] | **Fixed** radix for `DeriveActionOrdinal`'s `worldDay × RADIX + purpose` bijection (§3.1.1). MUST be constant across all versions and MUST exceed the largest purpose ordinal ever defined — using the growing purpose *count* instead would shift every prior `(worldDay, Occurrence)` ordinal on an append, breaking replay/save parity (the hazard FR-MD-008 prevents). 16 leaves ample headroom for Stage-3 purposes; every `purpose` MUST be `< DRAW_PURPOSE_RADIX`. |

**`DOMAIN_TAG_INJURIES_MEDICAL` / `SubsystemOrdinals.InjuriesMedical`** — `0x2A` / `92` respectively, per
`docs/tracking/injuries-medical-design.md` §5 and the roadmap §6 reservation; promoted at section-file
approval (ERR-041-001, spec-text-first like `0x22`/`0x20`); the code const landed at #41 T0;
**no stream registration exists or may be added** (ERR-041-012 — a registered stream is
cursor-positioned, forbidden by FR-MD-006/007; FR-LW-031's no-phantom rule is why ordinal 92 stays
deliberately unallocated). These
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
identity (T-MD-NEU-002). Stream independence is vacuous by construction since ERR-041-012 — #41 registers nothing and
holds no cursor — so every other registered stream's cursor is byte-identical across a full season run,
with or without #41 active (T-MD-NEU-003, as restated in §5.5).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial constant catalogue + worked examples (mid-recovery + post-fixture-draw save/restore; behaviour-neutral identity). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): integer-arithmetic fix — `SEVERITY_*_PERMILLE` + `SEVERITY_PERMILLE_DENOM` replace the float severity fractions; `MEDICAL_MODIFIER_IDENTITY_PERMILLE` added; `RECOVERY_DAYS_PER_TICK_BASE` / `TRAINING_RISK_PASSTHROUGH_WEIGHT` clarified integer. |
| 0.3 | 2026-07-23 | — | AR-2 (1M): `DRAW_PURPOSE_COUNT` [DERIVED] replaced by `DRAW_PURPOSE_RADIX` = 16 [FIXED] (append-parity radix). |
| 0.4 | 2026-08-07 | — | **ERR-041-011 (the balance pass)**: `INJURY_RISK_MAX` → `[CROSS: #29 Appendix A]` (ERR-041-003 discharged — one owner, one config key; now the probability ceiling — 16000 = 1.6%/day after the AR pass-1 headroom raise — ≤ `OCCURRENCE_DRAW_DENOM` fail-loud); `OCCURRENCE_DRAW_DENOM` → `[FIXED]` 1,000,000 (a config-tunable denominator re-rolls every career's draws); `APPEARANCE_LOAD_WEIGHT` 150 → 5600 (per-million scale, ≈3.9%/match over the window); + `BASELINE_DAILY_RISK` 4000 (before-mitigation, the R-2 refit note) and `APPEARANCE_WINDOW_DAYS` 7 (the FR-MD-010 unit, bounded [1,31] by #30's record). |
| 0.5 | 2026-08-08 | — | **Balance-pass AR pass 7 (M1)**: Appendix A asserted "the `injuries.occurrence` stream registration itself lands at #41 T2" while citing the anti-phantom FR; Appendix C defined T-MD-NEU-003 as the registration's independence while §5.5 (pass 6) had restated the same id as vacuous-by-construction — one test id, two contradictory definitions. Both re-anchored. |
| 0.6 | 2026-08-08 | — | **Balance-pass AR pass 8 (L4)**: the `DRAW_PURPOSE_OCCURRENCE` row still anchored the ordinal "on `injuries.occurrence`" — a stream that must never exist — in the appendix pass 7 bumped for this class. |
| 0.7 | 2026-08-08 | — | **Balance-pass AR pass 9 (L5)**: the severity-split invariant `Minor + Moderate ≤ DENOM` corrected to strict `<` — the same row states "the remaining 100‰ is Serious", and at a sum of exactly 1000 the §3.2 second bucket's bound is the method's own precondition, so `Serious` is unreachable with "≤" satisfied. Catalogue doc + lock (`Assert.Less`) corrected with it. |
| 0.8 | 2026-08-08 | — | **Balance-pass AR pass 10 (M1)**: the split-invariant row records its runtime enforcement (`ClassifySeverityFromDraw` fail-louds at the classifying site — the draw-site guard posture); until now this was the only one of the three catalogue invariants with no production guard, and the only lock ran config-unbound. |
| 0.9 | 2026-08-08 | — | **Balance-pass AR pass 11 (L3)**: the split invariant gains non-negativity — a negative `[GT]` numerator deleted its tier through the sum guard's own blind spot; zero stays legal. |
| 0.10 | 2026-08-08 | — | **Balance-pass AR pass 12 (M3)**: the recovery rate's row records its runtime enforcement — the one `[GT]` here whose lock had no runtime mirror, and whose silent failure (every injury permanent) is worse than the deleted tier the split guard stops. |
#endregion
