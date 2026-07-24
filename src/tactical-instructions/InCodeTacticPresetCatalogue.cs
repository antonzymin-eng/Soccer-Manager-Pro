// File:     src/tactical-instructions/InCodeTacticPresetCatalogue.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Tactical Presets #26 §2.2.2 (FR-TP-002/013), KD-6; Code Standards #20
// Purpose:  The default ITacticPresetCatalogue — a thin, stateless wrapper over the unchanged static
//           TacticPresetLibrary. Injecting this is byte-identical to the pre-WS-1 static path.

using System.Collections.Generic;

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// The Stage-0+1 default catalogue (WS-1): every member returns the corresponding
    /// <see cref="TacticPresetLibrary"/> value verbatim, so a match run against this catalogue is
    /// byte-identical to the pre-refactor static path (each preset already carries its A.3 scoring
    /// row, seeded in the library from <c>TacticalPresetsConstants</c>). The static library is kept
    /// as the backing store — this wrapper moves only <em>how</em> consumers reach the catalogue.
    /// Stateless; safe to construct per engine.
    /// </summary>
    public sealed class InCodeTacticPresetCatalogue : ITacticPresetCatalogue
    {
        /// <inheritdoc/>
        public IReadOnlyList<TacticPreset> Presets => TacticPresetLibrary.Presets;

        /// <inheritdoc/>
        public int Count => TacticPresetLibrary.Count;

        /// <inheritdoc/>
        public byte BalancedOrdinal => TacticPresetLibrary.BalancedOrdinal;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial (WS-1): default catalogue wrapping the static library. |
#endregion
