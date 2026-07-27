# New-Game Setup & Database Editor #47 — Section 9: Approval Checklist

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.3 — APPROVED: R-01..R-05 sign-off granted; back-props filed atomically)
**Last Updated (prior):** July 27, 2026 (v0.2 — G1 CLOSED; PASS-1 + AR-2 recorded)
**Version:** 0.3
**Status:** APPROVED

---

## 9.1 Content completeness

- [x] §1 scope / out-of-scope table / leaf DAG with the transitive-`season-save` argument / **§1.4's
      verification findings** / KD-1..KD-7 / determinism posture.
- [x] §2 FR-ED-001..032, data structures, failure modes F1..F8, and the *"attribute-only differentiation
      is not a failure"* note.
- [x] §3 FM-ED-01..04 with the writer and its round-trip contract, the setup handoff and its two root
      branches, the authored-`League` factory contract, the pin-precedence rule, the §3.5 no-arithmetic
      note, and thirteen worked examples.
- [x] §4 leaf assembly + CS0104 pre-check, file layout with three deliberate absences, the #38 hosting
      split, the root's two construction paths, the conditional sub-blob, neighbour contracts.
- [x] §5 test plan led by **the two locks the spec turns on**, then validation authority / authored
      construction and persistence / structural / setup delegation / localization / determinism /
      fail-loud + the T-phase closed-loop scenario.
- [x] §6 loop classification (**tooling, no cadence at all**), cost profile, `[GT]` ceilings, and §6.4
      identifying **save size** as the cost that actually matters.
- [x] §7 T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-7.
- [x] §8 XC-047-001..018 + the two back-props + the not-a-back-prop list.
- [x] Appendices A (constants), B (save layout), C (the authored-vs-generated comparison).

## 9.2 Constant-tag discipline

- [x] Every constant in Appendix A carries exactly **one** of `[FIXED]` / `[DERIVED]` / `[CROSS]` /
      `[CROSS-PENDING]` / `[GT]`.
