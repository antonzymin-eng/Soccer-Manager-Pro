# Positional Rotations Specification #25 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 2.1 Functional Requirements

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-RO-001 | The only mutation this spec performs is the atomic pairwise exchange of two agents' `SlotIndex` bindings; `FormationSlotRecord` tables, roles, duties, and `PlayerTactic` are never modified. | MUST | KD-2 |
| FR-RO-002 | Eligible pairs come exclusively from the static per-`FormationFamily` adjacency table (Appendix A); a pair absent from the table never rotates. | MUST | KD-1 |
| FR-RO-003 | The goalkeeper never appears in any adjacency row (table invariant test). | MUST | §1.1 |
| FR-RO-004 | Trigger predicate per §3.1: both agents closer to each other's composed target than to their own by ≥ the dial-scaled advantage. The predicate is pure geometry; it is **evaluated** only while phase ∈ {`InPoss`, `TransToAtk`} (outer gate, §3.2 — PASS-1 M-1). | MUST | KD-3 |
| FR-RO-005 | Commit requires the predicate to hold `ROTATION_TRIGGER_DWELL_TICKS` consecutive heartbeats (any miss resets the dwell). | MUST | KD-4 |
| FR-RO-006 | A committed rotation may not revert before `ROTATION_HOLD_TICKS` heartbeats; revert then requires the mirrored predicate to hold `ROTATION_TRIGGER_DWELL_TICKS` consecutive heartbeats. | MUST | KD-4 |
| FR-RO-007 | `ROTATION_HOLD_TICKS ≥` `ShapeAnalyzer`'s line-dwell constant — a `[DERIVED]` lower bound (Appendix D), enforced by a compile-time-adjacent invariant test. | MUST | KD-5 |
| FR-RO-008 | The controller runs after phase classification and before `SlotComposer` and `ShapeAnalyzer` each heartbeat; consumers therefore always see a consistent post-swap binding within a tick. | MUST | KD-2 / KD-5 / §4.2 |
| FR-RO-009 | At most `ROTATION_MAX_PER_TICK` (=1) commit per team per heartbeat; pairs are evaluated in ascending Appendix-A row order (deterministic priority); an agent already in a committed rotation is locked out of other pairs until reverted. | MUST | KD-6 |
| FR-RO-010 | Phase exit (leaving {`InPoss`, `TransToAtk`}) does not force an instant revert; it freezes trigger/revert dwell accumulation (committed rotations persist — snapping players home mid-transition is exactly the chaos this spec exists to avoid). | MUST | §3.4 |
| FR-RO-011 | `RotationFreedom.Off` (zero value) disables all evaluation; a default match is byte-identical to pre-#25. `Conservative`/`Free` scale the trigger via `ROTATION_ADVANTAGE_SCALAR` (§3.2). | MUST | KD-8 |
| FR-RO-012 | `RotationFreedom` is `byte`-backed, APPEND-only, ordinal-stability-tested (`Off=0, Conservative=1, Free=2`). | MUST | #16 precedent |
| FR-RO-013 | Serialized at wiring (one schema bump): the full slot-binding permutation (per-agent `SlotIndex`), per-pair state (§2.2.2), **and the controller's per-agent `LastComposedTarget` cache** (§4.2 — PASS-1 H-1: restore loads it verbatim; a re-seed would break byte-identity). Restore rebuilds through validating seams refusing a non-permutation (F2), incoherent pair state (F6), or non-finite cached targets. | MUST | KD-8 / #16 |
| FR-RO-014 | Routing per the #21 pattern: Phase-D writer solely populates the #12 snapshot's `RotationFreedom` field; `TestOnly_SlotBinding(teamId, agentId)` seam at wiring. | MUST | §4.3 |
| FR-RO-015 | The controller is pure-deterministic; no RNG draw site, no domain tag. | MUST | §1.4 |
| FR-RO-016 | All constants in `PositioningAIConstants`, one tag each; adjacency tables are `[GT]` data with invariant tests (GK-free, index-valid, pair-distinct, ≤ `ROTATION_MAX_PAIRS_PER_FAMILY` rows). | MUST | #20 |
| FR-RO-017 | Every §3 formula has units, ranges, and a worked example. | MUST | CLAUDE.md |
| FR-RO-018 | No phantom interfaces; cyclic rotations and OOP rotations are §7 deferrals with no hooks. | MUST | CLAUDE.md / #20 |

