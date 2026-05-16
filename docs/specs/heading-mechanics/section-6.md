# Heading Mechanics Specification #10 — Section 6: Performance Analysis & Budgets

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Per-tick and per-frame performance budgets, hot-path
allocation discipline, scaling analysis, and Stage 0 → Stage 1
migration notes. Budgets here are ratified (not overridden) by
Performance Optimization #18 §6 per #18 KD-2.

---

## 6.1 Per-Tick Budget

Heading Mechanics #10 executes on the 60 Hz physics loop. Budget is
split into two reconciled rows (pass-1 H-4 fix — §6.1 ceilings now
match §6.3 component sum exactly, no implicit overrun).

| Scenario                               | Budget          | Tag    |
|----------------------------------------|-----------------|--------|
| Steady-state per-tick (no duel frame)  | ≤80 µs          | `[EST]` |
| p99 duel-frame tail                    | ≤180 µs         | `[EST]` |
| Hot-path heap allocations              | 0 bytes / tick  | `[FIXED]` |

The steady-state ceiling applies at 22-agent match peak under
non-duel-frame load. The p99 duel-frame tail budget applies to
worst-case duel frames (3-way duel + near-tie tiebreak
perturbation). The 80 µs steady-state ceiling does NOT bind at
duel frames; the p99 budget binds instead. Both numbers fold back
into §6.3 component-cost breakdown for traceability.

Both `[EST]` budgets are not credible (i.e., not promotable to
`[GT]` / `[FIXED]`) until `docs/tracking/certification-platform.md`
Stage-0 host pin lands. Per CLAUDE.md OPEN ISSUES, that pin is a
lead-developer task and is the precondition for activating
`FR-PO-052` Stage 0+1 perf-gate enforcement. #10 sign-off does NOT
depend on this pin landing.

---

## 6.2 Hot-Path Allocation Discipline

Per #18 §3.10 0-bytes-per-tick `[FIXED]` budget (KD-11 of #10
ratifies; does not override):

- **No `new` in formula files.** `HeadingContactQuality.cs`,
  `HeadingPowerAngle.cs`, `HeadingSpinTransfer.cs`,
  `HeadingDuelResolution.cs`, `HeadingEligibility.cs`,
  `HeadingJumpKinematics.cs` contain zero `new` keywords on hot
  paths.
- **`ReadOnlySpan<>` for contact-event consumption.** The
  Collision System #3 contact-event list is consumed via
  `ReadOnlySpan<ContactEvent>` — no copy, no array allocation.
- **Struct return types throughout.** `HeaderIntent`,
  `HeaderContactState`, `ContestedDuelContext` are structs
  (CLAUDE.md "struct-based, zero-allocation").
- **Event publishing is pooled.** `HeaderExecutedEvent` and
  `HeaderAttemptFailedEvent` instances are drawn from a per-frame
  pool managed by Event System #17; #10 does not allocate the
  event-object payload itself.
- **No `HotPathAllocExempt` attribute uses required.** Struct-based
  data flow eliminates the need (per outline-detailed §4.5).

Cite: Performance Optimization #18 §3.10 channel registry; §3.7.5
`[HotPathAllocExempt]` ownership declaration site.

---

## 6.3 Scaling Analysis

Component-cost decomposition is the source of truth for the §6.1
budget rows.

### 6.3.1 Per-Frame Workload at 22-Agent Match Peak

| Component                          | Frequency       | Per-call cost (`[EST]`) | Per-frame cost (`[EST]`) |
|------------------------------------|-----------------|-------------------------|--------------------------|
| Eligibility predicate (§3.2)[^elig-bound] | ≤22 / frame | ≤2 µs                   | ≤44 µs                   |
| Jump-kinematics step (§3.3)        | ≤2 / frame      | ≤3 µs                   | ≤6 µs                    |
| Contact-quality scalar (§3.4)      | ≤2 / frame      | ≤4 µs                   | ≤8 µs                    |
| Power & launch-angle (§3.5)        | ≤2 / frame      | ≤3 µs                   | ≤6 µs                    |
| Spin transfer (§3.6)               | ≤2 / frame      | ≤3 µs                   | ≤6 µs                    |
| Own-goal-shape flag (§3.8)         | ≤2 / frame      | ≤4 µs                   | ≤8 µs                    |
| **Steady-state total**             |                 |                         | **≈78 µs (budget ≤80)**  |

