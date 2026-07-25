// File:     src/ui-framework/IViewModelSource.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §2.2 / §3.1 (FR-UI-002/006/007/008),
//           docs/tracking/ui-framework-t0-implementation-plan.md, Code Standards #20
// Purpose:  The read-only projection contract every screen binds: a pure read of an observation
//           surface returning an immutable view model. This is the composition seam (KD-5) — a screen
//           holds several sources (a sim/loop source + a #37 analytics source + later #48/#49), binds
//           each independently, and owns none of their logic.

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// Non-generic marker for a view-model source, so <see cref="ScreenRegistration"/> can hold
    /// heterogeneous sources without falling back to <c>object</c>. It is a readability aid, not a
    /// strong guarantee — an empty interface can be implemented by anything, so it narrows intent
    /// rather than enforcing it. Carries no members: the owning screen knows its own view-model type
    /// and casts to <see cref="IViewModelSource{T}"/> to project.
    /// </summary>
    public interface IViewModelSource
    {
    }

    /// <summary>
    /// Projects an immutable view model <typeparamref name="T"/> from an observation surface.
    /// <para>
    /// The contract (#38 §3.1):
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Pure</b> — <see cref="Project"/> MUST NOT mutate any sim or loop state,
    /// draw, or read a wall clock; the same observed state yields the same <typeparamref name="T"/>
    /// (FR-UI-006).</description></item>
    /// <item><description><b>Immutable output</b> — <typeparamref name="T"/> is a value type whose
    /// contents are copies; no live buffer or mutable engine handle may escape into it (FR-UI-002/007,
    /// failure mode F4).</description></item>
    /// <item><description><b>Fail-loud inputs</b> — every agent index is bounds-gated and every sampled
    /// value NaN-gated before it enters <typeparamref name="T"/> (FR-UI-008, failure mode F1).</description></item>
    /// </list>
    /// <para>
    /// The <c>struct</c> constraint is the mechanical half of "immutable output": a reference-type view
    /// model could hand out a mutable graph, and there would be no compile-time barrier to a screen
    /// writing through it.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The immutable view-model value type.</typeparam>
    public interface IViewModelSource<T> : IViewModelSource
        where T : struct
    {
        /// <summary>
        /// Reads the current observation surface and returns an immutable projection of it. Never
        /// advances, ticks, or otherwise mutates the observed system.
        /// </summary>
        T Project();
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): the generic projection contract     |
// |         |            |        | plus the non-generic marker the screen registry stores         |
// |         |            |        | (plan AR-1 L-1 — was `object`).                                |
#endregion
