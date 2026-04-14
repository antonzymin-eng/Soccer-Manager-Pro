### 3.1.4 Transition Table

All valid state transitions with conditions. Invalid transitions (not listed) are rejected by the state machine.

```
Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
Ã¢â€â€š FROM         Ã¢â€â€š TO           Ã¢â€â€š CONDITION                                       Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š IDLE         Ã¢â€â€š WALKING      Ã¢â€â€š speed > IDLE_EXIT (0.3 m/s)                     Ã¢â€â€š
Ã¢â€â€š IDLE         Ã¢â€â€š GROUNDED     Ã¢â€â€š Collision knockdown (external trigger)          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š WALKING      Ã¢â€â€š IDLE         Ã¢â€â€š speed < IDLE_ENTER (0.1 m/s)                    Ã¢â€â€š
Ã¢â€â€š WALKING      Ã¢â€â€š JOGGING      Ã¢â€â€š speed > JOG_ENTER (2.2 m/s)                    Ã¢â€â€š
Ã¢â€â€š WALKING      Ã¢â€â€š GROUNDED     Ã¢â€â€š Collision knockdown (external trigger)          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š JOGGING      Ã¢â€â€š WALKING      Ã¢â€â€š speed < JOG_EXIT (1.9 m/s)                     Ã¢â€â€š
Ã¢â€â€š JOGGING      Ã¢â€â€š SPRINTING    Ã¢â€â€š speed > SPRINT_ENTER (5.8 m/s) AND             Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š sprintReservoir >= SPRINT_RESERVOIR_REENTRY     Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š (0.35)                                          Ã¢â€â€š
Ã¢â€â€š JOGGING      Ã¢â€â€š DECELERATING Ã¢â€â€š Command requests speed < current AND            Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š speed > JOG_EXIT                                Ã¢â€â€š
Ã¢â€â€š JOGGING      Ã¢â€â€š DECELERATING Ã¢â€â€š aerobicPool < AEROBIC_JOG_FLOOR (0.15)         Ã¢â€â€š
Ã¢â€â€š JOGGING      Ã¢â€â€š GROUNDED     Ã¢â€â€š Collision knockdown (external trigger)          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š SPRINTING    Ã¢â€â€š JOGGING      Ã¢â€â€š speed < SPRINT_EXIT (5.5 m/s)                  Ã¢â€â€š
Ã¢â€â€š SPRINTING    Ã¢â€â€š JOGGING      Ã¢â€â€š sprintReservoir < SPRINT_RESERVOIR_FLOOR (0.20) Ã¢â€â€š
Ã¢â€â€š SPRINTING    Ã¢â€â€š DECELERATING Ã¢â€â€š Command requests speed < current                Ã¢â€â€š
Ã¢â€â€š SPRINTING    Ã¢â€â€š STUMBLING    Ã¢â€â€š Turn angle > STUMBLE_TURN_ANGLE (60Ã‚Â°) AND      Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š stumble check fails (Section 3.4)               Ã¢â€â€š
Ã¢â€â€š SPRINTING    Ã¢â€â€š GROUNDED     Ã¢â€â€š Collision knockdown (external trigger)          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š DECELERATING Ã¢â€â€š IDLE         Ã¢â€â€š speed < IDLE_ENTER (0.1 m/s)                    Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š WALKING      Ã¢â€â€š speed < JOG_EXIT AND speed > IDLE_EXIT          Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š JOGGING      Ã¢â€â€š Command requests acceleration AND               Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š speed > JOG_EXIT                                Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š SPRINTING    Ã¢â€â€š Command requests acceleration AND               Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š speed > SPRINT_ENTER                            Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š STUMBLING    Ã¢â€â€š Emergency decel at speed > STUMBLE_SPEED AND   Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š stumble check fails                             Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š GROUNDED     Ã¢â€â€š Collision knockdown (external trigger)          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š STUMBLING    Ã¢â€â€š IDLE         Ã¢â€â€š Dwell time elapsed AND speed < IDLE_ENTER       Ã¢â€â€š
Ã¢â€â€š STUMBLING    Ã¢â€â€š WALKING      Ã¢â€â€š Dwell time elapsed AND speed < JOG_EXIT         Ã¢â€â€š
Ã¢â€â€š STUMBLING    Ã¢â€â€š JOGGING      Ã¢â€â€š Dwell time elapsed AND speed > JOG_EXIT         Ã¢â€â€š
Ã¢â€â€š STUMBLING    Ã¢â€â€š GROUNDED     Ã¢â€â€š Speed > STUMBLE_SPEED at stumble start          Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š AND secondary stumble check fails               Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š GROUNDED     Ã¢â€â€š IDLE         Ã¢â€â€š Recovery dwell time elapsed                     Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š (agent always returns to IDLE after grounded)   Ã¢â€â€š
Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
```

