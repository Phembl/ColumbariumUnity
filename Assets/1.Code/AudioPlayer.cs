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

    public void PlayAudio(AudioClip audioClip)
    {
        Debug.Log("Playing storyPoint Audio for storypoint: " + gameObject.name);
        
        audioSource.clip = audioClip;
        audioSource.Play();
        StartCoroutine(WaitForAudioEnd());
    }

    public void PauseAudio()
    {
        Debug.Log("Pausing storyPoint Audio for storypoint: " + gameObject.name);
        
        isPaused = true;
        DOTween.Kill(audioSource);
        audioSource.DOFade(0f, 0.2f)
         .OnComplete(() => audioSource.Pause());

    }

    public void UnpauseAudio()
    {
        Debug.Log("Unpausing storyPoint Audio for storypoint: " + gameObject.name);
        
        isPaused = false;
        audioSource.UnPause();
        DOTween.Kill(audioSource);
        audioSource.DOFade(1f, 0.2f);
    }
    
    public void StopAudio(bool fade = true)
    {
        Debug.Log("Stoping storyPoint Audio for storypoint: " + gameObject.name);
        
        shouldBeDestroyed = true;
        shouldFade = fade;
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

        //StoryManager.Instance.StoryAudioHasFinished();

    }

  
}
