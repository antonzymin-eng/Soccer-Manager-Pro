# Media & Press Interactions #35 — Section 8: Cross-References & Back-Propagations

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass: ERR-030 renumbering)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-035-001 | #49 FR-LC-002 | A producer MUST NOT emit a baked localized string. #35 emits an identity + native values (FR-ME-005). |
| XC-035-002 | #49 FR-LC-004 | `ILocalizer.Render(in LocalizedTextRequest)` is the render seam; the request carries `TextTemplateId`, the `ulong` selection, slots, and the citation clause. |
| XC-035-003 | #49 FR-LC-005 | The renderer MUST NOT draw from any RNG stream — which is *why* the selection value is supplied by the producer, and why the seam's legitimate interest is determinism, not provenance (§1.4(c)). |
| XC-035-004 | #49 FR-LC-006 | State MUST be locale-independent. #35 stores **no `string`** (FR-ME-003), locked by T-ME-LOC-005. |
| XC-035-005 | #49 FR-LC-007 | `variant = draw % variantCount(BaseLocale, Id)` — **total at `draw = 0`**, which is what makes FR-ME-018's fallback viable. |
| XC-035-006 | #49 FR-LC-008a | Base-locale coverage. #35 extends it to its **full** roster — questions **and** options (FR-ME-012 / T-ME-LOC-001). |
| XC-035-007 | #49 FR-LC-012 | **No sim assembly may reference `TacticalDirector.Localization`** (F6 there is a build error). This is why `MediaTextBoundary` is not in `src/media/` — it would not compile (§4.2). |
| XC-035-008 | #49 FR-LC-013/014 + §2.2 | A producer binds by adding a **sibling boundary adapter**, and emits only its **own** native values — *"#35/#46 carry disjoint slots"* (§2.2 verbatim). |
| XC-035-009 | #49 §7.3 | Names `MediaTextBoundary` **in advance**: *"a new boundary adapter … is added when that producer is built."* #35 fits the existing extension point rather than extending it. |
| XC-035-010 | #49 FR-LC-015 | The pre-render gate is an **intent-VALUE roster check** (FR-ME-011), run before any selection work. |
| XC-035-011 | #49 FR-LC-020 | **The one #49 defect #35 surfaces.** A MUST on the generic core seam naming #22's specific stream — unsatisfiable by any other producer, and contradicting §7.3, FR-LC-013/014 and FR-LC-005 in the same spec. Resolved by ERR-049-001. |
| XC-035-012 | #33 FR-HS-002 | #33 owns per-`PlayerId` `MoraleState` + `PersonalityProfile`; *"no other assembly writes them."* #35 never does (FR-ME-004). |
| XC-035-013 | #33 FR-HS-024 | *"#46 is the only consumer that writes #33 morale."* #35 is a **read-only** consumer, deferred; its consequence travels as a value (KD-3). |
| XC-035-014 | #33 §2.2 `HumanSystemsDayInput` + §3.1 `ComputeMoraleTarget` | The committed-input mechanism #35's delta rides — a **transient** struct, which is what keeps `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` untouched (ERR-033-003). |
| XC-035-015 | #33 FR-HS-008 | The unadvanced cursor sentinel is `uint.MaxValue`, **not** `0` — day `0` is a legal world day. Adopted verbatim for `MediaCursors.LastAdvancedWorldDay`. |
| XC-035-016 | #33 F6 guard | Same-day ⇒ **no-op**. This is what **bars** same-day delivery: a re-run of step 3 would be a no-op, so the delta would be silently *dropped* rather than applied (KD-3). |
| XC-035-017 | #33 FR-HS-027 | The roster-lifecycle lockstep #35's drop-on-departure rule binds to (ERR-033-002 / F9). |
| XC-035-018 | #30 §3.4 `AdvanceAndPlayNextRound` / `EmitMatchOutcome` | The producer-only post-round hook the queue seam attaches to — the same path that already carries #44's availability null seam, so a second null seam there is an established shape. |
| XC-035-019 | #30 §3.3 step 3 | Where the per-player `HumanSystemsDayInput` is assembled, and therefore where the **drain** happens (ERR-030-023, the seam that would otherwise be missed). |
| XC-035-020 | #30 FR-SN-034 + §3.3 | The pinned tick order #35's expiry slot enters — **currently malformed** (§8.0). |
| XC-035-021 | #16 §3.4 | `_RESERVED_0x27_` / `SubsystemOrdinals 89` **already exist and are already correct** for a draw-free minimal tier — **no back-prop at approval**, unlike #45 which had to file its own placeholder. |
| XC-035-022 | #19 §3.1.4 | Test-ID prefixes; the §5.9 closed-loop scenario registration under `SCENARIO_PATH_CROSS_SPEC_PREFIX`. |

