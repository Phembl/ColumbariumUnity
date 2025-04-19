using UnityEngine;

public class StoryTrigger : MonoBehaviour
{
    private StoryObject storyObject;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find Parent which is the actual StoryObject
       GameObject storyObjectGO = transform.parent.gameObject;
       storyObject = storyObjectGO.GetComponent<StoryObject>();
    }
    
    public void OnInteract()
    {
        storyObject.OnInteract();
    }
    
    public void OnHoverEnter()
    {
        storyObject.OnHoverEnter();
    }
    
    public void OnHoverExit()
    {
        storyObject.OnHoverExit();
    }
}

