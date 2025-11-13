using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the player in Bird mode with vertical flying button and horizontal gliding
/// </summary>
public class BirdPlayerController : BasePlayerController
{
    [Header("Bird Settings")]
    [Tooltip("Vertical rise speed when fly button is pressed")]
    public float riseSpeed = 7f;
    
    [Tooltip("Horizontal gliding speed")]
    public float glideSpeed = 8f;
    
    [Tooltip("Constant gravitational pull")]
    public float gravityPull = 5f;
    
    [Tooltip("Air resistance during gliding (0.98 = moderate slowdown)")]
    [Range(0.8f, 0.999f)]
    public float airResistance = 0.98f;
    
    [Tooltip("Ground friction when landed (higher = quicker stop)")]
    [Range(1f, 20f)]
    public float groundFriction = 10f;

    // Input action for flying upward
    private InputAction flyAction;
    
    // Tracking variables
    private bool isFlying = false;
    private float verticalVelocity = 0f;
    private Vector3 horizontalVelocity = Vector3.zero;
    
    protected override void SetupInputActions()
    {
        base.SetupInputActions();
        
        // Set up the fly action
        var playerActionMap = inputActions.FindActionMap("Player");
        flyAction = playerActionMap.FindAction("Jump"); // Using Jump action for fly button
        
        if (flyAction == null)
        {
            Debug.LogError("Fly action not found in input actions. Bird controller won't be able to fly upward.");
        }
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Make sure the rigidbody is set up properly
        if (rb != null)
        {
            rb.useGravity = false; // We'll handle gravity ourselves
            rb.linearDamping = 0;  // We'll handle air resistance ourselves
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Check if fly button is being pressed
        isFlying = flyAction != null && flyAction.ReadValue<float>() > 0;
    }

    /// <summary>
    /// Handles movement physics for bird mode
    /// </summary>
    protected override void HandleMovement()
    {
        // Handle vertical movement based on fly button
        HandleVerticalMovement();
        
        // Handle horizontal movement (gliding)
        HandleHorizontalMovement();
        
        // Apply the final velocity
        ApplyFinalVelocity();
    }
    
    /// <summary>
    /// Handles vertical movement based on fly button input
    /// </summary>
    private void HandleVerticalMovement()
    {
        // Get current vertical velocity
        verticalVelocity = rb.linearVelocity.y;
        
        if (isGrounded)
        {
            // Reset vertical velocity when grounded
            verticalVelocity = 0;
            
            // Allow flying again from ground
            if (isFlying)
            {
                // Initial upward burst when starting to fly from ground
                verticalVelocity = riseSpeed * 0.8f;
            }
        }
        else
        {
            // In air - apply flying or gravity
            if (isFlying)
            {
                // Apply upward force when fly button is pressed
                verticalVelocity += riseSpeed * Time.fixedDeltaTime * 2f;
                
                // Cap maximum rise speed
                verticalVelocity = Mathf.Min(verticalVelocity, riseSpeed);
            }
            else
            {
                // Apply gravity when not actively flying
                verticalVelocity -= gravityPull * Time.fixedDeltaTime;
            }
        }
    }
    
    /// <summary>
    /// Handles horizontal movement and gliding
    /// </summary>
    private void HandleHorizontalMovement()
    {
        // Get current horizontal velocity
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        if (isGrounded)
        {
            // On ground - move like walking or apply friction to stop
            if (moveInput.magnitude > 0.1f)
            {
                // Calculate ground movement direction
                Vector3 moveDirection = CalculateMoveDirection();
                
                // Apply movement at reduced speed on ground
                horizontalVelocity = moveDirection * (glideSpeed * 0.5f);
            }
            else
            {
                // Apply ground friction to stop when no input
                horizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, Vector3.zero, Time.fixedDeltaTime * groundFriction);
            }
        }
        else
        {
            // In air - glide forward based on camera direction
            
            // Get forward direction from camera (ignoring vertical component)
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            // Get right direction from camera
            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();
            
            // If player is giving input, use it to steer
            if (moveInput.magnitude > 0.1f)
            {
                // Calculate desired direction based on input
                Vector3 inputDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
                inputDirection.Normalize();
                
                // Blend current direction with input direction for smooth turning
                Vector3 targetVelocity = inputDirection * glideSpeed;
                
                // Smooth transition to new direction
                horizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetVelocity, Time.fixedDeltaTime * 3f);
            }
            else if (currentHorizontalVelocity.magnitude > 0.5f)
            {
                // No input but already moving - maintain direction with air resistance
                horizontalVelocity = currentHorizontalVelocity * airResistance;
            }
            else
            {
                // No input and minimal speed - drift forward gently
                horizontalVelocity = cameraForward * (glideSpeed * 0.2f);
            }
        }
    }
    
    /// <summary>
    /// Applies the calculated velocity to the rigidbody
    /// </summary>
    private void ApplyFinalVelocity()
    {
        // Combine horizontal and vertical velocities
        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;
        
        // Apply the final velocity
        rb.linearVelocity = finalVelocity;
    }
}