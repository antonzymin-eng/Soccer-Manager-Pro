// File:     src/localization/LocalizationIdentifier.cs
// Created:  2026-09-10
// Modified: 2026-09-10
// Author:   —
// Spec:     Localization & Accessibility #49 §2.2/§3.5/§4.2, FR-LC-002/004/009/011
// Purpose:  Shared validation for catalogue identities and placeholder names.

using System;

namespace TacticalDirector.Localization
{
    /// <summary>Validates persisted localization identifiers before they reach catalogue lookup.</summary>
    internal static class LocalizationIdentifier
    {
        internal static void ValidateLocale(string value, string parameterName)
        {
            ValidateCommon(value, parameterName, 35, "locale code");

            int segmentLength = 0;
            int segmentIndex = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == '-')
                {
                    ValidateLocaleSegment(segmentIndex, segmentLength, parameterName);
                    segmentIndex++;
                    segmentLength = 0;
                    continue;
                }

                if (!IsAsciiLetter(character) && (segmentIndex == 0 || !IsAsciiDigit(character)))
                {
                    throw new ArgumentException("LocaleId must use ASCII letters, digits in subtags, and '-' separators.", parameterName);
                }

                segmentLength++;
            }

            ValidateLocaleSegment(segmentIndex, segmentLength, parameterName);
        }

        internal static void ValidateKey(string value, string parameterName)
        {
            ValidateCommon(value, parameterName, 255, "localization key");
            ValidateSegmentedIdentifier(value, '.', parameterName, "LocalizationKey");
        }

        internal static void ValidateSlotName(string value, string parameterName)
        {
            ValidateCommon(value, parameterName, 64, "slot name");
            if (!IsAsciiLetter(value[0]) && value[0] != '_')
            {
                throw new ArgumentException("NamedSlot names must start with an ASCII letter or underscore.", parameterName);
            }

            for (int i = 1; i < value.Length; i++)
            {
                char character = value[i];
                if (!IsAsciiLetter(character) && !IsAsciiDigit(character) && character != '_')
                {
                    throw new ArgumentException("NamedSlot names may contain only ASCII letters, digits, and underscores.", parameterName);
                }
            }
        }

        private static void ValidateCommon(string value, string parameterName, int maximumLength, string description)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value.Length == 0)
            {
                throw new ArgumentException("A " + description + " cannot be empty.", parameterName);
            }

            if (value.Length > maximumLength)
            {
                throw new ArgumentException("The " + description + " exceeds its maximum length.", parameterName);
            }
        }

        private static void ValidateSegmentedIdentifier(string value, char separator, string parameterName, string description)
        {
            bool atSegmentStart = true;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == separator)
                {
                    if (atSegmentStart)
                    {
                        throw new ArgumentException(description + " cannot contain empty segments.", parameterName);
                    }

                    atSegmentStart = true;
                    continue;
                }

                if (!IsAsciiLetter(character) && !IsAsciiDigit(character) && character != '_' && character != '-')
                {
                    throw new ArgumentException(description + " contains an invalid character.", parameterName);
                }

                atSegmentStart = false;
            }

            if (atSegmentStart)
            {
                throw new ArgumentException(description + " cannot end with a separator.", parameterName);
            }
        }

        private static void ValidateLocaleSegment(int segmentIndex, int segmentLength, string parameterName)
        {
            int minimum = segmentIndex == 0 ? 2 : 1;
            int maximum = segmentIndex == 0 ? 3 : 8;
            if (segmentLength < minimum || segmentLength > maximum)
            {
                throw new ArgumentException("LocaleId contains an invalid subtag length.", parameterName);
            }
        }

        private static bool IsAsciiLetter(char value)
        {
            return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
        }

        private static bool IsAsciiDigit(char value)
        {
            return value >= '0' && value <= '9';
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-09-10 | —      | Added bounded grammar validation for all string identities.  |
#endregion
