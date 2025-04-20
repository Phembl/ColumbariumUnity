using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using VInspector;
using VInspector.Libs;

/// <summary>
/// Manages story interactions in the game world.
/// Handles UI transitions, audio playback, and player control during story moments.
/// </summary>
public class StoryManager : MonoBehaviour
{
    // Singleton instance
    private static StoryManager _instance;
    public static StoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("StoryManager instance not found!");
            }
            return _instance;
        }
    }
    
    [Tab("Setup")]
   
    [Header("Story Setup")]
    [Tooltip("Select which chapter to start from")]
    public Chapter startingChapter = Chapter.NICHTS;
    
    [Header("Transition Settings")]
    [Tooltip("Duration of screen fade transitions")]
    [SerializeField] private float fadeScreenDuration = 2f;
    
    [Header("Specific Story Settings")]
    [SerializeField] private int gardenStoryCount = 5;
    [SerializeField] private int taubenschlagStoryCount = 7;
    [SerializeField] private int pidgeonStoryCount = 6;
    [SerializeField] private int tricksterStoryCount = 4;
    [SerializeField] private int altGarenStoryCount = 3;

    [Header("Volume Setup")] 
    public float narrationVolume = 1f;
    public float storyVolume = 1f;
    [EndTab]
    
    [Tab("References")]
    [Header("UI References")]
    [SerializeField] private Image blackScreenPanel;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private CanvasGroup textHolder;
    [SerializeField] private TextMeshProUGUI answerTextField1;
    [SerializeField] private TextMeshProUGUI answerTextField2;
    [SerializeField] private CanvasGroup creditsHolder;
    
    [Header("Player References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private GameObject human;
    [SerializeField] private GameObject bird;
    [SerializeField] private GameObject bug;
    private BasePlayerController playerController;

    [Header("Audio References")] 
    public AudioSource narrationAudioPlayer;
    [SerializeField] private GameObject storyAudioPlayer;
    [SerializeField] private AudioSource TaubenSchlagAtmo;
    [SerializeField] private AudioSource PidgeonFlugAtmo;
    [SerializeField] private AudioClip prologueAudio;
    [SerializeField] private AudioClip pidgeonQuestion;
    [SerializeField] private AudioClip tricksterQuestion;
    [SerializeField] private AudioClip farewellAudio1;
    [SerializeField] private AudioClip farewellAudio2;
    [SerializeField] private AudioClip epilogueAudio;
    
    [Header("Video References")] 
    [SerializeField] private VideoPlayer embryoVideoPlayer;
    
    [Header("Camera References")] 
    [SerializeField] private Camera farewellCam;
    [SerializeField] private GameObject farewellTargetPos;
    
    public enum Chapter
    {
        PROLOG,
        NICHTS,
        DER_PARADISE_GARTEN,
        DER_TAUBENSCHLAG,
        BECOMING_PIGEON,
        BE_A_TRICKSTER_BUILD_A_WORLD,
        EMBRYO,
        FAREWELL,
        EPILOG,
        GARTEN_ALTERNATIVE
    }
    

    [Header("Chapter References")]
    [Tooltip("GameObject containing Prolog content")]
    public GameObject chapter0;
    
    [Tooltip("GameObject containing Chapter 1 content")]
    public GameObject chapter1;
    
    [Tooltip("GameObject containing Chapter 2 content")]
    public GameObject chapter2;
    
    [Tooltip("GameObject containing Chapter 3 content")]
    public GameObject chapter3;
    
    [Tooltip("GameObject containing Chapter 4 content")]
    public GameObject chapter4;
    
    [Tooltip("GameObject containing Chapter 5 content")]
    public GameObject chapter5;
    
    [Tooltip("GameObject containing Chapter 6 content")]
    public GameObject chapter6;
    
    [Tooltip("GameObject containing Chapter 7 content")]
    public GameObject chapter7;
    
    [Tooltip("GameObject containing Chapter 8 content")]
    public GameObject chapter8;
    
    [Tooltip("GameObject containing Chapter 9 content")]
    public GameObject chapter9;
    
    [Header("Specific Story Elements")]
    [SerializeField] private TextMeshPro taubenschlagDoorText;
    [SerializeField] private GameObject chapter3Blocker;
    [EndTab]
    
    // Internal references
    private GameObject[] chapters;
    private int internalChapterProgress;
    private GameObject player;
    private GameObject currentStoryAudioPlayer;

    // Internal state tracking
    private bool isStoryPlaying;
    private Coroutine currentStoryCoroutine;
    private string storyContent;
    private AudioClip storyAudio;
    private GameObject storyWorldText;
    private int currentChapter;
    private Coroutine specialCheck = null;
    
    //Question Answering
    private bool questionActive;
    private int answer = 1;

    
    // Save cursor state to restore later
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    
    // Input Actions
    private InputAction moveAction;
    private InputAction selectAction;
    private Vector2 moveInput;
    private bool navigateProcessed = false;

    private void Awake()
    {
        // Set up singleton
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        // Make sure this object persists between scenes if needed
        DontDestroyOnLoad(gameObject);
        

        // Initialize story text to be invisible
        textHolder.DOFade(0f, 0f);
        blackScreenPanel.DOFade(0f, 0f);
        answerTextField1.DOFade(0f, 0f);
        answerTextField2.DOFade(0f, 0f);
        
        
        // Initialize array of chapter GameObjects for easier handling
        chapters = new GameObject[]
        {
            chapter0,
            chapter1,
            chapter2,
            chapter3,
            chapter4,
            chapter5,
            chapter6,
            chapter7,
            chapter8,
            chapter9
        };
        
        SetupInputActions();
    }
    
   private void SetupInputActions()
    {
        // Initialize the action map
        var playerActionMap = inputActions.FindActionMap("Player");
        
        // Set up common actions
        moveAction = playerActionMap.FindAction("Move");
        selectAction = playerActionMap.FindAction("Interact");
        
        selectAction.performed += ctx => SelectAnswer();
        
    }
   
    protected virtual void OnEnable()
    {
        // Enable input actions
        inputActions.Enable();
    }

    protected virtual void OnDisable()
    {
        // Disable input actions
        inputActions.Disable();
    }
    
    private void Start()
    {
        // Get the index of the selected chapter
        int selectedChapterIndex = (int)startingChapter;
        
        SwitchChapter(selectedChapterIndex, true);
        
        // Setup correct Text for missing Scans
        taubenschlagDoorText.text = $"{gardenStoryCount} Scans fehlen.";
    }

    private void Update()
    {
        // Get input values
        if (questionActive)
        {
            moveInput = moveAction.ReadValue<Vector2>();
            CheckAnswer();
        }
    }
    
    private void SwitchChapter(int chapter, bool newPlayerPosition)
    {
        currentChapter = chapter;
        internalChapterProgress = 0;
        Debug.Log($"Current Chapter: " + chapter);
        
        // Activate only the selected chapter, deactivate all others
        for (int i = 0; i < chapters.Length; i++)
        {
            if (chapters[i] != null)
            {
                chapters[i].SetActive(i == chapter);
            }
        }
        
        // Set Start Movement Mode
        if (chapter == 4)
        {
            SwitchPlayerController(bird);
        }
        else if (chapter == 5)
        {
            SwitchPlayerController(bug);
        }
        else if (chapter == 6 || chapter == 7)
        {
            // Deactivate all controllers
            human.SetActive(false);
            bird.SetActive(false);
            bug.SetActive(false);
            newPlayerPosition = false;
        }
        else
        {
            SwitchPlayerController(human);
        }
        
        if  (newPlayerPosition)
        {
            // Find the PlayerStart object in the active chapter
            Transform playerStart = chapters[chapter].transform.Find("PlayerStart");
            
            // Position the player at the PlayerStart position
            player.transform.position = playerStart.position;
                
            // Directly copy the rotation from PlayerStart to player
            player.transform.rotation = playerStart.rotation;
            
            // Lock input briefly to prevent immediate override
            playerController.LockInput();
            
            // Schedule unlock after a short delay (if not prolog or epilog)
            if (chapter != 0 &&  chapter != 6 && chapter != 7 && chapter != 8) StartCoroutine(DelayedUnlock(playerController));
            
        }

        if (chapter == 0 || chapter == 6 || chapter == 7 || chapter == 8)
        {
            StartCoroutine(CheckSpecialStoryMoment());
        }
            
    }
    
       
    /// <summary>
    /// Coroutine to unlock player input after a short delay
    /// </summary>
    private IEnumerator DelayedUnlock(BasePlayerController controller)
    {
        // Wait a short time to ensure rotation is applied
        yield return new WaitForSeconds(0.5f);
        
        // Unlock player input
        controller.UnlockInput();
    }
    
    private void SwitchPlayerController(GameObject mode)
    {
        human.SetActive(false);
        bird.SetActive(false);
        bug.SetActive(false);
        
        player = mode;
        player.SetActive(true);
        playerController = player.GetComponent<BasePlayerController>();
    }
    
    /// <summary>
    /// Instantiates a prefab with an AudioSource, plays the given clip at the specified position,
    /// and destroys the instance after the clip finishes.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    /// <param name="position">The world space position to play the clip at.</param>
    public void PlayStoryAudio(AudioClip clip, Vector3 position)
    {
        if (isStoryPlaying)
        {
            //FadeOut currently playing story
            currentStoryAudioPlayer.GetComponent<AudioSource>().DOFade(0f, 1f);
        }
        
        isStoryPlaying = true;
        
        currentStoryAudioPlayer = Object.Instantiate(storyAudioPlayer, position, Quaternion.identity);
        AudioSource audioSource = currentStoryAudioPlayer.GetComponent<AudioSource>();

        // Ensure it won't play automatically if the prefab had it checked (belt & braces)
        audioSource.playOnAwake = false;

        // Assign the clip
        audioSource.clip = clip;

        // Apply volume scale if needed (multiplies with the prefab's volume setting)
        audioSource.volume *= storyVolume;

        // --- Play and Destroy ---
        // Play the audio clip
        audioSource.Play();

        // Schedule the GameObject to be destroyed after the clip length
        // Use the clip's length for the delay
        StartCoroutine(StoryIsRunning(clip.length));
        
    }
    
    private IEnumerator StoryIsRunning(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        isStoryPlaying = false;
        Object.Destroy(currentStoryAudioPlayer);
        
    }
    
    // UI Select Answer
    private void SelectAnswer()
    {
        if (!questionActive) return;
        
        questionActive = false;
        
    }

    // UI CheckAnswer
    private void CheckAnswer()
    {
        if (!questionActive) return;

        if (moveInput.magnitude > 0.1 && !navigateProcessed)
        {
            if (moveInput.x > 0)
            {
                if (currentChapter == 4)
                {
                    answerTextField2.text = "»Yes, I do«";
                    answerTextField1.text = "»What?«";
                }
                else
                {
                    answer = 2;
                }

                answerTextField1.fontStyle = FontStyles.Normal;
                answerTextField2.fontStyle = FontStyles.Underline;
                
            }
            else if (moveInput.x < 0)
            {
                if (currentChapter == 4)
                {
                    answerTextField1.text = "»Yes«";
                    answerTextField2.text = "»No«";
                }
                else
                {
                    answer = 1;
                }
                
                answerTextField1.fontStyle = FontStyles.Underline;
                answerTextField2.fontStyle = FontStyles.Normal;
            
            }
            navigateProcessed = true;
        }
        else if (moveInput.magnitude < 0.1)
        {
            navigateProcessed = false;
        }

   
    }
        
    

    private IEnumerator CheckSpecialStoryMoment()
    {
        
        Debug.Log("Checking for secial Story Moment");
        Debug.Log($"Internal story progess: " + internalChapterProgress);
        Debug.Log($"Current Chapter: " + currentChapter);
        
        float waitForAudioEndTime = 0f;
        yield return new WaitForSeconds(0.05f);
        
        if (isStoryPlaying)
        {
            // If an Story episode is still playing in Background, wait until its finished
            waitForAudioEndTime = currentStoryAudioPlayer.GetComponent<AudioSource>().clip.length + 1f;
        }
        
        // Prologe
        switch (currentChapter)
        {
            case 0:
                blackScreenPanel.DOFade(1f, 0f);
                yield return new WaitForSeconds(2f);
            
                //Write Text
                storyText.text = "»Over a seven-day stint, we follow a character named LORD God as he creates the heavens, the earth, and everything in between […]. <br><br> He then goes on to form […] a male human. After breathing life into this man’s nostrils, he hovers eastward to Eden, not too far from Ethiopia. <br><br>There, LORD God plants […] a […] colourful garden with birds and bees, flowers and trees. […]«";
            
                storyText.DOFade(0f, 0f);
                textHolder.DOFade(1f, 0f);
            
                storyText.DOFade(1f, 1.5f);
                yield return new WaitForSeconds(0.5f);
            
                narrationAudioPlayer.PlayOneShot(prologueAudio, narrationVolume);
                yield return new WaitForSeconds(31f);
            
                storyText.DOFade(0f, 1.5f);
                yield return new WaitForSeconds(1.5f);
                storyText.text = "In this heavenly abode of great beauty, he drops the unnamed man, for him ‘to dress it and to keep it’ <br>(Genesis 2:15). <br><br> […] he makes the gardener ‘an help’ from the man’s rib […]. From then on, the gardener is referred to as Adam and accompanied by a female assistant gardener who, we later learn, is called Eve.«<br><br> – Patricia de Vries (Against Gardening, 2021) –";
                storyText.DOFade(1f, 1.5f);
                yield return new WaitForSeconds(28f);
            
                storyText.DOFade(0f, 1.5f);
                yield return new WaitForSeconds(1.5f);
                textHolder.DOFade(0f, 0f);
            
                SwitchChapter(1, true);
                yield return new WaitForSeconds(1.5f);
                blackScreenPanel.DOFade(0f, fadeScreenDuration);
                break;
            
            case 1:
                break;
            
            case 2:
                break;
            
            case 3:
                // Switch to Bird Chapter
                if (internalChapterProgress == taubenschlagStoryCount)
                {
                    //Wait for Switch
                    yield return new WaitForSeconds(waitForAudioEndTime);
            
                    blackScreenPanel.DOFade(1f, fadeScreenDuration);
                    TaubenSchlagAtmo.DOFade(0f, fadeScreenDuration);
                    yield return new WaitForSeconds(fadeScreenDuration);
                    playerController.LockInput();
            
                    //Write Question
                    storyText.text = "»Do you think they are individuals, like us?«";
                    answerTextField1.text = "»Yes«";
                    answerTextField2.text = "»No«";
            
                    storyText.DOFade(0f, 0f);
                    textHolder.DOFade(1f, 0f);
            
                    storyText.DOFade(1f, 1.5f);
                    yield return new WaitForSeconds(1f);
                    narrationAudioPlayer.PlayOneShot(pidgeonQuestion, narrationVolume);
            
                    yield return new WaitForSeconds(pidgeonQuestion.length - 1f);
                    answerTextField1.DOFade(1f, 1.5f);
                    answerTextField2.DOFade(1f, 1.5f);

                    questionActive = true;
                    yield return new WaitUntil(() => !questionActive);
            
                    answerTextField1.DOFade(0f, 1.5f);
                    answerTextField2.DOFade(0f, 1.5f);
                    storyText.DOFade(0f, 1.5f);
                    yield return new WaitForSeconds(1.5f);
            
                    if (answer == 1)
                    {
                        // Go to Pidgeon
                        SwitchChapter(4, true);
                    }
                    else if (answer == 2)
                    {
                        // Go to Garden alternative
                        SwitchChapter(9, true);
                        answer = 1;
                    }
            
                    yield return new WaitForSeconds(1f);
                    textHolder.DOFade(0f, 0f);
                    blackScreenPanel.DOFade(0f, fadeScreenDuration);
                }
                break;
            
            case 4:
                // Switch to Bug Chapter
                if (internalChapterProgress == pidgeonStoryCount)
                {
                    //Wait for Switch
                    yield return new WaitForSeconds(waitForAudioEndTime);
            
                    blackScreenPanel.DOFade(1f, fadeScreenDuration);
                    PidgeonFlugAtmo.DOFade(0f, fadeScreenDuration);
                    yield return new WaitForSeconds(fadeScreenDuration);
                    Destroy(PidgeonFlugAtmo);
                    playerController.LockInput();
                    
                    //Write Question
                    storyText.text = "»Do you know what a trickster is?«";
                    answerTextField1.text = "»Yes«";
                    answerTextField2.text = "»No«";
            
                    storyText.DOFade(0f, 0f);
                    textHolder.DOFade(1f, 0f);
            
                    storyText.DOFade(1f, 1.5f);
                    yield return new WaitForSeconds(1f);
                    narrationAudioPlayer.PlayOneShot(tricksterQuestion, narrationVolume);
            
                    yield return new WaitForSeconds(tricksterQuestion.length - 1f);
                    answerTextField1.DOFade(1f, 1.5f);
                    answerTextField2.DOFade(1f, 1.5f);

                    questionActive = true;
                    yield return new WaitUntil(() => !questionActive);
                    
                    answerTextField1.DOFade(0f, 1.5f);
                    answerTextField2.DOFade(0f, 1.5f);
                    storyText.DOFade(0f, 1.5f);
                    yield return new WaitForSeconds(1.5f);
            
                    SwitchChapter(5, true);
                    yield return new WaitForSeconds(1f);
                    textHolder.DOFade(0f, 0f);
                    blackScreenPanel.DOFade(0f, fadeScreenDuration);
                }
                break;
            
            case 5:
                // Switch to Embryo
                if (internalChapterProgress == tricksterStoryCount)
                {
                    //Wait for Switch
                    yield return new WaitForSeconds(waitForAudioEndTime);
            
                    blackScreenPanel.DOFade(1f, fadeScreenDuration);
                    yield return new WaitForSeconds(fadeScreenDuration);
            
                    SwitchChapter(6, false);
                    yield return new WaitForSeconds(1f);
                    blackScreenPanel.DOFade(0f, fadeScreenDuration);
                }
                break;
            
            case 6:
                //Embryo
                float videoPlayTime = embryoVideoPlayer.clip.length.ToFloat();
                if (videoPlayTime > 0) Debug.Log(videoPlayTime);
            
                yield return new WaitForSeconds(videoPlayTime - fadeScreenDuration);
                blackScreenPanel.DOFade(1f, fadeScreenDuration);
                yield return new WaitForSeconds(fadeScreenDuration);
            
                // Switch to Farewell
                yield return new WaitForSeconds(3f);
                SwitchChapter(7, false);
                break;
            
            case 7:
                // Farewell
                Debug.Log("Starting Farewell");
                blackScreenPanel.DOFade(0f, 0f);
            
                Vector3 camTarget = farewellTargetPos.transform.position;
                float moveDuration = 120f;
            
                farewellCam.transform.DOMove(camTarget, moveDuration).SetEase(Ease.InQuad);
                yield return new WaitForSeconds(3f);
            
                narrationAudioPlayer.PlayOneShot(farewellAudio1, narrationVolume);
                yield return new WaitForSeconds(farewellAudio1.length + 7f);
            
                narrationAudioPlayer.PlayOneShot(farewellAudio2, narrationVolume);
                yield return new WaitForSeconds(farewellAudio2.length + 30f);
            
                blackScreenPanel.DOFade(1f, 4f);
                yield return new WaitForSeconds(7f);
                
                // Switch to Epilog
                SwitchChapter(8, false);
                break;
            
            case 8:
                // Epilog
                storyText.DOFade(0f, 0f);
                textHolder.DOFade(1f, 0f);
                storyText.text = "»After all, there is only ever a Garden of Eden for as long as there is a gardener who tends to it.«<br><br>– Patricia de Vries (Against Gardening, 2021) –";
                storyText.DOFade(1f, 2f);
                yield return new WaitForSeconds(5f);
                storyText.DOFade(0f, 2f);
                yield return new WaitForSeconds(4f);
                StartCoroutine(Credits());
                break;
            
            case 9:
                if (internalChapterProgress == 0)
                {
                    blackScreenPanel.DOFade(1f, 0f);
                    yield return new WaitForSeconds(1f);
                    Debug.Log("Switching from Garden Alt to Pidgeon");
                    SwitchChapter(4, true);
                    yield return new WaitForSeconds(1f);
                    blackScreenPanel.DOFade(0f, fadeScreenDuration);
                }
                else if (internalChapterProgress == altGarenStoryCount)
                {
                    //Wait for Switch
                    yield return new WaitForSeconds(waitForAudioEndTime);
                    blackScreenPanel.DOFade(1f, fadeScreenDuration);
                    yield return new WaitForSeconds(fadeScreenDuration + 2f);
                    StartCoroutine(Credits());
                }
                break;
        }

        specialCheck = null;
    }

    
    public void ContinueStory(int storyID)
    {
        Debug.Log("continue story");
        switch (storyID)
        {
            case 0:
                //Entering Garden From Nichts
                SwitchChapter(2,false);
                break;
            
            case 1:
                // In the Garden
                internalChapterProgress++;
                if (internalChapterProgress < gardenStoryCount)
                {
                    taubenschlagDoorText.text = $"{gardenStoryCount - internalChapterProgress} Scans fehlen.";
                }
                else if (internalChapterProgress == gardenStoryCount)
                {
                    Debug.Log("Taubenschlag unlocked");
                    chapter3Blocker.SetActive(false);
                    taubenschlagDoorText.text = "";
                }    
                break;
            
            case 2:
                    // Entering Taubenschlag
                    SwitchChapter(3,true);
                    break;
                
            case 3:
                
                    // Inside Taubenschlag
                    if (internalChapterProgress < taubenschlagStoryCount) internalChapterProgress++;
                    if (specialCheck == null) specialCheck = StartCoroutine(CheckSpecialStoryMoment());
                    break;
                
            case 4:
                
                    // While Flying
                    if (internalChapterProgress < pidgeonStoryCount) internalChapterProgress++;
                    if (specialCheck == null) specialCheck = StartCoroutine(CheckSpecialStoryMoment());
                    break;
                
            case 5:
            {
                    // While Bug
                    if (internalChapterProgress < tricksterStoryCount) internalChapterProgress++;
                    if (specialCheck == null) specialCheck = StartCoroutine(CheckSpecialStoryMoment());
                    break;
            }
            case 6:
            {
                    // From Garden alt to Pidgeon
                    if (specialCheck == null) specialCheck = StartCoroutine(CheckSpecialStoryMoment());
                    break;
            }
            case 7:
            {
                // Inside Alt Garten
                if (internalChapterProgress < altGarenStoryCount) internalChapterProgress++;
                break;
            }
                
        }
    }

    private IEnumerator Credits()
    {
        blackScreenPanel.DOFade(1f, 0f);
        creditsHolder.DOFade(1f, fadeScreenDuration);
        yield break;
    }
    
}