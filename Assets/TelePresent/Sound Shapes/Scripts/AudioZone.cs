/*******************************************************
Product - Sound Shapes
  Publisher - TelePresent Games
              http://TelePresentGames.dk
  Author    - Martin Hansen
  Created   - 2025
  (c) 2025 Martin Hansen. All rights reserved.
/*******************************************************/

using UnityEngine;
using System.Collections.Generic;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TelePresent.SoundShapes
{

    [ExecuteInEditMode]
    public class AudioZone : MonoBehaviour
    {
        public enum ZoneMode { Shape, Mesh, MultiEmitter }

        [Header("Zone Mode")]
        [Tooltip("Mode for defining the audio zone.")]
        public ZoneMode mode = ZoneMode.Shape;
        [HideInInspector]
        public bool shouldTrack = true;
        [Tooltip("Anchor points defining the audio zone shape (in local space).")]
        public List<Vector3> points = new List<Vector3>();
        [Tooltip("If true, the shape will be closed.")]
        public bool closedShape = true;
        [Tooltip("Toggle freehand drawing mode in the Scene view.")]
        public bool freehandMode = false;

        [Tooltip("List of MeshFilters whose meshes define the audio zone.")]
        public List<MeshFilter> meshFilters = new List<MeshFilter>();
        [Tooltip("Offset to apply to the AudioSource in Mesh mode.")]
        public Vector3 meshAudioOffset = Vector3.zero;

        [Header("Cached Mesh Data (for non-readable meshes)")]
        [Tooltip("Pre-cached vertex and triangle data for each mesh (to avoid runtime read/write errors).")]
        public List<SoundShapes_CachedMeshData> cachedMeshDataList = new List<SoundShapes_CachedMeshData>();

        [Tooltip("Locations of the multi-emitter points (in local space).")]
        public List<Vector3> multiEmitterPoints = new List<Vector3>();

        [Tooltip("AudioSource that will follow along the zone.")]
        public AudioSource audioSource;
        [Tooltip("Override the AudioSource maxDistance for the trigger perimeter. If 0 or less, audioSource.maxDistance is used.")]
        public float triggerDistanceOverride = 0f;
        [Tooltip("Flip the trigger offset (multiplied by -1).")]
        public bool flipTriggerDistance = false;
        [Tooltip("Allow dual audio sources when the player is between two points.")]
        public bool enableDualAudio = false;

        [Header("Occlusion Settings")]
        [Tooltip("Enable occlusion effect for the audio zone.")]
        public bool enableOcclusion = false;
        [Tooltip("Layer mask for occluding objects.")]
        public LayerMask occlusionLayer = 1 << 0;
        [Tooltip("Volume multiplier when occluded (0 to 1).")]
        public float occlusionVolumeMultiplier = 0.5f;
        [Tooltip("Low pass filter cutoff frequency when occluded.")]
        public float occlusionLowPassCutoff = 7000;
        [Tooltip("Default low pass filter cutoff frequency.")]
        public float defaultLowPassCutoff = 22000f;
        [Tooltip("How many raycasts to distribute along the occlusion sampling circle.")]
        public int occlusionResolution = 4;
        [Tooltip("Distance between occlusion raycasts on the sampling circle.")]
        public float occlusionSampleRadius = 0.5f;

        public enum TrackingMode { Tag, Object }

        [Header("Tracking Settings")]
        [Tooltip("Determines whether the zone tracks by tag or by specific object.")]
        public TrackingMode trackingMode = TrackingMode.Tag;
        [Tooltip("Tag to track when in Tag mode.")]
        public string trackingTag = "Player";
        [Tooltip("Specific object to track when in Object mode.")]
        public Transform trackingObject;

        [Header("Debug & Preview")]
        [Tooltip("Use the Scene view camera as the target in editor preview.")]
        public bool editorPreview = false;
        [Tooltip("Show debug visualizations in Scene and Game views.")]
        public bool debugMode = false;

        public bool disabledAudioSourceForMultiEmitter;

        bool showTriggerSettingsFoldout;

        [HideInInspector]
        public Transform cachedTransform;

        public AudioZoneDualAudio dualAudioHandler;
        public AudioZoneMultiEmitterHandler multiEmitterHandler;

        static Material _lineMaterial;

        // Flag to store if the main AudioListener was originally enabled.
        private bool mainListenerWasEnabled = false;

        // Trigger-based detection field.
        private Transform playerTransform;

        public Vector3 currentTargetPosition;

        void Awake()
        {
            cachedTransform = transform;
            dualAudioHandler = new AudioZoneDualAudio(this);
            if (multiEmitterHandler == null)
                multiEmitterHandler = new AudioZoneMultiEmitterHandler(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                AudioZoneGeometry.GenerateMeshData(this);
            }
        }
#endif


        void Start()
        {
            if (audioSource != null && audioSource.GetComponent<AudioLowPassFilter>() == null)
            {
                audioSource.gameObject.AddComponent<AudioLowPassFilter>();
                audioSource.GetComponent<AudioLowPassFilter>().cutoffFrequency = defaultLowPassCutoff;
                AudioZoneGeometry.GenerateMeshData(this);
            }
        }

#if UNITY_EDITOR
        void OnEnable()
        {
            if (dualAudioHandler == null)
                dualAudioHandler = new AudioZoneDualAudio(this);
            if (multiEmitterHandler == null)
                multiEmitterHandler = new AudioZoneMultiEmitterHandler(this);
            EditorApplication.update += EditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StopPreview();
        }

        void EditorUpdate()
        {
            if (!Application.isPlaying && editorPreview)
            {
                if (audioSource == null)
                    return;
                Update();

                // Update the single scene audio listener.
                SceneAudioListenerManager.UpdateListener();

                // Ensure the audio source has a low pass filter.
                if (audioSource.GetComponent<AudioLowPassFilter>() == null)
                {
                    audioSource.gameObject.AddComponent<AudioLowPassFilter>();
                    audioSource.GetComponent<AudioLowPassFilter>().cutoffFrequency = defaultLowPassCutoff;
                    defaultLowPassCutoff = audioSource.GetComponent<AudioLowPassFilter>().cutoffFrequency;
                }

                // Disable the main camera's AudioListener if it was enabled.
                if (Camera.main != null)
                {
                    AudioListener mainListener = Camera.main.GetComponent<AudioListener>();
                    if (mainListener != null && mainListener.enabled)
                    {
                        mainListenerWasEnabled = true;
                        mainListener.enabled = false;
                    }
                }

                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// Called when the play mode state changes. Disables editor preview when exiting edit mode.
        /// </summary>
        /// <param name="state">The new play mode state.</param>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (editorPreview)
                {
                    StopPreview();
                }
            }
        }
#endif

        // A helper class to store candidate info.
        private class Candidate
        {
            public Vector3 position;
            public float distSqr;
            public int segmentIndex;

            public Candidate(Vector3 pos, float dist, int seg)
            {
                position = pos;
                distSqr = dist;
                segmentIndex = seg;
            }
        }

        void Update()
        {
            if (audioSource == null || !shouldTrack)
                return;

            if (!Application.isPlaying && !editorPreview)
                return;

            if (Application.isPlaying && !audioSource.isPlaying)
                return;

            GameObject targetObj = null;
#if UNITY_EDITOR
            if (editorPreview && !Application.isPlaying && SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                currentTargetPosition = SceneView.lastActiveSceneView.camera.transform.position;
            }
            else
            {
                if (trackingMode == TrackingMode.Tag)
                    targetObj = GetClosestObjectByTag(trackingTag);
                else if (trackingMode == TrackingMode.Object && trackingObject != null)
                    targetObj = trackingObject.gameObject;
                if (targetObj == null)
                    return;
                currentTargetPosition = targetObj.transform.position;
            }
#else
    if (trackingMode == TrackingMode.Tag)
        targetObj = GetClosestObjectByTag(trackingTag);
    else if (trackingMode == TrackingMode.Object && trackingObject != null)
        targetObj = trackingObject.gameObject;
    if (targetObj == null)
        return;
    currentTargetPosition = targetObj.transform.position;
#endif

            // MultiEmitter mode handling.
            if (mode == ZoneMode.MultiEmitter)
            {
                if (!Application.isPlaying) // Editor preview mode.
                {
                    audioSource.Stop();
                    audioSource.enabled = false;
                    disabledAudioSourceForMultiEmitter = true;
                    dualAudioHandler.StopAndCleanup();
                    multiEmitterHandler.UpdateMultiEmitterLogic();
                    return;
                }
                else // Play mode.
                {

                    multiEmitterHandler.UpdateMultiEmitterLogic();
                    return;
                }
            }
            else
            {

                // Ensure the main audio source has non-zero volume.
                if (audioSource != null && audioSource.volume == 0f)
                    audioSource.volume = 1.0f;
            }



            float triggerDistance = (triggerDistanceOverride > 0f) ? triggerDistanceOverride : audioSource.maxDistance;

            Vector3 primaryAudioPosition = Vector3.zero;
            bool isInRange = false;

            // ZoneMode.Shape logic.
            if (mode == ZoneMode.Shape && points.Count >= 1)
            {
                if (closedShape && AudioZoneGeometry.IsPointInsideZone(currentTargetPosition, points, transform))
                {
                    primaryAudioPosition = AudioZoneGeometry.GetConstrainedPosition(currentTargetPosition, points, closedShape, transform);
                    isInRange = true;
                }
                else
                {
                    Vector3 closestPerimeterPoint = AudioZoneGeometry.GetClosestPointOnPerimeter(currentTargetPosition, points, closedShape, transform);
                    if (Vector3.Distance(currentTargetPosition, closestPerimeterPoint) <= triggerDistance)
                    {
                        primaryAudioPosition = closestPerimeterPoint;
                        isInRange = true;
                    }
                }
            }
            // ZoneMode.Mesh logic.
            else if (mode == ZoneMode.Mesh && meshFilters != null && meshFilters.Count > 0)
            {
                List<Candidate> candidates = new List<Candidate>();
                float triggerDistSqr = triggerDistance * triggerDistance;

                foreach (MeshFilter mf in meshFilters)
                {
                    if (mf == null || mf.sharedMesh == null)
                        continue;

                    SoundShapes_CachedMeshData cachedData = null;
                    if (cachedMeshDataList == null)
                        AudioZoneGeometry.GenerateMeshData(this);
                    else
                        cachedData = cachedMeshDataList.Find(x => x.meshReference == mf.sharedMesh);

                    Vector3[] vertices;
                    int[] triangles;
                    if (cachedData != null)
                    {
                        vertices = cachedData.vertices;
                        triangles = cachedData.triangles;
                    }
                    else
                    {
                        Mesh mesh = mf.sharedMesh;
                        vertices = mesh.vertices;
                        triangles = mesh.triangles;
                    }

                    Transform mfT = mf.transform;
                    Vector3[] worldVertices = new Vector3[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++)
                        worldVertices[i] = mfT.TransformPoint(vertices[i]);

                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        Vector3 a = worldVertices[triangles[i]];
                        Vector3 b = worldVertices[triangles[i + 1]];
                        Vector3 c = worldVertices[triangles[i + 2]];

                        Vector3 candidatePoint = AudioZoneGeometry.ClosestPointOnTriangle(a, b, c, currentTargetPosition);
                        float distSqr = (currentTargetPosition - candidatePoint).sqrMagnitude;
                        if (distSqr <= triggerDistSqr)
                        {
                            candidates.Add(new Candidate(candidatePoint, distSqr, mf.GetInstanceID()));
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    candidates.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));
                    primaryAudioPosition = candidates[0].position + meshAudioOffset;
                    isInRange = true;
                }
            }

            if (isInRange)
            {
                if (enableDualAudio && mode == ZoneMode.Shape && points.Count >= 2)
                {
                    ProcessDualAudioForShape(currentTargetPosition, triggerDistance);
                }
                else if (enableDualAudio && mode == ZoneMode.Mesh && meshFilters != null && meshFilters.Count > 0)
                {
                    ProcessDualAudioForMesh(currentTargetPosition, triggerDistance);
                }
                else
                {
                    audioSource.transform.position = primaryAudioPosition;
                    dualAudioHandler.CleanupSecondaryAudio();
                }

                if (enableOcclusion)
                {
                    AudioZoneOcclusion.UpdateOcclusion(currentTargetPosition, audioSource.transform.position, audioSource, this);
                    if (dualAudioHandler.secondaryAudioSource != null)
                    {
                        AudioZoneOcclusion.UpdateOcclusion(currentTargetPosition, dualAudioHandler.secondaryAudioSource.transform.position, dualAudioHandler.secondaryAudioSource, this);
                    }
                }
                else if (dualAudioHandler.secondaryAudioSource != null && !enableOcclusion)
                {
                    dualAudioHandler.secondaryAudioSource.volume = dualAudioHandler.baseSecondaryVolume * dualAudioHandler.secondaryFadeFactor;
                }
            }
            else
            {
                dualAudioHandler.CleanupSecondaryAudio();
            }
        }



        private void ProcessDualAudioForShape(Vector3 currentTargetPosition, float triggerDistance)
        {
            float triggerDistSqr = triggerDistance * triggerDistance;
            List<Candidate> candidates = new List<Candidate>();

            // Iterate over segments between consecutive points.
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p1 = transform.TransformPoint(points[i]);
                Vector3 p2 = transform.TransformPoint(points[i + 1]);
                Vector3 proj = AudioZoneGeometry.ProjectPointOnLineSegment(p1, p2, currentTargetPosition);
                float distSqr = (currentTargetPosition - proj).sqrMagnitude;
                if (distSqr <= triggerDistSqr)
                    candidates.Add(new Candidate(proj, distSqr, i));
            }
            // For closed shapes, add the segment from the last point to the first.
            if (closedShape && points.Count > 2)
            {
                int i = points.Count - 1;
                Vector3 p1 = transform.TransformPoint(points[i]);
                Vector3 p2 = transform.TransformPoint(points[0]);
                Vector3 proj = AudioZoneGeometry.ProjectPointOnLineSegment(p1, p2, currentTargetPosition);
                float distSqr = (currentTargetPosition - proj).sqrMagnitude;
                if (distSqr <= triggerDistSqr)
                    candidates.Add(new Candidate(proj, distSqr, i));
            }

            if (candidates.Count >= 2)
            {
                candidates.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));
                bool validPairFound = false;
                Candidate primaryCandidate = null;
                Candidate secondaryCandidate = null;

                for (int m = 0; m < candidates.Count; m++)
                {
                    for (int n = m + 1; n < candidates.Count; n++)
                    {
                        // Skip candidates if their positions are nearly identical.
                        if (Vector3.Distance(candidates[m].position, candidates[n].position) < 0.01f)
                            continue;

                        Vector3 v1 = candidates[m].position - currentTargetPosition;
                        Vector3 v2 = candidates[n].position - currentTargetPosition;
                        float angle = Vector3.Angle(v1, v2);
                        if (angle > 70f)
                        {
                            primaryCandidate = candidates[m];
                            secondaryCandidate = candidates[n];
                            validPairFound = true;
                            break;
                        }
                    }
                    if (validPairFound)
                        break;
                }

                if (validPairFound)
                {
                    audioSource.transform.position = primaryCandidate.position;
                    if (!dualAudioHandler.secondaryAudioSource)
                        dualAudioHandler.HandleSecondaryAudio();
                    dualAudioHandler.secondaryAudioSource.transform.position = secondaryCandidate.position;
                }
                else
                {
                    audioSource.transform.position = candidates[0].position;
                    dualAudioHandler.CleanupSecondaryAudio();
                }
            }
            else if (candidates.Count == 1)
            {
                audioSource.transform.position = candidates[0].position;
                dualAudioHandler.CleanupSecondaryAudio();
            }
            else
            {
                dualAudioHandler.CleanupSecondaryAudio();
            }
        }

        private void ProcessDualAudioForMesh(Vector3 currentTargetPosition, float triggerDistance)
        {
            List<Candidate> candidates = new List<Candidate>();
            float triggerDistSqr = triggerDistance * triggerDistance;

            foreach (MeshFilter mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null)
                    continue;

                SoundShapes_CachedMeshData cachedData = null;
                if (cachedMeshDataList != null)
                    cachedData = cachedMeshDataList.Find(x => x.meshReference == mf.sharedMesh);

                Vector3[] vertices;
                int[] triangles;
                if (cachedData != null)
                {
                    vertices = cachedData.vertices;
                    triangles = cachedData.triangles;
                }
                else
                {
                    Mesh mesh = mf.sharedMesh;
                    vertices = mesh.vertices;
                    triangles = mesh.triangles;
                }

                Transform mfT = mf.transform;
                Vector3[] worldVertices = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                    worldVertices[i] = mfT.TransformPoint(vertices[i]);

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = worldVertices[triangles[i]];
                    Vector3 b = worldVertices[triangles[i + 1]];
                    Vector3 c = worldVertices[triangles[i + 2]];

                    Vector3 candidatePoint = AudioZoneGeometry.ClosestPointOnTriangle(a, b, c, currentTargetPosition);
                    float distSqr = (currentTargetPosition - candidatePoint).sqrMagnitude;
                    if (distSqr <= triggerDistSqr)
                    {
                        candidates.Add(new Candidate(candidatePoint, distSqr, mf.GetInstanceID()));
                    }
                }
            }

            if (candidates.Count >= 2)
            {
                candidates.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));
                bool validPairFound = false;
                Candidate primaryCandidate = null, secondaryCandidate = null;

                for (int m = 0; m < candidates.Count; m++)
                {
                    for (int n = m + 1; n < candidates.Count; n++)
                    {
                        if (Vector3.Distance(candidates[m].position, candidates[n].position) < 0.01f)
                            continue;

                        Vector3 v1 = candidates[m].position - currentTargetPosition;
                        Vector3 v2 = candidates[n].position - currentTargetPosition;
                        float angle = Vector3.Angle(v1, v2);
                        if (angle > 70f)
                        {
                            primaryCandidate = candidates[m];
                            secondaryCandidate = candidates[n];
                            validPairFound = true;
                            break;
                        }
                    }
                    if (validPairFound)
                        break;
                }

                if (validPairFound)
                {
                    audioSource.transform.position = primaryCandidate.position + meshAudioOffset;
                    if (!dualAudioHandler.secondaryAudioSource)
                        dualAudioHandler.HandleSecondaryAudio();
                    dualAudioHandler.secondaryAudioSource.transform.position = secondaryCandidate.position + meshAudioOffset;
                }
                else
                {
                    // Fallback to single audio if no valid pair is found.
                    audioSource.transform.position = candidates[0].position + meshAudioOffset;
                    dualAudioHandler.CleanupSecondaryAudio();
                }
            }
            else if (candidates.Count == 1)
            {
                audioSource.transform.position = candidates[0].position + meshAudioOffset;
                dualAudioHandler.CleanupSecondaryAudio();
            }
            else
            {
                dualAudioHandler.CleanupSecondaryAudio();
            }
        }




        // Returns the closest GameObject with the specified tag.
        private GameObject GetClosestObjectByTag(string tag)
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
            if (objs == null || objs.Length == 0)
                return null;
            GameObject closest = objs[0];
            float minDist = (objs[0].transform.position - transform.position).sqrMagnitude;
            foreach (GameObject obj in objs)
            {
                float dist = (obj.transform.position - transform.position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = obj;
                }
            }
            return closest;
        }


        public void StopPreview()
        {
            editorPreview = false;
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
            dualAudioHandler.StopAndCleanup();
            if (multiEmitterHandler != null)
                multiEmitterHandler.CleanupAll();
            if (disabledAudioSourceForMultiEmitter)
            {
                audioSource.enabled = true;
                disabledAudioSourceForMultiEmitter = false;
            }

            if (mainListenerWasEnabled && Camera.main != null)
            {
                AudioListener mainListener = Camera.main.GetComponent<AudioListener>();
                if (mainListener != null)
                    mainListener.enabled = true;
            }
            if (audioSource != null && audioSource.GetComponent<AudioLowPassFilter>() != null)
                audioSource.GetComponent<AudioLowPassFilter>().cutoffFrequency = defaultLowPassCutoff;
            mainListenerWasEnabled = false;

            SceneAudioListenerManager.DisableListener();
        }

