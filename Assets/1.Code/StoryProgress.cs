using UnityEngine;

public class StoryProgress : MonoBehaviour
{
    
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
        if (StoryManager.Instance != null)
        {
            Debug.Log("Sending Story ID: " + storyID);
            //StoryManager.Instance.ContinueStory(storyID);
            StoryManager.Instance.StoryPointTriggered(transform.gameObject, true);
        }

    }
    
    
    
}
