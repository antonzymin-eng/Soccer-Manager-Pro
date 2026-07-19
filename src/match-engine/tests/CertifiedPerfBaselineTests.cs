// File:     src/match-engine/tests/CertifiedPerfBaselineTests.cs
// Created:  2026-06-28
// Modified: 2026-07-19
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.4.4 / §4.3.2 / FR-PO-031 / FR-PO-052,
//           certification-platform.md (Stage 0 host pin), Testing Strategy & Framework #19,
//           Deterministic Simulation #16 §4.8 (EnvironmentFingerprint / ERR-016-006), Code Standards #20
// Purpose:  Locks the FR-PO-052 CERTIFIED perf baseline for the match-engine kickoff scenario. The
//           corpus entry (KickoffCertified()) carries the pinned-host 100-run capture manifest with a
//           genuine CreateStage0MonoCertified fingerprint + the measured p50/p99, and builds a complete
//           BaselineRecord that flows through PerfGateRunner. Also exercises the generic Pending() /
//           Certified() API surface + fail-closed invariants + platform-pin tokens.

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PerformanceOptimization;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Tests for the <see cref="CertifiedPerfBaseline"/> corpus entry of the match-engine kickoff
    /// scenario (<c>match-engine-kickoff-multi-second</c>), the FR-PO-052 per-tick certified baseline.
    /// </summary>
    [TestFixture]
    public sealed class CertifiedPerfBaselineTests
    {
        private const string Threshold = "FR-PO-052";

        // Illustrative certified number used ONLY to exercise the generic certified projection path
        // in-test (paired with CertManifest() below, which uses the dev fingerprint).
        private const float SampleCertP50Ms = 4.0f;
        private const float SampleCertP99Ms = 6.0f;

        // ── The real certified kickoff baseline ───────────────────────────────────────────
        // Transcribed from the certified 100-run capture on the pinned Windows 11 / Unity 6000.4.9f1
        // / Mono host (2026-07-19); see
        // docs/specs/performance-optimization/baselines/match-engine/kickoff-multi-second.cert.md.
        private const float KickoffCertP50Ms = 0.4768f;
        private const float KickoffCertP99Ms = 2.5669f;
        private const string KickoffGitSha = "224ee7eb8d2647ab1e75a5e69cafee534dbdca8b";
        // Host-supplied Mono runtime version (§4.8.3 field 2). Unity versions its forked Mono runtime
        // by editor release, so this identifies "the Mono bundled with Unity 6000.4.9f1" — ERR-016-006
        // Option A. The resulting FloatModelHash is golden-vector-validated in the .cert.md.
        private const string KickoffMonoRuntimeVersion = "mono-bundled-unity6000.4.9f1";
        private const string KickoffFloatModelHash =
            "73c47ad54d3a81408b46694b513634fd244f25262aa4104614712134b6bb756a";

        /// <summary>
        /// The real CERTIFIED kickoff baseline: the pinned-host capture manifest (with a genuine
        /// <see cref="EnvironmentFingerprint.CreateStage0MonoCertified"/> fingerprint) + the measured
        /// p50/p99. Matches the on-disk `kickoff-multi-second.cert.md` corpus entry.
        /// </summary>
        private static CertifiedPerfBaseline KickoffCertified()
        {
            var manifest = new SessionManifest(
                gitSha: KickoffGitSha,
                seed: MatchEngineCapstoneScenarios.KickoffMultiSecondSeed,
                environmentFingerprint:
                    EnvironmentFingerprint.CreateStage0MonoCertified(KickoffMonoRuntimeVersion),
                platformPin: CertifiedPerfBaseline.Stage0CertPlatformPin,
                scenarioManifestId: MatchEngineCapstoneScenarios.KickoffMultiSecondPath,
                sessionStartUtc: "2026-07-19T00:11:19Z",
                sessionEndUtc: "2026-07-19T00:12:04Z",
                hardwareCounters: new HardwareCounterSnapshot(
                    "Intel(R) Core(TM) i5-9300H CPU @ 2.40GHz",
                    4,
                    "idle, no sustained load prior to run (no monitoring tool used)"),
                harnessVersion: "1.0");

            return CertifiedPerfBaseline.Certified(
                manifest, LoopTag.PhysicsSixtyHz, KickoffCertP50Ms, KickoffCertP99Ms, Threshold);
        }

        private static SessionManifest CertManifest()
        {
            return new SessionManifest(
                gitSha: "0000000000000000000000000000000000000000",
                seed: MatchEngineCapstoneScenarios.KickoffMultiSecondSeed,
                environmentFingerprint: EnvironmentFingerprint.CreateStage0Dev(),
                platformPin: CertifiedPerfBaseline.Stage0CertPlatformPin,
                scenarioManifestId: MatchEngineCapstoneScenarios.KickoffMultiSecondPath,
                sessionStartUtc: "2026-06-28T00:00:00Z",
                sessionEndUtc: "2026-06-28T00:00:10Z",
                hardwareCounters: new HardwareCounterSnapshot("cert-host-cpu", 8, "nominal"),
                harnessVersion: "cert-baseline-1.0");
        }

        // ── Certified kickoff baseline (matches the on-disk .cert.md) ──────────────────────

        [Test]
        public void KickoffBaseline_IsCertified_CarriesRealMetric()
        {
            CertifiedPerfBaseline cb = KickoffCertified();

            Assert.AreEqual(CertificationStatus.Certified, cb.Status);
            Assert.AreEqual(MatchEngineCapstoneScenarios.KickoffMultiSecondPath, cb.ScenarioManifestId);
            Assert.AreEqual(LoopTag.PhysicsSixtyHz, cb.Loop);
            Assert.AreEqual(CertifiedPerfBaseline.Stage0CertPlatformPin, cb.PlatformPin);
            Assert.AreEqual(Threshold, cb.ThresholdCited);
            Assert.AreEqual(KickoffCertP50Ms, cb.CertifiedP50Ms);
            Assert.AreEqual(KickoffCertP99Ms, cb.CertifiedP99Ms);
            Assert.IsNotNull(cb.Manifest, "A certified entry must carry its session manifest.");
            Assert.IsTrue(cb.Manifest.IsComplete(), "The certified capture manifest must be complete.");
        }

        [Test]
        public void KickoffBaseline_CarriesRealMonoFloatModelHash_NotPlaceholder()
        {
            // ERR-016-006 Option A: the certified fingerprint carries a genuine §4.8.3 hash, not the
            // CreateStage0Dev placeholder — this is the datum that gated promotion to CERTIFIED.
            CertifiedPerfBaseline cb = KickoffCertified();

            EnvironmentFingerprint fp = cb.Manifest.EnvironmentFingerprint;
            Assert.IsFalse(fp.IsDevPlaceholder,
                "The certified baseline must not carry a placeholder floatModelHash.");
            Assert.AreEqual(KickoffFloatModelHash, fp.FloatModelHash,
                "floatModelHash must match the golden-vector-validated value in the .cert.md.");
            Assert.AreEqual("SSE4.2", fp.SimdFeatureLevel);
        }

        [Test]
        public void CertifiedKickoffBaseline_BuildsRecord()
        {
            CertifiedPerfBaseline cb = KickoffCertified();

            bool built = cb.TryBuildBaselineRecord(out BaselineRecord record);

            Assert.IsTrue(built, "A certified baseline must project to a corpus record.");
            Assert.IsNotNull(record);
            Assert.AreEqual(KickoffCertP50Ms, record.P50Ms);
            Assert.AreEqual(KickoffCertP99Ms, record.P99Ms);
        }

        [Test]
        public void CertifiedKickoffRecord_FlowsThroughPerfGate_SelfComparePasses()
        {
            // FR-PO-031 self-compare: the certified baseline vs itself ⇒ 0% delta ⇒ within +5%.
            // The real live-measurement comparison runs on the pinned host (a Linux measurement vs a
            // Windows-certified number is apples-to-oranges), so this proves the certified record is a
            // usable FR-PO-031 reference without asserting a cross-platform number.
            CertifiedPerfBaseline cb = KickoffCertified();
            Assert.IsTrue(cb.TryBuildBaselineRecord(out BaselineRecord record));

            PerfGateReport report = PerfGateRunner.Run(
                specId: 16,
                loopTag: PerformanceOptimizationConstants.LOOP_TAG_PHYSICS_60HZ,
                baseline: record,
                current: record,
                milestoneMs: float.NaN);

            Assert.AreEqual(
                MatchEngineCapstoneScenarios.KickoffMultiSecondPath, report.ScenarioManifestId);
            Assert.IsTrue(report.AllPassed, "Certified kickoff baseline self-comparison must pass the gate.");
        }

        // ── Certified projection ──────────────────────────────────────────────────────────

        [Test]
        public void CertifiedBaseline_BuildsCompleteRecord()
        {
            CertifiedPerfBaseline cb = CertifiedPerfBaseline.Certified(
                CertManifest(), LoopTag.PhysicsSixtyHz, SampleCertP50Ms, SampleCertP99Ms, Threshold);

            Assert.AreEqual(CertificationStatus.Certified, cb.Status);

            bool built = cb.TryBuildBaselineRecord(out BaselineRecord record);

            Assert.IsTrue(built);
            Assert.IsNotNull(record);
            Assert.AreEqual(SampleCertP50Ms, record.P50Ms);
            Assert.AreEqual(LoopTag.PhysicsSixtyHz, record.Loop);
            Assert.AreEqual(CertifiedPerfBaseline.Stage0CertPlatformPin, record.Manifest.PlatformPin);
            Assert.IsTrue(record.Manifest.IsComplete(), "Certified record manifest must be complete.");
        }

        [Test]
        public void CertifiedRecord_FlowsThroughPerfGate_SelfComparePasses()
        {
            CertifiedPerfBaseline cb = CertifiedPerfBaseline.Certified(
                CertManifest(), LoopTag.PhysicsSixtyHz, SampleCertP50Ms, SampleCertP99Ms, Threshold);
            Assert.IsTrue(cb.TryBuildBaselineRecord(out BaselineRecord record));

            // Self-comparison: identical baseline + current ⇒ 0% delta ⇒ within the +5% gate.
            PerfGateReport report = PerfGateRunner.Run(
                specId: 16,
                loopTag: PerformanceOptimizationConstants.LOOP_TAG_PHYSICS_60HZ,
                baseline: record,
                current: record,
                milestoneMs: float.NaN);

            Assert.AreEqual(
                MatchEngineCapstoneScenarios.KickoffMultiSecondPath, report.ScenarioManifestId);
            Assert.IsTrue(report.AllPassed, "Certified baseline self-comparison must pass the gate.");
        }

        // ── Fail-closed invariants ──────────────────────────────────────────────────────

        [Test]
        public void Certified_RejectsDegenerateMetrics()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CertifiedPerfBaseline.Certified(
                CertManifest(), LoopTag.PhysicsSixtyHz, 0f, SampleCertP99Ms, Threshold),
                "Non-positive p50 must fail closed.");

            Assert.Throws<ArgumentOutOfRangeException>(() => CertifiedPerfBaseline.Certified(
                CertManifest(), LoopTag.PhysicsSixtyHz, float.NaN, SampleCertP99Ms, Threshold),
                "Non-finite p50 must fail closed.");

            Assert.Throws<ArgumentOutOfRangeException>(() => CertifiedPerfBaseline.Certified(
                CertManifest(), LoopTag.PhysicsSixtyHz, 8f, 6f, Threshold),
                "p99 < p50 must fail closed.");
        }

        [Test]
        public void Certified_RejectsIncompleteManifest()
        {
            var incomplete = new SessionManifest(
                gitSha: "",                                   // empty ⇒ IsComplete() == false
                seed: MatchEngineCapstoneScenarios.KickoffMultiSecondSeed,
                environmentFingerprint: EnvironmentFingerprint.CreateStage0Dev(),
                platformPin: CertifiedPerfBaseline.Stage0CertPlatformPin,
                scenarioManifestId: MatchEngineCapstoneScenarios.KickoffMultiSecondPath,
                sessionStartUtc: "2026-06-28T00:00:00Z",
                sessionEndUtc: "2026-06-28T00:00:10Z",
                hardwareCounters: new HardwareCounterSnapshot("cert-host-cpu", 8, "nominal"),
                harnessVersion: "cert-baseline-1.0");

            Assert.Throws<ArgumentException>(() => CertifiedPerfBaseline.Certified(
                incomplete, LoopTag.PhysicsSixtyHz, SampleCertP50Ms, SampleCertP99Ms, Threshold));
        }

        [Test]
        public void Pending_RejectsEmptyArguments()
        {
            Assert.Throws<ArgumentException>(() => CertifiedPerfBaseline.Pending(
                "", LoopTag.PhysicsSixtyHz, CertifiedPerfBaseline.Stage0CertPlatformPin, Threshold));

            Assert.Throws<ArgumentException>(() => CertifiedPerfBaseline.Pending(
                MatchEngineCapstoneScenarios.KickoffMultiSecondPath, LoopTag.PhysicsSixtyHz, "", Threshold));
        }

        [Test]
        public void PlatformPinTokens_MatchDocumentedTuple()
        {
            // Guards against an accidental edit silently decoupling the token from
            // certification-platform.md / the capstone non-cert anchor.
            Assert.AreEqual(
                "win11-unity6000.4.9f1-dx11-mono-x64-sse4.2-1w-detflags",
                CertifiedPerfBaseline.Stage0CertPlatformPin);
            Assert.AreEqual(
                "linux-dotnet-noncert",
                CertifiedPerfBaseline.LinuxNonCertPlatformPin);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-28 | —      | Initial implementation. Locks the kickoff certified baseline as    |
// |         |            |        | PENDING (no metric, refuses to build) and proves the certified     |
// |         |            |        | projection + fail-closed invariants + platform-pin tokens.         |
// | 1.1     | 2026-07-19 | —      | Kickoff corpus entry promoted PENDING -> CERTIFIED (ERR-016-006     |
// |         |            |        | Option A resolved the floatModelHash gap). KickoffPending() ->      |
// |         |            |        | KickoffCertified() (real pinned-host manifest via                   |
// |         |            |        | CreateStage0MonoCertified + p50=0.4768/p99=2.5669). New tests:      |
// |         |            |        | IsCertified_CarriesRealMetric, CarriesRealMonoFloatModelHash_Not-   |
// |         |            |        | Placeholder, BuildsRecord, SelfComparePasses. Generic Pending()/    |
// |         |            |        | Certified() API tests (incl. Pending_RejectsEmptyArguments) kept.   |
#endregion
