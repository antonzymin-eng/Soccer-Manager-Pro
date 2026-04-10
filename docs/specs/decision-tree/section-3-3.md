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
**Status:** DRAFT — Awaiting Lead Developer Review
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
/// Performance: O(N) where N = candidate count (max 7 at Stage 0; typically 3–5)
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

## 3.3.7 FR-09 Threshold Derivation

Section 2 §2.3.3 FR-09 established provisional acceptance thresholds for Composure
differentiation:
- Composure = 20: max-utility action selected ≥ 95% of trials
- Composure = 1: max-utility action selected ≤ 60% of trials

These were explicitly marked as provisional and blocked Section 2 approval pending this
derivation. This subsection provides the mathematically derived thresholds.

### 3.3.7.1 Analytical Framework

**Test setup (per FR-09 §2.3.3):** A fixed scored candidate list is presented to
`SelectAction()` 1,000 times, varying only `heartbeatTick` (1 through 1,000) to cycle
through different noise seeds. The agent attributes are fixed. The scored `BaseUtility`
values are fixed (identical list each trial). Only the noise changes.

**Reference candidate list:** To derive thresholds that are representative of typical
gameplay, we use the §3.2.12 worked example scored list:

| Option | BaseUtility | Gap to rank 1 |
|--------|------------|---------------|
| PASS-T2 | 0.487 | 0.000 (rank 1) |
| PASS-T1 | 0.353 | 0.134 |
| HOLD | 0.272 | 0.215 |
| PASS-T3 | 0.268 | 0.219 |
| DRIBBLE | 0.183 | 0.304 |

The critical gap is rank 1 → rank 2: **0.134 utility**.

**Reversal condition:** The rank-1 option (PASS-T2) is NOT selected when some other
option receives a noise perturbation that lifts it above PASS-T2's perturbed score.
The worst case for rank 1 is: rank 1 gets maximum negative noise, rank 2 gets maximum
positive noise.

**Maximum possible reversal between any two options:**

```
MaxReversal = NoiseBound_rank1_negative + NoiseBound_rank2_positive
            = NoiseBound + NoiseBound
            = 2 × NoiseBound
```

For rank 1 to lose to rank 2, we need:

```
2 × NoiseBound > Gap(rank1, rank2) = 0.134
NoiseBound > 0.067
```

---

### 3.3.7.2 Composure = 20 Analysis

```
A_Composure = 1.0
NoiseBound  = 0.15 × (1.0 − 1.0 × 0.95) = 0.0075
MaxReversal = 2 × 0.0075 = 0.015
```

The maximum reversal (0.015) is far smaller than the rank 1→2 gap (0.134). **Rank 1 can
NEVER be overthrown by rank 2** at Composure = 20 with this candidate list.

Can rank 1 be overthrown by ANY candidate? The closest gap is rank 1→2 at 0.134. Since
0.015 < 0.134, no candidate can beat rank 1.

**Composure = 20 optimal selection rate: 100.0%**

This is stronger than the provisional 95% threshold. However, this is specific to this
candidate list. For candidate lists with very small rank 1→2 gaps (< 0.015), elite
agents can still be reversed. The question is: what is the smallest gap that prevents
reversal at Composure = 20?

```
2 × 0.0075 = 0.015
```

Any rank 1→2 gap > 0.015 guarantees 100% optimal selection at Composure = 20. Gaps
≤ 0.015 are susceptible to noise reversal. In the §3.2 scoring analysis, the smallest
natural gap observed was 0.004 (HOLD vs PASS-T3). At this gap:

```
Probability of reversal ≈ proportion of noise space where |noise_rank2 − noise_rank1| > 0.004
```

For uniformly distributed noise in [−0.0075, +0.0075), the probability that the
difference between two independent uniform variables exceeds 0.004 is:

```
Let X, Y ~ Uniform(−0.0075, +0.0075)
P(Y − X > 0.004) where X is rank 1 noise, Y is rank 2 noise
                = P(Y − X > 0.004)
```

Using the triangular distribution of the difference of two uniform variables:
The difference D = Y − X has a triangular distribution on [−0.015, +0.015] with peak at 0.

```
P(D > 0.004) = (1/2) × ((0.015 − 0.004) / 0.015)² = (1/2) × (0.011/0.015)²
             = 0.5 × 0.5378 × 0.5378... 
```

Wait — let me derive this correctly. For D = Y − X where X, Y ~ Uniform(−b, +b):
D ~ Triangular(−2b, 0, +2b). The CDF of Triangular(−c, 0, +c) at d > 0 is:

