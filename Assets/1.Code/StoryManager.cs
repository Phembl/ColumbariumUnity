using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

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

    [Header("UI References")]
    [Tooltip("Panel that darkens the screen during story moments")]
    [SerializeField] private Image blackScreenPanel;
    [Tooltip("Text object that displays the story text")]
    [SerializeField] private TextMeshProUGUI storyText;

    [Header("Player References")]
    [Tooltip("Reference to the player controller")]
    [SerializeField] private MultiModePlayerController playerController;
    [Tooltip("Reference to the player GameObject")]
    public GameObject player;
    
    [Header("Transition Settings")]
    [Tooltip("Duration of screen fade transitions")]
    [SerializeField] private float fadeScreenDuration = 0.8f;
    [Tooltip("Opacity of the black screen (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float blackScreenOpacity = 0.9f;

    [Header("Audio")]
    [Tooltip("Audio source for playing story narration")]
    [SerializeField] private AudioSource audioSource;
    
    // Enum to select the starting chapter
    public enum Chapter
    {
        PROLOG,
        NICHTS,
        DER_PARADISE_GARTEN,
        DER_TAUBENSCHLAG,
        BECOMING_PIGEON,
        BE_A_TRICKSTER_BUILD_A_WORLD,
        EMBRYO,
        FAREWELL
    }
    
    [Header("Story Setup")]
    [Tooltip("Select which chapter to start from")]
    public Chapter startingChapter = Chapter.NICHTS;
    

    [Header("Chapter References")]
    [Tooltip("GameObject containing Prolog content")]
    public GameObject prolog;
    
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
    
    // Specific Story Variables
    [Header("Specific Story Elements")]
    [SerializeField] private TextMeshPro taubenschlagDoorText;
    [SerializeField] private GameObject chapter3Blocker;
    
    [SerializeField] private int gardenStoryCount = 5;
    [SerializeField] private int taubenschlagStoryCount = 7;
    [SerializeField] private int pidgeonStoryCount = 6;
    [SerializeField] private int tricksterStoryCount = 4;
    
    // Internal references
    private GameObject[] chapters;
    private int internalChapterProgress;

    // Internal state tracking
    private bool isStoryPlaying = false;
    private Coroutine currentStoryCoroutine;
    private string storyContent;
    private AudioClip storyAudio;
    private GameObject storyWorldText;
    private float storyVolume;
    private int currentChapter;
    
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

        // Make sure UI elements are initialized correctly
        if (blackScreenPanel != null)
        {
            // Initialize black screen to be invisible
            Color initialColor = blackScreenPanel.color;
            initialColor.a = 0f;
            blackScreenPanel.color = initialColor;
            blackScreenPanel.gameObject.SetActive(false);
        }

        if (storyText != null)
        {
            // Initialize story text to be invisible
            Color initialColor = storyText.color;
            initialColor.a = 0f;
            storyText.color = initialColor;
            storyText.gameObject.SetActive(false);
        }
        
        // Initialize array of chapter GameObjects for easier handling
        chapters = new GameObject[]
        {
            prolog,
            chapter1,
            chapter2,
            chapter3,
            chapter4,
            chapter5,
            chapter6,
            chapter7
        };
    }
    
    private void Start()
    {
        // Initialize the game with the selected chapter
        InitializeGame();
    }
    
    /// <summary>
    /// Initializes the game by activating the selected chapter and positioning the player
    /// </summary>
    private void InitializeGame()
    {
        // Get the index of the selected chapter
        int selectedChapterIndex = (int)startingChapter;
        
        // Activate only the selected chapter, deactivate all others
        for (int i = 0; i < chapters.Length; i++)
        {
            if (chapters[i] != null)
            {
                chapters[i].SetActive(i == selectedChapterIndex);
            }
        }
        
        // Get the active chapter
        GameObject activeChapter = chapters[selectedChapterIndex];
        
        // Find the PlayerStart object in the active chapter
        Transform playerStart = activeChapter.transform.Find("PlayerStart");
        
        currentChapter = selectedChapterIndex;
        
        // Set Start Movement Mode
        if (selectedChapterIndex == 4)
        {
            playerController.SetMovementMode(MultiModePlayerController.MovementMode.Bird);
        }
        else if (selectedChapterIndex == 5)
        {
            playerController.SetMovementMode(MultiModePlayerController.MovementMode.Bug);
        }
        else
        {
            playerController.SetMovementMode(MultiModePlayerController.MovementMode.Human);
        }
            
        if (playerStart != null && player != null)
        {
                // Position the player at the PlayerStart position
                player.transform.position = playerStart.position;
                
                // Directly copy the rotation from PlayerStart to player
                player.transform.rotation = playerStart.rotation;

                
                // Lock input briefly to prevent immediate override
                playerController.LockInput();
                // Schedule unlock after a short delay
                StartCoroutine(DelayedUnlock(playerController));
                
        }
        else
        {
                Debug.LogError($"PlayerStart not found in Chapter {selectedChapterIndex + 1} or Player reference is missing");
        }
           
        
    }
       
    /// <summary>
    /// Coroutine to unlock player input after a short delay
    /// </summary>
    private IEnumerator DelayedUnlock(MultiModePlayerController controller)
    {
        // Wait a short time to ensure rotation is applied
        yield return new WaitForSeconds(0.5f);
        
        // Unlock player input
        controller.UnlockInput();
    }
    
    /// <summary>
    /// Public method to check if a story is currently playing
    /// </summary>
    /// <returns>True if a story is playing, false otherwise</returns>
    public bool IsStoryPlaying()
    {
        return isStoryPlaying;
    }

    /// <summary>
    /// Triggers a story moment with the provided text and audio clip
    /// </summary>
    /// <param name="storyContent">The text to display</param>
    /// <param name="audioClip">The audio clip to play</param>
    public void TriggerStoryMoment(string newStoryContent, AudioClip newStoryAudio, GameObject newStoryWorldText)
    {
        // If another story is already playing, stop it
        if (isStoryPlaying)
        {
            if (currentStoryCoroutine != null)
            {
                StopCoroutine(currentStoryCoroutine);
            }
            
            // Reset UI elements
            ResetStoryUI();
        }
        
        // Prepare new story moment
        storyContent = newStoryContent;
        storyAudio = newStoryAudio;

        if (newStoryWorldText != null)
        {
            storyWorldText = newStoryWorldText;
        }
        else
        {
            storyWorldText = null;
        }
        
        
        // Start new story sequence
        currentStoryCoroutine = StartCoroutine(PlayStorySequence());
    }

    /// <summary>
    /// Coroutine that handles the entire story sequence including UI transitions,
    /// audio playback, and player control
    /// </summary>
    private IEnumerator PlayStorySequence()
    {
        isStoryPlaying = true;

        // Disable player input
        playerController.LockInput();

        // Activate UI elements
        blackScreenPanel.gameObject.SetActive(true);
        storyText.gameObject.SetActive(true);
        storyText.text = storyContent;

        storyVolume = audioSource.volume;

        // Fade in black screen
        blackScreenPanel.DOFade(blackScreenOpacity, fadeScreenDuration);
        
        // Fade in text
        storyText.DOFade(1f, fadeScreenDuration);
        
        // Wait for Fade
        yield return new WaitForSeconds(fadeScreenDuration);
        
        // Play Audio
        audioSource.clip = storyAudio;
        audioSource.Play();
        
        // Wait for audio to finish
        yield return new WaitForSeconds(storyAudio.length);
        
        bool specialbreak = CheckSpecialStoryMoment();
        if (!specialbreak) FinalizeStoryMoment(); // Finalizes Story if there is no special story part

    }
    

    /// <summary>
    /// Force stops the current story moment with smooth transitions (for skipping)
    /// </summary>
    public void StopStoryMoment()
    {
        if (isStoryPlaying && currentStoryCoroutine != null)
        {
            // Stop the main story coroutine
            StopCoroutine(currentStoryCoroutine);
            
            // Duration for audio fade out
            float audioFadeDuration = 0.8f;
        
            // Remember original audio volume for later restoration
            float originalVolume = audioSource.volume;
        
            // Fade out Audio
            audioSource.DOFade(0f, audioFadeDuration)
                .OnComplete(() =>
                {

                    bool specialbreak = CheckSpecialStoryMoment();
                    if (!specialbreak) FinalizeStoryMoment(); // Finalizes Story if there is no special story part
                    
                });
        }
    }

    private void FinalizeStoryMoment()
    {
        // Fade out text
        storyText.DOFade(0f, fadeScreenDuration);

        // Fade out black screen
        blackScreenPanel.DOFade(blackScreenOpacity, fadeScreenDuration)
            .OnComplete(() => {
                
                // Deactivate UI elements
                blackScreenPanel.gameObject.SetActive(false);
                storyText.gameObject.SetActive(false);
                
                // Check WorldText
                if (storyWorldText != null)
                {
                    storyWorldText.SetActive(true);
                }
                
                // Re-enable player input
                playerController.UnlockInput();
                
                // Reset UI elements
                ResetStoryUI();

                audioSource.volume = storyVolume;
                
                isStoryPlaying = false;
            });
    }

    private bool CheckSpecialStoryMoment()
    {
        if (currentChapter == 3 && internalChapterProgress == taubenschlagStoryCount)
        {
            internalChapterProgress = 0;
            currentChapter = 4;
            
            // Fade out text
            storyText.DOFade(0f, fadeScreenDuration);
            blackScreenPanel.DOFade(1f, 1f)
                .OnComplete(() => {
                                
                chapters[3].SetActive(false);
                chapters[4].SetActive(true);
                
                playerController.SetMovementMode(MultiModePlayerController.MovementMode.Bird);
                Transform playerStart = chapters[4].transform.Find("PlayerStart");
                // Position the player at the PlayerStart position
                player.transform.position = playerStart.position;
                
                // Directly copy the rotation from PlayerStart to player
                player.transform.rotation = playerStart.rotation;

                
                // Lock input briefly to prevent immediate override
                playerController.LockInput();
                // Schedule unlock after a short delay
                StartCoroutine(DelayedUnlock(playerController));
                
                FinalizeStoryMoment();
                

            });
            
            return true;
        }
        if (currentChapter == 4 && internalChapterProgress == pidgeonStoryCount)
        {
            internalChapterProgress = 0;
            currentChapter = 5;
            
            // Fade out text
            storyText.DOFade(0f, fadeScreenDuration);
            blackScreenPanel.DOFade(1f, 1f)
                .OnComplete(() => {
                                
                    chapters[4].SetActive(false);
                    chapters[5].SetActive(true);
                    
                    playerController.SetMovementMode(MultiModePlayerController.MovementMode.Bug);
                    Transform playerStart = chapters[5].transform.Find("PlayerStart");
                    // Position the player at the PlayerStart position
                    player.transform.position = playerStart.position;
                
                    // Directly copy the rotation from PlayerStart to player
                    player.transform.rotation = playerStart.rotation;

                
                    // Lock input briefly to prevent immediate override
                    playerController.LockInput();
                    // Schedule unlock after a short delay
                    StartCoroutine(DelayedUnlock(playerController));
                
                    FinalizeStoryMoment();
                });
            
            return true;
        }
            
        
        return false;
    }
    
    /// <summary>
    /// Resets all UI elements to their default state
    /// </summary>
    private void ResetStoryUI()
    {
        // Reset black screen
        Color screenColor = blackScreenPanel.color;
        screenColor.a = 0f;
        blackScreenPanel.color = screenColor;
        blackScreenPanel.gameObject.SetActive(false);

        // Reset text
        Color textColor = storyText.color;
        textColor.a = 0f;
        storyText.color = textColor;
        storyText.gameObject.SetActive(false);

        // Stop audio if playing
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    public void ContinueStory(int storyID)
    {
        switch (storyID)
        {
            case 0:
                chapters[1].SetActive(false);
                chapters[2].SetActive(true);
                currentChapter = 2;
                break;
            case 1:
                if (internalChapterProgress < gardenStoryCount)
                {
                    internalChapterProgress++;
                    taubenschlagDoorText.text = $"{gardenStoryCount - internalChapterProgress} Scans fehlen.";
                }
                else if (internalChapterProgress == gardenStoryCount)
                {
                    Debug.Log("Taubenschlag unlocked");
                    chapter3Blocker.SetActive(false);
                    internalChapterProgress = 0;
                    taubenschlagDoorText.text = "";
                }    
                
                break;
            case 2:
                {
                    // Inside Taubenschlag
                    chapters[2].SetActive(false);
                    chapters[3].SetActive(true);
                    currentChapter = 3;
                    break;
                }
            case 3:
                {
                    if (internalChapterProgress < taubenschlagStoryCount) internalChapterProgress++;
                    break;
                }
            case 4:
                {
                    if (internalChapterProgress < pidgeonStoryCount) internalChapterProgress++;
                    break;
                }
            case 5:
            {
                if (internalChapterProgress < tricksterStoryCount) internalChapterProgress++;
                break;
            }
        }
    }
}