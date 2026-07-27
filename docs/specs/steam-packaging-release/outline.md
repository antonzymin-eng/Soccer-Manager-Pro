# Steam Packaging & Release Engineering #39 — Outline

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## Provenance

Promoted from `docs/tracking/steam-packaging-release-design.md` **v0.4** (AR-converged: AR-1 0H+2M →
AR-2 0H+1M → AR-3 0H+0M+2L = CONVERGENCE), itself promoted from
`docs/tracking/spec-plans/spec-39-steam-packaging-release.md` v0.1.

**FR prefix:** `FR-PK` · **Wave:** 8 (last) · **Tier:** S2 · **Assembly:** `TacticalDirector.Release`

## Section map

| § | Content |
|---|---|
| 1 | Scope, dependencies, the five verified facts — including **CI is skip-open** and **Cloud is the project's first second writer** — and KD-1..KD-7 |
| 2 | FR-PK-001..045, data structures, failure modes F1..F9 |
| 3 | FM-PK-01..04 — conflict resolution, gate evaluation, achievement evaluation and flush, sync on quiescence — with worked examples |
| 4 | Architecture: a leaf-but-for-#50 assembly, the shell-evaluated achievement inversion, and the release runbook as a process artifact |
| 5 | Test plan — the fail-closed property tested **as a property**, the conflict matrix and its metadata-refusal lock, observer-neutral achievements |
| 6 | Performance — no loop path at all; the costs are a sync boundary and a build |
| 7 | Future extensions, T-phase plan, risks R-1..R-7 |
| 8 | Cross-references XC-039-001..016; **no back-propagations at approval** |
| 9 | Approval checklist + the PASS-1 adversarial-review record |
| Appendices | A constants · B the release-gate evidence set · C the Cloud conflict matrix · D the store/compliance checklist |

## The one-paragraph summary

**#39 owns the build and packaging pipeline, the release gate, the Steam Cloud sync policy over the
existing save file, the achievement model as a read-only derivation, and the store/compliance checklist.**
It parses no save, compares no version, defines no determinism proof, and mutates no gameplay.

**Its central decision is an inversion, and verification is what forces it.** This repository's Unity CI
jobs are gated on a secret and report **success** when that secret is absent — the correct choice for CI,
since a contributor without secrets should not see red, and the exact wrong choice for a ship gate. A
green pipeline today is compatible with *"no player artifact was ever built and no Unity test ever ran"*,
which are the two facts a ship decision most depends on. **KD-2** therefore makes the gate
**evidence-positive and fail-closed**: it requires artifacts asserting that named checks *executed on a
named commit*, and treats absence as failure. *"Nothing was red"* is not an input.

**Its second decision bounds a genuinely new hazard.** Steam Cloud is the first **second writer** this
project has ever had to a save file whose entire discipline assumes one local atomic writer — and it can
deliver a save written by a **different build**. #39 syncs whole files at quiescent boundaries and
delegates **every** version judgement to **#50**, which already specifies `CompareForConflict` with the
dependency running `#50 → #39`.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial outline from supplement v0.4. Status IN REVIEW. |
#endregion
