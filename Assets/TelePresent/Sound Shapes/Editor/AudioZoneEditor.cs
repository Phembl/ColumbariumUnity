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

        private bool isDrawingShape = false;
        private bool isDrawingMultiEmitter = false;
        private bool showOcclusionSettings = false;

        private AudioZone audioZone;
        private Plane drawingPlane;

        // Custom GUI styles for scene overlay.
        private GUIStyle sceneBoxStyle;
        private GUIStyle sceneTitleStyle;
        private GUIStyle sceneLabelStyle;

        // Custom foldout style.
        private GUIStyle foldoutBoldStyle;

        // Serialized properties.
        private SerializedProperty modeProp;
        private SerializedProperty closedShapeProp;
        private SerializedProperty pointsProp;
        private SerializedProperty meshFiltersProp;
        private SerializedProperty meshAudioOffsetProp;
        private SerializedProperty multiEmitterPointsProp;
        private SerializedProperty audioSourceProp;
        private SerializedProperty enableDualAudioProp;
        private SerializedProperty enableOcclusionProp;
        private SerializedProperty occlusionLayerProp;
        private SerializedProperty occlusionVolumeMultiplierProp;
        private SerializedProperty occlusionLowPassCutoffProp;
        private SerializedProperty defaultLowPassCutoffProp;
        private SerializedProperty occlusionResolutionProp;
        private SerializedProperty occlusionSampleRadiusProp;
        private SerializedProperty flipTriggerDistanceProp;
        private SerializedProperty triggerDistanceOverrideProp;

        // New tracking mode properties.
        private SerializedProperty trackingModeProp;
        private SerializedProperty trackingTagProp;
        private SerializedProperty trackingObjectProp;

        // Icon.
        private Texture2D penIcon;
        private Texture2D exitPenIcon;
        private Texture2D previewOn;
        private Texture2D previewOff;
        private const string IconsFolderPath = "Assets/TelePresent/Sound Shapes/Editor/";
        private const string penIconPath = IconsFolderPath + "penicon.png";
        private const string exitPenIconPath = IconsFolderPath + "exitpenicon.png";
        private const string previewOnPath = IconsFolderPath + "previewon.png";
        private const string previewOffPath = IconsFolderPath + "previewoff.png";

        // EditorPrefs keys for persistent foldouts.
        private string TargetFoldoutKey { get { return "AudioZoneEditor_ShowTargetSettings_" + audioZone.GetInstanceID(); } }
        private string TriggerFoldoutKey { get { return "AudioZoneEditor_ShowTriggerSettings_" + audioZone.GetInstanceID(); } }

        private bool ShowTargetSettingsFoldout
        {
            get { return EditorPrefs.GetBool(TargetFoldoutKey, true); }
            set { EditorPrefs.SetBool(TargetFoldoutKey, value); }
        }
        private bool ShowTriggerSettingsFoldout
        {
            get { return EditorPrefs.GetBool(TriggerFoldoutKey, true); }
            set { EditorPrefs.SetBool(TriggerFoldoutKey, value); }
        }

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            penIcon = LoadIcon(penIconPath);
            exitPenIcon = LoadIcon(exitPenIconPath);
            previewOn = LoadIcon(previewOnPath);
            previewOff = LoadIcon(previewOffPath);

            audioZone = (AudioZone)target;
            drawingPlane = new Plane(Vector3.up, audioZone.transform.position);
            SceneView.duringSceneGui += OnSceneGUIDelegate;

            // Cache serialized properties.
            modeProp = serializedObject.FindProperty("mode");
            closedShapeProp = serializedObject.FindProperty("closedShape");
            pointsProp = serializedObject.FindProperty("points");
            meshFiltersProp = serializedObject.FindProperty("meshFilters");
            meshAudioOffsetProp = serializedObject.FindProperty("meshAudioOffset");
            multiEmitterPointsProp = serializedObject.FindProperty("multiEmitterPoints");
            audioSourceProp = serializedObject.FindProperty("audioSource");
            enableDualAudioProp = serializedObject.FindProperty("enableDualAudio");
            enableOcclusionProp = serializedObject.FindProperty("enableOcclusion");
            occlusionLayerProp = serializedObject.FindProperty("occlusionLayer");
            occlusionVolumeMultiplierProp = serializedObject.FindProperty("occlusionVolumeMultiplier");
            occlusionLowPassCutoffProp = serializedObject.FindProperty("occlusionLowPassCutoff");
            defaultLowPassCutoffProp = serializedObject.FindProperty("defaultLowPassCutoff");
            occlusionResolutionProp = serializedObject.FindProperty("occlusionResolution");
            occlusionSampleRadiusProp = serializedObject.FindProperty("occlusionSampleRadius");
            flipTriggerDistanceProp = serializedObject.FindProperty("flipTriggerDistance");
            triggerDistanceOverrideProp = serializedObject.FindProperty("triggerDistanceOverride");

            trackingModeProp = serializedObject.FindProperty("trackingMode");
            trackingTagProp = serializedObject.FindProperty("trackingTag");
            trackingObjectProp = serializedObject.FindProperty("trackingObject");
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
            int oldMode = modeProp.enumValueIndex;
            int newMode = GUILayout.Toolbar(oldMode, new string[] { "Shape", "Mesh", "Multi" });
            if (newMode != oldMode)
            {
                modeProp.enumValueIndex = newMode;
                if (newMode != (int)AudioZone.ZoneMode.MultiEmitter)
                {
                    audioZone.multiEmitterHandler.CleanupAll();
                    if (audioZone.disabledAudioSourceForMultiEmitter)
                    {
                        audioZone.audioSource.enabled = true;
                        audioZone.disabledAudioSourceForMultiEmitter = false;
                    }
                }
            }
            EditorGUILayout.Space();

            // Mode-specific settings.
            GUILayout.BeginVertical("box");
            int currentMode = modeProp.enumValueIndex;
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
                if (isDrawingShape)
                {
                    isDrawingShape = false;
                    e.Use();
                    Repaint();
                }
                if (isDrawingMultiEmitter)
                {
                    isDrawingMultiEmitter = false;
                    e.Use();
                    Repaint();
                }
            }

            InitSceneViewStyles();
            AudioZoneDrawingTools.DrawSceneOverlayUI(audioZone, isDrawingShape, isDrawingMultiEmitter, drawingPlane, sceneBoxStyle, sceneTitleStyle, sceneLabelStyle);
            if (audioZone.closedShape && audioZone.points.Count > 2)
            {
                AudioZoneDrawingTools.DrawFilledShape(audioZone);
            }
            SceneView.RepaintAll();
        }

        #endregion

        #region Draw Methods

        private void DrawShapeSettings()
        {
            EditorGUILayout.LabelField("Shape Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(pointsProp, true);
            EditorGUI.indentLevel--;
            string loopLabel = closedShapeProp.boolValue ? "Closed Shape: On" : "Closed Shape: Off";
            bool newLoop = GUILayout.Toggle(closedShapeProp.boolValue,
                new GUIContent(loopLabel, "Connects the last point to the first to form a closed shape."), "Button");
            if (newLoop != closedShapeProp.boolValue)
                closedShapeProp.boolValue = newLoop;

            // Button with custom vertical padding.
            GUIStyle paddedButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(4, 4, 10, 10)
            };

            if (!isDrawingShape)
            {
                if (GUILayout.Button(GetIconContent("  Enter Draw Mode", penIcon), paddedButtonStyle, GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                {
                    isDrawingShape = true;
                    AudioZoneDrawingTools.RebuildBVH();
                }
            }
            else
            {
                Color originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1.1f, .9f, .9f);
                if (GUILayout.Button(GetIconContent("  Exit Draw Mode", exitPenIcon), paddedButtonStyle, GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                    isDrawingShape = false;
                GUI.backgroundColor = originalBgColor;
                EditorGUILayout.HelpBox("Click in the Scene to add points.", MessageType.Info);
            }
        }

        private void DrawMeshSettings()
        {
            EditorGUILayout.LabelField("Mesh Mode Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(meshFiltersProp, new GUIContent("Mesh Filters"), true);
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(meshAudioOffsetProp, new GUIContent("Audio Offset"));
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
            EditorGUILayout.PropertyField(multiEmitterPointsProp, true);
            EditorGUI.indentLevel--;
            if (!isDrawingMultiEmitter)
            {
                if (GUILayout.Button(GetIconContent("  Enter Draw Mode", penIcon), paddedButtonStyle, GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                {
                    isDrawingMultiEmitter = true;
                    AudioZoneDrawingTools.RebuildBVH();
                }
            }
            else
            {
                Color originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1.1f, .9f, .9f);
                if (GUILayout.Button(GetIconContent("  Exit Draw Mode", exitPenIcon), paddedButtonStyle, GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                    isDrawingMultiEmitter = false;
                GUI.backgroundColor = originalBgColor;
                EditorGUILayout.HelpBox("Click in the Scene to place emitter points.", MessageType.Info);
            }
        }

        private void DrawAudioSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);

            // Draw Audio Source property field with a focus button on the same line.
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(audioSourceProp, new GUIContent("Audio Source"));

            // Get the default Unity icon for focus.
            GUIContent focusIcon = EditorGUIUtility.IconContent("TransformTool");

            // Disable the button if no AudioSource is assigned.
            if (audioZone.audioSource == null)
                GUI.enabled = false;

            if (GUILayout.Button(focusIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                if (audioZone.audioSource != null)
                {
                    // Select and ping the AudioSource's GameObject.
                    Selection.activeGameObject = audioZone.audioSource.gameObject;
                    EditorGUIUtility.PingObject(audioZone.audioSource.gameObject);
                }
            }

            // Re-enable GUI if it was disabled.
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // Dual Audio Toggle
            bool newDual = GUILayout.Toggle(enableDualAudioProp.boolValue, "Allow Dual Audio", "Button");
            if (newDual != enableDualAudioProp.boolValue)
            {
                enableDualAudioProp.boolValue = newDual;
                if (!newDual)
                    audioZone.dualAudioHandler.StopAndCleanup();
            }

            // Occlusion Toggle with change detection.
            bool newOcc = GUILayout.Toggle(enableOcclusionProp.boolValue, "Enable Occlusion", "Button");
            if (newOcc != enableOcclusionProp.boolValue)
            {
                enableOcclusionProp.boolValue = newOcc;
                if (newOcc)
                {
                    // When enabling occlusion, store the current default cutoff.
                    StoreDefaultOcclusion();
                }
                else
                {
                    // When disabling occlusion, reset settings.
                    DisableOcclusion();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (enableOcclusionProp.boolValue)
            {
                EditorGUI.indentLevel++;
                showOcclusionSettings = EditorGUILayout.Foldout(showOcclusionSettings, "Occlusion Settings", true);
                if (showOcclusionSettings)
                {
                    EditorGUILayout.PropertyField(occlusionLayerProp, new GUIContent("Occlusion Layer"));
                    EditorGUILayout.Slider(occlusionVolumeMultiplierProp, 0f, 1f, "Occlusion Volume Multiplier");
                    EditorGUILayout.Slider(occlusionLowPassCutoffProp, 20f, 22000f, "Occlusion LPF Cutoff");

                    // Draw the default LPF cutoff slider.
                    EditorGUILayout.Slider(defaultLowPassCutoffProp, 20f, 22000f, "Default LPF Cutoff");
                    // Immediately update the AudioLowPassFilter's cutoff when dragging.
                    UpdateAudioSourceDefaultLPF();

                    EditorGUILayout.IntSlider(occlusionResolutionProp, 0, 50, new GUIContent("Occlusion Resolution"));
                    EditorGUILayout.Slider(occlusionSampleRadiusProp, 0f, 5f, "Occlusion Sample Radius");
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void StoreDefaultOcclusion()
        {
            if (audioZone.audioSource != null)
            {
                AudioLowPassFilter lowPass = audioZone.audioSource.GetComponent<AudioLowPassFilter>();
                if (lowPass != null)
                {
                    defaultLowPassCutoffProp.floatValue = lowPass.cutoffFrequency;
                }
            }
        }

        private void DisableOcclusion()
        {
            if (audioZone.audioSource != null)
            {
                AudioLowPassFilter lowPass = audioZone.audioSource.GetComponent<AudioLowPassFilter>();
                if (lowPass != null)
                {
                    lowPass.cutoffFrequency = defaultLowPassCutoffProp.floatValue;
                }
                // Adjust volume back using the occlusion multiplier.
                audioZone.audioSource.volume = audioZone.audioSource.volume / occlusionVolumeMultiplierProp.floatValue;
            }
        }

        private void UpdateAudioSourceDefaultLPF()
        {
            if (audioZone.audioSource != null)
            {
                AudioLowPassFilter lowPass = audioZone.audioSource.GetComponent<AudioLowPassFilter>();
                if (lowPass != null)
                {
                    lowPass.cutoffFrequency = defaultLowPassCutoffProp.floatValue;
                }
            }
        }

        private void DrawTargetSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.indentLevel++;
            ShowTargetSettingsFoldout = EditorGUILayout.Foldout(ShowTargetSettingsFoldout, "Target Settings", true, foldoutBoldStyle);
            if (ShowTargetSettingsFoldout)
            {
                EditorGUILayout.Space();
                int currentTrackingMode = trackingModeProp.enumValueIndex;
                string[] trackingOptions = new string[]
                {
                "Track \"" + trackingTagProp.stringValue + "\" Tag",
                "Track Object"
                };
                int newTrackingMode = GUILayout.Toolbar(currentTrackingMode, trackingOptions);
                if (newTrackingMode != currentTrackingMode)
                    trackingModeProp.enumValueIndex = newTrackingMode;

                if (newTrackingMode == 0)
                    EditorGUILayout.PropertyField(trackingTagProp, new GUIContent("Tracking Tag"));
                else
                    EditorGUILayout.PropertyField(trackingObjectProp, new GUIContent("Tracking Object"));
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
                foldoutBoldStyle
            );

            if (ShowTriggerSettingsFoldout)
            {
                EditorGUILayout.Space();

                // Flip Trigger Button
                if (modeProp.enumValueIndex == (int)AudioZone.ZoneMode.Shape && audioZone.closedShape)
                {
                    string flipLabel = flipTriggerDistanceProp.boolValue ? "Flip Trigger: On" : "Flip Trigger: Off";
                    bool newFlip = GUILayout.Toggle(
                        flipTriggerDistanceProp.boolValue,
                        new GUIContent(flipLabel, "Flips the trigger direction."),
                        "Button"
                    );
                    if (newFlip != flipTriggerDistanceProp.boolValue)
                    {
                        flipTriggerDistanceProp.boolValue = newFlip;
                    }
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(
                    triggerDistanceOverrideProp,
                    new GUIContent("Trigger Distance", "Set a value to 0 to use AudioSource Range.")
                );

                string rangeLabel = triggerDistanceOverrideProp.floatValue != 0f
                    ? "Using Override"
                    : "Using Audio Max Range";

                EditorGUILayout.LabelField(rangeLabel, EditorStyles.miniLabel);

                EditorGUILayout.EndHorizontal();

                // Prevent negative values
                if (triggerDistanceOverrideProp.floatValue < 0f)
                {
                    triggerDistanceOverrideProp.floatValue = 0f;
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
            if (GUILayout.Button(audioZone.debugMode ? "Disable Debug Mode" : "Enable Debug Mode", GUILayout.Height(25)))
            {
                audioZone.debugMode = !audioZone.debugMode;
                EditorUtility.SetDirty(audioZone);
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
                audioZone.editorPreview ? "  Stop Preview" : "  Start Preview",
                audioZone.editorPreview ? previewOff : previewOn);

            bool newEditorPrev = GUI.Toggle(previewRect, audioZone.editorPreview, previewToggleContent, "Button");
            if (newEditorPrev != audioZone.editorPreview)
            {
                audioZone.editorPreview = newEditorPrev;
                if (audioZone.editorPreview)
                {
                    if (audioZone.mode != AudioZone.ZoneMode.MultiEmitter &&
                        audioZone.audioSource != null &&
                        audioZone.audioSource.enabled && !audioZone.audioSource.isPlaying)
                    {
                        if (audioZone.mode == AudioZone.ZoneMode.Mesh)
                        {
                            AudioZoneGeometry.GenerateMeshData(audioZone);
                        }
                    }
                    if (audioZone.audioSource)
                        audioZone.audioSource.Play();
                }
                else
                {
                    if (audioZone.audioSource)
                        audioZone.StopPreview();
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
            if (icon == null)
                return new GUIContent(text);
            return new GUIContent(text, icon);
        }

        private void InitializeFoldoutStyle()
        {
            if (foldoutBoldStyle == null)
            {
                foldoutBoldStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(20, 0, 0, 0)
                };
            }
        }

        private void InitSceneViewStyles()
        {
            if (sceneBoxStyle != null)
                return;

            sceneBoxStyle = new GUIStyle("box")
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(8, 8, 8, 8)
            };
            sceneTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white },
                fontSize = 12
            };
            sceneLabelStyle = new GUIStyle(EditorStyles.label)
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
                    if (zone.mode != AudioZone.ZoneMode.MultiEmitter && zone.audioSource != null && !zone.audioSource.isPlaying)
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