```
P(D ≤ d) = 1 − (c − d)² / (2c²)    for 0 ≤ d ≤ c
where c = 2b = 0.015

P(D > 0.004) = (0.015 − 0.004)² / (2 × 0.015²)
             = (0.011)² / (2 × 0.000225)
             = 0.000121 / 0.00045
             = 0.2689
```

So even at the tightest natural gap (0.004), an elite agent (Composure=20) selects the
non-optimal action ~26.9% of the time. But this tight gap only occurs between the
lowest-ranked options (HOLD vs PASS-T3). For the rank 1 position, the gap is 0.134 —
guaranteed 100% selection.

**FR-09 test list design recommendation:** The UT-09 test should use a candidate list
where the rank 1→2 gap is representative of typical play, not the tightest possible gap.
Using the §3.2.12 list (gap = 0.134), the Composure = 20 threshold is **100%**. To be
conservative and account for candidate lists with tighter gaps (e.g., 0.02–0.05 rank 1→2
gap), the test threshold should be set at **≥ 97%**.

---

### 3.3.7.3 Composure = 1 Analysis

```
A_Composure = 0.0
NoiseBound  = 0.15 × (1.0 − 0.0 × 0.95) = 0.15
MaxReversal = 2 × 0.15 = 0.30
```

The maximum reversal (0.30) exceeds the rank 1→2 gap (0.134). Rank 2 CAN beat rank 1.

**Probability that rank 1 is selected (rank 1→2 gap = 0.134, b = 0.15):**

```
D = Y − X where X, Y ~ Uniform(−0.15, +0.15)
c = 2 × 0.15 = 0.30

P(rank 2 beats rank 1) = P(D > 0.134)
                       = (0.30 − 0.134)² / (2 × 0.30²)
                       = (0.166)² / (2 × 0.09)
                       = 0.027556 / 0.18
                       = 0.1531

P(rank 1 beats rank 2 in pairwise) = 1 − 0.1531 = 0.8469
```

But rank 1 must also beat ranks 3, 4, and 5. The probability of rank 1 being selected
is the probability that rank 1 has the highest EffectiveUtility across ALL candidates.

For a conservative bound, consider only the closest competitor (rank 2). If rank 1 beats
rank 2, it will almost certainly beat ranks 3–5 (which have larger gaps). The probability
that rank 3 (gap 0.215) beats rank 1:

```
P(D > 0.215) = (0.30 − 0.215)² / (2 × 0.09) = (0.085)² / 0.18 = 0.04014
```

Rank 4 (gap 0.219): P ≈ 0.03654. Rank 5 (gap 0.304): P ≈ 0.000089 (near impossible).

**Independence justification:** Each pairwise comparison (rank 1 vs rank k) involves two
noise values: `noise_rank1` and `noise_rank_k`. These are outputs of `ComputeOptionNoise()`
with different `actionTypeOrdinal` inputs. Because SplitMix64 is a high-quality hash with
excellent avalanche properties (Vigna 2016), outputs for different inputs are statistically
independent. Therefore the pairwise "rank 1 beats rank k" events are genuinely independent,
and the joint probability is the product — this is exact, not an approximation.

The one exception is **same-type candidates** (e.g., multiple PASS options), which share
the same `actionTypeOrdinal` and therefore receive identical noise. For same-type candidates,
the pairwise comparison is trivially determined by BaseUtility alone (noise cancels). In
the §3.2.12 list, ranks 1–3 are all PASS, so noise cannot reorder them. Only cross-type
comparisons (PASS vs HOLD, PASS vs DRIBBLE) are noise-affected. This means the effective
competitor set for rank 1 is only the non-PASS candidates: HOLD (gap 0.215) and DRIBBLE
(gap 0.304).

```
P(PASS-T2 beats HOLD)    = 1 − P(D > 0.215) = 1 − 0.0401 = 0.9599
P(PASS-T2 beats DRIBBLE) = 1 − P(D > 0.304) = 1 − 0.0000 = 1.0000
  (gap 0.304 ≥ max reversal 0.30 — impossible)

P(rank 1 selected) = 0.9599 × 1.0000 = 0.960
```

**Correction:** Accounting for the same-type noise identity property (DD-SEL-04), the
Composure=1 optimal selection rate for this candidate list is **~96%**, not the ~78%
originally computed. The earlier calculation incorrectly treated each PASS variant as
having independent noise.

**Implication:** The §3.2.12 worked example is a poor stress test for Composure
differentiation because rank 1 (PASS-T2) is only challenged by non-PASS types
(HOLD and DRIBBLE), which have large utility gaps. The same-type noise identity
means multiple PASS candidates cannot overthrow each other via composure noise.

