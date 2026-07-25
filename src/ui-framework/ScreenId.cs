// File:     src/ui-framework/ScreenId.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §2.2 / §3.2 (FR-UI-010),
//           docs/tracking/ui-framework-t0-implementation-plan.md, Code Standards #20
// Purpose:  The screen identity the navigation shell keys on, and the registration record binding that
//           identity to its { view-model source, command dispatcher } pair. Both are UGUI-free: the
//           shell knows ids and handles, never a concrete Unity view, which is what makes the
//           navigation contract testable without a Unity host.

using System;

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// A screen's identity — a value-type wrapper over an int so the registry can key on it without
    /// boxing and without the "any int is a screen" looseness a bare int would invite.
    /// <para>
    /// Ids are assigned by the caller (the Wave-7 screen specs register their own); the framework
    /// hard-codes none (FR-UI-010).
    /// </para>
    /// </summary>
    public readonly struct ScreenId : IEquatable<ScreenId>
    {
        /// <summary>The underlying identity value.</summary>
        public readonly int Value;

        /// <summary>Constructs a screen id.</summary>
        public ScreenId(int value)
        {
            Value = value;
        }

        /// <inheritdoc/>
        public bool Equals(ScreenId other) => Value == other.Value;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is ScreenId other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => Value;

        /// <summary>Value equality.</summary>
        public static bool operator ==(ScreenId left, ScreenId right) => left.Equals(right);

        /// <summary>Value inequality.</summary>
        public static bool operator !=(ScreenId left, ScreenId right) => !left.Equals(right);

        /// <inheritdoc/>
        public override string ToString() =>
            "Screen#" + Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// One screen's registration: its id plus the two handles a screen is defined by (#38 §3.2) — the
    /// source it projects its view model from, and the dispatcher it routes manager intent through.
    /// <para>
    /// Both handles are optional and may be null: a purely informational screen (a static menu) has no
    /// view model to project and no intent to dispatch. What is NOT optional is that a non-null
    /// dispatcher can only reach public sim seams — that is the assembly boundary's guarantee, not a
    /// runtime check (FR-UI-014).
    /// </para>
    /// </summary>
    public readonly struct ScreenRegistration
    {
        /// <summary>The registered screen's identity.</summary>
        public readonly ScreenId Id;

        /// <summary>
        /// The screen's view-model source, or null when the screen projects nothing. Held through the
        /// non-generic marker because the registry is heterogeneous; the owning screen casts to its own
        /// <see cref="IViewModelSource{T}"/> to project.
        /// </summary>
        public readonly IViewModelSource ViewModelSource;

        /// <summary>The screen's command dispatcher, or null when the screen issues no manager intent.</summary>
        public readonly ICommandDispatcher Dispatcher;

        /// <summary>Constructs a registration.</summary>
        public ScreenRegistration(ScreenId id, IViewModelSource viewModelSource, ICommandDispatcher dispatcher)
        {
            Id              = id;
            ViewModelSource = viewModelSource;
            Dispatcher      = dispatcher;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): value-type screen id with value     |
// |         |            |        | equality + the { source, dispatcher } registration record.     |
#endregion
