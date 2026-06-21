# Living World System Specification #22 — Section 8: Cross-References, Back-Props, Invariant Binding

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.4 — PASS-7 fix pass: XC-022-008 updated to world.arcs/world.text sub-streams (AR7-L1))
**Last Updated (prior):** June 21, 2026 (v0.3 — PASS-5 fix pass: §8.2 fatigue row marked n/a — this layer consumes
the H-Gate (happiness), not fatigue (AR5-L2))
**Last Updated (prior):** June 21, 2026 (v0.2 — PASS-1 fix pass: ERR-022-005 recategorised — the season-calendar
clock is this spec's own forward deliverable (§7.1), not a cross-spec back-prop (L-2); back-props now 001..004)
**Version:** 0.4
**Status:** IN REVIEW (June 21, 2026)

---

## 8.1 Cross-references (XC-022-NNN)

| ID | Target | Nature |
|---|---|---|
| XC-022-001 | vol-2 §1.1 H-Gate | consume happiness state (FR-LW-001) |
| XC-022-002 | vol-2 §2.1 social graph | edge model + clique threshold (FR-LW-004) |
| XC-022-003 | vol-2 §2.2 Pulse Propagation | morale spread consumed as-is (FR-LW-007) |
| XC-022-004 | vol-2 §2.4 ego clash | arc trigger (FR-LW-014) |
| XC-022-005 | vol-2 §7 media & narrative | intent classes + arc routing (FR-LW-013/015) |
| XC-022-006 | vol-3 §4 board/governance | board arc routing (FR-LW-015) |
| XC-022-007 | vol-2 §4.1 / §5.1 supporters | aggregate fan node (FR-LW-015) |
| XC-022-008 | #16 `DeterministicRngService` | dedicated `world.arcs`/`world.text` RNG sub-streams (FR-LW-020) |
| XC-022-009 | #16 snapshot / `SNAPSHOT_SCHEMA_VERSION` | world-state serialisation (FR-LW-022 / §4.6) |
| XC-022-010 | #19 `ScenarioRunner` / `tools/spec-stress` | verification harnesses (FR-LW-030 / §6) |
| XC-022-011 | #18 hot-path/cold-path model | slow-loop posture (§6.0) |
| XC-022-012 | #20 layering / ordinal stability | assembly + enum contracts (FR-LW-002/028) |
| XC-022-013 | Match-engine outcome events | world-loop input (FR-LW-032) |
| XC-022-014 | `MatchClock` (#16) | distinct-clock boundary (KD-4 / §4.2) |

## 8.2 CLAUDE.md invariant binding

| Invariant | Binding |
|---|---|
| Deterministic replay (no `System.Random`, no `DateTime.Now`) | FR-LW-019/020/022 |
| Fatigue `0 = rested, 1 = fatigued` | n/a — this layer consumes the vol-2 **H-Gate (happiness)**, not fatigue; convention listed for completeness, never redefined here |
| No phantom interfaces | FR-LW-031 |
| Constant tags `[GT]/[FIXED]/[DERIVED]/[CROSS]`; no `[EST]` at APPROVED | FR-LW-029 / Appendix A |
| Single-machine snapshot determinism; Fixed64 Stage 5+ | §4.4 |
| 10 Hz / 60 Hz loop separation (this layer is a third, slower loop) | FR-LW-019 / §4.2 |

## 8.3 Back-propagations (ERR-022-NNN) — Stage-1 implementation-time, non-blocking

| ID | Target | Action | Stage |
|---|---|---|---|
| ERR-022-001 | #16 §3.4 | allocate `DOMAIN_TAG_LIVING_WORLD` + a dedicated RNG stream id | first `src/living-world/` commit |
| ERR-022-002 | #16 snapshot schema | add living-world field block + `SNAPSHOT_SCHEMA_VERSION` bump (Appendix B order) | world-store activation |
| ERR-022-003 | #19 §3.1.4 | register `T-LW-*` / `sim_*` prefixes | first test commit |
| ERR-022-004 | vol-2/vol-3 | confirm read-only public surface for the consumed state (no write-back seam) | human-systems implementation |

ERR-022-001..004 are genuine cross-spec back-props; none gate spec approval (parallel to the
#13/#14/#15/#21 deferred-back-prop precedent).

**Forward deliverable (not a back-prop).** The deterministic season-calendar clock + season loop do not
exist today and are **owned by this spec** (§7.1 / §4.2) — they are a Stage-1 implementation deliverable
of #22, not a change requested of another spec, so they are tracked in §7.1, not as an ERR-022 row.
Runtime activation remains gated on KD-10.
