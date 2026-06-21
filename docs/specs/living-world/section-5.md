# Living World System Specification #22 — Section 5: Test Plan

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.2 — PASS-1 fix pass: added T-LW-DET-007 for FR-LW-034 additive-only
identity (M-1); determinism count 6→7, total ≥73; traceability row for FR-LW-034)
**Version:** 0.2
**Status:** IN REVIEW (June 21, 2026)

> Test-ID prefixes follow #19 §3.1.4: `T-LW-U-*` unit, `T-LW-I-*` integration, `sim_*` / `T-LW-SIM-*`
> simulation (closed-loop on the #19 `ScenarioRunner`), `T-LW-DET-*` determinism, `T-LW-FAIL-*`
> failure-mode, `T-LW-EXP-*` exploit/robustness/stress.

---

## 5.1 Test counts (target)

| Layer | Count (≥) | Notes |
|---|---|---|
| Unit | 34 | enum ordinals, edge update/decay, memory eviction/pinning, intent→text determinism, arc lifecycle |
| Integration | 14 | each consume-as-is canon seam + arc routing + cold-store round-trip |
| Simulation (closed-loop) | 6 | one per construct, via #19 ScenarioRunner |
| Determinism | 7 | RNG stream, iteration order, text reproducibility, replay, additive-only identity |
| Failure-mode | 6 | F1–F6 |
| Exploit / stress | 6 | §6 harness classes (fuzz, soak, coverage, replay) + budget overflow + tier-transition |
| **Total** | **≥ 73** | |

## 5.2 Unit tests (`T-LW-U-*`)

- **T-LW-U-001..004** — `EnumOrdinalStability`: `EventKind`, `ArcKind`, `InteractionIntent`,
  `RelationshipLayer` assert `(int)Member == N` (APPEND-only; FR-LW-028).
- **T-LW-U-005..010** — edge update (§3.1): asymptotic toward [0,1], never overshoots; decay toward
  baseline; the §3.1 worked example reproduces `0.56`; `Trust` directional (A→B ≠ B→A) (FR-LW-004/005).
- **T-LW-U-011..018** — memory (§3.2): `episodeId` monotonic per edge; full-buffer evicts lowest-salience
  unpinned; pinned-skip; salience decay; ref-threshold eligibility (FR-LW-008/009/010).
- **T-LW-U-019..026** — procedural text (§3.3): same `(intent, cursor, slots)` ⇒ identical string;
  slot facts limited to emitted data; no model call on any path (FR-LW-011/012/013).
- **T-LW-U-027..034** — arc lifecycle (§3.4): spawn on threshold; `SpawnCause` captured; episodes pinned;
  resolve/escalate; `maxLifetime` enforced; `ArcKind`-ordinal evaluation (FR-LW-014/016/017/018).

## 5.3 Integration tests (`T-LW-I-*`)

- **T-LW-I-001..006** — consume-as-is seams: H-Gate, social graph, pulse propagation, media, board,
  supporters each read correctly and drive an interaction without write-back (FR-LW-001/007/027).
- **T-LW-I-007..010** — arc routing: `BoardPatienceCollapse` reads vol-3 §4.1 archetype and routes to
  sack/backing; `MediaVendetta` cites a §3.2 episode (FR-LW-015).
- **T-LW-I-011..014** — LOD: demotion→`ColdSummary`; rehydration round-trip equality; background tier
  bounded + deterministic (FR-LW-024/025).

## 5.4 Determinism tests (`T-LW-DET-*`)

- **T-LW-DET-001** — same `(world seed, calendar span, event log)` ⇒ identical world-state digest.
- **T-LW-DET-002** — graph iteration order is entity-ID/`ArcKind`-canonical: a shuffled internal
  container yields an identical digest (FR-LW-021/017).
- **T-LW-DET-003** — text reproducibility: identical intent+cursor+slots ⇒ identical string (FR-LW-011).
- **T-LW-DET-004** — no RNG draw site outside the dedicated world stream (FR-LW-020).
- **T-LW-DET-005** — snapshot/restore (deep + background tier) ⇒ bit-identical continuation on the
  pinned host (single-machine; FR-LW-022).
- **T-LW-DET-006** — `worldTick` advances only on the calendar clock, never the match loops (FR-LW-019).
- **T-LW-DET-007** — **additive-only identity:** an empty world (no episodes, no arcs) yields a
  canonical-behaviour digest bit-identical to the human-systems baseline with this layer disabled; an
  all-inactive-layer edge contributes nothing to any outcome (FR-LW-034).

## 5.5 Simulation / closed-loop (`T-LW-SIM-*`, via #19 ScenarioRunner)

One scenario per construct once the world store + season loop compose: `sim_memory_breaks_repetition`
(two identical triggers a month apart produce distinct cited episodes), `sim_text_varies_same_intent`,
`sim_arc_dressing_room_split`, `sim_arc_board_patience_collapse` (Tycoon archetype), `sim_cold_store_
journalist_remembers` (rehydration after a club change), `sim_background_tier_deterministic`. Each
asserts an envelope predicate on the realised world state, not exact strings.

## 5.6 Exploit / stress (`T-LW-EXP-*`) — see §6

The four §6 harness classes are realised here: **T-LW-EXP-001** invariant/property fuzz; **-002**
long-horizon soak (per-instance `maxLifetime` liveness); **-003** coverage/gap (unreached
`InteractionIntent`/`ArcKind`, expected-rarity-annotated); **-004** determinism replay; **-005**
save-budget overflow eviction; **-006** tier-transition round-trip.

## 5.7 FR traceability

| FR | Verified by |
|---|---|
| FR-LW-001/007/027 | T-LW-I-001..006, T-LW-DET (no write-back) |
| FR-LW-002/003 | asmdef-grep / inspection (structural) |
| FR-LW-004/005/006 | T-LW-U-005..010 |
| FR-LW-008/009/010 | T-LW-U-011..018, T-LW-FAIL-001/004 |
| FR-LW-011/012/013 | T-LW-U-019..026, T-LW-DET-003, T-LW-FAIL-003 |
| FR-LW-014/016/017/018 | T-LW-U-027..034, T-LW-FAIL-001 |
| FR-LW-015 | T-LW-I-007..010 |
| FR-LW-019/020/021/022 | T-LW-DET-001..006, T-LW-FAIL-002 |
| FR-LW-023/024/025 | T-LW-I-011..014, T-LW-FAIL-005 |
| FR-LW-026 | T-LW-FAIL-004, T-LW-EXP-005 |
| FR-LW-028 | T-LW-U-001..004 |
| FR-LW-029 | Appendix A / catalogue inspection |
| FR-LW-030 | T-LW-EXP-001..004 |
| FR-LW-031 | inspection (no phantom interface) |
| FR-LW-032 | §7 gating (named verification) |
| FR-LW-033 | §3 worked examples present |
| FR-LW-034 | T-LW-DET-007 |
