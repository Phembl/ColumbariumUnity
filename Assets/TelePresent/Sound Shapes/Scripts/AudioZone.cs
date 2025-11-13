/*******************************************************
Product   - Sound Shapes
Publisher - TelePresent Games
            http://TelePresentGames.dk
Author    - Martin Hansen
Created   - 2025
(c) 2025 Martin Hansen. All rights reserved.
*******************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TelePresent.SoundShapes
{
    [ExecuteInEditMode]
    public class AudioZone : MonoBehaviour
    {
        public enum ZoneMode     { Shape, Mesh, MultiEmitter }
        public enum TrackingMode { Tag,   Object }

        #region Inspector Fields ---------------------------------------------------------------

        [Header("Zone Mode")]
        public ZoneMode mode = ZoneMode.Shape;

        public event Action<bool> OnTrackingStateChanged;
        private bool _wasInRange;

        public List<Vector3> points = new();

        public bool closedShape = true;

        public bool freehandMode;

        public List<MeshFilter> meshFilters = new();

        public bool isInRange;

        public Vector3 meshAudioOffset = Vector3.zero;

        public List<SoundShapes_CachedMeshData> cachedMeshDataList = new();

        public List<Vector3> multiEmitterPoints = new();

        public AudioSource audioSource;

        public GameObject positionTarget;

        public GameObject soundShapeTracker;

        public bool requireAudioSourceComponent = true;

        public float triggerDistanceOverride;

        public bool flipTriggerDistance;

        public bool enableDualAudio;

        #region Occlusion ----------------------------------------------------------------------

        [Header("Occlusion Settings")]
        public bool   enableOcclusion;
        public LayerMask occlusionLayer = 1 << 0;
        public bool   occlusion2DMode;
        [Range(0f,180f)]
        public float  occlusion2DSpreadDegrees = 30f;
        [Range(0f, 1f)] public float occlusionVolumeMultiplier = 0.5f;
        public float occlusionLowPassCutoff = 7000f;
        public float defaultLowPassCutoff   = 22000f;
        public int   occlusionResolution    = 4;
        public float occlusionSampleRadius  = 0.5f;

        #endregion

        #region Tracking -----------------------------------------------------------------------

        [Header("Tracking Settings")]
        public TrackingMode trackingMode = TrackingMode.Tag;
        public string   trackingTag     = "Player";
        public Transform trackingObject;

        #endregion

        [Header("Debug & Preview")]
        public bool editorPreview;
        public bool debugMode;

        [HideInInspector] public bool  shouldTrack = true;
        [HideInInspector] public bool  disabledAudioSourceForMultiEmitter;
        [HideInInspector] public float occlusionRatio;

        #endregion
        /* ============================================================================ */

        #region Runtime Caches & Helpers --------------------------------------------------------

        public  Transform cachedTransform;

        private float   _triggerDistance;
        private float   _triggerDistSqr;
        public  Vector3 currentTargetPosition;

        private GameObject _prevSoundShapeTracker;
        private GameObject _targetObj;

        private bool _isMultiEmitter;
        private bool _editingPreview;

        private static Material lineMaterial;
        private static readonly int Cull = Shader.PropertyToID("_Cull");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");

        // Re‑used candidate buffers (cleared each frame)
        private readonly List<Candidate> _meshCandidates  = new(16);
        private readonly List<Candidate> _dualCandidates  = new(8);

        // Helper components (always kept valid by EnsureHandlers)
        public  AudioZoneDualAudio           DualAudioHandler;
        public  AudioZoneMultiEmitterHandler MultiEmitterHandler;

        #endregion
        /* ============================================================================ */

        #region Unity Lifecycle ----------------------------------------------------------------

        private void Awake()
        {
            cachedTransform      = transform;
            EnsureHandlers();
            _prevSoundShapeTracker = soundShapeTracker;
        }
        
        
#if UNITY_EDITOR
        private void OnEnable()
        {
            EditorApplication.update               += EditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update               -= EditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StopPreview();
        }
