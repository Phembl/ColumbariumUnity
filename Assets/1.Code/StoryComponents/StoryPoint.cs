using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using VInspector;

/// <summary>
/// Represents an object in the world that triggers a story moment when hit by the player.
/// </summary>
public class StoryPoint : MonoBehaviour
{
    [Header("Story Content")]
    [Tooltip("The text to display during the story moment")]
    [SerializeField, TextArea(3, 10)] private string storyText;
    
    [Tooltip("The audio clip to play during the story moment")]
    public AudioClip storyAudioClip;
    public AudioClip storyAudioClipEN;
    [SerializeField] private bool makeAudioBigger;
    [Space]

    [SerializeField] private bool hasWorldText;
    [SerializeField] private TMP_Text[] worldTextTMP;
    private TMP_Text worldText;


    // Interaction Settings
    private Color inactiveColor = new Color(0.75f, 0.75f, 0.75f, 1f); //Target Inactive Color;
    private float inactiveFadeTime = 2f;
    private float pulseTime = 2f;
    
    private GameObject storyObject;
    private GameObject activeModel;
   
    private Color textColor;
    private MeshRenderer modelRenderer;
    private Material modelMaterial;
    private Tween colorTween;
    private Tween pulseTween;

    // Track if this story has been triggered
    private bool hasBeenTriggeredBefore;
    private bool isCurrentlyPlaying;

    //Settings
    private bool english;

    private Coroutine resetTimer;
    

    public void Reset() //This is called at the beginning of the game by StoryManager
    {
        UpdateSettings();
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
        
        // Starting Color is black, objects are faded in
        modelMaterial.DOColor(Color.black, "_Tint", 0f);
        
        // Setup Worldtext
        if (hasWorldText)
        {
            if (worldTextTMP.Length != 2)
            {
                Debug.Log($"{gameObject.name} has not enough worldText Objects. Setup aborted");
                hasWorldText = false;
                return;
            }
            if (english) worldText = worldTextTMP[1];
            else worldText = worldTextTMP[0];
            
            worldTextTMP[0].DOFade(0f, 0f);
            worldTextTMP[1].DOFade(0f, 0f);
        }

     
    }
    
    
    private void UpdateSettings()
    {
        switch (SettingsManager.instance.language)
        {
            case SettingsManager.Language.German:
                english = false;
                break;
                
            case SettingsManager.Language.English:
                english = true;
                break;
        }
    }

    public void FadeIn(bool invert = false)
    {
        hasBeenTriggeredBefore = false;
        if (!invert)
        {
            modelMaterial.DOColor(Color.white, "_Tint", 2f); 
        }
        else
        {
            modelMaterial.DOColor(Color.black, "_Tint", 2f);
        }
        
    }
    
    public void FadeOut()
    {
        
        modelMaterial.DOColor(Color.black, "_Tint", 1f);
        
    }

    public void OnInteract()
    {
        if (isCurrentlyPlaying)
            return;
        
        Debug.Log("Starting interaction with " + gameObject.name);
        
        isCurrentlyPlaying =  true;
        
        if (colorTween.IsActive()) colorTween.Kill(); 
        if (pulseTween.IsActive()) pulseTween.Kill();
        modelMaterial.DOFloat(1f, "_SizeVariation", 0.5f);
        
        if (!hasBeenTriggeredBefore)
        {
            hasBeenTriggeredBefore = true;
            colorTween = modelMaterial.DOColor(inactiveColor, "_Tint", inactiveFadeTime).SetEase(Ease.InQuad);
            AudioManager.instance.PlayStoryPointAudio(this, makeAudioBigger);
            StoryManager2.instance.CheckStoryProgress();

            if (hasWorldText) StartCoroutine(ShowWorldText());
        }
        
        else
        {
            AudioManager.instance.PlayStoryPointAudio(this, makeAudioBigger);
        }
        
        resetTimer = StartCoroutine(WaitForReset(storyAudioClip.length + 1f));
    }
    
    private IEnumerator ShowWorldText()
    {
        yield return new WaitForSeconds(storyAudioClip.length - 2f);
        if (hasWorldText) worldText.DOFade(1f, 20f);
    }
    
    public void OnHoverEnter()
    {
        if (isCurrentlyPlaying)
            return;
        
        if (pulseTween.IsActive()) pulseTween.Kill();
        pulseTween = modelMaterial.DOFloat(2f, "_SizeVariation", pulseTime).SetEase(Ease.InOutQuad).SetLoops(-1, LoopType.Yoyo);

    }
    
    public void OnHoverExit()
    {
        if (isCurrentlyPlaying)
            return;

        if (pulseTween.IsActive()) pulseTween.Kill();
        pulseTween = modelMaterial.DOFloat(1f, "_SizeVariation", 0.5f);
    }

    private IEnumerator WaitForReset(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        isCurrentlyPlaying = false;
    }

    public void ResetImmediately()
    {
        if (resetTimer != null) StopCoroutine(resetTimer);
        resetTimer = null;
        isCurrentlyPlaying = false;
    }
    
    
}