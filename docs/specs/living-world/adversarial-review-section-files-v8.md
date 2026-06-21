# Living World #22 — Section-File Adversarial Review PASS-8

**Created:** June 21, 2026
**Reviewer:** fresh-eyes pass over `docs/specs/living-world/` section files v0.8
**Result:** 1 M + 1 L (no High). All resolved in the v0.9 fix pass (same day).

---

## Medium

**M-1 — Active-set membership dynamics undefined.** FR-LW-023 defines the active set as "own-club +
bounded external contacts" and §3.5 handles the demotion *mechanics* (compress to `ColdSummary`), but
**no rule states when a contact enters or leaves the active set.** `ACTIVE_SET_EXTERNAL_CONTACTS_MAX`
caps the count, which implies a demotion when the cap is exceeded — but **which** contact demotes is
unspecified, so the deep/background split (and thus which entities carry memory/arcs) is
nondeterministic at the boundary. This is the same selection-determinism class as PASS-7. It also leaves
the design-note "active-set churn" question (supplement §6.6) unaddressed in the promoted spec. **Fix:**
define the membership rule — entry on first interaction; when the cap is exceeded, demote the
**least-recently-interacted** external contact (last-interaction = max episode `worldTick` on the edge;
ties → lowest `EntityId`) to cold-store. Add to FR-LW-023 and §3.5; record it as closing the churn item.

## Low

**L-1 — `SAVE_SIZE_BUDGET` value reads as a placeholder.** Appendix A lists its value as "(platform)",
which brushes against FR-LW-029 (no `[EST]`; every `[GT]` carries a value). **Fix:** label it explicitly
**platform-tuned** (a per-platform designer value), not unset, to distinguish it from a disguised `[EST]`.
