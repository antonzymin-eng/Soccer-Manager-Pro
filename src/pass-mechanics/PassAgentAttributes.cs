// File:     src/pass-mechanics/PassAgentAttributes.cs
// Created:  2026-05-26
// Modified: 2026-06-11
// Author:   —
// Spec:     Pass Mechanics #5 §4.3.1, Code Standards #20
// Purpose:  PassAgentAttributes struct: Pass Mechanics' snapshot of agent attributes
//           consumed from Agent Movement. ERR-007 proxies documented here.

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Pass Mechanics' snapshot of the attributes consumed from Agent Movement.
    /// Pass Mechanics #5 §4.3.1.
    /// [ERR-007-PENDING] KickPower, WeakFootRating, Crossing are proxied until
    /// Agent Movement PlayerAttributes gains those fields.
    /// </summary>
    public struct PassAgentAttributes
    {
        /// <summary>Primary accuracy attribute [1–20]. Agent Movement §3.5.6.</summary>
        public float Passing;

        /// <summary>Secondary accuracy attribute; Vision proxy at Stage 0 [1–20]. Agent Movement §3.5.6.</summary>
        public float Technique;

        /// <summary>
        /// Primary velocity attribute [1–20]. Agent Movement §3.5.6.
        /// [TEMPORARY-PROXY-ERR-007] Computed as (Passing + Technique) * 0.5f until ERR-007 resolved.
        /// </summary>
        public float KickPower;

        /// <summary>
        /// Weak-foot quality rating [1–5]. Agent Movement §3.5.6.
        /// [TEMPORARY-PROXY-ERR-007] Defaults to 3 (mid-scale) until ERR-007 resolved.
        /// </summary>
        public int WeakFootRating;

        /// <summary>
        /// Cross accuracy attribute [1–20]. Agent Movement §3.5.6.
        /// [TEMPORARY-PROXY-ERR-007] Mirrors Passing until ERR-007 resolved.
        /// NOTE (AR-9 L-3): declared-but-unconsumed at Stage 0 — no calculator reads it
        /// (cross accuracy currently flows through Passing in the §3.5 error chain).
        /// Retained for the ERR-007 attribute split.
        /// </summary>
        public float Crossing;

        /// <summary>Fatigue scalar [0, 1]. 0 = fully rested, 1 = fully fatigued. Agent Movement §3.5.6.</summary>
        public float Fatigue;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-26 | —      | Extracted from IPassAgentQuery.cs per one-type-per-file rule (H5). |
// | 1.1     | 2026-06-11 | —      | AR-9 L-3 (doc-only): Crossing noted as declared-but-unconsumed at  |
// |         |            |        |     Stage 0; retained for the ERR-007 attribute split.             |
#endregion
