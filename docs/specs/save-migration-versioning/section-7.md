# Save Migration & Versioning #50 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + `SaveClass` / `BlobKind` / `SaveOriginStamp` / `IMigrationStep` + `SaveVersionClassifier` + `MigrationRegistry` + `MigrationRunner`, with an **empty registry**. No generation gate, no #49 binding, no #39 comparison. | **The identity.** A current save runs zero transforms; every other save is refused **exactly as today** |
| **T1** | The `SaveOriginStamp` in #30's frame (ERR-030-019) + `GenerationGate` + `GenerationRegistry` with **one** registered generator (the current one). | **The lock, with nothing yet to migrate.** An equal stamp proceeds; a differing one refuses |
| **T2** | `MigrationCommit`'s non-destructive write; `MigrationRefusal` through `MigrationTextBoundary` + the base-locale rows; the FR-MG-022 build-time completeness check. | **Complete for format bumps** — the first real bump has somewhere to register |
| **T3** | The first **generation** migration (at the first post-ship generation change), and `CompareForConflict` for **#39**. | **Named activation** — both gated on something outside #50 |

**T0 should land early precisely because it changes nothing.** It is the rare identity tier whose claim is
unusually strong: refusal **is** today's behaviour, so the seam is byte-identical to pre-#50 in both
branches, not merely in the disabled one. Landing it puts the registry in place while there is still
nothing to migrate — so the first real bump registers a step instead of provoking an emergency design
under release pressure.

**T1 before T2 is deliberate, and is the ordering most likely to be argued with.** The generation stamp
buys nothing until a generation changes, so it looks postponable — but it must be **in saves already
written** to be useful. A stamp added at T3 can only classify saves written after T3; every career from
before it is permanently unclassifiable, and refusing those is the worst possible first impression of a
migration system. **The stamp is worth writing long before it is worth reading.**

**The predicted T2 failure is the completeness check**, not the write discipline: FR-MG-022's build-time
assertion is easy to write as a warning and easy to skip under deadline, and its absence is invisible
until a player's save is refused with no diagnosis.

**T3's generation half is the expensive one**, and it is the tier where R-5's floor stops being a policy
sentence and starts being retained code.

## 7.2 Deep-tier extensions (designed for, not built)

- **Every per-bump `IMigrationStep`** — each lands with **its own** spec's bump, never in advance. There
  is nothing to migrate until a format changes twice, and a step written speculatively would be a
  transform from a version no save carries.
- **The `WORLD_GENERATION_VERSION` bump itself**, at the first post-ship generation change — and it should
  feel expensive, because it is (KD-2).
- **#39's conflict UX** over `CompareForConflict` (KD-5).
- **A wider generation surface** — #32's knowledge bands and #36's nationality derivation are already
  covered by FR-MG-011 and need no new mechanism; a new derived-on-read table joins the same version.
- **A migration report** — a diagnostic listing of which blobs migrated through which versions. Useful for
  support, and deliberately **not** persisted into the save (§7.3).

## 7.3 Explicitly not planned

- **Weakening any codec gate to accommodate a migration** (FR-MG-003). The day this happens, the reason
  the seam is safe stops being true, and a migration bug stops surfacing as a refusal.
- **Migrating in place with a rollback** (R-3). Simpler, will be proposed, and is the only design here
  that can lose a career.
- **A #50 sub-blob of its own.** A spec that migrates other people's data should not become the
  twenty-sixth format version (§4.5). The `SaveOriginStamp` goes in #30's frame.
- **Migration history inside the save.** A chain of "was migrated from" records would be a second,
  unversioned format living inside the version system.
- **Migrating on `BuildId`** rather than on format versions. Two builds sharing a format would become
  falsely incompatible — a whole class of spurious refusals produced by a diagnostic field (§2.2).
- **Eagerly materialising every save at save time** so no old generator is ever needed (KD-2, rejected).
  It inflates every save forever to insure against a bump that may never happen.
- **Forbidding generation changes post-ship** (KD-2, rejected). Unenforceable; balance work will want
  them, and a rule that must never be broken will be broken quietly.
- **Guaranteeing counterfactual identity** (FR-MG-033). A synthesizing bump makes it unachievable, and
  promising it would force a transform to fake data it does not have.
- **Silently regenerating a world whose generation version differs** (F6). The failure this spec exists to
  prevent.
- **Writing a migrated save back on load** (FR-MG-031). It turns opening a career on a second machine into
  a data-loss event.

## 7.4 Risks carried

- **R-1 — the generation blind spot is the whole reason this spec is more than plumbing** (KD-2). A #50
  shipped without it has a migration system that provably cannot detect its **most likely** breaking
  change: nothing about a `[GT]` ramp tweak looks like a save-format change, which is exactly why it gets
  made. Highest-priority item in the spec.
- **R-2 — 25 version constants and rising** (§1.4(a)). The per-blob model scales, but the registry's
  bookkeeping does not stay free: a spec that bumps a version and forgets to register a step turns every
  old save into `Unsupported` **with no diagnosis**. FR-MG-022's build-time check is the mitigation, and
  it must be a build failure rather than a warning.
- **R-3 — "migrate in place, roll back on failure" will be proposed** because it is simpler (KD-4). It is
  also the only design here that can lose a career, and it loses one exactly when the rollback fails —
  the moment it is most needed.
- **R-4 — KD-6's honesty may be read as weakness.** A synthesized field means a migrated career is not the
  career that would have been played. Saying so is better than a guarantee that quietly fails at the first
  representation change — and #45's `JobSecurity` float→band bump is already queued to be exactly that.
- **R-5 — the supported floor is a product decision with a real code cost.** It sets chain length, test
  surface, and — per KD-2 — **how many frozen generator versions the build ships**. #50 defines the
  mechanism and leaves the floor a policy constant, but it should be chosen knowing it is measured in
  retained code that normal play never exercises.
- **R-6 — the first migration will be written before anyone has migrated anything.** Every step in the
  registry is code whose only exercise is a test, written by a spec author whose attention is on the bump
  rather than on the transform. T-MG-FAIL-003's deliberately-broken step and T-MG-I-001's
  pass-the-real-codec assertion exist because the first real step is likelier to be wrong than any other
  code in this spec — and because the seam's design means a wrong one refuses rather than corrupts, which
  is the property worth preserving above all others here.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §7 (T0–T3, with the argument for landing T0 early — its identity claim is unusually strong because refusal *is* today's behaviour — and the argument for T1 before T2: the generation stamp **is worth writing long before it is worth reading**, since a stamp added late leaves every earlier career permanently unclassifiable; deep-tier extensions; the not-planned list, which carries the two rejected KD-2 alternatives and the two designs that lose data; risks R-1..R-6, with R-6 added for the fact that every registered step is code whose only exercise is a test). Status IN REVIEW. |
#endregion
