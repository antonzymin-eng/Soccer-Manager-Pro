# Audio & Sound Design #51 — Section 9: Approval Checklist

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.3 — APPROVED: R-01..R-05 sign-off granted; back-props filed atomically)
**Last Updated (prior):** July 27, 2026 (v0.2 — PASS-1 fix pass recorded)
**Version:** 0.3
**Status:** APPROVED

---

## 9.1 Completeness

- [x] §1 scope, dependencies, the six verified facts, KD-1..KD-6, staging.
- [x] §2 FR-AU-001..038, data structures, failure modes F1..F8.
- [x] §3 FM-AU-01..04 with fifteen hand-verifiable worked examples.
- [x] §4 architecture — the leaf assembly, the shell join, the one inbound edge.
- [x] §5 test plan, incl. shell-side mapping completeness, caption coverage by construction, the
      **directional** layer scan, unconditional observer neutrality, and §5.7's statement of what a green
      CI does **not** prove.
- [x] §6 performance — no sim-loop path, with the `Play`-on-the-tick-thread cadence stated precisely.
- [x] §7 T-phase plan, extensions, risks R-1..R-6.
- [x] §8 cross-references XC-051-001..017 and back-props **ERR-048-001** / **ERR-038-004**.
- [x] Appendices A (constants), B (the bus set and catalogue schema), C (the ducking table), D (the
      settings fragment).

## 9.2 Constant discipline

- [x] Every constant carries exactly one tag; Appendix A is the single catalogue.
- [x] No `[EST]` constants.
- [x] **No `[CROSS-PENDING]` constants** — #51 takes no determinism reservation (KD-6), so nothing is
      blocked on an upstream allocation.
- [x] **No `[GT]` constant affects the simulation.** #51's behavioural `[GT]` values are default gains and
      the ducking table — both **client config**, both downstream of every determinism boundary, and
      neither readable by any sim assembly (FR-AU-008). Verifiable in Appendix A and Appendix C.
- [x] The bus enumeration is **fixed and closed**, not a `[GT]` list — a tunable bus set would make
      "routed to a bus that does not exist" a runtime state (FR-AU-012).

## 9.3 Determinism discipline

- [x] No RNG stream, no domain tag, no `SubsystemOrdinal`, **no `_RESERVED_` placeholder** (FR-AU-032).
- [x] Cue variation is **display-side** and touches no serialized cursor (FR-AU-033) — the one plausible
      way an audio framework breaks determinism, asserted by T-AU-ID-004 **with variation active**.
- [x] **Bidirectional** sim isolation: the sim cannot read audio state, and the audio path cannot call
      into the sim (FR-AU-034/035).
- [x] Observer neutrality is **unconditional** — asserted with audio **enabled** (FR-AU-036).
- [x] Nothing is serialized into any sim save; no format version anywhere (FR-AU-037).

## 9.4 Gates

| Gate | Status |
|---|---|
| **G1** — section-file PASS-1 adversarial review + fix pass | **CLOSED** — see §9.4.1 |
| **G2** — back-props filed at approval (`ERR-048-001`, `ERR-038-004`) | **CLOSED** — filed and RESOLVED July 27, 2026, atomically with the flip (`spec-error-log.md` v1.47) |
| **G3** — lead-developer R-01..R-05 sign-off | **CLOSED** — R-01..R-05 granted by the lead developer, July 27, 2026 |
| **G4** — `[GT]` balance pass | **OPEN, and it is a mix pass rather than a balance pass** — the default gains and the ducking envelopes want tuning against how the game actually **sounds**, which requires the Unity host (KD-5). It gates nothing: a wrong attenuation sounds wrong and cannot make a match play differently |

### 9.4.1 PASS-1 adversarial review — **0H + 4M + 5L**, all resolved

**M-1 — the ducking table had no well-formedness rule.** KD-2 specifies its shape and its trigger and says
nothing about validity, so a row with `Trigger == Ducked`, or a two-row cycle, could sustain indefinite
attenuation. The failure is quiet in the worst way: **a mix that ducks itself into silence produces no
error and offers no recovery**, and the table is exactly the artifact a content author edits without a
compiler. Resolved as **FR-AU-017 / F5**; locked by T-AU-U-004/005.

**M-2 — the declined-ownership case had a named risk and no requirement.** R-3 records that #38 may
decline ERR-038-004, but nothing said what #51 does then — and the obvious answer, *define our own file
after all*, is precisely the outcome KD-3 exists to prevent. Resolved as **FR-AU-022 / F8**: the fallback
is **in-memory with persistence deferred**, on the stated ground that **a sixth store is worse than no
persistence, because it cannot be undone once shipped**.

**M-3 — `NoCaption` had no justification requirement, so KD-4's construction-time rule was satisfiable by
reflex.** The supplement argues correctly that an audit drifts by whatever is added after it and that a
required field cannot drift — but an unjustified escape hatch reintroduces the same drift one step later,
as authors reach for `NoCaption` by default. Resolved as **FR-AU-027 / F3**; locked by T-AU-A11Y-003.

**M-4 — §6 initially classified #51 as having no loop-path cost at all, which is not quite true.** #51
holds no sim type, so its loop isolation is provable from the reference graph — but **`Play` is called by
#48's adapter on the streamer's tick thread**, so an expensive `Play` slows the *simulation* exactly as
#48's own `OnTick` does. Stating "not in the loop" without that would have left the one cadence with real
consequences unbudgeted. Resolved in §6.1/§6.3, with `AU_BUDGET_PLAY_US` named as the only ceiling whose
overrun costs simulation time.

