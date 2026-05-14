# Spec #18 Performance Optimization — PASS-2 Adversarial Review

**Created:** May 14, 2026
**Reviewer:** Claude Code (adversarial pass over v0.2 section files)
**Scope:** `section-1.md` … `section-9-approval-checklist.md` + `appendices.md`,
all at v0.2 (PASS-1 fix pass landed May 14, 2026 via PR #59 + PR #60 merges
on top of v0.1 section files from May 13, 2026).
**Purpose:** Second adversarial pass. PASS-1 raised 4 H / 6 M / 13 L
findings (`ERR-018-002` … `ERR-018-011`), all resolved in v0.2. This PASS-2
sweep focuses on (a) PASS-1 fix-pass collisions caused by parallel branch
merges, (b) residual semantic ambiguities, and (c) new findings against
v0.2 surface.

**Total findings:** 2 H / 5 M / 8 L = 15.

---

## Root-cause context (process finding, not a numbered defect)

`git log --graph` shows two parallel branches both implemented v0.2:
PR #59 (`claude/fix-performance-specs-J1t5Z`, commit `14c6ba6`) and PR #60
(`claude/review-performance-specs-YHGga`, commit `dd6a87c`). Both landed in
`main` and were merged without de-duplication. Every PASS-2 H- and most M-
findings below trace to that merge: each branch independently authored a
fix for the same PASS-1 finding, both fixes were retained, and the
artefacts now coexist in the merged tree.

**Process recommendation:** before any future v0.3 fix-pass, run a duplicate-
header / duplicate-row diff against the v0.2 baseline (`grep -n "^### F\.0"
appendices.md`, `grep -c "^| 0.2" section-*.md`, etc.) and resolve before
authoring fresh content.

---

## HIGH

### H-1 — Appendix F has two `### F.0 Channel Registry Schema` sections with conflicting field sets

**Location.** `appendices.md` lines 231–256 and 258–281.

**Symptom.** The same heading appears twice, with two materially different
schemas:

- **First F.0** (lines 231–256): 13 fields, including `owning_subsystem`,
  `default_verbosity`, `sampling_rule`, `sample_n` (conditional),
  `sink_routing`, `determinism_class`, `inside_tick_pipeline`,
  `sign_off_log_ref`, `record_format_version`, `owner_contact`,
  `created_date`, `version_history`. No example rows.
- **Second F.0** (lines 258–281): 7 fields, with renamed and different-typed
  fields — `subsystem_owner` (vs `owning_subsystem`), `verbosity_tier_min`
  (vs `default_verbosity`), `sink_targets` (vs `sink_routing`),
  `emission_veto_required` bool (vs `inside_tick_pipeline` + `sign_off_log_ref`
  pair), `record_format` reference (vs `record_format_version` semver),
  `declared_stage` (vs `created_date` + `version_history`). Includes three
  example rows (`perf.budget`, `perf.alloc`, `perf.trace`).

**Why it matters.** The `[HotPathAllocExempt]` sign-off pattern audited by
§5.7.1 names `sign_off_log_ref` as the field walked. That field exists only
in the first F.0; the second F.0 collapses it into a single
`emission_veto_required` boolean. §5.7.1's audit hook is therefore
satisfiable only against the first F.0. Conversely, the F.1 … F.5
dashboards consume the `perf.budget` / `perf.alloc` / `perf.trace` channels
listed in the second F.0; the first F.0 publishes no rows. The schema is
ambiguous: either dashboards have no data or the audit hook has no field
to walk.

**Root cause.** PR #59 added one F.0; PR #60 added another; merge retained
both. Cf. `appendices.md` v0.2 history note: *"H-4 Appendix F.0 channel
registry schema added (ERR-018-005)"* — landed twice.

**Suggested fix.** Pick one canonical F.0. Recommendation: keep the first
F.0 schema (richer, supports §5.7.1 audit hook against `sign_off_log_ref`,
adds `record_format_version` semver per KD-11), but graft on the second
F.0's example rows (`perf.budget` / `perf.alloc` / `perf.trace`) so F.1 …
F.5 retain their upstream-channel citations. Rename
`emission_veto_required` references in F-narrative to match
`inside_tick_pipeline = true` per the canonical schema.

**Severity rationale.** HIGH — the schema is the §3.8.2 trace-pipeline
deliverable. Two contradictory authoritative tables in the same appendix
section is exactly the "fabricated checklist values" / "superseded file
references" class of bug CLAUDE.md flags.

### H-2 — `section-3.md` §3.10 Constants Catalogue has three duplicate-constant rows

