# Spec Stress-Test Strategy

> **Created:** May 18, 2026
> **Owner:** Lead developer (executor); any AI agent (probe runner)
> **Purpose:** Systematically stress-test all 20 approved specs along three orthogonal axes — **structural soundness**, **maintainability**, and **extendibility** — before any `src/` code is written.
> **Scope:** All files under `docs/specs/**` and the cross-spec contracts they encode. Out of scope: `src/` (does not exist yet).
> **Status:** v1.0 — APPROVED (nine adversarial review passes; no findings remain).

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
| T-10 | Channel-registry rows missing or unanchored | ERR-014-002, ERR-014-003 (#14); ERR-015-003, ERR-015-004 (#15); #11 OI-002 (gk.* channels) — all deferred to Stage 1 | Medium |
| T-11 | Float-vs-Fixed64 leakage in Stage 0 specs | #9 §8.1 Stage 5+ binding | Medium |
| T-12 | RNG non-determinism (`System.Random`, `DateTime.Now`) | CLAUDE.md "When Writing Code" | High |
| T-13 | Magic numbers in formula prose (constants not in catalogue) | Pass Mechanics F-A02 (WINDUP_FRAMES localization) | Medium |
| T-14 | Tick-rate conflation (10 Hz tactical vs 60 Hz physics) | KD-8 loop-separation tagging | Medium |
| T-15 | Citation/DOI fabrication | OI-003 (Bull 1985, Auger & Pellegrini 2007 — unfindable) | High |
| T-16 | Approval-status drift between `SPEC_INDEX.md` and spec §9 | Repeated reconciliation entries May 12–18 | Medium |
| T-17 | File-manifest drift after rename/move | `file-manifest.md` reconciliation history | Low |
| T-18 | Open-Issues entries with no "since" date or stale resolution | This file's own OPEN ISSUES discipline | Low |
| T-19 | Normative cross-spec constraint in downstream spec retroactively binding upstream spec without XC- entry | ERR-016-002 (#16 §3.2.5 silently bound #2 + #8; required patch commits to two already-APPROVED specs) | High |

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

Each Tier A probe is a script in `tools/spec-stress/` (to be authored after this strategy is approved). Probes are pure (no network, no LLM) so they can run in CI. Exception: A-13 (doi-resolver) requires outbound HTTP and must run in a CI environment with egress permitted; it is excluded from network-restricted CI runs and demoted to WARN (not FAIL) when the network is unavailable.

| Probe ID | Threat covered | Mechanism | Pass criterion |
|----------|----------------|-----------|----------------|
| A-01 spec-number-resolver | T-01, T-07 | Scan `docs/specs/**` only; also exclude `**/audit-report.md` and `**/changelog*.md` within spec folders (these intentionally preserve former spec numbers as historical documentation — per CLAUDE.md "Audit reports and changelog rows intentionally left unchanged"); exclude `docs/tracking/**` and `docs/planning/**`. Parse (a) every `#\d+ §[\d.]+` token and (b) every bare `#\d+` token. For both forms, resolve N against `SPEC_INDEX.md` canonical numbers; for (a) also resolve `§Y.Z` against headings in `docs/specs/<folder>/section-*.md`; flag any N that appears in SPEC_INDEX "FORMER NUMBERING" table as stale; flag any N that appears in neither the canonical numbers nor the FORMER NUMBERING table as unresolvable | Every token resolves to a known spec; zero stale-number hits (N in FORMER NUMBERING); zero unresolvable-number hits (N in neither list); zero section-not-found hits for `§Y.Z` citations |
| A-02 constant-tag-lint | T-04, T-13 | Grep every decimal numeric literal appearing in a formula, table cell, or inline calculation context (exclude: section/figure/table reference numbers, year literals, list-item indices, player/spec counts in prose, and cross-reference IDs matching `[A-Z]{2,3}-\d+` such as XC-001, FM-003, EC-012, ERR-010, FR-05, KD-8); require adjacency to one of `[GT]`/`[EST]`/`[FIXED]`/`[DERIVED]`/`[CROSS]`/`[CROSS-PENDING]` or a `<<CATALOGUE:…>>` marker | Zero untagged formula/calculation literals outside any `## Appendix` section (appendix labels vary by spec — A, B, C, D, F, etc.; exclude all) |
| A-03 cross-pending-tracker | T-05 | Find every `[CROSS-PENDING]`; require a matching `ERR-NNN-NNN` row in `spec-error-log.md` with `Status: OPEN` (literal string; `spec-error-log.md` must use the two-value convention `Status: OPEN` / `Status: RESOLVED` — any other value is a log-format error flagged by the probe) | Every CROSS-PENDING has a tracked upstream with `Status: OPEN`; zero orphans |
| A-04 domain-tag-allocator-audit | T-08 | Parse `#16 §3.4` table; assert every `DOMAIN_TAG_*` value is unique and ≤ 0xFF; assert any gap in the allocation sequence has an explicit `_RESERVED_0xNN_` placeholder row in the §3.4 table | Zero collisions; zero silent gaps (every skipped value must have a placeholder row); zero unallocated tags referenced elsewhere |
| A-05 fatigue-convention-guard | T-03 | Grep for inverted-assignment patterns only: `\b1(\.0)?\b\s*[=:]\s*rest` (flags "1 = rested", "1.0 = rested") and `\b0(\.0)?\b\s*[=:]\s*fatigue` (flags "0 = fatigued", "0.0 = fatigued"); also flag `rested\s*[=:]\s*\b1\b` and `fatigued\s*[=:]\s*\b0\b` | Zero hits matching inverted-assignment form; correct prose ("fatigue value 1.0 means fully fatigued") does not trigger; patterns match only "1"/"1.0" and "0"/"0.0" (not "10" or "00") |
| A-06 coordinate-convention-guard | T-03 | Grep for "center origin" / "centre origin" (direct origin claims; FAIL); grep for "pitch center" / "centre of pitch" / "(52.5, 34, 0)" (contextual mentions; WARN for human review — correct contextual prose such as "pitch center is at (52.5, 34, 0)" does not constitute an origin claim) | Zero FAIL hits; WARN hits reviewed manually to confirm no origin claim |
| A-07 approval-checklist-verifier | T-02 | Parse every `section-9-approval-checklist.md`; for each entry matching the canonical format `✓ verified at §Y.Z` (whitespace-normalised; implementation must also handle variants "verified against §Y.Z", "confirmed §Y.Z"), confirm the cited heading exists in the cited file | Zero unverifiable entries |
| A-08 status-reconciliation | T-16 | Compare each spec's `section-9` Approval row vs `SPEC_INDEX.md` row | Zero disagreements |
| A-09 file-manifest-sync | T-17 | Diff `file-manifest.md` against `find docs/specs -type f` | Zero missing or extra rows |
| A-10 rng-leak-guard | T-12 | Grep all specs for `System.Random`, `DateTime.Now`, `Random.value`, `UnityEngine.Random` | Zero hits outside §7 (Future Extensions / Stage 1+ deferrals) and Appendix sections |
| A-11 float-leak-guard | T-11 | Grep all approved Stage 0 specs for `Fixed64` outside §1 (any subsection — dependency declarations vary by spec), §7 deferrals, and #9 itself | Zero unexpected hits |
| A-12 tick-rate-literal-guard | T-14 | Grep all spec body text for loop-rate literals (pattern `[~≈]?\s*\d+(\.\d+)?\s?Hz` and `[~≈]?\s*\d+(\.\d+)?\s?ms`). Assert each hit, after stripping `~`/`≈` prefix and collapsing whitespace, is in the explicit whitelist: `{10 Hz, 10Hz, 60 Hz, 60Hz, 100 ms, 100ms, 100.0 ms, 100.0ms, 16.67 ms, 16.67ms, 16.7 ms, 16.7ms, 17 ms, 17ms}` (16.7/17 are legitimate rounded forms of 16.67 ms; 100.0 = 100 ms explicitly; 16 is not in the set — no loop runs at ~62.5 Hz). | Zero loop-rate literals outside the authoritative whitelist |
| A-13 doi-resolver | T-15 | For every `doi.org/...` link in §8 References, GET-request with redirect-following enabled; assert final HTTP status is non-4xx (doi.org returns 301 permanent redirects for valid DOIs — asserting only 200/302 would false-fail all valid links) | Zero 4xx or 5xx final responses |
| A-14 phantom-interface-guard | T-06 | Every `interface I…` in any spec section must have **both** producer and consumer specs cited in §1 dependencies (the template places interfaces in §4, but implementations may appear in §2 data structures or §3 pseudocode — scan all sections) | Zero one-sided interfaces |
| A-15 open-issues-staleness | T-18 | Every CLAUDE.md OPEN ISSUES entry must carry a "since" date; flag entries older than 30 days without status change | Zero undated; warn ≥30 days |
| A-16 normative-constraint-audit | T-19 | Grep every spec (all sections §1–§9) for sentences containing both a normative keyword (MUST, SHALL, MUST NOT, SHALL NOT) and a spec reference (`#\d+`). Emit WARN for each hit; determining which spec holds the obligation (subject-form vs. object-form) requires sentence-grammar analysis beyond automated grep — each WARN is reviewed by a human who then verifies the correct XC- or `[CROSS:]` entry exists in the obligated spec's §1 or §2. (XC- entries live in the constrained spec, not the spec making the claim.) Triage state persisted in `tools/spec-stress/reports/a16-triage.json`; a WARN is closed only when a human entry exists in that file. A-16 reaches PASS when zero open WARNs remain. | Zero hits left unreviewed (tracked in a16-triage.json); every WARN closed as either "XC- confirmed" or "ERR filed" |

### 4.2 Tier B — manual / adversarial probes

These probes require judgment. Each is a written exercise producing a dated report under `docs/tracking/stress-reports/YYYY-MM-DD-<probe>.md`.

| Probe ID | Axis | Exercise |
|----------|------|----------|
| B-01 renumber-simulation | Maintainability | Pick a random spec N (excluding #16, #8 — too central). Simulate renaming to N+10. Run A-01 with the rename overlay; count files touched and whether tag/error-log entries surface correctly. **Target:** ≤ K files per spec where K = number of distinct files containing `#N` (bare or with section suffix) as computed by A-01 for that spec; zero files affected beyond that count. |
| B-02 spec-21-injection | Extendibility | Author a 1-page outline for a hypothetical Spec #21 (e.g., "Substitutions"). Walk it through: SPEC_INDEX row, DOMAIN_TAG allocation against #16 §3.4, channel-registry rows against #18 Appendix F.0, KD-2 sequencing position. **Target:** no template change required. |
| B-03 carve-out-activation | Extendibility | For each Stage 0+1 carve-out (catalogue them first), trace the activation path end-to-end on paper. KD-18 jump-kinematics (#10→#2) is the canonical example. **Target:** every cited migration row exists on both sides. |
| B-04 constant-blast-radius | Maintainability | Pick five `[GT]` constants at random. For each: change value by ±50%; identify every test, formula, and derived constant that must re-validate. **Target:** blast radius is fully enumerable from `[DERIVED]` back-links — no surprise dependencies. |
| B-05 adversarial-pair-audit | Structural soundness | Pick two specs with a cross-spec dependency (e.g., #5 ↔ #1, #14 ↔ #8). Read both as if you'd never seen them; flag every disagreement in vocabulary, units, or directionality. **Target:** zero new findings on repeat cycles; first-cycle findings are filed as ERR entries and remediated before G-Stage1. |
| B-06 fixed64-swap-rehearsal | Extendibility | Walk the corpus as if Stage 5 begins tomorrow. Identify every `float` that must promote to `Fixed64`, every comparator that needs an epsilon, every RNG call site. **Target:** the work list is producible from #9 §8.4 alone; no `grep -r float` archaeology. |
| B-07 multiplayer-rehearsal | Extendibility | Walk the corpus as if Stage 5 multiplayer begins tomorrow. Identify every state-snapshot replacement, every "single-machine determinism" assumption (per CLAUDE.md) that must be re-verified. **Target:** work list bounded by #16 §3 + #9 §6.6. |
| B-08 fabricated-citation-hunt | Structural soundness | For each spec §8 References list, pick three citations at random and verify against primary source (DOI, ISBN, conference proceedings). Repeats OI-003 discipline across all 20 specs. **Target:** zero fabrications in the sampled citations; if any fabrication is found, extend verification to the full §8 of that spec before the probe closes. |
| B-09 audit-reproducibility | Maintainability | Hand a fresh AI agent the spec folder and the Approval Checklist with verification claims **stripped**. Ask it to re-verify each line. Compare to original. **Target:** ≥ 95% agreement, defined as: fraction of checklist entries where both the original and the fresh agent reach the same PASS/FAIL conclusion; citation-precision differences (e.g., §3.1 vs §3.1.2) do not count as disagreements unless one verdict is PASS and the other FAIL. |
| B-10 KD-2-rollback-drill | Maintainability | Pick an APPROVED upstream (#16 or #8). Simulate its rollback to IN REVIEW (e.g., a critical audit finding). Enumerate every downstream that must follow. **Target:** cascade is computable from `[CROSS]` / `[CROSS-PENDING]` graph; no judgment calls needed. |
| B-11 future-spec-sequencing-audit | Structural soundness | When a new spec (#21+) is proposed for SPEC_INDEX, walk its declared `[CROSS]` dependencies and verify every upstream is already APPROVED before the new spec's own IN REVIEW flip (T-09 gate). Also walk the existing 20-spec `[CROSS]` graph and confirm no consumer in the current corpus reached APPROVED before its declared producer. **Target:** zero sequencing inversions found in current corpus; sequencing check precondition documented in SPEC_INDEX for all future additions. |
| B-12 channel-registry-deferred-audit | Structural soundness | Catalogue every channel-registry row deferred to Stage 1 by scanning `spec-error-log.md` for ERR entries whose finding description references "channel" or "channel-registry" (do not enumerate spec numbers manually — scan the log). For each deferred row: verify (a) a Stage 1 delivery anchor exists in the citing spec's §7 deferrals and (b) the ERR log row naming the deferral is present and open. **Target:** every deferred channel-registry row has a §7 anchor on both the producer and consumer sides; zero silent deferrals (deferred without an ERR row or §7 entry). |

---

## 5. Pass/fail criteria and reporting

### 5.1 Per-probe outcome

Each probe produces one of: **PASS**, **WARN** (advisory, non-blocking), **FAIL** (blocks the gate it feeds).

### 5.2 Aggregate gates

| Gate | Definition | Blocks |
|------|------------|--------|
| **G-Push** | All Tier A probes PASS or WARN | Merge to `main` |
| **G-Code** | All Tier A PASS; B-01, B-02, B-04 run within 30 days of this strategy reaching APPROVED status with FAIL=0 | First `src/` commit |
| **G-Stage1** | G-Code must have passed; all Tier A+B PASS within 60 days of this strategy reaching APPROVED status | Stage 0→Stage 1 promotion |

### 5.3 Reports

- Tier A: machine-readable JSON written to `tools/spec-stress/reports/latest.json`; summary appended to `docs/tracking/stress-reports/tier-a-log.md` (one row per run).
- Tier B: one Markdown report per probe execution; index in `docs/tracking/stress-reports/INDEX.md`.

---

## 6. Cadence

| Trigger | Probes run |
|---------|------------|
| Push to `docs/specs/**` | Tier A subset relevant to changed files (precomputed dependency map) |
| Weekly cron | Full Tier A |
| Monthly | One Tier B probe (rotate B-01..B-12) |
| Before any spec status flip (any direction) | A-01, A-03, A-05, A-07, A-08 |
| Before first `src/` commit (G-Code) | Full Tier A + B-01, B-02, B-04 |
| Before Stage 1 promotion (G-Stage1) | Full Tier A + all Tier B |
| After any new defect class is discovered in the wild | Add a new T-NN row in §2; add at least one probe in §4 |
| When a new row is added to `SPEC_INDEX.md` (spec #21+) | A-01, A-03, A-04, A-08 + B-11 (verify all `[CROSS]` dependencies of new spec are APPROVED before its IN REVIEW flip — T-09 gate) |

---

## 7. Implementation plan

This document is the **strategy**. The deliverables it implies are tracked separately:

1. **Tools** — `tools/spec-stress/` directory containing one script per Tier A probe. Language: Python (matches existing tooling style implied by CLAUDE.md "SplitMix64 in Python tooling" note).
2. **Probe-dependency map** — `tools/spec-stress/probe-deps.toml` mapping each probe to the file globs that, when changed, require its re-run.
3. **Report skeletons** — `docs/tracking/stress-reports/tier-a-log.md`, `…/INDEX.md`, `…/template-tier-b.md`.
4. **CI hook** — runs Tier A on push; opens an issue tagged `spec-stress-fail` on any FAIL.
5. **A-16 triage state store** — `tools/spec-stress/reports/a16-triage.json` (human-maintained; created on first A-16 run; records each WARN hit as "XC- confirmed" or "ERR filed"; A-16 achieves PASS only when zero open entries remain).

None of (1)–(5) are written yet. They are authored only after this strategy itself is reviewed and approved.

---

## 8. Non-goals

- **Code-level unit and integration testing.** That is #19's job once `src/` exists. This strategy stops at the spec-corpus boundary. (A-10 and A-11 grep for C# symbols in spec pseudocode — that is corpus analysis, not code testing.)
- **Replacing adversarial review passes.** PASS-1 / PASS-2 critiques on individual specs continue as before. This strategy operates on the assembled corpus, not on a single spec under draft.
- **Auto-fixing failures.** Every FAIL is escalated to a human (or a fix-pass AI agent invoked deliberately). No probe rewrites spec content.

---

## 9. Open questions

| ID | Question | Owner | Since |
|----|----------|-------|-------|
| Q-01 | Is Python the right language for Tier A, or should we use a Unity-friendly C# tool so it shares CI with `src/` once code exists? | Lead dev | May 18, 2026 |
| Q-02 | Should B-09 (audit reproducibility) be run by a different model family than the one that wrote the spec, to avoid self-confirmation bias? | Lead dev | May 18, 2026 |
| Q-03 | What is the right cadence for B-08 / DOI re-verification — once is enough, or quarterly? | Lead dev | May 18, 2026 |
| Q-04 | Should the probe-dependency map be hand-authored or derived from a parse of `[CROSS]` tags? | Lead dev | May 18, 2026 |

---

## 10. Version history

| Version | Date | Change |
|---------|------|--------|
| v0.1 | May 18, 2026 | Initial draft. 18 threat-model rows; 15 Tier A probes; 10 Tier B probes; 3 aggregate gates. |
| v0.2 | May 18, 2026 | Pass-1 adversarial review fixes: Q-03 stale "B-13" → "B-08"; A-12 rewritten as vocabulary-based tick-rate literal check (prior tag convention never adopted in specs); A-13 HTTP criterion broadened to non-4xx (doi.org uses 301, not 302); A-02 appendix exclusion broadened from hardcoded "Appendix-A" to any `## Appendix` section; A-05 tightened to inverted-assignment patterns only; T-19 + A-16 added (normative-constraint-audit, covers ERR-016-002 defect class); G-Code 30-day window anchored to strategy APPROVED date; §6 cadence row added for future SPEC_INDEX additions (T-09 gate); B-05 target clarified (first-cycle findings go to ERR log; zero new findings on repeat cycles); B-11 added (future-spec-sequencing-audit, T-09); §8 non-goal clarified to "unit and integration testing" with A-10/A-11 note. 19 threat-model rows; 16 Tier A probes; 11 Tier B probes. |
| v0.3 | May 18, 2026 | Pass-2 adversarial review fixes: §4.1 intro adds A-13 network-egress exception (resolves contradiction with "no network" claim); A-12 pass criterion normalized to integer Hz/ms rounding to handle `~`/`≈` prefixes and no-space variants; A-05 regex corrected `\b1\.?0?\b` → `\b1(\.0)?\b` and `\b0\.?0?\b` → `\b0(\.0)?\b` (prior form also matched "10" and "00"); A-16 scope broadened from §2/§3 to all sections §1–§9; A-02 "numeric literal" scoped to formula/calculation contexts with explicit exclusions (section refs, years, indices, counts); A-04 gap-documentation convention specified as `_RESERVED_0xNN_` placeholder row; B-12 added (channel-registry-deferred-audit, T-10); G-Stage1 anchor date added; §6 monthly rotation updated to B-01..B-12. 19 threat-model rows; 16 Tier A probes; 12 Tier B probes. |
| v0.4 | May 18, 2026 | Pass-3 adversarial review fixes: A-12 integer-rounding approach replaced with explicit string whitelist (16.67 rounds to 17, not 16; whitelist covers 16.7/17 as legitimate rounded forms; 16 removed); A-16 MUST/SHALL match pattern broadened to any normative sentence containing `#\d+` (prior subject-only pattern missed object-clause references like ERR-016-002); A-01 extended to bare `#\d+` tokens (prior pattern required section suffix; the #7→#8 cascade was predominantly bare-number hits); A-10 exclusion scope defined as §7 + Appendix sections; A-06 split into FAIL (direct origin-claim phrases) and WARN (contextual mentions requiring human review); B-08 escalation clause added (fabrication found → full §8 scan for that spec); B-01 target K defined as A-01 distinct-file count for spec N. |
| v0.5 | May 18, 2026 | Pass-4 adversarial review fixes: A-16 directionality corrected — XC- entries live in the obligated/constrained spec, not the spec making the claim (subject-form: check #N; object-form: check current spec); A-07 format variants enumerated (implementation must handle "verified against", "confirmed" variants); A-12 whitelist extended with 100.0 ms/100.0ms forms; G-Stage1 now explicitly requires G-Code as prerequisite; B-12 ERR enumeration changed from hard-coded spec numbers to scan of spec-error-log.md by "channel"/"channel-registry" keyword (avoids incorrect ERR-013 assumption). |
| v0.6 | May 18, 2026 | Pass-5 adversarial review fixes: A-16 demoted from automated FAIL to WARN-only (subject/object-form directionality requires sentence-grammar analysis beyond grep; human closes each WARN); A-01 scan scope explicitly restricted to `docs/specs/**` (tracking docs contain historical `#N` references that would false-fire); A-03 `Status: OPEN` specified as literal string with two-value convention; A-11 `§1.3` exclusion generalised to `§1 (any subsection)` (dependency subsection number varies by spec); B-09 "95% agreement" metric defined (PASS/FAIL conclusion parity; citation-precision differences don't count). |
| v0.7 | May 18, 2026 | Pass-6 adversarial review fixes: A-01 pass criterion extended to cover unresolvable spec numbers (N not in SPEC_INDEX at all) in addition to stale former-numbered references; A-16 triage state persistence documented (a16-triage.json, WARN-closed only with human entry, PASS only when zero open WARNs remain); A-14 broadened from §4-only to all spec sections (interface definitions may appear in §2 or §3). |
| v0.8 | May 18, 2026 | Pass-7 adversarial review fixes: A-01 exclusion list extended to `**/audit-report.md` and `**/changelog*.md` (these intentionally preserve former spec numbers; scanning them would false-fire); §7 item 5 added (a16-triage.json as deliverable). |
| v0.9 | May 18, 2026 | Pass-8 adversarial review fixes: A-02 exclusion list extended to cross-reference IDs matching `[A-Z]{2,3}-\d+` (XC-, FM-, EC-, ERR-, FR-, KD- patterns are IDs, not constants); T-10 evidence corrected from "ERR-013/014/015" to specific documented ERR IDs from CLAUDE.md (ERR-014-002/003, ERR-015-003/004, #11 OI-002). |
| v1.0 | May 18, 2026 | Pass-9 adversarial review: no new findings. Cosmetic: §7 blank line between items 4–5 removed. Status promoted to APPROVED. |
