# Living World #22 — Section-File Adversarial Review PASS-9

**Created:** June 21, 2026
**Reviewer:** fresh-eyes pass over `docs/specs/living-world/` section files v0.9
**Result:** 1 M + 1 L (no High). All resolved in the v0.10 fix pass (same day).

---

## Medium

**M-1 — PASS-8's LRU-demotion contract has no test.** The active-set membership rule added in PASS-8
(at `ACTIVE_SET_EXTERNAL_CONTACTS_MAX`, demote the least-recently-interacted contact, ties → `EntityId`)
is a deterministic selection that **mutates persisted state**, but no test covers it — `T-LW-I-011..014`
exercise the demotion *mechanics* and rehydration, not the cap-exceeded *selection*. Same gap class as
PASS-4 (a prior fix's contract left unverified). **Fix:** add `T-LW-I-015` asserting cap-exceeded demotion
picks the least-recently-interacted contact (tie → lowest `EntityId`) and is replay-identical; update
counts + traceability.

## Low

**L-1 — Own-club departure demotion path unspecified.** §3.5 says own-club members "never demote **while
at the club**," which implies they demote **when they leave** (transfer/release) — but no rule states it.
FR-LW-025 covers "a contact leaving the active set" generically; §3.5 should connect own-club departure
to that path explicitly. **Fix:** state that on transfer-out/release an own-club member demotes to
cold-store like an external contact (preserving the "ex-player you might re-sign or face" history).
