# Localization & Accessibility #49 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (1H+1M+1L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 5.1 Test groups

### T-LC-SEAM — The single seam (FR-LC-001..004)
- **T-LC-SEAM-001** — `Resolve(key)` returns the keyed static string; `Render(req)` returns the expanded
  procedural string. Both are the sole surface-string entry points #49 exposes (a reflection/type-shape
  assert that no other public method returns a rendered string).
- **T-LC-SEAM-002** — **Coverage lock (routing):** #49 exposes no API that accepts and returns a baked
  human-readable string bypassing the catalogue (the routing check; per-producer emit-through-seam
  discipline is enforced at each producer's spec, FR-LC-002).

### T-LC-IDENTITY — Base-locale identity (FR-LC-016, the correctness anchor)
- **T-LC-IDENTITY-001** — For a fixed `(intent, draw, slots)`, `ILocalizer.Render` with only the base locale
  loaded returns **byte-identical** output to `InteractionTextGenerator`'s pre-retrofit result — the
  migrated corpus + `draw % variantCount` reproduce the exact template selection, slot expansion, and
  appended clause (the §3.6 / Appendix C worked case, plus a cited-episode case exercising the clause).
- **T-LC-IDENTITY-002** — `variantCount(BaseLocale, Id) == TemplatesFor(intent).Length` for every defined
  intent (the migrated corpus preserves counts).

### T-LC-DET — Localize-after-generate (FR-LC-005/006, KD-2)
- **T-LC-DET-001** — A `WorldStore` (and season) save produced under two different display locales is
  **byte-identical**; the `world.text` cursor and serialized memory match; only the rendered strings differ
  (the digest-lock class).
- **T-LC-DET-002** — Rendering (repeated `Render`/`Resolve`) draws from no RNG stream and advances no tick
  (a spy asserts zero draws / zero tick advances from the renderer).

### T-LC-FALLBACK — Fail-safe fallback (FR-LC-011, KD-5)
- **T-LC-FALLBACK-001** — A missing key / missing locale / missing `(Id, variant)` / missing clause renders
  the base-locale identity; **no throw**, **no** state mutation (F2/F4).
- **T-LC-FALLBACK-002** — In a dev build a missing entry MAY surface a `‹key›` marker; in a production build
  it MUST fall through to base (a build-flag-gated assert).

### T-LC-TEMPLATE — Template model (FR-LC-009/010, KD-3)
- **T-LC-TEMPLATE-001** — Named-placeholder substitution fills `{subject}`/`{opponent}`/`{score}`; a
  template declaring a plural/gender category selects the correct sub-form on the keyed slot.
- **T-LC-TEMPLATE-002** — Base-locale English (no categories declared) is identity with `.Replace`.
- **T-LC-TEMPLATE-003** — The citation clause is selected by `EventKind` (not the draw): varying the draw
  with a fixed `citationKind` yields the same clause; varying `citationKind` changes it (FR-LC-010).

### T-LC-PREDRAW — Pre-draw validation split (FR-LC-015, §3.4)
- **T-LC-PREDRAW-001** — A defined intent always has `variantCount(Id) ≥ 1`; the renderer never divides by
  zero. **Roster coverage (FR-LC-008a / F5):** a catalogue **missing** a base-locale row for a defined
  `InteractionIntent`, or missing a clause for a defined citable `EventKind`, or carrying an explicit
  0-count row, fails loud at **catalogue construction** — *before* any draw — not at render (so no
  consumed-cursor-then-fail).
- **T-LC-PREDRAW-002** — A refused producer call (None/out-of-roster intent, bad slots, sub-threshold
  citation) consumes **no** `world.text` cursor — the retrofit preserves the pre-draw refusal ordering
  (the slice-3 AR-1 L-3 invariant; this is a `living-world`-side lock, listed here as the retrofit's
  acceptance criterion).

### T-LC-LAYER — Reference direction (FR-LC-012/013/014)
- **T-LC-LAYER-001** — **No reverse reference:** no sim/loop assembly references
  `TacticalDirector.Localization` (a build/asmdef audit; F6).
- **T-LC-LAYER-002** — **Core references nothing sim-side:** `TacticalDirector.Localization` (core)
  references no producer assembly (asmdef audit) — the `living-world` reference lives only in the
  `localization-boundary` adapter assembly (FR-LC-012/013). The retrofit's `Generate` returns only
  `living-world`-owned types (a type-shape assert on the retrofit signature, FR-LC-014).

## 5.2 FR traceability

| FR | Test(s) |
|---|---|
| FR-LC-001..004 seam | T-LC-SEAM-001/002 |
| FR-LC-005/006 localize-after-generate | T-LC-DET-001/002 |
| FR-LC-007/008 variant selection + count | T-LC-IDENTITY-002, T-LC-PREDRAW-001 |
| FR-LC-008a roster coverage (construction) | T-LC-PREDRAW-001 |
| FR-LC-009/010 template model + clause | T-LC-TEMPLATE-001/002/003 |
| FR-LC-011 fallback | T-LC-FALLBACK-001/002 |
| FR-LC-012/013/014 reference direction | T-LC-LAYER-001/002 |
| FR-LC-015 pre-draw split | T-LC-PREDRAW-001/002 |
| FR-LC-016 base-locale identity | T-LC-IDENTITY-001 |
| FR-LC-017 no determinism ids | T-LC-DET-002 + T-LC-LAYER-001 (nothing to serialize to assert) |
| FR-LC-020 ulong draw | T-LC-IDENTITY-001 (the modulo matches `draw % (ulong)length`) |

## 5.3 Deliberately untested (out of scope)

- No translated-locale content tests (Wave 8 content; only the base-locale identity + the fallback
  *mechanism* are tested here).
- No a11y option-content / settings-store tests (Wave 8, KD-4).
- No UGUI rendering tests (the rendered string feeds #38's UI, which is Unity-host-gated).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial test plan (SEAM/IDENTITY/DET/FALLBACK/TEMPLATE/PREDRAW/LAYER) + FR traceability. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L; H-1 generic-core / per-producer boundary-adapter split, M-1 FR-LC-008a construction-time roster-coverage invariant, L-1 `{score}` derived) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
