// File:     src/performance-optimization/SessionManifest.cs
// Created:  2026-06-01
// Modified: 2026-06-02
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.3.2, Appendix A, Code Standards #20
// Purpose:  Immutable session manifest capturing all §3.3.2 required fields.
//           Sessions missing any field are rejected by the §3.4.4 baseline validator.
//           Binds to EnvironmentFingerprint from Deterministic Simulation #16 §4.8.

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Immutable session manifest for a profiling session per §3.3.2.
    /// Every field is required; the §3.4.4 baseline validator rejects sessions
    /// with any null or empty field. Carried verbatim in <see cref="BaselineRecord"/>.
    /// Performance Optimization Strategy #18 §3.3.2 / Appendix A.
    /// </summary>
    public sealed class SessionManifest
    {
        /// <summary>
        /// 40-hex-character git SHA of the build under measurement.
        /// Required. Appendix A.
        /// </summary>
        public string GitSha { get; }

        /// <summary>
        /// Deterministic seed used during the profiling run per KD-6.
        /// At Stage 0 this is a manually chosen value per the Appendix E runbook.
        /// At Stage 0+1 the seed is obtained via <c>tools/select-seed.py</c>.
        /// Required. Appendix A.
        /// </summary>
        public ulong Seed { get; }

        /// <summary>
        /// Environment fingerprint snapshot per Deterministic Simulation #16 §4.8.
        /// Required. Appendix A / §3.3.6 (missing fingerprint is an FR-PO-017 violation).
        /// </summary>
        public EnvironmentFingerprint EnvironmentFingerprint { get; }

        /// <summary>
        /// Platform pin identifier from <c>docs/tracking/certification-platform.md</c> Stage 0 row.
        /// Required. Appendix A / KD-9.
        /// </summary>
        public string PlatformPin { get; }

        /// <summary>
        /// Scenario manifest ID referencing a Deterministic Simulation #16 §5 scenario verbatim.
        /// Required. Appendix A / §3.3.3.
        /// </summary>
        public string ScenarioManifestId { get; }

        /// <summary>
        /// RFC 3339 wall-clock timestamp of session start. Used for run bookkeeping only;
        /// never used for in-game state per §3.3.2 / FR-CS-042.
        /// Required. Appendix A.
        /// </summary>
        public string SessionStartUtc { get; }

        /// <summary>
        /// RFC 3339 wall-clock timestamp of session end.
        /// Required. Appendix A.
        /// </summary>
        public string SessionEndUtc { get; }

        /// <summary>
        /// Hardware performance counter snapshot captured at session start.
        /// Required. Appendix A.
        /// </summary>
        public HardwareCounterSnapshot HardwareCounters { get; }

        /// <summary>
        /// Semver version of the harness used to capture this session.
        /// Stage 0: <c>tools/perf-harness/</c> version.
        /// Stage 0+1: <c>tests/perf/</c> version.
        /// Required. Appendix A.
        /// </summary>
        public string HarnessVersion { get; }

        /// <summary>
        /// Initialises a session manifest with all §3.3.2 required fields.
        /// The §3.4.4 validator will reject any baseline record whose manifest has null/empty fields.
        /// </summary>
        public SessionManifest(
            string gitSha,
            ulong seed,
            EnvironmentFingerprint environmentFingerprint,
            string platformPin,
            string scenarioManifestId,
            string sessionStartUtc,
            string sessionEndUtc,
            HardwareCounterSnapshot hardwareCounters,
            string harnessVersion)
        {
            GitSha                 = gitSha;
            Seed                   = seed;
            EnvironmentFingerprint = environmentFingerprint;
            PlatformPin            = platformPin;
            ScenarioManifestId     = scenarioManifestId;
            SessionStartUtc        = sessionStartUtc;
            SessionEndUtc          = sessionEndUtc;
            HardwareCounters       = hardwareCounters;
            HarnessVersion         = harnessVersion;
        }

        /// <summary>
        /// Returns true if all required fields are non-null and non-empty, including the
        /// embedded <see cref="HardwareCounterSnapshot"/> string fields and a positive
        /// <c>CoreCount</c>. Default-constructed hardware snapshots (null CpuModel /
        /// ThermalState, zero CoreCount) fail the check per §3.3.2.
        /// The §3.4.4 baseline validator calls this before entering a baseline into the corpus.
        /// </summary>
        public bool IsComplete()
        {
            return !string.IsNullOrEmpty(GitSha)
                && EnvironmentFingerprint != null
                && !string.IsNullOrEmpty(PlatformPin)
                && !string.IsNullOrEmpty(ScenarioManifestId)
                && !string.IsNullOrEmpty(SessionStartUtc)
                && !string.IsNullOrEmpty(SessionEndUtc)
                && !string.IsNullOrEmpty(HarnessVersion)
                && !string.IsNullOrEmpty(HardwareCounters.CpuModel)
                && !string.IsNullOrEmpty(HardwareCounters.ThermalState)
                && HardwareCounters.CoreCount > 0;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-01 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-2 L-1: IsComplete now also validates HardwareCounters.CpuModel  |
// |         |            |        | and HardwareCounters.ThermalState; default-struct slip-through     |
// |         |            |        | closed.                                                            |
// | 1.2     | 2026-06-02 | —      | PR #129 Codex P2: IsComplete also requires HardwareCounters.Core-  |
// |         |            |        | Count > 0 — completes the §3.3.2 hardware-snapshot triple (CPU     |
// |         |            |        | model, core count, thermal state).                                 |
#endregion
