# Steam Packaging & Release Engineering #39 — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.4 — AR-3 sweep: 0H+0M+2L, **CONVERGENCE**; prior v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.4
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#39** · **FR prefix:** `FR-PK` · **Wave:** 8 (LAST) · **Tier:** S2
> **Promoted from:** `docs/tracking/spec-plans/spec-39-steam-packaging-release.md` v0.1

---

## 0. Purpose and posture

This supplement resolves the five key decisions the #39 plan defers, against **verified** source rather
than assumption. Design only — no code, no section files, no registry row.

The plan is right about what #39 is (process and contract, not sim code) and right that it is authored
last. Verification changes two things about *how* it must be written:

- **The release gate cannot be modelled on CI, because CI is skip-open and #39 must be fail-closed.**
  The repository's Unity jobs are *gated on a secret* and report **success** when that secret is absent
  (§2(b)). A green pipeline today is compatible with "no player artifact was ever built, and no Unity
  test ever ran" — the two facts a ship decision most depends on. KD-2 inverts the posture.
- **Steam Cloud introduces a second writer to a save file whose entire discipline assumes one.** Every
  save path in the project is local, single-writer, and atomic-by-rename; Cloud can replace that file
  underneath a running process and can deliver a save written by a *different build* (§2(d)). KD-1/KD-5
  bound it, and delegate every version judgement to #50 rather than re-deriving one.

## 1. Scope

**#39 owns:** the **build/packaging pipeline** definition, the **release gate** (what must be true, with
evidence, before an artifact ships), the **Steam Cloud sync policy** over the existing save file, the
**achievement** model as a read-only derivation, and the **store/compliance asset checklist**.

**#39 does not own:**

| Not owned | Owner | How #39 relates |
|---|---|---|
| The save **format** and its version constants | #30 (frame) and each sub-blob's owning spec | #39 *transports* the file; it parses nothing and bumps nothing (KD-5) |
| **Version classification, migration, refusal** | **#50** | #39 asks #50 and obeys the answer; it never compares versions itself (KD-1) |
| The **certification machinery** (determinism KAT, perf baseline, the platform pin) | #16 / #18 / `certification-platform.md` | #39 **consumes its evidence** as a gate input; it defines no new determinism proof (KD-2) |
| **User-facing text** for conflict/refusal dialogs | **#49** | #39 emits an identity + slots; #49 renders (KD-1) |
| Any **sim or gameplay behaviour** | every other spec | #39 is downstream of all of it and mutates none of it (KD-3/§6) |
| **Store copy, art, trailers** as *content* | marketing/art production | #39 specifies the **checklist and its gate class**, not the assets (KD-4) |

## 2. What already exists (verified)

**(a) There is no build pipeline of any kind.** `.github/workflows/ci.yml` defines eleven jobs —
`markdown-lint`, `yaml-lint`, `link-check`, `spec-hygiene`, `file-manifest-check`, `csharp-format`,
`dotnet-compile-test`, `unity-meta-integrity`, `unity-asset-hygiene`, `unity-license-check`,
`unity-tests` — and a tree-wide search for `BuildPlayer` / a player-build invocation returns **nothing**.

**Consequence:** #39 does not harden an existing pipeline; it **introduces the first one**. That is
scope-defining: the spec's §3/§4 must describe a pipeline from zero, and the "reproducibility" claims it
makes are claims about a process that does not yet exist and can therefore still be designed to be
checkable.

**(b) The Unity half of CI is skip-open — it reports success while running nothing.** `unity-tests` is
declared `needs: unity-license-check` with `if: needs.unity-license-check.outputs.configured == 'true'`,
and `unity-license-check` emits `configured=false` plus a *notice* (not a failure) when `UNITY_LICENSE`
is unset. The job's own comment says it is *"cleanly SKIPPED (not failed) until then."*

That is the correct choice **for CI** — a contributor without secrets should not see red. It is the
**exact wrong** choice for a ship gate, and the two must not share a posture. A skipped job is
indistinguishable from a passed one in a summary status, so "CI is green" carries no information about
whether EditMode/PlayMode tests ran at all.

**Consequence:** KD-2's gate is **evidence-positive** — it requires artifacts asserting that named checks
*executed on a named build*, and treats their absence as failure. "Nothing was red" is not an input.