- [x] No `[EST]` remains (none was introduced).
- [x] Empty regions omitted (#20 prohibits them) — #47 has **no `[CROSS-PENDING]` constants at all**,
      because it takes no determinism reservation (KD-6), so that region does not appear.
- [x] `[CROSS]` rows name their authority and are consumed read-only — #47 re-declares none of #27's or
      `season-save`'s types (T-ED-BOUND-004).
- [x] `[DERIVED]` rows document their formula and are never set independently.
- [x] **No `[GT]` constant governs validation.** Every bound #47 could be tempted to declare belongs to
      `SquadFileLoader` or `LeagueBootstrap` (FR-ED-017/023) — a #47-side copy would be the second
      authority KD-2 forbids, and Appendix A records the absence deliberately.
- [x] The `[GT]` budget ceilings are declared **ceilings, not measurements**, and §5 asserts no timing.

## 9.3 Verification of load-bearing claims (checked against source, not asserted)

- [x] **`SeasonSaveCodec` contains no roster data at all** — verified. With rosters *"REGENERATED from the
      world seed rather than saved"* (root `CLAUDE.md`), **an authored player is not derivable from any
      seed**. The fact the whole spec turns on, and the one the plan missed.
- [x] `LeagueBootstrap.Generate(ulong worldSeed, int clubCount) → League` is the generated origin;
      `League` **is** the `ISquadProvider`; `League`'s constructor is **`internal` to `season-save`**.
      *(`src/season-save/`)* — the three facts behind FR-ED-003 / FR-ED-008 / ERR-030-018.
- [x] **`season-save` references `MatchEngine` and `LivingWorld`** — the fact that makes a #47 →
      `season-save` reference transitively pull the whole simulation into an editor (§4.1 / R-5).
- [x] **`SquadFileLoader` exposes exactly `Parse(string text, int clubId) → Squad`, and there is no
      `Write` / `Serialize` / `ToText` anywhere** in `src/player-database/` or `src/season-save/` for this
      format. The plan's *"read/write contract"* is parse-only; #47 supplies the missing half.
- [x] `SquadFileLoader` **bounds every numeric key** and had an **unbounded `age`** corrected against its
      own *"out-of-range int all throw"* contract — found by a **later adversarial review**, not by a test.
      *(root `CLAUDE.md`, July-16 AR)*
- [x] `SquadFileLoader` computed `PlayerId` from a **raw section-local index** instead of the club-scoped
      formula — **caught by a round-trip test** at #27 T0. Together with the row above: the argument for
      making the round-trip a build gate. *(root `CLAUDE.md`, #27 T0)*
- [x] `LeagueBootstrapGoldenVectorTests` pins the generation digest, and `RosterGenerator`'s draw budget
      is contract-locked under an ORDINAL STABILITY rule — so a change to generation *"would silently
      rewrite every club in every existing save with the whole suite green."*
- [x] `Club` holds a **`StrengthDelta`**, the seeded ramp applied so a generated table is *"not 20
      statistically identical teams"* — the value authored clubs must take as `0` (FR-ED-009).
- [x] `LeagueBootstrap.Generate` validates `clubCount ∈ [2, MaxClubCount]`, fails loud on a too-small name
      catalogue, and fails loud when club count would exhaust `MaxRngStreams`, **with messages naming the
      constant to change**; `League.CreateSeason(managedClubId)` gates the managed club. Why #47 adds
      **no gate of its own**.
- [x] The Stage-0 authoring grammar is explicitly *"NOT a determinism-pinned wire format"* and the Stage-1
      loader *"may replace the grammar leaving `Apply` untouched"* — free **only because** #47 binds to
      loader types (FR-ED-021).
- [x] **#36's supplement states in terms** that *"#47's authoring lands in this same table — an authored
      entry is a pin like any other… #47 adds no #36 surface at all"*, and names **precedence** as the
      one thing #47 must decide. *(`docs/tracking/national-teams-international-design.md` KD-1)*
- [x] #43 **FR-CP-004**: *"`CompetitionId` MUST be config-assigned at genesis"* — so custom competitions
      are authored **config**, not a runtime API. *(`competition-structure/section-2.md`)*
- [x] #49 **FR-LC-001** governs *"all user-facing text"*, and **`NamedSlotSet` carries proper nouns as
      already-formatted string values** — which is how #22 passes `SubjectName` / `OpponentName` today.
      So FR-LC-001 is satisfied **by routing**; **no exemption is claimed**.
      *(`localization-accessibility/section-2.md`)*
- [x] `MatchSaveManager` deliberately made the match file **self-sufficient** by carrying the boot seed
      rather than referencing it — the decisive precedent against the hash-plus-external-file design.
- [x] **#16 §3.4 has no row and no `_RESERVED_` placeholder for #47**, and the roadmap classifies the
      editor as tooling. Nothing to file. *(`deterministic-sim/section-3.md` §3.4; roadmap §6)*
- [x] **`ERR-030-017` and `-018` are free** — the filed rows reach `ERR-030-014`, and `-015`/`-016` are
      claimed by #46/#36 in this same authoring pass. *(`docs/tracking/spec-error-log.md`; the sibling
      supplements)*
- [x] `FR-ED-*` is **unclaimed** — verified by enumerating every `FR-[A-Z]{2,3}-` prefix in `docs/specs/`.

## 9.4 Gates

| Gate | Owner | Status |
|---|---|---|
| **G1** — section-file PASS-1 adversarial review + a fix pass, to convergence. | drafter | ✅ **CLOSED** — see §9.4.1 |
| **G2** — file **ERR-030-017** and **ERR-030-018** atomically with the status flip. | drafter | ✅ **CLOSED** — filed and RESOLVED July 27, 2026, atomically with the flip (`spec-error-log.md` v1.46) |
| **G3** — lead-developer R-01..R-05 sign-off. | lead developer | ✅ **CLOSED** — R-01..R-05 granted by the lead developer, July 27, 2026 |
| **G4** — `SPEC_INDEX.md` registry row + Registry-Changes entry, added at promotion. | drafter | ✅ **CLOSED** — row + Registry-Changes entry landed July 27, 2026 |

**Not gating (deferred by design, recorded so they are not mistaken for omissions):** the
`SEASON_SAVE_FORMAT_VERSION` bump (T2); the editor **screen**, authored nationality pins, and
custom-competition genesis config (all T3); authored-file migration across #47's own versions (deep
tier); and the Stage-0+1 parser swap, which is **#27's** and leaves #47's contract intact.

**#47 carries no prerequisite gate and no `[GT]` balance pass.** It has no tunable behaviour — every
number it could balance belongs to a consumer (§9.2), so the usual post-approval balance item does not
apply. Worth stating, because its absence in a wave where every sibling carries one reads as an omission.

### 9.4.1 PASS-1 adversarial review record (G1)

