using System;
using System.Collections;
using COLUMBARIUM.Global;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using AudioSettings = COLUMBARIUM.Global.AudioSettings;

public class AudioManager : MonoBehaviour
{
    [TitleGroup("References")]
    [Header("Prefabs")]
    [SerializeField] private GameObject storyPointAudioPlayer;
    [Header("Components")]
    [SerializeField] private AudioSource narrationAudioSource;
    [SerializeField] private AudioMixer audioMixer;

    [Header("NarrationAudio")] 
    [SerializeField] private AudioClip[] narrationAudioClips;
    
    [Header("Volume Settings")] 
    [Range(-80, 20)]
    public int voiceAttenuation = 0;
    [Range(-80, 20)]
    public int storyAttenuation = 0;
    [Range(-80, 20)]
    public int cinematicAttenuation = 0;
    [Range(-80, 20)]
    public int nichtsAttenuation = 0;
    [Range(-80, 20)]
    public int gartenAttenuation = 0;
    [Range(-80, 20)]
    public int taubenschlagAttenuation = 0;
    [Range(-80, 20)]
    public int pidgeonAttenuation = 0;
    [Range(-80, 20)]
    public int tricksterAttenuation = 0;
    [Range(-80, 20)]
    public int embryoAttenuation = 0;
    [Range(-80, 20)]
    public int neinGartenAttenuation = 0;
    
    private int atmosphereAttenuation = 0;
    
    //Settings
    //private bool english;
    private Language language;
    
    //Master
    private float masterVolume;
    //StoryPoint Audio
    private bool storyPointIsPlaying;
    private AudioPlayer currentStoryPointAudioPlayer;
    
    private StoryPoint lastStoryPoint;
    
    [Button("Set Attenuation")]
    public void button()
    {
        changeVolume();
    }

    public static AudioManager instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        MenuManager.PauseGame += GamePaused;
        
    }

    void Start()
    { 
        audioMixer.GetFloat("MasterVol", out masterVolume);
        changeVolume();
        UpdateSettings();
    }
    
    private void UpdateSettings()
    {
        language = SettingsManager.instance.GetLanguage();
    }

    void GamePaused(bool paused)
    {
        if (paused)
        {
            if (currentStoryPointAudioPlayer != null) currentStoryPointAudioPlayer.PauseAudio();
           
        }
        else
        {
            if (currentStoryPointAudioPlayer != null) currentStoryPointAudioPlayer.UnpauseAudio();
        }
    }
    
    void changeVolume()
    {
        audioMixer.SetFloat("VoiceVol", (voiceAttenuation));
        audioMixer.SetFloat("StoryVol", (storyAttenuation));
        audioMixer.SetFloat("CinematicVol", (cinematicAttenuation));
        audioMixer.SetFloat("NichtsVol", (nichtsAttenuation));
        audioMixer.SetFloat("GartenVol", (gartenAttenuation));
        audioMixer.SetFloat("TaubenschlagVol", (taubenschlagAttenuation));
        audioMixer.SetFloat("PidgeonVol", (pidgeonAttenuation));
        audioMixer.SetFloat("TricksterVol", (tricksterAttenuation));
        audioMixer.SetFloat("EmbryoVol", (embryoAttenuation));
        audioMixer.SetFloat("NeinGartenVol", (neinGartenAttenuation));
    }

    public AudioSettings GetAudioSettings()
    {
        AudioSettings currentAudioSettings = new AudioSettings();

        currentAudioSettings._narrationAtt = voiceAttenuation;
        currentAudioSettings._storyPointsAtt = storyAttenuation;
        currentAudioSettings._cinematicAtt = cinematicAttenuation;
        currentAudioSettings._atmosphereAtt = atmosphereAttenuation;
        
        return currentAudioSettings;
    }

    public void UpdateAudioSettings(AudioSettings newAudioSettings)
    {
        float newNarrationAtt = (voiceAttenuation + newAudioSettings._narrationAtt);
        
        audioMixer.SetFloat("VoiceVol", (newNarrationAtt));
        audioMixer.SetFloat("StoryVol", (storyAttenuation));
        audioMixer.SetFloat("CinematicVol", (cinematicAttenuation));
    }

    public float PlayNarrationAudio(int narrationIndex)
    {
        float audioTime = 0f;
        
        
        AudioClip narrationClip = narrationAudioClips[narrationIndex];

        if (language == Language.English && narrationIndex == 4) //Override Farewell
        {
            narrationClip = narrationAudioClips[7];
        }

        narrationAudioSource.clip = narrationClip;
        
        narrationAudioSource.Play();
        
        return narrationClip.length;
    }

    public void StopNarrationAudio()
    {
        narrationAudioSource.Stop();
    }

    public void PlayStoryPointAudio(StoryPoint storyPoint, bool makeBigger = false)
    {
        
        AudioClip audioClip = null;
        
        switch (language)
        {
            case Language.German:
                audioClip = storyPoint.storyAudioClip;
                break;
            
            case Language.English:
                audioClip = storyPoint.storyAudioClipEN;
                break;
            
            default:
                audioClip = storyPoint.storyAudioClipEN;
                break;
        }
        
        if (audioClip == null) audioClip = storyPoint.storyAudioClip; //Fallback to german
        
        Vector3 position = storyPoint.gameObject.transform.position;
        
        if (currentStoryPointAudioPlayer != null)
        {
            //FadeOut currently playing story
            lastStoryPoint.ResetImmediately();
            currentStoryPointAudioPlayer.StopAudio();
        }
        lastStoryPoint = storyPoint;
        
        GameObject newStoryPointAudioPlayer = Instantiate(storyPointAudioPlayer, position, Quaternion.identity);
        newStoryPointAudioPlayer.name = $"AudioPlayer_{storyPoint.name}";
        
        currentStoryPointAudioPlayer = newStoryPointAudioPlayer.GetComponent<AudioPlayer>();

        if (makeBigger)
        {
            AudioSource currentAudioSource = currentStoryPointAudioPlayer.gameObject.GetComponent<AudioSource>();
            if (currentAudioSource != null) currentAudioSource.maxDistance *= 3;
        }
        
        currentStoryPointAudioPlayer.PlayAudio(audioClip);
        
    }

    public IEnumerator FadeAwayAudio(float fadeTime = 2f, bool resetAfter = true)
    {
        audioMixer.GetFloat("MasterVol", out masterVolume);
        audioMixer.DOSetFloat("MasterVol", -80f, fadeTime);
        yield return new WaitForSeconds(fadeTime);
        
    }

    public void ResetMasterVolume()
    {
        audioMixer.SetFloat("MasterVol", masterVolume);
    }

}
