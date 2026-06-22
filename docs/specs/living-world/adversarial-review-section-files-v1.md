# Living World #22 — Section-File Adversarial Review PASS-1

**Created:** June 21, 2026
**Reviewer:** adversarial pass over `docs/specs/living-world/` section files v0.1
**Result:** 4 M + 3 L (no High). All resolved in the v0.2 fix pass (same day).

> Scope note: the four passes recorded in `docs/tracking/living-world-system-design.md` reviewed the
> **design supplement**. This is the first review of the **promoted section files** — it targets gaps
> the template restructure (FR-ID assignment, traceability matrix, section split) can introduce that a
> prose supplement cannot exhibit.

---

## Medium

**M-1 — No behavioural-identity FR (regression contract missing).** §2.3 describes the neutral/empty
state but **no FR asserts** that a world with no recorded episodes and no spawned arcs reproduces the
canonical human-systems behaviour exactly. #21 carries this as FR-TI-031; the restructure dropped its
analogue, so the no-regression contract is untestable and absent from the §5.7 matrix. **Fix:** add
**FR-LW-034** (additive-only behavioural identity) + a determinism test (T-LW-DET-007).

**M-2 — Tick-order step 2 contradicts the no-write-back rule.** §4.2 step (2) reads "canonical
human-systems update (vol-2/vol-3, read-and-route)," implying the world loop *runs* the H-Gate/
propagation update — directly against FR-LW-027 / KD-9 (read-only, never write back). **Fix:** reword so
the human-systems update is owned by vol-2/vol-3 and the world loop only **reads its result**.

**M-3 — Layer applicability by node-type undefined.** `RelationshipEdge` carries `PlayerEdge`/`Affinity`
/`Trust` for every ordered pair, but each layer is only meaningful for certain pairings (`PlayerEdge`
player↔player; `Affinity` manager↔non-player). Unspecified → meaningless stored values and an ambiguous
F6 ([0,1]) invariant on inactive layers. **Fix:** pin which layers are active per node-type pairing
(FR-LW-005).

**M-4 — Background-tier authority boundary.** §3.5 says the background tier *simulates* "transfers,
sackings," which risks making the living-world layer the authority for off-screen club outcomes — a
tension with KD-9's read-and-route spirit and with vol-3 §2/§4 ownership. **Fix:** the background tier
**reflects/summarises** outcomes from the (abstracted) club-AI, it does not *drive* them.

## Low

**L-1 — Open-roster enums vs. stability test.** `EventKind`/`InteractionIntent` rosters are "finalised
at implementation" (Appendix C) yet FR-LW-028 requires a stability test. Clarify the APPEND-only test
**grows with the roster** (it locks existing ordinals; deferred membership is appended).

**L-2 — ERR-022-005 miscategorised.** The season-calendar clock is **this spec's own deliverable**
(§7.1), not a cross-spec back-prop; listing it as an ERR-022 row alongside genuine back-props is a
category error. Reword to flag it as a forward deliverable.

**L-3 — §3.1 update/decay formula not FR-cited.** The edge update/decay rule is tested (T-LW-U-005..010)
but not bound to an FR. Cite it under FR-LW-005.