**PASS-1: 0H + 4M + 7L, all resolved in the v0.2 fix pass.** The M findings cluster on the **load** side
of KD-1 — the supplement specified the authored write path carefully and left the read path's failure
modes unstated, which is where the silent-wrong-world failure lives.

| # | Sev | Finding | Resolution |
|---|---|---|---|
| M-1 | M | **Nothing said what happens when an authored save's sub-blob is missing.** The natural fallback — regenerate — is precisely the **silent wrong world** §1.4(a) identifies as this spec's worst failure: the career loads, nothing throws, and the player's authored league is quietly replaced by a generated one. The supplement's own R-1 names the failure but no requirement forbade the fallback. | New **F7** + **FR-ED-013**'s load half; **T-ED-I-006** asserts the throw and specifically that generation does **not** run. |
| M-2 | M | **The `HasAuthoredDb` marker and the sub-blob's presence are two facts with no coherence gate.** A generated save carrying a stale authored blob, or an authored save flagged generated, would decode cleanly and load the wrong rosters — the same class as #54's two-open-tenures finding. | New **F8**; **T-ED-I-013** asserts both directions; ERR-030-017 now records the gate. |
| M-3 | M | **"No partial authoring" lived only in the risk list.** *"Author a few clubs, generate the rest"* is the most natural feature request against this spec, and it re-opens the generator coupling FR-ED-007 rejects — a risk-list note is not what stops it being implemented. | Promoted to **FR-ED-010**, with §7.3 recording that it would be **its own decision with its own determinism argument**. |
| M-4 | M | **The `LocalIndexOf` hazard was implicit.** The loader's identity default is **club-scoped**, not section-local, so a writer that guesses produces different `PlayerId`s on parse — the exact defect the round-trip caught at #27 T0. §3.1 stated the round-trip but not the trap it exists to catch. | §3.1 names it; **T-ED-U-002** asserts it with a `ClubId` whose scoping makes the two formulas differ, so a coincidence cannot pass. |
| L-1 | L | **KD-7 lived inside KD-1**, where the *conditionality* of #47's save footprint — the claim a reviewer checks first — was reachable only through the authored-database decision. | Promoted to a key decision of its own, with the negative half (**no block, not an empty one**) stated at both §1.5 and T-ED-ID-002. |
| L-2 | L | The writer surface was typed `Write(in Squad)`; **`Squad` is a sealed class**, where `in` is legal and meaningless. | Corrected in §3.1 and §2.2. |
| L-3 | L | No requirement said the writer must emit only what the **documented grammar** specifies, as opposed to what the current parser happens to tolerate — which is what makes the promised parser swap free. | New **FR-ED-020/021**; **T-ED-U-004/005**. |
| L-4 | L | The **pin-precedence rule** existed as prose in the "not a back-prop" list, where a reader looking for #47's requirements would not find it — despite being the one thing #36 explicitly delegated. | Promoted to **FR-ED-026**, with §3.4 giving the reason (a re-key is a live event; the authored value described a starting state). |
| L-5 | L | §6 did not identify **save size** as #47's real cost, and gave no figure — leaving the ~100 KB authored blob, two orders of magnitude larger than any other management block, undiscussed. | §6.4 rewritten around it, with the **0 bytes** generated case stated alongside and R-7 added. |
| L-6 | L | Nothing recorded that #47 declares **no `[GT]` validation constant**, though the temptation is direct: every bound it might declare belongs to `SquadFileLoader` or `LeagueBootstrap`. | §9.2 and Appendix A record the absence as deliberate. |
| L-7 | L | `NewGameConfig`, `AuthoredDatabase` and `AuthoredClub` were described in prose only. | Written out in §2.2, with the missing strength field annotated at the type. |

**AR-2 sweep: 0H + 0M + 3L, all resolved — CONVERGENCE** (an L-only round closes the cycle, per the
project convention). **L-1:** §7.1 did not state that T2's `SEASON_SAVE_FORMAT_VERSION` bump applies to
**every** save (a frame change) while the sub-blob appears only for authored ones — so *"a generated game
stays byte-identical"* needed the qualifier *"after the bump"*, which is what T-ED-ID-002 actually
asserts. **L-2:** §6.2 did not say that **per-commit parsing is the mechanism** rather than an
inefficiency, leaving an optimisation target where the design intends a cost. **L-3:** §8.5 did not record
that the **grammar itself is not a citation surface** — documenting it in #47 would create the second
definition FR-ED-021 exists to prevent, since the round-trip *is* the specification.

## 9.5 Sign-off