**Location.** `section-3.md` lines 565 ↔ 572, 566 ↔ 573, 567 ↔ 574.

**Symptom.** Three governance constants are declared twice each:

| First row (v0.1) | Duplicate row (v0.2) | Constant |
|------------------|----------------------|----------|
| Line 565: `[EST]-baseline acceptance tolerance = ±20%` `[GT]` → §3.9.1 | Line 572: `[EST]→[GT]` promotion tolerance = ±20% `[GT]` → §3.9.1 | The same ±20% promotion tolerance |
| Line 566: Dashboard sample window = 100 captures `[GT]` → Appendix F.1 | Line 573: Per-spec p50/p99 rolling window N = 100 captures `[GT]` → Appendix F.1 | The same N=100 window |
| Line 567: Flake-rate alert threshold = 1% `[GT]` → Appendix F.5 | Line 574: Flake-rate boundary-defect routing threshold = 1% `[GT]` → Appendix F.5 | The same 1% threshold |

**Why it matters.** §3.10 is meant to be the single authoritative catalogue
("Constant Provenance Summary" mirror at §8.4). Two rows per constant
violate that singularity and create a forking-citation hazard — a future
revision that tweaks 1% to 0.5% in only one row leaves §3.10 internally
inconsistent.

**Root cause.** Both PR #59 and PR #60 resolved ERR-018-008 and ERR-018-010
by appending rows; merge retained both sets. The v0.1 row exists because
v0.1's §3.10 already pre-emptively published a less-fully-tagged version
of the same value; the v0.2 fix pass re-added with the tagged version
instead of editing-in-place.

**Suggested fix.** Delete lines 565, 566, 567 (the v0.1 rows); the v0.2
rows at 572, 573, 574 carry richer rationale. Confirm §8.4 mirror table
matches the surviving rows.

**Severity rationale.** HIGH — propagates straight into the §8.4
evidence-artifact table and into §9.2 "Governance-number evidence-artifact
citations verified" checkbox. Auditor walking the §3.10 table will report
two §3.9.1 / Appendix F.1 / Appendix F.5 rows and a §9 reviewer cannot
mechanically resolve which is canonical.

---

## MEDIUM

### M-1 — Seven version-history tables carry duplicate v0.2 rows

