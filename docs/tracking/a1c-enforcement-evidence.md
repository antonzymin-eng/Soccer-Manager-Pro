# A1c — merge-protection enforcement evidence

> **Created:** August 30, 2026
> **Purpose:** Durable capture of the A1c enforcement measurement. `mergeable_state` is a computed,
> point-in-time value that GitHub does not retain historically, so it cannot be recovered later from
> the API. Everything else here — run ids, job ids, conclusions, head SHAs — is independently
> verifiable against the GitHub Actions API for as long as the runs are retained.
> **The two arm commits are preserved as remote branches** `evidence/a1c-green-arm` (`d689f2b`) and
> `evidence/a1c-red-arm` (`d497a4d`). They are deliberately NOT ancestors of the landing commit: the
> working branch was squashed to a single commit, which orphaned them. An earlier draft of this file
> claimed they remained reachable from the branch history — that was false, and preserving them as
> refs is the fix. `git diff evidence/a1c-green-arm evidence/a1c-red-arm` is the whole experiment:
> one file, thirteen lines. Do not delete those branches; this record depends on them.
> **Owning plan:** `docs/planning/project-architecture-governance-integration-plan.md` §11 A1c.

---

## 1. Configuration read

Repository ruleset **`CI for Main branch`**, targeting `main`, **Enforcement: Active**.

*Require status checks to pass*, read in repository settings by **antonzymin-eng** on
**August 30, 2026**, in full:

| # | Required context |
|---|---|
| 1 | `Markdown lint` |
| 2 | `YAML lint` |
| 3 | `Markdown link check` |
| 4 | `Spec hygiene checks` |
| 5 | `File manifest sanity` |
| 6 | `C# format check` |

**One absence is load-bearing**, and is the reason the completion criterion demands the whole list
rather than a spot-check for the context of interest. A second is recorded as context only, because
an earlier draft wrongly treated it as load-bearing too:

- **`Compile + test (Linux shim gate, non-certifying)` is NOT required.** It carries
  `sim_match_engine_close_chance`, held red by owner decision since August 11, 2026 and red on `main`
  itself. Requiring it would freeze every merge.
- **`Unity tests` is NOT required.** It resolves to `skipped` on every run here (no Unity licence).
  **Correction, recorded rather than silently dropped:** an earlier draft of this file, and of the
  owning plan's criterion 1, asserted that a required-but-skipped context would freeze every merge.
  **That is wrong.** GitHub documents `skipped` as satisfying a required status check — which is how
  path-filtered and conditional jobs avoid blocking — so requiring `Unity tests` would not deadlock
  merges. The assertion was made from inference, never tested, and is withdrawn. What remains true and
  is the only claim made here: a required context that reports `failure` blocks (§2 measures this), and
  the shim gate above is the real freeze risk because it reports `failure`, not `skipped`.
  **This repository has not tested the skipped-required case**, and nothing here should be cited as
  evidence about it.

**Required approving reviews: 0** at the time of measurement. See §4.

---

## 2. The two arms

Both arms were pushed from `claude/a1c-ci-ruleset-enforcement-nv5esp` and are preserved as the remote
branches `evidence/a1c-green-arm` and `evidence/a1c-red-arm` (see the header). They differ by exactly
one file: a temporary `docs/specs/_a1c-enforcement-negative-control.md` carrying a single stale
`Decision Tree #7` line, verified before pushing — against the job's own grep, including its
exclusion filters — to be the only hit in `docs/specs` and `src`. The difference is checkable today:
`git diff evidence/a1c-green-arm evidence/a1c-red-arm` reports one file, thirteen insertions.

### Green arm — `d689f2b`, workflow run `33282969787`

| Job | Job id | Conclusion | Required |
|---|---|---|---|
| `Markdown lint` | 99181180088 | success | ✅ |
| `YAML lint` | 99181180125 | success | ✅ |
| `Markdown link check` | 99181180067 | success | ✅ |
| `Spec hygiene checks` | 99181180035 | **success** | ✅ |
| `File manifest sanity` | 99181180091 | success | ✅ |
| `C# format check` | 99181180172 | success | ✅ |
| `Compile + test (Linux shim gate, non-certifying)` | 99181180037 | `in_progress` at read; **`cancelled`** at 00:25:29Z | ✗ |
| `Unity tests` | 99181190668 | skipped | ✗ |

**`mergeable_state: unstable`** — mergeable.

### Red arm — `d497a4d`, workflow run `33283274231`

| Job | Job id | Conclusion | Required |
|---|---|---|---|
| `Markdown lint` | 99181984696 | success | ✅ |
| `YAML lint` | 99181984757 | success | ✅ |
| `Markdown link check` | 99181984708 | success | ✅ |
| `Spec hygiene checks` | 99181984661 | **failure** | ✅ |
| `File manifest sanity` | 99181984649 | success | ✅ |
| `C# format check` | 99181984654 | success | ✅ |
| `Compile + test (Linux shim gate, non-certifying)` | 99181984720 | `in_progress` at read; **`cancelled`** | ✗ |
| `Unity tests` | 99181996640 | skipped | ✗ |

**`mergeable_state: blocked`** — not mergeable.

### Conclusion

