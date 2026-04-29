# Decision Tree Specification #8 — Section 3.3: Final Action Selection — Composure Model

**File:** `Decision_Tree_Spec_Section_3_3_v1_0.md`
**Purpose:** Defines the complete `SelectAction()` algorithm — Step 5 of the 6-step
Decision Tree pipeline. This section specifies: (1) the deterministic noise hash function
that introduces bounded variability into action selection; (2) the Composure-to-noise
scaling model that differentiates player quality tiers; (3) the tiebreak rule for equal
`EffectiveUtility` scores; (4) the no-viable-option fallback to HOLD; and (5) the
mathematically derived FR-09 acceptance thresholds that resolve the provisional values
blocking Section 2 approval. This section is the authoritative reference for all
composure-noise constants; all [GT] constants must be implemented exclusively in
`ComposureWeights.cs` (companion to `UtilityWeights.cs` from §3.2).

**Created:** March 02, 2026, 4:00 PM PST
**Version:** 1.0
**Status:** ✅ APPROVED — Lead developer signed off April 27, 2026 (draft-level quality gate; see §9 approval checklist)
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisite Sections:**
- Section 1 v1.1 (approved) — KD-3 establishes composure noise model; KD-7 determinism
- Section 2 v1.1 (approved) — FR-04, FR-08, FR-09; `ActionOption` struct (§2.2.3);
  `AgentAction` struct (§2.2.2); `DecisionMadeEvent` struct (§2.2.7)
- Section 3.1 v1.1 (approved) — option generation; candidate list structure
- Section 3.2 v1.2 (draft) — utility scoring; all `BaseUtility` values populated

**Upstream Cross-References (read-only):**
- `ActionOption.BaseUtility`: populated by ScoreOptions() (§3.2); range [0.01, 1.0]
- `ActionOption.EffectiveUtility`: written by this section; range [BaseUtility − NOISE_MAX, BaseUtility + NOISE_MAX]
- `ActionType` enum ordinals: §2.2.1 (PASS=0, SHOOT=1, DRIBBLE=2, HOLD=3, MOVE=4, PRESS=5, INTERCEPT=6)
- `DecisionMadeEvent.TiebreakerApplied`: §2.2.7; set by this section
- `DecisionMadeEvent.FallbackToHold`: §2.2.7; set by this section
- `PlayerAttributes.Composure`: raw [1–20] integer; normalised per §3.2.1.2
- `DecisionContext.MatchSeed`: `ulong`; set once per match by orchestrator
- `DecisionContext.AgentState.AgentId`: `int` [0–21]
- Deterministic hash pattern: consistent with Collision System #3 §2.6.4 (xorshift128+ / SplitMix64)
- Perception System #7 §3.3.4: additive-only deterministic noise pattern (precedent)

**Downstream Obligations:**
- This section MUST deliver derived FR-09 thresholds to replace §2.3.3 provisional values
- This section MUST define NOISE_MAX to validate §3.2.5.2 HOLD/PASS separation analysis

---

## Table of Contents

