# Audio & Sound Design #51 — Outline

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## Provenance

Promoted from `docs/tracking/audio-sound-design.md` **v0.4** (AR-converged: AR-1 0H+2M → AR-2 0H+1M →
AR-3 0H+0M+2L = CONVERGENCE), itself promoted from
`docs/tracking/spec-plans/spec-51-audio-sound-design.md` v0.2. Governing feature definition:
`docs/planning/master-plan-amendment-01-audio-multiplayer-transport.md` §2.

**FR prefix:** `FR-AU` · **Wave:** 8 · **Tier:** S1 min → S2 full → S3+ deep · **Assembly:**
`TacticalDirector.Audio`

## Section map

| § | Content |
|---|---|
| 1 | Scope, dependencies, the six verified facts — including the **layering contradiction in #48's approved text** that KD-1 exists to resolve — and KD-1..KD-6 |
| 2 | FR-AU-001..038, data structures, failure modes F1..F8 |
| 3 | FM-AU-01..04 — the shell resolve, playback routing, ducking evaluation, settings apply — with worked examples |
| 4 | Architecture: **#51 as a leaf**, the shell-owned join, and the one edge that legitimately points *at* #51 |
| 5 | Test plan — mapping completeness in the shell, caption coverage **by construction**, the directional layer scan, unconditional observer neutrality |
| 6 | Performance — a presentation-thread cost with **no** sim-loop path |
| 7 | Future extensions, T-phase plan, risks R-1..R-6 |
| 8 | Cross-references XC-051-001..017; back-props **ERR-048-001** and **ERR-038-004** |
| 9 | Approval checklist + the PASS-1 adversarial-review record |
| Appendices | A constants · B the bus set and cue-catalogue schema · C the ducking table · D the settings fragment |

## The one-paragraph summary

**#51 is the audio framework: the bus taxonomy, the cue catalogue, the playback API, ducking, music, UI
audio, the client-local audio settings schema, and the accessibility caption contract.** It does not
decide **when** a match sound fires — #48 does that, and #51 never observes a match. It is a **leaf**: it
references neither the spec that tells it what to play, nor the one that renders its captions, nor the
simulation.

**Its load-bearing decision resolves a contradiction in approved text.** #48 states both that *#51 never
references #48* and that *#51's catalogue will be keyed on #48's `CueId`* — and the second requires
exactly the reference the first forbids. Left alone it would surface as an assembly cycle at
implementation time, after both specs were APPROVED. **KD-1** splits the identity spaces (#48 owns the
semantic `CueId`; #51 owns the catalogue `CueKey`) and puts the mapping in the **composition root**, which
is the only layer that legitimately sees both — the third instance of that same inversion in this wave.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial outline from supplement v0.4. Status IN REVIEW. |
#endregion
