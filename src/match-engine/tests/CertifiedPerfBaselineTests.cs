// File:     src/match-engine/tests/CertifiedPerfBaselineTests.cs
// Created:  2026-06-28
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.4.4 / §4.3.2 / FR-PO-031 / FR-PO-052,
//           certification-platform.md (Stage 0 host pin), Testing Strategy & Framework #19,
//           Code Standards #20
// Purpose:  Locks the FR-PO-052 certified perf baseline for the match-engine kickoff scenario.
//           At Stage 0 the corpus entry is PENDING (no run on the pinned certification platform —
//           the Linux gate is NON-certifying), so it must carry no measured metric and refuse to
//           build a corpus record. Also proves the certified projection: once a real run on the
//           pinned platform supplies metrics, the entry builds a complete BaselineRecord that flows
//           through PerfGateRunner.

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

        // Illustrative certified number used ONLY to exercise the certified projection path in-test.
        // This is NOT a real certification measurement (the pinned platform is Windows/Unity, not
        // reachable from the Linux gate) — the on-disk corpus entry stays PENDING.
        private const float SampleCertP50Ms = 4.0f;
        private const float SampleCertP99Ms = 6.0f;

        private static CertifiedPerfBaseline KickoffPending()
        {
            return CertifiedPerfBaseline.Pending(
                MatchEngineCapstoneScenarios.KickoffMultiSecondPath,
                LoopTag.PhysicsSixtyHz,
                CertifiedPerfBaseline.Stage0CertPlatformPin,
                Threshold);
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

        // ── Pending (Stage 0) ─────────────────────────────────────────────────────────────

        [Test]
        public void KickoffBaseline_IsPending_CarriesNoMetric()
        {
            CertifiedPerfBaseline cb = KickoffPending();

            Assert.AreEqual(CertificationStatus.Pending, cb.Status);
            Assert.AreEqual(MatchEngineCapstoneScenarios.KickoffMultiSecondPath, cb.ScenarioManifestId);
            Assert.AreEqual(LoopTag.PhysicsSixtyHz, cb.Loop);
            Assert.AreEqual(CertifiedPerfBaseline.Stage0CertPlatformPin, cb.PlatformPin);
            Assert.AreEqual(Threshold, cb.ThresholdCited);
            Assert.IsNull(cb.Manifest, "A pending entry must not carry a session manifest.");
            Assert.IsNaN(cb.CertifiedP50Ms, "A pending entry must not carry a measured p50.");
            Assert.IsNaN(cb.CertifiedP99Ms, "A pending entry must not carry a measured p99.");
        }

        [Test]
        public void PendingBaseline_RefusesToBuildRecord()
        {
            CertifiedPerfBaseline cb = KickoffPending();

            bool built = cb.TryBuildBaselineRecord(out BaselineRecord record);

            Assert.IsFalse(built, "A pending baseline must not project to a corpus record (no measured metric).");
            Assert.IsNull(record);
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
                "win11-unity2022.3.62f1-mono-x64-sse4.2-1w-detflags",
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
#endregion
