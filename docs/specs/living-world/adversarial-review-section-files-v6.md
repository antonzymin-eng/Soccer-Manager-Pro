# Living World #22 — Section-File Adversarial Review PASS-6

**Created:** June 21, 2026
**Reviewer:** fresh-eyes pass over `docs/specs/living-world/` section files v0.6
**Result:** 1 M + 2 L (no High). All resolved in the v0.7 fix pass (same day).

> PASS-6 again found a genuine (subtle) determinism issue rather than pure churn — RNG draw-order
> interleaving on a single shared stream.

---

## Medium

**M-1 — Single shared RNG stream + unspecified interaction-generation timing = draw-order interleaving
hazard.** FR-LW-020 / §4.4 mandate **one** dedicated world stream, and §3.3 draws text from it
(`rng.DrawReserved(worldStream)`). But the spec never pins **when** interaction text is generated
relative to the tick. If generation is **aperiodic** (player-triggered, e.g. opening a press conference),
its draws interleave with the **periodic** tick-driven arc draws (§3.4) on the **same cursor** — so the
cursor position at any given draw depends on player-action timing, and a single shared stream makes the
two draw sources order-dependent on each other. **Fix:** split into **dedicated sub-streams**
(`world.arcs` periodic, `world.text` aperiodic) so text draws never perturb the arc/world cursor; the
`DeterministicRngService` stream registry already supports this. State that text generation uses the
isolated `world.text` sub-stream, so its timing relative to ticks cannot affect tick determinism.

## Low

**L-1 — Optional inspector interaction-log must be determinism-neutral.** The PASS-5 optional
`(intent, cursor, snapshotRef)` log (§7.2 / §3.6) must be **side-effect-free** w.r.t. world state and
**excluded from the determinism digest**, or a debug build that keeps the log diverges from a release
build that doesn't. **Fix:** add the guardrail note.

**L-2 — §3.3 cites `T-LW-DET-*` by wildcard.** The text-reproducibility claim should cite the specific
test (`T-LW-DET-003`) and the `world.text` sub-stream cursor. **Fix:** tighten the citation.