#endif

        private void Start()
        {
            if (audioSource && !audioSource.GetComponent<AudioLowPassFilter>())
            {
                var lpf = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
                lpf.cutoffFrequency = defaultLowPassCutoff;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                AudioZoneGeometry.GenerateMeshData(this);

            if (!soundShapeTracker && audioSource)
                soundShapeTracker = audioSource.gameObject;

            if (soundShapeTracker != _prevSoundShapeTracker)
            {
                if (soundShapeTracker)
                {
                    positionTarget = soundShapeTracker;
                    audioSource    = soundShapeTracker.GetComponent<AudioSource>();
                }
                else
                {
                    positionTarget = null;
                    audioSource    = null;
                }
                _prevSoundShapeTracker = soundShapeTracker;
            }
        }

        private void EditorUpdate()
        {
            if (!Application.isPlaying && editorPreview && audioSource)
            {
                Update();                                   // reuse runtime logic
                SceneAudioListenerManager.UpdateListener();
                EnsureLowPass();
                DisableMainListener();
                SceneView.RepaintAll();
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && editorPreview)
                StopPreview();
        }

        private void EnsureLowPass()
        {
            if (audioSource && !audioSource.GetComponent<AudioLowPassFilter>())
            {
                var lpf = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
                lpf.cutoffFrequency = defaultLowPassCutoff;
            }
        }

        private void DisableMainListener()
        {
            if (!Camera.main) return;
            var mainListener = Camera.main.GetComponent<AudioListener>();
            if (mainListener && mainListener.enabled)
            {
                mainListener.enabled = false;
                _editingPreview      = true;
            }
        }
#endif
        #endregion
        /* ============================================================================ */

        #region Main Update Loop ---------------------------------------------------------------

        private void Update()
        {
            EnsureHandlers();                 // make sure helpers live after reloads

#if UNITY_EDITOR
            if (!Application.isPlaying && !editorPreview)
                return;
#endif

            isInRange = false;

            /* ─── 1. tracker override sync ────────────────────────────────────── */
            if (soundShapeTracker != _prevSoundShapeTracker)
                SyncTrackerReference();

            _isMultiEmitter = mode == ZoneMode.MultiEmitter;
            if (!_isMultiEmitter && (!positionTarget || !shouldTrack))
                return;

            if (!_isMultiEmitter && audioSource && Application.isPlaying && !audioSource.isPlaying)
                return;

            /* ─── 2. listener position ───────────────────────────────────────── */
#if UNITY_EDITOR
            if (editorPreview && !Application.isPlaying &&
                SceneView.lastActiveSceneView?.camera)
            {
                currentTargetPosition =
                    SceneView.lastActiveSceneView.camera.transform.position;
            }
            else
#endif
            {
                if (trackingMode == TrackingMode.Tag)
                {
                    _targetObj = GetClosestObjectByTag(trackingTag);
                    if (!_targetObj) return;
                    currentTargetPosition = _targetObj.transform.position;
                }
                else if (trackingObject)
                {
                    currentTargetPosition = trackingObject.position;
                }
                else return;
            }

            /* ─── 3. mode diverge ─────────────────────────────────────────────── */
            if (_isMultiEmitter)
            {
                if (!Application.isPlaying)
                    EnterEditorMultiEmitter();
                else
                    MultiEmitterHandler.UpdateMultiEmitterLogic();
                return;
            }

            /* ─── 4. ensure audible & trigger radius ─────────────────────────── */
            if (audioSource && Mathf.Approximately(audioSource.volume, 0f))
                audioSource.volume = 1f;

            _triggerDistance = triggerDistanceOverride > 0f
                ? triggerDistanceOverride
                : (audioSource ? audioSource.maxDistance : 0f);
            _triggerDistSqr  = _triggerDistance * _triggerDistance;

            /* ─── 5. zone‑specific candidate position ────────────────────────── */
            Vector3 primaryPos = positionTarget ? positionTarget.transform.position
                                                : transform.position;

            switch (mode)
            {
                case ZoneMode.Shape:
                    ProcessShapeMode(ref primaryPos);
                    break;
                case ZoneMode.Mesh:
                    ProcessMeshMode(ref primaryPos);
                    break;
            }

            /* ─── 6. apply / dual‑audio / occlusion ──────────────────────────── */
            if (isInRange)
            {
                if (enableDualAudio && mode == ZoneMode.Shape && points.Count >= 2)
                    ProcessDualAudioForShape();
                else if (enableDualAudio && mode == ZoneMode.Mesh && meshFilters.Count > 0)
                    ProcessDualAudioForMesh();
                else
                    ApplyPrimaryPosition(primaryPos);

                if (enableOcclusion)
                    UpdateOcclusionForAll();
                else
                    DualAudioHandler?.ApplyFallbackSecondaryVolume();
            }
            else
            {
                DualAudioHandler?.CleanupSecondaryAudio();
            }

            /* ─── 7. range‑changed event ─────────────────────────────────────── */
            if (isInRange != _wasInRange)
            {
                OnTrackingStateChanged?.Invoke(isInRange);
            }
            _wasInRange = isInRange;
        }
        #endregion
        /* ============================================================================ */

        #region Initialiser/Utility ------------------------------------------------------------

        /// <summary>Guarantees helper instances exist after a domain reload.</summary>
        private void EnsureHandlers()
        {
            DualAudioHandler    ??= new AudioZoneDualAudio(this);
            MultiEmitterHandler ??= new AudioZoneMultiEmitterHandler(this);
        }

        private void SyncTrackerReference()
        {
            if (soundShapeTracker)
            {
                positionTarget = soundShapeTracker;
                audioSource    = soundShapeTracker.GetComponent<AudioSource>();
            }
            else
            {
                positionTarget = null;
                audioSource    = null;
            }
            _prevSoundShapeTracker = soundShapeTracker;
        }
        #endregion
        /* ============================================================================ */

        #region Zone Processing ---------------------------------------------------------------

        private void ProcessShapeMode(ref Vector3 primaryPos)
        {
            if (closedShape && AudioZoneGeometry.IsPointInsideZone(
                    currentTargetPosition, points, cachedTransform))
            {
                primaryPos = AudioZoneGeometry.GetConstrainedPosition(
                    currentTargetPosition, points, closedShape, cachedTransform);
                isInRange  = true;
                return;
            }

            Vector3 nearest = AudioZoneGeometry.GetClosestPointOnPerimeter(
                currentTargetPosition, points, closedShape, cachedTransform);

            if ((nearest - currentTargetPosition).sqrMagnitude <= _triggerDistSqr)
            {
                primaryPos = nearest;
                isInRange  = true;
            }
        }

        private void ProcessMeshMode(ref Vector3 primaryPos)
        {
            _meshCandidates.Clear();

            foreach (var mf in meshFilters)
            {
                if (!mf || !mf.sharedMesh) continue;

                var cache = cachedMeshDataList.Find(x => x.meshReference == mf.sharedMesh);
                var verts = cache != null ? cache.vertices   : mf.sharedMesh.vertices;
                var tris  = cache != null ? cache.triangles  : mf.sharedMesh.triangles;

                var worldVerts = new Vector3[verts.Length];
                for (int i = 0; i < verts.Length; i++)
                    worldVerts[i] = mf.transform.TransformPoint(verts[i]);

                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vector3 cand = AudioZoneGeometry.ClosestPointOnTriangle(
                        worldVerts[tris[i]],
                        worldVerts[tris[i + 1]],
                        worldVerts[tris[i + 2]],
                        currentTargetPosition);

                    float dSqr = (currentTargetPosition - cand).sqrMagnitude;
                    if (dSqr <= _triggerDistSqr)
                        _meshCandidates.Add(new Candidate(cand, dSqr));
                }
            }

            if (_meshCandidates.Count > 0)
            {
                _meshCandidates.Sort((a, b) => a.DistSqr.CompareTo(b.DistSqr));
                primaryPos = _meshCandidates[0].Position + meshAudioOffset;
                isInRange  = true;
            }
        }

        #endregion
        /* ============================================================================ */

        #region Dual Audio Helpers -------------------------------------------------------------

        private class Candidate
        {
            public readonly Vector3 Position;
            public readonly float   DistSqr;
            public readonly int     SegmentIndex;
            public Candidate(Vector3 pos, float d)
            {
                Position = pos;  DistSqr = d;
            }
        }

        private void ProcessDualAudioForShape()
        {
            _dualCandidates.Clear();

            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                if (!closedShape && next == 0) break;

                Vector3 p1   = cachedTransform.TransformPoint(points[i]);
                Vector3 p2   = cachedTransform.TransformPoint(points[next]);
                Vector3 proj = AudioZoneGeometry.ProjectPointOnLineSegment(
                                    p1, p2, currentTargetPosition);

                float dSqr = (currentTargetPosition - proj).sqrMagnitude;
                if (dSqr <= _triggerDistSqr)
                    _dualCandidates.Add(new Candidate(proj, dSqr));
            }

            ApplyDualCandidates();
        }

        private void ProcessDualAudioForMesh()
        {
            _dualCandidates.Clear();

            foreach (var mf in meshFilters)
            {
                if (!mf || !mf.sharedMesh) continue;

                var cache = cachedMeshDataList.Find(x => x.meshReference == mf.sharedMesh);
                var verts = cache != null ? cache.vertices : mf.sharedMesh.vertices;
                var tris  = cache != null ? cache.triangles : mf.sharedMesh.triangles;

                var worldVerts = new Vector3[verts.Length];
                for (int i = 0; i < verts.Length; i++)
                    worldVerts[i] = mf.transform.TransformPoint(verts[i]);

                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vector3 cand = AudioZoneGeometry.ClosestPointOnTriangle(
                        worldVerts[tris[i]],
                        worldVerts[tris[i + 1]],
                        worldVerts[tris[i + 2]],
                        currentTargetPosition);

                    float dSqr = (currentTargetPosition - cand).sqrMagnitude;
                    if (dSqr <= _triggerDistSqr)
                        _dualCandidates.Add(new Candidate(
                            cand + meshAudioOffset, dSqr));
                }
            }

            ApplyDualCandidates(isMesh: true);
        }

        private void ApplyDualCandidates(bool isMesh = false)
        {
            if (_dualCandidates.Count < 2)
            {
                if (_dualCandidates.Count == 1)
                    SingleDualCandidate(_dualCandidates[0]);
                else
                    DualAudioHandler?.CleanupSecondaryAudio();
                return;
            }

            _dualCandidates.Sort((a, b) => a.DistSqr.CompareTo(b.DistSqr));

            Candidate primary   = null;
            Candidate secondary = null;
            bool      found     = false;

            for (int m = 0; m < _dualCandidates.Count && !found; m++)
            {
                for (int n = m + 1; n < _dualCandidates.Count; n++)
                {
                    if ((_dualCandidates[m].Position -
                         _dualCandidates[n].Position).sqrMagnitude < 0.0001f)
                        continue;

                    float angle = Vector3.Angle(
                        _dualCandidates[m].Position - currentTargetPosition,
                        _dualCandidates[n].Position - currentTargetPosition);

                    if (angle > 70f)
                    {
                        primary   = _dualCandidates[m];
                        secondary = _dualCandidates[n];
                        found     = true;
                        break;
                    }
                }
            }

            if (found)
            {
                if (isMesh)
                {
                    if (positionTarget) positionTarget.transform.position = primary.Position;
                    if (audioSource)    audioSource.transform.position    = primary.Position;
                }
                else if (audioSource)
                {
                    audioSource.transform.position = primary.Position;
                }

                if (!DualAudioHandler.SecondaryAudioSource)
                    DualAudioHandler.HandleSecondaryAudio();

                if (DualAudioHandler.SecondaryAudioObj)
                    DualAudioHandler.SecondaryAudioObj.transform.position =
                        secondary.Position;
            }
            else
            {
                SingleDualCandidate(_dualCandidates[0]);
            }
        }

        private void SingleDualCandidate(Candidate cand)
        {
            if (mode == ZoneMode.Mesh && positionTarget)
                positionTarget.transform.position = cand.Position;

            if (audioSource)
                audioSource.transform.position = cand.Position;

            DualAudioHandler?.CleanupSecondaryAudio();
        }

        #endregion
        /* ============================================================================ */

        #region Occlusion / Primary Pos --------------------------------------------------------

        private void ApplyPrimaryPosition(Vector3 pos)
        {
            if (positionTarget)
                positionTarget.transform.position = pos;

            if (audioSource)
                audioSource.transform.position     = pos;

            DualAudioHandler?.CleanupSecondaryAudio();
        }

        private void UpdateOcclusionForAll()
        {
            if (!enableOcclusion) return;

            AudioZoneOcclusion.UpdateOcclusion(
                currentTargetPosition,
                positionTarget.transform.position,
                audioSource,
                this);

            if (DualAudioHandler.SecondaryAudioSource)
                AudioZoneOcclusion.UpdateOcclusion(
                    currentTargetPosition,
                    DualAudioHandler.SecondaryAudioSource.transform.position,
                    DualAudioHandler.SecondaryAudioSource,
                    this);
        }

        #endregion
        /* ============================================================================ */

        #region Utility Methods ---------------------------------------------------------------

        private GameObject GetClosestObjectByTag(string objectTag)
        {
            var objs = GameObject.FindGameObjectsWithTag(objectTag);
            if (objs.Length == 0) return null;

            GameObject closest = objs[0];
            float minSqr = (closest.transform.position - cachedTransform.position).sqrMagnitude;

            for (int i = 1; i < objs.Length; i++)
            {
                float d = (objs[i].transform.position - cachedTransform.position).sqrMagnitude;
                if (d < minSqr)
                {
                    minSqr  = d;
                    closest = objs[i];
                }
            }
            return closest;
        }

        #endregion
        /* ============================================================================ */

        #region Preview & Gizmos & LineMaterial -----------------------------------------------

        public void StopPreview()
        {
            editorPreview = false;

            if (audioSource && audioSource.isPlaying)
                audioSource.Stop();

            DualAudioHandler?.StopAndCleanup();
            MultiEmitterHandler?.CleanupAll();

            if (disabledAudioSourceForMultiEmitter && audioSource)
            {
                audioSource.enabled                = true;
                disabledAudioSourceForMultiEmitter = false;
            }

            if (_editingPreview && Camera.main)
            {
                var mainListener = Camera.main.GetComponent<AudioListener>();
                if (mainListener) mainListener.enabled = true;
                _editingPreview = false;
            }

            var lpf = audioSource ? audioSource.GetComponent<AudioLowPassFilter>() : null;
            if (lpf) lpf.cutoffFrequency = defaultLowPassCutoff;

            SceneAudioListenerManager.DisableListener();
        }

