# Localization & Accessibility #49 — Section 9: Approval Checklist

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.3 — PASS-1 (1H+1M+1L) → AR-2 convergence; R-01..R-05 signed; APPROVED)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/localization-seam-template-design.md` v0.2

---

Checklist entries are verified against real source; nothing is checked without a programmatically verifiable
anchor (CLAUDE.md "Never fabricate verification values"). This is a **forward-design** spec — implementation
gates are open by construction (nothing is built yet); review gates track the pipeline.

## 9.1 Content gates

- [x] Every Appendix A constant carries exactly one source tag (`[FIXED] BaseLocale`).
- [x] No `[EST]` tags present.
- [x] Every §3 contract has rules + a worked example (§3.6 / App. D render; §3.4 pre-draw split; App. C
      retrofit byte-identity table).
- [x] KD scope stated: seam + template contract only; translated locales + a11y content deferred to Wave 8
      (§1.3 / §7.1 / §7.2).
- [x] KD-2 localize-after-generate boundary stated with the save-round-trip consequence (§1.6 / §3.3).
- [x] KD-6 one-way reference direction stated (no sim assembly references #49; producer emits native
      values) (§1.6 / §4.1).

## 9.2 Implementation status (forward design — nothing built yet)

- [x] FR set complete + stable: FR-LC-001..020 + FR-LC-008a (grep-verified in §2/§5).
- [ ] `TacticalDirector.Localization` seam + base-locale catalogue — **NOT STARTED** (T0).
- [ ] #22 retrofit (`Generate` returns native values; corpus migrates; base-locale identity lock) — NOT
      STARTED (T1).
- [ ] Wave-8 locale content + a11y content surface — NOT STARTED (Wave 8).

## 9.3 Review gates

- [x] **PASS-1 adversarial review of the section files — RUN July 23, 2026 (results in §9.3.1); all fixed.**
- [x] **AR-2 convergence sweep — RUN July 23, 2026 (results in §9.3.1); CONVERGENCE.**
- [x] **Lead-developer R-01..R-05 sign-off — GRANTED July 23, 2026 (§9.5).**

### 9.3.1 PASS-1 / AR-2 record

**PASS-1 — July 23, 2026 (1H+1M+1L); all fixed.**
- **H-1 (extensibility / self-contradiction with the cited #38 rule):** the generic seam was coupled to the
  concrete #22 producer at three core-contract points — an `ILocalizer.Render(InteractionIntent, ulong, in
  InteractionSlots)` overload, a core `TextTemplateId.ForInteraction(InteractionIntent)` factory, and a
  `LocalizedTextRequest` modeling slots as fixed `subject/opponent/score` (InteractionSlots' shape). Each
  forced `TacticalDirector.Localization`'s **core** to reference `living-world` and would force a
  core-contract rewrite when #35/#46 land with disjoint slots — contradicting §4.1's own cited #38 rule
  ("generic substrate references nothing sim-side; concrete surfaces reference only built assemblies").
  Fixed structurally: the core is producer-agnostic (`ILocalizer` takes only `LocalizedTextRequest`/
  `LocalizationKey`; `TextTemplateId` = generic `(producerTag, localOrdinal)`; slots = producer-agnostic
  `NamedSlotSet`), and the #22 coupling lives in a **per-producer boundary adapter** `LivingWorldTextBoundary`
  (§2.2.1, a separate `localization-boundary` assembly) — the ONLY sim-side reference. #35/#46 add sibling
  adapters, never a core change (§2.2/§4.1/§4.2/§7.3/App. B/D).
- **M-1 (cross-assembly coverage invariant unenforced):** after the corpus migrates to #49, `living-world`'s
  pre-draw gate can no longer be `TemplatesFor` (now #49 content); §3.4/FR-LC-015 conflated the intent-VALUE
  gate (locale-independent, sim-side) with corpus-COVERAGE. A defined `InteractionIntent` with no #49 row
  would draw a `world.text` cursor then hit `variantCount == 0` — a consumed-cursor-then-fail (breaking the
  no-cursor-on-refusal invariant) or a divide-by-zero; F5 as written only caught an explicit 0-count row.
  Fixed: the sim-side gate is the intent-VALUE roster check (`None` + enum-defined); new **FR-LC-008a**
  asserts **construction-time roster coverage** (every defined intent ≥1 base template, every citable
  `EventKind` a clause) fail-loud at catalogue construction — so `variantCount ≥ 1` holds by construction
  (§3.4 / FR-LC-008a/015 / F1/F5 rewrite / T-LC-PREDRAW-001).
- **L-1:** `{score}` is a *derived* placeholder (`HomeGoals.ToString(InvariantCulture) + "-" + AwayGoals…`),
  not a raw slot; pinned in §3.5 + App. B/C so base-locale expansion is byte-identical.

**AR-2 — July 23, 2026 (0H+0M; L-only ⇒ CONVERGENCE).** Re-read all 11 files: the core/boundary-adapter
split is consistent across §2.2/§2.2.1/§4.1/§4.2/§7.3/App. B/D and now MATCHES the cited #38 rule (generic
core references nothing sim-side; the sole `living-world` reference is the adapter); FR-LC-008a ↔ F5 ↔ §3.4
↔ T-LC-PREDRAW-001 are wired end to end; `{score}` derivation is pinned everywhere it appears. The only
residual is the intentional out-of-sequence `FR-LC-008a` label (placed next to its consumer FR-LC-015) — a
Low doc nicety, not gating. Cycle closes per the #21–#38 L-only-round convention.

## 9.4 Consistency gates

- [x] FR prefix `FR-LC-` verified unclaimed by grep over `docs/specs/**` (0 hits before this spec).
- [x] Candidate number #49 matches the roadmap / `spec-plans/spec-49-…` reservation; the row is scoped
      "seam + template contract" so the Wave-8 content tier is distinct later.
- [x] Cited source APIs verified against real files: `InteractionTextGenerator.Generate` / the `world.text`
      draw / `templates[draw % (ulong)length]` / `Expand` (incl. `{score}` = `HomeGoals.ToString(Invariant
      Culture) + "-" + AwayGoals…`) / `EpisodeClause`; `InteractionTextCorpus` (`TemplatesFor` None-row +
      throw; `EpisodeClause` six kinds); `InteractionSlots` fields; #38 FR-UI-004 / §7.3-KD-5.
- [x] `SPEC_INDEX.md` row added at promotion (`IN REVIEW`), flipped `IN REVIEW → APPROVED` at sign-off.
- [x] No #16 §3.4 cross-cite needed — #49 registers no domain tag / ordinal / RNG (FR-LC-017); a positive
      property, not a deferred allocation (no `_RESERVED_` placeholder).

## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: SIGNED — July 23, 2026.** PASS-1 → AR-2 converged (§9.3.1, 0H unresolved). This is a forward
> design (nothing built) — sign-off approves the DESIGN, exactly as #21–#38 were approved before their T0
> code; the §7 roadmap is the post-APPROVED sequence.

| # | Review gate | Evidence to confirm | Status |
|---|---|---|---|
| R-01 | **Content completeness** — §1–§9 + appendices per the template | all files | ☑ |
| R-02 | **Technical accuracy** — the seam / determinism-boundary / template-model / retrofit contracts internally consistent; 21 FRs (FR-LC-001..020 + 008a); the generic core / per-producer boundary-adapter split; constants one tag each, no `[EST]`; cited APIs verified against `living-world` + #38 | §2/§3/§4/App. A/B/C/D | ☑ |
| R-03 | **Cross-spec consistency** — no #16 §3.4 allocation (FR-LC-017); the no-reverse-reference + producer-emits-native-values invariants; the generic core references nothing sim-side, only the boundary adapter references a built producer (no phantom dependency); locale/a11y content deferred to Wave 8 | §4 / §7 | ☑ |
| R-04 | **Stage-binding correctness** — presentation/content-layer display-time transform (§1.2, not the sim loops); read-only / no persistent state / no format bump; locale + a11y content honestly Wave-8-deferred | §1 / §4 / §6 | ☑ |
| R-05 | **Approval granted** — PASS-1 + AR-2 resolved; `SPEC_INDEX.md` flipped `IN REVIEW → APPROVED` | ☑ |

## 9.6 Decision

**APPROVED — July 23, 2026.** The section files are authored from the converged design supplement (v0.2,
AR-1 2H+1M+1L → AR-2 clean); the section-file PASS-1 (1H+1M+1L) → AR-2 convergence is resolved (§9.3.1); no
#16 §3.4 cross-cite is needed (#49 allocates no determinism identifier, FR-LC-017); and lead-developer
R-01..R-05 sign-off is granted (§9.5). `SPEC_INDEX.md` row 49 flips `IN REVIEW → APPROVED` (31 APPROVED /
0 IN REVIEW). This approves the **forward design** (the #21–#38 pre-T0 precedent); the §7 plan (T0 generic
core seam + base-locale catalogue → T1 #22 retrofit via the boundary adapter + base-locale-identity lock →
Wave-8 locale + a11y content) is the post-APPROVED sequence. Post-APPROVED, non-blocking: the #22 retrofit,
each producer's boundary adapter (#35/#46/#38-static), and the Wave-8 locale + a11y content tier.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial checklist. Content/consistency gates checked; review + implementation gates OPEN by construction (forward design). Status IN REVIEW. |
| 0.3 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L: H-1 generic-core / per-producer boundary-adapter split; M-1 FR-LC-008a construction-time roster-coverage invariant; L-1 `{score}` derived) → AR-2 convergence recorded (§9.3.1); R-01..R-05 signed; §9.6 APPROVED. Status APPROVED. |
#endregion
