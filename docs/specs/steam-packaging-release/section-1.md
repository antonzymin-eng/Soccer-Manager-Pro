# Steam Packaging & Release Engineering #39 — Section 1: Scope, Dependencies, Key Decisions

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 1.1 Purpose

**#39 is the spec that decides whether a build ships.** It owns the build/packaging pipeline definition,
the **release gate** (what must be true, with evidence, before an artifact ships), the **Steam Cloud sync
policy** over the existing save file, the **achievement** model as a read-only derivation, and the
**store/compliance asset checklist**.

It is authored last because it is downstream of everything, and it is process-and-contract rather than
simulation code: it parses no save, compares no version, defines no determinism proof, and mutates no
gameplay.

## 1.2 In scope / out of scope

**In scope**

- The **pipeline** definition — from a commit to a packaged artifact.
- The **release gate**: the evidence set, its fail-closed posture, and the runbook that produces it.
- The **Cloud sync policy**: what syncs, when, and what happens on conflict.
- The **achievement** model: identities, predicates, and where they are evaluated.
- The **checklist**, split into what blocks a release and what does not.

**Out of scope**

| Not owned | Owner | How #39 relates |
|---|---|---|
| The save **format** and its version constants | #30 (frame) + each sub-blob's owning spec | #39 **transports** the file; it parses nothing and bumps nothing (KD-5) |
| **Version classification, migration, refusal** | **#50** | #39 asks #50 and obeys; it **never compares versions itself** (KD-1) |
| The **certification machinery** — determinism KAT, perf baseline, the platform pin | #16 / #18 / `certification-platform.md` | #39 **consumes its evidence**; it defines no new determinism proof (KD-2) |
| **User-facing text** for conflict and refusal dialogs | **#49** | #39 emits an identity + slots; #49 renders (KD-1) |
| Any **sim or gameplay behaviour** | every other spec | #39 is downstream of all of it and mutates none of it (KD-7) |
| **Store copy, art, trailers** as *content* | marketing / art production | #39 specifies the **checklist and its gate class**, never the assets (KD-4) |

## 1.3 Dependencies

| Spec | Relationship |
|---|---|
| **#50** Save Migration & Versioning | Owns `Classify` and `CompareForConflict`. **The dependency runs `#50 → #39`, and #50 already names #39 as its consumer** (KD-1). |
| **#30** Season & Competition Loop | Owns the save frame #39 transports. #39 parses none of it. |
| **#16 / #18** + `certification-platform.md` | Supply the certified evidence the gate consumes (KD-2). |
| **#49** Localization & Accessibility | Renders conflict and refusal notices. #39 is an ordinary producer. |
| **#38** UI / Client Framework | Proposed owner of the client-local settings store, where #39's two client-local stores live. |
| **#52** (Stage 5+ netcode) | The tier at which bit-identical packaging becomes materially valuable (KD-6). |
| the composition root / client shell | **Evaluates achievement predicates** and binds the Steam API. This is what keeps #39 out of the simulation's dependency graph (KD-3). |

## 1.4 What already exists (verified, not assumed)

**(a) There is no build pipeline of any kind.** `.github/workflows/ci.yml` defines eleven jobs —
`markdown-lint`, `yaml-lint`, `link-check`, `spec-hygiene`, `file-manifest-check`, `csharp-format`,
`dotnet-compile-test`, `unity-meta-integrity`, `unity-asset-hygiene`, `unity-license-check`,
`unity-tests` — and a tree-wide search for `BuildPlayer` or any player-build invocation returns
**nothing**.

**Consequence:** #39 does not harden an existing pipeline; it **introduces the first one**. That is
scope-defining — §3 and §4 describe a pipeline from zero, and the reproducibility claims #39 makes are
claims about a process that does not yet exist and can therefore still be **designed to be checkable**.

**(b) The Unity half of CI is skip-open — it reports success while running nothing.** `unity-tests` is
declared `needs: unity-license-check` with `if: needs.unity-license-check.outputs.configured == 'true'`,
and `unity-license-check` emits `configured=false` plus a **notice, not a failure**, when `UNITY_LICENSE`
is unset. The job's own comment says it is *"cleanly SKIPPED (not failed) until then."*

**That is the correct choice for CI and the exact wrong choice for a ship gate, and the two must not
share a posture.** A skipped job is indistinguishable from a passed one in a summary status, so *"CI is
green"* carries **no information** about whether EditMode or PlayMode tests ran at all.

