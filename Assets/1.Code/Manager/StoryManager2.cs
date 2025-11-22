using System.Collections;
using UnityEngine;
using COLUMBARIUM.Global;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine.Video;

public class StoryManager2 : MonoBehaviour
{
    [TitleGroup("References")]
    [Header("Objects")] 
   // [SerializeField] private GameObject videoPlayerPrefab;
        
    [Header("Video")] 
    [SerializeField] private VideoPlayer cinematicVideoPlayer;
    [SerializeField] private AudioSource cinematicAudioPlayer;
    [Space]
    [SerializeField] private VideoClip startScreenVideo;
    [SerializeField] private VideoClip embryoVideo;
    
    
    //State Tracking
    private Chapter currentChapter;
    private int internalChapterProgress;
    private Coroutine currentChapterCoroutine;

    private GameObject player;
    private BasePlayerController playerController;
    private SceneLoader sceneLoader;
    
    
    public static StoryManager2 instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
    }
    
    
    public void InitStoryManager()
    {
        internalChapterProgress = 0;
    }

    public void StartChapter(Chapter chapter, bool afterSceneLoad = false, GameObject currentPlayer = null, SceneLoader currentSceneLoader = null)
    {
        
        currentChapter = chapter;
        internalChapterProgress = 0;
        
        player = currentPlayer;
        if (player!= null && chapter != Chapter.FAREWELL) 
        {
            playerController = player.GetComponent<BasePlayerController>();
        }
        
        sceneLoader = currentSceneLoader;
        
        Debug.Log("STORYMANAGER: Starting Chapter_" + currentChapter.ToString());

        if (currentChapterCoroutine != null) StopCoroutine(currentChapterCoroutine);
            
        switch (chapter)
        {
            case Chapter.STARTSCREEN:
                currentChapterCoroutine = StartCoroutine(Chapter_StartScreen());
                break;
            
            case Chapter.PROLOG:
                currentChapterCoroutine = StartCoroutine(Chapter_Prologue());
                break;
            
            case Chapter.NICHTS:
                currentChapterCoroutine = StartCoroutine(Chapter_Nichts());
                break;
            
            case Chapter.GARTEN:
                currentChapterCoroutine = StartCoroutine(Chapter_Garten(afterSceneLoad));
                break;
            
            case Chapter.GARTEN_INVERSE:
                currentChapterCoroutine = StartCoroutine(Chapter_Garten_Inverese());
                break;
            
            case Chapter.GARTEN_INV_QUESTION:
                currentChapterCoroutine = StartCoroutine(Chapter_Garten_Inv_Question());
                break;
            
            case Chapter.TAUBENSCHLAG:
                currentChapterCoroutine = StartCoroutine(Chapter_Taubenschlag());
                break;
            
            case Chapter.TAUBENSCHLAG_QUESTION:
                currentChapterCoroutine = StartCoroutine(Chapter_Taubenschlag_Question());
                break;
            
            case Chapter.PIGEON:
                currentChapterCoroutine = StartCoroutine(Chapter_Pigeon());
                break;
            
            case Chapter.PIGEON_QUESTION:
                currentChapterCoroutine = StartCoroutine(Chapter_Pidgeon_Question());
                break;
            
            case Chapter.TRICKSTER:
                currentChapterCoroutine = StartCoroutine(Chapter_Trickster());
                break;
            
            case Chapter.EMBRYO:
                currentChapterCoroutine = StartCoroutine(Chapter_Embryo());
                break;
            
            case Chapter.FAREWELL:
                currentChapterCoroutine = StartCoroutine(Chapter_Farewell());
                break;
            
            case Chapter.EPILOG:
                currentChapterCoroutine = StartCoroutine(Chapter_Epilog());
                break;
            
            case Chapter.CREDITS:
                currentChapterCoroutine = StartCoroutine(Chapter_Credits());
                break;
        }

    }
    
    #region |---------- CHAPTERS ----------|

    IEnumerator Chapter_StartScreen()
    {
        
        MenuManager.instance.MakeMenuNotUsable();
        InteractionManager.instance.ActivateInteraction(true);
        
        cinematicVideoPlayer.clip = startScreenVideo;
        cinematicAudioPlayer.volume = 1f;
        cinematicVideoPlayer.isLooping = true;
        cinematicVideoPlayer.SetTargetAudioSource(0, cinematicAudioPlayer);
        cinematicVideoPlayer.Prepare();
        
        while (!cinematicVideoPlayer.isPrepared)
        {
            yield return null; // Wait for the next frame
        }

        
        cinematicVideoPlayer.Play();
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(UIManager.instance.ShowInstructions(0));
        
        //Activate Start Button
        readyForStartButton = true;
        Debug.Log("StartScreen: Waiting for StartButton");

        //-----> This will actually start the game <-----
        yield return new WaitUntil(() => startButtonPressed);
        
        startButtonPressed = false;
        readyForStartButton = false;

        UIManager.instance.HideStartText();
        cinematicAudioPlayer.DOFade(0f, SettingsManager.instance.blackScreenFadeTime);
        
       
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(true,true));

        yield return new WaitForSeconds(0.5f);
        
        //Clean up
        cinematicVideoPlayer.Pause();
        
        StartChapter(Chapter.PROLOG);
        
    }

    IEnumerator Chapter_Prologue()
    {
        MenuManager.instance.MakeMenuNotUsable();
        UIManager.instance.LoadIcon(false);
        InteractionManager.instance.ActivateInteraction(true);
        readyForSkipButton = true;
        
        yield return new WaitForSeconds(2f);

        UIManager.instance.ShowNarrationText(0); //Show Text
        yield return new WaitForSeconds(0.5f);
        
        float audioLength = AudioManager.instance.PlayNarrationAudio(0); //Play Audio
        yield return new WaitForSeconds(audioLength - 1f);

        UIManager.instance.HideNarrationText();
        yield return new WaitForSeconds(1.6f);
        
        UIManager.instance.ShowNarrationText(1); //Show Text
        yield return new WaitForSeconds(0.5f);
        
        audioLength = AudioManager.instance.PlayNarrationAudio(1); //Play Audio
        yield return new WaitForSeconds(audioLength - 1f);
        
        UIManager.instance.HideNarrationText();
        
        yield return new WaitForSeconds(2f);

        StartCoroutine(GameManager.instance.SwitchToScene(Chapter.NICHTS, "Garten", true));
    }

    IEnumerator Chapter_Nichts()
    {
        
        InteractionManager.instance.ActivateInteraction(false);

        playerController.UnlockInput();
        
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
        MenuManager.instance.MakeMenuUsable();
        
        sceneLoader.ShowStoryPoints();
        
        yield return StartCoroutine(UIManager.instance.ShowInstructions(2, true));
        UIManager.instance.ShowScanCounter(Chapter.NICHTS, false);

    }
    
    IEnumerator Chapter_Garten(bool afterSceneLoad = true)
    {
        
        if (afterSceneLoad)
        {
            InteractionManager.instance.ActivateInteraction(false);
            
            playerController.UnlockInput();
            
            yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
            MenuManager.instance.MakeMenuUsable();
            sceneLoader.ShowStoryPoints();
        }

        else
        {
            sceneLoader.ShowStoryPoints(true);
        }
        
        
        UIManager.instance.ShowScanCounter(Chapter.GARTEN, false);
    }

    IEnumerator Chapter_Garten_Inverese()
    {
        InteractionManager.instance.ActivateInteraction(false);
        
        playerController.UnlockInput();
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
        MenuManager.instance.MakeMenuUsable();
        sceneLoader.ShowStoryPoints(false, true);
        
        UIManager.instance.ShowScanCounter(Chapter.GARTEN_INVERSE, false);
    }
    
    IEnumerator Chapter_Garten_Inv_Question()
    {
        MenuManager.instance.MakeMenuNotUsable();
        UIManager.instance.LoadIcon(false);
        
        yield return new WaitForSeconds(2f);
        
        //Show Question 
        UIManager.instance.ShowQuestionText(1); //Show Text
        yield return new WaitForSeconds(0.5f);
        
        //Show Answers and wait for selection
        yield return StartCoroutine(WaitForAnswer(Chapter.GARTEN_INV_QUESTION));

        int selectedAnswer = UIManager.instance.AnswerSelected();
        yield return new WaitForSeconds(2f);

        switch (selectedAnswer)
        {
            case 1:
                StartCoroutine(GameManager.instance.SwitchToScene(Chapter.PIGEON, "Garten", true));
                break;
            
            case 2:
                StartCoroutine(GameManager.instance.SwitchToScene(Chapter.CREDITS));
                break;
        }
        
    }
    
    IEnumerator Chapter_Taubenschlag()
    {
        
        InteractionManager.instance.ActivateInteraction(false);

        playerController.UnlockInput();
        
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
        MenuManager.instance.MakeMenuUsable();
        
        sceneLoader.ShowStoryPoints();
        UIManager.instance.ShowScanCounter(Chapter.TAUBENSCHLAG, false);
    }
    
    IEnumerator Chapter_Taubenschlag_Question()
    {
        MenuManager.instance.MakeMenuNotUsable();
        InteractionManager.instance.ActivateInteraction(false);
        UIManager.instance.LoadIcon(false);
        
        yield return new WaitForSeconds(2f);
        
        //Show Question 
        UIManager.instance.ShowQuestionText(0); //Show Text
        yield return new WaitForSeconds(0.5f);
        
        float audioLength = AudioManager.instance.PlayNarrationAudio(2); //Play Audio
        yield return new WaitForSeconds(audioLength - 0.5f);
        
        //Show Answers and wait for selection
        yield return StartCoroutine(WaitForAnswer(Chapter.TAUBENSCHLAG_QUESTION));

        int selectedAnswer = UIManager.instance.AnswerSelected();
        yield return new WaitForSeconds(2f);

        switch (selectedAnswer)
        {
            case 1:
                StartCoroutine(GameManager.instance.SwitchToScene(Chapter.PIGEON, "Garten", true));
                break;
            
            case 2:
                StartCoroutine(GameManager.instance.SwitchToScene(Chapter.GARTEN_INVERSE, "Garten", true));
                break;
        }

    }
    
    IEnumerator Chapter_Pigeon()
    {
        InteractionManager.instance.ActivateInteraction(false);
        
        playerController.UnlockInput();
        
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
        MenuManager.instance.MakeMenuUsable();
        sceneLoader.ShowStoryPoints();
        
        yield return StartCoroutine(UIManager.instance.ShowInstructions(3, true));
        UIManager.instance.ShowScanCounter(Chapter.PIGEON, false);
    }
    
    IEnumerator Chapter_Pidgeon_Question()
    {
        MenuManager.instance.MakeMenuNotUsable();
        UIManager.instance.LoadIcon(false);
        
        yield return new WaitForSeconds(2f);
        
        //Show Question 
        UIManager.instance.ShowQuestionText(2); //Show Text
        yield return new WaitForSeconds(0.5f);
        
        float audioLength = AudioManager.instance.PlayNarrationAudio(3); //This is 3 because Garten_Invert has no audio
        yield return new WaitForSeconds(audioLength - 3f);
        
        //Show Answers and wait for selection
        yield return StartCoroutine(WaitForAnswer(Chapter.PIGEON_QUESTION));
        
        int selectedAnswer = UIManager.instance.AnswerSelected();
        yield return new WaitForSeconds(2f);

        //No real selection happening
        StartCoroutine(GameManager.instance.SwitchToScene(Chapter.TRICKSTER, "Taubenschlag", true));
     
        
    }
    
    IEnumerator Chapter_Trickster()
    {
        MenuManager.instance.MakeMenuUsable();
        InteractionManager.instance.ActivateInteraction(false);
        
        playerController.UnlockInput();
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
        sceneLoader.ShowStoryPoints();
        
        UIManager.instance.ShowScanCounter(Chapter.TRICKSTER, false);
    }
    
    IEnumerator Chapter_Embryo()
    {
        MenuManager.instance.MakeMenuNotUsable();
        InteractionManager.instance.ActivateInteraction(false);
        InteractionManager.instance.ActivateInteraction(true);
        readyForSkipButton = true;
        
        cinematicVideoPlayer.clip = embryoVideo;
        cinematicAudioPlayer.volume = 1f;
        cinematicVideoPlayer.isLooping = false;
        cinematicVideoPlayer.SetTargetAudioSource(0, cinematicAudioPlayer);
        cinematicVideoPlayer.Prepare();
        
        while (!cinematicVideoPlayer.isPrepared)
        {
            yield return null; // Wait for the next frame
        }
        
        cinematicVideoPlayer.Play();
        StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
        float waitTime = (float)(cinematicVideoPlayer.clip.length - 3);
        Debug.Log("Video Wait Time: " + waitTime);
        yield return new WaitForSeconds(waitTime);
        
        cinematicAudioPlayer.DOFade(0f, SettingsManager.instance.blackScreenFadeTime);
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(true,true));

        yield return new WaitForSeconds(0.5f);
        
        //Clean up
        cinematicVideoPlayer.Pause();
        StartCoroutine(GameManager.instance.SwitchToScene(Chapter.FAREWELL, "Garten", true));
        
        
    }
    
    IEnumerator Chapter_Farewell()
    {
        MenuManager.instance.MakeMenuNotUsable();
        //UIManager.instance.LoadIcon(false);
        InteractionManager.instance.ActivateInteraction(true);
        readyForSkipButton = true;
        
        StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));

        float targetX = player.transform.position.x + 300f;
        player.transform.DOMoveX(targetX, 120f).SetEase(Ease.InQuad);
        yield return new WaitForSeconds(4f);
        
        float audioLength = AudioManager.instance.PlayNarrationAudio(4); //Play Audio
        yield return new WaitForSeconds(audioLength + 7f);
        
        //audioLength = AudioManager.instance.PlayNarrationAudio(5); //Play Audio
        yield return new WaitForSeconds(audioLength + 30f);
        
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(true,true));
        yield return new WaitForSeconds(1f);
        
        StartCoroutine(GameManager.instance.SwitchToScene(Chapter.EPILOG));
    }
    
    IEnumerator Chapter_Epilog()
    {
        MenuManager.instance.MakeMenuNotUsable();
        UIManager.instance.LoadIcon(false);
        InteractionManager.instance.ActivateInteraction(true);
        readyForSkipButton = true;
        
        yield return new WaitForSeconds(2f);

        UIManager.instance.ShowNarrationText(2); //Show Text
        yield return new WaitForSeconds(0.5f);
        
        float audioLength = AudioManager.instance.PlayNarrationAudio(6); //Play Audio
        yield return new WaitForSeconds(audioLength - 1f);

        UIManager.instance.HideNarrationText();
        yield return new WaitForSeconds(4f);
        
        StartCoroutine(GameManager.instance.SwitchToScene(Chapter.CREDITS));
    }
    
    IEnumerator Chapter_Credits()
    {
        UIManager.instance.LoadIcon(false);
        InteractionManager.instance.ActivateInteraction(true);
        readyForSkipButton = true;
        
        yield return StartCoroutine(UIManager.instance.ShowCredits());
        yield return new WaitForSeconds(1f);
        
        GameManager.instance.Restart();
    }
    
    #endregion
    
    #region |---------- STORY PROGRESS ----------|

    public void CheckStoryProgress(bool isPortal = false) //Send by StoryObjects and StoryPortals
    {
        internalChapterProgress++;

        switch (currentChapter)
        {
            case Chapter.NICHTS:
                if (isPortal) StartChapter(Chapter.GARTEN, false, player, sceneLoader);
                else UIManager.instance.ShowScanCounter(Chapter.NICHTS, true, internalChapterProgress);
                break;
            
            case Chapter.GARTEN:
                
                if (isPortal) //Finish Chapter
                {
                    StartCoroutine(FinishChapter_Garten());
                }

                else // Scan Object
                {
                    UIManager.instance.ShowScanCounter(Chapter.GARTEN, true, internalChapterProgress);
                    sceneLoader.SpecificSceneInteraction(Chapter.GARTEN, internalChapterProgress);
                }
                
                break;
            
            case Chapter.GARTEN_INVERSE:
                
                if (isPortal) //Finish Chapter
                {
                    StartCoroutine(FinishChapter_GartenInverse());
                }

                else // Scan Object
                {
                    UIManager.instance.ShowScanCounter(Chapter.GARTEN_INVERSE, true, internalChapterProgress);
                    sceneLoader.SpecificSceneInteraction(Chapter.GARTEN_INVERSE, internalChapterProgress);
                }
                
                break;
            
            case Chapter.TAUBENSCHLAG:
                
                if (isPortal) //Finish Chapter
                {
                    StartCoroutine(FinishChapter_Taubenschlag());
                }

                else // Scan Object
                {
                    UIManager.instance.ShowScanCounter(Chapter.TAUBENSCHLAG, true, internalChapterProgress);
                    sceneLoader.SpecificSceneInteraction(Chapter.TAUBENSCHLAG, internalChapterProgress);
                }
                
                break;
            
            case Chapter.PIGEON:
                
                if (isPortal) //Finish Chapter
                {
                    StartCoroutine(FinishChapter_Pidgeon());
                }

                else // Scan Object
                {
                    UIManager.instance.ShowScanCounter(Chapter.PIGEON, true, internalChapterProgress);
                    sceneLoader.SpecificSceneInteraction(Chapter.PIGEON, internalChapterProgress);
                }
                
                break;
            
            case Chapter.TRICKSTER:
               
                if (isPortal) //Finish Chapter
                {
                    StartCoroutine(FinishChapter_Trickster());
                }

                else // Scan Object
                {
                    UIManager.instance.ShowScanCounter(Chapter.TRICKSTER, true, internalChapterProgress);
                    sceneLoader.SpecificSceneInteraction(Chapter.TRICKSTER, internalChapterProgress);
                }
                
                break;
            
        }
    }

    IEnumerator FinishChapter_Garten()
    {
        Debug.Log("SCENE MANAGER: Finished Garten Chapter.");
        
        yield return StartCoroutine(EndChapter());
        StartCoroutine(GameManager.instance.SwitchToScene(Chapter.TAUBENSCHLAG, "Taubenschlag", true));
    }
    
    IEnumerator FinishChapter_GartenInverse()
    {
        Debug.Log("SCENE MANAGER: Finished Garten Inverse Chapter.");
        
        yield return StartCoroutine(EndChapter());
        StartCoroutine(GameManager.instance.SwitchToScene(Chapter.GARTEN_INV_QUESTION));
    }
    
    IEnumerator FinishChapter_Taubenschlag()
    {
        Debug.Log("SCENE MANAGER: Finished Taubenschlag Chapter.");
        
        yield return StartCoroutine(EndChapter());
        StartCoroutine(GameManager.instance.SwitchToScene(Chapter.TAUBENSCHLAG_QUESTION));
    }
    
    IEnumerator FinishChapter_Pidgeon()
    {
        Debug.Log("SCENE MANAGER: Finished Pidgeon Chapter.");
        
        yield return StartCoroutine(EndChapter());
        StartCoroutine(GameManager.instance.SwitchToScene(Chapter.PIGEON_QUESTION));
    }

    IEnumerator FinishChapter_Trickster()
    {
        Debug.Log("SCENE MANAGER: Finished Trickster Chapter.");
        
        yield return StartCoroutine(EndChapter());
        StartCoroutine(GameManager.instance.SwitchToScene(Chapter.EMBRYO));
    }
    
    #endregion
    
    #region |---------- INTERACTION EVENTS ----------|
    
    //Interaction Tracking
    private bool readyForStartButton;
    private bool readyForSubmitButton;
    private bool readyForSkipButton;
    
    private bool startButtonPressed;
    private bool submitButtonPressed;

    private int skipTracker;
    
    private void OnEnable()
    {
        InteractionManager.PauseButtonPressed += PressPauseButton;
        InteractionManager.SubmitButtonPressed += PressSubmitButton;
        InteractionManager.StartButtonPressed += PressStartButton;
        InteractionManager.SkipButtonPressed += PressSkipButton;
    }

    private void OnDisable()
    {
        InteractionManager.PauseButtonPressed -= PressPauseButton;
        InteractionManager.SubmitButtonPressed -= PressSubmitButton;
        InteractionManager.StartButtonPressed -= PressStartButton;
        InteractionManager.SkipButtonPressed -= PressSkipButton;
    }
    
    void PressPauseButton()
    {
        //Doesnt do anything here
       
        
    }
    
    void PressSubmitButton()
    {
        if(!readyForSubmitButton) return;
        
        submitButtonPressed = true;
    }

    void PressSkipButton()
    {
        if (!readyForSkipButton)  return;

        if (skipTracker == 0)
        {
            StartCoroutine(UIManager.instance.ShowInstructions(1, true));
            skipTracker++;
            StartCoroutine(ResetSkipTracker());
        }
        else
        {
            readyForSkipButton = false;
            skipTracker = 0;
            SkipChapter(); //This is only active in cinematic chapters
        }
        
        
    }

    IEnumerator ResetSkipTracker()
    {
        yield return new WaitForSeconds(6f);
        skipTracker = 0;
    }

    void PressStartButton()
    {
        if(!readyForStartButton) return;
        
        startButtonPressed = true;
    }

    #endregion
    
    
    #region |---------- HELPER ----------|

    IEnumerator EndChapter(float fadeTime = 2f)
    {
        StartCoroutine(UIManager.instance.UseBlackScreen(true,false, false));
        UIManager.instance.HideScanCounter(fadeTime);
        yield return StartCoroutine(AudioManager.instance.FadeAwayAudio(fadeTime));
        
        yield return new WaitForSeconds(1f);
        
    }

    IEnumerator WaitForAnswer(Chapter chapter)
    {
        //Show Answers
        UIManager.instance.ShowAnswerText(chapter);
        InteractionManager.instance.ActivateInteraction(true);
        readyForSubmitButton = true;
        
        //Wait for Answer selection
        yield return new WaitUntil(() => submitButtonPressed);
        submitButtonPressed = false;
        readyForSubmitButton = false;
        InteractionManager.instance.ActivateInteraction(false);
    }
    
    void SkipChapter()
    {
        EndChapterEarly();

        switch (currentChapter)
        {
            case Chapter.PROLOG:
                Debug.Log("StoryManager: Skip Prolog");
                StartCoroutine(GameManager.instance.SwitchToScene(Chapter.NICHTS, "Garten", true));
                break;
            
            case Chapter.EMBRYO:
                Debug.Log("StoryManager: Skip Embryo");
                StartCoroutine(GameManager.instance.SwitchToScene(Chapter.FAREWELL, "Garten", true));
                break;
            
            case Chapter.FAREWELL:
                Debug.Log("StoryManager: Skip Farewell");
                StartCoroutine(GameManager.instance.SwitchToScene(Chapter.EPILOG));
                break;
            
            case Chapter.EPILOG:
                Debug.Log("StoryManager: Skip Epilog");
                StartCoroutine(GameManager.instance.SwitchToScene(Chapter.CREDITS));
                break;
            
            case Chapter.CREDITS:
                Debug.Log("StoryManager: Skip Credits");
                GameManager.instance.Restart();
                break;
            
            default:
                Debug.Log("CHAPTER INVALID SKIP");
                break;
                
        }
    }

    public void EndChapterEarly()
    {
        StopCoroutine(currentChapterCoroutine);
        AudioManager.instance.StopNarrationAudio();
        cinematicVideoPlayer.Stop();
    }
    
    #endregion
    

   
}
