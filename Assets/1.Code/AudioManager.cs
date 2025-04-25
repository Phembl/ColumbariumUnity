using System;
using UnityEngine;
using UnityEngine.Audio;
using VInspector;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    
    [Header("Volume Settings")] 
    [Range(-80, 20)]
    public int voiceAttenuation = 0;
    [Range(-80, 20)]
    public int storyAttenuation = 0;
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
    
    [Button("Set Attenuation")]
    public void button()
    {
        changeVolume();
    }

    private void Awake()
    {

        
        
    }

    void Start()
    { 
       changeVolume();
    }

    void changeVolume()
    {
        audioMixer.SetFloat("VoiceVol", (voiceAttenuation));
        audioMixer.SetFloat("StoryVol", (storyAttenuation));
        audioMixer.SetFloat("NichtsVol", (nichtsAttenuation));
        audioMixer.SetFloat("GartenVol", (gartenAttenuation));
        audioMixer.SetFloat("TaubenschlagVol", (taubenschlagAttenuation));
        audioMixer.SetFloat("PidgeonVol", (pidgeonAttenuation));
        audioMixer.SetFloat("TricksterVol", (tricksterAttenuation));
        audioMixer.SetFloat("EmbryoVol", (embryoAttenuation));
    }
}
