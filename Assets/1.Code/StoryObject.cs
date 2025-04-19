using System.Collections;
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
    [SerializeField] private AudioClip storyAudioClip;

    // Interaction Settings
    private Color inactiveColor = new Color(0.75f, 0.75f, 0.75f, 1f); //Target Inactive Color;
    private float inactiveFadeTime = 2f;
    private float pulseTime = 2f;
    
    [SerializeField] private bool isStoryProgress;
    [ShowIf("isStoryProgress")]
    public int storyID;

    private GameObject storyObject;
    private GameObject activeModel;
    private GameObject worldText;
    private TextMeshPro worldTextMesh;
    private Color textColor;
    private MeshRenderer modelRenderer;
    private Material modelMaterial;
    private Tween colorTween;
    private Tween pulseTween;

    // Track if this story has been triggered
    private bool hasBeenTriggered = false;

    // We're using trigger colliders only

    private void Start()
    {
        
        // Get Parent which is the actual StoryHolder
        
        
        // Setup Model
        Transform childTransform = transform.Find("StoryObject");
        activeModel = childTransform?.gameObject;
        if (activeModel != null)
        {
            activeModel.SetActive(true);
            modelRenderer = activeModel.GetComponent<MeshRenderer>();
            modelMaterial = modelRenderer.material;
        }
        else
        {
            Debug.LogError("No Story Object found!");
        }
        

        
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
        
    }
    

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
        
        StoryManager.Instance.PlayClipAtPointUsingPrefab(storyAudioClip, transform.position);
        
        
        if (colorTween.IsActive()) colorTween.Kill(); 
        
        if (pulseTween.IsActive()) pulseTween.Kill();
        modelMaterial.DOFloat(1f, "_SizeVariation", 0.5f);

        colorTween = modelMaterial.DOColor(inactiveColor, "_Tint", inactiveFadeTime).SetEase(Ease.InQuad);
        
        // Fade in World Text
        if (worldText != null)
        {
            worldTextMesh.DOFade(1f, 20f);
                
        }
        
        StartCoroutine(WaitForReset(storyAudioClip.length + 1f));
    }
    
    public void OnHoverEnter()
    {
        if (hasBeenTriggered)
            return;
        
        if (pulseTween.IsActive()) pulseTween.Kill();
        pulseTween = modelMaterial.DOFloat(2f, "_SizeVariation", pulseTime).SetEase(Ease.InOutQuad).SetLoops(-1, LoopType.Yoyo);

    }
    
    
    public void OnHoverExit()
    {
        if (hasBeenTriggered)
            return;

        if (pulseTween.IsActive()) pulseTween.Kill();
        pulseTween = modelMaterial.DOFloat(1f, "_SizeVariation", 0.5f);
    }

    private IEnumerator WaitForReset(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        hasBeenTriggered = false;
    }
    
    
}