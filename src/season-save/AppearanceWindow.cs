// File:     src/season-save/AppearanceWindow.cs
// Created:  2026-08-07
// Modified: 2026-08-08
// Author:   —
// Spec:     Season & Competition Loop #30 §3.4 / Appendix B (the appearance record, ERR-041-010(b));
//           Injuries & Medical #41 FR-MD-010 (the AppearanceDays window unit); Code Standards #20
// Purpose:  The two pure operations on AppearanceState — record a fielded appearance, and read the
//           FR-MD-010 AppearanceDays count for a given world day. No RNG, no allocation, no day step.

using System;

using TacticalDirector.InjuriesMedical;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Records and reads <see cref="AppearanceState"/> (#30 Appendix B / ERR-041-010(b)).
    /// <para>
    /// <b>The window unit (FR-MD-010).</b> <see cref="AppearanceDaysOn"/> counts appearances on the
    /// <c>AppearanceWindowDays</c> days STRICTLY BEFORE the given day — days
    /// <c>[day − window, day − 1]</c>, never <c>day</c> itself. That exclusion is load-bearing for
    /// ERR-030-027's ordering: the occurrence draw runs on matchday morning, pre-round, so the window
    /// it reads must not be able to contain a match that has not been played yet — a match on day
    /// <i>d</i> first feeds the draw on day <i>d+1</i>. The window length is #41's <c>[GT]</c>
    /// (<see cref="InjuriesMedicalConstants.AppearanceWindowDays"/>), because #41 owns what
    /// <c>MatchLoad.AppearanceDays</c> means; #30 owns only the record.
    /// </para>
    /// <para>
    /// Pure integer bit arithmetic throughout — deterministic, allocation-free, and idempotent where
    /// the callers need it (re-recording the same day is a bit-OR; re-reading is a read).
    /// </para>
    /// </summary>
    public static class AppearanceWindow
    {
        /// <summary>
        /// Records that the player was fielded on <paramref name="worldDay"/>: re-anchors the bitmask
        /// to that day and sets bit 0. Idempotent per day. Bits shifted past bit 31 fall off — with the
        /// window bounded to 31 (see <see cref="RequireValidWindow"/>), nothing readable is ever lost.
        /// </summary>
        /// <param name="state">The player's appearance state, mutated in place.</param>
        /// <param name="worldDay">The day the player was fielded.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="worldDay"/> is EARLIER than the state's anchor — appearances are recorded in
        /// world-day order by the season loop, so a regression means the state was paired with the
        /// wrong career or the clock went backwards; re-anchoring backwards would silently discard the
        /// newer appearances.
        /// </exception>
        public static void Record(ref AppearanceState state, uint worldDay)
        {
            if (worldDay < state.BitsAsOfWorldDay)
            {
                throw new ArgumentException(
                    $"Appearance recorded on day {worldDay}, before the state's anchor day "
                    + $"{state.BitsAsOfWorldDay} — the season loop records in world-day order, so a "
                    + "regression is a wrong-career pairing or a clock fault, not a valid write.",
                    nameof(worldDay));
            }

            uint shift = worldDay - state.BitsAsOfWorldDay;
            uint carried = shift >= 32 ? 0u : state.RecentBits << (int)shift;

            state.RecentBits = carried | 1u;
            state.BitsAsOfWorldDay = worldDay;
        }

        /// <summary>
        /// The FR-MD-010 <c>AppearanceDays</c> count for a draw taken on <paramref name="worldDay"/>:
        /// the number of appearances on days <c>[worldDay − window, worldDay − 1]</c>. The current day
        /// is never counted (see the type doc — ERR-030-027's pre-round ordering depends on it).
        /// </summary>
        /// <param name="state">The player's appearance state, read-only.</param>
        /// <param name="worldDay">The day the occurrence draw is being taken on.</param>
        /// <exception cref="ArgumentException"><paramref name="worldDay"/> is earlier than the state's anchor — a read from the anchor's past has no well-defined window.</exception>
        /// <exception cref="InvalidOperationException">The configured window is outside <c>[1, 31]</c> — a catalogue integrity failure, not a caller error (see <see cref="RequireValidWindow"/>).</exception>
        public static int AppearanceDaysOn(in AppearanceState state, uint worldDay)
        {
            int window = RequireValidWindow();

            if (worldDay < state.BitsAsOfWorldDay)
            {
                throw new ArgumentException(
                    $"Appearance window read on day {worldDay}, before the state's anchor day "
                    + $"{state.BitsAsOfWorldDay}.",
                    nameof(worldDay));
            }

            // Bit k is the appearance on day (anchor − k); its age at worldDay is shift + k. The window
            // admits ages [1, window], so bits [max(0, 1 − shift) .. window − shift] qualify — empty
            // once the shift alone exceeds the window.
            uint shift = worldDay - state.BitsAsOfWorldDay;
            if (shift > (uint)window)
            {
                return 0;
            }

            int lo = shift == 0 ? 1 : 0;
            int hi = window - (int)shift;

            uint mask = WindowMask(lo, hi);
            return PopCount(state.RecentBits & mask);
        }

        /// <summary>Bits <paramref name="lo"/>..<paramref name="hi"/> inclusive, both in <c>[0, 31]</c> with <c>lo ≤ hi</c> guaranteed by the caller's window bound.</summary>
        private static uint WindowMask(int lo, int hi)
        {
            // width <= 31 on every path: RequireValidWindow bounds hi <= window - shift <= 31, and
            // lo == 0 only when shift >= 1 (AR pass 6 L2 — the old width >= 32 branch was
            // unreachable dead defence no test could cover).
            int width = hi - lo + 1;
            uint run = (1u << width) - 1u;
            return run << lo;
        }

        /// <summary>SWAR population count — no <c>System.Numerics</c> dependency under the netstandard2.1 gate profile.</summary>
        private static int PopCount(uint v)
        {
            v = v - ((v >> 1) & 0x55555555u);
            v = (v & 0x33333333u) + ((v >> 2) & 0x33333333u);
            return (int)((((v + (v >> 4)) & 0x0F0F0F0Fu) * 0x01010101u) >> 24);
        }

        /// <summary>
        /// The window length, gated to <c>[1, 31]</c>. 31 is the structural ceiling of the u32 bitmask
        /// (bit 0 is the anchor day itself, which the window never counts); a config value above it
        /// would silently under-count old appearances rather than fail, so it refuses here — the
        /// <c>DrawOccurrence</c> denominator-guard posture.
        /// </summary>
        private static int RequireValidWindow()
        {
            int window = InjuriesMedicalConstants.AppearanceWindowDays;
            if (window < 1 || window > SeasonSaveConstants.APPEARANCE_BITMASK_MAX_WINDOW_DAYS)
            {
                throw new InvalidOperationException(
                    $"AppearanceWindowDays = {window} is outside "
                    + $"[1, {SeasonSaveConstants.APPEARANCE_BITMASK_MAX_WINDOW_DAYS}] — the u32 "
                    + "appearance bitmask cannot represent a longer window (FR-MD-010); "
                    + "catalogue/config integrity failure.");
            }

            return window;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-07 | —      | Initial implementation (balance pass D2, ERR-041-010(b)): record  |
// |         |            |        | + windowed read, current day excluded per ERR-030-027's pre-round |
// |         |            |        | ordering.                                                         |
// | 1.1     | 2026-08-08 | —      | Balance-pass AR pass 5 (L4): RequireValidWindow reads the      |
// |         |            |        | catalogued [FIXED] bound instead of a bare 31.                 |
// | 1.2     | 2026-08-08 | —      | Balance-pass AR pass 6 (L2): WindowMask's unreachable          |
// |         |            |        | width >= 32 branch deleted — RequireValidWindow bounds every   |
// |         |            |        | path to width <= 31; the bound is stated where the shift is.   |
#endregion
