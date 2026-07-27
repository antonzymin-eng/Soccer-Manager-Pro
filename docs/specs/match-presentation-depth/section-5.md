# Match Presentation Depth #48 — Section 5: Test Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

Test-ID prefixes follow #19 §3.1.4: `T-MP-U-*` unit, `T-MP-I-*` integration, `T-MP-DET-*` determinism,
`T-MP-ID-*` identity, `T-MP-LOC-*` localization compliance, `T-MP-FAIL-*` fail-loud, `T-MP-BOUND-*`
structural.

Every value asserted below is **hand-derivable from §3.7** or is a relational property. **No test pins a
`SelectionMix` output** — that would be a fabricated hash; the mix is asserted relationally (§5.6).

## 5.1 The unconditional-neutrality lock (KD-7)

| ID | Test |
|---|---|
| T-MP-ID-001 | **The headline lock, and the claim that distinguishes #48 from every sibling.** A match run with **commentary, animation and audio cue mapping all ENABLED** produces a digest chain **byte-identical** to an unobserved same-seed run — the `MatchViewerTests` digest lock extended. Note this is an **unconditional** assertion: the plan expected it to be *conditioned on* the commentary decision, and KD-2(ii)'s draw-free design removes the condition. |
| T-MP-ID-002 | The complementary half: with **all depth disabled**, the pipeline is exactly today's viewer, no recorder is constructed, no consumer is registered on the tap, and the digest chain is byte-identical. |
| T-MP-ID-003 | **No RNG stream is registered and no cursor moves** (FR-MP-012/033): a full match at full depth leaves **every** registered stream's cursor byte-identical. The mechanical form of "the selection value is a mix, not a draw". |
| T-MP-ID-004 | **Nothing is serialized.** No #48 byte reaches any save; the season save frame is byte-identical with #48 present at full depth. |

## 5.2 Layer-taxonomy locks (KD-1)

| ID | Test |
|---|---|
| T-MP-BOUND-001 | **No sim assembly references #48** — the mechanical `.asmdef` reverse-reference scan #38's T0 ships (FR-UI-001), extended to #48's assembly. |
| T-MP-BOUND-002 | **#48 does not reference `match-client-core`** (FR-MP-004). Asserted **specifically and by name**, not as a corollary of the scan above: that assembly is in the **same layer**, its `ILiveMatchMutations` is a real write path, and the reference is one line away. **The single most important structural assertion in this spec.** |
| T-MP-BOUND-003 | #48 references neither `TacticalDirector.Localization` (FR-LC-012), `living-world`, `#51`, `SeasonSave`, nor any management spec. |
| T-MP-BOUND-004 | **#51 does not reference #48** — the symmetric half of FR-MP-025, asserted from #51's side so a Wave-8 spec cannot acquire a Wave-7 dependency. |
| T-MP-BOUND-005 | **No foreign writes:** a `MatchEngine` and its observation surface handed alongside every #48 entry point are **field-unchanged** after capture, derivation, mapping and view-model projection. Asserted behaviourally — match-engine's types are reachable, so the reference graph cannot prove this (§4.7 standing item). |
| T-MP-BOUND-006 | **#48 builds no second tap** (FR-MP-007 / F6) — asserted over the compiled surface: exactly one registration path exists, and it is a consumer registration rather than a tap construction. |
| T-MP-BOUND-007 | **#48 declares no type named `InteractionSlots`, `MediaSlots`, `InboxSlots`, or `TextTemplateId`** — the parallel-surface lock, and the FR-LC-014 disjoint-slots contract made mechanical. |

## 5.3 The no-new-engine-field lock (KD-3)

| ID | Test |
|---|---|
| T-MP-BOUND-008 | **#48 references exactly the enumerated observation properties** — `BallView`, `AgentView(i)`, `AgentTeamId(i)`, `AgentIsGoalkeeper(i)`, `PossessingAgentId`, `HomeScore`, `AwayScore`, `MatchEnded` — and no other match-engine member. Asserted as a **pinned set**, so an addition is **visible in a diff** rather than arriving quietly. This is the mechanical form of FR-MP-021's burden of proof. |
| T-MP-U-001 | §3.7(k): **a stationary agent's facing is derived, not read.** With no engine facing field, a stopped agent holds the last movement direction from the position history — the example that most looks like it needs a sim-side fact and does not. |
| T-MP-U-002 | Gait classification is a pure function of the derived speed, and the animation state machine's output is a pure function of `(previous state, gait, facing, tick)` — no hidden input, no sim read beyond the pinned set. |
| T-MP-U-003 | **The whole animation path runs without a Unity host** (FR-MP-022): derivation, state machine and view model are exercised headlessly. Only the renderer is host-gated. |

## 5.4 Commentary determinism and both replay paths (KD-2)

