# Match Presentation Depth #48 — Appendices

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #48 has no `[EST]` constants and — because it takes **no determinism
reservation at all** (KD-6, FR-MP-033) — **no `[CROSS-PENDING]` constants either**, so neither region
appears.

All of these live in `MatchPresentationConstants.cs` (§4.2), so no formula file carries a magic number.

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `MP_SELECTION_SEED` | `0x4D50_5245_5345_4E54` (`"MPRESENT"`) | `[FIXED]` | The domain-separating seed of FM-MP-02's local keyed SplitMix64. **Arbitrary but pinned**, and deliberately non-zero: it separates #48's selection space from every other local-mix site (`FixtureScheduler`, `LeagueBootstrap`, #35, #46) so two producers cannot draw correlated variants from the same key. **It is not a `[GT]` dial** — changing it re-shuffles every variant choice in the game and improves nothing, which is the definition of a value nobody should tune. |
| `MP_NO_SUBJECT` | `-1` | `[FIXED]` | The absent-subject sentinel in `CommentarySlots` (§2.2). **`-1`, emphatically not `0`** — `0` is a valid agent id, and a sentinel that collides with real data is the defect the `NO_ROSTER_CLUB_ID = -1` precedent exists to avoid. FM-MP-02's `subjectAgentId + 1` shift then maps it to the mix's **neutral** `0` input rather than to `0xFFFFFFFF` (§3.7(g)). |

### A.2 Derived

| Constant | Formula | Tag | Notes |
|---|---|---|---|
| `COMMENTARY_INTENT_COUNT` | `Enum.GetValues(typeof(CommentaryIntent)).Length` | `[DERIVED]` | Derived from the enum, **never a hand-maintained literal** — the `POSITION_COUNT` precedent, where two assemblies each carried a private copy of an enum's member count. FR-MP-015's FR-LC-008a coverage assertion sweeps this range, so a hand-written count that lagged an append would silently stop checking the newest intent for a base-locale row. |
| `CUE_ID_COUNT` | `Enum.GetValues(typeof(CueId)).Length` | `[DERIVED]` | Same reasoning, for the cue roster. |
| `MP_ANIM_AGENT_CAPACITY` | `MatchEngineConstants.SQUAD_SIZE` | `[DERIVED]` | `AnimationFrameView`'s fixed inline capacity. Derived rather than duplicated: a second copy would drift the moment the squad size moved, and the view must hold exactly one pose per on-pitch agent. |

### A.3 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| `BallView`, `AgentView(i)`, `AgentTeamId(i)`, `AgentIsGoalkeeper(i)`, `PossessingAgentId`, `HomeScore`, `AwayScore`, `MatchEnded` | match-engine | **Value copies.** The complete pinned set — see Appendix C, and `T-MP-BOUND-008`. |
| `MatchEngineConstants.SQUAD_SIZE` | match-engine | Source of `MP_ANIM_AGENT_CAPACITY` above. |
| The physics tick rate (`TICKS_PER_SECOND` in FM-MP-03) | match-engine | The 60 Hz physics cadence, used **only** to convert a per-tick position delta into a velocity. Consumed verbatim; #48 declares no cadence of its own, because a second copy would silently mis-scale every gait threshold if the loop rate ever moved. |
| The per-tick event tap's record type | **#37** (FR-AN-002) | Joined as a third consumer, **never redefined** (FR-MP-007/008). |
| `IViewModelSource<T>` (`where T : struct`) | #38 | The hosting contract — and the constraint that forces `CommentaryFeedView` to be a bounded by-value window rather than a handle (FR-MP-029). |
| `TextTemplateId`, `LocalizedTextRequest`, `ILocalizer` | #49 | Used **only inside `MatchTextBoundary`**, which is **not** a #48 assembly (FR-LC-012). |
| `LiveMatchFrame`, `HtmlReplayExporter` | match-viewer | Sibling frame types and the exporter that bakes the artifact's text at the boundary (FR-MP-017). |

**Two deliberate exclusions, recorded so their absence is not read as an oversight:**

1. **`ILiveMatchMutations` / `ManagerCommandQueue`** (`match-client-core`) — a genuine mutation surface in
   the **same layer**, one reference away. `T-MP-BOUND-002` asserts the absence **by name** rather than
   relying on the general reverse-reference scan, because the general scan would not catch it: it runs in
   the wrong direction.
2. **#51's playback API and cue catalogue** — #48 stops at the `CueId` (FR-MP-023). `ICueSink` is declared
   by #48 and implemented by the **shell**, so neither spec appears in the other's catalogue.

### A.4 GT

| Constant | Value | Notes |
|---|---|---|
| `COMMENTARY_WINDOW_LINES` | `20` | The bounded window's fixed capacity (FR-MP-029). A **display** quantity: it sets how many lines a screen shows, not what is captured — the full transcript stays inside #48 regardless (§3.5). |
| `MP_WALK_MAX` | `2.0` m/s | FM-MP-03 gait threshold: below this a derived speed classifies as `Walk`. |
| `MP_JOG_MAX` | `5.0` m/s | FM-MP-03 gait threshold: below this, `Jog`; at or above, `Sprint`. |
| `MP_FACING_MIN_SPEED` | `0.2` m/s | Below this the derived facing is **held** from the previous tick rather than recomputed from a near-zero velocity, whose direction is numerical noise (§3.7(k)). |
| `MP_BUDGET_ONTICK_US` | `20` µs | §6.3 ceiling for one `OnTick` on the **tick thread**. |
| `MP_BUDGET_ANIM_FRAME_MS` | `2` ms | §6.3 ceiling for one `DeriveAnimationFrame` over the full squad, on the UI thread. In **milliseconds** deliberately: a per-rendered-frame operation is measured against a frame budget. |
| `MP_BUDGET_FEED_VIEW_US` | `50` µs | §6.3 ceiling for one window snapshot. |

**The last three are ceilings, not measurements.** No certified number exists for #48 and none is invented
here: a certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #48 has no implementation to measure. They are generous so a first
measurement either passes comfortably or reveals something genuinely wrong — the `CertifiedPerfBaseline`
PENDING posture applied to a spec that has not been built. **`MP_BUDGET_ONTICK_US` is the only one whose
overrun costs simulation time** rather than frames (§6.3).

**No `[GT]` constant in this catalogue affects the simulation** (§9.2), and the four behavioural rows are
where a reader should check that claim rather than take it on trust. `COMMENTARY_WINDOW_LINES` sizes a
view model; the three thresholds classify a **derived** velocity into a display state. None is read by
match-engine, none reaches a digest, and none is serialized — which is what makes retuning any of them a
zero-risk change and makes observer neutrality **unconditional** (KD-7) rather than contingent on their
values.

**#48 therefore carries a `[GT]` balance pass in the presentational sense only** (§9.4): the gait
thresholds want tuning against how the animation actually *looks*, which requires the renderer, hence the
Unity host. That is a T3 activity, and it gates nothing — a wrong threshold makes a jog look like a
sprint, and cannot make a match play differently.

## Appendix B — The two rosters

### B.1 `CommentaryIntent` — APPEND-only (FR-MP-014)

The enum is **not enumerated with final membership here**; §2.2 gives the shape (`None = 0`, then goal,
card, save, chance, …) and the roster is filled as the mapping is authored. What is pinned is the
**contract**, which is stronger than either of the two APPEND-only cases this project has met before:

| The ordinal is… | …so a reorder |
|---|---|
| the `LocalOrdinal` half of the #49 `TextTemplateId` the catalogue is keyed on | **re-points every catalogue row** — the goal template becomes the card template, with no error |
| **embedded in exported HTML artifacts** | **mis-labels every existing export**, retroactively |

**Neither failure has a version gate in front of it.** #49's catalogue is keyed by ordinal, not by name;
and an exported artifact is explicitly *"NOT a determinism-pinned wire format"*, so nothing validates the
ordinals it carries. That combination is why `T-MP-LOC-002` is described in §5.6 as a
**save-correctness-class** lock in a spec that saves nothing.

**`None = 0` is a refused value, not a default.** FR-MP-015's pre-gate rejects it before any selection
work (F1), so the zero value can never be rendered — the inverse of the zero-value trap this project has
caught in `BoardModifier` / `MedicalModifier` / `BoardConfidence`, where `default(T)` was
field-in-range yet semantically severe. Here `default` is **defined as invalid** and fails loud.

### B.2 `CueId` — APPEND-only (FR-MP-027)

Same discipline, **a weaker reason, and it is worth keeping the two separate.** `CueId` ordinals are not
embedded in any artifact and are not a #49 key; the contract exists because **#51's catalogue will be
keyed on them**, which is a future dependency rather than a present one.

`T-MP-LOC-003` asserts it **separately from `T-MP-LOC-002`** for exactly that reason: the two enums fail
for different reasons, at different times, so a test that checked one and inferred the other would prove
nothing about the second.

### B.3 There is no save layout — the appendix that is deliberately absent

Every other spec in this wave has an appendix here giving a byte layout. **#48 has none, and that is a
classification rather than an omission** (FR-MP-032):

- **no save sub-blob**, in the season save frame or anywhere else;
- **no format version** — nothing for #50's registry to carry a row for;
- **no restore path**, because there is no state to restore;
- **no `_RESERVED_` placeholder** in #16 §3.4 — no stream, no domain tag, no `SubsystemOrdinal`, and
  therefore **nothing to promote later** (FR-MP-033). A future stochastic presentation surface would need
  a **fresh** allocation, and should not read #48's silence as a claim on one.

**The exported HTML artifact is not a counter-example.** It embeds rendered commentary text (FR-MP-017)
and is self-contained, but it is a **display artifact**: no version gate, no reader, nothing the
simulation loads back. That is precisely why its text can be baked in the export's locale without an
FR-LC-006 problem — a save must be locale-independent; a screenshot need not be.

**The transcript is session-scoped** (§4.6): it lives for the match, is released with the session, and is
never carried across matches or into a file. A shareable match report is the obvious ask and is a **new
persistence surface** with a format version, a #50 registry row and a locale question — its own decision
(§7.4 R-2), not a quiet extension of this appendix's absence.

## Appendix C — The pinned observation-surface inventory (KD-3)

This is the complete set of match-engine members #48 may read. `T-MP-BOUND-008` asserts it **as a pinned
set**, so an addition is **visible in a diff** rather than arriving quietly.

| Member | Shape | What #48 derives from it |
|---|---|---|
| `BallView` | value copy | ball position and height for the animation frame and for cue parameters |
| `AgentView(i)` | value copy | the per-agent position history — the sole source of **velocity, gait and facing** (FM-MP-03) |
| `AgentTeamId(i)` | `int` | team attribution in commentary slots and in cue parameters |
| `AgentIsGoalkeeper(i)` | `bool` | keeper-specific intents and poses |
| `PossessingAgentId` | `int` | the subject of possession-derived lines; `MP_NO_SUBJECT` when none |
| `HomeScore`, `AwayScore` | `int` | `CommentarySlots` score fields |
| `MatchEnded` | `bool` | the end-of-match transition |

**Everything else #48 renders is derived from these plus the live tap.** The three worth naming, because
each *looks* like it needs a sim-side field and does not:

1. **Facing** — a stationary agent's facing is the last direction he moved, which the position history
   carries (§3.7(k)). This is the example the burden-of-proof rule was written around.
2. **Gait** — a classification of a derived speed, not a state the engine holds.
3. **Minute** — a function of the tick, not a separate clock read.

**The addition rule (FR-MP-020/021), restated here because this is the table a future change edits.** Any
new member must be:

- **additive and read-only**, in the `BallView` / `AgentView` class — never a presentation-side push, and
  **never a new serialized field**;
- accompanied by a stated argument for **why the value cannot be derived** from the history;
- shown to leave the observer-neutrality digest lock **unchanged**.

**§7.4 R-1 names this as the single most likely way #48 damages the project**: a presentation need becomes
an engine field, and the layer taxonomy inverts one convenience at a time. The table above is the artifact
that makes each such step deliberate.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial appendices (A.1 Fixed with the argument that the selection seed is `[FIXED]` rather than `[GT]` and that `MP_NO_SUBJECT` must not be `0`; A.2 Derived, deriving both roster counts from their enums per the `POSITION_COUNT` precedent; A.3 Cross with the two deliberate exclusions; A.4 GT; B the two rosters with their **distinct** APPEND-only rationales and B.3 recording that #48 has **no save layout at all**; C the pinned observation-surface inventory with the addition rule restated at the table a future change would edit). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** the three `[GT]` budget ceilings declared in §6.3 were **absent from this catalogue**, which is meant to be the single catalogue and is what a reader greps for tag discipline — **the #45 PASS-1 M-2 defect, now seen for the seventh time in this wave**, which is enough repetition to be a process finding rather than seven independent slips; added to A.4. **M:** the four *behavioural* `[GT]` rows (`COMMENTARY_WINDOW_LINES`, the two gait thresholds, the facing floor) were absent too, so §9.2's assertion that **no `[GT]` constant affects the simulation** had nothing to check itself against; added, with the paragraph naming exactly where that claim is verifiable. **L:** A.2 gained `MP_ANIM_AGENT_CAPACITY` (the view's inline capacity was otherwise an implicit duplicate of `SQUAD_SIZE`); A.3 gained the physics tick rate, which FM-MP-03 consumes and nothing declared; B.1 gained the `None = 0` note distinguishing a **refused** zero value from the zero-value *trap* the wave's siblings carry; B.3's absence of a byte-layout appendix stated as a classification, since every other spec in the wave has one; C's derived-not-read list extended to `Minute`. |
#endregion
