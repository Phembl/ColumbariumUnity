using UnityEngine;

/// <summary>
/// Represents an object in the world that triggers a story moment when hit by the player.
/// </summary>
public class StoryObject : MonoBehaviour
{
    [Header("Story Content")]
    [Tooltip("The text to display during the story moment")]
    [SerializeField, TextArea(3, 10)] private string storyText;
    
    [Tooltip("The audio clip to play during the story moment")]
    [SerializeField] private AudioClip narrationAudio;

    [Header("Interaction Settings")]
    [Tooltip("Tag of the object that can trigger this story (usually 'Player')")]
    private string playerTag = "Player";

    [Tooltip("Can this story object be triggered multiple times?")]
    [SerializeField] private bool canTriggerMultipleTimes = false;

    [Header("Visual Indicator (Optional)")]
    [Tooltip("Visual effect to show this is interactable (optional)")]
    [SerializeField] private GameObject visualIndicator;
    
    [Tooltip("Should the object be destroyed after being triggered?")]
    [SerializeField] private bool destroyAfterTrigger = false;

    private GameObject activeModel;
    private GameObject inactiveModel;

    // Track if this story has been triggered
    private bool hasBeenTriggered = false;

    // We're using trigger colliders only

    private void Start()
    {
        // Set up the visual indicator if assigned
        if (visualIndicator != null)
        {
            visualIndicator.SetActive(true);
        }

        // Make sure this object has a collider
        Collider objCollider = GetComponent<Collider>();
        if (objCollider == null)
        {
            Debug.LogWarning($"StoryObject '{gameObject.name}' has no collider. Adding a BoxCollider.");
            gameObject.AddComponent<BoxCollider>();
        }

        // Ensure the collider is a trigger
        objCollider = GetComponent<Collider>();
        if (objCollider != null && !objCollider.isTrigger)
        {
            objCollider.isTrigger = true;
        }
        
        Transform childTransform = transform.Find("Active");
        activeModel = childTransform?.gameObject;
        childTransform = transform.Find("Inactive");
        inactiveModel = childTransform?.gameObject;

        if (activeModel == null || inactiveModel == null)
        {
            Debug.LogWarning("No models found.");
        }
        else
        {
            activeModel.SetActive(true);
            inactiveModel.SetActive(false);
        }
            
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            TriggerStoryMoment();
        }
    }

    /// <summary>
    /// Triggers the story moment if conditions are met
    /// </summary>
    private void TriggerStoryMoment()
    {
        // Check if object can be triggered again
        if (hasBeenTriggered && !canTriggerMultipleTimes)
            return;

        // Mark as triggered
        hasBeenTriggered = true;

        // Disable visual indicator if present
        if (visualIndicator != null)
        {
            visualIndicator.SetActive(false);
        }

        // Send story content to the StoryManager
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.TriggerStoryMoment(storyText, narrationAudio);
        }
        else
        {
            Debug.LogError("StoryManager not found in the scene. Make sure it exists before any StoryObjects are triggered.");
        }
        
        activeModel.SetActive(false);
        inactiveModel.SetActive(true);

        // Destroy the object if configured to do so
        if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }
    }

    // Optional: Add a method to update the story content at runtime
    public void UpdateStoryContent(string newText, AudioClip newAudio)
    {
        storyText = newText;
        narrationAudio = newAudio;
    }
}