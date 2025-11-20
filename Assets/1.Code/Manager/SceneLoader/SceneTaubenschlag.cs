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
    [SerializeField] private TextMeshPro taubenschlagPortalText;
    [SerializeField] private StoryPortal taubenschlagPortal;
    [SerializeField] private TextMeshPro tricksterPortalText;
    [SerializeField] private StoryPortal tricksterPortal;
    [SerializeField] private BoxCollider tricksterPortalBlocker;
    [Header("Text Files")]
    [SerializeField] private TextHolder[] scanTextHolders;
 
    
    private List<Transform> playerStarts = new List<Transform>();
    private List<ChapterHolder> chapterHolders =  new List<ChapterHolder>();

    private int chapterIndex;
    
    //Settings
    private bool english;
    
    //Text
    private string scanTextSingular = "";
    private string scanTextPlural = "";

    //This is called by the GameManager on SceneLoad
    //This returns the currently active Player
    public override GameObject InitChapterOnLoad(Chapter chapter)
    {
        Debug.Log("Initializing Taubenschlag Chapters");
        
        UpdateSettings();
        
        //Language
        if (english)
        {
            scanTextSingular = scanTextHolders[1].text[0];
            scanTextPlural = scanTextHolders[1].text[1];
        }
        else
        {
            scanTextSingular = scanTextHolders[0].text[0];
            scanTextPlural = scanTextHolders[0].text[1];
        }
        
        //Register ChapterHolder and Player Starts
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

        
        //Specific Scene Setup
        int controllerIndex = -1;
        switch (chapter)
        {
            case Chapter.TAUBENSCHLAG:
                taubenschlagPortal.DisablePortal();
                taubenschlagPortalText.text = $"{GlobalProgress.GetStorypointCounter(Chapter.TAUBENSCHLAG)} {scanTextPlural}";
                
                controllerIndex = 0;
                chapterIndex = 0;
                break;
                
            case Chapter.TRICKSTER:
                tricksterPortal.DisablePortal();
                tricksterPortalText.text = $"{GlobalProgress.GetStorypointCounter(Chapter.TRICKSTER)} {scanTextPlural}";
                tricksterPortalBlocker.isTrigger = false;
                
                controllerIndex = 2;
                chapterIndex = 1;
                break;
           
        }
        
        // Activate current ChapterHolder
        chapterHolderObjects[chapterIndex].SetActive(true);
        
        Vector3 nextSpawnPos = playerStarts[chapterIndex].position;
        Quaternion nextSpawnRot = playerStarts[chapterIndex].rotation;
            
        return base.SpawnPlayer(controllerIndex, nextSpawnPos, nextSpawnRot, false);
    }
    
    private void UpdateSettings()
    {
        switch (SettingsManager.instance.language)
        {
            case SettingsManager.Language.German:
                english = false;
                break;
            
            case SettingsManager.Language.English:
                english = true;
                break;
        }
    }

    public override void ShowStoryPoints(bool gardenSwitch = false, bool isInverse = false)
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
        int storyPointCount = 0;
        
        switch (chapter)
        {
            case Chapter.TAUBENSCHLAG:
                storyPointCount = GlobalProgress.GetStorypointCounter(Chapter.TAUBENSCHLAG);
                
                if (internalChapterProgress < storyPointCount)
                {
                    if (storyPointCount - internalChapterProgress == 1)
                        taubenschlagPortalText.text = $"{storyPointCount - internalChapterProgress} {scanTextSingular}";
                    
                    else 
                        taubenschlagPortalText.text = $"{storyPointCount - internalChapterProgress} {scanTextPlural}";
                }
                else if (internalChapterProgress == storyPointCount)
                {
                    Debug.Log("Taubenschlag: All necessary story points scanned. Portal opened.");
                    taubenschlagPortal.EnablePortal();
                    taubenschlagPortalText.text = "";
                }
                break;
            
            case Chapter.TRICKSTER:
                storyPointCount = GlobalProgress.GetStorypointCounter(Chapter.TRICKSTER);
                
                if (internalChapterProgress < storyPointCount)
                {
                    if (storyPointCount - internalChapterProgress == 1)
                        tricksterPortalText.text = $"{storyPointCount - internalChapterProgress} {scanTextSingular}";
                    
                    else 
                        tricksterPortalText.text = $"{storyPointCount - internalChapterProgress} {scanTextPlural}";
                }
                else if (internalChapterProgress == storyPointCount)
                {
                    Debug.Log("Trickster: All necessary story points scanned. Portal opened.");
                    tricksterPortal.EnablePortal();
                    tricksterPortalText.text = "";
                    tricksterPortalBlocker.isTrigger = true;
                }
                break;
            
            default:
                Debug.LogWarning("Specific scene interaction requested falsely. Chapter: " + chapter);
                break;
        }
    }
    
}