## 2.2 Data structures

### 2.2.1 `RotationFreedom` (new enum, #21-owned after back-prop)

`Off = 0` (identity), `Conservative = 1`, `Free = 2`. Appended to `TeamTactic` per the same
append-order coordination rule as #23/#24 (§2.2.1 of #24).

### 2.2.2 `RotationPairState` (per adjacency-table row, per team, persistent)

| Field | Type | Notes |
|---|---|---|
| TriggerDwellTicks | `int ≥ 0` | consecutive heartbeats the (commit or revert) predicate has held |
| Rotated | `bool` | pair currently swapped |
| HoldTicksRemaining | `int ≥ 0` | countdown before revert becomes evaluable; 0 when idle |

Zero-init is the valid "not rotated" state (KD-8 discipline).

### 2.2.4 `LastComposedTarget` cache (per agent, persistent — PASS-1 H-1)

`Vector2` per roster agent: the composed slot target from the previous #12 tick, written by the
controller's post-compose hook each heartbeat, consumed by the §3.1 predicate, serialized per
FR-RO-013. Boot-seeded from `SeedFromFormation`'s initial compose.

### 2.2.3 Adjacency table row

`readonly struct RotationPair { int SlotA; int SlotB; }` — slot indices into the family's
formation table; Appendix A enumerates rows per `FormationFamily`.

## 2.3 Serialization

At wiring: per agent `SlotIndex` (int32, roster order) + per agent `LastComposedTarget` (float32
X, float32 Y, roster order — PASS-1 H-1) + per pair (`TriggerDwellTicks` int32, `Rotated` byte,
`HoldTicksRemaining` int32, table-row order) + the dial byte via `WriteTeamTactic`. Field order
pinned in Appendix B.

## 2.4 Cross-spec back-props (filed at `APPROVED`)

| Pending ERR | Target | Amendment |
|---|---|---|
| ERR-021-NNN (to file) | #21 §2.2.1 / Appendix B | `TeamTactic.RotationFreedom` field + order row |
| ERR-012-NNN (to file) | #12 §3/§4 | controller position in the tick; the documented invariant that `SlotIndex` is no longer immutable after `SeedFromFormation` (a **text amendment to #12's own contract**, the reason the supplement ranked this spec riskiest) |

## 2.5 Failure modes

| F | Mode | Handling |
|---|---|---|
| F1 | Adjacency row referencing an invalid/GK/duplicate slot index | build-time invariant test failure (FR-RO-016) |
| F2 | Deserialized `SlotIndex` set is not a permutation of the roster's slot set | fail loud at the restore seam (`ArgumentException`) |
| F3 | Both directions of a pair's predicate true in one tick (degenerate geometry) | commit direction is defined by table order + current `Rotated` state; no oscillation within a tick |
| F4 | Non-finite composed-target or agent position in the predicate | predicate evaluates false this tick (dwell resets); never propagate NaN |
| F5 | Dial byte undefined at a routing seam | refuse (fail loud) |
| F6 | Deserialized pair state incoherent: `Rotated` byte ∉ {0,1}; `HoldTicksRemaining > ROTATION_HOLD_TICKS` or > 0 while not `Rotated`; `TriggerDwellTicks` negative (no upper bound — it may legitimately exceed the trigger threshold when the per-tick commit cap defers a commit); or a non-finite `LastComposedTarget` | fail loud at the restore seam (PASS-1 L-2) |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial FR set (18), pair-state model, permutation-restore gate, failure modes. |
| 0.2 | 2026-07-08 | — | PASS-1 fixes: §2.2.4 serialized `LastComposedTarget` cache + FR-RO-013 amendment (H-1); FR-RO-004 outer-gate wording (M-1); F6 pair-state gates (L-2). |
#endregion
