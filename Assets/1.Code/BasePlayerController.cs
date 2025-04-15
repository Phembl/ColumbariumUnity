using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Base abstract class for player controllers that handles common functionality
/// such as input processing, camera handling, and story integration
/// </summary>
public abstract class BasePlayerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the camera transform")]
    [SerializeField] protected Transform cameraTransform;
    [Tooltip("Reference to the Input System actions")]
    [SerializeField] protected InputActionAsset inputActions;

    [Header("Camera Settings")]
    [Tooltip("Mouse/look sensitivity")]
    public float lookSensitivity = 1.0f;
    [Tooltip("Maximum upward look angle in degrees")]
    public float maxLookUp = 90f;
    [Tooltip("Maximum downward look angle in degrees")]
    public float maxLookDown = -90f;

    [Header("Interaction Settings")]
    [Tooltip("Maximum distance for interaction raycasts")]
    [SerializeField] protected float interactionRange = 3f;
    [Tooltip("Layer mask for interactable objects")]
    [SerializeField] protected LayerMask interactionLayer;
    
    // Shared protected variables
    protected Rigidbody rb;
    protected CapsuleCollider capsuleCollider;
    protected Vector2 moveInput;
    protected Vector2 lookInput;
    protected float yRotation;
    protected float xRotation;
    protected bool isGrounded;
    protected bool isInputLocked = false;

    // Input Actions
    protected InputAction moveAction;
    protected InputAction lookAction;
    protected InputAction skipAction;
    protected InputAction interactionAction;
    
    // Interaction 
    protected GameObject currentInteractableObject;
    protected bool isSomethingHovered = false;

    // Constants
    protected const float GROUND_CHECK_DISTANCE = 0.3f;

    protected virtual void Awake()
    {
        // Get references
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Set up input actions
        SetupInputActions();
    }

    protected virtual void SetupInputActions()
    {
        // Initialize the action map
        var playerActionMap = inputActions.FindActionMap("Player");
        
        // Set up common actions
        moveAction = playerActionMap.FindAction("Move");
        lookAction = playerActionMap.FindAction("Look");
        skipAction = playerActionMap.FindAction("Skip");
        interactionAction = playerActionMap.FindAction("Interact");
        
        
      
        if (skipAction != null)
        {
            skipAction.performed += ctx => InteractWithObject();
            Debug.Log("Skip action registered in player controller");
        }
        else
        {
            Debug.LogWarning("Could not find 'Skip' action in Player action map. Story skipping won't work.");
        }

        if (interactionAction != null)
        {
            interactionAction.performed += ctx => InteractWithObject();
            Debug.Log("Interact action registered in player controller");
        }
            
        else
        {
            Debug.LogWarning("Could not find 'Interact' action in Player action map.");
        }
    }

    protected virtual void OnEnable()
    {
        // Enable input actions
        inputActions.Enable();
    }

    protected virtual void OnDisable()
    {
        // Disable input actions
        inputActions.Disable();
    }

    protected virtual void Start()
    {
        // Cursor setup for FPS controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    protected virtual void Update()
    {
        // Get input values
        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>() * (lookSensitivity / 10);

        // Handle camera rotation
        if (!isInputLocked)
        {
            HandleLookRotation();
        }
        
        // Check if grounded
        CheckGroundStatus();
        
        CheckForInteractableObjects();
        
        // Additional update logic specific to each controller type
        ControllerSpecificUpdate();
        
       
    }

    protected virtual void FixedUpdate()
    {
        // Skip movement handling if input is locked
        if (isInputLocked)
            return;
            
        // Handle movement - to be implemented by derived classes
        HandleMovement();
    }

    protected virtual void CheckForInteractableObjects()
    {
        if (cameraTransform == null) return;
    
        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactionRange, interactionLayer))
        {
            // We hit something on the interaction layer
            if (currentInteractableObject != hit.collider.gameObject)
            {
                isSomethingHovered = true;
                
                // We're looking at a new object
                currentInteractableObject = hit.collider.gameObject;
            
                // Call OnHover if the object has a method with that name
                currentInteractableObject.SendMessage("OnHoverEnter", SendMessageOptions.DontRequireReceiver);
                
            }
            
        }
        else if (currentInteractableObject != null)
        {
            // We were looking at something but now we're not
            currentInteractableObject.SendMessage("OnHoverExit", SendMessageOptions.DontRequireReceiver);
            currentInteractableObject = null;
            isSomethingHovered = false;
        }
    }

    protected virtual void InteractWithObject()
    {
        Debug.Log("Try Interaction");
        if (isSomethingHovered && currentInteractableObject != null)
        {
            // Call OnInteract if the object has a method with that name
            currentInteractableObject.SendMessage("OnInteract", SendMessageOptions.DontRequireReceiver);
            
        }
    }
    #region Abstract and Virtual Methods

    /// <summary>
    /// Handles controller-specific movement physics
    /// </summary>
    protected abstract void HandleMovement();

    /// <summary>
    /// Additional update logic specific to each controller type
    /// </summary>
    protected virtual void ControllerSpecificUpdate() { }

    #endregion
    
    

    #region Helper Methods

    /// <summary>
    /// Calculates movement direction based on camera orientation
    /// </summary>
    protected Vector3 CalculateMoveDirection()
    {
        // Calculate move direction based on camera orientation
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        // Zero out y components to keep movement on the horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        // Combine inputs
        return forward * moveInput.y + right * moveInput.x;
    }

    /// <summary>
    /// Handles camera rotation based on mouse/look input
    /// </summary>
    protected virtual void HandleLookRotation()
    {
        // Update camera rotation based on mouse input
        yRotation += lookInput.x;
        xRotation -= lookInput.y; // Invert Y axis
        
        // Clamp vertical rotation
        xRotation = Mathf.Clamp(xRotation, maxLookDown, maxLookUp);
        
        // Apply rotations
        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    /// <summary>
    /// Checks if the controller is grounded using raycasting
    /// </summary>
    protected virtual void CheckGroundStatus()
    {
        // Perform a raycast to check if we're grounded
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        
        isGrounded = Physics.Raycast(rayStart, Vector3.down, out hit, 
                                    capsuleCollider.height / 2 + GROUND_CHECK_DISTANCE);
    }

    /// <summary>
    /// Locks player input for cutscenes/story sequences
    /// </summary>
    public virtual void LockInput()
    {
        isInputLocked = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    /// <summary>
    /// Unlocks player input after cutscenes/story sequences
    /// </summary>
    public virtual void UnlockInput()
    {
        // Overrides current y rotation
        yRotation = transform.eulerAngles.y;

        isInputLocked = false;
        rb.isKinematic = false;
    }
    

    #endregion
}