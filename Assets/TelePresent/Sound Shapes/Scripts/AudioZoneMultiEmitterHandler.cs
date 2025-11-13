/*******************************************************
Product - Sound Shapes
Publisher - TelePresent Games
			http://TelePresentGames.dk
Author    - Martin Hansen
Created   - 2025
(c) 2025 Martin Hansen. All rights reserved.
*******************************************************/

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TelePresent.SoundShapes
{
	public class AudioZoneMultiEmitterHandler
	{
		private readonly AudioZone _zone;

		// Track spawned emitter? clone instances by their index
		private readonly Dictionary<int, GameObject> _activeInstances = new();

		public AudioZoneMultiEmitterHandler(AudioZone parentZone) => _zone = parentZone;


		public void UpdateMultiEmitterLogic()
		{
			if (!_zone)
				return;

			Vector3 listenerPos;

#if UNITY_EDITOR
			if (_zone.editorPreview && !Application.isPlaying)
			{
				var sv = SceneView.lastActiveSceneView;
				if (!sv?.camera) return;
				listenerPos = sv.camera.transform.position;
			}
			else if (!Application.isPlaying && !_zone.editorPreview)
			{
				return;
			}
			else
			{
				listenerPos = _zone.currentTargetPosition;
			}
#else
            listenerPos = _zone.currentTargetPosition;
#endif

			float baseMax = _zone.audioSource ? _zone.audioSource.maxDistance : 10f;
			float triggerDistance = _zone.triggerDistanceOverride > 0f
				? _zone.triggerDistanceOverride
				: baseMax;
			float triggerDistanceSq = triggerDistance * triggerDistance;

			GameObject sourceObject = _zone.audioSource
				? _zone.audioSource.gameObject
				: _zone.positionTarget;

			if (!sourceObject)
				return;


			for (int i = 0; i < _zone.multiEmitterPoints.Count; i++)
			{
				Vector3 worldPt = _zone.transform.TransformPoint(_zone.multiEmitterPoints[i]);

				bool inRange = (listenerPos - worldPt).sqrMagnitude <= triggerDistanceSq;

				if (inRange)
				{
					if (!_activeInstances.ContainsKey(i))
					{
						GameObject inst = CreateInstanceAt(worldPt, i, sourceObject);
						if (inst)
							_activeInstances[i] = inst;
					}

					if (_zone.enableOcclusion && _activeInstances.TryGetValue(i, out var go))
					{
						var src = go.GetComponent<AudioSource>();
						if (src)
							AudioZoneOcclusion.UpdateOcclusion(
								listenerPos,
								go.transform.position,
								src,
								_zone);
					}
				}
				else
				{
					if (_activeInstances.TryGetValue(i, out var inst))
					{
						DestroyInstance(inst);
						_activeInstances.Remove(i);
					}
				}
			}
		}

		//-------------------------------------------------------------------------
        #region Instance Creation / Destruction

		//-------------------------------------------------------------------------

		private GameObject CreateInstanceAt(Vector3 position,
			int emitterIndex,
			GameObject sourceObject)
		{

			if (_zone.requireAudioSourceComponent)
			{
				if (!_zone.audioSource)
				{
					Debug.LogWarning("AudioZoneMultiEmitterHandler: " +
									"requireAudioSourceComponent is TRUE, " +
									"but the zone has no AudioSource to copy.");
					return null;
				}

				GameObject go = new(_zone.audioSource.gameObject.name + "_Emitter_" + emitterIndex);
				go.transform.SetParent(_zone.transform, false);
				go.transform.position = position;
				go.transform.rotation = _zone.audioSource.transform.rotation;

				AudioSource dst = go.AddComponent<AudioSource>();
				CopyAudioSourceProperties(_zone.audioSource, dst);
				dst.Play();

				if (_zone.enableOcclusion)
				{
					var lpf = go.AddComponent<AudioLowPassFilter>();
					lpf.cutoffFrequency = _zone.defaultLowPassCutoff;
				}

				return go;
			}


			if (sourceObject == _zone.gameObject)
			{
				Debug.LogWarning("AudioZoneMultiEmitterHandler: cannot clone AudioSource " +
								"because it resides on the zone object itself.");
				return null;
			}

			GameObject clone = Object.Instantiate(
				sourceObject,
				position,
				sourceObject.transform.rotation,
				_zone.transform);
			clone.name = sourceObject.name + "_Emitter_" + emitterIndex;

			var zoneComp = clone.GetComponent<AudioZone>();
			if (zoneComp)
			{
#if UNITY_EDITOR
				if (Application.isPlaying)
					Object.Destroy(zoneComp);
				else
					Object.DestroyImmediate(zoneComp);
#else
                Object.Destroy(zoneComp);
#endif
			}

			var src = clone.GetComponent<AudioSource>();
			if (src)
			{
				src.enabled = true;
				src.Play();
			}

			return clone;
		}


		private void DestroyInstance(GameObject instance)
		{
			if (!instance) return;

#if UNITY_EDITOR
			if (Application.isPlaying)
				Object.Destroy(instance);
			else
				Object.DestroyImmediate(instance);
#else
            Object.Destroy(instance);
#endif
		}

		public void CleanupAll()
		{
			foreach (var kvp in new Dictionary<int, GameObject>(_activeInstances))
				DestroyInstance(kvp.Value);

			_activeInstances.Clear();
		}


        #endregion
        #region AudioSource copy helper


		private static void CopyAudioSourceProperties(AudioSource src, AudioSource dst)
		{
			dst.clip = src.clip;
			dst.outputAudioMixerGroup = src.outputAudioMixerGroup;
			dst.mute = src.mute;
			dst.bypassEffects = src.bypassEffects;
			dst.bypassListenerEffects = src.bypassListenerEffects;
			dst.bypassReverbZones = src.bypassReverbZones;
			dst.playOnAwake = src.playOnAwake;
			dst.loop = src.loop;
			dst.priority = src.priority;
			dst.volume = src.volume;
			dst.pitch = src.pitch;
			dst.panStereo = src.panStereo;
			dst.spatialBlend = src.spatialBlend;
			dst.reverbZoneMix = src.reverbZoneMix;
			dst.dopplerLevel = 0;
			dst.spread = src.spread;
			dst.rolloffMode = src.rolloffMode;
			dst.minDistance = src.minDistance;
			dst.maxDistance = src.maxDistance;
		}
		
        #endregion
	}
}
