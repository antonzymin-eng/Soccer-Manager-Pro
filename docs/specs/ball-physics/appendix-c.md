## Appendix C: Visual Diagrams

This appendix provides text-based diagrams for specification clarity. These are intended to be recreated as proper graphics during Stage 1 (2D Rendering Specification) but serve as authoritative references for implementation.

---

### C.1 Ball State Machine

```
                         Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
                         Ã¢â€â€šSTATIONARYÃ¢â€â€š
                         Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
                              Ã¢â€â€š
                    Kick/Touch event
                              Ã¢â€â€š
                    Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
                    Ã¢â€â€š                   Ã¢â€â€š
              v.z > ENTER         v.z Ã¢â€°Ë† 0
              threshold           (ground)
                    Ã¢â€â€š                   Ã¢â€â€š
               Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â      Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
               Ã¢â€â€š AIRBORNE Ã¢â€â€š      Ã¢â€â€š   ROLLING   Ã¢â€â€š
               Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ      Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
                    Ã¢â€â€š                    Ã¢â€â€š
        z Ã¢â€°Â¤ EXIT_THRESH          v < MIN_VELOCITY
        AND v.z < 0                     Ã¢â€â€š
                    Ã¢â€â€š              Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
               Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â        Ã¢â€â€š STATIONARY  Ã¢â€â€š
               Ã¢â€â€šBOUNCING Ã¢â€â€š        Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
               Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
                    Ã¢â€â€š
            Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
            Ã¢â€â€š       Ã¢â€â€š       Ã¢â€â€š
        v.z > BOUNCE   v.z < BOUNCE   v < MIN_V
        THRESH         THRESH
            Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š
       Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â   Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â  Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
       Ã¢â€â€š AIRBORNE Ã¢â€â€š   Ã¢â€â€š  ROLLING  Ã¢â€â€š  Ã¢â€â€š STATIONARY Ã¢â€â€š
       Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ

  Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬

  ANY STATE Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ IsOutOfBounds() Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº OUT_OF_PLAY

  ANY STATE Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Agent possession Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº CONTROLLED
  CONTROLLED Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Kick/release Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº AIRBORNE or ROLLING
```

**Hysteresis detail:**
```
  Height (z, ball center)
    Ã¢â€â€š
  0.17m Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ ENTER threshold (ROLLING Ã¢â€ â€™ AIRBORNE)
    Ã¢â€â€š          Ã¢â€“Â²
    Ã¢â€â€š   Dead   Ã¢â€â€š  Ball must RISE above 0.17m to become AIRBORNE
    Ã¢â€â€š   zone   Ã¢â€â€š  Ball must FALL below 0.13m to leave AIRBORNE
    Ã¢â€â€š          Ã¢â€“Â¼
  0.13m Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ EXIT threshold (AIRBORNE Ã¢â€ â€™ BOUNCING)
    Ã¢â€â€š
  0.11m Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ground level (ball center at RADIUS)
```

---

### C.2 Force Diagram: Airborne Ball

```
                    F_magnus (perpendicular to v and Ãâ€°)
                        Ã¢â€ â€”
                       /
                      /
            Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢Å â€¢Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº v (velocity)
                     Ã¢â€â€š\
                     Ã¢â€â€š \
                     Ã¢â€â€š  \ F_drag (opposes v)
                     Ã¢â€â€š
                     Ã¢â€“Â¼
                  F_gravity (always -Z)
                  = -4.22 N


  Net force: F_net = F_gravity + F_drag + F_magnus
  
  Typical magnitudes at 25 m/s, 12 rad/s spin:
    F_gravity = 4.22 N (constant, downward)
    F_drag    = 1.45 N (opposing motion)
    F_magnus  = 1.07 N (perpendicular to flight)
```

---

### C.3 Force Diagram: Rolling Ball

```
  Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ground surface
  
            F_drag (opposes v)
               Ã¢â€ ÂÃ¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ã¢Å â€¢ Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº v (velocity)
                     Ã¢â€â€š
                     Ã¢â€“Â¼
              F_rolling_friction
              = ÃŽÂ¼_r Ãƒâ€” m Ãƒâ€” g
              (opposes v, parallel to ground)

  Notes:
  - No gravity force shown (balanced by normal force from ground)
  - No Magnus force (only active in AIRBORNE state)
  - Drag is typically small at rolling speeds
  - Rolling friction dominates below ~7 m/s
```

