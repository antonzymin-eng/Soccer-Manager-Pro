# Goalkeeper Mechanics #11 — Section-Files Pass-1 Adversarial Review

**Created:** May 16, 2026
**Reviewer:** self-pass-1 against `outline-detailed.md` v1.2 and
project invariants in CLAUDE.md.
**Scope:** Section files `section-1.md` through
`section-9-approval-checklist.md` plus `appendices.md`, all at
v0.1.

**Severity legend:** H = blocks `IN REVIEW` declaration; M = must
resolve before approval; L = follow-up worth tracking.

**Result:** 11 findings (3 H / 5 M / 3 L). All resolved in v0.2.

---

## H — High severity

### AR-S1-H1 — §6 budget contradiction (steady-state)

`§6.1` declares steady-state per-tick budget ≤30 µs `[EST]`. `§6.3.1`
decomposes the components and arrives at ≈40 µs / match (2 GKs).
The decomposition exceeds the headline budget by ~33%, but the
prose merely flags `[EST]` without resolving the contradiction.

**Resolution.** §6.1 budget revised to ≤40 µs `[EST]` to match the
component decomposition, and §6.3.1 prose explicitly notes the
revision. §4.5.2 updated to track. Mirrors Heading #10 H-4
reconciliation: the worst-case decomposition is the binding budget,
not an aspirational headline number.

### AR-S1-H2 — `spillVelocity` Gaussian violates KD-7 single-purpose-per-site

`§3.5.3 spillVelocity` calls `rng.NextGaussian(DRAW_SITE_HANDLING_NOISE,
DOMAIN_TAG_GOALKEEPER)` for the deflection-angle perturbation. This
reuses `DRAW_SITE_HANDLING_NOISE` for a second purpose
(handling-scale noise vs. spill-angle noise), violating #16 §4.5
single-purpose-per-site rule explicitly called out in KD-7 / §3.5
note.

**Resolution.** Remove the Gaussian draw from `spillVelocity`; the
deflection angle becomes a deterministic function of `quality`
(`PARRY_DEFLECT_ANGLE_SIGMA_RAD · (1 − quality)`). Variability in
spill outcome is fully captured by `handlingScaleNoise` and
`pointErrorNoise` upstream of the band-to-action dispatch; a
second Gaussian at the helper level was redundant. §3.5.3 updated.
The draw-site count stays at 4 (matches §4.4.2, KD-7, FR-GK-010).

### AR-S1-H3 — `HANDLING_K_BALL_SPEED` unit mismatch

§3.4.5 declares `HANDLING_K_BALL_SPEED` unit as `per m/s`. The
formula in §3.5.1 uses it as `HANDLING_K_BALL_SPEED · max(0,
ballSpeed − ref) / ref`, which evaluates to dimensionless. The
`per m/s` unit would be correct only if the formula were
`HANDLING_K_BALL_SPEED · (ballSpeed − ref)` directly. The
division by `ref` cancels the m/s — the constant is dimensionless.

**Resolution.** §3.4.5 row updated to `dimensionless`. Value
range stays the same.

---

## M — Medium severity

### AR-S1-M1 — `BALL_ATTACKING_THIRD_X_M` citation incorrect

