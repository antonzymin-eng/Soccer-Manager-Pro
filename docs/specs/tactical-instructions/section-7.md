# Tactical Instructions Specification #21 — Section 7: Future Extensions & Stage Binding

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED (June 20, 2026)

---

## 7.1 Stage gating (KD-8 / FR-TI-030)

Runtime activation requires two prerequisites, neither owned by this spec:

1. **`[GT]` config-loader** (`src/CLAUDE.md` "WHAT IS NOT HERE YET") — the mechanism that injects
   manager-set values at boot/on-change. Until it exists, instruction defaults are the hardcoded `[GT]`
   constants and no manager input is possible.
2. **Match-engine Phase C (Resolve) + Phase D (AI)** — the consumers are stubs until then.

## 7.2 Implementation sequencing (T0–T4)

| Stage | Deliverable | Gate |
|---|---|---|
| T0 | `tactical-instructions/` assembly: all enums + structs + identity factories + `EnumOrdinalStabilityTests` | landable now (no live consumer); behaviour-neutral |
| T1 | `[GT]` config-loader + `TacticLoader` | unblocks injection |
| T2 | #8 seam: route via `TacticalContext`; `Mentality` map + `RoleWeightModifiers` (own balance + adversarial pass) | with match-engine Phase D |
| T3 | #12/#13/#14/#15 snapshot fields + consumer-side translation maps | as each Mechanics tick is wired |
| T4 | GK distribution policy, in-possession granular instructions, time-wasting, expanded formations, set-piece duties | polish |

## 7.3 Stage-1+ extensions (deferred, not in this spec's normative surface)

- In-possession granular instructions (overlap/underlap, work-ball-into-box, cross type, play-out-of-defence).
- Expanded `TacticFormation` / `PlayerRole` rosters (curated subset at Stage 1; append later — ordinal stability holds).
- Opposition-instructions (per-opponent show-inside/outside, hard-tackle).
- Set-piece *execution* routines (this spec defines only duty-assignment flags).
- Touchline-shout/morale interactions and AI-manager tactic selection.

## 7.4 Permanent exclusions

- Per-tick directive production (owned by subsystems — KD-1).
- Any physics/coordinate semantics.
- RNG / stochastic instruction effects (KD-6).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Stage gating, T0–T4 sequencing, deferrals, permanent exclusions. |
#endregion
