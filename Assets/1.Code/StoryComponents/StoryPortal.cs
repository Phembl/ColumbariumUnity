using DG.Tweening;
using UnityEngine;

public class StoryPortal : MonoBehaviour
{
    
    //[SerializeField] private int storyID;
    
    // Track if this story has been triggered
    private bool hasBeenTriggered = false;
    private string playerTag = "Player";

    //Set by StoryManager
    public bool isActive;
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (isActive && !hasBeenTriggered)
        {
            
            if (other.CompareTag(playerTag))
            {
                hasBeenTriggered = true;
                StoryManager2.instance.CheckStoryProgress(true);
                
            }
        }

    }

    public void EnablePortal()
    {
        isActive = true;
        gameObject.GetComponent<SpriteRenderer>().DOFade(1f, 2f);
    }

    public void DisablePortal()
    {
        isActive = false;
        gameObject.GetComponent<SpriteRenderer>().DOFade(0f, 0f);
    }


}