#if UNITY_EDITOR
        private void OnRenderObject()
        {
            if (!audioSource || !debugMode) return;
            if (mode == ZoneMode.MultiEmitter || !audioSource.isPlaying) return;

            Vector3 target;
            if (editorPreview && !Application.isPlaying &&
                SceneView.lastActiveSceneView && SceneView.lastActiveSceneView.camera)
                target = SceneView.lastActiveSceneView.camera.transform.position;
            else if (trackingMode == TrackingMode.Object && trackingObject)
                target = trackingObject.position;
            else
            {
                var obj = GetClosestObjectByTag(trackingTag);
                if (!obj) return;
                target = obj.transform.position;
            }

            float trig = triggerDistanceOverride > 0f
                ? triggerDistanceOverride
                : (audioSource ? audioSource.maxDistance : 0f);
            float trigSqr = trig * trig;

            CreateLineMaterial();
            lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            Vector3 pos = audioSource.transform.position;
            float cross = .25f;
            GL.Begin(GL.LINES);
            GL.Color(Color.cyan);
            GL.Vertex(pos + Vector3.right * cross);   GL.Vertex(pos - Vector3.right * cross);
            GL.Vertex(pos + Vector3.up    * cross);   GL.Vertex(pos - Vector3.up    * cross);
            GL.Vertex(pos + Vector3.forward * cross); GL.Vertex(pos - Vector3.forward * cross);
            GL.End();

            if (enableOcclusion && (target - pos).sqrMagnitude <= trigSqr)
            {
                GL.Begin(GL.LINES);

                if (!occlusion2DMode)                                                 // 3‑D
                {
                    var offs = AudioZoneOcclusion.GetOcclusionOffsets(
                        target, pos, occlusionSampleRadius, occlusionResolution);

                    foreach (var off in offs)
                    {
                        Vector3 p0 = target + off;
                        Vector3 p1 = pos    + off;
                        Color   col = Hit3D(p0, p1) ? Color.red : Color.yellow;
                        GL.Color(col); GL.Vertex(p0); GL.Vertex(p1);
                    }
                }
                else                                                                  // 2‑D
                {
                    Vector2 src2 = new(pos.x, pos.y);
                    Vector2 dst2 = new(target.x, target.y);
                    float   dist = Vector2.Distance(src2, dst2);

                    var dirs = Get2DDirections(src2, dst2,
                               occlusion2DSpreadDegrees, occlusionResolution);

                    foreach (var d in dirs)
                    {
                        Vector3 p0 = pos;
                        Vector3 p1 = pos + new Vector3(d.x, d.y, 0f) * dist;
                        Color   col = Hit2D(src2, d, dist) ? Color.red : Color.yellow;
                        GL.Color(col); GL.Vertex(p0); GL.Vertex(p1);
                    }
                }
                GL.End();
            }
            GL.PopMatrix();
        }

        private bool Hit3D(Vector3 p0, Vector3 p1)
        {
            return Physics.Raycast(p0, (p1 - p0).normalized,
                                   out var h,
                                   Vector3.Distance(p0, p1),
                                   occlusionLayer) &&
                   !(mode == ZoneMode.Mesh && meshFilters != null &&
                     meshFilters.Exists(mf => mf && h.collider.gameObject == mf.gameObject));
        }

        private bool Hit2D(Vector2 origin, Vector2 dir, float dist)
        {
            RaycastHit2D h = Physics2D.Raycast(origin, dir, dist, occlusionLayer);
            return h.collider != null;
        }

        private static List<Vector2> Get2DDirections(Vector2 src, Vector2 tgt,
                                                     float spreadDeg, int res)
        {
            Vector2 baseDir = (tgt - src).normalized;
            float half = spreadDeg * 0.5f * Mathf.Deg2Rad;
            res = Mathf.Max(1, res);
            var list = new List<Vector2>(res);

            if (res == 1) { list.Add(baseDir); return list; }
            for (int i = 0; i < res; i++)
            {
                float t = (float)i / (res - 1);
                float a = Mathf.Lerp(-half, half, t);
                float c = Mathf.Cos(a);
                float s = Mathf.Sin(a);
                Vector2 d = new Vector2(c * baseDir.x - s * baseDir.y,
                                         s * baseDir.x + c * baseDir.y);
                list.Add(d.normalized);
            }
            return list;
        }
        private void OnDrawGizmos()
        {
            if (Selection.activeGameObject != gameObject)
                return;

            if (mode == ZoneMode.Shape && points.Count > 0)
            {
                Gizmos.color = Color.green;
                for (var i = 0; i < points.Count - 1; i++)
                    Gizmos.DrawLine(cachedTransform.TransformPoint(points[i]),
                        cachedTransform.TransformPoint(points[i + 1]));
                if (points.Count > 2 && closedShape)
                    Gizmos.DrawLine(cachedTransform.TransformPoint(points[^1]),
                        cachedTransform.TransformPoint(points[0]));
            }

            if (mode == ZoneMode.MultiEmitter && multiEmitterPoints != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var pt in multiEmitterPoints)
                    Gizmos.DrawSphere(cachedTransform.TransformPoint(pt), 0.1f);
            }

            if (audioSource)
            {
                var offsetPerimeter = AudioZoneGeometry.GetOffsetPerimeter(this);
                if (offsetPerimeter.Count > 1)
                {
                    Gizmos.color = Color.red;
                    for (var i = 0; i < offsetPerimeter.Count; i++)
                    {
                        var current = offsetPerimeter[i];
                        var next = offsetPerimeter[(i + 1) % offsetPerimeter.Count];
                        Gizmos.DrawLine(current, next);
                    }
                }
            }
        }
