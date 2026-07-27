# Audio & Sound Design #51 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + `AudioBus` + `CueKey` + `CaptionId` + `CaptionDecision` + `CueEntry` + `CueCatalogue` + `DuckingTable`, with their construction-time refusals and well-formedness gates. **No playback, no shell adapter, no settings binding.** | **Inert and silent** — and fully testable, because every gate is a construction-time refusal over value types |
| **T1** | `IAudioPlayback` + `AudioMixer` gain composition + the settings fragment + `ApplySettings`' reset policy. Still **no** shell adapter. | Silent; the framework exists and composes gains for nothing |
| **T2** | The **shell's** `CueSinkAdapter` + the `CueId → CueKey` map + the **build-time completeness check**, and the host playback binding. | **Audible.** This is where #48's emitted cues first make a sound |
| **T3** | Captions bound through #49 (which gains the `→ #51` reference at this point, §4.5); commentary-audio delivery alongside #48's deep tier; richer mix states. | Audible + accessible |

**T0 before anything audible is not merely sequencing — it is where every rule this spec actually enforces
lives.** Caption coverage, bus closure, ducking well-formedness and catalogue coherence are all
**construction-time refusals**, so they are complete and locked before a single sample plays. A T-phase
plan that started with playback would land the sound first and the discipline afterwards, which is the
order in which the discipline does not land.

**The completeness check must arrive with the adapter, in T2, not after it.** It is the shell's (KD-1), it
is a build-time failure (FR-AU-005), and the window between "cues play" and "we check every cue maps" is
exactly the window in which unmapped cues become normal and the check becomes a source of build failures
someone disables.

**The predicted T2 failure is the CS0104 collision** (§4.2): `CueParams` exists in both #48 and #51, the
adapter is the one file that sees both, and the natural first draft imports both namespaces. It fails at
compile time, which is the good case — the bad case is a `using` alias that makes the wrong one look right
to a reviewer.

**T3's caption half is gated on #49's reference landing**, which is #49's approved design executing rather
than a change to it — so it is a sequencing dependency, not an open question.

## 7.2 Deep-tier extensions (designed for, not built)

- **Commentary-audio delivery**, alongside #48's deep presentation tier (S3+).
- **Richer mix states** driven from #38's navigation context — menu / match / paused / replay — which
  FR-AU-016 already permits.
- **Data-driven bus routing** (S3+), revisited **only if a real mix demands it**, and knowingly trading
  away the by-construction completeness property KD-2 bought.
- **Additional cue categories** — an APPEND-only `CueKey` roster extension, with its mapping rows.
- **Per-cue DSP parameters** (reverb zones, occlusion) — additive to `CueParams`, host-side.
- **Accessibility beyond captions** — mono downmix, visual cue indicators. Named because #51 owns the
  a11y contract for **audible information**, so the surface belongs here even though the contract
  currently stops at captions.

## 7.3 Explicitly not planned

- **#51 referencing #48**, in any form, including "just for the `CueId` enum" (FR-AU-001). This is the
  reference the approved text currently asks for and ERR-048-001 corrects.
- **#51 implementing `ICueSink`** (FR-AU-002). It would invert the layering and give a Wave-8 spec a
  Wave-7 dependency.
- **#51 referencing `TacticalDirector.Localization`** (FR-AU-007). Captions flow the other way.
- **Moving `CaptionId` into #49** — the natural "fix" for a symmetric layer scan's false positive, and it
  breaks KD-4 from the other side (§4.5).
- **Reading sim state for a mix decision** (FR-AU-015). *"Duck the crowd when a goal is scored"* reads
  identically to a designer and is architecturally different from *"duck the crowd when the commentary bus
  is sounding"*.
- **Drawing cue variation from a `deterministic-sim` stream** (FR-AU-033). It would make what the player
  hears change what is saved.
- **A sixth client-local settings store** (FR-AU-019/022). If ERR-038-004 is declined, the fallback is
  in-memory with persistence deferred — never a private file.
- **Save-grade refusal for settings** (FR-AU-020). A corrupt preference byte must not block launch.
- **A data-driven bus set at S2** (FR-AU-012). It would make "routed to a nonexistent bus" a runtime
  state.
- **Specifying the content.** Assets, mix tuning and "match feel" are production's; #51 specifies
  identities, routing and contracts (R-1).

## 7.4 Risks carried

- **R-1 — asset-heavy, engineering-light.** The spec is contracts and catalogues; the *content* dwarfs it,
  and §6.4 makes the same point in memory terms. Mix tuning and "match feel" must stay out of spec text —
  the #48 R-3 risk, with more force here because **#51 owns the catalogue itself** and so has a place to
  put the content it must not specify.
- **R-2 — the shell mapping table is a natural dumping ground** (KD-1). It is the one place that sees both
  id spaces, which is exactly why unrelated adapter logic will accumulate there. It should hold the map
  and the adapter and nothing else (§4.4), and T-AU-BOUND-007 is the mechanical form of that rule.
- **R-3 — the settings-store ownership back-prop may be declined** (ERR-038-004). The fallback is
  in-memory with persistence deferred (FR-AU-022), **not** a private file: a sixth store is worse than no
  persistence, because it is the failure mode that cannot be undone once shipped.
- **R-4 — caption coverage has a real authoring cost** (KD-4), and cost is what erodes construction-time
  rules. The `NoCaption` escape must stay cheap and legitimate — while requiring a justification
  (FR-AU-027), so it stays deliberate rather than reflexive. Those two pressures pull against each other
  and the balance is the risk.
- **R-5 — no playback verification in CI** (KD-5). The contract layer can be entirely green while the game
  is silent, mis-mixed or ducking wrongly. §5.7 names the unverified properties explicitly, because an
  audio spec is the easiest place in the tree to mistake a green contract suite for a working feature.
- **R-6 — ERR-048-001 is a text correction to an APPROVED spec, and it may be read as optional.** It
  changes no #48 code, contract or test, which is exactly why it could be deferred — and if it is, #48's
  FR-MP-027 continues to instruct implementers to key #51's catalogue on `CueId`. The cost of deferring is
  not a stale sentence; it is that **the next person to implement either spec builds the forbidden
  reference in good faith**, and finds out at the assembly cycle.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §7 (T0–T3, with the argument that T0 is where **every rule the spec enforces** lives, since all of them are construction-time refusals — so a playback-first ordering would land the sound before the discipline; the completeness check pinned to arrive **with** the adapter rather than after it; the predicted `CueParams` CS0104 failure and why the compile error is the good case; deep-tier extensions incl. a11y beyond captions; the not-planned list, which carries the two "natural fix" traps — moving `CaptionId` into #49, and reading sim state for a mix decision; risks R-1..R-6, with R-6 added because ERR-048-001 changes no code and is therefore the back-prop most likely to be deferred, at the price of the next implementer building the forbidden reference in good faith). Status IN REVIEW. |
#endregion