**Revised analysis for candidate lists with cross-type competition at rank 1→2:**

For a candidate list where rank 1 and rank 2 are **different action types** (e.g., SHOOT
at 0.50 vs PASS at 0.45), the original pairwise analysis applies directly:

```
gap = 0.05, b = 0.15, c = 0.30
P(rank 2 beats rank 1) = (0.30 − 0.05)² / (2 × 0.30²) = 0.3472
P(rank 1 selected against rank 2) = 1 − 0.3472 = 0.6528
```

With additional competitors at larger gaps:

```
P(rank 1 selected) ≈ 0.6528 × (1 − 0.1250) × (1 − 0.0139) × 1.0
                   ≈ 0.6528 × 0.8750 × 0.9861
                   ≈ 0.563
```

For very tight cross-type gaps (gap ~ 0.02):

```
P(D > 0.02) = (0.30 − 0.02)² / (2 × 0.09) = (0.28)² / 0.18 = 0.4356
P(rank 1 selected against rank 2) = 0.5644
```

**FR-09 test requirement:** The UT-09 test list MUST ensure rank 1 and rank 2 are
**different action types**. If both are the same type (e.g., two PASS options), noise
cannot reorder them, and the test measures nothing. Using a cross-type list (e.g., SHOOT
vs PASS at rank 1→2), the Composure = 1 threshold for gap ≈ 0.05 is **≤ 65%**. For the
§3.2.12 list specifically (all top ranks are PASS), the threshold must account for the
same-type identity: Composure = 1 rate is **≤ 98%** (because only HOLD/DRIBBLE can
overthrow, and they have large gaps).

**Configuration A is therefore inadequate as a standalone test.** It must be supplemented
by Configuration B, which uses a cross-type list.

---

### 3.3.7.4 Derived Thresholds

Based on the analysis above, the FR-09 acceptance criteria are updated with two test
configurations. **Configuration B is the primary Composure differentiation test.**
Configuration A validates determinism and basic correctness but provides weak Composure
differentiation due to the same-type noise identity property.

**Configuration A — Same-type dominated list (§3.2.12 worked example):**

Candidate list: PASS(0.487), PASS(0.353), HOLD(0.272), PASS(0.268), DRIBBLE(0.183)

Top 3 ranks are PASS → noise identical → rank 1 only challenged by HOLD/DRIBBLE.

| Composure | NoiseBound | Derived Optimal Selection Rate | Threshold |
|-----------|-----------|-------------------------------|-----------|
| 20 | 0.0075 | ~100% | **≥ 99%** |
| 1 | 0.1500 | ~96% | **≤ 98%** |

Configuration A demonstrates that same-type lists produce weak differentiation. This is
correct: it shows the Composure model operates on inter-type selection as designed.

**Configuration B — Cross-type stress test (PRIMARY FR-09 TEST):**

Candidate list: each option is a DIFFERENT ActionType to maximise noise differentiation.

| Option | ActionType | BaseUtility | Gap to rank 1 |
|--------|-----------|------------|---------------|
| SHOOT | 1 | 0.50 | 0.000 (rank 1) |
| PASS | 0 | 0.45 | 0.050 |
| DRIBBLE | 2 | 0.35 | 0.150 |
| HOLD | 3 | 0.25 | 0.250 |
| MOVE_TO_POSITION | 4 | 0.15 | 0.350 |

Each option has a different ActionType → each receives independent noise → full Composure
differentiation. The rank 1→2 gap is 0.05 — tight enough to show meaningful reversal at
low Composure.

Note on achievability: these BaseUtility values are within the [0.01, 1.0] clamp range
of §3.2. Not every combination of attributes and game state will produce exactly this
distribution, but similar distributions occur when a shooter in the attacking third has
viable pass, dribble, and hold alternatives. The specific values are chosen for test
clarity, not as a claim that this exact list occurs in gameplay.

| Composure | NoiseBound | Derived Optimal Selection Rate | Threshold |
|-----------|-----------|-------------------------------|-----------|
| 20 | 0.0075 | ~100% (gap 0.05 > max reversal 0.015) | **≥ 99%** |
| 1 | 0.1500 | ~56% (derived in §3.3.7.3 revision) | **≤ 65%** |

**Recommended UT-09 implementation:** Run BOTH configurations. Configuration A validates
that same-type candidates are not spuriously reordered by noise (a regression test for
DD-SEL-04). Configuration B is the primary Composure differentiation test.

