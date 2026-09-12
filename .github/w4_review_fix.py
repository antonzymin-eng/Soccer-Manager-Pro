from pathlib import Path


def replace_once(path, old, new):
    p = Path(path)
    text = p.read_text()
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{path}: expected one match, found {count}: {old[:100]!r}")
    p.write_text(text.replace(old, new, 1))


# MatchEngine: visibility owns both DT SAVE availability and reaction-episode timing.
path = "src/match-engine/MatchEngine.cs"
replace_once(
    path,
    '''                        ctx.SaveAvailable = KeeperPerceptionGate.SaveAvailable(
                            t, i, _agents[i].Position,
                            _ball.Position, _ball.Velocity, loose,
                            _agents, _isSentOff);
                        if (armed)
                        {
                            // ERR-011-006 (design KD-C2): seed the §3.2 detection stamp at the
                            // episode's ONSET when no stamp is live — the fallback anchor for
                            // threats with no shot event (deflections, rebounds, mis-hit passes).
                            // A no-op after the episode's first call (the stamp itself is the
                            // latch, serialized in the v19 GK block — no new cross-tick state),
                            // and a true shot CONTACT's NotifyKeeperOfShot stamp, landing in the
                            // prior Resolve phase, is already live by the time this runs, so the
                            // precise strike anchor survives.
                            _goalkeeper.OnThreatArmed(
                                t, _clock.CurrentMatchTimeMs, _ball.Velocity.magnitude,
                                PlayerAttributeProjection.ToGoalkeeper(in _canonicalAttrs[i], t, fatigue: 0f));
                        }
                        else
                        {
                            // One owner of "the episode is over". This latch and #11's own
                            // _saveIntentActive used to have DIFFERENT lifetimes — #11 cleared only when
                            // a dive resolved, this one clears as soon as the geometry lapses — so a
                            // threat that armed, committed and then cleared before the keeper dived left
                            // #11 armed indefinitely and fired at the next Anticipate: a dive at nothing.
                            // Disarming both here keeps them from disagreeing (ClearSaveIntent is a no-op
                            // while a dive is already in flight, so a live attempt still runs to its own
                            // resolution).
                            _saveCommittedForGk[t] = false;
                            _goalkeeper.ClearSaveIntent(t);
                        }
''',
    '''                        bool saveVisible = KeeperPerceptionGate.SaveAvailable(
                            t, i, _agents[i].Position,
                            _ball.Position, _ball.Velocity, loose,
                            _agents, _isSentOff);
                        ctx.SaveAvailable = saveVisible;
                        if (saveVisible)
                        {
                            // W4 review closure: the §3.2 reaction episode begins when the keeper can
                            // actually SEE the raw save threat, not when hidden geometry first arms.
                            // OnThreatArmed remains idempotent while the visible episode stays live, and
                            // a true shot CONTACT can still overwrite it through NotifyKeeperOfShot.
                            _goalkeeper.OnThreatArmed(
                                t, _clock.CurrentMatchTimeMs, _ball.Velocity.magnitude,
                                PlayerAttributeProjection.ToGoalkeeper(in _canonicalAttrs[i], t, fatigue: 0f));
                        }
                        else
                        {
                            // A raw goal-bound threat may still exist here (armed == true) but be hidden
                            // by a live body screen. It continues to veto RUSH through raw SaveArmed, yet
                            // it must not bank reaction time or leave an unconsumed SAVE intent behind.
                            // ClearSaveIntent preserves a dive already in flight, so losing sight cannot
                            // tear down a committed physical attempt.
                            _saveCommittedForGk[t] = false;
                            _goalkeeper.ClearSaveIntent(t);
                        }
''')

# Keeper map refresh belongs to Resolve entry after pending substitutions, not only to deflection frames.
replace_once(
    path,
    '''            PublishPendingSubstitutions();

            int frameNumber = (int)_clock.CurrentTick;          // narrows safely at Stage 0 (~414 days @ 60 Hz)
''',
    '''            PublishPendingSubstitutions();

            // W4 review closure: a substitution published at Resolve entry can change which agent owns
            // a keeper slot. Refresh unconditionally under the GK flag here, before collision/deflection
            // and shot-notification consumers, rather than making ResetSlot timing depend on whether a
            // body deflection happened later in this phase.
            if (_gkHeadingEnabled)
            {
                RefreshGkAgentIds();
            }

            int frameNumber = (int)_clock.CurrentTick;          // narrows safely at Stage 0 (~414 days @ 60 Hz)
''')