---

### C.4 Bounce Contact Mechanics

```
  BEFORE BOUNCE:                    AFTER BOUNCE:

       v_incoming                      v_rebound
          Ã¢â€ Ëœ                               Ã¢â€ â€”
           \  ÃŽÂ¸_in                  ÃŽÂ¸_out /
            \                            /
  Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€”ÂÃ¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬    Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€”ÂÃ¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
           GROUND                    GROUND

  Normal decomposition:
  
  v_n = v Ã‚Â· nÃŒâ€š  (into ground, negative)    Ã¢â€ â€™  v_n_after = -e Ãƒâ€” v_n
  v_t = v - v_nÃ‚Â·nÃŒâ€š  (along ground)         Ã¢â€ â€™  v_t_after = v_t + J_friction/m


  Contact point velocity (with spin):

                  Ãâ€° (angular velocity)
                  Ã¢â€ Âº
              Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
              Ã¢â€â€š       Ã¢â€â€š  r = 0.11m
              Ã¢â€â€š   Ã¢â€”Â   Ã¢â€â€š  center
              Ã¢â€â€š       Ã¢â€â€š
              Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
                  Ã¢â€â€š r_contact = -r Ãƒâ€” nÃŒâ€š
                  Ã¢â€”Â  contact point
  Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ ground
  
  v_contact = v_tangential + (Ãâ€° Ãƒâ€” r_contact)
  
  If ball has topspin: contact point moves FORWARD
    Ã¢â€ â€™ friction acts BACKWARD on contact point
    Ã¢â€ â€™ ball gains backspin, loses forward speed (less than no-spin case)
    
  If ball has backspin: contact point moves BACKWARD
    Ã¢â€ â€™ friction acts FORWARD on contact point  
    Ã¢â€ â€™ ball "checks up" Ã¢â‚¬â€ dramatically reduces forward speed
```

---

### C.5 Coordinate System Reference

```
  TOP VIEW (as rendered in 2D):
  
  Y (pitch width, 0-68m)
  Ã¢â€“Â²
  Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
  Ã¢â€â€š  Ã¢â€â€š                                                 Ã¢â€â€š
68mÃ¢â€â€š  Ã¢â€â€š                    Ã¢Å â€¢ Ball (x,y)                Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€š                                                 Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€š     GOAL                             GOAL       Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€š     Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤                             Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤       Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€š                                                 Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€š                                                 Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
  Ã¢â€â€š
  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº X (pitch length, 0-105m)
  
  
  SIDE VIEW (Z axis, not rendered but simulated):
  
  Z (height)
  Ã¢â€“Â²
  Ã¢â€â€š        Ã¢â€¢Â­Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ trajectory Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â®
  Ã¢â€â€š       Ã¢â€¢Â±                    Ã¢â€¢Â²
  Ã¢â€â€š      Ã¢â€¢Â±                      Ã¢â€¢Â²
  Ã¢â€â€š     Ã¢Å â€¢                        Ã¢â€¢Â²
  Ã¢â€â€š    Ã¢â€¢Â±                           Ã¢Å â€¢ Ã¢â€ Â bounce
  Ã¢â€â€š   Ã¢â€¢Â±                          Ã¢â€¢Â±  Ã¢â€¢Â²
  Ã¢â€â€šÃ¢â€â‚¬Ã¢â€â‚¬Ã¢Å â€¢Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢Å â€¢Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢Å â€¢Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ ground (z = 0.11m, ball center)
  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº X
  
  
  AXIS CONVENTIONS:
  X = pitch length (0-105m), goal-to-goal
  Y = pitch width (0-68m), touchline-to-touchline
  Z = height (0m = ground surface, ball center rests at 0.11m)
  
  Spin axes:
  Ãâ€°_x > 0 = rotation about X axis (topspin for +Y movement)
  Ãâ€°_y > 0 = rotation about Y axis (backspin for +X movement)  
  Ãâ€°_z > 0 = CCW from above (ball curves LEFT for +X movement)
  Ãâ€°_z < 0 = CW from above (ball curves RIGHT for +X movement)
```

