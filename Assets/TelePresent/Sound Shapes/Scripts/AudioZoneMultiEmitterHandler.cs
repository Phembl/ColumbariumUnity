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
namespace TelePresent.SoundShapes
{
    public class AudioZoneMultiEmitterHandler
    {
        private AudioZone zone;
        // Dictionary mapping multi-emitter point indices to their active AudioSource.
        private Dictionary<int, AudioSource> activeSources = new Dictionary<int, AudioSource>();

        public AudioZoneMultiEmitterHandler(AudioZone parentZone)
        {
            zone = parentZone;
        }

        /// <summary>
        /// Checks the player’s distance for each emitter point and spawns or removes AudioSources as needed.
        /// </summary>
        public void UpdateMultiEmitterLogic()
        {

            if (zone == null)
                return;

            Vector3 targetPosition = Vector3.zero;

#if UNITY_EDITOR
            if (zone.editorPreview && !Application.isPlaying)
            {
                if (UnityEditor.SceneView.lastActiveSceneView != null &&
                    UnityEditor.SceneView.lastActiveSceneView.camera != null)
                {
                    targetPosition = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;
                }
                else
                {
                    return; // No valid camera position.
                }
            }
            else if (!Application.isPlaying && !zone.editorPreview)
            {
                // Not in preview mode in the editor; skip updating.
                return;
            }
            else
            {

                targetPosition = zone.currentTargetPosition;
            }
#else
                targetPosition = zone.currentTargetPosition;
#endif


            // Determine trigger distance (fallback to 10 if no main AudioSource is set).
            float baseMax = (zone.audioSource != null) ? zone.audioSource.maxDistance : 10f;
            float triggerDistance = (zone.triggerDistanceOverride > 0f) ? zone.triggerDistanceOverride : baseMax;
            float triggerDistanceSqr = triggerDistance * triggerDistance;

            Transform zoneTransform = zone.transform;

            // Loop through all multi-emitter points.
            for (int i = 0; i < zone.multiEmitterPoints.Count; i++)
            {
                Vector3 localPt = zone.multiEmitterPoints[i];
                Vector3 worldPt = zoneTransform.TransformPoint(localPt);
                float distSqr = (targetPosition - worldPt).sqrMagnitude;
                bool inRange = distSqr <= triggerDistanceSqr;

                if (inRange)
                {
                    // Spawn an AudioSource if one isn't active.
                    if (!activeSources.ContainsKey(i))
                    {
                        AudioSource newSrc = CreateAudioSourceAt(worldPt, i);
                        activeSources[i] = newSrc;
                    }

                    // Update occlusion if enabled.
                    if (zone.enableOcclusion && activeSources.ContainsKey(i))
                    {
                        AudioZoneOcclusion.UpdateOcclusion(targetPosition, worldPt, activeSources[i], zone);
                    }
                }
                else
                {
                    // If the player leaves the range, destroy the AudioSource.
                    if (activeSources.ContainsKey(i))
                    {
                        DestroyAudioSource(activeSources[i]);
                        activeSources.Remove(i);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new AudioSource GameObject at the specified position and copies settings from the zone's main AudioSource.
        /// </summary>
        private AudioSource CreateAudioSourceAt(Vector3 position, int emitterIndex)
        {
            GameObject go = new GameObject("MultiEmitterSource_" + emitterIndex)
            {
                transform =
            {
                position = position,
                parent = zone.transform
            }
            };

            AudioSource newSrc = go.AddComponent<AudioSource>();
            newSrc.volume = zone.audioSource.volume;

            AudioLowPassFilter lpf = go.AddComponent<AudioLowPassFilter>();
            if (zone.enableOcclusion)
            {
                lpf.cutoffFrequency = zone.defaultLowPassCutoff;
            }
            else if (zone.audioSource.GetComponent<AudioLowPassFilter>() != null)
            {
                lpf.cutoffFrequency = zone.audioSource.GetComponent<AudioLowPassFilter>().cutoffFrequency;
            }

            if (zone.audioSource != null)
            {
                // Copy properties from the main AudioSource.
                newSrc.clip = zone.audioSource.clip;
                newSrc.outputAudioMixerGroup = zone.audioSource.outputAudioMixerGroup;
                newSrc.mute = zone.audioSource.mute;
                newSrc.bypassEffects = zone.audioSource.bypassEffects;
                newSrc.bypassListenerEffects = zone.audioSource.bypassListenerEffects;
                newSrc.bypassReverbZones = zone.audioSource.bypassReverbZones;
                newSrc.playOnAwake = false;
                newSrc.loop = zone.audioSource.loop;
                newSrc.priority = zone.audioSource.priority;
                newSrc.volume = zone.audioSource.volume;
                newSrc.pitch = zone.audioSource.pitch;
                newSrc.panStereo = zone.audioSource.panStereo;
                newSrc.spatialBlend = zone.audioSource.spatialBlend;
                newSrc.reverbZoneMix = zone.audioSource.reverbZoneMix;
                newSrc.dopplerLevel = zone.audioSource.dopplerLevel;
                newSrc.spread = zone.audioSource.spread;
                newSrc.rolloffMode = zone.audioSource.rolloffMode;
                newSrc.minDistance = zone.audioSource.minDistance;
                newSrc.maxDistance = zone.audioSource.maxDistance;
            }
            else
            {
                // Fallback default settings.
                newSrc.spatialBlend = 1.0f;
                newSrc.rolloffMode = AudioRolloffMode.Linear;
                newSrc.minDistance = 1.0f;
                newSrc.maxDistance = 10.0f;
                newSrc.loop = true;
            }

            if (newSrc.clip != null)
            {
                newSrc.Play();
            }
            else
            {
                Debug.LogWarning("AudioZone: No audio clip assigned to multi-emitter source at point " + emitterIndex);
            }

            return newSrc;
        }

        /// <summary>
        /// Destroys the specified AudioSource’s GameObject.
        /// </summary>
        private void DestroyAudioSource(AudioSource source)
        {
            if (source != null && source.gameObject != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    GameObject.Destroy(source.gameObject);
                else
                    GameObject.DestroyImmediate(source.gameObject);
#else
            GameObject.Destroy(source.gameObject);
#endif
            }
        }

        /// <summary>
        /// Destroys all active AudioSources managed by this handler.
        /// </summary>
        public void CleanupAll()
        {
            // Iterate over a copy to avoid modification during iteration.
            foreach (var kvp in new Dictionary<int, AudioSource>(activeSources))
            {
                DestroyAudioSource(kvp.Value);
            }
            activeSources.Clear();
        }
    }
}