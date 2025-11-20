using System.Collections;
using COLUMBARIUM.Global;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class SceneLoader : MonoBehaviour
{
    public abstract GameObject InitChapterOnLoad(Chapter chapter);
    public abstract void ShowStoryPoints(bool gardenSwitch = false, bool isInverse = false); //Shows All StoryPoints in the Scene Chapter
    
    public abstract void SpecificSceneInteraction(Chapter chapter, int internalChapterProgress = 0);
    
    private protected GameObject SpawnPlayer(int controllerIndex, Vector3 position, Quaternion rotation, bool invert = false)
    {
        GameObject player = Instantiate(GameManager.instance.playerController[controllerIndex]);
        player.transform.position = position;
        player.transform.rotation = rotation;
           
        player.GetComponent<BasePlayerController>().InitController();
        player.GetComponent<BasePlayerController>().LockInput();
        
        //Get Settings
        float playerMoveSpeed = 0;
        float playerLookSpeed = 0;
        
        //Bird
        float birdRiseSpeed = 0;
        float birdGlideSpeed = 0;
        float birdGravityPull = 0;

        switch (controllerIndex)
        {
            case 0: //Human
                playerMoveSpeed = SettingsManager.instance.humanMoveSpeed;
                playerLookSpeed = SettingsManager.instance.humanLookSensitivity;
                break;
            
            case 1: // Bird
                playerLookSpeed = SettingsManager.instance.birdLookSensitivity;
                birdRiseSpeed = SettingsManager.instance.birdRiseSpeed;
                birdGlideSpeed = SettingsManager.instance.birdGlideSpeed;
                birdGravityPull = SettingsManager.instance.birdGravityPull;
                break;
                
            case 2: //Bug
                playerMoveSpeed = SettingsManager.instance.bugMoveSpeed;
                playerLookSpeed = SettingsManager.instance.bugLookSensitivity;
                break;
                
        }
     
        
        player.GetComponent<BasePlayerController>().UpdateControllerSettings(playerMoveSpeed, playerLookSpeed, birdRiseSpeed, birdGlideSpeed, birdGravityPull);

        if (invert)
        {
            player.transform.GetChild(0).GetComponent<Camera>().backgroundColor = Color.white;
        }
        
        return player;
    }
}