**Consequence:** KD-2's gate is **evidence-positive** — it requires artifacts asserting that named checks
*executed on a named commit*, and treats their absence as failure. *"Nothing was red"* is not an input.

**(c) The certification machinery is real, complete, and host-bound.** `certification-platform.md` v1.4 is
**✅ PINNED** on Windows 11 / Unity 6000.4.9f1 / DX11 / Mono / x64 / SSE4.2 / 1 worker /
DAZ·FTZ·fp-contract·FMA all off. Two certified runs exist against it: the determinism KAT
(44 passed / 0 failed / 4 deferred skips) and the FR-PO-052 per-tick perf baseline
(p50 = 0.4768 ms, p99 = 2.5669 ms, promoted PENDING → CERTIFIED). `cert-run-runbook.md` is the operator
checklist, and it states in its own words that the Linux `tools/dotnet-ci` gate is **explicitly
NON-certifying** — *"a number sourced from it would be a fabricated certification."*

**Consequence:** #39 **reuses** this rather than re-inventing it (KD-2), and inherits its constraint: the
certifying half of the release gate **cannot run in CI**. The gate is therefore a **runbook executed on
the pinned host** producing a committed evidence record, with CI supplying necessary-but-insufficient
signal. **This is a limitation to state plainly, not to engineer around.**

**(d) The save is one atomic local file with a version-first frame and opaque sub-blobs.**
`SeasonSaveCodec` writes `SEASON_SAVE_FORMAT_VERSION` first, then a `matchPresent` flag byte, then three
length-prefixed sub-blobs — the living-world composite (always), the season state (always), the match save
(only mid-match) — and it *"never parses"* a sub-blob. Writers use `temp → fsync → rename`.

**Consequence:** Cloud sync can be **whole-file only** (KD-5). There is no meaningful per-blob sync, and
attempting one would require #39 to parse a frame it has no business parsing. It also means the mid-match
blob is **not separable** from the file: a mid-match save syncs or it does not, as one unit.

**(e) #50 already owns every version judgement #39 needs, and named #39 as its consumer.** #50 defines
`Classify(in header) → SaveClass` over `{ Current, Migratable, TooNew, Unsupported, Corrupt }`, and lists
`CompareForConflict(a, b)` with direction **`#50 → #39`**. Its KD-5 already pins two rules that bind #39:

> *"a save that is `TooNew` for this build **cannot be resolved by this build at all** … a migrated save is
> **written back only on the player's next explicit save**, never silently on load."*

It also introduces `WORLD_GENERATION_VERSION` because rosters are **regenerated, not stored**, so a save
is only reproducible against the generator code that wrote it.

**Consequence:** #39's conflict rule is a **thin policy over #50's classification**, and the coupling the
plan worried about resolves **by ownership rather than negotiation** — #39 never compares a version number
itself, and **§8 files no back-prop**. The generation version matters here more than anywhere else,
because **Cloud is precisely the mechanism that carries a save between two machines running two different
builds** (R-4).

## 1.5 Key decisions

### KD-1 — Cloud conflict resolution is a **policy over #50's classification**, never a second opinion

#39 asks #50 to classify each candidate save and compare the pair, then applies a short policy. **Every
clause exists to prevent a specific way of losing a career:**

| Situation | Rule | Why |
|---|---|---|
| Either side is `TooNew` | **This build resolves nothing** — surface the conflict, touch neither copy | Per #50 KD-5. A build cannot reason about a format it does not know, and *"newer wins"* is how the newer copy gets overwritten by a build that could not read it |
| Both `Current`, contents differ | **Player chooses**; **no automatic merge, ever** | Two divergent careers are not mergeable — a save is one causal history, and picking silently discards the other |
| Local `Migratable`, remote `Current` | Resolve as a normal conflict on the **pre-migration** versions | Migration is not a tiebreaker; treating a migrated copy as "newer" would let opening a career on machine B rewrite machine A's cloud copy |
| Any side `Corrupt` / `Unsupported` | **Never auto-select it, never delete it** | #50 KD-4's non-destructive refusal extends to Cloud: a refused save is left exactly as found |

