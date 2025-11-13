using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

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
    
    //StoryPoint Audio
    private bool storyPointIsPlaying;
    private AudioPlayer currentStoryPointAudioPlayer;
    
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
        
    }

    void Start()
    { 
       changeVolume();
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

    public float PlayNarrationAudio(int narrationIndex)
    {
        float audioTime = 0f;
        
        
        AudioClip narrationClip = narrationAudioClips[narrationIndex];

        narrationAudioSource.clip = narrationClip;
        
        narrationAudioSource.Play();
        
        return narrationClip.length;
    }

    public void PlayStoryPointAudio(StoryPoint storyPoint)
    {
        
        
        AudioClip audioClip = storyPoint.storyAudioClip;
        Vector3 position = storyPoint.gameObject.transform.position;
        
        if (currentStoryPointAudioPlayer != null)
        {
            //FadeOut currently playing story
           currentStoryPointAudioPlayer.StopAudio();
        }
        
        
        GameObject newStoryPointAudioPlayer = Instantiate(storyPointAudioPlayer, position, Quaternion.identity);
        newStoryPointAudioPlayer.name = $"AudioPlayer_{storyPoint.name}";
        
        currentStoryPointAudioPlayer = newStoryPointAudioPlayer.GetComponent<AudioPlayer>();
        currentStoryPointAudioPlayer.PlayAudio(audioClip);

        //GameObject storyPoint, AudioClip clip, Vector3 position

        /*
        if (chapterIsFadingOut)
        {
            // Don't allow any new Audio if the chapter is currently fading out to avoid overlapping bugs
            return;
        }



       

        // Create Story Audio Object
        if (voiceOnly)
        {
            storyAudioPlayerObject = Object.Instantiate(voiceAudioPlayer, position, Quaternion.identity);
        }
        else
        {
            storyAudioPlayerObject = Object.Instantiate(storyPointAudioPlayer, player.transform.position, Quaternion.identity);
        }

        storyAudioPlayerObject.GetComponent<AudioPlayer>().PlayAudio(clip);
        waitForAudioEndTime = storyAudioPlayerObject.GetComponent<AudioSource>().clip.length + 1f;
        */
    }

}
