# Match Presentation Depth #48 — Section 9: Approval Checklist

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — G1 CLOSED; PASS-1 + AR-2 recorded)
**Version:** 0.2
**Status:** IN REVIEW

---

## 9.1 Content completeness

- [x] §1 scope with the **content-vs-trigger** distinction in the out-of-scope table / dependencies + the
      doubly-inverted DAG / **§1.4's verification findings** / KD-1..KD-7 / determinism posture.
- [x] §2 FR-MP-001..034, data structures, failure modes F1..F7, and the two *"not a failure mode"* notes.
- [x] §3 FM-MP-01..04 with the live capture on the shared tap, the keyed selection mix **and its
      counterexample**, the animation derivation, cue mapping, the window snapshot and thread boundary,
      the §3.6 layering-not-numeric rule, and fifteen worked examples.
- [x] §4 assembly with the `match-client-core` trap, the CS0104 pre-check, file layout with three
      deliberate absences, the shared tap as a joined #37 contract, the #38 hosting split, the two
      identically-inverted seams, state and persistence, neighbour contracts.
- [x] §5 test plan led by the **unconditional-neutrality lock at full depth**, then layer-taxonomy /
      no-new-engine-field / commentary determinism and both replay paths / thread boundary /
      localization / fail-loud + the T-phase closed-loop scenario.
- [x] §6 loop classification (**the only wave spec with per-tick work**), cost profile, `[GT]` ceilings
      with the tick-thread one flagged as the only consequential budget, memory.
- [x] §7 T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-6.
- [x] §8 XC-048-001..018, **the empty back-prop table stated as a positive property**, the sequencing
      notes, the deferred set, the not-a-back-prop list.
- [x] Appendices A (constants), B (the intent / cue rosters), C (the pinned observation-surface
      inventory).

## 9.2 Constant-tag discipline

- [x] Every constant in Appendix A carries exactly **one** of `[FIXED]` / `[DERIVED]` / `[CROSS]` /
      `[CROSS-PENDING]` / `[GT]`.
