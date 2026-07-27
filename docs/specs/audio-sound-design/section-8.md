# Audio & Sound Design #51 — Section 8: Cross-References & Back-Propagations

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-051-001 | #48 **KD-4** — *"#48 emits cue ids into a seam with a trivial default sink — not into a direct playback call"*, *"chosen deliberately over 'direct playback'"* | **The plan's largest risk, already void.** #51's arrival is a **sink implementation**, not a rehoming of anything. |
| XC-051-002 | #48 **KD-4** / **FR-MP-025** — *"#51 does not implement `ICueSink`; the composition root does"* | The constraint #51 inherits and must not quietly break (FR-AU-002). |
| XC-051-003 | #48 **KD-4** / **FR-MP-027** — *"#51's catalogue will be keyed on it [`CueId`]"* | **The contradiction** (§1.4(c)): it requires exactly the `#51 → #48` reference XC-051-002 forbids. Filed as **ERR-048-001**. |
| XC-051-004 | #48 `ICueSink` + its **no-op default** (FR-MP-026) | The seam the shell's adapter implements, and the reason a headless or pre-#51 run stays valid forever. |
| XC-051-005 | #48 `CueParams` | **A different type from #51's `CueParams` by design** — the shell adapter translates, and is the one file that must fully qualify both (§4.2). |
| XC-051-006 | `MatchViewerTests`' observer-neutrality digest lock | The built precedent #51 **inherits** rather than invents (§1.4(d)); T-AU-ID-001 extends it. |
| XC-051-007 | #48's unconditional neutrality lock (FR-MP-034) | The stronger form — asserted with the feature **enabled** — that #51 matches rather than weakens. |
| XC-051-008 | #49 **KD-6** — a producer *"emits only types it already owns"* | Why `CaptionId` is **#51-owned** and why #51 holds no #49 type (FR-AU-025 / §1.4(f)). |
| XC-051-009 | #49 **KD-6** — *"the renderer references each **built** producer … never speculatively"* | Why **`#49 → #51`** is the approved design executing rather than a change to it (§4.5 / §8.4). |
| XC-051-010 | #49 **FR-LC-002 / 012 / 008a** | No baked strings; no sim-side reference to the localization assembly; base-locale coverage over #51's `CaptionId` roster. |
| XC-051-011 | #49 **FR-LC-018** | The client-local settings store's first claimant — locale + a11y options (§1.4(e)). |
| XC-051-012 | #38 `ui-client-framework/section-4.md` + `section-6.md` | *"UI preferences/layout are client-local settings outside it"* — the second claimant, and the proposed **owner** (ERR-038-004). |
| XC-051-013 | #38's navigation / mix context | The **permitted** presentation-state read (FR-AU-016), and the reason KD-2's prohibition is scoped to **sim** state rather than to all state. |
| XC-051-014 | #48 §4.6 — *"commentary on/off, **audio levels**, animation quality"* | An approved spec **already claims audio levels** for the shared store, so a private #51 file would fork state two specs believe they describe. |
| XC-051-015 | #39 supplement §5 | The fifth claimant — achievement progress + Cloud sync state. |
| XC-051-016 | #50 KD-3 / FR-MG-021 | Audio settings are **outside migration scope**; the reset-to-defaults policy is the deliberate **inverse** of #50's refusal, matched to the stakes. |
| XC-051-017 | #16 §3.4 | **No row and no `_RESERVED_` placeholder for #51** — the presentation-and-infra class (#37 / #44 / #46 / #48 / #50). Nothing to file, and **nothing to promote later** (FR-AU-032). |

## 8.2 At approval

| ID | Target | Change |
|---|---|---|
| **ERR-048-001** | #48 (`match-presentation-depth`) — **KD-4**, **FR-MP-027**, and the `CueId` declaration comment | Correct *"#51's catalogue will be keyed on it"*. It **cannot be**, without the `#51 → #48` reference the same key decision forbids (§1.4(c)). Restate as: `CueId` is #48's **semantic event identity**; **#51's catalogue is keyed on its own `CueKey`**; the **shell's `ICueSink` adapter holds the mapping**. `CueId`'s APPEND-only ordinal stability is **retained, with its rationale strengthened** — the shell's table is keyed on it, so a renumber silently re-points cues. **Text-only: no #48 code, contract or test changes.** |
| **ERR-038-004** | #38 (`ui-client-framework`) | Assign ownership of **one** client-local settings store — file location, schema-fragment registration, and the **reset-to-defaults** failure policy — to the client framework, with #49 (locale + a11y), #48 (presentation), #51 (audio) and #39 (achievements / Cloud state) contributing fragments. Today **five specs name this store and none owns it** (§1.4(e)), so each is one implementation decision away from writing its own file. #38 is the natural owner: it is the client framework, it already holds UI preferences, and it is the only candidate every contributor already composes with. |

