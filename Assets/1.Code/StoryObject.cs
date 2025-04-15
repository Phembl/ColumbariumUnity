using DG.Tweening;
using TMPro;
using UnityEngine;
using VInspector;

/// <summary>
/// Represents an object in the world that triggers a story moment when hit by the player.
/// </summary>
public class StoryObject : MonoBehaviour
{
    [Header("Story Content")]
    [Tooltip("The text to display during the story moment")]
    [SerializeField, TextArea(3, 10)] private string storyText;
    
    [Tooltip("The audio clip to play during the story moment")]
    [SerializeField] private AudioClip narrationAudio;

    [Header("Interaction Settings")]
    [SerializeField] private Color inactiveColor = new Color(0.75f, 0.75f, 0.75f, 1f); //Target Inactive Color;
    [SerializeField] private float inactiveFadeTime = 1f;
    [SerializeField] private float pulseTime = 2f;
    

    [SerializeField] private bool newMode;
    [SerializeField] private bool isStoryProgress;
    [ShowIf("isStoryProgress")]
    public int storyID;

    private GameObject activeModel;
    private GameObject worldText;
    private AudioSource narrationAudioSource;
    private TextMeshPro worldTextMesh;
    private Color textColor;
    private string playerTag = "Player"; // Tag to enter
    private MeshRenderer modelRenderer;
    private Material modelMaterial;
    private Tween colorTween;
    private Tween pulseTween;

    // Track if this story has been triggered
    private bool hasBeenTriggered = false;

    // We're using trigger colliders only

    private void Start()
    {
        
        // Setup Models
        Transform childTransform = transform.Find("Object");
        activeModel = childTransform?.gameObject;
        if (activeModel != null) activeModel.SetActive(true);
        
        modelRenderer = activeModel.GetComponent<MeshRenderer>();
        modelMaterial = modelRenderer.material;
        
        // Setup Worldtext
        childTransform = transform.Find("Worldtext");
        worldText = childTransform?.gameObject;
        if (worldText != null)
        {
            worldTextMesh = worldText.GetComponent<TextMeshPro>();
            textColor = worldTextMesh.color;
            textColor.a = 0;
            worldTextMesh.color = textColor;
        }
  
         
        childTransform = transform.Find("AudioSource");
        narrationAudioSource  = childTransform?.gameObject.GetComponent<AudioSource>();
        if (narrationAudioSource != null) narrationAudioSource.clip = narrationAudio;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            //TriggerStoryMoment();
        }
    }

    /*
    
    /// <summary>
    /// Triggers the story moment if conditions are met
    /// </summary>
    private void TriggerStoryMoment()
    {
        // Check if object can be triggered again
        if (hasBeenTriggered)
            return;

        // Mark as triggered
        hasBeenTriggered = true;

        if (newMode)
        {
            narrationAudioSource.Play();
            
            // Fade Color
            
            if (modelRenderer != null)
            {
                
                if (colorTween.IsActive()) colorTween.Kill(); 

                colorTween = modelMaterial.DOColor(inactiveColor, "_Tint", inactiveFadeTime)
                    .SetEase(Ease.Linear); // Use linear ease for predictable testing
                
            }
            else
            {
                Debug.LogWarning("No model renderer found on " + activeModel);
            }
            // Fade in World Text
            if (worldText != null)
            {
                worldTextMesh.DOFade(1f, 20f);
                
            }

        }
        else
        {
            // Send story content to the StoryManager
            StoryManager.Instance.TriggerStoryMoment(storyText, narrationAudio, worldText);
        }

        
        
    }
    
    */

    public void OnInteract()
    {
        if (hasBeenTriggered)
            return;
        
        hasBeenTriggered = true;
        
        if (isStoryProgress)
        {
            Debug.Log("Sending Story ID: " + storyID);
            StoryManager.Instance.ContinueStory(storyID);
        }
        
        narrationAudioSource.Play();
        
        if (colorTween.IsActive()) colorTween.Kill(); 
        
        if (pulseTween.IsActive()) pulseTween.Kill();
        modelMaterial.DOFloat(1f, "_SizeVariation", 0.5f);

        colorTween = modelMaterial.DOColor(inactiveColor, "_Tint", inactiveFadeTime);
        
        // Fade in World Text
        if (worldText != null)
        {
            worldTextMesh.DOFade(1f, 20f);
                
        }
    }
    
    public void OnHoverEnter()
    {
        if (hasBeenTriggered)
            return;
        
        if (pulseTween.IsActive()) pulseTween.Kill();
        //modelMaterial.DOFloat(0f, "_SizeVariation", 0f);
        pulseTween = modelMaterial.DOFloat(2f, "_SizeVariation", pulseTime).SetEase(Ease.InOutQuad).SetLoops(-1, LoopType.Yoyo);

    }
    
    
    public void OnHoverExit()
    {
        if (hasBeenTriggered)
            return;

        if (pulseTween.IsActive()) pulseTween.Kill();
        pulseTween = modelMaterial.DOFloat(1f, "_SizeVariation", 0.5f);
    }
    

    
}