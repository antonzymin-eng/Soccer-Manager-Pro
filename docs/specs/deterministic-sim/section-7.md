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
- Final digest algorithm upgrade path (`v1 → v2`) and migration cadence. Trigger criteria (any of): (a) NIST formal deprecation of SHA-256 for cryptographic use, (b) measured >10% Snapshot+Digest CPU share regression attributable to SHA-256 across two consecutive certification cycles, (c) format change requiring extended preimage that cannot be expressed under `DigestVersion=1`, (d) any change to `PHYSICS_TICK_HZ`, `TACTICAL_TICK_HZ`, or the derived `AI_PHASE_STRIDE` (per §3.1.2 — heartbeat changes reshape the per-tick `AI` vs `AI_NoOp` rollup composition), (e) any change to a domain-tag value, hash-input field width, or canonical-serializer encoding rule (§3.2.4 / §3.2.4.1). The list is normative and closed; new triggers MUST be added here before invoking a `DigestVersion` bump. Coexistence policy: `DigestVersion` field in the snapshot header lets `v1` and `v2` snapshots be distinguished at load time; replay readers MUST support the previous version for one full release cycle after promotion.
- Long-horizon replay storage tiering strategy.
- Optional Tier B expansion process for select physics-derived analytics fields.
- Stage 5 cross-platform activation criteria and toolchain hardening requirements.

### 7.2.1 Decision log template
Each deferred item MUST include: context, options considered, owner, target decision milestone, and rollback plan.

## 7.3 Permanent Exclusions
- Any use of wall-clock entropy in authoritative simulation.
- Platform-specific floating behavior without explicit **governance** (tolerance or environment) — see §7.3.1 carve-out.
- Non-deterministic container traversal in authoritative paths.

### 7.3.1 Exception policy
There are no runtime exceptions for permanent exclusions. Any requested exception MUST be rejected or rewritten as non-authoritative Tier C behavior.

**§1.3.1.1 carve-out (Stage 0 only):** The exclusion of "platform-specific floating behavior" does not prohibit Tier-A classification of `float` fields at Stage 0 when **environment governance** is applied per §1.3.1.1 — i.e., worker count, reduction topology, and SIMD level are pinned and recorded in `EnvironmentFingerprint`. This is *environment governance*, which satisfies the spirit of this exclusion for single-machine Stage 0 builds. A `float` Tier-A field that relies only on environment pinning (not tolerance rows) is compliant with §7.3 only when it satisfies both conditions in §1.3.1.1. Audit reviewers challenging a Stage 0 `float` Tier-A field under §7.3 should be directed to §1.3.1.1 for the explicit carve-out. Stage 5+ removes this carve-out by migrating to Fixed64.

## 7.4 Version History
- **v1.0 (May 4, 2026):** Pass 6 follow-up audit. §7.2 `v1 → v2` trigger criteria extended with two new triggers normatively required by Pass 6 changes: (d) heartbeat-rate change per §3.1.2, (e) domain-tag / hash-input-width / canonical-serializer-rule change per §3.2.4 / §3.2.4.1. List is now declared closed (additions require a §7.2 edit first).
- **v0.8 (May 2, 2026):** §7.3 Permanent Exclusions updated with §7.3.1 carve-out acknowledging §1.3.1.1 environment governance for Stage-0 float Tier-A fields (A-7).
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
