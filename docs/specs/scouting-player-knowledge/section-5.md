# Scouting & Player Knowledge #32 — Section 5: Test Plan

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.3 — cross-set AR; prior v0.2 — section-file AR PASS-1; prior v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

## 5.1 The view-not-mutation lock (KD-2) — the headline

- **T-SC-VIEW-001** — exercising **every** #32 path (view reads at every band, `AssignScout` /
  `CancelAssignment` / `AdvanceScoutingDay` lifecycle, band-ups, report stamps, `RankByEstimate`)
  leaves the #27 canonical squads **byte-identical** (the roadmap §5 invariant test; compare a full
  `ToArray()` snapshot of every squad record before/after).
- **T-SC-VIEW-002** — #32 exposes no API taking `ref Squad`/`ref PlayerRecord`; `EstimateFor` takes
  `in PlayerRecord` (static/reflection assertion — FR-SC-001).

## 5.2 Behaviour-neutral identity (KD-8)

- **T-SC-NEU-001** — a fog-off season advances **byte-identical** to pre-#32: the overlay stays
  empty, the #30 slot is a no-op, **no** RNG stream is registered and every existing cursor is
  byte-identical (the #40 `T-FN-NEU-003` class), and the omniscient view equals truth per-attribute
  for every player (`Min == Max == truth`, all 31 via `AttrIdx` — FR-SC-007).
- **T-SC-NEU-002** — `ResolveBand` with fog off returns `KNOWLEDGE_BAND_MAX` for any id; no **view**
  consumer branches on `fogEnabled`, and the dial acts in exactly the three FR-SC-002-named places
  (`ResolveBand`, the `AssignScout` gate, the `AdvanceScoutingDay` no-op) — a static/reflection
  sweep asserts no fourth site exists.

## 5.3 Estimate invariants (KD-1/KD-3)

- **T-SC-EST-001 (containment)** — for every band and a sweep of truth values `1..20`: `Min ≤ Max`,
  both `∈ [1,20]`, **truth `∈ [Min, Max]`** (FR-SC-003), including at the clamp boundaries
  (truth = 1 and truth = 20 with maximal width).
- **T-SC-EST-002 (collapse)** — `HALFWIDTH[]` strictly decreasing, terminal `0`; a `BAND_MAX` read
  returns `[truth, truth]` exactly (FR-SC-004/005).
- **T-SC-EST-003 (stability)** — same `(playerId, band)` ⇒ bit-identical estimate across repeated
  views, across days, across save→restore, and across call orders; the estimate changes **only** on
  a band advance (FR-SC-011 — the not-keyed-on-`worldDay` lock).
- **T-SC-EST-004 (draw-free identity)** — a `w == 0` read makes **no RNG call** (FR-SC-012 —
  instrumented rng seam asserts zero invocations at `BAND_MAX` and throughout the minimal tier).
- **T-SC-EST-005 (live-form window)** — mutate a truth attribute (the #28 growth analogue) at a
  fixed band: the window re-centres on current truth with the **same** offset (FR-SC-010 — the
  pinned freshness semantic, asserted so it cannot silently change).
- **T-SC-EST-006 (ordinal bijection)** — `DeriveScoutOrdinal` is injective over the radix bounds;
  out-of-bound band/attrIdx/purpose fails loud (§3.3).

## 5.4 Own-squad omniscience & roster hygiene (KD-2/KD-6, deep)

- **T-SC-OWN-001** — a managed-club player resolves `[truth, truth]` at any overlay state
  (FR-SC-009); `WeakFootRating` and identity facts are exact at every band (FR-SC-008).
- **T-SC-OWN-002 (F2)** — `AssignScout` on an own-squad id fails loud.
- **T-SC-HYG-001** — a roster re-key/retirement event drops the affected overlay entry (buy → the
  own-squad rule takes over; sell → knowledge reset); a view query for an unresolvable `PlayerId`
  fails loud (F1 — FR-SC-019).
- **T-SC-HYG-002 (assignment cancellation)** — a re-key/retirement of the **active assignment's
  target** cancels the assignment (in-band progress discarded, completed bands' entry dropped with
  the overlay drop); the manager-buys-the-scouted-player sequence leaves #32 state coherent and
  round-trippable — no dangling id anywhere (FR-SC-019).

## 5.5 Assignment lifecycle & quality scaling (KD-4/KD-7, deep)

- **T-SC-ASN-001** — band-up cadence follows `DaysPerBand(quality)`: neutral scout (`1000`) = base
  cadence; `1250` ⇒ 11 days (the §3.4 worked example); `700` ⇒ 20; floor-clamp ≥ 1 (FR-SC-023).
- **T-SC-ASN-002 (F5)** — `quality ≤ 0` at `AdvanceScoutingDay` fails loud.
- **T-SC-ASN-003** — an assignment completes at `BAND_MAX` and clears; re-assigning a fully-scouted
  player fails loud (F2); `CancelAssignment` discards `DaysIntoBand`, keeps completed bands, and
  fails loud with no active assignment (FR-SC-021).
- **T-SC-ASN-004 (F1/F2)** — `AssignScout` on an unknown id / busy slot / **with fog off** fails
  loud; the #30 slot is a no-op at minimal (FR-SC-022) and with no active assignment.
- **T-SC-ASN-006 (inert on fog-off load)** — a deep save with an active assignment loaded into a
  fog-off config neither advances nor drops it (`AdvanceScoutingDay` no-op); re-enabling fog resumes
  the assignment exactly where it stood (FR-SC-022).
- **T-SC-ASN-005 (speed-only)** — changing scout quality mid-assignment alters future cadence but
  **no** already-derived estimate (KD-4 — the retroactivity lock).

## 5.6 Save round-trip & determinism (KD-6)

- **T-SC-DET-001** — `ScoutingState` restores **field-identical** across a save: empty at minimal;
  populated bands + a mid-assignment `DaysIntoBand` cursor at deep; entries decode in strict
  ascending `PlayerId` order (FR-SC-017/018).
- **T-SC-DET-002** — overlay entries survive `RollToNextSeason` (durable career state, FR-SC-018).
- **T-SC-DET-003 (deep)** — two-run determinism: a full season of assignments/band-ups/reports from
  a fixed world seed produces a **byte-identical** `ScoutingState` and identical derived estimates.
- **T-SC-SHAPE-001** — the serialized block contains **no** `RngCursor`/`actionOrdinal` field
  (schema-shape assertion, FR-SC-014).
- **T-SC-INT-001** — every attribute/band/width/estimate/quality field is integer; #32 introduces
  **no** float (static/reflection assertion, FR-SC-027).

## 5.7 Fail-loud (F1..F6)

- **T-SC-FAIL-001 (F3)** — bad `SCOUTING_SAVE_FORMAT_VERSION` / out-of-bounds length prefix (the
  overflow-safe `total − offset` `Require`) / trailing bytes all throw at decode.
- **T-SC-FAIL-002 (F4)** — a decoded entry with `KnowledgeBand ∉ [0, BAND_MAX]`, non-ascending or
  duplicate `PlayerId`s, or an assignment referencing an absent player throws.
- **T-SC-FAIL-003 (F6)** — a `default(AttributeEstimate)` (`{0,0} ∉ [1,20]`) reaching a consuming
  seam throws; `KnowledgeBand = 0` alone does **not** (a legitimate unscouted band).
- **T-SC-FAIL-004 (F1)** — a knowledge query for a player absent from the resolvable pool throws.

## 5.8 Requirement traceability

Every FR-SC-001..027 maps to a T-SC-* test above **or** a recorded §7 deferral. Deep-tier-only
requirements (FR-SC-011/020/021/022/023/026) are locked at their minimal identity boundary now (the
fog-off equality — omniscient view, zero draws, empty slot) and fully at the deep T-phase.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §5 (view-not-mutation, behaviour-neutral identity, estimate invariants, own-squad/hygiene, assignment lifecycle, save/determinism, fail-loud, traceability), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M-1): T-SC-ASN-004 gains the fog-off refusal; new T-SC-ASN-006 locks the inert-on-fog-off-load semantics. |
| 0.3 | 2026-07-24 | — | Cross-set AR: T-SC-NEU-002 re-scoped to the three-site dial claim (+ a no-fourth-site sweep); new **T-SC-HYG-002** locks the assignment cancellation on target re-key/retirement (M-1). |
#endregion
