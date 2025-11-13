using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using COLUMBARIUM.Global;
using DG.Tweening;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VInspector.Libs;

public class GameManager : MonoBehaviour
{
    [TitleGroup("References")]
    [Header("Audio")]
    [SerializeField] private AudioListener mainAudioListener;
    
    [TitleGroup("Settings")]
    [Header("Story")]
    [SerializeField] private bool startNewGame;
    [SerializeField, MinValue(0), ToggleGroup("overwriteStorySettings")] private bool overwriteStorySettings;
    [SerializeField, MinValue(0), ToggleGroup("overwriteStorySettings")] private Chapter startChapter;
    [SerializeField, MinValue(0), ToggleGroup("overwriteStorySettings")] private int gardenStoryCount = 5;
    [SerializeField, MinValue(0), ToggleGroup("overwriteStorySettings")] private int taubenschlagStoryCount = 7;
    [SerializeField, MinValue(0), ToggleGroup("overwriteStorySettings")] private int pidgeonStoryCount = 6;
    [SerializeField, MinValue(0), ToggleGroup("overwriteStorySettings")] private int tricksterStoryCount = 4;
    [SerializeField, MinValue(0), ToggleGroup("overwriteStorySettings")] private int altGardenStoryCount = 3;

    [Button(ButtonSizes.Small, ButtonStyle.Box), ToggleGroup("overwriteStorySettings")]
    private void resetStoryPoints()
    {
        #if UNITY_EDITOR
        gardenStoryCount = 5;
        taubenschlagStoryCount = 7;
        pidgeonStoryCount = 6;
        tricksterStoryCount = 4;
        altGardenStoryCount = 3;
        #endif
    }
    
    [Header("General")]
    [SerializeField, Range(1, 5)] private float fadetime = 2f;
    [SerializeField] private bool english;
    public float GetFadeTime() => fadetime;
   


    private string currentSceneName;

    private Scene sceneMain;
    private Scene sceneGarten;
    private Scene sceneTaubenschlag;
    
    public static GameManager instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        //Set Framerate Cap
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 60;
        
        //Prepare Scene Names
        sceneMain = SceneManager.GetSceneByName("Main");
        sceneGarten = SceneManager.GetSceneByName("Garten");
        sceneGarten = SceneManager.GetSceneByName("Taubenschlag");
    }

    void Start()
    {
        UIManager.instance.InitUI();
        InteractionManager.instance.InitInteraction();
        GlobalProgress.english =  english;
        
        StartNewGame();
    }

    private void StartNewGame()
    {
        
        StartCoroutine(UIManager.instance.UseBlackScreen(true));
        
        StoryManager2.instance.InitStoryManager();

        if (startNewGame)
        {
            startChapter = Chapter.STARTSCREEN;
        }

        else
        {
            GlobalProgress.OverrideStorypointCounter(3, gardenStoryCount);
            GlobalProgress.OverrideStorypointCounter(4, altGardenStoryCount);
            GlobalProgress.OverrideStorypointCounter(5, taubenschlagStoryCount);
            GlobalProgress.OverrideStorypointCounter(6, pidgeonStoryCount);
            GlobalProgress.OverrideStorypointCounter(7, tricksterStoryCount);
        }
        
        //Determine Start Scene
        string sceneToStart = "";
        bool loadNewScene = false;
        switch (startChapter)
        {
            case Chapter.NICHTS: 
            case Chapter.GARTEN:
            case Chapter.GARTEN_ALTERNATIVE:
            case Chapter.PIDGEON:
            case Chapter.FAREWELL:
                sceneToStart = "Garten";
                loadNewScene = true;
                break;
            
            case Chapter.TAUBENSCHLAG:
            case Chapter.TRICKSTER:
                sceneToStart = "Taubenschlag";
                loadNewScene = true;
                break;
            
            default:
                sceneToStart = "";
                loadNewScene = false;
                break; 
        }
        
        StartCoroutine(SwitchToScene(startChapter, sceneToStart, loadNewScene));
        
    }
    
    public IEnumerator SwitchToScene(Chapter nextChapter, string sceneName = "", bool loadScene = false)
    {
        UIManager.instance.LoadIcon(true);
        
        //Unloads all open Scenes
        yield return StartCoroutine(SceneUnloader());

        SceneLoader currentSceneloader = null; //Prepare SceneLoader
        GameObject currentPlayerController = null; //Prepare playerController 
        
        //Loads new scene if needed
        if (loadScene)
        {
            mainAudioListener.enabled = false;
            yield return LoadScene(sceneName);
            currentSceneloader = GameObject.Find("SceneLoader").GetComponent<SceneLoader>();
            currentPlayerController = currentSceneloader.InitChapterOnLoad(nextChapter);
        }

        //Else Prep Main Scene
        else
        {
            SceneManager.SetActiveScene(sceneMain);
            mainAudioListener.enabled = true;
        }
        
        //StartChapter
        StoryManager2.instance.StartChapter(nextChapter, true, currentPlayerController, currentSceneloader);
    }

    #region |---------- SCENE LOADER ----------|
    private IEnumerator SceneUnloader()
    {
        Debug.Log("Starting to unload all game scenes...");

        // Create a list to hold the unload operations
        List<AsyncOperation> operations = new List<AsyncOperation>();

        // Check if scene is loaded and add it to unload List
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

           
            if (scene.name == "Main") //Skip Main Scene
            {
                continue; 
            }
            
            if (!scene.isLoaded) //Check if scene is actually loaded
            {
                continue;
            }

            Debug.Log($"Queueing scene for unload: {scene.name}");
            // Start the unload operation and add it to our list
            operations.Add(SceneManager.UnloadSceneAsync(scene));
        }


        if (operations.Count > 0) //Check if any scene is loaded
        {
            // The unloading happens in the background, this loop just waits for them.
            foreach (var operation in operations)
            {
                yield return operation; // Wait until this specific unload is done
            }
        }
        

        // This code will only run AFTER all scenes have been unloaded
        Debug.Log("All game scenes have been unloaded successfully!");
    }
    
    

    IEnumerator LoadScene(string sceneToLoad)
    {
        // --- Start Asynchronous Loading ---
        Debug.Log($"Starting to load scene: {sceneToLoad}");
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);

        // --- Wait for the Scene to Finish Loading ---
        // The 'yield return' pauses the coroutine until the operation is complete.
        yield return loadOperation;

        // --- Post-Load Setup (Optional but recommended) ---
        // This code runs only AFTER the scene is fully loaded.
        Scene newScene = SceneManager.GetSceneByName(sceneToLoad);
        if (newScene.IsValid())
        {
            SceneManager.SetActiveScene(newScene);
            Debug.Log($"Scene '{sceneToLoad}' loaded and set as active.");
        }
        else
        {
            Debug.LogError($"Something went wrong. Scene '{sceneToLoad}' could not be found after loading.");
        }
    }
   
    #endregion
}
