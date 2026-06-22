# Living World #22 — Section-File Adversarial Review PASS-4

**Created:** June 21, 2026
**Reviewer:** fresh-eyes pass over `docs/specs/living-world/` section files v0.4
**Result:** 1 M + 3 L (no High). All resolved in the v0.5 fix pass (same day). Diminishing returns —
recommend this be the terminal section-file pass before lead-developer sign-off (G3).

---

## Medium

**M-1 — PASS-3's read-only-`PlayerEdge` contract has no test.** AR3-M1 made `PlayerEdge` a read-only
mirror (FR-LW-004 strengthened), but no test asserts the §3.1 update leaves it unmutated; FR-LW-004
traces only to T-LW-U-005..010, which exercise the *owned*-layer formula and never check `PlayerEdge`.
An untested MUST is exactly the gap this project's "structurally dead suite" history warns against.
**Fix:** add **T-LW-U-035** asserting a §3.1 update on a player↔player edge leaves `PlayerEdge`
unchanged; update counts (unit 35, total ≥74) + traceability.

## Low

**L-1 — FR-LW-027 no-write-back scope omits vol-2 §2.1.** It forbids write-back to "the H-Gate or vol-2
§2.2 propagation math" but not the §2.1 social-graph edge — the very thing AR3-M1 pinned read-only. **Fix:**
add §2.1 (`PlayerEdge`) to FR-LW-027's no-write-back list.

**L-2 — KD-3 / KD-9 wording trails the read-only clarification.** §1.5 KD-3 says "never a re-scaling"
and KD-9 lists only H-Gate/propagation; neither states `PlayerEdge` is read-only/never mutated. **Fix:**
align KD-3 (read-only mirror) and KD-9 (include §2.1).

**L-3 — §3.2 worked example depth vs. constant.** The example uses "Buffer depth 8" while
`MEMORY_BUFFER_DEPTH = 12` (Appendix A). **Fix:** mark the example depth illustrative.