#endif

        private static void CreateLineMaterial()
        {
            if (!lineMaterial)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                lineMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                lineMaterial.SetInt(SrcBlend, (int)BlendMode.SrcAlpha);
                lineMaterial.SetInt(DstBlend, (int)BlendMode.OneMinusSrcAlpha);
                lineMaterial.SetInt(Cull, (int)CullMode.Off);
                lineMaterial.SetInt(ZWrite, 0);
            }
        }

        
        private void EnterEditorMultiEmitter()
        {
            EnsureHandlers();

            if (audioSource)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }

            disabledAudioSourceForMultiEmitter = true;
            DualAudioHandler.StopAndCleanup();
            MultiEmitterHandler.UpdateMultiEmitterLogic();
        }
        
        #endregion
        /* ============================================================================ */

  #region Public API ---------------------------------------------------------------------

        public void SetTarget(Transform tgt)
        {
            if (!tgt)
            {
                Debug.LogWarning("SetTarget called with null Transform.");
                return;
            }
            trackingObject = tgt;
        }

        public void ToggleClosedLoop(bool closed)
        {
            if (mode == ZoneMode.Shape)
                closedShape = closed;
            else
                Debug.LogWarning("Closed loop only valid in Shape mode.");
        }

        public void ToggleShouldTrack(bool track) => shouldTrack = track;

        public void SetTrackingTag(string newTag)
        {
            if (string.IsNullOrEmpty(newTag))
                Debug.LogWarning("SetTrackingTag called with null/empty tag.");
            else
                trackingTag = newTag;
        }

        public void AddMeshTarget(MeshFilter mf)
        {
            if (!mf)
            {
                Debug.LogWarning("AddMeshTarget called with null MeshFilter.");
                return;
            }
            meshFilters.Add(mf);
            AudioZoneGeometry.GenerateMeshData(this);
        }

        public void ClearMeshTargets()
        {
            meshFilters.Clear();
            cachedMeshDataList.Clear();
        }

        public void RemoveMultiPoint(int idx)
        {
            if (idx >= 0 && idx < multiEmitterPoints.Count)
                multiEmitterPoints.RemoveAt(idx);
            else
                Debug.LogWarning($"RemoveMultiPoint invalid index: {idx}");
        }

        public void AddMultiPoint(Vector3 loc)            => multiEmitterPoints.Add(loc);
        public void ClearMultiPoints()                    => multiEmitterPoints.Clear();

        public void SetMultiPointLocation(int i, Vector3 loc)
        {
            if (i >= 0 && i < multiEmitterPoints.Count)
                multiEmitterPoints[i] = loc;
            else
                Debug.LogWarning($"SetMultiPointLocation invalid index: {i}");
        }

        public void SetTrackingMode(TrackingMode m)       => trackingMode = m;

        public void PopulateMultiPoints(List<Transform> ts)
        {
            if (ts == null) { Debug.LogWarning("PopulateMultiPoints null list."); return; }
            foreach (var t in ts) if (t) multiEmitterPoints.Add(t.position);
        }

        public void PopulateMultiPoints(List<Vector3> vs)
        {
            if (vs == null) { Debug.LogWarning("PopulateMultiPoints null list."); return; }
            multiEmitterPoints.AddRange(vs);
        }

        public void AddShapePoint(Vector3 p)              => points.Add(p);
        public void RemoveShapePoint(int i)
        {
            if (i >= 0 && i < points.Count) points.RemoveAt(i);
            else Debug.LogWarning($"RemoveShapePoint invalid index: {i}");
        }
        public void ClearShapePoints()                    => points.Clear();

        public void SetShapePointLocation(int i, Vector3 p)
        {
            if (i >= 0 && i < points.Count) points[i] = p;
            else Debug.LogWarning($"SetShapePointLocation invalid index: {i}");
        }

        public void PopulateShapePoints(List<Transform> ts)
        {
            if (ts == null) { Debug.LogWarning("PopulateShapePoints null list."); return; }
            foreach (var t in ts) if (t) points.Add(t.position);
        }

        public void PopulateShapePoints(List<Vector3> vs)
        {
            if (vs == null) { Debug.LogWarning("PopulateShapePoints null list."); return; }
            points.AddRange(vs);
        }
        #endregion
    }
}
