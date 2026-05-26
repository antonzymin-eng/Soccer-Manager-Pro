// File:     src/first-touch/IFirstTouchSystem.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     First Touch Mechanics #4 §4.5.1, Code Standards #20
// Purpose:  Public interface for the first-touch evaluation and application pipeline.

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Contract for the first-touch system: pure evaluation and stateful application.
    /// First Touch Mechanics #4 §4.5.1.
    /// </summary>
    public interface IFirstTouchSystem
    {
        /// <summary>
        /// Evaluates a first-touch attempt and returns the computed result without side effects.
        /// First Touch Mechanics #4 §4.5.1.
        /// </summary>
        /// <param name="context">Snapshot of all per-touch input data.</param>
        /// <returns>Computed touch result including outcome, ball position, and possession IDs.</returns>
        public FirstTouchResult EvaluateFirstTouch(FirstTouchContext context);

        /// <summary>
        /// Applies a previously evaluated touch result to the simulation state via injected subsystems.
        /// First Touch Mechanics #4 §4.5.1.
        /// </summary>
        /// <param name="result">Result produced by EvaluateFirstTouch.</param>
        /// <param name="context">The same context passed to EvaluateFirstTouch.</param>
        public void ApplyTouchResult(FirstTouchResult result, FirstTouchContext context);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
