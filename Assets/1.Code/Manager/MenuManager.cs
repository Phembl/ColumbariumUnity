using System;
using System.Collections;
using COLUMBARIUM.Global;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    
    //Setting menu
    private bool settingMenuIsOpen;
    private Slider activeSettingsSlider;
    private float activeSliderMod = 1f;
    private int currentSettingMenuID = -1;
    Transform currentSliderHolder;
    Transform currentNumberHolder;
    private readonly float[] gameSettingsSliderMods =  {0.5f, 0.5f, 2f };
    
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
        pauseAction.performed += ctx => OpenMenu();
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
    private void OpenMenu(bool closeInstant = false)
    {
        if (!menuIsUsable) return;
        
        if (menuIsOpen)
        {
            Debug.Log("Menu is closing");
            if (closeInstant)
            {
                menuIsOpen = false;
                ResetMenu();
                //StoryManager.Instance.UnpauseGame();
                PauseGame?.Invoke(false);
            }
            else
            {
                menuHolder.GetComponent<CanvasGroup>().DOFade(0f, menuFadeTime)
                    .OnComplete(() =>
                    { 
                        menuIsOpen = false;
                        ResetMenu();
                        //StoryManager.Instance.UnpauseGame();
                        PauseGame?.Invoke(false);
                    });
            }
         
            
        }
        else
        {
            Debug.Log("Menu is opening");
            //StoryManager.Instance.PauseGame();
            StartCoroutine(SwitchMenuPage(0));
            menuHolder.GetComponent<CanvasGroup>().DOFade(1f, menuFadeTime);
            PauseGame?.Invoke(true);
            
            
        }
    }

    private IEnumerator SwitchMenuPage(int page, bool closePage = false)
    {
        
        //Checks if a page should also be closed
        if (closePage)
        {
            menuIsOpen = false;
            menuPages[currentMenuPage].GetComponent<CanvasGroup>().DOFade(0f, menuFadeTime).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(menuFadeTime);
            ResetAuswahl();
        }
        
        currentMenuPage = page;
        Debug.Log("Opening menu: " + menuPages[page].name);
        
        //Finds "pages" object which holds menu points
        currentSelectionHolder = menuPages[page].transform.Find("pages").gameObject;
        if (currentSelectionHolder == null)Debug.LogError("pages Holder not found");
        
        //Fades in new Menu page
        menuPages[page].GetComponent<CanvasGroup>().DOFade(1f, menuFadeTime).OnComplete(() => menuIsOpen = true).SetEase(Ease.OutQuad);
        
        //Select first item
        currentSelectionHolder.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Underline;
        currentSelection = 0;

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
            ResetAuswahl();
        }

    }

    private void ResetAuswahl()
    {
        //Make all Menu points not underlined
        foreach (Transform child in currentSelectionHolder.transform)
        {
            child.gameObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
        }
        
        //Underline first point
        currentSelectionHolder.transform.GetChild(0).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Underline;
    }


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
                        OpenMenu(); // Close Menu
                        //StoryManager.Instance.ResetGame(true);
                        break;
                    case 4: // -> Restart
                        OpenMenu(true); // Close Menu
                        GameManager.instance.Restart();
                        break;
                    
                }

                break;

            case 1: // In controls menu
                // Should never trigger because only Button is back button
                break;
            
            case 2: // In parts menu
                OpenMenu(true); // Close Menu
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
                    case 0: // -> AudioSettings
                        StartCoroutine(SwitchMenuPage(4, true));
                        break;
                    case 1: // -> PlayerSettings
                        StartCoroutine(SwitchMenuPage(5, true));
                        break;
                    
                    case 2: // -> GameSettings
                        PrepareSettingsMenu(2);
                        StartCoroutine(SwitchMenuPage(9, true));
                        break;

                }
                break;
            
            case 4: // Audio Settings
                break;
            
            case 5: // Player Settings
                switch (currentSelection)
                {
                    case 0: // -> PlayerHumanSettings
                        StartCoroutine(SwitchMenuPage(6, true));
                        break;
                    case 1: // -> PlayerBirdSettings
                        StartCoroutine(SwitchMenuPage(7, true));
                        break;
                    case 2: // -> PlayerBugSettings
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

        switch (settingID)
        {
            case 0: //Audio
                currentSettingMenuID = 0;
                break;

            case 1: //Player
                currentSettingMenuID = 1;
                break;


            case 2: //Game
                currentSliderHolder = gameSettingsMenu.transform.Find("sliders");
                currentNumberHolder = gameSettingsMenu.transform.Find("numbers");

                currentSliderHolder.GetChild(0).gameObject.GetComponent<Slider>().value =
                    SettingsManager.instance.blackScreenFadeTime;
                currentNumberHolder.GetChild(0).gameObject.GetComponent<TMP_Text>().text =
                    $"[{SettingsManager.instance.blackScreenFadeTime}]";
                
                currentSliderHolder.GetChild(1).gameObject.GetComponent<Slider>().value =
                    SettingsManager.instance.controlsDisplayDuration;
                currentNumberHolder.GetChild(1).gameObject.GetComponent<TMP_Text>().text =
                    $"[{SettingsManager.instance.controlsDisplayDuration}]";
                
                currentSliderHolder.GetChild(2).gameObject.GetComponent<Slider>().value =
                    SettingsManager.instance.creditsDuration;
                currentNumberHolder.GetChild(2).gameObject.GetComponent<TMP_Text>().text =
                    $"[{SettingsManager.instance.creditsDuration}]";

                activeSettingsSlider = currentSliderHolder.GetChild(0).gameObject.GetComponent<Slider>();
                currentSettingMenuID = 2;
                break;

            default:
                Debug.Log("Menu Manager: Invalid Settings Menu");
                break;
        }

    }
    
    void UpdateCurrentSettingSlider() //Called by NavigateMenu()
    {
        if (!settingMenuIsOpen) return;
        
        Debug.Log("Menu Manager: Updating current setting slider");

        switch (currentSettingMenuID)
        {
            case 0: //In audio settings
                break;
            
            case 1: //In player settings
                break;
            
            case 2: //In game settings
                activeSettingsSlider = currentSliderHolder.GetChild(currentSelection).gameObject.GetComponent<Slider>();
                activeSliderMod = gameSettingsSliderMods[currentSelection];
                break;
        }

        
        
    }
    
    void UpdateCurrentSliderValue(bool positive)
    {
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
    }
        
    #endregion
    

}
