using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    [SerializeField] private float fadeScreenDuration = 1.5f;
    [SerializeField] private float chapterEndTime = 3f;
    
    [Header("Specific Story Settings")]
    [SerializeField] private int gardenStoryCount = 5;
    [SerializeField] private int taubenschlagStoryCount = 7;
    [SerializeField] private int pidgeonStoryCount = 6;
    [SerializeField] private int tricksterStoryCount = 4;

    [Header("Volume Setup")] public float narrationVolume = 1f;
    [EndTab]
    
    [Tab("References")]
    [Header("UI References")]
    [Tooltip("Panel that darkens the screen during story moments")]
    [SerializeField] private Image blackScreenPanel;
    [Tooltip("Text object that displays the story text")]
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private GameObject textHolder;
    
    [Header("Player References")]
    [SerializeField] private GameObject human;
    [SerializeField] private GameObject bird;
    [SerializeField] private GameObject bug;
    private BasePlayerController playerController;

    [Header("Audio References")] 
    public AudioSource narrationAudioPlayer;
    public GameObject storyAudioPlayer;
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

    // Internal state tracking
    //private bool isStoryPlaying = false;
    private Coroutine currentStoryCoroutine;
    private string storyContent;
    private AudioClip storyAudio;
    private GameObject storyWorldText;
    private float storyVolume;
    private int currentChapter;
    private Coroutine specialCheck = null;
    
    // Save cursor state to restore later
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;

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
        textHolder.GetComponent<CanvasGroup>().DOFade(0f, 0f);
        blackScreenPanel.DOFade(0f, 0f);
        
        
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
    }
    
    private void Start()
    {
        // Get the index of the selected chapter
        int selectedChapterIndex = (int)startingChapter;
        
        SwitchChapter(selectedChapterIndex, true);
        
        // Setup correct Text for missing Scans
        taubenschlagDoorText.text = $"{gardenStoryCount} Scans fehlen.";
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
        
    

    private IEnumerator CheckSpecialStoryMoment()
    {
        
        Debug.Log("Checking for secial Story Moment");
        Debug.Log($"Internal story progess: " + internalChapterProgress);
        Debug.Log($"Current Chapter: " + currentChapter);
        
        // Prolopgue
        if (currentChapter == 0)
        {
            blackScreenPanel.DOFade(1f, 0f);
            yield return new WaitForSeconds(2f);
            
            //Write Text
            storyText.text = "»Over a seven-day stint, we follow a character named LORD God as he creates the heavens, the earth, and everything in between […]. <br><br> He then goes on to form […] a male human. After breathing life into this man’s nostrils, he hovers eastward to Eden, not too far from Ethiopia. <br><br>There, LORD God plants […] a […] colourful garden with birds and bees, flowers and trees. […]«";
            
            storyText.DOFade(0f, 0f);
            textHolder.GetComponent<CanvasGroup>().DOFade(1f, 0f);
            
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
            textHolder.GetComponent<CanvasGroup>().DOFade(0f, 0f);
            
            SwitchChapter(1, true);
            yield return new WaitForSeconds(1.5f);
            blackScreenPanel.DOFade(0f, fadeScreenDuration);
        }
        
        // Switch to Bird Chapter
        if (currentChapter == 3 && internalChapterProgress == taubenschlagStoryCount)
        {
            //Wait for Switch
            yield return new WaitForSeconds(chapterEndTime);
            
            blackScreenPanel.DOFade(1f, fadeScreenDuration);
            yield return new WaitForSeconds(fadeScreenDuration);
            
            //Write Question
            storyText.text = "»Do you think they are individuals, like us?«";
            storyText.DOFade(1f, 0.5f);
            AudioSource.PlayClipAtPoint(pidgeonQuestion, player.transform.position, narrationVolume);
            
            yield return new WaitForSeconds(pidgeonQuestion.length);
            
            storyText.DOFade(0f, 0.5f);
            SwitchChapter(4, true);
            yield return new WaitForSeconds(0.5f);
            
            blackScreenPanel.DOFade(0f, fadeScreenDuration);
            
        
        }
        
        // Switch to Bug Chapter
        if (currentChapter == 4 && internalChapterProgress == pidgeonStoryCount)
        {
            //Wait for Switch
            yield return new WaitForSeconds(chapterEndTime);
            
            blackScreenPanel.DOFade(1f, fadeScreenDuration);
            yield return new WaitForSeconds(fadeScreenDuration);
            
            SwitchChapter(5, true);
            yield return new WaitForSeconds(1f);
            blackScreenPanel.DOFade(0f, fadeScreenDuration);
            
        }

        // Switch to Embryo
        if (currentChapter == 5 && internalChapterProgress == tricksterStoryCount)
        {
            
            //Wait for Switch
            yield return new WaitForSeconds(chapterEndTime);
            
            blackScreenPanel.DOFade(1f, fadeScreenDuration);
            yield return new WaitForSeconds(fadeScreenDuration);
            
            SwitchChapter(6, false);
            yield return new WaitForSeconds(1f);
            blackScreenPanel.DOFade(0f, fadeScreenDuration);
                
        }
        
        // Embryo
        if (currentChapter == 6)
        {
            float videoPlayTime = embryoVideoPlayer.clip.length.ToFloat();
            if (videoPlayTime > 0) Debug.Log(videoPlayTime);
            
            yield return new WaitForSeconds(videoPlayTime - fadeScreenDuration);
            blackScreenPanel.DOFade(1f, fadeScreenDuration);
            yield return new WaitForSeconds(fadeScreenDuration);
            
            // Switch to Farewell
            yield return new WaitForSeconds(3f);
            SwitchChapter(7, false);
            
        }
        
        // Farewell
        if (currentChapter == 7)
        {
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
        }
        
        // Epilog
        if (currentChapter == 8)
        {
            storyText.DOFade(0f, 0f);
            textHolder.GetComponent<CanvasGroup>().DOFade(1f, 0f);
            storyText.text = "»After all, there is only ever a Garden of Eden for as long as there is a gardener who tends to it.«<br><br>– Patricia de Vries (Against Gardening, 2021) –";
            storyText.DOFade(1f, 2f);
            yield return new WaitForSeconds(1f);
            
        }

    }

    
    public void ContinueStory(int storyID)
    {
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
        }
    }
}