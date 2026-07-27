# Match Presentation Depth #48 — Section 8: Cross-References & Back-Propagations

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-048-001 | match-engine's public observation surface | `BallView`, `AgentView(i)`, `AgentTeamId(i)`, `AgentIsGoalkeeper(i)`, `PossessingAgentId`, `HomeScore`, `AwayScore`, `MatchEnded` — all **value-type copies**. The **pinned set** #48 may read (T-MP-BOUND-008). |
| XC-048-002 | `MatchViewerTests`' observer-neutrality digest lock | A recorded run is byte-identical to an unobserved same-seed run. The established property #48's own neutrality lock **extends to full depth**. |
| XC-048-003 | `match-viewer` — `HtmlReplayExporter`, `LiveMatchFrame`, `LiveMatchStreamer` | The sibling assembly #48 composes beside; the exporter is what embeds rendered commentary (FR-MP-017). |
| XC-048-004 | `match-client-core` — `ILiveMatchMutations`, `ManagerCommandQueue` | **A genuine mutation surface in the same layer.** #48 must not reference it (FR-MP-004) — listed so the exclusion is deliberate rather than accidental. |
| XC-048-005 | #38 ERR-038-001 / the browser viewer's playback-only invariant | The precedent: a presentation surface that gains a mutation channel **stops being presentation** (the interactive-client AR-1 H-2 finding). |
| XC-048-006 | #37 **FR-AN-002** | #37 consumes *"exactly two deterministic taps: the read-only per-tick ledger tap … and the observational world-state sample."* **The tap #48 joins as a third consumer.** |
| XC-048-007 | #37 **FR-AN-021** | #37 *"MUST consume **live during the match** (there is no post-match ledger reader); it MUST NOT assume the serialized ledger bytes can be re-parsed."* **The constraint that forces live capture** (KD-2(i)). |
| XC-048-008 | `EventBus.SerializeLedger(Span<byte>)` | **Write-only** — the bytes go into the digest and nothing reads them back. The other half of XC-048-007. |
| XC-048-009 | `EventBus.OnTickBoundary` | A per-tick **lifecycle reset**, *not* a consumer hook — the reason the tap is a contract awaiting construction rather than an existing surface (§4.3). |
| XC-048-010 | #37 FR-AN-020 | #37 *"MUST hold no persistent state"* — the property #48 shares, for the same reason (FR-MP-032). |
| XC-048-011 | #37 FR-AN-015 / FR-AN-017 | The read-only view-model posture and #37's own observer-neutrality requirement — the class #48 belongs to. |
| XC-048-012 | #49 FR-LC-002 / 004 / 005 / 012 / 013 / 014 / 015 / 008a | The producer contract: no baked strings, `Render(in LocalizedTextRequest)`, the renderer draws from no stream, no sim-side reference to the localization assembly, a **sibling boundary adapter**, **disjoint slots**, the intent-value pre-gate, base-locale coverage. |
| XC-048-013 | #49 FR-LC-020 / #35's **ERR-049-001** | FR-LC-020 binds `SelectionDraw` to #22's `world.text` draw. #48 **inherits** the fix as the **third** spec blocked on it (#35, #46, #48) and files no duplicate (FR-MP-018). |
| XC-048-014 | #35 KD-2 / `FixtureScheduler` / `LeagueBootstrap` | The **local keyed SplitMix64** precedent: a `ulong` with no stream, no cursor and nothing serialized — what makes #48 draw-free (FR-MP-012). |
| XC-048-015 | #38 `IViewModelSource<T>` (`where T : struct`) | The hosting contract, and the constraint that forces `CommentaryFeedView` to be a **bounded by-value window** rather than a handle (FR-MP-029). |
| XC-048-016 | #38 **FR-UI-023** and its **F6** | During a live streamed match the engine is owned by the streamer's **tick thread**, and even *commands* must marshal. #48 applies the same discipline in the **read** direction (FR-MP-030). |
| XC-048-017 | #51 (Wave 8) / its KD-1 "stub bus API" option | #48 stops at the `CueId`; playback, mixer, buses and the catalogue are #51's. #48 declares `ICueSink`; the **shell** implements it, so **neither spec references the other** (FR-MP-024/025). |
| XC-048-018 | #16 §3.4 | **No row and no `_RESERVED_` placeholder for #48** — consistent with the `0x2A` row's note that read-only / presentation / infra specs take no tag. Nothing to file, and **nothing to promote later** (FR-MP-033). |

## 8.2 At approval — **none**

**#48 files no back-propagations at approval.** It is a pure consumer of surfaces that already exist or
are already specified: the observation surface, the #37-contract live tick tap, #38's view-model contract,
and #49's adapter extension point.

This is the same positive property **#37, #44 and #46** have, and it is worth stating explicitly rather
than leaving as an empty table — because a **presentation spec is exactly where *"just add a field to the
engine for rendering"* pressure lands** (KD-3 / R-1). Filing nothing is the evidence that #48 sits
correctly in the layer, and any future #48 change that *does* require a back-prop against match-engine
should be read as a signal to re-examine the derivability argument first (FR-MP-021).

