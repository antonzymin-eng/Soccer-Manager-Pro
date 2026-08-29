# A1c enforcement negative control — TEMPORARY, reverted within PR #343

**Created:** August 29, 2026
**Purpose:** Deliberately trip the `Spec hygiene checks` job so that the `CI for Main branch`
ruleset can be observed actually blocking a merge. This file is removed in the next commit on
this branch. It is not a specification and must never survive to `main`.

The line below is the trip wire and is intentionally wrong:

The tactical layer is governed by Decision Tree #7 throughout.
