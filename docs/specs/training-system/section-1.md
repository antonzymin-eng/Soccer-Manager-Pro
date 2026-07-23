# Training System #29 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.3 — PASS-2 re-review; prior APPROVED)
**Version:** 0.3
**Status:** APPROVED

---

## 1.1 Introduction

The Training System advances each player's off-pitch preparation on the **world tick** (`WorldClock`, one
day = one `worldTick` — distinct from the 10 Hz tactical / 60 Hz physics match loops). A manager assigns a
per-player training **focus** (a weekly-cadence command); the world-tick day step then applies a
**deterministic** daily delta to a **conditioning** cursor and a **training-fatigue** accumulator, and — at
the deep tier — contributes a granular per-attribute growth **input** to Player Progression #28's CA/PA
curve.

## 1.2 Scope

**In scope:** the per-player training focus + schedule; the daily conditioning / training-fatigue
projection; the pure per-player growth-input contribution consumed by #28; the one-directional projection
of world-tick training-fatigue into the match-boot starting fatigue; the injury-risk output read by #41;
and the persistent training sub-blob under #30's season save.

**Out of scope (owned elsewhere):**
- The attribute-growth **curve** — #28's `GrowthProjection` is the sole attribute-mutation path (KD-2).
- Injury occurrence / severity / recovery — #41's model; #29 supplies a risk input only (KD-5).
- Coaching-staff attributes — #34's; #29 exposes an identity routing seam (KD-3).
- In-**match** fatigue (`1 − AerobicPool`) and any match-side per-agent form/context — the match engine's
  own concern; #29 reconciles only via the KD-1 starting-fatigue projection.
- **Match-participation-driven sharpness / morale ("form")** — a future owner's concern; #29 owns only the
  training-driven conditioning it can fully compute (a training spec must not become a shared writer of a
  match-driven concept).

## 1.3 Dependencies

| Spec | Relationship | Direction |
|---|---|---|
| #27 Squad/Player Data | reads `PlayerRecord` / `PlayerAttributes` | #29 → #27 |
| #28 Player Progression | constructs the `TrainingInput` value #28 reads | #29 → #28 |
| #16 Deterministic Sim | consumes the determinism namespace (no stream — KD-6) | #29 → #16 |
| #30 Season & Competition Loop | #30's day-advance loop invokes #29's step + the pure read | #30 → #29 |
| #34 Coaching (future) | supplies a non-identity `CoachingModifier` when it lands | #34 → #29 |
| #41 Injuries (future) | reads the `InjuryRiskContribution` output | #41 reads #29 |
| #31 / #38 (future) | read the `TrainingViewModel` observer (value copies, KD-7) | #31/#38 read #29 |

Reference DAG: `#30 → {#28, #29}`, `#29 → #28`, `{#28,#29} → {#27,#16}`. **Acyclic.** #28's assembly stays
schema-untouched (the `TrainingInput` append point is #28's own reserved extension).

## 1.4 Key decisions

- **KD-1 (fatigue reconciliation — the headline risk).** Training-fatigue is a #29-owned **integer
  world-tick accumulator**; match-tick fatigue is `1 − AerobicPool`. The two **never share a counter**.
  Reconciliation is a pure, one-directional `ProjectMatchEntryFatigue(in TrainingState) → float [0,1]`
  feeding the caller-supplied `float fatigue` at match boot (the `PlayerAttributeProjection` KD-P4 seam).
  Match-tick fatigue never writes back; #29 never touches `AerobicPool`. The projection is **not stored**
  (a pure function of the serialized accumulator), so save→restore is byte-exact and no double-count is
  representable — one accumulator, one read.

- **KD-2 (growth seam — single-owner attribute mutation).** #29 writes attributes **only** by populating
  #28's `TrainingInput`; `GrowthProjection` stays the sole attribute writer. #30 gathers each player's
  `ComputeTrainingInput` result into the batch #28's public `AdvanceDay(worldDay, in trainingInputs)`
  consumes (FR-PG-021) at the **slot-1** progression seam, while the mutating `AdvanceTrainingDay` runs at
  **slot-2** — #30's documented tick order is honored, with no reorder. The slot-1 read is order-independent
  of the slot-2 mutation because `ComputeTrainingInput` reads only fields `AdvanceTrainingDay` does not
  mutate (`Focus`/attributes/coach — the FR-TR-006 invariant), **not** merely because it is pure.

- **KD-3 (coaching modulation — identity routing seam).** The step takes `in CoachingModifier`, default
  `Identity` (×1.0). No #34 interface is built (FR-LW-031) — the routing-seam-as-identity pattern
  (#28's `TrainingInput.Neutral`).

- **KD-4 (cadence — daily accrual, weekly focus).** The world-tick step runs **daily**; the **focus is a
  persistent field** set by a weekly command. **No weekly batch boundary** and **no rollover step** — the
  #28 "no discrete rollover" resolution — so nothing can be double-counted. `LastAdvancedWorldDay` guards a
  day against being advanced twice.

- **KD-5 (injury-risk output — shaped for #41, not owned).** #29 exposes a read-only
  `InjuryRiskContribution` scalar; **#41 reads it** and owns the injury model. No #41 interface is built.

- **KD-6 (determinism — no stream).** #29 is fully deterministic: conditioning / fatigue / growth-input are
  pure integer projections; per-player variation is a deterministic function of the player's own attributes.
  It registers **no RNG stream** and does **not** promote `0x21`/83, which stay reserved. #28 promoted
  `0x20` only because **regen is a genuine draw site**; #29 has no analogous #29-owned stochastic outcome, so
  a named stream would be the phantom-surface class FR-LW-031 forbids.

- **KD-7 (persistence + roster-membership lifecycle).** Opaque `TRAINING_SAVE_FORMAT_VERSION` sub-blob under
  #30's season save (`SeasonSaveCodec` pattern); fail-loud gates; serialize-don't-regenerate (#30 KD-5). The
  per-club `TrainingState` set (keyed by `PlayerId`, focus is the single source of truth on
  `TrainingState.Focus` — no duplicate schedule copy) tracks roster membership **in lockstep with #28's
  season-boundary churn** (FR-TR-025): a #28 regen inserts a `TrainingState.Create(Balanced)` for the fresh
  `PlayerId`; a retirement removes the retiree's entry — the FR-PG-011 remove/insert parallel. Regens use
  `Create` (never `default`), so the day-0 sentinel invariant holds for new players too.

- **KD-8 (behaviour-neutral identity).** Attribute-growth dial off + `CoachingModifier.Identity` +
  `TrainingInput.Neutral` ⇒ #29 evolves only its own conditioning / training-fatigue and never touches #28's
  attributes/CA/PA. The Stage-2 minimal surface is the identity the deep tier extends.

## 1.5 Determinism posture

Byte-exact by construction: all cursors are integers, all deltas are pure integer projections, and there is
no RNG. Save/restore is exact because every field is serialized (KD-7) and the fatigue projection is
recomputed, not stored (KD-1).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | PASS-1 → AR-2 → AR-3; APPROVED. |
| 0.3 | 2026-07-23 | — | PASS-2: §1.3 +#31/#38 observer row; KD-7 gains the FR-TR-025 regen/retire roster-membership lifecycle clause. |
#endregion