---

### C.6 Drag Crisis Visualization

```
  C_d
  0.25 Ã¢â€â€š
       Ã¢â€â€š
  0.20 Ã¢â€â€šÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€â€œ
       Ã¢â€â€š                     Ã¢â€Æ’ Ã¢â€ Â Drag crisis transition
  0.15 Ã¢â€â€š                     Ã¢â€Æ’      (linear interpolation)
       Ã¢â€â€š                     Ã¢â€Æ’
  0.10 Ã¢â€â€š                     Ã¢â€â€”Ã¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€Â
       Ã¢â€â€š
  0.05 Ã¢â€â€š
       Ã¢â€â€š
  0.00 Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
               10      15    20      25      30     Speed (m/s)
                              Ã¢â€â€š       Ã¢â€â€š
                         CRISIS_LOW  CRISIS_HIGH
                          (20 m/s)   (25 m/s)

  
  Drag FORCE (shows the "dip"):
  
  F_drag (N)
  3.0 Ã¢â€â€š                                          Ã¢â€¢Â±
      Ã¢â€â€š                                        Ã¢â€¢Â±
  2.5 Ã¢â€â€š                                      Ã¢â€¢Â±
      Ã¢â€â€š                                    Ã¢â€¢Â±
  2.0 Ã¢â€â€š                        Ã¢â€¢Â±Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â²      Ã¢â€¢Â±
      Ã¢â€â€š                      Ã¢â€¢Â±     Ã¢â€¢Â²   Ã¢â€¢Â±
  1.5 Ã¢â€â€š                    Ã¢â€¢Â±        Ã¢â€¢Â²Ã¢â€¢Â±  Ã¢â€ Â KNUCKLEBALL WINDOW
      Ã¢â€â€š                  Ã¢â€¢Â±               Drag dips here
  1.0 Ã¢â€â€š               Ã¢â€¢Â±
      Ã¢â€â€š            Ã¢â€¢Â±
  0.5 Ã¢â€â€š         Ã¢â€¢Â±
      Ã¢â€â€š      Ã¢â€¢Â±
  0.0 Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â±Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
          5     10     15    20    25    30    Speed (m/s)
```

---

### C.7 Physics Update Loop Flowchart

```
  UpdateBallPhysics(ball, dt, surface, wind)
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Save last valid state
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Is state BOUNCING?
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ YES Ã¢â€ â€™ ApplyBounce() Ã¢â€ â€™ continue (don't return)
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ NO  Ã¢â€ â€™ continue
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Calculate forces by state:
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ AIRBORNE Ã¢â€ â€™ gravity + drag + Magnus
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ ROLLING  Ã¢â€ â€™ drag + rolling friction
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ BOUNCING Ã¢â€ â€™ drag only (bounce already applied)
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ OTHER    Ã¢â€ â€™ return (no physics)
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Semi-implicit Euler integration:
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ v += (F_net / m) Ãƒâ€” dt
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ x += v Ãƒâ€” dt    (uses NEW velocity)
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Update spin (AIRBORNE only):
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ Ãâ€° = UpdateSpinDecay(Ãâ€°, v, dt)
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Validate state (ALWAYS):
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ NaN/Infinity check Ã¢â€ â€™ recover if detected
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ Velocity clamp (50 m/s max)
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ Spin clamp (80 rad/s max)
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ Height clamp (50m max)
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ Ground penetration fix (z Ã¢â€°Â¥ RADIUS)
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ Position bounds (pitch + 20m buffer)
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Update state machine (ALWAYS):
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ Evaluate transitions with hysteresis
  Ã¢â€â€š
  Ã¢â€â€Ã¢â€â‚¬ Log snapshot (ALWAYS):
      Ã¢â€â€Ã¢â€â‚¬ TryLogSnapshot() at configured interval
```

---

### C.8 Spin Effect Visualization

