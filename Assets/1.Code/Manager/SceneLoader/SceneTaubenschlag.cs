using System.Collections.Generic;
using COLUMBARIUM.Global;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class SceneTaubenschlag : SceneLoader
{
    [TitleGroup("References")] 
    [SerializeField] private GameObject[] chapterHolderObjects;
    [SerializeField] private GameObject[] playerController;
    [Header("Special Story Objects")]
 
    
    private List<Transform> playerStarts = new List<Transform>();
    private List<ChapterHolder> chapterHolders =  new List<ChapterHolder>();

    private int chapterIndex;

    //This is called by the GameManager on SceneLoad
    //This returns the currently active Player
    public override GameObject InitChapterOnLoad(Chapter chapter)
    {
        Debug.Log("Initializing Taubenschlag Chapters");
        
        foreach (var chapterHolderObject in chapterHolderObjects)
        {
            ChapterHolder nextChapterHolder = chapterHolderObject.GetComponent<ChapterHolder>();

            if (nextChapterHolder == null)
            {
                Debug.LogError("Chapter Holder not found. Aborting Load Process");
                return null;
            }
            
                
            if (nextChapterHolder.storyPoints.Length > 0)
            {
                Debug.Log("Resetting StoryPoints");
                foreach (GameObject storyPoint in nextChapterHolder.storyPoints)
                {
                    storyPoint.GetComponent<StoryPoint>().Reset();
                }
               
            }
            
            chapterHolders.Add(nextChapterHolder);
            playerStarts.Add(nextChapterHolder.playerStart.transform);
            
            if (chapterHolderObject.activeSelf) chapterHolderObject.SetActive(false);
        }

        //Player Controller Setup
        Debug.Log("Initializing PlayerController");
        GameObject currentPlayerController = null;
        
        switch (chapter)
        {
            case Chapter.TAUBENSCHLAG:
                currentPlayerController = playerController[0];
                chapterIndex = 0;
                break;
                
            case Chapter.TRICKSTER:
                currentPlayerController = playerController[1];
                chapterIndex = 1;
                break;
           
        }
        

        if (currentPlayerController != null)
        {
            currentPlayerController.transform.position = playerStarts[chapterIndex].position;
            currentPlayerController.transform.rotation = playerStarts[chapterIndex].rotation;
            
            currentPlayerController.GetComponent<BasePlayerController>().InitController();
            currentPlayerController.GetComponent<BasePlayerController>().LockInput();
            currentPlayerController.SetActive(true);
        }
        else 
        {
            Debug.LogError("No player controller found.");
        }
        
        
        // Activate current ChapterHolder
        chapterHolderObjects[chapterIndex].SetActive(true);
        
        return currentPlayerController;
        
    }

    public override void ShowStoryPoints(bool gardenSwitch = false)
    {
        if (gardenSwitch)
        {
            foreach (GameObject storyPoint in chapterHolders[chapterIndex].storyPoints)
            {
                storyPoint.GetComponent<StoryPoint>().FadeOut();
            }

            chapterIndex++; //manually sets new chapterIndex from Nichts to Garden Chapter
        }
        
        if (chapterHolders[chapterIndex].storyPoints.Length > 0)
        {
            Debug.Log("Showing StoryPoints");
            
            foreach (GameObject storyPoint in chapterHolders[chapterIndex].storyPoints)
            {
                storyPoint.GetComponent<StoryPoint>().FadeIn();
            }
               
        }
    }

    public override void SpecificSceneInteraction(Chapter chapter, int internalChapterProgress = 0)
    {
        switch (chapter)
        {
           
        }
    }
    
}
