# Living World System Specification #22 — Section 6: Performance & Verification Harnesses

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW (June 21, 2026)

---

## 6.0 Performance posture

This layer is **entirely slow-loop / cold-path** (#18): it runs on the season-calendar clock (KD-4),
never inside the 10 Hz tactical or 60 Hz physics loops. It therefore carries **no per-frame hot-path
budget**. The only bounded-cost requirements are: (a) the per-`worldTick` deep-tier update over the
**active set only** (FR-LW-023) — O(active edges + live arcs), not O(world); (b) the background tier's
bounded per-tick cost (FR-LW-024); (c) the `[GT]` save-size budget capping total live state (§4.5). A
`worldTick` is one calendar day, so even a generous per-tick budget is imperceptible against match-loop
costs.

## 6.1 Invariant / property fuzzing (`T-LW-EXP-001`)

Random world seeds drive the loop; after every tick assert the never-violated rules: all edge/layer
values ∈ [0,1] (F6); no dangling `episodeId` (F1); no orphan or unresolvable arc; total live state
(live edges + live episodes + cold summaries) within the `[GT]` budget (F4); a cold-summary rehydration
reproduces a valid edge (F5).

## 6.2 Long-horizon soak (`T-LW-EXP-002`)

Run N seasons headless; assert no deadlock and no runaway/monotonic state drift. Liveness is **per
instance** (a finite random soak cannot prove a global "every arc resolves"): no arc instance stays
unresolved beyond its `[GT]` `maxLifetime`.

## 6.3 Coverage / gap detection (`T-LW-EXP-003`)

Track which `InteractionIntent` / `ArcKind` actually fire across a large seeded corpus; **unreached
content is the "gap,"** surfaced automatically rather than found in play. Content that is *intentionally*
rare (e.g. takeovers) carries an **expected-rarity annotation** so the harness flags genuine
unreachability, not designed scarcity.

## 6.4 Determinism replay (`T-LW-EXP-004`)

Same seed + snapshot-restore (deep tier *and* background tier) ⇒ bit-identical world state on the pinned
host — the off-pitch analogue of the match determinism gate (single-machine snapshot determinism;
cross-platform parity stays Stage 5+).

## 6.5 CI integration

These harnesses are **intended to run** in the non-certifying CI gate (`tools/dotnet-ci/`) alongside the
existing suites once the system is implemented; any invariant breach or determinism mismatch would fail
the build. They verify **structure** (no gaps/deadlocks/non-determinism), **not quality** —
tone/believability is covered by the §7 inspector + human review.