**Location.** `section-2.md` (lines 232, 234), `section-3.md` (593, 595),
`section-5.md` (173, 175), `section-7.md` (93, 95), `section-8.md`
(189, 191), `section-9-approval-checklist.md` (146, 148), `appendices.md`
(370, 372). Section-1 / section-4 / section-6 carry exactly one v0.2 row
(or none, in section-4 / section-6's case).

**Symptom.** In every affected file the order is `v0.2 (summary) | v0.1 |
v0.2 (detailed fix list)` — two v0.2 rows sandwiching the v0.1 row. The
two v0.2 rows have different wording but report the same May 14, 2026 fix
pass.

**Why it matters.** Per the project convention (CLAUDE.md "Append a
version history entry to every modified file"), each version is a single
row. Two rows-per-version corrupts the table's role as the audit trail —
a reviewer cannot tell which row is canonical, and a downstream `git
blame` no longer ties cleanly to a version.

**Root cause.** Same parallel-branch merge as H-1 / H-2.

**Suggested fix.** Delete the second of the two v0.2 rows in each file,
keeping the version-history row whose Notes column carries the richer
fix-list (the detailed `ERR-018-NNN` enumeration). Or merge the two row
texts into one.

### M-2 — `section-1.md` header `Last Updated` field is stale

**Location.** `section-1.md` line 4: `**Last Updated:** May 13, 2026`.

**Symptom.** A v0.2 row dated May 14, 2026 exists at §1.5 (line 302), but
the header still reports May 13, 2026.

**Why it matters.** CLAUDE.md "Include creation date and purpose header on
every new file" implies the header's `Last Updated` field should be
updated atomically with §1.5. (`section-2.md` through `section-9-*.md` and
`appendices.md` all carry `May 14, 2026` in their headers — section-1 is
the only outlier.)

**Suggested fix.** Update `**Last Updated:** May 13, 2026` →
`**Last Updated:** May 14, 2026 (v0.2 PASS-1 adversarial-review fix pass)`
to match every other section file.

### M-3 — `section-3.md` §3.5.2 conflates the +5% per-PR gate with the ±20% `[EST]`→`[GT]` promotion tolerance

**Location.** `section-3.md` §3.5.2 lines 273–275 (Shot Mechanics example):
*"For example, Shot Mechanics #6 §4.5 already declares a 0.05 ms total
budget; deviations larger than 5% from the 0.017 ms estimated cite #6 §4.5
authority, not §3.5.2 default."*

**Symptom.** The +5% per-PR regression threshold (§3.5.2 / FR-PO-031) is
defined against the **pre-PR captured baseline** for the same scenario /
seed / platform. The "0.017 ms estimated" is a spec-time `[EST]` anchor,
not a captured baseline. §3.9.1 explicitly says the first Stage 0+1
baseline capture promotes the `[EST]` to a measured `[GT]` only if within
±20% (`[GT]`) — the +5% gate does not apply until *after* promotion. The
example invokes the +5% gate against the un-promoted estimate.

**Why it matters.** Either (a) the +5% gate fires on every first-capture
of #6's shot path that lands anywhere between 0.018 ms and 0.020 ms,
contradicting §3.9.1's "within ±20% promotes silently"; or (b) the example
is wrong. Adversarial readers will pick (a) and the spec becomes
internally inconsistent.

**Suggested fix.** Either drop the Shot Mechanics example from §3.5.2
(it's already cited at §3.9.1 as the canonical `[EST]`-anchor case), or
rewrite to *"#6 §4.5 declares a 0.05 ms total budget against which the
first measured baseline is checked under §3.9.1's ±20% promotion
tolerance; subsequent per-PR captures use the +5% gate"*.

### M-4 — FR-PO-019 levels `MAY` but its statement embeds a `MUST`

**Location.** `section-2.md` §2.2.3 FR-PO-019:
*"Cross-scenario profiling (Spec #19 KD-8 cross-spec scenarios) is
permitted; the manifest ID and seed MUST be recorded the same way."*
Level column: `MAY`.

**Symptom.** The leading clause is permissive (MAY-grade); the trailing
clause states an unconditional MUST. RFC 2119 grammar treats the row's
declared level as the binding force of the whole statement — a MAY-row
that embeds a MUST is structurally identical to the MUST/MAY conflict
PASS-1 caught as ERR-018-003 (FR-PO-067 vs §3.4.4).

**Why it matters.** Conformance auditor reading the level column will not
enforce the recording requirement.

**Suggested fix.** Split into two FRs:
- FR-PO-019 (MAY): *"Cross-scenario profiling (Spec #19 KD-8 cross-spec
  scenarios) is permitted."*
- FR-PO-019a (MUST): *"For any cross-scenario profiling session, the
  manifest ID and seed MUST be recorded per FR-PO-016."*

Or upgrade FR-PO-019 to MUST with: *"Cross-scenario profiling sessions
MUST record manifest ID and seed per FR-PO-016 when used; use itself is
optional."*

### M-5 — §3.7.5 pre-specifies a C# attribute signature without a specified consumer

**Location.** `section-3.md` §3.7.5 lines 393–398: *"the C# `Attribute`
definition lands at first `src/` commit (targets: `Method | Constructor`;
required constructor argument: `string rationale`; companion lead-
developer-sign-off comment cites the `spec-error-log.md` row that
authorizes the exemption)."*

**Symptom.** The attribute's C# signature is fully specified at spec-time
— `Method | Constructor` targets, `string rationale` constructor argument,
companion-comment shape — but the consumer (the CI allocation-tracker
build step that reads the attribute) is not specified anywhere in #18,
#19, or #20. Per `src/CLAUDE.md` §4.6 pointer, the allocation-tracker
pin is §7.5 D2 / Stage 0+1.

**Why it matters.** CLAUDE.md "Interface Design Principle" (ERR-001 /
ERR-004 hazard): *"Write interfaces only when both sides are specified."*
This is the same trap that caught phantom interfaces in earlier
remediations. Locking in the attribute signature before the consumer is
spec'd risks forcing a redesign once the tracker pin lands.

**Suggested fix.** Defer the concrete C# signature to Stage 0+1 (D2
adjacent). §3.7.5 should declare only the *governance identifier* (the
name `[HotPathAllocExempt]`), the policy (exempt one-shot allocations,
require sign-off, cite rationale), and explicitly mark the C# binding as
"first `src/` commit deliverable". Move the `Method | Constructor` /
`string rationale` detail to a Stage 0+1 deliverable row in §7.1.

Alternative: if the attribute signature has a settled consumer story not
visible to PASS-2, cite that consumer explicitly so the "both sides
specified" rule is satisfied on the spec face.

---

## LOW

### L-1 — §3.3.4 "60 Hz physics loop produces ~17 samples per frame"

**Location.** `section-3.md` §3.3.4 line 169: *"the 60 Hz physics loop
produces ~17 samples per frame (~16.67 ms per frame)."*

**Symptom.** 1000 Hz / 60 Hz = 16.67 samples/frame, not 17.

**Suggested fix.** Use `≈16.67 samples per frame` or `~16 samples per
frame` (truncated) for consistency with the surrounding "100 samples per
tick" (which is exact at 1000 / 10).

### L-2 — §3.7.6 trailing "— cite" draft placeholder

**Location.** `section-3.md` §3.7.6 line 416: *"LINQ on hot paths (banned
per Spec #20 §3 — cite)."*

**Symptom.** "— cite" is a draft-time TODO marker that should resolve to
the actual §3.x subsection of Spec #20 that bans LINQ on hot paths (Spec
#20 is `APPROVED`).

**Suggested fix.** Replace with the specific subsection citation (e.g.,
`Spec #20 §3.x.y banned-allocation patterns`) or drop the parenthetical if
the bare reference is sufficient.

### L-3 — `[x]` checkbox in §9.3 lands ahead of §9.4 sign-off

**Location.** `section-9-approval-checklist.md` lines 88 and 101.

**Symptom.** Two checkboxes pre-emptively marked `[x]`:
*"`SPEC_INDEX.md` status updated atomically with the IN PROGRESS → IN
REVIEW flip"* and *"`docs/tracking/file-manifest.md` updated to reflect
new section-file content."* The other checkboxes (including the
lead-developer sign-off itself) remain `[ ]`.

