// File:     src/player-database/PlayerAttributes.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md) §3, Code Standards #20
// Purpose:  Canonical player attribute record reconciling the seven per-spec attribute structs
//           (Agent Movement #2, Decision Tree #8, Pass Mechanics #5, Shot Mechanics #6, Heading
//           Mechanics #10, Goalkeeper Mechanics #11, Perception #7). Every per-spec struct is a
//           projection of this record (a later T-phase wires the projection; not done here).

using System;

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>
    /// Canonical player attributes: 31 int[1,20] fields (26 consumed by an existing Stage-0 spec,
    /// 5 reserved for master-plan attributes no spec consumes yet — declared-but-unconsumed, same
    /// pattern as DtAgentAttributes.Crossing) + WeakFootRating, a separate [1,5] scale never folded
    /// into the [1,20] set. Design doc §3.
    /// </summary>
    public struct PlayerAttributes
    {
        // -- Physical (6) — Agent Movement #2 --
        /// <summary>Top speed potential [1,20]. Agent Movement #2 §3.5.1.</summary>
        public int Pace;
        /// <summary>Acceleration burst rate [1,20]. Agent Movement #2 §3.5.1.</summary>
        public int Acceleration;
        /// <summary>Lateral/backward movement quality [1,20]. Agent Movement #2 §3.5.1.</summary>
        public int Agility;
        /// <summary>Stability during turning/collision recovery [1,20]. Agent Movement #2 §3.5.1.</summary>
        public int Balance;
        /// <summary>Recovery speed from GROUNDED state [1,20]. Agent Movement #2 §3.5.1.</summary>
        public int Strength;
        /// <summary>Aerobic endurance [1,20]. Agent Movement #2 §3.5.1.</summary>
        public int Stamina;

        // -- Technical (7) --
        /// <summary>Primary passing accuracy [1,20]. Pass Mechanics #5 §4.3.1.</summary>
        public int Passing;
        /// <summary>Secondary accuracy / touch quality [1,20]. Pass Mechanics #5 §4.3.1, Shot Mechanics #6.</summary>
        public int Technique;
        /// <summary>Shooting accuracy [1,20]. Shot Mechanics #6.</summary>
        public int Finishing;
        /// <summary>Long-range shooting intent [1,20]. Shot Mechanics #6, Decision Tree #8 §3.1.4.2.</summary>
        public int LongShots;
        /// <summary>Ball manipulation skill [1,20]. Decision Tree #8 §3.2.4.</summary>
        public int Dribbling;
        /// <summary>Cross accuracy [1,20]. Decision Tree #8 §3.1.3.4; declared-but-unconsumed at Stage 0 (ERR-008-006).</summary>
        public int Crossing;
        /// <summary>Heading accuracy/power [1,20]. Heading Mechanics #10.</summary>
        public int Heading;

        // -- Mental (7) --
        /// <summary>Cognitive breadth under pressure [1,20]. Decision Tree #8 §3.1.3.6, Perception #7 §4.2.2.</summary>
        public int Decisions;
        /// <summary>Passing-range awareness [1,20]. Decision Tree #8 §3.2.2.</summary>
        public int Vision;
        /// <summary>Maintained calm under pressure [1,20]. Decision Tree #8 §3.3.4, Shot Mechanics #6, Goalkeeper #11.</summary>
        public int Composure;
        /// <summary>Proactive read of play [1,20]. Decision Tree #8 §3.2.8, Perception #7 §4.2.2.</summary>
        public int Anticipation;
        /// <summary>Pressing intensity / work capacity [1,20]. Decision Tree #8 §3.2.7.</summary>
        public int WorkRate;
        /// <summary>Pressing aggression [1,20]. Decision Tree #8 §3.2.7.</summary>
        public int Aggression;
        /// <summary>Positional discipline [1,20]. Decision Tree #8 §3.2.6.</summary>
        public int Positioning;

        // -- Goalkeeping (6) — Goalkeeper Mechanics #11 --
        /// <summary>Shot-stopping reaction quality [1,20]. Goalkeeper Mechanics #11.</summary>
        public int Reflexes;
        /// <summary>Catch-vs-parry handling quality [1,20]. Goalkeeper Mechanics #11.</summary>
        public int Handling;
        /// <summary>Aerial claim/cross-catching ability [1,20]. Goalkeeper Mechanics #11.</summary>
        public int Aerial;
        /// <summary>One-on-one composure [1,20]. Goalkeeper Mechanics #11.</summary>
        public int OneVsOne;
        /// <summary>Throw distribution quality [1,20]. Goalkeeper Mechanics #11.</summary>
        public int Throwing;
        /// <summary>Kick distribution quality [1,20]. Goalkeeper Mechanics #11.</summary>
        public int Kicking;

        // -- Reserved (5) — master plan §4.2; no Stage-0 spec consumes these yet --
        /// <summary>RESERVED [1,20]. Master plan §4.2; not yet consumed by any Stage-0 spec.</summary>
        public int Tackling;
        /// <summary>RESERVED [1,20]. Master plan §4.2; not yet consumed by any Stage-0 spec.</summary>
        public int Marking;
        /// <summary>RESERVED [1,20]. Master plan §4.2; not yet consumed by any Stage-0 spec.</summary>
        public int Concentration;
        /// <summary>RESERVED [1,20]. Master plan §4.2; not yet consumed by any Stage-0 spec.</summary>
        public int Teamwork;
        /// <summary>RESERVED [1,20]. Master plan §4.2 "First Touch" trait; distinct from the First Touch Mechanics spec (#4). Not yet consumed by any Stage-0 spec.</summary>
        public int FirstTouchAbility;

        // -- Special scale --
        /// <summary>
        /// Weak-foot quality rating [1,5] — a DIFFERENT scale from every field above; never folded
        /// into <see cref="ToArray"/>/<see cref="FromArray"/> or the [1,20] clamp. Pass Mechanics #5 / Shot Mechanics #6.
        /// </summary>
        public int WeakFootRating;

        /// <summary>Creates attributes initialised to mid-range (10) for all [1,20] fields, WeakFootRating = 3.</summary>
        public static PlayerAttributes CreateDefault()
        {
            var a = new PlayerAttributes();
            int[] mid = new int[AttrIdx.Count];
            for (int i = 0; i < mid.Length; i++)
            {
                mid[i] = PlayerDatabaseConstants.AttributeBaseMean;
            }
            a.FromArray(mid);
            a.WeakFootRating = PlayerDatabaseConstants.WeakFootBase;
            return a;
        }

        /// <summary>Copies the 31 [1,20] fields into a new array in <see cref="AttrIdx"/> order. Excludes WeakFootRating.</summary>
        public int[] ToArray()
        {
            var arr = new int[AttrIdx.Count];
            arr[AttrIdx.Pace] = Pace;
            arr[AttrIdx.Acceleration] = Acceleration;
            arr[AttrIdx.Agility] = Agility;
            arr[AttrIdx.Balance] = Balance;
            arr[AttrIdx.Strength] = Strength;
            arr[AttrIdx.Stamina] = Stamina;
            arr[AttrIdx.Passing] = Passing;
            arr[AttrIdx.Technique] = Technique;
            arr[AttrIdx.Finishing] = Finishing;
            arr[AttrIdx.LongShots] = LongShots;
            arr[AttrIdx.Dribbling] = Dribbling;
            arr[AttrIdx.Crossing] = Crossing;
            arr[AttrIdx.Heading] = Heading;
            arr[AttrIdx.Decisions] = Decisions;
            arr[AttrIdx.Vision] = Vision;
            arr[AttrIdx.Composure] = Composure;
            arr[AttrIdx.Anticipation] = Anticipation;
            arr[AttrIdx.WorkRate] = WorkRate;
            arr[AttrIdx.Aggression] = Aggression;
            arr[AttrIdx.Positioning] = Positioning;
            arr[AttrIdx.Reflexes] = Reflexes;
            arr[AttrIdx.Handling] = Handling;
            arr[AttrIdx.Aerial] = Aerial;
            arr[AttrIdx.OneVsOne] = OneVsOne;
            arr[AttrIdx.Throwing] = Throwing;
            arr[AttrIdx.Kicking] = Kicking;
            arr[AttrIdx.Tackling] = Tackling;
            arr[AttrIdx.Marking] = Marking;
            arr[AttrIdx.Concentration] = Concentration;
            arr[AttrIdx.Teamwork] = Teamwork;
            arr[AttrIdx.FirstTouchAbility] = FirstTouchAbility;
            return arr;
        }

        /// <summary>Assigns the 31 [1,20] fields from an array in <see cref="AttrIdx"/> order. Does not touch WeakFootRating.</summary>
        /// <exception cref="ArgumentException"><paramref name="values"/> is not exactly <see cref="AttrIdx.Count"/> long.</exception>
        public void FromArray(int[] values)
        {
            if (values == null || values.Length != AttrIdx.Count)
            {
                throw new ArgumentException($"Expected exactly {AttrIdx.Count} values.", nameof(values));
            }
            Pace = values[AttrIdx.Pace];
            Acceleration = values[AttrIdx.Acceleration];
            Agility = values[AttrIdx.Agility];
            Balance = values[AttrIdx.Balance];
            Strength = values[AttrIdx.Strength];
            Stamina = values[AttrIdx.Stamina];
            Passing = values[AttrIdx.Passing];
            Technique = values[AttrIdx.Technique];
            Finishing = values[AttrIdx.Finishing];
            LongShots = values[AttrIdx.LongShots];
            Dribbling = values[AttrIdx.Dribbling];
            Crossing = values[AttrIdx.Crossing];
            Heading = values[AttrIdx.Heading];
            Decisions = values[AttrIdx.Decisions];
            Vision = values[AttrIdx.Vision];
            Composure = values[AttrIdx.Composure];
            Anticipation = values[AttrIdx.Anticipation];
            WorkRate = values[AttrIdx.WorkRate];
            Aggression = values[AttrIdx.Aggression];
            Positioning = values[AttrIdx.Positioning];
            Reflexes = values[AttrIdx.Reflexes];
            Handling = values[AttrIdx.Handling];
            Aerial = values[AttrIdx.Aerial];
            OneVsOne = values[AttrIdx.OneVsOne];
            Throwing = values[AttrIdx.Throwing];
            Kicking = values[AttrIdx.Kicking];
            Tackling = values[AttrIdx.Tackling];
            Marking = values[AttrIdx.Marking];
            Concentration = values[AttrIdx.Concentration];
            Teamwork = values[AttrIdx.Teamwork];
            FirstTouchAbility = values[AttrIdx.FirstTouchAbility];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                    |
// | 1.0     | 2026-07-15 | —      | Initial implementation.  |
#endregion
