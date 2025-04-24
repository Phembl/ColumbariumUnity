using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private GameObject menuHolder;
    [SerializeField] private GameObject hauptMenu;
    [SerializeField] private GameObject chapterMenu;
    [SerializeField] private GameObject settingsMenu;
        
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


    
    private void Awake()
    {
      
        menuPages = new GameObject[]
        {
            hauptMenu,
            chapterMenu,
            settingsMenu
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
            menuIsOpen = false;
            StoryManager.Instance.UnpauseGame();
            menuHolder.GetComponent<CanvasGroup>().DOFade(0f, menuFadeTime);
            ResetMenu();
        }
        else
        {
            
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
            menuPages[currentMenuPage].GetComponent<CanvasGroup>().DOFade(0f, menuFadeTime);
            yield return new WaitForSeconds(menuFadeTime);
            ResetAuswahl();
        }
        
        currentMenuPage = page;
        Debug.Log("Opening menu: " + menuPages[page].name);
        
        //Finds "Auswahl" object which holds menu points
        currentAuswahlHolder = menuPages[page].transform.Find("Auswahl").gameObject;
        if (currentAuswahlHolder == null)Debug.LogError("Auswahl Holder not found");
        
        //Fades in new Menu page
        menuPages[page].GetComponent<CanvasGroup>().DOFade(1f, menuFadeTime).OnComplete(() => menuIsOpen = true);
        currentSelection = 0;

    }
    private void ResetMenu()
    {
        menuHolder.GetComponent<CanvasGroup>().alpha = 0f;
        hauptMenu.GetComponent<CanvasGroup>().alpha = 0f;
        chapterMenu.GetComponent<CanvasGroup>().alpha = 0f;
        settingsMenu.GetComponent<CanvasGroup>().alpha = 0f;

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
        if (currentMenuPage == 0) return;
        if (currentMenuPage == 1) StartCoroutine(SwitchMenuPage(0, true));
        if (currentMenuPage == 2) StartCoroutine(SwitchMenuPage(0, true));
    }


    private void SelectMenuPoint()
    {
        if (!menuIsOpen) return;

        if (currentMenuPage == 0)
        {
            if (currentSelection == 0)
            {
                StartCoroutine(SwitchMenuPage(1, true));
            }
            else if (currentSelection == 1)
            {
                StartCoroutine(SwitchMenuPage(2, true));
            }
        }
    }

}
