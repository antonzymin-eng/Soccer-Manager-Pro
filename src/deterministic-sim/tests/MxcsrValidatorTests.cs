// File:     src/deterministic-sim/tests/MxcsrValidatorTests.cs
// Created:  2026-07-21
// Author:   —
// Spec:     Deterministic Simulation #16 §4.8.2, §4.8.3, Code Standards #20
// Purpose:  Pure-decode locks for MxcsrValidator (DAZ/FTZ/RC extraction + Stage-0 pin match). These
//           exercise the managed decode logic against synthetic register values, so they need no native
//           shim and run on any host (incl. the Linux dotnet-ci gate). The certified live read (0x1F80
//           on the pinned host) is a cert-run deliverable, not a CI test — see native/README.md.

using NUnit.Framework;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Locks the §4.8.2 MXCSR decode + Stage-0 pin comparison. Bit positions per Intel SDM Vol.1 §10.2.3:
    /// DAZ = bit 6, FTZ = bit 15, RC = bits 13..14.
    /// </summary>
    [TestFixture]
    public sealed class MxcsrValidatorTests
    {
        // Default MXCSR after CPU/process init: all exceptions masked (0x1F80), DAZ/FTZ off, round-nearest.
        private const uint DefaultMxcsr = 0x1F80u;

        [Test]
        public void Default_DecodesToPinnedFloatMode()
        {
            Assert.IsFalse(MxcsrValidator.DenormalsAreZero(DefaultMxcsr), "DAZ should be off in 0x1F80.");
            Assert.IsFalse(MxcsrValidator.FlushToZero(DefaultMxcsr), "FTZ should be off in 0x1F80.");
            Assert.AreEqual(0u, MxcsrValidator.RoundingMode(DefaultMxcsr), "RC should be round-to-nearest.");
            Assert.IsTrue(MxcsrValidator.MatchesStage0Pin(DefaultMxcsr));
        }

        [Test]
        public void DazSet_FailsPin()
        {
            uint mxcsr = DefaultMxcsr | (1u << 6);
            Assert.IsTrue(MxcsrValidator.DenormalsAreZero(mxcsr));
            Assert.IsFalse(MxcsrValidator.MatchesStage0Pin(mxcsr));
        }

        [Test]
        public void FtzSet_FailsPin()
        {
            uint mxcsr = DefaultMxcsr | (1u << 15);
            Assert.IsTrue(MxcsrValidator.FlushToZero(mxcsr));
            Assert.IsFalse(MxcsrValidator.MatchesStage0Pin(mxcsr));
        }

        [TestCase(1u)] // round-down
        [TestCase(2u)] // round-up
        [TestCase(3u)] // round-toward-zero (truncate)
        public void NonNearestRounding_FailsPin(uint rc)
        {
            uint mxcsr = DefaultMxcsr | (rc << 13);
            Assert.AreEqual(rc, MxcsrValidator.RoundingMode(mxcsr));
            Assert.IsFalse(MxcsrValidator.MatchesStage0Pin(mxcsr));
        }

        [Test]
        public void ExceptionFlagsSet_DoNotAffectPin()
        {
            // The low 6 bits are sticky exception FLAGS, not float-mode fields — the pin ignores them.
            uint mxcsr = DefaultMxcsr | 0x3Fu;
            Assert.IsTrue(MxcsrValidator.MatchesStage0Pin(mxcsr));
        }

        [Test]
        public void ValidateStage0FloatMode_NoNativeShim_ReportsUnavailableNotFailure()
        {
            // On the CI host the native td_mxcsr library is absent, so the gate must be a no-op
            // (ProbeStatus.Unavailable), never a thrown failure. On a host where the shim IS present and
            // the float mode matches the pin it returns Validated; a divergent host throws — neither of
            // which occurs on the CI gate. Accept both non-throwing outcomes.
            MxcsrValidator.ProbeStatus status = MxcsrValidator.ValidateStage0FloatMode();
            Assert.That(
                status,
                Is.EqualTo(MxcsrValidator.ProbeStatus.Unavailable)
                    .Or.EqualTo(MxcsrValidator.ProbeStatus.Validated));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-21 | —      | Initial pure-decode locks for the §4.8.2 MXCSR gate.          |
#endregion
