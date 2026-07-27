# Steam Packaging & Release Engineering #39 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + `EvidenceRecord` / `EvidenceKind` / `ReleaseGate` + `ConflictOutcome` / `ConflictPolicy`, with their unit suites. **No Cloud, no achievements, no build job.** | **Inert** — and the gate's fail-closed property is already **fully testable**, because it is a pure fold |
| **T1** | The **release runbook** + the first committed evidence record, run manually on the pinned host against the current tree. **No player build yet** — the five input-side rows only. | **The gate exists and is exercised**, minus its artifact-side half |
| **T2** | The **first player build** + the packaged-build smoke path + the compliance checklist subset. The gate becomes complete. | **Shippable** — this is the minimal tier, with Cloud off |
| **T3** | Cloud enabled: `ICloudSyncPolicy`, the shell's Steam binding, `#50`-delegated conflict resolution. Achievements: the identity set + the shell's evaluator + the offline queue. | **Cloud-synced and achieving** |

**T0 is worth landing early for an unusual reason: the spec's central claim is testable before anything
ships.** `EvaluateGate` is a pure function of an evidence set (FR-PK-020), so *"a skipped check fails the
gate"* — the inversion this whole spec exists to introduce — can be locked against the **real**
skip-shaped CI artifact months before a release. A design that left the gate as prose would have nothing
to test until the first release, which is the worst possible moment to discover the posture is wrong.

**T1 before T2 is deliberate and is the ordering most likely to be argued with.** A gate with five of six
rows looks incomplete, and the temptation is to wait for the build. But **a gate never exercised is a
document** (R-5): running the runbook against the current tree proves the evidence records are producible,
reviewable and commit-bound, and it does so while the cost of finding out they are not is low.

**T2 is where the identity claim retires.** Up to that point *"Cloud off + no achievements = today's save
path"* is trivially true because none of it exists; from T2 it is a shipped property that FR-PK-044 and
T-PK-ID-002 hold.

**The predicted T3 failure is the metadata shortcut** (F4). The Steam SDK surfaces a conflict as
metadata, fetching costs a round trip, and *"resolve by timestamp"* is both the obvious implementation and
the one every rule in KD-1 exists to reject. §6.2 records that the performance argument is what will be
used to justify it.

## 7.2 Deep-tier extensions (designed for, not built)

- **A CI player-build job**, at the first packaged build — infrastructure, not a spec amendment, and
  deliberately not written before an artifact exists to build.
- **The achievement identity set**, per release — content, subject to FR-PK-028's APPEND-only rule once
  shipped.
- **Multi-branch / beta packaging** — a second artifact class through the same gate, with the same
  evidence requirements.
- **Gate automation**, as far as the host constraint permits — the input-side rows can be collected
  mechanically; the certifying half cannot move (R-2).
- **Bit-identical packaging** (KD-6), **Stage 5+**, alongside #52 — materially more valuable when peers
  must agree, and a project in itself before then.
- **Richer platform integration** — rich presence, stats, leaderboards. Each is subject to FR-PK-027: the
  sim may not read any of it.

## 7.3 Explicitly not planned

- **Modelling the gate on CI** (FR-PK-011/012). CI is skip-open by design and is right to be; the gate is
  fail-closed. **Sharing a posture is the defect this spec exists to prevent.**
- **Running the certifying evidence in CI** (FR-PK-017). It is a proposal to stop certifying (R-2), and
  the runbook says so in its own words.
- **Comparing version numbers inside #39** (FR-PK-001). A second version authority is the two-truths
  defect; the `#39 → #50` reference exists precisely to make it unnecessary.