**Forbidden Transitions (enforced):**
- IDLE Ã¢â€ â€™ SPRINTING (must pass through WALKING Ã¢â€ â€™ JOGGING first)
- IDLE Ã¢â€ â€™ JOGGING (must pass through WALKING first)
- WALKING Ã¢â€ â€™ SPRINTING (must pass through JOGGING first)
- GROUNDED Ã¢â€ â€™ any state except IDLE (must recover fully)
- STUMBLING Ã¢â€ â€™ SPRINTING (must stabilize before sprinting again)
- Any state Ã¢â€ â€™ STUMBLING without speed > STUMBLE_SPEED_THRESHOLD (low-speed turns never cause stumbles)
- Any state Ã¢â€ â€™ SPRINTING when sprintReservoir < SPRINT_RESERVOIR_REENTRY (must recover)
- JOGGING Ã¢â€ â€™ JOGGING when aerobicPool < AEROBIC_JOG_FLOOR (forced deceleration)

### 3.1.5 State Transition Logic

```csharp
/// <summary>
/// Evaluates and applies movement state transitions.
/// Called once per physics frame (60Hz) BEFORE locomotion formulas.
///
/// Design: Returns new state without modifying agent directly.
/// Caller is responsible for applying state change.
/// Mirrors BallStateMachine pattern from Spec #1.
/// </summary>
public static class AgentStateMachine
{
    /// <summary>
    /// Evaluates the next movement state based on current conditions.
    /// </summary>
    /// <param name="current">Current movement state</param>
    /// <param name="speed">Current scalar speed (m/s)</param>
    /// <param name="commandSpeed">Requested target speed from AI/command layer (m/s)</param>
    /// <param name="turnAngle">Requested turn angle this frame (degrees)</param>
    /// <param name="dwellTimer">Time spent in current state (seconds)</param>
    /// <param name="balance">Agent Balance attribute (1Ã¢â‚¬â€œ20)</param>
    /// <param name="agility">Agent Agility attribute (1Ã¢â‚¬â€œ20)</param>
    /// <param name="strength">Agent Strength attribute (1Ã¢â‚¬â€œ20)</param>
    /// <param name="sprintReservoir">Current sprint reservoir level (1.0 = full, 0.0 = empty)</param>
    /// <param name="aerobicPool">Current aerobic pool level (1.0 = fresh, 0.0 = spent)</param>
    /// <param name="isCollisionKnockdown">External flag from Collision System</param>
    /// <param name="collisionForce">Normalized collision force (0.0Ã¢â‚¬â€œ1.0), only valid when isCollisionKnockdown=true</param>
    /// <returns>New state (may be same as current if no transition)</returns>
    public static AgentMovementState EvaluateState(
        AgentMovementState current,
        float speed,
        float commandSpeed,
        float turnAngle,
        float dwellTimer,
        int balance,
        int agility,
        int strength,
        float sprintReservoir,
        float aerobicPool,
        bool isCollisionKnockdown,
        float collisionForce = 0.0f)
    {
        // Ã¢â€â‚¬Ã¢â€â‚¬ Priority 1: External collision knockdown overrides everything Ã¢â€â‚¬Ã¢â€â‚¬
        // Collision System (Spec #3) determines knockdown; we just respond.
        if (isCollisionKnockdown && current != AgentMovementState.GROUNDED)
        {
            return AgentMovementState.GROUNDED;
        }

        // Ã¢â€â‚¬Ã¢â€â‚¬ Priority 2: State-specific transition evaluation Ã¢â€â‚¬Ã¢â€â‚¬
        switch (current)
        {
            case AgentMovementState.IDLE:
                return EvaluateFromIdle(speed);

            case AgentMovementState.WALKING:
                return EvaluateFromWalking(speed);

            case AgentMovementState.JOGGING:
                return EvaluateFromJogging(speed, commandSpeed, turnAngle,
                    balance, agility, sprintReservoir, aerobicPool);

            case AgentMovementState.SPRINTING:
                return EvaluateFromSprinting(speed, commandSpeed, turnAngle,
                    balance, agility, sprintReservoir);

            case AgentMovementState.DECELERATING:
                return EvaluateFromDecelerating(speed, commandSpeed, turnAngle,
                    balance, agility, sprintReservoir, aerobicPool);

            case AgentMovementState.STUMBLING:
                return EvaluateFromStumbling(speed, dwellTimer, balance);

            case AgentMovementState.GROUNDED:
                return EvaluateFromGrounded(dwellTimer, balance, strength);

            default:
                // Safety: unknown state Ã¢â€ â€™ force IDLE
                return AgentMovementState.IDLE;
        }
    }

    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    // Per-state evaluation methods
    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬

    private static AgentMovementState EvaluateFromIdle(float speed)
    {
        if (speed > MovementThresholds.IDLE_EXIT)
            return AgentMovementState.WALKING;

        return AgentMovementState.IDLE;
    }

    private static AgentMovementState EvaluateFromWalking(float speed)
    {
        if (speed < MovementThresholds.IDLE_ENTER)
            return AgentMovementState.IDLE;

        if (speed > MovementThresholds.JOG_ENTER)
            return AgentMovementState.JOGGING;

        return AgentMovementState.WALKING;
    }

    private static AgentMovementState EvaluateFromJogging(
        float speed, float commandSpeed, float turnAngle,
        int balance, int agility, float sprintReservoir, float aerobicPool)
    {
        // Ã¢â€â‚¬Ã¢â€â‚¬ Aerobic exhaustion: too spent to jog Ã¢â€â‚¬Ã¢â€â‚¬
        if (aerobicPool < MovementThresholds.AEROBIC_JOG_FLOOR)
            return AgentMovementState.DECELERATING;

        if (speed < MovementThresholds.JOG_EXIT)
            return AgentMovementState.WALKING;

        // Ã¢â€â‚¬Ã¢â€â‚¬ Sprint entry: requires sufficient sprint reservoir Ã¢â€â‚¬Ã¢â€â‚¬
        // Uses REENTRY threshold (higher than FLOOR) to enforce recovery gap
        if (speed > MovementThresholds.SPRINT_ENTER
            && sprintReservoir >= MovementThresholds.SPRINT_RESERVOIR_REENTRY)
            return AgentMovementState.SPRINTING;

        if (speed > MovementThresholds.SPRINT_ENTER
            && sprintReservoir < MovementThresholds.SPRINT_RESERVOIR_REENTRY)
            return AgentMovementState.JOGGING;  // Block sprint entry, reservoir too low

        if (commandSpeed < speed - 0.5f)  // Meaningful deceleration requested
            return AgentMovementState.DECELERATING;

        return AgentMovementState.JOGGING;
    }

    private static AgentMovementState EvaluateFromSprinting(
        float speed, float commandSpeed, float turnAngle,
        int balance, int agility, float sprintReservoir)
    {
        // Ã¢â€â‚¬Ã¢â€â‚¬ Priority 1: Sprint reservoir depleted Ã¢â€â‚¬Ã¢â€â‚¬
        // Agent physically cannot sustain sprint effort below this threshold.
        // This is the "hit the wall" moment Ã¢â‚¬â€ legs refuse to sprint.
        // Uses FLOOR (lower than REENTRY) so agent squeezes out every last
        // bit of sprint before being forced down. Re-entry requires recovering
        // back to REENTRY threshold (see EvaluateFromJogging).
        if (sprintReservoir < MovementThresholds.SPRINT_RESERVOIR_FLOOR)
            return AgentMovementState.JOGGING;

        if (speed < MovementThresholds.SPRINT_EXIT)
            return AgentMovementState.JOGGING;

        // Ã¢â€â‚¬Ã¢â€â‚¬ Stumble check for sharp turns at sprint speed Ã¢â€â‚¬Ã¢â€â‚¬
        if (turnAngle > MovementThresholds.STUMBLE_TURN_ANGLE
            && speed > MovementThresholds.STUMBLE_SPEED_THRESHOLD)
        {
            if (ShouldStumble(speed, turnAngle, balance, agility))
                return AgentMovementState.STUMBLING;
        }

        if (commandSpeed < speed - 0.5f)
            return AgentMovementState.DECELERATING;

        return AgentMovementState.SPRINTING;
    }

    private static AgentMovementState EvaluateFromDecelerating(
        float speed, float commandSpeed, float turnAngle,
        int balance, int agility, float sprintReservoir, float aerobicPool)
    {
        // Settled to idle
        if (speed < MovementThresholds.IDLE_ENTER)
            return AgentMovementState.IDLE;

        // Slowed to walking pace
        if (speed < MovementThresholds.JOG_EXIT)
            return AgentMovementState.WALKING;

        // Re-acceleration requested (energy-gated)
        if (commandSpeed > speed + 0.5f)
        {
            if (speed > MovementThresholds.SPRINT_ENTER
                && sprintReservoir >= MovementThresholds.SPRINT_RESERVOIR_REENTRY)
                return AgentMovementState.SPRINTING;
            if (speed > MovementThresholds.JOG_EXIT
                && aerobicPool >= MovementThresholds.AEROBIC_JOG_FLOOR)
                return AgentMovementState.JOGGING;
        }

        // Emergency decel stumble check
        if (turnAngle > MovementThresholds.STUMBLE_TURN_ANGLE
            && speed > MovementThresholds.STUMBLE_SPEED_THRESHOLD)
        {
            if (ShouldStumble(speed, turnAngle, balance, agility))
                return AgentMovementState.STUMBLING;
        }

        return AgentMovementState.DECELERATING;
    }

    private static AgentMovementState EvaluateFromStumbling(
        float speed, float dwellTimer, int balance)
    {
        float requiredDwell = CalculateStumbleDwell(balance);

        if (dwellTimer < requiredDwell)
            return AgentMovementState.STUMBLING;  // Still recovering

        // Recovery complete Ã¢â‚¬â€ transition based on residual speed
        if (speed < MovementThresholds.IDLE_ENTER)
            return AgentMovementState.IDLE;
        if (speed < MovementThresholds.JOG_EXIT)
            return AgentMovementState.WALKING;

        return AgentMovementState.JOGGING;  // Never straight to SPRINTING
    }

    private static AgentMovementState EvaluateFromGrounded(
        float dwellTimer, int balance, int strength)
    {
        float requiredDwell = CalculateGroundedDwell(balance, strength);

        if (dwellTimer < requiredDwell)
            return AgentMovementState.GROUNDED;  // Still recovering

        return AgentMovementState.IDLE;  // Always return to IDLE
    }

    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    // Helper calculations
    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬

    /// <summary>
    /// Determines if a sharp turn at speed causes a stumble.
    /// 
    /// Stumble probability increases with:
    ///   - Higher speed (more momentum to redirect)
    ///   - Sharper turn angle (more lateral force required)
    /// Stumble probability decreases with:
    ///   - Higher Agility (better body control)
    ///   - Higher Balance (better stability)
    ///
    /// Formula:
    ///   stumble_risk = (speed / MAX_SPEED) Ãƒâ€” (turnAngle / 180) Ãƒâ€” difficulty_base
    ///   resistance   = ((agility + balance) / 40.0)
    ///   stumble      = stumble_risk > resistance
    ///
    /// This is a deterministic check, NOT random. Given identical inputs,
    /// the result is always the same (required for replay determinism).
    /// 
    /// Edge cases:
    ///   - Agility 20 + Balance 20 = resistance 1.0 Ã¢â€ â€™ almost never stumbles,
    ///     but MIN_STUMBLE_RISK (0.03) ensures extreme turns (>120Ã‚Â° at >9 m/s)
    ///     can still cause loss of balance even for elite players.
    ///   - Agility 1 + Balance 1 = resistance 0.05 Ã¢â€ â€™ stumbles on most
    ///     sharp turns at speed (physically ungifted player)
    /// </summary>
    private static bool ShouldStumble(
        float speed, float turnAngle, int balance, int agility)
    {
        float difficulty = 1.5f;  // Tunable base difficulty

        float stumbleRisk = (speed / MovementThresholds.MAX_SPEED)
                          * (turnAngle / 180.0f)
                          * difficulty;

        // Floor: even elite players have a minimum stumble risk on
        // extreme maneuvers. Without this, Agility 20 + Balance 20
        // would be physically immune to stumbling, which is unrealistic.
        stumbleRisk = Mathf.Max(stumbleRisk, MovementThresholds.MIN_STUMBLE_RISK);

        float resistance = (agility + balance) / 40.0f;

        return stumbleRisk > resistance;
    }

    /// <summary>
    /// Calculates minimum dwell time in STUMBLING state.
    /// Higher Balance = faster recovery.
    ///
    /// Formula: dwell = BASE / (balance / 20.0)
    /// Clamped to [0.3, 1.5] seconds.
    ///
    /// Examples:
    ///   Balance 20: 0.6 / 1.0 = 0.6s Ã¢â€ â€™ clamped to 0.6s
    ///   Balance 10: 0.6 / 0.5 = 1.2s
    ///   Balance  5: 0.6 / 0.25 = 2.4s Ã¢â€ â€™ clamped to 1.5s
    ///   Balance  1: 0.6 / 0.05 = 12.0s Ã¢â€ â€™ clamped to 1.5s
    /// </summary>
    private static float CalculateStumbleDwell(int balance)
    {
        float balanceFactor = Mathf.Max(balance / 20.0f, 0.05f);  // Prevent div by zero
        float dwell = MovementThresholds.STUMBLE_MIN_DWELL_BASE / balanceFactor;
        return Mathf.Clamp(dwell, 0.3f, 1.5f);
    }

    /// <summary>
    /// Calculates minimum dwell time in GROUNDED state.
    /// Varies by reason (collision vs voluntary), collision force, and attributes.
    ///
    /// Step 1 Ã¢â‚¬â€ Base dwell from reason:
    ///   Collision knockdown: base = BASE
    ///   Voluntary slide:     base = BASE Ãƒâ€” 0.6
    ///   Diving header:       base = BASE Ãƒâ€” 0.7
    ///
    /// Step 2 Ã¢â‚¬â€ Attribute scaling:
    ///   scaled = base / ((strength + balance) / 40.0)
    ///
    /// Step 3 Ã¢â‚¬â€ Collision force scaling (COLLISION reason only):
    ///   final = scaled Ãƒâ€” (COLLISION_DWELL_MIN + (1.0 - COLLISION_DWELL_MIN) Ãƒâ€” collisionForce)
    ///   At force 0.0 (light nudge): final = scaled Ãƒâ€” 0.5
    ///   At force 0.5 (standing tackle): final = scaled Ãƒâ€” 0.75
    ///   At force 1.0 (full-speed collision): final = scaled Ãƒâ€” 1.0
    ///   For voluntary actions (slide, header): force scaling skipped (controlled landing)
    ///
    /// Clamped to [0.5, 2.5] seconds.
    ///
    /// Examples Ã¢â‚¬â€ Collision at force 0.8 (heavy tackle):
    ///   forceScale = 0.65 + 0.35 Ãƒâ€” 0.8 = 0.93
    ///   S20+B20: 1.0 / 1.0 Ãƒâ€” 0.93 = 0.93s
    ///   S12+B12: 1.0 / 0.6 Ãƒâ€” 0.93 = 1.55s
    ///   S5+B5:   1.0 / 0.25 Ãƒâ€” 0.93 = 3.72s Ã¢â€ â€™ clamped to 2.5s
    ///
    /// Examples Ã¢â‚¬â€ Collision at force 0.2 (shoulder nudge):
    ///   forceScale = 0.65 + 0.35 Ãƒâ€” 0.2 = 0.72
    ///   S20+B20: 1.0 / 1.0 Ãƒâ€” 0.72 = 0.72s
    ///   S12+B12: 1.0 / 0.6 Ãƒâ€” 0.72 = 1.2s
    ///
    /// Examples Ã¢â‚¬â€ Voluntary sliding tackle:
    ///   S20+B20: 0.6 / 1.0 = 0.6s (no force scaling)
    ///   S12+B12: 0.6 / 0.6 = 1.0s
    /// </summary>
    private static float CalculateGroundedDwell(
        int balance, int strength,
        GroundedReason reason = GroundedReason.COLLISION,
        float collisionForce = 1.0f)
    {
        float reasonMultiplier = reason switch
        {
            GroundedReason.COLLISION      => 1.0f,
            GroundedReason.SLIDING_TACKLE => 0.6f,
            GroundedReason.DIVING_HEADER  => 0.7f,
            _ => 1.0f
        };

        float combinedFactor = Mathf.Max((strength + balance) / 40.0f, 0.05f);
        float dwell = (MovementThresholds.GROUNDED_MIN_DWELL_BASE * reasonMultiplier)
                     / combinedFactor;

        // Apply collision force scaling for involuntary knockdowns only.
        // Voluntary actions (slide, header) have controlled landings Ã¢â‚¬â€
        // force magnitude is irrelevant to recovery time.
        if (reason == GroundedReason.COLLISION)
        {
            float forceScale = MovementThresholds.COLLISION_DWELL_MIN
                             + (1.0f - MovementThresholds.COLLISION_DWELL_MIN)
                             * Mathf.Clamp01(collisionForce);
            dwell *= forceScale;
        }

        return Mathf.Clamp(dwell, 0.5f, 2.5f);
    }
}
```

