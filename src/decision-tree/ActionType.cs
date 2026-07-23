// File:     src/decision-tree/ActionType.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Modified: 2026-07-23 (SAVE = 7 — the DT-emitted goalkeeper save; ERR-008-013)
// Spec:     Decision Tree #8 §2.2.1, Code Standards #20
// Purpose:  Enum of all Stage 0 action types. Ordinal values are stable; used
//           as hash inputs by the composure noise function (§3.3.3). Do not reorder.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// All action types available in the Stage 0 Decision Tree pipeline.
    /// Ordinals (PASS=0 … SAVE=7) are canonical hash inputs — do not renumber.
    /// Decision Tree #8 §2.2.1.
    /// </summary>
    public enum ActionType
    {
        PASS            = 0,
        SHOOT           = 1,
        DRIBBLE         = 2,
        HOLD            = 3,
        MOVE_TO_POSITION = 4,
        PRESS           = 5,
        INTERCEPT       = 6,

        // SAVE (ERR-008-013): the goalkeeper save the #11 SaveIntent doc always anticipated the DT
        // committing. Generated only in the off-ball branch for the threatened keeper when
        // TacticalContext.SaveAvailable (set only under MatchEngine.EnableGkHeading — flag-off never
        // emits it). Ordinal 7 is the LAST that fits the 3-bit composure-noise field
        // (ActionSelector.ComputeOptionNoise); an 8th action would overflow it and force a
        // composure-noise digest rebaseline (a DT-emitted HEADER is deferred for exactly this reason).
        SAVE            = 7
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-07-23 | —      | + SAVE = 7 (DT-emitted goalkeeper save; ERR-008-013). Fits the 3-bit |
// |         |            |        |   noise field (last ordinal that does); HEADER=8 would overflow → deferred. |
#endregion
