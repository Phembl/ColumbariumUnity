/*******************************************************
Product - Sound Shapes
  Publisher - TelePresent Games
              http://TelePresentGames.dk
  Author    - Martin Hansen
  Created   - 2025
  (c) 2025 Martin Hansen. All rights reserved.
/*******************************************************/

using UnityEngine;
using UnityEditor;

namespace TelePresent.SoundShapes
{

    [CustomEditor(typeof(AudioZone))]
    public class AudioZoneEditor : Editor
    {
        #region Fields and Properties

        private bool _isDrawingShape;
        private bool _isDrawingMultiEmitter;
        private bool _showOcclusionSettings;

        private AudioZone _audioZone;
        private Plane _drawingPlane;

        // Custom GUI styles for scene overlay.
        private GUIStyle _sceneBoxStyle;
        private GUIStyle _sceneTitleStyle;
        private GUIStyle _sceneLabelStyle;

        // Custom foldout style.
        private GUIStyle _foldoutBoldStyle;

        // Serialized properties.
        private SerializedProperty _modeProp;
        private SerializedProperty _closedShapeProp;
        private SerializedProperty _pointsProp;
        private SerializedProperty _meshFiltersProp;
        private SerializedProperty _meshAudioOffsetProp;
        private SerializedProperty _multiEmitterPointsProp;
        private SerializedProperty _soundShapeTrackerProp;
        private SerializedProperty _requireAudioSourceComponentProp;
        private SerializedProperty _enableDualAudioProp;
        private SerializedProperty _enableOcclusionProp;
        private SerializedProperty _occlusionLayerProp;
        private SerializedProperty _occlusionVolumeMultiplierProp;
        private SerializedProperty _occlusionLowPassCutoffProp;
        private SerializedProperty _defaultLowPassCutoffProp;
        private SerializedProperty _occlusionResolutionProp;
        private SerializedProperty _occlusionSampleRadiusProp;
        private SerializedProperty _occlusion2DModeProp;
        private SerializedProperty _occlusion2DSpreadDegreesProp;
        private SerializedProperty _flipTriggerDistanceProp;
        private SerializedProperty _triggerDistanceOverrideProp;

        // New tracking mode properties.
        private SerializedProperty _trackingModeProp;
        private SerializedProperty _trackingTagProp;
        private SerializedProperty _trackingObjectProp;

        // Icon.
        private Texture2D _penIcon;
        private Texture2D _exitPenIcon;
        private Texture2D _previewOn;
        private Texture2D _previewOff;
        private const string IconsFolderPath = "Assets/TelePresent/Sound Shapes/Editor/";
        private const string PenIconPath = IconsFolderPath + "penicon.png";
        private const string ExitPenIconPath = IconsFolderPath + "exitpenicon.png";
        private const string PreviewOnPath = IconsFolderPath + "previewon.png";
        private const string PreviewOffPath = IconsFolderPath + "previewoff.png";

        // EditorPrefs keys for persistent foldouts.
        private string TargetFoldoutKey => "AudioZoneEditor_ShowTargetSettings_" + _audioZone.GetInstanceID();
        private string TriggerFoldoutKey => "AudioZoneEditor_ShowTriggerSettings_" + _audioZone.GetInstanceID();

        private bool ShowTargetSettingsFoldout
        {
            get => EditorPrefs.GetBool(TargetFoldoutKey, true);
            set => EditorPrefs.SetBool(TargetFoldoutKey, value);
        }
        private bool ShowTriggerSettingsFoldout
        {
            get => EditorPrefs.GetBool(TriggerFoldoutKey, true);
            set => EditorPrefs.SetBool(TriggerFoldoutKey, value);
        }

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            _penIcon = LoadIcon(PenIconPath);
            _exitPenIcon = LoadIcon(ExitPenIconPath);
            _previewOn = LoadIcon(PreviewOnPath);
            _previewOff = LoadIcon(PreviewOffPath);

