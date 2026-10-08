// File:     src/client-app/S0PresentationConfiguration.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §3, Localization #49 FR-LC-019, Code Standards #20
// Purpose:  Fixed shell-lifetime text scale, separate from simulation and client settings storage.

using System;

namespace TacticalDirector.ClientApp
{
    /// <summary>Read-only shell construction input, never a gameplay or save input.</summary>
    public sealed class S0PresentationConfiguration
    {
        /// <summary>Admits the owner-approved inclusive 100–200% range without clamping.</summary>
        public S0PresentationConfiguration(float textScale = 1f)
        {
            if (float.IsNaN(textScale) || textScale < 1f || textScale > 2f)
                throw new ArgumentOutOfRangeException(nameof(textScale), "S0 text scale must be finite and in [1, 2].");
            TextScale = textScale;
        }

        /// <summary>[FIXED] Shell-lifetime multiplier applied consistently without autoshrink.</summary>
        public float TextScale { get; }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Fixed, validated 100–200% presentation scale. |
#endregion
