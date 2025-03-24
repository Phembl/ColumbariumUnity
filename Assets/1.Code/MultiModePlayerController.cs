using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls a character that can switch between three movement modes: Human, Bird, and Bug
/// Each mode has unique movement characteristics and camera positions
/// </summary>
public class MultiModePlayerController : MonoBehaviour
{
    // Movement mode enum
    public enum MovementMode
    {
        Human,
        Bird,
        Bug
    }

    [Header("References")]
    [Tooltip("Reference to the camera transform")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Reference to the Input System actions")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Human Settings")]
    [Tooltip("Movement speed in Human mode")]
    public float humanWalkSpeed = 6f;
    [Tooltip("Jump force in Human mode")]
    public float humanJumpForce = 8f;
    [Tooltip("Camera height in Human mode")]
    public float humanCameraHeight = 1.8f;

    [Header("Bird Settings")]
    [Tooltip("Flying speed in Bird mode")]
    public float birdFlySpeed = 8f;
    [Tooltip("Gravity scale in Bird mode (lower values make flying easier)")]
    [Range(0f, 1f)]
    public float birdGravityScale = 0.2f;
    [Tooltip("Camera height in Bird mode")]
    public float birdCameraHeight = 1.8f;
    [Tooltip("Upward force when looking up in Bird mode")]
    public float birdUpwardForce = 15f;
    [Tooltip("Glide downward angle factor")]
    public float birdGlideDownwardFactor = 0.2f;
    [Tooltip("Air resistance during gliding (0.998 = minimal slowdown)")]
    [Range(0.9f, 0.999f)]
    public float birdGlideFactor = 0.998f;

    [Header("Bug Settings")]
    [Tooltip("Movement speed in Bug mode")]
    public float bugWalkSpeed = 3f;
    [Tooltip("Camera height in Bug mode")]
    public float bugCameraHeight = 0.2f;
    [Tooltip("Enable head bobbing in Bug mode")]
    public bool enableBugHeadBob = true;
    [Tooltip("Speed of head bobbing in Bug mode")]
    public float bugHeadBobSpeed = 10f;
    [Tooltip("Amount of head bobbing in Bug mode")]
    public float bugHeadBobAmount = 0.04f;

    // Private variables
    private MovementMode currentMode = MovementMode.Human;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float currentSpeed;
    private float yRotation;
    private float xRotation;
    private float headBobTimer;
    private float originalBugCameraHeight;
    private bool isJumping;
    private bool isGrounded;
    private float verticalVelocity;
    private float cameraVerticalOffset;
    private float cameraHorizontalOffset;
    private bool isInputLocked = false;

    // Input Actions
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction humanModeAction;
    private InputAction birdModeAction;
    private InputAction bugModeAction;
    private InputAction skipAction;

    [Header("Camera Settings")]
    [Tooltip("Mouse/look sensitivity")]
    public float lookSensitivity = 1.0f;
    [Tooltip("Maximum upward look angle in degrees")]
    public float maxLookUp = 90f;
    [Tooltip("Maximum downward look angle in degrees")]
    public float maxLookDown = -90f;

    // Constants
    private const float GROUND_CHECK_DISTANCE = 0.1f;

    private void Awake()
    {
        // Get references
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Initialize camera position
        originalBugCameraHeight = bugCameraHeight;

        // Set up input actions
        var playerActionMap = inputActions.FindActionMap("Player");
        
        moveAction = playerActionMap.FindAction("Move");
        lookAction = playerActionMap.FindAction("Look");
        jumpAction = playerActionMap.FindAction("Jump");
        humanModeAction = playerActionMap.FindAction("HumanMode");
        birdModeAction = playerActionMap.FindAction("BirdMode");
        bugModeAction = playerActionMap.FindAction("BugMode");
        skipAction = playerActionMap.FindAction("Skip");

        // Add input callbacks
        jumpAction.performed += ctx => StartJump();
        jumpAction.canceled += ctx => StopJump();
        humanModeAction.performed += ctx => SetMovementMode(MovementMode.Human);
        birdModeAction.performed += ctx => SetMovementMode(MovementMode.Bird);
        bugModeAction.performed += ctx => SetMovementMode(MovementMode.Bug);
        
        // Add skip story callback
        if (skipAction != null)
        {
            skipAction.performed += ctx => TrySkipStory();
            Debug.Log("Skip action registered in player controller");
        }
        else
        {
            Debug.LogWarning("Could not find 'Skip' action in Player action map. Story skipping won't work.");
        }
    }

    private void OnEnable()
    {
        // Enable input actions
        inputActions.Enable();
    }

    private void OnDisable()
    {
        // Disable input actions
        inputActions.Disable();
    }

    private void Start()
    {
        // Cursor setup for FPS controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Start in Human mode
        SetMovementMode(MovementMode.Human);
    }

    private void Update()
    {
        // Get input values
        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>() * lookSensitivity;

        // Handle camera rotation
        HandleLookRotation();

        // Check if grounded
        CheckGroundStatus();

        // Handle bug mode head bob
        if (currentMode == MovementMode.Bug && enableBugHeadBob)
        {
            HandleBugHeadBob();
        }
    }

    private void FixedUpdate()
    {
        // Skip movement handling if input is locked
        if (isInputLocked)
            return;
            
        // Handle movement based on current mode
        switch (currentMode)
        {
            case MovementMode.Human:
                HandleHumanMovement();
                break;
            case MovementMode.Bird:
                HandleBirdMovement();
                break;
            case MovementMode.Bug:
                HandleBugMovement();
                break;
        }
    }

    #region Movement Handlers

    private void HandleHumanMovement()
    {
        // Calculate movement direction
        Vector3 moveDirection = CalculateMoveDirection();
        
        // Apply movement
        Vector3 targetVelocity = moveDirection * humanWalkSpeed;
        
        // Preserve vertical velocity
        targetVelocity.y = rb.linearVelocity.y;
        
        // Apply velocity
        rb.linearVelocity = targetVelocity;
    }

    private void HandleBirdMovement()
    {
        // Get current velocity
        Vector3 currentVelocity = rb.linearVelocity;
        
        // Get input magnitude for forward movement
        float forwardInput = moveInput.y;
        
        // Get camera direction
        Vector3 cameraDirection = cameraTransform.forward;
        Vector3 cameraUpDirection = cameraTransform.up;
        
        // Apply gravity - ensure bird is always falling unless actively flying up
        if (currentVelocity.y > -1f)
        {
            // Add extra downward force to ensure bird is falling
            currentVelocity.y -= 0.2f;
        }
        
        if (forwardInput > 0.1f)  // Only apply upward force with significant forward input
        {
            // ACTIVE FLYING - Player is pressing forward
            
            // Calculate flying velocity based on camera direction
            Vector3 desiredVelocity = cameraDirection * forwardInput * birdFlySpeed;
            
            // Check if looking upward
            if (cameraDirection.y > 0.1f)  // Only count significant upward looking
            {
                // When looking up, apply extra force to fight gravity
                // The more you look up, the stronger the upward force
                float upwardForce = cameraDirection.y * forwardInput * birdUpwardForce;
                desiredVelocity.y = upwardForce;
            }
            else if (cameraDirection.y > -0.2f && cameraDirection.y <= 0.1f)
            {
                // Looking roughly forward - maintain altitude with difficulty
                // Just enough upward force to slow the fall, not stop it completely
                desiredVelocity.y = 1f;
            }
            // else - Looking downward - dive faster (let gravity do its work)
            
            // Smoothly blend current velocity with desired velocity
            Vector3 newVelocity = Vector3.zero;
            newVelocity.x = Mathf.Lerp(currentVelocity.x, desiredVelocity.x, Time.fixedDeltaTime * 5f);
            newVelocity.z = Mathf.Lerp(currentVelocity.z, desiredVelocity.z, Time.fixedDeltaTime * 5f);
            
            // Apply the desired Y velocity, but don't completely override gravity
            if (desiredVelocity.y > 0)
            {
                // When flying upward, blend toward desired Y
                newVelocity.y = Mathf.Lerp(currentVelocity.y, desiredVelocity.y, Time.fixedDeltaTime * 3f);
            }
            else
            {
                // When diving, don't interfere with gravity too much
                newVelocity.y = currentVelocity.y;
            }
            
            // Apply the new velocity
            rb.linearVelocity = newVelocity;
        }
        else
        {
            // GLIDING - No forward input or minimal input
            
            // Calculate the current speed magnitude (horizontal only)
            float currentHorizontalSpeed = Mathf.Sqrt(currentVelocity.x * currentVelocity.x + 
                                                    currentVelocity.z * currentVelocity.z);
            
            // Only apply gliding if we have some horizontal speed
            if (currentHorizontalSpeed > 0.3f)
            {
                // Apply glide factor (slight air resistance)
                Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                horizontalVelocity *= birdGlideFactor;
                
                // Set a downward angle proportional to speed for gliding
                float glideDownwardVelocity = Mathf.Min(-1.2f, -currentHorizontalSpeed * birdGlideDownwardFactor);
                
                // Apply glide physics
                Vector3 newVelocity = horizontalVelocity;
                
                // Always make sure we're falling, never hovering
                if (currentVelocity.y > glideDownwardVelocity)
                {
                    // Force the bird to adopt a downward glide angle
                    newVelocity.y = Mathf.Lerp(currentVelocity.y, glideDownwardVelocity, Time.fixedDeltaTime * 3f);
                }
                else
                {
                    // Keep current downward velocity
                    newVelocity.y = currentVelocity.y;
                }
                
                // Apply modified velocity
                rb.linearVelocity = newVelocity;
            }
            else
            {
                // Not enough horizontal speed - just fall
                // Make sure we have some downward velocity
                if (currentVelocity.y > -3f)
                {
                    currentVelocity.y = Mathf.Lerp(currentVelocity.y, -3f, Time.fixedDeltaTime * 2f);
                    rb.linearVelocity = new Vector3(currentVelocity.x, currentVelocity.y, currentVelocity.z);
                }
            }
        }
    }

    private void HandleBugMovement()
    {
        // Calculate movement direction
        Vector3 moveDirection = CalculateMoveDirection();
        
        // Apply movement
        Vector3 targetVelocity = moveDirection * bugWalkSpeed;
        
        // Preserve vertical velocity
        targetVelocity.y = rb.linearVelocity.y;
        
        // Apply velocity
        rb.linearVelocity = targetVelocity;
    }

    private void HandleBugHeadBob()
    {
        // Only apply head-bobbing when the bug is moving and on the ground
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;
        bool isMoving = speed > 0.1f;
        
        if (isMoving && isGrounded)
        {
            // Update the head-bob timer based on movement speed
            headBobTimer += Time.deltaTime * bugHeadBobSpeed * (speed / bugWalkSpeed);
            
            // Calculate the vertical offset using a sine wave
            float verticalOffset = Mathf.Sin(headBobTimer) * bugHeadBobAmount;
            
            // Apply the offset to the camera's Y position
            Vector3 newCameraPosition = cameraTransform.localPosition;
            newCameraPosition.y = bugCameraHeight + verticalOffset;
            
            // Add a slight side-to-side wobble for more natural movement
            float horizontalOffset = Mathf.Cos(headBobTimer * 0.5f) * (bugHeadBobAmount * 0.3f);
            newCameraPosition.x = horizontalOffset;
            
            cameraTransform.localPosition = newCameraPosition;
        }
        else
        {
            // When not moving, smoothly return to the original height
            Vector3 currentPosition = cameraTransform.localPosition;
            if (!Mathf.Approximately(currentPosition.y, bugCameraHeight) || 
                !Mathf.Approximately(currentPosition.x, 0f))
            {
                Vector3 targetPosition = new Vector3(0f, bugCameraHeight, currentPosition.z);
                Vector3 newPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * 5f);
                cameraTransform.localPosition = newPosition;
            }
        }
    }

    #endregion

    #region Helper Methods

    private Vector3 CalculateMoveDirection()
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

    private void HandleLookRotation()
    {
        // Skip rotation handling if input is locked
        if (isInputLocked)
            return;
            
        // Update camera rotation based on mouse input
        yRotation += lookInput.x;
        xRotation -= lookInput.y; // Invert Y axis
        
        // Clamp vertical rotation
        xRotation = Mathf.Clamp(xRotation, maxLookDown, maxLookUp);
        
        // Apply rotations
        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    private void CheckGroundStatus()
    {
        // Perform a raycast to check if we're grounded
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        
        isGrounded = Physics.Raycast(rayStart, Vector3.down, out hit, 
                                    capsuleCollider.height / 2 + GROUND_CHECK_DISTANCE);
    }

    private void StartJump()
    {
        // Only jump in Human or Bug mode and when grounded
        if (currentMode != MovementMode.Bird && isGrounded)
        {
            isJumping = true;
            
            // Apply jump force
            float jumpForce = (currentMode == MovementMode.Human) ? humanJumpForce : humanJumpForce * 0.5f;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void StopJump()
    {
        isJumping = false;
    }

    public void SetMovementMode(MovementMode newMode)
    {
        // Update current mode
        currentMode = newMode;
        
        // Apply mode-specific settings
        switch (currentMode)
        {
            case MovementMode.Human:
                // Apply human settings
                rb.useGravity = true;
                rb.linearDamping = 0;
                currentSpeed = humanWalkSpeed;
                
                // Set camera to human height
                Vector3 humanCameraPos = cameraTransform.localPosition;
                humanCameraPos.y = humanCameraHeight;
                cameraTransform.localPosition = humanCameraPos;
                break;
                
            case MovementMode.Bird:
                // Apply bird settings
                rb.useGravity = true;
                rb.linearDamping = 0.5f; // Add some air resistance
                currentSpeed = birdFlySpeed;
                
                // Set camera to bird height
                Vector3 birdCameraPos = cameraTransform.localPosition;
                birdCameraPos.y = birdCameraHeight;
                cameraTransform.localPosition = birdCameraPos;
                break;
                
            case MovementMode.Bug:
                // Apply bug settings
                rb.useGravity = true;
                rb.linearDamping = 0;
                currentSpeed = bugWalkSpeed;
                
                // Set camera very low to the ground
                Vector3 bugCameraPos = cameraTransform.localPosition;
                bugCameraPos.y = bugCameraHeight;
                cameraTransform.localPosition = bugCameraPos;
                
                // Reset head-bobbing timer
                headBobTimer = 0f;
                break;
        }
        
        Debug.Log($"Switched to {currentMode} mode");
    }

    // Methods to lock/unlock input for story sequences
    public void LockInput()
    {
        isInputLocked = true;
    }

    public void UnlockInput()
    {
        isInputLocked = false;
    }
    
    /// <summary>
    /// Attempts to skip the current story if one is playing
    /// </summary>
    private void TrySkipStory()
    {
        // Check if the StoryManager exists and if a story is playing
        if (StoryManager.Instance != null && StoryManager.Instance.IsStoryPlaying())
        {
            Debug.Log("Skip button pressed - stopping story moment");
            StoryManager.Instance.StopStoryMoment();
        }
    }

    #endregion
}