```
  TOP VIEW - Sidespin effects on ball moving in +X direction:
  
  Ãâ€°_z > 0 (CCW from above):        Ãâ€°_z < 0 (CW from above):
  
       Ã¢â€¢Â­Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ball path                 Ball path Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â®
      Ã¢â€¢Â±                                               Ã¢â€¢Â²
     Ã¢â€¢Â±   F_magnus Ã¢â€ Â (Ã¢Ë†â€™Y)                F_magnus Ã¢â€ â€™ (+Y) Ã¢â€¢Â²
    Ã¢â€¢Â±                                                      Ã¢â€¢Â²
   Ã¢Å â€¢ Start                              Start Ã¢Å â€¢
   
   Ball curves LEFT                     Ball curves RIGHT


  SIDE VIEW - Topspin vs Backspin on ball moving in +X direction:
  
  Topspin (Ãâ€°_y < 0):                Backspin (Ãâ€°_y > 0):
  
  ZÃ¢â€â€š    Ã¢â€¢Â­Ã¢â€¢Â®                           ZÃ¢â€â€š
   Ã¢â€â€š   Ã¢â€¢Â±  Ã¢â€¢Â² Ã¢â€ Â dips earlier            Ã¢â€â€š   Ã¢â€¢Â­Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â® Ã¢â€ Â floats longer
   Ã¢â€â€š  Ã¢â€¢Â±    Ã¢â€¢Â²                           Ã¢â€â€š  Ã¢â€¢Â±          Ã¢â€¢Â²
   Ã¢â€â€š Ã¢â€¢Â±      Ã¢â€¢Â²                          Ã¢â€â€š Ã¢â€¢Â±            Ã¢â€¢Â²
   Ã¢â€â€šÃ¢â€¢Â±        Ã¢â€¢Â²                         Ã¢â€â€šÃ¢â€¢Â±              Ã¢â€¢Â²
   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â²Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº X                   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â²Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº X
   
   Steeper descent,                    Flatter arc,
   ball "dips"                         ball "hangs"
```

---

**END OF APPENDICES**

---

## Document Status

**Appendices Completion:**
- Ã¢Å“â€¦ Appendix A: Formula derivations for all 7 physics systems (Magnus, Drag, Bounce, Rolling, Spin, Integration, Goal Post)
- Ã¢Å“â€¦ Appendix B: 7 reference data tables with numerically verified values
- Ã¢Å“â€¦ Appendix C: 8 visual diagrams covering state machine, forces, coordinates, and spin effects
- Ã¢Å“â€¦ All derivations trace back to Section 3.1 v2.3 formulas
- Ã¢Å“â€¦ Magnus force and drag force values independently computed and verified
- Ã¢Å“â€¦ Trajectory data generated from 60Hz numerical simulation (not analytical estimates)
- Ã¢Å“â€¦ Rolling distance data generated from 60Hz numerical simulation
- Ã¢Å“â€¦ No fabricated or unverified data
- Ã¢Å“â€¦ Tanh model comparison table explicitly marked as estimated with absent k parameter noted

**Flagged issues for resolution before Spec #1 approval:**
- âœ… Section 3.1.14 rolling distance test case: RESOLVED in v1.2. Î¼_r updated to 0.13 for GRASS_DRY; test case expected range updated to 26â€“31m; B.4 simulation confirms 28.3m. See REV-001.
- Ã¢Å¡Â Ã¯Â¸Â Appendix A.1.2 tanh comparison uses estimated curve parameters Ã¢â‚¬â€ acceptable for justifying the simplification but not authoritative for the literature model

**Known limitations:**
- Appendix C diagrams are text-based Ã¢â‚¬â€ proper vector graphics deferred to Stage 1

**Page Count:** ~22 pages  
**Status:** DRAFT v1.1 Ã¢â‚¬â€ Revision pass complete

**With these appendices, Ball Physics Specification #1 now has all template-required sections complete:**
- Sections 1-2 (Purpose, Requirements) v1.1
- Section 3.1 (Core Formulas) v2.3
- Section 4 (Implementation) v1.1
- Section 5 (Testing) v1.0
- Section 6 (Performance Analysis) v1.0
- Section 7 (Future Extensions) v1.0
- Section 8 (References) v1.2
- Appendices A, B, C v1.1 Ã¢â€ Â this document