- [3.3.0 Selection in the Pipeline](#330-selection-in-the-pipeline)
- [3.3.1 Naming Clarification: BaseUtility vs ScoredUtility](#331-naming-clarification-baseutility-vs-scoredutility)
- [3.3.2 SelectAction() Algorithm](#332-selectaction-algorithm)
  - [3.3.2.1 Algorithm Pseudocode](#3321-algorithm-pseudocode)
  - [3.3.2.2 Algorithm Invariants](#3322-algorithm-invariants)
- [3.3.3 Deterministic Noise Hash Function](#333-deterministic-noise-hash-function)
  - [3.3.3.1 Hash Input Composition](#3331-hash-input-composition)
  - [3.3.3.2 Hash-to-Float Conversion](#3332-hash-to-float-conversion)
  - [3.3.3.3 Design Decision: Per-Option vs Per-Agent Noise](#3333-design-decision-per-option-vs-per-agent-noise)
- [3.3.4 Composure Noise Scaling Model](#334-composure-noise-scaling-model)
  - [3.3.4.1 Noise Bound Formula](#3341-noise-bound-formula)
  - [3.3.4.2 Effective Utility Formula](#3342-effective-utility-formula)
  - [3.3.4.3 NOISE_MAX Derivation and Validation](#3343-noise_max-derivation-and-validation)
  - [3.3.4.4 Composure Tier Behaviour Table](#3344-composure-tier-behaviour-table)
- [3.3.5 Tiebreak Rule](#335-tiebreak-rule)
- [3.3.6 No-Viable-Option Fallback](#336-no-viable-option-fallback)
- [3.3.7 FR-09 Threshold Derivation](#337-fr-09-threshold-derivation)
  - [3.3.7.1 Analytical Framework](#3371-analytical-framework)
  - [3.3.7.2 Composure = 20 Analysis](#3372-composure--20-analysis)
  - [3.3.7.3 Composure = 1 Analysis](#3373-composure--1-analysis)
  - [3.3.7.4 Derived Thresholds](#3374-derived-thresholds)
  - [3.3.7.5 Section 2 Amendment Required](#3375-section-2-amendment-required)
- [3.3.8 Numerical Verification — Attribute Extremes](#338-numerical-verification--attribute-extremes)
  - [3.3.8.1 Case A: Elite Composure (Composure = 20)](#3381-case-a-elite-composure-composure--20)
  - [3.3.8.2 Case B: Minimum Composure (Composure = 1)](#3382-case-b-minimum-composure-composure--1)
  - [3.3.8.3 Case C: Mid-Tier Composure (Composure = 10)](#3383-case-c-mid-tier-composure-composure--10)
- [3.3.9 Worked Example — Full Selection Pass (Continuation of §3.2.12)](#339-worked-example--full-selection-pass-continuation-of-3212)
- [3.3.10 ComposureWeights.cs Constant Catalogue](#3310-composureweightscs-constant-catalogue)
- [3.3.11 Design Decision Log](#3311-design-decision-log)
- [3.3.12 Version History](#3312-version-history)

---

## 3.3.0 Selection in the Pipeline

Action selection is **Step 5** of the 6-step Decision Tree pipeline. It receives the
scored `ActionOption[]` list from `ScoreOptions()` (Step 4 / §3.2) and produces the
final `AgentAction` struct dispatched by `DispatchAction()` (Step 6 / §4).

```
Pipeline context:

  Step 4: ScoreOptions()       → ActionOption[] (BaseUtility populated ∈ [0.01, 1.0])
  Step 5: SelectAction()       → AgentAction (single winner; EffectiveUtility stored)
  Step 6: DispatchAction()     → routes AgentAction to execution system
```

`SelectAction()` performs three operations in sequence:
1. **Noise injection:** For each `ActionOption`, compute a deterministic noise value
   scaled by Composure, add it to `BaseUtility`, and store the result in
   `EffectiveUtility`.
2. **Winner selection:** Select the option with the highest `EffectiveUtility`.
3. **AgentAction construction:** Populate and return the `AgentAction` struct from the
   winning option's fields.

The selected action is logged via `DecisionMadeEvent` in Step 6 (§4). `SelectAction()`
itself has no side effects other than writing `EffectiveUtility` on each `ActionOption`
and returning the constructed `AgentAction`.

---

## 3.3.1 Naming Clarification: BaseUtility vs ScoredUtility

⚠ **Cross-section naming inconsistency identified and resolved here.**

Section 3.2 uses the term `ScoredUtility` in its narrative and numerical verification
to describe the clamped [0.01, 1.0] output of each utility formula. However, the
struct definition in §2.2.3 names this field `ActionOption.BaseUtility`.

Section 3.2 §3.2.0 (pipeline context box) lists `ScoredUtility` and `BaseUtility` as
if they were separate fields, but the `ActionOption` struct has only two utility
fields: `BaseUtility` and `EffectiveUtility`.

**Resolution — authoritative mapping:**

| Narrative Term (§3.2) | Struct Field (§2.2.3) | Set By | Range |
|---|---|---|---|
| `ScoredUtility` | `ActionOption.BaseUtility` | `ScoreOptions()` (Step 4) | [0.01, 1.0] |
| `EffectiveUtility` | `ActionOption.EffectiveUtility` | `SelectAction()` (Step 5) | unbounded* |

\* EffectiveUtility is not clamped — it may exceed 1.0 or drop below 0.01 after noise
injection. The clamp is on `BaseUtility` only (§3.2.1.1). EffectiveUtility is used
solely for ranking; its absolute value has no downstream meaning.

**Section 3.2 terminology was a documentation convenience.** The struct field name
`BaseUtility` is authoritative for implementation. This section uses `BaseUtility`
exclusively to refer to the struct field, consistent with §2.2.3 and §3.1.

**Recommended §3.2 amendment (non-blocking):** Replace `ScoredUtility` with
`BaseUtility` in §3.2.0 pipeline context box and §3.2 narrative to eliminate
the naming ambiguity. This is a cosmetic amendment with no formula impact.

---

## 3.3.2 SelectAction() Algorithm

### 3.3.2.1 Algorithm Pseudocode

```csharp
/// <summary>
/// Step 5 of the Decision Tree pipeline.
/// Applies composure-modulated deterministic noise to each candidate's BaseUtility,
/// selects the highest-scoring option, and constructs the dispatched AgentAction.
///
/// Preconditions:
///   - options.Length >= 1 (guaranteed by GenerateOptions(); if 0, fallback §3.3.6 applies)
///   - All options have BaseUtility in [0.01, 1.0] (guaranteed by ScoreOptions() clamp)
///   - DecisionContext is fully populated (Steps 1–2 complete)
///
/// Postconditions:
///   - Exactly one AgentAction is returned
///   - All options have EffectiveUtility populated
///   - DecisionMadeEvent fields are fully determined (published in Step 6)
///
/// Performance: O(N) where N = candidate count (baseline ≤ 16, hard cap 17 per §3.1
///              INV-GEN-09 and Appendices §A.1; typically 3–5 in steady-state play)
/// Memory: Zero heap allocations — all operations on stack-allocated structs
/// </summary>
public AgentAction SelectAction(
    ActionOption[] options,
    DecisionContext context)
{
    // ──────────────────────────────────────────────────────────────
    // PHASE 0: No-viable-option fallback (§3.3.6)
    // ──────────────────────────────────────────────────────────────
    if (options.Length == 0)
    {
        return ConstructFallbackHold(context);
    }

    // ──────────────────────────────────────────────────────────────
    // PHASE 1: Composure noise injection (§3.3.3, §3.3.4)
    // ──────────────────────────────────────────────────────────────

    // Normalise Composure from raw [1, 20] to [0.0, 1.0]
    // Standard normalisation per §3.2.1.2: A_x = (x_raw − 1) / 19
    float A_Composure = (context.AgentState.Composure - 1) / 19.0f;

    // Compute per-agent noise bound: scales inversely with Composure
    // NoiseBound ∈ [NOISE_FLOOR, NOISE_MAX]
    float noiseBound = ComposureWeights.NOISE_MAX
                     * (1.0f - A_Composure * ComposureWeights.COMPOSURE_SUPPRESSION);

    for (int i = 0; i < options.Length; i++)
    {
        // Compute deterministic noise for this specific option (§3.3.3)
        float rawNoise = ComputeOptionNoise(
            context.MatchSeed,
            context.AgentState.AgentId,
            context.HeartbeatTick,
            (int)options[i].Type        // ActionType enum ordinal
        );
        // rawNoise ∈ [−1.0, +1.0)

        // Scale by composure-dependent bound
        float noise = rawNoise * noiseBound;
        // noise ∈ [−noiseBound, +noiseBound)

        // Apply noise to scored utility
        options[i].EffectiveUtility = options[i].BaseUtility + noise;
    }

    // ──────────────────────────────────────────────────────────────
    // PHASE 2: Winner selection with tiebreak (§3.3.5)
    // ──────────────────────────────────────────────────────────────

    int winnerIndex = 0;
    bool tiebreakerApplied = false;

    for (int i = 1; i < options.Length; i++)
    {
        float diff = options[i].EffectiveUtility - options[winnerIndex].EffectiveUtility;

        if (diff > TIEBREAK_EPSILON)
        {
            // Strictly higher — new winner
            winnerIndex = i;
            tiebreakerApplied = false;
        }
        else if (diff >= -TIEBREAK_EPSILON)
        {
            // Within epsilon — tiebreak by ActionType ordinal (lower wins)
            if ((int)options[i].Type < (int)options[winnerIndex].Type)
            {
                winnerIndex = i;
            }
            tiebreakerApplied = true;
        }
        // else: strictly lower — skip
    }

    // ──────────────────────────────────────────────────────────────
    // PHASE 3: AgentAction construction
    // ──────────────────────────────────────────────────────────────

    ActionOption winner = options[winnerIndex];

    AgentAction action = new AgentAction(
        agentId:        context.AgentState.AgentId,
        type:           winner.Type,
        targetAgentId:  winner.TargetAgentId,
        targetPosition: winner.TargetPosition,
        passParams:     winner.PassParams,
        shotParams:     winner.ShotParams,
        utilityScore:   winner.EffectiveUtility,
        heartbeatTick:  context.HeartbeatTick
    );

    // Store selection metadata for DecisionMadeEvent (published in Step 6 / §4)
    _lastSelectionMetadata = new SelectionMetadata(
        candidateCount:     options.Length,
        tiebreakerApplied:  tiebreakerApplied,
        fallbackToHold:     false
    );

    return action;
}
```

**`_lastSelectionMetadata`** is an internal field consumed by `DispatchAction()` (§4) when
populating `DecisionMadeEvent`. It is not exposed beyond the `DecisionTree` class. It is
overwritten on every `SelectAction()` call and is therefore never stale.

```csharp
/// <summary>
/// Internal-only metadata from the most recent SelectAction() call.
/// Consumed by DispatchAction() (§4) to populate DecisionMadeEvent fields.
/// Not exposed outside DecisionTree.cs.
///
/// This struct is NOT part of the public interface. It is defined here for
/// completeness; implementation may use any equivalent mechanism to pass
/// metadata from Step 5 to Step 6.
/// </summary>
private struct SelectionMetadata
{
    public readonly int  CandidateCount;      // maps to DecisionMadeEvent.CandidateCount
    public readonly bool TiebreakerApplied;   // maps to DecisionMadeEvent.TiebreakerApplied
    public readonly bool FallbackToHold;       // maps to DecisionMadeEvent.FallbackToHold

    public SelectionMetadata(int candidateCount, bool tiebreakerApplied, bool fallbackToHold)
    {
        CandidateCount    = candidateCount;
        TiebreakerApplied = tiebreakerApplied;
        FallbackToHold    = fallbackToHold;
    }
}
```

---

### 3.3.2.2 Algorithm Invariants

These invariants must hold for every `SelectAction()` execution. They are tested in
Section 5 (testing framework). Any violation is a defect.

| ID | Invariant | Verified By |
|----|-----------|-------------|
| INV-SEL-01 | Exactly one `AgentAction` is returned per call | UT-10 |
| INV-SEL-02 | All `ActionOption` elements have `EffectiveUtility` != 0.0 after Phase 1 | UT-11 |
| INV-SEL-03 | The returned `AgentAction.Type` matches `options[winnerIndex].Type` | UT-12 |
| INV-SEL-04 | `AgentAction.UtilityScore` equals the winner's `EffectiveUtility` | UT-12 |
| INV-SEL-05 | If `options.Length == 1`, that option is always selected (noise cannot eliminate the sole candidate) | UT-13 |
| INV-SEL-06 | Given identical `(matchSeed, agentId, heartbeatTick)` and identical `BaseUtility[]`, the same `AgentAction.Type` is always returned (determinism) | UT-14 / FR-04 |
| INV-SEL-07 | `TiebreakerApplied` is `true` if and only if the winner's `EffectiveUtility` was within `TIEBREAK_EPSILON` of at least one other candidate | UT-15 |

---

## 3.3.3 Deterministic Noise Hash Function

The noise source is a deterministic hash function — not `System.Random`, not
`UnityEngine.Random`, and not any stateful RNG. This guarantees that identical match
seeds produce identical decision sequences across all platforms and all runs (FR-04,
KD-3).

### 3.3.3.1 Hash Input Composition

```csharp
/// <summary>
/// Computes a deterministic noise value for a specific action option.
///
/// The hash combines four inputs to ensure that:
///   (1) Different matches produce different noise       (matchSeed)
///   (2) Different agents produce different noise        (agentId)
///   (3) Different ticks produce different noise         (heartbeatTick)
///   (4) Different options produce different noise       (actionTypeOrdinal)
///
/// Without (4), all options for one agent at one tick receive the same noise,
/// which adds a constant offset to all utilities and has ZERO effect on ranking.
/// See Design Decision §3.3.3.3.
///
/// Uses SplitMix64 as the mixing function — consistent with Collision System #3 §2.6.4.
/// SplitMix64 is a high-quality non-cryptographic integer hash with excellent avalanche
/// properties (Vigna, 2016). It is NOT an RNG — it is a stateless hash function.
/// Each call is independent; no state is carried between calls.
/// </summary>
private static float ComputeOptionNoise(
    ulong matchSeed,
    int   agentId,
    int   heartbeatTick,
    int   actionTypeOrdinal)
{
    // Compose a unique 64-bit input from all four parameters.
    // Bit layout chosen to avoid collisions:
    //   - matchSeed occupies full 64 bits (XOR base)
    //   - agentId [0–21] occupies bits 0–4 (5 bits; max 31)
    //   - heartbeatTick occupies bits 5–24 (20 bits; max 1,048,575 ticks = ~29 hours)
    //   - actionTypeOrdinal [0–6] occupies bits 25–27 (3 bits; max 7)
    //
    // This packing guarantees zero bit overlap for all Stage 0 value ranges.
    // Stage 1+ agents (>22) or extended enums will require repacking — flagged in §8.2.

    ulong packed = ((ulong)agentId)
                 | ((ulong)heartbeatTick << 5)
                 | ((ulong)actionTypeOrdinal << 25);

    ulong combined = matchSeed ^ packed;

    // SplitMix64 hash — identical to Collision System #3 §4.7.2
    ulong hash = SplitMix64(combined);

    // Convert to float in [−1.0, +1.0)
    // Upper 24 bits → [0, 1) float, then remap to [−1, +1)
    float unit = (hash >> 40) * (1.0f / (1UL << 24));  // [0.0, 1.0)
    return unit * 2.0f - 1.0f;                           // [−1.0, +1.0)
}

/// <summary>
/// SplitMix64 stateless hash function.
/// Identical implementation to Collision System #3 §4.7.2 and §2.6.4.
/// Must not diverge — any change here must be reflected in both specifications.
///
/// Reference: Vigna, S. "An experimental exploration of Marsaglia's xorshift generators,
/// scrambled." ACM Trans. Math. Softw. 42(4), 2016.
/// </summary>
private static ulong SplitMix64(ulong x)
{
    x += 0x9E3779B97F4A7C15UL;
    x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
    x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
    return x ^ (x >> 31);
}
```

**Collision with Collision System's DeterministicRNG:** The Collision System (#3) uses
`DeterministicRNG` — a stateful xorshift128+ PRNG seeded from SplitMix64. The Decision
Tree does NOT use that RNG. The DT uses SplitMix64 directly as a stateless hash. This is
correct: the DT needs one noise value per option per tick, not a sequence. A stateful RNG
would require careful initialisation and ordering guarantees that add complexity without
benefit. The SplitMix64 hash provides identical determinism with simpler semantics.

**Cross-spec consistency note:** Both systems use the same SplitMix64 implementation.
If the SplitMix64 constants are ever changed (they should not be — they are well-tested
constants from Vigna 2016), both specifications must be updated simultaneously. This
dependency is logged in the Spec Error Log.

---

### 3.3.3.2 Hash-to-Float Conversion

The hash output is a 64-bit unsigned integer. Conversion to a signed float in [−1.0, +1.0)
uses the upper 24 bits, identical to the Collision System's `NextFloat()` method:

```
hash_64bit → take upper 24 bits → divide by 2^24 → float in [0.0, 1.0) → remap to [−1.0, +1.0)
```

**Why upper 24 bits?** SplitMix64's output has better uniformity in the upper bits than
the lower bits (a property of the multiply-xor-shift structure). 24 bits provide
16,777,216 distinct float values — sufficient precision for noise values that are
subsequently scaled by NOISE_MAX (0.15).

**Why [−1.0, +1.0) not [0.0, 1.0)?** The noise must be bipolar — it can increase or
decrease a candidate's effective utility. A unipolar [0, 1) noise would only ever boost
candidates, which would shift the ranking toward whichever candidate received the largest
boost, systematically favouring certain action types at certain tick values. Bipolar noise
is symmetric: over many ticks, the mean noise per option is approximately zero, preventing
systematic bias.

**Exact zero noise:** The hash output `0x0000_0000_0000_0000` maps to exactly −1.0. The
output that maps to exactly 0.0 (no noise) is `0x0080_0000_0000_0000` (upper 24 bits =
2^23). This is one value out of 16 million — effectively never occurs. No special-casing
is needed.

---

### 3.3.3.3 Design Decision: Per-Option vs Per-Agent Noise

⚠ **Cross-document conflict identified and resolved here.**

**Conflict:** KD-3 (§1.4) states: `hash(matchSeed, agentId, heartbeatTick, actionType)`
— four inputs, producing **per-option** noise (each candidate gets a different noise value).
The Outline §3.3.1 states: `noise_seed = matchSeed ^ (agentId << 16) ^ heartbeatTick` —
three inputs, producing **per-agent** noise (all candidates for one agent at one tick get
the same noise value).

**Analysis:** Per-agent noise (3 inputs) adds a constant offset to all candidates. If all
candidates receive `+0.05` noise, their relative ranking is unchanged. The noise would
have **zero effect on action selection**. This defeats the purpose of the composure model
entirely — Composure would be a non-functional attribute, violating FR-09.

Per-option noise (4 inputs) adds a different offset to each candidate. This can change
relative ranking: a candidate that was ranked 2nd before noise may become ranked 1st after
noise. This is the correct behaviour — a low-Composure agent may select a suboptimal
action because their noise perturbation reorders the candidates.

**Decision:** Per-option noise (4 inputs). This is consistent with KD-3 (§1.4), which is
the locked design decision and takes precedence over the Outline sketch. The Outline's
3-input formula was a preliminary sketch that omitted the actionType input.

**Consequence:** The Outline §3.3.1 formula is superseded. The authoritative noise hash
uses four inputs as defined in §3.3.3.1 above.

---

## 3.3.4 Composure Noise Scaling Model

### 3.3.4.1 Noise Bound Formula

The noise magnitude is bounded per agent per tick. The bound scales inversely with
Composure: high Composure → small noise bound → consistent selection; low Composure →
large noise bound → erratic selection.

```
NoiseBound(Composure) = NOISE_MAX × (1.0 − A_Composure × COMPOSURE_SUPPRESSION)

where:
  A_Composure           = (Composure_raw − 1) / 19      [standard normalisation, §3.2.1.2]
  NOISE_MAX             = 0.15 [GT]                      [maximum possible noise magnitude]
  COMPOSURE_SUPPRESSION = 0.95 [GT]                      [how much Composure reduces noise]
```

**Key property:** At `Composure = 20` (A_Composure = 1.0):

```
NoiseBound = 0.15 × (1.0 − 1.0 × 0.95) = 0.15 × 0.05 = 0.0075
```

At `Composure = 1` (A_Composure = 0.0):

```
NoiseBound = 0.15 × (1.0 − 0.0 × 0.95) = 0.15 × 1.0 = 0.15
```

**Noise bound range by Composure tier:**

| Composure (raw) | A_Composure | NoiseBound | Max perturbation per option |
|-----------------|-------------|------------|---------------------------|
| 1 | 0.000 | 0.1500 | ±0.150 |
| 3 | 0.105 | 0.1350 | ±0.135 |
| 5 | 0.211 | 0.1200 | ±0.120 |
| 8 | 0.368 | 0.0975 | ±0.098 |
| 10 | 0.474 | 0.0825 | ±0.083 |
| 12 | 0.579 | 0.0675 | ±0.068 |
| 15 | 0.737 | 0.0450 | ±0.045 |
| 18 | 0.895 | 0.0225 | ±0.023 |
| 20 | 1.000 | 0.0075 | ±0.008 |

---

### 3.3.4.2 Effective Utility Formula

For each `ActionOption` in the candidate list:

```
EffectiveUtility(option) = BaseUtility(option) + rawNoise(option) × NoiseBound

where:
  rawNoise(option) = ComputeOptionNoise(matchSeed, agentId, heartbeatTick, actionTypeOrdinal)
  rawNoise ∈ [−1.0, +1.0)

  EffectiveUtility ∈ [BaseUtility − NoiseBound, BaseUtility + NoiseBound)
```

**EffectiveUtility is NOT clamped.** It may exceed 1.0 or drop below 0.01. This is correct:
EffectiveUtility is used solely for ranking within a single `SelectAction()` call. Its
absolute value is stored in `AgentAction.UtilityScore` for debug/replay but has no
downstream semantic meaning for execution systems.

**Relationship to §2 Step 5 formula:** Section 2 §2.1.2 documented the preliminary
formula as:

```
EffectiveUtility(option) = U(option) + Noise × (1 − A_Composure)
```

The final formula is:

```
EffectiveUtility(option) = U(option) + rawNoise(option) × NOISE_MAX × (1 − A_Composure × COMPOSURE_SUPPRESSION)
```

The difference is:
1. `rawNoise` is per-option (4-input hash), not a single value per agent.
2. `NOISE_MAX` is explicit (0.15 [GT], not the outline's "±0.20").
3. `COMPOSURE_SUPPRESSION` (0.95) replaces the implicit 1.0 factor — this ensures
   even elite players have a non-zero (but negligible) noise floor, preventing exact
   ties between identically-scored options across all ticks.

The §2 formula was a preliminary sketch; this section is authoritative. §2 does not
require amendment because it explicitly defers to "Full derivation in Section 3."

---

### 3.3.4.2.1 Composure Double-Exposure Analysis

⚠ **The Composure attribute affects agent behaviour in TWO distinct places:**

**Exposure 1 — Scoring phase (§3.2):** The `SHOOT_COMPOSURE_EXP` (0.30) and
`HOLD_COMPOSURE_EXP` (0.50) exponents in the scoring formulas reduce the `BaseUtility`
of SHOOT and HOLD actions for low-Composure agents. This affects *how good* the agent
estimates these actions to be.

**Exposure 2 — Selection phase (§3.3):** The `NoiseBound` scaling reduces noise for
high-Composure agents, making their *selection* more consistent. This affects *how
reliably* the agent picks the highest-scoring action.

**Are these redundant?** No — they serve distinct purposes:

- §3.2 composure exponents model **action quality estimation**: a low-Composure player
  evaluates "how good is holding here?" less accurately in terms of the base utility
  score. This affects the *ranking* of options before noise is applied.
- §3.3 noise scaling models **decision consistency**: even after ranking is established,
  a low-Composure player may not select the highest-ranked option because noise
  perturbation reorders candidates.

**Interaction:** The combined effect is multiplicative, not additive. Consider two
scenarios for Composure=1 vs Composure=20 when evaluating HOLD:

```
Composure=20 (elite):
  §3.2: HOLD BaseUtility = high (AttributeMultiplier ≈ 1.0)
  §3.3: NoiseBound = 0.0075 → selection nearly deterministic
  Net: HOLD is correctly valued AND consistently selected

Composure=1 (terrible):
  §3.2: HOLD BaseUtility = reduced (AttributeMultiplier ≈ 0.707, 29% penalty)
  §3.3: NoiseBound = 0.15 → selection erratic
  Net: HOLD is undervalued AND inconsistently selected
```

**Risk of over-penalisation:** At Composure=1, the combined effect could make HOLD
nearly impossible to select. In the §3.2.12 example, HOLD BaseUtility at Composure=1
is 0.079 (§3.2.5.2 worst case). At NoiseBound = 0.15, the EffectiveUtility range for
HOLD is [−0.071, 0.229]. Meanwhile, other options with BaseUtility > 0.229 would always
beat HOLD regardless of noise — effectively suppressing HOLD entirely for low-Composure
agents under extreme pressure. This is the **intended** behaviour: a panicking player
under pressure should not calmly hold the ball.

**Conclusion:** The double exposure is intentional and produces the correct gameplay
gradient. Both exposures are [GT] and can be independently tuned: the §3.2 exponents
control the baseline scoring impact while §3.3 NOISE_MAX controls the selection noise.
Reducing one without the other allows fine-grained control over Composure's influence
at different pipeline stages.

---

### 3.3.4.3 NOISE_MAX Derivation and Validation

**AR-4 (Outline):** "Composure noise magnitude: if noise range is too wide at Composure=1,
DT produces pathological decisions (shoots from own half, passes backwards into opponent).
Must be bounded and tested."

The Outline recommended ±0.20. This section reduces NOISE_MAX to **0.15 [GT]**.

**Rationale for 0.15 over 0.20:**

The §3.2.12 worked example produced scores ranging from 0.183 (DRIBBLE) to 0.487 (PASS-T2).
The gap between rank 1 and rank 2 is `0.487 − 0.353 = 0.134`. At NOISE_MAX = 0.20, a
Composure=1 agent could apply ±0.20 to each option — enough to bridge a 0.40 gap (worst
case: rank 1 gets −0.20, rank 2 gets +0.20). This is excessive: it would allow the worst
option (DRIBBLE at 0.183) to beat the best option (PASS at 0.487) — a pathological reversal
of 0.304 utility points.

At NOISE_MAX = 0.15, the maximum possible reversal between two options is:

```
Max reversal = 2 × NOISE_MAX = 2 × 0.15 = 0.30
```

This means two options separated by more than 0.30 utility can NEVER be reversed by
composure noise, regardless of Composure value. In the worked example, PASS-T2 (0.487) vs
DRIBBLE (0.183) has a gap of 0.304 — just above the 0.30 reversal ceiling. The best option
cannot be beaten by the worst option even for a Composure=1 agent. This is correct:
composure noise should introduce erratic choice between *close* alternatives, not cause
completely irrational behaviour.

**Validation against §3.2.5.2 HOLD/PASS separation:**

§3.2.5.2 established that the HOLD/PASS worst-case separation is 0.015 utility. At
NOISE_MAX = 0.15, a Composure=1 agent has NoiseBound = 0.15. The maximum reversal is
0.30. This easily bridges the 0.015 gap, meaning low-Composure agents will frequently
reverse HOLD and PASS ordering when the margin is thin. This is **intended behaviour**
(§3.2.12 interpretation note: "a nervous player may not execute the correct choice").

**Validation against pathological behaviour:**

| Scenario | Gap | Max reversal (0.15) | Pathological? |
|---|---|---|---|
| PASS-T2 vs DRIBBLE (§3.2.12) | 0.304 | 0.300 | Borderline — reversal requires both options at noise extremes simultaneously. Probability < 0.1% per tick. Acceptable. |
| PASS-T2 vs HOLD (§3.2.12) | 0.215 | 0.300 | Reversible. Low-Composure agent may hold instead of passing. Correct emergent behaviour. |
| PASS-T2 vs PASS-T1 (§3.2.12) | 0.134 | 0.300 | Easily reversible. Agent may pass to T1 instead of T2. Correct — both are reasonable passes. |
| SHOOT vs PASS (typical) | 0.05–0.15 | 0.300 | Reversible. Under pressure, agent may shoot instead of finding the better pass. Correct. |
| HOLD floor vs PASS worst | 0.015 | 0.300 | Frequently reversed at low Composure. Agent holds when they should play the ball. Correct under-pressure behaviour. |

**Safe tuning range for NOISE_MAX:** [0.08, 0.20]. Below 0.08, Composure has negligible
effect on selection (all agents behave like Composure=20). Above 0.20, pathological
reversals become common even for moderate-Composure agents.

---

### 3.3.4.4 Composure Tier Behaviour Table

Expected selection behaviour by Composure tier, given a typical scored candidate list
with 0.10–0.15 utility gaps between adjacent ranks:

| Tier | Composure | NoiseBound | Expected Behaviour |
|------|-----------|------------|-------------------|
| **Elite** | 18–20 | 0.008–0.023 | Selects optimal action >98% of ticks. Noise insufficient to bridge typical gaps. Player appears calm, decisive, and consistent. |
| **Good** | 13–17 | 0.023–0.053 | Selects optimal action ~90–97% of ticks. Occasional suboptimal choice when two options are very close. Slightly erratic under extreme pressure. |
| **Average** | 8–12 | 0.053–0.098 | Selects optimal action ~70–90% of ticks. Noticeable suboptimal choices when gaps are < 0.10. Visibly more erratic under pressure. |
| **Poor** | 4–7 | 0.098–0.135 | Selects optimal action ~50–70% of ticks. Frequently picks wrong option when alternatives are close. Prone to holding when should pass. |
| **Terrible** | 1–3 | 0.135–0.150 | Selects optimal action ~40–55% of ticks. Regularly makes suboptimal choices. Strongest differentiator from elite players in identical tactical positions. |

These percentages are estimates based on typical utility gap distributions observed in
§3.2 numerical verification. The exact values depend on the specific scored candidate
list and are verified formally in §3.3.7 (FR-09 threshold derivation).

---

## 3.3.5 Tiebreak Rule

If two or more candidates have `EffectiveUtility` values within `TIEBREAK_EPSILON` of each
other after noise injection, the winner is determined by `ActionType` enum ordinal value.

```
TIEBREAK_EPSILON = 1e-6f  [DERIVED — not gameplay-tuned]
```

**Rule:** Among all candidates within `TIEBREAK_EPSILON` of the current maximum
`EffectiveUtility`, select the one with the **lowest** `ActionType` enum ordinal.

**Ordinal priority (from §2.2.1):**

```
PASS = 0 (highest tiebreak priority)
SHOOT = 1
DRIBBLE = 2
HOLD = 3
MOVE_TO_POSITION = 4
PRESS = 5
INTERCEPT = 6 (lowest tiebreak priority)
```

**Rationale for lowest-ordinal-wins:** The ordering places active ball-progression
actions (PASS, SHOOT) ahead of passive actions (HOLD) and defensive repositioning
(MOVE, PRESS, INTERCEPT). In a tie, the agent prefers decisive action over inaction.
This is a deterministic tiebreak, not a gameplay decision — it occurs only when two
options are effectively identical in utility (within 1 millionth of a utility point).

**Why epsilon-based, not exact equality?** Floating-point comparison via `==` is
unreliable across platforms. Two computations that *should* produce the same value may
differ by a few ULPs (units in the last place) due to FMA (fused multiply-add)
availability, compiler optimisation level, or platform-specific floating-point rounding.
An epsilon comparison ensures consistent tiebreak behaviour across all target platforms.

**Why 1e-6?** The smallest meaningful `BaseUtility` difference is the clamp floor
(0.01). NOISE_MAX is 0.15. The product `0.01 × epsilon_relative` should be large enough
to absorb FP rounding but small enough to avoid false tiebreaks. At 1e-6, two options
must be within 0.000001 utility of each other to trigger a tiebreak — this is well below
any meaningful scoring difference (minimum meaningful gap is ~0.004 per §3.2.12 HOLD/T3
analysis).

**Tiebreak frequency:** In practice, exact ties are extremely rare because the per-option
noise hash produces different values for different `ActionType` ordinals. Two options
would need to have `BaseUtility` values that differ by exactly the difference between
their noise values — a measure-zero event in continuous arithmetic. Ties are expected in
< 0.01% of all `SelectAction()` calls.

**Logging:** `DecisionMadeEvent.TiebreakerApplied = true` when any tiebreak is
activated. This is diagnostic only; no downstream system reads this flag at Stage 0.

---

## 3.3.6 No-Viable-Option Fallback

If `GenerateOptions()` (Step 3, §3.1) returns an empty candidate list, `SelectAction()`
must produce a valid `AgentAction` without exception. This satisfies FR-08.

```csharp
/// <summary>
/// Fallback when GenerateOptions() produces zero candidates.
/// This should not occur in normal play — GenerateOptions() always produces at least
/// HOLD (when agent has possession) or MOVE_TO_POSITION (when agent does not).
/// If it does occur, the agent holds position with utility 0.0.
///
/// This is a failure mode, not a normal code path. It is logged for investigation.
/// </summary>
private AgentAction ConstructFallbackHold(DecisionContext context)
{
    _lastSelectionMetadata = new SelectionMetadata(
        candidateCount:     0,
        tiebreakerApplied:  false,
        fallbackToHold:     true
    );

    return new AgentAction(
        agentId:        context.AgentState.AgentId,
        type:           ActionType.HOLD,
        targetAgentId:  -1,
        targetPosition: Vector2.zero,
        passParams:     default,
        shotParams:     default,
        utilityScore:   0.0f,       // Sentinel value — not a real score
        heartbeatTick:  context.HeartbeatTick
    );
}
```

**Failure mode FM-DT-08:** This fallback is classified as a **failure mode**, not normal
operation. `DecisionMadeEvent.FallbackToHold = true` flags the event for investigation.
If this fires more than once per match in testing, there is a defect in `GenerateOptions()`.

**HOLD when agent has no possession:** The fallback produces `ActionType.HOLD`
regardless of possession state. If the agent does not have the ball, HOLD semantics
(from Agent Movement #2) are "maintain current position" — equivalent to doing nothing.
This is the safest fallback for any state.

---

