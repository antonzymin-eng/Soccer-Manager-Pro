// File:     src/match-client-unity/MatchClientBehaviour.cs
// Created:  2026-08-15
// Modified: 2026-08-15
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4b),
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
    /// </summary>
    public sealed class MatchClientBehaviour : MonoBehaviour
    {
        [Header("Prefabs — root transform only; bake any fixed tilt onto a child mesh")]
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

        // Unity primitive defaults: a Cylinder/Sphere primitive has radius 0.5 m before scaling.
        private const float PrimitiveDefaultRadiusM = 0.5f;

        private MatchSession _session;
        private MatchRoster _roster;

        private GameObject[] _agentMarkers;
        private GameObject[] _possessionRings;
        private GameObject _ball;
        private GameObject _ballShadow;

        private Vector2[] _scratchAgentPositions;
        private AgentRenderModel[] _agentRenderModels;

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

            BuildMarkings();
            BuildAgentObjects();
            BuildBallObjects();
        }

        private void Start()
        {
            _session.Start();
        }

        private void Update()
        {
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

            foreach (PitchMarking marking in PitchMarkings.Build())
            {
                switch (marking.Kind)
                {
                    case PitchMarkingKind.Line:
                    case PitchMarkingKind.GoalMouth:
                        PlaceLine(parent, marking.A, marking.B);
                        break;

                    case PitchMarkingKind.Rectangle:
                        PlaceLine(parent, marking.A, new Vector2(marking.B.x, marking.A.y));
                        PlaceLine(parent, new Vector2(marking.B.x, marking.A.y), marking.B);
                        PlaceLine(parent, marking.B, new Vector2(marking.A.x, marking.B.y));
                        PlaceLine(parent, new Vector2(marking.A.x, marking.B.y), marking.A);
                        break;

                    case PitchMarkingKind.Circle:
                        PlaceRadial(parent, _markingCirclePrefab, marking.A, marking.Radius);
                        break;

                    case PitchMarkingKind.Spot:
                        PlaceRadial(parent, _markingSpotPrefab, marking.A, marking.Radius);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(marking.Kind), marking.Kind, "unhandled PitchMarkingKind");
                }
            }
        }

        private void PlaceLine(Transform parent, Vector2 fromXY, Vector2 toXY)
        {
            Vector3 from = PitchViewProjection.ToWorld(fromXY, 0f);
            Vector3 to = PitchViewProjection.ToWorld(toXY, 0f);

            GameObject go = Instantiate(_markingLinePrefab, parent);
            go.transform.position = (from + to) * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(to - from, Vector3.up);
            go.transform.localScale = new Vector3(
                go.transform.localScale.x, go.transform.localScale.y, (to - from).magnitude);
        }

        private void PlaceRadial(Transform parent, GameObject prefab, Vector2 centreXY, float radius)
        {
            GameObject go = Instantiate(prefab, parent);
            go.transform.position = PitchViewProjection.ToWorld(centreXY, 0f);
            float s = radius / PrimitiveDefaultRadiusM;
            go.transform.localScale = new Vector3(s, go.transform.localScale.y, s);
        }

        private void BuildAgentObjects()
        {
            _agentMarkers = new GameObject[_roster.AgentCount];
            _possessionRings = new GameObject[_roster.AgentCount];

            for (int i = 0; i < _roster.AgentCount; i++)
            {
                _agentMarkers[i] = Instantiate(_agentMarkerPrefab, transform);
                _possessionRings[i] = Instantiate(_possessionRingPrefab, transform);
                _possessionRings[i].SetActive(false);
            }
        }

        private void BuildBallObjects()
        {
            _ball = Instantiate(_ballPrefab, transform);
            _ballShadow = Instantiate(_ballShadowPrefab, transform);
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
                float s = model.MarkerRadius / PrimitiveDefaultRadiusM;
                marker.localScale = new Vector3(s, marker.localScale.y, s);

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
                    float ringScale = MatchClientConstants.PossessionRingRadiusM / PrimitiveDefaultRadiusM;
                    ring.localScale = new Vector3(ringScale, ring.localScale.y, ringScale);
                }
            }
        }

        private void RenderBall(float alpha)
        {
            Vector3 pitchBallPosition = FrameInterpolator.BallAt(_previousFrame, _currentFrame, alpha);
            BallRenderModel model = MatchRenderProjection.ProjectBall(pitchBallPosition);

            float ballScale = model.Radius / PrimitiveDefaultRadiusM;
            _ball.transform.position = model.WorldPosition;
            _ball.transform.localScale = new Vector3(ballScale, ballScale, ballScale);

            _ballShadow.transform.position = model.ShadowPosition;
            float shadowScale = model.ShadowRadius / PrimitiveDefaultRadiusM;
            _ballShadow.transform.localScale = new Vector3(shadowScale, _ballShadow.transform.localScale.y, shadowScale);
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
#endregion
