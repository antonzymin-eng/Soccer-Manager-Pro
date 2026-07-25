# Board & Ownership Dynamics #45 — Section 3: Algorithms

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.2 — AR-2 sweep fix)
**Version:** 0.2
**Status:** IN REVIEW

---

All arithmetic is **integer per-mille** (FR-BD-003). No float appears at any tier. The minimal tier makes
**no stochastic draw** (FR-BD-006); §3.5 is the deep tier's only draw site.

## 3.1 `AdvanceBoardDay` — the daily step (FM-BD-01)

Invoked once per modelled club per world day at #30's tick-order slot 8 (§4.4). `input` carries
**committed values** #30 routes in — #45 references neither #30 nor the league table.

```
AdvanceBoardDay(ref BoardConfidence c, in OwnershipProfile own, in BoardDayInput input, worldDay):
    # 0. Validate BEFORE any mutation (FR-BD-023 — a refused advance mutates nothing).
    RequireValidProfile(own)                       # F4: default() (all dials 0) and any dial <= 0 throw
    RequireInRange(input.ObjectiveTrackPermille, 0, 1000)      # F1
    RequireInRange(input.MoraleSignalPermille,   0, 1000)      # F1
    RequireInRange(c.ConfidencePermille,         0, 1000)      # F1 — a corrupt carrier fails at the seam

    # 1. F6 idempotency / gap guard (the #33 shape, verbatim).
    if c.LastAdvancedWorldDay != BD_NOT_ADVANCED_SENTINEL:
        if worldDay == c.LastAdvancedWorldDay:      return       # no-op — already advanced
        if worldDay != c.LastAdvancedWorldDay + 1:  throw        # day gap — #30 advances one day at a time

    # 2. Target from committed inputs (§3.2) — deterministic, integer, clamped [0,1000].
    target := ComputeConfidenceTarget(input, own)

    # 3. Drift toward it by a bounded step whose EROSION rate is the owner's patience dial (§3.3).
    step := (target < c.ConfidencePermille)
            ? ScaleDial(BD_CONFIDENCE_DRIFT_STEP_PERMILLE, own.PatienceDecayPermille)   # falling
            : BD_CONFIDENCE_DRIFT_STEP_PERMILLE                                         # rising / equal

    c.ConfidencePermille := DriftPermille(c.ConfidencePermille, target, step)     # clamped [0,1000]

    # 4. Stamp the cursor LAST, so a throw above leaves the day retryable.
    c.LastAdvancedWorldDay := worldDay
```

`DriftPermille(cur, tgt, step) = cur + sign(tgt − cur) · min(step, |tgt − cur|)`, clamped `[0,1000]` —
deterministic, monotone, and idempotent at `cur == tgt`. This is #45's own declaration of the shape #33
§3.1 specifies (KD-1); §5 pins the two semantically equivalent.

**Stamp-last** matters: validation and the target computation can throw, and a cursor stamped before them
would silently consume the day.

## 3.2 `ComputeConfidenceTarget` — target assembly (FM-BD-02)

The owner's `ExpectationSeverity` dial **shifts the reference point** rather than scaling the deviation.
This is the design choice worth stating, because the obvious alternative is wrong: scaling a signed
deviation by severity makes a demanding owner *harsher on success and more forgiving of failure*, since
dividing a negative deviation by a factor > 1 moves it toward neutral. Shifting the reference is
sign-coherent — a demanding owner simply requires a higher on-track reading to feel neutral.

```
ComputeConfidenceTarget(in BoardDayInput input, in OwnershipProfile own) -> int:
    # 1. Where "acceptable" sits for THIS owner. Identity (1000) => the neutral 500.
    reference := ScaleDial(BD_TRACK_NEUTRAL_PERMILLE, own.ExpectationSeverityPermille)

    # 2. Performance relative to that reference, re-centred on neutral confidence.
    target := BD_CONFIDENCE_NEUTRAL_PERMILLE + (input.ObjectiveTrackPermille - reference)

    # 3. Deep-tier morale contribution. BD_MORALE_WEIGHT_PERMILLE = 0 at minimal => contributes EXACTLY 0.
    target := target + ScaleDial(input.MoraleSignalPermille - BD_TRACK_NEUTRAL_PERMILLE,
                                 BD_MORALE_WEIGHT_PERMILLE)

    return Clamp(target, 0, 1000)
```

