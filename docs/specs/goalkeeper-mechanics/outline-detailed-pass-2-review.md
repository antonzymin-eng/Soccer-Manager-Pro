# Goalkeeper Mechanics #11 — `outline-detailed.md` v1.1 → Pass-2 Adversarial Review

**Created:** May 16, 2026, late evening (between `outline-detailed.md`
v1.1 and v1.2).
**Reviewer:** AI agent
(`claude/goalkeeper-mechanics-specs-pM9hR`, pass-2 critique role —
adversarial against v1.1, with awareness of pass-1's resolutions).
**Scope:** `outline-detailed.md` v1.1 only. Findings recorded here
for tracking; resolution required before v1.2 supersedes v1.1.
**Method:** Re-read v1.1 looking specifically for issues introduced
during the v1.0 → v1.1 fix pass (the common pattern from Heading
#10's pass-2 / #18's pass-2 — fixes can introduce new gaps), plus
any items pass-1 flagged for follow-up without resolving.

**Severity legend:** **H** = blocks v1.2 promotion; **M** = must
resolve in v1.2 to pass approval gate; **L** = follow-up worth
tracking but not v1.2-blocking.

**Totals:** 0 H / 2 M / 3 L = 5 findings. Pass-2 surfaces no
high-severity issues — v1.1's architectural KDs (KD-12, KD-13,
KD-14, KD-15, KD-16, KD-18) all land correctly.

---

## MEDIUM-SEVERITY FINDINGS

### P2-M-1 — `DRAW_SITE_HANDLING_NOISE` shared between two error sources.

**Location:** v1.1 §3.5 pseudocode.

**Problem:** §3.5 in v1.1 has two Gaussian draws (the handling-
scale `handlingNoise` and the contact-point `contactPointError`
noise) both wired to `DRAW_SITE_HANDLING_NOISE`. Pass-1 flagged
this in passing as a "recommendation beyond v1.1" (L-5 in
pass-1's recommendations section), but the v1.1 fix pass did not
land the split. Sharing a draw site between two error sources
violates the #16 §4.5 single-purpose-per-site rule.

**Why this matters:** during deterministic replay / digest
checking, the §16 RNG-draw digest (`SipHash-2-4-64` per #16
§3.2.5) is keyed by `(StreamKey, actionOrdinal, drawIndex)` —
if two semantically distinct draws share a `StreamKey`, the
draw-index ordering becomes the only disambiguator, and any
re-ordering of the two draws within the formula silently changes
the result without changing the spec. This is a determinism-
fragility hazard.

**Fix locus:** v1.2 splits the draw sites: `DRAW_SITE_HANDLING_NOISE`
(handling-scale Gaussian only) and a new
`DRAW_SITE_HANDLING_POINT_NOISE` (contact-point Gaussian). §4.4
draw-site count 3 → 4. §3.5 pseudocode updated. The fix is the
v1.1-pass-1-L-5 fix elevated to v1.2.

### P2-M-2 — `Ball.SetPossessor` surface presumed but not verified.

**Location:** v1.1 §4.3 / KD-21 / §3.5.

**Problem:** KD-21 specifies that the `Caught` band invokes
`Ball.SetPossessor(gkId)`, distinguishing it from the
`Parried`/`Deflected`/`Spilled` bands which invoke
`Ball.ApplyKick`. v1.1 §4.3 lists `Ball.SetPossessor` as an
output interface but does not verify that this method exists in
Ball Physics #1 §3.1. ERR-008 closed an earlier "no `PossessorId`
field" gap (Option B: possession external to `BallState`), but
the *setter* surface (`Ball.SetPossessor` vs. the implementation
path used by ERR-008's resolution) is not pinned in v1.1.

**Why this matters:** the entire `Caught` outcome branch of KD-21
depends on this surface. If it doesn't exist, the spec needs a
back-prop entry to add it as a non-behavioral patch to APPROVED
#1 — same pattern as `DOMAIN_TAG_HEADING = 0x16`. The risk is
ERR-006-class (Pass Mechanics §8 referenced `Ball.ApplyKick`
before §3.1.11 defined it).

**Fix locus:** v1.2 §4.3 explicit note: "presumed published per
ERR-008 resolution; back-prop entry filed if absent". OI-006
added to OPEN-ITEMS tracker. The verification itself is deferred
to `section-1.md` / `section-4.md` authoring (cannot be done at
outline stage without reading every Ball Physics §3 section).

---

## LOW-SEVERITY FINDINGS

### P2-L-1 — KD-12 has a dual reference that needs resolving.

**Location:** v1.1 KD-12.

**Problem:** v1.1 KD-12 says the GK ground re-entry on dive
landing "enters `GROUNDED` with `GroundedReason.DIVING_HEADER`
re-use OR a new `GroundedReason.DIVING_SAVE` enum value to AM #2".
The "OR" is unresolved. Heading #10 KD-18 set the precedent that
AM #2 amendments are avoided wherever possible (the preservation
of AM #2 APPROVED status is a project invariant).

**Fix locus:** v1.2 KD-12 resolved: Stage 0 re-uses `DIVING_HEADER`
(no AM #2 amendment); telemetry disambiguation via
`SaveAttemptedEvent.contactBodyPart` field; the `DIVING_SAVE`
enum value addition deferred to Stage 1+ §7.5 cleanup. OI-007 in
OPEN-ITEMS tracker.

### P2-L-2 — `FR-GK-026` atomic-resolution mechanism unclear.

**Location:** v1.1 §2.1 FR-GK-026.

**Problem:** v1.1 FR-GK-026 says "`DOMAIN_TAG_GOALKEEPER`
allocated `[CROSS-PENDING]`; resolved atomically on #16 back-
prop or on `IN REVIEW → APPROVED` transition for this spec".
The "or" here is ambiguous — is the resolution at #16 back-prop
landing, or at #11 status transition? These are two distinct
events (per Heading #10 OI-001 precedent: `DOMAIN_TAG_HEADING`
landed in #16 §3.4 v1.0.2 patch *concurrent with* #10's
`IN REVIEW → APPROVED` transition).

**Fix locus:** v1.2 FR refined: resolution is atomic with #16
back-prop landing (the `[CROSS-PENDING]` → `[CROSS]` promotion
in this spec); the #11 status flip is separately atomic for #12
GK constants (via KD-13). These are two independent atomic
events, not one disjunction.

### P2-L-3 — §6.3 cites Opta baselines without anchoring to §8.3.

**Location:** v1.1 §6.3.

**Problem:** §6.3 references "Opta baselines" for the cross-claim
duel rate estimate. §8.3 lists Opta/StatsBomb as a commercial-
data baseline class. v1.1 does not link the §6.3 invocation to
the §8.3 entry, so a future audit grep would flag the §6.3 cite
as unsourced.

**Fix locus:** v1.2 §6.3 anchors the Opta baseline cite to §8.3
explicitly. Mirrors Heading #10 pass-1 M-3 closure pattern.

---

## ITEMS PASS-1 RAISED THAT V1.1 RESOLVED CORRECTLY

For audit traceability — pass-2 confirms:

- KD-12 (dive Z-kinematics ownership) correctly mirrors Heading
  #10 KD-18; AM #2 APPROVED status preserved (subject to P2-L-1
  Stage 0/Stage 1+ split).
- KD-13 (#12 ratification protocol) is structurally complete:
  contract surface, ratification mechanism, atomic patch
  revision. §3.3.0 normative subsection adequately scoped.
- KD-14 (head/hand routing) correctly uses Collision System #3
  body-part determination, not intent — avoids the ERR-001
  phantom-interface class.
- KD-15 (rush abort policy) correctly mirrors Heading #10
  KD-17's intent-staleness posture.
- KD-16 (release-point ownership) correctly mirrors Heading #10
  KD-16's spin-transfer ownership.
- KD-18 (asymmetric reaction tolerance) correctly mirrors
  Heading #10 KD-2 / pass-1 H-1 fix.
- §3.4 master constants table at ~50 rows clears the M-8
  inventory-discipline gate.
- Six external references in §8.3 clear the L-6 sparseness gate.

---

## RECOMMENDATIONS BEYOND v1.2

None. Pass-2 returns 5 findings (0 H / 2 M / 3 L), all of which
are mechanical fixes addressable in a single v1.2 fix pass with
no architectural-level work. After v1.2 lands, the outline is
ready for section-file authoring with no further outlining
work — equivalent in maturity to Heading #10's
`outline-detailed.md` v1.1 at the moment its section files began.

A pass-3 review is NOT recommended. The diminishing-returns
inflection visible at Heading #10 (pass-2 returned 15 findings;
no pass-3 was performed) is reached here at pass-2 (5 findings,
all mechanical). Section-file PASS-1 adversarial review will
provide the next critique cycle.

---

## RESOLUTION SUMMARY

All 5 findings (0 H / 2 M / 3 L) addressed in
`outline-detailed.md` v1.2 (May 16, 2026, latest same evening).
Resolution mapping table is Appendix G of `outline-detailed.md`.
Pass-2 declares the outline clean: no remaining issues.