## 8.3 Sequencing notes (not back-props)

Two coordination facts that need recording but change no approved text:

- **The shared tap is #37-specified and unbuilt.** There is no `src/match-analytics/` — #37 is approved
  and unimplemented — and `EventBus.OnTickBoundary` is a lifecycle reset rather than a consumer hook. So
  **whichever of #37 / #44 / #48 is implemented first builds the tap, to #37's contract, and the others
  join** (FR-MP-008). If #48 lands first, the tap it builds is **#37's surface, not a #48 one** —
  T-MP-BOUND-006 asserts that #48 registers a *consumer* rather than owning a tap, which is the mechanical
  form of the rule. **Nothing in #37's approved text needs to change for this to hold**, which is why it
  is a note rather than a back-prop.
- **`ERR-049-001` is now load-bearing for three specs.** #35 filed it; #46 and #48 inherit it. If #49's
  owner declines the wording fix, all three take the `SelectionDraw = 0` fallback — **most visibly for
  #48**, since repeated commentary lines are immediately noticeable (§7.4 R-4).

## 8.4 Deferred — land at the named tier

- **`ICueSink`'s real implementation**, when **#51** lands (T3) — a `CueSinkAdapter` in the **client
  shell**, which is a **sink change, not a #48 change** (FR-MP-024).
- **The 3D renderer**, when **Unity host access** exists (T3) — the standing OPEN ISSUE that also gates
  the interactive client. #48's contract is authorable and testable now (FR-MP-022).
- **The exported artifact's embedded commentary** (T3) — rendered at the boundary by the exporter
  (FR-MP-017).
- **Any additive read-only match-engine observation property**, **if** a future fidelity proves something
  genuinely underivable — under FR-MP-020/021's burden of proof: a stated derivability argument **and** an
  unchanged neutrality lock. Deliberately listed as *deferred* rather than *planned*, because the default
  answer is that no such property is needed.

## 8.5 Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **match-engine — nothing.** No new field, no new serialized value, no new event (FR-MP-019/020). This
  is the absence that matters most: it is the one a presentation spec is under constant pressure to break,
  and R-1 names it as the single most likely way #48 damages the project.
- **#37 — nothing.** The tap is #37's contract and #48 joins it; the sequencing is a note (§8.3), not a
  change. #37 is also a read-only *source* for stat-driven lines, on the same tap.
- **#49 — nothing.** #48 adds a **sibling adapter**, which is the documented extension point (FR-LC-013),
  and **inherits** #35's `ERR-049-001` rather than filing a duplicate. #49's core `ILocalizer` /
  `TextTemplateId` / `LocalizedTextRequest` are untouched.
- **#51 — nothing, in either direction.** #48 declares `ICueSink` and the shell adapts it, so a Wave-8
  spec does **not** acquire a Wave-7 dependency (FR-MP-025).
- **#38 — nothing imposed.** #48 exposes view models through a contract #38 already defines
  (`IViewModelSource<T>`), and inherits FR-UI-023's threading discipline rather than extending it.
- **#22 — untouched.** #48 consumes neither `InteractionTextGenerator` nor `world.text`, superseding the
  plan's original framing.
- **#16 — no row, no `_RESERVED_` placeholder, nothing at all.** #48 is presentation/infra: no stream, no
  tag, no ordinal, and the selection value is **local arithmetic** rather than a draw. As with #46, that
  also means #48 has **nothing to promote later** — a future stochastic presentation surface would need a
  **fresh allocation**.
- **#50 — nothing.** #48 adds no format version and no save sub-blob, so there is no registry row to
  claim.

## 8.6 References

#48 introduces **no external citation**. Its content is a mapping, a capture discipline, and a set of
boundaries composed from this project's own approved specs and shipped source; there is no published
result it rests on, and inventing a citation to decorate the section would be the fabrication the
project's rules forbid.

Note in particular that **the commentary corpus is not a citation surface** and is not #48's at all: the
lines themselves are **#49's catalogue rows**, authored by production (§1.2). #48 supplies the roster they
must cover (FR-MP-015). Tabulating example lines here would both create a second definition and put a
baked string in a sim-adjacent spec — the thing FR-LC-002 exists to prevent.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §8 (XC-048-001..018; **§8.2 records that there are no approval-time back-props at all**, stated as a positive property rather than left as an empty table, since filing nothing is the evidence #48 sits correctly in the layer; §8.3 separates the two *sequencing notes* — the unbuilt shared tap and the three-way `ERR-049-001` dependency — from back-props, because neither changes approved text; the not-a-back-prop list leads with match-engine, the absence under the most pressure; and the no-external-citation rationale extends to record that the commentary corpus is #49's, not #48's). Status IN REVIEW. |
#endregion
