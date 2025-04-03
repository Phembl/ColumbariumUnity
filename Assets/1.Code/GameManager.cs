using UnityEngine;
using System.Collections;

/// <summary>
/// Manages game chapters and player initialization.
/// Controls which chapter is active and positions the player at the start point.
/// </summary>
public class GameManager : MonoBehaviour
{
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
    
    [Tooltip("Reference to the player GameObject")]
    public GameObject player;
    
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
    
    // Internal references
    private GameObject[] chapters;
    
    private void Awake()
    {
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
        
        // Validate references
        ValidateReferences();
    }
    
    private void Start()
    {
        // Initialize the game with the selected chapter
        InitializeGame();
    }
    
    /// <summary>
    /// Checks if all required references are assigned
    /// </summary>
    private void ValidateReferences()
    {
        if (player == null)
        {
            Debug.LogError("GameManager: Player reference is not assigned!");
        }
        
        bool missingChapter = false;
        for (int i = 0; i < chapters.Length; i++)
        {
            if (chapters[i] == null)
            {
                Debug.LogError($"GameManager: Chapter {i + 1} reference is not assigned!");
                missingChapter = true;
            }
        }
        
        if (missingChapter)
        {
            Debug.LogError("GameManager: Some chapter references are missing. The game may not function correctly.");
        }
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
        
        if (activeChapter != null)
        {
            // Find the PlayerStart object in the active chapter
            Transform playerStart = activeChapter.transform.Find("PlayerStart");
            
            if (playerStart != null && player != null)
            {
                // Position the player at the PlayerStart position
                player.transform.position = playerStart.position;
                
                // Directly copy the rotation from PlayerStart to player
                player.transform.rotation = playerStart.rotation;
                
                // Log rotation values for debugging
                Debug.Log($"Player positioned at Chapter {(selectedChapterIndex == 0 ? "Prolog" : selectedChapterIndex.ToString())} start point");
                Debug.Log($"PlayerStart rotation: {playerStart.rotation.eulerAngles}, Player rotation: {player.transform.rotation.eulerAngles}");
                
                // If player has a controller that might override rotation, try to temporarily disable it
                MultiModePlayerController playerController = player.GetComponent<MultiModePlayerController>();
                if (playerController != null)
                {
                    // Lock input briefly to prevent immediate override
                    playerController.LockInput();
                    
                    // Schedule unlock after a short delay
                    StartCoroutine(DelayedUnlock(playerController));
                }
            }
            else
            {
                Debug.LogError($"PlayerStart not found in Chapter {selectedChapterIndex + 1} or Player reference is missing");
            }
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
    /// Switches to a specific chapter at runtime
    /// </summary>
    /// <param name="chapterIndex">The index of the chapter to switch to (0-7, where 0 is Prolog)</param>
    public void SwitchToChapter(int chapterIndex)
    {
        if (chapterIndex < 0 || chapterIndex >= chapters.Length)
        {
            Debug.LogError($"Invalid chapter index: {chapterIndex}. Valid range is 0-7.");
            return;
        }
        
        // Update the current chapter
        startingChapter = (Chapter)chapterIndex;
        
        // Initialize with the new chapter
        InitializeGame();
    }
}