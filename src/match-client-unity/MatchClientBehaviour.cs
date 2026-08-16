// File:     src/match-client-unity/MatchClientBehaviour.cs
// Created:  2026-08-15
// Modified: 2026-08-16 (AR round 4 — Medium/Low findings M20/M21/M22/L12; see the VersionHistory
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
    /// <item><description><b>the mesh is unit-sized, in one of TWO ways, depending on which of the
    /// two classes below this slot is (M11 — this clause used to state one rule and every call site
    /// but the ball's silently needed a second one)</b>:
    /// <list type="bullet">
    /// <item><description><b>(a) FLAT ground props</b> — the agent marker, the possession ring, the
    /// ball shadow, a marking circle, a marking spot, a marking line, or the goal mouth (M18: the
    /// goal mouth was missing from this enumeration despite going through the identical
    /// <see cref="PlaceLine"/> path as a marking line and needing the identical authoring) — are
    /// authored at UNIT RADIUS (the round ones) or UNIT LENGTH ALONG LOCAL +Z WITH UNIT CROSS-SECTION
    /// (a marking line or the goal mouth), and with ZERO EXTENT IN LOCAL Y: the MESH ITSELF must be
    /// flat, e.g. a Quad lying flat
    /// rather than a Cylinder. This code assigns local Y = 1 to every one of these (see
    /// <see cref="FlatGroundScale"/> — named for this rule, not the ball's), which is INERT against a
    /// mesh with no height of its own. Flatness comes from the mesh being authored flat; the Y = 1
    /// assignment is a don't-care multiplier against a zero, not the source of it. A prefab whose
    /// mesh has real height (a genuinely unit-radius SPHERE used as a marker, say) renders as a
    /// squashed ellipsoid under this rule, not a flat disc — this code cannot detect that, so get the
    /// mesh right.
    /// <para><b>M22: FLAT is a sizing rule, not a silhouette</b> — two of the seven FLAT slots must be
    /// STROKED (an outline/ring, not a solid interior) and the rest FILLED, and this code cannot see
    /// or check the difference any more than it can check mesh height above. <b>Stroked:</b> the
    /// marking circle (<c>_markingCirclePrefab</c>) and the possession ring
    /// (<c>_possessionRingPrefab</c>) — <see cref="PitchMarkingKind.Circle"/>'s own doc distinguishes
    /// it from <see cref="PitchMarkingKind.Spot"/> precisely because "it is filled rather than
    /// stroked, and a renderer that collapsed the two would draw a solid centre circle"; the ring is
    /// meant to read as an annulus drawn AROUND the agent marker it annotates, not a second disc under
    /// it. <b>Filled:</b> the marking spot (<c>_markingSpotPrefab</c>,
    /// <see cref="PitchMarkingKind.Spot"/>'s own doc), the agent marker (<c>_agentMarkerPrefab</c>),
    /// and the ball shadow (<c>_ballShadowPrefab</c>) — each a solid disc. Author the two stroked slots
    /// as a ring/annulus mesh (or a hollow torus lying flat); a solid disc authored into either renders
    /// as a filled blob with no diagnostic short of eyeballing the pitch.</para></description></item>
    /// <item><description><b>(b) the ball</b> is the one VOLUMETRIC prop in the contract: a genuine
    /// unit-radius SPHERE, scaled uniformly on all three axes (<c>Vector3.one * model.Radius</c>), so
    /// it reads as a sphere at every radius the sim reports rather than a disc.</description></item>
    /// </list>
    /// Either way the scale this code assigns is then the metre figure ITSELF, with no conversion —
    /// which is the whole point: a per-primitive "what radius is a Unity Cylinder by default"
    /// constant is a class of bug rather than a number.</description></item>
    /// <item><description><b>the agent marker's material exposes the colour property named by
    /// <see cref="_colorPropertyName"/></b> — defaulted to <c>"_Color"</c>, the Built-in Render
    /// Pipeline standard shader's name for it. URP's Lit/SimpleLit/Unlit shaders expose
    /// <c>"_BaseColor"</c> instead, and this repo does not settle which pipeline resolves at
    /// runtime: <c>ProjectSettings/GraphicsSettings.asset</c> references a Universal render pipeline
    /// asset while <c>Packages/manifest.json</c> declares no URP package. The requirement is stated
    /// and checked rather than assumed because the failure is silent — <c>SetColor</c> against a
    /// property the shader does not have succeeds and changes nothing, so both teams, the goalkeeper
    /// tint and the sent-off tint would all render in whatever colour the prefab material already
    /// carries, with no diagnostic anywhere. Rejected per marker at instantiation (H6); point
    /// <see cref="_colorPropertyName"/> at the property your pipeline's shader actually
    /// exposes.</description></item>
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
    /// a project-setup requirement this file cannot enforce or detect; it is recorded here AND in
    /// (M14) the Editor-setup document this assembly's README has become —
    /// <c>src/match-client-unity/README.md</c> — which also carries the prefab contract as a table,
    /// this GameObject's own transform requirement (M15), and the team-colour palette shape.</para>
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
        [SerializeField] private GameObject _goalMouthPrefab;

        [Header("Team palette — index 0 = home, 1 = away")]
        [SerializeField] private Color[] _teamColors = new Color[] { Color.blue, Color.red };

        [Header("Camera")]
        [SerializeField] private Camera _matchCamera;

        /// <summary>
        /// The demo match's seed, as text. A <c>ulong</c> <c>[SerializeField]</c>'s round-trip
        /// through Unity's <c>SerializedProperty</c> for values above <c>long.MaxValue</c> is
        /// unverified in this environment (L5) — a string inspector field sidesteps the question
        /// entirely, at the cost of parsing it here instead of the type system doing it for free.
        /// That parse lives in <see cref="ValidateWiring"/> (H5), not at the point of use.
        /// </summary>
        [Header("Demo boot (until a real squad source is wired)")]
        [SerializeField] private string _demoSeedText = "1";

        /// <summary>
        /// Name of the shader property the marker's colour is written to — prefab-contract clause 3.
        /// An inspector field rather than a baked literal because the answer is a render-pipeline
        /// fact this code cannot see: the Built-in pipeline's standard shader calls it
        /// <c>"_Color"</c> (the default here), URP's Lit/SimpleLit/Unlit call it <c>"_BaseColor"</c>.
        /// Exposing it lets a URP project retarget the write from the Inspector instead of editing a
        /// file the CI gate cannot compile (§12 rule 1).
        /// </summary>
        [Header("Marker material — the colour property name this project's shader exposes")]
        [SerializeField] private string _colorPropertyName = "_Color";

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

        // Both are resolved ONCE in ValidateWiring, before anything is constructed, from the two
        // inspector fields above: the seed because parsing it lazily at the point of use threw out
        // of Awake (H5), the property id because the per-frame path must not look a shader property
        // up by string (M4). The id is an instance field rather than the static readonly one it
        // replaces for the same reason the name became an inspector field — a static initialiser
        // runs before deserialization and cannot see a serialized value.
        private ulong _demoSeed;
        private int _colorPropertyId;

        private bool _wiringRejected;

        // The previous/current frame decision (§12 rule 1: a state machine, so it lives in
        // match-client-core where the gate compiles and tests it — AR pass M-6).
        private readonly LiveFrameLatch _frameLatch = new LiveFrameLatch();
        private Vector2 _cameraTarget;

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

            // H5: Unity CATCHES an exception thrown out of Awake, logs it — and then delivers Start
            // and every Update anyway, WITHOUT disabling the component. So anything that throws
            // inside BuildScene (a MatchSession/MatchSetup constructor, PitchMarkings.BuildDrawables'
            // own invariant check, BuildMarkings' unreachable-by-design default: arm) would leave
            // _session null with this component still live, NullReferenceException-ing every frame
            // forever and naming none of it. Catching converts any such failure into the single
            // terminal, self-describing state this file already has for bad wiring.
            //
            // FR-CS-069 bans try/catch in per-frame INNER LOOPS; this is one-time initialization —
            // the same carve-out LiveMatchStreamer's post-tick observer and MatchSession's save path
            // cite for their own non-hot-path catches.
            try
            {
                BuildScene();
            }
            catch (Exception ex)
            {
                RejectWiring("scene construction threw " + ex.GetType().Name + ": " + ex.Message + ".");

                // Logged separately because the line above names the cause but drops the stack trace,
                // which is the half that says WHERE in the construction it went wrong.
                Debug.LogException(ex, this);
            }
        }

        /// <summary>
        /// Builds everything this binding owns, in the only order that works — the session first,
        /// since the roster it reports is what sizes the per-agent arrays. Called once, from
        /// <see cref="Awake"/> and only inside the guard documented there: nothing here may run
        /// before <see cref="ValidateWiring"/> has passed, and nothing here may throw past
        /// <see cref="Awake"/>.
        /// </summary>
        private void BuildScene()
        {
            _session = new MatchSession(MatchSetup.NeutralDemo(_demoSeed));
            _roster = MatchRoster.FromStreamer(_session.Streamer);

            _scratchAgentPositions = new Vector2[_roster.AgentCount];
            _agentRenderModels = new AgentRenderModel[_roster.AgentCount];
            _scratchPropertyBlock = new MaterialPropertyBlock();

            // L1: seeded to the pitch CENTRE, not the corner-origin frame's (0, 0) — leaving the
            // implicit Vector2.zero default put the camera's first target at a pitch corner, so every
            // match opened with a ~1 s slide-in before FollowBallCamera caught up to the kickoff spot.
            _cameraTarget = new Vector2(PitchViewProjection.HalfLengthM, PitchViewProjection.HalfWidthM);

            // Everything every PREFAB must satisfy UNIFORMLY is checked as each is instantiated, in
            // InstantiatePrefab below — the gate for a check that applies to every prefab the same way
            // (H-2's own comment). L9: a check that applies to only one SLOT belongs at its own
            // instantiation site instead, not here — H6's marker material/colour-property check in
            // BuildAgentObjects is the precedent for that pattern. ValidateWiring covers what has to be
            // true before any Instantiate call is even safe to make.
            //
            // L6: each step can itself set _wiringRejected partway through (a bad prefab found on
            // marking 3 of 20, say) — re-checked between steps so a rejection during BuildMarkings
            // does not go on to instantiate every agent and ball object too.
            BuildMarkings();
            if (_wiringRejected) { return; }

            BuildAgentObjects();
            if (_wiringRejected) { return; }

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

            // L12: MatchRenderProjection.ProjectAgents/ProjectBall (called inside RenderAgents/
            // RenderBall below) are fail-loud on a non-finite coordinate, and nothing upstream refuses
            // one — FrameInterpolator deliberately PROPAGATES a non-finite position rather than gating
            // it (it reads one as a discontinuity and snaps to it). Without this, one bad frame from
            // the sim throws the SAME exception every subsequent Update forever, with the rest of this
            // method (the camera, the click handler) never reached again on any later frame either —
            // an unresponsive-but-not-disabled client, H5's Awake failure mode one call site over.
            //
            // Mirrors H5's own Awake try/catch exactly, and the same FR-CS-069 reasoning applies: this
            // is Unity's top-level MonoBehaviour callback, invoked once per rendered frame — not a
            // per-frame INNER loop in the rule's sense (a physics substep loop iterated many times
            // inside one frame, say). FR-CS-069 bans a catch inside that kind of hot inner loop; it
            // does not ban one wrapping the once-per-frame entry point itself, which is exactly the
            // carve-out H5's own comment in Awake already claims for one-time initialization — Update
            // is the same shape, called once per frame rather than once per process, but never nested
            // inside a tighter loop of its own.
            try
            {
                RenderAgents(alpha);
                RenderBall(pitchBallPosition);
                UpdateCamera(pitchBallPosition);
                HandleClick();
            }
            catch (Exception ex)
            {
                RejectWiring("Update threw " + ex.GetType().Name + ": " + ex.Message + ".");

                // Logged separately for the same reason Awake's catch does: the RejectWiring message
                // names the cause but drops the stack trace, which is the half that says WHERE in the
                // frame's rendering it went wrong.
                Debug.LogException(ex, this);
            }
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
        /// assign, since both mix a world POSITION with a LOCAL scale. M15: a non-identity ROTATION on
        /// this GameObject's own transform is the same hazard's other half — <c>Instantiate(prefab,
        /// parent)</c> keeps <c>instantiateInWorldSpace</c> false, so an instantiated child's
        /// <c>localRotation</c> copies the prefab's (identity, per the neutral-root contract), but it
        /// still INHERITS this transform's WORLD rotation. <see cref="PlaceLine"/> assigns a world
        /// <c>rotation</c> explicitly and is immune; <see cref="PlaceRadial"/>, <see cref="RenderAgents"/>
        /// and <see cref="RenderBall"/> assign only world position and local scale, so an ancestor
        /// rotation would silently tilt every circle, spot, marker, ring, ball and shadow while the
        /// lines alone stayed flat — with no diagnostic anywhere short of this check. Checked against
        /// world <c>rotation</c>, not <c>localRotation</c>, so a rotated ANCESTOR is caught too, not
        /// just this GameObject's own transform. Reuses <see cref="RejectWiring"/>
        /// rather than duplicating its log-and-disable behaviour; <see cref="InstantiatePrefab"/> below
        /// is the complementary check that runs once a prefab HAS been instantiated (root neutrality,
        /// no world-space <c>LineRenderer</c>).
        ///
        /// <para>It also RESOLVES the two inspector fields that are text rather than a usable value —
        /// the demo seed and the shader colour-property id — because for both, "is this field valid"
        /// and "what does it parse to" are the same question, and both answers are needed before
        /// construction starts (H5).</para>
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
            if (_goalMouthPrefab == null) { RejectWiring(nameof(_goalMouthPrefab) + " is not assigned in the inspector."); return; }
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

            // M15: the same POSITION-vs-SCALE-vs-ROTATION mixing hazard as the check above, for
            // rotation. World rotation, not localRotation — a rotated ANCESTOR reaches every child the
            // same way a scaled one does, and Instantiate(prefab, parent) only ever resets local space.
            // M20: IsIdentityRotation, not != Quaternion.identity — see that method's doc for why a
            // plain equality check falsely rejects a legitimately-unrotated transform under quaternion
            // double cover.
            if (!IsIdentityRotation(transform.rotation))
            {
                RejectWiring(
                    "the MatchClient GameObject's own transform (or an ancestor's) must be at identity " +
                    "world rotation — PlaceRadial/RenderAgents/RenderBall assign only a world POSITION " +
                    "and a LOCAL scale, never a rotation, so a rotated parent silently tilts every " +
                    "circle, spot, marker, ring, ball and shadow while PlaceLine's markings (which do " +
                    "assign a world rotation) stay flat. transform.rotation is " +
                    FormatQuaternionComponents(transform.rotation) + " (eulerAngles " +
                    transform.rotation.eulerAngles + ").");
                return;
            }

            // H5: parsed HERE, ahead of every construction, rather than at the MatchSetup.NeutralDemo
            // call site — a parse failure there threw out of Awake, which Unity answers by logging and
            // carrying on, so Start and Update then ran forever against a null session with nothing
            // naming the seed as the cause. Invariant culture with no permitted number styles, so the
            // field means exactly "digits": a plain TryParse honours the host locale, under which a
            // group separator would read "1.000" as the seed 1000 and silently play a different match.
            if (!ulong.TryParse(_demoSeedText, NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed))
            {
                RejectWiring(
                    nameof(_demoSeedText) + " must be an unsigned 64-bit integer written as digits only " +
                    "(no sign, spaces, separators or exponent) — it is \"" + _demoSeedText +
                    "\", which MatchSetup.NeutralDemo cannot be given.");
                return;
            }

            _demoSeed = seed;

            if (string.IsNullOrEmpty(_colorPropertyName))
            {
                RejectWiring(
                    nameof(_colorPropertyName) + " is empty — it must name the colour property this " +
                    "project's marker shader exposes (Built-in Render Pipeline: \"_Color\"; URP Lit/" +
                    "SimpleLit/Unlit: \"_BaseColor\"), since a write to a property that does not exist " +
                    "is silently discarded.");
                return;
            }

            // H6/M4: the id is resolved once, off the per-frame path. Whether the marker's MATERIAL
            // actually carries the property is a separate question only an instantiated prefab can
            // answer — checked per marker in BuildAgentObjects.
            _colorPropertyId = Shader.PropertyToID(_colorPropertyName);
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
            PitchMarking[] drawables = PitchMarkings.BuildDrawables();

            for (int i = 0; i < drawables.Length; i++)
            {
                // L6: RejectWiring may have fired on a PREVIOUS iteration's InstantiatePrefab/PlaceLine/
                // PlaceRadial call. Without this, a single bad marking prefab keeps getting instantiated
                // (and keeps re-logging the same rejection) for every remaining entry — DRAWABLE_COUNT
                // is in the twenties, so that is ~20+ redundant instantiations and log lines for one bug.
                if (_wiringRejected) { return; }

                PitchMarking marking = drawables[i];

                // M16: BuildDrawables()'s ordering is a stated, deterministic contract (its own doc),
                // so indexing into it to spread the marking layer into a BAND is safe — every drawable
                // used to land at the exact same MarkingLayerHeightM, so the boundary/penalty
                // area/goal area/goal mouth all overlapped at each goal line, the centre spot sat
                // exactly on the halfway line, and the centre circle crossed it twice. Boot-validated
                // against BallShadowLayerHeightM in MatchClientConstants so the band cannot grow into
                // the next ground layer up.
                float layerHeightM = MatchClientConstants.MarkingLayerHeightM + i * MatchClientConstants.MarkingLayerStepM;

                switch (marking.Kind)
                {
                    case PitchMarkingKind.Line:
                        PlaceLine(
                            parent, _markingLinePrefab, nameof(_markingLinePrefab),
                            MatchClientConstants.MarkingLineWidthM, marking.A, marking.B, layerHeightM);
                        break;

                    case PitchMarkingKind.GoalMouth:
                        // M10: its own prefab and width, not the marking line's. PitchMarkingKind.
                        // GoalMouth's own doc is explicit that it is goal furniture drawn heavier than
                        // a marking line, not a Law 1 marking sharing the line's look.
                        PlaceLine(
                            parent, _goalMouthPrefab, nameof(_goalMouthPrefab),
                            MatchClientConstants.GoalMouthWidthM, marking.A, marking.B, layerHeightM);
                        break;

                    case PitchMarkingKind.Circle:
                        PlaceRadial(parent, _markingCirclePrefab, nameof(_markingCirclePrefab), marking.A, marking.Radius, layerHeightM);
                        break;

                    case PitchMarkingKind.Spot:
                        PlaceRadial(parent, _markingSpotPrefab, nameof(_markingSpotPrefab), marking.A, marking.Radius, layerHeightM);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(marking.Kind), marking.Kind,
                            "unhandled PitchMarkingKind — note BuildDrawables emits no Rectangle, so " +
                            "one arriving here means the decomposition regressed");
                }
            }
        }

        /// <summary>
        /// Places one straight marking-shaped primitive from <paramref name="fromXY"/> to
        /// <paramref name="toXY"/>, at <paramref name="layerHeightM"/> (M12/M16), using
        /// <paramref name="prefab"/> at width <paramref name="widthM"/>. Shared by
        /// <see cref="PitchMarkingKind.Line"/> (the boundary/halfway/box edges) and
        /// <see cref="PitchMarkingKind.GoalMouth"/> (M10: its own prefab and width, not the marking
        /// line's), which differ only in which prefab and width the caller passes.
        /// </summary>
        private void PlaceLine(Transform parent, GameObject prefab, string prefabField, float widthM, Vector2 fromXY, Vector2 toXY, float layerHeightM)
        {
            Vector3 from = PitchViewProjection.ToWorld(fromXY, layerHeightM);
            Vector3 to = PitchViewProjection.ToWorld(toXY, layerHeightM);
            Vector3 along = to - from;

            GameObject go = InstantiatePrefab(prefab, parent, prefabField);
            go.transform.position = (from + to) * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(along, Vector3.up);

            // Unit length along local +Z and unit cross-section, so both figures are metres as they
            // stand. Y stays at 1 — inert, not the source of flatness (M11): the mesh itself must be
            // authored with zero height (prefab-contract clause 2a), and a line painted on the turf
            // has no thickness of its own for that mesh to carry.
            Vector3 scale = Vector3.one;
            scale.x = widthM;
            scale.z = along.magnitude;
            go.transform.localScale = scale;
        }

        /// <summary>
        /// Places one round marking-shaped primitive (a circle or a spot) centred at
        /// <paramref name="centreXY"/>, at <paramref name="layerHeightM"/> (M12/M16), using
        /// <paramref name="prefab"/> at <paramref name="radius"/>.
        /// </summary>
        private void PlaceRadial(Transform parent, GameObject prefab, string prefabField, Vector2 centreXY, float radius, float layerHeightM)
        {
            GameObject go = InstantiatePrefab(prefab, parent, prefabField);
            go.transform.position = PitchViewProjection.ToWorld(centreXY, layerHeightM);
            go.transform.localScale = FlatGroundScale(radius);
        }

        private void BuildAgentObjects()
        {
            _agentMarkers = new GameObject[_roster.AgentCount];
            _agentMarkerRenderers = new MeshRenderer[_roster.AgentCount];
            _possessionRings = new GameObject[_roster.AgentCount];

            for (int i = 0; i < _roster.AgentCount; i++)
            {
                // L6: a bad prefab found on agent 0 of a 22-agent roster would otherwise keep
                // instantiating (and keep re-logging the same rejection) for every remaining agent.
                if (_wiringRejected) { return; }

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
                else if (markerRenderer.sharedMaterial == null)
                {
                    // H6: the colour write is the ONLY thing distinguishing the two teams, the
                    // goalkeeper and a sent-off player, and MaterialPropertyBlock.SetColor against a
                    // material that cannot receive it fails SILENTLY — so the material is checked
                    // here rather than discovered as "why is everyone blue?" on the pinned host.
                    RejectWiring(
                        nameof(_agentMarkerPrefab) + " agent index " + Inv(i) +
                        " has a MeshRenderer with no material, so its team colour would be discarded " +
                        "silently and every marker would render identically.");
                }
                else if (!markerRenderer.sharedMaterial.HasProperty(_colorPropertyId))
                {
                    Shader shader = markerRenderer.sharedMaterial.shader;

                    RejectWiring(
                        nameof(_agentMarkerPrefab) + " agent index " + Inv(i) + " has material \"" +
                        markerRenderer.sharedMaterial.name + "\" (shader \"" +
                        (shader == null ? "<none>" : shader.name) + "\"), which exposes no \"" +
                        _colorPropertyName + "\" property — SetColor would succeed and change nothing, " +
                        "rendering both teams, the goalkeeper tint and the sent-off tint in the " +
                        "material's own colour. Set " + nameof(_colorPropertyName) + " to the property " +
                        "this project's pipeline uses (Built-in Render Pipeline: \"_Color\"; URP Lit/" +
                        "SimpleLit/Unlit: \"_BaseColor\"), or give the prefab a material that has it.");
                }

                _agentMarkerRenderers[i] = markerRenderer;

                // L6: the marker check above may itself have just rejected agent i (a missing/bad
                // material) — without this, the possession-ring prefab still gets instantiated for the
                // same agent in the same iteration on top of the marker's own rejection.
                if (_wiringRejected) { return; }

                _possessionRings[i] = InstantiatePrefab(_possessionRingPrefab, transform, nameof(_possessionRingPrefab));
                _possessionRings[i].SetActive(false);
            }
        }

        private void BuildBallObjects()
        {
            _ball = InstantiatePrefab(_ballPrefab, transform, nameof(_ballPrefab));

            // L6: the ball itself may have just been rejected (a non-neutral root, say) — without
            // this, the shadow prefab still gets instantiated on top of that rejection.
            if (_wiringRejected) { return; }

            _ballShadow = InstantiatePrefab(_ballShadowPrefab, transform, nameof(_ballShadowPrefab));
        }

        /// <summary>
        /// Instantiates one prefab under <paramref name="parent"/> and rejects the client unless the
        /// instance honours the prefab contract in the type doc. This is the gate every scene object
        /// passes through for a check that applies UNIFORMLY to every prefab (root neutrality, no
        /// world-space <c>LineRenderer</c>) — so that is where a further wiring check of that shape
        /// belongs. L9: a check that applies to only one SLOT (a specific prefab's material, say)
        /// belongs at ITS OWN instantiation site instead, the way H6's marker material/colour-property
        /// check lives in <see cref="BuildAgentObjects"/> rather than here.
        /// </summary>
        private GameObject InstantiatePrefab(GameObject prefab, Transform parent, string prefabField)
        {
            GameObject instance = Instantiate(prefab, parent);
            Transform root = instance.transform;

            // M20: IsIdentityRotation, not != Quaternion.identity — see that method's doc.
            if (!IsIdentityRotation(root.localRotation) || root.localScale != Vector3.one)
            {
                RejectWiring(
                    prefabField + " must be authored with a NEUTRAL root — identity rotation, unit " +
                    "scale — and any fixed tilt or thickness baked onto a child mesh; this one has " +
                    "rotation " + FormatQuaternionComponents(root.localRotation) + " (eulerAngles " +
                    root.localRotation.eulerAngles + ") and scale " + root.localScale +
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
        /// Scale for a FLAT, unit-radius prop lying on the turf (prefab-contract clause 2a): the
        /// radius in metres on both ground axes, and Y left at 1 — inert against a mesh authored with
        /// zero height, not the source of the flatness. Under the prefab contract no conversion is
        /// involved — which is exactly what a shared "default primitive radius" divisor used to hide.
        ///
        /// <para>M11: named <c>Flat</c>GroundScale, not <c>GroundScale</c>, specifically so it reads
        /// as clause 2a's rule at every call site — <see cref="RenderBall"/>'s ball itself does NOT
        /// use this method, because the ball is clause 2b's one volumetric prop
        /// (<c>Vector3.one * model.Radius</c>, scaled uniformly on all three axes).</para>
        /// </summary>
        private static Vector3 FlatGroundScale(float radiusM)
        {
            Vector3 scale = Vector3.one;
            scale.x = radiusM;
            scale.z = radiusM;
            return scale;
        }

        /// <summary>
        /// M12: <paramref name="groundPosition"/> with its Y replaced by <paramref name="heightM"/> —
        /// one of the four ordered <c>MatchClientConstants</c> ground-layer heights. Every producer of
        /// <paramref name="groundPosition"/> in this file (<see cref="AgentRenderModel.WorldPosition"/>,
        /// <see cref="BallRenderModel.ShadowPosition"/>) reports Y = 0, so without this every ground
        /// layer would be coplanar and a real renderer would z-fight wherever two overlap — a shadow
        /// crossing a painted line, a ring sitting under the marker it annotates.
        ///
        /// <para>Applied here rather than threaded through <c>match-client-core</c>'s render models:
        /// the four layers are a pure rendering-order choice with no simulation meaning (unlike, say,
        /// <see cref="BallRenderModel.HeightM"/>, which is the engine's own physics height), and
        /// <c>PitchMarking</c> carries no height field at all — inventing one on either type to carry
        /// a number that already has a named, boot-validated home in the constant catalogue would move
        /// the decision without removing it. Reading the named constant directly here (rather than an
        /// inline literal) is what the finding actually asked for.</para>
        /// </summary>
        private static Vector3 WithGroundLayerHeight(Vector3 groundPosition, float heightM)
        {
            groundPosition.y = heightM;
            return groundPosition;
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
                marker.position = WithGroundLayerHeight(model.WorldPosition, MatchClientConstants.AgentMarkerLayerHeightM);
                marker.localScale = FlatGroundScale(model.MarkerRadius);

                _scratchPropertyBlock.SetColor(_colorPropertyId, ResolveMarkerColor(in model));
                _agentMarkerRenderers[i].SetPropertyBlock(_scratchPropertyBlock);

                Transform ring = _possessionRings[i].transform;
                _possessionRings[i].SetActive(model.HasBall);
                if (model.HasBall)
                {
                    // M12: the ring's ground point matches the marker's (same agent), but its LAYER
                    // height is deliberately its own, lower constant — the ring must render underneath
                    // the marker it annotates, per PossessionRingLayerHeightM's ordering.
                    ring.position = WithGroundLayerHeight(model.WorldPosition, MatchClientConstants.PossessionRingLayerHeightM);
                    // M3: reads AgentRenderModel.PossessionRingRadius (0 when !HasBall, otherwise the
                    // catalogue's [GT] radius) rather than the [GT] constant a second time — the
                    // model is the single source, per its own class doc.
                    ring.localScale = FlatGroundScale(model.PossessionRingRadius);
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

            // Prefab-contract clause 2b: the ball is the one VOLUMETRIC prop, scaled uniformly on all
            // three axes so it reads as a sphere at every radius rather than a flat disc (M11).
            // M21: model.WorldPosition.y is NOT the raw physics height below model.Radius (M17 floors
            // it there so the drawn sphere never sinks through the turf — see BallRenderModel's own
            // doc) — it is still left untouched by M12's ground-layer scheme, though, since it is not
            // a ground layer at all: a ball above its own radius rides on its real physics height, and
            // one below it rides on the floor instead, neither of which M12's ordering concerns.
            _ball.transform.position = model.WorldPosition;
            _ball.transform.localScale = Vector3.one * model.Radius;

            // M12: the shadow is a ground layer — its own height, above the markings it regularly
            // crosses and below the ring/marker it can coincide with.
            _ballShadow.transform.position = WithGroundLayerHeight(model.ShadowPosition, MatchClientConstants.BallShadowLayerHeightM);
            _ballShadow.transform.localScale = FlatGroundScale(model.ShadowRadius);
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

        /// <summary>
        /// M20: whether <paramref name="rotation"/> IS the identity rotation, within
        /// <see cref="RotationIdentityDotTolerance"/> — safe under quaternion double cover, unlike a
        /// plain <c>!= Quaternion.identity</c> comparison.
        ///
        /// <para>A rotation and its negation (<c>q</c> and <c>-q</c>) represent the IDENTICAL
        /// orientation — every component flipped in sign describes the same rotation, since a
        /// quaternion's sign is not observable in the rotation it produces. Unity's
        /// <c>Quaternion.operator==</c> is defined as <c>Dot(lhs, rhs) &gt; 1 - kEpsilon</c>, which is
        /// sign-SENSITIVE: <c>Dot(q, -q) == -Dot(q, q) == -1</c> for a unit quaternion, so a
        /// legitimately-unrotated transform whose composed rotation happens to land on the negative
        /// representative of identity compares UNEQUAL to <c>Quaternion.identity</c> and was rejected
        /// by both call sites of this method — a false positive with no way for the scene author to
        /// "fix" it, since the rotation already IS identity. Comparing <c>|Dot|</c> against 1 instead
        /// collapses the double cover: <c>|Dot(q, identity)|</c> is 1 for both <c>q</c> and
        /// <c>-q</c>.</para>
        /// </summary>
        private static bool IsIdentityRotation(Quaternion rotation) =>
            Mathf.Abs(Quaternion.Dot(rotation, Quaternion.identity)) >= 1f - RotationIdentityDotTolerance;

        /// <summary>
        /// [GT] Tolerance (M20) on the <c>|Dot|</c> test in <see cref="IsIdentityRotation"/>. Not a
        /// catalogue constant — this file is excluded from the shim gate (§12 rule 1; see the
        /// assembly's README) and has no consumer that would need it configurable — but named rather
        /// than inlined so both call sites read the same value and a future retune touches one line.
        /// </summary>
        private const float RotationIdentityDotTolerance = 1e-5f;

        /// <summary>
        /// M20: formats <paramref name="rotation"/>'s raw <c>(x, y, z, w)</c> components. The rejection
        /// messages that cite <see cref="IsIdentityRotation"/> print this ALONGSIDE
        /// <c>Quaternion.eulerAngles</c> rather than in place of it, because <c>eulerAngles</c> alone
        /// can actively mislead: the negative representative of identity, <c>(0, 0, 0, -1)</c>, still
        /// reports <c>eulerAngles == (0, 0, 0)</c> — before this fix, that rotation was rejected by a
        /// message reading "must be identity" while the only figure it showed already WAS <c>(0, 0,
        /// 0)</c>, telling the operator nothing about what was actually wrong. Printing the raw
        /// components alongside it means the message can never show a value that contradicts its own
        /// stated requirement.
        /// </summary>
        private static string FormatQuaternionComponents(Quaternion rotation) =>
            "(" + Inv(rotation.x) + ", " + Inv(rotation.y) + ", " + Inv(rotation.z) + ", " +
            Inv(rotation.w) + ")";

        private static string Inv(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Inv(float value) => value.ToString(CultureInfo.InvariantCulture);
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
// | 1.3     | 2026-08-15 | —      | AR round 2, High findings H4-H6. H4 is this file's missing      |
// |         |            |        | .cs.meta, generated by tools/unity-ci/generate-missing-metas.sh |
// |         |            |        | — no source change (a .cs with no committed .meta gets a fresh  |
// |         |            |        | random GUID on every checkout, breaking every scene/prefab      |
// |         |            |        | reference to this component; check-meta-integrity.sh is a       |
// |         |            |        | SEPARATE CI job from run-gate.sh, which is how two gate-        |
// |         |            |        | verified commits both missed it). H5: nothing that throws       |
// |         |            |        | inside Awake can now leave the component enabled with null      |
// |         |            |        | fields — Unity logs an Awake exception and then delivers        |
// |         |            |        | Start/Update anyway. Two parts: the _demoSeedText parse moves   |
// |         |            |        | INTO ValidateWiring (ahead of all construction, invariant-      |
// |         |            |        | culture digits-only, rejecting by field name and bad value),    |
// |         |            |        | and the throwing DemoSeed property is deleted in favour of the  |
// |         |            |        | single validated _demoSeed field; the rest of Awake's           |
// |         |            |        | construction moves into a new BuildScene() wrapped in try/catch |
// |         |            |        | that routes any failure to RejectWiring + LogException          |
// |         |            |        | (FR-CS-069 bans try/catch in per-frame inner loops; this is     |
// |         |            |        | one-time init — the LiveMatchStreamer / MatchSession carve-out).|
// |         |            |        | H6: the bare, unvalidated Shader.PropertyToID("_Color") literal |
// |         |            |        | becomes the [SerializeField] _colorPropertyName (Built-in       |
// |         |            |        | pipeline default; URP shaders expose _BaseColor, and this repo  |
// |         |            |        | does not settle which pipeline resolves — GraphicsSettings.asset|
// |         |            |        | names a Universal asset, manifest.json has no URP package), the |
// |         |            |        | prefab contract gains it as numbered clause 3, s_colorPropertyId|
// |         |            |        | becomes the instance _colorPropertyId resolved once in          |
// |         |            |        | ValidateWiring (a static initialiser cannot read a serialized   |
// |         |            |        | field), and BuildAgentObjects rejects any marker whose material |
// |         |            |        | is absent or lacks the property — SetColor against a missing    |
// |         |            |        | property succeeds silently, which would ship both teams, the    |
// |         |            |        | goalkeeper tint and the sent-off tint in one colour.            |
// | 1.4     | 2026-08-15 | —      | AR round 2, Medium/Low findings M10-M13/L6-L8. M10: goal mouths |
// |         |            |        | get their own _goalMouthPrefab (null-checked in ValidateWiring) |
// |         |            |        | and the new [GT] GoalMouthWidthM instead of sharing the marking |
// |         |            |        | line's prefab/width — PlaceLine is now parameterised over       |
// |         |            |        | (prefab, prefabField, widthM) rather than always reading the    |
// |         |            |        | marking-line fields, and PlaceRadial/RenderAgents/RenderBall    |
// |         |            |        | call the renamed FlatGroundScale (see M11). M11: prefab-contract|
// |         |            |        | clause 2 is rewritten into two explicit classes — 2a FLAT ground|
// |         |            |        | props (unit radius/length in local XZ, ZERO extent in local Y,  |
// |         |            |        | the mesh itself must be authored flat) and 2b the ball, the one |
// |         |            |        | VOLUMETRIC unit-radius sphere scaled uniformly on all three     |
// |         |            |        | axes — no 4th clause added. GroundScale renamed FlatGroundScale |
// |         |            |        | so the name states which of the two rules a call site follows; |
// |         |            |        | every call site updated. PlaceLine's "Y stays at 1" comment no  |
// |         |            |        | longer credits the Y=1 ASSIGNMENT with flatness — the mesh being|
// |         |            |        | authored flat is the source, Y=1 is inert against a zero height.|
// |         |            |        | M12: four new ordered MatchClientConstants ground-layer heights |
// |         |            |        | (markings lowest, then ball shadow, then possession ring, then  |
// |         |            |        | agent marker) replace the implicit world Y=0 every ground prop  |
// |         |            |        | shared — coplanar opaque surfaces z-fight in a real renderer,   |
// |         |            |        | and the ring is explicitly meant to sit under the marker it     |
// |         |            |        | annotates. New WithGroundLayerHeight helper applies the named   |
// |         |            |        | constant at each ground placement (markings via PlaceLine/      |
// |         |            |        | PlaceRadial, the marker and ring in RenderAgents, the shadow in |
// |         |            |        | RenderBall); the ball itself is untouched, since its height is  |
// |         |            |        | the engine's own physics height, not a ground layer. Read       |
// |         |            |        | directly from the catalogue here rather than threaded through   |
// |         |            |        | the match-client-core render models: the four layers are a pure |
// |         |            |        | rendering-order choice with no simulation meaning, and           |
// |         |            |        | PitchMarking carries no height field to extend. L6: BuildMarkings|
// |         |            |        | / BuildAgentObjects / BuildBallObjects each now check            |
// |         |            |        | _wiringRejected at the top of their loop body (and, where a      |
// |         |            |        | rejection can fire mid-iteration, immediately after) and return  |
// |         |            |        | rather than keep instantiating and re-logging the same failure   |
// |         |            |        | for every remaining marking/agent/ball object; BuildScene checks |
// |         |            |        | it between its three build steps too. L7: MarkingLineWidthM (and |
// |         |            |        | the new GoalMouthWidthM, validated the same way from the start)  |
// |         |            |        | now go through MatchClientConstants.RequireInRange like their    |
// |         |            |        | render-cue siblings, instead of an unvalidated Config.GetFloat.  |
// |         |            |        | L8: CameraTiltDegrees/CameraLateralOffsetM gain a pairing check  |
// |         |            |        | (MatchClientConstants.RequireTiltOrOffsetNonzero) — both zero    |
// |         |            |        | sits the camera directly above its target, an undefined          |
// |         |            |        | LookAt rotation; each dial stays individually legal at zero.     |
// | 1.5     | 2026-08-16 | —      | AR round 3, Medium/Low pass (M15/M16/M18/L9, plus the type-doc's |
// |         |            |        | M8 paragraph updated for M14 — M17/L10/L11 are match-client-core |
// |         |            |        | / doc-only findings, not this file). M14: the "Editor-setup      |
// |         |            |        | instructions, which this file does not own" phrase pointed at a  |
// |         |            |        | document that did not exist anywhere in the repo — now names     |
// |         |            |        | src/match-client-unity/README.md, which has become that document.|
// |         |            |        | M15: ValidateWiring gains a world-rotation check beside the      |
// |         |            |        | existing lossyScale one — Instantiate(prefab, parent) resets a   |
// |         |            |        | child's LOCAL rotation from the prefab but still inherits this   |
// |         |            |        | transform's (or an ancestor's) WORLD rotation, and PlaceRadial/  |
// |         |            |        | RenderAgents/RenderBall assign only world position and local     |
// |         |            |        | scale, never rotation, so a non-identity rotation here would     |
// |         |            |        | silently tilt every circle, spot, marker, ring, ball and shadow  |
// |         |            |        | while PlaceLine's markings (which DO assign a world rotation)    |
// |         |            |        | stayed flat, with no diagnostic anywhere. M16: the marking layer |
// |         |            |        | becomes a BAND, not a plane — BuildMarkings threads each          |
// |         |            |        | drawable's index from PitchMarkings.BuildDrawables() (a stated,  |
// |         |            |        | deterministic order) through PlaceLine/PlaceRadial's new          |
// |         |            |        | layerHeightM parameter as MarkingLayerHeightM + i *               |
// |         |            |        | MatchClientConstants.MarkingLayerStepM (new [GT], boot-validated  |
// |         |            |        | to stay comfortably clear of BallShadowLayerHeightM) — every      |
// |         |            |        | drawable used to land at the exact same height, so the boundary/  |
// |         |            |        | penalty area/goal area/goal mouth all overlapped at each goal     |
// |         |            |        | line, the centre spot sat exactly on the halfway line, and the    |
// |         |            |        | centre circle crossed it twice. M18: the type-doc prefab-contract |
// |         |            |        | clause 2a enumeration gains the goal mouth, missing since M10     |
// |         |            |        | gave it its own prefab despite going through the identical         |
// |         |            |        | PlaceLine path and needing the identical unit-length/unit-        |
// |         |            |        | cross-section authoring as a marking line. L9: InstantiatePrefab's |
// |         |            |        | doc and BuildScene's echoing comment no longer claim to be THE     |
// |         |            |        | single gate for any further wiring check — both now scope that     |
// |         |            |        | claim to checks applying UNIFORMLY to every prefab, naming H6's    |
// |         |            |        | per-marker material/property check (in BuildAgentObjects, not      |
// |         |            |        | here) as the precedent for a per-slot check's correct home.        |
// | 1.6     | 2026-08-16 | —      | AR round 4, Medium/Low findings M20/M21/M22/L12. M20:               |
// |         |            |        | ValidateWiring's rotation check and InstantiatePrefab's root-       |
// |         |            |        | neutrality check both used `!= Quaternion.identity`, which is       |
// |         |            |        | sign-sensitive under quaternion double cover — a legitimately-       |
// |         |            |        | unrotated transform whose composed quaternion lands on the          |
// |         |            |        | negative representative of identity, (0,0,0,-1), was falsely        |
// |         |            |        | rejected, and the rejection message (which printed only              |
// |         |            |        | eulerAngles) showed (0,0,0) — "must be identity" next to a value     |
// |         |            |        | that already read as identity. Both sites now call the new           |
// |         |            |        | IsIdentityRotation (|Dot(rotation, identity)| >= 1 - tolerance,       |
// |         |            |        | tolerance 1e-5), and both rejection messages now print the raw       |
// |         |            |        | (x, y, z, w) components alongside eulerAngles so the message can     |
// |         |            |        | never show a value that contradicts its own stated requirement.      |
// |         |            |        | M21: RenderBall's comment claiming the ball's "real physics          |
// |         |            |        | height rides on model.WorldPosition.y already" fell out of sync      |
// |         |            |        | with round 3's M17 (BallRenderModel.cs / MatchRenderProjection.cs,   |
// |         |            |        | this round's other two M21 sites) — false for any raw height below   |
// |         |            |        | the drawn radius, i.e. most of a match including rest; corrected to  |
// |         |            |        | state the floor. M22: the type-doc prefab-contract clause 2a         |
// |         |            |        | enumeration described the marking circle and the possession ring     |
// |         |            |        | identically to the marking spot/agent marker/ball shadow ("FLAT,     |
// |         |            |        | unit radius") despite PitchMarkingKind.Circle's own doc requiring    |
// |         |            |        | it STROKED (an outline) against PitchMarkingKind.Spot's FILLED —     |
// |         |            |        | "a renderer that collapsed the two would draw a solid centre         |
// |         |            |        | circle" — and the possession ring has the identical silent failure   |
// |         |            |        | mode. Clause 2a now names which of the seven FLAT slots must be      |
// |         |            |        | stroked (marking circle, possession ring) and which filled (the      |
// |         |            |        | rest), citing PitchMarkingKind's own docs as authority. L12: Update  |
// |         |            |        | had no H5-style guard — MatchRenderProjection.ProjectAgents/         |
// |         |            |        | ProjectBall are fail-loud on a non-finite coordinate and nothing     |
// |         |            |        | upstream refuses one (FrameInterpolator deliberately propagates a    |
// |         |            |        | non-finite position as a discontinuity to snap to), so one bad       |
// |         |            |        | frame threw the same exception every Update forever, with the        |
// |         |            |        | camera and click handler never reached again — unresponsive but      |
// |         |            |        | not disabled. RenderAgents/RenderBall/UpdateCamera/HandleClick are   |
// |         |            |        | now wrapped in a try/catch routing to RejectWiring, mirroring H5's    |
// |         |            |        | own Awake catch exactly; the comment beside it states explicitly     |
// |         |            |        | why this is the once-per-frame MonoBehaviour entry point rather      |
// |         |            |        | than a per-frame INNER loop in the FR-CS-069 sense, the same         |
// |         |            |        | carve-out H5's Awake comment already claims.                        |
#endregion
