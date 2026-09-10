// File:     src/localization/NamedSlot.cs
// Created:  2026-09-04
// Modified: 2026-09-10
// Author:   —
// Spec:     Localization & Accessibility #49 §2.2/§3.5, FR-LC-004/009/014, Code Standards #20
// Purpose:  Immutable name-to-string slot entry used by producer-agnostic localization requests.

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// One already-formatted named string value supplied by a producer boundary adapter.
    /// </summary>
    public readonly struct NamedSlot
    {
        /// <summary>
        /// Creates one localization slot. Values are strings before they enter the generic core.
        /// </summary>
        public NamedSlot(string name, string value)
        {
            LocalizationIdentifier.ValidateSlotName(name, nameof(name));
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets the placeholder name without braces.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the already-formatted replacement value.
        /// </summary>
        public string Value { get; }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                      |
// | 1.0     | 2026-09-04 | —      | Initial immutable named-slot value.        |
// | 1.1     | 2026-09-10 | —      | Enforce placeholder-safe bounded names.    |
#endregion
