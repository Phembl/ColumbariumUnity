using UnityEngine;

public class StoryProgress : MonoBehaviour
{
    
    [Tooltip("The text to display during the story moment")]
    [SerializeField] private int storyID;
    
    // Track if this story has been triggered
    private bool hasBeenTriggered = false;
    private string playerTag = "Player";
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (!hasBeenTriggered)
        {
            
            if (other.CompareTag(playerTag))
            {
                hasBeenTriggered = true;
                SendStoryID();
            }
        }

    }
    
    private void SendStoryID()
    {
        Debug.Log("Sending Story ID: " + storyID);
        StoryManager.Instance.ContinueStory(storyID);
    }
    
}