where `ScaleDial(v, dialPermille) = (int)(((long)v * dialPermille) / 1000)`.

**Identity property (load-bearing, §5 locks it).** At `ExpectationSeverityPermille = 1000` and
`BD_MORALE_WEIGHT_PERMILLE = 0`:

```
reference = 500  ⇒  target = 500 + (track − 500) = track
```

— the target is **exactly** the committed on-track projection. The minimal tier therefore adds no model
of its own on top of #30's: it only gives that projection *memory*. Every deep-tier dial deforms this
identity; none replaces it.

## 3.3 `TryProjectBoardModifier` — the #40 seam (FM-BD-03)

```
TryProjectBoardModifier(clubId, out BoardModifier mod) -> bool:
    if not TryGet(clubId, out c, out own):
        mod := default; return false            # NOT an error — a named legal state (FR-BD-018)
    RequireValidProfile(own)                    # present but malformed still fails loud (F4)

    dev   := c.ConfidencePermille - BD_CONFIDENCE_NEUTRAL_PERMILLE          # [-500, +500]
    delta := (int)(((long)dev * own.BudgetContributionPermille
                              * BD_BUDGET_SENSITIVITY_PERMILLE) / 1_000_000L)
    mod   := new BoardModifier(Clamp(1000 + delta, BD_BUDGET_MULT_MIN, BD_BUDGET_MULT_MAX))
    return true
```

**Why the minimal tier is exactly identity.** `BD_BUDGET_SENSITIVITY_PERMILLE = 0` at minimal, so `delta`
is `0` for **every** confidence value and the projection returns `BoardModifier(1000)` — identity —
regardless of how far confidence has drifted. This is the honest reading of "minimal": #45 *tracks* the
board relationship but does not yet let it move money. Turning sensitivity on is the deep tier's named
activation, not a silent consequence of confidence moving (FR-BD-019 / KD-8).

**Overflow.** The product is computed in `long` and narrowed after the divide. Worst case with the
Appendix A dial bounds — `|dev| ≤ 500`, contribution `≤ BD_DIAL_MAX`, sensitivity `≤ 1000` (its declared
`[0,1000]` bound, enforced at the seam — this argument rests on it) — is
`500 · BD_DIAL_MAX · 1000`; `BD_DIAL_MAX = 2000` puts that at `1.0 × 10⁹`, inside `int` range even before
the `long` promotion. The `long` intermediate is belt-and-braces, and the dial bound is what actually
guarantees it — which is why `BD_DIAL_MAX` is `[FIXED]`, not a tunable.

## 3.4 `DeriveJobSecurityBand` — the KD-5 projection (FM-BD-04)

The band #30's `JobSecurity` becomes at #45 T2 (ERR-030-009). **Derived on read, stored nowhere** — that
is the whole point: a stored band would be the second truth KD-5 exists to remove.

```
DeriveJobSecurityBand(confidencePermille) -> JobSecurityBand:
    if confidence <  BD_BAND_CRITICAL_MAX:  return Critical      # [0, 200)
    if confidence <  BD_BAND_INSECURE_MAX:  return Insecure      # [200, 450)
    if confidence <  BD_BAND_STABLE_MAX:    return Stable        # [450, 750)
    return Secure                                                # [750, 1000]
```

Bands are **half-open ascending and exhaustive** — every value in `[0,1000]` maps to exactly one band,
and the boundaries belong to the upper band (`200 → Insecure`, not `Critical`). Stated explicitly because
an off-by-one at a band edge is invisible in play and produces a sacking that looks like a bug.

## 3.5 `AdvanceTakeovers` — the deep tier's only draw (FM-BD-05, deferred)

Specified here so the determinism contract is reviewable now; **not built at the minimal tier**
(FR-BD-020).

```
AdvanceTakeovers(clubId, worldDay, ...):
    ordinal := DeriveActionOrdinal(clubId, worldDay, DRAW_PURPOSE_TAKEOVER)
    roll    := DrawKeyed(boardStream, ordinal, 1000)          # [0,1000)
    if roll >= takeoverChancePermille: return                  # no takeover today
    ... mutate #45-owned state ONLY (FR-BD-026) ...

DeriveActionOrdinal(clubId, worldDay, purpose) -> u64:
    require 0 <= purpose < DRAW_PURPOSE_RADIX                  # bound guard
    require 0 <= clubId  < BD_CLUB_STRIDE                      # injectivity guard — WITHOUT this, an
                                                              # out-of-stride club silently aliases onto
                                                              # another club's ordinal: same takeover, no
                                                              # error, no divergence signal
    return ((u64)worldDay * DRAW_PURPOSE_RADIX + purpose) * BD_CLUB_STRIDE + (u64)clubId
```

