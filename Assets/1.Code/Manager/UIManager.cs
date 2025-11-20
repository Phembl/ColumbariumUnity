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
    [Header("TextHolder")] 
    [SerializeField] private TextHolder[] narrationTextHolder;
    [SerializeField] private TextHolder[] questionsTextHolder;
    [SerializeField] private TextHolder[] answersTextHolder;
    [SerializeField] private TextHolder[] controlsTextHolder;
    
    [Header("Components")]
    [SerializeField] private Image blackScreen;
    [SerializeField] private Image loadIcon;
    [Space]
    [SerializeField] private TextMeshProUGUI narrationTMP;
    [SerializeField] private TextMeshProUGUI questionTMP;
    [SerializeField] private TextMeshProUGUI answer1TMP;
    [SerializeField] private TextMeshProUGUI answer2TMP;
    [SerializeField] private TextMeshProUGUI controlsTMP;
    [SerializeField] private TextMeshProUGUI scan1TMP;
    [SerializeField] private TextMeshProUGUI scan2TMP;
    [SerializeField] private TextMeshProUGUI startTextTMP;
    [Space]
    //[SerializeField] private CanvasGroup textHolder;
    [SerializeField] private CanvasGroup narrationHolder;
    [SerializeField] private CanvasGroup controlsHolder;
    [SerializeField] private CanvasGroup qaHolder;
    [SerializeField] private CanvasGroup creditsHolder;
    [SerializeField] private CanvasGroup scanTextHolder;
    [SerializeField] private CanvasGroup startTextHolder;
 

    //StateTracking
    private bool loadIconActive;
    private bool scanCountActive;
    private int selectedAnswer;
    private bool tricksterQuestion;
    
    //Settings
    private bool english;
    private float controlsDisplayDuration;
    private bool showScanCounter;
    private int creditDuration;
    private float blackScreenFadeTime; 
    
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
        
        HideAllText();
        UpdateSettings();
        
        Debug.Log("UIManager initialized.");
    }

    private void HideAllText()
    {

        DOTween.KillAll();
        
        narrationHolder.alpha = 0f;
        controlsHolder.alpha = 0f;
        qaHolder.alpha = 0f;
        creditsHolder.alpha = 0f;
        scanTextHolder.alpha = 0f;
        startTextHolder.alpha = 0f;
        
        
    }

    private void UpdateSettings()
    {
        showScanCounter = SettingsManager.instance.showScanCounter;
        controlsDisplayDuration =  SettingsManager.instance.controlsDisplayDuration;
        creditDuration = SettingsManager.instance.creditsDuration;
        blackScreenFadeTime = SettingsManager.instance.blackScreenFadeTime;
        
        
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
    
    public IEnumerator UseBlackScreen(bool activate, bool fade = false, bool finishLoading = false)
    {
        if (activate)
        {
            if (fade) blackScreen.DOFade(1f, blackScreenFadeTime);
            else blackScreen.DOFade(1f, 0f);
        }

        else //FadeOut
        {
            if (finishLoading && loadIconActive)
            {
                LoadIcon(false);
            }
            
            if (fade) blackScreen.DOFade(0f, blackScreenFadeTime);
            else blackScreen.DOFade(0f, 0f);
        }
        
        yield return new WaitForSeconds(blackScreenFadeTime + 0.5f);
        
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

    public IEnumerator ShowCredits()
    {
        creditsHolder.alpha = 1f;
        Transform credits = creditsHolder.transform.GetChild(0);

        credits.DOMoveY(-1000f, creditDuration);
        yield return new WaitForSeconds(creditDuration - 3f);
    }

    #region |---------- NARRATION ----------|
    
    public float ShowNarrationText(int narrationIndex)
    {
        float fadeTime = 1.5f;
        narrationTMP.text = narrationTextHolder[0].text[narrationIndex];
        narrationHolder.DOFade(1f, fadeTime);
        return fadeTime;
    }
    public void HideNarrationText()
    {
        narrationHolder.DOFade(0f, 1.5f)
            .OnComplete(() => narrationTMP.text = "");
    }
    
    #endregion
    
    #region |---------- INSTRUCTIONS ----------|
    
    public IEnumerator ShowInstructions(int instructionIndex, bool autohide = false)
    {
        TextHolder currentTextHolder;
       
        if (english) currentTextHolder = controlsTextHolder[1];
        else  currentTextHolder = controlsTextHolder[0];
        
        string controlsText = currentTextHolder.text[instructionIndex];
        
        TMP_Text instructionTMP = null;
        CanvasGroup instructionHolder = null;

        if (instructionIndex == 0 || instructionIndex == 1) //Start/Skip
        {
            instructionTMP = startTextTMP;
            instructionHolder = startTextHolder;
        }
        else //Controls
        {
            instructionTMP = controlsTMP;
            instructionHolder = controlsHolder;
        }
        
        instructionTMP.text = controlsText;
        instructionHolder.DOFade(1f, 1.5f);

        if (autohide)
        {
            yield return new WaitForSeconds(controlsDisplayDuration);
        
            instructionHolder.DOFade(0f, 1.5f);
            yield return new WaitForSeconds(1.5f);
        }
     
    }
    
    public void HideStartText()
    {
        startTextHolder.DOFade(0f, 1.5f);
    }

    
    #endregion

    #region |---------- Q & A ----------|
    
    public float ShowQuestionText(int questionIndex)
    {
        answer1TMP.DOFade(0f, 0f);
        answer2TMP.DOFade(0f, 0f);
        
        float fadeTime = 1.5f;
        questionTMP.text = questionsTextHolder[0].text[questionIndex];
        qaHolder.DOFade(1f, fadeTime);
        return fadeTime;
        
    }
    
    public void ShowAnswerText(Chapter chapter)
    {
        answer1TMP.fontStyle = FontStyles.Underline;
        answer2TMP.fontStyle = FontStyles.Normal;

        TextHolder currentTextHolder = answersTextHolder[0];
        
        switch (chapter)
        {
            case Chapter.TAUBENSCHLAG_QUESTION:
                answer1TMP.text = currentTextHolder.text[0]; //Yes
                answer2TMP.text = currentTextHolder.text[1]; //No
                break;
            
            case Chapter.GARTEN_INV_QUESTION:
                answer1TMP.text = currentTextHolder.text[2]; //I'm not sure
                answer2TMP.text = currentTextHolder.text[0]; //Yes
                break;
            
            case Chapter.PIGEON_QUESTION:
                tricksterQuestion = true;
                //Selects a random answer from the answer pool
                answer1TMP.text = currentTextHolder.text[Random.Range(0, currentTextHolder.text.Length)];
                answer2TMP.text = currentTextHolder.text[Random.Range(0, currentTextHolder.text.Length)];
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
                    answer1TMP.text = answersTextHolder[0].text[Random.Range(0, answersTextHolder[0].text.Length)];
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
                    answer2TMP.text = answersTextHolder[0].text[Random.Range(0, answersTextHolder[0].text.Length)];
                }
                
                selectedAnswer = 2;
                
                Debug.Log("Selected Answer 2");
                break;
        }
    }

    public int AnswerSelected()
    {
        qaHolder.DOFade(0f, 1.5f);
            
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

        if (chapter == Chapter.GARTEN_INVERSE)
        {
            scan1TMP.color = Color.black;
            scan2TMP.color = Color.black;
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
