# Localization & Accessibility #49 — Section 7: Forward Extensions

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (1H+1M+1L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 Wave-8 locale content (the content tier)

This slice pins the **seam + contract**; Wave 8 adds **translated locales** as **data** on the same seam.
Adding a locale is: supply, per `TextTemplateId`, `variantCount(Id)` localized templates (falling back to
base per `(Id, variant)`, KD-5); supply the static keys and the per-`EventKind` clauses; declare any
plural/gender categories the language needs (KD-3). **No new plumbing** — the renderer, the boundary, and
the producers are unchanged. The base locale is the identity later locales modulate (§8 anchor).

## 7.2 The accessibility content surface (KD-4 → Wave 8)

This slice records only the a11y **boundary** (a read-only presentation settings value; no sim reference, no
save impact). Wave 8 adds the **content**: the option catalogue (text-scale steps, high-contrast toggle,
colourblind-safe palette reusing the `dataviz` colour discipline, input-assist options), its client-settings
store, and the UI bindings. All stay client-local and display-time — the boundary this slice pins guarantees
they never touch serialized state.

## 7.3 Producer bindings (#35 / #46 / #38-static)

Each future text producer binds to the seam as it is authored (KD-1) — **by adding a sibling boundary
adapter (§2.2.1), never by changing the generic core seam** (FR-LC-013; the extensibility guarantee):
- **#38 static UI strings** — emit `LocalizationKey`s; the base-locale catalogue gains a static-string
  section. No new adapter and no new sim-side reference (static keys are #49-core's own type).
- **#35 media / #46 news-inbox procedural text** — emit their native template identity + slots + draw (if
  they draw); a **new boundary adapter** (`MediaTextBoundary`, `InboxTextBoundary`) is added **when that
  producer is built** (FR-LC-013), each referencing its producer and mapping its native slots into the
  generic `LocalizedTextRequest` / `TextTemplateId (producerTag, localOrdinal)`. The **core `ILocalizer` /
  `TextTemplateId` / `LocalizedTextRequest` are unchanged** — the adapter absorbs the per-producer shape
  (the #22 boundary-adapter split is exactly what makes this a data/adapter add, not a core rewrite). Each
  producer's own spec carries the coverage-lock (FR-LC-002) and its catalogue coverage assertion
  (FR-LC-008a extended to that producer's roster).

The single-seam invariant is only as strong as producer discipline; the eventual §1 of each producer spec
cites KD-1 verbatim so a producer cannot silently bake a localized string.

## 7.4 Grammar depth (Stage-3+ deferral)

KD-3 bounds the template model to named-placeholder substitution + a fixed plural/gender category selector.
Deeper grammatical agreement — case-declension synthesis, gender/number concord engines, morphological
inflection — is a **Stage-3+ deferral**, recorded here so a locale author cannot silently expand the model
into an unbounded agreement engine. A language needing more chooses among **more authored variants** rather
than synthesizing morphology at runtime.

## 7.5 The #22 retrofit (T-phase, this spec's own first code step)

The retrofit (§4, FR-LC-016) is sequenced as a T-phase: `InteractionTextGenerator.Generate` returns native
values, `InteractionTextCorpus` migrates to the base-locale catalogue, and the base-locale output is proven
byte-identical (Appendix C / T-LC-IDENTITY-001). It is behaviour-neutral at the base locale despite changing
#22's public return type — the one real API change this spec introduces, forward-designed here and applied
post-APPROVED like #21–#38 code.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial forward extensions: Wave-8 locale content + the a11y content surface; #35/#46/#38-static producer bindings; the grammar-depth Stage-3+ deferral; the #22 retrofit T-phase. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L; H-1 generic-core / per-producer boundary-adapter split, M-1 FR-LC-008a construction-time roster-coverage invariant, L-1 `{score}` derived) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