| ID | Test |
|---|---|
| T-MP-DET-001 | **Transcript determinism**: the same seed yields an identical `{tick, intent, slots}` sequence, across runs and across processes. |
| T-MP-DET-002 | §3.7(c)/(f): the selection mix is **position-independent and pure** — the same `(tick, intent, subject)` yields the same `ulong` regardless of how many selections preceded it. Asserted **relationally**; no literal output is pinned. |
| T-MP-DET-003 | §3.7(d)/(e): **`tick` is load-bearing in the key** (FR-MP-013). The same intent for the same agent at **different ticks** selects **different** variants across a swept range. **The counterexample matters more than the property:** with `tick` dropped, every occurrence picks one variant for the whole match — the most visible possible regression in a commentary system, and one that **passes every determinism test**. This test is what catches it. |
| T-MP-DET-004 | §3.7(g): the subject-less case (`MP_NO_SUBJECT`) maps to the mix's **neutral** input and cannot collide with a maximal agent id (the `+1` shift). |
| T-MP-I-001 | **In-session replay equivalence**: a scrub after the match yields exactly the lines the live run produced, read from the transcript. |
| T-MP-I-002 | **Export equivalence — the path that can actually regress.** An exported HTML artifact contains **exactly** the lines the live run produced. The in-session case is near-tautological (it reads the same memory); the export must **carry what it cannot re-derive** (FR-MP-010/017), so a missing embed is silent — the file simply has none (F5). |
| T-MP-I-003 | §3.7(o): an export opened under a **different UI locale** shows the **export's** locale — text was baked at export time, which is correct for a display artifact and involves no sim state. |
| T-MP-I-004 | **An empty transcript is handled everywhere** (§2.3): a match with no commentary-worthy event yields an empty window, an export with no lines, and no exception. |

## 5.5 The thread boundary (KD-5)

| ID | Test |
|---|---|
| T-MP-I-005 | §3.7(l): **the window is a value copy.** Appending to the transcript **after** a window is taken does **not** change the window. Asserted behaviourally rather than trusted to the type — `where T : struct` prevents a *field* alias, not a *boundary* mistake (F4). |
| T-MP-I-006 | **A live streamed match renders correctly while the tick thread appends**: the recorder writes on the streamer's tick thread and #38 reads on the UI thread, with no torn or duplicated line. The interactive-viewer `Start()` / `Stop()` race is the precedent for what an implicit lifecycle boundary costs. |
| T-MP-I-007 | The window is bounded at `COMMENTARY_WINDOW_LINES` and carries the **most recent** entries, with the full transcript remaining inside #48 (FR-MP-029). |

## 5.6 Localization compliance (#49)

| ID | Test |
|---|---|
| T-MP-LOC-001 | **FR-LC-008a coverage** over the full `CommentaryIntent` roster: every defined intent has a base-locale template row (FR-MP-015). |
| T-MP-LOC-002 | **`CommentaryIntent` ordinal stability** (FR-MP-014). A **save-correctness-class** lock even though #48 saves nothing: the ordinal is the #49 catalogue key **and** is embedded in exported artifacts, so a reorder re-points every catalogue row **and** mis-labels every existing export — neither with a version gate. |
| T-MP-LOC-003 | **`CueId` ordinal stability** (FR-MP-027) — #51's catalogue will be keyed on it. Asserted separately, because the two enums fail for different reasons and checking one proves nothing about the other. |
| T-MP-LOC-004 | **#48 emits no display string** (FR-MP-016): a source-level assertion over `src/match-presentation/` finds no `string` field, no `string` return, and no string formatting. The export's rendered text is baked by the **boundary**, not here. |
| T-MP-LOC-005 | The FR-LC-015 **value gate** refuses `CommentaryIntent.None` and undefined ordinals **before** any selection work, and holds through **any** #48 surface — not only through `MatchTextBoundary`, which any other consumer would bypass. |

## 5.7 Fail-loud (§2.3)

| ID | Test |
|---|---|
| T-MP-FAIL-001 | §3.7(h): `CommentaryIntent.None` or an undefined ordinal at the render path ⇒ **throws** (F1), before any selection work. |
| T-MP-FAIL-002 | §3.7(i): an undefined `CueId` ⇒ **throws at the mapper** (F2) — asserted **with the no-op default sink installed**, because that is the default configuration and a sink-side guard would be silently absent in it. |
| T-MP-FAIL-003 | §3.7(j): a non-finite position or an out-of-range agent id from the observation surface ⇒ **throws** (F3), and is specifically **not sanitised** — sanitising would hide a sim defect behind presentation. |
| T-MP-FAIL-004 | A malformed tap record ⇒ throws (F3). |
| T-MP-FAIL-005 | **The no-op `ICueSink` changes nothing observable** (FR-MP-026 / §2.3): a full match with the default sink produces identical state, an identical transcript and an identical digest to one with cue mapping disabled. |

## 5.8 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `presentation-depth-is-observer-neutral`, owning specs
`{16, 17, 19, 37, 38, 48, 49}`, registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`:

run a full match **twice from one seed** — once unobserved, once with commentary, animation and cue
mapping **all enabled** — and assert the two digest chains are **byte-identical**; assert the transcript
from the observed run is deterministic across a third run; scrub in-session and assert the lines match;
**export the artifact and assert it contains exactly those lines**; and assert every registered RNG
cursor is unchanged.

This is the composition-level proof that KD-1's observation-only posture, KD-2's draw-free capture, and
KD-7's *"neutral when on"* claim hold **together** at full depth — which is the only configuration that
matters, since neutrality when the feature is switched off proves nothing about the feature.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §5. §5.1 leads with the unconditional-neutrality lock **at full depth**, since neutrality with the feature disabled proves nothing. T-MP-BOUND-002 is called out as the single most important structural assertion (the `match-client-core` write path is one line away and in the same layer); T-MP-BOUND-008 pins the *current* observation-property set so a KD-3 violation is visible in a diff; T-MP-DET-003 is written around its counterexample, because dropping `tick` from the key is a maximally visible regression that passes every other determinism test. Status IN REVIEW. |
#endregion