**Why it matters.** Approval-checklist convention in approved specs (#5
re-review packet, #16 §9, #19 §9, #20 §9) keeps every box `[ ]` until the
§9.4 sign-off block records completion. Pre-flipped boxes can be read as
"approval partially granted", which is exactly the procedural-ambiguity
class that motivated ERR-018-011 (premature §9.4 status flip vs
`SPEC_INDEX.md`).

**Suggested fix.** Either (a) revert the two `[x]` to `[ ]` and let §9.4
sign-off flip them atomically; or (b) introduce an explicit convention
note that "[x]" rows are reviewer-pre-cleared atomic-tracking artifacts,
not partial approval.

### L-4 — KD-3 codification-map cell over-broad

**Location.** `section-1.md` §1.3 codification map: `KD-3 | Boundary with
#16 (inverted) | §3.3, §3.8, §5.7`.

**Symptom.** The emission-veto authority specifically lives at §3.8.3
(FR-PO-058a); the codification map cites only the parent §3.8. A reviewer
looking up KD-3's emission-veto rule has to scan all of §3.8.

**Suggested fix.** Cite the subsection: `§3.3, §3.8.3, §3.8.4, §5.7.1`.

### L-5 — Appendix B §6.5 demands data not required by §3.1.2 schema

**Location.** `appendices.md` Appendix B template §6.5: *"For each
`[CROSS]` or `[CROSS-PENDING]` budget consumed from another spec, cite the
source spec, section, and the value being consumed."*

**Symptom.** §3.1.2 ("Per-spec §6 schema") lists exactly five required
fields: total per-tick budget, per-tick budget by loop tag, allocation
budget, worst-case input parameters, headroom multiplier. "Cross-spec
budget consumption" is not in that list, but Appendix B (the paste-ready
template) adds it as §6.5.