[^elig-bound]: v0.2 L-5 clarification. The ≤22/frame cap is the
upper-bound pessimism for budget framing — the §4.6 pseudocode
gates the predicate on "agent has a HeaderIntent latched and
aerial-phase active", which is typically 0–2 agents per frame
during open play. The 22-cap binds the worst-case set-piece frame
(corner / wide free kick with full box presence) and is therefore
not the steady-state expectation; ≤44 µs is the worst-case
budget envelope, not a per-frame average.

### 6.3.2 p99 Duel-Frame Decomposition

| Component                                   | Per-frame cost (`[EST]`) |
|---------------------------------------------|--------------------------|
| Steady-state baseline (per §6.3.1)          | ≈78 µs                   |
| Duel resolution §3.7 (3-way)                | ≤50 µs                   |
| Near-tie tiebreak perturbation (§3.7 step 3) | ≤30 µs                   |
| Additional Gaussian noise draws (§3.4)      | ≤10 µs                   |
| Disturbance-factor application to losers    | ≤10 µs                   |
| **p99 duel-frame total**                    | **≈178 µs (budget ≤180)** |

### 6.3.3 Contested-Duel Frequency

Estimated ≤0.5 contested duels per match minute (≈45 duels per
90-minute match; ≈10 % of the ~28 headers per full match are
contested, per Opta / StatsBomb baselines). Pass-1 M-3 recalibrated
this estimate down from a pre-fix value of ≤3/min that was ~6× too
high. Source: §8.3 Kirkendall & Garrett 2001 + modern Opta /
StatsBomb match-level statistics.

The combination of low frequency and per-event budget gives an
amortised per-match-minute cost well under the steady-state
ceiling; duel frames are the p99 spike rows, not the steady-state
driver.

---

## 6.4 Profiling Compliance (KD-6 of #18)

Determinism-aware profiling hooks are placed at:

- §3.7 entry — duel-resolution entry trace (`heading.duel.entry`).
- §4.3 emission — `HeaderExecutedEvent` and
  `HeaderAttemptFailedEvent` publish trace.
- §3.4 contact-quality computation completion
  (`heading.contact.quality.computed`).

All trace channels are declared in §2.4 and allocated against
Performance Optimization #18 §3.10 channel registry via back-prop
(OI-002 in the open-items tracker; pending allocation when
`section-2.md` §2.4 lands in #18 §3.10).

Trace emission obeys the #18 determinism-of-emission veto: any
trace point added to the 60 Hz tick pipeline must not perturb
deterministic-replay output (per #18 KD-3 boundary with #16). The
hooks listed above emit timing telemetry only; they do NOT influence
the physics formula path.

---

## 6.5 Stage 0 → Stage 1 Performance Migration Notes

Stage 0 uses `float` arithmetic and achieves single-machine
determinism via state snapshots, not bit-exact cross-platform
parity (per CLAUDE.md "When Writing Code"; Fixed64 Math #9 §8.1
v0.2). The Fixed64 binding is a Stage 5+ concern when
cross-platform multiplayer becomes a requirement.

When Fixed64 migration arrives at Stage 5+:

- The §3.x formulas re-verify against Fixed64 arithmetic per #9
  §9.3 / §9.6 golden-vector corpus.
- The per-tick budget may shift; Fixed64 arithmetic is typically
  1.5–3× slower than `float` on commodity hardware. Budgets
  recompute at that time.
- The `[FIXED]` 0-bytes-per-tick budget is invariant under the
  migration.

No migration work is required for #10 at Stage 0.

---

## 6.6 Version History

| Version | Date         | Author  | Notes                                                  | Reviewer |
|---------|--------------|---------|--------------------------------------------------------|----------|
| 0.1     | May 16, 2026 | drafter | Initial section draft from outline-detailed v1.1       | pending  |
| 0.2     | May 16, 2026 | drafter | v0.2 PASS-1 fix pass: §6.3.1 eligibility-predicate frequency upper-bound clarified via footnote (L-5).                                          | pending  |
