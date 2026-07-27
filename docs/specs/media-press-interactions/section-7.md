# Media & Press Interactions #35 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + `MediaIntent` (with its band boundary) + the value types + `MediaCatalogue` + `MediaStore` + the pure lifecycle functions (§3.1–§3.4) + `SelectionValue` (§3.5), and their tests. Nothing wired into #30, no adapter. | **Inert** — no caller exists |
| **T1** | `MediaSaveCodec` + the round-trip / fail-loud / ordinal-stability suite. Still not composed into the season save. | **Inert** |
| **T2** | **First non-inert phase.** Wire **all three** #30 touch points (queue, expiry slot, **drain at step 3**); compose the sub-blob (bumps `SEASON_SAVE_FORMAT_VERSION`); add `MediaTextBoundary` in the boundary layer and the base-locale catalogue rows. | **Live.** Identity-preserving only while every consequence is `0` — which is a **catalogue** property, not a code one (§7.4 R-1) |
| **T3** | The deep tier: multi-archetype selection as a genuine keyed draw (**promotes `_RESERVED_0x27_`**), the #33 morale **read** for mood-aware phrasing, multi-target consequence spread, and the #45 board-facing routed value. | **Named activation** — the first stochastic surface #35 owns |

**T2 is not automatically behaviour-neutral, and that is the one place #35 differs from its siblings.**
For #53 and #45, the minimal tier's neutrality is a property of the *code* (baseline levels, identity
dials). For #35 it is a property of the **authored catalogue**: the moment a base-locale row carries a
non-zero consequence, morale moves. That is the feature working as intended, but it means "T2 is
identity-preserving" is a statement about the shipped data, and §5's identity tests must be run against
the shipped catalogue rather than against a synthetic one.

**Wiring only two of the three touch points is the predicted T2 failure**, and it is silent: deltas would
be recorded and never delivered, with every #35-local test green (§4.4).

## 7.2 Deep-tier extensions (designed for, not built)

- **Multi-archetype selection** (§3.6) — context-, rivalry- or form-aware question choice as a genuine
  keyed draw. This is #35's **only** stochastic surface and the thing that promotes `0x27`. One stream,
  keyed ordinals, no persisted cursor (FR-ME-019).
- **Mood-aware phrasing via a #33 morale read** — FR-HS-024 already anticipates #35 as a read-only morale
  consumer. **When it lands it arrives as routed committed values, not an assembly reference** (§4.1), so
  the structural assertions in §5.8 stay unconditional.
- **Multi-target consequence spread** — subject, squad, board — as **entries on the existing list**
  (KD-8), never new code paths. This is where a subject-less conference acquires a consequence.
- **The #45 board-facing signal** — a routed value into `BoardDayInput`, which already carries a
  deep-tier `MoraleSignalPermille` neutral at minimal: the same shape, so #45 needs no new mechanism.
- **Player-initiated media** (a manager *choosing* to make a statement, rather than answering) — a second
  queue source with the same record shape and the same consequence path. Additive; no new state type.
- **Rivalry and history context in slots** — extra native integers in `MediaSlots`, which widens the
  catalogue's row set but touches no #35 logic.

## 7.3 Explicitly not planned

- **A morale write.** #35 will never write #33 state, at any tier (FR-ME-004). This is barred by an
  approved MUST, and the routed-value path is the only compliant shape. A future maintainer adding a
  direct write would pass every #35 test while breaking #33's single-writer contract — §5.8's structural
  assertion is what actually catches it.
- **A reputation scalar.** Not at any tier #35 owns (KD-4). If one is wanted it is its own spec, or
  **#45's** — which already owns a club-scoped persistent relationship scalar and the drift machinery
  for one.
- **An inbox, an unread flag, or a push notification.** #46 pulls (KD-6). Adding a push would open the
  reverse edge and make #35 depend on a spec authored after it; T-ME-BOUND-005 locks it shut.
- **A citation clause.** `HasCitedEpisode` stays `false` and the field stays **absent** from `MediaSlots`
  (FR-ME-015) — a present-but-false field is what a later maintainer flips. Enabling it requires #22's
  `MemoryStore` and must re-argue that dependency explicitly.
- **A baked display string, anywhere, for any reason** — including a "debug" or "fallback" string
  (FR-ME-005 / FR-LC-002). The fallback is #49's to provide, not #35's to bake.
- **Media reaching the match engine.** No #35 value reaches the 10 Hz/60 Hz loops, and #35 feeds no
  digest.

## 7.4 Risks carried

- **R-1 — T2's neutrality is a catalogue property, not a code property** (§7.1). The identity tests pass
  or fail on the shipped base-locale rows, so a designer adding a non-zero consequence is *changing
  behaviour* even though no code changed. Stated here because the usual reading of "behaviour-neutral
  T2" is a code guarantee, and for #35 it is not.
- **R-2 — the §8.0 prerequisite is not #35's to decide.** If #30's tick-order repair lands with different
  numbers than proposed, #35's cited step must follow it. Re-verify at promotion.
- **R-3 — ERR-049-001 is a change to a spec approved days earlier.** #49's owner may reasonably prefer to
  keep FR-LC-020 as written; FR-ME-018 records the `SelectionDraw = 0` fallback so #35 does not stall on
  the answer. Either way the *contradiction* is real and outlives #35 — the next producer (#46, this same
  wave) hits it identically.
- **R-4 — "media should just write morale" is the recurring temptation**, and it is the one a future
  maintainer will act on first, because the routed path looks like indirection for its own sake. It is
  barred by FR-HS-002/024. §5.8's reference assertion is the mechanical defence; this row is the
  documentary one.
- **R-5 — the reputation vacuum will be filled by someone.** #36 (national-team selection) and #42
  (intake quality) both want one, and #54 proposes one. KD-4 keeps #35 out of that decision. Standing
  option, not a debt — but whoever needs it first should own it, and it should not be #35.
- **R-6 — deep-tier consequence-spread creep.** Bounded by `MEDIA_MAX_CONSEQUENCE_TARGETS`, enforced at
  the authoring boundary rather than by review — because review is exactly what fails when a catalogue
  grows one row at a time.
- **R-7 — the shared `ExternalDeltaPermille` field has a cost #35 imported knowingly.** A per-producer
  field would have made over-contribution impossible; a shared field makes the root's post-sum clamp
  load-bearing (§4.4). The trade was made because producer #3 would otherwise need a third field on an
  approved struct, but the clamp is now a correctness dependency outside #35, and T-ME-I-010 is what
  keeps it honest.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §7 (T0–T3 with T2's neutrality identified as a **catalogue** rather than a code property — the one place #35 differs from its siblings — deep-tier extensions, the not-planned list incl. the absent-not-false citation field, risks R-1..R-7 with the shared-field cost recorded as R-7). Status IN REVIEW. |
#endregion