#if UNITY_EDITOR
        void OnRenderObject()
        {
            if (audioSource == null || !debugMode)
                return;
            if ((!debugMode || mode == ZoneMode.MultiEmitter || !audioSource.isPlaying))
                return;

            Vector3 target;
            if (editorPreview && SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
                target = SceneView.lastActiveSceneView.camera.transform.position;
            else
            {
                GameObject targetObj = GetClosestObjectByTag(trackingTag);
                if (targetObj == null)
                    return;
                target = targetObj.transform.position;
            }

            float triggerDistance = (triggerDistanceOverride > 0f) ? triggerDistanceOverride : (audioSource ? audioSource.maxDistance : 0f);
            float triggerDistanceSqr = triggerDistance * triggerDistance;

            CreateLineMaterial();
            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            Vector3 pos = audioSource.transform.position;
            float crossSize = 0.25f;
            GL.Begin(GL.LINES);
            GL.Color(Color.cyan);
            GL.Vertex(pos + Vector3.right * crossSize);
            GL.Vertex(pos - Vector3.right * crossSize);
            GL.Vertex(pos + Vector3.up * crossSize);
            GL.Vertex(pos - Vector3.up * crossSize);
            GL.Vertex(pos + Vector3.forward * crossSize);
            GL.Vertex(pos - Vector3.forward * crossSize);
            GL.End();

            if (enableOcclusion)
            {
                if ((target - pos).sqrMagnitude <= triggerDistanceSqr)
                {
                    List<Vector3> offsets = AudioZoneOcclusion.GetOcclusionOffsets(target, pos, occlusionSampleRadius, occlusionResolution);
                    GL.Begin(GL.LINES);
                    foreach (Vector3 offset in offsets)
                    {
                        Vector3 sampleOrigin = target + offset;
                        Vector3 sampleTarget = pos + offset;
                        Color rayColor = Color.yellow;
                        if (Physics.Raycast(sampleOrigin, (sampleTarget - sampleOrigin).normalized,
                                            out RaycastHit hit,
                                            Vector3.Distance(sampleOrigin, sampleTarget),
                                            occlusionLayer))
                        {
                            if (!(mode == ZoneMode.Mesh && meshFilters != null && meshFilters.Count > 0 &&
                                 meshFilters.Exists(mf => mf != null && hit.collider.gameObject == mf.gameObject)))
                                rayColor = Color.red;
                        }
                        GL.Color(rayColor);
                        GL.Vertex(sampleOrigin);
                        GL.Vertex(sampleTarget);
                    }
                    GL.End();
                }
            }

            GL.PopMatrix();
        }

        void OnDrawGizmos()
        {
            if (Selection.activeGameObject != gameObject)
                return;

            if (mode == ZoneMode.Shape && points.Count > 0)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < points.Count - 1; i++)
                    Gizmos.DrawLine(cachedTransform.TransformPoint(points[i]), cachedTransform.TransformPoint(points[i + 1]));
                if (points.Count > 2 && closedShape)
                    Gizmos.DrawLine(cachedTransform.TransformPoint(points[points.Count - 1]), cachedTransform.TransformPoint(points[0]));
            }

            if (mode == ZoneMode.MultiEmitter && multiEmitterPoints != null)
            {
                Gizmos.color = Color.cyan;
                foreach (Vector3 pt in multiEmitterPoints)
                    Gizmos.DrawSphere(cachedTransform.TransformPoint(pt), 0.1f);
            }

            if (audioSource != null)
            {
                List<Vector3> offsetPerimeter = AudioZoneGeometry.GetOffsetPerimeter(this);
                if (offsetPerimeter.Count > 1)
                {
                    Gizmos.color = Color.red;
                    for (int i = 0; i < offsetPerimeter.Count; i++)
                    {
                        Vector3 current = offsetPerimeter[i];
                        Vector3 next = offsetPerimeter[(i + 1) % offsetPerimeter.Count];
                        Gizmos.DrawLine(current, next);
                    }
                }
            }
        }
