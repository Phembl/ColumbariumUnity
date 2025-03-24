using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

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

    [Header("Transition Settings")]
    [Tooltip("Duration of screen fade transitions")]
    [SerializeField] private float fadeScreenDuration = 0.5f;
    [Tooltip("Duration of text fade transitions")]
    [SerializeField] private float fadeTextDuration = 0.8f;
    [Tooltip("Opacity of the black screen (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float blackScreenOpacity = 0.9f;

    [Header("Audio")]
    [Tooltip("Audio source for playing story narration")]
    [SerializeField] private AudioSource audioSource;

    // Internal state tracking
    private bool isStoryPlaying = false;
    private Coroutine currentStoryCoroutine;
    
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
    }
    

    
    private void OnEnable()
    {
        // No actions to enable
    }
    
    private void OnDisable()
    {
        // No actions to disable
    }
    
    private void OnDestroy()
    {
        // No callbacks to remove
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
    public void TriggerStoryMoment(string storyContent, AudioClip audioClip)
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

        // Start new story sequence
        currentStoryCoroutine = StartCoroutine(PlayStorySequence(storyContent, audioClip));
    }

    /// <summary>
    /// Coroutine that handles the entire story sequence including UI transitions,
    /// audio playback, and player control
    /// </summary>
    private IEnumerator PlayStorySequence(string storyContent, AudioClip audioClip)
    {
        isStoryPlaying = true;

        // 1. Disable player input
        DisablePlayerInput();

        // 2. Activate UI elements
        blackScreenPanel.gameObject.SetActive(true);
        storyText.gameObject.SetActive(true);
        storyText.text = storyContent;

        // 3. Fade in black screen
        yield return FadeScreen(0f, blackScreenOpacity);

        // 4. Fade in text
        yield return FadeText(0f, 1f);

        // 5. Play audio
        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();

            // Wait for audio to finish
            yield return new WaitForSeconds(audioClip.length);
        }
        else
        {
            // If no audio, wait for a default reading time based on text length
            float readTime = Mathf.Max(5.0f, storyContent.Length * 0.05f); // Approx 20 chars per second
            yield return new WaitForSeconds(readTime);
        }

        // 6. Fade out text
        yield return FadeText(1f, 0f);

        // 7. Fade out black screen
        yield return FadeScreen(blackScreenOpacity, 0f);

        // 8. Deactivate UI elements
        blackScreenPanel.gameObject.SetActive(false);
        storyText.gameObject.SetActive(false);

        // 9. Re-enable player input
        EnablePlayerInput();

        isStoryPlaying = false;
    }

    /// <summary>
    /// Fades the black screen panel between alpha values
    /// </summary>
    private IEnumerator FadeScreen(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;
        Color panelColor = blackScreenPanel.color;

        while (elapsedTime < fadeScreenDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeScreenDuration;
            
            panelColor.a = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);
            blackScreenPanel.color = panelColor;
            
            yield return null;
        }

        // Ensure we end at the exact target alpha
        panelColor.a = endAlpha;
        blackScreenPanel.color = panelColor;
    }

    /// <summary>
    /// Fades the story text between alpha values
    /// </summary>
    private IEnumerator FadeText(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;
        Color textColor = storyText.color;

        while (elapsedTime < fadeTextDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeTextDuration;
            
            textColor.a = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);
            storyText.color = textColor;
            
            yield return null;
        }

        // Ensure we end at the exact target alpha
        textColor.a = endAlpha;
        storyText.color = textColor;
    }

    /// <summary>
    /// Disables player input to prevent movement during story moments
    /// </summary>
    private void DisablePlayerInput()
    {
        if (playerController != null)
        {
            // Directly lock input in the player controller
            playerController.LockInput();
            
            // Still disable the actions to prevent any input processing
            var playerInput = playerController.GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                var playerActionMap = playerInput.actions.FindActionMap("Player");
                if (playerActionMap != null)
                {
                    // Disable specific actions instead of the whole action map
                    var moveAction = playerActionMap.FindAction("Move");
                    var lookAction = playerActionMap.FindAction("Look");
                    var jumpAction = playerActionMap.FindAction("Jump");
                    
                    if (moveAction != null) moveAction.Disable();
                    if (lookAction != null) lookAction.Disable();
                    if (jumpAction != null) jumpAction.Disable();
                    
                    // Keep other actions enabled
                }
            }
            
            // Freeze the player's rigidbody
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
            // Save cursor state but keep it locked and invisible
            // This maintains immersion during story sequences
            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            
            // Keep cursor locked and invisible during story mode
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Re-enables player input after the story moment has concluded
    /// </summary>
    private void EnablePlayerInput()
    {
        if (playerController != null)
        {
            // Directly unlock input in the player controller
            playerController.UnlockInput();
            
            // Re-enable specific actions that we disabled
            var playerInput = playerController.GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                var playerActionMap = playerInput.actions.FindActionMap("Player");
                if (playerActionMap != null)
                {
                    // Re-enable specific actions
                    var moveAction = playerActionMap.FindAction("Move");
                    var lookAction = playerActionMap.FindAction("Look");
                    var jumpAction = playerActionMap.FindAction("Jump");
                    
                    if (moveAction != null) moveAction.Enable();
                    if (lookAction != null) lookAction.Enable();
                    if (jumpAction != null) jumpAction.Enable();
                }
            }
            
            // Re-enable physics
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
            
            // Restore previous cursor state
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
        }
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

    /// <summary>
    /// Force stops the current story moment with smooth transitions (for skipping)
    /// </summary>
    public void StopStoryMoment()
    {
        if (isStoryPlaying && currentStoryCoroutine != null)
        {
            // Stop the main story coroutine
            StopCoroutine(currentStoryCoroutine);
            
            // Start a new coroutine for smooth exit
            StartCoroutine(SmoothSkipSequence());
        }
    }
    
    /// <summary>
    /// Smoothly skips the current story with proper fade transitions
    /// </summary>
    private IEnumerator SmoothSkipSequence()
    {
        // Duration for audio fade out
        float audioFadeDuration = 0.5f;
        
        // Remember original audio volume for later restoration
        float originalVolume = audioSource.volume;
        
        // If audio is playing, fade it out
        if (isStoryPlaying)
        {
            // Reset story state
            isStoryPlaying = false;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < audioFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / audioFadeDuration;
                
                // Fade audio volume
                audioSource.volume = Mathf.Lerp(originalVolume, 0f, normalizedTime);
                
                yield return null;
            }
            
            // Ensure volume is zero
            audioSource.volume = 0f;
            audioSource.Stop();
            
            // Restore original volume for next time
            audioSource.volume = originalVolume;
        }
        
        // Fade out text (reuse existing method)
        yield return FadeText(storyText.color.a, 0f);
        
        // Fade out black screen (reuse existing method)
        yield return FadeScreen(blackScreenPanel.color.a, 0f);
        
        // Deactivate UI elements
        blackScreenPanel.gameObject.SetActive(false);
        storyText.gameObject.SetActive(false);
        
        // Re-enable player input
        EnablePlayerInput();
        

    }
}