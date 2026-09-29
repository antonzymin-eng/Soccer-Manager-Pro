// ============================================================================
// File:     src/localization/NamedSelectorSet.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 FR-LC-004/009, ERR-049-004
// Purpose:  Immutable canonical collection of named typed selector operands.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>Immutable producer-agnostic name-to-selector collection.</summary>
    public readonly struct NamedSelectorSet : IEquatable<NamedSelectorSet>
    {
        private readonly NamedSelector[] _selectors;

        /// <summary>Creates a defensive, canonically ordered copy of the supplied selectors.</summary>
        public NamedSelectorSet(params NamedSelector[] selectors)
        {
            if (selectors == null)
            {
                throw new ArgumentNullException(nameof(selectors));
            }

            _selectors = new NamedSelector[selectors.Length];
            Array.Copy(selectors, _selectors, selectors.Length);
            SortAndValidate(_selectors);
        }

        /// <summary>Gets the number of selectors.</summary>
        public int Count => _selectors == null ? 0 : _selectors.Length;

        /// <summary>Tries to retrieve an operand by its exact ordinal selector name.</summary>
        public bool TryGetValue(string name, out SelectorOperand operand)
        {
            if (_selectors != null)
            {
                for (int i = 0; i < _selectors.Length; i++)
                {
                    if (string.Equals(_selectors[i].Name, name, StringComparison.Ordinal))
                    {
                        operand = _selectors[i].Operand;
                        return true;
                    }
                }
            }

            operand = default(SelectorOperand);
            return false;
        }

        /// <inheritdoc />
        public bool Equals(NamedSelectorSet other)
        {
            if (Count != other.Count)
            {
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                if (!_selectors[i].Equals(other._selectors[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is NamedSelectorSet other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hash = 17;
            if (_selectors == null)
            {
                return hash;
            }

            unchecked
            {
                for (int i = 0; i < _selectors.Length; i++)
                {
                    hash = LocalizationHash.Combine(hash, _selectors[i].GetHashCode());
                }
            }

            return hash;
        }

        /// <summary>Compares two selector sets by canonical value.</summary>
        public static bool operator ==(NamedSelectorSet left, NamedSelectorSet right)
        {
            return left.Equals(right);
        }

        /// <summary>Compares two selector sets by canonical value.</summary>
        public static bool operator !=(NamedSelectorSet left, NamedSelectorSet right)
        {
            return !left.Equals(right);
        }

        private static void SortAndValidate(NamedSelector[] selectors)
        {
            for (int i = 0; i < selectors.Length; i++)
            {
                if (!selectors[i].IsValid)
                {
                    throw new ArgumentException("NamedSelectorSet cannot contain a default or invalid selector.", nameof(selectors));
                }

                int insert = i;
                while (insert > 0 && string.CompareOrdinal(selectors[insert - 1].Name, selectors[insert].Name) > 0)
                {
                    NamedSelector temporary = selectors[insert - 1];
                    selectors[insert - 1] = selectors[insert];
                    selectors[insert] = temporary;
                    insert--;
                }

                if (insert > 0 && string.Equals(selectors[insert - 1].Name, selectors[insert].Name, StringComparison.Ordinal))
                {
                    throw new ArgumentException("NamedSelectorSet cannot contain duplicate selector names.", nameof(selectors));
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial immutable L1 named selector set. |
// | 1.1     | 2026-09-11 | GPT-5.6 Sol | Add equality operators and remove redundant duplicate check. |
#endregion
