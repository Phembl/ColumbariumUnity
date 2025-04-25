using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

public class MenuManager : MonoBehaviour
{
    [ReadOnly]
    [SerializeField] private InputActionAsset inputActions;
    [ReadOnly]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GameObject menuHolder;
    [Header("Menu Pages")]   
    [SerializeField] private GameObject hauptMenu;
    [SerializeField] private GameObject controlsMenu;
    [SerializeField] private GameObject chapterMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject audioSettingsMenu;
    [SerializeField] private GameObject playerSettingsMenu;
    [SerializeField] private GameObject playerHumanSettingsMenu;
    [SerializeField] private GameObject playerBirdSettingsMenu;
    [SerializeField] private GameObject playerBugSettingsMenu;
    
        
    // Input Actions
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction pauseAction;
    private InputAction backAction;
    private Vector2 navigationInput;
    private bool navigationProcessing;
    
    // State tracking
    bool menuIsOpen;
    private int currentMenuPage;
    private int currentSelection;
    private GameObject[] menuPages;
    private GameObject currentAuswahlHolder;

    // Setup
    private float menuFadeTime = 0.5f;


    public static AudioManager Instance;
    
    private void Awake()
    {
      
        menuPages = new GameObject[]
        {
            hauptMenu,
            controlsMenu,
            chapterMenu,
            settingsMenu,
            audioSettingsMenu,
            playerSettingsMenu,
            playerHumanSettingsMenu,
            playerBirdSettingsMenu,
            playerBugSettingsMenu
        };
        
        SetupInputActions();
        ResetMenu();
    }

    // Update is called once per frame
    void Update()
    {
        if (menuIsOpen)
        {
            navigationInput = navigateAction.ReadValue<Vector2>();
            navigateMenu();
        }
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

    private void OpenMenu()
    {
        if (menuIsOpen)
        {
            Debug.Log("Menu is closing");
            menuHolder.GetComponent<CanvasGroup>().DOFade(0f, menuFadeTime)
                .OnComplete(() =>
                    { 
                        menuIsOpen = false;
                        ResetMenu();
                        StoryManager.Instance.UnpauseGame();
                    });
            
        }
        else
        {
            Debug.Log("Menu is opening");
            StoryManager.Instance.PauseGame();
            StartCoroutine(SwitchMenuPage(0));
            menuHolder.GetComponent<CanvasGroup>().DOFade(1f, menuFadeTime);
            
            
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
        
        //Finds "Auswahl" object which holds menu points
        currentAuswahlHolder = menuPages[page].transform.Find("Auswahl").gameObject;
        if (currentAuswahlHolder == null)Debug.LogError("Auswahl Holder not found");
        
        //Fades in new Menu page
        menuPages[page].GetComponent<CanvasGroup>().DOFade(1f, menuFadeTime).OnComplete(() => menuIsOpen = true).SetEase(Ease.OutQuad);
        currentSelection = 0;

    }
    private void ResetMenu()
    {
        menuHolder.GetComponent<CanvasGroup>().alpha = 0f;

        foreach (GameObject menuPage in menuPages) // Loops through all pages and makes them invisible
        {
            menuPage.GetComponent<CanvasGroup>().alpha = 0f;
        }

        if (currentAuswahlHolder != null)
        {
            ResetAuswahl();
        }

    }

    private void ResetAuswahl()
    {
        //Make all Menu points not underlined
        foreach (Transform child in currentAuswahlHolder.transform)
        {
            child.gameObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
        }
        
        //Underline first point
        currentAuswahlHolder.transform.GetChild(0).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Underline;
    }


    private void navigateMenu()
    {
        if (navigationInput.magnitude > 0.1 && !navigationProcessing)
        {
            navigationProcessing = true;
            
            if (navigationInput.y > 0)
            {
                if (currentSelection == 0)
                {
                    currentSelection = currentAuswahlHolder.transform.childCount - 1;
                }
                else
                {
                    currentSelection--;
                }
            }
            else if (navigationInput.y < 0)
            {
                if (currentSelection < currentAuswahlHolder.transform.childCount - 1)
                {
                    currentSelection++;
                }
                else
                {
                    currentSelection = 0;
                }
            }
            
            foreach (Transform child in currentAuswahlHolder.transform)
            {
                child.gameObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
            }
        
            currentAuswahlHolder.transform.GetChild(currentSelection).gameObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Underline;
            
        }

        else if (navigationInput.magnitude < 0.1)
        {
            navigationProcessing = false;
        }
        

        
    }
    
    void GoPageBack()
    {
        if (!menuIsOpen) return;

        switch (currentMenuPage)
        {
            case 0:
                return;
            case 1: // controls -> main
            case 2: // chapters -> main
            case 3: // settings -> main
                StartCoroutine(SwitchMenuPage(0, true));
                break;
            case 4: // audioSettings -> settings
            case 5: // playerSettings -> settings
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
        if (currentMenuPage != 0 && currentSelection == currentAuswahlHolder.transform.childCount - 1) GoPageBack();
            
        switch (currentMenuPage)
        {
            case 0: // In Main menu
                switch (currentSelection)
                {
                    case 0: // -> Controls
                        StartCoroutine(SwitchMenuPage(1, true));
                        break;
                    case 1: // -> Controls
                        StartCoroutine(SwitchMenuPage(2, true));
                        break;
                    case 2: // -> Settings
                        StartCoroutine(SwitchMenuPage(3, true));
                        break;
                }

                break;

            case 1: // In controls menu
                // Should never trigger because only Button is back button
                break;
            
            case 2: // In chapters menu
                OpenMenu(); // Close Menu
                StoryManager.Instance.StartNewGame(currentSelection); //Load selected scene
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
            
        }

    }

}
