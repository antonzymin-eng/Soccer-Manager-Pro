// File:     src/tactical-instructions/ITacticPresetCatalogue.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Tactical Presets #26 §2.2.2 / §4.4 (FR-TP-002/013), KD-6; Code Standards #20
// Purpose:  The injectable preset-catalogue abstraction (WS-1). Lets the running manager AI reach a
//           catalogue that is either the in-code default or one loaded from disk, without the
//           consumers reading a static singleton by reference.

using System.Collections.Generic;

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// An ordered, APPEND-only tactic-preset catalogue (#26 §2.2.2). The ordinal is both the
    /// serialized preset identity (<c>ManagerState.CurrentPresetOrdinal</c>) and the §3.4/§3.5
    /// <c>StepToward</c> ladder position (defensive → attacking). Consumers
    /// (<c>ManagerAdaptation</c> / the match engine) take this instead of reading
    /// <see cref="TacticPresetLibrary"/> by static reference, so a disk-loaded catalogue can be
    /// injected. Each <see cref="TacticPreset"/> carries its own A.3 kickoff-scoring row
    /// (<c>BaseFit</c>/<c>AggrAffinity</c>/<c>CautAffinity</c>), so <c>KickoffScore</c> reaches the
    /// scoring coefficients through <see cref="Presets"/> — the catalogue itself needs no parallel
    /// scoring table, and a catalogue of any <see cref="Count"/> is score-safe.
    /// </summary>
    public interface ITacticPresetCatalogue
    {
        /// <summary>The presets in pinned ladder order; index = ordinal.</summary>
        IReadOnlyList<TacticPreset> Presets { get; }

        /// <summary>Catalogue length (the F2 restore-seam bound for serialized ordinals).</summary>
        int Count { get; }

        /// <summary>
        /// The ordinal of the Balanced midpoint — the kickoff default for a no-affinity profile and
        /// the ManagerState seed. Consumed at a determinism-load-bearing site, so a loaded catalogue
        /// must establish and validate it (never assume the compiled constant).
        /// </summary>
        byte BalancedOrdinal { get; }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial (WS-1): injectable catalogue seam over the static      |
// |         |            |        |   TacticPresetLibrary, so a disk-loaded catalogue can reach     |
// |         |            |        |   the manager-AI consumers.                                    |
#endregion