#endif

        static void CreateLineMaterial()
        {
            if (!_lineMaterial)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _lineMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _lineMaterial.SetInt("_ZWrite", 0);
            }
        }

        public void SetTarget(Transform trackingTarget)
        {
            if (trackingTarget == null)
            {
                Debug.LogWarning("SetTarget called with null trackingTarget.");
                return;
            }
            trackingObject = trackingTarget;
        }

        public void ToggleClosedLoop(bool closedShape)
        {
            if (mode == ZoneMode.Shape)
                this.closedShape = closedShape;
            else
                Debug.LogWarning("Closed loop setting is only available in Shape mode.");
        }

        public void ToggleShouldTrack(bool shouldTrack)
        {
            this.shouldTrack = shouldTrack;
        }

        public void SetTrackingTag(string newTag)
        {
            if (newTag == null)
            {
                Debug.LogWarning("SetTrackingTag called with null newTag.");
                return;
            }
            trackingTag = newTag;
        }

        public void AddMeshTarget(MeshFilter filter)
        {
            if (filter == null)
            {
                Debug.LogWarning("AddMeshTarget called with null MeshFilter.");
                return;
            }
            meshFilters.Add(filter);
            AudioZoneGeometry.GenerateMeshData(this);
        }

        public void ClearMeshTargets()
        {
            meshFilters.Clear();
            cachedMeshDataList.Clear();
        }

        public void RemoveMultiPoint(int pointIndex)
        {
            if (pointIndex >= 0 && pointIndex < multiEmitterPoints.Count)
                multiEmitterPoints.RemoveAt(pointIndex);
            else
                Debug.LogWarning($"RemoveMultiPoint called with invalid index: {pointIndex}.");
        }

        public void AddMultiPoint(Vector3 location)
        {
            multiEmitterPoints.Add(location);
        }

        public void ClearMultiPoints()
        {
            multiEmitterPoints.Clear();
        }

        public void SetMultiPointLocation(int index, Vector3 location)
        {
            if (index >= 0 && index < multiEmitterPoints.Count)
                multiEmitterPoints[index] = location;
            else
                Debug.LogWarning($"SetMultiPointLocation called with invalid index: {index}.");
        }

        public void SetTrackingMode(TrackingMode newMode)
        {
            trackingMode = newMode;
        }

        public void PopulateMultiPoints(List<Transform> transforms)
        {
            if (transforms == null)
            {
                Debug.LogWarning("PopulateMultiPoints (Transform) called with null list.");
                return;
            }

            foreach (Transform t in transforms)
            {
                if (t != null)
                    multiEmitterPoints.Add(t.position);
                else
                    Debug.LogWarning("PopulateMultiPoints encountered a null Transform.");
            }
        }

        public void PopulateMultiPoints(List<Vector3> positions)
        {
            if (positions == null)
            {
                Debug.LogWarning("PopulateMultiPoints (Vector3) called with null list.");
                return;
            }

            foreach (Vector3 position in positions)
                multiEmitterPoints.Add(position);
        }

        public void AddShapePoint(Vector3 location)
        {
            points.Add(location);
        }

        public void RemoveShapePoint(int pointIndex)
        {
            if (pointIndex >= 0 && pointIndex < points.Count)
                points.RemoveAt(pointIndex);
            else
                Debug.LogWarning($"RemoveShapePoint called with invalid index: {pointIndex}.");
        }

        public void ClearShapePoints()
        {
            points.Clear();
        }

        public void SetShapePointLocation(int index, Vector3 location)
        {
            if (index >= 0 && index < points.Count)
                points[index] = location;
            else
                Debug.LogWarning($"SetShapePointLocation called with invalid index: {index}.");
        }

        public void PopulateShapePoints(List<Transform> transforms)
        {
            if (transforms == null)
            {
                Debug.LogWarning("PopulateShapePoints (Transform) called with null list.");
                return;
            }

            foreach (Transform t in transforms)
            {
                if (t != null)
                    points.Add(t.position);
                else
                    Debug.LogWarning("PopulateShapePoints encountered a null Transform.");
            }
        }

        public void PopulateShapePoints(List<Vector3> positions)
        {
            if (positions == null)
            {
                Debug.LogWarning("PopulateShapePoints (Vector3) called with null list.");
                return;
            }

            foreach (Vector3 position in positions)
                points.Add(position);
        }

    }
}
