// File:     src/client-app/S0TextRole.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §4, Code Standards #20
// Purpose:  Immutable identity, base pattern and typed positional schema for a client-owned text role.

using System;
using TacticalDirector.Localization;

namespace TacticalDirector.ClientApp
{
    /// <summary>One static role or composite pattern; S/string, I/nonnegative int and F/finite float.</summary>
    public sealed class S0TextRole
    {
        /// <summary>Creates a client-owned schema; construction also validates its base pattern.</summary>
        public S0TextRole(string suffix, string basePattern, string argumentTypes)
        {
            Key = new LocalizationKey("ui.s0." + suffix);
            BasePattern = basePattern ?? throw new ArgumentNullException(nameof(basePattern));
            ArgumentTypes = argumentTypes ?? throw new ArgumentNullException(nameof(argumentTypes));
            foreach (char type in ArgumentTypes)
                if (type != 'S' && type != 'I' && type != 'F')
                    throw new ArgumentException("Unknown S0 argument type.");
            S0TextFormatter.ValidatePattern(this, BasePattern);
        }

        /// <summary>Fully qualified exact static localization identity.</summary>
        public LocalizationKey Key { get; }
        /// <summary>Reviewed English content.</summary>
        public string BasePattern { get; }
        /// <summary>Position-ordered argument types; empty for a static label.</summary>
        public string ArgumentTypes { get; }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Typed S0 static/pattern schema. |
#endregion
