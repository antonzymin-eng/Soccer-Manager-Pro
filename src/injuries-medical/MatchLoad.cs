// File:     src/injuries-medical/MatchLoad.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Injuries & Medical #41 §2.2 / §3.4 (KD-3, FR-MD-010/011); Code Standards #20
// Purpose:  The caller-supplied recent-match-participation input to the occurrence risk. #41 never
//           computes or stores it — that is what keeps the match tick out of the injury model.

namespace TacticalDirector.InjuriesMedical
{
    /// <summary>
    /// Recent match participation, supplied by the caller as an occurrence-risk input (FR-MD-010).
    /// <b>#41 never tracks this itself</b> — #30's fixture result already counts appearances, and
    /// deriving it here would make #41 a second consumer of match state with its own semantics.
    /// <para>
    /// <see cref="AppearanceDays"/> is the Stage-2 term. <see cref="HardContacts"/> is the deep-tier
    /// KD-3 extension: a <b>read-only</b> derivation over the event ledger the match engine already
    /// emits for #37/#44, computed by an integration layer outside both assemblies and handed in here.
    /// #41 adds no match-engine producer and holds no match-engine reference (FR-MD-011), and its
    /// Stage-2 weight is zero, so populating it today changes nothing.
    /// </para>
    /// <para>
    /// <see cref="None"/> (all-zero) is the identity: no match-load contribution, leaving the risk
    /// score as #29's training contribution less the robustness term.
    /// </para>
    /// </summary>
    public readonly struct MatchLoad
    {
        /// <summary>Days of recent match participation feeding the Stage-2 load term.</summary>
        public readonly int AppearanceDays;

        /// <summary>Deep-tier (KD-3) hard-contact count derived read-only from the event ledger. Weighted zero at Stage 2.</summary>
        public readonly int HardContacts;

        /// <summary>Constructs a match-load input.</summary>
        /// <param name="appearanceDays">Days of recent match participation.</param>
        /// <param name="hardContacts">Deep-tier hard-contact count; pass 0 at Stage 2.</param>
        public MatchLoad(int appearanceDays, int hardContacts)
        {
            AppearanceDays = appearanceDays;
            HardContacts = hardContacts;
        }

        /// <summary>The identity input — no match-load contribution. <c>default</c> is legal here: both fields are counts, and zero is their true identity (contrast <c>MedicalModifier</c>, whose zero is not).</summary>
        public static MatchLoad None => default;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0, KD-3). |
#endregion
