# Youth Academy & Intake #42 — Section 4: Architecture

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

New assembly **`TacticalDirector.YouthAcademy`** (`src/youth-academy/`, at the T-phase). References
**`TacticalDirector.PlayerProgression`** (#28 — `RegenGenerator`, `AbilityModel`, `PlayerLifecycle`, its
constants), **`TacticalDirector.PlayerDatabase`** (#27 — `PlayerRecord` / `PlayerAttributes` /
`CLUB_SQUAD_SIZE`), and **`TacticalDirector.DeterministicSim`** (#16 — the RNG service + domain-tag
namespace). It references **neither #30 nor #34 nor #40**.

```
compositionRoot (season loop) ──► #42 YouthAcademy ──► { #28, #27, #16 }
        │                                 ▲
        ├─ invokes AdvanceAcademyDay at #30's academy tick slot
        ├─ assembles AcademyQuality from #34 / #40 (KD-3)
        ├─ applies IntakeResult / PromotionResult to the #27 Squad (KD-5)
        └─ composes the academy sub-blob into SeasonSaveCodec (KD-6)
                                          └── #29 / #32 / #38 consume promoted prospects + the view model (deferred)
```

**Acyclic.** No consumer references #42. #28 / #27 stay **schema-untouched at approval** — #42
constructs their existing types and calls their existing entry points.

**CS0104 pre-check.** #42 introduces no type whose bare name collides with an existing one in a scope
that sees both (`AcademyQuality` / `YouthProspect` / `AcademyState` / `IntakeResult` / `PromotionResult`
are all new names). The composition root will see #42 alongside #27/#28/#30 — a future #42 type named
`PlayerAttributes`, `TacticTranslation`, or similar would repeat the collision class this project has hit
twice (`TacticTranslation` at src/CLAUDE.md v1.73; `PlayerAttributes` at v2.19/v2.24) and MUST be avoided
at authoring time, not fixed after the compiler complains.

## 4.2 File layout (proposed, at T-phase)

| File | Contents |
|---|---|
| `AcademyQuality.cs` | the KD-3 caller-supplied value input (`Neutral == default`) |
| `YouthProspect.cs` | one prospect (#27 record + #28 lifecycle + academy fields) |
| `AcademyState.cs` | per-club state: latch, cohort, id high-water (the serialized surface) |
| `AcademyIntake.cs` | `AdvanceAcademyDay` (FM-YA-01) + `GenerateCohort` (FM-YA-03) |
| `AcademyAnchor.cs` | `DeriveActionOrdinal` + the stream ensure/anchor calls (FM-YA-02, KD-7) |
| `AcademyTransforms.cs` | `ApplyCeilingShift` + `ReanchorAge` — pure, no RNG parameter |
| `AcademyPromotion.cs` | `Promote` (FM-YA-04) |
| `AcademySaveCodec.cs` | `ACADEMY_SAVE_FORMAT_VERSION` sub-blob encode/decode (KD-6) |
| `AcademyConstants.cs` | the Appendix A catalogue |
| `AcademyViewModel.cs` | the read-only #38 observer |

## 4.3 The `AcademyQuality` input seam (KD-3)

#42 **declares the shape and consumes it**; the composition root **fills it**. At Stage 3 the root has no
producer wired, so it passes `AcademyQuality.Neutral` and #42 is provably an identity over #28. When #34
lands its coaching-quality projection and #40 its facility spend, the root maps them into the two dials —
**without any #42 change and without #42 ever referencing #34 or #40**.

This is the #29 `TrainingInput` / #34 projections-into-consumer-identity-types pattern. The critical
property is that `default(AcademyQuality)` **is** the identity (FR-YA-010): the zero-value enum/struct
trap that required explicit ctor seeding for `MarkingOrientation` (zero = `BallOriented`, not `Balanced`)
and `LineOfEngagement` (zero = `VeryLow`, not `Standard`) cannot occur, because zero per-mille is
arithmetically the identity rather than an arbitrary first enum member. A test locks this rather than
leaving it to inspection.

## 4.4 The RNG anchor call (KD-7 — decision recorded here)

The design supplement left one mechanism question open. **Decision: at T-phase, take the #16
`SeekStream(int streamIndex, ulong actionOrdinal)` back-prop rather than re-purposing `RestoreStream`.**

Rationale: `RestoreStream(index, in RngStreamState)` is the *save-restore* seam — its name, its
validation, and every existing call site say "reconstruct a persisted stream". Calling it every intake to
express "re-key this stream for a new keyed draw" would make every future reader of #42 believe a restore
is happening, and would couple #42 to the full `RngStreamState` shape when it needs to set one field. A
two-argument `SeekStream` states the intent, validates the one value, and is reusable by any future keyed
consumer (#41 derives its anchor per draw and would be a second user).

**Fallback if the #16 back-prop is declined:** call `RestoreStream` with a state whose `RngCursor` is 0
and `ActionOrdinal` is the derived anchor, with the intent documented at the call site. The **invariant**
(FR-YA-019 position-independence) is identical either way; only the call site's legibility differs. This
is recorded as a conditional back-prop in §8, not an approval blocker.

## 4.5 Save composition (KD-6)

`AcademySaveCodec.Encode(in AcademyState) → byte[]` produces the opaque sub-blob; the composition root
appends it to #30's `SeasonSaveCodec` frame as an additional opaque sub-blob (the **#41
`MEDICAL_SAVE_FORMAT_VERSION` / #33 `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` / #34 `STAFF_SAVE_FORMAT_VERSION`
precedent**, all "no `WORLD_STORE_FORMAT_VERSION` bump"), and the outer `SEASON_SAVE_FORMAT_VERSION` bump
is coordinated with #30 at T1 (**exact version TBD** — assigned by whichever T-phase lands first, never
hardcoded here).

