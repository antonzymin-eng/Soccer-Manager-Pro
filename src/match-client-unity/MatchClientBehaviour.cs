// File:     src/match-client-unity/MatchClientBehaviour.cs
// Created:  2026-08-15
// Modified: 2026-08-15 (AR pass round 2 — Medium/Low findings M1-M9, L1-L5; see the VersionHistory
//           block at the foot of this file for the per-finding detail)
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4b, §12),
//           Code Standards #20
// Purpose:  The Unity host for a live match (P4b). Owns a MatchSession, reads frames each Update,
//           and binds them onto scene objects. Every render/camera/click decision is already made in
//           match-client-core (P4a) — this type assigns transforms and forwards input, nothing more.

using System;
using System.Globalization;

using UnityEngine;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>
    /// Binds a live <see cref="MatchSession"/> onto scene objects. Contains no game decision — every
    /// value it assigns was already computed in <c>match-client-core</c> (§12 rule 1: the CI gate
    /// cannot compile this type, so nothing that needs testing may live here).
    ///
    /// <para><b>The prefab contract, which this file is the only statement of.</b> Every prefab slot
    /// below MUST be authored so that:</para>
    /// <list type="number">
    /// <item><description><b>the ROOT transform is neutral</b> — identity local rotation, unit local
    /// scale. This code assigns root rotation and scale outright, so a tilt or a size baked onto the
    /// root is either destroyed or silently multiplied into every metre figure. Bake a fixed tilt (a
    /// Quad lying flat, say) and any visual thickness onto a CHILD mesh, which nothing here
    /// touches;</description></item>
    /// <item><description><b>the mesh is unit-sized</b> — unit radius for anything round (marker,
    /// ring, ball, shadow, circle, spot), unit length along local +Z with unit cross-section for a
    /// marking line. The scale this code assigns is then the metre figure ITSELF, with no conversion
    /// — which is the whole point: a per-primitive "what radius is a Unity Cylinder by default"
    /// constant is a class of bug rather than a number.</description></item>
    /// </list>
    ///
    /// <para><b>Not supported: a world-space <c>LineRenderer</c>.</b> Its positions are absolute, so
    /// the transform this binding positions and scales is ignored entirely and the shape renders
    /// wherever it was authored, forever — a total no-op that looks like a placement bug. Author
    /// markings as meshes, or as a <c>LineRenderer</c> with <c>useWorldSpace = false</c> and
    /// unit-radius/unit-length local points. Rejected at instantiation rather than left as prose,
    /// since nothing else here can see it.</para>
    ///
    /// <para><b>M8 — Active Input Handling.</b> <see cref="HandleClick"/> uses the legacy
    /// <c>UnityEngine.Input</c> API. Project Settings → Player → Active Input Handling MUST be
    /// "Input Manager (Old)" or "Both" — under "Input System Package (New)" ONLY, every call in this
    /// file to <c>Input.GetMouseButtonDown</c>/<c>Input.mousePosition</c> throws every frame. This is
    /// a project-setup requirement this file cannot enforce or detect; it is recorded here because
    /// nothing else states it (flagged for the Editor-setup instructions, which this file does not
    /// own).</para>
    /// </summary>
    public sealed class MatchClientBehaviour : MonoBehaviour
    {
        [Header("Prefabs — neutral root, unit-sized mesh (see the type doc for the full contract)")]
        [SerializeField] private GameObject _agentMarkerPrefab;
        [SerializeField] private GameObject _possessionRingPrefab;
        [SerializeField] private GameObject _ballPrefab;
        [SerializeField] private GameObject _ballShadowPrefab;
        [SerializeField] private GameObject _markingLinePrefab;
        [SerializeField] private GameObject _markingCirclePrefab;
        [SerializeField] private GameObject _markingSpotPrefab;

        [Header("Team palette — index 0 = home, 1 = away")]
        [SerializeField] private Color[] _teamColors = new Color[] { Color.blue, Color.red };

        [Header("Camera")]
        [SerializeField] private Camera _matchCamera;

        [Header("Demo boot (until a real squad source is wired)")]
        [SerializeField] private string _demoSeedText = "1";

        // The colour property block shares this one cached shader-property id (M4) rather than
        // looking it up by string every frame for every agent.
        private static readonly int s_colorPropertyId = Shader.PropertyToID("_Color");

        private MatchSession _session;
        private MatchRoster _roster;

        private GameObject[] _agentMarkers;
        private MeshRenderer[] _agentMarkerRenderers;
        private GameObject[] _possessionRings;
        private GameObject _ball;
        private GameObject _ballShadow;

        private MaterialPropertyBlock _scratchPropertyBlock;
        private Vector2[] _scratchAgentPositions;
        private AgentRenderModel[] _agentRenderModels;

        private bool _wiringRejected;

        // The previous/current frame decision (§12 rule 1: a state machine, so it lives in
        // match-client-core where the gate compiles and tests it — AR pass M-6).
        private readonly LiveFrameLatch _frameLatch = new LiveFrameLatch();
        private Vector2 _cameraTarget;

        /// <summary>
        /// Parses <see cref="_demoSeedText"/> into the <c>ulong</c> <c>MatchSetup.NeutralDemo</c>
        /// needs. A <c>ulong</c> <c>[SerializeField]</c>'s round-trip through Unity's
        /// <c>SerializedProperty</c> for values above <c>long.MaxValue</c> is unverified in this
        /// environment (L5) — a string inspector field sidesteps the question entirely, at the cost
        /// of a fail-loud parse here instead of the type system doing it for free.
        /// </summary>
        private ulong DemoSeed =>
            ulong.TryParse(_demoSeedText, out ulong seed)
                ? seed
                : throw new InvalidOperationException(
                    "MatchClientBehaviour: _demoSeedText \"" + _demoSeedText + "\" is not a valid ulong.");

        private void Awake()
        {
            // M1: everything that must hold before ANY prefab is instantiated — a null inspector
            // reference would NullReferenceException inside Instantiate itself, and a mis-sized team
            // palette or a scaled parent transform would silently misrender every marker rather than
            // fail loud at the point of the mistake. Guards every step below it: a rejected client
            // must not go on to construct a session or build a scene.
            ValidateWiring();
            if (_wiringRejected)
            {
                return;
            }

            _session = new MatchSession(MatchSetup.NeutralDemo(DemoSeed));
            _roster = MatchRoster.FromStreamer(_session.Streamer);

            _scratchAgentPositions = new Vector2[_roster.AgentCount];
            _agentRenderModels = new AgentRenderModel[_roster.AgentCount];
            _scratchPropertyBlock = new MaterialPropertyBlock();

            // L1: seeded to the pitch CENTRE, not the corner-origin frame's (0, 0) — leaving the
            // implicit Vector2.zero default put the camera's first target at a pitch corner, so every
            // match opened with a ~1 s slide-in before FollowBallCamera caught up to the kickoff spot.
            _cameraTarget = new Vector2(PitchViewProjection.HalfLengthM, PitchViewProjection.HalfWidthM);

            // Everything the PREFAB CONTENTS must satisfy is checked as each is instantiated, in
            // InstantiatePrefab below — the one place a further per-instantiation wiring check
            // belongs (H-2's own comment). ValidateWiring above covers what has to be true before any
            // Instantiate call is even safe to make.
            BuildMarkings();
            BuildAgentObjects();
            BuildBallObjects();
        }

        private void Start()
        {
            // Disabling a component during Awake defers Start rather than cancelling it, so the
            // rejection has to be re-checked here: a rejected client must never start a match.
            if (_wiringRejected)
            {
                return;
            }

            _session.Start();
        }

        private void Update()
        {
            if (_wiringRejected)
            {
                return;
            }

            AdvanceFrame();

            if (!_frameLatch.HasFrame)
            {
                return;
            }

            float alpha = FrameInterpolator.ComputeAlpha(
                _frameLatch.Previous.Tick,
                _frameLatch.Current.Tick,
                _frameLatch.SecondsSinceCurrent(Time.time),
                _session.Streamer.EffectiveTicksPerSecond);

            // Computed once and reused by both RenderBall and UpdateCamera (L2) — the two used to
            // call FrameInterpolator.BallAt at DIFFERENT alphas (this one, and a hardcoded 1f under a
            // comment that wrongly called the result "the interpolated frame"), which was both a
            // redundant call and a silently stale camera target on every render frame that was not
            // fully caught up to the newest tick.
            Vector3 pitchBallPosition = FrameInterpolator.BallAt(_frameLatch.Previous, _frameLatch.Current, alpha);

            RenderAgents(alpha);
            RenderBall(pitchBallPosition);
            UpdateCamera(pitchBallPosition);
            HandleClick();
        }

        private void OnDestroy()
        {
            _session?.Stop();
        }

        // ---- wiring validation (Awake, before anything is instantiated) -----------------------

        /// <summary>
        /// M1: the wiring checks that must hold before ANY prefab is instantiated. A null inspector
        /// reference would throw inside <c>Instantiate</c> itself; a team palette shorter than
        /// <see cref="MatchEngineConstants.TEAM_COUNT"/> would index-out-of-range the first time an
        /// away-team agent is drawn; and a non-identity scale on this GameObject's own transform would
        /// silently double-scale every metre figure <see cref="PlaceLine"/>/<see cref="PlaceRadial"/>
        /// assign, since both mix a world POSITION with a LOCAL scale. Reuses <see cref="RejectWiring"/>
        /// rather than duplicating its log-and-disable behaviour; <see cref="InstantiatePrefab"/> below
        /// is the complementary check that runs once a prefab HAS been instantiated (root neutrality,
        /// no world-space <c>LineRenderer</c>).
        /// </summary>
        private void ValidateWiring()
        {
            if (_agentMarkerPrefab == null) { RejectWiring(nameof(_agentMarkerPrefab) + " is not assigned in the inspector."); return; }
            if (_possessionRingPrefab == null) { RejectWiring(nameof(_possessionRingPrefab) + " is not assigned in the inspector."); return; }
            if (_ballPrefab == null) { RejectWiring(nameof(_ballPrefab) + " is not assigned in the inspector."); return; }
            if (_ballShadowPrefab == null) { RejectWiring(nameof(_ballShadowPrefab) + " is not assigned in the inspector."); return; }
            if (_markingLinePrefab == null) { RejectWiring(nameof(_markingLinePrefab) + " is not assigned in the inspector."); return; }
            if (_markingCirclePrefab == null) { RejectWiring(nameof(_markingCirclePrefab) + " is not assigned in the inspector."); return; }
            if (_markingSpotPrefab == null) { RejectWiring(nameof(_markingSpotPrefab) + " is not assigned in the inspector."); return; }
            if (_matchCamera == null) { RejectWiring(nameof(_matchCamera) + " is not assigned in the inspector."); return; }

            if (_teamColors == null || _teamColors.Length != MatchEngineConstants.TEAM_COUNT)
            {
                RejectWiring(
                    nameof(_teamColors) + " must have exactly " + Inv(MatchEngineConstants.TEAM_COUNT) +
                    " entries (index 0 = home, 1 = away) — it is " +
                    (_teamColors == null ? "null" : Inv(_teamColors.Length)) +
                    "; RenderAgents indexes it by AgentRenderModel.TeamId unguarded.");
                return;
            }

            if (transform.lossyScale != Vector3.one)
            {
                RejectWiring(
                    "the MatchClient GameObject's own transform must be at identity scale — PlaceLine/" +
                    "PlaceRadial mix a world POSITION with a LOCAL scale, so a scaled parent silently " +
                    "double-scales every metre figure on the pitch. transform.lossyScale is " +
                    transform.lossyScale + ".");
                return;
            }
        }

        // ---- frame plumbing -------------------------------------------------------------------

        private void AdvanceFrame()
        {
            if (!_session.TryGetLatestFrame(out LiveMatchFrame frame))
            {
                return;
            }

            _frameLatch.TryAccept(in frame, Time.time);
        }

        // ---- scene construction (Awake, once) --------------------------------------------------

        private void BuildMarkings()
        {
            Transform parent = new GameObject("Markings").transform;
            parent.SetParent(transform, false);

            // BuildDrawables, not Build: match-client-core has already decomposed each rectangle into
            // the four lines that close it, so every entry here is one primitive and this file
            // synthesises no corner of its own (§12 rule 1).
            foreach (PitchMarking marking in PitchMarkings.BuildDrawables())
            {
                switch (marking.Kind)
                {
                    case PitchMarkingKind.Line:
                    case PitchMarkingKind.GoalMouth:
                        PlaceLine(parent, marking.A, marking.B);
                        break;

                    case PitchMarkingKind.Circle:
                        PlaceRadial(parent, _markingCirclePrefab, nameof(_markingCirclePrefab), marking.A, marking.Radius);
                        break;

                    case PitchMarkingKind.Spot:
                        PlaceRadial(parent, _markingSpotPrefab, nameof(_markingSpotPrefab), marking.A, marking.Radius);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(marking.Kind), marking.Kind,
                            "unhandled PitchMarkingKind — note BuildDrawables emits no Rectangle, so " +
                            "one arriving here means the decomposition regressed");
                }
            }
        }

        private void PlaceLine(Transform parent, Vector2 fromXY, Vector2 toXY)
        {
            Vector3 from = PitchViewProjection.ToWorld(fromXY, 0f);
            Vector3 to = PitchViewProjection.ToWorld(toXY, 0f);
            Vector3 along = to - from;

            GameObject go = InstantiatePrefab(_markingLinePrefab, parent, nameof(_markingLinePrefab));
            go.transform.position = (from + to) * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(along, Vector3.up);

            // Unit length along local +Z and unit cross-section, so both figures are metres as they
            // stand. Y stays at 1: a line painted on the turf has no thickness of its own.
            Vector3 scale = Vector3.one;
            scale.x = MatchClientConstants.MarkingLineWidthM;
            scale.z = along.magnitude;
            go.transform.localScale = scale;
        }

        private void PlaceRadial(Transform parent, GameObject prefab, string prefabField, Vector2 centreXY, float radius)
        {
            GameObject go = InstantiatePrefab(prefab, parent, prefabField);
            go.transform.position = PitchViewProjection.ToWorld(centreXY, 0f);
            go.transform.localScale = GroundScale(radius);
        }

        private void BuildAgentObjects()
        {
            _agentMarkers = new GameObject[_roster.AgentCount];
            _agentMarkerRenderers = new MeshRenderer[_roster.AgentCount];
            _possessionRings = new GameObject[_roster.AgentCount];

            for (int i = 0; i < _roster.AgentCount; i++)
            {
                _agentMarkers[i] = InstantiatePrefab(_agentMarkerPrefab, transform, nameof(_agentMarkerPrefab));

                // M4: resolved ONCE here rather than every frame in RenderAgents — the walk was
                // re-run 22 times a frame for a value (which mesh draws the marker) that never
                // changes after construction, and Renderer.material (the property the old per-frame
                // read used) clones a Material instance on first access, so the old code leaked one
                // per marker per Play session on top of the redundant walk.
                MeshRenderer markerRenderer = _agentMarkers[i].GetComponentInChildren<MeshRenderer>();
                if (markerRenderer == null)
                {
                    RejectWiring(
                        nameof(_agentMarkerPrefab) + " agent index " + Inv(i) +
                        " has no MeshRenderer under its instantiated root, so no team colour / " +
                        "sent-off / goalkeeper tint can be applied to it.");
                }
                _agentMarkerRenderers[i] = markerRenderer;

                _possessionRings[i] = InstantiatePrefab(_possessionRingPrefab, transform, nameof(_possessionRingPrefab));
                _possessionRings[i].SetActive(false);
            }
        }

        private void BuildBallObjects()
        {
            _ball = InstantiatePrefab(_ballPrefab, transform, nameof(_ballPrefab));
            _ballShadow = InstantiatePrefab(_ballShadowPrefab, transform, nameof(_ballShadowPrefab));
        }

        /// <summary>
        /// Instantiates one prefab under <paramref name="parent"/> and rejects the client unless the
        /// instance honours the prefab contract in the type doc. This is the single gate every scene
        /// object passes through, so it is also where any further wiring check belongs.
        /// </summary>
        private GameObject InstantiatePrefab(GameObject prefab, Transform parent, string prefabField)
        {
            GameObject instance = Instantiate(prefab, parent);
            Transform root = instance.transform;

            if (root.localRotation != Quaternion.identity || root.localScale != Vector3.one)
            {
                RejectWiring(
                    prefabField + " must be authored with a NEUTRAL root — identity rotation, unit " +
                    "scale — and any fixed tilt or thickness baked onto a child mesh; this one has " +
                    "rotation " + root.localRotation.eulerAngles + " and scale " + root.localScale +
                    ", which this binding overwrites.");
            }

            LineRenderer line = instance.GetComponentInChildren<LineRenderer>();

            if (line != null && line.useWorldSpace)
            {
                RejectWiring(
                    prefabField + " draws with a world-space LineRenderer, which ignores the transform " +
                    "this binding positions and scales it by. Author it as a mesh, or set " +
                    "useWorldSpace = false with unit-radius / unit-length local points.");
            }

            return instance;
        }

        /// <summary>
        /// Scale for a flat, unit-radius prop lying on the turf: the radius in metres on both ground
        /// axes, unit height. Under the prefab contract no conversion is involved — which is exactly
        /// what a shared "default primitive radius" divisor used to hide.
        /// </summary>
        private static Vector3 GroundScale(float radiusM)
        {
            Vector3 scale = Vector3.one;
            scale.x = radiusM;
            scale.z = radiusM;
            return scale;
        }

        private void RejectWiring(string reason)
        {
            Debug.LogError("MatchClientBehaviour: " + reason + " Disabling the client.", this);
            _wiringRejected = true;
            enabled = false;
        }

        // ---- per-frame binding ------------------------------------------------------------------

        private void RenderAgents(float alpha)
        {
            FrameInterpolator.AgentsAt(_frameLatch.Previous, _frameLatch.Current, alpha, _scratchAgentPositions);

            // M7: ProjectAgents returns how many slots it actually wrote — _agentRenderModels may be
            // longer (it is sized once, in Awake, off the roster the streamer reports at boot) — so
            // the loop below walks the returned count, not the destination array's own length.
            int count = MatchRenderProjection.ProjectAgents(
                _scratchAgentPositions, _frameLatch.Current, _roster, _agentRenderModels);

            for (int i = 0; i < count; i++)
            {
                AgentRenderModel model = _agentRenderModels[i];

                // M2: ShirtNumber, YellowCards and IsSubstitute are DELIBERATELY DEFERRED here, not
                // silently dropped. This landing wires no label prefab — Packages/manifest.json
                // carries no com.unity.textmeshpro dependency — so there is nowhere on the existing
                // marker to draw a number or a card count. IsGoalkeeper and IsSentOff below ARE
                // bound: both fit the existing marker material as a tint, with no new prefab slot.
                Transform marker = _agentMarkers[i].transform;
                marker.position = model.WorldPosition;
                marker.localScale = GroundScale(model.MarkerRadius);

                _scratchPropertyBlock.SetColor(s_colorPropertyId, ResolveMarkerColor(in model));
                _agentMarkerRenderers[i].SetPropertyBlock(_scratchPropertyBlock);

                Transform ring = _possessionRings[i].transform;
                _possessionRings[i].SetActive(model.HasBall);
                if (model.HasBall)
                {
                    ring.position = model.WorldPosition;
                    // M3: reads AgentRenderModel.PossessionRingRadius (0 when !HasBall, otherwise the
                    // catalogue's [GT] radius) rather than the [GT] constant a second time — the
                    // model is the single source, per its own class doc.
                    ring.localScale = GroundScale(model.PossessionRingRadius);
                }
            }
        }

        /// <summary>
        /// The marker colour for one agent: its team colour, lightened for a goalkeeper and darkened
        /// for a sent-off player (M2) — both a shade of the colour <see cref="_teamColors"/> already
        /// assigns, rather than a new palette entry or a new prefab slot, since neither distinction
        /// is a hue anyone chose.
        /// </summary>
        private Color ResolveMarkerColor(in AgentRenderModel model)
        {
            Color color = _teamColors[model.TeamId];

            if (model.IsGoalkeeper)
            {
                color = Color.Lerp(color, Color.white, MatchClientConstants.GoalkeeperTintFactor);
            }

            if (model.IsSentOff)
            {
                color = Color.Lerp(color, Color.black, MatchClientConstants.SentOffTintFactor);
            }

            return color;
        }

        private void RenderBall(Vector3 pitchBallPosition)
        {
            BallRenderModel model = MatchRenderProjection.ProjectBall(pitchBallPosition);

            _ball.transform.position = model.WorldPosition;
            _ball.transform.localScale = Vector3.one * model.Radius;

            _ballShadow.transform.position = model.ShadowPosition;
            _ballShadow.transform.localScale = GroundScale(model.ShadowRadius);
        }

        private void UpdateCamera(Vector3 pitchBallPosition)
        {
            Vector2 ballPitchXY = new Vector2(pitchBallPosition.x, pitchBallPosition.y);

            _cameraTarget = FollowBallCamera.ComputeTarget(_cameraTarget, ballPitchXY, Time.deltaTime);

            PitchCameraPose pose = PitchCameraRig.ComputePose(_cameraTarget);
            _matchCamera.transform.position = pose.Position;
            _matchCamera.transform.LookAt(pose.LookAt);
            _matchCamera.fieldOfView = pose.FieldOfViewDegrees;
        }

        private void HandleClick()
        {
            // M8: requires Player Settings → Active Input Handling = "Input Manager (Old)" or
            // "Both" — see the type doc. Under "Input System Package (New)" only, both calls below
            // throw every frame.
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            Ray ray = _matchCamera.ScreenPointToRay(Input.mousePosition);

            if (PitchViewProjection.TryGroundHit(ray.origin, ray.direction, out Vector2 pitchXY))
            {
                // L3: TEMP diagnostic until P5b wires this into a ManagerCommand via
                // _session.Commands — makes the resolved ground point observable instead of a dead,
                // never-read local behind an empty branch.
                Debug.Log("MatchClientBehaviour: ground click at pitch " + pitchXY + ".", this);
            }
        }

        private static string Inv(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-15 | —      | Initial creation (P4b): binds MatchSession frames onto scene   |
// |         |            |        | objects — markings, agents, possession ring, ball/shadow,      |
// |         |            |        | camera pose, ground-click resolution. No decision logic; every |
// |         |            |        | value read off match-client-core's P4a render model.            |
// | 1.1     | 2026-08-15 | —      | AR pass H-1/H-2/H-3. H-1: markings come from                    |
// |         |            |        | PitchMarkings.BuildDrawables(), so the rectangle arm and its    |
// |         |            |        | four synthesised corners are gone — that geometry now lives     |
// |         |            |        | where the gate compiles and tests it. H-2: the prefab contract  |
// |         |            |        | the header asserted is now ENFORCED — every instantiation goes  |
// |         |            |        | through InstantiatePrefab, which rejects a non-neutral root     |
// |         |            |        | (a baked root tilt this code would overwrite, a baked scale it  |
// |         |            |        | would multiply by) and disables the client naming the field;    |
// |         |            |        | and a marking line's WIDTH is assigned from the new [GT]        |
// |         |            |        | MarkingLineWidthM instead of inherited from the prefab. H-3:    |
// |         |            |        | PrimitiveDefaultRadiusM (0.5, an untagged magic constant valid  |
// |         |            |        | only for Unity's own Sphere/Cylinder) and every division by it  |
// |         |            |        | are deleted in favour of a unit-radius / unit-length authoring  |
// |         |            |        | contract, under which the assigned scale IS the metre figure.   |
// |         |            |        | A world-space LineRenderer — for which transform placement is   |
// |         |            |        | a silent no-op — is documented as unsupported and rejected at   |
// |         |            |        | instantiation rather than mishandled.                           |
// | 1.2     | 2026-08-15 | —      | AR pass round 2, Medium/Low findings. M1: ValidateWiring() runs |
// |         |            |        | at the top of Awake — null-checks all 8 inspector refs by name, |
// |         |            |        | asserts _teamColors.Length == TEAM_COUNT, asserts this          |
// |         |            |        | GameObject's own transform is at identity scale (PlaceLine/     |
// |         |            |        | PlaceRadial mix world position with LOCAL scale, so a scaled    |
// |         |            |        | parent would double-scale every metre figure) — reuses          |
// |         |            |        | RejectWiring, Awake early-returns on rejection. M2: IsGoalkeeper |
// |         |            |        | and IsSentOff are now bound (a lighten/darken of the agent's own |
// |         |            |        | team colour via the two new [GT] tint factors); ShirtNumber /   |
// |         |            |        | YellowCards / IsSubstitute stay deliberately DEFERRED (commented |
// |         |            |        | at the read site) — no label prefab, no TextMeshPro dependency  |
// |         |            |        | in this landing. M3: the possession ring reads                  |
// |         |            |        | AgentRenderModel.PossessionRingRadius (confirmed to exist)       |
// |         |            |        | instead of the [GT] constant a second time. M4: the agent marker |
// |         |            |        | MeshRenderer is resolved ONCE per agent in BuildAgentObjects     |
// |         |            |        | (fail-loud via RejectWiring if absent) instead of                |
// |         |            |        | GetComponentInChildren every frame, and coloured via a reused    |
// |         |            |        | MaterialPropertyBlock instead of the leaking .material.color     |
// |         |            |        | setter. M5: the effective-tick-rate product moves onto           |
// |         |            |        | LiveMatchStreamer.EffectiveTicksPerSecond (match-viewer) — the   |
// |         |            |        | local DeterministicSimConstants.PHYSICS_TICK_HZ recomputation is |
// |         |            |        | deleted, and the now-unused TacticalDirector.DeterministicSim    |
// |         |            |        | asmdef reference is reverted. M6: the previous/current frame     |
// |         |            |        | latch state machine moves into match-client-core's new           |
// |         |            |        | LiveFrameLatch (§12 rule 1) — AdvanceFrame is now three lines.   |
// |         |            |        | M7: RenderAgents walks the count MatchRenderProjection.          |
// |         |            |        | ProjectAgents returns, not _agentRenderModels.Length. M8: the    |
// |         |            |        | type doc and HandleClick both now state the Active Input         |
// |         |            |        | Handling requirement (Input Manager (Old) or Both) HandleClick's |
// |         |            |        | legacy Input.* calls depend on. L1: _cameraTarget is seeded to   |
// |         |            |        | the pitch CENTRE in Awake instead of the implicit corner-origin  |
// |         |            |        | Vector2.zero default, removing the ~1 s kickoff slide-in. L2:    |
// |         |            |        | RenderBall/UpdateCamera now take the ball's pitch position as a  |
// |         |            |        | parameter, computed once in Update at the real alpha, instead of |
// |         |            |        | UpdateCamera calling FrameInterpolator.BallAt a second time at a |
// |         |            |        | hardcoded 1f under a comment that wrongly called the result "the |
// |         |            |        | interpolated frame". L3: HandleClick's dead ground-hit branch    |
// |         |            |        | now logs a TEMP diagnostic instead of discarding pitchXY behind  |
// |         |            |        | an empty block. L5: _demoSeed (ulong) becomes _demoSeedText      |
// |         |            |        | (string) + a fail-loud DemoSeed parse — a ulong SerializeField's |
// |         |            |        | round-trip through SerializedProperty above long.MaxValue is     |
// |         |            |        | unverified without a Unity host. M9 and L4 are Editor-instruction |
// |         |            |        | findings, not code — not applicable to this file; flagged for   |
// |         |            |        | the orchestrator to correct those instructions separately.       |
#endregion
