// File:     src/match-client-unity/MatchClientBehaviour.cs
// Created:  2026-08-15
// Modified: 2026-08-15
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4b, §12),
//           Code Standards #20
// Purpose:  The Unity host for a live match (P4b). Owns a MatchSession, reads frames each Update,
//           and binds them onto scene objects. Every render/camera/click decision is already made in
//           match-client-core (P4a) — this type assigns transforms and forwards input, nothing more.

using System;

using UnityEngine;

using TacticalDirector.MatchClientCore;
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
        [SerializeField] private ulong _demoSeed = 1;

        private MatchSession _session;
        private MatchRoster _roster;

        private GameObject[] _agentMarkers;
        private GameObject[] _possessionRings;
        private GameObject _ball;
        private GameObject _ballShadow;

        private Vector2[] _scratchAgentPositions;
        private AgentRenderModel[] _agentRenderModels;

        private bool _wiringRejected;

        private bool _hasFrame;
        private LiveMatchFrame _previousFrame;
        private LiveMatchFrame _currentFrame;
        private float _currentFrameArrivalTime;
        private Vector2 _cameraTarget;

        private void Awake()
        {
            _session = new MatchSession(MatchSetup.NeutralDemo(_demoSeed));
            _roster = MatchRoster.FromStreamer(_session.Streamer);

            _scratchAgentPositions = new Vector2[_roster.AgentCount];
            _agentRenderModels = new AgentRenderModel[_roster.AgentCount];

            // Everything the scene must satisfy is checked as it is instantiated, in
            // InstantiatePrefab below — the one place a further wiring check belongs — and a failure
            // rejects the whole client here, before Start() puts the match in motion.
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

            if (!_hasFrame)
            {
                return;
            }

            float effectiveTicksPerSecond =
                DeterministicSim.DeterministicSimConstants.PHYSICS_TICK_HZ *
                _session.Streamer.SpeedMultiplier;

            float alpha = FrameInterpolator.ComputeAlpha(
                _previousFrame.Tick,
                _currentFrame.Tick,
                Time.time - _currentFrameArrivalTime,
                effectiveTicksPerSecond);

            RenderAgents(alpha);
            RenderBall(alpha);
            UpdateCamera();
            HandleClick();
        }

        private void OnDestroy()
        {
            _session?.Stop();
        }

        // ---- frame plumbing -------------------------------------------------------------------

        private void AdvanceFrame()
        {
            if (!_session.TryGetLatestFrame(out LiveMatchFrame frame))
            {
                return;
            }

            if (!_hasFrame)
            {
                _previousFrame = frame;
                _currentFrame = frame;
                _hasFrame = true;
            }
            else if (frame.Tick != _currentFrame.Tick)
            {
                _previousFrame = _currentFrame;
                _currentFrame = frame;
            }
            else
            {
                return;
            }

            _currentFrameArrivalTime = Time.time;
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
            _possessionRings = new GameObject[_roster.AgentCount];

            for (int i = 0; i < _roster.AgentCount; i++)
            {
                _agentMarkers[i] = InstantiatePrefab(_agentMarkerPrefab, transform, nameof(_agentMarkerPrefab));
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
            FrameInterpolator.AgentsAt(_previousFrame, _currentFrame, alpha, _scratchAgentPositions);

            MatchRenderProjection.ProjectAgents(
                _scratchAgentPositions, in _currentFrame, _roster, _agentRenderModels);

            for (int i = 0; i < _agentRenderModels.Length; i++)
            {
                AgentRenderModel model = _agentRenderModels[i];

                Transform marker = _agentMarkers[i].transform;
                marker.position = model.WorldPosition;
                marker.localScale = GroundScale(model.MarkerRadius);

                MeshRenderer markerRenderer = _agentMarkers[i].GetComponentInChildren<MeshRenderer>();
                if (markerRenderer != null)
                {
                    markerRenderer.material.color = _teamColors[model.TeamId];
                }

                Transform ring = _possessionRings[i].transform;
                _possessionRings[i].SetActive(model.HasBall);
                if (model.HasBall)
                {
                    ring.position = model.WorldPosition;
                    ring.localScale = GroundScale(MatchClientConstants.PossessionRingRadiusM);
                }
            }
        }

        private void RenderBall(float alpha)
        {
            Vector3 pitchBallPosition = FrameInterpolator.BallAt(_previousFrame, _currentFrame, alpha);
            BallRenderModel model = MatchRenderProjection.ProjectBall(pitchBallPosition);

            _ball.transform.position = model.WorldPosition;
            _ball.transform.localScale = Vector3.one * model.Radius;

            _ballShadow.transform.position = model.ShadowPosition;
            _ballShadow.transform.localScale = GroundScale(model.ShadowRadius);
        }

        private void UpdateCamera()
        {
            // Pitch-space XY, read straight off the interpolated frame rather than back out of the
            // placed world object.
            Vector3 pitchBall = FrameInterpolator.BallAt(_previousFrame, _currentFrame, 1f);
            Vector2 ballPitchXY = new Vector2(pitchBall.x, pitchBall.y);

            _cameraTarget = FollowBallCamera.ComputeTarget(_cameraTarget, ballPitchXY, Time.deltaTime);

            PitchCameraPose pose = PitchCameraRig.ComputePose(_cameraTarget);
            _matchCamera.transform.position = pose.Position;
            _matchCamera.transform.LookAt(pose.LookAt);
            _matchCamera.fieldOfView = pose.FieldOfViewDegrees;
        }

        private void HandleClick()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            Ray ray = _matchCamera.ScreenPointToRay(Input.mousePosition);

            if (PitchViewProjection.TryGroundHit(ray.origin, ray.direction, out Vector2 pitchXY))
            {
                // Ground point resolved. Not yet routed anywhere: P5b (the UGUI/tactical binding)
                // is what turns this into a ManagerCommand via _session.Commands.
            }
        }
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
#endregion
