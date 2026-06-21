# Living World #22 — Section-File Adversarial Review PASS-7

**Created:** June 21, 2026
**Reviewer:** fresh-eyes pass over `docs/specs/living-world/` section files v0.7
**Result:** 1 M + 1 L (no High). All resolved in the v0.8 fix pass (same day).

---

## Medium

**M-1 — Eviction selection has no deterministic tiebreak.** §3.2 evicts the "lowest-salience" unpinned
episode and §4.5 compacts the "lowest-`NetRelationship`" summary / "lowest-salience `RetainedEpisode`" —
but neither states how **ties** are resolved. FR-LW-021 pins *iteration* order to a stable entity ID, but
**selection/eviction** is a separate operation it does not cover; on a tie the choice falls to container
order, which FR-LW-021 itself calls non-deterministic. Since eviction **mutates persisted state**, a
nondeterministic tie breaks replay/save parity (the same class as F2). **Fix:** add a stable tiebreak —
episodes: lowest salience, ties → oldest `worldTick`, then lowest `episodeId`; cold summaries: lowest
`NetRelationship`, ties → lowest `EntityId`. Extend FR-LW-021's scope to selection/eviction and add a
test.

## Low

**L-1 — XC-022-008 stale post-PASS-6.** The cross-ref still reads "dedicated world RNG **stream**"; PASS-6
split it into `world.arcs`/`world.text` sub-streams. **Fix:** update the cross-ref wording.
