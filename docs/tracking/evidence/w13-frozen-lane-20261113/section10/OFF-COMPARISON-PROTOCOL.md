# Section 10 unarmed C0 comparison

Created 2026-11-13. This independent subset continues while the armed preflight is blocked by the declared post-tick possession hard stop. It does not waive that stop or start armed full matches.

Reference: reviewed 645924057694fd9aa8bf2ab33e43a5810d3d3feb (src 249ce15b33879717a774b1437a1147d28d7918b4). Candidate: frozen source integration of W13 4171d3d1 and that reference, recorded in integration-source.json. Both schemas are v29; compare literal digests, not structurally masked values. Config binding before first catalogue/engine construction sets match-engine.MarkMovementEnabled=false. Both teams C0 F442, otherwise the exact #543 v1.1 setup. 324000 calls, six fixed seeds, one seed per process, at most two captures concurrently. SDK/CLR/JIT/build/environment pins unchanged.

Compare every per-tick raw digest, aligned digest, trajectory, ordered ledger, team-possession witness, geometry, assignment/mark stream and phase witness. All must match. A mismatch, nonfinite value, invalid attribution, invariant failure, nonzero runner exit, truncated stream, or any mark target while disabled stops this subset. Preserve every attempt. No baseline re-adoption, gameplay tuning or source change. Observed mode independently verifies each snapshot preimage and previous link, with BuildHash retained.

These are 12 full C0 captures (six reference/candidate pairs), not an armed measurement or Unity certification. Short probes and independent diagnosis are separate. The armed hard stop remains unresolved; no M1-M8 armed acceptance claim is made.
