# Board & Ownership Dynamics #45 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 1.1 Purpose

#45 models the **manager↔board relationship** as persistent, evolving state. Before #45, a club's board
exists only as #30's `BoardState` — an objective plus a job-security scalar, evaluated pass/fail at the
season boundary. #45 adds the thing that makes a board feel like a counterparty rather than a verdict: a
**confidence** value that moves day by day with how the season is going, an **ownership profile** that
conditions how demanding and how patient that board is, and — at the deep tier — **takeovers** that can
change who owns the club.

## 1.2 Scope

**In scope**

- A per-club **board-confidence** scalar (integer per-mille), advanced once per world day.
- A per-club **ownership profile**: type plus the dials that condition expectation severity, patience,
  and the budget contribution.
- **Takeover events** (deep tier) — the spec's only stochastic surface.
- The `BoardModifier` projection #40 already specified as #45's product.
- A read-only confidence accessor #30 consumes for job security.

**Out of scope** — each already has an owner; duplicating it is the failure this section prevents:

| Not owned | Owner | #45's relation |
|---|---|---|
| The season **objective** (`FinishAtOrAbove(P)`) and its pass/fail evaluation | #30 (FR-SN-014/015) | reads the "on track?" projection as a committed value |
| The **budget numbers** | #40 (FR-FN-002/005/006) | produces the `BoardModifier` #40 consumes |
| The **sacking decision** | #30 | supplies confidence only (KD-3) |
| The **morale mechanics** | #33 | borrows the *shape*, never the state (KD-1) |
| Club **finances**, the wage ledger, FFP | #40 | untouched |
| Promotion/relegation, the league table | #30 / #43 | reads the committed result via #30 |

## 1.3 Dependencies

**Upstream (consumed):**

- **#16 Deterministic Simulation** — `CanonicalSerializer` for the sub-blob; the reserved namespace slot;
  (deep tier) `DeterministicRngService`.
- **#27 Squad/Player Data** — `ClubId` as the keying identity.
- **#30 Season & Competition Loop** — supplies the committed daily input and invokes the tick step.
  **Reference direction is `#30 → #45`; #45 never references #30.**

**Downstream (consumers):**

- **#40 Club Finances** — consumes the `BoardModifier` at `SettleFinances` (a contract #40 already
  specified, FR-FN-018/019).
- **#30** — reads confidence for job security.
- **#38 UI** — reads a value-copy view model.
- **#33** (deep tier) — receives a board delta as a **routed value**, never via a #45→#33 reference.

**Reference DAG**

```
root → {#30, #45}        #30 → {#28, #40, #45}        #45 → {#27, #16}        #45 → #40
```

**Acyclic**, at every tier. #45's assembly references neither #30, #33, `living-world`, `SeasonSave`,
nor `MatchEngine` — a structural property asserted by reference-absence (§5), not by convention.

## 1.4 Determinism posture

- **World tick only** (`WorldClock`, one day = one `worldTick`). #45 never touches the 10 Hz tactical or
  60 Hz physics loops.
- **All-integer.** Every #45 field and every formula is integer per-mille. **No float appears anywhere in
  #45 at any tier** — and KD-5 removes one from #30's season block as a side effect.