`DRAW_PURPOSE_RADIX` is **fixed**, never "the current purpose count" — a growing radix re-keys every
historical ordinal the moment a purpose is appended, breaking cross-version replay parity (#41's own
review finding, adopted here). The ordinal is a pure function of its arguments, so the draw is
**position-independent**: it does not matter how many draws preceded it, and there is consequently no
cursor to persist (FR-BD-021, Appendix B).

## 3.6 Division convention (pinned)

Every division above is C# **integer division, truncating toward zero**, which is **sign-symmetric**:
`(−7)/2 == −3` and `7/2 == 3`. Two substitutions would silently break that symmetry and are therefore
forbidden in #45:

- `Math.Floor` — rounds toward −∞, so a negative deviation would move one per-mille further than its
  positive mirror.
- `Math.Round` — banker's rounding **and** it operates on `double`, violating FR-BD-003 outright.

§5 locks the symmetry directly (`±N` produce equal-magnitude opposite moves), so a future "cleanup" to
either function fails a test rather than quietly skewing every negative case.

## 3.7 Worked examples (hand-verifiable)

All at `BD_CONFIDENCE_NEUTRAL_PERMILLE = BD_TRACK_NEUTRAL_PERMILLE = 500`,
`BD_CONFIDENCE_DRIFT_STEP_PERMILLE = 20`, `BD_MORALE_WEIGHT_PERMILLE = 0`.

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | Identity owner, exactly on track (`track = 500`), `conf = 500` | `reference = 500`; `target = 500 + (500−500) = 500`; `|Δ| = 0` | `conf = 500` — **idempotent** |
| (b) | Identity owner, doing well (`track = 800`), `conf = 500` | `target = 500 + 300 = 800`; rising ⇒ `step = 20`; `500 + min(20,300)` | `conf = 520` |
| (c) | Identity owner, struggling (`track = 200`), `conf = 500` | `target = 200`; falling ⇒ `step = 20·1000/1000 = 20` | `conf = 480` |
| (d) | **Demanding** owner (`severity = 1200`), same `track = 800`, `conf = 500` | `reference = 500·1200/1000 = 600`; `target = 500 + (800−600) = 700` — the same performance is worth 100 less than in (b) | `conf = 520`, toward a lower ceiling |
| (e) | **Impatient** owner (`patience = 1500`), `track = 200`, `conf = 500` | `target = 200`; falling ⇒ `step = 20·1500/1000 = 30` | `conf = 470` — erodes faster than (c) |
| (f) | Minimal `BoardModifier`, `conf = 380` (drifted), `sensitivity = 0` | `dev = −120`; `delta = (−120·1000·0)/10⁶ = 0`; `mult = 1000` | **`Identity`** — FR-BD-019 |
| (g) | Deep `BoardModifier`, `conf = 800`, `contribution = 1000`, `sensitivity = 200` | `dev = 300`; `delta = (300·1000·200)/10⁶ = 60` | `mult = 1060` |
| (h) | Band edge, `conf = 200` | `200 < 200` false ⇒ not `Critical`; `200 < 450` true | **`Insecure`** |

Example (d) is the one that justifies §3.2's reference-shift: under the rejected deviation-scaling
formulation the same setup would have produced a target **above** identity's for a *struggling* club,
which is the opposite of what a demanding owner means.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial §3 (FM-BD-01..05: daily step with validate-before-mutate + stamp-last, the reference-shift target assembly and why deviation-scaling is wrong, the #40 projection with its long-intermediate overflow argument, the half-open band derivation, the deferred keyed takeover draw with both ordinal guards, the §3.6 division-convention lock, eight hand-verifiable worked examples). Status IN REVIEW. |
| 0.2 | 2026-07-25 | — | AR-2 fix (L): §3.3's overflow argument was stated at `sensitivity ≤ 1000` but that bound was **declared nowhere** — the argument rested on an unstated premise. Now cites the explicit `[0,1000]` bound `BD_BUDGET_SENSITIVITY_PERMILLE` carries in A.3. |
#endregion
