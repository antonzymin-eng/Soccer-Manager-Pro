# News, Inbox & Man-Management #46 — Section 8: Cross-References & Back-Propagations

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass: ERR-030 renumbering)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-046-001 | #30 §2.2 `Fixture` | `{ RoundIndex, HomeClubId, AwayClubId, Played }` — *"the fixture list is the immutable schedule"*, and the result goes to the **table, not the fixture**. **The fact KD-1 rests on:** an item's scoreline is not recomputable from a save. |
| XC-046-002 | #30 §3.4 `AdvanceAndPlayNextRound` / `EmitMatchOutcome` | `Table.ApplyResult(result)` → `EmitMatchOutcome(result)` → `f.Played := true`. The `result` in hand at that instant carries the scoreline — the **only** moment an accurate match item can be captured (ERR-030-015). |
| XC-046-003 | #30 FR-SN-017 | #30 is *"the **producer only**"* of the match outcome — so a #46 projector at that site reads a committed value and imposes nothing on #30. |
| XC-046-004 | #30 §3.3 step 3 | Where the per-player `HumanSystemsDayInput` is assembled — #46's **drain** point, generalized to a producer loop with a post-sum clamp (ERR-030-024). |
| XC-046-005 | #30 §3.3 `RunWorldTickInFixedOrder` | #46 cites **no step number of its own** (KD-7). Its world-tick projectors run at **their producers'** already-pinned steps, so #46's emission ordering is inherited, not defined (§7.4 R-4). |
| XC-046-006 | #33 FR-HS-002 | #33 owns per-`PlayerId` `MoraleState` + `PersonalityProfile`; *"no other assembly writes them."* |
| XC-046-007 | #33 FR-HS-024 | *"#46 is the only consumer that **writes** #33 morale (man-management)."* Note #46 appears there **only as the writer** — the read-accessor list is *"#31/#35/#45"*. |
| XC-046-008 | #33 §3.3 + `XC-033-007` | *"no write path INTO #33 morale except **#46's future man-management seam** (deferred)."* **That seam is the routed `ExternalDeltaPermille`, not a #46-callable mutator** — ERR-033-004 files the wording so a future implementer cannot pick the other reading. |
| XC-046-009 | #33 **FR-HS-025** | *"Morale is a projection OUT of #33 — no two-way coupling … avoids determinism-ordering fragility."* **The requirement that makes FR-NW-006 a MUST**: #46 causes a write, so a #46 that also read morale would be exactly that coupling. |
| XC-046-010 | #33 §2.2 `HumanSystemsDayInput` + §3.1 `ComputeMoraleTarget` | A **transient input struct** — which is what keeps `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` untouched by ERR-033-003. |
| XC-046-011 | #33 F6 guard | `worldDay == LastAdvancedWorldDay ⇒ no-op` — covers external deltas for free, and is why exactly-once delivery is a property of **clearing at delivery** rather than of day matching. |
| XC-046-012 | #33 FR-HS-027 | The roster-lifecycle lockstep #46's **delta** drop-on-departure binds to (F8). #46's **items** deliberately do **not** (FR-NW-016). |
| XC-046-013 | #35 KD-6 read-only conference query | The surface #46's press projector consumes. **#35 exposes it for exactly this**, so neither spec references the other (KD-5). |
| XC-046-014 | #35 KD-3 / ERR-033-001 | The morale-routing mechanism #46 **adopts**, and whose producer-specific field name #46 **generalizes** (ERR-033-003 supersedes it). |
| XC-046-015 | #37 FR-AN-020 / FR-AN-021 | #37 *"MUST hold no persistent state"* and *"MUST consume live during the match"* — so #37 **cannot be called after the fact** and is **not an inbox source** (§1.4(c)). |
| XC-046-016 | #44 KD-1 | Records the same wall from the other side: *"#30 retains no per-fixture ledgers … recompute-on-load has no input."* The precedent KD-1 follows. |
| XC-046-017 | #49 FR-LC-002 / 006 / 012 / 013 / 014 / 015 / 008a | The producer contract: no baked string, locale-independent state, no sim-side reference to the localization assembly, a sibling adapter, disjoint slots, the intent-value pre-gate, base-locale coverage. |
| XC-046-018 | #49 §7.3 | Names **`InboxTextBoundary`** in advance, alongside `MediaTextBoundary` — #46 fits an existing extension point rather than extending the core. |
| XC-046-019 | #49 FR-LC-020 / #35's ERR-049-001 | #46 **inherits** the dependency and files nothing of its own (FR-NW-033). It is the second spec blocked on one wording fix, which is itself the argument for making it. |
| XC-046-020 | #19 §3.1.4 | Test-ID prefixes; the §5.10 closed-loop scenario registration under `SCENARIO_PATH_CROSS_SPEC_PREFIX`. |

## 8.2 At approval — land **atomically** with the status flip

