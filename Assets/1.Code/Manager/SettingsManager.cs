using System;
using COLUMBARIUM.Global;
using Sirenix.OdinInspector;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    
    

    [TitleGroup("Settings")] 
    [Header("Language")]
    [SerializeField] private Language language;

    [Header("Gameplay")] 
    public bool showScanCounter;
    
    [Header("UI")]
    [SerializeField, Range(1, 5)] public float blackScreenFadeTime = 2f;
    [SerializeField, Range(1, 8)] public float controlsDisplayDuration = 4f;
    [SerializeField, Range(30, 90)] public float creditsDuration = 60;
    
    [Header("Controller")]
    [Header("Human")]
    [SerializeField, Range(1, 10)] public float humanMoveSpeed = 6;
    [SerializeField, Range(5, 15)] public float humanLookSensitivity = 10;
    [Header("Bird")]
    [SerializeField, Range(1, 10)] public float birdRiseSpeed = 6;
    [SerializeField, Range(5, 10)] public float birdGlideSpeed = 8;
    [SerializeField, Range(0, 5)] public float birdGravityPull = 1.5f;
    [SerializeField, Range(5, 15)] public float birdLookSensitivity = 10;
    [Header("Bug")]
    [SerializeField, Range(0.1f, 5)] public float bugMoveSpeed = 2f;
    [SerializeField, Range(0, 3)] public float bugLookSensitivity = 0.1f;
    
    //TODO: ControllerSettings
    
    public static SettingsManager instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
    }
    
    public Language GetLanguage()
    {
        switch (language)
        {
            case Language.German:
                return Language.German;
                
            case Language.English:
                return Language.English;
            
            default:
                return Language.English;
        }
    }

    public Settings GetSettings()
    {
        Settings currentSettings = new Settings();
        
        currentSettings._language = language;
        currentSettings._showScanCounter = showScanCounter;
        currentSettings._blackScreenFadeTime = blackScreenFadeTime;
        currentSettings._controlsDisplayDuration = controlsDisplayDuration;
        currentSettings._creditsDuration = creditsDuration;
        currentSettings._humanMoveSpeed = humanMoveSpeed;
        currentSettings._humanLookSensitivity = humanLookSensitivity;
        currentSettings._birdRiseSpeed  = birdRiseSpeed;
        currentSettings._birdGlideSpeed = birdGlideSpeed;
        currentSettings._birdGravityPull = birdGravityPull;
        currentSettings._birdLookSensitivity = birdLookSensitivity;
        currentSettings._bugMoveSpeed = bugMoveSpeed;
        currentSettings._bugLookSensitivity = bugLookSensitivity;
        
        return currentSettings;
    }

    public static event Action SettingsUpdated;
        
    public void UpdateSettings(Settings newSettings)
    {
        language = newSettings._language;
        showScanCounter = newSettings._showScanCounter;
        blackScreenFadeTime = newSettings._blackScreenFadeTime;
        controlsDisplayDuration = newSettings._controlsDisplayDuration;
        creditsDuration = newSettings._creditsDuration;
        humanMoveSpeed = newSettings._humanMoveSpeed;
        humanLookSensitivity = newSettings._humanLookSensitivity;
        birdRiseSpeed = newSettings._birdRiseSpeed ;
        birdGlideSpeed = newSettings._birdGlideSpeed;
        birdGravityPull = newSettings._birdGravityPull;
        birdLookSensitivity = newSettings._birdLookSensitivity;
        bugMoveSpeed = newSettings._bugMoveSpeed;
        bugLookSensitivity = newSettings._bugLookSensitivity;
        
        SettingsUpdated?.Invoke();
    }
    
    
}