**L-1** — `CueParams` is declared by **both** #48 and #51 and the collision was unrecorded; §2.2 and §4.2
now name it, confine it to the single shell file that sees both, and require fully-qualified use there.
**L-2** — `CaptionDecision`'s zero value had no stated status; §2.2 now records that `default` is
**defined as invalid**, the inverse of the zero-value trap the wave's siblings carry. **L-3** — §5's layer
scan was written as a property of the assemblies rather than of the **test**; T-AU-BOUND-003 now makes
directionality a requirement on the scan itself, since a symmetric check would flag the legitimate
`#49 → #51` edge and the natural repair for that false positive breaks KD-4 from the other side.
**L-4** — the four `[GT]` budget ceilings declared in §6.3 were **absent from the Appendix A catalogue**
(the #45 PASS-1 M-2 defect, now seen for the **ninth** time in this wave); added. **L-5** — §8.5 did not
say why an accessibility spec carries no external standard citation; the absence is now stated as
deliberate, with the note that adopting one would make it **#49's** citation and leave #51's contract as
the mechanism that satisfies it.

**AR-2 sweep — 0H + 0M + 2L → CONVERGENCE** (an L-only round closes the cycle, per project convention).
The sweep re-walked every statement about reference direction, since the supplement's own AR-2 and AR-3
were both direction findings and the class was demonstrably the highest-yield thing to re-check.
**L-6:** §4.7's neighbour table gave the #49 relationship without its direction, which is the exact defect
the supplement's AR-3 L-1 caught in its own §7 — a surface table is read before the DAG, so a
direction-free row there outweighs a correct DAG below it; now stated as `#49 → #51`. **L-7:** §7.1's T3
row described captions as "bound through #49" without noting that the reference is **#49's to add at that
point**, which would leave an implementer looking for a #51-side change that must not exist.

## 9.5 Verification anchors

Every claim below is checkable against a named file; none is a summary of another summary.

| Claim | Anchor |
|---|---|
| There is no audio code in the tree | a `src/**` search for playback surfaces (§1.4(a)) |
| #48 chose the stub-sink option deliberately | #48 KD-4 / FR-MP-026 (XC-051-001) |
| #48 forbids `#51 → #48` **and** requires it | #48 KD-4 / FR-MP-025 vs. FR-MP-027 (XC-051-002/003) |
| Five specs name a settings store and none owns it | FR-LC-018; `ui-client-framework/section-4.md`+`6`; #48 §4.6; #39 §5 (§1.4(e)) |
| A producer emits only types it owns | #49 KD-6 (XC-051-008) |
| The renderer references each **built** producer | #49 KD-6 (XC-051-009) |
| `ERR-048-001` and `ERR-038-004` are free | `spec-error-log.md` + a sweep of every spec folder (§8.2) |

## 9.6 Decision

**APPROVED — July 27, 2026.** Lead-developer **R-01..R-05 sign-off granted**, and the back-props filed and RESOLVED **atomically with the flip** per this spec's own promotion pipeline step 6: **ERR-048-001**, **ERR-038-004** (`spec-error-log.md` v1.47). All 11 section files carry `Status: APPROVED`; the `SPEC_INDEX.md` row records the date.

**What approval does and does not mean here.** It approves the **forward design** — the #21–#30 pre-T0 precedent — not an implementation: #51 has **no `src/` assembly**, and its §7 T-phase plan is the sequence for building one. Items listed as *not gating* above remain open by design and are named at their tiers.

**The prior decision text is retained below, because the reasoning it records is what the sign-off was granted against.**

**(prior, recorded at `IN REVIEW`)** G1 is closed. **G2, G3 and G4 remain open**, and G3 cannot be closed by the
author: lead-developer R-01..R-05 sign-off is a human authority, not self-grantable, per the promotion
pipeline. The spec does **not** claim `APPROVED`, and the flip lands the §8.2 back-props atomically with
it.

**One reviewer question should be settled before anything else, because it is not #51's to settle alone:
ERR-048-001.** #48 is APPROVED and its FR-MP-027 currently instructs implementers to key #51's catalogue
on `CueId` — which the same key decision forbids two paragraphs earlier. #51 cannot resolve that by
building around it; either #48's sentence is corrected, or #51's KD-1 is wrong and the two id spaces
should be one. **The correction changes no #48 code, contract or test**, which makes it cheap to land and
easy to defer — and deferring it means the next implementer of either spec builds the forbidden reference
in good faith and discovers it as an assembly cycle, after both specs are APPROVED (R-6).

**A second, smaller question is whether #38 accepts ERR-038-004.** If it declines, #51 proceeds with
FR-AU-022's in-memory fallback rather than a private file — a worse product outcome and a strictly better
architectural one, and the trade should be made knowingly rather than by default.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial checklist. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 recorded: **0H + 4M + 5L**, all fixed in the v0.2 files; AR-2 sweep **0H + 0M + 2L → CONVERGENCE**. The four M were the missing ducking-table well-formedness rule (a self-ducking row has no error and no recovery), the declined-ownership case having a risk but no requirement (the obvious fallback being the one outcome KD-3 exists to prevent), `NoCaption` being satisfiable by reflex without a justification requirement, and §6's over-simple "no loop path" classification, which omitted that `Play` runs on #48's tick thread and therefore has the one cadence that can slow the **simulation**. §9.6 puts ERR-048-001 first as the question #51 cannot settle alone. |
| 0.3 | 2026-07-27 | — | **`IN REVIEW → APPROVED`.** Lead-developer R-01..R-05 sign-off granted. Back-props **ERR-048-001**, **ERR-038-004** filed and RESOLVED atomically with the flip (`spec-error-log.md` v1.47). Gates G2–G5 closed; §9.6 decision updated. All 11 section files flip to `Status: APPROVED`. |
#endregion
