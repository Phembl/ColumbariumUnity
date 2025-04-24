using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    
    private AudioSource audioSource;
    private bool isPaused;
    private bool shouldBeDestroyed;
    private bool shouldFade;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayAudio(AudioClip audioClip)
    {
        audioSource.clip = audioClip;
        audioSource.Play();
        StartCoroutine(WaitForAudioEnd());
    }

    public void PauseAudio()
    {
        isPaused = true;
        DOTween.Kill(audioSource);
        audioSource.DOFade(0f, 0.2f)
         .OnComplete(() => audioSource.Pause());

    }

    public void UnpauseAudio()
    {
        isPaused = false;
        audioSource.UnPause();
        DOTween.Kill(audioSource);
        audioSource.DOFade(1f, 0.2f);
    }
    
    private IEnumerator WaitForAudioEnd()
    {
        while ((audioSource.isPlaying || isPaused) && !shouldBeDestroyed)
        {
            yield return null;
        }

        if (shouldFade)
        {
            audioSource.DOFade(0f, 1.0f).OnComplete(() => Destroy(gameObject)) ;
        }
        else
        {
            Destroy(gameObject);
        }
        
    }

    public void StopAudio(bool fade = true)
    {
        shouldBeDestroyed = true;
        shouldFade = fade;
    }
}
