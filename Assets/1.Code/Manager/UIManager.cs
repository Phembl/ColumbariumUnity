using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using COLUMBARIUM.Global;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [TitleGroup("References")] 
    [Header("TextObjects")] 
    [SerializeField] private TextFile[] narrationFiles;
    [SerializeField] private TextFile ControlsTextFile;
    
    [Header("Components")]
    [SerializeField] private Image blackScreen;
    [SerializeField] private Image loadIcon;
    [Space]
    [SerializeField] private TextMeshProUGUI narrationTMP;
    [SerializeField] private TextMeshProUGUI answer1TMP;
    [SerializeField] private TextMeshProUGUI answer2TMP;
    [SerializeField] private TextMeshProUGUI controllerTMP;
    [SerializeField] private TextMeshProUGUI scan1TMP;
    [SerializeField] private TextMeshProUGUI scan2TMP;
    [Space]
    [SerializeField] private CanvasGroup textHolder;
    [SerializeField] private CanvasGroup creditsHolder;
    [SerializeField] private CanvasGroup scanTextHolder;
   
    
    [TitleGroup("Settings")]
    [SerializeField] private float controlsDisplayDuration = 5f;
    [SerializeField] private float creditDuration = 60f;
    [SerializeField] private bool showScanCounter = true;

    //StateTracking
    private bool loadIconActive;
    private bool scanCountActive;

    public static UIManager instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    
    public void InitUI()
    {
        // Initialize UI
        blackScreen.DOFade(1f, 0f); //Start with black Screen
        loadIcon.DOFade(0f, 0f);
        textHolder.DOFade(0f, 0f);;
        narrationTMP.DOFade(0f, 0f);
        answer1TMP.DOFade(0f, 0f);
        answer2TMP.DOFade(0f, 0f);
        creditsHolder.DOFade(0f, 0f);
        controllerTMP.DOFade(0f, 0f);
        scanTextHolder.DOFade(0f, 0f);
        
        Debug.Log("UIManager initialized.");
    }
    
    public IEnumerator UseBlackScreen(bool activate, bool fade = false, bool finishLoading = false)
    {
        float fadeTime = GameManager.instance.GetFadeTime();
        if (activate)
        {
            if (fade) blackScreen.DOFade(1f, fadeTime);
            else blackScreen.DOFade(1f, 0f);
        }

        else //FadeOut
        {
            if (finishLoading && loadIconActive)
            {
                LoadIcon(false);
            }
            
            if (fade) blackScreen.DOFade(0f, fadeTime);
            else blackScreen.DOFade(0f, 0f);
        }
        
        yield return new WaitForSeconds(fadeTime + 0.5f);
        
    }

    public void ShowNarrationText(int narrationIndex)
    {
        narrationTMP.DOFade(0f, 0f);
        textHolder.DOFade(1f, 0f);
        narrationTMP.text = narrationFiles[narrationIndex].text;
        
        narrationTMP.DOFade(1f, 1.5f);
    }

    public void HideNarrationText()
    {
        narrationTMP.DOFade(0f, 1.5f)
            .OnComplete(() => narrationTMP.text = "");
    }

    public void LoadIcon(bool activate)
    {
        if (activate)
        {
            loadIconActive = true;
            loadIcon.DOFade(1f, 1f);
        }
        
        else
        {
            loadIconActive = false;
            loadIcon.DOFade(0f, 1f);
        }
    }

    public void ShowScanCounter(Chapter chapter, bool update = true, int scanCount = 0)
    {
        if (!showScanCounter) return;
        
        string scanCountText = scanCount.ToString();
        string countTarget = GlobalProgress.GetStorypointCounter((int)chapter).ToString();
        
        if (update)
        {
            scan2TMP.text = $"{scanCountText} / {countTarget}";
        }

        else
        {
            string chapterName = GlobalProgress.chapterNames[(int)chapter];
            
            scan1TMP.text = chapterName;
            scan2TMP.text = $"{scanCountText} / {countTarget}";
            scanTextHolder.DOFade(1f, 2f);
            
            scanCountActive = true;
        }
    }

    public IEnumerator ShowControllerText()
    {
        string controlsText;
        if (GlobalProgress.english) controlsText = ControlsTextFile.textEng;
        else  controlsText = ControlsTextFile.text;
        
        controllerTMP.text = controlsText;
        controllerTMP.DOFade(1f, 0f);
        textHolder.DOFade(1f, 1.5f);
        
        yield return new WaitForSeconds(controlsDisplayDuration);
        
        textHolder.DOFade(0f, 1.5f);
        yield return new WaitForSeconds(1.5f);
    }



}
