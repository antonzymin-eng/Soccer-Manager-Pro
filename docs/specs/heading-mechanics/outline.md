# Heading Mechanics Specification #10 — Outline

## Purpose
Define deterministic, skill-informed heading interactions that feel realistic and tactically legible.

## Scope
Covers eligibility, timing quality, outcome generation, contested interactions, and validation criteria for heading actions.

## Section Plan
- Section 1 — Header eligibility rules
  - Proximity thresholds, jump windows, body-orientation requirements.
  - Aerial-state gating and invalid-state rejection.
- Section 2 — Timing and contact-quality model
  - Early / perfect / late contact windows.
  - Outcome shaping multipliers by contact quality.
- Section 3 — Direction and power generation
  - Attribute influences.
  - Momentum and incoming ball-flight integration.
- Section 4 — Contested header model
  - Duel resolution using balance/strength.
  - Collision interactions and disturbance factors.
- Section 5 — Edge-case handling
  - Glancing contact behavior.
  - Complete misses and defensive-clearance variants.
  - Own-goal risk modeling.
- Section 6 — Tuning controls and telemetry
  - Designer-exposed parameters.
  - Runtime counters for contact quality and duel outcomes.
- Section 7 — Unit validation scenarios
  - Eligibility, timing-window, and direction/power assertions.
- Section 8 — Integration match-feel scenarios
  - Crosses, set pieces, and aerial duels mapped to feel criteria.
- Section 9 — Approval checklist
- Appendices
  - Parameter ranges and exemplar tuning profiles.
