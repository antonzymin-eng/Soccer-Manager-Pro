# Match Presentation Depth #48 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + `CommentaryIntent` / `CueId` + the value types + `SelectionMix` + `AnimationDeriver` + `CueMapper`, with their unit and ordinal-stability suites. **No tap consumer registered**, no #38 binding, no adapter. | **Inert** — and testable, because every piece is a pure function over value copies |
| **T1** | The **shared tap** consumer: `CommentaryRecorder` joined to the #37-contract tap (built by whoever lands first), with the transcript and the **unconditional neutrality lock** (T-MP-ID-001). Still no UI. | **Live and neutral.** The claim is asserted here, at the earliest point it can be |
| **T2** | #38 binding: `CommentaryFeedView` / `AnimationFrameView` through `IViewModelSource<T>`, with the **snapshot-copy thread boundary** and its race locks. `MatchTextBoundary` in the boundary layer, plus the base-locale catalogue rows. | **Live.** A match view now narrates; still no audio, still no save |
| **T3** | The `CueSinkAdapter` in the client shell when **#51** lands (a sink change, not a #48 change); the exported artifact's embedded commentary; the 3D renderer when **Unity host access** exists. | **Named activation** — and two of the three are gated on things outside #48 |

**T1 is where the spec's headline claim becomes assertable, and it should not wait for T2.** Observer
neutrality at full capture depth is a property of the recorder and the mix, not of the UI — so binding to
#38 first would mean the claim is untested while the code that could break it is already running.

**The predicted T2 failure is the thread boundary**, not the rendering. `IViewModelSource<T>`'s
`where T : struct` prevents a *field* alias but not a *boundary* mistake: a `GetFeedView` that returns
without copying compiles, works in a single-threaded test, and tears only under a live streamed match.
T-MP-I-005/006 exist for exactly that.

**T3's export half is the one that can regress silently** (F5): an artifact that embeds no commentary
simply has none — no error, no crash. T-MP-I-002 is the lock.

## 7.2 Deep-tier extensions (designed for, not built)

- **The 3D renderer**, when Unity host access exists (the standing OPEN ISSUE that also gates the
  interactive client). **The contract is authorable and testable now**; only the renderer is gated —
  which is the whole reason #48 splits *animation state* from *rendering*.
- **`ICueSink`'s real implementation**, when **#51** lands: a `CueSinkAdapter` in the client shell. A
  **sink change, not a #48 change** — which is what the no-op-default seam bought.
- **Richer commentary sources** — stat-driven lines from #37, which is a read-only source on the **same**
  tap (FR-MP-007). Additive: more intents, no new mechanism, no second tap.
- **Additional cue categories** — an APPEND-only `CueId` extension, keyed by #51's catalogue.
- **Additional observation-derived animation fidelity** — anything derivable from the position history is
  a #48-internal state-machine change with **no** engine involvement (FR-MP-019).
- **An additive read-only match-engine observation property**, *if* a future fidelity proves something
  genuinely underivable — under FR-MP-020/021's burden of proof, and never as a serialized field.

## 7.3 Explicitly not planned

- **Any write into the simulation**, at any tier, through any surface (FR-MP-001). Including a "debug"
  or "editor" path.
- **A `match-client-core` reference.** Its `ILiveMatchMutations` and `ManagerCommandQueue` are a real
  mutation surface in the **same layer**, one line away (FR-MP-004). Playback controls belong to the
  shell.
- **A second live tap** (FR-MP-007). One ledger, one tap, three consumers — #37, #44, #48.
- **Re-parsing the serialized ledger.** `SerializeLedger` is write-only and FR-AN-021 forbids assuming the
  bytes are re-parseable; a reader would be a second, unowned ledger format (FR-MP-009).
- **Consuming `world.text` or #22's `InteractionTextGenerator`.** The selection value is a local keyed
  mix; #48 never references `living-world` (FR-MP-012).
- **An RNG stream for commentary variety.** It would make observer neutrality conditional, which is
  precisely the property KD-7 exists to make unconditional.
- **Persisting the transcript in a save.** It is session-scoped (FR-MP-032). A shareable match report is a
  new persistence surface with a format version and a locale question — its own decision, not a quiet
  extension (R-2).
- **#51 implementing `ICueSink`.** That would invert the layering and give a Wave-8 spec a Wave-7
  dependency (FR-MP-025).
- **Baking rendered text anywhere inside #48**, including in the transcript. The export's text is baked at
  the **boundary** (FR-MP-016/017).
- **Specifying the content.** The commentary corpus, the animation clips and the audio assets are
  production's; #48 specifies triggers, identities and contracts (§1.2 / R-3).

## 7.4 Risks carried

- **R-1 — "just expose it from the engine" (KD-3).** The single most likely way this spec damages the
  project: a presentation need becomes a new engine field, and the layer taxonomy inverts. Mitigated by
  requiring a **derivability argument plus an unchanged neutrality lock** for any new observation
  property (FR-MP-020/021), and by **T-MP-BOUND-008 pinning the current property set** so an addition is
  visible in a diff rather than arriving quietly.
- **R-2 — the transcript is session-scoped, and someone will want it saved.** A shareable match report is
  the obvious ask, and it is a **new persistence surface** with a format version, a #50 registry row, and
  a locale question — its own decision, not an extension of KD-2. Standing option.
- **R-3 — #48 is mostly assets, and the spec is mostly contract.** The engineering reality is that
  animation and audio **content dwarf the logic here**, and the spec must not imply the content is
  specified by specifying the triggers. §1.2's out-of-scope table disowns the asset surface explicitly for
  that reason.
- **R-4 — `ERR-049-001` is now load-bearing for three specs** (#35, #46, #48). If #49's owner declines it,
  all three take the `SelectionDraw = 0` fallback and lose phrasing variety at the minimal tier — **most
  visibly for #48**, since repeated commentary lines are immediately noticeable in a way a repeated inbox
  headline is not.
- **R-5 — the Unity host gate blocks the renderer, not the spec**, and the risk is the **inverse** of the
  usual one: that #48 is deferred *wholesale* because 3D is blocked, losing the commentary and
  cue-mapping contracts that are authorable now and that #38's match view wants.
- **R-6 — the tap's ownership can drift if #48 lands first.** FR-MP-008 says the tap is #37's contract
  regardless of who builds it, but an implementer building it inside `src/match-presentation/` would make
  it a #48 surface in practice, and #37/#44 would then join a presentation assembly. T-MP-BOUND-006
  asserts that #48 registers a consumer rather than owning a tap, which is the mechanical form of the
  rule.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §7 (T0–T3 with **T1 identified as the earliest point the neutrality claim is assertable** and the argument for not deferring it behind the UI binding; the predicted T2 thread-boundary failure and the silent T3 export failure both named; deep-tier extensions with the two externally-gated items marked; the not-planned list; risks R-1..R-6, with R-6 added for the tap-ownership drift that FR-MP-008 addresses in prose and T-MP-BOUND-006 addresses mechanically). Status IN REVIEW. |
#endregion
