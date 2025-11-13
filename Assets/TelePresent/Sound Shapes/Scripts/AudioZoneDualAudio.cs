using UnityEngine;
using System.Collections;

namespace TelePresent.SoundShapes
{
    /// <summary>
    /// Handles creation, fading and destruction of the secondary
    /// AudioSource used for the dual‑audio feature.
    /// </summary>
    public class AudioZoneDualAudio
    {
        private readonly AudioZone _zone;

        public  GameObject  SecondaryAudioObj;
        public  AudioSource SecondaryAudioSource;

        private Coroutine _fadeCoroutine;
        private bool      _isFadingOut;

        public  float  SecondaryFadeFactor = 1f;
        public  float  BaseSecondaryVolume = 1f;

        public float FadeInDuration  = 2f;
        public float FadeOutDuration = 2f;

        // ──────────────────────────────────────────────────────────────────────────────
        public AudioZoneDualAudio(AudioZone zone) => this._zone = zone;

        // ──────────────────────────────────────────────────────────────────────────────
        #region Public API
        public void HandleSecondaryAudio()
        {
            if (_zone.trackingMode == AudioZone.TrackingMode.Object && !_zone.trackingObject)
            {
                CleanupSecondaryAudio();
                return;
            }

            if (SecondaryAudioObj && _isFadingOut)
            {
                if (_fadeCoroutine != null)
                    _zone.StopCoroutine(_fadeCoroutine);

                _isFadingOut   = false;
                _fadeCoroutine = _zone.StartCoroutine(FadeInSecondary());
            }

            if (!SecondaryAudioObj)
            {
                SecondaryAudioObj = new GameObject("SecondaryAudioSource")
                {
                    transform =
                    {
                        parent = _zone.transform
                    }
                };
#if UNITY_EDITOR
                SecondaryAudioObj.hideFlags = HideFlags.DontSave;
#endif
                if (_zone.audioSource)
                {
                    SecondaryAudioSource = SecondaryAudioObj.AddComponent<AudioSource>();
                    CopyAudioSourceProperties(_zone.audioSource, SecondaryAudioSource);

                    BaseSecondaryVolume        = SecondaryAudioSource.volume;
                    SecondaryAudioSource.volume = 0f;
                    SecondaryFadeFactor        = 0f;

                    SecondaryAudioSource.Stop();
                    SecondaryAudioSource.clip = _zone.audioSource.clip;
                    SecondaryAudioSource.time = _zone.audioSource.time;
                    SecondaryAudioSource.Play();
                }

                _fadeCoroutine = _zone.StartCoroutine(FadeInSecondary());

                if (_zone.enableOcclusion &&
                    !SecondaryAudioObj.GetComponent<AudioLowPassFilter>())
                {
                    var lpf = SecondaryAudioObj.AddComponent<AudioLowPassFilter>();
                    lpf.cutoffFrequency = _zone.defaultLowPassCutoff;
                }
            }
        }

        public void CleanupSecondaryAudio()
        {
            if (!SecondaryAudioObj) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(SecondaryAudioObj);
                SecondaryAudioObj    = null;
                SecondaryAudioSource = null;
                _fadeCoroutine        = null;
                _isFadingOut          = false;
                return;
            }
#endif
            if (!_isFadingOut)
                _fadeCoroutine = _zone.StartCoroutine(FadeOutSecondary());
        }

        public void StopAndCleanup()
        {
            if (SecondaryAudioObj)
            {
                if (Application.isPlaying)
                    Object.Destroy(SecondaryAudioObj);
                else
                    Object.DestroyImmediate(SecondaryAudioObj);

                SecondaryAudioObj    = null;
                SecondaryAudioSource = null;
                _fadeCoroutine        = null;
                _isFadingOut          = false;
            }
        }

        public void ApplyFallbackSecondaryVolume()
        {
            if (SecondaryAudioSource)
                SecondaryAudioSource.volume = BaseSecondaryVolume * SecondaryFadeFactor;
        }
        #endregion
        // ──────────────────────────────────────────────────────────────────────────────

        #region Fade Coroutines
        private IEnumerator FadeInSecondary()
        {
            float elapsed = 0f;
            SecondaryFadeFactor = 0f;

            while (elapsed < FadeInDuration)
            {
                elapsed += Time.deltaTime;
                SecondaryFadeFactor = Mathf.Clamp01(elapsed / FadeInDuration);

                if (!_zone.enableOcclusion && SecondaryAudioSource)
                    SecondaryAudioSource.volume = BaseSecondaryVolume * SecondaryFadeFactor;

                yield return null;
            }

            SecondaryFadeFactor = 1f;
            if (!_zone.enableOcclusion && SecondaryAudioSource)
                SecondaryAudioSource.volume = BaseSecondaryVolume;

            _fadeCoroutine = null;
        }

        private IEnumerator FadeOutSecondary()
        {
            _isFadingOut = true;
            float elapsed   = 0f;
            float startFade = SecondaryFadeFactor;

            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.deltaTime;
                SecondaryFadeFactor =
                    Mathf.Clamp01(startFade * (1f - elapsed / FadeOutDuration));

                if (!_zone.enableOcclusion && SecondaryAudioSource)
                    SecondaryAudioSource.volume = BaseSecondaryVolume * SecondaryFadeFactor;

                yield return null;
            }

            SecondaryFadeFactor = 0f;

            if (Application.isPlaying)
                Object.Destroy(SecondaryAudioObj);
            else
                Object.DestroyImmediate(SecondaryAudioObj);

            SecondaryAudioObj    = null;
            SecondaryAudioSource = null;
            _fadeCoroutine        = null;
            _isFadingOut          = false;
        }
        #endregion
        // ──────────────────────────────────────────────────────────────────────────────

        #region Helpers
        private static void CopyAudioSourceProperties(AudioSource src, AudioSource dst)
        {
            dst.clip                  = src.clip;
            dst.outputAudioMixerGroup = src.outputAudioMixerGroup;
            dst.mute                  = src.mute;
            dst.bypassEffects         = src.bypassEffects;
            dst.bypassListenerEffects = src.bypassListenerEffects;
            dst.bypassReverbZones     = src.bypassReverbZones;
            dst.playOnAwake           = src.playOnAwake;
            dst.loop                  = src.loop;
            dst.priority              = src.priority;
            dst.volume                = src.volume;
            dst.pitch                 = src.pitch;
            dst.panStereo             = src.panStereo;
            dst.spatialBlend          = src.spatialBlend;
            dst.reverbZoneMix         = src.reverbZoneMix;
            dst.dopplerLevel          = 0f;
            dst.spread                = src.spread;
            dst.rolloffMode           = src.rolloffMode;
            dst.minDistance           = src.minDistance;
            dst.maxDistance           = src.maxDistance;
        }
        #endregion
    }
}
