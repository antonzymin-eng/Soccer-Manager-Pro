# Save Migration & Versioning #50 — Outline

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## Provenance

Promoted from `docs/tracking/save-migration-versioning-design.md` **v0.6** (AR-converged: AR-1 0H+2M →
AR-2 **1H** → AR-3 0H+2M → AR-4 0H+3M → AR-5 0H+0M+2L = CONVERGENCE), itself promoted from
`docs/tracking/spec-plans/spec-50-save-migration-versioning.md` v0.1.

**FR prefix:** `FR-MG` · **Wave:** 8 · **Tier:** S2 · **Assembly:** `TacticalDirector.SaveMigration`

## Section map

| § | Content |
|---|---|
| 1 | Scope, dependencies, the seven key decisions, and the two facts that shape the spec: the version surface is **25 constants**, and **the largest save-visible surface in this project is not versioned at all** |
| 2 | FR-MG-001..038, data structures, failure modes F1..F8 |
| 3 | FM-MG-01..05 — classification, the per-blob chain runner, the generation gate, the non-destructive write, the conflict comparison — with worked examples |
| 4 | Architecture: the leaf assembly, the delegate-registration inversion that makes the leaf claim true, and the load-path composition |
| 5 | Test plan — the classification matrix, the two **different** determinism properties, the generation lock, non-destructive refusal |
| 6 | Performance — a load-path cost, measured once per file open |
| 7 | Future extensions, T-phase plan, risks R-1..R-6 |
| 8 | Cross-references XC-050-001..016; back-props **ERR-030-019** and **ERR-027-003** |
| 9 | Approval checklist + the PASS-1 adversarial-review record |
| Appendices | A constants · B the `SaveOriginStamp` frame layout · C the version-surface inventory · D the classification matrix |

## The one-paragraph summary

**#50 is the layer that decides whether a save file from an older build may be opened, and turns it into
one the current codecs accept.** It classifies a save by reading **only** version fields — never a payload —
into `Current` / `Migratable` / `TooNew` / `Unsupported` / `Corrupt`; runs a **per-blob** chain of
transforms supplied by the specs that made each bump; and hands the result to the **unmodified** codec,
whose fail-loud gates still adjudicate. It bakes no string, holds no domain type, and references no spec's
assembly.

**Its load-bearing decision is the one the plan did not contain.** Rosters in this project are
**regenerated from the world seed, not saved**, so a change to the generator's draw order, the club-name
catalogue or the strength ramp rewrites the squads in every existing career — and **no format version
covers it, because nothing is serialized**. A #50 that migrated only formats would deliver a perfectly
migrated save containing a different team. `WORLD_GENERATION_VERSION` (KD-2) closes that door, at a cost
the spec states rather than discovers: materialising an old world requires running the **old generator**,
so the build must retain one per supported version.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial outline from supplement v0.6. Status IN REVIEW. |
#endregion
