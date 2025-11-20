using Sirenix.OdinInspector;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    
    public enum Language
    {
        German,
        English
    }

    [TitleGroup("Settings")] 
    [Header("Language")]
    public Language language;

    [Header("Gameplay")] 
    public bool showScanCounter;
    
    [Header("UI")]
    [SerializeField, Range(1, 5)] public float blackScreenFadeTime = 2f;
    [SerializeField, Range(1, 5)] public float controlsDisplayDuration = 4f;
    [SerializeField, Range(30, 90)] public int creditsDuration = 60;
    
    [Header("Controller")]
    [Header("Human")]
    [SerializeField, Range(1, 10)] public int humanMoveSpeed = 6;
    [SerializeField, Range(5, 15)] public int humanLookSensitivity = 10;
    [Header("Bird")]
    [SerializeField, Range(1, 10)] public int birdRiseSpeed = 6;
    [SerializeField, Range(5, 10)] public int birdGlideSpeed = 8;
    [SerializeField, Range(0, 5)] public float birdGravityPull = 1.5f;
    [SerializeField, Range(5, 15)] public int birdLookSensitivity = 10;
    [Header("Bug")]
    [SerializeField, Range(0.1f, 5)] public float bugMoveSpeed = 2f;
    [SerializeField, Range(0, 3)] public float bugLookSensitivity = 0.05f;
    
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
    
    
}