replace_once(
    path,
    '''        /// W4 same-Resolve deflection consumer. A changed flight restarts reaction timing only for
        /// the keeper whose goal the POST-deflection ball threatens under raw SaveArmed geometry.
        /// LOS deliberately gates DT SAVE emission, not existence of the reaction episode.
        /// </summary>
        private void ResetKeeperReactionAfterDeflection()
        {
            // Resolve may publish a queued substitution after Physics last refreshed this map.
            RefreshGkAgentIds();
            bool loose = _possessingAgentId == MatchEngineConstants.NO_POSSESSION;
''',
    '''        /// W4 same-Resolve deflection consumer. A changed flight restarts reaction timing only for
        /// the keeper who can currently SEE the POST-deflection raw save threat. A hidden deflection does
        /// not bank reaction credit; the ordinary 10 Hz visibility gate will seed the episode if/when the
        /// ball emerges from the screen.
        /// </summary>
        private void ResetKeeperReactionAfterDeflection()
        {
            bool loose = _possessingAgentId == MatchEngineConstants.NO_POSSESSION;
''')

replace_once(
    path,
    '''                if (!GkHeadingIntentSource.SaveArmed(
                        k, in _ball.Position, in _ball.Velocity, loose))
                {
                    continue;
                }

                _goalkeeper.OnThreatDeflected(
''',
    '''                if (!KeeperPerceptionGate.SaveAvailable(
                        k, agentId, _agents[agentId].Position,
                        _ball.Position, _ball.Velocity, loose,
                        _agents, _isSentOff))
                {
                    continue;
                }

                _goalkeeper.OnThreatDeflected(
''')

replace_once(
    path,
    '''// Modified: 2026-09-11, latest (W4 keeper perception — v1.73: DT SAVE uses live all-body LOS; same-Resolve applied deflections restart the threatened keeper's reaction timing; raw SaveArmed remains the W1 rush veto; no schema/RNG change).
''',
    '''// Modified: 2026-09-12, latest (W4 review closure — v1.74: reaction timing now begins only on visible save threats; post-deflection reset is visibility-gated; keeper-slot refresh moved to unconditional Resolve entry under the GK flag; no schema/RNG change).
// Modified: 2026-09-11 (W4 keeper perception — v1.73: DT SAVE uses live all-body LOS; same-Resolve applied deflections restart the threatened keeper's reaction timing; raw SaveArmed remains the W1 rush veto; no schema/RNG change).
''')

# Append v1.74 before the trailing #endregion in MatchEngine's version history.
p = Path(path)
text = p.read_text()
marker = '''// | 1.73    | 2026-09-11 | —      | W4 keeper perception. RunMechanicsAI gates only DT SAVE availability    |
// |         |            |        | through KeeperPerceptionGate (raw SaveArmed + current-frame all-body   |
// |         |            |        | LOS); raw SaveArmed remains the threat episode and W1 rush exclusion.  |
// |         |            |        | CollisionSystem's transient applied-deflection result is consumed in   |
// |         |            |        | the same Resolve call and restarts only the post-deflection-threatened  |
// |         |            |        | keeper via OnThreatDeflected. No new cross-tick state/schema/RNG.      |
'''
addition = marker + '''// | 1.74    | 2026-09-12 | —      | W4 review closure: reaction stamps are visibility-owned (screened raw  |
// |         |            |        | threats neither emit SAVE nor accrue reaction credit); deflection reset |
// |         |            |        | also requires live LOS. RefreshGkAgentIds moved to Resolve entry after |
// |         |            |        | pending substitutions, removing deflection-conditional ResetSlot timing.|
'''
if text.count(marker) != 1:
    raise RuntimeError("MatchEngine version-history v1.73 marker not found exactly once")
p.write_text(text.replace(marker, addition, 1))

# GoalkeeperMechanics documentation: OnThreatArmed is now a visible-episode fallback.
path = "src/goalkeeper-mechanics/GoalkeeperMechanics.cs"
replace_once(
    path,
    '''        /// or a save resolution clears the stamp, so the caller may invoke it every armed tick with
''',
    '''        /// or a save resolution clears the stamp, so the caller may invoke it every VISIBLE armed tick with
''')
replace_once(
    path,
    '''// Modified: 2026-09-11 (W4: OnThreatDeflected restarts reaction timing for a changed live flight without setting the shot-event latch; no new state/schema)
''',
    '''// Modified: 2026-09-12 (W4 review closure: OnThreatArmed is explicitly a visible-threat episode anchor; no state/schema change)
// Modified: 2026-09-11 (W4: OnThreatDeflected restarts reaction timing for a changed live flight without setting the shot-event latch; no new state/schema)
''')
p = Path(path)
text = p.read_text()
marker = '''// | 1.13 | 2026-09-11 | — | W4: OnThreatDeflected explicitly restarts the detection / required-reaction |
// |      |            |   | stamp after a real body deflection without setting _shotEventPending. A   |
// |      |            |   | deflection is a changed threat, not a newly struck shot. No new state.     |
'''
addition = marker + '''// | 1.14 | 2026-09-12 | — | W4 review closure: caller contract now states OnThreatArmed anchors a      |
// |      |            |   | visible threat episode; screened time is deliberately outside the clock.  |
'''
if text.count(marker) != 1:
    raise RuntimeError("GoalkeeperMechanics version-history v1.13 marker not found exactly once")
p.write_text(text.replace(marker, addition, 1))
