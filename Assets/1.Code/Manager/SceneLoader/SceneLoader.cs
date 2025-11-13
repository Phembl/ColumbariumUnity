using System.Collections;
using COLUMBARIUM.Global;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class SceneLoader : MonoBehaviour
{
    public abstract GameObject InitChapterOnLoad(Chapter chapter);
    public abstract void ShowStoryPoints(bool gardenSwitch = false); //Shows All StoryPoints in the Scene Chapter
    
    public abstract void SpecificSceneInteraction(Chapter chapter, int internalChapterProgress = 0);
}