            _audioZone = (AudioZone)target;
            _drawingPlane = new Plane(Vector3.up, _audioZone.transform.position);
            SceneView.duringSceneGui += OnSceneGUIDelegate;

            // Cache serialized properties.
            _modeProp = serializedObject.FindProperty("mode");
            _closedShapeProp = serializedObject.FindProperty("closedShape");
            _pointsProp = serializedObject.FindProperty("points");
            _meshFiltersProp = serializedObject.FindProperty("meshFilters");
            _meshAudioOffsetProp = serializedObject.FindProperty("meshAudioOffset");
            _multiEmitterPointsProp = serializedObject.FindProperty("multiEmitterPoints");
            _soundShapeTrackerProp = serializedObject.FindProperty("soundShapeTracker");
            _requireAudioSourceComponentProp = serializedObject.FindProperty("requireAudioSourceComponent");
            _enableDualAudioProp = serializedObject.FindProperty("enableDualAudio");
            _enableOcclusionProp = serializedObject.FindProperty("enableOcclusion");
            _occlusionLayerProp = serializedObject.FindProperty("occlusionLayer");
            _occlusionVolumeMultiplierProp = serializedObject.FindProperty("occlusionVolumeMultiplier");
            _occlusionLowPassCutoffProp = serializedObject.FindProperty("occlusionLowPassCutoff");
            _defaultLowPassCutoffProp = serializedObject.FindProperty("defaultLowPassCutoff");
            _occlusionResolutionProp = serializedObject.FindProperty("occlusionResolution");
            _occlusionSampleRadiusProp = serializedObject.FindProperty("occlusionSampleRadius");
            _occlusion2DModeProp          = serializedObject.FindProperty("occlusion2DMode");
            _occlusion2DSpreadDegreesProp = serializedObject.FindProperty("occlusion2DSpreadDegrees");
            _flipTriggerDistanceProp = serializedObject.FindProperty("flipTriggerDistance");
            _triggerDistanceOverrideProp = serializedObject.FindProperty("triggerDistanceOverride");

            _trackingModeProp = serializedObject.FindProperty("trackingMode");
            _trackingTagProp = serializedObject.FindProperty("trackingTag");
            _trackingObjectProp = serializedObject.FindProperty("trackingObject");
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUIDelegate;
        }

        private void OnSceneGUIDelegate(SceneView sceneView)
        {
            OnSceneGUI();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            InitializeFoldoutStyle();

            // Header
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Draw and configure audio zones by editing shape points or using mesh boundaries.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();

            // Mode selection toolbar.
            EditorGUILayout.LabelField("Area Mode", EditorStyles.boldLabel);
            int oldMode = _modeProp.enumValueIndex;
            int newMode = GUILayout.Toolbar(oldMode, new[] { "Shape", "Mesh", "Multi" });
            if (newMode != oldMode)
            {
                _modeProp.enumValueIndex = newMode;
                if (newMode != (int)AudioZone.ZoneMode.MultiEmitter)
                {
                    _audioZone.MultiEmitterHandler.CleanupAll();
                    if (_audioZone.disabledAudioSourceForMultiEmitter)
                    {
                        _audioZone.audioSource.enabled = true;
                        _audioZone.disabledAudioSourceForMultiEmitter = false;
                    }
                }
            }
            EditorGUILayout.Space();

            // Mode-specific settings.
            GUILayout.BeginVertical("box");
            int currentMode = _modeProp.enumValueIndex;
            if (currentMode == (int)AudioZone.ZoneMode.Shape)
            {
                DrawShapeSettings();
            }
            else if (currentMode == (int)AudioZone.ZoneMode.Mesh)
            {
                DrawMeshSettings();
            }
            else // MultiEmitter mode.
            {
                DrawMultiEmitterSettings();
            }
            GUILayout.EndVertical();

            // Audio Settings
            DrawAudioSettings();

            // Target Settings
            DrawTargetSettings();

            // Trigger Settings
            DrawTriggerSettings();

            // Debug and Preview Settings.
            DrawDebugAndPreviewSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                if (_isDrawingShape)
                {
                    _isDrawingShape = false;
                    e.Use();
                    Repaint();
                }
                if (_isDrawingMultiEmitter)
                {
                    _isDrawingMultiEmitter = false;
                    e.Use();
                    Repaint();
                }
            }