- [x] No `[EST]` remains (none was introduced).
- [x] Empty regions omitted (#20 prohibits them) — #48 has **no `[CROSS-PENDING]` constants at all**,
      because it takes no determinism reservation (KD-6), so that region does not appear.
- [x] `[CROSS]` rows name their authority and are consumed read-only — #48 re-declares none of #49's,
      #37's or match-engine's types (T-MP-BOUND-007).
- [x] `[DERIVED]` rows document their formula and are never set independently.
- [x] **No `[GT]` constant affects the simulation.** Every #48 tunable is a presentation threshold or a
      budget ceiling; §5.1 asserts that none of them can move a digest byte (T-MP-ID-001).
- [x] The `[GT]` magnitudes are declared **illustrative pending the T3 balance pass**, and §5 asserts only
      shape, determinism and neutrality — never magnitude.

## 9.3 Verification of load-bearing claims (checked against source, not asserted)

- [x] The public observation surface exposes `BallView` / `AgentView(i)` / `AgentTeamId(i)` /
      `AgentIsGoalkeeper(i)` / `PossessingAgentId` / `HomeScore` / `AwayScore` / `MatchEnded`, returning
      **value-type copies**. The pinned set of §5.3. *(`src/match-engine/MatchEngine.cs`)*
- [x] `MatchViewerTests` **digest-locks observer neutrality** — a recorded run is byte-identical to an
      unobserved same-seed run. The established property #48 extends to full depth.
      *(`src/match-viewer/`)*
- [x] `src/` carries `match-viewer`, `match-client-core`, `match-client-unity` and `ui-framework`, with
      references running **one way** and nothing sim-side referencing any of them — so #48 is a
      **sibling**, not a new tier.
- [x] **`match-client-core` carries `ILiveMatchMutations` and `ManagerCommandQueue`** — a genuine
      mutation surface **in the same layer**. The fact FR-MP-004 exists for, and the assertion §5.2 makes
      by name.
- [x] #38's ERR-038-001 records the browser viewer's **playback-only invariant** and the
      interactive-client AR-1 H-2 finding — the precedent for why a presentation surface must not gain a
      mutation channel.
- [x] #37 **FR-AN-021**: *"MUST consume **live during the match** (there is no post-match ledger reader);
      it MUST NOT assume the serialized ledger bytes can be re-parsed."* **The constraint that forces live
      capture.** *(`match-analytics-statistics/section-2.md`)*
- [x] **`EventBus.SerializeLedger` is write-only** — the bytes feed the digest and nothing reads them
      back. *(`src/event-system/`)*
- [x] **There is no `src/match-analytics/`**, and `EventBus.OnTickBoundary` is a per-tick **lifecycle
      reset**, not a consumer hook — so the live tap is a **#37-owned contract awaiting construction**,
      and the root `CLAUDE.md` records *"one tap feeds #37+#44"*. #48 makes it three.
- [x] #37 **FR-AN-002** defines the two deterministic taps; **FR-AN-020** requires #37 to hold **no
      persistent state** — the property #48 shares.
- [x] #49 FR-LC-002 / 004 / 005 / 012 / 013 / 014 / 015 / 008a define the producer contract, and **§7.3
      names the sibling-adapter extension point**. #48 is #49's **fourth** producer.
- [x] #49 **FR-LC-020** binds `SelectionDraw` to #22's `world.text` draw — the defect #35's
      **`ERR-049-001`** fixes, which #48 **inherits** as the third dependent spec.
- [x] **`SplitMix64` is not a shared public primitive** in `deterministic-sim` — `FixtureScheduler` and
      `LeagueBootstrap` each carry a local copy, which is the precedent for #48's local mix.
- [x] #38's **`IViewModelSource<T>` is `where T : struct`** — the constraint that forces
      `CommentaryFeedView` to be a bounded by-value window rather than a handle.
      *(`ui-client-framework/section-3.md`)*
- [x] #38 **FR-UI-023** and its **F6** pin the engine to the streamer's **tick thread** during a live
      match, with commands marshalled rather than applied cross-thread — the discipline #48 applies in the
      **read** direction.
- [x] The mutable-handle defect class is real and recurrent in this project — `SquadPositionCounts`,
      `MatchReplay`'s frame list, `TacticPreset.Players` — which is why the window is by value.
- [x] The HTML replay's output is explicitly *"NOT a determinism-pinned wire format"* — the contract that
      makes baking the export's locale into the artifact correct rather than an FR-LC-006 problem.
- [x] **#16 §3.4 has no row and no `_RESERVED_` placeholder for #48**, consistent with its `0x2A` row's
      note that read-only / presentation / infra specs take no tag. Nothing to file, **nothing to promote**.
- [x] `FR-MP-*` is **unclaimed** — verified by enumerating every `FR-[A-Z]{2,3}-` prefix in `docs/specs/`.
- [x] **No `ERR-*` id is proposed by #48** — there are no approval-time back-props (§8.2), so there is no
      id to verify free. Recorded because every sibling in this wave *does* propose one.

## 9.4 Gates

| Gate | Owner | Status |
|---|---|---|
| **G1** — section-file PASS-1 adversarial review + a fix pass, to convergence. | drafter | ✅ **CLOSED** — see §9.4.1 |
| **G2** — **no back-props to file.** | — | ✅ **N/A** — see §8.2 |
| **G3** — lead-developer R-01..R-05 sign-off. | lead developer | ⏳ **OPEN** — a human authority, not self-grantable |
| **G4** — `SPEC_INDEX.md` registry row + Registry-Changes entry, added at promotion. | drafter | ⏳ **OPEN** |

**Not gating (deferred by design, recorded so they are not mistaken for omissions):** `ICueSink`'s real
implementation (T3, when #51 lands — a **sink** change); the 3D renderer (T3, **Unity host access** —
the standing OPEN ISSUE); the exported artifact's embedded commentary (T3); and any additive match-engine
observation property, **if** one is ever proven necessary under FR-MP-021's burden of proof.

**G2 being N/A is unusual and is itself evidence.** Every sibling in this wave files at least one
back-prop; #48 files none, because it consumes only surfaces that already exist or are already specified.
A presentation spec is exactly where *"just add a field to the engine"* pressure lands, so **filing
nothing is the result to check, not a gap to fill** — and a future #48 change that *does* need a
match-engine back-prop should be read as a signal to re-examine the derivability argument first.

### 9.4.1 PASS-1 adversarial review record (G1)

**PASS-1: 0H + 4M + 6L, all resolved in the v0.2 fix pass.** The M findings cluster where a spec whose
central claim is *"we touch nothing"* is most likely to be wrong: the **guards** that make the claim
mechanical, and the **one input** whose omission would be invisible to every determinism test.

| # | Sev | Finding | Resolution |
|---|---|---|---|
| M-1 | M | **`tick`'s presence in the selection key was a parenthetical, not a requirement.** Drop it and every occurrence of one intent for one agent selects **the same variant for the whole match** — the same striker's every goal narrated in identical words. It is the most visible regression a commentary system can have, and it **passes every determinism test**, since the output is still a pure function of its inputs. | Promoted to **FR-MP-013**; §3.2 argues it with the counterexample; **T-MP-DET-003** sweeps different ticks and is written around the failure rather than the property. |
| M-2 | M | **The `CueId` validity guard had no stated home**, and the natural one is wrong: `ICueSink`'s default is a **no-op**, so a check living in the sink would be **silently absent in a headless or pre-#51 run** — which is the *default* configuration, not an edge case. | New **F2** with the guard pinned at the **mapper** (§3.4); **T-MP-FAIL-002** asserts it **with the no-op sink installed**. |
| M-3 | M | **Nothing said what #48 does with malformed observation input**, and both natural answers are wrong: render a nonsense frame, or **sanitise** it — which would hide a sim defect behind presentation, in the one layer that must never repair sim output. | New **F3**: fail loud at the boundary, explicitly **not** sanitised; **T-MP-FAIL-003** asserts both halves. |
| M-4 | M | **The no-new-engine-field rule had no mechanical form.** KD-3 stated a burden of proof, but nothing made an added observation property **visible**: a new `AgentView`-class member could be consumed quietly and the rule would be enforced only by review vigilance. | **T-MP-BOUND-008** pins the **current** observation-property set, so an addition shows up in a diff. This is the mechanical half of FR-MP-021, and R-1 names it as the mitigation. |
| L-1 | L | **KD-7 lived inside KD-2**, where *"neutral when **on**"* — the claim that distinguishes #48 from every sibling and the property that makes it safe to enable by default — was reachable only via the commentary decision. | Promoted to a key decision of its own; §5.1 asserts it **at full depth**, since neutrality with the feature disabled proves nothing about the feature. |
| L-2 | L | `AnimationFrameView` was not marked a struct although the same `IViewModelSource<T>` constraint binds it as binds the feed view. | Corrected in §2.2 and §4.4. |
| L-3 | L | §8's back-prop table was **empty with no explanation**, which reads as an unfinished section rather than a result — in a wave where every sibling files at least one. | §8.2 states it as a **positive property** with the #37/#44/#46 precedent, and adds the forward rule: a future #48 back-prop against match-engine is a signal to re-check derivability. |
| L-4 | L | The **tap-ownership drift** risk was unrecorded: FR-MP-008 says the tap is #37's contract regardless of who builds it, but an implementer building it **inside** `src/match-presentation/` would make it a #48 surface in practice. | New risk **R-6**; **T-MP-BOUND-006** asserts #48 registers a *consumer* rather than owning a tap. |
| L-5 | L | §6 did not distinguish the **tick-thread** budget from the UI-thread ones, though only the first can slow the **simulation** rather than the presentation. | §6.3 flags `MP_BUDGET_ONTICK_US` as the only consequential ceiling and sizes it against the certified FR-PO-052 per-tick baseline. |
| L-6 | L | `CommentaryLine`, `CommentarySlots`, `CommentaryFeedView`, `AnimationFrameView` and `ICueSink` were described in prose only. | Written out in §2.2, each annotated with the constraint that shapes it. |

**AR-2 sweep: 0H + 0M + 3L, all resolved — CONVERGENCE** (an L-only round closes the cycle, per the
project convention). **L-1:** §7.1 did not say that **T1 is the earliest point the neutrality claim is
assertable**, leaving an ordering in which the UI binding lands first and the headline property goes
untested while the code that could break it already runs. **L-2:** §4.2 did not explain why `SelectionMix`
is a **separate stateless file** — co-locating it with the recorder is the shortest path to caching a
variant per intent, which breaks FR-MP-013 without failing any determinism test. **L-3:** §8.6 did not
record that the **commentary corpus is #49's, not #48's** — tabulating example lines here would create a
second definition *and* put a baked string in a sim-adjacent spec.

## 9.5 Sign-off

| Role | Criterion | Signed |
|---|---|---|
| R-01 | Scope and out-of-scope boundaries are unambiguous; the **content-vs-trigger** distinction is stated plainly, and the #37 / #49 / #51 / #38 splits are explicit rather than implied. | ⏳ pending |
| R-02 | Every formula has units, ranges, and at least one worked example; no fabricated verification values — and **no test pins a `SelectionMix` output**, which would be a fabricated hash. | ⏳ pending |
| R-03 | Determinism posture is complete: draw-free at every tier, the local keyed mix justified, and **observer neutrality asserted unconditionally and at full depth** rather than conditioned on a decision. | ⏳ pending |
| R-04 | The **no persistent state** claim is exact: no save sub-blob, no format version, and the exported artifact's non-save status argued rather than assumed. | ⏳ pending |
| R-05 | The layer taxonomy is defended **mechanically**: the reverse-reference scan, the named `match-client-core` exclusion, the pinned observation-property set, and the symmetric #51 non-reference. | ⏳ pending |

## 9.6 Decision

**PENDING** — G1 closed (PASS-1 0H+4M+6L → AR-2 0H+0M+3L convergence, §9.4.1), G2 N/A. G3 and G4 remain
open: sign-off is a human authority, and the registry row is added at promotion.

**What verification did to this spec, restated at the decision point.** Two of the plan's five decisions
were answered by work that landed *after* it was written, and both made #48 **smaller**: commentary is a
**#49 producer** with a **local keyed mix** rather than a #22 consumer riding `world.text`, and the
composition question was already settled by client assemblies that exist. The one place verification made
#48 **harder** is the constraint the plan never mentions: **there is no post-match ledger reader**, and
`SerializeLedger` is write-only — so event-driven commentary can only be **captured live**, and a replay
replays a transcript rather than re-deriving one.

**The claim this spec should be judged on is unusual.** Most specs in this project claim to be neutral
**when switched off**. #48 claims to be neutral **when switched on**: a match rendered with commentary,
animation and audio cue mapping all enabled produces a **byte-identical digest chain** to an unobserved
run. That is only true because KD-2(ii) makes the selection value a **local keyed mix rather than a
draw**, and §5.1 asserts it at full depth — because neutrality with the feature disabled proves nothing
about the feature.

**And it files no back-props.** In a wave where every sibling changes something upstream, #48 changes
nothing — which is the evidence that a presentation spec sits correctly in the layer, and the result to
re-check if that ever stops being true.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §9 (completeness, tag discipline, the §9.3 source-verified claims table, four gates with **G2 marked N/A** and the reason stated, R-01..R-05). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | G1 CLOSED: §9.4.1 records the section-file PASS-1 (0H+4M+6L, all resolved — clustered on the guards that make *"we touch nothing"* mechanical, and on the one selection input whose omission is invisible to every determinism test) and the AR-2 convergence sweep (0H+0M+3L). §9.1 completeness updated for KD-7 and FR-MP-013; §9.2 gained the no-`[GT]`-affects-simulation line; §9.3 gained the `match-client-core` mutation-surface row, the missing-`src/match-analytics/` row, the `IViewModelSource<T>` struct-constraint row, the `FR-MP` prefix check, and the note that #48 proposes **no** `ERR-*` id at all. G3 and G4 remain open. |
#endregion