## 8.0 Prerequisite — must land **before or with** promotion

| ID | Target | Change |
|---|---|---|
| **ERR-030-022** | `season-competition-loop/section-3.md` §3.3 + `section-2.md` FR-SN-034 (+ `spec-error-log.md` errata) | **Tick-order reconciliation.** Give #32 scouting an unambiguous step (**9**, satisfying its own *"after staff"* rationale without renumbering the six slots approved specs cite by number); delete the orphaned duplicate `AdvanceDay` line; append the **#35 media expiry seam as step 10**; `AdvanceDay` → **step 11**; extend FR-SN-034's enumeration to include **#32 and #35**. Record as errata the two duplicate-id collisions (`ERR-030-007` used for both #42's academy step and #32's scouting step; `ERR-030-009` for both #45's `JobSecurity` band and #44's §3.4 availability filter) and the duplicate `0.7` / `0.8` version-history rows. |

**Why this is a prerequisite and not a #35 back-prop.** #30's pinned tick order is currently malformed —
**two seams claim step 7** (#42 academy and #32 scouting, the latter sitting *after* the live-tick line
with its own duplicate `# 8. world day`), **FR-SN-034's enumeration omits #32 entirely**, and two ERR ids
are each used twice for different changes. All three defects come from same-day parallel approvals (#42
and #32 both landed July 24; #44 and #45 both touched §3 on July 24–25). They exist **today, independent
of #35**, and need fixing whether or not #35 is ever authored. #35 merely cannot cite a defensible step
number until they are.

## 8.2 At approval — land **atomically** with the status flip

| ID | Target | Change | Kind |
|---|---|---|---|
| **ERR-049-001** | `localization-accessibility/section-2.md` FR-LC-020 | Generalize the `SelectionDraw` provenance from *"the `ulong` returned by `DeterministicRngService.DrawReserved` (the `world.text` reservation)"* to **"the producer's own deterministic, locale-independent selection value, carried verbatim"**, keeping #22's `world.text` draw as the named example. Resolves the contradiction with §7.3's *"if they draw"*, FR-LC-013/014's producer-agnostic core, and FR-LC-005 (§1.4(c)). **Contract-widening only** — #22's existing binding still satisfies it verbatim, and #49 needs no code, type, or catalogue change. | Doc-only requirement fix |
| **ERR-033-003** | `personalities-morale-dynamics/section-2.md` §2.2 `HumanSystemsDayInput` + §3.1 `ComputeMoraleTarget` | Add a **producer-agnostic** committed field `ExternalDeltaPermille`, `Neutral` = `0`, consumed as an additive term alongside `BoardObjectiveDeltaPermille`; **summed across producers and clamped by the root** before it reaches #33. **Transient input struct — no `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` bump**; `0` ⇒ target unchanged, so it is behaviour-neutral until a **non-zero** delta is delivered. **Filed jointly with #46**, which is the second producer of the same quantity. | Additive field on a transient struct |
| **ERR-033-002** | `personalities-morale-dynamics/section-2.md` FR-HS-027 | Extend the roster-lifecycle lockstep to state that a **routed input's** pending source-side value is dropped with the player's entries (the rule #35's undelivered deltas bind to, F9). Alternatively filed #35-side if #33's owner prefers the obligation on the producer — **the rule is what must exist, not its file.** | Doc-only rule extension |
| **ERR-030-023** | `season-competition-loop/section-3.md` §3.4 + §3.3 step 3 | **Two** media seams: the **queue** null seam after `EmitMatchOutcome(result)` (the #44 availability-seam shape), and the **drain** at step 3 where the per-player `HumanSystemsDayInput` is assembled. Both empty / `0` until #35 T2. **Filing only the first would produce recorded-but-never-delivered deltas, with every #35-local test still green.** | Doc-only seam insertion |

**Why `ERR-033-001` does not appear.** The supplement filed the morale field as `MediaDeltaPermille` under
that id. #46 — authored second in this wave — is the **second** producer of exactly this quantity (a
bounded off-pitch morale delta from a manager action), so a per-producer name does not survive: producer
#3 would need a third field on an approved struct. The field is therefore producer-agnostic and the id is
**ERR-033-003**, filed jointly. The *mechanism* #35 chose is unchanged and is what #46 adopts; only the
name and arity move, and only while both specs are pre-promotion with no implementation behind either.

This is the same shape as the defect #35 found in #49's FR-LC-020: **a contract written correctly for one
producer, surfacing the moment a second arrives.** Recording it in both specs means neither can be read
alone and get it wrong.

## 8.3 Deferred — land at the named tier, **not** at approval

- Promotion `_RESERVED_0x27_` → `DOMAIN_TAG_MEDIA = 0x27` at the deep tier's **first selection draw**
  (FR-ME-016). Registering it earlier is the phantom-surface class FR-LW-031 forbids.
- The outer `SEASON_SAVE_FORMAT_VERSION` bump, at **T2** when the sub-blob is first composed in.
- `BoardDayInput.MediaSignalPermille` on **#45** — the deep-tier board-facing consequence (KD-4),
  arriving as a routed committed value exactly like its existing `MoraleSignalPermille`.
- A #33 morale **read** for mood-aware phrasing (FR-HS-024 anticipates it; deferred per FR-LW-031). **When
  it lands it arrives as routed committed values, not an assembly reference** — preserving §4.1's DAG and
  keeping §5.8's assertions unconditional.

## 8.4 Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **#16 — nothing to change, and no placeholder to file.** `_RESERVED_0x27_` / `SubsystemOrdinals 89`
  **already exist** and are already correct for a draw-free minimal tier, added by the A-04 gap sweep at
  #33's approval. This is the contrast with #45, which had to file its own placeholder — worth naming,
  because "no #16 row" reads as an oversight in a wave where most specs file one.
- **#49's core seam — nothing to change structurally.** FR-LC-013/014 and §7.3 already specify the
  sibling-adapter extension point **by name**; the core `ILocalizer` / `TextTemplateId` /
  `LocalizedTextRequest` are untouched. This is the extensibility guarantee #49 was approved on, exercised
  for the first time. The **one** #49 change is ERR-049-001, a wording fix to a single requirement.
- **#22 — untouched.** #35 consumes neither `InteractionTextGenerator` nor `world.text`, so no cursor,
  corpus, or `WorldStore` surface changes. The plan's `living-world` dependency is gone entirely.
- **#46 — nothing imposed.** KD-6 makes it the **reader**, so #35's landing imposes no #46 change. The
  one shared item, ERR-033-003, is filed jointly rather than by #35 on #46's behalf.
- **#45 — nothing at approval.** Its `BoardDayInput` already carries a deep-tier `MoraleSignalPermille`
  neutral at minimal, so #35's deep-tier signal fits an existing shape and is deferred (§8.3).

## 8.5 References

#35 introduces **no external citation**. Its content is a lifecycle and routing model composed from this
project's own approved specs; there is no published result it rests on, and inventing a citation to
decorate the section would be the fabrication the project's rules forbid. The §8.1 typed cross-references
are the authorities, and every one names a file and a requirement that can be re-checked.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §8 (XC-035-001..022, the §8.0 prerequisite, the approval-time back-props, the deferred set, the not-a-back-prop list, and the no-external-citation rationale) from supplement v0.7. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fix. **M:** the supplement's proposed **`ERR-030-012`** (the tick-order prerequisite) and **`ERR-030-013`** (the two media seams) were **both already filed** — by #30's own T2 *implementation* on July 26, the same day the supplement was written: `-012` is the §4.5 cursor-stream contradiction, `-013` the §4.6 unimplementable outcome record, `-014` is ERR-030-014 itself. Verified against `spec-error-log.md`; reassigned to **`ERR-030-022`** and **`ERR-030-023`**. Filing against a used id is the collision class the project's numbering discipline exists to prevent, and it would have landed silently because nothing cross-checks a proposed id against the log. **L:** §8.4 gained the #45 row and the explicit note that "no #16 row" is a *contrast with #45*, not an oversight. |
#endregion
