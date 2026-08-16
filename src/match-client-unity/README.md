# `TacticalDirector.MatchClientUnity` — Unity-only render/UGUI skin

**Status:** P4b (the render/camera/click binding) **LANDED** —
`MatchClientBehaviour.cs`, three commits, two AR rounds (Medium/Low findings
M10-M18, L6-L9). The UGUI shell (P5b) and the on-host half of P6 remain open. See
`docs/tracking/interactive-unity-client-design.md` for the full landing history.

This is the **Unity-host** half of the interactive Unity client — the
`MonoBehaviour` that owns a `MatchSession`, reads frames each `Update`, and binds
them onto scene objects. It is deliberately thin: every render/camera/click
*decision* is already made in the host-free sibling `src/match-client-core/`
(`TacticalDirector.MatchClientCore`), which the `tools/dotnet-ci` shim gate
compiles and tests on every push. This assembly only ever depends on that core
plus the reused `match-viewer` streamer; it adds a skin, never new engine-facing
logic (§12 rule 1 — see `docs/tracking/interactive-unity-client-design.md`).

## Excluded from the shim gate — code here has never compiled

`tools/dotnet-ci/generate_projects.py` compiles every `src/**/*.asmdef` against a
~9-type UnityEngine shim with **no** rendering types. This assembly is therefore
listed in that generator's `SHIM_EXCLUDED_ASMDEFS` set — it is never generated,
compiled, or referenced by the Linux gate. It is verified only on the pinned
Unity host at a cert run, which has not yet happened for `MatchClientBehaviour.cs`.
Every AR round over it has been reviewed by hand.

## Editor setup — this is the document `MatchClientBehaviour.cs` defers to

`MatchClientBehaviour.cs`'s own type doc states the prefab contract and defers
project-setting requirements here, since a `MonoBehaviour` cannot enforce a
Project Settings value or fail a Unity install that lacks it. This section is
that document. `ValidateWiring()` enforces everything it can detect at runtime
(null references, array lengths, transform scale/rotation) and fails loud,
naming the offending field, when it can — but the four items below are either
checked only once a prefab is instantiated, or cannot be checked from code at
all.

### 1. The prefab contract — 8 slots

Every `[SerializeField] GameObject` prefab slot on `MatchClientBehaviour` must
be authored to the same contract (the type doc's numbered clauses; enforced at
instantiation by `InstantiatePrefab`, `BuildAgentObjects`, and `ValidateWiring`
where noted):

| Slot | Sizing (type-doc clause) |
|---|---|
| Agent marker | 2a — FLAT, unit radius |
| Possession ring | 2a — FLAT, unit radius |
| Ball | 2b — the one VOLUMETRIC prop, unit-radius sphere |
| Ball shadow | 2a — FLAT, unit radius |
| Marking line | 2a — FLAT, unit length along local +Z, unit cross-section |
| Marking circle | 2a — FLAT, unit radius |
| Marking spot | 2a — FLAT, unit radius |
| Goal mouth | 2a — FLAT, unit length along local +Z, unit cross-section (M18: shares the marking line's authoring, not its prefab or width — see `GoalMouthWidthM` in `MatchClientConstants.cs`) |

Every slot, whichever clause it follows:

- **Neutral root** (clause 1) — identity local rotation, unit local scale. This
  binding assigns root rotation and scale outright (world position always;
  local scale for a 2a prop, world rotation for a marking line/goal mouth), so
  a baked tilt or size is either destroyed or silently multiplied into every
  metre figure. Bake a fixed tilt or thickness onto a CHILD mesh instead —
  `InstantiatePrefab` rejects a non-neutral root and names the field.
- **A FLAT (2a) mesh has ZERO extent in local Y** — the mesh itself must be
  flat (a Quad lying flat, not a Cylinder). This binding's Y-scale assignment
  is inert against a zero-height mesh, not the source of the flatness; a
  genuinely-3D mesh (a real sphere used as a marker, say) renders as a
  squashed ellipsoid, undetectable from code.
- **No world-space `LineRenderer`** — its positions are absolute, so this
  binding's transform placement is a total no-op. Author markings as meshes,
  or as a `LineRenderer` with `useWorldSpace = false` and unit-radius/unit-length
  local points. `InstantiatePrefab` rejects a world-space one.
- **The agent marker's material must expose the colour property named by the
  `_colorPropertyName` inspector field** (clause 3; default `"_Color"`, the
  Built-in Render Pipeline standard shader's name — URP's Lit/SimpleLit/Unlit
  shaders expose `"_BaseColor"` instead, and this repo's `GraphicsSettings.asset`
  / `Packages/manifest.json` do not agree on which pipeline resolves). Checked
  per marker in `BuildAgentObjects`, since only an instantiated prefab's
  material can answer this — `SetColor` against a missing property succeeds
  and changes nothing, so a mismatch would otherwise render both teams, the
  goalkeeper tint and the sent-off tint in one undifferentiated colour with no
  diagnostic anywhere.

### 2. Active Input Handling

`HandleClick` uses the legacy `UnityEngine.Input` API. **Project Settings →
Player → Active Input Handling MUST be "Input Manager (Old)" or "Both".** Under
"Input System Package (New)" only, every call in `MatchClientBehaviour.cs` to
`Input.GetMouseButtonDown` / `Input.mousePosition` throws every frame. This is a
project-setup requirement the binding cannot enforce or detect from code.

### 3. The host GameObject's own transform must be at identity scale AND rotation

`ValidateWiring()` checks both and rejects the client, naming the actual value,
if either fails — but only for the `MatchClient` GameObject and its own
`transform`; it cannot see the scene hierarchy ahead of time, so set this up
correctly rather than relying on the runtime check alone:

- **Identity scale** (`transform.lossyScale == Vector3.one`) — `PlaceLine` /
  `PlaceRadial` mix a world POSITION with a LOCAL scale, so a scaled ancestor
  silently double-scales every metre figure on the pitch.
- **Identity world rotation** (`transform.rotation == Quaternion.identity`,
  M15) — `Instantiate(prefab, parent)` resets a child's LOCAL rotation from the
  prefab (identity, per the neutral-root contract) but still inherits the
  parent's WORLD rotation. `PlaceLine` assigns a world rotation explicitly and
  is immune; `PlaceRadial`, `RenderAgents` and `RenderBall` assign only world
  position and local scale, so a rotated ancestor would silently tilt every
  circle, spot, marker, ring, ball and shadow while the lines alone stayed
  flat — with no diagnostic short of this check.

Put the `MatchClientBehaviour` component on a GameObject with no scale or
rotation applied anywhere in its ancestry, at the scene root if in doubt.

### 4. Team-colour palette — 2 entries

`_teamColors` must have exactly `MatchEngineConstants.TEAM_COUNT` (2) entries:
**index 0 = home, index 1 = away.** `RenderAgents` indexes it by
`AgentRenderModel.TeamId` unguarded past `ValidateWiring`'s length check, so a
mis-sized array is rejected at boot rather than index-out-of-ranging the first
time an away-team agent is drawn.
