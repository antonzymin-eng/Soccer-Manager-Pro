# New-Game Setup & Database Editor #47 — Section 8: Cross-References & Back-Propagations

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-047-001 | `LeagueBootstrap.Generate(ulong worldSeed, int clubCount) → League` | The generated origin. **Untouched by #47** (FR-ED-002) — same call, same draw budget, same golden vector. |
| XC-047-002 | `SeasonSaveCodec` | Carries **no roster data at all** — verified. The fact that makes an authored player unrepresentable without a new block (§1.4(a)). |
| XC-047-003 | root `CLAUDE.md` — rosters are *"REGENERATED from the world seed rather than saved"* | The invariant #47's central decision follows through. |
| XC-047-004 | `LeagueBootstrapGoldenVectorTests` | The pinned generation digest. **Unchanged by #47**, asserted **inside #47's own suite** (T-ED-ID-003) — the #36 precedent for making the cost of touching generation visible in the consumer's tests. |
| XC-047-005 | `League` (sealed, **`internal` constructor** in `season-save`) | **#47 does not construct one** (FR-ED-003). The authored-source factory is a `season-save` addition (ERR-030-018), and this is what keeps #47 from taking a reference that transitively pulls `MatchEngine` and `LivingWorld`. |
| XC-047-006 | `League` **is** the `ISquadProvider` | Why an authored `League` is source-agnostic downstream (FR-ED-008) — nothing below branches on origin. |
| XC-047-007 | `Club.StrengthDelta` | The seeded ramp that stops a **generated** table being *"20 statistically identical teams"*. Authored clubs take `0` and **no ramp is applied** (FR-ED-009) — the one genuine difference between the two origins. |
| XC-047-008 | `SquadFileLoader.Parse(string, int) → Squad` | **The single validation authority** (FR-ED-017), and the arbiter of the writer's correctness (FR-ED-018). |
| XC-047-009 | The absence of any `Write` / `Serialize` / `ToText` in `src/player-database/` or `src/season-save/` | **Verified.** The plan's *"read/write contract"* is parse-only; #47 supplies the missing half (§1.4(b)). |
| XC-047-010 | `SquadFileLoader`'s two historical defects | The **club-scoped `PlayerId`** defect, caught by a **round-trip test** at #27 T0; the **unbounded `age`**, caught only by a later **adversarial review**. Together: the argument for making the round-trip a build gate rather than a review outcome (§3.1). |
| XC-047-011 | `TeamTacticFileLoader` / the Stage-0 grammar contract | The text format is *"NOT a determinism-pinned wire format"* and the Stage-1 loader *"may replace the grammar"* — which is free **only because** #47 binds to loader **types**, not syntax (FR-ED-021). |
| XC-047-012 | #30 T1's `SeasonState` (ERR-030-011 class) | **Constructible but not decodable** — the encode/decode asymmetry the writer's round-trip contract exists to cover. |
| XC-047-013 | `LeagueBootstrap.Generate`'s own gates | Validates `clubCount ∈ [2, MaxClubCount]`, fails loud on a too-small name catalogue, and fails loud when club count would exhaust `MaxRngStreams` — **with messages naming the constant to change**. Why #47 adds **no gate of its own** (FR-ED-022/023). |
| XC-047-014 | `League.CreateSeason(managedClubId)` | Gates the managed club. The second half of the delegation above. |
| XC-047-015 | #36 KD-1 / the `NationPin` table | *"#47's authoring lands in this same table — an authored entry is a pin like any other… #47 adds no #36 surface at all."* #47 supplies the **precedence rule** #36 left open (FR-ED-026). |
| XC-047-016 | #43 FR-CP-004 | *"`CompetitionId` MUST be config-assigned at genesis (deterministic; instance 0 = 0; never reused)"* — so custom competitions are authored **config**, not a runtime API (FR-ED-024). |
| XC-047-017 | #49 FR-LC-001 / `NamedSlotSet` | FR-LC-001 governs *"**all** user-facing text"* and a club name is user-facing. **No exemption is claimed**: proper nouns travel as **slot values** exactly as #22 passes `SubjectName` / `OpponentName`, so the name is **routed without being translated** (FR-ED-031). |
| XC-047-018 | `MatchSaveManager`'s self-sufficiency precedent | The match file carries the boot seed rather than referencing it. The decisive argument against the rejected hash-plus-external-file design (§4.5). |

## 8.2 At approval — land **atomically** with the status flip