            InitSceneViewStyles();
            AudioZoneDrawingTools.DrawSceneOverlayUI(_audioZone, _isDrawingShape, _isDrawingMultiEmitter, _drawingPlane, _sceneBoxStyle, _sceneTitleStyle, _sceneLabelStyle);
            if (_audioZone.closedShape && _audioZone.points.Count > 2)
            {
                AudioZoneDrawingTools.DrawFilledShape(_audioZone);
            }
            SceneView.RepaintAll();
        }

        #endregion

        #region Draw Methods

        private void DrawShapeSettings()
        {
            EditorGUILayout.LabelField("Shape Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_pointsProp, true);
            EditorGUI.indentLevel--;
            string loopLabel = _closedShapeProp.boolValue ? "Closed Shape: On" : "Closed Shape: Off";
            bool newLoop = GUILayout.Toggle(_closedShapeProp.boolValue,
                new GUIContent(loopLabel, "Connects the last point to the first to form a closed shape."), "Button");
            if (newLoop != _closedShapeProp.boolValue)
                _closedShapeProp.boolValue = newLoop;

            // Button with custom vertical padding.
            GUIStyle paddedButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(4, 4, 10, 10)
            };

            if (!_isDrawingShape)
            {
                if (GUILayout.Button(GetIconContent("  Enter Draw Mode", _penIcon), paddedButtonStyle, GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                {
                    _isDrawingShape = true;
                    AudioZoneDrawingTools.RebuildBvh();
                }
            }
            else
            {
                Color originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1.1f, .9f, .9f);
                if (GUILayout.Button(GetIconContent("  Exit Draw Mode", _exitPenIcon), paddedButtonStyle, GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                    _isDrawingShape = false;
                GUI.backgroundColor = originalBgColor;
                EditorGUILayout.HelpBox("Click in the Scene to add points.", MessageType.Info);
            }
        }

        private void DrawMeshSettings()
        {
            EditorGUILayout.LabelField("Mesh Mode Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_meshFiltersProp, new GUIContent("Mesh Filters"), true);
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(_meshAudioOffsetProp, new GUIContent("Audio Offset"));
        }

        private void DrawMultiEmitterSettings()
        {
            // Button with custom vertical padding.
            GUIStyle paddedButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(4, 4, 10, 10)
            };

            EditorGUILayout.LabelField("Multi-Emitter Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_multiEmitterPointsProp, true);
            EditorGUI.indentLevel--;
            if (!_isDrawingMultiEmitter)
            {
                if (GUILayout.Button(GetIconContent("  Enter Draw Mode", _penIcon), paddedButtonStyle, GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                {
                    _isDrawingMultiEmitter = true;
                    AudioZoneDrawingTools.RebuildBvh();
                }
            }
            else
            {
                Color originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1.1f, .9f, .9f);
                if (GUILayout.Button(GetIconContent("  Exit Draw Mode", _exitPenIcon), paddedButtonStyle, GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                    _isDrawingMultiEmitter = false;
                GUI.backgroundColor = originalBgColor;
                EditorGUILayout.HelpBox("Click in the Scene to place emitter points.", MessageType.Info);
            }
        }
private void DrawAudioSettings()
{
    EditorGUILayout.BeginVertical("box");
    string trackerMode = _audioZone.requireAudioSourceComponent ? "Audio Source" : "Tracker Object";
    EditorGUILayout.LabelField(trackerMode, EditorStyles.boldLabel);

    EditorGUILayout.BeginHorizontal();
    if (_requireAudioSourceComponentProp.boolValue)
    {
        AudioSource cur = _audioZone.soundShapeTracker
            ? _audioZone.soundShapeTracker.GetComponent<AudioSource>() : null;
        EditorGUI.BeginChangeCheck();
        AudioSource newSrc = (AudioSource)EditorGUILayout.ObjectField(
            new GUIContent(trackerMode), cur, typeof(AudioSource), true);
        if (EditorGUI.EndChangeCheck())
            _soundShapeTrackerProp.objectReferenceValue = newSrc ? newSrc.gameObject : null;
    }
    else
    {
        EditorGUILayout.PropertyField(_soundShapeTrackerProp, new GUIContent(trackerMode));
    }

    GUIContent focusIcon = EditorGUIUtility.IconContent("TransformTool");
    if (!_audioZone.audioSource) GUI.enabled = false;
    if (GUILayout.Button(focusIcon, GUILayout.Width(30), GUILayout.Height(20)))
    {
        if (_audioZone.audioSource)
        {
            Selection.activeGameObject = _audioZone.soundShapeTracker;
            EditorGUIUtility.PingObject(_audioZone.soundShapeTracker);
        }
    }
    GUI.enabled = true;

    GUIContent reqIcon = _requireAudioSourceComponentProp.boolValue
        ? EditorGUIUtility.IconContent("d_AudioSource Icon")
        : EditorGUIUtility.IconContent("d_GameObject Icon");
    if (GUILayout.Button(reqIcon, GUILayout.Width(30), GUILayout.Height(20)))
        _requireAudioSourceComponentProp.boolValue = !_requireAudioSourceComponentProp.boolValue;
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    bool newDual = GUILayout.Toggle(_enableDualAudioProp.boolValue, "Allow Dual Audio", "Button");
    if (newDual != _enableDualAudioProp.boolValue)
    {
        _enableDualAudioProp.boolValue = newDual;
        if (!newDual) _audioZone.DualAudioHandler.StopAndCleanup();
    }

    bool newOcc = GUILayout.Toggle(_enableOcclusionProp.boolValue, "Enable Occlusion", "Button");
    if (newOcc != _enableOcclusionProp.boolValue)
    {
        _enableOcclusionProp.boolValue = newOcc;
        if (newOcc) StoreDefaultOcclusion();
        else        DisableOcclusion();
    }
    EditorGUILayout.EndHorizontal();

    if (_enableOcclusionProp.boolValue)
    {
        EditorGUI.indentLevel++;
        _showOcclusionSettings = EditorGUILayout.Foldout(_showOcclusionSettings, "Occlusion Settings", true);
        if (_showOcclusionSettings)
        {
            // 3D / 2D tab
            int modeIndex = _occlusion2DModeProp.boolValue ? 1 : 0;
            int newIndex  = GUILayout.Toolbar(modeIndex, new[] { "3D", "2D" });
            if (newIndex != modeIndex)
                _occlusion2DModeProp.boolValue = newIndex == 1;

            EditorGUILayout.PropertyField(_occlusionLayerProp, new GUIContent("Occlusion Layer"));

            // shared sliders
            EditorGUILayout.Slider(_occlusionVolumeMultiplierProp, 0f, 1f, "Volume Multiplier");
            EditorGUILayout.Slider(_occlusionLowPassCutoffProp,   20f, 22000f, "Occlusion LPF Cutoff");
            EditorGUILayout.Slider(_defaultLowPassCutoffProp,     20f, 22000f, "Default LPF Cutoff");
            UpdateAudioSourceDefaultLpf();

            // mode‑specific controls
            if (_occlusion2DModeProp.boolValue)
            {
                EditorGUILayout.IntSlider(_occlusionResolutionProp, 1, 50, "Ray Count");
                EditorGUILayout.Slider(_occlusion2DSpreadDegreesProp, 0f, 180f, "Spread Degrees");
            }
            else
            {
                EditorGUILayout.IntSlider(_occlusionResolutionProp, 1, 50, "Resolution");
                EditorGUILayout.Slider(_occlusionSampleRadiusProp, 0f, 5f, "Sample Radius");
            }
        }
        EditorGUI.indentLevel--;
    }
    EditorGUILayout.EndVertical();
}



        private void StoreDefaultOcclusion()
        {
            if (_audioZone.audioSource)
            {
                AudioLowPassFilter lowPass = _audioZone.audioSource.GetComponent<AudioLowPassFilter>();
                if (lowPass)
                {
                    _defaultLowPassCutoffProp.floatValue = lowPass.cutoffFrequency;
                }
            }
        }

        private void DisableOcclusion()
        {
            if (_audioZone.audioSource)
            {
                AudioLowPassFilter lowPass = _audioZone.audioSource.GetComponent<AudioLowPassFilter>();
                if (lowPass)
                {
                    lowPass.cutoffFrequency = _defaultLowPassCutoffProp.floatValue;
                }
                // Adjust volume back using the occlusion multiplier.
                _audioZone.audioSource.volume = _audioZone.audioSource.volume / _occlusionVolumeMultiplierProp.floatValue;
            }
        }

        private void UpdateAudioSourceDefaultLpf()
        {
            if (_audioZone.audioSource)
            {
                AudioLowPassFilter lowPass = _audioZone.audioSource.GetComponent<AudioLowPassFilter>();
                if (lowPass)
                {
                    lowPass.cutoffFrequency = _defaultLowPassCutoffProp.floatValue;
                }
            }
        }

        private void DrawTargetSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.indentLevel++;
            ShowTargetSettingsFoldout = EditorGUILayout.Foldout(ShowTargetSettingsFoldout, "Target Settings", true, _foldoutBoldStyle);
            if (ShowTargetSettingsFoldout)
            {
                EditorGUILayout.Space();
                int currentTrackingMode = _trackingModeProp.enumValueIndex;
                string[] trackingOptions = new string[]
                {
                "Track \"" + _trackingTagProp.stringValue + "\" Tag",
                "Track Object"
                };
                int newTrackingMode = GUILayout.Toolbar(currentTrackingMode, trackingOptions);
                if (newTrackingMode != currentTrackingMode)
                    _trackingModeProp.enumValueIndex = newTrackingMode;

                if (newTrackingMode == 0)
                    EditorGUILayout.PropertyField(_trackingTagProp, new GUIContent("Tracking Tag"));
                else
                    EditorGUILayout.PropertyField(_trackingObjectProp, new GUIContent("Tracking Object"));
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawTriggerSettings()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            ShowTriggerSettingsFoldout = EditorGUILayout.Foldout(
                ShowTriggerSettingsFoldout,
                "Trigger Settings",
                true,
                _foldoutBoldStyle
            );

            if (ShowTriggerSettingsFoldout)
            {
                EditorGUILayout.Space();

                // Flip Trigger Button
                if (_modeProp.enumValueIndex == (int)AudioZone.ZoneMode.Shape && _audioZone.closedShape)
                {
                    string flipLabel = _flipTriggerDistanceProp.boolValue ? "Flip Trigger: On" : "Flip Trigger: Off";
                    bool newFlip = GUILayout.Toggle(
                        _flipTriggerDistanceProp.boolValue,
                        new GUIContent(flipLabel, "Flips the trigger direction."),
                        "Button"
                    );
                    if (newFlip != _flipTriggerDistanceProp.boolValue)
                    {
                        _flipTriggerDistanceProp.boolValue = newFlip;
                    }
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(
                    _triggerDistanceOverrideProp,
                    new GUIContent("Trigger Distance", "Set a value to 0 to use AudioSource Range.")
                );

                string rangeLabel = _triggerDistanceOverrideProp.floatValue != 0f
                    ? "Using Override"
                    : "Using Audio Max Range";

                EditorGUILayout.LabelField(rangeLabel, EditorStyles.miniLabel);

                EditorGUILayout.EndHorizontal();

                // Prevent negative values
                if (_triggerDistanceOverrideProp.floatValue < 0f)
                {
                    _triggerDistanceOverrideProp.floatValue = 0f;
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }


        private void DrawDebugAndPreviewSettings()
        {
            // Debug Settings
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
            if (GUILayout.Button(_audioZone.debugMode ? "Disable Debug Mode" : "Enable Debug Mode", GUILayout.Height(25)))
            {
                _audioZone.debugMode = !_audioZone.debugMode;
                EditorUtility.SetDirty(_audioZone);
            }
            EditorGUILayout.EndVertical();

            // Reserve a full-width rect for both buttons with a fixed height.
            Rect fullRect = EditorGUILayout.GetControlRect(GUILayout.Height(30));
            float halfWidth = fullRect.width / 2f;
            Rect previewRect = new Rect(fullRect.x, fullRect.y, halfWidth, fullRect.height);
            Rect soundscapeRect = new Rect(fullRect.x + halfWidth, fullRect.y, halfWidth, fullRect.height);

            // Editor Preview Toggle Button
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            GUIContent previewToggleContent = GetIconContent(
                _audioZone.editorPreview ? "  Stop Preview" : "  Start Preview",
                _audioZone.editorPreview ? _previewOff : _previewOn);

            bool newEditorPrev = GUI.Toggle(previewRect, _audioZone.editorPreview, previewToggleContent, "Button");
            if (newEditorPrev != _audioZone.editorPreview)
            {
                _audioZone.editorPreview = newEditorPrev;
                if (_audioZone.editorPreview)
                {
                    if (_audioZone.mode != AudioZone.ZoneMode.MultiEmitter &&
                        _audioZone.audioSource &&
                        _audioZone.audioSource.enabled && !_audioZone.audioSource.isPlaying)
                    {
                        if (_audioZone.mode == AudioZone.ZoneMode.Mesh)
                        {
                            AudioZoneGeometry.GenerateMeshData(_audioZone);
                        }
                    }
                    if (_audioZone.audioSource)
                        _audioZone.audioSource.Play();
                }
                else
                {
                    if (_audioZone.audioSource)
                        _audioZone.StopPreview();
                }
            }
            EditorGUI.EndDisabledGroup();

            // Preview Soundscape Button
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            if (GUI.Button(soundscapeRect, "Preview Soundscape"))
            {
                ToggleWholeSoundscape();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
        }

        #endregion

        #region Helper Methods

        private Texture2D LoadIcon(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Helper method to return a GUIContent with fallback to text if the icon is null.
        private GUIContent GetIconContent(string text, Texture2D icon)
        {
            if (!icon)
                return new GUIContent(text);
            return new GUIContent(text, icon);
        }

        private void InitializeFoldoutStyle()
        {
            _foldoutBoldStyle ??= new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(20, 0, 0, 0)
            };
        }

        private void InitSceneViewStyles()
        {
            if (_sceneBoxStyle != null)
                return;

            _sceneBoxStyle = new GUIStyle("box")
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(8, 8, 8, 8)
            };
            _sceneTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white },
                fontSize = 12
            };
            _sceneLabelStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.white },
                fontSize = 11
            };
        }

        private void ToggleWholeSoundscape()
        {
            AudioZone[] zones = Object.FindObjectsByType<AudioZone>(FindObjectsSortMode.None);
            bool enablePreview = false;
            foreach (AudioZone zone in zones)
            {
                if (!zone.editorPreview)
                {
                    enablePreview = true;
                    break;
                }
            }
            foreach (AudioZone zone in zones)
            {
                zone.editorPreview = enablePreview;
                EditorUtility.SetDirty(zone);
                if (enablePreview)
                {
                    if (zone.mode != AudioZone.ZoneMode.MultiEmitter && zone.audioSource && !zone.audioSource.isPlaying)
                    {
                        zone.audioSource.loop = true;
                        zone.audioSource.Play();
                    }
                }
                else
                {
                    zone.StopPreview();
                    if (zone.disabledAudioSourceForMultiEmitter)
                    {
                        zone.audioSource.enabled = true;
                        zone.disabledAudioSourceForMultiEmitter = false;
                    }
                }
            }
        }

        #endregion
    }
}