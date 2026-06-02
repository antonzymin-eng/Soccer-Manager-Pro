// File:     src/testing-strategy/TestTier.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.6.1, Deterministic Simulation #16 §1.1.1, Code Standards #20
// Purpose:  Tier classification mirror of Deterministic Simulation #16 §1.1.1
//           ("Equivalence policy by artifact"). Spec #19 consumes this vocabulary;
//           it does not own it (KD-1).

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Tier classification of an authoritative field or scenario per #16 §1.1.1.
    /// Determines comparator policy in #16 §5 regression suite and coverage targets in §3.6.2.
    /// Testing Strategy &amp; Framework #19 §3.6.1 / KD-1.
    /// </summary>
    public enum TestTier : byte
    {
        /// <summary>Authoritative-hard. Bitwise-equality comparator (#16 §1.1.1 / §3.4.2).</summary>
        TierA = 0,

        /// <summary>Bounded-authoritative. Numerical tolerance comparator per #16 §3.4.2 row.</summary>
        TierB = 1,

        /// <summary>Non-authoritative / cosmetic. Envelope comparator; excluded from digest.</summary>
        TierC = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
