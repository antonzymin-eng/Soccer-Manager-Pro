# Spec Stress-Test Strategy

> **Created:** May 18, 2026
> **Owner:** Lead developer (executor); any AI agent (probe runner)
> **Purpose:** Systematically stress-test all 20 approved specs along three orthogonal axes — **structural soundness**, **maintainability**, and **extendibility** — before any `src/` code is written.
> **Scope:** All files under `docs/specs/**` and the cross-spec contracts they encode. Out of scope: `src/` (does not exist yet).
> **Status:** v0.1 — initial design.

---

## 1. Why this exists

Stage 0 specification is complete: 20/20 APPROVED. Before implementation begins, the spec corpus is the single source of correctness — every bug that ships in `src/` will trace to either (a) code drift from spec, or (b) a defect already latent in the spec. This strategy attacks (b) before it becomes expensive.

The project has a documented history of recurring defect classes (see CLAUDE.md "Things That Have Gone Wrong Before"). Those classes form the threat model for this strategy. New defect classes discovered during execution must be back-ported into this document as new probes.

---

## 2. Threat model (defect classes)

Each row is a known or anticipated failure mode. Every probe in §4 maps to one or more rows.

| ID  | Defect class | Evidence | Severity |
|-----|--------------|----------|----------|
| T-01 | Stale spec numbers in cross-references | Decision Tree #7→#8 cascade; "FORMER NUMBERING" in SPEC_INDEX | High |
| T-02 | Fabricated approval-checklist entries | Pass Mechanics #5 audit (19 findings) | High |
| T-03 | Inverted domain conventions (fatigue, coords, axes) | Pass Mechanics §2 FR-02; Agent Movement §3.5 "pitch center" | High |
| T-04 | Untagged or mis-tagged constants ([GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]) | Multiple `[EST]→[GT]` promotions across #11, #12, #13 | Medium |
| T-05 | `[CROSS-PENDING]` left dangling after upstream APPROVED | ERR-011-001, ERR-012-001, ERR-013-005, ERR-014-004, ERR-015-001 | High |
| T-06 | Phantom interfaces (interface written against unspecified consumer) | ERR-001, ERR-004 | Medium |
| T-07 | Cross-spec citation drift (`#X §Y.Z` points to nonexistent or renamed section) | ERR-018-001 (#18 → #16 §7 vs §5 vs §3.2.4.1) | High |
| T-08 | DOMAIN_TAG collisions or gaps | ERR-011-001 `0x17`→`0x1D` shift after #12 first-to-APPROVED | High |
| T-09 | KD-2-style sequencing inversion (consumer APPROVED before producer) | #18/#19/#16 Tier 2 gating | High |
| T-10 | Channel-registry rows missing or unanchored | ERR-013/014/015 channel rows deferred to Stage 1 | Medium |
| T-11 | Float-vs-Fixed64 leakage in Stage 0 specs | #9 §8.1 Stage 5+ binding | Medium |
| T-12 | RNG non-determinism (`System.Random`, `DateTime.Now`) | CLAUDE.md "When Writing Code" | High |
| T-13 | Magic numbers in formula prose (constants not in catalogue) | Pass Mechanics F-A02 (WINDUP_FRAMES localization) | Medium |
| T-14 | Tick-rate conflation (10 Hz tactical vs 60 Hz physics) | KD-8 loop-separation tagging | Medium |
| T-15 | Citation/DOI fabrication | OI-003 (Bull 1985, Auger & Pellegrini 2007 — unfindable) | High |
| T-16 | Approval-status drift between `SPEC_INDEX.md` and spec §9 | Repeated reconciliation entries May 12–18 | Medium |
| T-17 | File-manifest drift after rename/move | `file-manifest.md` reconciliation history | Low |
| T-18 | Open-Issues entries with no "since" date or stale resolution | This file's own OPEN ISSUES discipline | Low |

---

## 3. Test axes

Each probe is classified by the axis it stresses. Some probes span axes; the table records the **primary** axis.

### 3.1 Structural soundness
Does the spec, as written today, internally cohere and externally agree with the rest of the corpus?
- Internal: every cross-ref resolves; every constant tagged; every formula has units and a worked example; no inverted conventions.
- External: every `#X §Y.Z` citation points to a section that exists in the current text of `#X`; every `[CROSS]` value matches its authority.

### 3.2 Maintainability
If we change one thing, what is the blast radius — and is that radius bounded by the design, or accidental?
- Local change cost: changing a `[GT]` constant value should require zero spec re-approvals (only re-validation of tests).
- Renumbering cost: simulated rename of spec N should produce a bounded, greppable change set.
- Audit reproducibility: every Approval Checklist must be re-verifiable by a fresh agent against the file tree alone.

### 3.3 Extendibility
Can the architecture absorb the additions that are already on the roadmap (Stage 1+), without forcing retroactive spec rewrites?
- Add spec #21 (e.g., Substitutions, Set Pieces): does SPEC_INDEX, DOMAIN_TAG allocation, channel-registry slot it in?
- Activate a Stage 0+1 carve-out (e.g., #10 KD-18 jump-kinematics retiring to AM #2 native Z): does the cited migration row exist on both sides?
- Add a new event channel: does Appendix F.0 of #18 + #17 §3.10 schema accept it without schema change?
- Swap deterministic backend (`float` → Fixed64 at Stage 5+): does the contract surface tagged by #9 §8.4 cover all consumers?

---

## 4. Probe catalogue

Probes are partitioned into **Tier A** (automated, fast, greppable) and **Tier B** (manual, adversarial, slow). Tier A runs on every push to `docs/specs/**`. Tier B runs on a cadence (§6).

### 4.1 Tier A — automated probes

Each Tier A probe is a script in `tools/spec-stress/` (to be authored after this strategy is approved). Probes are pure (no network, no LLM) so they can run in CI.

| Probe ID | Threat covered | Mechanism | Pass criterion |
|----------|----------------|-----------|----------------|
| A-01 spec-number-resolver | T-01, T-07 | Parse every `#\d+ §[\d.]+` token; resolve N against `SPEC_INDEX.md`; resolve `§Y.Z` against headings in `docs/specs/<folder>/section-*.md` | Every token resolves; zero stale-number hits against the "FORMER NUMBERING" table |
| A-02 constant-tag-lint | T-04, T-13 | Grep every numeric literal in spec body text; require adjacency to one of `[GT]`/`[EST]`/`[FIXED]`/`[DERIVED]`/`[CROSS]`/`[CROSS-PENDING]` or a `<<CATALOGUE:…>>` marker | Zero untagged literals outside Appendix-A derivation prose |
| A-03 cross-pending-tracker | T-05 | Find every `[CROSS-PENDING]`; require a matching `ERR-NNN-NNN` row in `spec-error-log.md` with `Status: OPEN` | Every CROSS-PENDING has a tracked upstream; zero orphans |
| A-04 domain-tag-allocator-audit | T-08 | Parse `#16 §3.4` table; assert every `DOMAIN_TAG_*` value is unique, contiguous-with-gaps-documented, and ≤ 0xFF | Zero collisions; zero unallocated tags referenced elsewhere |
| A-05 fatigue-convention-guard | T-03 | Grep for "fatigue" within 50 chars of "1.0" or "rested" / "fresh" / "1 = rested" / "0 = fatigued" | Zero hits matching inverted form |
| A-06 coordinate-convention-guard | T-03 | Grep for "pitch center" / "center origin" / "centre of pitch" / "(52.5, 34, 0)" used as **origin** | Zero hits; corner-origin only |
| A-07 approval-checklist-verifier | T-02 | Parse every `section-9-approval-checklist.md`; for each "✓ verified at §Y.Z" entry, confirm the cited heading exists in the cited file | Zero unverifiable entries |
| A-08 status-reconciliation | T-16 | Compare each spec's `section-9` Approval row vs `SPEC_INDEX.md` row | Zero disagreements |
| A-09 file-manifest-sync | T-17 | Diff `file-manifest.md` against `find docs/specs -type f` | Zero missing or extra rows |
| A-10 rng-leak-guard | T-12 | Grep all specs for `System.Random`, `DateTime.Now`, `Random.value`, `UnityEngine.Random` | Zero hits outside deferred Stage 5+ sections |
| A-11 float-leak-guard | T-11 | Grep all approved Stage 0 specs for `Fixed64` outside §1.3 dependencies, §7 deferrals, and #9 itself | Zero unexpected hits |
| A-12 tick-rate-tag-guard | T-14 | Every "Hz" or "ms" literal must be tagged `[LOOP:tactical]` or `[LOOP:physics]` (KD-8) in §6 perf tables | Zero untagged loop-rate references |
| A-13 doi-resolver | T-15 | For every `doi.org/...` link in §8 References, HEAD-request and assert 200/302 | Zero 404s |
| A-14 phantom-interface-guard | T-06 | Every `interface I…` in spec §4 must have **both** producer and consumer specs cited in §1 dependencies | Zero one-sided interfaces |
| A-15 open-issues-staleness | T-18 | Every CLAUDE.md OPEN ISSUES entry must carry a "since" date; flag entries older than 30 days without status change | Zero undated; warn ≥30 days |

### 4.2 Tier B — manual / adversarial probes

These probes require judgment. Each is a written exercise producing a dated report under `docs/tracking/stress-reports/YYYY-MM-DD-<probe>.md`.

| Probe ID | Axis | Exercise |
|----------|------|----------|
| B-01 renumber-simulation | Maintainability | Pick a random spec N (excluding #16, #8 — too central). Simulate renaming to N+10. Run A-01 with the rename overlay; count files touched and whether tag/error-log entries surface correctly. **Target:** ≤ K files per spec where K is bounded by `cross-references-incoming` count. |
| B-02 spec-21-injection | Extendibility | Author a 1-page outline for a hypothetical Spec #21 (e.g., "Substitutions"). Walk it through: SPEC_INDEX row, DOMAIN_TAG allocation against #16 §3.4, channel-registry rows against #18 Appendix F.0, KD-2 sequencing position. **Target:** no template change required. |
| B-03 carve-out-activation | Extendibility | For each Stage 0+1 carve-out (catalogue them first), trace the activation path end-to-end on paper. KD-18 jump-kinematics (#10→#2) is the canonical example. **Target:** every cited migration row exists on both sides. |
| B-04 constant-blast-radius | Maintainability | Pick five `[GT]` constants at random. For each: change value by ±50%; identify every test, formula, and derived constant that must re-validate. **Target:** blast radius is fully enumerable from `[DERIVED]` back-links — no surprise dependencies. |
| B-05 adversarial-pair-audit | Structural soundness | Pick two specs with a cross-spec dependency (e.g., #5 ↔ #1, #14 ↔ #8). Read both as if you'd never seen them; flag every disagreement in vocabulary, units, or directionality. **Target:** zero findings per pair after first cycle. |
| B-06 fixed64-swap-rehearsal | Extendibility | Walk the corpus as if Stage 5 begins tomorrow. Identify every `float` that must promote to `Fixed64`, every comparator that needs an epsilon, every RNG call site. **Target:** the work list is producible from #9 §8.4 alone; no `grep -r float` archaeology. |
| B-07 multiplayer-rehearsal | Extendibility | Walk the corpus as if Stage 5 multiplayer begins tomorrow. Identify every state-snapshot replacement, every "single-machine determinism" assumption (per CLAUDE.md) that must be re-verified. **Target:** work list bounded by #16 §3 + #9 §6.6. |
| B-08 fabricated-citation-hunt | Structural soundness | For each spec §8 References list, pick three citations at random and verify against primary source (DOI, ISBN, conference proceedings). Repeats OI-003 discipline across all 20 specs. **Target:** zero fabrications. |
| B-09 audit-reproducibility | Maintainability | Hand a fresh AI agent the spec folder and the Approval Checklist with verification claims **stripped**. Ask it to re-verify each line. Compare to original. **Target:** ≥ 95% agreement. |
| B-10 KD-2-rollback-drill | Maintainability | Pick an APPROVED upstream (#16 or #8). Simulate its rollback to IN REVIEW (e.g., a critical audit finding). Enumerate every downstream that must follow. **Target:** cascade is computable from `[CROSS]` / `[CROSS-PENDING]` graph; no judgment calls needed. |

---

## 5. Pass/fail criteria and reporting

### 5.1 Per-probe outcome

Each probe produces one of: **PASS**, **WARN** (advisory, non-blocking), **FAIL** (blocks the gate it feeds).

### 5.2 Aggregate gates

| Gate | Definition | Blocks |
|------|------------|--------|
| **G-Push** | All Tier A probes PASS or WARN | Merge to `main` |
| **G-Code** | All Tier A PASS; B-01, B-02, B-04 run within 30 days with FAIL=0 | First `src/` commit |
| **G-Stage1** | All Tier A+B PASS within 60 days | Stage 0→Stage 1 promotion |

### 5.3 Reports

- Tier A: machine-readable JSON written to `tools/spec-stress/reports/latest.json`; summary appended to `docs/tracking/stress-reports/tier-a-log.md` (one row per run).
- Tier B: one Markdown report per probe execution; index in `docs/tracking/stress-reports/INDEX.md`.

---

## 6. Cadence

| Trigger | Probes run |
|---------|------------|
| Push to `docs/specs/**` | Tier A subset relevant to changed files (precomputed dependency map) |
| Weekly cron | Full Tier A |
| Monthly | One Tier B probe (rotate B-01..B-10) |
| Before any spec status flip (any direction) | A-01, A-03, A-05, A-07, A-08 |
| Before first `src/` commit (G-Code) | Full Tier A + B-01, B-02, B-04 |
| Before Stage 1 promotion (G-Stage1) | Full Tier A + all Tier B |
| After any new defect class is discovered in the wild | Add a new T-NN row in §2; add at least one probe in §4 |

---

## 7. Implementation plan

This document is the **strategy**. The deliverables it implies are tracked separately:

1. **Tools** — `tools/spec-stress/` directory containing one script per Tier A probe. Language: Python (matches existing tooling style implied by CLAUDE.md "SplitMix64 in Python tooling" note).
2. **Probe-dependency map** — `tools/spec-stress/probe-deps.toml` mapping each probe to the file globs that, when changed, require its re-run.
3. **Report skeletons** — `docs/tracking/stress-reports/tier-a-log.md`, `…/INDEX.md`, `…/template-tier-b.md`.
4. **CI hook** — runs Tier A on push; opens an issue tagged `spec-stress-fail` on any FAIL.

None of (1)–(4) are written yet. They are authored only after this strategy itself is reviewed and approved.

---

## 8. Non-goals

- **Code-level testing.** That is #19's job once `src/` exists. This strategy stops at the spec-corpus boundary.
- **Replacing adversarial review passes.** PASS-1 / PASS-2 critiques on individual specs continue as before. This strategy operates on the assembled corpus, not on a single spec under draft.
- **Auto-fixing failures.** Every FAIL is escalated to a human (or a fix-pass AI agent invoked deliberately). No probe rewrites spec content.

---

## 9. Open questions

| ID | Question | Owner | Since |
|----|----------|-------|-------|
| Q-01 | Is Python the right language for Tier A, or should we use a Unity-friendly C# tool so it shares CI with `src/` once code exists? | Lead dev | May 18, 2026 |
| Q-02 | Should B-09 (audit reproducibility) be run by a different model family than the one that wrote the spec, to avoid self-confirmation bias? | Lead dev | May 18, 2026 |
| Q-03 | What is the right cadence for B-13 / DOI re-verification — once is enough, or quarterly? | Lead dev | May 18, 2026 |
| Q-04 | Should the probe-dependency map be hand-authored or derived from a parse of `[CROSS]` tags? | Lead dev | May 18, 2026 |

---

## 10. Version history

| Version | Date | Change |
|---------|------|--------|
| v0.1 | May 18, 2026 | Initial draft. 18 threat-model rows; 15 Tier A probes; 10 Tier B probes; 3 aggregate gates. |
