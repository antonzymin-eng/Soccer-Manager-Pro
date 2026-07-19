# Platform Determinism Certification Run — 2026-07-19

**Status:** ✅ **PASSED.** Stage-0 platform-determinism KAT run executed on the pinned
Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host. All three golden-vector corpora
(#16 §9.5 #4 a/b/c) and the §5 determinism-tier locks pass byte-exact; zero failures.
Certifies `docs/tracking/certification-platform.md` → ✅ PINNED and closes
`FR-DS-009-GATE` (#16 §5.5).
**Spec:** Deterministic Simulation #16 §5 / §5.5 (FR-DS-009-GATE) / §9.5 #4;
Testing Strategy #19 §7.5 (D1 test-runner pin) / FR-TS-016.
**Evidence:** `determinism-results-2026-07-19.xml` (NUnit3, this directory) — the raw
Unity Test Framework EditMode run output, committed verbatim.

This is the platform-determinism half of the Stage-0 certification, distinct from the
FR-PO-052 per-tick perf baseline (certified 2026-07-19,
`docs/specs/performance-optimization/baselines/match-engine/kickoff-multi-second.cert.md`).
The perf run measures speed; this run proves the bits are exact on the pinned tuple.

## Host tuple (certification-platform.md v1.3 → v1.4)

| Field | Certified value |
|-------|-----------------|
| OS | Windows 11 (10.0.26200) |
| Unity | 6000.4.9f1 (rev f7258d6eebbe) |
| Graphics API | DX11 |
| Backend | Mono (Unity editor EditMode runtime) |
| CPU | x64 / SSE4.2 baseline (no AVX/AVX2/FMA) |
| Worker threads | 1 (single-threaded) |
| Float flags | DAZ off · FTZ off · fp-contract off · FMA off |

Step 0 host pre-flight confirmed unchanged from the 2026-07-19 FR-PO-052 perf capture,
which ran on this exact tuple.

## Run

| Field | Value |
|-------|-------|
| Commit certified | `819f9d1db0214177e135c642dfaa0f0289464c70` |
| Command | `Unity -batchmode -runTests -projectPath . -testPlatform EditMode -testFilter "TacticalDirector.DeterministicSim.Tests" -testResults .\determinism-results.xml -logFile -` |
| Runner | Unity Test Framework, NUnit engine 3.5.0.0, EditMode (the §7.5 D1 test-runner pin) |
| Start / end (UTC) | 2026-07-19 19:13:03Z → 19:13:04Z |
| Total | 48 |
| Passed | 44 |
| Failed | **0** |
| Skipped | 4 (Stage-0+1 deferrals — see below) |

## Golden-vector corpora — #16 §9.5 #4 (all byte-exact)

| Corpus | Fixture | Result |
|--------|---------|--------|
| (a) HKDF-SHA256 KAT | `HkdfSha256KatTests` (RFC 5869 A.1/A.2/A.3 + project case 4) | 4/4 ✅ |
| (b) SipHash-2-4-64 KAT | `SipHash24KatTests` (all 64 Appendix A vectors + project draw preimage) | 2/2 ✅ |
| (c) SerializeCanonical corpus | `SerializeCanonicalCorpusTests` (primitives / floats / strings / structs / arrays / phase-digest + RNG-draw preimages / snapshot digest chain D04–D07) | 9/9 ✅ |

The corpora assert against **pinned reference bytes**, so a green pass is itself the
reproducibility proof — the assertion target is fixed, not run-relative. The chained
`Corpus_SnapshotDigestChain_D04toD07` and `SnapshotCodec_Encode_ProducesSpecChainedDigest`
locks confirm the digest chain reproduces the pinned expected digests on this host.
Two-run bit-exactness is additionally covered by the capstone's two-run determinism
assertion, which ran on this same host on 2026-07-19 (perf certification).

## §5 determinism-tier locks

`DeterministicSimTests` 24/24 ✅ (MatchClock same-seed identical ticks, RNG branch-cursor
parity, canonical serialize bool/u32/u64/−0.0, divergence-detector NaN handling,
`EnvironmentFingerprint` worker-count mismatch → `ERR_DS_REPLAY_ENV_MISMATCH`) and
`DeterministicSimAdversarialRegressionTests` 4/4 ✅ (RNG skip parity, digest chain depends
on prev digest).

## Skips — not failures (Stage-0+1 deferrals)

All 4 skips are in `DeterministicSimSaveLoadTests`, each `[Ignore]`d with an explicit
Stage-0+1 reason (require a temp-directory fixture + file I/O in EditMode, or the
`SaveManager.SaveAtomicMidTick` overload that does not exist at Stage 0):
`SaveLoad_ConsecutiveSaves_...`, `SaveLoad_Encode_CommitAtomic_Load_...`,
`SaveLoad_MidTickSnapshot_...`, `SaveLoad_ValidateHeader_RejectsTamperedDigest`. They are
outside the Stage-0 determinism-certification surface by design; `ReplayEngine_PrepareReplay_...`
(the one non-file-I/O save/load case) ran and passed.

## EnvironmentFingerprint

The certified host fingerprint is `EnvironmentFingerprint.CreateStage0MonoCertified("mono-bundled-unity6000.4.9f1")`
(ERR-016-006 Option A), `floatModelHash = 73c47ad54d3a81408b46694b513634fd244f25262aa4104614712134b6bb756a`
— the same genuine fingerprint recorded for the 2026-07-19 perf capture. This is now the
known reference platform against which `ERR_DS_REPLAY_ENV_MISMATCH` is meaningful (#16 §4.8).

## Residual (NOT blocking this certification)

The §4.8.2 **runtime MXCSR validation** (query live float-mode flags at match start and
reject on mismatch) remains unbuilt. It is a defense-in-depth guard that *enforces* the now-
certified pin at replay time — it does not participate in *proving* the bits exact, which is
what this run does. With a certified pin now in place for it to enforce, it becomes buildable
(the KAT-first ordering); it also still awaits a snapshot-deserialize/replay consumer path in
the match engine. Tracked in the root `CLAUDE.md` OPEN ISSUES floatModelHash entry.

## Sign-off

Platform Certification owner sign-off (Deterministic Simulation #16 §1.7 Governance Artifacts)
is recorded via the PR merge that lands this certification and the `certification-platform.md`
v1.4 flip (solo-project governance; the file's own Maintenance Rule requires owner sign-off on
any PR that edits it).