| ID | Target | Change | Kind |
|---|---|---|---|
| **ERR-033-003** | `personalities-morale-dynamics/section-2.md` §2.2 `HumanSystemsDayInput` + §3.1 `ComputeMoraleTarget` | Generalize #35's `MediaDeltaPermille` to a **producer-agnostic `ExternalDeltaPermille`**, **summed across producers and clamped by the root** before it reaches #33 (KD-3). **Supersedes `ERR-033-001` as filed by #35** — one field, not one per producer. **Filed jointly with #35.** If #35 is approved first this is a rename of a field with no implementation behind it; if #46 is approved first, #35's back-prop lands already generalized. **Transient struct — no `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` bump.** | Additive field on a transient struct |
| **ERR-033-004** | `personalities-morale-dynamics/section-3.md` §3.3 + `section-2.md` FR-HS-024 | State that *"#46's man-management seam"* **is** the routed `ExternalDeltaPermille`, **not** a #46-callable mutator — closing the reading under which #46 would assign `MoralePermille` directly and contradict FR-HS-002. **No behaviour change**; it makes the only coherent reading the only available one. | Doc-only disambiguation |
| **ERR-030-024** | `season-competition-loop/section-3.md` §3.3 step 3 | Generalize #35's drain so step 3 iterates **every** external-delta producer (#35, #46, …), **summing and clamping** into `ExternalDeltaPermille`. Empty until the first producer's T2. | Doc-only seam generalization |
| **ERR-030-015** | `season-competition-loop/section-3.md` §3.4, after `EmitMatchOutcome(result)` | The **match-item projector** null seam (KD-2). **Filed by #46 in its own right** rather than assumed from #35's seam: that seam is #35's *conference queue*, so relying on it would make #46's most basic item type depend on **#35 being approved** — silently contradicting §7.1's independent-promotion property. Same site, so if both land they coalesce into one hook with two calls; **if #35 never lands, #46 still works.** | Doc-only seam insertion |

**Why #46 files a #30 seam #35 appears to already provide.** It is the same *site* but a different
*seam*: #35's is a conference-queue hook, #46's is a projector hook. Sharing one would couple #46's most
basic item type to #35's approval, which is precisely the dependency KD-2 exists to remove. Two null seams
at one site cost nothing and coalesce naturally.

## 8.3 Deferred — land at the named tier, **not** at approval

- The outer `SEASON_SAVE_FORMAT_VERSION` bump, at **T2** when the sub-blob is first composed in.
- Each producer's **root projector** (#30 match at T2; #35 press, #44 discipline, #45 board, #31
  transfers at T3) — **each lands with its producer, never ahead of it** (FR-LW-031). None is a #46
  change.
- **Man-management itself** (T3). Until then #46 files no pending deltas and the KD-3 seam carries `0`.
- Capturing **#37 view models** alongside a match item, **if** post-match stats become an inbox item
  (§1.4(c)) — a *root* extension, not a #46 change.

## 8.4 Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **#16 — nothing to change, and no placeholder exists or is needed.** #46 is draw-free at **every** tier
  (KD-8), and the catalogue has no `_RESERVED_` row for it — the #37/#39 read-only-spec class. **This is
  an asymmetry with #35**, which has `0x27` reserved: a future stochastic news generator in #46 would need
  a **fresh allocation**, not a promotion. Worth naming, because "no #16 row" reads as an oversight in a
  wave where most specs file one.
- **#49 — nothing at all, not even the wording fix.** #46 adds a sibling adapter, which is the documented
  extension point, and it **inherits** #35's `ERR-049-001` (FR-NW-033). A second identical filing would be
  noise, and the fact that two specs in one wave need the same fix is the argument for granting it.
- **#35 — nothing.** KD-5's boundary needs nothing from it, and its KD-6 read-only query is already the
  surface #46's projector consumes. The one #35-adjacent change is the ERR-033-003 coordination, filed
  against **#33**, not against #35.
- **#44 / #37 — nothing.** Read-only producers; #46 consumes their committed state through root
  projection, and #37 is not a source at all.
- **#30's loop logic — nothing beyond the two null seams.** #46 changes no existing step, no boundary
  roll, no season state, and — unlike #45 — bumps **no** `SEASON_STATE_FORMAT_VERSION`.

## 8.5 References

#46 introduces **no external citation**. Its content is a log, a routing contract, and a set of
boundaries composed from this project's own approved specs; there is no published result it rests on, and
inventing a citation to decorate the section would be the fabrication the project's rules forbid. The
§8.1 typed cross-references are the authorities, and every one names a file and a requirement that can be
re-checked.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §8 (XC-046-001..020, the approval-time back-props, the deferred set, the not-a-back-prop list, and the no-external-citation rationale) from supplement v0.6. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fix. **M:** the supplement's proposed **`ERR-030-014`** was **already filed** — it is ERR-030-014 itself, the match-engine playability defect found while running #30's T2 calibration pilot on July 26, the same day the supplement was written. Verified against `spec-error-log.md`; reassigned to **`ERR-030-024`**. Filing against a used id is the collision class the project's numbering discipline exists to prevent, and it would have landed silently because nothing cross-checks a proposed id against the log. (`-015` is genuinely free and is unchanged.) **L:** §8.4 gained the explicit **#16 asymmetry with #35** — #46 has no reserved value to promote at all — since "no #16 row" reads as an oversight in a wave where most specs file one. |
#endregion
