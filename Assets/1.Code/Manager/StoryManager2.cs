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
        if (player!= null) playerController = player.GetComponent<BasePlayerController>();
        
        sceneLoader = currentSceneLoader;
        
        Debug.Log("Starting Chapter_" + currentChapter.ToString());

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
            
            case Chapter.GARTEN_ALTERNATIVE:
                currentChapterCoroutine = StartCoroutine(Chapter_Garten_Alternative());
                break;
            
            case Chapter.TAUBENSCHLAG:
                currentChapterCoroutine = StartCoroutine(Chapter_Taubenschlag());
                break;
            
            case Chapter.PIDGEON:
                currentChapterCoroutine = StartCoroutine(Chapter_Pidgeon());
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
        cinematicVideoPlayer.isLooping = true;
        cinematicVideoPlayer.SetTargetAudioSource(0, cinematicAudioPlayer);
        cinematicVideoPlayer.Prepare();
        
        while (!cinematicVideoPlayer.isPrepared)
        {
            yield return null; // Wait for the next frame
        }
        
        cinematicVideoPlayer.Play();
        
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
        
        //Activate Start Button
        readyForStartButton = true;
        Debug.Log("StartScreen: Waiting for StartButton");

        //-----> This will actually start the game <-----
        yield return new WaitUntil(() => startButtonPressed);
        
        startButtonPressed = false;
        readyForStartButton = false;
        
        cinematicAudioPlayer.DOFade(0f, GameManager.instance.GetFadeTime());
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(true,true));

        yield return new WaitForSeconds(0.5f);
        
        //Clean up
        cinematicVideoPlayer.Pause();
        
        StartChapter(Chapter.PROLOG);
        
    }

    IEnumerator Chapter_Prologue()
    {
        StartCoroutine(UIManager.instance.UseBlackScreen(true,false, true));
        
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

        GameManager.instance.SwitchToScene(Chapter.NICHTS, "Garten", true);
    }

    IEnumerator Chapter_Nichts()
    {
        
        MenuManager.instance.MakeMenuUsable();
        InteractionManager.instance.ActivateInteraction(false);

        playerController.UnlockInput();
        
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
        
        sceneLoader.ShowStoryPoints();
        
        yield return StartCoroutine(UIManager.instance.ShowControllerText());

    }
    
    IEnumerator Chapter_Garten(bool afterSceneLoad = true)
    {
        
        if (afterSceneLoad)
        {
            MenuManager.instance.MakeMenuUsable();
            InteractionManager.instance.ActivateInteraction(false);
            
            playerController.UnlockInput();
            yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
            sceneLoader.ShowStoryPoints();
        }

        else
        {
            sceneLoader.ShowStoryPoints(true);
        }
        
        
        UIManager.instance.ShowScanCounter(Chapter.GARTEN, false);
    }

    IEnumerator Chapter_Garten_Alternative()
    {
        yield return new WaitForEndOfFrame();
    }
    
    IEnumerator Chapter_Taubenschlag()
    {
        MenuManager.instance.MakeMenuUsable();
        InteractionManager.instance.ActivateInteraction(false);

        playerController.UnlockInput();
        
        yield return StartCoroutine(UIManager.instance.UseBlackScreen(false,true, true));
        
        sceneLoader.ShowStoryPoints();
        UIManager.instance.ShowScanCounter(Chapter.TAUBENSCHLAG, false);
    }
    
    IEnumerator Chapter_Pidgeon()
    {
        yield return new WaitForEndOfFrame();
    }
    
    IEnumerator Chapter_Trickster()
    {
        yield return new WaitForEndOfFrame();
    }
    
    IEnumerator Chapter_Embryo()
    {
        yield return new WaitForEndOfFrame();
    }
    
    IEnumerator Chapter_Farewell()
    {
        yield return new WaitForEndOfFrame();
    }
    
    IEnumerator Chapter_Epilog()
    {
        yield return new WaitForEndOfFrame();
    }
    
    IEnumerator Chapter_Credits()
    {
        yield return new WaitForEndOfFrame();
    }
    
    #endregion
    
    #region |---------- STORY PROGRESS ----------|

    public void CheckStoryProgress(bool isTrigger = false) //Send by StoryObjects and StoryTrigger
    {
        internalChapterProgress++;

        switch (currentChapter)
        {
            case Chapter.NICHTS:
                if (isTrigger) StartChapter(Chapter.GARTEN, false, player, sceneLoader);
                break;
            
            case Chapter.GARTEN:
                UIManager.instance.ShowScanCounter(Chapter.GARTEN, true, internalChapterProgress);
                sceneLoader.SpecificSceneInteraction(Chapter.GARTEN, internalChapterProgress);

                if (isTrigger)
                {
                    StartCoroutine(UIManager.instance.UseBlackScreen(true,false, false));
                    GameManager.instance.SwitchToScene(Chapter.TAUBENSCHLAG, "Taubenschlag", true);
                }
                break;
            
            case Chapter.GARTEN_ALTERNATIVE:
                UIManager.instance.ShowScanCounter(Chapter.GARTEN_ALTERNATIVE, true, internalChapterProgress);
                break;
            
            case Chapter.TAUBENSCHLAG:
                UIManager.instance.ShowScanCounter(Chapter.TAUBENSCHLAG, true, internalChapterProgress);
                break;
            
            case Chapter.PIDGEON:
                UIManager.instance.ShowScanCounter(Chapter.PIDGEON, true, internalChapterProgress);
                break;
            
            case Chapter.TRICKSTER:
                UIManager.instance.ShowScanCounter(Chapter.TRICKSTER, true, internalChapterProgress);
                break;
            
        }
    }
    
    #endregion
    
    #region |---------- EVENTS ----------|
    
    //Interaction Tracking
    private bool readyForStartButton;
    private bool startButtonPressed;
    
    private void OnEnable()
    {
        InteractionManager.StartButtonPressed += PressStartButton;
    }

    private void OnDisable()
    {
        InteractionManager.StartButtonPressed -= PressStartButton;
    }
    
    void PressStartButton()
    {
        if(!readyForStartButton) return;
        
        startButtonPressed = true;
    }

    #endregion
    
    

   
}
