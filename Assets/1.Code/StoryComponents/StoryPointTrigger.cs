using UnityEngine;

public class StoryPointTrigger : MonoBehaviour
{
    private StoryPoint _storyPoint;
    
    void Start()
    {
        // Find Parent which is the actual StoryObject
       GameObject storyObjectGO = transform.parent.gameObject;
       _storyPoint = storyObjectGO.GetComponent<StoryPoint>();
    }
    
    public void OnInteract()
    {
        _storyPoint.OnInteract();
    }
    
    public void OnHoverEnter()
    {
        _storyPoint.OnHoverEnter();
    }
    
    public void OnHoverExit()
    {
        _storyPoint.OnHoverExit();
    }
}