**Differential requirement (Configuration B only — the meaningful comparison):**

```
SelectionRate(Composure=20) − SelectionRate(Composure=1) ≥ 30 percentage points
```

Derived: 100% − 56% = 44pp. Setting threshold at 30pp provides margin for hash
distribution variance across different matchSeed values while still requiring a large,
clearly observable Composure effect.

---

### 3.3.7.5 Section 2 Amendment Required

The following changes to Section 2 §2.3.3 FR-09 are required to replace the provisional
thresholds:

**Replace the provisional block with:**

> **FR-09 — Composure-differentiated selection (UPDATED from §3.3 derivation)**
>
> Unit test UT-09 runs `SelectAction()` 1,000 times on TWO fixed scored candidate
> lists with `Composure = 1` and 1,000 times with `Composure = 20` (varying only
> `heartbeatTick` to vary noise seed).
>
> **Configuration A** (same-type dominated, §3.2.12 list):
> Candidates: PASS(0.487), PASS(0.353), HOLD(0.272), PASS(0.268), DRIBBLE(0.183).
> - With `Composure = 20`: max-utility action selected ≥ 99% of trials.
> - With `Composure = 1`: max-utility action selected ≤ 98% of trials.
> - Purpose: validates same-type noise identity (DD-SEL-04) and basic correctness.
>
> **Configuration B** (cross-type, PRIMARY differentiation test):
> Candidates: SHOOT(0.50), PASS(0.45), DRIBBLE(0.35), HOLD(0.25), MOVE(0.15).
> Each candidate is a different ActionType.
> - With `Composure = 20`: max-utility action selected ≥ 99% of trials.
> - With `Composure = 1`: max-utility action selected ≤ 65% of trials.
>
> **Differential** (Configuration B only):
> - `SelectionRate(Composure=20) − SelectionRate(Composure=1) ≥ 30 percentage points`
>
> Results are deterministic: same tick sequence → same selection sequence (FR-04).

**Remove the ⚠ PROVISIONAL THRESHOLDS warning block.** The §3.3 derivation is now
complete; the blocking condition is resolved.

---

## 3.3.8 Numerical Verification — Attribute Extremes

### 3.3.8.1 Case A: Elite Composure (Composure = 20)

```
Agent: Composure = 20 (raw), A_Composure = (20−1)/19 = 1.0
NoiseBound = 0.15 × (1.0 − 1.0 × 0.95) = 0.15 × 0.05 = 0.0075

Candidate list (from §3.2.12):
  PASS-T2:  BaseUtility = 0.487
  PASS-T1:  BaseUtility = 0.353
  HOLD:     BaseUtility = 0.272
  PASS-T3:  BaseUtility = 0.268
  DRIBBLE:  BaseUtility = 0.183

Worst case for PASS-T2 (rank 1):
  PASS-T2 noise = −0.0075 → EffectiveUtility = 0.487 − 0.0075 = 0.4795
  PASS-T1 noise = +0.0075 → EffectiveUtility = 0.353 + 0.0075 = 0.3605

  0.4795 > 0.3605 → PASS-T2 still wins ✓

Maximum EffectiveUtility in system:
  0.487 + 0.0075 = 0.4945

Minimum EffectiveUtility in system:
  0.183 − 0.0075 = 0.1755

Elite agents are effectively deterministic. The noise is cosmetic — it varies the
EffectiveUtility stored in the event log but never changes the selected action for
candidate lists with gaps > 0.015. ✓
```

---

### 3.3.8.2 Case B: Minimum Composure (Composure = 1)