| ID | Target | Change | Kind |
|---|---|---|---|
| **ERR-030-017** | `season-competition-loop/section-3.md` + the season-save composition | Record that `SeasonSaveCodec` composes an **optional** `AUTHORED_DB_SAVE_FORMAT_VERSION` sub-blob, present **only** for an authored game (KD-1(ii)), and that a **generated** game's frame is unchanged — **no block, not an empty one**. Also record the **coherence gate**: the `HasAuthoredDb` marker and the sub-blob's presence must agree, and a mismatch fails loud (F8). This is the one place #47 touches the save, and it is **conditional**. | Doc-only composition note |
| **ERR-030-018** | `season-save` / `League` | An **authored-source factory** for `League` — `Club[]` + `Squad[]` in, **no strength ramp applied** (FR-ED-009), with ascending-unique-id and one-squad-per-club guards. **`League`'s constructor is `internal` to `season-save`, so this must live there**; #47 supplies values and the root calls it. Also records that a `League` built this way is **`ISquadProvider`-identical** to a generated one, which is what keeps every downstream consumer source-agnostic. | New factory surface in `season-save` |

**Both back-props exist because of the same fact**, and it is worth stating once: `League` is
`season-save`'s type with an `internal` constructor, and the save frame is #30's. #47 owns the **data**
and the **format**, and neither of the two places that data must reach is #47's to change from the inside.
That is the same inversion #46's projectors and #49's boundary adapters use, and it is what keeps #47 a
leaf over #27 alone.

## 8.3 Deferred — land at the named tier, **not** at approval

- The **`SEASON_SAVE_FORMAT_VERSION` bump**, at **T2** when the sub-blob is first composed in.
- **Custom-competition genesis-config authoring**, on #43's FR-CP-004 shape (T3) — a #47-side *use* of an
  existing config surface, **not a #43 change**.
- **Authored nationality pins** (T3) — entries in #36's existing table; **no #36 surface change**, and the
  precedence rule ships with #47 at approval as FR-ED-026.
- The **Stage-0+1 text→binary parser swap**: the editor binds to loader **types**, so the swap is **#27's**
  and leaves #47's contract intact (FR-ED-021).
- **Authored-file migration** across #47's own format versions (FR-ED-032) — distinct from #50's *save*
  migration, at the deep tier.

## 8.4 Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **#27 — nothing to change.** The writer is a **new surface #47 adds over #27's format**, not an
  amendment to #27's model or grammar. #27 has no writer *because it never needed one* — the game reads
  authored text and generates everything else — so supplying one is #47's job by definition. `Parse` stays
  the sole authority, unchanged.
- **#36 — nothing to change.** Authored nationalities are entries in the `NationPin` table #36 **already
  ships** for transfer re-keys, and #36's own supplement says so in terms. #47 **closes** #36's one open
  question (precedence) **from #47's side**, as FR-ED-026 — which is why this is not a back-prop against
  #36.
- **#43 — nothing to change.** Custom competitions write the genesis config FR-CP-004 already defines. #47
  uses the surface as specified.
- **#49 — nothing to change, and no exemption claimed.** Authored proper nouns are `NamedSlotSet` **slot
  values**, which is how #22 already passes names. FR-LC-001 is satisfied **by routing** rather than by
  translating, so the MUST is met rather than argued around.
- **#16 — no row, no `_RESERVED_` placeholder, nothing at all.** #47 is tooling: no stream, no tag, no
  ordinal, and the `worldSeed` is an **input** rather than a draw (FR-ED-029/030). Worth naming because
  most specs in this wave file *something* against #16, and #47's total absence there is a deliberate
  classification rather than an oversight.
- **#50 — nothing to change.** An authored database is an **input artifact**, not a live save. Migrating
  *saves* stays #50's; migrating an authored **file** across #47's own format versions is #47's, at the
  deep tier (FR-ED-032). Recording the split here means #50 inherits a stated boundary rather than
  discovering an assumption.
- **#38 — nothing imposed.** #38 hosts a screen over #47's data layer through surfaces #38 already
  defines (`IViewModelSource<T>`, commands). #47 adds a consumer, not a requirement.

## 8.5 References

#47 introduces **no external citation**. Its content is a file format, a handoff, and a set of boundaries
composed from this project's own approved specs and shipped source; there is no published result it rests
on, and inventing a citation to decorate the section would be the fabrication the project's rules forbid.

Note in particular that **the authoring grammar is not a citation surface**: it is #27's, it is explicitly
*"NOT a determinism-pinned wire format"*, and #47's writer is specified **against the parser** rather than
against any external format standard. Documenting the grammar here would create the second definition
FR-ED-021 exists to prevent — the round-trip is the specification.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §8 (XC-047-001..018, the two approval-time back-props with the single fact they share stated once, the deferred set, the not-a-back-prop list — which names #16's *total* absence as a deliberate classification, since most specs in this wave file something there — and the no-external-citation rationale extended to record that the grammar itself is not a citation surface: the round-trip is the specification). Status IN REVIEW. |
#endregion
