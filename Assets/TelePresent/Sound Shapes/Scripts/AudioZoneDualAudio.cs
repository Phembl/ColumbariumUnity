using UnityEngine;
using System.Collections;

namespace TelePresent.SoundShapes
{
    public class AudioZoneDualAudio
    {
        private AudioZone zone;
        public GameObject secondaryAudioObj;
        public AudioSource secondaryAudioSource;
        private Coroutine fadeCoroutine;
        private bool isFadingOut = false;
        public float secondaryFadeFactor = 1f;
        public float baseSecondaryVolume = 1f;

        // Configurable fade durations (in seconds)
        public float fadeInDuration = 2f;
        public float fadeOutDuration = 2f;

        public AudioZoneDualAudio(AudioZone zone)
        {
            this.zone = zone;
        }

        public void HandleSecondaryAudio()
        {
            if (zone.audioSource == null)
            {
                CleanupSecondaryAudio();
                return;
            }

            // Cancel any fade-out in progress.
            if (secondaryAudioObj != null && isFadingOut)
            {
                if (fadeCoroutine != null)
                    zone.StopCoroutine(fadeCoroutine);
                isFadingOut = false;
                fadeCoroutine = zone.StartCoroutine(FadeInSecondary());
            }

            // Create the secondary audio source if needed.
            if (secondaryAudioObj == null)
            {
                secondaryAudioObj = new GameObject("SecondaryAudioSource");
                secondaryAudioObj.transform.parent = zone.transform;
                secondaryAudioSource = secondaryAudioObj.AddComponent<AudioSource>();
                CopyAudioSourceProperties(zone.audioSource, secondaryAudioSource);
                baseSecondaryVolume = secondaryAudioSource.volume;
                secondaryAudioSource.volume = 0f;
                secondaryFadeFactor = 0f;
                fadeCoroutine = zone.StartCoroutine(FadeInSecondary());
                if (zone.enableOcclusion && secondaryAudioObj.GetComponent<AudioLowPassFilter>() == null)
                {
                    AudioLowPassFilter lpf = secondaryAudioObj.AddComponent<AudioLowPassFilter>();
                    lpf.cutoffFrequency = zone.defaultLowPassCutoff;
                }
            }

            // Synchronize secondary audio source with primary only if necessary.
            if (!secondaryAudioSource.isPlaying ||
                secondaryAudioSource.clip != zone.audioSource.clip ||
                Mathf.Abs(secondaryAudioSource.time - zone.audioSource.time) > 0.1f)
            {
                secondaryAudioSource.Stop();
                secondaryAudioSource.clip = zone.audioSource.clip;
                secondaryAudioSource.time = zone.audioSource.time;
                secondaryAudioSource.Play();
            }
        }

        /// <summary>
        /// Initiates a fade-out and cleanup of the secondary audio source.
        /// </summary>
        public void CleanupSecondaryAudio()
        {
            if (secondaryAudioObj != null && !isFadingOut)
            {
                fadeCoroutine = zone.StartCoroutine(FadeOutSecondary());
            }
        }

        public bool SecondaryAudioSourceExists() => secondaryAudioSource != null;
        public Vector3 GetSecondaryAudioPosition() => secondaryAudioObj ? secondaryAudioObj.transform.position : Vector3.zero;
        public AudioSource GetSecondaryAudioSource() => secondaryAudioSource;

        /// <summary>
        /// Stops and cleans up the secondary audio source immediately.
        /// </summary>
        public void StopAndCleanup()
        {
            if (secondaryAudioObj != null)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(secondaryAudioObj);
                else
                    GameObject.DestroyImmediate(secondaryAudioObj);
                secondaryAudioObj = null;
                secondaryAudioSource = null;
                fadeCoroutine = null;
                isFadingOut = false;
            }
        }

        /// <summary>
        /// Gradually fades in the secondary audio source.
        /// </summary>
        private IEnumerator FadeInSecondary()
        {
            float elapsed = 0f;
            secondaryFadeFactor = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                secondaryFadeFactor = Mathf.Clamp01(elapsed / fadeInDuration);
                if (!zone.enableOcclusion && secondaryAudioSource != null)
                    secondaryAudioSource.volume = baseSecondaryVolume * secondaryFadeFactor;
                yield return null;
            }
            secondaryFadeFactor = 1f;
            if (!zone.enableOcclusion && secondaryAudioSource != null)
                secondaryAudioSource.volume = baseSecondaryVolume;
            fadeCoroutine = null;
        }

        /// <summary>
        /// Gradually fades out the secondary audio source and then cleans it up.
        /// </summary>
        private IEnumerator FadeOutSecondary()
        {
            isFadingOut = true;
            float elapsed = 0f;
            float startFade = secondaryFadeFactor;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                secondaryFadeFactor = Mathf.Clamp01(startFade * (1 - elapsed / fadeOutDuration));
                if (!zone.enableOcclusion && secondaryAudioSource != null)
                    secondaryAudioSource.volume = baseSecondaryVolume * secondaryFadeFactor;
                yield return null;
            }
            secondaryFadeFactor = 0f;
            if (secondaryAudioObj != null)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(secondaryAudioObj);
                else
                    GameObject.DestroyImmediate(secondaryAudioObj);
            }
            secondaryAudioObj = null;
            secondaryAudioSource = null;
            fadeCoroutine = null;
            isFadingOut = false;
        }

        /// <summary>
        /// Copies the properties from one AudioSource to another.
        /// </summary>
        private void CopyAudioSourceProperties(AudioSource source, AudioSource destination)
        {
            destination.clip = source.clip;
            destination.outputAudioMixerGroup = source.outputAudioMixerGroup;
            destination.mute = source.mute;
            destination.bypassEffects = source.bypassEffects;
            destination.bypassListenerEffects = source.bypassListenerEffects;
            destination.bypassReverbZones = source.bypassReverbZones;
            destination.playOnAwake = source.playOnAwake;
            destination.loop = source.loop;
            destination.priority = source.priority;
            destination.volume = source.volume;
            destination.pitch = source.pitch;
            destination.panStereo = source.panStereo;
            destination.spatialBlend = source.spatialBlend;
            destination.reverbZoneMix = source.reverbZoneMix;
            destination.dopplerLevel = 0;
            destination.spread = source.spread;
            destination.rolloffMode = source.rolloffMode;
            destination.minDistance = source.minDistance;
            destination.maxDistance = source.maxDistance;
        }
    }
}
