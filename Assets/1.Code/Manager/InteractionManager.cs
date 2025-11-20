using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private InputActionAsset inputActions;
    
    // Input Actions
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction pauseAction;
    private InputAction startAction;
    private InputAction skipAction;
    private Vector2 navigationInput;
    
    // Events
    public static event Action PauseButtonPressed;
    public static event Action SubmitButtonPressed;
    public static event Action StartButtonPressed;
    public static event Action SkipButtonPressed;
    public static event Action<int> NavigationInput;
    
    // External Control
    private bool isActive;
    
   
    
    public static InteractionManager instance;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        
    }

    public void InitInteraction()
    {
        SetupInputActions();
        
        Debug.Log("InteractionManager initialized.");
    }
    
    private void SetupInputActions()
    {
        // Initialize the action map
        var playerActionMap = inputActions.FindActionMap("UI");
        
        // Set up common actions
        navigateAction = playerActionMap.FindAction("Navigate");
        submitAction = playerActionMap.FindAction("Submit");
        pauseAction = playerActionMap.FindAction("Pause");
        skipAction = playerActionMap.FindAction("Skip");
        startAction = playerActionMap.FindAction("Start");
        
        submitAction.performed += ctx => PressSubmitButton();
        pauseAction.performed += ctx => PressPauseButton();
        skipAction.performed += ctx => PressSkipButton();
        startAction.performed += ctx => PressStartButton();
        
        inputActions.Enable();
        
    }
    
    public void ActivateInteraction(bool activate)
    {
        SetupInputActions();
        isActive = activate;
    }

    void PressPauseButton()
    {
        if (!isActive) return;
        
        Debug.Log("InteractionManager: Pause button pressed");
        PauseButtonPressed?.Invoke();
    }

    void PressSubmitButton()
    {
        if (!isActive) return;
        
        Debug.Log("InteractionManager: Submit button pressed");
        SubmitButtonPressed?.Invoke();
    }

    void PressSkipButton()
    {
        if (!isActive) return;
        Debug.Log("InteractionManager: Skip button pressed");
        SkipButtonPressed?.Invoke();
    }

    void PressStartButton()
    {
        if (!isActive) return;
        Debug.Log("InteractionManager: Start button pressed");
        StartButtonPressed?.Invoke();
    }

    
    void Update()
    {
        // Get input values
        if (!isActive) return;
        
        navigationInput = navigateAction.ReadValue<Vector2>();
        if (navigationInput.x != 0)
        {
            if (navigationInput.x > 0)
            {
                NavigationInput?.Invoke(1);
            }
            else if (navigationInput.x < 0)
            {
                NavigationInput?.Invoke(-1);
            }
        }
    }
}