One variable moved: the conclusion of `Spec hygiene checks`. Every other required check was green in
both arms; no approval was outstanding in either; no review thread was unresolved in either.
**A red `Spec hygiene checks` stops the merge. A1 has objective enforcement.**

**On the shim gate, stated precisely, because an earlier draft of this record got it wrong.** It was
**`in_progress` at the moment each reading was taken**, and ended **`cancelled`** in both arms — not
`failure`. The cancellation is the workflow's own doing: `.github/workflows/ci.yml` sets
`concurrency: { group: ci-${{ github.ref }}, cancel-in-progress: true }`, so pushing the next commit
killed the previous run's long-running job. This does not affect the result — the gate is not a
required context, and it was in the *same* state in both arms — but "red in both arms", as first
written, was false.

---

## 3. Why a single reading proves nothing

`mergeable_state: blocked` is returned for **any** of: an unmet approving-review requirement, an
unresolved review conversation, a *pending* required check, or a *failing* required check. It does
not name which.

This was learned the expensive way. A first attempt at this measurement, on this same pull request,
observed `blocked` with `Spec hygiene checks` red and reported it as proof of enforcement. It was
not: a then-standing approving-review requirement was holding the pull request `blocked` in **both**
arms, so the observation had no discriminating power at all. That claim was withdrawn. Two further
`blocked` readings during the same session were caused by `Markdown link check` — a required context
that takes ~4.5 minutes — still being *pending*.

Hence the rule now carried in the owning plan §11 A1c criterion 3: **paired arms varying exactly one
required check, with every other required check green in both and every non-required check in the
same state in both. A single-arm reading is not acceptable evidence.**

---

## 4. Review-policy change made during this work

Required approving reviews on `main` were **1**, and were set to **0** by the owner on
August 30, 2026, during A1c.

**This was a deliberate decision, not a side effect, and it is recorded here because A1c would
otherwise read as having strengthened one gate while quietly removing another.**

**⚠️ The rationale first written here was wrong and is corrected.** That draft said a required
approval on a single-maintainer repository "is satisfied by the author approving their own pull
request, which is ceremony rather than review". **GitHub does not permit a pull request's author to
approve their own pull request.** The requirement could therefore only ever have been satisfied by a
*second* person with write access. This repository has one maintainer.

That inverts the analysis, and the corrected version is the one to rely on:

- The requirement was **not** a working human gate that this change removed. With one maintainer and
  no second reviewer, it was **unsatisfiable** — every pull request would have been permanently
  unmergeable through the normal path, leaving admin bypass as the only remaining route to `main`. A
  control that can only be satisfied by overriding it is not a control; it trains the bypass habit
  instead.
- It had never actually bitten before, because the ruleset carried `enforcement: disabled` until
  August 29, 2026. PRs #338–#342 all merged under that disabled ruleset. The requirement became real
  for the first time on activation, and PR #343 was the first pull request it ever held.
- Setting it to 0 was therefore **necessary for normal, non-bypass merging by a single maintainer**
  once the ruleset went Active. It was not the removal of ceremony, and not the removal of a
  functioning review. Admin bypass did remain available, so "could not merge at all" would overstate
  it: the accurate claim is that every merge would have required an override.

**The cost, stated accurately.** The honest cost is not "a human gate was removed" — no such gate was
operating. It is that **no human-review gate now exists to be strengthened later**, and a substantial
share of what lands here is agent-authored. The six required status checks are mechanical and check
none of what a reviewer would. **The condition under which this should be revisited is concrete: if a
second person with write access ever exists, restore the requirement to 1** — at that point it
becomes a real gate rather than a deadlock. Until then it cannot be satisfied and should stay at 0.

The measurement did not depend on the setting *staying* at 0: the paired arms are already recorded
above, and restoring the requirement to 1 would not invalidate them. Reverting is therefore a free
decision at any time, and re-measurement would not be owed.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.2 | August 30, 2026 | — | Two wording contradictions from external review. §1 said "two absences are load-bearing" and then correctly explained that only the shim gate is; `Unity tests` is now recorded as context only. "Necessary to merge at all" is narrowed to "necessary for normal, non-bypass merging" throughout, since the same passages acknowledge admin bypass remained available. No substantive claim changed. |
| 1.1 | August 30, 2026 | — | Three corrections from external review. (a) The arm commits were orphaned by the branch squash and are NOT ancestors of the landing commit; they are now preserved as remote branches `evidence/a1c-green-arm` and `evidence/a1c-red-arm`, and the false "reachable on this branch's history" claim is replaced. (b) The claim that a required-but-skipped context would freeze merges is withdrawn — GitHub documents `skipped` as satisfying a required check; the assertion was inference, never tested. (c) The review-policy rationale is inverted: GitHub forbids a PR author approving their own pull request, so the 1-approval requirement was unsatisfiable on a single-maintainer repository rather than ceremony, and setting it to 0 was necessary to merge at all once the ruleset went Active. The revisit condition is now concrete: restore to 1 if a second write-access reviewer exists. |
| 1.0 | August 30, 2026 | — | Initial capture: required-checks list read in settings, both measurement arms with run/job ids and conclusions, the shim-gate `cancelled`-not-`failure` correction, why a single `blocked` reading is not evidence, and the required-approving-reviews 1 → 0 decision with its cost. |