**(c) The certification machinery is real, complete, and host-bound.** `certification-platform.md` v1.4
is **✅ PINNED** on Windows 11 / Unity 6000.4.9f1 / DX11 / Mono / x64 / SSE4.2 / 1 worker /
DAZ·FTZ·fp-contract·FMA all off. Two certified runs exist against it: the determinism KAT
(44 passed / 0 failed / 4 deferred skips — `cert-runs/determinism-cert-2026-07-19.md`) and the
FR-PO-052 per-tick perf baseline (p50 = 0.4768 ms, p99 = 2.5669 ms, promoted PENDING → CERTIFIED). The
`cert-run-runbook.md` is the operator checklist, and it states in its own words that the Linux
`tools/dotnet-ci` gate is **explicitly NON-certifying** — *"a number sourced from it would be a
fabricated certification."*

**Consequence:** #39 must **reuse** this, not re-invent it (KD-2), and inherits its constraint: the
certifying half of the release gate **cannot run in CI**. The gate is therefore a runbook executed on the
pinned host that produces a committed evidence record, with CI supplying necessary-but-insufficient
signal. This is a limitation to state plainly, not to engineer around.

**(d) The save is one atomic local file with a version-first frame and opaque sub-blobs.**
`SeasonSaveCodec` writes `SEASON_SAVE_FORMAT_VERSION` first, then a `matchPresent` flag byte, then three
length-prefixed sub-blobs — the living-world composite (always), the season state (always), the match
save (only mid-match) — and it *"never parses"* a sub-blob. Writers use the `temp → fsync → rename`
discipline (`SaveManager` / `MatchSaveManager`, and #50 KD-4 extends it).

**Consequence:** Cloud sync can be **whole-file only** (KD-5) — there is no meaningful per-blob sync, and
attempting one would require #39 to parse a frame it has no business parsing. It also means the
mid-match blob is not separable from the file: a mid-match save syncs or it does not, as one unit.

**(e) #50 already owns every version judgement #39 needs, and named #39 as its consumer.** The
save-migration supplement (landed July 26, 2026) defines `Classify(in header) → SaveClass` over
`{ Current, Migratable, TooNew, Unsupported, Corrupt }`, and a surface listed explicitly as
`CompareForConflict(a, b) → …` with direction **`#50 → #39`**. Its KD-5 already pins two rules that bind
#39:

> *"a save that is `TooNew` for this build **cannot be resolved by this build at all** … a migrated save is
> **written back only on the player's next explicit save**, never silently on load."*

It also introduces `WORLD_GENERATION_VERSION` (KD-2) because rosters are *regenerated, not stored*, so a
save is only reproducible against the generator code that wrote it.

**Consequence:** #39's conflict rule is a **thin policy over #50's classification**, and the sharpest
coupling the plan worried about (§9) resolves by ownership rather than negotiation: #39 never compares a
version number itself. The generation version matters here more than anywhere else — Cloud is precisely
the mechanism that carries a save between two machines running two different builds.

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | A reproducible build of the current tree; **Cloud disabled**; the release gate run manually from the runbook against a committed evidence record; a small achievement set evaluated read-only; the compliance subset of the asset checklist. With Cloud off, the save path is byte-for-byte today's local single-writer path — the identity. |
| **Deep** | Cloud enabled with the KD-5 quiescence rule + #50-delegated conflict resolution; a fuller achievement set; multi-branch/beta packaging; the gate automated as far as the host constraint permits. |

The minimal tier is genuinely shippable, and "Cloud off" is the identity in the strict project sense: no
new writer, no new failure mode, and every existing save test still describes the shipped behaviour.

## 4. Key decisions

### KD-1 — Cloud conflict resolution is a **policy over #50's classification**, never a second opinion

#39 asks #50 to classify each candidate save and to compare the pair; it consumes the answer. The policy
#39 adds on top is short, and every clause exists to prevent a specific way of losing a career:

| Situation | Rule | Why |
|---|---|---|
| Either side classifies `TooNew` | **This build resolves nothing** — surface the conflict, touch neither copy | Per #50 KD-5; a build cannot reason about a format it does not know, and "newer wins" is how the newer copy gets overwritten by a build that could not read it |
| Both `Current`, contents differ | **Player chooses**; no automatic merge, ever | Two divergent careers are not mergeable — a save is one causal history, and picking silently discards the other |
| Local `Migratable`, remote `Current` | Resolve as a normal conflict on the *pre-migration* versions | Migration is not a tiebreaker; treating a migrated copy as "newer" would let opening a career on machine B rewrite machine A's cloud copy |
| Any side `Corrupt` / `Unsupported` | Never auto-select it, never delete it | #50 KD-4's non-destructive refusal extends to Cloud: a refused save is left exactly as found |

**Every row above requires the bytes, so conflict resolution runs only after both copies are local.**
`Classify` reads version fields *inside* the file (#50 KD-1) — it cannot be evaluated against Cloud
metadata. #39 therefore fetches the remote copy to a staging path first and classifies it there;
resolving on **timestamp or size alone is forbidden**, because that is precisely the "newer wins"
heuristic every row of the table exists to reject. A metadata-only decision would happily overwrite a
`TooNew` save with an older one and call it a sync.

**A migrated save is never uploaded on load** — only on the player's next explicit save (#50 KD-5,
inherited verbatim). The failure this prevents is concrete: open the career on the new build, let it
sync, then return to the older build on the other machine and find the save now refuses to load. A read
must never become a write.

**Conflict UX text routes through #49** as slots + an identity, like every other producer; #39 bakes no
strings.

### KD-2 — The release gate is **fail-closed on positive evidence**, and it is not CI

This is the decision §2(b) forces. The gate is a set of **evidence artifacts**, each naming the commit it
describes, and each of which must be **present and affirmative**:

| Evidence | Source | Absent ⇒ |
|---|---|---|
| Determinism KAT run record on the pinned tuple | `cert-run-runbook.md` → `cert-runs/` | **FAIL** |
| FR-PO-052 perf baseline, CERTIFIED (not PENDING) | `CertifiedPerfBaseline` + the `.cert.md` record | **FAIL** |
| Unity EditMode + PlayMode results, **executed** | `unity-tests` artifacts | **FAIL** (a skip is not a pass — §2(b)) |
| Full `dotnet-ci` suite green, quarantine empty | `tools/dotnet-ci/run-gate.sh` | **FAIL** |
| Packaged-build smoke path (launch → save → relaunch → load) | the artifact itself | **FAIL** |
| Compliance subset of the asset checklist | KD-4 | **FAIL** |

**The inversion is the whole point.** CI answers "did anything break?" and is right to skip what it cannot
run. The gate answers "is this specific artifact proven shippable?", where an unanswered question is a
no. The same `unity-tests` job therefore feeds both, read with opposite defaults — and the gate reads its
*artifact*, not its status.

**Five of those six rows verify the *project at a commit*, not the *packaged binary* — and the spec must
say so.** The KAT, the perf baseline, the Unity Test Runner and `dotnet-ci` all execute against the
project's assemblies under a test runner; none of them runs inside a shipped player. Treating them as
artifact evidence would repeat, one level up, the mistake §2(b) catches: claiming a property that was
never measured on the thing being claimed about. So the gate is explicitly two-part:

- **Input-side evidence** (the five rows): binds to a **commit**, and is only admissible if that commit
  is the one the artifact was built from — which is what makes R-1's commit-matching a gate rule rather
  than hygiene.
- **Artifact-side evidence**: a **packaged-build smoke path** — launch the shipped player, create a
  career, save, quit, relaunch, load, advance a day. Small on purpose, because it is the only class of
  check that can catch a packaging-only failure (a stripped assembly, a missing asset, a broken path)
  that every project-side test passes straight through.

**The gate does not run in CI** (§2(c)): its certifying inputs are pinned-host-only, and sourcing them
from Linux would be a fabricated certification in the runbook's own words. So #39 specifies a
**release runbook** — the direct descendant of `cert-run-runbook.md`, which already proves the pattern
works — and the evidence records are committed artifacts, reviewable after the fact.

**Honest bound on "reproducible" (KD-6 below):** the gate does not require a bit-identical binary.

### KD-3 — Achievements are read-only derivations, persisted **outside** every sim save, and never read back

Three properties, in decreasing obviousness and increasing importance:

1. **Derived, not authored into the sim.** An achievement is a predicate over events the career already
   emits — the same read-only posture as #37 analytics and #44 discipline, and the same aggregation shape
   #46's inbox already defines. #39 adds no event, no hook, and no sim field.
2. **The store of record is the platform, and the local store is an offline queue — not a second truth.**
   Steam owns unlock state for a Steam achievement; a local file that also "owns" it produces the classic
   double-write (unlock granted twice) or lost-unlock (granted offline, never flushed). So the local
   store holds **pending unlocks awaiting flush** plus the running counters a predicate needs between
   sessions, and reconciles *from* the platform on connect — platform wins on any disagreement about
   whether something is unlocked. It is **never** a save sub-blob: putting it in the season save would
   make the save's byte content depend on which achievements a *player account* had earned, and would
   enlist #50 in migrating trophy state.
3. **Nothing in the sim may read achievement state.** This is the one that turns a cosmetic feature into
   a determinism defect if violated: a sim that branched on "has this player unlocked X" would make
   replay depend on account state that is not in the save. The spec states it as a prohibition, and §9
   tests it as one.

**Achievement evaluation lives in the client shell, not in #39's assembly** — the `ICueSink` inversion
#48 already uses. #39 defines the predicate contract and the identity set; the shell wires it to the
event surface. That is what keeps #39 a leaf (§10) despite consuming career events.

### KD-4 — The checklist has **two gate classes**, and only one of them can block

- **Compliance / hard gate** (blocks the release): the age-rating declaration, third-party licence and
  attribution manifest, EULA/privacy text present and localized through #49, the Cloud configuration
  matching KD-5's file set, the crash/exception reporting path, and the KD-2 evidence set.
- **Marketing / soft** (does not block): capsule art, trailer, screenshots, store copy, tags.

The split exists because a checklist without one becomes either theatre (nothing blocks) or a hostage to
a missing screenshot (everything blocks). **#39 specifies the checklist and its gate class; it does not
specify the assets** — the same boundary #48 drew for animation and audio content.

### KD-5 — Cloud syncs **whole files at quiescent boundaries**; the running game owns the file

Per §2(d) the save is one atomic file, so:

- **Whole-file sync only.** No per-sub-blob sync; #39 parses nothing.
- **The running game owns its save file.** Sync is permitted when no write is in flight and the game is
  not mid-write — the `temp → fsync → rename` discipline already guarantees the cloud never observes a
  partial file, and the policy adds that the game must not *load* a file that sync is replacing.
- **A mid-match save syncs as a unit or not at all.** The `matchPresent` blob rides inside the file; there
  is no coherent state in which the world syncs and the match does not, and #39 must not create one.
- **Sync on quiescence** — save completion and clean exit — rather than continuously mid-session. This
  is the conservative default; a session that ends by crash falls back to the last completed save, which
  is exactly what the local behaviour already is.

*Rejected alternative:* sync continuously so the cloud is always current. Rejected — it maximises the
window in which two machines hold divergent in-progress careers, which is the state KD-1 says cannot be
merged. Reducing conflict frequency is worth more than reducing conflict staleness.

### KD-6 — "Reproducible build" means **behaviourally certified**, not bit-identical

The honest claim, stated because the tempting one is unverifiable: a Unity/Mono player build embeds
timestamps and non-deterministic ordering that make byte-identical rebuilds a project in themselves, and
#39 must not gate on a property no one has demonstrated here.

What #39 **does** require is stronger where it matters and checkable today: the pinned source commit, built
on the pinned tuple, **reproduces the certified digests** — the determinism KAT's golden vectors and
digest chains (§2(c)). That is *behavioural* reproducibility, it is what a save, a replay, and a future
netcode peer actually depend on, and the machinery to check it already exists.

**Scope of that claim, per KD-2's two-part split:** it is measured **project-side**, so it certifies the
*inputs* the artifact is built from, not the player binary itself. The artifact inherits it only through
the commit binding (R-1) plus the smoke path (KD-2). Stating it the other way round would re-import the
exact error this supplement opened by naming.

Bit-identical packaging is recorded as a Stage-5+ deferral (it becomes materially more valuable when
#52's peers must agree), not a Stage-2 gate.

### KD-7 — Determinism posture and identity

Infra: **no RNG stream, no domain tag, no `SubsystemOrdinal`**; #16 has no row for #39 and needs none
(the roadmap §6 classification, which the read-only/presentation/infra specs #37/#44/#46/#48/#47/#50 all
share). #39 draws nothing, ticks nothing, and serializes nothing into a sim save.

**Identity:** with Cloud disabled and no achievements defined, the shipped game's save behaviour is
byte-for-byte today's local path, and the gate reduces to the existing runbook. Every addition is
additive over that.

## 5. Persistent state (shape)

**No sim persistent state, and no format-version bump.** #39 adds two **client-local** stores, both
outside every determinism-gated save (the #38/#49/#51 settings class):

```
AchievementProgress : { pendingUnlocks : set<AchievementId>,          # offline queue, flushed on connect
                        counters       : map<AchievementId, int> }    # cross-session predicate state
CloudSyncState      : { lastSyncedSaveId, lastSyncedAt }              # diagnostic only
```

`AchievementProgress` is a **queue and a counter store, not an unlock ledger** — the platform holds
unlock truth (KD-3(2)). `CloudSyncState` is **diagnostic only**: it deliberately caches no remote version,
because a cached version is a decision input that can be stale, and KD-1 requires classification from the
fetched bytes every time.

## 6. Determinism posture

- Infra; no stream, tag, or ordinal (KD-7).
- The sim **may not read** achievement or Cloud state (KD-3(3)) — the one rule whose violation would be a
  genuine determinism defect rather than a packaging bug.
- Build *behavioural* determinism is verified by the existing KAT, not by a new mechanism (KD-6).
- Cloud transports bytes and never rewrites them; every version decision is #50's (KD-1).

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| Release runbook + evidence record format | process | descendant of `cert-run-runbook.md`; fail-closed (KD-2) |
| `ReleaseGate` evidence checklist | process → release decision | absence ⇒ fail; a skipped job is not a pass |
| Packaged-build smoke path | process, run on the artifact | the only artifact-side evidence (KD-2 / R-1a) |
| `ICloudSyncPolicy` (quiescence + whole-file) | shell → #39 | #39 defines the policy; the shell binds the Steam API (KD-5) |
| `ResolveConflict(localClass, remoteClass, ordering) → Outcome` | #39, over #50's `CompareForConflict` | pure policy; no version arithmetic of its own (KD-1) |
| `AchievementDefinition { Id, predicate over career events }` | #39 → shell | evaluated in the shell; #39 references no sim assembly (KD-3) |
| `ConflictNotice` (identity + slots) | #39 → #49 | #39 bakes no strings |
| Store/compliance checklist with gate class | process | hard vs soft (KD-4) |

`AchievementId` carries **APPEND-only ordinal stability** (the `CueId`/text-intent precedent) — a shipped
achievement's identity is player-visible and cannot be renumbered.

## 8. Cross-spec back-props

### 8.1 At approval

**None.** This is a positive finding rather than an omission: #50 already specifies `CompareForConflict`
with direction `#50 → #39` and already pins the `TooNew` and write-back-on-explicit-save rules (§2(e)),
so #39 fits an existing contract instead of amending one — the same relationship #45 had to #40's
pre-specified `BoardModifier`. #39 consumes #49 as an ordinary text producer, which is an extension
point, not a change.

### 8.2 Deferred (land at the named tier)

- The **CI workflow addition** for a player-build job, at the first packaged build — infrastructure, not
  a spec amendment, and deliberately not written before an artifact exists to build.
- The **achievement identity set**, at the first release — content, and subject to KD-3's append-only rule
  once shipped.
- **Bit-identical packaging** (KD-6), Stage 5+ alongside #52.

### 8.3 Explicitly **not** back-props

- **#30 / the codecs** — #39 transports the file and parses nothing (KD-5).
- **#50** — the dependency runs #50 → #39; duplicating classification in #39 is the specific error KD-1
  exists to prevent.
- **#16** — no stream, no tag, nothing reserved (KD-7).
- **`certification-platform.md`** — #39 consumes the pin; it does not re-pin it.

## 9. Test focus

**The gate's fail-closed property, tested as a property** (KD-2): a gate evaluation with a *missing* or
*skipped* evidence artifact must fail — constructed by feeding it the real skip-shaped `unity-tests`
output from an unlicensed run, which is the exact input §2(b) shows CI treats as success. This is the
single most important test in the spec, because it tests the inversion the spec exists to introduce.

**Cloud conflict matrix** (KD-1), exhaustively over #50's five classes × {local newer, remote newer,
divergent}: `TooNew` on either side resolves to *no action*; no case auto-merges; no case deletes; a
`Migratable` local never uploads on load. **Plus the negative lock the matrix depends on:** a resolution
attempted with only metadata (timestamp/size) and no fetched bytes must **refuse**, not guess — the test
constructs an older-but-newer-timestamped copy, which is the input a "newer wins" implementation gets
wrong. **Whole-file/quiescence** (KD-5): a mid-match save syncs as one
unit; a sync never observes a partial file; a save round-trips through a simulated sync byte-identically
and loads. **Achievements** (KD-3): evaluation is observer-neutral — a career advanced with achievement
evaluation active produces a digest chain byte-identical to one without (the `MatchViewerTests` lock,
extended); progress lands in no sim save; a static check that no sim assembly reads achievement state;
and the offline path — an unlock earned with no connection flushes exactly once on reconnect, and a
platform that already holds it is not re-granted (KD-3(2)). **Identity** (KD-7): Cloud off + no achievements ⇒ save behaviour byte-identical to today.
**Reproducibility** (KD-6): a build from the pinned commit on the pinned tuple reproduces the certified
digests — and the spec asserts *that*, not bit-identity.

## 10. Reference DAG

```
shell → {#39, #50, #49, sim}        #39 → {#50}        #50 → { }        sim → { }
```

**Acyclic, and #39 is a leaf but for #50.** #39 holds policy over *classifications* and *evidence
artifacts*, not over domain types: achievement predicates are evaluated by the shell against the event
surface (KD-3), and the Steam API binding is the shell's. Had #39 evaluated achievements itself it would
reference the career/season assemblies — and `season-save` reaches `MatchEngine` and `LivingWorld`, so
the packaging spec would transitively depend on the whole simulation. The inversion is the same one #48
uses for `ICueSink` and #50 uses for its registered generators, and it is load-bearing in exactly the
same way.

## 11. Risks and standing options

- **R-1 — the gate is only as good as its evidence being *fresh*, and this is now load-bearing rather
  than hygienic.** Because five of six evidence rows measure the project rather than the artifact
  (KD-2), the commit identity is the *only* thing binding them to the build being shipped. An evidence
  record naming a different commit is worse than none, because it looks like a pass. Every record carries
  its commit; the gate compares it to the artifact's; a mismatch fails.
- **R-1a — the packaged-build smoke path is the only artifact-side check, so its coverage is a real
  decision.** Too small and packaging failures ship; too large and it becomes a second test suite
  maintained in the worst possible environment. The eventual §5 should justify its steps individually.
- **R-2 — the host constraint is permanent, not transitional** (§2(c)). The certifying half cannot move
  into CI without abandoning the pin. Any future proposal to "just run the cert in CI" is a proposal to
  stop certifying.
- **R-3 — Cloud is the first second-writer this project has ever had** (§2(d)). Most save bugs that
  reach players will arrive through it, and they will look like corruption while being conflict
  mishandling. KD-1's non-destructive rules are what keep those recoverable.
- **R-4 — the generation version bites hardest here** (#50 KD-2): two machines on different builds is the
  normal Cloud case, not an edge case, so #39 should expect `Unsupported` in the wild and treat a clean
  refusal as a success path rather than an error.
- **R-5 — front-loading** (the plan's own risk). The checklist and the policy can be written now; the
  gate can only be *exercised* against real artifacts, and a gate never exercised is a document. The
  spec should say which parts are exercisable pre-artifact.

## 12. Promotion pipeline

1. **This supplement, AR-converged** — **DONE at v0.4.** AR-1 (0H+2M) → v0.2, AR-2 (0H+1M) → v0.3,
   AR-3 (0H+0M+2L) → v0.4 = **CONVERGENCE** (an L-only round closes the cycle, per the project
   convention).
2. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/steam-packaging-release/`, FR
   prefix `FR-PK`.
3. **Section-file PASS-1 adversarial review** + a fix pass, recorded in §9.4.1 of the checklist.
4. **`SPEC_INDEX.md` registry row** at promotion.
5. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
6. **Flip to `APPROVED`** (no §8.1 back-props to land — §8.1).

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement promoted from the one-page plan. Verification supplies the posture the plan could not: **(1) there is no build pipeline at all** — eleven CI jobs and no player build anywhere in the tree — so #39 introduces the first one; **(2) CI is skip-open** (`unity-tests` is gated on `UNITY_LICENSE` and *skips clean* when unset, by design), so a green pipeline is compatible with "no artifact built, no Unity test run" — which makes an evidence-positive, **fail-closed** release gate (KD-2) the spec's central decision rather than a checklist detail; **(3) the certifying machinery exists and is host-bound** (`certification-platform.md` v1.4 PINNED; KAT 44/0; perf CERTIFIED; the Linux gate explicitly non-certifying), so #39 reuses it and inherits the constraint that the gate cannot run in CI; **(4) Steam Cloud is the project's first second writer** to a save file whose discipline assumes a single local atomic writer, resolved by whole-file sync at quiescent boundaries (KD-5); **(5) #50 already owns every version judgement and named #39 as its consumer** (`CompareForConflict`, `#50 → #39`), so KD-1 is a thin policy over #50's classification and **§8.1 files no back-prop**. KD-3 states the achievement prohibition that would otherwise be a real determinism defect (the sim may never read achievement state) and keeps progress client-local; KD-6 bounds "reproducible" honestly to *behavioural* certification against the existing digests rather than bit-identity, deferring the latter to Stage 5+ with #52. |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 0H + 2M, both resolved.** **M-1** — KD-1's conflict matrix was unimplementable as written: every row keys on a `SaveClass`, but `Classify` reads version fields **inside** the file, and a Cloud conflict is first observed as *metadata*. Without saying so, the natural implementation resolves on timestamp/size — the "newer wins" heuristic the table exists to reject, and the one that overwrites a `TooNew` save with an older one. Pinned: fetch to a staging path, classify the bytes, and **refuse** rather than guess if only metadata is available; §9 gains the negative lock (an older-but-newer-timestamped copy). **M-2** — KD-3 made a local file the owner of achievement unlock state, but for a Steam achievement **the platform is the store of record**; a second owner yields double-grants or unlocks lost offline. Recast as a **pending-unlock queue + counter store** that reconciles from the platform (platform wins on unlock disagreement), and §5's shape updated to match — `CloudSyncState` also stopped caching remote versions, since a cached version is a stale decision input that reopens M-1. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 0H + 1M, resolved.** **M-1** — the KD-2 evidence set claimed to gate *the artifact*, but five of its six rows (KAT, perf baseline, EditMode/PlayMode, `dotnet-ci`) execute against **the project's assemblies under a test runner**, never inside a shipped player. Left as written, the gate would repeat one level up the exact error §2(b) catches it making: asserting a property that was never measured on the thing being claimed about. Split explicitly into **input-side evidence** (binds to a commit — which promotes R-1's commit-matching from hygiene to a gate rule) and **artifact-side evidence** (a packaged-build smoke path: launch → career → save → relaunch → load → advance), the only class that can catch a packaging-only failure every project-side test passes through. New R-1a records that the smoke path's coverage is a genuine decision. |
| v0.4 | July 26, 2026 | **AR-3 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle). The sweep specifically re-read every reproducibility/evidence claim for AR-2 ripple, on the #50 precedent that one structural fix leaves stale statements elsewhere. **L-1** — KD-6 still said a *build* "reproduces the certified digests" without noting that this too is measured **project-side**, so it certifies the build's inputs and reaches the artifact only via the commit binding plus the smoke path; stating it the other way round would re-import the error the supplement opens by naming. **L-2** — §7's surface table omitted the packaged-build smoke path introduced in v0.3, leaving the one artifact-side check absent from the list an implementer reads first. |