- **Minimal tier is draw-free** ⇒ no RNG stream registered, no domain tag promoted (KD-2).
- **Deep tier draws are keyed and position-independent** ⇒ no cursor is ever persisted.
- Same-day re-advance is a **no-op**; a day **gap** **fails loud** (the #33 F6 guard, adopted verbatim).

## 1.5 Key decisions

### KD-1 — Board confidence is a morale-model analogue, not #33 state

Confidence reuses #33's *shape*: integer per-mille in `[0,1000]`, drift toward a computed target, a
`LastAdvancedWorldDay` idempotency cursor whose unadvanced sentinel is `uint.MaxValue` (**not** `0`), and
the same no-op/fail-loud day guards.

It is **club-scoped, keyed by `ClubId`**, and **#45-owned** — not a `MoraleState`, not a
`PairwiseRelationship`, and not a #33 `PlayerEdge`. #33's FR-HS-002 makes #33 the sole writer of its
per-`PlayerId` state; a club-scoped board relationship is a different entity on a different key, so
housing it in #33 would violate that ownership rather than honour it.

**#45 declares its own drift helper** rather than referencing #33 for a three-operation pure integer
function; equivalence is pinned by test against the formula as specified in #33 §3.1. *Rejected:*
extracting a shared integer-primitives assembly now — two call sites do not justify a new
bottom-of-graph assembly, and the extraction stays available (§7.4 R-3).

### KD-2 — Takeovers are deep-tier; `0x2D`/95 stays RESERVED, not promoted

The minimal tier is a deterministic projection with no stochastic draw, so #45 registers **no** stream
and promotes **no** domain tag at approval (the `_RESERVED_0x29_` #40 / `_RESERVED_0x21_` #29 / #33
FR-HS-013 precedent). It promotes to `DOMAIN_TAG_BOARD_OWNERSHIP = 0x2D` at the first takeover draw.

When it does: **one** subsystem-wide stream (siteId `board.takeover`, a fixed entity sentinel) registered
once regardless of club count, with the club folded into a **position-independent keyed action ordinal**
over `(clubId, worldDay, purpose)` at a **fixed** radix — the #41/#42/#28 idiom — so there is no
free-running cursor to persist.

This matters beyond #45: `RegisterStream` appends into a bounded, never-shrinking table
(`MaxRngStreams` = 64, no unregister), and #42 §7.4 R-1 records that a per-club registration model would
exhaust it across a full-world career. **#45's model does not contribute to that bound at any tier.**

A takeover mutates **#45-owned state only**. The budget effect propagates one-directionally, because the
`BoardModifier` #45 projects is read by #40 at the next boundary — there is no takeover→budget write path
and therefore no coupling knot.

### KD-3 — #45 supplies confidence; #30 decides the sacking

Strictly one-directional. #45 exposes a read-only confidence accessor, **no** sacking API, and fires no
event that terminates a manager. #30 applies its own threshold at its own cadence. Asserted structurally.

### KD-4 — Ownership types are dials on one code path

One `OwnershipProfile` value type carrying per-mille dials. Minimal ships a single generic profile whose
dials are exactly identity; deep-tier types are **values**, not new code paths.

`OwnershipProfile.Identity` is an **explicit factory** (all dials `1000`); `default(OwnershipProfile)` —
all-zero, i.e. ×0 — is **not** a valid runtime value and **fails loud** at the consuming seam. This
applies #40's `BoardModifier` F4 and #41's `MedicalModifier` lesson up front rather than discovering it
in a later review round.

### KD-5 — Reconciliation with #30: split the target from the relationship

| Concern | Owner |
|---|---|
| `BoardObjective` — the target — and its season-boundary pass/fail | **#30**, unchanged |
| The running "on track?" projection (FR-SN-015 already mandates it) | **#30** — #45's daily input |
| Persistent board **confidence** | **#45** — the new state |
| `BoardState.JobSecurity` | **#30 field → derived band** over #45's confidence, at #45 T2 |

#30 routes the projection in as a **committed value**, exactly as it routes `HumanSystemsDayInput` into
#33, so #45 references neither #30 nor the league table.

**Why `JobSecurity` must change.** Once #45 owns a persistent confidence scalar, an independent #30
job-security scalar is a **second truth for the same quantity** — they would diverge at the first restore
with nothing to detect it. Making it a derived band eliminates the divergence by construction, and
removes the layer's last float: `JobSecurity` is typed *"float/enum"* while #33, #40, #41, #28 and #42
are integer-only by requirement, and it sits inside a round-trip-deterministic save block.

**Consequence, stated not buried:** `JobSecurity` is serialized by #30's `WriteBoard`, so changing its
representation is a **`SEASON_STATE_FORMAT_VERSION` bump** — #30's own version, distinct from both
`SEASON_SAVE_FORMAT_VERSION` and #45's `BOARD_SAVE_FORMAT_VERSION`. Three independent versions, none
implying the others. This is the one place #45's landing is not purely additive.

**#45 does not define the projection's semantics** — including degenerate cases such as an all-square
table before any fixture is played. That is #30's (FR-SN-015); #45 consumes the committed integer.

### KD-6 — Persistence: an opaque, independently version-gated sub-blob

`BOARD_SAVE_FORMAT_VERSION` [FIXED] = 1, composed into #30's `SeasonSaveCodec` — **not** a
`WORLD_STORE_FORMAT_VERSION` bump. The outer codec never parses it. Version gate read first;
overflow-safe length prefixes; trailing-byte guard; fail loud on all three. **Serialize, don't
regenerate.** Deliberately absent: any RNG cursor (KD-2), and any copy of #30's objective or #40's
budget — mirroring either would re-introduce the double truth KD-5 removes.

### KD-7 — Cadence: world tick, at #30 slot 8

Confidence advances once per world day at a new #30 tick-order slot **8** (after #42's academy at 7),
pushing `WorldStore.AdvanceDay()` to 9 (ERR-030-008). Like #42's and unlike the #31/#34 deep-tier
position reservations, this seam **goes live at #45's own T2**.

**Pinned one-day-stale board→morale contract.** #33 occupies slot 3 and #45 slot 8, so when #45 becomes
the deep-tier producer of `HumanSystemsDayInput.BoardObjectiveDeltaPermille`, #33 reads the value #45
committed on the **previous** day. Moving #45 ahead of #33 was rejected: slots 1–7 are pre-declared
positions cited **by number** in six approved specs (#28/#29/#33/#41/#31/#34/#42), and renumbering them
to spare a one-day lag would force a re-pin across all of them — precisely the cost FR-SN-034's fixed
order exists to avoid. A board-confidence nudge is a slow-moving signal where one day is semantically
invisible, and the project already carries this contract in #23's one-stride-stale dismark carriers. The
lag is **pinned and tested**, not implicit — an unstated lag is what a later maintainer "fixes" by
reordering slots, silently breaking six specs' cited positions.

### KD-8 — Behaviour-neutral identity

With the generic profile and identity dials: `BoardModifier` = `Identity` ⇒ #40's `SettleFinances` yields
exactly `budget = f(finalTablePosition, prizeMoney)`, unchanged from pre-#45; no stream registered ⇒
every existing stream's cursor byte-identical; and a season advanced with the #30 board seam null is
byte-identical to the same season pre-#45 (the FR-SN-026 world-floor property). The deep tier **extends**
this identity; it never rewrites it.

## 1.6 Lessons folded in up front

Rather than leaving them to be found in review, three known traps are handled by construction:

1. **The zero-value trap** (#40 `BoardModifier`, #41 `MedicalModifier`) — `OwnershipProfile.Identity` is
   an explicit factory and `default(...)` fails loud (KD-4).
2. **The unadvanced-cursor sentinel** (#33 FR-HS-008) — `uint.MaxValue`, not `0`, because day `0` is a
   legal world day and a `0` sentinel silently no-ops a day-0 advance instead of failing loud.
3. **The absent-entry contract** (#40 FR-FN-025) — "club not modelled by #45" is a *named legal state*
   with an explicit `Try…` seam, not an error and not a silent default (§2 FR-BD-018).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial §1 (scope, out-of-scope table, dependencies + acyclic DAG, determinism posture, KD-1..KD-8, the §1.6 folded-in lessons) from supplement v0.3. Status IN REVIEW. |
#endregion
