// File:     src/localization/NamedSlotSet.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   —
// Spec:     Localization & Accessibility #49 §2.2/§3.5, FR-LC-004/009/014, Code Standards #20
// Purpose:  Immutable producer-agnostic name-to-string slot collection with no mutable storage exposure.

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// Immutable collection of already-formatted localization slots.
    /// </summary>
    public readonly struct NamedSlotSet
    {
        private readonly NamedSlot[] _slots;

        /// <summary>
        /// Creates a slot set by defensively copying the supplied entries and rejecting duplicate names.
        /// </summary>
        public NamedSlotSet(params NamedSlot[] slots)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            _slots = new NamedSlot[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                NamedSlot slot = slots[i];
                if (string.IsNullOrEmpty(slot.Name))
                {
                    throw new ArgumentException("NamedSlotSet contains a default or malformed slot.", nameof(slots));
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_slots[j].Name, slot.Name, StringComparison.Ordinal))
                    {
                        throw new ArgumentException("NamedSlotSet contains duplicate slot name '" + slot.Name + "'.", nameof(slots));
                    }
                }

                _slots[i] = slot;
            }
        }

        /// <summary>
        /// Gets the number of slots. A default instance is an empty set.
        /// </summary>
        public int Count => _slots == null ? 0 : _slots.Length;

        /// <summary>
        /// Attempts an ordinal lookup without exposing the internal backing array.
        /// </summary>
        public bool TryGetValue(string name, out string value)
        {
            if (name == null)
            {
                value = null;
                return false;
            }

            if (_slots != null)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (string.Equals(_slots[i].Name, name, StringComparison.Ordinal))
                    {
                        value = _slots[i].Value;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Returns the value for a required slot or throws when the slot is absent.
        /// </summary>
        public string GetValue(string name)
        {
            if (TryGetValue(name, out string value))
            {
                return value;
            }

            throw new InvalidOperationException("NamedSlotSet does not contain required slot '" + name + "'.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-09-04 | —      | Initial defensive-copy immutable slot collection.        |
#endregion