| Role | Criterion | Signed |
|---|---|---|
| R-01 | Scope and out-of-scope boundaries are unambiguous; the #27 / `season-save` / #38 split is explicit, and **#47 constructs no `League` and validates nothing**. | ⏳ pending |
| R-02 | Every formula has units, ranges, and at least one worked example; no fabricated verification values — and the writer's correctness is stated as a **round-trip property**, not as a grammar restatement. | ⏳ pending |
| R-03 | Determinism posture is complete: tooling classification, the seed as an **input**, the generated path's byte-identity **including the golden vector**, and an authored game's independence from generation order. | ⏳ pending |
| R-04 | Persistence is version-gated, opaque, fail-loud, canonically ordered, APPEND-only and **conditional**; the missing-blob and flag/blob-mismatch cases both **fail loud** rather than falling back. | ⏳ pending |
| R-05 | Cross-spec back-props are enumerated with owners and timing, every proposed ERR id is verified free, and the **#27 no-change claim** — the one that keeps the editor cheap — is argued rather than asserted. | ⏳ pending |

## 9.6 Decision

**APPROVED — July 27, 2026.** Lead-developer **R-01..R-05 sign-off granted**, and the back-props filed and RESOLVED **atomically with the flip** per this spec's own promotion pipeline step 6: **ERR-030-017**, **ERR-030-018** (`spec-error-log.md` v1.46). All 11 section files carry `Status: APPROVED`; the `SPEC_INDEX.md` row records the date.

**What approval does and does not mean here.** It approves the **forward design** — the #21–#30 pre-T0 precedent — not an implementation: #47 has **no `src/` assembly**, and its §7 T-phase plan is the sequence for building one. Items listed as *not gating* above remain open by design and are named at their tiers.

**The prior decision text is retained below, because the reasoning it records is what the sign-off was granted against.**

**(prior, recorded at `IN REVIEW`)** — G1 closed (PASS-1 0H+4M+7L → AR-2 0H+0M+3L convergence, §9.4.1). G2–G4 remain open:
back-props land atomically with the status flip, sign-off is a human authority, and the registry row is
added at promotion.

**What verification did to this spec, restated at the decision point.** #47's plan claimed the editor
*"adds no new save block."* Checking against source showed that claim holds **only for a generated game**:
this project does not save rosters, it **regenerates them from the world seed**, so an authored player is
**not derivable from any seed** and cannot survive a save/load unless the data itself is persisted. The
plan's second, smaller error was calling the loader seam a *"read/write contract"* when **there is no
writer at all**.

**The failure the design exists to prevent is silent.** An authored career that did not persist its
rosters would load with **generated** ones — no exception, no corruption, just a different world than the
one the player built. That is why F7 forbids the fallback, why T-ED-I-005 loads with the source file
deleted, and why the artifact lives in the save rather than being referenced by hash.

**Two things this spec should be judged on.** First, that it changes **nothing** in #27, #36, #43, #49,
#16 or #50 — the editor is a new surface *over* an existing format, and its two back-props exist only
because `League`'s constructor is `internal` and the save frame is #30's. Second, that its entire
save-format footprint is **conditional**: a generated career pays zero bytes and stays byte-identical,
golden vector included.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §9 (completeness, tag discipline, the §9.3 source-verified claims table, four gates plus the explicit note that #47 carries **no** prerequisite gate and **no** `[GT]` balance pass — unusual in this wave — R-01..R-05). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | G1 CLOSED: §9.4.1 records the section-file PASS-1 (0H+4M+7L, all resolved — clustered on the **load** side of KD-1, where the silent-wrong-world failure lives) and the AR-2 convergence sweep (0H+0M+3L). §9.1 completeness updated for KD-7 and FR-ED-010/020/021/026; §9.2 gained the no-`[GT]`-validation-constant line; §9.3 gained the `season-save`-references-`MatchEngine` row, both `SquadFileLoader` defect rows, the #49 `NamedSlotSet` row, the `ERR-030-017`/`-018`-are-free check and the `FR-ED` prefix check. G2–G4 remain open. |
| 0.3 | 2026-07-27 | — | **`IN REVIEW → APPROVED`.** Lead-developer R-01..R-05 sign-off granted. Back-props **ERR-030-017**, **ERR-030-018** filed and RESOLVED atomically with the flip (`spec-error-log.md` v1.46). Gates G2–G5 closed; §9.6 decision updated. All 11 section files flip to `Status: APPROVED`. |
#endregion
