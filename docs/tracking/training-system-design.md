# Training System #29 — Design Supplement

> **Created:** July 23, 2026
> **Status:** DESIGN SUPPLEMENT → **PROMOTED** (11-file section set authored + APPROVED July 23, 2026).
> **Candidate spec:** #29 · **FR prefix:** FR-TR (grep-verified unclaimed across `docs/specs/**`).
> **Master-plan home:** §4.4 · **Wave:** 2.
> **Determinism:** **fully deterministic — no RNG stream; `_RESERVED_0x21_` / `SubsystemOrdinals` 83 stay
> reserved (NOT promoted).** See KD-6.
> **Source plan:** `docs/tracking/spec-plans/spec-29-training-system.md` v0.1.
> **AR history:** AR-1 (1H+1M+2L) → AR-2 (1M cross-fix) → AR-3 clean (CONVERGENCE).

---

## 0. Scope

Weekly-directed, **daily-accrued** training scheduled on the **world tick** (`WorldClock`, one day = one
`worldTick` — never the 10 Hz/60 Hz match loops). A per-player training **focus** produces deterministic
daily deltas to a **conditioning** cursor and a **training-fatigue** accumulator, plus (deep tier) a
granular per-attribute growth contribution that is an **input to #28's CA/PA curve** — never a parallel
attribute write.

**Out of scope (owned elsewhere, referenced as seams):**
- The attribute-growth **curve** itself — #28 owns `GrowthProjection` as the sole attribute-mutation path;
  #29 only *feeds* it a `TrainingInput` (KD-2).
- Injury **occurrence / severity / recovery** — #41 owns the model; #29 supplies a risk *input* (KD-5).
- Coaching-staff **attributes** — #34 owns them; #29 exposes an identity routing seam for their future
  modulation (KD-3).
- In-**match** fatigue (`1 − AerobicPool`, match-tick) and any match-side per-agent form/context — the
  match engine owns them; #29 reconciles only via a one-directional projection into the match-boot
  caller-supplied *starting* fatigue (KD-1).
- **Match-driven sharpness / morale / confidence** — a match-participation-driven "form" concept belongs to
  its future owner (match-minutes / morale), not a training spec. #29 owns only the *training-driven*
  conditioning it can fully compute (see M-1, §7 KD-8).

## 1. What exists vs. what #29 adds

**Exists (verified against source):**
- `src/player-database/PlayerAttributes.cs` — 31 `int[1,20]` attributes + `WeakFootRating[1,5]`. **There is
  no `Form` / `Fitness` / `Condition` / `Sharpness` field** (`Stamina` is a trainable *attribute* — the
  capacity ceiling — not a transient condition). So the conditioning cursor is genuinely new #29-owned
  state, not a duplicate of an existing field.
- `src/match-engine/PlayerAttributeProjection.cs` — the match-boot projections take a **caller-supplied
  `float fatigue`** ("live runtime state, never sourced from a squad", KD-P4). This is the exact seam KD-1's
  projection feeds; #29 modifies neither `PlayerAttributeProjection` nor `MatchEngine`.
