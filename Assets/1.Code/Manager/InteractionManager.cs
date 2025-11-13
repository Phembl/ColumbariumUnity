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
    private InputAction startAction;
    private Vector2 moveInput;
    private bool navigateProcessed = false;
    
    // Events
    public static event Action StartButtonPressed;
    
    // External Control
    private bool isActive;
    public void ActivateInteraction(bool activate) => isActive = activate;
   
    
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
        startAction = playerActionMap.FindAction("Pause");
        
        submitAction.performed += ctx => PressSubmitButton();
        startAction.performed += ctx => PressStartButton();
        
        inputActions.Enable();
        
    }

    void PressStartButton()
    {
        if (!isActive) return;
        
        Debug.Log("InteractionManager: Start button pressed");
        StartButtonPressed?.Invoke();
    }

    void PressSubmitButton()
    {
        if (!isActive) return;
    }

    
    void Update()
    {
        // Get input values
        if (isActive)
        {
            moveInput = navigateAction.ReadValue<Vector2>();
        }
    }
}