The codec mirrors the `SeasonSaveCodec` fail-loud posture exactly: version gate first
(`ACADEMY_SAVE_FORMAT_VERSION`, F3), an **overflow-safe** `Require(offset, need, total)` bound compared
against **`total − offset`** (never `offset + need`, which can wrap on a corrupt near-`int.MaxValue`
length prefix — the `MatchSaveCodec` hardening), and a trailing-byte guard. The block is **opaque to
`SeasonSaveCodec`** (never parsed) and carries its own inner version gate, so the world / season / match
blobs stay byte-untouched. Layout in Appendix B.

**No RNG cursor is serialized** (FR-YA-020) — KD-7's per-intake anchor makes the next cohort a pure
function of `(worldSeed, clubId, intakeWorldDay)`, all of which are already in the blob or the world.

## 4.6 Interface contracts recorded for the composition root & #30

- **The composition root** MUST: invoke `AdvanceAcademyDay` at #30's academy tick-order slot (null until
  #42's T-phase); assemble `AcademyQuality` (Neutral until #34/#40 producers exist); apply
  `IntakeResult` / `PromotionResult` to the #27 `Squad` **atomically** with #42's academy-side change;
  pass `seniorSquadCount` into `Promote`; and compose #42's sub-blob into the season save. It MUST NOT
  let the UI mutate `AcademyState` directly, and MUST call the genesis seed **only at new-career
  genesis** — never on load, where `AcademyState` is reconstructed from the sub-blob (re-seeding a loaded
  career would destroy the restored cohort; the #34 §4.5 discipline).
- **The `PlayerId` authority (FR-YA-027).** #28 allocates ids for regens and #42 for prospects. The root
  MUST own **one** id authority both request from, or MUST partition the id space between them with a
  documented, serialized split. Two independent monotonic counters over one space is a collision waiting
  to happen and is explicitly **not** acceptable. This is a root contract because neither #28 nor #42 can
  see the other's allocator.
- **#30** MUST, at the T-phase: (a) carry the **academy tick-order null-seam slot** (ERR-030-007, filed at
  approval — §8); (b) bump `SEASON_SAVE_FORMAT_VERSION` composing the sub-blob. #30 grows **no**
  roster-commit for prospects beyond the one it already applies for #28's `RegenResult` — a promotion is
  the same shape of operation.
- **#28 / #27** are consumed by #42 calling their existing entry points and constructing their existing
  types. **#42 adds nothing to either, at approval or at T-phase** (KD-1/KD-2 exist to guarantee this).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §4 (assembly/reference direction + CS0104 pre-check, file layout, the `AcademyQuality` input seam, the KD-7 anchor-call decision (`SeekStream` back-prop preferred, `RestoreStream` fallback), save composition, root/#30/#28/#27 interface contracts incl. the `PlayerId` authority contract), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
