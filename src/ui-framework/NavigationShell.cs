// File:     src/ui-framework/NavigationShell.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §3.2 (FR-UI-009/010/011, failure mode F2),
//           docs/tracking/ui-framework-t0-implementation-plan.md, Code Standards #20
// Purpose:  The deterministic screen-registration + transition state machine. A pure stack machine over
//           registered ScreenIds with no UGUI dependency — the half of the client that can be fully
//           tested without a Unity host.

using System;
using System.Collections.Generic;

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// A deterministic navigation stack over registered screens (#38 §3.2).
    /// <para>
    /// Semantics, all fail-loud rather than silently forgiving (FR-UI-011 / F2):
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>Push</c> / <c>Replace</c> to an unregistered id throws — a typo must not
    /// strand the user on a blank screen.</description></item>
    /// <item><description><c>Pop</c> at depth 1 throws — the shell never empties its stack, so a rooted
    /// shell always has a <see cref="Current"/>.</description></item>
    /// <item><description><c>Current</c> before the first <c>Push</c> throws — an un-rooted shell has no
    /// current screen, and returning <c>default(ScreenId)</c> would silently alias screen id 0
    /// (the zero-value-safety convention <c>ManagerCommandKind.None</c> follows).</description></item>
    /// </list>
    /// <para>
    /// The shell hard-codes no screen (FR-UI-010): Wave-7 screen specs register into it without any
    /// framework change. It holds no sim reference and mutates nothing outside itself.
    /// </para>
    /// <para>
    /// NOT thread-safe: navigation is driven from the UI/render thread alone.
    /// </para>
    /// </summary>
    public sealed class NavigationShell
    {
        private readonly Dictionary<ScreenId, ScreenRegistration> _registry =
            new Dictionary<ScreenId, ScreenRegistration>();

        private readonly List<ScreenId> _stack = new List<ScreenId>();

        /// <summary>The screen currently on top of the stack. Throws when nothing has been pushed yet.</summary>
        public ScreenId Current
        {
            get
            {
                if (_stack.Count == 0)
                {
                    throw new InvalidOperationException(
                        "NavigationShell has no current screen: nothing has been pushed yet.");
                }
                return _stack[_stack.Count - 1];
            }
        }

        /// <summary>How many screens are on the stack. 0 until the first <see cref="Push"/> roots it.</summary>
        public int Depth => _stack.Count;

        /// <summary>How many screens are registered.</summary>
        public int RegisteredCount => _registry.Count;

        /// <summary>True when <paramref name="id"/> has been registered.</summary>
        public bool IsRegistered(ScreenId id) => _registry.ContainsKey(id);

        /// <summary>
        /// Registers a screen. Re-registering the same id is refused: a silent overwrite would swap a
        /// live screen's source/dispatcher underneath a stack that still references it.
        /// </summary>
        /// <exception cref="ArgumentException">The id is already registered.</exception>
        public void Register(in ScreenRegistration registration)
        {
            if (_registry.ContainsKey(registration.Id))
            {
                throw new ArgumentException(
                    "Screen id is already registered: " + registration.Id + ".", nameof(registration));
            }
            _registry.Add(registration.Id, registration);
        }

        /// <summary>Returns the registration for <paramref name="id"/>.</summary>
        /// <exception cref="ArgumentException">The id is not registered.</exception>
        public ScreenRegistration GetRegistration(ScreenId id)
        {
            if (!_registry.TryGetValue(id, out ScreenRegistration registration))
            {
                throw new ArgumentException("Screen id is not registered: " + id + ".", nameof(id));
            }
            return registration;
        }

        /// <summary>
        /// Pushes <paramref name="id"/> onto the stack, making it current. The first push roots the
        /// shell (#38 §3.5 starts at <c>stack=[0]</c>, which is that push).
        /// </summary>
        /// <exception cref="ArgumentException">The id is not registered (F2).</exception>
        public void Push(ScreenId id)
        {
            RequireRegistered(id);
            _stack.Add(id);
        }

        /// <summary>
        /// Pops the current screen, returning to the one beneath it.
        /// </summary>
        /// <exception cref="InvalidOperationException">The stack is at (or below) its root — the shell
        /// never empties (FR-UI-011).</exception>
        public void Pop()
        {
            if (_stack.Count <= 1)
            {
                throw new InvalidOperationException(
                    "NavigationShell cannot Pop the root screen: the stack would be empty (depth " +
                    _stack.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + ").");
            }
            _stack.RemoveAt(_stack.Count - 1);
        }

        /// <summary>
        /// Replaces the current screen with <paramref name="id"/>, leaving the stack depth unchanged.
        /// </summary>
        /// <exception cref="ArgumentException">The id is not registered (F2).</exception>
        /// <exception cref="InvalidOperationException">Nothing has been pushed yet — there is no
        /// current screen to replace.</exception>
        public void Replace(ScreenId id)
        {
            RequireRegistered(id);
            if (_stack.Count == 0)
            {
                throw new InvalidOperationException(
                    "NavigationShell cannot Replace: nothing has been pushed yet.");
            }
            _stack[_stack.Count - 1] = id;
        }

        /// <summary>Reads the stack bottom-up (index 0 = root). Returns a copy — the caller cannot mutate the shell.</summary>
        public ScreenId[] SnapshotStack() => _stack.ToArray();

        private void RequireRegistered(ScreenId id)
        {
            if (!_registry.ContainsKey(id))
            {
                throw new ArgumentException(
                    "Cannot navigate to an unregistered screen id: " + id +
                    ". Register it first (the shell never silently no-ops).", nameof(id));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): the §3.2 stack machine —            |
// |         |            |        | Register/Push/Pop/Replace/Current with fail-loud unregistered  |
// |         |            |        | navigation (F2), root-Pop refusal, and un-rooted Current       |
// |         |            |        | refusal (no default-id aliasing of screen 0).                  |
#endregion
