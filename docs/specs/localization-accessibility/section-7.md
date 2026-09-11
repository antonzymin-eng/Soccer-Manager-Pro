# Localization & Accessibility #49 — Section 7: Forward Extensions

**Created:** July 23, 2026
**Last Updated:** September 11, 2026 (v0.5 — L1 implements fixes for ERR-049-002/004; authoritative tracker closure remains landing-closeout work; ERR-049-005 fixed to L2)
**Last Updated (prior):** September 9, 2026 (v0.4 — L0R records ERR-049-002/003/004/005 with named T0/T1 discharge stages under C6; no contract hardening)
**Last Updated (prior):** September 7, 2026 (v0.3 — L0R records ERR-049-002/003 for T0/T1 discharge under C6; no contract hardening)
**Last Updated (prior):** July 23, 2026 (v0.2 — section-file PASS-1 (1H+1M+1L) → AR-2 convergence; APPROVED)
**Version:** 0.5
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

## 7.6 Recorded implementation-time defects and L1 dispositions

The L0R pass filed four implementation-time defects before a production localization assembly existed. L1
now supplies the executable core seam and implements the two fixes whose proof is purely structural. The
authoritative `spec-error-log.md` rows remain OPEN until the landing-closeout pass synchronizes those rows
with current-head executable evidence; this section does not pre-empt that tracker state. The other two
remain open for their named later slices.

- **ERR-049-002 — L1 FIX IMPLEMENTED; tracker closure pending landing close-out.** §1 KD-6 and the dependency
  table now state the already-approved architecture consistently: `TacticalDirector.Localization` references
  no sim/producer assembly; later producer-specific mapping belongs in a sibling boundary adapter. The L1
  production asmdef has an empty production-reference list; L1 tests parse that declared list, verify no
  other production asmdef references localization at this slice, and reflect the public type shape to exclude
  sim-owned types. The assembly-tier checker independently enforces the tier direction across the whole
  production graph. Historical PASS-1 text in §9 remains historical evidence.
- **ERR-049-003 — OPEN; discharge in L3B / #49 T1.** §2.2 defines `TextTemplateId.ProducerTag` as an integer
  identity but still assigns no symbolic allocation owner/table or append-only allocation rule, while later
  producer specs use symbolic catalogue identities. L3B must preserve the generic integer identity, allocate
  only built producers, make allocations append-only/non-reusable, and mechanically prevent collisions in
  `(ProducerTag, LocalOrdinal)`.
- **ERR-049-004 — L1 FIX IMPLEMENTED; tracker closure pending landing close-out.** §2.2 and the L1 core now
  define typed locale-neutral selector operands carried separately from preformatted string slots.
  `SelectorOperand` carries a cardinal value and/or bounded grammatical-gender value; `NamedSelectorSet`
  defensively copies caller storage, canonicalizes names, rejects duplicates, and participates in deterministic
  value equality/hash. `LocalizedTextRequest` carries that set without locale state, persisted state or RNG
  behavior. L1 tests reflect the operand shape, lock deterministic hashes, prove retained caller arrays cannot
  mutate the sets, and verify the core exposes no mutable static/RNG/persistence state. L2 remains responsible
  for mapping those operands to locale-authored `one/few/many/other`/gender sub-forms; that renderer behavior
  is outside this ERR's structural discharge.
- **ERR-049-005 — OPEN; discharge fixed at L2.** L1 keeps the approved public seam final:
  `string Resolve(LocalizationKey key)` and `string Render(in LocalizedTextRequest req)`. No Try/found-not-found
  signature is introduced. L2 must therefore resolve the still-undefined production terminal case for a
  static `LocalizationKey` absent from both selected and base catalogues, either by construction coverage for
  every admitted static key or by one explicit production-safe terminal result. L2 must prove no throw and no
  mutation for that terminal path (or kill the corresponding missing-key construction mutant).

The owning `spec-error-log.md` rows are the durable status authority and must be synchronized before this L1
landing is merge-ready.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial forward extensions: Wave-8 locale content + the a11y content surface; #35/#46/#38-static producer bindings; the grammar-depth Stage-3+ deferral; the #22 retrofit T-phase. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 fixes: H-1 generic core / per-producer boundary-adapter split (§2.2 core references nothing sim-side; §2.2.1 `LivingWorldTextBoundary`); M-1 FR-LC-008a construction-time roster-coverage invariant + F1/F5 rewrite + FR-LC-015 intent-value gate; L-1 `{score}` derived → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
| 0.3 | 2026-09-07 | — | L0R record-only pass: filed ERR-049-002 (stale KD-6 reference direction; deferred wholly to L1/T0) and ERR-049-003 (missing ProducerTag allocation ownership/collision contract; deferred to L3B/T1). C6 forbids pre-T0 hardening, so no normative fix is landed in this slice. |
| 0.4 | 2026-09-09 | — | L0R record-only scope extended to ERR-049-004 (missing typed plural/gender selector operand; deferred wholly to L1/T0) and ERR-049-005 (undefined static-key terminal fallback; resolution family decided at L1, discharge in L1 if signature-changing otherwise L2). Existing ERR-049-002/003 dispositions unchanged. C6 still forbids pre-T0 normative hardening. |
| 0.5 | 2026-09-11 | GPT-5.6 Sol | **L1 implementation disposition.** ERR-049-002 and ERR-049-004 fixes are implemented with structural tests, but their authoritative tracker rows remain OPEN until landing-closeout synchronization. The approved `Resolve`/`Render` signatures remain final, so ERR-049-005 is explicitly assigned to L2. ERR-049-003 remains open for L3B/T1. |
#endregion
