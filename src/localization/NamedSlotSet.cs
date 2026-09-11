// ============================================================================
// File:     src/localization/NamedSlotSet.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §2.2, FR-LC-004/009/014
// Purpose:  Immutable canonical collection of named preformatted string slots.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>Immutable producer-agnostic name-to-string slot collection.</summary>
    public readonly struct NamedSlotSet : IEquatable<NamedSlotSet>
    {
        private readonly NamedSlot[] _slots;

        /// <summary>Creates a defensive, canonically ordered copy of the supplied slots.</summary>
        public NamedSlotSet(params NamedSlot[] slots)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            _slots = new NamedSlot[slots.Length];
            Array.Copy(slots, _slots, slots.Length);
            SortAndValidate(_slots);
        }

        /// <summary>Gets the number of slots.</summary>
        public int Count => _slots == null ? 0 : _slots.Length;

        /// <summary>Tries to retrieve a value by its exact ordinal slot name.</summary>
        public bool TryGetValue(string name, out string value)
        {
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

        /// <inheritdoc />
        public bool Equals(NamedSlotSet other)
        {
            if (Count != other.Count)
            {
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                if (!_slots[i].Equals(other._slots[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is NamedSlotSet other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hash = 17;
            if (_slots == null)
            {
                return hash;
            }

            unchecked
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    hash = LocalizationHash.Combine(hash, _slots[i].GetHashCode());
                }
            }

            return hash;
        }

        /// <summary>Compares two slot sets by canonical value.</summary>
        public static bool operator ==(NamedSlotSet left, NamedSlotSet right)
        {
            return left.Equals(right);
        }

        /// <summary>Compares two slot sets by canonical value.</summary>
        public static bool operator !=(NamedSlotSet left, NamedSlotSet right)
        {
            return !left.Equals(right);
        }

        private static void SortAndValidate(NamedSlot[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsValid)
                {
                    throw new ArgumentException("NamedSlotSet cannot contain a default or invalid slot.", nameof(slots));
                }

                int insert = i;
                while (insert > 0 && string.CompareOrdinal(slots[insert - 1].Name, slots[insert].Name) > 0)
                {
                    NamedSlot temporary = slots[insert - 1];
                    slots[insert - 1] = slots[insert];
                    slots[insert] = temporary;
                    insert--;
                }

                if (insert > 0 && string.Equals(slots[insert - 1].Name, slots[insert].Name, StringComparison.Ordinal))
                {
                    throw new ArgumentException("NamedSlotSet cannot contain duplicate slot names.", nameof(slots));
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial immutable L1 named slot set. |
// | 1.1     | 2026-09-11 | GPT-5.6 Sol | Add equality operators and remove redundant duplicate check. |
#endregion