**Both ids were verified free against `spec-error-log.md` rather than assumed** — `ERR-048-*` is entirely
unfiled and unproposed, and `ERR-038-001..003` are filed so `-004` is genuinely next. Recorded explicitly
because three specs in this same wave proposed ids that had **already been filed** (§9.4.1).

**ERR-048-001 is a text correction with no code consequence, which makes it the back-prop most likely to
be deferred — and deferring it is the expensive option** (R-6). The cost is not a stale sentence: it is
that the next person to implement either spec reads FR-MP-027, builds the `#51 → #48` reference in good
faith, and discovers it as an assembly cycle after both specs are APPROVED.

## 8.3 Deferred — land at the named tier

- **The cue identity set and the shell's mapping rows** — content, landing with #48's mapper and the first
  audio assets (T2), under the build-time completeness check.
- **`#49 → #51`'s actual assembly reference**, when captions land (T3) — #49's approved design executing
  (§8.4).
- **Data-driven bus routing** (S3+), only if a real mix demands it, and knowingly trading away KD-2's
  by-construction completeness property.
- **Commentary-audio delivery** (S3+), alongside #48's deep tier.

## 8.4 Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **#49 — nothing.** #51 is an ordinary caption producer through the existing boundary, and the
  **`#49 → #51` reference captions require is not a back-prop**: #49's KD-6 already specifies that the
  renderer gains a reference to each producer *as it is built*. That is the approved design executing, not
  being amended — no requirement changes, no catalogue change, no text change. (`ERR-049-001` is already
  proposed by #35 and inherited by #46 / #48; **#51 adds nothing to it**, because #51 makes no
  `SelectionDraw`-style variant selection through #49.)
- **#16 — no row, no `_RESERVED_` placeholder, nothing at all.** No stream, no tag, no ordinal; cue
  variation is **display-side** (FR-AU-033). As with #46, #48 and #50, that also means #51 has **nothing
  to promote later** — a future stochastic audio surface would need a **fresh** allocation and should not
  read #51's silence as a claim on one.
- **#50 — nothing.** Audio settings are outside migration scope by construction (FR-AU-021): an unreadable
  fragment is already *defined* as "use the defaults", so there is nothing to migrate.
- **The simulation, in any form.** #51's whole design is that it is unreachable from it, and unable to
  reach it (FR-AU-034/035). This is the absence that matters most, and it is the one #51 can prove from
  its reference graph rather than only assert.
- **#37 / #44 — nothing.** #51 consumes no tap and observes no match; it hears about events only through
  #48's already-mapped cue ids.
- **Beyond ERR-038-004, #38 — nothing.** #51 reads the navigation/mix context through whatever surface #38
  already exposes and imposes no new one.

## 8.5 References

#51 introduces **no external citation**. Its content is a taxonomy, a catalogue schema, a routing rule and
a set of boundaries composed from this project's own approved specs and shipped source; there is no
published result it rests on, and inventing a citation to decorate the section would be the fabrication
the project's rules forbid.

**The audio content is not a citation surface and is not #51's** (R-1). The assets, the mix and the
"feel" are production's; tabulating example sounds here would both create a second definition and put
content into a spec whose whole claim is that it specifies identities and routing rather than material.

**Accessibility is the one place a reader might expect an external standard**, and its absence is
deliberate rather than an oversight: #51's caption obligation is a **construction-time contract**
(FR-AU-023/024), not a conformance claim against a published guideline. If a specific accessibility
standard is later adopted as a project-level requirement, it belongs to #49 — which owns rendering and
already owns the a11y options — and #51's contract would be the mechanism that satisfies it, not the
document that cites it.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §8 (XC-051-001..017, with 001/002/003 laid out as the closed risk, the inherited constraint and **the contradiction between them**; two approval-time back-props, **both ids verified free rather than assumed**, and ERR-048-001 flagged as the one most likely to be deferred precisely because it changes no code; §8.4 distinguishes an **anticipated** reference (`#49 → #51`, its approved design executing) from a back-prop (amending it), and leads the list with the simulation, which is the absence #51 can prove from its reference graph rather than only assert; §8.5 records why accessibility carries no external citation and where one would belong if adopted). Status IN REVIEW. |
#endregion
