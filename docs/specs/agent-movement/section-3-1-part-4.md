### 3.1.6 State-Dependent Physics Activation

Each state activates a specific subset of locomotion formulas defined in Sections 3.2Ã¢â‚¬â€œ3.5. This table is the authoritative mapping.

```
Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
Ã¢â€â€š State        Ã¢â€â€š Accel  Ã¢â€â€š Decel  Ã¢â€â€š Turn   Ã¢â€â€šDirectionÃ¢â€â€š Fatigue  Ã¢â€â€š VoluntaryÃ¢â€â€š
Ã¢â€â€š              Ã¢â€â€š (3.2)  Ã¢â€â€š (3.2)  Ã¢â€â€š (3.4)  Ã¢â€â€š Mult    Ã¢â€â€š (3.5)    Ã¢â€â€š Control  Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š (3.3)   Ã¢â€â€š          Ã¢â€â€š          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š IDLE         Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€š Free   Ã¢â€â€š   Ã¢â‚¬â€     Ã¢â€â€š Passive  Ã¢â€â€š Full     Ã¢â€â€š
Ã¢â€â€š WALKING      Ã¢â€â€š Linear Ã¢â€â€š Linear Ã¢â€â€š Free   Ã¢â€â€š  Yes    Ã¢â€â€š Passive  Ã¢â€â€š Full     Ã¢â€â€š
Ã¢â€â€š JOGGING      Ã¢â€â€š Exp    Ã¢â€â€š Ctrl   Ã¢â€â€šModerateÃ¢â€â€š  Yes    Ã¢â€â€š Active   Ã¢â€â€š Full     Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š         Ã¢â€â€š Aero gateÃ¢â€â€š          Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š         Ã¢â€â€š : 0.15   Ã¢â€â€š          Ã¢â€â€š
Ã¢â€â€š SPRINTING    Ã¢â€â€š Exp    Ã¢â€â€š Ctrl   Ã¢â€â€š Tight  Ã¢â€â€š  Yes    Ã¢â€â€š Active+  Ã¢â€â€š Full     Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š         Ã¢â€â€šSprint gatÃ¢â€â€š          Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š         Ã¢â€â€š e: 0.20  Ã¢â€â€š          Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€šCtrl/EmgÃ¢â€â€š LimitedÃ¢â€â€š  Yes    Ã¢â€â€š Active   Ã¢â€â€š Partial  Ã¢â€â€š
Ã¢â€â€š STUMBLING    Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€š Drag   Ã¢â€â€š  None  Ã¢â€â€š   Ã¢â‚¬â€     Ã¢â€â€šRecovery  Ã¢â€â€š None     Ã¢â€â€š
Ã¢â€â€š GROUNDED     Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€š  None  Ã¢â€â€š   Ã¢â‚¬â€     Ã¢â€â€šRecovery  Ã¢â€â€š None     Ã¢â€â€š
Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
```

**Key:**
- **Accel**: Linear = constant rate; Exp = exponential curve `a(t) = a_max Ãƒâ€” (1 - e^(-kÃƒâ€”t))`
- **Decel**: Ctrl = controlled braking; Emg = emergency braking; Drag = friction-only (no voluntary braking)
- **Turn**: Free = unrestricted; Moderate = radius constraint; Tight = large radius constraint; Limited = reduced rate; None = momentum only
- **Direction Mult**: Whether directional speed multipliers (forward/lateral/backward) are applied
- **Fatigue**: Passive = minimal drain; Active = standard drain; Active+ = accelerated drain (sprint reservoir); Recovery = fatigue recovery paused; **Gate values** = state forcibly exited when respective energy pool drops below threshold (sprint reservoir for SPRINTING, aerobic pool for JOGGING)
- **Voluntary Control**: Full = agent can change direction/speed freely; Partial = can brake but limited steering; None = physics-only (momentum carries agent)

### 3.1.7 Oscillation Guard

Mirrors the Ball Physics pattern for detecting state machine instability.

```csharp
/// <summary>
/// Tracks state transitions to detect oscillation.
/// If transitions exceed MAX_TRANSITIONS_PER_SECOND, locks current state
/// for a cooldown period.
///
/// Implementation: Ring buffer of transition timestamps.
/// Check: Count transitions in last 1.0 second window.
/// </summary>
public struct OscillationGuard
{
    private const int BUFFER_SIZE = 8;
    private float[] _transitionTimes;  // Fixed-size, no heap alloc after init
    private int _writeIndex;
    private bool _isLocked;
    private float _lockUntilTime;

    /// <summary>Lock duration when oscillation detected (seconds)</summary>
    private const float LOCK_DURATION = 0.5f;

    /// <summary>
    /// Records a state transition and checks for oscillation.
    /// Returns true if the transition should be BLOCKED (oscillation detected).
    /// </summary>
    public bool RecordAndCheck(float currentTime)
    {
        // If currently locked, block transition
        if (_isLocked && currentTime < _lockUntilTime)
            return true;  // BLOCK

        _isLocked = false;

        // Record transition
        _transitionTimes[_writeIndex] = currentTime;
        _writeIndex = (_writeIndex + 1) % BUFFER_SIZE;

        // Count transitions in last 1.0 second
        int recentCount = 0;
        for (int i = 0; i < BUFFER_SIZE; i++)
        {
            if (currentTime - _transitionTimes[i] < 1.0f)
                recentCount++;
        }

        if (recentCount > MovementThresholds.MAX_TRANSITIONS_PER_SECOND)
        {
            _isLocked = true;
            _lockUntilTime = currentTime + LOCK_DURATION;
            // Log WARNING: oscillation detected for agent [ID]
            return true;  // BLOCK
        }

        return false;  // ALLOW
    }
}
```

### 3.1.8 Coordinate System & Units

Consistent with Ball Physics Spec #1:

| Property | Axis | Unit | Notes |
|----------|------|------|-------|
| Position X | East-West | meters | Pitch length axis |
| Position Y | North-South | meters | Pitch width axis |
| Position Z | Vertical (up) | meters | 0 = ground level; agents always z Ã¢â€°Â¥ 0 |
| Velocity | XYZ | m/s | Magnitude = scalar speed |
| Facing direction | XY plane | unit vector | Z component always 0 for outfield players |
| Turn angle | Ã¢â‚¬â€ | degrees | Measured in XY plane, 0Ã‚Â° = no turn, 180Ã‚Â° = reversal |
| Time | Ã¢â‚¬â€ | seconds | Frame delta = 1/60 Ã¢â€°Ë† 0.01667s |

---

**End of Section 3.1**

**Page Count:** ~10 pages  
**Next Section:** Section 3.2 Ã¢â‚¬â€ Locomotion (Acceleration, Top Speed, Deceleration)