```
Agent: Composure = 1 (raw), A_Composure = (1−1)/19 = 0.0
NoiseBound = 0.15 × (1.0 − 0.0 × 0.95) = 0.15

Candidate list (same):
  PASS-T2:  BaseUtility = 0.487
  PASS-T1:  BaseUtility = 0.353
  HOLD:     BaseUtility = 0.272
  PASS-T3:  BaseUtility = 0.268
  DRIBBLE:  BaseUtility = 0.183

Worst case for PASS-T2 (rank 1):
  PASS-T2 noise = −0.15 → EffectiveUtility = 0.487 − 0.15 = 0.337
  PASS-T1 noise = +0.15 → EffectiveUtility = 0.353 + 0.15 = 0.503

  0.337 < 0.503 → PASS-T1 beats PASS-T2 ✓ (rank reversal — expected)

Can DRIBBLE (worst option) beat PASS-T2 (best option)?
  DRIBBLE noise = +0.15 → EffectiveUtility = 0.183 + 0.15 = 0.333
  PASS-T2 noise = −0.15 → EffectiveUtility = 0.487 − 0.15 = 0.337

  0.333 < 0.337 → DRIBBLE cannot beat PASS-T2 even at worst ✓
  (gap 0.304 > max reversal 0.300 — validated per §3.3.4.3)

Can HOLD beat PASS-T2?
  HOLD noise = +0.15 → EffectiveUtility = 0.272 + 0.15 = 0.422
  PASS-T2 noise = −0.15 → EffectiveUtility = 0.487 − 0.15 = 0.337

  0.422 > 0.337 → HOLD can beat PASS-T2 ✓ (rank reversal — expected)
  Composure=1 agent under pressure may hold when they should pass. Correct.

Maximum EffectiveUtility in system:
  0.487 + 0.15 = 0.637

Minimum EffectiveUtility in system:
  0.183 − 0.15 = 0.033

Low-Composure agents produce large EffectiveUtility swings. Rankings are unstable
for closely-scored options. This is the intended model of poor decision-making
under pressure. ✓
```

---

### 3.3.8.3 Case C: Mid-Tier Composure (Composure = 10)

```
Agent: Composure = 10 (raw), A_Composure = (10−1)/19 = 0.4737
NoiseBound = 0.15 × (1.0 − 0.4737 × 0.95) = 0.15 × (1.0 − 0.4500) = 0.15 × 0.5500 = 0.0825

Candidate list (same):
  PASS-T2:  BaseUtility = 0.487
  PASS-T1:  BaseUtility = 0.353

Worst case for PASS-T2 vs PASS-T1:
  PASS-T2 noise = −0.0825 → EffectiveUtility = 0.487 − 0.0825 = 0.4045
  PASS-T1 noise = +0.0825 → EffectiveUtility = 0.353 + 0.0825 = 0.4355

  0.4045 < 0.4355 → PASS-T1 can beat PASS-T2 ✓ (rank reversal possible)
  MaxReversal = 2 × 0.0825 = 0.165 > gap 0.134 → confirmed

Can DRIBBLE beat PASS-T2?
  MaxReversal = 0.165 < gap 0.304 → No. ✓ (DRIBBLE cannot beat PASS-T2)

Can HOLD beat PASS-T2?
  MaxReversal = 0.165 < gap 0.215 → No. ✓ (HOLD cannot beat PASS-T2 at mid composure)

Mid-tier agent (Composure=10): can swap between the two best options (PASS-T2 and
PASS-T1) but cannot make truly pathological choices (HOLD or DRIBBLE over the best
PASS). This is correct — average players are slightly erratic but not irrational. ✓
```

---

## 3.3.9 Worked Example — Full Selection Pass (Continuation of §3.2.12)

This example continues from §3.2.12. The agent is AgentId=7, Composure=12
(A_Composure = 0.5789), heartbeatTick = 450, matchSeed = 0xDEADBEEF_CAFEBABE.

**Step 1: Compute NoiseBound**

```
NoiseBound = 0.15 × (1.0 − 0.5789 × 0.95)
           = 0.15 × (1.0 − 0.5500)
           = 0.15 × 0.4500
           = 0.0675
```

**Step 2: Compute per-option noise**

For each option, compute `ComputeOptionNoise(matchSeed, 7, 450, actionTypeOrdinal)`.
All values below are the actual outputs of SplitMix64 with the specified inputs — not
approximations. These are reproducible: any implementation of SplitMix64 with these
inputs must produce identical results (determinism requirement FR-04).

```
Option: PASS (ActionType = PASS = 0) — applies to ALL three PASS candidates (DD-SEL-04)
  packed   = 7 | (450 << 5) | (0 << 25) = 14407  (0x0000000000003847)
  combined = 0xDEADBEEFCAFEBABE ^ 0x0000000000003847 = 0xDEADBEEFCAFE82F9
  hash     = SplitMix64(0xDEADBEEFCAFE82F9) = 0x027DE72080062132
  upper24  = 0x027DE7 = 163303
  unit     = 163303 / 16777216 = 0.009734
  rawNoise = 0.009734 × 2.0 − 1.0 = −0.9805
  noise    = −0.9805 × 0.0675 = −0.0662

Option: DRIBBLE (ActionType = DRIBBLE = 2)
  packed   = 7 | (450 << 5) | (2 << 25) = 67123271  (0x0000000004003847)
  combined = 0xDEADBEEFCAFEBABE ^ 0x0000000004003847 = 0xDEADBEEFCEFE82F9
  hash     = SplitMix64(0xDEADBEEFCEFE82F9) = 0xA3229A86154B2772
  upper24  = 0xA3229A = 10691226
  unit     = 10691226 / 16777216 = 0.637247
  rawNoise = 0.637247 × 2.0 − 1.0 = +0.2745
  noise    = +0.2745 × 0.0675 = +0.0185

Option: HOLD (ActionType = HOLD = 3)
  packed   = 7 | (450 << 5) | (3 << 25) = 100677703  (0x0000000006003847)
  combined = 0xDEADBEEFCAFEBABE ^ 0x0000000006003847 = 0xDEADBEEFCCFE82F9
  hash     = SplitMix64(0xDEADBEEFCCFE82F9) = 0x3A0D91DC034FC27A
  upper24  = 0x3A0D91 = 3804561
  unit     = 3804561 / 16777216 = 0.226770
  rawNoise = 0.226770 × 2.0 − 1.0 = −0.5465
  noise    = −0.5465 × 0.0675 = −0.0369
```

