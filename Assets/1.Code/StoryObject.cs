using DG.Tweening;
using TMPro;
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

    [SerializeField] private bool newMode;

    private GameObject activeModel;
    private GameObject inactiveModel;
    private GameObject worldText;
    private AudioSource narrationAudioSource;
    private TextMeshPro worldTextMesh;
    private Color textColor;

    // Track if this story has been triggered
    private bool hasBeenTriggered = false;

    // We're using trigger colliders only

    private void Start()
    {
        
        // Setup Models
        Transform childTransform = transform.Find("Active");
        activeModel = childTransform?.gameObject;
        if (activeModel != null) activeModel.SetActive(true);
        
        childTransform = transform.Find("Inactive");
        inactiveModel = childTransform?.gameObject;
        if (inactiveModel != null) inactiveModel.SetActive(false);
        
        // Setup Worldtext
        childTransform = transform.Find("Worldtext");
        worldText = childTransform?.gameObject;
        if (worldText != null)
        {
            worldTextMesh = worldText.GetComponent<TextMeshPro>();
            textColor = worldTextMesh.color;
            textColor.a = 0;
            worldTextMesh.color = textColor;
        }
  
         
        childTransform = transform.Find("AudioSource");
        narrationAudioSource  = childTransform?.gameObject.GetComponent<AudioSource>();
        if (narrationAudioSource != null) narrationAudioSource.clip = narrationAudio;
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
        if (hasBeenTriggered)
            return;

        // Mark as triggered
        hasBeenTriggered = true;

        if (newMode)
        {
            narrationAudioSource.Play();
            if (worldText != null)
            {
                worldTextMesh.DOFade(1f, 20f);

            }

        }
        else
        {
            // Send story content to the StoryManager
            StoryManager.Instance.TriggerStoryMoment(storyText, narrationAudio, worldText);
        }


        
        activeModel.SetActive(false);
        inactiveModel.SetActive(true);
        
    }
    

    
}