using UnityEngine;

public class StoryTrigger : MonoBehaviour
{
    
    //[SerializeField] private int storyID;
    
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
                StoryManager2.instance.CheckStoryProgress(true);
                
            }
        }

    }
    
    
}