- **Resolving a conflict from metadata** (FR-PK-003). Faster, obvious, and it undoes every conflict rule.
- **Merging two divergent careers** (FR-PK-005), at any tier. A save is one causal history.
- **Uploading a migrated save on load** (FR-PK-008). A read must never become a write.
- **Deleting or repairing a `Corrupt` copy** (FR-PK-007). Non-destructive refusal extends to Cloud.
- **Per-sub-blob sync** (FR-PK-033). It would require #39 to parse a frame it has no business parsing.
- **Continuous mid-session sync** (KD-5, rejected). It maximises the divergent-career window, which is
  the one state that cannot be merged.
- **Achievement state in a save** (FR-PK-026). It would make a save's bytes depend on a player *account*
  and enlist #50 in migrating trophy state.
- **A local unlock ledger** (FR-PK-024). The platform is the store of record; a second owner produces
  double grants or lost unlocks.
- **Any sim read of achievement state** (FR-PK-027). The one #39 violation that is a determinism defect.
- **Gating on a bit-identical rebuild** (FR-PK-038). Nobody has demonstrated it here, and gating on an
  undemonstrated property is how a gate becomes a formality.
- **Specifying store assets** (FR-PK-032). #39 specifies the checklist and its gate classes.

## 7.4 Risks carried

- **R-1 — the gate is only as good as its evidence being *fresh*, and this is load-bearing rather than
  hygienic.** Because five of six evidence rows measure the project rather than the artifact (KD-2), the
  **commit identity is the only thing binding them to the build being shipped**. An evidence record naming
  a different commit is **worse than none, because it looks like a pass**. Every record carries its
  commit; the gate compares it to the artifact's; a mismatch fails (FR-PK-014).
- **R-1a — the packaged-build smoke path is the only artifact-side check, so its coverage is a real
  decision.** Too small and packaging failures ship; too large and it becomes a second test suite
  maintained in the worst possible environment. §5.7 requires each step to be justified individually, and
  `PK_BUDGET_SMOKE_PATH_MIN` is the mechanism that keeps the pressure visible.
- **R-2 — the host constraint is permanent, not transitional** (§1.4(c)). The certifying half cannot move
  into CI without abandoning the pin. **Any future proposal to "just run the cert in CI" is a proposal to
  stop certifying**, and should be read as one.
- **R-3 — Cloud is the first second writer this project has ever had** (§1.4(d)). Most save bugs that
  reach players will arrive through it, and **they will look like corruption while being conflict
  mishandling** — which is why the diagnosis matters as much as the prevention. KD-1's non-destructive
  rules are what keep them recoverable.
- **R-4 — the generation version bites hardest here** (#50 KD-2). Two machines on different builds is the
  **normal** Cloud case, not an edge case, so #39 should expect `Unsupported` in the wild and treat a
  clean refusal as a **success path** rather than an error to engineer away.
- **R-5 — front-loading.** The checklist and the policy can be written now; the gate can only be
  *exercised* against real artifacts, and **a gate never exercised is a document**. §5.7 states exactly
  which parts are exercisable pre-artifact, so the gap is visible rather than assumed away.
- **R-6 — #39 is authored last and will be implemented under release pressure.** Every rule in this spec
  costs something at exactly the moment it is least welcome: the gate blocks a ship, the conflict policy
  refuses a sync, the smoke path adds ten minutes. **The rules most likely to be waived are the ones with
  the highest cost of being wrong** — FR-PK-012 (a skip is not a pass) and FR-PK-003 (never resolve from
  metadata). Recording that here is the only mitigation a spec can offer.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §7 (T0–T3, with the argument that T0 is unusually valuable because the spec's central claim is a **pure fold** and therefore testable against the real skip-shaped CI artifact long before a release; T1 before T2 on the ground that a gate never exercised is a document; the predicted T3 metadata-shortcut failure; deep-tier extensions incl. the Stage-5+ bit-identical deferral; the not-planned list, which is unusually long because most of this spec's content is a prohibition; risks R-1..R-6, with R-6 added because #39 is authored last and implemented under release pressure, which is precisely when its two highest-cost rules are most likely to be waived). Status IN REVIEW. |
#endregion
