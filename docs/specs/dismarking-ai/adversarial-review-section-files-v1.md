# Dismarking & Marker-Awareness AI Specification #23 — Section-Files PASS-1 Adversarial Review

**Created:** July 8, 2026
**Reviewed set:** all 11 section files at v0.1
**Findings:** 0 High / 1 Medium / 3 Low — all resolved in the v0.2 fix pass, same day.
**Method:** formula re-derivation; every code-anchor claim re-verified by grep against `src/`
(stride phase order per `MatchEngine` `RunMechanicsAI`; `FilteredView` build site; sibling-spec
stage-order coordination).

---

## M-1 — §3.2/§4.4 dwell-update ordering claim is impossible at #12's stride position

§3.2 v0.1 claimed dwell updates run "in ascending agent index inside the #12 tick, before
`SlotComposer` consumes pressure that same tick", citing the `RestDefenseEvaluator` same-tick
precedent. But the evaluator's input is `FilteredView`, which is built in the **per-agent
perception/decision pass** — and the stride order is Positioning (#12) → Pressing → Defensive →
Attacking → DecisionTree (per-agent). At #12's position, this stride's `FilteredView`s do not
exist yet. `RestDefense` can be same-tick only because it consumes `PositioningPerceptionSnapshot`,
not `FilteredView` — the analogy does not transfer.

**Resolution (v0.2):** pressure/dwell update is pinned to the per-agent perception pass at stride
N; the #12 offset stage consumes it at stride **N+1** (one-stride latency, documented as
conservative — a marker acquired this stride starts influencing the offset next stride, absorbed
by the dwell ramp anyway). The §3.4 passer-side penalty stays same-pass (fresh view). Restore
determinism is unaffected: the consumed value derives from serialized dwell state + the perception
state #16 already serializes. §3.2, §4.2, §4.4, FR-DM-003, and T-DM-I-003 all updated.

## L-1 — FR-DM-006 wording contradicted §3.2

"freezes decay-only dwell updates" — §3.2 *continues* decay out of phase; what stops is
accumulation. Reworded: "accumulation stops; decay continues".

## L-2 — `LastMarkerId` restore gates missing

F2 gated only `DwellTicks`. Added: `LastMarkerId ∈ {−1} ∪ [0, roster)`, plus the coherence gate
`DwellTicks > 0 ⇒ LastMarkerId ≥ 0` (§3.2 clears the id only at zero dwell). T-DM-U-014 extended.

## L-3 — Missing cross-cite to the #24 combined stage order

#24 §4.2 pins the combined `SlotComposer` stage order for both specs; #23 §4.2 showed its own
insertion without citing the sibling. Cross-cite added both ways per #24's "whichever implements
second cites the first" rule.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-07-08 | — | PASS-1 filed and resolved (0H+1M+3L) — v0.2 fix pass same day. |
#endregion
