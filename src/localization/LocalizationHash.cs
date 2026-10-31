// ============================================================================
// File:     src/localization/LocalizationHash.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §2.2, Code Standards #20 §3.4
// Purpose:  Stable ordinal hash helpers for localization contract value types.
// ============================================================================

namespace TacticalDirector.Localization
{
    internal static class LocalizationHash
    {
        internal static int StringOrdinal(string value)
        {
            if (value == null)
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }

                return hash;
            }
        }

        internal static int Combine(int left, int right)
        {
            unchecked
            {
                return (left * 397) ^ right;
            }
        }

        internal static int Long(long value)
        {
            unchecked
            {
                return Combine((int)value, (int)(value >> 32));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial stable hash helper. |
#endregion
