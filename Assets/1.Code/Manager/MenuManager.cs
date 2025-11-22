using System;
using System.Collections;
using COLUMBARIUM.Global;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using AudioSettings = COLUMBARIUM.Global.AudioSettings;

public class MenuManager : MonoBehaviour
{
    [ReadOnly]
    [SerializeField] private InputActionAsset inputActions;
    [ReadOnly]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GameObject menuHolder;
    [TitleGroup("Menu Pages")]   
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject controlsMenu;
    [SerializeField] private GameObject chapterMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject audioSettingsMenu;
    [SerializeField] private GameObject playerSettingsMenu;
    [SerializeField] private GameObject playerHumanSettingsMenu;
    [SerializeField] private GameObject playerBirdSettingsMenu;
    [SerializeField] private GameObject playerBugSettingsMenu;
    [SerializeField] private GameObject gameSettingsMenu;

    [TitleGroup("Slider Holder")] 
    [SerializeField] private Transform gameSettingsSlider;
    [SerializeField] private Transform audioSettingsSlider;
    [SerializeField] private Transform playerHumanSettingsSlider;
    [SerializeField] private Transform playerBirdSettingsSlider;
    [SerializeField] private Transform playerBugSettingsSlider;
    
        
    // Input Actions
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction pauseAction;
    private InputAction backAction;
    private Vector2 navigationInput;
    private bool navigationProcessing;
    
    // State tracking
    private bool menuIsUsable;
    bool menuIsOpen;
    private int currentMenuPage;
    private int currentSelection;
    private GameObject[] menuPages;
    private GameObject currentSelectionHolder;
    private bool settingsHaveBeenChanged;
    private bool audioSettingsHaveBeenChanged;
    
    //Setting menu
    private bool settingMenuIsOpen;
    private Slider activeSettingsSlider;
    private float activeSliderMod = 1f;
    private int currentSettingMenuID = -1;
    Transform currentSliderHolder;
    Transform currentNumberHolder;
    private readonly float[] gameSettingsSliderMods =  {0.5f, 0.5f, 2f};
    private readonly float[] audioSettingsSliderMods =  {1f, 1f, 1f, 1f};
    private readonly float[] playerHumanSettingsSliderMods =  {0.25f, 0.25f};
    private readonly float[] playerBirdSettingsSliderMods =  {0.5f, 0.5f, 0.25f, 0.25f};
    private readonly float[] playerBugSettingsSliderMods =  {0.05f, 0.1f};
    
    // Events
    public static event Action<bool> PauseGame;

    // Setup
    private float menuFadeTime = 0.5f;


    public static MenuManager instance;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
      
        menuPages = new GameObject[]
        {
            mainMenu,
            controlsMenu,
            chapterMenu,
            settingsMenu,
            audioSettingsMenu,
            playerSettingsMenu,
            playerHumanSettingsMenu,
            playerBirdSettingsMenu,
            playerBugSettingsMenu,
            gameSettingsMenu
            
        };
        
