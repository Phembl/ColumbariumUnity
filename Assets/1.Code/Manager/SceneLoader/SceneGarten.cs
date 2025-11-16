using System.Collections.Generic;
using COLUMBARIUM.Global;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class SceneGarten : SceneLoader
{
    [TitleGroup("References")] 
    [SerializeField] private GameObject[] chapterHolderObjects;
    [SerializeField] private GameObject[] playerController;
    [Header("Special Story Objects")]
    [SerializeField] private StoryPortal gartenPortal;
    [SerializeField] private TextMeshPro gartenPortalText;
    [SerializeField] private GameObject gartenDoorBlocker;
    [SerializeField] private StoryPortal gartenInversePortal;
    [SerializeField] private TextMeshPro gartenInversePortalText;
    [SerializeField] private StoryPortal pigeonPortal;
    [SerializeField] private TextMeshPro pigeonPortalText;
    [Header("Text Files")]
    [SerializeField] private TextFile scanTextFileSingular;
    [SerializeField] private TextFile scanTextFilePlural;
    [Header("Player controller")]
    
    private List<Transform> playerStarts = new List<Transform>();
    private List<ChapterHolder> chapterHolders =  new List<ChapterHolder>();

    private int chapterIndex;
    
    //Text
    private string scanTextSingular = "";
    private string scanTextPlural = "";


    //This is called by the GameManager on SceneLoad
    //This returns the currently active Player
    public override GameObject InitChapterOnLoad(Chapter chapter)
    {
        Debug.Log("SCENE MANAGER: Initializing Garten Chapters");
        
        foreach (var chapterHolderObject in chapterHolderObjects)
        {
            ChapterHolder nextChapterHolder = chapterHolderObject.GetComponent<ChapterHolder>();

            if (nextChapterHolder == null)
            {
                Debug.LogError("SCENE MANAGER: Chapter Holder not found. Aborting Load Process");
                return null;
            }
            
                
            if (nextChapterHolder.storyPoints.Length > 0)
            {
                Debug.Log("Resetting StoryPoints:" + nextChapterHolder.gameObject.name);
                foreach (GameObject storyPoint in nextChapterHolder.storyPoints)
                {
                    storyPoint.GetComponent<StoryPoint>().Reset();
                }
               
            }
            
            chapterHolders.Add(nextChapterHolder);
            playerStarts.Add(nextChapterHolder.playerStart.transform);
            
            if (chapterHolderObject.activeSelf) chapterHolderObject.SetActive(false);
        }
        
        //Language
        if (GlobalProgress.english)
        {
            scanTextSingular = scanTextFileSingular.textEng;
            scanTextPlural = scanTextFilePlural.textEng;
        }
        else
        {
            scanTextSingular = scanTextFileSingular.text;
            scanTextPlural = scanTextFilePlural.text;
        }
        
        //Specific Scene Setup
        int controllerIndex = -1;
        
        switch (chapter)
        {
            case Chapter.NICHTS:
                gartenDoorBlocker.SetActive(true);
                gartenPortal.DisablePortal();
                gartenPortalText.text = $"{GlobalProgress.GetStorypointCounter(Chapter.GARTEN)} {scanTextPlural}";
                
                controllerIndex = 0;
                chapterIndex = 0;
                
                break;
                
            case Chapter.GARTEN:
                gartenDoorBlocker.SetActive(true);
                gartenPortal.DisablePortal();
                gartenPortalText.text = $"{GlobalProgress.GetStorypointCounter(Chapter.GARTEN)} {scanTextPlural}";
                
                controllerIndex = 0;
                chapterIndex = 1;
                break;
            
            case Chapter.GARTEN_INVERSE:
                gartenInversePortal.DisablePortal();
                gartenInversePortalText.text = $"{GlobalProgress.GetStorypointCounter(Chapter.GARTEN)} {scanTextPlural}";
               
                controllerIndex = 0;
                chapterIndex = 2;
                break;
            
            case Chapter.PIGEON:
                pigeonPortal.DisablePortal();
                pigeonPortalText.text = $"{GlobalProgress.GetStorypointCounter(Chapter.GARTEN)} {scanTextPlural}";
               
                controllerIndex = 1;
                chapterIndex = 3;
                break;
            
            case Chapter.FAREWELL:
                controllerIndex = 99;
                chapterIndex = 4;
                break;
        }
        
        // Activate current ChapterHolder
        chapterHolderObjects[chapterIndex].SetActive(true);
        if (chapterIndex == 0) chapterHolderObjects[1].SetActive(true); //When Nichts is loaded it also already activates the Garden Objects (they are still invis)
        
        //Spawn Controller
        if (chapter != Chapter.FAREWELL) 
        {
            GameObject nextController = Instantiate(GameManager.instance.playerController[controllerIndex]);
            nextController.transform.position = playerStarts[chapterIndex].position;
            nextController.transform.rotation = playerStarts[chapterIndex].rotation;
            
            nextController.GetComponent<BasePlayerController>().InitController();
            nextController.GetComponent<BasePlayerController>().LockInput();
            nextController.SetActive(true);
            
            return nextController;
        }

        else //Farewell doesnt need controller
        {
            return null;
        }
        
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
            Debug.Log("SCENE MANAGER: Showing StoryPoints: " + chapterHolders[chapterIndex].gameObject.name);
            
            foreach (GameObject storyPoint in chapterHolders[chapterIndex].storyPoints)
            {
                storyPoint.GetComponent<StoryPoint>().FadeIn();
            }
               
        }
    }

    public override void SpecificSceneInteraction(Chapter chapter, int internalChapterProgress = 0)
    {
        int storyPointCount = 0;
        
        switch (chapter)
        {
            case Chapter.GARTEN:
                storyPointCount = GlobalProgress.GetStorypointCounter(Chapter.GARTEN);
                
                if (internalChapterProgress < storyPointCount) //Chapter not finished
                {
                    
                    if (storyPointCount - internalChapterProgress == 1)
                        gartenPortalText.text = $"{storyPointCount - internalChapterProgress} {scanTextSingular}";
                    
                    else 
                        gartenPortalText.text = $"{storyPointCount - internalChapterProgress} {scanTextPlural}";
                }
                
                else if (internalChapterProgress == storyPointCount) //All necessary story Points found
                {
                    Debug.Log("Garten: All necessary story points scanned. Portal opened.");
                    gartenDoorBlocker.SetActive(false);
                    gartenPortal.EnablePortal();
                    gartenPortalText.text = "";
                }

                break;
            
            case Chapter.GARTEN_INVERSE:
                storyPointCount = GlobalProgress.GetStorypointCounter(Chapter.GARTEN_INVERSE);
                
                if (internalChapterProgress < storyPointCount)
                {
                    if (storyPointCount - internalChapterProgress == 1)
                        gartenInversePortalText.text = $"{storyPointCount - internalChapterProgress} {scanTextSingular}";
                    
                    else 
                        gartenInversePortalText.text = $"{storyPointCount - internalChapterProgress} {scanTextPlural}";
                }
                else if (internalChapterProgress == storyPointCount)
                {
                    Debug.Log("Garten Inverse: All necessary story points scanned. Portal opened.");
                    gartenInversePortal.EnablePortal();
                    gartenInversePortalText.text = "";
                }
                break;
            

            
            case Chapter.PIGEON:
                storyPointCount = GlobalProgress.GetStorypointCounter(Chapter.PIGEON);
                
                if (internalChapterProgress < storyPointCount)
                {
                    if (storyPointCount - internalChapterProgress == 1)
                        pigeonPortalText.text = $"{storyPointCount - internalChapterProgress} {scanTextSingular}";
                    
                    else 
                        pigeonPortalText.text = $"{storyPointCount - internalChapterProgress} {scanTextPlural}";
                }
                else if (internalChapterProgress == storyPointCount)
                {
                    Debug.Log("Pigeon: All necessary story points scanned. Portal opened.");
                    pigeonPortal.EnablePortal();
                    pigeonPortalText.text = "";
                }
                break;
            
            default:
                Debug.LogWarning("Specific scene interaction requested falsely. Chapter: " + chapter);
                break;
            
            
        }
    }
    
}