**Every row requires the bytes, so conflict resolution runs only after both copies are local.** `Classify`
reads version fields **inside** the file (#50 KD-1); it cannot be evaluated against Cloud metadata. #39
therefore fetches the remote copy to a staging path and classifies it there, and **resolving on timestamp
or size alone is forbidden** — that is precisely the *"newer wins"* heuristic every row of the table
exists to reject. A metadata-only decision would happily overwrite a `TooNew` save with an older one and
call it a sync.

**A migrated save is never uploaded on load** — only on the player's next explicit save (#50 KD-5,
inherited verbatim). The failure this prevents is concrete: open the career on the new build, let it sync,
return to the older build on the other machine, and find the save now refuses to load. **A read must never
become a write.**

Conflict text routes through **#49** as an identity + slots, like every other producer; #39 bakes no
strings.

### KD-2 — The release gate is **fail-closed on positive evidence**, and it is not CI

This is the decision §1.4(b) forces. The gate is a set of **evidence artifacts**, each naming the commit
it describes, each of which must be **present and affirmative** (Appendix B).

**The inversion is the whole point.** CI answers *"did anything break?"* and is right to skip what it
cannot run. The gate answers *"is this specific artifact proven shippable?"*, where an **unanswered
question is a no**. The same `unity-tests` job feeds both, read with **opposite defaults** — and the gate
reads its **artifact**, not its status.

**Five of the six evidence rows verify the *project at a commit*, not the *packaged binary*, and the spec
says so.** The KAT, the perf baseline, the Unity Test Runner and `dotnet-ci` all execute against the
project's assemblies under a test runner; **none runs inside a shipped player**. Treating them as artifact
evidence would repeat, one level up, the mistake §1.4(b) catches: claiming a property that was never
measured on the thing being claimed about. So the gate is explicitly two-part:

- **Input-side evidence** (five rows): binds to a **commit**, and is admissible **only** if that commit is
  the one the artifact was built from — which makes commit-matching a **gate rule** rather than hygiene
  (R-1).
- **Artifact-side evidence**: a **packaged-build smoke path** — launch the shipped player, create a
  career, save, quit, relaunch, load, advance a day. Small on purpose, because it is the **only** class of
  check that can catch a packaging-only failure (a stripped assembly, a missing asset, a broken path) that
  every project-side test passes straight through.

**The gate does not run in CI** (§1.4(c)): its certifying inputs are pinned-host-only, and sourcing them
from Linux would be a fabricated certification in the runbook's own words. #39 therefore specifies a
**release runbook** — the direct descendant of `cert-run-runbook.md`, which already proves the pattern
works — whose evidence records are committed artifacts, reviewable after the fact.

### KD-3 — Achievements are read-only derivations, persisted **outside** every sim save, and never read back

Three properties, in increasing importance:

1. **Derived, not authored into the sim.** An achievement is a predicate over events the career already
   emits — the read-only posture of #37 analytics and #44 discipline, and the aggregation shape #46's
   inbox already defines. **#39 adds no event, no hook and no sim field.**
2. **The store of record is the platform; the local store is an offline queue, not a second truth.** Steam
   owns unlock state for a Steam achievement. A local file that also *owns* it produces the classic
   double-write (unlock granted twice) or lost unlock (granted offline, never flushed). So the local store
   holds **pending unlocks awaiting flush** plus the running counters a predicate needs between sessions,
   and reconciles **from** the platform on connect — **platform wins** on any disagreement about whether
   something is unlocked. It is **never** a save sub-blob: putting it in the season save would make the
   save's byte content depend on which achievements a *player account* had earned, and would enlist #50 in
   migrating trophy state.
3. **Nothing in the sim may read achievement state.** This is the one that turns a cosmetic feature into a
   **determinism defect** if violated: a sim branching on *"has this player unlocked X"* would make replay
   depend on account state that is not in the save. Stated as a prohibition, and tested as one.

**Achievement evaluation lives in the client shell, not in #39's assembly** — the `ICueSink` inversion #48
already uses. #39 defines the predicate contract and the identity set; the shell wires it to the event
surface. That is what keeps #39 out of the simulation's dependency graph (§4.1) despite consuming career
events.

### KD-4 — The checklist has **two gate classes**, and only one can block

- **Compliance / hard gate** (blocks the release): the age-rating declaration; the third-party licence and
  attribution manifest; EULA and privacy text present and localized through #49; the Cloud configuration
  matching KD-5's file set; the crash/exception reporting path; and the KD-2 evidence set.
- **Marketing / soft** (does not block): capsule art, trailer, screenshots, store copy, tags.