**Why it matters.** FR-PO-002 binds per-spec §6 sections to the Appendix B
schema. If Appendix B adds a field §3.1.2 does not mandate, schema-
conformance audit either over-rejects (flags specs that omit §6.5) or
under-rejects (lets specs ignore §6.5 because §3.1.2 doesn't require it).
Either way, the audit grammar is ambiguous.

**Suggested fix.** Either add cross-spec-budget-consumption to the §3.1.2
schema list (preferred — it's a real concept once `[CROSS]` budgets exist
between specs), or drop §6.5 from the Appendix B template.

### L-6 — §1.5 v0.1 row's "author-driven IN REVIEW flip" framing contradicts ERR-018-011 resolution

**Location.** `section-1.md` §1.5 line 303: *"SPEC_INDEX flip to IN REVIEW
is **author-driven**, not review-driven: it reflects 'draft complete,
awaiting lead-developer sign-off' per CLAUDE.md status definition."*

**Symptom.** PASS-1 explicitly filed `ERR-018-011` against this exact
framing (premature §9.4 IN REVIEW vs SPEC_INDEX.md showing IN PROGRESS).
v0.2 resolved by atomically updating SPEC_INDEX.md, but the v0.1 prose in
§1.5 still describes the flip as "author-driven" — which was the very
behaviour PASS-1 flagged as a CLAUDE.md "canonical source of truth"
contradiction.

**Why it matters.** Version-history rows are append-only audit trail.
Leaving the v0.1 framing visible is fine for history, but the v0.2 row
should explicitly note the procedural-correction story (currently it
notes only the `§1.1 / §1.2 / §1.3` text fixes for L-9 / L-10).

**Suggested fix.** Append to the §1.5 v0.2 row notes: *"Status caveat: v0.1
described the IN REVIEW flip as 'author-driven'; PASS-1 ERR-018-011
clarified that SPEC_INDEX.md must be updated atomically with §9.4 — that
atomic update landed in v0.2."*

### L-7 — `[GT]` provisional note inconsistently applied

**Location.** `section-3.md` §3.5.2 (line 267) carries *"(`[GT]`, §7.5 D9;
provisional — set conservatively ahead of first CI data; re-evaluated at
Stage 0+1 against measured baseline variance)"*. The §3.5.6
absolute-threshold guard (line 311) carries no parallel "provisional"
qualifier even though it is also `[GT]` and will be re-evaluated against
the same first-month CI data.

**Suggested fix.** Either add a parallel "(provisional)" note to §3.5.6,
or factor the "provisional" qualifier up into §3.10 as a column rather
than inline prose.

### L-8 — §3.5.3 table cell "Allocation | Spec #18 §3.7 (this section)" mislabels self-citation

**Location.** `section-3.md` §3.5.3 gate-composition table line 284.

**Symptom.** The other rows cite their section file explicitly (`Spec
#19 §6.2`, `Spec #16 §5 + §3.2.4.1`); the Allocation row says "Spec #18
§3.7 (this section)" — but §3.7 is a different section from §3.5.3 (the
table's containing section). The "(this section)" qualifier reads
loosely.

**Suggested fix.** Replace "(this section)" → "(this spec)" — minor
clarity nit.

---

## Findings NOT raised (deliberately considered and dropped)

- **Per-spec §6 grandfather rule risk.** §3.1.2 cites §6 sections of
  #1–#8 / #17 as having approved budgets, but those approvals predate
  the FR-PO-002 schema, so the rolled-up §3.1.3 table's first iteration
  will show schema-noncompliance everywhere. This is acknowledged by the
  §5.3 grandfather clause / FR-PO-007; deferring remediation to next
  natural per-spec revision is the right call.
- **`certification-platform.md` Stage 0 row not pinned.** Acknowledged at
  §1.4 / §9.4.1 as a Stage 0+1 activation precondition, not a #18 approval
  blocker — correct posture per KD-9 status caveat.
- **#16 `IN PROGRESS` / #19 `IN REVIEW` blocker chain.** Correctly listed
  at §9.4.1; bidirectional sequencing makes this self-resolving.
- **FR-PO-058a being a suffix-style FR-PO ID.** Numbering convention is
  irregular but documented in outline v1.1 history (May 13, 2026); not a
  defect.

---

## Severity summary

| Severity | Count | IDs |
|----------|-------|-----|
| HIGH     | 2     | H-1, H-2 |
| MEDIUM   | 5     | M-1, M-2, M-3, M-4, M-5 |
| LOW      | 8     | L-1, L-2, L-3, L-4, L-5, L-6, L-7, L-8 |
| **Total**| **15**| |

## Recommended v0.3 fix-pass scope

1. **De-duplicate the merge artefacts first** (H-1, H-2, M-1, M-2). These
   are mechanical edits and unblock everything else.
2. **Resolve the MUST-in-MAY conflict** (M-4) — same shape as PASS-1's
   ERR-018-003; uncontroversial split.
3. **Resolve the §3.5.2 ±20% / +5% conflation** (M-3) and the §3.7.5
   attribute-signature deferral (M-5) — both require a small rewrite, not
   structural change.
4. **L-1 … L-8 housekeeping** — bundle into the same v0.3 pass.

Each H- and M- finding warrants a fresh `ERR-018-NNN` row in
`spec-error-log.md` (suggested IDs `ERR-018-012` through `ERR-018-018`)
when the v0.3 fix pass begins.

---

## Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 1.0     | May 14, 2026 | Claude Code | PASS-2 adversarial review filed against v0.2 section files. 2 H / 5 M / 8 L = 15 findings. Root cause for H-1 / H-2 / M-1 traces to parallel-branch merge of PR #59 and PR #60. |
