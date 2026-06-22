# Living World #22 — Section-File Adversarial Review PASS-10

**Created:** June 21, 2026
**Reviewer:** fresh-eyes pass over `docs/specs/living-world/` section files v0.10
**Result:** 2 M + 1 L (no High). All resolved in the v0.11 fix pass (same day).

> Note: 2 M (up from 1) — the cold-store/membership machinery added in PASS-8/9 opened two new
> correctness edges around the demote→rehydrate cycle. Recent fixes created new surface, as in PASS-2.

---

## Medium

**M-1 — `episodeId` monotonicity does not survive cold-store.** FR-LW-009 requires `episodeId` to be
"monotonic within the edge" and durable (it's what an arc pins / a save references). But `ColdSummary`
stores only `RetainedEpisodes` (top-N), **not the per-edge `nextId` high-water mark.** On rehydration the
counter can only be re-derived as `max(retained)+1`, which is **wrong** — episodes with *higher* ids were
dropped during compression, so new episodes would **reuse** ids that previously existed, breaking
monotonicity and risking collision with a stale reference. **Fix:** persist the per-edge
`NextEpisodeId` high-water mark in `ColdSummary`; rehydration resumes the counter from it.

**M-2 — Contact demotion can orphan a live arc's pinned episodes.** FR-LW-018 / F1 guarantee arc-pinned
episodes survive *buffer eviction* — but **demotion to cold-store is a different drop path** it does not
cover. Top-N-by-salience retention (§3.5) does not guarantee an arc-pinned episode is kept, so demoting a
contact that a live arc references (e.g. an LRU-demoted journalist mid-`MediaVendetta`) would dangle the
arc. **Fix:** exclude from LRU demotion any contact with a live arc-pinned episode until the arc resolves
(parallel to the eviction exemption); extend FR-LW-018 to the demotion path.

## Low

**L-1 — Round-trip-equality claims overstate (lossy by design).** F5 ("round-trip equality check") and
`T-LW-I-011..014` ("rehydration round-trip equality") read as *full* equality, but demotion retains only
`COLD_SUMMARY_RETAINED_EPISODES`; §3.5 correctly scopes it to "retained fields." **Fix:** qualify F5 and
the test to **retained-fields** equality.