§3.4.2 cites `[DERIVED]` from "§3.1" but the derivation is from
`PITCH_LENGTH_M` (Ball Physics #1 §1.2). Citation column should
read "derived from #1 §1.2 `PITCH_LENGTH_M`".

**Resolution.** §3.4.2 citation column updated.

### AR-S1-M2 — Missing FR for §3.8.5 6-second-rule forced release

§3.1.1 transition table includes a "forced release" self-loop at
`HandsOnBall → HandsOnBall` when `currentTick − claimTick ≥
GK_HOLD_MAX_TICKS`. §5.1.8.5 tests the forced release with a
default ROLL distribution. But no FR in §2.1 codifies the forced
release behaviour. Without an FR row, the §5 test gate has no
upstream spec hook.

**Resolution.** Add `FR-GK-043` (MUST): "After
`GK_HOLD_MAX_TICKS` (6-second rule) without a `DistributeIntent`,
the GK forces a default ROLL distribution to the nearest own-team
agent within penalty area." Source KD-9 / Laws of the Game.

### AR-S1-M3 — `Throwing` and `Kicking` attribute reads claimed but unused

§1.4 dependency table and §4.2 input contract table list
`Throwing` and `Kicking` as attribute reads. Neither appears in
any §3 formula. Either they must be wired into §3.8 distribution
quality, or removed from the consumed-attributes list.

**Resolution.** Add a `distributionAccuracy` modulation in §3.8.1
using `Throwing_norm` (for Throw delivery) and `Kicking_norm` (for
Kick delivery) as multiplicative scalars on the emitted
`powerIntent`. New `[GT]` constants `THROW_ACCURACY_COEFF` and
`KICK_ACCURACY_COEFF` added to §3.4.7. Roll delivery does not
consume either attribute (low-skill action). Adds FR-GK-044.

### AR-S1-M4 — §3.6.1 priority-rule pseudocode missing constant

§3.6.1 body-part determination pseudocode references
`agent.handCapsule`, `agent.headSphere`, `agent.handZ`,
`agent.headZ` without specifying where these surfaces are declared.
The hand-capsule and head-sphere geometry are AM #2 #3 surfaces
not currently cited in §1.4 dependencies.

**Resolution.** Add a citation note to §3.6.1 specifying that
`agent.handCapsule` / `agent.headSphere` are Collision System #3
agent-shape surfaces (per #3 collider definitions); `agent.handZ`
/ `agent.headZ` derive from #3 collider centroids at the current
frame. §1.4 dependency row for #3 amended to include "agent
collider geometry" alongside `ICollisionEventConsumer`.

### AR-S1-M5 — Recovery state has no recovery-to-line trigger from non-Diving entries

§3.1.1 lists `Recovering → Set` triggered by "recovery-to-line
cooldown elapsed AND GK XY within `GK_REACTIVE_RADIUS_M` of #12
baseline." But during `Distributing → Recovering`, the GK is
typically near the goal line after a save. There is no transition
out of `Recovering` if the GK is already at baseline before the
cooldown elapses. The state machine can stall.

**Resolution.** §3.1.1 amended: `Recovering → Set` trigger now
reads "recovery-to-line cooldown elapsed (`RECOVERY_COOLDOWN_TICKS`)
OR GK XY already within `GK_REACTIVE_RADIUS_M` of #12 baseline."
Either condition releases `Recovering`. The cooldown still
enforces a minimum dwell on the failed-save / mistimed-dive paths
where the GK is OUT of baseline.

---

## L — Low severity

### AR-S1-L1 — §2.4 telemetry channel count discrepancy with §6.4

§2.4 declares 12 channels. §6.4 cites "channel allocations
declared in §2.4" without restating the count. §9.4 OI-002 says
"12 `gk.*` channel rows". The count is consistent at 12 but the
restatement in §6.4 would help cross-reference grep gates.

**Resolution.** §6.4 amended to "12 channels per §2.4" explicitly.

### AR-S1-L2 — `OneVsOne` reaction-coefficient sign documentation

§3.2.2 says the `OneVsOne` term is **subtractive** (positive
`OneVsOne` reduces required reaction time), but §3.4.3 just lists
`ONE_VS_ONE_REACTION_COEFF` with no sign indication. A reader
reading §3.4.3 alone could install the constant with the wrong
sign.

**Resolution.** §3.4.3 row for `ONE_VS_ONE_REACTION_COEFF` cites
"subtracts from `requiredReactionMs` per §3.2.2 sign" in the
Citation column.

### AR-S1-L3 — `[CROSS-PENDING]` plural in §3.4.9 — typo

§3.4.9 lists `DOMAIN_TAG_GOALKEEPER` as `[CROSS-PENDING]`. Tag
spelling matches CLAUDE.md exactly. No typo. Reclassify finding
as: §3.4.9 row could also reference the `ERR-011-001` back-prop
log entry explicitly.

**Resolution.** §3.4.9 row "Source" column updated to "ERR-011-001
back-prop entry in `docs/tracking/spec-error-log.md`".

---

## Pass-1 Resolution Summary

All 11 findings resolved in v0.2 fix pass. Section files updated:

- `section-2.md` v0.2 — FR-GK-043 (forced release) + FR-GK-044
  (distribution attribute consumption) added.
- `section-3.md` v0.2 — H1, H2, H3, M1, M4, M5, L2, L3 resolved.
- `section-4.md` v0.2 — §4.5.2 budget reconciliation (H1).
- `section-6.md` v0.2 — §6.1 / §6.3.1 budget reconciliation (H1);
  §6.4 channel-count restatement (L1).
- `section-1.md` v0.2 — §1.4 dependency row for #3 updated (M4).

Other section files unchanged at v0.1.

---

## Pass-2

Pass-2 self-critique applied to v0.2 section files. No
additional findings. v0.2 is the working draft; advancement to
`IN REVIEW` per `SPEC_INDEX.md` row 11 status flip proceeds.
