// ============================================================================
// File:     src/localization/StaticTextEntry.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §3.2, KD-5, FR-LC-011
// Purpose:  One opaque static string row of a catalogue.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// One static row: a key and its authored text. The text is opaque — it is never parsed or
    /// formatted, so client composite patterns such as <c>{0}</c> pass through unchanged.
    /// </summary>
    public readonly struct StaticTextEntry
    {
        /// <summary>Creates a static row.</summary>
        public StaticTextEntry(LocalizationKey key, string text)
        {
            if (!key.IsValid)
            {
                throw new ArgumentException("A static row requires a valid key.", nameof(key));
            }

            Key = key;
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <summary>Static string identity.</summary>
        public LocalizationKey Key { get; }

        /// <summary>Authored text, returned verbatim by <see cref="ILocalizer.Resolve"/>.</summary>
        public string Text { get; }

        /// <summary>True for a row built through the constructor.</summary>
        public bool IsValid => Key.IsValid && Text != null;
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 static row (localization-l2-plan v0.5 §5.1, §5.3). |
#endregion
