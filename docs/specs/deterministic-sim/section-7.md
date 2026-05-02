# Deterministic Simulation Specification #16 — Section 7: Future Extensions

## 7.1 Stage 1+ Extensions
- Deterministic lockstep networking compatibility profile.
- Differential snapshot compression with deterministic decode guarantees.
- Automated minimization of desync repro traces.

### 7.1.1 Extension admission criteria
| Criterion | Requirement |
|---|---|
| Determinism impact analysis | mandatory written assessment |
| Backward compatibility | no break to existing replay corpus |
| Certification cost | <= 20% increase unless approved waiver |
| Rollout plan | staged behind feature flag |

## 7.2 Deferred Decisions
- Final digest algorithm upgrade path (`v1 → v2`) and migration cadence. Trigger criteria (any of): (a) NIST formal deprecation of SHA-256 for cryptographic use, (b) measured >10% Snapshot+Digest CPU share regression attributable to SHA-256 across two consecutive certification cycles, (c) format change requiring extended preimage that cannot be expressed under `DigestVersion=1`. Coexistence policy: `DigestVersion` field in the snapshot header lets `v1` and `v2` snapshots be distinguished at load time; replay readers MUST support the previous version for one full release cycle after promotion.
- Long-horizon replay storage tiering strategy.
- Optional Tier B expansion process for select physics-derived analytics fields.
- Stage 5 cross-platform activation criteria and toolchain hardening requirements.

### 7.2.1 Decision log template
Each deferred item MUST include: context, options considered, owner, target decision milestone, and rollback plan.

## 7.3 Permanent Exclusions
- Any use of wall-clock entropy in authoritative simulation.
- Platform-specific floating behavior without explicit tolerance governance.
- Non-deterministic container traversal in authoritative paths.

### 7.3.1 Exception policy
There are no runtime exceptions for permanent exclusions. Any requested exception MUST be rejected or rewritten as non-authoritative Tier C behavior.

## 7.4 Version History
- **v0.7 (May 2, 2026):** §7.2 digest upgrade item now binds explicit trigger criteria (NIST deprecation / measured CPU regression / format change) and a coexistence policy keyed on `DigestVersion` field.
- **v0.5:** Added extension admission criteria, decision-log template, and exclusion exception policy.
- **v0.3:** Deferred roadmap and exclusions synchronized with deterministic governance model.

## 7.5 Candidate Extension Backlog (Illustrative)
| Extension | Benefit | Determinism risk | Readiness |
|---|---|---|---|
| deterministic rollback netcode | multiplayer resilience | high | research |
| snapshot delta trees | storage reduction | medium | prototype |
| auto-desync minimizer | faster triage | low | planned |

## 7.6 Extension Review Workflow
1. submit RFC with determinism impact analysis,
2. run pilot corpus with feature flag on/off,
3. compare digest parity across supported platforms,
4. approve for staged rollout only if no Tier A regressions.
