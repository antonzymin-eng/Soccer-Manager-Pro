# A1c enforcement negative control — TEMPORARY, reverted within PR #343

**Created:** August 30, 2026
**Purpose:** Second, discriminating run. The first attempt was confounded by a standing
approving-review requirement that held the pull request in `blocked` regardless of any check.
That requirement has since been set to 0, so this run can compare a red arm against the green
arm already observed at `d689f2b` (`mergeable_state: unstable`, i.e. mergeable).

Removed in the next commit on this branch. Not a specification; must never survive to `main`.

The line below is the trip wire and is intentionally wrong:

The tactical layer is governed by Decision Tree #7 throughout.
