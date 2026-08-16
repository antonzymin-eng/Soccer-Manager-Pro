# `TacticalDirector.MatchClientUnity` — Unity-only render/UGUI skin

**Status:** P4b (the render/camera/click binding) **LANDED** —
`MatchClientBehaviour.cs`, 9 commits across 4 AR rounds (Medium/Low findings
M1-M22, L1-L13, plus H1-H6). The UGUI shell (P5b) and the on-host half of P6
remain open. See `docs/tracking/interactive-unity-client-design.md` for the full
landing history.

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
naming the offending field, when it can — but the five items below are either
checked only once a prefab is instantiated, or cannot be checked from code at
all.

### 1. The prefab contract — 8 slots

Every `[SerializeField] GameObject` prefab slot on `MatchClientBehaviour` must
be authored to the same contract (the type doc's numbered clauses; enforced at
instantiation by `InstantiatePrefab`, `BuildAgentObjects`, and `ValidateWiring`
where noted):

| Slot | Sizing (type-doc clause) | Silhouette (M22) |
|---|---|---|
| Agent marker | 2a — FLAT, unit radius | Filled disc |
| Possession ring | 2a — FLAT, unit radius | **Stroked** — an outline/ring (annulus) drawn AROUND the marker it annotates, not a second disc under it |
| Ball | 2b — the one VOLUMETRIC prop, unit-radius sphere | Filled sphere |
| Ball shadow | 2a — FLAT, unit radius | Filled disc |
| Marking line | 2a — FLAT, unit length along local +Z, unit cross-section | Filled rectangle |
| Marking circle | 2a — FLAT, unit radius | **Stroked** — an outline, per `PitchMarkingKind.Circle`'s own doc: "distinct from Spot because it is filled rather than stroked, and a renderer that collapsed the two would draw a solid centre circle" |
| Marking spot | 2a — FLAT, unit radius | Filled disc — `PitchMarkingKind.Spot`'s own doc |
| Goal mouth | 2a — FLAT, unit length along local +Z, unit cross-section (M18: shares the marking line's authoring, not its prefab or width — see `GoalMouthWidthM` in `MatchClientConstants.cs`) | Filled rectangle |

M22: the marking circle and the possession ring are the only two STROKED slots — every other slot in
the table (round or straight) is FILLED. `MatchClientBehaviour` cannot check the difference any more
than it can check that a FLAT mesh has zero height (§1 above): authoring either stroked slot as a
solid disc renders a filled blob where an outline was required, with no diagnostic short of eyeballing
the rendered pitch. Author the two stroked slots as a ring/annulus mesh, or a hollow torus lying flat.

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
- **Identity world rotation** (M15; checked via `IsIdentityRotation` since M20,
  not a plain `== Quaternion.identity` — a quaternion and its negation encode
  the SAME rotation, so a naive equality check falsely rejects a legitimately-
  unrotated transform whose composed rotation happens to land on the negative
  representative of identity) — `Instantiate(prefab, parent)` resets a child's
  LOCAL rotation from the prefab (identity, per the neutral-root contract) but
  still inherits the parent's WORLD rotation. `PlaceLine` assigns a world
  rotation explicitly and is immune; `PlaceRadial`, `RenderAgents` and
  `RenderBall` assign only world position and local scale, so a rotated
  ancestor would silently tilt every circle, spot, marker, ring, ball and
  shadow while the lines alone stayed flat — with no diagnostic short of this
  check.

Put the `MatchClientBehaviour` component on a GameObject with no scale or
rotation applied anywhere in its ancestry, at the scene root if in doubt.

### 4. Team-colour palette — 2 entries

`_teamColors` must have exactly `MatchEngineConstants.TEAM_COUNT` (2) entries:
**index 0 = home, index 1 = away.** `RenderAgents` indexes it by
`AgentRenderModel.TeamId` unguarded past `ValidateWiring`'s length check, so a
mis-sized array is rejected at boot rather than index-out-of-ranging the first
time an away-team agent is drawn.

### 5. The pitch/ground surface (L13) — not a prefab slot, but still this contract's

Every M12/M16 ground-layer height in `MatchClientConstants.cs` is defined
**relative to the turf**, and `MarkingLayerHeightM` (the lowest of the four, and
the base the M16 marking BAND is built on) defaults to `0`. Nothing in the
prefab contract above, and no `[SerializeField]` slot on `MatchClientBehaviour`,
places the pitch/ground surface itself — that is scene-authoring the same way
the camera and lighting are, and this binding has no opinion on how it is
built (a single Plane/Quad, a tiled mesh, a terrain). But its placement is NOT
free of this contract: the M12/M16 scheme only stops the FOUR GROUND LAYERS
(and, within markings, the BAND) from z-fighting EACH OTHER — it says nothing
about the turf itself, the one ground object underneath all of them that has
no prefab slot to carry the requirement.

**Requirement: the pitch surface's top face must sit strictly BELOW
`MarkingLayerHeightM`, not at or above it — by a comfortable clearance, the
same M12/M16 reasoning applied one layer further down.** At the shipped
defaults `MarkingLayerHeightM` is `0`, and the LOWEST drawable in the M16
marking band (`BuildDrawables()` index `0`) sits at exactly
`MarkingLayerHeightM` — i.e. also `0`. A ground surface authored at the
"obvious" world `Y = 0` is therefore coplanar with that first marking, not
merely "under the markings" as the loose English might suggest — the exact
M12 class of z-fighting bug, on the one surface this contract does not
otherwise own. Give it clearance on the same millimetre-to-centimetre scale
the four ground layers themselves use (e.g. a top face at `Y = -0.01` or
lower is comfortably clear of every default in `MatchClientConstants.cs`
today) rather than `Y = 0`.

**Extent:** the pitch is `MatchEngineConstants.PITCH_LENGTH_M` × `PITCH_WIDTH_M`
(105 m × 68 m, `[FIXED]`, IFAB), and `PitchViewProjection.ToWorld` centres it on
the world origin — world X spans `[-52.5, 52.5]`, world Z spans `[-34, 34]`. A
ground surface narrower than that clips at the touchline/goal line before the
camera's `CameraOverscanM` margin does; a generous overshoot (the standard
run-off area a broadcast pitch model already has) costs nothing and avoids the
edge being visible at the tilted camera's default overscan.