        SetupInputActions();
        ResetMenu();
    }

    private void SetupInputActions()
    {
        Debug.Log("UI Input Actions Setup");
        
        // Initialize the action map
        var playerActionMap = inputActions.FindActionMap("UI");
        if (playerActionMap == null) Debug.Log("UI Input Actions not found");
        
        // Set up common actions
        submitAction = playerActionMap.FindAction("Submit");
        navigateAction = playerActionMap.FindAction("Navigate");
        pauseAction = playerActionMap.FindAction("Pause");
        backAction = playerActionMap.FindAction("Back");
        
        submitAction.performed += ctx => SelectMenuPoint();
        pauseAction.performed += ctx => OpenCloseMenu();
        backAction.performed += ctx => GoPageBack();
        
    }
    
    // Update is called once per frame
    void Update()
    {
        if (!menuIsUsable) return;
        
        if (menuIsOpen)
        {
            navigationInput = navigateAction.ReadValue<Vector2>();
            NavigateMenu();
        }
    }

    public void MakeMenuUsable()
    {
        menuIsUsable = true;
    }
    
    public void MakeMenuNotUsable()
    {
        menuIsUsable = false;
    }
    
    
    //Opens if closed and vice versa
    private void OpenCloseMenu(bool closeInstant = false)
    {
        if (!menuIsUsable) return;
        
        if (menuIsOpen) //Close menu
        {
            settingMenuIsOpen = false;
            
            //Update Settings if changed
            if (settingsHaveBeenChanged || audioSettingsHaveBeenChanged)
            {
                settingsHaveBeenChanged = false;
                audioSettingsHaveBeenChanged = false;
                
                WriteSettingsIntoManager();
            }
            
            Debug.Log("Menu is closing");
            if (closeInstant) //Close instant
            {
                menuIsOpen = false;
                ResetMenu();
                PauseGame?.Invoke(false);
            }
            else
            {
                menuHolder.GetComponent<CanvasGroup>().DOFade(0f, menuFadeTime)
                    .OnComplete(() =>
                    { 
                        menuIsOpen = false;
                        ResetMenu();
                        PauseGame?.Invoke(false);
                    });
            }
         
            
        }
        else //Open Menu
        {
            Debug.Log("Menu is opening");
            StartCoroutine(SwitchMenuPage(0));
            menuHolder.GetComponent<CanvasGroup>().DOFade(1f, menuFadeTime);
            PauseGame?.Invoke(true);
            
        }
    }
    
    private void ResetMenu()
    {
        menuHolder.GetComponent<CanvasGroup>().alpha = 0f;

        foreach (GameObject menuPage in menuPages) // Loops through all pages and makes them invisible
        {
            menuPage.GetComponent<CanvasGroup>().alpha = 0f;
        }

        if (currentSelectionHolder != null)
        {
            ResetSelection();
        }

    }

    #region |---------- SWITCH PAGES ----------|
    
    private IEnumerator SwitchMenuPage(int page, bool closePage = false)
    {
        
        //Checks if a page should also be closed
        if (closePage)
        {
            menuIsOpen = false;
            menuPages[currentMenuPage].GetComponent<CanvasGroup>().DOFade(0f, menuFadeTime).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(menuFadeTime);
            ResetSelection();
        }
        
        currentMenuPage = page;
        Debug.Log("Opening menu: " + menuPages[page].name);
        
        //Finds "pages" object which holds menu points
        currentSelectionHolder = menuPages[page].transform.Find("selection").gameObject;
        if (currentSelectionHolder == null)
        {
            Debug.LogError("Menu selection Holder not found");
            yield return null;
        }
        
        //Fades in new Menu page
        menuPages[page].GetComponent<CanvasGroup>().DOFade(1f, menuFadeTime).OnComplete(() => menuIsOpen = true).SetEase(Ease.OutQuad);
        
        //Select first item
        currentSelectionHolder.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Underline;
        currentSelection = 0;

    }
    
  

    private void ResetSelection()
    {
        //Make all Menu points not underlined
        foreach (Transform child in currentSelectionHolder.transform)
        {
            child.gameObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
        }
        
    }
    
    #endregion


    private void NavigateMenu()
    {
        if (navigationInput.magnitude > 0.1 && !navigationProcessing)
        {
            navigationProcessing = true;
            
            if (navigationInput.y > 0)
            {
                if (currentSelection == 0)
                {
                    currentSelection = currentSelectionHolder.transform.childCount - 1;
                }
                else
                {
                    currentSelection--;
                }
                
                if (settingMenuIsOpen)
                {
                    UpdateCurrentSettingSlider();
                }
            }
            
            else if (navigationInput.y < 0)
            {
                if (currentSelection < currentSelectionHolder.transform.childCount - 1)
                {
                    currentSelection++;
                }
                else
                {
                    currentSelection = 0;
                }

                if (settingMenuIsOpen)
                {
                    UpdateCurrentSettingSlider();
                }

            }
            
            foreach (Transform child in currentSelectionHolder.transform)
            {
                child.gameObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
            }
        
            currentSelectionHolder.transform.GetChild(currentSelection).gameObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Underline;
            

            if (settingMenuIsOpen)
            {
                //Update Slider positions
                if (navigationInput.x > 0)
                {
                    UpdateCurrentSliderValue(true);
                }
                
                else if (navigationInput.x < 0)
                {
                    UpdateCurrentSliderValue(false);
             
                }
            }
            
        }

        else if (navigationInput.magnitude < 0.1)
        {
            navigationProcessing = false;
        }
        
    }

    
    
    void GoPageBack()
    {
        if (!menuIsOpen) return;
        
        Debug.Log("Menu Manager: Going page back");
        switch (currentMenuPage)
        {
            case 0:
                return;
            case 1: // controls -> main
            case 2: // parts -> main
            case 3: // settings -> main
                StartCoroutine(SwitchMenuPage(0, true));
                break;
            case 4: // audioSettings -> settings
            case 5: // playerSettings -> settings
            case 9: // gameSettings -> settings
                settingMenuIsOpen = false;
                currentSettingMenuID = -1;
                StartCoroutine(SwitchMenuPage(3, true));
                break;
            case 6: // playerHumanSettings -> playerSettings
            case 7: // playerBirdSettings -> playerSettings
            case 8: // playerBugSettings -> playerSettings
                StartCoroutine(SwitchMenuPage(5, true));
                break;
                    
        }
    }


    private void SelectMenuPoint()
    {
        if (!menuIsOpen) return;

        // Catch Back button
        if (currentMenuPage != 0 && currentSelection == currentSelectionHolder.transform.childCount - 1)
        {
            GoPageBack();
            return;
        }
            
        switch (currentMenuPage)
        {
            case 0: // In Main menu
                switch (currentSelection)
                {
                    case 0: // -> Controls
                        StartCoroutine(SwitchMenuPage(1, true));
                        break;
                    case 1: // -> Parts
                        StartCoroutine(SwitchMenuPage(2, true));
                        break;
                    
                    case 2: // -> Settings
                        StartCoroutine(SwitchMenuPage(3, true));
                        break;
                    
                    case 3: // -> Credits
                        OpenCloseMenu(); // Close Menu
                        //StoryManager.Instance.ResetGame(true);
                        break;
                    case 4: // -> Restart
                        OpenCloseMenu(true); // Close Menu
                        GameManager.instance.Restart();
                        break;
                    
                }

                break;

            case 1: // In controls menu
                // Should never trigger because only Button is back button
                break;
            
            case 2: // In parts menu
                OpenCloseMenu(true); // Close Menu
                //Load selected scene
                StoryManager2.instance.EndChapterEarly();
                switch (currentSelection)
                {
                    case 0:
                        GameManager.instance.Restart(true, Chapter.PROLOG);
                        break;
                    
                    case 1:
                        GameManager.instance.Restart(true, Chapter.NICHTS);
                        break;
                    
                    case 2:
                        GameManager.instance.Restart(true, Chapter.GARTEN);
                        break;
                    
                    case 3:
                        GameManager.instance.Restart(true, Chapter.TAUBENSCHLAG);
                        break;
                    
                    case 4:
                        GameManager.instance.Restart(true, Chapter.PIGEON);
                        break;
                    
                    case 5:
                        GameManager.instance.Restart(true, Chapter.TRICKSTER);
                        break;
                    
                    case 6:
                        GameManager.instance.Restart(true, Chapter.EMBRYO);
                        break;
                    
                    case 7:
                        GameManager.instance.Restart(true, Chapter.FAREWELL);
                        break;
                    
                    case 8:
                        GameManager.instance.Restart(true, Chapter.EPILOG);
                        break;
                    
                    case 9:
                        GameManager.instance.Restart(true, Chapter.GARTEN_INVERSE);
                        break;
                }
                 
                break;
            
            case 3: // In Settings menu
                switch (currentSelection)
                {
                    case 0: // -> GameSettings
                        PrepareSettingsMenu(0);
                        StartCoroutine(SwitchMenuPage(9, true));
                        break;
                    case 1: // -> AudioSettings
                        PrepareSettingsMenu(1);
                        StartCoroutine(SwitchMenuPage(4, true));
                        break;
                    
                    case 2: // -> PlayerSettings
                        StartCoroutine(SwitchMenuPage(5, true));
                        break;

                }
                break;
            
            case 4: // Audio Settings
                break;
            
            case 5: // Player Settings
                switch (currentSelection)
                {
                    case 0: // -> PlayerHumanSettings
                        PrepareSettingsMenu(2);
                        StartCoroutine(SwitchMenuPage(6, true));
                        break;
                    case 1: // -> PlayerBirdSettings
                        PrepareSettingsMenu(3);
                        StartCoroutine(SwitchMenuPage(7, true));
                        break;
                    case 2: // -> PlayerBugSettings
                        PrepareSettingsMenu(4);
                        StartCoroutine(SwitchMenuPage(8, true));
                        break;

                }
                break;
            
            case 6: // Player Human settings
                break;
            
            case 7: // Player Bird settings
                break;
            
            case 8: // Player Bug settings
                break;
            
            case 9: //Game Settings
              
                //This might not be needed because settings don't need to be selected (language, bools?)

                break;
            
        }

    }

    #region |---------- SETTINGS MENUS ----------|

    void PrepareSettingsMenu(int settingID)
    {
        settingMenuIsOpen = true;
        Settings currentSettings = SettingsManager.instance.GetSettings();

        switch (settingID)
        {
            case 0: //Game
                currentSliderHolder = gameSettingsMenu.transform.Find("sliders");
                currentNumberHolder = gameSettingsMenu.transform.Find("numbers");

                currentSliderHolder.GetChild(0).gameObject.GetComponent<Slider>().value =
                    currentSettings._blackScreenFadeTime;
                currentNumberHolder.GetChild(0).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._blackScreenFadeTime.ToString()}]";
                
                currentSliderHolder.GetChild(1).gameObject.GetComponent<Slider>().value =
                    currentSettings._controlsDisplayDuration;
                currentNumberHolder.GetChild(1).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._controlsDisplayDuration.ToString()}]";
                
                currentSliderHolder.GetChild(2).gameObject.GetComponent<Slider>().value =
                    currentSettings._creditsDuration;
                currentNumberHolder.GetChild(2).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._controlsDisplayDuration.ToString()}]";

                
                currentSettingMenuID = 0;
                break;

            case 1: //Audio
                AudioSettings currentAudioSettings = AudioManager.instance.GetAudioSettings();
                
                currentSliderHolder = audioSettingsMenu.transform.Find("sliders");
                currentNumberHolder = audioSettingsMenu.transform.Find("numbers");
                
                currentSliderHolder.GetChild(0).gameObject.GetComponent<Slider>().value =
                    currentAudioSettings._narrationAtt;
                currentNumberHolder.GetChild(0).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentAudioSettings._narrationAtt.ToString()}]";
                
                currentSliderHolder.GetChild(1).gameObject.GetComponent<Slider>().value =
                    currentAudioSettings._storyPointsAtt;
                currentNumberHolder.GetChild(1).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentAudioSettings._storyPointsAtt.ToString()}]";
                
                currentSliderHolder.GetChild(2).gameObject.GetComponent<Slider>().value =
                    currentAudioSettings._cinematicAtt;
                currentNumberHolder.GetChild(2).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentAudioSettings._cinematicAtt.ToString()}]";
                
                currentSliderHolder.GetChild(3).gameObject.GetComponent<Slider>().value =
                    currentAudioSettings._atmosphereAtt;
                currentNumberHolder.GetChild(3).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentAudioSettings._atmosphereAtt.ToString()}]";
                
                currentSettingMenuID = 1;
                break;

            case 2: //Player Human
                currentSliderHolder = playerHumanSettingsMenu.transform.Find("sliders");
                currentNumberHolder = playerHumanSettingsMenu.transform.Find("numbers");
                
                currentSliderHolder.GetChild(0).gameObject.GetComponent<Slider>().value =
                    currentSettings._humanMoveSpeed;
                currentNumberHolder.GetChild(0).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._humanMoveSpeed.ToString()}]";
                
                currentSliderHolder.GetChild(1).gameObject.GetComponent<Slider>().value =
                    currentSettings._humanLookSensitivity;
                currentNumberHolder.GetChild(1).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._humanLookSensitivity.ToString()}]";
                
                currentSettingMenuID = 2;
                break;
            
            case 3: //Player Bird
                currentSliderHolder = playerBirdSettingsMenu.transform.Find("sliders");
                currentNumberHolder = playerBirdSettingsMenu.transform.Find("numbers");
                
                currentSliderHolder.GetChild(0).gameObject.GetComponent<Slider>().value =
                    currentSettings._birdRiseSpeed;
                currentNumberHolder.GetChild(0).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._birdRiseSpeed.ToString()}]";
                
                currentSliderHolder.GetChild(1).gameObject.GetComponent<Slider>().value =
                    currentSettings._birdGlideSpeed;
                currentNumberHolder.GetChild(1).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._birdGlideSpeed.ToString()}]";
                
                currentSliderHolder.GetChild(2).gameObject.GetComponent<Slider>().value =
                    currentSettings._birdGravityPull;
                currentNumberHolder.GetChild(2).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._birdGravityPull.ToString()}]";
                
                currentSliderHolder.GetChild(3).gameObject.GetComponent<Slider>().value =
                    currentSettings._birdLookSensitivity;
                currentNumberHolder.GetChild(3).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._birdLookSensitivity.ToString()}]";
                    
                currentSettingMenuID = 3;
                break;
            
            case 4: //Player Bug
                currentSliderHolder = playerBugSettingsMenu.transform.Find("sliders");
                currentNumberHolder = playerBugSettingsMenu.transform.Find("numbers");
                
                currentSliderHolder.GetChild(0).gameObject.GetComponent<Slider>().value =
                    currentSettings._bugMoveSpeed;
                currentNumberHolder.GetChild(0).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._bugMoveSpeed.ToString()}]";
                
                currentSliderHolder.GetChild(1).gameObject.GetComponent<Slider>().value =
                    currentSettings._bugLookSensitivity;
                currentNumberHolder.GetChild(1).gameObject.GetComponent<TMP_Text>().text =
                    $"[{currentSettings._bugLookSensitivity.ToString()}]";
                
                currentSettingMenuID = 4;
                break;

            default:
                Debug.Log("Menu Manager: Invalid Settings Menu");
                break;
        }
        
        //Set first slider active
        activeSettingsSlider = currentSliderHolder.GetChild(0).gameObject.GetComponent<Slider>();

    }
    
    void UpdateCurrentSettingSlider() //Called by NavigateMenu(), Updates the selected Slider
    {
        if (!settingMenuIsOpen) return;

        if (currentSelection == currentSelectionHolder.transform.childCount - 1) //Back Button selected
        {
            activeSettingsSlider = null;
            return;
        }
        
        Debug.Log("Menu Manager: Updating current setting slider");

        activeSettingsSlider = currentSliderHolder.GetChild(currentSelection).gameObject.GetComponent<Slider>();
        
        switch (currentSettingMenuID)
        {
            
            case 0: //In game settings
                activeSliderMod = gameSettingsSliderMods[currentSelection];
                break;
            
            case 1: //In audio settings
                activeSliderMod = audioSettingsSliderMods[currentSelection];
                break;
            
            case 2: //In player human settings
                activeSliderMod = playerBirdSettingsSliderMods[currentSelection];
                break;
            
            case 3: //In player bird settings
                activeSliderMod = playerBirdSettingsSliderMods[currentSelection];
                break;
            
            case 4: //In player bug settings
                activeSliderMod = playerBugSettingsSliderMods[currentSelection];
                break;
        }
        
        
    }
    
    void UpdateCurrentSliderValue(bool positive)
    {
        if (activeSettingsSlider == null) return;
        
        if (positive)
        {
            if (activeSettingsSlider.value + activeSliderMod <= activeSettingsSlider.maxValue)
                activeSettingsSlider.value += activeSliderMod;
            else activeSettingsSlider.value = activeSettingsSlider.maxValue;
            
        }
        
        else
        {
            if (activeSettingsSlider.value - activeSliderMod >= activeSettingsSlider.minValue)
                activeSettingsSlider.value -= activeSliderMod;
            else activeSettingsSlider.value = activeSettingsSlider.minValue;
        }
        
        currentNumberHolder.GetChild(currentSelection).GetComponent<TMP_Text>().text =
            $"[{activeSettingsSlider.value.ToString()}]";

        if (currentSettingMenuID != 1) //NormalSettings
        {
            settingsHaveBeenChanged = true;
        }
        else //AudioSettings
        {
            audioSettingsHaveBeenChanged = true;
        }
    }

    void WriteSettingsIntoManager()
    {
        if (settingsHaveBeenChanged)
        {
            Settings newSettings = SettingsManager.instance.GetSettings();
         
            newSettings._blackScreenFadeTime = gameSettingsSlider.GetChild(0).gameObject.GetComponent<Slider>().value;
            newSettings._controlsDisplayDuration = gameSettingsSlider.GetChild(1).gameObject.GetComponent<Slider>().value;
            newSettings._creditsDuration = gameSettingsSlider.GetChild(2).gameObject.GetComponent<Slider>().value;
         
            SettingsManager.instance.UpdateSettings(newSettings);
        }
        else if (audioSettingsHaveBeenChanged)
        {
            AudioSettings newAudioSettings = AudioManager.instance.GetAudioSettings();
            
            
            
            
        }
        
         
    }
        
    #endregion
    

}