- `docs/specs/player-progression-lifecycle/` (#28, APPROVED) — `GrowthProjection` is the **sole**
  attribute-mutation path; its daily step **reads `in TrainingInput`** (Neutral = identity, FR-PG-008/009);
  `TrainingInput` is a `readonly struct` with a documented append point: "Stage-3 #29 fields
  (focus/intensity/coach quality) append here; the daily step reads them." #28 registers **no** RNG stream
  for growth — growth is a deterministic integer projection (`0x20` covers regen generation only).
- `docs/specs/season-competition-loop/` (#30, APPROVED) — `WorldStore.AdvanceDay()` runs a **fixed,
  documented day-advance tick order** whose **slot 1 = progression (#28)** and **slot 2 = training (#29)**
  are reserved **null seams** today (FR-SN-034); `SeasonSaveCodec` composes opaque, independently
  version-gated sub-blobs; KD-5 = serialize-don't-regenerate.
- `deterministic-sim/section-3.md` — `_RESERVED_0x21_` / `SubsystemOrdinals` 83 are **held for #29**; this
  supplement confirms #29 needs no stream, so the reservation **stands** (see KD-6).

**#29 adds:** a per-player `TrainingState` (focus + conditioning + training-fatigue + last-day cursor);
a team training schedule; a **pure** `ComputeTrainingInput` read (feeds #28) and a **mutating**
`AdvanceTrainingDay` step (the slot-2 seam); the KD-1 match-entry-fatigue projection; the KD-5 injury-risk
observer surface; and the `TRAINING_SAVE_FORMAT_VERSION` sub-blob.

## 2. Staging (minimal-first → deep, one code path)

- **Stage-2 minimal** — the §4.4 "pick one of N focuses → affects conditioning only" model. Deterministic
  daily conditioning / training-fatigue deltas. **No attribute change** — the attribute-growth contribution
  is dialed to zero (`TrainingInput.Neutral`), so #28's curve is byte-identical to the no-training path.
- **Stage-3 deep** — granular per-attribute training that populates the `TrainingInput` fields #28 reads
  (a **deterministic** function of focus + attributes + coaching — no jitter; #28's curve stays
  deterministic).

**One code path (KD-8):** the deep-tier attribute-growth contribution and the coaching modulation both
default to their identities (dial off / `CoachingModifier.Identity`), so the Stage-2 surface is the exact
identity the deep tier extends — the #21 default-behaviour-neutral discipline, not a rewrite.

## 3. Dependencies & reference direction (one-way, no cycle)

- **#30 → #29** — the day-advance loop *invokes* #29's step at the reserved slot-2 seam and calls the pure
  `ComputeTrainingInput` at the slot-1 seam to feed #28. #29 never references #30.
- **#29 → #28** — #29 constructs the `TrainingInput` **value** #28 reads. #28 never references #29 (it reads
  a value handed in). This preserves #28's "`GrowthProjection` is sole writer" invariant.
- **#29 → #27, #16** — reads `PlayerRecord`/`PlayerAttributes`; consumes the determinism namespace.
- **#34 → #29** (future) — coaching modulation attaches as a value input (KD-3); no #34 interface today.
- **#41 reads #29** (future) — the injury-risk observer surface (KD-5); no #41 interface today.

Reference DAG: `#30 → {#28, #29}`, `#29 → #28`, `{#28,#29} → {#27,#16}`. Acyclic. #28's assembly stays
**schema-untouched** (the `TrainingInput` append point is #28's own reserved extension).

## 4. Persistent state & save impact (KD-7)

Adds an opaque, independently version-gated **training sub-blob** (`TRAINING_SAVE_FORMAT_VERSION`, [FIXED]
= 1) under #30's season save via the `SeasonSaveCodec` pattern — the composing format-version bump is
coordinated with #30, and the codec never parses the sub-blob's internals. Per club: each player's
`TrainingState` (focus enum, conditioning cursor, training-fatigue accumulator, last-advanced world-day) +
the team's `TrainingSchedule`. Fail-loud on version mismatch / out-of-bounds length prefix (overflow-safe
`ReadCount`) / trailing bytes (F3/F5, the `MatchSaveCodec` posture). **Serialize, don't regenerate** (#30
KD-5). Every field round-trip-covered.

## 5. Determinism (KD-6)

World tick only. **#29 is fully deterministic and registers NO RNG stream.** The Stage-2 conditioning /
training-fatigue deltas and the Stage-3 per-attribute growth contribution are all pure integer projections
of (focus, intensity, attributes, coaching) — the #28 growth-projection precedent, which registers no
stream. Per-player variation, where wanted for realism, is a **deterministic** function of the player's own
attributes (e.g. WorkRate), not an RNG draw.

Consequently `_RESERVED_0x21_` / `SubsystemOrdinals` 83 **stay reserved — NOT promoted**. #28 promoted
`0x20` because **regen is a genuine draw site** (random new-player attributes); #29 has no analogous
#29-owned stochastic outcome (growth flows through #28's deterministic curve; injury variation is #41's).
Promoting `0x21` to a named tag with a stream that never draws would be exactly the **phantom-surface class
FR-LW-031 forbids** (the `world.arcs` precedent). The reservation stands for a *future* stochastic training
extension; if one is ever designed, it promotes then. No `DETERMINISM_DIGEST_VERSION` bump; no code const.

**Training-fatigue is a world-tick accumulator, strictly separate from the match-tick fatigue** (`1 −
AerobicPool`). The two never share a counter; reconciliation is the one-directional KD-1 projection.

## 6. Primary surfaces (proposed → pinned in §4 of the section files)

```csharp
// #29-owned per-player world-tick training state (serialized, KD-7).
public struct TrainingState
{
    public TrainingFocus Focus;         // persistent; set by the weekly command (KD-4)
    public int Condition;               // ONE conditioning / match-readiness cursor, integer
                                        //   [CONDITION_MIN, CONDITION_MAX] — training-driven (M-1)
    public int TrainingFatigue;         // world-tick accumulator [0, TRAINING_FATIGUE_MAX] (NOT match fatigue)
    public uint LastAdvancedWorldDay;   // idempotency cursor — a day is advanced at most once (F6-class)
}

// Pure read → feeds #28's GrowthProjection at #30's slot-1 seam (KD-2). No mutation, no draw, no jitter.
public static TrainingInput ComputeTrainingInput(in TrainingState s, in PlayerAttributes a, in CoachingModifier coach);

// The slot-2 mutating step (KD-4): accrues conditioning + training-fatigue for one world day.
// Fully deterministic (no RNG parameter — see KD-6).
public static void AdvanceTrainingDay(ref TrainingState s, in PlayerAttributes a, in CoachingModifier coach,
                                      uint worldDay);

// KD-1: one-directional projection of world-tick training-fatigue → the match-boot caller-supplied
// starting fatigue [0,1] (the PlayerAttributeProjection `float fatigue` seam). Pure; match-tick fatigue
// NEVER writes back. Not stored — recomputed on demand, so it is automatically save-exact.
public static float ProjectMatchEntryFatigue(in TrainingState s);

// KD-3 coaching routing seam — identity until #34 lands (no phantom #34 interface).
public readonly struct CoachingModifier { public static CoachingModifier Identity => default; }

// KD-5 injury-risk output — a read-only per-player scalar #41 consumes; #29 owns no injury model.
public readonly struct InjuryRiskContribution { public readonly int RiskScore; /* from intensity + fatigue + condition */ }

// KD-7 observer surface for #31/#38 (value copies).
public readonly struct TrainingViewModel { /* focus / condition / training-fatigue */ }
```

## 7. Key design decisions

- **KD-1 (fatigue reconciliation — the headline risk).** Training-fatigue is a #29-owned **integer
  world-tick accumulator**; match-tick fatigue is `1 − AerobicPool`. They **never share a counter**.
  Reconciliation is the pure one-directional `ProjectMatchEntryFatigue` → the caller-supplied `float
  fatigue` at match boot (the KD-P4 seam). Match-tick fatigue never writes back into training-fatigue, and
  #29 never touches `AerobicPool` or the match-side per-agent form/context (out of scope, §0). The
  projection is **not stored** (a pure function of the serialized accumulator), so a mid-week save→restore
  restores the accumulator exactly and the projected match-entry fatigue is byte-identical — no double-count
  is representable because there is only one accumulator and one read. (Conditioning affecting match entry
  beyond the fatigue offset is a deferred extension, §9.)
- **KD-2 (growth seam — single-owner attribute mutation).** #29 writes attribute growth **only** by
  populating #28's `TrainingInput` fields; `GrowthProjection` stays the sole attribute writer (a #28
  contract #29 consumes). `ComputeTrainingInput` is **pure and deterministic** — usable at #30's slot-1
  progression seam to feed #28 the same world day, with **no staleness and no reorder** of #30's documented
  tick order (the mutating `AdvanceTrainingDay` runs at slot-2). At Stage 2 the contribution is `Neutral`
  → #28's curve is byte-identical to the no-training path.
- **KD-3 (coaching modulation — identity routing seam).** `AdvanceTrainingDay`/`ComputeTrainingInput` take
  `in CoachingModifier`, defaulting to `Identity` (×1.0). **No #34 interface is built** (FR-LW-031); #34
  becomes the producer of a non-identity modifier when it lands — the same routing-seam-as-identity pattern
  as #28's `TrainingInput.Neutral`.
- **KD-4 (cadence — daily accrual, weekly focus).** The world-tick step runs **daily** and applies a daily
  delta; the **focus is a persistent field** changed by a weekly command. There is **no separate weekly
  batch boundary** to serialize and **no rollover step** — mirroring #28's "no discrete rollover"
  resolution, so nothing can be double-counted. "Weekly" is the human's focus-selection cadence, not a
  batch tick. `LastAdvancedWorldDay` guards against advancing a day twice (a save→restore→re-run is a
  no-op for an already-advanced day).
- **KD-5 (injury-risk output — shaped for #41, not owned by #29).** #29 exposes a read-only per-player
  `InjuryRiskContribution` scalar (from intensity + training-fatigue + conditioning) on its observer
  surface; **#41 reads it** and owns occurrence/severity/recovery. **No #41 interface is built** (phantom
  avoidance) — the value sits on #29's state, the #28 `LifecycleViewModel`-for-#31/#38 precedent.
- **KD-6 (determinism — no stream).** #29 is fully deterministic; it registers no RNG stream and does not
  promote `0x21`/83, which stay reserved (§5). The reservation stands for any future stochastic training
  extension.
- **KD-7 (persistence).** Opaque `TRAINING_SAVE_FORMAT_VERSION` sub-blob under #30's season save;
  fail-loud gates; serialize-don't-regenerate.
- **KD-8 (behaviour-neutral identity).** Attribute-growth dial off + `CoachingModifier.Identity` +
  `TrainingInput.Neutral` ⇒ #29 evolves only its own conditioning / training-fatigue and never touches
  #28's attributes/CA/PA. The Stage-2 minimal surface is the identity the deep tier extends.

## 8. Test focus

- Save→restore round-trip across a **mid-week** boundary — the training-fatigue accumulator + focus +
  conditioning + `LastAdvancedWorldDay` restore field-identical; two-run determinism from one seed.
- **Behaviour-neutral identity proof** — Stage-2 (attribute-growth dial off) changes only conditioning /
  training-fatigue, and a `ComputeTrainingInput` under the dial-off/Neutral configuration equals
  `TrainingInput.Neutral`, so #28's `GrowthProjection` is byte-identical to the no-training path.
- **Fatigue-reconciliation lock** — `ProjectMatchEntryFatigue` is a pure function of the serialized
  accumulator (recompute after restore == before); match-tick fatigue never mutates training-fatigue (no
  write-back path exists — the two counters are distinct fields in distinct assemblies).
- Idempotency — advancing the same world day twice is a no-op (`LastAdvancedWorldDay`).
- Fail-loud — bad `TRAINING_SAVE_FORMAT_VERSION`, out-of-bounds length prefix, trailing bytes; an
  out-of-contract `TrainingInput`/focus fails loud at the consuming seam (the #27 `SquadFileLoader` /
  #28 F4 precedent).

## 9. Risks

- **Fatigue double-count (headline).** Mitigated structurally by KD-1: a single world-tick accumulator, a
  single pure projection, no write-back — there is no counter to share.
- **#28/#29 growth-seam duplication.** Mitigated by KD-2: #29 never writes attributes; it hands #28 a
  value. Cross-checked at the section-file stage against #28 FR-PG-008 ("`GrowthProjection` is the sole
  attribute-mutation path").
- **Tick-order coupling with #30.** Resolved without a #30 change: the pure `ComputeTrainingInput` read is
  valid at slot-1 (feeds #28) while the mutating step stays at slot-2 — #30's documented order is honored,
  no staleness.
- **#34 coaching lands later.** Mitigated by KD-3's identity routing seam — #29 ships behaviour-neutral
  before staff exist.
- **Deferred extensions (recorded, not built):** conditioning affecting match entry beyond the fatigue
  offset (would feed a match-side conditioning input); a match-participation-driven sharpness/morale "form"
  (its own future owner). Both are explicitly out of #29's Stage-2/3 scope so #29 stays single-owner.

## 10. Promotion pipeline

1. Author the 11-file section set at `IN REVIEW` (FR-TR-001..NNN).
2. Section-file PASS-1 adversarial review → AR-2/AR-3 to convergence.
3. R-01..R-05 lead-developer sign-off → APPROVED; flip `SPEC_INDEX.md` row.
4. #16 §3.4 note (ERR-029-001): record that #29 was authored and confirmed **deterministic** — no stream,
   `_RESERVED_0x21_` / 83 **remain reserved** (no promotion, no code const, no digest bump).
5. T-phase implementation (post-APPROVED): T0 value types + deterministic Stage-2 step → T1
   `TRAINING_SAVE_FORMAT_VERSION` sub-blob + season-save composition → T2 `AdvanceTrainingDay`/
   `ComputeTrainingInput` wired at #30's reserved seams → T3 deep per-attribute growth (the `TrainingInput`
   fields, deterministic) + coaching consumption when #34 lands.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.5 | August 6, 2026 | **T2 LANDED** (the wiring; post-promotion record). `PlayerCareerStates` in `season-save` is the #30-side owner — the piece T1 had no name for — holding both per-club sets keyed `(ClubId, PlayerId)`; `SeasonLoop` drives `AdvanceTrainingDay` at slot 2 on the pre-increment world day, projects `ProjectMatchEntryFatigue` into a new four-argument `MatchEngine.ConfigureSquads` (`AerobicPool = 1 − fatigue`), and reconciles roster membership at a new (d′) position in `RollToNextSeason`. Behaviour-neutral on the defaults **by construction, not by tuning**: `Balanced`'s daily load equals `FatigueDailyRecovery` exactly, so the accumulator never leaves 0 — a fact worth keeping, because it means the whole seam is invisible until a focus is set and any future change to either constant breaks that silently. **ERR-029-006 filed:** §3.5/§4.3's batch `#28.AdvanceDay(worldDay, in trainingInputs)` does not exist (only the per-player `AdvanceDayForPlayer`), and FR-TR-025's `RegenResult`/`RetirementResult` do not either. The handoff is realized as roster *reconciliation* against the roster #30 already holds — same contract, same key, over state that exists; slot 1 waits for D1 rather than gathering a batch for a consumer that cannot take one. **Negative result worth recording:** a `ToSchedule()` convenience was deliberately still not added at T2 (`ClubTrainingStates` v1.0's note anticipated it) — `ScheduleFor(clubId)` on the owner is where the club-scoped bind belongs, and a second construction path would reintroduce exactly the id/state pairing hazard `TrainingSchedule` exists to prevent. |
| v0.1 | July 23, 2026 | Initial design supplement from spec-plan v0.1. |
| v0.2 | July 23, 2026 | AR-1 (1H+1M+2L): **H — phantom RNG stream** (KD-6 invented a `training.session` stream while citing FR-LW-031; #29 has no honest #29-owned draw site — resolved to fully deterministic, no stream, `0x21`/83 stay reserved); **M-1 — Form/Fitness two-cursor muddle** (collapsed to one `Condition` cursor; match-driven form deferred to its owner); L-1 match-side form/context out-of-scope note; L-2 `InjuryRiskContribution`/deferred-conditioning shapes. |
| v0.3 | July 23, 2026 | AR-2 (1M cross-fix: §2/§6/§10 residual `jitter`/`training.session`/promotion mentions swept to the deterministic model) → AR-3 clean. CONVERGENCE. |
| v0.4 | July 23, 2026 | PROMOTED — 11-file section set authored + APPROVED. |
