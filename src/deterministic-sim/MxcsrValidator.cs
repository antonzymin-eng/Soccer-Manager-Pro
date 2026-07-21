// File:     src/deterministic-sim/MxcsrValidator.cs
// Created:  2026-07-21
// Author:   —
// Spec:     Deterministic Simulation #16 §4.8.2 (runtime float-mode validation), §4.8.3 (float-flag tuple), Code Standards #20
// Purpose:  Decodes the live MXCSR (via MxcsrNative) and validates the three float-mode fields the
//           determinism pin constrains — DAZ / FTZ / rounding mode — against their Stage-0 pinned
//           values (§4.8.3 fields 5/6/7, mirrored by FloatFlagTuple). Fail-loud on mismatch where the
//           native probe is available; a no-op where it is not (defense-in-depth over the certified pin,
//           not a replacement for the determinism-KAT proof). Init-time only — never on the hot path.

using System;
using System.Globalization;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Runtime float-mode gate (#16 §4.8.2). Queries the live MXCSR and rejects a host whose
    /// denormals-are-zero / flush-to-zero / rounding-mode bits diverge from the Stage-0 determinism pin
    /// — a divergence means a driver, plugin, or the runtime changed the register and the bits this run
    /// produces would not be the certified bits.
    /// </summary>
    public static class MxcsrValidator
    {
        // ── MXCSR bit layout (Intel SDM Vol.1 §10.2.3) ────────────────────────────────────
        // Only the fields the determinism pin constrains are decoded here.

        /// <summary>[FIXED] MXCSR bit 6 — Denormals Are Zero (DAZ).</summary>
        private const int DAZ_BIT = 6;

        /// <summary>[FIXED] MXCSR bit 15 — Flush To Zero (FTZ).</summary>
        private const int FTZ_BIT = 15;

        /// <summary>[FIXED] MXCSR bits 13..14 — Rounding Control (RC): 0=nearest, 1=down, 2=up, 3=truncate.</summary>
        private const int RC_SHIFT = 13;

        /// <summary>[FIXED] Two-bit mask for the rounding-control field once shifted.</summary>
        private const uint RC_MASK = 0x3u;

        // ── Stage-0 pinned values (#16 §4.8.3 fields 5/6/7; mirror FloatFlagTuple's Stage-0 required set) ──

        /// <summary>[FIXED] Stage-0 pin: DAZ off. §4.8.3 field 5.</summary>
        private const bool STAGE0_DAZ = false;

        /// <summary>[FIXED] Stage-0 pin: FTZ off. §4.8.3 field 6.</summary>
        private const bool STAGE0_FTZ = false;

        /// <summary>[FIXED] Stage-0 pin: round-to-nearest-even (RC == 0). §4.8.3 field 7.</summary>
        private const uint STAGE0_RC = 0u;

        /// <summary>Outcome of a runtime MXCSR check.</summary>
        public enum ProbeStatus
        {
            /// <summary>The native shim was not loadable on this host — the gate did not run (not a failure).</summary>
            Unavailable = 0,

            /// <summary>The live MXCSR was read and matches the Stage-0 float-mode pin.</summary>
            Validated = 1,
        }

        /// <summary>Denormals-Are-Zero bit of a raw MXCSR value.</summary>
        public static bool DenormalsAreZero(uint mxcsr) => ((mxcsr >> DAZ_BIT) & 1u) != 0u;

        /// <summary>Flush-To-Zero bit of a raw MXCSR value.</summary>
        public static bool FlushToZero(uint mxcsr) => ((mxcsr >> FTZ_BIT) & 1u) != 0u;

        /// <summary>Rounding-control field (0..3) of a raw MXCSR value.</summary>
        public static uint RoundingMode(uint mxcsr) => (mxcsr >> RC_SHIFT) & RC_MASK;

        /// <summary>
        /// True when <paramref name="mxcsr"/>'s DAZ / FTZ / rounding-mode fields equal the Stage-0 pin.
        /// Pure decode — no native query — so it is unit-testable without the plugin.
        /// </summary>
        public static bool MatchesStage0Pin(uint mxcsr)
        {
            return DenormalsAreZero(mxcsr) == STAGE0_DAZ
                && FlushToZero(mxcsr) == STAGE0_FTZ
                && RoundingMode(mxcsr) == STAGE0_RC;
        }

        /// <summary>
        /// Queries the live MXCSR and validates it against the Stage-0 float-mode pin (#16 §4.8.2).
        /// Returns <see cref="ProbeStatus.Unavailable"/> when the native shim is absent (the gate is a
        /// no-op off the pinned host); returns <see cref="ProbeStatus.Validated"/> when the live register
        /// matches the pin; throws <see cref="InvalidOperationException"/> when it is loadable but the
        /// float mode diverges (a driver / plugin / runtime changed MXCSR — the run must abort rather than
        /// produce non-certified bits). Call once at match start / restore step 0, on the sim thread.
        /// </summary>
        public static ProbeStatus ValidateStage0FloatMode()
        {
            if (!MxcsrNative.TryQuery(out uint mxcsr))
            {
                return ProbeStatus.Unavailable;
            }

            if (!MatchesStage0Pin(mxcsr))
            {
                throw new InvalidOperationException(
                    "Runtime float-mode pin violated: MXCSR=0x"
                    + mxcsr.ToString("X4", CultureInfo.InvariantCulture)
                    + " (DAZ=" + DenormalsAreZero(mxcsr).ToString(CultureInfo.InvariantCulture)
                    + ", FTZ=" + FlushToZero(mxcsr).ToString(CultureInfo.InvariantCulture)
                    + ", RC=" + RoundingMode(mxcsr).ToString(CultureInfo.InvariantCulture)
                    + "); expected DAZ/FTZ off and round-to-nearest-even (#16 §4.8.2 / §4.8.3).");
            }

            return ProbeStatus.Validated;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-21 | —      | Initial implementation. §4.8.2 runtime MXCSR gate: decode DAZ  |
// |         |            |        | /FTZ/RC + validate against the Stage-0 pin; fail-loud where    |
// |         |            |        | the native probe loads, no-op where it does not.               |
#endregion