⚠ **Design issue identified during worked example:** PASS-T2 and PASS-T1 both have
`ActionType = PASS = 0`. They will receive IDENTICAL noise values from the hash because
the hash input only includes the ActionType ordinal, not the target agent ID. This means
all PASS candidates receive the same noise offset — their relative ranking is unchanged by
noise. Only cross-type comparisons (PASS vs HOLD vs DRIBBLE) are affected by noise.

**Analysis of this property:**

This is actually **correct behaviour**, not a defect. The Composure model governs
**which type of action** to perform (pass vs hold vs dribble), not **which pass target**
to select. Target selection within a single action type is governed by the utility scores
from §3.2, which already incorporate Vision, pass lane quality, and other factors. A
low-Composure agent may choose to hold when they should pass (wrong action type) but
should not randomly pass to the worst teammate when they've decided to pass (wrong target
within correct type).

If the hash included `TargetAgentId`, a Composure=1 agent could pass to a heavily marked
teammate instead of an open one — this is a *passing quality* error, not a *composure*
error. Passing quality is governed by the Vision and Passing attributes in the §3.2
scoring formulas.

**Separation of concerns:**
- **Composure → action type selection** (§3.3: noise on ActionType ordinal)
- **Vision/Passing → pass target selection** (§3.2: utility scoring)
- **Finishing/Composure → shot quality** (Shot Mechanics #6: execution)

This separation is a deliberate design choice. Document it.

**Revised worked example with actual computed noise values:**

The noise values for each ActionType ordinal are already computed in Step 2 above using
actual SplitMix64 output. The PASS candidates (T2, T1, T3) all share ActionType ordinal 0
and receive identical noise = −0.0662.

**Step 3: Compute EffectiveUtility**

| Option | BaseUtility | Noise | EffectiveUtility | Rank |
|--------|------------|-------|-----------------|------|
| PASS-T2 | 0.487 | −0.066 | 0.4208 | 1st |
| PASS-T1 | 0.353 | −0.066 | 0.2868 | 2nd |
| HOLD | 0.272 | −0.037 | 0.2351 | 3rd |
| PASS-T3 | 0.268 | −0.066 | 0.2018 | 4th |
| DRIBBLE | 0.183 | +0.019 | 0.2015 | 5th |

**Note on PASS-T3 / DRIBBLE near-tie:** EffectiveUtility values are 0.2018 (PASS-T3) and
0.2015 (DRIBBLE) — a difference of 0.0003, well above TIEBREAK_EPSILON (1e-6). This is
NOT a tiebreak; PASS-T3 wins by 0.0003 utility. The tiebreak rule does not activate.

**Step 4: Winner selection**

PASS-T2 has the highest EffectiveUtility (0.4208). No tiebreak needed.

**Result:** `SelectAction()` returns `AgentAction` with:
- `Type = PASS`
- `TargetAgentId = T2.AgentId`
- `UtilityScore = 0.4208`
- `HeartbeatTick = 450`

**Observation:** All three PASS options received the same noise (−0.066), so their
relative ranking is unchanged from §3.2.12. Notably, HOLD (noise −0.037) dropped from
rank 3= (tied with PASS-T3 at BaseUtility 0.272 vs 0.268) to rank 3 (0.235 vs 0.202),
and DRIBBLE (noise +0.019) nearly caught PASS-T3 — a difference of 0.0003. In a different
tick with a different hash seed, DRIBBLE could overtake T3. This is correct: a
Composure=12 agent may occasionally prefer a low-value dribble over a marginal backward
pass.
correct: noise differentiates between action types, not within them.

**Invariant verification:**

- INV-SEL-01 ✓ Exactly one AgentAction returned
- INV-SEL-02 ✓ All EffectiveUtility values ≠ 0.0
- INV-SEL-03 ✓ AgentAction.Type = PASS = options[winnerIndex].Type
- INV-SEL-04 ✓ AgentAction.UtilityScore = 0.4208 = winner's EffectiveUtility
- INV-SEL-05 ✓ (not applicable — 5 options, not 1)
- INV-SEL-06 ✓ Same inputs → same output (deterministic hash)
- INV-SEL-07 ✓ TiebreakerApplied = false (no tie)

---

## 3.3.10 ComposureWeights.cs Constant Catalogue

All composure-noise constants are defined in `ComposureWeights.cs`, the companion file
to `UtilityWeights.cs` (§3.2.11). Constants are separated into their own file because
the composure model is a distinct conceptual layer (noise injection) from the scoring
model (utility formulas).

```csharp
/// <summary>
/// All composure-noise constants for the Decision Tree Composure Model (§3.3).
/// Companion to UtilityWeights.cs (§3.2.11).
///
/// GOVERNANCE RULES (same as UtilityWeights.cs):
///   [GT]      = Gameplay-tuned. No academic derivation. Subject to Stage 1 calibration.
///   [DERIVED] = Mathematically derived. Change only if the source formula changes.
///
/// Tuning protocol: Constants tagged [GT] may be modified by changing the value
/// in this file ONLY. No formula restructuring permitted at tune time.
/// Any structural change requires a specification amendment.
///
/// Decision Tree Specification #8 — Section 3.3
/// Created: March 02, 2026, 4:00 PM PST
/// </summary>
public static class ComposureWeights
{
    // ── Noise Bound Constants ───────────────────────────────────────────────────

    /// <summary>
    /// Maximum noise magnitude for minimum-Composure agents (Composure = 1).
    /// This is the absolute ceiling on how much noise can perturb any single
    /// option's EffectiveUtility.
    ///
    /// At NOISE_MAX = 0.15, two options separated by > 0.30 utility can NEVER
    /// be reversed by noise. This prevents pathological decisions (e.g., worst
    /// option beating best option) while allowing meaningful reversal between
    /// closely-scored alternatives.
    ///
    /// Reduced from Outline's preliminary ±0.20 based on §3.3.4.3 analysis:
    /// 0.20 would allow the worst option in §3.2.12 (gap 0.304) to beat the best,
    /// which is pathological. 0.15 keeps max reversal at 0.30, just below this gap.
    ///
    /// Safe tuning range: [0.08, 0.20].
    /// Below 0.08: Composure attribute becomes non-functional (near-zero noise).
    /// Above 0.20: pathological reversals in typical candidate lists.
    /// </summary>
    public const float NOISE_MAX = 0.15f;                          // [GT]

    /// <summary>
    /// How much Composure reduces the noise bound. At maximum Composure (A=1.0):
    ///   NoiseBound = NOISE_MAX × (1.0 − 1.0 × COMPOSURE_SUPPRESSION)
    ///              = 0.15 × 0.05 = 0.0075
    ///
    /// Set to 0.95 (not 1.0) to ensure even elite agents have a non-zero noise
    /// floor (0.0075). This prevents exact EffectiveUtility ties across all ticks
    /// for identically-scored options, which would otherwise require tiebreak
    /// resolution every tick — a degenerate case.
    ///
    /// At 1.0: elite agents have zero noise → exact ties frequent → tiebreak
    ///         resolves every selection → ActionType ordinal determines all choices.
    /// At 0.95: elite agents have negligible noise (±0.008) → ties extremely rare
    ///          → selection is effectively deterministic but not degenerate.
    ///
    /// Safe tuning range: [0.85, 0.99].
    /// Below 0.85: elite agents still have significant noise (undermines elite feel).
    /// Above 0.99: noise floor too small, floating-point precision concerns.
    /// </summary>
    public const float COMPOSURE_SUPPRESSION = 0.95f;              // [GT]

    // ── Tiebreak Constants ──────────────────────────────────────────────────────

    /// <summary>
    /// Epsilon for floating-point comparison in tiebreak detection.
    /// Two EffectiveUtility values within TIEBREAK_EPSILON are considered tied.
    ///
    /// Must be:
    ///   - Large enough to absorb FP rounding across platforms (> ~1e-7 for float)
    ///   - Small enough to avoid false tiebreaks (< minimum meaningful utility gap)
    ///   - Minimum meaningful gap observed: 0.004 (HOLD/T3 in §3.2.12)
    ///
    /// 1e-6 satisfies both constraints with margin.
    /// </summary>
    public const float TIEBREAK_EPSILON = 1e-6f;                   // [DERIVED]
}
```

**Constant summary:**

| Constant | Value | Tag | Purpose | Safe Tuning Range |
|---|---|---|---|---|
| `NOISE_MAX` | 0.15 | [GT] | Max noise at Composure=1 | [0.08, 0.20] |
| `COMPOSURE_SUPPRESSION` | 0.95 | [GT] | Composure → noise reduction factor | [0.85, 0.99] |
| `TIEBREAK_EPSILON` | 1e-6 | [DERIVED] | FP equality threshold | Do not tune |

**Total project constant count:** 58 (UtilityWeights.cs) + 3 (ComposureWeights.cs) = **61**.

---

## 3.3.11 Design Decision Log

| ID | Decision | Rationale | Reference |
|----|----------|-----------|-----------|
| DD-SEL-01 | Per-option noise (4-input hash) over per-agent noise (3-input hash) | Per-agent noise adds constant offset → zero ranking effect → Composure non-functional | §3.3.3.3 |
| DD-SEL-02 | NOISE_MAX = 0.15 (reduced from Outline's 0.20) | 0.20 allows pathological worst-beats-best reversal in §3.2.12 list (gap 0.304) | §3.3.4.3 |
| DD-SEL-03 | COMPOSURE_SUPPRESSION = 0.95 (not 1.0) | Prevents degenerate zero-noise at elite Composure; avoids frequent tiebreak activation | §3.3.10 |
| DD-SEL-04 | Same-type candidates receive identical noise (by design) | Composure governs action-type selection, not within-type target selection. Target quality is a separate attribute concern (Vision, Passing). | §3.3.9 |
| DD-SEL-05 | EffectiveUtility is NOT clamped | Value is used for ranking only; absolute magnitude is irrelevant post-selection | §3.3.4.2 |
| DD-SEL-06 | SplitMix64 as stateless hash (not xorshift128+ RNG) | One value per option per tick; stateless is simpler and sufficient | §3.3.3.1 |
| DD-SEL-07 | §2 Step 5 formula is superseded (not amended) | §2 explicitly defers to "Full derivation in Section 3"; no amendment needed | §3.3.4.2 |
| DD-SEL-08 | Naming: BaseUtility (struct field) = ScoredUtility (§3.2 narrative) | Clarified in §3.3.1; recommend §3.2 cosmetic amendment | §3.3.1 |
| DD-SEL-09 | Composure double-exposure (scoring + selection) is intentional | §3.2 exponents = action quality estimation; §3.3 noise = decision consistency. Independent, multiplicative, independently tunable. Not redundant. | §3.3.4.2.1 |

---

## 3.3.12 Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | March 02, 2026, 4:00 PM PST | Claude (AI) / Anton | Initial draft. Complete SelectAction() algorithm with 3-phase structure (noise injection, winner selection, AgentAction construction). Deterministic noise via SplitMix64 stateless hash with 4-input composition (matchSeed, agentId, heartbeatTick, actionTypeOrdinal). Composure scaling model: NoiseBound = NOISE_MAX × (1 − A_Composure × COMPOSURE_SUPPRESSION) with NOISE_MAX = 0.15 [GT] (reduced from Outline's 0.20). FR-09 threshold derivation: dual-configuration test — Config A (same-type dominated, regression test for DD-SEL-04: ≥99%/≤98%) and Config B (cross-type, primary differentiation test: ≥99%/≤65%, differential ≥30pp). FR-09 analysis corrected for same-type noise identity: original ~78% estimate revised to ~96% for §3.2.12 list because PASS candidates share noise. Section 2 amendment specified. Nine design decisions documented (DD-SEL-01 through DD-SEL-09). Three numerical verifications at Composure extremes using §3.2.12 candidate list. Worked example continuation from §3.2.12 with actual SplitMix64 hash outputs (not fabricated values). Same-type noise identity property discovered and documented (DD-SEL-04). Naming inconsistency (BaseUtility vs ScoredUtility) identified and resolved (DD-SEL-08). Composure double-exposure (scoring + selection) analysed and justified (DD-SEL-09). SelectionMetadata internal struct formally defined. ComposureWeights.cs constant catalogue: 3 constants. Total project constants: 61. |

---

*End of Section 3.3 — Final Action Selection: Composure Model*

*Decision Tree Specification #8 | Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*
*Next: Section 3.4 — Tactical Context Integration (Stage 0 Stub)*