**The split exists because a checklist without one becomes either theatre (nothing blocks) or a hostage to
a missing screenshot (everything blocks).** #39 specifies the checklist and its gate class; it does **not**
specify the assets — the same boundary #48 drew for animation and #51 for audio content.

### KD-5 — Cloud syncs **whole files at quiescent boundaries**; the running game owns the file

Per §1.4(d) the save is one atomic file, so:

- **Whole-file sync only.** No per-sub-blob sync; #39 parses nothing.
- **The running game owns its save file.** Sync is permitted when no write is in flight — the
  `temp → fsync → rename` discipline already guarantees the cloud never observes a partial file — and the
  policy adds that **the game must not load a file that sync is replacing**.
- **A mid-match save syncs as a unit or not at all.** The `matchPresent` blob rides inside the file; there
  is no coherent state in which the world syncs and the match does not, and #39 must not create one.
- **Sync on quiescence** — save completion and clean exit — rather than continuously mid-session. A
  session that ends by crash falls back to the last completed save, which is exactly what the local
  behaviour already is.

*Rejected:* **sync continuously so the cloud is always current.** It maximises the window in which two
machines hold divergent in-progress careers — the state KD-1 says cannot be merged. **Reducing conflict
frequency is worth more than reducing conflict staleness.**

### KD-6 — "Reproducible build" means **behaviourally certified**, not bit-identical

The honest claim, stated because the tempting one is unverifiable: a Unity/Mono player build embeds
timestamps and non-deterministic ordering that make byte-identical rebuilds a project in themselves, and
**#39 must not gate on a property nobody has demonstrated here.**

What #39 **does** require is stronger where it matters and checkable today: **the pinned source commit,
built on the pinned tuple, reproduces the certified digests** — the determinism KAT's golden vectors and
digest chains (§1.4(c)). That is *behavioural* reproducibility, it is what a save, a replay and a future
netcode peer actually depend on, and the machinery already exists.

**Scope of that claim, per KD-2's two-part split:** it is measured **project-side**, so it certifies the
**inputs** the artifact is built from, not the player binary itself. The artifact inherits it only through
the commit binding (R-1) plus the smoke path. Stating it the other way round would re-import the exact
error this spec opens by naming.

Bit-identical packaging is a **Stage 5+ deferral** — materially more valuable when #52's peers must agree
— not a Stage-2 gate.

### KD-7 — Determinism posture and identity

Infrastructure: **no RNG stream, no domain tag, no `SubsystemOrdinal`**; #16 has no row for #39 and needs
none — the read-only / presentation / infra class (#37, #44, #46, #47, #48, #50). #39 draws nothing, ticks
nothing, and serializes nothing into a sim save.

**Identity:** with **Cloud disabled and no achievements defined**, the shipped game's save behaviour is
byte-for-byte today's local path, and the gate reduces to the existing runbook. Every addition is additive
over that.

## 1.6 Staging

| Tier | Content | Behaviour |
|---|---|---|
| **Minimal (the identity)** | A reproducible build of the current tree; **Cloud disabled**; the gate run manually from the runbook against a committed evidence record; a small achievement set evaluated read-only; the compliance subset of the checklist | **Today's save path exactly** — no new writer, no new failure mode, and every existing save test still describes the shipped behaviour |
| **Deep** | Cloud enabled with KD-5's quiescence rule + #50-delegated conflict resolution; a fuller achievement set; multi-branch/beta packaging; the gate automated as far as the host constraint permits | Cloud-synced |

**The minimal tier is genuinely shippable**, and *"Cloud off"* is the identity in the strict project
sense. That matters more here than in most specs: the whole of KD-1 and KD-5 exists to manage a hazard
that **does not exist until Cloud is enabled**, so a first release can ship without ever taking it on.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §1 from supplement v0.4 (scope with the assets/content boundary stated up front; the five verified facts, with (b) — CI is **skip-open**, so a green pipeline is compatible with nothing having been built or tested — as the fact that forces the spec's central inversion, and (e) recording that #50 already names #39 as its consumer, which is why §8 files no back-prop; KD-1..KD-7, including KD-2's explicit input-side/artifact-side split and KD-6's honest bound on "reproducible"; the two-tier staging whose minimal identity is genuinely shippable because the Cloud hazard does not exist until Cloud is enabled). Status IN REVIEW. |
#endregion
