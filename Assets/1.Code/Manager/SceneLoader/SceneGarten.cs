using System.Collections.Generic;
using COLUMBARIUM.Global;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class SceneGarten : SceneLoader
{
    [TitleGroup("References")] 
    [SerializeField] private GameObject[] chapterHolderObjects;
    [SerializeField] private GameObject[] playerController;
    [Header("Special Story Objects")]
    [SerializeField] private TextMeshPro taubenschlagDoorText;
    [SerializeField] private GameObject taubenschlagBlocker;
    [SerializeField] private TextFile taubenschlagCounterTextSingular;
    [SerializeField] private TextFile taubenschlagCounterTextPlural;
    
    private List<Transform> playerStarts = new List<Transform>();
    private List<ChapterHolder> chapterHolders =  new List<ChapterHolder>();

    private int chapterIndex;

    //This is called by the GameManager on SceneLoad
    //This returns the currently active Player
    public override GameObject InitChapterOnLoad(Chapter chapter)
    {
        Debug.Log("Initializing Garten Chapters");
        
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
            case Chapter.NICHTS:
                currentPlayerController = playerController[0];
                chapterIndex = 0;
                
                break;
                
            case Chapter.GARTEN:
                currentPlayerController = playerController[0];
                chapterIndex = 1;
                break;
            
            case Chapter.GARTEN_ALTERNATIVE:
                currentPlayerController = playerController[0];
                chapterIndex = 2;
                break;
            
            case Chapter.PIDGEON:
                currentPlayerController = playerController[1];
                chapterIndex = 3;
                break;
            
            case Chapter.FAREWELL:
            

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
        if (chapterIndex == 0) chapterHolderObjects[1].SetActive(true); //When Nichts is loaded it also already activates the Garden Objects (they are still invis)
        
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
            case Chapter.GARTEN:
                int gardenStoryCount = GlobalProgress.GetStorypointCounter(3);
                
                if (internalChapterProgress < gardenStoryCount) //Chapter not finished
                {
                    string scanTextSingular = "";
                    string scanTextPlural = "";

                    if (GlobalProgress.english)
                    {
                        scanTextSingular = taubenschlagCounterTextSingular.textEng;
                        scanTextPlural = taubenschlagCounterTextPlural.textEng;
                    }
                    else
                    {
                        scanTextSingular = taubenschlagCounterTextSingular.text;
                        scanTextPlural = taubenschlagCounterTextPlural.text;
                    }
                        
                    if (gardenStoryCount - internalChapterProgress == 1)
                        taubenschlagDoorText.text = $"{gardenStoryCount - internalChapterProgress} {scanTextSingular}";
                    
                    else 
                        taubenschlagDoorText.text = $"{gardenStoryCount - internalChapterProgress} {scanTextPlural}";
                }
                else if (internalChapterProgress == gardenStoryCount) //All necessary story Points found
                {
                    Debug.Log("Taubenschlag unlocked");
                    taubenschlagBlocker.SetActive(false);
                    taubenschlagDoorText.text = "";
                }

                break;
        }
    }
    
}
