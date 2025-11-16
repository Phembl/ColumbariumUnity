using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using COLUMBARIUM.Global;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UIManager : MonoBehaviour
{
    [TitleGroup("References")] 
    [Header("TextObjects")] 
    [SerializeField] private TextFile[] narrationFiles;
    [SerializeField] private TextFile[] answerFiles;
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
    public int selectedAnswer;
    private bool tricksterQuestion;
    
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

    #region |---------- ANSWERS ----------|
    
    public void ShowAnswerText(Chapter chapter)
    {
        
        answer1TMP.fontStyle = FontStyles.Underline;
        answer2TMP.fontStyle = FontStyles.Normal;
        
        
        switch (chapter)
        {
            case Chapter.TAUBENSCHLAG_QUESTION:
                answer1TMP.text = answerFiles[0].text;
                answer2TMP.text = answerFiles[1].text;
                break;
            
            case Chapter.GARTEN_INV_QUESTION:
                answer1TMP.text = answerFiles[2].text;
                answer2TMP.text = answerFiles[3].text;
                break;
            
            case Chapter.PIGEON_QUESTION:
                tricksterQuestion = true;
                answer1TMP.text = answerFiles[Random.Range(4, 12)].text;
                answer2TMP.text = answerFiles[Random.Range(4, 12)].text;
                break;
            
            
            default:
                Debug.LogWarning("Answers requested for an invalid chapter:"  + chapter);
                break;
        }

        answer1TMP.DOFade(1f, 2f);
        answer2TMP.DOFade(1f, 2f);

        readyForInteraction = true;
        selectedAnswer = 1;
        
    }

    private void CheckAnswer(int answer)
    {
        if (!readyForInteraction) return;

        switch (answer)
        {
            case 1:
                if (selectedAnswer == 1) return;
                
                answer1TMP.fontStyle = FontStyles.Underline;
                answer2TMP.fontStyle = FontStyles.Normal;

                if (tricksterQuestion)
                {
                    answer1TMP.text = answerFiles[Random.Range(4, 13)].text;
                }
                
                selectedAnswer = 1;
                
                Debug.Log("Selected Answer 1");
                break;
            
            case 2:
                if  (selectedAnswer == 2) return;
                
                answer1TMP.fontStyle = FontStyles.Normal;
                answer2TMP.fontStyle = FontStyles.Underline;

                if (tricksterQuestion)
                {
                    answer2TMP.text = answerFiles[Random.Range(4, 13)].text;
                }
                
                selectedAnswer = 2;
                
                Debug.Log("Selected Answer 2");
                break;
        }
    }

    public int AnswerSelected()
    {
        textHolder.DOFade(0f, 1.5f)
            .OnComplete(() =>
            {
                answer1TMP.DOFade(1f, 0f);
                answer2TMP.DOFade(1f, 0f);
            });
        
        int answer = selectedAnswer;
        selectedAnswer = 0;
        readyForInteraction = false;
        tricksterQuestion = false;
        
        return answer;
    }
    
    #endregion
    
    #region |---------- SCAN COUNTER ----------|

    public void ShowScanCounter(Chapter chapter, bool update = true, int scanCount = 0)
    {
        if (!showScanCounter) return;
        
        string scanCountText = scanCount.ToString();
        string countMax = GlobalProgress.GetStorypointMax(chapter).ToString();
        
        if (update)
        {
            scan2TMP.text = $"{scanCountText} / {countMax}";
        }

        else
        {
            string chapterName = GlobalProgress.chapterNames[(int)chapter];
            
            scan1TMP.text = chapterName;
            scan2TMP.text = $"{scanCountText} / {countMax}";
            scanTextHolder.DOFade(1f, 2f);
            
            scanCountActive = true;
        }
    }

    public void HideScanCounter(float fadeTime = 2f)
    {
        if (!showScanCounter) return;

        scanTextHolder.DOFade(0f, fadeTime);
     
    }
    
    #endregion

    #region |---------- INTERACTION ----------|
    
    //Interaction Tracking
    private bool readyForInteraction;
    private bool submitButtonPressed;
    
    private void OnEnable()
    {
        InteractionManager.SubmitButtonPressed += PressSubmitButton;
        InteractionManager.NavigationInput += NavigationInputed;
    }

    private void OnDisable()
    {
        InteractionManager.SubmitButtonPressed -= PressSubmitButton;
        InteractionManager.NavigationInput -= NavigationInputed;
    }
    
    void PressSubmitButton()
    {
        if(!readyForInteraction) return;
        
        submitButtonPressed = true;
    }

    void NavigationInputed(int direction)
    {
        if(!readyForInteraction) return;
        
        if (direction > 0) CheckAnswer(2);
        else if (direction < 0) CheckAnswer(1);
    }

    #endregion

}
