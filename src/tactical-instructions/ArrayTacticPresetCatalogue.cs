// File:     src/tactical-instructions/ArrayTacticPresetCatalogue.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Tactical Presets #26 §2.2.2 (FR-TP-002/013), KD-6; Code Standards #20
// Purpose:  An immutable, array-backed ITacticPresetCatalogue for a catalogue built from data (e.g.
//           parsed from disk by TacticPresetFileLoader), as opposed to the static in-code library.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// A catalogue built from an explicit preset list (WS-1) — the shape a disk loader produces. The
    /// presets are snapshot-copied at construction and exposed read-only. A catalogue must be
    /// non-empty and its <see cref="BalancedOrdinal"/> must be in range (fail loud otherwise) — a
    /// preset catalogue is a variable-length ladder with no "identity" default, unlike a single
    /// TeamTactic. Any content validation beyond that (ordinal↔content stability, the Balanced-midpoint
    /// contract) is the loader's responsibility before construction.
    /// </summary>
    public sealed class ArrayTacticPresetCatalogue : ITacticPresetCatalogue
    {
        private readonly ReadOnlyCollection<TacticPreset> _presets;

        /// <summary>
        /// Constructs a catalogue from <paramref name="presets"/> (index = ordinal). Fails loud on a
        /// null/empty list or an out-of-range <paramref name="balancedOrdinal"/>.
        /// </summary>
        public ArrayTacticPresetCatalogue(IReadOnlyList<TacticPreset> presets, byte balancedOrdinal)
        {
            if (presets == null)
            {
                throw new ArgumentNullException(nameof(presets));
            }
            if (presets.Count == 0)
            {
                throw new ArgumentException(
                    "A preset catalogue must be non-empty (it has no identity default).", nameof(presets));
            }
            if (balancedOrdinal >= presets.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(balancedOrdinal), balancedOrdinal, "BalancedOrdinal beyond the catalogue.");
            }

            // Snapshot-copy so a retained caller reference cannot mutate the catalogue afterwards.
            var copy = new TacticPreset[presets.Count];
            for (int i = 0; i < presets.Count; i++)
            {
                copy[i] = presets[i];
            }
            _presets = new ReadOnlyCollection<TacticPreset>(copy);
            BalancedOrdinal = balancedOrdinal;
        }

        /// <inheritdoc/>
        public IReadOnlyList<TacticPreset> Presets => _presets;

        /// <inheritdoc/>
        public int Count => _presets.Count;

        /// <inheritdoc/>
        public byte BalancedOrdinal { get; }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial (WS-1): array-backed catalogue for disk-loaded presets.|
#endregion
